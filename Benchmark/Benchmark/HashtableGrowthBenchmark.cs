// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class HashtableGrowthBenchmark
{
    private byte[][] keys = null!;
    private UInt64Hashtable<int> emptyTable = null!;

    [Params(1_024, 16_384)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.keys = new byte[this.Count][];
        for (var i = 0; i < this.keys.Length; i++)
        {
            this.keys[i] = Encoding.UTF8.GetBytes("key-" + i);
        }

        this.emptyTable = new UInt64Hashtable<int>(this.Count);
    }

    [Benchmark]
    public UInt64Hashtable<int> GrowSequentialKeys()
    {
        var table = new UInt64Hashtable<int>();
        for (var i = 0; i < this.Count; i++)
        {
            table.TryAdd((ulong)i, i);
        }

        return table;
    }

    [Benchmark]
    public UInt64Hashtable<int> GrowCollidingKeys()
    {
        var table = new UInt64Hashtable<int>();
        for (var i = 0; i < this.Count; i++)
        {
            table.TryAdd((ulong)i * 256, i);
        }

        return table;
    }

    [Benchmark]
    public Utf8Hashtable<int> GrowUtf8Keys()
    {
        var table = new Utf8Hashtable<int>();
        for (var i = 0; i < this.Count; i++)
        {
            table.TryAdd(this.keys[i], i);
        }

        return table;
    }

    [Benchmark]
    public int[] EmptySnapshot() => this.emptyTable.ToArray();
}
