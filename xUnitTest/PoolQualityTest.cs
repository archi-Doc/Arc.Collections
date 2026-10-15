// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class PoolQualityTest
{
    [Fact]
    public void CacheTakeAndAddCycleDoesNotAllocateOnceWarm()
    {
        using var cache = new KeyedObjectCache<int, object>(8);
        var objects = Enumerable.Range(0, 8).Select(_ => new object()).ToArray();
        for (var i = 0; i < objects.Length; i++)
        {
            Assert.True(cache.TryAdd(i, objects[i]));
        }

        static void Cycle(KeyedObjectCache<int, object> cache, int rounds)
        {
            for (var i = 0; i < rounds; i++)
            {
                var key = i & 7;
                var obj = cache.TakeOrDefault(key)!;
                cache.TryAdd(key, obj);
            }
        }

        var allocated = AllocationHelper.Measure(() => Cycle(cache, 1_000));

        Assert.Equal(8, cache.Count);
        for (var i = 0; i < objects.Length; i++)
        {
            Assert.Same(objects[i], cache.TakeOrDefault(i));
        }

        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void CacheEvictsOldestRemainingEntryAfterRemovalsAnywhereInTheOrder()
    {
        var disposed = new List<int>();
        using var cache = new KeyedObjectCache<int, Tracked>(4);
        for (var i = 0; i < 4; i++)
        {
            Assert.True(cache.TryAdd(i, new Tracked(i, disposed)));
        }

        // Remove the middle, newest and oldest entries, then refill.
        Assert.Equal(2, cache.TakeOrDefault(2)!.Id);
        Assert.Equal(3, cache.TakeOrDefault(3)!.Id);
        Assert.Equal(0, cache.TakeOrDefault(0)!.Id);
        Assert.Equal(1, cache.Count);
        for (var i = 4; i < 7; i++)
        {
            Assert.True(cache.TryAdd(i, new Tracked(i, disposed)));
        }

        Assert.Empty(disposed);
        Assert.Equal(4, cache.Count);

        // Full: each addition evicts the oldest remaining entry in insertion order.
        Assert.True(cache.TryAdd(7, new Tracked(7, disposed)));
        Assert.True(cache.TryAdd(8, new Tracked(8, disposed)));
        Assert.Equal(new[] { 1, 4 }, disposed);
        Assert.Null(cache.TakeOrDefault(1));
        Assert.Null(cache.TakeOrDefault(4));

        cache.Dispose();
        Assert.Equal(new[] { 1, 4, 5, 6, 7, 8 }, disposed);
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryAdd(9, new Tracked(9, disposed)));
    }

    [Fact]
    public void CacheSupportsNullKeysAndSingleEntryCapacity()
    {
        using var cache = new KeyedObjectCache<string, string>(1);
        Assert.True(cache.TryAdd(null!, "null"));
        Assert.False(cache.TryAdd(null!, "duplicate"));
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryAdd("a", "a"));
        Assert.Null(cache.TakeOrDefault(null!));
        Assert.Equal("a", cache.TakeOrDefault("a"));
        Assert.Equal(0, cache.Count);
        Assert.True(cache.TryAdd(null!, "again"));
        Assert.Equal("again", cache.TakeOrDefault(null!));
    }

    [Fact]
    public void CacheDisposalCallbackCanTakeOtherEntries()
    {
        var disposed = new List<int>();
        var cache = new KeyedObjectCache<int, Tracked>(4);
        Tracked? taken = null;
        var first = new Tracked(0, disposed, () => taken = cache.TakeOrDefault(2));
        Assert.True(cache.TryAdd(0, first));
        Assert.True(cache.TryAdd(1, new Tracked(1, disposed)));
        Assert.True(cache.TryAdd(2, new Tracked(2, disposed)));
        Assert.True(cache.TryAdd(3, new Tracked(3, disposed)));

        cache.Dispose();

        Assert.Equal(2, taken!.Id);
        Assert.Equal(new[] { 0, 1, 3 }, disposed);
        Assert.Equal(0, cache.Count);
        Assert.Null(cache.TakeOrDefault(1));
    }

    [Fact]
    public void CacheKeepsEveryObjectAccountedForUnderConcurrency()
    {
        const int Threads = 4;
        const int PerThread = 2_000;
        var disposedCount = 0;
        var takenCount = 0;
        var rejectedCount = 0;
        var cache = new KeyedObjectCache<int, Counted>(16);

        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, thread =>
        {
            for (var i = 0; i < PerThread; i++)
            {
                var key = (thread * 7 + i) % 24;
                if ((i & 1) == 0)
                {
                    if (!cache.TryAdd(key, new Counted(() => Interlocked.Increment(ref disposedCount))))
                    {
                        Interlocked.Increment(ref rejectedCount);
                    }
                }
                else if (cache.TakeOrDefault(key) is not null)
                {
                    Interlocked.Increment(ref takenCount);
                }
            }
        });

        var cached = cache.Count;
        Assert.InRange(cached, 0, cache.CacheSize);
        var added = (Threads * PerThread / 2) - rejectedCount;
        Assert.Equal(added, takenCount + disposedCount + cached);

        cache.Dispose();
        Assert.Equal(added, takenCount + disposedCount);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void ObjectPoolReportsRoundedPoolSize()
    {
        Assert.Equal(ObjectPool<object>.DefaultPoolSize, new ObjectPool<object>(() => new object()).PoolSize);
        Assert.Equal(4, new ObjectPool<object>(() => new object(), 3).PoolSize);
        Assert.Equal(2, new ObjectPool<object>(() => new object(), 0).PoolSize);
        Assert.Equal(2, new ObjectPool<object>(() => new object(), -5).PoolSize);
    }

    [Fact]
    public void ObjectPoolDropsNonDisposableOverflowAndCallsFactoryWhenEmpty()
    {
        var created = 0;
        object Create()
        {
            created++;
            return new object();
        }

        var pool = new ObjectPool<object>(Create, 2);
        var items = Enumerable.Range(0, 3).Select(_ => pool.Rent()).ToArray();
        Assert.Equal(3, created);
        foreach (var x in items)
        {
            pool.Return(x);
        }

        var reused = new[] { pool.Rent(), pool.Rent() };
        Assert.Equal(new[] { items[0], items[1] }, reused);
        Assert.Equal(3, created);
        Assert.NotSame(items[2], pool.Rent());
        Assert.Equal(4, created);
        pool.Dispose();
    }

    [Fact]
    public void BytePoolReusesArraysOnlyAfterTheFinalReturn()
    {
        var pool = BytePool.CreateFlat(1024, 2);
        var owner = pool.Rent(100);
        Assert.Equal(128, owner.Array.Length);
        Assert.Same(owner.IncrementAndShare(), owner);
        owner.Return();
        Assert.True(owner.IsRented);

        // Still owned: a new rent cannot receive the same array.
        var other = pool.Rent(128);
        Assert.NotSame(owner, other);
        Assert.NotSame(owner.Array, other.Array);
        Assert.Null(other.Return());

        Assert.Null(owner.Return());
        Assert.True(owner.IsReturned);
        Assert.Equal(0, owner.ReferenceCount);
        var memory = owner.AsMemory();
        Assert.False(memory.IsRented);
        Assert.True(memory.IsReturned);
        Assert.False(memory.ReadOnly.IsRented);
        Assert.True(memory.ReadOnly.IsReturned);
        Assert.False(memory.TryIncrement());
        Assert.False(memory.ReadOnly.TryIncrement());
        Assert.Throws<InvalidOperationException>(() => memory.IncrementAndShare());
        Assert.Throws<InvalidOperationException>(() => memory.IncrementAndShareReadOnly());
        Assert.Throws<InvalidOperationException>(() => memory.ReadOnly.IncrementAndShare());
        Assert.Throws<InvalidOperationException>(() => memory.Return());
        Assert.Throws<InvalidOperationException>(() => memory.ReadOnly.Dispose());
        Assert.Throws<InvalidOperationException>(() => owner.Dispose());

        // The returned pair is reused (first in, first out) and starts with one owner again.
        var reused = pool.Rent(65);
        Assert.Same(other, reused);
        Assert.Same(owner, pool.Rent(128));
        Assert.Equal(1, owner.ReferenceCount);
        Assert.True(memory.IsRented);
        owner.Dispose();
        reused.Dispose();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(1024, 1024)]
    [InlineData(1025, 1025)]
    public void BytePoolRentsPowerOfTwoArraysInsideConfiguredBuckets(int minimumLength, int expectedLength)
    {
        var pool = BytePool.CreateExponential(1024, 4);
        var first = pool.Rent(minimumLength);
        Assert.Equal(expectedLength, first.Array.Length);
        first.Return();
        var second = pool.Rent(minimumLength);
        var pooled = minimumLength is > 0 and <= 1024;
        Assert.Equal(pooled, ReferenceEquals(first, second));
        second.Return();
    }

    [Fact]
    public void BytePoolFactoriesUseDefaultMaximumForNonPositiveLengths()
    {
        var exponential = BytePool.CreateExponential().CalculateMaxMemoryUsage();
        Assert.Equal(exponential, BytePool.CreateExponential(0).CalculateMaxMemoryUsage());
        Assert.Equal(exponential, BytePool.CreateExponential(-1).CalculateMaxMemoryUsage());

        var flat = BytePool.CreateFlat().CalculateMaxMemoryUsage();
        Assert.Equal(flat, BytePool.CreateFlat(0).CalculateMaxMemoryUsage());
        Assert.Equal(flat, BytePool.CreateFlat(int.MinValue).CalculateMaxMemoryUsage());
        Assert.Equal(256L * ((16L << 20) * 2 - 1), flat);
    }

    [Fact]
    public void ExponentialBucketsDoubleRetentionTowardSmallerArrays()
    {
        var pool = BytePool.CreateExponential(1024, 6);
        Assert.Equal(
            new[]
            {
                "1024 (?/2)", "512 (?/2)", "256 (?/4)", "128 (?/8)", "64 (?/8)", "32 (?/8)",
                "16 (?/8)", "8 (?/8)", "4 (?/8)", "2 (?/8)", "1 (?/8)",
            },
            DescribeBuckets(pool));

        // The same layout through public behavior: the retention bound, and the largest bucket keeping two arrays.
        Assert.Equal((1024L * 2) + (512 * 2) + (256 * 4) + (8L * (128 + 64 + 32 + 16 + 8 + 4 + 2 + 1)), pool.CalculateMaxMemoryUsage());
        var first = Enumerable.Range(0, 3).Select(_ => pool.Rent(1024)).ToArray();
        Assert.Equal(3, first.Distinct().Count());
        foreach (var x in first)
        {
            x.Return();
        }

        var second = Enumerable.Range(0, 3).Select(_ => pool.Rent(1024)).ToArray();
        Assert.Equal(2, second.Count(x => first.Contains(x)));

        Assert.Equal(new[] { "4 (?/2)", "2 (?/2)", "1 (?/2)" }, DescribeBuckets(BytePool.CreateFlat(3, 1)));

        // The default pool is the default exponential layout with a larger 32 KiB bucket.
        var defaults = DescribeBuckets(BytePool.Default);
        Assert.Equal(25, defaults.Length);
        Assert.Equal("16777216 (?/2)", defaults[0]);
        Assert.Equal("65536 (?/256)", defaults[8]);
        Assert.Equal("32768 (?/1024)", defaults[9]);
        Assert.Equal("16384 (?/256)", defaults[10]);
        Assert.Equal(
            BytePool.CreateExponential().CalculateMaxMemoryUsage() + (32768L * (1024 - 256)),
            BytePool.Default.CalculateMaxMemoryUsage());

        // 2 x 16 MiB, eight 16 MiB tiers (2 x 8 MiB down to 256 x 64 KiB), 1024 x 32 KiB,
        // and 256 arrays of each smaller length: about 200 MiB.
        Assert.Equal((32L << 20) + (8L * (16 << 20)) + (32L << 20) + (256L * ((16 << 10) * 2 - 1)), BytePool.Default.CalculateMaxMemoryUsage());
    }

    [Fact]
    public void SetPoolLimitCreatesMissingBucketAndReplacesExistingQueue()
    {
        var pool = BytePool.CreateFlat(1024, 2);
        var usage = pool.CalculateMaxMemoryUsage();
        var large = pool.Rent(4000);
        Assert.Equal(4000, large.Array.Length);
        large.Return();

        // Only the 4096-byte bucket is added; 2048-byte requests remain unpooled.
        pool.SetPoolLimit(4000, 3);
        Assert.Equal(usage + (4096L * 4), pool.CalculateMaxMemoryUsage());
        var pooled = pool.Rent(4000);
        Assert.Equal(4096, pooled.Array.Length);
        pooled.Return();
        Assert.Same(pooled, pool.Rent(2049));
        Assert.Equal(2048, pool.Rent(2048).Array.Length);

        // Replacing a queue discards the arrays it retained.
        var small = pool.Rent(10);
        small.Return();
        pool.SetPoolLimit(16, 8);
        Assert.Equal(usage + (4096L * 4) + (16L * 6), pool.CalculateMaxMemoryUsage());
        Assert.NotSame(small, pool.Rent(10));

        pool.SetPoolLimit(0, 8);
        pool.SetPoolLimit(-1, 8);
        Assert.Equal(usage + (4096L * 4) + (16L * 6), pool.CalculateMaxMemoryUsage());
    }

    [Fact]
    public void BytePoolHandsOutEachArrayToOneOwnerAtATimeUnderConcurrency()
    {
        const int Threads = 4;
        var pool = BytePool.CreateFlat(64, 2);
        var failures = 0;
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, thread =>
        {
            var marker = (byte)(thread + 1);
            for (var i = 0; i < 5_000; i++)
            {
                var owner = pool.Rent(64);
                var shared = owner.AsMemory(8, 16).IncrementAndShare();
                owner.Array.AsSpan().Fill(marker);
                Thread.SpinWait(i & 15);
                if (owner.Array.AsSpan().IndexOfAnyExcept(marker) >= 0 || owner.ReferenceCount != 2)
                {
                    Interlocked.Increment(ref failures);
                }

                owner.Return();
                shared.Return();
            }
        });

        Assert.Equal(0, failures);
    }

    [Fact]
    public void NonArrayMemoryProducesEmptyUntrackedViews()
    {
        using var manager = new NativeLikeMemoryManager(4);
        var memory = BytePool.RentedMemory.CreateFrom(manager.Memory);
        var readOnly = BytePool.RentedReadOnlyMemory.CreateFrom(manager.Memory);
        Assert.True(memory.IsEmpty);
        Assert.True(readOnly.IsEmpty);
        Assert.Null(memory.Owner);
        Assert.Null(readOnly.Owner);
        Assert.Equal(0, memory.Memory.Length);
        Assert.Equal(0, readOnly.Memory.Length);

        // Empty memory of any kind yields an empty view.
        Assert.True(BytePool.RentedMemory.CreateFrom(manager.Memory.Slice(0, 0)).IsEmpty);
        Assert.True(BytePool.RentedReadOnlyMemory.CreateFrom(manager.Memory.Slice(0, 0)).IsEmpty);
    }

    [Fact]
    public void UntrackedViewSharingReturnsEquivalentViews()
    {
        var array = new byte[] { 1, 2, 3, 4, 5, 6 };
        var memory = BytePool.RentedMemory.CreateFrom(array.AsMemory(1, 4));
        var shared = memory.IncrementAndShare();
        var sharedReadOnly = memory.IncrementAndShareReadOnly();
        var readOnlyShared = memory.ReadOnly.IncrementAndShare();
        Assert.Null(shared.Owner);
        Assert.Null(sharedReadOnly.Owner);
        Assert.Null(readOnlyShared.Owner);
        Assert.True(shared.Memory.Equals(memory.Memory));
        Assert.True(sharedReadOnly.Memory.Equals(memory.ReadOnly.Memory));
        Assert.True(readOnlyShared.Memory.Equals(memory.ReadOnly.Memory));
        Assert.Equal(new byte[] { 3, 4, 5 }, memory.Slice(1).Span.ToArray());
        Assert.Equal(new byte[] { 4, 5 }, sharedReadOnly.Slice(2).Span.ToArray());
        Assert.True(memory.Slice(4).IsEmpty);
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(5));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(17, 0)]
    [InlineData(0, 17)]
    [InlineData(8, 9)]
    [InlineData(int.MaxValue, 1)]
    public void RentedArrayViewsValidateRanges(int start, int length)
    {
        using var owner = BytePool.RentedArray.CreateFrom(new byte[16]);
        if (length == 0)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => owner.AsMemory(start));
            Assert.Throws<ArgumentOutOfRangeException>(() => owner.AsReadOnlyMemory(start));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => owner.AsMemory(start, length));
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.AsReadOnlyMemory(start, length));
        Assert.Equal(1, owner.ReferenceCount);
    }

    [Fact]
    public void SpanOwnerRejectsNegativeLengthsAndHandlesEmptyBuffers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<byte> scratch = stackalloc byte[4];
            using var owner = new SpanOwner<byte>(scratch, -1);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var owner = new SpanOwner<byte>(-1);
        });

        using (var empty = new SpanOwner<byte>(Span<byte>.Empty, 0))
        {
            Assert.True(empty.Span.IsEmpty);
        }

        using (var rentedEmpty = new SpanOwner<byte>(0))
        {
            Assert.True(rentedEmpty.Span.IsEmpty);
        }

        var defaultOwner = default(SpanOwner<string>);
        Assert.True(defaultOwner.Span.IsEmpty);
        defaultOwner.Dispose();
    }

    [Fact]
    public void SpanOwnerUsesWholeScratchBufferWhenLengthMatches()
    {
        Span<int> scratch = stackalloc int[8];
        using var owner = new SpanOwner<int>(scratch, 8);
        owner.Span.Fill(5);
        Assert.Equal(8, owner.Span.Length);
        Assert.Equal(5, scratch[7]);
    }

    private static string[] DescribeBuckets(BytePool pool)
    {
        // Reads the private bucket array; update the field name here if it is renamed.
        var field = typeof(BytePool).GetField("buckets", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var buckets = Assert.IsAssignableFrom<Array>(field.GetValue(pool));
        return buckets.Cast<object?>().Where(x => x is not null).Select(x => x!.ToString()!).ToArray();
    }

    private sealed class Tracked(int id, List<int> disposed, Action? callback = null) : IDisposable
    {
        public int Id { get; } = id;

        public void Dispose()
        {
            disposed.Add(this.Id);
            callback?.Invoke();
        }
    }

    private sealed class Counted(Action onDispose) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                onDispose();
            }
        }
    }

    private sealed class NativeLikeMemoryManager(int length) : MemoryManager<byte>
    {
        private readonly byte[] storage = new byte[length];

        public override Span<byte> GetSpan() => this.storage;

        public override MemoryHandle Pin(int elementIndex = 0) => default;

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
