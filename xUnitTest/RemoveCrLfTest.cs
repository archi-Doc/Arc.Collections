// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
using Xunit;

namespace xUnitTest;

public class RemoveCrLfTest
{
    [Fact]
    public void Test1()
    {
        BaseHelper.RemoveCrLf("ABC").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\r").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\r\n").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\n").Is("ABC");
        BaseHelper.RemoveCrLf("\r\nABC\r\n\r\n").Is("ABC");
        BaseHelper.RemoveCrLf("\r\nA\nB\r\nC").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\n012\r\n345").Is("ABC012345");
        BaseHelper.RemoveCrLf("\r\nA\rBC\r\n012\n345\n\n").Is("ABC012345");
    }
}
