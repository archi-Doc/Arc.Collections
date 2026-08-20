// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
/// Represents a high-performance hash map with optional duplicate and null keys.<br/>
/// Node indexes remain stable across resizing.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class UnorderedMap<TKey, TValue>
{
    private const int MinLogCapacity = 2;
    private const int MaximumCapacity = 1 << 30;

    /// <summary>
    /// Represents a node in the map.
    /// </summary>
    public struct Node
    {
        public const int UnusedNode = -2;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        internal int hashCode;
        internal int previous; // UnusedNode if unused.
        internal int next;
        internal TKey key;
        internal TValue value;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

        public readonly bool IsValid() => this.previous != UnusedNode;

        public readonly bool IsInvalid() => this.previous == UnusedNode;

        public readonly TKey Key => this.key;

        public readonly TValue Value => this.value;
    }

    // Null iff TKey is a value type and the default comparer is used.
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
        : this(0, null)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity.
    /// </summary>
    public UnorderedMap(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified comparer.
    /// </summary>
    public UnorderedMap(IEqualityComparer<TKey>? comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified capacity and comparer.
    /// </summary>
    public UnorderedMap(int capacity, IEqualityComparer<TKey>? comparer)
    {
        this.Initialize(capacity);

        // Keep the default comparer null for value types so that the JIT can
        // devirtualize equality and hash-code operations in hot paths.
        if (!typeof(TKey).IsValueType)
        {
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }
        else if (comparer is not null &&
                 !ReferenceEquals(comparer, EqualityComparer<TKey>.Default))
        {
            this.comparer = comparer;
        }
    }

    /// <summary>
    /// Gets the number of elements in the map.
    /// </summary>
    public int Count => this.nodeCount - this.freeCount;

    /// <summary>
    /// Gets the comparer used for keys.
    /// </summary>
    public IEqualityComparer<TKey> Comparer
        => this.comparer ?? EqualityComparer<TKey>.Default;

    /// <summary>
    /// Gets or sets a value indicating whether duplicate keys are allowed.
    /// </summary>
    public bool AllowDuplicate { get; protected set; }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[TKey? key]
    {
        get
        {
            var index = this.FindFirstNode(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            return this.nodes[index].value;
        }

        set
        {
            var index = this.FindFirstNode(key);
            if (index >= 0)
            {
                this.nodes[index].value = value;
                return;
            }

            this.Probe(key, value);
        }
    }

    /// <summary>
    /// Gets direct access to the internal node array.
    /// </summary>
    public (Node[] Nodes, int Max) UnsafeGetNodes()
        => (this.nodes, this.nodeCount);

    /// <summary>
    /// Adds an element, or returns the existing node when duplicate keys are disabled.
    /// </summary>
    public (int NodeIndex, bool NewlyAdded) Add(TKey? key, TValue value)
        => this.Probe(key, value);

    /// <summary>
    /// Determines whether the specified key exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey? key)
        => this.FindFirstNode(key) >= 0;

    /// <summary>
    /// Determines whether the specified key and value exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey? key, TValue value)
        => this.FindNode(key, value) >= 0;

    /// <summary>
    /// Determines whether the specified value exists.
    /// </summary>
    public bool ContainsValue(TValue value)
    {
        var nodes = this.nodes;
        var count = this.nodeCount;

        if (value is null)
        {
            for (var i = 0; i < count; i++)
            {
                ref var node = ref nodes[i];

                if (node.IsValid() && node.value is null)
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

            if (node.IsValid() && comparer.Equals(node.value, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
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
        var hashCode = this.GetKeyHashCode(key);
        var i = this.buckets[hashCode & this.hashMask];

        while (i >= 0)
        {
            ref var node = ref nodes[i];

            if (node.hashCode == hashCode &&
                this.KeysEqual(node.key, key))
            {
                value = node.value;
                return true;
            }

            i = node.next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Finds the first node with the specified key.
    /// </summary>
    /// <returns>The node index, or -1 if not found.</returns>
    public int FindFirstNode(TKey? key)
    {
        if (key is null)
        {
            return this.nullList;
        }

        var nodes = this.nodes;
        var hashCode = this.GetKeyHashCode(key);
        var i = this.buckets[hashCode & this.hashMask];

        while (i >= 0)
        {
            ref var node = ref nodes[i];

            if (node.hashCode == hashCode &&
                this.KeysEqual(node.key, key))
            {
                return i;
            }

            i = node.next;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first node with the specified key and value.
    /// </summary>
    /// <returns>The node index, or -1 if not found.</returns>
    public int FindNode(TKey? key, TValue value)
    {
        var nodes = this.nodes;
        var valueComparer = EqualityComparer<TValue>.Default;

        if (key is null)
        {
            var i = this.nullList;

            while (i >= 0)
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

        var hashCode = this.GetKeyHashCode(key);
        var index = this.buckets[hashCode & this.hashMask];

        while (index >= 0)
        {
            ref var node = ref nodes[index];

            if (node.hashCode == hashCode &&
                this.KeysEqual(node.key, key) &&
                valueComparer.Equals(node.value, value))
            {
                return index;
            }

            index = node.next;
        }

        return -1;
    }

    /// <summary>
    /// Removes the first element with the specified key.
    /// </summary>
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
    /// </summary>
    public void RemoveNode(int nodeIndex)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount)
        {
            return;
        }

        var nodes = this.nodes;
        ref var node = ref nodes[nodeIndex];

        if (node.IsInvalid())
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
    public bool SetNodeKey(int nodeIndex, TKey? key)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount)
        {
            return false;
        }

        var nodes = this.nodes;
        ref var node = ref nodes[nodeIndex];

        if (node.IsInvalid())
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

        // Unlink from the current chain.
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
            var oldBucketIndex = node.hashCode & this.hashMask;

            if (previous < 0)
            {
                this.buckets[oldBucketIndex] = next;
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

        // Relink using the new key.
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
    public bool SetNodeValue(int nodeIndex, TValue value)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount ||
            this.nodes[nodeIndex].IsInvalid())
        {
            return false;
        }

        this.nodes[nodeIndex].value = value;
        return true;
    }

    /// <summary>
    /// Changes a node value without additional validation beyond the node index.
    /// </summary>
    public void UnsafeChangeValue(int nodeIndex, TValue value)
    {
        if ((uint)nodeIndex >= (uint)this.nodeCount ||
            this.nodes[nodeIndex].IsInvalid())
        {
            return;
        }

        this.nodes[nodeIndex].value = value;
    }

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
    /// Enumerates node indexes with the specified key.
    /// </summary>
    public IEnumerable<int> EnumerateNode(TKey? key)
    {
        var version = this.version;
        var i = this.FindFirstNode(key);

        if (key is null)
        {
            while (i >= 0)
            {
                if (version != this.version)
                {
                    ThrowVersionMismatch();
                }

                yield return i;
                i = this.nodes[i].next;
            }

            yield break;
        }

        if (i < 0)
        {
            yield break;
        }

        var hashCode = this.nodes[i].hashCode;

        while (i >= 0)
        {
            if (version != this.version)
            {
                ThrowVersionMismatch();
            }

            ref var node = ref this.nodes[i];

            if (node.hashCode == hashCode &&
                this.KeysEqual(node.key, key))
            {
                yield return i;
            }

            i = node.next;
        }
    }

    /// <summary>
    /// Enumerates values with the specified key.
    /// </summary>
    public IEnumerable<TValue> EnumerateValue(TKey? key)
    {
        var version = this.version;
        var i = this.FindFirstNode(key);

        if (key is null)
        {
            while (i >= 0)
            {
                if (version != this.version)
                {
                    ThrowVersionMismatch();
                }

                yield return this.nodes[i].value;
                i = this.nodes[i].next;
            }

            yield break;
        }

        if (i < 0)
        {
            yield break;
        }

        var hashCode = this.nodes[i].hashCode;

        while (i >= 0)
        {
            if (version != this.version)
            {
                ThrowVersionMismatch();
            }

            ref var node = ref this.nodes[i];

            if (node.hashCode == hashCode &&
                this.KeysEqual(node.key, key))
            {
                yield return node.value;
            }

            i = node.next;
        }
    }

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

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
    /// Enumerates the elements in the map.
    /// </summary>
    public struct Enumerator
    {
        private readonly UnorderedMap<TKey, TValue> map;
        private readonly int version;
        private int index;
        private int currentIndex;

        internal Enumerator(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
            this.version = map.version;
            this.index = 0;
            this.currentIndex = -1;
        }

        public readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var node = ref this.map.nodes[this.currentIndex];
                return new(node.key, node.value);
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            var nodes = this.map.nodes;
            var count = this.map.nodeCount;
            var i = this.index;

            while ((uint)i < (uint)count)
            {
                if (nodes[i].IsValid())
                {
                    this.currentIndex = i;
                    this.index = i + 1;
                    return true;
                }

                i++;
            }

            this.index = count + 1;
            this.currentIndex = -1;
            return false;
        }
    }

    /// <summary>
    /// Enumerates the keys in the map without allocation.
    /// </summary>
    public readonly struct KeyEnumerable
    {
        private readonly UnorderedMap<TKey, TValue> map;

        internal KeyEnumerable(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map);

        public struct Enumerator
        {
            private readonly UnorderedMap<TKey, TValue> map;
            private readonly int version;
            private int index;
            private int currentIndex;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.index = 0;
                this.currentIndex = -1;
            }

            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.map.nodes[this.currentIndex].key;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var nodes = this.map.nodes;
                var count = this.map.nodeCount;
                var i = this.index;

                while ((uint)i < (uint)count)
                {
                    if (nodes[i].IsValid())
                    {
                        this.currentIndex = i;
                        this.index = i + 1;
                        return true;
                    }

                    i++;
                }

                this.index = count + 1;
                this.currentIndex = -1;
                return false;
            }
        }
    }

    /// <summary>
    /// Enumerates the values in the map without allocation.
    /// </summary>
    public readonly struct ValueEnumerable
    {
        private readonly UnorderedMap<TKey, TValue> map;

        internal ValueEnumerable(UnorderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this.map);

        public struct Enumerator
        {
            private readonly UnorderedMap<TKey, TValue> map;
            private readonly int version;
            private int index;
            private int currentIndex;

            internal Enumerator(UnorderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.index = 0;
                this.currentIndex = -1;
            }

            public readonly TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.map.nodes[this.currentIndex].value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var nodes = this.map.nodes;
                var count = this.map.nodeCount;
                var i = this.index;

                while ((uint)i < (uint)count)
                {
                    if (nodes[i].IsValid())
                    {
                        this.currentIndex = i;
                        this.index = i + 1;
                        return true;
                    }

                    i++;
                }

                this.index = count + 1;
                this.currentIndex = -1;
                return false;
            }
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
        var minimumCapacity = 1u << MinLogCapacity;

        if (size < minimumCapacity)
        {
            size = minimumCapacity;
        }

        if (size > MaximumCapacity)
        {
            size = MaximumCapacity;
        }

        var length = (int)size;

        this.hashMask = length - 1;
        this.buckets = new int[length];
        Array.Fill(this.buckets, -1);

        this.nodes = new Node[length];
        this.freeList = -1;
        this.nullList = -1;
    }

    private (int NodeIndex, bool NewlyAdded) Probe(TKey? key, TValue value)
    {
        if (key is null)
        {
            if (!this.AllowDuplicate && this.nullList >= 0)
            {
                return (this.nullList, false);
            }

            if (this.nodeCount == this.nodes.Length &&
                this.freeCount == 0)
            {
                this.Resize();
            }

            var nullIndex = this.NewNode();
            ref var nullNode = ref this.nodes[nullIndex];

            nullNode.hashCode = 0;
            nullNode.key = default!;
            nullNode.value = value;
            nullNode.previous = -1;
            nullNode.next = this.nullList;

            if (this.nullList >= 0)
            {
                this.nodes[this.nullList].previous = nullIndex;
            }

            this.nullList = nullIndex;
            this.version++;

            return (nullIndex, true);
        }

        var hashCode = this.GetKeyHashCode(key);
        var nodes = this.nodes;

        if (!this.AllowDuplicate)
        {
            var i = this.buckets[hashCode & this.hashMask];

            while (i >= 0)
            {
                ref var node = ref nodes[i];

                if (node.hashCode == hashCode &&
                    this.KeysEqual(node.key, key))
                {
                    return (i, false);
                }

                i = node.next;
            }
        }

        // Grow only after duplicate detection.
        if (this.nodeCount == nodes.Length &&
            this.freeCount == 0)
        {
            this.Resize();
            nodes = this.nodes;
        }

        var bucketIndex = hashCode & this.hashMask;
        var head = this.buckets[bucketIndex];
        var newIndex = this.NewNode();

        ref var newNode = ref nodes[newIndex];
        newNode.hashCode = hashCode;
        newNode.key = key;
        newNode.value = value;
        newNode.previous = -1;
        newNode.next = head;

        if (head >= 0)
        {
            nodes[head].previous = newIndex;
        }

        this.buckets[bucketIndex] = newIndex;
        this.version++;

        return (newIndex, true);
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

        // Resize is called only when there are no reusable nodes.
        var newSize = oldSize << 1;
        var newMask = newSize - 1;

        var newBuckets = new int[newSize];
        Array.Fill(newBuckets, -1);

        var count = this.nodeCount;
        var newNodes = new Node[newSize];

        Array.Copy(this.nodes, 0, newNodes, 0, count);

        for (var i = 0; i < count; i++)
        {
            ref var node = ref newNodes[i];

            // Null keys use the dedicated null chain and keep their indexes.
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
        if (comparer is null)
        {
            return key!.GetHashCode();
        }

        return comparer.GetHashCode(key!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool KeysEqual(TKey x, TKey y)
    {
        var comparer = this.comparer;

        if (comparer is null)
        {
            return EqualityComparer<TKey>.Default.Equals(x, y);
        }

        return comparer.Equals(x, y);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowVersionMismatch()
        => throw new InvalidOperationException(
            "Collection was modified after the enumerator was instantiated.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMaximumCapacity()
        => throw new InvalidOperationException(
            "The maximum capacity of the collection has been reached.");
}
