using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace XunitTest;

public record ObjectCacheClass(int Id, string Name);

public class ObjectCacheTest
{
    [Fact]
    public void Test1()
    {
        KeyedObjectCache<int, ObjectCacheClass> cache = new(4);

        cache.TakeOrDefault(0).IsNull();

        cache.TryAdd(1, new(1, "1"));
        var c1 = cache.TakeOrDefault(1)!;
        c1.Equals(new(1, "1")).IsTrue();
        cache.TakeOrDefault(1).IsNull();

        cache.TryAdd(1, c1);
        c1 = cache.TakeOrDefault(1)!;
        c1.Equals(new(1, "1")).IsTrue();

        cache.Count.Is(0);
        cache.TryAdd(1, c1);
        c1 = cache.TakeOrDefault(1);
        var v = cache.CreateLease(1, c1);
        v = v.Return();
        cache.Count.Is(1);

        c1 = cache.TakeOrDefault(1);
        using (var v2 = cache.CreateLease(1, c1))
        {
            cache.Count.Is(0);
        }

        cache.Count.Is(1);

        cache.TryAdd(1, new(1, "1"));
        cache.TryAdd(2, new(2, "2"));
        cache.TryAdd(3, new(3, "3"));
        cache.TryAdd(4, new(4, "4"));
        cache.TryAdd(5, new(5, "5"));
        cache.Count.Is(4);
        cache.TakeOrDefault(1).IsNull();
    }
}
