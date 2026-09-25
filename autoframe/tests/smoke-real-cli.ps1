#requires -Version 7.4
[CmdletBinding()]
param([switch]$RunRealCli,[string]$CodexCommand='codex',[int]$TimeoutSeconds=60)
$ErrorActionPreference='Stop'
if(-not $RunRealCli) { Write-Host '実CLIを呼ぶ場合だけ -RunRealCli を指定してください。疑似Worker試験とは別です。'; exit 0 }
. (Join-Path $PSScriptRoot '../invoke-worker.ps1')
$root=Join-Path ([IO.Path]::GetTempPath()) ('autoframe-cli-smoke-'+[guid]::NewGuid().ToString('N'))
$null=[IO.Directory]::CreateDirectory($root)
$schema=Join-Path $script:FrameRoot 'schemas/result.schema.json'; $prompt=Join-Path $root 'prompt.txt'; $result=Join-Path $root 'result.json'
$expected=@{schema_version=1;run_id='smoke';attempt_id=[guid]::NewGuid().ToString('N');phase='Plan';input_plan_hash=('0'*64);base_plan_version=0;execution_plan_hash=$null;execution_plan=$null;decision='needs_input';route_hint=$null;summary='Connectivity test only';task_results=@();changes=@();findings=@();evidence=@();progress=@()}
[IO.File]::WriteAllText($prompt,'Do not use tools or change files. This is a connectivity and structured-output smoke test, not a project run. Return exactly this JSON: '+(ConvertTo-Canonical $expected))
$command=Resolve-Cli $CodexCommand
try {
    $version=Invoke-Child $command @('--version') $root (Join-Path $root 'version') '' 15 5
    if($version.exit_code -ne 0) { throw 'CLI version failed.' }
    $r=Invoke-Child $command @('exec','--skip-git-repo-check','--ephemeral','--json','--sandbox','read-only','--output-schema',$schema,'-o',$result,'-C',$root,'-') $root (Join-Path $root 'exec') $prompt $TimeoutSeconds 5
    if($r.timed_out -or $r.exit_code -ne 0) { throw "CLI failed/timed out (exit $($r.exit_code)); inspect $root" }
    $value=Read-Json $result 'result'
    if((Get-ObjectHash $value) -cne (Get-ObjectHash $expected)) { throw 'Structured output mismatch.' }
    Save-Json (Join-Path $root 'verification.json') @{status='passed';version=[IO.File]::ReadAllText((Join-Path $root 'version/events.jsonl')).Trim();structured_output=$true;children_stopped=$true;production_run=$false}
    Write-Host "PASS real CLI smoke: $root"
} catch {
    Save-Json (Join-Path $root 'verification.json') @{status='unverified';reason=$_.Exception.Message;production_run=$false}
    [Console]::Error.WriteLine("UNVERIFIED real CLI: $($_.Exception.Message)`nLogs: $root")
    exit 6
}
