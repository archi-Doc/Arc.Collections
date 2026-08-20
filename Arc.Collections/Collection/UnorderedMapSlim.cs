// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a lightweight high-performance hash map.<br/>
/// Keys must be non-null.<br/>
/// <br/>NOT thread-safe:<br/>
/// Multiple readers are allowed only while the map is immutable.<br/>
/// If any writer exists, all access must be protected by mutual exclusion.
/// </summary>
/// <typeparam name="TKey">The type of keys in the map. Keys must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values in the map.</typeparam>
public class UnorderedMapSlim<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private const int StartOfFreeList = -3;
    private const int MaximumCapacity = 1 << 30;

    /// <summary>
    /// Represents a node in the map.
    /// </summary>
    public struct Node
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal uint hashCode;

        // >= -1: active chain
        // -2: end of free list
        // <= -3: encoded free-list index
        internal int next;

        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public readonly TKey Key => this.key;

        public readonly TValue Value => this.value;

        public readonly bool IsValid() => this.next >= -1;
    }

    private int[] _buckets;
    private Node[] _nodes;
    private int _count;
    private int _freeList;
    private int _freeCount;

    /// <summary>
    /// Initializes an empty map with the specified minimum capacity.
    /// </summary>
    public UnorderedMapSlim(uint minimumSize = 0)
    {
        if (minimumSize > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSize));
        }

        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);

        this._buckets = new int[capacity];
        this._nodes = new Node[capacity];
        this._freeList = -1;
    }

    /// <summary>
    /// Gets the number of elements in the map.
    /// </summary>
    public int Count => this._count - this._freeCount;

    /// <summary>
    /// Gets the current capacity.
    /// </summary>
    public int Capacity => this._nodes.Length;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[TKey key]
    {
        get
        {
            var index = this.FindIndex(key);

            if (index >= 0)
            {
                return this._nodes[index].value;
            }

            ThrowKeyNotFound();
            return default!;
        }

        set => this.TryInsert(key, value, true);
    }

    /// <summary>
    /// Adds or updates an element with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
        => this.TryInsert(key, value, true);

    /// <summary>
    /// Attempts to add an element without overwriting an existing value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue value)
        => this.TryInsert(key, value, false);

    /// <summary>
    /// Determines whether the map contains the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
        => this.FindIndex(key) >= 0;

    /// <summary>
    /// Determines whether the map contains the specified value.
    /// </summary>
    public bool ContainsValue(TValue value)
    {
        var nodes = this._nodes;
        var count = this._count;

        if (value is null)
        {
            for (var i = 0; i < count; i++)
            {
                if (nodes[i].next >= -1 &&
                    nodes[i].value is null)
                {
                    return true;
                }
            }

            return false;
        }

        var comparer = EqualityComparer<TValue>.Default;

        for (var i = 0; i < count; i++)
        {
            if (nodes[i].next >= -1 &&
                comparer.Equals(nodes[i].value, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var index = this.FindIndex(key);

        if (index >= 0)
        {
            value = this._nodes[index].value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    public bool Remove(TKey key)
    {
        if (key is null)
        {
            ThrowKeyNull();
        }

        var comparer = EqualityComparer<TKey>.Default;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var nodes = this._nodes;
        var previous = -1;
        var index = bucket - 1;

        while ((uint)index < (uint)nodes.Length)
        {
            ref var node = ref nodes[index];

            if (node.hashCode == hashCode &&
                comparer.Equals(node.key, key))
            {
                if (previous < 0)
                {
                    bucket = node.next + 1;
                }
                else
                {
                    nodes[previous].next = node.next;
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

                this._freeList = index;
                this._freeCount++;

                return true;
            }

            previous = index;
            index = node.next;
        }

        return false;
    }

    /// <summary>
    /// Removes all elements from the map.
    /// </summary>
    public void Clear()
    {
        var count = this._count;

        if (count == 0)
        {
            return;
        }

        Array.Clear(this._buckets);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<Node>())
        {
            Array.Clear(this._nodes, 0, count);
        }

        this._count = 0;
        this._freeList = -1;
        this._freeCount = 0;
    }

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>>
        IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements without allocation when used directly.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly UnorderedMapSlim<TKey, TValue> map;
        private int index;

        internal Enumerator(UnorderedMapSlim<TKey, TValue> map)
        {
            this.map = map;
            this.index = 0;
        }

        public readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var node = ref this.map._nodes[this.index - 1];
                return new(node.key, node.value);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 ||
                    this.index == this.map._count + 1)
                {
                    ThrowInvalidEnumeratorState();
                }

                return this.Current;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var nodes = this.map._nodes;
            var count = this.map._count;
            var i = this.index;

            while ((uint)i < (uint)count)
            {
                i++;

                if (nodes[i - 1].next >= -1)
                {
                    this.index = i;
                    return true;
                }
            }

            this.index = count + 1;
            return false;
        }

        public void Dispose()
        {
        }

        void IEnumerator.Reset()
        {
            this.index = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(TKey key)
    {
        if (key is null)
        {
            ThrowKeyNull();
        }

        var hashCode = GetHashCode(key);
        var nodes = this._nodes;
        var comparer = EqualityComparer<TKey>.Default;
        var index = this.GetBucket(hashCode) - 1;

        while ((uint)index < (uint)nodes.Length)
        {
            ref var node = ref nodes[index];

            if (node.hashCode == hashCode &&
                comparer.Equals(node.key, key))
            {
                return index;
            }

            index = node.next;
        }

        return -1;
    }

    private bool TryInsert(TKey key, TValue value, bool overwrite)
    {
        if (key is null)
        {
            ThrowKeyNull();
        }

        var nodes = this._nodes;
        var comparer = EqualityComparer<TKey>.Default;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var index = bucket - 1;

        while ((uint)index < (uint)nodes.Length)
        {
            ref var existing = ref nodes[index];

            if (existing.hashCode == hashCode &&
                comparer.Equals(existing.key, key))
            {
                if (overwrite)
                {
                    existing.value = value;
                    return true;
                }

                return false;
            }

            index = existing.next;
        }

        int newIndex;

        if (this._freeCount > 0)
        {
            newIndex = this._freeList;
            this._freeList =
                StartOfFreeList - nodes[newIndex].next;
            this._freeCount--;
        }
        else
        {
            var count = this._count;

            if (count == nodes.Length)
            {
                this.Resize();

                nodes = this._nodes;
                bucket = ref this.GetBucket(hashCode);
            }

            newIndex = count;
            this._count = count + 1;
        }

        ref var node = ref nodes[newIndex];

        node.hashCode = hashCode;
        node.next = bucket - 1;
        node.key = key;
        node.value = value;

        bucket = newIndex + 1;

        return true;
    }

    private void Resize()
    {
        var oldSize = this._nodes.Length;

        if (oldSize >= MaximumCapacity)
        {
            ThrowMaximumCapacity();
        }

        var newSize = oldSize << 1;
        var newMask = newSize - 1;
        var count = this._count;

        var newNodes = new Node[newSize];
        Array.Copy(this._nodes, newNodes, count);

        var newBuckets = new int[newSize];

        // Resize is called only after all free nodes have been reused.
        for (var i = 0; i < count; i++)
        {
            ref var node = ref newNodes[i];
            var bucketIndex = (int)(node.hashCode & (uint)newMask);

            node.next = newBuckets[bucketIndex] - 1;
            newBuckets[bucketIndex] = i + 1;
        }

        // Publish only after the new table is completely built.
        this._nodes = newNodes;
        this._buckets = newBuckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucket(uint hashCode)
    {
        var buckets = this._buckets;
        var index = (int)(hashCode & (uint)(buckets.Length - 1));

        return ref buckets[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetHashCode(TKey key)
        => unchecked((uint)key.GetHashCode());

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNull()
        => throw new ArgumentNullException("key");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound()
        => throw new KeyNotFoundException();

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidEnumeratorState()
        => throw new InvalidOperationException(
            "Enumeration has either not started or has already finished.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMaximumCapacity()
        => throw new InvalidOperationException(
            "The maximum capacity of the map has been reached.");
}
