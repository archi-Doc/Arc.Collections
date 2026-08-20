// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections.HotMethod;

namespace Arc.Collections;

/// <summary>
/// Represents a list of elements maintained in sorted order.
/// Duplicate elements are allowed and preserve their insertion order.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class OrderedList<T> : UnorderedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    public OrderedList()
        : this(0, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    public OrderedList(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    public OrderedList(IComparer<T>? comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    public OrderedList(int capacity, IComparer<T>? comparer)
        : base(capacity)
    {
        this.Comparer = comparer ?? Comparer<T>.Default;
        this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public OrderedList(IEnumerable<T> collection)
        : this(collection, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedList{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    /// <param name="comparer">The comparer to use for comparing elements.</param>
    public OrderedList(IEnumerable<T> collection, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(collection);

        this.Comparer = comparer ?? Comparer<T>.Default;
        this.HotMethod = HotMethodResolver.Get<T>(this.Comparer);

        var array = collection.ToArray();
        if (array.Length > 1)
        {
            Array.Sort(array, this.Comparer);
        }

        this.items = array;
        this.size = array.Length;
    }

    public IComparer<T> Comparer { get; }

    public IHotMethod<T>? HotMethod { get; }

    /// <summary>
    /// Adds an element while maintaining sorted order.
    /// Elements equal to existing elements are inserted after them.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public new void Add(T value)
    {
        this.Insert(this.UpperBoundExclusive(value), value);
    }

    /// <summary>
    /// Searches for the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// An index of a matching element, or the bitwise complement of its insertion index
    /// if no matching element exists.
    /// </returns>
    public int BinarySearch(T value)
    {
        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return hotMethod.BinarySearch(this.items, 0, this.size, value);
        }

        var comparer = this.Comparer;
        var min = 0;
        var max = this.size - 1;

        // Avoid boxing value types through IComparable<T>.
        if (!typeof(T).IsValueType &&
            ReferenceEquals(comparer, Comparer<T>.Default) &&
            value is IComparable<T> comparable)
        {
            while (min <= max)
            {
                var mid = min + ((max - min) >> 1);
                var comparison = comparable.CompareTo(this.items[mid]);

                if (comparison < 0)
                {
                    max = mid - 1;
                }
                else if (comparison > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
        }
        else
        {
            while (min <= max)
            {
                var mid = min + ((max - min) >> 1);
                var comparison = comparer.Compare(value, this.items[mid]);

                if (comparison < 0)
                {
                    max = mid - 1;
                }
                else if (comparison > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
        }

        return ~min;
    }

    /// <summary>
    /// Gets the index of the first element equal to or greater than the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The index, or -1 if all elements are less than the specified value.</returns>
    public int GetLowerBound(T value)
    {
        var index = this.LowerBound(value);
        return index < this.size ? index : -1;
    }

    /// <summary>
    /// Gets the index of the last element equal to or less than the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The index, or -1 if all elements are greater than the specified value.</returns>
    public int GetUpperBound(T value)
    {
        return this.UpperBoundExclusive(value) - 1;
    }

    /// <summary>
    /// Determines whether the specified value is in the list.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found.</returns>
    public new bool Contains(T value)
    {
        var index = this.LowerBound(value);
        return index < this.size && this.Comparer.Compare(this.items[index], value) == 0;
    }

    /// <summary>
    /// Removes the first occurrence of the specified value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was removed.</returns>
    public new bool Remove(T value)
    {
        var index = this.IndexOf(value);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    public new T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.items[index];
        }

        set => throw new InvalidOperationException("Elements cannot be replaced directly in an ordered list.");
    }

    /// <summary>
    /// Returns the index of the first occurrence of the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The first matching index, or -1 if the value is not found.</returns>
    public new int IndexOf(T value)
    {
        var index = this.LowerBound(value);

        if (index < this.size && this.Comparer.Compare(this.items[index], value) == 0)
        {
            return index;
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the first element not less than the specified value.
    /// </summary>
    private int LowerBound(T value)
    {
        var min = 0;
        var max = this.size;
        var comparer = this.Comparer;

        if (!typeof(T).IsValueType &&
            ReferenceEquals(comparer, Comparer<T>.Default) &&
            value is IComparable<T> comparable)
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparable.CompareTo(this.items[mid]) > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid;
                }
            }
        }
        else
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparer.Compare(this.items[mid], value) < 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid;
                }
            }
        }

        return min;
    }

    /// <summary>
    /// Returns the index of the first element greater than the specified value.
    /// </summary>
    private int UpperBoundExclusive(T value)
    {
        var min = 0;
        var max = this.size;
        var comparer = this.Comparer;

        if (!typeof(T).IsValueType &&
            ReferenceEquals(comparer, Comparer<T>.Default) &&
            value is IComparable<T> comparable)
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparable.CompareTo(this.items[mid]) >= 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid;
                }
            }
        }
        else
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparer.Compare(this.items[mid], value) <= 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid;
                }
            }
        }

        return min;
    }
}
