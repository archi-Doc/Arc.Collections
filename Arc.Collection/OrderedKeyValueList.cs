// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Arc.Collection.HotMethod;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1401
#pragma warning disable SA1405 // Debug.Assert should provide message text

namespace Arc.Collection
{
    /// <summary>
    /// Represents a list of key-value pairs that can be accessed by index and maintained in sorted order.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    public class OrderedKeyValueList<TKey, TValue> :
        IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        protected TKey[] keys;
        protected TValue[] values;
        protected KeyList? keyList;
        protected ValueList? valueList;
        protected int size;
        protected int version;

        private const int DefaultCapacity = 4;

        public IComparer<TKey> Comparer { get; private set; }

        public IHotMethod<TKey>? HotMethod { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        public OrderedKeyValueList()
        {
            this.keys = Array.Empty<TKey>();
            this.values = Array.Empty<TValue>();
            this.Comparer = Comparer<TKey>.Default;
            this.HotMethod = HotMethodResolver.Get<TKey>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public OrderedKeyValueList(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.keys = new TKey[capacity];
            this.values = new TValue[capacity];
            this.Comparer = Comparer<TKey>.Default;
            this.HotMethod = HotMethodResolver.Get<TKey>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing keys.</param>
        public OrderedKeyValueList(IComparer<TKey> comparer)
            : this()
        {
            this.Comparer = comparer ?? Comparer<TKey>.Default;
            this.HotMethod = HotMethodResolver.Get<TKey>(this.Comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <param name="comparer">The default comparer to use for comparing keys.</param>
        public OrderedKeyValueList(int capacity, IComparer<TKey> comparer)
            : this(comparer)
        {
            this.Capacity = capacity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The IDictionary implementation to copy to a new list.</param>
        public OrderedKeyValueList(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null!)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedKeyValueList{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The IDictionary implementation to copy to a new list.</param>
        /// <param name="comparer">The default comparer to use for comparing keys.</param>
        public OrderedKeyValueList(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
            : this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            int count = dictionary.Count;
            if (count != 0)
            {
                dictionary.Keys.CopyTo(this.keys, 0);
                dictionary.Values.CopyTo(this.values, 0);
                Debug.Assert(count == this.keys.Length);
                if (count > 1)
                {
                    Array.Sort<TKey, TValue>(this.keys, this.values, this.Comparer);
                }
            }

            this.size = count;
        }

        /// <summary>
        /// Searches a list for the specific value.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The index of the specified value in list. If the value is not found, the negative number returned is the bitwise complement of the index of the first element that is larger than value.</returns>
        public int BinarySearch(TKey key) // => Array.BinarySearch(this.items, 0, this.size, value, this.Comparer);
        {
            if (this.HotMethod != null)
            {
                return this.HotMethod.BinarySearch(this.keys, 0, this.size, key);
            }

            var min = 0;
            var max = this.size - 1;

            if (this.Comparer == Comparer<TKey>.Default && key is IComparable<TKey> ic)
            {// IComparable<T>
                while (min <= max)
                {
                    var mid = min + ((max - min) / 2);
                    var cmp = ic.CompareTo(this.keys[mid]); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                    if (cmp < 0)
                    {
                        max = mid - 1;
                        continue;
                    }
                    else if (cmp > 0)
                    {
                        min = mid + 1;
                        continue;
                    }
                    else
                    {// Found
                        return mid;
                    }
                }
            }
            else
            {// IComparer<T>
                while (min <= max)
                {
                    var mid = min + ((max - min) / 2);
                    var cmp = this.Comparer.Compare(key, this.keys[mid]); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd
                    if (cmp < 0)
                    {
                        max = mid - 1;
                        continue;
                    }
                    else if (cmp > 0)
                    {
                        min = mid + 1;
                        continue;
                    }
                    else
                    {// Found
                        return mid;
                    }
                }
            }

            return ~min;
        }

        #region IDictionary

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var pos = this.BinarySearch(key);
            if (pos < 0)
            {
                this.Insert(~pos, key, value);
            }
            else
            {// Adds to the end of the same keys.
                pos++;
                while (pos < this.size && this.Comparer.Compare(this.keys[pos], key) == 0)
                {
                    pos++;
                }

                this.Insert(pos, key, value);
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) => this.Add(keyValuePair.Key, keyValuePair.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var range = this.RangeOfKey(keyValuePair.Key);
            for (var n = range.start; n < range.end; n++)
            {
                if (n >= 0 && EqualityComparer<TValue>.Default.Equals(this.values[n], keyValuePair.Value))
                {
                    return true;
                }
            }

            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var range = this.RangeOfKey(keyValuePair.Key);
            for (var n = range.start; n < range.end; n++)
            {
                if (n >= 0 && EqualityComparer<TValue>.Default.Equals(this.values[n], keyValuePair.Value))
                {
                    this.RemoveAt(n);
                    return true;
                }
            }

            return false;
        }

        #endregion

        public int Capacity
        {
            get
            {
                return this.keys.Length;
            }

            set
            {
                if (value != this.keys.Length)
                {
                    if (value < this.size)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    if (value > 0)
                    {
                        var newKeys = new TKey[value];
                        var newValues = new TValue[value];
                        if (this.size > 0)
                        {
                            Array.Copy(this.keys, newKeys, this.size);
                            Array.Copy(this.values, newValues, this.size);
                        }

                        this.keys = newKeys;
                        this.values = newValues;
                    }
                    else
                    {
                        this.keys = Array.Empty<TKey>();
                        this.values = Array.Empty<TValue>();
                    }
                }
            }
        }

        void IDictionary.Add(object key, object? value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null && !(default(TValue) == null))
            {// null is an invalid value for Value types
                throw new ArgumentNullException(nameof(value));
            }

            if (!(key is TKey))
            {
                throw new ArgumentException(nameof(key));
            }

            if (!(value is TValue) && value != null)
            {// null is a valid value for Reference Types
                throw new ArgumentException(nameof(value));
            }

            this.Add((TKey)key, (TValue)value!);
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

        private KeyList GetKeyListHelper()
        {
            return this.keyList != null ? this.keyList : (this.keyList = new KeyList(this));
        }

        private ValueList GetValueListHelper()
        {
            return this.valueList != null ? this.valueList : (this.valueList = new ValueList(this));
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        bool IDictionary.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

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

            this.version++;
            this.size = 0;
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return this.ContainsKey((TKey)key);
            }

            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return this.IndexOfKey(key) >= 0;
        }

        public bool ContainsValue(TValue value)
        {
            return this.IndexOfValue(value) >= 0;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < this.Count; i++)
            {
                var entry = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
                array[arrayIndex + i] = entry;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(nameof(array));
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < this.Count)
            {
                throw new ArgumentException();
            }

            KeyValuePair<TKey, TValue>[]? keyValuePairArray = array as KeyValuePair<TKey, TValue>[];
            if (keyValuePairArray != null)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    keyValuePairArray[i + index] = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
                }
            }
            else
            {
                object[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(nameof(array));
                }

                try
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        objects[i + index] = new KeyValuePair<TKey, TValue>(this.keys[i], this.values[i]);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }
        }

        private const int MaxArrayLength = 0X7FEFFFFF;

        private void EnsureCapacity(int min)
        {
            int newCapacity = this.keys.Length == 0 ? DefaultCapacity : this.keys.Length * 2;

            if ((uint)newCapacity > MaxArrayLength)
            {
                newCapacity = MaxArrayLength;
            }
            else if (newCapacity < min)
            {
                newCapacity = min;
            }

            this.Capacity = newCapacity;
        }

        private TValue GetByIndex(int index)
        {
            if (index < 0 || index >= this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.values[index];
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DictEntry);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KeyValuePair);

        private TKey GetKey(int index)
        {
            if (index < 0 || index >= this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.keys[index];
        }

        public TValue this[TKey key]
        {
            get
            {
                int i = this.IndexOfKey(key);
                if (i >= 0)
                {
                    return this.values[i];
                }

                throw new KeyNotFoundException();
            }

            set
            {
                this.Add(key, value);
            }
        }

        object? IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    int i = this.IndexOfKey((TKey)key);
                    if (i >= 0)
                    {
                        return this.values[i];
                    }
                }

                return null;
            }

            set
            {
                ((IDictionary)this).Add(key, value);
            }
        }

        /// <summary>
        /// Returns the zero-based index of the specified key in a list.
        /// </summary>
        /// <param name="key">The key to locate in the list.</param>
        /// <returns>The zero-based index of the key parameter, if key is found in the list; otherwise, -1.</returns>
        public int IndexOfKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var ret = this.BinarySearch(key);
            ret--;
            while (ret >= 0 && this.Comparer.Compare(this.keys[ret], key) == 0)
            {
                ret--;
            }

            ret++;
            return ret >= 0 ? ret : -1;
        }

