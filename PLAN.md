# Arc.Collections 全ソースコード精査計画

## ユーザープロンプト（原文）

```text
目的: Arc.Collectionsプロジェクトのソースコードを精査する
対象: 未入力
完成条件: 全てのコードを対象に、クラス・構造体・レコード別の項目に分けて精査する。不具合、脆弱性がないか調査して、修正する。パフォーマンス改善策があるか調査し、あれば適用する。全てのコードの精査が終了したら完了とする。
対象外・制約: 未入力
```

## 実行計画

対象の未入力部分は、目的と「全てのコード」に基づき、ソリューション内の4プロジェクトと関連設定・生成元として具体化する。ユーザー指定の対象外・制約はないため、対応するJSON配列は空とする。共通実行基盤の保護はautoframe/SPEC.mdに従う。

型ごとの項目は全メンバーを対象とし、共通の確認手順は完成条件に定める。partialの複数宣言や作業中に増えた型は、棚卸しと最終監査で内部計画へ対応付ける。型に属さないコードにも必須項目を設ける。性能改善の採否は測定と正確性の検証に基づいて判断する。

<!-- autoframe:begin -->
```json
{
  "schema_version": 1,
  "project_id": "arc-collections-source-review",
  "objective": "Arc.Collectionsプロジェクトの全ソースコードを型別に精査し、不具合・脆弱性を修正し、有効なパフォーマンス改善を適用する。",
  "scope": [
    "Arc.Collections.slnxに含まれるArc.Collections、xUnitTest、Benchmark、Sandboxの全ソース。Benchmark/Design、Benchmark/Obsolete、コメント内に保存された旧実装、ビルド除外コードも含む。",
    "クラス・構造体・レコードを完全修飾名とソースパスで識別し、入れ子型も独立した必須項目として精査する。インターフェース・列挙型・デリゲートも個別項目とし、型に属さないコードを残余項目で扱う。",
    "Arc.Collections/HotMethod/PrimitiveMethod.ttと生成済みPrimitiveMethod.cs、プロジェクト直下のビルド・解析設定、ソリューション、.github/workflows、README.mdとdocの契約・利用説明。",
    "作業中に追加・変更された対象コードも台帳へ反映し、最終時点の全対象と照合する。autoframeは共通実行基盤としてSPEC.mdに従って参照し、その配布物と内部記録は製品精査の対象に含めない。bin/obj等のビルド出力と外部依存の実装ソースは棚卸しの対象ではないが、依存関係の利用・安全性は確認する。"
  ],
  "non_goals": [],
  "constraints": [],
  "work_scope": [
    "Arc.Collections/**",
    "xUnitTest/**",
    "Benchmark/**",
    "Sandbox/**",
    "doc/**",
    "README.md",
    "Directory.Build.props",
    "Directory.Build.targets",
    "global.json",
    "NuGet.Config",
    "nuget.config",
    "Directory.Packages.props",
    "Arc.Collections.slnx",
    "Arc.Collections.sln.old",
    ".editorconfig",
    "stylecop.json",
    ".github/workflows/**"
  ],
  "generated_scope": [
    "**/.vs/**",
    "**/bin/**",
    "**/obj/**",
    "**/TestResults/**",
    "**/BenchmarkDotNet.Artifacts/**"
  ],
  "environment_checks": [
    "dotnet --info",
    "dotnet --list-sdks",
    "dotnet --list-runtimes",
    "$PSVersionTable.PSVersion.ToString()"
  ],
  "references": [
    {
      "purpose": "§2の計画形式、保護範囲、実行・証拠の規則",
      "path": "autoframe/SPEC.md"
    },
    {
      "purpose": "公開APIの契約、既存のビルド・テスト・ベンチマーク手順",
      "path": "README.md"
    },
    {
      "purpose": "日本語のAPI説明・利用例",
      "path": "doc/README.jp.md"
    },
    {
      "purpose": "現行の4プロジェクト構成",
      "path": "Arc.Collections.slnx"
    },
    {
      "purpose": "Microsoft.Testing.Platformの選択",
      "path": "global.json"
    },
    {
      "purpose": "言語版と共通ビルド条件",
      "path": "Directory.Build.props"
    },
    {
      "purpose": "既存CIの環境と検証手順",
      "path": ".github/workflows/test.yml"
    },
    {
      "purpose": "BenchmarkDotNetの登録・起動方式",
      "path": "Benchmark/Program.cs"
    }
  ],
  "completion_criteria": [
    {
      "verification": "初期と最終のソース台帳・構文解析結果を必須項目と照合する。完全修飾名、ジェネリック引数、入れ子、partial、条件付き・除外・旧実装、生成元を含めて精査漏れ0件を確認する。証拠・進捗・指摘は内部記録へ保存する。",
      "condition": "対象範囲内の全ファイル・全型・全メンバー・非型コードが必須項目へ対応し、クラス・構造体・レコード別の精査が全件完了している。",
      "id": "C-Coverage"
    },
    {
      "verification": "各型の全メンバーと呼出関係について契約・不変条件・null/default・空/最大値・整数オーバーフロー・比較/ハッシュ・列挙・例外安全性を確認する。該当箇所はunsafe/Spanのメモリ境界、所有権、返却後参照、情報残留、並行性、入力による過剰資源消費も確認する。問題ごとに再現条件、原因、修正と必要な回帰テストを対応付け、変更不要の型も具体的な確認範囲と根拠を残す。",
      "condition": "全対象で不具合と脆弱性を調査し、発見した該当問題を修正して検証済みである。",
      "id": "C-Correctness"
    },
    {
      "verification": "型ごとに計算量、割当、コピー、検索/走査、容量拡張、キャッシュ、同期コストの該当性を検討する。候補は同じSDK・構成・入力・測定環境で変更前後を比較し、時間・割当量・分散と代表的入力/境界値を確認する。BenchmarkDotNetの既存測定を利用し、不足する比較は追加する。改善なし・不採用は理由を記録し、未測定の推測だけで採用しない。測定不能な改善候補を残して完了にしない。",
      "condition": "全対象で性能改善策を調査し、正確性・安全性を維持して有効と確認できた改善を適用している。",
      "id": "C-Performance"
    },
    {
      "verification": "README.mdの dotnet restore Arc.Collections.slnx、dotnet build Arc.Collections.slnx -c Release --no-restore、dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-restore を順に実行する。終了コード0、実行テスト1件以上、失敗0件を確認し、既存のスキップ理由を確認して新たな無効化で成功を作らない。性能比較は dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter \"<対象パターン>\" を対象に合わせて実行する。環境や依存不足、未実行を成功としない。必要性を確認した追加検証の結果も保存する。",
      "condition": "最終ソースに対するソリューションのReleaseビルドと全xUnitテストが成功し、必要な回帰・安全性・性能検証が完了している。",
      "id": "C-Validation"
    },
    {
      "verification": "全体監査でソース台帳と型別結果を再照合する。追加・更新したコードの再精査、発見した不具合・脆弱性の修正、全性能候補の採否、最終変更後の検証結果を確認する。未精査・未解決・未検証項目0件をもって完了とし、単なるテスト成功や時間上限では完了にしない。",
      "condition": "最終時点の全対象の精査が終了し、全必須項目・全マイルストーン・全完成条件が有効な検証結果で確認できる。",
      "id": "C-Completion"
    }
  ],
  "tasks": [
    {
      "id": "T-Inventory",
      "description": "全ソースと型を棚卸しし、実行環境・既存検証の基準を記録する",
      "required": true,
      "depends_on": [],
      "criterion_ids": [
        "C-Coverage",
        "C-Validation"
      ],
      "acceptance": "全ファイル・完全修飾型名・入れ子・partialの各宣言・非型コードと必須項目の対応表がある。条件付きコード、コメント内の旧実装、ビルド除外コード、生成コードと生成元も対応付ける。既存の変更と検証結果を区別できる。",
      "verification": "rg --files と構文解析・プロジェクト設定を照合し、初期項目にない型や分岐を内部計画の必須項目に追加する。README.mdのrestore/build/test手順とdotnet --infoを確認する。基準測定時の失敗は原因・再現手順を記録して後続修正へ引き継ぐ。"
    },
    {
      "id": "T-Arc-001",
      "description": "クラス Arc.AppCloseHandler — Arc.Collections/Arc/AppCloseHandler.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-002",
      "description": "デリゲート Arc.AppCloseHandler.ConsoleEventDelegate — Arc.Collections/Arc/AppCloseHandler.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-003",
      "description": "クラス Arc.BaseHelper — Arc.Collections/Arc/BaseHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-004",
      "description": "インターフェース Arc.IConversionOptions — Arc.Collections/Arc/IConversionOptions.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-005",
      "description": "インターフェース Arc.IStringConvertible<T> — Arc.Collections/Arc/IStringConvertible.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-006",
      "description": "インターフェース Arc.IUtf8Convertible<T> — Arc.Collections/Arc/IUtf8Convertible.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-007",
      "description": "構造体 Arc.Struct128 — Arc.Collections/Arc/Struct128.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-008",
      "description": "構造体 Arc.Struct256 — Arc.Collections/Arc/Struct256.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Arc-009",
      "description": "クラス Arc.VersionHelper — Arc.Collections/Arc/VersionHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 値表現、変換、境界値、比較、ライフサイクル",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-001",
      "description": "クラス Arc.Collections.CircularQueue<T> — Arc.Collections/Collection/CircularQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-002",
      "description": "構造体 Arc.Collections.CircularQueue<T>.Slot — Arc.Collections/Collection/CircularQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-003",
      "description": "構造体 Arc.Collections.PaddedHeadAndTail — Arc.Collections/Collection/CircularQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-004",
      "description": "クラス Arc.Collections.OrderedKeyValueList<TKey, TValue> — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-005",
      "description": "構造体 Arc.Collections.OrderedKeyValueList<TKey, TValue>.ComparableCompare — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-006",
      "description": "構造体 Arc.Collections.OrderedKeyValueList<TKey, TValue>.ComparerCompare — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-007",
      "description": "構造体 Arc.Collections.OrderedKeyValueList<TKey, TValue>.DefaultCompare — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-008",
      "description": "構造体 Arc.Collections.OrderedKeyValueList<TKey, TValue>.Enumerator — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-009",
      "description": "インターフェース Arc.Collections.OrderedKeyValueList<TKey, TValue>.IKeyCompare — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-010",
      "description": "クラス Arc.Collections.OrderedKeyValueList<TKey, TValue>.KeyList — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-011",
      "description": "クラス Arc.Collections.OrderedKeyValueList<TKey, TValue>.SortedListKeyEnumerator — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-012",
      "description": "クラス Arc.Collections.OrderedKeyValueList<TKey, TValue>.SortedListValueEnumerator — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-013",
      "description": "クラス Arc.Collections.OrderedKeyValueList<TKey, TValue>.ValueList — Arc.Collections/Collection/OrderedKeyValueList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-014",
      "description": "クラス Arc.Collections.OrderedList<T> — Arc.Collections/Collection/OrderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-015",
      "description": "構造体 Arc.Collections.OrderedList<T>.ComparableCompare — Arc.Collections/Collection/OrderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-016",
      "description": "構造体 Arc.Collections.OrderedList<T>.ComparerCompare — Arc.Collections/Collection/OrderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-017",
      "description": "構造体 Arc.Collections.OrderedList<T>.DefaultCompare — Arc.Collections/Collection/OrderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-018",
      "description": "インターフェース Arc.Collections.OrderedList<T>.IValueCompare — Arc.Collections/Collection/OrderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-019",
      "description": "列挙型 Arc.Collections.NodeColor — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-020",
      "description": "クラス Arc.Collections.OrderedMap<TKey, TValue> — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-021",
      "description": "構造体 Arc.Collections.OrderedMap<TKey, TValue>.Enumerator — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-022",
      "description": "構造体 Arc.Collections.OrderedMap<TKey, TValue>.KeyEnumerable — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-023",
      "description": "構造体 Arc.Collections.OrderedMap<TKey, TValue>.KeyEnumerable.Enumerator — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-024",
      "description": "クラス Arc.Collections.OrderedMap<TKey, TValue>.Node — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-025",
      "description": "構造体 Arc.Collections.OrderedMap<TKey, TValue>.ValueEnumerable — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-026",
      "description": "構造体 Arc.Collections.OrderedMap<TKey, TValue>.ValueEnumerable.Enumerator — Arc.Collections/Collection/OrderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-027",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue> — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-028",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.Enumerator — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-029",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.KeyEnumerable — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-030",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.KeyEnumerable.Enumerator — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-031",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.MatchedValueEnumerable — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-032",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.MatchedValueEnumerable.Enumerator — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-033",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue>.Node — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-034",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.NodeEnumerable — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-035",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.NodeEnumerable.Enumerator — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-036",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.ValueEnumerable — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-037",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.ValueEnumerable.Enumerator — Arc.Collections/Collection/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-038",
      "description": "クラス Arc.Collections.OrderedMultiSet<T> — Arc.Collections/Collection/OrderedMultiSet.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-039",
      "description": "構造体 Arc.Collections.OrderedMultiSet<T>.Enumerator — Arc.Collections/Collection/OrderedMultiSet.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-040",
      "description": "クラス Arc.Collections.OrderedSet<T> — Arc.Collections/Collection/OrderedSet.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-041",
      "description": "構造体 Arc.Collections.OrderedSet<T>.Enumerator — Arc.Collections/Collection/OrderedSet.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-042",
      "description": "クラス Arc.Collections.SlidingList<T> — Arc.Collections/Collection/SlidingList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-043",
      "description": "構造体 Arc.Collections.SlidingList<T>.Enumerator — Arc.Collections/Collection/SlidingList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-044",
      "description": "クラス Arc.Collections.SlidingList<T>.ThrowHelper — Arc.Collections/Collection/SlidingList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-045",
      "description": "構造体 Arc.Collections.TemporaryList<T> — Arc.Collections/Collection/TemporaryList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-046",
      "description": "構造体 Arc.Collections.TemporaryList<T>.Enumerator — Arc.Collections/Collection/TemporaryList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-047",
      "description": "クラス Arc.Collections.UnorderedLinkedList<T> — Arc.Collections/Collection/UnorderedLinkedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-048",
      "description": "構造体 Arc.Collections.UnorderedLinkedList<T>.Enumerator — Arc.Collections/Collection/UnorderedLinkedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-049",
      "description": "クラス Arc.Collections.UnorderedLinkedList<T>.Node — Arc.Collections/Collection/UnorderedLinkedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-050",
      "description": "クラス Arc.Collections.UnorderedList<T> — Arc.Collections/Collection/UnorderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-051",
      "description": "構造体 Arc.Collections.UnorderedList<T>.Enumerator — Arc.Collections/Collection/UnorderedList.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-052",
      "description": "クラス Arc.Collections.UnorderedMap<TKey, TValue> — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-053",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.Enumerator — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-054",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.KeyEnumerable — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-055",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.KeyEnumerable.Enumerator — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-056",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.MatchedValueEnumerable — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-057",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.MatchedValueEnumerable.Enumerator — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-058",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.Node — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-059",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.NodeEnumerable — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-060",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.NodeEnumerable.Enumerator — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-061",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.ValueEnumerable — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-062",
      "description": "構造体 Arc.Collections.UnorderedMap<TKey, TValue>.ValueEnumerable.Enumerator — Arc.Collections/Collection/UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-063",
      "description": "クラス Arc.Collections.UnorderedMapSlim<TKey, TValue> — Arc.Collections/Collection/UnorderedMapSlim.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-064",
      "description": "構造体 Arc.Collections.UnorderedMapSlim<TKey, TValue>.Enumerator — Arc.Collections/Collection/UnorderedMapSlim.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-065",
      "description": "構造体 Arc.Collections.UnorderedMapSlim<TKey, TValue>.Node — Arc.Collections/Collection/UnorderedMapSlim.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Collection-066",
      "description": "クラス Arc.Collections.UnorderedSet<T> — Arc.Collections/Collection/UnorderedSet.cs を精査・修正し、性能改善を検討・適用する。重点: 内部不変条件、追加・削除・更新、重複、順序、列挙中変更、例外安全性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-001",
      "description": "クラス Arc.Collections.HashtableHelper — Arc.Collections/Hashtable/HashtableHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-002",
      "description": "クラス Arc.Collections.Int32Hashtable<TValue> — Arc.Collections/Hashtable/Int32Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-003",
      "description": "クラス Arc.Collections.Int32Hashtable<TValue>.Item — Arc.Collections/Hashtable/Int32Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-004",
      "description": "クラス Arc.Collections.Int64Hashtable<TValue> — Arc.Collections/Hashtable/Int64Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-005",
      "description": "クラス Arc.Collections.Int64Hashtable<TValue>.Item — Arc.Collections/Hashtable/Int64Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-006",
      "description": "クラス Arc.Collections.UInt32Hashtable<TValue> — Arc.Collections/Hashtable/UInt32Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-007",
      "description": "クラス Arc.Collections.UInt32Hashtable<TValue>.Item — Arc.Collections/Hashtable/UInt32Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-008",
      "description": "クラス Arc.Collections.UInt64Hashtable<TValue> — Arc.Collections/Hashtable/UInt64Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-009",
      "description": "クラス Arc.Collections.UInt64Hashtable<TValue>.Item — Arc.Collections/Hashtable/UInt64Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-010",
      "description": "クラス Arc.Collections.Utf16Hashtable<TValue> — Arc.Collections/Hashtable/Utf16Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-011",
      "description": "クラス Arc.Collections.Utf16Hashtable<TValue>.Item — Arc.Collections/Hashtable/Utf16Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-012",
      "description": "クラス Arc.Collections.Utf16UnorderedMap<TValue> — Arc.Collections/Hashtable/Utf16UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-013",
      "description": "構造体 Arc.Collections.Utf16UnorderedMap<TValue>.Enumerator — Arc.Collections/Hashtable/Utf16UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-014",
      "description": "構造体 Arc.Collections.Utf16UnorderedMap<TValue>.Node — Arc.Collections/Hashtable/Utf16UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-015",
      "description": "クラス Arc.Collections.Utf8Hashtable<TValue> — Arc.Collections/Hashtable/Utf8Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-016",
      "description": "クラス Arc.Collections.Utf8Hashtable<TValue>.Item — Arc.Collections/Hashtable/Utf8Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-017",
      "description": "クラス Arc.Collections.Utf8UnorderedMap<TValue> — Arc.Collections/Hashtable/Utf8UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-018",
      "description": "構造体 Arc.Collections.Utf8UnorderedMap<TValue>.Enumerator — Arc.Collections/Hashtable/Utf8UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Hashtable-019",
      "description": "構造体 Arc.Collections.Utf8UnorderedMap<TValue>.Node — Arc.Collections/Hashtable/Utf8UnorderedMap.cs を精査・修正し、性能改善を検討・適用する。重点: 衝突、拡張、整数境界、キー所有権、UTF-8/UTF-16、不正入力",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-001",
      "description": "インターフェース Arc.Collections.HotMethod.IHotMethod — Arc.Collections/HotMethod/IHotMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-002",
      "description": "インターフェース Arc.Collections.HotMethod.IHotMethod<T> — Arc.Collections/HotMethod/IHotMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-003",
      "description": "クラス Arc.Collections.HotMethod.HotMethodResolver — Arc.Collections/HotMethod/IHotMethodResolver.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-004",
      "description": "インターフェース Arc.Collections.HotMethod.IHotMethodResolver — Arc.Collections/HotMethod/IHotMethodResolver.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-005",
      "description": "インターフェース Arc.Collections.HotMethod.IHotTreeMethod — Arc.Collections/HotMethod/IHotTreeMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-006",
      "description": "インターフェース Arc.Collections.HotMethod.IHotTreeMethod<TKey, TValue> — Arc.Collections/HotMethod/IHotTreeMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-007",
      "description": "クラス Arc.Collections.HotMethod.DateTimeMethod — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-008",
      "description": "クラス Arc.Collections.HotMethod.DateTimeMethod2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-009",
      "description": "クラス Arc.Collections.HotMethod.DoubleMethod — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-010",
      "description": "クラス Arc.Collections.HotMethod.DoubleMethod2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-011",
      "description": "クラス Arc.Collections.HotMethod.Int128Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-012",
      "description": "クラス Arc.Collections.HotMethod.Int128Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-013",
      "description": "クラス Arc.Collections.HotMethod.Int16Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-014",
      "description": "クラス Arc.Collections.HotMethod.Int16Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-015",
      "description": "クラス Arc.Collections.HotMethod.Int32Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-016",
      "description": "クラス Arc.Collections.HotMethod.Int32Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-017",
      "description": "クラス Arc.Collections.HotMethod.Int64Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-018",
      "description": "クラス Arc.Collections.HotMethod.Int64Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-019",
      "description": "クラス Arc.Collections.HotMethod.Int8Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-020",
      "description": "クラス Arc.Collections.HotMethod.Int8Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-021",
      "description": "クラス Arc.Collections.HotMethod.SingleMethod — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-022",
      "description": "クラス Arc.Collections.HotMethod.SingleMethod2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-023",
      "description": "クラス Arc.Collections.HotMethod.UInt128Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-024",
      "description": "クラス Arc.Collections.HotMethod.UInt128Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-025",
      "description": "クラス Arc.Collections.HotMethod.UInt16Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-026",
      "description": "クラス Arc.Collections.HotMethod.UInt16Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-027",
      "description": "クラス Arc.Collections.HotMethod.UInt32Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-028",
      "description": "クラス Arc.Collections.HotMethod.UInt32Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-029",
      "description": "クラス Arc.Collections.HotMethod.UInt64Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-030",
      "description": "クラス Arc.Collections.HotMethod.UInt64Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-031",
      "description": "クラス Arc.Collections.HotMethod.UInt8Method — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-032",
      "description": "クラス Arc.Collections.HotMethod.UInt8Method2<TValue> — Arc.Collections/HotMethod/PrimitiveMethod.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-033",
      "description": "クラス Arc.Collections.HotMethod.PrimitiveResolver — Arc.Collections/HotMethod/PrimitiveResolver.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-034",
      "description": "クラス Arc.Collections.HotMethod.PrimitiveResolver.MethodCache<T> — Arc.Collections/HotMethod/PrimitiveResolver.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-HotMethod-035",
      "description": "クラス Arc.Collections.HotMethod.PrimitiveResolver.MethodCache2<TKey, TValue> — Arc.Collections/HotMethod/PrimitiveResolver.cs を精査・修正し、性能改善を検討・適用する。重点: 比較・等値・ハッシュの整合、浮動小数点、型解決とキャッシュ、生成元との整合",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-001",
      "description": "クラス Arc.Collections.BytePool — Arc.Collections/Misc/BytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-002",
      "description": "クラス Arc.Collections.BytePool.Bucket — Arc.Collections/Misc/BytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-003",
      "description": "クラス Arc.Collections.BytePool.RentedArray — Arc.Collections/Misc/BytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-004",
      "description": "構造体 Arc.Collections.BytePool.RentedMemory — Arc.Collections/Misc/BytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-005",
      "description": "構造体 Arc.Collections.BytePool.RentedReadOnlyMemory — Arc.Collections/Misc/BytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-006",
      "description": "クラス Arc.Collections.CollectionHelper — Arc.Collections/Misc/CollectionHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-007",
      "description": "クラス Arc.Collections.KeyedObjectCache<TKey, TObject> — Arc.Collections/Misc/KeyedObjectCache.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-008",
      "description": "クラス Arc.Collections.KeyedObjectCache<TKey, TObject>.Item — Arc.Collections/Misc/KeyedObjectCache.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-009",
      "description": "構造体 Arc.Collections.KeyedObjectCache<TKey, TObject>.Lease — Arc.Collections/Misc/KeyedObjectCache.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-010",
      "description": "クラス Arc.Collections.ObjectPool<T> — Arc.Collections/Misc/ObjectPool.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-011",
      "description": "構造体 Arc.Collections.PooledStringBuilder — Arc.Collections/Misc/PooledStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-012",
      "description": "クラス Arc.Collections.PooledStringBuilder.Segment — Arc.Collections/Misc/PooledStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-013",
      "description": "構造体 Arc.Collections.PooledStringBuilder.StringCreationState — Arc.Collections/Misc/PooledStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-014",
      "description": "構造体 Arc.Collections.SequenceBuilder<T> — Arc.Collections/Misc/SequenceBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-015",
      "description": "クラス Arc.Collections.SequenceBuilder<T>.PooledSequenceSegment — Arc.Collections/Misc/SequenceBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-016",
      "description": "構造体 Arc.Collections.SpanOwner<T> — Arc.Collections/Misc/SpanOwner.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-017",
      "description": "クラス Arc.Collections.TagObject — Arc.Collections/Misc/TagObject.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Misc-018",
      "description": "クラス Arc.Collections.XxHash3Slim — Arc.Collections/Misc/XxHash3Slim.cs を精査・修正し、性能改善を検討・適用する。重点: 所有権、返却後参照、解放、並行操作、メモリ境界、ハッシュ計算",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-001",
      "description": "クラス xUnitTest.Arc.BaseHelperTests — xUnitTest/Arc/BaseHelperTests.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-002",
      "description": "クラス XunitTest.Arc.CircularComparisonTests — xUnitTest/Arc/CircularComparisonTests.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-003",
      "description": "クラス XunitTest.StringConvertibleClass — xUnitTest/Arc/StringConvertibleTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-004",
      "description": "クラス XunitTest.StringConvertibleClass2 — xUnitTest/Arc/StringConvertibleTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-005",
      "description": "クラス XunitTest.StringConvertibleTest — xUnitTest/Arc/StringConvertibleTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-006",
      "description": "クラス XunitTest.Arc.StructTests — xUnitTest/Arc/StructTests.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-007",
      "description": "クラス XunitTest.BaseHelperTest — xUnitTest/BaseHelperTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-008",
      "description": "クラス XunitTest.ByteRentalTest — xUnitTest/ByteArrayPoolTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-009",
      "description": "クラス XunitTest.CircularQueueTest — xUnitTest/CircularQueueTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-010",
      "description": "クラス Arc.Collections.Tests.CircularQueueTest2 — xUnitTest/CircularQueueTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-011",
      "description": "クラス XunitTest.CountDecimalCharsTest — xUnitTest/CountDecimalCharsTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-012",
      "description": "クラス XunitTest.HashCoverageTest — xUnitTest/HashCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-013",
      "description": "クラス XunitTest.HelperCoverageTest — xUnitTest/HelperCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-014",
      "description": "クラス Xunit.AssertEx — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-015",
      "description": "クラス Xunit.AssertEx.EqualInfo — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-016",
      "description": "クラス Xunit.AssertEx.EqualityComparer<T> — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-017",
      "description": "クラス Xunit.AssertEx.ExpressionDumper<T> — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-018",
      "description": "クラス Xunit.AssertEx.ReflectAccessor<T> — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-019",
      "description": "クラス Xunit.AssertException — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-020",
      "description": "クラス Xunit.AssertFailedException — xUnitTest/Internal/ChainingAssertion.xUnit.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-021",
      "description": "構造体 XunitTest.Identifier — xUnitTest/Internal/Identifier.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-022",
      "description": "クラス XunitTest.TestHelper — xUnitTest/Internal/TestHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-023",
      "description": "クラス XunitTest.NumericHashtableCoverageTest — xUnitTest/NumericHashtableCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-024",
      "description": "デリゲート XunitTest.NumericHashtableCoverageTest.Lookup<TKey> — xUnitTest/NumericHashtableCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-025",
      "description": "レコードクラス XunitTest.ObjectCacheClass — xUnitTest/ObjectCacheTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-026",
      "description": "クラス XunitTest.ObjectCacheTest — xUnitTest/ObjectCacheTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-027",
      "description": "クラス XunitTest.OrderedKeyValueListClass — xUnitTest/OrderedKeyValueListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-028",
      "description": "クラス XunitTest.OrderedKeyValueListTest — xUnitTest/OrderedKeyValueListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-029",
      "description": "クラス XunitTest.OrderedKeyValueListTest2 — xUnitTest/OrderedKeyValueListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-030",
      "description": "クラス XunitTest.IntComparer — xUnitTest/OrderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-031",
      "description": "クラス XunitTest.OrderedListClass — xUnitTest/OrderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-032",
      "description": "クラス XunitTest.OrderedListClass.InternalComparer — xUnitTest/OrderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-033",
      "description": "クラス XunitTest.OrderedListTest — xUnitTest/OrderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-034",
      "description": "クラス XunitTest.OrderedListTest2 — xUnitTest/OrderedListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-035",
      "description": "クラス XunitTest.OrderedMapTest — xUnitTest/OrderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-036",
      "description": "クラス XunitTest.OrderedMapTestClass — xUnitTest/OrderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-037",
      "description": "クラス XunitTest.OrderedMapTestComparer — xUnitTest/OrderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-038",
      "description": "クラス XunitTest.OrderedMapTestComparerInv — xUnitTest/OrderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-039",
      "description": "クラス XunitTest.OrderedMapTest2 — xUnitTest/OrderedMapTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-040",
      "description": "クラス XunitTest.OrderedMultiMapTest — xUnitTest/OrderedMultiMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-041",
      "description": "クラス XunitTest.OrderedMultiMapTest2 — xUnitTest/OrderedMultiMapTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-042",
      "description": "クラス XunitTest.OrderedSetCoverageTest — xUnitTest/OrderedSetCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-043",
      "description": "クラス XunitTest.PoolCoverageTest — xUnitTest/PoolCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-044",
      "description": "クラス XunitTest.PoolCoverageTest.Disposable — xUnitTest/PoolCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-045",
      "description": "クラス XunitTest.PoolCoverageTest.PooledReference — xUnitTest/PoolCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-046",
      "description": "クラス Arc.Collections.Tests.PooledStringBuilderTest — xUnitTest/PooledStringBuilderTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-047",
      "description": "構造体 Arc.Collections.Tests.PooledStringBuilderTest.LargeFormattable — xUnitTest/PooledStringBuilderTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-048",
      "description": "クラス XunitTest.PrimitiveOrderingCoverageTest — xUnitTest/PrimitiveOrderingCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-049",
      "description": "クラス XunitTest.RegressionTest — xUnitTest/RegressionTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-050",
      "description": "クラス XunitTest.RegressionTest.DisposableObject — xUnitTest/RegressionTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-051",
      "description": "クラス XunitTest.ReviewRegressionTest — xUnitTest/ReviewRegressionTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-052",
      "description": "クラス XunitTest.ReviewRegressionTest.Disposable — xUnitTest/ReviewRegressionTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-053",
      "description": "レコードクラス XunitTest.ReviewRegressionTest.Entry — xUnitTest/ReviewRegressionTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-054",
      "description": "クラス Arc.Collections.Tests.SequenceBuilderTest — xUnitTest/SequenceBuilderTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-055",
      "description": "クラス XunitTest.SlidingListClass — xUnitTest/SlidingListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-056",
      "description": "クラス XunitTest.SlidingListTest — xUnitTest/SlidingListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-057",
      "description": "クラス XunitTest.SlidingListTest2 — xUnitTest/SlidingListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-058",
      "description": "クラス XunitTest.SumTest — xUnitTest/SumTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-059",
      "description": "クラス XunitTest.TemplateTest — xUnitTest/TemplateTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-060",
      "description": "クラス XunitTest.TemporaryListClass — xUnitTest/TemporaryListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-061",
      "description": "クラス XunitTest.TemporaryListTest — xUnitTest/TemporaryListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-062",
      "description": "クラス XunitTest.UInt64HashtableTest — xUnitTest/UInt64HashtableTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-063",
      "description": "クラス XunitTest.UnorderedLinkedListTest — xUnitTest/UnorderedLinkedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-064",
      "description": "クラス XunitTest.UnorderedLinkedListTest2 — xUnitTest/UnorderedLinkedListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-065",
      "description": "クラス XunitTest.UnorderedListTest — xUnitTest/UnorderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-066",
      "description": "クラス XunitTest.UnorderedMapSlimTest — xUnitTest/UnorderedMapSlimTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-067",
      "description": "クラス XunitTest.UnorderedMapSlimTest2 — xUnitTest/UnorderedMapSlimTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-068",
      "description": "クラス XunitTest.UnorderedMapTest — xUnitTest/UnorderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-069",
      "description": "クラス XunitTest.UnorderedMapTestClass — xUnitTest/UnorderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-070",
      "description": "クラス XunitTest.UnorderedMapTest2 — xUnitTest/UnorderedMapTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-071",
      "description": "クラス XunitTest.UnorderedSetTest — xUnitTest/UnorderedSetTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-072",
      "description": "クラス XunitTest.Utf16HashtableTest — xUnitTest/Utf16HashtableTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-073",
      "description": "クラス XunitTest.Utf16UnorderedMapTest — xUnitTest/Utf16UnorderedMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-074",
      "description": "クラス XunitTest.Utf8CollectionsCoverageTest — xUnitTest/Utf8CollectionsCoverageTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Tests-075",
      "description": "クラス XunitTest.Utf8ValidatorTests — xUnitTest/Utf8TextTest.cs を精査・修正し、性能改善を検討・適用する。重点: 期待値の妥当性、独立性、境界・例外・並行性の検証、偽陽性、再現性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-001",
      "description": "クラス Benchmark.BinarySearchStringTest — Benchmark/Benchmark/BinarySearchStringTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-002",
      "description": "クラス Benchmark.BinarySearchTest — Benchmark/Benchmark/BinarySearchTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-003",
      "description": "クラス Benchmark.BytePoolTest — Benchmark/Benchmark/BytePoolTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-004",
      "description": "クラス Benchmark.ByteSumBenchmark — Benchmark/Benchmark/ByteSumBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-005",
      "description": "クラス CircularQueueBenchmark — Benchmark/Benchmark/CircularQueueBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-006",
      "description": "クラス CircularQueueBenchmark2 — Benchmark/Benchmark/CircularQueueBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-007",
      "description": "クラス Benchmark.CountDecimalCharsBenchmark — Benchmark/Benchmark/CountDecimalCharsBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-008",
      "description": "クラス Benchmark.CountLeadingSpacesBenchmark — Benchmark/Benchmark/CountLeadingSpacesBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-009",
      "description": "クラス Benchmark.FactoryBenchmark — Benchmark/Benchmark/FactoryBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-010",
      "description": "クラス Benchmark.HashCombinerBenchmark — Benchmark/Benchmark/HashCombinerBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-011",
      "description": "クラス Benchmark.IComparerTest — Benchmark/Benchmark/IComparerTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-012",
      "description": "クラス Benchmark.OrderedListClass — Benchmark/Benchmark/IComparerTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-013",
      "description": "クラス Benchmark.OrderedListClassComparer — Benchmark/Benchmark/IComparerTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-014",
      "description": "構造体 Benchmark.Identifier — Benchmark/Benchmark/Identifier.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-015",
      "description": "クラス Benchmark.Int128Benchmark — Benchmark/Benchmark/Int128Benchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-016",
      "description": "クラス Benchmark.LimitedArrayTest — Benchmark/Benchmark/LimitedArrayTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-017",
      "description": "クラス Benchmark.LimitedArrayTestClass — Benchmark/Benchmark/LimitedArrayTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-018",
      "description": "クラス Benchmark.NewInstanceBenchmark — Benchmark/Benchmark/NewInstanceBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-019",
      "description": "クラス Benchmark.ObjectCacheBenchmark — Benchmark/Benchmark/ObjectCacheBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-020",
      "description": "レコードクラス Benchmark.ObjectCacheClass — Benchmark/Benchmark/ObjectCacheBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-021",
      "description": "クラス Benchmark.ObjectPoolBenchmark — Benchmark/Benchmark/ObjectPoolBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-022",
      "description": "レコードクラス Benchmark.ObjectPoolTestClass — Benchmark/Benchmark/ObjectPoolBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-023",
      "description": "レコード構造体 Benchmark.ObjectPoolTestStruct — Benchmark/Benchmark/ObjectPoolBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-024",
      "description": "クラス Benchmark.ObjectPoolBenchmark2 — Benchmark/Benchmark/ObjectPoolBenchmark2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-025",
      "description": "レコードクラス Benchmark.ObjectPoolTestClass2 — Benchmark/Benchmark/ObjectPoolBenchmark2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-026",
      "description": "レコード構造体 Benchmark.ObjectPoolTestStruct2 — Benchmark/Benchmark/ObjectPoolBenchmark2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-027",
      "description": "クラス Benchmark.OrderedListTest — Benchmark/Benchmark/OrderedListTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-028",
      "description": "クラス Benchmark.OrderedListClass2 — Benchmark/Benchmark/OrderedListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-029",
      "description": "クラス Benchmark.OrderedListClass2.InternalComparer — Benchmark/Benchmark/OrderedListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-030",
      "description": "クラス Benchmark.OrderedListTest2 — Benchmark/Benchmark/OrderedListTest2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-031",
      "description": "クラス Benchmark.OrderedMapSetKeyBenchmark — Benchmark/Benchmark/OrderedMapSetKeyBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-032",
      "description": "クラス Benchmark.OrderedMultiMapTest — Benchmark/Benchmark/OrderedMultiMapTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-033",
      "description": "クラス Benchmark.OrderedMultiMapUnsafe — Benchmark/Benchmark/OrderedMultiMapUnsafe.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-034",
      "description": "クラス Benchmark.OrderedPublicTest — Benchmark/Benchmark/OrderedPublicTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-035",
      "description": "クラス Benchmark.OrderedSetClass — Benchmark/Benchmark/OrderedSetTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-036",
      "description": "クラス Benchmark.OrderedSetTest — Benchmark/Benchmark/OrderedSetTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-037",
      "description": "クラス Benchmark.RemoveCrBenchmark — Benchmark/Benchmark/RemoveCrBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-038",
      "description": "クラス Benchmark.RemoveCrLfTest — Benchmark/Benchmark/RemoveCrLfTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-039",
      "description": "クラス Benchmark.ReverseOrderTest — Benchmark/Benchmark/ReverseOrderTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-040",
      "description": "クラス Benchmark.SequenceBuilderAddBenchmark — Benchmark/Benchmark/SequenceBuilderAddBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-041",
      "description": "クラス Benchmark.SortedSetBenchmark — Benchmark/Benchmark/SortedSetBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-042",
      "description": "クラス Benchmark.SpanownerBenchmark — Benchmark/Benchmark/SpanownerBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-043",
      "description": "クラス Benchmark.StringBuilderBenchmark — Benchmark/Benchmark/StringBuilderBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-044",
      "description": "クラス Benchmark.TemplateBenchmark — Benchmark/Benchmark/TemplateBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-045",
      "description": "クラス Benchmark.TemporaryListBenchmark — Benchmark/Benchmark/TemporaryListBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-046",
      "description": "クラス Benchmark.TemporaryListClass — Benchmark/Benchmark/TemporaryListBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-047",
      "description": "クラス UInt64HashtableBenchmark — Benchmark/Benchmark/UInt64HashtableBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-048",
      "description": "クラス UnorderedLinkedListBenchmark — Benchmark/Benchmark/UnorderedLinkedListBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-049",
      "description": "クラス UnorderedListBenchmark — Benchmark/Benchmark/UnorderedListBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-050",
      "description": "クラス Benchmark.UnorderedMapSlimTest — Benchmark/Benchmark/UnorderedMapSlimTest.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-051",
      "description": "クラス Benchmark.UnorderedMapValues — Benchmark/Benchmark/UnorderedMapValues.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-052",
      "description": "クラス Benchmark.Utf16HashtableBenchmark — Benchmark/Benchmark/Utf16HashtableBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-053",
      "description": "クラス Utf16HashtableBenchmark2 — Benchmark/Benchmark/Utf16HashtableBenchmark2.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-054",
      "description": "クラス Utf16UnorderedMapBenchmark — Benchmark/Benchmark/Utf16UnorderedMapBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-055",
      "description": "クラス UtfHashtableBenchmark — Benchmark/Benchmark/UtfHashtableBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-056",
      "description": "クラス Benchmark.WhiteSpaceBenchmark — Benchmark/Benchmark/WhiteSpaceBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Bench-057",
      "description": "クラス Benchmark.WhiteSpaceHelper — Benchmark/Benchmark/WhiteSpaceBenchmark.cs を精査・修正し、性能改善を検討・適用する。重点: 測定対象の正しさ、公平な比較、入力分布、初期化・後処理、割当量",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-001",
      "description": "クラス Benchmark.LooseObjectPool<T> — Benchmark/Design/LooseObjectPool.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-002",
      "description": "クラス Benchmark.LooseObjectPool<T>.Node — Benchmark/Design/LooseObjectPool.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-003",
      "description": "クラス Benchmark.ObjectPoolConcurrentQueue<T> — Benchmark/Design/ObjectPoolConcurrentQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-004",
      "description": "クラス Arc.Collections.ObjectPoolObsolete<T> — Benchmark/Design/ObjectPoolObsolete.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-005",
      "description": "構造体 Kimi.Compiler.Lexing.RefStringBuilder — Benchmark/Design/RefStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-006",
      "description": "クラス Kimi.Compiler.Lexing.RefStringBuilder.PooledSequenceSegment — Benchmark/Design/RefStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-007",
      "description": "構造体 Kimi.Compiler.Lexing.RefStringBuilder.StringCreationState — Benchmark/Design/RefStringBuilder.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-008",
      "description": "構造体 Arc.Collections.TemporaryListObsolete<TObject> — Benchmark/Design/TemporaryListObsolete.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Design-009",
      "description": "構造体 Arc.Collections.TemporaryListObsolete<TObject>.Enumerator — Benchmark/Design/TemporaryListObsolete.cs を精査・修正し、性能改善を検討・適用する。重点: 境界値、所有権、並行性、例外安全性、比較の妥当性",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-001",
      "description": "クラス BinarySearchOptimized — Benchmark/Obsolete/Branchless.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-002",
      "description": "クラス Arc.Collections.HashtableHelper — Benchmark/Obsolete/HashtableHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-003",
      "description": "クラス Arc.Collections.IntIntHashtable（コメント内に保存された旧実装） — Benchmark/Obsolete/IntIntHashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-004",
      "description": "クラス Arc.Collections.IntIntHashtable.Item（コメント内に保存された旧実装） — Benchmark/Obsolete/IntIntHashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-005",
      "description": "クラス Arc.Collections.LimitedArray<T> — Benchmark/Obsolete/LimitedArray.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-006",
      "description": "クラス Arc.Collections.Obsolete.LooseObjectPool<T>（コメント内に保存された旧実装） — Benchmark/Obsolete/LooseObjectPool.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-007",
      "description": "クラス Arc.Collections.Obsolete.LooseObjectPool<T>.Node（コメント内に保存された旧実装） — Benchmark/Obsolete/LooseObjectPool.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-008",
      "description": "クラス Arc.Collections.NotThreadsafeHashtable<TKey, TValue> — Benchmark/Obsolete/NotThreadsafeHashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-009",
      "description": "クラス Arc.Collections.NotThreadsafeHashtable<TKey, TValue>.Item — Benchmark/Obsolete/NotThreadsafeHashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-010",
      "description": "クラス Arc.Collections.ObjectPool<T>（コメント内に保存された旧実装） — Benchmark/Obsolete/ObjectPoolObsolete.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-011",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue>（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-012",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.Enumerator（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-013",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue>.KeyCollection（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-014",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.KeyCollection.Enumerator（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-015",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue>.Node（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-016",
      "description": "クラス Arc.Collections.OrderedMultiMap<TKey, TValue>.ValueCollection（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-017",
      "description": "構造体 Arc.Collections.OrderedMultiMap<TKey, TValue>.ValueCollection.Enumerator（コメント内に保存された旧実装） — Benchmark/Obsolete/OrderedMultiMap.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-018",
      "description": "クラス Arc.Collections.SimpleBytePool — Benchmark/Obsolete/SimpleBytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-019",
      "description": "クラス Arc.Collections.SimpleBytePool.Bucket — Benchmark/Obsolete/SimpleBytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-020",
      "description": "クラス Arc.Collections.SimpleBytePool.RentArray — Benchmark/Obsolete/SimpleBytePool.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-021",
      "description": "クラス Arc.Collections.Obsolete.SlidingList<T> — Benchmark/Obsolete/SlidingList.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-022",
      "description": "構造体 Arc.Collections.Obsolete.SlidingList<T>.Enumerator — Benchmark/Obsolete/SlidingList.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-023",
      "description": "構造体 Arc.Collections.TemporaryQueue<TObject>（コメント内に保存された旧実装） — Benchmark/Obsolete/TemporaryQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-024",
      "description": "構造体 Arc.Collections.TemporaryQueue<TObject>.Enumerator（コメント内に保存された旧実装） — Benchmark/Obsolete/TemporaryQueue.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-025",
      "description": "クラス Arc.Collections.UInt16Hashtable<TValue> — Benchmark/Obsolete/UInt16Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-026",
      "description": "クラス Arc.Collections.UInt16Hashtable<TValue>.Item — Benchmark/Obsolete/UInt16Hashtable.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-027",
      "description": "クラス Arc.Collections.Obsolete.UnorderedMap2<TKey, TValue> — Benchmark/Obsolete/UnorderedMap2.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-028",
      "description": "構造体 Arc.Collections.Obsolete.UnorderedMap2<TKey, TValue>.Node — Benchmark/Obsolete/UnorderedMap2.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-029",
      "description": "クラス Arc.Collections.Obsolete.UnorderedMapClass<TKey, TValue> — Benchmark/Obsolete/UnorderedMapClass.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-030",
      "description": "クラス Arc.Collections.Obsolete.UnorderedMapClass<TKey, TValue>.Node — Benchmark/Obsolete/UnorderedMapClass.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-031",
      "description": "クラス Arc.Collections.Obsolete.UnorderedMapPrime<TKey, TValue> — Benchmark/Obsolete/UnorderedMapPrime.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Obsolete-032",
      "description": "構造体 Arc.Collections.Obsolete.UnorderedMapPrime<TKey, TValue>.Node — Benchmark/Obsolete/UnorderedMapPrime.cs を精査・修正し、性能改善を検討・適用する。重点: 保存コードを含む不具合・安全性・計算量、現行実装との関係",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-BenchHost-001",
      "description": "クラス Benchmark.BenchmarkHelper — Benchmark/BenchmarkHelper.cs を精査・修正し、性能改善を検討・適用する。重点: 引数、登録漏れ、リフレクション、初期化・後処理",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-BenchHost-002",
      "description": "クラス Benchmark.BenchmarkConfig — Benchmark/Program.cs を精査・修正し、性能改善を検討・適用する。重点: 引数、登録漏れ、リフレクション、初期化・後処理",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-BenchHost-003",
      "description": "クラス Benchmark.Program — Benchmark/Program.cs を精査・修正し、性能改善を検討・適用する。重点: 引数、登録漏れ、リフレクション、初期化・後処理",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-Sandbox-001",
      "description": "クラス Sandbox.Program — Sandbox/Program.cs を精査・修正し、性能改善を検討・適用する。重点: 利用例の妥当性、終了処理、実行条件",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "当該型の全メンバーをC-Correctness/C-Performanceの手順で精査し、発見した問題を修正済み。改善を適用した結果、または変更不要・候補不採用の具体的根拠を確認できる。入れ子型は対応する個別項目で検証する。",
      "verification": "当該型のソース全体、利用箇所、契約、関連テストを照合する。必要な再現・境界・回帰検証と性能比較を行い、C-Correctness/C-Performanceの証拠を当該IDに対応付ける。"
    },
    {
      "id": "T-NonType",
      "description": "型に属さないコード、条件付き・無効化コード、コメント内の保存実装、partial宣言の結合、属性・using・トップレベル処理を全ソースファイルで確認する",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "型別項目で扱っていないコードも全件精査済み。追加の型は必須項目に分割し、型別項目との対応がある。コメント内の旧実装も精査し、実行対象への復帰を前提にせず修正の妥当性を確認できる。",
      "verification": "T-Inventoryの台帳と各ファイル全文を突合し、型別項目の隙間がないことを確認する。対象は固定件数に限定せず、作業中に追加・変更されたコードも台帳と必須項目へ反映する。"
    },
    {
      "id": "T-Template",
      "description": "Arc.Collections/HotMethod/PrimitiveMethod.ttの全生成ロジックを精査し、PrimitiveMethod.csの各型と整合させる",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance"
      ],
      "acceptance": "テンプレート全体と全生成型の対応が確認でき、修正が再生成で失われない。型別精査と同じ正確性・安全性・性能条件を満たす。",
      "verification": "T4の生成条件・型一覧・テンプレート内コードと生成済みC#を照合する。生成方法を確認し、再生成可能なら差分を検証する。ツール不足を検証成功として扱わず、必要な未確認事項を解消する。"
    },
    {
      "id": "T-Configuration",
      "description": "プロジェクト・ソリューション・ビルド設定・CIと依存関係を精査する",
      "required": true,
      "depends_on": [
        "T-Inventory"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Validation"
      ],
      "acceptance": "4プロジェクト、Directory.Build.props、global.json、現行と旧ソリューション、.github/workflows、解析設定のコード動作への影響を確認済み。依存関係に関する該当する既知の脆弱性を調査し、発見した問題を解消する。",
      "verification": "csprojのCompile/除外/生成/unsafe/AOT設定、README.mdとCIのコマンド、テスト検出・実行方式を照合する。実行環境で利用できるNuGet監査手順を確認し、直接・推移依存を調査する。取得不能や未実行は合格としない。公開ワークフローは内容を確認し、実行による公開は検証に使わない。"
    },
    {
      "id": "T-FinalValidation",
      "description": "全修正を統合してビルド・全テスト・必要な性能比較を実施する",
      "required": true,
      "depends_on": [
        "T-Arc-001",
        "T-Arc-002",
        "T-Arc-003",
        "T-Arc-004",
        "T-Arc-005",
        "T-Arc-006",
        "T-Arc-007",
        "T-Arc-008",
        "T-Arc-009",
        "T-Collection-001",
        "T-Collection-002",
        "T-Collection-003",
        "T-Collection-004",
        "T-Collection-005",
        "T-Collection-006",
        "T-Collection-007",
        "T-Collection-008",
        "T-Collection-009",
        "T-Collection-010",
        "T-Collection-011",
        "T-Collection-012",
        "T-Collection-013",
        "T-Collection-014",
        "T-Collection-015",
        "T-Collection-016",
        "T-Collection-017",
        "T-Collection-018",
        "T-Collection-019",
        "T-Collection-020",
        "T-Collection-021",
        "T-Collection-022",
        "T-Collection-023",
        "T-Collection-024",
        "T-Collection-025",
        "T-Collection-026",
        "T-Collection-027",
        "T-Collection-028",
        "T-Collection-029",
        "T-Collection-030",
        "T-Collection-031",
        "T-Collection-032",
        "T-Collection-033",
        "T-Collection-034",
        "T-Collection-035",
        "T-Collection-036",
        "T-Collection-037",
        "T-Collection-038",
        "T-Collection-039",
        "T-Collection-040",
        "T-Collection-041",
        "T-Collection-042",
        "T-Collection-043",
        "T-Collection-044",
        "T-Collection-045",
        "T-Collection-046",
        "T-Collection-047",
        "T-Collection-048",
        "T-Collection-049",
        "T-Collection-050",
        "T-Collection-051",
        "T-Collection-052",
        "T-Collection-053",
        "T-Collection-054",
        "T-Collection-055",
        "T-Collection-056",
        "T-Collection-057",
        "T-Collection-058",
        "T-Collection-059",
        "T-Collection-060",
        "T-Collection-061",
        "T-Collection-062",
        "T-Collection-063",
        "T-Collection-064",
        "T-Collection-065",
        "T-Collection-066",
        "T-Hashtable-001",
        "T-Hashtable-002",
        "T-Hashtable-003",
        "T-Hashtable-004",
        "T-Hashtable-005",
        "T-Hashtable-006",
        "T-Hashtable-007",
        "T-Hashtable-008",
        "T-Hashtable-009",
        "T-Hashtable-010",
        "T-Hashtable-011",
        "T-Hashtable-012",
        "T-Hashtable-013",
        "T-Hashtable-014",
        "T-Hashtable-015",
        "T-Hashtable-016",
        "T-Hashtable-017",
        "T-Hashtable-018",
        "T-Hashtable-019",
        "T-HotMethod-001",
        "T-HotMethod-002",
        "T-HotMethod-003",
        "T-HotMethod-004",
        "T-HotMethod-005",
        "T-HotMethod-006",
        "T-HotMethod-007",
        "T-HotMethod-008",
        "T-HotMethod-009",
        "T-HotMethod-010",
        "T-HotMethod-011",
        "T-HotMethod-012",
        "T-HotMethod-013",
        "T-HotMethod-014",
        "T-HotMethod-015",
        "T-HotMethod-016",
        "T-HotMethod-017",
        "T-HotMethod-018",
        "T-HotMethod-019",
        "T-HotMethod-020",
        "T-HotMethod-021",
        "T-HotMethod-022",
        "T-HotMethod-023",
        "T-HotMethod-024",
        "T-HotMethod-025",
        "T-HotMethod-026",
        "T-HotMethod-027",
        "T-HotMethod-028",
        "T-HotMethod-029",
        "T-HotMethod-030",
        "T-HotMethod-031",
        "T-HotMethod-032",
        "T-HotMethod-033",
        "T-HotMethod-034",
        "T-HotMethod-035",
        "T-Misc-001",
        "T-Misc-002",
        "T-Misc-003",
        "T-Misc-004",
        "T-Misc-005",
        "T-Misc-006",
        "T-Misc-007",
        "T-Misc-008",
        "T-Misc-009",
        "T-Misc-010",
        "T-Misc-011",
        "T-Misc-012",
        "T-Misc-013",
        "T-Misc-014",
        "T-Misc-015",
        "T-Misc-016",
        "T-Misc-017",
        "T-Misc-018",
        "T-Tests-001",
        "T-Tests-002",
        "T-Tests-003",
        "T-Tests-004",
        "T-Tests-005",
        "T-Tests-006",
        "T-Tests-007",
        "T-Tests-008",
        "T-Tests-009",
        "T-Tests-010",
        "T-Tests-011",
        "T-Tests-012",
        "T-Tests-013",
        "T-Tests-014",
        "T-Tests-015",
        "T-Tests-016",
        "T-Tests-017",
        "T-Tests-018",
        "T-Tests-019",
        "T-Tests-020",
        "T-Tests-021",
        "T-Tests-022",
        "T-Tests-023",
        "T-Tests-024",
        "T-Tests-025",
        "T-Tests-026",
        "T-Tests-027",
        "T-Tests-028",
        "T-Tests-029",
        "T-Tests-030",
        "T-Tests-031",
        "T-Tests-032",
        "T-Tests-033",
        "T-Tests-034",
        "T-Tests-035",
        "T-Tests-036",
        "T-Tests-037",
        "T-Tests-038",
        "T-Tests-039",
        "T-Tests-040",
        "T-Tests-041",
        "T-Tests-042",
        "T-Tests-043",
        "T-Tests-044",
        "T-Tests-045",
        "T-Tests-046",
        "T-Tests-047",
        "T-Tests-048",
        "T-Tests-049",
        "T-Tests-050",
        "T-Tests-051",
        "T-Tests-052",
        "T-Tests-053",
        "T-Tests-054",
        "T-Tests-055",
        "T-Tests-056",
        "T-Tests-057",
        "T-Tests-058",
        "T-Tests-059",
        "T-Tests-060",
        "T-Tests-061",
        "T-Tests-062",
        "T-Tests-063",
        "T-Tests-064",
        "T-Tests-065",
        "T-Tests-066",
        "T-Tests-067",
        "T-Tests-068",
        "T-Tests-069",
        "T-Tests-070",
        "T-Tests-071",
        "T-Tests-072",
        "T-Tests-073",
        "T-Tests-074",
        "T-Tests-075",
        "T-Bench-001",
        "T-Bench-002",
        "T-Bench-003",
        "T-Bench-004",
        "T-Bench-005",
        "T-Bench-006",
        "T-Bench-007",
        "T-Bench-008",
        "T-Bench-009",
        "T-Bench-010",
        "T-Bench-011",
        "T-Bench-012",
        "T-Bench-013",
        "T-Bench-014",
        "T-Bench-015",
        "T-Bench-016",
        "T-Bench-017",
        "T-Bench-018",
        "T-Bench-019",
        "T-Bench-020",
        "T-Bench-021",
        "T-Bench-022",
        "T-Bench-023",
        "T-Bench-024",
        "T-Bench-025",
        "T-Bench-026",
        "T-Bench-027",
        "T-Bench-028",
        "T-Bench-029",
        "T-Bench-030",
        "T-Bench-031",
        "T-Bench-032",
        "T-Bench-033",
        "T-Bench-034",
        "T-Bench-035",
        "T-Bench-036",
        "T-Bench-037",
        "T-Bench-038",
        "T-Bench-039",
        "T-Bench-040",
        "T-Bench-041",
        "T-Bench-042",
        "T-Bench-043",
        "T-Bench-044",
        "T-Bench-045",
        "T-Bench-046",
        "T-Bench-047",
        "T-Bench-048",
        "T-Bench-049",
        "T-Bench-050",
        "T-Bench-051",
        "T-Bench-052",
        "T-Bench-053",
        "T-Bench-054",
        "T-Bench-055",
        "T-Bench-056",
        "T-Bench-057",
        "T-Design-001",
        "T-Design-002",
        "T-Design-003",
        "T-Design-004",
        "T-Design-005",
        "T-Design-006",
        "T-Design-007",
        "T-Design-008",
        "T-Design-009",
        "T-Obsolete-001",
        "T-Obsolete-002",
        "T-Obsolete-003",
        "T-Obsolete-004",
        "T-Obsolete-005",
        "T-Obsolete-006",
        "T-Obsolete-007",
        "T-Obsolete-008",
        "T-Obsolete-009",
        "T-Obsolete-010",
        "T-Obsolete-011",
        "T-Obsolete-012",
        "T-Obsolete-013",
        "T-Obsolete-014",
        "T-Obsolete-015",
        "T-Obsolete-016",
        "T-Obsolete-017",
        "T-Obsolete-018",
        "T-Obsolete-019",
        "T-Obsolete-020",
        "T-Obsolete-021",
        "T-Obsolete-022",
        "T-Obsolete-023",
        "T-Obsolete-024",
        "T-Obsolete-025",
        "T-Obsolete-026",
        "T-Obsolete-027",
        "T-Obsolete-028",
        "T-Obsolete-029",
        "T-Obsolete-030",
        "T-Obsolete-031",
        "T-Obsolete-032",
        "T-BenchHost-001",
        "T-BenchHost-002",
        "T-BenchHost-003",
        "T-Sandbox-001",
        "T-NonType",
        "T-Template",
        "T-Configuration"
      ],
      "criterion_ids": [
        "C-Correctness",
        "C-Performance",
        "C-Validation"
      ],
      "acceptance": "C-Validationが成立し、採用した各性能改善は最終ソースでC-Performanceを満たす。各修正の再現条件と検証結果が結び付いている。",
      "verification": "README.md記載のrestore、Releaseビルド、全xUnitテストを実施する。変更の影響に応じてDebug、並行性、境界値、AOT互換性の検証を追加する。採用した改善に対応するBenchmarkDotNet測定を同一条件で照合する。"
    },
    {
      "id": "T-CompletionAudit",
      "description": "最終ソース台帳と全型別項目を照合し、全コードの精査完了を監査する",
      "required": true,
      "depends_on": [
        "T-FinalValidation"
      ],
      "criterion_ids": [
        "C-Coverage",
        "C-Correctness",
        "C-Performance",
        "C-Validation",
        "C-Completion"
      ],
      "acceptance": "全完成条件が最終入力に対する有効な証拠で成立し、精査漏れ、未修正の不具合・脆弱性、未評価の性能候補、未検証変更が0件。全マイルストーンの到達条件を確認できる。",
      "verification": "最終ファイル一覧・型一覧を再取得し、初期台帳との差分、partial・入れ子・レコード・生成元・非型コードを全必須項目へ突合する。全指摘の解消、各型の性能判断、最終Work後の検証結果を照合し、件数やビルド成功だけで完了にしない。"
    }
  ],
  "milestones": [
    {
      "id": "M-Inventory",
      "description": "精査対象と検証基準の確定",
      "task_ids": [
        "T-Inventory"
      ],
      "acceptance": "対象台帳と全必須項目の対応、実行環境、既存の検証結果または実行阻害要因を確認できる。"
    },
    {
      "id": "M-Arc",
      "description": "基礎ユーティリティの全型精査・修正",
      "task_ids": [
        "T-Arc-001",
        "T-Arc-002",
        "T-Arc-003",
        "T-Arc-004",
        "T-Arc-005",
        "T-Arc-006",
        "T-Arc-007",
        "T-Arc-008",
        "T-Arc-009"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Collection",
      "description": "コレクションの全型精査・修正",
      "task_ids": [
        "T-Collection-001",
        "T-Collection-002",
        "T-Collection-003",
        "T-Collection-004",
        "T-Collection-005",
        "T-Collection-006",
        "T-Collection-007",
        "T-Collection-008",
        "T-Collection-009",
        "T-Collection-010",
        "T-Collection-011",
        "T-Collection-012",
        "T-Collection-013",
        "T-Collection-014",
        "T-Collection-015",
        "T-Collection-016",
        "T-Collection-017",
        "T-Collection-018",
        "T-Collection-019",
        "T-Collection-020",
        "T-Collection-021",
        "T-Collection-022",
        "T-Collection-023",
        "T-Collection-024",
        "T-Collection-025",
        "T-Collection-026",
        "T-Collection-027",
        "T-Collection-028",
        "T-Collection-029",
        "T-Collection-030",
        "T-Collection-031",
        "T-Collection-032",
        "T-Collection-033",
        "T-Collection-034",
        "T-Collection-035",
        "T-Collection-036",
        "T-Collection-037",
        "T-Collection-038",
        "T-Collection-039",
        "T-Collection-040",
        "T-Collection-041",
        "T-Collection-042",
        "T-Collection-043",
        "T-Collection-044",
        "T-Collection-045",
        "T-Collection-046",
        "T-Collection-047",
        "T-Collection-048",
        "T-Collection-049",
        "T-Collection-050",
        "T-Collection-051",
        "T-Collection-052",
        "T-Collection-053",
        "T-Collection-054",
        "T-Collection-055",
        "T-Collection-056",
        "T-Collection-057",
        "T-Collection-058",
        "T-Collection-059",
        "T-Collection-060",
        "T-Collection-061",
        "T-Collection-062",
        "T-Collection-063",
        "T-Collection-064",
        "T-Collection-065",
        "T-Collection-066"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Hashtable",
      "description": "ハッシュテーブルと文字列マップの全型精査・修正",
      "task_ids": [
        "T-Hashtable-001",
        "T-Hashtable-002",
        "T-Hashtable-003",
        "T-Hashtable-004",
        "T-Hashtable-005",
        "T-Hashtable-006",
        "T-Hashtable-007",
        "T-Hashtable-008",
        "T-Hashtable-009",
        "T-Hashtable-010",
        "T-Hashtable-011",
        "T-Hashtable-012",
        "T-Hashtable-013",
        "T-Hashtable-014",
        "T-Hashtable-015",
        "T-Hashtable-016",
        "T-Hashtable-017",
        "T-Hashtable-018",
        "T-Hashtable-019"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-HotMethod",
      "description": "型別高速処理の全型精査・修正",
      "task_ids": [
        "T-HotMethod-001",
        "T-HotMethod-002",
        "T-HotMethod-003",
        "T-HotMethod-004",
        "T-HotMethod-005",
        "T-HotMethod-006",
        "T-HotMethod-007",
        "T-HotMethod-008",
        "T-HotMethod-009",
        "T-HotMethod-010",
        "T-HotMethod-011",
        "T-HotMethod-012",
        "T-HotMethod-013",
        "T-HotMethod-014",
        "T-HotMethod-015",
        "T-HotMethod-016",
        "T-HotMethod-017",
        "T-HotMethod-018",
        "T-HotMethod-019",
        "T-HotMethod-020",
        "T-HotMethod-021",
        "T-HotMethod-022",
        "T-HotMethod-023",
        "T-HotMethod-024",
        "T-HotMethod-025",
        "T-HotMethod-026",
        "T-HotMethod-027",
        "T-HotMethod-028",
        "T-HotMethod-029",
        "T-HotMethod-030",
        "T-HotMethod-031",
        "T-HotMethod-032",
        "T-HotMethod-033",
        "T-HotMethod-034",
        "T-HotMethod-035"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Misc",
      "description": "プール・キャッシュ・バッファ等の全型精査・修正",
      "task_ids": [
        "T-Misc-001",
        "T-Misc-002",
        "T-Misc-003",
        "T-Misc-004",
        "T-Misc-005",
        "T-Misc-006",
        "T-Misc-007",
        "T-Misc-008",
        "T-Misc-009",
        "T-Misc-010",
        "T-Misc-011",
        "T-Misc-012",
        "T-Misc-013",
        "T-Misc-014",
        "T-Misc-015",
        "T-Misc-016",
        "T-Misc-017",
        "T-Misc-018"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Tests",
      "description": "テストとテスト補助の全型精査・修正",
      "task_ids": [
        "T-Tests-001",
        "T-Tests-002",
        "T-Tests-003",
        "T-Tests-004",
        "T-Tests-005",
        "T-Tests-006",
        "T-Tests-007",
        "T-Tests-008",
        "T-Tests-009",
        "T-Tests-010",
        "T-Tests-011",
        "T-Tests-012",
        "T-Tests-013",
        "T-Tests-014",
        "T-Tests-015",
        "T-Tests-016",
        "T-Tests-017",
        "T-Tests-018",
        "T-Tests-019",
        "T-Tests-020",
        "T-Tests-021",
        "T-Tests-022",
        "T-Tests-023",
        "T-Tests-024",
        "T-Tests-025",
        "T-Tests-026",
        "T-Tests-027",
        "T-Tests-028",
        "T-Tests-029",
        "T-Tests-030",
        "T-Tests-031",
        "T-Tests-032",
        "T-Tests-033",
        "T-Tests-034",
        "T-Tests-035",
        "T-Tests-036",
        "T-Tests-037",
        "T-Tests-038",
        "T-Tests-039",
        "T-Tests-040",
        "T-Tests-041",
        "T-Tests-042",
        "T-Tests-043",
        "T-Tests-044",
        "T-Tests-045",
        "T-Tests-046",
        "T-Tests-047",
        "T-Tests-048",
        "T-Tests-049",
        "T-Tests-050",
        "T-Tests-051",
        "T-Tests-052",
        "T-Tests-053",
        "T-Tests-054",
        "T-Tests-055",
        "T-Tests-056",
        "T-Tests-057",
        "T-Tests-058",
        "T-Tests-059",
        "T-Tests-060",
        "T-Tests-061",
        "T-Tests-062",
        "T-Tests-063",
        "T-Tests-064",
        "T-Tests-065",
        "T-Tests-066",
        "T-Tests-067",
        "T-Tests-068",
        "T-Tests-069",
        "T-Tests-070",
        "T-Tests-071",
        "T-Tests-072",
        "T-Tests-073",
        "T-Tests-074",
        "T-Tests-075"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Bench",
      "description": "ベンチマークの全型精査・修正",
      "task_ids": [
        "T-Bench-001",
        "T-Bench-002",
        "T-Bench-003",
        "T-Bench-004",
        "T-Bench-005",
        "T-Bench-006",
        "T-Bench-007",
        "T-Bench-008",
        "T-Bench-009",
        "T-Bench-010",
        "T-Bench-011",
        "T-Bench-012",
        "T-Bench-013",
        "T-Bench-014",
        "T-Bench-015",
        "T-Bench-016",
        "T-Bench-017",
        "T-Bench-018",
        "T-Bench-019",
        "T-Bench-020",
        "T-Bench-021",
        "T-Bench-022",
        "T-Bench-023",
        "T-Bench-024",
        "T-Bench-025",
        "T-Bench-026",
        "T-Bench-027",
        "T-Bench-028",
        "T-Bench-029",
        "T-Bench-030",
        "T-Bench-031",
        "T-Bench-032",
        "T-Bench-033",
        "T-Bench-034",
        "T-Bench-035",
        "T-Bench-036",
        "T-Bench-037",
        "T-Bench-038",
        "T-Bench-039",
        "T-Bench-040",
        "T-Bench-041",
        "T-Bench-042",
        "T-Bench-043",
        "T-Bench-044",
        "T-Bench-045",
        "T-Bench-046",
        "T-Bench-047",
        "T-Bench-048",
        "T-Bench-049",
        "T-Bench-050",
        "T-Bench-051",
        "T-Bench-052",
        "T-Bench-053",
        "T-Bench-054",
        "T-Bench-055",
        "T-Bench-056",
        "T-Bench-057"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Design",
      "description": "比較用設計実装の全型精査・修正",
      "task_ids": [
        "T-Design-001",
        "T-Design-002",
        "T-Design-003",
        "T-Design-004",
        "T-Design-005",
        "T-Design-006",
        "T-Design-007",
        "T-Design-008",
        "T-Design-009"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Obsolete",
      "description": "旧実装の全型精査・修正",
      "task_ids": [
        "T-Obsolete-001",
        "T-Obsolete-002",
        "T-Obsolete-003",
        "T-Obsolete-004",
        "T-Obsolete-005",
        "T-Obsolete-006",
        "T-Obsolete-007",
        "T-Obsolete-008",
        "T-Obsolete-009",
        "T-Obsolete-010",
        "T-Obsolete-011",
        "T-Obsolete-012",
        "T-Obsolete-013",
        "T-Obsolete-014",
        "T-Obsolete-015",
        "T-Obsolete-016",
        "T-Obsolete-017",
        "T-Obsolete-018",
        "T-Obsolete-019",
        "T-Obsolete-020",
        "T-Obsolete-021",
        "T-Obsolete-022",
        "T-Obsolete-023",
        "T-Obsolete-024",
        "T-Obsolete-025",
        "T-Obsolete-026",
        "T-Obsolete-027",
        "T-Obsolete-028",
        "T-Obsolete-029",
        "T-Obsolete-030",
        "T-Obsolete-031",
        "T-Obsolete-032"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-BenchHost",
      "description": "ベンチマーク起動・補助の全型精査・修正",
      "task_ids": [
        "T-BenchHost-001",
        "T-BenchHost-002",
        "T-BenchHost-003"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Sandbox",
      "description": "Sandboxの全型精査・修正",
      "task_ids": [
        "T-Sandbox-001"
      ],
      "acceptance": "対象項目の全型について、全メンバーの精査、不具合・脆弱性の解消、性能改善の検討と採否の根拠を確認できる。修正箇所の検証が成功し、未検証の指摘が残っていない。"
    },
    {
      "id": "M-Support",
      "description": "型以外のコード・生成元・設定の精査",
      "task_ids": [
        "T-NonType",
        "T-Template",
        "T-Configuration"
      ],
      "acceptance": "全ソースの残余部分、生成元、設定・CI・依存関係が確認済みで、対応する未解決問題がない。"
    },
    {
      "id": "M-Complete",
      "description": "全体検証と全コード精査完了",
      "task_ids": [
        "T-FinalValidation",
        "T-CompletionAudit"
      ],
      "acceptance": "全完成条件が確認済みで、全コードの精査・必要な修正・有効な性能改善と最終検証が完了している。"
    }
  ]
}
```
<!-- autoframe:end -->
