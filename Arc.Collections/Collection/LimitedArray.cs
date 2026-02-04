// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Arc.Collections;

public class LimitedArray<T>
    where T : class
{
    private readonly Lock syncObject = new();
    private T[] values;

    public LimitedArray()
    {
        this.values = [];
    }

    public LimitedArray(IEnumerable<T> values)
    {
        this.values = values.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] GetValues()
        => Volatile.Read(ref this.values);

    public void Add(T value)
    {
        using (this.syncObject.EnterScope())
        {
            T[] newArray = new T[this.values.Length + 1];
            Array.Copy(this.values, newArray, this.values.Length);
            newArray[^1] = value;
            Volatile.Write(ref this.values, newArray);
        }
    }

    public bool Remove(Func<T, bool> predicate)
    {
        using (this.syncObject.EnterScope())
        {
            for (var i = 0; i < this.values.Length; i++)
            {
                if (predicate(this.values[i]))
                {
                    this.values[i] = this.values[this.values.Length - 1];
                    Array.Resize(ref this.values, this.values.Length - 1);
                    return true;

                }
            }
        }

        return false;
    }
}
