// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Arc.Collections;

/// <summary>
/// A fast and thread-safe cache of objects (uses <see cref="UnorderedMap{TKey, TValue}"/> and <see cref="UnorderedLinkedList{T}"/>).<br/>
/// You can cache used objects and retrieve them the next time by specifying the <typeparamref name="TKey"/>.<br/>
/// This is for classes with very high costs, such as encryption.<br/>
/// <br/>
/// If <typeparamref name="TObject"/> implements <see cref="IDisposable"/>, <see cref="KeyedObjectCache{TKey, TObject}"/> calls <see cref="IDisposable.Dispose"/> when the instance is no longer needed.<br/>
/// This class can also be disposed, although this is not always necessary.
/// </summary>
/// <typeparam name="TKey">The type of the key used to retrieve an object from the cache.</typeparam>
/// <typeparam name="TObject">The type of objects contained in the cache.</typeparam>
public sealed class KeyedObjectCache<TKey, TObject> : IDisposable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// A helper interface for acquiring and returning objects.
    /// </summary>
    public readonly struct Interface : IDisposable
    {
        /// <summary>
        /// The cache that the object is returned to.
        /// </summary>
        public readonly KeyedObjectCache<TKey, TObject> ObjectCache;

        /// <summary>
        /// The key associated with the object.
        /// </summary>
        public readonly TKey Key;

        /// <summary>
        /// The object held by this instance, or <see langword="null"/> if it has already been returned.
        /// </summary>
        public readonly TObject? Object;

        internal Interface(KeyedObjectCache<TKey, TObject> objectCache, TKey key, TObject? obj)
        {
            this.ObjectCache = objectCache;
            this.Key = key;
            this.Object = obj;
        }

        /// <summary>
        /// Returns the object to the cache (<see cref="Return"/> and <see cref="Dispose"/> are the same).<br/>
        /// If the object cannot be cached because an object with the same key already exists,
        /// it is disposed when it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <returns>An <see cref="Interface"/> with the object set to its default value.</returns>
        public Interface Return()
        {
            if (this.Object is not null &&
                !this.ObjectCache.Cache(this.Key, this.Object) &&
                this.Object is IDisposable disposable)
            {// Not cached and therefore no longer owned by anyone.
                disposable.Dispose();
            }

            return new(this.ObjectCache, this.Key, default);
        }

        /// <summary>
        /// Returns the object to the cache (<see cref="Return"/> and <see cref="Dispose"/> are the same).
        /// </summary>
        public void Dispose()
            => this.Return();
    }

    private class Item
    {
        public Item(TObject obj)
        {
            this.Object = obj;
            this.MapIndex = -1;
            this.LinkedListNode = null;
        }

#pragma warning disable SA1401 // Fields should be private
        internal TObject Object;
        internal int MapIndex;
        internal UnorderedLinkedList<Item>.Node? LinkedListNode;
#pragma warning restore SA1401 // Fields should be private
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedObjectCache{TKey, TObject}"/> class.<br/>
    /// </summary>
    /// <param name="cacheSize">The maximum number of objects in the cache. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cacheSize"/> is less than 1.</exception>
    public KeyedObjectCache(int cacheSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cacheSize, 1);

        this.CacheSize = cacheSize;
    }

    /// <summary>
    /// Creates an <see cref="Interface"/> that returns <paramref name="obj"/> to this cache when disposed.
    /// </summary>
    /// <param name="key">The key associated with the object.</param>
    /// <param name="obj">The object to be returned to the cache, or <see langword="null"/>.</param>
    /// <returns>An <see cref="Interface"/> instance.</returns>
    public Interface CreateInterface(TKey key, TObject? obj)
        => new(this, key, obj);

    /// <summary>
    /// Gets an instance from the cache by specifying the key.
    /// </summary>
    /// <param name="key">The key used to retrieve an object from the cache.</param>
    /// <returns>An instance of type <typeparamref name="TObject"/>.</returns>
    public TObject? TryGet(TKey key)
    {
        Item? item;
        using (this.lockObject.EnterScope())
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

    /// <summary>
    /// Add an instance to the cache by specifying the key.
    /// </summary>
    /// <param name="key">The key of the object to cache.</param>
    /// <param name="obj">The object to cache.</param>
    /// <returns><see langword="true"/>; The object is successfully cached.<br/>
    /// <see langword="false"/>; An object with the same key already exists.</returns>
    public bool Cache(TKey key, TObject obj)
    {
        using (this.lockObject.EnterScope())
        {
            while (this.linkedList.Count >= this.CacheSize)
            {
                if (this.linkedList.First is not { } first)
                {// Nothing left to evict.
                    break;
                }

                this.DisposeItem(first.Value);
            }

            if (!this.map.ContainsKey(key))
            {
                var item = new Item(obj);
                (item.MapIndex, _) = this.map.Add(key, item);
                item.LinkedListNode = this.linkedList.AddLast(item);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the maximum number of objects that the cache can hold.
    /// </summary>
    public int CacheSize { get; }

    /// <summary>
    /// Gets the number of objects currently held in the cache.
    /// </summary>
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

    private Lock lockObject = new();
    private UnorderedMap<TKey, Item> map = new();
    private UnorderedLinkedList<Item> linkedList = new();

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Disposes all cached objects that implement <see cref="IDisposable"/> and empties the cache.
    /// </summary>
    /// <remarks>The cache holds no unmanaged resources, so calling this is optional.</remarks>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        using (this.lockObject.EnterScope())
        {
            while (this.linkedList.First is { } first)
            {
                this.DisposeItem(first.Value);
            }
        }
    }
    #endregion
}
