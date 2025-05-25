// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
using Xunit;

namespace xUnitTest;

public class CountDecimalCharsTest
{
    [Fact]
    public void TestInt()
    {
        int x = 1;
        for (var i = 0; i < 10; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        x = -1;
        for (var i = 0; i < 10; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        Test(0);
        Test(int.MinValue);
        Test(int.MaxValue);

        void Test(int value)
        {
            var x = BaseHelper.CountDecimalChars(value);
            var y = CountDigits_TryFormat(value);
            x.Is(y);
        }
    }

    [Fact]
    public void TestUInt()
    {
        uint x = 1;
        for (var i = 0; i < 10; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        Test(uint.MinValue);
        Test(uint.MaxValue);

        void Test(uint value)
        {
            var x = BaseHelper.CountDecimalChars(value);
            var y = CountDigits_TryFormat(value);
            x.Is(y);
        }
    }

    [Fact]
    public void TestLong()
    {
        long x = 1;
        for (var i = 0; i < 19; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        x = -1;
        for (var i = 0; i < 19; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        Test(0);
        Test(long.MinValue);
        Test(long.MaxValue);

        void Test(long value)
        {
            var x = BaseHelper.CountDecimalChars(value);
            var y = CountDigits_TryFormat(value);
            x.Is(y);
        }
    }

    [Fact]
    public void TestULong()
    {
        ulong x = 1;
        for (var i = 0; i < 20; i++, x *= 10)
        {
            Test(x - 2);
            Test(x - 1);
            Test(x);
            Test(x + 1);
            Test(x + 2);
        }

        Test(ulong.MinValue);
        Test(ulong.MaxValue);

        void Test(ulong value)
        {
            var x = BaseHelper.CountDecimalChars(value);
            var y = CountDigits_TryFormat(value);
            x.Is(y);
        }
    }

    private static int CountDigits_TryFormat(int value)
    {
        Span<char> buffer = stackalloc char[11];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }

    private static int CountDigits_TryFormat(uint value)
    {
        Span<char> buffer = stackalloc char[11];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }

    private static int CountDigits_TryFormat(ulong value)
    {
        Span<char> buffer = stackalloc char[20];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }

    private static int CountDigits_TryFormat(long value)
    {
        Span<char> buffer = stackalloc char[20];
        value.TryFormat(buffer, out int charsWritten);
        return charsWritten;
    }
}
