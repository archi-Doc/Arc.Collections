// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class UnorderedSetTest
{
    [Fact]
    public void AddAndContains()
    {
        var set = new UnorderedSet<int>();

        Assert.True(set.Add(1));
        Assert.True(set.Add(2));
        Assert.True(set.Add(3));

        Assert.Equal(3, set.Count);

        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
        Assert.True(set.Contains(3));
        Assert.False(set.Contains(4));
    }

    [Fact]
    public void DuplicateIsRejectedByDefault()
    {
        var set = new UnorderedSet<int>();

        Assert.True(set.Add(1));
        Assert.False(set.Add(1));
        Assert.False(set.Add(1));

        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void DuplicateCanBeAllowed()
    {
        var set = new UnorderedSet<int>(allowDuplicate: true);

        Assert.True(set.Add(1));
        Assert.True(set.Add(1));
        Assert.True(set.Add(1));

        Assert.Equal(3, set.Count);

        var values = set.OrderBy(x => x).ToArray();

        Assert.Equal([1, 1, 1], values);
    }

    [Fact]
    public void Remove()
    {
        var set = new UnorderedSet<int>();

        set.Add(1);
        set.Add(2);

        Assert.True(set.Remove(1));
        Assert.False(set.Remove(1));

        Assert.Equal(1, set.Count);
        Assert.False(set.Contains(1));
        Assert.True(set.Contains(2));
    }

    [Fact]
    public void RemoveOnlyOneDuplicate()
    {
        var set = new UnorderedSet<int>(allowDuplicate: true);

        set.Add(1);
        set.Add(1);
        set.Add(1);

        Assert.True(set.Remove(1));

        Assert.Equal(2, set.Count);
        Assert.True(set.Contains(1));
    }

    [Fact]
    public void NullElement()
    {
        var set = new UnorderedSet<string?>();

        Assert.True(set.Add(null));
        Assert.True(set.Add("A"));

        Assert.True(set.Contains(null));
        Assert.True(set.Contains("A"));

        Assert.False(set.Add(null));
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void NullDuplicates()
    {
        var set = new UnorderedSet<string?>(allowDuplicate: true);

        Assert.True(set.Add(null));
        Assert.True(set.Add(null));
        Assert.True(set.Add(null));

        Assert.Equal(3, set.Count);
    }

    [Fact]
    public void CustomComparer()
    {
        var set = new UnorderedSet<string>(
            StringComparer.OrdinalIgnoreCase);

        Assert.True(set.Add("ABC"));
        Assert.False(set.Add("abc"));

        Assert.True(set.Contains("AbC"));
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void ConstructorFromCollection()
    {
        var set = new UnorderedSet<int>(new[] { 1, 2, 3, 2, 1 });

        Assert.Equal(3, set.Count);

        Assert.True(set.Contains(1));
        Assert.True(set.Contains(2));
        Assert.True(set.Contains(3));
    }

    [Fact]
    public void ConstructorFromCollectionAllowsDuplicates()
    {
        var set = new UnorderedSet<int>(
            new[] { 1, 2, 2, 3, 3, 3 },
            allowDuplicate: true);

        Assert.Equal(6, set.Count);

        Assert.Equal(
            [1, 2, 2, 3, 3, 3],
            set.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void Clear()
    {
        var set = new UnorderedSet<int>();

        set.Add(1);
        set.Add(2);
        set.Add(3);

        set.Clear();

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(1));
        Assert.False(set.Contains(2));
        Assert.False(set.Contains(3));

        Assert.True(set.Add(10));
        Assert.True(set.Contains(10));
    }

    [Fact]
    public void ToArray()
    {
        var set = new UnorderedSet<int>();

        set.Add(3);
        set.Add(1);
        set.Add(2);

        var array = set.ToArray();
        Array.Sort(array);

        Assert.Equal([1, 2, 3], array);
    }

    [Fact]
    public void CopyTo()
    {
        var set = new UnorderedSet<int>();

        set.Add(1);
        set.Add(2);
        set.Add(3);

        var array = new int[5];

        set.CopyTo(array, 1);

        Array.Sort(array, 1, 3);

        Assert.Equal(0, array[0]);
        Assert.Equal(1, array[1]);
        Assert.Equal(2, array[2]);
        Assert.Equal(3, array[3]);
        Assert.Equal(0, array[4]);
    }

    [Fact]
    public void CopyToThrowsWhenDestinationIsTooSmall()
    {
        var set = new UnorderedSet<int>();

        set.Add(1);
        set.Add(2);
        set.Add(3);

        var array = new int[2];

        Assert.Throws<ArgumentException>(() => set.CopyTo(array, 0));
    }

    [Fact]
    public void ResizePreservesElements()
    {
        var set = new UnorderedSet<int>(4);

        const int count = 1000;

        for (var i = 0; i < count; i++)
        {
            Assert.True(set.Add(i));
        }

        Assert.Equal(count, set.Count);

        for (var i = 0; i < count; i++)
        {
            Assert.True(set.Contains(i));
        }
    }

    [Fact]
    public void Foreach()
    {
        var set = new UnorderedSet<int>();

        set.Add(5);
        set.Add(1);
        set.Add(3);

        var values = new List<int>();

        foreach (var item in set)
        {
            values.Add(item);
        }

        values.Sort();

        Assert.Equal([1, 3, 5], values);
    }

    [Fact]
    public void Linq()
    {
        var set = new UnorderedSet<int>();

        set.Add(5);
        set.Add(1);
        set.Add(3);

        Assert.Equal(
            [1, 3, 5],
            set.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void EnumeratorDetectsModification()
    {
        var set = new UnorderedSet<int>();

        set.Add(1);
        set.Add(2);

        var enumerator = set.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        set.Add(3);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
}
