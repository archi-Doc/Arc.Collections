// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1204 // Static elements should appear before instance elements

/// <summary>
/// Represents an explicitly resizable ring buffer whose elements are identified by a <b>position</b>
/// instead of a physical index.
/// </summary>
/// <typeparam name="T">The type of the elements. Must be a reference type, because <see langword="null"/> is used internally to mark an empty slot.</typeparam>
/// <remarks>
/// A position is a 31-bit unsigned value (<c>0</c> to <see cref="int.MaxValue"/>) that increases as elements are
/// added and wraps around to <c>0</c> after <see cref="int.MaxValue"/>. Positions returned by <see cref="Add(T)"/>
/// remain valid until the element leaves the window, so they can be stored and used later as stable handles.<br/>
/// The window of valid positions is <c>[<see cref="StartPosition"/>, StartPosition + <see cref="Capacity"/>)</c>.
/// It advances when the elements at the head are removed (see <see cref="TrySlide"/>), which discards the
/// positions that fall behind it.<br/>
/// Removing an element from the middle leaves a hole. <see cref="Consumed"/> counts the slots in use including
/// holes, whereas <see cref="ICollection{T}.Count"/> counts only the live elements; enumeration,
/// <see cref="ToArray"/> and <see cref="CopyTo(T[], int)"/> skip holes.<br/>
/// <b>Note:</b> because this class addresses elements by position, the whole <see cref="IList{T}"/> surface
/// (<see cref="this[int]"/>, <see cref="IndexOf(T)"/>, <see cref="Insert(int, T)"/>, <see cref="RemoveAt(int)"/>)
/// takes and returns positions rather than zero-based indexes.<br/>
/// Instance members are not thread-safe.
/// </remarks>
public class SlidingList<T> : IList<T>, IReadOnlyList<T>
    where T : class
{
    private const int PositionMask = 0x7FFFFFFF;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingList{T}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the list can hold. The capacity is fixed unless <see cref="Resize(int)"/> is called.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public SlidingList(int capacity)
    {
        if (capacity < 0)
        {
            throw ThrowHelper.CapacityOutOfRange();
        }

        this.items = capacity == 0 ? Array.Empty<T?>() : new T?[capacity];
    }

    #region FieldAndProperty

    private T?[] items;
    private int startPosition; // The position of items[headIndex] (always masked, 0..PositionMask).
    private int headIndex; // The head index in items (the first used item).
    private int consumed; // The number of slots (including holes) occupied from headIndex.
    private int count; // The number of live (non-null) elements.
    private int version;

    /// <summary>
    /// Gets the position of the first slot in use, which is also the lower bound of the valid position window.
    /// </summary>
    /// <remarks>When the list is empty this is the position the next added element will receive.</remarks>
    public int StartPosition => this.startPosition;

    /// <summary>
    /// Gets the position one past the last slot in use, that is, the exclusive end of <see cref="Consumed"/>.
    /// </summary>
    /// <remarks>This is the position that <see cref="Add(T)"/> will return next, provided <see cref="CanAdd"/> is <see langword="true"/>.</remarks>
    public int EndPosition => PositionMask & (this.startPosition + this.consumed);

    /// <summary>
    /// Gets the maximum number of elements that the <see cref="SlidingList{T}"/> can hold, and the size of the position window.
    /// </summary>
    public int Capacity => this.items.Length;

    /// <summary>
    /// Gets the number of slots in use, counting both live elements and the holes left by removed elements.
    /// </summary>
    /// <remarks>This is the distance from <see cref="StartPosition"/> to <see cref="EndPosition"/>, and never exceeds <see cref="Capacity"/>.</remarks>
    public int Consumed => this.consumed;

    /// <summary>
    /// Gets a value indicating whether the <see cref="SlidingList{T}"/> has a free slot, and therefore whether <see cref="Add(T)"/> will succeed.
    /// </summary>
    /// <remarks>A hole in the middle of the window does not count as free space; only <see cref="TrySlide"/> reclaims slots.</remarks>
    public bool CanAdd => this.consumed < this.items.Length;

    /// <summary>
    /// Gets the number of live elements, excluding the holes counted by <see cref="Consumed"/>.
    /// </summary>
    int ICollection<T>.Count => this.count;

    /// <inheritdoc cref="ICollection{T}.Count"/>
    int IReadOnlyCollection<T>.Count => this.count;

    /// <summary>
    /// Gets the element at <see cref="StartPosition"/>, or <see langword="null"/> if the <see cref="SlidingList{T}"/> is empty.
    /// </summary>
    /// <remarks>
    /// <b>This getter is not read-only:</b> it calls <see cref="TrySlide"/> to drop any leading holes, which may
    /// advance <see cref="StartPosition"/> and invalidate outstanding enumerators.
    /// </remarks>
    public T? FirstOrDefault
    {
        get
        {
            if (this.consumed == 0)
            {
                return null;
            }

            this.TrySlide();
            return this.items[this.headIndex];
        }
    }

    #endregion

    /// <summary>
    /// Copies the live elements to a new array, in order from <see cref="StartPosition"/>, skipping holes.
    /// <br/>O(n) operation.
    /// </summary>
    /// <returns>A new array of length <see cref="ICollection{T}.Count"/>, or an empty array if there is no live element.</returns>
    public T[] ToArray()
    {
        var count = this.count;
        if (count == 0)
        {
            return Array.Empty<T>();
        }

        var array = new T[count];
        if (count == this.consumed)
        {// No holes: bulk copy (1 or 2 memmove).
            CopyWindow(this.items, this.headIndex, count, array!);
            return array;
        }

        var items = this.items;
        var length = items.Length;
        var i = this.headIndex;
        var j = 0;
        for (var remaining = this.consumed; remaining > 0; remaining--)
        {
            var item = items[i];
            if (++i == length)
            {
                i = 0;
            }

            if (item is not null)
            {
                array[j] = item;
                if (++j == count)
                {
                    break;
                }
            }
        }

        return array;
    }

    /// <summary>
    /// Changes the <see cref="Capacity"/> of the <see cref="SlidingList{T}"/>, preserving the elements and their positions.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="capacity">The new capacity. It must be at least <see cref="Consumed"/>.</param>
    /// <returns><see langword="true"/> if the capacity was changed or already equal to <paramref name="capacity"/>;
    /// <see langword="false"/> if <paramref name="capacity"/> is smaller than <see cref="Consumed"/>, in which case the list is left untouched.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public bool Resize(int capacity)
    {
        if (capacity < 0)
        {
            throw ThrowHelper.CapacityOutOfRange();
        }

        var items = this.items;
        if (items.Length == capacity)
        {// Identical
            return true;
        }
        else if (this.consumed > capacity)
        {
            return false;
        }

        var array = capacity == 0 ? Array.Empty<T?>() : new T?[capacity];
        CopyWindow(items, this.headIndex, this.consumed, array);
        this.items = array;
        this.headIndex = 0; // startPosition is unchanged: it still designates items[0].
        this.version++;
        return true;
    }

    /// <summary>
    /// Advances the window past the holes at its head, freeing those slots for <see cref="Add(T)"/>.
    /// </summary>
    /// <returns>The number of slots the window advanced by; <c>0</c> if the first slot holds a live element or the list is empty.</returns>
    /// <remarks>The positions skipped this way become invalid. A non-zero result invalidates outstanding enumerators.</remarks>
    public int TrySlide()
    {
        var consumed = this.consumed;
        if (consumed == 0)
        {
            return 0;
        }

        var items = this.items;
        var length = items.Length;
        var i = this.headIndex;
        var n = 0;
        while (items[i] is null)
        {
            if (++i == length)
            {
                i = 0;
            }

            if (++n == consumed)
            {
                break;
            }
        }

        if (n == 0)
        {
            return 0;
        }

        this.headIndex = i;
        this.startPosition = PositionMask & (this.startPosition + n);
        this.consumed = consumed - n;
        this.version++;
        return n;
    }

    /// <summary>
    /// Adds an element at <see cref="EndPosition"/>, at the tail of the window.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>The position of the new element, or <c>-1</c> if the list is full (see <see cref="CanAdd"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public int Add(T value)
    {
        if (value is null)
        {
            throw ThrowHelper.ValueNull();
        }

        var consumed = this.consumed;
        var items = this.items;
        if (consumed >= items.Length)
        {// No space
            return -1;
        }

        var i = this.headIndex + consumed;
        if (i >= items.Length)
        {
            i -= items.Length;
        }

        items[i] = value;
        this.consumed = consumed + 1;
        this.count++;
        this.version++;
        return PositionMask & (this.startPosition + consumed);
    }

    /// <summary>
    /// Removes the element at the specified position, leaving a hole unless it is the first slot.
    /// </summary>
    /// <param name="position">The position of the element to remove.</param>
    /// <returns><see langword="true"/> if an element was removed; <see langword="false"/> if <paramref name="position"/>
    /// is outside the window or the slot is already empty.</returns>
    /// <remarks>Removing the element at <see cref="StartPosition"/> also calls <see cref="TrySlide"/>.</remarks>
    public bool Remove(int position)
    {
        var offset = this.PositionToOffset(position);
        if (offset < 0)
        {
            return false;
        }

        var index = this.OffsetToIndex(offset);
        if (this.items[index] is null)
        {
            return false;
        }

        this.items[index] = null;
        this.count--;
        this.version++;
        if (offset == 0)
        {
            this.TrySlide();
        }

        return true;
    }

    /// <summary>
    /// Gets the element at the specified position.
    /// </summary>
    /// <param name="position">The position of the element.</param>
    /// <returns>The element, or <see langword="null"/> if <paramref name="position"/> is outside the window or the slot is empty.</returns>
    public T? Get(int position)
    {
        var offset = this.PositionToOffset(position);
        return offset < 0 ? null : this.items[this.OffsetToIndex(offset)];
    }

    /// <summary>
    /// Sets the element at the specified position, which may be any position inside the window, not only one already in use.
    /// </summary>
    /// <param name="position">The position of the element.</param>
    /// <param name="value">The value to store.</param>
    /// <returns><see langword="true"/> if the value was stored; <see langword="false"/> if <paramref name="position"/> is outside the window.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>Setting a position beyond <see cref="EndPosition"/> extends <see cref="Consumed"/> and leaves the slots in between as holes.</remarks>
    public bool Set(int position, T value)
    {
        if (value is null)
        {
            throw ThrowHelper.ValueNull();
        }

        var offset = this.PositionToOffset(position);
        if (offset < 0)
        {
            return false;
        }

        var index = this.OffsetToIndex(offset);
        if (this.items[index] is null)
        {
            this.count++;
        }

        this.items[index] = value;
        if (offset >= this.consumed)
        {
            this.consumed = offset + 1;
        }

        this.version++;
        return true;
    }

    /// <summary>
    /// Converts a position into an offset from <see cref="StartPosition"/>.
    /// </summary>
    /// <param name="position">The position to convert.</param>
    /// <returns>The offset in the range <c>[0, Capacity)</c>, or <c>-1</c> if <paramref name="position"/> is outside the window.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PositionToOffset(int position)
    {
        if (position < 0)
        {
            return -1;
        }

        // (position - startPosition) modulo 2^31. Always non-negative.
        var offset = PositionMask & (position - this.startPosition);
        return offset < this.items.Length ? offset : -1;
    }

    /// <summary>
    /// Converts an offset from <see cref="StartPosition"/> into an index in <see cref="items"/>.
    /// </summary>
    /// <param name="offset">The offset, which must be in the range <c>[0, Capacity)</c>.</param>
    /// <returns>The corresponding index in <see cref="items"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int OffsetToIndex(int offset)
    {
        var index = this.headIndex + offset;
        return index < this.items.Length ? index : index - this.items.Length;
    }

    /// <summary>
    /// Copies a window of a ring buffer to the head of a destination array, unwrapping it into one or two block copies.
    /// </summary>
    /// <param name="source">The ring buffer to copy from.</param>
    /// <param name="head">The index in <paramref name="source"/> at which the window starts.</param>
    /// <param name="consumed">The number of slots to copy. It must not exceed the length of either array.</param>
    /// <param name="destination">The array to copy to.</param>
    private static void CopyWindow(T?[] source, int head, int consumed, T?[] destination)
    {
        if (consumed == 0)
        {
            return;
        }

        var first = source.Length - head;
        if (first >= consumed)
        {
            Array.Copy(source, head, destination, 0, consumed);
        }
        else
        {
            Array.Copy(source, head, destination, 0, first);
            Array.Copy(source, 0, destination, first, consumed - first);
        }
    }

    #region ICollection

    /// <summary>
    /// Gets a value indicating whether the <see cref="SlidingList{T}"/> is read-only. Always <see langword="false"/>.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Removes all elements from the <see cref="SlidingList{T}"/>.
    /// </summary>
    /// <remarks>Positions are not reset: the empty window starts at the former <see cref="EndPosition"/>, so positions
    /// handed out before the call are never reused.</remarks>
    public void Clear()
    {
        Array.Clear(this.items, 0, this.items.Length);
        this.startPosition = PositionMask & (this.startPosition + this.consumed);
        this.headIndex = 0;
        this.consumed = 0;
        this.count = 0;
        this.version++;
    }

    /// <summary>
    /// Determines whether an element is in the <see cref="SlidingList{T}"/>, using <see cref="EqualityComparer{T}.Default"/>.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is found; otherwise, <see langword="false"/>.
    /// A <see langword="null"/> argument always returns <see langword="false"/>.</returns>
    public bool Contains(T value) => this.IndexOf(value) >= 0;

    /// <summary>
    /// Copies the live elements to an array, in order from <see cref="StartPosition"/>, skipping holes.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the copied elements.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="arrayIndex"/> is out of range, or <paramref name="array"/> has
    /// less than <see cref="ICollection{T}.Count"/> elements available from <paramref name="arrayIndex"/>.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array is null)
        {
            throw ThrowHelper.ArrayNull();
        }

        var count = this.count;
        if ((uint)arrayIndex > (uint)array.Length || array.Length - arrayIndex < count)
        {
            throw ThrowHelper.ArrayTooSmall();
        }

        if (count == 0)
        {
            return;
        }

        if (count == this.consumed)
        {// No holes: bulk copy.
            var items = this.items;
            var head = this.headIndex;
            var first = items.Length - head;
            if (first >= count)
            {
                Array.Copy(items, head, array, arrayIndex, count);
            }
            else
            {
                Array.Copy(items, head, array, arrayIndex, first);
                Array.Copy(items, 0, array, arrayIndex + first, count - first);
            }

            return;
        }

        var source = this.items;
        var length = source.Length;
        var i = this.headIndex;
        var j = arrayIndex;
        for (var remaining = this.consumed; remaining > 0; remaining--)
        {
            var item = source[i];
            if (++i == length)
            {
                i = 0;
            }

            if (item is not null)
            {
                array[j++] = item;
                if (j - arrayIndex == count)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Copies the live elements to the beginning of an array, in order from <see cref="StartPosition"/>, skipping holes.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the copied elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="array"/> has less than <see cref="ICollection{T}.Count"/> elements.</exception>
    public void CopyTo(T[] array) => this.CopyTo(array, 0);

    /// <summary>
    /// Removes the first occurrence of a specific object, searching from <see cref="StartPosition"/>.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The object to remove.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(T value)
    {
        var position = this.IndexOf(value);
        return position >= 0 && this.Remove(position);
    }

    /// <summary>
    /// Adds an element at <see cref="EndPosition"/>. The element is silently dropped if the list is full;
    /// use <see cref="Add(T)"/> to detect that case.
    /// </summary>
    /// <param name="item">The value to add.</param>
    void ICollection<T>.Add(T item)
        => this.Add(item);

    #endregion

    #region IList

    /// <summary>
    /// Gets or sets the element at the specified position. The parameter is a position, not a zero-based index.
    /// </summary>
    /// <param name="position">The position of the element.</param>
    /// <returns>The element at <paramref name="position"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is outside the window, or, when getting,
    /// the slot is empty. Use <see cref="Get(int)"/> or <see cref="Set(int, T)"/> to test instead of throwing.</exception>
    /// <exception cref="ArgumentNullException">The value being set is <see langword="null"/>.</exception>
    public T this[int position]
    {
        get => this.Get(position) ?? throw ThrowHelper.PositionOutOfRange();
        set
        {
            if (!this.Set(position, value))
            {
                throw ThrowHelper.PositionOutOfRange();
            }
        }
    }

    /// <summary>
    /// Searches from <see cref="StartPosition"/> and returns the position of the first occurrence of a value,
    /// using <see cref="EqualityComparer{T}.Default"/>. The result is a position, not a zero-based index.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>The position of the first occurrence of <paramref name="value"/>, or <c>-1</c> if it is not found
    /// or <paramref name="value"/> is <see langword="null"/>.</returns>
    public int IndexOf(T value)
    {
        if (value is null)
        {
            return -1;
        }

        var items = this.items;
        var length = items.Length;
        var consumed = this.consumed;
        var i = this.headIndex;
        var comparer = EqualityComparer<T>.Default;
        for (var offset = 0; offset < consumed; offset++)
        {
            var item = items[i];
            if (++i == length)
            {
                i = 0;
            }

            if (item is not null && comparer.Equals(item, value))
            {
                return PositionMask & (this.startPosition + offset);
            }
        }

        return -1;
    }

    /// <summary>
    /// Stores an element at the specified position. Unlike <see cref="IList{T}.Insert(int, T)"/>, this overwrites
    /// the slot and does not shift the subsequent elements; it is equivalent to <see cref="Set(int, T)"/>.
    /// </summary>
    /// <param name="position">The position at which the item is stored.</param>
    /// <param name="item">The object to store.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is outside the window.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    public void Insert(int position, T item) => this[position] = item;

    /// <summary>
    /// Removes the element at the specified position. The parameter is a position, not a zero-based index;
    /// it is <see cref="Remove(int)"/> with an exception instead of a <see langword="false"/> result.
    /// </summary>
    /// <param name="position">The position of the element to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is outside the window, or the slot is already empty.</exception>
    public void RemoveAt(int position)
    {
        if (!this.Remove(position))
        {
            throw ThrowHelper.PositionOutOfRange();
        }
    }

    #endregion

    #region Enumerator

    /// <summary>
    /// Returns an enumerator that iterates the live elements in order from <see cref="StartPosition"/>, skipping holes.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> for the <see cref="SlidingList{T}"/>.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates live elements in window order, skipping holes.
    /// </summary>
    /// <remarks>
    /// Changes to the contents or window invalidate the enumerator.
    /// </remarks>
    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly SlidingList<T> list;
        private int index; // The index in items.
        private int remaining; // The number of slots left to scan.
        private int version;
        private T? current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="list">The list to enumerate.</param>
        internal Enumerator(SlidingList<T> list)
        {
            this.list = list;
            this.index = list.headIndex;
            this.remaining = list.consumed;
            this.version = list.version;
            this.current = null;
        }

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Advances the enumerator to the next live element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator moved to the next element; <see langword="false"/> if it passed the end.</returns>
        /// <exception cref="InvalidOperationException">The list was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            var list = this.list;
            if (this.version != list.version)
            {
                throw ThrowHelper.VersionMismatch();
            }

            var items = list.items;
            var length = items.Length;
            var i = this.index;
            var remaining = this.remaining;
            while (remaining > 0)
            {
                var item = items[i];
                if (++i == length)
                {
                    i = 0;
                }

                remaining--;
                if (item is not null)
                {
                    this.index = i;
                    this.remaining = remaining;
                    this.current = item;
                    return true;
                }
            }

            this.index = i;
            this.remaining = 0;
            this.current = null;
            return false;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        /// <remarks>The value is undefined before the first <see cref="MoveNext"/> and after it returns <see langword="false"/>.</remarks>
        public T Current => this.current!;

        /// <inheritdoc cref="Current"/>
        object IEnumerator.Current => this.current!;

        /// <summary>
        /// Sets the enumerator back to its initial position, before the first element.
        /// </summary>
        /// <exception cref="InvalidOperationException">The list was modified after the enumerator was created.</exception>
        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw ThrowHelper.VersionMismatch();
            }

            this.index = this.list.headIndex;
            this.remaining = this.list.consumed;
            this.current = null;
        }
    }

    #endregion

    /// <summary>
    /// Exception factories. Keeping them out of line keeps the hot paths small enough for the JIT to inline.
    /// </summary>
    private static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception VersionMismatch()
            => new InvalidOperationException("List was modified after the enumerator was instantiated.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CapacityOutOfRange()
            => new ArgumentOutOfRangeException("capacity", "Capacity must be a non-negative value.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception ValueNull()
            => new ArgumentNullException("value");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception ArrayNull()
            => new ArgumentNullException("array");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception ArrayTooSmall()
            => new ArgumentException("The destination array is too small.", "array");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception PositionOutOfRange()
            => new ArgumentOutOfRangeException("position");
    }
}
