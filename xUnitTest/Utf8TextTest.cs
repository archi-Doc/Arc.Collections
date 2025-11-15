using System;
using System.Text;
using Arc;
using Xunit;

namespace xUnitTest;

public class Utf8ValidatorTests
{
    [Fact]
    public void EmptyBytes_ReturnsZero()
    {
        var result = BaseHelper.GetValidUtf8Length(new byte[0]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AsciiOnly_ReturnsFullLength()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello World");
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(bytes.Length, result);
    }

    [Fact]
    public void CompleteUtf8Japanese_ReturnsFullLength()
    {
        var bytes = Encoding.UTF8.GetBytes("こんにちは");
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(bytes.Length, result);
    }

    [Fact]
    public void CompleteUtf8Mixed_ReturnsFullLength()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello世界123");
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(bytes.Length, result);
    }

    [Fact]
    public void TruncatedTwoByteChar_ReturnsValidLength()
    {
        // "あ" = E3 81 82 (3バイト)
        // "い" = E3 81 84 (3バイト)
        var bytes = Encoding.UTF8.GetBytes("あい");
        var truncated = new byte[bytes.Length + 1];
        Array.Copy(bytes, truncated, bytes.Length);
        truncated[bytes.Length] = 0xE3; // 3バイト文字の最初の1バイトのみ

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(bytes.Length, result);

        // 変換可能であることを確認
        var str = Encoding.UTF8.GetString(truncated, 0, result);
        Assert.Equal("あい", str);
    }

    [Fact]
    public void TruncatedThreeByteChar_FirstByteOnly_ReturnsValidLength()
    {
        var complete = Encoding.UTF8.GetBytes("Test");
        var truncated = new byte[complete.Length + 1];
        Array.Copy(complete, truncated, complete.Length);
        truncated[complete.Length] = 0xE3; // 3バイト文字の1バイト目のみ

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(complete.Length, result);
    }

    [Fact]
    public void TruncatedThreeByteChar_TwoBytesOnly_ReturnsValidLength()
    {
        var complete = Encoding.UTF8.GetBytes("Test");
        var truncated = new byte[complete.Length + 2];
        Array.Copy(complete, truncated, complete.Length);
        truncated[complete.Length] = 0xE3;     // 3バイト文字の1バイト目
        truncated[complete.Length + 1] = 0x81; // 2バイト目

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(complete.Length, result);
    }

    [Fact]
    public void TruncatedFourByteChar_Emoji_ReturnsValidLength()
    {
        // 😀 = F0 9F 98 80 (4バイト)
        var bytes = Encoding.UTF8.GetBytes("Hello😀");
        var truncated = new byte[bytes.Length - 1]; // 最後の1バイトを削除
        Array.Copy(bytes, truncated, truncated.Length);

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(5, result); // "Hello" の部分のみ

        var str = Encoding.UTF8.GetString(truncated, 0, result);
        Assert.Equal("Hello", str);
    }

    [Fact]
    public void TruncatedFourByteChar_OnlyFirstByte_ReturnsValidLength()
    {
        var complete = Encoding.UTF8.GetBytes("ABC");
        var truncated = new byte[complete.Length + 1];
        Array.Copy(complete, truncated, complete.Length);
        truncated[complete.Length] = 0xF0; // 4バイト文字の1バイト目のみ

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(complete.Length, result);
    }

    [Fact]
    public void CompleteEmoji_ReturnsFullLength()
    {
        var bytes = Encoding.UTF8.GetBytes("😀😁😂");
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(bytes.Length, result);
    }

    [Fact]
    public void MixedWithTruncatedAtEnd_ReturnsValidLength()
    {
        var complete = Encoding.UTF8.GetBytes("ABC日本語123");
        var truncated = new byte[complete.Length + 2];
        Array.Copy(complete, truncated, complete.Length);
        truncated[complete.Length] = 0xE3;
        truncated[complete.Length + 1] = 0x81;

        var result = BaseHelper.GetValidUtf8Length(truncated);
        Assert.Equal(complete.Length, result);
    }

    [Fact]
    public void WithOffsetAndCount_ReturnsCorrectLength()
    {
        var bytes = Encoding.UTF8.GetBytes("___Test___");
        var result = BaseHelper.GetValidUtf8Length(bytes.AsSpan(3, 4));
        Assert.Equal(4, result);
    }

    [Fact]
    public void WithOffsetAndTruncated_ReturnsValidLength()
    {
        var full = Encoding.UTF8.GetBytes("___Test");
        var bytes = new byte[full.Length + 2];
        Array.Copy(full, bytes, full.Length);
        bytes[full.Length] = 0xE3;
        bytes[full.Length + 1] = 0x81;

        var result = BaseHelper.GetValidUtf8Length(bytes.AsSpan(3, bytes.Length - 3));
        Assert.Equal(4, result);
    }

    [Fact]
    public void SingleAsciiChar_ReturnsOne()
    {
        var bytes = new byte[] { 0x41 }; // 'A'
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(1, result);
    }

    [Fact]
    public void SingleCompleteThreeByteChar_ReturnsThree()
    {
        var bytes = Encoding.UTF8.GetBytes("あ");
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(3, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Hello")]
    [InlineData("こんにちは")]
    [InlineData("Hello世界")]
    [InlineData("😀😁😂")]
    [InlineData("Test🎉日本語ABC")]
    public void CompleteStrings_AlwaysReturnFullLength(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(bytes.Length, result);
    }

    [Fact]
    public void ContinuationByteOnly_ReturnsZero()
    {
        var bytes = new byte[] { 0x80 }; // 継続バイトのみ
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(0, result);
    }

    [Fact]
    public void MultipleContinuationBytes_ReturnsZero()
    {
        var bytes = new byte[] { 0x80, 0x81, 0x82 }; // 継続バイトのみ
        var result = BaseHelper.GetValidUtf8Length(bytes);
        Assert.Equal(0, result);
    }
}
