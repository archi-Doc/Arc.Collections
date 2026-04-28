using System;
using Arc;
using Xunit;

namespace xUnitTest.Arc;

public class BaseHelperTests
{
    [Fact]
    public void SplitLines_EmptyInput_IncludeEmptyLines_ReturnsArrayWithEmptyString()
    {
        // Arrange
        var source = "".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("", result[0]);
    }

    [Fact]
    public void SplitLines_EmptyInput_ExcludeEmptyLines_ReturnsEmptyArray()
    {
        // Arrange
        var source = "".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SplitLines_SingleLineNoNewline_IncludeEmptyLines_ReturnsSingleLine()
    {
        // Arrange
        var source = "Hello World".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello World", result[0]);
    }

    [Fact]
    public void SplitLines_SingleLineNoNewline_ExcludeEmptyLines_ReturnsSingleLine()
    {
        // Arrange
        var source = "Hello World".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello World", result[0]);
    }

    [Fact]
    public void SplitLines_SingleLineWithLF_IncludeEmptyLines_ReturnsTwoLines()
    {
        // Arrange
        var source = "Hello World\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Hello World", result[0]);
        Assert.Equal("", result[1]);
    }

    [Fact]
    public void SplitLines_SingleLineWithLF_ExcludeEmptyLines_ReturnsSingleLine()
    {
        // Arrange
        var source = "Hello World\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello World", result[0]);
    }

    [Fact]
    public void SplitLines_SingleLineWithCRLF_IncludeEmptyLines_ReturnsTwoLines()
    {
        // Arrange
        var source = "Hello World\r\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Hello World", result[0]);
        Assert.Equal("", result[1]);
    }

    [Fact]
    public void SplitLines_SingleLineWithCRLF_ExcludeEmptyLines_ReturnsSingleLine()
    {
        // Arrange
        var source = "Hello World\r\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello World", result[0]);
    }

    [Fact]
    public void SplitLines_MultipleLinesWithLF_IncludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\nLine2\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
    }

    [Fact]
    public void SplitLines_MultipleLinesWithLF_ExcludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\nLine2\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
    }

    [Fact]
    public void SplitLines_MultipleLinesWithCRLF_IncludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\r\nLine2\r\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
    }

    [Fact]
    public void SplitLines_MultipleLinesWithCRLF_ExcludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\r\nLine2\r\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
    }

    [Fact]
    public void SplitLines_EmptyLineAtStart_IncludeEmptyLines_ReturnsEmptyLineFirst()
    {
        // Arrange
        var source = "\nLine1\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("", result[0]);
        Assert.Equal("Line1", result[1]);
        Assert.Equal("Line2", result[2]);
    }

    [Fact]
    public void SplitLines_EmptyLineAtStart_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "\nLine1\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_EmptyLineInMiddle_IncludeEmptyLines_ReturnsEmptyLineInMiddle()
    {
        // Arrange
        var source = "Line1\n\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("Line2", result[2]);
    }

    [Fact]
    public void SplitLines_EmptyLineInMiddle_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "Line1\n\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_ConsecutiveEmptyLines_IncludeEmptyLines_ReturnsAllEmptyLines()
    {
        // Arrange
        var source = "Line1\n\n\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("", result[2]);
        Assert.Equal("Line2", result[3]);
    }

    [Fact]
    public void SplitLines_ConsecutiveEmptyLines_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "Line1\n\n\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_MixedLineEndings_IncludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\nLine2\r\nLine3\nLine4".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
        Assert.Equal("Line4", result[3]);
    }

    [Fact]
    public void SplitLines_MixedLineEndings_ExcludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\nLine2\r\nLine3\nLine4".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
        Assert.Equal("Line4", result[3]);
    }

    [Fact]
    public void SplitLines_OnlyNewlines_IncludeEmptyLines_ReturnsEmptyStrings()
    {
        // Arrange
        var source = "\n\n\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.All(result, line => Assert.Equal("", line));
    }

    [Fact]
    public void SplitLines_OnlyNewlines_ExcludeEmptyLines_ReturnsEmptyArray()
    {
        // Arrange
        var source = "\n\n\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SplitLines_OnlyCRLF_IncludeEmptyLines_ReturnsEmptyStrings()
    {
        // Arrange
        var source = "\r\n\r\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.All(result, line => Assert.Equal("", line));
    }

    [Fact]
    public void SplitLines_OnlyCRLF_ExcludeEmptyLines_ReturnsEmptyArray()
    {
        // Arrange
        var source = "\r\n\r\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SplitLines_TrailingEmptyLinesWithLF_IncludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "Line1\nLine2\n\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("", result[2]);
        Assert.Equal("", result[3]);
    }

    [Fact]
    public void SplitLines_TrailingEmptyLinesWithLF_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "Line1\nLine2\n\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_DefaultParameter_ExcludesEmptyLines()
    {
        // Arrange
        var source = "Line1\n\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_CRWithoutLF_IncludeEmptyLines_PreservesCarriageReturn()
    {
        // Arrange
        var source = "Line1\rLine2\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1\rLine2", result[0]);
        Assert.Equal("Line3", result[1]);
    }

    [Fact]
    public void SplitLines_CRWithoutLF_ExcludeEmptyLines_PreservesCarriageReturn()
    {
        // Arrange
        var source = "Line1\rLine2\nLine3".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1\rLine2", result[0]);
        Assert.Equal("Line3", result[1]);
    }

    [Fact]
    public void SplitLines_EmptyLinesWithCRLF_IncludeEmptyLines_ReturnsEmptyLines()
    {
        // Arrange
        var source = "Line1\r\n\r\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("", result[1]);
        Assert.Equal("Line2", result[2]);
    }

    [Fact]
    public void SplitLines_EmptyLinesWithCRLF_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "Line1\r\n\r\nLine2".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
    }

    [Fact]
    public void SplitLines_ComplexScenario_IncludeEmptyLines_ReturnsAllLines()
    {
        // Arrange
        var source = "\r\nLine1\n\nLine2\r\n\r\nLine3\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: true);

        // Assert
        Assert.Equal(7, result.Length);
        Assert.Equal("", result[0]);
        Assert.Equal("Line1", result[1]);
        Assert.Equal("", result[2]);
        Assert.Equal("Line2", result[3]);
        Assert.Equal("", result[4]);
        Assert.Equal("Line3", result[5]);
        Assert.Equal("", result[6]);
    }

    [Fact]
    public void SplitLines_ComplexScenario_ExcludeEmptyLines_ReturnsOnlyNonEmptyLines()
    {
        // Arrange
        var source = "\r\nLine1\n\nLine2\r\n\r\nLine3\n".AsSpan();

        // Act
        var result = BaseHelper.SplitLines(source, includeEmptyLines: false);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Line1", result[0]);
        Assert.Equal("Line2", result[1]);
        Assert.Equal("Line3", result[2]);
    }
}
