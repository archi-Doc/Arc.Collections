// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using Arc.Collection.Obsolete;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Collection.HotMethod
{
    public sealed class UInt8Method : IHotMethod<byte>
    {
        public static readonly UInt8Method Instance = new ();

        private UInt8Method()
        {
        }

        public int BinarySearch(byte[] array, int index, int length, byte value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<byte>.Node? leaf) SearchNode(OrderedSetObsolete<byte>.Node? target, byte value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class UInt8Method2<TValue> : IHotMethod2<byte, TValue>
    {
        public (int cmp, OrderedMap<byte, TValue>.Node? leaf) SearchNode(OrderedMap<byte, TValue>.Node? target, byte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<byte, TValue>.Node? leaf) SearchNode(OrderedMultiMap<byte, TValue>.Node? target, byte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<byte, TValue>.Node? SearchHashtable(UnorderedMap<byte, TValue>.Node?[] hashtable, byte key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<byte, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<byte, TValue>.Node?[] hashtable, byte key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class Int8Method : IHotMethod<sbyte>
    {
        public static readonly Int8Method Instance = new ();

        private Int8Method()
        {
        }

        public int BinarySearch(sbyte[] array, int index, int length, sbyte value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<sbyte>.Node? leaf) SearchNode(OrderedSetObsolete<sbyte>.Node? target, sbyte value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class Int8Method2<TValue> : IHotMethod2<sbyte, TValue>
    {
        public (int cmp, OrderedMap<sbyte, TValue>.Node? leaf) SearchNode(OrderedMap<sbyte, TValue>.Node? target, sbyte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<sbyte, TValue>.Node? leaf) SearchNode(OrderedMultiMap<sbyte, TValue>.Node? target, sbyte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<sbyte, TValue>.Node? SearchHashtable(UnorderedMap<sbyte, TValue>.Node?[] hashtable, sbyte key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<sbyte, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<sbyte, TValue>.Node?[] hashtable, sbyte key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class UInt16Method : IHotMethod<ushort>
    {
        public static readonly UInt16Method Instance = new ();

        private UInt16Method()
        {
        }

        public int BinarySearch(ushort[] array, int index, int length, ushort value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<ushort>.Node? leaf) SearchNode(OrderedSetObsolete<ushort>.Node? target, ushort value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class UInt16Method2<TValue> : IHotMethod2<ushort, TValue>
    {
        public (int cmp, OrderedMap<ushort, TValue>.Node? leaf) SearchNode(OrderedMap<ushort, TValue>.Node? target, ushort key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<ushort, TValue>.Node? leaf) SearchNode(OrderedMultiMap<ushort, TValue>.Node? target, ushort key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<ushort, TValue>.Node? SearchHashtable(UnorderedMap<ushort, TValue>.Node?[] hashtable, ushort key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<ushort, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<ushort, TValue>.Node?[] hashtable, ushort key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class Int16Method : IHotMethod<short>
    {
        public static readonly Int16Method Instance = new ();

        private Int16Method()
        {
        }

        public int BinarySearch(short[] array, int index, int length, short value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<short>.Node? leaf) SearchNode(OrderedSetObsolete<short>.Node? target, short value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class Int16Method2<TValue> : IHotMethod2<short, TValue>
    {
        public (int cmp, OrderedMap<short, TValue>.Node? leaf) SearchNode(OrderedMap<short, TValue>.Node? target, short key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<short, TValue>.Node? leaf) SearchNode(OrderedMultiMap<short, TValue>.Node? target, short key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<short, TValue>.Node? SearchHashtable(UnorderedMap<short, TValue>.Node?[] hashtable, short key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<short, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<short, TValue>.Node?[] hashtable, short key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class UInt32Method : IHotMethod<uint>
    {
        public static readonly UInt32Method Instance = new ();

        private UInt32Method()
        {
        }

        public int BinarySearch(uint[] array, int index, int length, uint value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<uint>.Node? leaf) SearchNode(OrderedSetObsolete<uint>.Node? target, uint value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class UInt32Method2<TValue> : IHotMethod2<uint, TValue>
    {
        public (int cmp, OrderedMap<uint, TValue>.Node? leaf) SearchNode(OrderedMap<uint, TValue>.Node? target, uint key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<uint, TValue>.Node? leaf) SearchNode(OrderedMultiMap<uint, TValue>.Node? target, uint key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<uint, TValue>.Node? SearchHashtable(UnorderedMap<uint, TValue>.Node?[] hashtable, uint key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<uint, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<uint, TValue>.Node?[] hashtable, uint key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class Int32Method : IHotMethod<int>
    {
        public static readonly Int32Method Instance = new ();

        private Int32Method()
        {
        }

        public int BinarySearch(int[] array, int index, int length, int value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<int>.Node? leaf) SearchNode(OrderedSetObsolete<int>.Node? target, int value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class Int32Method2<TValue> : IHotMethod2<int, TValue>
    {
        public (int cmp, OrderedMap<int, TValue>.Node? leaf) SearchNode(OrderedMap<int, TValue>.Node? target, int key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<int, TValue>.Node? leaf) SearchNode(OrderedMultiMap<int, TValue>.Node? target, int key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<int, TValue>.Node? SearchHashtable(UnorderedMap<int, TValue>.Node?[] hashtable, int key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<int, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<int, TValue>.Node?[] hashtable, int key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class UInt64Method : IHotMethod<ulong>
    {
        public static readonly UInt64Method Instance = new ();

        private UInt64Method()
        {
        }

        public int BinarySearch(ulong[] array, int index, int length, ulong value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<ulong>.Node? leaf) SearchNode(OrderedSetObsolete<ulong>.Node? target, ulong value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class UInt64Method2<TValue> : IHotMethod2<ulong, TValue>
    {
        public (int cmp, OrderedMap<ulong, TValue>.Node? leaf) SearchNode(OrderedMap<ulong, TValue>.Node? target, ulong key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<ulong, TValue>.Node? leaf) SearchNode(OrderedMultiMap<ulong, TValue>.Node? target, ulong key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<ulong, TValue>.Node? SearchHashtable(UnorderedMap<ulong, TValue>.Node?[] hashtable, ulong key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<ulong, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<ulong, TValue>.Node?[] hashtable, ulong key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class Int64Method : IHotMethod<long>
    {
        public static readonly Int64Method Instance = new ();

        private Int64Method()
        {
        }

        public int BinarySearch(long[] array, int index, int length, long value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<long>.Node? leaf) SearchNode(OrderedSetObsolete<long>.Node? target, long value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class Int64Method2<TValue> : IHotMethod2<long, TValue>
    {
        public (int cmp, OrderedMap<long, TValue>.Node? leaf) SearchNode(OrderedMap<long, TValue>.Node? target, long key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<long, TValue>.Node? leaf) SearchNode(OrderedMultiMap<long, TValue>.Node? target, long key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<long, TValue>.Node? SearchHashtable(UnorderedMap<long, TValue>.Node?[] hashtable, long key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<long, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<long, TValue>.Node?[] hashtable, long key)
        {
            var hashCode = (int)key;
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class SingleMethod : IHotMethod<float>
    {
        public static readonly SingleMethod Instance = new ();

        private SingleMethod()
        {
        }

        public int BinarySearch(float[] array, int index, int length, float value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<float>.Node? leaf) SearchNode(OrderedSetObsolete<float>.Node? target, float value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class SingleMethod2<TValue> : IHotMethod2<float, TValue>
    {
        public (int cmp, OrderedMap<float, TValue>.Node? leaf) SearchNode(OrderedMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<float, TValue>.Node? leaf) SearchNode(OrderedMultiMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<float, TValue>.Node? SearchHashtable(UnorderedMap<float, TValue>.Node?[] hashtable, float key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<float, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<float, TValue>.Node?[] hashtable, float key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class DoubleMethod : IHotMethod<double>
    {
        public static readonly DoubleMethod Instance = new ();

        private DoubleMethod()
        {
        }

        public int BinarySearch(double[] array, int index, int length, double value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<double>.Node? leaf) SearchNode(OrderedSetObsolete<double>.Node? target, double value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class DoubleMethod2<TValue> : IHotMethod2<double, TValue>
    {
        public (int cmp, OrderedMap<double, TValue>.Node? leaf) SearchNode(OrderedMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<double, TValue>.Node? leaf) SearchNode(OrderedMultiMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<double, TValue>.Node? SearchHashtable(UnorderedMap<double, TValue>.Node?[] hashtable, double key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<double, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<double, TValue>.Node?[] hashtable, double key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }

    public sealed class DateTimeMethod : IHotMethod<DateTime>
    {
        public static readonly DateTimeMethod Instance = new ();

        private DateTimeMethod()
        {
        }

        public int BinarySearch(DateTime[] array, int index, int length, DateTime value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                if (value < array[mid])
                {
                    max = mid - 1;
                    continue;
                }
                else if (value > array[mid])
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }

        public (int cmp, OrderedSetObsolete<DateTime>.Node? leaf) SearchNode(OrderedSetObsolete<DateTime>.Node? target, DateTime value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (value < x.Value)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (value > x.Value)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class DateTimeMethod2<TValue> : IHotMethod2<DateTime, TValue>
    {
        public (int cmp, OrderedMap<DateTime, TValue>.Node? leaf) SearchNode(OrderedMap<DateTime, TValue>.Node? target, DateTime key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public (int cmp, OrderedMultiMap<DateTime, TValue>.Node? leaf) SearchNode(OrderedMultiMap<DateTime, TValue>.Node? target, DateTime key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key < x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key > x.Key)
                {
                    x = x.Right;
                    cmp = 1;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }

        public UnorderedMap<DateTime, TValue>.Node? SearchHashtable(UnorderedMap<DateTime, TValue>.Node?[] hashtable, DateTime key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            var n = hashtable[index];
            while (n != null)
            {
                if (n.HashCode == hashCode && n.Key == key)
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

        public (UnorderedMap<DateTime, TValue>.Node? found, int hashCode, int index) Probe(bool allowMultiple, UnorderedMap<DateTime, TValue>.Node?[] hashtable, DateTime key)
        {
            var hashCode = key.GetHashCode();
            var index = hashCode & (hashtable.Length - 1);
            if (!allowMultiple)
            {
                var n = hashtable[index];
                while (n != null)
                {
                    if (n.HashCode == hashCode && n.Key == key)
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
        }
    }
}
