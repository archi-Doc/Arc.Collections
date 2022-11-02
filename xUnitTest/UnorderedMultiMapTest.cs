using System;
using Xunit;
using Arc.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace xUnitTest
{
    public class UnorderedMultiMapTest
    {
        [Fact]
        public void Test1()
        {
            var um = new UnorderedMultiMap<int, int>();
            var mm = new OrderedMultiMap<int, int>();

            AddAndValidate(0, 0);
            AddAndValidate(1, 1);
            AddAndValidate(0, 2);

            RemoveAndValidate(0, 0);

            AddAndValidate(0, 0);

            RemoveAndValidate(0, 0);
            RemoveAndValidate(0, 2);

            AddAndValidate(0, 0);
            AddAndValidate(-1, 0);
            AddAndValidate(1, 2);

            um.EnumerateValue(-99).OrderBy(x => x).SequenceEqual(new int[] { });
            um.EnumerateValue(-99).OrderBy(x => x).SequenceEqual(new int[] { 0, });
            um.EnumerateValue(-99).OrderBy(x => x).SequenceEqual(new int[] { 1, 2, });

            void AddAndValidate(int x, int y)
            {
                um.Add(x, y);
                mm.Add(x, y);
                um.ValidateWithOrderedMultiMap(mm);
            }

            void RemoveAndValidate(int x, int y)
            {
                um.Remove(x, y);
                mm.Remove(x, y);
                um.ValidateWithOrderedMultiMap(mm);
            }
        }

        [Fact]
        public void Random()
        {
            var r = new Random(12);

            for (var n = 0; n < 10; n++)
            {
                RandomTest(r, -100, 100, 100);
                RandomTestDup(r, -100, 100, 10);
                RandomTestDup(r, -100, 100, 100);
                RandomTestDup(r, -100, 100, 200);

                RandomTest(r, -1000, 1000, 1000);
                RandomTestDup(r, -1000, 1000, 100);
                RandomTestDup(r, -1000, 1000, 1000);
                RandomTestDup(r, -1000, 1000, 2000);
            }
        }

        private void RandomTest(Random r, int start, int end, int count)
        {
            var um = new UnorderedMultiMap<int, int>();
            var mm = new OrderedMultiMap<int, int>();
            IEnumerable<int> e;

            e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);

            var array = e.ToArray();
            foreach (var x in array)
            {
                um.Add(x, x);
                mm.Add(x, x);
            }

            um.ValidateWithOrderedMultiMap(mm);

            var branch = r.Next(3);
            if (branch == 1)
            {
                e = TestHelper.GetRandomNumbers(r, start, end, count);
                array = e.ToArray();
            }
            else
            {
                TestHelper.Shuffle(r, array);
            }

            foreach (var x in array)
            {
                um.Remove(x);
                mm.Remove(x);
            }

            um.ValidateWithOrderedMultiMap(mm);
            if (branch != 1)
            {
                um.Count.Is(0);
                mm.Count.Is(0);
            }
        }

        private void RandomTestDup(Random r, int start, int end, int count)
        {
            var mm = new OrderedMultiMap<int, int>();
            var um = new UnorderedMultiMap<int, int>();
            IEnumerable<int> e;

            e = TestHelper.GetRandomNumbers(r, start, end, count);

            var array = e.ToArray();
            foreach (var x in array)
            {
                mm.Add(x, x);
                um.Add(x, x);
            }

            um.ValidateWithOrderedMultiMap(mm);

            e = TestHelper.GetRandomNumbers(r, start, end, count / 2);

            array = e.ToArray();
            foreach (var x in array)
            {
                mm.Remove(x, x);
                um.Remove(x, x);
            }

            um.ValidateWithOrderedMultiMap(mm);

            e = TestHelper.GetRandomNumbers(r, start, end, count / 2);

            array = e.ToArray();
            foreach (var x in array)
            {
                mm.Add(x, x);
                um.Add(x, x);
            }

            um.ValidateWithOrderedMultiMap(mm);
        }

        [Fact]
        public void Test2()
        {
            var um = new UnorderedMultiMap<int, int>();
            um.TryGetMostDuplicateKey().Is((0, 0));

            um.Add(1, 1);
            um.TryGetMostDuplicateKey().Is((1, 1));

            um.Add(2, 1);
            um.TryGetMostDuplicateKey().Is((1, 1));
            um.Add(2, 2);
            um.TryGetMostDuplicateKey().Is((2, 2));

            um.Add(3, 1);
            um.TryGetMostDuplicateKey().Is((2, 2));
            um.Add(3, 2);
            um.TryGetMostDuplicateKey().Is((2, 2));
            um.Add(3, 3);
            um.TryGetMostDuplicateKey().Is((3, 3));

            um.Add(4, 1);
            um.TryGetMostDuplicateKey().Is((3, 3));
        }
        }
}

