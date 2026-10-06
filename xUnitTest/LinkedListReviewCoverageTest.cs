using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class LinkedListReviewCoverageTest
{
    [Fact]
    public void CollectionInterfacesCopyValuesAndValidateDestinations()
    {
        var list = new UnorderedLinkedList<int>();
        ICollection<int> generic = list;
        generic.Add(1);
        generic.Add(2);
        Assert.False(generic.IsReadOnly);
        ICollection collection = list;
        Assert.False(collection.IsSynchronized);
        Assert.Same(list, collection.SyncRoot);

        var values = new int[2];
        list.CopyTo(values);
        Assert.Equal(new[] { 1, 2 }, values);
        var destination = new int[4];
        collection.CopyTo(destination, 1);
        Assert.Equal(new[] { 0, 1, 2, 0 }, destination);
        var objects = new object[3];
        collection.CopyTo(objects, 1);
        Assert.Null(objects[0]);
        Assert.Equal(1, objects[1]);
        Assert.Equal(2, objects[2]);

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(values, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(values, 3));
        Assert.Throws<ArgumentException>(() => list.CopyTo(values, 1));
        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new int[2, 2], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(Array.CreateInstance(typeof(int), new[] { 3 }, new[] { 1 }), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(values, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(values, 3));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(values, 1));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new DateTime[2], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new string[2], 0));

        var empty = new UnorderedLinkedList<int>();
        ((ICollection)empty).CopyTo(values, values.Length);
        Assert.Equal(new[] { 1, 2 }, values);
    }

    [Fact]
    public void FindLastAndValueRemovalHandleDuplicatesAndNulls()
    {
        var list = new UnorderedLinkedList<string?>();
        Assert.Null(list.Find("missing"));
        Assert.Null(list.FindLast("missing"));
        var first = list.AddLast("repeat");
        var firstNull = list.AddLast((string?)null);
        var last = list.AddLast("repeat");
        var lastNull = list.AddLast((string?)null);
        list.AddLast("tail");

        Assert.Same(first, list.Find("repeat"));
        Assert.Same(last, list.FindLast("repeat"));
        Assert.Same(firstNull, list.Find(null));
        Assert.Same(lastNull, list.FindLast(null));
        Assert.Null(list.FindLast("missing"));
        Assert.False(list.Remove("missing"));
        Assert.True(list.Remove("repeat"));
        Assert.Null(first.List);
        Assert.Same(last, list.Find("repeat"));
        Assert.True(list.Remove((string?)null));
        Assert.Null(firstNull.List);
        Assert.Same(lastNull, list.FindLast(null));
        Assert.True(list.Remove((string?)null));
        Assert.Null(list.FindLast(null));
        Assert.Null(list.Find(null));
        Assert.False(list.Remove((string?)null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DetachedNodesCanBeAddedToEmptyAndNonemptyLists(bool atHead)
    {
        var donor = new UnorderedLinkedList<int>();
        var first = donor.AddLast(1);
        var second = donor.AddLast(2);
        donor.Clear();
        var list = new UnorderedLinkedList<int>();
        if (atHead)
        {
            list.AddFirst(first);
            list.AddFirst(second);
        }
        else
        {
            list.AddLast(first);
            list.AddLast(second);
        }

        Assert.Equal(atHead ? new[] { 2, 1 } : new[] { 1, 2 }, list.ToArray());
        Assert.Same(list, first.List);
        Assert.Same(list, second.List);
        Assert.Null(list.First!.Previous);
        Assert.Null(list.Last!.Next);
        Assert.Same(list.Last, list.First.Next);
        Assert.Same(list.First, list.Last.Previous);
        Assert.Empty(donor);
    }

    [Fact]
    public void DetachedNodesCanBeInsertedBeforeAndAfterExistingNodes()
    {
        var donor = new UnorderedLinkedList<int>();
        var one = donor.AddLast(1);
        var three = donor.AddLast(3);
        var four = donor.AddLast(4);
        donor.Clear();
        var list = new UnorderedLinkedList<int>();
        var two = list.AddLast(2);
        list.AddBefore(two, one);
        list.AddAfter(two, four);
        list.AddBefore(four, three);

        Assert.Equal(new[] { 1, 2, 3, 4 }, list.ToArray());
        Assert.Same(one, list.First);
        Assert.Same(four, list.Last);
        Assert.Same(three, two.Next);
        Assert.Same(two, three.Previous);
        Assert.Same(list, one.List);
        Assert.Same(list, three.List);
        Assert.Same(list, four.List);
    }

    [Fact]
    public void ForeignAndAttachedNodeArgumentsAreRejectedWithoutChangingEitherList()
    {
        var list = new UnorderedLinkedList<int>(new[] { 1, 2 });
        var other = new UnorderedLinkedList<int>(new[] { 3, 4 });
        var own = list.First!;
        var foreign = other.First!;
        var detached = other.AddLast(5);
        other.Remove(detached);
        var enumerator = list.GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => list.Remove(foreign));
        Assert.Throws<InvalidOperationException>(() => list.Remove(detached));
        Assert.Throws<InvalidOperationException>(() => list.MoveToFirst(foreign));
        Assert.Throws<InvalidOperationException>(() => list.MoveToLast(foreign));
        Assert.Throws<InvalidOperationException>(() => list.AddAfter(foreign, 5));
        Assert.Throws<InvalidOperationException>(() => list.AddBefore(foreign, 5));
        Assert.Throws<InvalidOperationException>(() => list.AddAfter(foreign, detached));
        Assert.Throws<InvalidOperationException>(() => list.AddBefore(foreign, detached));
        Assert.Throws<InvalidOperationException>(() => list.AddFirst(foreign));
        Assert.Throws<InvalidOperationException>(() => list.AddLast(own));
        Assert.Throws<InvalidOperationException>(() => list.AddAfter(own, foreign));
        Assert.Throws<InvalidOperationException>(() => list.AddBefore(own, own));
        Assert.Throws<ArgumentNullException>(() => list.Remove((UnorderedLinkedList<int>.Node)null!));
        Assert.Throws<ArgumentNullException>(() => list.AddFirst((UnorderedLinkedList<int>.Node)null!));
        Assert.Throws<ArgumentNullException>(() => list.AddLast((UnorderedLinkedList<int>.Node)null!));
        Assert.Throws<ArgumentNullException>(() => list.AddAfter(own, (UnorderedLinkedList<int>.Node)null!));
        Assert.Throws<ArgumentNullException>(() => list.AddBefore(own, (UnorderedLinkedList<int>.Node)null!));

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.Equal(new[] { 1, 2 }, list.ToArray());
        Assert.Equal(new[] { 3, 4 }, other.ToArray());
        Assert.Null(detached.List);
        Assert.Null(detached.Previous);
        Assert.Null(detached.Next);
    }

    [Fact]
    public void NonGenericEnumeratorChecksCurrentAndCanReset()
    {
        var list = new UnorderedLinkedList<int>(new[] { 1, 2 });
        var enumerator = ((IEnumerable)list).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current);
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void StructuralChangesInvalidateMoveNextAndReset(int mutation)
    {
        var list = new UnorderedLinkedList<int>(new[] { 1, 2, 3 });
        var enumerator = ((IEnumerable)list).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        switch (mutation)
        {
            case 0:
                list.Clear();
                break;
            case 1:
                list.RemoveLast();
                break;
            case 2:
                list.MoveToFirst(list.Last!);
                break;
            default:
                list.MoveToLast(list.First!);
                break;
        }

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
    }

    [Fact]
    public void ValueChangesAndNoOpMovesKeepEnumeratorsValid()
    {
        var list = new UnorderedLinkedList<int>(new[] { 1, 2 });
        var enumerator = list.GetEnumerator();
        list.MoveToFirst(list.First!);
        list.MoveToLast(list.Last!);
        list.First!.UnsafeChangeValue(10);
        list.Last!.ValueRef = 20;

        Assert.True(enumerator.MoveNext());
        Assert.Equal(10, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(20, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }
}
