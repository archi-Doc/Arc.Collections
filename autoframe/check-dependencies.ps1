#requires -Version 7.4
# Run by the Worker in its own execution environment, never as a runner-side probe.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$DotnetCommand='dotnet'
)
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$report=@{status='blocked';command='dotnet nuget list source --format short';exit_code=$null;error=$null;release_condition=$null}
Push-Location -LiteralPath $ProjectRoot
try {
    try {
        $global:LASTEXITCODE=0
        $messages=@(& $DotnetCommand nuget list source --format short 2>&1)
        $ok=$?; $report.exit_code=$LASTEXITCODE
        if($ok -and $LASTEXITCODE -eq 0) { $report.status='ready' }
        else {
            $report.error=($messages | ForEach-Object { "$_" }) -join "`n"
            if([string]::IsNullOrWhiteSpace($report.error)) { $report.error="Dependency command failed with exit code $LASTEXITCODE." }
        }
    } catch { $report.error=$_.Exception.Message }
    if($report.status -ceq 'blocked') {
        $report.release_condition='Resolve the reported SDK/configuration access problem in the Worker environment while preserving NuGet sources and authentication; rerun this check and the planned restore. Do not change ACLs, replace configuration, or fall back to different sources automatically.'
    }
    # Source listings are not persisted on success. No restore, ACL or configuration changes here.
    [IO.File]::WriteAllText($OutputPath,($report | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
    Write-Output "Dependency preflight: $($report.status). Report: $OutputPath"
} finally { Pop-Location }
