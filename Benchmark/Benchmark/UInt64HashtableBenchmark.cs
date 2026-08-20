using System.Collections.Concurrent;
using System.Collections.Generic;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[Config(typeof(BenchmarkConfig))]
public class UInt64HashtableBenchmark
{
    private const int Count = 10_000;

    private UInt64Hashtable<int> hashtable = null!;
    private Dictionary<ulong, int> dictionary = null!;
    private ConcurrentDictionary<ulong, int> concurrentDictionary = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.hashtable = new UInt64Hashtable<int>(Count);
        this.dictionary = new Dictionary<ulong, int>(Count);
        this.concurrentDictionary = new ConcurrentDictionary<ulong, int>();

        for (var i = 0; i < Count; i++)
        {
            var key = (ulong)i;

            this.hashtable.Add(key, i);
            this.dictionary.Add(key, i);
            this.concurrentDictionary.TryAdd(key, i);
        }
    }

    [Benchmark]
    public UInt64Hashtable<int> UInt64Hashtable_Add()
    {
        var table = new UInt64Hashtable<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            table.Add((ulong)i, i);
        }

        return table;
    }

    //[Benchmark]
    public Dictionary<ulong, int> Dictionary_Add()
    {
        var table = new Dictionary<ulong, int>(Count);

        for (var i = 0; i < Count; i++)
        {
            table.Add((ulong)i, i);
        }

        return table;
    }

    //[Benchmark]
    public ConcurrentDictionary<ulong, int> ConcurrentDictionary_Add()
    {
        var table = new ConcurrentDictionary<ulong, int>();

        for (var i = 0; i < Count; i++)
        {
            table.TryAdd((ulong)i, i);
        }

        return table;
    }

    [Benchmark]
    public int UInt64Hashtable_Get()
    {
        var sum = 0;
        var table = this.hashtable;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue((ulong)i, out var value);
            sum += value;
        }

        return sum;
    }

    //[Benchmark]
    public int Dictionary_Get()
    {
        var sum = 0;
        var table = this.dictionary;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue((ulong)i, out var value);
            sum += value;
        }

        return sum;
    }

    //[Benchmark]
    public int ConcurrentDictionary_Get()
    {
        var sum = 0;
        var table = this.concurrentDictionary;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue((ulong)i, out var value);
            sum += value;
        }

        return sum;
    }

    /*[Benchmark]
    public bool UInt64Hashtable_GetLast()
        => this.hashtable.TryGetValue(Count - 1, out _);

    [Benchmark]
    public bool Dictionary_GetLast()
        => this.dictionary.TryGetValue(Count - 1, out _);

    [Benchmark]
    public bool ConcurrentDictionary_GetLast()
        => this.concurrentDictionary.TryGetValue(Count - 1, out _);

    [Benchmark]
    public int UInt64Hashtable_GetOrAddExisting()
        => this.hashtable.GetOrAdd(Count / 2, static key => (int)key);

    [Benchmark]
    public int ConcurrentDictionary_GetOrAddExisting()
        => this.concurrentDictionary.GetOrAdd(Count / 2, static key => (int)key);*/

    [Benchmark]
    public int UInt64Hashtable_ToArray()
        => this.hashtable.ToArray().Length;
}
