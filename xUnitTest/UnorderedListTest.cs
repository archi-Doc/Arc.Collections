// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Collections;
using Xunit;

namespace XunitTest;

#pragma warning disable xUnit2013
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection

public class UnorderedListTest
{
    [Fact]
    public void Add()
    {
        var list = new UnorderedList<int>();

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void ConstructorWithCapacity()
    {
        var list = new UnorderedList<int>(16);

        Assert.Equal(0, list.Count);
        Assert.Equal(16, list.Capacity);
    }

    [Fact]
    public void ConstructorFromCollection()
    {
        var list = new UnorderedList<int>([1, 2, 3, 4]);

        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
        Assert.Equal(4, list[3]);
    }

    [Fact]
    public void Indexer()
    {
        var list = new UnorderedList<int>([1, 2, 3]);

        list[1] = 10;

        Assert.Equal(1, list[0]);
        Assert.Equal(10, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void IndexerOutOfRange()
    {
        var list = new UnorderedList<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[3] = 1);
    }

    [Fact]
    public void Insert()
    {
        var list = new UnorderedList<int>([1, 3]);

        list.Insert(1, 2);
        list.Insert(0, 0);
        list.Insert(list.Count, 4);

        Assert.Equal([0, 1, 2, 3, 4], list);
    }

    [Fact]
    public void RemoveAt()
    {
        var list = new UnorderedList<int>([1, 2, 3, 4]);

        list.RemoveAt(1);

        Assert.Equal(3, list.Count);
        Assert.Equal([1, 3, 4], list);
    }

    [Fact]
    public void Remove()
    {
        var list = new UnorderedList<int>([1, 2, 3, 2]);

        Assert.True(list.Remove(2));

        Assert.Equal(3, list.Count);
        Assert.Equal([1, 3, 2], list);

        Assert.False(list.Remove(10));
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Clear()
    {
        var list = new UnorderedList<int>([1, 2, 3]);

        list.Clear();

        Assert.Equal(0, list.Count);
        Assert.Empty(list);
    }

    [Fact]
    public void ContainsAndIndexOf()
    {
        var list = new UnorderedList<string>(["A", "B", "C"]);

        Assert.True(list.Contains("B"));
        Assert.False(list.Contains("D"));

        Assert.Equal(1, list.IndexOf("B"));
        Assert.Equal(-1, list.IndexOf("D"));
    }

    [Fact]
    public void CopyTo()
    {
        var list = new UnorderedList<int>([1, 2, 3]);
        var array = new int[5];

        list.CopyTo(array, 1);

        Assert.Equal([0, 1, 2, 3, 0], array);
    }

    [Fact]
    public void CapacityGrows()
    {
        var list = new UnorderedList<int>();

        for (var i = 0; i < 100; i++)
        {
            list.Add(i);
        }

        Assert.Equal(100, list.Count);
        Assert.True(list.Capacity >= 100);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(i, list[i]);
        }
    }

    [Fact]
    public void CapacityCanBeChanged()
    {
        var list = new UnorderedList<int>([1, 2, 3]);

        list.Capacity = 16;

        Assert.Equal(16, list.Capacity);
        Assert.Equal([1, 2, 3], list);

        list.Capacity = 3;

        Assert.Equal(3, list.Capacity);
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void CapacityCannotBeSmallerThanCount()
    {
        var list = new UnorderedList<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 2);
    }

    [Fact]
    public void Enumerate()
    {
        var list = new UnorderedList<int>([1, 2, 3, 4]);

        var result = new List<int>();

        foreach (var x in list)
        {
            result.Add(x);
        }

        Assert.Equal([1, 2, 3, 4], result);
    }

    [Fact]
    public void EnumeratorDetectsAdd()
    {
        var list = new UnorderedList<int>([1, 2, 3]);
        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        list.Add(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void EnumeratorDetectsRemove()
    {
        var list = new UnorderedList<int>([1, 2, 3]);
        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        list.RemoveAt(0);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void EnumeratorDetectsIndexerChange()
    {
        var list = new UnorderedList<int>([1, 2, 3]);
        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        list[0] = 10;

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void EnumeratorCurrentThrowsBeforeStartAndAfterEnd()
    {
        var list = new UnorderedList<int>([1]);

        System.Collections.IEnumerator enumerator = list.GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        Assert.False(enumerator.MoveNext());

        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void ReferenceValuesAreReleasedOnClear()
    {
        var list = new UnorderedList<string>(["A", "B", "C"]);

        list.Clear();

        Assert.Empty(list);

        list.Add("D");

        Assert.Equal("D", list[0]);
    }

    [Fact]
    public void EmptyList()
    {
        var list = new UnorderedList<int>();

        Assert.Equal(0, list.Count);
        Assert.Empty(list);
        Assert.False(list.Contains(1));
        Assert.Equal(-1, list.IndexOf(1));
    }
}
