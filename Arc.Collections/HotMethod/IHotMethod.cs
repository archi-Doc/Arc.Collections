// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Marks specialized span bound-search implementations.
/// </summary>
public interface IHotMethod
{
}

/// <summary>
/// Defines specialized bound searches over spans sorted in ascending order.<br/>
/// Comparisons match <see cref="System.Collections.Generic.Comparer{T}.Default"/> exactly (for example, NaN sorts
/// before every other floating-point value); results are unspecified if the span is not sorted that way.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IHotMethod<T> : IHotMethod
{
    /// <summary>
    /// Returns the index of the first element that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="span">The sorted span to search.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// The index of the first element that is greater than or equal to <paramref name="value"/>,
    /// or <paramref name="span"/>.Length if no such element exists.
    /// </returns>
    int LowerBound(ReadOnlySpan<T> span, T value);

    /// <summary>
    /// Returns the index of the first element that is greater than the specified value.
    /// </summary>
    /// <param name="span">The sorted span to search.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// The index of the first element that is greater than <paramref name="value"/>,
    /// or <paramref name="span"/>.Length if no such element exists.
    /// </returns>
    int UpperBoundExclusive(ReadOnlySpan<T> span, T value);
}
