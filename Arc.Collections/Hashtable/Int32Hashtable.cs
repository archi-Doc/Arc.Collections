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
/// Provides a thread-safe hash table with <see langword="int" /> keys.
/// </summary>
/// <typeparam name="TValue">The type of values in the hashtable.</typeparam>
/// <remarks>
/// Writes are serialized by an internal lock. Lookups are lock-free and see each key either before or after a write;
/// <see cref="Count"/> is updated separately and may briefly lag behind lookups.<br/>
/// <see cref="TryAdd"/> preserves existing values; <see cref="AddOrUpdate"/> replaces them.<br/>
/// The key is used as its own hash code, so keys that share their low-order bits
/// (for example, multiples of a large power of two) fall into the same bucket.<br/>
/// At most 2^29 (536,870,912) items can be stored; adding another key throws <see cref="InvalidOperationException"/>.
/// </remarks>
public class Int32Hashtable<TValue>
{
    private const int MaximumCapacity = 1 << 30;

    private sealed class Item
    {
        internal readonly int Key;
        internal readonly TValue Value;
        internal readonly Item? Next;

        internal Item(int key, TValue value, Item? next)
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
    /// Initializes a new instance of the <see cref="Int32Hashtable{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The minimum number of items the hashtable can hold before it grows.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than 2^29 (536,870,912).</exception>
    public Int32Hashtable(int capacity = 4)
    {
        this.table = new Item?[HashtableHelper.CalculateCapacity(capacity)];
    }

    /// <summary>
    /// Gets an array containing all values, in no particular order.
    /// </summary>
    /// <returns>A snapshot of the current values, or an empty array when the hashtable is empty.</returns>
    /// <remarks>The snapshot is taken under the write lock.</remarks>
    public TValue[] ToArray()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.count == 0)
            {
                return Array.Empty<TValue>();
            }

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
    /// Attempts to add a key-value pair. An existing value is left unchanged.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><see langword="true"/> if the pair was added;
    /// otherwise, <see langword="false"/> if the key already exists.</returns>
    /// <exception cref="InvalidOperationException">The key is absent and the hashtable already holds the maximum number of items.</exception>
    public bool TryAdd(int key, TValue value)
        => this.AddInternal(key, value, false);

    /// <summary>
    /// Adds a key-value pair, or replaces the value if the key already exists.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="InvalidOperationException">The key is absent and the hashtable already holds the maximum number of items.</exception>
    public void AddOrUpdate(int key, TValue value)
        => this.AddInternal(key, value, true);

    /// <summary>
    /// Gets the value associated with the specified key, or adds a value created by <paramref name="valueFactory"/> if the key is absent.
    /// </summary>
    /// <param name="key">The key to locate or add.</param>
    /// <param name="valueFactory">The function that creates the value for <paramref name="key"/> when the key is absent.</param>
    /// <returns>The existing value, or the newly added value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="valueFactory"/> modified this hashtable in a way that invalidates the insertion,
    /// or the hashtable already holds the maximum number of items.</exception>
    /// <remarks><paramref name="valueFactory"/> runs under the write lock, at most once per call and only while the key is absent;
    /// other writers wait until it returns. It must not modify this hashtable. A modification that would invalidate the insertion
    /// (to the same bucket, a resize, or <see cref="Clear"/>) throws <see cref="InvalidOperationException"/>.
    /// If <paramref name="valueFactory"/> throws or the insertion fails, <paramref name="key"/> is not added and any created value is discarded.</remarks>
    public TValue GetOrAdd(int key, Func<int, TValue> valueFactory)
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
    /// <param name="value">When this method returns, the value associated with <paramref name="key"/> if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(int key, [MaybeNullWhen(false)] out TValue value)
    {
        var table = Volatile.Read(ref this.table);
        var item = Volatile.Read(ref table[key & (table.Length - 1)]);

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
    public bool ContainsKey(int key)
        => this.TryGetValue(key, out _);

    /// <summary>
    /// Attempts to remove the item with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if the key was found and removed; otherwise, <see langword="false"/>.</returns>
    public bool TryRemove(int key)
        => this.TryRemove(key, out _);

    /// <summary>
    /// Attempts to remove the item with the specified key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="value">When this method returns, the removed value if the key was found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found and removed; otherwise, <see langword="false"/>.</returns>
    public bool TryRemove(int key, [MaybeNullWhen(false)] out TValue value)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = key & (table.Length - 1);
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
    /// Removes all key-value pairs. The capacity is not reduced.
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

    private bool AddInternal(int key, TValue value, bool updateValue)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = key & (table.Length - 1);
            var head = table[bucketIndex];

            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Key != key)
                {
                    continue;
                }

                if (!updateValue)
                {
                    return false;
                }

                Volatile.Write(ref table[bucketIndex], ReplaceValue(head!, item, value));
                return false;
            }

            if (this.count >= (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = key & (table.Length - 1);
                head = table[bucketIndex];
            }

            Volatile.Write(ref table[bucketIndex], new Item(key, value, head));
            Volatile.Write(ref this.count, this.count + 1);

            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue GetOrAddSlow(int key, Func<int, TValue> valueFactory)
    {
        using (this.lockObject.EnterScope())
        {
            var table = this.table;
            var bucketIndex = key & (table.Length - 1);

            var head = table[bucketIndex];
            for (var item = head; item is not null; item = item.Next)
            {
                if (item.Key == key)
                {
                    return item.Value;
                }
            }

            var value = valueFactory(key);

            if (!ReferenceEquals(table, this.table) || !ReferenceEquals(head, table[bucketIndex]))
            {// The lock is reentrant, so a factory calling back into this hashtable is not blocked.
                HashtableHelper.ThrowFactoryModifiedTable();
            }

            if (this.count >= (table.Length >> 1))
            {
                this.RebuildTable();

                table = this.table;
                bucketIndex = key & (table.Length - 1);
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
        var mask = nextTable.Length - 1;

        for (var i = 0; i < table.Length; i++)
        {
            var head = table[i];
            if (head is null)
            {
                continue;
            }

            // The final run that stays in one bucket can be shared with readers of the old table.
            var item = head;
            var lastRun = item;
            var lastBucketIndex = item.Key & mask;
            for (var next = item.Next; next is not null; next = next.Next)
            {
                var bucketIndex = next.Key & mask;
                if (bucketIndex != lastBucketIndex)
                {
                    lastBucketIndex = bucketIndex;
                    lastRun = next;
                }
            }

            nextTable[lastBucketIndex] = lastRun;

            // Only the prefix whose links change needs new immutable nodes.
            for (; !ReferenceEquals(item, lastRun); item = item.Next!)
            {
                var bucketIndex = item.Key & mask;
                nextTable[bucketIndex] = new Item(item.Key, item.Value, nextTable[bucketIndex]);
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
}
