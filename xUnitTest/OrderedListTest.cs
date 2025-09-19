using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

namespace xUnitTest;

public class OrderedListClass : IComparable<OrderedListClass>
{
    public OrderedListClass(int id, int serial)
    {
        this.Id = id;
        this.Serial = serial;
    }

    public int Id { get; set; }

    public int Serial { get; }

    public int CompareTo(OrderedListClass? other)
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

    public class InternalComparer : IComparer<int>
    {
        public static InternalComparer Instance = new();

        public int Compare(int x, int y)
        {
            if (x < y)
            {
                return 1;
            }
            else if (x > y)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}

public class IntComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        if (x < y)
        {
            return -1;
        }
        else if (x > y)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}

public class OrderedListTest
{
    [Fact]
    public void Test1()
    {
        var ol = new OrderedList<int>() { 1, 3, 2, 0, 5, -10, 0, };
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, };
        Array.Sort(array);
        ol.SequenceEqual(array).IsTrue();

        ol = new OrderedList<int>(array, OrderedListClass.InternalComparer.Instance);
        ol.SequenceEqual(Enumerable.Reverse(array)).IsTrue();

        var ol2 = new OrderedList<OrderedListClass>();
        ol2.Add(new OrderedListClass(1, 0)); // 0
        ol2.Add(new OrderedListClass(3, 1)); // 1
        ol2.Add(new OrderedListClass(2, 2)); // 2
        ol2.Add(new OrderedListClass(0, 3)); // 3
        ol2.Add(new OrderedListClass(5, 4)); // 4
        ol2.Add(new OrderedListClass(-10, 5)); // 5
        ol2.Add(new OrderedListClass(2, 6)); // 6
        ol2.Add(new OrderedListClass(0, 7)); // 7
        ol2.Add(new OrderedListClass(2, 8)); // 8
        ol2.Add(new OrderedListClass(2, 9)); // 9

        var n = 0;
        ol2[n].Id.Is(-10);
        ol2[n++].Serial.Is(5);
        ol2[n].Id.Is(0);
        ol2[n++].Serial.Is(3);
        ol2[n].Id.Is(0);
        ol2[n++].Serial.Is(7);
        ol2[n].Id.Is(1);
        ol2[n++].Serial.Is(0);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(2);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(6);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(8);
        ol2[n].Id.Is(2);
        ol2[n++].Serial.Is(9);
        ol2[n].Id.Is(3);
        ol2[n++].Serial.Is(1);
        ol2[n].Id.Is(5);
        ol2[n++].Serial.Is(4);
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

        var ol = new OrderedList<int>(array);
        ol.SequenceEqual(sortedArray).IsTrue();

        ol = new OrderedList<int>();
        foreach (var x in array)
        {
            ol.Add(x);
        }

        ol.SequenceEqual(sortedArray).IsTrue();

        ol = new OrderedList<int>(array, new IntComparer());
        ol.SequenceEqual(sortedArray).IsTrue();

        ol = new OrderedList<int>();
        foreach (var x in array)
        {
            ol.Add(x);
        }

        ol.SequenceEqual(sortedArray).IsTrue();
    }

    [Fact]
    public void BoundTest()
    {
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, 2, };
        var array2 = new int[] { -10, 0, 0, 1, 2, 2, 3, 5, };

        var list = new OrderedList<int>(array);
        list.SequenceEqual(array2).IsTrue();

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
