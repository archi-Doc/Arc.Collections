// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collections;

internal static class HashtableHelper
{
    public static int CalculateCapacity(int collectionSize)
    {
        collectionSize *= 2;
        int capacity = 1;
        while (capacity < collectionSize)
        {
            capacity <<= 1;
        }

        if (capacity < 8)
        {
            return 8;
        }

        return capacity;
    }
}
