// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Arc;

/// <summary>
/// Defines span-based parsing and formatting of UTF-16 text.
/// </summary>
/// <typeparam name="T">The type of object to be converted.</typeparam>
/// <remarks>
/// Implement all members. At least one of <see cref="GetStringLength" /> and
/// <see cref="MaxStringLength" /> must provide a nonnegative length in characters;
/// the other may return -1. Formatting options may affect the required length.
/// </remarks>
public interface IStringConvertible<T>
    where T : IStringConvertible<T>
{
    /// <summary>
    /// Attempts to parse an instance from a UTF-16 span and reports the consumed characters.
    /// </summary>
    /// <param name="source">The source UTF-16 span.</param>
    /// <param name="object">An object converted from the UTF-16 span.</param>
    /// <param name="read">The number of chars read from the source span.</param>
    /// <param name="conversionOptions">Conversion options that may influence the parsing behavior.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    static abstract bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out T? @object, out int read, IConversionOptions? conversionOptions = default);

    /// <summary>
    /// Gets the maximum formatted length in characters, or -1 when unavailable.
    /// </summary>
    /// <value>The maximum number of characters, or -1 when unavailable.</value>
    static abstract int MaxStringLength { get; }

    /// <summary>
    /// Gets the formatted length in characters, or -1 when unavailable.
    /// Conversion options may change the required length.
    /// </summary>
    /// <returns>The number of characters, or -1 when unavailable.</returns>
    int GetStringLength();

    /// <summary>
    /// Formats this instance into a UTF-16 span.
    /// </summary>
    /// <param name="destination">The destination span of <see cref="char"/> (UTF-16).<br/>
    /// Use a nonnegative length from <see cref="GetStringLength"/> or <see cref="MaxStringLength"/>,
    /// allowing for any additional space required by the conversion options.</param>
    /// <param name="written">The number of UTF-16 characters written to the destination.</param>
    /// <param name="conversionOptions">Conversion options that may influence the formatting behavior.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default);
}
