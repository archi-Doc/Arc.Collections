// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Resolves specialized span bound searches and ordered-tree searches.<br/>
/// The library never calls custom implementations of this interface: <see cref="HotMethodResolver"/>
/// and the built-in collections use only the built-in resolver.
/// </summary>
public interface IHotMethodResolver
{
    /// <summary>
    /// Gets the specialized span search for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The specialized implementation, or <see langword="null"/> if this resolver has none for <typeparamref name="T"/>.</returns>
    IHotMethod<T>? GetHotMethod<T>();

    /// <summary>
    /// Gets the specialized tree search for keys of type <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The specialized implementation, or <see langword="null"/> if this resolver has none for <typeparamref name="TKey"/>.</returns>
    IHotTreeMethod<TKey, TValue>? GetHotTreeMethod<TKey, TValue>();
}

/// <summary>
/// Selects the built-in specialized span and tree searches for supported types when the default comparer is used.<br/>
/// Supported types: <see cref="byte"/>, <see cref="sbyte"/>, <see cref="ushort"/>, <see cref="short"/>,
/// <see cref="uint"/>, <see cref="int"/>, <see cref="ulong"/>, <see cref="long"/>, <see cref="UInt128"/>,
/// <see cref="Int128"/>, <see cref="float"/>, <see cref="double"/>, and <see cref="DateTime"/>.
/// The returned implementations are stateless, thread-safe, and cached (calls with the same type arguments
/// return the same instance); their ordering matches <see cref="Comparer{T}.Default"/>.
/// </summary>
public static class HotMethodResolver
{
    /// <summary>
    /// Gets the specialized <see cref="IHotMethod{T}"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="comparer">The comparer in use. A specialized implementation is returned only for <see cref="Comparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotMethod<T>? Get<T>(IComparer<T> comparer)
    {
        IHotMethod<T>? method = null;

        if (comparer == Comparer<T>.Default)
        {
            method = PrimitiveResolver.Instance.GetHotMethod<T>();
        }

        return method;
    }

    /// <summary>
    /// Gets the specialized <see cref="IHotTreeMethod{TKey, TValue}"/> for <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="comparer">The comparer in use. A specialized implementation is returned only for <see cref="Comparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotTreeMethod<TKey, TValue>? Get<TKey, TValue>(IComparer<TKey> comparer)
    {
        IHotTreeMethod<TKey, TValue>? method = null;

        if (comparer == Comparer<TKey>.Default)
        {
            method = PrimitiveResolver.Instance.GetHotTreeMethod<TKey, TValue>();
        }

        return method;
    }

    /// <summary>
    /// Gets the specialized <see cref="IHotTreeMethod{TKey, TValue}"/> for <typeparamref name="TKey"/>,
    /// the same instance that <see cref="Get{TKey, TValue}(IComparer{TKey})"/> returns for the default comparer.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="comparer">The equality comparer in use. A specialized implementation is returned only for <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHotTreeMethod<TKey, TValue>? Get<TKey, TValue>(IEqualityComparer<TKey> comparer)
    {
        IHotTreeMethod<TKey, TValue>? method = null;

        if (comparer == EqualityComparer<TKey>.Default)
        {
            method = PrimitiveResolver.Instance.GetHotTreeMethod<TKey, TValue>();
        }

        return method;
    }
}
