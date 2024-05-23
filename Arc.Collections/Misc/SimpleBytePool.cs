// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable SA1124
#pragma warning disable SA1202

namespace Arc.Collections;

/// <summary>
/// A thread-safe pool of byte arrays (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// <see cref="BytePool"/> is slightly slower than 'new byte[]' or <see cref="System.Buffers.ArrayPool{T}"/> (especially byte arrays of 1kbytes or less), but it has some advantages.<br/>
/// 1. Can handle a rent byte array and a created ('new byte[]') byte array in the same way.<br/>
/// 2. By using <see cref="BytePool.RentMemory"/>, you can handle a rent byte array in the same way as <see cref="Memory{T}"/>.<br/>
/// 3. Can be used by multiple users by incrementing the reference count.<br/>
/// ! It is recommended to use <see cref="BytePool"/> within a class, and not between classes, as the responsibility for returning the buffer becomes unclear.
/// </summary>
public class SimpleBytePool
{
    private const int DefaultMaxArrayLength = 1024 * 1024 * 16; // 16MB
    private const int DefaultPoolLimit = 100;

    public static readonly SimpleBytePool Default = SimpleBytePool.Create();

    /// <summary>
    /// Represents an owner of a byte array (one owner instance for each byte array).<br/>
    /// <see cref="RentArray"/> has a reference count, and when it reaches zero, it returns the byte array to the pool.
    /// </summary>
    public class RentArray : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RentArray"/> class from a byte array.<br/>
        /// This is a feature for compatibility with conventional memory management (e.g new byte[]), <br/>
        /// The byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="array">A byte array (allocated with 'new').</param>
        internal RentArray(byte[] array)
        {
            this.bucket = null;
            this.byteArray = array;
        }

        internal RentArray(Bucket bucket)
        {
            this.bucket = bucket;
            this.byteArray = new byte[bucket.ArrayLength];
        }

        #region FieldAndProperty

        private readonly byte[] byteArray;
        private readonly Bucket? bucket;

        /// <summary>
        /// Gets a rent byte array.
        /// </summary>
        public byte[] Array => this.byteArray;

        #endregion

        /// <summary>
        /// Create a <see cref="Span{T}"/> object from <see cref="RentArray"/>.
        /// </summary>
        /// <returns><see cref="Span{T}"/>.</returns>
        public Span<byte> AsSpan()
            => new(this.byteArray);

        /// <summary>
        /// Create a <see cref="Span{T}"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="Span{T}"/>.</returns>
        public Span<byte> AsSpan(int start)
            => new(this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Create a <see cref="Span{T}"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="Span{T}"/>.</returns>
        public Span<byte> AsSpan(int start, int length)
            => new(this.byteArray, start, length);

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the byte array to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="null"></see>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentArray? Return()
        {
            if (this.bucket != null)
            {
                this.bucket.Queue.TryEnqueue(this);
            }

            return null;
        }

        public void Dispose()
            => this.Return();
    }

    internal sealed class Bucket
    {
        public Bucket(SimpleBytePool bytePool, int arrayLength, int poolLimit)
        {
            this.bytePool = bytePool;
            this.ArrayLength = arrayLength;
            this.Queue = new(poolLimit);
        }

        public int ArrayLength { get; }

        public int PoolLimit => this.Queue.Capacity;

#pragma warning disable SA1401 // Fields should be private
        internal CircularQueue<RentArray> Queue;
#pragma warning restore SA1401 // Fields should be private

        private SimpleBytePool bytePool;

        public override string ToString()
            => $"{this.ArrayLength} (?/{this.PoolLimit})";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleBytePool"/> class.<br/>
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    private SimpleBytePool(int maxArrayLength, int poolLimit)
    {
        if (maxArrayLength <= 0)
        {
            maxArrayLength = DefaultMaxArrayLength;
        }

        var leadingZero = BitOperations.LeadingZeroCount((uint)maxArrayLength - 1);
        this.buckets = new Bucket[33];
        var limit = 1;
        for (var i = 0; i <= 32; i++)
        {
            if (i < leadingZero)
            {
                this.buckets[i] = null;
            }
            else
            {
                this.buckets[i] = new(this, 1 << (32 - i), limit);
                limit <<= 1;
                limit = limit > poolLimit ? poolLimit : limit;
            }
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BytePool"/> class.<br/>
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
    /// <returns>A new instance of the <see cref="BytePool"/> class.</returns>
    public static SimpleBytePool Create(int maxArrayLength = DefaultMaxArrayLength, int poolLimit = DefaultPoolLimit)
        => new(maxArrayLength, poolLimit);

    #region FieldAndProperty

    private Bucket?[] buckets;

    #endregion

    /// <summary>
    /// Gets a <see cref="RentArray"/> from the pool or allocate a new byte array if not available.<br/>
    /// </summary>
    /// <param name="minimumLength">The minimum length of the byte array.</param>
    /// <returns>A rent <see cref="RentArray"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RentArray Rent(int minimumLength)
    {
        var bucket = this.buckets[BitOperations.LeadingZeroCount((uint)minimumLength - 1)];
        if (bucket == null)
        {// Since the bucket is empty, allocate and return the byte array using the conventional method.
            return new RentArray(new byte[minimumLength]);
        }

        if (!bucket.Queue.TryDequeue(out var array))
        {// Allocate a new byte array.
            return new RentArray(bucket);
        }

        // Rent a byte array from the pool.
        return array;
    }

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
