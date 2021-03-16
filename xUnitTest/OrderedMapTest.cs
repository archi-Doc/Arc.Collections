using System;
using Xunit;
using Arc.Collection;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest
{
    public class OrderedMapTest
    {
        [Fact]
        public void Test1()
        {
            var sd = new SortedDictionary<int, int>();
            var om = new OrderedMap<int, int>();

            var os = new OrderedSet<int>();
            os.Add(3);

            AddAndValidate(0, 0);
            RemoveAndValidate(0);

            var r = new Random(12);

            Clear();

            void Clear()
            {
                sd.Clear();
                om.Clear();
            }

            void AddAndValidate(int x, int y)
            {
                sd.Add(x, y);
                om.AddAndValidate(x, y);
                om.SequenceEqual(sd).IsTrue();
            }

            void RemoveAndValidate(int x)
            {
                sd.Remove(x);
                om.RemoveAndValidate(x);
                om.SequenceEqual(sd).IsTrue();
            }
        }
    }
}
