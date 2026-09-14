# autoframe Specification 0.1

This document defines the PowerShell and prompt requirements. See the [run instructions](USAGE.en.md) and [verification results and limitations](VERIFICATION.md). The [Japanese specification](SPEC.md) is authoritative.

Keep the [README](README.en.md) focused on setup and basic operations; document arguments, diagnostics, and recovery details here. When updating the root README.md and README.en.md, synchronize their bodies into autoframe/, adjusting only relative links for the destination. The Japanese text is authoritative; reflect the same content in English.

## QuickStart

### 1. Check the runtime

Use Windows, PowerShell 7.4 or later, a supported Codex CLI, existing authentication and permissions, and the project development tools. Place the complete [autoframe/][qs-runner] directory at the project root, then check real CLI connectivity.

```powershell
pwsh -NoProfile -File ./autoframe/tests/smoke-real-cli.ps1 -RunRealCli
```

A successful smoke test does not establish product completion or permission to write evidence files. See the [verification record][qs-verification] for tested coverage.

```gitignore
# Local runner state, evidence, and logs (including nested projects).
.autoframe/
```

For Git repositories, add `.autoframe/` to the repository root `.gitignore`. This excludes run records at every depth while keeping the `autoframe/` distribution trackable. If records are already tracked, run the following from that project root, then commit `.gitignore` and the removal from tracking:

```powershell
git rm -r --cached -- .autoframe
```

Local records remain; past commits are unchanged. `.autoframe/` contains state and evidence needed for Resume, so do not delete it as disposable temporary data. The runner does not automatically change Git settings or tracking.

### 2. Create PLAN.md

Ask Codex to create your plan using the following prompt. Replace the placeholders with your requirements. If PLAN.md already exists, review it before running.

```text
Following section 2 of autoframe/SPEC.md, create PLAN.md at the project root.

Objective: <what you want to achieve>
Scope: <features, folders, and reference materials>
Completion criteria: <conditions that determine when the work is complete>
Non-goals and constraints: <what must remain unchanged and prohibited actions>

Inspect the project structure and existing verification procedures.
Save the four fields above verbatim in a "User prompt (original)" section in PLAN.md.
Make details concrete wherever the available information supports a decision.
Automatically generate tasks and milestones from the objective and completion criteria.
Give each milestone target tasks and observable acceptance conditions, covering every required task.
Ask only about missing information that affects the objective, mandatory
requirements, or permitted scope of changes.
Create only PLAN.md. Do not start product changes, tests, or automated execution.
```

You can also use the [PLAN template][qs-template] and [creation prompt][qs-create-plan]. Store the original before the JSON and check that the refined JSON agrees with it. Use one milestone for small jobs or several meaningful checkpoints. Execution progress stays in internal records; verified counts are displayed and Completion Audit checks milestone acceptance conditions.

### 3. Run and resume

After reviewing PLAN.md, run commands from the project root containing `autoframe/`. Choose the example that matches your use case:

```powershell
# Start
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot .

# Start with limits of 120 cumulative minutes and 30 Worker launch attempts
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -MaxRunMinutes 120 -MaxPhaseAttempts 30

# Start for another project (quote paths containing spaces)
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot 'C:/Projects/Sample App'

# Resume an interrupted run
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume

# Resume with the cumulative runtime limit increased to 720 minutes
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume -MaxRunMinutes 720

# Regenerate PLAN.md on the 5th, 10th, 15th, and subsequent multiples of five Plan launches
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume -PlanRegenerationInterval 5

# Run one task per Work and perform Completion Audit after every Verify
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -MaxTasksPerWork 1 -CompletionAuditInterval 1

# Start a new run while retaining history and artifacts
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -NewRun
```

If an unfinished run exists, specify `-Resume` or `-NewRun`. They cannot be combined. Resume inherits omitted settings and changes explicit values only. Reverify evidence invalidated by a settings change.

`PlanRegenerationInterval` is a positive integer, defaulting to 3. It counts Plan launch attempts. Resume retains the count and setting; NewRun starts the count at zero. The runner validates and saves the Worker's proposal, then revalidates evidence affected by the change. A missing original prompt stops the run with `NeedsInput`. Regeneration does not reset stall counters or runtime usage.

The default limits are 480 cumulative minutes and 100 Worker launch attempts per run. Resuming does not reset these totals. Each Work phase can handle up to three tasks.

**At MaxRunMinutes, the runner stops new launches and forcibly terminates any active Worker and its child processes.** By default, Workers are instructed to start saving and handing off five minutes before their deadline. After stopping and saving recovery state, the runner returns `Paused`, or `Error` if termination cannot be confirmed. Each termination check or recovery operation has a default 60-second wait limit; total shutdown duration is not guaranteed. To resume after exhausting the cumulative limit, increase MaxRunMinutes as shown above.

A successful run ends with `Complete`. Time or attempt limits can result in `Paused`. Other states are `Blocked` for external dependencies, `NeedsInput` for user decisions, `Stalled` for repeated lack of progress, and `Error` for failures. A normal `-Resume` does not clear `Stalled`.

`-NewRun` also checks unverified artifacts before continuing work. Resuming a completed run triggers a fresh audit if completion evidence is missing or corrupt. Internal plan updates preserve user-defined dependencies, completion-criterion mappings, and milestone task membership.

Edit PLAN.md manually only while the runner is stopped, and do not edit internal state files manually. See [SPEC.en.md][qs-spec] for the full specification and [autoframe/USAGE.en.md][qs-runner] for runner details.

### 4. Worker interruption and file changes

**Ordinary project files are not watched continuously, so adding, changing, or deleting a file does not immediately interrupt an active Worker.** The runner compares content hashes and other records before and after phases to decide whether to accept results, replan, or stop.

