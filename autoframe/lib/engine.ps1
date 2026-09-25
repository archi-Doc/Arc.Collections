. (Join-Path $PSScriptRoot 'regeneration.ps1')
$script:WorkerClock=$null
function Assert-Settings($Settings) {
    if(-not $Settings.Contains('PlanRegenerationInterval')) { $Settings.PlanRegenerationInterval=3 }
    if($Settings.PlanRegenerationInterval -le 0) { throw 'PlanRegenerationInterval must be positive.' }
    foreach($key in @('MaxRunMinutes','MaxPhaseAttempts','PhaseTimeoutMinutes','SaveReserveMinutes','StopTimeoutSeconds','MaxTasksPerWork','CompletionAuditInterval')) { if($Settings[$key] -le 0) { throw "$key must be positive." } }
    if($Settings.MaxTasksPerWork -gt 3 -or $Settings.SaveReserveMinutes -ge $Settings.PhaseTimeoutMinutes) { throw 'Invalid batch/reserve limit.' }
    foreach($key in $Settings.PhaseModels.Keys) { if($key -cnotin $script:Phases -or $Settings.PhaseModels[$key] -isnot [string] -or [string]::IsNullOrWhiteSpace($Settings.PhaseModels[$key])) { throw 'Invalid PhaseModels entry.' } }
    if([string]::IsNullOrWhiteSpace($Settings.CodexCommand)) { throw 'CodexCommand is empty.' }
}
function New-State([string]$Root,$Plan,$Settings,[string]$HomePath) {
    $logical=New-Logical $Plan.data
    @{
        schema_version=1;run_id=[guid]::NewGuid().ToString('N');project_root=$Root;project_id=$Plan.data.project_id;plan_hash=$Plan.hash
        plan_ref=(Save-Record $HomePath $Plan.data);base_plan_version=0;logical_ref=(Save-Record $HomePath $logical)
        status='Running';phase='Plan';settings=$Settings;elapsed_seconds=0.0;heartbeat_utc=[datetime]::UtcNow.ToString('O');running=$false
        attempts=0;counters=@{no_progress_works=0;workless_plan_returns=0;audit_revisions=0;works_since_audit=0;work_since_plan=$false}
        plan_attempts=0;plan_regeneration_pending=$false;plan_publication=$null
        active=$null;uncertain=@();pending_work=$false;pending_route=$null;verify_targets=@();accepted_attempts=@();history=@()
        last_audit_key=$null;complete_signature=$null;approval_key=$null;milestone_keys=@();message=''
    }
}
function Save-State($Candidate=$script:State) {
    if($null -ne $script:StateHash -and [Autoframe.Json]::FileHash($script:StatePath) -cne $script:StateHash) { throw 'state.json changed outside the runner.' }
    $Candidate.elapsed_seconds=$script:BaseElapsed+$script:RunClock.Elapsed.TotalSeconds
    $Candidate.heartbeat_utc=[datetime]::UtcNow.ToString('O')
    Assert-Schema $Candidate 'state'
    $hash=Get-ObjectHash $Candidate
    Save-Json $script:StatePath $Candidate
    # Install an acceptance only after its single durable publication. No fallible read follows it.
    $script:State=$Candidate
    $script:StateHash=$hash
    $script:LastHeartbeat=$script:RunClock.Elapsed.TotalSeconds
}
function Save-Logical($Logical,[switch]$Increment) {
    $ref=Save-Record $script:HomePath $Logical
    if($ref -cne $script:State.logical_ref) {
        $script:State.logical_ref=$ref
        if($Increment) { $script:State.base_plan_version++ }
    }
}
function Get-Remaining { $script:State.settings.MaxRunMinutes*60-($script:BaseElapsed+$script:RunClock.Elapsed.TotalSeconds) }
function Format-RunDuration([double]$Seconds) {
    $total=[long][Math]::Floor([Math]::Max(0,$Seconds))
    '{0:00}:{1:00}:{2:00}' -f [long][Math]::Floor($total/3600),[long][Math]::Floor(($total%3600)/60),($total%60)
}
function Write-RunEvent([string]$Message) { [Console]::WriteLine("[$([datetime]::Now.ToString('HH:mm:ss'))] $Message") }
function Write-RunStatus {
    $elapsed=if($null -ne $script:WorkerClock) { Format-RunDuration $script:WorkerClock.Elapsed.TotalSeconds } else { '-' }
    $totalElapsed=Format-RunDuration ($script:BaseElapsed+$script:RunClock.Elapsed.TotalSeconds)
    $remaining=Format-RunDuration (Get-Remaining)
    Write-RunEvent "$($script:Activity) | phase=$($script:State.phase) | attempts=$($script:State.attempts)/$($script:State.settings.MaxPhaseAttempts) | Plan=$($script:State.plan_attempts) | elapsed=$elapsed | total_elapsed=$totalElapsed | remaining=$remaining"
    $script:LastStatus=$script:RunClock.Elapsed.TotalSeconds
}
function Get-AncestorInstructions([string]$Root) {
    $hashes=[Collections.Generic.List[object]]::new()
    for($dir=[IO.DirectoryInfo]::new($Root);$null -ne $dir;$dir=$dir.Parent) {
        foreach($name in @('AGENTS.md','AGENTS.override.md','.codex/config.toml')) {
            $path=Join-Path $dir.FullName $name
            if([IO.File]::Exists($path)) { $hashes.Add(@{path=$path;hash=[Autoframe.Json]::FileHash($path,[Action]{ Invoke-Tick })}) }
        }
    }
    return ,$hashes.ToArray()
}
function Get-Environment($Plan,[string]$Root,[string]$Directory,[double]$Seconds,[string]$CliVersion) {
    $checks=@(); $timer=[Diagnostics.Stopwatch]::StartNew()
    for($i=0;$i -lt $Plan.environment_checks.Count;$i++) {
        $dir=Join-Path $Directory "probe-$i"; $null=[IO.Directory]::CreateDirectory($dir)
        $scriptPath=Join-Path $dir 'check.ps1'
        $code="`$ErrorActionPreference='Stop'`n`$PSNativeCommandUseErrorActionPreference=`$true`ntry {`n"+$Plan.environment_checks[$i]+"`nif (-not `$?) { exit 1 }`n} catch { [Console]::Error.WriteLine(`$_.Exception.Message); exit 1 }`n"
        [Autoframe.Json]::Atomic($scriptPath,$code)
        $remaining=[Math]::Min($Seconds-$timer.Elapsed.TotalSeconds,(Get-Remaining))
        if($remaining -le 0) { throw [TimeoutException]::new('Environment check deadline.') }
        $r=Invoke-Child (Get-Command pwsh -CommandType Application | Select-Object -First 1).Source @('-NoProfile','-NonInteractive','-File',$scriptPath) $Root $dir '' $remaining $script:State.settings.StopTimeoutSeconds
        if($r.timed_out) { throw [TimeoutException]::new('Environment check timed out.') }
        if($r.exit_code -ne 0) { throw "Environment check failed ($i); see $dir" }
        $checks+=@(@{command=$Plan.environment_checks[$i];exit_code=$r.exit_code;output_hash=[Autoframe.Json]::FileHash((Join-Path $dir 'events.jsonl'));stderr_hash=[Autoframe.Json]::FileHash((Join-Path $dir 'stderr.log'))})
    }
    # Configuration hashes identify inherited settings without disclosing configuration contents.
    $configHashes=@()
    $configRoot=if($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex' }
    foreach($name in @('config.toml','AGENTS.md','AGENTS.override.md')) {
        $p=Join-Path $configRoot $name
        $configHashes+=@(@{name=$name;hash=$(if(Test-Path -LiteralPath $p -PathType Leaf) { [Autoframe.Json]::FileHash($p) } else { $null })})
    }
    @{os=[Runtime.InteropServices.RuntimeInformation]::OSDescription;powershell=$PSVersionTable.PSVersion.ToString();cli=$CliVersion;settings=$script:State.settings;permissions='inherited';config=$configHashes;instructions=(Get-AncestorInstructions $Root);checks=$checks}
}
function Get-FrameworkHash { Get-ObjectHash (Get-Snapshot $script:FrameRoot -Distribution) }
function Assert-ProtectedChanges($Before,$After,[string]$Directory,[string]$Phase,$FrameworkBefore=@(),$FrameworkAfter=@()) {
    $changes=@(Get-ManifestChanges $Before.entries $After.entries | ForEach-Object { $_.area='project'; $_ })
    $changes+=@(Get-ManifestChanges $FrameworkBefore $FrameworkAfter | ForEach-Object { $_.area='framework'; $_ })
    if(-not $changes.Count) { return }
    $report=Join-Path $Directory 'protected-changes.json'
    Save-Json $report @{phase=$Phase;changes=$changes}
    $details=@($changes | Select-Object -First 20 | ForEach-Object { "$($_.area): $($_.change) $($_.path)" }) -join '; '
    throw "Protected files changed during $Phase ($($changes.Count)). $details. Change origin is unknown. See $report"
}
function Save-Manifest($Manifest) {
    $ref=Save-Record $script:HomePath $Manifest 'manifests'
    [IO.Path]::GetFileNameWithoutExtension($ref)
}
function New-WorkerInput($Plan,$Logical,$Environment,[string]$FrameworkHash,$Snapshot,[string]$Directory,[double]$Seconds) {
    $state=$script:State; $phase=$state.phase
    $inputManifest=Get-InputManifest $state.project_root $Plan.data $Snapshot
    $inputHash=Save-Manifest $inputManifest
    $signature=Get-Signature $Plan.hash $Logical $inputHash $Environment $FrameworkHash
    $targets=@(if($phase -cin @('Work','Verify','Audit') -and $null -ne $Logical.execution_plan) { $Logical.execution_plan.tasks.id })
    if($phase -ceq 'Verify') {
        $known=New-IdMap $Logical.tasks
        $targets=@(@($targets)+@($state.verify_targets)+@($state.uncertain | ForEach-Object { $_.targets }) | Where-Object { $known.ContainsKey($_) -and $known[$_].status -cne 'superseded' } | Sort-Object -CaseSensitive -Unique)
        if(-not $targets.Count) { $targets=@($Logical.tasks | Where-Object { $_.status -cin @('pending','implemented') } | ForEach-Object { $_.id }) }
    }
    $artifactScopes=@($Logical.tasks | ForEach-Object { $_.deliverables } | Sort-Object -CaseSensitive -Unique)
    foreach($p in $artifactScopes) { Assert-Relative $p }
    $artifactHash=Save-Manifest (New-Manifest $Snapshot $artifactScopes -FilesOnly)
    $output=Join-Path $Directory 'output'; $null=[IO.Directory]::CreateDirectory($output)
    @{
        schema_version=1;run_id=$state.run_id;attempt_id=[IO.Path]::GetFileName($Directory);phase=$phase;input_plan_hash=$Plan.hash
        base_plan_version=$state.base_plan_version;execution_plan_hash=$(if($null -ne $Logical.execution_plan) { Get-ObjectHash $Logical.execution_plan } else { $null })
        project_root=$state.project_root;plan_path=(Join-Path $state.project_root 'PLAN.md');plan_record=(Join-Path $script:HomePath $state.plan_ref)
        logical_path=(Join-Path $script:HomePath $state.logical_ref);targets=$targets;uncertain=$state.uncertain;pending_work=$state.pending_work;verify_targets=$state.verify_targets
        output_directory=$output;result_schema=(Join-Path $script:FrameRoot 'schemas/result.schema.json');signature=$signature;input_manifest=$inputHash;artifact_manifest=$artifactHash
        max_tasks=$state.settings.MaxTasksPerWork;deadline_utc=[datetime]::UtcNow.AddSeconds($Seconds).ToString('O')
        save_from_utc=[datetime]::UtcNow.AddSeconds([Math]::Max(0,$Seconds-$state.settings.SaveReserveMinutes*60)).ToString('O')
        environment=$Environment;framework_hash=$FrameworkHash
        default_generated_scope=@(Get-DefaultGeneratedScope);effective_generated_scope=@(Get-GeneratedScope $Plan.data)
        dependency_preflight=$(if(@($inputManifest.entries | Where-Object { $_.kind -ceq 'file' -and $_.path -match '\.(slnx?|[cf]sproj|vbproj)$' -and -not (Test-Protected $state.project_root $_.path) }).Count) {
            @{script=(Join-Path $script:FrameRoot 'check-dependencies.ps1');report=(Join-Path $output 'dependency-preflight.json')}
        } else { $null })
        milestone_progress=@(Get-MilestoneProgress $Logical)
        plan_attempt_number=($state.plan_attempts+1);regenerate_plan=(Test-PlanRegenerationDue $state)
        original_prompt=$(if(Test-PlanRegenerationDue $state) { Get-OriginalPrompt ([IO.File]::ReadAllText((Join-Path $state.project_root 'PLAN.md'))) } else { $null })
        plan_schema=(Join-Path $script:FrameRoot 'schemas/plan.schema.json')
    }
}
function Write-WorkerPrompt($Context,[string]$Directory) {
    $contextPath=Join-Path $Directory 'input.json'; Save-Json $contextPath $Context
    $common=[IO.File]::ReadAllText((Join-Path $script:FrameRoot 'prompts/common.md'))
    $phase=[IO.File]::ReadAllText((Join-Path $script:FrameRoot "prompts/$($Context.phase).md"))
    $text=$common+"`n`n"+$phase+"`n`n入力参照: $contextPath`nまずこのJSONを読み、指定された現行記録だけを必要に応じて参照する。`n"
    [Autoframe.Json]::Atomic((Join-Path $Directory 'prompt.md'),$text)
}
function Assert-ProofFiles($Logical,[string]$Signature) {
    foreach($e in $Logical.evidence) {
        # Old evidence remains history. Only proofs referenced by current conclusions are required here.
        $referenced=@($Logical.tasks | Where-Object { $_.status -ceq 'verified' -and $e.id -cin $_.evidence_refs }).Count -or @($Logical.findings | Where-Object { $_.status -ceq 'resolved' -and $e.id -cin $_.evidence_refs }).Count
        if($referenced -and [Autoframe.Json]::FileHash((Join-Path $script:HomePath $e.record_path)) -cne $e.hash) { throw "Referenced evidence corrupt: $($e.id)" }
    }
}
function Assert-ApprovalProofs($State,$Logical,$Context,[string]$HomePath) {
    $approval=$null
    for($i=$State.history.Count-1;$i -ge 0;$i--) {
        $r=Read-Record $HomePath $State.history[$i]
        if($r.Contains('phase') -and $r.phase -ceq 'Audit' -and $r.decision -ceq 'approved') { $approval=$r; break }
    }
    if($null -eq $approval -or $approval.input_plan_hash -cne $Context.input_plan_hash -or (Get-ObjectHash $approval.execution_plan) -cne (Get-ObjectHash $Logical.execution_plan)) { throw 'Audit acceptance does not match this Work.' }
    $proof=New-IdMap $Logical.evidence
    $ids=@($approval.evidence | Where-Object { $_.kind -ceq 'plan' } | ForEach-Object { $_.id })
    if(-not $ids.Count) { throw 'Audit proof missing.' }
    foreach($id in $ids) {
        if(-not $proof.ContainsKey($id)) { throw 'Audit proof missing.' }
        $e=$proof[$id]
        if($e.phase -cne 'Audit' -or $e.input_signature -cne $Context.signature -or [Autoframe.Json]::FileHash((Join-Path $HomePath $e.record_path),[Action]{ Invoke-Tick }) -cne $e.hash) { throw 'Audit proof invalid.' }
    }
}
function Assert-DependencyPreflight($Context) {
    if($null -eq $Context.dependency_preflight) { return }
    $report=Read-Json $Context.dependency_preflight.report
    if(-not $report.Contains('status') -or $report.status -cnotin @('ready','blocked') -or -not $report.Contains('exit_code')) { throw 'Invalid Worker dependency preflight report.' }
    if($report.status -ceq 'ready' -and $report.exit_code -ne 0) { throw 'Successful dependency preflight requires exit code 0.' }
    if($report.status -ceq 'blocked') {
        if(-not $report.Contains('error') -or [string]::IsNullOrWhiteSpace($report.error) -or -not $report.Contains('release_condition') -or [string]::IsNullOrWhiteSpace($report.release_condition)) { throw 'Blocked dependency preflight requires a cause and release condition.' }
        Write-RunEvent "Worker dependency preflight blocked. See $($Context.dependency_preflight.report)"
    }
}
function Test-CompletionProofs($State,$Logical,$Plan,$Context,$Snapshot,[string]$HomePath) {
    if($State.complete_signature -cne $Context.signature -or -not (Test-AllRequired $Logical) -or $State.uncertain.Count -or $State.pending_work) { return $false }
    if(@($Logical.tasks | Where-Object { $_.status -ceq 'implemented' }).Count -or @($Logical.findings | Where-Object { $_.required -and $_.status -ceq 'open' }).Count) { return $false }
    try {
        $acceptance=$null
        for($i=$State.history.Count-1;$i -ge 0;$i--) {
            Invoke-Tick
            $record=Read-Record $HomePath $State.history[$i]
            if($record.Contains('phase') -and $record.phase -ceq 'CompletionAudit') { $acceptance=$record; break }
        }
        if($null -eq $acceptance -or $acceptance.decision -cne 'complete' -or $acceptance.attempt_id -cnotin $State.accepted_attempts -or $acceptance.input_plan_hash -cne $Context.input_plan_hash) { return $false }
        $proof=New-IdMap $Logical.evidence
        foreach($criterion in $Plan.completion_criteria) {
            $accepted=@($acceptance.evidence | Where-Object { $_.kind -ceq 'criterion' -and $criterion.id -cin $_.target_ids })
            if(-not $accepted.Count) { return $false }
            foreach($e in $accepted) {
                if(-not $proof.ContainsKey($e.id)) { return $false }
                $saved=$proof[$e.id]
                if($saved.phase -cne 'CompletionAudit' -or $saved.kind -cne 'criterion' -or $saved.hash -cne $e.hash -or $saved.input_signature -cne $Context.signature -or $e.input_signature -cne $Context.signature -or $criterion.id -cnotin $saved.target_ids) { return $false }
                if([Autoframe.Json]::FileHash((Join-Path $HomePath $saved.record_path),[Action]{ Invoke-Tick }) -cne $saved.hash) { return $false }
                $manifest=Read-Record $HomePath "manifests/$($saved.artifact_manifest).json" 'manifest'
                if((Get-ObjectHash (New-Manifest $Snapshot $manifest.scope -FilesOnly)) -cne $saved.artifact_manifest) { return $false }
            }
        }
        return $true
    } catch {
        if($_.Exception.GetBaseException() -is [TimeoutException] -or $_.Exception.GetBaseException() -is [OperationCanceledException]) { throw }
        return $false
    }
}
function Start-Frame([string]$Root,$Settings,$Explicit,[switch]$Resume,[switch]$NewRun,[switch]$ResetStallCounters,[string]$ResetReason) {
    if(-not $IsWindows) { throw 'This implementation requires Windows Job Objects; other operating systems are unsupported.' }
    if($Resume -and $NewRun) { throw 'Resume and NewRun are mutually exclusive.' }
    if(($ResetStallCounters -and (-not $Resume -or [string]::IsNullOrWhiteSpace($ResetReason))) -or (-not $ResetStallCounters -and $Explicit.ContainsKey('ResetReason'))) { throw 'Reset requires Resume, ResetStallCounters and a nonempty ResetReason.' }
    $Root=[IO.Path]::TrimEndingDirectorySeparator((Resolve-Path -LiteralPath $Root -ErrorAction Stop).ProviderPath)
    if(-not (Test-Path -LiteralPath $Root -PathType Container)) { throw 'ProjectRoot must be an existing directory.' }
    if((Get-Item -LiteralPath $Root).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'ProjectRoot cannot be a link.' }
    $plan=Read-Plan $Root
    foreach($name in @('common')+$script:Phases) { if(-not (Test-Path -LiteralPath (Join-Path $script:FrameRoot "prompts/$name.md") -PathType Leaf)) { throw "Missing prompt: $name" } }
    foreach($name in @('plan','result','logical','state','manifest')) { $null=Read-Json (Join-Path $script:FrameRoot "schemas/$name.schema.json") }
    $script:HomePath=Join-Path $Root '.autoframe'; $script:StatePath=Join-Path $script:HomePath 'state.json'
    $null=[IO.Directory]::CreateDirectory($script:HomePath)
    if((Get-Item -LiteralPath $script:HomePath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw '.autoframe cannot be a link.' }
    foreach($entry in Get-ChildItem -LiteralPath $script:HomePath -Recurse -Force) {
        if($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Internal record path cannot be a link: $($entry.FullName)" }
    }
    $lock=$null; $script:State=$null; $script:StateHash=$null; $script:Tick=$null; $started=$false
    try {
        $lock=[IO.File]::Open((Join-Path $script:HomePath 'lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
        [Autoframe.Cancellation]::Register()
        $script:SessionStarted=[datetimeoffset]::Now
        $script:RunClock=[Diagnostics.Stopwatch]::StartNew(); $script:LastHeartbeat=0.0
        $script:WorkerClock=$null
        $previous=$null; $badState=$false
        if(Test-Path -LiteralPath $script:StatePath) {
            try { $previous=Read-Json $script:StatePath 'state'; $null=Read-Record $script:HomePath $previous.logical_ref 'logical'; $null=Read-Record $script:HomePath $previous.plan_ref }
            catch { if(-not $NewRun) { throw }; $badState=$true }
        }
        if($Resume -and $null -eq $previous) { throw 'Resume requires valid saved state.' }
        if($NewRun -and $previous -and $previous.Contains('plan_publication') -and $null -ne $previous.plan_publication) { throw 'Resume must finish the pending PLAN publication before NewRun.' }
        if(-not $Resume -and -not $NewRun -and $previous -and $previous.status -cne 'Complete') { throw 'An unfinished run exists. Use Resume or NewRun.' }
        if($Resume) {
            if($previous.project_root -cne $Root -or $previous.project_id -cne $plan.data.project_id) { throw 'ProjectRoot/project_id changed; use NewRun.' }
            Initialize-PlanRegeneration $previous $script:HomePath
            foreach($key in @($Settings.Keys)) { if(-not $Explicit.ContainsKey($key)) { $Settings[$key]=$previous.settings[$key] } }
        }
        Assert-Settings $Settings; $Settings.CodexCommand=Resolve-Cli $Settings.CodexCommand
        Stop-OldChildren $script:HomePath $Settings.StopTimeoutSeconds
        $script:StateHash=if(Test-Path -LiteralPath $script:StatePath) { [Autoframe.Json]::FileHash($script:StatePath) } else { $null }
        if($Resume) {
            $script:State=$previous
            $script:BaseElapsed=[double]$previous.elapsed_seconds
            if($previous.running) {
                $estimate=[Math]::Max(0,([datetime]::UtcNow-[datetime]::Parse($previous.heartbeat_utc).ToUniversalTime()).TotalSeconds)
                $charge=[Math]::Max(0,$estimate-$script:RunClock.Elapsed.TotalSeconds)
                $script:BaseElapsed+=$charge
                $script:State.history+=@(Save-Record $script:HomePath @{kind='estimated-time';interval_seconds=$estimate;added_seconds=$charge;from=$previous.heartbeat_utc;to=[datetime]::UtcNow.ToString('O');overlap='current stopwatch interval excluded'})
            }
            if((Get-ObjectHash $Settings) -cne (Get-ObjectHash $previous.settings)) { $script:State.history+=@(Save-Record $script:HomePath @{kind='settings';before=$previous.settings;after=$Settings}) }
            $script:State.settings=$Settings
            if($ResetStallCounters) {
                $script:State.history+=@(Save-Record $script:HomePath @{kind='reset-stall';reason=$ResetReason;counters=$script:State.counters})
                $script:State.counters.no_progress_works=0; $script:State.counters.workless_plan_returns=0; $script:State.counters.audit_revisions=0
                $script:State.status='Running'
            }
        } else {
            if($script:StateHash) {
                $archive=Join-Path $script:HomePath ('records/previous-'+[guid]::NewGuid().ToString('N')+'.json')
                $null=[IO.Directory]::CreateDirectory((Join-Path $script:HomePath 'records'))
                [IO.File]::Copy($script:StatePath,$archive,$false)
            }
            $script:State=New-State $Root $plan $Settings $script:HomePath; $script:BaseElapsed=0.0
            if($previous -and $previous.active) { $script:State.uncertain+=@($previous.active) }
            if($previous) { $script:State.uncertain+=@($previous.uncertain) }
            if($previous -and $previous.pending_work) {
                # A new run discards completion judgments, not the obligation to inspect unfinished effects.
                $pendingId=[guid]::NewGuid().ToString('N')
                $oldDirectory=Join-Path $script:HomePath "runs/$($previous.run_id)/$pendingId"
                $beforeHash=$null
                for($i=$previous.history.Count-1;$i -ge 0;$i--) {
                    try { $record=Read-Record $script:HomePath $previous.history[$i] } catch { continue }
                    if(-not $record.Contains('phase') -or $record.phase -cne 'Work') { continue }
                    $pendingId=$record.attempt_id
                    $oldDirectory=Join-Path $script:HomePath "runs/$($previous.run_id)/$($record.attempt_id)"
                    try { $beforeHash=(Read-Json (Join-Path $oldDirectory 'before.json')).manifest } catch { }
                    break
                }
                $script:State.uncertain+=@(@{attempt_id=$pendingId;phase='Work';targets=@($previous.verify_targets);directory=$oldDirectory;before_manifest=$beforeHash;input_manifest=$null;signature=$null;started_utc=$previous.heartbeat_utc;process_receipt=(Join-Path $oldDirectory 'worker/process.json')})
            }
            if($badState) { $script:State.message='旧状態は破損。旧記録を保持し、現物から再計画する。' }
        }
        $script:State.running=$true; $started=$true
        if($script:State.active) { $script:State.uncertain+=@($script:State.active); $script:State.active=$null }
        $wasComplete=$Resume -and $script:State.status -ceq 'Complete'
        if($script:State.status -cne 'Stalled') { $script:State.status='Running'; $script:State.phase='Plan' }
        Save-State
        $script:Activity='Initializing'
        Write-RunEvent "$(if($Resume) { 'Resume' } else { 'Start' }) | session_started=$($script:SessionStarted.ToString('yyyy-MM-dd HH:mm:ss zzz')) | run=$($script:State.run_id)"
        Write-RunEvent "Project: $Root"
        Write-RunEvent "Limits: run=$($Settings.MaxRunMinutes)m | phase=$($Settings.PhaseTimeoutMinutes)m | reserve=$($Settings.SaveReserveMinutes)m | PLAN regeneration every $($Settings.PlanRegenerationInterval) Plan attempts"
        Write-RunEvent "Records: $script:HomePath"
        Write-RunStatus
        Complete-PlanPublication
        $plan=Read-Plan $Root
        $script:Tick={
            if($script:RunClock.Elapsed.TotalSeconds-$script:LastHeartbeat -ge 20) { Save-State }
            if($script:RunClock.Elapsed.TotalSeconds-$script:LastStatus -ge 60) { Write-RunStatus }
            if((Get-Remaining) -le 0) { throw [TimeoutException]::new('Cumulative run deadline reached.') }
        }
        # CLI checks share the cumulative budget and heartbeat, including failures and resumed runs.
        $cliVersion=''
        $script:Activity='Checking CLI'
        if((Get-Remaining) -gt $Settings.SaveReserveMinutes*60 -or ($wasComplete -and (Get-Remaining) -gt 0)) {
            $preflight=Join-Path $script:HomePath ('runs/preflight-'+[guid]::NewGuid().ToString('N'))
            Invoke-Tick
            $versionResult=Invoke-Child $Settings.CodexCommand @('--version') $Root (Join-Path $preflight 'version') '' ([Math]::Min(20,(Get-Remaining))) $Settings.StopTimeoutSeconds
            if($versionResult.exit_code -ne 0 -or $versionResult.timed_out) { throw 'CLI version check failed.' }
            $cliVersion=[IO.File]::ReadAllText((Join-Path $preflight 'version/events.jsonl')).Trim()
            Invoke-Tick
            $helpResult=Invoke-Child $Settings.CodexCommand @('exec','--help') $Root (Join-Path $preflight 'help') '' ([Math]::Min(20,(Get-Remaining))) $Settings.StopTimeoutSeconds
            $help=[IO.File]::ReadAllText((Join-Path $preflight 'help/events.jsonl'))
            if($helpResult.exit_code -ne 0 -or $helpResult.timed_out) { throw 'CLI help check failed.' }
            foreach($flag in @('--json','--output-schema','--output-last-message','--ephemeral','--skip-git-repo-check')) { if(-not $help.Contains($flag)) { throw "CLI does not support $flag" } }
        }
        $script:Activity='Preparing phase'
        $logical=Read-Record $script:HomePath $script:State.logical_ref 'logical'
        foreach($uncertain in $script:State.uncertain) {
            $beforeValid=$true
            try {
                if(-not $uncertain.before_manifest) { throw 'Missing before manifest.' }
                $null=Read-Record $script:HomePath "manifests/$($uncertain.before_manifest).json" 'manifest'
            } catch { $beforeValid=$false }
            if(-not $beforeValid) {
                foreach($t in $logical.tasks) { if($t.status -ceq 'verified') { $t.status='pending'; $t.remaining=@('前記録不完全。現物から再検証する。') } }
                $script:State.history+=@(Save-Record $script:HomePath @{kind='incomplete-before';attempt_id=$uncertain.attempt_id;diff_complete=$false})
                Save-Logical $logical -Increment
            }
            if((Get-Remaining) -gt 0 -and $uncertain.directory.StartsWith($script:HomePath+[IO.Path]::DirectorySeparatorChar,$script:PathComparison)) {
                $afterPath=Join-Path $uncertain.directory 'after.json'
                if(-not (Test-Path -LiteralPath $afterPath)) { Save-Json $afterPath @{manifest=(Save-Manifest (New-Manifest (Get-Snapshot $Root) @('**')));recovered=$true} }
            }
        }
        while($script:State.status -ceq 'Running') {
            $script:WorkerClock=$null
            $script:Activity='Preparing phase'
            $stop=Get-StopStatus $script:State ($script:BaseElapsed+$script:RunClock.Elapsed.TotalSeconds)
            if($wasComplete -and (Get-Remaining) -gt 0) { $stop='Running' }
            if($stop -cne 'Running') { $script:State.status=$stop; break }
            if(-not $wasComplete -and (Get-Remaining) -le $Settings.SaveReserveMinutes*60) { $script:State.status='Paused'; break }
            $current=Read-Plan $Root
            if($current.data.project_id -cne $script:State.project_id) { throw 'project_id changed; use NewRun.' }
            if($current.hash -cne $script:State.plan_hash) {
                $script:State.history+=@(Save-Record $script:HomePath @{kind='plan-change';old_plan=$script:State.plan_ref;old_logical=$script:State.logical_ref;new_hash=$current.hash})
                $logical=New-Logical $current.data
                # Preserve unresolved findings as review obligations; old task references are historical.
                $old=Read-Record $script:HomePath $script:State.logical_ref 'logical'
                $logical.findings=@($old.findings | Where-Object { $_.status -ceq 'open' } | ForEach-Object { $f=Copy-Value $_; $f.task_ids=@(); $f })
                $script:State.plan_hash=$current.hash; $script:State.plan_ref=Save-Record $script:HomePath $current.data
                Save-Logical $logical -Increment; $script:State.phase='Plan'; $script:State.approval_key=$null; $script:State.complete_signature=$null
                Save-State
            }
            $plan=$current
            $directory=Join-Path $script:HomePath "runs/$($script:State.run_id)/$([guid]::NewGuid().ToString('N'))"
            $null=[IO.Directory]::CreateDirectory($directory)
            $frameworkSnapshot=Get-Snapshot $script:FrameRoot -Distribution
            $frameworkHash=Get-ObjectHash $frameworkSnapshot
            $before=Get-Snapshot $Root
            $beforeProbe=Get-ProtectedManifest $Root $plan.data $before 'Probe'
            if($plan.data.environment_checks.Count) {
                $script:State.active=@{attempt_id=[IO.Path]::GetFileName($directory);phase=$script:State.phase;targets=@();directory=$directory;before_manifest=(Save-Manifest (New-Manifest $before @('**')));input_manifest=$null;signature=$null;started_utc=[datetime]::UtcNow.ToString('O');process_receipt=(Join-Path $directory 'probe-0/process.json')}
                Save-State
            }
            $seconds=[Math]::Min((Get-Remaining),$Settings.PhaseTimeoutMinutes*60)
            $environment=Get-Environment $plan.data $Root $directory $seconds $cliVersion
            $snapshot=Get-Snapshot $Root
            Assert-ProtectedChanges $beforeProbe (Get-ProtectedManifest $Root $plan.data $snapshot 'Probe') $directory 'Probe'
            if($script:State.active) { $script:State.active=$null; Save-State }
            $Context=New-WorkerInput $plan $logical $environment $frameworkHash $snapshot $directory ([Math]::Min((Get-Remaining),$Settings.PhaseTimeoutMinutes*60))
            if(Update-StaleEvidence $logical $Context.signature $script:HomePath $snapshot) {
                Save-Logical $logical -Increment
                if($script:State.phase -ceq 'Verify' -and $script:State.pending_work) { $script:State.pending_route='replan' }
                else { $script:State.phase='Plan' }
                $script:State.approval_key=$null; Save-State
                $Context=New-WorkerInput $plan $logical $environment $frameworkHash $snapshot $directory ([Math]::Min((Get-Remaining),$Settings.PhaseTimeoutMinutes*60))
            }
            Assert-ProofFiles $logical $Context.signature
            if($wasComplete) {
                $wasComplete=$false
                if(Test-CompletionProofs $script:State $logical $plan.data $Context $snapshot $script:HomePath) { $script:State.status='Complete'; break }
                # Missing completion proof requires a new audit, even if task evidence is unchanged.
                $script:State.complete_signature=$null; $script:State.last_audit_key=$null
                Save-State
            }
            $auditKey=Get-AuditKey $plan.hash $logical $Context.signature
            if($Context.phase -ceq 'CompletionAudit' -and $script:State.last_audit_key -ceq $auditKey) {
                Set-PlanReturn $script:State; Save-State; continue
            }
            if($Context.phase -ceq 'Work') {
                Assert-ExecutionPlan $logical.execution_plan $logical $plan.data $Root $Settings.MaxTasksPerWork
                $key=Get-ObjectHash @{signature=$Context.signature;execution=$logical.execution_plan;findings=$logical.findings}
                if($script:State.approval_key -cne $key -or $script:State.uncertain.Count -or $script:State.pending_work) { throw 'Work has no valid Audit approval or has uncertain/unverified outcomes.' }
                Assert-ApprovalProofs $script:State $logical $Context $script:HomePath
            }
            if($Context.phase -ceq 'Verify' -and -not $Context.targets.Count) { throw 'Verify has no target.' }
            if($script:State.attempts -ge $Settings.MaxPhaseAttempts -or (Get-Remaining) -le $Settings.SaveReserveMinutes*60) { $script:State.status='Paused'; break }
            if($Context.regenerate_plan -and -not $Context.original_prompt) {
                $script:State.plan_regeneration_pending=$true; $script:State.status='NeedsInput'
                $script:State.message='PLAN regeneration requires a saved original user prompt (text block under ユーザープロンプト（原文） / User prompt (original)).'
                break
            }
            Write-WorkerPrompt $Context $directory
            $manifest=New-Manifest $snapshot @('**')
            $beforeHash=Save-Manifest $manifest
            Save-Json (Join-Path $directory 'before.json') @{manifest=$beforeHash}
            $script:State.active=@{attempt_id=$Context.attempt_id;phase=$Context.phase;targets=$Context.targets;directory=$directory;before_manifest=$beforeHash;input_manifest=$Context.input_manifest;signature=$Context.signature;started_utc=[datetime]::UtcNow.ToString('O');process_receipt=(Join-Path $directory 'worker/process.json')}
            if($Context.phase -ceq 'Work') { $script:State.counters.workless_plan_returns=0; $script:State.counters.audit_revisions=0; $script:State.counters.work_since_plan=$true }
            $script:State.attempts++
            if($Context.phase -ceq 'Plan') {
                $script:State.plan_attempts++
                if($Context.regenerate_plan) { $script:State.plan_regeneration_pending=$true }
            }
            Save-State
            $script:Activity='Worker running'
            $script:WorkerClock=[Diagnostics.Stopwatch]::StartNew()
            Write-RunEvent "$($script:State.attempts): $($Context.phase) | targets=$(if($Context.targets.Count) { $Context.targets -join ',' } else { '-' }) | deadline=$([datetimeoffset]::Parse($Context.deadline_utc).ToLocalTime().ToString('HH:mm:ss zzz'))"
            Write-RunStatus
            Write-RunEvent "Trial: $directory"
            $childResult=$null
            try {
                $childResult=Invoke-Worker $Settings $Context $directory ([Math]::Min((Get-Remaining),$Settings.PhaseTimeoutMinutes*60))
                Write-RunEvent "Worker exited | phase=$($Context.phase) | duration=$(Format-RunDuration $childResult.seconds) | exit=$($childResult.exit_code) | timed_out=$($childResult.timed_out)"
                Save-Json (Join-Path $directory 'metrics.json') @{phase=$Context.phase;exit_code=$childResult.exit_code;timed_out=$childResult.timed_out;seconds=$childResult.seconds}
            } finally {
                $script:WorkerClock.Stop()
                $script:Activity='Checking result'
                # Even a missing/invalid result leaves a durable before/after record when time allows.
                if((Get-Remaining) -gt 0) {
                    $oldTick=$script:Tick; $cancelled=[Autoframe.Cancellation]::Requested
                    $cleanupClock=[Diagnostics.Stopwatch]::StartNew()
                    if($null -eq $childResult -or $childResult.timed_out -or $childResult.exit_code -ne 0 -or $cancelled) {
                        [Autoframe.Cancellation]::Requested=$false
                        $script:Tick={
                            if($cleanupClock.Elapsed.TotalSeconds -ge $Settings.StopTimeoutSeconds) { throw [TimeoutException]::new('After-manifest recovery deadline reached.') }
                            & $oldTick
                        }
                    }
                    try {
                        $after=Get-Snapshot $Root
                        Save-Json (Join-Path $directory 'after.json') @{manifest=(Save-Manifest (New-Manifest $after @('**')))}
                    } finally { $script:Tick=$oldTick; [Autoframe.Cancellation]::Requested=$cancelled -or [Autoframe.Cancellation]::Requested }
                }
            }
            if($childResult.timed_out) { throw [TimeoutException]::new('Worker phase deadline reached.') }
            if($childResult.exit_code -ne 0) { throw "Worker exit code $($childResult.exit_code). See $directory" }
            if($Context.phase -cin @('Verify','CompletionAudit')) {
                $checkedEnvironment=Get-Environment $plan.data $Root (Join-Path $directory 'environment-after') ([Math]::Min((Get-Remaining),$Settings.PhaseTimeoutMinutes*60)) $cliVersion
                if((Get-ObjectHash $checkedEnvironment) -cne (Get-ObjectHash $environment)) { throw 'Verification environment changed during the phase.' }
                $after=Get-Snapshot $Root
            }
            $latest=Read-Plan $Root
            Assert-ProtectedChanges (Get-ProtectedManifest $Root $plan.data $snapshot $Context.phase) (Get-ProtectedManifest $Root $plan.data $after $Context.phase) $directory $Context.phase $frameworkSnapshot (Get-Snapshot $script:FrameRoot -Distribution)
            if($latest.hash -cne $Context.input_plan_hash) {
                $script:State.uncertain+=@($script:State.active); $script:State.active=$null; Set-PlanReturn $script:State; Save-State; continue
            }
            $afterInput=Get-InputManifest $Root $plan.data $after
            if($Context.phase -cne 'Work' -and (Get-ObjectHash $afterInput) -cne $Context.input_manifest) { throw 'Verification/planning input changed during the phase.' }
            $result=Read-Json (Join-Path $Context.output_directory 'result.json') 'result'
            Assert-ResultEnvelope $result $script:State $Context
            Assert-DependencyPreflight $Context
            if($Context.phase -ceq 'Work') {
                $allowed=@($logical.execution_plan.tasks | ForEach-Object { $_.edit_scope })+@(Get-GeneratedScope $plan.data)
                $oldMap=@{}; foreach($e in $snapshot) { $oldMap[$e.path]=$e }
                $newMap=@{}; foreach($e in $after) { $newMap[$e.path]=$e }
                foreach($p in @(@($oldMap.Keys)+@($newMap.Keys) | Sort-Object -Unique)) {
                    if((Get-ObjectHash $oldMap[$p]) -cne (Get-ObjectHash $newMap[$p]) -and (($oldMap[$p] -and $oldMap[$p].kind -ceq 'file') -or ($newMap[$p] -and $newMap[$p].kind -ceq 'file')) -and -not (Test-InScope $p $allowed)) { throw "Work exceeded edit_scope: $p" }
                }
            }
            $acceptContext=Copy-Value $Context
            $artifactScopes=@($logical.tasks | ForEach-Object { $_.deliverables } | Sort-Object -CaseSensitive -Unique)
            $acceptContext.artifact_manifest=Save-Manifest (New-Manifest $after $artifactScopes -FilesOnly)
            $updated=Apply-Result $logical $result $plan.data $acceptContext $Root $script:HomePath
            $regenerated=$null
            if($Context.regenerate_plan -and $result.decision -cin @('ready','recover')) {
                $regenerated=Read-RegeneratedPlan $plan $updated $Context $Root
                if($regenerated.hash -cne $plan.hash) {
                    $null=Update-StaleEvidence $updated 'PLAN regenerated; verification required' $script:HomePath $after
                }
            }
            if($Context.phase -cin @('Prepare','Audit') -and $null -ne $result.execution_plan) { Assert-ExecutionPlan $result.execution_plan $updated $plan.data $Root $Settings.MaxTasksPerWork }
            $candidate=Copy-Value $script:State
            if($Context.phase -ceq 'Plan' -and $candidate.pending_work -and $result.decision -ceq 'ready') { throw 'Pending Work must pass Verify before another Work.' }
            if($Context.phase -ceq 'Plan' -and $script:State.uncertain.Count -and $result.decision -ceq 'ready') {
                $ids=@($script:State.uncertain | ForEach-Object { $_.targets } | Sort-Object -Unique -CaseSensitive)
                $known=New-IdMap $updated.tasks
                $reported=@($result.task_results | ForEach-Object { $_.id })
                foreach($id in $ids) { if($known.ContainsKey($id) -and $id -cnotin $reported) { throw 'Plan must explicitly return uncertain targets to pending/blocked or recover them.' } }
                $candidate.uncertain=@()
            }
            if($Context.phase -ceq 'Verify' -and $result.decision -cne 'needs_input') { $candidate.uncertain=@(); $candidate.verify_targets=@() }
            if($Context.phase -ceq 'CompletionAudit' -and $result.decision -ceq 'complete' -and ($script:State.uncertain.Count -or $script:State.pending_work)) { throw 'Uncertain/unverified work prevents completion.' }
            if($Context.phase -ceq 'CompletionAudit' -and $result.decision -ceq 'complete') {
                # The final audit can regenerate outputs excluded from the input signature. Recheck task proofs too.
                $finalSnapshot=Get-Snapshot $Root
                $finalPlan=Read-Plan $Root
                if($finalPlan.hash -cne $Context.input_plan_hash) {
                    $script:State.uncertain+=@($script:State.active); $script:State.active=$null
                    Set-PlanReturn $script:State; Save-State; continue
                }
                $finalInput=Get-ObjectHash (Get-InputManifest $Root $plan.data $finalSnapshot)
                $finalSignature=Get-Signature $finalPlan.hash $updated $finalInput $environment (Get-FrameworkHash)
                $proofCheck=Copy-Value $updated
                if($finalSignature -cne $Context.signature -or (Update-StaleEvidence $proofCheck $finalSignature $script:HomePath $finalSnapshot)) { throw 'Completion evidence changed; recover through Verify.' }
                Assert-ProofFiles $updated $finalSignature
            }
            $meaningful=$false
            if($Context.phase -ceq 'Verify') {
                $meaningful=Test-MeaningfulProgress $logical $result
            }
            $acceptedRef=Save-Record $script:HomePath $result
            $logicalRef=Save-Record $script:HomePath $updated
            if($logicalRef -cne $candidate.logical_ref) { $candidate.logical_ref=$logicalRef; $candidate.base_plan_version++ }
            if($Context.phase -ceq 'Work') { $candidate.verify_targets=@($Context.targets) }
            if($Context.phase -ceq 'Plan' -and $result.decision -ceq 'recover') {
                $candidate.verify_targets=@(@($candidate.verify_targets)+@($result.task_results | ForEach-Object { $_.id }) | Sort-Object -CaseSensitive -Unique)
            }
            $recoveryProgress=$Context.phase -ceq 'Verify' -and (Test-MeaningfulProgress $logical $result -CompletionOnly)
            $candidate=Get-Transition $candidate $result $updated $meaningful (Get-AuditKey $plan.hash $updated $Context.signature) $recoveryProgress
            $candidate.accepted_attempts+=@($Context.attempt_id); $candidate.history+=@($acceptedRef); $candidate.active=$null
            if($Context.phase -ceq 'Audit' -and $result.decision -ceq 'approved') { $candidate.approval_key=Get-ObjectHash @{signature=$Context.signature;execution=$updated.execution_plan;findings=$updated.findings} }
            if($candidate.status -ceq 'Complete') { $candidate.complete_signature=$Context.signature }
            if($Context.phase -ceq 'Verify' -and $candidate.phase -ceq 'Prepare') {
                $milestoneKeys=@(Get-NewMilestoneKeys $updated $Context.signature $candidate.milestone_keys)
                if($milestoneKeys.Count) { $candidate.milestone_keys+=@($milestoneKeys); $candidate.phase='CompletionAudit' }
            }
            if($null -ne $regenerated) {
                $candidate.plan_regeneration_pending=$false
                $candidate.history+=@(Save-Record $script:HomePath @{kind='plan-regeneration';attempt_id=$Context.attempt_id;plan_attempt=$candidate.plan_attempts;old_hash=$plan.hash;new_hash=$regenerated.hash;original_prompt=$Context.original_prompt;old_text_ref=(Save-Record $script:HomePath @{text=$regenerated.old_text});new_text_ref=(Save-Record $script:HomePath @{text=$regenerated.text})})
                if($regenerated.hash -cne $plan.hash) {
                    $candidate.plan_ref=Save-Record $script:HomePath $regenerated.data; $candidate.plan_hash=$regenerated.hash
                    $candidate.plan_publication=@{old_hash=$plan.hash;new_hash=$regenerated.hash;text_ref=(Save-Record $script:HomePath @{text=$regenerated.text})}
                    Set-PlanReturn $candidate
                    $candidate.status=Get-StopStatus $candidate $candidate.elapsed_seconds
                    $candidate.approval_key=$null; $candidate.complete_signature=$null; $candidate.last_audit_key=$null; $candidate.milestone_keys=@()
                }
            }
            $candidate.message=$result.summary; Save-State $candidate
            Complete-PlanPublication
            if($null -ne $regenerated) { [Console]::WriteLine("PLAN regenerated from original prompt at Plan $($candidate.plan_attempts).") }
            $logical=$updated
            Write-RunEvent "Accepted | phase=$($Context.phase) | decision=$($result.decision) | verified=$(@($logical.tasks | Where-Object { $_.status -ceq 'verified' }).Count)/$(@($logical.tasks | Where-Object { $_.status -cne 'superseded' }).Count) | next=$($script:State.phase)"
            $script:Activity='Preparing phase'
            if($Context.phase -ceq 'Verify') {
                foreach($milestone in (Get-MilestoneProgress $logical)) {
                    $suffix=if($milestone.ready_for_audit) { ' (audit candidate)' } else { '' }
                    [Console]::WriteLine("Milestone $($milestone.id): $($milestone.verified_count)/$($milestone.total_tasks) verified$suffix")
                }
            }
        }
    } catch {
        if(-not $started) { throw }
        $cause=$_.Exception.GetBaseException()
        $script:State.status=if($script:State.status -ceq 'Stalled') { 'Stalled' } elseif($cause -is [TimeoutException] -or $cause -is [OperationCanceledException] -or $cause -is [Management.Automation.PipelineStoppedException]) { 'Paused' } else { 'Error' }
        $script:State.message=$_.Exception.Message
        $script:State.history+=@(Save-Record $script:HomePath @{kind='error';message=$_.Exception.Message;stack=$_.ScriptStackTrace})
        [Console]::Error.WriteLine($script:State.message)
        [Console]::Error.WriteLine($_.ScriptStackTrace)
    } finally {
        $script:Tick=$null
        if($started) {
            try {
                Stop-OldChildren $script:HomePath $Settings.StopTimeoutSeconds
                if($script:State.active) { $script:State.uncertain+=@($script:State.active); $script:State.active=$null }
                $script:State.running=$false; Save-State
            } catch { $script:State.status='Error'; [Console]::Error.WriteLine("Recovery save failed: $($_.Exception.Message)") }
        }
        if($lock) { $lock.Dispose() }
        [Autoframe.Cancellation]::Unregister()
    }
    $script:Activity=$script:State.status
    Write-RunEvent "$($script:State.status): $($script:State.message)"
    Write-RunEvent "Finished | session_ended=$([datetimeoffset]::Now.ToString('yyyy-MM-dd HH:mm:ss zzz')) | session_duration=$(Format-RunDuration $script:RunClock.Elapsed.TotalSeconds)"
    Write-RunStatus
    return @{Complete=0;Paused=2;Blocked=3;NeedsInput=4;Stalled=5;Error=6}[$script:State.status]
}
