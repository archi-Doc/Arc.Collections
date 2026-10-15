// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Arc.Collections;

/// <summary>
/// Caches reusable objects by key and transfers ownership when they are retrieved.
/// </summary>
/// <typeparam name="TKey">The type of the key used to retrieve an object from the cache.</typeparam>
/// <typeparam name="TObject">The type of objects contained in the cache.</typeparam>
/// <remarks>
/// Stores at most one object per key and evicts the oldest cached object when full.
/// Cache operations are thread-safe; callers manage objects they retrieve.
/// Cached objects implementing <see cref="IDisposable" /> are disposed on eviction or cache disposal,
/// synchronously under the cache lock: their <see cref="IDisposable.Dispose" /> may reenter the cache,
/// but must not wait for another thread that uses it.
/// An object rejected by <see cref="Lease.Return"/> is disposed after the lock is released.
/// </remarks>
public sealed class KeyedObjectCache<TKey, TObject> : IDisposable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Returns an object to its cache when disposed.
    /// </summary>
    /// <remarks>
    /// Return or dispose a lease once. This readonly struct retains its fields after returning;
    /// copies refer to the same object and must not be returned independently.
    /// A default lease and the empty lease returned by <see cref="Return"/> are safe to dispose.
    /// </remarks>
    public readonly struct Lease : IDisposable
    {
        /// <summary>
        /// The cache that the object is returned to.
        /// </summary>
        public readonly KeyedObjectCache<TKey, TObject> Cache;

        /// <summary>
        /// The key associated with the object.
        /// </summary>
        public readonly TKey Key;

        /// <summary>
        /// The object held by this lease. Returning the lease does not change this field.
        /// </summary>
        public readonly TObject? Object;

        private readonly bool hasObject;

        internal Lease(KeyedObjectCache<TKey, TObject> objectCache, TKey key, TObject? obj, bool hasObject = true)
        {
            this.Cache = objectCache;
            this.Key = key;
            this.Object = obj;
            this.hasObject = hasObject && obj is not null;
        }

        /// <summary>
        /// Returns the object to the cache (<see cref="Return"/> and <see cref="Dispose"/> are the same).<br/>
        /// If the object cannot be cached,
        /// it is disposed when it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <returns>An empty <see cref="Lease"/> for the same cache and key; disposing it does nothing.</returns>
        public Lease Return()
        {
            if (this.hasObject &&
                !this.Cache.TryAdd(this.Key, this.Object!) &&
                this.Object is IDisposable disposable)
            {// Not cached and therefore no longer owned by anyone.
                disposable.Dispose();
            }

            return new(this.Cache, this.Key, default, false);
        }

        /// <summary>
        /// Returns the object to the cache (<see cref="Return"/> and <see cref="Dispose"/> are the same).
        /// </summary>
        public void Dispose()
            => this.Return();
    }

    private struct Entry
    {
        public Entry(TObject obj, int previous)
        {
            this.Object = obj;
            this.Previous = previous;
            this.Next = -1;
        }

#pragma warning disable SA1401 // Fields should be private
        internal TObject Object;
        internal int Previous; // The node index of the next older entry, or -1.
        internal int Next; // The node index of the next newer entry, or -1.
#pragma warning restore SA1401 // Fields should be private
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedObjectCache{TKey, TObject}"/> class.
    /// </summary>
    /// <param name="cacheSize">The maximum number of objects in the cache. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cacheSize"/> is less than 1.</exception>
    public KeyedObjectCache(int cacheSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cacheSize, 1);

        this.CacheSize = cacheSize;
    }

    /// <summary>
    /// Creates a <see cref="Lease"/> that returns <paramref name="obj"/> to this cache when disposed.
    /// </summary>
    /// <param name="key">The key associated with the object.</param>
    /// <param name="obj">The object to be returned to the cache, or <see langword="null"/>.</param>
    /// <returns>A <see cref="Lease"/> instance; it is empty when <paramref name="obj"/> is <see langword="null"/>.</returns>
    public Lease CreateLease(TKey key, TObject? obj)
        => new(this, key, obj);

    /// <summary>
    /// Removes an object from the cache and transfers ownership to the caller.
    /// </summary>
    /// <param name="key">The key used to retrieve an object from the cache.</param>
    /// <returns>The cached object, or the default value if the key is absent.</returns>
    public TObject? TakeOrDefault(TKey key)
    {
        using (this.lockObject.EnterScope())
        {
            var index = this.map.FindFirstNode(key);
            if (index >= 0)
            {// Remove
                return this.RemoveEntry(index);
            }
            else
            {
                return default;
            }
        }
    }

    /// <summary>
    /// Transfers ownership of an object to the cache, evicting the oldest cached object if full.
    /// </summary>
    /// <param name="key">The key of the object to cache.</param>
    /// <param name="obj">The object to cache.</param>
    /// <returns><see langword="true"/> if the object was cached.<br/>
    /// <see langword="false"/> if the key is already cached, the cache is disposed, or disposing the evicted
    /// object disposed or refilled the cache. The caller retains ownership in that case.</returns>
    /// <remarks>An evicted object is disposed synchronously while the cache lock is held.</remarks>
    public bool TryAdd(TKey key, TObject obj)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.disposed)
            {
                return false;
            }

            if (this.map.Count >= this.CacheSize)
            {// Evict the oldest object, unless the key is already cached.
                if (this.map.ContainsKey(key))
                {
                    return false;
                }

                DisposeObject(this.RemoveEntry(this.oldest));

                // Disposal can reenter this cache and change its state.
                if (this.disposed || this.map.Count >= this.CacheSize)
                {
                    return false;
                }
            }

            // Add() rejects an existing key, so the key is hashed only once when the cache is not full.
            var (index, newlyAdded) = this.map.Add(key, new(obj, this.newest));
            if (!newlyAdded)
            {
                return false;
            }

            if (this.newest >= 0)
            {
                var last = this.GetEntry(this.newest);
                last.Next = index;
                this.map.UnsafeChangeValue(this.newest, last);
            }
            else
            {
                this.oldest = index;
            }

            this.newest = index;
            return true;
        }
    }

    /// <summary>
    /// Gets the maximum number of objects that the cache can hold.
    /// </summary>
    public int CacheSize { get; }

    /// <summary>
    /// Gets the number of objects currently held in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            using (this.lockObject.EnterScope())
            {
                return this.map.Count;
            }
        }
    }

    private static void DisposeObject(TObject obj)
    {
        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entry GetEntry(int index)
        => this.map.UnsafeGetNodes().Nodes[index].Value;

    /// <summary>
    /// Unlinks and removes an entry before its object is handed out or disposed,
    /// so that reentrant calls observe a consistent cache.
    /// </summary>
    private TObject RemoveEntry(int index)
    {
        var entry = this.GetEntry(index);
        if (entry.Previous >= 0)
        {
            var previous = this.GetEntry(entry.Previous);
            previous.Next = entry.Next;
            this.map.UnsafeChangeValue(entry.Previous, previous);
        }
        else
        {
            this.oldest = entry.Next;
        }

        if (entry.Next >= 0)
        {
            var next = this.GetEntry(entry.Next);
            next.Previous = entry.Previous;
            this.map.UnsafeChangeValue(entry.Next, next);
        }
        else
        {
            this.newest = entry.Previous;
        }

        this.map.RemoveNode(index);
        return entry.Object;
    }

    private readonly Lock lockObject = new();

    // Entries form a doubly linked list from oldest to newest through their stable node indexes,
    // so caching an object does not allocate once the map has grown.
    private readonly UnorderedMap<TKey, Entry> map = new();
    private int oldest = -1; // The node index of the oldest entry, or -1 if the cache is empty.
    private int newest = -1; // The node index of the newest entry, or -1 if the cache is empty.

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Disposes all cached objects that implement <see cref="IDisposable"/> and empties the cache.
    /// </summary>
    /// <remarks>Call this to release disposable objects still owned by the cache.
    /// Later <see cref="TryAdd"/> calls fail, so leases returned afterward dispose their objects.</remarks>
    public void Dispose()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            while (this.oldest >= 0)
            {
                DisposeObject(this.RemoveEntry(this.oldest));
            }
        }
    }
    #endregion
}