| Check timing | Condition | Behavior |
| --- | --- | --- |
| During execution | Remaining run time or `PhaseTimeoutMinutes` (60 minutes by default) expires | Forcibly stop the Worker and its children at the earlier deadline; normally return `Paused` |
| During execution | Ctrl+C cancellation request | Stop the Worker and its children; normally return `Paused` |
| During execution | Log writes or heartbeat state saves fail, or a state save detects an external change to `.autoframe/state.json` | Attempt to stop the Worker and its children; return `Error` |
| At exit or acceptance | Abnormal CLI exit, surviving child processes, or invalid results or evidence | Reject the result and return `Error`; stop remaining children |
| After execution | PLAN.md changed | Reject the result based on the old input, retain partial artifacts, and return to Plan. Invalid format or a changed project_id within the same run causes `Error` |
| After execution | Work changed files within its permitted `edit_scope` and `generated_scope` | The change alone does not stop the run; proceed to Verify after validating the result |
| After execution | Protected or shared framework files changed, Work exceeded its edit scope, or a non-Work phase changed verification inputs | Reject the result and return `Error`. Permitted verification outputs follow scope and evidence rules |
| Before the next phase | File, environment, or other changes invalidate evidence | Revoke affected completion decisions and reevaluate through Plan or Verify |

File diffs do not identify who made a change. If a user or another process edits the same permitted scope, the runner cannot distinguish or stop the edit solely because it came from outside. Changes reverted before the next comparison may also go undetected.

`MaxPhaseAttempts` limits subsequent launches; reaching it alone does not interrupt an active Worker. New launches also stop when the remaining time is no greater than `SaveReserveMinutes`. Stalls, pending user decisions, and external blockers are evaluated from phase results and saved state.

Unconfirmed termination causes `Error`. If saving also fails, state may remain uncertain. Forcibly terminating the runner causes its Windows Job Object to terminate member processes, but a clean state save is not guaranteed. Resume checks remaining processes and partial artifacts.

### 5. Runtime status

No extra arguments are needed. At startup or resume, the runner displays the local session start time with UTC offset, run ID, project directory, time limits, and record directory. Each Worker launch shows the phase, target IDs, and deadline. Worker exit shows duration and exit code; result acceptance shows the decision, verified task count, and next phase.

During long operations, status lines appear approximately once a minute. Start, exit, and phase changes are reported as they occur; recovery state is still saved approximately every 20 seconds. Example:

```text
[10:00:00] Start | session_started=2026-09-14 10:00:00 +09:00 | run=...
[10:00:03] 1: Plan | targets=- | deadline=11:00:03 +09:00
[10:01:03] Worker running | phase=Plan | attempts=1/100 | Plan=1 | elapsed=00:01:00 | total_elapsed=00:01:03 | remaining=01:58:57
```

`attempts` is the cumulative launch attempt count across all phases / limit, including failed launches. `Plan` counts Plan launch attempts only. `elapsed` measures time since the current Worker started, resets for each Worker, and freezes while its result is checked. It is `-` when no Worker has started. `total_elapsed` includes runtime before Resume, and `remaining` is the remaining total budget. Time limits still use cumulative runtime. `session_started` and the final `session_duration` describe this invocation only. `verified` counts verified tasks among all non-superseded tasks; it is not a percentage of total work completed.

Status lines indicate runner activity, not confirmed progress inside the Worker. Synchronous operations may delay updates. For details, inspect the trial directory shown by `Trial` at Worker launch.
### IDE cache and dependency diagnostics

The fixed defaults are `**/.vs/**`, `**/bin/**`, `**/obj/**`, `**/TestResults/**`, and `**/BenchmarkDotNet.Artifacts/**`. They apply to every project and phase regardless of language, IDE, or existing folders. They remain active when PLAN omits `generated_scope` or sets it to an empty array; new templates list them explicitly. The effective generated scope combines these defaults with PLAN's declarations. Workers receive `default_generated_scope` and `effective_generated_scope`; existing PLAN files remain unchanged.

Declare other outputs in PLAN's `generated_scope`. Do not exclude all of `.vscode` or `.idea`, which can contain shared settings.

The fixed default affects verification input and allowed-scope checks. Before/after snapshots and required artifact hashing continue, with instruction and distribution protection taking precedence. Explicitly listing a required input or reference inside generated scope remains an error.

Protected changes stop execution with `Protected files changed during ...`, without attributing them to the Worker. The log lists the first 20 paths with added, modified, or deleted status. The trial directory's `protected-changes.json` contains every change, labeled `framework` for distribution files or `project` for project files. Instruction files remain protected even inside generated directories.

When verification inputs include a .NET solution or project, each Worker first runs `check-dependencies.ps1` in its own command execution environment. It saves the outcome of `dotnet nuget list source --format short`, any failure cause, and release conditions to its own `output/dependency-preflight.json`. The runner rejects missing or invalid reports. Successful source listings are not saved. Runner-side `environment_checks` do not prove Worker-side read access.

On dependency failure, the Worker reports only affected tasks as `blocked` and continues independent work. If the check cannot run, it records the cause and release conditions in the same report. A successful read check does not prove restore succeeds. Work and Verify run the planned restore before lengthy dependent work; they must not substitute old assets or `--no-restore` for a failed restore. They do not automatically change ACLs, permissions, NuGet configuration, or sources, and provide recovery steps that preserve existing sources and authentication.

## 1. Principles

autoframe splits large jobs into planning, auditing, execution, and verification.

**Define project objectives, completion criteria, and work scope in PLAN.md.** Set operational limits and models through the arguments in §7. Workers do not edit PLAN.md directly; the runner applies only periodic regeneration under §5.5.

| Component | Responsibility |
| --- | --- |
| PLAN.md | User objective, completion criteria, scope, constraints, and optional initial plan |
| runner | Launch Workers sequentially, run mechanical checks, update state, route phases, and stop |
| Worker | A Codex session for one phase: planning, work, audit, or verification |
| Internal state | Generated plans, progress, findings, evidence, and execution position |
| Artifacts | Files Workers read or modify under PLAN.md |

Do not require specific documents such as SPEC.md or STATUS.md, or manual editing of internal state.

The initial release runs one project sequentially on a Windows local filesystem. Each phase starts a fresh Worker session and passes context through files. Parallel editing, scheduled runs, artifact writes outside the project, and external service updates or publishing are out of scope. Use temporary files and caches within existing permissions.

PowerShell, Codex CLI, authentication, and development tools are environment prerequisites. Follow user instructions, applicable AGENTS.md files, and environment permissions. Git is not a required control input. Do not automatically run reset, clean, stash, commit, or push.

## 2. User Input

