using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace xUnitTest;

public class OrderedMultiMapTest
{
    [Fact]
    public void Test1()
    {
        var list = new OrderedKeyValueList<int, int>();
        var mm = new OrderedMultiMap<int, int>();

        AddAndValidate(0, 0);
        AddAndValidate(1, 1);
        AddAndValidate(0, 2);

        RemoveAndValidate(0, 0);

        AddAndValidate(0, 0);

        RemoveAndValidate(0, 0);
        RemoveAndValidate(0, 2);

        AddAndValidate(0, 0);
        AddAndValidate(-1, 0);
        AddAndValidate(1, 2);

        mm.EnumerateNode(-99).Select(x => x.Value).SequenceEqual(new int[] { });
        mm.EnumerateNode(0).Select(x => x.Value).SequenceEqual(new int[] { 0, });
        mm.EnumerateNode(1).Select(x => x.Value).SequenceEqual(new int[] { 1, 2, });

        void AddAndValidate(int x, int y)
        {
            list.Add(x, y);
            mm.Add(x, y);
            mm.SequenceEqual(list).IsTrue();
            mm.SequenceEqual(TestHelper.ToReverseArray(mm)).IsTrue();
        }

        void RemoveAndValidate(int x, int y)
        {
            list.Remove(x, y);
            mm.Remove(x, y);
            mm.SequenceEqual(list).IsTrue();
            mm.SequenceEqual(TestHelper.ToReverseArray(mm)).IsTrue();
        }
    }

    [Fact]
    public void Random()
    {
        var r = new Random(12);

        for (var n = 0; n < 10; n++)
        {
            RandomTest(r, -100, 100, 100);
            RandomTestDup(r, -100, 100, 10);
            RandomTestDup(r, -100, 100, 100);
            RandomTestDup(r, -100, 100, 200);

            RandomTest(r, -1000, 1000, 1000);
            RandomTestDup(r, -1000, 1000, 100);
            RandomTestDup(r, -1000, 1000, 1000);
            RandomTestDup(r, -1000, 1000, 2000);
        }
    }

    private void RandomTest(Random r, int start, int end, int count)
    {
        var om = new OrderedMap<int, int>();
        var mm = new OrderedMultiMap<int, int>();
        IEnumerable<int> e;

        e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);

        var array = e.ToArray();
        foreach (var x in array)
        {
            om.Add(x, x);
            mm.Add(x, x);
        }

        mm.SequenceEqual(om).IsTrue();

        var branch = r.Next(3);
        if (branch == 1)
        {
            e = TestHelper.GetRandomNumbers(r, start, end, count);
            array = e.ToArray();
        }
        else
        {
            TestHelper.Shuffle(r, array);
        }

        foreach (var x in array)
        {
            om.Remove(x);
            mm.Remove(x);
        }

        mm.SequenceEqual(om).IsTrue();
        mm.SequenceEqual(TestHelper.ToReverseArray(mm)).IsTrue();
        if (branch != 1)
        {
            mm.Count.Is(0);
            om.Count.Is(0);
        }
    }

    private void RandomTestDup(Random r, int start, int end, int count)
    {
        var mm = new OrderedMultiMap<int, int>();
        var list = new OrderedKeyValueList<int, int>();
        IEnumerable<int> e;

        e = TestHelper.GetRandomNumbers(r, start, end, count);

        var array = e.ToArray();
        foreach (var x in array)
        {
            mm.Add(x, x);
            list.Add(x, x);
        }

        mm.SequenceEqual(list).IsTrue();
    }

    [Fact]
    public void BoundTest()
    {
        var array = new int[] { 1, 4, 2, 0, 6, -10, 0, 2, };
        var array2 = new int[] { -10, 0, 0, 1, 2, 2, 4, 6, };

        var map = new OrderedMultiMap<int, int>();
        for (var i = 0; i < array.Length; i++)
        {
            map.Add(array[i], i);
        }

        map.Keys.SequenceEqual(array2).IsTrue();

        var node = map.First;
        var j = 0;
        while (node != null)
        {
            map.SetNodeValue(node, j++);
            node = node.Next;
        }

        map.GetLowerBound(-11)!.Value.Is(0);
        map.GetLowerBound(-10)!.Value.Is(0);
        map.GetLowerBound(-9)!.Value.Is(1);
        map.GetLowerBound(0)!.Value.Is(1);
        map.GetLowerBound(1)!.Value.Is(3);
        map.GetLowerBound(2)!.Value.Is(4);
        map.GetLowerBound(3)!.Value.Is(6);
        map.GetLowerBound(4)!.Value.Is(6);
        map.GetLowerBound(5)!.Value.Is(7);
        map.GetLowerBound(6)!.Value.Is(7);
        map.GetLowerBound(7).IsNull();

        map.GetUpperBound(-11).IsNull();
        map.GetUpperBound(-10)!.Value.Is(0);
        map.GetUpperBound(-9)!.Value.Is(0);
        map.GetUpperBound(0)!.Value.Is(2);
        map.GetUpperBound(1)!.Value.Is(3);
        map.GetUpperBound(2)!.Value.Is(5);
        map.GetUpperBound(3)!.Value.Is(5);
        map.GetUpperBound(4)!.Value.Is(6);
        map.GetUpperBound(5)!.Value.Is(6);
        map.GetUpperBound(6)!.Value.Is(7);
        map.GetUpperBound(7)!.Value.Is(7);
    }

    [Fact]
    public void UnsafePresearchTest()
    {
        var r = new Random(12);
        var n = 1000;

        var array = new int[n];
        var mm = new OrderedMultiMap<Identifier, int>();
        mm.UnsafePresearchForStructKey = true;
        var mm2 = new OrderedMultiMap<Identifier, int>(true);
        mm2.UnsafePresearchForStructKey = true;

        var i = 0;
        for (i = 0; i < n; i++)
        {
            var v = r.Next();
            array[i] = v;
            mm.Add(new(v), v);
            mm2.Add(new(v), v);
        }

        Array.Sort(array);

        i = 0;
        foreach (var x in mm)
        {
            x.Value.Equals(array[i]).IsTrue();
            x.Key.IsStructuralEqual(new Identifier(array[i]));
            i++;
        }

        Array.Reverse(array);

        /*i = 0;
        foreach (var x in mm2)
        {
            x.Value.Equals(array[i]).IsTrue();
            x.Key.IsStructuralEqual(new Identifier(array[i]));
            i++;
        }*/
    }
}
