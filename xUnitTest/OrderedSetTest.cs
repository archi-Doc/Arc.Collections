using System;
using Xunit;
using Arc.Collection;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace xUnitTest
{
    public class OrderedSetTest
    {
        [Fact]
        public void Test1()
        {
            var ss = new SortedSet<int>();
            var os = new OrderedSet<int>();

            AddAndValidate(0);
            RemoveAndValidate(0);

            AddAndValidate(0);
            AddAndValidate(1);
            AddAndValidate(2);
            AddAndValidate(3);
            AddAndValidate(31);
            AddAndValidate(-3);
            AddAndValidate(4);
            RemoveAndValidate(3);
            RemoveAndValidate(-1);
            RemoveAndValidate(2);

            (var node, _) = os.Add(0);
            os.RemoveNode(node);

            Clear();

            AddAndValidate(0);
            AddAndValidate(1);
            AddAndValidate(2);
            RemoveAndValidate(1);
            AddAndValidate(1);
            RemoveAndValidate(2);
            AddAndValidate(2);
            RemoveAndValidate(3);
            AddAndValidate(3);

            Clear();

            AddAndValidate(0);
            AddAndValidate(1);
            AddAndValidate(2);
            RemoveAndValidate(1);

            var r = new Random(12);
            for (var n = 0; n < 1000; n++)
            {
                AddAndValidate(r.Next(500));
            }

            for (var n = 0; n < 1000; n++)
            {
                RemoveAndValidate(r.Next(500));
            }

            Clear();

            void Clear()
            {
                ss.Clear();
                os.Clear();
            }

            void AddAndValidate(int x)
            {
                ss.Add(x);
                os.AddAndValidate(x);
                os.SequenceEqual((IEnumerable<int>)ss).IsTrue();
            }

            void RemoveAndValidate(int x)
            {
                ss.Remove(x);
                os.RemoveAndValidate(x);
                os.SequenceEqual((IEnumerable<int>)ss).IsTrue();
            }
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

        [Fact]
        public void Random2()
        {
            var r = new Random(13);

            for (var n = 0; n < 10; n++)
            {
                var ss = new SortedSet<int>();
                var os = new OrderedSet<int>();

                var array = TestHelper.GetRandomNumbers(r, 0, 100, 1000).ToArray();
                for (var m = 0; m < array.Length; m++)
                {
                    ss.Add(array[m]);
                    os.Add(array[m]);
                    os.Validate().IsTrue();
                }

                ss.SequenceEqual(os).IsTrue();
                ss.Clear();
                os.Clear();

                array = TestHelper.GetRandomNumbers(r, 0, 100, 10000).ToArray();
                for (var m = 0; m < array.Length; m += 2)
                {
                    ss.Add(array[m]);
                    os.Add(array[m]);
                    os.Validate().IsTrue();

                    ss.Remove(array[m + 1]);
                    os.Remove(array[m + 1]);
                    os.Validate().IsTrue();
                }

                ss.SequenceEqual(os).IsTrue();
                ss.Clear();
                os.Clear();
            }
        }

        private void RandomTest(Random r, int start, int end, int count, bool duplicate)
        {
            var ss = new SortedSet<int>();
            var os = new OrderedSet<int>();
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
                ss.Add(x);
                os.Add(x);
                os.Validate().IsTrue();
            }

            ss.SequenceEqual(os).IsTrue();

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
                ss.Remove(x);
                os.Remove(x);
                os.Validate().IsTrue();
            }

            ss.SequenceEqual(os).IsTrue();
            if (branch != 1)
            {
                ss.Count.Is(0);
                os.Count.Is(0);
            }
        }

        [Fact]
        void Node()
        {
            var r = new Random(14);

            for (var n = 0; n < 10; n++)
            {
                NodeTest(r, -100, 100, 100, false);
                NodeTest(r, -100, 100, 100, true);
                NodeTest(r, -500, 500, 100, false);
                NodeTest(r, -600, 600, 1000, true);
            }
        }

        private void NodeTest(Random r, int start, int end, int count, bool duplicate)
        {
            var ss = new SortedSet<int>();
            var os = new OrderedSet<int>();
            IEnumerable<int> e;
            OrderedSet<int>.Node[] nodes;

            if (duplicate)
            {
                e = TestHelper.GetRandomNumbers(r, start, end, count);
            }
            else
            {
                e = TestHelper.GetUniqueRandomNumbers(r, start, end, count);
            }

            var array = e.ToArray();
            nodes = new OrderedSet<int>.Node[array.Length];
            var n = 0;
            foreach (var x in array)
            {
                ss.Add(x);
                (nodes[n++], _) = os.Add(x);
                os.Validate().IsTrue();
            }

            ss.SequenceEqual(os).IsTrue();

            var branch = r.Next(3);
            if (branch == 0)
            {
                e = TestHelper.GetRandomNumbers(r, start, end, count);
                array = e.ToArray();
            }
            else if (branch == 1)
            {
                TestHelper.Shuffle(r, nodes);
            }

            var branch2 = r.Next(3);
            if (branch2 == 0 && branch == 2)
            {// mixed
                n = 0;
                foreach (var x in array)
                {
                    ss.Remove(x);
                    if (n % 2 == 0)
                    {
                        os.RemoveNode(nodes[n]);
                    }
                    else
                    {
                        os.Remove(x);
                    }
                    n++;
                    os.Validate().IsTrue();
                }
            }
            else if (branch2 == 1 && branch == 2)
            {// Node[]
                n = 0;
                foreach (var x in array)
                {
                    ss.Remove(x);
                    os.RemoveNode(nodes[n++]);
                    os.Validate().IsTrue();
                }
            }
            else
            {// int[]
                foreach (var x in array)
                {
                    ss.Remove(x);
                    os.Remove(x);
                    os.Validate().IsTrue();
                }
            }

            ss.SequenceEqual(os).IsTrue();
            if (branch == 2)
            {
                ss.Count.Is(0);
                os.Count.Is(0);
            }
        }
    }
}
