// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Arc;

/// <summary>
/// An interface for converting between an object and a utf-16 string.<br/>
/// Required: TryParse() and TryFormat()<br/>
/// Either one is required: GetStringLength() or MaxStringLength.
/// </summary>
/// <typeparam name="T">The type of object to be converted.</typeparam>
public interface IStringConvertible<T>
    where T : IStringConvertible<T>
{
    /// <summary>
    /// Convert a <see cref="char"/> (utf-16) span to an object.
    /// </summary>
    /// <param name="source">The source utf-16 span.</param>
    /// <param name="object">An object converted from the utf-16 span.</param>
    /// <param name="read">The number of chars read from the source span.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    static abstract bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out T? @object, out int read);

    /// <summary>
    ///  Gets the maximum length of the utf-16 encoded data.<br/>
    ///  Implementation of either <see cref="GetStringLength"/> or <see cref="MaxStringLength"/> is required.<br/>
    ///  If not implemented, please return -1 instead of throwing an exception.
    /// </summary>
    /// <returns>The maximum utf-16 encoded length.</returns>
    static abstract int MaxStringLength { get; }

    /// <summary>
    ///  Get the actual length of the utf-16 encoded data.<br/>
    ///  Implementation of either <see cref="GetStringLength"/> or <see cref="MaxStringLength"/> is required.<br/>
    ///  If not implemented, please return -1 instead of throwing an exception.
    /// </summary>
    /// <returns>The actual utf-16 encoded length.</returns>
    int GetStringLength();

    /// <summary>
    /// Convert an object to a <see cref="char"/> (utf-16) span.
    /// </summary>
    /// <param name="destination">The destination span of <see cref="char"/> (utf-16).<br/>
    /// Allocate an array of a length greater than or equal to <seealso cref="GetStringLength"/> or <seealso cref="MaxStringLength"/>.</param>
    /// <param name="written">The number of bytes that were written in destination.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    bool TryFormat(Span<char> destination, out int written);
}
