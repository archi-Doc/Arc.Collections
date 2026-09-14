# A CLI double, never used by the production default. Fixture configuration is test data only.
$ErrorActionPreference='Stop'
if('--version' -in $args) { 'autoframe-fake 1'; exit 0 }
if('--help' -in $args) { '--json --output-schema --output-last-message --ephemeral --skip-git-repo-check'; exit 0 }
$prompt=@($input) -join "`n"
try {
    $match=[regex]::Match($prompt,'(?m)^入力参照: (.+)$')
    if(-not $match.Success) { throw 'Context reference missing from stdin.' }
    $context=Get-Content -LiteralPath $match.Groups[1].Value.Trim() -Raw | ConvertFrom-Json -AsHashtable
    $logical=Get-Content -LiteralPath $context.logical_path -Raw | ConvertFrom-Json -AsHashtable
    $plan=Get-Content -LiteralPath $context.plan_record -Raw | ConvertFrom-Json -AsHashtable
    $scenario=Get-Content -LiteralPath (Join-Path $context.project_root 'scenario.json') -Raw | ConvertFrom-Json -AsHashtable
    if($scenario.mode -ceq 'default-outputs-all-phases') {
        foreach($name in @('.vs','bin','obj','TestResults','BenchmarkDotNet.Artifacts')) {
            if("**/$name/**" -cnotin $context.default_generated_scope -or "**/$name/**" -cnotin $context.effective_generated_scope) { throw "Default scope missing from Worker input: $name" }
            foreach($relative in @($name,"nested/$name")) {
                $dir=Join-Path $context.project_root $relative
                $null=[IO.Directory]::CreateDirectory($dir)
                [IO.File]::WriteAllText((Join-Path $dir 'cache.json'),$context.phase)
            }
        }
    }
    $result=@{
        schema_version=1;run_id=$context.run_id;attempt_id=$context.attempt_id;phase=$context.phase
        input_plan_hash=$context.input_plan_hash;base_plan_version=$context.base_plan_version;execution_plan_hash=$context.execution_plan_hash
        execution_plan=$null;decision='ready';route_hint=$null;summary='疑似Workerの結果';task_results=@();changes=@();findings=@();evidence=@();progress=@()
    }
    function Add-Proof([string]$Kind,[string[]]$Ids) {
        $id="$($context.attempt_id)-E$($result.evidence.Count)"; $name="$id.txt"
        [IO.File]::WriteAllText((Join-Path $context.output_directory $name),"$Kind / $($Ids -join ','): fixture verified",[Text.UTF8Encoding]::new($false))
        $hash=(Get-FileHash -LiteralPath (Join-Path $context.output_directory $name) -Algorithm SHA256).Hash.ToLowerInvariant()
        $result.evidence+=@(@{id=$id;kind=$Kind;path=$name;hash=$hash;target_ids=@($Ids);input_signature=$context.signature})
        return $id
    }
    function Task-Result([string]$Id,[string]$Status,$Evidence=@()) { @{id=$Id;status=$Status;evidence_ids=@($Evidence);remaining=@();blocker=$null} }
    if($null -ne $context.dependency_preflight -and $scenario.mode -cne 'dependency-missing') {
        $dependency=@{status='ready';exit_code=0;error=$null;release_condition=$null}
        if($scenario.mode -ceq 'dependency-blocked') { $dependency=@{status='blocked';exit_code=1;error='Access denied: C:/Users/fixture/NuGet.Config';release_condition='Restore read access in the Worker environment and rerun restore.'} }
        $dependency | ConvertTo-Json | Set-Content -LiteralPath $context.dependency_preflight.report -Encoding utf8
    }
    if($scenario.mode -eq 'invalid-result') { [IO.File]::WriteAllText((Join-Path $context.output_directory 'result.json'),'{}'); exit 0 }
    if($scenario.mode -eq 'error') { [Console]::Error.WriteLine('intentional failure'); exit 19 }
    if($scenario.mode -eq 'large-log') { for($i=0;$i -lt 5000;$i++) { [Console]::WriteLine(('x'*1024)); [Console]::Error.WriteLine(('y'*1024)) } }
    switch -CaseSensitive ($context.phase) {
        'Plan' {
            if($scenario.mode -ceq 'dependency-blocked') {
                $blocked=Task-Result 'T1' 'blocked'
                $blocked.blocker=@{reason=$dependency.error;release_condition=$dependency.release_condition}
                $result.task_results+=@($blocked)
            }
            if($scenario.mode -like 'plan-proof-*') {
                $ids=switch($scenario.mode) {
                    'plan-proof-missing' { 'unknown-evidence' }
                    'plan-proof-target' { Add-Proof 'progress' @('T2') }
                    'plan-proof-kind' { Add-Proof 'plan' @('T1') }
                    'plan-proof-unlinked' { $null=Add-Proof 'plan' @('T1') }
                    'plan-proof-progress' { $result.progress=@(@{task_id='T1';description='planning note';evidence_ids=@(Add-Proof 'plan' @('T1'))}) }
                }
                $result.task_results+=@(Task-Result 'T1' 'pending' @($ids | Where-Object { $null -ne $_ }))
            }
            if($context.regenerate_plan -and $scenario.mode -like 'regeneration*') {
                if($scenario.mode -eq 'regeneration-needs-input') { $result.decision='needs_input' }
                else {
                    if($scenario.mode -ne 'regeneration-identical') {
                        $plan.tasks[0].description="regenerated on Plan $($context.plan_attempt_number)"
                        $result.changes+=@(@{kind='task';id=$plan.tasks[0].id;set=@{description=$plan.tasks[0].description;required=$null;depends_on=$null;criterion_ids=$null;acceptance=$null;verification=$null;source_ids=$null;deliverables=$null;replacements=$null}})
                    }
                    if($scenario.mode -eq 'regeneration-invalid') { $plan.constraints=@('changed without authorization') }
                    $plan | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $context.output_directory 'regenerated-plan.json') -Encoding utf8
                }
            }
            if($scenario.mode -eq 'auto-milestone' -and -not $logical.milestones.Count) {
                foreach($task in $logical.tasks) {
                    $result.changes+=@(@{kind='milestone';id="M-$($task.id)";set=@{description='generated checkpoint';task_ids=@($task.id);acceptance='fixture verified'}})
                }
            }
            if(-not $logical.tasks.Count) {
                $result.changes+=@(@{kind='task';id='T1';set=@{description='generated fixture task';required=$true;depends_on=@();criterion_ids=@('C1');acceptance='fixture';verification='check fixture';source_ids=@();deliverables=@();replacements=@()}})
            }
            if($scenario.mode -eq 'heartbeat') {
                Start-Sleep -Seconds 22
                $live=Get-Content -LiteralPath (Join-Path $context.project_root '.autoframe/state.json') -Raw | ConvertFrom-Json -AsHashtable
                if($live.base_plan_version -ne $context.base_plan_version -or $live.elapsed_seconds -lt 15) { throw 'Heartbeat changed version or did not persist elapsed time.' }
                Start-Sleep -Seconds 42
            }
            if($scenario.mode -in @('finding-open','finding-resolved') -and -not $logical.findings.Count) {
                $result.findings=@(@{id='F1';kind='product';required=$true;content='fixture issue';resolution='check result';task_ids=@('T1');status='open';evidence_refs=@()})
            }
            foreach($t in $logical.tasks) {
                if(-not $t.deliverables.Count -and $plan.work_scope.Count) {
                    $set=@{description=$null;required=$null;depends_on=$null;criterion_ids=$null;acceptance=$null;verification=$null;source_ids=$null;deliverables=@("out/$($t.id).txt");replacements=$null}
                    $result.changes+=@(@{kind='task';id=$t.id;set=$set})
                }
            }
            if($scenario.mode -eq 'workless') { $result.decision='replan' }
            elseif($scenario.mode -cne 'plan-proof-recover' -and $result.decision -cne 'needs_input' -and ($context.pending_work -or $context.uncertain.Count -or @($logical.tasks | Where-Object { $_.status -eq 'implemented' -or $_.remaining.Count }).Count)) {
                $result.decision='recover'
                foreach($t in $logical.tasks | Where-Object { $_.status -in @('pending','implemented') }) { $result.task_results+=@(Task-Result $t.id 'pending') }
                if($scenario.mode -eq 'recover-subset') { $result.task_results=@($result.task_results | Select-Object -First 1) }
                if($scenario.mode -eq 'skip-pending-verify') { $result.decision='ready' }
            }
            if($scenario.mode -cne 'missing-milestone') {
                # Fulfill the same mandatory Plan coverage contract as a real Worker.
                $milestones=@($logical.milestones)+@($result.changes | Where-Object kind -CEQ milestone | ForEach-Object { $_.set })
                $covered=@($milestones | ForEach-Object { $_.task_ids })
                $required=@($logical.tasks | Where-Object { $_.required -and $_.status -cne 'superseded' } | ForEach-Object { $_.id })
                if(-not $logical.tasks.Count) { $required=@('T1') }
                $missing=@($required | Where-Object { $_ -cnotin $covered })
                if($missing.Count) { $result.changes+=@(@{kind='milestone';id='M-fixture';set=@{description='fixture checkpoint';task_ids=$missing;acceptance='fixture verified'}}) }
            }
        }
        'Prepare' {
            $map=@{}; foreach($t in $logical.tasks) { $map[$t.id]=$t }
            $targets=@($logical.tasks | Where-Object { $_.status -eq 'pending' -and -not @($_.depends_on | Where-Object { $map[$_].status -ne 'verified' }).Count } | Select-Object -First $context.max_tasks)
            if(-not $targets.Count) { $result.decision='completion_candidate' }
            else {
                $result.execution_plan=@{summary='fixture plan';tasks=@(foreach($t in $targets) { @{id=$t.id;edit_scope=@(if($plan.work_scope.Count) { "out/$($t.id).txt" });steps=@('perform fixture operation');verification='check fixture';minutes=1} })}
            }
        }
        'Audit' {
            $result.decision=if($scenario.mode -eq 'audit-loop') { 'revise' } else { 'approved' }
            $result.execution_plan=$logical.execution_plan
            $null=Add-Proof 'plan' $(if($scenario.mode -eq 'audit-uncovered') { @('unknown') } else { $context.targets })
        }
        'Work' {
            $result.decision='reported'; $result.route_hint=if($scenario.mode -eq 'route-replan') { 'replan' } else { 'continue' }
            foreach($id in $context.targets) {
                if($plan.work_scope.Count) {
                    $null=[IO.Directory]::CreateDirectory((Join-Path $context.project_root 'out'))
                    [IO.File]::WriteAllText((Join-Path $context.project_root "out/$id.txt"),'fixture result')
                }
                $result.task_results+=@(Task-Result $id 'implemented' @(Add-Proof 'work' @($id)))
            }
            if($scenario.mode -eq 'work-crash') { exit 9 }
            if($scenario.mode -eq 'work-sleep') { Start-Sleep -Seconds 30 }
            if($scenario.mode -eq 'plan-change') {
                # Simulate an external user edit; production Workers are explicitly forbidden to do this.
                $path=Join-Path $context.project_root 'PLAN.md'
                if(-not [IO.File]::ReadAllText($path).Contains('external fixture edit')) { [IO.File]::AppendAllText($path,"`nexternal fixture edit`n") }
            }
            if($scenario.mode -eq 'protected-write') { [IO.File]::WriteAllText((Join-Path $context.project_root 'protected.txt'),'changed') }
            if($scenario.mode -eq 'ide-cache-write') { [IO.File]::WriteAllText((Join-Path $context.project_root '.vs/DocumentLayout.json'),'changed') }
        }
        'Verify' {
            $result.decision='accepted'
            foreach($id in $context.targets) {
                if($scenario.mode -eq 'unknown-environment') {
                    $result.decision='revise'; $r=Task-Result $id 'blocked'; $r.blocker=@{reason='External dependency cannot be identified';release_condition='Declare an identifying environment check'}
                    $result.task_results+=@($r); continue
                }
                if($scenario.mode -eq 'no-progress' -or ($scenario.mode -eq 'partial' -and $id -eq 'T2')) {
                    $result.decision='revise'; $result.task_results+=@(Task-Result $id 'pending'); continue
                }
                if($plan.work_scope.Count -and [IO.File]::ReadAllText((Join-Path $context.project_root "out/$id.txt")) -ne 'fixture result') { throw 'Fixture artifact does not match.' }
                $result.task_results+=@(Task-Result $id 'verified' @(Add-Proof 'task' @($id)))
                if($scenario.mode -eq 'verified-remaining') { $result.task_results[-1].remaining=@('not finished') }
            }
            if($scenario.mode -eq 'finding-resolved') {
                $result.changes=@(@{kind='finding';id='F1';set=@{task_ids=$null;status='resolved';evidence_refs=@(Add-Proof 'finding' @('F1'));reason='verified fixture'}})
            }
        }
        'CompletionAudit' {
            if($scenario.mode -eq 'audit-mutates-deliverable') { [IO.File]::WriteAllText((Join-Path $context.project_root 'out/T1.txt'),'changed during audit') }
            $result.decision=if($scenario.mode -eq 'completion-loop') { 'incomplete' } else { 'complete' }
            if($scenario.mode -in @('milestone','auto-milestone') -and @($logical.tasks | Where-Object { $_.required -and $_.status -ne 'verified' }).Count) { $result.decision='incomplete' }
            foreach($c in $plan.completion_criteria) { $null=Add-Proof 'criterion' @($c.id) }
        }
    }
    $result | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $context.output_directory 'result.json') -Encoding utf8
    '{"type":"fixture.finished"}'
    exit 0
} catch { [Console]::Error.WriteLine($_.ScriptStackTrace+"`n"+$_.Exception.Message); exit 1 }
