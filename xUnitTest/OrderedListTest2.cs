using System;
using System.Collections.Generic;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class OrderedListTest2
{
    [Fact]
    public void Add_SortsElements()
    {
        var list = new OrderedList<int>();

        list.Add(3);
        list.Add(1);
        list.Add(2);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Add_AllowsDuplicates()
    {
        var list = new OrderedList<int>();

        list.Add(2);
        list.Add(1);
        list.Add(2);
        list.Add(2);
        list.Add(3);

        Assert.Equal(5, list.Count);
        Assert.Equal(new[] { 1, 2, 2, 2, 3 }, list);
    }

    [Fact]
    public void Constructor_SortsCollection()
    {
        var list = new OrderedList<int>(new[] { 5, 1, 4, 2, 3 });

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    public void IndexOf_ReturnsFirstOccurrence(int value, int expected)
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 2, 3, 4 });

        Assert.Equal(expected, list.IndexOf(value));
    }

    [Fact]
    public void IndexOf_ReturnsMinusOne_WhenNotFound()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 3 });

        Assert.Equal(-1, list.IndexOf(4));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    [InlineData(5, -1)]
    public void GetLowerBound_ReturnsExpectedIndex(int value, int expected)
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 2, 3, 4 });

        Assert.Equal(expected, list.GetLowerBound(value));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    public void GetUpperBound_ReturnsExpectedIndex(int value, int expected)
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 2, 3, 4 });

        Assert.Equal(expected, list.GetUpperBound(value));
    }

    [Fact]
    public void Contains_ReturnsExpectedResult()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 3 });

        Assert.True(list.Contains(2));
        Assert.False(list.Contains(4));
    }

    [Fact]
    public void Remove_RemovesFirstOccurrence()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 2, 3 });

        Assert.True(list.Remove(2));

        Assert.Equal(new[] { 1, 2, 2, 3 }, list);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenNotFound()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 3 });

        Assert.False(list.Remove(4));
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void BinarySearch_FindsExistingValue()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 2, 3, 4 });

        var index = list.BinarySearch(2);

        Assert.InRange(index, 1, 2);
        Assert.Equal(2, list[index]);
    }

    [Fact]
    public void BinarySearch_ReturnsInsertionIndex_WhenNotFound()
    {
        var list = new OrderedList<int>(new[] { 1, 3, 5 });

        var index = list.BinarySearch(4);

        Assert.Equal(2, ~index);
    }

    [Fact]
    public void CustomComparer_SortsUsingComparer()
    {
        var list = new OrderedList<int>(
            new[] { 1, 3, 2, 5, 4 },
            Comparer<int>.Create(static (x, y) => y.CompareTo(x)));

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, list);
    }

    [Fact]
    public void IndexerSetter_Throws()
    {
        var list = new OrderedList<int>(new[] { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => list[1] = 10);
    }

    [Fact]
    public void EmptyList_BoundsAreMinusOne()
    {
        var list = new OrderedList<int>();

        Assert.Equal(-1, list.GetLowerBound(1));
        Assert.Equal(-1, list.GetUpperBound(1));
        Assert.Equal(-1, list.IndexOf(1));
        Assert.False(list.Contains(1));
    }
}
