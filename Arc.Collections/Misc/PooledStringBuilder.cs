// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1405 // Debug.Assert should provide message text

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
    public readonly int Length => this.length;

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
        this.Append<T>(value, default, DefaultFormatProvider);
    }

    /// <summary>
    /// Appends the formatted representation of a value using the specified format and format provider.
    /// </summary>
    /// <typeparam name="T">The type of value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    /// <param name="format">The optional format string to apply during formatting.</param>
    /// <param name="formatProvider">The format provider that supplies culture-specific formatting information.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append<T>(T value, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
        where T : ISpanFormattable
    {
        var array = this.currentArray;
        var index = this.currentIndex;

        if (array is not null)
        {
            var destination = array.AsSpan(index);
            if (value.TryFormat(destination, out var charsWritten, format, formatProvider))
            {
                this.currentIndex = index + charsWritten;
                this.length += charsWritten;
                return;
            }
        }

        this.AppendFormattedSlow(value, format, formatProvider, array, index);
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
    /// Appends the invariant string representation of a Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(bool value)
        => this.Append(value ? "True" : "False");

    /// <summary>
    /// Appends a line feed character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine()
        => this.Append(BaseHelper.LfChar);

    /// <summary>
    /// Appends a contiguous range of characters followed by a line feed character.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(ReadOnlySpan<char> value)
    {
        this.Append(value);
        this.Append(BaseHelper.LfChar);
    }

    /// <summary>
    /// Appends the formatted representation of a value followed by a line feed
    /// character.
    /// </summary>
    /// <typeparam name="T">The type of value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine<T>(T value)
        where T : ISpanFormattable
    {
        this.Append(value);
        this.Append(BaseHelper.LfChar);
    }

    /// <summary>
    /// Appends the formatted representation of a value using the specified format
    /// and format provider, followed by a line feed character.
    /// </summary>
    /// <typeparam name="T">The type of value to append.</typeparam>
    /// <param name="value">The value to format and append.</param>
    /// <param name="format">The optional format string to apply during formatting.</param>
    /// <param name="formatProvider">
    /// The format provider that supplies culture-specific formatting information.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine<T>(T value, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
        where T : ISpanFormattable
    {
        this.Append(value, format, formatProvider);
        this.Append('\n');
    }

    /// <summary>
    /// Creates a string containing all characters written to this builder.
    /// </summary>
    /// <returns>
    /// A newly allocated string containing the builder's current contents.
    /// </returns>
    public override readonly string ToString()
    {
        if (this.length == 0)
        {
            return string.Empty;
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
            this.length,
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

    /// <summary>
    /// Gets the last character and the character immediately preceding it.
    /// </summary>
    /// <param name="previous">
    /// The character immediately preceding the last character,
    /// or <c>'\0'</c> if none exists.
    /// </param>
    /// <param name="last">The last character, or <c>'\0'</c> if none exists.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetLastTwoChars(out char previous, out char last)
    {
        var array = this.currentArray;
        var index = this.currentIndex;

        // Common case: both characters are in the current array.
        if (index >= 2)
        {
            previous = array![index - 2];
            last = array![index - 1];
            return;
        }

        var lastSegment = this.lastSegment;
        if (lastSegment is null)
        {
            last = default;
            previous = default;
            return;
        }

        if (index == 1)
        {
            previous = lastSegment.Array![lastSegment.WrittenLength - 1];
            last = array![0];
            return;
        }

        var writtenLength = lastSegment.WrittenLength;
        var lastSegmentArray = lastSegment.Array!;
        last = lastSegmentArray[writtenLength - 1];
        if (writtenLength >= 2)
        {
            previous = lastSegmentArray[writtenLength - 2];
            return;
        }

        // The last segment contains only one character.
        // Find the preceding non-empty segment.
        Segment? precedingSegment = null;
        var segment = this.firstSegment;
        while (segment != lastSegment)
        {
            if (segment!.WrittenLength != 0)
            {
                precedingSegment = segment;
            }

            segment = segment.Next;
        }

        previous = precedingSegment is null ? default : precedingSegment.Array![precedingSegment.WrittenLength - 1];
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
    private void AppendFormattedSlow<T>(T value, ReadOnlySpan<char> format, IFormatProvider? formatProvider, char[]? array, int index)
        where T : ISpanFormattable
    {
        if (array is not null)
        {
            if (index != 0)
            {
                this.AddSegment(array, index);
                array = null;
            }

            this.currentArray = null;
            this.currentIndex = 0;
        }

        while (true)
        {
            if (array is null)
            {
                array = this.RentChunk(DefaultInitialCapacity);
            }

            bool succeeded;
            int charsWritten;

            try
            {
                succeeded = value.TryFormat(array, out charsWritten, format, formatProvider);
            }
            catch
            {
                CharPool.Return(array);
                throw;
            }

            if (succeeded)
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
                var formatString = format.IsEmpty ? null : format.ToString();
                this.Append(value.ToString(formatString, formatProvider));
                return;
            }

            array = this.RentChunk(GetNextChunkCapacity(currentLength));
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
        Debug.Assert(writtenLength > 0);
        Debug.Assert(writtenLength <= array.Length);

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
