---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-map-criteria', 'step-04-analyze-gaps', 'step-05-gate-decision']
lastStep: 'step-05-gate-decision'
lastSaved: '2026-05-19'
tempCoverageMatrixPath: '_bmad-output/test-artifacts/trace-coverage-matrix-2026-05-19.json'
gateDecision: 'CONCERNS'
gateStatusFile: '_bmad-output/test-artifacts/gate-decision.json'
e2eTraceSummaryFile: '_bmad-output/test-artifacts/e2e-trace-summary.json'
workflowStatus: 'completed'
mode: 'epic-level'
target_scope: 'Epic 10 — Durable Projection Write Safety'
target_epic_id: 'epic-10'
coverageBasis: 'acceptance_criteria'
oracleResolutionMode: 'formal_requirements'
oracleConfidence: 'high'
oracleSources:
  - '_bmad-output/test-artifacts/test-design-epic-10.md'
  - '_bmad-output/test-artifacts/atdd-checklist-10-4-projection-write-conformance-and-recovery-tests.md'
  - '_bmad-output/test-artifacts/automation-summary.md'
  - '_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md'
  - '_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md'
  - '_bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md'
  - '_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md'
  - '_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/epics.md'
externalPointerStatus: 'not_used'
---

# Coverage Traceability Matrix — Epic 10: Durable Projection Write Safety

**Date:** 2026-05-19 (initial trace) / **Amended:** 2026-05-19 (post-validation pass)
**Author:** Jerome (drafted by Murat — Master Test Architect)
**Status:** Draft (workflow complete; validation amendments applied)

---

## Validation Amendments Applied (2026-05-19)

This matrix was validated in a follow-up `/bmad-testarch-trace` run (mode: Validate). The validation surfaced one false positive and several minor schema/freshness issues, all of which have been corrected in-line below. Summary of changes:

- **VAL-1 (HIGH):** `ProjectionWriteConformanceIntegrationTests.cs` does not exist on disk — `T-R001-INT-001` (`TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync`) was a planned test that was never implemented. Throughout this matrix, T-R001-INT-001 is now classified **NOT IMPLEMENTED** (was: "skipped by precondition"). The R-001 BLOCKER risk remains covered at unit level by `T-R001-UNIT-001`/`002`, so the **gate verdict stays CONCERNS** (not FAIL) — but a new **REC-1B** is added to either build the integration test or formally waive the integration tier.
- **VAL-3 (MEDIUM):** Test baseline updated from **640 passed / 1 skipped** (pre-Story-11-1) to **655 passed / 1 skipped** (post-Story-11-1, per `sprint-status.yaml`). Epic-10-scoped tests are unchanged (still 22 named tests, all passing).
- **VAL-4 / VAL-7 (LOW/MEDIUM):** The companion `e2e-trace-summary.json` now carries `source_sha = bcda64b05b20bf0b87da243c964672feeb852c48` and a `commit_url` link.
- **VAL-5 (MEDIUM):** `e2e-trace-summary.json` `coverage.by_level` is restructured to the canonical `{unit, api, component, e2e, other}` keys.
- **VAL-6 (MEDIUM):** `gate-decision.json` now carries `nfr_status: NOT_ASSESSED` explicitly.
- **VAL-8 (LOW):** `gate-decision.json` blocking items now carry `target_date: 2026-05-26` (+7 days).
- **VAL-9 (LOW):** REC-2 and REC-3 now carry inline Given-When-Then sketches (in `trace-coverage-matrix-2026-05-19.json`).

Full validation findings: see [`trace-validation-report.md`](trace-validation-report.md).

---

## Step 1 — Load Context (Completed)

### Coverage Oracle Resolution

- **Resolution mode:** `formal_requirements` (first-tier oracle per workflow rules)
- **Coverage basis:** `acceptance_criteria` — all 5 stories of Epic 10 carry numbered AC lists, and an authoritative test-design document exists (`test-design-epic-10.md`, authored by Murat on 2026-05-19)
- **Confidence:** **HIGH** — multiple converging formal sources:
  - Story-level ACs (10.1 × 12 ACs, 10.2 × 12 ACs, 10.3A × 12 ACs, 10.3B × 10 ACs, 10.4 implied via ATDD checklist + test-design)
  - Epic-level test design with 23 identified risks (R-001 through R-023), 25 named tests + 1 fixture rule + 1 CI guard
  - ATDD red-phase scaffold checklist already executed for the R-001 BLOCKER trio
  - PRD FRs (FR25–FR30, FR53) and NFRs (NFR5, NFR17, NFR20, NFR23) explicitly reinforced
- **External pointer status:** `not_used` — all requirements live in-repo
- **Synthetic oracle:** not engaged — formal sources are sufficient

### Knowledge Base Loaded

- `test-priorities-matrix.md` — P0/P1/P2/P3 criteria + risk score → priority mapping
- `risk-governance.md` — Gate decision engine (PASS / CONCERNS / FAIL / WAIVED), coverage traceability schema
- `probability-impact.md` — Probability × Impact (1–9) → Action (DOCUMENT/MONITOR/MITIGATE/BLOCK)
- `test-quality.md` — Test Definition of Done (determinism, isolation, explicit assertions, <300 lines, <1.5 min)
- `selective-testing.md` — Tag/grep, spec filter, diff-based selection, promotion rules

### Sprint Status Snapshot (per `sprint-status.yaml`, 2026-05-19)

| Story | Status | Notes |
|---|---|---|
| 10.1 — optimistic concurrency for tenant read-model writes | **done** | Code review complete, all patches applied |
| 10.2 — audit projection write safety | **done** | Code review complete, 6 patches applied |
| 10.3A — EventStore projection cancellation API prerequisite | **done** | EventStore submodule pinned at commit `bcccd504` |
| 10.3B — cancellation token threading for tenant projection queries | **done** | Code review complete, 7 patches applied |
| 10.4 — projection write conformance and recovery tests | **review** | Implemented; full Debug/no-restore gate **655 passed, 1 skipped** (post-Story-11-1; was 640/1 pre-Story-11-1) |
| epic-10-retrospective | optional | — |

**Working assumption:** Epic 10 is *effectively code-complete*; this trace exists to formally close the gate on 10.4 review and certify the epic for retrospective.

### Artifacts Inventoried

**Planning / requirements:**
- Test design (Epic 10): comprehensive, lists every T-RXXX test ID + risk linkage + priority + level
- ATDD checklist (Story 10.4): documents R-001 BLOCKER trio scaffolding decisions
- Automation summary: per-story automation rationale (current state: Story 10.2-focused step-2 output, predates 10.3B/10.4 work)
- 5 story files: each carries 10–12 numbered ACs

**Tests (from agent inventory):**
- `tests/Hexalith.Tenants.Server.Tests/Projections/` — 12 `*Tests.cs` test files including the new `ProjectionWriteConformanceTests.cs` (6 conformance methods), `ProjectionWriteConformanceFixture.cs` (R-008 binding), and the actor-layer `TenantsProjectionActorTests.cs` (cancellation precedence × 10 methods). **AMENDED:** Originally claimed a `ProjectionWriteConformanceIntegrationTests.cs` housing T-R001-INT-001 — that file does not exist. The integration test was planned in the test design but never built.
- `tests/Hexalith.Tenants.Server.Tests/DomainProcessing/`, `Aggregates/`, `Queries/` — supporting coverage for command + query paths (not Epic 10 scope, but relevant interworking)
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/` — `TenantConformanceTests.cs` × 10 tests (in-memory conformance suite, used by consuming services)
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/` — `AspireTopologyFixture.cs`, `TenantsDaprTestFixture.cs`, `DaprFactAttribute.cs` (R-011 + Tier 3 infra)

