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
    /// Initializes a set from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a set from the specified collection and comparer.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
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
    public IComparer<T> Comparer => this.map.Comparer;

    /// <summary>
    /// Gets a value indicating whether the collection is sorted in reverse order.
    /// </summary>
    public bool Reverse => this.map.Reverse;

    /// <summary>
    /// Gets the first node in sort order.
    /// </summary>
    public OrderedMap<T, byte>.Node? FirstNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.First;
    }

    /// <summary>
    /// Gets the last node in sort order.
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
    /// <returns><see langword="true"/> if the element was newly added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(T item)
        => this.map.Add(item, DummyValue).NewlyAdded;

    /// <summary>
    /// Adds an element and returns its node.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMap<T, byte>.Node Node, bool NewlyAdded) AddNode(T item)
        => this.map.Add(item, DummyValue);

    /// <summary>
    /// Adds an element, optionally reusing an unused node.
    /// </summary>
    /// <param name="item">The element.</param>
    /// <param name="reuse">An unused node to reuse when possible.</param>
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
    /// Removes the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveNode(OrderedMap<T, byte>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Changes the element stored in the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the value was changed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetNodeValue(OrderedMap<T, byte>.Node node, T value)
        => this.map.SetNodeKey(node, value);

    /// <summary>
    /// Removes all elements from the set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

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
    /// <returns>The first node at or after the specified key in collection order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMap<T, byte>.Node? GetLowerBound(T? value)
        => this.map.GetLowerBound(value);

    /// <summary>
    /// Gets the last node equal to or before the specified value in sort order.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The last node at or before the specified key in collection order, or <see langword="null"/> if none exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMap<T, byte>.Node? GetUpperBound(T? value)
        => this.map.GetUpperBound(value);

    /// <summary>
    /// Gets the nodes delimiting the specified range.
    /// </summary>
    /// <param name="lower">The lower key.</param>
    /// <param name="upper">The upper key.</param>
    /// <returns>The first node at or after <paramref name="lower"/> and the last node at or before <paramref name="upper"/>, or <c>(null, null)</c> if the range is empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMap<T, byte>.Node? Lower, OrderedMap<T, byte>.Node? Upper) GetRange(T? lower, T? upper)
        => this.map.GetRange(lower, upper);

    /// <summary>
    /// Validates the underlying Red-Black Tree.
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
    /// Enumerates the elements in sort order.
    /// </summary>
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
