// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace Arc.Collections.Obsolete
{
    /// <summary>
    /// A fast and thread-safe pool of objects.<br/>
    /// </summary>
    /// <typeparam name="T">The type of the objects contained in the pool.</typeparam>
    public class LooseObjectPool<T>
        where T : class
    {
        public const int DefaultLimit = 32;

        private readonly Func<T> objectGenerator;
        private Node current = default!;

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
            if (this.current.value != null)
            {
                this.current = this.current.next;
            }

            Volatile.Write(ref this.current.value, instance);

            // Alternative
            /*if (Interlocked.CompareExchange(ref this.current.value, instance, null) == null)
            {// Set instance
                return;
            }
            else
            {
                this.current = this.current.next;
                Volatile.Write(ref this.current.value, instance);
            }*/
        }
    }
}
