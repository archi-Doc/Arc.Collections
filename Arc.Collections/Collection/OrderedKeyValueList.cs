// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Arc.Collections.HotMethod;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

namespace Arc.Collections;

/// <summary>
/// Represents key-value pairs stored in sorted key order.
/// Keys are unique according to <see cref="Comparer"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TValue">The type of values in the collection.</typeparam>
public class OrderedKeyValueList<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IDictionary,
    IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private const int DefaultCapacity = 4;

    protected TKey[] keys;
    protected TValue[] values;
    protected KeyList? keyList;
    protected ValueList? valueList;
    protected int size;
    protected int version;

    /// <summary>
    /// Gets the comparer used to order keys.
    /// </summary>
    public IComparer<TKey> Comparer { get; }

    /// <summary>
    /// Gets the optimized key operations available for the current comparer.
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
    /// <param name="capacity">The initial capacity.</param>
    public OrderedKeyValueList(int capacity)
        : this(capacity, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="comparer">The comparer used to order keys.</param>
    public OrderedKeyValueList(IComparer<TKey>? comparer)
        : this(0, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The comparer used to order keys.</param>
    public OrderedKeyValueList(int capacity, IComparer<TKey>? comparer)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        this.keys = capacity == 0 ? Array.Empty<TKey>() : new TKey[capacity];
        this.values = capacity == 0 ? Array.Empty<TValue>() : new TValue[capacity];

        this.Comparer = comparer ?? Comparer<TKey>.Default;
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
    /// <param name="comparer">The comparer used to order keys.</param>
    public OrderedKeyValueList(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer)
        : this(dictionary?.Count ?? throw new ArgumentNullException(nameof(dictionary)), comparer)
    {
        var index = 0;

        foreach (var item in dictionary)
        {
            if (item.Key is null)
            {
                throw new ArgumentException("The dictionary contains a null key.", nameof(dictionary));
            }

            if (index == this.keys.Length)
            {
                this.size = index;
                this.EnsureCapacity(index + 1);
            }

            this.keys[index] = item.Key;
            this.values[index] = item.Value;
            index++;
        }

        this.size = index;

        if (index <= 1)
        {
            return;
        }

        Array.Sort(this.keys, this.values, 0, index, this.Comparer);

        // Different source and destination comparers may consider distinct source keys equal.
        for (var i = 1; i < index; i++)
        {
            if (this.Comparer.Compare(this.keys[i - 1], this.keys[i]) == 0)
            {
                throw new ArgumentException("The dictionary contains duplicate keys according to the specified comparer.", nameof(dictionary));
            }
        }
    }

    /// <summary>
    /// Searches for the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// The index of the key if found; otherwise, the bitwise complement of the insertion index.
    /// </returns>
    public int BinarySearch(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var hotMethod = this.HotMethod;
        if (hotMethod is not null)
        {
            return hotMethod.BinarySearch(this.keys, 0, this.size, key);
        }

        var keys = this.keys;
        var comparer = this.Comparer;
        var lo = 0;
        var hi = this.size - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var cmp = comparer.Compare(key, keys[mid]);

            if (cmp < 0)
            {
                hi = mid - 1;
            }
            else if (cmp > 0)
            {
                lo = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return ~lo;
    }

    /// <summary>
    /// Returns the index of the first element equal to or greater than the specified key,
    /// or -1 if all elements are less than the key.
    /// </summary>
    public int GetLowerBound(TKey key)
    {
        var index = this.BinarySearch(key);
        if (index >= 0)
        {
            return index;
        }

        index = ~index;
        return index < this.size ? index : -1;
    }

    /// <summary>
    /// Returns the index of the last element equal to or less than the specified key,
    /// or -1 if all elements are greater than the key.
    /// </summary>
    public int GetUpperBound(TKey key)
    {
        var index = this.BinarySearch(key);
        return index >= 0 ? index : (~index - 1);
    }

    #region IDictionary

    public void Add(TKey key, TValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var index = this.BinarySearch(key);
        if (index >= 0)
        {
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }

        this.Insert(~index, key, value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        => this.Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        var index = this.IndexOfKey(item.Key);
        return index >= 0 && EqualityComparer<TValue>.Default.Equals(this.values[index], item.Value);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = this.IndexOfKey(item.Key);
        if (index < 0 || !EqualityComparer<TValue>.Default.Equals(this.values[index], item.Value))
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    #endregion

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

    void IDictionary.Add(object key, object? value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key is not TKey typedKey)
        {
            throw new ArgumentException("The key is of an incompatible type.", nameof(key));
        }

        if (value is null)
        {
            if (default(TValue) is not null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.Add(typedKey, default!);
            return;
        }

        if (value is not TValue typedValue)
        {
            throw new ArgumentException("The value is of an incompatible type.", nameof(value));
        }

        this.Add(typedKey, typedValue);
    }

    public int Count => this.size;

    /// <summary>
    /// Gets a read-only view of the keys.
    /// </summary>
    public KeyList Keys => this.GetKeyListHelper();

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

    ICollection IDictionary.Keys => this.GetKeyListHelper();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.GetKeyListHelper();

    /// <summary>
    /// Gets a read-only view of the values.
    /// </summary>
    public ValueList Values => this.GetValueListHelper();

    ICollection<TValue> IDictionary<TKey, TValue>.Values => this.GetValueListHelper();

    ICollection IDictionary.Values => this.GetValueListHelper();

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.GetValueListHelper();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private KeyList GetKeyListHelper()
        => this.keyList ??= new KeyList(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ValueList GetValueListHelper()
        => this.valueList ??= new ValueList(this);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    bool IDictionary.IsReadOnly => false;

    bool IDictionary.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    public void Clear()
    {
        var size = this.size;

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            Array.Clear(this.keys, 0, size);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            Array.Clear(this.values, 0, size);
        }

        this.size = 0;
        this.version++;
    }

    bool IDictionary.Contains(object key)
    {
        return IsCompatibleKey(key) && this.ContainsKey((TKey)key);
    }

    public bool ContainsKey(TKey key)
        => this.IndexOfKey(key) >= 0;

    public bool ContainsValue(TValue value)
        => this.IndexOfValue(value) >= 0;

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if ((uint)arrayIndex > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        var size = this.size;
        if (array.Length - arrayIndex < size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        for (var i = 0; i < size; i++)
        {
            array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
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

        var size = this.size;
        if (array.Length - index < size)
        {
            throw new ArgumentException("The destination array is too small.", nameof(array));
        }

        if (array is KeyValuePair<TKey, TValue>[] pairs)
        {
            for (var i = 0; i < size; i++)
            {
                pairs[index + i] = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
            }

            return;
        }

        if (array is not object[] objects)
        {
            throw new ArgumentException("The array type is incompatible.", nameof(array));
        }

        try
        {
            for (var i = 0; i < size; i++)
            {
                objects[index + i] = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
            }
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("The array type is incompatible.", nameof(array));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity(int min)
    {
        var current = this.keys.Length;
        var newCapacity = current == 0 ? DefaultCapacity : current * 2;

        if ((uint)newCapacity > Array.MaxLength)
        {
            newCapacity = Array.MaxLength;
        }

        if (newCapacity < min)
        {
            newCapacity = min;
        }

        this.Capacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TValue GetByIndex(int index)
    {
        if ((uint)index >= (uint)this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return this.values[index];
    }

    public Enumerator GetEnumerator()
        => new(this, Enumerator.KeyValuePair);

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    IDictionaryEnumerator IDictionary.GetEnumerator()
        => new Enumerator(this, Enumerator.DictEntry);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(this, Enumerator.KeyValuePair);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TKey GetKey(int index)
    {
        if ((uint)index >= (uint)this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return this.keys[index];
    }

    public TValue this[TKey key]
    {
        get
        {
            var index = this.IndexOfKey(key);
            if (index >= 0)
            {
                return this.values[index];
            }

            throw new KeyNotFoundException();
        }

        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var index = this.BinarySearch(key);
            if (index >= 0)
            {
                this.values[index] = value;
                this.version++;
                return;
            }

            this.Insert(~index, key, value);
        }
    }

    object? IDictionary.this[object key]
    {
        get
        {
            if (!IsCompatibleKey(key))
            {
                return null;
            }

            var index = this.IndexOfKey((TKey)key);
            return index >= 0 ? this.values[index] : null;
        }

        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key is not TKey typedKey)
            {
                throw new ArgumentException("The key is of an incompatible type.", nameof(key));
            }

            if (value is null)
            {
                if (default(TValue) is not null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this[typedKey] = default!;
                return;
            }

            if (value is not TValue typedValue)
            {
                throw new ArgumentException("The value is of an incompatible type.", nameof(value));
            }

            this[typedKey] = typedValue;
        }
    }

    /// <summary>
    /// Returns the zero-based index of the specified key, or -1 if the key is not present.
    /// </summary>
    public int IndexOfKey(TKey key)
    {
        var index = this.BinarySearch(key);
        return index >= 0 ? index : -1;
    }

    /// <summary>
    /// Returns the half-open range containing the specified key.
    /// </summary>
    /// <returns>The range [Start, End), or (-1, -1) if the key is not present.</returns>
    public (int Start, int End) RangeOfKey(TKey key)
    {
        var index = this.BinarySearch(key);
        return index >= 0 ? (index, index + 1) : (-1, -1);
    }

    /// <summary>
    /// Returns the zero-based index of the first occurrence of the specified value.
    /// </summary>
    public int IndexOfValue(TValue value)
        => Array.IndexOf(this.values, value, 0, this.size);

    private void Insert(int index, TKey key, TValue value)
    {
        var size = this.size;

        if (size == this.keys.Length)
        {
            this.EnsureCapacity(size + 1);
        }

        if (index < size)
        {
            var count = size - index;
            Array.Copy(this.keys, index, this.keys, index + 1, count);
            Array.Copy(this.values, index, this.values, index + 1, count);
        }

        this.keys[index] = key;
        this.values[index] = value;
        this.size = size + 1;
        this.version++;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var index = this.IndexOfKey(key);
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
    public void RemoveAt(int index)
    {
        var size = this.size;
        if ((uint)index >= (uint)size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var newSize = size - 1;

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

    public bool Remove(TKey key)
    {
        var index = this.IndexOfKey(key);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    public bool Remove(TKey key, TValue value)
    {
        var index = this.IndexOfKey(key);
        if (index < 0 || !EqualityComparer<TValue>.Default.Equals(this.values[index], value))
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
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
        var capacity = this.keys.Length;
        var threshold = (int)(capacity * 0.9);

        if (this.size < threshold)
        {
            this.Capacity = this.size;
        }
    }

    private static bool IsCompatibleKey(object key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return key is TKey;
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
            this.key = default;
            this.value = default;
            this.index = 0;
        }

        public KeyValuePair<TKey, TValue> Current
            => new(this.key!, this.value!);

        object IDictionaryEnumerator.Key
        {
            get
            {
                this.ThrowIfInvalidCurrent();
                return this.key!;
            }
        }

        DictionaryEntry IDictionaryEnumerator.Entry
        {
            get
            {
                this.ThrowIfInvalidCurrent();
                return new DictionaryEntry(this.key!, this.value);
            }
        }

        object? IDictionaryEnumerator.Value
        {
            get
            {
                this.ThrowIfInvalidCurrent();
                return this.value;
            }
        }

        object? IEnumerator.Current
        {
            get
            {
                this.ThrowIfInvalidCurrent();

                return this.getEnumeratorRetType == DictEntry
                    ? new DictionaryEntry(this.key!, this.value)
                    : new KeyValuePair<TKey, TValue>(this.key!, this.value!);
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
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
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            this.index = 0;
            this.key = default;
            this.value = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidCurrent()
        {
            if (this.index == 0 || this.index == this.list.size + 1)
            {
                throw new InvalidOperationException();
            }
        }
    }

    public struct KeyEnumerator : IEnumerator<TKey>
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;
        private readonly int version;
        private TKey? current;
        private int index;

        internal KeyEnumerator(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
            this.version = list.version;
            this.current = default;
            this.index = 0;
        }

        public TKey Current => this.current!;

        object? IEnumerator.Current
        {
            get
            {
                this.ThrowIfInvalidCurrent();
                return this.current;
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            if ((uint)this.index < (uint)this.list.size)
            {
                this.current = this.list.keys[this.index++];
                return true;
            }

            this.index = this.list.size + 1;
            this.current = default;
            return false;
        }

        public void Dispose()
        {
            this.index = 0;
            this.current = default;
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            this.index = 0;
            this.current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidCurrent()
        {
            if (this.index == 0 || this.index == this.list.size + 1)
            {
                throw new InvalidOperationException();
            }
        }
    }

    public struct ValueEnumerator : IEnumerator<TValue>
    {
        private readonly OrderedKeyValueList<TKey, TValue> list;
        private readonly int version;
        private TValue? current;
        private int index;

        internal ValueEnumerator(OrderedKeyValueList<TKey, TValue> list)
        {
            this.list = list;
            this.version = list.version;
            this.current = default;
            this.index = 0;
        }

        public TValue Current => this.current!;

        object? IEnumerator.Current
        {
            get
            {
                this.ThrowIfInvalidCurrent();
                return this.current;
            }
        }

        public bool MoveNext()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            if ((uint)this.index < (uint)this.list.size)
            {
                this.current = this.list.values[this.index++];
                return true;
            }

            this.index = this.list.size + 1;
            this.current = default;
            return false;
        }

        public void Dispose()
        {
            this.index = 0;
            this.current = default;
        }

        void IEnumerator.Reset()
        {
            if (this.version != this.list.version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            this.index = 0;
            this.current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidCurrent()
        {
            if (this.index == 0 || this.index == this.list.size + 1)
            {
                throw new InvalidOperationException();
            }
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

        object ICollection.SyncRoot => this.list;

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
        {
            ArgumentNullException.ThrowIfNull(array);
            Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
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

            try
            {
                Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("The array type is incompatible.", nameof(array));
            }
        }

        public void Insert(int index, TKey value)
            => throw new NotSupportedException();

        public int IndexOf(TKey key)
            => this.list.IndexOfKey(key);

        public bool Remove(TKey key)
            => throw new NotSupportedException();

        public void RemoveAt(int index)
            => throw new NotSupportedException();

        /// <summary>
        /// Returns an allocation-free enumerator.
        /// </summary>
        public KeyEnumerator GetEnumerator()
            => new(this.list);

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            => new KeyEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new KeyEnumerator(this.list);
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

        object ICollection.SyncRoot => this.list;

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
        {
            ArgumentNullException.ThrowIfNull(array);
            Array.Copy(this.list.values, 0, array, arrayIndex, this.list.size);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
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

            try
            {
                Array.Copy(this.list.values, 0, array, arrayIndex, this.list.size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("The array type is incompatible.", nameof(array));
            }
        }

        public void Insert(int index, TValue value)
            => throw new NotSupportedException();

        public int IndexOf(TValue value)
            => Array.IndexOf(this.list.values, value, 0, this.list.size);

        public bool Remove(TValue value)
            => throw new NotSupportedException();

        public void RemoveAt(int index)
            => throw new NotSupportedException();

        /// <summary>
        /// Returns an allocation-free enumerator.
        /// </summary>
        public ValueEnumerator GetEnumerator()
            => new(this.list);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            => new ValueEnumerator(this.list);

        IEnumerator IEnumerable.GetEnumerator()
            => new ValueEnumerator(this.list);
    }

    #endregion
}
