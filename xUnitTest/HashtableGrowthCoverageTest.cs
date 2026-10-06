// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class HashtableGrowthCoverageTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf16MapMatchesDictionaryWithReferenceInsertion(bool useSpan)
    {
        var map = new Utf16UnorderedMap<string?>();
        var expected = new Dictionary<string, string?>();
        var random = new Random(1847);
        for (var i = 0; i < 2500; i++)
        {
            var key = "日本語-" + random.Next(150);
            var value = i % 5 == 0 ? null : i.ToString();
            switch (random.Next(4))
            {
                case 0:
                    Assert.Equal(expected.TryAdd(key, value), useSpan ? map.TryAdd(key.AsSpan(), value) : map.TryAdd(key, value));
                    break;
                case 1:
                    if (useSpan)
                    {
                        map.AddOrUpdate(key.AsSpan(), value);
                    }
                    else
                    {
                        map.AddOrUpdate(key, value);
                    }

                    expected[key] = value;
                    break;
                case 2:
                    Assert.Equal(expected.Remove(key), useSpan ? map.Remove(key.AsSpan()) : map.Remove(key));
                    break;
                default:
                    ref var slot = ref (useSpan
                        ? ref map.GetValueRefOrAddDefault(key.AsSpan(), out var exists)
                        : ref map.GetValueRefOrAddDefault(key, out exists));
                    Assert.Equal(expected.ContainsKey(key), exists);
                    Assert.Equal(expected.GetValueOrDefault(key), slot);
                    slot = value;
                    expected[key] = value;
                    break;
            }

            Assert.Equal(expected.Count, map.Count);
        }

        Assert.Equal(expected.OrderBy(x => x.Key), map.OrderBy(x => x.Key));
        foreach (var pair in expected)
        {
            Assert.True(map.TryGetValue(pair.Key.AsSpan(), out var value));
            Assert.Equal(pair.Value, value);
        }

        Assert.False(map.TryGetValue("missing".AsSpan(), out _));
        map.Clear();
        map.Clear();
        Assert.False(map.ContainsValue(null));
        Assert.Empty(map);
        map.GetValueRefOrAddDefault("reused".AsSpan(), out var reused) = "value";
        Assert.False(reused);
        Assert.Equal("value", map["reused"]);
        Assert.Throws<ArgumentNullException>(() => map.GetValueRefOrAddDefault((string)null!, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Utf16UnorderedMap<int>(uint.MaxValue));
    }

    [Fact]
    public void Utf16MapEnumeratorChecksCurrentAndCanReset()
    {
        var map = new Utf16UnorderedMap<int>();
        map.AddOrUpdate("removed", 1);
        map.AddOrUpdate("kept", 2);
        Assert.True(map.Remove("removed"));
        var enumerator = ((IEnumerable)map).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(KeyValuePair.Create("kept", 2), enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(KeyValuePair.Create("kept", 2), enumerator.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf16TablePreservesNeighborsThroughGrowthUpdatesAndRemoval(bool useSpan)
    {
        var table = new Utf16Hashtable<int>();
        var keys = Enumerable.Range(0, 300).Select(i => "日本語-" + i).ToArray();
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Assert.Equal(i, useSpan ? table.GetOrAdd(key.AsSpan(), _ => i) : table.GetOrAdd(key, _ => i));
            Assert.Equal(i, table.GetOrAdd(key.AsSpan(), _ => throw new Exception("The key already exists.")));
            if (useSpan)
            {
                table.AddOrUpdate(key.AsSpan(), i + 1);
            }
            else
            {
                table.AddOrUpdate(key, i + 1);
            }
        }

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Assert.True(table.ContainsKey(key));
            Assert.True(table.ContainsKey(key.AsSpan()));
            Assert.True(useSpan ? table.TryRemove(key.AsSpan(), out var removed) : table.TryRemove(key, out removed));
            Assert.Equal(i + 1, removed);
            Assert.False(table.TryRemove(key));
            Assert.False(table.TryRemove(key.AsSpan()));
            Assert.False(table.TryGetValue(key.AsSpan(), out _));
        }

        Assert.Equal(0, table.Count);
        Assert.Empty(table.ToKeyValuePairs());
        Assert.Throws<ArgumentNullException>(() => table.TryRemove((string)null!));
        Assert.Throws<ArgumentNullException>(() => table.TryRemove((string)null!, out _));
    }

    [Fact]
    public void UInt64TableRemovesCollidingKeysAndReusesAfterClear()
    {
        var table = new UInt64Hashtable<int>();
        var keys = Enumerable.Range(0, 300).Select(i => (ulong)i * 4096).ToArray();
        for (var i = 0; i < keys.Length; i++)
        {
            Assert.True(table.TryAdd(keys[i], i));
        }

        foreach (var i in Enumerable.Range(0, keys.Length).Where(i => i % 3 == 1))
        {
            Assert.True(table.TryRemove(keys[i], out var value));
            Assert.Equal(i, value);
            Assert.False(table.TryRemove(keys[i]));
        }

        for (var i = 0; i < keys.Length; i++)
        {
            Assert.Equal(i % 3 != 1, table.TryGetValue(keys[i], out var value));
            if (i % 3 != 1)
            {
                Assert.Equal(i, value);
            }
        }

        Assert.Equal(200, table.Count);
        table.Clear();
        table.Clear();
        Assert.Empty(table.ToArray());
        Assert.False(table.TryRemove(0));
        Assert.Equal(7, table.GetOrAdd(0, _ => 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task ReadersSeeIntactValuesDuringGrowth(int kind)
    {
        const int count = 4096;
        var strings = Enumerable.Range(0, count).Select(i => "key-" + i).ToArray();
        var bytes = strings.Select(Encoding.UTF8.GetBytes).ToArray();
        Action<int, Payload> add;
        Func<int, Payload?> read;
        switch (kind)
        {
            case 0:
                var int32 = new Int32Hashtable<Payload>();
                add = (i, value) => int32.AddOrUpdate(i * 8, value);
                read = i => int32.TryGetValue(i * 8, out var value) ? value : null;
                break;
            case 1:
                var uint32 = new UInt32Hashtable<Payload>();
                add = (i, value) => uint32.AddOrUpdate((uint)i * 8, value);
                read = i => uint32.TryGetValue((uint)i * 8, out var value) ? value : null;
                break;
            case 2:
                var int64 = new Int64Hashtable<Payload>();
                add = (i, value) => int64.AddOrUpdate((long)i * 8, value);
                read = i => int64.TryGetValue((long)i * 8, out var value) ? value : null;
                break;
            case 3:
                var uint64 = new UInt64Hashtable<Payload>();
                add = (i, value) => uint64.AddOrUpdate((ulong)i * 8, value);
                read = i => uint64.TryGetValue((ulong)i * 8, out var value) ? value : null;
                break;
            case 4:
                var utf16 = new Utf16Hashtable<Payload>();
                add = (i, value) => utf16.AddOrUpdate(strings[i], value);
                read = i => utf16.TryGetValue(strings[i], out var value) ? value : null;
                break;
            default:
                var utf8 = new Utf8Hashtable<Payload>();
                add = (i, value) => utf8.AddOrUpdate(bytes[i], value);
                read = i => utf8.TryGetValue(bytes[i], out var value) ? value : null;
                break;
        }

        var expected = new Payload(1, long.MaxValue, long.MinValue);
        add(0, expected);
        using var progress = new SemaphoreSlim(0);
        var cancellationToken = TestContext.Current.CancellationToken;
        var phase = 0;
        var finished = 0;
        var reader = Task.Run(() =>
        {
            var observedPhase = -1;
            do
            {
                var currentPhase = Volatile.Read(ref phase);
                Assert.Equal(expected, read(0));
                if (observedPhase != currentPhase)
                {
                    observedPhase = currentPhase;
                    progress.Release();
                }
            }
            while (Volatile.Read(ref finished) == 0);
        }, cancellationToken);

        try
        {
            Assert.True(await progress.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
            for (var i = 1; i < count; i++)
            {
                add(i, new Payload(i, ~i, i * 17));
                if (i % 512 == 0 || i == count - 1)
                {
                    // Require reader progress at intermediate sizes, not just before or after growth.
                    Volatile.Write(ref phase, phase + 1);
                    Assert.True(await progress.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
                }
            }
        }
        finally
        {
            Volatile.Write(ref finished, 1);
            await reader;
        }

        Assert.Equal(expected, read(0));
        for (var i = 1; i < count; i++)
        {
            Assert.Equal(new Payload(i, ~i, i * 17), read(i));
        }
    }

    private readonly record struct Payload(long First, long Second, long Third);
}
