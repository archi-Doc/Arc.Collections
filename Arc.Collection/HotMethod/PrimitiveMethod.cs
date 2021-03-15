// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;

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

        public (int cmp, OrderedSet<byte>.Node? leaf) SearchNode(OrderedSet<byte>.Node? target, byte value)
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
        public static readonly UInt8Method2<TValue> Instance = new ();

        private UInt8Method2()
        {
        }

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

        public (int cmp, OrderedSet<sbyte>.Node? leaf) SearchNode(OrderedSet<sbyte>.Node? target, sbyte value)
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
        public static readonly Int8Method2<TValue> Instance = new ();

        private Int8Method2()
        {
        }

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

        public (int cmp, OrderedSet<ushort>.Node? leaf) SearchNode(OrderedSet<ushort>.Node? target, ushort value)
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
        public static readonly UInt16Method2<TValue> Instance = new ();

        private UInt16Method2()
        {
        }

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

        public (int cmp, OrderedSet<short>.Node? leaf) SearchNode(OrderedSet<short>.Node? target, short value)
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
        public static readonly Int16Method2<TValue> Instance = new ();

        private Int16Method2()
        {
        }

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

        public (int cmp, OrderedSet<uint>.Node? leaf) SearchNode(OrderedSet<uint>.Node? target, uint value)
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
        public static readonly UInt32Method2<TValue> Instance = new ();

        private UInt32Method2()
        {
        }

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

        public (int cmp, OrderedSet<int>.Node? leaf) SearchNode(OrderedSet<int>.Node? target, int value)
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
        public static readonly Int32Method2<TValue> Instance = new ();

        private Int32Method2()
        {
        }

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

        public (int cmp, OrderedSet<ulong>.Node? leaf) SearchNode(OrderedSet<ulong>.Node? target, ulong value)
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
        public static readonly UInt64Method2<TValue> Instance = new ();

        private UInt64Method2()
        {
        }

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

        public (int cmp, OrderedSet<long>.Node? leaf) SearchNode(OrderedSet<long>.Node? target, long value)
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
        public static readonly Int64Method2<TValue> Instance = new ();

        private Int64Method2()
        {
        }

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

        public (int cmp, OrderedSet<float>.Node? leaf) SearchNode(OrderedSet<float>.Node? target, float value)
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
        public static readonly SingleMethod2<TValue> Instance = new ();

        private SingleMethod2()
        {
        }

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

        public (int cmp, OrderedSet<double>.Node? leaf) SearchNode(OrderedSet<double>.Node? target, double value)
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
        public static readonly DoubleMethod2<TValue> Instance = new ();

        private DoubleMethod2()
        {
        }

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

        public (int cmp, OrderedSet<DateTime>.Node? leaf) SearchNode(OrderedSet<DateTime>.Node? target, DateTime value)
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
        public static readonly DateTimeMethod2<TValue> Instance = new ();

        private DateTimeMethod2()
        {
        }

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
    }
}