### 2.1. PLAN.md fields

PLAN.md contains only the user definitions below, without progress, evidence, findings, or runner revisions. `schema_version` is 1. Strings must be nonempty. Arrays may be empty where allowed below. Reject unknown fields, duplicate JSON keys, and invalid types.

Before the JSON, add a "User prompt (original)" heading and a text code block containing the supplied objective, scope, completion criteria, and non-goals and constraints verbatim. Preserve embedded headings as text and mark missing fields as not provided. Check the refined JSON against the original. Periodic regeneration must preserve the original; users changing requirements should update both the original and JSON consistently while stopped. Do not reconstruct missing original text by guessing.

| Field | Required | Meaning |
| --- | --- | --- |
| schema_version / project_id | Yes | Format version and project identifier |
| objective / scope | Yes | Goal and work targets |
| non_goals / constraints | Yes | Exclusions and required limits; empty arrays if none |
| completion_criteria | Yes | One or more entries with id, condition, and verification |
| work_scope | Yes | Allowed source and other edit paths; empty for read-only work |
| input_scope | No | Verification input paths; defaults to the whole project as the baseline |
| generated_scope | No | Additional allowed paths for verification outputs; defaults to []. The five fixed defaults above always apply separately |
| environment_checks | No | Read-only commands to identify the environment; defaults to [] |
| references | No | Reference path and purpose; defaults to [] |
| milestones / tasks | Optional in the format | Automatically generated when creating a plan; omitted fields in existing plans default to [] |

Completion criteria must define a finite scope and observable expected results. Avoid open-ended conditions such as “until no problems remain.” Verification may use commands, document comparisons, or review procedures.

Paths are relative to ProjectRoot. Verification inputs always include files in work_scope and references, even when input_scope is specified. Do not use generated_scope to exclude source files, configuration, or references from inputs. See §5.1 for scope rules.

`environment_checks` is an array of read-only PowerShell command strings that identify required tool versions or external dependencies. See §5.2 for execution and missing environment information.

### 2.2. Automatic initial plans and milestones

Each task has id, description, required (boolean), depends_on and criterion_ids (ID arrays), acceptance, and verification. Each milestone has id, description, task_ids, and acceptance. IDs must be unique within each type. References use exact, case-sensitive matching. Reordering must not change IDs.

When creating PLAN.md, automatically generate tasks and at least one milestone from the objective and completion criteria. Use one milestone for small jobs, or several meaningful checkpoints where appropriate. Each milestone must have nonempty task_ids; every required task must belong to a milestone. Write acceptance conditions that can be checked against artifacts and evidence. Check coverage during creation, and keep progress and evidence out of PLAN.md.

Existing plans may omit the initial plan; Plan fills it in internally. Before accepting ready/recover, mechanically require at least one milestone, nonempty task_ids for each, and coverage of all required tasks after resolving replacements. replan/needs_input may save an incomplete plan.

Preserve initial tasks as user requirements. Plan may refine or split them internally while retaining their original ID mapping, required status, and conditions. If the objective or user-defined conditions must change, save a concrete proposal and stop with NeedsInput. Routine additions, splits, and reordering are automatic.

Do not remove user-defined dependencies, criterion_ids, or milestone task membership. Map split tasks to their replacements. Dependency ordering may be preserved through intermediate tasks.

Milestones mark progress. Any condition required for completion must also appear in completion_criteria.

If a milestone references a superseded task, evaluate it using the status and evidence of every replacement.

### 2.3. PLAN.md example

Place one JSON code block between the required markers. JSON is authoritative for control. Store the original user prompt and background outside that block. The original prompt records the user's request; it is not generation history or progress.

This Python example allows test failures during investigation but requires all tests to pass after repair.

~~~~markdown
# Investigate Tests and Fix Defects

## User prompt (original)

```text
Objective: Investigate existing test failures and fix defects.
Scope: src and tests. Refer to README.md for usage instructions.
Completion criteria: All existing tests pass, and any necessary fixes can be validated.
Non-goals and constraints: Do not change public APIs or delete or disable tests. Preserve uncommitted changes.
```

## Execution plan

<!-- autoframe:begin -->
```json
{
  "schema_version": 1,
  "project_id": "sample-test-repair",
  "objective": "Investigate existing test failures and fix defects",
  "scope": ["src and tests"],
  "non_goals": ["Public API changes", "Deleting or disabling tests"],
  "constraints": ["Preserve existing uncommitted changes"],
  "work_scope": ["src/**", "tests/**"],
  "input_scope": ["src/**", "tests/**", "pyproject.toml"],
  "generated_scope": ["**/.vs/**", "**/bin/**", "**/obj/**", "**/TestResults/**", "**/BenchmarkDotNet.Artifacts/**"],
  "environment_checks": ["python --version", "python -m pip freeze"],
  "references": [{"path": "README.md", "purpose": "Usage instructions"}],
  "completion_criteria": [
    {
      "id": "C1",
      "condition": "All existing tests pass, and any required fixes are validated",
      "verification": "Run python -B -m unittest discover -s tests -v. Require at least one test and zero failures, errors, or skips. If changes were made, also review the diff and reproduction results"
    }
  ],
  "tasks": [
    {
      "id": "T1", "description": "Run tests and investigate failures",
      "required": true, "depends_on": [], "criterion_ids": ["C1"],
      "acceptance": "Record test results and failure causes, or evidence that no tests failed",
      "verification": "Compare logs with the investigation. Test failures are allowed for this task"
    },
    {
      "id": "T2", "description": "Apply required fixes and perform final verification",
      "required": true, "depends_on": ["T1"], "criterion_ids": ["C1"],
      "acceptance": "Meet C1. If no fix is needed, record why",
      "verification": "Follow the verification procedure in C1"
    }
  ],
  "milestones": [
    {
      "id": "M1", "description": "Investigation and repair complete",
      "task_ids": ["T1", "T2"], "acceptance": "Confirm how each cause relates to the repair results"
    }
  ]
}
```
<!-- autoframe:end -->
~~~~

Adapt commands, environment checks, and scope to the target project.

## 3. Internal Plan and Records

### 3.1. Layout and persistence

