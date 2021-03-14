// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401

namespace Arc.Collection
{
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
            public Node? Next => this.next == null || this == this.List.head ? null : this.next;

            /// <summary>
            /// Gets the value contained in the node.
            /// </summary>
            public T Value => this.value;

            public ref T ValueRef => ref this.value;

            internal Node(UnorderedLinkedList<T> list, T value)
            {
                this.list = list;
                this.value = value;
            }

            internal void Clear()
            {
                this.list = null!;
                this.previous = null;
                this.next = null;
            }
        }

        protected Node? head; // doubly-Linked circular list.
        protected int size;
        protected int version;

        /// <summary>
        /// Gets the first node.
        /// </summary>
        public Node? First => this.head;

        /// <summary>
        /// Gets the last node.
        /// </summary>
        public Node? Last => this.head?.Previous;

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
                n.Clear();
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
                    for (var n = index; n < (index + this.Count); n++)
                    {
                        if (node == null)
                        {
                            break;
                        }

                        objects[n] = node.Value;
                        node = node.Next;
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

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

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

        public Node? Find(T value)
        {
            var n = this.First;
            var c = EqualityComparer<T>.Default;

            if (value == null)
            {
                while (n != null)
                {
                    if (n.Value == null)
                    {
                        break;
                    }

                    n = n.Next;
                }
            }
            else
            {
                while (n != null)
                {
                    if (c.Equals(n.Value, value))
                    {
                        break;
                    }

                    n = n.Next;
                }
            }

            return n;
        }

        public Node AddAfter(Node node, T value)
        {
            this.ValidateNode(node);
            var result = new Node(node.List, value);
            this.InternalInsertNodeBefore(node.next!, result);
            return result;
        }

        public void AddAfter(Node node, Node newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node.next!, newNode);
            newNode.list = this;
        }

        public Node AddBefore(Node node, T value)
        {
            ValidateNode(node);
            Node result = new Node(node.list!, value);
            InternalInsertNodeBefore(node, result);
            if (node == head)
            {
                head = result;
            }
            return result;
        }

        public void AddBefore(Node node, Node newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node, newNode);
            newNode.list = this;
            if (node == head)
            {
                head = newNode;
            }
        }

        public Node AddFirst(T value)
        {
            Node result = new Node(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
                head = result;
            }
            return result;
        }

        public void AddFirst(Node node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
                head = node;
            }
            node.list = this;
        }

        public Node AddLast(T value)
        {
            Node result = new Node(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
            }
            return result;
        }

        public void AddLast(Node node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
            }
            node.list = this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalInsertNodeBefore(Node node, Node newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev!.next = newNode;
            node.prev = newNode;
            version++;
            count++;
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
                node.next!.prev = node.prev;
                node.prev!.next = node.next;
                if (this.head == node)
                {
                    this.head = node.next;
                }
            }

            node.Clear();
            this.size--;
            this.version++;
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
    }
}
