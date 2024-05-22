// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
        var total = BytePool.Default.CalculateMaxMemoryUsage();
        Console.WriteLine(total / 1024 / 1024);
    }

    [Benchmark]
    public byte[] NewByte1()
    {
        return new byte[N];
    }

    [Benchmark]
    public byte[] ConcurrentQueue1()
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
    public byte[] ArrayPool1()
    {// CircularQueue
        var array = ArrayPool<byte>.Shared.Rent(N);
        ArrayPool<byte>.Shared.Return(array);
        return array;
    }

    [Benchmark]
    public byte[] BytePool1()
    {// BytePool
        var rentArray = BytePool.Default.Rent(N);
        rentArray.Return();
        return rentArray.Array;
    }

    [Benchmark]
    public byte[] SimpleBytePool1()
    {// SimpleBytePool
        var rentArray = SimpleBytePool.Default.Rent(N);
        rentArray.Return();
        return rentArray.Array;
    }

    [Benchmark]
    public (byte[], byte[], byte[]) NewByte3()
    {
        return (new byte[N], new byte[N], new byte[N]);
    }

    [Benchmark]
    public (byte[], byte[], byte[]) ConcurrentQueue3()
    {// ConcurrentQueue + Interlocked
        if (this.concurrentQueue.TryDequeue(out var byteArray))
        {
            Interlocked.Decrement(ref this.count);
        }
        else
        {
            byteArray = new byte[N];
        }

        if (this.concurrentQueue.TryDequeue(out var byteArray2))
        {
            Interlocked.Decrement(ref this.count);
        }
        else
        {
            byteArray2 = new byte[N];
        }

        if (this.concurrentQueue.TryDequeue(out var byteArray3))
        {
            Interlocked.Decrement(ref this.count);
        }
        else
        {
            byteArray3 = new byte[N];
        }

        this.concurrentQueue.Enqueue(byteArray);
        Interlocked.Increment(ref this.count);
        this.concurrentQueue.Enqueue(byteArray2);
        Interlocked.Increment(ref this.count);
        this.concurrentQueue.Enqueue(byteArray3);
        Interlocked.Increment(ref this.count);
        return (byteArray, byteArray2, byteArray3);
    }

    [Benchmark]
    public (byte[], byte[], byte[]) ArrayPool3()
    {// CircularQueue
        var array = ArrayPool<byte>.Shared.Rent(N);
        var array2 = ArrayPool<byte>.Shared.Rent(N);
        var array3 = ArrayPool<byte>.Shared.Rent(N);
        ArrayPool<byte>.Shared.Return(array);
        ArrayPool<byte>.Shared.Return(array2);
        ArrayPool<byte>.Shared.Return(array3);
        return (array, array2, array3);
    }

    [Benchmark]
    public (byte[], byte[], byte[]) BytePool3()
    {// BytePool
        var rentArray = BytePool.Default.Rent(N);
        var rentArray2 = BytePool.Default.Rent(N);
        var rentArray3 = BytePool.Default.Rent(N);
        rentArray.Return();
        rentArray2.Return();
        rentArray3.Return();
        return (rentArray.Array, rentArray2.Array, rentArray3.Array);
    }

    [Benchmark]
    public (byte[], byte[], byte[]) SimpleBytePool3()
    {// SimpleBytePool
        var rentArray = SimpleBytePool.Default.Rent(N);
        var rentArray2 = SimpleBytePool.Default.Rent(N);
        var rentArray3 = SimpleBytePool.Default.Rent(N);
        rentArray.Return();
        rentArray2.Return();
        rentArray3.Return();
        return (rentArray.Array, rentArray2.Array, rentArray3.Array);
    }
}
