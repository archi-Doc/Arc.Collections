// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1401
#pragma warning disable SA1611 // Element parameters should be documented
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text

namespace Arc.Collections;

/// <summary>
/// Represents a thread-safe collection of UTF-8 key/value pairs.<br/>
/// Writes are serialized, while lookups are lock-free.<br/>
/// Optimized for collections that are built infrequently and read frequently.<br/>
/// Stored key arrays must not be modified after insertion.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
public class Utf8Hashtable<TValue>
{
    private const int MaximumCapacity = 1 << 30;

    private sealed class Item
    {
        internal readonly byte[] Key;
        internal readonly TValue Value;
        internal readonly int Hash;
        internal readonly Item? Next;

        internal Item(byte[] key, TValue value, int hash, Item? next)
        {
            this.Key = key;
            this.Value = value;
            this.Hash = hash;
            this.Next = next;
        }
    }

    private readonly Lock lockObject = new();
    private Item?[] table;
    private int count;

    /// <summary>
    /// Gets the number of items in the hashtable.
    /// </summary>
    public int Count => Volatile.Read(ref this.count);

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8Hashtable{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    public Utf8Hashtable(int capacity = 4)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        var size = HashtableHelper.CalculateCapacity(capacity);
        this.table = new Item?[size];
    }

    /// <summary>
    /// Gets an array containing all values.
    /// </summary>
    /// <returns>A new array containing the elements.</returns>
    public TValue[] ToArray()
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var values = new TValue[this.count];
            var n = 0;

            for (var i = 0; i < table.Length; i++)
            {
                for (var item = table[i]; item is not null; item = item.Next)
                {
                    values[n++] = item.Value;
                }
            }

