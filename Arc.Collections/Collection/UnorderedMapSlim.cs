// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1309 // Field names should not begin with underscore

/// <summary>
/// Represents a collection of key/value pairs that are organized based on the hash code of the key.
/// This is a lightweight implementation optimized for performance with minimal memory overhead.
/// </summary>
/// <typeparam name="TKey">The type of keys in the map. Keys must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values in the map.</typeparam>
public class UnorderedMapSlim<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{// GetHashCodeCode, EqualityComparerCode
    private const int StartOfFreeList = -3;

    public struct Node
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal uint hashCode;

        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        internal int next;
        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public TKey Key => this.key;

        public TValue Value => this.value;
    }

    #region FieldAndProperty

    private int[] _buckets;
    private Node[] _nodes;
    private int _count;
    private int _freeList;
    private int _freeCount;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    public int Count => this._count - this._freeCount;

    /// <summary>
    /// Gets the total capacity of the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    public int Capacity => this._nodes.Length;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMapSlim{TKey, TValue}"/> class that is empty with the specified initial capacity.
    /// </summary>
    /// <param name="minimumSize">The minimum capacity to allocate.</param>
    public UnorderedMapSlim(uint minimumSize = 0)
    {
        this.Initialize(minimumSize);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist in the collection.</exception>
    public TValue this[TKey key]
    {
        get
        {
            if (this.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException();
        }

        set => this.TryInsert(key, value, true);
    }

    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <exception cref="ArgumentNullException">key is null.</exception>
    public void Add(TKey key, TValue value) => this.TryInsert(key, value, true);

    /// <summary>
    /// Attempts to add the specified key and value to the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added successfully; <see langword="false"/> if the key already exists.</returns>
    /// <exception cref="ArgumentNullException">key is null.</exception>
    public bool TryAdd(TKey key, TValue value) => this.TryInsert(key, value, false);

    /// <summary>
    /// Removes all keys and values from the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    public void Clear()
    {
        var count = this._count;
        if (count > 0)
        {
            Array.Clear(this._buckets);

            this._count = 0;
            this._freeList = -1;
            this._freeCount = 0;
            Array.Clear(this._nodes, 0, count);
        }
    }

    /// <summary>
    /// Determines whether the <see cref="UnorderedMapSlim{TKey, TValue}"/> contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="UnorderedMapSlim{TKey, TValue}"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="UnorderedMapSlim{TKey, TValue}"/> contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
    public bool ContainsKey(TKey key) => this.TryGetValue(key, out _);

    /// <summary>
    /// Determines whether the <see cref="UnorderedMapSlim{TKey, TValue}"/> contains a specific value.
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="UnorderedMapSlim{TKey, TValue}"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="UnorderedMapSlim{TKey, TValue}"/> contains an element with the specified value; otherwise, <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value)
    {
        var nodes = this._nodes;
        if (value is null)
        {
            for (var i = 0; i < this._count; i++)
            {
                if (nodes[i].next >= -1 && nodes[i].value is null)
                {
                    return true;
                }
            }
        }

        var comparer = EqualityComparer<TValue>.Default; // EqualityComparerCode
        for (var i = 0; i < this._count; i++)
        {
            if (nodes[i].next >= -1 && comparer.Equals(nodes[i].value, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the element with the specified key from the <see cref="UnorderedMapSlim{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">key is null.</exception>
    /// <exception cref="InvalidOperationException">An error occurred during the operation.</exception>
    public bool Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var comparer = EqualityComparer<TKey>.Default; // EqualityComparerCode
        uint collisionCount = 0;
        var hashCode = (uint)comparer.GetHashCode(key); // GetHashCodeCode
        ref int bucket = ref this.GetBucket(hashCode);
        var nodes = this._nodes;
        var last = -1;
        var i = bucket - 1;
        while (i >= 0)
        {
            ref Node node = ref nodes[i];

            if (node.hashCode == hashCode && comparer.Equals(key, node.key))
            {
                if (last < 0)
                {
                    bucket = node.next + 1; // Value in buckets is 1-based
                }
                else
                {
                    nodes[last].next = node.next;
                }

                node.next = StartOfFreeList - this._freeList;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
                {
                    node.key = default!;
                }

                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
                {
                    node.value = default!;
                }

                this._freeList = i;
                this._freeCount++;
                return true;
            }

            last = i;
            i = node.next;

            collisionCount++;
            if (collisionCount > (uint)nodes.Length)
            {
                throw new InvalidOperationException();
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found;
    /// otherwise, the default value for the type of the value parameter.</param>
    /// <returns><see langword="true"/> if the object that implements <see cref="UnorderedMapSlim{TKey, TValue}"/> contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var comparer = EqualityComparer<TKey>.Default; // EqualityComparerCode
        var hashCode = (uint)comparer.GetHashCode(key); // GetHashCodeCode
        var i = this.GetBucket(hashCode);
        var nodes = this._nodes;
        uint collisionCount = 0;
        i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
        do
        {
            if ((uint)i >= (uint)nodes.Length)
            {
                value = default;
                return false;
            }

            ref Node node = ref nodes[i];
            if (node.hashCode == hashCode && comparer.Equals(key, node.key))
            {
                value = node.value;
                return true;
            }

            i = node.next;

            collisionCount++;
        }
        while (collisionCount <= (uint)nodes.Length);

        value = default;
        return false;
    }

    [MemberNotNull(nameof(_buckets), nameof(_nodes))]
    private uint Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);
        this._buckets = new int[capacity];
        this._nodes = new Node[capacity];
        this._freeList = -1;

        return capacity;
    }

    private bool TryInsert(TKey key, TValue value, bool overwrite)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var comparer = EqualityComparer<TKey>.Default; // EqualityComparerCode
        var nodes = this._nodes;
        var hashCode = (uint)comparer.GetHashCode(key); // GetHashCodeCode
        uint collisionCount = 0;
        ref int bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1; // Value in _buckets is 1-based

        while ((uint)i < (uint)nodes.Length)
        {
            if (nodes[i].hashCode == hashCode && comparer.Equals(nodes[i].key, key))
            {
                if (overwrite)
                {
                    nodes[i].value = value;
                    return true;
                }

                return false;
            }

            i = nodes[i].next;

            collisionCount++;
            if (collisionCount > (uint)nodes.Length)
            {
                throw new InvalidOperationException();
            }
        }

        int index;
        if (this._freeCount > 0)
        {
            index = this._freeList;
            this._freeList = StartOfFreeList - nodes[this._freeList].next;
            this._freeCount--;
        }
        else
        {
            int count = this._count;
            if (count == nodes.Length)
            {
                this.Resize();
                bucket = ref this.GetBucket(hashCode);
            }

            index = count;
            this._count = count + 1;
            nodes = this._nodes;
        }

        ref Node node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1; // Value in _buckets is 1-based
        node.key = key;
        node.value = value;
        bucket = index + 1; // Value in _buckets is 1-based

        return true;
    }

    private void Resize() => this.Resize(this._count << 1);

    private void Resize(int newSize)
    {
        var nodes = new Node[newSize];
        var count = this._count;
        Array.Copy(this._nodes, nodes, count);

        this._buckets = new int[newSize];
        for (var i = 0; i < count; i++)
        {
            if (nodes[i].next >= -1)
            {
                ref int bucket = ref this.GetBucket(nodes[i].hashCode);
                nodes[i].next = bucket - 1; // Value in _buckets is 1-based
                bucket = i + 1;
            }
        }

        this._nodes = nodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucket(uint hashCode)
    {
        var bucket = this._buckets;
        return ref bucket[hashCode & (bucket.Length - 1)];
    }

    #region IEnumerable

#pragma warning disable SA1202 // Elements should be ordered by access
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this);
#pragma warning restore SA1202 // Elements should be ordered by access

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly UnorderedMapSlim<TKey, TValue> _map;
        private int _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(UnorderedMapSlim<TKey, TValue> dictionary)
        {
            this._map = dictionary;
            this._index = 0;
            this._current = default;
        }

        public bool MoveNext()
        {
            while ((uint)this._index < (uint)this._map._count)
            {
                ref Node node = ref this._map._nodes[this._index++];

                if (node.next >= -1)
                {
                    this._current = new KeyValuePair<TKey, TValue>(node.key, node.value);
                    return true;
                }
            }

            this._index = this._map._count + 1;
            this._current = default;
            return false;
        }

        public KeyValuePair<TKey, TValue> Current => this._current;

        public void Dispose()
        {
        }

        object? IEnumerator.Current
        {
            get
            {
                if (this._index == 0 || (this._index == this._map._count + 1))
                {
                    throw new InvalidOperationException();
                }

                return new KeyValuePair<TKey, TValue>(this._current.Key, this._current.Value);
            }
        }

        void IEnumerator.Reset()
        {
            this._index = 0;
            this._current = default;
        }
    }

    #endregion
}
