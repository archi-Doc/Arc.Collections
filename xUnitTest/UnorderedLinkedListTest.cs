using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest;

public class UnorderedLinkedListTest
{
    [Fact]
    public void Test1()
    {
        var l = new UnorderedLinkedList<int>();
        var array = new int[] { 1, 3, 2, 0, 5, -10, 0, };

        new LinkedList<int>(array).SequenceEqual(array).IsTrue();

        var l2 = new UnorderedLinkedList<int>(array);
        l2.SequenceEqual(array).IsTrue();

        foreach (var x in array)
        {
            l.AddLast(x);
        }
        l.SequenceEqual(array).IsTrue();
        l.Clear();

        foreach (var x in array.Reverse())
        {
            l.AddFirst(x);
        }
        l.SequenceEqual(array).IsTrue();
        l.Clear();

        foreach (var x in array)
        {
            l.AddLast(x);
        }

        var node = l.Find(0)!;
        node.IsNotNull();
        l.Remove(node);
        l.AddFirst(0);

        node = l.Find(5)!;
        node.IsNotNull();
        l.Remove(node);
        l.AddLast(5);

        l.SequenceEqual([0, 1, 3, 2, -10, 0, 5,]).IsTrue();

        node = l.Find(5)!;
        node.IsNotNull();
        l.MoveToFirst(node);
        l.SequenceEqual([5, 0, 1, 3, 2, -10, 0,]).IsTrue();

        node = l.Find(1)!;
        node.IsNotNull();
        l.MoveToLast(node);
        l.SequenceEqual([5, 0, 3, 2, -10, 0, 1,]).IsTrue();

        l.Clear();
        l.Count.Is(0);

        l.AddFirst(1);
        node = l.Find(1)!;
        node.IsNotNull();
        l.MoveToLast(node);
        l.SequenceEqual([1,]).IsTrue();
    }

    [Fact]
    public void Random()
    {
        var r = new Random(12);

        for (var n = 0; n < 10; n++)
        {
            RandomTest(r, -100, 100, 100, false);
            RandomTest(r, -100, 100, 100, true);
            RandomTest(r, -500, 500, 100, false);
            RandomTest(r, -600, 600, 1000, true);
        }
    }

    private void RandomTest(Random r, int start, int end, int count, bool duplicate)
    {
        IEnumerable<int> e;
        if (duplicate)
        {
            e = TestHelper.GetRandomNumbers(r, start, end, count);
        }
        else
        {
            e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);
        }

        var array = e.ToArray();
        var list = new List<int>(array);
        var l = new UnorderedLinkedList<int>(array);
        l.SequenceEqual(array).IsTrue();
        l.SequenceEqual(list).IsTrue();

        for (var n = 0; n < count / 4; n++)
        {
            var x = r.Next(start, end);
            var node = l.Find(x);
            if (node != null)
            {
                l.Remove(node);
                list.Remove(x);
            }
        }

        l.SequenceEqual(list).IsTrue();
    }
}
