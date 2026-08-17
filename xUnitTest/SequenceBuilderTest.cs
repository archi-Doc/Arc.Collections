// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Linq;
using Xunit;

namespace Arc.Collections.Tests;

public class SequenceBuilderTest
{
    [Fact]
    public void Empty()
    {
        var builder = new SequenceBuilder<int>();

        var sequence = builder.ToReadOnlySequence();

        Assert.True(sequence.IsEmpty);
        Assert.Equal(0, builder.Length);

        builder.Dispose();
    }

    [Fact]
    public void AddSingle()
    {
        var builder = new SequenceBuilder<int>();

        builder.Add(123);

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(1, builder.Length);
        Assert.Equal(new[] { 123 }, sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void AddMultiple()
    {
        var builder = new SequenceBuilder<int>(4);

        for (var i = 0; i < 100; i++)
        {
            builder.Add(i);
        }

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(100, builder.Length);
        Assert.Equal(Enumerable.Range(0, 100).ToArray(), sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void AddRangeSingleChunk()
    {
        var builder = new SequenceBuilder<int>(16);
        var source = new[] { 1, 2, 3, 4, 5 };

        builder.AddRange(source);

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(source.Length, builder.Length);
        Assert.Equal(source, sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void AddRangeMultipleChunks()
    {
        var builder = new SequenceBuilder<int>(4);
        var source = Enumerable.Range(0, 1000).ToArray();

        builder.AddRange(source);

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(source.Length, builder.Length);
        Assert.Equal(source, sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void AddAndAddRange()
    {
        var builder = new SequenceBuilder<int>(4);

        builder.Add(1);
        builder.AddRange(new[] { 2, 3, 4, 5 });
        builder.Add(6);
        builder.AddRange(new[] { 7, 8, 9, 10 });

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(10, builder.Length);
        Assert.Equal(Enumerable.Range(1, 10).ToArray(), sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void ToReadOnlySequenceCanBeCalledMultipleTimes()
    {
        var builder = new SequenceBuilder<int>(4);

        builder.AddRange(new[] { 1, 2, 3, 4, 5, 6 });

        var sequence1 = builder.ToReadOnlySequence();
        var sequence2 = builder.ToReadOnlySequence();

        Assert.Equal(sequence1.Length, sequence2.Length);
        Assert.Equal(sequence1.ToArray(), sequence2.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void AddAfterFinalizationThrows()
    {
        var builder = new SequenceBuilder<int>();

        builder.Add(1);
        builder.ToReadOnlySequence();

        try
        {
            builder.Add(2);
            Assert.Fail("Expected InvalidOperationException.");
        }
        catch (InvalidOperationException)
        {
        }

        builder.Dispose();
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        var builder = new SequenceBuilder<int>(4);

        builder.AddRange(new[] { 1, 2, 3, 4, 5 });

        builder.Dispose();
        builder.Dispose();

        Assert.Equal(0, builder.Length);
        Assert.True(builder.ToReadOnlySequence().IsEmpty);
    }

    [Fact]
    public void DisposeBeforeFinalization()
    {
        var builder = new SequenceBuilder<int>(4);

        builder.AddRange(new[] { 1, 2, 3 });

        builder.Dispose();

        Assert.Equal(0, builder.Length);
        Assert.True(builder.ToReadOnlySequence().IsEmpty);
    }

    [Fact]
    public void DefaultBuilder()
    {
        SequenceBuilder<int> builder = default;

        builder.Add(1);
        builder.Add(2);
        builder.Add(3);

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(3, builder.Length);
        Assert.Equal(new[] { 1, 2, 3 }, sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void DefaultBuilderWithReferenceType()
    {
        SequenceBuilder<string> builder = default;

        builder.Add("A");
        builder.Add("B");
        builder.Add("C");

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(new[] { "A", "B", "C" }, sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void ExactChunkBoundary()
    {
        var builder = new SequenceBuilder<int>(16);

        for (var i = 0; i < 16; i++)
        {
            builder.Add(i);
        }

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(16, sequence.Length);
        Assert.Equal(Enumerable.Range(0, 16).ToArray(), sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void CrossChunkBoundary()
    {
        var builder = new SequenceBuilder<int>(16);

        for (var i = 0; i < 17; i++)
        {
            builder.Add(i);
        }

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(17, sequence.Length);
        Assert.Equal(Enumerable.Range(0, 17).ToArray(), sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void MultipleLargeChunks()
    {
        const int count = SequenceBuilder<int>.MaxChunkCapacity * 3 + 123;

        var builder = new SequenceBuilder<int>(256);

        for (var i = 0; i < count; i++)
        {
            builder.Add(i);
        }

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(count, builder.Length);
        Assert.Equal(count, sequence.Length);

        var index = 0;
        foreach (var memory in sequence)
        {
            foreach (var value in memory.Span)
            {
                Assert.Equal(index++, value);
            }
        }

        Assert.Equal(count, index);

        builder.Dispose();
    }

    [Fact]
    public void AddRangeEmptyDoesNotChangeLength()
    {
        var builder = new SequenceBuilder<int>();

        builder.AddRange(ReadOnlySpan<int>.Empty);

        Assert.Equal(0, builder.Length);
        Assert.True(builder.ToReadOnlySequence().IsEmpty);

        builder.Dispose();
    }

    [Fact]
    public void AddRangeAcrossExistingChunkBoundary()
    {
        var builder = new SequenceBuilder<int>(8);

        builder.AddRange(new[] { 0, 1, 2, 3, 4, 5 });
        builder.AddRange(new[] { 6, 7, 8, 9, 10, 11, 12 });

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(Enumerable.Range(0, 13).ToArray(), sequence.ToArray());

        builder.Dispose();
    }

    [Fact]
    public void SequenceContainsCorrectSegmentData()
    {
        var builder = new SequenceBuilder<int>(4);

        for (var i = 0; i < 100; i++)
        {
            builder.Add(i);
        }

        var sequence = builder.ToReadOnlySequence();

        var expected = 0;
        foreach (var memory in sequence)
        {
            foreach (var value in memory.Span)
            {
                Assert.Equal(expected++, value);
            }
        }

        Assert.Equal(100, expected);

        builder.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(SequenceBuilder<int>.MaxChunkCapacity + 1)]
    public void InvalidInitialCapacityThrows(int initialCapacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(initialCapacity));
    }

    [Fact]
    public void MaximumInitialCapacityIsAccepted()
    {
        var builder = new SequenceBuilder<int>(SequenceBuilder<int>.MaxChunkCapacity);

        builder.Add(1);

        Assert.Equal(new[] { 1 }, builder.ToReadOnlySequence().ToArray());

        builder.Dispose();
    }

    [Fact]
    public void ReferenceTypeValuesIncludingNull()
    {
        var builder = new SequenceBuilder<string?>(4);

        builder.Add("A");
        builder.Add(null);
        builder.Add("B");
        builder.Add(null);

        var sequence = builder.ToReadOnlySequence();

        Assert.Equal(new string?[] { "A", null, "B", null }, sequence.ToArray());

        builder.Dispose();
    }

    private static void Add(ref SequenceBuilder<int> builder, int value)
        => builder.Add(value);

    private static void AddRange(ref SequenceBuilder<int> builder, ReadOnlySpan<int> values)
        => builder.AddRange(values);

    private static void Create(int initialCapacity)
    {
        var builder = new SequenceBuilder<int>(initialCapacity);
        builder.Dispose();
    }
}
