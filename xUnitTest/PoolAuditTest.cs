using System;
using System.Buffers;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class PoolAuditTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("\n", "\n")]
    [InlineData("text", "text\n\n")]
    [InlineData("text\n", "text\n\n")]
    [InlineData("text\n\n", "text\n\n")]
    [InlineData("\0", "\0\n\n")]
    [InlineData("\0\n", "\0\n\n")]
    [InlineData("text\0", "text\0\n\n")]
    public void BlankLineSuffixDistinguishesNullCharactersFromMissingCharacters(string input, string expected)
    {
        var builder = new PooledStringBuilder();
        try
        {
            builder.Append(input);
            builder.EnsureTrailingBlankLine();
            builder.EnsureTrailingBlankLine();
            Assert.Equal(expected, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("text", "text ")]
    [InlineData("text ", "text ")]
    [InlineData("text\n", "text\n")]
    [InlineData("\0", "\0 ")]
    public void SpaceSuffixIsIdempotent(string input, string expected)
    {
        var builder = new PooledStringBuilder();
        try
        {
            builder.Append(input);
            builder.EnsureTrailingSpace();
            builder.EnsureTrailingSpace();
            Assert.Equal(expected, builder.ToString());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void EvictionCannotAddToCacheDisposedByCallback()
    {
        using var cache = new KeyedObjectCache<int, CallbackDisposable>(1);
        var first = new CallbackDisposable(cache.Dispose);
        var rejected = new CallbackDisposable();
        Assert.True(cache.TryAdd(1, first));
        Assert.False(cache.TryAdd(2, rejected));
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(0, rejected.DisposeCount);
        Assert.Equal(0, cache.Count);
        Assert.Null(cache.TakeOrDefault(2));
    }

    [Fact]
    public void EvictionCallbackAddingPendingKeyDoesNotCorruptCache()
    {
        using var cache = new KeyedObjectCache<int, CallbackDisposable>(2);
        var replacement = new CallbackDisposable();
        var first = new CallbackDisposable(() => Assert.True(cache.TryAdd(3, replacement)));
        var second = new CallbackDisposable();
        var rejected = new CallbackDisposable();
        Assert.True(cache.TryAdd(1, first));
        Assert.True(cache.TryAdd(2, second));
        Assert.False(cache.TryAdd(3, rejected));
        Assert.Equal(2, cache.Count);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
        Assert.Equal(0, rejected.DisposeCount);
        Assert.Same(second, cache.TakeOrDefault(2));
        Assert.Same(replacement, cache.TakeOrDefault(3));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void EvictionCallbackRefillingCacheIsNotEvictedAgain()
    {
        using var cache = new KeyedObjectCache<int, CallbackDisposable>(1);
        var replacement = new CallbackDisposable();
        var first = new CallbackDisposable(() => Assert.True(cache.TryAdd(3, replacement)));
        var rejected = new CallbackDisposable();
        Assert.True(cache.TryAdd(1, first));
        Assert.False(cache.TryAdd(2, rejected));
        Assert.Equal(1, cache.Count);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(0, replacement.DisposeCount);
        Assert.Equal(0, rejected.DisposeCount);
        Assert.Same(replacement, cache.TakeOrDefault(3));
    }

    [Fact]
    public void ReturnedValueTypeLeaseIsEmptyAndDefaultLeaseIsSafe()
    {
        default(KeyedObjectCache<int, int>.Lease).Dispose();
        default(KeyedObjectCache<int, CallbackDisposable>.Lease).Dispose();
        using var cache = new KeyedObjectCache<int, int>(1);
        var lease = cache.CreateLease(1, 42);
        lease = lease.Return();
        Assert.Equal(42, cache.TakeOrDefault(1));
        lease.Dispose();
        Assert.Equal(0, cache.Count);
        using (cache.CreateLease(2, 0))
        {
        }

        Assert.Equal(1, cache.Count);
        Assert.Equal(0, cache.TakeOrDefault(2));

        using var referenceCache = new KeyedObjectCache<int, CallbackDisposable>(1);
        referenceCache.CreateLease(1, null).Dispose();
        Assert.Equal(0, referenceCache.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16384)]
    [InlineData(32768)]
    [InlineData(32769)]
    [InlineData(100000)]
    public void SequenceRangesPreserveDataAcrossChunkBoundaries(int length)
    {
        var expected = Enumerable.Range(0, length).ToArray();
        var builder = new SequenceBuilder<int>(16);
        try
        {
            builder.Add(-1);
            builder.AddRange(expected);
            builder.Add(length);
            var sequence = builder.ToReadOnlySequence();
            Assert.Equal(length + 2, builder.Length);
            Assert.Equal(new[] { -1 }.Concat(expected).Append(length), sequence.ToArray());
            Assert.Equal(sequence, builder.ToReadOnlySequence());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void LargeContiguousRangeNeedsOnlyOneChunk()
    {
        var expected = Enumerable.Range(0, 16384).ToArray();
        var builder = new SequenceBuilder<int>();
        try
        {
            builder.AddRange(expected);
            var sequence = builder.ToReadOnlySequence();
            Assert.True(sequence.IsSingleSegment);
            Assert.Equal(expected, sequence.ToArray());
        }
        finally
        {
            builder.Dispose();
        }
    }

    [Fact]
    public void UntrackedMemoryViewsShareArrayWithoutAcquiringOwnership()
    {
        var array = new byte[] { 1, 2, 3, 4, 5 };
        var memory = BytePool.RentedMemory.CreateFrom(array.AsMemory(1, 3));
        var readOnly = BytePool.RentedReadOnlyMemory.CreateFrom(array.AsMemory(1, 3));
        Assert.Null(memory.Owner);
        Assert.Null(readOnly.Owner);
        Assert.False(memory.IsRented);
        Assert.False(readOnly.IsRented);
        Assert.True(memory.IsReturned);
        Assert.True(readOnly.IsReturned);
        Assert.True(memory.TryIncrement());
        Assert.True(readOnly.TryIncrement());
        memory.IncrementAndShare().Memory.Span[0] = 9;
        Assert.Equal(new byte[] { 9, 3, 4 }, readOnly.IncrementAndShare().Memory.ToArray());
        Assert.Equal(readOnly.Memory, memory.IncrementAndShareReadOnly().Memory);
        readOnly.UnsafeMemory.Span[2] = 8;
        Assert.Equal(new byte[] { 9, 3, 8 }, memory.ReadOnly.Span.ToArray());
        Assert.Equal(new byte[] { 1, 9, 3, 8, 5 }, array);
        Assert.True(memory.Return().IsEmpty);
        Assert.True(readOnly.Return().IsEmpty);
        memory.Dispose();
        readOnly.Dispose();
    }

    [Fact]
    public void EmptyMemoryViewsSupportSharingAndDisposal()
    {
        var memory = BytePool.RentedMemory.Empty;
        var readOnly = BytePool.RentedReadOnlyMemory.Empty;
        Assert.True(memory.IncrementAndShare().IsEmpty);
        Assert.True(memory.IncrementAndShareReadOnly().IsEmpty);
        Assert.True(readOnly.IncrementAndShare().IsEmpty);
        Assert.True(BytePool.RentedMemory.CreateFrom(ReadOnlyMemory<byte>.Empty).IsEmpty);
        Assert.True(BytePool.RentedReadOnlyMemory.CreateFrom(ReadOnlyMemory<byte>.Empty).IsEmpty);
        memory.Dispose();
        readOnly.Dispose();
    }

    [Fact]
    public void TrackedArrayViewsPreserveOffsetsAndLifetime()
    {
        var array = new byte[] { 1, 2, 3, 4 };
        using var owner = BytePool.RentedArray.CreateFrom(array);
        Assert.Same(array, owner.Array);
        Assert.Equal(new byte[] { 2, 3, 4 }, owner.AsMemory(1).Span.ToArray());
        Assert.Equal(new byte[] { 2, 3 }, owner.AsReadOnlyMemory(1, 2).Span.ToArray());
        Assert.Equal(new byte[] { 2, 3, 4 }, owner.AsReadOnlyMemory(1).Span.ToArray());
        Assert.Equal(new byte[] { 2, 3 }, owner.AsSpan(1, 2).ToArray());
        Assert.Equal(new byte[] { 2, 3, 4 }, owner.AsSpan(1).ToArray());
        Assert.Equal(array, owner.AsSpan().ToArray());
        var memory = owner.AsMemory();
        var readOnly = memory.ReadOnly;
        Assert.Same(owner, memory.Owner);
        Assert.Same(owner, readOnly.Owner);
        Assert.True(memory.TryIncrement());
        Assert.True(readOnly.TryIncrement());
        memory.Dispose();
        readOnly.Dispose();
        Assert.Equal(1, owner.ReferenceCount);
        using var shared = memory.IncrementAndShare();
        using var sharedReadOnly = readOnly.IncrementAndShare();
        Assert.Equal(3, owner.ReferenceCount);
    }

    [Fact]
    public void ByteArrayFactoriesTrackOwnershipAndValidateInputs()
    {
        var array = new byte[] { 1, 2, 3 };
        using var full = BytePool.RentedMemory.CreateFrom(array);
        using var slice = BytePool.RentedMemory.CreateFrom(array, 1, 2);
        using var readOnly = BytePool.RentedReadOnlyMemory.CreateFrom(array);
        using var readOnlySlice = BytePool.RentedReadOnlyMemory.CreateFrom(array, 1, 2);
        Assert.NotNull(full.Owner);
        Assert.NotNull(readOnly.Owner);
        Assert.Equal(new byte[] { 2, 3 }, slice.Span.ToArray());
        Assert.Equal(new byte[] { 2, 3 }, readOnlySlice.Span.ToArray());
        Assert.Throws<ArgumentNullException>(() => BytePool.RentedArray.CreateFrom(null!));
        Assert.Throws<ArgumentNullException>(() => BytePool.RentedMemory.CreateFrom((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => BytePool.RentedReadOnlyMemory.CreateFrom((byte[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => BytePool.Default.Rent(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BytePool.RentedMemory.CreateFrom(array, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BytePool.RentedReadOnlyMemory.CreateFrom(array, 2, 2));
    }

    [Fact]
    public void SpanOwnerRentsWhenScratchIsTooSmall()
    {
        Span<int> scratch = stackalloc int[2];
        scratch.Fill(7);
        using var owner = new SpanOwner<int>(scratch, 17);
        owner.Span.Fill(3);
        Assert.Equal(17, owner.Span.Length);
        Assert.Equal(new[] { 7, 7 }, scratch.ToArray());
        Assert.Equal(3, owner.Span[16]);
    }

    private sealed class CallbackDisposable(Action? callback = null) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            this.DisposeCount++;
            callback?.Invoke();
        }
    }
}
