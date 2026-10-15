// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class PoolFinalAuditTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ObjectPoolDisposesRemainingObjectsAfterFailures(int failureCount)
    {
        var pool = new ObjectPool<Disposable>(() => new(), 4);
        var items = CreateItems(failureCount);
        foreach (var item in items)
        {
            pool.Return(item);
        }

        var error = Assert.Throws<AggregateException>(pool.Dispose);
        AssertFailures(items, failureCount, error);
        pool.Dispose();
        Assert.All(items, item => Assert.Equal(1, item.DisposeCount));
        Assert.Throws<ObjectDisposedException>(() => pool.Rent());
        Assert.Throws<ObjectDisposedException>(() => pool.Return(new()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CacheDisposesRemainingObjectsAfterFailures(int failureCount)
    {
        var cache = new KeyedObjectCache<int, Disposable>(4);
        var items = CreateItems(failureCount);
        for (var i = 0; i < items.Length; i++)
        {
            Assert.True(cache.TryAdd(i, items[i]));
        }

        var error = Assert.Throws<AggregateException>(cache.Dispose);
        AssertFailures(items, failureCount, error);
        Assert.Equal(0, cache.Count);
        cache.Dispose();
        Assert.All(items, item => Assert.Equal(1, item.DisposeCount));
        for (var i = 0; i < items.Length; i++)
        {
            Assert.Null(cache.TakeOrDefault(i));
        }

        Assert.False(cache.TryAdd(5, new()));
    }

    [Fact]
    public void CacheRemainsConsistentWhenFailingDisposalReenters()
    {
        var cache = new KeyedObjectCache<int, Disposable>(3);
        var taken = new Disposable();
        var last = new Disposable();
        var first = new Disposable(() =>
        {
            cache.Dispose();
            Assert.Same(taken, cache.TakeOrDefault(2));
            Assert.False(cache.TryAdd(4, new()));
        }, new InvalidOperationException("disposal failed"));
        Assert.True(cache.TryAdd(1, first));
        Assert.True(cache.TryAdd(2, taken));
        Assert.True(cache.TryAdd(3, last));

        var error = Assert.Throws<AggregateException>(cache.Dispose);
        Assert.Same(first.Error, Assert.Single(error.InnerExceptions));
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(0, taken.DisposeCount);
        Assert.Equal(1, last.DisposeCount);
        Assert.Equal(0, cache.Count);
        taken.Dispose();
    }

    [Fact]
    public void EmptyByteRentalsShareStorageButKeepIndependentOwnership()
    {
        var pool = BytePool.CreateFlat(16, 2);
        var first = pool.Rent(0);
        var second = pool.Rent(0);
        Assert.NotSame(first, second);
        Assert.Same(Array.Empty<byte>(), first.Array);
        Assert.Same(first.Array, second.Array);

        first.Return();
        Assert.True(first.IsReturned);
        Assert.True(second.IsRented);
        Assert.Equal(1, second.ReferenceCount);
        var shared = second.IncrementAndShare();
        second.Return();
        Assert.Equal(1, shared.ReferenceCount);
        shared.Return();
        Assert.True(second.IsReturned);
    }

    [Fact]
    public void EmptyByteRentalsAllocateOnlyTheirOwnershipHandle()
    {
        var pool = BytePool.CreateFlat(16, 2);
        var expected = AllocationHelper.Measure(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var owner = BytePool.RentedArray.CreateFrom(Array.Empty<byte>());
            }
        });
        var actual = AllocationHelper.Measure(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var owner = pool.Rent(0);
            }
        });

        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrimmingAtLeadingNullDoesNotAllocate()
    {
        byte[] source = [0, 1, 2];
        var allocated = AllocationHelper.Measure(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                Assert.Same(Array.Empty<byte>(), BaseHelper.TrimAtFirstNull(source));
            }
        });

        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);
    }

    private static Disposable[] CreateItems(int failureCount)
    {
        var items = new Disposable[4];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new(error: i < failureCount ? new InvalidOperationException($"failure {i}") : null);
        }

        return items;
    }

    private static void AssertFailures(Disposable[] items, int failureCount, AggregateException error)
    {
        Assert.Equal(failureCount, error.InnerExceptions.Count);
        Assert.All(items, item => Assert.Equal(1, item.DisposeCount));
        for (var i = 0; i < failureCount; i++)
        {
            Assert.Same(items[i].Error, error.InnerExceptions[i]);
        }
    }

    private sealed class Disposable(Action? callback = null, Exception? error = null) : IDisposable
    {
        public Exception? Error { get; } = error;

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            this.DisposeCount++;
            callback?.Invoke();
            if (this.Error is { } error)
            {
                throw error;
            }
        }
    }
}