```text
project/
  PLAN.md
  autoframe/                    # May be shared from another location
    run.ps1 / invoke-worker.ps1
    lib/                        # Plans, state, transitions, manifests
    prompts/                    # Shared rules and six phases
    schemas/                    # PLAN, internal records, phase results
    templates/PLAN.template.md
    tests/ / README.md / README.en.md / USAGE.md / USAGE.en.md
  .autoframe/
    lock
    state.json                  # Current record references and execution control
    records/                    # Accepted results, internal plans, evidence history
    manifests/                  # Shared by content hash
    runs/<run-id>/<attempt-id>/  # Input references, logs, before/after diffs
```

`state.json` and its referenced records are authoritative. Only the runner updates them. Each Worker receives its own attempt output location. Retain referenced records; operation must not require manual state edits.

Persist new records before atomically replacing state.json from a temporary file on the same filesystem. Unreferenced records are unaccepted. Serialize all updates, including heartbeats, and prevent concurrent runners with an exclusive lock.

**base_plan_version covers internal tasks, execution plans, progress, findings, and evidence.** Increment it when result acceptance, PLAN changes, evidence expiry, or recovery changes and saves this content. Heartbeats, time updates, and attempt-start saves alone do not increment it.

Never reuse run_id or attempt_id. Reject reapplication of an accepted attempt_id. Commit result acceptance, internal plan version, next phase, and counter updates in one state transaction.

### 3.2. Internal tasks and status

Internal tasks extend §2.2 with source_ids (initial task mappings), deliverables (required artifact paths or globs), status, evidence_refs, remaining, blocker, and replacements. New tasks not derived from initial tasks may use empty source_ids.

```text
pending → implemented → verified
   ↑           │           │
   └───────────┴───────────┘  Needs correction or reverification
```

`blocked` means waiting on an external condition; `superseded` means replaced. Replacements inherit the original conditions, and dependent tasks must point to the replacement IDs. Reject removal of required tasks or changes that make them optional.

**in_progress is represented by the active attempt and target IDs in state.json, not by task status.** At Work start, save only that execution state; do not change PLAN.md or task definitions. Work may propose implemented; only Verify may propose verified.

Before Work starts, all dependencies must have valid verified status. Dependencies of required tasks are necessary for completion even if marked optional. Plan ready/recover must map every completion criterion to required tasks. Mechanically validate IDs, references, cycles, and replacements.

Record the completed portion of partial work. Verify returns it to pending for correction or reverification, or marks it blocked. Return blocked tasks to pending once release conditions are confirmed. To check existing artifacts or expired evidence, Plan may route directly to Verify with recover.

### 3.3. Findings and history

Each finding has id, kind (plan/product), required, description, resolution conditions, assigned task IDs, open/resolved status, and evidence references. Plan assigns tasks. Audit confirms resolution of plan findings; Verify or Completion Audit confirms resolution of product findings. Mark false positives resolved with a reason.

Bodies of resolved findings, superseded tasks, and old evidence may move to history. Keep the IDs, replacement IDs, and references needed in current state. Required conditions and dependencies must remain part of completion checks. Omitting a task or finding does not delete or resolve it.

## 4. Execution Flow

### 4.1. Roles and transitions

| Phase | Responsibility | decision → destination |
| --- | --- | --- |
| Plan | Update the internal plan by changes; map required findings to tasks | ready → Prepare; recover → Verify |
| Prepare | Select targets, scope, steps, verification, and time allocation | ready → Audit; completion_candidate → Completion Audit |
| Audit | Check overall coverage and review or revise the execution plan | approved → Work; revise → Prepare |
| Work | Perform work; record self-checks and remaining work | reported → always Verify |
| Verify | Assess each task against artifacts and evidence | accepted / revise → route based on state |
| Completion Audit | Check completion criteria, regressions, and required findings | complete → completion checks; incomplete → Plan |

“Acceptance” means adopting a valid report, not necessarily successful work. Verify's accepted means all target tasks are complete; revise includes incomplete tasks. Both update state if format and authority checks pass.

All phases except Work may also return replan. Work requests replanning through route_hint in a reported result and must pass through Verify first. Any phase may stop with needs_input while preserving unverified partial work.

Record external waits per task. Prioritize recovery of unsettled attempts, then continue any independent runnable tasks. Use Blocked only when all remaining necessary work awaits external conditions. If no candidate can be selected for another reason, return to Plan.

Plan, Prepare, and Audit must not modify the product. Verify and Completion Audit only verify; send fixes back to Work. Verification writes are limited to generated_scope and the Worker's evidence output location.

After acceptance, check completion, pending user decisions, stalls, and time or attempt limits. Then prioritize verification of unverified work, necessary replanning, Completion Audit, and the next Prepare, in that order. Do not accept results with PLAN.md changes or format errors; follow §5.

### 4.2. Batching and Audit

One Work attempt may include up to three independent tasks. At start, all dependencies must be satisfied, tasks must not depend on each other or have conflicting edit scopes, and time must allow self-checks and handoff. Run checks that share outputs sequentially. Three is a limit, not a target. Return results per task.

The initial release runs Audit before every Work. Bind approval to PLAN, the input signature, execution plan, and findings, and recheck its evidence immediately before Work. Exclude attempt IDs, absolute deadlines, and in-progress indicators from the execution plan hash.

Pass the revised execution plan accepted by Audit to Work. Before Work uses a new or changed internal plan, Audit must confirm coverage and resolution of required plan findings. Recovery Verify may run first. Mechanical checks alone cannot prevent semantic weakening of requirements.

Use existing CLI model settings by default; PhaseModels may override each phase. Do not switch all phases to lighter models without checking quality and usage.

### 4.3. Verification and completion

Verify must inspect evidence, not rely only on self-reports. Unperformed checks, skips, and missing evidence are not success. Reviews and documentation work may be verified through comparison records; code changes and test runs are not universally required.

Run Completion Audit when all required tasks and necessary dependencies have valid verified status, a new milestone may be reached, or the configured number of Work attempts have been accepted through Verify. Combine simultaneous triggers into one audit. An accepted Completion Audit resets the interval counter to zero.

