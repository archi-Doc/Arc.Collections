// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Arc.Collections;

// Ported from System.IO.Hashing.XxHash3Slim.

[SkipLocalsInit]
public static unsafe class XxHash3Slim
{
    private const int StripeLengthBytes = 64;
    private const int SecretLengthBytes = 192;
    private const int SecretLastAccStartBytes = 7;
    private const int SecretConsumeRateBytes = 8;
    private const int MidSizeMaxBytes = 240;
    private const int AccumulatorCount = StripeLengthBytes / sizeof(ulong);

    private const ulong Prime64_1 = 0x9E3779B185EBCA87UL;
    private const ulong Prime64_2 = 0xC2B2AE3D27D4EB4FUL;
    private const ulong Prime64_3 = 0x165667B19E3779F9UL;
    private const ulong Prime64_4 = 0x85EBCA77C2B2AE63UL;
    private const ulong Prime64_5 = 0x27D4EB2F165667C5UL;

    private const uint Prime32_1 = 0x9E3779B1U;
    private const uint Prime32_2 = 0x85EBCA77U;
    private const uint Prime32_3 = 0xC2B2AE3DU;
    private const uint Prime32_4 = 0x27D4EB2FU;
    private const uint Prime32_5 = 0x165667B1U;

    private const ulong DefaultSecretUInt64_0 = 0xBE4BA423396CFEB8;
    private const ulong DefaultSecretUInt64_1 = 0x1CAD21F72C81017C;
    private const ulong DefaultSecretUInt64_2 = 0xDB979083E96DD4DE;
    private const ulong DefaultSecretUInt64_3 = 0x1F67B3B7A4A44072;
    private const ulong DefaultSecretUInt64_4 = 0x78E5C0CC4EE679CB;
    private const ulong DefaultSecretUInt64_5 = 0x2172FFCC7DD05A82;
    private const ulong DefaultSecretUInt64_6 = 0x8E2443F7744608B8;
    private const ulong DefaultSecretUInt64_7 = 0x4C263A81E69035E0;
    private const ulong DefaultSecretUInt64_8 = 0xCB00C391BB52283C;
    private const ulong DefaultSecretUInt64_9 = 0xA32E531B8B65D088;
    private const ulong DefaultSecretUInt64_10 = 0x4EF90DA297486471;
    private const ulong DefaultSecretUInt64_11 = 0xD8ACDEA946EF1938;
    private const ulong DefaultSecretUInt64_12 = 0x3F349CE33F76FAA8;
    private const ulong DefaultSecretUInt64_13 = 0x1D4F0BC7C7BBDCF9;
    private const ulong DefaultSecretUInt64_14 = 0x3159B4CD4BE0518A;
    private const ulong DefaultSecretUInt64_15 = 0x647378D9C97E9FC8;

    private const ulong DefaultSecret3UInt64_0 = 0x81017CBE4BA42339;
    private const ulong DefaultSecret3UInt64_1 = 0x6DD4DE1CAD21F72C;
    private const ulong DefaultSecret3UInt64_2 = 0xA44072DB979083E9;
    private const ulong DefaultSecret3UInt64_3 = 0xE679CB1F67B3B7A4;
    private const ulong DefaultSecret3UInt64_4 = 0xD05A8278E5C0CC4E;
    private const ulong DefaultSecret3UInt64_5 = 0x4608B82172FFCC7D;
    private const ulong DefaultSecret3UInt64_6 = 0x9035E08E2443F774;
    private const ulong DefaultSecret3UInt64_7 = 0x52283C4C263A81E6;
    private const ulong DefaultSecret3UInt64_8 = 0x65D088CB00C391BB;
    private const ulong DefaultSecret3UInt64_9 = 0x486471A32E531B8B;
    private const ulong DefaultSecret3UInt64_10 = 0xEF19384EF90DA297;
    private const ulong DefaultSecret3UInt64_11 = 0x76FAA8D8ACDEA946;
    private const ulong DefaultSecret3UInt64_12 = 0xBBDCF93F349CE33F;
    private const ulong DefaultSecret3UInt64_13 = 0xE0518A1D4F0BC7C7;

