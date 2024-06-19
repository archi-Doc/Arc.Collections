using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;

namespace Benchmark;

public class OrderedSetClass : IComparable<OrderedSetClass>
{
    public OrderedSetClass(int id)
    {
        this.Id = id;
    }

    public int Id { get; set; }

    public int CompareTo(OrderedSetClass? other)
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
public class OrderedSetTest
{
    [Params(100, 10_000)]
    // [Params(10_000)]
    public int Length;

    public int[] IntArray = default!;

    public System.Collections.Generic.SortedSet<int> IntSetRef = new();

    public OrderedMap<int, int> IntSet = new();

    public OrderedMap<int, int>.Node Node0 = default!;
    public OrderedMap<int, int>.Node Node7 = default!;
    public OrderedMap<int, int>.Node Node11 = default!;
    public OrderedMap<int, int>.Node Node55 = default!;

    static System.Collections.Generic.IEnumerable<int> GetUniqRandomNumbers(int rangeBegin, int rangeEnd, int count)
    {
        var work = new int[rangeEnd - rangeBegin + 1];
        for (int n = rangeBegin, i = 0; n <= rangeEnd; n++, i++)
        {
            work[i] = n;
        }

        var rnd = new Random(1286);
        for (int resultPos = 0; resultPos < count; resultPos++)
        {
            int nextResultPos = rnd.Next(resultPos, work.Length);
            (work[resultPos], work[nextResultPos]) = (work[nextResultPos], work[resultPos]);
        }

        return work.Take(count);
    }

    public OrderedSetTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.IntArray = GetUniqRandomNumbers(-Length, +Length, Length).ToArray();

        foreach (var x in this.IntArray)
        {
            this.IntSetRef.Add(x);
        }

        foreach (var x in this.IntArray)
        {
            this.IntSet.Add(x, x);
            // Debug.Assert(this.IntSet.Validate());
        }

        (this.Node0, _) = this.IntSet.Add(0, 0);
        (this.Node7, _) = this.IntSet.Add(7, 7);
        (this.Node11, _) = this.IntSet.Add(11, 11);
        (this.Node55, _) = this.IntSet.Add(55, 55);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

