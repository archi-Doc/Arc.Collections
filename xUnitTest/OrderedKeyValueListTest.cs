using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest;

public class OrderedKeyValueListClass : IComparable<OrderedKeyValueListClass>
{
    public OrderedKeyValueListClass(int id)
    {
        this.Id = id;
        this.Serial = serial++;
    }

    public int Id { get; set; }

    public int Serial { get; }

    private static int serial;

    public int CompareTo(OrderedKeyValueListClass? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (this.Id < other.Id)
        {
            return -1;
        }
        else if (this.Id > other.Id)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public override string ToString() => $"{this.Id} ({this.Serial})";
}

public class OrderedKeyValueListTest
{
    [Fact]
    public void Test1()
    {
        var ol = new OrderedKeyValueList<int, int>();
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, };
        Array.Sort(array);
        foreach (var x in array)
        {
            ol.Add(x, x);
        }
        ol.Keys.SequenceEqual(array).IsTrue();
        ol.Values.SequenceEqual(array).IsTrue();

        var ol2 = new OrderedKeyValueList<int, OrderedKeyValueListClass>();
        ol2.Add(7, new OrderedKeyValueListClass(0)); // 0
        ol2.Add(0, new OrderedKeyValueListClass(1)); // 1
        ol2.Add(5, new OrderedKeyValueListClass(-10)); // 2
        ol2.Add(3, new OrderedKeyValueListClass(0)); // 3
        ol2.Add(8, new OrderedKeyValueListClass(2)); // 4
        ol2.Add(4, new OrderedKeyValueListClass(5)); // 5
        ol2.Add(6, new OrderedKeyValueListClass(2)); // 6
        ol2.Add(2, new OrderedKeyValueListClass(2)); // 7
        ol2.Add(1, new OrderedKeyValueListClass(3)); // 8
        ol2.Add(9, new OrderedKeyValueListClass(2)); // 9

        var n = 0;
        ol2[n].Id.Is(1);
        ol2[n++].Serial.Is(1);
        ol2[n].Id.Is(3);
        ol2[n++].Serial.Is(8);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(7);
        ol2[n].Id.Is(0);
        ol2[n++].Serial.Is(3);
        ol2[n].Id.Is(5);
        ol2[n++].Serial.Is(5);
        ol2[n].Id.Is(-10);
        ol2[n++].Serial.Is(2);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(6);
        ol2[n].Id.Is(0);
        ol2[n++].Serial.Is(0);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(4);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(9);
    }

    [Fact]
    public void Random()
    {
        var r = new Random(12);

        for (var n = 0; n < 10; n++)
        {
            RandomTest(r, -100, 100, 100, false);
            RandomTest(r, -100, 100, 100, true);
            RandomTest(r, -500, 500, 100, false);
            RandomTest(r, -600, 600, 1000, true);
        }
    }

    private void RandomTest(Random r, int start, int end, int count, bool duplicate)
    {
        IEnumerable<int> e;
        if (duplicate)
        {
            e = TestHelper.GetRandomNumbers(r, start, end, count);
        }
        else
        {
            e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);
        }

        var array = e.ToArray();
        var sortedArray = (int[])array.Clone();
        Array.Sort(sortedArray);

        var ol = new OrderedKeyValueList<int, int>();
        foreach (var x in array)
        {
            ol.Add(x, x);
        }
        ol.Keys.SequenceEqual(sortedArray).IsTrue();
        ol.Values.SequenceEqual(sortedArray).IsTrue();
    }

    [Fact]
    public void BoundTest()
    {
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, 2, };
        var array2 = new int[] { -10, 0, 0, 1, 2, 2, 3, 5, };

        var list = new OrderedKeyValueList<int, int>();
        foreach (var x in array)
        {
            list.Add(x, x);
        }

        list.Keys.SequenceEqual(array2).IsTrue();

        list.GetLowerBound(-11).Is(0);
        list.GetLowerBound(-10).Is(0);
        list.GetLowerBound(-9).Is(1);
        list.GetLowerBound(0).Is(1);
        list.GetLowerBound(1).Is(3);
        list.GetLowerBound(2).Is(4);
        list.GetLowerBound(3).Is(6);
        list.GetLowerBound(4).Is(7);
        list.GetLowerBound(5).Is(7);
        list.GetLowerBound(6).Is(-1);

        list.GetUpperBound(-11).Is(-1);
        list.GetUpperBound(-10).Is(0);
        list.GetUpperBound(-9).Is(0);
        list.GetUpperBound(0).Is(2);
        list.GetUpperBound(1).Is(3);
        list.GetUpperBound(2).Is(5);
        list.GetUpperBound(3).Is(6);
        list.GetUpperBound(4).Is(6);
        list.GetUpperBound(5).Is(7);
        list.GetUpperBound(6).Is(7);
    }
}
