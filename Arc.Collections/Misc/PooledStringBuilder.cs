// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

#pragma warning disable SA1401 // Fields should be private

namespace Arc.Collections;

/// <summary>
/// Builds a string using pooled character arrays.<br/>
/// Although it has the constraints of being a ref struct and requiring Dispose() to be called to return the rented arrays,<br/>
/// it aims to achieve performance comparable to string interpolation.
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
    public const int DefaultInitialCapacity = 512;

    /// <summary>
    /// The maximum requested size, in characters, of a rented chunk.
    /// </summary>
    public const int MaxChunkCapacity = 32 * 1024;

    /// <summary>
    /// The maximum number of reusable segments retained in the segment pool.
    /// </summary>
    public const int SegmentPoolCapacity = 1024;

    private static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;
    private static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;
    private static readonly ObjectPool<Segment> SegmentPool = new(static () => new(), SegmentPoolCapacity);

    private char[]? currentArray;
    private Segment? firstSegment;
    private Segment? lastSegment;

    private int length;
    private int currentIndex;
    private int nextChunkCapacity;

    /// <summary>
    /// Gets the number of characters written to this builder.
    /// </summary>
    public readonly int Length
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
    /// Appends the formatted representation of a value.
    /// </summary>
    /// <typeparam name="T">The type of value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append<T>(T value)
        where T : ISpanFormattable
    {
        var array = this.currentArray;
        var index = this.currentIndex;

        if (array is not null)
        {
            var destination = array.AsSpan(index);

            if (value.TryFormat(destination, out var charsWritten, default, DefaultFormatProvider))
            {
                this.currentIndex = index + charsWritten;
                this.length += charsWritten;
                return;
            }
        }

        this.AppendFormattedSlow(value, array, index);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(bool value)
    => this.Append(value ? "True" : "False");

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
            throw new InvalidOperationException("The number of characters exceeds the maximum string length.");
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

                    segment.Array!.AsSpan(0, writtenLength).CopyTo(destination.Slice(destinationIndex, writtenLength));

                    destinationIndex += writtenLength;
                    segment = segment.Next;
                }
                while (segment is not null);

                var currentIndex = state.CurrentIndex;
                if (currentIndex != 0)
                {
                    state.CurrentArray.AsSpan(0, currentIndex).CopyTo(destination.Slice(destinationIndex, currentIndex));
                }
            });
    }

    /// <summary>
    /// Returns all rented character arrays and segments to their pools.
    /// </summary>
    public void Dispose()
    {
        if (this.currentArray is not null)
        {
            // char contains no managed references, so clearing is unnecessary.
            CharPool.Return(this.currentArray);
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

            segment.Array = null;
            segment.Next = null;
            segment.WrittenLength = 0;

            SegmentPool.Return(segment);
        }

        this = default;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AppendFormattedFallback<T>(
        ref PooledStringBuilder builder,
        T value)
        where T : ISpanFormattable
    {
        // An arbitrary ISpanFormattable implementation may require more than
        // MaxChunkCapacity characters. Fall back to its string representation.
        var text = value.ToString(default, DefaultFormatProvider);

        if (text is not null)
        {
            builder.Append(text.AsSpan());
        }
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendFormattedSlow<T>(T value, char[]? array, int index)
        where T : ISpanFormattable
    {
        // Preserve the contents already written to the current chunk.
        if (array is not null && index != 0)
        {
            this.AddSegment(array, index);
            array = null;
        }
        else if (array is not null)
        {
            // The empty current array is reused for the first formatting attempt.
            index = 0;
        }

        while (true)
        {
            if (array is null)
            {
                array = this.RentChunk(DefaultInitialCapacity);
            }

            if (value.TryFormat(array, out var charsWritten, default, DefaultFormatProvider))
            {
                this.currentArray = array;
                this.currentIndex = charsWritten;
                this.length += charsWritten;
                return;
            }

            var currentLength = array.Length;

            CharPool.Return(array);
            array = null;

            if (currentLength >= MaxChunkCapacity)
            {
                var text = value.ToString(default, DefaultFormatProvider);
                if (text is not null)
                {
                    this.Append(text.AsSpan());
                }

                AppendFormattedFallback(ref this, value);
                return;
            }

            var minimumCapacity = currentLength <= MaxChunkCapacity / 2
                ? currentLength << 1
                : MaxChunkCapacity;

            array = CharPool.Rent(minimumCapacity);
        }
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
        segment.Array = array;
        segment.WrittenLength = writtenLength;
        segment.Next = null;

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

    private readonly struct StringCreationState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        internal char[]? Array;
        internal Segment? Next;
        internal int WrittenLength;
    }
}
