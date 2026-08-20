using System;
using System.Text;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public class UtfHashtableBenchmark
{
    private const int Count = 10_000;

    private Utf16Hashtable<int> utf16 = null!;
    private Utf8Hashtable<int> utf8 = null!;

    private string[] stringKeys = null!;
    private byte[][] utf8Keys = null!;

    private string hitString = null!;
    private string missString = null!;

    private byte[] hitUtf8 = null!;
    private byte[] missUtf8 = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.stringKeys = new string[Count];
        this.utf8Keys = new byte[Count][];

        this.utf16 = new Utf16Hashtable<int>(Count);
        this.utf8 = new Utf8Hashtable<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            var key = $"Key-{i:D8}";
            var bytes = Encoding.UTF8.GetBytes(key);

            this.stringKeys[i] = key;
            this.utf8Keys[i] = bytes;

            this.utf16.Add(key, i);
            this.utf8.Add(bytes, i);
        }

        this.hitString = this.stringKeys[Count - 1];
        this.hitUtf8 = this.utf8Keys[Count - 1];

        this.missString = "Key-99999999";
        this.missUtf8 = Encoding.UTF8.GetBytes(this.missString);
    }

    // ---------------------------------------------------------------------
    // Build
    // ---------------------------------------------------------------------

    [Benchmark]
    public Utf16Hashtable<int> Utf16_Build()
    {
        var table = new Utf16Hashtable<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            table.Add(this.stringKeys[i], i);
        }

        return table;
    }

    [Benchmark]
    public Utf8Hashtable<int> Utf8_Build_ByteArray()
    {
        var table = new Utf8Hashtable<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            table.Add(this.utf8Keys[i], i);
        }

        return table;
    }

    [Benchmark]
    public Utf8Hashtable<int> Utf8_Build_Span()
    {
        var table = new Utf8Hashtable<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            table.Add(this.utf8Keys[i].AsSpan(), i);
        }

        return table;
    }

    // ---------------------------------------------------------------------
    // TryGetValue - hit
    // ---------------------------------------------------------------------

    [Benchmark]
    public int Utf16_Get_String()
    {
        this.utf16.TryGetValue(this.hitString, out var value);
        return value;
    }

    [Benchmark]
    public int Utf16_Get_Span()
    {
        this.utf16.TryGetValue(this.hitString.AsSpan(), out var value);
        return value;
    }

    [Benchmark]
    public int Utf8_Get_ByteArray()
    {
        this.utf8.TryGetValue(this.hitUtf8, out var value);
        return value;
    }

    [Benchmark]
    public int Utf8_Get_Span()
    {
        this.utf8.TryGetValue(this.hitUtf8.AsSpan(), out var value);
        return value;
    }

    // ---------------------------------------------------------------------
    // TryGetValue - miss
    // ---------------------------------------------------------------------

    [Benchmark]
    public bool Utf16_Get_Miss_String()
        => this.utf16.TryGetValue(this.missString, out _);

    [Benchmark]
    public bool Utf16_Get_Miss_Span()
        => this.utf16.TryGetValue(this.missString.AsSpan(), out _);

    [Benchmark]
    public bool Utf8_Get_Miss_ByteArray()
        => this.utf8.TryGetValue(this.missUtf8, out _);

    [Benchmark]
    public bool Utf8_Get_Miss_Span()
        => this.utf8.TryGetValue(this.missUtf8.AsSpan(), out _);

    // ---------------------------------------------------------------------
    // ContainsKey
    // ---------------------------------------------------------------------

    [Benchmark]
    public bool Utf16_Contains_String()
        => this.utf16.ContainsKey(this.hitString);

    [Benchmark]
    public bool Utf16_Contains_Span()
        => this.utf16.ContainsKey(this.hitString.AsSpan());

    /*[Benchmark]
    public bool Utf8_Contains_ByteArray()
        => this.utf8.ContainsKey(this.hitUtf8);

    [Benchmark]
    public bool Utf8_Contains_Span()
        => this.utf8.ContainsKey(this.hitUtf8.AsSpan());*/

    // ---------------------------------------------------------------------
    // Full lookup
    // ---------------------------------------------------------------------

    [Benchmark]
    public int Utf16_GetAll()
    {
        var sum = 0;

        for (var i = 0; i < Count; i++)
        {
            this.utf16.TryGetValue(this.stringKeys[i], out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Utf8_GetAll()
    {
        var sum = 0;

        for (var i = 0; i < Count; i++)
        {
            this.utf8.TryGetValue(this.utf8Keys[i], out var value);
            sum += value;
        }

        return sum;
    }

    // ---------------------------------------------------------------------
    // Snapshot
    // ---------------------------------------------------------------------

    [Benchmark]
    public int Utf16_ToArray()
        => this.utf16.ToArray().Length;

    [Benchmark]
    public int Utf8_ToArray()
        => this.utf8.ToArray().Length;
}
