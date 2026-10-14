---
title: 'DW-499/DW-502 EventStore command recovery integrity'
type: 'bugfix'
created: '2026-10-14'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: b58da5f0846c0c78ccfc71c82052056fca0f44f5
eventstore_baseline_commit: 173ad7f4e56a72f44621c7d3d7be86a5ec19dc03
context:
  - /home/administrator/projects/hexalith/tenants/AGENTS.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore/AGENTS.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-state-instructions.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore/docs/architecture/command-recovery-integrity.md
---

<frozen-after-approval reason="user authorized this complete bundle and persistence on 2026-10-14">

## Intent

Repair command recovery in the owning EventStore repository: durable terminal advisory repair after failed writes/crashes, and refusal of unverifiable persisted resume ranges before publication. Implement the recorded [owning contract](../../references/Hexalith.EventStore/docs/architecture/command-recovery-integrity.md). Existing specifications settle the choices; no unresolved policy decision was found. The user's request authorizes implementation without another permission checkpoint.

## Boundaries & Constraints

Always preserve exact command identity, original event range, actor-scoped evidence, logical terminal result, advisory non-authority, opaque admission replay and other owners. Use actor-state persistence/reminders/remediation. Missing legacy evidence remains unverifiable. One C# object/type per file. Work in EventStore; never load or discover skills inside references/. Inspect owning build/config/test guidance before edits.

Never reconstruct proof from aggregate head, add direct backend access, update dependencies/gitlinks, stage, commit, branch, push, deploy, replay production or migrate data. Preserve inherited B1–B7 and concurrent external commit/pointer movements and the historical gitlink blocker. Root maintains workspace evidence/ledger: implementation agent limits edits to EventStore source/tests and relevant documentation; report exact commands and initial failures to root.

## I/O & Edge-Case Matrix

| State | Expected behavior |
| --- | --- |
| Terminal status write fails / crash after terminal save | Cached terminal outcome and original range survive; discoverable repair retries exact persisted status. |
| Crash before terminal commit / ambiguous save acknowledgement | Existing durable inspection/discard resolves checkpoint vs terminal intent without uncommitted publication. |
| Exact terminal duplicate / conflicting identity | Exact pending intent may repair without executing/publishing events; cached result unchanged; conflicts cannot repair. |
| No-op / rejection / legacy terminal without intent | No fabricated proof; established cached behavior retained. |
| Malformed/missing persisted command or envelope identity/scope/sequence/count/causation | Zero publication; checkpoint/events/drain evidence retained; repeat recovery cannot exhaust/delete it. |
| Verified range / restored exact evidence | Existing successful resume and drain continue with original command range. |
| Other commands / other aggregates | Their persisted evidence and recovery ownership remain intact. |

</frozen-after-approval>

## Code Map

Paths below are relative to EventStore root `references/Hexalith.EventStore`.

- `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs`: duplicate return around861; exact checkpoint resume929; activation141; drain2977–3240; stale handoff4497; resume4622; terminal6660; proof6582. Guard resume, handoff, activation and drain before side effects. Reuse cache barriers/save ambiguity inspection. Keep unrelated actor flow.
- `PipelineState.cs`, `CommandProcessingIdentity.cs`, `IdempotencyChecker.cs`, `UnpublishedEventsRecord.cs`, `UnpublishedPublicationIndex.cs`: exact identity/range, immutable recovery/accounting. Avoid changing opaque admission or public records needlessly.
- `AggregateActor.cs` ambiguity seams: `InspectIdempotencyCleanupSaveFailureAsync` and successful `InspectDrainCleanupSaveFailureAsync` must verify exact repair intent/index alongside existing invariants; exhaustion shares drain inspection and must not require a terminal-success intent. Failed intent cleanup discards staged cache and inspects both intent/index, preserving unrelated intents. Guard Int32 range arithmetic in current reader. Event envelope MessageId is distinct from command MessageId.
- New focused actor partial and intent/index types: stage repair in terminal/drain cleanup transaction; bounded activation rearm, reminder retry and exact-duplicate pending repair. Use distinct reminder/index from publication ownership.
- `tests/Hexalith.EventStore.Server.Tests/Actors/StateMachineIntegrationTests.cs`: malformed resume currently expects completion399, missing causation515; replace superseded expectations with zero-publication/evidence assertions, correct valid fixture causation.
- `tests/Hexalith.EventStore.Server.Tests/Actors/EventDrainRecoveryTests.cs`: malformed proof342, legacy mismatches414; preserve genuine valid legacy evidence path and refuse unverifiable recovery. Existing persisted/fault/recreation helpers.
- `EventPublicationIntegrationTests`, `PublicationRecoveryActivationTests`, `FaultInjectingActorStateManager`: original-range vs advanced-head, observing terminal write after durable cleanup, bounded activation and before/after-commit faults. Successful advisory intent cleanup adds one actor save; adjust only specific obsolete save-count assertions with rationale.
- `tests/Hexalith.EventStore.Server.Tests/TestUtilities`: transactional/boundary failure state managers; `Hexalith.EventStore.Testing.Fakes.InMemoryStateManager` stores durable contents. Use meaningful failure/end-state regressions; avoid tests that merely mirror private helper calls.
- `docs/reference/command-api.md`, `docs/concepts/command-lifecycle.md`, `docs/guides/disaster-recovery.md`, architecture AD-1/5/8/12: original advisory/identity/refusal/restore contract; link bounded repair/refusal behavior without readiness claims.

## Tasks & Acceptance

- [ ] EventStore actor repair types/partial and terminal/drain integration — durable intents, bounded retries and matching duplicates.
- [ ] EventStore resume/handoff/activation/drain — verify all persisted evidence before publication or destructive recovery transitions.
- [ ] Actor tests — fault/crash/duplicate/malformed/legacy/success/repeated/end-state and isolation matrix; preserve old assertions except explicitly superseded unsafe expectations.
- [ ] Owning docs — explain repair, refusal and exact-evidence recovery.
- [ ] Root verification — independent review, individual applicable projects, package consumers, unchanged coverage and focused Tenants source compatibility; exact evidence/limits and closure only when proven.

Given a terminal result, when advisory I/O fails or the actor restarts, then its exact pending status remains durably recoverable without domain execution or event republication.
Given unverifiable resume evidence, when any recovery route runs, then zero events publish and original persisted evidence remains; when trusted exact evidence is restored, then normal resume succeeds.
Given other recovery owners, when this command repairs/refuses/completes, then their state and ownership remain unchanged.

## Implementation Notes

Entry state and external movements are in `b8-entry-state.json`; tracked hashes are retained under `/tmp/hexalith-b8-20261014/entry-state.json`. Root handles canonical ledger/readiness changes after verification. No sprint/archive change. Implementation verification should begin with the focused actor project build/tests and report exact failure logs.

## Spec Change Log

## Review Triage Log

## Verification

Use individual `dotnet build/test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj` first, Release, warnings as errors; focused actor filters then full project. Root runs applicable remaining lanes and package/coverage/Tenants gates after implementation.
