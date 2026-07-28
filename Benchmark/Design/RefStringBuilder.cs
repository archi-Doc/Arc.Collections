// Copyright (c) All contributors.
// All rights reserved.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections;

#pragma warning disable SA1401 // Fields should be private

namespace Kimi.Compiler.Lexing;

/// <summary>
/// Builds a <see cref="ReadOnlySequence{Char}"/> backed by pooled character arrays.
/// </summary>
/// <remarks>
/// The returned sequence directly references pooled arrays owned by this builder.
/// It is valid only until <see cref="Dispose"/> is called.
///
/// <para>
/// <see cref="ToString"/> creates an independent string and therefore remains valid
/// after this builder is disposed.
/// </para>
/// </remarks>
public ref struct RefStringBuilder
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

    private static readonly ObjectPool<PooledSequenceSegment> SegmentPool =
        new(static () => new PooledSequenceSegment(), SegmentPoolCapacity);

    private char[]? currentArray;
    private int currentIndex;

    private PooledSequenceSegment? firstSegment;
    private PooledSequenceSegment? lastSegment;

    private long length;
    private int nextChunkCapacity;

    private bool isFinalized;
    private ReadOnlySequence<char> sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefStringBuilder"/> struct.
    /// </summary>
    /// <param name="initialCapacity">
    /// The initial chunk size to request from <see cref="ArrayPool{Char}"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is less than or equal to zero,
    /// or greater than <see cref="MaxChunkCapacity"/>.
    /// </exception>
    public RefStringBuilder(int initialCapacity)
    {
        if ((uint)(initialCapacity - 1) >= MaxChunkCapacity)
        {
            ThrowInitialCapacityOutOfRange();
        }

        this.currentArray = null;
        this.currentIndex = 0;

        this.firstSegment = null;
        this.lastSegment = null;

        this.length = 0;
        this.nextChunkCapacity = initialCapacity;

        this.isFinalized = false;
        this.sequence = ReadOnlySequence<char>.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefStringBuilder"/> struct
    /// with the default initial capacity.
    /// </summary>
    public RefStringBuilder()
        : this(DefaultInitialCapacity)
    {
    }

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
    public void Add(char value)
    {
        if (this.isFinalized)
        {
            ThrowAlreadyFinalized();
        }

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
    public void AddRange(ReadOnlySpan<char> value)
    {
        if (this.isFinalized)
        {
            ThrowAlreadyFinalized();
        }

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
    /// Appends a string.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// This builder has already been finalized.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.AddRange(value.AsSpan());
    }

    /// <summary>
    /// Finalizes this builder and returns the produced character sequence.
    /// </summary>
    /// <returns>
    /// A sequence referencing the pooled arrays owned by this builder.
    /// The sequence remains valid only until <see cref="Dispose"/> is called.
    /// </returns>
    public ReadOnlySequence<char> ToReadOnlySequence()
    {
        if (this.isFinalized)
        {
            return this.sequence;
        }

        this.isFinalized = true;

        if (this.length == 0)
        {
            this.sequence = ReadOnlySequence<char>.Empty;
            return this.sequence;
        }

        // Single-chunk fast path. No segment object is required.
        if (this.firstSegment is null)
        {
            this.sequence = new ReadOnlySequence<char>(
                this.currentArray!.AsMemory(0, this.currentIndex));

            return this.sequence;
        }

        // Commit the last partially filled array.
        if (this.currentIndex != 0)
        {
            this.CommitCurrentChunk();
        }

        var first = this.firstSegment!;
        var last = this.lastSegment!;

        this.sequence = new ReadOnlySequence<char>(
            first,
            0,
            last,
            last.Memory.Length);

        return this.sequence;
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
        if (first == this.lastSegment &&
            currentIndex == 0)
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
                    var source = segment.Array!
                        .AsSpan(0, segment.WrittenLength);

                    source.CopyTo(destination.Slice(destinationIndex));
                    destinationIndex += source.Length;

                    segment = segment.GetNextSegment();
                }

                if (state.CurrentIndex != 0)
                {
                    state.CurrentArray!
                        .AsSpan(0, state.CurrentIndex)
                        .CopyTo(destination.Slice(destinationIndex));
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
            var next = segment.GetNextSegment();
            var array = segment.Array;

            if (array is not null)
            {
                ArrayPool<char>.Shared.Return(array);
            }

            segment.ResetForPool();
            SegmentPool.Return(segment);

            segment = next;
        }

        this.currentIndex = 0;

        this.firstSegment = null;
        this.lastSegment = null;

        this.length = 0;
        this.nextChunkCapacity = 0;

        this.sequence = ReadOnlySequence<char>.Empty;
        this.isFinalized = true;
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
    private static void ThrowInitialCapacityOutOfRange()
        => throw new ArgumentOutOfRangeException(
            "initialCapacity",
            $"The initial capacity must be between 1 and {MaxChunkCapacity}.");

    [DoesNotReturn]
    private static void ThrowAlreadyFinalized()
        => throw new InvalidOperationException(
            "The sequence has already been finalized.");

    [DoesNotReturn]
    private static void ThrowStringTooLong()
        => throw new InvalidOperationException(
            "The number of characters exceeds the maximum string length.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char[] RentChunk()
    {
        var capacity = this.nextChunkCapacity;

        // Handles a default-initialized SequenceBuilder.
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

        segment.Initialize(
            array,
            writtenLength,
            this.length - writtenLength);

        var lastSegment = this.lastSegment;

        if (lastSegment is null)
        {
            this.firstSegment = segment;
        }
        else
        {
            lastSegment.SetNext(segment);
        }

        this.lastSegment = segment;

        this.currentArray = null;
        this.currentIndex = 0;
    }

    private readonly struct StringCreationState
    {
        public StringCreationState(
            PooledSequenceSegment firstSegment,
            char[]? currentArray,
            int currentIndex)
        {
            this.FirstSegment = firstSegment;
            this.CurrentArray = currentArray;
            this.CurrentIndex = currentIndex;
        }

        public readonly PooledSequenceSegment FirstSegment;

        public readonly char[]? CurrentArray;

        public readonly int CurrentIndex;
    }

    private sealed class PooledSequenceSegment : ReadOnlySequenceSegment<char>
    {
        internal char[]? Array;
        internal int WrittenLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(
            char[] array,
            int writtenLength,
            long runningIndex)
        {
            this.Array = array;
            this.WrittenLength = writtenLength;

            this.Memory = array.AsMemory(0, writtenLength);
            this.RunningIndex = runningIndex;
            this.Next = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNext(PooledSequenceSegment next)
            => this.Next = next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledSequenceSegment? GetNextSegment()
            => (PooledSequenceSegment?)this.Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetForPool()
        {
            this.Array = null;
            this.WrittenLength = 0;

            this.Memory = default;
            this.RunningIndex = 0;
            this.Next = null;
        }
    }
}
