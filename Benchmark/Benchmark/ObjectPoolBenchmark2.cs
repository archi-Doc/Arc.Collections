// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

public record class ObjectPoolTestClass2
{
    private static ObjectPool<ObjectPoolTestClass2> pool = new(() => new ObjectPoolTestClass2(), 32);

    public static ObjectPoolTestClass2 Rent(object obj1, object obj2, object obj3, object obj4)
    {
        var obj = pool.Rent();
        obj.Object1 = obj1;
        obj.Object2 = obj2;
        obj.Object3 = obj3;
        obj.Object4 = obj4;
        return obj;
    }

    public static void Return(ObjectPoolTestClass2 obj) => pool.Return(obj);

    public ObjectPoolTestClass2(object obj1, object obj2, object obj3, object obj4)
    {
        this.Object1 = obj1;
        this.Object2 = obj2;
        this.Object3 = obj3;
        this.Object4 = obj4;
    }

    private ObjectPoolTestClass2()
    {
        this.Object1 = default!;
        this.Object2 = default!;
        this.Object3 = default!;
        this.Object4 = default!;
    }

    public object Object1 { get; set; }

    public object Object2 { get; set; }

    public object Object3 { get; set; }

    public object Object4 { get; set; }

    public void Process()
    {
        lock (this.Object1)
        {
            lock (this.Object2)
            {
            }
        }
    }
}

public record struct ObjectPoolTestStruct2
{
    public ObjectPoolTestStruct2(object obj1, object obj2, object obj3, object obj4)
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

    public void Process()
    {
        lock (this.Object1)
        {
            lock (this.Object2)
            {
            }
        }
    }
}

[Config(typeof(BenchmarkConfig))]
public class ObjectPoolBenchmark2
{
    private readonly ObjectPoolTestClass2 testClass = new(new(), new(), new(), new());

    public ObjectPoolBenchmark2()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public ObjectPoolTestStruct2 TestStruct_New()
    {
        var st = new ObjectPoolTestStruct2(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        st.Process();
        return st;
    }

    [Benchmark]
    public ObjectPoolTestClass2 TestClass_Pool1()
    {
        var obj = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        obj.Process();
        ObjectPoolTestClass2.Return(obj);
        return obj;
    }

    [Benchmark]
    public ObjectPoolTestClass2 TestClass_Pool8()
    {
        var obj1 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj2 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj3 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj4 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj5 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj6 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj7 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        var obj8 = ObjectPoolTestClass2.Rent(this.testClass.Object1, this.testClass.Object2, this.testClass.Object3, this.testClass.Object4);
        obj1.Process();
        obj2.Process();
        obj3.Process();
        obj4.Process();
        obj5.Process();
        obj6.Process();
        obj7.Process();
        obj8.Process();
        ObjectPoolTestClass2.Return(obj1);
        ObjectPoolTestClass2.Return(obj2);
        ObjectPoolTestClass2.Return(obj3);
        ObjectPoolTestClass2.Return(obj4);
        ObjectPoolTestClass2.Return(obj5);
        ObjectPoolTestClass2.Return(obj6);
        ObjectPoolTestClass2.Return(obj7);
        ObjectPoolTestClass2.Return(obj8);
        return obj1;
    }
}
