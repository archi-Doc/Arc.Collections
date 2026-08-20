// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

namespace Arc.Collections;

/// <summary>
/// Color of a node in a Red-Black tree.
/// </summary>
internal enum NodeColor : byte
{
    Black,
    Red,
    Unused,
    LinkedList,
}

/// <summary>
/// Represents a key/value collection maintained in sorted order.<br/>
/// Uses a Red-Black Tree to provide O(log n) lookup, insertion, and removal.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class OrderedMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    #region Node

    /// <summary>
    /// Represents a node in the map.
    /// </summary>
    public class Node
    {
        internal Node(TKey key, TValue value, NodeColor color)
        {
            this.Key = key;
            this.Value = value;
            this.Color = color;
        }

        public TKey Key { get; internal set; }

        public TValue Value { get; internal set; }

        internal Node? Parent { get; set; }

        internal Node? Left { get; set; }

        internal Node? Right { get; set; }

        internal NodeColor Color { get; set; }

        /// <summary>
        /// Gets the previous node in sort order.
        /// </summary>
        public Node? Previous
        {
            get
            {
                if (this.Left is not null)
                {
                    var node = this.Left;
                    while (node.Right is not null)
                    {
                        node = node.Right;
                    }

                    return node;
                }

                var current = this;
                var parent = this.Parent;
                while (parent is not null &&
                       ReferenceEquals(current, parent.Left))
                {
                    current = parent;
                    parent = parent.Parent;
                }

                return parent;
            }
        }

        /// <summary>
        /// Gets the next node in sort order.
        /// </summary>
        public Node? Next
        {
            get
            {
                if (this.Right is not null)
                {
                    var node = this.Right;
                    while (node.Left is not null)
                    {
                        node = node.Left;
                    }

                    return node;
                }

                var current = this;
                var parent = this.Parent;
                while (parent is not null &&
                       ReferenceEquals(current, parent.Right))
                {
                    current = parent;
                    parent = parent.Parent;
                }

                return parent;
            }
        }

        /// <summary>
        /// Changes the value without version tracking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeChangeValue(TValue value)
            => this.Value = value;

        internal static bool IsNonNullRed(Node? node)
            => node is not null && node.IsRed;

        internal static bool IsNullOrBlack(Node? node)
            => node is null || node.IsBlack;

        internal bool IsBlack => this.Color == NodeColor.Black;

        internal bool IsRed => this.Color == NodeColor.Red;

        public bool IsUnused => this.Color == NodeColor.Unused;

        public override string ToString()
            => this.Color.ToString() + ": " + this.Value?.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ColorBlack()
            => this.Color = NodeColor.Black;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ColorRed()
            => this.Color = NodeColor.Red;

        internal void Clear()
        {
            this.Key = default!;
            this.Value = default!;
            this.Parent = null;
            this.Left = null;
            this.Right = null;
            this.Color = NodeColor.Unused;
        }

        internal void Reset(TKey key, TValue value, NodeColor color)
        {
            this.Key = key;
            this.Value = value;
            this.Parent = null;
            this.Left = null;
            this.Right = null;
            this.Color = color;
        }
    }

    #endregion

    private Node? root;
    private int version;
    private int count;

    public int Count => this.count;

    public int CompareFactor { get; }

    public IComparer<TKey> Comparer { get; }

    public IHotMethod2<TKey, TValue>? HotMethod2 { get; }

    /// <summary>
    /// Initializes an empty map.
    /// </summary>
    public OrderedMap(bool reverse = false)
        : this(Comparer<TKey>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified comparer.
    /// </summary>
    public OrderedMap(IComparer<TKey>? comparer, bool reverse = false)
    {
        this.CompareFactor = reverse ? -1 : 1;
        this.Comparer = comparer ?? Comparer<TKey>.Default;
        this.HotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.Comparer);
    }

    /// <summary>
    /// Initializes a map from the specified sequence.
    /// </summary>
    public OrderedMap(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey>? comparer = null, bool reverse = false)
        : this(comparer, reverse)
    {
        ArgumentNullException.ThrowIfNull(collection);
        foreach (var item in collection)
        {
            this.Add(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Gets the first node in sort order.
    /// </summary>
    public Node? First
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetFirst(this.root);
    }

    /// <summary>
    /// Gets the last node in sort order.
    /// </summary>
    public Node? Last
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetLast(this.root);
    }

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

    public TValue this[TKey key]
    {
        get
        {
            var node = this.FindNode(key);
            if (node is not null)
            {
                return node.Value;
            }

            ThrowKeyNotFound();
            return default!;
        }

        set
        {
            var result = this.Probe(key, value, null);
            if (!result.NewlyAdded)
            {
                result.Node.Value = value;
                this.version++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey? key)
        => this.FindNode(key) is not null;

    public bool ContainsValue(TValue value)
    {
        var node = this.First;
        if (value is null)
        {
            while (node is not null)
            {
                if (node.Value is null)
                {
                    return true;
                }

                node = node.Next;
            }

            return false;
        }

        var comparer = EqualityComparer<TValue>.Default;
        while (node is not null)
        {
            if (comparer.Equals(node.Value, value))
            {
                return true;
            }

            node = node.Next;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = this.FindNode(key);
        if (node is not null)
        {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes all nodes from the map and invalidates existing node objects.
    /// </summary>
    public void Clear()
    {
        var node = this.root;
        if (node is null)
        {
            return;
        }

        // Destructively walk the tree without allocating a traversal stack.
        while (node is not null)
        {
            if (node.Left is not null)
            {
                var next = node.Left;
                node.Left = null;
                node = next;
                continue;
            }

            if (node.Right is not null)
            {
                var next = node.Right;
                node.Right = null;
                node = next;
                continue;
            }

            var parent = node.Parent;
            node.Clear();
            node = parent;
        }

        this.root = null;
        this.count = 0;
        this.version++;
    }

    /// <summary>
    /// Copies the elements to the specified array.
    /// </summary>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.count)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        foreach (var item in this)
        {
            array[index++] = item;
        }
    }

    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey? key)
    {
        var node = this.FindNode(key);
        if (node is null)
        {
            return false;
        }

        this.RemoveNode(node);
        return true;
    }

    /// <summary>
    /// Adds an element if the key does not already exist.
    /// </summary>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value)
        => this.Probe(key, value, null);

    /// <summary>
    /// Adds an element, optionally reusing an unused node.
    /// </summary>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value, Node reuse)
        => this.Probe(key, value, reuse);

    /// <summary>
    /// Updates a node key while preserving the node when possible.
    /// </summary>
    public bool SetNodeKey(Node node, TKey key)
    {
        if (node.IsUnused)
        {
            return false;
        }

        var cmp = this.CompareInTreeOrder(node.Key, key);
        if (cmp == 0)
        {
            return false;
        }

        if (cmp < 0)
        {
            var next = node.Next;
            if (next is null)
            {
                node.Key = key;
                this.version++;
                return true;
            }

            var nextCmp = this.CompareInTreeOrder(next.Key, key);
            if (nextCmp == 0)
            {
                return false;
            }

            if (nextCmp > 0)
            {
                node.Key = key;
                this.version++;
                return true;
            }
        }
        else
        {
            var previous = node.Previous;
            if (previous is null)
            {
                node.Key = key;
                this.version++;
                return true;
            }

            var previousCmp = this.CompareInTreeOrder(previous.Key, key);
            if (previousCmp == 0)
            {
                return false;
            }

            if (previousCmp < 0)
            {
                node.Key = key;
                this.version++;
                return true;
            }
        }

        // Check before removing the current node to avoid losing the entry
        // when the target key already exists.
        var existing = this.FindNode(key);
        if (existing is not null &&
            !ReferenceEquals(existing, node))
        {
            return false;
        }

        var value = node.Value;
        this.RemoveNode(node);
        this.Probe(key, value, node);
        return true;
    }

    /// <summary>
    /// Updates the value of an active node.
    /// </summary>
    public void SetNodeValue(Node node, TValue value)
    {
        if (node.IsUnused)
        {
            return;
        }

        node.Value = value;
        this.version++;
    }

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    public void RemoveNode(Node node)
    {
        if (node.IsUnused)
        {
            return;
        }

        var originalColor = node.Color;
        Node? replacement = null;
        Node? fixParent = node.Parent;
        var direction = 0;
        if (node.Parent is not null)
        {
            direction = ReferenceEquals(node.Parent.Left, node) ? -1 : 1;
        }

        this.version++;
        this.count--;

        if (node.Left is null)
        {
            replacement = node.Right;
            this.TransplantNode(node.Right, node);
        }
        else if (node.Right is null)
        {
            replacement = node.Left;
            this.TransplantNode(node.Left, node);
        }
        else
        {
            var successor = node.Right;
            while (successor.Left is not null)
            {
                successor = successor.Left;
            }

            originalColor = successor.Color;
            replacement = successor.Right;
            if (ReferenceEquals(successor.Parent, node))
            {
                fixParent = successor;
                direction = 1;
            }
            else
            {
                fixParent = successor.Parent;
                direction = -1;
                this.TransplantNode(successor.Right, successor);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }

            this.TransplantNode(successor, node);
            successor.Left = node.Left;
            successor.Left.Parent = successor;
            successor.Color = node.Color;
        }

        if (originalColor == NodeColor.Red)
        {
            node.Clear();
            this.root?.ColorBlack();
            return;
        }

        // A non-null replacement of a removed black node must be red in a
        // valid Red-Black tree. Recoloring it resolves the black-height deficit.
        if (replacement is not null)
        {
            replacement.ColorBlack();
            node.Clear();
            this.root?.ColorBlack();
            return;
        }

        if (fixParent is null)
        {
            node.Clear();
            this.root?.ColorBlack();
            return;
        }

        var parent = fixParent;
        while (true)
        {
            Node? sibling;
            if (direction < 0)
            {
                sibling = parent.Right;
                if (Node.IsNonNullRed(sibling))
                {
                    sibling!.ColorBlack();
                    parent.ColorRed();
                    this.RotateLeft(parent);
                    sibling = parent.Right;
                }

                if (sibling is null)
                {
                    // Propagate the black-height deficit upward.
                }
                else if (Node.IsNullOrBlack(sibling.Left) &&
                         Node.IsNullOrBlack(sibling.Right))
                {
                    sibling.ColorRed();
                }
                else
                {
                    // Inner rotation only when the far child is black (CLRS);
                    // when both children are red, go directly to the far-child case.
                    if (Node.IsNullOrBlack(sibling.Right))
                    {
                        sibling.Left!.ColorBlack();
                        sibling.ColorRed();
                        this.RotateRight(sibling);
                        sibling = parent.Right;
                    }

                    sibling!.Color = parent.Color;
                    parent.ColorBlack();
                    sibling.Right!.ColorBlack();
                    this.RotateLeft(parent);
                    break;
                }
            }
            else
            {
                sibling = parent.Left;
                if (Node.IsNonNullRed(sibling))
                {
                    sibling!.ColorBlack();
                    parent.ColorRed();
                    this.RotateRight(parent);
                    sibling = parent.Left;
                }

                if (sibling is null)
                {
                    // Propagate the black-height deficit upward.
                }
                else if (Node.IsNullOrBlack(sibling.Left) &&
                         Node.IsNullOrBlack(sibling.Right))
                {
                    sibling.ColorRed();
                }
                else
                {
                    // Inner rotation only when the far child is black (CLRS);
                    // when both children are red, go directly to the far-child case.
                    if (Node.IsNullOrBlack(sibling.Left))
                    {
                        sibling.Right!.ColorBlack();
                        sibling.ColorRed();
                        this.RotateLeft(sibling);
                        sibling = parent.Left;
                    }

                    sibling!.Color = parent.Color;
                    parent.ColorBlack();
                    sibling.Left!.ColorBlack();
                    this.RotateRight(parent);
                    break;
                }
            }

            if (parent.IsRed || parent.Parent is null)
            {
                parent.ColorBlack();
                break;
            }

            var nextParent = parent.Parent;
            direction = ReferenceEquals(parent, nextParent.Left)
                ? -1
                : 1;
            parent = nextParent;
        }

        node.Clear();
        this.root?.ColorBlack();
    }

    #region Search

    /// <summary>
    /// Searches for a node with the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node? FindNode(TKey? key)
    {
        var result = this.SearchNode(this.root, key);
        return result.Cmp == 0
            ? result.Leaf
            : null;
    }

    /// <summary>
    /// Gets the first node equal to or after the specified key in map order.
    /// </summary>
    public Node? GetLowerBound(TKey? key)
    {
        var (cmp, node) = this.SearchNode(this.root, key);
        if (cmp == 0)
        {
            return node;
        }

        return cmp < 0
            ? node
            : node?.Next;
    }

    /// <summary>
    /// Gets the last node equal to or before the specified key in map order.
    /// </summary>
    public Node? GetUpperBound(TKey? key)
    {
        var (cmp, node) = this.SearchNode(this.root, key);
        if (cmp == 0)
        {
            return node;
        }

        return cmp < 0
            ? node?.Previous
            : node;
    }

    /// <summary>
    /// Gets the nodes delimiting the specified range.
    /// </summary>
    public (Node? Lower, Node? Upper) GetRange(TKey? lower, TKey? upper)
    {
        var lowerNode = this.GetLowerBound(lower);
        if (lowerNode is null)
        {
            return (null, null);
        }

        var upperNode = this.GetUpperBound(upper);
        if (upperNode is null ||
            this.CompareInTreeOrder(lowerNode.Key, upperNode.Key) > 0)
        {
            return (null, null);
        }

        return (lowerNode, upperNode);
    }

    private (int Cmp, Node? Leaf) SearchNode(Node? target, TKey? key)
    {
        var node = target;
        Node? parent = null;
        var cmp = 0;
        var comparer = this.Comparer;
        var hotMethod = this.HotMethod2;

        if (this.CompareFactor > 0)
        {
            if (hotMethod is not null)
            {
                return hotMethod.SearchNode(node, key!);
            }

            if (key is null)
            {
                while (node is not null)
                {
                    if (node.Key is null)
                    {
                        return (0, node);
                    }

                    parent = node;
                    cmp = -1;
                    node = node.Left;
                }

                return (cmp, parent);
            }

            if (typeof(TKey).IsValueType &&
                ReferenceEquals(comparer, Comparer<TKey>.Default))
            {
                // Comparer<TKey>.Default.Compare is devirtualized and inlined by the JIT
                // for value-type instantiations. This path also avoids the boxing that
                // the IComparable<TKey> pattern below incurs once per search.
                while (node is not null)
                {
                    cmp = Comparer<TKey>.Default.Compare(key, node.Key);
                    parent = node;
                    if (cmp < 0)
                    {
                        node = node.Left;
                    }
                    else if (cmp > 0)
                    {
                        node = node.Right;
                    }
                    else
                    {
                        return (0, node);
                    }
                }

                return (cmp, parent);
            }

            if (ReferenceEquals(comparer, Comparer<TKey>.Default) &&
                key is IComparable<TKey> comparable)
            {
                while (node is not null)
                {
                    cmp = comparable.CompareTo(node.Key);
                    parent = node;
                    if (cmp < 0)
                    {
                        node = node.Left;
                    }
                    else if (cmp > 0)
                    {
                        node = node.Right;
                    }
                    else
                    {
                        return (0, node);
                    }
                }

                return (cmp, parent);
            }

            while (node is not null)
            {
                cmp = comparer.Compare(key, node.Key);
                parent = node;
                if (cmp < 0)
                {
                    node = node.Left;
                }
                else if (cmp > 0)
                {
                    node = node.Right;
                }
                else
                {
                    return (0, node);
                }
            }
        }
        else
        {
            if (hotMethod is not null)
            {
                return hotMethod.SearchNodeReverse(node, key!);
            }

            if (key is null)
            {
                while (node is not null)
                {
                    if (node.Key is null)
                    {
                        return (0, node);
                    }

                    parent = node;
                    cmp = 1;
                    node = node.Right;
                }

                return (cmp, parent);
            }

            if (typeof(TKey).IsValueType &&
                ReferenceEquals(comparer, Comparer<TKey>.Default))
            {
                while (node is not null)
                {
                    var c = Comparer<TKey>.Default.Compare(key, node.Key);
                    parent = node;
                    if (c > 0)
                    {
                        cmp = -1;
                        node = node.Left;
                    }
                    else if (c < 0)
                    {
                        cmp = 1;
                        node = node.Right;
                    }
                    else
                    {
                        return (0, node);
                    }
                }

                return (cmp, parent);
            }

            if (ReferenceEquals(comparer, Comparer<TKey>.Default) &&
                key is IComparable<TKey> comparable)
            {
                while (node is not null)
                {
                    var c = comparable.CompareTo(node.Key);
                    parent = node;
                    if (c > 0)
                    {
                        cmp = -1;
                        node = node.Left;
                    }
                    else if (c < 0)
                    {
                        cmp = 1;
                        node = node.Right;
                    }
                    else
                    {
                        return (0, node);
                    }
                }

                return (cmp, parent);
            }

            while (node is not null)
            {
                var c = comparer.Compare(key, node.Key);
                parent = node;
                if (c > 0)
                {
                    cmp = -1;
                    node = node.Left;
                }
                else if (c < 0)
                {
                    cmp = 1;
                    node = node.Right;
                }
                else
                {
                    return (0, node);
                }
            }
        }

        return (cmp, parent);
    }

    #endregion

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>>
        IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly OrderedMap<TKey, TValue> map;
        private readonly int version;
        private Node? current;
        private Node? next;

        internal Enumerator(OrderedMap<TKey, TValue> map)
        {
            this.map = map;
            this.version = map.version;
            this.current = null;
            this.next = GetFirst(map.root);
        }

        public readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var node = this.current!;
                return new(node.Key, node.Value);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                this.ValidateCurrent();
                return this.Current;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            var node = this.next;
            if (node is null)
            {
                this.current = null;
                return false;
            }

            this.current = node;
            this.next = node.Next;
            return true;
        }

        public void Dispose()
        {
            this.current = null;
            this.next = null;
        }

        void IEnumerator.Reset()
            => this.ResetCore();

        private void ResetCore()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            this.current = null;
            this.next = GetFirst(this.map.root);
        }

        private readonly void ValidateCurrent()
        {
            if (this.version != this.map.version)
            {
                ThrowVersionMismatch();
            }

            if (this.current is null)
            {
                ThrowInvalidEnumeratorState();
            }
        }
    }

    public readonly struct KeyEnumerable : IEnumerable<TKey>
    {
        private readonly OrderedMap<TKey, TValue> map;

        internal KeyEnumerable(OrderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly OrderedMap<TKey, TValue> map;
            private readonly int version;
            private Node? current;
            private Node? next;

            internal Enumerator(OrderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.current = null;
                this.next = GetFirst(map.root);
            }

            public readonly TKey Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.current!.Key;
            }

            object? IEnumerator.Current
            {
                get
                {
                    this.ValidateCurrent();
                    return this.current!.Key;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var node = this.next;
                if (node is null)
                {
                    this.current = null;
                    return false;
                }

                this.current = node;
                this.next = node.Next;
                return true;
            }

            public void Dispose()
            {
                this.current = null;
                this.next = null;
            }

            void IEnumerator.Reset()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                this.current = null;
                this.next = GetFirst(this.map.root);
            }

            private readonly void ValidateCurrent()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                if (this.current is null)
                {
                    ThrowInvalidEnumeratorState();
                }
            }
        }
    }

    public readonly struct ValueEnumerable : IEnumerable<TValue>
    {
        private readonly OrderedMap<TKey, TValue> map;

        internal ValueEnumerable(OrderedMap<TKey, TValue> map)
        {
            this.map = map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly OrderedMap<TKey, TValue> map;
            private readonly int version;
            private Node? current;
            private Node? next;

            internal Enumerator(OrderedMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.current = null;
                this.next = GetFirst(map.root);
            }

            public readonly TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.current!.Value;
            }

            object? IEnumerator.Current
            {
                get
                {
                    this.ValidateCurrent();
                    return this.current!.Value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                var node = this.next;
                if (node is null)
                {
                    this.current = null;
                    return false;
                }

                this.current = node;
                this.next = node.Next;
                return true;
            }

            public void Dispose()
            {
                this.current = null;
                this.next = null;
            }

            void IEnumerator.Reset()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                this.current = null;
                this.next = GetFirst(this.map.root);
            }

            private readonly void ValidateCurrent()
            {
                if (this.version != this.map.version)
                {
                    ThrowVersionMismatch();
                }

                if (this.current is null)
                {
                    ThrowInvalidEnumeratorState();
                }
            }
        }
    }

    #endregion

    #region Insert

    private (Node Node, bool NewlyAdded) Probe(TKey key, TValue value, Node? reuse)
    {
        var (cmp, parent) = this.SearchNode(this.root, key);
        if (cmp == 0 && parent is not null)
        {
            return (parent, false);
        }

        Node node;
        if (reuse is not null && reuse.IsUnused)
        {
            reuse.Reset(key, value, NodeColor.Red);
            node = reuse;
        }
        else
        {
            node = new Node(key, value, NodeColor.Red);
        }

        node.Parent = parent;
        if (parent is null)
        {
            this.root = node;
            node.ColorBlack();
            this.count++;
            this.version++;
            return (node, true);
        }

        if (cmp < 0)
        {
            parent.Left = node;
        }
        else
        {
            parent.Right = node;
        }

        this.count++;
        this.version++;

        var current = node;
#nullable disable
        while (current.Parent is not null &&
               current.Parent.IsRed)
        {
            var grandParent = current.Parent.Parent;
            if (ReferenceEquals(current.Parent, grandParent.Right))
            {
                var uncle = grandParent.Left;
                if (uncle is not null && uncle.IsRed)
                {
                    uncle.ColorBlack();
                    current.Parent.ColorBlack();
                    grandParent.ColorRed();
                    current = grandParent;
                }
                else
                {
                    if (ReferenceEquals(current, current.Parent.Left))
                    {
                        current = current.Parent;
                        this.RotateRight(current);
                    }

                    current.Parent.ColorBlack();
                    current.Parent.Parent.ColorRed();
                    this.RotateLeft(current.Parent.Parent);
                    break;
                }
            }
            else
            {
                var uncle = grandParent.Right;
                if (uncle is not null && uncle.IsRed)
                {
                    uncle.ColorBlack();
                    current.Parent.ColorBlack();
                    grandParent.ColorRed();
                    current = grandParent;
                }
                else
                {
                    if (ReferenceEquals(current, current.Parent.Right))
                    {
                        current = current.Parent;
                        this.RotateLeft(current);
                    }

                    current.Parent.ColorBlack();
                    current.Parent.Parent.ColorRed();
                    this.RotateRight(current.Parent.Parent);
                    break;
                }
            }
        }
#nullable enable

        this.root!.ColorBlack();
        return (node, true);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates the Red-Black Tree.
    /// </summary>
    public bool Validate()
    {
        if (this.root is null)
        {
            return this.count == 0;
        }

        if (!this.root.IsBlack ||
            this.root.Parent is not null)
        {
            return false;
        }

        Node? previous = null;
        var actualCount = 0;
        if (!this.ValidateBST(this.root, ref previous, ref actualCount))
        {
            return false;
        }

        if (actualCount != this.count)
        {
            return false;
        }

        if (!ValidateColors(this.root))
        {
            return false;
        }

        return ValidateBlackHeight(this.root) >= 0;
    }

    private bool ValidateBST(Node? node, ref Node? previous, ref int actualCount)
    {
        if (node is null)
        {
            return true;
        }

        if (node.Left is not null &&
            !ReferenceEquals(node.Left.Parent, node))
        {
            return false;
        }

        if (node.Right is not null &&
            !ReferenceEquals(node.Right.Parent, node))
        {
            return false;
        }

        if (!this.ValidateBST(node.Left, ref previous, ref actualCount))
        {
            return false;
        }

        if (previous is not null &&
            this.CompareInTreeOrder(previous.Key, node.Key) >= 0)
        {
            return false;
        }

        previous = node;
        actualCount++;
        return this.ValidateBST(node.Right, ref previous, ref actualCount);
    }

    private static bool ValidateColors(Node? node)
    {
        if (node is null)
        {
            return true;
        }

        if (node.IsRed &&
            (Node.IsNonNullRed(node.Left) ||
             Node.IsNonNullRed(node.Right)))
        {
            return false;
        }

        return ValidateColors(node.Left) &&
               ValidateColors(node.Right);
    }

    private static int ValidateBlackHeight(Node? node)
    {
        if (node is null)
        {
            return 0;
        }

        var left = ValidateBlackHeight(node.Left);
        var right = ValidateBlackHeight(node.Right);
        if (left < 0 ||
            right < 0 ||
            left != right)
        {
            return -1;
        }

        return left + (node.IsBlack ? 1 : 0);
    }

    #endregion

    #region LowLevel

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Node? GetFirst(Node? node)
    {
        if (node is null)
        {
            return null;
        }

        while (node.Left is not null)
        {
            node = node.Left;
        }

        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Node? GetLast(Node? node)
    {
        if (node is null)
        {
            return null;
        }

        while (node.Right is not null)
        {
            node = node.Right;
        }

        return node;
    }

    private int CompareInTreeOrder(TKey? x, TKey? y)
    {
        int cmp;
        if (x is null)
        {
            cmp = y is null ? 0 : -1;
        }
        else if (y is null)
        {
            cmp = 1;
        }
        else
        {
            cmp = this.Comparer.Compare(x, y);
        }

        if (this.CompareFactor > 0)
        {
            return cmp;
        }

        return cmp < 0
            ? 1
            : cmp > 0
                ? -1
                : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TransplantNode(Node? node, Node destination)
    {
        var parent = destination.Parent;
        if (parent is null)
        {
            this.root = node;
        }
        else if (ReferenceEquals(destination, parent.Left))
        {
            parent.Left = node;
        }
        else
        {
            parent.Right = node;
        }

        if (node is not null)
        {
            node.Parent = parent;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateLeft(Node node)
    {
        var right = node.Right!;
        node.Right = right.Left;
        if (right.Left is not null)
        {
            right.Left.Parent = node;
        }

        var parent = node.Parent;
        right.Parent = parent;
        if (parent is null)
        {
            this.root = right;
        }
        else if (ReferenceEquals(node, parent.Left))
        {
            parent.Left = right;
        }
        else
        {
            parent.Right = right;
        }

        right.Left = node;
        node.Parent = right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateRight(Node node)
    {
        var left = node.Left!;
        node.Left = left.Right;
        if (left.Right is not null)
        {
            left.Right.Parent = node;
        }

        var parent = node.Parent;
        left.Parent = parent;
        if (parent is null)
        {
            this.root = left;
        }
        else if (ReferenceEquals(node, parent.Right))
        {
            parent.Right = left;
        }
        else
        {
            parent.Left = left;
        }

        left.Right = node;
        node.Parent = left;
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

    #endregion
}
