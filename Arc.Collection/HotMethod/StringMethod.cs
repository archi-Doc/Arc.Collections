// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Collection.HotMethod
{
    public sealed class StringMethod : IHotMethod<string>
    {
        public static readonly StringMethod Instance = new();

        private StringMethod()
        {
        }

        public int BinarySearch(string[] array, int index, int length, string value)
        {
            var min = index;
            var max = length - 1;
            while (min <= max)
            {
                var mid = min + ((max - min) / 2);
                var cmp = string.Compare(value, array[mid]);
                if (cmp < 0)
                {
                    max = mid - 1;
                    continue;
                }
                else if (cmp > 0)
                {
                    min = mid + 1;
                    continue;
                }
                else
                {// Found
                    return mid;
                }
            }

            return ~min;
        }
    }
}