    private static ReadOnlySpan<byte> DefaultSecret =>
    [
        0xb8, 0xfe, 0x6c, 0x39, 0x23, 0xa4, 0x4b, 0xbe, // DefaultSecretUInt64_0
            0x7c, 0x01, 0x81, 0x2c, 0xf7, 0x21, 0xad, 0x1c, // DefaultSecretUInt64_1
            0xde, 0xd4, 0x6d, 0xe9, 0x83, 0x90, 0x97, 0xdb, // DefaultSecretUInt64_2
            0x72, 0x40, 0xa4, 0xa4, 0xb7, 0xb3, 0x67, 0x1f, // DefaultSecretUInt64_3
            0xcb, 0x79, 0xe6, 0x4e, 0xcc, 0xc0, 0xe5, 0x78, // DefaultSecretUInt64_4
            0x82, 0x5a, 0xd0, 0x7d, 0xcc, 0xff, 0x72, 0x21, // DefaultSecretUInt64_5
            0xb8, 0x08, 0x46, 0x74, 0xf7, 0x43, 0x24, 0x8e, // DefaultSecretUInt64_6
            0xe0, 0x35, 0x90, 0xe6, 0x81, 0x3a, 0x26, 0x4c, // DefaultSecretUInt64_7
            0x3c, 0x28, 0x52, 0xbb, 0x91, 0xc3, 0x00, 0xcb, // DefaultSecretUInt64_8
            0x88, 0xd0, 0x65, 0x8b, 0x1b, 0x53, 0x2e, 0xa3, // DefaultSecretUInt64_9
            0x71, 0x64, 0x48, 0x97, 0xa2, 0x0d, 0xf9, 0x4e, // DefaultSecretUInt64_10
            0x38, 0x19, 0xef, 0x46, 0xa9, 0xde, 0xac, 0xd8, // DefaultSecretUInt64_11
            0xa8, 0xfa, 0x76, 0x3f, 0xe3, 0x9c, 0x34, 0x3f, // DefaultSecretUInt64_12
            0xf9, 0xdc, 0xbb, 0xc7, 0xc7, 0x0b, 0x4f, 0x1d, // DefaultSecretUInt64_13
            0x8a, 0x51, 0xe0, 0x4b, 0xcd, 0xb4, 0x59, 0x31, // DefaultSecretUInt64_14
            0xc8, 0x9f, 0x7e, 0xc9, 0xd9, 0x78, 0x73, 0x64, // DefaultSecretUInt64_15
            0xea, 0xc5, 0xac, 0x83, 0x34, 0xd3, 0xeb, 0xc3, // DefaultSecretUInt64_16
            0xc5, 0x81, 0xa0, 0xff, 0xfa, 0x13, 0x63, 0xeb, // DefaultSecretUInt64_17
            0x17, 0x0d, 0xdd, 0x51, 0xb7, 0xf0, 0xda, 0x49, // DefaultSecretUInt64_18
            0xd3, 0x16, 0x55, 0x26, 0x29, 0xd4, 0x68, 0x9e, // DefaultSecretUInt64_19
            0x2b, 0x16, 0xbe, 0x58, 0x7d, 0x47, 0xa1, 0xfc, // DefaultSecretUInt64_20
            0x8f, 0xf8, 0xb8, 0xd1, 0x7a, 0xd0, 0x31, 0xce, // DefaultSecretUInt64_21
            0x45, 0xcb, 0x3a, 0x8f, 0x95, 0x16, 0x04, 0x28, // DefaultSecretUInt64_22
            0xaf, 0xd7, 0xfb, 0xca, 0xbb, 0x4b, 0x40, 0x7e, // DefaultSecretUInt64_23
        ];