   [Benchmark]
    public int NewAndAdd_SortedSet()
    {
        var ss = new System.Collections.Generic.SortedSet<int>();
        ss.Add(1);
        ss.Add(10);
        ss.Add(4);
        ss.Add(34);
        ss.Add(-4);
        ss.Add(43);
        ss.Add(5);
        ss.Add(0);
        ss.Add(9);
        ss.Add(20);
        foreach (var x in this.IntArray)
        {
            ss.Add(x);
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd_OrderedSet()
    {
        var ss = new OrderedSet<int>();
        ss.Add(1);
        ss.Add(10);
        ss.Add(4);
        ss.Add(34);
        ss.Add(-4);
        ss.Add(43);
        ss.Add(5);
        ss.Add(0);
        ss.Add(9);
        ss.Add(20);
        foreach (var x in this.IntArray)
        {
            ss.Add(x);
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd_OrderedMap()
    {
        var ss = new OrderedMap<int, int>();
        ss.Add(1, 0);
        ss.Add(10, 0);
        ss.Add(4, 0);
        ss.Add(34, 0);
        ss.Add(-4, 0);
        ss.Add(43, 0);
        ss.Add(5, 0);
        ss.Add(0, 0);
        ss.Add(9, 0);
        ss.Add(20, 0);
        foreach (var x in this.IntArray)
        {
            ss.Add(x, 0);
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd2_SortedSet()
    {
        var ss = new System.Collections.Generic.SortedSet<OrderedSetClass>();
        ss.Add(new OrderedSetClass(1));
        ss.Add(new OrderedSetClass(10));
        ss.Add(new OrderedSetClass(4));
        ss.Add(new OrderedSetClass(34));
        ss.Add(new OrderedSetClass(-4));
        ss.Add(new OrderedSetClass(43));
        ss.Add(new OrderedSetClass(5));
        ss.Add(new OrderedSetClass(0));
        ss.Add(new OrderedSetClass(9));
        ss.Add(new OrderedSetClass(20));
        foreach (var x in this.IntArray)
        {
            ss.Add(new OrderedSetClass(x));
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd2_OrderedSet()
    {
        var ss = new OrderedSet<OrderedSetClass>();
        ss.Add(new OrderedSetClass(1));
        ss.Add(new OrderedSetClass(10));
        ss.Add(new OrderedSetClass(4));
        ss.Add(new OrderedSetClass(34));
        ss.Add(new OrderedSetClass(-4));
        ss.Add(new OrderedSetClass(43));
        ss.Add(new OrderedSetClass(5));
        ss.Add(new OrderedSetClass(0));
        ss.Add(new OrderedSetClass(9));
        ss.Add(new OrderedSetClass(20));
        foreach (var x in this.IntArray)
        {
            ss.Add(new OrderedSetClass(x));
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd2_OrderedMap()
    {
        var ss = new OrderedMap<OrderedSetClass, int>();
        ss.Add(new OrderedSetClass(1), 0);
        ss.Add(new OrderedSetClass(10), 0);
        ss.Add(new OrderedSetClass(4), 0);
        ss.Add(new OrderedSetClass(34), 0);
        ss.Add(new OrderedSetClass(-4), 0);
        ss.Add(new OrderedSetClass(43), 0);
        ss.Add(new OrderedSetClass(5), 0);
        ss.Add(new OrderedSetClass(0), 0);
        ss.Add(new OrderedSetClass(9), 0);
        ss.Add(new OrderedSetClass(20), 0);
        foreach (var x in this.IntArray)
        {
            ss.Add(new OrderedSetClass(x), 0);
        }
        return ss.Count;
    }

    [Benchmark]
    public int AddRemove_SortedSet()
    {
        this.IntSetRef.Remove(0);
        this.IntSetRef.Remove(7);
        this.IntSetRef.Remove(11);
        this.IntSetRef.Remove(55);

        this.IntSetRef.Add(0);
        this.IntSetRef.Add(7);
        this.IntSetRef.Add(11);
        this.IntSetRef.Add(55);

        return this.IntSetRef.Count;
    }

    [Benchmark]
    public int AddRemove_OrderedSet()
    {
        this.IntSet.Remove(0);
        this.IntSet.Remove(7);
        this.IntSet.Remove(11);
        this.IntSet.Remove(55);

        this.IntSet.Add(0, 0);
        this.IntSet.Add(7, 7);
        this.IntSet.Add(11, 11);
        this.IntSet.Add(55, 55);

        return this.IntSet.Count;
    }

    [Benchmark]
    public int AddRemoveNode_OrderedSet()
    {
        this.IntSet.RemoveNode(this.Node0);
        this.IntSet.RemoveNode(this.Node7);
        this.IntSet.RemoveNode(this.Node11);
        this.IntSet.RemoveNode(this.Node55);

        (this.Node0, _) = this.IntSet.Add(0, 0);
        (this.Node7, _) = this.IntSet.Add(7, 7);
        (this.Node11, _) = this.IntSet.Add(11, 11);
        (this.Node55, _) = this.IntSet.Add(55, 55);

        return this.IntSet.Count;
    }

    [Benchmark]
    public int AddRemoveReuse_OrderedSet()
    {
        this.IntSet.RemoveNode(this.Node0);
        this.IntSet.RemoveNode(this.Node7);
        this.IntSet.RemoveNode(this.Node11);
        this.IntSet.RemoveNode(this.Node55);

        (this.Node0, _) = this.IntSet.Add(0, 0, this.Node0);
        (this.Node7, _) = this.IntSet.Add(7, 7, this.Node7);
        (this.Node11, _) = this.IntSet.Add(11, 11, this.Node11);
        (this.Node55, _) = this.IntSet.Add(55, 55, this.Node55);

        return this.IntSet.Count;
    }

    [Benchmark]
    public int AddRemoveReplace_OrderedSet()
    {
        this.IntSet.SetNodeKey(this.Node0, 0);
        this.IntSet.SetNodeKey(this.Node7, 7);
        this.IntSet.SetNodeKey(this.Node11, 11);
        this.IntSet.SetNodeKey(this.Node55, 55);

        return this.IntSet.Count;
    }

    [Benchmark]
    public int Enumerate_SortedSet()
    {
        var total = 0;
        foreach (var x in this.IntSetRef)
        {
            total += x;
        }

        return total;
    }

    [Benchmark]
    public int Enumerate_OrderedSet()
    {
        var total = 0;
        foreach (var x in this.IntSet.Keys)
        {
            total += x;
        }

        return total;
    }
}
