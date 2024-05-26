using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class Int128Benchmark
{
    private OrderedMap<Int128, int> map = new();

    public Int128Benchmark()
    {
        this.map.Add(1, 1);
        this.map.Add(10, 10);
        this.map.Add(3, 3);
        this.map.Add(-100, -100);
        this.map.Add(100, 100);
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
    public int Test1()
    {
        var total = 0;
        this.map.TryGetValue(1, out var i);
        total += i;
        this.map.TryGetValue(10, out i);
        total += i;
        this.map.TryGetValue(3, out i);
        total += i;
        this.map.TryGetValue(-100, out i);
        total += i;
        this.map.TryGetValue(100, out i);
        total += i;

        return total;
    }
}
