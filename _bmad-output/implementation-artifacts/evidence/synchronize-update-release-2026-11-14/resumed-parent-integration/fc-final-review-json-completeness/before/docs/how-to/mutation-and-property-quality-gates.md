---
title: Mutation and Property Quality Gates
description: Run and interpret the Story 10-4 mutation and command idempotency property gates.
genre: how-to
audience: framework-contributor
ownerStory: 10-4-mutation-testing-and-property-based-testing
status: active
reviewed: 2026-05-10
uid: hfc.howto.mutation-property-quality-gates
---

# Mutation and Property Quality Gates

Story 10-4 adds two authoritative nightly gates and one local replay path for contributors.

## Local Commands

Restore pinned tools and packages:

```bash no-compile reason="developer shell command"
dotnet tool restore
dotnet restore Hexalith.FrontComposer.slnx
```

Run the deterministic command idempotency property suite:

```bash no-compile reason="developer shell command"
pwsh ./eng/run-lifecycle-property-suite.ps1 -MaxTest 1000 -Replay "15485863,32452843,0"
```

Omit `-Replay` for a random nightly-style seed. The script writes the actual seed and replay command to `artifacts/property/property-seed-summary.md`, plus structured max-size, sequence-count, operation-distribution, shrink-policy, and replay-command evidence to `artifacts/property/property-run-evidence.json`.

Both segment configs are project-scoped: they pin `project`/`test-projects` to the SourceTools graph and build under `Release`, with no umbrella `Hexalith.FrontComposer.slnx` Stryker build. The private runner helper constructs the same explicit project, test-project, config and output arguments and starts each segment in the SourceTools source directory. This avoids Stryker's automatic solution selection beside a root `.slnx`.

The mutation gate uses the distinct private version `5.1.0-hexalith.compat.1`, built from official Stryker 5.1 commit `7052fa6c646a6bdfb143608e695d11542e2be815` plus the checked-in static-host isolation patch. Its [definition, patch and license](../../eng/stryker-compat/README.md) authenticate the archive, exact source changes, SDK, locked package inputs, HTML resource and compiled assembly identity. The `.config/dotnet-tools.json` 5.1.0 entry identifies the official base; the gate executes the authenticated private build. No third-party package is published by this workflow.

Use Linux, the workspace's `run-with-temp` command, and a clean source checkout. Preparation and both segments must share **one** wrapper invocation because the private executable and caches live in its scratch directory. Useful reports remain under the checkout's `artifacts/mutation` directory. CI uses its job's `RUNNER_TEMP` for the same lifetime.

```bash no-compile reason="mutation run is long-running and environment-specific"
run-with-temp bash -c '
  set -euo pipefail
  export UseMicrosoftTestingPlatformRunner=true
  python3 eng/stryker_compat.py prepare
  hfc_happy_status=0
  python3 eng/stryker_compat.py run happy-path || hfc_happy_status=$?
  hfc_error_status=0
  python3 eng/stryker_compat.py run error-handling || hfc_error_status=$?
  hfc_validation_status=0
  pwsh ./eng/validate-stryker-reports.ps1 -RequireCompatibilityProvenance || hfc_validation_status=$?
  if (( hfc_happy_status != 0 )); then exit "$hfc_happy_status"; fi
  if (( hfc_error_status != 0 )); then exit "$hfc_error_status"; fi
  exit "$hfc_validation_status"
'
```

The runner property builds xUnit 4.0.3 with its MTP entrypoint so Stryker can discover native `--filter` support and capture coverage in the test process. The Happy predicate is unchanged. Error handling selects the union of `MutationErrorHandling` and `MutationErrorRegression`, excluding `Performance`; the regression trait covers adverse assertions and invalid-to-valid recovery cases. Ordinary CI and Quality continue to run the complete test suite.

The compatibility change resets the MTP host before each static or isolation-required mutant and clears its active ID and resets again afterward. Ordinary mutants retain host reuse. This addresses the measured warm-host static false survivor: a fresh process detected the changed command identifiers, while the stock reused process passed those same assertions. Finite known-mutation controls establish runner activation, not a complete segment score.

Each segment records its exact source commit, config blob/content hash, fixed output and invoked DLL/build identity before execution. Strict validation verifies those records against the current checkout and private build, then records each original report SHA and raw source/status inventory. Changing the source requires a fresh run; prior receipts stay attached to their original source.

