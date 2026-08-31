// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented

namespace Arc.Collections;

/// <summary>
/// Represents a sorted multi-map backed by a Red-Black Tree and circular linked lists.<br/>
/// Duplicate keys are stored in insertion order.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class OrderedMultiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
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

        /// <summary>
        /// Gets the key stored in the node.
        /// </summary>
        public TKey Key { get; internal set; }

        /// <summary>
        /// Gets the value stored in the node.
        /// </summary>
        public TValue Value { get; internal set; }

        internal Node? Parent { get; set; }

        internal Node? Left { get; set; }

        internal Node? Right { get; set; }

        internal Node? ListPrevious { get; set; }

        internal Node? ListNext { get; set; }

        internal NodeColor Color { get; set; }

        /// <summary>
        /// Gets the previous node in map order.
        /// </summary>
        public Node? Previous
        {
            get
            {
                if (this.IsLinkedListNode)
                {
                    return this.ListPrevious;
                }

                if (this.Left is not null)
                {
                    var node = this.Left;
                    while (node.Right is not null)
                    {
                        node = node.Right;
                    }

                    return node.IsSingleNode ? node : node.ListPrevious;
                }

                var current = this;
                var parent = this.Parent;
                while (parent is not null && ReferenceEquals(current, parent.Left))
                {
                    current = parent;
                    parent = parent.Parent;
                }

                return parent is null || parent.IsSingleNode
                    ? parent
                    : parent.ListPrevious;
            }
        }

        /// <summary>
        /// Gets the next node in map order.
        /// </summary>
        public Node? Next
        {
            get
            {
                Node treeNode;
                if (this.IsSingleNode)
                {
                    treeNode = this;
                }
                else if (this.IsLinkedListNode)
                {
                    var next = this.ListNext!;
                    if (next.IsLinkedListNode)
                    {
                        return next;
                    }

                    treeNode = next;
                }
                else
                {
                    return this.ListNext;
                }

                if (treeNode.Right is not null)
                {
                    var node = treeNode.Right;
                    while (node.Left is not null)
                    {
                        node = node.Left;
                    }

                    return node;
                }

                var current = treeNode;
                var parent = treeNode.Parent;
                while (parent is not null && ReferenceEquals(current, parent.Right))
                {
                    current = parent;
                    parent = parent.Parent;
                }

                return parent;
            }
        }

        /// <summary>
        /// Changes the value without validation or version tracking, so outstanding enumerators stay valid.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeChangeValue(TValue value)
            => this.Value = value;

        internal static bool IsNonNullRed(Node? node)
            => node is not null && node.IsRed;

        internal static bool IsNullOrBlack(Node? node)
            => node is null || node.IsBlack;

        internal bool IsBlack => this.Color == NodeColor.Black;

        internal bool IsRed => this.Color == NodeColor.Red;

        internal bool IsUnused => this.Color == NodeColor.Unused;

        internal bool IsLinkedListNode => this.Color == NodeColor.LinkedList;

        internal bool IsSingleNode => this.ListPrevious is null;

        /// <summary>
        /// Returns a string representation of the node.
        /// </summary>
        /// <returns>The node color followed by its value.</returns>
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
            this.ListPrevious = null;
            this.ListNext = null;
            this.Color = NodeColor.Unused;
        }

        internal void Reset(TKey key, TValue value, NodeColor color)
        {
            this.Key = key;
            this.Value = value;
            this.Parent = null;
            this.Left = null;
            this.Right = null;
            this.ListPrevious = null;
            this.ListNext = null;
            this.Color = color;
        }
    }

    #endregion

    private readonly IComparer<TKey> comparer;
    private readonly IHotMethod2<TKey, TValue>? hotMethod2;
    private Node? root;
    private int version;
    private int count;

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => this.count;

    /// <summary>
    /// Gets a value indicating whether the collection is sorted in reverse order.
    /// </summary>
    public bool Reverse { get; }

    /// <summary>
    /// Gets the comparer used to order the keys.
    /// </summary>
    public IComparer<TKey> Comparer => this.comparer;

    /// <summary>
    /// Gets the specialized tree-search implementation for <typeparamref name="TKey"/>,
    /// or <see langword="null"/> when none is available.
    /// </summary>
    public IHotMethod2<TKey, TValue>? HotMethod2 => this.hotMethod2;

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
    /// Initializes a new instance of the <see cref="OrderedMultiMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="reverse"><see langword="true"/> to sort the keys in descending order.</param>
    public OrderedMultiMap(bool reverse = false)
        : this(Comparer<TKey>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiMap{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer to use for the keys, or <see langword="null"/> for <see cref="Comparer{T}.Default"/>.</param>
    /// <param name="reverse"><see langword="true"/> to sort the keys in descending order.</param>
    public OrderedMultiMap(IComparer<TKey>? comparer, bool reverse = false)
    {
        this.Reverse = reverse;
        this.comparer = comparer ?? Comparer<TKey>.Default;
        this.hotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiMap{TKey, TValue}"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary whose entries are copied.</param>
    /// <param name="reverse"><see langword="true"/> to sort the keys in descending order.</param>
    public OrderedMultiMap(IDictionary<TKey, TValue> dictionary, bool reverse = false)
        : this(dictionary, Comparer<TKey>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMultiMap{TKey, TValue}"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary whose entries are copied.</param>
    /// <param name="comparer">The comparer to use for the keys, or <see langword="null"/> for <see cref="Comparer{T}.Default"/>.</param>
    /// <param name="reverse"><see langword="true"/> to sort the keys in descending order.</param>
    public OrderedMultiMap(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer, bool reverse = false)
        : this(comparer, reverse)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        foreach (var item in dictionary)
        {
            this.Add(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Gets the first node in map order.
    /// </summary>
    public Node? First
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetFirst(this.root);
    }

    /// <summary>
    /// Gets the last node in map order.
    /// </summary>
    public Node? Last
    {
        get
        {
            var node = GetLastTreeNode(this.root);
            return node is null || node.IsSingleNode
                ? node
                : node.ListPrevious;
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value of the first node with <paramref name="key"/>.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist (getter only).</exception>
    public TValue this[TKey key]
    {
        get
        {
            var node = this.FindFirstNode(key);
            if (node is not null)
            {
                return node.Value;
            }

            ThrowKeyNotFound();
            return default!;
        }

        set => this.Add(key, value);
    }

    /// <summary>
    /// Determines whether the collection contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey? key)
        => this.FindFirstNode(key) is not null;

    /// <summary>
    /// Determines whether the collection contains the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, the value of the first matching node; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey? key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = this.FindFirstNode(key);
        if (node is not null)
        {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes all nodes and invalidates existing node objects.
    /// </summary>
    public void Clear()
    {
        if (this.root is null)
        {
            return;
        }

        ClearTree(this.root);
        this.root = null;
        this.count = 0;
        this.version++;
    }

    /// <summary>
    /// Copies the key-value pairs to an array, in map order.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based destination index.</param>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.count)
        {
            throw new ArgumentException(
                "The destination array is too small.",
                nameof(array));
        }

        var node = this.First;
        while (node is not null)
        {
            array[index++] = new(node.Key, node.Value);
            node = node.Next;
        }
    }

    /// <summary>
    /// Removes the first node with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if a node was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey? key)
    {
        var node = this.FindFirstNode(key);
        if (node is null)
        {
            return false;
        }

        this.RemoveNode(node);
        return true;
    }

    /// <summary>
    /// Removes the first node with the specified key and value.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="value">The value to match.</param>
    /// <returns><see langword="true"/> if a node was removed; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key, TValue value)
    {
        var node = this.FindNode(key, value);
        if (node is null)
        {
            return false;
        }

        this.RemoveNode(node);
        return true;
    }

    /// <summary>
    /// Adds a new value. Duplicate keys are allowed.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value)
        => this.Probe(key, value, null);

    /// <summary>
    /// Adds a new value, optionally reusing an unused node.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="reuse">An unused node to reuse when possible.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value, Node reuse)
        => this.Probe(key, value, reuse);

    /// <summary>
    /// Changes the key while preserving the node object.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key was changed; otherwise, <see langword="false"/>.</returns>
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

        // Only a single tree node may be changed in place.
        // Nodes in a duplicate group must all retain the same key.
        if (node.IsSingleNode)
        {
            if (cmp < 0)
            {
                var next = node.Next;
                if (next is null ||
                    this.CompareInTreeOrder(next.Key, key) > 0)
                {
                    node.Key = key;
                    this.version++;
                    return true;
                }
            }
            else
            {
                var previous = node.Previous;
                if (previous is null ||
                    this.CompareInTreeOrder(previous.Key, key) < 0)
                {
                    node.Key = key;
                    this.version++;
                    return true;
                }
            }
        }

        var value = node.Value;
        this.RemoveNode(node);
        this.Probe(key, value, node);
        return true;
    }

    /// <summary>
    /// Changes the value of an active node without invalidating enumerators.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNodeValue(Node node, TValue value)
    {
        if (!node.IsUnused)
        {
            node.Value = value;
        }
    }

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    /// <param name="node">The node.</param>
    public void RemoveNode(Node node)
    {
        if (node.IsUnused)
        {
            return;
        }

        this.version++;
        this.count--;

        if (node.IsLinkedListNode)
        {
            node.ListPrevious!.ListNext = node.ListNext;
            node.ListNext!.ListPrevious = node.ListPrevious;
            var head = node.ListNext!;
            if (ReferenceEquals(head, head.ListNext))
            {
                Debug.Assert(!head.IsLinkedListNode);
                head.ListPrevious = null;
                head.ListNext = null;
            }

            node.Clear();
            return;
        }

        if (!node.IsSingleNode)
        {
            // Promote the next duplicate to the tree node.
            node.ListPrevious!.ListNext = node.ListNext;
            node.ListNext!.ListPrevious = node.ListPrevious;
            var newHead = node.ListNext!;
            newHead.Color = node.Color;
            this.TransplantNode(newHead, node);
            newHead.Left = node.Left;
            if (newHead.Left is not null)
            {
                newHead.Left.Parent = newHead;
            }

            newHead.Right = node.Right;
            if (newHead.Right is not null)
            {
                newHead.Right.Parent = newHead;
            }

            if (ReferenceEquals(newHead.ListNext, newHead))
            {
                newHead.ListPrevious = null;
                newHead.ListNext = null;
            }

            node.Clear();
            return;
        }

        this.RemoveTreeNode(node);
    }

    private void RemoveTreeNode(Node node)
    {
        var originalColor = node.Color;
        Node? replacement;
        Node? fixParent = node.Parent;
        var direction = 0;
        if (node.Parent is not null)
        {
            direction = ReferenceEquals(node, node.Parent.Left) ? -1 : 1;
        }

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

        // A non-null replacement of a removed black node is red.
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
                    // Propagate the black-height deficit.
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
                    // Propagate the black-height deficit.
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

    private (int Cmp, Node? Leaf) SearchFirstNode(Node? target, TKey? key)
    {
        var node = target;
        Node? parent = null;
        var cmp = 0;
        var comparer = this.comparer;
        var hotMethod = this.hotMethod2;

        if (!this.Reverse)
        {
            // Handle null before HotMethod because HotMethod is intended for non-null value keys.
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

            if (hotMethod is not null)
            {
                return hotMethod.SearchNode(node, key);
            }

            if (typeof(TKey).IsValueType &&
                ReferenceEquals(comparer, Comparer<TKey>.Default))
            {
                // Comparer<TKey>.Default.Compare is devirtualized and inlined by the JIT
                // for value-type instantiations.
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

            if (!typeof(TKey).IsValueType &&
                ReferenceEquals(comparer, Comparer<TKey>.Default) &&
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

            if (hotMethod is not null)
            {
                return hotMethod.SearchNodeReverse(node, key);
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

            if (!typeof(TKey).IsValueType &&
                ReferenceEquals(comparer, Comparer<TKey>.Default) &&
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

    /// <summary>
    /// Finds the first node with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The first matching node, or <see langword="null"/> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node? FindFirstNode(TKey? key)
    {
        var result = this.SearchFirstNode(this.root, key);
        return result.Cmp == 0 ? result.Leaf : null;
    }

    /// <summary>
    /// Finds a node with the specified key and value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The matching node, or <see langword="null"/> if not found.</returns>
    public Node? FindNode(TKey? key, TValue value)
    {
        var result = this.SearchFirstNode(this.root, key);
        if (result.Cmp != 0 || result.Leaf is null)
        {
            return null;
        }

        var node = result.Leaf;
        var comparer = EqualityComparer<TValue>.Default;
        if (node.IsSingleNode)
        {
            return comparer.Equals(node.Value, value)
                ? node
                : null;
        }

        do
        {
            if (comparer.Equals(node.Value, value))
            {
                return node;
            }

            node = node.ListNext!;
        }
        while (node.IsLinkedListNode);

        return null;
    }

    /// <summary>
    /// Gets the first node equal to or after the specified key in map order.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The first node at or after the specified key in collection order, or <see langword="null"/> if none exists.</returns>
    public Node? GetLowerBound(TKey? key)
    {
        var (cmp, node) = this.SearchFirstNode(this.root, key);
        if (node is null || cmp <= 0)
        {
            return node;
        }

        return GetNextTreeNode(node);
    }

    /// <summary>
    /// Gets the last node equal to or before the specified key in map order.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The last node at or before the specified key in collection order, or <see langword="null"/> if none exists.</returns>
    public Node? GetUpperBound(TKey? key)
    {
        var (cmp, node) = this.SearchFirstNode(this.root, key);
        if (node is null)
        {
            return null;
        }

        if (cmp >= 0)
        {
            return node.IsSingleNode
                ? node
                : node.ListPrevious;
        }

        return node.Previous;
    }

    /// <summary>
    /// Gets the nodes delimiting the inclusive range between two keys.
    /// </summary>
    /// <param name="lower">The lower key.</param>
    /// <param name="upper">The upper key.</param>
    /// <returns>The first node at or after <paramref name="lower"/> and the last node at or before
    /// <paramref name="upper"/>, or <c>(null, null)</c> if the range is empty.</returns>
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

    /// <summary>
    /// Enumerates nodes with the specified key without allocation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>An allocation-free enumerable over the matching nodes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NodeEnumerable EnumerateNode(TKey? key)
        => new(this, key);

    /// <summary>
    /// Enumerates values with the specified key without allocation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>An allocation-free enumerable over the matching values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MatchedValueEnumerable EnumerateValue(TKey? key)
        => new(this, key);

    /// <summary>
    /// Enumerates the nodes of a duplicate-key group.
    /// </summary>
    public readonly struct NodeEnumerable : IEnumerable<Node>
    {
        private readonly OrderedMultiMap<TKey, TValue> map;
        private readonly TKey? key;

        internal NodeEnumerable(OrderedMultiMap<TKey, TValue> map, TKey? key)
        {
            this.map = map;
            this.key = key;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map, this.key);

        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
            => new Enumerator(this.map, this.key);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map, this.key);

        /// <summary>
        /// Enumerates the elements of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<Node>
        {
            private readonly OrderedMultiMap<TKey, TValue> map;
            private readonly TKey? key;
            private readonly int version;
            private Node? current;
            private Node? next;

            internal Enumerator(OrderedMultiMap<TKey, TValue> map, TKey? key)
            {
                this.map = map;
                this.key = key;
                this.version = map.version;
                this.current = null;
                this.next = map.FindFirstNode(key);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly Node Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.current!;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (this.version != this.map.version)
                    {
                        ThrowVersionMismatch();
                    }

                    if (this.current is null)
                    {
                        ThrowInvalidEnumeratorState();
                    }

                    return this.current;
                }
            }

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

                var node = this.next;
                if (node is null)
                {
                    this.current = null;
                    return false;
                }

                this.current = node;
                if (node.IsSingleNode)
                {
                    this.next = null;
                }
                else
                {
                    var listNext = node.ListNext!;
                    this.next = listNext.IsLinkedListNode ? listNext : null;
                }

                return true;
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

                this.current = null;
                this.next = this.map.FindFirstNode(this.key);
            }
        }
    }

    /// <summary>
    /// Enumerates the values of a duplicate-key group.
    /// </summary>
    public readonly struct MatchedValueEnumerable : IEnumerable<TValue>
    {
        private readonly OrderedMultiMap<TKey, TValue> map;
        private readonly TKey? key;

        internal MatchedValueEnumerable(OrderedMultiMap<TKey, TValue> map, TKey? key)
        {
            this.map = map;
            this.key = key;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map, this.key);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(this.map, this.key);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map, this.key);

        /// <summary>
        /// Enumerates the elements of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private NodeEnumerable.Enumerator enumerator;

            internal Enumerator(OrderedMultiMap<TKey, TValue> map, TKey? key)
            {
                this.enumerator = new NodeEnumerable.Enumerator(map, key);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.enumerator.Current.Value;
            }

            object? IEnumerator.Current
                => ((IEnumerator)this.enumerator).Current is Node node
                    ? node.Value
                    : default;

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
                => ((IEnumerator)this.enumerator).Reset();
        }
    }

    #endregion

    #region Enumerator

    /// <summary>
    /// Returns an allocation-free enumerator.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<KeyValuePair<TKey, TValue>>
        IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly OrderedMultiMap<TKey, TValue> map;
        private readonly int version;
        private Node? current;
        private Node? next;

        internal Enumerator(OrderedMultiMap<TKey, TValue> map)
        {
            this.map = map;
            this.version = map.version;
            this.current = null;
            this.next = GetFirst(map.root);
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
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

            var node = this.next;
            if (node is null)
            {
                this.current = null;
                return false;
            }

            this.current = node;
            this.next = GetNextNode(node);
            return true;
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

    /// <summary>
    /// Provides an allocation-free enumerable over the keys of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
    /// </summary>
    public readonly struct KeyEnumerable : IEnumerable<TKey>
    {
        private readonly OrderedMultiMap<TKey, TValue> map;

        internal KeyEnumerable(OrderedMultiMap<TKey, TValue> map)
        {
            this.map = map;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        /// <summary>
        /// Enumerates the elements of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly OrderedMultiMap<TKey, TValue> map;
            private readonly int version;
            private Node? current;
            private Node? next;

            internal Enumerator(OrderedMultiMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.current = null;
                this.next = GetFirst(map.root);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
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

                var node = this.next;
                if (node is null)
                {
                    this.current = null;
                    return false;
                }

                this.current = node;
                this.next = GetNextNode(node);
                return true;
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

    /// <summary>
    /// Provides an allocation-free enumerable over the values of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
    /// </summary>
    public readonly struct ValueEnumerable : IEnumerable<TValue>
    {
        private readonly OrderedMultiMap<TKey, TValue> map;

        internal ValueEnumerable(OrderedMultiMap<TKey, TValue> map)
        {
            this.map = map;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new(this.map);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new Enumerator(this.map);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this.map);

        /// <summary>
        /// Enumerates the elements of a <see cref="OrderedMultiMap{TKey, TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly OrderedMultiMap<TKey, TValue> map;
            private readonly int version;
            private Node? current;
            private Node? next;

            internal Enumerator(OrderedMultiMap<TKey, TValue> map)
            {
                this.map = map;
                this.version = map.version;
                this.current = null;
                this.next = GetFirst(map.root);
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
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

                var node = this.next;
                if (node is null)
                {
                    this.current = null;
                    return false;
                }

                this.current = node;
                this.next = GetNextNode(node);
                return true;
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
        var (cmp, parent) = this.SearchFirstNode(this.root, key);
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

        this.version++;
        this.count++;

        if (cmp == 0 && parent is not null)
        {
            node.Color = NodeColor.LinkedList;
            if (parent.IsSingleNode)
            {
                parent.ListPrevious = node;
                parent.ListNext = node;
                node.ListPrevious = parent;
                node.ListNext = parent;
            }
            else
            {
                node.ListPrevious = parent.ListPrevious;
                node.ListNext = parent;
                parent.ListPrevious!.ListNext = node;
                parent.ListPrevious = node;
            }

            return (node, true);
        }

        node.Parent = parent;
        if (parent is null)
        {
            this.root = node;
            node.ColorBlack();
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

        var current = node;
#nullable disable
        while (current.Parent is not null && current.Parent.IsRed)
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
    /// Validates the Red-Black Tree and the duplicate-group linked lists.
    /// </summary>
    /// <returns><see langword="true"/> if the internal structure is valid; otherwise, <see langword="false"/>.</returns>
    public bool Validate()
    {
        if (this.root is null)
        {
            return this.count == 0;
        }

        if (!this.root.IsBlack || this.root.Parent is not null)
        {
            return false;
        }

        Node? previous = null;
        var actualCount = 0;
        if (!this.ValidateNode(this.root, ref previous, ref actualCount))
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

    private bool ValidateNode(Node? node, ref Node? previous, ref int actualCount)
    {
        if (node is null)
        {
            return true;
        }

        if (node.IsLinkedListNode || node.IsUnused)
        {
            return false;
        }

        if (node.Left is not null && !ReferenceEquals(node.Left.Parent, node))
        {
            return false;
        }

        if (node.Right is not null && !ReferenceEquals(node.Right.Parent, node))
        {
            return false;
        }

        if (!this.ValidateNode(node.Left, ref previous, ref actualCount))
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

        // Duplicate-group invariants: a circular list of LinkedList-colored nodes
        // holding the same key, consistently linked in both directions.
        if (!node.IsSingleNode)
        {
            var p = node;
            var q = node.ListNext;
            while (true)
            {
                if (q is null || !ReferenceEquals(q.ListPrevious, p))
                {
                    return false;
                }

                if (ReferenceEquals(q, node))
                {
                    break;
                }

                if (!q.IsLinkedListNode ||
                    this.CompareInTreeOrder(q.Key, node.Key) != 0)
                {
                    return false;
                }

                actualCount++;
                if (actualCount > this.count)
                {
                    return false;
                }

                p = q;
                q = q.ListNext;
            }
        }

        return this.ValidateNode(node.Right, ref previous, ref actualCount);
    }

    private static bool ValidateColors(Node? node)
    {
        if (node is null)
        {
            return true;
        }

        if (node.IsRed &&
            (Node.IsNonNullRed(node.Left) || Node.IsNonNullRed(node.Right)))
        {
            return false;
        }

        return ValidateColors(node.Left) && ValidateColors(node.Right);
    }

    private static int ValidateBlackHeight(Node? node)
    {
        if (node is null)
        {
            return 0;
        }

        var left = ValidateBlackHeight(node.Left);
        var right = ValidateBlackHeight(node.Right);
        if (left < 0 || right < 0 || left != right)
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
    private static Node? GetLastTreeNode(Node? node)
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

    private static Node? GetNextNode(Node node)
    {
        if (!node.IsSingleNode)
        {
            var next = node.ListNext!;
            if (!node.IsLinkedListNode ||
                next.IsLinkedListNode)
            {
                return next;
            }

            // The last duplicate points back to the tree head.
            node = next;
        }

        return GetNextTreeNode(node);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Node? GetNextTreeNode(Node node)
    {
        if (node.Right is not null)
        {
            node = node.Right;
            while (node.Left is not null)
            {
                node = node.Left;
            }

            return node;
        }

        var current = node;
        var parent = node.Parent;
        while (parent is not null &&
               ReferenceEquals(current, parent.Right))
        {
            current = parent;
            parent = parent.Parent;
        }

        return parent;
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
            cmp = this.comparer.Compare(x, y);
        }

        if (!this.Reverse)
        {
            return cmp;
        }

        return cmp < 0
            ? 1
            : cmp > 0
                ? -1
                : 0;
    }

    // Red-Black tree depth is O(log n), so recursion depth remains small.
    private static void ClearTree(Node? node)
    {
        if (node is null)
        {
            return;
        }

        ClearTree(node.Left);
        ClearTree(node.Right);
        if (!node.IsSingleNode)
        {
            var listNode = node.ListNext!;
            while (!ReferenceEquals(listNode, node))
            {
                var next = listNode.ListNext!;
                listNode.Clear();
                listNode = next;
            }
        }

        node.Clear();
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
