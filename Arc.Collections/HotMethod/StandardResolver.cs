﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Default composited resolver.
/// </summary>
public sealed class StandardResolver : IHotMethodResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly StandardResolver Instance = new StandardResolver();

    private StandardResolver()
    {
    }

    public IHotMethod<T>? TryGet<T>()
    {
        return MethodCache<T>.Method;
    }

    private static class MethodCache<T>
    {
        public static readonly IHotMethod<T>? Method;

        static MethodCache()
        {
            MethodCache<T>.Method = (IHotMethod<T>)Activator.CreateInstance(typeof(StandardMethod<>).MakeGenericType(typeof(T)))!;
        }
    }

    public IHotMethod2<TKey, TValue>? TryGet<TKey, TValue>()
    {
        return MethodCache2<TKey, TValue>.Method;
    }

    private static class MethodCache2<TKey, TValue>
    {
        public static readonly IHotMethod2<TKey, TValue>? Method;

        static MethodCache2()
        {
            MethodCache2<TKey, TValue>.Method = (IHotMethod2<TKey, TValue>)Activator.CreateInstance(typeof(StandardMethod2<,>).MakeGenericType(typeof(TKey), typeof(TValue)))!;
        }
    }
}
