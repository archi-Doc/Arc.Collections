using System;
using System.Linq;
using Arc.Collections;

namespace Sandbox
{
    public class TestClass : IComparable<TestClass>, IEquatable<TestClass>
    {
        public TestClass(int id)
        {
            this.Id = id;
        }

        public int Id { get; set; }

        public int CompareTo(TestClass? other)
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

        public bool Equals(TestClass? other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Id == other.Id;
        }

        public override int GetHashCode() => this.Id.GetHashCode();

        public override string ToString() => this.Id.ToString();
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var um = new UnorderedMap<int, int>();
            um.Add(1, 2);
            um.Add(1, 3);
            um.Add(2, 4);

            var um2 = new UnorderedMap<TestClass, int>();
            um2.Add(new TestClass(1), 2);
            um2.Add(new TestClass(2), 4);

            var ol = new OrderedKeyValueList<int, int>();
            ol.Add(2, 0);
            ol.Add(3, 0);
            ol.Add(1, 1);

            var om = new OrderedMap<int?, int?>();
            om.Add(2, 0);
            om.Add(3, 0);
            om.Add(1, 1);
            om.Add(null, 3);
            var v = om[2];
            v = om[null];

            var om2 = new OrderedMap<TestClass, int>();
            om2.Add(new TestClass(2), 2);
            om2.Add(new TestClass(1), 1);
            om2.Add(null!, 0);
            var v2 = om2[new TestClass(1)];
            v2 = om2[null!];

            var om3 = new OrderedMap<TestClass?, int>();
            om3.Add(new TestClass(2), 2);
            om3.Add(new TestClass(1), 1);
            om3.Add(null, 0);
            v2 = om3[new TestClass(1)];
            v2 = om3[null];

            var list = new UnorderedLinkedList<int>();
            list.AddFirst(1);

            var mm = new OrderedMultiMap<int, int>();
            mm.Add(1, 0);
            mm.Add(0, 1);
            mm.Add(1, 2);
            mm.Add(2, 3);
            mm.Add(-1, 4);

            var ms = new OrderedMultiSet<int>();
            ms.Add(0);
            ms.Add(1);
            ms.Add(0);
            ms.Add(-1);
            ms.Add(0);
            ms.Add(-2);
        }
    }
}
