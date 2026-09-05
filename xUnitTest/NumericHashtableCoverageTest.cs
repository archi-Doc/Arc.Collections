using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class NumericHashtableCoverageTest
{
    [Fact]
    public void Int32MatchesDictionaryThroughCollisionsAndResize()
    {
        var table = new Int32Hashtable<string>(0);
        Verify<int>(new int[] { int.MinValue, -1, 0, 1, int.MaxValue }.Concat(Enumerable.Range(1, 256).Select(i => (int)(i * 1024))).Distinct().ToArray(),
            table.Add, table.TryAdd, table.TryGetValue, table.TryRemove, table.GetOrAdd,
            table.ContainsKey, () => table.Count, table.ToArray, table.Clear);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Int32Hashtable<string>(-1));
    }

    [Fact]
    public void Int32GetOrAddPublishesOneValue()
    {
        var table = new Int32Hashtable<string>();
        var calls = 0;
        Parallel.For(0, 100, _ =>
            Assert.Equal("value", table.GetOrAdd(1, key =>
            {
                Interlocked.Increment(ref calls);
                return "value";
            })));
        Assert.Equal(1, calls);
        Assert.Equal(1, table.Count);
        Assert.True(table.TryRemove(1));
        Assert.False(table.TryRemove(1));
    }

    [Fact]
    public void UInt32MatchesDictionaryThroughCollisionsAndResize()
    {
        var table = new UInt32Hashtable<string>(0);
        Verify<uint>(new uint[] { 0, 1, uint.MaxValue }.Concat(Enumerable.Range(1, 256).Select(i => (uint)(i * 1024))).Distinct().ToArray(),
            table.Add, table.TryAdd, table.TryGetValue, table.TryRemove, table.GetOrAdd,
            table.ContainsKey, () => table.Count, table.ToArray, table.Clear);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UInt32Hashtable<string>(-1));
    }

    [Fact]
    public void UInt32GetOrAddPublishesOneValue()
    {
        var table = new UInt32Hashtable<string>();
        var calls = 0;
        Parallel.For(0, 100, _ =>
            Assert.Equal("value", table.GetOrAdd(1, key =>
            {
                Interlocked.Increment(ref calls);
                return "value";
            })));
        Assert.Equal(1, calls);
        Assert.Equal(1, table.Count);
        Assert.True(table.TryRemove(1));
        Assert.False(table.TryRemove(1));
    }

    [Fact]
    public void Int64MatchesDictionaryThroughCollisionsAndResize()
    {
        var table = new Int64Hashtable<string>(0);
        Verify<long>(new long[] { long.MinValue, -1, 0, 1, 0x100000001L, long.MaxValue }.Concat(Enumerable.Range(1, 256).Select(i => (long)(i * 1024))).Distinct().ToArray(),
            table.Add, table.TryAdd, table.TryGetValue, table.TryRemove, table.GetOrAdd,
            table.ContainsKey, () => table.Count, table.ToArray, table.Clear);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Int64Hashtable<string>(-1));
    }

    [Fact]
    public void Int64GetOrAddPublishesOneValue()
    {
        var table = new Int64Hashtable<string>();
        var calls = 0;
        Parallel.For(0, 100, _ =>
            Assert.Equal("value", table.GetOrAdd(1, key =>
            {
                Interlocked.Increment(ref calls);
                return "value";
            })));
        Assert.Equal(1, calls);
        Assert.Equal(1, table.Count);
        Assert.True(table.TryRemove(1));
        Assert.False(table.TryRemove(1));
    }

    private delegate bool Lookup<TKey>(TKey key, [MaybeNullWhen(false)] out string value);

    private static void Verify<TKey>(TKey[] keys, Action<TKey, string> add, Func<TKey, string, bool> tryAdd,
        Lookup<TKey> lookup, Lookup<TKey> remove, Func<TKey, Func<TKey, string>, string> getOrAdd,
        Func<TKey, bool> contains, Func<int> count, Func<string[]> values, Action clear)
        where TKey : notnull
    {
        var expected = new Dictionary<TKey, string>();
        clear();
        Assert.Empty(values());
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var value = i.ToString();
            if (i % 2 == 0)
            {
                Assert.True(tryAdd(key, value));
            }
            else
            {
                Assert.Equal(value, getOrAdd(key, _ => value));
            }

            expected.Add(key, value);
        }

        foreach (var key in keys)
        {
            Assert.False(tryAdd(key, "duplicate"));
            Assert.Equal(expected[key], getOrAdd(key, _ => throw new Exception("Factory must not run.")));
            Assert.True(contains(key));
        }

        // Updating and removing interior collision-chain entries must preserve every neighbor.
        foreach (var key in keys.Where((_, i) => i % 3 == 0))
        {
            add(key, "updated-" + expected[key]);
            expected[key] = "updated-" + expected[key];
        }

        foreach (var key in keys.Where((_, i) => i % 3 == 1))
        {
            Assert.True(remove(key, out var removed));
            Assert.Equal(expected[key], removed);
            expected.Remove(key);
            Assert.False(remove(key, out _));
            Assert.False(contains(key));
        }

        Assert.Equal(expected.Count, count());
        foreach (var pair in expected)
        {
            Assert.True(lookup(pair.Key, out var actual));
            Assert.Equal(pair.Value, actual);
        }

        Assert.Equal(expected.Values.Order(), values().Order());
        Assert.Throws<ArgumentNullException>(() => getOrAdd(keys[0], null!));
        clear();
        Assert.Equal(0, count());
        foreach (var key in keys)
        {
            Assert.False(lookup(key, out var missing));
            Assert.Null(missing);
        }

        add(keys[0], "reused");
        Assert.True(lookup(keys[0], out var reused));
        Assert.Equal("reused", reused);
    }
}
