## Arc.Collections
![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

日本語ドキュメントは[こちら](/doc/README.jp.md)



Arc.Collections is a fast C# Collection Library which includes

| Collection                                                   | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| ```UnorderedList<T>  ```<br />(equivalent to ```List<T>```)  | A list of objects that can be accessed by index.             |
| ```UnorderedLinkedList<T>```<br />(```LinkedList<T>```)      | A doubly linked list which has ```Node<T>``` operation.      |
| OrderedList<T>                                               | A list of objects that can be accessed by index and maintained in sorted order. ```IComparable<T>``` or ```IComparer<T>``` is required. |
| ```OrderedKeyValueList<TKey, TValue>```<br />(```SortedList<TKey,TValue>```) | A list of key-value pairs that can be accessed by index and maintained in sorted order.```IComparable<TKey>``` or ```IComparer<TKey>``` is required. |
| ```OrderedMap<TKey, TValue>```<br />(```SortedDictionary<TKey, TValue>```) | A collection of key/value pairs that are sorted on the key (Red-Black Tree). The difference from ```SortedDictionary<TKey, TValue>``` is that ```OrderedMap<TKey, TValue>``` has ```Node<T>``` interface and ```TKey``` can be null. ```IComparable<TKey>``` or ```IComparer<TKey>``` is required.` |
| `OrderedSet`<br/> `(SortedSet<T>`)                           | A collection of objects that is maintained in sorted order. ```OrderedSet<T>``` is a subset of ```OrderedMap<TKey, TValue>``` and it's actually ```OrderedMap<T, int>``` (TValue int is not used). |
| ```OrderedMultiMap<TKey, TValue>```                          | A collection of key/value pairs that are sorted on the key. Duplicate keys are allowed in this class. |
| ```OrderedMultiSet<T>```                                     | A collection of objects that is maintained in sorted order. Duplicate keys are allowed in this class. |
| ```UnorderedMap<TKey, TValue>```<br />(```Dictionary<TKey, TValue>```) | A collection of key/value pairs that are stored as a hash table. ```UnorderedMap<TKey, TValue>```  is a bit slower than ```Dictionary<TKey, TValue>```, but ```UnorderedMap<TKey, TValue>``` has Node index interface and allows null key. |
| ```UnorderedSet<T>```                                        | A subset of ```UnorderedMap<TKey, TValue>``` and it's actually ```UnorderedMap<T, int>``` (TValue int is not used). |
| ```UnorderedMultiMap<TKey, TValue>```                        | A collection of key/value pairs that are stored as a hash table. Duplicate keys are allowed in this class. |
| ```UnorderedMultiSet<T>```                                   | A subset of ```UnorderedMap<TKey, TValue>``` and it's actually ```UnorderedMap<T, int>``` (TValue int is not used). |
| `ObjectPool<T>`                                              | A fast and thread-safe pool of objects (implemented using `ConcurrentQueue<T>`). |



I know it's reinventing the wheels, but these classes are necessary for implementing [CrossLink](https://github.com/archi-Doc/CrossLink). And reinventing the wheels is a kind of fun for me :)



## Quick Start

Install Arc.Collection using Package Manager Console.

```
Install-Package Arc.Collection
```

Sample code. You can use these classes in the same way as generic collection classes.

```csharp
using Arc.Collection;
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



## Performance

`OrderedSet<T>` use the same tree structure (Red-Black Tree) as `SortedSet<T>`. The difference is that `OrderedSet<T>` has a link to a parent node and is overall faster than `SortedSet<T>`.

Reference: `System.Collections.Generic.SortedSet<T>`

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



## Collections

The features of the various collections. Please use them well.

| Name                          | Structure   | Access | Add      | Remove   | Search   | Sort       | Enum.    |
| ----------------------------- | ----------- | ------ | -------- | -------- | -------- | ---------- | -------- |
| ```UnorderedList<T>```        | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |
| ```UnorderedLinkedList<T>```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```OrderedList<T>```          | Array       | Index  | O(n)     | O(n)     | O(log n) | Sorted     | O(1)     |
| ```OrderedKeyValueList<V>```  | Array       | Index  | O(n)     | O(n)     | O(log n) | Sorted     | O(1)     |
| ```OrderedMap<K, V>```        | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```OrderedSet<T>```           | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```OrderedMultiMap<K, V>```   | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```OrderedMultiSet<T>```      | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```UnorderedMap<K, V>```      | Hash table  | Node   | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| ```UnorderedSet<T>```         | Hash table  | Node   | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| ```UnorderedMultiMap<K, V>``` | Hash table  | Node   | O(1)     | O(1)     | O(1)     | No         | O(1)     |
| ```UnorderedMultiSet<T>```    | Hash table  | Node   | O(1)     | O(1)     | O(1)     | No         | O(1)     |

- Ordered collections require ```IComparable<T>``` or ```IComparer<T>```.
- Unordered collections (e.g. ```UnorderedMap<TKey, TValue>```) are based on hash tables, which require ```IEquatable<T>```/```GetHashCode()``` or ```IEqualityComparer<T>```.
- ```Multi``` collection allows duplicate keys.
- `OrderedMap` uses Red-black trees and is faster than `OrderedKeyValueList<TKey, TValue>` in most situations.
  For this reason, I recommend using ```OrderedMap<TKey, TValue>``` over ```OrderedKeyValueList<TKey, TValue>``` unless index access is absolutely necessary.

