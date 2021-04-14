﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

namespace Arc.Collection
{
    /// <summary>
    /// Represents a collection of objects. <see cref="UnorderedMultiMap{TKey, TValue}"/> uses a hash table structure to store objects.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    public class UnorderedMultiMap<TKey, TValue> : UnorderedMap<TKey, TValue>
    {
        public UnorderedMultiMap()
            : base()
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(int capacity)
            : base(capacity)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(int capacity, IEqualityComparer<TKey>? comparer)
            : base(capacity, comparer)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IDictionary<TKey, TValue> dictionary)
            : base(dictionary)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
            : base(dictionary, comparer)
        {
            this.AllowMultiple = true;
        }
    }
}