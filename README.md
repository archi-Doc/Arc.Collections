## Arc.Collections
![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

日本語ドキュメントは[こちら](/doc/README.jp.md)

Arc.Collections is a fast C# collection library. It provides drop-in alternatives to the generic
collections in `System.Collections.Generic` that expose a `Node` interface, plus a set of
pooling, buffer and hashing utilities.

Target framework: **.NET 10**. The public API lives in the `Arc.Collections` and `Arc` namespaces.

I know it's reinventing the wheels, but these classes are necessary for implementing [CrossLink](https://github.com/archi-Doc/CrossLink). And reinventing the wheels is a kind of fun for me :)



## Quick Start

Install `Arc.Collections` using the Package Manager Console.

```
Install-Package Arc.Collections
```

Sample code. You can use these classes in the same way as the generic collection classes.

```csharp
using Arc.Collections;
```

```csharp
var array = new int[] { 2, 1, 3, };
var os = new OrderedSet<int>(array);

ConsoleWriteIEnumerable("Array:", array); // 2, 1, 3
ConsoleWriteIEnumerable("OrderedSet:", os); // 1, 2, 3

Console.WriteLine("Add 4, 0");
os.Add(4);
os.Add(0);
ConsoleWriteIEnumerable("OrderedSet:", os); // 0, 1, 2, 3, 4

static void ConsoleWriteIEnumerable<T>(string header, IEnumerable<T> e)
{
    Console.WriteLine(string.Format("{0,-12}", header) + string.Join(", ", e));
}
```



## Collections

| Collection                                                   | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `UnorderedList<T>`<br />(equivalent to `List<T>`)             | A list of objects that can be accessed by index. `AsSpan()` gives the fastest way to enumerate it. |
| `UnorderedLinkedList<T>`<br />(`LinkedList<T>`)               | A doubly linked list which has a `Node` interface.           |
| `OrderedList<T>`                                             | A list of objects that can be accessed by index and maintained in sorted order. `IComparable<T>` or `IComparer<T>` is required. |
| `OrderedKeyValueList<TKey, TValue>`<br />(`SortedList<TKey, TValue>`) | A list of key-value pairs that can be accessed by index and maintained in sorted order. Duplicate keys are allowed. `IComparable<TKey>` or `IComparer<TKey>` is required. |
| `OrderedMap<TKey, TValue>`<br />(`SortedDictionary<TKey, TValue>`) | A collection of key/value pairs sorted on the key (Red-Black Tree). The difference from `SortedDictionary<TKey, TValue>` is that `OrderedMap<TKey, TValue>` has a `Node` interface and `TKey` can be null. `IComparable<TKey>` or `IComparer<TKey>` is required. |
| `OrderedSet<T>`<br />(`SortedSet<T>`)                        | A collection of unique objects maintained in sorted order. `OrderedSet<T>` is a thin wrapper over `OrderedMap<T, byte>` (the value is not used). |
| `OrderedMultiMap<TKey, TValue>`                              | A collection of key/value pairs sorted on the key. Duplicate keys are allowed and keep their insertion order. |
| `OrderedMultiSet<T>`                                         | A collection of objects maintained in sorted order. Duplicate objects are allowed. |
| `UnorderedMap<TKey, TValue>`<br />(`Dictionary<TKey, TValue>`) | A collection of key/value pairs stored in a hash table. It is a bit slower than `Dictionary<TKey, TValue>`, but it has a node index interface, allows a null key, and can be configured to allow duplicate keys. |
| `UnorderedSet<T>`                                            | A thin wrapper over `UnorderedMap<T, byte>` (the value is not used). Duplicate and null elements are supported when configured. |
| `UnorderedMapSlim<TKey, TValue>`                             | A lightweight hash map with minimal memory overhead. Keys must be non-null and no version check is performed. `GetValueRefOrAddDefault()` makes read-modify-write patterns cheap. |
| `SlidingList<T>`                                             | A fixed-capacity ring buffer whose elements are addressed by a stable **position** instead of an index. Useful for sliding windows (e.g. send/receive buffers). |
| `CircularQueue<T>`                                           | A thread-safe bounded circular queue (Vyukov-style MPMC). Faster than `ConcurrentQueue<T>` when a bounded capacity is acceptable. |
| `TemporaryList<TObject>`                                     | A `ref struct` list that keeps up to four objects inline without a heap allocation. Handy for collecting objects during a `foreach` and modifying them afterwards. |

### Keyed lookup

| Type                                                         | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `Int32Hashtable<TValue>`, `UInt32Hashtable<TValue>`<br />`Int64Hashtable<TValue>`, `UInt64Hashtable<TValue>`<br />`Utf8Hashtable<TValue>`, `Utf16Hashtable<TValue>` | Thread-safe hash tables. Writes are serialized with a lock while lookups are lock-free, so they are optimized for tables that are built infrequently and read frequently. |
| `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>`      | Lightweight, **not** thread-safe hash maps keyed by a UTF-8 (`byte[]`) or UTF-16 (`string`) key. Both `ReadOnlySpan` and array/string overloads are provided, and the key is materialized only when an entry is actually inserted. |

### Pools and buffers

| Type                                                         | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `ObjectPool<T>`                                              | A fast and thread-safe pool of objects (implemented with `CircularQueue<T>`). Objects implementing `IDisposable` are disposed when they cannot be pooled. |
| `KeyedObjectCache<TKey, TObject>`                            | A thread-safe LRU-style cache of expensive objects (e.g. cryptographic transforms) retrieved by key. |
| `BytePool`                                                   | A fast thread-safe pool of byte arrays. A rented array (`RentArray`) can be shared by reference counting and exposed as `RentMemory` / `RentReadOnlyMemory`. |
| `SpanOwner<T>`                                               | A `ref struct` that folds the "stackalloc if small, `ArrayPool` if large, return at the end" pattern into a single `using` declaration. |
| `SequenceBuilder<T>`                                         | Builds a `ReadOnlySequence<T>` from pooled array chunks.      |
| `PooledStringBuilder`                                        | Builds a string using pooled character chunks, aiming for performance comparable to string interpolation. |

### Helpers

| Type                                                         | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `XxHash3Slim`                                                | A slim, allocation-free XXH3 (64-bit) implementation, ported from `System.IO.Hashing`. |
| `CollectionHelper`                                           | Capacity calculation helpers (power-of-two and prime sizing). |
| `TagObject`                                                  | Cached objects for integer tags in the range 0-255, so a tag can be passed as an `object` without boxing. |
| `Arc.BaseHelper`                                             | Span/string helpers: line splitting, separator scanning, decimal digit counting, SIMD byte sums, UTF-8 validation, resource loading. |
| `Arc.Struct128`, `Arc.Struct256`                             | 128-bit and 256-bit value types for handling fixed-size binary data. |
| `Arc.IStringConvertible<T>`, `Arc.IUtf8Convertible<T>`       | Contracts for converting an object to and from a UTF-16 / UTF-8 representation without allocating. |
| `Arc.VersionHelper`, `Arc.AppCloseHandler`                   | Assembly version information and an application close (process exit / console close) handler. |



## Performance

`OrderedSet<T>` uses the same tree structure (Red-Black Tree) as `SortedSet<T>`. The difference is that
`OrderedSet<T>` has a link to a parent node and is overall faster than `SortedSet<T>`.

The numbers below were measured with BenchmarkDotNet against `System.Collections.Generic.SortedSet<T>`.
Run the `Benchmark` project to reproduce them on your machine.

| Method                      | Length    |                Mean |            Error |           StdDev |              Median |       Gen 0 |    Allocated |
| --------------------------- | --------- | ------------------: | ---------------: | ---------------: | ------------------: | ----------: | -----------: |
| **NewAndAdd_SortedSet**     | **100**   |     **4,160.11 ns** |    **16.214 ns** |    **22.730 ns** |     **4,157.33 ns** |  **1.0223** |   **4288 B** |
| NewAndAdd_OrderedSet        | 100       |         3,384.44 ns |         8.101 ns |        12.126 ns |         3,384.49 ns |      1.4381 |       6024 B |
| NewAndAdd2_SortedSet        | 100       |         8,709.41 ns |       151.310 ns |       221.788 ns |         8,551.29 ns |      1.8463 |       7776 B |
| NewAndAdd2_OrderedSet       | 100       |         8,042.45 ns |        53.162 ns |        79.570 ns |         8,043.79 ns |      2.0599 |       8664 B |
| AddRemove_SortedSet         | 100       |           422.21 ns |         0.637 ns |         0.934 ns |           421.94 ns |      0.0381 |        160 B |
| AddRemove_OrderedSet        | 100       |           172.03 ns |         0.423 ns |         0.593 ns |           171.93 ns |      0.0534 |        224 B |
| AddRemoveNode_OrderedSet    | 100       |           128.04 ns |         0.327 ns |         0.469 ns |           127.89 ns |      0.0534 |        224 B |
| AddRemoveReuse_OrderedSet   | 100       |           118.24 ns |         0.239 ns |         0.335 ns |           118.13 ns |           - |            - |
| AddRemoveReplace_OrderedSet | 100       |            11.76 ns |         0.211 ns |         0.289 ns |            11.54 ns |           - |            - |
| Enumerate_SortedSet         | 100       |         1,664.30 ns |        17.294 ns |        25.349 ns |         1,682.97 ns |      0.0401 |        168 B |
| Enumerate_OrderedSet        | 100       |         1,218.03 ns |         4.344 ns |         6.230 ns |         1,219.51 ns |      0.0114 |         48 B |



## Choosing a collection

The features of the various collections. Please use them well.

| Name                          | Structure   | Access   | Add      | Remove   | Search   | Sort       | Enum.    |
| ----------------------------- | ----------- | -------- | -------- | -------- | -------- | ---------- | -------- |
| `UnorderedList<T>`            | Array       | Index    | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |
| `UnorderedLinkedList<T>`      | Linked list | Node     | O(1)     | O(1)     | O(n)     | -          | O(1)     |
| `OrderedList<T>`              | Array       | Index    | O(n)     | O(n)     | O(log n) | Sorted     | O(1)     |
| `OrderedKeyValueList<K, V>`   | Array       | Index    | O(n)     | O(n)     | O(log n) | Sorted     | O(1)     |
| `OrderedMap<K, V>`            | RB Tree     | Node     | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `OrderedSet<T>`               | RB Tree     | Node     | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `OrderedMultiMap<K, V>`       | RB Tree     | Node     | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `OrderedMultiSet<T>`          | RB Tree     | Node     | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `UnorderedMap<K, V>`          | Hash table  | Node     | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| `UnorderedSet<T>`             | Hash table  | Node     | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| `UnorderedMapSlim<K, V>`      | Hash table  | Index    | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| `SlidingList<T>`              | Ring buffer | Position | O(1)     | O(1)     | O(n)     | No         | O(1)     |
| `CircularQueue<T>`            | Ring buffer | FIFO     | O(1)     | O(1)     | -        | No         | -        |

- Ordered collections require `IComparable<T>` or `IComparer<T>`.
- Unordered collections (e.g. `UnorderedMap<TKey, TValue>`) are based on hash tables, which require
  `IEquatable<T>`/`GetHashCode()` or `IEqualityComparer<T>`.
- `Multi` collections allow duplicate keys. `UnorderedMap<TKey, TValue>` and `UnorderedSet<T>` also
  accept duplicate keys when constructed with `allowDuplicate: true`.
- `OrderedMap` uses a Red-Black tree and is faster than `OrderedKeyValueList<TKey, TValue>` in most
  situations. For this reason, I recommend using `OrderedMap<TKey, TValue>` over
  `OrderedKeyValueList<TKey, TValue>` unless index access is absolutely necessary.
- `Add` on `UnorderedList<T>` is O(1) amortized; a single call may resize the internal array.



## Notes on thread safety

Unless stated otherwise, the collections are **not** thread-safe: multiple readers are fine only while
the instance is not being modified, and any writer requires external mutual exclusion.

The exceptions are:

- `CircularQueue<T>`, `ObjectPool<T>`, `BytePool` and `KeyedObjectCache<TKey, TObject>`, which are
  thread-safe for their documented operations (`ObjectPool<T>.Dispose` must not run concurrently with
  `Rent`/`Return`).
- The `*Hashtable<TValue>` types, whose writes are serialized internally and whose lookups are lock-free.

`UnorderedMapSlim<TKey, TValue>`, `Utf8UnorderedMap<TValue>` and `Utf16UnorderedMap<TValue>` perform no
version check at all, so modifying them while enumerating is undefined behavior rather than an exception.
