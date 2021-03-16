// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collection.HotMethod;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1602 // Enumeration items should be documented

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
    /// Represents a collection of objects that is maintained in sorted order. <see cref="OrderedMap{TKey, TValue}"/> uses Red-Black Tree structure to store objects.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    public class OrderedMap<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
         where TKey : notnull
    {
        #region Node

        /// <summary>
        /// Represents a node in a <see cref="OrderedMap{TKey, TValue}"/>.
        /// </summary>
        public sealed class Node
        {
            internal Node(TKey key, TValue value, NodeColor color)
            {
                this.Key = key;
                this.Value = value;
                this.Color = color;
            }

            /// <summary>
            /// Gets the key contained in the node.
            /// </summary>
            public TKey Key { get; internal set; }

            /// <summary>
            /// Gets the value contained in the node.
            /// </summary>
            public TValue Value { get; internal set; }

            /// <summary>
            /// Gets or sets the parent node in the <see cref="OrderedMap{TKey, TValue}"/>.
            /// </summary>
            internal Node? Parent { get; set; }

            /// <summary>
            /// Gets or sets the left node in the <see cref="OrderedMap{TKey, TValue}"/>.
            /// </summary>
            internal Node? Left { get; set; }

            /// <summary>
            /// Gets or sets the right node in the <see cref="OrderedMap{TKey, TValue}"/>.
            /// </summary>
            internal Node? Right { get; set; }

            /// <summary>
            /// Gets or sets the color of the node.
            /// </summary>
            internal NodeColor Color { get; set; }

            /// <summary>
            /// Gets the previous node in the <see cref="OrderedMap{TKey, TValue}"/>.
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
            /// Gets the next node in the <see cref="OrderedMap{TKey, TValue}"/>
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

            internal bool IsBlack => this.Color == NodeColor.Black;

            internal bool IsRed => this.Color == NodeColor.Red;

            internal bool IsUnused => this.Color == NodeColor.Unused;

            internal bool IsLinkedList => this.Color == NodeColor.LinkedList;

            public override string ToString() => this.Color.ToString() + ": " + this.Value?.ToString();

            internal void ColorBlack() => this.Color = NodeColor.Black;

            internal void ColorRed() => this.Color = NodeColor.Red;

            internal void Clear()
            {
                this.Key = default(TKey)!;
                this.Value = default(TValue)!;
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
        private KeyCollection? keys;
        private ValueCollection? values;

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="OrderedMap{TKey, TValue}"/>.
        /// </summary>
        public int Count { get; private set; }

        public IComparer<TKey> Comparer { get; private set; }

        public IHotMethod2<TKey, TValue>? HotMethod2 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class.
        /// </summary>
        public OrderedMap()
        {
            this.Comparer = Comparer<TKey>.Default;
            this.HotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedMap(IComparer<TKey> comparer)
        {
            this.Comparer = comparer ?? Comparer<TKey>.Default;
            this.HotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The IDictionary implementation to copy to a new collection.</param>
        public OrderedMap(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, Comparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The IDictionary implementation to copy to a new collection.</param>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedMap(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            this.Comparer = comparer ?? Comparer<TKey>.Default;
            this.HotMethod2 = HotMethodResolver.Get<TKey, TValue>(this.Comparer);

            foreach (var x in dictionary)
            {
                this.Add(x.Key, x.Value);
            }
        }

        /// <summary>
        /// Gets the first node in the <see cref="OrderedMap{TKey, TValue}"/>.
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
        /// Gets the last node in the <see cref="OrderedMap{TKey, TValue}"/>. O(log n) operation.
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

        #region Enumerator

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            private readonly OrderedMap<TKey, TValue> map;
            private readonly int version;
            private readonly int getEnumeratorRetType;
            private Node? node;
            private TKey? key;
            private TValue? value;

            internal Enumerator(OrderedMap<TKey, TValue> set, int getEnumeratorRetType)
            {
                this.map = set;
                this.version = this.map.version;
                this.getEnumeratorRetType = getEnumeratorRetType;
                this.node = this.map.First;
                this.key = default;
                this.value = default;
            }

            public void Dispose()
            {
                this.node = null;
                this.key = default;
                this.value = default;
            }

            public bool MoveNext()
            {
                if (this.version != this.map.version)
                {
                    throw ThrowVersionMismatch();
                }

                if (this.node == null)
                {
                    this.key = default(TKey)!;
                    this.value = default(TValue)!;
                    return false;
                }

                this.key = this.node.Key;
                this.value = this.node.Value;
                this.node = this.node.Next;
                return true;
            }

            DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(this.key!, this.value!);

            object IDictionaryEnumerator.Key => this.key!;

            object IDictionaryEnumerator.Value => this.value!;

            public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(this.key!, this.value!);

            object? IEnumerator.Current
            {
                get
                {
                    if (this.getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(this.key!, this.value!);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(this.key!, this.value!);
                    }
                }
            }

            void System.Collections.IEnumerator.Reset() => this.Reset();

            internal void Reset()
            {
                if (this.version != this.map.version)
                {
                    throw ThrowVersionMismatch();
                }

                this.node = this.map.First;
                this.key = default;
                this.value = default;
            }

            private static Exception ThrowVersionMismatch()
            {
                throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.'");
            }
        }

        #endregion

        #region ICollection

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(nameof(array));
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.Count)
            {
                throw new ArgumentException();
            }

            var node = this.First;
            KeyValuePair<TKey, TValue>[]? keyValuePairArray = array as KeyValuePair<TKey, TValue>[];
            if (keyValuePairArray != null)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    keyValuePairArray[i + index] = new KeyValuePair<TKey, TValue>(node!.Key, node!.Value);
                    node = node.Next;
                }
            }
            else
            {
                object[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(nameof(array));
                }

                try
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        objects[i + index] = new KeyValuePair<TKey, TValue>(node!.Key, node!.Value);
                        node = node.Next;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }
        }

        #endregion

        #region IDictionary

        object IDictionary.this[object key]
        {
            get
            {
                if (key is TKey k)
                {
                    if (this.TryGetValue(k, out var value))
                    {
                        return value!;
                    }
                }

                return null!;
            }

            set
            {
                this[(TKey)key] = (TValue)value;
            }
        }

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => (ICollection)this.Keys;

        ICollection IDictionary.Values => (ICollection)this.Values;

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.Add((TKey)key, (TValue)value);
        }

        bool IDictionary.Contains(object key)
        {
            if (key is TKey k)
            {
                return this.ContainsKey(k);
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DictEntry);

        void IDictionary.Remove(object key)
        {
            if (key is TKey k)
            {
                this.Remove(k);
            }
        }

        #endregion

        #region IDictionary<TKey, TValue>

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.Keys;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => this.Values;

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => this.Add(key, value);

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>>

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => this.Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            var node = this.FindNode(item.Key);
            return node != null && EqualityComparer<TValue>.Default.Equals(node.Value, item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection)this).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            var node = this.FindNode(item.Key);
            if (node == null || !EqualityComparer<TValue>.Default.Equals(node.Value, item.Value))
            {
                return false;
            }

            this.RemoveNode(node);
            return true;
        }

        #endregion

        #region KeyValueCollection

        public KeyCollection Keys => this.keys != null ? this.keys : (this.keys = new KeyCollection(this));

        public ValueCollection Values => this.values != null ? this.values : (this.values = new ValueCollection(this));

        public sealed class KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly OrderedMap<TKey, TValue> map;

            public KeyCollection(OrderedMap<TKey, TValue> map)
            {
                if (map == null)
                {
                    throw new ArgumentNullException(nameof(map));
                }

                this.map = map;
            }

            public Enumerator GetEnumerator() => new Enumerator(this.map);

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => new Enumerator(this.map);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this.map);

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < this.Count)
                {
                    throw new ArgumentException();
                }

                var node = this.map.First;
                while (node != null)
                {
                    array[index++] = node.Key;
                    node = node.Next;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException(nameof(array));
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException(nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < this.map.Count)
                {
                    throw new ArgumentException();
                }

                TKey[]? keys = array as TKey[];
                if (keys != null)
                {
                    this.CopyTo(keys, index);
                }
                else
                {
                    try
                    {
                        object[] objects = (object[])array;
                        var node = this.map.First;
                        while (node != null)
                        {
                            objects[index++] = node.Key!;
                            node = node.Next;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(nameof(array));
                    }
                }
            }

            public int Count => this.map.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

            void ICollection<TKey>.Clear() => throw new NotSupportedException();

            bool ICollection<TKey>.Contains(TKey item) => this.map.ContainsKey(item);

            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)this.map).SyncRoot;

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private IEnumerator<KeyValuePair<TKey, TValue>> mapEnum;

                internal Enumerator(OrderedMap<TKey, TValue> map)
                {
                    this.mapEnum = map.GetEnumerator();
                }

                public void Dispose() => this.mapEnum.Dispose();

                public bool MoveNext() => this.mapEnum.MoveNext();

                public TKey Current => this.mapEnum.Current.Key;

                object? IEnumerator.Current => this.Current;

                void IEnumerator.Reset() => this.mapEnum.Reset();
            }
        }

        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly OrderedMap<TKey, TValue> map;

            public ValueCollection(OrderedMap<TKey, TValue> map)
            {
                if (map == null)
                {
                    throw new ArgumentNullException(nameof(map));
                }

                this.map = map;
            }

            public Enumerator GetEnumerator() => new Enumerator(this.map);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(this.map);

            IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this.map);

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < this.Count)
                {
                    throw new ArgumentException();
                }

                var node = this.map.First;
                while (node != null)
                {
                    array[index++] = node.Value;
                    node = node.Next;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException(nameof(array));
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException(nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (array.Length - index < this.map.Count)
                {
                    throw new ArgumentException();
                }

                TValue[]? values = array as TValue[];
                if (values != null)
                {
                    this.CopyTo(values, index);
                }
                else
                {
                    try
                    {
                        object?[] objects = (object?[])array;
                        var node = this.map.First;
                        while (node != null)
                        {
                            objects[index++] = node.Value;
                            node = node.Next;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(nameof(array));
                    }
                }
            }

            public int Count => this.map.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            bool ICollection<TValue>.Contains(TValue item)
            {
                return this.map.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)this.map).SyncRoot;

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private IEnumerator<KeyValuePair<TKey, TValue>> mapEnum;

                internal Enumerator(OrderedMap<TKey, TValue> map)
                {
                    this.mapEnum = map.GetEnumerator();
                }

                public void Dispose() => this.mapEnum.Dispose();

                public bool MoveNext() => this.mapEnum.MoveNext();

                public TValue Current => this.mapEnum.Current.Value;

                object? IEnumerator.Current => this.Current;

                void IEnumerator.Reset() => this.mapEnum.Reset();
            }
        }

        #endregion

        #region Main

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var node = this.FindNode(key);
                if (node == null)
                {
                    throw new KeyNotFoundException();
                }

                return node.Value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var node = this.FindNode(key);
                if (node == null)
                {
                    this.Add(key, value);
                }
                else
                {
                    node.Value = value;
                }
            }
        }

        public bool ContainsKey(TKey key) => this.FindNode(key) != null;

        public bool ContainsValue(TValue value)
        {
            var found = false;

            if (value == null)
            {
                var node = this.First;
                while (node != null)
                {
                    if (node.Value == null)
                    {
                        found = true;
                        break;
                    }

                    node = node.Next;
                }
            }
            else
            {
                var comparer = EqualityComparer<TValue>.Default;
                var node = this.First;
                while (node != null)
                {
                    if (comparer.Equals(node.Value, value))
                    {
                        found = true;
                        break;
                    }

                    node = node.Next;
                }
            }

            return found;
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var node = this.FindNode(key);
            if (node == null)
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }

        public void Clear()
        {
            this.root = null;
            this.version = 0;
            this.Count = 0;
        }

        /// <summary>
        /// Removes a specified item from the <see cref="OrderedMap{TKey, TValue}"/>.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The element to remove.</param>
        /// <returns>true if the element is found and successfully removed.</returns>
        public bool Remove(TKey key)
        {
            var p = this.FindNode(key);
            if (p == null)
            {
                return false;
            }

            this.RemoveNode(p);
            return true;
        }

        /// <summary>
        /// Adds an element to a collection. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the node is created.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value) => this.Probe(key, value, null);

        /// <summary>
        /// Adds an element to a collection. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <param name="reuse">Reuse a node to avoid memory allocation.</param>
        /// <returns>node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the node is created.</returns>
        public (Node node, bool newlyAdded) Add(TKey key, TValue value, Node reuse) => this.Probe(key, value, reuse);

        /// <summary>
        /// Adds an element to a collection. If the element is already in the set, this method replaces the stored element with the new element and sets the replaced flag to true.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
        /// replaced: true if the node is replaced.</returns>
        public (Node node, bool replaced) Replace(TKey key, TValue value)
        {
            var result = this.Probe(key, value, null);
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
        /// Updates the node's key with the specified key. Removes the node and inserts in the correct position if necessary.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="node">The <see cref="OrderedMap{TKey, TValue}.Node"/> to replace.</param>
        /// <param name="key">The key to set.</param>
        /// <returns>true if the node is replaced.</returns>
        public bool ReplaceNode(Node node, TKey key)
        {
            if (this.Comparer.Compare(node.Key, key) == 0)
            {// Identical
                return false;
            }

            var value = node.Value;
            this.RemoveNode(node);
            this.Probe(key, value, node);
            return true;
        }

        /// <summary>
        /// Removes a specified node from the <see cref="OrderedMap{TKey, TValue}"/>.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="node">The <see cref="OrderedMap{TKey, TValue}.Node"/> to remove.</param>
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

        /// <summary>
        /// Searches for a <see cref="OrderedMap{TKey, TValue}.Node"/> with the specified value.
        /// </summary>
        /// <param name="key">The value to search in a collection.</param>
        /// <returns>The node with the specified value.</returns>
        public Node? FindNode(TKey key)
        {
            Node? x = this.root;
            Node? p = null;
            int cmp = 0;

            if (this.HotMethod2 != null)
            {
                (cmp, p) = this.HotMethod2.SearchNode(x, key);
                if (cmp == 0 && p != null)
                {// Found
                    return p;
                }
            }
            else if (this.Comparer == Comparer<TKey>.Default && key is IComparable<TKey> ic)
            {// IComparable<TKey>
                while (x != null)
                {
                    cmp = ic.CompareTo(x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
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
                    {// Found
                        return x;
                    }
                }
            }
            else
            {// IComparer<TKey>
                while (x != null)
                {
                    cmp = this.Comparer.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
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
                    {// Found
                        return x;
                    }
                }
            }

            // Not found
            return null;
        }

        /// <summary>
        /// Adds an element to the set. If the element is already in the set, this method returns the stored node without creating a new node.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="key">The element to add to the set.</param>
        /// <returns>node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the node is created.</returns>
        private (Node node, bool newlyAdded) Probe(TKey key, TValue value, Node? reuse)
        {
            Node? x = this.root; // Traverses tree looking for insertion point.
            Node? p = null; // Parent of x; node at which we are rebalancing.
            int cmp = 0;

            if (this.HotMethod2 != null)
            {
                (cmp, p) = this.HotMethod2.SearchNode(x, key);
                if (cmp == 0 && p != null)
                {// Found
                    return (p, false);
                }
            }
            else if (this.Comparer == Comparer<TKey>.Default && key is IComparable<TKey> ic)
            {// IComparable<TKey>
                while (x != null)
                {
                    cmp = ic.CompareTo(x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
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
                    {// Found
                        return (x, false);
                    }
                }
            }
            else
            {// IComparer<TKey>
                while (x != null)
                {
                    cmp = this.Comparer.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
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
                    {// Found
                        return (x, false);
                    }
                }
            }

            this.version++;
            this.Count++;

            Node n;
            if (reuse != null && reuse.IsUnused)
            {
                reuse.Reset(key, value, NodeColor.Red);
                n = reuse;
            }
            else
            {
                n = new Node(key, value, NodeColor.Red); // Newly inserted node.
            }

            n.Parent = p;
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

        #endregion

        #region Validation

        /// <summary>
        /// Validate Red-Black Tree.
        /// </summary>
        /// <returns>true if the tree is valid.</returns>
        public bool Validate()
        {
            bool result = true;
            result &= this.ValidateBST(this.root);
            result &= this.ValidateBlackHeight(this.root) >= 0;
            result &= this.ValidateColor(this.root) == NodeColor.Black;

            return result;
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

            result &= this.IsSmaller(node.Left, node.Key) && this.IsLarger(node.Right, node.Key);
            result &= this.ValidateBST(node.Left) && this.ValidateBST(node.Right);
            return result;
        }

        private bool IsSmaller(Node? node, TKey key)
        {// Node value is smaller than TKey value.
            if (node == null)
            {
                return true;
            }

            var cmp = this.Comparer.Compare(node.Key, key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
            return cmp == -1 && this.IsSmaller(node.Left, key) && this.IsSmaller(node.Right, key);
        }

        private bool IsLarger(Node? node, TKey key)
        {// Node value is larger than TKey value.
            if (node == null)
            {
                return true;
            }

            var cmp = this.Comparer.Compare(node.Key, key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
            return cmp == 1 && this.IsLarger(node.Left, key) && this.IsLarger(node.Right, key);
        }

        #endregion

        #region LowLevel

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

        #endregion
    }
}
