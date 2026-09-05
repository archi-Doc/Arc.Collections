// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

/// <summary>
/// Provides a thread-safe pool of reusable objects.
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
/// <remarks>
/// Rent and Return are thread-safe; Dispose must not run concurrently with them.
/// Returned objects implementing <see cref="IDisposable" /> are disposed when no slot is available.
/// Disposing the pool disposes its cached objects. Return each rented object once.
/// </remarks>
public sealed class ObjectPool<T> : IDisposable
    where T : class
{
    /// <summary>
    /// The default maximum number of objects kept in the pool.
    /// </summary>
    public const int DefaultPoolSize = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.<br/>
    /// </summary>
    /// <param name="createFunc">A thread-safe factory that creates a distinct instance on each call.</param>
    /// <param name="poolSize">The requested maximum number of objects in the pool.<br/>
    /// Rounded up to a power of two and clamped between 2 and <see cref="CircularQueue{T}.MaximumCapacity"/>.</param>
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
    /// Rents an available instance, or calls the factory when the pool is empty.
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

    /// <summary>
    /// Disposes every pooled object that implements <see cref="IDisposable"/> and empties the pool.
    /// </summary>
    /// <remarks>Must not be called concurrently with <see cref="Rent"/> or <see cref="Return"/>.</remarks>
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
