# Paths are lexical root-relative globs; reparse points are rejected conservatively.
$script:PathComparison = if($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$script:RegexOptions = if($IsWindows) { [Text.RegularExpressions.RegexOptions]::IgnoreCase } else { [Text.RegularExpressions.RegexOptions]::None }
$script:GlobCache=[Collections.Generic.Dictionary[string,regex]]::new([StringComparer]::Ordinal)
function Assert-Relative([string]$Path,[switch]$Literal) {
    if([string]::IsNullOrWhiteSpace($Path) -or $Path.Contains('\') -or $Path -match '(^/|[:!{}\[\]\x00-\x1f])' -or $Path.EndsWith('/') -or @($Path.Split('/') | Where-Object { $_ -in @('','.','..') }).Count) { throw "Invalid relative path: $Path" }
    if($Literal -and $Path -match '[*?]') { throw "Expected literal path: $Path" }
    foreach($part in $Path.Split('/')) {
        if($part.Contains('**') -and $part -cne '**') { throw "** must be an entire segment: $Path" }
        if($IsWindows -and ($part -match '[. ]$|[<>"|]' -or $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)')) { throw "Ambiguous or reserved Windows path: $Path" }
    }
}
function Resolve-Safe([string]$Root,[string]$Relative) {
    Assert-Relative $Relative -Literal
    $path=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
    if(-not $path.StartsWith($Root.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,$script:PathComparison)) { throw 'Path escapes root.' }
    $part=$Root
    foreach($segment in $Relative.Split('/')) {
        $part=Join-Path $part $segment
        if(Test-Path -LiteralPath $part) { if((Get-Item -LiteralPath $part -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Links are not supported: $part" } }
    }
    $path
}
function Convert-Glob([string]$Pattern) {
    if($script:GlobCache.ContainsKey($Pattern)) { return $script:GlobCache[$Pattern] }
    Assert-Relative $Pattern
    $segments=$Pattern.Split('/'); $b=[Text.StringBuilder]::new('^')
    for($i=0;$i -lt $segments.Length;$i++) {
        $s=$segments[$i]
        if($s -ceq '**') {
            if($i -eq $segments.Length-1) { $null=$b.Append('.*') } else { $null=$b.Append('(?:[^/]+/)*') }
        } else {
            $null=$b.Append([regex]::Escape($s).Replace('\*','[^/]*').Replace('\?','[^/]'))
            if($i -lt $segments.Length-1) { $null=$b.Append('/') }
        }
    }
    $null=$b.Append('$'); $regex=[regex]::new($b.ToString(),$script:RegexOptions)
    if($script:GlobCache.Count -ge 1024) { $script:GlobCache.Clear() }
    $script:GlobCache.Add($Pattern,$regex); return $regex
}
function Test-InScope([string]$Path,$Patterns) {
    foreach($p in $Patterns) { if((Convert-Glob $p).IsMatch($Path)) { return $true } }
    return $false
}
function Get-DefaultGeneratedScope { '**/.vs/**'; '**/bin/**'; '**/obj/**'; '**/TestResults/**'; '**/BenchmarkDotNet.Artifacts/**' }
function Get-GeneratedScope($Plan) {
    @(@(Get-DefaultGeneratedScope)+@($Plan.generated_scope) | Sort-Object -CaseSensitive -Unique)
}
function Test-Protected([string]$Root,[string]$Path) {
    if($Path -match '(^|/)(\.git|\.autoframe|\.codex|\.agents)(/|$)' -or [IO.Path]::GetFileName($Path) -iin @('AGENTS.md','AGENTS.override.md') -or $Path -ieq 'PLAN.md') { return $true }
    $full=Join-Path $Root $Path
    return $full.StartsWith($script:FrameRoot.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,$script:PathComparison) -or $full.Equals($script:FrameRoot,$script:PathComparison)
}
function Assert-PlanScopes([string]$Root,$Plan) {
    $generated=Get-GeneratedScope $Plan
    $inputPatterns=if($Plan.Contains('input_scope')) { @($Plan.input_scope) } else { @('**') }
    foreach($p in @($inputPatterns)+@($Plan.work_scope)+@($Plan.generated_scope)) { Assert-Relative $p }
    foreach($r in $Plan.references) { $null=Resolve-Safe $Root $r.path }
    foreach($p in @($inputPatterns)+@($Plan.work_scope)+@($Plan.references | ForEach-Object { $_.path })) {
        if($p -and $p -notmatch '[*?]' -and (Test-InScope $p $generated)) { throw "Explicit input excluded as generated: $p" }
    }
    foreach($p in @($Plan.work_scope)+@($Plan.generated_scope)) {
        if($p -notmatch '[*?]' -and (Test-Protected $Root $p)) { throw "Protected write target: $p" }
    }
}
function Get-Tree([string]$Root,[switch]$Distribution) {
    $stack=[Collections.Generic.Stack[string]]::new(); $stack.Push($Root)
    $entries=[Collections.Generic.List[object]]::new()
    while($stack.Count) {
        Invoke-Tick
        $dir=$stack.Pop()
        foreach($item in [IO.DirectoryInfo]::new($dir).EnumerateFileSystemInfos()) {
            $relative=[IO.Path]::GetRelativePath($Root,$item.FullName).Replace('\','/')
            if(-not $Distribution -and $relative -match '(^|/)(\.git|\.autoframe)(/|$)') { continue }
            if($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Links are not supported: $relative" }
            if($item -is [IO.DirectoryInfo]) {
                $entries.Add(@{path=$relative;kind='directory';hash=$null}); $stack.Push($item.FullName)
            } else {
                Invoke-Tick
                $hash=[Autoframe.Json]::FileHash($item.FullName,[Action]{ Invoke-Tick })
                $entries.Add(@{path=$relative;kind='file';hash=$hash})
            }
        }
    }
    $array=$entries.ToArray()
    # Culture-independent, case-preserving order in every manifest.
    [Array]::Sort($array,[Collections.Generic.Comparer[object]]::Create({param($a,$b) [StringComparer]::Ordinal.Compare($a.path,$b.path)}))
    return ,$array
}
function Get-Snapshot([string]$Root,[switch]$Distribution) {
    $first=$null
    for($retry=0;$retry -lt 3;$retry++) {
        Invoke-Tick
        try {
            if($null -eq $first) { $first=Get-Tree $Root -Distribution:$Distribution }
            $second=Get-Tree $Root -Distribution:$Distribution
            if((Get-ObjectHash $first) -ceq (Get-ObjectHash $second)) { return ,$second }
            $first=$second
        } catch {
            if($_.Exception.GetBaseException() -isnot [IO.IOException]) { throw }
            $first=$null
        }
    }
    throw 'Files did not stabilize during manifest capture.'
}
function New-Manifest($Snapshot,$Scope,$Exclude=@(),[switch]$FilesOnly) {
    $entries=[Collections.Generic.List[object]]::new()
    foreach($e in $Snapshot) {
        if($FilesOnly -and $e.kind -cne 'file') { continue }
        if((Test-InScope $e.path $Scope) -and -not (Test-InScope $e.path $Exclude)) { $entries.Add($e) }
    }
    foreach($p in $Scope) {
        if($p -notmatch '[*?]' -and -not @($Snapshot | Where-Object { $_.path.Equals($p,$script:PathComparison) }).Count -and -not (Test-InScope $p $Exclude)) { $entries.Add(@{path=$p;kind='missing';hash=$null}) }
    }
    $array=$entries.ToArray(); [Array]::Sort($array,[Collections.Generic.Comparer[object]]::Create({param($a,$b) [StringComparer]::Ordinal.Compare($a.path,$b.path)}))
    @{schema_version=1;scope=@($Scope | Sort-Object -CaseSensitive -Unique);entries=@($array)}
}
function Get-InputManifest([string]$Root,$Plan,$Snapshot) {
    $generated=Get-GeneratedScope $Plan
    foreach($r in $Plan.references) { if(-not (Test-Path -LiteralPath (Resolve-Safe $Root $r.path) -PathType Leaf)) { throw "Missing reference: $($r.path)" } }
    $patterns=@(if($Plan.Contains('input_scope')) { $Plan.input_scope } else { '**' })
    $patterns+=@($Plan.work_scope)+@($Plan.references | ForEach-Object { $_.path })
    $filtered=@($Snapshot | Where-Object { -not (Join-Path $Root $_.path).StartsWith($script:FrameRoot.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,$script:PathComparison) })
    $instructions=@('**/AGENTS.md','**/AGENTS.override.md','**/.codex/**','**/.agents/**')
    $filtered=@($filtered | Where-Object { (Test-InScope $_.path $instructions) -or -not (Test-InScope $_.path $generated) })
    New-Manifest $filtered (@($patterns)+$instructions) -FilesOnly
}
function Get-ProtectedManifest([string]$Root,$Plan,$Snapshot,[string]$Phase) {
    $allowed=if($Phase -ceq 'Work') { @($Plan.work_scope)+@($Plan.generated_scope) } elseif($Phase -cin @('Verify','CompletionAudit','Probe')) { @($Plan.generated_scope) } else { @() }
    # Default output directories allow background updates in every phase; protected files still win.
    $allowed=@($allowed)+@(Get-DefaultGeneratedScope)
    $entries=@($Snapshot | Where-Object { (Test-Protected $Root $_.path) -or ($_.kind -ceq 'file' -and -not (Test-InScope $_.path $allowed)) })
    # PLAN edits are handled separately by the stale-input path, never silently accepted.
    @{schema_version=1;scope=@('protected', $Phase);entries=@($entries | Where-Object { $_.path -ine 'PLAN.md' -and $_.kind -ceq 'file' })}
}
function Get-ManifestChanges($Before,$After) {
    $old=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $current=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $paths=[Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    foreach($e in $Before) { $old[$e.path]=$e; $null=$paths.Add($e.path) }
    foreach($e in $After) { $current[$e.path]=$e; $null=$paths.Add($e.path) }
    foreach($p in $paths) {
        $a=if($old.ContainsKey($p)) { $old[$p] } else { $null }
        $b=if($current.ContainsKey($p)) { $current[$p] } else { $null }
        if($null -eq $a -or $null -eq $b -or $a.kind -cne $b.kind -or $a.hash -cne $b.hash) {
            @{path=$p;change=$(if($null -eq $a) { 'added' } elseif($null -eq $b) { 'deleted' } else { 'modified' })}
        }
    }
}
function Assert-ExecutionPlan($Execution,$Logical,$Plan,[string]$Root,[int]$Maximum) {
    if($null -eq $Execution -or $Execution.tasks.Count -lt 1 -or $Execution.tasks.Count -gt $Maximum) { throw 'Invalid Work batch size.' }
    Assert-Unique @($Execution.tasks.id)
    $map=New-IdMap $Logical.tasks
    $prefixes=[Collections.Generic.List[object]]::new()
    foreach($entry in $Execution.tasks) {
        if(-not $map.ContainsKey($entry.id)) { throw "Unknown execution target: $($entry.id)" }
        $t=$map[$entry.id]
        if($t.status -cne 'pending') { throw "Target is not pending: $($t.id)" }
        foreach($dep in $t.depends_on) { if($map[$dep].status -cne 'verified' -or $dep -cin $Execution.tasks.id) { throw "Dependency not ready: $dep" } }
        foreach($p in $entry.edit_scope) {
            Assert-Relative $p
            # A plan may use a declared glob or narrow it to a literal. This is decidable and conservative.
            if($p -cnotin $Plan.work_scope -and ($p -match '[*?]' -or -not (Test-InScope $p $Plan.work_scope))) { throw "edit_scope is outside work_scope: $p" }
            if($p -notmatch '[*?]' -and (Test-Protected $Root $p)) { throw 'Protected edit target.' }
            $prefix=($p -split '[*?]',2)[0].TrimEnd('/')
            foreach($old in $prefixes) {
                if($old.id -cne $entry.id -and ($prefix.StartsWith($old.prefix,$script:PathComparison) -or $old.prefix.StartsWith($prefix,$script:PathComparison))) { throw 'Batch edit scopes may overlap; use separate Work trials.' }
            }
            $prefixes.Add(@{id=$entry.id;prefix=$prefix})
        }
    }
}
function Get-Signature($PlanHash,$Logical,$InputHash,$Environment,$FrameworkHash) {
    Get-ObjectHash @{plan=$PlanHash;definitions=(Get-Definitions $Logical);input=$InputHash;environment=$Environment;framework=$FrameworkHash}
}
function Get-EvidenceKey($Logical,$Ids,$Map=$null) {
    if($null -eq $Map) { $Map=New-IdMap $Logical.evidence }
    $keys=@(foreach($id in $Ids) {
        if(-not $Map.ContainsKey($id)) { throw "Unknown evidence: $id" }
        $e=$Map[$id]
        Get-ObjectHash @{kind=$e.kind;phase=$e.phase;hash=$e.hash;targets=@($e.target_ids | Sort-Object -CaseSensitive);signature=$e.input_signature;artifacts=$e.artifact_manifest}
    })
    return ,@($keys | Sort-Object -CaseSensitive -Unique)
}
function Get-AuditKey($PlanHash,$Logical,[string]$Signature) {
    $map=New-IdMap $Logical.evidence
    $tasks=@($Logical.tasks | ForEach-Object { @{id=$_.id;status=$_.status;evidence=(Get-EvidenceKey $Logical $_.evidence_refs $map)} })
    $findings=@($Logical.findings | ForEach-Object { $f=Copy-Value $_; $f.evidence_refs=Get-EvidenceKey $Logical $_.evidence_refs $map; $f })
    Get-ObjectHash @{plan=$PlanHash;definitions=(Get-Definitions $Logical);signature=$Signature;milestones=$Logical.milestones;evidence=$tasks;findings=$findings}
}
