// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Arc.Collections.HotMethod;

namespace Arc.Collections;

#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

/// <summary>
/// Represents a list of elements maintained in sorted order.
/// Duplicate elements are allowed and preserve their insertion order.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
/// <remarks>
/// The members that would break the sort order (<see cref="Insert(int, T)"/> and the
/// <see cref="this[int]"/> setter) throw <see cref="InvalidOperationException"/>.<br/>
/// <see cref="IList{T}"/> is re-implemented so that interface calls also honor the sort order.
/// Mutating an instance through an <see cref="UnorderedList{T}"/>-typed reference bypasses that
/// and can corrupt the order. Writes through <see cref="UnorderedList{T}.AsSpan"/> also bypass ordering.
/// </remarks>
public class OrderedList<T> : UnorderedList<T>, IList<T>, IReadOnlyList<T>
{
    // Below this many added elements, inserting each one is cheaper than sorting the batch first.
    private const int MergeThreshold = 16;

    // When T implements IComparable<T>, Comparer<T>.Default orders exactly like the IComparable<T> search path.
    // Otherwise (e.g., an interface), elements may still implement it and the search uses their implementation.
    private static readonly bool TypeIsComparable = typeof(IComparable<T>).IsAssignableFrom(typeof(T));

    // Cached so hot paths can select a comparison strategy without a ReferenceEquals per call.
    private readonly bool comparerIsDefault;

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
        this.comparerIsDefault = ReferenceEquals(this.Comparer, Comparer<T>.Default);
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
        : this(0, comparer)
    {
        ArgumentNullException.ThrowIfNull(collection);

        // The source cannot observe a list under construction, so even a lazy one is added in bulk.
        base.AddRange(collection);
        this.MergeAppended(0);
    }

    /// <summary>
    /// Gets the comparer used to order the elements.
    /// </summary>
    public IComparer<T> Comparer { get; }

    /// <summary>
    /// Gets the specialized comparison implementation for <typeparamref name="T"/>,
    /// or <see langword="null"/> when none is available.
    /// </summary>
    public IHotMethod<T>? HotMethod { get; }

    /// <summary>
    /// Adds an element while maintaining sorted order.
    /// Elements equal to existing elements are inserted after them.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public new void Add(T value)
    {
        base.Insert(this.UpperBoundExclusiveCore(value, 0, this.size), value);
    }