        /// <summary>
        /// Returns a range of indexes with the specified key in a list.
        /// </summary>
        /// <param name="key">The key to locate in the list.</param>
        /// <returns>The zero-based index of the key parameter, if key is found in the list; otherwise, -1.</returns>
        public (int start, int end) RangeOfKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var ret = this.BinarySearch(key);
            if (ret < 0)
            {
                return (-1, -1);
            }

            ret--;
            while (ret >= 0 && this.Comparer.Compare(this.keys[ret], key) == 0)
            {
                ret--;
            }

            ret++;

            int n;
            for (n = ret + 1; n < this.size; n++)
            {
                if (this.Comparer.Compare(key, this.keys[n]) != 0)
                {
                    break;
                }
            }

            return (ret, n);
        }

        /// <summary>
        /// Returns the zero-based index of the first occurrence of the specified value in a list.
        /// </summary>
        /// <param name="value">The value to locate in the list.</param>
        /// <returns>The zero-based index of the first occurrence of the value parameter, if value is found in the list; otherwise, -1.</returns>
        public int IndexOfValue(TValue value)
        {
            return Array.IndexOf(this.values, value, 0, this.size);
        }

        private void Insert(int index, TKey key, TValue value)
        {
            if (this.size == this.keys.Length)
            {
                this.EnsureCapacity(this.size + 1);
            }

            if (index < this.size)
            {
                Array.Copy(this.keys, index, this.keys, index + 1, this.size - index);
                Array.Copy(this.values, index, this.values, index + 1, this.size - index);
            }

            this.keys[index] = key;
            this.values[index] = value;
            this.size++;
            this.version++;
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            var i = this.IndexOfKey(key);
            if (i >= 0)
            {
                value = this.values[i];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes the element at the specified index of a list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= this.size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index < this.size)
            {
                Array.Copy(this.keys, index + 1, this.keys, index, this.size - index);
                Array.Copy(this.values, index + 1, this.values, index, this.size - index);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                this.keys[this.size] = default(TKey)!;
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                this.values[this.size] = default(TValue)!;
            }

            this.version++;
            this.size--;
        }

        public bool Remove(TKey key)
        {
            var i = this.IndexOfKey(key);
            if (i >= 0)
            {
                this.RemoveAt(i);
            }

            return i >= 0;
        }

        public bool Remove(TKey key, TValue value)
        {
            var range = this.RangeOfKey(key);
            for (var n = range.start; n < range.end; n++)
            {
                if (n >= 0 && EqualityComparer<TValue>.Default.Equals(this.values[n], value))
                {
                    this.RemoveAt(n);
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
            int threshold = (int)(((double)this.keys.Length) * 0.9);
            if (this.size < threshold)
            {
                this.Capacity = this.size;
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return key is TKey;
        }

        #region Enumerator

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly OrderedKeyValueList<TKey, TValue> list;
            private readonly int version;
            private readonly int getEnumeratorRetType;  // What should Enumerator.Current return?
            private TKey? key;
            private TValue? value;
            private int index;

            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            internal Enumerator(OrderedKeyValueList<TKey, TValue> list, int getEnumeratorRetType)
            {
                this.list = list;
                this.index = 0;
                this.version = this.list.version;
                this.getEnumeratorRetType = getEnumeratorRetType;
                this.key = default;
                this.value = default;
            }

            public void Dispose()
            {
                this.index = 0;
                this.key = default;
                this.value = default;
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return this.key!;
                }
            }

            public bool MoveNext()
            {
                if (this.version != this.list.version)
                {
                    throw new InvalidOperationException();
                }

                if ((uint)this.index < (uint)this.list.Count)
                {
                    this.key = this.list.keys[this.index];
                    this.value = this.list.values[this.index];
                    this.index++;
                    return true;
                }

                this.index = this.list.Count + 1;
                this.key = default;
                this.value = default;
                return false;
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return new DictionaryEntry(this.key!, this.value);
                }
            }

            public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(this.key!, this.value!);

            object? IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    if (this.getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(this.key!, this.value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(this.key!, this.value!);
                    }
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return this.value;
                }
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
        }

        private sealed class SortedListKeyEnumerator : IEnumerator<TKey>, IEnumerator
        {
            private readonly OrderedKeyValueList<TKey, TValue> list;
            private int index;
            private int version;
            private TKey? currentKey;

            internal SortedListKeyEnumerator(OrderedKeyValueList<TKey, TValue> list)
            {
                this.list = list;
                this.version = list.version;
            }

            public void Dispose()
            {
                this.index = 0;
                this.currentKey = default;
            }

            public bool MoveNext()
            {
                if (this.version != this.list.version)
                {
                    throw new InvalidOperationException();
                }

                if ((uint)this.index < (uint)this.list.Count)
                {
                    this.currentKey = this.list.keys[this.index];
                    this.index++;
                    return true;
                }

                this.index = this.list.Count + 1;
                this.currentKey = default;
                return false;
            }

            public TKey Current => this.currentKey!;

            object? IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return this.currentKey;
                }
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

        private sealed class SortedListValueEnumerator : IEnumerator<TValue>, IEnumerator
        {
            private readonly OrderedKeyValueList<TKey, TValue> list;
            private int index;
            private int version;
            private TValue? currentValue;

            internal SortedListValueEnumerator(OrderedKeyValueList<TKey, TValue> list)
            {
                this.list = list;
                this.version = list.version;
            }

            public void Dispose()
            {
                this.index = 0;
                this.currentValue = default;
            }

            public bool MoveNext()
            {
                if (this.version != this.list.version)
                {
                    throw new InvalidOperationException();
                }

                if ((uint)this.index < (uint)this.list.Count)
                {
                    this.currentValue = this.list.values[this.index];
                    this.index++;
                    return true;
                }

                this.index = this.list.Count + 1;
                this.currentValue = default;
                return false;
            }

            public TValue Current => this.currentValue!;

            object? IEnumerator.Current
            {
                get
                {
                    if (this.index == 0 || (this.index == this.list.Count + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    return this.currentValue;
                }
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

            public void Add(TKey key) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(TKey key) => this.list.ContainsKey(key);

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.Count);
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array == null || array.Rank != 1)
                {
                    throw new ArgumentException(nameof(array));
                }

                try
                {
                    Array.Copy(this.list.keys, 0, array, arrayIndex, this.list.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }

            public void Insert(int index, TKey value) => new NotSupportedException();

            public TKey this[int index]
            {
                get
                {
                    return this.list.GetKey(index);
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public IEnumerator<TKey> GetEnumerator() => new SortedListKeyEnumerator(this.list);

            IEnumerator IEnumerable.GetEnumerator() => new SortedListKeyEnumerator(this.list);

            public int IndexOf(TKey key) => this.list.IndexOfKey(key);

            public bool Remove(TKey key) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();
        }

        public sealed class ValueList : IList<TValue>, ICollection
        {
            private readonly OrderedKeyValueList<TKey, TValue> list; // Do not rename (binary serialization)

            internal ValueList(OrderedKeyValueList<TKey, TValue> list)
            {
                this.list = list;
            }

            public int Count => this.list.size;

            public bool IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)this.list).SyncRoot;

            public void Add(TValue key) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(TValue value) => this.list.ContainsValue(value);

            public void CopyTo(TValue[] array, int arrayIndex) => Array.Copy(this.list.values, 0, array, arrayIndex, this.list.Count);

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null || array.Rank != 1)
                {
                    throw new ArgumentException(nameof(array));
                }

                try
                {
                    Array.Copy(this.list.values, 0, array, index, this.list.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(nameof(array));
                }
            }

            public void Insert(int index, TValue value) => new NotSupportedException();

            public TValue this[int index]
            {
                get
                {
                    return this.list.GetByIndex(index);
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public IEnumerator<TValue> GetEnumerator() => new SortedListValueEnumerator(this.list);

            IEnumerator IEnumerable.GetEnumerator() => new SortedListValueEnumerator(this.list);

            public int IndexOf(TValue value) => Array.IndexOf(this.list.values, value, 0, this.list.Count);

            public bool Remove(TValue value) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();
        }

        #endregion
    }
}
