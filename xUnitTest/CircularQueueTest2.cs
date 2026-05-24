using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace Arc.Collections.Tests;

public sealed class CircularQueueTest2
{
    [Fact]
    public void Constructor_NegativeCapacity_UsesMinimumCapacity()
    {
        var queue = new CircularQueue<int>(-1);

        Assert.Equal(1, queue.Capacity);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Constructor_ZeroCapacity_UsesMinimumCapacity()
    {
        var queue = new CircularQueue<int>(0);

        Assert.Equal(1, queue.Capacity);
        Assert.Equal(0, queue.Count);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(7, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    [InlineData(1023, 1024)]
    [InlineData(1024, 1024)]
    [InlineData(1025, 2048)]
    public void Constructor_RoundsCapacityUpToPowerOfTwo(int requestedCapacity, int expectedCapacity)
    {
        var queue = new CircularQueue<int>(requestedCapacity);

        Assert.Equal(expectedCapacity, queue.Capacity);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var queue = new CircularQueue<int>(4);

        var result = queue.TryDequeue(out var item);

        Assert.False(result);
        Assert.Equal(default, item);
    }

    [Fact]
    public void TryEnqueue_SingleItem_ReturnsTrue()
    {
        var queue = new CircularQueue<int>(4);

        var result = queue.TryEnqueue(123);

        Assert.True(result);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void TryDequeue_SingleItem_ReturnsItem()
    {
        var queue = new CircularQueue<int>(4);

        Assert.True(queue.TryEnqueue(123));

        var result = queue.TryDequeue(out var item);

        Assert.True(result);
        Assert.Equal(123, item);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void TryEnqueue_WhenFull_ReturnsFalse()
    {
        var queue = new CircularQueue<int>(2);

        Assert.True(queue.TryEnqueue(1));
        Assert.True(queue.TryEnqueue(2));

        Assert.False(queue.TryEnqueue(3));
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void TryDequeue_PreservesFifoOrder()
    {
        var queue = new CircularQueue<int>(4);

        Assert.True(queue.TryEnqueue(1));
        Assert.True(queue.TryEnqueue(2));
        Assert.True(queue.TryEnqueue(3));

        Assert.True(queue.TryDequeue(out var item1));
        Assert.True(queue.TryDequeue(out var item2));
        Assert.True(queue.TryDequeue(out var item3));

        Assert.Equal(1, item1);
        Assert.Equal(2, item2);
        Assert.Equal(3, item3);

        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void TryEnqueue_AfterDequeue_ReusesSlot()
    {
        var queue = new CircularQueue<int>(2);

        Assert.True(queue.TryEnqueue(1));
        Assert.True(queue.TryEnqueue(2));
        Assert.False(queue.TryEnqueue(3));

        Assert.True(queue.TryDequeue(out var item1));
        Assert.Equal(1, item1);

        Assert.True(queue.TryEnqueue(3));

        Assert.True(queue.TryDequeue(out var item2));
        Assert.True(queue.TryDequeue(out var item3));

        Assert.Equal(2, item2);
        Assert.Equal(3, item3);
        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void RepeatedEnqueueDequeue_WrapsAroundCorrectly()
    {
        var queue = new CircularQueue<int>(8);

        const int Iterations = 100_000;

        for (var i = 0; i < Iterations; i++)
        {
            Assert.True(queue.TryEnqueue(i));
            Assert.True(queue.TryDequeue(out var item));
            Assert.Equal(i, item);
        }

        Assert.Equal(0, queue.Count);
        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public void BatchEnqueueDequeue_WrapsAroundCorrectly()
    {
        var queue = new CircularQueue<int>(8);

        const int Iterations = 10_000;

        var expected = 0;

        for (var round = 0; round < Iterations; round++)
        {
            for (var i = 0; i < queue.Capacity; i++)
            {
                Assert.True(queue.TryEnqueue(expected + i));
            }

            Assert.False(queue.TryEnqueue(-1));

            for (var i = 0; i < queue.Capacity; i++)
            {
                Assert.True(queue.TryDequeue(out var item));
                Assert.Equal(expected + i, item);
            }

            Assert.False(queue.TryDequeue(out _));

            expected += queue.Capacity;
        }
    }

    [Fact]
    public void ReferenceType_Dequeue_ReleasesReference()
    {
        var weakReference = CreateWeakReferenceAfterDequeue();

        ForceGarbageCollection();

        Assert.False(weakReference.IsAlive);
    }

    private static WeakReference CreateWeakReferenceAfterDequeue()
    {
        var queue = new CircularQueue<object>(1);

        var obj = new object();
        var weakReference = new WeakReference(obj);

        Assert.True(queue.TryEnqueue(obj));
        Assert.True(queue.TryDequeue(out var dequeued));
        Assert.Same(obj, dequeued);

        return weakReference;
    }

    [Fact]
    public async Task SingleProducerSingleConsumer_AllItemsAreTransferred()
    {
        var queue = new CircularQueue<int>(1024);

        const int Count = 1_000_000;

        var producer = Task.Run(() =>
        {
            for (var i = 0; i < Count; i++)
            {
                while (!queue.TryEnqueue(i))
                {
                    Thread.Yield();
                }
            }
        }, TestContext.Current.CancellationToken);

        var consumed = new int[Count];

        var consumer = Task.Run(() =>
        {
            for (var i = 0; i < Count; i++)
            {
                while (!queue.TryDequeue(out consumed[i]))
                {
                    Thread.Yield();
                }
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(producer, consumer);

        for (var i = 0; i < Count; i++)
        {
            Assert.Equal(i, consumed[i]);
        }

        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task MultipleProducersSingleConsumer_AllItemsAreTransferred()
    {
        var queue = new CircularQueue<long>(1024);

        const int ProducerCount = 4;
        const int ItemsPerProducer = 250_000;
        const int TotalCount = ProducerCount * ItemsPerProducer;

        var seen = new ConcurrentDictionary<long, byte>();

        var producers = Enumerable.Range(0, ProducerCount)
            .Select(producerId => Task.Run(() =>
            {
                var baseValue = (long)producerId << 32;

                for (var i = 0; i < ItemsPerProducer; i++)
                {
                    var value = baseValue | (uint)i;

                    while (!queue.TryEnqueue(value))
                    {
                        Thread.Yield();
                    }
                }
            }))
            .ToArray();

        var consumer = Task.Run(() =>
        {
            for (var i = 0; i < TotalCount; i++)
            {
                long value;

                while (!queue.TryDequeue(out value))
                {
                    Thread.Yield();
                }

                Assert.True(seen.TryAdd(value, 0), $"Duplicated value: {value}");
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(producers.Append(consumer));

        Assert.Equal(TotalCount, seen.Count);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task MultipleProducersMultipleConsumers_AllItemsAreTransferredExactlyOnce()
    {
        var queue = new CircularQueue<long>(1024);

        const int ProducerCount = 4;
        const int ConsumerCount = 4;
        const int ItemsPerProducer = 250_000;
        const int TotalCount = ProducerCount * ItemsPerProducer;

        var remaining = TotalCount;
        var seen = new ConcurrentDictionary<long, byte>();

        var producers = Enumerable.Range(0, ProducerCount)
            .Select(producerId => Task.Run(() =>
            {
                var baseValue = (long)producerId << 32;

                for (var i = 0; i < ItemsPerProducer; i++)
                {
                    var value = baseValue | (uint)i;

                    while (!queue.TryEnqueue(value))
                    {
                        Thread.Yield();
                    }
                }
            }))
            .ToArray();

        var consumers = Enumerable.Range(0, ConsumerCount)
            .Select(_ => Task.Run(() =>
            {
                while (true)
                {
                    var current = Volatile.Read(ref remaining);
                    if (current <= 0)
                    {
                        return;
                    }

                    if (queue.TryDequeue(out var value))
                    {
                        Assert.True(seen.TryAdd(value, 0), $"Duplicated value: {value}");
                        Interlocked.Decrement(ref remaining);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(producers);
        await Task.WhenAll(consumers);

        Assert.Equal(0, remaining);
        Assert.Equal(TotalCount, seen.Count);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task MultipleProducersMultipleConsumers_WithSmallCapacity_AllItemsAreTransferredExactlyOnce()
    {
        var queue = new CircularQueue<long>(2);

        const int ProducerCount = 4;
        const int ConsumerCount = 4;
        const int ItemsPerProducer = 50_000;
        const int TotalCount = ProducerCount * ItemsPerProducer;

        var remaining = TotalCount;
        var seen = new ConcurrentDictionary<long, byte>();

        var producers = Enumerable.Range(0, ProducerCount)
            .Select(producerId => Task.Run(() =>
            {
                var baseValue = (long)producerId << 32;

                for (var i = 0; i < ItemsPerProducer; i++)
                {
                    var value = baseValue | (uint)i;

                    while (!queue.TryEnqueue(value))
                    {
                        Thread.Yield();
                    }
                }
            }))
            .ToArray();

        var consumers = Enumerable.Range(0, ConsumerCount)
            .Select(_ => Task.Run(() =>
            {
                while (true)
                {
                    if (Volatile.Read(ref remaining) <= 0)
                    {
                        return;
                    }

                    if (queue.TryDequeue(out var value))
                    {
                        Assert.True(seen.TryAdd(value, 0), $"Duplicated value: {value}");
                        Interlocked.Decrement(ref remaining);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(producers);
        await Task.WhenAll(consumers);

        Assert.Equal(0, remaining);
        Assert.Equal(TotalCount, seen.Count);
        Assert.Equal(0, queue.Count);
    }

    private static void ForceGarbageCollection()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
