using System;
using System.Collections.Generic;
using Arc.Collections;
using Xunit;

namespace XunitTest;

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

public class OrderedKeyValueListTest2
{
    [Fact]
    public void Add_And_Indexer_Get()
    {
        var list = new OrderedKeyValueList<int, string>();

        list.Add(3, "C");
        list.Add(1, "A");
        list.Add(2, "B");

        Assert.Equal(3, list.Count);
        Assert.Equal("A", list[1]);
        Assert.Equal("B", list[2]);
        Assert.Equal("C", list[3]);
    }

    [Fact]
    public void Add_DuplicateKey_Throws()
    {
        var list = new OrderedKeyValueList<int, string>();

        list.Add(1, "A");

        Assert.Throws<ArgumentException>(() => list.Add(1, "B"));
    }

    [Fact]
    public void Indexer_Set_UpdatesExistingValue()
    {
        var list = new OrderedKeyValueList<int, string>();

        list.Add(1, "A");
        list[1] = "B";

        Assert.Equal(1, list.Count);
        Assert.Equal("B", list[1]);
    }

    [Fact]
    public void Indexer_Set_InsertsNewValue()
    {
        var list = new OrderedKeyValueList<int, string>();

        list[3] = "C";
        list[1] = "A";
        list[2] = "B";

        Assert.Equal(3, list.Count);
        Assert.Equal("A", list[1]);
        Assert.Equal("B", list[2]);
        Assert.Equal("C", list[3]);
    }