### Provenance / Confidence Summary

| Source dimension | Status |
|---|---|
| Formal requirements present | ✅ ACs across all 5 stories |
| Test design pre-authored | ✅ `test-design-epic-10.md` with 25 named tests + 1 fixture rule + 1 CI guard |
| ATDD red-phase scaffolding already done | ✅ For R-001 BLOCKER trio; characterization-style for already-shipped 10.1 |
| Tests actually exist in repo | ✅ Confirmed via inventory (see Step 2) |
| Test suite executes green | ✅ 655 passed, 1 skipped on 2026-05-19 (post-Story-11-1; was 640/1 pre-Story-11-1) |
| External system pointers requiring MCP resolution | None |

**Confidence in trace oracle:** HIGH. Proceeding to Step 2 (test discovery).

---

## Step 2 — Discover & Catalog Tests (Completed)

### Test Discovery Scope

For Epic 10 (Durable Projection Write Safety, backend .NET), the relevant test surface is:

- **Projection write helpers and handlers** (`tests/Hexalith.Tenants.Server.Tests/Projections/`)
- **Projection actor cancellation behavior** (same folder, `TenantsProjectionActorTests.cs`)
- **Aspire/Tier-3 fixture liveness** (`tests/Hexalith.Tenants.IntegrationTests/Fixtures/`)
- **Conformance contract** (`tests/Hexalith.Tenants.Testing.Tests/Conformance/`) — interworking surface, not Epic 10 ACs
- **Read-model unit tests** (Projections folder) — interworking; ensures applied-event correctness post-conflict

Test files outside these folders (Aggregates, DomainProcessing, Queries) are part of the broader test suite but **not in Epic 10 scope**.

### Tests Catalogued by Level

#### Tier 1 — Unit (deterministic, in-process)

| Test ID | File | Test Method | Status |
|---|---|---|---|
| T-R001-UNIT-001 | `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs` | `TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync` | passing |
| T-R001-UNIT-002 | `ProjectionWriteConformanceTests.cs` | `TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync` | passing |
| T-R002-UNIT-001 | `ProjectionWriteConformanceTests.cs` | `TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync` | passing |
| T-R002-UNIT-002 *(equiv.)* | `TenantProjectionHandlerTests.cs` | `ProjectAsync_RetryExhaustionThrowsAfterMaxAttemptsAsync` | passing |
| T-R003-UNIT-001 | `ProjectionWriteConformanceTests.cs` | `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicateAndOrdersByTimestampThenEventIdAsync` | passing |
| T-R003-UNIT-002 *(equiv.)* | `TenantProjectionHandlerTests.cs` | `ProjectAsync_AuditRetryExhaustionThrowsWithoutSuccessfulProjectionAsync` | passing |
| T-R004-UNIT-001 | `TenantProjectionHandlerTests.cs` | `ProjectAsync_AuditStateConflictReloadsAndMergesEntriesByEventIdAsync` (+ duplicate sub-case in Conformance) | passing |
| T-R004-UNIT-002 | `ProjectionWriteConformanceTests.cs` (covered inside Audit_ConflictThenSuccess case "persistedAuthoritativeDuplicate") | merged into combined assertion | passing |
| T-R005-UNIT-001 | `TenantProjectionHandlerTests.cs` | `ProjectAsync_TenantStateConflictReloadsStateAndRetriesExactlyOnceAsync` + `ProjectAsync_TenantIndexConflictPreservesReloadedExistingTenantsAsync` | passing |
| T-R006-UNIT-001 | _NOT FOUND in inventory_ | (CachingProjectionActor cache-coherence-after-retry) | **DEFERRED to EventStore** (see Open-Item Resolutions) |
| T-R007-UNIT-001 | `ProjectionWriteConformanceTests.cs` | inside `TenantIndex_RetryExhaustion_..._WithoutClaimingSuccessAsync` lines 220-228 (sentinel-value negative content gate) | passing |
| T-R007-UNIT-002 | `TenantsProjectionActorTests.cs` | `RoleSensitiveQuery_with_malformed_user_logs_only_safe_contextAsync` (closest analog for cancellation-context safety; cancellation cases also present) | passing |
| T-R007-UNIT-003 | `TenantProjectionHandlerTests.cs` | `ProjectAsync_AuditMergeSkipsMalformedPayloadsAndPreservesValidEventsDuringRetryAsync` + Conformance dup-EventId assertion | passing |
| T-R008-FIXTURE-001 | `ProjectionWriteConformanceFixture.cs` | binding contract (R-008 mechanical assertion `BindsToProductionPolicy()` invoked at lines 156, 231) | passing |
| T-R009-UNIT-001 | `ProjectionWriteConformanceTests.cs` | covered inside `Audit_ConflictThenSuccess_..._OrdersByTimestampThenEventIdAsync` + `TenantAuditReadModelTests.SortEntries_orders_entries_by_timestamp_then_event_id` | passing |
| T-R010-UNIT-001 | `TenantsProjectionActorTests.cs` | `ListTenants_with_pre_cancelled_token_throws_before_state_accessAsync` + `RoleSensitiveQuery_pre_cancelled_throws_OCE_not_domain_errorAsync` | passing |
| T-R010-UNIT-002 | `TenantsProjectionActorTests.cs` | `ListTenants_passes_received_token_to_projection_state_readsAsync` + `GetTenantAudit_cancellation_after_audit_state_read_does_not_return_partial_pageAsync` | passing |
| T-R010-UNIT-003 | `TenantsProjectionActorTests.cs` | `RoleSensitiveQuery_pre_cancelled_with_malformed_user_throws_OCE_per_base_actor_precedenceAsync` (taxonomy — OCE vs forbidden/notfound/invalid-cursor) | passing |
| T-R013-UNIT-001 | `TenantProjectionHandlerTests.cs` | `ProjectAsync_IndexRetryExhaustionAfterTenantSaveThrowsAsync` (3-attempt budget exhaustion) | passing |
| T-R015-UNIT-001 | `TenantAuditProjectionTests.cs` | `Project_continues_when_one_event_has_malformed_payload` | passing |
| T-R015-UNIT-002 | `TenantAuditProjectionTests.cs` | `Project_propagates_invariant_violation_when_metadata_missing` | passing |
| T-R016-UNIT-001 | `ProjectionWriteConformanceTests.cs` | inside `TenantIndex_RetryExhaustion_..._WithoutClaimingSuccessAsync` lines 213-218 (asserts 2× EventId 100101 Warning + 1× EventId 100102 Error) | passing |

#### Tier 2 — Integration (DAPR/Docker required)

| Test ID | File | Test Method | Status |
|---|---|---|---|
| T-R001-INT-001 | _NOT IMPLEMENTED_ — file `ProjectionWriteConformanceIntegrationTests.cs` does not exist on disk | `TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync` (planned, never built) | **NONE** — R-001 risk covered at unit level by `T-R001-UNIT-001`/`002`; integration tier was a planned defense-in-depth that was missed. The "1 skipped" in the gate is actually one of the `[DaprFact]`-attributed tests in `Hexalith.Tenants.IntegrationTests` (Aspire/DaprEndToEnd/GracefulDegradation/StatelessRestart), not this test. |
| T-R006-INT-001 | _NOT FOUND in inventory_ | (cache coherence after conflict with real DAPR storage) | **DEFERRED to EventStore** |

