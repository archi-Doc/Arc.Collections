using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class OrderedSetCoverageTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetsMatchReferenceCollections(bool reverse)
    {
        var comparer = Comparer<int>.Create((x, y) => reverse ? y.CompareTo(x) : x.CompareTo(y));
        var set = new OrderedSet<int>(reverse);
        var multi = new OrderedMultiSet<int>(reverse);
        var reference = new SortedSet<int>(comparer);
        var values = new List<int>();
        var random = new Random(501);
        for (var i = 0; i < 1000; i++)
        {
            var key = random.Next(30);
            if (random.Next(2) == 0)
            {
                Assert.Equal(reference.Add(key), set.Add(key));
                multi.Add(key);
                values.Add(key);
            }
            else
            {
                Assert.Equal(reference.Remove(key), set.Remove(key));
                Assert.Equal(values.Remove(key), multi.Remove(key));
            }

            Assert.True(set.Validate());
            Assert.True(multi.Validate());
            Assert.Equal(reference, set);
            Assert.Equal(values.OrderBy(x => x, comparer), multi);
        }

        Assert.Equal(reverse, set.Reverse);
        Assert.Equal(reverse, multi.Reverse);
        Assert.Same(Comparer<int>.Default, set.Comparer);
        Assert.Same(Comparer<int>.Default, multi.Comparer);
        Assert.Equal(reference.Min, set.FirstNode!.Key);
        Assert.Equal(reference.Max, set.LastNode!.Key);
        var sorted = values.OrderBy(x => x, comparer).ToArray();
        Assert.Equal(sorted[0], multi.FirstNode!.Key);
        Assert.Equal(sorted[^1], multi.LastNode!.Key);
        for (var key = -1; key <= 30; key++)
        {
            Assert.Equal(reference.Contains(key), set.Contains(key));
            Assert.Equal(values.Contains(key), multi.Contains(key));
            Assert.Equal(values.Count(x => x == key), multi.GetCount(key));
            Assert.Equal(values.Count(x => x == key), multi.EnumerateNode(key).Count());
            Assert.Equal(reference.Where(x => comparer.Compare(x, key) >= 0).Select(x => (int?)x).FirstOrDefault(), set.GetLowerBound(key)?.Key);
            Assert.Equal(reference.Where(x => comparer.Compare(x, key) <= 0).Select(x => (int?)x).LastOrDefault(), set.GetUpperBound(key)?.Key);
            Assert.Equal(sorted.Where(x => comparer.Compare(x, key) >= 0).Select(x => (int?)x).FirstOrDefault(), multi.GetLowerBound(key)?.Key);
            Assert.Equal(sorted.Where(x => comparer.Compare(x, key) <= 0).Select(x => (int?)x).LastOrDefault(), multi.GetUpperBound(key)?.Key);
        }

        var copy = new int[set.Count + 1];
        set.CopyTo(copy, 1);
        Assert.Equal(reference, copy.Skip(1));
        copy = new int[multi.Count + 1];
        multi.CopyTo(copy, 1);
        Assert.Equal(sorted, copy.Skip(1));
        VerifyUntypedEnumeration(set, reference);
        VerifyUntypedEnumeration(multi, sorted);
        foreach (var key in values.Distinct())
        {
            Assert.Equal(values.Count(x => x == key), multi.RemoveAll(key));
            Assert.Equal(0, multi.RemoveAll(key));
            Assert.True(multi.Validate());
        }

        Assert.Empty(multi);
        set.Clear();
        Assert.Empty(set);
    }

    [Fact]
    public void NodesCanBeRenamedRemovedAndReused()
    {
        var set = new OrderedSet<string?>(new[] { "b", "a", "a", null }, StringComparer.Ordinal);
        var multi = new OrderedMultiSet<string?>(new[] { "b", "a", "a", null }, StringComparer.Ordinal);
        Assert.Equal(3, set.Count);
        Assert.Equal(4, multi.Count);
        Assert.True(set.Contains(null));
        Assert.True(multi.Contains(null));
        var node = set.FindNode("a")!;
        Assert.False(set.SetNodeValue(node, "b"));
        Assert.True(set.SetNodeValue(node, "c"));
        set.RemoveNode(node);
        Assert.Same(node, set.AddNode("d", node).Node);
        Assert.False(set.AddNode("d").NewlyAdded);
        var duplicate = multi.FindFirstNode("a")!;
        Assert.True(multi.SetNodeValue(duplicate, "b"));
        Assert.Equal(2, multi.GetCount("b"));
        multi.RemoveNode(duplicate);
        Assert.Same(duplicate, multi.Add("c", duplicate));
        Assert.Equal("b", set.GetRange("a", "c").Lower!.Key);
        Assert.Equal("b", set.GetRange("a", "c").Upper!.Key);
        Assert.Equal("a", multi.GetRange("a", "c").Lower!.Key);
        Assert.Equal("c", multi.GetRange("a", "c").Upper!.Key);
        Assert.True(set.Validate());
        Assert.True(multi.Validate());
        multi.Clear();
        Assert.Empty(multi);
    }

    [Fact]
    public void InvalidCopyArgumentsAreRejected()
    {
        var set = new OrderedSet<int>(new[] { 1, 2 });
        var multi = new OrderedMultiSet<int>(new[] { 1, 2 });
        Assert.Throws<ArgumentNullException>(() => set.CopyTo(null!, 0));
        Assert.Throws<ArgumentNullException>(() => multi.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new int[2], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => multi.CopyTo(new int[2], 3));
        Assert.Throws<ArgumentException>(() => set.CopyTo(new int[2], 1));
        Assert.Throws<ArgumentException>(() => multi.CopyTo(new int[2], 1));
        Assert.Throws<ArgumentNullException>(() => new OrderedSet<int>((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => new OrderedMultiSet<int>((IEnumerable<int>)null!));
    }

    private static void VerifyUntypedEnumeration(IEnumerable collection, IEnumerable<int> expected)
    {
        var enumerator = collection.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        var values = new List<int>();
        while (enumerator.MoveNext())
        {
            values.Add((int)enumerator.Current);
        }

        Assert.Equal(expected, values);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        ((IDisposable)enumerator).Dispose();
    }
}
