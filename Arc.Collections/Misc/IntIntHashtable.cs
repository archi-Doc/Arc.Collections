// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#pragma warning disable SA1401

namespace Arc.Collections;

public class IntIntHashtable
{ // HashTable for int/int
    public IntIntHashtable(uint capacity = CollectionHelper.MinimumCapacity)
    {
        var size = CollectionHelper.CalculatePowerOfTwoCapacity(capacity);
        this.table = new Item[size];
    }

    public bool TryAdd(int key, int value)
    {
        bool successAdd;

        if ((this.count * 2) > this.table.Length)
        {// rehash
            this.RebuildTable();
        }

        // add entry(insert last is thread safe for read)
        successAdd = this.AddKeyValue(key, value);

        if (successAdd)
        {
            this.count++;
        }

        return successAdd;
    }

    public bool TryGetValue(uint key, [MaybeNullWhen(false)] out int value)
    {
        var table = this.table;
        var hash = unchecked((int)key);
        var item = table[hash & (table.Length - 1)];

        while (item != null)
        {
            if (key == item.Key)
            { // Identical. alternative: (key == item.Key).
                value = item.Value;
                return true;
            }

            item = item.Next;
        }

        value = default;
        return false;
    }

    public void Clear()
    {
        Array.Clear(this.table);
    }

    private void RebuildTable()
    {
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

        // replace field(threadsafe for read)
        Volatile.Write(ref this.table, nextTable);
    }

    private bool AddItem(Item[] table, Item item)
    {
        var h = item.Hash & (table.Length - 1);

        if (table[h] == null)
        {
            Volatile.Write(ref table[h], item);
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

            Volatile.Write(ref i.Next, item);
        }

        return true;
    }

    private bool AddKeyValue(int key, int value)
    {
        var table = this.table;
        var hash = unchecked((int)key);
        var h = hash & (table.Length - 1);

        if (table[h] == null)
        {
            var item = new Item(key, value, hash);
            Volatile.Write(ref table[h], item);
        }
        else
        {
            var i = table[h]!;
            while (true)
            {
                if (key == i.Key)
                {// Identical
                    return false;
                }

                if (i.Next == null)
                { // Last item.
                    break;
                }

                i = i.Next;
            }

            var item = new Item(key, value, hash);
            Volatile.Write(ref i.Next, item);
        }

        return true;
    }

    private Item?[] table;
    private int count;

    private class Item
    {
        public int Key;
        public int Value;
        public int Hash;
        public Item? Next;

        public Item(int key, int value, int hash)
        {
            this.Key = key;
            this.Value = value;
            this.Hash = hash;
        }
    }
}
