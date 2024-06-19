// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Collections.HotMethod
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
    }

    public sealed class UInt8Method2<TValue> : IHotMethod2<byte, TValue>
    {
        public (int Cmp, OrderedMap<byte, TValue>.Node? Leaf) SearchNode(OrderedMap<byte, TValue>.Node? target, byte key)
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

        public (int Cmp, OrderedMap<byte, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<byte, TValue>.Node? target, byte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<byte, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<byte, TValue>.Node? target, byte key)
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

        public (int Cmp, OrderedMultiMap<byte, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<byte, TValue>.Node? target, byte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class Int8Method2<TValue> : IHotMethod2<sbyte, TValue>
    {
        public (int Cmp, OrderedMap<sbyte, TValue>.Node? Leaf) SearchNode(OrderedMap<sbyte, TValue>.Node? target, sbyte key)
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

        public (int Cmp, OrderedMap<sbyte, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<sbyte, TValue>.Node? target, sbyte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<sbyte, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<sbyte, TValue>.Node? target, sbyte key)
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

        public (int Cmp, OrderedMultiMap<sbyte, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<sbyte, TValue>.Node? target, sbyte key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class UInt16Method2<TValue> : IHotMethod2<ushort, TValue>
    {
        public (int Cmp, OrderedMap<ushort, TValue>.Node? Leaf) SearchNode(OrderedMap<ushort, TValue>.Node? target, ushort key)
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

        public (int Cmp, OrderedMap<ushort, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<ushort, TValue>.Node? target, ushort key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<ushort, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<ushort, TValue>.Node? target, ushort key)
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

        public (int Cmp, OrderedMultiMap<ushort, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<ushort, TValue>.Node? target, ushort key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class Int16Method2<TValue> : IHotMethod2<short, TValue>
    {
        public (int Cmp, OrderedMap<short, TValue>.Node? Leaf) SearchNode(OrderedMap<short, TValue>.Node? target, short key)
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

        public (int Cmp, OrderedMap<short, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<short, TValue>.Node? target, short key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<short, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<short, TValue>.Node? target, short key)
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

        public (int Cmp, OrderedMultiMap<short, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<short, TValue>.Node? target, short key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class UInt32Method2<TValue> : IHotMethod2<uint, TValue>
    {
        public (int Cmp, OrderedMap<uint, TValue>.Node? Leaf) SearchNode(OrderedMap<uint, TValue>.Node? target, uint key)
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

        public (int Cmp, OrderedMap<uint, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<uint, TValue>.Node? target, uint key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<uint, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<uint, TValue>.Node? target, uint key)
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

        public (int Cmp, OrderedMultiMap<uint, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<uint, TValue>.Node? target, uint key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class Int32Method2<TValue> : IHotMethod2<int, TValue>
    {
        public (int Cmp, OrderedMap<int, TValue>.Node? Leaf) SearchNode(OrderedMap<int, TValue>.Node? target, int key)
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

        public (int Cmp, OrderedMap<int, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<int, TValue>.Node? target, int key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<int, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<int, TValue>.Node? target, int key)
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

        public (int Cmp, OrderedMultiMap<int, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<int, TValue>.Node? target, int key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class UInt64Method2<TValue> : IHotMethod2<ulong, TValue>
    {
        public (int Cmp, OrderedMap<ulong, TValue>.Node? Leaf) SearchNode(OrderedMap<ulong, TValue>.Node? target, ulong key)
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

        public (int Cmp, OrderedMap<ulong, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<ulong, TValue>.Node? target, ulong key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<ulong, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<ulong, TValue>.Node? target, ulong key)
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

        public (int Cmp, OrderedMultiMap<ulong, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<ulong, TValue>.Node? target, ulong key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class Int64Method2<TValue> : IHotMethod2<long, TValue>
    {
        public (int Cmp, OrderedMap<long, TValue>.Node? Leaf) SearchNode(OrderedMap<long, TValue>.Node? target, long key)
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

        public (int Cmp, OrderedMap<long, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<long, TValue>.Node? target, long key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<long, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<long, TValue>.Node? target, long key)
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

        public (int Cmp, OrderedMultiMap<long, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<long, TValue>.Node? target, long key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

    public sealed class UInt128Method : IHotMethod<UInt128>
    {
        public static readonly UInt128Method Instance = new ();

        private UInt128Method()
        {
        }

        public int BinarySearch(UInt128[] array, int index, int length, UInt128 value)
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
    }

    public sealed class UInt128Method2<TValue> : IHotMethod2<UInt128, TValue>
    {
        public (int Cmp, OrderedMap<UInt128, TValue>.Node? Leaf) SearchNode(OrderedMap<UInt128, TValue>.Node? target, UInt128 key)
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

        public (int Cmp, OrderedMap<UInt128, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<UInt128, TValue>.Node? target, UInt128 key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<UInt128, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<UInt128, TValue>.Node? target, UInt128 key)
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

        public (int Cmp, OrderedMultiMap<UInt128, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<UInt128, TValue>.Node? target, UInt128 key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

    public sealed class Int128Method : IHotMethod<Int128>
    {
        public static readonly Int128Method Instance = new ();

        private Int128Method()
        {
        }

        public int BinarySearch(Int128[] array, int index, int length, Int128 value)
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
    }

    public sealed class Int128Method2<TValue> : IHotMethod2<Int128, TValue>
    {
        public (int Cmp, OrderedMap<Int128, TValue>.Node? Leaf) SearchNode(OrderedMap<Int128, TValue>.Node? target, Int128 key)
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

        public (int Cmp, OrderedMap<Int128, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<Int128, TValue>.Node? target, Int128 key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<Int128, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<Int128, TValue>.Node? target, Int128 key)
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

        public (int Cmp, OrderedMultiMap<Int128, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<Int128, TValue>.Node? target, Int128 key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class SingleMethod2<TValue> : IHotMethod2<float, TValue>
    {
        public (int Cmp, OrderedMap<float, TValue>.Node? Leaf) SearchNode(OrderedMap<float, TValue>.Node? target, float key)
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

        public (int Cmp, OrderedMap<float, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<float, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<float, TValue>.Node? target, float key)
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

        public (int Cmp, OrderedMultiMap<float, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class DoubleMethod2<TValue> : IHotMethod2<double, TValue>
    {
        public (int Cmp, OrderedMap<double, TValue>.Node? Leaf) SearchNode(OrderedMap<double, TValue>.Node? target, double key)
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

        public (int Cmp, OrderedMap<double, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<double, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<double, TValue>.Node? target, double key)
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

        public (int Cmp, OrderedMultiMap<double, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
    }

    public sealed class DateTimeMethod2<TValue> : IHotMethod2<DateTime, TValue>
    {
        public (int Cmp, OrderedMap<DateTime, TValue>.Node? Leaf) SearchNode(OrderedMap<DateTime, TValue>.Node? target, DateTime key)
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

        public (int Cmp, OrderedMap<DateTime, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<DateTime, TValue>.Node? target, DateTime key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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

        public (int Cmp, OrderedMultiMap<DateTime, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<DateTime, TValue>.Node? target, DateTime key)
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

        public (int Cmp, OrderedMultiMap<DateTime, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<DateTime, TValue>.Node? target, DateTime key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key > x.Key)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key < x.Key)
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
}
