﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    private int version;

    /// <summary>
    /// Gets the position of the first element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int StartPosition => PositionMask & (this.itemsPosition + this.headIndex);

    /// <summary>
    /// Gets the position of the last element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int EndPosition => PositionMask & (this.itemsPosition + this.headIndex + this.Consumed);

    /// <summary>
    /// Gets the maximum number of elements that <see cref="SlidingList{T}"/> can hold.
    /// </summary>
    public int Capacity => this.items.Length;

    /* /// <summary>
    /// Gets the number of elements contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    // public int Count { get; private set; }*/

    int ICollection<T>.Count => this.Consumed;

    int IReadOnlyCollection<T>.Count => this.Consumed;

    /// <summary>
    /// Gets the number of consumed elements in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int Consumed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is space in the <see cref="SlidingList{T}"/> and if a new element can be added.
    /// </summary>
    public bool CanAdd => this.Consumed < this.items.Length;

    /// <summary>
    /// Gets the first element of the <see cref="SlidingList{T}"/>, or a default value if the <see cref="SlidingList{T}"/> contains no elements.
    /// </summary>
    public T? FirstOrDefault
    {
        get
        {
            if (this.Consumed == 0)
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
        var array = new T?[this.Consumed];
        var j = 0;
        for (var i = 0; i < this.Consumed; i++)
        {
            if (this.items[this.ClipIndex(this.headIndex + i)] is { } item)
            {
                array[j++] = item;
                if (j == this.Consumed)
                {
                    break;
                }
            }
        }

        if (j != this.Consumed)
        {
            Array.Resize(ref array, j);
        }

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
        else if (this.Consumed > capacity)
        {
            return false;
        }

        var array = new T?[capacity];
        var j = 0;
        for (var i = 0; i < this.Consumed; i++)
        {
            array[j++] = this.items[this.ClipIndex(this.headIndex + i)];
        }

        this.items = array;
        this.itemsPosition += this.headIndex;
        this.headIndex = 0;
        this.version++;

        return true;
    }

    /// <summary>
    /// Slide the <see cref="SlidingList{T}"/> by the number of empty elements at the beginning.
    /// </summary>
    /// <returns>The number of slides made.</returns>
    public int TrySlide()
    {
        if (this.items[this.headIndex] is not null ||
            this.Consumed == 0)
        {
            return 0;
        }

        var count = 0;
        for (var i = this.headIndex; i < this.headIndex + this.Consumed; i++)
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

        this.Consumed -= count;
        this.version++;

        return count;
    }

    /// <summary>
    /// Inserts an element into an available space in the list. If insertion is not possible, returns -1.
    /// </summary>
    /// <param name="value">The value to be added.</param>
    /// <returns>The position of the new element.</returns>
    public int Add(T value)
    {
        if (!this.CanAdd)
        {
            return -1;
        }

        var i = this.ClipIndex(this.headIndex + this.Consumed);
        this.items[i] = value;
        this.Consumed++;
        this.version++;
        return this.IndexToPosition(i);
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

        if (index == this.headIndex)
        {
            this.TrySlide();
        }

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

        int dif;
        if (index >= this.headIndex)
        {
            dif = index - this.headIndex;
        }
        else
        {
            dif = this.headIndex - index;
        }

        if (dif > this.Consumed)
        {
            this.Consumed = dif;
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
        var end = PositionMask & (this.itemsPosition + this.headIndex + this.items.Length);
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
    /// Removes all elements from the list.
    /// </summary>
    public void Clear()
    {
        Array.Clear(this.items, 0, this.items.Length);
        this.itemsPosition = 0;
        this.headIndex = 0;
        this.Consumed = 0;
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

    void ICollection<T>.Add(T item)
        => this.Add(item);

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
        private int last;
        private int version;
        private T? current;

        internal Enumerator(SlidingList<T> list)
        {
            this.list = list;
            this.index = list.headIndex;
            this.last = list.headIndex + this.list.Consumed;
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

            while (this.index != this.last)
            {
                if (this.list.items[this.list.ClipIndex(this.index)] is { } item)
                {
                    this.index++;
                    this.current = item;
                    return true;
                }
                else
                {
                    this.index++;
                }
            }

            this.current = default(T);
            return false;
        }

        public T Current => this.current!;

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.last)
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
