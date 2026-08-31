// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

namespace Arc.Collections;

/// <summary>
/// Represents a sorted list of key-value pairs that allows duplicate keys.
/// Dictionary-style lookup returns the first value for a key, while assignment adds a new entry.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class OrderedKeyValueList<TKey, TValue> :
    IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private const int DefaultCapacity = 4;

    // Cached so hot paths can select a comparison strategy without a ReferenceEquals per call.
    private readonly bool comparerIsDefault;

    /// <summary>
    /// The backing key array. Only the first <see cref="Count"/> entries are in use.
    /// </summary>
    protected TKey[] keys;

    /// <summary>
    /// The backing value array. Only the first <see cref="Count"/> entries are in use.
    /// </summary>
    protected TValue[] values;

    /// <summary>
    /// The cached <see cref="KeyList"/> view, created on first use.
    /// </summary>
    protected KeyList? keyList;

    /// <summary>
    /// The cached <see cref="ValueList"/> view, created on first use.
    /// </summary>
    protected ValueList? valueList;

    /// <summary>
    /// The number of entries in use.
    /// </summary>
    protected int size;

    /// <summary>
    /// The modification counter used to invalidate enumerators.
    /// </summary>
    protected int version;

    /// <summary>
    /// Gets the comparer used to order the keys.
    /// </summary>
    public IComparer<TKey> Comparer { get; }

    /// <summary>
    /// Gets the specialized comparison implementation for <typeparamref name="TKey"/>,
    /// or <see langword="null"/> when none is available.
    /// </summary>
    public IHotMethod<TKey>? HotMethod { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    public OrderedKeyValueList()
        : this(0, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    public OrderedKeyValueList(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer to use for comparing keys.</param>
    public OrderedKeyValueList(IComparer<TKey>? comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    /// <param name="comparer">The comparer to use for comparing keys.</param>
    public OrderedKeyValueList(int capacity, IComparer<TKey>? comparer)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (capacity == 0)
        {
            this.keys = Array.Empty<TKey>();
            this.values = Array.Empty<TValue>();
        }
        else
        {
            this.keys = new TKey[capacity];
            this.values = new TValue[capacity];
        }

        this.Comparer = comparer ?? Comparer<TKey>.Default;
        this.comparerIsDefault = ReferenceEquals(this.Comparer, Comparer<TKey>.Default);
        this.HotMethod = HotMethodResolver.Get<TKey>(this.Comparer);
    }

    /// <summary>
    /// Initializes a new instance by copying the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy.</param>
    public OrderedKeyValueList(IDictionary<TKey, TValue> dictionary)
        : this(dictionary, null)
    {
    }

    /// <summary>
    /// Initializes a new instance by copying the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy.</param>
    /// <param name="comparer">The comparer to use for comparing keys.</param>
    public OrderedKeyValueList(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer)
        : this(dictionary is null ? 0 : dictionary.Count, comparer)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var count = dictionary.Count;
        if (count != 0)
        {
            // Bulk copy is considerably faster than enumerating pairs one by one.
            dictionary.Keys.CopyTo(this.keys, 0);
            dictionary.Values.CopyTo(this.values, 0);
            if (count > 1)
            {
                Array.Sort(this.keys, this.values, 0, count, this.Comparer);
            }
        }

        this.size = count;
    }

    /// <summary>
    /// Gets or sets the number of entries the internal arrays can hold without resizing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than <see cref="Count"/>.</exception>
    public int Capacity
    {
        get => this.keys.Length;
        set
        {
            if (value == this.keys.Length)
            {
                return;
            }

            if (value < this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value == 0)
            {
                this.keys = Array.Empty<TKey>();
                this.values = Array.Empty<TValue>();
                return;
            }

            var newKeys = new TKey[value];
            var newValues = new TValue[value];
            if (this.size != 0)
            {
                Array.Copy(this.keys, newKeys, this.size);
                Array.Copy(this.values, newValues, this.size);
            }

            this.keys = newKeys;
            this.values = newValues;
        }
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => this.size;

    /// <summary>
    /// Gets a read-only, index-accessible view of the keys, in sort order.
    /// </summary>
    public IList<TKey> Keys => this.GetKeyListHelper();

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

    ICollection IDictionary.Keys => this.GetKeyListHelper();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

    /// <summary>
    /// Gets a read-only, index-accessible view of the values, ordered by their keys.
    /// </summary>
    public IList<TValue> Values => this.GetValueListHelper();

    ICollection<TValue> IDictionary<TKey, TValue>.Values => this.GetValueListHelper();

    ICollection IDictionary.Values => this.GetValueListHelper();

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.GetValueListHelper();

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    bool IDictionary.IsReadOnly => false;

    bool IDictionary.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    /// <summary>
    /// Searches for the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// The index of the first matching key, or the bitwise complement of the insertion index if no match exists.
    /// </returns>
    public int BinarySearch(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        return this.IndexOfFirstCore(key);
    }

    /// <summary>
    /// Gets the index of the first element equal to or greater than the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The matching index, or -1 if all elements are less than the key.</returns>
    public int GetLowerBound(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var index = this.LowerBoundCore(key);
        return index < this.size ? index : -1;
    }

    /// <summary>
    /// Gets the index of the last element equal to or less than the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The matching index, or -1 if all elements are greater than the key.</returns>
    public int GetUpperBound(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        return this.UpperBoundExclusiveCore(key, 0) - 1;
    }

    /// <summary>
    /// Adds a key-value pair, keeping the list sorted.
    /// Entries with an equal key are inserted after the existing ones.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public void Add(TKey key, TValue value)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        // A single upper-bound search yields the insertion index directly and
        // inserts after all existing entries with the same key.
        this.Insert(this.UpperBoundExclusiveCore(key, 0), key, value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => this.Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        var (start, end) = this.RangeOfKey(item.Key);
        if (start < 0)
        {
            return false;
        }

        var values = this.values;
        var comparer = EqualityComparer<TValue>.Default;
        for (var i = start; i < end; i++)
        {
            if (comparer.Equals(values[i], item.Value))
            {
                return true;
            }
        }

        return false;
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => this.Remove(item.Key, item.Value);

    void IDictionary.Add(object key, object? value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key is not TKey typedKey)
        {
            throw new ArgumentException("The key has an incompatible type.", nameof(key));
        }

        if (value is TValue typedValue)
        {
            this.Add(typedKey, typedValue);
            return;
        }

        if (value is null && default(TValue) is null)
        {
            this.Add(typedKey, default!);
            return;
        }

        throw new ArgumentException("The value has an incompatible type.", nameof(value));
    }

    /// <summary>
    /// Removes all entries from the list.
    /// </summary>
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            Array.Clear(this.keys, 0, this.size);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            Array.Clear(this.values, 0, this.size);
        }

        this.size = 0;
        this.version++;
    }

    bool IDictionary.Contains(object key)
    {
        return IsCompatibleKey(key) && this.ContainsKey((TKey)key);
    }

    /// <summary>
    /// Determines whether the list contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    public bool ContainsKey(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        return this.IndexOfFirstCore(key) >= 0;
    }

    /// <summary>
    /// Determines whether the list contains the specified value.
    /// <br/>O(n) operation.
    /// </summary>
    /// <param name="value">The value to locate.</param>
    /// <returns><see langword="true"/> if the value is found; otherwise, <see langword="false"/>.</returns>
    public bool ContainsValue(TValue value)
    {
        return this.IndexOfValue(value) >= 0;
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
        KeyValuePair<TKey, TValue>[] array,
        int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)arrayIndex > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < this.size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        var keys = this.keys;
        var values = this.values;
        var size = this.size;
        for (var i = 0; i < size; i++)
        {
            array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1)
        {
            throw new ArgumentException("Only single-dimensional arrays are supported.", nameof(array));
        }

        if (array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("Non-zero lower-bound arrays are not supported.", nameof(array));
        }

        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < this.size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        if (array is KeyValuePair<TKey, TValue>[] pairs)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(pairs, index);
            return;
        }

        if (array is not object[] objects)
        {
            throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
        }

        try
        {
            var keys = this.keys;
            var values = this.values;
            var size = this.size;
            for (var i = 0; i < size; i++)
            {
                objects[index + i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
            }
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public Enumerator GetEnumerator()
        => new(this, Enumerator.KeyValuePair);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    IDictionaryEnumerator IDictionary.GetEnumerator()
        => new Enumerator(this, Enumerator.DictEntry);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    /// <summary>
    /// Gets the value of the first entry with the specified key, or adds a new entry.
    /// The setter always adds an entry, because duplicate keys are allowed.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <returns>The value of the first entry with <paramref name="key"/>.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist (getter only).</exception>
    public TValue this[TKey key]
    {
        get
        {
            if (key is null)
            {
                ThrowArgumentNullKey();
            }

            var index = this.IndexOfFirstCore(key);
            if (index >= 0)
            {
                return this.values[index];
            }

            throw new KeyNotFoundException();
        }

        set => this.Add(key, value);
    }

    object? IDictionary.this[object key]
    {
        get
        {
            if (IsCompatibleKey(key))
            {
                var index = this.IndexOfFirstCore((TKey)key);
                if (index >= 0)
                {
                    return this.values[index];
                }
            }

            return null;
        }

        set => ((IDictionary)this).Add(key, value);
    }

    /// <summary>
    /// Returns the index of the first entry with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The first matching index, or -1 if the key is not found.</returns>
    public int IndexOfKey(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var index = this.IndexOfFirstCore(key);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Returns the half-open range containing all entries with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// A range in the form [Start, End), or (-1, -1) if the key is not found.
    /// </returns>
    public (int Start, int End) RangeOfKey(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var start = this.IndexOfFirstCore(key);
        if (start < 0)
        {
            return (-1, -1);
        }

        // The entry at 'start' is known to be equal, so the search can begin at start + 1.
        return (start, this.UpperBoundExclusiveCore(key, start + 1));
    }

    /// <summary>
    /// Returns the index of the first occurrence of the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The zero-based index of the first occurrence, or -1 if not found.</returns>
    public int IndexOfValue(TValue value)
    {
        return Array.IndexOf(this.values, value, 0, this.size);
    }

    /// <summary>
    /// Attempts to get the value of the first entry with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, the value of the first matching entry; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var index = this.IndexOfFirstCore(key);
        if (index >= 0)
        {
            value = this.values[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var newSize = this.size - 1;
        if (index < newSize)
        {
            var count = newSize - index;
            Array.Copy(this.keys, index + 1, this.keys, index, count);
            Array.Copy(this.values, index + 1, this.values, index, count);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            this.keys[newSize] = default!;
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            this.values[newSize] = default!;
        }

        this.size = newSize;
        this.version++;
    }

    /// <summary>
    /// Removes a range of elements starting at the specified index.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0 || this.size - index < count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return;
        }

        var newSize = this.size - count;
        if (index < newSize)
        {
            Array.Copy(this.keys, index + count, this.keys, index, newSize - index);
            Array.Copy(this.values, index + count, this.values, index, newSize - index);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            Array.Clear(this.keys, newSize, count);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            Array.Clear(this.values, newSize, count);
        }

        this.size = newSize;
        this.version++;
    }

    /// <summary>
    /// Removes the first entry with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(TKey key)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var index = this.IndexOfFirstCore(key);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the first entry matching both the specified key and value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if an element was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(TKey key, TValue value)
    {
        if (key is null)
        {
            ThrowArgumentNullKey();
        }

        var (start, end) = this.RangeOfKey(key);
        if (start < 0)
        {
            return false;
        }

        var values = this.values;
        var comparer = EqualityComparer<TValue>.Default;
        for (var i = start; i < end; i++)
        {
            if (comparer.Equals(values[i], value))
            {
                this.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes all entries with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>The number of entries removed.</returns>
    public int RemoveAll(TKey key)
    {
        var (start, end) = this.RangeOfKey(key);
        if (start < 0)
        {
            return 0;
        }

        var count = end - start;
        this.RemoveRange(start, count);
        return count;
    }

    void IDictionary.Remove(object key)
    {
        if (IsCompatibleKey(key))
        {
            this.Remove((TKey)key);
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

        if (this.keys.Length < capacity)
        {
            this.Capacity = ComputeNewCapacity(this.keys.Length, capacity);
        }

        return this.keys.Length;
    }

    /// <summary>
    /// Sets the capacity to the actual number of entries, if that is less than 90% of the current capacity.
    /// </summary>
    public void TrimExcess()
    {
        var threshold = (int)(this.keys.Length * 0.9);
        if (this.size < threshold)
        {
            this.Capacity = this.size;
        }
    }

    #region Search core

    // The comparison strategies below let a single generic search implementation be
    // instantiated per strategy: for value-type keys with the default comparer the JIT
    // devirtualizes and inlines Comparer<TKey>.Default.Compare (no boxing, no virtual
    // call); for reference-type comparable keys a single interface call remains; the
    // custom-comparer path matches the previous behavior.
    private interface IKeyCompare
    {
        /// <summary>Returns the sign of Compare(key, element).</summary>
        int CompareKeyTo(TKey element);
    }

    private readonly struct DefaultCompare : IKeyCompare
    {
        private readonly TKey key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DefaultCompare(TKey key) => this.key = key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareKeyTo(TKey element) => Comparer<TKey>.Default.Compare(this.key, element);
    }

    private readonly struct ComparableCompare : IKeyCompare
    {
        private readonly IComparable<TKey> key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComparableCompare(IComparable<TKey> key) => this.key = key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareKeyTo(TKey element) => this.key.CompareTo(element);
    }

    private readonly struct ComparerCompare : IKeyCompare
    {
        private readonly IComparer<TKey> comparer;
        private readonly TKey key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComparerCompare(IComparer<TKey> comparer, TKey key)
        {
            this.comparer = comparer;
            this.key = key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareKeyTo(TKey element) => this.comparer.Compare(this.key, element);
    }

    // Validates the range once so the search loops can use unchecked element access
    // (removes per-iteration array bounds checks) while remaining memory-safe.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TKey ValidateRangeAndGetReference(TKey[] keys, int lo, int hi)
    {
        if ((uint)hi > (uint)keys.Length || (uint)lo > (uint)hi)
        {
            ThrowInvalidRange();
        }

        return ref MemoryMarshal.GetArrayDataReference(keys);
    }

    /// <summary>Returns the first index in [lo, hi) whose key is greater than or equal to the search key, or hi.</summary>
    private static int LowerBound<TCompare>(TKey[] keys, int lo, int hi, TCompare compare)
        where TCompare : struct, IKeyCompare
    {
        ref var first = ref ValidateRangeAndGetReference(keys, lo, hi);
        while (lo < hi)
        {
            var mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (compare.CompareKeyTo(Unsafe.Add(ref first, mid)) > 0)
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

    /// <summary>Returns the first index in [lo, hi) whose key is greater than the search key, or hi.</summary>
    private static int UpperBound<TCompare>(TKey[] keys, int lo, int hi, TCompare compare)
        where TCompare : struct, IKeyCompare
    {
        ref var first = ref ValidateRangeAndGetReference(keys, lo, hi);
        while (lo < hi)
        {
            var mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (compare.CompareKeyTo(Unsafe.Add(ref first, mid)) >= 0)
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

    /// <summary>Returns the index of the first entry equal to the key, or the bitwise complement of the insertion index.</summary>
    private static int FirstIndex<TCompare>(TKey[] keys, int size, TCompare compare)
        where TCompare : struct, IKeyCompare
    {
        var lo = LowerBound(keys, 0, size, compare);
        if (lo < size && compare.CompareKeyTo(keys[lo]) == 0)
        {
            return lo;
        }

        return ~lo;
    }

    /// <summary>Returns the index of the first entry equal to the key, or the bitwise complement of the insertion index.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOfFirstCore(TKey key)
    {
        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            var index = hotMethod.LowerBound(new ReadOnlySpan<TKey>(this.keys, 0, this.size), key);
            if ((uint)index < (uint)this.size &&
                this.Comparer.Compare(this.keys[index], key) == 0)
            {
                return index;
            }

            return ~index;
        }

        if (this.comparerIsDefault)
        {
            if (typeof(TKey).IsValueType)
            {
                return FirstIndex(this.keys, this.size, new DefaultCompare(key));
            }

            if (key is IComparable<TKey> comparable)
            {
                return FirstIndex(this.keys, this.size, new ComparableCompare(comparable));
            }
        }

        return FirstIndex(this.keys, this.size, new ComparerCompare(this.Comparer, key));
    }

    /// <summary>Returns the index of the first key not less than the specified key, or size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LowerBoundCore(TKey key)
    {
        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return hotMethod.LowerBound(new ReadOnlySpan<TKey>(this.keys, 0, this.size), key);
        }

        if (this.comparerIsDefault)
        {
            if (typeof(TKey).IsValueType)
            {
                return LowerBound(this.keys, 0, this.size, new DefaultCompare(key));
            }

            if (key is IComparable<TKey> comparable)
            {
                return LowerBound(this.keys, 0, this.size, new ComparableCompare(comparable));
            }
        }

        return LowerBound(this.keys, 0, this.size, new ComparerCompare(this.Comparer, key));
    }

    /// <summary>Returns the index of the first key greater than the specified key, searching [start, size), or size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpperBoundExclusiveCore(TKey key, int start)
    {
        if ((uint)start >= (uint)this.size)
        {
            return this.size;
        }

        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return start + hotMethod.UpperBoundExclusive(
                new ReadOnlySpan<TKey>(this.keys, start, this.size - start),
                key);
        }

        if (this.comparerIsDefault)
        {
            if (typeof(TKey).IsValueType)
            {
                return UpperBound(this.keys, start, this.size, new DefaultCompare(key));
            }

            if (key is IComparable<TKey> comparable)
            {
                return UpperBound(this.keys, start, this.size, new ComparableCompare(comparable));
            }
        }

        return UpperBound(this.keys, start, this.size, new ComparerCompare(this.Comparer, key));
    }

    #endregion

    [DoesNotReturn]
    private static void ThrowArgumentNullKey()
        => throw new ArgumentNullException("key");

    [DoesNotReturn]
    private static void ThrowInvalidRange()
        => throw new InvalidOperationException("The search range is outside the bounds of the internal array.");

    private static int ComputeNewCapacity(int capacity, int min)
    {
        int newCapacity;
        if (capacity == 0)
        {
            newCapacity = DefaultCapacity;
        }
        else if (capacity > Array.MaxLength / 2)
        {
            newCapacity = Array.MaxLength;
        }
        else
        {
            newCapacity = capacity * 2;
        }

        return newCapacity < min ? min : newCapacity;
    }

    private TValue GetByIndex(int index)
    {
        if ((uint)index >= (uint)this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return this.values[index];
    }

    private TKey GetKey(int index)
    {
        if ((uint)index >= (uint)this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return this.keys[index];
    }

    private void Insert(int index, TKey key, TValue value)
    {
        if (this.size == this.keys.Length)
        {
            // Grow and shift in a single copy pass instead of copy-then-shift.
            this.GrowForInsertion(index);
        }
        else if (index < this.size)
        {
            var count = this.size - index;
            Array.Copy(this.keys, index, this.keys, index + 1, count);
            Array.Copy(this.values, index, this.values, index + 1, count);
        }

        this.keys[index] = key;
        this.values[index] = value;
        this.size++;
        this.version++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowForInsertion(int index)
    {
        var newCapacity = ComputeNewCapacity(this.keys.Length, this.size + 1);
        var newKeys = new TKey[newCapacity];
        var newValues = new TValue[newCapacity];
        if (index != 0)
        {
            Array.Copy(this.keys, newKeys, index);
            Array.Copy(this.values, newValues, index);
        }

        if (this.size != index)
        {
            Array.Copy(this.keys, index, newKeys, index + 1, this.size - index);
            Array.Copy(this.values, index, newValues, index + 1, this.size - index);
        }

        this.keys = newKeys;
        this.values = newValues;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCompatibleKey(object? key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return key is TKey;
    }

    private KeyList GetKeyListHelper()
    {
        return this.keyList ??= new KeyList(this);
    }

    private ValueList GetValueListHelper()
    {
        return this.valueList ??= new ValueList(this);
    }

    #region Enumerator

    /// <summary>
    /// Enumerates the elements of a <see cref="OrderedKeyValueList{TKey, TValue}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
    {
        internal const int KeyValuePair = 1;
        internal const int DictEntry = 2;

        private readonly OrderedKeyValueList<TKey, TValue> list;
        private readonly int version;
        private readonly int getEnumeratorRetType;
        private TKey? key;
        private TValue? value;
        private int index;

        internal Enumerator(OrderedKeyValueList<TKey, TValue> list, int getEnumeratorRetType)
        {
            this.list = list;
            this.version = list.version;
            this.getEnumeratorRetType = getEnumeratorRetType;
            this.index = 0;
            this.key = default;
            this.value = default;
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<TKey, TValue> Current
            => new(this.key!, this.value!);

        object IDictionaryEnumerator.Key
        {
            get
            {
                this.ValidateCurrent();
                return this.key!;
            }
        }

        DictionaryEntry IDictionaryEnumerator.Entry
        {
            get
            {
                this.ValidateCurrent();
                return new DictionaryEntry(this.key!, this.value);
            }
        }

        object? IDictionaryEnumerator.Value
        {
            get
            {
                this.ValidateCurrent();
                return this.value;
            }
        }

        object? IEnumerator.Current
        {
            get
            {
                this.ValidateCurrent();
                return this.getEnumeratorRetType == DictEntry
                    ? new DictionaryEntry(this.key!, this.value)
                    : new KeyValuePair<TKey, TValue>(this.key!, this.value!);
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was advanced; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            var list = this.list;
            if (this.version != list.version)
            {
                throw new InvalidOperationException();
            }

            var index = this.index;
            if ((uint)index < (uint)list.size)
            {
                this.key = list.keys[index];
                this.value = list.values[index];
                this.index = index + 1;
                return true;
            }

            this.index = list.size + 1;
            this.key = default;
            this.value = default;
            return false;
        }

        /// <summary>
        /// Releases the resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose()
        {
            this.index = 0;
            this.key = default;
            this.value = default;
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            this.index = 0;
            this.key = default;
            this.value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateCurrent()
        {
            if (this.index == 0 || this.index == this.list.size + 1)
            {
                throw new InvalidOperationException();
            }
        }
    }

    private sealed class SortedListKeyEnumerator : IEnumerator<TKey>
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;
        private readonly int version;
        private int index;
        private TKey? currentKey;

        internal SortedListKeyEnumerator(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
            this.version = list.version;
        }

        public TKey Current => this.currentKey!;

        object? IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.list.size + 1)
                {
                    throw new InvalidOperationException();
                }

                return this.currentKey;
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            if ((uint)this.index < (uint)this.list.size)
            {
                this.currentKey = this.list.keys[this.index++];
                return true;
            }

            this.index = this.list.size + 1;
            this.currentKey = default;
            return false;
        }

        public void Dispose()
        {
            this.index = 0;
            this.currentKey = default;
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            this.index = 0;
            this.currentKey = default;
        }
    }

    private sealed class SortedListValueEnumerator : IEnumerator<TValue>
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;
        private readonly int version;
        private int index;
        private TValue? currentValue;

        internal SortedListValueEnumerator(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
            this.version = list.version;
        }

        public TValue Current => this.currentValue!;

        object? IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.list.size + 1)
                {
                    throw new InvalidOperationException();
                }

                return this.currentValue;
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            if ((uint)this.index < (uint)this.list.size)
            {
                this.currentValue = this.list.values[this.index++];
                return true;
            }

            this.index = this.list.size + 1;
            this.currentValue = default;
            return false;
        }

        public void Dispose()
        {
            this.index = 0;
            this.currentValue = default;
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            this.index = 0;
            this.currentValue = default;
        }
    }

    /// <summary>
    /// Represents a read-only, index-accessible view of the keys of an <see cref="OrderedKeyValueList{TKey, TValue}"/>.
    /// </summary>
    public sealed class KeyList : IList<TKey>, ICollection
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;

        internal KeyList(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => this.list.size;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always <see langword="false"/>.
        /// </summary>
        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.list).SyncRoot;

        /// <summary>
        /// Gets the key at the specified index. The setter is not supported.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The key at <paramref name="index"/>.</returns>
        /// <exception cref="NotSupportedException">The setter is used.</exception>
        public TKey this[int index]
        {
            get => this.list.GetKey(index);
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="key">Not used.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Add(TKey key)
            => throw new NotSupportedException();

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Clear()
            => throw new NotSupportedException();

        /// <summary>
        /// Determines whether the view contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
        public bool Contains(TKey key)
            => this.list.ContainsKey(key);

        /// <summary>
        /// Copies the keys to an array, in order.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based destination index.</param>
        public void CopyTo(TKey[] array, int arrayIndex)
            => Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (array.Rank != 1)
            {
                throw new ArgumentException("Only single-dimensional arrays are supported.", nameof(array));
            }

            try
            {
                Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
            }
        }

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="index">Not used.</param>
        /// <param name="value">The value that would have been inserted.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Insert(int index, TKey value)
            => throw new NotSupportedException();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<TKey> GetEnumerator()
            => new SortedListKeyEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new SortedListKeyEnumerator(this.list);

        /// <summary>
        /// Returns the index of the first occurrence of the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The zero-based index, or -1 if not found.</returns>
        public int IndexOf(TKey key)
            => this.list.IndexOfKey(key);

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="key">Not used.</param>
        /// <returns>This method never returns.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public bool Remove(TKey key)
            => throw new NotSupportedException();

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="index">Not used.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void RemoveAt(int index)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Represents a read-only, index-accessible view of the values of an <see cref="OrderedKeyValueList{TKey, TValue}"/>.
    /// </summary>
    public sealed class ValueList : IList<TValue>, ICollection
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;

        internal ValueList(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => this.list.size;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always <see langword="false"/>.
        /// </summary>
        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.list).SyncRoot;

        /// <summary>
        /// Gets the value at the specified index. The setter is not supported.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The value at <paramref name="index"/>.</returns>
        /// <exception cref="NotSupportedException">The setter is used.</exception>
        public TValue this[int index]
        {
            get => this.list.GetByIndex(index);
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Add(TValue value)
            => throw new NotSupportedException();

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Clear()
            => throw new NotSupportedException();

        /// <summary>
        /// Determines whether the view contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
        public bool Contains(TValue value)
            => this.list.ContainsValue(value);

        /// <summary>
        /// Copies the values to an array, in order.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based destination index.</param>
        public void CopyTo(TValue[] array, int arrayIndex)
            => Array.Copy(this.list.values, 0, array, arrayIndex, this.list.size);

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (array.Rank != 1)
            {
                throw new ArgumentException("Only single-dimensional arrays are supported.", nameof(array));
            }

            try
            {
                Array.Copy(this.list.values, 0, array, index, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
            }
        }

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="index">Not used.</param>
        /// <param name="value">The value that would have been inserted.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Insert(int index, TValue value)
            => throw new NotSupportedException();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<TValue> GetEnumerator()
            => new SortedListValueEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new SortedListValueEnumerator(this.list);

        /// <summary>
        /// Returns the index of the first occurrence of the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based index, or -1 if not found.</returns>
        public int IndexOf(TValue value)
            => Array.IndexOf(this.list.values, value, 0, this.list.size);

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <returns>This method never returns.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public bool Remove(TValue value)
            => throw new NotSupportedException();

        /// <summary>
        /// Not supported: the view is read-only.
        /// </summary>
        /// <param name="index">Not used.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void RemoveAt(int index)
            => throw new NotSupportedException();
    }

    #endregion
}
