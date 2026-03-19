using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark;

public class TemporaryListClass
{
    public int X { get; set; }
}

[Config(typeof(BenchmarkConfig))]
public class TemporaryListBenchmark
{
    private readonly TemporaryListClass c0 = new();
    private readonly TemporaryListClass c1 = new();
    private readonly TemporaryListClass c2 = new();

    public TemporaryListBenchmark()
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
    public int GenericList()
    {
        var list = new List<TemporaryListClass>();
        list.Add(this.c0);
        list.Add(this.c1);
        list.Add(this.c2);

        var sum = 0;
        foreach (var x in list)
        {
            sum += x.X;
        }

        return sum;
    }

    [Benchmark]
    public int TemporaryList()
    {
        var list = new TemporaryList<TemporaryListClass>();
        list.Add(this.c0);
        list.Add(this.c1);
        list.Add(this.c2);

        var sum = 0;
        foreach (var x in list)
        {
            sum += x.X;
        }

        return sum;
    }

    [Benchmark]
    public int TemporaryList2()
    {
        var list = new TemporaryList2<TemporaryListClass>();
        list.Add(this.c0);
        list.Add(this.c1);
        list.Add(this.c2);

        var sum = 0;
        foreach (var x in list)
        {
            sum += x.X;
        }

        return sum;
    }
}
