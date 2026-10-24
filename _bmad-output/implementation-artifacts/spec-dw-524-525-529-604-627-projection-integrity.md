---
title: 'Resolve five deferred projection integrity and evidence records'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'dispatch'
baseline_commit: '8767377dba1ed0d67dde4e8279bb7c5a2fe5a49e'
review_loop_iteration: 0
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="user delegated five deferred work items and push to main">

## Intent

**Problem:** Five open records leave tenant index replay ordering (DW-524), legacy projection admission (DW-525), performance revision attribution (DW-529), tenant schema rebuild guidance (DW-604), and global administrator concurrent replacement (DW-627) incomplete.

**Approach:** Complete these five records using existing EventStore persistence policies, fail-closed intake validation, executable provenance preflight and a documented coordinated rebuild procedure. The user expressly requests five items and publication on main; keep the complete bundle and proceed through independent review and validation without further routine approval.

## Boundaries & Constraints

**Always:** Assert persisted end-state, use IReadModelStore/ReadModelWritePolicy, preserve audit-stream isolation, same-sequence sibling events and unrelated tenants. Use optimistic-concurrency reload/merge and ordinal identities. Run Debug source references locally and relevant Release/package checks. Preserve original ledger fields/history, dependency pointers and unrelated decisions. Capture full canonical Git identifiers. Work sequentially during implementation. Read tracked build/test guidance first.

