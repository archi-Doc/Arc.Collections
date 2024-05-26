using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class TemplateBenchmark
{
    public TemplateBenchmark()
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
    public bool Test1()
    {
        return true;
    }
}
