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
        s.TryAdd(new(1));
        s.TryAdd(new(2));
        s.TryAdd(new(4));
        s.TryAdd(new(3));
        s.TryAdd(new(5));

        var array = s.ToArray().Select(x => x!.Id);
        array.SequenceEqual([1, 2, 4, 3]).IsTrue();

        array = ((IEnumerable<SlidingListClass>)s).ToArray().Select(x => x!.Id);
        array.SequenceEqual([1, 2, 4, 3]).IsTrue();

        s.Resize(2).IsFalse();
        s.Resize(5).IsTrue();
        s.TryAdd(new(5));
        s.ToArray().Select(x => x!.Id).SequenceEqual([1, 2, 4, 3, 5]).IsTrue();

        s.Remove(0).IsTrue();
        s.Remove(0).IsFalse();
        s.Remove(1).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([4, 3, 5]).IsTrue();

        s.TryAdd(new(10));
        s.TryAdd(new(20));
        s.ToArray().Select(x => x!.Id).SequenceEqual([4, 3, 5, 10, 20]).IsTrue();
        array = ((IEnumerable<SlidingListClass>)s).ToArray().Select(x => x!.Id);
        array.SequenceEqual([4, 3, 5, 10, 20]).IsTrue();

        s.Remove(3).IsTrue();
        s.Remove(4).IsTrue();
        s.Remove(2).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([10, 20]).IsTrue();

        s.TryAdd(new(11)).Is(7);
        s.TryAdd(new(22)).Is(8);
        s.TryAdd(new(33)).Is(9);
        s.Remove(5).IsTrue();
        s.ToArray().Select(x => x!.Id).SequenceEqual([20, 11, 22, 33]).IsTrue();
        s.Resize(4);
        s.ToArray().Select(x => x!.Id).SequenceEqual([20, 11, 22, 33]).IsTrue();

        s.StartPosition.Is(6);
        s.EndPosition.Is(10);
    }
}
