using System;
using Arc;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

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
