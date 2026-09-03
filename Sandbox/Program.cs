using System;
using System.Linq;
using System.Text;
using Arc;
using Arc.Collections;

namespace Sandbox;

class Program
{
    static int Main(string[] args)
    {
        VerifyBaseHelpers();
        VerifyCollections();
        VerifyHashtableVariants();
        VerifyBytePool();

        Console.WriteLine("NativeAOT smoke test passed.");
        return 0;
    }

    private static void VerifyBaseHelpers()
    {
        var lines = BaseHelper.SplitLines("one\r\ntwo\nthree");
        Require(lines.SequenceEqual(["one", "two", "three"]), nameof(BaseHelper.SplitLines));

        Require(BaseHelper.CountDecimalChars(int.MinValue) == 11, nameof(BaseHelper.CountDecimalChars));
        Require(BaseHelper.GetValidUtf8Length(Encoding.UTF8.GetBytes("abc")) == 3, nameof(BaseHelper.GetValidUtf8Length));
        Require(XxHash3Slim.Hash64("abc") != 0, nameof(XxHash3Slim.Hash64));
    }

    private static void VerifyCollections()
    {
        var orderedMap = new OrderedMap<int, string>();
        orderedMap.Add(2, "b");
        orderedMap.Add(1, "a");
        Require(orderedMap.Keys.SequenceEqual([1, 2]), nameof(OrderedMap<int, string>));

        var orderedSet = new OrderedSet<int>();
        orderedSet.Add(3);
        orderedSet.Add(1);
        Require(orderedSet.SequenceEqual([1, 3]), nameof(OrderedSet<int>));

        var unorderedMap = new UnorderedMap<int, string>();
        unorderedMap.Add(10, "ten");
        Require(unorderedMap.TryGetValue(10, out var ten) && ten == "ten", nameof(UnorderedMap<int, string>));

        var unorderedMapSlim = new UnorderedMapSlim<int, string>();
        unorderedMapSlim.Add(20, "twenty");
        Require(unorderedMapSlim.TryGetValue(20, out var twenty) && twenty == "twenty", nameof(UnorderedMapSlim<int, string>));

        var list = new TemporaryList<int>();
        list.Add(1);
        list.Add(2);
        Require(list.ToArray().SequenceEqual([1, 2]), nameof(TemporaryList<int>));

        var queue = new CircularQueue<int>(2);
        Require(queue.TryEnqueue(7), nameof(CircularQueue<int>.TryEnqueue));
        Require(queue.TryDequeue(out var item) && item == 7, nameof(CircularQueue<int>.TryDequeue));
    }

    private static void VerifyHashtableVariants()
    {
        var intTable = new Int32Hashtable<string>();
        intTable.Add(1, "one");
        Require(intTable.TryGetValue(1, out var one) && one == "one", nameof(Int32Hashtable<string>));

        var utf8Table = new Utf8Hashtable<int>();
        utf8Table.Add("key"u8, 42);
        Require(utf8Table.TryGetValue("key"u8, out var value) && value == 42, nameof(Utf8Hashtable<int>));

        var utf16Table = new Utf16Hashtable<int>();
        utf16Table.Add("key", 43);
        Require(utf16Table.TryGetValue("key", out value) && value == 43, nameof(Utf16Hashtable<int>));

        var utf8Map = new Utf8UnorderedMap<int>();
        utf8Map.Add("abc"u8, 1);
        Require(utf8Map.TryGetValue("abc"u8, out value) && value == 1, nameof(Utf8UnorderedMap<int>));

        var utf16Map = new Utf16UnorderedMap<int>();
        utf16Map.Add("abc", 2);
        Require(utf16Map.TryGetValue("abc", out value) && value == 2, nameof(Utf16UnorderedMap<int>));
    }

    private static void VerifyBytePool()
    {
        using var owner = BytePool.Default.Rent(16);
        owner.AsSpan()[0] = 123;
        Require(owner.Array[0] == 123, nameof(BytePool));
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{name} failed.");
        }
    }
}
