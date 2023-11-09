using System;
using BenchmarkDotNet.Attributes;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Arc.Collections;
using Arc.Collections.Obsolete;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class UnorderedMapTest2
{
    [Params(1_000_000)]
    public int Length;

    public HashSet<int> HashSet { get; private set; } = default!;

    public UnorderedMapTest2()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.HashSet = new();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public HashSet<int> HashSet_Add()
    {
        var set = new HashSet<int>();
        for (var i = 0; i < Length; i++)
        {
            set.Add(i);
        }

        return set;
    }

    [Benchmark]
    public UnorderedMap<int, int> UnorderedMap_Add()
    {
        var map = new UnorderedMap<int, int>();
        for (var i = 0; i < Length; i++)
        {
            map.Add(i, i);
        }

        return map;
    }

    [Benchmark]
    public UnorderedMultiMap<int, int> UnorderedMultiMap_Add()
    {
        var map = new UnorderedMultiMap<int, int>();
        for (var i = 0; i < Length; i++)
        {
            map.Add(i, i);
        }

        return map;
    }
}