Each Worker receives milestone_progress with milestone IDs, target IDs after replacement, verified and total task counts, and audit-candidate flags. Reflect expired evidence in these counts and print them after accepting Verify. All targets with valid verified status make a milestone an audit candidate. Completion Audit checks its acceptance conditions against artifacts and evidence and records the decision and gaps. Counts alone do not confirm a milestone or overall completion.

Deduplicate audits using requirements, internal plan definitions, current evidence, and required finding status. Include milestone acceptance conditions. Version or timestamp changes alone must not trigger another audit. If the same incomplete audit is requested again, return to Plan and count it toward stalling.

Complete requires all of the following:

- Completion Audit passed for the current inputs.
- All required tasks and necessary dependencies have valid verified status, with evidence for every completion criterion.
- All required findings are resolved, with no unsettled attempts or unverified work.

Unstarted optional tasks may remain. The runner checks structure and consistency; the audit Worker checks substantive validity. A clean exit or valid Schema alone does not establish completion.

## 5. Diffs, Evidence, and Recovery

### 5.1. Manifests and scope

A manifest records scope definitions and normalized, sorted relative paths, existence, entry types, and SHA-256 content hashes. Compare additions, deletions, and empty match sets. Modification times or sizes alone do not establish equality. Stream file contents and share identical records by hash.

| Purpose | Scope and capture time |
| --- | --- |
| Edit record | work_scope and generated_scope, before and after Work |
| Verification inputs | Union of input_scope, work_scope, and references, excluding generated files; before and after verification |
| Artifacts | Outputs required by completion criteria; during verification and evidence reuse |
| Protected files | Unauthorized project files, PLAN.md, and shared framework files; before and after each phase |

Use paths relative to ProjectRoot with `/` separators. In globs, `*` and `?` do not cross separators; a standalone `**` matches zero or more directory levels. `docs/**` includes all descendants. Negation and brace expansion are unsupported. The initial release targets case-insensitive Windows paths and rejects links, reparse points, reserved names, and trailing spaces or dots. Limit edit_scope to a declared work_scope glob or a concrete path within it; assess batch overlap conservatively.

If input_scope is omitted, use the whole project as the baseline. Exclude Git metadata, runner records, and the effective generated scope (fixed defaults plus generated_scope) from verification inputs; hash shared framework files separately. Required artifacts must still be checked even if generated.

Protect PLAN.md, applicable instruction files, Git metadata, shared framework files, and runner state regardless of work_scope or generated_scope. Workers must not modify Git metadata or runner records excluded from comparison. Worker outputs are limited to their attempt-specific area.

A broad glob may exclude its overlap with generated_scope, but reject exclusions of explicitly listed inputs or references. Record and allow missing inputs or edit targets intended for creation. Missing required references or read failures make a record incomplete.

If files change during enumeration or hashing, capture again. Do not accept a record that cannot stabilize within the time limit. Before/after diffs show changes between snapshots, not every operation, its author, or all out-of-scope actions. Post-run checks do not replace OS write restrictions.

### 5.2. Evidence reuse

Verification records contain procedures, expected and actual results, log and artifact references, and this signature:

```text
input signature = SHA-256(canonical JSON {
  hash of the entire PLAN.md,
  hash of target definitions, conditions, and verification procedures,
  hash of the verification input manifest,
  hash of environment information and shared framework files
})
```

Definition hashes exclude status, evidence IDs, and timestamps. Fix key ordering and character encoding for JSON used in mechanical comparisons.

Environment information includes the OS, PowerShell, CLI, execution settings for all phases, configuration and instruction file hashes, and environment_checks exit status and output. Exclude the current phase, time, and attempt ID. Changes to limits or models also change the signature and require reverification. Empty environment_checks does not automatically mean insufficient information. If a required environment detail or dependency cannot be identified, retain reference results and mark affected tasks blocked.

Save environment commands in an attempt script and run it with `pwsh -NoProfile -NonInteractive -File`. Propagate PowerShell errors and nonzero native command exits as failures. Apply phase and overall time limits, and save results. Failed checks do not provide valid identification. Do not generate commands from natural language.

Reusing evidence requires matching signatures, log hashes, required artifact hashes, and acceptance by the correct phase: Verify for tasks, Audit for plan audits, and Completion Audit for completion. Acceptance by another phase is not a substitute.

Reject verification if inputs differ before and after it. After Work, verify the changed inputs. Check the signature again immediately before accepting completion. Signatures establish equality of declared inputs only; Plan and Audit must check that those definitions are complete.

An expired signature does not imply that a fix is needed. Mark affected tasks for reverification, using Plan's recover route to Verify when appropriate. Multiple tasks may be reverified against the same current inputs. Retain old investigation records, but do not reuse them unchanged as current verified evidence.

### 5.3. Interrupted attempts and recovery

Before Work, persist the edit manifest, attempt ID, target IDs, and start record. Do not launch the Worker if saving fails. Capture the after manifest when the Worker ends, whether or not it returned a result.

An attempt that ends without acceptance is unsettled. On resume, identify old processes by start time and other attributes as well as PID, and confirm that they and their children have stopped. Do not terminate unrelated processes. Stop with Error if termination cannot be confirmed.

Plan reviews diffs and partial artifacts, then uses recover to pass them to Verify. Do not blindly repeat the same operation before recovery is accepted or returned for remaining work. After interrupted verification or environment checks, do not reuse generated files or evidence without checking them.

Recapture incomplete after records within the time limit. If before records are missing or corrupt, do not claim a complete diff: invalidate existing evidence and replan from current artifacts. Keep anything that cannot be verified under current requirements as blocked or needs_input. Missing records alone must not make recovery permanently impossible.

NewRun may start without internal records but must not inherit previous completion status. Preserve artifacts, uncommitted changes, and corrupt records.

### 5.4. PLAN.md changes

Normally edit PLAN.md while stopped. The runner compares its entire-file hash at each phase's start and acceptance. If it changes during execution, do not apply the old result to the current plan. Preserve partial work and return to Plan. Invalid PLAN.md causes Error.

Reevaluate internal plans, findings, and evidence against user changes. Record how removed or changed requirements were handled; do not automatically restore obsolete requirements. Changing project_id requires NewRun. Runner-driven periodic regeneration follows the next section.

### 5.5. Periodic regeneration from the original prompt

