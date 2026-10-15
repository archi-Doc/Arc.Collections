// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Arc;
using Xunit;

namespace XunitTest;

public class ArcHelperQualityTest
{
    private const ulong HalfRange64 = 0x8000_0000_0000_0000;
    private const uint HalfRange32 = 0x8000_0000;

    [Fact]
    public void EnvironmentParsingReturnsDefaultWhenParsingFails()
    {
        var variable = "ARC_COLLECTIONS_QUALITY_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(variable, "bad");
            Assert.False(BaseHelper.TryParseFromEnvironmentVariable<PartialResultConvertible>(variable, out var result));
            Assert.Null(result);
            Assert.False(BaseHelper.TryParseFromSourceOrEnvironmentVariable<PartialResultConvertible>("bad", variable, out result));
            Assert.Null(result);

            Environment.SetEnvironmentVariable(variable, null);
            Assert.False(BaseHelper.TryParseFromSourceOrEnvironmentVariable<PartialResultConvertible>("bad", variable, out result));
            Assert.Null(result);

            Environment.SetEnvironmentVariable(variable, "ok");
            Assert.True(BaseHelper.TryParseFromEnvironmentVariable<PartialResultConvertible>(variable, out result));
            Assert.Equal("ok", result!.Text);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Theory]
    [InlineData(5UL, 3UL, 1)]
    [InlineData(3UL, 5UL, -1)]
    [InlineData(7UL, 7UL, 0)]
    [InlineData(0UL, ulong.MaxValue, 1)]
    [InlineData(ulong.MaxValue, 0UL, -1)]
    [InlineData(0UL, HalfRange64 - 1, -1)]
    [InlineData(0UL, HalfRange64 + 1, 1)]
    [InlineData(0UL, HalfRange64, -1)]
    [InlineData(HalfRange64, 0UL, 1)]
    public void CircularComparisonOrdersUInt64ValuesAcrossWrapAround(ulong value1, ulong value2, int expected)
    {
        Assert.Equal(expected, value1.CircularCompareTo(value2));
        Assert.Equal(-expected, value2.CircularCompareTo(value1));

        var min = expected < 0 ? value1 : value2;
        var max = expected < 0 ? value2 : value1;
        Assert.Equal(min, BaseHelper.CircularMin(value1, value2));
        Assert.Equal(min, BaseHelper.CircularMin(value2, value1));
        Assert.Equal(max, BaseHelper.CircularMax(value1, value2));
        Assert.Equal(max, BaseHelper.CircularMax(value2, value1));
    }

    [Theory]
    [InlineData(5U, 3U, 1)]
    [InlineData(3U, 5U, -1)]
    [InlineData(7U, 7U, 0)]
    [InlineData(0U, uint.MaxValue, 1)]
    [InlineData(uint.MaxValue, 0U, -1)]
    [InlineData(0U, HalfRange32 - 1, -1)]
    [InlineData(0U, HalfRange32 + 1, 1)]
    [InlineData(0U, HalfRange32, -1)]
    [InlineData(HalfRange32, 0U, 1)]
    public void CircularComparisonOrdersUInt32ValuesAcrossWrapAround(uint value1, uint value2, int expected)
    {
        Assert.Equal(expected, value1.CircularCompareTo(value2));
        Assert.Equal(-expected, value2.CircularCompareTo(value1));
    }

