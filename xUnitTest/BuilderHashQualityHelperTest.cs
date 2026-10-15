using System.Linq;
using System.Numerics;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class BuilderHashQualityHelperTest
{
    [Fact]
    public void PowerOfTwoCapacityIsSmallestPowerOfTwoWithinLimits()
    {
        uint[] values =
        [
            0, 1, 7, 8, 9, 15, 16, 17, 1000, 4096, 4097,
            (1u << 29) - 1, 1u << 29, (1u << 29) + 1, (1u << 30) - 1, 1u << 30, (1u << 30) + 1,
            int.MaxValue, (uint)int.MaxValue + 1, uint.MaxValue,
        ];

        foreach (var value in values)
        {
            var capacity = CollectionHelper.CalculatePowerOfTwoCapacity(value);
            Assert.True(BitOperations.IsPow2(capacity));
            Assert.InRange(capacity, CollectionHelper.MinimumCapacity, CollectionHelper.MaximumCapacity);
            if (value <= CollectionHelper.MaximumCapacity)
            {
                Assert.True(capacity >= value);
                Assert.True(capacity == CollectionHelper.MinimumCapacity || capacity / 2 < value);
            }

            if (value <= int.MaxValue)
            {
                Assert.Equal((int)capacity, CollectionHelper.CalculatePowerOfTwoCapacity((int)value));
            }
        }

        Assert.Equal((int)CollectionHelper.MinimumCapacity, CollectionHelper.CalculatePowerOfTwoCapacity(int.MinValue));
    }

    [Fact]
    public void PrimeTableIsAscendingPrimesAndExpandPrimeAlwaysGrows()
    {
        var primes = CollectionHelper.Primes;
        for (var i = 0; i < primes.Length; i++)
        {
            Assert.True(IsPrime(primes[i]), $"{primes[i]} is not prime.");
            Assert.True(i == 0 || primes[i - 1] < primes[i]);
            Assert.Equal(primes[i], CollectionHelper.GetPrime(primes[i]));
            Assert.Equal(primes[i], CollectionHelper.GetPrime(i == 0 ? int.MinValue : primes[i - 1] + 1));
        }

        Assert.True(primes[^1] <= CollectionHelper.MaxPrimeArrayLength);
        for (long size = 0; size < CollectionHelper.MaxPrimeArrayLength; size = (size * 3 / 2) + 1)
        {
            var expanded = CollectionHelper.ExpandPrime((int)size);
            Assert.True(expanded > size, $"ExpandPrime({size}) returned {expanded}.");
            Assert.True(expanded == CollectionHelper.MaxPrimeArrayLength || primes.Contains(expanded));

            // The documented result for a non-negative size.
            var doubled = size * 2;
            Assert.Equal(doubled > CollectionHelper.MaxPrimeArrayLength ? CollectionHelper.MaxPrimeArrayLength : CollectionHelper.GetPrime((int)doubled), expanded);
        }

        Assert.Equal(CollectionHelper.MaxPrimeArrayLength, CollectionHelper.ExpandPrime(CollectionHelper.MaxPrimeArrayLength));
        Assert.Equal(CollectionHelper.MaxPrimeArrayLength, CollectionHelper.ExpandPrime((CollectionHelper.MaxPrimeArrayLength / 2) + 1));
        Assert.Equal(CollectionHelper.MaxPrimeArrayLength, CollectionHelper.ExpandPrime(int.MaxValue));
    }

    private static bool IsPrime(int value)
    {
        if (value < 2)
        {
            return false;
        }

        for (var divisor = 2; (long)divisor * divisor <= value; divisor++)
        {
            if (value % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
