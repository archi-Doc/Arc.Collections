// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Collections.HotMethod
{
    /// <summary>
    /// Provides binary search operations specialized for <see cref="byte"/>.
    /// </summary>
    public sealed class UInt8Method : IHotMethod<byte>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly UInt8Method Instance = new ();

        private UInt8Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="byte"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class UInt8Method2<TValue> : IHotMethod2<byte, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="sbyte"/>.
    /// </summary>
    public sealed class Int8Method : IHotMethod<sbyte>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Int8Method Instance = new ();

        private Int8Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="sbyte"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class Int8Method2<TValue> : IHotMethod2<sbyte, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="ushort"/>.
    /// </summary>
    public sealed class UInt16Method : IHotMethod<ushort>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly UInt16Method Instance = new ();

        private UInt16Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="ushort"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class UInt16Method2<TValue> : IHotMethod2<ushort, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="short"/>.
    /// </summary>
    public sealed class Int16Method : IHotMethod<short>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Int16Method Instance = new ();

        private Int16Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="short"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class Int16Method2<TValue> : IHotMethod2<short, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="uint"/>.
    /// </summary>
    public sealed class UInt32Method : IHotMethod<uint>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly UInt32Method Instance = new ();

        private UInt32Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="uint"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class UInt32Method2<TValue> : IHotMethod2<uint, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="int"/>.
    /// </summary>
    public sealed class Int32Method : IHotMethod<int>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Int32Method Instance = new ();

        private Int32Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="int"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class Int32Method2<TValue> : IHotMethod2<int, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="ulong"/>.
    /// </summary>
    public sealed class UInt64Method : IHotMethod<ulong>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly UInt64Method Instance = new ();

        private UInt64Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="ulong"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class UInt64Method2<TValue> : IHotMethod2<ulong, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="long"/>.
    /// </summary>
    public sealed class Int64Method : IHotMethod<long>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Int64Method Instance = new ();

        private Int64Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="long"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class Int64Method2<TValue> : IHotMethod2<long, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="UInt128"/>.
    /// </summary>
    public sealed class UInt128Method : IHotMethod<UInt128>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly UInt128Method Instance = new ();

        private UInt128Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="UInt128"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class UInt128Method2<TValue> : IHotMethod2<UInt128, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="Int128"/>.
    /// </summary>
    public sealed class Int128Method : IHotMethod<Int128>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Int128Method Instance = new ();

        private Int128Method()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="Int128"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class Int128Method2<TValue> : IHotMethod2<Int128, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="float"/>.
    /// </summary>
    public sealed class SingleMethod : IHotMethod<float>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly SingleMethod Instance = new ();

        private SingleMethod()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="float"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class SingleMethod2<TValue> : IHotMethod2<float, TValue>
    {
        /// <inheritdoc/>
        public (int Cmp, OrderedMap<float, TValue>.Node? Leaf) SearchNode(OrderedMap<float, TValue>.Node? target, float key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMap<float, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<float, TValue>.Node? target, float key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMultiMap<float, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<float, TValue>.Node? target, float key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMultiMap<float, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<float, TValue>.Node? target, float key)
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="double"/>.
    /// </summary>
    public sealed class DoubleMethod : IHotMethod<double>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly DoubleMethod Instance = new ();

        private DoubleMethod()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="double"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class DoubleMethod2<TValue> : IHotMethod2<double, TValue>
    {
        /// <inheritdoc/>
        public (int Cmp, OrderedMap<double, TValue>.Node? Leaf) SearchNode(OrderedMap<double, TValue>.Node? target, double key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMap<double, TValue>.Node? Leaf) SearchNodeReverse(OrderedMap<double, TValue>.Node? target, double key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMultiMap<double, TValue>.Node? Leaf) SearchNode(OrderedMultiMap<double, TValue>.Node? target, double key)
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

        /// <inheritdoc/>
        public (int Cmp, OrderedMultiMap<double, TValue>.Node? Leaf) SearchNodeReverse(OrderedMultiMap<double, TValue>.Node? target, double key)
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

    /// <summary>
    /// Provides binary search operations specialized for <see cref="DateTime"/>.
    /// </summary>
    public sealed class DateTimeMethod : IHotMethod<DateTime>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly DateTimeMethod Instance = new ();

        private DateTimeMethod()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    /// <summary>
    /// Provides Red-Black tree search operations specialized for <see cref="DateTime"/> keys.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the map.</typeparam>
    public sealed class DateTimeMethod2<TValue> : IHotMethod2<DateTime, TValue>
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
