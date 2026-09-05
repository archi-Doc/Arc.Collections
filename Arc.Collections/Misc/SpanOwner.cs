// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

// ============================================================================
//  SpanOwner.cs
//
//  A tiny ref struct that folds the common
//  "stackalloc if small, ArrayPool.Rent if large, Return at the end"
//  boilerplate into a single `using` declaration, with zero overhead.
//
//  This is the same technique as the .NET runtime's internal ValueStringBuilder:
//  because C# cannot return a stackalloc'd span from a callee, the CALLER
//  always stackallocs a fixed-size scratch buffer and hands it in; the struct
//  uses it when the requested length fits and rents from the pool otherwise.
//
//  Usage:
//      using var owner = new SpanOwner<byte>(stackalloc byte[BaseHelper.StackallocThreshold], length);
//      Span<byte> buffer = owner.Span;
//      // ... use buffer ...
//      // Dispose (emitted by `using`) returns the pooled array, if any.
//
//  Notes:
//  - `stackalloc byte[CONSTANT]` compiles to a constant stack-pointer bump and,
//    combined with [SkipLocalsInit], involves no zeroing — this is typically
//    *cheaper* than the variable-length `stackalloc byte[length]` in the
//    original pattern (which needs stack probing and defeats constant folding).
//  - The `using` statement's try/finally also makes the pool Return
//    exception-safe, which the hand-written pattern usually is not.
//  - Being a ref struct, it cannot be used in async methods or iterators
//    (neither can stackalloc/Span locals, so nothing is lost).
// ============================================================================

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Arc.Collections;

#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text
#pragma warning disable SA1629 // Documentation text should end with a period

/// <summary>
/// Provides a temporary span using caller-supplied storage or a rented array.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Uses the supplied span when the requested length fits; otherwise rents from
/// <see cref="ArrayPool{T}.Shared" />. Dispose once and do not copy an owner of a rented array.
/// Rented arrays containing references are cleared on return. Do not use the span after disposal.
/// </remarks>
public ref struct SpanOwner<T>
{
    private readonly Span<T> span;
    private T[]? arrayToReturn;

    /// <summary>
    /// Initializes a buffer of exactly <paramref name="length"/> elements,
    /// using <paramref name="scratchBuffer"/> when it is large enough and
    /// renting from the shared pool otherwise.
    /// </summary>
    /// <param name="scratchBuffer">A scratch buffer, typically <c>stackalloc T[Threshold]</c>.</param>
    /// <param name="length">The required number of elements.</param>
    /// <remarks>If you want to disable zero-initialization, add the [SkipLocalsInit] attribute to the method or assembly</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanOwner(Span<T> scratchBuffer, int length)
    {
        if ((uint)length <= (uint)scratchBuffer.Length)
        {
            this.span = scratchBuffer.Slice(0, length);
            this.arrayToReturn = null;
        }
        else
        {
            T[] array = ArrayPool<T>.Shared.Rent(length);
            this.arrayToReturn = array;
            this.span = array.AsSpan(0, length);
        }
    }

    /// <summary>
    /// Initializes a buffer of exactly <paramref name="length"/> elements,
    /// always rented from the shared pool (for contexts where no stackalloc
    /// scratch buffer is available).
    /// </summary>
    /// <param name="length">The required number of elements.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanOwner(int length)
    {
        T[] array = ArrayPool<T>.Shared.Rent(length);
        this.arrayToReturn = array;
        this.span = array.AsSpan(0, length);
    }

    /// <summary>Gets the buffer (its length is exactly the requested length).</summary>
    public readonly Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.span;
    }

    /// <summary>Returns the rented array to the pool, if one was rented.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        T[]? array = this.arrayToReturn;
        if (array is not null)
        {
            this.arrayToReturn = null;
            ArrayPool<T>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
