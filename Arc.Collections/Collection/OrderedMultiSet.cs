// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collections;

/// <summary>
/// Represents a collection of objects that is maintained in sorted order (ascending by default).
/// <br/><see cref="OrderedMultiSet{T}"/> uses Red-Black Tree + Linked List structure to store objects.
/// <br/><see cref="OrderedMultiSet{T}"/> can store duplicate keys.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class OrderedMultiSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="reverse">true to reverses the comparison provided by the comparer. </param>
    public OrderedMultiSet(bool reverse = false)
    {
        this.map = new(reverse);
        // this.map.CreateNode = static (key, value, color) => new Node(key, color);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    /// <param name="reverse">true to reverses the comparison provided by the comparer. </param>
    public OrderedMultiSet(IComparer<T> comparer, bool reverse = false)
    {
        this.map = new(comparer, reverse);
        // this.map.CreateNode = static (key, value, color) => new Node(key, color);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="collection">The enumerable collection to be copied.</param>
    /// <param name="reverse">true to reverses the comparison provided by the comparer. </param>
    public OrderedMultiSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiSet{T}"/> class.
    /// </summary>
    /// <param name="collection">The enumerable collection to be copied.</param>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    /// <param name="reverse">true to reverses the comparison provided by the comparer. </param>
    public OrderedMultiSet(IEnumerable<T> collection, IComparer<T> comparer, bool reverse = false)
    {
        this.map = new(comparer, reverse);
        // this.map.CreateNode = static (key, value, color) => new Node(key, color);

        foreach (var x in collection)
        {
            this.Add(x);
        }
    }

    private OrderedMultiMap<T, int> map;

    /* Inherited Node class is a bit (10-20%) slower bacause of the casting operaiton.
    public class Node : OrderedMap<T, int>.Node
    {
        internal Node(T key, NodeColor color)
            : base(key, 0, color)
        {
        }
    }*/

    #region Main

    /// <summary>
    /// Gets the number of nodes actually contained in the <see cref="OrderedMultiSet{T}"/>.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the first node in the <see cref="OrderedMultiSet{T}"/>.
    /// </summary>
    public OrderedMultiMap<T, int>.Node? First => this.map.First;

    /// <summary>
    /// Gets the last node in the <see cref="OrderedMultiSet{T}"/>.
    /// </summary>
    public OrderedMultiMap<T, int>.Node? Last => this.map.Last;

    /*public bool UnsafePresearchForStructKey
    {
        get => this.map.UnsafePresearchForStructKey;
        set => this.map.UnsafePresearchForStructKey = value;
    }*/

    /// <summary>
    /// Adds an element to a collection. If the element is already in the set, this method returns the stored element without creating a new node, and sets NewlyAdded to false.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The value of the element to add.</param>
    /// <returns>Node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
    /// NewlyAdded: true if the node is created.</returns>
    public (OrderedMultiMap<T, int>.Node Node, bool NewlyAdded) Add(T value)
    {
        var result = this.map.Add(value, 0);
        return result;
    }

    /// <summary>
    /// Determines whether a collection contains a specific value.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the collection.</param>
    /// <returns>true if the collection contains an element with the specified value; otherwise, false.</returns>
    public bool Contains(T value) => this.map.ContainsKey(value);

    /// <summary>
    /// Removes a specified value from the collection."/>.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="value">The element to remove.</param>
    /// <returns>true if the element is found and successfully removed.</returns>
    public bool Remove(T value) => this.map.Remove(value);

    /// <summary>
    /// Removes a specified node from the collection.
    /// <br/>O(log n) operation.
    /// </summary>
    /// <param name="node">The <see cref="OrderedMap{TKey, TValue}.Node"/> to remove.</param>
    public void RemoveNode(OrderedMultiMap<T, int>.Node node) => this.map.RemoveNode(node);

    /// <summary>
    /// Removes all elements from a collection.
    /// </summary>
    public void Clear() => this.map.Clear();

    #endregion

    #region Interface

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection<T>.Add(T item) => this.map.Add(item, 0);

    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => this.map.Keys.CopyTo(array, arrayIndex);

    void ICollection.CopyTo(Array array, int index) => ((ICollection)this.map.Keys).CopyTo(array, index);

    public OrderedMultiMap<T, int>.KeyCollection.Enumerator GetEnumerator() => this.map.Keys.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.map.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.map.Keys.GetEnumerator();

    #endregion
}
