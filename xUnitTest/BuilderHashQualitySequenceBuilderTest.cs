using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class BuilderHashQualitySequenceBuilderTest
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)] // int contains no references.
    public void DisposeClearsValueArraysOnlyWhenRequested(bool? clearArrayOnReturn, bool expectCleared)
    {
        var builder = new SequenceBuilder<int>(16, clearArrayOnReturn);
        List<ArraySegment<int>> chunks;
        try
        {
            for (var i = 1; i <= 40; i++)
            {
                builder.Add(i);
            }

            chunks = GetChunks(builder.ToReadOnlySequence());
            Assert.Equal(2, chunks.Count);
        }
        finally
        {
            builder.Dispose();
        }

        // Disposal returned the arrays to the pool; inspect them without writing.
        foreach (var chunk in chunks)
        {
            Assert.Equal(expectCleared, chunk.All(x => x == 0));
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, true)] // string is a reference type.
    public void DisposeClearsReferenceArraysUnlessDisabled(bool? clearArrayOnReturn, bool expectCleared)
    {
        var builder = new SequenceBuilder<string>(16, clearArrayOnReturn);
        List<ArraySegment<string>> chunks;
        try
        {
            for (var i = 1; i <= 40; i++)
            {
                builder.Add(i.ToString());
            }

            chunks = GetChunks(builder.ToReadOnlySequence());
            Assert.Equal(2, chunks.Count);
        }
        finally
        {
            builder.Dispose();
        }

        foreach (var chunk in chunks)
        {
            Assert.Equal(expectCleared, chunk.All(x => x is null));
        }
    }

    [Fact]
    public void AddRangeAndAddThrowAfterFinalizationOrDisposal()
    {
        var builder = new SequenceBuilder<int>();
        try
        {
            builder.Add(1);
            var sequence = builder.ToReadOnlySequence();
            Assert.True(AddRangeThrows(ref builder, [2, 3]));
            Assert.True(AddRangeThrows(ref builder, ReadOnlySpan<int>.Empty));
            Assert.Equal(new[] { 1 }, sequence.ToArray());
            Assert.Equal(1, builder.Length);
        }
        finally
        {
            builder.Dispose();
        }

        Assert.True(AddThrows(ref builder, 4));
        Assert.True(AddRangeThrows(ref builder, [5]));
        Assert.Equal(0, builder.Length);
        Assert.True(builder.ToReadOnlySequence().IsEmpty);
    }

    [Fact]
    public void SegmentsHaveConsistentRunningIndexes()
    {
        var expected = Enumerable.Range(0, 1000).ToArray();
        var builder = new SequenceBuilder<int>(4);
        try
        {
            var random = new Random(318);
            var index = 0;
            while (index < expected.Length)
            {
                if (random.Next(2) == 0)
                {
                    builder.Add(expected[index++]);
                }
                else
                {
                    var count = Math.Min(random.Next(0, 50), expected.Length - index);
                    builder.AddRange(expected.AsSpan(index, count));
                    index += count;
                }
            }

            var sequence = builder.ToReadOnlySequence();
            Assert.Equal(expected.Length, sequence.Length);

            long runningIndex = 0;
            var segment = sequence.Start.GetObject() as ReadOnlySequenceSegment<int>;
            Assert.NotNull(segment);
            while (segment is not null)
            {
                Assert.Equal(runningIndex, segment.RunningIndex);
                Assert.False(segment.Memory.IsEmpty);
                runningIndex += segment.Memory.Length;
                segment = segment.Next;
            }

            Assert.Equal(expected.Length, runningIndex);
            for (var start = 0; start < expected.Length; start += 37)
            {
                var length = Math.Min(53, expected.Length - start);
                Assert.Equal(expected.AsSpan(start, length).ToArray(), sequence.Slice(start, length).ToArray());
                Assert.Equal(start, sequence.GetOffset(sequence.GetPosition(start)));
            }
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void PooledSegmentsAreResetForReuse()
    {
        for (var round = 0; round < 3; round++)
        {
            var count = 300 - (round * 100);
            var builder = new SequenceBuilder<int>(8);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    builder.Add(round + i);
                }

                var sequence = builder.ToReadOnlySequence();
                Assert.False(sequence.IsSingleSegment);
                Assert.Equal(Enumerable.Range(round, count), sequence.ToArray());
            }
            finally
            {
                builder.Dispose();
            }
        }
    }

    [Fact]
    public void SingleChunkAddsAndFinalizationDoNotAllocate()
    {
        var (length, _) = Build(); // Warms up the code paths.
        var allocated = AllocationHelper.Minimum(static () => Build().Allocated);

        Assert.Equal(SequenceBuilder<int>.DefaultInitialCapacity, length);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static (long Length, long Allocated) Build()
        {
            var builder = new SequenceBuilder<int>();
            try
            {
                // Rent the chunk before measuring, so the result does not depend on what the pool retains.
                builder.Add(0);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 1; i < SequenceBuilder<int>.DefaultInitialCapacity; i++)
                {
                    builder.Add(i);
                }

                var sequence = builder.ToReadOnlySequence();
                var after = GC.GetAllocatedBytesForCurrentThread();

                Assert.True(sequence.IsSingleSegment);
                Assert.Equal(Enumerable.Range(0, SequenceBuilder<int>.DefaultInitialCapacity), sequence.ToArray());
                return (sequence.Length, after - before);
            }
            finally
            {
                builder.Dispose();
            }
        }
    }

    private static bool AddThrows(ref SequenceBuilder<int> builder, int value)
    {
        try
        {
            builder.Add(value);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static bool AddRangeThrows(ref SequenceBuilder<int> builder, ReadOnlySpan<int> values)
    {
        try
        {
            builder.AddRange(values);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static List<ArraySegment<T>> GetChunks<T>(ReadOnlySequence<T> sequence)
    {
        var chunks = new List<ArraySegment<T>>();
        foreach (var memory in sequence)
        {
            Assert.True(MemoryMarshal.TryGetArray(memory, out var segment));
            Assert.False(segment.All(x => EqualityComparer<T>.Default.Equals(x, default)));
            chunks.Add(segment);
        }

        return chunks;
    }
}
