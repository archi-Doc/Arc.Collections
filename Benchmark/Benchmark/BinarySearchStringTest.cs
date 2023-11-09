using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class BinarySearchStringTest
{
    [Params(10, 100, 10_000)]
    public int Length;

    public string[] StringArray = default!;

    public string[] SortedArray = default!;

    public string Value = default!;

    public OrderedList<string> OrderedList = default!;

    public BinarySearchStringTest()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        this.StringArray = new string[this.Length];
        var r = new Random();
        for (var n = 0; n < this.Length; n++)
        {
            this.StringArray[n] = r.Next(this.Length).ToString();
        }

        this.Value = this.StringArray[this.Length / 2];
        this.OrderedList = new(this.StringArray);
        Array.Sort(this.StringArray);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public int Array_BinarySearch()
    {
        return Array.BinarySearch(this.StringArray, this.Value);
    }

    [Benchmark]
    public int OrderedList_BinarySearch()
    {
        return this.OrderedList.BinarySearch(this.Value);
    }
}
