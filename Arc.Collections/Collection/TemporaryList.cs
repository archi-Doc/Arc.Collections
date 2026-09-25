// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Arc.Collections;

/// <summary>
/// Provides a temporary list with up to four elements stored inline.
/// </summary>
/// <typeparam name="T">The type of the objects.</typeparam>
/// <remarks>
/// Additional elements use a heap-allocated list, which copies of this struct share; do not copy it.
/// </remarks>
public ref struct TemporaryList<T> // : IEnumerable<T>, IEnumerable // ref struct types cannot implement interfaces or be boxed.
{
    private const int StackObjectCount = 4;

    private int count;
    private T obj0;
    private T obj1;
    private T obj2;
    private T obj3;
    private List<T>? list;

    /// <summary>
    /// Gets the number of objects in the list.
    /// </summary>
    public readonly int Count => this.count;

    /// <summary>
    /// Adds an object to the list.
    /// </summary>
    /// <param name="item">The object to add to the list.</param>
    public void Add(T item)
    {
        if (this.count == 0)
        {
            this.count = 1;
            this.obj0 = item;
            return;
        }
        else if (this.count == 1)
        {
            this.count = 2;
            this.obj1 = item;
            return;
        }
        else if (this.count == 2)
        {
            this.count = 3;
            this.obj2 = item;
            return;
        }
        else if (this.count == 3)
        {
            this.count = 4;
            this.obj3 = item;
            return;
        }

        this.list ??= new();
        this.list.Add(item);
        this.count++;
    }

    /// <summary>
    /// Copies the current contents of this temporary list to a new array.
    /// </summary>
    /// <returns>
    /// A new array containing all items in insertion order.<br/>
    /// Returns an empty array when the list contains no items.
    /// </returns>
    public readonly T[] ToArray()
    {
        var count = this.count;
        if (count == 0)
        {
            return [];
        }

        var array = new T[count];
        array[0] = this.obj0;

        if (count == 1)
        {
            return array;
        }

        array[1] = this.obj1;

        if (count == 2)
        {
            return array;
        }

        array[2] = this.obj2;

        if (count == 3)
        {
            return array;
        }

        array[3] = this.obj3;

        if (count > StackObjectCount)
        {
            // Copy only this instance's elements: a copy of the struct shares the overflow list.
            this.list!.CopyTo(0, array, StackObjectCount, count - StackObjectCount);
        }

        return array;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public readonly Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Enumerates the elements of a <see cref="TemporaryList{T}"/>.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly TemporaryList<T> temporaryList;
        private int index;
        private T? current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="temporaryList">The list to enumerate.</param>
        public Enumerator(TemporaryList<T> temporaryList)
        {
            this.temporaryList = temporaryList;
            this.index = -1;
            this.current = default;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public T Current => this.current!;

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (++this.index >= this.temporaryList.Count)
            {
                return false;
            }

            if (this.index >= StackObjectCount &&
                this.temporaryList.list is { } list)
            {
                var i = this.index - StackObjectCount;
                if (i < list.Count)
                {
                    this.current = list[i];
                    return true;
                }
            }
            else if (this.index == 0)
            {
                this.current = this.temporaryList.obj0;
                return true;
            }
            else if (this.index == 1)
            {
                this.current = this.temporaryList.obj1;
                return true;
            }
            else if (this.index == 2)
            {
                this.current = this.temporaryList.obj2;
                return true;
            }
            else if (this.index == 3)
            {
                this.current = this.temporaryList.obj3;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, before the first element.
        /// </summary>
        public void Reset()
        {
            this.index = -1;
            this.current = default;
        }
    }
}
