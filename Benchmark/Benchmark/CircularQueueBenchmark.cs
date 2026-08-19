using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Benchmark;
using BenchmarkDotNet.Attributes;

/// <summary>
/// Multi-threaded producer/consumer benchmark.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CircularQueueBenchmark
{
    private const int Capacity = 1024;
    private const int ItemsPerProducer = 10_000;

    [Params(1, 2, 4)]
    public int ThreadPairs { get; set; } // number of producer threads == number of consumer threads

    private CircularQueue<int> circularQueue = default!;
    private ConcurrentQueue<int> concurrentQueue = default!;

    [IterationSetup]
    public void IterationSetup()
    {
        this.circularQueue = new CircularQueue<int>(Capacity);
        this.concurrentQueue = new ConcurrentQueue<int>();
    }

    [Benchmark(Baseline = true)]
    public void CircularQueue_ProducerConsumer()
    {
        var queue = this.circularQueue;
        var total = this.ThreadPairs * ItemsPerProducer;
        var consumed = 0;
        var tasks = new Task[this.ThreadPairs * 2];

        for (var i = 0; i < this.ThreadPairs; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < ItemsPerProducer; j++)
                {
                    while (!queue.TryEnqueue(j))
                    {
                        Thread.SpinWait(1); // Queue is full: spin until a slot is available.
                    }
                }
            });

            tasks[this.ThreadPairs + i] = Task.Run(() =>
            {
                while (Volatile.Read(ref consumed) < total)
                {
                    if (queue.TryDequeue(out _))
                    {
                        Interlocked.Increment(ref consumed);
                    }
                    else
                    {
                        Thread.SpinWait(1);
                    }
                }
            });
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void ConcurrentQueue_ProducerConsumer()
    {
        var queue = this.concurrentQueue;
        var total = this.ThreadPairs * ItemsPerProducer;
        var consumed = 0;
        var tasks = new Task[this.ThreadPairs * 2];

        for (var i = 0; i < this.ThreadPairs; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < ItemsPerProducer; j++)
                {
                    // Emulate the bounded limit of CircularQueue.
                    while (queue.Count >= Capacity)
                    {
                        Thread.SpinWait(1);
                    }

                    queue.Enqueue(j);
                }
            });

            tasks[this.ThreadPairs + i] = Task.Run(() =>
            {
                while (Volatile.Read(ref consumed) < total)
                {
                    if (queue.TryDequeue(out _))
                    {
                        Interlocked.Increment(ref consumed);
                    }
                    else
                    {
                        Thread.SpinWait(1);
                    }
                }
            });
        }

        Task.WaitAll(tasks);
    }
}
