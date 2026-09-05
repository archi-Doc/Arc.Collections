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
/// Null elements are supported. Duplicate elements are allowed only when enabled at construction.
/// </remarks>
public sealed class UnorderedSet<T> : IEnumerable<T>
{
    private readonly UnorderedMap<T, byte> map;

    /// <summary>
    /// Initializes an empty set with the specified duplicate behavior.
    /// </summary>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(bool allowDuplicate = false)
        : this(0, null, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(int capacity, bool allowDuplicate = false)
        : this(capacity, null, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(IEqualityComparer<T>? comparer, bool allowDuplicate = false)
        : this(0, comparer, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes an empty set with the specified capacity, comparer, and duplicate behavior.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(int capacity, IEqualityComparer<T>? comparer, bool allowDuplicate)
    {
        this.map = new UnorderedMap<T, byte>(capacity, comparer, allowDuplicate);
    }

    /// <summary>
    /// Initializes a set containing the specified elements.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    public UnorderedSet(IEnumerable<T> collection)
        : this(collection, null, false)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        : this(collection, comparer, false)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified duplicate behavior.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(IEnumerable<T> collection, bool allowDuplicate)
        : this(collection, null, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes a set containing the specified elements using the specified comparer and duplicate behavior.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer, bool allowDuplicate)
    {
        ArgumentNullException.ThrowIfNull(collection);

        this.map = new UnorderedMap<T, byte>(
            GetCollectionCount(collection),
            comparer,
            allowDuplicate);

        foreach (var item in collection)
        {
            this.map.Add(item, 0);
        }
    }

    /// <summary>
    /// Gets a value indicating whether duplicate elements are allowed.
    /// </summary>
    public bool AllowDuplicate => this.map.AllowDuplicate;

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the current capacity.
    /// </summary>
    public int Capacity => this.map.Capacity;

    /// <summary>
    /// Gets the comparer used for elements.
    /// </summary>
    public IEqualityComparer<T> Comparer => this.map.Comparer;

    /// <summary>
    /// Adds the specified element to the set.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element was added; otherwise, <see langword="false"/>.</returns>
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
    /// Removes all elements from the set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Copies the elements to a new array.
    /// </summary>
    /// <returns>A new array containing the elements.</returns>
    public T[] ToArray()
    {
        var array = new T[this.Count];
        this.CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Copies the elements to the specified array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index.</param>
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
