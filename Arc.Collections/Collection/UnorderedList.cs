// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable SA1401

namespace Arc.Collections;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1615 // Element return value should be documented

/// <summary>
/// Represents a list of objects that can be accessed by index.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class UnorderedList<T> : IList<T>, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;

    protected T[] items;
    protected int size;
    protected int version;

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
        if (capacity < 0 || capacity > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.items = capacity == 0 ? Array.Empty<T>() : new T[capacity];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnorderedList{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    public UnorderedList(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection is ICollection<T> c)
        {
            var count = c.Count;
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
            this.items = Array.Empty<T>();
            using var enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                this.Add(enumerator.Current);
            }
        }
    }

    #region ICollection

    public int Count => this.size;

    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an object to the end of the list.
    /// <br/>O(1) amortized operation.
    /// </summary>
    /// <param name="value">The value to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T value)
    {
        var size = this.size;
        var items = this.items;
        if ((uint)size < (uint)items.Length)
        {
            items[size] = value;
            this.size = size + 1;
            this.version++;
            return;
        }

        this.AddWithResize(value);
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="collection">The collection whose elements are added.</param>
    public void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection is ICollection<T> c)
        {
            var count = c.Count;
            if (count > 0)
            {
                var size = this.size;
                if (this.items.Length - size < count)
                {
                    this.Grow(size + count);
                }

                // Bulk copy; also safe when the collection is this instance, because the
                // source range [0, size) never overlaps the destination range [size, size + count).
                c.CopyTo(this.items, size);
                this.size = size + count;
            }

            this.version++;
        }
        else
        {
            foreach (var item in collection)
            {
                this.Add(item);
            }
        }
    }

    /// <summary>
    /// Adds the elements of the specified array to the end of the list.
    /// </summary>
    /// <param name="array">The array whose elements are added.</param>
    /// <remarks>This overload also disambiguates arrays, which convert to both
    /// <see cref="IEnumerable{T}"/> and <see cref="ReadOnlySpan{T}"/>.</remarks>
    public void AddRange(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        this.AddRange(new ReadOnlySpan<T>(array));
    }

    /// <summary>
    /// Adds the elements of the specified span to the end of the list.
    /// </summary>
    /// <param name="source">The span whose elements are added.</param>
    public void AddRange(ReadOnlySpan<T> source)
    {
        if (!source.IsEmpty)
        {
            var size = this.size;
            if (this.items.Length - size < source.Length)
            {
                this.Grow(size + source.Length);
            }

            source.CopyTo(this.items.AsSpan(size));
            this.size = size + source.Length;
        }

        this.version++;
    }

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    public void Clear()
    {
        var size = this.size;
        if (size > 0)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(this.items, 0, size);
            }

            this.size = 0;
        }

        this.version++;
    }

    /// <summary>
    /// Determines whether an element is in the list.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T value) => this.IndexOf(value) >= 0;

    /// <summary>
    /// Copies the elements to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based destination index.</param>
    public void CopyTo(T[] array, int arrayIndex)
        => Array.Copy(this.items, 0, array, arrayIndex, this.size);

    /// <summary>
    /// Copies the elements to an array.
    /// </summary>
    /// <param name="array">The destination array.</param>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    /// <summary>
    /// Copies the elements to a new array.
    /// </summary>
    public T[] ToArray()
    {
        var size = this.size;
        if (size == 0)
        {
            return Array.Empty<T>();
        }

        var array = new T[size];
        Array.Copy(this.items, array, size);
        return array;
    }

    /// <summary>
    /// Removes the first occurrence of the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T value)
    {
        var index = this.IndexOf(value);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    #endregion

    #region IList

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)this.size)
            {
                ThrowIndexOutOfRange();
            }

            return this.items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)this.size)
            {
                ThrowIndexOutOfRange();
            }

            this.items[index] = value;
            this.version++;
        }
    }

    /// <summary>
    /// Returns the zero-based index of the first occurrence of the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The zero-based index if found; otherwise, -1.</returns>
    public int IndexOf(T value)
        => Array.IndexOf(this.items, value, 0, this.size);

    /// <summary>
    /// Inserts an element at the specified index.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="item">The value to insert.</param>
    public void Insert(int index, T item)
    {
        var size = this.size;
        if ((uint)index > (uint)size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (size == this.items.Length)
        {
            this.Grow(size + 1);
        }

        if (index < size)
        {
            Array.Copy(this.items, index, this.items, index + 1, size - index);
        }

        this.items[index] = item;
        this.size = size + 1;
        this.version++;
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        var size = this.size;
        if ((uint)index >= (uint)size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        size--;
        if (index < size)
        {
            Array.Copy(this.items, index + 1, this.items, index, size - index);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            this.items[size] = default!;
        }

        this.size = size;
        this.version++;
    }

    #endregion

    /// <summary>
    /// Gets or sets the number of elements the internal array can hold without resizing.
    /// </summary>
    public int Capacity
    {
        get => this.items.Length;
        set
        {
            if ((uint)value > (uint)Array.MaxLength || value < this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == this.items.Length)
            {
                return;
            }

            if (value == 0)
            {
                this.items = Array.Empty<T>();
                return;
            }

            var newItems = new T[value];
            if (this.size > 0)
            {
                Array.Copy(this.items, 0, newItems, 0, this.size);
            }

            this.items = newItems;
        }
    }

    /// <summary>
    /// Ensures that the capacity is at least the specified value.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>The new capacity.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (this.items.Length < capacity)
        {
            this.Grow(capacity);
        }

        return this.items.Length;
    }

    /// <summary>
    /// Sets the capacity to the actual number of elements, if that is less than 90% of the current capacity.
    /// </summary>
    public void TrimExcess()
    {
        var threshold = (int)(this.items.Length * 0.9);
        if (this.size < threshold)
        {
            this.Capacity = this.size;
        }
    }

    /// <summary>
    /// Gets a span over the elements currently in the list.
    /// This is the fastest way to enumerate the list (no per-element version checks).
    /// <br/>The span is invalidated by any operation that adds, inserts, removes, or changes capacity;
    /// do not use it after such an operation, and do not add or remove elements while holding it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => new(this.items, 0, this.size);

    /// <summary>
    /// Gets a read-only span over the elements currently in the list.
    /// See <see cref="AsSpan"/> for the invalidation rules.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => new(this.items, 0, this.size);

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the elements of an <see cref="UnorderedList{T}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        // The array and size are snapshotted so the hot path reads at most one field
        // (version) through the list reference per MoveNext, instead of three
        // (version, size, items). Any mutation changes the version, so a stale
        // snapshot is never observed.
        private readonly UnorderedList<T> list;
        private readonly T[] items;
        private readonly int size;
        private readonly int version;
        private int index;
        private T current;

        internal Enumerator(UnorderedList<T> list)
        {
            this.list = list;
            this.items = list.items;
            this.size = list.size;
            this.version = list.version;
            this.index = 0;
            this.current = default!;
        }

        public readonly T Current => this.current;

        object? IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.size + 1)
                {
                    ThrowInvalidEnumeratorState();
                }

                return this.current;
            }
        }

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = this.index;
            if (this.version == this.list.version &&
                (uint)index < (uint)this.size)
            {
                this.current = this.items[index];
                this.index = index + 1;
                return true;
            }

            return this.MoveNextRare();
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                ThrowVersionMismatch();
            }

            this.index = 0;
            this.current = default!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool MoveNextRare()
        {
            if (this.version != this.list.version)
            {
                ThrowVersionMismatch();
            }

            this.index = this.size + 1;
            this.current = default!;
            return false;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowVersionMismatch()
            => throw new InvalidOperationException(
                "List was modified after the enumerator was instantiated.");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidEnumeratorState()
            => throw new InvalidOperationException(
                "Enumeration has either not started or has already finished.");
    }

    #endregion

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T value)
    {
        var size = this.size;
        this.Grow(size + 1);
        this.items[size] = value;
        this.size = size + 1;
        this.version++;
    }

    private void Grow(int min)
    {
        if ((uint)min > (uint)Array.MaxLength)
        {
            ThrowCapacityExceeded();
        }

        var newCapacity = this.items.Length == 0
            ? DefaultCapacity
            : this.items.Length * 2;

        // The uint comparison also handles integer overflow from doubling.
        if ((uint)newCapacity > (uint)Array.MaxLength)
        {
            newCapacity = Array.MaxLength;
        }

        if (newCapacity < min)
        {
            newCapacity = min;
        }

        this.Capacity = newCapacity;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange()
        => throw new ArgumentOutOfRangeException("index");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowCapacityExceeded()
        => throw new InvalidOperationException("The list has reached its maximum capacity.");
}
