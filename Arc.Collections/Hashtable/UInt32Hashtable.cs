// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable SA1204
#pragma warning disable SA1401
#pragma warning disable SA1604 // Element documentation should have summary
#pragma warning disable SA1611
#pragma warning disable SA1615
#pragma warning disable SA1642

namespace Arc.Collections;

/// <summary>
/// Represents a thread-safe collection of uint/value pairs.<br/>
/// Writes are serialized, while lookups are lock-free.<br/>
/// Optimized for collections that are built infrequently and read frequently.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
public class UInt32Hashtable<TValue>
{
    private const int MaximumCapacity = 1 << 30;

    private sealed class Item
    {
        internal readonly uint Key;
        internal readonly TValue Value;
        internal readonly Item? Next;

        internal Item(uint key, TValue value, Item? next)
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
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => Volatile.Read(ref this.count);

    /// <summary>
    /// Initializes a new instance of the <see cref="UInt32Hashtable{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public UInt32Hashtable(int capacity = 4)
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
    /// <returns>A new array with the values currently in the hashtable.</returns>
    public TValue[] ToArray()
    {
        using (this.lockObject.EnterScope())
        {
            var values = new TValue[this.count];
            var table = this.table;
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
    /// Attempts to add a key-value pair.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><see langword="true"/> if the pair was added;
    /// otherwise, <see langword="false"/> if the key already exists.</returns>
    public bool TryAdd(uint key, TValue value)
        => this.AddInternal(key, value, false, out _);

    /// <summary>
    /// Adds or updates a key-value pair.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to store.</param>
    public void Add(uint key, TValue value)
        => this.AddInternal(key, value, true, out _);

    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The factory invoked to create the value when the key is absent.</param>
    /// <returns>The existing value, or the newly created value.</returns>
    /// <remarks><paramref name="valueFactory"/> is invoked while holding the internal lock;
    /// it must not call back into this hashtable.</remarks>
    public TValue GetOrAdd(uint key, Func<uint, TValue> valueFactory)
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
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, the value associated with <paramref name="key"/>; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(uint key, [MaybeNullWhen(false)] out TValue value)
    {
        var table = Volatile.Read(ref this.table);
        var item = Volatile.Read(ref table[GetHashCode(key) & (table.Length - 1)]);

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
    /// <param name="key">The key to locate.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(uint key)
        => this.TryGetValue(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(uint key)
        => this.TryRemove(key, out _);

    /// <summary>
    /// Attempts to remove the value with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="value">When this method returns, the removed value; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found and removed.</returns>
    public bool TryRemove(uint key, [MaybeNullWhen(false)] out TValue value)
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

                var newHead = item.Next;
                for (var p = head; !ReferenceEquals(p, item); p = p.Next!)
                {
                    newHead = new Item(p!.Key, p.Value, newHead);
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

            Volatile.Write(ref this.table, new Item?[this.table.Length]);
            Volatile.Write(ref this.count, 0);
        }
    }

    private bool AddInternal(uint key, TValue value, bool updateValue, out TValue resultingValue)
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

                Volatile.Write(ref table[bucketIndex], ReplaceValue(head!, item, value));
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

            Volatile.Write(ref table[bucketIndex], new Item(key, value, head));
            Volatile.Write(ref this.count, this.count + 1);

            resultingValue = value;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue GetOrAddSlow(uint key, Func<uint, TValue> valueFactory)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var hash = GetHashCode(key);
            var bucketIndex = hash & (table.Length - 1);

            for (var item = table[bucketIndex]; item is not null; item = item.Next)
            {
                if (item.Key == key)
                {
                    return item.Value;
                }
            }

            var value = valueFactory(key);

            if (this.count >= (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = hash & (table.Length - 1);
            }

            Volatile.Write(ref table[bucketIndex], new Item(key, value, table[bucketIndex]));
            Volatile.Write(ref this.count, this.count + 1);

            return value;
        }
    }

    private void RebuildTable()
    {
        var table = this.table;

        if (table.Length >= MaximumCapacity)
        {
            throw new InvalidOperationException("The maximum capacity of the hashtable has been reached.");
        }

        var nextTable = new Item?[table.Length << 1];

        for (var i = 0; i < table.Length; i++)
        {
            var item = table[i];

            while (item is not null)
            {
                var bucketIndex = GetHashCode(item.Key) & (nextTable.Length - 1);
                nextTable[bucketIndex] = new Item(item.Key, item.Value, nextTable[bucketIndex]);
                item = item.Next;
            }
        }

        Volatile.Write(ref this.table, nextTable);
    }

    private static Item ReplaceValue(Item head, Item target, TValue value)
    {
        var newHead = new Item(target.Key, value, target.Next);
        var item = head;

        while (!ReferenceEquals(item, target))
        {
            newHead = new Item(item.Key, item.Value, newHead);
            item = item.Next!;
        }

        return newHead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetHashCode(uint key) => unchecked((int)key);
}
