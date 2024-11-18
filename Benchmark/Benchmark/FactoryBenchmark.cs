using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class FactoryBenchmark
{
    private readonly OrderedMap<int, int> map = new();
    private readonly SortedDictionary<int, int> dictionary = new();

    public FactoryBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public int TestOrderedMap()
    {
        this.map.Add(1, 1);
        this.map.Add(2, 2);
        this.map.Add(3, 3);
        this.map.Remove(1);
        this.map.Remove(2);
        this.map.Remove(3);
        return this.map.Count;
    }

    [Benchmark]
    public int TestOrderedMap2()
    {
        if (!this.map.ContainsKey(1))
        {
            this.map.Add(1, 1);
        }

        if (!this.map.ContainsKey(2))
        {
            this.map.Add(2, 2);
        }

        if (!this.map.ContainsKey(3))
        {
            this.map.Add(3, 3);
        }

        this.map.Remove(1);
        this.map.Remove(2);
        this.map.Remove(3);
        return this.map.Count;
    }

    [Benchmark]
    public int TestOrderedMap3()
    {
        if (this.map.FindNode(1) is null)
        {
            this.map.Add(1, 1);
        }

        if (this.map.FindNode(2) is null)
        {
            this.map.Add(2, 2);
        }

        if (this.map.FindNode(3) is null)
        {
            this.map.Add(3, 3);
        }

        this.map.Remove(1);
        this.map.Remove(2);
        this.map.Remove(3);
        return this.map.Count;
    }

    [Benchmark]
    public int TestSortedDictionary()
    {
        this.dictionary.Add(1, 1);
        this.dictionary.Add(2, 2);
        this.dictionary.Add(3, 3);
        this.dictionary.Remove(1);
        this.dictionary.Remove(2);
        this.dictionary.Remove(3);
        return this.dictionary.Count;
    }
}
