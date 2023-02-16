// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Arc.Collections.Obsolete;

namespace Arc.Collections.HotMethod
{
    public sealed class StandardMethod<T> : IHotMethod<T>
    {
        public int BinarySearch(T[] array, int index, int length, T value) => Array.BinarySearch<T>(array, index, length, value);

        public (int Cmp, OrderedSetObsolete<T>.Node? Leaf) SearchNode(OrderedSetObsolete<T>.Node? target, T value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<T>.Default.Compare(value, x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class StandardMethod2<TKey, TValue> : IHotMethod2<TKey, TValue>
    {
        public (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMap<TKey, TValue>.Node? target, TKey key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<TKey>.Default.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int Cmp, OrderedMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<TKey, TValue>.Node? target, TKey key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<TKey>.Default.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp > 0)
                {
                    cmp = -1;
                    x = x.Left;
                }
                else if (cmp < 0)
                {
                    cmp = 1;
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<TKey, TValue>.Node? target, TKey key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<TKey>.Default.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int Cmp, OrderedMultiMap<TKey, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<TKey, TValue>.Node? target, TKey key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<TKey>.Default.Compare(key, x.Key); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp > 0)
                {
                    cmp = -1;
                    x = x.Left;
                }
                else if (cmp < 0)
                {
                    cmp = 1;
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        /* public UnorderedMap<TKey, TValue>.Node? SearchHashtable(UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key)
        {
            var ec = EqualityComparer<TKey>.Default;
            var hashCode = key == null ? 0 : ec.GetHashCode(key);
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && ec.Equals(n.Key, key))
                {// Identical
                    return n;
                }

                if (n == hashtable[index])
                {
                    break;
                }
                else
                {
                    n = n.Next;
                }
            }

            return null; // Not found
        }

        public (UnorderedMap<TKey, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<TKey, TValue>.Node?[] hashtable, TKey key)
        {
            var ec = EqualityComparer<TKey>.Default;
            var hashCode = key == null ? 0 : ec.GetHashCode(key);
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && ec.Equals(n.Key, key))
                    {// Identical
                        return (n, hashCode, index);
                    }

                    // Next item
                    if (n == hashtable[index])
                    {
                        break;
                    }
                    else
                    {
                        n = n.Next;
                    }
                }
            }

            return (null, hashCode, index);
        }*/
    }
}