            return values;
        }
    }

    /// <summary>
    /// Gets an array containing all key-value pairs.
    /// </summary>
    /// <returns>A new array containing the key-value pairs.</returns>
    public KeyValuePair<byte[], TValue>[] ToKeyValuePairs()
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var pairs = new KeyValuePair<byte[], TValue>[this.count];
            var n = 0;

            for (var i = 0; i < table.Length; i++)
            {
                for (var item = table[i]; item is not null; item = item.Next)
                {
                    pairs[n++] = new(item.Key, item.Value);
                }
            }

            return pairs;
        }
    }

    /// <summary>
    /// Attempts to add a key-value pair.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the element was added; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(byte[] key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.AddInternal(key, value, false, out _);
    }

    /// <summary>
    /// Attempts to add a key-value pair.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the element was added; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(ReadOnlySpan<byte> key, TValue value)
        => this.AddInternal(key, value, false, out _);

    /// <summary>
    /// Adds or updates a key-value pair.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Add(byte[] key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        this.AddInternal(key, value, true, out _);
    }

    /// <summary>
    /// Adds or updates a key-value pair.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Add(ReadOnlySpan<byte> key, TValue value)
        => this.AddInternal(key, value, true, out _);

    /// <summary>
    /// Gets the existing value or adds a newly created value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The factory invoked to create the value when the key is absent.</param>
    /// <returns>The existing value, or the newly created value.</returns>
    /// <remarks><paramref name="valueFactory"/> is invoked while holding the internal lock;
    /// it must not call back into this hashtable.</remarks>
    public TValue GetOrAdd(byte[] key, Func<byte[], TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueFactory);

        if (this.TryGetValue(key, out var value))
        {
            return value;
        }

        return this.GetOrAddSlow(key, valueFactory);
    }

    /// <summary>
    /// Gets the existing value or adds a newly created value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The factory invoked to create the value when the key is absent.</param>
    /// <returns>The existing value, or the newly created value.</returns>
    /// <remarks><paramref name="valueFactory"/> is invoked while holding the internal lock;
    /// it must not call back into this hashtable.</remarks>
    public TValue GetOrAdd(ReadOnlySpan<byte> key, Func<byte[], TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        if (this.TryGetValue(key, out var value))
        {
            return value;
        }

        return this.GetOrAddSlow(key, valueFactory);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(byte[] key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var table = Volatile.Read(ref this.table);
        var hash = GetHashCode(key);
        var item = Volatile.Read(ref table[hash & (table.Length - 1)]);

        while (item is not null)
        {
            if (item.Hash == hash && key.AsSpan().SequenceEqual(item.Key))
            {
                value = item.Value;
                return true;
            }

            item = item.Next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<byte> key, [MaybeNullWhen(false)] out TValue value)
    {
        var table = Volatile.Read(ref this.table);
        var hash = GetHashCode(key);
        var item = Volatile.Read(ref table[hash & (table.Length - 1)]);

        while (item is not null)
        {
            if (item.Hash == hash && key.SequenceEqual(item.Key))
            {
                value = item.Value;
                return true;
            }

            item = item.Next;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the hashtable contains the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(byte[] key)
        => this.TryGetValue(key, out _);

    /// <summary>
    /// Determines whether the hashtable contains the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ReadOnlySpan<byte> key)
        => this.TryGetValue(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.TryRemove(key.AsSpan(), out _);
    }

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(byte[] key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this.TryRemove(key.AsSpan(), out value);
    }

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(ReadOnlySpan<byte> key)
        => this.TryRemove(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(ReadOnlySpan<byte> key, [MaybeNullWhen(false)] out TValue value)
    {
        var hash = GetHashCode(key);
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = hash & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Hash != hash || !key.SequenceEqual(item.Key))
                {
                    continue;
                }

                value = item.Value;

                // Rebuild the chain without the target, cloning only the prefix,
                // so that nodes visible to lock-free readers are never mutated.
                var newHead = item.Next;
                for (var p = head; !ReferenceEquals(p, item); p = p.Next!)
                {
                    newHead = new Item(p!.Key, p.Value, p.Hash, newHead);
                }

                Volatile.Write(ref table[bucketIndex], newHead);
                Volatile.Write(ref this.count, this.count - 1);
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// Removes all key-value pairs.
    /// </summary>
    public void Clear()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.count == 0)
            {
                return;
            }

            var table = new Item?[this.table.Length];

            Volatile.Write(ref this.table, table);
            Volatile.Write(ref this.count, 0);
        }
    }

    private bool AddInternal(byte[] key, TValue value, bool updateValue, out TValue resultingValue)
    {
        var hash = GetHashCode(key);
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = hash & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Hash != hash || !key.AsSpan().SequenceEqual(item.Key))
                {
                    continue;
                }

                if (!updateValue)
                {
                    resultingValue = item.Value;
                    return false;
                }

                var newHead = ReplaceValue(head!, item, value);

                Volatile.Write(ref table[bucketIndex], newHead);

                resultingValue = value;
                return false;
            }

            if (this.count > (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
                head = table[bucketIndex];
            }

            var newItem = new Item(key, value, hash, head);

            Volatile.Write(ref table[bucketIndex], newItem);
            Volatile.Write(ref this.count, this.count + 1);

            resultingValue = value;
            return true;
        }
    }

    private bool AddInternal(ReadOnlySpan<byte> key, TValue value, bool updateValue, out TValue resultingValue)
    {
        var hash = GetHashCode(key);
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = hash & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Hash != hash || !key.SequenceEqual(item.Key))
                {
                    continue;
                }

                if (!updateValue)
                {
                    resultingValue = item.Value;
                    return false;
                }

                var newHead = ReplaceValue(head!, item, value);

                Volatile.Write(ref table[bucketIndex], newHead);

                resultingValue = value;
                return false;
            }

            if (this.count > (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
                head = table[bucketIndex];
            }

            // Materialize the key only when actually inserting.
            var arrayKey = key.ToArray();
            var newItem = new Item(arrayKey, value, hash, head);

            Volatile.Write(ref table[bucketIndex], newItem);
            Volatile.Write(ref this.count, this.count + 1);

            resultingValue = value;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue GetOrAddSlow(byte[] key, Func<byte[], TValue> valueFactory)
    {
        var hash = GetHashCode(key);
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = hash & (table.Length - 1);

            for (var item = table[bucketIndex]; item is not null; item = item.Next)
            {
                if (item.Hash == hash && key.AsSpan().SequenceEqual(item.Key))
                {
                    return item.Value;
                }
            }

            var value = valueFactory(key);

            if (this.count > (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
            }

            var newItem = new Item(key, value, hash, table[bucketIndex]);

            Volatile.Write(ref table[bucketIndex], newItem);
            Volatile.Write(ref this.count, this.count + 1);

            return value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue GetOrAddSlow(ReadOnlySpan<byte> key, Func<byte[], TValue> valueFactory)
    {
        var hash = GetHashCode(key);
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = hash & (table.Length - 1);

            for (var item = table[bucketIndex]; item is not null; item = item.Next)
            {
                if (item.Hash == hash && key.SequenceEqual(item.Key))
                {
                    return item.Value;
                }
            }

            var arrayKey = key.ToArray();
            var value = valueFactory(arrayKey);

            if (this.count > (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
            }

            var newItem = new Item(arrayKey, value, hash, table[bucketIndex]);

            Volatile.Write(ref table[bucketIndex], newItem);
            Volatile.Write(ref this.count, this.count + 1);

            return value;
        }
    }

    private void RebuildTable()
    {
        var table = this.table;

        if (table.Length >= MaximumCapacity)
        {
            throw new InvalidOperationException(
                "The maximum capacity of the hashtable has been reached.");
        }

        var nextTable = new Item?[table.Length << 1];
        var mask = nextTable.Length - 1;

        for (var i = 0; i < table.Length; i++)
        {
            var item = table[i];

            while (item is not null)
            {
                var bucketIndex = item.Hash & mask;

                nextTable[bucketIndex] =
                    new Item(item.Key, item.Value, item.Hash, nextTable[bucketIndex]);

                item = item.Next;
            }
        }

        Volatile.Write(ref this.table, nextTable);
    }

    /// <summary>
    /// Replaces a value without modifying nodes visible to lock-free readers.
    /// </summary>
    private static Item ReplaceValue(Item head, Item target, TValue value)
    {
        // Clone only the prefix that points to the replaced node and reuse the
        // immutable suffix, instead of cloning the entire chain.
        var newHead = new Item(target.Key, value, target.Hash, target.Next);
        var item = head;

        while (!ReferenceEquals(item, target))
        {
            newHead = new Item(item.Key, item.Value, item.Hash, newHead);
            item = item.Next!;
        }

        return newHead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCode(ReadOnlySpan<byte> key)
        => unchecked((int)XxHash3Slim.Hash64(key));
}
