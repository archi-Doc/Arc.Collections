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

            /*else
            {
                method = StandardResolver.Instance.TryGet<T>();
            }

            if (method == null)
            {
                throw new FormatterNotRegisteredException(typeof(T).FullName + " is not registered in resolver.");
            }*/

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
