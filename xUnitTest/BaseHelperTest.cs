// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc;
using Xunit;

namespace xUnitTest;

public class BaseHelperTest
{
    [Fact]
    public void RemoveCrLfTest()
    {
        BaseHelper.RemoveCrLf("ABC").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\n").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\r\n").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\n").Is("ABC");
        BaseHelper.RemoveCrLf("\r\nABC\r\n\r\n").Is("ABC");
        BaseHelper.RemoveCrLf("\r\nA\nB\r\nC").Is("ABC");
        BaseHelper.RemoveCrLf("ABC\n012\r\n345").Is("ABC012345");
        BaseHelper.RemoveCrLf("\r\nA\rBC\r\n012\n345\n\n").Is("ABC012345");
    }

    [Fact]
    public void IndexOfLfOrCrLfTest()
    {
        // Test: Not found
        var index = BaseHelper.IndexOfLfOrCrLf("ABC", out var newLineLength);
        index.Is(-1);
        newLineLength.Is(0);

        // Test: Lf only (\n)
        index = BaseHelper.IndexOfLfOrCrLf("ABC\n", out newLineLength);
        index.Is(3);
        newLineLength.Is(1);

        // Test: CrLf (\r\n)
        index = BaseHelper.IndexOfLfOrCrLf("ABC\r\n", out newLineLength);
        index.Is(3);
        newLineLength.Is(2);

        // Test: Lf in middle
        index = BaseHelper.IndexOfLfOrCrLf("AB\nC", out newLineLength);
        index.Is(2);
        newLineLength.Is(1);

        // Test: CrLf in middle
        index = BaseHelper.IndexOfLfOrCrLf("AB\r\nC", out newLineLength);
        index.Is(2);
        newLineLength.Is(2);

        // Test: Lf at start
        index = BaseHelper.IndexOfLfOrCrLf("\nABC", out newLineLength);
        index.Is(0);
        newLineLength.Is(1);

        // Test: CrLf at start
        index = BaseHelper.IndexOfLfOrCrLf("\r\nABC", out newLineLength);
        index.Is(0);
        newLineLength.Is(2);

        // Test: Empty string
        index = BaseHelper.IndexOfLfOrCrLf("", out newLineLength);
        index.Is(-1);
        newLineLength.Is(0);

        // Test: Only \n
        index = BaseHelper.IndexOfLfOrCrLf("\n", out newLineLength);
        index.Is(0);
        newLineLength.Is(1);

        // Test: Only \r\n
        index = BaseHelper.IndexOfLfOrCrLf("\r\n", out newLineLength);
        index.Is(0);
        newLineLength.Is(2);

        // Test: Multiple line endings - should find first
        index = BaseHelper.IndexOfLfOrCrLf("ABC\nDEF\r\nGHI", out newLineLength);
        index.Is(3);
        newLineLength.Is(1);

        // Test: Multiple line endings (CrLf first)
        index = BaseHelper.IndexOfLfOrCrLf("ABC\r\nDEF\nGHI", out newLineLength);
        index.Is(3);
        newLineLength.Is(2);
    }
}
