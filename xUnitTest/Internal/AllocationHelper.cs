// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Measures managed allocations on the current thread for zero-allocation tests.<br/>
/// The runtime occasionally charges one-off bytes to the measured thread (tiering, OSR compilation, or a GC triggered
/// by a test running in parallel), so a run is repeated and the smallest result is used.
/// Allocations made by the code under test recur on every run and still fail the test.
/// </summary>
public static class AllocationHelper
{
    /// <summary>
    /// The maximum number of measured runs.
    /// </summary>
    public const int MaxAttempts = 10;

    /// <summary>
    /// Runs <paramref name="action"/> once to warm it up, then measures it up to <see cref="MaxAttempts"/> times.
    /// </summary>
    /// <param name="action">The code to measure. It must leave its inputs in a state where it can run again.</param>
    /// <returns>The fewest bytes allocated on the current thread by a single run.</returns>
    public static long Measure(Action action)
    {
        action();
        return Minimum(() =>
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        });
    }

    /// <summary>
    /// Calls <paramref name="measure"/> up to <see cref="MaxAttempts"/> times, stopping once it reports zero.
    /// </summary>
    /// <param name="measure">Runs the code once and returns the bytes it allocated.</param>
    /// <returns>The smallest value returned by <paramref name="measure"/>.</returns>
    public static long Minimum(Func<long> measure)
    {
        var minimum = long.MaxValue;
        for (var i = 0; i < MaxAttempts && minimum > 0; i++)
        {
            minimum = Math.Min(minimum, measure());
        }

        return minimum;
    }

    /// <summary>
    /// Skips the current test unless the library and the tests run as JIT-optimized code.<br/>
    /// Unoptimized code (a Debug build, or a debugger that suppresses optimization) boxes value-type keys in generic null checks.
    /// </summary>
    public static void SkipUnlessOptimized()
        => Assert.SkipWhen(
            Debugger.IsAttached || IsJitOptimizerDisabled(typeof(OrderedMap<,>).Assembly) || IsJitOptimizerDisabled(typeof(AllocationHelper).Assembly),
            "Allocation checks require optimized code.");

    private static bool IsJitOptimizerDisabled(Assembly assembly)
        => assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;
}