    [Fact]
    public void OccurrenceCountMatchesSequentialNonOverlappingSearch()
    {
        var text = string.Concat(Enumerable.Repeat("a,b", 500)) + "aaaa";
        foreach (var value in new[] { ",", "a", "a,", ",b", "a,ba", "aa", "aaa", "zz", text, text + "a" })
        {
            Assert.Equal(CountSequentially(text, value), BaseHelper.CountOccurrences(text, value));
        }

        Assert.Equal(2, BaseHelper.CountOccurrences("aaaa", "aa"));
        Assert.Equal(1, BaseHelper.CountOccurrences("aaa", "aa"));
        Assert.Equal(0, BaseHelper.CountOccurrences(text, ReadOnlySpan<char>.Empty));
        Assert.Equal(0, BaseHelper.CountOccurrences(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
    }

    [Fact]
    public void NewlineRemovalHandlesNullEmptyAndUnchangedInput()
    {
        Assert.Same(string.Empty, BaseHelper.RemoveCrAndLfChars(null!));
        Assert.Same(string.Empty, BaseHelper.RemoveCrAndLfChars(string.Empty));
        var text = "no newline";
        Assert.Same(text, BaseHelper.RemoveCrAndLfChars(text));
        Assert.Equal(string.Empty, BaseHelper.RemoveCrAndLfChars("\r\n\n\r"));

        Assert.Null(BaseHelper.RemoveAllOccurrences((string)null!, 'a'));
        Assert.Same(string.Empty, BaseHelper.RemoveAllOccurrences(string.Empty, 'a'));
        Assert.Same(text, BaseHelper.RemoveAllOccurrences(text, 'z'));
        Assert.Equal(string.Empty, BaseHelper.RemoveAllOccurrences("aaa", 'a'));

        var buffer = "xaxbx".ToCharArray();
        Assert.Equal("ab", new string(BaseHelper.RemoveAllOccurrences(buffer, 'x')));
        Assert.Equal(string.Empty, new string(BaseHelper.RemoveAllOccurrences("xxx".ToCharArray(), 'x')));
    }

    [Fact]
    public void NewlineHelpersMatchCharacterwiseReferenceForSparseAndDenseInput()
    {
        var long1 = new string('a', 100);
        var inputs = new List<string>
        {
            "\r" + long1, long1 + "\n", "\n" + long1, long1 + "\r\n" + long1 + "\n" + long1,
            "\r\n" + long1 + "\n" + long1, long1 + "\r" + long1 + "\r\r\n\n" + long1,
            new string('a', 30) + "\r\n", new string('a', 29) + "\r\n\r", // Removal count exactly at and just above length / 16.
            "\n" + new string('a', 62) + "\n", "\n" + new string('a', 30) + "\r\n" + new string('a', 30) + "\n", // Line feed count exactly at and just above length / 32.
        };

        var random = new Random(42);
        var alphabet = "abc\r\n";
        foreach (var length in new[] { 1, 2, 15, 16, 17, 31, 32, 33, 64, 200, 1000 })
        {
            foreach (var newlineRate in new[] { 0.01, 0.05, 0.1, 0.5, 1.0 })
            {
                var chars = new char[length];
                for (var i = 0; i < length; i++)
                {
                    chars[i] = random.NextDouble() < newlineRate ? alphabet[3 + random.Next(2)] : alphabet[random.Next(3)];
                }

                inputs.Add(new string(chars));
            }
        }

        foreach (var input in inputs)
        {
            var withoutNewlines = new string(input.Where(c => c != '\r' && c != '\n').ToArray());
            Assert.Equal(withoutNewlines, BaseHelper.RemoveCrAndLfChars(input));

            foreach (var value in new[] { '\r', '\n', 'a' })
            {
                var expected = new string(input.Where(c => c != value).ToArray());
                var result = BaseHelper.RemoveAllOccurrences(input, value);
                Assert.Equal(expected, result);
                if (expected.Length == input.Length)
                {
                    Assert.Same(input, result);
                }

                Assert.Equal(expected, new string(BaseHelper.RemoveAllOccurrences(input.ToCharArray(), value)));
            }

            var crLf = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '\n' && (i == 0 || input[i - 1] != '\r'))
                {
                    crLf.Append('\r');
                }

                crLf.Append(input[i]);
            }

            var converted = BaseHelper.ConvertLfToCrLf(input);
            Assert.Equal(crLf.ToString(), converted);
            if (converted.Length == input.Length)
            {
                Assert.Same(input, converted);
            }
        }
    }

    [Fact]
    public void LineSplittingKeepsCarriageReturnWithoutLineFeed()
    {
        Assert.Equal(new[] { "a\r", "b\r" }, BaseHelper.SplitLines("a\r\r\nb\r", includeEmptyLines: true));
        Assert.Equal(new[] { "\r", "b" }, BaseHelper.SplitLines("\r\r\n\r\nb"));
    }

    [Fact]
    public void StringConvertibleDetectionRequiresSelfImplementation()
    {
        Assert.True(BaseHelper.ImplementsIStringConvertible(typeof(PartialResultConvertible)));

        // Non-generic and unrelated generic interfaces are skipped.
        Assert.False(BaseHelper.ImplementsIStringConvertible(typeof(int)));

        // Inherits IStringConvertible<StringConvertibleClass>, not IStringConvertible<DerivedConvertible>.
        Assert.False(BaseHelper.ImplementsIStringConvertible(typeof(DerivedConvertible)));
    }

    [Fact]
    public void AppCloseHandlerKeepsFirstHandlerAndInvokesItOnce()
    {
        Assert.Throws<ArgumentNullException>(() => AppCloseHandler.Register(null!));

        // The registration is process-wide; this is the only test that registers a handler.
        // Private callbacks are invoked directly because a real close event would end the test process.
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var type = typeof(AppCloseHandler);
        var registeredHandler = type.GetField("registeredHandler", flags)!;
        var consoleEventCallback = type.GetMethod("ConsoleEventCallback", flags)!;
        var processExitCallback = type.GetMethod("ProcessExitCallback", flags)!;

        var count = 0;
        Action first = () => count++;
        AppCloseHandler.Register(first);
        AppCloseHandler.Register(() => throw new InvalidOperationException("Only the first handler is registered."));
        Assert.Same(first, registeredHandler.GetValue(null));

        Assert.False((bool)consoleEventCallback.Invoke(null, [0])!); // CTRL_C_EVENT is not handled.
        Assert.Equal(0, count);

        Assert.True((bool)consoleEventCallback.Invoke(null, [2])!); // CTRL_CLOSE_EVENT
        Assert.Equal(1, count);

        processExitCallback.Invoke(null, [null, EventArgs.Empty]);
        Assert.True((bool)consoleEventCallback.Invoke(null, [2])!);
        Assert.Equal(1, count);
    }

