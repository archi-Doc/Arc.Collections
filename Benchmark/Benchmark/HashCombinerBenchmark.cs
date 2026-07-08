using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class HashCombinerBenchmark
{
    private ulong hash0 = 0x938C3585E8EF9A4D;
    private ulong hash1 = 0x91E645B3297BED48;

    public HashCombinerBenchmark()
    {
    }

    [Benchmark]
    public ulong HashCodeCombine()
    {
        return (ulong)HashCode.Combine(this.hash0++, this.hash1++);
    }

    [Benchmark]
    public ulong XxHash3()
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;
        BitConverter.TryWriteBytes(span, this.hash0++);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.hash1++);

        return XxHash3Slim.Hash64(buffer);
    }

    [Benchmark]
    public ulong CombineRotateMultiply()
    {
        return XxHash3Slim.CombineRotateMultiply(this.hash0++, this.hash1++);
    }

    [Benchmark]
    public ulong CombineMultiplyFold()
    {
        return XxHash3Slim.Combine(this.hash0++, this.hash1++);
    }
}
