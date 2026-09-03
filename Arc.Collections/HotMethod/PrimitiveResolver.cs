// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Arc.Collections.HotMethod;

/// <summary>
/// Default composited resolver.
/// </summary>
internal sealed class PrimitiveResolver : IHotMethodResolver
{
    /// <summary>
    /// The singleton instance that can be used.
    /// </summary>
    public static readonly PrimitiveResolver Instance = new PrimitiveResolver();

    private static readonly Dictionary<Type, object> TypeToMethod = new()
    {
        // Primitive
        { typeof(byte), UInt8Method.Instance },
        { typeof(sbyte), Int8Method.Instance },
        { typeof(ushort), UInt16Method.Instance },
        { typeof(short), Int16Method.Instance },
        { typeof(uint), UInt32Method.Instance },
        { typeof(int), Int32Method.Instance },
        { typeof(ulong), UInt64Method.Instance },
        { typeof(long), Int64Method.Instance },
        { typeof(UInt128), UInt128Method.Instance },
        { typeof(Int128), Int128Method.Instance },
        { typeof(float), SingleMethod.Instance },
        { typeof(double), DoubleMethod.Instance },
        // { typeof(string), StringMethod.Instance }, // Slow
        { typeof(DateTime), DateTimeMethod.Instance },
    };

    private PrimitiveResolver()
    {
    }

    public IHotMethod<T>? TryGet<T>()
    {
        return MethodCache<T>.Method;
    }

    public IHotMethod2<TKey, TValue>? TryGet<TKey, TValue>()
    {
        return MethodCache2<TKey, TValue>.Method;
    }

    /// <summary>
    /// Creates the <see cref="IHotMethod2{TKey, TValue}"/> for <typeparamref name="TKey"/>, if one exists.<br/>
    /// The closed generic types are named explicitly (instead of <see cref="Type.MakeGenericType(Type[])"/>)
    /// so that Native AOT can generate the code ahead of time.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The specialized implementation, or <see langword="null"/> if none is available.</returns>
    private static object? CreateMethod2<TKey, TValue>()
    {
        // Primitive
        if (typeof(TKey) == typeof(byte))
        {
            return new UInt8Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(sbyte))
        {
            return new Int8Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(ushort))
        {
            return new UInt16Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(short))
        {
            return new Int16Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(uint))
        {
            return new UInt32Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(int))
        {
            return new Int32Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(ulong))
        {
            return new UInt64Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(long))
        {
            return new Int64Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(UInt128))
        {
            return new UInt128Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(Int128))
        {
            return new Int128Method2<TValue>();
        }
        else if (typeof(TKey) == typeof(float))
        {
            return new SingleMethod2<TValue>();
        }
        else if (typeof(TKey) == typeof(double))
        {
            return new DoubleMethod2<TValue>();
        }
        else if (typeof(TKey) == typeof(DateTime))
        {
            return new DateTimeMethod2<TValue>();
        }
        else
        {// typeof(string) => StringMethod2<TValue> is intentionally not handled (slow).
            return null;
        }
    }

    private static class MethodCache<T>
    {
        public static readonly IHotMethod<T>? Method;

        static MethodCache()
        {
            if (PrimitiveResolver.TypeToMethod.TryGetValue(typeof(T), out var obj))
            {
                MethodCache<T>.Method = (IHotMethod<T>)obj;
            }
        }
    }

    private static class MethodCache2<TKey, TValue>
    {
        public static readonly IHotMethod2<TKey, TValue>? Method;

        static MethodCache2()
        {
            MethodCache2<TKey, TValue>.Method = (IHotMethod2<TKey, TValue>?)PrimitiveResolver.CreateMethod2<TKey, TValue>();
        }
    }
}
