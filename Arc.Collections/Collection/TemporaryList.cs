﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Arc.Collections;

/// <summary>
/// A list of temporary objects created using a ref struct.<br/>
/// If the count is 4 or fewer, it avoids creating a <see cref="List{T}"/> and keeps the objects on the stack.<br/>
/// It is primarily used when you need to manipulate a collection after exiting a for or foreach loop.
/// </summary>
/// <typeparam name="TObject">The type of the objects.</typeparam>
public ref struct TemporaryList<TObject>
    where TObject : class
{
    private const int FieldSize = 4;

    private TObject? obj0;
    private TObject? obj1;
    private TObject? obj2;
    private TObject? obj3;
    private List<TObject>? list;

    /// <summary>
    /// Gets the number of objects in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            if (this.obj0 is null)
            {
                return 0;
            }
            else if (this.obj1 is null)
            {
                return 1;
            }
            else if (this.obj2 is null)
            {
                return 2;
            }
            else if (this.obj3 is null)
            {
                return 3;
            }
            else
            {
                if (this.list is null)
                {
                    return FieldSize;
                }
                else
                {
                    return FieldSize + this.list.Count;
                }
            }
        }
    }

    /// <summary>
    /// Adds an object to the list.
    /// </summary>
    /// <param name="obj">The object to add to the queue.</param>
    public void Add(TObject obj)
    {
        if (this.obj0 is null)
        {
            this.obj0 = obj;
            return;
        }

        if (this.obj1 is null)
        {
            this.obj1 = obj;
            return;
        }

        if (this.obj2 is null)
        {
            this.obj2 = obj;
            return;
        }

        if (this.obj3 is null)
        {
            this.obj3 = obj;
            return;
        }

        this.list ??= new();
        this.list.Add(obj);
    }

    /*
    /// <summary>
    /// Clears all objects from the queue.<br/>
    /// This struct exists on the stack, so there is no need to actively call Clear().
    /// </summary>
    public void Clear()
    {
        this.obj0 = default;
        this.obj1 = default;
        this.obj2 = default;
        this.obj3 = default;
        if (this.list is not null)
        {
            this.list.Clear();
            this.list = default;
        }
    }*/

    public Enumerator GetEnumerator() => new Enumerator(this);

    public ref struct Enumerator : IEnumerator<TObject>
    {
        private readonly TemporaryList<TObject> queue;
        private int index;
        private TObject? current;

        public Enumerator(TemporaryList<TObject> queue)
        {
            this.queue = queue;
            this.index = -1;
            this.current = default;
        }

        public TObject Current => this.current!;

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            this.index++;
            if (this.index >= FieldSize)
            {
                if (this.queue.list is { } list)
                {
                    var i = this.index - FieldSize;
                    if (i < list.Count)
                    {
                        this.current = list[i];
                        return true;
                    }
                }
            }
            else if (this.index == 0)
            {
                this.current = this.queue.obj0;
                return this.queue.obj0 is not null;
            }
            else if (this.index == 1)
            {
                this.current = this.queue.obj1;
                return this.queue.obj1 is not null;
            }
            else if (this.index == 2)
            {
                this.current = this.queue.obj2;
                return this.queue.obj2 is not null;
            }
            else if (this.index == 3)
            {
                this.current = this.queue.obj3;
                return this.queue.obj3 is not null;
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
