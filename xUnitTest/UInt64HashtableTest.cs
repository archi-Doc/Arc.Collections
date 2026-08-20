// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class UInt64HashtableTest
{
    [Fact]
    public void AddAndTryGetValue()
    {
        var table = new UInt64Hashtable<string>();

        table.Add(1, "A");
        table.Add(2, "B");
        table.Add(3, "C");

        Assert.Equal(3, table.Count);

        Assert.True(table.TryGetValue(1, out var a));
        Assert.True(table.TryGetValue(2, out var b));
        Assert.True(table.TryGetValue(3, out var c));

        Assert.Equal("A", a);
        Assert.Equal("B", b);
        Assert.Equal("C", c);

        Assert.False(table.TryGetValue(4, out _));
    }

    [Fact]
    public void AddUpdatesExistingValue()
    {
        var table = new UInt64Hashtable<string>();

        table.Add(1, "A");
        table.Add(1, "B");

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue(1, out var value));
        Assert.Equal("B", value);
    }

    [Fact]
    public void TryAddDoesNotOverwriteExistingValue()
    {
        var table = new UInt64Hashtable<string>();

        Assert.True(table.TryAdd(1, "A"));
        Assert.False(table.TryAdd(1, "B"));

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue(1, out var value));
        Assert.Equal("A", value);
    }

    [Fact]
    public void GetOrAdd()
    {
        var table = new UInt64Hashtable<string>();

        var created = table.GetOrAdd(1, static key => $"Value-{key}");
        var existing = table.GetOrAdd(1, static _ => "Other");

        Assert.Equal("Value-1", created);
        Assert.Equal("Value-1", existing);
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void GetOrAddDoesNotCallFactoryForExistingKey()
    {
        var table = new UInt64Hashtable<int>();

        table.Add(1, 10);

        var called = false;
        var value = table.GetOrAdd(1, _ =>
        {
            called = true;
            return 20;
        });

        Assert.False(called);
        Assert.Equal(10, value);
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void Clear()
    {
        var table = new UInt64Hashtable<string>();

        table.Add(1, "A");
        table.Add(2, "B");
        table.Add(3, "C");

        table.Clear();

        Assert.Equal(0, table.Count);
        Assert.False(table.TryGetValue(1, out _));
        Assert.False(table.TryGetValue(2, out _));
        Assert.False(table.TryGetValue(3, out _));

        // The table remains usable after Clear.
        table.Add(4, "D");

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue(4, out var value));
        Assert.Equal("D", value);
    }

    [Fact]
    public void ToArray()
    {
        var table = new UInt64Hashtable<int>();

        table.Add(1, 10);
        table.Add(2, 20);
        table.Add(3, 30);
        table.Add(4, 40);

        var values = table.ToArray();
        Array.Sort(values);

        Assert.Equal([10, 20, 30, 40], values);
    }

    [Fact]
    public void CollisionChain()
    {
        var table = new UInt64Hashtable<string>(4);

        // These keys have the same ulong.GetHashCode().
        const ulong key1 = 1;
        const ulong key2 = 0x0000000100000000UL;

        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());

        table.Add(key1, "A");
        table.Add(key2, "B");

        Assert.Equal(2, table.Count);

        Assert.True(table.TryGetValue(key1, out var value1));
        Assert.True(table.TryGetValue(key2, out var value2));

        Assert.Equal("A", value1);
        Assert.Equal("B", value2);

        var values = table.ToArray();
        Array.Sort(values);

        Assert.Equal(["A", "B"], values);
    }

    [Fact]
    public void ResizePreservesItems()
    {
        var table = new UInt64Hashtable<int>(4);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            table.Add((ulong)i, i * 10);
        }

        Assert.Equal(count, table.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(table.TryGetValue((ulong)i, out var value));
            Assert.Equal(i * 10, value);
        }
    }

    [Fact]
    public void SupportsFullUInt64Key()
    {
        var table = new UInt64Hashtable<string>();

        table.Add(0, "Zero");
        table.Add(ulong.MaxValue, "Max");
        table.Add(0x123456789ABCDEF0UL, "Value");

        Assert.True(table.TryGetValue(0, out var zero));
        Assert.True(table.TryGetValue(ulong.MaxValue, out var max));
        Assert.True(table.TryGetValue(0x123456789ABCDEF0UL, out var value));

        Assert.Equal("Zero", zero);
        Assert.Equal("Max", max);
        Assert.Equal("Value", value);
    }

    [Fact]
    public void ConcurrentTryAddSameKey()
    {
        var table = new UInt64Hashtable<int>();

        var added = 0;

        Parallel.For(0, 100, i =>
        {
            if (table.TryAdd(123, i))
            {
                Interlocked.Increment(ref added);
            }
        });

        Assert.Equal(1, added);
        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue(123, out _));
    }

    [Fact]
    public void ConcurrentAddDifferentKeys()
    {
        var table = new UInt64Hashtable<int>();

        const int count = 1000;

        Parallel.For(0, count, i =>
        {
            table.Add((ulong)i, i);
        });

        Assert.Equal(count, table.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(table.TryGetValue((ulong)i, out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void ConcurrentRead()
    {
        var table = new UInt64Hashtable<int>();

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            table.Add((ulong)i, i);
        }

        Parallel.For(0, count, i =>
        {
            Assert.True(table.TryGetValue((ulong)i, out var value));
            Assert.Equal(i, value);
        });
    }
}
