# autoframe Usage Guide 0.1

Uses Windows, PowerShell 7.4 or later, a supported Codex CLI, and existing authentication and permissions. Define objectives, completion criteria, and work scope in PLAN.md at the project root. The [Japanese version](USAGE.md) is authoritative.

## Installation and execution

Copy this entire directory into a project, or keep a shared installation and specify ProjectRoot. Update it while the runner is stopped, preserving the project PLAN.md and `.autoframe/` records.

See [SPEC QuickStart](SPEC.en.md#quickstart) for plan creation, start and resume examples, defaults, stopping, and status output. A [template](templates/PLAN.template.md) and [creation prompt](prompts/create-plan.md) are also available.

Ordinary Plan phases update the internal plan. By default, on Plan launches 3, 6, 9, and so on, the runner regenerates tasks and milestones in PLAN.md while preserving the original prompt and requirements, then revalidates evidence. Resume retains settings, counts, and runtime; NewRun starts a new run. Neither skips checks of unverified artifacts.

## Records and results

`.autoframe/state.json` and its references are the current state. Do not edit internal state manually.

| Location | Contents |
| --- | --- |
| records/ | Accepted results, internal plans, evidence, and history |
| manifests/ | File records shared by content hash |
| runs/<run_id>/<attempt_id>/ | Per-attempt input.json, prompt.md, before.json, after.json, and metrics.json |
| worker/ within that directory | events.jsonl, stderr.log, and process termination records |
| output/ within that directory | Worker result.json, evidence, and candidate JSON for periodic regeneration |

Keep before.json references after acceptance for recovery, including NewRun. Save records before committing state; never infer acceptance from unreferenced records. Heartbeats do not change the internal plan version.

The [result Schema](schemas/result.schema.json) defines Worker output. Evidence paths are relative to output_directory; hash is SHA-256 and input_signature uses the supplied signature. Use null for unchanged changes.set values and an inapplicable blocker. List required artifacts in internal task deliverables. Follow [SPEC §8.1](SPEC.en.md#81-result-acceptance) when distinguishing Plan notes from task evidence.

Workers inherit existing model, authentication, and permission settings. They need permission to write evidence to output_directory; the runner does not add permission-expansion flags. CLI connectivity alone does not establish this permission or completion of all six phases.

Windows Job Objects manage child processes; other operating systems return Error before launch. File-diff checks do not replace OS write restrictions or record every operation. See [SPEC §5](SPEC.en.md#5-diffs-evidence-and-recovery) for paths, evidence, and recovery, and [SPEC §7](SPEC.en.md#7-runps1-interface) for public arguments.

## Verification

```powershell
# Framework tests with temporary projects and simulated Workers
pwsh -NoProfile -File ./autoframe/tests/test.ps1

# Real CLI smoke test: no tools, read-only
pwsh -NoProfile -File ./autoframe/tests/smoke-real-cli.ps1 -RunRealCli
```

Failed simulated tests retain their temporary directories. `-KeepFixtures` also retains successful fixtures; `-Filter '*name*'` selects individual tests. Real CLI connectivity is a separate test and does not require a product PLAN or production run. Record results and limitations in [VERIFICATION.md](VERIFICATION.md).
