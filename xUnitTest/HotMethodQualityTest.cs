using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arc.Collections;
using Arc.Collections.HotMethod;
using Xunit;

namespace XunitTest;

public class HotMethodQualityTest
{
    private static readonly DateTime[] DateTimeDomain =
    [
        DateTime.MinValue,
        new DateTime(1, DateTimeKind.Utc),
        new DateTime(5, DateTimeKind.Utc),
        new DateTime(5, DateTimeKind.Local), // Kind is ignored by the default comparer.
        new DateTime(5, DateTimeKind.Unspecified),
        new DateTime(2000, 1, 1),
        new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DateTime.MaxValue.AddTicks(-1),
        DateTime.MaxValue,
    ];

    private static readonly double[] DoubleDomain =
    [
        double.NaN,
        BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8_0000_0000_0001)), // Another NaN payload.
        double.NegativeInfinity,
        double.MinValue,
        -1.5,
        -double.Epsilon,
        -0d,
        0d,
        double.Epsilon,
        1.5,
        double.MaxValue,
        double.PositiveInfinity,
    ];

    private static readonly float[] SingleDomain =
    [
        float.NaN,
        BitConverter.Int32BitsToSingle(unchecked((int)0xFFC0_0001)), // Another NaN payload.
        float.NegativeInfinity,
        float.MinValue,
        -1.5f,
        -float.Epsilon,
        -0f,
        0f,
        float.Epsilon,
        1.5f,
        float.MaxValue,
        float.PositiveInfinity,
    ];

    [Fact]
    public void ResolverReturnsSpecializationsOnlyForSupportedTypesAndDefaultComparers()
    {
        AssertSupported<byte>();
        AssertSupported<sbyte>();
        AssertSupported<ushort>();
        AssertSupported<short>();
        AssertSupported<uint>();
        AssertSupported<int>();
        AssertSupported<ulong>();
        AssertSupported<long>();
        AssertSupported<UInt128>();
        AssertSupported<Int128>();
        AssertSupported<float>();
        AssertSupported<double>();
        AssertSupported<DateTime>();

        AssertUnsupported<string>();
        AssertUnsupported<int?>();
        AssertUnsupported<DayOfWeek>();
        AssertUnsupported<char>();
        AssertUnsupported<decimal>();
        AssertUnsupported<Half>();
        AssertUnsupported<DateTimeOffset>();
        AssertUnsupported<TimeSpan>();
    }

    [Fact]
    public void EqualityComparerOverloadResolvesTheTreeSearch()
    {
        var hot = HotMethodResolver.Get<double, string>(EqualityComparer<double>.Default);
        Assert.NotNull(hot);
        Assert.Same(HotMethodResolver.Get<double, string>(Comparer<double>.Default), hot);

        var map = new OrderedMap<double, string>();
        map.Add(1.5, "a");
        var node = map.First!;
        Assert.Equal((0, node), hot.SearchNode(node, 1.5));
        Assert.Equal((-1, node), hot.SearchNode(node, double.NaN));
        Assert.Equal((1, node), hot.SearchNode(node, 2));

        Assert.Null(HotMethodResolver.Get<double, string>(EqualityComparer<double>.Create((x, y) => x == y, x => x.GetHashCode())));
        Assert.Null(HotMethodResolver.Get<string, string>(EqualityComparer<string>.Default));
    }

    [Fact]
    public void BoundsMatchDefaultComparerOnRandomSpans()
    {
        VerifyBounds(Domain(x => (byte)(x + 128), byte.MinValue, byte.MaxValue));
        VerifyBounds(Domain(x => (sbyte)x, sbyte.MinValue, sbyte.MaxValue));
        VerifyBounds(Domain(x => (ushort)(x + 128), ushort.MinValue, ushort.MaxValue));
        VerifyBounds(Domain(x => (short)x, short.MinValue, short.MaxValue));
        VerifyBounds(Domain(x => (uint)(x + 128), uint.MinValue, uint.MaxValue, 1u << 31));
        VerifyBounds(Domain(x => x, int.MinValue, int.MaxValue));
        VerifyBounds(Domain(x => (ulong)(x + 128), ulong.MinValue, ulong.MaxValue, 1ul << 63));
        VerifyBounds(Domain(x => (long)x, long.MinValue, long.MaxValue));
        VerifyBounds(Domain(x => (UInt128)(x + 128), UInt128.MinValue, UInt128.MaxValue, UInt128.One << 64, UInt128.One << 127));
        VerifyBounds(Domain(x => (Int128)x, Int128.MinValue, Int128.MaxValue, Int128.One << 64, -(Int128.One << 64)));
        VerifyBounds(SingleDomain);
        VerifyBounds(DoubleDomain);
        VerifyBounds(DateTimeDomain);
    }

    [Fact]
    public void TreeSearchesFindKeysOrReturnTheInsertionParent()
    {
        foreach (var reverse in new[] { false, true })
        {
            VerifyTreeSearch(Domain(x => (byte)(x + 128), byte.MinValue, byte.MaxValue), reverse);
            VerifyTreeSearch(Domain(x => x, int.MinValue, int.MaxValue), reverse);
            VerifyTreeSearch(Domain(x => (ulong)(x + 128), ulong.MinValue, ulong.MaxValue), reverse);
            VerifyTreeSearch(Domain(x => (Int128)x, Int128.MinValue, Int128.MaxValue), reverse);
            VerifyTreeSearch(SingleDomain, reverse);
            VerifyTreeSearch(DoubleDomain, reverse);
            VerifyTreeSearch(DateTimeDomain, reverse);
        }
    }

    [Fact]
    public void TreeSearchesOnEmptyTreeReturnNoParent()
    {
        var hot = HotMethodResolver.Get<int, int>(Comparer<int>.Default)!;
        Assert.Equal((0, (OrderedMap<int, int>.Node?)null), hot.SearchNode((OrderedMap<int, int>.Node?)null, 1));
        Assert.Equal((0, (OrderedMap<int, int>.Node?)null), hot.SearchNodeReverse((OrderedMap<int, int>.Node?)null, 1));
        Assert.Equal((0, (OrderedMultiMap<int, int>.Node?)null), hot.SearchNode((OrderedMultiMap<int, int>.Node?)null, 1));
        Assert.Equal((0, (OrderedMultiMap<int, int>.Node?)null), hot.SearchNodeReverse((OrderedMultiMap<int, int>.Node?)null, 1));
    }

    private static T[] Domain<T>(Func<int, T> convert, params T[] extremes)
        => Enumerable.Range(-40, 80).Select(convert).Concat(extremes).ToArray();

    private static void AssertSupported<T>()
    {
        var comparer = Comparer<T>.Default;
        Assert.NotNull(HotMethodResolver.Get(comparer));
        Assert.NotNull(HotMethodResolver.Get<T, int>(comparer));
        Assert.NotNull(HotMethodResolver.Get<T, int>(EqualityComparer<T>.Default));

        // Any other comparer disables the specialization, even one with the same ordering.
        var custom = Comparer<T>.Create(comparer.Compare);
        Assert.Null(HotMethodResolver.Get(custom));
        Assert.Null(HotMethodResolver.Get<T, int>(custom));
    }

    private static void AssertUnsupported<T>()
    {
        Assert.Null(HotMethodResolver.Get(Comparer<T>.Default));
        Assert.Null(HotMethodResolver.Get<T, int>(Comparer<T>.Default));
        Assert.Null(HotMethodResolver.Get<T, int>(EqualityComparer<T>.Default));
    }

    private static void VerifyBounds<T>(T[] domain)
    {
        var comparer = Comparer<T>.Default;
        var hot = HotMethodResolver.Get(comparer)!;
        var random = new Random(12345);
        for (var length = 0; length <= 70; length++)
        {
            // Draw from a small subset so that runs of equal elements are common.
            var subset = domain.OrderBy(_ => random.Next()).Take(1 + random.Next(domain.Length)).ToArray();
            var array = Enumerable.Range(0, length).Select(_ => subset[random.Next(subset.Length)]).ToArray();
            Array.Sort(array, comparer);

            // A slice checks that results are relative to the span start.
            var start = length == 0 ? 0 : random.Next(length);
            var span = new ReadOnlySpan<T>(array, start, length - start);
            foreach (var value in domain)
            {
                var below = 0;
                var notAbove = 0;
                foreach (var x in span)
                {
                    var c = comparer.Compare(x, value);
                    below += c < 0 ? 1 : 0;
                    notAbove += c <= 0 ? 1 : 0;
                }

                Assert.Equal(below, hot.LowerBound(span, value));
                Assert.Equal(notAbove, hot.UpperBoundExclusive(span, value));
            }
        }
    }

    private static void VerifyTreeSearch<T>(T[] domain, bool reverse)
    {
        var comparer = Comparer<T>.Default;
        var treeComparer = reverse ? Comparer<T>.Create((x, y) => comparer.Compare(y, x)) : comparer;
        var random = new Random(678);
        for (var round = 0; round < 8; round++)
        {
            var map = new OrderedMap<T, int>(reverse);
            var multi = new OrderedMultiMap<T, int>(reverse);
            var keyCount = round == 0 ? 1 : 1 + random.Next(domain.Length);
            foreach (var key in domain.OrderBy(_ => random.Next()).Take(keyCount))
            {
                map.Add(key, 0);
                multi.Add(key, 0);
                multi.Add(key, 1);
            }

            Assert.NotNull(map.HotTreeMethod);
            IHotTreeMethod<T, int> hot = map.HotTreeMethod;
            Assert.Same(hot, multi.HotTreeMethod);

            var keys = map.Keys.ToArray(); // Distinct keys in tree order.
            var root = TreeLinks<OrderedMap<T, int>.Node>.Root(map);
            var multiRoot = TreeLinks<OrderedMultiMap<T, int>.Node>.Root(multi);
            foreach (var key in domain)
            {
                var (cmp, node) = SearchMap(root, key);
                VerifySearchResult(keys, treeComparer, key, root, cmp, node, x => x.Key);

                (cmp, var multiNode) = SearchMulti(multiRoot, key);
                VerifySearchResult(keys, treeComparer, key, multiRoot, cmp, multiNode, x => x.Key);
            }

            (int Comparison, OrderedMap<T, int>.Node? Node) SearchMap(OrderedMap<T, int>.Node target, T key)
                => reverse ? hot.SearchNodeReverse(target, key) : hot.SearchNode(target, key);

            (int Comparison, OrderedMultiMap<T, int>.Node? Node) SearchMulti(OrderedMultiMap<T, int>.Node target, T key)
                => reverse ? hot.SearchNodeReverse(target, key) : hot.SearchNode(target, key);
        }
    }

    private static void VerifySearchResult<T, TNode>(T[] keys, IComparer<T> treeComparer, T key, TNode root, int cmp, TNode? node, Func<TNode, T> getKey)
        where TNode : class
    {
        Assert.NotNull(node);

        // The result must be exactly where a plain descent from the root stops.
        var expected = Descend(root, key, treeComparer, getKey);
        Assert.Equal(expected.Comparison, cmp);
        Assert.Same(expected.Node, node);

        var nodeKey = getKey(node);
        var index = Array.BinarySearch(keys, nodeKey, treeComparer);
        Assert.True(index >= 0);
        if (cmp == 0)
        {// Found.
            Assert.Equal(0, treeComparer.Compare(key, nodeKey));
        }
        else if (cmp < 0)
        {// The key belongs in the empty left slot: between the predecessor and the node.
            Assert.Equal(-1, cmp);
            Assert.Null(TreeLinks<TNode>.Left(node));
            Assert.True(treeComparer.Compare(key, nodeKey) < 0);
            Assert.True(index == 0 || treeComparer.Compare(keys[index - 1], key) < 0);
        }
        else
        {// The key belongs in the empty right slot: between the node and the successor.
            Assert.Equal(1, cmp);
            Assert.Null(TreeLinks<TNode>.Right(node));
            Assert.True(treeComparer.Compare(key, nodeKey) > 0);
            Assert.True(index == keys.Length - 1 || treeComparer.Compare(key, keys[index + 1]) < 0);
        }
    }

    private static (int Comparison, TNode? Node) Descend<T, TNode>(TNode root, T key, IComparer<T> treeComparer, Func<TNode, T> getKey)
        where TNode : class
    {
        TNode? node = root;
        TNode? parent = null;
        var cmp = 0;
        while (node is not null)
        {
            var c = treeComparer.Compare(key, getKey(node));
            if (c == 0)
            {
                return (0, node);
            }

            parent = node;
            cmp = c < 0 ? -1 : 1;
            node = cmp < 0 ? TreeLinks<TNode>.Left(node) : TreeLinks<TNode>.Right(node);
        }

        return (cmp, parent);
    }

    /// <summary>
    /// Reads the internal tree links of <see cref="OrderedMap{TKey, TValue}"/> and <see cref="OrderedMultiMap{TKey, TValue}"/> nodes.
    /// </summary>
    private static class TreeLinks<TNode>
        where TNode : class
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly PropertyInfo LeftProperty = typeof(TNode).GetProperty("Left", Flags)!;
        private static readonly PropertyInfo RightProperty = typeof(TNode).GetProperty("Right", Flags)!;

        public static TNode Root(object tree)
            => (TNode)tree.GetType().GetField("root", Flags)!.GetValue(tree)!;

        public static TNode? Left(TNode node) => (TNode?)LeftProperty.GetValue(node);

        public static TNode? Right(TNode node) => (TNode?)RightProperty.GetValue(node);
    }
}