    /// <summary>
    /// Adds the elements of the specified collection while maintaining sorted order.
    /// </summary>
    /// <param name="collection">The collection whose elements are added.</param>
    public new void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection is ICollection<T> c)
        {
            if (c.Count > 0)
            {
                var start = this.size;
                base.AddRange(c); // Bulk copy; also handles a collection that is this list.
                this.MergeAppended(start);
            }
        }
        else
        {
            // A lazy source may read this list while it is enumerated, so it must stay sorted after each element.
            foreach (var x in collection)
            {
                this.Add(x);
            }
        }
    }

    /// <summary>
    /// Adds the elements of the specified array while maintaining sorted order.
    /// </summary>
    /// <param name="array">The array whose elements are added.</param>
    public new void AddRange(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        this.AddRange(new ReadOnlySpan<T>(array));
    }

    /// <summary>
    /// Adds the elements of the specified span while maintaining sorted order.
    /// </summary>
    /// <param name="source">The span whose elements are added.</param>
    public new void AddRange(ReadOnlySpan<T> source)
    {
        if (!source.IsEmpty)
        {
            var start = this.size;
            base.AddRange(source); // Copies before any element is moved, so the source may overlap this list.
            this.MergeAppended(start);
        }
    }

    /// <summary>
    /// Not supported: inserting at an arbitrary index would break the sort order.
    /// Use <see cref="Add(T)"/> instead.
    /// </summary>
    /// <param name="index">Not used.</param>
    /// <param name="item">The value that would have been inserted.</param>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    public new void Insert(int index, T item)
        => throw new InvalidOperationException("Elements cannot be inserted at an arbitrary index in an ordered list.");

    /// <summary>
    /// Searches for the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>
    /// The index of the first matching element, or the bitwise complement of its insertion index
    /// if no matching element exists.
    /// </returns>
    public int BinarySearch(T value)
    {
        return this.IndexOfFirstCore(value);
    }

    /// <summary>
    /// Gets the index of the first element equal to or greater than the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The index, or -1 if all elements are less than the specified value.</returns>
    public int GetLowerBound(T value)
    {
        var index = this.LowerBoundCore(value);
        return index < this.size ? index : -1;
    }

    /// <summary>
    /// Gets the index of the last element equal to or less than the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The index, or -1 if all elements are greater than the specified value.</returns>
    public int GetUpperBound(T value)
    {
        return this.UpperBoundExclusiveCore(value, 0, this.size) - 1;
    }

    /// <summary>
    /// Returns the half-open range containing all elements equal to the specified value.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns>A range in the form [Start, End), or (-1, -1) if the value is not found.</returns>
    public (int Start, int End) RangeOf(T value)
    {
        var start = this.IndexOfFirstCore(value);
        if (start < 0)
        {
            return (-1, -1);
        }

        // The element at 'start' is known to be equal, so the search can begin at start + 1.
        return (start, this.UpperBoundExclusiveCore(value, start + 1, this.size));
    }

    /// <summary>
    /// Determines whether the specified value is in the list.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found.</returns>
    public new bool Contains(T value)
    {
        return this.IndexOfFirstCore(value) >= 0;
    }

    /// <summary>
    /// Removes the first occurrence of the specified value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was removed.</returns>
    public new bool Remove(T value)
    {
        var index = this.IndexOfFirstCore(value);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Gets the element at the specified index. The setter is not supported because
    /// replacing an element would break the sort order.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">The setter is used.</exception>
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
        var index = this.IndexOfFirstCore(value);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Restores the sort order after unsorted elements were appended at [<paramref name="start"/>, size).
    /// They are placed after existing equal elements and keep their relative order.
    /// If the comparer throws, the list stays sorted and keeps the elements merged so far.
    /// </summary>
    /// <param name="start">The index of the first appended element.</param>
    private void MergeAppended(int start)
    {
        var items = this.items;
        var end = this.size;
        var count = end - start;
        if (count < MergeThreshold)
        {
            // Few elements: insert each one (a binary search and a block move).
            // Add writes at most up to index i (== size), which is already read.
            this.size = start;
            try
            {
                for (var i = start; i < end; i++)
                {
                    this.Add(items[i]);
                }
            }
            catch
            {
                this.ClearAfterSize(end);
                throw;
            }

            return;
        }

        // Many elements: sort them stably (ties by original position), then insert them from the largest.
        // A binary search over the unmerged existing elements finds those that follow each one, and they
        // move as a block, so the cost is O(m log m + m log n) comparisons and O(n + m) moves.
        SortEntry[]? pooled = null;
        var existingEnd = start; // The unmerged existing elements are [0, existingEnd).
        var mergedStart = end; // The merged elements are [mergedStart, end).
        try
        {
            pooled = ArrayPool<SortEntry>.Shared.Rent(count);
            var buffer = pooled.AsSpan(0, count);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = new(items[start + i], i);
            }

            try
            {
                if (this.comparerIsDefault && (typeof(T).IsValueType || !TypeIsComparable))
                {// Direct IComparable<SortEntry> calls: devirtualized for value types.
                    buffer.Sort();
                }
                else
                {// A class comparer: calls on a struct shared by reference types would go through instantiating stubs.
                    buffer.Sort(new StableComparer(this.Comparer));
                }
            }
            catch (InvalidOperationException e) when (e.InnerException is not null)
            {// Sort wraps the comparer's exception; throw it as Add does.
                ExceptionDispatchInfo.Throw(e.InnerException);
            }

            for (var i = count - 1; i >= 0; i--)
            {
                var value = buffer[i].Value;

                // The upper bound keeps existing equal elements before the added one.
                var index = this.UpperBoundExclusiveCore(value, 0, existingEnd);
                var moved = existingEnd - index;
                Array.Copy(items, index, items, mergedStart - moved, moved);
                mergedStart -= moved + 1;
                items[mergedStart] = value;
                existingEnd = index;
            }
        }
        catch
        {
            // [0, existingEnd) is untouched and [mergedStart, end) holds the merged maximums: join them.
            Array.Copy(items, mergedStart, items, existingEnd, end - mergedStart);
            this.size = existingEnd + (end - mergedStart);
            this.ClearAfterSize(end);
            throw;
        }
        finally
        {
            if (pooled is not null)
            {
                ArrayPool<SortEntry>.Shared.Return(pooled, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }
        }

        this.version++;
    }

    private void ClearAfterSize(int end)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(this.items, this.size, end - this.size);
        }

        this.version++;
    }

    // An element and its position in the batch: ties are ordered by position, which makes the sort stable.
    private readonly struct SortEntry : IComparable<SortEntry>
    {
        public readonly T Value;
        public readonly int Index;

        public SortEntry(T value, int index)
        {
            this.Value = value;
            this.Index = index;
        }

        // Used with the default comparer for value types, and for reference types that do not implement
        // IComparable<T> (e.g., interfaces); compares like the search strategies (see IndexOfFirstCore).
        public int CompareTo(SortEntry other)
        {
            int cmp;
            if (!typeof(T).IsValueType && this.Value is IComparable<T> comparable)
            {// Comparer<T>.Default would not use the element's IComparable<T>, but the search does.
                cmp = other.Value is null ? 1 : comparable.CompareTo(other.Value);
            }
            else
            {
                cmp = Comparer<T>.Default.Compare(this.Value, other.Value);
            }

            return cmp != 0 ? cmp : this.Index.CompareTo(other.Index);
        }
    }

    private sealed class StableComparer : IComparer<SortEntry>
    {
        private readonly IComparer<T> comparer;

        public StableComparer(IComparer<T> comparer) => this.comparer = comparer;

        public int Compare(SortEntry x, SortEntry y)
        {
            var cmp = this.comparer.Compare(x.Value, y.Value);
            return cmp != 0 ? cmp : x.Index.CompareTo(y.Index);
        }
    }

    #region Interface reimplementation

    // UnorderedList<T> implements IList<T> with non-virtual members, so the 'new' members above
    // would be bypassed by an interface call. Re-implementing the interface here remaps it
    // without adding a virtual call to the base class hot paths.
    T IList<T>.this[int index]
    {
        get => this[index];
        set => throw new InvalidOperationException("Elements cannot be replaced directly in an ordered list.");
    }

    T IReadOnlyList<T>.this[int index] => this[index];

    void ICollection<T>.Add(T value) => this.Add(value);

    bool ICollection<T>.Contains(T value) => this.Contains(value);

    bool ICollection<T>.Remove(T value) => this.Remove(value);

    int IList<T>.IndexOf(T value) => this.IndexOf(value);

    void IList<T>.Insert(int index, T item) => this.Insert(index, item);

    #endregion

    #region Search core

    // The comparison strategies below let a single generic search implementation be
    // instantiated per strategy: for value-type elements with the default comparer the JIT
    // devirtualizes and inlines Comparer<T>.Default.Compare (no boxing, no virtual call);
    // for reference-type comparable elements a single interface call remains; the
    // custom-comparer path matches the previous behavior.
    private interface IValueCompare
    {
        /// <summary>Returns the sign of Compare(value, element).</summary>
        int CompareValueTo(T element);
    }

    private readonly struct DefaultCompare : IValueCompare
    {
        private readonly T value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DefaultCompare(T value) => this.value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareValueTo(T element) => Comparer<T>.Default.Compare(this.value, element);
    }

    private readonly struct ComparableCompare : IValueCompare
    {
        private readonly IComparable<T> value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComparableCompare(IComparable<T> value) => this.value = value;

        // Comparer<T>.Default never passes null to CompareTo; a null element sorts first.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareValueTo(T element) => element is null ? 1 : this.value.CompareTo(element);
    }

    private readonly struct ComparerCompare : IValueCompare
    {
        private readonly IComparer<T> comparer;
        private readonly T value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComparerCompare(IComparer<T> comparer, T value)
        {
            this.comparer = comparer;
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareValueTo(T element) => this.comparer.Compare(this.value, element);
    }

    // Validates the range once so the search loops can use unchecked element access
    // (removes per-iteration array bounds checks) while remaining memory-safe.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T ValidateRangeAndGetReference(T[] items, int lo, int hi)
    {
        if ((uint)hi > (uint)items.Length || (uint)lo > (uint)hi)
        {
            ThrowInvalidRange();
        }

        return ref MemoryMarshal.GetArrayDataReference(items);
    }

    /// <summary>Returns the first index in [lo, hi) whose element is greater than or equal to the value, or hi.</summary>
    private static int LowerBound<TCompare>(T[] items, int lo, int hi, TCompare compare)
        where TCompare : struct, IValueCompare
    {
        ref var first = ref ValidateRangeAndGetReference(items, lo, hi);
        while (lo < hi)
        {
            var mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (compare.CompareValueTo(Unsafe.Add(ref first, mid)) > 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>Returns the first index in [lo, hi) whose element is greater than the value, or hi.</summary>
    private static int UpperBound<TCompare>(T[] items, int lo, int hi, TCompare compare)
        where TCompare : struct, IValueCompare
    {
        ref var first = ref ValidateRangeAndGetReference(items, lo, hi);
        while (lo < hi)
        {
            var mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (compare.CompareValueTo(Unsafe.Add(ref first, mid)) >= 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>Returns the index of the first element equal to the value, or the bitwise complement of the insertion index.</summary>
    private static int FirstIndex<TCompare>(T[] items, int size, TCompare compare)
        where TCompare : struct, IValueCompare
    {
        var lo = LowerBound(items, 0, size, compare);
        if (lo < size && compare.CompareValueTo(items[lo]) == 0)
        {
            return lo;
        }

        return ~lo;
    }

    /// <summary>Returns the index of the first element equal to the value, or the bitwise complement of the insertion index.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOfFirstCore(T value)
    {
        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            var index = hotMethod.LowerBound(new ReadOnlySpan<T>(this.items, 0, this.size), value);
            if ((uint)index < (uint)this.size &&
                this.Comparer.Compare(this.items[index], value) == 0)
            {
                return index;
            }

            return ~index;
        }

        if (this.comparerIsDefault)
        {
            if (typeof(T).IsValueType)
            {
                return FirstIndex(this.items, this.size, new DefaultCompare(value));
            }

            if (value is IComparable<T> comparable)
            {
                return FirstIndex(this.items, this.size, new ComparableCompare(comparable));
            }
        }

        return FirstIndex(this.items, this.size, new ComparerCompare(this.Comparer, value));
    }

    /// <summary>Returns the index of the first element not less than the specified value, or size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LowerBoundCore(T value)
    {
        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return hotMethod.LowerBound(new ReadOnlySpan<T>(this.items, 0, this.size), value);
        }

        if (this.comparerIsDefault)
        {
            if (typeof(T).IsValueType)
            {
                return LowerBound(this.items, 0, this.size, new DefaultCompare(value));
            }

            if (value is IComparable<T> comparable)
            {
                return LowerBound(this.items, 0, this.size, new ComparableCompare(comparable));
            }
        }

        return LowerBound(this.items, 0, this.size, new ComparerCompare(this.Comparer, value));
    }

    /// <summary>Returns the index of the first element greater than the specified value, searching [start, end), or end.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpperBoundExclusiveCore(T value, int start, int end)
    {
        if ((uint)start >= (uint)end)
        {
            return end;
        }

        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return start + hotMethod.UpperBoundExclusive(
                new ReadOnlySpan<T>(this.items, start, end - start),
                value);
        }

        if (this.comparerIsDefault)
        {
            if (typeof(T).IsValueType)
            {
                return UpperBound(this.items, start, end, new DefaultCompare(value));
            }

            if (value is IComparable<T> comparable)
            {
                return UpperBound(this.items, start, end, new ComparableCompare(comparable));
            }
        }

        return UpperBound(this.items, start, end, new ComparerCompare(this.Comparer, value));
    }

    [DoesNotReturn]
#pragma warning disable SA1204 // Static elements should appear before instance elements
    private static void ThrowInvalidRange()
#pragma warning restore SA1204 // Static elements should appear before instance elements
        => throw new InvalidOperationException("The search range is outside the bounds of the internal array.");

    #endregion
}
