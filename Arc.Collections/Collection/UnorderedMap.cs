// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a hash map that supports null keys and optional duplicate keys.<br/>
/// Node indexes remain stable while nodes are active, including across resizing.<br/>
/// Removed node indexes may be reused.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private const int MinimumCapacity = 4;
    private const int MaximumCapacity = 1 << 30;

    /// <summary>
    /// Represents a node in the map.
    /// </summary>
    public struct Node
    {
        /// <summary>
        /// The <c>previous</c> value that marks a node as removed and available for reuse.
        /// </summary>
        public const int UnusedNode = -2;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal int hashCode;
        internal int previous; // UnusedNode if unused.
        internal int next;
        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        /// <summary>
        /// Determines whether the node currently holds an element.
        /// </summary>
        /// <returns><see langword="true"/> if the node is in use; otherwise, <see langword="false"/>.</returns>
        public readonly bool IsValid() => this.previous != UnusedNode;

        /// <summary>
        /// Determines whether the node has been removed.
        /// </summary>
        /// <returns><see langword="true"/> if the node is unused; otherwise, <see langword="false"/>.</returns>
        public readonly bool IsInvalid() => this.previous == UnusedNode;

        /// <summary>
        /// Gets the key stored in the node.
        /// </summary>
        public readonly TKey? Key => this.key;

        /// <summary>
        /// Gets the value stored in the node.
        /// </summary>
        public readonly TValue Value => this.value;
    }

    // Null when the default comparer is used.
    private readonly IEqualityComparer<TKey>? comparer;

    private int version;
    private int hashMask;
    private int[] buckets = default!;
    private Node[] nodes = default!;
    private int nodeCount;
    private int freeList;
    private int freeCount;
    private int nullList;

    /// <summary>
    /// Initializes an empty map.
    /// </summary>
    public UnorderedMap()
        : this(0, null, false)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    public UnorderedMap(int capacity)
        : this(capacity, null, false)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    public UnorderedMap(IEqualityComparer<TKey>? comparer)
        : this(0, comparer, false)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified duplicate-key behavior.
    /// </summary>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedMap(bool allowDuplicate)
        : this(0, null, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity and comparer.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    public UnorderedMap(int capacity, IEqualityComparer<TKey>? comparer)
        : this(capacity, comparer, false)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity and duplicate-key behavior.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedMap(int capacity, bool allowDuplicate)
        : this(capacity, null, allowDuplicate)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity, comparer, and duplicate-key behavior.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="allowDuplicate"><see langword="true"/> to allow duplicate keys.</param>
    public UnorderedMap(int capacity, IEqualityComparer<TKey>? comparer, bool allowDuplicate)
    {
        this.Initialize(capacity);
        this.AllowDuplicate = allowDuplicate;

        var defaultComparer = EqualityComparer<TKey>.Default;
        this.comparer = comparer is null || ReferenceEquals(comparer, defaultComparer)
            ? null
            : comparer;
    }

    /// <summary>
    /// Gets the number of elements in the map.
    /// </summary>
    public int Count => this.nodeCount - this.freeCount;

    /// <summary>
    /// Gets the current node capacity.
    /// </summary>
    public int Capacity => this.nodes.Length;

    /// <summary>
    /// Gets the comparer used for keys.
    /// </summary>
    public IEqualityComparer<TKey> Comparer
        => this.comparer ?? EqualityComparer<TKey>.Default;

    /// <summary>
    /// Gets a value indicating whether duplicate keys are allowed.
    /// </summary>
    public bool AllowDuplicate { get; }

    /// <summary>
    /// Gets an allocation-free enumerable over the keys.
    /// </summary>
    public KeyEnumerable Keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Gets an allocation-free enumerable over the values.
    /// </summary>
    public ValueEnumerable Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[TKey? key]
    {
        get
        {
            var index = this.FindFirstNode(key);
            if (index >= 0)
            {
                return this.nodes[index].value;
            }

            ThrowKeyNotFound();
            return default!;
        }

        set => this.SetValue(key, value);
    }

    /// <summary>
    /// Gets direct access to the internal node array.<br/>
    /// Only nodes with <see cref="Node.IsValid"/> are active, and the array may be
    /// replaced when the map is resized; do not hold it across mutations.
    /// </summary>
    /// <returns>The internal node array and the number of node slots in use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Node[] Nodes, int Max) UnsafeGetNodes()
        => (this.nodes, this.nodeCount);

    /// <summary>
    /// Adds an element, or returns the existing node when duplicate keys are disabled.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The node index, and a flag that is <see langword="true"/> when the element was newly added.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int NodeIndex, bool NewlyAdded) Add(TKey? key, TValue value)
        => this.Probe(key, value);

    /// <summary>
    /// Determines whether the specified key exists.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey? key)
        => this.FindFirstNode(key) >= 0;

    /// <summary>
    /// Determines whether the specified key and value exist.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey? key, TValue value)
        => this.FindNode(key, value) >= 0;

    /// <summary>
    /// Determines whether the specified value exists.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise, <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value)
    {
        var nodes = this.nodes;
        var count = this.nodeCount;

        if (value is null)
        {
            for (var i = 0; i < count; i++)
            {
                ref var node = ref nodes[i];

                if (node.previous != Node.UnusedNode &&
                    node.value is null)
                {
                    return true;
                }
            }

            return false;
        }

        var comparer = EqualityComparer<TValue>.Default;

        for (var i = 0; i < count; i++)
        {
            ref var node = ref nodes[i];

            if (node.previous != Node.UnusedNode &&
                comparer.Equals(node.value, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
    {
        if (key is null)
        {
            var index = this.nullList;

            if (index >= 0)
            {
                value = this.nodes[index].value;
                return true;
            }

            value = default;
            return false;
        }

        var nodes = this.nodes;
        var comparer = this.comparer;

        if (comparer is null)
        {
            var hashCode = key.GetHashCode();
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    EqualityComparer<TKey>.Default.Equals(node.key, key))
                {
                    value = node.value;
                    return true;
                }

                i = node.next;
            }
        }
        else
        {
            var hashCode = comparer.GetHashCode(key);
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    comparer.Equals(node.key, key))
                {
                    value = node.value;
                    return true;
                }

                i = node.next;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Finds the first node with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The node index, or -1 if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindFirstNode(TKey? key)
    {
        if (key is null)
        {
            return this.nullList;
        }

        var nodes = this.nodes;
        var comparer = this.comparer;

        if (comparer is null)
        {
            var hashCode = key.GetHashCode();
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    EqualityComparer<TKey>.Default.Equals(node.key, key))
                {
                    return i;
                }

                i = node.next;
            }
        }
        else
        {
            var hashCode = comparer.GetHashCode(key);
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    comparer.Equals(node.key, key))
                {
                    return i;
                }

                i = node.next;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the first node with the specified key and value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The node index, or -1 if not found.</returns>
    public int FindNode(TKey? key, TValue value)
    {
        var nodes = this.nodes;
        var valueComparer = EqualityComparer<TValue>.Default;

        if (key is null)
        {
            var i = this.nullList;

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (valueComparer.Equals(node.value, value))
                {
                    return i;
                }

                i = node.next;
            }

            return -1;
        }

        var comparer = this.comparer;

        if (comparer is null)
        {
            var hashCode = key.GetHashCode();
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    EqualityComparer<TKey>.Default.Equals(node.key, key) &&
                    valueComparer.Equals(node.value, value))
                {
                    return i;
                }

                i = node.next;
            }
        }
        else
        {
            var hashCode = comparer.GetHashCode(key);
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    comparer.Equals(node.key, key) &&
                    valueComparer.Equals(node.value, value))
                {
                    return i;
                }

                i = node.next;
            }
        }

        return -1;
    }

    /// <summary>
    /// Removes the first element with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey? key)
    {
        var index = this.FindFirstNode(key);

        if (index < 0)
        {
            return false;
        }

        this.RemoveNode(index);
        return true;
    }

    /// <summary>
    /// Removes the first element with the specified key and value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey? key, TValue value)
    {
        var index = this.FindNode(key, value);

        if (index < 0)
        {
            return false;
        }

        this.RemoveNode(index);
        return true;
    }

    /// <summary>
    /// Removes the specified node in O(1) time.
    /// Does nothing when the index is out of range or the node is already unused.
    /// </summary>
    /// <param name="nodeIndex">The node index.</param>
    public void RemoveNode(int nodeIndex)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount)
        {
            return;
        }

        var nodes = this.nodes;
        ref var node = ref nodes[nodeIndex];

        if (node.previous == Node.UnusedNode)
        {
            return;
        }

        var previous = node.previous;
        var next = node.next;

        if (node.key is null)
        {
            if (previous < 0)
            {
                this.nullList = next;
            }
            else
            {
                nodes[previous].next = next;
            }
        }
        else
        {
            var bucketIndex = node.hashCode & this.hashMask;

            if (previous < 0)
            {
                this.buckets[bucketIndex] = next;
            }
            else
            {
                nodes[previous].next = next;
            }
        }

        if (next >= 0)
        {
            nodes[next].previous = previous;
        }

        node.previous = Node.UnusedNode;
        node.next = this.freeList;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            node.key = default!;
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            node.value = default!;
        }

        this.freeList = nodeIndex;
        this.freeCount++;
        this.version++;
    }

    /// <summary>
    /// Updates the key of the specified node while preserving its node index.
    /// </summary>
    /// <param name="nodeIndex">The node index.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key was changed; otherwise, <see langword="false"/>.</returns>
    public bool SetNodeKey(int nodeIndex, TKey? key)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount)
        {
            return false;
        }

        var nodes = this.nodes;
        ref var node = ref nodes[nodeIndex];

        if (node.previous == Node.UnusedNode)
        {
            return false;
        }

        if (key is null)
        {
            if (node.key is null)
            {
                return false;
            }
        }
        else if (node.key is not null &&
                 this.KeysEqual(node.key, key))
        {
            return false;
        }

        if (!this.AllowDuplicate)
        {
            var existing = this.FindFirstNode(key);

            if (existing >= 0 && existing != nodeIndex)
            {
                return false;
            }
        }

        var previous = node.previous;
        var next = node.next;

        if (node.key is null)
        {
            if (previous < 0)
            {
                this.nullList = next;
            }
            else
            {
                nodes[previous].next = next;
            }
        }
        else
        {
            var bucketIndex = node.hashCode & this.hashMask;

            if (previous < 0)
            {
                this.buckets[bucketIndex] = next;
            }
            else
            {
                nodes[previous].next = next;
            }
        }

        if (next >= 0)
        {
            nodes[next].previous = previous;
        }

        if (key is null)
        {
            node.hashCode = 0;
            node.key = default!;
            node.previous = -1;
            node.next = this.nullList;

            if (this.nullList >= 0)
            {
                nodes[this.nullList].previous = nodeIndex;
            }

            this.nullList = nodeIndex;
        }
        else
        {
            var hashCode = this.GetKeyHashCode(key);
            var bucketIndex = hashCode & this.hashMask;
            var head = this.buckets[bucketIndex];

            node.hashCode = hashCode;
            node.key = key;
            node.previous = -1;
            node.next = head;

            if (head >= 0)
            {
                nodes[head].previous = nodeIndex;
            }

            this.buckets[bucketIndex] = nodeIndex;
        }

        this.version++;
        return true;
    }

    /// <summary>
    /// Updates the value of the specified node.
    /// </summary>
    /// <param name="nodeIndex">The node index.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the value was changed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetNodeValue(int nodeIndex, TValue value)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount ||
            this.nodes[nodeIndex].previous == Node.UnusedNode)
        {
            return false;
        }

        this.nodes[nodeIndex].value = value;
        this.version++;
        return true;
    }

    /// <summary>
    /// Changes a node value without validation or version tracking.
    /// </summary>
    /// <param name="nodeIndex">The node index.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeChangeValue(int nodeIndex, TValue value)
        => this.nodes[nodeIndex].value = value;

    /// <summary>
    /// Removes all elements from the map.
    /// </summary>
    public void Clear()
    {
        var count = this.nodeCount;

        if (count == 0)
        {
            return;
        }

        Array.Fill(this.buckets, -1);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<Node>())
        {
            Array.Clear(this.nodes, 0, count);
        }

        this.nodeCount = 0;
        this.freeList = -1;
        this.freeCount = 0;
        this.nullList = -1;
        this.version++;
    }

    /// <summary>
    /// Enumerates node indexes matching the specified key without allocation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>An allocation-free enumerable over the matching nodes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NodeEnumerable EnumerateNode(TKey? key)
        => new(this, key);

    /// <summary>
    /// Enumerates values matching the specified key without allocation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>An allocation-free enumerable over the matching values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MatchedValueEnumerable EnumerateValue(TKey? key)
        => new(this, key);

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>>
        IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    /// <summary>
    /// Enumerates key/value pairs without allocation when used directly.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly UnorderedMap<TKey, TValue> map;
        private readonly Node[] nodes;
        private readonly int version;
        private readonly int count;
        private int index;

        internal Enumerator(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
            this.nodes = map.nodes;
            this.version = map.version;
            this.count = map.nodeCount;
            this.index = 0;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var node = ref this.nodes[this.index - 1];
                return new(node.key, node.value);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                if (this.index == 0 || this.index == this.count + 1)
                {
                    ThrowInvalidEnumeratorState();
                }

                return this.Current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            var nodes = this.nodes;
            var count = this.count;
            var i = this.index;

            while ((uint)i < (uint)count)
            {
                i++;

                if (nodes[i - 1].previous != Node.UnusedNode)
                {
                    this.index = i;
                    return true;
                }
            }

            this.index = count + 1;
            return false;
        }

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            this.index = 0;
        }
    }

    /// <summary>
    /// Enumerates the keys in the map.
    /// </summary>
    public readonly struct KeyEnumerable : IEnumerable<TKey>
    {
        private readonly UnorderedMap<TKey, TValue> map;

        internal KeyEnumerable(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        /// <summary>
        /// Enumerates the keys in the map without a defined sort order.
        /// </summary>
        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly UnorderedMap<TKey, TValue> map;
            private readonly Node[] nodes;
            private readonly int version;
            private readonly int count;
            private int index;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.nodes = map.nodes;
                this.version = map.version;
                this.count = map.nodeCount;
                this.index = 0;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.nodes[this.index - 1].key;
            }

            object? IEnumerator.Current
            {
                get
                {
                    if (this.version != this.map.version)
                    {
                        ThrowVersionMismatch();
                    }

                    if (this.index == 0 || this.index == this.count + 1)
                    {
                        ThrowInvalidEnumeratorState();
                    }

                    return this.Current;
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var nodes = this.nodes;
                var count = this.count;
                var i = this.index;

                while ((uint)i < (uint)count)
                {
                    i++;

                    if (nodes[i - 1].previous != Node.UnusedNode)
                    {
                        this.index = i;
                        return true;
                    }
                }

                this.index = count + 1;
                return false;
            }

            /// <summary>
            /// Releases the resources used by the enumerator. This is a no-op.
            /// </summary>
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                this.index = 0;
            }
        }
    }

    /// <summary>
    /// Enumerates the values in the map.
    /// </summary>
    public readonly struct ValueEnumerable : IEnumerable<TValue>
    {
        private readonly UnorderedMap<TKey, TValue> map;

        internal ValueEnumerable(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        /// <summary>
        /// Enumerates the values in the map without a defined sort order.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly UnorderedMap<TKey, TValue> map;
            private readonly Node[] nodes;
            private readonly int version;
            private readonly int count;
            private int index;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.nodes = map.nodes;
                this.version = map.version;
                this.count = map.nodeCount;
                this.index = 0;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.nodes[this.index - 1].value;
            }

            object? IEnumerator.Current
            {
                get
                {
                    if (this.version != this.map.version)
                    {
                        ThrowVersionMismatch();
                    }

                    if (this.index == 0 || this.index == this.count + 1)
                    {
                        ThrowInvalidEnumeratorState();
                    }

                    return this.Current;
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var nodes = this.nodes;
                var count = this.count;
                var i = this.index;

                while ((uint)i < (uint)count)
                {
                    i++;

                    if (nodes[i - 1].previous != Node.UnusedNode)
                    {
                        this.index = i;
                        return true;
                    }
                }

                this.index = count + 1;
                return false;
            }

            /// <summary>
            /// Releases the resources used by the enumerator. This is a no-op.
            /// </summary>
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                this.index = 0;
            }
        }
    }

    /// <summary>
    /// Enumerates node indexes matching a key.
    /// </summary>
    public readonly struct NodeEnumerable : IEnumerable<int>
    {
        private readonly UnorderedMap<TKey, TValue> map;
        private readonly TKey? key;

        internal NodeEnumerable(UnorderedMap<TKey, TValue> map, TKey? key)
        {
            this.map = map;
            this.key = key;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map, this.key);

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
            => new Enumerator(this.map, this.key);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map, this.key);

        /// <summary>
        /// Enumerates node indexes whose keys match the requested key.
        /// </summary>
        public struct Enumerator : IEnumerator<int>
        {
            private readonly UnorderedMap<TKey, TValue> map;
            private readonly Node[] nodes;
            private readonly IEqualityComparer<TKey>? comparer;
            private readonly TKey? key;
            private readonly int version;
            private readonly int hashCode;
            private readonly int firstIndex;
            private int nextIndex;
            private int currentIndex;

            internal Enumerator(UnorderedMap<TKey, TValue> map, TKey? key)
            {
                this.map = map;
                this.nodes = map.nodes;
                this.comparer = map.comparer;
                this.key = key;
                this.version = map.version;
                this.currentIndex = -1;

                if (key is null)
                {
                    this.hashCode = 0;
                    this.firstIndex = map.nullList;
                }
                else
                {
                    var comparer = this.comparer;

                    this.hashCode = comparer is null
                        ? key.GetHashCode()
                        : comparer.GetHashCode(key);

                    this.firstIndex =
                        map.buckets[this.hashCode & map.hashMask];
                }

                this.nextIndex = this.firstIndex;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.currentIndex;
            }

            internal readonly TValue CurrentValue
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.nodes[this.currentIndex].value;
            }

            object IEnumerator.Current
                => this.GetCurrentIndexChecked();

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var nodes = this.nodes;
                var i = this.nextIndex;

                if (this.key is null)
                {
                    if ((uint)i >= (uint)nodes.Length)
                    {
                        this.currentIndex = -1;
                        return false;
                    }

                    this.currentIndex = i;
                    this.nextIndex = nodes[i].next;
                    return true;
                }

                var comparer = this.comparer;

                if (comparer is null)
                {
                    while ((uint)i < (uint)nodes.Length)
                    {
                        ref var node = ref nodes[i];
                        var next = node.next;

                        if (node.hashCode == this.hashCode &&
                            EqualityComparer<TKey>.Default.Equals(node.key, this.key))
                        {
                            this.currentIndex = i;
                            this.nextIndex = next;
                            return true;
                        }

                        i = next;
                    }
                }
                else
                {
                    while ((uint)i < (uint)nodes.Length)
                    {
                        ref var node = ref nodes[i];
                        var next = node.next;

                        if (node.hashCode == this.hashCode &&
                            comparer.Equals(node.key, this.key!))
                        {
                            this.currentIndex = i;
                            this.nextIndex = next;
                            return true;
                        }

                        i = next;
                    }
                }

                this.nextIndex = -1;
                this.currentIndex = -1;
                return false;
            }

            /// <summary>
            /// Releases the resources used by the enumerator. This is a no-op.
            /// </summary>
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
                => this.ResetCore();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void ResetCore()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                this.nextIndex = this.firstIndex;
                this.currentIndex = -1;
            }

            internal readonly TValue GetCurrentValueChecked()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                if (this.currentIndex < 0)
                {
                    ThrowInvalidEnumeratorState();
                }

                return this.nodes[this.currentIndex].value;
            }

            private readonly int GetCurrentIndexChecked()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                if (this.currentIndex < 0)
                {
                    ThrowInvalidEnumeratorState();
                }

                return this.currentIndex;
            }
        }
    }

    /// <summary>
    /// Enumerates values matching a key.
    /// </summary>
    public readonly struct MatchedValueEnumerable : IEnumerable<TValue>
    {
        private readonly UnorderedMap<TKey, TValue> map;
        private readonly TKey? key;

        internal MatchedValueEnumerable(UnorderedMap<TKey, TValue> map, TKey? key)
        {
            this.map = map;
            this.key = key;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map, this.key);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(this.map, this.key);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map, this.key);

        /// <summary>
        /// Enumerates values whose keys match the requested key.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private NodeEnumerable.Enumerator enumerator;

            internal Enumerator(UnorderedMap<TKey, TValue> map, TKey? key)
            {
                this.enumerator = new NodeEnumerable.Enumerator(map, key);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.enumerator.CurrentValue;
            }

            object? IEnumerator.Current
                => this.enumerator.GetCurrentValueChecked();

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
                => this.enumerator.MoveNext();

            /// <summary>
            /// Releases the resources used by the enumerator. This is a no-op.
            /// </summary>
            public void Dispose()
            {
            }

            void IEnumerator.Reset()
                => this.enumerator.ResetCore();
        }
    }

    #endregion

    private void Initialize(int capacity)
    {
        if (capacity < 0 || capacity > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        var size = BitOperations.RoundUpToPowerOf2((uint)capacity);

        if (size < MinimumCapacity)
        {
            size = MinimumCapacity;
        }

        var length = (int)size;

        this.hashMask = length - 1;

        this.buckets = new int[length];
        Array.Fill(this.buckets, -1);

        this.nodes = new Node[length];
        this.freeList = -1;
        this.nullList = -1;
    }

    private void SetValue(TKey? key, TValue value)
    {
        if (key is null)
        {
            var index = this.nullList;

            if (index >= 0)
            {
                this.nodes[index].value = value;
                this.version++;
                return;
            }

            this.InsertNull(value);
            return;
        }

        var nodes = this.nodes;
        var comparer = this.comparer;

        if (comparer is null)
        {
            var hashCode = key.GetHashCode();
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    EqualityComparer<TKey>.Default.Equals(node.key, key))
                {
                    node.value = value;
                    this.version++;
                    return;
                }

                i = node.next;
            }

            this.InsertNew(key, value, hashCode);
        }
        else
        {
            var hashCode = comparer.GetHashCode(key);
            var i = this.buckets[hashCode & this.hashMask];

            while ((uint)i < (uint)nodes.Length)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    comparer.Equals(node.key, key))
                {
                    node.value = value;
                    this.version++;
                    return;
                }

                i = node.next;
            }

            this.InsertNew(key, value, hashCode);
        }
    }

    private (int NodeIndex, bool NewlyAdded) Probe(TKey? key, TValue value)
    {
        if (key is null)
        {
            if (!this.AllowDuplicate && this.nullList >= 0)
            {
                return (this.nullList, false);
            }

            return (this.InsertNull(value), true);
        }

        var nodes = this.nodes;
        var comparer = this.comparer;

        if (comparer is null)
        {
            var hashCode = key.GetHashCode();

            if (!this.AllowDuplicate)
            {
                var i = this.buckets[hashCode & this.hashMask];

                while ((uint)i < (uint)nodes.Length)
                {
                    ref var node = ref nodes[i];

                    if (node.hashCode == hashCode &&
                        EqualityComparer<TKey>.Default.Equals(node.key, key))
                    {
                        return (i, false);
                    }

                    i = node.next;
                }
            }

            return (this.InsertNew(key, value, hashCode), true);
        }
        else
        {
            var hashCode = comparer.GetHashCode(key);

            if (!this.AllowDuplicate)
            {
                var i = this.buckets[hashCode & this.hashMask];

                while ((uint)i < (uint)nodes.Length)
                {
                    ref var node = ref nodes[i];

                    if (node.hashCode == hashCode &&
                        comparer.Equals(node.key, key))
                    {
                        return (i, false);
                    }

                    i = node.next;
                }
            }

            return (this.InsertNew(key, value, hashCode), true);
        }
    }

    private int InsertNull(TValue value)
    {
        if (this.nodeCount == this.nodes.Length &&
            this.freeCount == 0)
        {
            this.Resize();
        }

        var nodes = this.nodes;
        var index = this.NewNode();
        ref var node = ref nodes[index];

        node.hashCode = 0;
        node.key = default!;
        node.value = value;
        node.previous = -1;
        node.next = this.nullList;

        if (this.nullList >= 0)
        {
            nodes[this.nullList].previous = index;
        }

        this.nullList = index;
        this.version++;

        return index;
    }

    private int InsertNew(TKey key, TValue value, int hashCode)
    {
        if (this.nodeCount == this.nodes.Length &&
            this.freeCount == 0)
        {
            this.Resize();
        }

        var nodes = this.nodes;
        var bucketIndex = hashCode & this.hashMask;
        var head = this.buckets[bucketIndex];
        var index = this.NewNode();

        ref var node = ref nodes[index];

        node.hashCode = hashCode;
        node.key = key;
        node.value = value;
        node.previous = -1;
        node.next = head;

        if (head >= 0)
        {
            nodes[head].previous = index;
        }

        this.buckets[bucketIndex] = index;
        this.version++;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NewNode()
    {
        if (this.freeCount > 0)
        {
            var index = this.freeList;

            this.freeList = this.nodes[index].next;
            this.freeCount--;

            return index;
        }

        return this.nodeCount++;
    }

    private void Resize()
    {
        var oldSize = this.nodes.Length;

        if (oldSize >= MaximumCapacity)
        {
            ThrowMaximumCapacity();
        }

        // Resize is called only when no reusable nodes remain.
        var newSize = oldSize << 1;
        var newMask = newSize - 1;

        var newBuckets = new int[newSize];
        Array.Fill(newBuckets, -1);

        var count = this.nodeCount;
        var newNodes = new Node[newSize];

        Array.Copy(this.nodes, newNodes, count);

        for (var i = 0; i < count; i++)
        {
            ref var node = ref newNodes[i];

            // Null keys use the dedicated chain.
            if (node.key is null)
            {
                continue;
            }

            var bucketIndex = node.hashCode & newMask;
            var head = newBuckets[bucketIndex];

            node.previous = -1;
            node.next = head;

            if (head >= 0)
            {
                newNodes[head].previous = i;
            }

            newBuckets[bucketIndex] = i;
        }

        this.hashMask = newMask;
        this.buckets = newBuckets;
        this.nodes = newNodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKeyHashCode(TKey key)
    {
        var comparer = this.comparer;

        return comparer is null
            ? key!.GetHashCode()
            : comparer.GetHashCode(key!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool KeysEqual(TKey x, TKey y)
    {
        var comparer = this.comparer;

        return comparer is null
            ? EqualityComparer<TKey>.Default.Equals(x, y)
            : comparer.Equals(x, y);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound()
        => throw new KeyNotFoundException();

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowVersionMismatch()
        => throw new InvalidOperationException(
            "Collection was modified after the enumerator was instantiated.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidEnumeratorState()
        => throw new InvalidOperationException(
            "Enumeration has either not started or has already finished.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMaximumCapacity()
        => throw new InvalidOperationException(
            "The maximum capacity of the collection has been reached.");
}
