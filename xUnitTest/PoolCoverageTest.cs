using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class PoolCoverageTest
{
    [Fact]
    public void SharedArrayIsReturnedOnlyAfterEveryOwnerReleasesIt()
    {
        var owner = BytePool.RentArray.CreateFrom(new byte[16]);
        Parallel.For(0, 1000, _ => owner.IncrementAndShare());
        Assert.Equal(1001, owner.Count);
        Parallel.For(0, 1000, _ => owner.Return());
        Assert.Equal(1, owner.Count);
        Assert.True(owner.IsRent);
        owner.Return();
        Assert.True(owner.IsReturned);
        Assert.False(owner.TryIncrement());
        Assert.Throws<InvalidOperationException>(() => owner.IncrementAndShare());
        Assert.Throws<InvalidOperationException>(() => owner.Return());
    }

    [Fact]
    public void ReferenceCountCannotOverflowIntoSingleOwnerSentinel()
    {
        var owner = BytePool.RentArray.CreateFrom(new byte[1]);
        var counter = typeof(BytePool.RentArray).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)!;
        counter.SetValue(owner, BytePool.SingleCount - 1);
        Assert.Throws<InvalidOperationException>(() => owner.TryIncrement());
        Assert.Throws<InvalidOperationException>(() => owner.IncrementAndShare());
        Assert.Equal(BytePool.SingleCount - 1, owner.Count);
    }

    [Fact]
    public void SlicesShareOwnershipAndPreserveOffsets()
    {
        var owner = BytePool.RentArray.CreateFrom(new byte[16]);
        var memory = owner.AsMemory(4, 4);
        memory.Span.Fill(7);
        var shared = memory.Slice(1, 2).IncrementAndShareReadOnly();
        memory.Return();
        Assert.True(shared.IsRent);
        Assert.Equal(new byte[] { 7, 7 }, shared.Span.ToArray());
        Assert.Equal(0, shared.Slice(2).Length);
        Assert.Equal(0, shared.Slice(2, 0).Length);
        shared.Return();
        Assert.True(owner.IsReturned);
        Assert.Equal(0, BytePool.RentMemory.Empty.Slice(0, 0).Length);
        Assert.Equal(0, BytePool.RentReadOnlyMemory.Empty.Slice(0, 0).Length);
    }

    [Fact]
    public void ObjectPoolReusesObjectsAndDisposesOverflowAndContents()
    {
        var pool = new ObjectPool<Disposable>(() => new Disposable(), 2);
        var first = pool.Rent();
        var second = pool.Rent();
        var overflow = pool.Rent();
        Assert.NotSame(first, second);
        pool.Return(first);
        Assert.Same(first, pool.Rent());
        pool.Return(first);
        pool.Return(second);
        pool.Return(overflow);
        Assert.Equal(1, overflow.DisposeCount);
        pool.Dispose();
        pool.Dispose();
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => pool.Rent());
        Assert.Throws<ObjectDisposedException>(() => pool.Return(new Disposable()));
        Assert.Throws<ArgumentNullException>(() => new ObjectPool<object>(null!));
    }

    [Fact]
    public void SpanOwnerUsesScratchAndClearsReferencesReturnedToPool()
    {
        var scratch = new int[4];
        using (var owner = new SpanOwner<int>(scratch, 3))
        {
            owner.Span.Fill(42);
            Assert.Equal(3, owner.Span.Length);
        }

        Assert.Equal(new[] { 42, 42, 42, 0 }, scratch);
        var weak = ReturnReferenceBuffer();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReturnReferenceBuffer()
    {
        var rented = new SpanOwner<PooledReference>(1);
        var value = new PooledReference();
        var weak = new WeakReference(value);
        rented.Span[0] = value;
        rented.Dispose();
        rented.Dispose();
        return weak;
    }

    private sealed class PooledReference
    {
    }

    private sealed class Disposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => this.DisposeCount++;
    }
}
