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
    /// Represents a collection of objects that is maintained in sorted order. <see cref="OrderedSet{T}"/> uses Red-Black Tree structure to store objects.
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedSet{T}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedSet(IComparer<T> comparer)
        {
            this.map = new(comparer);
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
            foreach (var x in collection)
            {
                this.Add(x);
            }
        }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        private OrderedMap<T, int> map;
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

        /// <summary>
        /// Gets the number of nodes actually contained in the <see cref="OrderedSet{T}"/>.
        /// </summary>
        public int Count => this.map.Count;

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public void Add(T item) => this.map.Add(item, 0);

        public void Clear() => this.map.Clear();

        public bool Contains(T item) => this.map.ContainsKey(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => this.map.Keys.CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.map.Keys).CopyTo(array, index);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.map.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.map.Keys.GetEnumerator();

        public bool Remove(T item) => this.map.Remove(item);
    }
}
