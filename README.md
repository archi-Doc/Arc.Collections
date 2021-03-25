## Arc.Collection
![Nuget](https://img.shields.io/nuget/v/Arc.Collection) ![Build and Test](https://github.com/archi-Doc/Arc.Collection/workflows/Build%20and%20Test/badge.svg)



Arc.Collection is a fast C# Collection Library which includes

- ```UnorderedList<T>  ``` (equivalent to ```List<T>```) : A list of objects that can be accessed by index.
- ```UnorderedLinkedList<T>``` (```LinkedList<T>```) : A doubly linked list which has ```Node<T>``` operation.
- ```OrderedList<T> ``` : A list of objects that can be accessed by index and maintained in sorted order. ```IComparable<T>``` or ```IComparer<T>``` is required.
- ```OrderedKeyValueList<TKey, TValue>``` (```SortedList<TKey,TValue>```) : A list of key-value pairs that can be accessed by index and maintained in sorted order.```IComparable<TKey>``` or ```IComparer<TKey>``` is required.
- ```OrderedMap<TKey, TValue>``` (```SortedDictionary<TKey, TValue>```) : A collection of key/value pairs that are sorted on the key (Red-Black Tree). The difference from ```SortedDictionary<TKey, TValue>``` is that ```OrderedMap<TKey, TValue>``` has ```Node<T>``` interface and ```TKey``` can be null. ```IComparable<TKey>``` or ```IComparer<TKey>``` is required.
- ```OrderedSet<T>``` (```SortedSet<T>```) : A collection of objects that is maintained in sorted order. ```OrderedSet<T>``` is a subset of ```OrderedMap<TKey, TValue>``` and it's actually ```OrderedMap<T, int>``` (TKey int is not used).
- ```OrderedMultiMap<TKey, TValue>``` : A collection of key/value pairs that are sorted on the key. Duplicate keys are allowed in this class.
- ```OrderedMultiSet<T>``` :  A collection of objects that is maintained in sorted order. Duplicate keys are allowed in this class.



## Quick Start

```
Install-Package Arc.Collection
```

Sample code

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

```OrderedMap<T>``` and ```OrderedSet<T>``` use the same tree structure (Red-Black Tree) as ```SortedSet<T>```. The difference is that ```OrderedSet<T>``` has a link to a parent node and is overall faster than ```SortedSet<T>```.

Reference: ```System.Collections.Generic.SortedSet<T>```

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
| **NewAndAdd_SortedSet**     | **10000** | **1,497,938.64 ns** | **4,287.530 ns** | **6,284.605 ns** | **1,494,800.59 ns** | **82.0313** | **400328 B** |
| NewAndAdd_OrderedSet        | 10000     |     1,231,310.67 ns |     2,496.231 ns |     3,658.942 ns |     1,230,951.37 ns |    101.5625 |     560480 B |
| NewAndAdd2_SortedSet        | 10000     |     2,229,837.77 ns |     5,179.728 ns |     7,592.379 ns |     2,225,916.99 ns |    125.0000 |     720624 B |
| NewAndAdd2_OrderedSet       | 10000     |     1,870,890.74 ns |     7,969.192 ns |    10,908.320 ns |     1,877,329.00 ns |    144.5313 |     800720 B |
| AddRemove_SortedSet         | 10000     |           851.56 ns |        21.362 ns |        31.973 ns |           853.15 ns |      0.0381 |        160 B |
| AddRemove_OrderedSet        | 10000     |           225.37 ns |         0.335 ns |         0.470 ns |           225.28 ns |      0.0534 |        224 B |
| AddRemoveNode_OrderedSet    | 10000     |           158.05 ns |         0.329 ns |         0.483 ns |           158.03 ns |      0.0534 |        224 B |
| AddRemoveReuse_OrderedSet   | 10000     |           147.02 ns |         0.250 ns |         0.366 ns |           146.93 ns |           - |            - |
| AddRemoveReplace_OrderedSet | 10000     |            11.53 ns |         0.011 ns |         0.017 ns |            11.53 ns |           - |            - |
| Enumerate_SortedSet         | 10000     |       185,841.88 ns |       173.477 ns |       231.587 ns |       185,827.69 ns |           - |        280 B |
| Enumerate_OrderedSet        | 10000     |       192,531.52 ns |       708.353 ns |     1,015.899 ns |       192,752.53 ns |           - |         48 B |



