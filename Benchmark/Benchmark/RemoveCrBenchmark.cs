using System;
using Arc;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class RemoveCrBenchmark
{
    private string[] testStrings;
    private string[] tempStrings;
    private char[][] testArrays;
    private char[][] tempArrays;

    public RemoveCrBenchmark()
    {
        testStrings = [
            "",
            "ABC",
            "ABC\r\n",
            "ABC\r\n0123456789\r\n987654321123456789987654321\r\n0123456789\r\nABC",
            "ABC\r\n0123456789\r\n987654321123456789987654321\r\n0123456789\r\nABC\r\n\r\nABC\r\n0123456789\r\n987654321123456789987654321\r\n0123456789\r\nABC",
             "ABC\r\n0123456789\r\n987654321123456789987654321\r\n0123456789\r\nABC\r\n987654321123456789987654321n987654321123456789987654321\r\n987654321123456789987654321n987654321123456789987654321\r\n987654321123456789987654321",
            ];

        this.tempStrings = new string[testStrings.Length];
        this.testArrays = new char[testStrings.Length][];
        this.tempArrays = new char[testStrings.Length][];
        for (var i = 0; i < testStrings.Length; i++)
        {
            this.testArrays[i] = this.testStrings[i].ToCharArray();
            this.tempArrays[i] = new char[this.testStrings[i].Length];
        }
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
    public string[] RemoveCr_String()
    {
        for (var i = 0; i < testStrings.Length; i++)
        {
            this.tempStrings[i] = BaseHelper.RemoveAllOccurrences(this.testStrings[i], '\r');
        }

        return this.tempStrings;
    }

    [Benchmark]
    public char[][] RemoveCr_Span()
    {
        for (var i = 0; i < testStrings.Length; i++)
        {
            this.testArrays[i].AsSpan().CopyTo(this.tempArrays[i]);
            var span = this.tempArrays[i].AsSpan();
            BaseHelper.RemoveAllOccurrences(span, '\r');

        }

        return this.tempArrays;
    }
}