Count Plan launch attempts per run_id. At each multiple of PlanRegenerationInterval, regenerate the plan from the saved "User prompt (original)" section of PLAN.md. The default is 3: the 3rd, 6th, 9th, and subsequent multiples of three Plan launches perform regeneration. Other phases and prelaunch checks do not count; failed launch attempts do. Resume retains the count and setting; NewRun starts at zero. Interval changes apply to the cumulative count from the next launch without resetting it.

- Read the original text block and rebuild tasks and milestones using current artifacts, progress, and findings. Preserve all text outside the JSON block and every other JSON field. Retain existing IDs, required conditions, dependency ordering, and milestone acceptance and membership while refining, adding, or reordering tasks. Required changes to user requirements cause NeedsInput.
- Worker input includes plan_attempt_number, regenerate_plan, and original_prompt. The designated Plan writes a complete plan JSON to output_directory/regenerated-plan.json alongside its normal result and changes. Workers never edit PLAN.md directly. A candidate is required for ready/recover; missing, invalid, or requirement-changing candidates cause Error and leave PLAN.md unchanged. Do not apply candidates from replan/needs_input.
- The runner checks Schema, references, cycles, preservation of requirements, milestone coverage of required tasks, consistency with the internal plan, and the original file hash. A missing original prompt stops with NeedsInput before launching the designated Plan. An insufficient prompt makes the Worker return needs_input. Never reconstruct the original by guessing.
- Record old and new versions and the Plan count in history. Persist plan_publication with accepted state before atomically replacing only the JSON block. Resume finishes interrupted publication without applying it twice. External-edit conflicts cause Error without overwriting the edit. Reject NewRun while publication is pending.
- Changed PLAN content invalidates evidence and audit approvals. Retain artifacts, findings, unverified Work, and recovery targets, then reevaluate through Plan. Regeneration alone does not prove progress or completion and does not reset runtime, launch counts, or stall counters. Record acceptance of an identical JSON candidate without changing the file.
- Persist plan_attempts and plan_regeneration_pending internally. After failure, interruption, or a pending user decision, continue regeneration on the next Plan launch. For older states, reconstruct the count from saved Plan attempt records and default a missing interval to 3.

## 6. Stopping and Document Size

### 6.1. Stalls and limits

Progress means concrete advancement accepted by Verify, such as implementation, verification, or identifying a cause. Update counters once per attempt; Resume must not count it again.

| Counter | Increment and reset | Action |
| --- | --- | --- |
| NoProgressWorks | Increment when Work is accepted through Verify without progress; reset to 0 on progress | Plan at 2; Stalled at 4 |
| WorklessPlanReturns | Increment on each automatic return to Plan without Work since the previous Plan; reset at Work start | Stalled at 3 |
| AuditRevisions | Count Audit revisions before Work starts; reset at Work start | Plan at 2 |

Recovery or reverification that adds newly valid completed tasks or resolves required findings also resets WorklessPlanReturns. Reusing the same evidence or rewording a plan does not.

Initial and Resume entry into Plan neither increments WorklessPlanReturns nor clears its saved value. Changing task IDs or plans alone does not reset counters. Starting Work alone does not reset NoProgressWorks.

Normal Resume or higher limits do not clear Stalled. After fixing its cause, use ResetStallCounters with ResetReason to reset only the three stall counters; preserve cumulative time and total attempts. Evaluate stalls before ordinary limit exhaustion.

### 6.2. Run status

| Status | Exit code | Meaning |
| --- | --- | --- |
| Complete | 0 | Overall completion accepted |
| Paused | 2 | Time or attempt limit, user interruption, or phase timeout |
| Blocked | 3 | Waiting externally, with no independent runnable tasks |
| NeedsInput | 4 | Awaiting a decision, such as a requirement change |
| Stalled | 5 | Repeated work or planning without progress |
| Error | 6 | Launch, CLI, format, persistence, consistency, or other failure |

Use Running during execution. Paused means a resumable state was saved; it does not guarantee progress on resume. Do not automatically retry errors. An inability to return an exit code, such as forced termination, does not mean completion.

### 6.3. Concise documents

**Keep documents as short as possible while preserving the accuracy needed for decisions.**

| Document | Required content |
| --- | --- |
| PLAN.md | User definitions only |
| Execution plan | Target IDs, changes, scope, steps, verification, time allocation |
| Work report | Per-task results, evidence references, unperformed work, blockers, next actions |
| Update proposal | Base internal plan version and changed types, IDs, and fields |
| Audit result | Decision, unresolved findings, resolution conditions |

Use at most five summary bullets by default. Reference definitions and evidence by ID; do not duplicate logs, code, background, or history. Retain relevant failures and unverified items. Exceed the length guideline when accuracy requires it.

Give Workers the necessary targets, dependencies, findings, diffs, and evidence references. Share the PLAN.md snapshot and manifests instead of copying them into each document. Read history bodies only when needed.

### 6.4. Shared prompt rules

Include these instructions in every Worker's input, along with phase-specific objectives, allowed operations, and Schema:

- Perform only the assigned phase; do not launch the next Worker. Follow higher-priority instructions and permission limits.
- Treat PLAN.md as read-only. Use current internal plans and accepted records, distinguishing them from history. Resolve gaps by reading references, not guessing.
- Edit only allowed paths and preserve unrelated changes. Follow role and acceptance authority rules in §3 and §4.
- Do not weaken or omit required conditions to claim completion. If a decision is needed, return a concrete proposal and needs_input.
- Report per-task results. Partial success, unperformed checks, skips, or missing evidence must not become overall success. Handle evidence under §5 and progress under §6.1.
- Stop work by the start of the save reserve. Save partial artifacts, unverified items, blockers, and next actions before the deadline.
- Follow §6.3 for writing and §8.1 for result identifiers, format, and updates.

Shared instructions may be a separate file or embedded in phase prompts. Expand the required rules when generating input; do not repeat the entire specification each time. Verify the actual input the runner sends to every Worker.

## 7. run.ps1 Interface

### 7.1. Arguments

Run with PowerShell 7.4 or later (`pwsh`). The names, types, and behaviors below define the initial public interface. See the [usage guide](USAGE.en.md) for supported environments and verification status.

