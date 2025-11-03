// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using Arc;
using Xunit;

namespace xUnitTest;

public class SumTest
{
    [Fact]
    public void TestSbyte()
    {
        var data = new sbyte[256];
        var rand = new Random(1234);
        rand.NextBytes(MemoryMarshal.AsBytes(data.AsSpan()));

        for (var i = 0; i < data.Length; i++)
        {
            var span = data.AsSpan(0, i);
            BaseHelper.Sum(span).Is(this.Sum(span));
        }
    }

    [Fact]
    public void TestByte()
    {
        var data = new byte[256];
        var rand = new Random(1234);
        rand.NextBytes(data.AsSpan());

        for (var i = 0; i < data.Length; i++)
        {
            var span = data.AsSpan(0, i);
            BaseHelper.Sum(span).Is(this.Sum(span));
        }
    }

    private int Sum(ReadOnlySpan<sbyte> data)
    {
        var span = data;
        int sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        return sum;
    }

    private uint Sum(ReadOnlySpan<byte> data)
    {
        var span = data;
        uint sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        return sum;
    }
}
