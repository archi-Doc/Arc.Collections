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
/// Represents a thread-safe collection of int/value pairs.<br/>
/// Writes are serialized, while lookups are lock-free.<br/>
/// Optimized for collections that are built infrequently and read frequently.
/// </summary>
/// <typeparam name="TValue">The type of value.</typeparam>
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

    public int Count => Volatile.Read(ref this.count);

    public Int32Hashtable(int capacity = 4)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        var size = HashtableHelper.CalculateCapacity(capacity);
        this.table = new Item?[size];
    }

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

    public bool TryAdd(int key, TValue value)
        => this.AddInternal(key, value, false, out _);

    public void Add(int key, TValue value)
        => this.AddInternal(key, value, true, out _);

    /// <remarks><paramref name="valueFactory"/> is invoked while holding the internal lock;
    /// it must not call back into this hashtable.</remarks>
    public TValue GetOrAdd(int key, Func<int, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        if (this.TryGetValue(key, out var value))
        {
            return value;
        }

        return this.GetOrAddSlow(key, valueFactory);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(int key)
        => this.TryGetValue(key, out _);

    public bool TryRemove(int key)
        => this.TryRemove(key, out _);

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

    private bool AddInternal(int key, TValue value, bool updateValue, out TValue resultingValue)
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
                bucketIndex = key & (table.Length - 1);
                head = table[bucketIndex];
            }

            Volatile.Write(ref table[bucketIndex], new Item(key, value, head));
            Volatile.Write(ref this.count, this.count + 1);

            resultingValue = value;
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

        for (var i = 0; i < table.Length; i++)
        {
            var item = table[i];

            while (item is not null)
            {
                var bucketIndex = item.Key & (nextTable.Length - 1);
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
}
