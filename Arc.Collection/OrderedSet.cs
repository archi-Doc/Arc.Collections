// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Collection
{
    public enum NodeColor : byte
    {
        Black,
        Red,
        Error,
    }

    public class OrderedSet<T> : IEnumerable<T>, IEnumerable
    {
        public sealed class Node
        {
            public Node(T value, NodeColor color)
            {
                this.Value = value;
                this.Color = color;
            }

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

            public static bool IsNonNullBlack(Node? node) => node != null && node.IsBlack;

            public static bool IsNonNullRed(Node? node) => node != null && node.IsRed;

            public static bool IsNullOrBlack(Node? node) => node == null || node.IsBlack;

            public T Value { get; set; }

            public Node? Parent { get; set; }

            public Node? Left { get; set; }

            public Node? Right { get; set; }

            public NodeColor Color { get; set; }

            public bool IsBlack => this.Color == NodeColor.Black;

            public bool IsRed => this.Color == NodeColor.Red;

            public void ColorBlack() => this.Color = NodeColor.Black;

            public void ColorRed() => this.Color = NodeColor.Red;

            public override string ToString() => this.Color.ToString() + ": " + this.Value?.ToString();

            public void Clear()
            {
                this.Value = default(T)!;
                this.Parent = null;
                this.Left = null;
                this.Right = null;
                this.Color = NodeColor.Black;
            }
        }

        private Node? root;
        private int version;

        public int Count { get; private set; }

        public IComparer<T> Comparer { get; private set; } = default!;

        public OrderedSet()
        {
            this.Comparer = Comparer<T>.Default;
        }

        public OrderedSet(IComparer<T>? comparer)
        {
            this.Comparer = comparer ?? Comparer<T>.Default;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

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

            public void Dispose()
            {
            }

            public T Current => this.current != null ? this.current.Value : default(T)!;

            object? System.Collections.IEnumerator.Current => this.current != null ? this.current.Value : default(T)!;

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

        public bool Validate()
        {
            bool result = true;
            result &= this.ValidateBST(this.root);
            result &= this.ValidateBlackHeight(this.root) >= 0;
            result &= this.ValidateColor(this.root) == NodeColor.Black;

            return result;
        }

        public void Clear()
        {
            this.root = null;
            this.version = 0;
            this.Count = 0;
        }

        public (Node node, bool newlyAdded) Add(T value) => this.Probe(value);

        public (Node node, bool replaced) Replace(T value)
        {
            var result = this.Probe(value);
            if (result.newlyAdded)
            {// New
                return (result.node, false);
            }

            // Replace
            this.version++;
            result.node.Value = value;
            return (result.node, true);
        }

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

        public void RemoveNode(Node node)
        {
            Node? f; // Node to fix.
            int dir = 0;
            var originalColor = node.Color;

            this.version++;
            this.Count--;

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

        /*public void RemoveNode(Node node)
        {
            int cmp;
            Node? p; // Parent of node.

            // cmp -1: node.Parent?.Left == node
            // cmp  1: node.Parent?.Right == node
            // cmp  0: node.Parent == null (node is root)
            p = node.Parent;
            if (p == null)
            {
                p = null!;
                cmp = 0;
            }
            else if (p.Left == node)
            {
                cmp = -1;
            }
            else
            {
                cmp = 1;
            }

            Node? f; // Node at which we are rebalancing.
            Node? s;
            if (node.Right == null)
            {// checked
                if (cmp < 0)
                {// Left
                    p.Left = node.Left;
                }
                else if (cmp > 0)
                {// Right
                    p.Right = node.Left;
                }
                else
                {// Root
                    this.root = node.Left;
                }

                if (node.Left != null)
                {
                    node.Left.Parent = node.Parent;
                }

                f = p;
            }
            else
            {
                NodeColor t;
                Node r = node.Right;

                if (r.Left == null)
                {
                    r.Left = node.Left;
                    if (cmp < 0)
                    {// Left
                        p.Left = r;
                    }
                    else if (cmp > 0)
                    {// Right
                        p.Right = r;
                    }
                    else
                    {// Root
                        this.root = r;
                    }

                    r.Parent = node.Parent;
                    if (r.Left != null)
                    {
                        r.Left.Parent = r;
                    }

                    t = node.Color;
                    node.Color = r.Color;
                    r.Color = t;

                    f = r;
                    cmp = 1;
                }
                else
                {
                    s = r.Left;
                    while (s.Left != null)
                    {
                        s = s.Left;
                    }

                    r = s.Parent!;
                    r.Left = s.Right;
                    s.Left = node.Left;
                    s.Right = node.Right;
                    if (cmp < 0)
                    {// Left
                        p.Left = s;
                    }
                    else if (cmp > 0)
                    {// Right
                        p.Right = s;
                    }
                    else
                    {// Root
                        this.root = s;
                    }

                    if (s.Left != null)
                    {
                        s.Left.Parent = s;
                    }

                    s.Right.Parent = s;
                    s.Parent = node.Parent;
                    if (r.Left != null)
                    {
                        r.Left.Parent = r;
                    }

                    t = node.Color;
                    node.Color = s.Color;
                    s.Color = t;

                    f = r;
                    cmp = -1;
                }
            }

            if (f == null)
            {
                this.root?.ColorBlack();
                return;
            }
            else if (node.IsRed)
            {
                return;
            }

#nullable disable
            while (f != this.root && f.IsBlack)
            {
                if (f == f.Parent!.Left)
                {
                    s = f.Parent.Right;
                    if (s.IsRed)
                    {
                        // case 3.1
                        s.ColorBlack();
                        f.Parent.ColorBlack();
                        this.RotateLeft(f.Parent);
                        s = f.Parent.Right;
                    }

                    if (s.Left.IsBlack && s.Right.IsBlack)
                    {
                        // case 3.2
                        s.ColorRed();
                        f = f.Parent;
                    }
                    else
                    {
                        if (s.Right.IsBlack)
                        {
                            // case 3.3
                            s.Left.ColorBlack();
                            s.ColorRed();
                            this.RotateRight(s);
                            s = f.Parent.Right;
                        }

                        // case 3.4
                        s.Color = f.Parent.Color;
                        f.Parent.ColorBlack();
                        s.Right.ColorBlack();
                        this.RotateLeft(f.Parent);
                        f = this.root;
                    }
                }
                else
                {
                    s = f.Parent.Left;
                    if (s.IsRed)
                    {
                        // case 3.1
                        s.ColorBlack();
                        f.Parent.ColorRed();
                        this.RotateRight(f.Parent);
                        s = f.Parent.Left;
                    }

                    if (s.Right.IsBlack && s.Right.IsBlack)
                    {
                        // case 3.2
                        s.ColorRed();
                        f = f.Parent;
                    }
                    else
                    {
                        if (s.Left.IsBlack)
                        {
                            // case 3.3
                            s.Right.ColorBlack();
                            s.ColorRed();
                            this.RotateLeft(s);
                            s = f.Parent.Left;
                        }

                        // case 3.4
                        s.Color = f.Parent.Color;
                        f.Parent.ColorBlack();
                        s.Left.ColorBlack();
                        this.RotateRight(f.Parent);
                        f = this.root;
                    }
                }
            }

            f.ColorBlack();
#nullable enable

            return;
        }*/

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
        /// Adds the value to the tree.
        /// If a duplicate node is found in the tree, returns the duplicate node without creating a new node.
        /// </summary>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>node: New node. newlyAdded: true if the new node is created.</returns>
        private (Node node, bool newlyAdded) Probe(T value)
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
            var n = new Node(value, NodeColor.Red); // Newly inserted node.
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
            if (leftColor == NodeColor.Error || rightColor == NodeColor.Error)
            {
                return NodeColor.Error;
            }

            if (color == NodeColor.Black)
            {
                return color;
            }
            else if (color == NodeColor.Red && leftColor == NodeColor.Black && rightColor == NodeColor.Black)
            {
                return color;
            }

            return NodeColor.Error;
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
        private void TransplantNode(Node? node, Node destination, int dir)
        {// Transplant Node node to Node destination
            if (dir < 0)
            {
                destination.Parent!.Left = node;
            }
            else if (dir > 0)
            {
                destination.Parent!.Right = node;
            }
            else
            {
                this.root = node;
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
