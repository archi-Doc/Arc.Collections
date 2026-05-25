// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

/// <summary>
/// A fast and thread-safe pool of objects (uses <see cref="CircularQueue{T}"/>).<br/>
/// Target: Classes that will be used/reused frequently but are not large enough to use <see cref="ArrayPool{T}"/>.<br/>
/// <br/>
/// If an object implements <see cref="IDisposable"/>, it is disposed when it cannot be stored
/// because the pool is full, or when the pool itself is disposed.
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
/// <remarks>
/// Rent and Return are thread-safe. Dispose must not be called concurrently with Rent or Return.
/// </remarks>
public sealed class ObjectPool<T> : IDisposable
    where T : class
{
    public const int DefaultPoolSize = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.<br/>
    /// </summary>
    /// <param name="createFunc">Delegate to create a new instance.</param>
    /// <param name="poolSize">The requested maximum number of objects in the pool.<br/>
    /// The actual capacity may be rounded up.</param>
    public ObjectPool(Func<T> createFunc, int poolSize = DefaultPoolSize)
    {
        this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        this.queue = new(poolSize);
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets the maximum number of objects in the pool.
    /// </summary>
    public int PoolSize => this.queue.Capacity;

    private readonly Func<T> createFunc;
    private readonly CircularQueue<T> queue;

    #endregion

    /// <summary>
    /// Gets an instance from the pool or create a new instance if not available.<br/>
    /// The instance is guaranteed to be unique even if multiple threads called this method simultaneously.<br/>
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Rent()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        if (this.queue.TryDequeue(out var item))
        {
            return item;
        }

        return this.createFunc();
    }

    /// <summary>
    /// Returns an instance to the pool.<br/>
    /// Forgetting to return is not fatal, but may lead to decreased performance.<br/>
    /// Do not call this method multiple times on the same instance.
    /// </summary>
    /// <param name="instance">The instance to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T instance)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        if (!this.queue.TryEnqueue(instance))
        {// The pool is full.
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    #region IDisposable Support

    private bool disposed = false; // To detect redundant calls.

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        while (this.queue.TryDequeue(out var item))
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    #endregion
}
