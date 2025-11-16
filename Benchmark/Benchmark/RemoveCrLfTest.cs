using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Arc;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class RemoveCrLfTest
{
    private readonly string TestString = "This is a test string.This is a test string.This is a test string.This is a test string.";
    private readonly string TestString2 = "This is a test string.\r\nThis is a test string.\nThis is a test string.\r\nThis is a test string.";

    public RemoveCrLfTest()
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
    public string Test1()
    {
        return this.RemoveCrLf(this.TestString);
    }

    [Benchmark]
    public string Test1_IdexOf()
    {
        return BaseHelper.RemoveCrLf(this.TestString);
    }

    [Benchmark]
    public string Test2()
    {
        return this.RemoveCrLf(this.TestString2);
    }

    [Benchmark]
    public string Test2_IdexOf()
    {
        return BaseHelper.RemoveCrLf(this.TestString2);
    }

    private string RemoveCrLf(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        /*if (input.IndexOfAny(['\r', '\n']) < 0)
        {
            return input;
        }*/

        var newlineCount = 0;
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '\r' || input[i] == '\n')
            {
                newlineCount++;
            }
        }

        if (newlineCount == 0)
        {
            return input;
        }

        var resultLength = input.Length - newlineCount;
        return string.Create(resultLength, input, static (span, src) =>
        {
            var position = 0;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] != '\r' && src[i] != '\n')
                {
                    span[position++] = src[i];
                }
            }
        });
    }
}
