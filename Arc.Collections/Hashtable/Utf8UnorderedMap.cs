// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a collection of UTF-8 key/value pairs organized by the hash code of the key.<br/>
/// This is a lightweight implementation optimized for performance with minimal memory overhead.<br/>
/// Stored key arrays must not be modified after insertion.<br/>
/// <br/>NOT thread-safe:<br/>
/// It can be accessed from multiple reader threads if used as immutable.<br/>
/// If there is any writer thread, all access must be protected by mutual exclusion.<br/>
/// Modifying the map while enumerating it is undefined behavior (no version check is performed).
/// </summary>
/// <typeparam name="TValue">The type of values in the map.</typeparam>
public class Utf8UnorderedMap<TValue> : IEnumerable<KeyValuePair<byte[], TValue>>
{
    private const int StartOfFreeList = -3;
    private const int MaximumCapacity = 1 << 30;

    public struct Node
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal int hashCode;

        /// <summary>
        /// The next node index, or an encoded free-list link when the node is unused.
        /// </summary>
        internal int next;
        internal byte[] key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public byte[] Key => this.key;

        public TValue Value => this.value;
    }

    private int[] _buckets;
    private Node[] _nodes;
    private int _count;
    private int _freeList;
    private int _freeCount;

    /// <summary>
    /// Gets the number of elements in the map.
    /// </summary>
    public int Count => this._count - this._freeCount;

    /// <summary>
    /// Gets the total capacity of the map.
    /// </summary>
    public int Capacity => this._nodes.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8UnorderedMap{TValue}"/> class.
    /// </summary>
    public Utf8UnorderedMap(uint minimumSize = 0)
    {
        if (minimumSize > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSize));
        }

        this.Initialize(minimumSize);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[byte[] key]
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
    /// Adds or updates a key/value pair.
    /// </summary>
    public void Add(byte[] key, TValue value)
        => this.TryInsert(key, value, true);

    /// <summary>
    /// Adds or updates a key/value pair.
    /// </summary>
    public void Add(ReadOnlySpan<byte> key, TValue value)
        => this.TryInsert(key, value, true);

    /// <summary>
    /// Attempts to add a key/value pair without overwriting an existing value.
    /// </summary>
    public bool TryAdd(byte[] key, TValue value)
        => this.TryInsert(key, value, false);

    /// <summary>
    /// Attempts to add a key/value pair without overwriting an existing value.
    /// </summary>
    public bool TryAdd(ReadOnlySpan<byte> key, TValue value)
        => this.TryInsert(key, value, false);

    /// <summary>
    /// Removes all keys and values from the map.
    /// </summary>
    public void Clear()
    {
        var count = this._count;
        if (count == 0)
        {
            return;
        }

        Array.Clear(this._buckets);
        Array.Clear(this._nodes, 0, count);
        this._count = 0;
        this._freeList = -1;
        this._freeCount = 0;
    }

    /// <summary>
    /// Determines whether the map contains the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.TryGetValue(key, out _);
    }

    /// <summary>
    /// Determines whether the map contains the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ReadOnlySpan<byte> key)
        => this.TryGetValue(key, out _);

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
                if (nodes[i].next >= -1 && nodes[i].value is null)
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
    /// Removes the element with the specified key.
    /// </summary>
    public bool Remove(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.Remove(key.AsSpan());
    }

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    public bool Remove(ReadOnlySpan<byte> key)
    {
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var nodes = this._nodes;
        var previous = -1;
        var i = bucket - 1;
        uint collisionCount = 0;

        // The (uint) comparison also lets the JIT elide the bounds check on nodes[i].
        while ((uint)i < (uint)nodes.Length)
        {
            ref var node = ref nodes[i];
            if (node.hashCode == hashCode &&
                key.SequenceEqual(node.key))
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

                // Always clear the array reference when releasing a node.
                node.key = null!;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
                {
                    node.value = default!;
                }

                this._freeList = i;
                this._freeCount++;
                return true;
            }

            previous = i;
            i = node.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(byte[] key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        var hashCode = GetHashCode(key);
        var i = this.GetBucket(hashCode) - 1;
        var nodes = this._nodes;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var node = ref nodes[i];
            if (node.hashCode == hashCode &&
                key.AsSpan().SequenceEqual(node.key))
            {
                value = node.value;
                return true;
            }

            i = node.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<byte> key, [MaybeNullWhen(false)] out TValue value)
    {
        var hashCode = GetHashCode(key);
        var i = this.GetBucket(hashCode) - 1;
        var nodes = this._nodes;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var node = ref nodes[i];
            if (node.hashCode == hashCode &&
                key.SequenceEqual(node.key))
            {
                value = node.value;
                return true;
            }

            i = node.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets a reference to the value associated with the specified key, adding a new entry
    /// with a default value when the key does not exist.<br/>
    /// This performs the hash computation and chain walk only once.
    /// </summary>
    /// <returns>
    /// A reference to the value slot. The reference is invalidated by any subsequent
    /// addition to or removal from the map; do not hold it across mutations.
    /// </returns>
    public ref TValue GetValueRefOrAddDefault(byte[] key, out bool exists)
    {
        ArgumentNullException.ThrowIfNull(key);

        var nodes = this._nodes;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode &&
                key.AsSpan().SequenceEqual(existing.key))
            {
                exists = true;
                return ref existing.value;
            }

            i = existing.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        var index = this.GetNewNodeIndex();

        // Resize may have replaced both arrays; re-fetch them.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;
        node.key = key;
        node.value = default!;
        bucket = index + 1;
        exists = false;

        return ref node.value;
    }

    /// <summary>
    /// Gets a reference to the value associated with the specified key, adding a new entry
    /// with a default value when the key does not exist.
    /// </summary>
    public ref TValue GetValueRefOrAddDefault(ReadOnlySpan<byte> key, out bool exists)
    {
        var nodes = this._nodes;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode &&
                key.SequenceEqual(existing.key))
            {
                exists = true;
                return ref existing.value;
            }

            i = existing.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        var index = this.GetNewNodeIndex();

        // Resize may have replaced both arrays; re-fetch them.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;

        // Allocate only after confirming that the key does not already exist.
        node.key = key.ToArray();
        node.value = default!;
        bucket = index + 1;
        exists = false;

        return ref node.value;
    }

    [MemberNotNull(nameof(_buckets), nameof(_nodes))]
    private void Initialize(uint minimumSize)
    {
        var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(minimumSize);
        var size = checked((int)capacity);

        this._buckets = new int[size];
        this._nodes = new Node[size];
        this._freeList = -1;
    }

    private bool TryInsert(byte[] key, TValue value, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(key);

        var nodes = this._nodes;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode &&
                key.AsSpan().SequenceEqual(existing.key))
            {
                if (overwrite)
                {
                    existing.value = value;
                    return true;
                }

                return false;
            }

            i = existing.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        var index = this.GetNewNodeIndex();

        // Resize may have replaced both arrays; re-fetch them.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;
        node.key = key;
        node.value = value;
        bucket = index + 1;

        return true;
    }

    private bool TryInsert(ReadOnlySpan<byte> key, TValue value, bool overwrite)
    {
        var nodes = this._nodes;
        var hashCode = GetHashCode(key);
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode &&
                key.SequenceEqual(existing.key))
            {
                if (overwrite)
                {
                    existing.value = value;
                    return true;
                }

                return false;
            }

            i = existing.next;
            if (++collisionCount > (uint)nodes.Length)
            {
                ThrowConcurrentOperationsNotSupported();
            }
        }

        var index = this.GetNewNodeIndex();

        // Resize may have replaced both arrays; re-fetch them.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;

        // Allocate only after confirming that the key does not already exist.
        node.key = key.ToArray();
        node.value = value;
        bucket = index + 1;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewNodeIndex()
    {
        if (this._freeCount > 0)
        {
            var index = this._freeList;
            this._freeList = StartOfFreeList - this._nodes[index].next;
            this._freeCount--;
            return index;
        }

        var count = this._count;
        if (count == this._nodes.Length)
        {
            this.Resize();
        }

        this._count = count + 1;
        return count;
    }

    private void Resize()
    {
        var oldSize = this._nodes.Length;
        if (oldSize >= MaximumCapacity)
        {
            throw new InvalidOperationException(
                "The maximum capacity of the map has been reached.");
        }

        var newSize = oldSize << 1;
        if (newSize <= 0 || newSize > MaximumCapacity)
        {
            newSize = MaximumCapacity;
        }

        var nodes = new Node[newSize];
        Array.Copy(this._nodes, nodes, this._count);

        var buckets = new int[newSize];
        var mask = newSize - 1;
        for (var i = 0; i < this._count; i++)
        {
            ref var node = ref nodes[i];
            if (node.next < -1)
            {
                continue;
            }

            ref var bucket = ref buckets[node.hashCode & mask];
            node.next = bucket - 1;
            bucket = i + 1;
        }

        this._nodes = nodes;
        this._buckets = buckets;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucket(int hashCode)
    {
        var buckets = this._buckets;
        return ref buckets[hashCode & (buckets.Length - 1)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCode(ReadOnlySpan<byte> key)
        => unchecked((int)XxHash3Slim.Hash64(key));

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowConcurrentOperationsNotSupported()
        => throw new InvalidOperationException(
            "Concurrent operations are not supported.");

    #region IEnumerable

    /// <summary>
    /// Returns an enumerator for the map.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<byte[], TValue>> IEnumerable<KeyValuePair<byte[], TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<byte[], TValue>>
    {
        // The node array and count are snapshotted. Mutating the map during
        // enumeration is undefined behavior.
        private readonly Node[] _nodes;
        private readonly int _count;
        private int _index;
        private KeyValuePair<byte[], TValue> _current;

        internal Enumerator(Utf8UnorderedMap<TValue> map)
        {
            this._nodes = map._nodes;
            this._count = map._count;
            this._index = 0;
            this._current = default;
        }

        public KeyValuePair<byte[], TValue> Current => this._current;

        object IEnumerator.Current
        {
            get
            {
                if (this._index == 0 || this._index == this._count + 1)
                {
                    throw new InvalidOperationException();
                }

                return this._current;
            }
        }

        public bool MoveNext()
        {
            var nodes = this._nodes;
            var count = this._count;
            var index = this._index;

            while ((uint)index < (uint)count)
            {
                ref var node = ref nodes[index++];

                if (node.next >= -1)
                {
                    this._current = new(node.key, node.value);
                    this._index = index;
                    return true;
                }
            }

            this._index = count + 1;
            this._current = default;
            return false;
        }

        public void Dispose()
        {
        }

        void IEnumerator.Reset()
        {
            this._index = 0;
            this._current = default;
        }
    }

    #endregion
}
