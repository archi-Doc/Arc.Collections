// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Arc;

/// <summary>
/// An interface for converting between an object and a utf-8 string.<br/>
/// Required: TryParse() and TryFormat()<br/>
/// Either one is required: GetStringLength() or MaxStringLength.
/// </summary>
/// <typeparam name="T">The type of object to be converted.</typeparam>
public interface IUtf8Convertible<T>
    where T : IUtf8Convertible<T>
{
    /// <summary>
    /// Convert a <see cref="byte"/> (utf-8) span to an object.
    /// </summary>
    /// <param name="source">The source utf-8 span.</param>
    /// <param name="object">An object converted from the utf-8 span.</param>
    /// <param name="read">The number of bytes read from the source span.</param>
    /// <param name="conversionOptions">Conversion options that may influence the parsing behavior.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    static abstract bool TryParse(ReadOnlySpan<byte> source, [MaybeNullWhen(false)] out T? @object, out int read, IConversionOptions? conversionOptions);

    /// <summary>
    ///  Gets the maximum length of the utf-8 encoded data.<br/>
    ///  Implementation of either <see cref="GetStringLength"/> or <see cref="MaxStringLength"/> is required.
    /// </summary>
    /// <returns>The maximum utf-8 encoded length.</returns>
    static abstract int MaxStringLength { get; }

    /// <summary>
    ///  Get the actual length of the utf-8 encoded data.<br/>
    ///  Note that the length may change depending on the implementation of <see cref="IConversionOptions"/> and <see cref="TryFormat(Span{byte}, out int, IConversionOptions?)"/>.<br/>
    ///  Implementation of either <see cref="GetStringLength"/> or <see cref="MaxStringLength"/> is required.
    /// </summary>
    /// <returns>The actual utf-8 encoded length.</returns>
    int GetStringLength();

    /// <summary>
    /// Convert an object to a <see cref="byte"/> (utf-8) span.
    /// </summary>
    /// <param name="destination">The destination span of <see cref="byte"/> (utf-8).<br/>
    /// Allocate an array of a length greater than or equal to <seealso cref="GetStringLength"/> or <seealso cref="MaxStringLength"/>.</param>
    /// <param name="written">The number of bytes that were written in destination.</param>
    /// <param name="conversionOptions">Conversion options that may influence the formatting behavior.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    bool TryFormat(Span<byte> destination, out int written, IConversionOptions? conversionOptions);
}
