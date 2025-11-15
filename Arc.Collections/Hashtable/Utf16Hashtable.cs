// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

#pragma warning disable SA1401

namespace Arc.Collections;

/// <summary>
/// Represents a collection of utf-16 key and value pairs.<br/>
/// It is thread-safe, and it locks when adding items, but it is lock-free when retrieving them.<br/>
/// Please use this for use cases where the collection is initially built and then primarily used for retrieval.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
public class Utf16Hashtable<TValue>
{
    private class Item
    {
        public string Key;
        public TValue Value;
        public int Hash;
        public Item? Next;

        public Item(string key, TValue value, int hash)
        {
            this.Key = key;
            this.Value = value;
            this.Hash = hash;
        }
    }

    private readonly Lock lockObject = new();
    private Item?[] table;
    private int count;

    public int Count => this.count;

    public Utf16Hashtable(int capacity = 4)
    {
        var size = HashtableHelper.CalculateCapacity(capacity);
        this.table = new Item[size];
    }

    /// <summary>
    /// Gets an array of values.
    /// </summary>
    /// <returns>An array of values.</returns>
    public TValue[] ToArray()
    {
        using (this.lockObject.EnterScope())
        {
            var t = this.table;
            var values = new TValue[this.count];
            var n = 0;
            for (var i = 0; i < t.Length; i++)
            {
                if (t[i] is { } item)
                {
                    values[n++] = item.Value;
                    if (n >= this.count)
                    {
                        break;
                    }
                }
            }

            return values;
        }
    }

    /// <summary>
    /// Attempts to add a key-value pair to the hashtable.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><c>true</c> if the key-value pair was added successfully; otherwise, <c>false</c> if the key already exists.</returns>
    public bool TryAdd(string key, TValue value)
        => this.AddInternal(key, false, _ => value, out _);

    /// <summary>
    /// Attempts to add a key-value pair to the hashtable.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><c>true</c> if the key-value pair was added successfully; otherwise, <c>false</c> if the key already exists.</returns>
    public bool TryAdd(ReadOnlySpan<char> key, TValue value)
        => this.AddInternal(key, false, _ => value, out _);

    /// <summary>
    /// Adds a key-value pair to the hashtable.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    public void Add(string key, TValue value)
        => this.AddInternal(key, true, _ => value, out _);

    /// <summary>
    /// Adds a key-value pair to the hashtable.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    public void Add(ReadOnlySpan<char> key, TValue value)
        => this.AddInternal(key, true, _ => value, out _);

    /// <summary>
    /// Gets the value associated with the specified key if it exists in the hashtable; otherwise, adds a new key-value pair using the specified value factory function and returns the added value.
    /// </summary>
    /// <param name="key">The key to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key if it doesn't exist.</param>
    /// <returns>The value associated with the specified key if it exists; otherwise, the newly added value.</returns>
    public TValue GetOrAdd(string key, Func<string, TValue> valueFactory)
    {
        TValue? v;
        if (this.TryGetValue(key, out v))
        {
            return v;
        }

        this.AddInternal(key, false, valueFactory, out v);
        return v;
    }

