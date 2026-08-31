## Arc.Collections
![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

Arc.Collectionsは各種コレクションを実装した高速なC#ライブラリーです。

本家はGitHub [archi-Doc/Arc.Collections](https://github.com/archi-Doc/Arc.Collections) にあります。

対象フレームワークは **.NET 10** で、公開APIは `Arc.Collections` および `Arc` 名前空間にあります。

フツーにジェネリックコレクションがあるのに・・・

車輪の再発明と言うほかありませんが、[CrossLink](https://github.com/archi-Doc/CrossLink) に必要だったため作ってしまいました。実装作業は結構楽しかった。



## Quick Start

Package Manager Consoleでインストールします。

```
Install-Package Arc.Collections
```

サンプルコード。フツーのコレクションと同じノリで使用できます。

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

| コレクション                                                 | 説明                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `UnorderedList<T>`<br />(`List<T>`と同等)                    | Indexアクセスが可能な、オブジェクトのリスト。`AsSpan()` を使うと最速で列挙できます。 |
| `UnorderedLinkedList<T>`<br />(`LinkedList<T>`と同等)        | 双方向リストで、`Node` による操作が可能です。                |
| `OrderedList<T>`                                             | ソート済みでIndexアクセスが可能な、オブジェクトのリスト。<br />`IComparable<T>` または `IComparer<T>` が必要。 |
| `OrderedKeyValueList<TKey, TValue>`<br />(`SortedList<TKey, TValue>`) | ソート済みでIndexアクセスが可能な、Key/Valueのリスト。重複キーも可。<br />`IComparable<TKey>` または `IComparer<TKey>` が必要。 |
| `OrderedMap<TKey, TValue>`<br />(`SortedDictionary<TKey, TValue>`) | Keyでソート済み（Red-Black Tree）のKey/Value コレクション。`SortedDictionary<TKey, TValue>` との違いは、`Node`アクセスが可能なこと、`TKey`がnullも可ということです。<br />`IComparable<TKey>` または `IComparer<TKey>` が必要。 |
| `OrderedSet<T>`<br />(`SortedSet<T>`)                        | ソート済み（Red-Black Tree）のコレクション。実体は `OrderedMap<T, byte>` です（TValue は使用されません）。 |
| `OrderedMultiMap<TKey, TValue>`                              | Keyでソート済み（Red-Black Tree）のKey/Value コレクション。重複キーを使用可能で、追加順が保持されます。 |
| `OrderedMultiSet<T>`                                         | ソート済み（Red-Black Tree）のコレクション。重複オブジェクトも可。 |
| `UnorderedMap<TKey, TValue>`<br />(`Dictionary<TKey, TValue>`) | Hash tableで管理されるKey/Value コレクション。`Dictionary<TKey, TValue>` より少し遅いですが、`Node index`操作が可能で、`TKey`がnullも可、`allowDuplicate` で重複キーも可です。 |
| `UnorderedSet<T>`                                            | 実体は `UnorderedMap<T, byte>` です（TValue は使用されません）。重複要素・null要素にも対応します。 |
| `UnorderedMapSlim<TKey, TValue>`                             | メモリ効率を優先した軽量なHash map。キーはnull不可で、バージョンチェックを行いません。`GetValueRefOrAddDefault()` によりRead-Modify-Writeを高速に行えます。 |
| `SlidingList<T>`                                             | Index ではなく安定した **Position** で要素を指す、固定容量のリングバッファ。送受信ウィンドウなどに使えます。 |
| `CircularQueue<T>`                                           | スレッドセーフな固定容量の循環キュー（Vyukov方式のMPMC）。容量制限を許容できる場合は `ConcurrentQueue<T>` より高速です。 |
| `TemporaryList<TObject>`                                     | 4個までヒープ確保なしで保持する `ref struct` のリスト。`foreach` 中に集めたオブジェクトを後から操作する用途に便利です。 |

### Keyed lookup

| 型                                                           | 説明                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `Int32Hashtable<TValue>`, `UInt32Hashtable<TValue>`<br />`Int64Hashtable<TValue>`, `UInt64Hashtable<TValue>`<br />`Utf8Hashtable<TValue>`, `Utf16Hashtable<TValue>` | スレッドセーフなHash table。書き込みはロックで直列化し、読み取りはロックフリーです。構築の頻度が低く読み取りが多い用途に最適化されています。 |
| `Utf8UnorderedMap<TValue>`, `Utf16UnorderedMap<TValue>`      | UTF-8 (`byte[]`) / UTF-16 (`string`) をキーとする軽量なHash map（スレッドセーフではありません）。`ReadOnlySpan` のオーバーロードを備え、実際に挿入するときだけキーを確保します。 |

### Pools and buffers

| 型                                                           | 説明                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `ObjectPool<T>`                                              | 高速でスレッドセーフなオブジェクトプール（`CircularQueue<T>` を使用）。プールに入らなかった `IDisposable` は破棄されます。 |
| `KeyedObjectCache<TKey, TObject>`                            | キーで取り出す、コストの高いオブジェクト（暗号化など）のスレッドセーフなキャッシュ。 |
| `BytePool`                                                   | 高速でスレッドセーフなbyte配列のプール。参照カウントで共有でき、`RentMemory` / `RentReadOnlyMemory` として扱えます。 |
| `SpanOwner<T>`                                               | 「小さければstackalloc、大きければArrayPool、最後にReturn」というパターンを `using` 一行にまとめる `ref struct`。 |
| `SequenceBuilder<T>`                                         | プールされた配列から `ReadOnlySequence<T>` を構築します。    |
| `PooledStringBuilder`                                        | プールされたchar配列で文字列を構築します。文字列補間に匹敵する性能を目指しています。 |

### Helpers

| 型                                                           | 説明                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `XxHash3Slim`                                                | `System.IO.Hashing` から移植した、アロケーションなしのXXH3(64bit)実装。 |
| `CollectionHelper`                                           | 容量計算（2の冪／素数）のヘルパー。                          |
| `TagObject`                                                  | 0～255 のタグに対応するキャッシュ済みオブジェクト。ボックス化なしでタグを `object` として渡せます。 |
| `Arc.BaseHelper`                                             | Span/文字列のヘルパー。行分割、区切り文字の検索、10進桁数の計算、SIMDによるbyte合計、UTF-8長の検証、リソース読み込みなど。 |
| `Arc.Struct128`, `Arc.Struct256`                             | 固定長バイナリを扱うための128bit／256bitの値型。             |
| `Arc.IStringConvertible<T>`, `Arc.IUtf8Convertible<T>`       | オブジェクトとUTF-16／UTF-8表現をアロケーションなしで相互変換するためのインターフェース。 |
| `Arc.VersionHelper`, `Arc.AppCloseHandler`                   | アセンブリのバージョン情報と、アプリケーション終了（プロセス終了／コンソールクローズ）ハンドラー。 |



## Performance

`OrderedSet<T>` は `SortedSet<T>` と同様に、データ構造に赤黒木を使用しています。違いは、`OrderedSet<T>` は内部的に親ノードへのリンクを持つこと、そしてノードアクセスが可能なことです。

`SortedSet<T>` より高速に動作します。フツーに使っても速いし、`Node`を使ったアクセスは断然速いです。

以下はBenchmarkDotNetによる測定結果です。手元で再現するには `Benchmark` プロジェクトを実行してください。

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



## 使い分け

各コレクションの特徴です。うまく使い分けてください。

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
| `UnorderedMap<K, V>`          | Hash table  | Node     | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| `UnorderedSet<T>`             | Hash table  | Node     | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| `UnorderedMapSlim<K, V>`      | Hash table  | Index    | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| `SlidingList<T>`              | Ring buffer | Position | O(1)     | O(1)     | O(n)     | -          | O(1)     |
| `CircularQueue<T>`            | Ring buffer | FIFO     | O(1)     | O(1)     | -        | -          | -        |

- `Ordered` コレクションはオブジェクトをソートするため、`IComparable<T>` または `IComparer<T>` が必要です。
- Hash tableを使用するコレクション（`UnorderedMap<TKey, TValue>`とか）は適切な `IEquatable<T>`/`GetHashCode()` または `IEqualityComparer<T>` が必要です。
- `Multi` がついたコレクションは、重複キーを使用可能です。`UnorderedMap<TKey, TValue>` と `UnorderedSet<T>` も `allowDuplicate: true` で重複キーを扱えます。
- `OrderedMap<TKey, TValue>` は赤黒木（Red-black trees）を使用し、`OrderedKeyValueList<TKey, TValue>` よりもほとんどのシチュエーションで高速です。絶対にIndexアクセスが必要な場面以外は、`OrderedMap<TKey, TValue>` の使用をお勧めします。
- `UnorderedList<T>` の `Add` は償却 O(1) です（内部配列の拡張が発生することがあります）。



## スレッドセーフティ

特に記載がない限り、各コレクションはスレッドセーフ**ではありません**。変更が行われていない間に限り複数スレッドから読み取れますが、書き込みを行う場合は呼び出し側で排他制御が必要です。

例外は次の通りです。

- `CircularQueue<T>`、`ObjectPool<T>`、`BytePool`、`KeyedObjectCache<TKey, TObject>`（ただし `ObjectPool<T>.Dispose` は `Rent`/`Return` と同時に実行しないでください）。
- `*Hashtable<TValue>` 各種。書き込みは内部で直列化され、読み取りはロックフリーです。

`UnorderedMapSlim<TKey, TValue>`、`Utf8UnorderedMap<TValue>`、`Utf16UnorderedMap<TValue>` はバージョンチェックを行わないため、列挙中の変更は例外ではなく未定義動作になります。
