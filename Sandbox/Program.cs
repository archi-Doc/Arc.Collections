using System;
using Arc.Collection;

namespace Sandbox
{
    public class TestClass : IComparable<TestClass>
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

        public override string ToString() => this.Id.ToString();
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

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

            var list = new OrderedList<int>();
        }
    }
}
