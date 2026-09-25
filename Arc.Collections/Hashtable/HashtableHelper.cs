// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Arc.Collections;

internal static class HashtableHelper
{
    /// <summary>
    /// The largest initial capacity: the tables are kept at most half full, and hold at most 2^30 buckets.
    /// </summary>
    public const int MaximumInitialCapacity = 1 << 29;

    /// <summary>
    /// Gets the bucket count for an initial capacity: twice the capacity, rounded up to a power of two (at least 8).
    /// </summary>
    /// <param name="capacity">The initial capacity, between 0 and <see cref="MaximumInitialCapacity"/>.</param>
    /// <returns>The bucket count.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative or greater than <see cref="MaximumInitialCapacity"/>.</exception>
    public static int CalculateCapacity(int capacity)
    {
        if ((uint)capacity > MaximumInitialCapacity)
        {
            ThrowCapacityOutOfRange(capacity);
        }

        return (int)CollectionHelper.CalculatePowerOfTwoCapacity((uint)capacity * 2);
    }

    /// <summary>
    /// Throws when a <c>GetOrAdd</c> value factory has modified the hashtable (the lock is reentrant, so it cannot be blocked).
    /// </summary>
    [DoesNotReturn]
    public static void ThrowFactoryModifiedTable()
        => throw new InvalidOperationException("The value factory must not modify the hashtable.");

    [DoesNotReturn]
    private static void ThrowCapacityOutOfRange(int capacity)
        => throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"The capacity must be between 0 and {MaximumInitialCapacity}.");
}
