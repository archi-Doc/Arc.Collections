using System;
using BenchmarkDotNet.Attributes;
using Arc.Collections;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class OrderedMultiMapUnsafe
    {
        public const int Length = 1_00_000;

        public int[] IntArray = default!;
        public int[] IntArrayShuffled = default!;

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = new int[Length];
            for (var i = 0; i < Length; i++)
            {
                var v = r.Next();
                this.IntArray[i] = v;
            }
        }

        [Benchmark]
        public object Bench_OrderedMultiMap()
        {
            var mm = new OrderedMultiMap<Identifier, int>();
            for (var i = 0; i < Length; i++)
            {
                mm.Add(new(this.IntArray[i]), this.IntArray[i]);
            }

            return mm;
        }

        [Benchmark]
        public object Bench_OrderedMultiMapUnsafe()
        {
            var mm = new OrderedMultiMap<Identifier, int>();
            // mm.UnsafePresearchForStructKey = true;
            for (var i = 0; i < Length; i++)
            {
                mm.Add(new(this.IntArray[i]), this.IntArray[i]);
            }

            return mm;
        }
    }
}
