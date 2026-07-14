---
title: 'EventStore benchmark data and strict scheduled performance lane'
type: 'feature'
created: '2026-07-14'
status: 'in-review'
baseline_commit: '968a993f15c23dcfb1b4735e846599b9248d04af'
baseline_eventstore_commit: 'df06eceaee781b2ba0d991cf80a60e06eb25e3f6'
baseline_builds_commit: 'f83df11d8b324211fd913ff08880fcfeef04c45c'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md'
  - '{project-root}/_bmad-output/implementation-artifacts/investigations/long-running-performance-test-investigation.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenants builds benchmark history through 500,000 full command turns, while the shared scheduled job can select performance tests without enabling their EventStore gate and still pass when all selected tests skip.

**Approach:** Add a domain-neutral EventStore test helper that prepares production-readable aggregate histories through DAPR actor-state transactions. Harden Hexalith.Builds to enable performance gates, reject empty/all-skipped execution, and retain structured evidence.

## Boundaries & Constraints

**Always:** Keep the helper in `Hexalith.EventStore.Testing.Integration`; accept caller-supplied identities, payloads, deterministic metadata, and optional snapshot state; use bounded transactions on the official target-actor state endpoint; reserve one global-position range; validate before writing; prove production rehydration compatibility; keep Builds caller-compatible.

**Ask First:** Production EventStore API/actor changes, raw state-store access, new packages, weakened protection/write-once rules, or any commit/push/release.

**Never:** Reference Tenants contracts; modify Tenants in this scope; compose physical Redis keys; add a production seeding endpoint; overwrite an existing stream; tune snapshots; claim NFR13 compliance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Behavior | Error Handling |
|---|---|---|---|
| Valid plan | Fresh actors; valid events/snapshot | Production streams plus one monotonic position range | Return counts, range and timings |
| Invalid/existing | Bad invariant or metadata already present | No allocation, write, or overwrite | Name the invariant without payload data |
| Transaction failure | Allocation/write fails | No success receipt | Support-safe error; cleanup remains consumer-owned |
| Scheduled result | TRX present | Execute at least one test; summarize and upload evidence | Fail distinctly for missing/no-match/all-skip |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/Benchmarking/` -- seed inputs, builder, validator and receipt.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/{Events,Actors}/` -- unchanged persisted formats and global-position actor.
- `references/Hexalith.EventStore/tests/{Hexalith.EventStore.Testing.Integration.Tests,Hexalith.EventStore.Server.LiveSidecar.Tests}/` -- contract and production-reader proofs.
- `references/Hexalith.Builds/.github/workflows/domain-ci.{yml,md}` -- strict scheduled execution and documentation.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/Benchmarking/*.cs` -- implement validation, one range allocation, bounded actor transactions, metadata-last visibility, overwrite guard, read-back validation, cleanup and receipt.
- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Integration.Tests/Benchmarking/*.cs` and `tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Benchmarking/*.cs` -- cover the matrix; seed a small Sample stream and prove production reads, snapshot+tail state, and next sequence `N+1`.
- [x] `references/Hexalith.Builds/.github/workflows/domain-ci.yml` -- set EventStore/Tenants opt-ins only on the scheduled performance step; always parse TRX into `performance-test-summary.json`; reject missing/no-match/all-skip; upload the entire performance directory with missing files treated as errors.
- [x] `references/Hexalith.Builds/.github/workflows/domain-ci.md` -- document strict execution and evidence ownership.

**Acceptance Criteria:**
- Given a valid fresh plan, when seeding completes, then one range covers production-format streams and a real actor rehydrates the expected snapshot, tail, and sequence.
- Given invalid or existing state, when seeding is requested, then no destructive overwrite occurs and diagnostics leak no payload.
- Given scheduled selection, when tests finish, then at least one executes and TRX/JSON/consumer reports are retained even on failure.
- Given Tenants adoption is absent, when results are reported, then they are platform readiness—not NFR13 proof.

## Spec Change Log

- 2026-07-14: Live DAPR 1.18.1 validation showed that actor-state transaction writes return `ERR_ACTOR_INSTANCE_MISSING` for a never-activated actor. Refined the helper lifecycle without changing the approved intent: preflight metadata, activate the empty production actor through its read-only metadata method, perform bounded writes while quiescent, explicitly deactivate through the DAPR app actor endpoint, then read back and prove a fresh production activation. Cleanup uses the same activate/delete/deactivate lifecycle. No raw store access or production seeding surface is introduced.

## Design Notes

Use `POST /v1.0/actors/{actorType}/{actorId}/state`, never Redis keys. DAPR requires the target actor instance to exist for transaction writes, so the helper owns an explicit read-only activation and app-endpoint deactivation cycle around each quiescent write/delete phase. Preserve the actor-state JSON shape; a live test guards drift after reactivation. Write events first and snapshot/metadata last. Fresh actor IDs plus explicit deactivation avoid stale actor caches. Bound operation count and serialized bytes.

The Builds guard reads TRX `Counters.executed`; xUnit v3 can emit `NotExecuted` while `Counters.notExecuted` remains zero. Builds adds only an execution summary and retains consumer benchmark reports unchanged.

## Verification

**Commands:**
- `dotnet build Hexalith.EventStore.slnx --configuration Debug --no-restore` -- clean build.
- Focused helper and live-sidecar test projects -- contract and production-reader proofs pass, or the exact DAPR blocker is recorded.
- `actionlint .github/workflows/domain-ci.yml` and `git diff --check` -- workflow and repository diffs are clean.
- Positive, all-skipped, and no-match probes -- positive emits TRX/JSON; negative probes fail distinctly.
