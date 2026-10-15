// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Contract and coverage tests for <see cref="UnorderedSet{T}"/>.
/// </summary>
public class UnorderedMapQualitySetTest
{
    [Fact]
    public void ConstructorsExposeOptions()
    {
        var set = new UnorderedSet<string>();
        set.AllowDuplicates.IsFalse();
        set.Capacity.Is(4);
        set.Comparer.IsSameReferenceAs(EqualityComparer<string>.Default);

        set = new UnorderedSet<string>(StringComparer.OrdinalIgnoreCase, true);
        set.AllowDuplicates.IsTrue();
        set.Comparer.IsSameReferenceAs(StringComparer.OrdinalIgnoreCase);
        set.Add("a").IsTrue();
        set.Add("A").IsTrue();
        set.Count.Is(2);

        set = new UnorderedSet<string>(20, true);
        set.AllowDuplicates.IsTrue();
        set.Capacity.Is(32);
    }

    [Fact]
    public void CollectionConstructorsApplyComparerAndDuplicateOption()
    {
        var set = new UnorderedSet<string>(new[] { "a", "A", "b" }, StringComparer.OrdinalIgnoreCase);
        set.Count.Is(2);
        set.AllowDuplicates.IsFalse();
        set.Comparer.IsSameReferenceAs(StringComparer.OrdinalIgnoreCase);
        set.Contains("B").IsTrue();

        var duplicates = new UnorderedSet<string?>(new string?[] { null, "x", null }, null, true);
        duplicates.Count.Is(3);
        duplicates.ToArray().Count(x => x is null).Is(2);
        duplicates.Remove(null).IsTrue();
        duplicates.Contains(null).IsTrue();
        duplicates.Remove(null).IsTrue();
        duplicates.Contains(null).IsFalse();
        duplicates.Remove(null).IsFalse();

        Assert.Throws<ArgumentNullException>(() => new UnorderedSet<int>((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => new UnorderedSet<int>((IEnumerable<int>)null!, (IEqualityComparer<int>?)null));
        Assert.Throws<ArgumentNullException>(() => new UnorderedSet<int>((IEnumerable<int>)null!, true));
        Assert.Throws<ArgumentNullException>(() => new UnorderedSet<int>((IEnumerable<int>)null!, null, true));
    }

    [Fact]
    public void CollectionConstructorPresizesFromKnownCounts()
    {
        // Nine equal items collapse to one element, so the capacity reveals whether the source count was used.
        var items = Enumerable.Repeat(7, 9).ToArray();

        new UnorderedSet<int>(new List<int>(items)).Capacity.Is(16);
        new UnorderedSet<int>(new ReadOnlyOnlyCollection<int>(items)).Capacity.Is(16);
        new UnorderedSet<int>(Lazy(items)).Capacity.Is(4);

        // A set source presizes unless its duplicates are merged, so merging never over-allocates.
        var duplicates = new UnorderedSet<int>(items, true);
        duplicates.Count.Is(9);
        var merged = new UnorderedSet<int>(duplicates);
        merged.Count.Is(1);
        merged.Capacity.Is(4);
        var kept = new UnorderedSet<int>(duplicates, true);
        kept.Count.Is(9);
        kept.Capacity.Is(16);

        var distinct = new UnorderedSet<int>(Enumerable.Range(0, 9));
        new UnorderedSet<int>(distinct).Capacity.Is(16);
        new UnorderedSet<int>(distinct, true).Capacity.Is(16);

        // With a different comparer the source count is only an upper bound, as for ICollection<T> sources.
        var variants = new[] { "ab", "aB", "Ab", "AB", "cd" };
        var ordinal = new UnorderedSet<string>(variants, StringComparer.Ordinal);
        var ignoreCase = new UnorderedSet<string>(ordinal, StringComparer.OrdinalIgnoreCase);
        ignoreCase.Count.Is(2);
        ignoreCase.Capacity.Is(8);
        new UnorderedSet<string>(variants, StringComparer.OrdinalIgnoreCase).Capacity.Is(8);

        static IEnumerable<int> Lazy(int[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    [Fact]
    public void CopyingAnotherSetDoesNotGrowRepeatedly()
    {
        var source = new UnorderedSet<int>();
        for (var i = 0; i < 1000; i++)
        {
            source.Add(i);
        }

        var (copy, _) = Copy(source); // Warms up the code paths.
        var (baseline, _) = Fill(source);
        var copyBytes = AllocationHelper.Minimum(() => Copy(source).Bytes);
        var baselineBytes = AllocationHelper.Minimum(() => Fill(source).Bytes);

        copy.Count.Is(1000);
        copy.Capacity.Is(baseline.Capacity);
        copy.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 1000)).IsTrue();

        // The baseline fills a presized set element by element, so per-element costs (such as key boxing in Debug builds)
        // appear on both sides. Growing from the minimum capacity would roughly double the storage.
        Assert.True(copyBytes < baselineBytes + 1024, $"copy={copyBytes} baseline={baselineBytes}");

        static (UnorderedSet<int> Set, long Bytes) Copy(UnorderedSet<int> source)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var set = new UnorderedSet<int>(source);
            return (set, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        static (UnorderedSet<int> Set, long Bytes) Fill(UnorderedSet<int> source)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var set = new UnorderedSet<int>(source.Count);
            foreach (var item in source)
            {
                set.Add(item);
            }

            return (set, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }

    [Fact]
    public void CopyingSetWithSameComparerReusesHashCodes()
    {
        var comparer = new CountingComparer();
        var source = new UnorderedSet<string?>(comparer, true);
        for (var i = 0; i < 40; i++)
        {
            source.Add(i.ToString());
        }

        source.Add(null);
        source.Add(null);
        for (var i = 0; i < 40; i += 3)
        {
            source.Remove(i.ToString()).IsTrue();
        }

        source.Add("x");
        source.Add("x");
        var expected = Sorted(source);
        expected.Length.Is(30);

        // Same comparer and duplicates kept: no hashing or probing.
        comparer.Reset();
        var copy = new UnorderedSet<string?>(source, comparer, true);
        comparer.HashCalls.Is(0);
        comparer.EqualsCalls.Is(0);
        copy.Capacity.Is(32);
        Sorted(copy).Is(expected);

        // Same comparer but duplicates merged: every non-null element is hashed and probed.
        comparer.Reset();
        var merged = new UnorderedSet<string?>(source, comparer, false);
        comparer.HashCalls.Is(28);
        Sorted(merged).Is(expected.Distinct().ToArray());

        // A duplicate-free source copied with its comparer skips hashing even when the copy allows duplicates.
        comparer.Reset();
        var again = new UnorderedSet<string?>(merged, comparer, true);
        comparer.HashCalls.Is(0);
        Sorted(again).Is(Sorted(merged));
        again.Add("x").IsTrue();

        // A different comparer rehashes every non-null element.
        var other = new CountingComparer();
        var rehashed = new UnorderedSet<string?>(merged, other, false);
        other.HashCalls.Is(27);
        Sorted(rehashed).Is(Sorted(merged));

        // Every copy has intact chains: each element is found and removable exactly as often as it occurs.
        CheckAndDrain(copy);
        CheckAndDrain(merged);
        CheckAndDrain(again);
        CheckAndDrain(rehashed);
        source.Count.Is(30);

        static void CheckAndDrain(UnorderedSet<string?> set)
        {
            set.Contains("missing").IsFalse();
            var items = set.ToArray();
            foreach (var item in items)
            {
                set.Contains(item).IsTrue();
            }

            foreach (var item in items)
            {
                set.Remove(item).IsTrue();
            }

            set.Count.Is(0);
            items.Any(set.Contains).IsFalse();
        }
    }

    [Fact]
    public void ClearRemovesElementsAndKeepsCapacity()
    {
        var set = new UnorderedSet<string?>(Enumerable.Range(0, 20).Select(x => x.ToString()));
        set.Add(null);
        var capacity = set.Capacity;
        capacity.Is(32);

        set.Clear();
        set.Count.Is(0);
        set.Capacity.Is(capacity);
        set.Contains("1").IsFalse();
        set.Contains(null).IsFalse();
        set.ToArray().Length.Is(0);

        set.Add("1").IsTrue();
        set.Add(null).IsTrue();
        set.ToArray().OrderBy(x => x, StringComparer.Ordinal).ToArray().Is(new string?[] { null, "1" });
    }

    [Fact]
    public void CopyToValidatesArguments()
    {
        var set = new UnorderedSet<string?>(allowDuplicates: true);
        set.Add("a");
        set.Add(null);
        set.Add("a");

        Assert.Throws<ArgumentNullException>(() => set.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new string?[3], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new string?[3], 4));
        Assert.Throws<ArgumentException>(() => set.CopyTo(new string?[3], 1));

        var array = new string?[5];
        array[0] = "x";
        array[4] = "y";
        set.CopyTo(array, 1);
        array[0].Is("x");
        array[4].Is("y");
        array.Skip(1).Take(3).OrderBy(x => x, StringComparer.Ordinal).ToArray().Is(null, "a", "a");

        // An empty set may copy to the end of the array.
        new UnorderedSet<string>().CopyTo(Array.Empty<string>(), 0);
        new UnorderedSet<string>().CopyTo(new string[2], 2);
    }

    [Fact]
    public void NonGenericEnumerationFollowsEnumeratorContract()
    {
        var set = new UnorderedSet<int>();
        set.Add(1);
        set.Add(2);

        var enumerator = ((IEnumerable)set).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        var items = new List<int>();
        while (enumerator.MoveNext())
        {
            items.Add((int)enumerator.Current!);
        }

        items.OrderBy(x => x).ToArray().Is(1, 2);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        enumerator.MoveNext().IsTrue();
        set.Remove(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        using var generic = ((IEnumerable<int>)set).GetEnumerator();
        generic.MoveNext().IsTrue();
        generic.Current.Is(2);
        generic.MoveNext().IsFalse();
    }

    private static string?[] Sorted(IEnumerable<string?> source)
        => source.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    private sealed class CountingComparer : IEqualityComparer<string?>
    {
        public int HashCalls { get; private set; }

        public int EqualsCalls { get; private set; }

        public void Reset()
        {
            this.HashCalls = 0;
            this.EqualsCalls = 0;
        }

        public bool Equals(string? x, string? y)
        {
            this.EqualsCalls++;
            return string.Equals(x, y, StringComparison.Ordinal);
        }

        public int GetHashCode(string obj)
        {
            this.HashCalls++;
            return StringComparer.Ordinal.GetHashCode(obj);
        }
    }

    private sealed class ReadOnlyOnlyCollection<T> : IReadOnlyCollection<T>
    {
        private readonly T[] items;

        public ReadOnlyOnlyCollection(T[] items)
        {
            this.items = items;
        }

        public int Count => this.items.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)this.items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();
    }
}
