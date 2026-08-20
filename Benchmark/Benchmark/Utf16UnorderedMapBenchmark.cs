using System;
using System.Collections.Generic;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[Config(typeof(BenchmarkConfig))]
public class Utf16UnorderedMapBenchmark
{
    private const int Count = 10_000;

    private Utf16UnorderedMap<int> unorderedMap = null!;
    private Dictionary<string, int> dictionary = null!;
    private string[] keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.keys = new string[Count];
        this.unorderedMap = new Utf16UnorderedMap<int>(Count);
        this.dictionary = new Dictionary<string, int>(Count);

        for (var i = 0; i < Count; i++)
        {
            var key = $"Key-{i}";
            this.keys[i] = key;

            this.unorderedMap.Add(key, i);
            this.dictionary.Add(key, i);
        }
    }

    [Benchmark]
    public Utf16UnorderedMap<int> UnorderedMap_Add()
    {
        var map = new Utf16UnorderedMap<int>(Count);
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            map.Add(keys[i], i);
        }

        return map;
    }

    [Benchmark]
    public Dictionary<string, int> Dictionary_Add()
    {
        var map = new Dictionary<string, int>(Count);
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            map.Add(keys[i], i);
        }

        return map;
    }

    [Benchmark]
    public int UnorderedMap_Get()
    {
        var sum = 0;
        var map = this.unorderedMap;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            map.TryGetValue(keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int UnorderedMap_GetSpan()
    {
        var sum = 0;
        var map = this.unorderedMap;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            map.TryGetValue(keys[i].AsSpan(), out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Dictionary_Get()
    {
        var sum = 0;
        var map = this.dictionary;
        var keys = this.keys;

        for (var i = 0; i < Count; i++)
        {
            map.TryGetValue(keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public bool UnorderedMap_GetLast()
        => this.unorderedMap.TryGetValue(this.keys[^1], out _);

    [Benchmark]
    public bool UnorderedMap_GetLastSpan()
        => this.unorderedMap.TryGetValue(this.keys[^1].AsSpan(), out _);

    [Benchmark]
    public bool Dictionary_GetLast()
        => this.dictionary.TryGetValue(this.keys[^1], out _);

    [Benchmark]
    public bool UnorderedMap_ContainsKey()
        => this.unorderedMap.ContainsKey(this.keys[^1]);

    [Benchmark]
    public bool Dictionary_ContainsKey()
        => this.dictionary.ContainsKey(this.keys[^1]);

    [Benchmark]
    public int UnorderedMap_Enumerate()
    {
        var sum = 0;

        foreach (var pair in this.unorderedMap)
        {
            sum += pair.Value;
        }

        return sum;
    }

    [Benchmark]
    public int Dictionary_Enumerate()
    {
        var sum = 0;

        foreach (var pair in this.dictionary)
        {
            sum += pair.Value;
        }

        return sum;
    }
}
