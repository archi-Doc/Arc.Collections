# コード点検・テストカバレッジ調査

測定環境: Windows x64 / .NET SDK 10.0.400 / .NET 10.0.11。
測定ツール: dotnet-coverage 18.11.0。

## 修正内容

- **CircularQueue**: 容量1では満杯のスロットと次の書き込み位置を区別できず、未読要素が上書きされる。最小容量を2に変更し、XMLドキュメントと容量のテストを更新した。`ObjectPool`など、このキューを内部で利用する型にも適用される。
- **KeyedObjectCache**: 満杯の状態で重複キーを登録すると、登録できない場合でも既存オブジェクトを追い出していた。重複確認を退避処理より前に移した。
- **KeyedObjectCache**: Dispose後の返却でオブジェクトが再びキャッシュに残り、解放されなかった。破棄状態をロック内で管理し、破棄済みキャッシュへの登録を拒否する。`Cache`がfalseを返す場合は呼び出し元が所有権を保持し、`Interface.Return/Dispose`からの返却ではオブジェクトを破棄する。
- **OrderedList**: 自身を`AddRange`に渡すと途中で例外になり、内部配列と重なるSpanを渡すと追加中の移動で元データが変化していた。重なる入力だけスナップショットを作成する。
- **OrderedList**: コンストラクターのソートが不安定で、比較結果が等しい要素の順番を保持するという仕様に反していた。安定ソートに変更した。
- **BytePool**: メモリの`Slice`が親の範囲ではなく配列全体の範囲を基準にしていた。負の開始位置、親範囲を超える長さ、空メモリに対する不正な切り出しを拒否する。
- **BytePool**: 単独所有から共有への変更と返却の更新がアトミックではなかった。参照カウントをCASで更新し、上限値が単独所有用の特別値に変化することも防止した。
- **SpanOwner**: 参照型を含む配列をプールに返す際に参照を消去せず、不要なオブジェクトを保持していた。参照を含む型のみクリアする。
- **OrderedSet / OrderedMultiSet**: 非ジェネリック列挙子の`Current`が列挙開始前・終了後にNullReferenceExceptionを出していた。内部の既存状態検査を利用し、InvalidOperationExceptionに統一した。ジェネリック列挙の経路は変更していない。
- **CollectionHelper**: `ExpandPrime`が上限容量でオーバーフローし、容量3を返していた。上限で飽和させる。
- **Benchmark**: 起動時に選択対象とは無関係に走っていた`DebugRun<SpanownerBenchmark>()`の呼び出しを削除した。手動確認用のメソッドと既存のコメントアウトコードは保持した。

速度向上を推測した変更は追加していない。安定ソートと参照カウントの変更は正しさを満たすためのものであり、高速化の主張はしていない。

## カバレッジ

以下はRelease構成・通常のCPU設定で測定した **Arc.Collectionsアセンブリ単体** の値。テスト自身や依存ライブラリの値は含めない。生成コードも除外していない。

| 指標 | 修正前 | 修正後 |
| --- | ---: | ---: |
| 成功テスト数 | 536 | 601 |
| 行カバレッジ | 52.57% | 80.21% |
| 分岐カバレッジ | 47.83% | 72.45% |

追加した65ケースには、修正箇所の回帰テスト、Dictionary/SortedSetとの比較、固定シードによる操作列、衝突・拡張・削除後の再利用、NaNと極値の比較、参照カウントの並行更新、配列返却後の参照解放、標準XXH3実装との照合を含む。

主なファイルの行カバレッジ:

| ファイル | 修正前 | 修正後 |
| --- | ---: | ---: |
| Int32Hashtable.cs | 0% | 95.7% |
| UInt32Hashtable.cs | 0% | 95.8% |
| Int64Hashtable.cs | 0% | 96.0% |
| Utf8Hashtable.cs | 0% | 93.3% |
| Utf8UnorderedMap.cs | 0% | 94.1% |
| OrderedSet.cs | 7.5% | 94.4% |
| OrderedMultiSet.cs | 0% | 95.5% |
| PrimitiveMethod.cs | 14.5% | 100% |
| CollectionHelper.cs | 15.4% | 100% |
| Struct128.cs / Struct256.cs | 16.3% / 11.9% | 100% / 100% |
| ObjectPool.cs | 50.0% | 95.5% |
| BytePool.cs | 26.7% | 54.1% |
| KeyedObjectCache.cs | 98.4% | 100% |

## 検証結果

- ソリューション全体のReleaseビルド: 警告0、エラー0。
- Release / Debug: それぞれ601ケース成功。
- AVX2無効 / ハードウェア組み込み命令全無効: それぞれ601ケース成功。ハッシュ・合計処理の代替経路も検証した。
- Sandbox: 通常実行とWindows x64 NativeAOTへの発行・生成実行ファイルの実行に成功。
- `git diff --check`: 成功。
- 既存のブロックコメントを原文と比較し、保持を確認。行コメントの削除は仕様を更新したXMLドキュメントだけで、コメントアウトされたコードの削除はない。

## 再測定

リポジトリのルートから実行する。ツールはプロジェクト内のGit対象外ディレクトリに置く。

```powershell
dotnet restore Arc.Collections.slnx
dotnet build Arc.Collections.slnx -c Release --no-restore
# ツールが未導入の場合のみ:
dotnet tool install dotnet-coverage --version 18.11.0 --tool-path artifacts/tools
artifacts/tools/dotnet-coverage collect -o artifacts/coverage-after.cobertura.xml -f cobertura dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build --no-restore

[xml]$coverage = Get-Content artifacts/coverage-after.cobertura.xml
$coverage.coverage.packages.package |
    Where-Object name -eq 'Arc.Collections' |
    Select-Object name, line-rate, branch-rate
```

測定XMLは`artifacts/coverage-before.cobertura.xml`、`artifacts/coverage-after.cobertura.xml`、`artifacts/coverage-no-avx2.cobertura.xml`、`artifacts/coverage-scalar.cobertura.xml`に保存した。XMLの率は0〜1の値。

## 残る検証範囲

100%の網羅や無不具合を保証する調査ではない。AppCloseHandlerとVersionHelperは今回の単体テストでは未実行。OSの終了イベント、ARM固有の命令経路、実際の最大サイズ配列の確保、すべてのスレッド間実行順序は検証していない。BytePoolの一部オーバーロード、OrderedKeyValueListの非ジェネリックAPIなどにも未実行の経路が残る。ファイル単位の詳細は測定XMLを参照。
