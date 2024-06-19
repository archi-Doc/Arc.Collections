using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class OrderedMapSetKeyBenchmark
{
    private readonly OrderedMap<int, int> map = new();
    private readonly OrderedMap<int, int>.Node node5;
    private readonly OrderedMap<int, int>.Node node15;

    public OrderedMapSetKeyBenchmark()
    {
        map.Add(1, 1);
        map.Add(10, 10);
        (this.node5, _) = map.Add(5, 5);
        map.Add(-10, -10);
        map.Add(7, 7);
        (this.node15, _) = map.Add(15, 15);
        // map.SetNodeKey(this.node15, this.node15.Key + 1);
        // map.SetNodeKey(this.node15, 0);
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
    public bool SetNodeKey()
    {
        map.SetNodeKey(this.node5, this.node5.Key + 1);
        return map.SetNodeKey(this.node5, this.node5.Key - 1);
    }

    /*[Benchmark]
    public bool SetNodeKey2()
    {
        map.SetNodeKey2(this.node5, this.node5.Key + 1);
        return map.SetNodeKey2(this.node5, this.node5.Key - 1);
    }*/
}
