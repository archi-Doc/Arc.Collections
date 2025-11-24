// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

#pragma warning disable SA1405

namespace Arc;

/// <summary>
/// Provides helper methods for various base operations.
/// </summary>
public static class BaseHelper
{
    /// <summary>
    /// The maximum number of characters required to represent a 32-bit signed integer in decimal format, including the sign.
    /// </summary>
    public const int Int32MaxDecimalChars = 11;

    /// <summary>
    /// The maximum number of characters required to represent a 32-bit unsigned integer in decimal format.
    /// </summary>
    public const int UInt32MaxDecimalChars = 10;

    /// <summary>
    /// The maximum number of characters required to represent a 64-bit signed integer in decimal format, including the sign.
    /// </summary>
    public const int Int64MaxDecimalChars = 20;

    /// <summary>
    /// The maximum number of characters required to represent a 64-bit unsigned integer in decimal format.
    /// </summary>
    public const int UInt64MaxDecimalChars = 20;

    private static readonly uint[] Pow10 = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000,];
    private static readonly ulong[] Pow10B = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000, 10_000_000_000_000, 100_000_000_000_000, 1_000_000_000_000_000, 10_000_000_000_000_000, 100_000_000_000_000_000, 1_000_000_000_000_000_000, 10_000_000_000_000_000_000,];

    /// <summary>
    /// Removes all newline characters ('\r' and '\n') from the input string.
    /// </summary>
    /// <param name="input">The input string from which to remove newline characters.</param>
    /// <returns>
    /// A new string with all newline characters removed. If the input is null or empty, returns an empty string.
    /// If there are no newline characters, returns the original string.
    /// </returns>
    public static string RemoveCrLf(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        if (!input.Contains('\n'))
        {
            return input;
        }

        var newlineCount = 0;
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '\r' || input[i] == '\n')
            {
                newlineCount++;
            }
        }

        if (newlineCount == 0)
        {
            return input;
        }

        var resultLength = input.Length - newlineCount;
        return string.Create(resultLength, input, static (span, src) =>
        {
            var position = 0;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] != '\r' && src[i] != '\n')
                {
                    span[position++] = src[i];
                }
            }
        });
    }

    /// <summary>
    /// Estimates the number of bytes that form a valid UTF-8 string in the byte array.<br/>
    /// Only the trailing bytes are inspected; the entire byte array is not validated.
    /// </summary>
    /// <param name="bytes">A byte span used to obtain the number of valid bytes.</param>
    /// <returns>Returns the number of valid bytes.</returns>
    public static int GetValidUtf8Length(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length;
        var i = length - 1;
        if (length == 0)
        {// Empty buffer
            return 0;
        }

        if (bytes[i] <= 0x7F)
        {// ASCII byte
            return length;
        }

        if ((bytes[i] & 0b1100_0000) == 0b1000_0000)
        {
            i--;
            if (i >= 0 && (bytes[i] & 0b1100_0000) == 0b1000_0000)
            {
                i--;
                if (i >= 0 && (bytes[i] & 0b1100_0000) == 0b1000_0000)
                {
                    i--;
                }
            }
        }

        if (i < 0)
        {
            return 0;
        }

        int seqLen;
        if ((bytes[i] & 0b1110_0000) == 0b1100_0000)
        {
            seqLen = 2;
        }
        else if ((bytes[i] & 0b1111_0000) == 0b1110_0000)
        {
            seqLen = 3;
        }
        else if ((bytes[i] & 0b1111_1000) == 0b1111_0000)
        {
            seqLen = 4;
        }
        else
        {
            return length;
        }

        if (length < i + seqLen)
        {
            return i;
        }
        else
        {
            return length;
        }
    }

    /// <summary>
    /// Computes the sum of all elements in a span of signed bytes using SIMD acceleration when available.
    /// </summary>
    /// <param name="data">The read-only span of signed bytes to sum.</param>
    /// <returns>The sum of all elements in the span as a 32-bit signed integer.</returns>
    public static unsafe int Sum(ReadOnlySpan<sbyte> data)
    {
        int sum = 0;
        int i = 0;

        if (Avx2.IsSupported && data.Length >= 32)
        {
            var accumulator = Vector256<int>.Zero;

            for (; i <= data.Length - 32; i += 32)
            {
                var bytes = Avx2.LoadVector256((sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data.Slice(i))));

                var low16 = Avx2.ConvertToVector256Int16(bytes.GetLower());
                var high16 = Avx2.ConvertToVector256Int16(bytes.GetUpper());

                var low32_1 = Avx2.ConvertToVector256Int32(low16.GetLower());
                var low32_2 = Avx2.ConvertToVector256Int32(low16.GetUpper());
                var high32_1 = Avx2.ConvertToVector256Int32(high16.GetLower());
                var high32_2 = Avx2.ConvertToVector256Int32(high16.GetUpper());

                accumulator = Avx2.Add(accumulator, low32_1);
                accumulator = Avx2.Add(accumulator, low32_2);
                accumulator = Avx2.Add(accumulator, high32_1);
                accumulator = Avx2.Add(accumulator, high32_2);
            }

            var x = Avx2.HorizontalAdd(accumulator, accumulator);
            x = Avx2.HorizontalAdd(x, x);
            var sum128 = Sse2.Add(x.GetLower(), x.GetUpper());
            sum = Sse2.ConvertToInt32(sum128);
        }

        for (; i < data.Length; i++)
        {
            sum += data[i];
        }

        return sum;
    }

    /// <summary>
    /// Computes the sum of all elements in a span of unsigned bytes using SIMD acceleration when available.
    /// </summary>
    /// <param name="data">The read-only span of unsigned bytes to sum.</param>
    /// <returns>The sum of all elements in the span as a 32-bit unsigned integer.</returns>
    public static unsafe ulong Sum(ReadOnlySpan<byte> data)
    {
        ulong acc = 0;
        ref byte p = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Sse2.IsSupported)
        {
            for (; i + 32 <= len; i += 32)
            {
                var v256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref p, i));
                var sadLo = Sse2.SumAbsoluteDifferences(v256.GetLower(), Vector128<byte>.Zero).AsUInt64();
                var sadHi = Sse2.SumAbsoluteDifferences(v256.GetUpper(), Vector128<byte>.Zero).AsUInt64();
                acc += sadLo.GetElement(0) + sadLo.GetElement(1) + sadHi.GetElement(0) + sadHi.GetElement(1);
            }

            for (; i + 16 <= len; i += 16)
            {
                var v128 = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref p, i));
                var sad = Sse2.SumAbsoluteDifferences(v128, Vector128<byte>.Zero).AsUInt64();
                acc += sad.GetElement(0) + sad.GetElement(1);
            }
        }

        for (; i < len; i++)
        {
            acc += data[i];
        }

        return acc;
    }

    /// <summary>
    /// Finds the index of the first line feed character ('\n') or carriage return ('\r') in the specified text span,
    /// accommodating both Lf ('\n') and CrLf ('\r\n') line endings.
    /// </summary>
    /// <param name="text">The span of characters to search for line feed or carriage return characters.</param>
    /// <param name="newLineLength">When this method returns, contains the length of the detected line ending:
    /// 1 for Lf ('\n'), 2 for CrLf ('\r\n'), or 0 if no line ending is found.</param>
    /// <returns>The zero-based index of the line ending (Lf or CrLf) in the text span, or -1 if no line ending is found.</returns>
    public static int IndexOfLfOrCrLf(ReadOnlySpan<char> text, out int newLineLength)
    {// \r\n (CrLf) or \n (Lf)
        var index = text.IndexOf('\n');
        if (index < 0)
        {// Not found]
            newLineLength = 0;
            return -1;
        }
        else if (index > 0 && text[index - 1] == '\r')
        {// \r\n (CrLf)
            newLineLength = 2;
            return index - 1;
        }
        else
        {// \n (Lf)
            newLineLength = 1;
            return index;
        }
    }

    /// <summary>
    /// Converts all line feed ('\n') characters in the input string to carriage return and line feed ("\r\n") pairs,
    /// except where the line feed is already preceded by a carriage return. This ensures all line endings are in CRLF format.
    /// </summary>
    /// <param name="text">The input string to convert line endings for.</param>
    /// <returns>
    /// A new string with all line feed characters converted to CRLF pairs, unless already in CRLF format.
    /// If no conversion is necessary, returns the original string.
    /// </returns>
    public static string ConvertLfToCrLf(string text)
    {
        var extra = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' && (i == 0 || text[i - 1] != '\r'))
            {
                extra++;
            }
        }

        if (extra == 0)
        {
            return text;
        }

        return string.Create(text.Length + extra, text, static (dest, source) =>
        {
            var position = 0;
            for (var i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (c == '\n' && (i == 0 || source[i - 1] != '\r'))
                {
                    dest[position++] = '\r';
                    dest[position++] = '\n';
                }
                else
                {
                    dest[position++] = c;
                }
            }
        });
    }

    /// <summary>
    /// Attempts to append a single character to the destination span.
    /// </summary>
    /// <param name="destination">The span to which the character will be appended. The span is updated to exclude the written character.</param>
    /// <param name="written">A reference to the count of characters written so far. This value is incremented if the append succeeds.</param>
    /// <param name="c">The character to append.</param>
    /// <returns><c>true</c> if the character was successfully appended; otherwise, <c>false</c> if there was not enough space.</returns>
    public static bool TryAppend(scoped ref Span<char> destination, ref int written, char c)
    {
        if (destination.Length == 0)
        {
            return false;
        }

        written += 1;
        destination[0] = c;
        destination = destination.Slice(1);
        return true;
    }

    /// <summary>
    /// Attempts to append a span of characters to the destination span.
    /// </summary>
    /// <param name="destination">The span to which the characters will be appended. The span is updated to exclude the written characters.</param>
    /// <param name="written">A reference to the count of characters written so far. This value is incremented by the length of <paramref name="span"/> if the append succeeds.</param>
    /// <param name="span">The span of characters to append.</param>
    /// <returns><c>true</c> if the characters were successfully appended; otherwise, <c>false</c> if there was not enough space.</returns>
    public static bool TryAppend(scoped ref Span<char> destination, ref int written, ReadOnlySpan<char> span)
    {
        if (destination.Length < span.Length)
        {
            return false;
        }

        span.CopyTo(destination);
        destination = destination.Slice(span.Length);
        written += span.Length;
        return true;
    }

    /// <summary>
    /// Returns the number of characters required to represent a 32-bit signed integer in decimal format, including the sign if negative.
    /// </summary>
    /// <param name="value">The 32-bit signed integer value.</param>
    /// <returns>The number of decimal characters required to represent the value.</returns>
    public static int CountDecimalChars(int value)
    {
        if (value == 0)
        {
            return 1;
        }
        else if (value > 0)
        {
            var v2 = (uint)value;
            int bits = 32 - BitOperations.LeadingZeroCount(v2);
            int log10 = (bits * 1233) >> 12;
            return log10 + ((v2 >= Pow10[log10]) ? 1 : 0);
        }
        else
        {
            var v2 = (uint)-value;
            int bits = 32 - BitOperations.LeadingZeroCount(v2);
            int log10 = (bits * 1233) >> 12;
            return log10 + 1 + ((v2 >= Pow10[log10]) ? 1 : 0);
        }
    }

    /// <summary>
    /// Returns the number of characters required to represent a 32-bit unsigned integer in decimal format.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer value.</param>
    /// <returns>The number of decimal characters required to represent the value.</returns>
    public static int CountDecimalChars(uint value)
    {
        if (value == 0)
        {
            return 1;
        }

        int bits = 32 - BitOperations.LeadingZeroCount(value);
        int log10 = (bits * 1233) >> 12;
        return log10 + ((value >= Pow10[log10]) ? 1 : 0);
    }

    /// <summary>
    /// Returns the number of characters required to represent a 64-bit signed integer in decimal format, including the sign if negative.
    /// </summary>
    /// <param name="value">The 64-bit signed integer value.</param>
    /// <returns>The number of decimal characters required to represent the value.</returns>
    public static int CountDecimalChars(long value)
    {
        if (value == 0)
        {
            return 1;
        }
        else if (value > 0)
        {
            var v2 = (ulong)value;
            int bits = 64 - BitOperations.LeadingZeroCount(v2);
            int log10 = (bits * 1233) >> 12;
            return log10 + ((v2 >= Pow10B[log10]) ? 1 : 0);
        }
        else
        {
            var v2 = (ulong)-value;
            int bits = 64 - BitOperations.LeadingZeroCount(v2);
            int log10 = (bits * 1233) >> 12;
            return log10 + 1 + ((v2 >= Pow10B[log10]) ? 1 : 0);
        }
    }

    /// <summary>
    /// Returns the number of characters required to represent a 64-bit unsigned integer in decimal format.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer value.</param>
    /// <returns>The number of decimal characters required to represent the value.</returns>
    public static int CountDecimalChars(ulong value)
    {
        if (value == 0)
        {
            return 1;
        }

        int bits = 64 - BitOperations.LeadingZeroCount(value);
        int log10 = (bits * 1233) >> 12;
        return log10 + ((value >= Pow10B[log10]) ? 1 : 0);
    }

    /// <summary>
    /// Tries to load a resource from the specified assembly and returns it as a byte array.
    /// </summary>
    /// <param name="assembly">The assembly to load the resource from. If null, the executing assembly is used.</param>
    /// <param name="resourceName">The name of the resource to load.<br/>
    /// e.g. 'Resources.Name.tinyhand'.</param>
    /// <param name="data">When this method returns, contains the loaded resource data if successful; otherwise, null.</param>
    /// <returns><c>true</c> if the resource was successfully loaded; otherwise, <c>false</c>.</returns>
    public static bool TryLoadResource(Assembly? assembly, string resourceName, [MaybeNullWhen(false)] out byte[] data)
    {
        try
        {
            assembly ??= Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + resourceName);
            if (stream is null)
            {
                data = default;
                return false;
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            data = ms.ToArray();
            return true;
        }
        catch
        {
            data = default;
            return false;
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> indicating a size mismatch.
    /// </summary>
    /// <param name="argumentName">The name of the argument.</param>
    /// <param name="size">The expected size.</param>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowSizeMismatchException(string argumentName, int size)
    {
        throw new ArgumentOutOfRangeException($"The {nameof(argumentName)} length must be {size} bytes.");
    }

    /// <summary>
    /// Trims the utf8 string at the first occurrence of a null byte.
    /// </summary>
    /// <param name="utf8">The input byte span.</param>
    /// <returns>A span of bytes trimmed at the first null byte.</returns>
    public static ReadOnlySpan<byte> TrimAtFirstNull(ReadOnlySpan<byte> utf8)
    {
        var firstNull = utf8.IndexOf((byte)0);
        if (firstNull < 0)
        {
            return utf8;
        }
        else
        {
            return utf8.Slice(0, firstNull);
        }
    }

    /// <summary>
    /// Trims the utf8 string at the first occurrence of a null byte.
    /// </summary>
    /// <param name="utf8">The input byte array.</param>
    /// <returns>A byte array trimmed at the first null byte.</returns>
    public static byte[] TrimAtFirstNull(byte[] utf8)
    {
        var firstNull = Array.IndexOf(utf8, (byte)0);
        if (firstNull < 0)
        {
            return utf8;
        }
        else
        {
            var trimmed = new byte[firstNull];
            Array.Copy(utf8, trimmed, firstNull);
            return trimmed;
        }
    }

    /*
#pragma warning disable SA1503 // Braces should not be omitted
    private const int P1 = 10;
    private const int P2 = 100;
    private const int P3 = 1000;
    private const int P4 = 10000;
    private const int P5 = 100000;
    private const int P6 = 1000000;
    private const int P7 = 10000000;
    private const int P8 = 100000000;
    private const int P9 = 1000000000;
    private const long P10 = 10000000000;
    private const long P11 = 100000000000;
    private const long P12 = 1000000000000;
    private const long P13 = 10000000000000;
    private const long P14 = 100000000000000;
    private const long P15 = 1000000000000000;
    private const long P16 = 10000000000000000;
    private const long P17 = 100000000000000000;
    private const long P18 = 1000000000000000000;

    /// <summary>
    /// Gets the length of the string representation of the specified number.
    /// </summary>
    /// <param name="number">The number to get the string length for.</param>
    /// <returns>The length of the string representation of the number.</returns>
    public static int CountDecimalChars(int number)
    {
        int add = 0;
        if (number < 0)
        {
            add = 1;
            number = -number;
        }

        // 1,2,3,4,5,6,7,8,9,10
        if (number < P4)
        {// 1,2,3,4
            if (number < P2)
            {// 1,2
                if (number < P1) return 1 + add;
                else return 2 + add;
            }
            else
            {// 3,4
                if (number < P3) return 3 + add;
                else return 4 + add;
            }
        }
        else
        {// 5,6,7,8,9,10
            if (number < P6)
            {// 5,6
                if (number < P5) return 5 + add;
                else return 6 + add;
            }
            else
            {// 7,8,9,10
                if (number < P8)
                {// 7,8
                    if (number < P7) return 7 + add;
                    else return 8 + add;
                }
                else
                {// 9,10
                    if (number < P9) return 9 + add;
                    else return 10 + add;
                }
            }
        }
    }

    /// <summary>
    /// Gets the length of the string representation of the specified number.
    /// </summary>
    /// <param name="number">The number to get the string length for.</param>
    /// <returns>The length of the string representation of the number.</returns>
    public static int CountDecimalChars(long number)
    {
        int add = 0;
        if (number < 0)
        {
            add = 1;
            number = -number;
        }

        // 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19
        if (number < P8)
        {// 1,2,3,4,5,6,7, 8
            if (number < P4)
            {// 1,2,3,4
                if (number < P2)
                {// 1,2
                    if (number < P1) return 1 + add;
                    else return 2 + add;
                }
                else
                {// 3,4
                    if (number < P3) return 3 + add;
                    else return 4 + add;
                }
            }
            else
            {// 5,6,7,8
                if (number < P6)
                {// 5, 6
                    if (number < P5) return 5 + add;
                    else return 6 + add;
                }
                else
                {// 7, 8
                    if (number < P7) return 7 + add;
                    else return 8 + add;
                }
            }
        }
        else
        {// 9,10,11,12,13,14,15,16,17,18,19
            if (number < P12)
            {// 9,10,11,12
                if (number < P10)
                {// 9, 10
                    if (number < P9) return 9 + add;
                    else return 10 + add;
                }
                else
                {// 11, 12
                    if (number < P11) return 11 + add;
                    else return 12 + add;
                }
            }
            else
            {// 13,14,15,16,17,18,19
                if (number < P15)
                {// 13,14,15
                    if (number < P13)
                    {// 13
                        return 13 + add;
                    }
                    else
                    {// 14, 15
                        if (number < P14) return 14 + add;
                        else return 15 + add;
                    }
                }
                else
                {// 16,17,18,19
                    if (number < P17)
                    {// 16, 17
                        if (number < P16) return 16 + add;
                        else return 17 + add;
                    }
                    else
                    {// 18, 19
                        if (number < P18) return 18 + add;
                        else return 19 + add;
                    }
                }
            }
        }
    }
#pragma warning restore SA1503 // Braces should not be omitted
    */

    /// <summary>
    /// Parses the value from the provided source or environment variable and assigns it to the <paramref name="instance"/> parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value to parse.</typeparam>
    /// <param name="source">The source value to parse.</param>
    /// <param name="variable">The name of the environment variable to check if the source value is empty.</param>
    /// <param name="instance">When this method returns, contains the parsed value if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <param name="conversionOptions">Conversion options that may influence the parsing behavior.</param>
    /// <returns><c>true</c> if the value was successfully parsed; otherwise, <c>false</c>.</returns>
    public static bool TryParseFromSourceOrEnvironmentVariable<T>(ReadOnlySpan<char> source, string variable, [MaybeNullWhen(false)] out T instance, IConversionOptions? conversionOptions = default)
        where T : IStringConvertible<T>
    {
        // 1st Source
        if (T.TryParse(source, out instance!, out _, conversionOptions))
        {// source.Length > 0 &&
            return true;
        }

        // 2nd: Environment variable
        if (Environment.GetEnvironmentVariable(variable) is { } source2)
        {
            if (T.TryParse(source2, out instance!, out _, conversionOptions))
            {
                return true;
            }
        }

        instance = default;
        return false;
    }

    /// <summary>
    /// Parses the value from the provided environment variable and assigns it to the <paramref name="instance"/> parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value to parse.</typeparam>
    /// <param name="variable">The name of the environment variable to check if the source value is empty.</param>
    /// <param name="instance">When this method returns, contains the parsed value if successful; otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <param name="conversionOptions">Conversion options that may influence the parsing behavior.</param>
    /// <returns><c>true</c> if the value was successfully parsed; otherwise, <c>false</c>.</returns>
    public static bool TryParseFromEnvironmentVariable<T>(string variable, [MaybeNullWhen(false)] out T instance, IConversionOptions? conversionOptions = default)
        where T : IStringConvertible<T>
    {
        if (Environment.GetEnvironmentVariable(variable) is { } source)
        {
            return T.TryParse(source, out instance, out _, conversionOptions);
        }
        else
        {
            instance = default;
            return false;
        }
    }

    /// <summary>
    /// Converts the object to its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to convert.</param>
    /// <param name="conversionOptions">Conversion options that may influence the formatting behavior.</param>
    /// <returns>The string representation of the object.</returns>
    [SkipLocalsInit]
    public static string ConvertToString<T>(this T obj, IConversionOptions? conversionOptions = default)
        where T : IStringConvertible<T>
    { // MemoryMarshal.CreateSpan<char>(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);
        var length = obj.GetStringLength();
        if (length < 0)
        {
            length = T.MaxStringLength;
        }

        if (length < 0)
        {
            return string.Empty;
        }

        char[]? rentArray = null;
        Span<char> span = length <= Arc.BaseConstants.StackallocThreshold ?
            stackalloc char[length] : (rentArray = ArrayPool<char>.Shared.Rent(length));

        try
        {
            if (obj.TryFormat(span, out var written, conversionOptions))
            {
                return new string(span.Slice(0, written));
            }
            else
            {
                return string.Empty;
            }
        }
        finally
        {
            if (rentArray != null)
            {
                ArrayPool<char>.Shared.Return(rentArray);
            }
        }
    }

    /// <summary>
    /// Converts the object to its UTF-8 byte array representation.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object to convert.</param>
    /// <param name="conversionOptions">Conversion options that may influence the formatting behavior.</param>
    /// <returns>The UTF-8 byte array representation of the object.</returns>
    [SkipLocalsInit]
    public static byte[] ConvertToUtf8<T>(this T obj, IConversionOptions? conversionOptions = default)
        where T : IStringConvertible<T>
    { // MemoryMarshal.CreateSpan<char>(ref MemoryMarshal.GetReference(str.AsSpan()), str.Length);
        var length = obj.GetStringLength();
        if (length < 0)
        {
            length = T.MaxStringLength;
        }

        if (length < 0)
        {
            return Array.Empty<byte>();
        }

        char[]? rentArray = null;
        Span<char> span = length <= Arc.BaseConstants.StackallocThreshold ?
            stackalloc char[length] : (rentArray = ArrayPool<char>.Shared.Rent(length));

        try
        {
            if (obj.TryFormat(span, out var written, conversionOptions))
            {
                var result = span.Slice(0, written);
                var count = Encoding.UTF8.GetByteCount(result);
                var array = new byte[count];
                length = Encoding.UTF8.GetBytes(result, array);
                Debug.Assert(length == array.Length);
                return array;
            }
            else
            {
                return Array.Empty<byte>();
            }
        }
        finally
        {
            if (rentArray != null)
            {
                ArrayPool<char>.Shared.Return(rentArray);
            }
        }
    }
}
