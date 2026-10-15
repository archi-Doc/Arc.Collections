using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class CollectionFinalAuditTest
{
    [Fact]
    public void OrderedListReusesItsStableSortComparerAcrossBatches()
    {
        var comparer = Comparer<BatchEntry>.Create((x, y) => x.Key.CompareTo(y.Key));
        var list = new OrderedList<BatchEntry>(64, comparer);
        var batch = Enumerable.Range(0, 32).Select(i => new BatchEntry((31 - i) % 5, i)).ToArray();
        var expected = batch.OrderBy(x => x.Key).ToArray();

        var allocated = AllocationHelper.Measure(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                list.Clear();
                list.AddRange(batch);
            }
        });

        Assert.Equal(expected, list.ToArray());
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0L, allocated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMapKeyChangesFinishComparisonsBeforeRemovingTheEntry(bool reverse)
    {
        OrderedMap<int, int>? map = null;
        var comparer = Comparer<int>.Create((x, y) =>
        {
            if (map is not null && map.Count != 31)
            {
                throw new InvalidOperationException("The map changed before comparisons finished.");
            }

            return x.CompareTo(y);
        });
        var target = new OrderedMap<int, int>(comparer, reverse);
        var nodes = Enumerable.Range(0, 31).Select(i => target.Add(i * 4, i).Node).ToArray();
        map = target;
        var random = new Random(1729);
        for (var i = 0; i < 500; i++)
        {
            var node = nodes[random.Next(nodes.Length)];
            // Alternating extremes force removal, rotations, and relinking in both directions.
            var key = (i % 2 == 0 ? -1 : 1) * (1000 + i);
            var value = node.Value;
            Assert.True(map.SetNodeKey(node, key));
            Assert.Same(node, map.FindNode(key));
            Assert.Equal(value, node.Value);
            Assert.True(map.Validate());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMultiMapKeyChangesFinishComparisonsBeforeRemovingTheEntry(bool reverse)
    {
        OrderedMultiMap<int, int>? map = null;
        var comparer = Comparer<int>.Create((x, y) =>
        {
            if (map is not null && map.Count != 40)
            {
                throw new InvalidOperationException("The map changed before comparisons finished.");
            }

            return x.CompareTo(y);
        });
        var target = new OrderedMultiMap<int, int>(comparer, reverse);
        var nodes = Enumerable.Range(0, 40).Select(i => target.Add(i * 4, i).Node).ToArray();
        map = target;
        var random = new Random(1729);
        for (var i = 0; i < 1000; i++)
        {
            var node = nodes[random.Next(nodes.Length)];
            var key = i % 3 == 0 ? (i % 13) * 4 : (i % 2 == 0 ? -1 : 1) * (1000 + i);
            var changed = node.Key != key;
            var value = node.Value;
            Assert.Equal(changed, map.SetNodeKey(node, key));
            Assert.Equal(value, node.Value);
            Assert.Equal(key, node.Key);
            Assert.Contains(node, map.EnumerateNodes(key));
            Assert.True(map.Validate());
            Assert.Equal(40, map.Count);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMapPreservesEntryWhenEverySubsequentComparisonThrows(bool reverse)
    {
        var threw = false;
        var succeeded = false;
        for (var limit = 0; limit < 20; limit++)
        {
            var comparer = new FailingComparer();
            var map = new OrderedMap<int, int>(comparer, reverse);
            for (var i = 0; i < 31; i++)
            {
                map.Add(i * 4, i);
            }

            var node = map.FindNode(60)!;
            var before = map.ToArray();
            var enumerator = map.GetEnumerator();
            comparer.Remaining = limit;
            var error = Record.Exception(() => map.SetNodeKey(node, 1000));
            comparer.Remaining = int.MaxValue;
            if (error is not null)
            {
                threw = true;
                Assert.IsType<InvalidOperationException>(error);
                Assert.Equal(before, map.ToArray());
                Assert.Equal(60, node.Key);
                Assert.True(enumerator.MoveNext());
            }
            else
            {
                succeeded = true;
                Assert.Same(node, map.FindNode(1000));
            }

            Assert.Equal(31, map.Count);
            Assert.True(map.Validate());
        }

        Assert.True(threw);
        Assert.True(succeeded);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void OrderedMultiMapPreservesEntryWhenEverySubsequentComparisonThrows(bool reverse, bool duplicates)
    {
        var threw = false;
        var succeeded = false;
        for (var limit = 0; limit < 20; limit++)
        {
            var comparer = new FailingComparer();
            var map = new OrderedMultiMap<int, int>(comparer, reverse);
            for (var i = 0; i < 31; i++)
            {
                map.Add(i * 4, i);
            }

            var node = map.FindFirstNode(60)!;
            if (duplicates)
            {
                map.Add(60, 100);
                map.Add(60, 101);
            }

            var before = map.ToArray();
            var enumerator = map.GetEnumerator();
            comparer.Remaining = limit;
            var error = Record.Exception(() => map.SetNodeKey(node, 1000));
            comparer.Remaining = int.MaxValue;
            if (error is not null)
            {
                threw = true;
                Assert.IsType<InvalidOperationException>(error);
                Assert.Equal(before, map.ToArray());
                Assert.Equal(60, node.Key);
                Assert.True(enumerator.MoveNext());
            }
            else
            {
                succeeded = true;
                Assert.Same(node, map.FindFirstNode(1000));
            }

            Assert.Equal(before.Length, map.Count);
            Assert.True(map.Validate());
        }

        Assert.True(threw);
        Assert.True(succeeded);
    }

    private sealed record BatchEntry(int Key, int Sequence);

    private sealed class FailingComparer : IComparer<int>
    {
        public int Remaining { get; set; } = int.MaxValue;

        public int Compare(int x, int y)
        {
            if (this.Remaining-- <= 0)
            {
                throw new InvalidOperationException("Comparison failed.");
            }

            return x.CompareTo(y);
        }
    }
}
