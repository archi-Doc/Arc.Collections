# 検証記録

対象: autoframe仕様0.1のWindows実装。2026-09-14、PowerShell 7.6.6、Codex CLI 0.154.0。

## 生成物5種類の固定既定値とREADME簡素化（最新）

2026-09-15、`.vs`に加え`bin`・`obj`・`TestResults`・`BenchmarkDotNet.Artifacts`を全プロジェクト・全階層の固定既定値にした。未指定・空配列のPLAN、全段階での生成物更新、完成後のResume、指示ファイル・配布物の保護、必要成果物のハッシュ保持、追加生成先との和集合を確認した。

日本語READMEを248行から77行へ整理し、英語版と配布用の本文を同期した。引数・診断・回復の詳細はSPECに保持し、古い除外候補の説明を固定既定値へ更新した。バージョンは変更していない。

途中でユーザーからテスト省略の指定があったため、追加実行は行わなかった。既に開始していた関連13件は停止確認時点で終了コード0となっていた（defaults 2件、diagnostics 5件、保護変更・範囲／リンク・生成物の完成証拠・テンプレート・同一PLAN再作成・正常6段階が各1件）。全件テストと実CLI試験は今回実行していない。

## .vsを固定既定値へ変更

2026-09-15、IDE検出による宣言方式を廃止し、`**/.vs/**`をRunnerの固定既定値とした。PLANの省略・空配列、起動後の生成、全6段階での更新、IDE更新だけがあった完成後のResumeを確認し、既存PLANのバイト列を保持した。テンプレートにも既定値を明記した。

固定した最終コピーで関連11件が成功（defaults 2件、diagnostics 5件、範囲・リンク拒否、生成物の完成証拠、テンプレート、同一PLAN再作成が各1件）。指示ファイル・共通配布物・共有設定を保護し、必要成果物のハッシュを保持することも確認した。全PowerShell構文、日英README／SPECの対応、git diff --checkを確認。バージョン変更と実行先への配布は行っていない。

## IDEキャッシュ・依存確認・Worker経過時間

2026-09-15、固定した実装コピーで疑似CLIによる全88件の回帰テストが成功した。最後に追加した保護領域内のIDEキャッシュを除外候補にしない条件は、最終コピーで追加6件を再実行して確認した。最終コピーと作業フォルダーのPowerShell・C#・JSONファイルの一致、全PowerShell構文、日英README／SPECの対応、git diff --checkも確認した。バージョンは変更していない。

- PLAN作成時の生成先宣言、既存PLANの`.vs`宣言漏れの案内、許可されたキャッシュ更新、生成物内の指示ファイル保護を確認。
- 保護対象の追加・更新・削除と共通配布物を診断JSONに保存し、変更元をWorkerと断定しないことを確認。Arc.Collectionsの停止時の保存済みマニフェストでも、`.vs`内5件だけを検出し、修正済みgenerated_scopeでは違反にならないことを確認。
- 依存確認の設定アクセス拒否・SDK不在・成功・報告欠落と、影響する項目だけのblocked化を確認。実際のdotnetでも、この実行環境からユーザーNuGet.Configへのアクセス拒否を検出し、対象パス・終了コード1・解除条件を報告できた。ACL、設定、取得元は変更していない。実Codex Workerによる新プロンプトの試験とrestoreは実行していない。
- 各Workerのelapsedが0から始まり、累計予算と独立することを確認。64秒待機する既存テストで、1分ごとの表示と20秒の状態保存、Worker結果の受理を確認。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'diagnostics:*'
```

## 状況表示を1分間隔へ変更

2026-09-14、状況表示の時計を状態保存から分離し、定期表示は約60秒、再開用の状態保存は約20秒とした。開始・終了・段階切替の表示は即時のまま。日英README／SPECに指定の.gitignore例も掲載した。

`-Filter 'heartbeat saves*'`と`-Filter 'successful six phases*'`の2件が成功。疑似Workerを64秒待機させ、22秒時点で状態が保存済みであること、状況行の時刻差が60秒以上であること、終了結果を受理できることを確認した。構文、共通QuickStart、Git除外規則、git diff --checkも合格。実CLIは再実行していない。

## 仕様・実装の整合性監査

2026-09-14、固定した実装コピーで全82件の回帰テストが成功した。検証後、PowerShell・C#・Schema全17ファイルが作業フォルダーと一致することを確認した。最終プロンプトで6段階の正常完了も再確認した。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -KeepFixtures
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'successful six phases*'
pwsh -NoProfile -File ./autoframe/tests/smoke-real-cli.ps1 -RunRealCli
```

