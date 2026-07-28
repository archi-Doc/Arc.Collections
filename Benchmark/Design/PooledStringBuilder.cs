// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1401 // Fields should be private

namespace Arc.Collections;

/// <summary>
/// Builds a string using pooled character arrays.
/// </summary>
/// <remarks>
/// Character arrays and segment objects are returned to their pools when
/// <see cref="Dispose"/> is called.
/// </remarks>
public ref struct PooledStringBuilder
{
    /// <summary>
    /// The default size, in characters, of the first rented chunk.
    /// </summary>
    public const int DefaultInitialCapacity = 256;

    /// <summary>
    /// The maximum requested size, in characters, of a rented chunk.
    /// </summary>
    public const int MaxChunkCapacity = 32 * 1024;

    /// <summary>
    /// The maximum number of reusable segments retained in the segment pool.
    /// </summary>
    public const int SegmentPoolCapacity = 4 * 1024;

    private static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;

    private static readonly ObjectPool<Segment> SegmentPool = new(static () => new(), SegmentPoolCapacity);

    private char[]? currentArray;
    private Segment? firstSegment;
    private Segment? lastSegment;

    private long length;
    private int currentIndex;
    private int nextChunkCapacity;

    /// <summary>
    /// Gets the number of characters written to this builder.
    /// </summary>
    public readonly long Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.length;
    }

    /// <summary>
    /// Appends a character.
    /// </summary>
    /// <param name="value">The character to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        var array = this.currentArray;
        var index = this.currentIndex;

        if (array is null)
        {
            array = this.RentChunk(DefaultInitialCapacity);
            this.currentArray = array;
        }
        else if ((uint)index >= (uint)array.Length)
        {
            this.AddSegment(array, index);

            array = this.RentChunk(DefaultInitialCapacity);
            this.currentArray = array;
            index = 0;
        }

        array[index] = value;

        this.currentIndex = index + 1;
        this.length++;
    }

    /// <summary>
    /// Appends a contiguous range of characters.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        var array = this.currentArray;
        var index = this.currentIndex;

        if (array is not null)
        {
            var availableLength = array.Length - index;

            // Common fast path: the entire input fits in the current chunk.
            if ((uint)value.Length <= (uint)availableLength)
            {
                value.CopyTo(array.AsSpan(index));

                this.currentIndex = index + value.Length;
                this.length += value.Length;
                return;
            }
        }

        this.AppendSlow(value, array, index);
    }

    /// <summary>
    /// Creates a string containing all characters written to this builder.
    /// </summary>
    /// <returns>
    /// A newly allocated string containing the builder's current contents.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The number of characters exceeds the maximum string length.
    /// </exception>
    public override readonly string ToString()
    {
        var totalLength = this.length;

        if (totalLength == 0)
        {
            return string.Empty;
        }

        if ((ulong)totalLength > int.MaxValue)
        {
            ThrowStringTooLong();
        }

        var array = this.currentArray;
        var index = this.currentIndex;
        var first = this.firstSegment;

        // Most instances use only one rented array.
        if (first is null)
        {
            return new string(array!, 0, index);
        }

        return string.Create(
            (int)totalLength,
            new StringCreationState(first, array, index),
            static (destination, state) =>
            {
                var destinationIndex = 0;
                var segment = state.FirstSegment;

                do
                {
                    var writtenLength = segment.WrittenLength;

                    segment.Array!
                        .AsSpan(0, writtenLength)
                        .CopyTo(destination.Slice(destinationIndex, writtenLength));

                    destinationIndex += writtenLength;
                    segment = segment.Next;
                }
                while (segment is not null);

                var currentIndex = state.CurrentIndex;

                if (currentIndex != 0)
                {
                    state.CurrentArray!
                        .AsSpan(0, currentIndex)
                        .CopyTo(destination.Slice(destinationIndex, currentIndex));
                }
            });
    }

    /// <summary>
    /// Returns all rented character arrays and segments to their pools.
    /// </summary>
    public void Dispose()
    {
        var currentArray = this.currentArray;

        if (currentArray is not null)
        {
            // char contains no managed references, so clearing is unnecessary.
            CharPool.Return(currentArray);
        }

        var segment = this.firstSegment;

        while (segment is not null)
        {
            var next = segment.Next;
            var array = segment.Array;

            if (array is not null)
            {
                CharPool.Return(array);
            }

            segment.Reset();
            SegmentPool.Return(segment);

            segment = next;
        }

        this = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendSlow(ReadOnlySpan<char> value, char[]? array, int index)
    {
        var totalLength = this.length;
        while (!value.IsEmpty)
        {
            if (array is null)
            {
                array = this.RentChunk(value.Length);
                index = 0;
            }
            else if ((uint)index >= (uint)array.Length)
            {
                this.AddSegment(array, index);

                array = this.RentChunk(value.Length);
                index = 0;
            }

            var availableLength = array.Length - index;
            var copyLength = value.Length;

            if (copyLength > availableLength)
            {
                copyLength = availableLength;
            }

            value.Slice(0, copyLength).CopyTo(array.AsSpan(index, copyLength));

            index += copyLength;
            totalLength += copyLength;
            value = value.Slice(copyLength);
        }

        this.currentArray = array;
        this.currentIndex = index;
        this.length = totalLength;
    }

    /// <summary>
    /// Rents a chunk large enough for the expected write while preserving geometric growth for small writes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char[] RentChunk(int minimumCapacity)
    {
        var capacity = this.nextChunkCapacity;
        if (capacity <= 0)
        {
            capacity = DefaultInitialCapacity;
        }

        // Avoid creating many small segments when a large span is appended.
        if (minimumCapacity > capacity)
        {
            capacity = minimumCapacity;

            if (capacity > MaxChunkCapacity)
            {
                capacity = MaxChunkCapacity;
            }
        }

        var array = CharPool.Rent(capacity);
        this.nextChunkCapacity = GetNextChunkCapacity(capacity);
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddSegment(char[] array, int writtenLength)
    {
        var segment = SegmentPool.Rent();
        segment.Initialize(array, writtenLength);

        var last = this.lastSegment;
        if (last is null)
        {
            this.firstSegment = segment;
        }
        else
        {
            last.Next = segment;
        }

        this.lastSegment = segment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextChunkCapacity(int currentCapacity)
    {
        if (currentCapacity >= MaxChunkCapacity / 2)
        {
            return MaxChunkCapacity;
        }

        return currentCapacity << 1;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowStringTooLong()
        => throw new InvalidOperationException(            "The number of characters exceeds the maximum string length.");

    private readonly struct StringCreationState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringCreationState(            Segment firstSegment,            char[]? currentArray,            int currentIndex)
        {
            this.FirstSegment = firstSegment;
            this.CurrentArray = currentArray;
            this.CurrentIndex = currentIndex;
        }

        public readonly Segment FirstSegment;

        public readonly char[]? CurrentArray;

        public readonly int CurrentIndex;
    }

    private sealed class Segment
    {
        internal char[]? Array;
        internal Segment? Next;
        internal int WrittenLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Initialize(char[] array, int writtenLength)
        {
            this.Array = array;
            this.WrittenLength = writtenLength;
            this.Next = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            this.Array = null;
            this.Next = null;
            this.WrittenLength = 0;
        }
    }
}