| Argument | Type | New-run default | Behavior and constraints |
| --- | --- | --- | --- |
| ProjectRoot | string | Caller's current directory | Existing project root containing PLAN.md |
| MaxRunMinutes | int | 480 | Cumulative active runtime limit, including time before resume, in minutes |
| Resume | switch | false | Resume a saved run |
| NewRun | switch | false | Explicitly start a separate run even if state exists |
| MaxPhaseAttempts | int | 100 | Cumulative Worker launch limit; failed launches count |
| PhaseTimeoutMinutes | int | 60 | Maximum time from each Worker's launch to exit |
| SaveReserveMinutes | int | 5 | Handoff time reserved before each Worker's deadline |
| StopTimeoutSeconds | int | 60 | Wait limit for termination checks and recovery operations, not the entire shutdown |
| MaxTasksPerWork | int | 3 | Maximum tasks per Work; 1–3 |
| CompletionAuditInterval | int | 3 | Work attempts accepted through Verify between completion audits |
| PlanRegenerationInterval | int | 3 | Regenerate PLAN.md from the original prompt at multiples of this Plan launch count |
| CodexCommand | string | codex | Command name or CLI launcher path, without extra arguments |
| PhaseModels | hashtable | Empty | Phase-to-model mapping; unspecified phases use CLI settings |
| ResetStallCounters | switch | false | Clear only stall counters on Resume |
| ResetReason | string | Unset | Required, nonempty reason when resetting counters |

Numbers must be positive integers. MaxTasksPerWork must be 1–3, and SaveReserveMinutes must be less than PhaseTimeoutMinutes. Zero and negative values do not enable unlimited runs. Resume and NewRun are mutually exclusive. ResetStallCounters requires Resume and ResetReason; reject ResetReason by itself.

PhaseModels accepts only Plan, Prepare, Audit, Work, Verify, and CompletionAudit as keys, with nonempty model IDs. An explicitly supplied table replaces the saved table. Pass hashtables by calling the script directly within PowerShell; do not implicitly convert JSON strings.

For a listed phase, pass `--model <model ID>` to the Worker's codex exec invocation. For phases absent from the table, omit the model override and use effective CLI settings. Do not automatically select models by phase. If the CLI has no model setting, use its recommended model; the runner has no hard-coded default model. See §7.5 for an invocation example.

Reasoning effort is separate from model IDs and uses the CLI's `model_reasoning_effort` setting. The runner does not override reasoning effort, and PhaseModels does not change it. Public arguments for per-phase reasoning effort are not implemented. Supported values such as `low`, `medium`, and `high`, and defaults when unset, follow the CLI and model.

### 7.2. Paths, launch, and I/O

Resolve ProjectRoot relative to the caller to an absolute path of an existing directory. Do not search parents for PLAN.md or create the root automatically. Workers and verification commands also use ProjectRoot as their working directory.

Resolve shared framework files relative to run.ps1. Resolve CodexCommand through PATH or relative to the caller. Support .exe, .cmd, and .ps1; reject aliases and functions. Do not launch paths or arguments containing spaces or special characters through string concatenation or Invoke-Expression.

Send prompts through stdin. Use separate files for final results, events, and stderr. Stream logs to disk instead of keeping them entirely in memory. Confirm no child processes remain before accepting results.

Record the original CLI or launcher exit code; do not replace every nonzero exit with 1.

Before starting a run, validate arguments, PLAN, shared files, the CLI path, saved state format for Resume, and exclusive access. Fail with Error without overwriting existing state. Check the CLI version and features after saving the started state, within the run budget; persist failures and elapsed time as Error. Do not change authentication or permissions automatically.

### 7.3. New runs and Resume

| Request and saved state | Behavior |
| --- | --- |
| Neither switch; no state or previous Complete | Preserve history; start at Plan with a new run_id |
| Neither switch; incomplete, corrupt, or unsupported state | Error; direct the user to Resume or NewRun |
| Resume; supported state exists | Recover and enter Plan with the same run_id and cumulative totals |
| Resume; missing, corrupt, or unsupported state | Error; never silently start a new run |
| NewRun | Archive old state; start with a new run_id and fresh totals |

On Resume, omitted numeric settings, CodexCommand, and PhaseModels inherit saved values; change only explicit arguments. Distinguish them with PSBoundParameters and record changes. Do not inherit ProjectRoot or operation switches.

ProjectRoot and project_id are fixed within a run. NewRun must still confirm old processes have stopped and recover unsettled artifacts. It must not inherit completion judgments from corrupt or unsupported state.

NewRun must also recover accepted Work that has not passed Verify. Save a reference to the before manifest in the attempt directory, and inspect partial artifacts before repeating work.

Resuming Complete rechecks current signatures and evidence within the remaining time. Exit 0 if valid; otherwise return to Plan. Stalled requires the explicit reset in §6.1.

Missing or corrupt completion evidence requires a fresh audit; clear suppression of the old duplicate audit. If time or attempts are exhausted, save Paused so the run can resume after its limits are raised.

### 7.4. Runtime limits and stopping

MaxRunMinutes is cumulative for a run_id. After using 480 minutes, resuming with 720 leaves about 240 minutes.

At the limit, stop new Worker launches and forcibly terminate active Workers and their child processes. Do not wait indefinitely for natural completion. SaveReserveMinutes instructs Workers to save and hand off before the deadline; terminate at the deadline even if a Worker ignores that instruction.

Count active time from lock acquisition through shutdown, including Workers, checks, and saves. Exclude time spent stopped after a normal shutdown. Use a monotonic clock. Save cumulative time, UTC timestamp, and an active flag at phase boundaries and processing checkpoints approximately every 20 seconds. Synchronous I/O or OS waits may delay saving and status output.

If forced termination leaves the end time unknown, conservatively add the nonnegative interval from the saved timestamp to confirmed process termination on resume. Record the estimated interval and avoid double counting. If it overstates actual runtime, increase the cumulative limit as needed.

A Worker's deadline is the earlier of the overall remaining time and PhaseTimeoutMinutes, with SaveReserveMinutes reserved inside it. Do not launch if remaining time is no greater than the reserve. The launch-attempt limit must not prevent acceptance and saving of results already in progress.

