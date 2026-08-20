// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

    private readonly bool useComparableFastPath;

    protected TKey[] keys;
    protected TValue[] values;
    protected KeyList? keyList;
    protected ValueList? valueList;
    protected int size;
    protected int version;

    public IComparer<TKey> Comparer { get; }

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
        this.HotMethod = HotMethodResolver.Get<TKey>(this.Comparer);

        this.useComparableFastPath =
            this.HotMethod is null &&
            !typeof(TKey).IsValueType &&
            ReferenceEquals(this.Comparer, Comparer<TKey>.Default);
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

        var count = 0;
        foreach (var pair in dictionary)
        {
            this.keys[count] = pair.Key;
            this.values[count] = pair.Value;
            count++;
        }

        if (count > 1)
        {
            Array.Sort(this.keys, this.values, 0, count, this.Comparer);
        }

        this.size = count;
    }

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

    public int Count => this.size;

    public IList<TKey> Keys => this.GetKeyListHelper();

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

    ICollection IDictionary.Keys => this.GetKeyListHelper();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

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
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);

        if ((uint)index < (uint)this.size &&
            this.Comparer.Compare(this.keys[index], key) == 0)
        {
            return index;
        }

        return ~index;
    }

    /// <summary>
    /// Gets the index of the first element equal to or greater than the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The matching index, or -1 if all elements are less than the key.</returns>
    public int GetLowerBound(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);
        return index < this.size ? index : -1;
    }

    /// <summary>
    /// Gets the index of the last element equal to or less than the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The matching index, or -1 if all elements are greater than the key.</returns>
    public int GetUpperBound(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return this.UpperBoundExclusive(key) - 1;
    }

    public void Add(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Insert after all existing entries with the same key.
        this.Insert(this.UpperBoundExclusive(key), key, value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => this.Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        var range = this.RangeOfKey(item.Key);
        if (range.Start < 0)
        {
            return false;
        }

        var comparer = EqualityComparer<TValue>.Default;

        for (var i = range.Start; i < range.End; i++)
        {
            if (comparer.Equals(this.values[i], item.Value))
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

    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);

        return (uint)index < (uint)this.size &&
            this.Comparer.Compare(this.keys[index], key) == 0;
    }

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

        for (var i = 0; i < this.size; i++)
        {
            array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(
                this.keys[i],
                this.values[i]);
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
            for (var i = 0; i < this.size; i++)
            {
                pairs[index + i] = new KeyValuePair<TKey, TValue>(
                    this.keys[i],
                    this.values[i]);
            }

            return;
        }

        if (array is not object[] objects)
        {
            throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
        }

        try
        {
            for (var i = 0; i < this.size; i++)
            {
                objects[index + i] = new KeyValuePair<TKey, TValue>(
                    this.keys[i],
                    this.values[i]);
            }
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("The destination array has an incompatible type.", nameof(array));
        }
    }

    public Enumerator GetEnumerator()
        => new(this, Enumerator.KeyValuePair);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    IDictionaryEnumerator IDictionary.GetEnumerator()
        => new Enumerator(this, Enumerator.DictEntry);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    public TValue this[TKey key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            var index = this.LowerBound(key);
            if ((uint)index < (uint)this.size &&
                this.Comparer.Compare(this.keys[index], key) == 0)
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
                var typedKey = (TKey)key;
                var index = this.LowerBound(typedKey);

                if ((uint)index < (uint)this.size &&
                    this.Comparer.Compare(this.keys[index], typedKey) == 0)
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
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);

        if ((uint)index < (uint)this.size &&
            this.Comparer.Compare(this.keys[index], key) == 0)
        {
            return index;
        }

        return -1;
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
        ArgumentNullException.ThrowIfNull(key);

        var start = this.LowerBound(key);

        if ((uint)start >= (uint)this.size ||
            this.Comparer.Compare(this.keys[start], key) != 0)
        {
            return (-1, -1);
        }

        return (start, this.UpperBoundExclusive(key, start));
    }

    /// <summary>
    /// Returns the index of the first occurrence of the specified value.
    /// </summary>
    public int IndexOfValue(TValue value)
    {
        return Array.IndexOf(this.values, value, 0, this.size);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);

        if ((uint)index < (uint)this.size &&
            this.Comparer.Compare(this.keys[index], key) == 0)
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
    /// Removes the first entry with the specified key.
    /// </summary>
    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var index = this.LowerBound(key);

        if ((uint)index >= (uint)this.size ||
            this.Comparer.Compare(this.keys[index], key) != 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the first entry matching both the specified key and value.
    /// </summary>
    public bool Remove(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var start = this.LowerBound(key);

        if ((uint)start >= (uint)this.size ||
            this.Comparer.Compare(this.keys[start], key) != 0)
        {
            return false;
        }

        var end = this.UpperBoundExclusive(key, start);
        var comparer = EqualityComparer<TValue>.Default;

        for (var i = start; i < end; i++)
        {
            if (comparer.Equals(this.values[i], value))
            {
                this.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    void IDictionary.Remove(object key)
    {
        if (IsCompatibleKey(key))
        {
            this.Remove((TKey)key);
        }
    }

    public void TrimExcess()
    {
        var threshold = (int)(this.keys.Length * 0.9);

        if (this.size < threshold)
        {
            this.Capacity = this.size;
        }
    }

    /// <summary>
    /// Returns the index of the first key not less than the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LowerBound(TKey key)
    {
        var hotMethod = this.HotMethod;

        if (hotMethod is not null)
        {
            return hotMethod.LowerBound(
                new ReadOnlySpan<TKey>(this.keys, 0, this.size),
                key);
        }

        return this.LowerBoundSlow(key);
    }

    /// <summary>
    /// Returns the index of the first key greater than the specified key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpperBoundExclusive(TKey key)
    {
        var hotMethod = this.HotMethod;

        if (hotMethod is not null)
        {
            return hotMethod.UpperBoundExclusive(
                new ReadOnlySpan<TKey>(this.keys, 0, this.size),
                key);
        }

        return this.UpperBoundExclusiveSlow(key, 0);
    }

    /// <summary>
    /// Returns the index of the first key greater than the specified key,
    /// starting from the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpperBoundExclusive(TKey key, int start)
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

        return this.UpperBoundExclusiveSlow(key, start);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int LowerBoundSlow(TKey key)
    {
        var min = 0;
        var max = this.size;
        var keys = this.keys;

        if (this.useComparableFastPath && key is IComparable<TKey> comparable)
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparable.CompareTo(keys[mid]) > 0)
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
            var comparer = this.Comparer;

            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparer.Compare(keys[mid], key) < 0)
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int UpperBoundExclusiveSlow(TKey key, int min)
    {
        var max = this.size;
        var keys = this.keys;

        if (this.useComparableFastPath && key is IComparable<TKey> comparable)
        {
            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparable.CompareTo(keys[mid]) >= 0)
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
            var comparer = this.Comparer;

            while (min < max)
            {
                var mid = min + ((max - min) >> 1);

                if (comparer.Compare(keys[mid], key) <= 0)
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int min)
    {
        var capacity = this.keys.Length;
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

        if (newCapacity < min)
        {
            newCapacity = min;
        }

        this.Capacity = newCapacity;
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
            this.EnsureCapacity(this.size + 1);
        }

        if (index < this.size)
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

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException();
            }

            if ((uint)this.index < (uint)this.list.size)
            {
                this.key = this.list.keys[this.index];
                this.value = this.list.values[this.index];
                this.index++;
                return true;
            }

            this.index = this.list.size + 1;
            this.key = default;
            this.value = default;
            return false;
        }

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

    public sealed class KeyList : IList<TKey>, ICollection
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;

        internal KeyList(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
        }

        public int Count => this.list.size;

        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.list).SyncRoot;

        public TKey this[int index]
        {
            get => this.list.GetKey(index);
            set => throw new NotSupportedException();
        }

        public void Add(TKey key)
            => throw new NotSupportedException();

        public void Clear()
            => throw new NotSupportedException();

        public bool Contains(TKey key)
            => this.list.ContainsKey(key);

        public void CopyTo(TKey[] array, int arrayIndex)
            => Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            try
            {
                Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(nameof(array));
            }
        }

        public void Insert(int index, TKey value)
            => throw new NotSupportedException();

        public IEnumerator<TKey> GetEnumerator()
            => new SortedListKeyEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new SortedListKeyEnumerator(this.list);

        public int IndexOf(TKey key)
            => this.list.IndexOfKey(key);

        public bool Remove(TKey key)
            => throw new NotSupportedException();

        public void RemoveAt(int index)
            => throw new NotSupportedException();
    }

    public sealed class ValueList : IList<TValue>, ICollection
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;

        internal ValueList(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
        }

        public int Count => this.list.size;

        public bool IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)this.list).SyncRoot;

        public TValue this[int index]
        {
            get => this.list.GetByIndex(index);
            set => throw new NotSupportedException();
        }

        public void Add(TValue value)
            => throw new NotSupportedException();

        public void Clear()
            => throw new NotSupportedException();

        public bool Contains(TValue value)
            => this.list.ContainsValue(value);

        public void CopyTo(TValue[] array, int arrayIndex)
            => Array.Copy(this.list.values, 0, array, arrayIndex, this.list.size);

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            try
            {
                Array.Copy(this.list.values, 0, array, index, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(nameof(array));
            }
        }

        public void Insert(int index, TValue value)
            => throw new NotSupportedException();

        public IEnumerator<TValue> GetEnumerator()
            => new SortedListValueEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new SortedListValueEnumerator(this.list);

        public int IndexOf(TValue value)
            => Array.IndexOf(this.list.values, value, 0, this.list.size);

        public bool Remove(TValue value)
            => throw new NotSupportedException();

        public void RemoveAt(int index)
            => throw new NotSupportedException();
    }

    #endregion
}
