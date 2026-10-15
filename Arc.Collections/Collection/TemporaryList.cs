// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Arc.Collections;

/// <summary>
/// Provides an append-only list that stores its first four elements inline.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <remarks>
/// Additional elements use a heap-allocated overflow list, which copies of this struct share.
/// Do not copy it: once the overflow list exists, adding through more than one copy mixes their elements.
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
    /// Gets the number of elements in the list.
    /// </summary>
    public readonly int Count => this.count;

    /// <summary>
    /// Adds an element to the end of the list.
    /// </summary>
    /// <param name="item">The element to add.</param>
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
    /// Enumerates the elements of a <see cref="TemporaryList{T}"/> in insertion order.<br/>
    /// The enumerator holds a copy of the list; elements added after it is created are not enumerated.
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
        /// Gets the element at the current position of the enumerator.<br/>
        /// The value is undefined before the first call to <see cref="MoveNext"/> or after it returns <see langword="false"/>.
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
            var index = this.index + 1;
            var count = this.temporaryList.count;
            if (index >= count)
            {
                // Stop at the end so repeated calls cannot overflow the index.
                this.index = count;
                return false;
            }

            this.index = index;

            // An if chain rather than a switch keeps this method inlinable.
            if (index >= StackObjectCount)
            {
                // index < count, so the overflow list exists and holds this element.
                this.current = this.temporaryList.list![index - StackObjectCount];
            }
            else if (index == 0)
            {
                this.current = this.temporaryList.obj0;
            }
            else if (index == 1)
            {
                this.current = this.temporaryList.obj1;
            }
            else if (index == 2)
            {
                this.current = this.temporaryList.obj2;
            }
            else
            {
                this.current = this.temporaryList.obj3;
            }

            return true;
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
