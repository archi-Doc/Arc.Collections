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
/// The color of a red-black tree node, or the state of a node outside the tree (unused, or a duplicate in a linked list).
/// </summary>
internal enum NodeColor : byte
{
    Black,
    Red,
    Unused,
    LinkedList,
}

/// <summary>
/// Stores unique keys and their values in a red-black tree ordered by a comparer.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
/// <remarks>
/// Lookup, insertion, and removal take O(log n) time. Null keys are supported: the default comparer
/// orders null first (last when the map is reversed), and a custom comparer receives null keys and defines their order.
/// With the default comparer, a non-null key whose runtime type implements <see cref="IComparable{T}"/>
/// of <typeparamref name="TKey"/> is compared through that interface, so keys typed as an interface or base class work.
/// Adding an existing key preserves its value; the indexer setter replaces it.
/// Active nodes passed to node operations must belong to this map; ownership is not checked.
/// Advancing an enumerator after the map is modified throws <see cref="InvalidOperationException"/>;
/// <see cref="Node.UnsafeChangeValue(TValue)"/> does not count as a modification.
/// </remarks>
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

        internal NodeColor Color { get; set; }

        /// <summary>
        /// Gets the previous node in sort order, or <see langword="null"/> if this is the first node or the node is unused.
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
        /// Gets the next node in sort order, or <see langword="null"/> if this is the last node or the node is unused.
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
        /// Changes the value without checking whether the node is in use. Outstanding enumerators stay valid.
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

        /// <summary>
        /// Gets a value indicating whether the node has been removed from the map and can be reused.
        /// </summary>
        public bool IsUnused => this.Color == NodeColor.Unused;

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

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => this.count;

    /// <summary>
    /// Gets a value indicating whether the collection is sorted in reverse order.
    /// </summary>
    public bool IsReversed { get; }

    /// <summary>
    /// Gets the comparer used to order the keys.
    /// </summary>
    /// <remarks>
    /// With the default comparer, a key whose runtime type implements <c>IComparable&lt;TKey&gt;</c> is compared through
    /// that interface even when <typeparamref name="TKey"/> itself does not implement it (for example, <see cref="object"/>
    /// or an interface); calling this comparer directly on such keys may order them differently or throw.
    /// </remarks>
    public IComparer<TKey> Comparer { get; }

    /// <summary>
    /// Gets the specialized tree-search implementation for <typeparamref name="TKey"/>, or <see langword="null"/>
    /// when none is available. One is resolved only when <see cref="Comparer"/> is <see cref="Comparer{T}.Default"/>.
    /// </summary>
    public IHotTreeMethod<TKey, TValue>? HotTreeMethod { get; }

    /// <summary>
    /// Initializes an empty map.
    /// </summary>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedMap(bool reverse = false)
        : this(Comparer<TKey>.Default, reverse)
    {
    }

    /// <summary>
    /// Initializes an empty map with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    public OrderedMap(IComparer<TKey>? comparer, bool reverse = false)
    {
        this.IsReversed = reverse;
        this.Comparer = comparer ?? Comparer<TKey>.Default;
        this.HotTreeMethod = HotMethodResolver.Get<TKey, TValue>(this.Comparer);
    }

    /// <summary>
    /// Initializes a map from the specified sequence. For duplicate keys, the first value is kept.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied.</param>
    /// <param name="comparer">The comparer to use, or <see langword="null"/> for the default comparer.</param>
    /// <param name="reverse"><see langword="true"/> to sort in descending order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
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
    /// Gets the first node in sort order, or <see langword="null"/> if the map is empty.
    /// </summary>
    public Node? First
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetFirst(this.root);
    }

    /// <summary>
    /// Gets the last node in sort order, or <see langword="null"/> if the map is empty.
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

    /// <summary>
    /// Gets or sets the value associated with the specified key. Setting a missing key adds it.
    /// </summary>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value of the node with <paramref name="key"/>.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist (getter only).</exception>
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

    /// <summary>
    /// Determines whether the collection contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey? key)
        => this.FindNode(key) is not null;

    /// <summary>
    /// Determines whether the collection contains the specified value, using <see cref="EqualityComparer{T}.Default"/>.
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
    /// <param name="value">When this method returns, the value of the matching node; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
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
    /// Removes all elements. Their nodes become unused and can be reused with <see cref="Add(TKey, TValue, Node)"/>.
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
    /// Copies the key-value pairs to an array, in sort order.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based destination index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the array length.</exception>
    /// <exception cref="ArgumentException">The array is too small to hold the elements from <paramref name="index"/>.</exception>
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
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
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
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value)
        => this.Probe(key, value, null);

    /// <summary>
    /// Adds an element if the key does not already exist, optionally reusing an unused node.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="reuse">A node to hold the new element if it is unused; otherwise, a new node is allocated.
    /// It is left untouched when the key already exists.</param>
    /// <returns>The node holding the element, and a flag that is <see langword="true"/> when the node was newly added.</returns>
    public (Node Node, bool NewlyAdded) Add(TKey key, TValue value, Node reuse)
        => this.Probe(key, value, reuse);

    /// <summary>
    /// Changes the key of an active node, keeping the same node object and value.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="key">The new key.</param>
    /// <returns><see langword="true"/> if the key was changed; <see langword="false"/> if the node is unused,
    /// <paramref name="key"/> compares equal to the current key (the stored key is kept),
    /// or another node already has <paramref name="key"/>.</returns>
    /// <remarks>If the comparer throws, the map is unchanged.</remarks>
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

        // Finish all comparisons before removing the node: even a comparer that keeps
        // throwing must not prevent the original entry from being preserved.
        var (position, parent) = this.SearchNode(this.root, key);
        if (position == 0 && parent is not null)
        {
            return false;
        }

        var successor = position < 0 ? parent : parent?.Next;
        var value = node.Value;
        this.RemoveNode(node);

        // Rotations may change the insertion parent, but not the next key in sort order.
        // The gap is its empty left child, or the right child of its predecessor.
        if (successor is not null && successor.Left is null)
        {
            this.Link(-1, successor, key, value, node);
        }
        else
        {
            this.Link(1, GetLast(successor is null ? this.root : successor.Left), key, value, node);
        }

        return true;
    }

    /// <summary>
    /// Updates the value of an active node. An unused node is ignored.
    /// </summary>
    /// <remarks>This increments the version and therefore invalidates outstanding enumerators.
    /// Use <see cref="Node.UnsafeChangeValue(TValue)"/> to change a value without doing so.</remarks>
    /// <param name="node">The node.</param>
    /// <param name="value">The value.</param>
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
    /// Removes the specified node. An unused node is ignored.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <remarks>The removed node becomes unused and can be reused with <see cref="Add(TKey, TValue, Node)"/>.</remarks>
    [Leash.Exempt("Keep node replacement, rotations, and recoloring together in the deletion hot path to avoid additional helper calls and state passing.")]
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

        // The root stays black without a final recolor: a red node is never the root, the node that
        // takes the removed node's place is either recolored black or inherits its color, the fixup loop
        // below blackens the root when it reaches it, and any node the loop rotates into the root's place is black.
        if (originalColor == NodeColor.Red)
        {
            node.Clear();
            return;
        }

        // A non-null replacement of a removed black node is red in a valid red-black tree;
        // recoloring it black restores the black height.
        if (replacement is not null)
        {
            replacement.ColorBlack();
            node.Clear();
            return;
        }

        if (fixParent is null)
        {
            // The removed node was the only node, so the tree is now empty.
            node.Clear();
            return;
        }

        var parent = fixParent;
        while (true)
        {
            // The sibling is never null: its subtree's black height exceeds the deficit side's by one,
            // so it is at least one. After the red-sibling rotation, the new sibling is a former child
            // of the red sibling and has the same black height.
            Node sibling;
            if (direction < 0)
            {
                sibling = parent.Right!;
                if (sibling.IsRed)
                {
                    sibling.ColorBlack();
                    parent.ColorRed();
                    this.RotateLeft(parent);
                    sibling = parent.Right!;
                }

                if (Node.IsNullOrBlack(sibling.Left) &&
                    Node.IsNullOrBlack(sibling.Right))
                {
                    // Propagate the black-height deficit.
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
                        sibling = parent.Right!;
                    }

                    sibling.Color = parent.Color;
                    parent.ColorBlack();
                    sibling.Right!.ColorBlack();
                    this.RotateLeft(parent);
                    break;
                }
            }
            else
            {
                sibling = parent.Left!;
                if (sibling.IsRed)
                {
                    sibling.ColorBlack();
                    parent.ColorRed();
                    this.RotateRight(parent);
                    sibling = parent.Left!;
                }

                if (Node.IsNullOrBlack(sibling.Left) &&
                    Node.IsNullOrBlack(sibling.Right))
                {
                    // Propagate the black-height deficit.
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
                        sibling = parent.Left!;
                    }

                    sibling.Color = parent.Color;
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
    }

    #region Search

    /// <summary>
    /// Searches for a node with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The matching node, or <see langword="null"/> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Node? FindNode(TKey? key)
    {
        var result = this.SearchNode(this.root, key);
        return result.Comparison == 0
            ? result.Node
            : null;
    }

    /// <summary>
    /// Gets the first node equal to or after the specified key in sort order.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The first node at or after <paramref name="key"/> in sort order, or <see langword="null"/> if none exists.</returns>
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
    /// Gets the last node equal to or before the specified key in sort order.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The last node at or before <paramref name="key"/> in sort order, or <see langword="null"/> if none exists.</returns>
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
    /// Gets the nodes delimiting the inclusive range between two keys in sort order.
    /// </summary>
    /// <param name="lower">The bound that comes first in sort order (the larger key when the map is reversed).</param>
    /// <param name="upper">The bound that comes last in sort order (the smaller key when the map is reversed).</param>
    /// <returns>The first node at or after <paramref name="lower"/> and the last node at or before
    /// <paramref name="upper"/> in sort order, or <c>(null, null)</c> if the range is empty.</returns>
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

    [Leash.Exempt("Keep null, primitive, value-type, and comparable search fast paths together for both sort directions to preserve JIT optimization opportunities.")]
    private (int Comparison, Node? Node) SearchNode(Node? target, TKey? key)
    {
        var node = target;
        Node? parent = null;
        var cmp = 0;
        var comparer = this.Comparer;
        var hotMethod = this.HotTreeMethod;

        if (!this.IsReversed)
        {
            // Comparer<TKey>.Default orders null before every other key; a custom comparer
            // defines its own null order, so null keys go through it (last loop below).
            // This also runs before HotMethod, which is only defined for non-null value keys.
            if (key is null)
            {
                if (ReferenceEquals(comparer, Comparer<TKey>.Default))
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
            }
            else if (hotMethod is not null)
            {
                return hotMethod.SearchNode(node, key);
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
                    // Comparer<TKey>.Default never passes null to CompareTo; a null key sorts first.
                    cmp = node.Key is null ? 1 : comparable.CompareTo(node.Key);
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
            // See the forward direction for the null handling.
            if (key is null)
            {
                if (ReferenceEquals(comparer, Comparer<TKey>.Default))
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
            }
            else if (hotMethod is not null)
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

            if (ReferenceEquals(comparer, Comparer<TKey>.Default) &&
                key is IComparable<TKey> comparable)
            {
                while (node is not null)
                {
                    var c = node.Key is null ? 1 : comparable.CompareTo(node.Key);
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
    /// Enumerates the key-value pairs of an <see cref="OrderedMap{TKey, TValue}"/> in sort order.
    /// </summary>
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
            this.next = node.Next;
            return true;
        }

        /// <summary>
        /// Clears the current node and ends enumeration.
        /// </summary>
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

    /// <summary>
    /// Provides a struct enumerable over the keys in sort order.
    /// </summary>
    /// <remarks>
    /// Direct iteration avoids boxing; enumeration through an interface may allocate.
    /// </remarks>
    public readonly struct KeyEnumerable : IEnumerable<TKey>
    {
        private readonly OrderedMap<TKey, TValue> map;

        internal KeyEnumerable(OrderedMap<TKey, TValue> map)
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
        /// Enumerates keys in sort order.
        /// </summary>
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
                this.next = node.Next;
                return true;
            }

            /// <summary>
            /// Clears the current node and ends enumeration.
            /// </summary>
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

            internal readonly void ValidateCurrent()
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
    /// Provides a struct enumerable over the values in sort order.
    /// </summary>
    /// <remarks>
    /// Direct iteration avoids boxing; enumeration through an interface may allocate.
    /// </remarks>
    public readonly struct ValueEnumerable : IEnumerable<TValue>
    {
        private readonly OrderedMap<TKey, TValue> map;

        internal ValueEnumerable(OrderedMap<TKey, TValue> map)
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
        /// Enumerates values in sort order.
        /// </summary>
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
                this.next = node.Next;
                return true;
            }

            /// <summary>
            /// Clears the current node and ends enumeration.
            /// </summary>
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

        return this.Link(cmp, parent, key, value, reuse);
    }

    /// <summary>
    /// Inserts a node at a known empty child position without comparing keys.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Leash.Exempt("Keep node reuse, insertion, and red-black balancing in one insertion hot path to avoid additional helper calls and state passing.")]
    private (Node Node, bool NewlyAdded) Link(int cmp, Node? parent, TKey key, TValue value, Node? reuse)
    {
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
    /// Checks the red-black tree invariants, node links, key order, and count. O(n); intended for diagnostics.
    /// </summary>
    /// <returns><see langword="true"/> if the internal structure is valid; otherwise, <see langword="false"/>.</returns>
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
        if (!typeof(TKey).IsValueType &&
            ReferenceEquals(this.Comparer, Comparer<TKey>.Default) &&
            x is IComparable<TKey> comparable)
        {
            // Compare like SearchNode: with the default comparer, a key's own IComparable<TKey> decides
            // the order even when TKey does not implement it (e.g., object), where Comparer<TKey>.Default
            // would use the non-generic IComparable (possibly ordering differently) or throw.
            cmp = y is null ? 1 : comparable.CompareTo(y);
        }
        else
        {
            // Comparer<TKey>.Default orders null first; a custom comparer defines its own null order.
            cmp = this.Comparer.Compare(x, y);
        }

        if (!this.IsReversed)
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
