// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Resolves specialized span comparisons and ordered-tree searches.
/// </summary>
public interface IHotMethodResolver
{
    /// <summary>
    /// Gets an <see cref="IHotMethod{T}"/> instance that can process some type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of value to be processed.</typeparam>
    /// <returns><see cref="IHotMethod{T}"/>, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
    IHotMethod<T>? TryGet<T>();

    /// <summary>
    /// Gets an <see cref="IHotMethod2{TKey, TValue}"/> instance that can process some type.
    /// </summary>
    /// <typeparam name="TKey">The key to be processed.</typeparam>
    /// <typeparam name="TValue">The value to be processed.</typeparam>
    /// <returns>The specialized tree search, or <see langword="null"/> if this resolver has none.</returns>
    IHotMethod2<TKey, TValue>? TryGet<TKey, TValue>();
}

/// <summary>
/// Selects specialized comparison implementations for supported types and default comparers.
/// </summary>
public static class HotMethodResolver
{
    /// <summary>
    /// Gets the specialized <see cref="IHotMethod{T}"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to be processed.</typeparam>
    /// <param name="comparer">The comparer in use. A specialized implementation is returned only for <see cref="Comparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotMethod<T>? Get<T>(IComparer<T> comparer)
    {
        IHotMethod<T>? method = null;

        if (comparer == Comparer<T>.Default)
        {
            method = PrimitiveResolver.Instance.TryGet<T>();
        }

        return method;
    }

    /// <summary>
    /// Gets the specialized <see cref="IHotMethod2{TKey, TValue}"/> for <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="comparer">The comparer in use. A specialized implementation is returned only for <see cref="Comparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotMethod2<TKey, TValue>? Get<TKey, TValue>(IComparer<TKey> comparer)
    {
        IHotMethod2<TKey, TValue>? method = null;

        if (comparer == Comparer<TKey>.Default)
        {
            method = PrimitiveResolver.Instance.TryGet<TKey, TValue>();
        }

        return method;
    }

    /// <summary>
    /// Gets the specialized <see cref="IHotMethod2{TKey, TValue}"/> for <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="comparer">The equality comparer in use. A specialized implementation is returned only for <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotMethod2<TKey, TValue>? Get<TKey, TValue>(IEqualityComparer<TKey> comparer)
    {
        IHotMethod2<TKey, TValue>? method = null;

        if (comparer == EqualityComparer<TKey>.Default)
        {
            method = PrimitiveResolver.Instance.TryGet<TKey, TValue>();
        }

        return method;
    }
}
