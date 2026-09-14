# Dot-source this adapter. No project-specific commands or default permission escalation.
. (Join-Path $PSScriptRoot 'lib/core.ps1')
function Resolve-Cli([string]$Command) {
    $resolved=Get-Command -Name $Command -ErrorAction Stop | Select-Object -First 1
    if($resolved.CommandType -notin @('Application','ExternalScript')) { throw 'CodexCommand must be an executable or launcher; aliases/functions are rejected.' }
    $resolved.Source
}
function Invoke-Child([string]$Command,[string[]]$Arguments,[string]$Root,[string]$Directory,[string]$PromptPath,[double]$TimeoutSeconds,[int]$StopSeconds=60) {
    $null=[IO.Directory]::CreateDirectory($Directory)
    $invocation=Join-Path $Directory 'invocation.json'; $gate=Join-Path $Directory 'release'
    Save-Json $invocation @{command=$Command;arguments=@($Arguments);root=$Root;prompt_path=$PromptPath;gate=$gate}
    $jobName='Local\autoframe-'+[guid]::NewGuid().ToString('N')
    $receipt=Join-Path $Directory 'process.json'
    # Save ownership before spawning, so a crash in Process.Start is still discoverable.
    Save-Json $receipt @{job=$jobName;pid=$null;start_utc=$null;owner_pid=$PID;owner_start=(Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('O');stopped=$false}
    $child=$null; $timer=[Diagnostics.Stopwatch]::StartNew(); $stopped=$false; $timedOut=$false
    try {
        $pwsh=(Get-Command pwsh -CommandType Application | Select-Object -First 1).Source
        $child=[Autoframe.Child]::new($pwsh,[string[]]@('-NoProfile','-NonInteractive','-File',(Join-Path $script:FrameRoot 'lib/launcher.ps1'),'-InvocationPath',$invocation),$Root,(Join-Path $Directory 'events.jsonl'),(Join-Path $Directory 'stderr.log'),$jobName)
        $record=@{job=$jobName;pid=$child.Id;start_utc=$child.StartUtc;owner_pid=$PID;owner_start=(Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('O');stopped=$false}
        Save-Json $receipt $record
        [Autoframe.Json]::Atomic($gate,'go')
        while(-not $child.Exited) {
            Invoke-Tick
            if($timer.Elapsed.TotalSeconds -ge $TimeoutSeconds) { $timedOut=$true; break }
            $child.Flush(); Start-Sleep -Milliseconds 100
        }
        if($timedOut) { $child.Stop() }
        else {
            # Job accounting can lag the root exit notification briefly.
            $exitGrace=[Diagnostics.Stopwatch]::StartNew()
            while($child.ActiveCount -gt 0 -and $exitGrace.Elapsed.TotalMilliseconds -lt 500) { Start-Sleep -Milliseconds 25 }
            if($child.ActiveCount -gt 0) { $child.Stop(); throw 'CLI exited with live descendants; result is not accepted.' }
        }
        $stopTimer=[Diagnostics.Stopwatch]::StartNew()
        while($child.ActiveCount -gt 0 -or -not $child.Drained) {
            if($stopTimer.Elapsed.TotalSeconds -ge $StopSeconds) { throw 'Process stop/pipe drain could not be confirmed.' }
            Start-Sleep -Milliseconds 50
        }
        $child.Finish(); $stopped=$true
        $record.stopped=$true; Save-Json $receipt $record
        @{exit_code=$child.ExitCode;timed_out=$timedOut;seconds=$timer.Elapsed.TotalSeconds;receipt=$receipt}
    } finally {
        try {
            if($child -and -not $stopped) {
                $child.Stop(); $stopTimer=[Diagnostics.Stopwatch]::StartNew()
                while($child.ActiveCount -gt 0 -and $stopTimer.Elapsed.TotalSeconds -lt $StopSeconds) { Start-Sleep -Milliseconds 50 }
                if($child.ActiveCount -eq 0) { $record.stopped=$true; Save-Json $receipt $record }
            }
        } finally { if($child) { $child.Dispose() } }
    }
}
function Stop-OldChildren([string]$HomePath,[int]$StopSeconds) {
    $runs=Join-Path $HomePath 'runs'
    if(-not (Test-Path -LiteralPath $runs)) { return }
    $timer=[Diagnostics.Stopwatch]::StartNew()
    foreach($file in Get-ChildItem -LiteralPath $runs -Filter process.json -Recurse -File) {
        $r=Read-Json $file.FullName
        if($r.stopped) { continue }
        if($r.job -cnotmatch '^Local\\autoframe-[a-f0-9]{32}$') { throw "Cannot identify old job: $($file.FullName)" }
        $p=$null
        if($null -ne $r.pid) {
            $p=Get-Process -Id $r.pid -ErrorAction SilentlyContinue
            if($p -and $p.StartTime.ToUniversalTime().ToString('O') -cne $r.start_utc) {
                # Reused PID is deliberately left alone. The unique job, not that process, owns the old children.
                $p.Dispose(); $p=$null
            }
        }
        try {
            while(-not [Autoframe.Child]::RecoverJob($r.job)) {
                if($timer.Elapsed.TotalSeconds -ge $StopSeconds) { throw 'Old process tree could not be stopped.' }
                Start-Sleep -Milliseconds 50
            }
            # A missing job alone is not proof that the identified launcher has exited.
            if($p -and -not $p.WaitForExit([int][Math]::Max(0,($StopSeconds-$timer.Elapsed.TotalSeconds)*1000))) { throw 'Old launcher exit could not be confirmed.' }
        } finally { if($p) { $p.Dispose() } }
        $r.stopped=$true; Save-Json $file.FullName $r
    }
}
function Invoke-Worker($Settings,$Context,[string]$Directory,[double]$Seconds) {
    $promptPath=Join-Path $Directory 'prompt.md'; $output=Join-Path $Context.output_directory 'result.json'
    $args=@('exec','--skip-git-repo-check','--ephemeral','--json','--color','never','--output-schema',(Join-Path $script:FrameRoot 'schemas/result.schema.json'),'-o',$output,'-C',$Context.project_root)
    if($Settings.PhaseModels.Contains($Context.phase)) { $args+=@('--model',$Settings.PhaseModels[$Context.phase]) }
    $args+=@('-')
    Invoke-Child $Settings.CodexCommand $args $Context.project_root (Join-Path $Directory 'worker') $promptPath $Seconds $Settings.StopTimeoutSeconds
}
