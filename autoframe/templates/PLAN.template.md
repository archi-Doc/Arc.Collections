# 実行目的（ユーザーが編集）
未確認のコマンドや条件を確定事項として書かず、実行前に対象に合わせて記入する。進捗・証拠・指摘は記載しない。
作成時は目的と完成条件からtasksとmilestonesを自動生成する。小規模な作業は1マイルストーンでもよい。各マイルストーンに対象項目と確認可能な到達条件を記し、全必須項目をいずれかのマイルストーンに含める。
generated_scopeの下記5パターンは全プロジェクト共通の固定既定値。省略・空配列でもRunnerが適用する。その他の生成先は必要に応じて追加し、設定ファイルを除外しない。依存する長時間作業の前にWorker環境で依存確認・restoreを行い、失敗した項目の原因と解除条件を記録する。

## ユーザープロンプト（原文）

以下をユーザーが入力した原文に置き換える。要約・補完せず、未入力の項目は未入力と記す。

```text
目的: <達成したいこと>
対象: <機能、フォルダー、参照資料>
完成条件: <どの状態になれば終了できるか>
対象外・制約: <変更しないもの、互換性、禁止する操作>
```

## 実行計画

原文を具体化した制御入力。保存前に原文との整合を確認する。

<!-- autoframe:begin -->
```json
{
  "schema_version": 1,
  "project_id": "replace-with-stable-project-id",
  "objective": "達成する目的を記入",
  "scope": ["有限の対象範囲を記入"],
  "non_goals": ["対象外を記入"],
  "constraints": ["既存の無関係な変更を保持する"],
  "work_scope": [],
  "generated_scope": ["**/.vs/**", "**/bin/**", "**/obj/**", "**/TestResults/**", "**/BenchmarkDotNet.Artifacts/**"],
  "environment_checks": [],
  "references": [],
  "completion_criteria": [
    {
      "id": "C1",
      "condition": "完了を判定できる期待結果を記入",
      "verification": "確認手順と合格基準を記入"
    }
  ],
  "tasks": [
    {
      "id": "T1",
      "description": "C1を満たすための作業を記入",
      "required": true,
      "depends_on": [],
      "criterion_ids": ["C1"],
      "acceptance": "C1を満たすこと",
      "verification": "C1の確認手順を実行する"
    }
  ],
  "milestones": [
    {
      "id": "M1",
      "description": "C1の達成",
      "task_ids": ["T1"],
      "acceptance": "T1が検証済みで、C1の期待結果を確認できること"
    }
  ]
}
```
<!-- autoframe:end -->
