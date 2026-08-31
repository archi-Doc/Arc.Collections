// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Arc;

/// <summary>
/// Represents an unconventional struct designed to handle 128-bit (16-byte) data.
/// </summary>
/// <remarks>
/// This structure supports multiple constructors for initialization, methods for byte conversion,<br/>
/// and implements <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/> interfaces.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Struct128 : IEquatable<Struct128>, IComparable<Struct128>
{
    public const string Name = "Struct128";
    public const int Length = 16;

    public static readonly Struct128 Zero = default;

    public static readonly Struct128 One = new(1);

    public static readonly Struct128 Two = new(2);

    public static readonly Struct128 Three = new(3);

    #region FieldAndProperty

    [FieldOffset(0)]
    public readonly UInt128 UInt128;

    [FieldOffset(0)]
    public readonly long Long0;
    [FieldOffset(8)]
    public readonly long Long1;

    [FieldOffset(0)]
    public readonly int Int0;
    [FieldOffset(4)]
    public readonly int Int1;
    [FieldOffset(8)]
    public readonly int Int2;
    [FieldOffset(12)]
    public readonly int Int3;

    #endregion

    public Struct128(int int0)
    {
        this.Long0 = int0;
        this.Long1 = 0;
    }

    public Struct128(long long0)
    {
        this.Long0 = long0;
        this.Long1 = 0;
    }

    public Struct128(long long0, long long1)
    {
        this.Long0 = long0;
        this.Long1 = long1;
    }

    public Struct128(ref Struct128 struct128)
    {
        this.Long0 = struct128.Long0;
        this.Long1 = struct128.Long1;
    }

    public Struct128(ReadOnlySpan<byte> span)
    {
        if (span.Length < Length)
        {
            throw new ArgumentException($"Length of a byte array must be at least {Length}.", nameof(span));
        }

        this = MemoryMarshal.Read<Struct128>(span);
    }

    public bool TryWriteBytes(Span<byte> destination)
    {
        if (destination.Length < Length)
        {
            return false;
        }

        MemoryMarshal.Write(destination, in this);
        return true;
    }

    [UnscopedRef]
    public ReadOnlySpan<byte> AsSpan()
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));

    public bool IsZero
        => this.UInt128 == 0;

    public bool Equals(Struct128 other)
        => this.UInt128 == other.UInt128;

    public override bool Equals(object? obj)
        => obj is Struct128 other && this.Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Long0, this.Long1);
    }

    public override string ToString()
    {
        if (this.Long1 == 0)
        {
            if (this.Long0 == 0)
            {
                return $"{Name}: 0";
            }
            else if (this.Long0 == 1)
            {
                return $"{Name}: 1";
            }
            else if (this.Long0 == 2)
            {
                return $"{Name}: 2";
            }
        }

        return $"{Name}: {this.Long0:D4}";
    }

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

    public static bool operator ==(Struct128 left, Struct128 right)
        => left.Equals(right);

    public static bool operator !=(Struct128 left, Struct128 right)
        => !left.Equals(right);
}
