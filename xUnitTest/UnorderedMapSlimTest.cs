using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;

namespace xUnitTest;

public class UnorderedMapSlimTest
{
    [Fact]
    public void Test1()
    {
        var dic = new Dictionary<int, int>();
        var um = new UnorderedMapSlim<int, int>();

        um.TryGetValue(1, out var nn);

        AddAndValidate(0, 0);
        RemoveAndValidate(0);

        AddAndValidate(0, 0);
        AddAndValidate(1, 2);
        AddAndValidate(11, 12);
        AddAndValidate(2, 4);
        RemoveAndValidate(3);
        RemoveAndValidate(1);

        var r = new Random(12);

        Clear();

        void Clear()
        {
            dic.Clear();
            um.Clear();
        }

        void AddAndValidate(int x, int y)
        {
            dic.Add(x, y);
            um.Add(x, y);
            um.ValidateWithDictionary(dic);
        }

        void RemoveAndValidate(int x)
        {
            dic.Remove(x);
            um.Remove(x);
            um.ValidateWithDictionary(dic);
        }
    }

    [Fact]
    public void TestClass()
    {
        var um = new UnorderedMapSlim<UnorderedMapTestClass, int>();
        um.Add(new UnorderedMapTestClass(1), 1);
        um.Add(new UnorderedMapTestClass(2), 0);
        um.Add(new UnorderedMapTestClass(3), 3);
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
        var dic = new Dictionary<int, int>();
        var um = new UnorderedMapSlim<int, int>();
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
        foreach (var x in array)
        {
            dic[x] = x;
            um.Add(x, x);
        }

        um.ValidateWithDictionary(dic);

        // um.UnsafeValues.SequenceEqual(um.Values).IsTrue();

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
            dic.Remove(x);
            um.Remove(x);
        }

        um.ValidateWithDictionary(dic);

        if (branch != 1)
        {
            dic.Count.Is(0);
            um.Count.Is(0);
        }
    }

    [Fact]
    public void Random2()
    {
        var r = new Random(12);

        RandomTest2(r, 0, 100, 50, 1);
        RandomTest2(r, 0, 100, 50, 10);
        RandomTest2(r, -100, 100, 50, 10);
        RandomTest2(r, -1000, 1000, 50, 10);
        RandomTest2(r, -10000, 10000, 1000, 10);
        RandomTest2(r, int.MinValue, int.MaxValue, 1000, 10);
    }

    private void RandomTest2(Random r, int start, int end, int count, int repeat)
    {
        var dic = new Dictionary<int, int>();
        var um = new UnorderedMapSlim<int, int>();

        for (var n = 0; n < repeat; n++)
        {
            for (var m = 0; m < count; m++)
            {
                var x = r.Next(start, end);
                dic[x] = x;
                um[x] = x;
            }

            um.ValidateWithDictionary(dic);

            for (var m = 0; m < count; m++)
            {
                var x = r.Next(start, end);
                dic.Remove(x);
                um.Remove(x);
            }

            um.ValidateWithDictionary(dic);
        }
    }
}
