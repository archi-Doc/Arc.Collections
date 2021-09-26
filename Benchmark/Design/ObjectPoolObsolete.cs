// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

/// <summary>
/// A fast and thread-safe pool of objects (uses <see cref="ConcurrentQueue{T}"/>).<br/>
/// Target: Classes that will be used/reused frequently but are not large enough to use <see cref="ArrayPool{T}"/>.<br/>
/// If <typeparamref name="T"/> implements <see cref="IDisposable"/>, <see cref="ObjectPoolObsolete{T}"/> calls <see cref="IDisposable.Dispose"/> when the instance is no longer needed.<br/>
/// It is not necessary, but you can dispose this class.
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
public class ObjectPoolObsolete<T> : IDisposable
{
    public const uint MinimumPoolSize = 4;
    public const uint DefaultPoolSize = 32;

    private readonly Func<T> objectGenerator;
    private readonly ConcurrentQueue<T> objects;
    private bool isDisposable = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPoolObsolete{T}"/> class.
    /// </summary>
    /// <param name="objectGenerator">Delegate to create a new instance.</param>
    /// <param name="poolSize">The maximum number of objects in the pool.</param>
    public ObjectPoolObsolete(Func<T> objectGenerator, uint poolSize = DefaultPoolSize)
    {
        this.objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        if (poolSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize));
        }
        else if (poolSize < MinimumPoolSize)
        {
            poolSize = MinimumPoolSize;
        }

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            this.isDisposable = true;
        }

        this.PoolSize = poolSize;
        this.objects = new ConcurrentQueue<T>();
    }

    /// <summary>
    /// Gets the maximum number of objects in the pool.
    /// </summary>
    public uint PoolSize { get; }

    /// <summary>
    /// Gets an instance from the pool or create a new instance if not available.<br/>
    /// The instance is guaranteed to be unique even if multiple threads called this method simultaneously.<br/>
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get() => this.objects.TryDequeue(out T? item) ? item : this.objectGenerator();

    /// <summary>
    /// Returns an instance to the pool.<br/>
    /// Forgetting to return is not fatal, but may lead to decreased performance.<br/>
    /// Do not call this method multiple times on the same instance.
    /// </summary>
    /// <param name="instance">The instance to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T instance)
    {
        if (this.objects.Count < this.PoolSize)
        {
            this.objects.Enqueue(instance);
        }
        else if (this.isDisposable && instance is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectPoolObsolete{T}"/> class.
    /// </summary>
    ~ObjectPoolObsolete()
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
            this.disposed = true;
        }
    }
    #endregion
}
