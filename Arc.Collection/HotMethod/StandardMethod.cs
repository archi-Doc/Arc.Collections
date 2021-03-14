// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Arc.Collection.HotMethod
{
    public sealed class StandardMethod<T> : IHotMethod<T>
    {
        public int BinarySearch(T[] array, int index, int length, T value) => Array.BinarySearch<T>(array, index, length, value);

        public (int cmp, OrderedSet<T>.Node? leaf) SearchNode(OrderedSet<T>.Node? target, T value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<T>.Default.Compare(value, x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

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

        public (int cmp, OrderedMap<T>.Node? leaf) SearchNode(OrderedMap<T>.Node? target, T value)
        {
            var x = target;
            var p = target;
            int cmp = 0;

            while (x != null)
            {
                cmp = Comparer<T>.Default.Compare(value, x.Value); // -1: 1st < 2nd, 0: equals, 1: 1st > 2nd

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
}
