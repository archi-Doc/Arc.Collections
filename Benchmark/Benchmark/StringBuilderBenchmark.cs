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
    private int x = 123456789;
    private double y = 0.987654321;
    private bool z = true;

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
    public string Interpolation()
    {
        return $"{this.list[0]}{this.list[1]}{this.list[2]}{this.list[3]} {this.x}{this.y}->{this.z}";
    }

    [Benchmark]
    public string StringBuilder()
    {
        var sb = new StringBuilder();
        foreach (var x in list)
        {
            sb.Append(x);
        }

        sb.Append(' ');
        sb.Append(this.x);
        sb.Append(this.y);
        sb.Append("->");
        sb.Append(this.z);

        return sb.ToString();
    }

    // [Benchmark]
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

        sb.Append(' ');
        sb.Append(this.x);
        sb.Append(this.y);
        sb.Append("->");
        sb.Append(this.z);

        var st = sb.ToString();
        return st;
    }

    // [Benchmark]
    public string StringJoin()
    {
        var st = string.Concat(this.list);
        return st;
    }
}
