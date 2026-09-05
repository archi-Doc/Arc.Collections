using System;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class HashCoverageTest
{
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void XxHash3MatchesStandardImplementationAtEveryLengthBoundary(long seed)
    {
        var data = new byte[65540];
        new Random(402).NextBytes(data);
        int[] lengths = [0, 1, 2, 3, 4, 7, 8, 9, 15, 16, 17, 31, 32, 33, 63, 64, 65,
            95, 96, 97, 127, 128, 129, 159, 160, 191, 192, 239, 240, 241, 255, 256,
            511, 512, 1023, 1024, 1025, 2048, 4096, 32768, 65536];
        foreach (var length in lengths)
        {
            for (var offset = 0; offset < 4; offset++)
            {
                var source = data.AsSpan(offset, length);
                Assert.Equal(XxHash3.HashToUInt64(source, seed), XxHash3Slim.Hash64(source, seed));
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("日本語😀\0test")]
    public void Utf16HashMatchesRawCharacterBytes(string text)
    {
        var expected = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(text.AsSpan()));
        Assert.Equal(expected, XxHash3Slim.Hash64(text));
        Assert.Equal(expected, XxHash3Slim.Hash64(text.AsSpan()));
    }
}
