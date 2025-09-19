using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

public record ObjectCacheClass(int Id, string Name);

[Config(typeof(BenchmarkConfig))]
public class ObjectCacheBenchmark
{
    private const int N = 50;
    private KeyedObjectCache<int, ObjectCacheClass> cache = new(N);

    public ObjectCacheBenchmark()
    {
        for (var i = 0; i < (N / 2); i++)
        {
            this.cache.Cache(i, new(i, i.ToString()));
        }
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
    public KeyedObjectCache<int, ObjectCacheClass> Cache5()
    {
        var cache = new KeyedObjectCache<int, ObjectCacheClass>(10);
        for (var i = 0; i < 5; i++)
        {
            cache.Cache(i, new(i, i.ToString()));
        }

        return cache;
    }

    [Benchmark]
    public int TryGetAndCache()
    {
        var t = this.cache.TryGet(10);
        if (t != null)
        {
            this.cache.Cache(10, t);
        }

        return this.cache.Count;
    }
}
