using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace xUnitTest;

public record ObjectCacheClass(int Id, string Name);

public class ObjectCacheTest
{
    [Fact]
    public void Test1()
    {
        ObjectCache<int, ObjectCacheClass> cache = new(4);

        cache.TryGet(0).IsNull();

        cache.Cache(1, new(1, "1"));
        var c1 = cache.TryGet(1)!;
        c1.Equals(new(1, "1")).IsTrue();
        cache.TryGet(1).IsNull();

        cache.Cache(1, c1);
        c1 = cache.TryGet(1)!;
        c1.Equals(new(1, "1")).IsTrue();

        cache.Count.Is(0);
        cache.Cache(1, c1);
        c1 = cache.TryGet(1);
        var v = cache.CreateInterface(1, c1);
        v = v.Return();
        cache.Count.Is(1);

        c1 = cache.TryGet(1);
        using (var v2 = cache.CreateInterface(1, c1))
        {
            cache.Count.Is(0);
        }

        cache.Count.Is(1);

        cache.Cache(1, new(1, "1"));
        cache.Cache(2, new(2, "2"));
        cache.Cache(3, new(3, "3"));
        cache.Cache(4, new(4, "4"));
        cache.Cache(5, new(5, "5"));
        cache.Count.Is(4);
        cache.TryGet(1).IsNull();
    }
}
