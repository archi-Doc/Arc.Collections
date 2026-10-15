// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Collections;
using Xunit;

namespace XunitTest;

/// <summary>
/// Regression tests for defects found in <see cref="UnorderedList{T}"/> and <see cref="OrderedList{T}"/>.
/// </summary>
public class ListQualityRegressionTest
{
    [Fact]
    public void UnorderedList_EmptyAddRangeDoesNotInvalidateEnumerators()
    {
        // Adding nothing does not modify the list, like List<T> and OrderedList<T>.
        var list = new UnorderedList<int>(new[] { 1, 2, 3 });
        var visited = new List<int>();
        foreach (var x in list)
        {
            list.AddRange(new List<int>());
            list.AddRange(Array.Empty<int>());
            list.AddRange(ReadOnlySpan<int>.Empty);
            list.AddRange(Enumerable.Empty<int>());
            visited.Add(x);
        }

        visited.Is(1, 2, 3);
        list.Count.Is(3);
    }
}
