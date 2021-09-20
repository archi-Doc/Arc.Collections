// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1124 // Do not use regions

namespace Arc.Collections
{
    /// <summary>
    /// Represents a collection of objects.
    /// <br/><see cref="UnorderedSet{T}"/> uses a hash table structure to store objects.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class UnorderedSet<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
        /// </summary>
        public UnorderedSet()
        {
            this.map = new();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public UnorderedSet(IEqualityComparer<T> comparer)
        {
            this.map = new(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        public UnorderedSet(IEnumerable<T> collection)
            : this(collection, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedSet{T}"/> class.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public UnorderedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            this.map = new(comparer);

            foreach (var x in collection)
            {
                this.Add(x);
            }
        }

        private UnorderedMap<T, int> map;

        #region Main

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="UnorderedSet{T}"/>.
        /// </summary>
        public int Count => this.map.Count;

        /// <summary>
        /// Adds an element to a collection. If the element is already in the set, this method returns the stored element without creating a new node, and sets newlyAdded to false.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>nodeIndex: the added node index.<br/>
        /// newlyAdded: true if the node is created.</returns>
        public (int nodeIndex, bool newlyAdded) Add(T value)
        {
            var result = this.map.Add(value, 0);
            return result;
        }

        /// <summary>
        /// Determines whether a collection contains a specific value.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="value">The value to locate in the collection.</param>
        /// <returns>true if the collection contains an element with the specified value; otherwise, false.</returns>
        public bool Contains(T value) => this.map.ContainsKey(value);

        /// <summary>
        /// Updates the node's value with the specified value. Removes the node and inserts it in the correct position if necessary.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="nodeIndex">The <see cref="UnorderedMap{TKey, TValue}.Node"/> to set the value.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the node is successfully updated.</returns>
        public bool SetNodeValue(int nodeIndex, T? value) => this.map.SetNodeKey(nodeIndex, value);

        /// <summary>
        /// Removes a specified value from the collection."/>.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="value">The element to remove.</param>
        /// <returns>true if the element is found and successfully removed.</returns>
        public bool Remove(T value) => this.map.Remove(value);

        /// <summary>
        /// Removes a specified node from the collection.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="nodeIndex">The index of the node to remove.</param>
        public void RemoveNode(int nodeIndex) => this.map.RemoveNode(nodeIndex);

        /// <summary>
        /// Removes all elements from a collection.
        /// </summary>
        public void Clear() => this.map.Clear();

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
