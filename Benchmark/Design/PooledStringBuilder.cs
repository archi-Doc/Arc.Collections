// Copyright (c) All contributors.
// All rights reserved.
// Licensed under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Kimi.Compiler.Lexing;

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
    /// The default requested capacity of the first segment.
    /// </summary>
    public const int DefaultInitialCapacity = 256;

    /// <summary>
    /// The maximum requested capacity of a single segment.
    /// </summary>
    public const int MaxSegmentCapacity = 32 * 1024;

    /// <summary>
    /// The maximum number of segment objects retained in the segment pool.
    /// </summary>
    public const int SegmentPoolCapacity = 4 * 1024;

    private static readonly ObjectPool<Segment> SegmentPool = new(static () => new(), SegmentPoolCapacity);

    private Segment? firstSegment;
    private Segment? lastSegment;

    private string? cachedString;
    private int length;
    private int nextSegmentCapacity;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledStringBuilder"/> struct.
    /// </summary>
    /// <param name="initialCapacity">
    /// The requested capacity of the first segment.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is outside the supported range.
    /// </exception>
    public PooledStringBuilder(int initialCapacity)
    {
        if ((uint)(initialCapacity - 1) >= MaxSegmentCapacity)
        {
            ThrowInitialCapacityOutOfRange();
        }

        this.firstSegment = null;
        this.lastSegment = null;
        this.cachedString = null;

        this.length = 0;
        this.nextSegmentCapacity = initialCapacity;
        this.isDisposed = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledStringBuilder"/> struct
    /// using the default initial capacity.
    /// </summary>
    public PooledStringBuilder()
        : this(DefaultInitialCapacity)
    {
    }

    /// <summary>
    /// Gets the number of characters stored in this builder.
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
    /// <exception cref="ObjectDisposedException">
    /// This builder has already been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        if (this.isDisposed)
        {
            ThrowDisposed();
        }

        var segment = this.lastSegment;

        if (segment is null ||
            (uint)segment.WrittenLength >= (uint)segment.Array.Length)
        {
            segment = this.AddSegment(this.nextSegmentCapacity);
        }

        segment.Array[segment.WrittenLength++] = value;

        this.length++;
        this.cachedString = null;
    }

    /// <summary>
    /// Appends a contiguous range of characters.
    /// </summary>
    /// <param name="value">The characters to append.</param>
    /// <exception cref="ObjectDisposedException">
    /// This builder has already been disposed.
    /// </exception>
    /// <exception cref="OutOfMemoryException">
    /// The resulting string would exceed the supported maximum length.
    /// </exception>
    public void Append(ReadOnlySpan<char> value)
    {
        if (this.isDisposed)
        {
            ThrowDisposed();
        }

        if (value.IsEmpty)
        {
            return;
        }

        if (value.Length > int.MaxValue - this.length)
        {
            ThrowStringTooLong();
        }

        this.cachedString = null;

        while (!value.IsEmpty)
        {
            var segment = this.lastSegment;

            if (segment is null ||
                (uint)segment.WrittenLength >= (uint)segment.Array.Length)
            {
                segment = this.AddSegment(
                    GetRequiredSegmentCapacity(
                        this.nextSegmentCapacity,
                        value.Length));
            }

            var writtenLength = segment.WrittenLength;
            var copyLength = Math.Min(
                segment.Array.Length - writtenLength,
                value.Length);

            value[..copyLength].CopyTo(
                segment.Array.AsSpan(writtenLength, copyLength));

            segment.WrittenLength = writtenLength + copyLength;
            this.length += copyLength;

            value = value[copyLength..];
        }
    }

    /// <summary>
    /// Appends a string.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.Append(value.AsSpan());
    }

    /// <summary>
    /// Creates or retrieves a string containing all appended characters.
    /// </summary>
    /// <returns>
    /// A string containing all characters stored in this builder.
    /// </returns>
    /// <remarks>
    /// This operation does not release the pooled resources and does not prevent
    /// further calls to <see cref="Append(char)"/> or
    /// <see cref="Append(ReadOnlySpan{Char})"/>.
    ///
    /// Repeated calls return the cached string until the builder is modified.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// This builder has already been disposed.
    /// </exception>
    public override string ToString()
    {
        if (this.isDisposed)
        {
            ThrowDisposed();
        }

        var cachedString = this.cachedString;

        if (cachedString is not null)
        {
            return cachedString;
        }

        if (this.length == 0)
        {
            return string.Empty;
        }

        var firstSegment = this.firstSegment!;

        if (ReferenceEquals(firstSegment, this.lastSegment))
        {
            cachedString = new string(
                firstSegment.Array,
                0,
                firstSegment.WrittenLength);
        }
        else
        {
            cachedString = string.Create(
                this.length,
                firstSegment,
                static (destination, segment) =>
                {
                    var destinationIndex = 0;

                    while (segment is not null)
                    {
                        var writtenLength = segment.WrittenLength;

                        segment.Array.AsSpan(0, writtenLength).CopyTo(destination.Slice(
                                destinationIndex, writtenLength));

                        destinationIndex += writtenLength;
                        segment = segment.Next!;
                    }
                });
        }

        this.cachedString = cachedString;
        return cachedString;
    }

    /// <summary>
    /// Returns all rented character arrays and segment objects to their pools.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        var segment = this.firstSegment;

        while (segment is not null)
        {
            var next = segment.Next;
            var array = segment.Array;

            ArrayPool<char>.Shared.Return(array);

            segment.Reset();
            SegmentPool.Return(segment);

            segment = next;
        }

        this.firstSegment = null;
        this.lastSegment = null;
        this.cachedString = null;

        this.length = 0;
        this.nextSegmentCapacity = 0;
        this.isDisposed = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetRequiredSegmentCapacity(int nextCapacity, int remainingLength)
    {
        if (nextCapacity <= 0)
        {
            nextCapacity = DefaultInitialCapacity;
        }

        if (remainingLength <= nextCapacity)
        {
            return nextCapacity;
        }

        return Math.Min(remainingLength, MaxSegmentCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextSegmentCapacity(int currentCapacity)
    {
        if (currentCapacity >= MaxSegmentCapacity)
        {
            return MaxSegmentCapacity;
        }

        return Math.Min(currentCapacity << 1, MaxSegmentCapacity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Segment AddSegment(int requestedCapacity)
    {
        if (requestedCapacity <= 0)
        {
            requestedCapacity = DefaultInitialCapacity;
        }
        else if (requestedCapacity > MaxSegmentCapacity)
        {
            requestedCapacity = MaxSegmentCapacity;
        }

        var array = ArrayPool<char>.Shared.Rent(requestedCapacity);
        var segment = SegmentPool.Rent();

        segment.Initialize(array);

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
        this.nextSegmentCapacity =
            GetNextSegmentCapacity(requestedCapacity);

        return segment;
    }

    [DoesNotReturn]
    private static void ThrowInitialCapacityOutOfRange()
        => throw new ArgumentOutOfRangeException(
            "initialCapacity",
            $"The initial capacity must be between 1 and {MaxSegmentCapacity}.");

    [DoesNotReturn]
    private static void ThrowDisposed()
        => throw new ObjectDisposedException(nameof(PooledStringBuilder));

    [DoesNotReturn]
    private static void ThrowStringTooLong()
        => throw new OutOfMemoryException(
            "The resulting string exceeds the maximum supported length.");

    private sealed class Segment
    {
        internal char[] Array = null!;

        internal int WrittenLength;

        internal Segment? Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Initialize(char[] array)
        {
            this.Array = array;
            this.WrittenLength = 0;
            this.Next = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            this.Array = null!;
            this.WrittenLength = 0;
            this.Next = null;
        }
    }
}
