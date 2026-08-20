using System.Collections.Generic;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public class UnorderedListBenchmark
{
    private const int Count = 1000;

    private UnorderedList<int> unorderedList = null!;
    private List<int> list = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.unorderedList = new UnorderedList<int>(Count);
        this.list = new List<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            this.unorderedList.Add(i);
            this.list.Add(i);
        }
    }

    /*[Benchmark]
    public UnorderedList<int> Unordered_Add()
    {
        var list = new UnorderedList<int>();

        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        return list;
    }

    [Benchmark]
    public List<int> List_Add()
    {
        var list = new List<int>();

        for (var i = 0; i < Count; i++)
        {
            list.Add(i);
        }

        return list;
    }

    [Benchmark]
    public int Unordered_Indexer()
    {
        var sum = 0;
        var list = this.unorderedList;

        for (var i = 0; i < Count; i++)
        {
            sum += list[i];
        }

        return sum;
    }

    [Benchmark]
    public int List_Indexer()
    {
        var sum = 0;
        var list = this.list;

        for (var i = 0; i < Count; i++)
        {
            sum += list[i];
        }

        return sum;
    }*/

    [Benchmark]
    public int Unordered_Enumerate()
    {
        var sum = 0;

        foreach (var x in this.unorderedList)
        {
            sum += x;
        }

        return sum;
    }

    [Benchmark]
    public int List_Enumerate()
    {
        var sum = 0;

        foreach (var x in this.list)
        {
            sum += x;
        }

        return sum;
    }

    /*[Benchmark]
    public bool Unordered_Contains()
        => this.unorderedList.Contains(Count - 1);

    [Benchmark]
    public bool List_Contains()
        => this.list.Contains(Count - 1);

    [Benchmark]
    public int Unordered_IndexOf()
        => this.unorderedList.IndexOf(Count - 1);

    [Benchmark]
    public int List_IndexOf()
        => this.list.IndexOf(Count - 1);

    [Benchmark]
    public UnorderedList<int> Unordered_Insert()
    {
        var list = new UnorderedList<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        return list;
    }

    [Benchmark]
    public List<int> List_Insert()
    {
        var list = new List<int>(Count);

        for (var i = 0; i < Count; i++)
        {
            list.Insert(0, i);
        }

        return list;
    }

    [Benchmark]
    public UnorderedList<int> Unordered_RemoveAt()
    {
        var list = new UnorderedList<int>(this.unorderedList);

        while (list.Count > 0)
        {
            list.RemoveAt(0);
        }

        return list;
    }

    [Benchmark]
    public List<int> List_RemoveAt()
    {
        var list = new List<int>(this.list);

        while (list.Count > 0)
        {
            list.RemoveAt(0);
        }

        return list;
    }*/
}
