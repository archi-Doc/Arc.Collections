# Arc.Collections Renaming

Public API names changed after **1.46.0**. Only names changed; behavior is unchanged.

## Migration steps

1. Update the `Arc.Collections` package reference.
2. Build, then fix each compile error with the tables below.
3. Named arguments and tuple element names break only where you use them
   (for example `allowDuplicate:`, `minimumSize:`, `.Max`, `.Cmp`, `.Leaf`).
4. If you implement `IUtf8Convertible<T>` or `IHotMethodResolver`, rename the implemented members.
   A type implementing both `IStringConvertible<T>` and `IUtf8Convertible<T>` now provides
   `MaxStringLength`/`GetStringLength()` (UTF-16 chars) **and** `MaxUtf8Length`/`GetUtf8Length()` (UTF-8 bytes).

## Types

| Namespace | Old | New |
| --- | --- | --- |
| `Arc.Collections` | `BytePool.RentArray` | `BytePool.RentedArray` |
| `Arc.Collections` | `BytePool.RentMemory` | `BytePool.RentedMemory` |
| `Arc.Collections` | `BytePool.RentReadOnlyMemory` | `BytePool.RentedReadOnlyMemory` |
| `Arc.Collections` | `KeyedObjectCache<TKey, TObject>.Interface` | `KeyedObjectCache<TKey, TObject>.Lease` |
| `Arc.Collections.HotMethod` | `IHotMethod2` | `IHotTreeMethod` |
| `Arc.Collections.HotMethod` | `IHotMethod2<TKey, TValue>` | `IHotTreeMethod<TKey, TValue>` |
| `Arc.Collections` | `TemporaryList<TObject>` | `TemporaryList<T>` (type parameter name only) |

## Members

### Arc

| Type | Old | New | Note |
| --- | --- | --- | --- |
| `BaseHelper` | `SeparatorCharFlag` | `SeparatorCharFlags` | |
| `BaseHelper` | `RemoveCrLf(string)` | `RemoveCrAndLfChars(string)` | Removes every CR and LF. |
| `BaseHelper` | `GetValidUtf8Length(ReadOnlySpan<byte>)` | `GetCompleteUtf8Length(ReadOnlySpan<byte>)` | |
| `IUtf8Convertible<T>` | `MaxStringLength` | `MaxUtf8Length` | Implementers must rename. |
| `IUtf8Convertible<T>` | `GetStringLength()` | `GetUtf8Length()` | Implementers must rename. |
| `VersionHelper` | `Build` | `BuildVersion` | |
| `VersionHelper` | `VersionInt` | `EncodedVersion` | |
| `AppCloseHandler` | `Set(Action)` | `Register(Action)` | |
| `Struct128`, `Struct256` | `Name` (const) | `TypeName` | |

### Arc.Collections

