// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Xunit;

namespace xUnitTest;

public class ByteRentalTest
{
    [Fact]
    public void Test1()
    {
        var owner = BytePool.Default.Rent(10);
        owner.Count.Is(1);
        owner.IsRent.IsTrue();
        owner.IsReturned.IsFalse();
        owner.Return();
        owner.Count.Is(0);
        owner.IsRent.IsFalse();
        owner.IsReturned.IsTrue();

        owner = BytePool.Default.Rent(10);
        owner.Count.Is(1);
        owner.Return();

        owner = BytePool.Default.Rent(0);
        owner.Count.Is(1);
        owner.Return();
    }
}
