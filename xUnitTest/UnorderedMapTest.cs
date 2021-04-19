using System;
using Xunit;
using Arc.Collection;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace xUnitTest
{
    public class UnorderedMapTestClass : IEqualityComparer<UnorderedMapTestClass>
    {
        public UnorderedMapTestClass(int id)
        {
            this.Id = id;
        }

        public int Id { get; set; }

        public bool Equals(UnorderedMapTestClass? x, UnorderedMapTestClass? y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null)
            {
                return false;
            }
            else
            {
                return x.Id == y.Id;
            }
        }

        public int GetHashCode([DisallowNull] UnorderedMapTestClass obj) => this.Id.GetHashCode();

        public override string ToString() => $"{this.Id}";
    }

    public class UnorderedMapTest
    {
        [Fact]
        public void Test1()
        {
            var dic = new Dictionary<int, int>();
            var um = new UnorderedMap<int, int>();

            AddAndValidate(0, 0);
            RemoveAndValidate(0);

            AddAndValidate(0, 0);
            AddAndValidate(1, 2);
            AddAndValidate(11, 12);
            AddAndValidate(2, 4);
            RemoveAndValidate(3);
            RemoveAndValidate(1);

            var r = new Random(12);

            Clear();

            void Clear()
            {
                dic.Clear();
                um.Clear();
            }

            void AddAndValidate(int x, int y)
            {
                dic.Add(x, y);
                um.Add(x, y);
                um.ValidateWithDictionary(dic);
            }

            void RemoveAndValidate(int x)
            {
                dic.Remove(x);
                um.Remove(x);
                um.ValidateWithDictionary(dic);
            }
        }

        [Fact]
        public void TestNull()
        {
            var um = new UnorderedMap<int?, UnorderedMapTestClass>();
            um.Add(0, new UnorderedMapTestClass(0));
            um.Add(null, new UnorderedMapTestClass(999));
            um.Add(2, new UnorderedMapTestClass(2));
            um.Add(1, new UnorderedMapTestClass(1));
            um.Add(5, new UnorderedMapTestClass(5));

            um.Count.Is(5);
            um[null].Id.Is(999);
            um[0].Id.Is(0);
            um[1].Id.Is(1);
            um[2].Id.Is(2);
            um[5].Id.Is(5);

            um.Keys.OrderBy(x => x).SequenceEqual(new int?[] { null, 0, 1, 2, 5, }).IsTrue();
            um.OrderBy(x => x.Key).Select(x => x.Value.Id).SequenceEqual(new int[] { 999, 0, 1, 2, 5, }).IsTrue();
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
            var dic = new Dictionary<int, int>();
            var um = new UnorderedMap<int, int>();
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
            foreach (var x in array)
            {
                dic[x] = x;
                um.Add(x, x);
            }

            um.ValidateWithDictionary(dic);

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
                dic.Remove(x);
                um.Remove(x);
            }

            um.ValidateWithDictionary(dic);

            if (branch != 1)
            {
                dic.Count.Is(0);
                um.Count.Is(0);
            }
        }

        [Fact]
        public void Random2()
        {
            var r = new Random(12);

            for (var n = 0; n < 10; n++)
            {
            }
        }
    }
}
