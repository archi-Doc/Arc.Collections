// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1615 // Element return value should be documented

namespace Arc.Collections;

/// <summary>
/// Represents an unordered collection that allows duplicate elements.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class UnorderedMultiSet<T>
{
    private readonly UnorderedMultiMap<T, int> map;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMultiSet{T}"/> class.
    /// </summary>
    public UnorderedMultiSet()
    {
        this.map = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    public UnorderedMultiSet(IEqualityComparer<T> comparer)
    {
        this.map = new(comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMultiSet{T}"/> class containing the specified elements.
    /// </summary>
    /// <param name="collection">The elements to add to the collection.</param>
    public UnorderedMultiSet(IEnumerable<T> collection)
        : this(collection, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMultiSet{T}"/> class containing the specified elements.
    /// </summary>
    /// <param name="collection">The elements to add to the collection.</param>
    /// <param name="comparer">The equality comparer to use for the elements.</param>
    public UnorderedMultiSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
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
    /// Gets the number of elements contained in the collection, including duplicates.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Adds the specified value to the collection.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>The node index and whether a new key entry was created.</returns>
    public (int NodeIndex, bool NewlyAdded) Add(T value)
        => this.map.Add(value, 0);

    /// <summary>
    /// Determines whether the collection contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is contained in the collection; otherwise, <see langword="false"/>.</returns>
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
    /// Removes one occurrence of the specified value from the collection.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if an occurrence was found and removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T value)
        => this.map.Remove(value);

    /// <summary>
    /// Removes the specified node from the collection.
    /// </summary>
    /// <param name="nodeIndex">The index of the node to remove.</param>
    public void RemoveNode(int nodeIndex)
        => this.map.RemoveNode(nodeIndex);

    /// <summary>
    /// Removes all elements from the collection.
    /// </summary>
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnorderedMultiMap<T, int>.Enumerator GetEnumerator() => new(this.map);

    #endregion

    #region Interface

    public

    public UnorderedMultiMap<T, int>.KeyCollection.Enumerator GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => this.map.Keys.GetEnumerator();

    #endregion
}
