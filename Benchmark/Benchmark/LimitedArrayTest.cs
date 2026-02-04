using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public class LimitedArrayTestClass
{
    public int Id { get; set; }

    public LimitedArrayTestClass(int id)
    {
        this.Id = id;
    }

    public override string ToString()
    {
        return $"Id:{this.Id}";
    }
}

[Config(typeof(BenchmarkConfig))]
public class LimitedArrayTest
{
    private const int TargetId = 42;
    private readonly int[] idArray = [0, 123456, -888, 42, 789456123];
    private readonly LimitedArrayTestClass[] array;
    private readonly Dictionary<int, LimitedArrayTestClass> dictionary;
    private readonly ConcurrentDictionary<int, LimitedArrayTestClass> concurrentDictionary;
    private readonly LimitedArray<LimitedArrayTestClass> limitedArray;

    public LimitedArrayTest()
    {
        this.array = this.idArray.Select(x => new LimitedArrayTestClass(x)).ToArray();

        this.dictionary = this.array.ToDictionary(x => x.Id, x => x);
        this.concurrentDictionary = new(this.array.ToDictionary(x => x.Id, x => x));
        this.limitedArray = new(this.array);
    }

    [Benchmark]
    public LimitedArrayTestClass? Find_Array()
    {
        foreach (var x in this.array)
        {
            if (x.Id == TargetId)
            {
                return x;
            }
        }

        return default;
    }

    [Benchmark]
    public LimitedArrayTestClass? FirstOrDefault_Array()
        => this.array.FirstOrDefault(static x => x.Id == TargetId);

    [Benchmark]
    public LimitedArrayTestClass? Dictionary_TryGetValue()
    {
        this.dictionary.TryGetValue(TargetId, out var value);
        return value;
    }

    [Benchmark]
    public LimitedArrayTestClass? ConcurrentDictionary_TryGetValue()
    {
        this.concurrentDictionary.TryGetValue(TargetId, out var value);
        return value;
    }

    [Benchmark]
    public LimitedArrayTestClass? Find_LimitedArray()
    {
        foreach (var x in this.limitedArray.GetValues())
        {
            if (x.Id == TargetId)
            {
                return x;
            }
        }

        return default;
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }
}
