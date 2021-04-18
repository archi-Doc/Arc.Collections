// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collection.HotMethod;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Collection
{
    /// <summary>
    /// Represents a collection of objects. <see cref="UnorderedMapClass{TKey, TValue}"/> uses a hash table structure to store objects.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    public class UnorderedMapClass<TKey, TValue>
    {
        #region Node

        /// <summary>
        /// Represents a node in a <see cref="UnorderedMapClass{TKey, TValue}"/>.
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

            public override string ToString() => this.Key?.ToString() + " : " + this.Value?.ToString();

            internal void Reset(int hashCode, TKey key, TValue value)
            {
                this.HashCode = hashCode;
                this.Key = key;
                this.Value = value;
                // this.Previous = null;
                // this.Next = null;
            }
        }

        #endregion

        public UnorderedMapClass()
            : this(0, null)
        {
        }

        public UnorderedMapClass(int capacity)
            : this(capacity, null)
        {
        }

        public UnorderedMapClass(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        public UnorderedMapClass(int capacity, IEqualityComparer<TKey>? comparer)
        {
            this.Initialize(capacity);
            this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
            this.HotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.Comparer);
        }

        public UnorderedMapClass(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        public UnorderedMapClass(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
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

        private const int MinLogCapacity = 4;
        private int version;
        private Node?[] hashTable = default!;
        private Node? nullNode;
        private int hashMask;

        private void Initialize(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity == 0)
            {
                this.hashTable = Array.Empty<Node>();
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

                if (log < MinLogCapacity)
                {
                    log = MinLogCapacity;
                }
                else if (log > 31)
                {
                    log = 31;
                }

                this.hashTable = new Node[1 << log];
                this.hashMask = this.hashTable.Length - 1;
            }
        }

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="UnorderedMapClass{TKey, TValue}"/>.
        /// </summary>
        public int Count { get; private set; }

        public IEqualityComparer<TKey> Comparer { get; private set; }

        public IHotMethod2<TKey, TValue>? HotMethod2 { get; private set; }

        public bool AllowMultiple { get; protected set; }

        #region Main

        public TValue this[TKey key]
        {
            get
            {
                var node = this.FindFirstNode(key);
                if (node == null)
                {
                    throw new KeyNotFoundException();
                }

                return node.Value;
            }

            set
            {
                var result = this.Add(key, value);
                if (!result.newlyAdded)
                {
                    result.node.Value = value;
                }
            }
        }

        public bool ContainsKey(TKey? key) => this.FindFirstNode(key) != null;

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            var node = this.FindFirstNode(key);
            if (node == null)
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }

        /// <summary>
        /// Removes all elements from a collection.
        /// </summary>
        public void Clear()
        {
            this.version = 0;
            this.hashTable = Array.Empty<Node>();
            this.nullNode = null;
            this.hashMask = 0;
            this.Count = 0;
        }

        /// <summary>
        /// Copies the elements of the collection to the specified array of KeyValuePair structures, starting at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional array of KeyValuePair structures that is the destination of the elements.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => ((ICollection)this).CopyTo(array, index);

        /// <summary>
        /// Removes the first element with the specified key from a collection.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is found and successfully removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey? key)
        {
            var p = this.FindFirstNode(key);
            if (p == null)
            {
                return false;
            }

            this.RemoveNode(p);
            return true;
        }

        /// <summary>
        /// Searches for the first <see cref="UnorderedMapClass{TKey, TValue}.Node"/> with the specified key.
        /// </summary>
        /// <param name="key">The key to search in a collection.</param>
        /// <returns>The first node with the specified key.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node? FindFirstNode(TKey? key)
        {
            /*if (this.HotMethod2 != null)
            {// HotMethod is available for value type (key is not null).
                return this.HotMethod2.SearchHashtable(this.hashTable, key!);
            }
            else */
            if (key == null)
            {
                return this.nullNode;
            }
            else
            {
                var hashCode = this.Comparer.GetHashCode(key);
                var index = hashCode & this.hashMask;
                var n = this.hashTable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && this.Comparer.Equals(n.Key, key))
                    {// Identical
                        return n;
                    }

                    if (n == this.hashTable[index])
                    {
                        break;
                    }
                    else
                    {
                        n = n.Next;
                    }
                }

                return null; // Not found
            }
        }

        /// <summary>
        /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>node: the added <see cref="UnorderedMapClass{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded:true if the new key is inserted.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value) => this.Probe(key, value, null);

        /// <summary>
        /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <param name="reuse">Reuse a node to avoid memory allocation.</param>
        /// <returns>node: the added <see cref="UnorderedMapClass{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the new key is inserted.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value, Node reuse) => this.Probe(key, value, reuse);

        /// <summary>
        /// Updates the node's key with the specified key. Removes the node and inserts in the correct position if necessary.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="node">The <see cref="UnorderedMapClass{TKey, TValue}.Node"/> to replace.</param>
        /// <param name="key">The key to set.</param>
        /// <returns>true if the node is replaced.</returns>
        public bool ReplaceNode(Node node, TKey key)
        {
            if (this.Comparer.Equals(node.Key, key))
            {// Identical
                return false;
            }

            var value = node.Value;
            this.RemoveNode(node);
            this.Probe(key, value, node);
            return true;
        }

        /// <summary>
        /// Removes a specified node from the collection.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="node">The <see cref="UnorderedMapClass{TKey, TValue}.Node"/> to remove.</param>
        public void RemoveNode(Node node)
        {
            if (node.Key == null)
            {
                if (node.Next == node)
                {
                    this.nullNode = null;
                }
                else
                {
                    node.Next!.Previous = node.Previous;
                    node.Previous!.Next = node.Next;
                    if (this.nullNode == node)
                    {
                        this.nullNode = node.Next;
                    }
                }
            }
            else
            {
                var index = node.HashCode & this.hashMask;
                if (node.Next == node)
                {
                    this.hashTable[index] = null;
                }
                else
                {
                    node.Next!.Previous = node.Previous;
                    node.Previous!.Next = node.Next;
                    if (this.hashTable[index] == node)
                    {
                        this.hashTable[index] = node.Next;
                    }
                }
            }

            this.version++;
            this.Count--;
        }

        /// <summary>
        /// Adds an element to the map. If the element is already in the map, this method returns the stored node without creating a new node.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="key">The element to add to the set.</param>
        /// <returns>node: the added <see cref="UnorderedMapClass{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the new key is inserted.</returns>
        private (Node node, bool newlyAdded) Probe(TKey key, TValue value, Node? reuse)
        {
            if (this.Count >= (this.hashTable.Length >> 1))
            {
                this.Resize();
            }

            Node newNode;
            /*if (this.HotMethod2 != null)
            {// HotMethod is available for value type (key is not null).
                (var node, var hashCode, var index) = this.HotMethod2.Probe(this.AllowMultiple, this.hashTable, key!);
                if (!this.AllowMultiple && node != null)
                {
                    return (node, false);
                }

                if (reuse != null)
                {
                    reuse.Reset(hashCode, key, value);
                    newNode = reuse;
                }
                else
                {
                    newNode = new Node(hashCode, key, value);
                }

                if (this.hashTable[index] == null)
                {
                    newNode.Previous = newNode;
                    newNode.Next = newNode;
                    this.hashTable[index] = newNode;
                }
                else
                {
                    this.InternalInsertNodeBefore(this.hashTable[index]!, newNode);
                }

                this.version++;
                this.Count++;
                return (newNode, true);
            }
            else */if (key == null)
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
                var hashCode = this.Comparer.GetHashCode(key);
                var index = hashCode & this.hashMask;
                if (!this.AllowMultiple)
                {
                    var n = this.hashTable[index];
                    while (n != null)
                    {
                        if (n.HashCode == hashCode && this.Comparer.Equals(n.Key, key))
                        {// Identical
                            return (n, false);
                        }

                        // Next item
                        if (n == this.hashTable[index])
                        {
                            break;
                        }
                        else
                        {
                            n = n.Next;
                        }
                    }
                }

                if (reuse != null)
                {
                    reuse.Reset(hashCode, key, value);
                    newNode = reuse;
                }
                else
                {
                    newNode = new Node(hashCode, key, value);
                }

                if (this.hashTable[index] == null)
                {
                    newNode.Previous = newNode;
                    newNode.Next = newNode;
                    this.hashTable[index] = newNode;
                }
                else
                {
                    this.InternalInsertNodeBefore(this.hashTable[index]!, newNode);
                }

                this.version++;
                this.Count++;
                return (newNode, true);
            }
        }

        internal void Resize()
        {
            const int minimumCapacity = 1 << MinLogCapacity;
            var newSize = this.hashTable.Length << 1;
            if (newSize < minimumCapacity)
            {
                newSize = minimumCapacity;
            }

            var newMask = newSize - 1;
            var newTable = new Node[newSize];
            for (var i = 0; i < this.hashTable.Length; i++)
            {
                var node = this.hashTable[i];
                while (node != null)
                {
                    var nodeNext = node.Next;

                    // Add
                    var i2 = node.HashCode & newMask;
                    if (newTable[i2] == null)
                    {
                        node.Previous = node;
                        node.Next = node;
                        newTable[i2] = node;
                    }
                    else
                    {
                        node.Previous = newTable[i2]!.Previous;
                        node.Next = newTable[i2];
                        newTable[i2]!.Previous!.Next = node;
                        newTable[i2]!.Previous = node;
                    }

                    // Next item
                    if (nodeNext == this.hashTable[i])
                    {
                        break;
                    }
                    else
                    {
                        node = nodeNext;
                    }
                }
            }

            // New table
            this.hashTable = newTable;
            this.hashMask = newMask;
            this.version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InternalInsertNodeBefore(Node node, Node newNode)
        {
            newNode.Next = node;
            newNode.Previous = node.Previous;
            node.Previous!.Next = newNode;
            node.Previous = newNode;
        }

        #endregion
    }
}
