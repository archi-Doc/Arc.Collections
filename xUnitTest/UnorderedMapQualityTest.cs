// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Model-based and contract tests for <see cref="UnorderedMap{TKey, TValue}"/>.
/// </summary>
public class UnorderedMapQualityTest
{
    private static readonly string?[] ValuePool = [null, "a", "b", "c"];

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    public void RandomOperationsMatchModel_CustomComparer(bool allowDuplicates, int seed)
    {
        // Equal hash codes for unequal keys, and unequal hash codes that share a bucket.
        var map = new UnorderedMap<int?, string?>(0, new ModuloComparer(), allowDuplicates);
        int?[] keyPool = [null, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6];
        RunModel(map, keyPool, seed, 1000);
    }

    [Theory]
    [InlineData(false, 5)]
    [InlineData(false, 6)]
    [InlineData(true, 7)]
    [InlineData(true, 8)]
    public void RandomOperationsMatchModel_DefaultComparer(bool allowDuplicates, int seed)
    {
        var map = new UnorderedMap<CollidingKey, string?>(allowDuplicates);
        var keyPool = new CollidingKey?[14];
        for (var i = 1; i < keyPool.Length; i++)
        {
            keyPool[i] = new CollidingKey(i - 1);
        }

        RunModel(map, keyPool, seed, 1000);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData((1 << 30) + 1)]
    [InlineData(int.MaxValue)]
    public void ConstructorsRejectCapacityOutOfRange(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedMap<int, int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedMap<int, int>(capacity, EqualityComparer<int>.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedMap<int, int>(capacity, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedMap<int, int>(capacity, null, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedSet<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnorderedSet<int>(capacity, null, true));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(1000, 1024)]
    public void ConstructorsRoundCapacityUpToPowerOfTwo(int capacity, int expected)
    {
        new UnorderedMap<int, int>(capacity).Capacity.Is(expected);
        new UnorderedMap<int, int>(capacity, IdentityComparer.Instance).Capacity.Is(expected);
        new UnorderedMap<int, int>(capacity, true).Capacity.Is(expected);
        new UnorderedSet<int>(capacity).Capacity.Is(expected);
    }

    [Fact]
    public void ConstructorsStoreComparerAndDuplicateOption()
    {
        new UnorderedMap<string, int>().Comparer.IsSameReferenceAs(EqualityComparer<string>.Default);
        new UnorderedMap<string, int>(EqualityComparer<string>.Default).Comparer.IsSameReferenceAs(EqualityComparer<string>.Default);
        new UnorderedMap<string, int>(StringComparer.OrdinalIgnoreCase).Comparer.IsSameReferenceAs(StringComparer.OrdinalIgnoreCase);

        var withCapacity = new UnorderedMap<string, int>(10, StringComparer.OrdinalIgnoreCase);
        withCapacity.Comparer.IsSameReferenceAs(StringComparer.OrdinalIgnoreCase);
        withCapacity.AllowDuplicates.IsFalse();
        withCapacity.Capacity.Is(16);

        var duplicates = new UnorderedMap<string, int>(10, true);
        duplicates.AllowDuplicates.IsTrue();
        duplicates.Comparer.IsSameReferenceAs(EqualityComparer<string>.Default);
        duplicates.Add("x", 1).NewlyAdded.IsTrue();
        duplicates.Add("x", 2).NewlyAdded.IsTrue();
        duplicates.Count.Is(2);
    }

    [Fact]
    public void CustomComparerIsUsedForEveryLookup()
    {
        var map = new UnorderedMap<string?, int>(StringComparer.OrdinalIgnoreCase);
        map.Add("Key", 1);
        map.Add(null, 0);

        map.TryGetValue("KEY", out var value).IsTrue();
        value.Is(1);
        map.TryGetValue("other", out _).IsFalse();
        map.ContainsKey("kEy").IsTrue();
        map.Contains("KEY", 1).IsTrue();
        map.Contains("KEY", 2).IsFalse();
        map.FindNode("other", 1).Is(-1);

        map["KEY"] = 5;
        map.Count.Is(2);
        map["key"].Is(5);
        map["Other"] = 6;
        map.Count.Is(3);
        map["OTHER"].Is(6);

        map.EnumerateValues("KEY").ToArray().Is(5);
        map.Remove("KEY", 4).IsFalse();
        map.Remove("KEY", 5).IsTrue();
        map.ContainsKey("key").IsFalse();
        map[null].Is(0);
    }

    [Fact]
    public void NullKeyChainSurvivesResizeAndRelinking()
    {
        var map = new UnorderedMap<string?, int>(allowDuplicates: true);
        var nulls = new[] { map.Add(null, 1).NodeIndex, map.Add(null, 2).NodeIndex, map.Add(null, 3).NodeIndex };
        var a = map.Add("a", 10).NodeIndex;

        // The fifth node forces a resize from the null-key insertion path.
        map.Capacity.Is(4);
        var fourth = map.Add(null, 4).NodeIndex;
        map.Capacity.Is(8);

        for (var i = 0; i < 100; i++)
        {
            map.Add(i.ToString(), i);
        }

        Sorted(map.EnumerateNodes(null)).Is(nulls[0], nulls[1], nulls[2], fourth);
        map.FindNode(null, 2).Is(nulls[1]);
        map.FindNode(null, 99).Is(-1);
        map.Contains(null, 4).IsTrue();

        // Unlink a null node from the middle of the chain, then the head.
        map.RemoveNode(nulls[1]);
        Sorted(map.EnumerateValues(null)).Is(1, 3, 4);
        map.Remove(null, 4).IsTrue();
        map.Remove(null, 4).IsFalse();
        Sorted(map.EnumerateValues(null)).Is(1, 3);

        // Move a keyed node into the null chain and a null node back out of it.
        map.SetNodeKey(a, null).IsTrue();
        Sorted(map.EnumerateValues(null)).Is(1, 3, 10);
        map.ContainsKey("a").IsFalse();
        map.SetNodeKey(nulls[2], "a").IsTrue();
        map["a"].Is(3);
        Sorted(map.EnumerateValues(null)).Is(1, 10);
        map.SetNodeKey(nulls[0], null).IsFalse();
        map.Count.Is(103);
    }

    [Fact]
    public void NullKeyIndexerAddsThenUpdates()
    {
        var map = new UnorderedMap<string?, int>();
        map.TryGetValue(null, out _).IsFalse();
        Assert.Throws<KeyNotFoundException>(() => map[null]);

        map[null] = 1;
        map.Count.Is(1);
        map[null].Is(1);

        map[null] = 2;
        map.Count.Is(1);
        map[null].Is(2);

        map.Add(null, 3).NewlyAdded.IsFalse();
        map[null].Is(2);
        map.Remove(null).IsTrue();
        map.Remove(null).IsFalse();
        map.ContainsKey(null).IsFalse();
    }

    [Fact]
    public void SetNodeKeyReportsRejectedChanges()
    {
        var map = new UnorderedMap<CollidingKey?, int>();
        var a = map.Add(new(0), 0).NodeIndex;
        var b = map.Add(new(1), 1).NodeIndex; // Same hash code as key 0.
        var n = map.Add(null, 2).NodeIndex;

        map.SetNodeKey(-1, new(5)).IsFalse();
        map.SetNodeKey(map.UnsafeGetNodes().SlotCount, new(5)).IsFalse();
        map.SetNodeKey(a, new(0)).IsFalse(); // Unchanged key.
        map.SetNodeKey(a, new(1)).IsFalse(); // Duplicate of another node.
        map.SetNodeKey(a, null).IsFalse(); // Duplicate null key.
        map.SetNodeKey(n, null).IsFalse(); // Already null.

        map.RemoveNode(b);
        map.SetNodeKey(b, new(5)).IsFalse(); // Removed node.

        map.SetNodeKey(a, new(1)).IsTrue();
        map.FindFirstNode(new(1)).Is(a);
        map.ContainsKey(new(0)).IsFalse();
        map.SetNodeKey(n, new(0)).IsTrue();
        map.ContainsKey(null).IsFalse();
        map[new(0)].Is(2);
        map.Count.Is(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetNodeKeyHashesNewKeyOnce(bool allowDuplicates)
    {
        // Custom comparer path.
        var comparer = new CountingComparer();
        var map = new UnorderedMap<string?, int>(0, comparer, allowDuplicates);
        var a = map.Add("a", 1).NodeIndex;
        var b = map.Add("b", 2).NodeIndex;
        map.Add(null, 3);

        comparer.HashCalls = 0;
        map.SetNodeKey(a, "c").IsTrue();
        comparer.HashCalls.Is(1);

        comparer.HashCalls = 0;
        map.SetNodeKey(a, "b").Is(allowDuplicates);
        comparer.HashCalls.Is(1);

        comparer.HashCalls = 0;
        map.SetNodeKey(b, null).Is(allowDuplicates);
        comparer.HashCalls.Is(0);

        map.Count.Is(3);
        Sorted(map.EnumerateValues(null)).Is(allowDuplicates ? new[] { 2, 3 } : new[] { 3 });
        Sorted(map.EnumerateValues("b")).Is(allowDuplicates ? new[] { 1 } : new[] { 2 });
        map.ContainsKey("c").Is(!allowDuplicates);
        map.ContainsKey("a").IsFalse();

        // Default comparer path.
        var counter = new int[1];
        var keyed = new UnorderedMap<CountedKey, int>(allowDuplicates);
        var x = keyed.Add(new(1, counter), 1).NodeIndex;
        keyed.Add(new(2, counter), 2);

        counter[0] = 0;
        keyed.SetNodeKey(x, new(3, counter)).IsTrue();
        counter[0].Is(1);

        counter[0] = 0;
        keyed.SetNodeKey(x, new(2, counter)).Is(allowDuplicates);
        counter[0].Is(1);

        Sorted(keyed.EnumerateValues(new(2, counter))).Is(allowDuplicates ? new[] { 1, 2 } : new[] { 2 });
        keyed.ContainsKey(new(3, counter)).Is(!allowDuplicates);
        keyed.ContainsKey(new(1, counter)).IsFalse();
    }

    [Fact]
    public void RemoveNodeIgnoresInvalidIndexes()
    {
        var map = new UnorderedMap<string, string>();
        var a = map.Add("a", "A").NodeIndex;
        map.Add("b", "B");
        var enumerator = map.GetEnumerator();
        enumerator.MoveNext().IsTrue();

        map.RemoveNode(-1);
        map.RemoveNode(2);
        map.RemoveNode(int.MaxValue);
        map.Count.Is(2);
        enumerator.MoveNext().IsTrue(); // No-ops do not invalidate enumerators.

        map.RemoveNode(a);
        map.RemoveNode(a);
        map.Count.Is(1);

        // Removed nodes release their references.
        var (nodes, slotCount) = map.UnsafeGetNodes();
        slotCount.Is(2);
        nodes[a].IsUnused().IsTrue();
        nodes[a].IsInUse().IsFalse();
        nodes[a].Key.IsNull();
        nodes[a].Value.IsNull();
        map.SetNodeValue(a, "X").IsFalse();
    }

    [Fact]
    public void UnsafeGetNodesExposesActiveNodes()
    {
        var map = new UnorderedMap<int, string>();
        var a = map.Add(1, "one").NodeIndex;
        var b = map.Add(2, "two").NodeIndex;

        var (nodes, slotCount) = map.UnsafeGetNodes();
        slotCount.Is(2);
        nodes.Length.Is(map.Capacity);
        nodes[a].IsInUse().IsTrue();
        nodes[a].Key.Is(1);
        nodes[a].Value.Is("one");
        nodes[b].Key.Is(2);
        nodes[b].Value.Is("two");
        UnorderedMap<int, string>.Node.UnusedMarker.Is(-2);
    }

    [Fact]
    public void UnsafeChangeValueDoesNotInvalidateEnumerators()
    {
        var map = new UnorderedMap<int, int>();
        var a = map.Add(1, 10).NodeIndex;
        map.Add(2, 20);

        var visited = 0;
        foreach (var pair in map)
        {
            map.UnsafeChangeValue(a, 100);
            visited++;
        }

        visited.Is(2);
        map[1].Is(100);
    }

    [Fact]
    public void ClearResetsSlotsAndKeepsCapacity()
    {
        var empty = new UnorderedMap<int, int>();
        empty.Clear();
        empty.Count.Is(0);

        var map = new UnorderedMap<int?, string>();
        for (var i = 0; i < 20; i++)
        {
            map.Add(i, i.ToString());
        }

        map.Add(null, "null");
        for (var i = 0; i < 20; i++)
        {
            map.Remove(i).IsTrue();
        }

        map.Remove(null).IsTrue();
        map.Count.Is(0);
        map.UnsafeGetNodes().SlotCount.Is(21);

        var capacity = map.Capacity;
        map.Clear();
        map.Capacity.Is(capacity);
        map.UnsafeGetNodes().SlotCount.Is(0);
        map.ContainsKey(null).IsFalse();
        map.Add(7, "seven").NodeIndex.Is(0);
        map.Add(null, "null").NodeIndex.Is(1);
        map.Count.Is(2);
    }

    [Fact]
    public void ContainsValueHandlesNullAndRemovedNodes()
    {
        var map = new UnorderedMap<int, string?>();
        map.ContainsValue(null).IsFalse();
        var a = map.Add(1, null).NodeIndex;
        map.Add(2, "b");
        map.ContainsValue(null).IsTrue();

        map.RemoveNode(a);
        map.ContainsValue(null).IsFalse(); // The released slot holds null but is not in use.
        map.ContainsValue("b").IsTrue();
        map.ContainsValue("c").IsFalse();
    }

    [Fact]
    public void PairEnumeratorFollowsEnumeratorContract()
    {
        var map = CreateSample();
        CheckEnumeratorContract<KeyValuePair<string?, int>>(map, 4, () => map.Add("new", 9));

        map = CreateSample();
        CheckEnumeratorContract<KeyValuePair<string?, int>>(map, 4, () => map.SetNodeValue(map.FindFirstNode("a"), 7));
    }

    [Fact]
    public void KeyEnumeratorFollowsEnumeratorContract()
    {
        var map = CreateSample();
        CheckEnumeratorContract<string?>(map.Keys, 4, () => map.Remove("a"));
    }

    [Fact]
    public void ValueEnumeratorFollowsEnumeratorContract()
    {
        var map = CreateSample();
        CheckEnumeratorContract<int>(map.Values, 4, () => map["a"] = 5);
        map.Values.Sum().Is(5 + 2 + 3 + 4);
    }

    [Fact]
    public void NodeEnumeratorFollowsEnumeratorContract()
    {
        var map = CreateSample();
        CheckEnumeratorContract<int>(map.EnumerateNodes("dup"), 2, () => map.SetNodeKey(map.FindFirstNode("a"), "b"));

        map = CreateSample();
        CheckEnumeratorContract<int>(map.EnumerateNodes(null), 1, () => map.Clear());

        map = CreateSample();
        CheckEnumeratorContract<int>(map.EnumerateNodes("missing"), 0, () => map.Add("missing", 0));
    }

    [Fact]
    public void MatchedValueEnumeratorFollowsEnumeratorContract()
    {
        var map = CreateSample();
        CheckEnumeratorContract<int>(map.EnumerateValues("dup"), 2, () => map.RemoveNode(map.FindFirstNode("a")));
        map.EnumerateValues("dup").OrderBy(x => x).ToArray().Is(3, 4);

        map = CreateSample();
        CheckEnumeratorContract<int>(map.EnumerateValues(null), 1, () => map[null] = 1);
    }

    [Fact]
    public void NodeEnumeratorWalksCollidingChainsWithCustomComparer()
    {
        var map = new UnorderedMap<int?, int>(0, new ModuloComparer(), true);
        for (var i = 0; i < 30; i++)
        {
            map.Add(i % 10, i);
        }

        for (var key = 0; key < 10; key++)
        {
            Sorted(map.EnumerateValues(key)).Is(key, key + 10, key + 20);
            map.EnumerateNodes(key).Select(x => map.UnsafeGetNodes().Nodes[x].Key).All(x => x == key).IsTrue();
        }

        map.EnumerateNodes(11).Any().IsFalse();
        map.EnumerateValues(null).Any().IsFalse();
    }

    [Fact]
    public void StructEnumerationAndLookupsDoNotAllocate()
    {
        var map = new UnorderedMap<int?, int>(allowDuplicates: true);
        for (var i = 0; i < 64; i++)
        {
            map.Add(i % 16, i);
        }

        map.Add(null, -1);

        var expected = Run(map);
        long actual = 0;
        var allocated = AllocationHelper.Measure(() => actual = Run(map));

        actual.Is(expected);
        AllocationHelper.SkipUnlessOptimized();
        allocated.Is(0L);

        static long Run(UnorderedMap<int?, int> map)
        {
            long sum = 0;
            foreach (var pair in map)
            {
                sum += pair.Value;
            }

            foreach (var key in map.Keys)
            {
                sum += key ?? 0;
            }

            foreach (var value in map.Values)
            {
                sum += value;
            }

            foreach (var index in map.EnumerateNodes(3))
            {
                sum += index;
            }

            foreach (var value in map.EnumerateValues(null))
            {
                sum += value;
            }

            sum += map.TryGetValue(5, out var found) ? found : 0;
            sum += map.FindFirstNode(7);
            sum += map.FindNode(7, 23);

            var added = map.Add(100, 1).NodeIndex;
            map.RemoveNode(added);
            return sum;
        }
    }

    private static UnorderedMap<string?, int> CreateSample()
    {
        var map = new UnorderedMap<string?, int>(allowDuplicates: true);
        map.Add("a", 1);
        map.Add(null, 2);
        map.Add("dup", 3);
        map.Add("dup", 4);
        return map;
    }

    private static void CheckEnumeratorContract<T>(IEnumerable<T> enumerable, int expectedCount, Action mutate)
    {
        var expected = enumerable.ToList();
        expected.Count.Is(expectedCount);

        // Non-generic path: Current is guarded before the start, after the end, and after Reset.
        var nonGeneric = ((IEnumerable)enumerable).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => nonGeneric.Current);
        var items = new List<T>();
        while (nonGeneric.MoveNext())
        {
            items.Add((T)nonGeneric.Current!);
        }

        items.Is(expected);
        nonGeneric.MoveNext().IsFalse();
        Assert.Throws<InvalidOperationException>(() => nonGeneric.Current);

        nonGeneric.Reset();
        Assert.Throws<InvalidOperationException>(() => nonGeneric.Current);
        items.Clear();
        while (nonGeneric.MoveNext())
        {
            items.Add((T)nonGeneric.Current!);
        }

        items.Is(expected);

        // Generic path: every member that validates the version throws after a modification.
        var generic = enumerable.GetEnumerator();
        generic.MoveNext().Is(expectedCount > 0);
        mutate();
        Assert.Throws<InvalidOperationException>(() => generic.MoveNext());
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)generic).Current);
        Assert.Throws<InvalidOperationException>(() => generic.Reset());
        generic.Dispose();
    }

