// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1405 // Debug.Assert should provide message text

namespace Arc.Collections;

public class SlidingList<T> : IList<T>, IReadOnlyList<T>
    where T : class
{
    private const int PositionMask = 0x7FFFFFFF;

    public SlidingList(int capacity)
    {
        this.items = new T?[capacity];
    }

    #region FieldAndProperty

    private T?[] items;
    private int itemsPosition; // The position of the first element in items.
    private int headIndex; // The head index in items (the first used item).
    private int addHint;
    private int version;

    /// <summary>
    /// Gets the position of the first element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int StartPosition => PositionMask & (this.itemsPosition + this.headIndex);

    /// <summary>
    /// Gets the position of the last element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int EndPosition => PositionMask & (this.itemsPosition + this.headIndex + this.items.Length);

    /// <summary>
    /// Gets the maximum number of elements that <see cref="SlidingList{T}"/> can hold.
    /// </summary>
    public int Capacity => this.items.Length;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is space in the <see cref="SlidingList{T}"/> and if a new element can be added.
    /// </summary>
    public bool CanAdd => this.Count < this.items.Length;

    /// <summary>
    /// Gets the first element of the <see cref="SlidingList{T}"/>, or a default value if the <see cref="SlidingList{T}"/> contains no elements.
    /// </summary>
    public T? FirstOrDefault
    {
        get
        {
            if (this.Count == 0)
            {
                return default;
            }

            this.TrySlide();
            return this.items[this.headIndex];
        }
    }

    #endregion

    /// <summary>
    /// Copies the elements of <see cref="SlidingList{T}"/> to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements of the <see cref="SlidingList{T}"/>.</returns>
    public T?[] ToArray()
    {
        var array = new T?[this.Count];
        var j = 0;
        for (var i = 0; i < this.items.Length; i++)
        {
            if (this.items[this.ClipIndex(this.headIndex + i)] is { } item)
            {
                array[j++] = item;
                if (j == this.Count)
                {
                    break;
                }
            }
        }

        Debug.Assert(j == this.Count);
        return array;
    }

    /// <summary>
    /// Changes the number of elements of the <see cref="SlidingList{T}"/> to the specified new size.
    /// </summary>
    /// <param name="capacity">The size of the <see cref="SlidingList{T}"/>.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Resize(int capacity)
    {
        if (this.items.Length == capacity)
        {// Identical
            return true;
        }
        else if (this.Count > capacity)
        {
            return false;
        }

        var array = new T?[capacity];
        var j = 0;
        for (var i = 0; i < this.items.Length; i++)
        {
            if (this.items[this.ClipIndex(this.headIndex + i)] is { } item)
            {
                array[j++] = item;
                if (j == this.Count)
                {
                    break;
                }
            }
        }

        this.items = array;
        this.itemsPosition += this.headIndex;
        this.headIndex = 0;
        this.addHint = j;
        this.version++;

        return true;
    }

    /// <summary>
    /// Slide the <see cref="SlidingList{T}"/> by the number of empty elements at the beginning.
    /// </summary>
    /// <returns>The number of slides made.</returns>
    public int TrySlide()
    {
        if (this.items[this.headIndex] is not null)
        {
            return 0;
        }

        var count = 0;
        for (var i = this.headIndex; i < this.headIndex + this.items.Length; i++)
        {
            if (this.items[this.ClipIndex(i)] is null)
            {
                count++;
            }
            else
            {
                break;
            }
        }

        this.headIndex += count;
        if (this.headIndex >= this.items.Length)
        {
            this.headIndex -= this.items.Length;
            this.itemsPosition += this.items.Length;
        }

        this.addHint = this.headIndex;
        this.version++;

        return count;
    }

    /// <summary>
    /// Adds an element to the end of the List and returns its position. If addition is not possible, returns -1.
    /// </summary>
    /// <param name="value">The value to be added.</param>
    /// <returns>The position of the new element.</returns>
    public int TryAdd(T value)
    {
        if (!this.CanAdd)
        {
            return -1;
        }

        for (var i = this.addHint; i < this.addHint + this.items.Length; i++)
        {
            var j = this.ClipIndex(i);
            if (this.items[j] is null)
            {
                this.addHint = this.ClipIndex(i + 1);

                this.items[j] = value;
                this.Count++;
                this.version++;
                return this.IndexToPosition(j);
            }
        }

        return -1;
    }

    /// <summary>
    /// Removes the element at the specified position.
    /// </summary>
    /// <param name="position">The position of the element to remove.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Remove(int position)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return false;
        }

        if (this.items[index] is null)
        {
            return false;
        }

        this.items[index] = default;
        this.Count--;

        if (index == this.headIndex)
        {
            this.TrySlide();
        }

        this.addHint = this.headIndex; // Reset
        this.version++;
        return true;
    }

    /// <summary>
    /// Gets the value of the element at the specified position.
    /// </summary>
    /// <param name="position">The position of the element.</param>
    /// <returns>The value.</returns>
    public T? Get(int position)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return default;
        }

        return this.items[index];
    }

    /// <summary>
    /// Sets the value of the element at the specified position.
    /// </summary>
    /// <param name="position">The position of the element.</param>
    /// <param name="value">The value of the element.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Set(int position, T value)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return default;
        }

        if (this.items[index] is null)
        {// New
            this.Count++;
        }

        this.items[index] = value;
        return true;
    }

    public bool UnsafeChangeValue(int position, T value)
    {
        var index = this.PositionToIndex(position);
        if (index < 0)
        {
            return false;
        }

        this.items[index] = value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexToPosition(int index)
    {
        if (index >= this.headIndex)
        {
            return PositionMask & (this.itemsPosition + index);
        }
        else
        {
            return PositionMask & (this.itemsPosition + this.items.Length + index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PositionToIndex(int position)
    {
        int index;
        var start = this.StartPosition;
        var end = this.EndPosition;
        if (start < end)
        {
            if (start <= position && position < end)
            {
                index = position - start + this.headIndex;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (start <= position)
            {
                index = position - start + this.headIndex;
            }
            else if (position < end)
            {
                index = PositionMask & (position - start + this.headIndex);
            }
            else
            {
                return -1;
            }
        }

        return index < this.items.Length ? index : index - this.items.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ClipIndex(int index) => index < this.items.Length ? index : index - this.items.Length;

    #region ICollection

    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an object to the end of the list.
    /// <br/>O(1) operation.
    /// </summary>
    /// <param name="value">The value to be added to the end of the list.</param>
    public void Add(T value) => this.TryAdd(value);

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    public void Clear()
    {
        Array.Clear(this.items, 0, this.items.Length);
        this.itemsPosition = 0;
        this.headIndex = 0;
        this.addHint = 0;
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
    public void CopyTo(T[] array, int arrayIndex) => Array.Copy(this.ToArray(), 0, array, arrayIndex, this.items.Length);

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

    public T this[int position]
    {
        get => this.Get(position) ?? throw new ArgumentOutOfRangeException();

        set => this.Set(position, value);
    }

    /// <summary>
    /// Returns the zero-based index of the first occurrence of a value in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate in the list.</param>
    /// <returns>The zero-based index of the first occurrence of item.</returns>
    public int IndexOf(T value) => Array.IndexOf(this.items, value);

    /// <summary>
    /// Inserts an element into the <see cref="UnorderedList{T}"/> at the specified index.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="position">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    public void Insert(int position, T item) => this.Set(position, item);

    /// <summary>
    /// Removes the element at the specified index of the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= this.items.Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        this.items[index] = default;
        if (index == this.headIndex)
        {
            this.TrySlide();
        }

        this.addHint = this.headIndex; // Reset
        this.version++;
    }

    #endregion

    #region Enumerator

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private SlidingList<T> list;
        private int index;
        private int version;
        private T? current;

        internal Enumerator(SlidingList<T> list)
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

            if (this.index < this.list.items.Length)
            {
                this.current = this.list.items[this.list.ClipIndex(this.list.headIndex + this.index)];
                this.index++;
                return true;
            }

            this.index = this.list.items.Length + 1;
            this.current = default(T);
            return false;
        }

        public T Current => this.current!;

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.list.items.Length + 1)
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
    }

    private static Exception ThrowVersionMismatch()
    {
        throw new InvalidOperationException("List was modified after the enumerator was instantiated.'");
    }

    #endregion
}