#### Tier 3 — End-to-End (Aspire topology)

| Test ID | File | Test Method | Status |
|---|---|---|---|
| T-R011-E2E-001 | `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` | `CommandApi_resource_starts_and_is_alive`, `Tenants_resource_starts_and_is_alive`, `Sample_resource_starts_and_is_alive` (`[DaprFact]` precondition-gated) | passing |

#### CI Guards (not test methods)

| Test ID | Location | Mechanism | Status |
|---|---|---|---|
| T-R012-CI-001 | `.github/workflows/{ci,release}.yml` | Submodule pointer drift check for EventStore pinned at commit `bcccd504` | ❌ **NOT IMPLEMENTED** |

### Coverage Heuristics Inventory

For an Epic-10-shaped trace (projection write safety, cancellation taxonomy), the standard heuristics translate as:

| Heuristic | Epic 10 Surface | Coverage Signal |
|---|---|---|
| **State-key coverage** (analog of "API endpoint coverage") | 3 state keys: `projection:tenants:{tenantId}`, `projection:tenant-index:singleton`, `audit:{tenantId}` | ✅ All 3 covered in `ProjectionWriteConformanceTests` and `TenantProjectionHandlerTests` |
| **Retry-exhaustion coverage** | One per state key + cross-key partial-success | ✅ All 3 covered (tenant detail, tenant index, audit) + `TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync` |
| **Idempotency / duplicate-EventId** | Audit merge by EventId | ✅ Conformance test `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicate...` + handler test `ProjectAsync_AuditStateConflictReloadsAndMergesEntriesByEventIdAsync` |
| **Authorization-before-state-access (cancellation precedence)** | Base actor precedence: forbidden > NotFound > InvalidCursor > Cancellation; cancellation > state access | ✅ 5 actor cancellation tests + 4 forbidden-precedence tests in `TenantsProjectionActorTests.cs` |
| **Negative log-content / PII non-leak (R-007)** | Diagnostic shape on retry-exhaustion, cancellation-failure, duplicate-EventId-mismatch | ⚠️ **PARTIAL** — `ProjectionWriteConformanceTests.cs:220-228` carries the explicit sentinel-value gate for retry-exhaustion. The cancellation-failure analog (T-R007-UNIT-002) and audit-key retry-exhaustion analog (Story 10.2 AC#9) are covered structurally but not with dedicated sentinel-value gates. |
| **Invariant-failure boundary** (preserve 10.2 behavior) | Malformed JSON skip vs metadata-invariant-failure | ✅ `Project_continues_when_one_event_has_malformed_payload` + `Project_propagates_invariant_violation_when_metadata_missing` |
| **Stale-instance reuse on retry** | New `TenantReadModel` / `TenantIndexReadModel` per attempt | ✅ Covered transitively by `ProjectAsync_TenantStateConflictReloadsStateAndRetriesExactlyOnceAsync` + per-attempt etag mechanical assertions in Conformance |
| **Cache poisoning after conflict (R-006)** | `CachingProjectionActor` returns persisted state, not stale snapshot | ⚠️ **NO DIRECT TENANTS TEST.** `TenantsProjectionActorTests.cs:270-272` confirms architectural boundary: `CachingProjectionActor` lives in `Hexalith.EventStore`. Per sprint-status, EventStore Server.Tests 82/82 passing — coverage owned there. |
| **Submodule pointer drift (R-012)** | EventStore pinned at `bcccd504` | ❌ **CI guard MISSING.** Workflows do `submodules: true` (initialize) but no step asserts the specific commit. |
| **CI execution lane** | PR lane (Tier 1) ≤ 12 min; Nightly (Tier 2); Weekly (NFR) | Last gate: 655 passed, 1 skipped on full Debug/no-restore run (post-Story-11-1) |

### Open-Item Resolutions (verified in Step 3 prep)

| # | Item | Resolution |
|---|---|---|
| 1 | T-R006 cache poisoning | **Deferred to EventStore-side coverage.** No matching test in Tenants suite. `TenantsProjectionActorTests.cs:270-272` confirms architectural boundary. Per sprint-status, EventStore Server.Tests 82/82. Tenants validates pre-cancellation precedence; EventStore owns cache-coherence-after-conflict. |
| 2 | T-R007 negative-content (retry exhaustion) | ✅ **EXPLICITLY PRESENT** at `ProjectionWriteConformanceTests.cs:220-228`. Zero-tolerance gate asserts `SensitiveTenantName` and `SensitiveUserId` (synthetic sentinel values) do NOT appear in log `Message` OR `StateText` across all captured entries. |
| 3 | T-R016 structured-log shape | ✅ **EXPLICITLY PRESENT** at `ProjectionWriteConformanceTests.cs:213-218`. Asserts exactly 2 conflict warnings (EventId 100101, Warning level) + 1 retry-exhausted error (EventId 100102, Error level). |
| 4 | T-R012-CI-001 submodule pointer guard | ❌ **NOT IMPLEMENTED.** Both `ci.yml` and `release.yml` use `submodules: true` to initialize, but neither asserts EventStore is pinned at commit `bcccd504` (or any specific commit). **This is a real gap and the principal driver of the CONCERNS gate decision.** |
| 5 | T-R011-E2E-001 actual test methods | ✅ **CONFIRMED** in `AspireTopologyTests.cs:28/37/46` — three `*_resource_starts_and_is_alive` methods (`CommandApi_`, `Tenants_`, `Sample_`). All use `[DaprFact]` for skip-by-precondition. |

---

## Step 3 — Map Coverage Oracle to Tests (Completed)

### Coverage Status Legend

| Status | Definition |
|---|---|
| **FULL** | At least one direct test asserts the AC's behavior, including required negative/error paths |
| **PARTIAL** | AC's happy path covered; an asserted negative path or alternate state missing |
| **UNIT-ONLY** | Behavior covered at unit level only; integration sanity intentionally skipped or not yet wired |
| **INTEGRATION-ONLY** | Behavior covered only at integration tier (no unit-level analog) |
| **NONE** | No test in the Tenants suite asserts the behavior |
| **DEFERRED** | Behavior covered in an out-of-scope codebase (e.g., EventStore submodule) by design |

### Story 10.1 — Optimistic Concurrency for Tenant Read-Model Writes (status: done)

| AC# | Theme | Covering Tests | Coverage | Level | Notes |
|---|---|---|---|---|---|
| AC#1 | ETag-aware write on `projection:tenants:{tenantId}` | `TenantProjectionHandlerTests.ProjectAsync_ExistingTenantStateUsesLoadedETagAndFirstWriteOptionsAsync` + `ProjectionWriteConformanceTests.TenantDetail_ConflictThenSuccess_...` | **FULL** | Unit | Both ETag-load path and conflict reload covered |
| AC#2 | ETag-aware write on `projection:tenant-index:singleton` | `ProjectionWriteConformanceTests.TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync` | **FULL** | Unit | R-001 BLOCKER scenario directly asserted at unit level. **AMENDED:** planned defense-in-depth integration test T-R001-INT-001 was never built (see VAL-1 / REC-1B). |
| AC#3 | Retry policy applies, no silent data loss | TenantDetail/TenantIndex/Audit `ConflictThenSuccess_*` tests | **FULL** | Unit | All 3 keys verified |
| AC#4 | Retry limit exceeded → observable failure | `TenantIndex_RetryExhaustion_FailsObservably_..._WithoutClaimingSuccessAsync` + `TenantProjectionHandlerTests.ProjectAsync_IndexRetryExhaustionAfterTenantSaveThrowsAsync` | **FULL** | Unit | Throws `InvalidOperationException`, asserts log shape, asserts no successful return |
| AC#5 | Focused tests simulate concurrent writes | All 6 `ProjectionWriteConformanceTests` methods + `TenantProjectionHandlerTests` retry scenarios | **FULL** | Unit | Scripted state store, deterministic |
| AC#6 | ETag conflict → retry reloads latest, applies events exactly once | `TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync` lines 80-89 (asserts `external-user` + `user-1` both present, etc.) | **FULL** | Unit | "Exactly once" asserted via membership equality |
| AC#7 | Singleton index conflict → preserves existing tenants | `TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync` lines 134-137 (asserts {tenant-a, tenant-b, tenant-c}) | **FULL** | Unit | Zero loss invariant directly asserted |
| AC#8 | Missing-state path uses `FirstWrite`, existing-state uses loaded ETag | `ProjectAsync_ExistingTenantStateUsesLoadedETagAndFirstWriteOptionsAsync` + `ProjectAsync_MissingTenantStateUsesNoETagAndFirstWriteOptionsAsync` | **FULL** | Unit | Both branches |
| AC#9 | Retry exhaustion → safe structured logs, no payload | `TenantIndex_RetryExhaustion_..._WithoutClaimingSuccessAsync` lines 213-228 (positive shape + R-007 negative content) | **FULL** | Unit | Sentinel values used for R-007 verification |
| AC#10 | One save succeeds + later save exhausts → fails through failure path, no atomicity claim | `TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync` | **FULL** | Unit | Explicit attempt counts per key + log entries asserted |
| AC#11 | Per-key ETag never reused across keys | Conformance tests use distinct per-key etags (`tenant-etag-1`, `index-etag-1`, `audit-etag-1`); `TrySaveAttempts[i].ETag.ShouldBe(...)` per attempt | **FULL** | Unit | Mechanical via scripted store |
| AC#12 | Missing state → default model fresh per attempt | `TenantIndex_RetryExhaustion_...` enqueues `EnqueueRead<TenantIndexReadModel>(..., null, ...)` × 3 + each save uses different ETag | **FULL** | Unit | "Default model fresh per attempt" verified by sequenced reads |

**Story 10.1 verdict: 12/12 ACs FULL coverage.**

### Story 10.2 — Audit Projection Write Safety (status: done)

| AC# | Theme | Covering Tests | Coverage | Level | Notes |
|---|---|---|---|---|---|
| AC#1 | Guarded ETag-aware audit save, no LWW | `TenantProjectionHandlerTests.ProjectAsync_AuditStateConflictReloadsAndMergesEntriesByEventIdAsync` + `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicate...` | **FULL** | Unit | |
| AC#2 | Existing query behavior preserved post-conflict | `TenantAuditReadModelTests.SortEntries_orders_entries_by_timestamp_then_event_id` + Audit conformance asserts `["evt-added", "evt-external", "evt-removed", "evt-role"]` ordering | **FULL** | Unit | Date-range / pagination behavior covered transitively by audit query tests in Queries folder |
| AC#3 | ETag conflict → retry, idempotent merge by EventId, fresh ETag | `Audit_ConflictThenSuccess_...` lines 296-297 (2 try-save attempts with different etags) + handler's merge test | **FULL** | Unit | |
| AC#4 | Max 3 attempts → observable failure | `ProjectAsync_AuditRetryExhaustionThrowsWithoutSuccessfulProjectionAsync` | **FULL** | Unit | |
| AC#5 | Focused tests for concurrent add/remove/role-change, exact membership, EventId dedup, ordering | `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicateAndOrdersByTimestampThenEventIdAsync` (covers all 3 event types + dedup + ordering) | **FULL** | Unit | Single conformance test threads all requirements |
| AC#6 | Replay collapses duplicate EventId to one record; same-timestamp distinct events preserved | `Audit_ConflictThenSuccess_...` line 323 (4 distinct EventIds, `"evt-added"` duplicate collapsed); `TenantAuditReadModelTests.SortEntries_orders_entries_by_timestamp_then_event_id` | **FULL** | Unit | |
| AC#7 | Malformed JSON payloads skipped, valid preserved (today's behavior) | `TenantAuditProjectionTests.Project_continues_when_one_event_has_malformed_payload` + handler's `ProjectAsync_AuditMergeSkipsMalformedPayloadsAndPreservesValidEventsDuringRetryAsync` | **FULL** | Unit | Regression guard preserved |
| AC#8 | Invariant failures (missing MessageId/UserId) propagate via existing failure path | `TenantAuditProjectionTests.Project_propagates_invariant_violation_when_metadata_missing` + `TenantAuditReadModelTests.Apply_throws_when_message_id_is_missing` / `Apply_throws_when_user_id_is_missing` | **FULL** | Unit | |
| AC#9 | Retry-exhaustion log → safe structured fields, no payloads | Pattern from Story 10.1 AC#9 (`TenantIndex_RetryExhaustion_...` sentinel-value gate) applies *by symmetry* through the same `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` path; audit-specific `ProjectAsync_AuditRetryExhaustionThrowsWithoutSuccessfulProjectionAsync` covers behavior + structured log shape | **PARTIAL** | Unit | ⚠️ Audit-key-specific exhaustion + R-007 sentinel test not explicitly enumerated. Recommend dedicated `Audit_RetryExhaustion_FailsObservablyAndDoesNotLeakAudit*` mirroring the 10.1 sentinel pattern. |
| AC#10 | Audit save succeeds + later write fails → projection reports failure; replay idempotent | `Audit_ReplayAfterAuditSaveAndLaterIndexFailure_DoesNotDuplicateEntriesAsync` | **FULL** | Unit | Cross-key non-atomicity explicit |
| AC#11 | Reloaded entry with same EventId, different details → persisted authoritative | `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicate...` lines 324-326 (`savedDuplicate.EventType.ShouldBe(nameof(UserRemovedFromTenant))`, persisted wins despite incoming `UserAddedToTenant` event with same EventId) | **FULL** | Unit | R-004 BLOCKER scenario directly asserted |
| AC#12 | Valid events followed by invariant failure → entries constructed/validated before any save | `TenantAuditProjectionTests.Project_propagates_invariant_violation_when_metadata_missing` (no save occurs when invariant breaks); audit handler asserts validate-before-save ordering | **FULL** | Unit | Per sprint-status note: "reorder validate-before-tenant-write" patch applied during 10.2 review |

**Story 10.2 verdict: 11/12 ACs FULL, 1/12 PARTIAL (AC#9 audit-key sentinel-value gate not explicit).**

### Story 10.3A — EventStore Projection Cancellation API Prerequisite (status: done)

> **Scope note:** Story 10.3A landed in the EventStore submodule (`bcccd504`). Its tests live in `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests` — out of scope for *this* Tenants trace by design. Sprint status references "EventStore Server.Tests 82/82" passing after 10.3A. Tenants-side coverage of the 10.3A handoff lives in 10.3B traces below.

| AC# | Theme | Coverage Locus | Coverage | Notes |
|---|---|---|---|---|
| AC#1–AC#5 | EventStore-side cancellation API surface | EventStore submodule tests | **DEFERRED** | 82/82 passing per sprint-status |
| AC#6 | Existing callers compile against non-cancellation API | Tenants build passing (655 tests post-Story-11-1; was 640 at 10.3A merge) | **FULL** | Tenants compiles + tests green against the new EventStore surface |
| AC#7 | Story 10.3B names exact EventStore APIs + submodule commit | Story 10.3B Dev Notes name `bcccd504` per sprint-status messages | **FULL** | Verified in 10.3B story content |
| AC#8–AC#12 | Cancellation observability + actor-boundary behavior | EventStore tests + Tenants 10.3B tests | **FULL (split)** | Tenants-side proxy via `TenantsProjectionActorTests.cs` cancellation suite |

**Story 10.3A verdict: 12/12 ACs covered (mix of DEFERRED to EventStore and FULL in Tenants-side handoff coverage). No Tenants-side gap.**

### Story 10.3B — Cancellation Token Threading (status: done)

| AC# | Theme | Covering Tests | Coverage | Level | Notes |
|---|---|---|---|---|---|
| AC#1 | Cancellation propagated through tenant projection query path | `TenantsProjectionActorTests.ListTenants_with_pre_cancelled_token_throws_before_state_accessAsync` + `ListTenants_passes_received_token_to_projection_state_readsAsync` | **FULL** | Unit | Pre-cancel + mid-flow both covered |
| AC#2 | Audit read observes cancellation; DAPR reads/filtering/pagination stop | `GetTenantAudit_cancellation_after_authorization_throws_before_audit_state_readAsync` + `GetTenantAudit_cancellation_after_audit_state_read_does_not_return_partial_pageAsync` | **FULL** | Unit | Pre-state-read + post-state-read cancellation both verified |
| AC#3 | `ProjectAsync` write path observes cancellation | `GlobalAdministratorProjectionHandlerTests.ProjectAsync_WithPreCancelledTokenThrowsBeforeSaveAsync` + `ProjectAsync_PassesCancellationTokenToSaveStateBoundaryAsync` | **FULL** | Unit | Write-path cancellation covered for both global admin and tenant paths |
| AC#4 | Verified 10.3A complete + recorded EventStore signatures | EventStore submodule pinned at `bcccd504` (visible in story content + sprint-status) | **FULL** | n/a | Process AC; sprint-status carries the commit reference |
| AC#5 | Cancellation observed before state access → no successful result, no state corruption | `ListTenants_with_pre_cancelled_token_throws_before_state_accessAsync` (state never accessed) + cancellation-after-state-read tests (no partial page returned) | **FULL** | Unit | |
| AC#6 | Non-cancelled callers execute same flows (no behavior regression) | 611-passing baseline → 640-passing post-10.3B-and-10.4 → 655-passing post-Story-11-1 confirms no regression; no listed test failures | **FULL** | Sprint-status evidence | |
| AC#7 | Cancellation → safe structured context distinct from forbidden/not-found/invalid-cursor/etc., no payload leak | `TenantsProjectionActorTests.RoleSensitiveQuery_with_malformed_user_logs_only_safe_contextAsync` (forbidden-path analog) + `RoleSensitiveQuery_pre_cancelled_with_malformed_user_throws_OCE_per_base_actor_precedenceAsync` (taxonomy preservation) | **PARTIAL** | Unit | ⚠️ Taxonomy assertion present (OCE not converted to other types); dedicated cancellation-failure-path sentinel-value test (T-R007-UNIT-002 analog) not explicit. Recommend follow-up. |
| AC#8 | If 10.3A not done → no Tenants-local bypass | Sprint-status: 10.3A done before 10.3B started | **FULL** | Process | |
| AC#9 | Cancellation checkpoint after guards, before state I/O | `RoleSensitiveQuery_with_malformed_user_returns_forbidden_before_state_accessAsync` + `GetTenantAudit_cancellation_after_authorization_throws_before_audit_state_readAsync` (cancellation observed *after* auth) | **FULL** | Unit | Precedence order: forbidden > cancellation > state access |
| AC#10 | No cross-key atomic rollback claim | `Audit_ReplayAfterAuditSaveAndLaterIndexFailure_DoesNotDuplicateEntriesAsync` + `TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync` | **FULL** | Unit | Both anti-atomicity tests present |

**Story 10.3B verdict: 9/10 ACs FULL, 1/10 PARTIAL (AC#7 cancellation-specific R-007 sentinel-value test not explicit).**

### Story 10.4 — Projection Write Conformance & Recovery Tests (status: review)

> Story 10.4 is itself the test-design implementation story; its ACs are all about *writing the tests*. The traceability question reduces to: do the tests called for in the ATDD checklist + test design actually exist?

| Test ID (from test design) | Implemented? | File / Method | Status |
|---|---|---|---|
| T-R001-UNIT-001 | ✅ | `ProjectionWriteConformanceTests.TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync` | passing |
| T-R001-UNIT-002 | ✅ | `ProjectionWriteConformanceTests.TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync` | passing |
| T-R001-INT-001 | ❌ **NOT IMPLEMENTED** | file `ProjectionWriteConformanceIntegrationTests.cs` does not exist | **GAP (REC-1B)** — was planned in test-design but never built; R-001 covered at unit level only |
| T-R002-UNIT-001 | ✅ | `ProjectionWriteConformanceTests.TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync` | passing |
| T-R002-UNIT-002 | ✅ (proxy) | `TenantProjectionHandlerTests.ProjectAsync_RetryExhaustionThrowsAfterMaxAttemptsAsync` | passing |
| T-R003-UNIT-001 | ✅ | `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicate...` | passing |
| T-R003-UNIT-002 | ✅ (proxy) | `TenantProjectionHandlerTests.ProjectAsync_AuditRetryExhaustionThrowsWithoutSuccessfulProjectionAsync` | passing |
| T-R004-UNIT-001 | ✅ | `Audit_ConflictThenSuccess_...` (duplicate-EventId persisted-wins assertion) | passing |
| T-R004-UNIT-002 | ✅ (combined) | Same conformance test asserts persisted authoritative on payload mismatch | passing |
| T-R005-UNIT-001 | ✅ | `TenantProjectionHandlerTests.ProjectAsync_TenantStateConflictReloadsStateAndRetriesExactlyOnceAsync` | passing |
| T-R006-UNIT-001 | ❌ → DEFERRED | EventStore-side `CachingProjectionActor` cache-coherence tests | EventStore Server.Tests 82/82 (out of Tenants scope) |
| T-R006-INT-001 | ❌ → DEFERRED | Same | Same |
| T-R007-UNIT-001 | ✅ | `TenantIndex_RetryExhaustion_..._WithoutClaimingSuccessAsync` lines 220-228 | passing |
| T-R007-UNIT-002 | ⚠️ PARTIAL | Actor-layer `_logs_only_safe_contextAsync` covers forbidden path; cancellation-specific sentinel-value test not explicit | passing where present |
| T-R007-UNIT-003 | ✅ (proxy) | `ProjectAsync_AuditMergeSkipsMalformedPayloadsAndPreservesValidEventsDuringRetryAsync` covers malformed-payload non-leak; duplicate-EventId-mismatch diagnostic sentinel covered structurally in Audit_ConflictThenSuccess test path | passing |
| T-R008-FIXTURE-001 | ✅ | `ProjectionWriteConformanceFixture.BindsToProductionPolicy()` invoked from conformance tests (lines 156, 231) | passing |
| T-R009-UNIT-001 | ✅ | `Audit_ConflictThenSuccess_...OrdersByTimestampThenEventIdAsync` line 323 + `TenantAuditReadModelTests.SortEntries_orders_entries_by_timestamp_then_event_id` | passing |
| T-R010-UNIT-001 | ✅ | `TenantsProjectionActorTests.ListTenants_with_pre_cancelled_token_throws_before_state_accessAsync` | passing |
| T-R010-UNIT-002 | ✅ | `GetTenantAudit_cancellation_after_audit_state_read_does_not_return_partial_pageAsync` | passing |
| T-R010-UNIT-003 | ✅ | `RoleSensitiveQuery_pre_cancelled_with_malformed_user_throws_OCE_per_base_actor_precedenceAsync` (taxonomy enforcement) | passing |
| T-R011-E2E-001 | ✅ | `AspireTopologyTests.{CommandApi,Tenants,Sample}_resource_starts_and_is_alive` | passing (DaprFact skips when DAPR unavailable) |
| T-R012-CI-001 | ❌ | **NOT IMPLEMENTED** in `.github/workflows/ci.yml` or `release.yml` | **GAP** |
| T-R013-UNIT-001 | ✅ | `TenantProjectionHandlerTests.ProjectAsync_IndexRetryExhaustionAfterTenantSaveThrowsAsync` (3-attempt budget) | passing |
| T-R015-UNIT-001 | ✅ | `TenantAuditProjectionTests.Project_continues_when_one_event_has_malformed_payload` | passing |
| T-R015-UNIT-002 | ✅ | `TenantAuditProjectionTests.Project_propagates_invariant_violation_when_metadata_missing` | passing |
| T-R016-UNIT-001 | ✅ | `TenantIndex_RetryExhaustion_..._WithoutClaimingSuccessAsync` lines 213-218 | passing |

**Story 10.4 verdict (AMENDED): 21/25 test IDs FULL, 2/25 DEFERRED to EventStore (R-006 trio), 2/25 NONE (T-R012-CI-001 CI guard + T-R001-INT-001 planned integration test never built). Plus 1 PARTIAL (T-R007-UNIT-002 cancellation sentinel-value). One fixture rule (T-R008) PASSING.**

### Coverage Validation (per Step 3 §2 rules)

| Validation Rule | Result |
|---|---|
| P0/P1 items have coverage | ⚠️ **AMENDED:** P0 R-001 risk covered at unit level (T-R001-UNIT-001/002); planned integration tier T-R001-INT-001 was never built (P0 test-inventory 2/3 = 67%; P0 risk coverage 100%). All P1 covered except T-R007-UNIT-002 cancellation sentinel (PARTIAL) and T-R006 trio (DEFERRED to EventStore). |
| No duplicate coverage across levels without justification | ✅ T-R001 has UNIT + INT variants by design (R-001 BLOCKER warrants both); no other duplication |
| Items not happy-path-only when oracle implies error handling | ✅ Every retry/exhaustion/cancellation AC has a paired error-path test |
| API items marked FULL without endpoint check | n/a — no API-level oracle items in Epic 10 |
| Auth/authz items include denied/invalid-path tests | ✅ `TenantsProjectionActorTests` carries the malformed-user / pre-cancellation precedence tests |
| Synthetic UI journeys with no E2E | n/a — formal oracle, no synthetic UI journeys |

### Trace Matrix Summary

| Story | ACs Total | FULL | PARTIAL | NONE | DEFERRED |
|---|---|---|---|---|---|
| 10.1 | 12 | 12 | 0 | 0 | 0 |
| 10.2 | 12 | 11 | 1 (AC#9) | 0 | 0 |
| 10.3A | 12 | 7 in-scope | 0 | 0 | 5 (EventStore-side) |
| 10.3B | 10 | 9 | 1 (AC#7) | 0 | 0 |
| 10.4 (test IDs) | 25 + 1 fixture rule + 1 CI guard | 21 | 1 (UNIT-002 cancellation sentinel) | 2 (CI guard T-R012-CI-001 + planned integration test T-R001-INT-001) | 2 (R-006 trio) |
| **Totals (story ACs)** | **46** | **39** | **2** | **0** | **5** |

**Headline coverage rate (excluding deferred), AMENDED:** 39/41 = **95.1%** FULL for story-level ACs, with 2 PARTIAL (both related to R-007 sentinel-value gaps in audit-exhaustion and cancellation-failure paths) + **2 missing test-design items** (T-R012-CI-001 CI guard + T-R001-INT-001 planned integration test). Story-AC totals are unchanged because R-001 ACs are satisfied at unit level; the integration test was test-design defense-in-depth, not a unique AC source.

---

## Step 4 — Analyze Gaps & Phase 1 Completion (Completed)

### Execution Mode

- **Requested:** `auto` (per config `tea_execution_mode`)
- **Resolved:** **`sequential`** — single-agent context; no agent-team or subagent runtime in this loop. Probe disabled by capability check.

### Gap Classification

| Severity | Count | Items |
|---|---|---|
| **CRITICAL (P0)** | 0 | None (R-001 BLOCKER covered at unit level; integration tier missing but not blocking) |
| **HIGH (P1)** | 1 (AMENDED) | **T-R001-INT-001** — planned integration test never implemented (REC-1B). Originally listed at 0; reclassified post-validation. Behavioral impact: zero (R-001 covered at unit level); test-design completeness impact: HIGH. |
| **MEDIUM (P2)** | 1 | **T-R012-CI-001** — submodule pointer guard not implemented in CI workflows (REC-1) |
| **LOW (P3)** | 0 | None |
| **PARTIAL** | 2 | Story 10.2 AC#9 (audit-key R-007 sentinel-value gate) + Story 10.3B AC#7 / T-R007-UNIT-002 (cancellation-failure R-007 sentinel-value gate) |
| **DEFERRED** | 2 | T-R006-UNIT-001 + T-R006-INT-001 (EventStore CachingProjectionActor cache-coherence; covered by EventStore Server.Tests 82/82) |

### Coverage Statistics

**By story-level acceptance criteria (46 total):**

| Bucket | Count | % |
|---|---|---|
| FULL | 39 | 84.8% |
| PARTIAL | 2 | 4.3% |
| NONE | 0 | 0.0% |
| DEFERRED | 5 (all 10.3A EventStore-side) | 10.9% |
| **Effective in-scope coverage** | **39/41 FULL** | **95.1%** |

**By test design test ID (25 tests + 1 fixture rule + 1 CI guard = 27) — AMENDED:**

| Bucket | Count |
|---|---|
| Implemented & passing | 21 (was 22 before VAL-1; T-R001-INT-001 was incorrectly counted as "skipped-by-precondition") |
| Implemented & skipped-by-precondition (intentional) | 0 (was 1; the "1 skipped" in the gate is a `[DaprFact]` test in `Hexalith.Tenants.IntegrationTests`, not T-R001-INT-001) |
| Fixture rule passing | 1 (T-R008-FIXTURE-001) |
| PARTIAL | 1 (T-R007-UNIT-002) |
| DEFERRED (EventStore) | 2 (T-R006 trio) |
| **NONE** | **2 (T-R012-CI-001 CI guard + T-R001-INT-001 planned integration test never built)** |

**Priority breakdown (per test design):**

| Priority | Total | Covered (in-scope) | % | Notes |
|---|---|---|---|---|
| **P0** (R-001 BLOCKER trio) — AMENDED | 3 planned tests | 2 unit tests implemented + 0 integration tests implemented | **67% test-inventory; 100% risk coverage** | T-R001-UNIT-001/002 directly assert the BLOCKER scenario at unit level. T-R001-INT-001 planned but never built (file `ProjectionWriteConformanceIntegrationTests.cs` does not exist). |
| **P1** (R-002…R-008) | 13 (11 tests + 1 fixture + 1 INT) | 11 + 1 fixture = 12; 2 DEFERRED to EventStore | **100% in-scope**; 1 PARTIAL (T-R007-UNIT-002) | Conformance + handler tests + actor cancellation suite |
| **P2** (R-009…R-016) | 11 (10 tests + 1 CI guard) | 10 tests; **0/1 CI guard** | **91%** | Missing T-R012-CI-001 |
| **P3** | 0 | 0 | n/a | Documented-only per test design |

### Coverage Heuristics Gap Counts

| Heuristic | Gap Count | Notes |
|---|---|---|
| Endpoints without tests | 0 | n/a — no API-level oracle items |
| Auth negative-path gaps | 0 | All forbidden-precedence variants covered |
| Happy-path-only criteria | 0 | Every retry/exhaustion AC has paired error-path |
| UI journeys without E2E | 0 | n/a — no UI in scope |
| UI state gaps | 0 | n/a |
| **Missing PII non-leak sentinel gates** | **2** | Audit-key retry-exhaustion + cancellation-failure paths |
| **Missing CI guards** | **1** | T-R012-CI-001 submodule pointer drift |
| **Missing planned integration tests (AMENDED)** | **1** | T-R001-INT-001 planned in test-design but never built |

### Recommendations (prioritized)

| # | Priority | Action | Owner | Effort | Blocks Epic Close? |
|---|---|---|---|---|---|
| **REC-1** | **HIGH** | Implement T-R012-CI-001 submodule pointer guard in `ci.yml` and `release.yml`. Add a step that runs `git -C Hexalith.EventStore rev-parse HEAD` and asserts it matches a checked-in expected commit (e.g., `.eventstore-pinned-commit` file or env var); fail on drift with diff in log. | CI/Platform | 2-4h (target: 2026-05-26) | **YES** per test-design Exit Criteria |
| **REC-1B** *(AMENDED)* | **HIGH** | Decide T-R001-INT-001: either (a) build `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs` with `TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync` (DAPR-precondition-gated via `[DaprFact]`), OR (b) author a sprint-change-proposal formally waiving the integration tier on the basis that unit-level R-001 coverage (T-R001-UNIT-001/002) is sufficient. | Story 10.4 follow-up dev or SCP author | 3-6h (build) or 0.5h (waive) — target: 2026-05-26 | **YES** per test-design Exit Criteria (planned test never built) |
| REC-2 | MEDIUM | Add `Audit_RetryExhaustion_FailsObservablyAndDoesNotLeakSensitiveAuditAsync` to `ProjectionWriteConformanceTests.cs`. Mirror the tenant-index sentinel pattern at lines 167-232 but target the audit-key exhaustion path. **GWT:** GIVEN three queued projection events targeting an audit key whose ETag is contended × 3 AND the audit narrative + user identifiers carry sentinel values `SensitiveAuditNarrative_DoNotLog` and `SensitiveUserId_DoNotLog`; WHEN `ProjectAsync` is invoked and the retry budget exhausts; THEN it throws `InvalidOperationException`, logger captures 2× EventId 100101 Warning + 1× EventId 100102 Error, AND no captured log entry contains either sentinel string. | Story 10.4 follow-up | 1-2h | No (PARTIAL coverage covers behavior, missing only the sentinel-value PII gate analog) |
| REC-3 | MEDIUM | Add `Cancellation_Failure_DiagnosticDoesNotLeakSensitiveContextAsync` to `TenantsProjectionActorTests.cs`. Trigger mid-flow cancellation against tenant audit query, assert no sentinel values in captured logs. **GWT:** GIVEN a `GetTenantAudit` query whose audit narrative contains sentinels `SensitiveTenantName_DoNotLog` + `SensitiveCursorPayload_DoNotLog`, AND a CancellationToken signalled after authorization but before state read returns; WHEN the actor method is awaited; THEN `OperationCanceledException` is thrown (not converted to forbidden/not-found/invalid-cursor), AND no captured log entry contains either sentinel. | Story 10.3B follow-up | 1-2h | No |
| REC-4 | LOW | Cross-check EventStore Server.Tests carries R-006 cache-coherence-after-conflict tests. If absent, escalate to EventStore-side story. | Epic 10 retrospective | 0.5h | No |
| REC-5 | LOW | Run `/bmad-testarch-test-review` against `ProjectionWriteConformanceTests.cs` (~390 lines total) + `TenantsProjectionActorTests.cs` for test-quality DoD validation. | Murat (TEA) | 1h | No |

### Phase 1 Summary

```
✅ Phase 1 Complete: Coverage Matrix Generated (AMENDED 2026-05-19)

📊 Coverage Statistics:
- Total story-level ACs: 46
- Fully Covered: 39 (84.8%)
- Partially Covered: 2 (R-007 sentinel-value gaps)
- Uncovered: 0
- Deferred (EventStore-owned): 5

🎯 Priority Coverage (test IDs) — AMENDED:
- P0: 2/3 implemented (67% test-inventory) / 100% risk coverage (R-001 unit-level)
- P1: 11/11 in-scope FULL + 1 PARTIAL + 2 DEFERRED to EventStore
- P2: 10/11 (91% — missing T-R012-CI-001 CI guard)
- P3: n/a (documented only)

⚠️ Gaps Identified — AMENDED:
- Critical (P0): 0 (R-001 covered at unit level)
- High (P1): 1 (T-R001-INT-001 planned integration test never built — REC-1B)
- Medium (P2): 1 (T-R012-CI-001 — REC-1)
- Low (P3): 0
- PARTIAL: 2 (R-007 sentinel analogs)

🔍 Coverage Heuristics:
- Missing PII non-leak sentinel gates: 2
- Missing CI guards: 1
- Missing planned integration tests: 1 (AMENDED)
- All standard heuristics (endpoints/auth/error-paths/UI) green or n/a

📝 Recommendations: 6 (AMENDED — added REC-1B)
- 2 HIGH (REC-1 CI guard + REC-1B integration test decision — both block epic close)
- 2 MEDIUM (PII sentinel analogs)
- 2 LOW (cross-check + test-review)

🧪 Last Gate (2026-05-19, post-Story-11-1): 655 passed / 1 skipped / 0 failed
   (was 640/1/0 pre-Story-11-1; refreshed per sprint-status — VAL-3)

📄 Full coverage matrix JSON: _bmad-output/test-artifacts/trace-coverage-matrix-2026-05-19.json
📄 Validation report:         _bmad-output/test-artifacts/trace-validation-report.md

🔄 Phase 2: Gate decision (next step)
```

---

## Step 5 — Gate Decision (Completed)

### Gate Eligibility

- **Collection status:** `COLLECTED` ✅
- **Allow gate:** `true` ✅
- **Gate eligible:** `true` ✅

### Decision Logic Applied

| Rule | Threshold | Actual | Status |
|---|---|---|---|
| Rule 1 — P0 coverage *(AMENDED)* | 100% required | **100% risk coverage** (R-001 BLOCKER verified at unit level via T-R001-UNIT-001/002); **67% test-inventory** (2/3 planned tests implemented; T-R001-INT-001 never built) | ✅ MET (on risk-coverage basis) |
| Rule 2 — Overall coverage | ≥ 80% required | **95%** (39/41 in-scope ACs FULL) | ✅ MET |
| Rule 3 — P1 coverage | ≥ 80% required | **~91%** (11/12 in-scope; 2 DEFERRED to EventStore) | ✅ MET |
| Rule 4 — P1 PASS target | ≥ 90% | **~91%** | ✅ MET (would yield PASS) |
| Test-design Exit Criteria — CI guard T-R012-CI-001 | Required | **NOT IMPLEMENTED** | ❌ NOT MET |
| Test-design Exit Criteria — Integration tier T-R001-INT-001 *(AMENDED)* | Required (planned) | **NOT IMPLEMENTED** (file does not exist on disk) | ❌ NOT MET |
| Test-design Exit Criteria — PII non-leak sentinel gates | 3 expected | 1 explicit (tenant-index); 2 covered by behavioral analog only | ⚠️ PARTIAL |

### 🚨 GATE DECISION: **CONCERNS**

**Rationale (AMENDED):** P0 risk coverage is 100% (R-001 BLOCKER verified at unit level), P1 coverage is ~91%, overall is 95% — the formal priority-threshold logic on a risk-coverage basis still yields **PASS**. However, the test-design Exit Criteria explicitly require **two** items that are **not implemented**: (a) CI guard **T-R012-CI-001** (EventStore submodule pointer drift) in `.github/workflows/{ci,release}.yml`, and (b) the planned integration test **T-R001-INT-001** (`TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync` in `ProjectionWriteConformanceIntegrationTests.cs` — the file does not exist on disk). Additionally, two PARTIAL items (Story 10.2 AC#9 and Story 10.3B AC#7 / T-R007-UNIT-002) leave the R-007 negative-content sentinel-value gates incomplete on the audit-key and cancellation-failure paths — the behavior is covered, but the explicit zero-tolerance PII sentinel assertion is only present on one of the three paths.

**Murat's call:** **CONCERNS** is the honest answer (verdict unchanged by validation amendments). Coverage is excellent; the work is largely done; but two explicitly-named mandatory exit criteria are unmet (REC-1 + REC-1B), and the security-critical R-007 sentinel pattern should be applied consistently across all three exhaustion/cancellation diagnostics (REC-2, REC-3). None of this blocks the *code* from working; it blocks the *epic from formally closing per its own exit criteria*.

### Required Actions Before Epic Close

1. **HIGH — REC-1:** Implement T-R012-CI-001 submodule pointer guard in CI workflows. **Blocks epic close per test-design Exit Criteria.** Estimated effort: 2-4 hours. Owner: CI/Platform. Target date: 2026-05-26.
2. **HIGH — REC-1B (AMENDED):** Decide T-R001-INT-001 — build the planned integration test OR formally waive the integration tier via sprint-change-proposal. **Blocks epic close per test-design Exit Criteria** (planned defense-in-depth test was never built; R-001 unit coverage is sufficient for behavior verification). Estimated effort: 3-6 hours (build) or 0.5 hours (waive). Owner: Story 10.4 follow-up dev or SCP author. Target date: 2026-05-26.

### Recommended Actions (do not block epic close, advisory)

2. **MEDIUM — REC-2:** Add `Audit_RetryExhaustion_FailsObservablyAndDoesNotLeakSensitiveAuditAsync` mirroring the tenant-index sentinel pattern. Closes Story 10.2 AC#9 PARTIAL. Effort: 1-2 hours.
3. **MEDIUM — REC-3:** Add `Cancellation_Failure_DiagnosticDoesNotLeakSensitiveContextAsync` to `TenantsProjectionActorTests.cs`. Closes Story 10.3B AC#7 / T-R007-UNIT-002 PARTIAL. Effort: 1-2 hours.
4. **LOW — REC-4:** Cross-check EventStore Server.Tests carries R-006 cache-coherence coverage. Effort: 0.5 hours.
5. **LOW — REC-5:** Run `/bmad-testarch-test-review` against new conformance + actor cancellation test files. Effort: 1 hour.

### Decision Display

```
🚨 GATE DECISION: CONCERNS (verdict unchanged by 2026-05-19 validation amendments)

📊 Coverage Analysis (AMENDED):
- P0 Risk Coverage:       100% (R-001 verified at unit level)         → MET
- P0 Test-Inventory:       67% (2/3 planned tests implemented)       → ADVISORY GAP
- P1 Coverage:             ~91% (PASS target: 90%, min: 80%)         → MET
- Overall Coverage:        95%  (Minimum: 80%)                       → MET
- Test-design Exit Criteria (T-R012-CI-001 CI guard):                → NOT MET
- Test-design Exit Criteria (T-R001-INT-001 integration tier):       → NOT MET (AMENDED)
- Test-design Exit Criteria (PII non-leak sentinel gates):           → PARTIAL

✅ Decision Rationale (AMENDED):
P0 BLOCKER (R-001) is covered at unit level by T-R001-UNIT-001/002; the conformance suite
directly drives production TenantProjectionWritePolicy via R-008 fixture binding; the actor
cancellation taxonomy is preserved per Story 10.3B; the 655-passing-1-skipped post-Story-11-1
gate run on 2026-05-19 confirms no regression. The CONCERNS verdict is driven by TWO missing
test-design Exit Criteria items: (1) CI guard T-R012-CI-001, and (2) the planned integration
test T-R001-INT-001 which was never built (file ProjectionWriteConformanceIntegrationTests.cs
does not exist on disk). R-001 behavior is preserved by the existing unit tests, so neither
gap blocks the gate verdict from being CONCERNS rather than FAIL.

⚠️ Critical Gaps:   0 (R-001 covered at unit level)
⚠️ High Gaps:       1 (T-R001-INT-001 planned integration test never built — REC-1B)
⚠️ Mandatory Exit Criteria gaps: 2 (T-R012-CI-001 CI guard + T-R001-INT-001 integration tier)
⚠️ Sentinel-pattern PARTIAL items: 2 (R-007 analogs)

📝 Recommended Actions (top 4 — AMENDED):
1. HIGH — REC-1:  Implement T-R012-CI-001 CI submodule pointer guard         (target 2026-05-26)
2. HIGH — REC-1B: Build or formally waive T-R001-INT-001 integration tier    (target 2026-05-26)
3. MEDIUM — REC-2: Add Audit_RetryExhaustion sentinel-value gate
4. MEDIUM — REC-3: Add Cancellation_Failure sentinel-value gate

📂 Outputs:
- Traceability matrix (markdown):    _bmad-output/test-artifacts/traceability-matrix.md
- Coverage matrix (JSON):            _bmad-output/test-artifacts/trace-coverage-matrix-2026-05-19.json
- E2E trace summary (JSON):          _bmad-output/test-artifacts/e2e-trace-summary.json
- Gate decision (JSON):              _bmad-output/test-artifacts/gate-decision.json
- Validation report:                 _bmad-output/test-artifacts/trace-validation-report.md

⚠️  GATE: CONCERNS — Proceed with caution; address T-R012-CI-001 + decide T-R001-INT-001 before formal epic close.
```

---

## Workflow Complete

This trace + gate analysis closes Phase 2 of the `bmad-testarch-trace` workflow.

**Next recommended workflow:** Run REC-1 implementation as a focused patch (CI workflow change), then re-run this trace to confirm gate flips to PASS. REC-2 and REC-3 can ship in the Epic 10 retrospective patch series or a dedicated security-hardening follow-up.
