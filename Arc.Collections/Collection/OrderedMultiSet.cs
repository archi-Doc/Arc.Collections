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
public class OrderedMultiSet<T> : IEnumerable<T>
{
    private const byte DummyValue = 0;

    private readonly OrderedMultiMap<T, byte> map;

    /// <summary>
    /// Initializes an empty collection.
    /// </summary>
    public OrderedMultiSet(bool reverse = false)
    {
        this.map = new OrderedMultiMap<T, byte>(reverse);
    }

    /// <summary>
    /// Initializes an empty collection with the specified comparer.
    /// </summary>
    public OrderedMultiSet(IComparer<T>? comparer, bool reverse = false)
    {
        this.map = new OrderedMultiMap<T, byte>(comparer, reverse);
    }

    /// <summary>
    /// Initializes a collection from the specified sequence.
    /// </summary>
    public OrderedMultiSet(IEnumerable<T> collection, bool reverse = false)
        : this(collection, Comparer<T>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a collection from the specified sequence and comparer.
    /// </summary>
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
    public IComparer<T> Comparer => this.map.Comparer;

    public bool Reverse => this.map.Reverse;

    /// <summary>
    /// Gets the first node in sort order.
    /// </summary>
    public OrderedMultiMap<T, byte>.Node? FirstNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.First;
    }

    /// <summary>
    /// Gets the last node in sort order.
    /// </summary>
    public OrderedMultiMap<T, byte>.Node? LastNode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.map.Last;
    }

    /// <summary>
    /// Adds an element and returns the created node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node Add(T item)
        => this.map.Add(item, DummyValue).Node;

    /// <summary>
    /// Adds an element by reusing an unused node when possible.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node Add(T item, OrderedMultiMap<T, byte>.Node reuse)
        => this.map.Add(item, DummyValue, reuse).Node;

    /// <summary>
    /// Determines whether the collection contains the specified element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T? item)
        => this.map.ContainsKey(item);

    /// <summary>
    /// Finds the first node with the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? FindFirstNode(T? item)
        => this.map.FindFirstNode(item);

    /// <summary>
    /// Removes the first occurrence of the specified element.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T? item)
        => this.map.Remove(item);

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveNode(OrderedMultiMap<T, byte>.Node node)
        => this.map.RemoveNode(node);

    /// <summary>
    /// Changes the element stored in the specified node.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetNodeValue(OrderedMultiMap<T, byte>.Node node, T value)
        => this.map.SetNodeKey(node, value);

    /// <summary>
    /// Removes all elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
        => this.map.Clear();

    /// <summary>
    /// Copies all elements to the specified array.
    /// </summary>
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
    public int GetCount(T? item)
    {
        var count = 0;
        var enumerator = this.map.EnumerateNode(item).GetEnumerator();

        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Removes all occurrences of the specified element.
    /// </summary>
    public int RemoveAll(T? item)
    {
        var count = 0;

        while (true)
        {
            var node = this.map.FindFirstNode(item);
            if (node is null)
            {
                return count;
            }

            this.map.RemoveNode(node);
            count++;
        }
    }

    /// <summary>
    /// Gets the first node equal to or after the specified value in sort order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? GetLowerBound(T? value)
        => this.map.GetLowerBound(value);

    /// <summary>
    /// Gets the last node equal to or before the specified value in sort order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.Node? GetUpperBound(T? value)
        => this.map.GetUpperBound(value);

    /// <summary>
    /// Gets the nodes delimiting the specified range.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (OrderedMultiMap<T, byte>.Node? Lower, OrderedMultiMap<T, byte>.Node? Upper) GetRange(T? lower, T? upper)
        => this.map.GetRange(lower, upper);

    /// <summary>
    /// Enumerates nodes matching the specified element without allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OrderedMultiMap<T, byte>.NodeEnumerable EnumerateNode(T? item)
        => this.map.EnumerateNode(item);

    /// <summary>
    /// Validates the underlying Red-Black Tree and duplicate lists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Validate()
        => this.map.Validate();

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
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
    public struct Enumerator : IEnumerator<T>
    {
        private OrderedMultiMap<T, byte>.KeyEnumerable.Enumerator enumerator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(OrderedMultiMap<T, byte> map)
        {
            this.enumerator = map.Keys.GetEnumerator();
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.Current;
        }

        object? IEnumerator.Current
            => this.Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
            => this.enumerator.MoveNext();

        public void Dispose()
        {
        }

        void IEnumerator.Reset()
            => throw new NotSupportedException();
    }

    #endregion
}
