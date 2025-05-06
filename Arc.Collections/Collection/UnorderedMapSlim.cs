// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

/// <summary>
/// Represents a collection of objects. <see cref="UnorderedMapSlim{TKey, TValue}"/> uses a hash table structure to store objects.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMapSlim<TKey, TValue>
    where TKey : notnull
{// GetHashCodeCode, CreateKeyCode
    private const int UnusedIndex = -1;

    public struct Node
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal int hash;
        internal int next;
        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public TKey Key => this.key;

        public TValue Value => this.value;
    }

    #region FieldAndProperty

    private int[] buckets;
    private Node[] nodes;
    private int nodeCount;
    private int freeList;
    private int freeCount;

    /// <summary>
    /// Gets or sets a value indicating whether the collection allows duplicate keys.
    /// </summary>
    public bool AllowDuplicate { get; protected set; }

    /// <summary>
    /// Gets the number of nodes actually contained in the <see cref="UnorderedMap{TKey, TValue}"/>.
    /// </summary>
    public int Count => this.nodeCount - this.freeCount;

    #endregion

    public UnorderedMapSlim(uint minimumSize = CollectionHelper.MinimumCapacity)
    {
        this.Initialize(minimumSize);
    }

    public (int NodeIndex, bool NewlyAdded) Add(TKey key, TValue value) => this.Probe(key, value);

    public ref Node FindNode(TKey key)
    {
        var hash = key.GetHashCode(); // GetHashCodeCode
        var i = this.buckets[hash & (this.buckets.Length - 1)];
        while (i >= 0)
        {
            ref Node node = ref this.nodes[i];
            if (node.hash == hash && node.key.Equals(key))
            {// Identical
                return ref node;
            }

            i = node.next;
        }

        return ref Unsafe.NullRef<Node>();
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = this.FindNode(key);
        if (Unsafe.IsNullRef(ref node))
        {// Not found
            value = default;
            return false;
        }
        else
        {// Found
            value = node.value;
            return true;
        }

        /*var hash = key.GetHashCode();
        var i = this.buckets[hash & (this.buckets.Length - 1)];
        while (i >= 0)
        {
            ref Node node = ref this.nodes[i];
            if (node.hash == hash && node.key.Equals(key))
            {// Identical
                value = node.value;
                return true;
            }

            i = node.next;
        }

        value = default;
        return false;*/
    }

    public bool Remove(TKey key)
    {
        var node = this.FindNode(key);
        if (Unsafe.IsNullRef(ref node))
        {// Not found
            return false;
        }
        else
        {// Found
            return true;
        }
    }

    /// <summary>
    /// Removes a specified node from the collection.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="UnorderedMap{TKey, TValue}.Node"/> to remove.</param>
    public void RemoveNode(int nodeIndex)
    {
        if (this.nodes[nodeIndex].IsInvalid())
        {
            return;
        }

        var nodePrevious = this.nodes[nodeIndex].previous;
        var nodeNext = this.nodes[nodeIndex].next;
        if (this.nodes[nodeIndex].key == null)
        {// Null list
            if (nodeIndex >= this.nodeCount)
            {// check node index.
                return;
            }

            if (nodePrevious == -1)
            {
                this.nullList = nodeNext;
            }
            else
            {
                this.nodes[nodePrevious].next = nodeNext;
            }

            if (nodeNext != -1)
            {
                this.nodes[nodeNext].previous = nodePrevious;
            }
        }
        else
        {
            // node index <= this.nodeCount
            var index = this.nodes[nodeIndex].hashCode & this.hashMask;
            if (nodePrevious == -1)
            {
                this.buckets[index] = nodeNext;
            }
            else
            {
                this.nodes[nodePrevious].next = nodeNext;
            }

            if (nodeNext != -1)
            {
                this.nodes[nodeNext].previous = nodePrevious;
            }
        }

        this.nodes[nodeIndex].hashCode = 0;
        this.nodes[nodeIndex].previous = Node.UnusedNode;
        this.nodes[nodeIndex].next = this.freeList;
        this.nodes[nodeIndex].key = default!;
        this.nodes[nodeIndex].value = default!;
        this.freeList = nodeIndex;
        this.freeCount++;

        this.version++;
    }

    [MemberNotNull(nameof(this.buckets), nameof(this.nodes))]
    private void Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);

        this.buckets = new int[capacity];
        for (var n = 0; n < capacity; n++)
        {
            this.buckets[n] = UnusedIndex;
        }

        this.nodes = new Node[capacity];
        this.freeList = UnusedIndex;
    }

    private (int NodeIndex, bool NewlyAdded) Probe(TKey key, TValue value)
    {
        if (this.nodeCount == this.nodes.Length)
        {
            this.Resize();
        }

        var hash = key.GetHashCode();
        var index = hash & (this.buckets.Length - 1);
        if (!this.AllowDuplicate)
        {
            var i = this.buckets[index];
            while (i >= 0)
            {
                ref Node node = ref this.nodes[i];
                if (node.hash == hash && node.key.Equals(key))
                {// Identical
                    return (i, false);
                }

                i = node.next;
            }
        }

        var newIndex = this.NewNode();
        this.nodes[newIndex].hash = hash;
        this.nodes[newIndex].key = key;
        this.nodes[newIndex].value = value;

        if (this.buckets[index] == UnusedIndex)
        {
            this.nodes[newIndex].next = UnusedIndex;
            this.buckets[index] = newIndex;
        }
        else
        {
            this.nodes[newIndex].next = this.buckets[index];
            this.buckets[index] = newIndex;
        }

        return (newIndex, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewNode()
    {
        int nodeIndex;
        if (this.freeCount > 0)
        {// Free list
            nodeIndex = this.freeList;
            this.freeList = this.nodes[nodeIndex].next;
            this.freeCount--;
        }
        else
        {
            nodeIndex = this.nodeCount;
            this.nodeCount++;
        }

        return nodeIndex;
    }

    private void Resize()
    {
        var newSize = this.nodes.Length << 1;
        var newMask = newSize - 1;
        var newBuckets = new int[newSize];
        for (var i = 0; i < newBuckets.Length; i++)
        {
            newBuckets[i] = UnusedIndex;
        }

        var newNodes = new Node[newSize];
        this.nodes.AsSpan(0, this.nodeCount).CopyTo(newNodes); // Array.Copy(this.nodes, 0, newNodes, 0, this.nodeCount);

        for (var i = 0; i < this.nodeCount; i++)
        {
            ref Node newNode = ref newNodes[i];
            if (newNode.IsValid())
            {
                var index = newNode.hash & newMask;
                if (newBuckets[index] == UnusedIndex)
                {
                    newNode.next = UnusedIndex;
                    newBuckets[index] = i;
                }
                else
                {
                    var newBucket = newBuckets[index];
                    newNode.next = newBucket;
                    newBuckets[index] = i;
                }
            }
        }

        // Update
        this.buckets = newBuckets;
        this.nodes = newNodes;
    }

}
