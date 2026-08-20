// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collections;

#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a collection of objects maintained in sorted order (ascending by default).
/// <br/><see cref="OrderedMultiSet{T}"/> uses a red-black tree and linked list to store objects.
/// <br/><see cref="OrderedMultiSet{T}"/> allows duplicate elements.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class OrderedMultiSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
{
    private readonly OrderedMultiMap<T, int> map;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="reverse">true to reverse the default comparison order.</param>
    public OrderedMultiSet(bool reverse = false)
    {
        this.map = new(reverse);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    /// <param name="reverse">true to reverse the comparison order.</param>
    public OrderedMultiSet(IComparer<T> comparer, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        this.map = new(comparer, reverse);
    }

    /// <summary>
    /// Initializes a new instance by copying the specified collection.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    /// <param name="reverse">true to reverse the default comparison order.</param>
    public OrderedMultiSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a new instance by copying the specified collection.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    /// <param name="reverse">true to reverse the comparison order.</param>
    public OrderedMultiSet(
        IEnumerable<T> collection,
        IComparer<T> comparer,
        bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(comparer);

        this.map = new(comparer, reverse);

        foreach (var item in collection)
        {
            this.map.Add(item, 0);
        }
    }

    #region Main

    /// <summary>
    /// Gets the number of elements in the set, including duplicates.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the first node in the set.
    /// </summary>
    public OrderedMultiMap<T, int>.Node? First => this.map.First;

    /// <summary>
    /// Gets the last node in the set.
    /// </summary>
    public OrderedMultiMap<T, int>.Node? Last => this.map.Last;

    /// <summary>
    /// Adds an element to the set.
    /// <br/>Duplicate elements are allowed.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The element to add.</param>
    /// <returns>The added node and the result reported by the underlying map.</returns>
    public (OrderedMultiMap<T, int>.Node Node, bool NewlyAdded) Add(T value)
        => this.map.Add(value, 0);

    /// <summary>
    /// Determines whether the set contains the specified value.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>true if at least one matching element exists; otherwise, false.</returns>
    public bool Contains(T value)
        => this.map.ContainsKey(value);

    /// <summary>
    /// Removes one element matching the specified value.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns>true if an element was removed; otherwise, false.</returns>
    public bool Remove(T value)
        => this.map.Remove(value);

    /// <summary>
    /// Removes the specified node from the set.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveNode(OrderedMultiMap<T, int>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    public void Clear()
        => this.map.Clear();

    #endregion

    #region Interface

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection<T>.Add(T item)
        => this.map.Add(item, 0);

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        => this.map.Keys.CopyTo(array, arrayIndex);

    void ICollection.CopyTo(Array array, int index)
        => ((ICollection)this.map.Keys).CopyTo(array, index);

    public OrderedMultiMap<T, int>.KeyCollection.Enumerator GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    #endregion
}
