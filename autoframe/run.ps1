#requires -Version 7.4
[CmdletBinding()]
param(
    [string]$ProjectRoot=(Get-Location).Path,
    [int]$MaxRunMinutes=480,
    [switch]$Resume,
    [switch]$NewRun,
    [int]$MaxPhaseAttempts=100,
    [int]$PhaseTimeoutMinutes=60,
    [int]$SaveReserveMinutes=5,
    [int]$StopTimeoutSeconds=60,
    [int]$MaxTasksPerWork=3,
    [int]$CompletionAuditInterval=3,
    [int]$PlanRegenerationInterval=3,
    [string]$CodexCommand='codex',
    [hashtable]$PhaseModels=@{},
    [switch]$ResetStallCounters,
    [string]$ResetReason
)
$ErrorActionPreference='Stop'
try {
    . (Join-Path $PSScriptRoot 'lib/core.ps1')
    . (Join-Path $PSScriptRoot 'lib/manifest.ps1')
    . (Join-Path $PSScriptRoot 'lib/transition.ps1')
    . (Join-Path $PSScriptRoot 'invoke-worker.ps1')
    . (Join-Path $PSScriptRoot 'lib/engine.ps1')
    $settings=@{}
    foreach($key in @('MaxRunMinutes','MaxPhaseAttempts','PhaseTimeoutMinutes','SaveReserveMinutes','StopTimeoutSeconds','MaxTasksPerWork','CompletionAuditInterval','PlanRegenerationInterval','CodexCommand','PhaseModels')) { $settings[$key]=Get-Variable -Name $key -ValueOnly }
    $code=Start-Frame $ProjectRoot $settings $PSBoundParameters -Resume:$Resume -NewRun:$NewRun -ResetStallCounters:$ResetStallCounters -ResetReason $ResetReason
    exit $code
} catch { [Console]::Error.WriteLine("autoframe Error: $($_.Exception.Message)"); exit 6 }
