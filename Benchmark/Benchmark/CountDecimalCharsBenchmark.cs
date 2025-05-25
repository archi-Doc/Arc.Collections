using System;
using BenchmarkDotNet.Attributes;
using Arc;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CountDecimalCharsBenchmark
{
    private int[] array = [0, 1, -1, 11, 1234, -1234567, 234, 34567123, 789456123, 789456,];

    public CountDecimalCharsBenchmark()
    {
    }

    private static int CountDigits_Log10(int value)
    {
        if (value == 0) return 1;
        long abs = value < 0 ? -(long)value : value;
        int digits = (int)Math.Floor(Math.Log10(abs)) + 1;
        return digits + (value < 0 ? 1 : 0);
    }

    private static int CountDigits_TryFormat(int value)
    {
        Span<char> buffer = stackalloc char[11];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }

    private static int CountDecimalChars(int value)
    {
        uint x = value < 0 ? (uint)(-(long)value) : (uint)value;
        int digits = 1;
        if (x >= 100_000_000) { digits += 8; x /= 100_000_000; }
        if (x >= 10_000) { digits += 4; x /= 10_000; }
        if (x >= 100) { digits += 2; x /= 100; }
        if (x >= 10) { digits += 1; }
        return digits + (value < 0 ? 1 : 0);
    }

    [Benchmark]
    public int Test1()
    {
        var count = 0;
        foreach (var x in this.array)
        {
            count += BaseHelper.CountDecimalChars(x);
        }

        return count;
    }

    [Benchmark]
    public int Test_CountDecimalChars()
    {
        var count = 0;
        foreach (var x in this.array)
        {
            count += CountDecimalChars(x);
        }

        return count;
    }

    [Benchmark]
    public int Test_Log()
    {
        var count = 0;
        foreach (var x in this.array)
        {
            count += CountDigits_Log10(x);
        }

        return count;
    }

    [Benchmark]
    public int Test_TryFormat()
    {
        var count = 0;
        foreach (var x in this.array)
        {
            count += CountDigits_TryFormat(x);
        }

        return count;
    }
}
