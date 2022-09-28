using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

namespace xUnitTest;

public class OrderedMapTestClass : IComparable<OrderedMapTestClass>
{
    public OrderedMapTestClass(int id)
    {
        this.Id = id;
    }

    public int Id { get; set; }

    public int CompareTo(OrderedMapTestClass? other)
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

    public override string ToString() => $"{this.Id}";
}

public class OrderedMapTestComparer : IComparer<OrderedMapTestClass>
{
    public int Compare(OrderedMapTestClass? x, OrderedMapTestClass? y)
    {// -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
        if (x == null)
        {
            return y == null ? 0 : -1;
        }
        else if (y == null)
        {
            return 1;
        }

        if (x.Id < y.Id)
        {
            return -1;
        }
        else if (x.Id > y.Id)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}

public class OrderedMapTestComparerInv : IComparer<OrderedMapTestClass>
{
    public int Compare(OrderedMapTestClass? x, OrderedMapTestClass? y)
    {// -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
        if (x == null)
        {
            return y == null ? 0 : 1;
        }
        else if (y == null)
        {
            return -1;
        }

        if (x.Id < y.Id)
        {
            return 1;
        }
        else if (x.Id > y.Id)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }
}

public class OrderedMapTest
{
    [Fact]
    public void Test1()
    {
        var sd = new SortedDictionary<int, int>();
        var om = new OrderedMap<int, int>();

        var os = new OrderedSet<int>();
        os.Add(3);

        AddAndValidate(0, 0);
        RemoveAndValidate(0);

        var r = new Random(12);

        Clear();

        void Clear()
        {
            sd.Clear();
            om.Clear();
        }

        void AddAndValidate(int x, int y)
        {
            sd.Add(x, y);
            om.AddAndValidate(x, y);
            om.SequenceEqual(sd).IsTrue();
        }

        void RemoveAndValidate(int x)
        {
            sd.Remove(x);
            om.RemoveAndValidate(x);
            om.SequenceEqual(sd).IsTrue();
        }
    }

    [Fact]
    public void TestNull()
    {
        var om = new OrderedMap<int?, OrderedMapTestClass>();
        om.Add(0, new OrderedMapTestClass(0));
        om.Add(null, new OrderedMapTestClass(999));
        om.Add(2, new OrderedMapTestClass(2));
        om.Add(1, new OrderedMapTestClass(1));
        om.Add(5, new OrderedMapTestClass(5));

        om.Count.Is(5);
        om[null].Id.Is(999);
        om[0].Id.Is(0);
        om[1].Id.Is(1);
        om[2].Id.Is(2);
        om[5].Id.Is(5);

        om.Keys.SequenceEqual(new int?[] { null, 0, 1, 2, 5, }).IsTrue();
        om.Values.Select(x => x.Id).SequenceEqual(new int[] { 999, 0, 1, 2, 5, }).IsTrue();
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
        var ss = new SortedList<int, int>();
        var om = new OrderedMap<int, int>();
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
        foreach (var x in array)
        {
            ss[x] = x; // ss.Add(x, x);
            om.Add(x, x);
            om.Validate().IsTrue();
        }

        ss.SequenceEqual(om).IsTrue();

        var branch = r.Next(3);
        if (branch == 1)
        {
            e = TestHelper.GetRandomNumbers(r, start, end, count);
            array = e.ToArray();
        }
        else
        {
            TestHelper.Shuffle(r, array);
        }

        foreach (var x in array)
        {
            ss.Remove(x);
            om.Remove(x);
            om.Validate().IsTrue();
        }

        ss.SequenceEqual(om).IsTrue();
        if (branch != 1)
        {
            ss.Count.Is(0);
            om.Count.Is(0);
        }
    }

    [Fact]
    public void BoundTest()
    {
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, 2, };
        var array2 = new int[] { -10, 0, 1, 2, 3, 5, };

        var map = new OrderedMap<int, int>();
        foreach (var x in array)
        {
            map.Add(x, x);
        }

        map.Keys.SequenceEqual(array2).IsTrue();

        map.GetLowerBound(-11)!.Key.Is(-10);
        map.GetLowerBound(-10)!.Key.Is(-10);
        map.GetLowerBound(-9)!.Key.Is(0);
        map.GetLowerBound(0)!.Key.Is(0);
        map.GetLowerBound(1)!.Key.Is(1);
        map.GetLowerBound(2)!.Key.Is(2);
        map.GetLowerBound(3)!.Key.Is(3);
        map.GetLowerBound(4)!.Key.Is(5);
        map.GetLowerBound(5)!.Key.Is(5);
        map.GetLowerBound(6).IsNull();

        map.GetUpperBound(-11).IsNull();
        map.GetUpperBound(-10)!.Key.Is(-10);
        map.GetUpperBound(-9)!.Key.Is(-10);
        map.GetUpperBound(0)!.Key.Is(0);
        map.GetUpperBound(1)!.Key.Is(1);
        map.GetUpperBound(2)!.Key.Is(2);
        map.GetUpperBound(3)!.Key.Is(3);
        map.GetUpperBound(4)!.Key.Is(3);
        map.GetUpperBound(5)!.Key.Is(5);
        map.GetUpperBound(6)!.Key.Is(5);
    }
}
