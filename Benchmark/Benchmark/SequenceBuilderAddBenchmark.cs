using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[MemoryDiagnoser]
public class SequenceBuilderAddBenchmark
{
    // [Params(32, 256, 1024, 16 * 1024)]
    [Params(1024)]
    public int Count { get; set; }

    [Benchmark(Baseline = true)]
    public long Current()
    {
        var builder = new SequenceBuilder<int>(256);

        for (var i = 0; i < this.Count; i++)
        {
            builder.Add(i);
        }

        var length = builder.Length;
        builder.Dispose();
        return length;
    }
}
