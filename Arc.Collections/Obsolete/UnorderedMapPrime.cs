// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Collections.Obsolete;

/// <summary>
/// Represents a collection of objects. <see cref="UnorderedMapPrime{TKey, TValue}"/> uses a hash table structure to store objects.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMapPrime<TKey, TValue>
{
    private const int HashCodeMask = 0x7FFFFFFF;

    private struct Node
    {
        public int HashCode; // Lower 31 bits of hash code, -1 if unused
        public int Previous;   // Index of previous entry
        public int Next;        // Index of next entry
        public TKey Key;      // Key
        public TValue Value; // Value
    }

    public UnorderedMapPrime()
        : this(0, null)
    {
    }

    public UnorderedMapPrime(int capacity)
        : this(capacity, null)
    {
    }

    public UnorderedMapPrime(IEqualityComparer<TKey> comparer)
        : this(0, comparer)
    {
    }

    public UnorderedMapPrime(int capacity, IEqualityComparer<TKey>? comparer)
    {
        this.Initialize(capacity);
        this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    public UnorderedMapPrime(IDictionary<TKey, TValue> dictionary)
        : this(dictionary, null)
    {
    }

    public UnorderedMapPrime(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
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

    private int version;
    private int[] buckets = default!;
    private Node[] nodes = default!;
    private int nodeCount;
    private int freeList;
    private int freeCount;
    private int nullList;

    private void Initialize(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (capacity == 0)
        {
            this.buckets = Array.Empty<int>();
            this.nodes = Array.Empty<Node>();
        }
        else
        {
            var size = PrimeHelper.GetPrime(capacity);
            this.buckets = new int[size];
            for (var n = 0; n < size; n++)
            {
                this.buckets[n] = -1;
            }

            this.nodes = new Node[size];
        }

        this.nullList = -1;
        this.freeList = -1;
    }

    /// <summary>
    /// Gets the number of nodes actually contained in the <see cref="UnorderedMapPrime{TKey, TValue}"/>.
    /// </summary>
    public int Count => this.nodeCount - this.freeCount;

    public IEqualityComparer<TKey> Comparer { get; private set; }

    public bool AllowMultiple { get; protected set; }

    #region Main

    public TValue this[TKey key]
    {
        get
        {
            var index = this.FindFirstNode(key);
            if (index == -1)
            {
                throw new KeyNotFoundException();
            }

            return this.nodes[index].Value;
        }

        set
        {
            var result = this.Add(key, value);
            if (!result.NewlyAdded)
            {
                this.nodes[result.NodeIndex].Value = value;
            }
        }
    }

    public bool ContainsKey(TKey? key) => this.FindFirstNode(key) != -1;

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        if (key == null)
        {
            if (this.nullList != -1)
            {
                value = this.nodes[this.nullList].Value;
                return true;
            }
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key!) & HashCodeMask;
            var index = hashCode % this.buckets.Length;
            var i = this.buckets[index];
            while (i >= 0)
            {
                ref Node n = ref this.nodes[i];
                if (n.HashCode == hashCode && this.Comparer.Equals(n.Key, key!))
                {// Identical
                    value = n.Value;
                    return true;
                }

                i = n.Next;
            }
        }

        value = default;
        return false; // Not found
    }

    /// <summary>
    /// Removes all elements from a collection.
    /// </summary>
    public void Clear()
    {
        if (this.nodeCount > 0)
        {
            for (var i = 0; i < this.buckets.Length; i++)
            {
                this.buckets[i] = -1;
            }

            Array.Clear(this.nodes, 0, this.nodeCount);
            this.nodeCount = 0;
            this.freeList = -1;
            this.freeCount = 0;
            this.nullList = -1;
        }
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
        if (p == -1)
        {
            return false;
        }

        this.RemoveNode(p);
        return true;
    }

    /// <summary>
    /// Searches for the first <see cref="UnorderedMapPrime{TKey, TValue}.Node"/> index with the specified key.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <returns>The first node index with the specified key. -1: not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindFirstNode(TKey? key)
    {
        if (key == null)
        {
            return this.nullList;
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key!) & HashCodeMask;
            var index = hashCode % this.buckets.Length;
            var i = this.buckets[index];
            while (i >= 0)
            {
                ref Node n = ref this.nodes[i];
                if (n.HashCode == hashCode && this.Comparer.Equals(n.Key, key!))
                {// Identical
                    return i;
                }

                i = n.Next;
            }

            return -1; // Not found
        }
    }

    /// <summary>
    /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets NewlyAdded to false.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns>NodeIndex: the added <see cref="UnorderedMapPrime{TKey, TValue}.Node"/>.<br/>
    /// NewlyAdded:true if the new key is inserted.</returns>
    public (int NodeIndex, bool NewlyAdded) Add(TKey key, TValue value) => this.Probe(key, value);

    /// <summary>
    /// Updates the node's key with the specified key. Removes the node and inserts in the correct position if necessary.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="UnorderedMapPrime{TKey, TValue}.Node"/> to replace.</param>
    /// <param name="key">The key to set.</param>
    /// <returns>true if the node is replaced.</returns>
    public bool ReplaceNode(int nodeIndex, TKey key)
    {
        if (this.Comparer.Equals(this.nodes[nodeIndex].Key, key))
        {// Identical
            return false;
        }

        var value = this.nodes[nodeIndex].Value;
        this.RemoveNode(nodeIndex);
        this.Probe(key, value);
        return true;
    }

    /// <summary>
    /// Removes a specified node from the collection.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="UnorderedMapPrime{TKey, TValue}.Node"/> to remove.</param>
    public void RemoveNode(int nodeIndex)
    {
        var nodePrevious = this.nodes[nodeIndex].Previous;
        var nodeNext = this.nodes[nodeIndex].Next;
        if (this.nodes[nodeIndex].Key == null)
        {
            if (nodePrevious == -1)
            {
                this.nullList = nodeNext;
            }
            else
            {
                this.nodes[nodePrevious].Next = nodeNext;
            }

            if (nodeNext != -1)
            {
                this.nodes[nodeNext].Previous = nodePrevious;
            }
        }
        else
        {
            var index = this.nodes[nodeIndex].HashCode % this.buckets.Length;
            if (nodePrevious == -1)
            {
                this.buckets[index] = nodeNext;
            }
            else
            {
                this.nodes[nodePrevious].Next = nodeNext;
            }

            if (nodeNext != -1)
            {
                this.nodes[nodeNext].Previous = nodePrevious;
            }
        }

        this.nodes[nodeIndex].HashCode = -1;
        this.nodes[nodeIndex].Next = this.freeList;
        this.nodes[nodeIndex].Key = default!;
        this.nodes[nodeIndex].Value = default!;
        this.freeList = nodeIndex;
        this.freeCount++;

        this.version++;
    }

    /// <summary>
    /// Adds an element to the map. If the element is already in the map, this method returns the stored node without creating a new node.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="key">The element to add to the set.</param>
    /// <returns>node: the added <see cref="UnorderedMapPrime{TKey, TValue}.Node"/>.<br/>
    /// NewlyAdded: true if the new key is inserted.</returns>
    private (int NodeIndex, bool NewlyAdded) Probe(TKey key, TValue value)
    {
        if (this.nodeCount == this.nodes.Length)
        {
            this.Resize();
        }

        int newIndex;
        if (key == null)
        {// Null key
            if (this.AllowMultiple == false && this.nullList != -1)
            {
                return (this.nullList, false);
            }

            newIndex = this.NewNode();
            this.nodes[newIndex].HashCode = 0;
            this.nodes[newIndex].Key = key;
            this.nodes[newIndex].Value = value;

            if (this.nullList == -1)
            {
                this.nodes[newIndex].Previous = -1;
                this.nodes[newIndex].Next = -1;
                this.nullList = newIndex;
            }
            else
            {
                this.nodes[newIndex].Next = this.nullList;
                this.nodes[newIndex].Previous = -1;
                this.nodes[this.nullList].Previous = newIndex;
                this.nullList = newIndex;
            }

            return (newIndex, true);
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key!) & HashCodeMask;
            var index = hashCode % this.buckets.Length;
            if (!this.AllowMultiple)
            {
                var i = this.buckets[index];
                while (i >= 0)
                {
                    if (this.nodes[i].HashCode == hashCode && this.Comparer.Equals(this.nodes[i].Key, key))
                    {// Identical
                        return (i, false);
                    }

                    i = this.nodes[i].Next;
                }
            }

            newIndex = this.NewNode();
            ref Node newNode = ref this.nodes[newIndex];
            newNode.HashCode = hashCode;
            newNode.Key = key;
            newNode.Value = value;

            if (this.buckets[index] == -1)
            {
                newNode.Previous = -1;
                newNode.Next = -1;
                this.buckets[index] = newIndex;
            }
            else
            {
                newNode.Previous = -1;
                newNode.Next = this.buckets[index];
                this.nodes[this.buckets[index]].Previous = newIndex;
                this.buckets[index] = newIndex;
            }

            this.version++;
            return (newIndex, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewNode()
    {
        int index;
        if (this.freeCount > 0)
        {// Free list
            index = this.freeList;
            this.freeList = this.nodes[index].Next;
            this.freeCount--;
        }
        else
        {
            index = this.nodeCount;
            this.nodeCount++;
        }

        return index;
    }

    internal void Resize()
    {
        var newSize = PrimeHelper.ExpandPrime(this.nodeCount);
        var newBuckets = new int[newSize];
        for (var i = 0; i < newBuckets.Length; i++)
        {
            newBuckets[i] = -1;
        }

        var newNodes = new Node[newSize];
        Array.Copy(this.nodes, 0, newNodes, 0, this.nodeCount);

        for (var i = 0; i < this.nodeCount; i++)
        {
            ref Node newNode = ref newNodes[i];
            if (newNode.HashCode >= 0)
            {
                var bucket = newNode.HashCode % newSize;
                if (newBuckets[bucket] == -1)
                {
                    newNode.Previous = -1;
                    newNode.Next = -1;
                    newBuckets[bucket] = i;
                }
                else
                {
                    var newBucket = newBuckets[bucket];
                    newNode.Previous = -1;
                    newNode.Next = newBucket;
                    newBuckets[bucket] = i;
                    newNodes[newBucket].Previous = i;
                }
            }
        }

        // Update
        this.version++;
        this.buckets = newBuckets;
        this.nodes = newNodes;
    }

    #endregion
}
