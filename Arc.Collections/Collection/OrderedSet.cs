// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collections;

#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a collection of objects maintained in sorted order (ascending by default).
/// <br/><see cref="OrderedSet{T}"/> uses a red-black tree to store objects.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class OrderedSet<T>
{
    private readonly OrderedMap<T, int> map;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
    /// </summary>
    /// <param name="reverse">true to reverse the default comparison order.</param>
    public OrderedSet(bool reverse = false)
    {
        this.map = new(reverse);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    /// <param name="reverse">true to reverse the comparison order.</param>
    public OrderedSet(IComparer<T> comparer, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        this.map = new(comparer, reverse);
    }

    /// <summary>
    /// Initializes a new instance by copying the specified collection.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    /// <param name="reverse">true to reverse the default comparison order.</param>
    public OrderedSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a new instance by copying the specified collection.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    /// <param name="reverse">true to reverse the comparison order.</param>
    public OrderedSet(
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
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the first node in the set.
    /// </summary>
    public OrderedMap<T, int>.Node? First => this.map.First;

    /// <summary>
    /// Gets the last node in the set.
    /// </summary>
    public OrderedMap<T, int>.Node? Last => this.map.Last;

    /// <summary>
    /// Adds an element to the set.
    /// </summary>
    /// <param name="value">The element to add.</param>
    /// <returns>
    /// The stored node and true if a new node was created; otherwise, the existing node and false.
    /// </returns>
    public (OrderedMap<T, int>.Node Node, bool NewlyAdded) Add(T value)
        => this.map.Add(value, 0);

    /// <summary>
    /// Determines whether the set contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>true if the value exists; otherwise, false.</returns>
    public bool Contains(T value)
        => this.map.ContainsKey(value);

    /// <summary>
    /// Removes the specified value from the set.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns>true if the value was removed; otherwise, false.</returns>
    public bool Remove(T value)
        => this.map.Remove(value);

    /// <summary>
    /// Removes the specified node from the set.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveNode(OrderedMap<T, int>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Validates the red-black tree.
    /// </summary>
    /// <returns>true if the tree is valid; otherwise, false.</returns>
    public bool Validate()
        => this.map.Validate();

    #endregion
}
