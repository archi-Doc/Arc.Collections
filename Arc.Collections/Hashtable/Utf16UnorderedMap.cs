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
/// Represents a collection of UTF-16 key/value pairs organized by the hash code of the key.<br/>
/// This is a lightweight implementation optimized for performance with minimal memory overhead.<br/>
/// <br/>NOT thread-safe:<br/>
/// It can be accessed from multiple reader threads if used as immutable.<br/>
/// If there is any writer thread, all access must be protected by mutual exclusion.<br/>
/// Modifying the map while enumerating it is undefined behavior (no version check is performed).
/// </summary>
/// <typeparam name="TValue">The type of values in the map.</typeparam>
public class Utf16UnorderedMap<TValue> : IEnumerable<KeyValuePair<string, TValue>>
{
    private const int StartOfFreeList = -3;
    private const int MaximumCapacity = 1 << 30;

    public struct Node
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal uint hashCode;

        /// <summary>
        /// The next node index, or an encoded free-list link when the node is unused.
        /// </summary>
        internal int next;
        internal string key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public string Key => this.key;

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
    /// Initializes a new instance of the <see cref="Utf16UnorderedMap{TValue}"/> class.
    /// </summary>
    public Utf16UnorderedMap(uint minimumSize = 0)
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
    public TValue this[string key]
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
    public void Add(string key, TValue value)
        => this.TryInsert(key, value, true);

    /// <summary>
    /// Adds or updates a key/value pair.
    /// </summary>
    public void Add(ReadOnlySpan<char> key, TValue value)
        => this.TryInsert(key, value, true);

    /// <summary>
    /// Attempts to add a key/value pair without overwriting an existing value.
    /// </summary>
    public bool TryAdd(string key, TValue value)
        => this.TryInsert(key, value, false);

    /// <summary>
    /// Attempts to add a key/value pair without overwriting an existing value.
    /// </summary>
    public bool TryAdd(ReadOnlySpan<char> key, TValue value)
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
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.TryGetValue(key, out _);
    }

    /// <summary>
    /// Determines whether the map contains the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ReadOnlySpan<char> key)
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
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.Remove(key.AsSpan());
    }

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    public bool Remove(ReadOnlySpan<char> key)
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
                key.SequenceEqual(node.key.AsSpan()))
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

                // Always clear the string reference when releasing a node.
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
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        var hashCode = GetHashCode(key.AsSpan());
        var i = this.GetBucket(hashCode) - 1;
        var nodes = this._nodes;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var node = ref nodes[i];
            if (node.hashCode == hashCode && key == node.key)
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
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
    {
        var hashCode = GetHashCode(key);
        var i = this.GetBucket(hashCode) - 1;
        var nodes = this._nodes;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var node = ref nodes[i];
            if (node.hashCode == hashCode &&
                key.SequenceEqual(node.key.AsSpan()))
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
    /// This performs the hash computation and chain walk only once, which makes
    /// read-modify-write patterns (counters, accumulators) roughly twice as fast as
    /// a TryGetValue/Add pair.
    /// </summary>
    /// <param name="key">The key to look up or add.</param>
    /// <param name="exists"><see langword="true"/> if the key already existed.</param>
    /// <returns>
    /// A reference to the value slot. The reference is invalidated by any subsequent
    /// addition to or removal from the map; do not hold it across mutations.
    /// </returns>
    public ref TValue GetValueRefOrAddDefault(string key, out bool exists)
    {
        ArgumentNullException.ThrowIfNull(key);

        var nodes = this._nodes;
        var hashCode = GetHashCode(key.AsSpan());
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode && key == existing.key)
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

        // Resize may have replaced both arrays: re-fetch them. Note that ref-reassigning
        // a ref parameter inside the callee would NOT update this method's ref local,
        // so the re-fetch must happen here, in the caller.
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
    /// See <see cref="GetValueRefOrAddDefault(string, out bool)"/> for details and the
    /// reference invalidation rules.
    /// </summary>
    public ref TValue GetValueRefOrAddDefault(ReadOnlySpan<char> key, out bool exists)
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
                key.SequenceEqual(existing.key.AsSpan()))
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

        // Resize may have replaced both arrays: re-fetch them. Note that ref-reassigning
        // a ref parameter inside the callee would NOT update this method's ref local,
        // so the re-fetch must happen here, in the caller.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;

        // Allocate only after confirming that the key does not already exist.
        node.key = key.ToString();
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

    private bool TryInsert(string key, TValue value, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(key);

        var nodes = this._nodes;
        var hashCode = GetHashCode(key.AsSpan());
        ref var bucket = ref this.GetBucket(hashCode);
        var i = bucket - 1;
        uint collisionCount = 0;

        while ((uint)i < (uint)nodes.Length)
        {
            ref var existing = ref nodes[i];
            if (existing.hashCode == hashCode && key == existing.key)
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

        // Resize may have replaced both arrays: re-fetch them. Note that ref-reassigning
        // a ref parameter inside the callee would NOT update this method's ref local,
        // so the re-fetch must happen here, in the caller.
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

    private bool TryInsert(ReadOnlySpan<char> key, TValue value, bool overwrite)
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
                key.SequenceEqual(existing.key.AsSpan()))
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

        // Resize may have replaced both arrays: re-fetch them. Note that ref-reassigning
        // a ref parameter inside the callee would NOT update this method's ref local,
        // so the re-fetch must happen here, in the caller.
        nodes = this._nodes;
        bucket = ref this.GetBucket(hashCode);
        ref var node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;

        // Allocate only after confirming that the key does not already exist.
        node.key = key.ToString();
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
    private ref int GetBucket(uint hashCode)
    {
        var buckets = this._buckets;
        return ref buckets[hashCode & (buckets.Length - 1)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetHashCode(ReadOnlySpan<char> key)
        => unchecked((uint)XxHash3Slim.Hash64(key));

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

    IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
    {
        // The node array and count are snapshotted; the map performs no version checks,
        // and mutating it during enumeration is undefined anyway, so this only removes
        // two dependent field loads per MoveNext.
        private readonly Node[] _nodes;
        private readonly int _count;
        private int _index;
        private KeyValuePair<string, TValue> _current;

        internal Enumerator(Utf16UnorderedMap<TValue> map)
        {
            this._nodes = map._nodes;
            this._count = map._count;
            this._index = 0;
            this._current = default;
        }

        public KeyValuePair<string, TValue> Current => this._current;

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
