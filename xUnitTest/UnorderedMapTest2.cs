// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class UnorderedMapTest2
{
    [Fact]
    public void AddAndTryGetValue()
    {
        var map = new UnorderedMap<int, string>();

        var r1 = map.Add(1, "A");
        var r2 = map.Add(2, "B");
        var r3 = map.Add(3, "C");

        Assert.True(r1.NewlyAdded);
        Assert.True(r2.NewlyAdded);
        Assert.True(r3.NewlyAdded);
        Assert.Equal(3, map.Count);

        Assert.True(map.TryGetValue(1, out var v1));
        Assert.True(map.TryGetValue(2, out var v2));
        Assert.True(map.TryGetValue(3, out var v3));

        Assert.Equal("A", v1);
        Assert.Equal("B", v2);
        Assert.Equal("C", v3);
    }

    [Fact]
    public void AddDuplicateReturnsExistingNode()
    {
        var map = new UnorderedMap<int, string>();

        var first = map.Add(1, "A");
        var second = map.Add(1, "B");

        Assert.True(first.NewlyAdded);
        Assert.False(second.NewlyAdded);
        Assert.Equal(first.NodeIndex, second.NodeIndex);

        Assert.Equal(1, map.Count);
        Assert.Equal("A", map[1]);
    }

    [Fact]
    public void AllowDuplicateAddsMultipleNodes()
    {
        var map = new UnorderedMap<int, string>(allowDuplicate: true);

        var r1 = map.Add(1, "A");
        var r2 = map.Add(1, "B");
        var r3 = map.Add(1, "C");

        Assert.True(r1.NewlyAdded);
        Assert.True(r2.NewlyAdded);
        Assert.True(r3.NewlyAdded);
        Assert.Equal(3, map.Count);

        var values = map.EnumerateValue(1).OrderBy(x => x).ToArray();

        Assert.Equal(["A", "B", "C"], values);
    }

    [Fact]
    public void NullKey()
    {
        var map = new UnorderedMap<string?, int>();

        map.Add(null, 100);
        map.Add("A", 1);

        Assert.True(map.ContainsKey(null));
        Assert.True(map.TryGetValue(null, out var value));
        Assert.Equal(100, value);

        Assert.Equal(100, map[null]);
    }

    [Fact]
    public void NullKeyDuplicate()
    {
        var map = new UnorderedMap<string?, int>(allowDuplicate: true);

        map.Add(null, 1);
        map.Add(null, 2);
        map.Add(null, 3);

        Assert.Equal(3, map.Count);

        var values = map.EnumerateValue(null).OrderBy(x => x).ToArray();

        Assert.Equal([1, 2, 3], values);
    }

    [Fact]
    public void IndexerUpdatesExistingValue()
    {
        var map = new UnorderedMap<int, string>();

        map.Add(1, "A");
        map[1] = "B";

        Assert.Equal(1, map.Count);
        Assert.Equal("B", map[1]);
    }

    [Fact]
    public void IndexerAddsMissingValue()
    {
        var map = new UnorderedMap<int, string>();

        map[10] = "X";

        Assert.Equal(1, map.Count);
        Assert.Equal("X", map[10]);
    }

    [Fact]
    public void IndexerThrowsForMissingKey()
    {
        var map = new UnorderedMap<int, int>();

        Assert.Throws<KeyNotFoundException>(() => _ = map[123]);
    }

    [Fact]
    public void Contains()
    {
        var map = new UnorderedMap<int, string>();

        map.Add(1, "A");

        Assert.True(map.ContainsKey(1));
        Assert.False(map.ContainsKey(2));

        Assert.True(map.Contains(1, "A"));
        Assert.False(map.Contains(1, "B"));

        Assert.True(map.ContainsValue("A"));
        Assert.False(map.ContainsValue("B"));
    }

    [Fact]
    public void ContainsNullValue()
    {
        var map = new UnorderedMap<int, string?>();

        map.Add(1, null);
        map.Add(2, "A");

        Assert.True(map.ContainsValue(null));
        Assert.True(map.ContainsValue("A"));
        Assert.False(map.ContainsValue("B"));
    }

    [Fact]
    public void FindFirstNode()
    {
        var map = new UnorderedMap<int, string>();

        var added = map.Add(10, "A");

        Assert.Equal(added.NodeIndex, map.FindFirstNode(10));
        Assert.Equal(-1, map.FindFirstNode(20));
    }

    [Fact]
    public void FindNode()
    {
        var map = new UnorderedMap<int, string>(allowDuplicate: true);

        var a = map.Add(1, "A");
        var b = map.Add(1, "B");

        Assert.Equal(a.NodeIndex, map.FindNode(1, "A"));
        Assert.Equal(b.NodeIndex, map.FindNode(1, "B"));
        Assert.Equal(-1, map.FindNode(1, "C"));
    }

    [Fact]
    public void RemoveByKey()
    {
        var map = new UnorderedMap<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");

        Assert.True(map.Remove(1));
        Assert.False(map.Remove(1));

        Assert.Equal(1, map.Count);
        Assert.False(map.ContainsKey(1));
        Assert.True(map.ContainsKey(2));
    }

    [Fact]
    public void RemoveByKeyAndValue()
    {
        var map = new UnorderedMap<int, string>(allowDuplicate: true);

        map.Add(1, "A");
        map.Add(1, "B");

        Assert.True(map.Remove(1, "A"));
        Assert.False(map.Contains(1, "A"));
        Assert.True(map.Contains(1, "B"));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void RemoveNode()
    {
        var map = new UnorderedMap<int, string>();

        var a = map.Add(1, "A");
        map.Add(2, "B");

        map.RemoveNode(a.NodeIndex);

        Assert.Equal(1, map.Count);
        Assert.False(map.ContainsKey(1));
        Assert.True(map.ContainsKey(2));
    }

    [Fact]
    public void RemovedNodeIndexIsReused()
    {
        var map = new UnorderedMap<int, int>();

        var first = map.Add(1, 1);
        map.Add(2, 2);

        map.RemoveNode(first.NodeIndex);

        var added = map.Add(3, 3);

        Assert.Equal(first.NodeIndex, added.NodeIndex);
    }

    [Fact]
    public void SetNodeValue()
    {
        var map = new UnorderedMap<int, string>();

        var node = map.Add(1, "A");

        Assert.True(map.SetNodeValue(node.NodeIndex, "B"));
        Assert.Equal("B", map[1]);

        Assert.False(map.SetNodeValue(-1, "X"));
        Assert.False(map.SetNodeValue(100, "X"));
    }

    [Fact]
    public void SetNodeKeyPreservesNodeIndex()
    {
        var map = new UnorderedMap<int, string>();

        var node = map.Add(1, "A");

        Assert.True(map.SetNodeKey(node.NodeIndex, 100));

        Assert.False(map.ContainsKey(1));
        Assert.True(map.ContainsKey(100));
        Assert.Equal("A", map[100]);

        Assert.Equal(node.NodeIndex, map.FindFirstNode(100));
    }

    [Fact]
    public void SetNodeKeyRejectsDuplicate()
    {
        var map = new UnorderedMap<int, string>();

        var a = map.Add(1, "A");
        map.Add(2, "B");

        Assert.False(map.SetNodeKey(a.NodeIndex, 2));

        Assert.Equal("A", map[1]);
        Assert.Equal("B", map[2]);
        Assert.Equal(2, map.Count);
    }

    [Fact]
    public void SetNodeKeySupportsNull()
    {
        var map = new UnorderedMap<string?, int>();

        var node = map.Add("A", 1);

        Assert.True(map.SetNodeKey(node.NodeIndex, null));

        Assert.False(map.ContainsKey("A"));
        Assert.True(map.ContainsKey(null));
        Assert.Equal(1, map[null]);
    }

    [Fact]
    public void Clear()
    {
        var map = new UnorderedMap<int, int>();

        for (var i = 0; i < 100; i++)
        {
            map.Add(i, i);
        }

        map.Clear();

        Assert.Equal(0, map.Count);

        for (var i = 0; i < 100; i++)
        {
            Assert.False(map.ContainsKey(i));
        }

        map.Add(1000, 1000);

        Assert.Equal(1, map.Count);
        Assert.Equal(1000, map[1000]);
    }

    [Fact]
    public void ResizePreservesValuesAndNodeIndexes()
    {
        var map = new UnorderedMap<int, int>(4);
        var indexes = new int[1000];

        for (var i = 0; i < indexes.Length; i++)
        {
            indexes[i] = map.Add(i, i * 10).NodeIndex;
        }

        Assert.Equal(1000, map.Count);

        for (var i = 0; i < indexes.Length; i++)
        {
            Assert.Equal(i * 10, map[i]);
            Assert.Equal(indexes[i], map.FindFirstNode(i));
        }
    }

    [Fact]
    public void CustomComparer()
    {
        var map = new UnorderedMap<string, int>(
            StringComparer.OrdinalIgnoreCase);

        map.Add("ABC", 1);

        Assert.True(map.ContainsKey("abc"));
        Assert.Equal(1, map["AbC"]);

        var second = map.Add("abc", 2);

        Assert.False(second.NewlyAdded);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void EnumeratePairs()
    {
        var map = new UnorderedMap<int, string>();

        map.Add(5, "E");
        map.Add(1, "A");
        map.Add(3, "C");

        var result = map
            .OrderBy(x => x.Key)
            .ToArray();

        Assert.Equal(3, result.Length);
        Assert.Equal(1, result[0].Key);
        Assert.Equal("A", result[0].Value);
        Assert.Equal(3, result[1].Key);
        Assert.Equal("C", result[1].Value);
        Assert.Equal(5, result[2].Key);
        Assert.Equal("E", result[2].Value);
    }

    [Fact]
    public void EnumerateKeysAndValues()
    {
        var map = new UnorderedMap<int?, int>();

        map.Add(null, 999);
        map.Add(0, 0);
        map.Add(1, 1);
        map.Add(2, 2);
        map.Add(5, 5);

        Assert.True(
            map.Keys
                .OrderBy(x => x)
                .SequenceEqual(new int?[] { null, 0, 1, 2, 5 }));

        Assert.True(
            map.OrderBy(x => x.Key)
                .Select(x => x.Value)
                .SequenceEqual(new[] { 999, 0, 1, 2, 5 }));
    }

    [Fact]
    public void EnumeratorDetectsModification()
    {
        var map = new UnorderedMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Add(3, 3);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void KeyEnumeratorDetectsModification()
    {
        var map = new UnorderedMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.Keys.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Remove(1);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void ValueEnumeratorDetectsValueUpdate()
    {
        var map = new UnorderedMap<int, int>();

        var node = map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.Values.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.SetNodeValue(node.NodeIndex, 100);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
}
