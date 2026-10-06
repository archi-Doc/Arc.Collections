using System;
using System.IO;
using System.Reflection;
using System.Text;
using Arc;
using Xunit;

namespace XunitTest;

public class HelperAuditTest
{
    [Theory]
    [InlineData('\t', true)]
    [InlineData('\r', true)]
    [InlineData(' ', true)]
    [InlineData(',', true)]
    [InlineData(';', true)]
    [InlineData('\u0085', true)]
    [InlineData('\u00A0', true)]
    [InlineData('\u2000', true)]
    [InlineData('\u200A', true)]
    [InlineData('\u2028', true)]
    [InlineData('\u2029', true)]
    [InlineData('\u3000', true)]
    [InlineData('\0', false)]
    [InlineData('a', false)]
    [InlineData('\u200B', false)]
    [InlineData('\u202F', false)]
    public void SeparatorLookupAndSearchUseSameRules(char value, bool expected)
    {
        Assert.Equal(expected, BaseHelper.IsSeparator(value));
        Assert.Equal(expected ? 1 : -1, BaseHelper.IndexOfSeparator($"a{value}b"));
        Assert.Equal(-1, BaseHelper.IndexOfSeparator(ReadOnlySpan<char>.Empty));
    }

    [Theory]
    [InlineData("aaaaa", "aa", 2)]
    [InlineData("abcabc", "abc", 2)]
    [InlineData("abc", "z", 0)]
    [InlineData("abc", "", 0)]
    [InlineData("", "abc", 0)]
    public void OccurrenceCountingDoesNotOverlap(string source, string value, int expected)
        => Assert.Equal(expected, BaseHelper.CountOccurrences(source, value));

