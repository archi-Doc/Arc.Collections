using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

public class OrderedListClassComparer : IComparer<OrderedListClass>
{
    public int Compare(OrderedListClass? x, OrderedListClass? y)
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

public class OrderedListClass : IComparable<OrderedListClass>
{
    public OrderedListClass(int id)
    {
        this.Id = id;
    }

    public int Id { get; set; }

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

    public override string ToString() => this.Id.ToString();
}

[Config(typeof(BenchmarkConfig))]
public class IComparerTest
{
    [Params(10, 100, 10_000)]
    public int Length;

    public int[] IntArray = default!;

    public OrderedListClass[] ClassArray = default!;

    public int Value;

    public OrderedList<OrderedListClass> OrderedListComparable = default!;

    public OrderedList<OrderedListClass> OrderedListComparer = default!;

    public OrderedListClass ValueClass = default!;

    public IComparerTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.IntArray = new int[this.Length];
        this.ClassArray = new OrderedListClass[this.Length];
        var r = new Random(1);
        for (var n = 0; n < this.Length; n++)
        {
            this.IntArray[n] = r.Next(this.Length);
            this.ClassArray[n] = new OrderedListClass(this.IntArray[n]);
        }

        this.Value = this.IntArray[this.Length / 2];
        this.ValueClass = new(this.Value);
        this.OrderedListComparable = new(this.ClassArray);
        this.OrderedListComparer = new(this.ClassArray, new OrderedListClassComparer());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public OrderedList<OrderedListClass> Add_Comparable()
    {
        var ol = new OrderedList<OrderedListClass>();
        foreach (var x in this.ClassArray)
        {
            ol.Add(x);
        }

        return ol;
    }

    [Benchmark]
    public OrderedList<OrderedListClass> Add_Comparer()
    {
        var ol = new OrderedList<OrderedListClass>(new OrderedListClassComparer());
        foreach (var x in this.ClassArray)
        {
            ol.Add(x);
        }

        return ol;
    }

    [Benchmark]
    public int Search_Comparable()
    {
        return this.OrderedListComparable.IndexOf(this.ValueClass);
    }

    [Benchmark]
    public int Search_Comparer()
    {
        return this.OrderedListComparer.IndexOf(this.ValueClass);
    }
}
