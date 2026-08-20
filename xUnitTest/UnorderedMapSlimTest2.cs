// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class UnorderedMapSlimTest2
{
    [Fact]
    public void AddAndTryGetValue()
    {
        var map = new UnorderedMapSlim<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");
        map.Add(3, "C");

        Assert.Equal(3, map.Count);

        Assert.True(map.TryGetValue(1, out var a));
        Assert.True(map.TryGetValue(2, out var b));
        Assert.True(map.TryGetValue(3, out var c));

        Assert.Equal("A", a);
        Assert.Equal("B", b);
        Assert.Equal("C", c);

        Assert.False(map.TryGetValue(4, out _));
    }

    [Fact]
    public void AddUpdatesExistingValue()
    {
        var map = new UnorderedMapSlim<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");

        Assert.Equal(1, map.Count);
        Assert.Equal("B", map[1]);
    }

    [Fact]
    public void TryAddDoesNotOverwriteExistingValue()
    {
        var map = new UnorderedMapSlim<int, string>();

        Assert.True(map.TryAdd(1, "A"));
        Assert.False(map.TryAdd(1, "B"));

        Assert.Equal(1, map.Count);
        Assert.Equal("A", map[1]);
    }

    [Fact]
    public void Indexer()
    {
        var map = new UnorderedMapSlim<int, string>();

        map[1] = "A";

        Assert.Equal("A", map[1]);

        map[1] = "B";

        Assert.Equal("B", map[1]);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void IndexerThrowsWhenKeyDoesNotExist()
    {
        var map = new UnorderedMapSlim<int, int>();

        Assert.Throws<KeyNotFoundException>(() => _ = map[123]);
    }

    [Fact]
    public void ContainsKey()
    {
        var map = new UnorderedMapSlim<int, int>();

        map.Add(1, 10);
        map.Add(2, 20);

        Assert.True(map.ContainsKey(1));
        Assert.True(map.ContainsKey(2));
        Assert.False(map.ContainsKey(3));
    }

    [Fact]
    public void ContainsValue()
    {
        var map = new UnorderedMapSlim<int, int>();

        map.Add(1, 10);
        map.Add(2, 20);

        Assert.True(map.ContainsValue(10));
        Assert.True(map.ContainsValue(20));
        Assert.False(map.ContainsValue(30));
    }

    [Fact]
    public void ContainsNullValue()
    {
        var map = new UnorderedMapSlim<int, string?>();

        map.Add(1, null);
        map.Add(2, "A");

        Assert.True(map.ContainsValue(null));
        Assert.True(map.ContainsValue("A"));
        Assert.False(map.ContainsValue("B"));
    }

    [Fact]
    public void Remove()
    {
        var map = new UnorderedMapSlim<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");
        map.Add(3, "C");

        Assert.True(map.Remove(2));

        Assert.Equal(2, map.Count);
        Assert.True(map.ContainsKey(1));
        Assert.False(map.ContainsKey(2));
        Assert.True(map.ContainsKey(3));

        Assert.False(map.Remove(2));
    }

    [Fact]
    public void RemovedSlotIsReused()
    {
        var map = new UnorderedMapSlim<int, int>(4);

        map.Add(1, 1);
        map.Add(2, 2);
        map.Add(3, 3);
        map.Add(4, 4);

        var capacity = map.Capacity;

        Assert.True(map.Remove(2));
        Assert.Equal(3, map.Count);

        map.Add(5, 5);

        Assert.Equal(4, map.Count);
        Assert.Equal(capacity, map.Capacity);

        Assert.True(map.ContainsKey(5));
        Assert.Equal(5, map[5]);
    }

    [Fact]
    public void Clear()
    {
        var map = new UnorderedMapSlim<int, int>();

        map.Add(1, 10);
        map.Add(2, 20);
        map.Add(3, 30);

        map.Clear();

        Assert.Equal(0, map.Count);

        Assert.False(map.ContainsKey(1));
        Assert.False(map.ContainsKey(2));
        Assert.False(map.ContainsKey(3));

        map.Add(4, 40);

        Assert.Equal(1, map.Count);
        Assert.Equal(40, map[4]);
    }

    [Fact]
    public void ResizePreservesItems()
    {
        var map = new UnorderedMapSlim<int, int>(4);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            map.Add(i, i * 10);
        }

        Assert.Equal(count, map.Count);
        Assert.True(map.Capacity >= count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(map.TryGetValue(i, out var value));
            Assert.Equal(i * 10, value);
        }
    }

    [Fact]
    public void RemoveAndResizePreserveItems()
    {
        var map = new UnorderedMapSlim<int, int>(4);

        for (var i = 0; i < 100; i++)
        {
            map.Add(i, i);
        }

        for (var i = 0; i < 100; i += 2)
        {
            Assert.True(map.Remove(i));
        }

        for (var i = 100; i < 1000; i++)
        {
            map.Add(i, i);
        }

        Assert.Equal(950, map.Count);

        for (var i = 1; i < 100; i += 2)
        {
            Assert.Equal(i, map[i]);
        }

        for (var i = 100; i < 1000; i++)
        {
            Assert.Equal(i, map[i]);
        }

        for (var i = 0; i < 100; i += 2)
        {
            Assert.False(map.ContainsKey(i));
        }
    }

    [Fact]
    public void StringKey()
    {
        var map = new UnorderedMapSlim<string, int>();

        map.Add("Alpha", 1);
        map.Add("Beta", 2);

        Assert.True(map.ContainsKey("Alpha"));
        Assert.True(map.ContainsKey("Beta"));

        Assert.Equal(1, map["Alpha"]);
        Assert.Equal(2, map["Beta"]);
    }

    [Fact]
    public void KeysAreCaseSensitive()
    {
        var map = new UnorderedMapSlim<string, int>();

        map.Add("ABC", 1);
        map.Add("abc", 2);

        Assert.Equal(2, map.Count);
        Assert.Equal(1, map["ABC"]);
        Assert.Equal(2, map["abc"]);
    }

    [Fact]
    public void NullKeyThrows()
    {
        var map = new UnorderedMapSlim<string, int>();

        Assert.Throws<ArgumentNullException>(() => map.Add(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.TryAdd(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.ContainsKey(null!));
        Assert.Throws<ArgumentNullException>(() => map.TryGetValue(null!, out _));
        Assert.Throws<ArgumentNullException>(() => map.Remove(null!));
    }

    [Fact]
    public void Foreach()
    {
        var map = new UnorderedMapSlim<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");
        map.Add(3, "C");

        var result = new Dictionary<int, string>();

        foreach (var pair in map)
        {
            result.Add(pair.Key, pair.Value);
        }

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[1]);
        Assert.Equal("B", result[2]);
        Assert.Equal("C", result[3]);
    }

    [Fact]
    public void EnumerationSkipsRemovedNodes()
    {
        var map = new UnorderedMapSlim<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");
        map.Add(3, "C");

        map.Remove(2);

        var result = map
            .OrderBy(x => x.Key)
            .ToArray();

        Assert.Equal(2, result.Length);

        Assert.Equal(1, result[0].Key);
        Assert.Equal("A", result[0].Value);

        Assert.Equal(3, result[1].Key);
        Assert.Equal("C", result[1].Value);
    }

    [Fact]
    public void Linq()
    {
        var map = new UnorderedMapSlim<int, int>();

        map.Add(5, 50);
        map.Add(1, 10);
        map.Add(3, 30);

        var values = map
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToArray();

        Assert.Equal([10, 30, 50], values);
    }

    [Fact]
    public void CapacityGrows()
    {
        var map = new UnorderedMapSlim<int, int>(8);

        Assert.Equal(8, map.Capacity);

        for (var i = 0; i < 9; i++)
        {
            map.Add(i, i);
        }

        Assert.True(map.Capacity > 4);
    }

    [Fact]
    public void ClearPreservesCapacity()
    {
        var map = new UnorderedMapSlim<int, int>(4);

        for (var i = 0; i < 100; i++)
        {
            map.Add(i, i);
        }

        var capacity = map.Capacity;

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.Equal(capacity, map.Capacity);
    }
}
