using System;
using BenchmarkDotNet.Attributes;
using Arc.Collection;
using System.Linq;
using System.Diagnostics;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class TestBenchmark
    {
        [Params(10_000)]
        public int Length;

        public int[] IntArray = default!;

        public System.Collections.Generic.SortedSet<int> IntSetRef = new();

        public OrderedSet<int> IntSet = new();

        public OrderedSet<int>.Node Node0 = default!;
        public OrderedSet<int>.Node Node7 = default!;
        public OrderedSet<int>.Node Node11 = default!;
        public OrderedSet<int>.Node Node55 = default!;

        static System.Collections.Generic.IEnumerable<int> GetUniqRandomNumbers(int rangeBegin, int rangeEnd, int count)
        {
            var work = new int[rangeEnd - rangeBegin + 1];
            for (int n = rangeBegin, i = 0; n <= rangeEnd; n++, i++)
            {
                work[i] = n;
            }

            var rnd = new Random(1286);
            for (int resultPos = 0; resultPos < count; resultPos++)
            {
                int nextResultPos = rnd.Next(resultPos, work.Length);
                (work[resultPos], work[nextResultPos]) = (work[nextResultPos], work[resultPos]);
            }

            return work.Take(count);
        }

        public TestBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.IntArray = GetUniqRandomNumbers(-Length, +Length, Length).ToArray();

            foreach (var x in this.IntArray)
            {
                this.IntSetRef.Add(x);
            }

            foreach (var x in this.IntArray)
            {
                this.IntSet.Add(x);
                // Debug.Assert(this.IntSet.Validate());
            }

            (this.Node0, _) = this.IntSet.Add(0);
            (this.Node7, _) = this.IntSet.Add(7);
            (this.Node11, _) = this.IntSet.Add(11);
            (this.Node55, _) = this.IntSet.Add(55);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        /*[Benchmark]
        public int EnumerateRef()
        {
            var total = 0;
            foreach (var x in this.IntSetRef.Take(10))
            {
                total += x;
            }

            return total;
        }

        [Benchmark]
        public int Enumerate()
        {
            var total = 0;
            foreach (var x in this.IntSet.Take(10))
            {
                total += x;
            }

            return total;
        }

        [Benchmark]
        public int NewAndAddRef()
        {
            var ss = new System.Collections.Generic.SortedSet<int>();
            ss.Add(1);
            ss.Add(10);
            ss.Add(4);
            ss.Add(34);
            ss.Add(-4);
            ss.Add(43);
            ss.Add(5);
            ss.Add(0);
            ss.Add(9);
            ss.Add(20);
            foreach (var x in this.IntArray)
            {
                ss.Add(x);
            }
            return ss.Count;
        }

        [Benchmark]
        public int NewAndAdd()
        {
            var ss = new OrderedSet<int>();
            ss.Add(1);
            ss.Add(10);
            ss.Add(4);
            ss.Add(34);
            ss.Add(-4);
            ss.Add(43);
            ss.Add(5);
            ss.Add(0);
            ss.Add(9);
            ss.Add(20);
            foreach (var x in this.IntArray)
            {
                ss.Add(x);
            }
            return ss.Count;
        }

        [Benchmark]
        public int AddRemoveRef()
        {
            this.IntSetRef.Remove(0);
            this.IntSetRef.Remove(7);
            this.IntSetRef.Remove(11);
            this.IntSetRef.Remove(55);

            this.IntSetRef.Add(0);
            this.IntSetRef.Add(7);
            this.IntSetRef.Add(11);
            this.IntSetRef.Add(55);

            return this.IntSetRef.Count;
        }

        [Benchmark]
        public int AddRemove()
        {
            this.IntSet.Remove(0);
            this.IntSet.Remove(7);
            this.IntSet.Remove(11);
            this.IntSet.Remove(55);

            this.IntSet.Add(0);
            this.IntSet.Add(7);
            this.IntSet.Add(11);
            this.IntSet.Add(55);

            return this.IntSet.Count;
        }*/

        [Benchmark]
        public int AddRemoveNode()
        {
            this.IntSet.RemoveNode(this.Node0);
            this.IntSet.RemoveNode(this.Node7);
            this.IntSet.RemoveNode(this.Node11);
            this.IntSet.RemoveNode(this.Node55);

            (this.Node0, _) = this.IntSet.Add(0);
            (this.Node7, _) = this.IntSet.Add(7);
            (this.Node11, _) = this.IntSet.Add(11);
            (this.Node55, _) = this.IntSet.Add(55);

            return this.IntSet.Count;
        }

        [Benchmark]
        public int AddRemoveReuse()
        {
            this.IntSet.RemoveNode(this.Node0);
            this.IntSet.RemoveNode(this.Node7);
            this.IntSet.RemoveNode(this.Node11);
            this.IntSet.RemoveNode(this.Node55);

            (this.Node0, _) = this.IntSet.Add(0, this.Node0);
            (this.Node7, _) = this.IntSet.Add(7, this.Node7);
            (this.Node11, _) = this.IntSet.Add(11, this.Node11);
            (this.Node55, _) = this.IntSet.Add(55, this.Node55);

            return this.IntSet.Count;
        }

        [Benchmark]
        public int AddRemoveReplace()
        {
            this.IntSet.ReplaceNode(this.Node0, 0);
            this.IntSet.ReplaceNode(this.Node7, 7);
            this.IntSet.ReplaceNode(this.Node11, 11);
            this.IntSet.ReplaceNode(this.Node55, 55);

            return this.IntSet.Count;
        }
    }
}
