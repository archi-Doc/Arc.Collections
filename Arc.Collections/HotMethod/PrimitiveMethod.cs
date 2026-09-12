// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Collections.HotMethod
{
    internal sealed class UInt8Method : IHotMethod<byte>
    {
        public static readonly UInt8Method Instance = new ();

        private UInt8Method()
        {
        }

        public int LowerBound(ReadOnlySpan<byte> span, byte value)
        {
            ref byte r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<byte> span, byte value)
        {
            ref byte r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class UInt8Method2<TValue> : IHotTreeMethod<byte, TValue>
    {
        public (int Comparison, OrderedMap<byte, TValue>.Node? Node) SearchNode(OrderedMap<byte, TValue>.Node? target, byte key)
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

        public (int Comparison, OrderedMap<byte, TValue>.Node? Node) SearchNodeReverse(OrderedMap<byte, TValue>.Node? target, byte key)
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

        public (int Comparison, OrderedMultiMap<byte, TValue>.Node? Node) SearchNode(OrderedMultiMap<byte, TValue>.Node? target, byte key)
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

        public (int Comparison, OrderedMultiMap<byte, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<byte, TValue>.Node? target, byte key)
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

    internal sealed class Int8Method : IHotMethod<sbyte>
    {
        public static readonly Int8Method Instance = new ();

        private Int8Method()
        {
        }

        public int LowerBound(ReadOnlySpan<sbyte> span, sbyte value)
        {
            ref sbyte r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<sbyte> span, sbyte value)
        {
            ref sbyte r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class Int8Method2<TValue> : IHotTreeMethod<sbyte, TValue>
    {
        public (int Comparison, OrderedMap<sbyte, TValue>.Node? Node) SearchNode(OrderedMap<sbyte, TValue>.Node? target, sbyte key)
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

        public (int Comparison, OrderedMap<sbyte, TValue>.Node? Node) SearchNodeReverse(OrderedMap<sbyte, TValue>.Node? target, sbyte key)
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

        public (int Comparison, OrderedMultiMap<sbyte, TValue>.Node? Node) SearchNode(OrderedMultiMap<sbyte, TValue>.Node? target, sbyte key)
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

        public (int Comparison, OrderedMultiMap<sbyte, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<sbyte, TValue>.Node? target, sbyte key)
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

    internal sealed class UInt16Method : IHotMethod<ushort>
    {
        public static readonly UInt16Method Instance = new ();

        private UInt16Method()
        {
        }

        public int LowerBound(ReadOnlySpan<ushort> span, ushort value)
        {
            ref ushort r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<ushort> span, ushort value)
        {
            ref ushort r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class UInt16Method2<TValue> : IHotTreeMethod<ushort, TValue>
    {
        public (int Comparison, OrderedMap<ushort, TValue>.Node? Node) SearchNode(OrderedMap<ushort, TValue>.Node? target, ushort key)
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

        public (int Comparison, OrderedMap<ushort, TValue>.Node? Node) SearchNodeReverse(OrderedMap<ushort, TValue>.Node? target, ushort key)
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

        public (int Comparison, OrderedMultiMap<ushort, TValue>.Node? Node) SearchNode(OrderedMultiMap<ushort, TValue>.Node? target, ushort key)
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

        public (int Comparison, OrderedMultiMap<ushort, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<ushort, TValue>.Node? target, ushort key)
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

    internal sealed class Int16Method : IHotMethod<short>
    {
        public static readonly Int16Method Instance = new ();

        private Int16Method()
        {
        }

        public int LowerBound(ReadOnlySpan<short> span, short value)
        {
            ref short r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<short> span, short value)
        {
            ref short r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class Int16Method2<TValue> : IHotTreeMethod<short, TValue>
    {
        public (int Comparison, OrderedMap<short, TValue>.Node? Node) SearchNode(OrderedMap<short, TValue>.Node? target, short key)
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

        public (int Comparison, OrderedMap<short, TValue>.Node? Node) SearchNodeReverse(OrderedMap<short, TValue>.Node? target, short key)
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

        public (int Comparison, OrderedMultiMap<short, TValue>.Node? Node) SearchNode(OrderedMultiMap<short, TValue>.Node? target, short key)
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

        public (int Comparison, OrderedMultiMap<short, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<short, TValue>.Node? target, short key)
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

    internal sealed class UInt32Method : IHotMethod<uint>
    {
        public static readonly UInt32Method Instance = new ();

        private UInt32Method()
        {
        }

        public int LowerBound(ReadOnlySpan<uint> span, uint value)
        {
            ref uint r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<uint> span, uint value)
        {
            ref uint r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class UInt32Method2<TValue> : IHotTreeMethod<uint, TValue>
    {
        public (int Comparison, OrderedMap<uint, TValue>.Node? Node) SearchNode(OrderedMap<uint, TValue>.Node? target, uint key)
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

        public (int Comparison, OrderedMap<uint, TValue>.Node? Node) SearchNodeReverse(OrderedMap<uint, TValue>.Node? target, uint key)
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

        public (int Comparison, OrderedMultiMap<uint, TValue>.Node? Node) SearchNode(OrderedMultiMap<uint, TValue>.Node? target, uint key)
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

        public (int Comparison, OrderedMultiMap<uint, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<uint, TValue>.Node? target, uint key)
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

    internal sealed class Int32Method : IHotMethod<int>
    {
        public static readonly Int32Method Instance = new ();

        private Int32Method()
        {
        }

        public int LowerBound(ReadOnlySpan<int> span, int value)
        {
            ref int r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<int> span, int value)
        {
            ref int r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class Int32Method2<TValue> : IHotTreeMethod<int, TValue>
    {
        public (int Comparison, OrderedMap<int, TValue>.Node? Node) SearchNode(OrderedMap<int, TValue>.Node? target, int key)
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

        public (int Comparison, OrderedMap<int, TValue>.Node? Node) SearchNodeReverse(OrderedMap<int, TValue>.Node? target, int key)
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

        public (int Comparison, OrderedMultiMap<int, TValue>.Node? Node) SearchNode(OrderedMultiMap<int, TValue>.Node? target, int key)
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

        public (int Comparison, OrderedMultiMap<int, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<int, TValue>.Node? target, int key)
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

    internal sealed class UInt64Method : IHotMethod<ulong>
    {
        public static readonly UInt64Method Instance = new ();

        private UInt64Method()
        {
        }

        public int LowerBound(ReadOnlySpan<ulong> span, ulong value)
        {
            ref ulong r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<ulong> span, ulong value)
        {
            ref ulong r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class UInt64Method2<TValue> : IHotTreeMethod<ulong, TValue>
    {
        public (int Comparison, OrderedMap<ulong, TValue>.Node? Node) SearchNode(OrderedMap<ulong, TValue>.Node? target, ulong key)
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

        public (int Comparison, OrderedMap<ulong, TValue>.Node? Node) SearchNodeReverse(OrderedMap<ulong, TValue>.Node? target, ulong key)
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

        public (int Comparison, OrderedMultiMap<ulong, TValue>.Node? Node) SearchNode(OrderedMultiMap<ulong, TValue>.Node? target, ulong key)
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

        public (int Comparison, OrderedMultiMap<ulong, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<ulong, TValue>.Node? target, ulong key)
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

    internal sealed class Int64Method : IHotMethod<long>
    {
        public static readonly Int64Method Instance = new ();

        private Int64Method()
        {
        }

        public int LowerBound(ReadOnlySpan<long> span, long value)
        {
            ref long r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<long> span, long value)
        {
            ref long r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class Int64Method2<TValue> : IHotTreeMethod<long, TValue>
    {
        public (int Comparison, OrderedMap<long, TValue>.Node? Node) SearchNode(OrderedMap<long, TValue>.Node? target, long key)
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

        public (int Comparison, OrderedMap<long, TValue>.Node? Node) SearchNodeReverse(OrderedMap<long, TValue>.Node? target, long key)
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

        public (int Comparison, OrderedMultiMap<long, TValue>.Node? Node) SearchNode(OrderedMultiMap<long, TValue>.Node? target, long key)
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

        public (int Comparison, OrderedMultiMap<long, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<long, TValue>.Node? target, long key)
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

    internal sealed class UInt128Method : IHotMethod<UInt128>
    {
        public static readonly UInt128Method Instance = new ();

        private UInt128Method()
        {
        }

        public int LowerBound(ReadOnlySpan<UInt128> span, UInt128 value)
        {
            ref UInt128 r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<UInt128> span, UInt128 value)
        {
            ref UInt128 r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class UInt128Method2<TValue> : IHotTreeMethod<UInt128, TValue>
    {
        public (int Comparison, OrderedMap<UInt128, TValue>.Node? Node) SearchNode(OrderedMap<UInt128, TValue>.Node? target, UInt128 key)
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

        public (int Comparison, OrderedMap<UInt128, TValue>.Node? Node) SearchNodeReverse(OrderedMap<UInt128, TValue>.Node? target, UInt128 key)
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

        public (int Comparison, OrderedMultiMap<UInt128, TValue>.Node? Node) SearchNode(OrderedMultiMap<UInt128, TValue>.Node? target, UInt128 key)
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

        public (int Comparison, OrderedMultiMap<UInt128, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<UInt128, TValue>.Node? target, UInt128 key)
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

    internal sealed class Int128Method : IHotMethod<Int128>
    {
        public static readonly Int128Method Instance = new ();

        private Int128Method()
        {
        }

        public int LowerBound(ReadOnlySpan<Int128> span, Int128 value)
        {
            ref Int128 r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<Int128> span, Int128 value)
        {
            ref Int128 r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class Int128Method2<TValue> : IHotTreeMethod<Int128, TValue>
    {
        public (int Comparison, OrderedMap<Int128, TValue>.Node? Node) SearchNode(OrderedMap<Int128, TValue>.Node? target, Int128 key)
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

        public (int Comparison, OrderedMap<Int128, TValue>.Node? Node) SearchNodeReverse(OrderedMap<Int128, TValue>.Node? target, Int128 key)
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

        public (int Comparison, OrderedMultiMap<Int128, TValue>.Node? Node) SearchNode(OrderedMultiMap<Int128, TValue>.Node? target, Int128 key)
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

        public (int Comparison, OrderedMultiMap<Int128, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<Int128, TValue>.Node? target, Int128 key)
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

    internal sealed class SingleMethod : IHotMethod<float>
    {
        public static readonly SingleMethod Instance = new ();

        private SingleMethod()
        {
        }

        public int LowerBound(ReadOnlySpan<float> span, float value)
        {
            ref float r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid).CompareTo(value) < 0)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<float> span, float value)
        {
            ref float r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid).CompareTo(value) <= 0)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class SingleMethod2<TValue> : IHotTreeMethod<float, TValue>
    {
        public (int Comparison, OrderedMap<float, TValue>.Node? Node) SearchNode(OrderedMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) < 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) > 0)
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

        public (int Comparison, OrderedMap<float, TValue>.Node? Node) SearchNodeReverse(OrderedMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) > 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) < 0)
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

        public (int Comparison, OrderedMultiMap<float, TValue>.Node? Node) SearchNode(OrderedMultiMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) < 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) > 0)
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

        public (int Comparison, OrderedMultiMap<float, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<float, TValue>.Node? target, float key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) > 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) < 0)
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

    internal sealed class DoubleMethod : IHotMethod<double>
    {
        public static readonly DoubleMethod Instance = new ();

        private DoubleMethod()
        {
        }

        public int LowerBound(ReadOnlySpan<double> span, double value)
        {
            ref double r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid).CompareTo(value) < 0)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<double> span, double value)
        {
            ref double r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid).CompareTo(value) <= 0)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class DoubleMethod2<TValue> : IHotTreeMethod<double, TValue>
    {
        public (int Comparison, OrderedMap<double, TValue>.Node? Node) SearchNode(OrderedMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) < 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) > 0)
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

        public (int Comparison, OrderedMap<double, TValue>.Node? Node) SearchNodeReverse(OrderedMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) > 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) < 0)
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

        public (int Comparison, OrderedMultiMap<double, TValue>.Node? Node) SearchNode(OrderedMultiMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) < 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) > 0)
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

        public (int Comparison, OrderedMultiMap<double, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<double, TValue>.Node? target, double key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                p = x;
                if (key.CompareTo(x.Key) > 0)
                {
                    x = x.Left;
                    cmp = -1;
                }
                else if (key.CompareTo(x.Key) < 0)
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

    internal sealed class DateTimeMethod : IHotMethod<DateTime>
    {
        public static readonly DateTimeMethod Instance = new ();

        private DateTimeMethod()
        {
        }

        public int LowerBound(ReadOnlySpan<DateTime> span, DateTime value)
        {
            ref DateTime r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) < value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }

        public int UpperBoundExclusive(ReadOnlySpan<DateTime> span, DateTime value)
        {
            ref DateTime r = ref MemoryMarshal.GetReference(span);
            nuint lo = 0;
            nuint count = (nuint)(uint)span.Length;
            while (count != 0)
            {
                nuint half = count >> 1;
                nuint mid = lo + half;
                if (Unsafe.Add(ref r, (nint)mid) <= value)
                {
                    lo = mid + 1;
                    count -= half + 1;
                }
                else
                {
                    count = half;
                }
            }

            return (int)lo;
        }
    }

    internal sealed class DateTimeMethod2<TValue> : IHotTreeMethod<DateTime, TValue>
    {
        public (int Comparison, OrderedMap<DateTime, TValue>.Node? Node) SearchNode(OrderedMap<DateTime, TValue>.Node? target, DateTime key)
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

        public (int Comparison, OrderedMap<DateTime, TValue>.Node? Node) SearchNodeReverse(OrderedMap<DateTime, TValue>.Node? target, DateTime key)
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

        public (int Comparison, OrderedMultiMap<DateTime, TValue>.Node? Node) SearchNode(OrderedMultiMap<DateTime, TValue>.Node? target, DateTime key)
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

        public (int Comparison, OrderedMultiMap<DateTime, TValue>.Node? Node) SearchNodeReverse(OrderedMultiMap<DateTime, TValue>.Node? target, DateTime key)
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
