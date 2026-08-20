using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public class Utf16HashtableBenchmark2
{
    private const int Count = 10_000;

    private Utf16Hashtable<int> utf16Hashtable = null!;
    private Dictionary<string, int> dictionary = null!;
    private ConcurrentDictionary<string, int> concurrentDictionary = null!;
    private string[] keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.keys = new string[Count];
        this.utf16Hashtable = new Utf16Hashtable<int>(Count);
        this.dictionary = new Dictionary<string, int>(Count);
        this.concurrentDictionary = new ConcurrentDictionary<string, int>();

        for (var i = 0; i < Count; i++)
        {
            var key = $"Key-{i}";
            this.keys[i] = key;

            this.utf16Hashtable.Add(key, i);
            this.dictionary.Add(key, i);
            this.concurrentDictionary.TryAdd(key, i);
        }
    }

    [Benchmark]
    public Utf16Hashtable<int> Utf16Hashtable_Add()
    {
        var table = new Utf16Hashtable<int>(Count);
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.Add(keys[i], i);
        }

        return table;
    }

    [Benchmark]
    public Dictionary<string, int> Dictionary_Add()
    {
        var table = new Dictionary<string, int>(Count);
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.Add(keys[i], i);
        }

        return table;
    }

    [Benchmark]
    public ConcurrentDictionary<string, int> ConcurrentDictionary_Add()
    {
        var table = new ConcurrentDictionary<string, int>();
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.TryAdd(keys[i], i);
        }

        return table;
    }

    [Benchmark]
    public int Utf16Hashtable_Get()
    {
        var sum = 0;
        var table = this.utf16Hashtable;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue(keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Dictionary_Get()
    {
        var sum = 0;
        var table = this.dictionary;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue(keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int ConcurrentDictionary_Get()
    {
        var sum = 0;
        var table = this.concurrentDictionary;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue(keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Utf16Hashtable_GetSpan()
    {
        var sum = 0;
        var table = this.utf16Hashtable;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            table.TryGetValue(keys[i].AsSpan(), out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public bool Utf16Hashtable_GetLast()
        => this.utf16Hashtable.TryGetValue(this.keys[^1], out _);

    [Benchmark]
    public bool Dictionary_GetLast()
        => this.dictionary.TryGetValue(this.keys[^1], out _);

    [Benchmark]
    public bool ConcurrentDictionary_GetLast()
        => this.concurrentDictionary.TryGetValue(this.keys[^1], out _);

    [Benchmark]
    public int Utf16Hashtable_GetOrAddExisting()
        => this.utf16Hashtable.GetOrAdd(this.keys[Count / 2], static _ => -1);

    [Benchmark]
    public int ConcurrentDictionary_GetOrAddExisting()
        => this.concurrentDictionary.GetOrAdd(this.keys[Count / 2], static _ => -1);

    [Benchmark]
    public int Utf16Hashtable_ToArray()
        => this.utf16Hashtable.ToArray().Length;

    [Benchmark]
    public int Utf16Hashtable_ToKeyValuePairs()
        => this.utf16Hashtable.ToKeyValuePairs().Length;
}