仕様は、通常の内部計画更新と定期再作成、内部計画版の更新条件、実行開始前の検査と開始後のCLI確認、設定変更による証拠失効、停止処理の待機上限を明確化した。重複する遷移図・実行例、不完全な結果JSON例、検証状況の重複記載を削り、日英README／SPECを対応させた。

修正と確認結果:

- Planのready／recoverで、マイルストーン未設定・空の対象・必須項目の対応漏れを拒否する。既存PLANの読込互換性、置換先の対応、修正後のResumeを確認。
- 原文のtextブロック内にMarkdown見出しがあっても全文を取得する。日英の原文見出しと、他の節のtextを誤取得しないことを確認。
- 非同期ログ書込の失敗をWorker実行中のFlushで伝播する。stdout／stderr双方の失敗を注入して確認。子停止は既存のタイムアウト・キャンセル・残留プロセス試験でも確認。
- Worker終了を結果検査前に表示する。不正JSONでも終了イベントが残り、正常実行では起動ごとに1回だけ表示することを確認。

全体試験では、6段階の完成、部分成功、依存証拠の失効、マイルストーン、停滞、予算、Resume／NewRun、強制終了からの回復、PLAN再作成・反映途中の回復・競合拒否を確認した。日英の節番号、共通QuickStart、PLAN例3件、ローカルリンク49件、PowerShell全11ファイルの構文とgit diff --checkも合格。

実CLI疎通は通常のユーザー環境で成功し、0.154.0の構造化出力と子停止を確認した。制限環境ではホームディレクトリを取得できず失敗したため、結果を区別する。実モデルによる6段階の通し完了・原文の解釈品質は今回も未検証。Kimigayo側の配布物・実行記録は変更していない。

今回の記録（この環境の一時ディレクトリ）:

- 固定実装: `%TEMP%/autoframe-audit-0c6545d7cbd94dc1aee2dd2fa52e6922/autoframe/`
- 全82件の試行記録: `%TEMP%/autoframe-tests-24f00802cc254052ade93682a4aa4aa4/`
- 実CLI成功: `%TEMP%/autoframe-cli-smoke-7c5253e45cb3418c93fe3bff3a1e85be/verification.json`
- 制限環境での実CLI失敗: `%TEMP%/autoframe-cli-smoke-07177d651a7a468d988fa64e6e3a7009/verification.json`

以下は変更ごとの過去の検証記録。

## 原文からの定期再作成の検証

