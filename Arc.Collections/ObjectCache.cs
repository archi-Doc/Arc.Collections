// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

public sealed class ObjectCache<TKey, TObject> : IDisposable
    where TKey : IEquatable<TKey>
{
    private class Item
    {
        /*public Item()
        {
            this.Key = default!;
            this.Object = default!;
            this.MapIndex = -1;
            this.LinkedListNode = null;
        }*/

        public Item(TKey key, TObject obj)
        {
            this.Key = key;
            this.Object = obj;
            this.MapIndex = -1;
            this.LinkedListNode = null;
        }

#pragma warning disable SA1401 // Fields should be private
        internal TKey Key;
        internal TObject Object;
        internal int MapIndex;
        internal UnorderedLinkedList<Item>.Node? LinkedListNode;
#pragma warning restore SA1401 // Fields should be private
    }

    public ObjectCache(int cacheSize)
    {
        this.CacheSize = cacheSize;
    }

    public TObject? TryGet(TKey key)
    {
        Item? item;
        lock (this.syncObject)
        {
            if (this.map.TryGetValue(key, out item))
            {// Remove
                this.RemoveFromCollection(item);
                return item.Object;
            }
            else
            {
                return default;
            }
        }
    }

    public bool Cache(TKey key, TObject obj)
    {
        lock (this.syncObject)
        {
            while (this.linkedList.Count >= this.CacheSize)
            {
                if (this.linkedList.First is { } first)
                {
                    this.DisposeItem(first.Value);
                }
            }

            if (!this.map.ContainsKey(key))
            {
                var item = new Item(key, obj);
                (item.MapIndex, _) = this.map.Add(key, item);
                item.LinkedListNode = this.linkedList.AddLast(item);
                return true;
            }
        }

        return false;
    }

    public int CacheSize { get; }

    public int Count => this.linkedList.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFromCollection(Item item)
    {
        if (item.MapIndex >= 0)
        {
            this.map.RemoveNode(item.MapIndex);
            item.MapIndex = -1;
        }

        if (item.LinkedListNode != null)
        {
            this.linkedList.Remove(item.LinkedListNode);
            item.LinkedListNode = null;
        }
    }

    private void DisposeItem(Item item)
    {
        this.RemoveFromCollection(item);
        if (item.Object is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private object syncObject = new();
    private UnorderedMap<TKey, Item> map = new();
    private UnorderedLinkedList<Item> linkedList = new();

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectCache{TObject, TKey}"/> class.
    /// </summary>
    ~ObjectCache()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                lock (this.syncObject)
                {
                    while (this.linkedList.First is { } first)
                    {
                        this.DisposeItem(first.Value);
                    }
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
