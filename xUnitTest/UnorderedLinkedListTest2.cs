// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

#pragma warning disable xUnit2013
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection

public class UnorderedLinkedListTest2
{
    [Fact]
    public void AddLast()
    {
        var list = new UnorderedLinkedList<int>();

        var n1 = list.AddLast(1);
        var n2 = list.AddLast(2);
        var n3 = list.AddLast(3);

        Assert.Equal(3, list.Count);
        Assert.Same(n1, list.First);
        Assert.Same(n3, list.Last);

        Assert.Null(n1.Previous);
        Assert.Same(n2, n1.Next);

        Assert.Same(n1, n2.Previous);
        Assert.Same(n3, n2.Next);

        Assert.Same(n2, n3.Previous);
        Assert.Null(n3.Next);
    }

    [Fact]
    public void AddFirst()
    {
        var list = new UnorderedLinkedList<int>();

        list.AddFirst(1);
        list.AddFirst(2);
        list.AddFirst(3);

        Assert.Equal([3, 2, 1], list.ToArray());
        Assert.Equal(3, list.First!.Value);
        Assert.Equal(1, list.Last!.Value);
    }

    [Fact]
    public void AddBeforeAndAfter()
    {
        var list = new UnorderedLinkedList<int>();

        var n2 = list.AddLast(2);
        var n1 = list.AddBefore(n2, 1);
        var n3 = list.AddAfter(n2, 3);

        Assert.Equal([1, 2, 3], list.ToArray());

        Assert.Same(n1, list.First);
        Assert.Same(n3, list.Last);
    }

    [Fact]
    public void Remove()
    {
        var list = new UnorderedLinkedList<int>();

        var n1 = list.AddLast(1);
        var n2 = list.AddLast(2);
        var n3 = list.AddLast(3);

        list.Remove(n2);

        Assert.Equal(2, list.Count);
        Assert.Equal([1, 3], list.ToArray());

        Assert.Null(n2.List);
        Assert.Null(n2.Previous);
        Assert.Null(n2.Next);

        Assert.Same(n1, list.First);
        Assert.Same(n3, list.Last);
    }

    [Fact]
    public void RemoveFirstAndLast()
    {
        var list = new UnorderedLinkedList<int>();

        list.AddLast(1);
        list.AddLast(2);
        list.AddLast(3);

        list.RemoveFirst();

        Assert.Equal([2, 3], list.ToArray());

        list.RemoveLast();

        Assert.Equal([2], list.ToArray());

        list.RemoveFirst();

        Assert.Empty(list);
        Assert.Null(list.First);
        Assert.Null(list.Last);
    }

    [Fact]
    public void MoveToFirst()
    {
        var list = new UnorderedLinkedList<int>();

        var n1 = list.AddLast(1);
        var n2 = list.AddLast(2);
        var n3 = list.AddLast(3);

        list.MoveToFirst(n3);

        Assert.Equal([3, 1, 2], list.ToArray());
        Assert.Same(n3, list.First);
        Assert.Same(n2, list.Last);

        list.MoveToFirst(n2);

        Assert.Equal([2, 3, 1], list.ToArray());

        Assert.Same(n1, list.Last);
    }

    [Fact]
    public void MoveToLast()
    {
        var list = new UnorderedLinkedList<int>();

        var n1 = list.AddLast(1);
        var n2 = list.AddLast(2);
        var n3 = list.AddLast(3);

        list.MoveToLast(n1);

        Assert.Equal([2, 3, 1], list.ToArray());
        Assert.Same(n2, list.First);
        Assert.Same(n1, list.Last);

        list.MoveToLast(n2);

        Assert.Equal([3, 1, 2], list.ToArray());
        Assert.Same(n3, list.First);
        Assert.Same(n2, list.Last);
    }

    [Fact]
    public void FindAndContains()
    {
        var list = new UnorderedLinkedList<string?>();

        list.AddLast("A");
        var b = list.AddLast("B");
        var n = list.AddLast((string?)null);

        Assert.True(list.Contains("A"));
        Assert.True(list.Contains("B"));
        Assert.True(list.Contains(null));
        Assert.False(list.Contains("C"));

        Assert.Same(b, list.Find("B"));
        Assert.Same(n, list.Find(null));
        Assert.Null(list.Find("C"));
    }

    [Fact]
    public void Clear()
    {
        var list = new UnorderedLinkedList<int>();

        var n1 = list.AddLast(1);
        var n2 = list.AddLast(2);
        var n3 = list.AddLast(3);

        list.Clear();

        Assert.Equal(0, list.Count);
        Assert.Null(list.First);
        Assert.Null(list.Last);

        Assert.Null(n1.List);
        Assert.Null(n2.List);
        Assert.Null(n3.List);

        // Detached nodes retain their values.
        Assert.Equal(1, n1.Value);
        Assert.Equal(2, n2.Value);
        Assert.Equal(3, n3.Value);
    }

    [Fact]
    public void CopyTo()
    {
        var list = new UnorderedLinkedList<int>([1, 2, 3,]);
        var array = new int[5];

        list.CopyTo(array, 1);

        Assert.Equal([0, 1, 2, 3, 0], array);
    }

    [Fact]
    public void Enumerate()
    {
        var list = new UnorderedLinkedList<int>([1, 2, 3,]);

        var values = new List<int>();

        foreach (var x in list)
        {
            values.Add(x);
        }

        Assert.Equal([1, 2, 3], values);
    }

    [Fact]
    public void EnumeratorDetectsModification()
    {
        var list = new UnorderedLinkedList<int>([1, 2, 3,]);
        var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        list.AddLast(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void ValueRef()
    {
        var list = new UnorderedLinkedList<int>();
        var node = list.AddLast(10);

        node.ValueRef = 20;

        Assert.Equal(20, node.Value);
        Assert.Equal(20, list.First!.Value);
    }

    [Fact]
    public void ConstructorFromCollection()
    {
        var list = new UnorderedLinkedList<int>([1, 2, 3, 4]);

        Assert.Equal(4, list.Count);
        Assert.Equal([1, 2, 3, 4], list.ToArray());
    }

    [Fact]
    public void EmptyListThrowsOnRemoveFirstAndLast()
    {
        var list = new UnorderedLinkedList<int>();

        Assert.Throws<InvalidOperationException>(() => list.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => list.RemoveLast());
    }

    [Fact]
    public void CannotRemoveNodeFromAnotherList()
    {
        var list1 = new UnorderedLinkedList<int>();
        var list2 = new UnorderedLinkedList<int>();

        var node = list1.AddLast(1);

        Assert.Throws<InvalidOperationException>(() => list2.Remove(node));
    }
}
