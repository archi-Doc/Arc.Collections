// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace Arc.Collection.HotMethod
{
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
                MethodCache<T>.Method = (IHotMethod<T>)Activator.CreateInstance(typeof(StandardMethod<>).MakeGenericType(typeof(T)));
            }
        }
    }
}
