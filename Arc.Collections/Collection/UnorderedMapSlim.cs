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

    private struct Entry
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        public uint hashCode;

        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        public int next;
        public TKey key;     // Key of entry
        public TValue value; // Value of entry
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
    }

    #region FieldAndProperty

    private int[] _buckets;
    private Entry[] _entries;
    private int _count;
    private int _freeList;
    private int _freeCount;

    public int Count => this._count - this._freeCount;

    public int Capacity => this._entries.Length;

    #endregion

    public UnorderedMapSlim(uint minimumSize = 0)
    {
        this.Initialize(minimumSize);
    }

    public TValue this[TKey key]
    {
        get
        {
            ref TValue value = ref this.FindValue(key);
            if (!Unsafe.IsNullRef(ref value))
            {
                return value;
            }

            throw new KeyNotFoundException($"The given key '{key}' was not present in the collection.");
        }

        set => this.TryInsert(key, value, true);
    }

    public void Add(TKey key, TValue value) => this.TryInsert(key, value, true);

    public void Clear()
    {
        var count = this._count;
        if (count > 0)
        {
            Array.Clear(this._buckets);

            this._count = 0;
            this._freeList = -1;
            this._freeCount = 0;
            Array.Clear(this._entries, 0, count);
        }
    }

    public bool ContainsKey(TKey key) => !Unsafe.IsNullRef(ref this.FindValue(key));

    public bool ContainsValue(TValue value)
    {
        var entries = this._entries;
        if (value is null)
        {
            for (var i = 0; i < this._count; i++)
            {
                if (entries![i].next >= -1 && entries[i].value is null)
                {
                    return true;
                }
            }
        }
        else if (typeof(TValue).IsValueType)
        {
            for (var i = 0; i < this._count; i++)
            {// EqualityComparerCode
                if (entries![i].next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].value, value))
                {
                    return true;
                }
            }
        }
        else
        {
            var defaultComparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < this._count; i++)
            {// EqualityComparerCode
                if (entries![i].next >= -1 && defaultComparer.Equals(entries[i].value, value))
                {
                    return true;
                }
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

        uint collisionCount = 0;
        var hashCode = (uint)key.GetHashCode();
        ref int bucket = ref this.GetBucket(hashCode);
        var entries = this._entries;
        var last = -1;
        var i = bucket - 1;
        while (i >= 0)
        {
            ref Entry entry = ref entries[i];

            if (entry.hashCode == hashCode && key.Equals(entry.key))
            {// EqualityComparerCode
                if (last < 0)
                {
                    bucket = entry.next + 1; // Value in buckets is 1-based
                }
                else
                {
                    entries[last].next = entry.next;
                }

                entry.next = StartOfFreeList - this._freeList;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
                {
                    entry.key = default!;
                }

                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
                {
                    entry.value = default!;
                }

                this._freeList = i;
                this._freeCount++;
                return true;
            }

            last = i;
            i = entry.next;

            collisionCount++;
            if (collisionCount > (uint)entries.Length)
            {
                throw new InvalidOperationException();
            }
        }

        return false;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ref TValue valRef = ref this.FindValue(key);
        if (!Unsafe.IsNullRef(ref valRef))
        {
            value = valRef;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryAdd(TKey key, TValue value) => this.TryInsert(key, value, false);

    [MemberNotNull(nameof(_buckets), nameof(_entries))]
    private uint Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);
        this._buckets = new int[capacity];
        this._entries = new Entry[capacity];

        this._freeList = -1;

        return capacity;
    }

    private ref TValue FindValue(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        ref Entry entry = ref Unsafe.NullRef<Entry>();
        if (typeof(TKey).IsValueType)
        {
            var hashCode = (uint)key.GetHashCode();
            var i = this.GetBucket(hashCode);
            var entries = this._entries;
            uint collisionCount = 0;

            i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
            do
            {
                if ((uint)i >= (uint)entries.Length)
                {
                    goto ReturnNotFound;
                }

                entry = ref entries[i];
                if (entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key))
                {// EqualityComparerCode
                    goto ReturnFound;
                }

                i = entry.next;

                collisionCount++;
            }
            while (collisionCount <= (uint)entries.Length);
        }
        else
        {
            var hashCode = (uint)key.GetHashCode();
            var i = this.GetBucket(hashCode);
            var entries = this._entries;
            uint collisionCount = 0;
            i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
            do
            {
                if ((uint)i >= (uint)entries.Length)
                {
                    goto ReturnNotFound;
                }

                entry = ref entries[i];
                if (entry.hashCode == hashCode && key.Equals(entry.key))
                {// EqualityComparerCode
                    goto ReturnFound;
                }

                i = entry.next;

                collisionCount++;
            }
            while (collisionCount <= (uint)entries.Length);
        }

        throw new InvalidOperationException(); // ConcurrentOperation

ReturnFound:
        ref TValue value = ref entry.value;
Return:
        return ref value;
ReturnNotFound:
        value = ref Unsafe.NullRef<TValue>();
        goto Return;
    }

    private bool TryInsert(TKey key, TValue value, bool overwrite)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var entries = this._entries;
        var hashCode = (uint)key.GetHashCode();
        uint collisionCount = 0;
        ref int bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1; // Value in _buckets is 1-based

        if (typeof(TKey).IsValueType)
        {
            while ((uint)i < (uint)entries.Length)
            {// EqualityComparerCode
                if (entries[i].hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entries[i].key, key))
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
        }
        else
        {
            while ((uint)i < (uint)entries.Length)
            {// EqualityComparerCode
                if (entries[i].hashCode == hashCode && key.Equals(entries[i].key))
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
            entries = this._entries;
        }

        ref Entry entry = ref entries![index];
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
        var entries = new Entry[newSize];
        var count = this._count;
        Array.Copy(this._entries, entries, count);

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

        this._entries = entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucket(uint hashCode)
    {
        var buckets = this._buckets;
        return ref buckets[hashCode & (buckets.Length - 1)];
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
                ref Entry entry = ref this._map._entries![this._index++];

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