    /// <summary>Computes the XXH3 hash of the provided data.</summary>
    /// <param name="source">The data to hash.</param>
    /// <param name="seed">The seed value for this hash computation.</param>
    /// <returns>The computed XXH3 hash.</returns>
    public static unsafe ulong Hash64(ReadOnlySpan<byte> source, long seed = 0)
    {
        uint length = (uint)source.Length;
        fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
        {
            if (length <= 16)
            {
                return HashLength0To16(sourcePtr, length, (ulong)seed);
            }

            if (length <= 128)
            {
                return HashLength17To128(sourcePtr, length, (ulong)seed);
            }

            if (length <= MidSizeMaxBytes)
            {
                return HashLength129To240(sourcePtr, length, (ulong)seed);
            }

            return HashLengthOver240(sourcePtr, length, (ulong)seed);
        }
    }

    /// <summary>
    /// Static function: Calculates a 64bit hash from the given string.
    /// </summary>
    /// <param name="input">The read-only span that contains input data.</param>
    /// <returns>A 64bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash64(ReadOnlySpan<char> input)
        => Hash64(MemoryMarshal.Cast<char, byte>(input));

    /// <summary>
    /// Static function: Calculates a 64bit hash from the given string.
    /// </summary>
    /// <param name="str">The string containing the characters to calculates.</param>
    /// <returns>A 64bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash64(string str)
        => Hash64(MemoryMarshal.Cast<char, byte>(str));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong XxHash64Avalanche(ulong hash)
    {
        hash ^= hash >> 33;
        hash *= Prime64_2;
        hash ^= hash >> 29;
        hash *= Prime64_3;
        hash ^= hash >> 32;
        return hash;
    }

