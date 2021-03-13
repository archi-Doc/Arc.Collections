// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collection.HotMethod
{
    /// <summary>
    /// A base interface for <see cref="IHotMethod{T}"/> so that all generic implementations
    /// can be detected by a common base type.
    /// </summary>
    public interface IHotMethod
    {
    }

    /// <summary>
    /// The contract for processing methods of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be processed.</typeparam>
    public interface IHotMethod<T> : IHotMethod
    {
        /// <summary>
        /// Searches a list for the specific value.
        /// </summary>
        /// <param name="array">The sorted one-dimensional, zero-based Array to search.</param>
        /// <param name="index">The starting index of the range to search.</param>
        /// <param name="length">The length of the range to search.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>The index of the specified value in list. If the value is not found, the negative number returned is the bitwise complement of the index of the first element that is larger than value.</returns>
        int BinarySearch(T[] array, int index, int length, T value);
    }
}
