﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

/// <summary>
/// Represents a collection of objects. <see cref="UnorderedMap{TKey, TValue}"/> uses a hash table structure to store objects.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMap<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
{
    public struct Node
    {
        public const int UnusedNode = -2;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal int hashCode;
        internal int previous;   // Index of previous node, UnusedNode(-2) if the node is not used.
        internal int next;        // Index of next node
        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public bool IsValid() => this.previous != UnusedNode;

        public bool IsInvalid() => this.previous == UnusedNode;

        public TKey Key => this.key;

        public TValue Value => this.value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    public UnorderedMap()
        : this(0, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the collection can contain.</param>
    public UnorderedMap(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    public UnorderedMap(IEqualityComparer<TKey> comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the collection can contain.</param>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    public UnorderedMap(int capacity, IEqualityComparer<TKey>? comparer)
    {
        this.Initialize(capacity);
        this.Comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The IDictionary implementation to copy to a new collection.</param>
    public UnorderedMap(IDictionary<TKey, TValue> dictionary)
        : this(dictionary, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The IDictionary implementation to copy to a new collection.</param>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    public UnorderedMap(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
        : this(dictionary != null ? dictionary.Count : 0, comparer)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        foreach (var pair in dictionary)
        {
            this.Add(pair.Key, pair.Value);
        }
    }

    public (Node[] Nodes, int Max) UnsafeGetNodes()
        => (this.nodes, this.nodeCount);

    #region Enumerator

    public Enumerator GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
    {
        internal const int KeyValuePair = 1;
        internal const int DictEntry = 2;

        private readonly UnorderedMap<TKey, TValue> map;
        private readonly int version;
        private readonly int getEnumeratorRetType;
        private int index;
        private TKey? key;
        private TValue? value;

        internal Enumerator(UnorderedMap<TKey, TValue> map, int getEnumeratorRetType)
        {
            this.map = map;
            this.version = this.map.version;
            this.getEnumeratorRetType = getEnumeratorRetType;
            this.index = 0;
            this.key = default;
            this.value = default;
        }

        public void Dispose()
        {
            this.index = 0;
            this.key = default;
            this.value = default;
        }

        public bool MoveNext()
        {
            if (this.version != this.map.version)
            {
                throw ThrowVersionMismatch();
            }

            while ((uint)this.index < (uint)this.map.nodeCount)
            {
                if (this.map.nodes[this.index].IsValid())
                {
                    this.key = this.map.nodes[this.index].key;
                    this.value = this.map.nodes[this.index].value;
                    this.index++;
                    return true;
                }

                this.index++;
            }

            this.index = this.map.nodeCount + 1;
            this.key = default;
            this.value = default;
            return false;
        }

        DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(this.key!, this.value!);

        object IDictionaryEnumerator.Key => this.key!;

        object IDictionaryEnumerator.Value => this.value!;

        public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(this.key!, this.value!);

        object? IEnumerator.Current
        {
            get
            {
                if (this.getEnumeratorRetType == DictEntry)
                {
                    return new DictionaryEntry(this.key!, this.value!);
                }
                else
                {
                    return new KeyValuePair<TKey, TValue>(this.key!, this.value!);
                }
            }
        }

        void System.Collections.IEnumerator.Reset() => this.Reset();

        internal void Reset()
        {
            if (this.version != this.map.version)
            {
                throw ThrowVersionMismatch();
            }

            this.index = 0;
            this.key = default;
            this.value = default;
        }

        private static Exception ThrowVersionMismatch()
        {
            throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.'");
        }
    }

    #endregion

    #region ICollection

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection.CopyTo(Array array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (array.Rank != 1)
        {
            throw new ArgumentException(nameof(array));
        }

        if (array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException(nameof(array));
        }

        if (index < 0 || index > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.Count)
        {
            throw new ArgumentException();
        }

        uint nodeIndex = 0;
        KeyValuePair<TKey, TValue>[]? keyValuePairArray = array as KeyValuePair<TKey, TValue>[];
        if (keyValuePairArray != null)
        {
            while (nodeIndex < (uint)this.nodeCount)
            {
                if (this.nodes[nodeIndex].IsValid())
                {
                    keyValuePairArray[index + nodeIndex] = new KeyValuePair<TKey, TValue>(this.nodes[nodeIndex].key, this.nodes[nodeIndex].value);
                }

                nodeIndex++;
            }
        }
        else
        {
            object[]? objects = array as object[];
            if (objects == null)
            {
                throw new ArgumentException(nameof(array));
            }

            try
            {
                while (nodeIndex < (uint)this.nodeCount)
                {
                    if (this.nodes[nodeIndex].IsValid())
                    {
                        objects[index + nodeIndex] = new KeyValuePair<TKey, TValue>(this.nodes[nodeIndex].key, this.nodes[nodeIndex].value);
                    }

                    nodeIndex++;
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(nameof(array));
            }
        }
    }

    #endregion

    #region IDictionary

    object? IDictionary.this[object key]
    {
        get
        {
            if (key == null)
            {
                if (this.TryGetValue(default, out var value))
                {
                    return value!;
                }
            }
            else if (key is TKey k)
            {
                if (this.TryGetValue(k, out var value))
                {
                    return value!;
                }
            }

            return null!;
        }

        set
        {
            this[(TKey)key] = (TValue)value!;
        }
    }

    bool IDictionary.IsFixedSize => false;

    bool IDictionary.IsReadOnly => false;

    ICollection IDictionary.Keys => (ICollection)this.Keys;

    ICollection IDictionary.Values => (ICollection)this.Values;

    void IDictionary.Add(object key, object? value) => this.Add((TKey)key, (TValue)value!);

    bool IDictionary.Contains(object key)
    {
        if (key == null)
        {
            return this.ContainsKey(default);
        }
        else if (key is TKey k)
        {
            return this.ContainsKey(k);
        }

        return false;
    }

    IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DictEntry);

    void IDictionary.Remove(object key)
    {
        if (key == null)
        {
            this.Remove(default);
        }
        else if (key is TKey k)
        {
            this.Remove(k);
        }
    }

    #endregion

    #region IDictionary<TKey, TValue>

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => this.Values;

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => this.Add(key, value);

    #endregion

    #region ICollection<KeyValuePair<TKey,TValue>>

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => this.Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => this.FindNode(item.Key, item.Value) != -1;

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => ((ICollection)this).CopyTo(array, index);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = this.FindNode(item.Key, item.Value);
        if (index == -1)
        {
            return false;
        }

        this.RemoveNode(index);
        return true;
    }

    #endregion

    #region KeyValueCollection

    public KeyCollection Keys => this.keys != null ? this.keys : (this.keys = new KeyCollection(this));

    public ValueCollection Values => this.values != null ? this.values : (this.values = new ValueCollection(this));

    public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
    {
        private readonly UnorderedMap<TKey, TValue> map;

        public KeyCollection(UnorderedMap<TKey, TValue> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            this.map = map;
        }

        public Enumerator GetEnumerator() => new Enumerator(this.map);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this.map);

        public void CopyTo(TKey[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.Count)
            {
                throw new ArgumentException();
            }

            uint nodeIndex = 0;
            while (nodeIndex < (uint)this.map.nodeCount)
            {
                if (this.map.nodes[nodeIndex].IsValid())
                {
                    array[index++] = this.map.nodes[nodeIndex].key;
                }

                nodeIndex++;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.map.Count)
            {
                throw new ArgumentException();
            }

            TKey[]? keys = array as TKey[];
            if (keys != null)
            {
                this.CopyTo(keys, index);
            }
            else
            {
                try
                {
                    object[] objects = (object[])array;
                    uint nodeIndex = 0;
                    while (nodeIndex < (uint)this.map.nodeCount)
                    {
                        if (this.map.nodes[nodeIndex].IsValid())
                        {
                            objects[index++] = this.map.nodes[nodeIndex].key!;
                        }

                        nodeIndex++;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }
        }

        public int Count => this.map.Count;

        bool ICollection<TKey>.IsReadOnly => true;

        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        bool ICollection<TKey>.Contains(TKey item) => this.map.ContainsKey(item);

        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.map).SyncRoot;

        public struct Enumerator : IEnumerator<TKey>, IEnumerator
        {
            private IEnumerator<KeyValuePair<TKey, TValue>> mapEnum;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.mapEnum = map.GetEnumerator();
            }

            public void Dispose() => this.mapEnum.Dispose();

            public bool MoveNext() => this.mapEnum.MoveNext();

            public TKey Current => this.mapEnum.Current.Key;

            object? IEnumerator.Current => this.Current;

            void IEnumerator.Reset() => this.mapEnum.Reset();
        }
    }

    public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
    {
        private readonly UnorderedMap<TKey, TValue> map;

        public ValueCollection(UnorderedMap<TKey, TValue> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            this.map = map;
        }

        public Enumerator GetEnumerator() => new Enumerator(this.map);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this.map);

        public void CopyTo(TValue[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.Count)
            {
                throw new ArgumentException();
            }

            uint nodeIndex = 0;
            while (nodeIndex < (uint)this.map.nodeCount)
            {
                if (this.map.nodes[nodeIndex].IsValid())
                {
                    array[index++] = this.map.nodes[nodeIndex].value;
                }

                nodeIndex++;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.map.Count)
            {
                throw new ArgumentException();
            }

            TValue[]? values = array as TValue[];
            if (values != null)
            {
                this.CopyTo(values, index);
            }
            else
            {
                try
                {
                    object?[] objects = (object?[])array;
                    uint nodeIndex = 0;
                    while (nodeIndex < (uint)this.map.nodeCount)
                    {
                        if (this.map.nodes[nodeIndex].IsValid())
                        {
                            objects[index++] = this.map.nodes[nodeIndex].value;
                        }

                        nodeIndex++;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }
        }

        public int Count => this.map.Count;

        bool ICollection<TValue>.IsReadOnly => true;

        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        bool ICollection<TValue>.Contains(TValue item)
        {
            return this.map.ContainsValue(item);
        }

        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.map).SyncRoot;

        public struct Enumerator : IEnumerator<TValue>, IEnumerator
        {
            private IEnumerator<KeyValuePair<TKey, TValue>> mapEnum;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.mapEnum = map.GetEnumerator();
            }

            public void Dispose() => this.mapEnum.Dispose();

            public bool MoveNext() => this.mapEnum.MoveNext();

            public TValue Current => this.mapEnum.Current.Value;

            object? IEnumerator.Current => this.Current;

            void IEnumerator.Reset() => this.mapEnum.Reset();
        }
    }

    #endregion

    private const int MinLogCapacity = 2;
    private const int MaxLogCapacity = 31;
    private int version;
    private KeyCollection? keys;
    private ValueCollection? values;
    private int hashMask;
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
        else if (log > MaxLogCapacity)
        {
            log = MaxLogCapacity;
        }

        var size = 1 << log;
        this.hashMask = size - 1;
        this.buckets = new int[size];
        for (n = 0; n < size; n++)
        {
            this.buckets[n] = -1;
        }

        this.nodes = new Node[size];

        this.nullList = -1;
        this.freeList = -1;
    }

    /// <summary>
    /// Gets the number of nodes actually contained in the <see cref="UnorderedMap{TKey, TValue}"/>.
    /// </summary>
    public int Count => this.nodeCount - this.freeCount;

    public IEqualityComparer<TKey> Comparer { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the collection allows duplicate keys.
    /// </summary>
    public bool AllowDuplicate { get; protected set; }

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

            return this.nodes[index].value;
        }

        set
        {
            var result = this.Add(key, value);
            if (!result.NewlyAdded)
            {
                this.nodes[result.NodeIndex].value = value;
            }
        }
    }

    public bool ContainsKey(TKey? key) => this.FindFirstNode(key) != -1;

    public bool ContainsValue(TValue value)
    {
        if (value == null)
        {
            for (var i = 0; i < this.nodeCount; i++)
            {
                if (this.nodes[i].IsValid() && this.nodes[i].value == null)
                {
                    return true;
                }
            }
        }
        else
        {
            var c = EqualityComparer<TValue>.Default;
            for (int i = 0; i < this.nodeCount; i++)
            {
                if (this.nodes[i].IsValid() && c.Equals(this.nodes[i].value, value))
                {
                    return true;
                }
            }
        }

        return false;
    }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        if (key == null)
        {
            if (this.nullList != -1)
            {
                value = this.nodes[this.nullList].value;
                return true;
            }
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key!);
            var index = hashCode & this.hashMask;
            var i = this.buckets[index];
            while (i >= 0)
            {
                if (this.nodes[i].hashCode == hashCode && this.Comparer.Equals(this.nodes[i].key, key!))
                {// Identical
                    value = this.nodes[i].value;
                    return true;
                }

                i = this.nodes[i].next;
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
    /// Removes the first element with the specified key/value from a collection.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">The value of the element to remove.</param>
    /// <returns>true if the element is found and successfully removed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey? key, TValue value)
    {
        var p = this.FindNode(key, value);
        if (p == -1)
        {
            return false;
        }

        this.RemoveNode(p);
        return true;
    }

    /// <summary>
    /// Searches for the first <see cref="UnorderedMap{TKey, TValue}.Node"/> index with the specified key.
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
            var hashCode = this.Comparer.GetHashCode(key!);
            var index = hashCode & this.hashMask;
            var i = this.buckets[index];
            while (i >= 0)
            {
                if (this.nodes[i].hashCode == hashCode && this.Comparer.Equals(this.nodes[i].key, key!))
                {// Identical
                    return i;
                }

                i = this.nodes[i].next;
            }

            return -1; // Not found
        }
    }

    /// <summary>
    /// Searches for the first <see cref="UnorderedMap{TKey, TValue}.Node"/> index with the specified key and value.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <param name="value">The value to search in a collection.</param>
    /// <returns>The first node index with the specified key and value. -1: not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindNode(TKey? key, TValue value)
    {
        var c = EqualityComparer<TValue>.Default;
        if (key == null)
        {
            var i = this.nullList;
            while (i >= 0)
            {
                if (c.Equals(this.nodes[i].value, value))
                {// Identical
                    return i;
                }

                i = this.nodes[i].next;
            }
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key);
            var index = hashCode & this.hashMask;
            var i = this.buckets[index];
            while (i >= 0)
            {
                if (this.nodes[i].hashCode == hashCode &&
                    this.Comparer.Equals(this.nodes[i].key, key) &&
                    c.Equals(this.nodes[i].value, value))
                {// Identical
                    return i;
                }

                i = this.nodes[i].next;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines whether the collection contains a specific key and value.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <param name="value">The value to search in a collection.</param>
    /// <returns>true if the key and value is found in the collection.</returns>
    public bool Contains(TKey? key, TValue value) => this.FindNode(key, value) != -1;

    /// <summary>
    /// Enumerates <see cref="UnorderedMap{TKey, TValue}.Node"/> indexes with the specified key.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <returns>The node indexes with the specified key.</returns>
    public IEnumerable<int> EnumerateNode(TKey? key)
    {
        var i = this.FindFirstNode(key);
        if (i < 0)
        {// Not found
            yield break;
        }

        if (key == null)
        {// Null list
            while (i >= 0)
            {
                yield return i;
                i = this.nodes[i].next;
            }
        }
        else
        {
            var hashCode = this.nodes[i].hashCode;
            while (i >= 0)
            {
                if (this.nodes[i].hashCode == hashCode && this.Comparer.Equals(this.nodes[i].key, key!))
                {// Identical
                    yield return i;
                }

                i = this.nodes[i].next;
            }
        }
    }

    /// <summary>
    /// Enumerates <see cref="UnorderedMap{TKey, TValue}.Node"/> values with the specified key.
    /// </summary>
    /// <param name="key">The key to search in a collection.</param>
    /// <returns>The node values with the specified key.</returns>
    public IEnumerable<TValue> EnumerateValue(TKey? key)
    {
        var i = this.FindFirstNode(key);
        if (i < 0)
        {// Not found
            yield break;
        }

        if (key == null)
        {// Null list
            while (i >= 0)
            {
                yield return this.nodes[i].value;
                i = this.nodes[i].next;
            }
        }
        else
        {
            var hashCode = this.nodes[i].hashCode;
            while (i >= 0)
            {
                if (this.nodes[i].hashCode == hashCode && this.Comparer.Equals(this.nodes[i].key, key!))
                {// Identical
                    yield return this.nodes[i].value;
                }

                i = this.nodes[i].next;
            }
        }
    }

    /// <summary>
    /// Adds an element to a collection. If the element is already in the map, this method returns the stored element without creating a new node, and sets NewlyAdded to false.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns>NodeIndex: the added <see cref="UnorderedMap{TKey, TValue}.Node"/>.<br/>
    /// NewlyAdded:true if the new key is inserted.</returns>
    /// <remarks>To optimize Value creation, we considered using a Factory delegate but decided against it due to performance degradation.<br/>
    /// Instead, consider searching with ContainsKey() or FindNode() first, and if the item does not exist, add the Value using Add().</remarks>
    public (int NodeIndex, bool NewlyAdded) Add(TKey key, TValue value) => this.Probe(key, value);

    /// <summary>
    /// Updates the node's key with the specified key. Removes the node and inserts it in the correct position if necessary.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="UnorderedMap{TKey, TValue}.Node"/> to set the key.</param>
    /// <param name="key">The key to set.</param>
    /// <returns>true if the node is successfully updated.</returns>
    public bool SetNodeKey(int nodeIndex, TKey? key)
    {
        if (key == null)
        {
            if (this.nodes[nodeIndex].key == null)
            {// Identical
                return false;
            }
        }
        else
        {
            if (this.Comparer.Equals(this.nodes[nodeIndex].key, key))
            {// Identical
                return false;
            }
        }

        var value = this.nodes[nodeIndex].value;
        this.RemoveNode(nodeIndex);
        var result = this.Probe(key, value); // Reuse nodeIndex from this.freeList
        return result.NewlyAdded;
    }

    /// <summary>
    /// Updates the node's value with the specified value.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="UnorderedMap{TKey, TValue}.Node"/> to set the value.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>true if the node is successfully updated.</returns>
    public bool SetNodeValue(int nodeIndex, TValue value)
    {
        if (this.nodes[nodeIndex].IsInvalid())
        {
            return false;
        }

        if (this.nodes[nodeIndex].key == null)
        {// Null list
            if (nodeIndex >= this.nodeCount)
            {// check node index.
                return false;
            }
        }

        this.nodes[nodeIndex].value = value;
        return true;
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

    public void UnsafeChangeValue(int nodeIndex, TValue value)
    {
        if (this.nodes[nodeIndex].IsInvalid())
        {
            return;
        }

        this.nodes[nodeIndex].value = value;
    }

    protected (TKey? Key, int Count) TryGetMostDuplicateKeyInternal()
    {
        TKey? key = default;
        int count = 0;

        for (var index = 0; index <= this.hashMask; index++)
        {
            var currentIndex = this.buckets[index];
            if (currentIndex >= 0)
            {
                var currentCount = 1;
                var currentKey = this.nodes[currentIndex].key;
                var hashCode = currentKey != null ? this.Comparer.GetHashCode(currentKey) : 0;

                currentIndex = this.nodes[currentIndex].next;
                while (currentIndex >= 0)
                {
                    if (this.nodes[currentIndex].hashCode == hashCode &&
                        this.Comparer.Equals(this.nodes[currentIndex].key, currentKey))
                    {// Identical
                        currentCount++;
                    }
                    else
                    {
                        break;
                    }

                    currentIndex = this.nodes[currentIndex].next;
                }

                if (currentCount > count)
                {
                    count = currentCount;
                    key = currentKey;
                }
            }
        }

        return (key, count);
    }

    /// <summary>
    /// Adds an element to the map. If the element is already in the map, this method returns the stored node without creating a new node.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="key">The element to add to the set.</param>
    /// <returns>NodeIndex: the added <see cref="UnorderedMap{TKey, TValue}.Node"/>.<br/>
    /// NewlyAdded: true if the new key is inserted.</returns>
    private (int NodeIndex, bool NewlyAdded) Probe(TKey? key, TValue value)
    {
        if (this.nodeCount == this.nodes.Length)
        {
            this.Resize();
        }

        int newIndex;
        if (key == null)
        {// Null key
            if (this.AllowDuplicate == false && this.nullList != -1)
            {
                return (this.nullList, false);
            }

            newIndex = this.NewNode();
            this.nodes[newIndex].hashCode = 0;
            this.nodes[newIndex].key = key!;
            this.nodes[newIndex].value = value;

            if (this.nullList == -1)
            {
                this.nodes[newIndex].previous = -1;
                this.nodes[newIndex].next = -1;
                this.nullList = newIndex;
            }
            else
            {
                this.nodes[newIndex].previous = -1;
                this.nodes[newIndex].next = this.nullList;
                this.nodes[this.nullList].previous = newIndex;
                this.nullList = newIndex;
            }

            this.version++;
            return (newIndex, true);
        }
        else
        {
            var hashCode = this.Comparer.GetHashCode(key);
            var index = hashCode & this.hashMask;
            if (!this.AllowDuplicate)
            {
                var i = this.buckets[index];
                while (i >= 0)
                {
                    if (this.nodes[i].hashCode == hashCode && this.Comparer.Equals(this.nodes[i].key, key))
                    {// Identical
                        return (i, false);
                    }

                    i = this.nodes[i].next;
                }
            }

            newIndex = this.NewNode();
            this.nodes[newIndex].hashCode = hashCode;
            this.nodes[newIndex].key = key;
            this.nodes[newIndex].value = value;

            if (this.buckets[index] == -1)
            {
                this.nodes[newIndex].previous = -1;
                this.nodes[newIndex].next = -1;
                this.buckets[index] = newIndex;
            }
            else
            {
                this.nodes[newIndex].previous = -1;
                this.nodes[newIndex].next = this.buckets[index];
                this.nodes[this.buckets[index]].previous = newIndex;
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
            this.freeList = this.nodes[index].next;
            this.freeCount--;
        }
        else
        {
            index = this.nodeCount;
            this.nodeCount++;
        }

        return index;
    }

    private void Resize()
    {
        const int minimumCapacity = 1 << MinLogCapacity;
        var newSize = this.nodes.Length << 1;
        if (newSize < minimumCapacity)
        {
            newSize = minimumCapacity;
        }

        var newMask = newSize - 1;
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
            if (newNode.IsValid())
            {
                if (newNode.key == null)
                {// Null list. No need to modify.
                }
                else
                {
                    var bucket = newNode.hashCode & newMask;
                    if (newBuckets[bucket] == -1)
                    {
                        newNode.previous = -1;
                        newNode.next = -1;
                        newBuckets[bucket] = i;
                    }
                    else
                    {
                        var newBucket = newBuckets[bucket];
                        newNode.previous = -1;
                        newNode.next = newBucket;
                        newBuckets[bucket] = i;
                        newNodes[newBucket].previous = i;
                    }
                }
            }
        }

        // Update
        this.version++;
        this.hashMask = newMask;
        this.buckets = newBuckets;
        this.nodes = newNodes;
    }

    #endregion
}
