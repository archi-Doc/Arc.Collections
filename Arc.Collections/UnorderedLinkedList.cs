// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401

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
        internal UnorderedLinkedList<T> list;
        internal Node? previous;
        internal Node? next;
        internal T value;

        public UnorderedLinkedList<T> List => this.list;

        /// <summary>
        /// Gets the previous node.
        /// </summary>
        public Node? Previous => this.previous == null || this == this.List.head ? null : this.previous;

        /// <summary>
        /// Gets the next node.
        /// </summary>
        public Node? Next => this.next == null || this.next == this.List.head ? null : this.next;

        /// <summary>
        /// Gets the value contained in the node.
        /// </summary>
        public T Value => this.value;

        public ref T ValueRef => ref this.value;

        public void UnsafeChangeValue(T value) => this.value = value;

        internal Node(UnorderedLinkedList<T> list, T value)
        {
            this.list = list;
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            this.list = null!;
            this.previous = null;
            this.next = null;
        }
    }

    protected Node? head; // doubly-Linked circular list.
    protected Node? removed;
    protected int size;
    protected int version;

    /// <summary>
    /// Gets the first node.
    /// </summary>
    public Node? First => this.head;

    /// <summary>
    /// Gets the last node.
    /// </summary>
    public Node? Last => this.head?.previous;

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
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        foreach (var x in collection)
        {
            this.AddLast(x);
        }
    }

    #region ICollection

    public int Count => this.size;

    public bool IsReadOnly => false;

    void ICollection<T>.Add(T value)
    {
        this.AddLast(value);
    }

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    public void Clear()
    {
        var n = this.First;
        while (n != null)
        {
            var t = n.Next; // use Next the instead of "next", otherwise it will loop forever
            // n.Clear();
            n.list = null!;
            n.previous = null;
            n.next = this.removed;
            this.removed = n;
            n = t;
        }

        this.head = null;
        this.size = 0;
        this.version++;
    }

    /// <summary>
    /// Determines whether an element is in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>true if value is found in the list.</returns>
    public bool Contains(T value) => this.Find(value) != null;

    /// <summary>
    /// Copies the list or a portion of it to an array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < this.size)
        {
            throw new ArgumentException(nameof(array));
        }

        var node = this.First;
        if (node != null)
        {
            do
            {
                array[arrayIndex++] = node!.value;
                node = node.next;
            }
            while (node != this.head);
        }
    }

    /// <summary>
    /// Copies the list or a portion of it to an array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    void ICollection.CopyTo(Array array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        else if (array.Rank != 1 || array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException(nameof(array));
        }
        else if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        else if (array.Length - index < this.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(array));
        }

        T[]? tarray = array as T[];
        if (tarray != null)
        {
            this.CopyTo(tarray, index);
        }
        else
        {
            object?[]? objects = array as object[];
            if (objects == null)
            {
                throw new ArgumentException(nameof(array));
            }

            try
            {
                var node = this.First;
                if (node != null)
                {
                    for (var n = index; n < (index + this.Count); n++)
                    {
                        objects[n] = node!.Value;
                        node = node.next;

                        if (node == this.head)
                        {
                            break;
                        }
                    }
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(nameof(array));
            }
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="UnorderedLinkedList{T}"/>.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The object to remove from the <see cref="UnorderedLinkedList{T}"/>. </param>
    /// <returns>true if item is successfully removed.</returns>
    public bool Remove(T value)
    {
        var node = this.Find(value);
        if (node != null)
        {
            this.InternalRemoveNode(node);
            return true;
        }

        return false;
    }

    public bool IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    #region Enumerator

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements of a <see cref="UnorderedLinkedList{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly UnorderedLinkedList<T> list;
        private readonly int version;
        private Node? node;
        private T current;

        internal Enumerator(UnorderedLinkedList<T> list)
        {
            this.list = list;
            this.version = list.version;
            this.node = this.list.First;
            this.current = default(T)!;
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw ThrowVersionMismatch();
            }

            if (this.node == null)
            {
                this.current = default(T)!;
                return false;
            }

            this.current = this.node.Value;
            this.node = this.node.next;
            if (this.node == this.list.head)
            {
                this.node = null;
            }

            return true;
        }

        public void Dispose()
        {
        }

        public T Current => this.current;

        object? System.Collections.IEnumerator.Current => this.current;

        void System.Collections.IEnumerator.Reset() => this.Reset();

        internal void Reset()
        {
            if (this.version != this.list.version)
            {
                throw ThrowVersionMismatch();
            }

            this.node = this.list.head;
            this.current = default(T)!;
        }

        private static Exception ThrowVersionMismatch()
        {
            throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.'");
        }
    }

    #endregion

    #region LinkedList

    /// <summary>
    /// Finds the first node that contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>The first <see cref="Node"/> that contains the specified value, if found; otherwise, null.</returns>
    public Node? Find(T value)
    {
        var node = this.head;
        var c = EqualityComparer<T>.Default;
        if (node != null)
        {
            if (value != null)
            {
                do
                {
                    if (c.Equals(node!.value, value))
                    {
                        return node;
                    }

                    node = node.next;
                }
                while (node != this.head);
            }
            else
            {
                do
                {
                    if (node!.value == null)
                    {
                        return node;
                    }

                    node = node.next;
                }
                while (node != this.head);
            }
        }

        return null;
    }

    /// <summary>
    /// Adds a new node or value after an existing node in the list.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> after which to insert a new <see cref="Node"/> containing value.</param>
    /// <param name="value">The value to add to the list.</param>
    /// <returns>The new <see cref="Node"/> containing value.</returns>
    public Node AddAfter(Node node, T value)
    {
        this.ValidateNode(node);
        var result = this.NewNode(node.List, value);
        this.InternalInsertNodeBefore(node.next!, result);
        return result;
    }

    /// <summary>
    /// Adds the specified new node after the specified existing node in the list.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> after which to insert newNode.</param>
    /// <param name="newNode">The new <see cref="Node"/> to add to the list.</param>
    public void AddAfter(Node node, Node newNode)
    {
        this.ValidateNode(node);
        this.ValidateNewNode(newNode);
        this.InternalInsertNodeBefore(node.next!, newNode);
        newNode.list = this;
    }

    /// <summary>
    /// Adds a new node containing the specified value before the specified existing node in the list.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> before which to insert a new <see cref="Node"/> containing value.</param>
    /// <param name="value">The value to add to the list.</param>
    /// <returns>The new <see cref="Node"/> containing value.</returns>
    public Node AddBefore(Node node, T value)
    {
        this.ValidateNode(node);
        var result = this.NewNode(node.list!, value);
        this.InternalInsertNodeBefore(node, result);
        if (node == this.head)
        {
            this.head = result;
        }

        return result;
    }

    /// <summary>
    /// Adds the specified new node before the specified existing node in the list.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> before which to insert newNode.</param>
    /// <param name="newNode">The new <see cref="Node"/> to add to the list.</param>
    public void AddBefore(Node node, Node newNode)
    {
        this.ValidateNode(node);
        this.ValidateNewNode(newNode);
        this.InternalInsertNodeBefore(node, newNode);
        newNode.list = this;
        if (node == this.head)
        {
            this.head = newNode;
        }
    }

    /// <summary>
    /// Adds a new node or value at the start of the list.
    /// </summary>
    /// <param name="value">The value to add at the start of the list.</param>
    /// <returns>The new <see cref="Node"/> containing value.</returns>
    public Node AddFirst(T value)
    {
        var result = this.NewNode(this, value);
        if (this.head == null)
        {
            this.InternalInsertNodeToEmptyList(result);
        }
        else
        {
            this.InternalInsertNodeBefore(this.head, result);
            this.head = result;
        }

        return result;
    }

    /// <summary>
    /// Adds a new node or value at the start of the list.
    /// </summary>
    /// <param name="node">The new <see cref="Node"/> to add at the start of the list.</param>
    public void AddFirst(Node node)
    {
        this.ValidateNewNode(node);

        if (this.head == null)
        {
            this.InternalInsertNodeToEmptyList(node);
        }
        else
        {
            this.InternalInsertNodeBefore(this.head, node);
            this.head = node;
        }

        node.list = this;
    }

    /// <summary>
    /// Adds a new node or value at the end of the list.
    /// </summary>
    /// <param name="value">The value to add at the end of the list.</param>
    /// <returns>The new <see cref="Node"/> containing value.</returns>
    public Node AddLast(T value)
    {
        var result = this.NewNode(this, value);
        if (this.head == null)
        {
            this.InternalInsertNodeToEmptyList(result);
        }
        else
        {
            this.InternalInsertNodeBefore(this.head, result);
        }

        return result;
    }

    /// <summary>
    /// Adds a new node or value at the end of the list.
    /// </summary>
    /// <param name="node">The new <see cref="Node"/> to add at the end of the list.</param>
    public void AddLast(Node node)
    {
        this.ValidateNewNode(node);

        if (this.head == null)
        {
            this.InternalInsertNodeToEmptyList(node);
        }
        else
        {
            this.InternalInsertNodeBefore(this.head, node);
        }

        node.list = this;
    }

    /// <summary>
    /// Removes the specified node from the list.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to remove from the list.</param>
    public void Remove(Node node)
    {
        this.ValidateNode(node);
        this.InternalRemoveNode(node);
    }

    public void MoveToFirst(Node node)
    {
        if (node.next == node)
        {// Single node
            return;
        }

        node.next!.previous = node.previous;
        node.previous!.next = node.next;
        if (this.head == node)
        {
            this.head = node.next;
        }

        node.next = this.head;
        node.previous = this.head!.previous;
        this.head.previous!.next = node;
        this.head.previous = node;
        this.head = node;

        this.version++;
        this.size++;
    }

    public void MoveToLast(Node node)
    {
        if (node.next == node)
        {// Single node
            return;
        }

        node.next!.previous = node.previous;
        node.previous!.next = node.next;
        if (this.head == node)
        {
            this.head = node.next;
        }

        node.next = this.head;
        node.previous = this.head!.previous;
        this.head.previous!.next = node;
        this.head.previous = node;

        this.version++;
        this.size++;
    }

    /// <summary>
    /// Removes the node at the start of the list.
    /// </summary>
    public void RemoveFirst()
    {
        if (this.head == null)
        {
            throw new InvalidOperationException("The LinkedList is empty.'");
        }

        this.InternalRemoveNode(this.head);
    }

    /// <summary>
    /// Removes the node at the end of the list.
    /// </summary>
    public void RemoveLast()
    {
        if (this.head == null)
        {
            throw new InvalidOperationException("The LinkedList is empty.'");
        }

        this.InternalRemoveNode(this.head.previous!);
    }

    internal void InternalInsertNodeToEmptyList(Node newNode)
    {
        Debug.Assert(this.head == null && this.size == 0, "LinkedList must be empty when this method is called!");
        newNode.next = newNode;
        newNode.previous = newNode;
        this.head = newNode;
        this.version++;
        this.size++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalInsertNodeBefore(Node node, Node newNode)
    {
        newNode.next = node;
        newNode.previous = node.previous;
        node.previous!.next = newNode;
        node.previous = newNode;
        this.version++;
        this.size++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalRemoveNode(Node node)
    {
        Debug.Assert(node.List == this, "Deleting the node from another list!");
        Debug.Assert(this.head != null, "This method shouldn't be called on empty list!");

        if (node.next == node)
        {
            Debug.Assert(this.size == 1 && this.head == node, "this should only be true for a list with only one node");
            this.head = null;
        }
        else
        {
            node.next!.previous = node.previous;
            node.previous!.next = node.next;
            if (this.head == node)
            {
                this.head = node.next;
            }
        }

        // node.Clear();
        node.list = null!;
        node.previous = null;
        node.next = this.removed;
        this.removed = node;

        this.size--;
        this.version++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ValidateNewNode(Node node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.list != null)
        {
            throw new InvalidOperationException("The LinkedList node already belongs to a LinkedList.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ValidateNode(Node node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.List != this)
        {
            throw new InvalidOperationException("The LinkedList node does not belong to current LinkedList.");
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Node NewNode(UnorderedLinkedList<T> list, T value)
    {
        if (this.removed is not null)
        {
            var newNode = this.removed;
            this.removed = this.removed.next;
            newNode.list = list;
            newNode.value = value;
            return newNode;
        }
        else
        {
            return new Node(list, value);
        }
    }
}
