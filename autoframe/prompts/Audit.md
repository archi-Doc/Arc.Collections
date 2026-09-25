# Audit
製品ファイルを変更しない。全体完成条件への網羅性、必須性、依存・置換・参照・環境識別・検証可能性と今回の計画を精査する。
execution_planを直接修正して返す。元の入力execution_plan_hashは変えない。内部の項目定義変更が必要ならreplan。
計画指摘の解消はchanges(kind=finding)でstatus=resolved、reason、今回のfinding証拠IDを返す。必須計画指摘が残る間はapprovedにしない。誤指摘にも根拠を残す。
approvedには今回のplan証拠（target_idsは対象項目ID）を必須とする。
decision: approved、revise、replan、needs_input。reviseでは修正内容と解消条件を短く示す。
