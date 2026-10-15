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
/// Represents a sorted collection that allows duplicate elements.<br/>
/// Uses <see cref="OrderedMultiMap{TKey, TValue}"/> as the underlying Red-Black Tree
/// and duplicate-group linked-list implementation.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// Equal elements are kept in insertion order. Null elements are supported and ordered as in
/// <see cref="OrderedMultiMap{TKey, TValue}"/>.
/// Active nodes passed to node operations must belong to this collection; ownership is not checked.
/// Advancing an enumerator after the collection is modified throws <see cref="InvalidOperationException"/>.
/// </remarks>
public class OrderedMultiSet<T> : IEnumerable<T>
{
    private const byte DummyValue = 0;

    private readonly OrderedMultiMap<T, byte> map;

    /// <summary>
    /// Initializes an empty collection.
    /// </summary>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedMultiSet(bool reverse = false)
    {
        this.map = new OrderedMultiMap<T, byte>(reverse);
    }

    /// <summary>
    /// Initializes an empty collection with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedMultiSet(IComparer<T>? comparer, bool reverse = false)
    {
        this.map = new OrderedMultiMap<T, byte>(comparer, reverse);
    }

    /// <summary>
    /// Initializes a collection from the specified sequence.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public OrderedMultiSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a collection from the specified sequence and comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public OrderedMultiSet(IEnumerable<T> collection, IComparer<T>? comparer, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        this.map = new OrderedMultiMap<T, byte>(comparer, reverse);

        foreach (var item in collection)
        {
            this.map.Add(item, DummyValue);
        }
    }

    /// <summary>
    /// Gets the number of elements, including duplicates.
    /// </summary>
    public int Count => this.map.Count;

    /// <summary>
    /// Gets the comparer used to order elements.
    /// </summary>
    /// <remarks>See <see cref="OrderedMultiMap{TKey, TValue}.Comparer"/> for how the default comparer is applied.</remarks>
    public IComparer<T> Comparer => this.map.Comparer;

    /// <summary>
    /// Gets a value indicating whether the collection is sorted in reverse order.
    /// </summary>
    public bool IsReversed => this.map.IsReversed;

    /// <summary>
    /// Gets the first node in sort order, or <see langword="null"/> if the collection is empty.
    /// </summary>
    public OrderedMultiMap<T, byte>.Node? FirstNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.First;
    }

    /// <summary>
    /// Gets the last node in sort order, or <see langword="null"/> if the collection is empty.
    /// </summary>
    public OrderedMultiMap<T, byte>.Node? LastNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.Last;
    }

    /// <summary>
    /// Adds an element and returns the created node. A duplicate is placed after the existing occurrences of the element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The node that holds the element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node Add(T item)
        => this.map.Add(item, DummyValue).Node;

    /// <summary>
    /// Adds an element by reusing an unused node when possible.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <param name="reuse">A node to hold the new element if it is unused; otherwise, a new node is allocated.</param>
    /// <returns>The node that holds the element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node Add(T item, OrderedMultiMap<T, byte>.Node reuse)
        => this.map.Add(item, DummyValue, reuse).Node;

    /// <summary>
    /// Determines whether the collection contains the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T? item)
        => this.map.ContainsKey(item);

    /// <summary>
    /// Finds the first node with the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The first matching node, or <see langword="null"/> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? FindFirstNode(T? item)
        => this.map.FindFirstNode(item);

    /// <summary>
    /// Removes the first occurrence of the specified element.
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
    /// <remarks>The removed node becomes unused and can be reused with <see cref="Add(T, OrderedMultiMap{T, byte}.Node)"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveNode(OrderedMultiMap<T, byte>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Changes the element stored in an active node, keeping the same node object.
    /// The node is placed after the existing occurrences of the new element.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="key">The new element.</param>
    /// <returns><see langword="true"/> if the element was changed; <see langword="false"/> if the node is unused
    /// or <paramref name="key"/> compares equal to the current element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetNodeKey(OrderedMultiMap<T, byte>.Node node, T key)
        => this.map.SetNodeKey(node, key);

    /// <summary>
    /// Removes all elements. Their nodes become unused and can be reused with <see cref="Add(T, OrderedMultiMap{T, byte}.Node)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Copies all elements to the specified array, in sort order.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based destination index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the array length.</exception>
    /// <exception cref="ArgumentException">The array is too small to hold the elements starting at <paramref name="index"/>.</exception>
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
    /// Counts occurrences of the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The number of matching elements.</returns>
    public int GetCount(T? item)
    {
        var count = 0;
        var enumerator = this.map.EnumerateNodes(item).GetEnumerator();

        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Removes all occurrences of the specified element.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The number of elements removed.</returns>
    public int RemoveAll(T? item)
    {
        var node = this.map.FindFirstNode(item);
        if (node is null)
        {
            return 0;
        }

        // Unlink the duplicates chained to the tree node in O(1) each, then remove the tree node,
        // instead of searching the tree again for every duplicate.
        var count = 1;
        while (!node.IsSingleNode)
        {
            this.map.RemoveNode(node.ListNext!);
            count++;
        }

        this.map.RemoveNode(node);
        return count;
    }

    /// <summary>
    /// Gets the first node equal to or after the specified value in sort order.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The first node at or after <paramref name="value"/> in sort order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? GetLowerBound(T? value)
        => this.map.GetLowerBound(value);

    /// <summary>
    /// Gets the last node equal to or before the specified value in sort order.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The last node at or before <paramref name="value"/> in sort order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? GetUpperBound(T? value)
        => this.map.GetUpperBound(value);

    /// <summary>
    /// Gets the nodes delimiting the inclusive range between two values in sort order.
    /// </summary>
    /// <param name="lower">The bound that comes first in sort order (the larger value when the collection is reversed).</param>
    /// <param name="upper">The bound that comes last in sort order (the smaller value when the collection is reversed).</param>
    /// <returns>The first node at or after <paramref name="lower"/> and the last node at or before <paramref name="upper"/> in sort order, or <c>(null, null)</c> if the range is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMultiMap<T, byte>.Node? Lower, OrderedMultiMap<T, byte>.Node? Upper) GetRange(T? lower, T? upper)
        => this.map.GetRange(lower, upper);

    /// <summary>
    /// Enumerates nodes matching the specified element in insertion order, without allocation.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>An allocation-free enumerable over the matching nodes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.NodeEnumerable EnumerateNodes(T? item)
        => this.map.EnumerateNodes(item);

    /// <summary>
    /// Checks the underlying red-black tree and duplicate-group lists. O(n); intended for diagnostics.
    /// </summary>
    /// <returns><see langword="true"/> if the internal structure is valid; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        private OrderedMultiMap<T, byte>.KeyEnumerable.Enumerator enumerator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(OrderedMultiMap<T, byte> map)
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