**Never:** Run a production rebuild, delete durable data, edit dependency contents or pointers, initialize nested dependencies, change event schemas, weaken authorization, introduce persistence plumbing, or claim tests that did not execute. Rebuild guidance is an operator procedure requiring its own explicit authorization before execution. Do not change unresolved revocation, search, status or release policies.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| Older/equal tenant replay against a newer persisted index | Name, lifecycle and membership remain current; watermark belongs to its aggregate |
| New tenant batch with same-sequence siblings, or index ETag conflict | Every admitted sibling applies once; retry preserves other tenants and newer state |
| Legacy index without watermarks | Explicit safe full-history initialization/rebuild behavior; do not infer freshness from tenant detail or silently accept unsafe partial migration |
| Unsupported request evolution hints or event metadata | Real /project route rejects before persistence; ordinary legacy metadata remains supported |
| Older global full replay races a newer persisted replay | Shared policy retries preserve newer singleton authority/version; returned response agrees with persisted choice |
| Performance run from dirty root/dependency or mismatched dependency checkout | Fail before builds/seeding/measurement or revision-bound output; valid clean source records exact root/dependency revisions |
| Historical tenant schema change | Document writer fencing, all affected keys/shared index, authoritative replay, validation and rollback without performing mutation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`: detail already guards sequence; singleton index currently applies every event. Reuse shared UpdateAsync and same-sequence rules.
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`: add durable per-aggregate ordering metadata with backward-compatible deserialization. Define explicit legacy-model handling.
- `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`: audit already uses MergeAsync; singleton currently uses unguarded SaveAsync. Preserve identity checks and canonical audit proof.
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`, `src/Hexalith.Tenants/Program.cs`: bespoke project intake. Platform `DomainProjectionEvolutionAdmission` is internal; inspect public alternatives first, otherwise mirror its domain-boundary rejection using existing public contracts without copying transport/persistence infrastructure.
- `tests/Hexalith.Tenants.Server.Tests/Projections/`: handler/dispatcher tests and scripted store fixtures; update existing global write assertions for guarded writes. `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` supplies real protected /project HTTP fixture.
- `scripts/run-tenant-audit-performance.sh`: environment.json currently records only root HEAD before build/seed. Add provenance guard before any side effect. `tests/scripts/` contains hermetic Git fixtures.
- `docs/event-contract-reference.md`, `docs/audit-stream-rollout.md`: existing event/audit authority and rollout constraints; extend tenant schema rebuild guidance rather than inventing an automated migration capability.

## Tasks & Acceptance

- [x] `TenantProjectionHandler.cs`, `TenantIndexReadModel.cs` and projection tests — guard index replay with persisted per-aggregate watermarks, concurrency and explicit legacy compatibility.
- [x] `ProjectionDispatcher.cs` and route tests — reject unsupported evolution fields before any persistence; cover valid legacy controls and each unsupported metadata family.
- [x] `GlobalAdministratorProjectionHandler.cs` and tests — replace unguarded singleton write with shared concurrency policy, including older/equal/unversioned and controlled competing-write end-state cases.
- [x] `scripts/run-tenant-audit-performance.sh`, a narrow provenance helper and hermetic tests — enforce source identity before any run side effect, record canonical root/dependency revisions, and cover root/dependency dirty or mismatched cases.
- [x] `docs/event-contract-reference.md` and linked rebuild procedure — specify coordinated tenant detail/audit/shared-index schema rollout, authoritative history requirements, isolated validation, cutover and rollback; no production commands executed.
- [x] Deferred ledger, maintenance index, `tests/test-summary.md` and colocated evidence — close exactly the five verified records, reconcile counts and preserve unrelated records.

**Acceptance Criteria:**

- Given supported projection requests, when replays arrive out of order or writes conflict, then persisted tenant index and global authority never regress and other tenants remain intact.
- Given unsupported evolution hints, when authenticated /project is called, then it refuses before reads/writes; valid legacy requests retain their behavior.
- Given an attributable clean performance source, when preflight succeeds, then canonical root and root-dependency identities are recorded; dirty/mismatched sources fail before work begins.
- Given an operator planning a schema rebuild, when following the documentation, then all affected models and writer/history/cutover/rollback requirements are explicit without altering canonical events.
- Given passing required checks and independent review, when completion is recorded, then exactly these five records close and the finished commit is pushed to main with a pinned-commitlint-validated message.

## Implementation Notes

Investigation found no intent gaps. Product behavior follows existing replay/admission contracts. No irreversible data operation is required; user explicitly authorized commit/push. Initial tree is clean, canonical baseline is recorded above, and Aspire describe reports every resource Running/Healthy. User authorization overrides older project-context branch advice: main is the requested destination and baseline Git default.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / disposition |
| --- | --- | --- | --- |
| B1 | false | reject | The documented legacy projection intake supplies a complete contiguous prefix in sequence order (ProjectionRequest.Events contract and ProjectionUpdateOrchestrator full-prefix read). A manufactured gapped suffix is outside that supported delivery contract; initial sparse histories are already refused. This does not establish a supported stale-replay regression. |
| B2 | false | reject | Supported legacy delivery passes an immutable complete prefix, including the entire same-sequence group. Splitting a group and later appending siblings to an already delivered sequence is not a supported source history; current within-batch siblings and complete duplicate replay are exercised. Adding public per-event state for that protocol is unwarranted. |
| B3 | medium | patch | The handler currently derives authority version from an incomplete history even though IsCompleteGlobalReplay withholds audit proof. Reuse that completeness decision before replacing singleton authority; preserve existing ordered authority for incomplete requests and refuse unsafe initial authority. |
| B4 | medium | patch | A populated singleton from an unversioned initial request cannot satisfy the new next-call guard. Refuse unverifiable initial authority before either key is written; missing/empty initialization remains separate. |
| B5 | medium | patch | Zero is not a legitimate committed owner watermark under one-based initialization. Reject zero and negative persisted values before writes, and qualify both cases. |
| B6 | low | patch | A dependency can change while later dependencies are inspected. Recheck captured dependency revisions and cleanliness at the end of admission; the isolated dedicated-runner contract still requires no external source mutation during measurement. |
| B7 | medium | patch | Git status can hide modifications under assume-unchanged/skip-worktree. Reject those index flags in root/dependencies and qualify both with hermetic fixtures. |
| B8 | medium | patch | No normal CI step executes the new Python file. Add its exact invocation to Story Guards. |
| B9 | low | patch | The current-state Remaining sentence is stale despite correct rows and table. Correct it to 77 records /71 rows. |
| B10 | medium | patch | Positive protected legacy controls cover only tenants. Add successful global HTTP controls and a real-route populated-legacy refusal with persisted-state assertions. |
| E1 | false | reject | Same supported-delivery refutation as B1: current full-prefix transport does not send gapped suffixes. Exact old/equal complete prefixes and competing commits are the supported replay cases under qualification. |
| E2 | false | reject | Same supported-group refutation as B2: complete immutable legacy prefix delivery does not append new siblings to a previously completed group. |
| E3 | medium | patch | Initialization sorts a request to validate completeness but detail consumes original order. Normalize the supplied collection once so the new admitted prefix produces consistent detail/index state; add persisted unordered-prefix coverage. |
| E4 | medium | patch | Same incomplete-global replacement defect as B3; reuse existing canonical replay proof for authority selection. |
| E5 | low | patch | Same inspected-dependency change window as B6; recheck all recorded checkouts before emitting proof. |
| E6 | low | patch | Same stale current-state sentence as B9; update it directly. |
| V1 | medium | patch | Preverified broken-verification gap: maintained workflows omit the new provenance suite. Add its explicit Story Guards invocation. |
| V2 | medium | patch | Preverified caller gap: clean helper stdout is tested, but environment.json persistence is not. Add a controlled clean runner artifact test that stops before builds/seeding/measurement. |
| V3 | medium | patch | Parent reproduced full existing AuditStreamPersistenceTests:119 total,97 failed,22 passed, zero skips. Ordinary AuthorizedStore fixtures lack an ordered version; supply explicit valid authority for ordinary tests and retain separate legacy-refusal cases. |
| P1 | medium | patch | Parent CI trace shows new replay regressions and existing audit persistence cases reside only in the advisory topology project. Extend the existing blocking topology-free contract job to run those classes with minimum/zero-skip guards; update the project-route minimum for its added cases. |
| P2 | low | patch | New Allman braces contain avoidable indentation and blank-line drift. Correct only added blocks without unrelated formatting. |

## Verification

Build Server and IntegrationTests in Debug/source mode with serialized MSBuild; run focused projection/route cases and full Server suite. Exercise relevant Release/package route/handler tests. Run hermetic provenance tests, bash syntax checks, existing performance prepare-run tests and story-gitlink guard. Preserve exact commands, failures, results and any broad blockers. Parent stages and reads the entire diff, audits matrix coverage and runs independent review. Before publication fetch/inspect divergence, validate the exact commit message with pinned commitlint, commit and push main.

## Implementation handoff

All implementation tasks and the specified validation lanes pass. Independent review,
final staged-diff inspection, fetch/divergence inspection, validated commit and push
remain parent-owned before completion; status remains in-progress until those finish.
See [colocated validation evidence](dw-524-525-529-604-627-validation-evidence.md).

## File List

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/ProjectionDispatcherTelemetryTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/ProjectionReplayIntegrityTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `scripts/run-tenant-audit-performance.sh`
- `scripts/tenant-audit-source-provenance.py`
- `tests/scripts/test_tenant_audit_source_provenance.py`
- `docs/event-contract-reference.md`
- `docs/audit-stream-rollout.md`
- `docs/tenant-projection-schema-rebuild.md`
- `docs/maintenance-backlog.md`
- `tests/test-summary.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/dw-524-525-529-604-627-validation-evidence.md`
- `_bmad-output/implementation-artifacts/spec-dw-524-525-529-604-627-projection-integrity.md`
- `.github/workflows/story-guards.yml`
- `.github/workflows/ci.yml`
- `tests/Hexalith.Tenants.IntegrationTests/AuditStreamPersistenceTests.cs`

Parent acceptance audit: staged all 23 intended files, inspected the complete unified diff in file groups and source, and matched every matrix row to executed replay/route/provenance tests or the explicit manual rebuild-procedure inspection. All six implementation tasks are satisfied; publication waits for independent review.

Independent review pass 1: all three reviewers completed. Every finding has an individual verdict before grouping. Patch groups reuse existing private helpers/contracts and change no public transport surface. Parent full HTTP class passes136/136; existing audit-class failure is preserved for correction.

## Parent final acceptance

All three review layers completed; every finding has an individual triage row. Accepted corrections passed Verification Gap and Edge Case rechecks. Edge Case independently traced full-prefix transport and withdrew the gapped/split-prefix findings; no remaining supported-input defect was found. No new deferred entry was required. Final parent checks passed: Server1475, full HTTP141, audit119 direct, generated44 and replay31 in Debug/source; package replay31, project35, audit117 unique MTP, generated44 and status18; provenance19, prepare-run4, shell syntax, actionlint and gitlink guard. All tests have zero failures/skips and both builds have zero warnings/errors.

The user explicitly requested push to main, overriding the workflow default against automatic push. Exact full commit-message validation used pinned commitlint21.2.3 and passed with zero problems/warnings. A newly observed upstream compatibility commit is preserved by rebasing before publication; affected combined-source checks are required before push.

Final combined-source acceptance: preserved upstream2ea496168635284301f75c05e53b32bec4583e80, corrected its eight nonexistent failure-reason references against inspected pinned API, and repeated20 checks. All four builds have zero warnings/errors. Full Server1475 passes in both modes; all source/package HTTP, audit, replay and provenance lanes pass with zero failures/skips. Earlier Debug/package CS0117 failures and corrected tool syntax are retained in evidence. User-authorized publication is the remaining Git action.

Additional integration file declarations:

- `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs` — use the verified pinned mismatch category after upstream integration.
- `src/Hexalith.Tenants/Queries/Handlers/TenantReadAuthorityQueryHandler.cs` — use the verified pinned execution-failure category after upstream integration.
