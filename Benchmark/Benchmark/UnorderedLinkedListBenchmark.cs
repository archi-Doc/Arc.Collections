using System.Collections.Generic;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public class UnorderedLinkedListBenchmark
{
    private const int Count = 1000;

    private UnorderedLinkedList<int> unorderedList = null!;
    private LinkedList<int> linkedList = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.unorderedList = new UnorderedLinkedList<int>();
        this.linkedList = new LinkedList<int>();

        for (var i = 0; i < Count; i++)
        {
            this.unorderedList.AddLast(i);
            this.linkedList.AddLast(i);
        }
    }

    [Benchmark]
    public UnorderedLinkedList<int> Unordered_AddLast()
    {
        var list = new UnorderedLinkedList<int>();

        for (var i = 0; i < Count; i++)
        {
            list.AddLast(i);
        }

        return list;
    }

    [Benchmark]
    public LinkedList<int> LinkedList_AddLast()
    {
        var list = new LinkedList<int>();

        for (var i = 0; i < Count; i++)
        {
            list.AddLast(i);
        }

        return list;
    }

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
    public int LinkedList_Enumerate()
    {
        var sum = 0;

        foreach (var x in this.linkedList)
        {
            sum += x;
        }

        return sum;
    }

    [Benchmark]
    public bool Unordered_Contains()
        => this.unorderedList.Contains(Count - 1);

    [Benchmark]
    public bool LinkedList_Contains()
        => this.linkedList.Contains(Count - 1);

    [Benchmark]
    public int Unordered_RemoveFirstAddLast()
    {
        var list = this.unorderedList;
        var value = list.First!.Value;

        list.RemoveFirst();
        list.AddLast(value);

        return list.Count;
    }

    [Benchmark]
    public int LinkedList_RemoveFirstAddLast()
    {
        var list = this.linkedList;
        var value = list.First!.Value;

        list.RemoveFirst();
        list.AddLast(value);

        return list.Count;
    }
}
