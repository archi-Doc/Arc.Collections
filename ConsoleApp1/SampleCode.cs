// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;

namespace ConsoleApp1;

internal class ObjectPoolSmall
{// Small class used frequently.
    internal static ObjectPool<ObjectPoolSmall> ObjectPool { get; } = new(() => new ObjectPoolSmall(), 10);

    internal static void Test()
    {
        var c = ObjectPool.Get();
        c.Process();
        ObjectPool.Return(c);
    }

    internal void Process()
    {
        Console.WriteLine("ObjectPoolSmall");
    }
}

internal class ObjectPoolSlow
{// Class that takes time to create an instance.
    internal static ObjectPool<ObjectPoolSlow> ObjectPool { get; } = new(() => new ObjectPoolSlow(), 10, true);

    internal static void Test()
    {
        var pool = ObjectPool; // Prepare

        var c = new ObjectPoolSlow(); // Slow
        c.Process("new()");

        c = pool.Get(); // Fast
        c.Process("ObjectPool");
        pool.Return(c);

        c = new ObjectPoolSlow(); // Slow
        c.Process("new()");

        c = pool.Get(); // Fast
        c.Process("ObjectPool");
        pool.Return(c);
    }

    internal ObjectPoolSlow()
    {
        Thread.Sleep(1000);
    }

    internal void Process(string text)
    {
        Console.WriteLine($"ObjectPoolSlow: {text}");
    }
}
