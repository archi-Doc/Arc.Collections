using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class CollectionReviewCoverageTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderedKeyValueListCopyPreservesDuplicateOrder(bool customComparer)
    {
        var comparer = customComparer ? Comparer<int>.Create((x, y) => y.CompareTo(x)) : Comparer<int>.Default;
        var source = new OrderedKeyValueList<int, int>(comparer);
        for (var i = 0; i < 96; i++)
        {
            source.Add(i % 3, i);
        }

        var copy = new OrderedKeyValueList<int, int>(source, comparer);

        Assert.Equal(source.ToArray(), copy.ToArray());
        Assert.Equal(source[1], copy[1]);
        copy.RemoveAll(1);
        Assert.True(source.ContainsKey(1));
    }

    [Fact]
    public void OrderedKeyValueListCopyResortsWhenComparerChanges()
    {
        var source = new OrderedKeyValueList<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" } };
        var copy = new OrderedKeyValueList<int, string>(source, Comparer<int>.Create((x, y) => y.CompareTo(x)));

        Assert.Equal(new[] { 3, 2, 1 }, copy.Keys);
        Assert.Equal(new[] { "three", "two", "one" }, copy.Values);
        Assert.Equal(new[] { 1, 2, 3 }, source.Keys);
    }

    [Fact]
    public void OrderedKeyValueListCopySupportsRuntimeComparableKeys()
    {
        var source = new OrderedKeyValueList<object, string>();
        source.Add(new ObjectKey(2), "two");
        source.Add(new ObjectKey(1), "one");

        var copy = new OrderedKeyValueList<object, string>(source);

        Assert.Equal("one", copy[new ObjectKey(1)]);
        Assert.Equal("two", copy[new ObjectKey(2)]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void SlidingListClearHandlesContiguousAndWrappedWindows(int head)
    {
        var list = new SlidingList<string>(8);
        for (var i = 0; i < head; i++)
        {
            Assert.True(list.TryRemoveAt(list.Add("advance")));
        }

        var start = list.StartPosition;
        Assert.True(list.TrySet(start, "first"));
        Assert.True(list.TrySet(start + 2, "middle"));
        Assert.True(list.TrySet(start + 5, "last"));
        Assert.True(list.TryRemoveAt(start + 2));
        var end = list.EndPosition;
        var enumerator = list.GetEnumerator();
        list.Clear();

        Assert.Equal(end, list.StartPosition);
        Assert.Equal(0, list.UsedSlotCount);
        Assert.Empty(list);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.True(list.TrySet(end + 7, "new"));
        Assert.Equal(new[] { "new" }, list.ToArray());
        var destination = new string[1];
        list.CopyTo(destination);
        Assert.Equal(new[] { "new" }, destination);
        list.Clear();
        list.Clear();
        Assert.Empty(list);
        Assert.Equal(end + 8, list.Add("reused"));
    }

    [Fact]
    public void ZeroCapacitySlidingListCanBeClearedAndResized()
    {
        var list = new SlidingList<string>(0);
        list.Clear();
        Assert.Empty(list.ToArray());
        Assert.True(list.Resize(2));
        Assert.Equal(0, list.Add("value"));
        list.Clear();
        Assert.True(list.Resize(0));
        list.Clear();
        Assert.Equal(1, list.StartPosition);
    }

    [Fact]
    public void EmptyUnorderedSetReusesEmptyArray()
    {
        var set = new UnorderedSet<string>();
        Assert.Same(Array.Empty<string>(), set.ToArray());
        set.Add("value");
        Assert.Equal(new[] { "value" }, set.ToArray());
        set.Clear();
        Assert.Same(Array.Empty<string>(), set.ToArray());
    }

    [Fact]
    public void OrderedKeyValueListNonGenericDictionaryChecksTypesAndSupportsNullValues()
    {
        var list = new OrderedKeyValueList<string, string?>();
        IDictionary dictionary = list;
        dictionary.Add("key", null);
        dictionary["key"] = "second";
        Assert.Null(dictionary["key"]);
        Assert.Null(dictionary[42]);
        Assert.False(dictionary.Contains(42));
        Assert.Throws<ArgumentNullException>(() => dictionary.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => dictionary.Add(null!, "value"));
        Assert.Throws<ArgumentException>(() => dictionary.Add(42, "value"));
        Assert.Throws<ArgumentException>(() => dictionary.Add("other", 42));
        Assert.Throws<ArgumentException>(() => dictionary[42] = "value");
        Assert.Throws<ArgumentException>(() => dictionary["other"] = 42);
        dictionary.Remove(42);
        dictionary.Remove("key");
        Assert.Equal("second", dictionary["key"]);
        Assert.False(dictionary.IsReadOnly);
        Assert.False(dictionary.IsFixedSize);
        Assert.False(dictionary.IsSynchronized);
        Assert.Same(list, dictionary.SyncRoot);
        Assert.Same(list.Keys, dictionary.Keys);
        Assert.Same(list.Values, dictionary.Values);

        IDictionary valueDictionary = new OrderedKeyValueList<int, int>();
        Assert.Throws<ArgumentException>(() => valueDictionary.Add(1, null));
        Assert.Throws<ArgumentException>(() => valueDictionary[1] = null);
    }

    [Fact]
    public void OrderedKeyValueListViewsAreReadOnlyLiveViews()
    {
        var list = new OrderedKeyValueList<int, string> { { 2, "two" } };
        var keys = list.Keys;
        var values = list.Values;
        list.Add(1, "one");

        Assert.True(keys.IsReadOnly);
        Assert.True(values.IsReadOnly);
        Assert.Equal(1, keys[0]);
        Assert.Equal("one", values[0]);
        Assert.True(keys.Contains(2));
        Assert.True(values.Contains("two"));
        Assert.Equal(1, keys.IndexOf(2));
        Assert.Equal(1, values.IndexOf("two"));
        Assert.Throws<ArgumentOutOfRangeException>(() => keys[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => values[2]);
        Assert.Throws<NotSupportedException>(() => keys[0] = 3);
        Assert.Throws<NotSupportedException>(() => values[0] = "three");
        Assert.Throws<NotSupportedException>(() => keys.Add(3));
        Assert.Throws<NotSupportedException>(() => values.Add("three"));
        Assert.Throws<NotSupportedException>(() => keys.Insert(0, 3));
        Assert.Throws<NotSupportedException>(() => values.Insert(0, "three"));
        Assert.Throws<NotSupportedException>(() => keys.Remove(1));
        Assert.Throws<NotSupportedException>(() => values.Remove("one"));
        Assert.Throws<NotSupportedException>(() => keys.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => values.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => keys.Clear());
        Assert.Throws<NotSupportedException>(() => values.Clear());
        list.Remove(1);
        Assert.Equal(new[] { 2 }, keys);
        Assert.Equal(new[] { "two" }, values);
    }

    [Fact]
    public void OrderedKeyValueListDictionaryEnumeratorValidatesStateAndVersion()
    {
        var list = new OrderedKeyValueList<int, string> { { 1, "one" } };
        var enumerator = ((IDictionary)list).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new DictionaryEntry(1, "one"), enumerator.Entry);
        Assert.Equal(enumerator.Entry, enumerator.Current);
        Assert.Equal(1, enumerator.Key);
        Assert.Equal("one", enumerator.Value);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        ((IDisposable)enumerator).Dispose();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        list.Add(2, "two");
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OrderedKeyValueListViewEnumeratorsValidateStateAndVersion(bool keys)
    {
        var list = new OrderedKeyValueList<int, string> { { 1, "one" } };
        var view = keys ? (IEnumerable)list.Keys : (IEnumerable)list.Values;
        var enumerator = view.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(keys ? (object)1 : "one", enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        ((IDisposable)enumerator).Dispose();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        list.Add(2, "two");
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
    }

    [Fact]
    public void OrderedKeyValueListCopyToValidatesDestinationAndPreservesPairs()
    {
        var list = new OrderedKeyValueList<int, string> { { 2, "two" }, { 1, "one" } };
        ICollection collection = list;
        var objects = new object[3];
        collection.CopyTo(objects, 1);
        Assert.Equal(new KeyValuePair<int, string>(1, "one"), objects[1]);
        Assert.Equal(new KeyValuePair<int, string>(2, "two"), objects[2]);
        var pairs = new KeyValuePair<int, string>[2];
        collection.CopyTo(pairs, 0);
        Assert.Equal(list.ToArray(), pairs);
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new object[2, 2], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(Array.CreateInstance(typeof(object), new[] { 3 }, new[] { 1 }), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(objects, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(objects, 4));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(objects, 2));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new int[2], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new string[2], 0));
    }

    [Fact]
    public void UnorderedMapSlimRefInsertionResetsReusedSlotsAndHandlesCollisions()
    {
        var map = new UnorderedMapSlim<CollisionKey, int>();
        for (var i = 0; i < 20; i++)
        {
            ref var value = ref map.GetValueRefOrAddDefault(new CollisionKey(i), out var exists);
            Assert.False(exists);
            Assert.Equal(0, value);
            value = i + 1;
        }

        Assert.True(map.Remove(new CollisionKey(10), out var removed));
        Assert.Equal(11, removed);
        ref var reused = ref map.GetValueRefOrAddDefault(new CollisionKey(50), out var found);
        Assert.False(found);
        Assert.Equal(0, reused);
        reused = 99;
        ref var existing = ref map.GetValueRefOrAddDefault(new CollisionKey(50), out found);
        Assert.True(found);
        Assert.Equal(99, existing);
        Assert.False(map.TryAdd(new CollisionKey(50), -1));
        Assert.Equal(99, map[new CollisionKey(50)]);
        map.Clear();
        Assert.Empty(map);
        ref var cleared = ref map.GetValueRefOrAddDefault(new CollisionKey(0), out found);
        Assert.False(found);
        Assert.Equal(0, cleared);
    }

    private sealed record ObjectKey(int Value) : IComparable<object>
    {
        public int CompareTo(object? other) => this.Value.CompareTo(((ObjectKey)other!).Value);
    }

    private readonly record struct CollisionKey(int Value)
    {
        public override int GetHashCode() => 0;
    }
}
