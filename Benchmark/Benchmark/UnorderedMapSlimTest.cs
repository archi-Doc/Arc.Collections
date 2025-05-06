using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class UnorderedMapSlimTest
{
    [Params(1000)]
    public int Count;

    public int TargetInt = 90;

    public int[] IntArray = default!;
    public Dictionary<int, int> IntDictionary = default!;
    public UnorderedMap<int, int> IntUnorderedMap = default!;

    public UnorderedMapSlimTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        var r = new Random(12);
        this.IntArray = BenchmarkHelper.GetRandomNumbers(r, 0, this.Count, this.Count).ToArray();

        this.IntDictionary = new();
        var om = new OrderedMap<int, int>();
        var total = 0;
        foreach (var x in this.IntArray)
        {
            this.IntDictionary.TryAdd(x, x * 2);
            this.IntUnorderedMap.Add(x, x * 2);

            var result = om.Add(x, x * 2);
            if (result.NewlyAdded)
            {
                total += result.Node.Value;
            }
        }
    }

    [Benchmark]
    public int AddSerialInt_Dictionary()
    {
        var c = new Dictionary<int, int>();
        for (var n = 0; n < this.Count; n++)
        {
            c.Add(n, n);
        }

        return c.Count;
    }

    [Benchmark]
    public int AddSerialInt_UnorderedMap()
    {
        var c = new UnorderedMap<int, int>();
        for (var n = 0; n < this.Count; n++)
        {
            c.Add(n, n);
        }

        return c.Count;
    }

    /*[Benchmark]
    public int AddSerialInt_UnorderedMapSlim()
    {
        var c = new UnorderedMapSlim<int, int>();
        for (var n = 0; n < this.Count; n++)
        {
            c.Add(n, n);
        }

        return c.Count;
    }*/

    [Benchmark]
    public int AddRandomInt_Dictionary()
    {
        var c = new Dictionary<int, int>();
        for (var n = 0; n < this.Count; n++)
        {
            c.TryAdd(this.IntArray[n], this.IntArray[n]);
        }

        return c.Count;
    }

    [Benchmark]
    public int AddRandomInt_UnorderedMap()
    {
        var c = new UnorderedMap<int, int>();
        for (var n = 0; n < this.Count; n++)
        {
            c.Add(this.IntArray[n], this.IntArray[n]);
        }

        return c.Count;
    }

    [Benchmark]
    public int GetRandomInt_Dictionary()
    {
        var total = 0;
        for (var n = 0; n < this.Count; n++)
        {
            if (this.IntDictionary.TryGetValue(n, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark]
    public int GetRandomInt_UnorderedMap()
    {
        var total = 0;
        for (var n = 0; n < this.Count; n++)
        {
            if (this.IntUnorderedMap.TryGetValue(n, out var value))
            {
                total += value;
            }
        }

        return total;
    }
}
