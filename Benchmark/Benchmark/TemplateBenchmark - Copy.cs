using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class Utf16HashtableBenchmark
{
    private string[] keys = default!;
    private int[] values = default!;
    private Utf16Hashtable<int> table = default!;
    private string target = default!;

    public Utf16HashtableBenchmark()
    {
        this.keys =
        [
            "alpha001",
            "bravo002",
            "charlie03",
            "delta004",
            "echo0005",
            "foxtrot6",
            "golf0007",
            "hotel008",      // target
            "india009",
            "juliett10",
            "kilo0011",
            "lima0012",
            "mike0013",
            "november",
            "oscar015",
            "papa0016",
        ];

        this.values = new int[this.keys.Length];

        this.table = new Utf16Hashtable<int>(this.keys.Length);

        for (var i = 0; i < this.keys.Length; i++)
        {
            this.values[i] = i;
            this.table.TryAdd(this.keys[i], i);
        }

        this.target = this.keys[7];
    }

    [Benchmark(Baseline = true)]
    public int StringArray_Search()
    {
        var keys = this.keys;
        var values = this.values;
        var target = this.target;

        for (var i = 0; i < keys.Length; i++)
        {
            if (keys[i] == target)
            {
                return values[i];
            }
        }

        return -1;
    }

    public int StringArray_Search_StringEqualsOrdinal()
    {
        var keys = this.keys;
        var values = this.values;
        var target = this.target;

        for (var i = 0; i < keys.Length; i++)
        {
            if (string.Equals(keys[i], target, StringComparison.Ordinal))
            {
                return values[i];
            }
        }

        return -1;
    }

    [Benchmark]
    public int Utf16Hashtable_Search_String()
    {
        if (this.table.TryGetValue(this.target, out var value))
        {
            return value;
        }

        return -1;
    }

    [Benchmark]
    public int Utf16Hashtable_Search_ReadOnlySpan()
    {
        var key = this.target.AsSpan();
        if (this.table.TryGetValue(key, out var value))
        {
            return value;
        }

        return -1;
    }
}
