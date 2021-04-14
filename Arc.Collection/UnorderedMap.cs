// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collection.HotMethod;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Collection
{
    /// <summary>
    /// Represents a collection of objects. <see cref="UnorderedMap{TKey, TValue}"/> uses a hash table structure to store objects.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    public class UnorderedMap<TKey, TValue>
    {
        #region Node

        /// <summary>
        /// Represents a node in a <see cref="UnorderedMap{TKey, TValue}"/>.
        /// </summary>
        public class Node
        {
            internal Node(int hashCode, TKey key, TValue value)
            {
                this.HashCode = hashCode;
                this.Key = key;
                this.Value = value;
            }

            /// <summary>
            /// Gets the hash code contained in the node.
            /// </summary>
            public int HashCode { get; internal set; }

            /// <summary>
            /// Gets or sets the previous linked list node (doubly-Linked circular list).
            /// </summary>
            internal Node? Previous { get; set; }

            /// <summary>
            /// Gets or sets the next linked list node (doubly-Linked circular list).
            /// </summary>
            internal Node? Next { get; set; }

            /// <summary>
            /// Gets the key contained in the node.
            /// </summary>
            public TKey Key { get; internal set; }

            /// <summary>
            /// Gets the value contained in the node.
            /// </summary>
            public TValue Value { get; internal set; }

            internal void Reset(int hashCode, TKey key, TValue value)
            {
                this.HashCode = hashCode;
                this.Key = key;
                this.Value = value;
                this.Previous = null;
                this.Next = null;
            }
        }

        #endregion

        public UnorderedMap()
            : this(0, null)
        {
        }

        public UnorderedMap(int capacity)
            : this(capacity, null)
        {
        }

        public UnorderedMap(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public UnorderedMap(int capacity, IEqualityComparer<TKey>? comparer)
        {
            this.Initialize(capacity);
            this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public UnorderedMap(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        public UnorderedMap(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
            : this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            foreach (var pair in dictionary)
            {
                // this.Add(pair.Key, pair.Value);
            }
        }

        private const int MinimumLog = 4;
        private int version;
        private Node?[] hashtable = default!;
        private Node? nullNode;
        private int hashmask;

        private void Initialize(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity == 0)
            {
                this.hashtable = Array.Empty<Node>();
            }
            else
            {
                var log = -1;
                var n = capacity;
                while (n > 0)
                {
                    log++;
                    n >>= 1;
                }

                if (capacity != (1 << log))
                {
                    log++;
                }

                if (log < MinimumLog)
                {
                    log = MinimumLog;
                }
                else if (log > 31)
                {
                    log = 31;
                }

                this.hashtable = new Node[1 << log];
                this.hashmask = this.hashtable.Length - 1;
            }
        }

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="UnorderedMap{TKey, TValue}"/>.
        /// </summary>
        public int Count { get; private set; }

        public IEqualityComparer<TKey> Comparer { get; private set; }

        protected bool AllowMultiple { get; set; }

        #region Main

        /// <summary>
        /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>node: the added <see cref="UnorderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded:true if the new key is inserted.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value) => this.InternalAdd(key, value, null);

        /// <summary>
        /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <param name="reuse">Reuse a node to avoid memory allocation.</param>
        /// <returns>node: the added <see cref="UnorderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the new key is inserted.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value, Node reuse) => this.InternalAdd(key, value, reuse);

        /// <summary>
        /// Adds an element to the map. If the element is already in the map, this method returns the stored node without creating a new node.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The element to add to the set.</param>
        /// <returns>node: the added <see cref="OrderedMultiMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the new key is inserted.</returns>
        private (Node node, bool newlyAdded) InternalAdd(TKey key, TValue value, Node? reuse)
        {
            Node newNode;

            if (key == null)
            {// Null key
                if (this.AllowMultiple == false && this.nullNode != null)
                {
                    return (this.nullNode, false);
                }

                if (reuse != null)
                {
                    reuse.Reset(0, key, value);
                    newNode = reuse;
                }
                else
                {
                    newNode = new Node(0, key, value);
                }

                if (this.nullNode == null)
                {
                    newNode.Previous = newNode;
                    newNode.Next = newNode;
                    this.nullNode = newNode;
                }
                else
                {
                    this.InternalInsertNodeBefore(this.nullNode, newNode);
                }

                return (newNode, true);
            }
            else
            {
                var hashCode = key == null ? 0 : this.Comparer.GetHashCode(key);
                var index = hashCode & this.hashmask;
                var n = this.hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && this.Comparer.Equals(n.Key, key))
                    {// Identical
                        return (n, false);
                    }

                    n.Value = value;
                    this.version++;
                    return (n, false);
                }

                if (reuse != null)
                {
                    reuse.Reset(0, key, value);
                    newNode = reuse;
                }
                else
                {
                    newNode = new Node(0, key, value);
                }

                newNode.HashCode = hashCode;
                if (this.hashtable[index] == null)
                {
                    newNode.Previous = newNode;
                    newNode.Next = newNode;
                    this.hashtable[index] = newNode;
                }
                else
                {
                    this.InternalInsertNodeBefore(this.hashtable[index]!, newNode);
                }

                return (newNode, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalInsertNodeBefore(Node node, Node newNode)
        {
            newNode.Next = node;
            newNode.Previous = node.Previous;
            node.Previous!.Next = newNode;
            node.Previous = newNode;
            this.version++;
            this.Count++;
        }

        #endregion
    }
}
