using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class SortedSetBenchmark
{
    public const int Length = 1_00;

    private long[] array = default!;
    private SortedSet<long> sortedSet = new();
    private OrderedSet<long> orderedSet = new();
    private Queue<long> queue = new();
    private long x = 100;

    [GlobalSetup]
    public void Setup()
    {
        var r = new Random(12);
        this.array = new long[Length];
        for (var i = 0; i < Length; i++)
        {
            this.array[i] = r.NextInt64();
        }

        foreach (var x in this.array)
        {
            this.sortedSet.Add(x);
            this.orderedSet.Add(x);
            this.queue.Enqueue(x);
        }
    }

    [Benchmark]
    public bool AddRemove_SortedSet()
    {
        this.sortedSet.Add(x);
        var result = this.sortedSet.Remove(x);
        return result;
    }

    [Benchmark]
    public bool AddRemove_OrderedSet()
    {
        this.orderedSet.Add(x);
        var result = this.orderedSet.Remove(x);
        return result;
    }

    [Benchmark]
    public long Sum_SortedSet()
    {
        long sum = 0;
        foreach (var x in this.sortedSet)
        {
            sum += x;
        }

        return sum;
    }

    [Benchmark]
    public long Sum_OrderedSet()
    {
        long sum = 0;
        foreach (var x in this.orderedSet)
        {
            sum += x;
        }

        return sum;
    }

    [Benchmark]
    public long Sum_OrderedSet2()
    {
        long sum = 0;
        var node = this.orderedSet.First;
        while (node is not null)
        {
            sum += node.Key;
            node = node.Next;
        }

        return sum;
    }

    [Benchmark]
    public long Sum_Queue()
    {
        long sum = 0;
        foreach (var x in this.queue)
        {
            sum += x;
        }

        return sum;
    }
}
