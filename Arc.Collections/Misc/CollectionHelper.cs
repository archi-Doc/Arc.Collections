// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Numerics;

namespace Arc.Collections;

/// <summary>
/// Provides helper methods for sizing collection storage.
/// </summary>
public static class CollectionHelper
{
    /// <summary>
    /// The smallest power-of-two capacity returned by <see cref="CalculatePowerOfTwoCapacity(uint)"/>.
    /// </summary>
    public const uint MinimumCapacity = 8;

    /// <summary>
    /// The largest power-of-two capacity returned by <see cref="CalculatePowerOfTwoCapacity(uint)"/>.
    /// </summary>
    public const uint MaximumCapacity = 1u << 30;

    /*public static T? GetOption<T>(this IConversionOptions conversionOptions)
        where T : class
        => conversionOptions.GetOption(typeof(T)) as T;*/

    /// <summary>
    /// Calculates the next power-of-two capacity that is greater than or equal to the specified minimum size.
    /// </summary>
    /// <param name="minimumSize">The minimum required capacity.</param>
    /// <returns>
    /// A power-of-two value that is greater than or equal to <paramref name="minimumSize"/>,
    /// clamped between <see cref="MinimumCapacity"/> and <see cref="MaximumCapacity"/>.
    /// </returns>
    /// <remarks>
    /// This method ensures the returned capacity is always a power of two, which is optimal
    /// for hash-based collections and memory allocation patterns.
    /// </remarks>
    public static uint CalculatePowerOfTwoCapacity(uint minimumSize)
    {
        if (minimumSize < MinimumCapacity)
        {
            return MinimumCapacity;
        }
        else if (minimumSize >= MaximumCapacity)
        {
            return MaximumCapacity;
        }

        return 1u << (32 - BitOperations.LeadingZeroCount(minimumSize - 1));
    }

    /// <summary>
    /// Calculates the next power-of-two capacity that is greater than or equal to the specified minimum size.
    /// </summary>
    /// <param name="minimumSize">The minimum required capacity.</param>
    /// <returns>
    /// A power-of-two value that is greater than or equal to <paramref name="minimumSize"/>,
    /// with a minimum value of <see cref="MinimumCapacity"/>.
    /// </returns>
    /// <remarks>
    /// This method ensures the returned capacity is always a power of two, which is optimal
    /// for hash-based collections and memory allocation patterns.
    /// </remarks>
    public static int CalculatePowerOfTwoCapacity(int minimumSize)
    {
        if (minimumSize < MinimumCapacity)
        {
            return (int)MinimumCapacity;
        }
        else if (minimumSize >= MaximumCapacity)
        {
            return (int)MaximumCapacity;
        }

        return 1 << (32 - BitOperations.LeadingZeroCount((uint)minimumSize - 1));
    }

    /// <summary>
    /// The largest array length that <see cref="ExpandPrime(int)"/> can return.
    /// </summary>
    public const int MaxPrimeArrayLength = 0x7FEFFFFD;

    /// <summary>
    /// A table of prime numbers used to size prime-bucketed hash tables.
    /// </summary>
    public static readonly int[] Primes =
    {
        3, 5, 7, 11, 17, 29, 43, 67, 101, 151, 227, 347, 521, 787, 1181, 1777, 2671, 4007,
        6011, 9029, 13553, 20333, 30509, 45763, 68659, 103001, 154501, 231779, 347671,
        521519, 782297, 1173463, 1760203, 2640317, 3960497, 5940761, 8911141, 13366711,
        20050081, 30075127, 45112693, 67669079, 101503627, 152255461, 228383273,
        342574909, 513862367, 770793589, 1156190419, 1734285653,
    };

    /// <summary>
    /// Gets the smallest prime in <see cref="Primes"/> that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="min">The minimum required value.</param>
    /// <returns>The matching prime, or the largest entry of <see cref="Primes"/> if none is large enough.</returns>
    public static int GetPrime(int min)
    {
        for (var i = 0; i < Primes.Length; i++)
        {
            if (Primes[i] >= min)
            {
                return Primes[i];
            }
        }

        return Primes[Primes.Length - 1];
    }

    /// <summary>
    /// Gets the prime capacity to grow to, roughly doubling the specified size.
    /// </summary>
    /// <param name="oldSize">The current size.</param>
    /// <returns>The new capacity, capped at <see cref="MaxPrimeArrayLength"/>.</returns>
    public static int ExpandPrime(int oldSize)
    {
        int newSize = 2 * oldSize;
        if ((uint)newSize > MaxPrimeArrayLength && oldSize < MaxPrimeArrayLength)
        {
            return MaxPrimeArrayLength;
        }

        return GetPrime(newSize);
    }
}