The commands retain debug phase and test diagnostics in the console log and stop a segment if its unmutated baseline fails. A baseline failure is not a mutation score; both complete reports must still pass validation.

The happy-path config sets `additional-timeout` to 170,000 milliseconds. Pinned Stryker 5.1 estimates MTP test durations from the suite average, which gives the package-consumer test a 30-second coverage-capture limit despite its [105.033-second passing Quality result](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43605722548). Applying the tool's 1.5 duration multiplier and existing 10-second allowance gives 167.5495 seconds, rounded to 170 seconds. This setting also adds 160 seconds to every happy-path mutant deadline compared with the default; it is not a capture-only setting. The error-handling segment uses a 60-second allowance grounded in the [paired-load timing control](../../eng/stryker-compat/README.md#error-timeout-allowance), and the mutation job has a 360-minute bound. The prior complete-cohort diagnostic exhausted 90 minutes; the wider bound accommodates the measured workload without reducing either threshold or test/mutation scope. It supplies no completion guarantee.

Both segments use two concurrent test servers. In [the four-server run](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43612007215), telemetry recorded exhausted available memory and swap, followed by a cgroup OOM-kill counter increase during runner shutdown. The exact victim and signal sender were not identified. Reducing concurrent servers preserves the complete mutation and test cohorts, per-test coverage, thresholds, and strict report validation. The mutation job also runs in the pinned SDK container with four CPUs, 10 GiB memory and no swap; its child-process resource limit is separate from the hosted Runner. Earlier unexplained Runner losses retain their original unknown cause. Full reports and both segment exits still determine the gate result. The wrapper executes the mutation tool directly in the original process group and session, samples process and cgroup resources, forwards cancellation, and reaps its owned children. It does not use ptrace: a prior private Error207 diagnostic stopped when strace failed during a normal test-server kill/restart, before producing a report. That diagnostic supplies no mutation score.

## Thresholds

The happy-path SourceTools Parse/Transform segment is blocking at 80 percent. The error-handling segment is blocking at 60 percent. These are separate gates; a blended score is not accepted as evidence.

## Survivor Triage

Pinned Stryker 5.1 records compiler rollback candidates before applying file filters. Strict validation classifies only `CompileError` with the exact reason `Mutant caused compile errors`, a numeric ID and present empty coverage/kills arrays as nonexecuted invalid candidates. It retains their raw statuses and IDs, excludes them from live scope/minimum counts, and refuses unknown or execution-bearing shapes. The authenticated private patch leaves this upstream compiler behavior unchanged.

Every `Survived`, `NoCoverage`, `Timeout`, and remaining `CompileError` mutant needs one action:

- `kill-test-added`: a focused test now kills the mutant.
- `equivalent-accepted`: the mutant is equivalent or non-actionable, with rationale.
- `deferred-with-owner`: the gap is real and has an owner/follow-up story.
- `blocking`: the gate cannot pass until the mutant is addressed.

Record accepted or deferred problem mutants in `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json` under `problemMutantTriage`; validation fails when report counts exceed reviewed triage entries.

## Property Counterexamples

FsCheck failures must retain the package version, seed, size or max size, sequence count, operation distribution, shrunk sequence, and replay command. Confirmed bug or protected-invariant counterexamples go in `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Lifecycle/Fixtures/command-idempotency-counterexamples.json`.

Do not commit transient non-bug seeds. Fixtures must use synthetic correlation, tenant, user, and message values only.

## Artifact Allowlist

Allowed artifacts are bounded Stryker HTML/JSON reports, mutation validation summaries, FsCheck TRX files, seed summaries, and sanitized failure envelopes. Artifacts must not contain secrets, bearer tokens, environment dumps, full payload bodies, tenant/user identifiers, machine-local paths, or unbounded generated source.

## Oracle Contract

The lifecycle property oracle runs each generated sequence against fresh `LifecycleStateService`, deterministic time, a fresh transition subscriber set, and a fresh reference evaluation. It compares final lifecycle state, message ID, idempotency flag counts, transition trace, visible terminal notifications, warning counts, and invalid-transition counts between original and replay.

The authoritative gate status is nightly blocking. PR smoke may provide a fast advisory signal only; it cannot replace the nightly thresholds or suppress mutation/property evidence. Story 10-5 owns any quarantine or CI diet policy.
