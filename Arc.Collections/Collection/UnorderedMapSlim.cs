// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1309 // Field names should not begin with underscore

public class UnorderedMapSlim<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
    where TValue : notnull
{// GetHashCode, EqualityComparerCode
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
        internal TKey key;     // Key of entry
        internal TValue value; // Value of entry
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

    public int Count => this._count - this._freeCount;

    public int Capacity => this._nodes.Length;

    #endregion

    public UnorderedMapSlim(uint minimumSize = 0)
    {
        this.Initialize(minimumSize);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (this.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"The given key '{key}' was not present in the collection.");
        }

        set => this.TryInsert(key, value, true);
    }

    public void Add(TKey key, TValue value) => this.TryInsert(key, value, true);

    public bool TryAdd(TKey key, TValue value) => this.TryInsert(key, value, false);

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

    public bool ContainsKey(TKey key) => this.TryGetValue(key, out _);

    public bool ContainsValue(TValue value)
    {
        var entries = this._nodes;
        if (value is null)
        {
            for (var i = 0; i < this._count; i++)
            {
                if (entries[i].next >= -1 && entries[i].value is null)
                {
                    return true;
                }
            }
        }

        var comparer = EqualityComparer<TValue>.Default; // EqualityComparerCode
        for (var i = 0; i < this._count; i++)
        {
            if (entries![i].next >= -1 && comparer.Equals(entries[i].value, value))
            {
                return true;
            }
        }

        return false;
    }

    public bool Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var comparer = EqualityComparer<TKey>.Default; // EqualityComparerCode
        uint collisionCount = 0;
        var hashCode = (uint)comparer.GetHashCode(key);
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

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var comparer = EqualityComparer<TKey>.Default; // EqualityComparerCode
        var hashCode = (uint)comparer.GetHashCode(key);
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
        var entries = this._nodes;
        var hashCode = (uint)comparer.GetHashCode(key);
        uint collisionCount = 0;
        ref int bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1; // Value in _buckets is 1-based

        while ((uint)i < (uint)entries.Length)
        {
            if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
            {
                if (overwrite)
                {
                    entries[i].value = value;
                    return true;
                }

                return false;
            }

            i = entries[i].next;

            collisionCount++;
            if (collisionCount > (uint)entries.Length)
            {
                throw new InvalidOperationException();
            }
        }

        int index;
        if (this._freeCount > 0)
        {
            index = this._freeList;
            this._freeList = StartOfFreeList - entries[this._freeList].next;
            this._freeCount--;
        }
        else
        {
            int count = this._count;
            if (count == entries.Length)
            {
                this.Resize();
                bucket = ref this.GetBucket(hashCode);
            }

            index = count;
            this._count = count + 1;
            entries = this._nodes;
        }

        ref Node entry = ref entries![index];
        entry.hashCode = hashCode;
        entry.next = bucket - 1; // Value in _buckets is 1-based
        entry.key = key;
        entry.value = value;
        bucket = index + 1; // Value in _buckets is 1-based

        return true;
    }

    private void Resize() => this.Resize(this._count << 1);

    private void Resize(int newSize)
    {
        var entries = new Node[newSize];
        var count = this._count;
        Array.Copy(this._nodes, entries, count);

        this._buckets = new int[newSize];
        for (var i = 0; i < count; i++)
        {
            if (entries[i].next >= -1)
            {
                ref int bucket = ref this.GetBucket(entries[i].hashCode);
                entries[i].next = bucket - 1; // Value in _buckets is 1-based
                bucket = i + 1;
            }
        }

        this._nodes = entries;
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
                ref Node entry = ref this._map._nodes![this._index++];

                if (entry.next >= -1)
                {
                    this._current = new KeyValuePair<TKey, TValue>(entry.key, entry.value);
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
