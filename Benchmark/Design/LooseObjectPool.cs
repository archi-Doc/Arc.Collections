// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
    public class LooseObjectPool<T> : IDisposable
        where T : class
    {
        public const int DefaultLimit = 32;

        private readonly Func<T> objectGenerator;
        private Node current = default!;
        private bool isDisposable = false;

        internal class Node
        {
            internal Node()
            {
            }

            internal Node previous = default!;
            internal Node next = default!;
            internal T? value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LooseObjectPool{T}"/> class.
        /// </summary>
        /// <param name="objectGenerator">Delegate to create a new instance.</param>
        /// <param name="limit">The maximum number of objects in the pool.</param>
        public LooseObjectPool(Func<T> objectGenerator, int limit = DefaultLimit)
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
            var first = new Node();
            var previous = first;
            for (var i = 1; i < limit; i++)
            {
                var node = new Node();
                previous.next = node;
                node.previous = previous;
                previous = node;
            }

            first.previous = previous;
            previous.next = first;
            this.current = first;
        }

        /// <summary>
        /// Gets the maximum number of objects in the pool.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Gets an instance from the pool or create a new instance if not available.<br/>
        /// The instance is guaranteed to be unique even if multiple threads called this method simultaneously.<br/>
        /// However, since it's loose, a new instance may be created even if the pool is not empty.
        /// </summary>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Rent()
        {
            var instance = Interlocked.Exchange(ref this.current.value, null);
            if (instance == null)
            {
                return this.objectGenerator();
            }
            else
            {
                this.current = this.current.previous;
                return instance;
            }
        }

        /// <summary>
        /// Returns an instance to the pool.<br/>
        /// However, since it's loose, the instance can be lost due to the synchronization problem or the pool is full.<br/>
        /// Forgetting to return is not fatal, but may lead to decreased performance.
        /// </summary>
        /// <param name="instance">The instance to return to the pool.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T instance)
        {
            this.current = this.current.next;
            var original = Interlocked.Exchange(ref this.current.value, instance);
            if (original != null && this.isDisposable && original is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Returns an instance to the pool.<br/>
        /// However, since it's loose, the instance can be lost due to the synchronization problem or the pool is full.<br/>
        /// Forgetting to return is not fatal, but may lead to decreased performance.
        /// </summary>
        /// <param name="instance">The instance to return to the pool.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReturnObsolete(T instance)
        {
            if (this.current.value != null)
            {
                this.current = this.current.next;
            }

            Volatile.Write(ref this.current.value, instance);
        }

#pragma warning disable SA1124 // Do not use regions
        #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

        private bool disposed = false; // To detect redundant calls.

        /// <summary>
        /// Finalizes an instance of the <see cref="LooseObjectPool{T}"/> class.
        /// </summary>
        ~LooseObjectPool()
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
                    {
                        for (var i = 0; i < this.Limit; i++)
                        {
                            var original = Interlocked.Exchange(ref this.current.value, null);
                            if (original != null && original is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }

                            this.current = this.current.next;
                        }
                    }
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
        #endregion
    }
