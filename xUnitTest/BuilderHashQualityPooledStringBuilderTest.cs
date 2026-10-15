using System;
using System.Globalization;
using System.Text;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class BuilderHashQualityPooledStringBuilderTest
{
    [Fact]
    public void GetLastCharReadsCurrentChunkAndPreviousChunk()
    {
        var builder = new PooledStringBuilder();
        try
        {
            builder.GetLastChar(out var last);
            Assert.Equal('\0', last);
            Assert.Same(string.Empty, builder.ToString());

            builder.Append("ab");
            builder.GetLastChar(out last);
            Assert.Equal('b', last);

            var text = new string('x', PooledStringBuilder.DefaultInitialCapacity - 2) + "yz";
            builder.Append(text); // Fills the first chunk exactly.
            builder.GetLastChar(out last);
            Assert.Equal('z', last);

            builder.Append('!'); // Starts the second chunk.
            builder.GetLastChar(out last);
            Assert.Equal('!', last);
            builder.GetLastTwoChars(out var previous, out last);
            Assert.Equal('z', previous);
            Assert.Equal('!', last);
            Assert.Equal("ab" + text + "!", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void TrailingCharactersAreReadFromSegmentsWhenCurrentChunkIsEmpty()
    {
        var builder = new PooledStringBuilder();
        try
        {
            // A value that does not fit the current chunk commits it and is formatted into a new one.
            // Writing no characters leaves the new chunk empty, so the trailing characters live in segments.
            builder.Append('X');
            builder.Append(new ScriptedFormattable(string.Empty, failures: 1));
            Assert.Equal(1, builder.Length);
            builder.GetLastChar(out var last);
            Assert.Equal('X', last);
            builder.GetLastTwoChars(out var previous, out last);
            Assert.Equal('\0', previous);
            Assert.Equal('X', last);

            builder.Append('A');
            builder.Append(new ScriptedFormattable(string.Empty, failures: 1));
            builder.GetLastTwoChars(out previous, out last);
            Assert.Equal('X', previous); // The last segment holds one character; the previous one is in the preceding segment.
            Assert.Equal('A', last);

            builder.Append("BC");
            builder.Append(new ScriptedFormattable(string.Empty, failures: 1));
            builder.GetLastChar(out last);
            Assert.Equal('C', last);
            builder.GetLastTwoChars(out previous, out last);
            Assert.Equal('B', previous);
            Assert.Equal('C', last);

            builder.EnsureTrailingSpace();
            builder.EnsureTrailingSpace();
            Assert.Equal("XABC ", builder.ToString());

            builder.Append(new ScriptedFormattable(string.Empty, failures: 1));
            builder.EnsureTrailingBlankLine();
            builder.EnsureTrailingBlankLine();
            Assert.Equal("XABC \n\n", builder.ToString());
            Assert.Equal(7, builder.Length);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FormattingExceptionLeavesBuilderUsable(int failures)
    {
        var builder = new PooledStringBuilder();
        try
        {
            builder.Append("ok");
            var value = new ScriptedFormattable("unused", failures, throwAfterFailures: true);
            var thrown = false;
            try
            {
                builder.Append(value);
            }
            catch (FormatException)
            {
                thrown = true;
            }

            Assert.True(thrown);
            Assert.Equal(failures + 1, value.Calls);
            Assert.Equal(2, builder.Length);
            Assert.Equal("ok", builder.ToString());
            builder.GetLastTwoChars(out var previous, out var last);
            Assert.Equal('o', previous);
            Assert.Equal('k', last);

            thrown = false;
            try
            {
                builder.AppendLine(new ScriptedFormattable("unused", failures, throwAfterFailures: true), "G", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                thrown = true;
            }

            Assert.True(thrown);
            Assert.Equal("ok", builder.ToString()); // No line feed is appended.

            builder.Append('!');
            builder.AppendLine(12);
            Assert.Equal("ok!12\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("X4")]
    public void OversizedValueFallsBackToToStringWithFormatAndProvider(string format)
    {
        var text = new string('v', PooledStringBuilder.MaxChunkCapacity + 1);
        var provider = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        var value = new ScriptedFormattable(text, failures: 0);
        var builder = new PooledStringBuilder();
        try
        {
            builder.Append('[');
            builder.AppendLine(value, format, provider);

            Assert.Equal(format.Length == 0 ? null : format, value.LastFormat);
            Assert.Same(provider, value.LastProvider);
            Assert.Equal(text.Length + 2, builder.Length);
            Assert.Equal("[" + text + "\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void DefaultFormattingIsInvariantAndNullProviderUsesCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        var builder = new PooledStringBuilder();
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = culture;
            builder.Append(1.5);
            builder.Append(' ');
            builder.AppendLine(2.5);
            builder.Append(1.5, default, null);
            Assert.Equal("1.5 2.5\n1,5", builder.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            builder.Dispose();
        }
    }

    [Fact]
    public void FormatProviderIsPassedToValueUnchanged()
    {
        var value = new ScriptedFormattable("v", failures: 0);
        var provider = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        var builder = new PooledStringBuilder();
        try
        {
            builder.Append(value);
            Assert.Same(CultureInfo.InvariantCulture, value.LastProvider);
            builder.AppendLine(value);
            Assert.Same(CultureInfo.InvariantCulture, value.LastProvider);
            builder.Append(value, default, provider);
            Assert.Same(provider, value.LastProvider);
            builder.AppendLine(value, default, null);
            Assert.Null(value.LastProvider); // The value's type decides what null means.

            Assert.Equal(4, value.Calls);
            Assert.Equal("vv\nvv\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendMatchesStringBuilderAcrossChunkBoundaries()
    {
        var random = new Random(318);
        var expected = new StringBuilder();
        var builder = new PooledStringBuilder();
        try
        {
            for (var i = 0; i < 4000; i++)
            {
                switch (random.Next(8))
                {
                    case 0:
                    case 1:
                    case 2:
                        var c = (char)random.Next(0x20, 0x7F);
                        builder.Append(c);
                        expected.Append(c);
                        break;
                    case 3:
                        var text = new string((char)random.Next('a', 'z' + 1), random.Next(0, 1500));
                        builder.Append(text);
                        expected.Append(text);
                        break;
                    case 4:
                        var number = random.Next(int.MinValue, int.MaxValue);
                        builder.Append(number);
                        expected.Append(number.ToString(CultureInfo.InvariantCulture));
                        break;
                    case 5:
                        var real = random.NextDouble() * 1000;
                        builder.Append(real, "F3", CultureInfo.InvariantCulture);
                        expected.Append(real.ToString("F3", CultureInfo.InvariantCulture));
                        break;
                    case 6:
                        builder.AppendLine();
                        expected.Append('\n');
                        break;
                    default:
                        builder.EnsureTrailingSpace();
                        if (expected.Length > 0 && expected[^1] != ' ' && expected[^1] != '\n')
                        {
                            expected.Append(' ');
                        }

                        break;
                }

                Assert.Equal(expected.Length, builder.Length);
                builder.GetLastTwoChars(out var previous, out var last);
                Assert.Equal(expected.Length > 1 ? expected[^2] : '\0', previous);
                Assert.Equal(expected.Length > 0 ? expected[^1] : '\0', last);

                if (i % 500 == 499)
                {
                    Assert.Equal(expected.ToString(), builder.ToString());
                }

                if (i == 2000)
                {
                    builder.Clear();
                    expected.Clear();
                }
            }

            Assert.Equal(expected.ToString(), builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendsIntoRentedChunkDoNotAllocate()
    {
        Measure(); // Warms up the code paths.
        var allocated = AllocationHelper.Minimum(Measure);

        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static long Measure()
        {
            var builder = new PooledStringBuilder();
            try
            {
                AppendSample(ref builder); // Rents the first chunk and warms up formatting.
                builder.Clear(); // Retains the chunk.

                var before = GC.GetAllocatedBytesForCurrentThread();
                AppendSample(ref builder);
                var after = GC.GetAllocatedBytesForCurrentThread();

                Assert.True(builder.Length < PooledStringBuilder.DefaultInitialCapacity);
                return after - before;
            }
            finally
            {
                builder.Dispose();
            }
        }

        static void AppendSample(ref PooledStringBuilder builder)
        {
            for (var i = 0; i < 16; i++)
            {
                builder.Append(i);
                builder.Append(1.25);
                builder.Append(true);
                builder.Append('c');
                builder.AppendLine("ab");
                builder.EnsureTrailingSpace();
                builder.EnsureTrailingBlankLine();
            }
        }
    }

    private sealed class ScriptedFormattable : ISpanFormattable
    {
        private readonly string text;
        private readonly int failures;
        private readonly bool throwAfterFailures;

        public ScriptedFormattable(string text, int failures, bool throwAfterFailures = false)
        {
            this.text = text;
            this.failures = failures;
            this.throwAfterFailures = throwAfterFailures;
        }

        public int Calls { get; private set; }

        public string? LastFormat { get; private set; }

        public IFormatProvider? LastProvider { get; private set; }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            this.Calls++;
            this.LastProvider = provider;
            charsWritten = 0;
            if (this.Calls <= this.failures)
            {
                return false;
            }

            if (this.throwAfterFailures)
            {
                throw new FormatException("Scripted failure.");
            }

            if (!this.text.AsSpan().TryCopyTo(destination))
            {
                return false;
            }

            charsWritten = this.text.Length;
            return true;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            this.LastFormat = format;
            this.LastProvider = formatProvider;
            return this.text;
        }
    }
}
