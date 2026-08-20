// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class OrderedMapTest2
{
    [Fact]
    public void AddAndGet()
    {
        var map = new OrderedMap<int, string>();

        var a = map.Add(3, "C");
        var b = map.Add(1, "A");
        var c = map.Add(2, "B");

        Assert.True(a.NewlyAdded);
        Assert.True(b.NewlyAdded);
        Assert.True(c.NewlyAdded);

        Assert.Equal(3, map.Count);
        Assert.Equal("A", map[1]);
        Assert.Equal("B", map[2]);
        Assert.Equal("C", map[3]);
        Assert.True(map.Validate());
    }

    [Fact]
    public void AddDuplicateReturnsExistingNode()
    {
        var map = new OrderedMap<int, string>();

        var first = map.Add(1, "A");
        var second = map.Add(1, "B");

        Assert.True(first.NewlyAdded);
        Assert.False(second.NewlyAdded);
        Assert.Same(first.Node, second.Node);

        Assert.Equal(1, map.Count);
        Assert.Equal("A", map[1]);
        Assert.True(map.Validate());
    }

    [Fact]
    public void IndexerAddsAndUpdates()
    {
        var map = new OrderedMap<int, string>();

        map[1] = "A";

        Assert.Equal(1, map.Count);
        Assert.Equal("A", map[1]);

        map[1] = "B";

        Assert.Equal(1, map.Count);
        Assert.Equal("B", map[1]);
        Assert.True(map.Validate());
    }

    [Fact]
    public void IndexerThrowsForMissingKey()
    {
        var map = new OrderedMap<int, int>();

        Assert.Throws<KeyNotFoundException>(() => _ = map[123]);
    }

    [Fact]
    public void ContainsAndTryGetValue()
    {
        var map = new OrderedMap<int, string>();

        map.Add(1, "A");
        map.Add(2, "B");

        Assert.True(map.ContainsKey(1));
        Assert.True(map.ContainsKey(2));
        Assert.False(map.ContainsKey(3));

        Assert.True(map.TryGetValue(1, out var value));
        Assert.Equal("A", value);

        Assert.False(map.TryGetValue(3, out _));

        Assert.True(map.ContainsValue("A"));
        Assert.True(map.ContainsValue("B"));
        Assert.False(map.ContainsValue("C"));
    }

    [Fact]
    public void ContainsNullValue()
    {
        var map = new OrderedMap<int, string?>();

        map.Add(1, null);
        map.Add(2, "A");

        Assert.True(map.ContainsValue(null));
        Assert.True(map.ContainsValue("A"));
        Assert.False(map.ContainsValue("B"));
    }

    [Fact]
    public void EnumeratesInAscendingOrder()
    {
        var map = new OrderedMap<int, string>();

        map.Add(5, "E");
        map.Add(1, "A");
        map.Add(3, "C");
        map.Add(2, "B");
        map.Add(4, "D");

        Assert.Equal(
            [1, 2, 3, 4, 5],
            map.Select(x => x.Key).ToArray());

        Assert.Equal(
            ["A", "B", "C", "D", "E"],
            map.Select(x => x.Value).ToArray());
    }

    [Fact]
    public void EnumeratesInDescendingOrder()
    {
        var map = new OrderedMap<int, int>(reverse: true);

        for (var i = 1; i <= 5; i++)
        {
            map.Add(i, i);
        }

        Assert.Equal(
            [5, 4, 3, 2, 1],
            map.Select(x => x.Key).ToArray());

        Assert.True(map.Validate());
    }

    [Fact]
    public void KeysAndValues()
    {
        var map = new OrderedMap<int, string>();

        map.Add(3, "C");
        map.Add(1, "A");
        map.Add(2, "B");

        Assert.Equal(
            [1, 2, 3],
            map.Keys.ToArray());

        Assert.Equal(
            ["A", "B", "C"],
            map.Values.ToArray());
    }

    [Fact]
    public void FirstAndLast()
    {
        var map = new OrderedMap<int, int>();

        map.Add(30, 30);
        map.Add(10, 10);
        map.Add(20, 20);
        map.Add(40, 40);

        Assert.Equal(10, map.First!.Key);
        Assert.Equal(40, map.Last!.Key);
    }

    [Fact]
    public void PreviousAndNext()
    {
        var map = new OrderedMap<int, int>();

        for (var i = 1; i <= 5; i++)
        {
            map.Add(i, i);
        }

        var node = map.FindNode(3);

        Assert.NotNull(node);
        Assert.Equal(2, node!.Previous!.Key);
        Assert.Equal(4, node.Next!.Key);

        Assert.Null(map.First!.Previous);
        Assert.Null(map.Last!.Next);
    }

    [Fact]
    public void Remove()
    {
        var map = new OrderedMap<int, int>();

        for (var i = 0; i < 10; i++)
        {
            map.Add(i, i);
        }

        Assert.True(map.Remove(5));
        Assert.False(map.Remove(5));

        Assert.Equal(9, map.Count);
        Assert.False(map.ContainsKey(5));
        Assert.True(map.Validate());

        Assert.Equal(
            [0, 1, 2, 3, 4, 6, 7, 8, 9],
            map.Select(x => x.Key).ToArray());
    }

    [Fact]
    public void RemoveAll()
    {
        var map = new OrderedMap<int, int>();

        const int count = 100;

        for (var i = 0; i < count; i++)
        {
            map.Add(i, i);
        }

        for (var i = 0; i < count; i++)
        {
            Assert.True(map.Remove(i));
            Assert.True(map.Validate());
        }

        Assert.Equal(0, map.Count);
        Assert.Null(map.First);
        Assert.Null(map.Last);
    }

    [Fact]
    public void RemoveInReverseOrder()
    {
        var map = new OrderedMap<int, int>();

        const int count = 100;

        for (var i = 0; i < count; i++)
        {
            map.Add(i, i);
        }

        for (var i = count - 1; i >= 0; i--)
        {
            Assert.True(map.Remove(i));
            Assert.True(map.Validate());
        }

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void RemoveMixedOrder()
    {
        var map = new OrderedMap<int, int>();

        var order = new[]
        {
            10, 5, 15, 3, 7, 13, 17, 1, 4, 6, 8, 11, 14, 16, 19,
            0, 2, 9, 12, 18,
        };

        foreach (var value in order)
        {
            map.Add(value, value);
            Assert.True(map.Validate());
        }

        foreach (var value in order)
        {
            Assert.True(map.Remove(value));
            Assert.True(map.Validate());
        }

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void SetNodeValue()
    {
        var map = new OrderedMap<int, string>();

        var node = map.Add(1, "A").Node;

        map.SetNodeValue(node, "B");

        Assert.Equal("B", map[1]);
        Assert.True(map.Validate());
    }

    [Fact]
    public void SetNodeKeyWithoutTreeReinsert()
    {
        var map = new OrderedMap<int, string>();

        map.Add(1, "A");
        var node = map.Add(3, "C").Node;
        map.Add(5, "E");

        Assert.True(map.SetNodeKey(node, 4));

        Assert.False(map.ContainsKey(3));
        Assert.True(map.ContainsKey(4));
        Assert.Equal("C", map[4]);

        Assert.Same(node, map.FindNode(4));
        Assert.True(map.Validate());
    }

    [Fact]
    public void SetNodeKeyWithTreeReinsert()
    {
        var map = new OrderedMap<int, string>();

        var node = map.Add(1, "A").Node;
        map.Add(2, "B");
        map.Add(3, "C");
        map.Add(4, "D");

        Assert.True(map.SetNodeKey(node, 10));

        Assert.False(map.ContainsKey(1));
        Assert.True(map.ContainsKey(10));
        Assert.Equal("A", map[10]);

        Assert.Same(node, map.FindNode(10));
        Assert.True(map.Validate());

        Assert.Equal(
            [2, 3, 4, 10],
            map.Select(x => x.Key).ToArray());
    }

    [Fact]
    public void SetNodeKeyRejectsDuplicate()
    {
        var map = new OrderedMap<int, string>();

        var first = map.Add(1, "A").Node;
        map.Add(2, "B");

        Assert.False(map.SetNodeKey(first, 2));

        Assert.Equal(2, map.Count);
        Assert.Equal("A", map[1]);
        Assert.Equal("B", map[2]);
        Assert.Same(first, map.FindNode(1));
        Assert.True(map.Validate());
    }

    [Fact]
    public void RemovedNodeCanBeReused()
    {
        var map = new OrderedMap<int, string>();

        var node = map.Add(1, "A").Node;

        map.RemoveNode(node);

        Assert.True(node.IsUnused);

        var result = map.Add(2, "B", node);

        Assert.True(result.NewlyAdded);
        Assert.Same(node, result.Node);
        Assert.Equal(2, node.Key);
        Assert.Equal("B", node.Value);
        Assert.True(map.Validate());
    }

    [Fact]
    public void LowerAndUpperBound()
    {
        var map = new OrderedMap<int, int>();

        map.Add(10, 10);
        map.Add(20, 20);
        map.Add(30, 30);
        map.Add(40, 40);

        Assert.Equal(20, map.GetLowerBound(15)!.Key);
        Assert.Equal(10, map.GetUpperBound(15)!.Key);

        Assert.Equal(20, map.GetLowerBound(20)!.Key);
        Assert.Equal(20, map.GetUpperBound(20)!.Key);

        Assert.Equal(10, map.GetLowerBound(1)!.Key);
        Assert.Null(map.GetUpperBound(1));

        Assert.Null(map.GetLowerBound(100));
        Assert.Equal(40, map.GetUpperBound(100)!.Key);
    }

    [Fact]
    public void GetRange()
    {
        var map = new OrderedMap<int, int>();

        for (var i = 0; i <= 10; i++)
        {
            map.Add(i, i);
        }

        var range = map.GetRange(3, 7);

        Assert.Equal(3, range.Lower!.Key);
        Assert.Equal(7, range.Upper!.Key);
    }

    [Fact]
    public void Clear()
    {
        var map = new OrderedMap<int, int>();

        for (var i = 0; i < 100; i++)
        {
            map.Add(i, i);
        }

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.Null(map.First);
        Assert.Null(map.Last);
        Assert.True(map.Validate());

        map.Add(1000, 1000);

        Assert.Equal(1, map.Count);
        Assert.Equal(1000, map[1000]);
        Assert.True(map.Validate());
    }

    [Fact]
    public void ClearMarksNodesUnused()
    {
        var map = new OrderedMap<int, int>();

        var a = map.Add(1, 1).Node;
        var b = map.Add(2, 2).Node;
        var c = map.Add(3, 3).Node;

        map.Clear();

        Assert.True(a.IsUnused);
        Assert.True(b.IsUnused);
        Assert.True(c.IsUnused);
    }

    [Fact]
    public void EnumeratorDetectsModification()
    {
        var map = new OrderedMap<int, int>();

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
        var map = new OrderedMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);

        var enumerator = map.Keys.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.Remove(2);

        Assert.Throws<InvalidOperationException>(
            () => enumerator.MoveNext());
    }

    [Fact]
    public void ValueEnumeratorDetectsModification()
    {
        var map = new OrderedMap<int, int>();

        var node = map.Add(1, 1).Node;
        map.Add(2, 2);

        var enumerator = map.Values.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        map.SetNodeValue(node, 100);

        Assert.Throws<InvalidOperationException>(
            () => enumerator.MoveNext());
    }

    [Fact]
    public void EnumeratorReset()
    {
        var map = new OrderedMap<int, int>();

        map.Add(1, 1);
        map.Add(2, 2);
        map.Add(3, 3);

        var enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current.Key);

        ((System.Collections.IEnumerator)enumerator).Reset();

        // IEnumerator.Reset boxes a struct enumerator, so test the interface path separately.
        System.Collections.IEnumerator interfaceEnumerator =
            ((IEnumerable<KeyValuePair<int, int>>)map).GetEnumerator();

        Assert.True(interfaceEnumerator.MoveNext());
        interfaceEnumerator.Reset();
        Assert.True(interfaceEnumerator.MoveNext());

        Assert.Equal(
            1,
            ((KeyValuePair<int, int>)interfaceEnumerator.Current).Key);
    }

    [Fact]
    public void ManyInsertionsRemainValid()
    {
        var map = new OrderedMap<int, int>();

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            map.Add(i, i * 10);

            if ((i & 31) == 0)
            {
                Assert.True(map.Validate());
            }
        }

        Assert.Equal(count, map.Count);
        Assert.True(map.Validate());

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i * 10, map[i]);
        }
    }

    [Fact]
    public void MixedInsertRemoveRemainValid()
    {
        var map = new OrderedMap<int, int>();
        var random = new Random(12345);
        var expected = new SortedSet<int>();

        for (var i = 0; i < 5000; i++)
        {
            var key = random.Next(0, 500);

            if ((random.Next() & 1) == 0)
            {
                var added = expected.Add(key);
                var result = map.Add(key, key);

                Assert.Equal(added, result.NewlyAdded);
            }
            else
            {
                var removed = expected.Remove(key);

                Assert.Equal(removed, map.Remove(key));
            }

            Assert.Equal(expected.Count, map.Count);
            Assert.True(map.Validate());
        }

        Assert.Equal(
            expected.ToArray(),
            map.Select(x => x.Key).ToArray());
    }
}