全75件の回帰テストが成功した。その後、新規項目の追加を許容する検査を調整し、追加1件を含む再作成関連8件を再実行してすべて成功した。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'regeneration:*'
```

- 既定のPlan 3・6・9回目での発火、原文保持、設定変更・Resumeでの継承、NewRunでの初期化。
- 原文欠落時のNeedsInput、不正候補の拒否と元ファイル保持、次のPlanでの再試行。
- ファイル置換前・置換後の中断回復、重複反映の防止、外部編集との競合時の上書き防止。
- 未検証Workと成果物を保持してVerifyへ引き継ぎ、Workを重複実行せずCompleteへ到達。
- 同一候補でファイルのバイト列を保持、新規項目・初期項目なしの計画を受理、既存必須条件の弱体化を拒否。
- 旧状態の回数復元で未起動の入力を除外し、再作成だけの反復でも停滞判定が働くこと。

既存試験では間隔1000を明示して各機能を分離し、専用試験では既定値3と指定値1・2を検証した。PowerShell全11ファイルの構文、仕様例・テンプレートのSchema、日英の節番号、ローカルリンク32件も確認した。実モデルによる原文解釈・再作成品質は未検証であり、Runnerの受理・保存・回復は疑似Workerで検証した。

## 疑似Worker（前回まで）

既存56件に回帰9件を追加し、計65件を確認した。修正後の全体63件と、最終マイルストーン修正後の追加2件はすべて成功。PowerShell構文検査、仕様のJSON例・PLAN Schema検査、日英の節番号照合、git diff --checkも成功。

配置先をautoframe/へ変更後、6段階の正常完了テストを再実行して成功した。ローカルリンクと見出し参照24件、日英のPLAN例、PowerShell構文も確認した。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'spec: milestone*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'successful six phases*'
```

| 仕様 | 主な確認内容 |
| --- | --- |
| §2 入力 | 厳密なJSON・Schema、ID・依存・参照、初期項目なしの計画生成、ユーザー条件の保持 |
| §3 記録 | 排他、原子的な受理、保存失敗、証拠破損の回復、進捗と計画版の分離 |
| §4 遷移 | 6段階の完了、読取専用作業、3件一括・部分成功、必須指摘、節目の監査、重複抑止 |
| §5 証拠・回復 | 入力・依存証拠の失効、保護範囲、PLAN変更、未確定成果、NewRun、完成証拠の欠落・破損 |
| §6 停止 | Workなしループ、停滞と明示解除、回復時の進捗判定、実際に渡す共通プロンプト |
| §7 Runner | 引数、設定継承、上限、ハートビート、時間切れ・キャンセル・強制終了、子停止、大量ログ、終了コード |
| §8 受理契約 | 段階別権限、対象IDの網羅、再受理拒否、最終WorkのVerify、完成直前の照合 |

## 今回の修正

| 問題 | 修正・回帰確認 |
| --- | --- |
| Planがユーザー指定の依存関係・criterion_idsを削除できた | 元の対応と依存順序を検査。中間項目を使う正当な詳細化は許容 |
| マイルストーン対象の削除と、内部データの参照共有 | 元の対象を保持し、PLANデータから独立して複製 |
| 分割済み項目のマイルストーンが到達しなかった | 置換先すべての状態と証拠で判定。未完了時の監査と重複抑止を確認 |
| Workなしの部分的な回復でも計画停滞を解除できた | 新たな有効完了・必須指摘解消に限定 |
| ランチャーが非0終了を1に置き換えていた | native・ps1・cmdの元の終了コードを保持 |
| 完成証拠の欠落でErrorとなり、破損時も重複抑止が再監査を妨げた | 受理済みCompletionAuditと証拠を再照合し、必要なら新しい監査へ。上限中はPaused |
| NewRunが受理済み・未検証Workを再実行できた | 回復対象を引き継ぎ、before.jsonに実行前マニフェスト参照を保持 |

## マイルストーン自動作成・進捗表示の追加検証

