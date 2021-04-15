using System;
using BenchmarkDotNet.Attributes;
using Arc.Collection;
using System.Linq;
using System.Diagnostics;
using Arc.Collection.Obsolete;
using System.Collections.Generic;

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class UnorderedMapTest
    {
        [Params(100)]
        public int Count;

        public int[] IntArray = default!;
        public UnorderedMap<int, int>.Node[] Reuse = default!;
        public Dictionary<int, int> IntDictionary = default!;
        public UnorderedMap<int, int> IntUnorderedMap = default!;

        public UnorderedMapTest()
        {
            
        }

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = BenchmarkHelper.GetRandomNumbers(r, 0, this.Count, this.Count).ToArray();
            this.Reuse = new UnorderedMap<int, int>.Node[100];

            this.IntDictionary = new();
            this.IntUnorderedMap = new(0);
            var om = new OrderedMap<int, int>();
            var total = 0;
            foreach (var x in this.IntArray)
            {
                this.IntDictionary.TryAdd(x, x * 2);
                this.IntUnorderedMap.Add(x, x * 2);

                var result = om.Add(x, x * 2);
                if (result.newlyAdded)
                {
                    total += result.node.Value;
                }
            }

            var n = total;
        }

        [Benchmark]
        public int AddSerialInt_Dictionary()
        {
            var c = new Dictionary<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddSerialInt_UnorderedMap()
        {
            var c = new UnorderedMap<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        /*[Benchmark]
        public int AndInt_UnorderedMapReuse()
        {
            var c = new UnorderedMap<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n, this.Reuse[n]);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddRandomInt_Dictionary()
        {
            var c = new Dictionary<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.TryAdd(this.IntArray[n], this.IntArray[n]);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddRandomInt_UnorderedMap()
        {
            var c = new UnorderedMap<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(this.IntArray[n], this.IntArray[n]);
            }

            return c.Count;
        }*/

        /*[Benchmark]
        public int GetRandomInt_Dictionary()
        {
            var total = 0;
            for (var n = 0; n < this.Count; n++)
            {
                if (this.IntDictionary.TryGetValue(n, out var value))
                {
                    total += value;
                }
            }

            return total;
        }

        [Benchmark]
        public int GetRandomInt_UnorderedMap()
        {
            var total = 0;
            for (var n = 0; n < this.Count; n++)
            {
                if (this.IntUnorderedMap.TryGetValue(n, out var value))
                {
                    total += value;
                }
            }

            return total;
        }*/


        /*[Benchmark]
        public int AddString_Dictionary()
        {
            var c = new Dictionary<string, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n.ToString(), n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AndString_UnorderedMap()
        {
            var c = new UnorderedMap<string, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n.ToString(), n);
            }

            return c.Count;
        }*/
    }
}
