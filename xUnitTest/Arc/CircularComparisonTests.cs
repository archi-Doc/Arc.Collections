// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Xunit;

namespace XunitTest.Arc;

public class CircularComparisonTests
{
    [Fact]
    public void UIntHalfRangeComparisonIsAntisymmetric()
    {
        const uint first = 0;
        const uint second = 0x8000_0000;

        Assert.Equal(-first.CircularCompareTo(second), second.CircularCompareTo(first));
    }

    [Fact]
    public void ULongHalfRangeComparisonIsAntisymmetric()
    {
        const ulong first = 0;
        const ulong second = 0x8000_0000_0000_0000;

        Assert.Equal(-first.CircularCompareTo(second), second.CircularCompareTo(first));
    }
}
