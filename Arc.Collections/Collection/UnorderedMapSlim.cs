// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections;

/// <summary>
/// Represents a collection of objects. <see cref="UnorderedMapSlim{TKey, TValue}"/> uses a hash table structure to store objects.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMapSlim<TKey, TValue>
{// GetHashCodeCode, CreateKeyCode
    public struct Node
    {
        public const int UnusedNode = -2;

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
    private uint bucketMask;
    private Node[] nodes = [];

    #endregion

    public UnorderedMapSlim()
    {
    }

    // public (int NodeIndex, bool NewlyAdded) Add(TKey key, TValue value) => this.Probe(key, value);

    private void Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);

        this.bucketMask = capacity - 1;
        this.buckets = new int[capacity];
        for (var n = 0; n < capacity; n++)
        {
            this.buckets[n] = -1;
        }

        this.nodes = new Node[capacity];
        // this.nullList = -1;
        // this.freeList = -1;
    }
}
