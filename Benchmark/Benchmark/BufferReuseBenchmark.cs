// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class SequenceBuilderRangeBenchmark
{
    private int[] values = null!;

    [Params(256, 16384, 65536)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup() => this.values = new int[this.Count];

    [Benchmark]
    public long AddRange()
    {
        using var builder = new SequenceBuilder<int>();
        builder.AddRange(this.values);
        return builder.ToReadOnlySequence().Length;
    }
}

[Config(typeof(BenchmarkConfig))]
public class SlidingListClearBenchmark
{
    private SlidingList<object> list = null!;
    private readonly object value = new();

    [Params(1024, 65536)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup() => this.list = new SlidingList<object>(this.Capacity);

    [Benchmark]
    public void ClearSparseWindow()
    {
        this.list.Add(this.value);
        this.list.Clear();
    }
}
