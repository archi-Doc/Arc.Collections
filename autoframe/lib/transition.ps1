function Set-PlanReturn($State) {
    if(-not $State.counters.work_since_plan) { $State.counters.workless_plan_returns++ }
    $State.counters.work_since_plan=$false; $State.phase='Plan'
}
function Get-StopStatus($State,[double]$Elapsed) {
    if($State.status -ceq 'Complete') { return 'Complete' }
    if($State.status -ceq 'NeedsInput') { return 'NeedsInput' }
    if($State.counters.no_progress_works -ge 4 -or $State.counters.workless_plan_returns -ge 3 -or $State.status -ceq 'Stalled') { return 'Stalled' }
    if($Elapsed -ge $State.settings.MaxRunMinutes*60 -or $State.attempts -ge $State.settings.MaxPhaseAttempts) { return 'Paused' }
    return 'Running'
}
function Test-AllRequired($Logical) {
    $map=New-IdMap $Logical.tasks
    foreach($id in (Get-RequiredIds $Logical)) { if($map[$id].status -cne 'verified') { return $false } }
    return $true
}
function Get-MilestoneProgress($Logical) {
    $map=New-IdMap $Logical.tasks
    foreach($milestone in $Logical.milestones) {
        $ids=@($milestone.task_ids | ForEach-Object {
            if($map[$_].status -ceq 'superseded') { $map[$_].replacements } else { $_ }
        } | Sort-Object -CaseSensitive -Unique)
        $verified=@($ids | Where-Object { $map[$_].status -ceq 'verified' })
        @{id=$milestone.id;task_ids=$ids;verified_count=$verified.Count;total_tasks=$ids.Count;ready_for_audit=($ids.Count -gt 0 -and $verified.Count -eq $ids.Count)}
    }
}
function Get-NewMilestoneKeys($Logical,[string]$Signature,$KnownKeys) {
    $map=New-IdMap $Logical.tasks; $milestones=New-IdMap $Logical.milestones
    foreach($progress in (Get-MilestoneProgress $Logical)) {
        if(-not $progress.ready_for_audit) { continue }
        $key=Get-ObjectHash @{milestone=$milestones[$progress.id];signature=$Signature;evidence=(Get-EvidenceKey $Logical @($progress.task_ids | ForEach-Object { $map[$_].evidence_refs }))}
        if($key -cnotin $KnownKeys) { $key }
    }
}
function Test-MeaningfulProgress($Logical,$Result,[switch]$CompletionOnly) {
    $tasks=New-IdMap $Logical.tasks; $findings=New-IdMap $Logical.findings
    foreach($r in $Result.task_results) {
        if($r.status -cne 'verified' -or $tasks[$r.id].status -ceq 'verified') { continue }
        foreach($e in $Result.evidence | Where-Object { $_.id -cin $r.evidence_ids }) {
            if(-not @($Logical.evidence | Where-Object { $_.phase -ceq 'Verify' -and $_.kind -ceq 'task' -and $r.id -cin $_.target_ids -and $_.hash -ceq $e.hash -and $_.input_signature -ceq $e.input_signature }).Count) { return $true }
        }
    }
    $oldHashes=@($Logical.evidence | ForEach-Object { $_.hash })
    foreach($p in $(if($CompletionOnly) { @() } else { $Result.progress })) {
        if(@($Result.evidence | Where-Object { $_.id -cin $p.evidence_ids -and $_.hash -cnotin $oldHashes }).Count) { return $true }
    }
    return @($Result.changes | Where-Object { $_.kind -ceq 'finding' -and $_.set.status -ceq 'resolved' -and $findings[$_.id].required -and $findings[$_.id].status -ceq 'open' }).Count -gt 0
}
function Test-Blocked($Logical) {
    $map=New-IdMap $Logical.tasks; $required=@(Get-RequiredIds $Logical)
    $unfinished=@($required | Where-Object { $map[$_].status -cne 'verified' })
    if(-not $unfinished.Count) { return $false }
    # If an independent task can run, continue. A pending task waiting only on blocked dependencies is external waiting too.
    foreach($t in $Logical.tasks) {
        if($t.status -ceq 'pending' -and -not @($t.depends_on | Where-Object { $map[$_].status -cne 'verified' }).Count) { return $false }
    }
    foreach($id in $unfinished) { if($map[$id].status -ceq 'implemented') { return $false } }
    return @($unfinished | Where-Object { $map[$_].status -ceq 'blocked' }).Count -gt 0
}
function Get-Transition($State,$Result,$Logical,[bool]$Meaningful,[string]$AuditKey,[bool]$RecoveryProgress=$false) {
    $next=Copy-Value $State
    $phase=$Result.phase; $decision=$Result.decision
    if($decision -ceq 'needs_input') {
        if($phase -ceq 'Work') { $next.pending_work=$true; $next.pending_route=$Result.route_hint }
        $next.status='NeedsInput'; return $next
    }
    if($phase -ceq 'Work') {
        $next.pending_work=$true; $next.pending_route=$Result.route_hint; $next.phase='Verify'
        return $next
    }
    if($phase -ceq 'Verify') {
        if(($next.pending_work -and $Meaningful) -or $RecoveryProgress) { $next.counters.workless_plan_returns=0 }
        if($next.pending_work) {
            $next.counters.works_since_audit++
            if($Meaningful) { $next.counters.no_progress_works=0 } else { $next.counters.no_progress_works++ }
        }
        $next.pending_work=$false
        if($decision -ceq 'replan' -or $next.pending_route -ceq 'replan' -or $next.counters.no_progress_works -ge 2) { Set-PlanReturn $next }
        elseif((Test-AllRequired $Logical) -or $next.counters.works_since_audit -ge $next.settings.CompletionAuditInterval) { $next.phase='CompletionAudit' }
        else { $next.phase='Prepare' }
        $next.pending_route=$null
        if(Test-Blocked $Logical) { $next.status='Blocked' }
    } elseif($phase -ceq 'CompletionAudit') {
        $next.counters.works_since_audit=0; $next.last_audit_key=$AuditKey
        if($decision -ceq 'complete') { $next.status='Complete' }
        else { Set-PlanReturn $next }
    } elseif($decision -ceq 'replan') { Set-PlanReturn $next }
    elseif($phase -ceq 'Plan') {
        $next.counters.work_since_plan=$false
        $next.phase=if($decision -ceq 'recover') { 'Verify' } else { 'Prepare' }
        if($decision -cne 'recover' -and (Test-Blocked $Logical)) { $next.status='Blocked' }
    } elseif($phase -ceq 'Prepare') {
        $next.phase=if($decision -ceq 'completion_candidate') { 'CompletionAudit' } else { 'Audit' }
    } elseif($phase -ceq 'Audit') {
        if($decision -ceq 'approved') { $next.phase='Work' }
        else {
            $next.counters.audit_revisions++
            if($next.counters.audit_revisions -ge 2) { Set-PlanReturn $next } else { $next.phase='Prepare' }
        }
    }
    $status=Get-StopStatus $next $next.elapsed_seconds
    if($status -cne 'Running') { $next.status=$status }
    return $next
}

