// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable SA1124
#pragma warning disable SA1202

namespace Arc.Collections;

/// <summary>
/// Pools byte arrays in power-of-two buckets with reference-counted ownership.
/// </summary>
/// <remarks>
/// Renting and returning arrays are thread-safe; configure bucket limits before sharing the pool.
/// Arrays are not cleared. Each owned reference must be returned once, and all views become
/// invalid after the final return. Reference counting does not synchronize access to the bytes.
/// </remarks>
public class BytePool
{
    /// <summary>
    /// The reference count value that marks an array owned by a single owner.
    /// </summary>
    public const int SingleCount = int.MaxValue;
    private const int DefaultMaxArrayLength = 1024 * 1024 * 16; // 16 MB
    private const int DefaultPoolLimit = 256;
    private const int StandardArrayLength = 1024 * 32; // 32 KB (TinyhandSerializer.InitialBufferSize, ByteSequence.DefaultVaultSize)
    private const int StandardPoolLimit = 1024;

    /// <summary>
    /// The shared default pool.
    /// </summary>
    public static readonly BytePool Default;

    static BytePool()
    {
        Default = BytePool.CreateExponential();
        Default.SetPoolLimit(StandardArrayLength, StandardPoolLimit);
    }

    /// <summary>
    /// Tracks ownership of a pooled or externally supplied byte array.
    /// </summary>
    /// <remarks>
    /// Views and reference copies do not acquire ownership. Use <see cref="IncrementAndShare" />
    /// for each additional owner, and return each owned reference once.
    /// Do not access this object or its views after the final return; pooled instances may be reused.
    /// </remarks>
    public class RentedArray : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="RentedArray"/> instance with the specified byte array.<br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="array">The byte array.</param>
        /// <returns>A new <see cref="RentedArray"/> instance.</returns>
        public static RentedArray CreateFrom(byte[] array)
        {
            return new(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RentedArray"/> class from a byte array.<br/>
        /// This is a feature for compatibility with conventional memory management (e.g new byte[]), <br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="array">A byte array (allocated with 'new').</param>
        internal RentedArray(byte[] array)
        {
            this.bucket = null;
            this.byteArray = array;
            this.ResetCount();
        }

        internal RentedArray(Bucket bucket)
        {
            this.bucket = bucket;
            this.byteArray = new byte[bucket.ArrayLength];
            this.ResetCount();
        }

        #region FieldAndProperty

        private readonly byte[] byteArray;
        private readonly Bucket? bucket;
        private int count;

        /// <summary>
        /// Gets the rented byte array.
        /// </summary>
        public byte[] Array => this.byteArray;

        /// <summary>
        /// Gets a value indicating whether the reference count is positive, including for an unpooled array.
        /// </summary>
        public bool IsRented => Volatile.Read(ref this.count) > 0;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) has been returned.
        /// </summary>
        public bool IsReturned => Volatile.Read(ref this.count) <= 0;

        /// <summary>
        /// Gets the reference count of the owner.
        /// </summary>
        public int ReferenceCount
        {
            get
            {
                var c = Volatile.Read(ref this.count);
                if (c == SingleCount)
                {
                    return 1;
                }
                else
                {
                    return c;
                }
            }
        }

        #endregion

        /// <summary>
        /// Increments the reference count and returns the current <see cref="RentedArray"/> instance.
        /// </summary>
        /// <returns>The current <see cref="RentedArray"/> instance.</returns>
        public RentedArray IncrementAndShare()
        {
            if (!this.TryIncrement())
            {
                throw new InvalidOperationException("The reference counter cannot be less than or equal to 0.");
            }

            return this;
        }

        /// <summary>
        /// Increments the reference count and attempts to share the byte array.
        /// </summary>
        /// <returns><see langword="true"/> if the increment and sharing were successful; otherwise, <see langword="false"/>.</returns>
        public bool TryIncrement()
        {
            int currentCount;
            int newCount;
            do
            {
                currentCount = Volatile.Read(ref this.count);
                if (currentCount <= 0)
                {
                    return false;
                }

                if (currentCount == SingleCount)
                {
                    newCount = 2;
                }
                else
                {
                    if (currentCount == SingleCount - 1)
                    {
                        throw new InvalidOperationException("The reference counter has reached its maximum value.");
                    }

                    newCount = currentCount + 1;
                }
            }
            while (Interlocked.CompareExchange(ref this.count, newCount, currentCount) != currentCount);

            return true;
        }

        /// <summary>
        /// Releases one owned reference. On the final release, a pooled array is offered back to its bucket.
        /// </summary>
        /// <returns><see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentedArray? Return()
        {
            int currentCount;
            int newCount;
            do
            {
                currentCount = Volatile.Read(ref this.count);
                if (currentCount <= 0)
                {
                    throw new InvalidOperationException("The reference counter cannot be less than or equal to 0.");
                }

                newCount = currentCount == SingleCount ? 0 : currentCount - 1;
            }
            while (Interlocked.CompareExchange(ref this.count, newCount, currentCount) != currentCount);

            if (newCount == 0 && this.bucket != null)
            {
                this.bucket.Queue.TryEnqueue(this);
            }

            return null;
        }

        /// <summary>
        /// Creates a <see cref="RentedMemory"/> object from the current <see cref="RentedArray"/> instance.
        /// </summary>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedMemory AsMemory()
            => new(this);

        /// <summary>
        /// Creates a <see cref="RentedMemory"/> object by specifying the start index.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedMemory AsMemory(int start)
            => new(this, this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Creates a <see cref="RentedMemory"/> object by specifying the start index and length.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedMemory AsMemory(int start, int length)
            => new(this, this.byteArray, start, length);

        /// <summary>
        /// Creates a <see cref="RentedReadOnlyMemory"/> object from the current <see cref="RentedArray"/> instance.
        /// </summary>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedReadOnlyMemory AsReadOnlyMemory()
            => new(this);

        /// <summary>
        /// Creates a <see cref="RentedReadOnlyMemory"/> object by specifying the start index.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedReadOnlyMemory AsReadOnlyMemory(int start)
            => new(this, this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Creates a <see cref="RentedReadOnlyMemory"/> object by specifying the start index and length.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A view of the same bytes, without an additional owned reference.</returns>
        public RentedReadOnlyMemory AsReadOnlyMemory(int start, int length)
            => new(this, this.byteArray, start, length);

        /// <summary>
        /// Creates a <see cref="Span{T}"/> object from the current <see cref="RentedArray"/> instance.
        /// </summary>
        /// <returns>A <see cref="Span{T}"/> object.</returns>
        public Span<byte> AsSpan()
            => new(this.byteArray);

        /// <summary>
        /// Creates a <see cref="Span{T}"/> object by specifying the start index.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <returns>A <see cref="Span{T}"/> object.</returns>
        public Span<byte> AsSpan(int start)
            => new(this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Creates a <see cref="Span{T}"/> object by specifying the start index and length.
        /// </summary>
        /// <param name="start">The start index of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A <see cref="Span{T}"/> object.</returns>
        public Span<byte> AsSpan(int start, int length)
            => new(this.byteArray, start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetCount()
            => this.count = SingleCount;

        /// <summary>
        /// Releases one owned reference, as <see cref="Return"/> does.
        /// </summary>
        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Provides a writable byte-memory view with optional reference-counted ownership.
    /// </summary>
    /// <remarks>
    /// Copying, slicing, or converting a view does not increment the reference count.
    /// Use <see cref="IncrementAndShare" /> for an additional owner. Return or dispose each owned
    /// reference once; doing so does not clear this struct or its copies.
    /// </remarks>
    public readonly struct RentedMemory : IDisposable
    {
        /// <summary>
        /// An empty <see cref="RentedMemory"/>.
        /// </summary>
        public static readonly RentedMemory Empty = default;

        /// <summary>
        /// Creates a new <see cref="RentedMemory"/> instance from the specified byte array.<br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="source">The source byte array.</param>
        /// <returns>A new <see cref="RentedMemory"/> instance.</returns>
        public static RentedMemory CreateFrom(byte[] source)
        {
            return new RentedArray(source).AsMemory();
        }

        /// <summary>
        /// Creates a new <see cref="RentedMemory"/> instance from the specified byte array.<br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="source">The source byte array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <returns>A new <see cref="RentedMemory"/> instance.</returns>
        public static RentedMemory CreateFrom(byte[] source, int start, int length)
        {
            return new RentedArray(source).AsMemory(start, length);
        }

        /// <summary>
        /// Wraps array-backed memory without copying or tracking ownership.
        /// </summary>
        /// <param name="source">The memory to wrap.</param>
        /// <returns>A view over the underlying array, or an empty view if the array cannot be obtained.</returns>
        public static RentedMemory CreateFrom(ReadOnlyMemory<byte> source)
        {
            if (MemoryMarshal.TryGetArray<byte>(source, out var segment) &&
                segment.Array is not null)
            {
                return new(segment.Array, segment.Offset, segment.Count);
            }

            return default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RentedMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        internal RentedMemory(byte[] byteArray, int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal RentedMemory(RentedArray array)
        {
            this.array = array;
            this.byteArray = array.Array;
            this.length = array.Array.Length;
        }

        internal RentedMemory(BytePool.RentedArray? array, byte[]? byteArray, int start, int length)
        {
            if (byteArray is null)
            {
                return;
            }
            else if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.array = array;
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        #region FieldAndProperty

        private readonly BytePool.RentedArray? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether a tracked owner has outstanding references, including for an unpooled array.
        /// </summary>
        public bool IsRented => this.array != null && this.array.IsRented;

        /// <summary>
        /// Gets a value indicating whether there is no tracked owner or its reference count has reached zero.
        /// </summary>
        public bool IsReturned => this.array == null || this.array.IsReturned;

        /// <summary>
        /// Gets a value indicating whether the memory is empty.
        /// </summary>
        public bool IsEmpty => this.length == 0;

        /// <summary>
        /// Gets the number of bytes in the memory.
        /// </summary>
        public int Length => this.length;

        /// <summary>
        /// Gets the tracked array owner, or <see langword="null"/> for an untracked view.
        /// </summary>
        public RentedArray? Owner => this.array;

        /// <summary>
        /// Gets a <see cref="Span{T}"/> from <see cref="RentedMemory"/>.
        /// </summary>
        public Span<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="Memory{T}"/> from <see cref="RentedMemory"/>.
        /// </summary>
        public Memory<byte> Memory => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a read-only view without acquiring another owned reference.
        /// </summary>
        public RentedReadOnlyMemory ReadOnly => new(this.array, this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        /// Acquires another owned reference, if this view has a tracked owner.
        /// </summary>
        /// <returns>A view sharing the same bytes; return its owned reference separately.</returns>
        public RentedMemory IncrementAndShare()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                if (this.byteArray is null)
                {
                    return default;
                }
                else
                {
                    return new(this.byteArray, this.start, this.length);
                }
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        /// Acquires another owned reference, if this view has a tracked owner.
        /// </summary>
        /// <returns>A view sharing the same bytes; return its owned reference separately.</returns>
        public RentedReadOnlyMemory IncrementAndShareReadOnly()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                if (this.byteArray is null)
                {
                    return default;
                }
                else
                {
                    return new(this.byteArray, this.start, this.length);
                }
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        /// Attempts to acquire another owned reference.
        /// </summary>
        /// <returns><see langword="true"/> if acquired or no owner is tracked;
        /// <see langword="false"/> if the tracked owner has been returned.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                return true;
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Creates a slice from the specified offset without acquiring another owned reference.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentedMemory"/>.</returns>
        public RentedMemory Slice(int start)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)this.length, nameof(start));
            return new(this.array, this.byteArray, this.start + start, this.length - start);
        }

        /// <summary>
        /// Creates a slice of the specified range without acquiring another owned reference.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentedMemory"/>.</returns>
        public RentedMemory Slice(int start, int length)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)this.length, nameof(start));
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)(this.length - start), nameof(length));
            return new(this.array, this.byteArray, this.start + start, length);
        }

        /// <summary>
        /// Releases one owned reference; untracked views require no action.
        /// A pooled array is offered back to its bucket on the final release.
        /// </summary>
        /// <returns>An empty view. The current struct and its copies are unchanged.</returns>
        public RentedMemory Return()
        {
            this.array?.Return();
            return default;
        }

        /// <summary>
        /// Decrements the reference count (the same as <see cref="Return"/>).
        /// </summary>
        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Provides a read-only byte-memory view with optional reference-counted ownership.
    /// </summary>
    /// <remarks>
    /// Copying, slicing, or converting a view does not increment the reference count.
    /// Use <see cref="IncrementAndShare" /> for an additional owner. Return or dispose each owned
    /// reference once; doing so does not clear this struct or its copies.
    /// </remarks>
    public readonly struct RentedReadOnlyMemory : IDisposable
    {
        /// <summary>
        /// An empty <see cref="RentedReadOnlyMemory"/>.
        /// </summary>
        public static readonly RentedReadOnlyMemory Empty = default;

        /// <summary>
        /// Creates a new <see cref="RentedReadOnlyMemory"/> instance from the specified byte array.<br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="source">The source byte array.</param>
        /// <returns>A new <see cref="RentedReadOnlyMemory"/> instance.</returns>
        public static RentedReadOnlyMemory CreateFrom(byte[] source)
        {
            return new RentedArray(source).AsReadOnlyMemory();
        }

        /// <summary>
        /// Creates a new <see cref="RentedReadOnlyMemory"/> instance from the specified byte array.<br/>
        /// The byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="source">The source byte array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <returns>A new <see cref="RentedReadOnlyMemory"/> instance.</returns>
        public static RentedReadOnlyMemory CreateFrom(byte[] source, int start, int length)
        {
            return new RentedArray(source).AsReadOnlyMemory(start, length);
        }

        /// <summary>
        /// Wraps array-backed memory without copying or tracking ownership.
        /// </summary>
        /// <param name="source">The memory to wrap.</param>
        /// <returns>A view over the underlying array, or an empty view if the array cannot be obtained.</returns>
        public static RentedReadOnlyMemory CreateFrom(ReadOnlyMemory<byte> source)
        {
            if (MemoryMarshal.TryGetArray<byte>(source, out var segment) &&
                segment.Array is not null)
            {
                return new(segment.Array, segment.Offset, segment.Count);
            }

            return default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RentedReadOnlyMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be added to a pool when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        internal RentedReadOnlyMemory(byte[] byteArray, int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal RentedReadOnlyMemory(RentedArray array)
        {
            this.array = array;
            this.byteArray = array.Array;
            this.length = array.Array.Length;
        }

        internal RentedReadOnlyMemory(BytePool.RentedArray? array, byte[]? byteArray, int start, int length)
        {
            if (byteArray is null)
            {
                return;
            }
            else if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.array = array;
            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        #region FieldAndProperty

        private readonly BytePool.RentedArray? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether a tracked owner has outstanding references, including for an unpooled array.
        /// </summary>
        public bool IsRented => this.array != null && this.array.IsRented;

        /// <summary>
        /// Gets a value indicating whether there is no tracked owner or its reference count has reached zero.
        /// </summary>
        public bool IsReturned => this.array == null || this.array.IsReturned;

        /// <summary>
        /// Gets a value indicating whether the memory is empty.
        /// </summary>
        public bool IsEmpty => this.length == 0;

        /// <summary>
        /// Gets the number of bytes in the memory.
        /// </summary>
        public int Length => this.length;

        /// <summary>
        /// Gets the tracked array owner, or <see langword="null"/> for an untracked view.
        /// </summary>
        public RentedArray? Owner => this.array;

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> over the memory.
        /// </summary>
        /// <remarks>Use <see cref="UnsafeMemory"/> when a writable view is required.</remarks>
        public ReadOnlySpan<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="ReadOnlyMemory{T}"/> over the memory.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a writable view without acquiring another owned reference.
        /// </summary>
        public RentedMemory UnsafeMemory => new(this.array, this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        /// Acquires another owned reference, if this view has a tracked owner.
        /// </summary>
        /// <returns>A view sharing the same bytes; return its owned reference separately.</returns>
        public RentedReadOnlyMemory IncrementAndShare()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                if (this.byteArray is null)
                {
                    return default;
                }
                else
                {
                    return new(this.byteArray, this.start, this.length);
                }
            }

            return new(this.array.IncrementAndShare(), this.byteArray, this.start, this.length);
        }

        /// <summary>
        /// Attempts to acquire another owned reference.
        /// </summary>
        /// <returns><see langword="true"/> if acquired or no owner is tracked;
        /// <see langword="false"/> if the tracked owner has been returned.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                return true;
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Creates a slice from the specified offset without acquiring another owned reference.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentedReadOnlyMemory"/>.</returns>
        public RentedReadOnlyMemory Slice(int start)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)this.length, nameof(start));
            return new(this.array, this.byteArray, this.start + start, this.length - start);
        }

        /// <summary>
        /// Creates a slice of the specified range without acquiring another owned reference.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentedReadOnlyMemory"/>.</returns>
        public RentedReadOnlyMemory Slice(int start, int length)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)this.length, nameof(start));
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)(this.length - start), nameof(length));
            return new(this.array, this.byteArray, this.start + start, length);
        }

        /// <summary>
        /// Releases one owned reference; untracked views require no action.
        /// A pooled array is offered back to its bucket on the final release.
        /// </summary>
        /// <returns>An empty view. The current struct and its copies are unchanged.</returns>
        public RentedReadOnlyMemory Return()
        {
            this.array?.Return();
            return default;
        }

        /// <summary>
        /// Decrements the reference count (the same as <see cref="Return"/>).
        /// </summary>
        public void Dispose()
            => this.Return();
    }

    internal sealed class Bucket
    {
        public Bucket(int arrayLength, int poolLimit)
        {
            this.ArrayLength = arrayLength;
            this.Queue = new(poolLimit);
        }

        public int ArrayLength { get; }

        public int PoolLimit => this.Queue.Capacity;

#pragma warning disable SA1401 // Fields should be private
        internal CircularQueue<RentedArray> Queue;
#pragma warning restore SA1401 // Fields should be private

        public override string ToString()
            => $"{this.ArrayLength} (?/{this.PoolLimit})";
    }

    private BytePool()
    {
        this.buckets = new Bucket[33];
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BytePool"/> class.<br/>
    /// Smaller array buckets retain more arrays, up to the requested limit.
    /// Bucket limits are rounded up to powers of two, with a minimum of two arrays.
    /// </summary>
    /// <param name="maxArrayLength">The largest pooled array length, rounded up to a power of two.
    /// Nonpositive values use the default maximum.</param>
    /// <param name="poolLimit">The requested retention limit per bucket, before capacity rounding.</param>
    /// <returns>A new instance of the <see cref="BytePool"/> class.</returns>
    public static BytePool CreateExponential(int maxArrayLength = DefaultMaxArrayLength, int poolLimit = DefaultPoolLimit)
    {
        var bytePool = new BytePool();
        if (maxArrayLength <= 0)
        {
            maxArrayLength = DefaultMaxArrayLength;
        }

        var leadingZero = BitOperations.LeadingZeroCount((uint)maxArrayLength - 1);
        var limit = 1;
        for (var i = 0; i <= 32; i++)
        {
            if (i < leadingZero)
            {
                bytePool.buckets[i] = null;
            }
            else
            {
                bytePool.buckets[i] = new(1 << (32 - i), limit);
                limit <<= 1;
                limit = limit > poolLimit ? poolLimit : limit;
            }
        }

        return bytePool;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BytePool"/> class.<br/>
    /// Each bucket uses the requested array count, rounded up to a power of two with a minimum of two.
    /// </summary>
    /// <param name="maxArrayLength">The largest pooled array length, rounded up to a power of two.
    /// Nonpositive values use the default maximum.</param>
    /// <param name="poolLimit">The requested retention limit per bucket, before capacity rounding.</param>
    /// <returns>A new instance of the <see cref="BytePool"/> class.</returns>
    public static BytePool CreateFlat(int maxArrayLength = DefaultMaxArrayLength, int poolLimit = DefaultPoolLimit)
    {
        var bytePool = new BytePool();
        if (maxArrayLength <= 0)
        {
            maxArrayLength = DefaultMaxArrayLength;
        }

        var leadingZero = BitOperations.LeadingZeroCount((uint)maxArrayLength - 1);
        for (var i = 0; i <= 32; i++)
        {
            if (i < leadingZero)
            {
                bytePool.buckets[i] = null;
            }
            else
            {
                bytePool.buckets[i] = new(1 << (32 - i), poolLimit);
            }
        }

        return bytePool;
    }

    #region FieldAndProperty

    private Bucket?[] buckets;

    #endregion

    /// <summary>
    /// Sets the pool limit of the bucket that serves the specified array length,
    /// creating the bucket if it does not exist yet.
    /// </summary>
    /// <param name="arrayLength">The array length identifying the bucket. Values below 1 are ignored.</param>
    /// <param name="poolLimit">The requested retained array count, rounded up to a power of two with a minimum of two.</param>
    /// <remarks>Any arrays already pooled in the bucket are discarded. This is not thread-safe;
    /// call it during setup, before the pool is shared.</remarks>
    public void SetPoolLimit(int arrayLength, int poolLimit)
    {
        if (arrayLength < 1)
        {
            return;
        }

        var i = BitOperations.LeadingZeroCount((uint)arrayLength - 1);
        if (this.buckets[i] is null)
        {
            this.buckets[i] = new(1 << (32 - i), poolLimit);
        }
        else
        {
            this.buckets[i]!.Queue = new(poolLimit);
        }
    }

    /// <summary>
    /// Rents an array of at least the requested length, allocating one if necessary.
    /// </summary>
    /// <param name="minimumLength">The minimum length of the byte array.</param>
    /// <returns>A rented <see cref="RentedArray"/>. When no bucket serves the requested length,
    /// a plain byte array is allocated and is not returned to the pool.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RentedArray Rent(int minimumLength)
    {
        var bucket = this.buckets[BitOperations.LeadingZeroCount((uint)minimumLength - 1)];
        if (bucket == null)
        {// Since the bucket is empty, allocate and return the byte array using the conventional method.
            return new RentedArray(new byte[minimumLength]);
        }

        if (!bucket.Queue.TryDequeue(out var array))
        {// Allocate a new byte array.
            return new RentedArray(bucket);
        }

        // Rent a byte array from the pool.
        array.ResetCount();
        return array;
    }

    /// <summary>
    /// Calculates the maximum number of bytes the pool can retain when every bucket is full.
    /// </summary>
    /// <returns>The upper bound of the pooled memory, in bytes.</returns>
    public long CalculateMaxMemoryUsage()
    {
        var usage = 0L;
        foreach (var x in this.buckets)
        {
            if (x is not null)
            {
                usage += (long)x.ArrayLength * (long)x.PoolLimit;
            }
        }

        return usage;
    }
}
