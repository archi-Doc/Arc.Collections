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

    private readonly struct Table
    {
    }

    #region FieldAndProperty

    private int[] buckets = [];
    private Node[] nodes = [];
    private int nodeCount;
    private int freeList;
    private int freeCount;
    // private int nullList;

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

    public (int NodeIndex, bool NewlyAdded) Add(TKey key, TValue value) => this.Probe(key, _ => value);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        (var nodeIndex, var newlyAdded) = this.Probe(key, default);
        if (nodeIndex == UnusedIndex)
        {
            value = default;
            return false;
        }
        else
        {
            value = this.nodes[nodeIndex].Value;
            return true;
        }
    }

    private void Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);

        this.buckets = new int[capacity];
        for (var n = 0; n < capacity; n++)
        {
            this.buckets[n] = UnusedIndex;
        }

        this.nodes = new Node[capacity];
        // this.nullList = UnusedIndex;
        this.freeList = UnusedIndex;
    }

    private (int NodeIndex, bool NewlyAdded) Probe(TKey key, Func<TKey, TValue>? valueFactory)
    {
        if (this.nodeCount == this.nodes.Length)
        {
            // this.Resize();
        }

        /*if (key == null)
        {// Null key
            if (this.AllowDuplicate == false && this.nullList != UnusedIndex)
            {
                return (this.nullList, false);
            }

            newIndex = this.NewNode();
            this.nodes[newIndex].hash = 0;
            this.nodes[newIndex].key = key!;
            this.nodes[newIndex].value = value;

            if (this.nullList == UnusedIndex)
            {
                this.nodes[newIndex].next = UnusedIndex;
                this.nullList = newIndex;
            }
            else
            {
                this.nodes[newIndex].next = this.nullList;
                this.nullList = newIndex;
            }

            return (newIndex, true);
        }
        else*/

        var hash = key.GetHashCode();
        var index = hash & (this.buckets.Length - 1);
        if (valueFactory is null ||
            !this.AllowDuplicate)
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

            if (valueFactory is null)
            {
                return (UnusedIndex, false);
            }
        }

        var newIndex = this.NewNode();
        this.nodes[newIndex].hash = hash;
        this.nodes[newIndex].key = key;
        this.nodes[newIndex].value = valueFactory(key);

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
}
