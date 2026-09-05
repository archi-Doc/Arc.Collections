using System;
using System.Linq;
using Arc;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class HelperCoverageTest
{
    [Theory]
    [InlineData(CollectionHelper.MaxPrimeArrayLength)]
    [InlineData(int.MaxValue)]
    public void PrimeExpansionNeverWrapsAtMaximum(int size)
        => Assert.Equal(CollectionHelper.MaxPrimeArrayLength, CollectionHelper.ExpandPrime(size));

    [Fact]
    public void CapacityHelpersRespectBounds()
    {
        Assert.Equal(8, CollectionHelper.CalculatePowerOfTwoCapacity(-1));
        Assert.Equal(8u, CollectionHelper.CalculatePowerOfTwoCapacity(0u));
        Assert.Equal(16, CollectionHelper.CalculatePowerOfTwoCapacity(9));
        Assert.Equal(16u, CollectionHelper.CalculatePowerOfTwoCapacity(9u));
        Assert.Equal((int)CollectionHelper.MaximumCapacity, CollectionHelper.CalculatePowerOfTwoCapacity(int.MaxValue));
        Assert.Equal(CollectionHelper.MaximumCapacity, CollectionHelper.CalculatePowerOfTwoCapacity(uint.MaxValue));
        Assert.Equal(3, CollectionHelper.GetPrime(0));
        Assert.Equal(7, CollectionHelper.GetPrime(6));
        Assert.Equal(CollectionHelper.Primes[^1], CollectionHelper.GetPrime(int.MaxValue));
        Assert.Equal(11, CollectionHelper.ExpandPrime(5));
        Assert.Equal(CollectionHelper.MaxPrimeArrayLength, CollectionHelper.ExpandPrime(1_500_000_000));
    }

    [Fact]
    public void TagsRoundTripUsingCachedObjects()
    {
        for (var tag = 0; tag < TagObject.MaxTag; tag++)
        {
            var value = TagObject.FromTag(tag);
            Assert.Same(value, TagObject.FromTag(tag));
            Assert.Equal(tag, value.Tag);
            Assert.Equal(tag, TagObject.ToTag(value));
        }

        Assert.Equal(TagObject.InvalidTag, TagObject.ToTag(null));
        Assert.Equal(TagObject.InvalidTag, TagObject.ToTag(1));
        Assert.Throws<IndexOutOfRangeException>(() => TagObject.FromTag(-1));
        Assert.Throws<IndexOutOfRangeException>(() => TagObject.FromTag(TagObject.MaxTag));
    }

    [Fact]
    public void FixedSizeStructsRoundTripWithoutTouchingTrailingBytes()
    {
        var input = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var small = new Struct128(input);
        var large = new Struct256(input);
        var destination = Enumerable.Repeat((byte)255, 40).ToArray();
        Assert.True(small.TryWriteBytes(destination));
        Assert.Equal(input.Take(16), destination.Take(16));
        Assert.All(destination.Skip(16), b => Assert.Equal((byte)255, b));
        Assert.True(large.TryWriteBytes(destination));
        Assert.Equal(input, destination.Take(32));
        Assert.All(destination.Skip(32), b => Assert.Equal((byte)255, b));
        Assert.Equal(input.Take(16), small.AsSpan().ToArray());
        Assert.Equal(input, large.AsSpan().ToArray());
        Assert.False(small.TryWriteBytes(new byte[15]));
        Assert.False(large.TryWriteBytes(new byte[31]));
        Assert.Throws<ArgumentException>(() => new Struct128(new byte[15]));
        Assert.Throws<ArgumentException>(() => new Struct256(new byte[31]));
        Assert.Equal(small, new Struct128(ref small));
        Assert.Equal(large, new Struct256(ref large));
        Assert.Equal(small.GetHashCode(), new Struct128(input).GetHashCode());
        Assert.Equal(large.GetHashCode(), new Struct256(input).GetHashCode());
        Assert.True(small.Equals((object)new Struct128(input)));
        Assert.True(large.Equals((object)new Struct256(input)));
        Assert.False(small.Equals((object)large));
        Assert.False(large.Equals((object)small));
        Assert.True(new Struct128(1L) == Struct128.One);
        Assert.True(new Struct256(1L) == Struct256.One);
        Assert.True(small != Struct128.Zero);
        Assert.True(large != Struct256.Zero);
        Assert.Equal("Struct128: 1", Struct128.One.ToString());
        Assert.Equal("Struct256: 1", Struct256.One.ToString());
        Assert.Equal("Struct128: " + small.UInt128.ToString("X32"), small.ToString());
        Assert.Equal("Struct256: " + large.UInt128Upper.ToString("X32") + large.UInt128Lower.ToString("X32"), large.ToString());
    }

    [Fact]
    public void FixedSizeStructComparisonUsesEverySignedWord()
    {
        foreach (var sign in new[] { -1L, 1L })
        {
            for (var field = 0; field < 4; field++)
            {
                var words = new long[4];
                words[field] = sign;
                var value = new Struct256(words[0], words[1], words[2], words[3]);
                Assert.Equal(sign, value.CompareTo(Struct256.Zero));
                Assert.False(value.IsZero);
                if (field < 2)
                {
                    var small = new Struct128(words[0], words[1]);
                    Assert.Equal(sign, small.CompareTo(Struct128.Zero));
                    Assert.False(small.IsZero);
                }
            }
        }

        Assert.True(Struct128.Zero.IsZero);
        Assert.True(Struct256.Zero.IsZero);
        Assert.Equal(0, Struct128.Zero.CompareTo(default));
        Assert.Equal(0, Struct256.Zero.CompareTo(default));
    }
}
