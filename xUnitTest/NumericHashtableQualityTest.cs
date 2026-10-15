// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class NumericHashtableQualityTest
{
    public static TheoryData<int> Kinds => new() { 0, 1, 2, 3 };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void CollidingKeysShareOneBucketInTheTable(int kind)
    {
        // The chain tests rely on these keys sharing one bucket, so check the table's own layout.
        // Keys that share a bucket of 2^17 also share one in every smaller table, which covers the sizes those tests reach.
        var table = Create(kind, 1 << 16);
        foreach (var key in table.CollidingKeys)
        {
            Assert.True(table.TryAdd(key, key.ToString()));
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var buckets = (Array)table.Instance.GetType().GetField("table", flags)!.GetValue(table.Instance)!;
        Assert.Equal(1 << 17, buckets.Length);

        var head = Assert.Single(buckets.Cast<object?>(), x => x is not null)!;
        var nextField = head.GetType().GetField("Next", flags)!;
        var chainLength = 0;
        for (var item = head; item is not null; item = nextField.GetValue(item))
        {
            chainLength++;
        }

        Assert.Equal(table.CollidingKeys.Length, chainLength);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void CollisionChainOperationsPreserveNeighbors(int kind)
    {
        var table = Create(kind);
        var keys = table.CollidingKeys;
        Assert.True(table.TryAdd(keys[0], "a0"));
        Assert.True(table.TryAdd(keys[1], "a1"));
        Assert.True(table.TryAdd(keys[2], "a2"));

        // GetOrAdd walks the existing chain under the lock before invoking the factory.
        Assert.Equal("a3", table.GetOrAdd(keys[3], key =>
        {
            Assert.Equal(keys[3], key);
            return "a3";
        }));

        Assert.Equal("a1", table.GetOrAdd(keys[1], _ => throw new InvalidOperationException("The key exists.")));
        Assert.False(table.TryAdd(keys[2], "duplicate"));

        // Replace the value of every chain position: head, interior, and tail.
        for (var i = 0; i < keys.Length; i++)
        {
            table.AddOrUpdate(keys[i], "u" + i);
            for (var j = 0; j < keys.Length; j++)
            {
                Assert.True(table.TryGetValue(keys[j], out var value));
                Assert.Equal((j <= i ? "u" : "a") + j, value);
            }
        }

        Assert.Equal(4, table.Count());

        // Remove an interior node, then the remaining ones from both ends.
        Assert.True(table.TryRemove(keys[1], out var removed));
        Assert.Equal("u1", removed);
        Assert.False(table.TryRemove(keys[1], out removed));
        Assert.Null(removed);
        Assert.Equal(new[] { "u0", "u2", "u3" }, table.ToArray().Order());

        // Growth must keep the colliding chain intact while non-colliding keys are redistributed.
        for (var i = 0; i < 100; i++)
        {
            Assert.True(table.TryAdd(1000 + i, "g" + i));
        }

        Assert.Equal(103, table.Count());
        foreach (var i in new[] { 0, 2, 3 })
        {
            Assert.True(table.TryGetValue(keys[i], out var value));
            Assert.Equal("u" + i, value);
        }

        Assert.False(table.TryGetValue(keys[1], out _));
        for (var i = 0; i < 100; i++)
        {
            Assert.True(table.TryGetValue(1000 + i, out var value));
            Assert.Equal("g" + i, value);
        }

        Assert.True(table.TryRemove(keys[3], out removed));
        Assert.Equal("u3", removed);
        Assert.True(table.TryRemove(keys[0], out removed));
        Assert.Equal("u0", removed);
        Assert.True(table.TryGetValue(keys[2], out var last));
        Assert.Equal("u2", last);
        Assert.Equal(101, table.Count());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void GetOrAddAloneGrowsTheTable(int kind)
    {
        var table = Create(kind, 0);
        var calls = 0;
        for (var i = 0; i < 200; i++)
        {
            // Every insertion goes through GetOrAdd, including those that cross the growth threshold.
            Assert.Equal("v" + i, table.GetOrAdd(i * 3, key =>
            {
                calls++;
                return "v" + (key / 3);
            }));
        }

        Assert.Equal(200, calls);
        Assert.Equal(200, table.Count());
        for (var i = 0; i < 200; i++)
        {
            Assert.Equal("v" + i, table.GetOrAdd(i * 3, _ => throw new InvalidOperationException("The key exists.")));
            Assert.False(table.TryGetValue((i * 3) + 1, out _));
        }

        Assert.Equal(200, calls);
        Assert.Equal(Enumerable.Range(0, 200).Select(i => "v" + i).Order(), table.ToArray().Order());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void GetOrAddWaitingForTheLockReturnsTheValueAddedMeanwhile(int kind)
    {
        var table = Create(kind);
        using var factoryEntered = new ManualResetEventSlim();
        using var releaseFactory = new ManualResetEventSlim();
        string? holderResult = null;
        string? waiterResult = null;
        Exception? error = null;
        var waiterFactoryCalls = 0;

        // Exceptions are captured because an unhandled exception on a raw thread would terminate the test host.
        Thread CreateThread(Action body) => new(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref error, ex, null);
            }
        })
        {
            IsBackground = true,
        };

        var holder = CreateThread(() => holderResult = table.GetOrAdd(42, _ =>
        {
            factoryEntered.Set();
            releaseFactory.Wait();
            return "holder";
        }));

        // The waiter misses on the lock-free path while the holder's factory runs, then blocks on the lock.
        var waiter = CreateThread(() => waiterResult = table.GetOrAdd(42, _ =>
        {
            Interlocked.Increment(ref waiterFactoryCalls);
            return "waiter";
        }));

        holder.Start();
        try
        {
            Assert.True(factoryEntered.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken), $"The holder's factory did not run: {error}");
            waiter.Start();

            // Waiting only makes the lock path more likely; the assertions hold either way.
            var stopwatch = Stopwatch.StartNew();
            while (waiter.IsAlive && (waiter.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0 && stopwatch.Elapsed < TimeSpan.FromSeconds(10))
            {
                Thread.Sleep(1);
            }
        }
        finally
        {
            releaseFactory.Set();
        }

        // Bounded joins turn a hang into a failure of this test only.
        Assert.True(holder.Join(TimeSpan.FromSeconds(30)), "The holder did not finish.");
        Assert.True(waiter.Join(TimeSpan.FromSeconds(30)), "The waiter did not finish.");
        Assert.Null(error);
        Assert.Equal("holder", holderResult);
        Assert.Equal("holder", waiterResult);
        Assert.Equal(0, waiterFactoryCalls);
        Assert.Equal(1, table.Count());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void GetOrAddRejectsFactoryThatGrowsOrEditsTheBucket(int kind)
    {
        // A factory that triggers growth invalidates the captured table.
        var table = Create(kind, 0);
        Assert.True(table.TryAdd(1, "1"));
        Assert.True(table.TryAdd(2, "2"));
        Assert.True(table.TryAdd(3, "3"));
        Assert.Throws<InvalidOperationException>(() => table.GetOrAdd(0, _ =>
        {
            Assert.True(table.TryAdd(4, "4"));
            Assert.True(table.TryAdd(5, "5"));
            return "0";
        }));

        Assert.False(table.TryGetValue(0, out _));
        Assert.Equal(5, table.Count());
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, table.ToArray().Order());

        // A factory that removes a node from the target bucket invalidates the captured chain.
        table = Create(kind);
        var keys = table.CollidingKeys;
        Assert.True(table.TryAdd(keys[0], "k0"));
        Assert.True(table.TryAdd(keys[1], "k1"));
        Assert.Throws<InvalidOperationException>(() => table.GetOrAdd(keys[2], key =>
        {
            Assert.True(table.TryRemove(keys[0], out _));
            return "k2";
        }));

        Assert.False(table.TryGetValue(keys[2], out _));
        Assert.False(table.TryGetValue(keys[0], out _));
        Assert.True(table.TryGetValue(keys[1], out var kept));
        Assert.Equal("k1", kept);
        Assert.Equal(1, table.Count());

        // A factory that replaces a value in the target bucket also invalidates the captured chain.
        Assert.Throws<InvalidOperationException>(() => table.GetOrAdd(keys[2], _ =>
        {
            table.AddOrUpdate(keys[1], "k1-updated");
            return "k2";
        }));

        Assert.False(table.TryGetValue(keys[2], out _));
        Assert.True(table.TryGetValue(keys[1], out kept));
        Assert.Equal("k1-updated", kept);
        Assert.Equal(1, table.Count());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task GetOrAddFactoryExceptionAddsNothingAndReleasesTheLock(int kind)
    {
        var table = Create(kind);
        Assert.True(table.TryAdd(2, "2"));
        Assert.Throws<FormatException>(() => table.GetOrAdd(1, _ => throw new FormatException()));
        Assert.False(table.TryGetValue(1, out _));
        Assert.Equal(1, table.Count());

        // Another thread can write only if the failed call released the lock.
        Assert.True(await Task.Run(() => table.TryAdd(1, "1"), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
        Assert.Equal("1", table.GetOrAdd(1, _ => "other"));
        Assert.Equal(2, table.Count());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task ReadersNeverMissStableKeysWhileTheirChainIsRewritten(int kind)
    {
        var table = Create(kind);
        var keys = table.CollidingKeys;
        Assert.True(table.TryAdd(keys[0], "0"));
        Assert.True(table.TryAdd(keys[1], "stable"));
        Assert.True(table.TryAdd(keys[2], "2"));
        Assert.True(table.TryAdd(keys[3], "updated-0"));

        var cancellationToken = TestContext.Current.CancellationToken;
        using var readerStarted = new ManualResetEventSlim();
        var finished = 0;
        var reader = Task.Run(() =>
        {
            do
            {
                // keys[1] is never written, and keys[3] is only replaced, so neither may ever be missing.
                Assert.True(table.TryGetValue(keys[1], out var stable));
                Assert.Equal("stable", stable);
                Assert.True(table.TryGetValue(keys[3], out var updated));
                Assert.StartsWith("updated-", updated);
                readerStarted.Set();
            }
            while (Volatile.Read(ref finished) == 0);
        }, cancellationToken);

        try
        {
            Assert.True(readerStarted.Wait(TimeSpan.FromSeconds(30), cancellationToken));
            for (var i = 1; i <= 20_000; i++)
            {
                // Rewrite the prefix and suffix around the stable node in different orders.
                Assert.True(table.TryRemove(keys[0], out _));
                table.AddOrUpdate(keys[3], "updated-" + i);
                Assert.True(table.TryAdd(keys[0], "0"));
                Assert.True(table.TryRemove(keys[2], out _));
                Assert.True(table.TryAdd(keys[2], "2"));
            }
        }
        finally
        {
            Volatile.Write(ref finished, 1);
            await reader;
        }

        Assert.Equal(4, table.Count());
        Assert.True(table.TryGetValue(keys[3], out var last));
        Assert.Equal("updated-20000", last);
    }

    private static Table Create(int kind, int capacity = 4)
    {
        switch (kind)
        {
            case 0:
                var int32 = new Int32Hashtable<string>(capacity);
                return new()
                {
                    Instance = int32,
                    CollidingKeys = [5, 5 | (1 << 30), 5 | int.MinValue, 5 | int.MinValue | (1 << 30)],
                    Count = () => int32.Count,
                    TryAdd = (key, value) => int32.TryAdd(checked((int)key), value),
                    AddOrUpdate = (key, value) => int32.AddOrUpdate(checked((int)key), value),
                    GetOrAdd = (key, factory) => int32.GetOrAdd(checked((int)key), x => factory(x)),
                    TryGetValue = (long key, [MaybeNullWhen(false)] out string value) => int32.TryGetValue(checked((int)key), out value),
                    TryRemove = (long key, [MaybeNullWhen(false)] out string value) => int32.TryRemove(checked((int)key), out value),
                    ToArray = int32.ToArray,
                };

            case 1:
                var uint32 = new UInt32Hashtable<string>(capacity);
                return new()
                {
                    Instance = uint32,
                    CollidingKeys = [5, 5 | (1L << 30), 5 | (1L << 31), 5 | (3L << 30)],
                    Count = () => uint32.Count,
                    TryAdd = (key, value) => uint32.TryAdd(checked((uint)key), value),
                    AddOrUpdate = (key, value) => uint32.AddOrUpdate(checked((uint)key), value),
                    GetOrAdd = (key, factory) => uint32.GetOrAdd(checked((uint)key), x => factory(x)),
                    TryGetValue = (long key, [MaybeNullWhen(false)] out string value) => uint32.TryGetValue(checked((uint)key), out value),
                    TryRemove = (long key, [MaybeNullWhen(false)] out string value) => uint32.TryRemove(checked((uint)key), out value),
                    ToArray = uint32.ToArray,
                };

            case 2:
                var int64 = new Int64Hashtable<string>(capacity);
                return new()
                {
                    Instance = int64,

                    // The upper and lower halves XOR to 5 for each key.
                    CollidingKeys = [5, 0x1_0000_0004L, 0x2_0000_0007L, -6],
                    Count = () => int64.Count,
                    TryAdd = int64.TryAdd,
                    AddOrUpdate = int64.AddOrUpdate,
                    GetOrAdd = (key, factory) => int64.GetOrAdd(key, x => factory(x)),
                    TryGetValue = (long key, [MaybeNullWhen(false)] out string value) => int64.TryGetValue(key, out value),
                    TryRemove = (long key, [MaybeNullWhen(false)] out string value) => int64.TryRemove(key, out value),
                    ToArray = int64.ToArray,
                };

            default:
                var uint64 = new UInt64Hashtable<string>(capacity);
                return new()
                {
                    Instance = uint64,

                    // The upper and lower halves XOR to 5 for each key; -6 maps to 0xFFFF_FFFF_FFFF_FFFA.
                    CollidingKeys = [5, 0x1_0000_0004L, 0x2_0000_0007L, -6],
                    Count = () => uint64.Count,
                    TryAdd = (key, value) => uint64.TryAdd(unchecked((ulong)key), value),
                    AddOrUpdate = (key, value) => uint64.AddOrUpdate(unchecked((ulong)key), value),
                    GetOrAdd = (key, factory) => uint64.GetOrAdd(unchecked((ulong)key), x => factory(unchecked((long)x))),
                    TryGetValue = (long key, [MaybeNullWhen(false)] out string value) => uint64.TryGetValue(unchecked((ulong)key), out value),
                    TryRemove = (long key, [MaybeNullWhen(false)] out string value) => uint64.TryRemove(unchecked((ulong)key), out value),
                    ToArray = uint64.ToArray,
                };
        }
    }

    private delegate bool TryFunc(long key, [MaybeNullWhen(false)] out string value);

    /// <summary>
    /// Exposes the four numeric hashtables through <see langword="long"/> keys.
    /// </summary>
    private sealed class Table
    {
        public required object Instance { get; init; }

        public required long[] CollidingKeys { get; init; }

        public required Func<int> Count { get; init; }

        public required Func<long, string, bool> TryAdd { get; init; }

        public required Action<long, string> AddOrUpdate { get; init; }

        public required Func<long, Func<long, string>, string> GetOrAdd { get; init; }

        public required TryFunc TryGetValue { get; init; }

        public required TryFunc TryRemove { get; init; }

        public required Func<string[]> ToArray { get; init; }
    }
}
