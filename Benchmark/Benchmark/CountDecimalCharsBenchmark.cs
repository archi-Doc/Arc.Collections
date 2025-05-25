using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Arc;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class CountDecimalCharsBenchmark
{
    private int[] array = [0, 1, -1, 11, 1234, -1234567, 234, 34567123, 789456123, 789456,];
    private uint[] array2 = [0, 1, 19789, 11, 1234, 1234567, 234, 34567123, 789456123, 789456,];
    private static readonly int[] table = new int[32];

    private static readonly uint[] Pow10 = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000,];
    private static readonly ulong[] Pow10B = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000, 10_000_000_000_000, 100_000_000_000_000, 1_000_000_000_000_000, 10_000_000_000_000_000, 100_000_000_000_000_000, 1_000_000_000_000_000_000, 10_000_000_000_000_000_000,];

    public CountDecimalCharsBenchmark()
    {
        CreateTable();
    }

    private static void CreateTable()
    {
        for (int i = 0; i < 32; i++)
        {
            int bits = 32 - i; // - BitOperations.LeadingZeroCount(i);
            int log10 = (bits * 1233) >> 12; // 0-9
            table[i] = log10;
        }

        for (int i = 31; i >= 0; i--)
        {
            var min = 1u << (31 - i);
            var max = (1u << (31 - i + 1)) - 1;
            if (i == 0)
            {
                max = uint.MaxValue;
            }

            int log10 = ((32 - i) * 1233) >> 12; // 0-9

            Console.WriteLine($"lzc {i} {min}({CountDigits_TryFormat(min)})-{max}({CountDigits_TryFormat(max)}) log10 {log10} Pow {Pow10[log10]}");
        }

        for (int i = 63; i >= 0; i--)
        {
            var min = 1ul << (63 - i);
            var max = (1ul << (63 - i + 1)) - 1;
            if (i == 0)
            {
                max = ulong.MaxValue;
            }

            int log10 = ((64 - i) * 1233) >> 12;

            // Console.WriteLine($"lzc {i} {min}({CountDigits_TryFormat(min)})-{max}({CountDigits_TryFormat(max)}) log10 {log10} Pow {Pow10B[log10]}");
            Console.WriteLine($"lzc {i} ({CountDigits_TryFormat(min)})-({CountDigits_TryFormat(max)}) log10 {log10}");
        }
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

    private static int CountDigits_TryFormat(uint value)
    {
        Span<char> buffer = stackalloc char[11];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }

    private static int CountDigits_TryFormat(ulong value)
    {
        Span<char> buffer = stackalloc char[20];
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

    private static int CountDecimalChars2(int value)
    {
        if (value == 0)
        {
            return 1;
        }
        else if (value > 0)
        {
            int bits = 31 - BitOperations.LeadingZeroCount((uint)value);
            int log10 = ((bits + 1) * 1233) >> 12;
            return log10 + ((value >= Pow10[log10]) ? 1 : 0);
        }
        else
        {
            int bits = 31 - BitOperations.LeadingZeroCount((uint)-value);
            int log10 = ((bits + 1) * 1233) >> 12;
            return log10 + 1 + ((-value >= Pow10[log10]) ? 1 : 0);
        }
    }

    private static int CountDecimalChars2(uint value)
    {
        if (value == 0)
        {
            return 1;
        }

        int bits = 32 - BitOperations.LeadingZeroCount(value);
        int log10 = (bits * 1233) >> 12; // 0-9
        return log10 + ((value >= Pow10[log10]) ? 1 : 0);
    }

    private static int CountDecimalChars3(uint value)
    {
        if (value == 0)
        {
            return 1;
        }

        var log10 = table[BitOperations.LeadingZeroCount(value)];
        return log10 + ((value >= Pow10[log10]) ? 1 : 0);
    }

    private static int CountDecimalChars4(uint x)
    {
        if (x < 10u) return 1;
        if (x < 100u) return 2;
        if (x < 1000u) return 3;
        if (x < 10000u) return 4;
        if (x < 100000u) return 5;
        if (x < 1000000u) return 6;
        if (x < 10000000u) return 7;
        if (x < 100000000u) return 8;
        if (x < 1000000000u) return 9;
        return 10;
    }

    /*[Benchmark]
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
    public int Test_CountDecimalChars2()
    {
        var count = 0;
        foreach (var x in this.array)
        {
            count += CountDecimalChars2(x);
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
    }*/

    [Benchmark]
    public int Test2()
    {
        var count = 0;
        foreach (var x in this.array2)
        {
            count += BaseHelper.CountDecimalChars(x);
        }

        return count;
    }

    [Benchmark]
    public int Test2_CountDecimalChars2()
    {
        var count = 0;
        foreach (var x in this.array2)
        {
            count += CountDecimalChars2(x);
        }

        return count;
    }

    [Benchmark]
    public int Test2_CountDecimalChars3()
    {
        var count = 0;
        foreach (var x in this.array2)
        {
            count += CountDecimalChars3(x);
        }

        return count;
    }

    [Benchmark]
    public int Test2_CountDecimalChars4()
    {
        var count = 0;
        foreach (var x in this.array2)
        {
            count += CountDecimalChars4(x);
        }

        return count;
    }
}
