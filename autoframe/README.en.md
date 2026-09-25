# autoframe 0.1

A Windows framework that organizes Codex work into planning, auditing, implementation, and verification. [日本語](README.md)

Define the objective and completion criteria in `PLAN.md`. The runner repeats `Plan → Prepare → Audit → Work → Verify` and returns `Complete` when the completion audit confirms all criteria.

## Getting started

### 1. Install

Prepare Windows, PowerShell 7.4 or later, Codex CLI, and the required development tools. Copy the entire `autoframe/` folder into the target project. Existing authentication and permissions apply.

If using Git, add `.autoframe/` to `.gitignore`. It stores execution state and evidence; keep it when resuming.

### 2. Create PLAN.md

Ask Codex using the prompt below, or use the [template][template].

```text
Follow autoframe/prompts/create-plan.md to create PLAN.md at the project root.
Objective: <what to achieve>
Scope: <features or folders>
Completion criteria: <conditions for finishing>
Non-goals and constraints: <what to preserve or prohibit>
Create PLAN.md only; do not start automatic execution.
```

### 3. Run or resume

Review PLAN.md, then run from the target project root.

```powershell
# Start
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot .

# Resume
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume

# Resume with the cumulative time limit increased to 720 minutes
pwsh -NoProfile -File ./autoframe/run.ps1 -ProjectRoot . -Resume -MaxRunMinutes 720
```

Defaults are 480 cumulative minutes, 100 Worker launches, and 60 minutes per Worker. Resume retains time, counts, and settings. Use `-NewRun` for a new execution; it cannot be combined with `-Resume`.

Every third Plan launch regenerates tasks from the saved original user prompt. Change the interval with `-PlanRegenerationInterval`.

## File handling

These five directories are default generated outputs in every project, at any depth, even when omitted from PLAN:

`.vs/`, `bin/`, `obj/`, `TestResults/`, `BenchmarkDotNet.Artifacts/`

Declare additional outputs in PLAN's `generated_scope`. Instruction files and the framework remain protected, and required artifacts remain subject to verification. Update PLAN or the distribution while the runner is stopped. Do not manually edit internal `.autoframe/` state.

## Status and stopping

About once a minute, the runner shows `elapsed` (current Worker runtime), `total_elapsed` (cumulative runtime), and `remaining` (remaining total budget). The log's `Trial` entry identifies the record directory.

Time limits and Ctrl+C stop the Worker and its children, normally returning `Paused`. Other statuses are `Blocked` for external dependencies, `NeedsInput` for user decisions, `Stalled` for lack of progress, and `Error` for failures. See the [specification][spec] for stall resets and troubleshooting.

Protected changes are diagnosed with file paths. For .NET, Workers check dependency configuration in their own environment and record the cause and release conditions for affected tasks.

## Model and reasoning effort settings

Models use Codex CLI settings. Use `-PhaseModels` for model IDs by phase. On Resume, omitting it retains saved assignments; an explicit table replaces the entire saved table.

Reasoning effort uses the CLI's `model_reasoning_effort` setting. autoframe has no per-phase reasoning effort argument.

## Details

[Usage guide][usage] · [Specification and all arguments][spec] · [PLAN creation prompt][create-plan] · [Verification record][verification]

[template]: templates/PLAN.template.md
[usage]: USAGE.en.md
[spec]: SPEC.en.md
[create-plan]: prompts/create-plan.md
[verification]: VERIFICATION.md
