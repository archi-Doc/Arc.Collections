using System;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class ReviewRegressionTest
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SmallQueueDoesNotOverwriteUnreadItems(int capacity)
    {
        var queue = new CircularQueue<int>(capacity);
        for (var round = 0; round < 20; round++)
        {
            for (var i = 0; i < queue.Capacity; i++)
            {
                Assert.True(queue.TryEnqueue(i));
            }

            Assert.False(queue.TryEnqueue(-1));
            for (var i = 0; i < queue.Capacity; i++)
            {
                Assert.True(queue.TryDequeue(out var value));
                Assert.Equal(i, value);
            }

            Assert.False(queue.TryDequeue(out _));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void DuplicateCacheKeyDoesNotEvictAnyObject(int duplicateKey)
    {
        using var cache = new KeyedObjectCache<int, Disposable>(2);
        var first = new Disposable();
        var second = new Disposable();
        var rejected = new Disposable();
        Assert.True(cache.Cache(1, first));
        Assert.True(cache.Cache(2, second));
        Assert.False(cache.Cache(duplicateKey, rejected));
        Assert.Equal(2, cache.Count);
        Assert.Equal(0, first.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
        Assert.Equal(0, rejected.DisposeCount);
        Assert.Same(first, cache.TryGet(1));
        Assert.Same(second, cache.TryGet(2));
    }

    [Fact]
    public void OutstandingCacheLeaseIsDisposedWhenReturnedAfterCacheDisposal()
    {
        var cache = new KeyedObjectCache<int, Disposable>(2);
        var value = new Disposable();
        var lease = cache.CreateInterface(1, value);
        cache.Dispose();
        lease.Dispose();
        cache.Dispose();
        Assert.Equal(1, value.DisposeCount);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void OrderedListCanAddItself()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 3 });
        list.AddRange(list);
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(16)]
    public void OrderedListCanAddOverlappingSpan(int capacity)
    {
        var list = new OrderedList<int>(capacity);
        list.AddRange(new[] { 1, 2, 3 });
        list.AddRange(list.AsReadOnlySpan());
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 3 }, list.ToArray());
    }

    [Fact]
    public void OrderedListConstructorPreservesEqualElementOrder()
    {
        var input = Enumerable.Range(0, 100).Select(i => new Entry(i % 3, i)).ToArray();
        var comparer = System.Collections.Generic.Comparer<Entry>.Create((x, y) => x.Key.CompareTo(y.Key));
        var list = new OrderedList<Entry>(input, comparer);
        Assert.Equal(input.OrderBy(x => x.Key), list.ToArray());
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 5)]
    [InlineData(4, 1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void RentedMemorySliceCannotEscapeItsParent(int start, int length)
    {
        using var owner = BytePool.RentArray.CreateFrom(new byte[16]);
        var memory = owner.AsMemory(4, 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(start, length));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.ReadOnly.Slice(start, length));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void RentedMemorySliceStartMustBeInsideParent(int start)
    {
        using var owner = BytePool.RentArray.CreateFrom(new byte[16]);
        var memory = owner.AsMemory(4, 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(start));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.ReadOnly.Slice(start));
        Assert.Throws<ArgumentOutOfRangeException>(() => BytePool.RentMemory.Empty.Slice(start));
        Assert.Throws<ArgumentOutOfRangeException>(() => BytePool.RentReadOnlyMemory.Empty.Slice(start));
    }

    private sealed record Entry(int Key, int Sequence);

    private sealed class Disposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => this.DisposeCount++;
    }
}
