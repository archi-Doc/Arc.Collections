// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Arc.Collections;

/// <summary>
/// A fast and thread-safe pool of objects (uses <see cref="CircularQueue{T}"/>).<br/>
/// Target: Classes that will be used/reused frequently but are not large enough to use <see cref="ArrayPool{T}"/>.<br/>
/// <br/>
/// If <typeparamref name="T"/> implements <see cref="IDisposable"/>, <see cref="ObjectPool{T}"/> calls <see cref="IDisposable.Dispose"/> when the instance is no longer needed.<br/>
/// This class can also be disposed, although this is not always necessary.
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
public class ObjectPool<T> : IDisposable
    where T : class
{
    public const int DefaultPoolSize = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.<br/>
    /// </summary>
    /// <param name="createFunc">Delegate to create a new instance.</param>
    /// <param name="poolSize">The maximum number of objects in the pool.</param>
    public ObjectPool(Func<T> createFunc, int poolSize = DefaultPoolSize)
    {
        this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {// T is disposable.
            this.isDisposable = true;
        }

        this.queue = new(poolSize);
        this.fastItem = default;
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets the maximum number of objects in the pool.
    /// </summary>
    public int PoolSize
        => this.queue.Capacity;

    private readonly Func<T> createFunc;
    private CircularQueue<T> queue;
    private T? fastItem;
    private bool isDisposable = false;

    #endregion

    /// <summary>
    /// Gets an instance from the pool or create a new instance if not available.<br/>
    /// The instance is guaranteed to be unique even if multiple threads called this method simultaneously.<br/>
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Rent()
    {
        var item = this.fastItem;
        if (item == null || Interlocked.CompareExchange(ref this.fastItem, null, item) != item)
        {
            if (this.queue.TryDequeue(out item))
            {
                return item;
            }

            return this.createFunc();
        }

        return item;

        // return this.objects.TryDequeue(out T? item) ? item : this.createFunc();
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
        if (this.fastItem != null || Interlocked.CompareExchange(ref this.fastItem, instance, null) != null)
        {
            if (!this.queue.TryEnqueue(instance))
            {// The pool is full.
                if (this.isDisposable && instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    #region IDisposable Support

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    ~ObjectPool()
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
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                if (this.isDisposable)
                {// Disposable
                    while (this.queue.TryDequeue(out var item))
                    {
                        if (item is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                else
                {// Non-disposable
                    this.queue = new(this.PoolSize);
                }
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
