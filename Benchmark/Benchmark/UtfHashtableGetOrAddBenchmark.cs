// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class UtfHashtableGetOrAddBenchmark
{
    private const int Count = 256;

    private string[] strings = null!;
    private byte[][] bytes = null!;
    private Utf16Hashtable<int> utf16 = null!;
    private Utf8Hashtable<int> utf8 = null!;

    [Params(16, 4096)]
    public int KeyLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.strings = new string[Count];
        this.bytes = new byte[Count][];
        this.utf16 = new Utf16Hashtable<int>(Count);
        this.utf8 = new Utf8Hashtable<int>(Count);
        for (var i = 0; i < Count; i++)
        {
            this.strings[i] = i.ToString("D8") + new string('k', this.KeyLength - 8);
            this.bytes[i] = Encoding.UTF8.GetBytes(this.strings[i]);
            this.utf16.TryAdd(this.strings[i], i);
            this.utf8.TryAdd(this.bytes[i], i);
        }
    }

    [Benchmark]
    public Utf16Hashtable<int> Utf16StringMiss()
    {
        var table = new Utf16Hashtable<int>(Count);
        foreach (var key in this.strings)
        {
            table.GetOrAdd(key, static _ => 1);
        }

        return table;
    }

    [Benchmark]
    public Utf16Hashtable<int> Utf16SpanMiss()
    {
        var table = new Utf16Hashtable<int>(Count);
        foreach (var key in this.strings)
        {
            table.GetOrAdd(key.AsSpan(), static _ => 1);
        }

        return table;
    }

    [Benchmark]
    public Utf8Hashtable<int> Utf8ArrayMiss()
    {
        var table = new Utf8Hashtable<int>(Count);
        foreach (var key in this.bytes)
        {
            table.GetOrAdd(key, static _ => 1);
        }

        return table;
    }

    [Benchmark]
    public Utf8Hashtable<int> Utf8SpanMiss()
    {
        var table = new Utf8Hashtable<int>(Count);
        foreach (var key in this.bytes)
        {
            table.GetOrAdd(key.AsSpan(), static _ => 1);
        }

        return table;
    }

    [Benchmark]
    public int Utf16StringHit() => this.utf16.GetOrAdd(this.strings[0], static _ => -1);

    [Benchmark]
    public int Utf16SpanHit() => this.utf16.GetOrAdd(this.strings[0].AsSpan(), static _ => -1);

    [Benchmark]
    public int Utf8ArrayHit() => this.utf8.GetOrAdd(this.bytes[0], static _ => -1);

    [Benchmark]
    public int Utf8SpanHit() => this.utf8.GetOrAdd(this.bytes[0].AsSpan(), static _ => -1);
}
