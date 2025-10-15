// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public record class ObjectPoolTestClass
{
    private static ObjectPool<ObjectPoolTestClass> pool = new(() => new ObjectPoolTestClass(), 32);

    public static ObjectPoolTestClass Rent() => pool.Rent();

    public static void Return(ObjectPoolTestClass obj) => pool.Return(obj);

    public ObjectPoolTestClass()
    {
    }

    public object Object1 { get; set; } = new();

    public object Object2 { get; set; } = new();

    public object Object3 { get; set; } = new();

    public object Object4 { get; set; } = new();
}

public record struct ObjectPoolTestStruct
{
    public ObjectPoolTestStruct(object obj1, object obj2, object obj3, object obj4)
    {
        this.Object1 = obj1;
        this.Object2 = obj2;
        this.Object3 = obj3;
        this.Object4 = obj4;
    }

    public object Object1 { get; set; }

    public object Object2 { get; set; }

    public object Object3 { get; set; }

    public object Object4 { get; set; }
}

[Config(typeof(BenchmarkConfig))]
public class ObjectPoolBenchmark
{
    public ObjectPoolBenchmark()
    {
        var provider = new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider();
        provider.MaximumRetained = 32;
        this.objectPool = provider.Create<Sha3_256>();
    }

    private ObjectPoolTestClass testClass = new();

    public Sha3_256 SHA3Instance { get; } = new();

#pragma warning disable SA1401 // Fields should be private
    public Sha3_256? SHA3Instance2;
#pragma warning restore SA1401 // Fields should be private

    public ObjectPool<Sha3_256> ObjectPool { get; } = new(() => new Sha3_256());

    public ObjectPoolObsolete<Sha3_256> ObjectPoolObsolete { get; } = new(() => new Sha3_256());

    public LooseObjectPool<Sha3_256> LooseObjectPool { get; } = new(() => new Sha3_256());

    // public ObjectPool<Sha3_256> ObjectPoolPrepare { get; } = new(() => new Sha3_256(), 32, true);

    private Microsoft.Extensions.ObjectPool.ObjectPool<Sha3_256> objectPool;

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public ObjectPoolTestStruct TestStruct_New()
    {
        var st = new ObjectPoolTestStruct(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        st.Object1 = st.Object2;
        st.Object3 = st.Object4;
        return st;
    }

    [Benchmark]
    public ObjectPoolTestClass TestClass_New()
    {
        return new();
    }

    [Benchmark]
    public ObjectPoolTestClass TestClass_Pool1()
    {
        var obj = ObjectPoolTestClass.Rent();
        ObjectPoolTestClass.Return(obj);
        return obj;
    }

    [Benchmark]
    public ObjectPoolTestClass TestClass_Pool8()
    {
        var obj1 = ObjectPoolTestClass.Rent();
        var obj2 = ObjectPoolTestClass.Rent();
        var obj3 = ObjectPoolTestClass.Rent();
        var obj4 = ObjectPoolTestClass.Rent();
        var obj5 = ObjectPoolTestClass.Rent();
        var obj6 = ObjectPoolTestClass.Rent();
        var obj7 = ObjectPoolTestClass.Rent();
        var obj8 = ObjectPoolTestClass.Rent();
        ObjectPoolTestClass.Return(obj1);
        ObjectPoolTestClass.Return(obj2);
        ObjectPoolTestClass.Return(obj3);
        ObjectPoolTestClass.Return(obj4);
        ObjectPoolTestClass.Return(obj5);
        ObjectPoolTestClass.Return(obj6);
        ObjectPoolTestClass.Return(obj7);
        ObjectPoolTestClass.Return(obj8);
        return obj1;
    }

    [Benchmark]
    public Sha3_256 SHA3_NewAndGet()
    {
        var h = new Sha3_256();
        // return h.GetHash(this.ByteArray);
        return h;
    }

    [Benchmark]
    public Sha3_256 SHA3_ObjectPool()
    {
        var h = this.ObjectPool.Rent();
        try
        {
            // return h.GetHash(this.ByteArray);
            return h;
        }
        finally
        {
            this.ObjectPool.Return(h);
        }
    }

    [Benchmark]
    public Sha3_256 SHA3_ObjectPoolMs()
    {
        var h = this.objectPool.Get();
        try
        {
            // return h.GetHash(this.ByteArray);
            return h;
        }
        finally
        {
            this.objectPool.Return(h);
        }
    }

    /*[Benchmark]
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

    /*[Benchmark]
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
    }*/
}
