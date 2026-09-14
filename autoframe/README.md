# autoframe 0.1

Codexの作業を計画・監査・実装・検証に分けて進める、Windows向けの自動実行フレームワークです。[English](README.en.md)

目的と完成条件を`PLAN.md`に定義し、`Plan → Prepare → Audit → Work → Verify`を繰り返します。全体監査で完成条件を確認すると`Complete`になります。

## はじめに

### 1. 配置する

Windows、PowerShell 7.4以降、Codex CLIと必要な開発ツールを用意し、`autoframe/`一式を対象プロジェクトへコピーします。認証・権限は既存の設定を使います。

Gitを使う場合は`.gitignore`へ`.autoframe/`を追加してください。実行状態・証拠を保存するフォルダーなので、再開時に削除しないでください。

### 2. PLAN.mdを作る

Codexへ次のように依頼します。[テンプレート][template]も利用できます。

```text
autoframe/prompts/create-plan.mdに従い、プロジェクト直下にPLAN.mdを作成してください。
目的: <達成したいこと>
対象: <機能・フォルダー>
完成条件: <終了を判断できる条件>
対象外・制約: <変更しないもの・禁止する操作>
PLAN.mdだけを作成し、自動実行は開始しないでください。
```

### 3. 実行・再開する

PLAN.mdを確認し、対象プロジェクトのルートで実行します。

```powershell
# 開始
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot .

# 再開
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume

# 累計時間の上限を720分に増やして再開
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume -MaxRunMinutes 720
```

既定値は累計480分・Worker起動100回、1回のWorkerは最大60分です。Resumeは時間・回数・設定を引き継ぎます。新しい実行には`-NewRun`を使い、`-Resume`と併用しません。

Planの起動3回ごとに、保存したユーザープロンプトの原文から作業項目を再作成します。間隔は`-PlanRegenerationInterval`で変更できます。

## ファイルの扱い

次の5種類は、全プロジェクト・全階層で既定の生成物として扱います。PLANで未指定でも有効です。

`.vs/`、`bin/`、`obj/`、`TestResults/`、`BenchmarkDotNet.Artifacts/`

追加の生成先はPLANの`generated_scope`へ記載します。指示ファイルとautoframe本体の保護、必要成果物の検証は維持します。PLANや配布物の更新はRunner停止中に行い、`.autoframe/`の内部状態は手で編集しないでください。

## 状況と停止

約1分ごとに、`elapsed`（現在のWorkerの経過時間）、`total_elapsed`（累計稼働時間）、`remaining`（全体予算の残り時間）を表示します。記録先はログの`Trial`で確認できます。

時間上限・Ctrl+CではWorkerと子プロセスを停止し、通常は`Paused`になります。`Blocked`は外部待ち、`NeedsInput`は判断待ち、`Stalled`は停滞、`Error`は異常です。`Stalled`の解除やエラー対応は[SPEC][spec]を参照してください。

保護対象の変更はパス付きで診断します。.NETではWorker環境で依存設定を確認し、失敗した項目の原因・解除条件を記録します。

## モデルと推論量の設定

モデルはCodex CLIの設定を使います。段階別のモデルIDは`-PhaseModels`で指定できます。Resume時の省略は保存済み設定を継承し、明示した表は全体を置き換えます。

推論量はCLIの`model_reasoning_effort`を使用します。autoframeには段階別の推論量を指定する引数はありません。

## 詳細

[利用ガイド][usage] · [仕様・全引数][spec] · [PLAN作成用プロンプト][create-plan] · [検証記録][verification]

[template]: templates/PLAN.template.md
[usage]: USAGE.md
[spec]: SPEC.md
[create-plan]: prompts/create-plan.md
[verification]: VERIFICATION.md
