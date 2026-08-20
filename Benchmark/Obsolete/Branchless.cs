using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

public unsafe class BinarySearchOptimized
{
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

    /// <summary>
    /// Returns the index of the first element not less than the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LowerBound_Branchless(ReadOnlySpan<long> span, long value)
    {
        ref long r = ref MemoryMarshal.GetReference(span);
        nuint lo = 0;
        nuint n = (nuint)(uint)span.Length;

        while (n > 1)
        {
            nuint half = n >> 1;
            bool c = Unsafe.Add(ref r, lo + half - 1) < value;
            lo += half & (nuint)(0 - (nuint)Unsafe.As<bool, byte>(ref c));
            n -= half;
        }

        if (n != 0)
        {
            bool c = Unsafe.Add(ref r, lo) < value;
            lo += Unsafe.As<bool, byte>(ref c);
        }
        return (int)lo;
    }

    /// <summary>
    /// Returns the index of the first element greater than the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UpperBoundExclusive_Branchless(ReadOnlySpan<long> span, long value)
    {
        ref long r = ref MemoryMarshal.GetReference(span);
        nuint lo = 0;
        nuint n = (nuint)(uint)span.Length;

        while (n > 1)
        {
            nuint half = n >> 1;
            bool c = Unsafe.Add(ref r, lo + half - 1) <= value;
            lo += half & (nuint)(0 - (nuint)Unsafe.As<bool, byte>(ref c));
            n -= half;
        }

        if (n != 0)
        {
            bool c = Unsafe.Add(ref r, lo) <= value;
            lo += Unsafe.As<bool, byte>(ref c);
        }
        return (int)lo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LowerBoundPrefetch(ReadOnlySpan<long> span, long value)
    {
        ref long r = ref MemoryMarshal.GetReference(span);
        nuint lo = 0;
        nuint n = (nuint)(uint)span.Length;

        while (n > 1)
        {
            nuint half = n >> 1;
            if (Sse.IsSupported)
            {
                nuint q = half >> 1;
                Sse.Prefetch0(Unsafe.AsPointer(ref Unsafe.Add(ref r, lo + q)));
                Sse.Prefetch0(Unsafe.AsPointer(ref Unsafe.Add(ref r, lo + half + q)));
            }
            bool c = Unsafe.Add(ref r, lo + half - 1) < value;
            lo += half & (nuint)(0 - (nuint)Unsafe.As<bool, byte>(ref c));
            n -= half;
        }

        if (n != 0)
        {
            bool c = Unsafe.Add(ref r, lo) < value;
            lo += Unsafe.As<bool, byte>(ref c);
        }
        return (int)lo;
    }
}
