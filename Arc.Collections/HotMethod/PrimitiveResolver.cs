// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Arc.Collections.HotMethod
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

        private static readonly Dictionary<Type, Type> TypeToMethod2 = new()
        {
            // Primitive
            { typeof(byte), typeof(UInt8Method2<>) },
            { typeof(sbyte), typeof(Int8Method2<>) },
            { typeof(ushort), typeof(UInt16Method2<>) },
            { typeof(short), typeof(Int16Method2<>) },
            { typeof(uint), typeof(UInt32Method2<>) },
            { typeof(int), typeof(Int32Method2<>) },
            { typeof(ulong), typeof(UInt64Method2<>) },
            { typeof(long), typeof(Int64Method2<>) },
            { typeof(float), typeof(SingleMethod2<>) },
            { typeof(double), typeof(DoubleMethod2<>) },
            // { typeof(string), typeof(StringMethod2<>) }, // Slow
            { typeof(DateTime), typeof(DateTimeMethod2<>) },
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
        {
            return MethodCache2<TKey, TValue>.Method;
        }

        private static class MethodCache2<TKey, TValue>
        {
            public static readonly IHotMethod2<TKey, TValue>? Method;

            static MethodCache2()
            {
                if (PrimitiveResolver.TypeToMethod2.TryGetValue(typeof(TKey), out var type))
                {
                    MethodCache2<TKey, TValue>.Method = (IHotMethod2<TKey, TValue>)Activator.CreateInstance(type.MakeGenericType(typeof(TValue)))!;
                }
            }
        }
    }
}
