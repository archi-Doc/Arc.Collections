// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Arc.Collections;

public static class CollectionHelper
{
    public const uint MinimumCapacity = 8;
    public const uint MaximumCapacity = 0x80000000;

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

    public const int MaxPrimeArrayLength = 0x7FEFFFFD;
    public static readonly int[] Primes =
    {
        3, 5, 7, 11, 17, 29, 43, 67, 101, 151, 227, 347, 521, 787, 1181, 1777, 2671, 4007,
        6011, 9029, 13553, 20333, 30509, 45763, 68659, 103001, 154501, 231779, 347671,
        521519, 782297, 1173463, 1760203, 2640317, 3960497, 5940761, 8911141, 13366711,
        20050081, 30075127, 45112693, 67669079, 101503627, 152255461, 228383273,
        342574909, 513862367, 770793589, 1156190419, 1734285653,
    };

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
