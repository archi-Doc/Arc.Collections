// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable SA1124
#pragma warning disable SA1202

namespace Arc.Collections;

/// <summary>
/// A fast thread-safe pool of byte arrays (uses <see cref="CircularQueue{T}"/>).<br/>
/// <see cref="BytePool"/> is faster than <see cref="System.Buffers.ArrayPool{T}"/> and only slightly slower than creating a new byte array (particularly for arrays of 256 bytes or less).<br/>
/// However, it offers several advantages.<br/>
/// 1. <see cref="BytePool"/> can handle a rent byte array and a created ('new byte[]') byte array in the same way.<br/>
/// 2. <see cref="BytePool"/> can handle a rent byte array in the same way as <see cref="Memory{T}"/> by using <see cref="BytePool.RentMemory"/>.<br/>
/// 3. <see cref="BytePool"/> can be used by multiple users by incrementing the reference count.<br/>
/// ! It is recommended to use <see cref="BytePool"/> within a class, and not between classes, as the responsibility for returning the byte array becomes unclear.
/// </summary>
public class BytePool
{
    public const int SingleCount = int.MaxValue;
    private const int DefaultMaxArrayLength = 1024 * 1024 * 16; // 16 MB
    private const int DefaultPoolLimit = 256;
    private const int StandardArrayLength = 1024 * 32; // 32 KB (TinyhandSerializer.InitialBufferSize, ByteSequence.DefaultVaultSize)
    private const int StandardPoolLimit = 1024;

    public static readonly BytePool Default;

