// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Arc.Collection.HotMethod
{
    /// <summary>
    /// Default composited resolver.
    /// </summary>
    public sealed class PrimitiveResolver : IHotMethodResolver
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

        public IHotMethod2<TKey, TValue>? TryGet<TKey, TValue>()
            where TKey : notnull
        {
            return MethodCache2<TKey, TValue>.Method;
        }

        private static class MethodCache2<TKey, TValue>
            where TKey : notnull
        {
            public static readonly IHotMethod2<TKey, TValue>? Method;

            static MethodCache2()
            {
                if (PrimitiveResolver.TypeToMethod.TryGetValue(typeof(TKey), out var obj))
                {
                    MethodCache2<TKey, TValue>.Method = (IHotMethod2<TKey, TValue>)obj;
                }
            }
        }
    }
}
