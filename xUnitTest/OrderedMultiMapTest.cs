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
    }
}
