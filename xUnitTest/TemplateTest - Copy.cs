using System;
using System.Globalization;
using Xunit;

namespace Arc.Collections.Tests;

public class PooledStringBuilderTest
{
    [Fact]
    public void DefaultBuilder_IsEmpty()
    {
        var builder = new PooledStringBuilder();

        try
        {
            Assert.Equal(0, builder.Length);
            Assert.Equal(string.Empty, builder.ToString());

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('\0', previous);
            Assert.Equal('\0', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendChar_AppendsCharacter()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append('A');
            builder.Append('B');
            builder.Append('C');

            Assert.Equal(3, builder.Length);
            Assert.Equal("ABC", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendSpan_AppendsCharacters()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("Hello");
            builder.Append(", ");
            builder.Append("world!");

            Assert.Equal(13, builder.Length);
            Assert.Equal("Hello, world!", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendEmptySpan_DoesNothing()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("ABC");
            builder.Append(ReadOnlySpan<char>.Empty);

            Assert.Equal(3, builder.Length);
            Assert.Equal("ABC", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendBool_AppendsInvariantRepresentation()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(true);
            builder.Append(',');
            builder.Append(false);

            Assert.Equal("True,False", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendGeneric_AppendsInvariantRepresentation()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(123);
            builder.Append(',');
            builder.Append(45.5);

            Assert.Equal("123,45.5", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendGeneric_WithFormatAndProvider_AppliesFormatting()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(
                1234.5,
                "N2",
                CultureInfo.InvariantCulture);

            Assert.Equal("1,234.50", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLine_AppendsOnlyLineFeed()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("First");
            builder.AppendLine();
            builder.Append("Second");

            Assert.Equal("First\nSecond", builder.ToString());
            Assert.DoesNotContain('\r', builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLineSpan_AppendsValueAndLineFeed()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.AppendLine("First");
            builder.AppendLine("Second");

            Assert.Equal("First\nSecond\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLineEmptySpan_AppendsLineFeed()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.AppendLine(ReadOnlySpan<char>.Empty);

            Assert.Equal(1, builder.Length);
            Assert.Equal("\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLineGeneric_AppendsValueAndLineFeed()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.AppendLine(123);
            builder.AppendLine(45.5);

            Assert.Equal("123\n45.5\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLineGeneric_WithFormatAndProvider_AppliesFormatting()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.AppendLine(
                1234.5,
                "N2",
                CultureInfo.InvariantCulture);

            Assert.Equal("1,234.50\n", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void ToString_CanBeCalledMultipleTimes()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("ABC");

            Assert.Equal("ABC", builder.ToString());
            Assert.Equal("ABC", builder.ToString());

            builder.Append("DEF");

            Assert.Equal("ABCDEF", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLargeSpan_CreatesMultipleSegments()
    {
        const int length = PooledStringBuilder.MaxChunkCapacity * 3 + 123;

        var source = new string('X', length);
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(source);

            Assert.Equal(length, builder.Length);
            Assert.Equal(source, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendMultipleLargeSpans_PreservesOrder()
    {
        var first = new string('A', PooledStringBuilder.MaxChunkCapacity + 17);
        var second = new string('B', PooledStringBuilder.MaxChunkCapacity + 31);
        var third = new string('C', 257);

        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(first);
            builder.Append(second);
            builder.Append(third);

            var result = builder.ToString();

            Assert.Equal(first.Length + second.Length + third.Length, builder.Length);
            Assert.Equal(first + second + third, result);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendChar_AfterLargeSpan_PreservesContent()
    {
        var source = new string('A', PooledStringBuilder.MaxChunkCapacity * 2);

        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(source);
            builder.Append('Z');

            Assert.Equal(source.Length + 1, builder.Length);
            Assert.Equal(source + "Z", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLargeFormattable_FallsBackToStringRepresentation()
    {
        const int length = PooledStringBuilder.MaxChunkCapacity + 123;

        var value = new LargeFormattable('F', length);
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(value);

            Assert.Equal(length, builder.Length);
            Assert.Equal(new string('F', length), builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void AppendLargeFormattable_AfterExistingContent_PreservesOrder()
    {
        const int formattedLength = PooledStringBuilder.MaxChunkCapacity + 123;

        var value = new LargeFormattable('X', formattedLength);
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("Prefix:");
            builder.Append(value);
            builder.Append(":Suffix");

            var expected = "Prefix:" + new string('X', formattedLength) + ":Suffix";

            Assert.Equal(expected.Length, builder.Length);
            Assert.Equal(expected, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void Clear_RemovesAllContent()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("ABC");
            builder.Clear();

            Assert.Equal(0, builder.Length);
            Assert.Equal(string.Empty, builder.ToString());

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('\0', previous);
            Assert.Equal('\0', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void Clear_AfterMultipleSegments_RemovesAllContent()
    {
        var source = new string('A', PooledStringBuilder.MaxChunkCapacity * 3);

        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(source);
            builder.Clear();

            Assert.Equal(0, builder.Length);
            Assert.Equal(string.Empty, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void Clear_AllowsBuilderToBeReused()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(new string('A', PooledStringBuilder.MaxChunkCapacity * 2));
            builder.Clear();

            builder.Append("Reused");

            Assert.Equal(6, builder.Length);
            Assert.Equal("Reused", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void Clear_CanBeCalledMultipleTimes()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Clear();
            builder.Clear();

            builder.Append("ABC");
            builder.Clear();
            builder.Clear();

            Assert.Equal(0, builder.Length);
            Assert.Equal(string.Empty, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void GetLastTwoChars_WithOneCharacter_ReturnsOnlyLast()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append('A');

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('\0', previous);
            Assert.Equal('A', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void GetLastTwoChars_WithTwoCharacters_ReturnsBoth()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("AB");

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('A', previous);
            Assert.Equal('B', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void GetLastTwoChars_WithMultipleCharacters_ReturnsLastTwo()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("ABCDE");

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('D', previous);
            Assert.Equal('E', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void GetLastTwoChars_AcrossSegmentBoundary_ReturnsLastTwo()
    {
        var source = new string('A', PooledStringBuilder.MaxChunkCapacity * 2);

        var builder = new PooledStringBuilder();

        try
        {
            builder.Append(source);
            builder.Append('B');

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('A', previous);
            Assert.Equal('B', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void GetLastTwoChars_AfterAppendLine_ReturnsValueAndLineFeed()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.AppendLine("ABC");

            builder.GetLastTwoChars(out var previous, out var last);

            Assert.Equal('C', previous);
            Assert.Equal('\n', last);
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void Dispose_OnEmptyBuilder_DoesNotThrow()
    {
        var builder = new PooledStringBuilder();

        builder.Dispose();

        Assert.Equal(0, builder.Length);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var builder = new PooledStringBuilder();

        builder.Append(new string('A', PooledStringBuilder.MaxChunkCapacity * 2));

        builder.Dispose();
        builder.Dispose();

        Assert.Equal(0, builder.Length);
        Assert.Equal(string.Empty, builder.ToString());
    }

    [Fact]
    public void Dispose_ResetsBuilder()
    {
        var builder = new PooledStringBuilder();

        builder.Append("ABC");
        builder.Dispose();

        Assert.Equal(0, builder.Length);
        Assert.Equal(string.Empty, builder.ToString());

        builder.GetLastTwoChars(out var previous, out var last);

        Assert.Equal('\0', previous);
        Assert.Equal('\0', last);
    }

    [Fact]
    public void Builder_CanBeUsedAgainAfterDispose()
    {
        var builder = new PooledStringBuilder();

        try
        {
            builder.Append("First");
            builder.Dispose();

            builder.Append("Second");

            Assert.Equal(6, builder.Length);
            Assert.Equal("Second", builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    private readonly struct LargeFormattable : ISpanFormattable
    {
        private readonly char character;
        private readonly int length;

        public LargeFormattable(char character, int length)
        {
            this.character = character;
            this.length = length;
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            if (destination.Length < this.length)
            {
                charsWritten = 0;
                return false;
            }

            destination[..this.length].Fill(this.character);
            charsWritten = this.length;
            return true;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
            => new(this.character, this.length);

        public override string ToString()
            => new(this.character, this.length);
    }
}
