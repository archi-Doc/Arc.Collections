# Arc.Collections

![Nuget](https://img.shields.io/nuget/v/Arc.Collections) ![Build and Test](https://github.com/archi-Doc/Arc.Collections/workflows/Build%20and%20Test/badge.svg)

Arc.Collections は **.NET 10** 向けの C# コレクションライブラリーです。再利用できるノードを持つソート済みコレクションと重複キー対応コレクション、安定したノードインデックスを持つハッシュマップ、ロックフリーで検索できるスレッドセーフなハッシュテーブル、そしてプール・バッファ・ハッシュ関連のユーティリティを提供します。[NuGet](https://www.nuget.org/packages/Arc.Collections) で公開しています。

本家はGitHub [archi-Doc/Arc.Collections](https://github.com/archi-Doc/Arc.Collections) にあります。

フツーにジェネリックコレクションがあるのに・・・

車輪の再発明と言うほかありませんが、[CrossLink](https://github.com/archi-Doc/CrossLink) に必要だったため作ってしまいました。実装作業は結構楽しかった。

ほとんどの型は `Arc.Collections` 名前空間にあります。汎用ヘルパーは `Arc`、特殊な検索用のコントラクトは `Arc.Collections.HotMethod` にあります。各コレクションは `System.Collections.Generic` をそのまま置き換えられるものではありません。インターフェース、重複の扱い、変更に関するルールが異なります。

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
var (node, added) = map.Add(2, "two"); // ノードと、追加されたかどうかを返します。
map.Add(1, "one");
Console.WriteLine(node.Previous!.Value); // one

map.RemoveNode(node); // ノードは未使用になります。
map.Add(3, "three", node); // 新たに確保せず、ノードを再利用します。
Console.WriteLine(string.Join(", ", map.Keys)); // 1, 3
```

## Choosing a collection

| 型 | 構造 | 順序 | 重複 | null | アクセス方法 | 類似の型 |
| --- | --- | --- | --- | --- | --- | --- |
| `UnorderedList<T>` | 可変長配列 | 挿入順 | 可 | 可 | インデックス | `List<T>` |
| `OrderedList<T>` | ソート済み配列 | 比較子 | 可 | 可 | インデックス | |
| `OrderedKeyValueList<TKey, TValue>` | ソート済みの並列配列 | キーの比較子 | 可 | 不可 | インデックス | `SortedList<TKey, TValue>` |
| `OrderedMap<TKey, TValue>` | 赤黒木 | キーの比較子（逆順も可） | 不可 | 可 | ノード | `SortedDictionary<TKey, TValue>` |
| `OrderedSet<T>` | `OrderedMap<T, byte>` | 比較子（逆順も可） | 不可 | 可 | ノード | `SortedSet<T>` |
| `OrderedMultiMap<TKey, TValue>` | 重複リスト付きの赤黒木 | キーの比較子（逆順も可） | 可 | 可 | ノード | |
| `OrderedMultiSet<T>` | `OrderedMultiMap<T, byte>` | 比較子（逆順も可） | 可 | 可 | ノード | |
| `UnorderedMap<TKey, TValue>` | ハッシュテーブル | 未規定 | オプション | 可 | ノードインデックス | `Dictionary<TKey, TValue>` |
| `UnorderedSet<T>` | `UnorderedMap<T, byte>` | 未規定 | オプション | 可 | | `HashSet<T>` |
| `UnorderedMapSlim<TKey, TValue>` | コンパクトなハッシュテーブル | 未規定 | 不可 | 不可 | 値への `ref` | `Dictionary<TKey, TValue>` |
| `UnorderedLinkedList<T>` | 双方向連結リスト | 挿入順または明示的な指定 | 可 | 可 | ノード | `LinkedList<T>` |
| `SlidingList<T>` | 参照のリングバッファ | 位置 | 可 | 不可 | 位置 | |
| `CircularQueue<T>` | 固定容量のリングバッファ | FIFO | 可 | 可 | | `ConcurrentQueue<T>`（容量制限付き） |
| `TemporaryList<T>` | 4 つのインラインスロットを持つ `ref struct` | 挿入順 | 可 | 可 | | |

- null に対応していない型では、null のキーや要素を格納すると `ArgumentNullException` がスローされます。
- `IDictionary<TKey, TValue>` を実装しているのは `OrderedKeyValueList<TKey, TValue>` だけです。`UnorderedList<T>`、`OrderedList<T>`、`SlidingList<T>` は `IList<T>` を、`UnorderedLinkedList<T>` は `ICollection<T>` を実装します。その他のマップとセットは `IEnumerable<T>` だけを実装します。
- スレッドセーフな検索や UTF キーによる検索については、[Hashtables and UTF-keyed maps](#hashtables-and-utf-keyed-maps) を参照してください。

## Behavior

### Ordering and comparers

- ソート済みコレクションは、指定された `IComparer<T>` または `Comparer<T>.Default` を使い、比較結果が 0 のキーを等しいとみなします（`Equals` は使いません）。
- ツリー系コレクション（`OrderedMap<TKey, TValue>`、`OrderedSet<T>`、`OrderedMultiMap<TKey, TValue>`、`OrderedMultiSet<T>`）は `reverse: true` を指定すると降順になります。ソート済みリストにはこの指定はありません。
- 既定の比較子では、null は先頭に（逆順のツリーでは末尾に）並びます。カスタム比較子には null キーもそのまま渡され、その順序は比較子が決めます。
- 既定の比較子では、宣言型 `T` がインターフェースや基底クラスであっても、キーは実行時の型が実装する `IComparable<T>` で比較されます。`Comparer` プロパティ（`Comparer<T>.Default`）はこの実装を考慮しないため、そのようなキーに対して例外をスローしたり、異なる順序で並べたりすることがあります。
- `GetLowerBound(key)` はコレクションの順序でキー以降にある最初の要素を、`GetUpperBound(key)` はキー以前にある最後の要素を返します。ソート済みリストはインデックス（該当なしは -1）を、ツリーはノード（該当なしは null）を返します。
- ハッシュベースの型のうち、`IEqualityComparer<T>` を受け取るのは `UnorderedMap<TKey, TValue>` と `UnorderedSet<T>` だけです。
- 格納済みのキーを、比較結果やハッシュコードが変わるような形で変更しないでください。

### Duplicates and assignment

| 型 | 既存のキーを追加した場合 | 既存のキーに対するインデクサーの setter |
| --- | --- | --- |
| `OrderedMap<TKey, TValue>`、`UnorderedMap<TKey, TValue>` | `Add` は既存のノード（またはノードインデックス）を `NewlyAdded == false` で返し、値は変更しません。 | 値を置き換えます。 |
| `allowDuplicates: true` を指定した `UnorderedMap<TKey, TValue>` | `Add` はエントリを追加します。 | 一致するエントリのうち 1 つの値を置き換えます。 |
| `OrderedMultiMap<TKey, TValue>`、`OrderedKeyValueList<TKey, TValue>` | `Add` は、同じキーを持つ既存のエントリの後ろにエントリを追加します。 | エントリを追加します。置き換えはしません。 |
| `UnorderedMapSlim<TKey, TValue>`、UTF キーのマップ、ハッシュテーブル | `Add` はありません。`TryAdd` は `false` を返して値を維持し、`AddOrUpdate` は値を置き換えます。 | 値を置き換えます（マップのみ）。 |

- インデクサーの setter は、存在しないキーを追加します。`OrderedSet<T>.Add` は既存の要素に対して `false` を返します。`UnorderedSet<T>.Add` も、重複を許可していなければ同様です。`OrderedMultiSet<T>` と `OrderedList<T>` は、等しい既存の要素の後ろに挿入します。
- キーに重複がある場合、単一のエントリを対象とする操作（`TryGetValue`、`FindFirstNode`、`IndexOf`、`IndexOfKey`、`Remove`、インデクサーの getter）は、ソート済みコレクションではコレクションの順序で最初に一致したものを、`UnorderedMap<TKey, TValue>` では一致したもののうちどれか 1 つ（未規定）を使います。

### Nodes and node indexes

- ツリーのノードは、コレクション内にある間だけ有効です。ツリーはノードの所属をチェックしないため、別のツリーで使用中のノードを `RemoveNode` や `SetNodeKey` に渡すと、両方のツリーが壊れます。
- `RemoveNode` と `Clear` はノードを未使用にします（`OrderedMap<TKey, TValue>.Node.IsUnused` で確認できます）。未使用ノードの `Previous` と `Next` は null を返し、`RemoveNode` は未使用ノードを無視します。
- 未使用ノードを `Add`（`OrderedSet<T>` では `AddNode`）の `reuse` 引数に渡すと、アロケーションを避けられます。使用中のノードは再利用されません。`OrderedMap<TKey, TValue>` または `OrderedSet<T>` にそのキーが既にある場合、`reuse` には手を付けません。
- `UnorderedMap<TKey, TValue>` のノードインデックスは、リサイズ後も有効です。削除されたインデックスは後から追加されたエントリに再利用されることがあり、`Clear` はすべてのインデックスを無効にします。
- `UnorderedLinkedList<T>` は他のリストのノードを受け付けません。`Remove(node)` と `Clear` はノードを切り離し、切り離したノードはアロケーションなしで再び追加できます。

### Enumeration

- コレクションを変更すると、その列挙子は `InvalidOperationException` をスローします。ただし、次の例外があります。
  - ノードの `UnsafeChangeValue` や `ValueRef`、`UnorderedMap<TKey, TValue>.UnsafeChangeValue`、`OrderedMultiMap<TKey, TValue>.SetNodeValue`、`UnorderedList<T>.AsSpan()`、`UnorderedMap<TKey, TValue>.UnsafeGetNodes()` が返す配列を介した書き込みは検出されません。
  - `UnorderedMapSlim<TKey, TValue>`、UTF キーのマップ、`TemporaryList<T>` は変更を一切検出しません。列挙中に変更しないでください。
- `GetEnumerator` は構造体を返します。`Keys` と `Values`（`OrderedKeyValueList<TKey, TValue>` を除く）、`EnumerateNodes`、`EnumerateValues` も同様です。具象型に対する `foreach` はアロケーションしませんが、インターフェースを介して列挙すると列挙子がボックス化されます。

### Complexity

| 型 | 検索 | 挿入 | 削除 |
| --- | --- | --- | --- |
| `UnorderedList<T>` | インデックス指定は O(1)、値の探索は O(n) | 末尾への追加は償却 O(1)、途中への挿入は O(n) | O(n) |
| `OrderedList<T>`、`OrderedKeyValueList<TKey, TValue>` | O(log n) | 比較は O(log n)、要素の移動は O(n) | O(n) |
| ツリー系コレクション | O(log n) | O(log n) | O(log n) |
| ハッシュマップとハッシュセット、ハッシュテーブル | 平均 O(1)、最悪 O(n) | 償却 O(1) | 平均 O(1) |
| `UnorderedLinkedList<T>` | 値の探索は O(n) | 既知のノードの隣なら O(1) | ノードを指定すれば O(1) |
| `SlidingList<T>` | 位置指定は O(1)、値の探索は O(n) | O(1) | O(1)。先頭を削除したときは後続の穴もスキップします |

- キーの d 個の重複を走査する操作は O(log n + d) です。たとえば `OrderedMultiMap<TKey, TValue>.FindNode(key, value)` や `OrderedMultiSet<T>.GetCount` が該当します。
- `ICollection<T>`、配列、span を渡した `OrderedList<T>.AddRange` は、追加分をソートしてからマージします。比較は O(m log m + m log n)、移動は O(n + m) です。それ以外のシーケンスは 1 要素ずつ追加されます。

## Collection notes

### Lists

- `UnorderedList<T>.AsSpan()` は内部配列を公開します。リストが要素を追加・削除したり容量を変更したりすると、その span は無効になります。
- `OrderedList<T>` は `UnorderedList<T>` を継承しています。`Insert` とインデクサーの setter は `InvalidOperationException` をスローします。`UnorderedList<T>` 型の参照や `AsSpan()` を介した書き込みはソート順を無視し、基底クラスの `Contains`、`IndexOf`、`Remove` は `EqualityComparer<T>.Default` による線形探索を行います。
- `OrderedKeyValueList<TKey, TValue>.Keys` と `Values` は、リストの現在の内容を反映する読み取り専用の `IList<T>` ビューです。
- `OrderedKeyValueList<TKey, TValue>` を同じ比較子インスタンスでコピーすると、重複の順序は維持されます。それ以外の場合、重複の順序は未規定です。引数が 1 つのコピーコンストラクターは `Comparer<TKey>.Default` を使うため、コピー元の比較子がカスタムの場合はその `Comparer` を渡してください。

### Trees

- `GetRange(lower, upper)` は範囲内の最初と最後のノードを返します。逆順のツリーでは、`lower` が大きい方のキーです。
- `OrderedMultiMap<TKey, TValue>.EnumerateValues(key)` と `EnumerateNodes(key)` は、キーのエントリを挿入順に列挙します。`FindNode(key, value)` と `Remove(key, value)` は `EqualityComparer<TValue>.Default` で値を照合します。

### Hash maps

- `UnorderedMap<TKey, TValue>` は null キーを比較子に渡しません。また、`Clear` は容量を維持します。
- `GetValueRefOrAddDefault`（`UnorderedMapSlim<TKey, TValue>` と UTF キーのマップ）は、1 回の検索で値のスロットへの参照を返します。この参照は、追加、削除、`Clear` の後は無効です。

### SlidingList\<T\>

`SlidingList<T>` はリングバッファで、要素は 0 から始まるインデックスではなく **位置**（position）で指定します。

- `Add` は新しい要素の位置を返します。リストが満杯のときは -1 を返します。
- 位置は `int.MaxValue` の次に 0 に戻ります。有効な位置は `[StartPosition, StartPosition + Capacity)`（2^31 を法とする）です。
- 途中の要素を削除すると、スロットを占有したままの穴が残ります。`UsedSlotCount` は穴を数えますが、`Count` は数えません。先頭の削除、`Slide()`、`GetFirstOrDefault()` はウィンドウを穴の先まで進めます。そのため `GetFirstOrDefault()` は書き込み操作であり、列挙子を無効にすることがあります。
- `IList<T>` のメンバーは位置を引数に取ります。`Insert` はスロットを上書きし、`ICollection<T>.Add` はリストが満杯のとき要素を追加せずに捨てます。`ElementAt` などのインデックスに基づく LINQ 演算子は、`StartPosition` が 0 で穴がない場合を除き、誤った結果を返します。
- `Clear()` は、新しいウィンドウを直前の `EndPosition` から開始します。`Resize(capacity)` は位置を維持します。`capacity` が `UsedSlotCount` より小さい場合は `false` を返し、何も変更しません。

```csharp
using System;
using Arc.Collections;

var window = new SlidingList<string>(3);
var a = window.Add("a"); // a == 0
var b = window.Add("b"); // b == 1
window.Add("c"); // 位置 2。
Console.WriteLine(window.Add("d")); // -1（満杯）

window.TryRemoveAt(b); // 位置 1 に穴が残ります。
Console.WriteLine(window.CanAdd); // False：穴もスロットを占有しています。
window.TryRemoveAt(a); // 先頭を削除し、穴の先までスライドします。
Console.WriteLine(window.StartPosition); // 2
Console.WriteLine(window.Add("d")); // 3
Console.WriteLine(window[3]); // d（引数は位置）
```

### Other collections

- `CircularQueue<T>` の容量は固定で、2 から 2^30 までの 2 の累乗に切り上げられます。並行アクセス中の `Count` は推定値です。`TryEnqueue` はキューが満杯のとき、または並行するデキューが完了するまでの短い間 `false` を返します。`TryDequeue` はキューが空のとき `false` を返します。
- `TemporaryList<T>` は、5 つ目以降の要素をヒープに確保した `List<T>` に格納します。破棄は不要ですが、コピーしないでください。コピー同士がその `List<T>` を共有します。

## Hashtables and UTF-keyed maps

| 型 | キー | 備考 |
| --- | --- | --- |
| `Int32Hashtable<TValue>`、`UInt32Hashtable<TValue>`、`Int64Hashtable<TValue>`、`UInt64Hashtable<TValue>` | `int`、`uint`、`long`、`ulong` | スレッドセーフ。インデクサーと列挙子はありません。 |
| `Utf8Hashtable<TValue>`、`Utf16Hashtable<TValue>` | UTF-8：`byte[]` または `ReadOnlySpan<byte>`。UTF-16：`string` または `ReadOnlySpan<char>`。 | スレッドセーフ。インデクサーと列挙子はありません。 |
| `Utf8UnorderedMap<TValue>`、`Utf16UnorderedMap<TValue>` | UTF ハッシュテーブルと同じ | スレッドセーフではありません。インデクサー、列挙子、`GetValueRefOrAddDefault` があります。 |

- ハッシュテーブルへの書き込みと、`ToArray`・`ToKeyValuePairs` によるスナップショットの作成は、書き込みロックを取得します。検索はロックフリーで、各キーについて並行する書き込みの前か後のいずれかの状態を参照します。`Count` は検索結果より一時的に遅れることがあります。
- `GetOrAdd` のファクトリーは書き込みロックの下で実行されるため、同じテーブルを変更してはいけません。保留中の挿入を無効にする変更（同じバケットへの変更、テーブルの拡張、`Clear`）は `InvalidOperationException` をスローします。それ以外の変更は検出されません。
- 数値キーは下位ビットでバケットに振り分けられます（64 ビットのキーは、先に上位と下位の 32 ビットを XOR します）。そのため、大きな 2 の累乗の倍数のような規則的なパターンは、同じバケットに集まることがあります。
- UTF キーは内容の序数（ordinal）比較で照合され、検証、正規化、大文字・小文字の同一視は行いません。
- span のキーは、新しいエントリを追加するときだけコピーされます。`byte[]` や `string` のキーはコピーされず、参照のまま格納されます。**格納済みの `byte[]` キーを変更しないでください**。列挙や `ToKeyValuePairs` が返す配列も同様です。

```csharp
using System;
using Arc.Collections;

var ids = new Utf8UnorderedMap<int>();
ids.AddOrUpdate("alice"u8, 1); // span を新しいキー配列にコピーします。
ids.AddOrUpdate("alice"u8, 2); // 既存のエントリを更新します。
Console.WriteLine(ids.TryAdd("alice"u8, 3)); // False
Console.WriteLine(ids.TryGetValue("alice"u8, out var id) ? id : -1); // 2
```

## Pools and buffers

| 型 | 用途 |
| --- | --- |
| `ObjectPool<T>` | 呼び出し側が指定したファクトリーで生成したオブジェクトを再利用します。 |
| `KeyedObjectCache<TKey, TObject>` | キーごとに最大 1 つのオブジェクトを保持し、満杯になると最も古いオブジェクトを追い出します。 |
| `BytePool` | byte 配列を 2 の累乗サイズのバケットでプールし、参照カウント付きのハンドルで所有権を管理します。 |
| `SpanOwner<T>` | 呼び出し側が渡した作業用 span が十分に大きければそれを使い、足りなければ `ArrayPool<T>.Shared` から借ります。 |
| `SequenceBuilder<T>` | プールされたチャンクから `ReadOnlySequence<T>` を構築します。 |
| `PooledStringBuilder` | プールされた文字のチャンクから文字列を構築します。 |

### ObjectPool\<T\> and KeyedObjectCache\<TKey, TObject\>

- `ObjectPool<T>.PoolSize` は 2 の累乗に切り上げられます（既定値は 32）。ファクトリーはスレッドセーフで、毎回新しいインスタンスを返す必要があります。
- `Return` はプールが満杯のとき `IDisposable` なオブジェクトを破棄し、`Dispose` はプール内のオブジェクトを破棄します。各オブジェクトは 1 回だけ（2 回目の返却は検出されません）、プールを破棄する前に返却してください。破棄後の `Rent` と `Return` は `ObjectDisposedException` をスローします。
- `KeyedObjectCache<TKey, TObject>.TakeOrDefault(key)` は、キャッシュされたオブジェクトの所有権を呼び出し側に移します。`TryAdd(key, obj)` は、`true` を返したときに限り所有権をキャッシュに移します。
- キャッシュは、追い出したオブジェクトをロック内で破棄します。そのオブジェクトの `Dispose` で、キャッシュを使う別のスレッドを待機してはいけません。
- `CreateLease(key, obj)` は `Lease` を返します。`Lease` は破棄されるとオブジェクトをキャッシュに渡し、キャッシュに受け入れられなかった場合はオブジェクトを破棄します。リースの破棄は 1 回だけにしてください。コピーも同じリースとみなされます。`lease = lease.Return()` とすると空のリースが残り、これは破棄しても安全です。

### BytePool

- `Rent(length)` は、所有参照を 1 つ持つ `RentedArray` を返します。`RentedMemory` と `RentedReadOnlyMemory` は、その一部の範囲を表すビューです。
- プールされた配列の長さはバケットの 2 の累乗サイズで、内容は**クリアされていません**。0 や最大のバケットを超える長さを要求すると、プールされない配列が返されます。
- `BytePool.Default` は 16 MiB までの配列をプールし、合計で約 200 MiB まで保持します。`CreateExponential` と `CreateFlat` では別の上限を設定できます。
- ハンドルのコピーやビューの作成（`AsMemory`、`AsReadOnlyMemory`、`ReadOnly`、`Slice`）では、所有者は**増えません**。`IncrementAndShare` は所有者を増やします。所有参照はそれぞれちょうど 1 回だけ返却または破棄してください。
- 最後の返却の後は、すべてのハンドルとビューが無効になります。配列とその `RentedArray` が別の借り手に渡る可能性があるためです。
- `RentedArray`、`RentedMemory`、`RentedReadOnlyMemory` の `CreateFrom(byte[])` は、プールされない配列を同じルールで管理します。`CreateFrom(ReadOnlyMemory<byte>)` は、返却の不要な管理対象外のビューを作成します。メモリが配列に基づいていない場合、ビューは空になります。

```csharp
using System;
using Arc.Collections;

var rented = BytePool.Default.Rent(100);
Console.WriteLine(rented.Array.Length); // 128
var view = rented.AsMemory(0, 100); // ビューであり、所有者ではありません。
view.Span.Clear();
var shared = view.IncrementAndShare(); // 2 つ目の所有者。
Console.WriteLine(rented.ReferenceCount); // 2

rented.Return();
shared.Return(); // 最後の返却：これ以降、すべてのハンドルとビューは無効です。
```

### Temporary buffers and builders

- `SpanOwner<T>`、`SequenceBuilder<T>`、`PooledStringBuilder` は、プールされた配列を所有する `ref struct` 型です。リソースを所有している間はコピーせず、破棄は 1 回だけ行い、破棄後はその span やシーケンスを使わないでください。
- `SpanOwner<T>` は、借りた配列を使用前にクリアしません。`BaseHelper.StackallocThreshold`（1024）は、`stackalloc` で確保する作業用 span の推奨上限サイズです。
- `SequenceBuilder<T>.ToReadOnlySequence()` はビルダーを確定させます。以降の `Add` 呼び出しは `InvalidOperationException` をスローします。
- `PooledStringBuilder` は、プロバイダーを渡さない限りインバリアントカルチャで書式設定します。null のプロバイダーを渡すと、数値と日付は現在のカルチャで書式設定されます。`AppendLine` は `\n` を追加します。

```csharp
using System;
using Arc;
using Arc.Collections;

using var owner = new SpanOwner<byte>(stackalloc byte[BaseHelper.StackallocThreshold], 4096); // 4096 > 1024 なのでプールから借ります。
owner.Span.Clear();

using var text = new PooledStringBuilder();
text.Append("Total: ");
text.Append(1234.5);
Console.WriteLine(text.ToString()); // Total: 1234.5
```

## Helpers

| 型または名前空間 | 用途 |
| --- | --- |
| `XxHash3Slim` | アロケーションなしの非暗号学的な XXH3 64 ビットハッシュ。`Hash64(ReadOnlySpan<byte>, seed)` は `System.IO.Hashing.XxHash3.HashToUInt64` と同じ値を返します。`Hash64(string)` は UTF-8 ではなく UTF-16 のコードユニットをハッシュします。 |
| `CollectionHelper` | 容量を 2 の累乗に切り上げる処理と、ハッシュテーブルのサイズを決めるための素数テーブル。 |
| `TagObject` | 0 から 255 までのタグごとに 1 つずつキャッシュされたオブジェクト。小さなタグをボックス化なしで `object` フィールドに格納するために使います。 |
| `Arc.BaseHelper` | 行、テキストの走査、数値、UTF-8、変換、埋め込みリソースに関するヘルパー。 |
| `Arc.Struct128`、`Arc.Struct256` | フィールドが重なり合う固定長のバイナリ値。バイトはネイティブのバイト順で、`CompareTo` は符号付きの `long` フィールドを順に比較します。 |
| `Arc.IStringConvertible<T>`、`Arc.IUtf8Convertible<T>`、`Arc.IConversionOptions` | span ベースで UTF-16 / UTF-8 の解析と書式設定を行うためのコントラクトと、型付きのオプション。 |
| `Arc.VersionHelper` | エントリアセンブリ、または名前の部分一致で選んだ読み込み済みアセンブリ（`SetAssembly`）のバージョン番号。 |
| `Arc.AppCloseHandler` | 最初に登録されたハンドラーを、プロセスの終了時または Windows のコンソールクローズイベント時（Ctrl+C は対象外）に最大 1 回実行します。2 回目以降の `Register` 呼び出しは無視されます。 |
| `Arc.Collections.HotMethod` | `HotMethodResolver` は、8～128 ビットの整数型、`float`、`double`、`DateTime` 向けに `IHotMethod<T>` と `IHotTreeMethod<TKey, TValue>` による検索を提供します。`IHotMethod` と `IHotTreeMethod` は、それらの非ジェネリックなマーカーです。 |

- `BaseHelper.GetCompleteUtf8Length` は末尾の不完全なシーケンスを除外しますが、テキストの検証は行いません。`TryLoadResource(null, name)` は、呼び出し元ではなく Arc.Collections アセンブリからリソースを読み込みます。
- `BaseHelper.ConvertToString` と `ConvertToUtf8` は、長さを取得できない場合や書式設定に失敗した場合に空の結果を返します。より大きなバッファでの再試行はしません。
- 各変換インターフェースでは、長さを表すメンバー（`GetStringLength()` または `MaxStringLength`、`GetUtf8Length()` または `MaxUtf8Length`）の少なくとも一方が 0 以上の長さを返す必要があります。もう一方は -1 を返してもかまいません。
- ソート済みコレクションは、既定の比較子を使う場合に `HotMethod` の検索を自動的に使います。ライブラリーがカスタムの `IHotMethodResolver` を呼び出すことはありません。

## Thread safety

| 型 | 並行使用 |
| --- | --- |
| `CircularQueue<T>` | 複数のプロデューサーとコンシューマーから使用できます。 |
| 数値および UTF のハッシュテーブル | 読み取りと書き込みを任意に組み合わせて並行に使用できます。 |
| `ObjectPool<T>` | `Rent` と `Return` はスレッドセーフです。`Dispose` をそれらと同時に実行しないでください。 |
| `KeyedObjectCache<TKey, TObject>` | すべてのメンバーがスレッドセーフです。 |
| `BytePool` とそのハンドル | 貸し出し、返却、参照カウントはスレッドセーフですが、`SetPoolLimit` はスレッドセーフではありません。バイト列へのアクセスは同期されません。 |
| `XxHash3Slim`、`HotMethodResolver`、`AppCloseHandler`、`VersionHelper` | スレッドセーフです。ただし `VersionHelper.SetAssembly` は例外で、起動時に呼び出してください。 |
| `SpanOwner<T>`、`SequenceBuilder<T>`、`PooledStringBuilder`、`TemporaryList<T>` | スレッドセーフではありません。インスタンスを共有しないでください。 |
| その他のすべてのコレクション | 書き込むスレッドがない間は、並行して読み取っても安全です。それ以外の場合は、すべてのアクセスを同期してください。`SlidingList<T>.GetFirstOrDefault()` は書き込み操作です。 |

## Build and test

.NET 10 SDK をインストールした環境で、リポジトリのルートから実行します。

```sh
dotnet restore Arc.Collections.slnx
dotnet build Arc.Collections.slnx -c Release --no-restore
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build
```

テストは Microsoft.Testing.Platform 上の xUnit v3 を使っています。`dotnet run --project xUnitTest/xUnitTest.csproj -c Release --no-build -- --help` でオプションの一覧を表示できます。約 2 GB を確保するテストが 1 つあり、既定ではスキップされます。これを含めるにはテストコマンドに `--explicit on` を、このテストだけを実行するには `--explicit only` を追加してください。SIMD パスのスカラー版フォールバックをテストするには、環境変数 `DOTNET_EnableHWIntrinsic=0` を設定してください。

`Arc.Collections.dll` の行カバレッジと分岐カバレッジを `TestResults/coverage.cobertura.xml` に収集するには、次のコマンドを実行します（CI と同じコマンドです）。

```sh
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build --coverage --coverage-settings xUnitTest/coverage.config --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --results-directory TestResults
```

## Performance

`Benchmark` プロジェクトには BenchmarkDotNet のワークロードが含まれています。`--list flat` で一覧を表示し、`--filter` で選択します。

```sh
dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*OrderedPublicTest*"
```

以下の結果（`OrderedPublicTest`）は、`OrderedSet<T>` と `System.Collections.Generic.SortedSet<T>` を比較したものです。これは過去の測定結果です。ランタイムとハードウェアが記録されていないため、現在の性能を反映していない可能性があります。

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

Arc.Collections は [MIT License](../LICENSE) の下で公開されています。
