#requires -Version 7.4
param([Parameter(Mandatory)][string]$InvocationPath)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$OutputEncoding=[Console]::InputEncoding=[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
try {
    $document=[Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText($InvocationPath))
    try {
        $node=$document.RootElement
        $invocation=@{}
        foreach($key in @('gate','root','command','prompt_path')) { $invocation[$key]=$node.GetProperty($key).GetString() }
        $invocation.arguments=@(foreach($item in $node.GetProperty('arguments').EnumerateArray()) { $item.GetString() })
    } finally { $document.Dispose() }
    $timer=[Diagnostics.Stopwatch]::StartNew()
    while(-not [IO.File]::Exists($invocation.gate)) {
        if($timer.Elapsed.TotalSeconds -gt 30) { throw 'Parent did not release launcher gate.' }
        Start-Sleep -Milliseconds 25
    }
    Set-Location -LiteralPath $invocation.root
    $arguments=@($invocation.arguments)
    $global:LASTEXITCODE=0
    if($invocation.prompt_path) {
        [IO.File]::ReadAllText($invocation.prompt_path) | & $invocation.command @arguments
    } else { & $invocation.command @arguments }
    $succeeded=$?
    if($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    if(-not $succeeded) { exit 1 }
    exit $LASTEXITCODE
} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
