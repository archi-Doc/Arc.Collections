// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class OrderedMultiMapQualityTest
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private interface IShape
    {
        int Size { get; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeComparableKeysCompareLikeSearch(bool reverse)
    {
        // Comparer<IShape>.Default cannot compare shapes; searches use the runtime IComparable<IShape>,
        // and so must SetNodeKey, GetRange, and Validate.
        var map = new OrderedMultiMap<IShape?, int>(reverse);
        for (var i = 0; i < 21; i++)
        {
            map.Add(new Shape(i % 7), i);
        }

        map.Add(null, -1);

        var head = map.FindFirstNode(new Shape(3))!;
        Assert.True(map.SetNodeKey(head, new Shape(5)));
        Assert.False(map.SetNodeKey(head, new Shape(5)));
        var single = map.Add(new Shape(20), 100).Node;
        Assert.True(map.SetNodeKey(single, new Shape(15)));
        Assert.True(map.SetNodeKey(single, null));
        Assert.True(map.Validate());

        Assert.Equal(new[] { 10, 17 }, map.EnumerateValues(new Shape(3)));
        Assert.Equal(new[] { 5, 12, 19, 3 }, map.EnumerateValues(new Shape(5)));
        Assert.Equal(new[] { -1, 100 }, map.EnumerateValues(null));

        var (lower, upper) = map.GetRange(reverse ? new Shape(4) : new Shape(2), reverse ? new Shape(2) : new Shape(4));
        Assert.Equal(reverse ? 4 : 2, lower!.Key!.Size);
        Assert.Equal(reverse ? 2 : 4, upper!.Key!.Size);
        Assert.Equal((null, null), map.GetRange(reverse ? new Shape(2) : new Shape(4), reverse ? new Shape(4) : new Shape(2)));

        var set = new OrderedMultiSet<IShape>(reverse);
        var node = set.Add(new Shape(1));
        set.Add(new Shape(1));
        set.Add(new Shape(2));
        Assert.True(set.SetNodeKey(node, new Shape(3)));
        Assert.True(set.Validate());
        Assert.Equal(reverse ? new[] { 3, 2, 1 } : new[] { 1, 2, 3 }, set.Select(x => x.Size));

        // As documented on the Comparer properties, the default comparer itself cannot order these keys.
        Assert.Same(Comparer<IShape?>.Default, map.Comparer);
        Assert.Throws<ArgumentException>(() => map.Comparer.Compare(new Shape(1), new Shape(2)));
        Assert.Throws<ArgumentException>(() => set.Comparer.Compare(new Shape(1), new Shape(2)));
    }

    [Fact]
    public void NodeNavigationCrossesDuplicateGroups()
    {
        var map = new OrderedMultiMap<int, string>();
        foreach (var key in new[] { 4, 2, 6, 1, 3, 5, 7 })
        {
            map.Add(key, key.ToString());
        }

        map.Add(3, "3b");
        map.Add(3, "3c");
        map.Add(4, "4b");
        map.Add(6, "6b");
        string[] expected = ["1", "2", "3", "3b", "3c", "4", "4b", "5", "6", "6b", "7"];

        var forward = new List<string>();
        for (var node = map.First; node is not null; node = node.Next)
        {
            forward.Add(node.Value);
        }

        var backward = new List<string>();
        for (var node = map.Last; node is not null; node = node.Previous)
        {
            backward.Add(node.Value);
        }

        Assert.Equal(expected, forward);
        Assert.Equal(expected.Reverse(), backward);
        Assert.Equal("3c", map.FindNode(3, "3b")!.Next!.Value);
        Assert.Equal("4b", map.FindFirstNode(5)!.Previous!.Value);
        Assert.Equal("3c", map.FindFirstNode(4)!.Previous!.Value);
        Assert.Equal("6b", map.Last!.Previous!.Value);
    }

    [Fact]
    public void ValueChangesKeepEnumeratorsValid()
    {
        var map = new OrderedMultiMap<int, string?>();
        var a = map.Add(1, "A").Node;
        var b = map.Add(1, "B").Node;
        map.Add(2, null);
        Assert.Equal("Black: A", a.ToString());
        Assert.Equal("LinkedList: B", b.ToString());

        var enumerator = map.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        a.UnsafeChangeValue("X");
        map.SetNodeValue(b, "Y");
        Assert.True(enumerator.MoveNext());
        Assert.Equal(KeyValuePair.Create(1, (string?)"Y"), enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Equal(new[] { "X", "Y", null }, map.Values);
        Assert.EndsWith(": ", map.Last!.ToString());

        map.RemoveNode(a);
        map.SetNodeValue(a, "Z");
        Assert.Null(a.Value);
        Assert.Equal(new[] { "Y", null }, map.Values);
    }

    [Fact]
    public void HotTreeMethodIsResolvedOnlyForDefaultPrimitiveComparers()
    {
        Assert.NotNull(new OrderedMultiMap<int, int>().HotTreeMethod);
        Assert.NotNull(new OrderedMultiMap<double, int>(reverse: true).HotTreeMethod);
        Assert.Null(new OrderedMultiMap<int, int>(Comparer<int>.Create((x, y) => x.CompareTo(y))).HotTreeMethod);
        Assert.Null(new OrderedMultiMap<string, int>().HotTreeMethod);
        Assert.Same(Comparer<int>.Default, new OrderedMultiMap<int, int>((IComparer<int>?)null).Comparer);
    }

    [Fact]
    public void ConstructorsCopyDictionaries()
    {
        var dictionary = new Dictionary<string, int> { ["b"] = 2, ["A"] = 1, ["c"] = 3 };

        var map = new OrderedMultiMap<string, int>(dictionary);
        Assert.Equal(new[] { "A", "b", "c" }, map.Keys);
        Assert.False(map.IsReversed);

        map = new OrderedMultiMap<string, int>(dictionary, reverse: true);
        Assert.Equal(new[] { "c", "b", "A" }, map.Keys);
        Assert.True(map.IsReversed);

        map = new OrderedMultiMap<string, int>(dictionary, StringComparer.Ordinal);
        Assert.Equal(new[] { "A", "b", "c" }, map.Keys);
        Assert.Same(StringComparer.Ordinal, map.Comparer);
        Assert.True(map.Validate());

        Assert.Throws<ArgumentNullException>(() => new OrderedMultiMap<string, int>((IDictionary<string, int>)null!));
        Assert.Throws<ArgumentNullException>(() => new OrderedMultiMap<string, int>(null!, StringComparer.Ordinal, true));
    }

    [Fact]
    public void ContainsValueUsesDefaultEquality()
    {
        var map = new OrderedMultiMap<int, string?>();
        Assert.False(map.ContainsValue(null));
        map.Add(1, "A");
        map.Add(1, "B");
        Assert.False(map.ContainsValue(null));
        Assert.True(map.ContainsValue("B"));
        Assert.False(map.ContainsValue("C"));

        var numbers = new OrderedMultiMap<int, int>();
        numbers.Add(1, 10);
        numbers.Add(1, 20);
        Assert.True(numbers.ContainsValue(20));
        Assert.False(numbers.ContainsValue(30));
    }

    [Fact]
    public void ClearMakesNodesReusable()
    {
        var map = new OrderedMultiMap<int, int>();
        var enumerator = map.GetEnumerator();
        map.Clear();
        Assert.False(enumerator.MoveNext()); // Clearing an empty map does not invalidate enumerators.

        var head = map.Add(1, 10).Node;
        var duplicate = map.Add(1, 11).Node;
        map.Add(2, 20);
        enumerator = map.GetEnumerator();
        map.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Equal(0, map.Count);
        Assert.True(map.Validate());

        Assert.Same(duplicate, map.Add(3, 30, duplicate).Node);
        Assert.Same(head, map.Add(3, 31, head).Node);
        Assert.Equal(new[] { 30, 31 }, map.EnumerateValues(3));
        Assert.NotSame(head, map.Add(4, 40, head).Node); // An active node is not reused.
        Assert.True(map.Validate());
    }

    [Fact]
    public void CopyToValidatesArguments()
    {
        var map = new OrderedMultiMap<int, int>();
        map.CopyTo(Array.Empty<KeyValuePair<int, int>>(), 0);
        map.Add(1, 1);
        map.Add(1, 2);

        Assert.Throws<ArgumentNullException>(() => map.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.CopyTo(new KeyValuePair<int, int>[2], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.CopyTo(new KeyValuePair<int, int>[2], 3));
        Assert.Throws<ArgumentException>(() => map.CopyTo(new KeyValuePair<int, int>[2], 1));

        var array = new KeyValuePair<int, int>[2];
        map.CopyTo(array, 0);
        Assert.Equal(new[] { KeyValuePair.Create(1, 1), KeyValuePair.Create(1, 2) }, array);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetNodeKeyMovesNodesTowardTheStart(bool reverse)
    {
        var map = new OrderedMultiMap<int, string>(reverse);
        int[] keys = reverse ? [50, 40, 30, 20, 10] : [10, 20, 30, 40, 50];
        foreach (var key in keys)
        {
            map.Add(key, key.ToString());
        }

        var step = reverse ? -1 : 1;
        var first = map.First!;
        Assert.True(map.SetNodeKey(first, keys[0] - (5 * step))); // No previous node.
        var node = map.FindFirstNode(keys[3])!;
        Assert.True(map.SetNodeKey(node, keys[2] + (5 * step))); // Fits after the previous node.
        Assert.Same(node, map.FindFirstNode(keys[2] + (5 * step)));
        Assert.True(map.SetNodeKey(node, keys[1])); // Joins the previous key.
        Assert.False(map.SetNodeKey(node, keys[1]));
        Assert.Equal(new[] { keys[1].ToString(), keys[3].ToString() }, map.EnumerateValues(keys[1]));
        Assert.True(map.Validate());

        map.RemoveNode(node);
        Assert.False(map.SetNodeKey(node, 0));
        Assert.Equal(4, map.Count);
    }

    [Fact]
    public void SetNodeKeyMovesGroupHeadBelowItself()
    {
        var map = new OrderedMultiMap<int, string>();
        map.Add(10, "10");
        map.Add(5, "5");
        var head = map.Add(15, "15a").Node;
        map.Add(15, "15b");

        // The new position is a child of the head itself, which the next duplicate replaces.
        Assert.True(map.SetNodeKey(head, 20));
        Assert.True(map.Validate());
        Assert.Same(head, map.FindFirstNode(20));
        Assert.Equal(new[] { "5", "10", "15b", "15a" }, map.Values);

        var next = map.Add(20, "20b").Node;
        Assert.True(map.SetNodeKey(head, 1));
        Assert.True(map.Validate());
        Assert.Same(next, map.FindFirstNode(20));
        Assert.Equal(new[] { "15a", "5", "10", "15b", "20b" }, map.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryRemovalKeepsTreeValid(bool duplicates)
    {
        var random = new Random(77);
        for (var size = 1; size <= 40; size++)
        {
            for (var order = 0; order < 3; order++)
            {
                var map = new OrderedMultiMap<int, int>();
                var keys = Enumerable.Range(0, size).ToArray();
                foreach (var key in keys.OrderBy(_ => random.Next()))
                {
                    map.Add(key, key);
                    if (duplicates && key % 3 == 0)
                    {
                        map.Add(key, -key);
                    }
                }

                var removal = order switch
                {
                    0 => keys,
                    1 => keys.Reverse().ToArray(),
                    _ => keys.OrderBy(_ => random.Next()).ToArray(),
                };

                var count = map.Count;
                foreach (var key in removal)
                {
                    while (map.Remove(key))
                    {
                        count--;
                        Assert.True(map.Validate());
                        Assert.Equal(count, map.Count);
                    }
                }

                Assert.Null(map.First);
                Assert.Equal(0, count);
            }
        }
    }

    [Fact]
    public void RemovingUnusedNodeIsNoOp()
    {
        var map = new OrderedMultiMap<int, int>();
        var node = map.Add(1, 1).Node;
        map.Add(2, 2);
        map.RemoveNode(node);

        var enumerator = map.GetEnumerator();
        map.RemoveNode(node);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, map.Count);
        Assert.False(map.Remove(1));
        Assert.False(map.Remove(2, 3));
    }

    [Fact]
    public void BoundsAndRangesHandleMissingKeys()
    {
        var map = new OrderedMultiMap<int, int>();
        Assert.Null(map.First);
        Assert.Null(map.Last);
        Assert.Null(map.GetLowerBound(1));
        Assert.Null(map.GetUpperBound(1));
        Assert.Equal((null, null), map.GetRange(0, 10));
        Assert.Null(map.FindNode(1, 1));

        map.Add(5, 50);
        map.Add(5, 51);
        map.Add(7, 70);
        Assert.Equal((null, null), map.GetRange(8, 9));
        Assert.Equal((null, null), map.GetRange(0, 4));
        Assert.Equal((null, null), map.GetRange(7, 5));
        Assert.Equal((null, null), map.GetRange(6, 6));
        var (lower, upper) = map.GetRange(5, 5);
        Assert.Equal(50, lower!.Value);
        Assert.Equal(51, upper!.Value);
        Assert.Throws<KeyNotFoundException>(() => map[6]);
    }

    [Fact]
    public void UntypedEnumeratorsFollowTheContract()
    {
        var map = Create();
        VerifyUntypedEnumerator(map, new object[] { KeyValuePair.Create(1, "A"), KeyValuePair.Create(1, "A2"), KeyValuePair.Create(2, "B") }, () => map.Add(3, "C"));
        map = Create();
        VerifyUntypedEnumerator(map.Keys, new object[] { 1, 1, 2 }, () => map.Add(3, "C"));
        map = Create();
        VerifyUntypedEnumerator(map.Values, new object[] { "A", "A2", "B" }, () => map.Remove(2));
        map = Create();
        VerifyUntypedEnumerator(map.EnumerateNodes(1), new object[] { map.FindNode(1, "A")!, map.FindNode(1, "A2")! }, () => map.Add(1, "A3"));
        map = Create();
        VerifyUntypedEnumerator(map.EnumerateValues(1), new object[] { "A", "A2" }, () => map.Remove(1, "A2"));

        IEnumerable<string> values = map.EnumerateValues(9);
        Assert.Empty(values);
        IEnumerable<OrderedMultiMap<int, string>.Node> empty = map.EnumerateNodes(9);
        Assert.Empty(empty);

        static OrderedMultiMap<int, string> Create()
        {
            var map = new OrderedMultiMap<int, string>();
            map.Add(2, "B");
            map.Add(1, "A");
            map.Add(1, "A2");
            return map;
        }
    }

    [Fact]
    public void StructEnumeratorsDetectModification()
    {
        var map = new OrderedMultiMap<int, int>();
        map.Add(1, 10);
        map.Add(1, 11);
        map.Add(2, 20);

        var values = new List<int>();
        foreach (var value in map.Values)
        {
            values.Add(value);
        }

        Assert.Equal(new[] { 10, 11, 20 }, values);

        var valueEnumerator = map.Values.GetEnumerator();
        Assert.True(valueEnumerator.MoveNext());
        map.Add(3, 30);
        Assert.Throws<InvalidOperationException>(() => valueEnumerator.MoveNext());

        var nodeEnumerator = map.EnumerateNodes(1).GetEnumerator();
        Assert.True(nodeEnumerator.MoveNext());
        map.Remove(3);
        Assert.Throws<InvalidOperationException>(() => nodeEnumerator.MoveNext());

        var matchedEnumerator = map.EnumerateValues(1).GetEnumerator();
        Assert.True(matchedEnumerator.MoveNext());
        Assert.Equal(10, matchedEnumerator.Current);
        Assert.True(matchedEnumerator.MoveNext());
        Assert.Equal(11, matchedEnumerator.Current);
        Assert.False(matchedEnumerator.MoveNext());
        Assert.False(matchedEnumerator.MoveNext());
        matchedEnumerator.Dispose();
    }

    [Fact]
    public void WarmedUpOperationsDoNotAllocate()
    {
        var map = new OrderedMultiMap<int, int>();
        for (var i = 0; i < 64; i++)
        {
            map.Add(i % 16, i);
        }

        var spare = map.Add(100, 100).Node;
        map.RemoveNode(spare);
        var allocated = AllocationHelper.Measure(() => Exercise(map, ref spare));

        Assert.Equal(64, map.Count);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static void Exercise(OrderedMultiMap<int, int> map, ref OrderedMultiMap<int, int>.Node spare)
        {
            var sum = 0;
            foreach (var pair in map)
            {
                sum += pair.Value;
            }

            foreach (var key in map.Keys)
            {
                sum += key;
            }

            foreach (var value in map.Values)
            {
                sum += value;
            }

            foreach (var node in map.EnumerateNodes(3))
            {
                sum += node.Value;
            }

            foreach (var value in map.EnumerateValues(4))
            {
                sum += value;
            }

            spare = map.Add(7, 700, spare).Node;
            Assert.True(map.Remove(7, 700));
            Assert.True(map.TryGetValue(5, out var found));
            Assert.True(sum > found);
        }
    }

    [Theory]
    [InlineData("EmptyCount")]
    [InlineData("RootRed")]
    [InlineData("RootParent")]
    [InlineData("CountHigh")]
    [InlineData("CountLow")]
    [InlineData("TreeNodeLinkedListColor")]
    [InlineData("TreeNodeUnused")]
    [InlineData("LeftParent")]
    [InlineData("RightParent")]
    [InlineData("DeepLeftParent")]
    [InlineData("Order")]
    [InlineData("ListNextNull")]
    [InlineData("ListPrevious")]
    [InlineData("ListColor")]
    [InlineData("ListKey")]
    [InlineData("RedRed")]
    [InlineData("RedRedLeft")]
    [InlineData("BlackHeight")]
    public void ValidateDetectsCorruption(string corruption)
    {
        var map = new OrderedMultiMap<int, int>();
        if (corruption == "EmptyCount")
        {
            SetMember(map, "count", 1);
            Assert.False(map.Validate());
            return;
        }

        // Insert 1-31 in a scrambled order so that red nodes appear as both left and right children.
        for (var i = 1; i <= 31; i++)
        {
            var key = i * 9 % 32;
            map.Add(key, key);
        }

        map.Add(1, 100);
        map.Add(1, 101);
        Assert.True(map.Validate());

        var root = GetMember(map, "root")!;
        var head = map.FindFirstNode(1)!;
        var duplicate = map.FindNode(1, 100)!;
        var treeNodes = Traverse(root).ToArray();
        switch (corruption)
        {
            case "RootRed":
                SetColor(root, "Red");
                break;
            case "RootParent":
                SetMember(root, "Parent", head);
                break;
            case "CountHigh":
                SetMember(map, "count", map.Count + 1);
                break;
            case "CountLow":
                SetMember(map, "count", 1);
                break;
            case "TreeNodeLinkedListColor":
                SetColor(map.FindFirstNode(20)!, "LinkedList");
                break;
            case "TreeNodeUnused":
                SetColor(map.FindFirstNode(20)!, "Unused");
                break;
            case "LeftParent":
                SetMember(GetMember(root, "Left")!, "Parent", null);
                break;
            case "RightParent":
                SetMember(GetMember(root, "Right")!, "Parent", null);
                break;
            case "DeepLeftParent":
                SetMember(GetMember(GetMember(root, "Left")!, "Left")!, "Parent", null);
                break;
            case "Order":
                SetMember(map.FindFirstNode(2)!, "Key", 100);
                break;
            case "ListNextNull":
                SetMember(map.FindNode(1, 101)!, "ListNext", null);
                break;
            case "ListPrevious":
                SetMember(duplicate, "ListPrevious", duplicate);
                break;
            case "ListColor":
                SetColor(duplicate, "Red");
                break;
            case "ListKey":
                SetMember(duplicate, "Key", 2);
                break;
            case "RedRed":
            case "RedRedLeft":
                // Push the red of a red node down to its two black children. Black heights stay equal, so only
                // the red-red check can reject the tree; every new red-red edge is a right (or left) edge.
                var redRedParent = treeNodes.First(x => IsRedRedCandidate(x, corruption == "RedRedLeft"));
                SetColor(redRedParent, "Black");
                foreach (var child in Children(redRedParent).ToArray())
                {
                    SetColor(child, "Red");
                }

                Assert.True(BlackHeight(root) > 0);
                break;
            case "BlackHeight":
                SetColor(treeNodes.First(x => IsColor(x, "Red") && !Children(x).Any()), "Black");
                break;
        }

        Assert.False(map.Validate());
    }

    [Fact]
    public void MultiSetUsesSuppliedComparer()
    {
        var set = new OrderedMultiSet<string>(StringComparer.OrdinalIgnoreCase, reverse: true);
        set.Add("b");
        set.Add("A");
        set.Add("a");
        Assert.Same(StringComparer.OrdinalIgnoreCase, set.Comparer);
        Assert.True(set.IsReversed);
        Assert.True(set.Contains("B"));
        Assert.Equal(2, set.GetCount("A"));
        Assert.Equal(new[] { "b", "A", "a" }, set);
        Assert.Equal(new[] { "A", "a" }, set.EnumerateNodes("a").Select(x => x.Key));
        Assert.Equal("a", set.LastNode!.Key);

        // Range bounds follow sort order, so the larger value comes first in a reversed set.
        var (lower, upper) = set.GetRange("B", "a");
        Assert.Equal("b", lower!.Key);
        Assert.Same(set.LastNode, upper);
        Assert.Equal((null, null), set.GetRange("a", "b"));

        var empty = new OrderedMultiSet<int>((IComparer<int>?)null);
        Assert.Same(Comparer<int>.Default, empty.Comparer);
        Assert.Null(empty.FirstNode);
        Assert.Null(empty.LastNode);
    }

    [Fact]
    public void MultiSetReleasesReusableNodes()
    {
        var set = new OrderedMultiSet<int>();
        var removed = set.Add(1);
        var cleared = set.Add(2);
        set.Add(2);
        set.RemoveNode(removed);
        Assert.Same(removed, set.Add(3, removed));
        Assert.Equal(new[] { 2, 2, 3 }, set);

        set.Clear();
        Assert.Equal(0, set.Count);
        Assert.Same(cleared, set.Add(4, cleared));
        Assert.Same(removed, set.Add(5, removed));
        Assert.NotSame(cleared, set.Add(6, cleared)); // An active node is not reused.
        Assert.Equal(new[] { 4, 5, 6 }, set);
        Assert.True(set.Validate());
    }

    private static void VerifyUntypedEnumerator(IEnumerable enumerable, IEnumerable<object> expected, Action modify)
    {
        var items = expected.ToArray();
        var enumerator = enumerable.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        var actual = new List<object?>();
        while (enumerator.MoveNext())
        {
            actual.Add(enumerator.Current);
        }

        Assert.Equal(items, actual);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(items[0], enumerator.Current);

        modify();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        ((IDisposable)enumerator).Dispose();
    }

    private static object? GetMember(object target, string name)
    {
        var property = target.GetType().GetProperty(name, InstanceFlags);
        return property is not null
            ? property.GetValue(target)
            : target.GetType().GetField(name, InstanceFlags)!.GetValue(target);
    }

    private static void SetMember(object target, string name, object? value)
    {
        var property = target.GetType().GetProperty(name, InstanceFlags);
        if (property is not null)
        {
            property.SetValue(target, value);
        }
        else
        {
            target.GetType().GetField(name, InstanceFlags)!.SetValue(target, value);
        }
    }

    private static void SetColor(object node, string color)
    {
        var property = node.GetType().GetProperty("Color", InstanceFlags)!;
        property.SetValue(node, Enum.Parse(property.PropertyType, color));
    }

    private static bool IsColor(object node, string color)
        => GetMember(node, "Color")!.ToString() == color;

    private static IEnumerable<object> Children(object node)
    {
        if (GetMember(node, "Left") is { } left)
        {
            yield return left;
        }

        if (GetMember(node, "Right") is { } right)
        {
            yield return right;
        }
    }

    private static bool IsRedRedCandidate(object node, bool redLeft)
    {
        var children = Children(node).ToArray();
        if (!IsColor(node, "Red") ||
            children.Length != 2 ||
            !children.All(x => IsColor(x, "Black")))
        {
            return false;
        }

        // Every red-red edge created by recoloring the children must be on the tested side.
        var (tested, other) = redLeft ? ("Left", "Right") : ("Right", "Left");
        return children.Any(x => HasRedChild(x, tested)) && !children.Any(x => HasRedChild(x, other));
    }

    private static bool HasRedChild(object node, string side)
        => GetMember(node, side) is { } child && IsColor(child, "Red");

    private static int BlackHeight(object? node)
    {
        if (node is null)
        {
            return 0;
        }

        var left = BlackHeight(GetMember(node, "Left"));
        var right = BlackHeight(GetMember(node, "Right"));
        if (left < 0 || left != right)
        {
            return -1;
        }

        return left + (IsColor(node, "Black") ? 1 : 0);
    }

    private static IEnumerable<object> Traverse(object node)
    {
        yield return node;
        foreach (var child in Children(node))
        {
            foreach (var x in Traverse(child))
            {
                yield return x;
            }
        }
    }

    private sealed record Shape(int Size) : IShape, IComparable<IShape>
    {
        public int CompareTo(IShape? other) => other is null ? 1 : this.Size.CompareTo(other.Size);
    }
}
