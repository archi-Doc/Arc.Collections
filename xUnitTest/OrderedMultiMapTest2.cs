// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

public class OrderedMultiMapTest2
{
    [Fact]
    public void AddAndGet()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(3, "C");
        map.Add(1, "A");
        map.Add(2, "B");

        Assert.Equal(3, map.Count);

        Assert.Equal("A", map[1]);
        Assert.Equal("B", map[2]);
        Assert.Equal("C", map[3]);

        Assert.True(map.ContainsKey(1));
        Assert.True(map.ContainsKey(2));
        Assert.True(map.ContainsKey(3));
        Assert.False(map.ContainsKey(4));
    }

    [Fact]
    public void DuplicateKeys()
    {
        var map = new OrderedMultiMap<int, string>();

        var a = map.Add(1, "A");
        var b = map.Add(1, "B");
        var c = map.Add(1, "C");

        Assert.True(a.NewlyAdded);
        Assert.True(b.NewlyAdded);
        Assert.True(c.NewlyAdded);

        Assert.Equal(3, map.Count);

        Assert.Equal(
            ["A", "B", "C"],
            map.EnumerateValue(1).ToArray());
    }

    [Fact]
    public void IndexerReturnsFirstValue()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");

        Assert.Equal("A", map[1]);
    }

    [Fact]
    public void IndexerSetterAddsDuplicate()
    {
        var map = new OrderedMultiMap<int, string>();

        map[1] = "A";
        map[1] = "B";

        Assert.Equal(2, map.Count);

        Assert.Equal(
            ["A", "B"],
            map.EnumerateValue(1).ToArray());
    }

    [Fact]
    public void IndexerThrowsForMissingKey()
    {
        var map = new OrderedMultiMap<int, int>();

        Assert.Throws<KeyNotFoundException>(() => _ = map[123]);
    }

    [Fact]
    public void TryGetValueReturnsFirstValue()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");

        Assert.True(map.TryGetValue(1, out var value));
        Assert.Equal("A", value);

        Assert.False(map.TryGetValue(2, out _));
    }

    [Fact]
    public void ContainsValue()
    {
        var map = new OrderedMultiMap<int, string?>();

        map.Add(1, "A");
        map.Add(2, null);
        map.Add(3, "C");

        Assert.True(map.ContainsValue("A"));
        Assert.True(map.ContainsValue(null));
        Assert.True(map.ContainsValue("C"));
        Assert.False(map.ContainsValue("B"));
    }

    [Fact]
    public void EnumerationIsOrdered()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(3, "C1");
        map.Add(1, "A1");
        map.Add(2, "B1");
        map.Add(1, "A2");
        map.Add(3, "C2");

        var result = map.ToArray();

        Assert.Equal(5, result.Length);

        Assert.Equal(1, result[0].Key);
        Assert.Equal("A1", result[0].Value);

        Assert.Equal(1, result[1].Key);
        Assert.Equal("A2", result[1].Value);

        Assert.Equal(2, result[2].Key);
        Assert.Equal("B1", result[2].Value);

        Assert.Equal(3, result[3].Key);
        Assert.Equal("C1", result[3].Value);

        Assert.Equal(3, result[4].Key);
        Assert.Equal("C2", result[4].Value);
    }

    [Fact]
    public void ReverseEnumeration()
    {
        var map = new OrderedMultiMap<int, int>(reverse: true);

        map.Add(1, 1);
        map.Add(3, 3);
        map.Add(2, 2);
        map.Add(3, 30);

        Assert.Equal(
            [3, 3, 2, 1],
            map.Keys.ToArray());

        Assert.Equal(
            [3, 30, 2, 1],
            map.Values.ToArray());
    }

    [Fact]
    public void KeysAndValues()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(3, "C");
        map.Add(1, "A");
        map.Add(2, "B");
        map.Add(2, "B2");

        Assert.Equal(
            [1, 2, 2, 3],
            map.Keys.ToArray());

        Assert.Equal(
            ["A", "B", "B2", "C"],
            map.Values.ToArray());
    }

    [Fact]
    public void FirstAndLast()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(2, "B");
        map.Add(1, "A");
        map.Add(3, "C1");
        map.Add(3, "C2");

        Assert.NotNull(map.First);
        Assert.NotNull(map.Last);

        Assert.Equal(1, map.First!.Key);
        Assert.Equal("A", map.First.Value);

        Assert.Equal(3, map.Last!.Key);
        Assert.Equal("C2", map.Last.Value);
    }

    [Fact]
    public void PreviousAndNext()
    {
        var map = new OrderedMultiMap<int, string>();

        var a = map.Add(1, "A").Node;
        var b1 = map.Add(2, "B1").Node;
        var b2 = map.Add(2, "B2").Node;
        var c = map.Add(3, "C").Node;

        Assert.Same(a, b1.Previous);
        Assert.Same(b2, b1.Next);

        Assert.Same(b1, b2.Previous);
        Assert.Same(c, b2.Next);

        Assert.Null(a.Previous);
        Assert.Null(c.Next);
    }

    [Fact]
    public void FindNode()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");

        var node = map.FindNode(1, "B");

        Assert.NotNull(node);
        Assert.Equal(1, node!.Key);
        Assert.Equal("B", node.Value);

        Assert.Null(map.FindNode(1, "X"));
        Assert.Null(map.FindNode(2, "B"));
    }

    [Fact]
    public void RemoveByKeyRemovesFirstValue()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");

        Assert.True(map.Remove(1));

        Assert.Equal(2, map.Count);

        Assert.Equal(
            ["B", "C"],
            map.EnumerateValue(1).ToArray());
    }

    [Fact]
    public void RemoveByKeyAndValue()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");

        Assert.True(map.Remove(1, "B"));
        Assert.False(map.Remove(1, "B"));

        Assert.Equal(
            ["A", "C"],
            map.EnumerateValue(1).ToArray());
    }

    [Fact]
    public void RemoveDuplicateHead()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");
        map.Add(2, "D");

        Assert.True(map.Remove(1, "A"));

        Assert.Equal(3, map.Count);
        Assert.Equal("B", map[1]);

        Assert.Equal(
            ["B", "C"],
            map.EnumerateValue(1).ToArray());

        Assert.Equal(
            [1, 1, 2],
            map.Keys.ToArray());
    }

    [Fact]
    public void RemoveDuplicateUntilSingleNode()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(1, "C");

        Assert.True(map.Remove(1, "B"));
        Assert.True(map.Remove(1, "C"));

        Assert.Equal(1, map.Count);
        Assert.Equal("A", map[1]);

        Assert.Equal(
            ["A"],
            map.EnumerateValue(1).ToArray());
    }

    [Fact]
    public void RemoveAll()
    {
        var map = new OrderedMultiMap<int, int>();

        const int count = 100;

        for (var i = 0; i < count; i++)
        {
            map.Add(i, i);
            map.Add(i, i + 1000);
        }

        Assert.Equal(count * 2, map.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(map.Remove(i));
            Assert.True(map.Remove(i));
            Assert.False(map.Remove(i));
        }

        Assert.Equal(0, map.Count);
        Assert.Null(map.First);
        Assert.Null(map.Last);
    }

    [Fact]
    public void RemoveTreeNodesInMixedOrder()
    {
        var map = new OrderedMultiMap<int, int>();

        var values = new[]
        {
            10, 5, 15, 3, 7, 13, 17, 1, 4, 6,
            8, 11, 14, 16, 19, 0, 2, 9, 12, 18,
        };

        foreach (var value in values)
        {
            map.Add(value, value);
        }

        foreach (var value in values)
        {
            Assert.True(map.Remove(value));

            var keys = map.Keys.ToArray();

            for (var i = 1; i < keys.Length; i++)
            {
                Assert.True(keys[i - 1] < keys[i]);
            }
        }

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void SetNodeValue()
    {
        var map = new OrderedMultiMap<int, string>();

        var node = map.Add(1, "A").Node;

        map.SetNodeValue(node, "B");

        Assert.Equal("B", map[1]);
    }

    [Fact]
    public void SetNodeKeyForSingleNode()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        var node = map.Add(3, "C").Node;
        map.Add(5, "E");

        Assert.True(map.SetNodeKey(node, 4));

        Assert.False(map.ContainsKey(3));
        Assert.True(map.ContainsKey(4));
        Assert.Equal("C", map[4]);

        Assert.Same(node, map.FindFirstNode(4));

        Assert.Equal(
            [1, 4, 5],
            map.Keys.ToArray());
    }

    [Fact]
    public void SetNodeKeyMovesDuplicateNodeToAnotherKey()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(1, "A");
        var node = map.Add(1, "B").Node;
        map.Add(2, "C");

        Assert.True(map.SetNodeKey(node, 3));

        Assert.Equal(
            ["A"],
            map.EnumerateValue(1).ToArray());

        Assert.Equal("B", map[3]);

        Assert.Equal(
            [1, 2, 3],
            map.Keys.ToArray());
    }

    [Fact]
    public void SetNodeKeyCanJoinExistingDuplicateGroup()
    {
        var map = new OrderedMultiMap<int, string>();

        var node = map.Add(1, "A").Node;

        map.Add(2, "B");
        map.Add(2, "C");

        Assert.True(map.SetNodeKey(node, 2));

        Assert.Equal(3, map.Count);
        Assert.False(map.ContainsKey(1));

        Assert.Equal(
            ["B", "C", "A"],
            map.EnumerateValue(2).ToArray());
    }

    [Fact]
    public void SetNodeKeySameKeyReturnsFalse()
    {
        var map = new OrderedMultiMap<int, string>();

        var node = map.Add(1, "A").Node;

        Assert.False(map.SetNodeKey(node, 1));
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void ReuseRemovedNode()
    {
        var map = new OrderedMultiMap<int, string>();

        var node = map.Add(1, "A").Node;

        map.RemoveNode(node);

        var result = map.Add(2, "B", node);

        Assert.True(result.NewlyAdded);
        Assert.Same(node, result.Node);

        Assert.Equal(2, node.Key);
        Assert.Equal("B", node.Value);
        Assert.Equal("B", map[2]);
    }

    [Fact]
    public void LowerAndUpperBound()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(10, "A");
        map.Add(20, "B1");
        map.Add(20, "B2");
        map.Add(30, "C");

        Assert.Equal(20, map.GetLowerBound(15)!.Key);
        Assert.Equal(10, map.GetUpperBound(15)!.Key);

        Assert.Equal("B1", map.GetLowerBound(20)!.Value);
        Assert.Equal("B2", map.GetUpperBound(20)!.Value);

        Assert.Equal(10, map.GetLowerBound(1)!.Key);
        Assert.Null(map.GetUpperBound(1));

        Assert.Null(map.GetLowerBound(100));
        Assert.Equal(30, map.GetUpperBound(100)!.Key);
    }

    [Fact]
    public void GetRange()
    {
        var map = new OrderedMultiMap<int, int>();

        for (var i = 0; i <= 10; i++)
        {
            map.Add(i, i);
        }

        map.Add(5, 50);

        var range = map.GetRange(3, 7);

        Assert.NotNull(range.Lower);
        Assert.NotNull(range.Upper);

        Assert.Equal(3, range.Lower!.Key);
        Assert.Equal(7, range.Upper!.Key);
    }

    [Fact]
    public void Clear()
    {
        var map = new OrderedMultiMap<int, int>();

        for (var i = 0; i < 100; i++)
        {
            map.Add(i, i);
            map.Add(i, i + 100);
        }

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.Null(map.First);
        Assert.Null(map.Last);

        map.Add(1000, 1000);

        Assert.Equal(1, map.Count);
        Assert.Equal(1000, map[1000]);
    }

    [Fact]
    public void CopyTo()
    {
        var map = new OrderedMultiMap<int, string>();

        map.Add(2, "B");
        map.Add(1, "A");
        map.Add(2, "B2");

        var array = new KeyValuePair<int, string>[5];

        map.CopyTo(array, 1);

        Assert.Equal(default, array[0]);

        Assert.Equal(1, array[1].Key);
        Assert.Equal("A", array[1].Value);

        Assert.Equal(2, array[2].Key);
        Assert.Equal("B", array[2].Value);

        Assert.Equal(2, array[3].Key);
        Assert.Equal("B2", array[3].Value);
    }

    [Fact]
    public void EnumeratorDetectsModification()
    {
        var map = new OrderedMultiMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Add(3, 3);

        Assert.Throws<InvalidOperationException>(
            () => enumerator.MoveNext());
    }

    [Fact]
    public void KeyEnumeratorDetectsModification()
    {
        var map = new OrderedMultiMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.Keys.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Remove(2);

        Assert.Throws<InvalidOperationException>(
            () => enumerator.MoveNext());
    }

    [Fact]
    public void EnumeratorDetectsClear()
    {
        var map = new OrderedMultiMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Clear();

        Assert.Throws<InvalidOperationException>(
            () => enumerator.MoveNext());
    }

    [Fact]
    public void ManyDuplicates()
    {
        var map = new OrderedMultiMap<int, int>();

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            map.Add(1, i);
        }

        Assert.Equal(count, map.Count);

        Assert.Equal(
            Enumerable.Range(0, count),
            map.EnumerateValue(1));
    }

    [Fact]
    public void ManyMixedItemsRemainOrdered()
    {
        var map = new OrderedMultiMap<int, int>();
        var random = new Random(12345);

        for (var i = 0; i < 5000; i++)
        {
            var key = random.Next(0, 200);
            map.Add(key, i);
        }

        var previous = int.MinValue;
        var count = 0;

        foreach (var pair in map)
        {
            Assert.True(pair.Key >= previous);

            previous = pair.Key;
            count++;
        }

        Assert.Equal(map.Count, count);
    }
}
