// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Stores elements in a hash table with optional duplicates.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
/// <remarks>
/// Null elements are supported and are never passed to the comparer. Duplicate elements are allowed only when enabled at construction.<br/>
/// Enumeration order is unspecified. The set wraps <see cref="UnorderedMap{TKey, TValue}"/> and shares its enumeration and thread-safety rules.
/// </remarks>
public sealed class UnorderedSet<T> : IEnumerable<T>
{
    private readonly UnorderedMap<T, byte> map;

    /// <summary>
    /// Initializes an empty set with the specified duplicate behavior.
    /// </summary>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    public UnorderedSet(bool allowDuplicates = false)
        : this(0, null, allowDuplicates)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified capacity and duplicate behavior.
    /// </summary>
    /// <param name="capacity">The initial capacity, rounded up to a power of two (at least 4).</param>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than 2^30.</exception>
    public UnorderedSet(int capacity, bool allowDuplicates = false)
        : this(capacity, null, allowDuplicates)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified comparer and duplicate behavior.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    public UnorderedSet(IEqualityComparer<T>? comparer, bool allowDuplicates = false)
        : this(0, comparer, allowDuplicates)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified capacity, comparer, and duplicate behavior.
    /// </summary>
    /// <param name="capacity">The initial capacity, rounded up to a power of two (at least 4).</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than 2^30.</exception>
    public UnorderedSet(int capacity, IEqualityComparer<T>? comparer, bool allowDuplicates)
    {
        this.map = new UnorderedMap<T, byte>(capacity, comparer, allowDuplicates);
    }

    /// <summary>
    /// Initializes a set containing the specified elements.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public UnorderedSet(IEnumerable<T> collection)
        : this(collection, null, false)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(collection, comparer, false)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified duplicate behavior.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public UnorderedSet(IEnumerable<T> collection, bool allowDuplicates)
        : this(collection, null, allowDuplicates)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified comparer and duplicate behavior.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicates"><see langword="true"/> to allow duplicate elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer, bool allowDuplicates)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is UnorderedSet<T> set)
        {
            // Presize from the source's count, which is exact with the same comparer and only an upper bound with a different one
            // (as for ICollection<T> sources). Skip presizing when the source's duplicates must be merged,
            // since its count could then far exceed the result.
            var capacity = set.AllowDuplicates && !allowDuplicates ? 0 : set.Count;
            this.map = new UnorderedMap<T, byte>(capacity, comparer, allowDuplicates);

            // With the same comparer, the stored hash codes are reused and no element is probed.
            if (!this.map.TryCopyFrom(set.map))
            {
                foreach (var item in set)
                {
                    this.map.Add(item, 0);
                }
            }

            return;
        }

        this.map = new UnorderedMap<T, byte>(
            GetCollectionCount(collection),
            comparer,
            allowDuplicates);

        foreach (var item in collection)
        {
            this.map.Add(item, 0);
        }
    }

    /// <summary>
    /// Gets a value indicating whether duplicate elements are allowed.
    /// </summary>
    public bool AllowDuplicates => this.map.AllowDuplicates;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the number of allocated node slots. It doubles when an element is added while all slots are in use.
    /// </summary>
    public int Capacity => this.map.Capacity;

    /// <summary>
    /// Gets the comparer used for non-null elements.
    /// </summary>
    public IEqualityComparer<T> Comparer => this.map.Comparer;

    /// <summary>
    /// Adds the specified element to the set.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element was added; <see langword="false"/> if duplicates are disabled and an equal element already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T? item)
        => this.map.Add(item, 0).NewlyAdded;

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T? item)
        => this.map.ContainsKey(item);

    /// <summary>
    /// Removes one matching element from the set.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T? item)
        => this.map.Remove(item);

    /// <summary>
    /// Removes all elements from the set. The capacity is unchanged.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Copies the elements to a new array.
    /// </summary>
    /// <returns>A new array containing the elements, or the shared empty array when the set is empty.</returns>
    public T[] ToArray()
    {
        if (this.Count == 0)
        {
            return Array.Empty<T>();
        }

        var array = new T[this.Count];
        this.CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Copies the elements to the specified array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the length of <paramref name="array"/>.</exception>
    /// <exception cref="ArgumentException">The destination array is too small.</exception>
    public void CopyTo(T[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.Count)
        {
            throw new ArgumentException(
                "The destination array is too small.",
                nameof(array));
        }

        foreach (var item in this.map.Keys)
        {
            array[index++] = item;
        }
    }

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnorderedMap<T, byte>.KeyEnumerable.Enumerator GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetCollectionCount(IEnumerable<T> collection)
    {
        if (collection is ICollection<T> collection1)
        {
            return collection1.Count;
        }

        if (collection is IReadOnlyCollection<T> collection2)
        {
            return collection2.Count;
        }

        return 0;
    }
}