Apply the overall time limit to runner operations too. Save timed-out captures or environment checks as incomplete and recover on resume. Prioritize accepted-record reconciliation and process termination checks. If too little time remains, return Paused without launching a Worker.

At a deadline or Ctrl+C, stop new launches and attempt child termination and recovery-state saving. StopTimeoutSeconds limits each termination check or recovery operation, not the entire shutdown. Return Paused on success or Error if termination cannot be confirmed; leave the attempt unsettled if shutdown cannot finish. Capture incomplete after manifests on resume. Shutdown time counts toward total runtime.

### 7.5. Examples

See QuickStart for starting, resuming, and changing limits. To reset stalls or select a model by phase:

```powershell
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume -ResetStallCounters -ResetReason 'Clarified completion criteria'

# Pass a hashtable directly from PowerShell
& ./autoframe/run.ps1 -ProjectRoot . -Resume -PhaseModels @{ Audit = '<available model ID>' }
```

## 8. Result Contract and Implementation Plan

### 8.1. Result acceptance

Common result fields are schema_version, run_id, attempt_id, phase, input_plan_hash, base_plan_version, execution_plan_hash, execution_plan, decision, route_hint, summary, task_results, changes, findings, evidence, and progress.

phase uses the names in §7.1. route_hint is null, continue, or replan and only affects routing after Work. Return the input execution_plan_hash, or null if absent. execution_plan is required for Prepare ready and Audit approved; other decisions in those phases may return null. All other phases return null. The runner stores the returned plan's hash and rejects unknown fields.

task_results contains IDs, status, evidence IDs, and remaining work. Blocked results also include a reason and release conditions. Work returns pending, implemented, or blocked; Verify returns pending, verified, or blocked. Both must report exactly the target IDs, without omissions, duplicates, or extras. Other phases may propose only pending or blocked for affected tasks.

Each task_results[].evidence_ids entry must exist in the current result or records and include the exact task ID in target_ids. Non-verified results allow work, task, or progress evidence, but not plan, criterion, or finding evidence. Use an empty array when no reference is needed. Verified results require task evidence produced by the current Verify attempt.

Plan returns task_results=[] when it only creates a plan. recover must identify verification targets explicitly. Save planning notes as standalone kind=plan evidence. Reference errors identify the phase, task, evidence ID, and whether the ID is missing, the target mismatches, or the kind is invalid. Never relabel evidence merely to pass acceptance.

Only Work and Verify may report progress, and only for the current trial targets. Other phases must return an empty array. Record planning changes in summary, changes, and plan evidence. Violations identify the phase and target in the error message.

changes is a list of diffs with kind (task/milestone/finding), id, and set. Omit unchanged targets from the list. Return every Schema field for that kind in set, using null for unchanged values. New tasks need all required definitions. task_results.blocker must also be null unless status is blocked.

findings contains new findings. evidence carries IDs, kinds, relative paths, SHA-256 hashes, target IDs, and input signatures. Use [] for inapplicable arrays. Validate types and required keys against the [result Schema](schemas/result.schema.json).

Apply status through task_results and definitions through changes. Only Plan changes definitions or replacements; Audit resolves plan findings; Verify and Completion Audit resolve product findings.

The runner checks CLI and child termination, Schema, input identifiers, versions and hashes, targets, update authority, evidence, and before/after diffs. Accept once under §3.1. Invalid results cause Error while preserving partial artifacts for recovery. Handle external PLAN changes under §5.4; never infer state from prose or stale results.

### 8.2. Implementation order and acceptance

| Order | Deliverables | Validation |
| --- | --- | --- |
| 1 | PLAN, internal record, and result Schemas; loading; ID and dependency checks | Reject invalid types, duplicate keys, cycles, and unknown references; loading does not modify PLAN |
| 2 | Locking, persistence, versions, history, and manifests | Interruptions at every save boundary cause no duplicate acceptance; handle additions, deletions, links, and read failures |
| 3 | Pure transition logic; stall and budget checks | Deterministic success, revision, partial success, loops without Work, and recovery |
| 4 | Worker adapter, time limits, and process termination | Simulated CLI tests for stdin, results, large logs, launch failures, remaining children, and timeouts |
| 5 | run.ps1, prompts, Resume, and PLAN regeneration | Test settings inheritance, NewRun, verbatim prompts, regeneration cadence, interrupted publication, and conflict rejection |
| 6 | PLAN template, README, and integration tests | Temporary projects for different use cases cover completion, stopping, evidence expiry, and archiving |

Use simulated Workers for default tests. Run real Codex only in explicit smoke tests. Check the installed version's arguments, authentication, structured output, and termination. Simulated tests alone do not establish real CLI support. Do not run NativeAOT in default tests.

Final acceptance checks must cover the final Work's Verify, expired dependency evidence, missing environment information, audit deduplication, preservation of required findings, limits during recovery, result acceptance during heartbeats, and inability to bypass stalls with normal Resume. Also check that every generated prompt includes §6.4.

Record framework tests for all phases, real CLI connectivity, and real-model workflow tests separately, including limitations. Internal records provide launch counts, runtime, phase durations, and revisions. Model usage is available only through events supplied by the CLI; the runner does not aggregate it separately.

CLI integration reference: [official non-interactive execution documentation](https://learn.chatgpt.com/docs/non-interactive-mode). Check exact flags against the installed version.

## Appendix: Build Instructions

```text
Use autoframe/SPEC.md as the authoritative specification to implement the automation
framework in <destination>/autoframe/.

Follow the order in §8.2. Create PowerShell scripts, shared and six-phase
prompts, JSON Schemas, a PLAN template, simulated-Worker tests, and brief run
instructions. Follow the shared rules in §6.4 and public interface in §7.
Keep project-specific logic out of the scripts.

Inspect existing files and update them as needed. Be accurate and concise.
Separate real CLI smoke tests from simulated-Worker tests, and state what
remains untested. Do not run NativeAOT, create a PLAN.md for product work,
or start a production run.
```

The runner must be reusable across projects. This specification must be sufficient without existing prompt files.

[qs-runner]: USAGE.en.md
[qs-verification]: VERIFICATION.md
[qs-template]: templates/PLAN.template.md
[qs-create-plan]: prompts/create-plan.md
[qs-spec]: SPEC.en.md
