﻿using System;
using BenchmarkDotNet.Attributes;
using Arc.Collection;
using System.Linq;
using System.Diagnostics;
using Arc.Collection.Obsolete;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Benchmark
{
    public class CustomEqualityClass : IEqualityComparer<CustomEqualityClass>
    {
        public int Id { get; set; }

        public CustomEqualityClass(int id)
        {
            this.Id = id;
        }

        public bool Equals(CustomEqualityClass? x, CustomEqualityClass? y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (y == null)
                {
                    return false;
                }
                else
                {
                    return x.Id == y.Id;
                }
            }
        }

        public int GetHashCode([DisallowNull] CustomEqualityClass obj) => obj.Id;
    }

    [Config(typeof(BenchmarkConfig))]
    public class UnorderedMapTest
    {
        // [Params(100, 1_000, 10_000)]
        [Params(1000)]
        public int Count;

        public int TargetInt = 90;

        public int[] IntArray = default!;
        public UnorderedMapClass<int, int>.Node[] Reuse = default!;
        public UnorderedMapClass<int, int>.Node TargetNode = default!;
        public int TargetNodeIndex3 = -1;
        public Dictionary<int, int> IntDictionary = default!;
        public UnorderedMapClass<int, int> IntUnorderedMapClass = default!;
        public UnorderedMap<int, int> IntUnorderedMap = default!;
        public UnorderedMap2<int, int> IntUnorderedMap2 = default!;
        public UnorderedMap4<int, int> IntUnorderedMap4 = default!;

        public UnorderedMapTest()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            var r = new Random(12);
            this.IntArray = BenchmarkHelper.GetRandomNumbers(r, 0, this.Count, this.Count).ToArray();
            this.Reuse = new UnorderedMapClass<int, int>.Node[1000];

            this.IntDictionary = new();
            this.IntUnorderedMapClass = new(0);
            this.IntUnorderedMap2 = new(0);
            this.IntUnorderedMap = new(0);
            this.IntUnorderedMap4 = new(0);
            var om = new OrderedMap<int, int>();
            var total = 0;
            foreach (var x in this.IntArray)
            {
                this.IntDictionary.TryAdd(x, x * 2);
                this.IntUnorderedMapClass.Add(x, x * 2);
                this.IntUnorderedMap2.Add(x, x * 2);
                this.IntUnorderedMap.Add(x, x * 2);
                this.IntUnorderedMap4.Add(x, x * 2);

                var result = om.Add(x, x * 2);
                if (result.newlyAdded)
                {
                    total += result.node.Value;
                }
            }

            this.TargetNode = this.IntUnorderedMapClass.FindFirstNode(TargetInt)!;
            this.TargetNodeIndex3 = this.IntUnorderedMap.FindFirstNode(TargetInt)!;
            var n = total;
        }

        /*[Benchmark]
        public int AddSerialInt_Dictionary__()
        {
            var c = new Dictionary<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddSerialInt_DictionaryB_()
        {
            var c = new Dictionary<int, int>(1000);
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddSerialInt_UnorderedMapClass()
        {
            var c = new UnorderedMapClass<int, int>();
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

        [Benchmark]
        public int AddSerialInt_UnorderedMapB()
        {
            var c = new UnorderedMap<int, int>(1000);
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddSerialInt_UnorderedMap2()
        {
            var c = new UnorderedMap2<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddSerialInt_UnorderedMap4()
        {
            var c = new UnorderedMap4<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n, n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddRandomInt_Dictionary__()
        {
            var c = new Dictionary<int, int>(2);
            for (var n = 0; n < this.Count; n++)
            {
                c.TryAdd(this.IntArray[n], this.IntArray[n]);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddRandomInt_UnorderedMapClass()
        {
            var c = new UnorderedMapClass<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(this.IntArray[n], this.IntArray[n]);
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
        }

        [Benchmark]
        public int AddRandomInt_UnorderedMap2()
        {
            var c = new UnorderedMap2<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(this.IntArray[n], this.IntArray[n]);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddRandomInt_UnorderedMap4()
        {
            var c = new UnorderedMap4<int, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(this.IntArray[n], this.IntArray[n]);
            }

            return c.Count;
        }*/

        /*[Benchmark]
        public int RemoveAdd_Dictionary__()
        {
            this.IntDictionary.Remove(TargetInt);
            this.IntDictionary.Add(TargetInt, TargetInt);

            return this.IntDictionary.Count;
        }

        [Benchmark]
        public int RemoveAdd_UnorderedMap()
        {
            this.IntUnorderedMap.Remove(TargetInt);
            this.IntUnorderedMap.Add(TargetInt, TargetInt);

            return this.IntUnorderedMap.Count;
        }

        [Benchmark]
        public int Replace_UnorderedMapNode()
        {
            this.IntUnorderedMap.RemoveNode(this.TargetNode);
            this.IntUnorderedMap.Add(TargetInt, TargetInt, this.TargetNode);

            return this.IntUnorderedMap.Count;
        }

        [Benchmark]
        public int RemoveAdd_UnorderedMap3()
        {
            this.IntUnorderedMap3.Remove(TargetInt);
            this.IntUnorderedMap3.Add(TargetInt, TargetInt);

            return this.IntUnorderedMap.Count;
        }

        [Benchmark]
        public int Replace_UnorderedMap3Node()
        {
            this.IntUnorderedMap3.RemoveNode(this.TargetNodeIndex3);
            this.IntUnorderedMap3.Add(TargetInt, TargetInt);

            return this.IntUnorderedMap3.Count;
        }*/

        [Benchmark]
        public int GetRandomInt_Dictionary__()
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
        public int GetRandomInt_UnorderedMapClass()
        {
            var total = 0;
            for (var n = 0; n < this.Count; n++)
            {
                if (this.IntUnorderedMapClass.TryGetValue(n, out var value))
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
        }

        [Benchmark]
        public int GetRandomInt_UnorderedMap2()
        {
            var total = 0;
            for (var n = 0; n < this.Count; n++)
            {
                if (this.IntUnorderedMap2.TryGetValue(n, out var value))
                {
                    total += value;
                }
            }

            return total;
        }

        /*[Benchmark]
        public int AddString_Dictionary__()
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
        }

        [Benchmark]
        public int AndString_UnorderedMap2()
        {
            var c = new UnorderedMap2<string, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n.ToString(), n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AndString_UnorderedMap3()
        {
            var c = new UnorderedMap3<string, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(n.ToString(), n);
            }

            return c.Count;
        }*/

        /*[Benchmark]
        public int AddCustom_Dictionary__()
        {
            var c = new Dictionary<CustomEqualityClass, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(new CustomEqualityClass(n), n);
            }

            return c.Count;
        }

        [Benchmark]
        public int AddCustom_UnorderedMap()
        {
            var c = new UnorderedMap<CustomEqualityClass, int>();
            for (var n = 0; n < this.Count; n++)
            {
                c.Add(new CustomEqualityClass(n), n);
            }

            return c.Count;
        }*/
    }
}
