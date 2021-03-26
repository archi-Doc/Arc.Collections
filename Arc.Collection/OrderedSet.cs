// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arc.Collection.HotMethod;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collection
{
    /// <summary>
    /// Represents a collection of objects that is maintained in sorted order.
    /// <br/><see cref="OrderedSet{T}"/> uses Red-Black Tree structure to store objects.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class OrderedSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        public OrderedSet()
        {
            this.map = new();
            // this.map.CreateNode = static (key, value, color) => new Node(key, color);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedSet(IComparer<T> comparer)
        {
            this.map = new(comparer);
            // this.map.CreateNode = static (key, value, color) => new Node(key, color);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        public OrderedSet(IEnumerable<T> collection)
            : this(collection, Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedSet(IEnumerable<T> collection, IComparer<T> comparer)
        {
            this.map = new(comparer);
            // this.map.CreateNode = static (key, value, color) => new Node(key, color);

            foreach (var x in collection)
            {
                this.Add(x);
            }
        }

        private OrderedMap<T, int> map;

        /* Inherited Node class is a bit (10-20%) slower bacause of the casting operaiton.
        public class Node : OrderedMap<T, int>.Node
        {
            internal Node(T key, NodeColor color)
                : base(key, 0, color)
            {
            }
        }*/

        #region Main

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public int Count => this.map.Count;

        /// <summary>
        /// Gets the first node in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public OrderedMap<T, int>.Node? First => this.map.First;

        /// <summary>
        /// Gets the last node in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public OrderedMap<T, int>.Node? Last => this.map.Last;

        /// <summary>
        /// Adds an element to a collection. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>node: the added <see cref="OrderedMap{TKey, TValue}.Node"/>.<br/>
        /// newlyAdded: true if the node is created.</returns>
        public (OrderedMap<T, int>.Node node, bool newlyAdded) Add(T value)
        {
            var result = this.map.Add(value, 0);
            return result;
        }

        /// <summary>
        /// Determines whether a collection contains a specific value.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the collection.</param>
        /// <returns>true if the collection contains an element with the specified value; otherwise, false.</returns>
        public bool Contains(T value) => this.map.ContainsKey(value);

        /// <summary>
        /// Removes a specified value from the collection."/>.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The element to remove.</param>
        /// <returns>true if the element is found and successfully removed.</returns>
        public bool Remove(T value) => this.map.Remove(value);

        /// <summary>
        /// Removes a specified node from the collection.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="node">The <see cref="OrderedMap{TKey, TValue}.Node"/> to remove.</param>
        public void RemoveNode(OrderedMap<T, int>.Node node) => this.map.RemoveNode(node);

        /// <summary>
        /// Removes all elements from a collection.
        /// </summary>
        public void Clear() => this.map.Clear();

        /// <summary>
        /// Validate Red-Black Tree.
        /// </summary>
        /// <returns>true if the tree is valid.</returns>
        public bool Validate() => this.map.Validate();

        #endregion

        #region Interface

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection<T>.Add(T item) => this.map.Add(item, 0);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => this.map.Keys.CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.map.Keys).CopyTo(array, index);

        public IEnumerator<T> GetEnumerator() => this.map.Keys.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.map.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.map.Keys.GetEnumerator();

        #endregion
    }
}
