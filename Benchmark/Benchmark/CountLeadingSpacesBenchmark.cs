// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Arc;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CountLeadingSpacesBenchmark
{
    private string text = string.Empty;

    [Params(0, 1, 7, 8, 15, 16, 31, 32, 64, 256, 1024)]
    public int LeadingSpaceCount { get; set; }

    [Params(false, true)]
    public bool HasTerminatingCharacter { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var suffix = this.HasTerminatingCharacter ? "x" : string.Empty;
        this.text = string.Concat(new string(BaseHelper.SpaceChar, this.LeadingSpaceCount), suffix);
    }

    [Benchmark(Baseline = true)]
    public int CountLeadingSpaces()
        => BaseHelper.CountLeadingSpaces(this.text);

    [Benchmark]
    public int CountLeadingSpacesObs()
        => CountLeadingSpacesObs(this.text);

    [Benchmark]
    public int ScalarLoop()
    {
        var span = this.text.AsSpan();
        var i = 0;
        while (i < span.Length && span[i] == BaseHelper.SpaceChar)
        {
            i++;
        }

        return i;
    }

    private static unsafe int CountLeadingSpacesObs(ReadOnlySpan<char> span)
    {
        var length = span.Length;
        if (length == 0 || span[0] != BaseHelper.SpaceChar)
        {
            return 0;
        }

        var i = 0;
        if (length < 8)
        {
            i = 1;
            while (i < length && span[i] == BaseHelper.SpaceChar)
            {
                i++;
            }

            return i;
        }

        fixed (char* p = span)
        {
            if (Avx2.IsSupported)
            {
                Vector256<ushort> spaces = Vector256.Create((ushort)BaseHelper.SpaceChar);
                while (i <= length - 16)
                {
                    Vector256<ushort> chunk = Avx.LoadVector256((ushort*)(p + i));
                    Vector256<ushort> eq = Avx2.CompareEqual(chunk, spaces);
                    uint nonSpaceMask = ~(uint)Avx2.MoveMask(eq.AsByte()) & 0x55555555u;
                    if (nonSpaceMask != 0)
                    {
                        return i + (BitOperations.TrailingZeroCount(nonSpaceMask) >> 1);
                    }

                    i += 16;
                }
            }

            if (Sse2.IsSupported)
            {
                Vector128<ushort> spaces = Vector128.Create((ushort)BaseHelper.SpaceChar);

                while (i <= length - 8)
                {
                    Vector128<ushort> chunk = Sse2.LoadVector128((ushort*)(p + i));
                    Vector128<ushort> eq = Sse2.CompareEqual(chunk, spaces);
                    uint nonSpaceMask = ~(uint)Sse2.MoveMask(eq.AsByte()) & 0x5555u;
                    if (nonSpaceMask != 0)
                    {
                        return i + (BitOperations.TrailingZeroCount(nonSpaceMask) >> 1);
                    }

                    i += 8;
                }
            }

            while (i < length && p[i] == BaseHelper.SpaceChar)
            {
                i++;
            }
        }

        return i;
    }
}
