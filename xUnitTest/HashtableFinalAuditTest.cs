// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class HashtableFinalAuditTest
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 16)]
    [InlineData(true, 16)]
    [InlineData(false, 240)]
    [InlineData(true, 240)]
    [InlineData(false, 1025)]
    [InlineData(true, 1025)]
    [InlineData(false, 8192)]
    [InlineData(true, 8192)]
    public void UtfGetOrAddPreservesKeysAndNullValues(bool useSpan, int length)
    {
        var key = new string('\u65e5', length);
        var bytes = Encoding.UTF8.GetBytes(key);
        var utf16 = new Utf16Hashtable<string?>();
        var utf8 = new Utf8Hashtable<string?>();
        var calls = 0;
        string? StringFactory(string storedKey)
        {
            Assert.Equal(key, storedKey);
            calls++;
            return null;
        }

        string? ByteFactory(byte[] storedKey)
        {
            Assert.Equal(bytes, storedKey);
            calls++;
            return null;
        }

        Assert.Null(useSpan ? utf16.GetOrAdd(key.AsSpan(), StringFactory) : utf16.GetOrAdd(key, StringFactory));
        Assert.Null(useSpan ? utf8.GetOrAdd(bytes.AsSpan(), ByteFactory) : utf8.GetOrAdd(bytes, ByteFactory));

        // Look up equal content in distinct buffers and in slices with unrelated surrounding data.
        var paddedString = "[" + key + "]";
        var paddedBytes = Encoding.UTF8.GetBytes(paddedString);
        Assert.Null(utf16.GetOrAdd(new string(key.AsSpan()), StringFactory));
        Assert.Null(utf16.GetOrAdd(paddedString.AsSpan(1, key.Length), StringFactory));
        Assert.Null(utf8.GetOrAdd(bytes.AsSpan().ToArray(), ByteFactory));
        Assert.Null(utf8.GetOrAdd(paddedBytes.AsSpan(1, bytes.Length), ByteFactory));
        Assert.Equal(2, calls);
        Assert.True(utf16.TryGetValue(key, out var textValue));
        Assert.Null(textValue);
        Assert.True(utf8.TryGetValue(bytes, out var byteValue));
        Assert.Null(byteValue);
        Assert.Equal(1, utf16.Count);
        Assert.Equal(1, utf8.Count);
        Assert.Equal(key, Assert.Single(utf16.ToKeyValuePairs()).Key);
        var storedBytes = Assert.Single(utf8.ToKeyValuePairs()).Key;
        Assert.Equal(bytes, storedBytes);
        if (!useSpan)
        {
            Assert.Same(bytes, storedBytes);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThrowingUtfFactoriesLeaveTheirKeysAbsentAndAllowRetry(bool useSpan)
    {
        var key = new string('k', 4096);
        var bytes = Encoding.UTF8.GetBytes(key);
        var utf16 = new Utf16Hashtable<int>();
        var utf8 = new Utf8Hashtable<int>();
        if (useSpan)
        {
            Assert.Throws<FormatException>(() => utf16.GetOrAdd(key.AsSpan(), static _ => throw new FormatException()));
            Assert.Throws<FormatException>(() => utf8.GetOrAdd(bytes.AsSpan(), static _ => throw new FormatException()));
        }
        else
        {
            Assert.Throws<FormatException>(() => utf16.GetOrAdd(key, static _ => throw new FormatException()));
            Assert.Throws<FormatException>(() => utf8.GetOrAdd(bytes, static _ => throw new FormatException()));
        }

        Assert.False(utf16.ContainsKey(key));
        Assert.False(utf8.ContainsKey(bytes));
        Assert.Empty(utf16.ToArray());
        Assert.Empty(utf8.ToArray());
        Assert.Equal(10, useSpan ? utf16.GetOrAdd(key.AsSpan(), static _ => 10) : utf16.GetOrAdd(key, static _ => 10));
        Assert.Equal(20, useSpan ? utf8.GetOrAdd(bytes.AsSpan(), static _ => 20) : utf8.GetOrAdd(bytes, static _ => 20));
        Assert.Equal(1, utf16.Count);
        Assert.Equal(1, utf8.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UtfGetOrAddRejectsFactoryClearWithoutRestoringRemovedValues(bool useSpan)
    {
        var utf16 = new Utf16Hashtable<int>();
        var utf8 = new Utf8Hashtable<int>();
        utf16.TryAdd("existing", 1);
        utf8.TryAdd("existing"u8, 1);
        Func<string, int> stringFactory = _ => { utf16.Clear(); return 2; };
        Func<byte[], int> byteFactory = _ => { utf8.Clear(); return 2; };
        if (useSpan)
        {
            Assert.Throws<InvalidOperationException>(() => utf16.GetOrAdd("new".AsSpan(), stringFactory));
            Assert.Throws<InvalidOperationException>(() => utf8.GetOrAdd("new"u8, byteFactory));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => utf16.GetOrAdd("new", stringFactory));
            Assert.Throws<InvalidOperationException>(() => utf8.GetOrAdd("new"u8.ToArray(), byteFactory));
        }

        Assert.Equal(0, utf16.Count);
        Assert.Equal(0, utf8.Count);
        Assert.Empty(utf16.ToKeyValuePairs());
        Assert.Empty(utf8.ToKeyValuePairs());
        Assert.True(utf16.TryAdd("new", 3));
        Assert.True(utf8.TryAdd("new"u8, 3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UtfGetOrAddAllocatesNoMoreThanDirectInsertion(bool useSpan)
    {
        var key = new string('k', 1024);
        var bytes = Encoding.UTF8.GetBytes(key);
        var utf16 = new Utf16Hashtable<int>();
        var utf8 = new Utf8Hashtable<int>();
        Func<string, int> stringFactory = static _ => 1;
        Func<byte[], int> byteFactory = static _ => 1;
        var checksum = 0;
        var direct = AllocationHelper.Measure(() => Run(false));
        var factory = AllocationHelper.Measure(() => Run(true));
        Assert.True(checksum > 0);
        Assert.Equal(0, utf16.Count);
        Assert.Equal(0, utf8.Count);
        AllocationHelper.SkipUnlessOptimized();
        Assert.True(direct > 0);
        Assert.Equal(direct, factory);

        void Run(bool useFactory)
        {
            for (var i = 0; i < 100; i++)
            {
                if (useFactory)
                {
                    checksum += useSpan ? utf16.GetOrAdd(key.AsSpan(), stringFactory) : utf16.GetOrAdd(key, stringFactory);
                    checksum += useSpan ? utf8.GetOrAdd(bytes.AsSpan(), byteFactory) : utf8.GetOrAdd(bytes, byteFactory);
                }
                else
                {
                    checksum += (useSpan ? utf16.TryAdd(key.AsSpan(), 1) : utf16.TryAdd(key, 1)) ? 1 : 0;
                    checksum += (useSpan ? utf8.TryAdd(bytes.AsSpan(), 1) : utf8.TryAdd(bytes, 1)) ? 1 : 0;
                }

                utf16.TryRemove(key);
                utf8.TryRemove(bytes);
            }
        }
    }
}
