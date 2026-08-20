// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class Utf16HashtableTest
{
    [Fact]
    public void AddAndTryGetValue()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 1);
        table.Add("B", 2);
        table.Add("C", 3);

        Assert.Equal(3, table.Count);

        Assert.True(table.TryGetValue("A", out var a));
        Assert.True(table.TryGetValue("B", out var b));
        Assert.True(table.TryGetValue("C", out var c));

        Assert.Equal(1, a);
        Assert.Equal(2, b);
        Assert.Equal(3, c);

        Assert.False(table.TryGetValue("D", out _));
    }

    [Fact]
    public void AddUpdatesExistingValue()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 1);
        table.Add("A", 2);

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue("A", out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void TryAddDoesNotOverwrite()
    {
        var table = new Utf16Hashtable<int>();

        Assert.True(table.TryAdd("A", 1));
        Assert.False(table.TryAdd("A", 2));

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue("A", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void ReadOnlySpanKey()
    {
        var table = new Utf16Hashtable<int>();

        ReadOnlySpan<char> key = "Alpha";

        Assert.True(table.TryAdd(key, 123));
        Assert.True(table.TryGetValue("Alpha".AsSpan(), out var value));

        Assert.Equal(123, value);
    }

    [Fact]
    public void SpanAndStringKeysAreEquivalent()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("Alpha", 1);

        Assert.False(table.TryAdd("Alpha".AsSpan(), 2));
        Assert.Equal(1, table.Count);

        Assert.True(table.TryGetValue("Alpha".AsSpan(), out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void GetOrAdd()
    {
        var table = new Utf16Hashtable<int>();

        var first = table.GetOrAdd("A", static _ => 10);
        var second = table.GetOrAdd("A", static _ => 20);

        Assert.Equal(10, first);
        Assert.Equal(10, second);
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void GetOrAddDoesNotCallFactoryForExistingKey()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 10);

        var called = false;

        var value = table.GetOrAdd("A", _ =>
        {
            called = true;
            return 20;
        });

        Assert.False(called);
        Assert.Equal(10, value);
    }

    [Fact]
    public void GetOrAddWithSpan()
    {
        var table = new Utf16Hashtable<int>();

        ReadOnlySpan<char> key = "Alpha";

        var value = table.GetOrAdd(key, static x => x.Length);

        Assert.Equal(5, value);
        Assert.Equal(1, table.Count);

        Assert.True(table.TryGetValue("Alpha", out var stored));
        Assert.Equal(5, stored);
    }

    [Fact]
    public void Clear()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 1);
        table.Add("B", 2);
        table.Add("C", 3);

        table.Clear();

        Assert.Equal(0, table.Count);

        Assert.False(table.TryGetValue("A", out _));
        Assert.False(table.TryGetValue("B", out _));
        Assert.False(table.TryGetValue("C", out _));

        table.Add("D", 4);

        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue("D", out var value));
        Assert.Equal(4, value);
    }

    [Fact]
    public void ToArray()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 10);
        table.Add("B", 20);
        table.Add("C", 30);

        var values = table.ToArray();
        Array.Sort(values);

        Assert.Equal([10, 20, 30], values);
    }

    [Fact]
    public void ToKeyValuePairs()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("A", 1);
        table.Add("B", 2);
        table.Add("C", 3);

        var pairs = table.ToKeyValuePairs();
        Array.Sort(pairs, static (x, y) => string.CompareOrdinal(x.Key, y.Key));

        Assert.Equal(3, pairs.Length);

        Assert.Equal("A", pairs[0].Key);
        Assert.Equal(1, pairs[0].Value);

        Assert.Equal("B", pairs[1].Key);
        Assert.Equal(2, pairs[1].Value);

        Assert.Equal("C", pairs[2].Key);
        Assert.Equal(3, pairs[2].Value);
    }

    [Fact]
    public void ResizePreservesItems()
    {
        var table = new Utf16Hashtable<int>(4);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            table.Add($"Key-{i}", i);
        }

        Assert.Equal(count, table.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(table.TryGetValue($"Key-{i}", out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void EmptyKey()
    {
        var table = new Utf16Hashtable<int>();

        table.Add(string.Empty, 123);

        Assert.True(table.TryGetValue(string.Empty, out var value1));
        Assert.True(table.TryGetValue(ReadOnlySpan<char>.Empty, out var value2));

        Assert.Equal(123, value1);
        Assert.Equal(123, value2);
    }

    [Fact]
    public void CaseSensitive()
    {
        var table = new Utf16Hashtable<int>();

        table.Add("ABC", 1);
        table.Add("abc", 2);

        Assert.Equal(2, table.Count);

        Assert.True(table.TryGetValue("ABC", out var upper));
        Assert.True(table.TryGetValue("abc", out var lower));

        Assert.Equal(1, upper);
        Assert.Equal(2, lower);
    }

    [Fact]
    public void ConcurrentTryAddSameKey()
    {
        var table = new Utf16Hashtable<int>();
        var added = 0;

        Parallel.For(0, 100, i =>
        {
            if (table.TryAdd("SameKey", i))
            {
                Interlocked.Increment(ref added);
            }
        });

        Assert.Equal(1, added);
        Assert.Equal(1, table.Count);
        Assert.True(table.TryGetValue("SameKey", out _));
    }

    [Fact]
    public void ConcurrentAddDifferentKeys()
    {
        var table = new Utf16Hashtable<int>();

        const int count = 1000;

        Parallel.For(0, count, i =>
        {
            table.Add($"Key-{i}", i);
        });

        Assert.Equal(count, table.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(table.TryGetValue($"Key-{i}", out var value));
            Assert.Equal(i, value);
        }
    }

    [Fact]
    public void ConcurrentRead()
    {
        var table = new Utf16Hashtable<int>();

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            table.Add($"Key-{i}", i);
        }

        Parallel.For(0, count, i =>
        {
            Assert.True(table.TryGetValue($"Key-{i}", out var value));
            Assert.Equal(i, value);
        });
    }
}
