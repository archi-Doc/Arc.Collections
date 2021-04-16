// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Arc.Collection
{
    public static class PrimeHelper
    {
        public const int MaxPrimeArrayLength = 0x7FEFFFFD;
        public static readonly int[] Primes =
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369,
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

            return 7199369;
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
}
