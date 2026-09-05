// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Marks specialized span comparison implementations.
/// </summary>
public interface IHotMethod
{
}

/// <summary>
/// Defines specialized comparisons and bound searches over sorted spans.
/// </summary>
/// <typeparam name="T">The type to be processed.</typeparam>
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