    static BytePool()
    {
        Default = BytePool.CreateExponential();
        Default.SetPoolLimit(StandardArrayLength, StandardPoolLimit);
    }

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
            this.ResetCount();
        }

        internal RentArray(Bucket bucket)
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
        /// Gets a rent byte array.
        /// </summary>
        public byte[] Array => this.byteArray;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => Volatile.Read(ref this.count) > 0;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
        /// </summary>
        public bool IsReturned => Volatile.Read(ref this.count) <= 0;

        /// <summary>
        /// Gets the reference count of the owner.
        /// </summary>
        public int Count
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
        ///  Increment the reference count and get an <see cref="RentArray"/> instance.
        /// </summary>
        /// <returns><see cref="RentArray"/> instance (<see langword="this"/>).</returns>
        public RentArray IncrementAndShare()
        {
            if (this.count == SingleCount)
            {
                this.count = 2;
                return this;
            }
            else if (this.count <= 0)
            {
                throw new InvalidOperationException("The reference counter cannot be less than or equal to 0.");
            }

            Interlocked.Increment(ref this.count);
            return this;
        }

        /// <summary>
        ///  Increment the counter and attempt to share the byte array.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            int currentCount;
            int newCount;
            do
            {
                currentCount = this.count;
                if (this.count <= 0)
                {
                    return false;
                }

                if (this.count == SingleCount)
                {
                    newCount = 2;
                }
                else
                {
                    newCount = currentCount + 1;
                }
            }
            while (Interlocked.CompareExchange(ref this.count, newCount, currentCount) != currentCount);

            return true;
        }

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the byte array to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="null"></see>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentArray? Return()
        {
            if (this.count == SingleCount)
            {
                this.count = 0;
                this.bucket?.Queue.TryEnqueue(this);
                return null;
            }
            else if (this.count <= 0)
            {
                throw new InvalidOperationException("The reference counter cannot be less than or equal to 0.");
            }

            if (Interlocked.Decrement(ref this.count) <= 0 && this.bucket != null)
            {
                this.bucket.Queue.TryEnqueue(this);
            }

            return null;
        }

        /// <summary>
        /// Create a <see cref="RentMemory"/> object from <see cref="RentArray"/>.
        /// </summary>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentMemory AsMemory()
            => new(this);

        /// <summary>
        /// Create a <see cref="RentMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentMemory AsMemory(int start)
            => new(this, this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Create a <see cref="RentMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentMemory AsMemory(int start, int length)
            => new(this, this.byteArray, start, length);

        /// <summary>
        /// Create a <see cref="RentReadOnlyMemory"/> object from <see cref="RentArray"/>.
        /// </summary>
        /// <returns><see cref="RentReadOnlyMemory"/>.</returns>
        public RentReadOnlyMemory AsReadOnly()
            => new(this);

        /// <summary>
        /// Create a <see cref="RentMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentReadOnlyMemory AsReadOnly(int start)
            => new(this, this.byteArray, start, this.byteArray.Length - start);

        /// <summary>
        /// Create a <see cref="RentReadOnlyMemory"/> object by specifying the index and length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentReadOnlyMemory"/>.</returns>
        public RentReadOnlyMemory AsReadOnly(int start, int length)
            => new(this, this.byteArray, start, length);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetCount()
            => this.count = SingleCount;

        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Represents an owner of a byte array and a <see cref="Memory{T}"/> object.
    /// </summary>
    public readonly struct RentMemory : IDisposable
    {
        public static readonly RentMemory Empty = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="RentMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        public RentMemory(byte[] byteArray)
        {
            this.byteArray = byteArray;
            this.length = byteArray.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RentMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        public RentMemory(byte[] byteArray, int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal RentMemory(RentArray array)
        {
            this.array = array;
            this.byteArray = array.Array;
            this.length = array.Array.Length;
        }

        internal RentMemory(BytePool.RentArray? array, byte[]? byteArray, int start, int length)
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

        private readonly BytePool.RentArray? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => this.array != null && this.array.IsRent;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
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
        /// Gets a <see cref="RentArray"/> from <see cref="RentMemory"/>.
        /// </summary>
        public RentArray? RentArray => this.array;

        /// <summary>
        /// Gets a <see cref="Span{T}"/> from <see cref="RentMemory"/>.
        /// </summary>
        public Span<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="Memory{T}"/> from <see cref="RentMemory"/>.
        /// </summary>
        public Memory<byte> Memory => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="RentReadOnlyMemory"/> from <see cref="RentMemory"/>.
        /// </summary>
        public RentReadOnlyMemory ReadOnly => new(this.array, this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="RentArray"/> instance (<see langword="this"/>).</returns>
        public RentMemory IncrementAndShare()
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
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="RentArray"/> instance (<see langword="this"/>).</returns>
        public RentReadOnlyMemory IncrementAndShareReadOnly()
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
        ///  Increment the counter and attempt to share the <see cref="RentArray"/>.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                return true;
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentMemory Slice(int start)
            => new(this.array, this.byteArray, this.start + start, this.length - start);

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentMemory"/>.</returns>
        public RentMemory Slice(int start, int length)
            => new(this.array, this.byteArray, this.start + start, length);

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the <see cref="RentArray"/> to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="default"></see>.</returns>
        public RentMemory Return()
        {
            this.array?.Return();
            return default;
        }

        public void Dispose()
            => this.Return();
    }

    /// <summary>
    /// Represents an owner of a byte array and a <see cref="Memory{T}"/> object.
    /// </summary>
    public readonly struct RentReadOnlyMemory : IDisposable
    {
        public static readonly RentReadOnlyMemory Empty = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="RentReadOnlyMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        public RentReadOnlyMemory(byte[] byteArray)
        {
            this.byteArray = byteArray;
            this.length = byteArray.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RentReadOnlyMemory"/> struct from a byte array.<br/>
        /// This is a feature for compatibility with <see cref="BytePool"/>, and the byte array will not be returned when <see cref="Return"/> is called.
        /// </summary>
        /// <param name="byteArray">A byte array (other than <see cref="BytePool"/>).</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        public RentReadOnlyMemory(byte[] byteArray, int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)byteArray.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.byteArray = byteArray;
            this.start = start;
            this.length = length;
        }

        internal RentReadOnlyMemory(RentArray array)
        {
            this.array = array;
            this.byteArray = array.Array;
            this.length = array.Array.Length;
        }

        internal RentReadOnlyMemory(BytePool.RentArray? array, byte[]? byteArray, int start, int length)
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

        private readonly BytePool.RentArray? array;
        private readonly byte[]? byteArray;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is rent or not.
        /// </summary>
        public bool IsRent => this.array != null && this.array.IsRent;

        /// <summary>
        /// Gets a value indicating whether the owner (byte array) is returned or not.
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
        /// Gets a <see cref="RentArray"/> from <see cref="RentMemory"/>.
        /// </summary>
        public RentArray? RentArray => this.array;

        /// <summary>
        /// Gets a <see cref="Span{T}"/> from <see cref="RentReadOnlyMemory"/>.
        /// </summary>
        public Span<byte> Span => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a span from <see cref="RentReadOnlyMemory"/>.
        /// </summary>
        public ReadOnlyMemory<byte> Memory => new(this.byteArray, this.start, this.length);

        /// <summary>
        /// Gets a <see cref="RentMemory"/> from <see cref="RentReadOnlyMemory"/>.
        /// </summary>
        public RentMemory UnsafeMemory => new(this.array, this.byteArray, this.start, this.length);

        #endregion

        /// <summary>
        ///  Increment the reference count.
        /// </summary>
        /// <returns><see cref="RentArray"/> instance (<see langword="this"/>).</returns>
        public RentReadOnlyMemory IncrementAndShare()
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
        ///  Increment the counter and attempt to share the <see cref="RentArray"/>.
        /// </summary>
        /// <returns><see langword="true"/>; Success.</returns>
        public bool TryIncrement()
        {
            if (this.array == null)
            {// Since the data is an ordinary byte array, Increment/Return operations will not be performed.
                return true;
            }

            return this.array.TryIncrement();
        }

        /// <summary>
        /// Forms a slice out of the current memory that begins at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns><see cref="RentReadOnlyMemory"/>.</returns>
        public RentReadOnlyMemory Slice(int start)
        {
            return new(this.array, this.byteArray, this.start + start, this.length - start);
        }

        /// <summary>
        /// Forms a slice out of the current memory starting at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of elements to include in the slice.</param>
        /// <returns><see cref="RentReadOnlyMemory"/>.</returns>
        public RentReadOnlyMemory Slice(int start, int length)
        {
            return new(this.array, this.byteArray, this.start + start, length);
        }

        /// <summary>
        /// Decrement the reference count.<br/>
        /// When it reaches zero, it returns the <see cref="RentArray"/> to the pool.<br/>
        /// Failure to return a rented array is not a fatal error (eventually be garbage-collected).
        /// </summary>
        /// <returns><see langword="default"></see>.</returns>
        public RentReadOnlyMemory Return()
        {
            this.array?.Return();
            return default;
        }

        public void Dispose()
            => this.Return();
    }

    internal sealed class Bucket
    {
        public Bucket(BytePool bytePool, int arrayLength, int poolLimit)
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

        private BytePool bytePool;

        public override string ToString()
            => $"{this.ArrayLength} (?/{this.PoolLimit})";
    }

    private BytePool()
    {
        this.buckets = new Bucket[33];
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BytePool"/> class.<br/>
    /// The pool limit starts with one array of <paramref name="maxArrayLength"/> and doubles each time it halves until it reaches <paramref name="poolLimit"/>.
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
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
                bytePool.buckets[i] = new(bytePool, 1 << (32 - i), limit);
                limit <<= 1;
                limit = limit > poolLimit ? poolLimit : limit;
            }
        }

        return bytePool;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="BytePool"/> class.<br/>
    /// The pool limit is set to the specified number of byte arrays, each with a length of <paramref name="maxArrayLength"/> or less.
    /// </summary>
    /// <param name="maxArrayLength">The maximum length of a byte array instance that may be stored in the pool.</param>
    /// <param name="poolLimit">The maximum number of array instances that may be stored in each bucket in the pool.</param>
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
                bytePool.buckets[i] = new(bytePool, 1 << (32 - i), poolLimit);
            }
        }

        return bytePool;
    }

    #region FieldAndProperty

    private Bucket?[] buckets;

    #endregion

    public void SetPoolLimit(int arrayLength, int poolLimit)
    {
        if (arrayLength < 1)
        {
            return;
        }

        var i = BitOperations.LeadingZeroCount((uint)arrayLength - 1);
        if (this.buckets[i] is null)
        {
            this.buckets[i] = new(this, 1 << (32 - i), poolLimit);
        }
        else
        {
            this.buckets[i]!.Queue = new(poolLimit);
        }
    }

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
        array.ResetCount();
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
