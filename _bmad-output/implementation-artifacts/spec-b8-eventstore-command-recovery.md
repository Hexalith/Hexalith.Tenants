---
title: 'DW-499/DW-502 EventStore command recovery integrity'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
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

- [x] EventStore actor repair types/partial and terminal/drain integration — durable intents, bounded retries and matching duplicates.
- [x] EventStore resume/handoff/activation/drain — verify all persisted evidence before publication or destructive recovery transitions.
- [x] Actor tests — fault/crash/duplicate/malformed/legacy/success/repeated/end-state and isolation matrix; preserve old assertions except explicitly superseded unsafe expectations.
- [x] Owning docs — explain repair, refusal and exact-evidence recovery.
- [x] Root verification — independent review, individual applicable projects, package consumers, unchanged coverage and focused Tenants source compatibility; exact evidence/limits and closure only when proven.

Given a terminal result, when advisory I/O fails or the actor restarts, then its exact pending status remains durably recoverable without domain execution or event republication.
Given unverifiable resume evidence, when any recovery route runs, then zero events publish and original persisted evidence remains; when trusted exact evidence is restored, then normal resume succeeds.
Given other recovery owners, when this command repairs/refuses/completes, then their state and ownership remain unchanged.

## Implementation Notes

Review patches retain positive exact ranges, durable batching, zero invalid publication, cache remediation and every B1–B7/provenance boundary. Fair activation scan cursors are actor-owned control metadata, separate from retained command evidence. No bounds or gates are lowered.

Entry state and external movements are in `b8-entry-state.json`; tracked hashes are retained under `/tmp/hexalith-b8-20261014/entry-state.json`. Root handles canonical ledger/readiness changes after verification. No sprint/archive change. Implementation verification should begin with the focused actor project build/tests and report exact failure logs.

## Spec Change Log

## Review Triage Log

Initial independent review input: `/tmp/hexalith-b8-20261014/review.diff`, 191,625 bytes, SHA-256 `f83fcee08e75c9ef22a72a4771bfe482820392f573e9399719cf8a85a4a9a065`. All three context-free layers reported before triage. No layer skipped. Each finding is classified separately before grouping.

| Finding | Verdict | Evidence / route |
| --- | --- | --- |
| BH1 | false | The recorded contract promises durable discoverability and rearming on activation/exact duplicate, matching existing drain registration semantics; it does not promise autonomous scheduling while the reminder service is unavailable. Registration failure leaves an unarmed intent. Add a registration-dependent recovery test and keep this availability limit explicit. |
| BH2 | low | Confirmed pending index grows linearly during a status outage. A paginated discovery redesign is substantial and the contract does not claim bounded storage/transaction bytes. Actor-state failure preserves the committed checkpoint; no evidence eviction is authorized. Reject that redesign in this bounded repair and retain scalability/provider qualification as a release limit. |
| BH3 | high | Verified ordinary early state reads escape per-entry boundaries and skip later healthy owners. Patch isolated read failure handling while stopping on unsafe/ambiguous state cache. |
| BH4 | medium | Verified activation accepts a coherent intent whose MessageId differs from its indexed state key. Patch exact key-to-intent binding; retain wrong-key evidence. |
| BH5 | high | Verified a coherent Completed receipt can contradict cached.Accepted=false. Patch terminal classification and rejection evidence binding where authoritative cached terminal outcome exists, including reminder recovery. |
| BH6 | false | Activation explicitly degrades rather than throwing, including the pre-existing publication block. Operational command/read/reminder cancellation still propagates. Preserve established activation behavior rather than impose a new activation exception contract. |
| BH7 | medium | Verified legacy idempotency JSON/deserialization corruption is wrapped as provider I/O, unlike event reads. Patch deterministic corruption refusal and evidence retention separately from transient retry. |
| BH8 | false | The existing proof contract trusts the actor-owned checkpoint atomically binding exact MessageId/type to its original range and verifies envelope correlation/causation. The proposed independent witness assumes a coherent forged checkpoint and demands stronger provenance absent from current envelope specs. No cryptographic at-rest or second-writer proof is claimed; AD-5/provider qualification remains open. |
| BH9 | medium | Verified invalid activation/drain comparison branches can be silent without sampled tracing. Patch bounded support-safe owner/reason diagnostics without payload or provider detail. |
| BH10 | medium | Verified ReadEventsRangeAsync allocates count capacity before reading evidence; a numerically coherent huge malformed range can allocate excessively before detecting its first missing event. Patch bounded initial allocation and avoid duplicate activation verification. Entry budgets do not claim a byte/time limit for a genuinely large verified range; retain NFR/performance qualification. |
| BH11 | low | Clarify docs to Completed/domain-Rejected completion and successful drain, which are the authoritative terminal-idempotency writers. PublishFailed stays recoverable and TimedOut can be ambiguous; do not fabricate new terminal proof for them. |
| EH1 | high | Verified 32 retained invalid publication owners consume the fixed-prefix probe budget forever. Patch fair persisted scan progress and recreation regression without deleting evidence or reducing budgets. |
| EH2 | high | Verified the terminal advisory scan has the same starvation with missing/malformed intents. Patch fair persisted scan progress and recreation regression. |
| EH3 | medium | Verified terminal intent validation wrongly applies int.MaxValue although advisory repair reads no event range and EventPersister uses long. Patch long-range advisory coherence while retaining current resume-reader boundary. |
| VG1 | high | Filed pre-verified registration bypass: existing tests fabricate callbacks and survive deletion of RegisterReminderAsync. Patch a registration-driven timer harness with fault/recovery and exact persisted cleanup assertions. |
| VG2 | medium | Verified renamed missing-checkpoint method remains pinned in integration selector and immutable evidence authorities. Preserve original executable test identity with an explanatory comment and new retention assertions; do not rewrite historical manifests or lower required case counts. |
| SELF1 | high | Envelope MessageId is the stable publication identity, distinct from command identity. Current per-envelope guard checks syntax but not repeated IDs within one range. Patch ordinal uniqueness and duplicate-ID persisted refusal/restore regression. |
| SELF2 | high | A publication-owned MessageId with missing checkpoint and idempotency can reach fresh processing; its existing ownership proves recovery evidence is missing, not a new command. Patch refusal before domain/persistence, retaining owner/events and valid reminder recovery; verify the path before changing it. |

