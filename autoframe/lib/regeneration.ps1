# PLAN publication is journaled in state before the runner replaces the file.
function Initialize-PlanRegeneration($State,[string]$HomePath) {
    if(-not $State.settings.Contains('PlanRegenerationInterval')) { $State.settings.PlanRegenerationInterval=3 }
    if(-not $State.Contains('plan_attempts')) {
        $runPath=Join-Path $HomePath "runs/$($State.run_id)"
        $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($ref in $State.history) {
            $record=Read-Record $HomePath $ref
            if($record.Contains('phase') -and $record.phase -ceq 'Plan' -and $record.run_id -ceq $State.run_id) { $null=$ids.Add($record.attempt_id) }
        }
        foreach($attempt in @($State.uncertain)+@($State.active)) {
            if($null -ne $attempt -and $attempt.phase -ceq 'Plan' -and [IO.Path]::GetFullPath($attempt.directory).StartsWith($runPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { $null=$ids.Add($attempt.attempt_id) }
        }
        if(Test-Path -LiteralPath $runPath) {
            foreach($dir in Get-ChildItem -LiteralPath $runPath -Directory) {
                $inputPath=Join-Path $dir.FullName 'input.json'
                # Ignore input drafts never launched or durably charged to state.
                if((Test-Path -LiteralPath $inputPath) -and ((Test-Path -LiteralPath (Join-Path $dir.FullName 'metrics.json')) -or (Test-Path -LiteralPath (Join-Path $dir.FullName 'worker/process.json')))) {
                    $inputRecord=Read-Json $inputPath
                    if($inputRecord.phase -ceq 'Plan') { $null=$ids.Add($inputRecord.attempt_id) }
                }
            }
        }
        $State.plan_attempts=$ids.Count
    }
    if(-not $State.Contains('plan_regeneration_pending')) { $State.plan_regeneration_pending=$false }
    if(-not $State.Contains('plan_publication')) { $State.plan_publication=$null }
}
function Test-PlanRegenerationDue($State) {
    $State.phase -ceq 'Plan' -and ($State.plan_regeneration_pending -or (($State.plan_attempts+1) % $State.settings.PlanRegenerationInterval -eq 0))
}
function Get-OriginalPrompt([string]$Text) {
    # Headings inside the text fence are part of the user's verbatim prompt.
    $block=[regex]::Match($Text,'(?ms)^##[ \t]+(?:ユーザープロンプト（原文）|User prompt \(original\))[ \t]*\r?\n(?:(?!^##[ \t]|<!-- autoframe:begin -->).)*?^```text[ \t]*\r?\n(?<text>.*?)^```[ \t]*\r?$')
    if(-not $block.Success -or [string]::IsNullOrWhiteSpace($block.Groups['text'].Value)) { return $null }
    $block.Groups['text'].Value
}
function Read-RegeneratedPlan($Plan,$Logical,$Context,[string]$Root) {
    $path=Join-Path $Root 'PLAN.md'; $raw=[IO.File]::ReadAllText($path)
    if([Autoframe.Json]::FileHash($path) -cne $Plan.hash) { throw 'PLAN changed before regeneration acceptance.' }
    if(-not (Get-OriginalPrompt $raw)) { throw 'Original user prompt is missing.' }
    $data=Read-Json (Join-Path $Context.output_directory 'regenerated-plan.json') 'plan'
    foreach($key in @('generated_scope','environment_checks','references','tasks','milestones')) { if(-not $data.Contains($key)) { $data[$key]=@() } }
    foreach($key in @(@($Plan.data.Keys)+@($data.Keys) | Sort-Object -Unique)) {
        if($key -cnotin @('tasks','milestones') -and (Get-ObjectHash $data[$key]) -cne (Get-ObjectHash $Plan.data[$key])) { throw "Regeneration changed a user requirement: $key" }
    }
    Assert-Graph $data.tasks $data.completion_criteria $data.milestones
    Assert-PlanScopes $Root $data
    if(-not $data.tasks.Count -or -not $data.milestones.Count) { throw 'Regeneration requires tasks and milestones.' }
    $covered=@($data.milestones | ForEach-Object { if(-not $_.task_ids.Count) { throw 'Empty regenerated milestone.' }; $_.task_ids })
    foreach($task in $data.tasks) { if($task.required -and $task.id -cnotin $covered) { throw "Milestone coverage missing: $($task.id)" } }
    # Preserve existing user IDs, conditions, dependency ordering and milestone membership.
    $comparison=New-Logical $data; $existing=New-IdMap $Plan.data.tasks
    foreach($task in $comparison.tasks) { if(-not $existing.ContainsKey($task.id)) { $task.source_ids=@() } }
    Assert-Logical $comparison $Plan.data
    foreach($task in $Logical.tasks) { if($task.id -cin @($data.tasks.id) -and $task.id -cnotin $task.source_ids) { $task.source_ids+=@($task.id) } }
    Assert-Logical $Logical $data
    $text=$raw
    if((Get-ObjectHash $data) -cne (Get-ObjectHash $Plan.data)) {
        $match=[regex]::Match($raw,'(?s)<!-- autoframe:begin -->\s*```json\s*\r?\n(.*?)\r?\n```\s*<!-- autoframe:end -->')
        $json=$match.Groups[1]
        $text=$raw.Substring(0,$json.Index)+(ConvertTo-Json $data -Depth 100)+$raw.Substring($json.Index+$json.Length)
    }
    @{data=$data;text=$text;old_text=$raw;hash=$(if($text -ceq $raw) { $Plan.hash } else { [Autoframe.Json]::Hash($text) })}
}
function Complete-PlanPublication {
    $publication=$script:State.plan_publication
    if($null -eq $publication) { return }
    $path=Join-Path $script:State.project_root 'PLAN.md'
    $text=(Read-Record $script:HomePath $publication.text_ref).text
    if([Autoframe.Json]::Hash($text) -cne $publication.new_hash) { throw 'Corrupt PLAN publication text.' }
    $current=[Autoframe.Json]::FileHash($path)
    if($current -cne $publication.new_hash) {
        if($current -cne $publication.old_hash) { throw 'PLAN publication conflicts with an external edit; saved plan was not overwritten.' }
        [Autoframe.Json]::Atomic($path,$text)
    }
    $cleared=Copy-Value $script:State; $cleared.plan_publication=$null
    Save-State $cleared
}
