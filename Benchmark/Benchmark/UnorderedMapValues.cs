// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class UnorderedMapValues
{
    private const int N = 100;
    private readonly int[] array;
    private HashSet<int> hashset;
    private Dictionary<int, int> dictionary;
    private UnorderedMap<int, int> map;

    public UnorderedMapValues()
    {
        var xo = new Xoshiro256StarStar(1);
        this.array = new int[N];
        for (var i = 0; i < N; i++)
        {
            this.array[i] = (int)xo.NextUInt32();
        }

        this.hashset = new(this.array);
        this.dictionary = this.CreateDictionary();
        this.map = this.CreateUnorderedMap();
    }

    

    [GlobalSetup]
    public void Setup()
    {
    }

    // [Benchmark]
    public HashSet<int> CreateHashSet()
    {
        return new(this.array);
    }

    // [Benchmark]
    public Dictionary<int, int> CreateDictionary()
    {
        var dic = new Dictionary<int, int>();
        for (var i = 0; i < this.array.Length; i++)
        {
            dic.Add(this.array[i], this.array[i]);
        }

        return dic;
    }

    // [Benchmark]
    public UnorderedMap<int, int> CreateUnorderedMap()
    {
        var map = new UnorderedMap<int, int>();
        for (var i = 0; i < this.array.Length; i++)
        {
            map.Add(this.array[i], this.array[i]);
        }

        return map;
    }

    // [Benchmark]
    public int[] HashSetToArray()
        => this.hashset.ToArray();

    public int[] DictionaryToArray()
        => this.dictionary.Values.ToArray();

    [Benchmark]
    public int[] UnorderedMapToArray()
        => this.map.Values.ToArray();

    [Benchmark]
    public int[] UnorderedMapToArray2()
        => this.map.UnsafeValues.ToArray();
}
