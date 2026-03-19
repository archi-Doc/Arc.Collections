// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using Xunit;

namespace xUnitTest;

public class TemporaryListClass
{
    public int X { get; set; }

    public TemporaryListClass(int x)
    {
        this.X = x;
    }
}

public class TemporaryListTest
{
    private int[] referenceArray = [1, 3, 7, 11, 32, 47];

    [Fact]
    public void Test1()
    {
        for (var i = 0; i < this.referenceArray.Length; i++)
        {
            var sum = Sum(this.referenceArray.AsSpan(0, i));
            Test(i, sum);
        }
    }

    private void Test(int count, int sum)
    {
        var span = this.referenceArray.AsSpan(0, count);

        var list = new TemporaryList<int>();
        foreach (var x in span)
        {
            list.Add(x);
        }

        var y = 0;
        foreach (var x in list)
        {
            y += x;
        }

        y.Is(sum);

        var array = list.ToArray();
        array.SequenceEqual(span);

    }

    public static int Sum(ReadOnlySpan<int> span)
    {
        var sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }

        return sum;
    }
}
