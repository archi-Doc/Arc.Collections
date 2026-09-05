using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Arc.Collections.HotMethod;
using Xunit;

namespace XunitTest;

public class PrimitiveOrderingCoverageTest
{
    [Fact]
    public void ByteMatchesDefaultComparer() => Verify(new byte[] { 0, 1, 2, 2, byte.MaxValue });

    [Fact]
    public void SByteMatchesDefaultComparer() => Verify(new sbyte[] { sbyte.MinValue, -1, 0, 1, 1, sbyte.MaxValue });

    [Fact]
    public void UInt16MatchesDefaultComparer() => Verify(new ushort[] { 0, 1, 2, 2, ushort.MaxValue });

    [Fact]
    public void Int16MatchesDefaultComparer() => Verify(new short[] { short.MinValue, -1, 0, 1, 1, short.MaxValue });

    [Fact]
    public void UInt32MatchesDefaultComparer() => Verify(new uint[] { 0, 1, 2, 2, uint.MaxValue });

    [Fact]
    public void Int32MatchesDefaultComparer() => Verify(new int[] { int.MinValue, -1, 0, 1, 1, int.MaxValue });

    [Fact]
    public void UInt64MatchesDefaultComparer() => Verify(new ulong[] { 0, 1, 2, 2, ulong.MaxValue });

    [Fact]
    public void Int64MatchesDefaultComparer() => Verify(new long[] { long.MinValue, -1, 0, 1, 1, long.MaxValue });

    [Fact]
    public void UInt128MatchesDefaultComparer() => Verify(new UInt128[] { 0, 1, 2, 2, UInt128.MaxValue });

    [Fact]
    public void Int128MatchesDefaultComparer() => Verify(new Int128[] { Int128.MinValue, -1, 0, 1, 1, Int128.MaxValue });

    [Fact]
    public void SingleMatchesDefaultComparer() => Verify(new float[] { float.NaN, float.NegativeInfinity, -1, -0f, 0f, 1, float.PositiveInfinity, float.NaN });

    [Fact]
    public void DoubleMatchesDefaultComparer() => Verify(new double[] { double.NaN, double.NegativeInfinity, -1, -0d, 0d, 1, double.PositiveInfinity, double.NaN });

    [Fact]
    public void DateTimeMatchesDefaultComparer() => Verify(new DateTime[] { DateTime.MinValue, new DateTime(1), new DateTime(1), new DateTime(2), DateTime.MaxValue });

    private static void Verify<T>(T[] input)
        where T : notnull
    {
        var comparer = Comparer<T>.Default;
        var sorted = input.OrderBy(x => x, comparer).ToArray();
        var hot = HotMethodResolver.Get(comparer);
        Assert.NotNull(hot);
        foreach (var key in input)
        {
            Assert.Equal(0, hot.LowerBound(ReadOnlySpan<T>.Empty, key));
            Assert.Equal(0, hot.UpperBoundExclusive(ReadOnlySpan<T>.Empty, key));
            Assert.Equal(sorted.Count(x => comparer.Compare(x, key) < 0), hot.LowerBound(sorted, key));
            Assert.Equal(sorted.Count(x => comparer.Compare(x, key) <= 0), hot.UpperBoundExclusive(sorted, key));
        }

        foreach (var reverse in new[] { false, true })
        {
            var map = new OrderedMap<T, int>(reverse);
            var multi = new OrderedMultiMap<T, int>(reverse);
            var reference = new SortedDictionary<T, int>(Comparer<T>.Create((x, y) =>
                reverse ? comparer.Compare(y, x) : comparer.Compare(x, y)));
            // Alternating insertion order exercises both sides of the search tree.
            var shuffled = input.Where((_, i) => i % 2 == 0).Concat(input.Where((_, i) => i % 2 == 1).Reverse()).ToArray();
            for (var i = 0; i < shuffled.Length; i++)
            {
                var key = shuffled[i];
                var added = reference.TryAdd(key, i);
                Assert.Equal(added, map.Add(key, i).NewlyAdded);
                multi.Add(key, i);
                Assert.True(map.Validate());
                Assert.True(multi.Validate());
            }

            Assert.Equal(reference.Keys, map.Keys);
            var expectedMulti = shuffled.Select((key, i) => KeyValuePair.Create(key, i))
                .OrderBy(x => x.Key, reference.Comparer).ToArray();
            Assert.Equal(expectedMulti, multi);
            foreach (var pair in reference)
            {
                Assert.True(map.TryGetValue(pair.Key, out var value));
                Assert.Equal(pair.Value, value);
                Assert.Equal(expectedMulti.Where(x => comparer.Compare(x.Key, pair.Key) == 0).Select(x => x.Value),
                    multi.EnumerateValue(pair.Key));
            }

            foreach (var key in input)
            {
                Assert.Equal(reference.Remove(key), map.Remove(key));
                Assert.True(multi.Remove(key));
                Assert.True(map.Validate());
                Assert.True(multi.Validate());
            }

            Assert.Empty(map);
            Assert.Empty(multi);
        }
    }
}
