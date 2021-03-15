// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collection.HotMethod
{
    /// <summary>
    /// Allows querying for a formatter for serializing or deserializing a particular <see cref="Type" />.
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
        /// <returns><see cref="IHotMethod2{TKey, TValue}"/>.</returns>
        IHotMethod2<TKey, TValue>? TryGet<TKey, TValue>()
            where TKey : notnull;
    }

    public static class HotMethodResolver
    {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IHotMethod2<TKey, TValue>? Get<TKey, TValue>(IComparer<TKey> comparer)
            where TKey : notnull
        {
            IHotMethod2<TKey, TValue>? method = null;

            if (comparer == Comparer<TKey>.Default)
            {
                method = PrimitiveResolver.Instance.TryGet<TKey, TValue>();
            }

            return method;
        }
    }

    public class FormatterNotRegisteredException : Exception
    {
        public FormatterNotRegisteredException(string message)
            : base(message)
        {
        }
    }
}
