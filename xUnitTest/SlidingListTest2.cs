// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arc.Collections;
using Xunit;

namespace XunitTest;

#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection

public class SlidingListTest2
{
    private const int MaxPosition = 0x7FFFFFFF;

    /// <summary>
    /// Forces the position window to start at <paramref name="position"/> so that the 31-bit
    /// wraparound can be tested without performing 2^31 operations.
    /// </summary>
    private static SlidingList<string> CreateAt(int capacity, int position)
    {
        var list = new SlidingList<string>(capacity);
        typeof(SlidingList<string>)
            .GetField("startPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(list, position);
        return list;
    }

    private static SlidingList<string> Filled(int capacity)
    {
        var list = new SlidingList<string>(capacity);
        for (var i = 0; i < capacity; i++)
        {
            list.Add("a" + i);
        }

        return list;
    }

    private static int Count(SlidingList<string> list) => ((ICollection<string>)list).Count;

    #region Construction

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Constructor_SetsCapacityAndEmptyState(int capacity)
    {
        var list = new SlidingList<string>(capacity);

        Assert.Equal(capacity, list.Capacity);
        Assert.Equal(0, list.Consumed);
        Assert.Equal(0, Count(list));
        Assert.Equal(0, list.StartPosition);
        Assert.Equal(0, list.EndPosition);
        Assert.Equal(capacity > 0, list.CanAdd);
        Assert.Null(list.FirstOrDefault);
        Assert.Empty(list.ToArray());
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingList<string>(-1));

    #endregion

    #region Add

    [Fact]
    public void Add_ReturnsConsecutivePositions()
    {
        var list = new SlidingList<string>(4);

        Assert.Equal(0, list.Add("a"));
        Assert.Equal(1, list.Add("b"));
        Assert.Equal(2, list.Add("c"));
        Assert.Equal(3, list.Consumed);
        Assert.Equal(0, list.StartPosition);
        Assert.Equal(3, list.EndPosition);
        Assert.Equal(3, Count(list));
        Assert.Equal("a", list.FirstOrDefault);
    }

    [Fact]
    public void Add_ReturnsEndPosition()
    {
        var list = new SlidingList<string>(4);
        for (var i = 0; i < 4; i++)
        {
            var expected = list.EndPosition;
            Assert.Equal(expected, list.Add("a" + i));
        }
    }

    [Fact]
    public void Add_WhenFull_ReturnsMinusOne()
    {
        var list = Filled(3);

        Assert.False(list.CanAdd);
        Assert.Equal(-1, list.Add("x"));
        Assert.Equal(3, list.Consumed);
    }

    [Fact]
    public void Add_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => new SlidingList<string>(4).Add(null!));

    [Fact]
    public void Add_ZeroCapacity_ReturnsMinusOne()
        => Assert.Equal(-1, new SlidingList<string>(0).Add("a"));

    #endregion

    #region Get / Set

    [Fact]
    public void GetSet_RoundTrip()
    {
        var list = new SlidingList<string>(4);
        var position = list.Add("a");

        Assert.Equal("a", list.Get(position));
        Assert.True(list.Set(position, "b"));
        Assert.Equal("b", list.Get(position));
        Assert.Equal(1, Count(list));
    }

    [Fact]
    public void Get_OutsideWindow_ReturnsNull()
    {
        var list = Filled(4);

        Assert.Null(list.Get(-1));
        Assert.Null(list.Get(list.StartPosition + 4));
        Assert.Null(list.Get(MaxPosition));
    }

