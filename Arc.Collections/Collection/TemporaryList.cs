// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Arc.Collections;

/// <summary>
/// A temporary list implemented as a ref struct.<br/>
/// Up to four objects are stored inline without an additional heap allocation.<br/>
/// Use this mainly when you want to modify objects after iterating a collection in a 'for' or 'foreach' loop.
/// </summary>
/// <typeparam name="TObject">The type of the objects.</typeparam>
public ref struct TemporaryList<TObject> // : IEnumerable<TObject>, IEnumerable // ref struct types cannot implement interfaces or be boxed.
{
    private const int StackObjectCount = 4;

    private int count;
    private TObject obj0;
    private TObject obj1;
    private TObject obj2;
    private TObject obj3;
    private List<TObject>? list;

    /// <summary>
    /// Gets the number of objects in the list.
    /// </summary>
    public readonly int Count => this.count;

    /// <summary>
    /// Adds an object to the list.
    /// </summary>
    /// <param name="obj">The object to add to the list.</param>
    public void Add(TObject obj)
    {
        if (this.count == 0)
        {
            this.count = 1;
            this.obj0 = obj;
            return;
        }
        else if (this.count == 1)
        {
            this.count = 2;
            this.obj1 = obj;
            return;
        }
        else if (this.count == 2)
        {
            this.count = 3;
            this.obj2 = obj;
            return;
        }
        else if (this.count == 3)
        {
            this.count = 4;
            this.obj3 = obj;
            return;
        }

        this.list ??= new();
        this.list.Add(obj);
        this.count++;
    }

    /// <summary>
    /// Copies the current contents of this temporary list to a new array.
    /// </summary>
    /// <returns>
    /// A new array containing all items in insertion order.<br/>
    /// Returns an empty array when the list contains no items.
    /// </returns>
    public readonly TObject[] ToArray()
    {
        var count = this.count;
        if (count == 0)
        {
            return [];
        }

        var array = new TObject[count];
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
            this.list!.CopyTo(array, StackObjectCount);
        }

        return array;
    }

    public readonly Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator
    {
        private readonly TemporaryList<TObject> temporaryList;
        private int index;
        private TObject? current;

        public Enumerator(TemporaryList<TObject> temporaryList)
        {
            this.temporaryList = temporaryList;
            this.index = -1;
            this.current = default;
        }

        public TObject Current => this.current!;

        public void Dispose()
        {
        }

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

        public void Reset()
        {
            this.index = -1;
            this.current = default;
        }
    }
}
