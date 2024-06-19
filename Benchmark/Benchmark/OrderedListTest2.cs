using System;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Arc.Collections;

namespace Benchmark;

public class OrderedListClass2
{
    public OrderedListClass2(int id)
    {
        this.Id = id;
    }

    public int Id { get; set; }

    public override string ToString() => $"{this.Id}";

    public class InternalComparer : IComparer<OrderedListClass2>
    {
        public static InternalComparer Instance = new();

        public int Compare(OrderedListClass2? x, OrderedListClass2? y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
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

}
[Config(typeof(BenchmarkConfig))]
public class OrderedListTest2
{
    [Params(10, 100, 10_000)]
    public int Length;

    public int[] IntArray = default!;

    public OrderedListTest2()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        var r = new Random(12);
        this.IntArray = BenchmarkHelper.GetUniqueRandomNumbers(r, -this.Length, this.Length, this.Length).ToArray();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public int Add_OrderedList()
    {
        var list = new OrderedList<OrderedListClass2>(OrderedListClass2.InternalComparer.Instance);
        foreach (var x in this.IntArray)
        {
            list.Add(new OrderedListClass2(x));
        }

        return list.Count;
    }

    [Benchmark]
    public int Add_OrderedKeyValueList()
    {
        var list = new OrderedKeyValueList<int, OrderedListClass2>();
        foreach (var x in this.IntArray)
        {
            list.Add(x, new OrderedListClass2(x));
        }

        return list.Count;
    }

    [Benchmark]
    public int Add_SortedList()
    {
        var list = new SortedList<int, OrderedListClass2>();
        foreach (var x in this.IntArray)
        {
            list.Add(x, new OrderedListClass2(x));
        }

        return list.Count;
    }
}
