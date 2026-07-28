using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Kimi.Compiler.Lexing;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class StringBuilderBenchmark
{
    private List<string> list = ["abc", "0123456789", "ABCDEFGHIJ", "9876543210", ];

    public StringBuilderBenchmark()
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
    public string StringBuilder()
    {
        var sb = new StringBuilder();
        foreach (var x in list)
        {
            sb.Append(x);
        }

        return sb.ToString();
    }

    [Benchmark]
    public string SequenceWriter()
    {
        using var sb = new RefStringBuilder();
        foreach (var x in list)
        {
            sb.AddRange(x);
        }

        return sb.ToString();
    }

    [Benchmark]
    public string PooledStringBuilder()
    {
        using var sb = new PooledStringBuilder();
        foreach (var x in list)
        {
            sb.Append(x);
        }

        var st = sb.ToString();
        return st;
    }
}
