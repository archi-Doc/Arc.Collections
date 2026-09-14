#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:FrameRoot = Split-Path $PSScriptRoot -Parent
if (-not ('Autoframe.Json' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'Native.cs') }
$script:Phases = @('Plan','Prepare','Audit','Work','Verify','CompletionAudit')
$script:Tick = $null
function Invoke-Tick {
    if([Autoframe.Cancellation]::Requested) { throw [OperationCanceledException]::new('User interruption requested.') }
    if ($script:Tick) { & $script:Tick }
}
function ConvertTo-Canonical($Value) { [Autoframe.Json]::Canonical((ConvertTo-Json -InputObject $Value -Depth 100 -Compress)) }
function Get-ObjectHash($Value) { [Autoframe.Json]::Hash((ConvertTo-Canonical $Value)) }
function Copy-Value($Value) { return ,([Autoframe.Json]::Parse((ConvertTo-Canonical $Value))) }
function Read-Json([string]$Path, [string]$Schema = '') {
    $raw = [IO.File]::ReadAllText($Path, [Text.UTF8Encoding]::new($false,$true))
    $canonical = [Autoframe.Json]::Canonical($raw)
    if ($Schema -and -not (Test-Json -Json $canonical -SchemaFile (Join-Path $script:FrameRoot "schemas/$Schema.schema.json") -ErrorAction Stop)) { throw "Schema: $Path" }
    [Autoframe.Json]::Parse($canonical)
}
function Assert-Schema($Value, [string]$Schema) {
    if (-not (Test-Json -Json (ConvertTo-Canonical $Value) -SchemaFile (Join-Path $script:FrameRoot "schemas/$Schema.schema.json") -ErrorAction Stop)) { throw "Invalid $Schema" }
}
function Save-Json([string]$Path, $Value) { [Autoframe.Json]::Atomic($Path,(ConvertTo-Canonical $Value)) }
function Save-Record([string]$HomePath, $Value, [string]$Directory = 'records') {
    $text = ConvertTo-Canonical $Value
    $hash = [Autoframe.Json]::Hash($text)
    $relative = "$Directory/$hash.json"
    $path = Join-Path $HomePath $relative
    if (Test-Path -LiteralPath $path) {
        if ([Autoframe.Json]::FileHash($path) -cne $hash) {
            # Regeneration can restore exactly the content addressed by this hash. Preserve the corrupt bytes.
            $archive=Join-Path $HomePath ("records/corrupt-$hash-"+[guid]::NewGuid().ToString('N')+'.raw')
            $null=[IO.Directory]::CreateDirectory((Join-Path $HomePath 'records'))
            [IO.File]::Copy($path,$archive,$false)
            [Autoframe.Json]::Atomic($path,$text)
        }
    } else { [Autoframe.Json]::Atomic($path,$text) }
    $relative
}
function Read-Record([string]$HomePath,[string]$Relative,[string]$Schema = '') {
    if ($Relative -cnotmatch '^(records|manifests)/([a-f0-9]{64})\.json$') { throw "Invalid record reference: $Relative" }
    $expected=$Matches[2]; $path=Join-Path $HomePath $Relative
    if ([Autoframe.Json]::FileHash($path) -cne $expected) { throw "Corrupt record: $Relative" }
    Read-Json $path $Schema
}
function New-IdMap($Items) {
    $map=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($item in $Items) {
        if (-not $map.TryAdd($item.id,$item)) { throw "Duplicate ID: $($item.id)" }
    }
    return ,$map
}
function Assert-Unique($Ids) {
    $set=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($id in $Ids) { if (-not $set.Add($id)) { throw "Duplicate ID: $id" } }
}
function Assert-Graph($Tasks,$Criteria,$Milestones) {
    $map=New-IdMap $Tasks; $criteriaMap=New-IdMap $Criteria; $null=New-IdMap $Milestones
    foreach ($task in $Tasks) {
        Assert-Unique $task.depends_on; Assert-Unique $task.criterion_ids
        foreach ($id in $task.depends_on) { if (-not $map.ContainsKey($id)) { throw "Unknown dependency: $id" } }
        foreach ($id in $task.criterion_ids) { if (-not $criteriaMap.ContainsKey($id)) { throw "Unknown criterion: $id" } }
    }
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $visiting=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Visit([string]$Id) {
        if ($seen.Contains($Id)) { return }
        if (-not $visiting.Add($Id)) { throw "Dependency cycle: $Id" }
        foreach ($dep in $map[$Id].depends_on) { Visit $dep }
        $null=$visiting.Remove($Id); $null=$seen.Add($Id)
    }
    foreach ($task in $Tasks) { Visit $task.id }
    foreach ($m in $Milestones) { Assert-Unique $m.task_ids; foreach ($id in $m.task_ids) { if (-not $map.ContainsKey($id)) { throw "Unknown milestone task: $id" } } }
}
function Read-Plan([string]$Root) {
    $path=Join-Path $Root 'PLAN.md'
    $bytes=[IO.File]::ReadAllBytes($path)
    $raw=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
    if ([regex]::Matches($raw,'<!-- autoframe:begin -->').Count -ne 1 -or [regex]::Matches($raw,'<!-- autoframe:end -->').Count -ne 1) { throw 'PLAN markers must occur exactly once.' }
    $match=[regex]::Match($raw,'(?s)<!-- autoframe:begin -->\s*```json\s*\r?\n(.*?)\r?\n```\s*<!-- autoframe:end -->')
    if (-not $match.Success) { throw 'PLAN requires one marked JSON block.' }
    $json=[Autoframe.Json]::Canonical($match.Groups[1].Value)
    $plan=[Autoframe.Json]::Parse($json)
    Assert-Schema $plan 'plan'
    foreach ($key in @('generated_scope','environment_checks','references','tasks','milestones')) { if (-not $plan.Contains($key)) { $plan[$key]=@() } }
    Assert-Graph $plan.tasks $plan.completion_criteria $plan.milestones
    Assert-PlanScopes $Root $plan
    @{ data=$plan; hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant() }
}
function New-Logical($Plan) {
    $tasks=@(foreach($t in $Plan.tasks) {
        $v=Copy-Value $t
        $v.source_ids=@($t.id); $v.deliverables=@(); $v.status='pending'; $v.evidence_refs=@(); $v.remaining=@(); $v.blocker=$null; $v.replacements=@(); $v
    })
    @{schema_version=1;tasks=$tasks;milestones=(Copy-Value @($Plan.milestones));findings=@();evidence=@();execution_plan=$null}
}
function Get-Definitions($Logical) {
    @(foreach ($t in $Logical.tasks) {
        $v=Copy-Value $t
        foreach($k in @('status','evidence_refs','remaining','blocker')) { $v.Remove($k) }
        $v
    })
}
function Get-RequiredIds($Logical) {
    $map=New-IdMap $Logical.tasks; $set=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Include([string]$Id) {
        if (-not $set.Add($Id)) { return }
        $t=$map[$Id]
        foreach($dep in $t.depends_on) { Include $dep }
        foreach($replacement in $t.replacements) { Include $replacement }
    }
    foreach($t in $Logical.tasks) { if($t.required) { Include $t.id } }
    @($set | Where-Object { $map[$_].status -cne 'superseded' })
}
function Assert-Logical($Logical,$Plan,[switch]$AllowIncomplete,[switch]$RequireMilestones) {
    Assert-Schema $Logical 'logical'
    Assert-Graph $Logical.tasks $Plan.completion_criteria $Logical.milestones
    $map=New-IdMap $Logical.tasks; $null=New-IdMap $Logical.findings; $null=New-IdMap $Logical.evidence
    $sources=New-IdMap $Plan.tasks
    function Get-CurrentIds([string]$Id) {
        if($map[$Id].status -ceq 'superseded') { return @($map[$Id].replacements) }
        return @($Id)
    }
    function Assert-Dependencies($Task,$RequiredIds) {
        # Splitting may introduce an intermediate task, but must retain the original ordering.
        $reachable=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        function Walk([string]$Id) {
            foreach($dep in $map[$Id].depends_on) { if($reachable.Add($dep)) { Walk $dep } }
        }
        Walk $Task.id
        foreach($id in $RequiredIds) {
            foreach($current in (Get-CurrentIds $id)) {
                if(-not $reachable.Contains($current)) { throw "Task loses dependency: $($Task.id) -> $current" }
            }
        }
    }
    foreach($t in $Logical.tasks) {
        foreach($s in $t.source_ids) { if(-not $sources.ContainsKey($s)) { throw "Unknown source task: $s" } }
        foreach($r in $t.replacements) {
            if(-not $map.ContainsKey($r) -or $r -ceq $t.id -or $map[$r].status -ceq 'superseded') { throw "Invalid replacement: $r" }
            if($t.required -and -not $map[$r].required) { throw 'Required replacement cannot be optional.' }
            foreach($s in $t.source_ids) { if($s -cnotin $map[$r].source_ids) { throw 'Replacement loses source task trace.' } }
            foreach($c in $t.criterion_ids) { if($c -cnotin $map[$r].criterion_ids) { throw 'Replacement loses a criterion.' } }
            Assert-Dependencies $map[$r] $t.depends_on
        }
        if(($t.status -ceq 'superseded') -ne ($t.replacements.Count -gt 0)) { throw 'Replacement/status mismatch.' }
        foreach($dep in $t.depends_on) { if($map[$dep].status -ceq 'superseded') { throw 'Dependency must be redirected to replacements.' } }
    }
    foreach($source in $Plan.tasks) {
        if(-not $map.ContainsKey($source.id)) { throw "User task omitted: $($source.id)" }
        $t=$map[$source.id]
        foreach($k in @('required','acceptance','verification')) { if((Get-ObjectHash $t[$k]) -cne (Get-ObjectHash $source[$k])) { throw "User condition changed: $($source.id).$k" } }
        if($source.id -cnotin $t.source_ids) { throw 'User task trace missing.' }
        foreach($c in $source.criterion_ids) { if($c -cnotin $t.criterion_ids) { throw "User criterion mapping lost: $($source.id) -> $c" } }
        Assert-Dependencies $t $source.depends_on
    }
    $milestones=New-IdMap $Logical.milestones
    foreach($source in $Plan.milestones) {
        if(-not $milestones.ContainsKey($source.id) -or $milestones[$source.id].acceptance -cne $source.acceptance) { throw "User milestone condition changed: $($source.id)" }
        foreach($id in $source.task_ids) {
            foreach($current in (Get-CurrentIds $id)) {
                $represented=@($milestones[$source.id].task_ids | ForEach-Object { Get-CurrentIds $_ })
                if($current -cnotin $represented) { throw "User milestone task lost: $($source.id) -> $current" }
            }
        }
    }
    foreach($c in $Plan.completion_criteria) {
        if($AllowIncomplete) { continue }
        if(-not @($Logical.tasks | Where-Object { $_.required -and $_.status -cne 'superseded' -and $c.id -cin $_.criterion_ids }).Count) { throw "Uncovered criterion: $($c.id)" }
    }
    if($RequireMilestones) {
        if(-not $Logical.milestones.Count) { throw 'Plan requires at least one milestone.' }
        $covered=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($m in $Logical.milestones) {
            if(-not $m.task_ids.Count) { throw "Empty milestone: $($m.id)" }
            foreach($id in $m.task_ids) { foreach($current in (Get-CurrentIds $id)) { $null=$covered.Add($current) } }
        }
        foreach($t in $Logical.tasks) {
            if($t.required -and $t.status -cne 'superseded' -and -not $covered.Contains($t.id)) { throw "Required task has no milestone: $($t.id)" }
        }
    }
    foreach($f in $Logical.findings) { foreach($id in $f.task_ids) { if(-not $map.ContainsKey($id)) { throw "Unknown finding target: $id" } } }
}
