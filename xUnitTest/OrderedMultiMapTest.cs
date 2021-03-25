using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collection;
using Xunit;

namespace xUnitTest
{
    public class OrderedMultiMapTest
    {
        [Fact]
        public void Test1()
        {
            var list = new OrderedKeyValueList<int, int>();
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

            void AddAndValidate(int x, int y)
            {
                list.Add(x, y);
                mm.Add(x, y);
                mm.SequenceEqual(list).IsTrue();
            }

            void RemoveAndValidate(int x, int y)
            {
                list.Remove(x, y);
                mm.Remove(x, y);
                mm.SequenceEqual(list).IsTrue();
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
            var om = new OrderedMap<int, int>();
            var mm = new OrderedMultiMap<int, int>();
            IEnumerable<int> e;

            e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);

            var array = e.ToArray();
            foreach (var x in array)
            {
                om.Add(x, x);
                mm.Add(x, x);
            }

            mm.SequenceEqual(om).IsTrue();

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
                om.Remove(x);
                mm.Remove(x);
            }

            mm.SequenceEqual(om).IsTrue();
            if (branch != 1)
            {
                mm.Count.Is(0);
                om.Count.Is(0);
            }
        }

        private void RandomTestDup(Random r, int start, int end, int count)
        {
            var mm = new OrderedMultiMap<int, int>();
            var list = new OrderedKeyValueList<int, int>();
            IEnumerable<int> e;

            e = TestHelper.GetRandomNumbers(r, start, end, count);

            var array = e.ToArray();
            foreach (var x in array)
            {
                mm.Add(x, x);
                list.Add(x, x);
            }

            mm.SequenceEqual(list).IsTrue();
        }
    }
}