function Assert-ResultEnvelope($Result,$State,$Context) {
    Assert-Schema $Result 'result'
    foreach($key in @('run_id','attempt_id','phase','input_plan_hash','base_plan_version','execution_plan_hash')) {
        if((Get-ObjectHash $Result[$key]) -cne (Get-ObjectHash $Context[$key])) { throw "Stale or mismatched result: $key" }
    }
    if($Result.attempt_id -cin $State.accepted_attempts) { throw 'Attempt already accepted.' }
    $allowed=@{
        Plan=@('ready','recover','replan','needs_input');Prepare=@('ready','completion_candidate','replan','needs_input')
        Audit=@('approved','revise','replan','needs_input');Work=@('reported','needs_input')
        Verify=@('accepted','revise','replan','needs_input');CompletionAudit=@('complete','incomplete','replan','needs_input')
    }
    if($Result.decision -cnotin $allowed[$Result.phase]) { throw 'Decision is not allowed in this phase.' }
    Assert-Unique @($Result.task_results | ForEach-Object { $_.id })
    if($Result.phase -ceq 'Plan' -and $Result.decision -ceq 'recover' -and -not $Result.task_results.Count) { throw 'Plan recover must explicitly identify verification targets.' }
    if($Result.phase -cin @('Work','Verify')) {
        if((Get-ObjectHash @($Result.task_results | ForEach-Object { $_.id } | Sort-Object -CaseSensitive)) -cne (Get-ObjectHash @($Context.targets | Sort-Object -CaseSensitive))) { throw 'Work/Verify must report every target exactly once.' }
    }
    if($Result.phase -cnotin @('Prepare','Audit') -and $null -ne $Result.execution_plan) { throw 'Unexpected execution plan.' }
    if(($Result.phase -ceq 'Prepare' -and $Result.decision -ceq 'ready') -or ($Result.phase -ceq 'Audit' -and $Result.decision -ceq 'approved')) {
        if($null -eq $Result.execution_plan) { throw 'Execution plan is required.' }
    }
    if($Result.phase -ceq 'Verify' -and $Result.decision -ceq 'accepted' -and @($Result.task_results | Where-Object { $_.status -cne 'verified' }).Count) { throw 'Verify accepted requires all targets verified.' }
    if($Result.phase -ceq 'Verify' -and $Result.decision -ceq 'revise' -and -not @($Result.task_results | Where-Object { $_.status -cne 'verified' }).Count) { throw 'Verify revise requires an unfinished target.' }
}