    [Fact]
    public void FixedSizeStructsSignExtendInt32Values()
    {
        var small = new Struct128(-1);
        Assert.Equal(-1L, small.Long0);
        Assert.Equal(-1, small.Int0);
        Assert.Equal(-1, small.Int1);
        Assert.Equal(0L, small.Long1);
        Assert.Equal(new Struct128(-1L), small);
        Assert.Equal("Struct128: 0000000000000000FFFFFFFFFFFFFFFF", small.ToString());

        var large = new Struct256(-1);
        Assert.Equal(-1L, large.Long0);
        Assert.Equal(-1, large.Int1);
        Assert.Equal(0L, large.Long1);
        Assert.Equal(0L, large.Long2);
        Assert.Equal(0L, large.Long3);
        Assert.Equal(new Struct256(-1L), large);
    }

    [Fact]
    public void FixedSizeStructsCompareSignedFieldsLexicographically()
    {
        // Signed comparison: -1 is less than 1 although its unsigned bit pattern is larger.
        Assert.True(new Struct128(-1, 0).CompareTo(new Struct128(1, 0)) < 0);
        Assert.True(new Struct128(0, long.MaxValue).CompareTo(new Struct128(1, long.MinValue)) < 0);
        Assert.True(new Struct128(1, long.MinValue).CompareTo(new Struct128(1, -1)) < 0);

        Assert.True(new Struct256(-1, 0, 0, 0).CompareTo(new Struct256(1, 0, 0, 0)) < 0);
        Assert.True(new Struct256(0, 0, 0, long.MaxValue).CompareTo(new Struct256(0, 0, 1, long.MinValue)) < 0);
        Assert.True(new Struct256(0, 0, 1, 0).CompareTo(new Struct256(0, 0, 0, 1)) > 0);
    }

    [Fact]
    public void FixedSizeStructsExposeAllBitsThroughEqualityTextAndBytes()
    {
        Assert.Equal(Struct128.Length, Unsafe.SizeOf<Struct128>());
        Assert.Equal(Struct256.Length, Unsafe.SizeOf<Struct256>());

        Assert.Equal("Struct128: 9", new Struct128(9).ToString());
        Assert.Equal("Struct128: 0000000000000000000000000000000A", new Struct128(10).ToString());
        Assert.Equal("Struct256: 9", new Struct256(9).ToString());
        Assert.Equal("Struct256: " + "00000000000000000000000000000001" + "00000000000000000000000000000001", new Struct256(1, 0, 1, 0).ToString());

        var upperOnly = new Struct256(0, 0, 0, 1);
        Assert.False(upperOnly.IsZero);
        Assert.NotEqual(new Struct256(0, 0, 1, 0), upperOnly);
        Assert.False(new Struct128(0, 1).IsZero);
        Assert.NotEqual(new Struct128(1, 0), new Struct128(0, 1));

        var small = new Struct128(0x0102_0304_0506_0708, -2);
        Span<byte> smallBytes = stackalloc byte[Struct128.Length];
        Assert.True(small.TryWriteBytes(smallBytes));
        Assert.True(small.AsSpan().SequenceEqual(smallBytes));
        Assert.Equal(small, new Struct128(small.AsSpan()));

        var large = new Struct256(1, -2, 3, -4);
        Span<byte> largeBytes = stackalloc byte[Struct256.Length];
        Assert.True(large.TryWriteBytes(largeBytes));
        Assert.True(large.AsSpan().SequenceEqual(largeBytes));
        Assert.Equal(large, new Struct256(large.AsSpan()));
    }

    private static int CountSequentially(string text, string value)
    {
        var count = 0;
        var start = 0;
        while (start <= text.Length - value.Length)
        {
            var index = text.IndexOf(value, start, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            count++;
            start = index + value.Length;
        }

        return count;
    }

    private sealed class DerivedConvertible : StringConvertibleClass
    {
    }

    private sealed class PartialResultConvertible(string text) : IStringConvertible<PartialResultConvertible>
    {
        public static int MaxStringLength => 16;

        public string Text { get; } = text;

        public static bool TryParse(ReadOnlySpan<char> source, out PartialResultConvertible? result, out int charsRead, IConversionOptions? conversionOptions = null)
        {
            // Reports a partial result on failure, which the helpers must not expose.
            result = new(source.ToString());
            charsRead = source.Length;
            return source.SequenceEqual("ok");
        }

        public int GetStringLength() => this.Text.Length;

        public bool TryFormat(Span<char> destination, out int charsWritten, IConversionOptions? conversionOptions = null)
        {
            charsWritten = 0;
            if (!this.Text.AsSpan().TryCopyTo(destination))
            {
                return false;
            }

            charsWritten = this.Text.Length;
            return true;
        }
    }
}
