using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace xUnitTest;

public class SlidingListClass
{
    public SlidingListClass(int id)
    {
        this.Id = id;
    }

    public int Id { get; set; }

    public override string ToString()
        => this.Id.ToString();
}

public class SlidingListTest
{
    [Fact]
    public void Test1()
    {
        var s = new SlidingList<SlidingListClass>(4);
        s.Add(new(1));
        s.Add(new(2));
        s.Add(new(4));
        s.Add(new(3));
        s.Add(new(5));

        var array = s.ToArray().Select(x => x!.Id);
        array.SequenceEqual([1, 2, 4, 3]).IsTrue();

        array = ((IEnumerable<SlidingListClass>)s).ToArray().Select(x => x!.Id);
        array.SequenceEqual([1, 2, 4, 3]).IsTrue();

        s.Resize(2).IsFalse();
        s.Resize(5).IsTrue();
        s.Add(new(5));
        s.ToArray().Select(x => x!.Id).SequenceEqual([1, 2, 4, 3, 5]).IsTrue();

        s.Remove(0).IsTrue();
        s.Remove(0).IsFalse();
        s.Remove(1).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([4, 3, 5]).IsTrue();

        s.Add(new(10));
        s.Add(new(20));
        s.ToArray().Select(x => x!.Id).SequenceEqual([4, 3, 5, 10, 20]).IsTrue();
        array = ((IEnumerable<SlidingListClass>)s).ToArray().Select(x => x!.Id);
        array.SequenceEqual([4, 3, 5, 10, 20]).IsTrue();

        s.Remove(3).IsTrue();
        s.Remove(4).IsTrue();
        s.Remove(2).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([10, 20]).IsTrue();

        s.Add(new(11)).Is(7);
        s.Add(new(22)).Is(8);
        s.Add(new(33)).Is(9);
        s.Remove(5).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([20, 11, 22, 33]).IsTrue();
        s.Resize(4);
        s.ToArray().Select(x => x!.Id).SequenceEqual([20, 11, 22, 33]).IsTrue();

        s.StartPosition.Is(6);
        s.EndPosition.Is(10);

        var prop = s.GetType().GetField("itemsPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(s, int.MaxValue - 1);
        s.StartPosition.Is(int.MaxValue - 1);
        s.EndPosition.Is(2);

        s.Get(int.MaxValue - 1)!.Id.Is(20);
        s.Get(int.MaxValue)!.Id.Is(11);
        s.Get(0)!.Id.Is(22);
        s.Get(1)!.Id.Is(33);

        s.Resize(5);
        s.Add(new(99)).Is(2);

        s.Remove(int.MaxValue - 1).IsTrue();

        s.Get(int.MaxValue)!.Id.Is(11);
        s.Get(0)!.Id.Is(22);
        s.Get(1)!.Id.Is(33);
        s.Get(2)!.Id.Is(99);

        s.ToArray().Select(x => x!.Id).SequenceEqual([11, 22, 33, 99]).IsTrue();

        s.Set(0, new(23)).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([11, 23, 33, 99]).IsTrue();

        s.Add(new(0));
        s.ToArray().Select(x => x!.Id).SequenceEqual([11, 23, 33, 99, 0]).IsTrue();
    }

    [Fact]
    public void Test2()
    {
        var s = new SlidingList<SlidingListClass>(4);

        var array = s.ToArray().Select(x => x!.Id);
        array.SequenceEqual([]).IsTrue();
        foreach (var x in s)
        {
            var y = x.Id;
        }

        s.Add(new(1));
        foreach (var x in s)
        {
            x.Id.Is(1);
        }
    }

    [Fact]
    public void Test3()
    {
        var s = new SlidingList<SlidingListClass>(4);
        s.Add(new(1));
        s.Add(new(2));
        s.Add(new(3));
        s.Add(new(4));

        s.Remove(0).IsTrue();
        s.StartPosition.Is(1);
        s.Remove(1).IsTrue();
        s.StartPosition.Is(2);
        s.Remove(2).IsTrue();
        s.StartPosition.Is(3);
        s.Remove(3).IsTrue();
        s.StartPosition.Is(4);
        s.Consumed.Is(0);
    }

    [Fact]
    public void Test4()
    {
        var s = new SlidingList<SlidingListClass>(5);
        s.Add(new(1));
        s.Add(new(2));
        s.Add(new(3));
        s.Add(new(4));

        var array = s.ToArray().Select(x => x!.Id);
        array.SequenceEqual([1, 2, 3, 4]).IsTrue();

        s.Remove(2);
        s.ToArray().Select(x => x!.Id).SequenceEqual([1, 2, 4,]).IsTrue();

        s.Add(new(5));
        s.ToArray().Select(x => x!.Id).SequenceEqual([1, 2, 4, 5,]).IsTrue();
    }
}
