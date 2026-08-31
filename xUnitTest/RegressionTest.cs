using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Regression tests for previously reported defects.
/// </summary>
public class RegressionTest
{
    [Fact]
    public void OrderedList_InterfaceCallsKeepSortOrder()
    {
        var list = new OrderedList<int>();
        ICollection<int> collection = list;

        collection.Add(3);
        collection.Add(1);
        collection.Add(2);

        list.AsSpan().SequenceEqual([1, 2, 3]).IsTrue();
        collection.Contains(2).IsTrue();
        ((IList<int>)list).IndexOf(2).Is(1);

        collection.Remove(1).IsTrue();
        list.AsSpan().SequenceEqual([2, 3]).IsTrue();
    }

    [Fact]
    public void OrderedList_OrderBreakingMembersThrow()
    {
        var list = new OrderedList<int> { };
        list.Add(1);
        list.Add(2);

        Assert.Throws<InvalidOperationException>(() => list.Insert(0, 99));
        Assert.Throws<InvalidOperationException>(() => ((IList<int>)list).Insert(0, 99));
        Assert.Throws<InvalidOperationException>(() => list[0] = 99);
        Assert.Throws<InvalidOperationException>(() => ((IList<int>)list)[0] = 99);
    }

    [Fact]
    public void OrderedList_AddRangeKeepsSortOrder()
    {
        var list = new OrderedList<int>();
        list.AddRange([5, 1, 4]);
        list.AddRange(new List<int> { 3, 2, });

        list.AsSpan().SequenceEqual([1, 2, 3, 4, 5]).IsTrue();
    }

    [Fact]
    public void OrderedMap_DoubleKeysMatchDefaultComparer()
    {
        // The specialized double implementation must order NaN the same way
        // Comparer<double>.Default does: NaN sorts before every other value and
        // is a key of its own. (Comparer<double>.Default treats -0.0 and 0.0 as equal.)
        var map = new OrderedMap<double, int>();
        map.Add(1d, 1);
        map.Add(double.NaN, 2);
        map.Add(-1d, 3);
        map.Add(double.NaN, 4); // Duplicate key.

        map.Count.Is(3);
        map.Validate().IsTrue();
        map.ContainsKey(double.NaN).IsTrue();

        var keys = new List<double>(map.Keys);
        keys.Count.Is(3);
        double.IsNaN(keys[0]).IsTrue();
        keys[1].Is(-1d);
        keys[2].Is(1d);
    }

    [Fact]
    public void OrderedList_DoubleMatchesDefaultComparer()
    {
        var list = new OrderedList<double>();
        list.Add(1d);
        list.Add(double.NaN);
        list.Add(-1d);

        double.IsNaN(list[0]).IsTrue();
        list[1].Is(-1d);
        list[2].Is(1d);
        list.Contains(double.NaN).IsTrue();
    }

    [Fact]
    public void KeyedObjectCache_RejectsNonPositiveSize()
    {
        // A non-positive cache size used to spin forever inside Cache().
        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyedObjectCache<int, object>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyedObjectCache<int, object>(-1));
    }

    [Fact]
    public void KeyedObjectCache_DisposesObjectThatCannotBeCached()
    {
        using var cache = new KeyedObjectCache<int, DisposableObject>(4);
        var first = new DisposableObject();
        var second = new DisposableObject();

        cache.Cache(1, first).IsTrue();

        // The key is already present, so 'second' is not cached and must be disposed.
        cache.CreateInterface(1, second).Return();

        first.IsDisposed.IsFalse();
        second.IsDisposed.IsTrue();
    }

    [Fact]
    public void OrderedMap_NullKeyIsSupportedForValueTypeValues()
    {
        var map = new OrderedMap<string?, int>();
        map.Add(null, 1);
        map.Add("a", 2);

        map.Count.Is(2);
        map.ContainsKey(null).IsTrue();
        map.First!.Key.IsNull();
        map.Validate().IsTrue();
    }

    private sealed class DisposableObject : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => this.IsDisposed = true;
    }
}
