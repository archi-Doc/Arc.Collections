// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Arc;

/// <summary>
/// Stores 128 bits of binary data with overlapping numeric field views.
/// </summary>
/// <remarks>
/// Byte conversions use native byte order. Comparison is lexicographic over the signed
/// 64-bit fields in field order, rather than unsigned 128-bit numeric order.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Struct128 : IEquatable<Struct128>, IComparable<Struct128>
{
    /// <summary>
    /// The display name used by <see cref="ToString"/>.
    /// </summary>
    public const string Name = "Struct128";

    /// <summary>
    /// The size of the structure, in bytes.
    /// </summary>
    public const int Length = 16;

    /// <summary>
    /// A value with all bits cleared.
    /// </summary>
    public static readonly Struct128 Zero = default;

    /// <summary>
    /// A value representing 1.
    /// </summary>
    public static readonly Struct128 One = new(1);

    /// <summary>
    /// A value representing 2.
    /// </summary>
    public static readonly Struct128 Two = new(2);

    /// <summary>
    /// A value representing 3.
    /// </summary>
    public static readonly Struct128 Three = new(3);

    #region FieldAndProperty

    /// <summary>
    /// The whole value viewed as a <see cref="System.UInt128"/>.
    /// </summary>
    [FieldOffset(0)]
    public readonly UInt128 UInt128;

    /// <summary>
    /// The 64-bit value at byte offset 0.
    /// </summary>
    [FieldOffset(0)]
    public readonly long Long0;

    /// <summary>
    /// The 64-bit value at byte offset 8.
    /// </summary>
    [FieldOffset(8)]
    public readonly long Long1;

    /// <summary>
    /// The 32-bit value at byte offset 0.
    /// </summary>
    [FieldOffset(0)]
    public readonly int Int0;

    /// <summary>
    /// The 32-bit value at byte offset 4.
    /// </summary>
    [FieldOffset(4)]
    public readonly int Int1;

    /// <summary>
    /// The 32-bit value at byte offset 8.
    /// </summary>
    [FieldOffset(8)]
    public readonly int Int2;

    /// <summary>
    /// The 32-bit value at byte offset 12.
    /// </summary>
    [FieldOffset(12)]
    public readonly int Int3;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Struct128"/> struct from a 32-bit value.
    /// </summary>
    /// <param name="int0">The value stored in the lowest 64 bits. The remaining bits are cleared.</param>
    public Struct128(int int0)
    {
        this.Long0 = int0;
        this.Long1 = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Struct128"/> struct from a 64-bit value.
    /// </summary>
    /// <param name="long0">The value stored in the lowest 64 bits. The remaining bits are cleared.</param>
    public Struct128(long long0)
    {
        this.Long0 = long0;
        this.Long1 = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Struct128"/> struct from two 64-bit values.
    /// </summary>
    /// <param name="long0">The value stored at byte offset 0.</param>
    /// <param name="long1">The value stored at byte offset 8.</param>
    public Struct128(long long0, long long1)
    {
        this.Long0 = long0;
        this.Long1 = long1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Struct128"/> struct by copying another value.
    /// </summary>
    /// <param name="struct128">The value to copy.</param>
    public Struct128(ref Struct128 struct128)
    {
        this.Long0 = struct128.Long0;
        this.Long1 = struct128.Long1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Struct128"/> struct from a byte span.
    /// </summary>
    /// <param name="span">The source span. It must contain at least <see cref="Length"/> bytes.</param>
    /// <exception cref="ArgumentException"><paramref name="span"/> is shorter than <see cref="Length"/>.</exception>
    public Struct128(ReadOnlySpan<byte> span)
    {
        if (span.Length < Length)
        {
            throw new ArgumentException($"Length of a byte array must be at least {Length}.", nameof(span));
        }

        this = MemoryMarshal.Read<Struct128>(span);
    }

    /// <summary>
    /// Attempts to write this value to the destination span in native byte order.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <returns><see langword="true"/> if the value was written; <see langword="false"/> if the span is shorter than <see cref="Length"/>.</returns>
    public bool TryWriteBytes(Span<byte> destination)
    {
        if (destination.Length < Length)
        {
            return false;
        }

        MemoryMarshal.Write(destination, in this);
        return true;
    }

    /// <summary>
    /// Returns a read-only view over the raw bytes of this value.
    /// </summary>
    /// <returns>A span of <see cref="Length"/> bytes.</returns>
    [UnscopedRef]
    public ReadOnlySpan<byte> AsSpan()
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));

    /// <summary>
    /// Gets a value indicating whether all bits are zero.
    /// </summary>
    public bool IsZero
        => this.UInt128 == 0;

    /// <summary>
    /// Determines whether this value equals another value of the same type.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Struct128 other)
        => this.UInt128 == other.UInt128;

    /// <summary>
    /// Determines whether this value equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Struct128"/> with the same value.</returns>
    public override bool Equals(object? obj)
        => obj is Struct128 other && this.Equals(other);

    /// <summary>
    /// Returns the hash code for this value.
    /// </summary>
    /// <returns>A hash code derived from all 128 bits.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Long0, this.Long1);
    }

    /// <summary>
    /// Returns a string representation of this value.
    /// </summary>
    /// <returns>The name followed by the full 128-bit value in hexadecimal,
    /// or by a short decimal form for small values.</returns>
    public override string ToString()
    {
        if (this.UInt128 <= 9)
        {
            return $"{Name}: {(ulong)this.UInt128}";
        }

        return $"{Name}: {this.UInt128:X32}";
    }

    /// <summary>
    /// Compares this value with another value of the same type.
    /// The fields are compared in ascending order, starting with <see cref="Long0"/>.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>A negative value, zero, or a positive value depending on the relative order.</returns>
    public int CompareTo(Struct128 other)
    {
        if (this.Long0 > other.Long0)
        {
            return 1;
        }
        else if (this.Long0 < other.Long0)
        {
            return -1;
        }

        if (this.Long1 > other.Long1)
        {
            return 1;
        }
        else if (this.Long1 < other.Long1)
        {
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Struct128 left, Struct128 right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two values are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the values differ; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Struct128 left, Struct128 right)
        => !left.Equals(right);
}
