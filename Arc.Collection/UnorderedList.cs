// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1401

namespace Arc.Collection
{
    /// <summary>
    /// Represents a list of objects that can be accessed by index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class UnorderedList<T> : IList<T>, IReadOnlyList<T>
    {
        protected T[] items;
        protected int size;
        protected int version;

        private const int MaxSize = 0X7FEFFFFF;
        private const int DefaultCapacity = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedList{T}"/> class.
        /// </summary>
        public UnorderedList()
        {
            this.items = Array.Empty<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedList{T}"/> class.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public UnorderedList(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            else if (capacity > MaxSize)
            {
                capacity = MaxSize;
            }

            if (capacity == 0)
            {
                this.items = Array.Empty<T>();
            }
            else
            {
                this.items = new T[capacity];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnorderedList{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public UnorderedList(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    this.items = Array.Empty<T>();
                }
                else
                {
                    this.items = new T[count];
                    c.CopyTo(this.items, 0);
                    this.size = count;
                }
            }
            else
            {
                this.size = 0;
                this.items = Array.Empty<T>();

                using (var en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        this.Add(en.Current);
                    }
                }
            }
        }

        #region ICollection

        public int Count => this.size;

        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an object to the end of the list.
        /// <br/>O(1) operation.
        /// </summary>
        /// <param name="value">The value to be added to the end of the list.</param>
        public void Add(T value)
        {
            if (this.size == this.items.Length)
            {
                this.EnsureCapacity(this.size + 1);
            }

            this.items[this.size++] = value;
            this.version++;
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public void Clear()
        {
            if (this.size > 0)
            {
                Array.Clear(this.items, 0, this.size);
                this.size = 0;
            }

            this.version++;
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>true if value is found in the list.</returns>
        public bool Contains(T value) => this.IndexOf(value) >= 0;

        /// <summary>
        /// Copies the list or a portion of it to an array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(this.items, 0, array, arrayIndex, this.size);

        /// <summary>
        /// Copies the list or a portion of it to an array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from list.</param>
        public void CopyTo(T[] array) => this.CopyTo(array, 0);

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="UnorderedList{T}"/>.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The object to remove from the <see cref="UnorderedList{T}"/>. </param>
        /// <returns>true if item is successfully removed.</returns>
        public bool Remove(T value)
        {
            var index = this.IndexOf(value);
            if (index >= 0)
            {
                this.RemoveAt(index);
                return true;
            }

            return false;
        }

        #endregion

        #region IList

        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return this.items[index];
            }

            set
            {
                if ((uint)index >= (uint)this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                this.items[index] = value;
                this.version++;
            }
        }

        /// <summary>
        /// Returns the zero-based index of the first occurrence of a value in the list.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>The zero-based index of the first occurrence of item.</returns>
        public int IndexOf(T value) => Array.IndexOf(this.items, value, 0, this.size);

        /// <summary>
        /// Inserts an element into the <see cref="UnorderedList{T}"/> at the specified index.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (this.size == this.items.Length)
            {
                this.EnsureCapacity(this.size + 1);
            }

            if (index < this.size)
            {
                Array.Copy(this.items, index, this.items, index + 1, this.size - index);
            }

            this.items[index] = item;
            this.size++;
            this.version++;
        }

        /// <summary>
        /// Removes the element at the specified index of the list.
        /// <br/>O(n) operation.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.size--;
            if (index < this.size)
            {
                Array.Copy(this.items, index + 1, this.items, index, this.size - index);
            }

            this.items[this.size] = default(T)!;
            this.version++;
        }

        #endregion

        private void EnsureCapacity(int min)
        {
            if (this.items.Length < min)
            {
                int newCapacity = this.items.Length == 0 ? DefaultCapacity : this.items.Length * 2;

                if ((uint)newCapacity > MaxSize)
                {
                    newCapacity = MaxSize;
                }

                if (newCapacity < min)
                {
                    newCapacity = min;
                }

                this.Capacity = newCapacity;
            }
        }

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity
        {
            get => this.items.Length;
            set
            {
                if (value < this.size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != this.items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (this.size > 0)
                        {
                            Array.Copy(this.items, 0, newItems, 0, this.size);
                        }

                        this.items = newItems;
                    }
                    else
                    {
                        this.items = Array.Empty<T>();
                    }
                }
            }
        }

        #region Enumerator

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private UnorderedList<T> list;
            private int index;
            private int version;
            private T? current;

            internal Enumerator(UnorderedList<T> list)
            {
                this.list = list;
                this.index = 0;
                this.version = list.version;
                this.current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.version != this.list.version)
                {
                    throw ThrowVersionMismatch();
                }

                if ((uint)this.index < (uint)this.list.size)
                {
                    this.current = this.list.items[this.index];
                    this.index++;
                    return true;
                }

                this.index = this.list.size + 1;
                this.current = default(T);
                return false;
            }

            public T Current => this.current!;

            object IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || this.index == this.list.size + 1)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return this.Current!;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (this.version != this.list.version)
                {
                    throw ThrowVersionMismatch();
                }

                this.index = 0;
                this.current = default(T);
            }

            private static Exception ThrowVersionMismatch()
            {
                throw new InvalidOperationException("List was modified after the enumerator was instantiated.'");
            }
        }
        #endregion
    }
}
