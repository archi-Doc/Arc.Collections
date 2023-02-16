using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;
using System.Linq;
using System.Diagnostics;
using Arc.Collections.Obsolete;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class OrderedMultiMapTest
    {
        public const int Size = 1000;

        [Params(100, 500, 1000)]
        public int Count;

        public int[] IntArray = default!;
        public int[] IntArrayShuffled = default!;

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = BenchmarkHelper.GetRandomNumbers(r, 0, Size, this.Count).ToArray();
            this.IntArrayShuffled = (int[])this.IntArray.Clone();
            BenchmarkHelper.Shuffle(r, IntArrayShuffled);
        }

        [Benchmark]
        public int Bench_OrderedMap()
        {
            var m = new OrderedMap<int, int>();
            foreach (var x in this.IntArray)
            {
                m.Add(x, x);
            }

            for (var n = 0; n < this.Count / 2; n++)
            {
                m.Remove(this.IntArrayShuffled[n]);
            }

            var accum = 0;
            var node = m.First;
            while (node != null)
            {
                accum += node.Value;
                node = node.Next;
            }

            return accum;
        }

        [Benchmark]
        public int Bench_OrderedMultiMap()
        {
            var m = new OrderedMultiMap<int, int>();
            foreach (var x in this.IntArray)
            {
                m.Add(x, x);
            }

            for (var n = 0; n < this.Count / 2; n++)
            {
                m.Remove(this.IntArrayShuffled[n], this.IntArrayShuffled[n]);
            }

            var accum = 0;
            var node = m.First;
            while (node != null)
            {
                accum += node.Value;
                node = node.Next;
            }

            return accum;
        }

        // [Benchmark]
        public int Bench_OrderedKeyValueList()
        {
            var m = new OrderedKeyValueList<int, int>();
            foreach (var x in this.IntArray)
            {
                m.Add(x, x);
            }

            for (var n = 0; n < this.Count / 2; n++)
            {
                m.Remove(this.IntArrayShuffled[n], this.IntArrayShuffled[n]);
            }

            var accum = 0;
            foreach (var y in m.Values)
            {
                accum += y;
            }

            return accum;
        }
    }
}