    /// <summary>
    /// Gets the value associated with the specified key if it exists in the hashtable; otherwise, adds a new key-value pair using the specified value factory function and returns the added value.
    /// </summary>
    /// <param name="key">The key to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key if it doesn't exist.</param>
    /// <returns>The value associated with the specified key if it exists; otherwise, the newly added value.</returns>
    public TValue GetOrAdd(ReadOnlySpan<char> key, Func<string, TValue> valueFactory)
    {
        TValue? v;
        if (this.TryGetValue(key, out v))
        {
            return v;
        }

        this.AddInternal(key, false, valueFactory, out v);
        return v;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the hashtable.
    /// </summary>
    /// <param name="key">The key to retrieve the value for.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the key was found and the value was successfully retrieved; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
    {
        var table = this.table;
        var hash = unchecked((int)XxHash3Slim.Hash64(key));
        var item = table[hash & (table.Length - 1)];

        while (item != null)
        {
            if (key.SequenceEqual(item.Key.AsSpan()))
            {// Identical
                value = item.Value;
                return true;
            }

            item = item.Next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Clears the hashtable, removing all key-value pairs.
    /// </summary>
    public void Clear()
    {
        using (this.lockObject.EnterScope())
        {
            for (var n = 0; n < this.table.Length; n++)
            {
                this.table[n] = default;
            }
        }
    }

    private bool AddInternal(string key, bool updateValue, Func<string, TValue> valueFactory, out TValue resultingValue)
    {
        using (this.lockObject.EnterScope())
        {
            if ((this.count * 2) > this.table.Length)
            {// Rebuild table
                this.RebuildTable();
            }

            var table = this.table;
            var hash = unchecked((int)XxHash3Slim.Hash64(key));
            var h = hash & (table.Length - 1);

            if (table[h] is null)
            {
                resultingValue = valueFactory(key);
                var item = new Item(key, resultingValue, hash);
                table[h] = item;

                this.count++;
                return true;
            }
            else
            {
                var i = table[h]!;
                while (true)
                {
                    if (key == i.Key)
                    {// Identical
                        if (updateValue)
                        {
                            i.Value = valueFactory(key);
                        }

                        resultingValue = i.Value;
                        return false;
                    }

                    if (i.Next == null)
                    { // Last item.
                        break;
                    }

                    i = i.Next;
                }

                resultingValue = valueFactory(key);
                var item = new Item(key, resultingValue, hash);
                i.Next = item;

                this.count++;
                return true;
            }
        }
    }

    private bool AddInternal(ReadOnlySpan<char> key, bool updateValue, Func<string, TValue> valueFactory, out TValue resultingValue)
    {
        using (this.lockObject.EnterScope())
        {
            if ((this.count * 2) > this.table.Length)
            {// Rebuild table
                this.RebuildTable();
            }

            var table = this.table;
            var hash = unchecked((int)XxHash3Slim.Hash64(key));
            var h = hash & (table.Length - 1);

            if (table[h] is null)
            {
                var st = key.ToString();
                resultingValue = valueFactory(st);
                var item = new Item(st, resultingValue, hash);
                table[h] = item;

                this.count++;
                return true;
            }
            else
            {
                var i = table[h]!;
                while (true)
                {
                    if (key == i.Key)
                    {// Identical
                        if (updateValue)
                        {
                            i.Value = valueFactory(key.ToString());
                        }

                        resultingValue = i.Value;
                        return false;
                    }

                    if (i.Next == null)
                    { // Last item.
                        break;
                    }

                    i = i.Next;
                }

                var st = key.ToString();
                resultingValue = valueFactory(st);
                var item = new Item(st, resultingValue, hash);
                i.Next = item;

                this.count++;
                return true;
            }
        }
    }

    private void RebuildTable()
    {// lock(cs) required.
        var nextCapacity = this.table.Length * 2;
        var nextTable = new Item[nextCapacity];
        for (var i = 0; i < this.table.Length; i++)
        {
            var e = this.table[i];
            while (e != null)
            {
                var newItem = new Item(e.Key, e.Value, e.Hash);
                this.AddItem(nextTable, newItem);
                e = e.Next;
            }
        }

        Volatile.Write(ref this.table, nextTable);
    }

    private bool AddItem(Item[] table, Item item)
    {// lock(cs) required.
        var h = item.Hash & (table.Length - 1);

        if (table[h] == null)
        {
            table[h] = item;
        }
        else
        {
            var i = table[h];
            while (true)
            {
                if (i.Key == item.Key)
                {// Identical
                    return false;
                }

                if (i.Next == null)
                { // Last item.
                    break;
                }

                i = i.Next;
            }

            i.Next = item;
        }

        return true;
    }
}
