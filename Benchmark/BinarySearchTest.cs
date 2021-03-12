using System;
using BenchmarkDotNet.Attributes;
using Arc.Collection;
using System.Linq;
using System.Diagnostics;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class BinarySearchTest
    {
        [Params(10, 100, 10_000)]
        public int Length;

        public int[] IntArray = default!;

        public int[] SortedArray = default!;

        public int Value;

        public OrderedList<int> OrderedList = default!;

        public BinarySearchTest()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            this.IntArray = new int[this.Length];
            var r = new Random();
            for (var n = 0; n < this.Length; n++)
            {
                this.IntArray[n] = r.Next(this.Length);
            }

            this.Value = this.IntArray[this.Length / 2];
            this.OrderedList = new(this.IntArray);
            Array.Sort(this.IntArray);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        [Benchmark]
        public int Array_BinarySearch()
        {
            return Array.BinarySearch(this.IntArray, this.Value);
        }

        [Benchmark]
        public int OrderedList_BinarySearch()
        {
            return this.OrderedList.BinarySearch(this.Value);
        }
    }
}