    private static ulong HashLength0To16(byte* source, uint length, ulong seed)
    {
        if (length > 8)
        {
            return HashLength9To16(source, length, seed);
        }

        if (length >= 4)
        {
            return HashLength4To8(source, length, seed);
        }

        if (length != 0)
        {
            return HashLength1To3(source, length, seed);
        }

        const ulong SecretXor = DefaultSecretUInt64_7 ^ DefaultSecretUInt64_8;
        return XxHash64Avalanche(seed ^ SecretXor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLength1To3(byte* source, uint length, ulong seed)
    {
        byte c1 = *source;
        byte c2 = source[length >> 1];
        byte c3 = source[length - 1];
        uint combined = ((uint)c1 << 16) | ((uint)c2 << 24) | c3 | (length << 8);
        const uint SecretXor = unchecked((uint)DefaultSecretUInt64_0) ^ (uint)(DefaultSecretUInt64_0 >> 32);
        return XxHash64Avalanche(combined ^ (SecretXor + seed));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLength4To8(byte* source, uint length, ulong seed)
    {
        seed ^= (ulong)BinaryPrimitives.ReverseEndianness((uint)seed) << 32;
        uint inputLow = ReadUInt32LE(source);
        uint inputHigh = ReadUInt32LE(source + length - sizeof(uint));
        const ulong SecretXor = DefaultSecretUInt64_1 ^ DefaultSecretUInt64_2;
        ulong bitflip = SecretXor - seed;
        ulong input64 = inputHigh + (((ulong)inputLow) << 32);
        return Rrmxmx(input64 ^ bitflip, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLength9To16(byte* source, uint length, ulong seed)
    {
        const ulong SecretXorL = DefaultSecretUInt64_3 ^ DefaultSecretUInt64_4;
        const ulong SecretXorR = DefaultSecretUInt64_5 ^ DefaultSecretUInt64_6;
        ulong bitflipLow = SecretXorL + seed;
        ulong bitflipHigh = SecretXorR - seed;

        ulong inputLow = ReadUInt64LE(source) ^ bitflipLow;
        ulong inputHigh = ReadUInt64LE(source + length - sizeof(ulong)) ^ bitflipHigh;

        return Avalanche(
            length +
            BinaryPrimitives.ReverseEndianness(inputLow) +
            inputHigh +
            Multiply64To128ThenFold(inputLow, inputHigh));
    }

    private static ulong HashLength17To128(byte* source, uint length, ulong seed)
    {
        ulong hash = length * Prime64_1;
        switch ((length - 1) / 32)
        {
            default: // case 3
                hash += Mix16Bytes(source + 48, DefaultSecretUInt64_12, DefaultSecretUInt64_13, seed);
                hash += Mix16Bytes(source + length - 64, DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);
                goto case 2;
            case 2:
                hash += Mix16Bytes(source + 32, DefaultSecretUInt64_8, DefaultSecretUInt64_9, seed);
                hash += Mix16Bytes(source + length - 48, DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
                goto case 1;
            case 1:
                hash += Mix16Bytes(source + 16, DefaultSecretUInt64_4, DefaultSecretUInt64_5, seed);
                hash += Mix16Bytes(source + length - 32, DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
                goto case 0;
            case 0:
                hash += Mix16Bytes(source, DefaultSecretUInt64_0, DefaultSecretUInt64_1, seed);
                hash += Mix16Bytes(source + length - 16, DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
                break;
        }

        return Avalanche(hash);
    }

    private static ulong HashLength129To240(byte* source, uint length, ulong seed)
    {
        ulong hash = length * Prime64_1;
        hash += Mix16Bytes(source + (16 * 0), DefaultSecretUInt64_0, DefaultSecretUInt64_1, seed);
        hash += Mix16Bytes(source + (16 * 1), DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
        hash += Mix16Bytes(source + (16 * 2), DefaultSecretUInt64_4, DefaultSecretUInt64_5, seed);
        hash += Mix16Bytes(source + (16 * 3), DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
        hash += Mix16Bytes(source + (16 * 4), DefaultSecretUInt64_8, DefaultSecretUInt64_9, seed);
        hash += Mix16Bytes(source + (16 * 5), DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
        hash += Mix16Bytes(source + (16 * 6), DefaultSecretUInt64_12, DefaultSecretUInt64_13, seed);
        hash += Mix16Bytes(source + (16 * 7), DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);
        hash = Avalanche(hash);

        switch ((length - (16 * 8)) / 16)
        {
            default: // case 7
                hash += Mix16Bytes(source + (16 * 14), DefaultSecret3UInt64_12, DefaultSecret3UInt64_13, seed);
                goto case 6;
            case 6:
                hash += Mix16Bytes(source + (16 * 13), DefaultSecret3UInt64_10, DefaultSecret3UInt64_11, seed);
                goto case 5;
            case 5:
                hash += Mix16Bytes(source + (16 * 12), DefaultSecret3UInt64_8, DefaultSecret3UInt64_9, seed);
                goto case 4;
            case 4:
                hash += Mix16Bytes(source + (16 * 11), DefaultSecret3UInt64_6, DefaultSecret3UInt64_7, seed);
                goto case 3;
            case 3:
                hash += Mix16Bytes(source + (16 * 10), DefaultSecret3UInt64_4, DefaultSecret3UInt64_5, seed);
                goto case 2;
            case 2:
                hash += Mix16Bytes(source + (16 * 9), DefaultSecret3UInt64_2, DefaultSecret3UInt64_3, seed);
                goto case 1;
            case 1:
                hash += Mix16Bytes(source + (16 * 8), DefaultSecret3UInt64_0, DefaultSecret3UInt64_1, seed);
                goto case 0;
            case 0:
                hash += Mix16Bytes(source + length - 16, 0x7378D9C97E9FC831, 0xEBD33483ACC5EA64, seed); // DefaultSecret[119], DefaultSecret[127]
                break;
        }

        return Avalanche(hash);
    }

    private static ulong HashLengthOver240(byte* source, uint length, ulong seed)
    {
        fixed (byte* defaultSecret = &MemoryMarshal.GetReference(DefaultSecret))
        {
            byte* secret = defaultSecret;
            if (seed != 0)
            {
                byte* customSecret = stackalloc byte[SecretLengthBytes];
                DeriveSecretFromSeed(customSecret, seed);
                secret = customSecret;
            }

            ulong* accumulators = stackalloc ulong[AccumulatorCount];
            InitializeAccumulators(accumulators);

            HashInternalLoop(accumulators, source, length, secret);

            return MergeAccumulators(accumulators, secret + 11, length * Prime64_1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rrmxmx(ulong hash, uint length)
    {
        hash ^= BitOperations.RotateLeft(hash, 49) ^ BitOperations.RotateLeft(hash, 24);
        hash *= 0x9FB21C651E98DF25;
        hash ^= (hash >> 35) + length;
        hash *= 0x9FB21C651E98DF25;
        return XorShift(hash, 28);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LE(byte* data) =>
        BitConverter.IsLittleEndian ?
            Unsafe.ReadUnaligned<uint>(data) :
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64LE(byte* data) =>
        BitConverter.IsLittleEndian ?
            Unsafe.ReadUnaligned<ulong>(data) :
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong XorShift(ulong value, int shift)
    {
        return value ^ (value >> shift);
    }

    private static ulong MergeAccumulators(ulong* accumulators, byte* secret, ulong start)
    {
        ulong result64 = start;

        result64 += Multiply64To128ThenFold(accumulators[0] ^ ReadUInt64LE(secret), accumulators[1] ^ ReadUInt64LE(secret + 8));
        result64 += Multiply64To128ThenFold(accumulators[2] ^ ReadUInt64LE(secret + 16), accumulators[3] ^ ReadUInt64LE(secret + 24));
        result64 += Multiply64To128ThenFold(accumulators[4] ^ ReadUInt64LE(secret + 32), accumulators[5] ^ ReadUInt64LE(secret + 40));
        result64 += Multiply64To128ThenFold(accumulators[6] ^ ReadUInt64LE(secret + 48), accumulators[7] ^ ReadUInt64LE(secret + 56));

        return Avalanche(result64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix16Bytes(byte* source, ulong secretLow, ulong secretHigh, ulong seed) =>
        Multiply64To128ThenFold(
            ReadUInt64LE(source) ^ (secretLow + seed),
            ReadUInt64LE(source + sizeof(ulong)) ^ (secretHigh - seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Multiply32To64(uint v1, uint v2) => (ulong)v1 * v2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Avalanche(ulong hash)
    {
        hash = XorShift(hash, 37);
        hash *= 0x165667919E3779F9;
        hash = XorShift(hash, 32);
        return hash;
    }

    private static ulong Multiply64To128(ulong left, ulong right, out ulong lower)
    {
        return Math.BigMul(left, right, out lower);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Multiply64To128ThenFold(ulong left, ulong right)
    {
        ulong upper = Multiply64To128(left, right, out ulong lower);
        return lower ^ upper;
    }

    private static void DeriveSecretFromSeed(byte* destinationSecret, ulong seed)
    {
        fixed (byte* defaultSecret = &MemoryMarshal.GetReference(DefaultSecret))
        {
            if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                Vector256<ulong> seedVec = Vector256.Create(seed, 0u - seed, seed, 0u - seed);
                for (int i = 0; i < SecretLengthBytes; i += Vector256<byte>.Count)
                {
                    Vector256.Store(Vector256.Load((ulong*)(defaultSecret + i)) + seedVec, (ulong*)(destinationSecret + i));
                }
            }
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                Vector128<ulong> seedVec = Vector128.Create(seed, 0u - seed);
                for (int i = 0; i < SecretLengthBytes; i += Vector128<byte>.Count)
                {
                    Vector128.Store(Vector128.Load((ulong*)(defaultSecret + i)) + seedVec, (ulong*)(destinationSecret + i));
                }
            }
            else
            {
                for (int i = 0; i < SecretLengthBytes; i += sizeof(ulong) * 2)
                {
                    WriteUInt64LE(destinationSecret + i, ReadUInt64LE(defaultSecret + i) + seed);
                    WriteUInt64LE(destinationSecret + i + 8, ReadUInt64LE(defaultSecret + i + 8) - seed);
                }
            }
        }
    }

    /// <summary>Optimized version of looping over <see cref="Accumulate512"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Accumulate(ulong* accumulators, byte* source, byte* secret, int stripesToProcess, bool scramble = false, int blockCount = 1)
    {
        byte* secretForAccumulate = secret;
        byte* secretForScramble = secret + (SecretLengthBytes - StripeLengthBytes);

        if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            Vector256<ulong> acc1 = Vector256.Load(accumulators);
            Vector256<ulong> acc2 = Vector256.Load(accumulators + Vector256<ulong>.Count);

            for (int j = 0; j < blockCount; j++)
            {
                secret = secretForAccumulate;
                for (int i = 0; i < stripesToProcess; i++)
                {
                    Vector256<uint> secretVal = Vector256.Load((uint*)secret);
                    acc1 = Accumulate256(acc1, source, secretVal);
                    source += Vector256<byte>.Count;

                    secretVal = Vector256.Load((uint*)secret + Vector256<uint>.Count);
                    acc2 = Accumulate256(acc2, source, secretVal);
                    source += Vector256<byte>.Count;

                    secret += SecretConsumeRateBytes;
                }

                if (scramble)
                {
                    acc1 = ScrambleAccumulator256(acc1, Vector256.Load((ulong*)secretForScramble));
                    acc2 = ScrambleAccumulator256(acc2, Vector256.Load((ulong*)secretForScramble + Vector256<ulong>.Count));
                }
            }

            Vector256.Store(acc1, accumulators);
            Vector256.Store(acc2, accumulators + Vector256<ulong>.Count);
        }
        else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            Vector128<ulong> acc1 = Vector128.Load(accumulators);
            Vector128<ulong> acc2 = Vector128.Load(accumulators + Vector128<ulong>.Count);
            Vector128<ulong> acc3 = Vector128.Load(accumulators + (Vector128<ulong>.Count * 2));
            Vector128<ulong> acc4 = Vector128.Load(accumulators + (Vector128<ulong>.Count * 3));

            for (int j = 0; j < blockCount; j++)
            {
                secret = secretForAccumulate;
                for (int i = 0; i < stripesToProcess; i++)
                {
                    Vector128<uint> secretVal = Vector128.Load((uint*)secret);
                    acc1 = Accumulate128(acc1, source, secretVal);
                    source += Vector128<byte>.Count;

                    secretVal = Vector128.Load((uint*)secret + Vector128<uint>.Count);
                    acc2 = Accumulate128(acc2, source, secretVal);
                    source += Vector128<byte>.Count;

                    secretVal = Vector128.Load((uint*)secret + (Vector128<uint>.Count * 2));
                    acc3 = Accumulate128(acc3, source, secretVal);
                    source += Vector128<byte>.Count;

                    secretVal = Vector128.Load((uint*)secret + (Vector128<uint>.Count * 3));
                    acc4 = Accumulate128(acc4, source, secretVal);
                    source += Vector128<byte>.Count;

                    secret += SecretConsumeRateBytes;
                }

                if (scramble)
                {
                    acc1 = ScrambleAccumulator128(acc1, Vector128.Load((ulong*)secretForScramble));
                    acc2 = ScrambleAccumulator128(acc2, Vector128.Load((ulong*)secretForScramble + Vector128<ulong>.Count));
                    acc3 = ScrambleAccumulator128(acc3, Vector128.Load((ulong*)secretForScramble + (Vector128<ulong>.Count * 2)));
                    acc4 = ScrambleAccumulator128(acc4, Vector128.Load((ulong*)secretForScramble + (Vector128<ulong>.Count * 3)));
                }
            }

            Vector128.Store(acc1, accumulators);
            Vector128.Store(acc2, accumulators + Vector128<ulong>.Count);
            Vector128.Store(acc3, accumulators + (Vector128<ulong>.Count * 2));
            Vector128.Store(acc4, accumulators + (Vector128<ulong>.Count * 3));
        }
        else
        {
            for (int j = 0; j < blockCount; j++)
            {
                for (int i = 0; i < stripesToProcess; i++)
                {
                    Accumulate512Inlined(accumulators, source, secret + (i * SecretConsumeRateBytes));
                    source += StripeLengthBytes;
                }

                if (scramble)
                {
                    ScrambleAccumulators(accumulators, secretForScramble);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Accumulate512(ulong* accumulators, byte* source, byte* secret)
    {
        Accumulate512Inlined(accumulators, source, secret);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Accumulate512Inlined(ulong* accumulators, byte* source, byte* secret)
    {
        if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < AccumulatorCount / Vector256<ulong>.Count; i++)
            {
                Vector256<ulong> accVec = Accumulate256(Vector256.Load(accumulators), source, Vector256.Load((uint*)secret));
                Vector256.Store(accVec, accumulators);

                accumulators += Vector256<ulong>.Count;
                secret += Vector256<byte>.Count;
                source += Vector256<byte>.Count;
            }
        }
        else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < AccumulatorCount / Vector128<ulong>.Count; i++)
            {
                Vector128<ulong> accVec = Accumulate128(Vector128.Load(accumulators), source, Vector128.Load((uint*)secret));
                Vector128.Store(accVec, accumulators);

                accumulators += Vector128<ulong>.Count;
                secret += Vector128<byte>.Count;
                source += Vector128<byte>.Count;
            }
        }
        else
        {
            for (int i = 0; i < AccumulatorCount; i++)
            {
                ulong sourceVal = ReadUInt64LE(source + (8 * i));
                ulong sourceKey = sourceVal ^ ReadUInt64LE(secret + (i * 8));

                accumulators[i ^ 1] += sourceVal; // swap adjacent lanes
                accumulators[i] += Multiply32To64((uint)sourceKey, (uint)(sourceKey >> 32));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Accumulate256(Vector256<ulong> accVec, byte* source, Vector256<uint> secret)
    {
        Vector256<uint> sourceVec = Vector256.Load((uint*)source);
        Vector256<uint> sourceKey = sourceVec ^ secret;

        // TODO: Figure out how to unwind this shuffle and just use Vector256.Multiply
        Vector256<uint> sourceKeyLow = Vector256.Shuffle(sourceKey, Vector256.Create(1u, 0, 3, 0, 5, 0, 7, 0));
        Vector256<uint> sourceSwap = Vector256.Shuffle(sourceVec, Vector256.Create(2u, 3, 0, 1, 6, 7, 4, 5));
        Vector256<ulong> sum = accVec + sourceSwap.AsUInt64();
        Vector256<ulong> product = Avx2.IsSupported ?
            Avx2.Multiply(sourceKey, sourceKeyLow) :
            (sourceKey & Vector256.Create(~0u, 0u, ~0u, 0u, ~0u, 0u, ~0u, 0u)).AsUInt64() * (sourceKeyLow & Vector256.Create(~0u, 0u, ~0u, 0u, ~0u, 0u, ~0u, 0u)).AsUInt64();

        accVec = product + sum;
        return accVec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ulong> Accumulate128(Vector128<ulong> accVec, byte* source, Vector128<uint> secret)
    {
        Vector128<uint> sourceVec = Vector128.Load((uint*)source);
        Vector128<uint> sourceKey = sourceVec ^ secret;

        // TODO: Figure out how to unwind this shuffle and just use Vector128.Multiply
        Vector128<uint> sourceSwap = Vector128.Shuffle(sourceVec, Vector128.Create(2u, 3, 0, 1));
        Vector128<ulong> sum = accVec + sourceSwap.AsUInt64();

        Vector128<ulong> product = MultiplyWideningLower(sourceKey);
        accVec = product + sum;
        return accVec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ulong> MultiplyWideningLower(Vector128<uint> source)
    {
        if (AdvSimd.IsSupported)
        {
            Vector64<uint> sourceLow = Vector128.Shuffle(source, Vector128.Create(0u, 2, 0, 0)).GetLower();
            Vector64<uint> sourceHigh = Vector128.Shuffle(source, Vector128.Create(1u, 3, 0, 0)).GetLower();
            return AdvSimd.MultiplyWideningLower(sourceLow, sourceHigh);
        }
        else
        {
            Vector128<uint> sourceLow = Vector128.Shuffle(source, Vector128.Create(1u, 0, 3, 0));
            return Sse2.IsSupported ?
                Sse2.Multiply(source, sourceLow) :
                (source & Vector128.Create(~0u, 0u, ~0u, 0u)).AsUInt64() * (sourceLow & Vector128.Create(~0u, 0u, ~0u, 0u)).AsUInt64();
        }
    }

    private static void ScrambleAccumulators(ulong* accumulators, byte* secret)
    {
        if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < AccumulatorCount / Vector256<ulong>.Count; i++)
            {
                Vector256<ulong> accVec = ScrambleAccumulator256(Vector256.Load(accumulators), Vector256.Load((ulong*)secret));
                Vector256.Store(accVec, accumulators);

                accumulators += Vector256<ulong>.Count;
                secret += Vector256<byte>.Count;
            }
        }
        else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < AccumulatorCount / Vector128<ulong>.Count; i++)
            {
                Vector128<ulong> accVec = ScrambleAccumulator128(Vector128.Load(accumulators), Vector128.Load((ulong*)secret));
                Vector128.Store(accVec, accumulators);

                accumulators += Vector128<ulong>.Count;
                secret += Vector128<byte>.Count;
            }
        }
        else
        {
            for (int i = 0; i < AccumulatorCount; i++)
            {
                ulong xorShift = XorShift(*accumulators, 47);
                ulong xorWithKey = xorShift ^ ReadUInt64LE(secret);
                *accumulators = xorWithKey * Prime32_1;

                accumulators++;
                secret += sizeof(ulong);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> ScrambleAccumulator256(Vector256<ulong> accVec, Vector256<ulong> secret)
    {
        Vector256<ulong> xorShift = accVec ^ Vector256.ShiftRightLogical(accVec, 47);
        Vector256<ulong> xorWithKey = xorShift ^ secret;
        accVec = xorWithKey * Vector256.Create((ulong)Prime32_1);
        return accVec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ulong> ScrambleAccumulator128(Vector128<ulong> accVec, Vector128<ulong> secret)
    {
        Vector128<ulong> xorShift = accVec ^ Vector128.ShiftRightLogical(accVec, 47);
        Vector128<ulong> xorWithKey = xorShift ^ secret;
        accVec = xorWithKey * Vector128.Create((ulong)Prime32_1);
        return accVec;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt64LE(byte* data, ulong value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeAccumulators(ulong* accumulators)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256.Store(Vector256.Create(Prime32_3, Prime64_1, Prime64_2, Prime64_3), accumulators);
            Vector256.Store(Vector256.Create(Prime64_4, Prime32_2, Prime64_5, Prime32_1), accumulators + 4);
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            Vector128.Store(Vector128.Create(Prime32_3, Prime64_1), accumulators);
            Vector128.Store(Vector128.Create(Prime64_2, Prime64_3), accumulators + 2);
            Vector128.Store(Vector128.Create(Prime64_4, Prime32_2), accumulators + 4);
            Vector128.Store(Vector128.Create(Prime64_5, Prime32_1), accumulators + 6);
        }
        else
        {
            accumulators[0] = Prime32_3;
            accumulators[1] = Prime64_1;
            accumulators[2] = Prime64_2;
            accumulators[3] = Prime64_3;
            accumulators[4] = Prime64_4;
            accumulators[5] = Prime32_2;
            accumulators[6] = Prime64_5;
            accumulators[7] = Prime32_1;
        }
    }

    private static void HashInternalLoop(ulong* accumulators, byte* source, uint length, byte* secret)
    {
        const int StripesPerBlock = (SecretLengthBytes - StripeLengthBytes) / SecretConsumeRateBytes;
        const int BlockLen = StripeLengthBytes * StripesPerBlock;
        int blocksNum = (int)((length - 1) / BlockLen);

        Accumulate(accumulators, source, secret, StripesPerBlock, true, blocksNum);
        int offset = BlockLen * blocksNum;

        int stripesNumber = (int)((length - 1 - offset) / StripeLengthBytes);
        Accumulate(accumulators, source + offset, secret, stripesNumber);
        Accumulate512(accumulators, source + length - StripeLengthBytes, secret + (SecretLengthBytes - StripeLengthBytes - SecretLastAccStartBytes));
    }
}
