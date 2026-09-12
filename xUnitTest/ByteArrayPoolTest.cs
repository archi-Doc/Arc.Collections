// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Xunit;

namespace XunitTest;

public class ByteRentalTest
{
    [Fact]
    public void Test1()
    {
        var owner = BytePool.Default.Rent(10);
        owner.ReferenceCount.Is(1);
        owner.IsRented.IsTrue();
        owner.IsReturned.IsFalse();
        owner.Return();
        owner.ReferenceCount.Is(0);
        owner.IsRented.IsFalse();
        owner.IsReturned.IsTrue();

        owner = BytePool.Default.Rent(10);
        owner.ReferenceCount.Is(1);
        owner.Return();

        owner = BytePool.Default.Rent(0);
        owner.ReferenceCount.Is(1);
        owner.Return();
    }
}
