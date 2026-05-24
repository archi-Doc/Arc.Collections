// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace xUnitTest;

public class CircularQueueTest
{
    [Fact]
    public void Test1()
    {
        var q = new CircularQueue<int>(4);
        q.TryEnqueue(1);
        q.TryEnqueue(2);
        q.TryEnqueue(3);
        q.TryEnqueue(4);
        q.TryEnqueue(5);

        int i;
        q.TryDequeue(out i).IsTrue();
        i.Is(1);
        q.TryDequeue(out i).IsTrue();
        i.Is(2);
        q.TryDequeue(out i).IsTrue();
        i.Is(3);
        q.TryDequeue(out i).IsTrue();
        i.Is(4);
        q.TryDequeue(out i).IsFalse();
    }

    [Fact]
    public async Task Test2()
    {
        const int Concurrent = 10;
        const int N = 1000;

        var total = N * Concurrent;
        var q = new CircularQueue<int>(total);

        Parallel.ForEach(Enumerable.Range(0, Concurrent), x =>
        {
            var start = x * N;
            for (var j = 0; j < N; j++)
            {
                q.TryEnqueue(start + j);
            }
        });

        q.Count.Is(total);
        var list = new List<int>();
        while (q.TryDequeue(out var x))
        {
            list.Add(x);
        }

        list.Sort();

        list.Count.Is(total);
        for (var i = 0; i < total; i++)
        {
            list[i].Is(i);
        }
    }

    private static void DequeueN(CircularQueue<int> queue, int count)
    {
        SpinWait spinner = default;
        while (true)
        {
            if (count == 0)
            {
                return;
            }

            if (queue.TryDequeue(out _))
            {
                count--;
            }
            else
            {
                spinner.SpinOnce();
            }
        }
    }

    private static void Dequeue(CircularQueue<int> queue)
    {
        SpinWait spinner = default;
        while (true)
        {
            if (queue.TryDequeue(out _))
            {
                return;
            }
            else
            {
                spinner.SpinOnce();
            }
        }
    }

    private static void EnqueueN(CircularQueue<int> queue, Random random, int count)
    {
        while (count-- > 0)
        {
            Enqueue(queue, random);
        }
    }

    private static void Enqueue(CircularQueue<int> queue, Random random)
    {
        SpinWait spinner = default;
        var value = (int)random.NextInt64();
        while (true)
        {
            if (queue.TryEnqueue(value))
            {
                return;
            }

            spinner.SpinOnce();
        }
    }
}