| Type | Old | New | Note |
| --- | --- | --- | --- |
| `BytePool.RentedArray` | `Count` | `ReferenceCount` | |
| `BytePool.RentedArray` | `AsReadOnly()`, `AsReadOnly(int)`, `AsReadOnly(int, int)` | `AsReadOnlyMemory(...)` | |
| `BytePool.RentedArray`, `RentedMemory`, `RentedReadOnlyMemory` | `IsRent` | `IsRented` | |
| `BytePool.RentedMemory`, `RentedReadOnlyMemory` | `RentArray` (property) | `Owner` | |
| `KeyedObjectCache<TKey, TObject>` | `CreateInterface(TKey, TObject?)` | `CreateLease(TKey, TObject?)` | |
| `KeyedObjectCache<TKey, TObject>` | `TryGet(TKey)` | `TakeOrDefault(TKey)` | |
| `KeyedObjectCache<TKey, TObject>` | `Cache(TKey, TObject)` | `TryAdd(TKey, TObject)` | |
| `KeyedObjectCache<TKey, TObject>.Lease` | `ObjectCache` (field) | `Cache` | |
| `TagObject` | `MaxTag` | `TagCount` | Value is still 256. |
| `SlidingList<T>` | `TrySlide()` | `Slide()` | |
| `SlidingList<T>` | `Consumed` | `UsedSlotCount` | |
| `SlidingList<T>` | `FirstOrDefault` (property) | `GetFirstOrDefault()` (method) | Add `()`. |
| `SlidingList<T>` | `Get(int)` | `GetOrDefault(int)` | |
| `SlidingList<T>` | `Set(int, T)` | `TrySet(int, T)` | |
| `SlidingList<T>` | `Remove(int)` | `TryRemoveAt(int)` | `Remove(T)` is unchanged. |
| `UnorderedMapSlim<TKey, TValue>` | `Add(TKey, TValue)` | `AddOrUpdate(TKey, TValue)` | Still overwrites. `TryAdd` is unchanged. |
| `Int32Hashtable<TValue>`, `UInt32Hashtable<TValue>`, `Int64Hashtable<TValue>`, `UInt64Hashtable<TValue>` | `Add(key, value)` | `AddOrUpdate(key, value)` | Still overwrites. `TryAdd` and `GetOrAdd` are unchanged. |
| `Utf8Hashtable<TValue>`, `Utf16Hashtable<TValue>` | `Add(key, value)` (array/string and span overloads) | `AddOrUpdate(key, value)` | Still overwrites. `TryAdd` and `GetOrAdd` are unchanged. |
| `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>` | `Add(key, value)` (array/string and span overloads) | `AddOrUpdate(key, value)` | Still overwrites. `TryAdd` is unchanged. |
| `UnorderedMap<TKey, TValue>.Node` | `IsValid()` | `IsInUse()` | |
| `UnorderedMap<TKey, TValue>.Node` | `IsInvalid()` | `IsUnused()` | |
| `UnorderedMap<TKey, TValue>.Node` | `UnusedNode` (const) | `UnusedMarker` | |
| `UnorderedMapSlim<TKey, TValue>.Node` | `IsValid()` | `IsInUse()` | |
| `UnorderedMap`, `UnorderedMapSlim` | `UnsafeGetNodes()` tuple element `Max` | `SlotCount` | |
| `UnorderedMap`, `OrderedMultiMap`, `OrderedMultiSet` | `EnumerateNode(key)` | `EnumerateNodes(key)` | |
| `UnorderedMap`, `OrderedMultiMap` | `EnumerateValue(key)` | `EnumerateValues(key)` | |
| `UnorderedMap`, `UnorderedSet` | `AllowDuplicate` | `AllowDuplicates` | |
| `OrderedMap`, `OrderedMultiMap`, `OrderedSet`, `OrderedMultiSet` | `Reverse` | `IsReversed` | Constructor parameter `reverse` is unchanged. |
| `OrderedMap`, `OrderedMultiMap` | `HotMethod2` | `HotTreeMethod` | |
| `OrderedSet`, `OrderedMultiSet` | `SetNodeValue(node, value)` | `SetNodeKey(node, key)` | `SetNodeValue` on the maps is unchanged. |

### Arc.Collections.HotMethod

| Type | Old | New | Note |
| --- | --- | --- | --- |
| `IHotMethodResolver` | `TryGet<T>()` | `GetHotMethod<T>()` | Implementers must rename. |
| `IHotMethodResolver` | `TryGet<TKey, TValue>()` | `GetHotTreeMethod<TKey, TValue>()` | Implementers must rename. |
| `IHotTreeMethod<TKey, TValue>` | `SearchNode`/`SearchNodeReverse` tuple `(Cmp, Leaf)` | `(Comparison, Node)` | Implementers must rename tuple elements. |

## Parameters

These affect only named arguments.

