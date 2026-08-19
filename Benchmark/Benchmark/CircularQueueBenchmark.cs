using System.Collections.Concurrent;
using Arc.Collections;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class CircularQueueBenchmark
{
    private const int Capacity = 1024;
    private const int Operations = 1000;

    private CircularQueue<int> circularQueue = null!;
    private ConcurrentQueue<int> concurrentQueue = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.circularQueue = new CircularQueue<int>(Capacity);
        this.concurrentQueue = new ConcurrentQueue<int>();
    }

    [Benchmark(Baseline = true)]
    public int CircularQueue()
    {
        var sum = 0;

        for (var i = 0; i < Operations; i++)
        {
            this.circularQueue.TryEnqueue(i);
            this.circularQueue.TryDequeue(out var value);
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int ConcurrentQueue()
    {
        var sum = 0;

        for (var i = 0; i < Operations; i++)
        {
            this.concurrentQueue.Enqueue(i);
            this.concurrentQueue.TryDequeue(out var value);
            sum += value;
        }

        return sum;
    }
}