    [Fact]
    public void Set_OutsideWindow_ReturnsFalse()
    {
        var list = new SlidingList<string>(4);

        Assert.False(list.Set(-1, "x"));
        Assert.False(list.Set(list.StartPosition + 4, "x"));
        Assert.Equal(0, list.Consumed);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void Set_BeyondEndPosition_ExtendsConsumed(int offset, int expectedConsumed)
    {
        var list = new SlidingList<string>(4);

        Assert.True(list.Set(list.StartPosition + offset, "x"));
        Assert.Equal(expectedConsumed, list.Consumed);
        Assert.Equal(1, Count(list));
        Assert.Equal(new[] { "x" }, list.ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Set_AfterHeadWrapped_KeepsConsumedConsistent(int slides)
    {
        // Slide the head forward so that the window wraps around the end of the internal array.
        var list = Filled(4);
        for (var i = 0; i < slides; i++)
        {
            Assert.True(list.Remove(list.StartPosition));
        }

        var position = MaxPosition & (list.StartPosition + 3);
        Assert.True(list.Set(position, "x"));

        Assert.Equal("x", list.Get(position));
        Assert.Equal(4, list.Consumed);
        Assert.Equal(4 - slides + 1, Count(list));
    }

    [Fact]
    public void Set_Null_Throws()
    {
        var list = new SlidingList<string>(4);
        var position = list.Add("a");

        Assert.Throws<ArgumentNullException>(() => list.Set(position, null!));
    }

    #endregion

    #region Remove / TrySlide

    [Fact]
    public void Remove_Head_SlidesTheWindow()
    {
        var list = Filled(3);

        Assert.True(list.Remove(list.StartPosition));

        Assert.Equal(1, list.StartPosition);
        Assert.Equal(2, list.Consumed);
        Assert.Equal(2, Count(list));
        Assert.True(list.CanAdd);
        Assert.Equal("a1", list.FirstOrDefault);
    }

    [Fact]
    public void Remove_Middle_LeavesAHole()
    {
        var list = Filled(3);

        Assert.True(list.Remove(1));

        Assert.Equal(0, list.StartPosition);
        Assert.Equal(3, list.Consumed); // the hole still occupies a slot
        Assert.Equal(2, Count(list));
        Assert.False(list.CanAdd);
        Assert.Equal(new[] { "a0", "a2" }, list.ToArray());
    }

    [Fact]
    public void Remove_Twice_ReturnsFalse()
    {
        var list = Filled(3);

        Assert.True(list.Remove(1));
        Assert.False(list.Remove(1));
    }

    [Fact]
    public void Remove_OutsideWindow_ReturnsFalse()
    {
        var list = Filled(3);

        Assert.False(list.Remove(-1));
        Assert.False(list.Remove(list.StartPosition + 3));
    }

    [Fact]
    public void TrySlide_ReclaimsLeadingHoles()
    {
        var list = Filled(4);

        // Remove the middle elements first, so no implicit slide happens.
        Assert.True(list.Remove(1));
        Assert.True(list.Remove(2));
        Assert.Equal(0, list.TrySlide());

        Assert.True(list.Remove(0)); // removing the head slides over positions 0, 1 and 2
        Assert.Equal(3, list.StartPosition);
        Assert.Equal(1, list.Consumed);
        Assert.Equal(0, list.TrySlide());
    }

    [Fact]
    public void TrySlide_OnEmptyList_ReturnsZero()
    {
        Assert.Equal(0, new SlidingList<string>(0).TrySlide());
        Assert.Equal(0, new SlidingList<string>(4).TrySlide());
    }

    [Fact]
    public void RemoveAll_EmptiesTheListAndKeepsPositions()
    {
        var list = Filled(3);
        for (var position = 0; position < 3; position++)
        {
            Assert.True(list.Remove(position));
        }

        Assert.Equal(0, list.Consumed);
        Assert.Equal(0, Count(list));
        Assert.Equal(3, list.StartPosition);
        Assert.Equal(3, list.Add("next"));
    }

    #endregion

    #region Count / ToArray / CopyTo

    [Fact]
    public void Count_ExcludesHoles_ConsumedIncludesThem()
    {
        var list = Filled(4);
        Assert.True(list.Remove(2));

        Assert.Equal(4, list.Consumed);
        Assert.Equal(3, Count(list));
        Assert.Equal(3, list.ToArray().Length);
        Assert.Equal(3, list.Count()); // LINQ agrees with the enumeration
    }

    [Fact]
    public void ToArray_SkipsHolesAndKeepsOrder()
    {
        var list = Filled(5);
        Assert.True(list.Remove(1));
        Assert.True(list.Remove(3));

        Assert.Equal(new[] { "a0", "a2", "a4" }, list.ToArray());
    }

    [Fact]
    public void ToArray_WhenWrapped_KeepsOrder()
    {
        var list = Filled(4);
        Assert.True(list.Remove(0));
        Assert.True(list.Remove(1));
        list.Add("b0");
        list.Add("b1");

        Assert.Equal(new[] { "a2", "a3", "b0", "b1" }, list.ToArray());
    }

    [Fact]
    public void CopyTo_CopiesLiveElementsAtTheGivenIndex()
    {
        var list = Filled(4);
        Assert.True(list.Remove(1));
        var array = new string[5];

        list.CopyTo(array, 2);

        Assert.Equal(new string?[] { null, null, "a0", "a2", "a3" }, array);
    }

    [Fact]
    public void CopyTo_InvalidArguments_Throw()
    {
        var list = Filled(4);

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentException>(() => list.CopyTo(new string[3]));
        Assert.Throws<ArgumentException>(() => list.CopyTo(new string[4], 1));
    }

    #endregion

    #region IList surface

    [Fact]
    public void IndexOf_ReturnsPositionUsableWithTheIndexer()
    {
        var list = Filled(4);
        Assert.True(list.Remove(0));
        Assert.True(list.Remove(1));
        var position = list.Add("target");

        Assert.Equal(position, list.IndexOf("target"));
        Assert.Equal("target", list[position]);
        Assert.True(list.Contains("target"));
    }

    [Fact]
    public void IndexOf_NotFound_ReturnsMinusOne()
    {
        var list = Filled(2);

        Assert.Equal(-1, list.IndexOf("nope"));
        Assert.Equal(-1, list.IndexOf(null!));
        Assert.False(list.Contains(null!)); // an empty slot is not an element
    }

    [Fact]
    public void RemoveByValue_RemovesTheFirstOccurrence()
    {
        var list = new SlidingList<string>(4);
        list.Add("a");
        var second = list.Add("dup");
        list.Add("dup");

        Assert.True(list.Remove("dup"));
        Assert.Null(list.Get(second));
        Assert.Equal(2, Count(list));
        Assert.False(list.Remove("missing"));
    }

    [Fact]
    public void Indexer_Get_InvalidPosition_Throws()
    {
        var list = Filled(2);
        Assert.True(list.Remove(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => list[1]); // the slot is empty
        Assert.Throws<ArgumentOutOfRangeException>(() => list[99]); // outside the window
    }

    [Fact]
    public void Indexer_Set_OutsideWindow_Throws()
    {
        var list = new SlidingList<string>(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => { list[99] = "x"; });
    }

    [Fact]
    public void Insert_StoresWithoutShifting()
    {
        var list = Filled(3);

        list.Insert(1, "replaced");

        Assert.Equal(new[] { "a0", "replaced", "a2" }, list.ToArray());
    }

    [Fact]
    public void RemoveAt_InvalidPosition_Throws()
    {
        var list = Filled(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(99));
        list.RemoveAt(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1)); // already empty
    }

    [Fact]
    public void IsReadOnly_IsFalse() => Assert.False(Filled(1).IsReadOnly);

    #endregion

    #region Resize / Clear

    [Theory]
    [InlineData(8)]
    [InlineData(5)]
    [InlineData(4)]
    public void Resize_PreservesElementsAndPositions(int capacity)
    {
        var list = Filled(4);
        Assert.True(list.Remove(0)); // head slides to position 1
        var start = list.StartPosition;

        Assert.True(list.Resize(capacity));

        Assert.Equal(capacity, list.Capacity);
        Assert.Equal(start, list.StartPosition);
        Assert.Equal(3, list.Consumed);
        Assert.Equal(new[] { "a1", "a2", "a3" }, list.ToArray());
        Assert.Equal("a2", list.Get(start + 1));
    }

    [Fact]
    public void Resize_TooSmall_ReturnsFalseAndKeepsTheList()
    {
        var list = Filled(4);

        Assert.False(list.Resize(3));
        Assert.Equal(4, list.Capacity);
        Assert.Equal(4, Count(list));
    }

    [Fact]
    public void Resize_SameCapacity_ReturnsTrue() => Assert.True(Filled(4).Resize(4));

    [Fact]
    public void Resize_NegativeCapacity_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new SlidingList<string>(4).Resize(-1));

    [Fact]
    public void Clear_EmptiesTheListAndDoesNotReusePositions()
    {
        var list = Filled(3);
        var first = list.StartPosition;

        list.Clear();

        Assert.Equal(0, list.Consumed);
        Assert.Equal(0, Count(list));
        Assert.Empty(list.ToArray());
        Assert.Null(list.Get(first)); // positions handed out before Clear are gone
        Assert.Equal(3, list.Add("new")); // and are not handed out again
    }

    #endregion

    #region Enumeration

    [Fact]
    public void Enumerator_SkipsHolesAndKeepsOrder()
    {
        var list = Filled(4);
        Assert.True(list.Remove(1));

        Assert.Equal(new[] { "a0", "a2", "a3" }, list.ToList());
    }

    [Fact]
    public void Enumerator_OnEmptyList_YieldsNothing()
        => Assert.Empty(new SlidingList<string>(4).ToList());

    [Fact]
    public void Enumerator_AfterModification_Throws()
    {
        var list = Filled(4);
        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        Assert.True(list.Remove(2));

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_Reset_RestartsFromTheHead()
    {
        var list = Filled(4);
        Assert.True(list.Remove(0));
        Assert.True(list.Remove(1));
        list.Add("b0");

        IEnumerator enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        enumerator.Reset();

        var items = new List<object?>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        Assert.Equal(new object?[] { "a2", "a3", "b0" }, items);
    }

    #endregion

    #region Position wraparound

    [Theory]
    [InlineData(MaxPosition - 1)]
    [InlineData(MaxPosition)]
    public void Positions_WrapAroundTheThirtyOneBitBoundary(int start)
    {
        var list = CreateAt(4, start);

        var positions = new int[4];
        for (var i = 0; i < 4; i++)
        {
            positions[i] = list.Add("a" + i);
            Assert.True(positions[i] >= 0);
        }

        Assert.Equal(MaxPosition & (start + 4), list.EndPosition);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal("a" + i, list.Get(positions[i]));
        }

        // The wrapped positions round-trip through the whole API.
        Assert.Equal(positions[2], list.IndexOf("a2"));
        Assert.True(list.Set(positions[3], "z"));
        Assert.Equal("z", list[positions[3]]);
        Assert.True(list.Remove(positions[0]));
        Assert.Equal(positions[1], list.StartPosition);
        Assert.Equal(new[] { "a1", "a2", "z" }, list.ToArray());
    }

    #endregion
}
