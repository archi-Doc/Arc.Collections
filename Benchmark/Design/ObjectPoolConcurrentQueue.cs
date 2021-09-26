// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
/// A fast and thread-safe pool of objects.<br/>
/// </summary>
/// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
public class ObjectPoolConcurrentQueue<T> : IDisposable
{
    public const int DefaultLimit = 32;

    private readonly Func<T> objectGenerator;
    private readonly ConcurrentQueue<T> objects;
    private bool isDisposable = false;

    public ObjectPoolConcurrentQueue(Func<T> objectGenerator, int limit = DefaultLimit)
    {
        this.objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            this.isDisposable = true;
        }

        this.Limit = limit;
        this.objects = new ConcurrentQueue<T>();
    }

    /// <summary>
    /// Gets the maximum number of objects in the pool.
    /// </summary>
    public int Limit { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get() => this.objects.TryDequeue(out T? item) ? item : this.objectGenerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        if (this.objects.Count < this.Limit)
        {
            this.objects.Enqueue(item);
        }
        else if (this.isDisposable && item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="ObjectPoolConcurrentQueue{T}"/> class.
    /// </summary>
    ~ObjectPoolConcurrentQueue()
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
