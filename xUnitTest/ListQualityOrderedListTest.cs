// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Behavior and coverage tests for <see cref="OrderedList{T}"/>.
/// </summary>
public class ListQualityOrderedListTest
{
    private static readonly IComparer<Entry> EntryComparer = Comparer<Entry>.Create((x, y) => x.Key.CompareTo(y.Key));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    public void AddRangeOfInOrderBatchUsesLinearComparisons(int initialCount)
    {
        // A batch that is sorted and does not precede the last element is already in place.
        var comparer = new CountingComparer();
        var list = new OrderedList<int>(Enumerable.Range(0, initialCount), comparer);
        var batch = Enumerable.Range(Math.Max(initialCount - 1, 0) * 3, 1000).Select(x => x / 3).ToArray();

        comparer.Count = 0;
        list.AddRange(batch);

        comparer.Count.Is(x => x <= batch.Length);
        list.ToArray().Is(Enumerable.Range(0, initialCount).Concat(batch));
    }

    [Fact]
    public void ConstructorFromSortedSourceUsesLinearComparisons()
    {
        var comparer = new CountingComparer();
        var source = Enumerable.Range(0, 1000).Select(x => x / 4).ToArray();

        var list = new OrderedList<int>(source, comparer);

        comparer.Count.Is(source.Length - 1);
        list.ToArray().Is(source);
    }

    [Fact]
    public void AddRangeOfSortedInterleavingBatchSkipsTheSort()
    {
        // A sorted batch only needs the merge: m comparisons to check the order,
        // then one binary search over at most n elements (at most 10 comparisons for n = 1000) per element.
        var comparer = new CountingComparer();
        var list = new OrderedList<int>(Enumerable.Range(0, 1000).Select(x => 2 * x), comparer);
        var batch = Enumerable.Range(0, 1000).Select(x => (2 * x) + 1).ToArray();

        comparer.Count = 0;
        list.AddRange(batch);

        comparer.Count.Is(x => x <= batch.Length * 11);
        list.ToArray().Is(Enumerable.Range(0, 2000));
    }

    [Fact]
    public void SortedBatchThatPrecedesLastElementIsMergedAfterEqualElements()
    {
        // KeyedValue uses the default comparer without a HotMethod; Entry uses a custom comparer.
        var existing = Enumerable.Range(0, 20).Select(i => new KeyedValue(i / 2, i)).ToArray();
        var list = new OrderedList<KeyedValue>(existing);
        var batch = Enumerable.Range(0, 30).Select(i => new KeyedValue(i / 3, 100 + i)).ToArray();
        list.AddRange(batch);
        list.ToArray().Is(existing.Concat(batch).OrderBy(x => x.Key));

        var entries = Enumerable.Range(0, 20).Select(i => new Entry(i / 2, i)).ToArray();
        var entryList = new OrderedList<Entry>(entries, EntryComparer);
        var entryBatch = Enumerable.Range(0, 30).Select(i => new Entry(i / 3, 100 + i)).ToArray();
        entryList.AddRange(entryBatch);
        entryList.ToArray().Is(entries.Concat(entryBatch).OrderBy(x => x.Key));
    }

