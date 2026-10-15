// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Behavior and coverage tests for <see cref="UnorderedList{T}"/>.
/// </summary>
public class ListQualityUnorderedListTest
{
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ConstructorRejectsInvalidCapacity(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new UnorderedList<int>(capacity));
    }

    [Fact]
    public void ConstructorFromCollections()
    {
        var empty = new UnorderedList<string>(new List<string>());
        empty.Count.Is(0);
        empty.Capacity.Is(0);

        var lazy = new UnorderedList<int>(Enumerable.Range(0, 10).Where(x => x % 2 == 0));
        lazy.ToArray().Is(0, 2, 4, 6, 8);

        var copied = new UnorderedList<int>(new[] { 3, 1, 2 });
        copied.ToArray().Is(3, 1, 2);
        copied.Capacity.Is(3);

        new UnorderedList<int>(0).Capacity.Is(0);
        Assert.Throws<ArgumentNullException>(() => new UnorderedList<int>(null!));
    }

    [Fact]
    public void AddRangeOverloads()
    {
        var list = new UnorderedList<int>();
        list.AddRange(new[] { 1, 2 });
        list.AddRange(new List<int> { 3 });
        list.AddRange(new ReadOnlySpan<int>(new[] { 4, 5 }));
        list.AddRange(Enumerable.Range(6, 2).Select(x => x));
        list.AddRange(list);

        list.ToArray().Is(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6, 7);
        Assert.Throws<ArgumentNullException>(() => list.AddRange((int[])null!));
        Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<int>)null!));
    }

    [Fact]
    public void AddRangeGrowsToTheRequiredCapacity()
    {
        var list = new UnorderedList<int>(new[] { 1, 2, 3, 4 });

        // Doubling (8) is not enough, so the exact required capacity is used.
        list.AddRange(Enumerable.Range(5, 10).ToArray());
        list.Capacity.Is(14);

        // Doubling is used when it is enough.
        list.Add(15);
        list.Capacity.Is(28);
        list.ToArray().Is(Enumerable.Range(1, 15));
    }

    [Fact]
    public void AddRangeInvalidatesEnumeratorsWhenElementsAreAdded()
    {
        var list = new UnorderedList<int>(new[] { 1 });
        var enumerator = list.GetEnumerator();
        list.AddRange(new List<int> { 2 });
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        enumerator = list.GetEnumerator();
        list.AddRange(new[] { 3 });
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void CopyToAndToArray()
    {
        var list = new UnorderedList<int>(new[] { 1, 2, 3 });
        var array = new int[3];
        list.CopyTo(array);
        array.Is(1, 2, 3);

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(new int[3], -1));
        Assert.Throws<ArgumentException>(() => list.CopyTo(new int[3], 1));

        new UnorderedList<int>(8).ToArray().IsSameReferenceAs(Array.Empty<int>());
        list.ToArray().IsNotSameReferenceAs(list.ToArray());
    }

    [Fact]
    public void InsertAndRemoveAtRejectInvalidIndexes()
    {
        var list = new UnorderedList<int>(new[] { 1, 2 });

        Assert.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list.Insert(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveAt(2));
        list.ToArray().Is(1, 2);

        // Inserting into a full list grows it.
        list.Capacity = 2;
        list.Insert(1, 9);
        list.ToArray().Is(1, 9, 2);
    }

    [Fact]
    public void RemovalReleasesReferences()
    {
        var list = new InspectableList<string>(new[] { "a", "b", "c", "d" });

        list.RemoveAt(1);
        list.Remove("d").IsTrue();
        list.ToArray().Is("a", "c");
        list.Items[2].IsNull();
        list.Items[3].IsNull();

        list.Clear();
        list.Items.All(x => x is null).IsTrue();
    }

    [Fact]
    public void CapacitySetter()
    {
        var list = new InspectableList<int>(new[] { 1, 2, 3 });
        var items = list.Items;

        list.Capacity = 3;
        list.Items.IsSameReferenceAs(items);

        Assert.Throws<ArgumentOutOfRangeException>("value", () => list.Capacity = 2);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => list.Capacity = -1);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => list.Capacity = Array.MaxLength + 1);

        list.Clear();
        list.Capacity = 0;
        list.Items.IsSameReferenceAs(Array.Empty<int>());
    }

    [Fact]
    public void EnsureCapacity()
    {
        var list = new UnorderedList<int>(new[] { 1, 2, 3, 4 });

        list.EnsureCapacity(0).Is(4);
        list.EnsureCapacity(4).Is(4);
        list.EnsureCapacity(5).Is(8);
        list.EnsureCapacity(100).Is(100);
        new UnorderedList<int>().EnsureCapacity(1).Is(4);

        Assert.Throws<ArgumentOutOfRangeException>("capacity", () => list.EnsureCapacity(-1));
        Assert.Throws<InvalidOperationException>(() => list.EnsureCapacity(Array.MaxLength + 1));
        list.Capacity.Is(100);
        list.ToArray().Is(1, 2, 3, 4);
    }

    [Fact]
    public void TrimExcess()
    {
        var list = new UnorderedList<int>(100);
        list.AddRange(Enumerable.Range(0, 95).ToArray());
        list.TrimExcess();
        list.Capacity.Is(100); // At least 90% used.

        list.AddRange(Enumerable.Range(0, 5).ToArray());
        list.Clear();
        list.AddRange(Enumerable.Range(0, 10).ToArray());
        list.TrimExcess();
        list.Capacity.Is(10);
        list.ToArray().Is(Enumerable.Range(0, 10));

        list.Clear();
        list.TrimExcess();
        list.Capacity.Is(0);
    }

    [Fact]
    public void IsReadOnlyIsFalse()
    {
        ICollection<int> collection = new UnorderedList<int>();
        collection.IsReadOnly.IsFalse();
    }

    [Fact]
    public void SpansAccessTheElementsInUse()
    {
        var list = new UnorderedList<int>(16);
        list.AddRange(new[] { 1, 2, 3 });

        list.AsSpan().Length.Is(3);
        list.AsSpan()[1] = 20;
        list.AsReadOnlySpan().ToArray().Is(1, 20, 3);
        new UnorderedList<int>().AsSpan().IsEmpty.IsTrue();
    }

    [Fact]
    public void NonGenericEnumeration()
    {
        IEnumerable enumerable = new UnorderedList<int>(new[] { 1, 2, 3 });
        var result = new List<object?>();
        foreach (var x in enumerable)
        {
            result.Add(x);
        }

        result.Is(1, 2, 3);
    }

    [Fact]
    public void EnumeratorReset()
    {
        var list = new UnorderedList<int>(new[] { 1, 2, 3 });
        IEnumerator<int> enumerator = list.GetEnumerator();

        enumerator.MoveNext().IsTrue();
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is(2);

        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is(1);

        // Reset after the end restarts the enumeration.
        while (enumerator.MoveNext())
        {
        }

        enumerator.MoveNext().IsFalse();
        enumerator.Reset();
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is(1);

        list.Add(4);
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        enumerator.Dispose();
    }

    [Fact]
    public void EnumeratorThrowsAfterModificationAtTheEnd()
    {
        var list = new UnorderedList<int>(new[] { 1 });
        var enumerator = list.GetEnumerator();
        enumerator.MoveNext().IsTrue();
        enumerator.MoveNext().IsFalse();

        list.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void StructEnumerationAndAddWithinCapacityDoNotAllocate()
    {
        var list = new UnorderedList<int>(64);
        var ordered = new OrderedList<int>(64);
        var sum = 0;
        Measure(); // Warm up.
        var allocated = AllocationHelper.Minimum(Measure);

        sum.Is(2 * (31 * 32 / 2));
        list.Count.Is(33);
        ordered.Count.Is(33);
        AllocationHelper.SkipUnlessOptimized();
        allocated.Is(0L);

        long Measure()
        {
            list.Clear();
            ordered.Clear();
            for (var i = 0; i < 32; i++)
            {
                list.Add(i);
                ordered.Add(31 - i);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            sum = SumAndAppend(list, ordered);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }

    private static int SumAndAppend(UnorderedList<int> list, OrderedList<int> ordered)
    {
        var sum = 0;
        foreach (var x in list)
        {
            sum += x;
        }

        foreach (var x in ordered)
        {
            sum += x;
        }

        list.Add(0);
        ordered.Add(0);
        ordered.Contains(5);
        return sum;
    }

    private sealed class InspectableList<T> : UnorderedList<T>
    {
        public InspectableList(IEnumerable<T> collection)
            : base(collection)
        {
        }

        public T[] Items => this.items;
    }
}
