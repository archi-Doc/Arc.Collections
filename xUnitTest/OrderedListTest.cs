using System;
using Xunit;
using Arc.Collection;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest
{
    public class OrderedListClass : IComparable<OrderedListClass>
    {
        public OrderedListClass(int id)
        {
            this.Id = id;
            this.Serial = serial++;
        }

        public int Id { get; set; }

        public int Serial { get; }

        private static int serial;

        public int CompareTo(OrderedListClass? other)
        {
            if (other == null)
            {
                return 1;
            }

            if (this.Id < other.Id)
            {
                return -1;
            }
            else if (this.Id > other.Id)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public override string ToString() => $"{this.Id} ({this.Serial})";
    }

    public class IntComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            if (x < y)
            {
                return -1;
            }
            else if (x > y)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class OrderedListTest
    {
        [Fact]
        public void Test1()
        {
            var ol = new OrderedList<int>() { 1, 3, 2, 0, 5, -10, 0, };
            var array = new int[] { 1, 3, 2, 0, 5, -10, 0, };
            Array.Sort(array);
            ol.SequenceEqual(array).IsTrue();

            var ol2 = new OrderedList<OrderedListClass>();
            ol2.Add(new OrderedListClass(1)); // 0
            ol2.Add(new OrderedListClass(3)); // 1
            ol2.Add(new OrderedListClass(2)); // 2
            ol2.Add(new OrderedListClass(0)); // 3
            ol2.Add(new OrderedListClass(5)); // 4
            ol2.Add(new OrderedListClass(-10)); // 5
            ol2.Add(new OrderedListClass(2)); // 6
            ol2.Add(new OrderedListClass(0)); // 7
            ol2.Add(new OrderedListClass(2)); // 8
            ol2.Add(new OrderedListClass(2)); // 9

            var n = 0;
            ol2[n].Id.Is(-10);
            ol2[n++].Serial.Is(5);
            ol2[n].Id.Is(0);
            ol2[n++].Serial.Is(3);
            ol2[n].Id.Is(0);
            ol2[n++].Serial.Is(7);
            ol2[n].Id.Is(1);
            ol2[n++].Serial.Is(0);
            ol2[n].Id.Is(2);
            ol2[n++].Serial.Is(2);
            ol2[n].Id.Is(2);
            ol2[n++].Serial.Is(6);
            ol2[n].Id.Is(2);
            ol2[n++].Serial.Is(8);
            ol2[n].Id.Is(2);
            ol2[n++].Serial.Is(9);
            ol2[n].Id.Is(3);
            ol2[n++].Serial.Is(1);
            ol2[n].Id.Is(5);
            ol2[n++].Serial.Is(4);
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

            var ol = new OrderedList<int>(array);
            ol.SequenceEqual(sortedArray).IsTrue();

            ol = new OrderedList<int>();
            foreach (var x in array)
            {
                ol.Add(x);
            }

            ol.SequenceEqual(sortedArray).IsTrue();

            ol = new OrderedList<int>(array, new IntComparer());
            ol.SequenceEqual(sortedArray).IsTrue();

            ol = new OrderedList<int>();
            foreach (var x in array)
            {
                ol.Add(x);
            }

            ol.SequenceEqual(sortedArray).IsTrue();
        }
    }
}