    [Theory]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("a\rb", "a\rb")]
    [InlineData("a\r\nb", "a\r\nb")]
    [InlineData("\na\n\r\nb\r", "\r\na\r\n\r\nb\r")]
    public void CrLfConversionPreservesExistingPairsAndLoneCarriageReturns(string input, string expected)
    {
        var result = BaseHelper.ConvertLfToCrLf(input);
        Assert.Equal(expected, result);
        Assert.Same(result, BaseHelper.ConvertLfToCrLf(result));
        if (input == expected)
        {
            Assert.Same(input, result);
        }
    }

    [Fact]
    public void SpanAppendUpdatesDestinationOnlyWhenItFits()
    {
        var buffer = new char[4];
        Span<char> destination = buffer;
        var written = 0;
        Assert.True(BaseHelper.TryAppend(ref destination, ref written, 'a'));
        Assert.True(BaseHelper.TryAppend(ref destination, ref written, "bc"));
        Assert.False(BaseHelper.TryAppend(ref destination, ref written, "too long"));
        Assert.Equal(3, written);
        Assert.Equal(1, destination.Length);
        Assert.True(BaseHelper.TryAppend(ref destination, ref written, 'd'));
        Assert.False(BaseHelper.TryAppend(ref destination, ref written, 'e'));
        Assert.True(BaseHelper.TryAppend(ref destination, ref written, ReadOnlySpan<char>.Empty));
        Assert.Equal(4, written);
        Assert.Equal("abcd", new string(buffer));
    }

    [Fact]
    public void NullTrimmingHandlesNoNullAndLeadingNull()
    {
        byte[] source = [1, 2, 0, 3];
        Assert.Equal(new byte[] { 1, 2 }, BaseHelper.TrimAtFirstNull(source));
        Assert.Equal(new byte[] { 1, 2 }, BaseHelper.TrimAtFirstNull(source.AsSpan()).ToArray());
        byte[] withoutNull = [1, 2, 3];
        Assert.Same(withoutNull, BaseHelper.TrimAtFirstNull(withoutNull));
        Assert.Equal(withoutNull, BaseHelper.TrimAtFirstNull(withoutNull.AsSpan()).ToArray());
        Assert.Empty(BaseHelper.TrimAtFirstNull(new byte[] { 0, 1 }));
        Assert.Empty(BaseHelper.TrimAtFirstNull(ReadOnlySpan<byte>.Empty).ToArray());
    }

    [Fact]
    public void AppendLfIsIndependentOfPlatformLineEnding()
    {
        var builder = new StringBuilder();
        Assert.Same(builder, builder.AppendLf("text"));
        Assert.Same(builder, builder.AppendLf());
        Assert.Equal("text\n\n", builder.ToString());
    }

    [Fact]
    public void EnvironmentParsingPrefersSourceAndHandlesInvalidOrMissingFallback()
    {
        var variable = "ARC_COLLECTIONS_TEST_" + Guid.NewGuid().ToString("N");
        try
        {
            Assert.False(BaseHelper.TryParseFromEnvironmentVariable<StringConvertibleClass>(variable, out var result));
            Assert.Null(result);
            Assert.False(BaseHelper.TryParseFromSourceOrEnvironmentVariable<StringConvertibleClass>("bad", variable, out result));
            Assert.Null(result);
            Environment.SetEnvironmentVariable(variable, "a");
            Assert.True(BaseHelper.TryParseFromEnvironmentVariable<StringConvertibleClass>(variable, out result));
            Assert.NotNull(result);
            Assert.True(BaseHelper.TryParseFromSourceOrEnvironmentVariable<StringConvertibleClass>("bad", variable, out result));
            Assert.NotNull(result);
            Environment.SetEnvironmentVariable(variable, "bad");
            Assert.True(BaseHelper.TryParseFromSourceOrEnvironmentVariable<StringConvertibleClass>("a", variable, out result));
            Assert.NotNull(result);
            Assert.False(BaseHelper.TryParseFromEnvironmentVariable<StringConvertibleClass>(variable, out result));
            Assert.Null(result);
            Assert.False(BaseHelper.TryParseFromSourceOrEnvironmentVariable<StringConvertibleClass>("bad", variable, out result));
            Assert.Null(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void ConversionsHandleLargeUnicodeAndUnavailableFormatting()
    {
        var text = new string('\u3042', 2048) + "\U0001F600";
        var value = new StringConvertibleClass2(text);
        Assert.Equal(text, value.ConvertToString());
        Assert.Equal(Encoding.UTF8.GetBytes(text), value.ConvertToUtf8());
        Assert.Empty(new Unformattable(-1).ConvertToString());
        Assert.Empty(new Unformattable(-1).ConvertToUtf8());
        Assert.Empty(new Unformattable(10).ConvertToString());
        Assert.Empty(new Unformattable(10).ConvertToUtf8());
        Assert.Empty(new StringConvertibleClass2(string.Empty).ConvertToUtf8());
    }

    [Fact]
    public void ResourceLoadingReadsShortChunksAndDisposesTheStream()
    {
        var expected = Encoding.UTF8.GetBytes("embedded fixture");
        var stream = new ShortReadStream(expected);
        var assembly = new ResourceAssembly(stream);
        Assert.True(BaseHelper.TryLoadResource(assembly, "fixture", out var bytes));
        Assert.Equal(expected, bytes);
        Assert.Equal("TestAssembly.fixture", assembly.RequestedName);
        Assert.False(stream.CanRead);
        Assert.False(BaseHelper.TryLoadResource(null, "missing-fixture", out bytes));
        Assert.Null(bytes);
        Assert.False(BaseHelper.TryLoadResource(new ResourceAssembly(new FaultedReadStream()), "fixture", out bytes));
        Assert.Null(bytes);
    }

    [Fact]
    public void VersionSelectionUsesLoadedAssemblyAndLeavesUnknownNameUnchanged()
    {
        var assembly = typeof(BaseHelper).Assembly;
        var version = assembly.GetName().Version!;
        try
        {
            VersionHelper.SetAssembly(assembly.GetName().Name!);
            Assert.Equal(version.Major, VersionHelper.MajorVersion);
            Assert.Equal(version.Minor, VersionHelper.MinorVersion);
            Assert.Equal(version.Build, VersionHelper.BuildVersion);
            Assert.Equal($"{version.Major}.{version.Minor}.{version.Build}", VersionHelper.VersionString);
            Assert.Equal((version.Major << 24) + (version.Minor << 16) + (version.Build << 8), VersionHelper.EncodedVersion);
            var saved = VersionHelper.VersionString;
            VersionHelper.SetAssembly("missing-" + Guid.NewGuid().ToString("N"));
            Assert.Equal(saved, VersionHelper.VersionString);
            Assert.Throws<ArgumentNullException>(() => VersionHelper.SetAssembly(null!));
        }
        finally
        {
            if (Assembly.GetEntryAssembly()?.GetName().Name is { } name)
            {
                VersionHelper.SetAssembly(name);
            }
        }
    }

    private readonly struct Unformattable(int length) : IStringConvertible<Unformattable>
    {
        public static int MaxStringLength => -1;

        public int GetStringLength() => length;

        public static bool TryParse(ReadOnlySpan<char> source, out Unformattable result, out int charsRead, IConversionOptions? conversionOptions = null)
        {
            result = default;
            charsRead = 0;
            return false;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, IConversionOptions? conversionOptions = null)
        {
            charsWritten = 0;
            return false;
        }
    }

    private sealed class ResourceAssembly(Stream stream) : Assembly
    {
        public string? RequestedName { get; private set; }

        public override AssemblyName GetName() => new("TestAssembly");

        public override Stream GetManifestResourceStream(string name)
        {
            this.RequestedName = name;
            return stream;
        }
    }

    private sealed class ShortReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);
    }

    private sealed class FaultedReadStream : MemoryStream
    {
        public override long Length => throw new IOException("Unreadable resource.");
    }
}
