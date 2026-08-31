// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

namespace Arc.Collections;

/// <summary>
/// Represents a doubly linked list.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class UnorderedLinkedList<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
{
    /// <summary>
    /// Represents a node in a <see cref="UnorderedLinkedList{T}"/>.
    /// </summary>
    public sealed class Node
    {
        internal UnorderedLinkedList<T>? list;
        internal Node? previous;
        internal Node? next;
        internal T value;

        internal Node(UnorderedLinkedList<T> list, T value)
        {
            this.list = list;
            this.value = value;
        }

        /// <summary>
        /// Gets the list to which this node belongs, or <see langword="null"/> if detached.
        /// </summary>
        public UnorderedLinkedList<T>? List => this.list;

        /// <summary>
        /// Gets the previous node.
        /// </summary>
        public Node? Previous
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var list = this.list;
                return list is null || ReferenceEquals(this, list.head)
                    ? null
                    : this.previous;
            }
        }

        /// <summary>
        /// Gets the next node.
        /// </summary>
        public Node? Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var list = this.list;
                var next = this.next;
                return list is null || ReferenceEquals(next, list.head)
                    ? null
                    : next;
            }
        }

        /// <summary>
        /// Gets the value contained in the node.
        /// </summary>
        public T Value => this.value;

        /// <summary>
        /// Gets a writable reference to the value contained in the node.
        /// </summary>
        public ref T ValueRef => ref this.value;

        /// <summary>
        /// Changes the value without modifying the list structure.
        /// </summary>
        /// <param name="value">The value.</param>
        public void UnsafeChangeValue(T value) => this.value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            this.list = null;
            this.previous = null;
            this.next = null;
        }
    }

    /// <summary>
    /// The first node of the doubly linked circular list, or <see langword="null"/> if empty.
    /// </summary>
    protected Node? head; // Doubly linked circular list.

    /// <summary>
    /// The number of nodes in the list.
    /// </summary>
    protected int size;

    /// <summary>
    /// The modification counter used to invalidate enumerators.
    /// </summary>
    protected int version;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedLinkedList{T}"/> class.
    /// </summary>
    public UnorderedLinkedList()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedLinkedList{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public UnorderedLinkedList(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        // Build the chain locally and close the circle once, instead of paying the
        // insert-before-head bookkeeping (head lookup, version increment) per element.
        Node? first = null;
        Node? last = null;
        var count = 0;
        foreach (var x in collection)
        {
            var node = new Node(this, x);
            if (last is null)
            {
                first = node;
            }
            else
            {
                last.next = node;
                node.previous = last;
            }

            last = node;
            count++;
        }

        if (first is not null)
        {
            first.previous = last;
            last!.next = first;
            this.head = first;
            this.size = count;
        }
    }

    /// <summary>
    /// Gets the first node.
    /// </summary>
    public Node? First => this.head;

    /// <summary>
    /// Gets the last node.
    /// </summary>
    public Node? Last => this.head?.previous;

    #region ICollection

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => this.size;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always <see langword="false"/>.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether access to the list is synchronized. Always <see langword="false"/>.
    /// </summary>
    public bool IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection<T>.Add(T value) => this.AddLast(value);

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    public void Clear()
    {
        var head = this.head;
        if (head is not null)
        {
            var node = head;
            do
            {
                var next = node.next!;
                node.Clear();
                node = next;
            }
            while (!ReferenceEquals(node, head));

            this.head = null;
            this.size = 0;
        }

        this.version++;
    }

    /// <summary>
    /// Determines whether an element is in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T value) => this.Find(value) is not null;

    /// <summary>
    /// Copies the list to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based destination index.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)arrayIndex > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < this.size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        var node = this.head;
        var end = arrayIndex + this.size;
        while (arrayIndex < end)
        {
            array[arrayIndex++] = node!.value;
            node = node.next;
        }
    }

    /// <summary>
    /// Copies the list to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1)
        {
            throw new ArgumentException("The array must be one-dimensional.", nameof(array));
        }

        if (array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("The array must have a zero lower bound.", nameof(array));
        }

        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        try
        {
            if (array is T[] tarray)
            {
                this.CopyTo(tarray, index);
                return;
            }

            if (array is not object?[] objects)
            {
                throw new ArgumentException("Invalid array type.", nameof(array));
            }

            var node = this.head;
            var end = index + this.size;
            while (index < end)
            {
                objects[index++] = node!.value;
                node = node.next;
            }
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("Invalid array type.", nameof(array));
        }
    }

    /// <summary>
    /// Removes the first occurrence of the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T value)
    {
        var node = this.Find(value);
        if (node is null)
        {
            return false;
        }

        this.InternalRemoveNode(node);
        return true;
    }

    #endregion

    #region Enumerator

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements of a <see cref="UnorderedLinkedList{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnorderedLinkedList<T> list;
        private readonly Node? head; // Cached: saves a dependent memory load per MoveNext; a stale value is unreachable because the version check fires first.
        private readonly int version;
        private Node? nextNode;
        private Node? currentNode;

        internal Enumerator(UnorderedLinkedList<T> list)
        {
            this.list = list;
            this.head = list.head;
            this.version = list.version;
            this.nextNode = this.head;
            this.currentNode = null;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var node = this.currentNode;
                if (node is null)
                {
                    ThrowInvalidEnumeratorState();
                }

                return node.value;
            }
        }

        object? IEnumerator.Current => this.Current;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                ThrowVersionMismatch();
            }

            var node = this.nextNode;
            if (node is null)
            {
                this.currentNode = null;
                return false;
            }

            this.currentNode = node;
            var next = node.next;
            this.nextNode = ReferenceEquals(next, this.head)
                ? null
                : next;
            return true;
        }

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                ThrowVersionMismatch();
            }

            this.nextNode = this.head;
            this.currentNode = null;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowVersionMismatch()
            => throw new InvalidOperationException(
                "Collection was modified after the enumerator was instantiated.");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidEnumeratorState()
            => throw new InvalidOperationException(
                "Enumeration has either not started or has already finished.");
    }

    #endregion

    #region LinkedList

    /// <summary>
    /// Finds the first node containing the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The first matching node, or <see langword="null"/> if not found.</returns>
    public Node? Find(T value)
    {
        var head = this.head;
        if (head is null)
        {
            return null;
        }

        var node = head;
        if (value is not null)
        {
            // EqualityComparer<T>.Default is invoked directly (not via a local) so the JIT
            // reliably devirtualizes and inlines the comparison for value types.
            do
            {
                if (EqualityComparer<T>.Default.Equals(node.value, value))
                {
                    return node;
                }

                node = node.next!;
            }
            while (!ReferenceEquals(node, head));
        }
        else
        {
            do
            {
                if (node.value is null)
                {
                    return node;
                }

                node = node.next!;
            }
            while (!ReferenceEquals(node, head));
        }

        return null;
    }

    /// <summary>
    /// Finds the last node containing the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The last matching node, or <see langword="null"/> if not found.</returns>
    public Node? FindLast(T value)
    {
        var head = this.head;
        if (head is null)
        {
            return null;
        }

        var last = head.previous!;
        var node = last;
        if (value is not null)
        {
            do
            {
                if (EqualityComparer<T>.Default.Equals(node.value, value))
                {
                    return node;
                }

                node = node.previous!;
            }
            while (!ReferenceEquals(node, last));
        }
        else
        {
            do
            {
                if (node.value is null)
                {
                    return node;
                }

                node = node.previous!;
            }
            while (!ReferenceEquals(node, last));
        }

        return null;
    }

    /// <summary>
    /// Adds a new value after the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="value">The value.</param>
    /// <returns>The new node.</returns>
    public Node AddAfter(Node node, T value)
    {
        this.ValidateNode(node);
        var result = new Node(this, value);
        this.InternalInsertNodeBefore(node.next!, result);
        return result;
    }

    /// <summary>
    /// Adds the specified node after an existing node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="newNode">The node to add.</param>
    public void AddAfter(Node node, Node newNode)
    {
        this.ValidateNode(node);
        this.ValidateNewNode(newNode);
        this.InternalInsertNodeBefore(node.next!, newNode);
        newNode.list = this;
    }

    /// <summary>
    /// Adds a new value before the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="value">The value.</param>
    /// <returns>The new node.</returns>
    public Node AddBefore(Node node, T value)
    {
        this.ValidateNode(node);
        var result = new Node(this, value);
        this.InternalInsertNodeBefore(node, result);
        if (ReferenceEquals(node, this.head))
        {
            this.head = result;
        }

        return result;
    }

    /// <summary>
    /// Adds the specified node before an existing node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="newNode">The node to add.</param>
    public void AddBefore(Node node, Node newNode)
    {
        this.ValidateNode(node);
        this.ValidateNewNode(newNode);
        this.InternalInsertNodeBefore(node, newNode);
        newNode.list = this;
        if (ReferenceEquals(node, this.head))
        {
            this.head = newNode;
        }
    }

    /// <summary>
    /// Adds a value at the start of the list.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The new node.</returns>
    public Node AddFirst(T value)
    {
        var result = new Node(this, value);
        var head = this.head;
        if (head is null)
        {
            this.InternalInsertNodeToEmptyList(result);
        }
        else
        {
            this.InternalInsertNodeBefore(head, result);
            this.head = result;
        }

        return result;
    }

    /// <summary>
    /// Adds a node at the start of the list.
    /// </summary>
    /// <param name="node">The node.</param>
    public void AddFirst(Node node)
    {
        this.ValidateNewNode(node);
        var head = this.head;
        if (head is null)
        {
            this.InternalInsertNodeToEmptyList(node);
        }
        else
        {
            this.InternalInsertNodeBefore(head, node);
            this.head = node;
        }

        node.list = this;
    }

    /// <summary>
    /// Adds a value at the end of the list.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The new node.</returns>
    public Node AddLast(T value)
    {
        var result = new Node(this, value);
        var head = this.head;
        if (head is null)
        {
            this.InternalInsertNodeToEmptyList(result);
        }
        else
        {
            this.InternalInsertNodeBefore(head, result);
        }

        return result;
    }

    /// <summary>
    /// Adds a node at the end of the list.
    /// </summary>
    /// <param name="node">The node.</param>
    public void AddLast(Node node)
    {
        this.ValidateNewNode(node);
        var head = this.head;
        if (head is null)
        {
            this.InternalInsertNodeToEmptyList(node);
        }
        else
        {
            this.InternalInsertNodeBefore(head, node);
        }

        node.list = this;
    }

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public void Remove(Node node)
    {
        this.ValidateNode(node);
        this.InternalRemoveNode(node);
    }

    /// <summary>
    /// Moves the specified node to the start of the list.
    /// </summary>
    /// <param name="node">The node.</param>
    public void MoveToFirst(Node node)
    {
        this.ValidateNode(node);
        var head = this.head!;
        if (ReferenceEquals(node, head))
        {
            return;
        }

        node.next!.previous = node.previous;
        node.previous!.next = node.next;
        node.next = head;
        node.previous = head.previous;
        head.previous!.next = node;
        head.previous = node;
        this.head = node;
        this.version++;
    }

    /// <summary>
    /// Moves the specified node to the end of the list.
    /// </summary>
    /// <param name="node">The node.</param>
    public void MoveToLast(Node node)
    {
        this.ValidateNode(node);
        var head = this.head!;
        if (ReferenceEquals(node, head.previous))
        {
            return;
        }

        node.next!.previous = node.previous;
        node.previous!.next = node.next;
        if (ReferenceEquals(node, head))
        {
            head = node.next!;
            this.head = head;
        }

        node.next = head;
        node.previous = head.previous;
        head.previous!.next = node;
        head.previous = node;
        this.version++;
    }

    /// <summary>
    /// Removes the node at the start of the list.
    /// </summary>
    public void RemoveFirst()
    {
        var head = this.head;
        if (head is null)
        {
            ThrowEmptyList();
        }

        this.InternalRemoveNode(head);
    }

    /// <summary>
    /// Removes the node at the end of the list.
    /// </summary>
    public void RemoveLast()
    {
        var head = this.head;
        if (head is null)
        {
            ThrowEmptyList();
        }

        this.InternalRemoveNode(head.previous!);
    }

    internal void InternalInsertNodeToEmptyList(Node newNode)
    {
        Debug.Assert(
            this.head is null && this.size == 0,
            "LinkedList must be empty when this method is called.");
        newNode.next = newNode;
        newNode.previous = newNode;
        this.head = newNode;
        this.size++;
        this.version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalInsertNodeBefore(Node node, Node newNode)
    {
        newNode.next = node;
        newNode.previous = node.previous;
        node.previous!.next = newNode;
        node.previous = newNode;
        this.size++;
        this.version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalRemoveNode(Node node)
    {
        Debug.Assert(
            ReferenceEquals(node.list, this),
            "Deleting a node from another list.");
        Debug.Assert(
            this.head is not null,
            "This method must not be called on an empty list.");
        if (ReferenceEquals(node.next, node))
        {
            Debug.Assert(
                this.size == 1 && ReferenceEquals(this.head, node),
                "A self-referencing node must be the only node in the list.");
            this.head = null;
        }
        else
        {
            node.next!.previous = node.previous;
            node.previous!.next = node.next;
            if (ReferenceEquals(this.head, node))
            {
                this.head = node.next;
            }
        }

        node.Clear();
        this.size--;
        this.version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ValidateNewNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.list is not null)
        {
            ThrowNodeAlreadyBelongsToList();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ValidateNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!ReferenceEquals(node.list, this))
        {
            ThrowNodeDoesNotBelongToList();
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEmptyList()
        => throw new InvalidOperationException("The LinkedList is empty.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNodeAlreadyBelongsToList()
        => throw new InvalidOperationException(
            "The LinkedList node already belongs to a LinkedList.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNodeDoesNotBelongToList()
        => throw new InvalidOperationException(
            "The LinkedList node does not belong to the current LinkedList.");

    #endregion
}
