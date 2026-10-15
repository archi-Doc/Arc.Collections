// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Compares <see cref="OrderedMultiMap{TKey, TValue}"/> with a simple reference model under random operations.
/// </summary>
public class OrderedMultiMapQualityModelTest
{
    private static readonly string?[] Words = [null, "a", "b", "c", "d", "e", "f", "g", "h"];

    // Comparer<IKey>.Default cannot compare keys; the map uses their runtime IComparable<IKey>.
    private interface IKey
    {
        int Value { get; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Int32KeysUseHotMethod(bool reverse)
        => Run(r => r.Next(12), null, reverse, 1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecimalKeysUseDefaultValueComparer(bool reverse)
        => Run(r => (decimal)r.Next(12) / 4, null, reverse, 2);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullableKeysUseNullOrdering(bool reverse)
        => Run(r => r.Next(8) == 0 ? null : (int?)r.Next(10), null, reverse, 3);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StringKeysUseComparablePath(bool reverse)
        => Run(r => Words[r.Next(Words.Length)], null, reverse, 4);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StringKeysUseCustomComparer(bool reverse)
        => Run(r => Words[r.Next(Words.Length)], StringComparer.Ordinal, reverse, 5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EquivalentKeysShareDuplicateGroup(bool reverse)
        => Run(r => r.Next(40), Comparer<int>.Create((x, y) => (x % 6).CompareTo(y % 6)), reverse, 6);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterfaceKeysUseRuntimeComparable(bool reverse)
        => Run<IKey?>(
            r => r.Next(10) == 0 ? null : new Key(r.Next(10)),
            null,
            reverse,
            7,
            Comparer<IKey?>.Create((x, y) => x is null ? (y is null ? 0 : -1) : y is null ? 1 : x.Value.CompareTo(y.Value)));

    private static void Run<TKey>(Func<Random, TKey> nextKey, IComparer<TKey>? comparer, bool reverse, int seed, IComparer<TKey>? modelComparer = null)
    {
        var random = new Random(seed);
        var model = new Model<TKey>(modelComparer ?? comparer ?? Comparer<TKey>.Default, reverse);
        var map = new OrderedMultiMap<TKey, int>(comparer, reverse);
        var removed = new List<OrderedMultiMap<TKey, int>.Node>();
        var nextValue = 0;

        for (var step = 0; step < 600; step++)
        {
            var key = nextKey(random);
            var operation = random.Next(model.Entries.Count < 8 ? 4 : 12);
            switch (operation)
            {
                case <= 3:
                    {
                        // Add, sometimes reusing a removed node.
                        var value = nextValue++;
                        (OrderedMultiMap<TKey, int>.Node Node, bool NewlyAdded) result;
                        if (removed.Count > 0 && random.Next(2) == 0)
                        {
                            var reuse = removed[^1];
                            removed.RemoveAt(removed.Count - 1);
                            result = map.Add(key, value, reuse);
                            Assert.Same(reuse, result.Node);
                        }
                        else
                        {
                            result = map.Add(key, value);
                        }

                        Assert.True(result.NewlyAdded);
                        model.Add(result.Node, key, value);
                        break;
                    }

                case 4:
                    {
                        // Remove the first node with the key.
                        var expected = model.FirstWithKey(key);
                        Assert.Equal(expected is not null, map.Remove(key));
                        if (expected is not null)
                        {
                            model.Remove(expected);
                            removed.Add(expected.Node);
                        }

                        break;
                    }

                case 5:
                    {
                        // Remove by key and value.
                        var target = model.Entries[random.Next(model.Entries.Count)];
                        var value = random.Next(4) == 0 ? -1 : target.Value;
                        var expected = model.Entries.Where(x => model.Compare(x.Key, key) == 0 && x.Value == value).OrderBy(x => x.Sequence).FirstOrDefault();
                        Assert.Same(expected?.Node, map.FindNode(key, value));
                        Assert.Equal(expected is not null, map.Remove(key, value));
                        if (expected is not null)
                        {
                            model.Remove(expected);
                            removed.Add(expected.Node);
                        }

                        break;
                    }

                case 6:
                    {
                        // Remove a node directly; removing an unused node is a no-op.
                        if (removed.Count > 0 && random.Next(4) == 0)
                        {
                            map.RemoveNode(removed[0]);
                            break;
                        }

                        var target = model.Entries[random.Next(model.Entries.Count)];
                        map.RemoveNode(target.Node);
                        model.Remove(target);
                        removed.Add(target.Node);
                        break;
                    }

                case <= 9:
                    {
                        // Change a key.
                        if (removed.Count > 0 && random.Next(8) == 0)
                        {
                            Assert.False(map.SetNodeKey(removed[0], key));
                            break;
                        }

                        var target = model.Entries[random.Next(model.Entries.Count)];
                        var changed = model.Compare(target.Key, key) != 0;
                        Assert.Equal(changed, map.SetNodeKey(target.Node, key));
                        if (changed)
                        {
                            model.ChangeKey(target, key);
                        }

                        break;
                    }

                default:
                    {
                        // Change a value.
                        var target = model.Entries[random.Next(model.Entries.Count)];
                        var value = nextValue++;
                        map.SetNodeValue(target.Node, value);
                        target.Value = value;
                        break;
                    }
            }

            Verify(map, model, nextKey(random), nextKey(random));
        }
    }

    private static void Verify<TKey>(OrderedMultiMap<TKey, int> map, Model<TKey> model, TKey probe, TKey probe2)
    {
        Assert.True(map.Validate());
        var expected = model.Sorted();
        Assert.Equal(expected.Length, map.Count);
        Assert.Equal(expected.Select(x => KeyValuePair.Create(x.Key, x.Value)), map);
        Assert.Equal(expected.Select(x => x.Key), map.Keys);
        Assert.Equal(expected.Select(x => x.Value), map.Values);

        // Node navigation in both directions.
        var forward = new List<int>();
        for (var node = map.First; node is not null; node = node.Next)
        {
            forward.Add(node.Value);
        }

        var backward = new List<int>();
        for (var node = map.Last; node is not null; node = node.Previous)
        {
            backward.Add(node.Value);
        }

        backward.Reverse();
        Assert.Equal(expected.Select(x => x.Value), forward);
        Assert.Equal(expected.Select(x => x.Value), backward);

        foreach (var key in new[] { probe, probe2 })
        {
            var group = expected.Where(x => model.Compare(x.Key, key) == 0).ToArray();
            Assert.Equal(group.Select(x => x.Value), map.EnumerateValues(key));
            Assert.Equal(group.Select(x => x.Node), map.EnumerateNodes(key));
            Assert.Same(group.FirstOrDefault()?.Node, map.FindFirstNode(key));
            Assert.Equal(group.Length > 0, map.ContainsKey(key));
            Assert.Equal(group.Length > 0, map.TryGetValue(key, out var value));
            Assert.Equal(group.Length > 0 ? group[0].Value : 0, value);
            Assert.Same(model.LowerBound(expected, key)?.Node, map.GetLowerBound(key));
            Assert.Same(model.UpperBound(expected, key)?.Node, map.GetUpperBound(key));
        }

        var lower = model.LowerBound(expected, probe);
        var upper = model.UpperBound(expected, probe2);
        if (lower is null || upper is null || model.Compare(lower.Key, upper.Key) > 0)
        {
            lower = upper = null;
        }

        var range = map.GetRange(probe, probe2);
        Assert.Same(lower?.Node, range.Lower);
        Assert.Same(upper?.Node, range.Upper);
    }

    private sealed class Entry<TKey>
    {
        public Entry(OrderedMultiMap<TKey, int>.Node node, TKey key, int value, int sequence)
        {
            this.Node = node;
            this.Key = key;
            this.Value = value;
            this.Sequence = sequence;
        }

        public OrderedMultiMap<TKey, int>.Node Node { get; }

        public TKey Key { get; set; }

        public int Value { get; set; }

        public int Sequence { get; set; }
    }

    private sealed class Model<TKey>
    {
        private readonly IComparer<TKey> comparer;
        private readonly bool reverse;
        private int sequence;

        public Model(IComparer<TKey> comparer, bool reverse)
        {
            this.comparer = comparer;
            this.reverse = reverse;
        }

        public List<Entry<TKey>> Entries { get; } = new();

        public int Compare(TKey x, TKey y)
        {
            var cmp = Math.Sign(this.comparer.Compare(x, y));
            return this.reverse ? -cmp : cmp;
        }

        public void Add(OrderedMultiMap<TKey, int>.Node node, TKey key, int value)
            => this.Entries.Add(new(node, key, value, this.sequence++));

        public void Remove(Entry<TKey> entry)
            => Assert.True(this.Entries.Remove(entry));

        // A node that moves to another key joins the end of that key's duplicate group.
        public void ChangeKey(Entry<TKey> entry, TKey key)
        {
            entry.Key = key;
            entry.Sequence = this.sequence++;
        }

        public Entry<TKey>? FirstWithKey(TKey key)
            => this.Entries.Where(x => this.Compare(x.Key, key) == 0).OrderBy(x => x.Sequence).FirstOrDefault();

        public Entry<TKey>[] Sorted()
            => this.Entries.OrderBy(x => x.Key, Comparer<TKey>.Create(this.Compare)).ThenBy(x => x.Sequence).ToArray();

        public Entry<TKey>? LowerBound(Entry<TKey>[] sorted, TKey key)
            => sorted.FirstOrDefault(x => this.Compare(x.Key, key) >= 0);

        public Entry<TKey>? UpperBound(Entry<TKey>[] sorted, TKey key)
            => sorted.LastOrDefault(x => this.Compare(x.Key, key) <= 0);
    }

    private sealed record Key(int Value) : IKey, IComparable<IKey>
    {
        public int CompareTo(IKey? other) => other is null ? 1 : this.Value.CompareTo(other.Value);
    }
}