    [Fact]
    public void InOrderBatchIsPlacedAfterEqualExistingElements()
    {
        var list = new OrderedList<Entry>(new[] { new Entry(1, 0), new Entry(2, 1), new Entry(2, 2) }, EntryComparer);
        var batch = Enumerable.Range(0, 20).Select(i => new Entry(2 + (i / 5), 10 + i)).ToArray();

        list.AddRange(batch);

        list.ToArray().Is(new[] { new Entry(1, 0), new Entry(2, 1), new Entry(2, 2) }.Concat(batch));
        list.RangeOf(new Entry(2, -1)).Is((1, 8));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BatchThatIsNotInOrderIsMergedStably(bool precedesLastElement)
    {
        var existing = Enumerable.Range(0, 30).Select(i => new Entry(i % 10, i)).ToArray();
        var list = new OrderedList<Entry>(existing, EntryComparer);

        // Either a sorted batch that starts below the largest element, or one whose last element is out of order.
        var batch = Enumerable.Range(0, 40).Select(i => new Entry(precedesLastElement ? i / 4 : 9 + (i / 4), 100 + i)).ToList();
        if (!precedesLastElement)
        {
            batch.Add(new Entry(0, 200));
        }

        list.AddRange(batch);

        list.ToArray().Is(existing.Concat(batch).OrderBy(x => x.Key));
    }

    [Fact]
    public void ValueTypeWithDefaultComparerAndNoHotMethod()
    {
        // KeyedValue compares only Key, so Sequence shows where equal elements were placed.
        var list = new OrderedList<KeyedValue>();
        list.HotMethod.IsNull();

        var added = new[] { new KeyedValue(5, 0), new KeyedValue(1, 1), new KeyedValue(5, 2), new KeyedValue(3, 3) };
        foreach (var x in added)
        {
            list.Add(x);
        }

        list.Select(x => x.Sequence).ToArray().Is(1, 3, 0, 2);
        list.Contains(new KeyedValue(3, -1)).IsTrue();
        list.Contains(new KeyedValue(4, -1)).IsFalse();
        list.IndexOf(new KeyedValue(5, -1)).Is(2);
        list.BinarySearch(new KeyedValue(4, -1)).Is(~2);
        list.GetLowerBound(new KeyedValue(2, -1)).Is(1);
        list.GetLowerBound(new KeyedValue(6, -1)).Is(-1);
        list.GetUpperBound(new KeyedValue(5, -1)).Is(3);
        list.GetUpperBound(new KeyedValue(0, -1)).Is(-1);
        list.RangeOf(new KeyedValue(5, -1)).Is((2, 4));

        // Batches use the sort path (unordered) and the in-order path (sorted).
        var random = new Random(3);
        var unordered = Enumerable.Range(0, 40).Select(i => new KeyedValue(random.Next(8), 10 + i)).ToArray();
        list.AddRange(unordered);
        var ordered = Enumerable.Range(0, 20).Select(i => new KeyedValue(7 + (i / 5), 100 + i)).ToArray();
        list.AddRange(ordered);

        list.ToArray().Is(added.OrderBy(x => x.Key).Concat(unordered).OrderBy(x => x.Key).Concat(ordered).OrderBy(x => x.Key));
        list.Remove(new KeyedValue(5, -1)).IsTrue();
        list.Contains(new KeyedValue(5, 0)).IsTrue();
        list.Count.Is(added.Length + unordered.Length + ordered.Length - 1);
    }

    [Fact]
    public void InterfaceElementsWithNullsUseRuntimeComparable()
    {
        // Comparer<IRank>.Default cannot compare ranks; elements are compared through IComparable<IRank>.
        var random = new Random(5);
        var batch = Enumerable.Range(0, 40).Select(i => i % 7 == 0 ? null : (IRank)new Rank(random.Next(10), i)).ToArray();
        var list = new OrderedList<IRank?>();
        list.Add(new Rank(4, -1));
        list.AddRange(batch);

        var nullCount = batch.Count(x => x is null);
        var expected = batch.Prepend(new Rank(4, -1)).OrderBy(x => x is null ? -1 : x.Value).ToArray();
        list.ToArray().Is(expected);
        list.RangeOf(null).Is((0, nullCount));
        list.GetLowerBound(null).Is(0);
        list.GetUpperBound(null).Is(nullCount - 1);
        list.IndexOf(new Rank(4, -2)).Is(Array.FindIndex(expected, x => x is not null && x.Value == 4));

        // The list does not compare like its Comparer, which ignores IComparable<IRank>.
        list.Comparer.IsSameReferenceAs(Comparer<IRank?>.Default);
        Assert.Throws<ArgumentException>(() => list.Comparer.Compare(new Rank(1, 0), new Rank(2, 0)));

        // An in-order batch that starts with null elements.
        var ordered = new OrderedList<IRank?>(new IRank?[] { null, null }.Concat(Enumerable.Range(0, 20).Select(i => new Rank(i / 2, i))));
        ordered.Count.Is(22);
        ordered[0].IsNull();
        ordered[21]!.Value.Is(9);
    }

    [Fact]
    public void ComparerExceptionDuringOrderCheckDropsBatchAndReleasesReferences()
    {
        var comparer = new ThrowingComparer();
        var list = new InspectableOrderedList<string>(comparer);
        list.Add("b");
        list.Add("a");
        var batch = Enumerable.Range(0, 20).Select(i => $"c{i:D2}").ToArray();

        var enumerator = list.GetEnumerator();
        comparer.FailAt(0);
        Assert.Throws<InvalidOperationException>(() => list.AddRange(batch));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.ToArray().Is("a", "b");
        list.Items.Skip(list.Count).All(x => x is null).IsTrue();
    }

    [Fact]
    public void ComparerExceptionWhileInsertingFewElementsKeepsMergedElements()
    {
        var comparer = new ThrowingComparer();
        var list = new InspectableOrderedList<string>(comparer);
        list.AddRange(new[] { "d", "b" });

        // "c" is inserted with two comparisons; inserting "a" throws on its second comparison.
        comparer.FailAt(3);
        Assert.Throws<InvalidOperationException>(() => list.AddRange(new[] { "c", "a", "e", "f" }));

        list.ToArray().Is("b", "c", "d");
        list.Items.Skip(list.Count).All(x => x is null).IsTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RangeOfReturnsHalfOpenRange(bool customComparer)
    {
        var comparer = customComparer ? Comparer<int>.Create((x, y) => x.CompareTo(y)) : null;
        var list = new OrderedList<int>(new[] { 1, 3, 3, 3, 5 }, comparer);

        list.RangeOf(3).Is((1, 4));
        list.RangeOf(1).Is((0, 1));
        list.RangeOf(5).Is((4, 5));
        list.RangeOf(0).Is((-1, -1));
        list.RangeOf(4).Is((-1, -1));
        list.RangeOf(6).Is((-1, -1));
        new OrderedList<int>(comparer).RangeOf(1).Is((-1, -1));
    }

    [Fact]
    public void CustomComparerBounds()
    {
        var list = new OrderedList<int>(new[] { 1, 5, 3, 3 }, Comparer<int>.Create((x, y) => y.CompareTo(x)));
        list.HotMethod.IsNull();

        list.ToArray().Is(5, 3, 3, 1);
        list.GetLowerBound(3).Is(1);
        list.GetLowerBound(4).Is(1);
        list.GetLowerBound(0).Is(-1);
        list.GetUpperBound(3).Is(2);
        list.GetUpperBound(6).Is(-1);
        list.BinarySearch(2).Is(~3);
    }

    [Fact]
    public void ReferenceTypeBoundsWithNullValue()
    {
        var list = new OrderedList<string?>(new[] { "b", null, "a", null });

        list.ToArray().Is(null, null, "a", "b");
        list.GetLowerBound(null).Is(0);
        list.GetUpperBound(null).Is(1);
        list.GetLowerBound("c").Is(-1);
        list.GetUpperBound("a").Is(2);
        list.BinarySearch("ab").Is(~3);
    }

    [Fact]
    public void SearchesUseTheComparerForEquality()
    {
        var list = new OrderedList<string>(new[] { "b", "a" }, StringComparer.OrdinalIgnoreCase);

        list.Contains("A").IsTrue();
        list.IndexOf("B").Is(1);
        ((ICollection<string>)list).Contains("A").IsTrue();

        // The hidden base members use EqualityComparer<T>.Default.
        UnorderedList<string> unordered = list;
        unordered.Contains("A").IsFalse();
        unordered.IndexOf("a").Is(0);

        list.Remove("A").IsTrue();
        list.ToArray().Is("b");
    }

    [Fact]
    public void IndexerAndInterfaceIndexers()
    {
        var list = new OrderedList<int>(new[] { 3, 1, 2 });
        IReadOnlyList<int> readOnly = list;
        IList<int> indexed = list;

        list[0].Is(1);
        readOnly[1].Is(2);
        indexed[2].Is(3);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list[3]);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => readOnly[3]);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => indexed[-1]);
    }

    [Fact]
    public void ComparerAndHotMethodResolution()
    {
        var list = new OrderedList<int>();
        list.Comparer.IsSameReferenceAs(Comparer<int>.Default);
        list.HotMethod.IsNotNull();

        var custom = Comparer<int>.Create((x, y) => x.CompareTo(y));
        var customList = new OrderedList<int>(16, custom);
        customList.Comparer.IsSameReferenceAs(custom);
        customList.HotMethod.IsNull();
        customList.Capacity.Is(16);

        new OrderedList<string>().HotMethod.IsNull();
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        var list = new OrderedList<int>();

        Assert.Throws<ArgumentNullException>(() => new OrderedList<int>((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => new OrderedList<int>(null!, Comparer<int>.Default));
        Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => list.AddRange((int[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderedList<int>(-1));
    }

    [Fact]
    public void CorruptedSizeThrowsInsteadOfReadingOutOfBounds()
    {
        // A derived class can corrupt the protected state; searches must not read past the array.
        var list = new InspectableOrderedList<string>(null);
        list.Add("a");
        list.CorruptSize(list.Items.Length + 1);

        Assert.Throws<InvalidOperationException>(() => list.Contains("a"));
        Assert.Throws<InvalidOperationException>(() => list.GetLowerBound("a"));
        Assert.Throws<InvalidOperationException>(() => list.GetUpperBound("a"));
    }

    private interface IRank
    {
        int Value { get; }
    }

    private readonly record struct KeyedValue(int Key, int Sequence) : IComparable<KeyedValue>
    {
        public int CompareTo(KeyedValue other) => this.Key.CompareTo(other.Key);
    }

    private sealed record Entry(int Key, int Sequence);

    private sealed record Rank(int Value, int Sequence) : IRank, IComparable<IRank>
    {
        public int CompareTo(IRank? other) => other is null ? 1 : this.Value.CompareTo(other.Value);
    }

    private sealed class InspectableOrderedList<T> : OrderedList<T>
    {
        public InspectableOrderedList(IComparer<T>? comparer)
            : base(comparer)
        {
        }

        public T[] Items => this.items;

        public void CorruptSize(int size) => this.size = size;
    }

    private sealed class CountingComparer : IComparer<int>
    {
        public int Count { get; set; }

        public int Compare(int x, int y)
        {
            this.Count++;
            return x.CompareTo(y);
        }
    }

    private sealed class ThrowingComparer : IComparer<string>
    {
        private int remaining = -1;

        public void FailAt(int n) => this.remaining = n;

        public int Compare(string? x, string? y)
        {
            if (this.remaining >= 0 && this.remaining-- == 0)
            {
                throw new InvalidOperationException();
            }

            return string.CompareOrdinal(x, y);
        }
    }
}