    [Fact]
    public void Keys_AreSorted()
    {
        var list = new OrderedKeyValueList<int, string>();

        list.Add(5, "E");
        list.Add(1, "A");
        list.Add(3, "C");
        list.Add(2, "B");
        list.Add(4, "D");

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list.Keys);
    }

    [Fact]
    public void ContainsKey_ReturnsExpectedResult()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 3, "C" },
            { 5, "E" },
        };

        Assert.True(list.ContainsKey(1));
        Assert.True(list.ContainsKey(3));
        Assert.False(list.ContainsKey(2));
        Assert.False(list.ContainsKey(6));
    }

    [Fact]
    public void ContainsValue_ReturnsExpectedResult()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
        };

        Assert.True(list.ContainsValue("A"));
        Assert.True(list.ContainsValue("B"));
        Assert.False(list.ContainsValue("C"));
    }

    [Fact]
    public void TryGetValue_ReturnsExpectedResult()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.True(list.TryGetValue(1, out var value));
        Assert.Equal("A", value);

        Assert.False(list.TryGetValue(2, out value));
        Assert.Null(value);
    }

    [Fact]
    public void IndexOfKey_ReturnsSortedIndex()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 30, "C" },
            { 10, "A" },
            { 20, "B" },
        };

        Assert.Equal(0, list.IndexOfKey(10));
        Assert.Equal(1, list.IndexOfKey(20));
        Assert.Equal(2, list.IndexOfKey(30));
        Assert.Equal(-1, list.IndexOfKey(25));
    }

    [Fact]
    public void BinarySearch_ReturnsExpectedResult()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 10, "A" },
            { 20, "B" },
            { 30, "C" },
        };

        Assert.Equal(0, list.BinarySearch(10));
        Assert.Equal(1, list.BinarySearch(20));
        Assert.Equal(2, list.BinarySearch(30));

        Assert.Equal(~0, list.BinarySearch(5));
        Assert.Equal(~1, list.BinarySearch(15));
        Assert.Equal(~3, list.BinarySearch(40));
    }

    [Fact]
    public void GetLowerBound_ReturnsExpectedIndex()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 10, "A" },
            { 20, "B" },
            { 30, "C" },
        };

        Assert.Equal(0, list.GetLowerBound(5));
        Assert.Equal(0, list.GetLowerBound(10));
        Assert.Equal(1, list.GetLowerBound(15));
        Assert.Equal(1, list.GetLowerBound(20));
        Assert.Equal(2, list.GetLowerBound(25));
        Assert.Equal(2, list.GetLowerBound(30));
        Assert.Equal(-1, list.GetLowerBound(40));
    }

    [Fact]
    public void GetUpperBound_ReturnsExpectedIndex()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 10, "A" },
            { 20, "B" },
            { 30, "C" },
        };

        Assert.Equal(-1, list.GetUpperBound(5));
        Assert.Equal(0, list.GetUpperBound(10));
        Assert.Equal(0, list.GetUpperBound(15));
        Assert.Equal(1, list.GetUpperBound(20));
        Assert.Equal(1, list.GetUpperBound(25));
        Assert.Equal(2, list.GetUpperBound(30));
        Assert.Equal(2, list.GetUpperBound(40));
    }

    [Fact]
    public void RangeOfKey_ReturnsExpectedRange()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 10, "A" },
            { 20, "B" },
            { 30, "C" },
        };

        Assert.Equal((1, 2), list.RangeOfKey(20));
        Assert.Equal((-1, -1), list.RangeOfKey(25));
    }

    [Fact]
    public void Remove_RemovesExpectedItem()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
            { 3, "C" },
        };

        Assert.True(list.Remove(2));

        Assert.Equal(2, list.Count);
        Assert.False(list.ContainsKey(2));
        Assert.Equal(new[] { 1, 3 }, list.Keys);
    }

    [Fact]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.False(list.Remove(2));
        Assert.Equal(1, list.Count);
    }

    [Fact]
    public void Remove_KeyAndValue_RequiresBothToMatch()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.False(list.Remove(1, "B"));
        Assert.True(list.ContainsKey(1));

        Assert.True(list.Remove(1, "A"));
        Assert.False(list.ContainsKey(1));
    }

    [Fact]
    public void RemoveAt_FirstMiddleLast()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
            { 3, "C" },
            { 4, "D" },
            { 5, "E" },
        };

        list.RemoveAt(0);
        Assert.Equal(new[] { 2, 3, 4, 5 }, list.Keys);

        list.RemoveAt(1);
        Assert.Equal(new[] { 2, 4, 5 }, list.Keys);

        list.RemoveAt(list.Count - 1);
        Assert.Equal(new[] { 2, 4 }, list.Keys);
    }

    [Fact]
    public void RemoveAt_WhenCapacityEqualsCount_DoesNotThrow()
    {
        var list = new OrderedKeyValueList<int, string>(3)
        {
            { 1, "A" },
            { 2, "B" },
            { 3, "C" },
        };

        Assert.Equal(3, list.Count);
        Assert.Equal(3, list.Capacity);

        list.RemoveAt(2);

        Assert.Equal(2, list.Count);
        Assert.Equal(new[] { 1, 2 }, list.Keys);
    }

    [Fact]
    public void RemoveAt_InvalidIndex_Throws()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
        };

        list.Clear();

        Assert.Equal(0, list.Count);
        Assert.Empty(list);
        Assert.Empty(list.Keys);
        Assert.Empty(list.Values);
    }

    [Fact]
    public void Capacity_CanGrowAndShrink()
    {
        var list = new OrderedKeyValueList<int, string>(2)
        {
            { 1, "A" },
            { 2, "B" },
        };

        list.Capacity = 10;

        Assert.Equal(10, list.Capacity);
        Assert.Equal("A", list[1]);
        Assert.Equal("B", list[2]);

        list.Capacity = 2;

        Assert.Equal(2, list.Capacity);
    }

    [Fact]
    public void Capacity_SmallerThanCount_Throws()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 1);
    }

    [Fact]
    public void Constructor_FromDictionary_SortsItems()
    {
        var dictionary = new Dictionary<int, string>
        {
            [30] = "C",
            [10] = "A",
            [20] = "B",
        };

        var list = new OrderedKeyValueList<int, string>(dictionary);

        Assert.Equal(new[] { 10, 20, 30 }, list.Keys);
        Assert.Equal(new[] { "A", "B", "C" }, list.Values);
    }

    [Fact]
    public void Constructor_WithCustomComparer_UsesComparer()
    {
        var list = new OrderedKeyValueList<int, string>(Comparer<int>.Create((x, y) => y.CompareTo(x)))
        {
            { 1, "A" },
            { 3, "C" },
            { 2, "B" },
        };

        Assert.Equal(new[] { 3, 2, 1 }, list.Keys);
        Assert.Equal("C", list[3]);
    }

    [Fact]
    public void Enumeration_ReturnsSortedItems()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 3, "C" },
            { 1, "A" },
            { 2, "B" },
        };

        var result = new List<KeyValuePair<int, string>>();

        foreach (var item in list)
        {
            result.Add(item);
        }

        Assert.Equal(
            new[]
            {
                new KeyValuePair<int, string>(1, "A"),
                new KeyValuePair<int, string>(2, "B"),
                new KeyValuePair<int, string>(3, "C"),
            },
            result);
    }

    [Fact]
    public void Enumerator_DetectsModification()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
            { 2, "B" },
        };

        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        list[1] = "X";

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void KeyList_IsReadOnly()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.True(list.Keys.IsReadOnly);

        Assert.Throws<NotSupportedException>(() => list.Keys.Add(2));
        Assert.Throws<NotSupportedException>(() => list.Keys.Insert(0, 2));
        Assert.Throws<NotSupportedException>(() => list.Keys.RemoveAt(0));
    }

    [Fact]
    public void ValueList_IsReadOnly()
    {
        var list = new OrderedKeyValueList<int, string>
        {
            { 1, "A" },
        };

        Assert.True(list.Values.IsReadOnly);

        Assert.Throws<NotSupportedException>(() => list.Values.Add("B"));
        Assert.Throws<NotSupportedException>(() => list.Values.Insert(0, "B"));
        Assert.Throws<NotSupportedException>(() => list.Values.RemoveAt(0));
    }

    [Fact]
    public void TrimExcess_ReducesCapacity()
    {
        var list = new OrderedKeyValueList<int, string>(100)
        {
            { 1, "A" },
            { 2, "B" },
        };

        list.TrimExcess();

        Assert.Equal(list.Count, list.Capacity);
    }

    [Fact]
    public void StringComparerOrdinalIgnoreCase_DetectsDuplicateKey()
    {
        var list = new OrderedKeyValueList<string, int>(StringComparer.OrdinalIgnoreCase);

        list.Add("abc", 1);

        Assert.Throws<ArgumentException>(() => list.Add("ABC", 2));
    }

    [Fact]
    public void StringComparerOrdinalIgnoreCase_IndexerUpdatesEquivalentKey()
    {
        var list = new OrderedKeyValueList<string, int>(StringComparer.OrdinalIgnoreCase);

        list.Add("abc", 1);
        list["ABC"] = 2;

        Assert.Equal(1, list.Count);
        Assert.Equal(2, list["abc"]);
        Assert.Equal(2, list["ABC"]);
    }
}
