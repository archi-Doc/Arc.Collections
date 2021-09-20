// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Collections.Obsolete;
using Xunit;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1009

namespace xUnitTest
{
    public static class TestHelper
    {
        public static void ValidateWithOrderedMultiMap<TKey, TValue>(this UnorderedMultiMap<TKey, TValue> um, OrderedMultiMap<TKey, TValue> map)
        {
            um.Count.Is(map.Count);

            foreach (var x in map)
            {
                um.Contains(x).IsTrue();
            }
        }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        public static void ValidateWithDictionary<TKey, TValue>(this UnorderedMap<TKey, TValue> um, Dictionary<TKey, TValue> dic)
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        {
            um.Count.Is(dic.Count);
            foreach (var x in um)
            {
                dic.TryGetValue(x.Key, out var value).IsTrue();
                Comparer<TValue>.Default.Compare(x.Value, value).Is(0);
            }

            foreach(var x in dic)
            {
                um.TryGetValue(x.Key, out var value).IsTrue();
                Comparer<TValue>.Default.Compare(x.Value, value).Is(0);
            }
        }

        public static OrderedMap<T, int>.Node AddAndValidate<T>(this OrderedSet<T> os, T value)
        {
            var result = os.Add(value);
            os.Validate().IsTrue();
            return result.node;
        }

        public static bool RemoveAndValidate<T>(this OrderedSet<T> os, T value)
        {
            var result = os.Remove(value);
            os.Validate().IsTrue();
            return result;

        }
        public static OrderedSetObsolete<T>.Node AddAndValidate<T>(this OrderedSetObsolete<T> os, T value)
        {
            var result = os.Add(value);
            os.Validate().IsTrue();
            return result.node;
        }

        public static bool RemoveAndValidate<T>(this OrderedSetObsolete<T> os, T value)
        {
            var result = os.Remove(value);
            os.Validate().IsTrue();
            return result;
        }

        public static OrderedMap<TKey, TValue>.Node AddAndValidate<TKey, TValue>(this OrderedMap<TKey, TValue> om, TKey key, TValue value)
        {
            var result = om.Add(key, value);
            om.Validate().IsTrue();
            return result.node;
        }

        public static bool RemoveAndValidate<TKey, TValue>(this OrderedMap<TKey, TValue> om, TKey key)
        {
            var result = om.Remove(key);
            om.Validate().IsTrue();
            return result;
        }

        public static void Shuffle<T>(Random r, T[] array)
        {
            var n = array.Length;
            while (n > 1)
            {
                n--;
                var m = r.Next(n + 1);
                (array[m], array[n]) = (array[n], array[m]);
            }
        }

        public static System.Collections.Generic.IEnumerable<int> GetUniqueRandomNumbers(Random r, int start, int end, int count)
        {
            var work = new int[end - start + 1];
            for (int n = start, i = 0; n <= end; n++, i++)
            {
                work[i] = n;
            }

            for (int resultPos = 0; resultPos < count; resultPos++)
            {
                int nextResultPos = r.Next(resultPos, work.Length);
                (work[resultPos], work[nextResultPos]) = (work[nextResultPos], work[resultPos]);
            }

            return work.Take(count);
        }

        public static System.Collections.Generic.IEnumerable<int> GetRandomNumbers(Random r, int start, int end, int count)
        {
            for (var n = 0; n < count; n++)
            {
                yield return r.Next(start, end);
            }
        }

        public static KeyValuePair<TKey, TValue>[] ToReverseArray<TKey, TValue>(OrderedMultiMap<TKey, TValue> map)
        {
            var list = new KeyValuePair<TKey, TValue>[map.Count];
            var n = map.Count - 1;
            var node = map.Last;
            while( node != null)
            {
                list[n--] = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
                node = node.Previous;
            }

            n.Is(-1);
            return list;
        }
    }
}
