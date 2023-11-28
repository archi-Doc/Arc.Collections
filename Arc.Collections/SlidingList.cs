// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Collections;

public class SlidingList<T> : IEnumerable<T>, IEnumerable
    where T : class
{
    private const int PositionMask = 0x7FFFFFFF;

    public SlidingList(int size)
    {
        this.items = new T?[size];
    }

    #region FieldAndProperty

    private T?[] items;
    private int itemsPosition; // Position of the first element in items.
    private int headIndex; // The head index in items.
    private int headSize; // The number of items in use, beginning from the head index.
    private int version;

    /// <summary>
    /// Gets the position of the first element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int StartPosition => PositionMask & (this.itemsPosition + this.headIndex);

    /// <summary>
    /// Gets the position of the last element contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int EndPosition => PositionMask & (this.itemsPosition + this.headIndex + this.headSize);

    /// <summary>
    /// Gets the maximum number of elements that <see cref="SlidingList{T}"/> can hold.
    /// </summary>
    public int Capacity => this.items.Length;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="SlidingList{T}"/>.
    /// </summary>
    public int Count => this.headSize;

    /// <summary>
    /// Gets a value indicating whether there is space in the <see cref="SlidingList{T}"/> and if a new element can be added.
    /// </summary>
    public bool CanAdd => this.headSize < this.items.Length;

    /// <summary>
    /// Gets the first element of the <see cref="SlidingList{T}"/>, or a default value if the <see cref="SlidingList{T}"/> contains no elements.
    /// </summary>
    public T? FirstOrDefault => this.headSize == 0 ? null : this.items[this.headIndex];

    #endregion

    /// <summary>
    /// Copies the elements of <see cref="SlidingList{T}"/> to a new array.
    /// </summary>
    /// <returns>An array containing copies of the elements of the <see cref="SlidingList{T}"/>.</returns>
    public T?[] ToArray()
    {
        var array = new T?[this.headSize];
        for (var i = 0; i < this.headSize; i++)
        {
            array[i] = this.items[this.ClipIndex(this.headIndex + i)];
        }

        return array;
    }

    /// <summary>
    /// Changes the number of elements of the <see cref="SlidingList{T}"/> to the specified new size.
    /// </summary>
    /// <param name="size">The size of the <see cref="SlidingList{T}"/>.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public bool Resize(int size)
    {
        if (this.items.Length == size)
        {// Identical
            return true;
        }
        else if (this.headSize > size)
        {
            return false;
        }

        var newItems = new T?[size];
        for (var i = 0; i < this.headSize; i++)
        {
            newItems[i] = this.items[this.ClipIndex(this.headIndex + i)];
        }

        this.items = newItems;
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
        var count = 0;
        for (var i = this.headIndex; i < this.headIndex + this.headSize; i++)
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

        this.headSize -= count;
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

        var index = this.headIndex + this.headSize;
        if (index >= this.items.Length)
        {
            index -= this.items.Length;
        }

        this.headSize++;
        this.items[index] = value;
        this.version++;
        return this.IndexToPosition(index);
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

            if (this.index < this.list.headSize)
            {
                this.current = this.list.items[this.list.ClipIndex(this.list.headIndex + this.index)];
                this.index++;
                return true;
            }

            this.index = this.list.headSize + 1;
            this.current = default(T);
            return false;
        }

        public T Current => this.current!;

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.list.headSize + 1)
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
