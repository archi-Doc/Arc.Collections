// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arc.Collection.HotMethod;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

namespace Arc.Collection
{
    public class OrderedList<T> : UnorderedList<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
        /// </summary>
        public OrderedList()
        {
            this.Comparer = Comparer<T>.Default;
            this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public OrderedList(int capacity)
            : base(capacity)
        {
            this.Comparer = Comparer<T>.Default;
            this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedList(IComparer<T> comparer)
        {
            this.Comparer = comparer ?? Comparer<T>.Default;
            this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public OrderedList(IEnumerable<T> collection)
            : this(collection, Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        public OrderedList(IEnumerable<T> collection, IComparer<T> comparer)
        {
            this.Comparer = comparer ?? Comparer<T>.Default;
            this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var array = collection.ToArray();
            Array.Sort(array, this.Comparer);
            this.size = array.Length;
            this.items = array;
        }

        public IComparer<T> Comparer { get; private set; }

        public IHotMethod<T>? HotMethod { get; private set; }

        /// <summary>
        /// Adds an object to the list in order.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The value to be added to the list.</param>
        public new void Add(T value)
        {
            var pos = this.BinarySearch(value);
            if (pos < 0)
            {
                this.Insert(~pos, value);
            }
            else
            {// Adds to the end of the same values.
                pos++;
                while (pos < this.size && this.Comparer.Compare(this.items[pos], value) == 0)
                {
                    pos++;
                }

                this.Insert(pos, value);
            }
        }

        /// <summary>
        /// Searches a list for the specific value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The index of the specified value in list. If the value is not found, the negative number returned is the bitwise complement of the index of the first element that is larger than value.</returns>
        public int BinarySearch(T value)
        {// => Array.BinarySearch(this.items, 0, this.size, value, this.Comparer);
            if (this.HotMethod != null)
            {
                return this.HotMethod.BinarySearch(this.items, 0, this.size, value);
            }
            else if (this.size >= 1000)
            {
                return Array.BinarySearch(this.items, 0, this.size, value, this.Comparer);
            }

            var min = 0;
            var max = this.size - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                var cmp = this.Comparer.Compare(value, this.items[mid]); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                if (cmp < 0)
                {
                    max = mid - 1;
                    continue;
                }
                else if (cmp > 0)
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public int ArrayBinarySearch(T value) => Array.BinarySearch(this.items, 0, this.size, value, this.Comparer);

        /// <summary>
        /// Determines whether an element is in the list.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>true if value is found in the list.</returns>
        public new bool Contains(T value) => this.BinarySearch(value) >= 0;

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="UnorderedList{T}"/>.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="UnorderedList{T}"/>. </param>
        /// <returns>true if item is successfully removed.</returns>
        public new bool Remove(T value)
        {
            var index = this.BinarySearch(value);
            if (index >= 0)
            {
                this.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the zero-based index of the first occurrence of a value in the list.
        /// <br/>O(log n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>The zero-based index of the first occurrence of item.</returns>
        public new int IndexOf(T value) => this.BinarySearch(value);
    }
}
