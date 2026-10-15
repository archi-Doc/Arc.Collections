// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class UtfHashtableQualityTest
{
    private static readonly Func<byte[], int> Utf8Factory = static _ => -1;
    private static readonly Func<string, int> Utf16Factory = static _ => -1;

    [Fact]
    public void EmptyHashtablesReturnEmptySnapshots()
    {
        var utf8 = new Utf8Hashtable<int>();
        var utf16 = new Utf16Hashtable<int>();
        utf8.Clear();
        utf16.Clear();
        Assert.Empty(utf8.ToArray());
        Assert.Empty(utf8.ToKeyValuePairs());
        Assert.Empty(utf16.ToArray());
        Assert.Empty(utf16.ToKeyValuePairs());

        Assert.True(utf8.TryAdd("key"u8, 1));
        Assert.True(utf16.TryAdd("key", 1));
        utf8.Clear();
        utf16.Clear();
        Assert.Equal(0, utf8.Count);
        Assert.Equal(0, utf16.Count);
        Assert.Empty(utf8.ToKeyValuePairs());
        Assert.Empty(utf16.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf8HashtableUpdatesAndRemovesKeysInsideACollisionChain(bool useSpan)
    {
        // The minimum table has 8 buckets, so three colliding keys share one chain without a resize.
        var keys = FindCollidingKeys(3, static key => XxHash3Slim.Hash64(Encoding.UTF8.GetBytes(key))).Select(Encoding.UTF8.GetBytes).ToArray();
        var table = new Utf8Hashtable<int>(0);
        for (var i = 0; i < keys.Length; i++)
        {
            Assert.True(table.TryAdd(keys[i], i));
        }

        // The oldest key is the deepest node, so updating it rebuilds the prefix of the chain.
        Assert.False(useSpan ? table.TryAdd(keys[0].AsSpan(), -1) : table.TryAdd(keys[0], -1));
        if (useSpan)
        {
            table.AddOrUpdate(keys[0].AsSpan(), 100);
            table.AddOrUpdate(keys[1].AsSpan(), 101);
        }
        else
        {
            table.AddOrUpdate(keys[0], 100);
            table.AddOrUpdate(keys[1], 101);
        }

        Assert.Equal(3, table.Count);
        Assert.True(table.TryGetValue(keys[0], out var value));
        Assert.Equal(100, value);
        Assert.True(table.TryGetValue(keys[1].AsSpan(), out value));
        Assert.Equal(101, value);
        Assert.True(table.TryGetValue(keys[2], out value));
        Assert.Equal(2, value);

        // The chain is now keys[1] -> keys[2] -> keys[0], so both removals below clone a prefix.
        Assert.True(useSpan ? table.TryRemove(keys[2].AsSpan(), out var removed) : table.TryRemove(keys[2], out removed));
        Assert.Equal(2, removed);
        Assert.False(table.ContainsKey(keys[2]));
        Assert.Equal(2, table.Count);
        Assert.Equal(new[] { 100, 101 }, table.ToArray().Order());

        // Updates keep the original key array.
        Assert.Contains(table.ToKeyValuePairs(), pair => ReferenceEquals(pair.Key, keys[0]) && pair.Value == 100);

        Assert.True(useSpan ? table.TryRemove(keys[0].AsSpan(), out removed) : table.TryRemove(keys[0], out removed));
        Assert.Equal(100, removed);
        Assert.False(table.ContainsKey(keys[0].AsSpan()));
        var remaining = Assert.Single(table.ToKeyValuePairs());
        Assert.Same(keys[1], remaining.Key);
        Assert.Equal(101, remaining.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf16HashtableUpdatesAndRemovesKeysInsideACollisionChain(bool useSpan)
    {
        var keys = FindCollidingKeys(3, static key => XxHash3Slim.Hash64(key.AsSpan()));
        var table = new Utf16Hashtable<int>(0);
        for (var i = 0; i < keys.Length; i++)
        {
            Assert.True(table.TryAdd(keys[i], i));
        }

        Assert.False(useSpan ? table.TryAdd(keys[0].AsSpan(), -1) : table.TryAdd(keys[0], -1));
        if (useSpan)
        {
            table.AddOrUpdate(keys[0].AsSpan(), 100);
            table.AddOrUpdate(keys[1].AsSpan(), 101);
        }
        else
        {
            table.AddOrUpdate(keys[0], 100);
            table.AddOrUpdate(keys[1], 101);
        }

        Assert.Equal(3, table.Count);
        Assert.True(table.TryGetValue(keys[0], out var value));
        Assert.Equal(100, value);
        Assert.True(table.TryGetValue(keys[1].AsSpan(), out value));
        Assert.Equal(101, value);
        Assert.True(table.TryGetValue(keys[2], out value));
        Assert.Equal(2, value);

        // The chain is now keys[1] -> keys[2] -> keys[0], so both removals below clone a prefix.
        Assert.True(useSpan ? table.TryRemove(keys[2].AsSpan(), out var removed) : table.TryRemove(keys[2], out removed));
        Assert.Equal(2, removed);
        Assert.False(table.ContainsKey(keys[2]));
        Assert.Equal(2, table.Count);
        Assert.Equal(new[] { 100, 101 }, table.ToArray().Order());
        Assert.Contains(table.ToKeyValuePairs(), pair => ReferenceEquals(pair.Key, keys[0]) && pair.Value == 100);

        Assert.True(useSpan ? table.TryRemove(keys[0].AsSpan(), out removed) : table.TryRemove(keys[0], out removed));
        Assert.Equal(100, removed);
        Assert.False(table.ContainsKey(keys[0].AsSpan()));
        var remaining = Assert.Single(table.ToKeyValuePairs());
        Assert.Same(keys[1], remaining.Key);
        Assert.Equal(101, remaining.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashtablesGrowFromTheMinimumCapacityThroughEveryInsertionPath(bool useSpan)
    {
        const int Count = 200;
        var utf8 = new Utf8Hashtable<int>(0);
        var utf16 = new Utf16Hashtable<int>(0);
        var keys = Enumerable.Range(0, Count).Select(i => "grow-" + i).ToArray();
        var bytes = keys.Select(Encoding.UTF8.GetBytes).ToArray();
        var factoryKeys = new byte[Count][];
        for (var i = 0; i < Count; i++)
        {
            var index = i;
            var value = useSpan
                ? utf8.GetOrAdd(bytes[i].AsSpan(), key =>
                {
                    factoryKeys[index] = key;
                    return index;
                })
                : utf8.GetOrAdd(bytes[i], key =>
                {
                    factoryKeys[index] = key;
                    return index;
                });
            Assert.Equal(i, value);

            if (useSpan)
            {
                Assert.True(utf16.TryAdd(keys[i].AsSpan(), i));
            }
            else
            {
                utf16.AddOrUpdate(keys[i].AsSpan(), i);
            }
        }

        Assert.Equal(Count, utf8.Count);
        Assert.Equal(Count, utf16.Count);
        for (var i = 0; i < Count; i++)
        {
            Assert.True(utf8.TryGetValue(bytes[i].AsSpan(), out var value));
            Assert.Equal(i, value);
            Assert.True(utf16.TryGetValue(keys[i], out value));
            Assert.Equal(i, value);

            // The span overload passes a copy, the array overload passes the caller's array.
            Assert.Equal(bytes[i], factoryKeys[i]);
            Assert.Equal(!useSpan, ReferenceEquals(bytes[i], factoryKeys[i]));
        }

        // The factory receives the key instance that is stored.
        var stored = utf8.ToKeyValuePairs().ToDictionary(pair => pair.Value, pair => pair.Key);
        for (var i = 0; i < Count; i++)
        {
            Assert.Same(factoryKeys[i], stored[i]);
        }
    }

    [Fact]
    public void Utf16HashtableSpanGetOrAddCanInternTheStoredKey()
    {
        var table = new Utf16Hashtable<string>();
        var buffer = "prefix-interned".ToCharArray();
        var interned = table.GetOrAdd(buffer.AsSpan(7), static key => key);

        Assert.Equal("interned", interned);
        Assert.Same(interned, table.GetOrAdd("interned".AsSpan(), ThrowingStringFactory));
        Assert.Same(interned, Assert.Single(table.ToKeyValuePairs()).Key);
    }

    [Fact]
    public void GetOrAddWaitingForTheLockUsesTheValueAddedByTheLockHolder()
    {
        var utf8 = new Utf8Hashtable<int>();
        var bytes = "contended"u8.ToArray();
        AssertWaitingGetOrAddUsesExistingValue(factory => utf8.GetOrAdd(bytes, _ => factory()));
        Assert.True(utf8.TryRemove(bytes));
        AssertWaitingGetOrAddUsesExistingValue(factory => utf8.GetOrAdd(bytes.AsSpan(), _ => factory()));
        Assert.Equal(1, utf8.Count);

        var utf16 = new Utf16Hashtable<int>();
        AssertWaitingGetOrAddUsesExistingValue(factory => utf16.GetOrAdd("contended", _ => factory()));
        Assert.True(utf16.TryRemove("contended"));
        AssertWaitingGetOrAddUsesExistingValue(factory => utf16.GetOrAdd("contended".AsSpan(), _ => factory()));
        Assert.Equal(1, utf16.Count);
    }

    [Fact]
    public void NullKeysAreRejectedWithTheKeyParameterName()
    {
        var utf16 = new Utf16Hashtable<int>();
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16.TryAdd((string)null!, 1)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16.AddOrUpdate((string)null!, 1)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16.GetOrAdd((string)null!, Utf16Factory)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16.TryGetValue((string)null!, out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16.ContainsKey((string)null!)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => utf16.GetOrAdd("key", null!)).ParamName);
        Assert.Equal("valueFactory", Assert.Throws<ArgumentNullException>(() => utf16.GetOrAdd("key".AsSpan(), null!)).ParamName);

        var utf8 = new Utf8Hashtable<int>();
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8.ContainsKey((byte[])null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8.TryRemove((byte[])null!, out _)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8.GetOrAdd((byte[])null!, Utf8Factory)).ParamName);

        var utf8Map = new Utf8UnorderedMap<int>();
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8Map.ContainsKey((byte[])null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8Map[(byte[])null!]).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf8Map[(byte[])null!] = 1).ParamName);

        var utf16Map = new Utf16UnorderedMap<int>();
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16Map.ContainsKey((string)null!)).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16Map[(string)null!]).ParamName);
        Assert.Equal("key", Assert.Throws<ArgumentNullException>(() => utf16Map[(string)null!] = 1).ParamName);
        Assert.Equal(0, utf8Map.Count + utf16Map.Count);
    }

    [Fact]
    public void Utf8MapDetectsACorruptedCollisionChain()
    {
        var map = new Utf8UnorderedMap<int>();
        map.AddOrUpdate("loop"u8, 1);
        CreateSelfLoop(map);
        var other = "other"u8.ToArray();

        // The looping node is found before the loop matters.
        Assert.Equal(1, map["loop"u8.ToArray()]);

        Assert.Throws<InvalidOperationException>(() => map.TryGetValue(other, out _));
        Assert.Throws<InvalidOperationException>(() => map.TryGetValue(other.AsSpan(), out _));
        Assert.Throws<InvalidOperationException>(() => map.ContainsKey(other));
        Assert.Throws<InvalidOperationException>(() => map.TryAdd(other, 2));
        Assert.Throws<InvalidOperationException>(() => map.AddOrUpdate(other.AsSpan(), 2));
        Assert.Throws<InvalidOperationException>(() => map.GetValueRefOrAddDefault(other, out _));
        Assert.Throws<InvalidOperationException>(() => map.GetValueRefOrAddDefault(other.AsSpan(), out _));
        Assert.Throws<InvalidOperationException>(() => map.Remove(other));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Utf16MapDetectsACorruptedCollisionChain()
    {
        var map = new Utf16UnorderedMap<int>();
        map.AddOrUpdate("loop", 1);
        CreateSelfLoop(map);

        Assert.Equal(1, map["loop"]);

        Assert.Throws<InvalidOperationException>(() => map.TryGetValue("other", out _));
        Assert.Throws<InvalidOperationException>(() => map.TryGetValue("other".AsSpan(), out _));
        Assert.Throws<InvalidOperationException>(() => map.ContainsKey("other"));
        Assert.Throws<InvalidOperationException>(() => map.TryAdd("other", 2));
        Assert.Throws<InvalidOperationException>(() => map.AddOrUpdate("other".AsSpan(), 2));
        Assert.Throws<InvalidOperationException>(() => map.GetValueRefOrAddDefault("other", out _));
        Assert.Throws<InvalidOperationException>(() => map.GetValueRefOrAddDefault("other".AsSpan(), out _));
        Assert.Throws<InvalidOperationException>(() => map.Remove("other"));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void MapsKeepAllEntriesWhenGrowingAfterFreeListReuse()
    {
        var utf8 = new Utf8UnorderedMap<int>();
        var utf16 = new Utf16UnorderedMap<int>();
        var expected = new Dictionary<string, int>();
        for (var i = 0; i < 8; i++)
        {
            Add("k" + i, i);
        }

        for (var i = 0; i < 8; i += 2)
        {
            Assert.True(utf8.Remove(Encoding.UTF8.GetBytes("k" + i)));
            Assert.True(utf16.Remove("k" + i));
            expected.Remove("k" + i);
        }

        // Refill the freed nodes, then grow.
        for (var i = 8; i < 12; i++)
        {
            Add("k" + i, i);
        }

        Assert.Equal(8, utf8.Capacity);
        Assert.Equal(8, utf16.Capacity);
        for (var i = 12; i < 40; i++)
        {
            Add("k" + i, i);
        }

        Assert.True(utf8.Capacity >= 32);
        Assert.True(utf16.Capacity >= 32);
        Assert.Equal(expected.Count, utf8.Count);
        Assert.Equal(expected.Count, utf16.Count);
        foreach (var pair in expected)
        {
            Assert.True(utf8.TryGetValue(Encoding.UTF8.GetBytes(pair.Key), out var value));
            Assert.Equal(pair.Value, value);
            Assert.True(utf16.TryGetValue(pair.Key.AsSpan(), out value));
            Assert.Equal(pair.Value, value);
        }

        for (var i = 0; i < 8; i += 2)
        {
            Assert.False(utf8.ContainsKey(Encoding.UTF8.GetBytes("k" + i)));
            Assert.False(utf16.ContainsKey("k" + i));
        }

        Assert.Equal(expected.OrderBy(x => x.Key), utf16.OrderBy(x => x.Key));
        Assert.Equal(expected.OrderBy(x => x.Key), utf8.Select(x => KeyValuePair.Create(Encoding.UTF8.GetString(x.Key), x.Value)).OrderBy(x => x.Key));

        void Add(string key, int value)
        {
            Assert.True(utf8.TryAdd(Encoding.UTF8.GetBytes(key), value));
            Assert.True(utf16.TryAdd(key, value));
            expected.Add(key, value);
        }
    }

    [Fact]
    public void Utf8MapEnumeratorsFollowTheEnumeratorContract()
    {
        var map = new Utf8UnorderedMap<int>();
        var removed = "removed"u8.ToArray();
        var kept = "kept"u8.ToArray();
        map.AddOrUpdate(removed, 1);
        map.AddOrUpdate(kept, 2);
        Assert.True(map.Remove(removed));

        var count = 0;
        foreach (var pair in map)
        {
            Assert.Same(kept, pair.Key);
            Assert.Equal(2, pair.Value);
            count++;
        }

        Assert.Equal(1, count);

        var enumerator = ((IEnumerable)map).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(KeyValuePair.Create(kept, 2), enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(KeyValuePair.Create(kept, 2), enumerator.Current);

        using var generic = ((IEnumerable<KeyValuePair<byte[], int>>)new Utf8UnorderedMap<int>()).GetEnumerator();
        Assert.False(generic.MoveNext());
        Assert.Equal(default(KeyValuePair<byte[], int>), generic.Current);
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)generic).Current);
    }

    [Fact]
    public void DefaultMapNodesAreEmpty()
    {
        Assert.Null(default(Utf8UnorderedMap<int>.Node).Key);
        Assert.Equal(0, default(Utf8UnorderedMap<int>.Node).Value);
        Assert.Null(default(Utf16UnorderedMap<string>.Node).Key);
        Assert.Null(default(Utf16UnorderedMap<string>.Node).Value);
    }

    [Fact]
    public void SpanOperationsOnExistingKeysDoNotAllocate()
    {
        var utf8Map = new Utf8UnorderedMap<int>();
        var utf16Map = new Utf16UnorderedMap<int>();
        var utf8Table = new Utf8Hashtable<int>();
        var utf16Table = new Utf16Hashtable<int>();
        utf8Map.AddOrUpdate("key"u8, 1);
        utf16Map.AddOrUpdate("key", 1);
        utf8Table.AddOrUpdate("key"u8, 1);
        utf16Table.AddOrUpdate("key", 1);

        var checksum = 0;
        var allocated = AllocationHelper.Measure(() => checksum += Run());

        Assert.NotEqual(0, checksum);
        Assert.Equal(1, utf8Map.Count);
        Assert.Equal(1, utf16Map.Count);
        Assert.Equal(1, utf8Table.Count);
        Assert.Equal(1, utf16Table.Count);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        int Run()
        {
            var sum = 0;
            for (var i = 0; i < 100; i++)
            {
                // Existing keys are never materialized.
                sum += utf8Map.TryAdd("key"u8, i) ? 1000 : 1;
                utf8Map.AddOrUpdate("key"u8, i);
                utf8Map.GetValueRefOrAddDefault("key"u8, out _)++;
                sum += utf8Map.TryGetValue("key"u8, out var value) ? value : 0;
                sum += utf8Map.Remove("missing"u8) ? 1000 : 1;
                foreach (var pair in utf8Map)
                {
                    sum += pair.Value;
                }

                sum += utf16Map.TryAdd("key".AsSpan(), i) ? 1000 : 1;
                utf16Map.AddOrUpdate("key".AsSpan(), i);
                utf16Map.GetValueRefOrAddDefault("key".AsSpan(), out _)++;
                sum += utf16Map.TryGetValue("key".AsSpan(), out value) ? value : 0;
                sum += utf16Map.Remove("missing".AsSpan()) ? 1000 : 1;
                foreach (var pair in utf16Map)
                {
                    sum += pair.Value;
                }

                sum += utf8Table.TryAdd("key"u8, i) ? 1000 : 1;
                sum += utf8Table.GetOrAdd("key"u8, Utf8Factory);
                sum += utf8Table.TryRemove("missing"u8) ? 1000 : 1;
                sum += utf16Table.TryAdd("key".AsSpan(), i) ? 1000 : 1;
                sum += utf16Table.GetOrAdd("key".AsSpan(), Utf16Factory);
                sum += utf16Table.TryRemove("missing".AsSpan()) ? 1000 : 1;
            }

            return sum;
        }
    }

    [Fact(Explicit = true)] // Allocates about 2 GB; run with --explicit on.
    public void FailedSpanKeyCopyLeavesCollectionsUnchanged()
    {
        // Longer than the maximum string length and, as bytes, longer than Array.MaxLength,
        // so the key can be hashed and looked up but copying it throws OutOfMemoryException.
        var chars = new char[1_073_741_800];

        var utf8Map = new Utf8UnorderedMap<int>();
        utf8Map.AddOrUpdate("a"u8, 1);
        Assert.Throws<OutOfMemoryException>(() => utf8Map.TryAdd(MemoryMarshal.AsBytes(chars.AsSpan()), 2));
        Assert.Throws<OutOfMemoryException>(() => utf8Map.AddOrUpdate(MemoryMarshal.AsBytes(chars.AsSpan()), 2));
        Assert.Throws<OutOfMemoryException>(() => utf8Map.GetValueRefOrAddDefault(MemoryMarshal.AsBytes(chars.AsSpan()), out _));
        Assert.Equal(1, utf8Map.Count);
        Assert.False(utf8Map.ContainsValue(0));
        Assert.Equal("a"u8.ToArray(), Assert.Single(utf8Map).Key);

        var utf16Map = new Utf16UnorderedMap<int>();
        utf16Map.AddOrUpdate("a", 1);
        Assert.Throws<OutOfMemoryException>(() => utf16Map.TryAdd(chars.AsSpan(), 2));
        Assert.Throws<OutOfMemoryException>(() => utf16Map.AddOrUpdate(chars.AsSpan(), 2));
        Assert.Throws<OutOfMemoryException>(() => utf16Map.GetValueRefOrAddDefault(chars.AsSpan(), out _));
        Assert.Equal(1, utf16Map.Count);
        Assert.False(utf16Map.ContainsValue(0));
        Assert.Equal("a", Assert.Single(utf16Map).Key);

        // Growing afterwards must not pick up a half-initialized node.
        for (var i = 0; i < 20; i++)
        {
            utf8Map.AddOrUpdate(Encoding.UTF8.GetBytes("k" + i), i);
            utf16Map.AddOrUpdate("k" + i, i);
        }

        Assert.Equal(21, utf8Map.Count);
        Assert.Equal(21, utf16Map.Count);
        Assert.All(utf8Map, pair => Assert.NotNull(pair.Key));
        Assert.All(utf16Map, pair => Assert.NotNull(pair.Key));

        var utf8Table = new Utf8Hashtable<int>();
        var utf16Table = new Utf16Hashtable<int>();
        Assert.Throws<OutOfMemoryException>(() => utf8Table.TryAdd(MemoryMarshal.AsBytes(chars.AsSpan()), 2));
        Assert.Throws<OutOfMemoryException>(() => utf8Table.GetOrAdd(MemoryMarshal.AsBytes(chars.AsSpan()), Utf8Factory));
        Assert.Throws<OutOfMemoryException>(() => utf16Table.TryAdd(chars.AsSpan(), 2));
        Assert.Throws<OutOfMemoryException>(() => utf16Table.GetOrAdd(chars.AsSpan(), Utf16Factory));
        Assert.Equal(0, utf8Table.Count);
        Assert.Equal(0, utf16Table.Count);
        Assert.Empty(utf8Table.ToKeyValuePairs());
        Assert.Empty(utf16Table.ToKeyValuePairs());
    }

    private static string ThrowingStringFactory(string key)
        => throw new InvalidOperationException("The key already exists.");

    private static string[] FindCollidingKeys(int count, Func<string, ulong> hash)
    {
        // Keys whose low three hash bits match share a bucket in an 8-bucket table.
        var keys = new List<string>();
        var bucket = -1;
        for (var i = 0; keys.Count < count; i++)
        {
            var key = "chain-" + i;
            var index = (int)(hash(key) & 7);
            if (bucket < 0)
            {
                bucket = index;
            }

            if (index == bucket)
            {
                keys.Add(key);
            }
        }

        return keys.ToArray();
    }

    private static void CreateSelfLoop(object map)
    {
        // Simulates corruption by unsynchronized writers: every bucket leads to node 0, which links to itself.
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = map.GetType();
        Array.Fill((int[])type.GetField("_buckets", Flags)!.GetValue(map)!, 1);
        var nodes = (Array)type.GetField("_nodes", Flags)!.GetValue(map)!;
        var node = nodes.GetValue(0)!;
        node.GetType().GetField("next", Flags)!.SetValue(node, 0);
        nodes.SetValue(node, 0);
    }

    private static void AssertWaitingGetOrAddUsesExistingValue(Func<Func<int>, int> getOrAdd)
    {
        var secondFactoryCalls = 0;
        var secondResult = 0;
        Exception? secondError = null;
        Thread? second = null;
        using var secondStarted = new ManualResetEventSlim();

        var first = getOrAdd(() =>
        {
            // This factory runs under the write lock, so the second call cannot insert yet.
            var thread = new Thread(() =>
            {
                try
                {
                    secondStarted.Set();
                    secondResult = getOrAdd(() =>
                    {
                        Interlocked.Increment(ref secondFactoryCalls);
                        return 2;
                    });
                }
                catch (Exception ex)
                {
                    secondError = ex;
                }
            })
            {
                IsBackground = true,
            };

            second = thread;
            thread.Start();
            secondStarted.Wait(TimeSpan.FromSeconds(30));

            // Let the second call miss its lock-free lookup and block on the lock.
            // The assertions below hold for any interleaving; the wait only makes the contended path likely.
            SpinWait.SpinUntil(() => (thread.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(10));
            return 1;
        });

        // Bounded joins turn a hang into a failure of this test only.
        Assert.True(second!.Join(TimeSpan.FromSeconds(30)), "The second call did not finish.");
        Assert.Null(secondError);
        Assert.Equal(1, first);
        Assert.Equal(1, secondResult);
        Assert.Equal(0, secondFactoryCalls);
    }
}
