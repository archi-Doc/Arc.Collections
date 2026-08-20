// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class Utf16UnorderedMapTest
{
    [Fact]
    public void AddAndTryGetValue()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        Assert.Equal(3, map.Count);

        Assert.True(map.TryGetValue("A", out var a));
        Assert.True(map.TryGetValue("B", out var b));
        Assert.True(map.TryGetValue("C", out var c));

        Assert.Equal(1, a);
        Assert.Equal(2, b);
        Assert.Equal(3, c);

        Assert.False(map.TryGetValue("D", out _));
    }

    [Fact]
    public void AddOverwritesExistingValue()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("A", 2);

        Assert.Equal(1, map.Count);
        Assert.Equal(2, map["A"]);
    }

    [Fact]
    public void TryAddDoesNotOverwriteExistingValue()
    {
        var map = new Utf16UnorderedMap<int>();

        Assert.True(map.TryAdd("A", 1));
        Assert.False(map.TryAdd("A", 2));

        Assert.Equal(1, map.Count);
        Assert.Equal(1, map["A"]);
    }

    [Fact]
    public void ReadOnlySpanKey()
    {
        var map = new Utf16UnorderedMap<int>();

        ReadOnlySpan<char> key = "Alpha";

        Assert.True(map.TryAdd(key, 123));

        Assert.True(map.TryGetValue("Alpha", out var value1));
        Assert.True(map.TryGetValue("Alpha".AsSpan(), out var value2));

        Assert.Equal(123, value1);
        Assert.Equal(123, value2);
    }

    [Fact]
    public void StringAndSpanKeysAreEquivalent()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("Alpha", 1);

        Assert.False(map.TryAdd("Alpha".AsSpan(), 2));
        Assert.Equal(1, map.Count);
        Assert.Equal(1, map["Alpha"]);
    }

    [Fact]
    public void Indexer()
    {
        var map = new Utf16UnorderedMap<int>();

        map["A"] = 1;
        Assert.Equal(1, map["A"]);

        map["A"] = 10;
        Assert.Equal(10, map["A"]);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void IndexerThrowsWhenKeyDoesNotExist()
    {
        var map = new Utf16UnorderedMap<int>();

        Assert.Throws<KeyNotFoundException>(() => _ = map["A"]);
    }

    [Fact]
    public void ContainsKey()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("Alpha", 1);

        Assert.True(map.ContainsKey("Alpha"));
        Assert.True(map.ContainsKey("Alpha".AsSpan()));

        Assert.False(map.ContainsKey("Beta"));
        Assert.False(map.ContainsKey("Beta".AsSpan()));
    }

    [Fact]
    public void ContainsValue()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 10);
        map.Add("B", 20);
        map.Add("C", 30);

        Assert.True(map.ContainsValue(10));
        Assert.True(map.ContainsValue(30));
        Assert.False(map.ContainsValue(40));
    }

    [Fact]
    public void ContainsNullValue()
    {
        var map = new Utf16UnorderedMap<string?>();

        map.Add("A", "Value");
        map.Add("B", null);

        Assert.True(map.ContainsValue("Value"));
        Assert.True(map.ContainsValue(null));
        Assert.False(map.ContainsValue("Other"));
    }

    [Fact]
    public void Remove()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        Assert.True(map.Remove("B"));

        Assert.Equal(2, map.Count);
        Assert.False(map.ContainsKey("B"));
        Assert.True(map.ContainsKey("A"));
        Assert.True(map.ContainsKey("C"));

        Assert.False(map.Remove("B"));
    }

    [Fact]
    public void RemoveWithSpan()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("Alpha", 1);

        Assert.True(map.Remove("Alpha".AsSpan()));
        Assert.False(map.ContainsKey("Alpha"));
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void RemovedSlotCanBeReused()
    {
        var map = new Utf16UnorderedMap<int>(4);

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        var capacity = map.Capacity;

        Assert.True(map.Remove("B"));

        map.Add("D", 4);

        Assert.Equal(3, map.Count);
        Assert.Equal(capacity, map.Capacity);

        Assert.True(map.TryGetValue("A", out var a));
        Assert.True(map.TryGetValue("C", out var c));
        Assert.True(map.TryGetValue("D", out var d));

        Assert.Equal(1, a);
        Assert.Equal(3, c);
        Assert.Equal(4, d);
    }

    [Fact]
    public void Clear()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.False(map.ContainsKey("A"));
        Assert.False(map.ContainsKey("B"));
        Assert.False(map.ContainsKey("C"));

        // The map remains usable after Clear.
        map.Add("D", 4);

        Assert.Equal(1, map.Count);
        Assert.Equal(4, map["D"]);
    }

    [Fact]
    public void ResizePreservesItems()
    {
        var map = new Utf16UnorderedMap<int>(4);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            map.Add($"Key-{i}", i);
        }

        Assert.Equal(count, map.Count);
        Assert.True(map.Capacity >= count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(map.TryGetValue($"Key-{i}", out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void EmptyKey()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add(string.Empty, 123);

        Assert.True(map.TryGetValue(string.Empty, out var value1));
        Assert.True(map.TryGetValue(ReadOnlySpan<char>.Empty, out var value2));

        Assert.Equal(123, value1);
        Assert.Equal(123, value2);
    }

    [Fact]
    public void KeysAreCaseSensitive()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("ABC", 1);
        map.Add("abc", 2);

        Assert.Equal(2, map.Count);
        Assert.Equal(1, map["ABC"]);
        Assert.Equal(2, map["abc"]);
    }

    [Fact]
    public void Enumerate()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        var result = new Dictionary<string, int>();

        foreach (var pair in map)
        {
            result.Add(pair.Key, pair.Value);
        }

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result["A"]);
        Assert.Equal(2, result["B"]);
        Assert.Equal(3, result["C"]);
    }

    [Fact]
    public void EnumerationSkipsRemovedNodes()
    {
        var map = new Utf16UnorderedMap<int>();

        map.Add("A", 1);
        map.Add("B", 2);
        map.Add("C", 3);

        map.Remove("B");

        var result = new Dictionary<string, int>();

        foreach (var pair in map)
        {
            result.Add(pair.Key, pair.Value);
        }

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["A"]);
        Assert.Equal(3, result["C"]);
        Assert.False(result.ContainsKey("B"));
    }

    [Fact]
    public void NullStringKeyThrows()
    {
        var map = new Utf16UnorderedMap<int>();

        Assert.Throws<ArgumentNullException>(() => map.Add(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.TryAdd(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.ContainsKey((string)null!));
        Assert.Throws<ArgumentNullException>(() => map.TryGetValue((string)null!, out _));
        Assert.Throws<ArgumentNullException>(() => map.Remove((string)null!));
    }
}
