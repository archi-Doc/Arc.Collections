// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections.Obsolete;

namespace Arc.Collections.HotMethod
{
    /*public sealed class StringMethod : IHotMethod<string>
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

        public (int cmp, OrderedSetObsolete<string>.Node? leaf) SearchNode(OrderedSetObsolete<string>.Node? target, string value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = value.CompareTo(x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }

    public sealed class StringMethod2<TValue> : IHotMethod2<string, TValue>
    {
        public (int cmp, OrderedMap<string, TValue>.Node? leaf) SearchNode(OrderedMap<string, TValue>.Node? target, string key)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = key.CompareTo(x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

                p = x;
                if (cmp < 0)
                {
                    x = x.Left;
                }
                else if (cmp > 0)
                {
                    x = x.Right;
                }
                else
                {// Found
                    return (0, x);
                }
            }

            return (cmp, p);
        }
    }*/
}
