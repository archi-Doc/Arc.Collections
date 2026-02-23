using System;
using Arc;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

internal static class WhiteSpaceHelper
{
    public static int IndexOfSeparatorB(this ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            var val = span[i];

            if (val < 0xFF)
            {
                if (val <= 0x0D && val >= 0x09)
                { // U+0009 to U+000D
                    return i;
                }
                else if (val == 0x20 || val == 0x2C || val == 0xA0)
                { // Separator (Space, ',', NBSP)
                    return i;
                }

                // Not separator.
                continue;
            }

            if (val >= '\u2000' && val <= '\u200A')
            {// U+2000 to U+200A
                return i;
            }
            else if (val == '\u2028' || val == '\u2029' || val == '\u3000')
            {// U+2028, U+2029, U+3000
                return i;
            }

            // Not separator.
        }

        return -1;
    }
}

[Config(typeof(BenchmarkConfig))]
public class WhiteSpaceBenchmark
{
    public string Text { get; private set; } = "test123456 zzz";

    public string Text2 { get; private set; } = "テストあういえおかきくけこtest　zzz";

    public WhiteSpaceBenchmark()
    {
        int i;
        i = this.IsWhiteSpace();
        i = this.IndexOfSeparator();
        i = this.IndexOfSeparatorB();
        i = this.IsWhiteSpace2();
        i = this.IndexOfSeparator2();
        i = this.IndexOfSeparatorB2();
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
    public int IsWhiteSpace()
    {
        int i = 0;
        var span = this.Text.AsSpan();
        for (i = 0; i < span.Length; i++)
        {
            if (char.IsWhiteSpace(span[i]))
            {
                return i;
            }
        }

        return i;
    }

    [Benchmark]
    public int IndexOfSeparator()
    {
        return this.Text.AsSpan().IndexOfSeparator();
    }

    [Benchmark]
    public int IndexOfSeparatorB()
    {
        return this.Text.AsSpan().IndexOfSeparatorB();
    }

    [Benchmark]
    public int IsWhiteSpace2()
    {
        int i = 0;
        var span = this.Text2.AsSpan();
        for (i = 0; i < span.Length; i++)
        {
            if (char.IsWhiteSpace(span[i]))
            {
                return i;
            }
        }

        return i;
    }

    [Benchmark]
    public int IndexOfSeparator2()
    {
        return this.Text2.AsSpan().IndexOfSeparator();
    }

    [Benchmark]
    public int IndexOfSeparatorB2()
    {
        return this.Text2.AsSpan().IndexOfSeparatorB();
    }
}
