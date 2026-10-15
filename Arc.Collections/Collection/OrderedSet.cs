// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a collection of unique values maintained in sorted order.<br/>
/// Uses <see cref="OrderedMap{TKey, TValue}"/> as the underlying Red-Black Tree.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
/// <remarks>
/// Adding an existing element leaves the set unchanged. Null elements are supported and ordered as in
/// <see cref="OrderedMap{TKey, TValue}"/>.
/// Active nodes passed to node operations must belong to this set; ownership is not checked.
/// Advancing an enumerator after the set is modified throws <see cref="InvalidOperationException"/>.
/// </remarks>
public class OrderedSet<T> : IEnumerable<T>
{
    private const byte DummyValue = 0;

    private readonly OrderedMap<T, byte> map;

    /// <summary>
    /// Initializes an empty set.
    /// </summary>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedSet(bool reverse = false)
    {
        this.map = new OrderedMap<T, byte>(reverse);
    }

    /// <summary>
    /// Initializes an empty set with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedSet(IComparer<T>? comparer, bool reverse = false)
    {
        this.map = new OrderedMap<T, byte>(comparer, reverse);
    }

    /// <summary>
    /// Initializes a set from the specified collection. Duplicate elements are ignored.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public OrderedSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a set from the specified collection and comparer. Duplicate elements are ignored.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public OrderedSet(IEnumerable<T> collection, IComparer<T>? comparer, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        this.map = new OrderedMap<T, byte>(comparer, reverse);

        foreach (var item in collection)
        {
            this.map.Add(item, DummyValue);
        }
    }

    /// <summary>
    /// Gets the number of elements in the set.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the comparer used to order elements.
    /// </summary>
    /// <remarks>See <see cref="OrderedMap{TKey, TValue}.Comparer"/> for how the default comparer is applied.</remarks>
    public IComparer<T> Comparer => this.map.Comparer;

    /// <summary>
    /// Gets a value indicating whether the collection is sorted in reverse order.
    /// </summary>
    public bool IsReversed => this.map.IsReversed;

    /// <summary>
    /// Gets the first node in sort order, or <see langword="null"/> if the set is empty.
    /// </summary>
    public OrderedMap<T, byte>.Node? FirstNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.First;
    }

    /// <summary>
    /// Gets the last node in sort order, or <see langword="null"/> if the set is empty.
    /// </summary>
    public OrderedMap<T, byte>.Node? LastNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.Last;
    }

    /// <summary>
    /// Adds an element to the set.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element was newly added; <see langword="false"/> if it already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T item)
        => this.map.Add(item, DummyValue).NewlyAdded;

    /// <summary>
    /// Adds an element if it does not already exist, and returns the node holding it.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMap<T, byte>.Node Node, bool NewlyAdded) AddNode(T item)
        => this.map.Add(item, DummyValue);

    /// <summary>
    /// Adds an element if it does not already exist, optionally reusing an unused node.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <param name="reuse">A node to hold the new element if it is unused; otherwise, a new node is allocated.
    /// It is left untouched when the element already exists.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMap<T, byte>.Node Node, bool NewlyAdded) AddNode(T item, OrderedMap<T, byte>.Node reuse)
        => this.map.Add(item, DummyValue, reuse);

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T? item)
        => this.map.ContainsKey(item);

    /// <summary>
    /// Finds the node containing the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The matching node, or <see langword="null"/> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMap<T, byte>.Node? FindNode(T? item)
        => this.map.FindNode(item);

    /// <summary>
    /// Removes the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T? item)
        => this.map.Remove(item);

    /// <summary>
    /// Removes the specified node. An unused node is ignored.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <remarks>The removed node becomes unused and can be reused with <see cref="AddNode(T, OrderedMap{T, byte}.Node)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveNode(OrderedMap<T, byte>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Changes the element stored in an active node, keeping the same node object.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="key">The new element.</param>
    /// <returns><see langword="true"/> if the element was changed; <see langword="false"/> if the node is unused,
    /// <paramref name="key"/> compares equal to the current element (the stored element is kept),
    /// or another node already holds <paramref name="key"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetNodeKey(OrderedMap<T, byte>.Node node, T key)
        => this.map.SetNodeKey(node, key);

    /// <summary>
    /// Removes all elements. Their nodes become unused and can be reused with <see cref="AddNode(T, OrderedMap{T, byte}.Node)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Copies the elements to an array, in sort order.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based destination index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the array length.</exception>
    /// <exception cref="ArgumentException">The array is too small to hold the elements from <paramref name="index"/>.</exception>
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

        var enumerator = this.GetEnumerator();
        while (enumerator.MoveNext())
        {
            array[index++] = enumerator.Current;
        }
    }

    /// <summary>
    /// Gets the first node equal to or after the specified value in sort order.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The first node at or after <paramref name="value"/> in sort order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMap<T, byte>.Node? GetLowerBound(T? value)
        => this.map.GetLowerBound(value);

    /// <summary>
    /// Gets the last node equal to or before the specified value in sort order.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The last node at or before <paramref name="value"/> in sort order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMap<T, byte>.Node? GetUpperBound(T? value)
        => this.map.GetUpperBound(value);

    /// <summary>
    /// Gets the nodes delimiting the inclusive range between two values in sort order.
    /// </summary>
    /// <param name="lower">The bound that comes first in sort order (the larger value when the set is reversed).</param>
    /// <param name="upper">The bound that comes last in sort order (the smaller value when the set is reversed).</param>
    /// <returns>The first node at or after <paramref name="lower"/> and the last node at or before <paramref name="upper"/> in sort order, or <c>(null, null)</c> if the range is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMap<T, byte>.Node? Lower, OrderedMap<T, byte>.Node? Upper) GetRange(T? lower, T? upper)
        => this.map.GetRange(lower, upper);

    /// <summary>
    /// Checks the underlying red-black tree. O(n); intended for diagnostics.
    /// </summary>
    /// <returns><see langword="true"/> if the internal structure is valid; otherwise, <see langword="false"/>.</returns>
    public bool Validate()
        => this.map.Validate();

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(this.map);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new Enumerator(this.map);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this.map);

    /// <summary>
    /// Enumerates elements in sort order.
    /// </summary>
    /// <remarks><see cref="IEnumerator.Reset"/> is not supported and throws <see cref="NotSupportedException"/>.</remarks>
    public struct Enumerator : IEnumerator<T>
    {
        private OrderedMap<T, byte>.KeyEnumerable.Enumerator enumerator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(OrderedMap<T, byte> map)
        {
            this.enumerator = map.Keys.GetEnumerator();
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.Current;
        }

        object? IEnumerator.Current
        {
            get
            {
                this.enumerator.ValidateCurrent();
                return this.Current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
            => this.enumerator.MoveNext();

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
        }

        void IEnumerator.Reset()
            => throw new NotSupportedException();
    }

    #endregion
}