新規3件と既存5件の計8件が成功した。今回の実行コマンド:

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter '*milestone*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter '*timeout stops process tree*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'successful six phases*'
```

- テンプレートのSchema適合、初期項目・マイルストーン、全必須項目の対応を確認。
- 部分進捗、置換先すべての集計、pendingへの差戻しによる監査候補の取消し、空の既存マイルストーンの扱いを確認。
- 疑似Workerが内部マイルストーンを生成し、未完了の節目を残して監査へ遷移すること、Worker入力と実行ログの件数、PLAN.mdの不変を確認。
- 既存の依存・置換・監査重複抑止、6段階の完了、時間切れでの子プロセス停止を再確認。

PowerShell全10ファイルの構文、日英SPEC内のPLAN例のSchema・参照、変更文書のローカルリンク31件も確認した。実モデルによるPLAN.mdの自動作成品質は今回の試験対象外。作成手順はプロンプトとテンプレートで定義しており、RunnerがPLAN.mdを書き換えるものではない。

## 実CLI

```powershell
pwsh -NoProfile -File ./autoframe/tests/smoke-real-cli.ps1 -RunRealCli
```

通常ユーザーの実行環境で疎通試験が成功した。CLI 0.154.0、既存認証によるモデル応答、stdin入力、構造化JSONの完全一致、子プロセス停止を確認。制限された実行環境ではホームディレクトリ取得に失敗したため、両環境の結果を区別した。

Runnerでも一時プロジェクトの1ファイルを対象に、実CLIで読取専用レビューを開始した。PlanのJSONを1件受理し、約68秒でNeedsInputとして保存・停止した。Workerは読取専用で動作し、試行用の証拠ファイルを保存できなかった。対象ファイルの不変、未完了扱い、running=false、全子プロセスの停止を確認。権限・認証・モデル設定は変更していない。

保存先（この環境の一時ディレクトリ）:

- 疎通成功: `%TEMP%/autoframe-cli-smoke-5ec7e103506d47e3a13ae85cb9e0f86b/verification.json`
- Runnerの停止記録: `%TEMP%/autoframe-real-run-ee85f503b456450a9f89c7630492a0e9/.autoframe/state.json`

疎通成功は、Workerの証拠書込権限や実CLIの6段階完了を意味しない。利用環境で必要な権限を確認したうえで実行する。

## Planの証拠参照エラーの追加検証

2026-09-14、復元された実行結果を読み取り、一時ディレクトリに証拠を保存してApply-Resultを再実行した。元の結果はPlanのpending項目36件からkind=planを参照しており、最初のT01で種類不一致となった。この参照を外すだけでは、Planによるprogress報告2件も拒否された。evidence_idsとprogressを空にしたコピーは検査を通り、定義変更37件、pending項目36件、マイルストーン11件、独立した計画証拠2件を保持した。元の配布物・実行結果・状態は変更していない。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'Plan evidence*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'successful six phases*'
```

新規3件と既存1件が成功。証拠IDの欠落・対象不一致・種類不一致の診断、Planの不正progressの拒否、独立したplan証拠の保存、未完了状態の維持、修正済み疑似WorkerによるResumeと6段階完了を確認した。PowerShell全11ファイルの構文、日英それぞれのREADME/SPECのQuickStart一致、git diff --checkも確認した。実モデルの再実行と配布先への修正反映は未実施。

## 実行状況表示の追加検証

2026-09-14、既存の疑似Worker試験3件を拡張して実行し、すべて成功した。

```powershell
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'successful six phases*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'heartbeat saves*'
pwsh -NoProfile -File ./autoframe/tests/test.ps1 -Filter 'inherited settings*'
```

開始・終了時刻、各段階の起動・終了・受理、検証済み件数、累計起動回数、20秒超のWorker実行中の定期表示、Resumeでの既存回数と設定の表示を確認した。ハートビート後も実行中の結果を受理でき、6段階でCompleteとなった。24時間超と負の残り時間の表示、PowerShell全11ファイルの構文、日英それぞれのREADME/SPECのQuickStart一致、git diff --checkも確認した。実CLIの再実行・Kimigayo側の配布物更新は行っていない。

## 未完了・制限

- 実CLIによる6段階の完了。今回の通し試験はPlanでNeedsInputとなった。実モデルの監査品質・使用量・長時間運転も未検証。
- OSの強制電源断、実キーボードのCtrl+C。プロセス強制終了、キャンセル要求、時間切れ、子残留と停止は疑似試験で確認。
- Windows以外は非対応。リンクは一律拒否し、globの包含・競合判定は保守的に行う。差分検査はOSの権限制御を代替しない。
- NativeAOT、製品作業用PLAN.md、本番の自動実行は対象外。試験用PLAN.mdは一時プロジェクト内に限定。
