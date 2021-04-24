## Arc.Collection
![Nuget](https://img.shields.io/nuget/v/Arc.Collection) ![Build and Test](https://github.com/archi-Doc/Arc.Collection/workflows/Build%20and%20Test/badge.svg)

Arc.Collectionは各種コレクションを実装した高速なC#ライブラリーです。

本家はGitHub [archi-Doc/Arc.Collection](https://github.com/archi-Doc/Arc.Collection) にあります。

| コレクション                                                 | 説明                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| ```UnorderedList<T>  ``` <br />(`List<T>`と同等)             | Indexアクセスが可能な、オブジェクトのリスト。                |
| ```UnorderedLinkedList<T>```<br />(`LinkedList<T>`と同等)    | 双方向リストで、`Node` による操作が可能です。                |
| ```OrderedList<T> ```                                        | ソート済みでIndexアクセスが可能な、オブジェクトのリスト。<br />`IComparable<T>` または `IComparer<T>` が必要。 |
| ```OrderedKeyValueList<TKey, TValue>``` <br />(`SortedList<TKey,TValue>`) | ソート済みでIndexアクセスが可能な、Key/Valueのリスト。<br />`IComparable<TKey>` または `IComparer<TKey>` が必要。 |
| ```OrderedMap<TKey, TValue>``` (`SortedDictionary<TKey, TValue>`) | Keyでソート済み（Red-Black Tree）のKey/Value コレクション。 `SortedDictionary<TKey, TValue>` との違いは、`Node`アクセスが可能なこと、`TKey`がnullも可ということです。<br />`IComparable<TKey>` または `IComparer<TKey>` が必要。 |
| ```OrderedSet<T>```<br />(`SortedSet<T>`)                    | ソート済み（Red-Black Tree）のコレクション。 `OrderedSet<T>` は `OrderedMap<TKey, TValue>` のサブセットで、実際は ```OrderedMap<T, int>``` です（TValue は int で、使用されません)。 |
| ```OrderedMultiMap<TKey, TValue>```                          | Keyでソート済み（Red-Black Tree）のKey/Value コレクション。 重複キーを使用可能です。 |
| ```OrderedMultiSet<T>```                                     | ソート済み（Red-Black Tree）のコレクション。重複オブジェクトも可。 |
| `UnorderedMap<TKey, TValue>`<br />(`Dictionary<TKey, TValue>`) | Hash tableで管理されるKey/Value コレクション。`UnorderedMap<TKey, TValue>` は `Dictionary<TKey, TValue>` より少し遅いですが、`UnorderedMap<TKey, TValue>` は`Node index`操作が可能で、`TKey`がnullも可です。 |
| ```UnorderedSet<T>```                                        | `UnorderedMap<TKey, TValue>` のサブセットで、実際は `UnorderedMap<T, int>` です（TValue は int で、使用されません)。 |
| ```UnorderedMultiMap<TKey, TValue>```                        | Hash tableで管理されるKey/Value コレクション。重複キーを使用可能です。 |
| ```UnorderedMultiSet<T>```                                   | `UnorderedMap<TKey, TValue>` のサブセットで、実際は `UnorderedMap<T, int>` です（TValue は int で、使用されません)。 |



フツーにジェネリックコレクションがあるのに・・・

車輪の再発明と言うほかありませんが、[CrossLink](https://github.com/archi-Doc/CrossLink) に必要だったため作ってしまいました。実装作業は結構楽しかった。



## Quick Start

Package Manager Consoleでインストールします。

```
Install-Package Arc.Collection
```

サンプルコード。フツーのコレクションと同じノリで使用できます。

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

`OrderedSet<T>` は `SortedSet<T>` と同様に、データ構造に赤黒木を使用しています。違いは、`OrderedSet<T>` は内部的に親ノードへのリンクを持つこと、そしてノードアクセスが可能なことです。

`SortedSet<T>` より高速に動作します。フツーに使っても速いし、`Node`を使ったアクセスは断然速いです。

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

各コレクションの特徴です。うまく使い分けてください。

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
| ```UnorderedMap<K, V>```      | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| ```UnorderedSet<T>```         | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| ```UnorderedMultiMap<K, V>``` | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| ```UnorderedMultiSet<T>```    | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |

- `Ordered` コレクションはオブジェクトをソートするため、`IComparable<T>` または `IComparer<T>` が必要です。
- Hash tableを使用するコレクション（`UnorderedMap<TKey, TValue>`とか）は適切な ```IEquatable<T>```/```GetHashCode()``` または `IEqualityComparer<T>` が必要です。
- ```Multi``` がついたコレクションは、重複キーを使用可能です。
- `OrderedMap<TKey, TValue>` は赤黒木（Red-black trees）を使用し、`OrderedKeyValueList<TKey, TValue>` よりもほとんどのシチュエーションで高速です。絶対にIndexアクセスが必要な場面以外は、`OrderedMap<TKey, TValue>` の使用をお勧めします。

