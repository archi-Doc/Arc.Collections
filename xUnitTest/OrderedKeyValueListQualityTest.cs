using System;
using System.Collections;
using System.Collections.Generic;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class OrderedKeyValueListQualityTest
{
    [Fact]
    public void CopyFromDictionarySortsGenericOnlyComparableKeysLikeAdd()
    {
        // Add orders object keys by their IComparable<object>; copying must sort them the same way.
        var dictionary = new Dictionary<object, string>
        {
            [new GenericOnlyKey(3)] = "three",
            [new GenericOnlyKey(1)] = "one",
            [new GenericOnlyKey(2)] = "two",
        };

        var list = new OrderedKeyValueList<object, string>(dictionary);

        Assert.Equal(new[] { "one", "two", "three" }, list.Values);
        Assert.Equal("two", list[new GenericOnlyKey(2)]);
        Assert.Equal(1, list.IndexOfKey(new GenericOnlyKey(2)));
    }

    [Fact]
    public void CopySortsKeysLikeSearchWhenNonGenericComparableDisagrees()
    {
        // IComparable<object> (used by the search) orders ascending; IComparable orders descending.
        var source = new OrderedKeyValueList<object, int>(Comparer<object>.Create((x, y) => ((TwoWayKey)y!).Value.CompareTo(((TwoWayKey)x!).Value)));
        for (var i = 0; i < 20; i++)
        {
            source.Add(new TwoWayKey(i), i);
        }

        var copy = new OrderedKeyValueList<object, int>(source);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(i, copy.Values[i]);
            Assert.True(copy.TryGetValue(new TwoWayKey(i), out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void CopyFromDictionaryWithNonGenericComparableObjectKeysUsesDefaultComparer()
    {
        // Boxed integers implement IComparable but not IComparable<object>: both sorting and search use Comparer<object>.Default.
        var dictionary = new Dictionary<object, string> { [3] = "three", [1] = "one", [2] = "two" };

        var list = new OrderedKeyValueList<object, string>(dictionary);

        Assert.Equal(new object[] { 1, 2, 3 }, list.Keys);
        Assert.Equal("two", list[2]);
        Assert.Equal(~3, list.BinarySearch(4));
        Assert.Equal(1, list.GetLowerBound(2));
        Assert.Equal(-1, list.GetLowerBound(4));
        Assert.Equal(1, list.GetUpperBound(2));
    }

    [Fact]
    public void CopyConstructorReportsSortFailuresAsInvalidOperationException()
    {
        // Array.Sort wraps comparison failures, as documented; Add reports the original exception directly.
        var incomparable = new Dictionary<object, int> { [new object()] = 1, [new object()] = 2 };
        var exception = Assert.Throws<InvalidOperationException>(() => new OrderedKeyValueList<object, int>(incomparable));
        Assert.IsType<ArgumentException>(exception.InnerException);

        var throwing = Comparer<int>.Create((x, y) => throw new NotSupportedException());
        exception = Assert.Throws<InvalidOperationException>(() => new OrderedKeyValueList<int, int>(new Dictionary<int, int> { [1] = 1, [2] = 2 }, throwing));
        Assert.IsType<NotSupportedException>(exception.InnerException);

        // A single entry is not sorted, so nothing is compared.
        var single = new OrderedKeyValueList<object, int>(new Dictionary<object, int> { [new object()] = 1 });
        Assert.Single(single);
        Assert.Throws<ArgumentException>(() => single.Add(new object(), 2));
    }

    [Fact]
    public void ViewNonGenericCopyToReportsUncastableElementsAsArgumentException()
    {
        var list = new OrderedKeyValueList<object, object> { { 1, "one" } };
        var keys = ((IDictionary)list).Keys;
        var values = ((IDictionary)list).Values;

        // Array.Copy throws InvalidCastException for these downcasts; ICollection.CopyTo specifies ArgumentException.
        Assert.Throws<ArgumentException>(() => keys.CopyTo(new string[1], 0));
        Assert.Throws<ArgumentException>(() => values.CopyTo(new int[1], 0));
        Assert.Throws<ArgumentException>(() => keys.CopyTo(new DateTime[1], 0));

        var destination = new object[2];
        keys.CopyTo(destination, 1);
        values.CopyTo(destination, 0);
        Assert.Equal(new object[] { "one", 1 }, destination);
    }

    [Fact]
    public void ViewCopyToCopiesInOrderAndValidatesArguments()
    {
        var list = new OrderedKeyValueList<int, string> { { 2, "two" }, { 1, "one" }, { 1, "uno" } };
        ICollection keys = (ICollection)list.Keys;
        ICollection values = (ICollection)list.Values;

        var keyArray = new int[4];
        list.Keys.CopyTo(keyArray, 1);
        Assert.Equal(new[] { 0, 1, 1, 2 }, keyArray);
        var valueArray = new string[3];
        list.Values.CopyTo(valueArray, 0);
        Assert.Equal(new[] { "one", "uno", "two" }, valueArray);

        var objects = new object[3];
        keys.CopyTo(objects, 0);
        Assert.Equal(new object[] { 1, 1, 2 }, objects);
        values.CopyTo(objects, 0);
        Assert.Equal(new object[] { "one", "uno", "two" }, objects);
        var longs = new long[3];
        keys.CopyTo(longs, 0); // Array.Copy widens primitive values.
        Assert.Equal(new long[] { 1, 1, 2 }, longs);

        Assert.Throws<ArgumentNullException>(() => list.Keys.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Values.CopyTo(valueArray, -1));
        Assert.Throws<ArgumentException>(() => list.Keys.CopyTo(keyArray, 2));
        Assert.Throws<ArgumentNullException>(() => keys.CopyTo(null!, 0));
        Assert.Throws<ArgumentNullException>(() => values.CopyTo(null!, 0));
        Assert.Throws<ArgumentException>(() => keys.CopyTo(new int[2, 2], 0));
        Assert.Throws<ArgumentException>(() => values.CopyTo(new string[2, 2], 0));
        Assert.Throws<ArgumentException>(() => keys.CopyTo(new string[3], 0)); // ArrayTypeMismatchException
        Assert.Throws<ArgumentException>(() => values.CopyTo(new int[3], 0));
        Assert.Throws<ArgumentException>(() => keys.CopyTo(new object[2], 0));

        Assert.False(keys.IsSynchronized);
        Assert.False(values.IsSynchronized);
        Assert.Same(list, keys.SyncRoot);
        Assert.Same(list, values.SyncRoot);
    }

    [Fact]
    public void InterfaceViewsShareTheCachedInstances()
    {
        var list = new OrderedKeyValueList<int, string> { { 1, "one" } };
        IDictionary<int, string> dictionary = list;
        IReadOnlyDictionary<int, string> readOnly = list;

        Assert.Same(list.Keys, dictionary.Keys);
        Assert.Same(list.Keys, readOnly.Keys);
        Assert.Same(list.Values, dictionary.Values);
        Assert.Same(list.Values, readOnly.Values);
        Assert.Equal(new[] { 1 }, readOnly.Keys);
        Assert.Equal(new[] { "one" }, readOnly.Values);
    }

    [Fact]
    public void GenericCollectionInterfaceMatchesKeyAndValue()
    {
        var list = new OrderedKeyValueList<string, int>();
        ICollection<KeyValuePair<string, int>> collection = list;

        Assert.False(collection.IsReadOnly);
        collection.Add(new("b", 2));
        collection.Add(new("a", 1));
        collection.Add(new("b", 3));

        Assert.True(collection.Contains(new("b", 3)));
        Assert.True(collection.Contains(new("a", 1)));
        Assert.False(collection.Contains(new("b", 1)));
        Assert.False(collection.Contains(new("c", 2)));
        Assert.Throws<ArgumentNullException>(() => collection.Contains(new(null!, 0)));

        Assert.False(collection.Remove(new("b", 4)));
        Assert.False(collection.Remove(new("c", 2)));
        Assert.True(collection.Remove(new("b", 3)));
        Assert.Equal(new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) }, list);
        Assert.Throws<ArgumentNullException>(() => collection.Remove(new(null!, 0)));
    }

    [Fact]
    public void NonGenericDictionaryFindsCompatibleKeys()
    {
        var list = new OrderedKeyValueList<int, string> { { 1, "one" }, { 1, "uno" } };
        IDictionary dictionary = list;

        Assert.True(dictionary.Contains(1));
        Assert.False(dictionary.Contains(2));
        Assert.Equal("one", dictionary[1]);
        Assert.Null(dictionary[2]);
        dictionary.Remove(2);
        dictionary.Remove(1);
        Assert.Equal("uno", dictionary[1]);
        Assert.Single(list);
    }

    [Fact]
    public void NullKeysAreRejected()
    {
        var list = new OrderedKeyValueList<string, string> { { "a", "1" } };

        Assert.Throws<ArgumentNullException>("key", () => list.Add(null!, "x"));
        Assert.Throws<ArgumentNullException>("key", () => list[null!] = "x");
        Assert.Throws<ArgumentNullException>("key", () => list[null!]);
        Assert.Throws<ArgumentNullException>("key", () => list.BinarySearch(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.GetLowerBound(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.GetUpperBound(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.ContainsKey(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.IndexOfKey(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.RangeOfKey(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.TryGetValue(null!, out _));
        Assert.Throws<ArgumentNullException>("key", () => list.Remove(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.Remove(null!, "1"));
        Assert.Throws<ArgumentNullException>("key", () => list.RemoveAll(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.Keys.Contains(null!));
        Assert.Throws<ArgumentNullException>("key", () => list.Keys.IndexOf(null!));
        Assert.Single(list);
    }

    [Fact]
    public void ConstructorsValidateArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderedKeyValueList<int, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderedKeyValueList<int, int>(-1, null));
        Assert.Throws<ArgumentNullException>(() => new OrderedKeyValueList<int, int>((IDictionary<int, int>)null!));
        Assert.Throws<ArgumentNullException>(() => new OrderedKeyValueList<int, int>((IDictionary<int, int>)null!, null));

        var empty = new OrderedKeyValueList<int, int>(new Dictionary<int, int>());
        Assert.Empty(empty);
        Assert.Equal(0, empty.Capacity);

        var single = new OrderedKeyValueList<int, string>(new Dictionary<int, string> { [5] = "five" });
        Assert.Equal(1, single.Capacity);
        Assert.Equal("five", single[5]);
        single.Add(3, "three");
        Assert.Equal(new[] { 3, 5 }, single.Keys);

        var comparer = Comparer<int>.Create((x, y) => y.CompareTo(x));
        var fromDictionary = new OrderedKeyValueList<int, string>(new SortedDictionary<int, string> { [1] = "one", [2] = "two" }, comparer);
        Assert.Same(comparer, fromDictionary.Comparer);
        Assert.Null(fromDictionary.HotMethod);
        Assert.Equal(new[] { 2, 1 }, fromDictionary.Keys);
        Assert.NotNull(new OrderedKeyValueList<int, string>().HotMethod);
    }

    [Fact]
    public void CapacityCanBeKeptReleasedAndRegrown()
    {
        var list = new OrderedKeyValueList<string, string>(3);
        list.Capacity = 3;
        Assert.Equal(3, list.Capacity);

        list.Capacity = 0;
        Assert.Equal(0, list.Capacity);
        list.TrimExcess();
        Assert.Equal(0, list.Capacity);

        list.Add("b", "2");
        Assert.Equal(4, list.Capacity);
        list.Add("a", "1");
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 1);
        list.Capacity = 2;
        Assert.Equal(new[] { "a", "b" }, list.Keys);
        list.Add("c", "3");
        Assert.Equal(4, list.Capacity);
        Assert.Equal(new[] { "1", "2", "3" }, list.Values);
    }

    [Fact]
    public void EnsureCapacityGrowsGeometricallyOrToTheRequest()
    {
        var list = new OrderedKeyValueList<int, int>();
        Assert.Throws<ArgumentOutOfRangeException>(() => list.EnsureCapacity(-1));
        Assert.Equal(0, list.EnsureCapacity(0));
        Assert.Equal(4, list.EnsureCapacity(1));
        Assert.Equal(4, list.EnsureCapacity(3));
        Assert.Equal(8, list.EnsureCapacity(5));
        Assert.Equal(100, list.EnsureCapacity(100));

        list.Add(2, 20);
        list.Add(1, 10);
        Assert.Equal(100, list.EnsureCapacity(50));
        list.TrimExcess();
        Assert.Equal(2, list.Capacity);
        Assert.Equal(new[] { 10, 20 }, list.Values);
    }

    [Fact]
    public void ClearAndRemovalsReleaseReferences()
    {
        var list = new InspectableList();
        for (var i = 0; i < 6; i++)
        {
            list.Add(i.ToString(), "v" + i);
        }

        list.RemoveAt(5);
        Assert.Null(list.KeyArray[5]);
        Assert.Null(list.ValueArray[5]);

        list.RemoveRange(1, 2);
        Assert.Equal(new[] { "0", "3", "4" }, list.Keys);
        Assert.Equal(new[] { "v0", "v3", "v4" }, list.Values);
        Assert.Null(list.KeyArray[3]);
        Assert.Null(list.ValueArray[4]);

        list.Clear();
        Assert.Empty(list);
        Assert.All(list.KeyArray, x => Assert.Null(x));
        Assert.All(list.ValueArray, x => Assert.Null(x));
        Assert.Equal(8, list.Capacity);
        list.Add("x", "y");
        Assert.Equal("y", list["x"]);
    }

    [Fact]
    public void RemoveRangeValidatesArgumentsAndIgnoresEmptyRanges()
    {
        var list = new OrderedKeyValueList<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } };
        Assert.Throws<ArgumentOutOfRangeException>("index", () => list.RemoveRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(2, 2));
        Assert.Throws<ArgumentOutOfRangeException>("count", () => list.RemoveRange(4, 0));

        var enumerator = list.GetEnumerator();
        list.RemoveRange(3, 0);
        Assert.True(enumerator.MoveNext()); // An empty range does not invalidate enumerators.

        list.RemoveRange(1, 2);
        Assert.Equal(new[] { 1 }, list.Keys);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        list.RemoveRange(0, 1);
        Assert.Empty(list);
    }

    [Fact]
    public void DuplicateKeysKeepInsertionOrderAndRemoveSelectively()
    {
        var list = new OrderedKeyValueList<int, string>();
        list.Add(2, "a");
        list.Add(1, "b");
        list.Add(2, "c");
        list[2] = "d";
        list.Add(3, "e");

        Assert.Equal(new[] { 1, 2, 2, 2, 3 }, list.Keys);
        Assert.Equal(new[] { "b", "a", "c", "d", "e" }, list.Values);
        Assert.Equal("a", list[2]);
        Assert.Equal((1, 4), list.RangeOfKey(2));
        Assert.Equal(1, list.BinarySearch(2));
        Assert.Equal(1, list.GetLowerBound(2));
        Assert.Equal(3, list.GetUpperBound(2));

        Assert.False(list.Remove(2, "x"));
        Assert.False(list.Remove(4, "a"));
        Assert.True(list.Remove(2, "c"));
        Assert.Equal(new[] { "b", "a", "d", "e" }, list.Values);
        Assert.True(list.Remove(2));
        Assert.Equal("d", list[2]);

        Assert.Equal(0, list.RemoveAll(4));
        Assert.Equal(1, list.RemoveAll(2));
        Assert.Equal(new[] { 1, 3 }, list.Keys);
        Assert.Throws<KeyNotFoundException>(() => list[2]);
        Assert.Equal(~1, list.BinarySearch(2));
        Assert.Equal(-1, list.IndexOfValue("a"));
        Assert.Equal(1, list.IndexOfValue("e"));
    }

    [Fact]
    public void GenericCopyToValidatesArguments()
    {
        var list = new OrderedKeyValueList<int, string> { { 2, "two" }, { 1, "one" } };
        ICollection<KeyValuePair<int, string>> collection = list;

        var array = new KeyValuePair<int, string>[3];
        collection.CopyTo(array, 1);
        Assert.Equal(new KeyValuePair<int, string>[] { default, new(1, "one"), new(2, "two") }, array);

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, 4));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 2));

        ICollection<KeyValuePair<int, string>> empty = new OrderedKeyValueList<int, string>();
        empty.CopyTo(array, 3);
        Assert.Equal(new KeyValuePair<int, string>(2, "two"), array[2]);
    }

    [Fact]
    public void NonGenericEnumeratorReturnsKeyValuePairs()
    {
        var list = new OrderedKeyValueList<int, string> { { 2, "two" }, { 1, "one" } };
        var enumerator = ((IEnumerable)list).GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new KeyValuePair<int, string>(1, "one"), enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new KeyValuePair<int, string>(2, "two"), enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        using var generic = ((IEnumerable<KeyValuePair<int, string>>)list).GetEnumerator();
        Assert.True(generic.MoveNext());
        Assert.Equal(1, generic.Current.Key);
    }

    [Fact]
    public void ValueTypeKeysWithoutHotMethodSearchCorrectly()
    {
        // char has no specialized IHotMethod, so the default-comparer struct strategy is used.
        var list = new OrderedKeyValueList<char, int>();
        Assert.Null(list.HotMethod);
        foreach (var c in "dbfbd")
        {
            list.Add(c, c);
        }

        Assert.Equal("bbddf", string.Concat(list.Keys));
        Assert.True(list.ContainsKey('d'));
        Assert.False(list.ContainsKey('c'));
        Assert.Equal(~2, list.BinarySearch('c'));
        Assert.Equal(~5, list.BinarySearch('g'));
        Assert.Equal(-1, list.IndexOfKey('a'));
        Assert.Equal((2, 4), list.RangeOfKey('d'));
        Assert.Equal((4, 5), list.RangeOfKey('f'));
        Assert.Equal((-1, -1), list.RangeOfKey('e'));
        Assert.Equal(0, list.GetLowerBound('a'));
        Assert.Equal(2, list.GetLowerBound('c'));
        Assert.Equal(-1, list.GetLowerBound('g'));
        Assert.Equal(-1, list.GetUpperBound('a'));
        Assert.Equal(3, list.GetUpperBound('d'));
        Assert.Equal(4, list.GetUpperBound('z'));
        Assert.Equal(2, list.RemoveAll('b'));
        Assert.Equal("ddf", string.Concat(list.Keys));
    }

    [Fact]
    public void ComparableReferenceKeysSearchCorrectly()
    {
        // A custom comparer, and the default comparer (string implements IComparable<string>).
        var ordinal = new OrderedKeyValueList<string, int>(StringComparer.Ordinal);
        foreach (var key in new[] { "b", "B", "a", "c", "b" })
        {
            ordinal.Add(key, key.Length);
        }

        Assert.Equal(new[] { "B", "a", "b", "b", "c" }, ordinal.Keys);
        Assert.Equal((2, 4), ordinal.RangeOfKey("b"));
        Assert.Equal(1, ordinal.GetLowerBound("a"));
        Assert.Equal(-1, ordinal.GetLowerBound("d"));
        Assert.Equal(3, ordinal.GetUpperBound("b"));
        Assert.Equal(-1, ordinal.GetUpperBound("A"));
        Assert.False(ordinal.ContainsKey("A"));

        var list = new OrderedKeyValueList<string, int>();
        foreach (var key in new[] { "b", "a", "c", "b" })
        {
            list.Add(key, key.Length);
        }

        Assert.Equal(new[] { "a", "b", "b", "c" }, list.Keys);
        Assert.Equal((1, 3), list.RangeOfKey("b"));
        Assert.Equal(-1, list.GetLowerBound("d"));
        Assert.Equal(0, list.GetLowerBound("0"));
        Assert.Equal(2, list.GetUpperBound("b"));
        Assert.Equal(-1, list.GetUpperBound("0"));
        Assert.Equal(~3, list.BinarySearch("bb"));
        Assert.False(list.TryGetValue("d", out var missing));
        Assert.Equal(0, missing);
    }

    [Fact]
    public void NaNKeysSortFirstAndCanBeFound()
    {
        var list = new OrderedKeyValueList<double, string>();
        list.Add(1, "one");
        list.Add(double.NaN, "nan");
        list.Add(double.NegativeInfinity, "-inf");
        list.Add(double.NaN, "nan2");

        Assert.Equal(new[] { "nan", "nan2", "-inf", "one" }, list.Values);
        Assert.Equal("nan", list[double.NaN]);
        Assert.Equal((0, 2), list.RangeOfKey(double.NaN));
        Assert.Equal(2, list.RemoveAll(double.NaN));
        Assert.False(list.ContainsKey(double.NaN));
    }

    [Fact]
    public void LookupsAndStructEnumerationDoNotAllocate()
    {
        var numbers = new OrderedKeyValueList<int, int>();
        var chars = new OrderedKeyValueList<char, int>();
        var strings = new OrderedKeyValueList<string, int>();
        for (var i = 0; i < 64; i++)
        {
            numbers.Add(i % 16, i);
            chars.Add((char)('a' + (i % 16)), i);
            strings.Add((i % 16).ToString("D2"), i);
        }

        long total = 0;
        var allocated = AllocationHelper.Measure(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                total += Exercise(numbers, chars, strings);
            }
        });

        Assert.NotEqual(0, total);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0L, allocated);

        static long Exercise(OrderedKeyValueList<int, int> numbers, OrderedKeyValueList<char, int> chars, OrderedKeyValueList<string, int> strings)
        {
            long sum = 0;
            foreach (var pair in numbers)
            {
                sum += pair.Key + pair.Value;
            }

            sum += numbers[7] + chars['c'] + strings["05"];
            sum += numbers.TryGetValue(20, out var value) ? value : 1;
            sum += numbers.RangeOfKey(3).End + chars.GetUpperBound('q') + strings.GetLowerBound("10");
            return sum;
        }
    }

    private sealed class InspectableList : OrderedKeyValueList<string, string>
    {
        public string[] KeyArray => this.keys;

        public string[] ValueArray => this.values;
    }

    private sealed record GenericOnlyKey(int Value) : IComparable<object>
    {
        public int CompareTo(object? other) => this.Value.CompareTo(((GenericOnlyKey)other!).Value);
    }

    private sealed record TwoWayKey(int Value) : IComparable<object>, IComparable
    {
        int IComparable<object>.CompareTo(object? other) => this.Value.CompareTo(((TwoWayKey)other!).Value);

        int IComparable.CompareTo(object? other) => ((TwoWayKey)other!).Value.CompareTo(this.Value);
    }
}
