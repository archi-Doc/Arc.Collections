// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using Arc.Collections;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class BytePoolTest
{
    private const int N = 256;

    private ConcurrentQueue<byte[]> concurrentQueue = new();
    private int count;

    public BytePoolTest()
    {
    }

    [Benchmark]
    public byte[] NewByteN()
    {
        return new byte[N];
    }

    [Benchmark]
    public byte[] ConcurrentQueueN()
    {// ConcurrentQueue + Interlocked
        byte[]? byteArray;
        if (this.concurrentQueue.TryDequeue(out byteArray))
        {
            Interlocked.Decrement(ref this.count);
        }
        else
        {
            byteArray = new byte[N];
        }

        this.concurrentQueue.Enqueue(byteArray);
        Interlocked.Increment(ref this.count);
        return byteArray;
    }

    [Benchmark]
    public byte[] ArrayPoolN()
    {// CircularQueue
        var array = ArrayPool<byte>.Shared.Rent(N);
        ArrayPool<byte>.Shared.Return(array);
        return array;
    }

    [Benchmark]
    public byte[] BytePoolN()
    {// BytePool
        var rentArray = BytePool.Default.Rent(N);
        rentArray.Return();
        return rentArray.Array;
    }
}
