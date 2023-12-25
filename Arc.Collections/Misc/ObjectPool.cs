// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Arc.Collections;

/// <summary>
/// A fast and thread-safe pool of objects (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// Target: Classes that will be used/reused frequently but are not large enough to use <see cref="ArrayPool{T}"/>.<br/>
/// <br/>
/// If <typeparamref name="T"/> implements <see cref="IDisposable"/>, <see cref="ObjectPool{T}"/> calls <see cref="IDisposable.Dispose"/> when the instance is no longer needed.<br/>
/// This class can also be disposed, although this is not always necessary.
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
public class ObjectPool<T> : IDisposable
    where T : class
{
    public const uint MinimumPoolSize = 4;
    public const uint DefaultPoolSize = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.<br/>
    /// Set <paramref name="prepareInstances"/> to true to pre-create instances with high initialization cost (another thread).
    /// </summary>
    /// <param name="createFunc">Delegate to create a new instance.</param>
    /// <param name="poolSize">The maximum number of objects in the pool.</param>
    /// <param name="prepareInstances"><see langword="true"/>: Pre-create instances in another thread.</param>
    public ObjectPool(Func<T> createFunc, uint poolSize = DefaultPoolSize, bool prepareInstances = false)
    {
        this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        if (poolSize < MinimumPoolSize)
        {
            poolSize = MinimumPoolSize;
        }

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {// T is disposable.
            this.isDisposable = true;
        }

        this.PoolSize = poolSize;
        this.objects = new ConcurrentQueue<T>();
        this.fastItem = default;
        this.numberOfObjects = 0;
        this.prepareInstances = prepareInstances;
        if (prepareInstances)
        {
            this.prepareThreshold = this.PoolSize / 4;
            this.prepareThreshold = this.prepareThreshold == 0 ? 1 : this.prepareThreshold;
            this.PrepareInstanceInternal();
        }
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets the maximum number of objects in the pool.
    /// </summary>
    public uint PoolSize { get; }

    private readonly Func<T> createFunc;
    private readonly ConcurrentQueue<T> objects;
    private readonly bool prepareInstances;
    private T? fastItem;
    private uint numberOfObjects;
    private uint prepareCount;
    private uint prepareThreshold;
    private bool isTaskRunning = false;
    private bool isDisposable = false;

    #endregion

    /// <summary>
    /// Gets an instance from the pool or create a new instance if not available.<br/>
    /// The instance is guaranteed to be unique even if multiple threads called this method simultaneously.<br/>
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get()
    {
        if (this.prepareInstances)
        {
            if (this.prepareCount++ >= this.prepareThreshold)
            {
                this.prepareCount = 0;
                if (this.numberOfObjects <= (this.PoolSize >> 1))
                {
                    this.PrepareInstanceInternal();
                }
            }
        }

        var item = this.fastItem;
        if (item == null || Interlocked.CompareExchange(ref this.fastItem, null, item) != item)
        {
            if (this.objects.TryDequeue(out item))
            {
                Interlocked.Decrement(ref this.numberOfObjects);
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
            if (this.numberOfObjects < this.PoolSize)
            {
                Interlocked.Increment(ref this.numberOfObjects);
                this.objects.Enqueue(instance);
            }
            else
            {// The pool is full.
                if (this.isDisposable && instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /*if (this.objects.Count < this.PoolSize)
        {
            this.objects.Enqueue(instance);
        }
        else if (this.isDisposable && instance is IDisposable disposable)
        {// The queue is full.
            disposable.Dispose();
        }*/
    }

    private Task PrepareInstanceInternal()
    {
        if (this.isTaskRunning)
        {
            return Task.CompletedTask;
        }

        this.isTaskRunning = true;
        return Task.Run(() =>
        {
            try
            {
                while (this.numberOfObjects < this.PoolSize)
                {
                    Interlocked.Increment(ref this.numberOfObjects);
                    this.objects.Enqueue(this.createFunc());
                }
            }
            finally
            {
                this.isTaskRunning = false;
            }
        });
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
                    while (this.objects.TryDequeue(out var item))
                    {
                        if (item is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                else
                {// Non-disposable
                    this.objects.Clear();
                }
            }

            // free native resources here if there are any.
            this.numberOfObjects = 0;
            this.disposed = true;
        }
    }
    #endregion
}
