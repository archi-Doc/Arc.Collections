// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Regression and coverage tests for <see cref="OrderedMap{TKey, TValue}"/> and <see cref="OrderedSet{T}"/>.
/// </summary>
public class OrderedMapQualityTest
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // NodeColor is internal: Black = 0, Red = 1.
    private static readonly object Red = Enum.ToObject(typeof(OrderedMap<int, int>.Node).GetProperty("Color", InstanceFlags)!.PropertyType, 1);

    private static readonly object Black = Enum.ToObject(typeof(OrderedMap<int, int>.Node).GetProperty("Color", InstanceFlags)!.PropertyType, 0);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeComparableKeysCompareLikeTheSearch(bool reverse)
    {
        // object does not implement IComparable<object>, so Comparer<object>.Default cannot compare these keys;
        // the search uses the keys' own IComparable<object>, and every other comparison must do the same.
        var map = new OrderedMap<object, int>(reverse);
        foreach (var x in new[] { 5, 1, 9, 3, 7 })
        {
            map.Add(new ObjectKey(x), x).NewlyAdded.IsTrue();
        }

        map.Validate().IsTrue();
        map.Keys.Select(x => ((ObjectKey)x).Value).ToArray().Is(reverse ? new[] { 9, 7, 5, 3, 1 } : new[] { 1, 3, 5, 7, 9 });

        var (lower, upper) = map.GetRange(new ObjectKey(reverse ? 8 : 2), new ObjectKey(reverse ? 2 : 8));
        lower!.Value.Is(reverse ? 7 : 3);
        upper!.Value.Is(reverse ? 3 : 7);

        var node = map.FindNode(new ObjectKey(5))!;
        map.SetNodeKey(node, new ObjectKey(6)).IsTrue(); // In place.
        map.SetNodeKey(node, new ObjectKey(10)).IsTrue(); // Reinserted.
        map.SetNodeKey(node, new ObjectKey(1)).IsFalse(); // Duplicate.
        map.Validate().IsTrue();
        map.ContainsKey(new ObjectKey(10)).IsTrue();
        map.ContainsKey(new ObjectKey(5)).IsFalse();

        var set = new OrderedSet<object>(new object[] { new ObjectKey(2), new ObjectKey(1) }, reverse);
        set.Validate().IsTrue();
        var range = set.GetRange(new ObjectKey(reverse ? 3 : 0), new ObjectKey(reverse ? 0 : 3));
        range.Lower!.Key.IsSameReferenceAs(set.FirstNode!.Key);
        range.Upper!.Key.IsSameReferenceAs(set.LastNode!.Key);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SequenceConstructorKeepsFirstValueAndHonorsOrdering(bool reverse)
    {
        var source = new KeyValuePair<int, string>[] { new(3, "c"), new(1, "a"), new(3, "x"), new(2, "b") };
        var map = new OrderedMap<int, string>(source, null, reverse);
        map.Count.Is(3);
        map[3].Is("c");
        map.IsReversed.Is(reverse);
        map.Comparer.IsSameReferenceAs(Comparer<int>.Default);
        map.HotTreeMethod.IsNotNull();
        map.Keys.ToArray().Is(reverse ? new[] { 3, 2, 1 } : new[] { 1, 2, 3 });
        map.Validate().IsTrue();

        var descending = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var custom = new OrderedMap<int, string>(source, descending, reverse);
        custom.Comparer.IsSameReferenceAs(descending);
        custom.HotTreeMethod.IsNull();
        custom.Keys.ToArray().Is(reverse ? new[] { 1, 2, 3 } : new[] { 3, 2, 1 });

        Assert.Throws<ArgumentNullException>(() => new OrderedMap<int, string>((IEnumerable<KeyValuePair<int, string>>)null!));
    }

    [Fact]
    public void ContainsValueHandlesNullAndMissingValues()
    {
        var map = new OrderedMap<int, string?>();
        map.ContainsValue(null).IsFalse();
        map.Add(1, "a");
        map.Add(2, "b");
        map.Add(3, null);
        map.ContainsValue(null).IsTrue();
        map.ContainsValue("b").IsTrue();
        map.ContainsValue("z").IsFalse();

        map.SetNodeValue(map.FindNode(3)!, "c");
        map.ContainsValue(null).IsFalse();
        map.ContainsValue("c").IsTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopyToCopiesInOrderAndValidatesArguments(bool reverse)
    {
        var map = new OrderedMap<int, int>(reverse);
        foreach (var x in new[] { 3, 0, 4, 1, 2 })
        {
            map.Add(x, x * 10);
        }

        var array = new KeyValuePair<int, int>[7];
        map.CopyTo(array, 2);
        array.Take(2).Is(default(KeyValuePair<int, int>), default(KeyValuePair<int, int>));
        var keys = reverse ? new[] { 4, 3, 2, 1, 0 } : new[] { 0, 1, 2, 3, 4 };
        array.Skip(2).Is(keys.Select(x => new KeyValuePair<int, int>(x, x * 10)));

        Assert.Throws<ArgumentNullException>(() => map.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.CopyTo(array, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.CopyTo(array, 8));
        Assert.Throws<ArgumentException>(() => map.CopyTo(array, 3));

        // An empty map can be copied to the end of an array.
        new OrderedMap<int, int>().CopyTo(array, array.Length);
    }

    [Theory]
    [InlineData(false, 30, 30, false)] // Same key.
    [InlineData(false, 30, 20, false)] // Equal to the previous key.
    [InlineData(false, 30, 40, false)] // Equal to the next key.
    [InlineData(false, 30, 25, true)] // In place, toward the previous key.
    [InlineData(false, 30, 35, true)] // In place, toward the next key.
    [InlineData(false, 30, 10, false)] // Another node has the key.
    [InlineData(false, 30, 50, false)]
    [InlineData(false, 30, 5, true)] // Reinserted.
    [InlineData(false, 30, 55, true)]
    [InlineData(false, 10, 5, true)] // First node, in place.
    [InlineData(false, 50, 55, true)] // Last node, in place.
    [InlineData(false, 10, 60, true)]
    [InlineData(true, 30, 30, false)]
    [InlineData(true, 30, 20, false)]
    [InlineData(true, 30, 40, false)]
    [InlineData(true, 30, 25, true)]
    [InlineData(true, 30, 35, true)]
    [InlineData(true, 30, 10, false)]
    [InlineData(true, 30, 50, false)]
    [InlineData(true, 30, 5, true)]
    [InlineData(true, 30, 55, true)]
    [InlineData(true, 10, 5, true)]
    [InlineData(true, 50, 55, true)]
    [InlineData(true, 10, 60, true)]
    public void SetNodeKeyKeepsTheNodeAndOrder(bool reverse, int from, int to, bool expected)
    {
        var keys = new SortedSet<int> { 10, 20, 30, 40, 50 };
        var map = new OrderedMap<int, string>(reverse);
        foreach (var x in keys)
        {
            map.Add(x, x.ToString());
        }

        var node = map.FindNode(from)!;
        var enumerator = map.GetEnumerator();
        map.SetNodeKey(node, to).Is(expected);
        map.Validate().IsTrue();
        node.IsUnused.IsFalse();
        node.Value.Is(from.ToString());
        if (expected)
        {
            keys.Remove(from);
            keys.Add(to);
            node.Key.Is(to);
            map.FindNode(to).IsSameReferenceAs(node);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }
        else
        {
            node.Key.Is(from);
            enumerator.MoveNext().IsTrue();
        }

        map.Keys.ToArray().Is(reverse ? keys.Reverse() : keys);

        map.RemoveNode(node);
        map.SetNodeKey(node, 100).IsFalse();
        map.ContainsKey(100).IsFalse();
    }

    [Fact]
    public void NodeOperationsIgnoreUnusedNodes()
    {
        var map = new OrderedMap<int, string?>();
        var node = map.Add(1, "a").Node;
        map.Add(2, "b");
        map.RemoveNode(node);
        node.IsUnused.IsTrue();
        node.Value.IsNull();
        node.Previous.IsNull();
        node.Next.IsNull();
        map.Count.Is(1);

        var enumerator = map.GetEnumerator();
        map.SetNodeValue(node, "z");
        map.RemoveNode(node);
        map.Count.Is(1);
        node.Value.IsNull();
        enumerator.MoveNext().IsTrue(); // Neither call modified the map.
        enumerator.Current.Is(new KeyValuePair<int, string?>(2, "b"));

        var active = map.FindNode(2)!;
        map.Add(3, "c", active).Node.IsNotSameReferenceAs(active); // An active node is not reused.
        map.Add(3, "d", node).NewlyAdded.IsFalse(); // The key exists, so the unused node is left untouched.
        node.IsUnused.IsTrue();
        map.Add(4, "e", node).Node.IsSameReferenceAs(node);
        map.Validate().IsTrue();
    }

    [Fact]
    public void UnsafeChangeValueKeepsEnumeratorsValid()
    {
        var map = new OrderedMap<int, string>();
        map.Add(1, "a");
        map.Add(2, "b");
        var enumerator = map.GetEnumerator();
        enumerator.MoveNext().IsTrue();
        map.FindNode(2)!.UnsafeChangeValue("B");
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Value.Is("B");

        map.SetNodeValue(map.FindNode(1)!, "A");
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        map[1].Is("A");

        var values = map.Values.GetEnumerator();
        map[2] = "C"; // The indexer setter replaces the value and invalidates enumerators.
        Assert.Throws<InvalidOperationException>(() => values.MoveNext());
        map.Values.ToArray().Is("A", "C");
    }

    [Fact]
    public void RemovingTheOnlyNodeEmptiesTheMap()
    {
        var map = new OrderedMap<string, int>();
        var node = map.Add("a", 1).Node;
        map.RemoveNode(node);
        map.Count.Is(0);
        map.First.IsNull();
        map.Last.IsNull();
        map.Validate().IsTrue();
        node.IsUnused.IsTrue();

        map.Add("b", 2, node).Node.IsSameReferenceAs(node);
        map.First.IsSameReferenceAs(node);
        map.Validate().IsTrue();
    }

    [Fact]
    public void ClearLeavesEveryNodeUnusedAndReusable()
    {
        var map = new OrderedMap<int, int>();
        var nodes = Enumerable.Range(0, 20).Select(x => map.Add(x, x).Node).ToArray();
        var enumerator = map.GetEnumerator();
        map.Clear();
        map.Count.Is(0);
        map.First.IsNull();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        foreach (var node in nodes)
        {
            node.IsUnused.IsTrue();
        }

        map.Add(5, 50, nodes[7]).Node.IsSameReferenceAs(nodes[7]);
        map.Add(6, 60, nodes[0]).Node.IsSameReferenceAs(nodes[0]);
        map.Keys.ToArray().Is(5, 6);
        map.Validate().IsTrue();

        var set = new OrderedSet<string>(new[] { "a", "b", "c" });
        var setNode = set.FindNode("b")!;
        set.Clear();
        setNode.IsUnused.IsTrue();
        set.AddNode("x", setNode).Node.IsSameReferenceAs(setNode);
        set.Validate().IsTrue();
    }

    [Fact]
    public void SetNodeKeyKeepsTheStoredKeyWhenTheComparerReportsEqual()
    {
        var map = new OrderedMap<string, int>(StringComparer.OrdinalIgnoreCase);
        var node = map.Add("a", 1).Node;
        map.Add("b", 2);
        map.SetNodeKey(node, "A").IsFalse();
        node.Key.Is("a");
        map.SetNodeKey(node, "B").IsFalse(); // "b" already exists.
        node.Key.Is("a");

        var set = new OrderedSet<string>(StringComparer.OrdinalIgnoreCase);
        var setNode = set.AddNode("a").Node;
        set.SetNodeKey(setNode, "A").IsFalse();
        setNode.Key.Is("a");
        set.Validate().IsTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetRangeReturnsEmptyOutsideTheKeys(bool reverse)
    {
        // Negated keys in a reversed map have the same relative order as the original keys in a forward map.
        int K(int x) => reverse ? -x : x;
        var map = new OrderedMap<int, int>(reverse);
        map.Add(K(10), 10);
        map.Add(K(20), 20);
        map.Add(K(30), 30);

        var (lower, upper) = map.GetRange(K(40), K(50)); // No lower bound.
        lower.IsNull();
        upper.IsNull();
        (lower, upper) = map.GetRange(K(0), K(5)); // No upper bound.
        lower.IsNull();
        upper.IsNull();
        (lower, upper) = map.GetRange(K(25), K(15)); // The bounds cross.
        lower.IsNull();
        upper.IsNull();
        (lower, upper) = map.GetRange(K(15), K(25));
        lower!.Value.Is(20);
        upper.IsSameReferenceAs(lower);
        (lower, upper) = map.GetRange(K(10), K(30));
        lower!.Value.Is(10);
        upper!.Value.Is(30);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValueTypeKeysWithoutHotMethodMatchReference(bool reverse)
    {
        // decimal has no specialized tree search, so the generic value-type search is used.
        var map = new OrderedMap<decimal, int>(reverse);
        map.HotTreeMethod.IsNull();
        var comparer = Comparer<decimal>.Create((x, y) => reverse ? y.CompareTo(x) : x.CompareTo(y));
        var reference = new SortedSet<decimal>(comparer);
        var random = new Random(318);
        for (var i = 0; i < 600; i++)
        {
            var key = random.Next(40) / 2m;
            if (random.Next(3) == 0)
            {
                map.Remove(key).Is(reference.Remove(key));
            }
            else
            {
                map.Add(key, i).NewlyAdded.Is(reference.Add(key));
            }
        }

        map.Validate().IsTrue();
        map.Keys.ToArray().Is(reference);
        for (var x = -1m; x <= 21m; x += 0.25m)
        {
            map.ContainsKey(x).Is(reference.Contains(x));
            var lowerKey = map.GetLowerBound(x)?.Key;
            lowerKey.Is(reference.Where(y => comparer.Compare(y, x) >= 0).Select(y => (decimal?)y).FirstOrDefault());
            var upperKey = map.GetUpperBound(x)?.Key;
            upperKey.Is(reference.Where(y => comparer.Compare(y, x) <= 0).Select(y => (decimal?)y).LastOrDefault());
        }
    }

    [Fact]
    public void NonGenericEnumeratorsValidateCurrentAndReset()
    {
        var map = new OrderedMap<int, string>();
        map.Add(2, "b");
        map.Add(1, "a");

        VerifyEnumerator(((IEnumerable)map).GetEnumerator(), new object[] { new KeyValuePair<int, string>(1, "a"), new KeyValuePair<int, string>(2, "b") }, () => map.Add(3, "c"));
        VerifyEnumerator(((IEnumerable)map.Keys).GetEnumerator(), new object[] { 1, 2, 3 }, () => map.Add(4, "d"));
        VerifyEnumerator(((IEnumerable)map.Values).GetEnumerator(), new object[] { "a", "b", "c", "d" }, () => map.Remove(1));
        VerifyEnumerator(((IEnumerable<string>)map.Values).GetEnumerator(), new object[] { "b", "c", "d" }, () => map.Clear());
    }

    [Fact]
    public void DisposedEnumeratorsStopAndCanBeReset()
    {
        var map = new OrderedMap<int, int>();
        map.Add(1, 10);
        map.Add(2, 20);

        var enumerator = map.GetEnumerator();
        enumerator.MoveNext().IsTrue();
        enumerator.Dispose();
        enumerator.MoveNext().IsFalse();
        ResetInPlace(ref enumerator); // Without boxing a copy.
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Key.Is(1);

        var keys = map.Keys.GetEnumerator();
        keys.MoveNext().IsTrue();
        keys.Dispose();
        keys.MoveNext().IsFalse();

        var values = map.Values.GetEnumerator();
        values.MoveNext().IsTrue();
        values.Dispose();
        values.MoveNext().IsFalse();
    }

    [Fact]
    public void ValidateDetectsCorruption()
    {
        // Root color.
        var map = CreateMap(7);
        var root = GetRoot(map);
        SetColor(root, Red);
        map.Validate().IsFalse();

        // Root parent.
        map = CreateMap(7);
        root = GetRoot(map);
        SetLink(root, "Parent", root.Next);
        map.Validate().IsFalse();

        // Count.
        map = CreateMap(7);
        typeof(OrderedMap<int, int>).GetField("count", InstanceFlags)!.SetValue(map, 8);
        map.Validate().IsFalse();

        // Key order.
        map = CreateMap(7);
        typeof(OrderedMap<int, int>.Node).GetProperty("Key")!.SetValue(map.Last, 0);
        map.Validate().IsFalse();

        // Parent links of the left and right children, also below the root.
        map = CreateMap(15);
        SetLink(GetLink(GetRoot(map), "Left")!, "Parent", null);
        map.Validate().IsFalse();
        map = CreateMap(15);
        SetLink(GetLink(GetRoot(map), "Right")!, "Parent", null);
        map.Validate().IsFalse();
        map = CreateMap(15);
        var left = GetLink(GetRoot(map), "Left")!;
        SetLink(GetLink(left, "Left")!, "Parent", null);
        map.Validate().IsFalse();

        // Red node with a red child.
        map = CreateMap(15);
        left = GetLink(GetRoot(map), "Left")!;
        SetColor(left, Red);
        SetColor(GetLink(left, "Left")!, Red);
        map.Validate().IsFalse();

        // Black height: a red leaf turned black, which also propagates the mismatch through its ancestors.
        map = CreateMap(15);
        var redLeaf = EnumerateNodes(map).First(x => GetColor(x).Equals(Red) &&
            GetLink(x, "Left") is null &&
            GetLink(x, "Right") is null &&
            !ReferenceEquals(GetLink(x, "Parent"), GetRoot(map)));
        SetColor(redLeaf, Black);
        map.Validate().IsFalse();

        map = CreateMap(15);
        map.Validate().IsTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultComparerOrdersNullFirstOrLastWhenReversed(bool reverse)
    {
        var map = new OrderedMap<string?, int>(reverse);
        map.Add("b", 2);
        map.Add(null, 0);
        map.Add("a", 1);
        map.Validate().IsTrue();
        map.Keys.ToArray().Is(reverse ? new[] { "b", "a", null } : new[] { null, "a", "b" });
        map.FindNode(null)!.Value.Is(0);
        (reverse ? map.Last : map.First)!.Key.IsNull();

        var nullable = new OrderedSet<int?>(new int?[] { 2, null, 1 }, reverse);
        nullable.Validate().IsTrue();
        nullable.ToArray().Is(reverse ? new int?[] { 2, 1, null } : new int?[] { null, 1, 2 });
        nullable.Contains(null).IsTrue();
    }

    [Fact]
    public void EveryRemovalKeepsTreeValid()
    {
        var random = new Random(77);
        for (var size = 1; size <= 40; size++)
        {
            // Remove each key from a fresh tree, which covers every removal position for each tree shape.
            var keys = Enumerable.Range(0, size).ToArray();
            for (var target = 0; target < size; target++)
            {
                var map = new OrderedMap<int, int>();
                foreach (var key in keys)
                {
                    map.Add(key, key);
                }

                map.Remove(target).IsTrue();
                map.Validate().IsTrue();
                map.Keys.ToArray().Is(keys.Where(x => x != target));
            }

            for (var order = 0; order < 3; order++)
            {
                var map = new OrderedMap<int, int>(order == 1);
                foreach (var key in keys.OrderBy(_ => random.Next()))
                {
                    map.Add(key, key);
                }

                var removal = order switch
                {
                    0 => keys,
                    1 => keys.Reverse().ToArray(),
                    _ => keys.OrderBy(_ => random.Next()).ToArray(),
                };

                var count = size;
                foreach (var key in removal)
                {
                    map.Remove(key).IsTrue();
                    count--;
                    map.Validate().IsTrue();
                    map.Count.Is(count);
                }

                map.First.IsNull();
            }
        }
    }

    [Theory]
    [InlineData(false, 101)]
    [InlineData(true, 202)]
    public void RandomOperationsMatchModelAndKeepTreeValid(bool reverse, int seed)
    {
        var random = new Random(seed);
        var comparer = Comparer<int>.Create((x, y) => reverse ? y.CompareTo(x) : x.CompareTo(y));
        var model = new SortedDictionary<int, int>(comparer);
        var map = new OrderedMap<int, int>(reverse);
        var unused = new Stack<OrderedMap<int, int>.Node>();
        for (var i = 0; i < 4000; i++)
        {
            var key = random.Next(200);
            switch (random.Next(5))
            {
                case 0:
                case 1:
                    var reuse = unused.Count > 0 && random.Next(2) == 0 ? unused.Pop() : null;
                    var result = reuse is null ? map.Add(key, i) : map.Add(key, i, reuse);
                    result.NewlyAdded.Is(model.TryAdd(key, i));
                    if (reuse is not null && !result.NewlyAdded)
                    {
                        unused.Push(reuse); // An existing key leaves the unused node untouched.
                    }

                    break;

                case 2:
                    map.Remove(key).Is(model.Remove(key));
                    break;

                case 3:
                    var node = map.FindNode(key);
                    if (node is not null)
                    {
                        map.RemoveNode(node);
                        node.IsUnused.IsTrue();
                        model.Remove(key);
                        unused.Push(node);
                    }

                    break;

                default:
                    var target = map.FindNode(key);
                    if (target is not null)
                    {
                        var newKey = random.Next(200);
                        var changed = newKey != key && !model.ContainsKey(newKey);
                        map.SetNodeKey(target, newKey).Is(changed);
                        if (changed)
                        {
                            model.Remove(key);
                            model.Add(newKey, target.Value);
                            map.FindNode(newKey).IsSameReferenceAs(target);
                        }
                    }

                    break;
            }

            map.Validate().IsTrue();
            map.Count.Is(model.Count);
        }

        map.ToArray().Is(model.ToArray());
    }

    [Fact]
    public void NodeToStringShowsColorAndValue()
    {
        var map = new OrderedMap<int, string?>();
        var node = map.Add(1, "a").Node;
        node.ToString().Is("Black: a");
        map.Add(2, null).Node.ToString().Is("Red: ");
        map.RemoveNode(node);
        node.ToString().Is("Unused: ");
    }

    [Fact]
    public void WarmedUpOperationsDoNotAllocate()
    {
        var map = new OrderedMap<int, int>();
        var set = new OrderedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 100; i++)
        {
            map.Add(i * 2, i);
            set.Add(i.ToString());
        }

        long sum = 0;
        var allocated = AllocationHelper.Measure(() => sum += Run(map, set));
        sum.IsNot(0L);
        map.Validate().IsTrue();
        set.Validate().IsTrue();

        AllocationHelper.SkipUnlessOptimized();
        allocated.Is(0L);

        static long Run(OrderedMap<int, int> map, OrderedSet<string> set)
        {
            long sum = 0;
            for (var i = 0; i < 200; i++)
            {
                if (map.TryGetValue(i, out var value))
                {
                    sum += value;
                }

                sum += map.ContainsKey(i + 1) ? 1 : 0;
                sum += map.GetLowerBound(i + 1)?.Key ?? 0;
                sum += map.GetUpperBound(i + 1)?.Key ?? 0;
            }

            foreach (var x in map)
            {
                sum += x.Key + x.Value;
            }

            foreach (var x in map.Keys)
            {
                sum += x;
            }

            foreach (var x in map.Values)
            {
                sum += x;
            }

            foreach (var x in set)
            {
                sum += x.Length;
            }

            // Node reuse and key changes that keep the node.
            var node = map.FindNode(10)!;
            map.RemoveNode(node);
            map.Add(10, 5, node);
            map.SetNodeKey(node, 11);
            map.SetNodeKey(node, 301);
            map.SetNodeKey(node, 10);
            var setNode = set.FindNode("5")!;
            set.RemoveNode(setNode);
            set.AddNode("5", setNode);
            return sum;
        }
    }

    private static OrderedMap<int, int> CreateMap(int count)
    {
        var map = new OrderedMap<int, int>();
        for (var i = 1; i <= count; i++)
        {
            map.Add(i, i);
        }

        map.Validate().IsTrue();
        return map;
    }

    private static IEnumerable<OrderedMap<int, int>.Node> EnumerateNodes(OrderedMap<int, int> map)
    {
        for (var node = map.First; node is not null; node = node.Next)
        {
            yield return node;
        }
    }

    private static OrderedMap<int, int>.Node GetRoot(OrderedMap<int, int> map)
        => (OrderedMap<int, int>.Node)typeof(OrderedMap<int, int>).GetField("root", InstanceFlags)!.GetValue(map)!;

    private static OrderedMap<int, int>.Node? GetLink(OrderedMap<int, int>.Node node, string name)
        => (OrderedMap<int, int>.Node?)typeof(OrderedMap<int, int>.Node).GetProperty(name, InstanceFlags)!.GetValue(node);

    private static void SetLink(OrderedMap<int, int>.Node node, string name, OrderedMap<int, int>.Node? value)
        => typeof(OrderedMap<int, int>.Node).GetProperty(name, InstanceFlags)!.SetValue(node, value);

    private static object GetColor(OrderedMap<int, int>.Node node)
        => typeof(OrderedMap<int, int>.Node).GetProperty("Color", InstanceFlags)!.GetValue(node)!;

    private static void SetColor(OrderedMap<int, int>.Node node, object color)
        => typeof(OrderedMap<int, int>.Node).GetProperty("Color", InstanceFlags)!.SetValue(node, color);

    private static void ResetInPlace<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct, IEnumerator
        => enumerator.Reset();

    private static void VerifyEnumerator(IEnumerator enumerator, object[] expected, Action modify)
    {
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (var pass = 0; pass < 2; pass++)
        {
            var actual = new List<object?>();
            while (enumerator.MoveNext())
            {
                actual.Add(enumerator.Current);
            }

            Assert.Equal(expected, actual.ToArray());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.Reset();
        }

        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is(expected[0]);
        modify();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        ((IDisposable)enumerator).Dispose();
    }

    private sealed record ObjectKey(int Value) : IComparable<object>
    {
        public int CompareTo(object? other) => this.Value.CompareTo(((ObjectKey)other!).Value);
    }
}