| Member | Old | New |
| --- | --- | --- |
| `UnorderedMap`, `UnorderedSet` constructors | `allowDuplicate` | `allowDuplicates` |
| `UnorderedMapSlim`, `Utf8UnorderedMap`, `Utf16UnorderedMap` constructors | `minimumSize` | `minimumCapacity` |
| `CollectionHelper.CalculatePowerOfTwoCapacity` | `minimumSize` | `minimumCapacity` |
| `CollectionHelper.GetPrime` | `min` | `minimum` |
| `ObjectPool<T>` constructor | `createFunc` | `factory` |
| `TemporaryList<T>.Add` | `obj` | `item` |
| `XxHash3Slim.Hash64(ReadOnlySpan<char>)`, `Hash64(string)` | `input`, `str` | `source` |
| `Struct128(int)`, `Struct256(int)` | `int0` | `value` |
| `Struct128(ref Struct128)`, `Struct256(ref Struct256)` | `struct128`, `struct256` | `source` |
| `IStringConvertible<T>.TryParse` / `TryFormat` | `@object`, `read` / `written` | `result`, `charsRead` / `charsWritten` |
| `IUtf8Convertible<T>.TryParse` / `TryFormat` | `@object`, `read` / `written` | `result`, `bytesRead` / `bytesWritten` |
| `BaseHelper.ImplementsIStringConvertible` | `t` | `type` |
| `BaseHelper.IsSeparator` | `val` | `value` |
| `BaseHelper.ThrowSizeMismatchException` | `size` | `expectedSize` |
| `BaseHelper.TryParseFromSourceOrEnvironmentVariable`, `TryParseFromEnvironmentVariable` | `variable`, `instance` | `environmentVariableName`, `result` |
| `VersionHelper.SetAssembly` | `assemblyName` | `partialAssemblyName` |
| `AppCloseHandler.Register` | `closeEventHandler` | `handler` |

## Search-and-replace hints

Regex replacements (word boundaries) that are safe in most codebases:

| Find | Replace |
| --- | --- |
| `\bRentReadOnlyMemory\b` | `RentedReadOnlyMemory` |
| `\bRentMemory\b` | `RentedMemory` |
| `\bRentArray\b` | `RentedArray` (then change property access `x.RentedArray` to `x.Owner`) |
| `\bIsRent\b` | `IsRented` |
| `\bIHotMethod2\b` | `IHotTreeMethod` |
| `\bHotMethod2\b` | `HotTreeMethod` |
| `\bTagObject\.MaxTag\b` | `TagObject.TagCount` |
| `\bTrySlide\b` | `Slide` |
| `\bRemoveCrLf\b` | `RemoveCrAndLfChars` |
| `\bGetValidUtf8Length\b` | `GetCompleteUtf8Length` |
| `\bSeparatorCharFlag\b` | `SeparatorCharFlags` |
| `\bVersionHelper\.Build\b` | `VersionHelper.BuildVersion` |
| `\bVersionHelper\.VersionInt\b` | `VersionHelper.EncodedVersion` |
| `\bAppCloseHandler\.Set\(` | `AppCloseHandler.Register(` |
| `\bStruct(128\|256)\.Name\b` | `Struct$1.TypeName` |
| `\bCreateInterface\b` | `CreateLease` |
| `\bEnumerateNode\b` | `EnumerateNodes` |
| `\bEnumerateValue\b` | `EnumerateValues` |
| `\bAllowDuplicate\b` | `AllowDuplicates` |
| `\ballowDuplicate:` | `allowDuplicates:` |

Fix the remaining renames from compiler errors, because the old names are common words:
`Interface`, `TryGet`, `Cache`, `Count`, `AsReadOnly`, `Reverse`, `SetNodeValue` (sets only),
`IsValid`/`IsInvalid`, `UnusedNode`, `Consumed`, `FirstOrDefault`, `Get`/`Set`/`Remove(int)` (`SlidingList<T>`),
`Add` (`UnorderedMapSlim<TKey, TValue>`, `*Hashtable<TValue>`, `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>`), `MaxStringLength`/`GetStringLength` (`IUtf8Convertible<T>` only),
and tuple elements `Max`, `Cmp`, `Leaf`.
