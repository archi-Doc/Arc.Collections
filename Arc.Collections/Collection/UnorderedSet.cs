// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collections;

/// <summary>
/// Represents an unordered collection of unique elements backed by a hash table.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public sealed class UnorderedSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
{
    private readonly UnorderedMap<T, int> map;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
    /// </summary>
    public UnorderedSet()
    {
        this.map = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    public UnorderedSet(IEqualityComparer<T> comparer)
    {
        this.map = new(comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class containing the specified elements.
    /// </summary>
    /// <param name="collection">The elements to add to the set.</param>
    public UnorderedSet(IEnumerable<T> collection)
        : this(collection, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class containing the specified elements.
    /// </summary>
    /// <param name="collection">The elements to add to the set.</param>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(collection);

        this.map = new(comparer);
        foreach (var item in collection)
        {
            this.map.Add(item, 0);
        }
    }

    #region Main

    /// <summary>
    /// Gets the number of elements contained in the set.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Adds the specified value to the set.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>
    /// The node index and whether a new node was added.
    /// </returns>
    public (int NodeIndex, bool NewlyAdded) Add(T value)
        => this.map.Add(value, 0);

    /// <summary>
    /// Determines whether the set contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is contained in the set; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T value)
        => this.map.ContainsKey(value);

    /// <summary>
    /// Replaces the value of the specified node.
    /// </summary>
    /// <param name="nodeIndex">The index of the node to update.</param>
    /// <param name="value">The new value.</param>
    /// <returns><see langword="true"/> if the node was successfully updated; otherwise, <see langword="false"/>.</returns>
    public bool SetNodeValue(int nodeIndex, T value)
        => this.map.SetNodeKey(nodeIndex, value);

    /// <summary>
    /// Removes the specified value from the set.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was found and removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T value)
        => this.map.Remove(value);

    /// <summary>
    /// Removes the specified node from the set.
    /// </summary>
    /// <param name="nodeIndex">The index of the node to remove.</param>
    public void RemoveNode(int nodeIndex)
        => this.map.RemoveNode(nodeIndex);

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

    public UnorderedMap<T, int>.KeyCollection.Enumerator GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    #endregion
}
