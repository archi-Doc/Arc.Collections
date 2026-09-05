# Arc.Collections

![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

日本語ドキュメントは[こちら](doc/README.jp.md)

Arc.Collections is a C# collection library with node-based access, sorted and duplicate-key
collections, specialized hash tables, and pooling, buffer, and hashing utilities.

The current source targets **.NET 10**. Most types are in `Arc.Collections`; general helpers are
in `Arc`, and specialized comparison contracts are in `Arc.Collections.HotMethod`.
Interfaces and mutation semantics vary by collection; these are not universally drop-in
replacements for `System.Collections.Generic`.

I know it's reinventing the wheels, but these classes are necessary for implementing [CrossLink](https://github.com/archi-Doc/CrossLink). And reinventing the wheels is a kind of fun for me :)

## Quick start

Install the package in your application:

```sh
dotnet add package Arc.Collections
```

```csharp
using System;
using Arc.Collections;

var values = new OrderedSet<int>(new[] { 2, 1, 3 });
values.Add(4);
values.Add(0);
Console.WriteLine(string.Join(", ", values)); // 0, 1, 2, 3, 4

var map = new OrderedMap<int, string>();
var (node, added) = map.Add(2, "two");
map.Add(1, "one");
Console.WriteLine(node.Previous!.Value); // one
map.RemoveNode(node);
```

## Collections

### Lists, trees, and hash maps

| Type | Use and behavior |
| --- | --- |
| `UnorderedList<T>` | Resizable array with indexed access and writable `AsSpan()`. Preserves insertion order; elements are not sorted. |
| `UnorderedLinkedList<T>` | Doubly linked list with node navigation, insertion, removal, and reuse. |
| `OrderedList<T>` | Sorted array with indexed access and binary-search bounds. Equal elements retain insertion order. |
| `OrderedKeyValueList<TKey, TValue>` | Sorted key/value arrays with indexed key/value views. Allows duplicate keys; rejects null keys. |
| `OrderedMap<TKey, TValue>` | Red-black tree with unique keys, node navigation, range endpoints, and node reuse. Supports null keys. |
| `OrderedSet<T>` | Sorted unique elements, backed by `OrderedMap<T, byte>`. |
| `OrderedMultiMap<TKey, TValue>` | Sorted tree with insertion-ordered duplicate groups and matching-node/value enumeration. Supports null keys. |
| `OrderedMultiSet<T>` | Sorted elements with insertion-ordered duplicates, backed by `OrderedMultiMap<T, byte>`. |
| `UnorderedMap<TKey, TValue>` | Hash map with node indexes, null keys, and optional duplicates. Enumeration order is unspecified. |
| `UnorderedSet<T>` | Hash-based elements with null support and optional duplicates, backed by `UnorderedMap<T, byte>`. |
| `UnorderedMapSlim<TKey, TValue>` | Compact hash map for non-null keys, with direct value-reference access and no enumeration version checks. |

Ordered collections use a supplied comparer or `Comparer<T>.Default`. Tree collections also
support reverse ordering. Hash collections use their supported equality comparer; equal keys
must have equal hash codes. Keep keys' comparison and hash behavior unchanged while stored.

For sorted arrays, lookup is O(log n), but insertion and removal may shift O(n) elements.
Tree lookup, insertion, and removal are O(log n). Hash lookup is O(1) on average and O(n) in
the worst case. Appending to `UnorderedList<T>` is amortized O(1); linked-list insertion and
removal are O(1) when the node is already known. Full enumeration visits all live elements;
array-backed hash maps may also scan vacant slots.

### Duplicate keys and assignment

| Type | `Add` with an existing key | Indexer assignment |
| --- | --- | --- |
| `OrderedMap<TKey, TValue>` | Returns the existing node with `NewlyAdded == false`; keeps the value. | Replaces the value. |
| `OrderedMultiMap<TKey, TValue>` | Adds a duplicate. | Adds a duplicate; lookup returns the first matching value. |
| `OrderedKeyValueList<TKey, TValue>` | Adds a duplicate. | Adds a duplicate; lookup returns the first matching value. |
| `UnorderedMap<TKey, TValue>` | Keeps the existing value unless `allowDuplicate: true`. | Updates one matching entry or adds a new entry. |
| `UnorderedMapSlim<TKey, TValue>` and UTF unordered maps | Replaces the value. | Replaces the value. |

`OrderedSet<T>` ignores duplicates; `OrderedMultiSet<T>` retains them.
`UnorderedMap<TKey, TValue>` and `UnorderedSet<T>` enable duplicates with
`allowDuplicate: true`; null support does not require that option.

`OrderedList<T>` rejects positional insertion and indexer assignment. Mutating it through
an `UnorderedList<T>` reference or its writable span can break sorting. Do not retain
list spans or map value references across mutations that can move or replace their storage.

Node handles belong to their collection. Remove a node before reusing it, and do not use
a removed node or hash-map node index as a live handle. Hash-map indexes may be recycled.

### Windows, queues, and temporary storage

| Type | Use and behavior |
| --- | --- |
| `SlidingList<T>` | Bounded, explicitly resizable ring buffer for non-null reference values. Uses stable positions instead of zero-based indexes. |
| `CircularQueue<T>` | Thread-safe bounded FIFO queue for multiple producers and consumers. |
| `TemporaryList<TObject>` | `ref struct` list with four inline elements; additional elements use a heap-allocated list. |

`SlidingList<T>.Add` returns a position, or -1 when no slot is available. Positions wrap
modulo 2³¹. Its `IList<T>` operations also use positions. Middle removals leave holes:
`Consumed` includes those holes, while `ICollection<T>.Count` and enumeration count only
live elements. `TrySlide()` advances past empty leading slots; `Resize()` changes capacity
when the current window fits.

`CircularQueue<T>` rounds capacity up to a power of two and clamps it between 2 and 2³⁰.
`TryEnqueue` can fail when full; `TryDequeue` can fail when empty. Concurrent operations
may retry or wait, and `Count` is an estimate during concurrent access.

## Specialized keyed lookup

| Type | Keys and concurrency |
| --- | --- |
| `Int32Hashtable<TValue>`, `UInt32Hashtable<TValue>` | Signed or unsigned 32-bit keys. Serialized writes and lock-free lookups. |
| `Int64Hashtable<TValue>`, `UInt64Hashtable<TValue>` | Signed or unsigned 64-bit keys. Serialized writes and lock-free lookups. |
| `Utf8Hashtable<TValue>`, `Utf16Hashtable<TValue>` | UTF-8 byte or UTF-16 character keys, with span-based lookup. Serialized writes and lock-free lookups. |
| `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>` | UTF-keyed maps with span lookup, enumeration, and direct value-reference access. Require external synchronization for writes. |

All these types update existing values with `Add`; `TryAdd` leaves existing values unchanged.
The hashtables also provide `GetOrAdd`. Its factory runs under the write lock and must not
reenter the same table.

UTF keys are compared by ordinal content without validation, normalization, or case folding.
Span-based insertion materializes a key only for a new entry. Array/string overloads retain
the supplied key; **do not modify stored UTF-8 byte arrays**.

```csharp
using System;
using Arc.Collections;

var names = new Utf8UnorderedMap<int>();
names.Add("alice"u8, 1);
names.Add("alice"u8, 2); // Updates the existing entry.
Console.WriteLine(names.TryGetValue("alice"u8, out var id) ? id : -1); // 2
```

## Pools and buffers

| Type | Purpose and lifetime |
| --- | --- |
| `ObjectPool<T>` | Reuses objects from a caller-supplied factory. Rejected or still-pooled `IDisposable` objects are disposed. |
| `KeyedObjectCache<TKey, TObject>` | Caches reusable objects by key. Retrieval removes the object and transfers ownership to the caller. |
| `BytePool` | Pools byte arrays with reference-counted `RentArray`, `RentMemory`, and `RentReadOnlyMemory` handles. |
| `SpanOwner<T>` | Uses a supplied scratch span when it fits, otherwise rents from `ArrayPool<T>.Shared`. Dispose to return a rented array. |
| `SequenceBuilder<T>` | Builds a pooled `ReadOnlySequence<T>`. Finalization prevents further additions; the sequence is valid until builder disposal. |
| `PooledStringBuilder` | Builds strings from pooled character chunks. `ToString()` creates an independent string; dispose to return pooled resources. |

Return each rented object once. An `ObjectPool<T>` factory must be thread-safe and create
distinct instances. Return outstanding objects before disposing the pool; `Rent` and
`Return` throw after disposal.

`KeyedObjectCache<TKey, TObject>` retains at most one object per key and evicts the oldest
cached object when full. A failed `Cache` call leaves ownership with the caller.
`CreateInterface` creates a lease that returns an object on disposal, or disposes it if
caching fails. This readonly lease retains its fields after return: return or dispose it
once, including across copies.

### Byte ownership

`BytePool.CreateFlat` gives every bucket the same retention limit; `CreateExponential`
retains more arrays in smaller buckets. Array sizes and retention limits are rounded up
to powers of two, with at least two retained slots per bucket. Requests outside configured
buckets allocate unpooled arrays. Configure `SetPoolLimit` before sharing the pool.

A rent starts with one owned reference. Copying a handle, slicing, or calling `AsMemory`,
`AsReadOnly`, or `ReadOnly` does **not** add an owner. Use `IncrementAndShare` for another
owner and return each owned reference once. After the final return, every old handle and
view is invalid because the pooled owner and array may be reused. Arrays are not cleared.

```csharp
using Arc.Collections;

using var rented = BytePool.Default.Rent(128);
var view = rented.AsMemory(0, 128); // Shares rented's ownership.
view.Span.Clear();
using var shared = view.IncrementAndShare(); // Owns one additional reference.
shared.Span[0] = 42;
```

`RentMemory.CreateFrom(byte[])` and its read-only counterpart track an unpooled array.
Their `CreateFrom(ReadOnlyMemory<byte>)` overloads instead create an untracked view of
array-backed memory, or return an empty view if the underlying array cannot be obtained.

### Temporary buffers and builders

Do not copy a `SpanOwner<T>`, `SequenceBuilder<T>`, or `PooledStringBuilder` while it owns
pooled resources. Dispose the owner after use and do not retain pooled spans or sequences
past disposal. `SpanOwner<T>` clears reference-containing arrays on return.

```csharp
using System;
using Arc.Collections;

int length = 300;
using var owner = new SpanOwner<byte>(stackalloc byte[256], length);
owner.Span.Clear();

using var builder = new SequenceBuilder<int>();
builder.AddRange(new[] { 1, 2, 3 });
var sequence = builder.ToReadOnlySequence();
Console.WriteLine(sequence.Length); // 3; consume before builder disposal.
```

`PooledStringBuilder` uses invariant formatting by default and LF for `AppendLine`.
`Clear()` retains its current character buffer for reuse.

## Helpers

| Type or namespace | Purpose |
| --- | --- |
| `XxHash3Slim` | Allocation-free, non-cryptographic XXH3 64-bit hashing, ported from `System.IO.Hashing.XxHash3`. |
| `CollectionHelper` | Power-of-two and prime capacity calculations. |
| `TagObject` | Cached object instances for tags 0–255, avoiding boxing. |
| `Arc.BaseHelper` | Span and text helpers, line/separator scanning, decimal digit counts, SIMD byte sums, and resource loading. `GetValidUtf8Length` estimates a trailing boundary; it does not validate UTF-8. |
| `Arc.Struct128`, `Arc.Struct256` | Fixed-size binary values with overlapping numeric fields. Byte conversion uses native byte order; comparison uses signed 64-bit fields in field order. |
| `Arc.IStringConvertible<T>`, `Arc.IUtf8Convertible<T>` | Span-based UTF-16/UTF-8 parsing and formatting contracts. |
| `Arc.IConversionOptions` | Typed options for parsing and formatting. |
| `Arc.VersionHelper` | Version information for the entry assembly or a selected loaded assembly. `SetAssembly` matches an ordinal name substring. |
| `Arc.AppCloseHandler` | Registers one handler, invoked at most once for process exit or a Windows console close event. |
| `Arc.Collections.HotMethod` | `IHotMethod`, `IHotMethod<T>`, `IHotMethod2`, `IHotMethod2<TKey, TValue>`, `IHotMethodResolver`, and `HotMethodResolver` for specialized span bounds and tree searches. The built-in resolver supports selected types with default comparers. |

Conversion interfaces require all members to be implemented. At least one of
`GetStringLength()` and `MaxStringLength` must provide a nonnegative length; the other
may return -1. Lengths and written counts use UTF-16 characters or UTF-8 bytes respectively.
Formatting options may require additional destination space.

## Thread safety

Unless documented otherwise, collections require external synchronization whenever a writer
is present. Multiple readers are supported only while the instance is unchanged.

- `CircularQueue<T>` supports concurrent producers and consumers.
- `ObjectPool<T>.Rent` and `Return` are thread-safe; disposal must not overlap either operation.
- `BytePool` rental, return, and reference counting are thread-safe; `SetPoolLimit` is not.
  Reference counting does not synchronize reads and writes to the rented bytes.
- `KeyedObjectCache<TKey, TObject>` synchronizes cache operations; retrieved objects remain
  the caller's responsibility.
- `*Hashtable<TValue>` types serialize writes and provide lock-free lookups.

`UnorderedMapSlim<TKey, TValue>` and UTF unordered maps do not check enumeration versions.
Do not mutate them during enumeration. Struct enumerators avoid boxing when iterated directly;
enumeration through an interface may allocate.

## Build and test

From the repository root with the .NET 10 SDK installed:

```sh
dotnet restore Arc.Collections.slnx
dotnet build Arc.Collections.slnx -c Release --no-restore
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-restore
```

The test command uses the repository's Microsoft.Testing.Platform configuration.
`Benchmark` contains BenchmarkDotNet workloads; run a selected group with:

```sh
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*OrderedPublicTest*"
```

## Performance

Choose using the operations and data sizes your application needs, then benchmark that workload.
Node reuse can avoid node allocation, and spans or direct value references can avoid repeated
lookup or enumeration overhead.

The following retained BenchmarkDotNet results compare `OrderedSet<T>` with
`System.Collections.Generic.SortedSet<T>`. They are historical measurements; their runtime
and hardware context is not recorded here, so they do not establish current performance.
Run the benchmarks above to measure your environment.

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
