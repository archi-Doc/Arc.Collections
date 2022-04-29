// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class ObjectPoolBenchmark
{
    public ObjectPoolBenchmark()
    {
    }

    [Params(10)]
    public int Length { get; set; }

    public byte[] ByteArray { get; set; } = default!;

    public Sha3_256 SHA3Instance { get; } = new();

#pragma warning disable SA1401 // Fields should be private
    public Sha3_256? SHA3Instance2;
#pragma warning restore SA1401 // Fields should be private

    public ObjectPool<Sha3_256> ObjectPool { get; } = new(() => new Sha3_256());

    public ObjectPoolObsolete<Sha3_256> ObjectPoolObsolete { get; } = new(() => new Sha3_256());

    public LooseObjectPool<Sha3_256> LooseObjectPool { get; } = new(() => new Sha3_256());

    public ObjectPool<Sha3_256> ObjectPoolPrepare { get; } = new(() => new Sha3_256(), 32, true);

    [GlobalSetup]
    public void Setup()
    {
        this.ByteArray = new byte[this.Length];
        for (var i = 0; i < this.Length; i++)
        {
            this.ByteArray[i] = (byte)i;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    /*[Benchmark]
    public byte[] SHA3_NewAndGet()
    {
        var h = new SHA3_256();
        return h.GetHash(this.ByteArray);
    }

    [Benchmark]
    public byte[] SHA3_ObjectPool()
    {
        var h = this.ObjectPool.Get();
        try
        {
            return h.GetHash(this.ByteArray);
        }
        finally
        {
            this.ObjectPool.Return(h);
        }
    }

    [Benchmark]
    public byte[] SHA3_LooseObjectPool()
    {
        var h = this.LooseObjectPool.Rent();
        try
        {
            return h.GetHash(this.ByteArray);
        }
        finally
        {
            this.LooseObjectPool.Return(h);
        }
    }

    [Benchmark]
    public byte[] SHA3_ObjectPoolObsolete()
    {
        var h = this.ObjectPoolObsolete.Get();
        try
        {
            return h.GetHash(this.ByteArray);
        }
        finally
        {
            this.ObjectPoolObsolete.Return(h);
        }
    }

    [Benchmark]
    public byte[] SHA3_NoInstance()
    {
        return this.SHA3Instance.GetHash(this.ByteArray);
    }*/

    [Benchmark]
    public Sha3 SHA3_NewInstance()
    {
        return new Sha3_256();
    }

    [Benchmark]
    public Sha3 SHA3_PrepareInstance()
    {
        var h = this.ObjectPoolPrepare.Get();
        try
        {
            return h;
        }
        finally
        {
            this.ObjectPoolPrepare.Return(h);
        }
    }
}
