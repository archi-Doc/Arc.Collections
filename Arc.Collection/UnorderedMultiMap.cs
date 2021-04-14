using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arc.Collection
{
    public class UnorderedMultiMap<TKey, TValue> : UnorderedMap<TKey, TValue>
    {
        public UnorderedMultiMap()
            : base()
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(int capacity)
            : base(capacity)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(int capacity, IEqualityComparer<TKey>? comparer)
            : base(capacity, comparer)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IDictionary<TKey, TValue> dictionary)
            : base(dictionary)
        {
            this.AllowMultiple = true;
        }

        public UnorderedMultiMap(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
            : base(dictionary, comparer)
        {
            this.AllowMultiple = true;
        }
    }
}
