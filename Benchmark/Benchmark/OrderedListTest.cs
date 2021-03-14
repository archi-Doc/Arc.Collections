using System;
using BenchmarkDotNet.Attributes;
using Arc.Collection;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class OrderedListTest
    {
        [Params(10, 100, 1000)]
        public int Length;

        public int[] IntArray = default!;

        public OrderedListTest()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = BenchmarkHelper.GetUniqueRandomNumbers(r, -this.Length, this.Length, this.Length).ToArray();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        [Benchmark]
        public int Add_OrderedList()
        {
            var list = new OrderedList<int>();
            foreach (var x in this.IntArray)
            {
                list.Add(x);
            }

            return list.Count;
        }

        [Benchmark]
        public int Add_OrderedKeyValueList()
        {
            var list = new OrderedKeyValueList<int, int>();
            foreach (var x in this.IntArray)
            {
                list.Add(x, x);
            }

            return list.Count;
        }

        [Benchmark]
        public int Add_SortedList()
        {
            var list = new SortedList<int, int>();
            foreach (var x in this.IntArray)
            {
                list.Add(x, x);
            }

            return list.Count;
        }

        [Benchmark]
        public int Add_OrderedSet()
        {
            var list = new OrderedSet<int>();
            foreach (var x in this.IntArray)
            {
                list.Add(x);
            }

            return list.Count;
        }
    }
}
