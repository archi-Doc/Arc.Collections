// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Regression tests for defects found by the source audit.
/// </summary>
public class AuditRegressionTest
{
    // Orders null after every string, the opposite of Comparer<string>.Default.
    private static readonly IComparer<string?> NullLastComparer = Comparer<string?>.Create((x, y) => string.CompareOrdinal(y, x));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMap_NullKeyFollowsCustomComparer(bool reverse)
    {
        var map = new OrderedMap<string?, int>(NullLastComparer, reverse);
        map.Add(null, 0).NewlyAdded.IsTrue();
        foreach (var x in "abcdef")
        {
            map.Add(x.ToString(), x);
        }

        map.Add(null, 1).NewlyAdded.IsFalse();
        map.Count.Is(7);
        map.Validate().IsTrue();
        map.ContainsKey(null).IsTrue();
        map[null].Is(0);

        var expected = new string?[] { "f", "e", "d", "c", "b", "a", null };
        map.Keys.ToArray().Is(reverse ? Enumerable.Reverse(expected) : expected);

        map.Remove(null).IsTrue();
        map.Validate().IsTrue();
        map.ContainsKey(null).IsFalse();

        var node = map.FindNode("c")!;
        map.SetNodeKey(node, null).IsTrue();
        map.Validate().IsTrue();
        map.ContainsKey(null).IsTrue();
        map.ContainsKey("c").IsFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedSet_NullKeyFollowsCustomComparer(bool reverse)
    {
        var set = new OrderedSet<string?>(NullLastComparer, reverse);
        set.Add(null).IsTrue();
        set.Add("a").IsTrue();
        set.Add("b").IsTrue();
        set.Add(null).IsFalse();
        set.Count.Is(3);
        set.Contains(null).IsTrue();
        set.ToArray().Is(reverse ? new string?[] { null, "a", "b" } : new string?[] { "b", "a", null });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMultiMap_NullKeyFollowsCustomComparer(bool reverse)
    {
        var map = new OrderedMultiMap<string?, int>(NullLastComparer, reverse);
        map.Add(null, 1);
        foreach (var x in "abcdef")
        {
            map.Add(x.ToString(), x);
        }

        map.Add(null, 2);
        map.Count.Is(8);
        map.Validate().IsTrue();
        map.ContainsKey(null).IsTrue();
        map.EnumerateValues(null).ToArray().Is(1, 2);

        var expected = new string?[] { "f", "e", "d", "c", "b", "a", null, null };
        map.Keys.ToArray().Is(reverse ? new string?[] { null, null, "a", "b", "c", "d", "e", "f" } : expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMap_DefaultComparerDoesNotPassNullToCompareTo(bool reverse)
    {
        var map = new OrderedMap<NaiveComparable?, int>(reverse);
        map.Add(null, 0);
        map.Add(new(2), 2);
        map.Add(new(1), 1);
        map.Add(new(3), 3);

        map.Validate().IsTrue();
        map.ContainsKey(null).IsTrue();
        map.ContainsKey(new(2)).IsTrue();
        var keys = map.Keys.Select(x => x?.Value ?? -1).ToArray();
        keys.Is(reverse ? new[] { 3, 2, 1, -1 } : new[] { -1, 1, 2, 3 });

        var set = new OrderedSet<NaiveComparable?>(reverse);
        set.Add(null);
        set.Add(new(1));
        set.Contains(new(1)).IsTrue();
        set.Contains(null).IsTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedMultiMap_DefaultComparerDoesNotPassNullToCompareTo(bool reverse)
    {
        var map = new OrderedMultiMap<NaiveComparable?, int>(reverse);
        map.Add(null, 0);
        map.Add(new(2), 2);
        map.Add(new(1), 1);
        map.Add(null, 4);

        map.Validate().IsTrue();
        map.EnumerateValues(null).ToArray().Is(0, 4);
        map.ContainsKey(new(1)).IsTrue();

        var set = new OrderedMultiSet<NaiveComparable?>(reverse);
        set.Add(null);
        set.Add(new(1));
        set.Contains(new(1)).IsTrue();
    }

    [Fact]
    public void OrderedList_DefaultComparerDoesNotPassNullToCompareTo()
    {
        var list = new OrderedList<NaiveComparable?>();
        list.Add(null);
        list.Add(new(2));
        list.Add(new(1));
        list.Add(null);

        list.Select(x => x?.Value ?? -1).ToArray().Is(-1, -1, 1, 2);
        list.Contains(null).IsTrue();
        list.IndexOf(new(1)).Is(2);
        list.BinarySearch(new(3)).Is(~4);
        list.GetLowerBound(new(0)).Is(2);
        list.GetUpperBound(new(1)).Is(2);
        list.RangeOf(null).Is((0, 2));
        list.Remove(new(2)).IsTrue();
    }

    [Fact]
    public void OrderedMultiMap_MatchedValueEnumeratorReset()
    {
        var map = new OrderedMultiMap<int, string>();
        map.Add(1, "A");
        map.Add(1, "B");
        map.Add(2, "C");

        // Boxed once, as a caller of the interface sees it.
        IEnumerator<string> enumerator = map.EnumerateValues(1).GetEnumerator();
        enumerator.MoveNext().IsTrue();
        enumerator.MoveNext().IsTrue();
        enumerator.MoveNext().IsFalse();

        enumerator.Reset();
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is("A");
        ((IEnumerator)enumerator).Current.Is("A");
        enumerator.MoveNext().IsTrue();
        enumerator.Current.Is("B");
        enumerator.MoveNext().IsFalse();

        map.Add(1, "D");
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
    }

    [Fact]
    public void OrderedMap_SetNodeKeyKeepsEntryWhenComparerThrows()
    {
        var comparer = new ThrowingComparer();
        var threwAfterRemoval = false;
        for (var failAt = 0; ; failAt++)
        {
            var map = new OrderedMap<int, int>(comparer);
            for (var i = 1; i <= 15; i++)
            {
                map.Add(i, i * 10);
            }

            var node = map.FindNode(3)!;
            comparer.FailAt(failAt);
            var threw = false;
            try
            {
                map.SetNodeKey(node, 100);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            finally
            {
                comparer.Disarm();
            }

            map.Validate().IsTrue();
            map.Count.Is(15);
            if (!threw)
            {
                map.ContainsKey(100).IsTrue();
                map.ContainsKey(3).IsFalse();
                break;
            }

            map.ContainsKey(3).IsTrue();
            map.ContainsKey(100).IsFalse();
            node.IsUnused.IsFalse();
            node.Key.Is(3);
            node.Value.Is(30);
            threwAfterRemoval |= failAt > 0;
        }

        threwAfterRemoval.IsTrue();
    }

    [Theory]
    [InlineData(3, "3")] // The tree node of a duplicate group.
    [InlineData(3, "3c")] // A linked duplicate.
    [InlineData(7, "7")] // A single node.
    public void OrderedMultiMap_SetNodeKeyKeepsEntryWhenComparerThrows(int key, string value)
    {
        var comparer = new ThrowingComparer();
        for (var failAt = 0; ; failAt++)
        {
            var map = new OrderedMultiMap<int, string>(comparer);
            for (var i = 1; i <= 15; i++)
            {
                map.Add(i, i.ToString());
            }

            map.Add(3, "3b");
            map.Add(3, "3c");
            var node = map.FindNode(key, value)!;
            var original = map.Select(x => (x.Key, x.Value)).ToArray();

            comparer.FailAt(failAt);
            var threw = false;
            try
            {
                map.SetNodeKey(node, 100);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            finally
            {
                comparer.Disarm();
            }

            map.Validate().IsTrue();
            map.Count.Is(17);
            if (!threw)
            {
                map.EnumerateValues(100).ToArray().Is(value);
                map.Select(x => (x.Key, x.Value)).ToArray().Is(original.Where(x => x.Value != value).Append((100, value)));
                break;
            }

            // Including the order within the duplicate group.
            map.Select(x => (x.Key, x.Value)).ToArray().Is(original);
            node.Key.Is(key);
            node.Value.Is(value);
        }
    }

    [Fact]
    public void UnorderedMap_SetNodeKeyKeepsEntryWhenHashThrows()
    {
        var map = new UnorderedMap<HashKey, int>(allowDuplicates: true);
        var index = map.Add(new(1), 10).NodeIndex;

        Assert.Throws<InvalidOperationException>(() => map.SetNodeKey(index, new HashKey(2, throwOnHash: true)));
        map.Count.Is(1);
        map.ContainsKey(new(1)).IsTrue();
        map.TryGetValue(new(1), out var value).IsTrue();
        value.Is(10);

        map.Add(new(1), 20);
        map.RemoveNode(index);
        map.Count.Is(1);
        map.TryGetValue(new(1), out value).IsTrue();
        value.Is(20);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData((1 << 29) + 1)]
    [InlineData(1 << 30)]
    [InlineData(int.MaxValue)]
    public void Hashtable_RejectsCapacityOutOfRange(int capacity)
    {
        // (1 << 29) + 1 used to spin forever in the capacity calculation; larger values silently created 8 buckets.
        Assert.Throws<ArgumentOutOfRangeException>(() => new Int32Hashtable<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UInt32Hashtable<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Int64Hashtable<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UInt64Hashtable<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Utf16Hashtable<int>(capacity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Utf8Hashtable<int>(capacity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Hashtable_AcceptsCapacity(int capacity)
    {
        var table = new UInt64Hashtable<int>(capacity);
        for (var i = 0ul; i < 100; i++)
        {
            table.TryAdd(i, (int)i).IsTrue();
        }

        table.Count.Is(100);
    }

    [Fact]
    public void Hashtable_GetOrAddRejectsFactoryThatModifiesTable()
    {
        var int32 = new Int32Hashtable<int>();
        Assert.Throws<InvalidOperationException>(() => int32.GetOrAdd(7, k => { int32.TryAdd(k, 1); return 2; }));
        int32.Count.Is(1);
        int32.ToArray().Is(1);

        var uint32 = new UInt32Hashtable<int>();
        Assert.Throws<InvalidOperationException>(() => uint32.GetOrAdd(7, k => { uint32.TryAdd(k, 1); return 2; }));
        uint32.ToArray().Is(1);

        var int64 = new Int64Hashtable<int>();
        Assert.Throws<InvalidOperationException>(() => int64.GetOrAdd(7, k => { int64.TryAdd(k, 1); return 2; }));
        int64.ToArray().Is(1);

        var uint64 = new UInt64Hashtable<int>();
        uint64.TryAdd(1, 1);
        Assert.Throws<InvalidOperationException>(() => uint64.GetOrAdd(7, k => { uint64.Clear(); return 2; }));
        uint64.Count.Is(0);
        uint64.ContainsKey(7).IsFalse();

        var utf16 = new Utf16Hashtable<int>();
        Assert.Throws<InvalidOperationException>(() => utf16.GetOrAdd("a", k => { utf16.TryAdd(k, 1); return 2; }));
        Assert.Throws<InvalidOperationException>(() => utf16.GetOrAdd("b".AsSpan(), k => { utf16.TryAdd(k, 1); return 2; }));
        utf16.Count.Is(2);
        utf16.ToArray().Is(1, 1);

        var utf8 = new Utf8Hashtable<int>();
        Assert.Throws<InvalidOperationException>(() => utf8.GetOrAdd("a"u8.ToArray(), k => { utf8.TryAdd(k, 1); return 2; }));
        Assert.Throws<InvalidOperationException>(() => utf8.GetOrAdd("b"u8, k => { utf8.TryAdd(k, 1); return 2; }));
        utf8.Count.Is(2);
        utf8.ToArray().Is(1, 1);

        // Reading the table from the factory is allowed.
        uint64.GetOrAdd(8, k => uint64.ContainsKey(k) ? -1 : 3).Is(3);
        uint64.GetOrAdd(8, k => 4).Is(3);
        utf16.GetOrAdd("c", k => utf16.TryGetValue(k, out _) ? -1 : 3).Is(3);
    }

    [Fact]
    public void BytePool_ClampsArrayLengthsAbove2Pow30()
    {
        // Buckets of 2^30, 2^29, ..., 1 bytes: bucket 2^31 (int.MinValue) must not exist.
        BytePool.CreateFlat(int.MaxValue).CalculateMaxMemoryUsage().Is(256L * ((1L << 31) - 1));
        BytePool.CreateExponential((1 << 30) + 1, 1).CalculateMaxMemoryUsage().Is(2L * ((1L << 31) - 1));

        var pool = BytePool.CreateFlat(1024, 2);
        var usage = pool.CalculateMaxMemoryUsage();
        pool.SetPoolLimit(int.MaxValue, 2);
        pool.SetPoolLimit((1 << 30) + 1, 2);
        pool.CalculateMaxMemoryUsage().Is(usage);
    }

    [Fact]
    public void TemporaryList_ToArrayOfCopyMatchesEnumeration()
    {
        var list = default(TemporaryList<int>);
        for (var i = 0; i < 5; i++)
        {
            list.Add(i);
        }

        // Copies share the overflow list; the copied list must not overrun its own count.
        var copy = list;
        copy.Add(100);
        list.Add(5);

        var array = list.ToArray();
        array.Length.Is(list.Count);
        var enumerated = new List<int>();
        foreach (var x in list)
        {
            enumerated.Add(x);
        }

        array.Is(enumerated);
    }

    [Fact]
    public void OrderedMultiSet_RemoveAllRemovesEveryDuplicate()
    {
        var set = new OrderedMultiSet<int>();
        for (var i = 0; i < 1000; i++)
        {
            set.Add(i % 10);
        }

        set.RemoveAll(3).Is(100);
        set.RemoveAll(3).Is(0);
        set.Count.Is(900);
        set.Contains(3).IsFalse();
        set.Validate().IsTrue();

        set.RemoveAll(0).Is(100);
        set.RemoveAll(9).Is(100);
        set.Validate().IsTrue();
        set.ToArray().Is(Enumerable.Range(0, 1000).Select(x => x % 10).Where(x => x is not 0 and not 3 and not 9).Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(100)]
    public void OrderedList_AddRangeMatchesStableSort(int initialCount)
    {
        var random = new Random(initialCount);
        var comparer = Comparer<Entry>.Create((x, y) => x.Key.CompareTo(y.Key));
        var sequence = 0;
        Entry[] Batch(int count) => Enumerable.Range(0, count).Select(_ => new Entry(random.Next(20), sequence++)).ToArray();

        var list = new OrderedList<Entry>(comparer);
        var reference = new List<Entry>();
        foreach (var x in Batch(initialCount))
        {
            list.Add(x);
            reference.Add(x);
        }

        foreach (var count in new[] { 0, 1, 5, 31, 32, 33, 100, 1000 })
        {
            for (var kind = 0; kind < 4; kind++)
            {
                var batch = Batch(count);
                switch (kind)
                {
                    case 0:
                        list.AddRange(batch);
                        break;
                    case 1:
                        list.AddRange(new ReadOnlySpan<Entry>(batch));
                        break;
                    case 2:
                        list.AddRange(new List<Entry>(batch));
                        break;
                    default:
                        list.AddRange(Yield(batch));
                        break;
                }

                reference = reference.Concat(batch).OrderBy(x => x.Key).ToList();
                list.ToArray().Is(reference);
            }
        }

        list.AddRange(list);
        reference = reference.Concat(reference).OrderBy(x => x.Key).ToList();
        list.ToArray().Is(reference);

        list.AddRange(list.AsReadOnlySpan().Slice(list.Count / 3, 50));
        list.Count.Is(reference.Count + 50);
        IsSorted(list, comparer).IsTrue();

        var ordered = new OrderedList<Entry>(reference.AsEnumerable().Reverse(), comparer);
        ordered.ToArray().Is(reference.AsEnumerable().Reverse().OrderBy(x => x.Key));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(200)]
    public void OrderedList_AddRangeKeepsOrderWhenComparerThrows(int batchSize)
    {
        var comparer = new ThrowingComparer();
        for (var failAt = 0; ; failAt += 7)
        {
            var list = new OrderedList<int>(comparer);
            for (var i = 0; i < 100; i++)
            {
                list.Add((i * 37) % 101);
            }

            var batch = Enumerable.Range(0, batchSize).Select(i => (i * 53) % 97).ToArray();
            comparer.FailAt(failAt);
            var threw = false;
            try
            {
                list.AddRange(batch);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            finally
            {
                comparer.Disarm();
            }

            IsSorted(list, Comparer<int>.Default).IsTrue();
            list.Count.Is(x => x >= 100 && x <= 100 + batchSize);

            // Every existing element is kept; only a part of the batch may be dropped.
            var remaining = list.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            foreach (var x in Enumerable.Range(0, 100).Select(i => (i * 37) % 101))
            {
                remaining[x]--;
            }

            foreach (var x in batch)
            {
                if (remaining.TryGetValue(x, out var n) && n > 0)
                {
                    remaining[x] = n - 1;
                }
            }

            remaining.Values.All(n => n == 0).IsTrue();
            if (!threw)
            {
                list.Count.Is(100 + batchSize);
                break;
            }
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void OrderedList_AddRangeSortsElementsAddedBeforeEnumerationFailure(int count)
    {
        var list = new OrderedList<int>(new[] { 50, 10, 30 });
        var added = Enumerable.Range(0, count).Select(i => (i * 17) % 41).ToArray();
        IEnumerable<int> Failing()
        {
            foreach (var x in added)
            {
                yield return x;
            }

            throw new InvalidOperationException();
        }

        Assert.Throws<InvalidOperationException>(() => list.AddRange(Failing()));
        list.ToArray().Is(added.Concat(new[] { 50, 10, 30 }).Order());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void OrderedList_AddRangeLazySourceSeesSortedList(int count)
    {
        // A lazy source that reads the list must see every element added before it.
        var values = Enumerable.Range(0, count).Select(i => (i * 7) % 5).ToArray();
        var list = new OrderedList<int>(new[] { 4 });
        list.AddRange(values.Where(x => !list.Contains(x)));
        list.ToArray().Is(values.Append(4).Distinct().Order());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void OrderedList_AddRangeRethrowsComparerException(int count)
    {
        var comparer = Comparer<int>.Create((x, y) => x == 13 || y == 13 ? throw new ArgumentException() : x.CompareTo(y));
        var list = new OrderedList<int>(new[] { 1, 20 }, comparer);
        var batch = Enumerable.Range(0, count).Select(i => i == count / 2 ? 13 : i).ToArray();
        Assert.Throws<ArgumentException>(() => list.AddRange(batch));
        IsSorted(list, comparer).IsTrue();
    }

    [Fact]
    public void OrderedList_AddRangeComparesLikeSearch()
    {
        // Comparer<IShape>.Default cannot compare shapes; searches use the runtime IComparable<IShape>.
        var shapes = Enumerable.Range(0, 40).Select(i => (IShape)new Shape((i * 7) % 11)).ToArray();
        var list = new OrderedList<IShape>();
        list.Add(new Shape(5));
        list.AddRange(shapes);
        list.Select(x => x.Size).ToArray().Is(shapes.Select(x => x.Size).Append(5).Order());
        new OrderedList<IShape>(shapes).Select(x => x.Size).ToArray().Is(shapes.Select(x => x.Size).Order());
    }

    [Fact]
    public void OrderedList_EmptyAddRangeDoesNotInvalidateEnumerators()
    {
        var list = new OrderedList<int>(new[] { 1, 2 });
        foreach (var x in list)
        {
            list.AddRange(Array.Empty<int>());
            list.AddRange(ReadOnlySpan<int>.Empty);
            list.AddRange(new List<int>());
            list.AddRange(Enumerable.Empty<int>());
        }
    }

    private static IEnumerable<T> Yield<T>(IEnumerable<T> source)
    {
        foreach (var x in source)
        {
            yield return x;
        }
    }

    private static bool IsSorted<T>(OrderedList<T> list, IComparer<T> comparer)
    {
        for (var i = 1; i < list.Count; i++)
        {
            if (comparer.Compare(list[i - 1], list[i]) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private interface IShape
    {
        int Size { get; }
    }

    private sealed record Shape(int Size) : IShape, IComparable<IShape>
    {
        public int CompareTo(IShape? other) => other is null ? 1 : this.Size.CompareTo(other.Size);
    }

    private sealed record Entry(int Key, int Sequence);

    // CompareTo does not accept null, which is valid because Comparer<T>.Default never passes null to it.
    private sealed class NaiveComparable : IComparable<NaiveComparable>, IEquatable<NaiveComparable>
    {
        public NaiveComparable(int value) => this.Value = value;

        public int Value { get; }

        public int CompareTo(NaiveComparable? other) => this.Value.CompareTo(other!.Value);

        public bool Equals(NaiveComparable? other) => other is not null && other.Value == this.Value;

        public override bool Equals(object? obj) => this.Equals(obj as NaiveComparable);

        public override int GetHashCode() => this.Value;
    }

    private sealed class HashKey : IEquatable<HashKey>
    {
        private readonly bool throwOnHash;

        public HashKey(int id, bool throwOnHash = false)
        {
            this.Id = id;
            this.throwOnHash = throwOnHash;
        }

        public int Id { get; }

        public bool Equals(HashKey? other) => other is not null && other.Id == this.Id;

        public override bool Equals(object? obj) => this.Equals(obj as HashKey);

        public override int GetHashCode() => this.throwOnHash ? throw new InvalidOperationException() : this.Id;
    }

    // Throws once, on the n-th comparison after being armed.
    private sealed class ThrowingComparer : IComparer<int>
    {
        private int remaining = -1;

        public void FailAt(int n) => this.remaining = n;

        public void Disarm() => this.remaining = -1;

        public int Compare(int x, int y)
        {
            if (this.remaining >= 0 && this.remaining-- == 0)
            {
                throw new InvalidOperationException();
            }

            return x.CompareTo(y);
        }
    }
}
