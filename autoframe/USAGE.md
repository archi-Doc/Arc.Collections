# autoframe 利用ガイド 0.1

Windows、PowerShell 7.4以降、対応するCodex CLIと既存の認証・権限を使用する。目的・完成条件・作業範囲はプロジェクト直下のPLAN.mdで定義する。[English](USAGE.en.md)

## 配置と実行

このフォルダー一式を対象プロジェクトへ配置するか、共通配置してProjectRootを指定する。更新はRunner停止中に行い、対象プロジェクトのPLAN.mdと`.autoframe/`を保持する。

PLAN作成、実行・再開の例、既定値、停止条件、状況表示は[SPECのQuickStart](SPEC.md#quickstart)を参照する。[テンプレート](templates/PLAN.template.md)と[作成用プロンプト](prompts/create-plan.md)も利用できる。

通常のPlanは内部計画を更新し、既定でPlanの3回目・6回目・9回目…にRunnerがPLAN.mdのtasksとmilestonesを再作成する。原文・要件を保持し、更新後の証拠を再検証する。Resumeは設定・回数・稼働時間を引き継ぎ、NewRunは新しい実行を始める。どちらも未検証成果の確認を省略しない。

## 記録と結果

`.autoframe/state.json`と参照先が現行状態。内部状態は手で編集しない。

| 保存先 | 内容 |
| --- | --- |
| records/ | 受理結果、内部計画、証拠、履歴 |
| manifests/ | 内容ハッシュで共有するファイル記録 |
| runs/<run_id>/<attempt_id>/ | 試行ごとのinput.json、prompt.md、before.json、after.json、metrics.json |
| 同上のworker/ | events.jsonl、stderr.log、プロセス停止記録 |
| 同上のoutput/ | Workerのresult.json、証拠、定期再作成時の候補JSON |

before.jsonの参照は受理後も残し、NewRunの回復でも利用する。状態の確定前に記録を保存し、未参照記録から受理を推測しない。ハートビートは内部計画の版を変更しない。

[result Schema](schemas/result.schema.json)がWorkerの出力契約。証拠のpathはoutput_directory基準、hashはSHA-256、input_signatureは入力signatureを使う。changes.setの変更しない値と不要なblockerはnullとする。必要成果物は内部taskのdeliverablesに列挙する。Planの計画メモと項目証拠の使い分けは[SPEC §8.1](SPEC.md#81-結果の受理)に従う。

Workerは既存のモデル・認証・権限を継承する。少なくともoutput_directoryへ証拠を書ける権限が必要で、Runnerは権限拡張フラグを付けない。CLI疎通成功だけでは、この書込権限や6段階の完了を保証しない。

Windows Job Objectで子プロセスを管理し、他OSは起動前にErrorとする。ファイル差分による事後検査はOSの書込制限や全操作の記録を代替しない。パス・証拠・回復の規則は[SPEC §5](SPEC.md#5-差分証拠再開)、公開引数は[SPEC §7](SPEC.md#7-runps1の仕様)を参照する。

## 検証

```powershell
# 一時プロジェクトと疑似Workerによる基盤試験
pwsh -NoProfile -File ./autoframe/tests/test.ps1

# ツール不使用・読取専用の実CLI疎通試験
pwsh -NoProfile -File ./autoframe/tests/smoke-real-cli.ps1 -RunRealCli
```

疑似試験の失敗時は一時フォルダーを保持する。`-KeepFixtures`は成功時も保持し、`-Filter '*対象名*'`は個別試験を選ぶ。実CLI疎通は別試験であり、製品用PLANや本番の自動実行は不要。結果と未確認事項は[VERIFICATION.md](VERIFICATION.md)に記録する。
