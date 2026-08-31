// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
using Xunit;

namespace XunitTest.Arc;

public class StructTests
{
    [Fact]
    public void Struct128RoundTripsBytes()
    {
        byte[] source = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
        var value = new Struct128(source);
        Span<byte> destination = stackalloc byte[Struct128.Length];

        Assert.True(value.TryWriteBytes(destination));
        Assert.True(source.AsSpan().SequenceEqual(destination));
        Assert.False(value.TryWriteBytes(destination[..^1]));
    }

    [Fact]
    public void Struct256RoundTripsBytes()
    {
        byte[] source = [
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
        ];
        var value = new Struct256(source);
        Span<byte> destination = stackalloc byte[Struct256.Length];

        Assert.True(value.TryWriteBytes(destination));
        Assert.True(source.AsSpan().SequenceEqual(destination));
        Assert.False(value.TryWriteBytes(destination[..^1]));
    }

    [Fact]
    public void ThrowSizeMismatchUsesProvidedParameterName()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BaseHelper.ThrowSizeMismatchException("buffer", 16));
        Assert.Equal("buffer", exception.ParamName);
        Assert.Contains("buffer length must be 16 bytes", exception.Message, StringComparison.Ordinal);
    }
}