    private static int[] Sorted(IEnumerable<int> source)
    {
        var array = source.ToArray();
        Array.Sort(array);
        return array;
    }

    private static string Format<TKey>(TKey? key)
        => key?.ToString() ?? "<null>";

    // Compares two multisets; builds the detailed assertion message only on failure.
    private static void SameItems(IEnumerable<string> actual, IEnumerable<string> expected)
    {
        var a = actual.ToArray();
        var e = expected.ToArray();
        Array.Sort(a, StringComparer.Ordinal);
        Array.Sort(e, StringComparer.Ordinal);
        if (!a.AsSpan().SequenceEqual(e))
        {
            Assert.Equal(e, a);
        }
    }

    private static void RunModel<TKey>(UnorderedMap<TKey, string?> map, TKey?[] keyPool, int seed, int steps)
    {
        var random = new Random(seed);
        var comparer = map.Comparer;
        var allowDuplicates = map.AllowDuplicates;
        var live = new Dictionary<int, (TKey? Key, string? Value)>();

        for (var step = 0; step < steps; step++)
        {
            switch (random.Next(12))
            {
                case 0:
                case 1:
                case 2:
                    {
                        var key = NextKey();
                        var value = NextValue();
                        var matches = Matches(key);
                        var (index, added) = map.Add(key, value);
                        if (!allowDuplicates && matches.Count > 0)
                        {
                            added.IsFalse();
                            matches.Is(index);
                        }
                        else
                        {
                            added.IsTrue();
                            live.ContainsKey(index).IsFalse();
                            live[index] = (key, value);
                        }
                    }

                    break;

                case 3:
                    {
                        var key = NextKey();
                        var matches = Matches(key);
                        var first = map.FindFirstNode(key);
                        if (matches.Count == 0)
                        {
                            first.Is(-1);
                            map.Remove(key).IsFalse();
                        }
                        else
                        {
                            matches.Contains(first).IsTrue();
                            map.Remove(key).IsTrue();
                            live.Remove(first);
                        }
                    }

                    break;

                case 4:
                    {
                        var key = NextKey();
                        var value = NextValue();
                        var matches = Matches(key).Where(x => live[x].Value == value).ToList();
                        var found = map.FindNode(key, value);
                        map.Contains(key, value).Is(matches.Count > 0);
                        if (matches.Count == 0)
                        {
                            found.Is(-1);
                            map.Remove(key, value).IsFalse();
                        }
                        else
                        {
                            matches.Contains(found).IsTrue();
                            map.Remove(key, value).IsTrue();
                            live.Remove(found);
                        }
                    }

                    break;

                case 5:
                    {
                        var index = PickIndex();
                        map.RemoveNode(index);
                        live.Remove(index);
                    }

                    break;

                case 6:
                case 7:
                    {
                        var index = PickIndex();
                        var key = NextKey();
                        var expected = live.TryGetValue(index, out var entry) &&
                            !KeyEquals(entry.Key, key) &&
                            (allowDuplicates || !live.Any(x => x.Key != index && KeyEquals(x.Value.Key, key)));
                        map.SetNodeKey(index, key).Is(expected);
                        if (expected)
                        {
                            live[index] = (key, entry.Value);
                        }
                    }

                    break;

                case 8:
                    {
                        var index = PickIndex();
                        var value = NextValue();
                        var expected = live.TryGetValue(index, out var entry);
                        map.SetNodeValue(index, value).Is(expected);
                        if (expected)
                        {
                            live[index] = (entry.Key, value);
                        }
                    }

                    break;

                case 9:
                case 10:
                    {
                        var key = NextKey();
                        var value = NextValue();
                        var matches = Matches(key);
                        var first = map.FindFirstNode(key);
                        map[key] = value;
                        if (matches.Count > 0)
                        {
                            matches.Contains(first).IsTrue();
                            live[first] = (live[first].Key, value);
                        }
                        else
                        {
                            first.Is(-1);
                            var index = map.FindFirstNode(key);
                            live.ContainsKey(index).IsFalse();
                            live[index] = (key, value);
                        }
                    }

                    break;

                default:
                    if (random.Next(40) == 0)
                    {
                        map.Clear();
                        live.Clear();
                    }
                    else if (live.Count > 0)
                    {
                        var index = live.Keys.ElementAt(random.Next(live.Count));
                        var value = NextValue();
                        map.UnsafeChangeValue(index, value);
                        live[index] = (live[index].Key, value);
                    }

                    break;
            }

            Validate();
        }

        TKey? NextKey() => keyPool[random.Next(keyPool.Length)];

        string? NextValue() => ValuePool[random.Next(ValuePool.Length)];

        int PickIndex()
            => live.Count == 0 || random.Next(4) == 0
            ? random.Next(-2, map.UnsafeGetNodes().SlotCount + 2)
            : live.Keys.ElementAt(random.Next(live.Count));

        bool KeyEquals(TKey? x, TKey? y)
            => x is null ? y is null : y is not null && comparer.Equals(x, y);

        List<int> Matches(TKey? key)
            => live.Where(x => KeyEquals(x.Value.Key, key)).Select(x => x.Key).OrderBy(x => x).ToList();

        void Validate()
        {
            map.Count.Is(live.Count);

            var (nodes, slotCount) = map.UnsafeGetNodes();
            nodes.Length.Is(map.Capacity);
            live.Keys.All(x => x < slotCount).IsTrue();
            for (var i = 0; i < slotCount; i++)
            {
                if (live.TryGetValue(i, out var entry))
                {
                    nodes[i].IsInUse().IsTrue();
                    KeyEquals(nodes[i].Key, entry.Key).IsTrue();
                    nodes[i].Value.Is(entry.Value);
                }
                else
                {
                    nodes[i].IsUnused().IsTrue();
                    nodes[i].Value.IsNull();
                }
            }

            SameItems(map.Select(x => Format(x.Key) + "=" + x.Value), live.Values.Select(x => Format(x.Key) + "=" + x.Value));
            SameItems(map.Keys.Select(x => Format(x)), live.Values.Select(x => Format(x.Key)));
            SameItems(map.Values.Select(x => x ?? "<null>"), live.Values.Select(x => x.Value ?? "<null>"));

            foreach (var key in keyPool)
            {
                var matches = Matches(key);
                SameItems(map.EnumerateNodes(key).Select(x => x.ToString()), matches.Select(x => x.ToString()));
                SameItems(map.EnumerateValues(key).Select(x => x ?? "<null>"), matches.Select(x => live[x].Value ?? "<null>"));
                map.ContainsKey(key).Is(matches.Count > 0);

                var first = map.FindFirstNode(key);
                if (matches.Count == 0)
                {
                    first.Is(-1);
                    map.TryGetValue(key, out _).IsFalse();
                }
                else
                {
                    matches.Contains(first).IsTrue();
                    map.TryGetValue(key, out var value).IsTrue();
                    value.Is(live[first].Value);
                    map[key].Is(live[first].Value);
                }
            }

            foreach (var value in ValuePool)
            {
                map.ContainsValue(value).Is(live.Values.Any(x => x.Value == value));
            }
        }
    }

