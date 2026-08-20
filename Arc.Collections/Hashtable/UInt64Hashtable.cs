// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
/// Represents a thread-safe collection of ulong/value pairs.<br/>
/// Writes are serialized, while lookups are lock-free.<br/>
/// Optimized for collections that are built infrequently and read frequently.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
public class UInt64Hashtable<TValue>
{
    private const int MaximumCapacity = 1 << 30;

    private sealed class Item
    {
        internal readonly ulong Key;
        internal readonly TValue Value;
        internal readonly Item? Next;

        internal Item(ulong key, TValue value, Item? next)
        {
            this.Key = key;
            this.Value = value;
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
    /// Initializes a new instance of the <see cref="UInt64Hashtable{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    public UInt64Hashtable(int capacity = 4)
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
    public TValue[] ToArray()
    {
        using (this.lockObject.EnterScope())
        {
            var count = this.count;
            var values = new TValue[count];
            var table = this.table;
            var n = 0;

            for (var i = 0; i < table.Length; i++)
            {
                var item = table[i];

                while (item is not null)
                {
                    values[n++] = item.Value;
                    item = item.Next;
                }
            }

            return values;
        }
    }

    /// <summary>
    /// Attempts to add a key-value pair.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the pair was added;
    /// otherwise, <see langword="false"/> if the key already exists.
    /// </returns>
    public bool TryAdd(ulong key, TValue value)
        => this.AddInternal(key, value, false, out _);

    /// <summary>
    /// Adds or updates a key-value pair.
    /// </summary>
    public void Add(ulong key, TValue value)
        => this.AddInternal(key, value, true, out _);

    /// <summary>
    /// Gets the existing value or adds a newly created value.
    /// </summary>
    /// <remarks><paramref name="valueFactory"/> is invoked while holding the internal lock;
    /// it must not call back into this hashtable.</remarks>
    public TValue GetOrAdd(ulong key, Func<ulong, TValue> valueFactory)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out TValue value)
    {
        var table = Volatile.Read(ref this.table);
        var hash = GetHashCode(key);
        var item = Volatile.Read(ref table[hash & (table.Length - 1)]);

        while (item is not null)
        {
            if (item.Key == key)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ulong key)
        => this.TryGetValue(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(ulong key)
        => this.TryRemove(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(ulong key, [MaybeNullWhen(false)] out TValue value)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = GetHashCode(key) & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Key != key)
                {
                    continue;
                }

                value = item.Value;

                // Rebuild the chain without the target, cloning only the prefix,
                // so that nodes visible to lock-free readers are never mutated.
                var newHead = item.Next;
                for (var p = head; !ReferenceEquals(p, item); p = p.Next!)
                {
                    newHead = new Item(p.Key, p.Value, newHead);
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

    private bool AddInternal(ulong key, TValue value, bool updateValue, out TValue resultingValue)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var hash = GetHashCode(key);
            var bucketIndex = hash & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Key != key)
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

            if (this.count >= (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
                head = table[bucketIndex];
            }

            var newItem = new Item(key, value, head);

            // Publish the fully initialized immutable chain.
            Volatile.Write(ref table[bucketIndex], newItem);
            Volatile.Write(ref this.count, this.count + 1);

            resultingValue = value;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue GetOrAddSlow(ulong key, Func<ulong, TValue> valueFactory)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var hash = GetHashCode(key);
            var bucketIndex = hash & (table.Length - 1);
            var item = table[bucketIndex];

            while (item is not null)
            {
                if (item.Key == key)
                {
                    return item.Value;
                }

                item = item.Next;
            }

            var value = valueFactory(key);

            if (this.count >= (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
            }

            var newItem = new Item(key, value, table[bucketIndex]);

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

        for (var i = 0; i < table.Length; i++)
        {
            var item = table[i];

            while (item is not null)
            {
                var hash = GetHashCode(item.Key);
                var bucketIndex = hash & (nextTable.Length - 1);

                nextTable[bucketIndex] =
                    new Item(item.Key, item.Value, nextTable[bucketIndex]);

                item = item.Next;
            }
        }

        // Publish the completely built table atomically.
        Volatile.Write(ref this.table, nextTable);
    }

    /// <summary>
    /// Replaces a value without mutating nodes visible to lock-free readers.
    /// </summary>
    private static Item ReplaceValue(Item head, Item target, TValue value)
    {
        var newHead = new Item(target.Key, value, target.Next);

        // Clone only the prefix that points to the replaced node.
        var item = head;

        while (!ReferenceEquals(item, target))
        {
            newHead = new Item(item.Key, item.Value, newHead);
            item = item.Next!;
        }

        return newHead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCode(ulong key)
    {
        return key.GetHashCode();

        // Fibonacci hashing: a single multiply whose high bits are well mixed.
        // ulong.GetHashCode() only XOR-folds the two halves, which degenerates into
        // pathological collision chains under the power-of-two mask for strided or
        // aligned keys (multiples of 4096, pointer-like values, and so on); this
        // costs one multiplication and is robust for such patterns.
        // return (int)((key * 0x9E3779B97F4A7C15UL) >> 32);
    }
}
