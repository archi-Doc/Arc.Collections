using System;
using Xunit;
using Arc.Collection;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest
{
    public class OrderedMultiSetTest
    {
        [Fact]
        public void Test1()
        {
            var ol = new OrderedList<int>() { 1, 3, 2, 0, 5, -10, 0, };
            var array = new int[] { 1, 3, 2, 0, 5, -10, 0, };
            var ms = new OrderedMultiSet<int>(array);
            Array.Sort(array);
            ol.SequenceEqual(array).IsTrue();
            ol.SequenceEqual(ms).IsTrue();

            ms = new OrderedMultiSet<int>(array, OrderedListClass.InternalComparer.Instance);
            ms.SequenceEqual(array.Reverse()).IsTrue();

            var ms2 = new OrderedMultiSet<OrderedListClass>();
            ms2.Add(new OrderedListClass(1, 0)); // 0
            ms2.Add(new OrderedListClass(3, 1)); // 1
            ms2.Add(new OrderedListClass(2, 2)); // 2
            ms2.Add(new OrderedListClass(0, 3)); // 3
            ms2.Add(new OrderedListClass(5, 4)); // 4
            ms2.Add(new OrderedListClass(-10, 5)); // 5
            ms2.Add(new OrderedListClass(2, 6)); // 6
            ms2.Add(new OrderedListClass(0, 7)); // 7
            ms2.Add(new OrderedListClass(2, 8)); // 8
            ms2.Add(new OrderedListClass(2, 9)); // 9

            var node = ms2.First;
            node!.Key.Id.Is(-10);
            node!.Key.Serial.Is(5);
            node = node.Next;

            node!.Key.Id.Is(0);
            node!.Key.Serial.Is(3);
            node = node.Next;

            node!.Key.Id.Is(0);
            node!.Key.Serial.Is(7);
            node = node.Next;

            node!.Key.Id.Is(1);
            node!.Key.Serial.Is(0);
            node = node.Next;

            node!.Key.Id.Is(2);
            node!.Key.Serial.Is(2);
            node = node.Next;

            node!.Key.Id.Is(2);
            node!.Key.Serial.Is(6);
            node = node.Next;

            node!.Key.Id.Is(2);
            node!.Key.Serial.Is(8);
            node = node.Next;

            node!.Key.Id.Is(2);
            node!.Key.Serial.Is(9);
            node = node.Next;

            node!.Key.Id.Is(3);
            node!.Key.Serial.Is(1);
            node = node.Next;

            node!.Key.Id.Is(5);
            node!.Key.Serial.Is(4);
            node = node.Next;
            node.IsNull();
        }

        [Fact]
        public void Random()
        {
            var r = new Random(12);

            for (var n = 0; n < 10; n++)
            {
                RandomTest(r, -100, 100, 100, false);
                RandomTest(r, -100, 100, 100, true);
                RandomTest(r, -500, 500, 100, false);
                RandomTest(r, -600, 600, 1000, true);
            }
        }

        private void RandomTest(Random r, int start, int end, int count, bool duplicate)
        {
            IEnumerable<int> e;
            if (duplicate)
            {
                e = TestHelper.GetRandomNumbers(r, start, end, count);
            }
            else
            {
                e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);
            }

            var array = e.ToArray();
            var sortedArray = (int[])array.Clone();
            Array.Sort(sortedArray);

            var ms = new OrderedMultiSet<int>(array);
            ms.SequenceEqual(sortedArray).IsTrue();

            ms = new OrderedMultiSet<int>();
            foreach (var x in array)
            {
                ms.Add(x);
            }

            ms.SequenceEqual(sortedArray).IsTrue();

            ms = new OrderedMultiSet<int>(array, new IntComparer());
            ms.SequenceEqual(sortedArray).IsTrue();

            ms = new OrderedMultiSet<int>();
            foreach (var x in array)
            {
                ms.Add(x);
            }

            ms.SequenceEqual(sortedArray).IsTrue();
        }
    }
}
