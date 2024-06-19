using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ReverseOrderTest
{
    [Params(1000)]
    public int Length;

    public int[] IntArray = default!;

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

    public ReverseOrderTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.IntArray = GetUniqRandomNumbers(-Length, +Length, Length).ToArray();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public int NewAndAdd_SortedSet()
    {
        var ss = new System.Collections.Generic.SortedSet<int>();
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
        foreach (var x in this.IntArray)
        {
            ss.Add(x);
        }

        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd_OrderedSetRev()
    {
        var ss = new OrderedSet<int>(true);
        foreach (var x in this.IntArray)
        {
            ss.Add(x);
        }

        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd2_SortedSet()
    {
        var ss = new System.Collections.Generic.SortedSet<OrderedSetClass>();
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
        foreach (var x in this.IntArray)
        {
            ss.Add(new OrderedSetClass(x));
        }
        return ss.Count;
    }

    [Benchmark]
    public int NewAndAdd2_OrderedSetRev()
    {
        var ss = new OrderedSet<OrderedSetClass>(true);
        foreach (var x in this.IntArray)
        {
            ss.Add(new OrderedSetClass(x));
        }
        return ss.Count;
    }
}