function Apply-Result($Logical,$Result,$Plan,$Context,[string]$Root,[string]$HomePath) {
    $next=Copy-Value $Logical
    $tasks=New-IdMap $next.tasks; $findings=New-IdMap $next.findings; $milestones=New-IdMap $next.milestones
    $oldProof=New-IdMap $next.evidence; $newProof=New-IdMap $Result.evidence
    foreach($e in $Result.evidence) {
        if($oldProof.ContainsKey($e.id)) { throw "Evidence ID already exists: $($e.id)" }
        if($e.input_signature -cne $Context.signature) { throw 'Evidence signature mismatch.' }
        $path=Resolve-Safe $Context.output_directory $e.path
        $value=Copy-Value $e; $value.phase=$Result.phase
        $value.record_path="records/$($e.hash).evidence"
        $dest=Join-Path $HomePath $value.record_path
        [Autoframe.Json]::StoreEvidence($path,$dest,$e.hash,[Action]{ Invoke-Tick })
        $value.artifact_manifest=$Context.artifact_manifest
        $next.evidence+=@($value)
    }
    $proof=New-IdMap $next.evidence
    function Require-Proof($Ids,[string]$Target,[string[]]$Kinds,[switch]$Current) {
        if(-not $Ids.Count) { throw "Missing evidence: $Target" }
        foreach($id in $Ids) {
            $reference="Invalid evidence reference: $id (phase=$($Result.phase), target=$Target)"
            if(-not $proof.ContainsKey($id)) { throw "${reference}: evidence ID not found." }
            if($Target -cnotin $proof[$id].target_ids) { throw "${reference}: target_ids=[$($proof[$id].target_ids -join ',')]; target must match exactly." }
            if($proof[$id].kind -cnotin $Kinds) { throw "${reference}: kind=$($proof[$id].kind); allowed kinds=[$($Kinds -join ',')]." }
            if($Current -and -not $newProof.ContainsKey($id)) { throw 'This acceptance requires evidence from this trial.' }
        }
    }
    $changed=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($c in $Result.changes) {
        if(-not $changed.Add("$($c.kind):$($c.id)")) { throw 'Repeated change target.' }
        if($c.kind -cin @('task','milestone') -and $Result.phase -cne 'Plan') { throw 'Only Plan may change definitions.' }
        if($c.kind -ceq 'task') {
            if(-not $tasks.ContainsKey($c.id)) {
                $v=@{id=$c.id;required=$false;status='pending';evidence_refs=@();remaining=@();blocker=$null;replacements=@();source_ids=@();deliverables=@()}
                $tasks.Add($c.id,$v); $next.tasks+=@($v)
            }
            $t=$tasks[$c.id]
            if($t.required -and $null -ne $c.set.required -and -not $c.set.required) { throw 'A required task cannot become optional.' }
            foreach($k in $c.set.Keys) { if($null -ne $c.set[$k]) { $t[$k]=$c.set[$k] } }
            if($t.replacements.Count) { $t.status='superseded' }
        } elseif($c.kind -ceq 'milestone') {
            if(-not $milestones.ContainsKey($c.id)) { $v=@{id=$c.id}; $milestones.Add($c.id,$v); $next.milestones+=@($v) }
            foreach($k in $c.set.Keys) { if($null -ne $c.set[$k]) { $milestones[$c.id][$k]=$c.set[$k] } }
        } else {
            if(-not $findings.ContainsKey($c.id)) { throw 'Use findings to add a finding.' }
            $f=$findings[$c.id]
            if($null -ne $c.set.task_ids) {
                if($Result.phase -cne 'Plan') { throw 'Only Plan assigns findings.' }; $f.task_ids=$c.set.task_ids
            }
            if($null -ne $c.set.status) {
                $allowed=if($f.kind -ceq 'plan') { @('Audit') } else { @('Verify','CompletionAudit') }
                if($Result.phase -cnotin $allowed -or [string]::IsNullOrWhiteSpace($c.set.reason)) { throw 'Finding resolution authority/reason missing.' }
                Require-Proof $c.set.evidence_refs $f.id @('finding') -Current
                $f.status='resolved'; $f.evidence_refs=$c.set.evidence_refs
            } elseif($null -ne $c.set.evidence_refs -or $null -ne $c.set.reason) { throw 'Resolution fields require status.' }
        }
    }
    foreach($f in $Result.findings) {
        if($f.status -cne 'open' -or $f.evidence_refs.Count -or $findings.ContainsKey($f.id)) { throw 'New findings must have unique IDs and remain open.' }
        $findings.Add($f.id,$f); $next.findings+=@($f)
    }
    foreach($r in $Result.task_results) {
        if(-not $tasks.ContainsKey($r.id)) { throw "Unknown result target: $($r.id)" }
        $allowed=if($Result.phase -ceq 'Work') { @('pending','implemented','blocked') } elseif($Result.phase -ceq 'Verify') { @('pending','verified','blocked') } else { @('pending','blocked') }
        if($r.status -cnotin $allowed -or $tasks[$r.id].status -ceq 'superseded') { throw 'Task status authority violation.' }
        if(($r.status -ceq 'blocked') -ne ($null -ne $r.blocker)) { throw 'Blocked needs reason and release condition.' }
        if($r.status -ceq 'verified') {
            if($r.remaining.Count) { throw 'Verified cannot retain unfinished work.' }
            Require-Proof $r.evidence_ids $r.id @('task') -Current
            $artifacts=Read-Record $HomePath "manifests/$($Context.artifact_manifest).json" 'manifest'
            foreach($p in $tasks[$r.id].deliverables) {
                if(-not @($artifacts.entries | Where-Object { $_.kind -ceq 'file' -and (Test-InScope $_.path @($p)) }).Count) { throw "Required deliverable missing: $p" }
            }
        }
        elseif($r.evidence_ids.Count) { Require-Proof $r.evidence_ids $r.id @('work','task','progress') }
        $t=$tasks[$r.id]; $t.status=$r.status; $t.evidence_refs=$r.evidence_ids; $t.remaining=$r.remaining; $t.blocker=$r.blocker
    }
    foreach($p in $Result.progress) {
        if($Result.phase -cnotin @('Work','Verify')) { throw "Progress is only allowed in Work/Verify; phase=$($Result.phase), task=$($p.task_id). Return progress=[] for this phase." }
        if($p.task_id -cnotin $Context.targets) { throw "Invalid progress target: $($p.task_id); phase=$($Result.phase), allowed targets=[$($Context.targets -join ',')]." }
        Require-Proof $p.evidence_ids $p.task_id @('task','work','progress') -Current
    }
    if($null -ne $Result.execution_plan) { $next.execution_plan=$Result.execution_plan }
    if($Result.phase -ceq 'Plan') { $next.execution_plan=$null }
    Assert-Logical $next $Plan -AllowIncomplete:($Result.phase -ceq 'Plan' -and $Result.decision -cin @('replan','needs_input')) -RequireMilestones:($Result.phase -ceq 'Plan' -and $Result.decision -cin @('ready','recover'))
    if($Result.phase -ceq 'Audit' -and $Result.decision -ceq 'approved') {
        if(@($next.findings | Where-Object { $_.required -and $_.kind -ceq 'plan' -and $_.status -ceq 'open' }).Count) { throw 'Required plan findings remain open.' }
        if(-not @($Result.evidence | Where-Object { $_.kind -ceq 'plan' }).Count) { throw 'Audit approval requires plan evidence.' }
        foreach($id in $next.execution_plan.tasks.id) {
            Require-Proof @($Result.evidence | Where-Object { $_.kind -ceq 'plan' -and $id -cin $_.target_ids } | ForEach-Object { $_.id }) $id @('plan') -Current
        }
    }
    if($Result.phase -ceq 'CompletionAudit' -and $Result.decision -ceq 'complete') {
        if(-not (Test-AllRequired $next) -or @($next.tasks | Where-Object { $_.status -ceq 'implemented' }).Count -or @($next.findings | Where-Object { $_.required -and $_.status -ceq 'open' }).Count) { throw 'Completion gate failed.' }
        foreach($c in $Plan.completion_criteria) { Require-Proof @($Result.evidence | Where-Object { $_.kind -ceq 'criterion' -and $c.id -cin $_.target_ids } | ForEach-Object { $_.id }) $c.id @('criterion') -Current }
    }
    return $next
}

