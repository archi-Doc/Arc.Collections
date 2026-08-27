using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Arc;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class SpanownerBenchmark
{
    private int length = 500;
    public SpanownerBenchmark()
    {
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
    public byte Classic()
    {
        byte[]? pooledName = null;
        scoped Span<byte> buffer = length <= BaseHelper.StackallocThreshold ?
            stackalloc byte[length] :
            (pooledName = ArrayPool<byte>.Shared.Rent(length)).AsSpan(0, length);

        var b = buffer[0];

        // use buffer
        if (pooledName is not null)
        {
            ArrayPool<byte>.Shared.Return(pooledName);
        }

        return b;
    }

    [Benchmark]
    public byte SpanOwner()
    {
        using var owner = new SpanOwner<byte>(stackalloc byte[BaseHelper.StackallocThreshold], this.length);
        Span<byte> buffer = owner.Span;
        return buffer[0];
    }

    [Benchmark]
    [SkipLocalsInit]
    public byte SpanOwner2()
    {
        using var owner = new SpanOwner<byte>(stackalloc byte[BaseHelper.StackallocThreshold], this.length);
        Span<byte> buffer = owner.Span;
        return buffer[0];
    }
}
