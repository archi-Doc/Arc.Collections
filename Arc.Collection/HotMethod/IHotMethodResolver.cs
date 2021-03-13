// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Arc.Collection.HotMethod
{
    /// <summary>
    /// Allows querying for a formatter for serializing or deserializing a particular <see cref="Type" />.
    /// </summary>
    public interface IHotMethodResolver
    {
        /// <summary>
        /// Gets an <see cref="IHotMethod{T}"/> instance that can serialize or deserialize some type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to be serialized or deserialized.</typeparam>
        /// <returns>A formatter, if this resolver supplies one for type <typeparamref name="T"/>; otherwise <c>null</c>.</returns>
        IHotMethod<T>? TryGetFormatter<T>();
    }

    public static class ResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITinyhandFormatter<T> GetFormatter<T>(this IFormatterResolver resolver)
        {
            ITinyhandFormatter<T>? formatter;

            formatter = resolver.TryGetFormatter<T>();
            if (formatter == null)
            {
                Throw(typeof(T), resolver);
            }

            return formatter!;
        }
    }
