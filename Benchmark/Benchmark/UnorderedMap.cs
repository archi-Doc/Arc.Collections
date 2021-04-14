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

        public UnorderedMap<int, int>.Node[] Reuse;

        public UnorderedMapTest()
        {
            this.Reuse = new UnorderedMap<int, int>.Node[100];
        }

        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public int AddInt_Dictionary()
        {
            var c = new Dictionary<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AndInt_UnorderedMap()
        {
            var c = new UnorderedMap<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AndInt_UnorderedMapReuse()
        {
            var c = new UnorderedMap<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n, this.Reuse[n]);
            }

            return c.Count;
        }

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
