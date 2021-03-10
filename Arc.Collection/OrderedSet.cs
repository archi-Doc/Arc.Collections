// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collection
{
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
    /// Represents a collection of objects that is maintained in sorted order. <see cref="OrderedSet{T}"/> uses Red-Black Tree structure to store objects.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class OrderedSet<T> : IEnumerable<T>, IEnumerable, ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        /// <summary>
        /// Represents a node in a <see cref="OrderedSet{T}"/>.
        /// </summary>
        public sealed class Node
        {
            internal Node(T value, NodeColor color)
            {
                this.Value = value;
                this.Color = color;
            }

            /// <summary>
            /// Gets the previous node in the <see cref="OrderedSet{T}"/>.
            /// <br/>O(log n) operation.
            /// </summary>
            public Node? Previous
            {
                get
                {
                    Node? node;
                    if (this.Left == null)
                    {
                        node = this;
                        Node? p = this.Parent;
                        while (p != null && node == p.Left)
                        {
                            node = p;
                            p = p.Parent;
                        }

                        return p;
                    }
                    else
                    {
                        node = this.Left;
                        while (node.Right != null)
                        {
                            node = node.Right;
                        }

                        return node;
                    }
                }
            }

            /// <summary>
            /// Gets the next node in the <see cref="OrderedSet{T}"/>
            /// <br/>O(log n) operation.
            /// </summary>
            public Node? Next
            {
                get
                {
                    Node? node;
                    if (this.Right == null)
                    {
                        node = this;
                        Node? p = this.Parent;
                        while (p != null && node == p.Right)
                        {
                            node = p;
                            p = p.Parent;
                        }

                        return p;
                    }
                    else
                    {
                        node = this.Right;
                        while (node.Left != null)
                        {
                            node = node.Left;
                        }

                        return node;
                    }
                }
            }

            internal static bool IsNonNullBlack(Node? node) => node != null && node.IsBlack;

            internal static bool IsNonNullRed(Node? node) => node != null && node.IsRed;

            internal static bool IsNullOrBlack(Node? node) => node == null || node.IsBlack;

            /// <summary>
            /// Gets the value contained in the node.
            /// </summary>
            public T Value { get; internal set; }

            /// <summary>
            /// Gets or sets the parent node in the <see cref="OrderedSet{T}"/>.
            /// </summary>
            internal Node? Parent { get; set; }

            /// <summary>
            /// Gets or sets the left node in the <see cref="OrderedSet{T}"/>.
            /// </summary>
            internal Node? Left { get; set; }

            /// <summary>
            /// Gets or sets the right node in the <see cref="OrderedSet{T}"/>.
            /// </summary>
            internal Node? Right { get; set; }

            /// <summary>
            /// Gets or sets the color of the node.
            /// </summary>
            internal NodeColor Color { get; set; }

            internal bool IsBlack => this.Color == NodeColor.Black;

            internal bool IsRed => this.Color == NodeColor.Red;

            internal bool IsUnused => this.Color == NodeColor.Unused;

            public override string ToString() => this.Color.ToString() + ": " + this.Value?.ToString();

            internal void Clear()
            {
                this.Value = default(T)!;
                this.Parent = null;
                this.Left = null;
                this.Right = null;
                this.Color = NodeColor.Unused;
            }

            internal void Reset(T value, NodeColor color)
            {
                this.Value = value;
                this.Parent = null;
                this.Left = null;
                this.Right = null;
                this.Color = color;
            }

            internal void ColorBlack() => this.Color = NodeColor.Black;

            internal void ColorRed() => this.Color = NodeColor.Red;
        }

        private Node? root;
        private int version;

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public int Count { get; private set; }

        public IComparer<T> Comparer { get; private set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        public OrderedSet()
        {
            this.Comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedSet(IComparer<T>? comparer)
        {
            this.Comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        public OrderedSet(IEnumerable<T> collection)
        {
            this.Comparer = Comparer<T>.Default;

            foreach (var x in collection)
            {
                this.Add(x);
            }
        }

        #region Enumerator

        /// <summary>
        /// Returns an Enumerator for the <see cref="OrderedSet{T}"/>.
        /// </summary>
        /// <returns>Enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Enumerates the elements of a <see cref="OrderedSet{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly OrderedSet<T> set;
            private readonly int version;
            private readonly bool reverse;
            private bool firstFlag;
            private Node? current;

            internal Enumerator(OrderedSet<T> set)
                : this(set, reverse: false)
            {
            }

            internal Enumerator(OrderedSet<T> set, bool reverse)
            {
                this.set = set;
                this.version = set.version;
                this.reverse = reverse;
                this.firstFlag = false;
                this.current = null;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (this.version != this.set.version)
                {
                    throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.'");
                }

                if (!this.firstFlag)
                {
                    this.firstFlag = true;
                    this.current = this.set.First;
                }
                else if (this.current != null)
                {
                    this.current = this.current.Next;
                }

                return this.current != null;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }

            /// <inheritdoc/>
            public T Current => this.current != null ? this.current.Value : default(T)!;

            /// <inheritdoc/>
            object? System.Collections.IEnumerator.Current => this.current != null ? this.current.Value : default(T)!;

            /// <inheritdoc/>
            void System.Collections.IEnumerator.Reset() => this.Reset();

            internal void Reset()
            {
                if (this.version != this.set.version)
                {
                    throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.'");
                }

                this.firstFlag = false;
                this.current = null;
            }
        }
        #endregion

        /// <summary>
        /// Gets the first node in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public Node? First
        {
            get
            {
                if (this.root == null)
                {
                    return null;
                }

                var node = this.root;
                while (node.Left != null)
                {
                    node = node.Left;
                }

                return node;
            }
        }

        /// <summary>
        /// Gets the last node in the <see cref="OrderedSet{T}"/>. O(log n) operation.
        /// </summary>
        public Node? Last
        {
            get
            {
                if (this.root == null)
                {
                    return null;
                }

                var node = this.root;
                while (node.Right != null)
                {
                    node = node.Right;
                }

                return node;
            }
        }

        #region ICollection

        public bool IsReadOnly => false;

        void ICollection<T>.Add(T item) => this.Add(item);

        public bool Contains(T item) => this.FindNode(item) != null;

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            else if (this.Count > array.Length - index)
            {
                throw new ArgumentOutOfRangeException(nameof(array));
            }

            var node = this.First;
            for (var n = index; n < (index + this.Count); n++)
            {
                if (node == null)
                {
                    break;
                }

                array[n] = node.Value;
                node = node.Next;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            else if (array.Rank != 1 || array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(nameof(array));
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            else if (array.Length - index < this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(array));
            }

            T[]? tarray = array as T[];
            if (tarray != null)
            {
                this.CopyTo(tarray, index);
            }
            else
            {
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(nameof(array));
                }

                try
                {
                    var node = this.First;
                    for (var n = index; n < (index + this.Count); n++)
                    {
                        if (node == null)
                        {
                            break;
                        }

                        objects[n] = node.Value;
                        node = node.Next;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }
        }

        public bool IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        #region Validate

        /// <summary>
        /// Validate Red-Black Tree.
        /// </summary>
        /// <returns>True if the tree is valid.</returns>
        public bool Validate()
        {
            bool result = true;
            result &= this.ValidateBST(this.root);
            result &= this.ValidateBlackHeight(this.root) >= 0;
            result &= this.ValidateColor(this.root) == NodeColor.Black;

            return result;
        }

        /// <summary>
        /// Removes all elements from the set.
        /// </summary>
        public void Clear()
        {
            this.root = null;
            this.version = 0;
            this.Count = 0;
        }

        /// <summary>
        /// Adds an element to the set. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to add to the set.</param>
        /// <returns>node: the added <see cref="OrderedSet{T}.Node"/>.<br/>
        /// newlyAdded: True if the node is created.</returns>
        public (Node node, bool newlyAdded) Add(T value) => this.Probe(value, null);

        /// <summary>
        /// Adds an element to the set. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to add to the set.</param>
        /// <param name="reuse">Reuse a node to avoid memory allocation.</param>
        /// <returns>node: the added <see cref="OrderedSet{T}.Node"/>.<br/>
        /// newlyAdded: True if the node is created.</returns>
        public (Node node, bool newlyAdded) Add(T value, Node reuse) => this.Probe(value, reuse);

        /// <summary>
        /// Adds an element to the set. If the element is already in the set, this method replaces the stored element with the new element and sets the replaced flag to true.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to add to the set.</param>
        /// <returns>node: the added <see cref="OrderedSet{T}.Node"/>.<br/>
        /// replaced: True if the node is replaced.</returns>
        public (Node node, bool replaced) Replace(T value)
        {
            var result = this.Probe(value, null);
            if (result.newlyAdded)
            {// New
                return (result.node, false);
            }

            // Replace
            this.version++;
            result.node.Value = value;
            return (result.node, true);
        }

        /// <summary>
        /// Removes a specified item from the <see cref="OrderedSet{T}"/>.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to remove.</param>
        /// <returns>true if the element is found and successfully removed.</returns>
        public bool Remove(T value)
        {
            Node? p; // Node to delete.
            int cmp = -1;

            if (this.root == null)
            {// No root
                return false;
            }

            p = this.root;
            while (true)
            {
                cmp = this.Comparer.Compare(value, p.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                if (cmp < 0)
                {
                    p = p.Left;
                }
                else if (cmp > 0)
                {
                    p = p.Right;
                }
                else
                {
                    break;
                }

                if (p == null)
                {// Not found
                    return false;
                }
            }

            this.RemoveNode(p);
            return true;
        }

        /// <summary>
        /// Removes a specified node from the <see cref="OrderedSet{T}"/>.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="node">The <see cref="OrderedSet{T}.Node"/> to remove.</param>
        public void RemoveNode(Node node)
        {
            Node? f; // Node to fix.
            int dir = 0;

            var originalColor = node.Color;
            if (node.Color == NodeColor.Unused)
            {// empty
                return;
            }

            f = node.Parent;
            if (node.Parent == null)
            {
                dir = 0;
            }
            else if (node.Parent.Left == node)
            {
                dir = -1;
            }
            else if (node.Parent.Right == node)
            {
                dir = 1;
            }

            this.version++;
            this.Count--;

            if (node.Left == null)
            {
                this.TransplantNode(node.Right, node);
            }
            else if (node.Right == null)
            {
                this.TransplantNode(node.Left, node);
            }
            else
            {
                // Minimum
                Node? m = node.Right;
                while (m.Left != null)
                {
                    m = m.Left;
                }

                originalColor = m.Color;
                if (m.Parent == node)
                {
                    f = m;
                    dir = 1;
                }
                else
                {
                    f = m.Parent;
                    dir = -1;

                    this.TransplantNode(m.Right, m);
                    m.Right = node.Right;
                    m.Right.Parent = m;
                }

                this.TransplantNode(m, node);
                m.Left = node.Left;
                m.Left.Parent = m;
                m.Color = node.Color;
            }

            if (originalColor == NodeColor.Red || f == null)
            {
                node.Clear();
                if (this.root != null)
                {
                    this.root.ColorBlack();
                }

                return;
            }

            Node? s;
            while (true)
            {
                if (dir < 0)
                {
                    s = f.Right;
                    if (Node.IsNonNullRed(s))
                    {
                        s!.ColorBlack();
                        f.ColorRed();
                        this.RotateLeft(f);
                        s = f.Right;
                    }

                    // s is null or black
                    if (s == null)
                    {
                        // loop
                    }
                    else if (Node.IsNullOrBlack(s.Left) && Node.IsNullOrBlack(s.Right))
                    {
                        s.ColorRed();
                        // loop
                    }
                    else
                    {// s is black and one of children is red.
                        if (Node.IsNonNullRed(s.Left))
                        {
                            s.Left!.ColorBlack();
                            s.ColorRed();
                            this.RotateRight(s);
                            s = f.Right;
                        }

                        s!.Color = f.Color;
                        f.ColorBlack();
                        s.Right!.ColorBlack();
                        this.RotateLeft(f);
                        break;
                    }
                }
                else
                {
                    s = f.Left;
                    if (Node.IsNonNullRed(s))
                    {
                        s!.ColorBlack();
                        f.ColorRed();
                        this.RotateRight(f);
                        s = f.Left;
                    }

                    // s is null or black
                    if (s == null)
                    {
                        // loop
                    }
                    else if (Node.IsNullOrBlack(s.Left) && Node.IsNullOrBlack(s.Right))
                    {
                        s.ColorRed();
                        // loop
                    }
                    else
                    {// s is black and one of children is red.
                        if (Node.IsNonNullRed(s.Right))
                        {
                            s.Right!.ColorBlack();
                            s.ColorRed();
                            this.RotateLeft(s);
                            s = f.Left;
                        }

                        s!.Color = f.Color;
                        f.ColorBlack();
                        s.Left!.ColorBlack();
                        this.RotateRight(f);
                        break;
                    }
                }

                if (f.IsRed || f.Parent == null)
                {
                    f.ColorBlack();
                    break;
                }

                if (f == f.Parent.Left)
                {
                    dir = -1;
                }
                else
                {
                    dir = 1;
                }

                f = f.Parent;
            }

            node.Clear();
            return;
        }

        public Node? FindNode(T value)
        {
            var p = this.root;
            while (p != null)
            {
                var cmp = this.Comparer.Compare(value, p.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                if (cmp < 0)
                {
                    p = p.Left;
                }
                else if (cmp > 0)
                {
                    p = p.Right;
                }
                else
                {// Found
                    return p;
                }
            }

            // Not found
            return null;
        }

        /// <summary>
        /// Adds an element to the set. If the element is already in the set, this method returns the stored node without creating a new node.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to add to the set.</param>
        /// <returns>node: the added <see cref="OrderedSet{T}.Node"/>.<br/>
        /// newlyAdded: True if the node is created.</returns>
        private (Node node, bool newlyAdded) Probe(T value, Node? reuse)
        {
            Node? x = this.root; // Traverses tree looking for insertion point.
            Node? p = null; // Parent of x; node at which we are rebalancing.
            int cmp = 0;

            while (x != null)
            {
                cmp = this.Comparer.Compare(value, x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {
                    return (x, false);
                }
            }

            this.version++;
            this.Count++;

            Node n;
            if (reuse != null && reuse.IsUnused)
            {
                reuse.Reset(value, NodeColor.Red);
                n = reuse;
            }
            else
            {
                n = new Node(value, NodeColor.Red); // Newly inserted node.
            }

            n.Parent = p;
            n.Value = value;
            if (p != null)
            {
                if (cmp < 0)
                {
                    p.Left = n;
                }
                else
                {
                    p.Right = n;
                }
            }
            else
            {// Root
                this.root = n;
                n.ColorBlack();
                return (n, true);
            }

            p = n;

#nullable disable
            while (p.Parent != null && p.Parent.IsRed)
            {// p.Parent is not root (root is black), so p.Parent.Parent != null
                if (p.Parent == p.Parent.Parent.Right)
                {
                    x = p.Parent.Parent.Left; // uncle
                    if (x != null && x.IsRed)
                    {
                        x.ColorBlack();
                        p.Parent.ColorBlack();
                        p.Parent.Parent.ColorRed();
                        p = p.Parent.Parent; // loop
                    }
                    else
                    {
                        if (p == p.Parent.Left)
                        {
                            p = p.Parent;
                            this.RotateRight(p);
                        }

                        p.Parent.ColorBlack();
                        p.Parent.Parent.ColorRed();
                        this.RotateLeft(p.Parent.Parent);
                        break;
                    }
                }
                else
                {
                    x = p.Parent.Parent.Right; // uncle

                    if (x != null && x.IsRed)
                    {
                        x.ColorBlack();
                        p.Parent.ColorBlack();
                        p.Parent.Parent.ColorRed();
                        p = p.Parent.Parent; // loop
                    }
                    else
                    {
                        if (p == p.Parent.Right)
                        {
                            p = p.Parent;
                            this.RotateLeft(p);
                        }

                        p.Parent.ColorBlack();
                        p.Parent.Parent.ColorRed();
                        this.RotateRight(p.Parent.Parent);
                        break;
                    }
                }
            }
#nullable enable

            this.root!.ColorBlack();
            return (n, true);
        }

        private NodeColor ValidateColor(Node? node)
        {
            if (node == null)
            {
                return NodeColor.Black;
            }

            var color = node.Color;
            var leftColor = this.ValidateColor(node.Left);
            var rightColor = this.ValidateColor(node.Right);
            if (leftColor == NodeColor.Unused || rightColor == NodeColor.Unused)
            { // Error
                return NodeColor.Unused;
            }

            if (color == NodeColor.Black)
            {
                return color;
            }
            else if (color == NodeColor.Red && leftColor == NodeColor.Black && rightColor == NodeColor.Black)
            {
                return color;
            }

            return NodeColor.Unused; // Error
        }

        private int ValidateBlackHeight(Node? node)
        {
            if (node == null)
            {
                return 0;
            }

            int leftHeight = this.ValidateBlackHeight(node.Left);
            int rightHeight = this.ValidateBlackHeight(node.Right);
            if (leftHeight < 0 || rightHeight < 0 || leftHeight != rightHeight)
            {// Invalid
                return -1;
            }

            return leftHeight + (node.IsBlack ? 1 : 0);
        }

        private bool ValidateBST(Node? node)
        {// Binary Search Tree
            if (node == null)
            {
                return true;
            }

            bool result = true;

            if (node.Parent == null)
            {
                result &= this.root == node;
            }

            if (node.Left != null)
            {
                result &= node.Left.Parent == node;
            }

            if (node.Right != null)
            {
                result &= node.Right.Parent == node;
            }

            result &= this.IsSmaller(node.Left, node.Value) && this.IsLarger(node.Right, node.Value);
            result &= this.ValidateBST(node.Left) && this.ValidateBST(node.Right);
            return result;
        }

        private bool IsSmaller(Node? node, T value)
        {// Node value is smaller than T value.
            if (node == null)
            {
                return true;
            }

            var cmp = this.Comparer.Compare(node.Value, value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
            return cmp == -1 && this.IsSmaller(node.Left, value) && this.IsSmaller(node.Right, value);
        }

        private bool IsLarger(Node? node, T value)
        {// Node value is larger than T value.
            if (node == null)
            {
                return true;
            }

            var cmp = this.Comparer.Compare(node.Value, value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
            return cmp == 1 && this.IsLarger(node.Left, value) && this.IsLarger(node.Right, value);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransplantNode(Node? node, Node destination)
        {// Transplant Node node to Node destination
            if (destination.Parent == null)
            {
                this.root = node;
            }
            else if (destination == destination.Parent.Left)
            {
                destination.Parent.Left = node;
            }
            else
            {
                destination.Parent.Right = node;
            }

            if (node != null)
            {
                node.Parent = destination.Parent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RotateLeft(Node x)
        {// checked
            var y = x.Right!;
            x.Right = y.Left;
            if (y.Left != null)
            {
                y.Left.Parent = x;
            }

            var p = x.Parent; // Parent of x
            y.Parent = p;
            if (p == null)
            {
                this.root = y;
            }
            else if (x == p.Left)
            {
                p.Left = y;
            }
            else
            {
                p.Right = y;
            }

            y.Left = x;
            x.Parent = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RotateRight(Node x)
        {// checked
            var y = x.Left!;
            x.Left = y.Right;
            if (y.Right != null)
            {
                y.Right.Parent = x;
            }

            var p = x.Parent; // Parent of x
            y.Parent = p;
            if (p == null)
            {
                this.root = y;
            }
            else if (x == p.Right)
            {
                p.Right = y;
            }
            else
            {
                p.Left = y;
            }

            y.Right = x;
            x.Parent = y;
        }

        private void RotateRightLeft(Node x)
        {
            var p = x.Parent;
            var y = x.Right!;
            var z = y.Left!;

            x.Parent = z;
            x.Right = z.Left;
            if (x.Right != null)
            {
                x.Right.Parent = x;
            }

            y.Parent = z;
            y.Left = z.Right;
            if (y.Left != null)
            {
                y.Left.Parent = y;
            }

            z.Parent = p;
            if (z.Parent == null)
            {
                this.root = z;
            }

            z.Left = x;
            z.Right = y;
        }

        private void RotateLeftRight(Node x)
        {
            var p = x.Parent;
            var y = x.Left!;
            var z = y.Right!;

            x.Parent = z;
            x.Left = z.Right;
            if (x.Left != null)
            {
                x.Left.Parent = x;
            }

            y.Parent = z;
            y.Right = z.Left;
            if (y.Right != null)
            {
                y.Right.Parent = y;
            }

            z.Parent = p;
            if (z.Parent == null)
            {
                this.root = z;
            }

            z.Right = x;
            z.Left = y;
        }
    }
}