Correction recheck input: `/tmp/hexalith-b8-20261014/review-final.diff`, 238,632 bytes, SHA-256 `cd04f3405863aecaed4ffa406f9aa305afa9ffecbbcb057fa24295d0da57adc8`. Three independent rechecks completed before this second triage; the third launch waited for an available agent slot. The logging test-only capture correction was read from current source. The original VG1/VG2 findings are resolved by a registered-only harness and the restored executable selector (10 focused Release cases pass). Original starvation/long-range findings and cursor/key/per-owner corrections were rechecked as resolved. Every new finding remains an individual row below.

| Finding | Verdict | Evidence / route |
| --- | --- | --- |
| BH-C1 | high | Executable reflection probe confirms a zero-event Rejected intent matches terminal backpressure refusal. Patch domain-rejection classification using available cached rejection evidence, excluding backpressure, without changing logical results or imposing a FailureReason rule that rejects valid successful-drain records. Add persisted callback/duplicate refusal and valid rejection regressions. |
| BH-C2 | high | Executable probe confirms a fully matching legacy rejected drain with recoverable Accepted=false/rejection-type evidence returns null causation only because the helper requires Accepted. Patch cached classification to agree with IsRejection, retaining exact message/type/correlation/count/causation and malformed refusal. |
| EH-C1 | high | Same legacy rejection branch independently confirmed. Group with BH-C2 only after retaining this row; patch verified legacy rejection resume/drain and contradiction tests. |
| ROOT-C1 | medium | Broad Server run reaches two finalizer tests' fault at the newly added earlier ownership lookup instead of their intended finalizer. Adjust only precise injection ordinals/arming to target the unchanged finalizer seam; retain every end-state, safe-diagnostic and repeated-reconciliation assertion. |
| ROOT-C2 | medium | Broad Server positive drain proof fixtures always return their originally seeded Recoverable record after Terminal is saved. Patch faithful persisted fixture transitions; retain original exact-range/advanced-head proof assertions. |
| ROOT-C3 | medium | Two Dw8 drain classifier fixtures lack exact persisted identity required before publication/read. Supply genuine matching command/envelope evidence and preserve stable publish/transient-read reason assertions, with transient read exceptions propagating and evidence retained. |
| ROOT-C4 | low | The normalized-causation exact-key test observes the added legitimate advisory idempotency validation lookup. Correct the precise expected count to two while retaining normalized key identity and no correlation-key lookup. |

Final focused correction input: `/tmp/hexalith-b8-20261014/review-final-2.diff`, 254,621 bytes, SHA-256 `61d0ddf047598b4f20cfdcb7c71704497a73d416d07e3a180ad528d399416f7e`. Both independent semantic reviewers report BH-C1/BH-C2/EH-C1 resolved; blind reviewer executed all 75 current recovery integrity cases with zero failures. Root confirms precise classifier/finalizer/fixture corrections preserve core assertions;268 affected cases pass. No new policy decision or unresolved high/medium correction remains. Original low/false limitations retain their recorded routes.

## Verification

Use individual `dotnet build/test tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj` first, Release, warnings as errors; focused actor filters then full project. Root runs applicable remaining lanes and package/coverage/Tenants gates after implementation.

Final verification and qualified closure are recorded in [B8 evidence](b8-validation-evidence.md) and [exact command results](b8-command-results.json). Only DW-499/DW-502 close; remaining release blockers and external provenance are preserved. The user expressly prohibited assistant Git mutations, so the workflow commit step was not performed. No story key or sprint update applies.
