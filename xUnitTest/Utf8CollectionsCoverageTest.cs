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

public class Utf8CollectionsCoverageTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapMatchesDictionaryAcrossGrowthRemovalAndReuse(bool useSpan)
    {
        var map = new Utf8UnorderedMap<string?>();
        var expected = new Dictionary<string, string?>();
        var random = new Random(4201);
        for (var i = 0; i < 3000; i++)
        {
            var key = "日本語-" + random.Next(150);
            var bytes = Encoding.UTF8.GetBytes(key);
            var value = i % 5 == 0 ? null : i.ToString();
            switch (random.Next(4))
            {
                case 0:
                    Assert.Equal(expected.TryAdd(key, value), useSpan ? map.TryAdd(bytes.AsSpan(), value) : map.TryAdd(bytes, value));
                    break;
                case 1:
                    if (useSpan)
                    {
                        map.Add(bytes.AsSpan(), value);
                    }
                    else
                    {
                        map[bytes] = value;
                    }

                    expected[key] = value;
                    break;
                case 2:
                    Assert.Equal(expected.Remove(key), useSpan ? map.Remove(bytes.AsSpan()) : map.Remove(bytes));
                    break;
                default:
                    var expectedExists = expected.ContainsKey(key);
                    ref var slot = ref (useSpan
                        ? ref map.GetValueRefOrAddDefault(bytes.AsSpan(), out var exists)
                        : ref map.GetValueRefOrAddDefault(bytes, out exists));
                    Assert.Equal(expectedExists, exists);
                    Assert.Equal(expected.GetValueOrDefault(key), slot);
                    slot = value;
                    expected[key] = value;
                    break;
            }

            Assert.Equal(expected.Count, map.Count);
        }

        Assert.True(map.Capacity >= map.Count);
        foreach (var pair in expected)
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Key);
            Assert.True(map.ContainsKey(bytes));
            Assert.True(map.ContainsKey(bytes.AsSpan()));
            Assert.True(map.TryGetValue(bytes, out var actual));
            Assert.Equal(pair.Value, actual);
            Assert.True(map.TryGetValue(bytes.AsSpan(), out actual));
            Assert.Equal(pair.Value, map[bytes]);
            Assert.True(map.ContainsValue(pair.Value));
        }

        Assert.False(map.ContainsValue("missing"));
        Assert.Equal(expected.OrderBy(x => x.Key), map.Select(x => KeyValuePair.Create(Encoding.UTF8.GetString(x.Key), x.Value)).OrderBy(x => x.Key));
        var enumerator = ((IEnumerable)map).GetEnumerator();
        var count = 0;
        while (enumerator.MoveNext())
        {
            Assert.IsType<KeyValuePair<byte[], string?>>(enumerator.Current);
            count++;
        }

        Assert.Equal(map.Count, count);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        map.Clear();
        map.Clear();
        Assert.Empty(map);
        Assert.False(map.ContainsValue(null));
        Assert.False(map.TryGetValue("missing"u8, out _));
        Assert.Throws<KeyNotFoundException>(() => map["missing"u8.ToArray()]);
        map.Add(Array.Empty<byte>(), "empty");
        Assert.Equal("empty", map[Array.Empty<byte>()]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HashtableMatchesDictionaryAcrossGrowthAndRemoval(bool useSpan)
    {
        var table = new Utf8Hashtable<int>(0);
        var expected = new Dictionary<string, int>();
        for (var i = 0; i < 300; i++)
        {
            var key = i == 0 ? "" : "日本語-" + i;
            var bytes = Encoding.UTF8.GetBytes(key);
            Assert.True(useSpan ? table.TryAdd(bytes.AsSpan(), i) : table.TryAdd(bytes, i));
            Assert.False(useSpan ? table.TryAdd(bytes.AsSpan(), -1) : table.TryAdd(bytes, -1));
            if (useSpan)
            {
                table.Add(bytes.AsSpan(), i + 1);
            }
            else
            {
                table.Add(bytes, i + 1);
            }

            expected[key] = i + 1;
        }

        foreach (var pair in expected.ToArray())
        {
            var bytes = Encoding.UTF8.GetBytes(pair.Key);
            Assert.True(table.ContainsKey(bytes));
            Assert.True(table.ContainsKey(bytes.AsSpan()));
            Assert.Equal(pair.Value, useSpan
                ? table.GetOrAdd(bytes.AsSpan(), _ => throw new Exception())
                : table.GetOrAdd(bytes, _ => throw new Exception()));
            if (pair.Value % 2 == 0)
            {
                Assert.True(useSpan ? table.TryRemove(bytes.AsSpan(), out var removed) : table.TryRemove(bytes, out removed));
                Assert.Equal(pair.Value, removed);
                Assert.False(table.TryRemove(bytes));
                Assert.False(table.TryRemove(bytes.AsSpan()));
                expected.Remove(pair.Key);
            }
            else
            {
                Assert.True(table.TryGetValue(bytes, out var actual));
                Assert.Equal(pair.Value, actual);
                Assert.True(table.TryGetValue(bytes.AsSpan(), out actual));
                Assert.Equal(pair.Value, actual);
            }
        }

        Assert.Equal(expected.Count, table.Count);
        Assert.Equal(expected.Values.Order(), table.ToArray().Order());
        Assert.Equal(expected.OrderBy(x => x.Key), table.ToKeyValuePairs().Select(x => KeyValuePair.Create(Encoding.UTF8.GetString(x.Key), x.Value)).OrderBy(x => x.Key));
        table.Clear();
        table.Clear();
        Assert.Empty(table.ToArray());
        Assert.False(table.TryGetValue("missing"u8.ToArray(), out _));
        Assert.False(table.TryGetValue("missing"u8, out _));
        Assert.Equal(7, table.GetOrAdd("new"u8, _ => 7));
        Assert.Equal(8, table.GetOrAdd("array"u8.ToArray(), _ => 8));
    }

    [Fact]
    public void SpanKeysAreCopiedAndFactoriesRunOnce()
    {
        var bytes = "original"u8.ToArray();
        var map = new Utf8UnorderedMap<int>();
        var table = new Utf8Hashtable<int>();
        map.Add(bytes.AsSpan(), 1);
        table.Add(bytes.AsSpan(), 2);
        bytes[0] = 0;
        Assert.True(map.TryGetValue("original"u8, out var value));
        Assert.Equal(1, value);
        Assert.True(table.TryGetValue("original"u8, out value));
        Assert.Equal(2, value);
        var calls = 0;
        Parallel.For(0, 100, _ => Assert.Equal(9, table.GetOrAdd("concurrent"u8, key =>
        {
            Interlocked.Increment(ref calls);
            return 9;
        })));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void InvalidArgumentsAreRejected()
    {
        var map = new Utf8UnorderedMap<int>();
        var table = new Utf8Hashtable<int>();
        Assert.Throws<ArgumentOutOfRangeException>(() => new Utf8UnorderedMap<int>(uint.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Utf8Hashtable<int>(-1));
        Assert.Throws<ArgumentNullException>(() => map.Add((byte[])null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.TryAdd((byte[])null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.Remove((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => map.ContainsKey((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => map.TryGetValue((byte[])null!, out _));
        Assert.Throws<ArgumentNullException>(() => map.GetValueRefOrAddDefault((byte[])null!, out _));
        Assert.Throws<ArgumentNullException>(() => table.Add((byte[])null!, 1));
        Assert.Throws<ArgumentNullException>(() => table.TryAdd((byte[])null!, 1));
        Assert.Throws<ArgumentNullException>(() => table.TryRemove((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => table.TryGetValue((byte[])null!, out _));
        Assert.Throws<ArgumentNullException>(() => table.GetOrAdd("key"u8, null!));
        Assert.Throws<ArgumentNullException>(() => table.GetOrAdd("key"u8.ToArray(), null!));
    }
}
