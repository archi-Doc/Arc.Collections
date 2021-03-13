// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Arc.Collection.HotMethod
{
    public sealed class StandardMethod<T> : IHotMethod<T>
    {
        public int BinarySearch(T[] array, int index, int length, T value) => Array.BinarySearch<T>(array, index, length, value);
    }
}
