// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Arc;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ByteSumBenchmark
{
    private byte[] data = [];

    // [Params(0, 1, 15, 16, 31, 32, 33, 64, 256, 1024, 65_536)]
    [Params(1, 16, 64, 256, 1024, 65_536)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        this.data = GC.AllocateUninitializedArray<byte>(this.Length);
        new Random(42).NextBytes(this.data);
    }

    [Benchmark(Baseline = true)]
    public ulong Sum()
        => BaseHelper.Sum(this.data);

    [Benchmark]
    public ulong SumObs()
        => SumObs(this.data);

    [Benchmark]
    public ulong ScalarLoop()
    {
        ulong sum = 0;
        foreach (var value in this.data)
        {
            sum += value;
        }

        return sum;
    }

    /// <summary>
    /// Computes the sum of all elements in a span of unsigned bytes using SIMD acceleration when available.
    /// </summary>
    /// <param name="data">The read-only span of unsigned bytes to sum.</param>
    /// <returns>The sum of all elements in the span as a 32-bit unsigned integer.</returns>
    public static ulong SumObs(ReadOnlySpan<byte> data)
    {
        ulong acc = 0;
        ref byte p = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Avx2.IsSupported)
        {
            var accumulator = Vector256<ulong>.Zero;
            for (; i + 32 <= len; i += 32)
            {
                var v256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref p, i));
                var sad = Avx2.SumAbsoluteDifferences(v256, Vector256<byte>.Zero).AsUInt64();
                accumulator = Avx2.Add(accumulator, sad);
            }

            acc += accumulator.GetElement(0) + accumulator.GetElement(1) + accumulator.GetElement(2) + accumulator.GetElement(3);
        }

        if (Sse2.IsSupported)
        {
            var accumulator = Vector128<ulong>.Zero;
            for (; i + 16 <= len; i += 16)
            {
                var v128 = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref p, i));
                var sad = Sse2.SumAbsoluteDifferences(v128, Vector128<byte>.Zero).AsUInt64();
                accumulator = Sse2.Add(accumulator, sad);
            }

            acc += accumulator.GetElement(0) + accumulator.GetElement(1);
        }

        for (; i < len; i++)
        {
            acc += data[i];
        }

        return acc;
    }
}