    private sealed class ModuloComparer : IEqualityComparer<int?>
    {
        public bool Equals(int? x, int? y) => x == y;

        // Null keys never reach the comparer.
        public int GetHashCode(int? obj) => obj!.Value % 3;
    }

    private sealed class CountingComparer : IEqualityComparer<string?>
    {
        public int HashCalls { get; set; }

        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string obj)
        {
            this.HashCalls++;
            return StringComparer.Ordinal.GetHashCode(obj);
        }
    }

    // Counts GetHashCode calls in a counter shared by all keys of one test.
    private sealed class CountedKey : IEquatable<CountedKey>
    {
        private readonly int[] counter;

        public CountedKey(int value, int[] counter)
        {
            this.Value = value;
            this.counter = counter;
        }

        public int Value { get; }

        public bool Equals(CountedKey? other) => other is not null && other.Value == this.Value;

        public override bool Equals(object? obj) => this.Equals(obj as CountedKey);

        public override int GetHashCode()
        {
            this.counter[0]++;
            return this.Value;
        }
    }

    private sealed class IdentityComparer : IEqualityComparer<int>
    {
        public static readonly IdentityComparer Instance = new();

        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj;
    }

    // Keys 2n and 2n + 1 share a hash code; other keys can still share a bucket.
    private sealed class CollidingKey : IEquatable<CollidingKey>
    {
        public CollidingKey(int value)
        {
            this.Value = value;
        }

        public int Value { get; }

        public bool Equals(CollidingKey? other) => other is not null && other.Value == this.Value;

        public override bool Equals(object? obj) => this.Equals(obj as CollidingKey);

        public override int GetHashCode() => this.Value >> 1;

        public override string ToString() => this.Value.ToString();
    }
}
