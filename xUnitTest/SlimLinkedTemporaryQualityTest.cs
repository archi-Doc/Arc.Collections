// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class SlimLinkedTemporaryQualityTest
{
    #region UnorderedMapSlim

    [Fact]
    public void MapSlim_ConstructorRoundsCapacityAndRejectsOversizedRequests()
    {
        Assert.Equal((int)CollectionHelper.MinimumCapacity, new UnorderedMapSlim<int, int>().Capacity);
        Assert.Equal(16, new UnorderedMapSlim<int, int>(9).Capacity);
        Assert.Equal(16, new UnorderedMapSlim<int, int>(16).Capacity);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedMapSlim<int, int>((1u << 30) + 1));
        Assert.Equal("minimumCapacity", exception.ParamName);
    }

    [Fact]
    public void MapSlim_NullKeysAreRejectedByEveryKeyedMember()
    {
        var map = new UnorderedMapSlim<string, int>();
        map.AddOrUpdate("a", 1);

        Assert.Throws<ArgumentNullException>(() => map[null!]);
        Assert.Throws<ArgumentNullException>(() => map[null!] = 1);
        Assert.Throws<ArgumentNullException>(() => map.AddOrUpdate(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.TryAdd(null!, 1));
        Assert.Throws<ArgumentNullException>(() => map.ContainsKey(null!));
        Assert.Throws<ArgumentNullException>(() => map.TryGetValue(null!, out _));
        Assert.Throws<ArgumentNullException>(() => map.GetValueRefOrAddDefault(null!, out _));
        Assert.Throws<ArgumentNullException>(() => map.Remove(null!));
        Assert.Throws<ArgumentNullException>(() => map.Remove(null!, out _));
        Assert.Equal(1, map.Count);
        Assert.Equal(1, map["a"]);
    }

    [Fact]
    public void MapSlim_IndexerGetThrowsForMissingKeyAndSetAddsOrReplaces()
    {
        var map = new UnorderedMapSlim<string, string>();
        Assert.Throws<KeyNotFoundException>(() => map["missing"]);

        map["a"] = "1";
        map["a"] = "2";
        map["b"] = "3";

        Assert.Equal(2, map.Count);
        Assert.Equal("2", map["a"]);
        Assert.Equal("3", map["b"]);
        Assert.True(map.ContainsKey("a"));
        Assert.False(map.ContainsKey("c"));
        Assert.False(map.TryGetValue("c", out var missing));
        Assert.Null(missing);
    }

    [Fact]
    public void MapSlim_UnsafeGetNodesExposesLiveAndRemovedSlots()
    {
        var map = new UnorderedMapSlim<string, string>();
        for (var i = 0; i < 5; i++)
        {
            map.AddOrUpdate("k" + i, "v" + i);
        }

        Assert.True(map.Remove("k2", out var removed));
        Assert.Equal("v2", removed);

        var (nodes, slotCount) = map.UnsafeGetNodes();
        Assert.Equal(5, slotCount);
        Assert.Equal(map.Capacity, nodes.Length);

        var live = new List<(string Key, string Value)>();
        for (var i = 0; i < slotCount; i++)
        {
            if (nodes[i].IsInUse())
            {
                live.Add((nodes[i].Key, nodes[i].Value));
            }
            else
            {
                // A removed slot must not keep its key or value reachable.
                Assert.Equal(2, i);
                Assert.Null(nodes[i].Key);
                Assert.Null(nodes[i].Value);
            }
        }

        Assert.Equal(new[] { ("k0", "v0"), ("k1", "v1"), ("k3", "v3"), ("k4", "v4") }, live);
    }

    [Fact]
    public void MapSlim_FreedSlotsAreReusedBeforeGrowing()
    {
        var map = new UnorderedMapSlim<int, int>(8);
        for (var i = 0; i < 8; i++)
        {
            map.AddOrUpdate(i, i * 10);
        }

        Assert.True(map.Remove(1));
        Assert.True(map.Remove(5));
        Assert.True(map.Remove(3));
        Assert.Equal(5, map.Count);

        Assert.True(map.TryAdd(100, 1000));
        ref var value = ref map.GetValueRefOrAddDefault(101, out var exists);
        Assert.False(exists);
        Assert.Equal(0, value);
        value = 1010;
        map[102] = 1020;

        Assert.Equal(8, map.Capacity);
        Assert.Equal(8, map.UnsafeGetNodes().SlotCount);
        Assert.Equal(8, map.Count);

        // The free list is exhausted, so the next addition grows the table.
        map[103] = 1030;
        Assert.Equal(16, map.Capacity);
        Assert.Equal(9, map.Count);

        var expected = new Dictionary<int, int>
        {
            { 0, 0 }, { 2, 20 }, { 4, 40 }, { 6, 60 }, { 7, 70 },
            { 100, 1000 }, { 101, 1010 }, { 102, 1020 }, { 103, 1030 },
        };
        map.ValidateWithDictionary(expected);
    }

    [Fact]
    public void MapSlim_GetValueRefOrAddDefaultWritesThroughAfterGrowing()
    {
        var map = new UnorderedMapSlim<int, string>(8);
        for (var i = 0; i < 8; i++)
        {
            Assert.True(map.TryAdd(i * 8, "v" + i));
        }

        // Every key shares bucket 0 before growing, so the new entry must be linked into the rebuilt table.
        ref var value = ref map.GetValueRefOrAddDefault(64, out var exists);
        Assert.False(exists);
        Assert.Null(value);
        value = "added";

        Assert.Equal(16, map.Capacity);
        Assert.Equal("added", map[64]);
        for (var i = 0; i < 8; i++)
        {
            Assert.Equal("v" + i, map[i * 8]);
        }

        ref var existing = ref map.GetValueRefOrAddDefault(64, out exists);
        Assert.True(exists);
        Assert.Equal("added", existing);
    }

    [Fact]
    public void MapSlim_ContainsValueIgnoresRemovedSlots()
    {
        var references = new UnorderedMapSlim<string, string?>();
        references.AddOrUpdate("a", "x");
        Assert.False(references.ContainsValue(null));

        references.AddOrUpdate("b", null);
        Assert.True(references.ContainsValue(null));

        // The removed slot's value is cleared to null; it must not be reported.
        Assert.True(references.Remove("b"));
        Assert.False(references.ContainsValue(null));
        Assert.True(references.ContainsValue("x"));
        Assert.True(references.Remove("a"));
        Assert.False(references.ContainsValue("x"));

        // Unmanaged values are not cleared on removal; they must still be skipped.
        var values = new UnorderedMapSlim<int, int>();
        values.AddOrUpdate(1, 42);
        values.AddOrUpdate(2, 7);
        Assert.True(values.Remove(1));
        Assert.False(values.ContainsValue(42));
        Assert.True(values.ContainsValue(7));

        var nullable = new UnorderedMapSlim<int, int?>();
        nullable.AddOrUpdate(1, 5);
        Assert.False(nullable.ContainsValue(null));
        nullable.AddOrUpdate(2, null);
        Assert.True(nullable.ContainsValue(null));
    }

    [Fact]
    public void MapSlim_ClearReleasesReferencesAndKeepsCapacity()
    {
        var empty = new UnorderedMapSlim<string, string>();
        empty.Clear();
        Assert.Equal(0, empty.Count);
        Assert.Equal(8, empty.Capacity);

        var map = new UnorderedMapSlim<string, string>();
        for (var i = 0; i < 20; i++)
        {
            map.AddOrUpdate("k" + i, "v" + i);
        }

        Assert.True(map.Remove("k3"));
        var capacity = map.Capacity;
        var (nodes, _) = map.UnsafeGetNodes();
        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.Equal(capacity, map.Capacity);
        Assert.Equal(0, map.UnsafeGetNodes().SlotCount);
        Assert.Empty(map);
        Assert.All(nodes, x => Assert.Null(x.Key));
        Assert.All(nodes, x => Assert.Null(x.Value));
        Assert.False(map.ContainsKey("k0"));

        map.AddOrUpdate("new", "value");
        Assert.Equal(1, map.Count);
        Assert.Equal("value", map["new"]);
        Assert.Equal(new[] { new KeyValuePair<string, string>("new", "value") }, map.ToArray());
    }

    [Fact]
    public void MapSlim_NonGenericEnumeratorChecksCurrentAndCanReset()
    {
        var map = new UnorderedMapSlim<int, string>();
        map.AddOrUpdate(1, "one");
        map.AddOrUpdate(2, "two");
        map.AddOrUpdate(3, "three");
        map.Remove(3);

        IEnumerable enumerable = map;
        var enumerator = enumerable.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        var first = new List<object>();
        while (enumerator.MoveNext())
        {
            first.Add(enumerator.Current);
        }

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        var second = new List<object>();
        while (enumerator.MoveNext())
        {
            second.Add(enumerator.Current);
        }

        Assert.Equal(
            new object[] { new KeyValuePair<int, string>(1, "one"), new KeyValuePair<int, string>(2, "two") },
            first);
        Assert.Equal(first, second);
        ((IDisposable)enumerator).Dispose();
    }

    [Fact]
    public void MapSlim_EnumerationSkipsRemovedSlotsAtBothEnds()
    {
        var map = new UnorderedMapSlim<int, int>();
        for (var i = 0; i < 6; i++)
        {
            map.AddOrUpdate(i, i);
        }

        map.Remove(0);
        map.Remove(5);
        map.Remove(4);

        var keys = new List<int>();
        foreach (var pair in map)
        {
            keys.Add(pair.Key);
        }

        Assert.Equal(new[] { 1, 2, 3 }, keys);

        map.Remove(1);
        map.Remove(2);
        map.Remove(3);
        Assert.Empty(map);
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void MapSlim_MatchesDictionaryUnderRandomOperations()
    {
        var random = new Random(42);
        var map = new UnorderedMapSlim<string, int>();
        var model = new Dictionary<string, int>();
        for (var i = 0; i < 4000; i++)
        {
            var key = "k" + random.Next(300);
            switch (random.Next(5))
            {
                case 0:
                    Assert.Equal(model.TryAdd(key, i), map.TryAdd(key, i));
                    break;

                case 1:
                    model[key] = i;
                    map.AddOrUpdate(key, i);
                    break;

                case 2:
                    Assert.Equal(model.Remove(key, out var expected), map.Remove(key, out var actual));
                    Assert.Equal(expected, actual);
                    break;

                case 3:
                    ref var value = ref map.GetValueRefOrAddDefault(key, out var exists);
                    Assert.Equal(model.ContainsKey(key), exists);
                    value += 1;
                    model[key] = model.GetValueOrDefault(key) + 1;
                    break;

                default:
                    Assert.Equal(model.TryGetValue(key, out var modelValue), map.TryGetValue(key, out var mapValue));
                    Assert.Equal(modelValue, mapValue);
                    break;
            }

            if (i % 500 == 0)
            {
                map.ValidateWithDictionary(model);
            }

            if (i == 2000)
            {
                map.Clear();
                model.Clear();
            }
        }

        map.ValidateWithDictionary(model);
    }

    [Fact]
    public void MapSlim_WarmLookupsUpdatesAndEnumerationDoNotAllocate()
    {
        var map = new UnorderedMapSlim<int, int>();
        for (var i = 0; i < 100; i++)
        {
            map.AddOrUpdate(i, i);
        }

        long sum = 0;
        var allocated = AllocationHelper.Measure(() => sum += RunMapOperations(map));

        Assert.NotEqual(0, sum);
        Assert.Equal(100, map.Count);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static long RunMapOperations(UnorderedMapSlim<int, int> map)
        {
            long sum = 0;
            for (var i = 0; i < 100; i++)
            {
                map.AddOrUpdate(i, i + 1);
                map.TryGetValue(i, out var value);
                sum += value + map[i];
                if (map.ContainsKey(i))
                {
                    sum++;
                }

                ref var slot = ref map.GetValueRefOrAddDefault(i, out _);
                slot--;
            }

            foreach (var pair in map)
            {
                sum += pair.Value;
            }

            return sum;
        }
    }

    #endregion

    #region UnorderedLinkedList

    [Fact]
    public void LinkedList_MatchesModelUnderRandomNodeOperations()
    {
        var random = new Random(7);
        var list = new UnorderedLinkedList<int>();
        var model = new List<UnorderedLinkedList<int>.Node>();
        var detached = new List<UnorderedLinkedList<int>.Node>();
        var next = 0;
        for (var i = 0; i < 3000; i++)
        {
            var operation = random.Next(11);
            if (model.Count == 0 && operation >= 4)
            {
                operation = random.Next(4);
            }

            var position = model.Count == 0 ? 0 : random.Next(model.Count);
            switch (operation)
            {
                case 0:
                    model.Insert(0, list.AddFirst(next++));
                    break;

                case 1:
                    model.Add(list.AddLast(next++));
                    break;

                case 2:
                    if (detached.Count > 0)
                    {
                        var node = detached[^1];
                        detached.RemoveAt(detached.Count - 1);
                        list.AddFirst(node);
                        model.Insert(0, node);
                    }

                    break;

                case 3:
                    if (detached.Count > 0)
                    {
                        var node = detached[^1];
                        detached.RemoveAt(detached.Count - 1);
                        list.AddLast(node);
                        model.Add(node);
                    }

                    break;

                case 4:
                    model.Insert(position + 1, list.AddAfter(model[position], next++));
                    break;

                case 5:
                    model.Insert(position, list.AddBefore(model[position], next++));
                    break;

                case 6:
                    if (detached.Count > 0)
                    {
                        var node = detached[^1];
                        detached.RemoveAt(detached.Count - 1);
                        if (random.Next(2) == 0)
                        {
                            list.AddAfter(model[position], node);
                            model.Insert(position + 1, node);
                        }
                        else
                        {
                            list.AddBefore(model[position], node);
                            model.Insert(position, node);
                        }
                    }

                    break;

                case 7:
                    {
                        var node = model[position];
                        list.Remove(node);
                        model.RemoveAt(position);
                        detached.Add(node);
                    }

                    break;

                case 8:
                    {
                        var node = model[position];
                        list.MoveToFirst(node);
                        model.RemoveAt(position);
                        model.Insert(0, node);
                    }

                    break;

                case 9:
                    {
                        var node = model[position];
                        list.MoveToLast(node);
                        model.RemoveAt(position);
                        model.Add(node);
                    }

                    break;

                default:
                    {
                        var removeFirst = random.Next(2) == 0;
                        var node = removeFirst ? model[0] : model[^1];
                        if (removeFirst)
                        {
                            list.RemoveFirst();
                        }
                        else
                        {
                            list.RemoveLast();
                        }

                        model.Remove(node);
                        detached.Add(node);
                    }

                    break;
            }

            ValidateLinkedList(list, model, detached);
        }
    }

    [Fact]
    public void LinkedList_ClearDetachesNodesForReuseInAnotherList()
    {
        var source = new UnorderedLinkedList<string>(["a", "b", "c"]);
        var nodes = new List<UnorderedLinkedList<string>.Node>();
        for (var node = source.First; node is not null; node = node.Next)
        {
            nodes.Add(node);
        }

        source.Clear();
        Assert.Empty(source);
        Assert.All(nodes, x => Assert.Null(x.List));
        Assert.All(nodes, x => Assert.Null(x.Previous));
        Assert.All(nodes, x => Assert.Null(x.Next));

        var target = new UnorderedLinkedList<string>();
        foreach (var node in nodes)
        {
            target.AddFirst(node);
        }

        Assert.Equal(new[] { "c", "b", "a" }, target);
        Assert.All(nodes, x => Assert.Same(target, x.List));
        Assert.Same(nodes[0], target.FindLast("a"));
        Assert.Same(nodes[2], target.Find("c"));
    }

    private static void ValidateLinkedList<T>(
        UnorderedLinkedList<T> list,
        List<UnorderedLinkedList<T>.Node> model,
        List<UnorderedLinkedList<T>.Node> detached)
    {
        Assert.Equal(model.Count, list.Count);
        if (model.Count == 0)
        {
            Assert.Null(list.First);
            Assert.Null(list.Last);
        }
        else
        {
            Assert.Same(model[0], list.First);
            Assert.Same(model[^1], list.Last);
        }

        for (var i = 0; i < model.Count; i++)
        {
            var node = model[i];
            Assert.Same(list, node.List);
            Assert.Same(i == 0 ? null : model[i - 1], node.Previous);
            Assert.Same(i == model.Count - 1 ? null : model[i + 1], node.Next);
        }

        foreach (var node in detached)
        {
            Assert.Null(node.List);
            Assert.Null(node.Previous);
            Assert.Null(node.Next);
        }

        Assert.Equal(model.Select(x => x.Value), list);
    }

    #endregion

    #region TemporaryList

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(9)]
    public void TemporaryList_EnumerationAndToArrayPreserveInsertionOrder(int count)
    {
        var list = default(TemporaryList<string>);
        var expected = new List<string>();
        for (var i = 0; i < count; i++)
        {
            list.Add("v" + i);
            expected.Add("v" + i);
        }

        Assert.Equal(count, list.Count);
        Assert.Equal(expected, list.ToArray());

        var enumerated = new List<string>();
        foreach (var x in list)
        {
            enumerated.Add(x);
        }

        Assert.Equal(expected, enumerated);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void TemporaryList_EnumeratorStopsAtEndAndCanReset(int count)
    {
        var list = default(TemporaryList<int>);
        for (var i = 0; i < count; i++)
        {
            list.Add((i * 10) + 1);
        }

        var enumerator = new TemporaryList<int>.Enumerator(list);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(11, enumerator.Current);

        enumerator.Reset();
        var values = new List<int>();
        while (enumerator.MoveNext())
        {
            values.Add(enumerator.Current);
        }

        Assert.Equal(Enumerable.Range(0, count).Select(x => (x * 10) + 1), values);
        for (var i = 0; i < 3; i++)
        {
            Assert.False(enumerator.MoveNext());
        }

        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        enumerator.Dispose();
    }

    [Fact]
    public void TemporaryList_EnumeratorIgnoresElementsAddedAfterCreation()
    {
        var list = default(TemporaryList<int>);
        for (var i = 0; i < 5; i++)
        {
            list.Add(i);
        }

        var enumerator = list.GetEnumerator();
        list.Add(5);
        list.Add(6);

        var values = new List<int>();
        while (enumerator.MoveNext())
        {
            values.Add(enumerator.Current);
        }

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, values);
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6 }, list.ToArray());
    }

    [Fact]
    public void TemporaryList_CopiesMadeBeforeOverflowStayIndependent()
    {
        var a = default(TemporaryList<int>);
        a.Add(0);
        var b = a;
        for (var i = 1; i <= 5; i++)
        {
            a.Add(i);
            b.Add(-i);
        }

        // Each copy allocates its own overflow list on its fifth element.
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, a.ToArray());
        Assert.Equal(new[] { 0, -1, -2, -3, -4, -5 }, b.ToArray());
    }

    [Fact]
    public void TemporaryList_InlineElementsDoNotAllocate()
    {
        var sum = 0;
        var allocated = AllocationHelper.Measure(() => sum = FillAndSum(4));
        Assert.Equal(0 + 1 + 2 + 3, sum);

        // The fifth element moves to the heap-allocated overflow list.
        var overflowAllocated = AllocationHelper.Measure(() => sum = FillAndSum(5));
        Assert.NotEqual(0, overflowAllocated);
        Assert.Equal(0 + 1 + 2 + 3 + 4, sum);

        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static int FillAndSum(int count)
        {
            var list = default(TemporaryList<int>);
            for (var i = 0; i < count; i++)
            {
                list.Add(i);
            }

            var sum = 0;
            foreach (var x in list)
            {
                sum += x;
            }

            return sum;
        }
    }

    #endregion
}
