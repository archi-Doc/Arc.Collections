# Arc.Collections

![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

日本語ドキュメントは[こちら](doc/README.jp.md)

Arc.Collections is a C# collection library for **.NET 10**.
It provides sorted and duplicate-key collections with reusable nodes, hash maps with stable node indexes,
thread-safe hashtables with lock-free lookups, and pooling, buffer, and hashing utilities.
It is published on [NuGet](https://www.nuget.org/packages/Arc.Collections).

I know it's reinventing the wheel, but these classes are necessary for implementing [CrossLink](https://github.com/archi-Doc/CrossLink). And reinventing the wheel is kind of fun for me :)

Most types are in the `Arc.Collections` namespace. General helpers are in `Arc`, and specialized search contracts are in `Arc.Collections.HotMethod`.
The collections are not drop-in replacements for `System.Collections.Generic`: their interfaces, duplicate handling, and mutation rules differ.

## Quick start

```sh
dotnet add package Arc.Collections
```

```csharp
using System;
using Arc.Collections;

var set = new OrderedSet<int>(new[] { 2, 1, 3 });
set.Add(0);
Console.WriteLine(string.Join(", ", set)); // 0, 1, 2, 3

var map = new OrderedMap<int, string>();
var (node, added) = map.Add(2, "two"); // Returns the node and whether it was added.
map.Add(1, "one");
Console.WriteLine(node.Previous!.Value); // one

map.RemoveNode(node); // The node is now unused.
map.Add(3, "three", node); // Reuses the node instead of allocating one.
Console.WriteLine(string.Join(", ", map.Keys)); // 1, 3
```

## Choosing a collection

| Type | Structure | Order | Duplicates | Nulls | Access by | Similar to |
| --- | --- | --- | --- | --- | --- | --- |
| `UnorderedList<T>` | Resizable array | Insertion | Yes | Yes | Index | `List<T>` |
| `OrderedList<T>` | Sorted array | Comparer | Yes | Yes | Index | |
| `OrderedKeyValueList<TKey, TValue>` | Sorted parallel arrays | Key comparer | Yes | No | Index | `SortedList<TKey, TValue>` |
| `OrderedMap<TKey, TValue>` | Red-black tree | Key comparer, optionally reversed | No | Yes | Node | `SortedDictionary<TKey, TValue>` |
| `OrderedSet<T>` | `OrderedMap<T, byte>` | Comparer, optionally reversed | No | Yes | Node | `SortedSet<T>` |
| `OrderedMultiMap<TKey, TValue>` | Red-black tree with duplicate lists | Key comparer, optionally reversed | Yes | Yes | Node | |
| `OrderedMultiSet<T>` | `OrderedMultiMap<T, byte>` | Comparer, optionally reversed | Yes | Yes | Node | |
| `UnorderedMap<TKey, TValue>` | Hash table | Unspecified | Optional | Yes | Node index | `Dictionary<TKey, TValue>` |
| `UnorderedSet<T>` | `UnorderedMap<T, byte>` | Unspecified | Optional | Yes | | `HashSet<T>` |
| `UnorderedMapSlim<TKey, TValue>` | Compact hash table | Unspecified | No | No | `ref` to value | `Dictionary<TKey, TValue>` |
| `UnorderedLinkedList<T>` | Doubly linked list | Insertion or explicit | Yes | Yes | Node | `LinkedList<T>` |
| `SlidingList<T>` | Ring buffer of references | Position | Yes | No | Position | |
| `CircularQueue<T>` | Bounded ring buffer | FIFO | Yes | Yes | | `ConcurrentQueue<T>` (bounded) |
| `TemporaryList<T>` | `ref struct` with four inline slots | Insertion | Yes | Yes | | |

- Where nulls are not supported, storing a null key or element throws `ArgumentNullException`.
- Only `OrderedKeyValueList<TKey, TValue>` implements `IDictionary<TKey, TValue>`. `UnorderedList<T>`, `OrderedList<T>`, and `SlidingList<T>` implement `IList<T>`,
  and `UnorderedLinkedList<T>` implements `ICollection<T>`. The other maps and sets implement only `IEnumerable<T>`.
- For thread-safe or UTF-keyed lookups, see [Hashtables and UTF-keyed maps](#hashtables-and-utf-keyed-maps).

## Behavior

### Ordering and comparers

- Sorted collections use the supplied `IComparer<T>` or `Comparer<T>.Default`, and treat keys as equal when it returns 0 (`Equals` is not used).
- The tree collections (`OrderedMap<TKey, TValue>`, `OrderedSet<T>`, `OrderedMultiMap<TKey, TValue>`, `OrderedMultiSet<T>`) accept `reverse: true` for descending order. The sorted lists do not.
- With the default comparer, null sorts first, or last in a reversed tree. A custom comparer receives null keys and decides their order.
- With the default comparer, a key whose declared type `T` is an interface or base class is still compared through its runtime `IComparable<T>` implementation. The `Comparer` property (`Comparer<T>.Default`) ignores that implementation and may throw or order such keys differently.
- `GetLowerBound(key)` returns the first element at or after the key in collection order, and `GetUpperBound(key)` the last element at or before it.
  The sorted lists return an index (-1 if none), and the trees return a node (null if none).
- Among the hash-based types, only `UnorderedMap<TKey, TValue>` and `UnorderedSet<T>` accept an `IEqualityComparer<T>`.
- Do not change a stored key in a way that changes its comparison result or hash code.

### Duplicates and assignment

| Type | Adding an existing key | Indexer setter with an existing key |
| --- | --- | --- |
| `OrderedMap<TKey, TValue>`, `UnorderedMap<TKey, TValue>` | `Add` returns the existing node (or node index) with `NewlyAdded == false` and keeps the value. | Replaces the value. |
| `UnorderedMap<TKey, TValue>` with `allowDuplicates: true` | `Add` adds an entry. | Replaces the value of one matching entry. |
| `OrderedMultiMap<TKey, TValue>`, `OrderedKeyValueList<TKey, TValue>` | `Add` adds an entry after the existing entries with that key. | Adds an entry; never replaces. |
| `UnorderedMapSlim<TKey, TValue>`, UTF-keyed maps, hashtables | No `Add`. `TryAdd` returns `false` and keeps the value; `AddOrUpdate` replaces it. | Replaces the value (maps only). |

- Indexer setters add a missing key. `OrderedSet<T>.Add` returns `false` for an existing element, as does `UnorderedSet<T>.Add` unless duplicates are allowed.
  `OrderedMultiSet<T>` and `OrderedList<T>` insert after the existing equal elements.
- When a key has duplicates, single-entry operations (`TryGetValue`, `FindFirstNode`, `IndexOf`, `IndexOfKey`, `Remove`, the indexer getter)
  use the first match in collection order for sorted collections, and an unspecified match for `UnorderedMap<TKey, TValue>`.

### Nodes and node indexes

- A tree node is valid while it is in its collection. Trees do not check ownership: passing an active node from another tree to `RemoveNode` or `SetNodeKey` corrupts both trees.
- `RemoveNode` and `Clear` make nodes unused (`OrderedMap<TKey, TValue>.Node.IsUnused` reports this).
  An unused node's `Previous` and `Next` return null, and `RemoveNode` ignores it.
- Pass an unused node as the `reuse` argument of `Add` (`AddNode` for `OrderedSet<T>`) to avoid an allocation.
  A node that is still in use is not reused. If `OrderedMap<TKey, TValue>` or `OrderedSet<T>` already has the key, `reuse` is left untouched.
- `UnorderedMap<TKey, TValue>` node indexes stay valid across resizing. A removed index can be reused by a later entry, and `Clear` invalidates every index.
- `UnorderedLinkedList<T>` rejects nodes from other lists. `Remove(node)` and `Clear` detach nodes, which can be added again without allocating.

### Enumeration

- Modifying a collection makes its enumerators throw `InvalidOperationException`, with these exceptions:
  - Writes through a node's `UnsafeChangeValue` or `ValueRef`, `UnorderedMap<TKey, TValue>.UnsafeChangeValue`, `OrderedMultiMap<TKey, TValue>.SetNodeValue`,
    `UnorderedList<T>.AsSpan()`, or the array returned by `UnorderedMap<TKey, TValue>.UnsafeGetNodes()` are not detected.
  - `UnorderedMapSlim<TKey, TValue>`, the UTF-keyed maps, and `TemporaryList<T>` never detect modification; do not modify them while enumerating.
- `GetEnumerator` returns a struct. So do `Keys` and `Values` (except on `OrderedKeyValueList<TKey, TValue>`), `EnumerateNodes`, and `EnumerateValues`.
  `foreach` over the concrete type does not allocate; enumerating through an interface boxes the enumerator.

### Complexity

| Type | Lookup | Insertion | Removal |
| --- | --- | --- | --- |
| `UnorderedList<T>` | O(1) by index; O(n) search | O(1) amortized append; O(n) insert | O(n) |
| `OrderedList<T>`, `OrderedKeyValueList<TKey, TValue>` | O(log n) | O(log n) comparisons, O(n) moves | O(n) |
| Tree collections | O(log n) | O(log n) | O(log n) |
| Hash maps and sets, hashtables | O(1) average, O(n) worst case | O(1) amortized | O(1) average |
| `UnorderedLinkedList<T>` | O(n) search | O(1) next to a known node | O(1) given the node |
| `SlidingList<T>` | O(1) by position; O(n) search | O(1) | O(1); removing the head also skips any holes |

- Operations that scan a key's d duplicates cost O(log n + d), for example `OrderedMultiMap<TKey, TValue>.FindNode(key, value)` and `OrderedMultiSet<T>.GetCount`.
- `OrderedList<T>.AddRange` with an `ICollection<T>`, array, or span sorts and merges the batch: O(m log m + m log n) comparisons and O(n + m) moves. Other sequences are added one element at a time.

## Collection notes

### Lists

- `UnorderedList<T>.AsSpan()` exposes the backing array. The span is invalid after the list adds or removes elements or changes capacity.
- `OrderedList<T>` derives from `UnorderedList<T>`. Its `Insert` and indexer setter throw `InvalidOperationException`.
  Writes through an `UnorderedList<T>` reference or `AsSpan()` bypass the sort order, and the base `Contains`, `IndexOf`, and `Remove` search linearly with `EqualityComparer<T>.Default`.
- `OrderedKeyValueList<TKey, TValue>.Keys` and `Values` are live, read-only `IList<T>` views.
- Copying an `OrderedKeyValueList<TKey, TValue>` with the same comparer instance keeps the duplicate order; otherwise duplicate order is unspecified.
  The single-argument copy constructor uses `Comparer<TKey>.Default`, so pass the source's `Comparer` when it is custom.

### Trees

- `GetRange(lower, upper)` returns the first and last nodes in the range. In a reversed tree, `lower` is the larger key.
- `OrderedMultiMap<TKey, TValue>.EnumerateValues(key)` and `EnumerateNodes(key)` visit a key's entries in insertion order.
  `FindNode(key, value)` and `Remove(key, value)` match values with `EqualityComparer<TValue>.Default`.

### Hash maps

- `UnorderedMap<TKey, TValue>` never passes null keys to its comparer, and `Clear` keeps the capacity.
- `GetValueRefOrAddDefault` (`UnorderedMapSlim<TKey, TValue>` and the UTF-keyed maps) returns a reference to the value slot with one lookup;
  the reference is invalid after an add, a removal, or `Clear`.

### SlidingList\<T\>

`SlidingList<T>` is a ring buffer whose elements are addressed by **position**, not by zero-based index.

- `Add` returns the new element's position, or -1 when the list is full.
- Positions wrap to 0 after `int.MaxValue`. Valid positions are `[StartPosition, StartPosition + Capacity)`, modulo 2^31.
- Removing a middle element leaves a hole that still uses a slot: `UsedSlotCount` counts it, `Count` does not.
  Removing the head, `Slide()`, and `GetFirstOrDefault()` move the window past holes, so `GetFirstOrDefault()` is a write: it can invalidate enumerators.
- The `IList<T>` members take positions. `Insert` overwrites a slot, and `ICollection<T>.Add` drops the element when the list is full.
  Index-based LINQ operators such as `ElementAt` return wrong results unless `StartPosition` is 0 and there are no holes.
- `Clear()` starts the new window at the former `EndPosition`. `Resize(capacity)` keeps positions; it returns `false` and changes nothing when `capacity` is less than `UsedSlotCount`.

```csharp
using System;
using Arc.Collections;

var window = new SlidingList<string>(3);
var a = window.Add("a"); // a == 0
var b = window.Add("b"); // b == 1
window.Add("c"); // Position 2.
Console.WriteLine(window.Add("d")); // -1 (full)

window.TryRemoveAt(b); // Leaves a hole at position 1.
Console.WriteLine(window.CanAdd); // False: the hole still uses a slot.
window.TryRemoveAt(a); // Removes the head and slides past the hole.
Console.WriteLine(window.StartPosition); // 2
Console.WriteLine(window.Add("d")); // 3
Console.WriteLine(window[3]); // d (the argument is a position)
```

### Other collections

- `CircularQueue<T>` has a fixed capacity, rounded up to a power of two between 2 and 2^30. Under concurrent access, `Count` is an estimate.
  `TryEnqueue` returns `false` when the queue is full, or briefly while a concurrent dequeue finishes; `TryDequeue` returns `false` when it is empty.
- `TemporaryList<T>` stores elements after the fourth in a heap-allocated `List<T>`. It needs no disposal, but do not copy it: copies share that list.

## Hashtables and UTF-keyed maps

| Type | Keys | Notes |
| --- | --- | --- |
| `Int32Hashtable<TValue>`, `UInt32Hashtable<TValue>`, `Int64Hashtable<TValue>`, `UInt64Hashtable<TValue>` | `int`, `uint`, `long`, `ulong` | Thread-safe. No indexer or enumerator. |
| `Utf8Hashtable<TValue>`, `Utf16Hashtable<TValue>` | UTF-8: `byte[]` or `ReadOnlySpan<byte>`. UTF-16: `string` or `ReadOnlySpan<char>`. | Thread-safe. No indexer or enumerator. |
| `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>` | Same as the UTF hashtables | Not thread-safe. Has an indexer, an enumerator, and `GetValueRefOrAddDefault`. |

- Hashtable writes and the `ToArray` and `ToKeyValuePairs` snapshots take the write lock.
  Lookups are lock-free and see each key as it was before or after a concurrent write; `Count` may briefly lag behind them.
- The `GetOrAdd` factory runs under the write lock and must not modify the same table. A change that invalidates the pending insertion (same bucket, growth, or `Clear`) throws `InvalidOperationException`; other changes are not detected.
- Numeric keys are bucketed by their low-order bits (64-bit keys first XOR their two halves), so regular patterns such as multiples of a large power of two can share a bucket.
- UTF keys are compared by ordinal content, without validation, normalization, or case folding.
- A span key is copied only when a new entry is added. A `byte[]` or `string` key is stored by reference, not copied:
  **do not modify a stored `byte[]` key**, including arrays returned by enumeration or `ToKeyValuePairs`.

```csharp
using System;
using Arc.Collections;

var ids = new Utf8UnorderedMap<int>();
ids.AddOrUpdate("alice"u8, 1); // Copies the span into a new key array.
ids.AddOrUpdate("alice"u8, 2); // Updates the existing entry.
Console.WriteLine(ids.TryAdd("alice"u8, 3)); // False
Console.WriteLine(ids.TryGetValue("alice"u8, out var id) ? id : -1); // 2
```

## Pools and buffers

| Type | Purpose |
| --- | --- |
| `ObjectPool<T>` | Reuses objects created by a caller-supplied factory. |
| `KeyedObjectCache<TKey, TObject>` | Holds at most one object per key and evicts the oldest object when full. |
| `BytePool` | Pools byte arrays in power-of-two buckets and tracks ownership with reference-counted handles. |
| `SpanOwner<T>` | Uses a caller-supplied scratch span when it is large enough, and otherwise rents from `ArrayPool<T>.Shared`. |
| `SequenceBuilder<T>` | Builds a `ReadOnlySequence<T>` from pooled chunks. |
| `PooledStringBuilder` | Builds a string from pooled character chunks. |

### ObjectPool\<T\> and KeyedObjectCache\<TKey, TObject\>

- `ObjectPool<T>.PoolSize` is rounded up to a power of two (default 32). The factory must be thread-safe and return a new instance each time.
- `Return` disposes an `IDisposable` object when the pool is full, and `Dispose` disposes the pooled objects.
  Return each object only once (a second return is not detected) and before disposing the pool; afterward, `Rent` and `Return` throw `ObjectDisposedException`.
- `KeyedObjectCache<TKey, TObject>.TakeOrDefault(key)` transfers ownership of the cached object to the caller. `TryAdd(key, obj)` transfers ownership to the cache only when it returns `true`.
- The cache disposes evicted objects under its lock. Their `Dispose` must not wait for another thread that uses the cache.
- `CreateLease(key, obj)` returns a `Lease` that offers the object to the cache when disposed, and disposes the object if the cache rejects it.
  Dispose a lease only once; copies count as the same lease. `lease = lease.Return()` leaves an empty lease that is safe to dispose.

### BytePool

- `Rent(length)` returns a `RentedArray` with one owned reference. `RentedMemory` and `RentedReadOnlyMemory` are views of a range.
- A pooled array has its bucket's power-of-two length and is **not cleared**. A request of 0 or above the largest bucket gets an unpooled array.
- `BytePool.Default` pools arrays up to 16 MiB and retains up to about 200 MiB in total. `CreateExponential` and `CreateFlat` set other limits.
- Copying a handle and creating views (`AsMemory`, `AsReadOnlyMemory`, `ReadOnly`, `Slice`) do **not** add an owner.
  `IncrementAndShare` does. Return or dispose each owned reference exactly once.
- After the final return, every handle and view is invalid, because the array and its `RentedArray` can go to another renter.
- `CreateFrom(byte[])` on `RentedArray`, `RentedMemory`, or `RentedReadOnlyMemory` tracks an unpooled array with the same rules.
  `CreateFrom(ReadOnlyMemory<byte>)` creates an untracked view that needs no return; the view is empty if the memory is not array-backed.

```csharp
using System;
using Arc.Collections;

var rented = BytePool.Default.Rent(100);
Console.WriteLine(rented.Array.Length); // 128
var view = rented.AsMemory(0, 100); // A view, not an owner.
view.Span.Clear();
var shared = view.IncrementAndShare(); // A second owner.
Console.WriteLine(rented.ReferenceCount); // 2

rented.Return();
shared.Return(); // The final return: all handles and views are now invalid.
```

### Temporary buffers and builders

- `SpanOwner<T>`, `SequenceBuilder<T>`, and `PooledStringBuilder` are `ref struct` types that own pooled arrays.
  Do not copy them while they own resources, dispose them once, and do not use their spans or sequences afterward.
- `SpanOwner<T>` does not clear a rented array before use. `BaseHelper.StackallocThreshold` (1024) is the suggested size limit for a `stackalloc` scratch span.
- `SequenceBuilder<T>.ToReadOnlySequence()` finalizes the builder; later `Add` calls throw `InvalidOperationException`.
- `PooledStringBuilder` formats with the invariant culture unless you pass a provider; a null provider uses the current culture for numbers and dates. `AppendLine` appends `\n`.

```csharp
using System;
using Arc;
using Arc.Collections;

using var owner = new SpanOwner<byte>(stackalloc byte[BaseHelper.StackallocThreshold], 4096); // Rents: 4096 > 1024.
owner.Span.Clear();

using var text = new PooledStringBuilder();
text.Append("Total: ");
text.Append(1234.5);
Console.WriteLine(text.ToString()); // Total: 1234.5
```

## Helpers

| Type or namespace | Purpose |
| --- | --- |
| `XxHash3Slim` | Allocation-free, non-cryptographic XXH3 64-bit hashing; `Hash64(ReadOnlySpan<byte>, seed)` returns the same values as `System.IO.Hashing.XxHash3.HashToUInt64`. `Hash64(string)` hashes UTF-16 code units, not UTF-8. |
| `CollectionHelper` | Power-of-two capacity rounding and a prime table for sizing hash tables. |
| `TagObject` | One cached object per tag from 0 to 255, for storing a small tag in an `object` field without boxing. |
| `Arc.BaseHelper` | Line, text-scanning, number, UTF-8, conversion, and embedded-resource helpers. |
| `Arc.Struct128`, `Arc.Struct256` | Fixed-size binary values with overlapping fields. Bytes use native byte order; `CompareTo` compares the signed `long` fields in order. |
| `Arc.IStringConvertible<T>`, `Arc.IUtf8Convertible<T>`, `Arc.IConversionOptions` | Span-based UTF-16 and UTF-8 parsing and formatting contracts, with typed options. |
| `Arc.VersionHelper` | Version numbers of the entry assembly, or of a loaded assembly selected by name substring (`SetAssembly`). |
| `Arc.AppCloseHandler` | Runs the first registered handler at most once, on process exit or a Windows console close event (not on Ctrl+C). Later `Register` calls are ignored. |
| `Arc.Collections.HotMethod` | `HotMethodResolver` provides `IHotMethod<T>` and `IHotTreeMethod<TKey, TValue>` searches for the 8- to 128-bit integer types, `float`, `double`, and `DateTime`. `IHotMethod` and `IHotTreeMethod` are their non-generic markers. |

- `BaseHelper.GetCompleteUtf8Length` excludes an incomplete trailing sequence but does not validate the text.
  `TryLoadResource(null, name)` loads from the Arc.Collections assembly, not from the caller's.
- `BaseHelper.ConvertToString` and `ConvertToUtf8` return an empty result when no length is available or formatting fails. They do not retry with a larger buffer.
- In each conversion interface, at least one length member (`GetStringLength()` or `MaxStringLength`; `GetUtf8Length()` or `MaxUtf8Length`) must return a nonnegative length; the other may return -1.
- The sorted collections use the `HotMethod` searches automatically with the default comparer. The library never calls a custom `IHotMethodResolver`.

## Thread safety

| Type | Concurrent use |
| --- | --- |
| `CircularQueue<T>` | Multiple producers and consumers. |
| Numeric and UTF hashtables | Any mix of readers and writers. |
| `ObjectPool<T>` | `Rent` and `Return` are thread-safe. `Dispose` must not overlap them. |
| `KeyedObjectCache<TKey, TObject>` | All members are thread-safe. |
| `BytePool` and its handles | Renting, returning, and reference counting are thread-safe; `SetPoolLimit` is not. Access to the bytes is not synchronized. |
| `XxHash3Slim`, `HotMethodResolver`, `AppCloseHandler`, `VersionHelper` | Thread-safe, except `VersionHelper.SetAssembly`; call it at startup. |
| `SpanOwner<T>`, `SequenceBuilder<T>`, `PooledStringBuilder`, `TemporaryList<T>` | Not thread-safe; do not share an instance. |
| All other collections | Concurrent reads are safe while no thread writes; otherwise synchronize all access. `SlidingList<T>.GetFirstOrDefault()` is a write. |

## Build and test

From the repository root with the .NET 10 SDK installed:

```sh
dotnet restore Arc.Collections.slnx
dotnet build Arc.Collections.slnx -c Release --no-restore
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build
```

The tests use xUnit v3 on Microsoft.Testing.Platform; `dotnet run --project xUnitTest/xUnitTest.csproj -c Release --no-build -- --help` lists their options.
One test allocates about 2 GB and is skipped by default. Add `--explicit on` to the test command to include it, or `--explicit only` to run it alone.
To test the scalar fallbacks of the SIMD paths, set `DOTNET_EnableHWIntrinsic=0` in the environment.

Collect line and branch coverage of `Arc.Collections.dll` into `TestResults/coverage.cobertura.xml` (the same command as CI):

```sh
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build --coverage --coverage-settings xUnitTest/coverage.config --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --results-directory TestResults
```

## Performance

The `Benchmark` project contains BenchmarkDotNet workloads. List them with `--list flat` and select them with `--filter`:

```sh
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*OrderedPublicTest*"
```

The results below (`OrderedPublicTest`) compare `OrderedSet<T>` with `System.Collections.Generic.SortedSet<T>`.
They are historical: the runtime and hardware were not recorded, so they may not reflect current performance.

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

## License

Arc.Collections is released under the [MIT License](LICENSE).