function Update-StaleEvidence($Logical,[string]$Signature,[string]$HomePath,$Snapshot) {
    $changed=$false; $proof=New-IdMap $Logical.evidence
    foreach($t in $Logical.tasks) {
        if($t.status -cne 'verified') { continue }
        $valid=$t.evidence_refs.Count -gt 0
        foreach($id in $t.evidence_refs) {
            if(-not $proof.ContainsKey($id)) { $valid=$false; break }
            $e=$proof[$id]
            try {
                $artifact=Read-Record $HomePath "manifests/$($e.artifact_manifest).json" 'manifest'
                $current=New-Manifest $Snapshot $artifact.scope -FilesOnly
                if($e.phase -cne 'Verify' -or $e.kind -cne 'task' -or $t.id -cnotin $e.target_ids -or $e.input_signature -cne $Signature -or [Autoframe.Json]::FileHash((Join-Path $HomePath $e.record_path),[Action]{ Invoke-Tick }) -cne $e.hash -or (Get-ObjectHash $current) -cne $e.artifact_manifest) { $valid=$false }
            } catch { if($_.Exception.GetBaseException() -is [TimeoutException] -or $_.Exception.GetBaseException() -is [OperationCanceledException]) { throw }; $valid=$false }
        }
        if(-not $valid) { $t.status='pending'; $t.remaining=@('証拠失効。現行入力で再検証する。'); $changed=$true }
    }
    foreach($f in $Logical.findings) {
        if($f.status -cne 'resolved') { continue }
        $valid=$f.evidence_refs.Count -gt 0
        $phases=if($f.kind -ceq 'plan') { @('Audit') } else { @('Verify','CompletionAudit') }
        foreach($id in $f.evidence_refs) {
            if(-not $proof.ContainsKey($id)) { $valid=$false; break }
            $e=$proof[$id]
            try {
                $artifact=Read-Record $HomePath "manifests/$($e.artifact_manifest).json" 'manifest'
                $current=New-Manifest $Snapshot $artifact.scope -FilesOnly
                if($e.phase -cnotin $phases -or $e.kind -cne 'finding' -or $f.id -cnotin $e.target_ids -or $e.input_signature -cne $Signature -or [Autoframe.Json]::FileHash((Join-Path $HomePath $e.record_path),[Action]{ Invoke-Tick }) -cne $e.hash -or (Get-ObjectHash $current) -cne $e.artifact_manifest) { $valid=$false }
            } catch { if($_.Exception.GetBaseException() -is [TimeoutException] -or $_.Exception.GetBaseException() -is [OperationCanceledException]) { throw }; $valid=$false }
        }
        if(-not $valid) { $f.status='open'; $f.evidence_refs=@(); $changed=$true }
    }
    return $changed
}
