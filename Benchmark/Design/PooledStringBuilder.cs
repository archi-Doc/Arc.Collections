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
    /// The maximum number of reusable sequence segments retained in the segment pool.
    /// </summary>
    public const int SegmentPoolCapacity = 4 * 1024;

    private static readonly ObjectPool<Segment> SegmentPool = new(static () => new(), SegmentPoolCapacity);

    private char[]? currentArray;
    private int currentIndex;

    private Segment? firstSegment;
    private Segment? lastSegment;

    private long length;
    private int nextChunkCapacity;

    /// <summary>
    /// Gets the number of characters written to this builder.
    /// </summary>
    public long Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.length;
    }

    /// <summary>
    /// Appends a character.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <exception cref="InvalidOperationException">
    /// This builder has already been finalized.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        var array = this.currentArray;

        if (array is null)
        {
            array = this.RentChunk();
            this.currentArray = array;
        }
        else if ((uint)this.currentIndex >= (uint)array.Length)
        {
            this.CommitCurrentChunk();

            array = this.RentChunk();
            this.currentArray = array;
        }

        array[this.currentIndex] = value;

        this.currentIndex++;
        this.length++;
    }

    /// <summary>
    /// Appends a contiguous range of characters.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    /// <exception cref="InvalidOperationException">
    /// This builder has already been finalized.
    /// </exception>
    public void Append(ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            var array = this.currentArray;

            if (array is null)
            {
                array = this.RentChunk();
                this.currentArray = array;
            }

            var availableLength = array.Length - this.currentIndex;

            if (availableLength == 0)
            {
                this.CommitCurrentChunk();
                continue;
            }

            var copyLength = Math.Min(availableLength, value.Length);

            value.Slice(0, copyLength)
                .CopyTo(array.AsSpan(this.currentIndex, copyLength));

            this.currentIndex += copyLength;
            this.length += copyLength;

            value = value.Slice(copyLength);
        }
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
    public override string ToString()
    {
        if (this.length == 0)
        {
            return string.Empty;
        }

        if (this.length > int.MaxValue)
        {
            ThrowStringTooLong();
        }

        var stringLength = (int)this.length;
        var first = this.firstSegment;
        var currentArray = this.currentArray;
        var currentIndex = this.currentIndex;

        // Most builders remain inside the first rented array.
        // The char[] constructor performs a single allocation and direct copy.
        if (first is null)
        {
            return new string(currentArray!, 0, currentIndex);
        }

        // A finalized sequence consisting of exactly one committed segment.
        if (first == this.lastSegment && currentIndex == 0)
        {
            return new string(first.Array!, 0, first.WrittenLength);
        }

        // Allocate the final string exactly once and copy every chunk directly
        // into the string's backing storage.
        return string.Create(
            stringLength,
            new StringCreationState(first, currentArray, currentIndex),
            static (destination, state) =>
            {
                var destinationIndex = 0;
                var segment = state.FirstSegment;

                while (segment is not null)
                {
                    var source = segment.Array!.AsSpan(0, segment.WrittenLength);

                    source.CopyTo(destination.Slice(destinationIndex));
                    destinationIndex += source.Length;

                    segment = segment.Next;
                }

                if (state.CurrentIndex != 0)
                {
                    state.CurrentArray!.AsSpan(0, state.CurrentIndex).CopyTo(destination.Slice(destinationIndex));
                }
            });
    }

    /// <summary>
    /// Returns all rented character arrays and sequence segments to their pools.
    /// </summary>
    /// <remarks>
    /// Any previously returned <see cref="ReadOnlySequence{Char}"/> becomes invalid
    /// after this method is called.
    /// </remarks>
    public void Dispose()
    {
        var currentArray = this.currentArray;
        if (currentArray is not null)
        {
            // char contains no managed references, so clearing is unnecessary.
            ArrayPool<char>.Shared.Return(currentArray);
            this.currentArray = null;
        }

        var segment = this.firstSegment;
        while (segment is not null)
        {
            var next = segment.Next;
            var array = segment.Array;

            if (array is not null)
            {
                ArrayPool<char>.Shared.Return(array);
            }

            segment.Reset();
            SegmentPool.Return(segment);

            segment = next;
        }

        this.currentIndex = 0;

        this.firstSegment = null;
        this.lastSegment = null;

        this.length = 0;
        this.nextChunkCapacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextChunkCapacity(int currentCapacity)
    {
        if (currentCapacity >= MaxChunkCapacity)
        {
            return MaxChunkCapacity;
        }

        if (currentCapacity > MaxChunkCapacity / 2)
        {
            return MaxChunkCapacity;
        }

        return currentCapacity << 1;
    }

    [DoesNotReturn]
    private static void ThrowStringTooLong()
        => throw new InvalidOperationException("The number of characters exceeds the maximum string length.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char[] RentChunk()
    {
        var capacity = this.nextChunkCapacity;
        if (capacity <= 0)
        {
            capacity = DefaultInitialCapacity;
        }

        var array = ArrayPool<char>.Shared.Rent(capacity);

        this.nextChunkCapacity = GetNextChunkCapacity(capacity);

        return array;
    }

    private void CommitCurrentChunk()
    {
        var array = this.currentArray;

        if (array is null)
        {
            return;
        }

        var writtenLength = this.currentIndex;

        if (writtenLength == 0)
        {
            return;
        }

        var segment = SegmentPool.Rent();

        segment.Initialize(array, writtenLength);

        var lastSegment = this.lastSegment;

        if (lastSegment is null)
        {
            this.firstSegment = segment;
        }
        else
        {
            lastSegment.Next = segment;
        }

        this.lastSegment = segment;

        this.currentArray = null;
        this.currentIndex = 0;
    }

    private readonly struct StringCreationState
    {
        public StringCreationState(Segment firstSegment, char[]? currentArray, int currentIndex)
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
        internal char[] Array;
        internal int WrittenLength;
        internal Segment? Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(char[] array, int writtenLength)
        {
            this.Array = array;
            this.WrittenLength = writtenLength;
            this.Next = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            this.Array = default!;
            this.WrittenLength = 0;
            this.Next = null;
        }
    }
}
