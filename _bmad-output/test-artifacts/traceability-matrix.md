---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-map-criteria', 'step-04-analyze-gaps', 'step-05-gate-decision']
lastStep: 'step-05-gate-decision'
lastSaved: '2026-05-19'
mode: 'story-level'
target_scope: 'Story 10.4 — Projection Write Conformance and Recovery Tests'
target_story_id: '10-4'
target_story_status: 'done'
coverageBasis: 'acceptance_criteria'
oracleResolutionMode: 'formal_requirements'
oracleConfidence: 'high'
oracleSources:
  - '_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md'
  - '_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md'
  - '_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md'
  - '_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md'
  - '_bmad-output/test-artifacts/test-design-epic-10.md'
  - '_bmad-output/test-artifacts/atdd-checklist-10-4-projection-write-conformance-and-recovery-tests.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
externalPointerStatus: 'not_used'
priorTraceContext: '_bmad-output/test-artifacts/traceability-matrix-epic-10-2026-05-19.md'
priorReviewContext: '_bmad-output/test-artifacts/traceability-matrix-story-10-4-review-stage-2026-05-19.md'
tempCoverageMatrixPath: '_bmad-output/test-artifacts/trace-coverage-matrix-story-10-4-2026-05-19.json'
gateDecision: 'PASS'
gateStatusFile: '_bmad-output/test-artifacts/gate-decision.json'
e2eTraceSummaryFile: '_bmad-output/test-artifacts/e2e-trace-summary.json'
workflowStatus: 'completed'
---

# Coverage Traceability Matrix — Story 10.4: Projection Write Conformance and Recovery Tests

**Date:** 2026-05-19 (refreshed against `done` state)
**Author:** Jerome (drafted by Murat — Master Test Architect)
**Status:** Final (workflow complete; gate **PASS**)
**Scope:** Story-level trace for Story 10.4 (**status: `done`** as of 2026-05-19)

> **Trace history:**
> - **2026-05-19 (initial Story 10.4 trace):** Run while story was in `review` state with 16 open Review patches. Verdict: **CONCERNS**. Archived to `traceability-matrix-story-10-4-review-stage-2026-05-19.md`.
> - **2026-05-19 (this refresh):** Story moved to `done` after all 16 patches applied (12 of 17 actionable, 1 dismissed, 4 from resolved decisions D2–D5). Conformance fixture refactored to drive production behavior exclusively via `ProjectAsync` (D5 / patch 16); `ApplyIndexEvent` visibility reverted to `private` per spec line 67. Full solution gate **660 passed / 1 skipped**. Verdict upgraded: **PASS**.
> - **Prior Epic 10 trace:** Archived to `traceability-matrix-epic-10-2026-05-19.md`.

---

## Step 1 — Load Context (Completed)

### 1.1 Coverage Oracle Resolution

| Attribute | Value |
|---|---|
| **Resolution mode** | `formal_requirements` |
| **Coverage basis** | `acceptance_criteria` — 11 numbered ACs |
| **Confidence** | **HIGH** |
| **External pointer status** | `not_used` |
| **Synthetic oracle** | not engaged |

### 1.2 Story 10.4 State at Gate

| Field | Value |
|---|---|
| **Story status** | **`done`** (line 3 of story file) |
| **Completion note** | "Deterministic projection write conformance and recovery tests implemented and reviewed; post-review patches applied (12 of 17 actionable, 1 dismissed, 4 from resolved decisions). Conformance fixture refactored to drive production behavior exclusively through `ProjectAsync` after `ApplyIndexEvent` visibility reverted to `private`. Full solution test gate passes (660 passed, 1 skipped)." |
| **Story-file gate baseline** | **660 passed, 1 skipped** |
| **This-trace solution gate** | **660 passed, 1 skipped** — verified by re-run: 48 + 35 + 17 + 89 + 438 + 33 = 660 passed; 1 skip is `Hexalith.Tenants.IntegrationTests.SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` (DAPR/Docker precondition) |
| **Focused conformance + handler run** | **28/28 passing** (11 conformance + 17 handler) |
| **Build status** | 0 warnings / 0 errors (Debug, no-restore) |
| **Open Review patches** | **0** — all 16 patches resolved (15 applied + 1 dismissed) |

### 1.3 Story 10.4 Acceptance Criteria (Oracle Items)

| AC# | Summary | Risk linkage |
|---|---|---|
| AC#1 | Conformance proves no event loss on all 3 keys | R-001, R-002, R-003 |
| AC#2 | Retry exercise: eventual success OR safe observable failure | R-001, R-002, R-003 |
| AC#3 | Retry exhaustion + diagnostic redaction | R-007 |
| AC#4 | Event ordering preserved across mixed event types | R-002, R-003 |
| AC#5 | Reusable fixture contract | R-008 |
| AC#6 | Per-key transactionality; idempotency | R-001, R-003, R-017 |
| AC#7 | Retry-attempt boundaries via observable seams | R-005 |
| AC#8 | Audit duplicate `EventId` persisted-authoritative + Timestamp+EventId ordering | R-004 |
| AC#9 | Dev Agent Record captures prerequisite evidence | R-008 |
| AC#10 | Deterministic per-key state-store scripts fail fast | R-008 |
| AC#11 | Structured-field diagnostic assertions over brittle full-message matching | R-007, R-016 |

---

## Step 2 — Test Discovery (Completed)

### 2.1 Primary Story 10.4 Deliverables (Tier 1 — Unit)

**File: `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`** (704 lines, **11 `[Fact]` tests, all passing**)

| Test ID | Title | Line | Status | T-RXXX / Patch mapping |
|---|---|---|---|---|
| T-10.4-CONF-001 | `TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync` | 45 | ✅ | T-R002-UNIT-001/002 + **P10 (Apply-based seed)** |
| T-10.4-CONF-002 | `TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync` | 109 | ✅ | **T-R001-UNIT-001 (P0 BLOCKER)** + **P16/D5 (ProjectAsync-driven)** |
| T-10.4-CONF-003 | `TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync` | 179 | ✅ | **T-R001-UNIT-002 (P0)** + **P9 (extended sentinel)** + **P3 (structured KV)** + **P5 (MarkTerminalFailure)** + **P7 (MaxAttempts constant)** |
| T-10.4-CONF-004 | `TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync` | 273 | ✅ | T-R017-UNIT-001 + **P5 (MarkTerminalFailure)** |
| T-10.4-CONF-005 | `TenantDetail_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync` | 313 | ✅ | **NEW (P14/D3)** |
| T-10.4-CONF-006 | `Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicateAndOrdersByTimestampThenEventIdAsync` | 359 | ✅ | T-R003-UNIT-001 + T-R004-UNIT-001/002 + **P8 (wins-by-contest)** |
| T-10.4-CONF-007 | `Audit_ReplayAfterAuditSaveAndLaterIndexFailure_DoesNotDuplicateEntriesAsync` | 428 | ✅ | T-R003-UNIT-002 + T-R004-UNIT-001 |
| T-10.4-CONF-008 | `TenantIndex_ReplayAfterPartialSuccess_DoesNotDuplicateOrLoseEntriesAsync` | 466 | ✅ | **NEW (P11) — fills AC#6 index-replay gap** |
| T-10.4-CONF-009 | `Conformance_MixedLifecycleAndMembershipAndConfiguration_OrderingPreservedAsync` | 517 | ✅ | **NEW (P13/D2) — 9-event-type ordering** |
| T-10.4-CONF-010 | `Audit_MalformedPayloadPreserved_AndInvariantFailureAbortsBeforeWritesAsync` | 587 | ✅ | **NEW (P15a/D4) — malformed payload + invariant abort** |
| T-10.4-CONF-011 | `Audit_MixedEventTypeOrdering_PreservedAcrossConflictReloadAsync` | 644 | ✅ | **NEW (P15b/D4) — audit ordering 9 event types** |

**File: `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`** (342 lines — fixture, R-008 contract)

| Identity | Role | Patches applied |
|---|---|---|
| `ProjectionWriteConformanceFixture.RunProjectionHandlerAsync()` | Sole entry path for tests — drives production `TenantProjectionHandler.ProjectAsync` | **P4 + P16/D5 (BindsToProductionPolicy removed; tests cannot bypass production by construction)** |
| `ScriptedTenantProjectionStateStore.MarkTerminalFailure(key)` | AC10 enforcement: throws on subsequent reads/writes against terminally-failed key | **P5** |
| `CapturingLogger<TCategory>.Log(...)` | Captures structured `IReadOnlyList<KV>` state via `ExtractStructuredState`; exposes `CapturedLog.StructuredState` | **P3** |
| `GetReadAttemptCount(key)`, `GetSaveAttemptCount(key)`, `GetSaveAttempt(key, index)`, `GetLogEntries(eventId)` | Fixture contract API for future projection opt-in | **P6** |

**File: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`** (593 lines, **17 `[Fact]` tests, all passing**)

All 17 handler-level tests passing. The dead `stateStore.PlainSaveAttempts.ShouldBeEmpty()` assertion (formerly at line 866) has been removed (P12 ✅). The `PlainSaveAttempts` property remains on the inner test-only state store for capture purposes but no test asserts on it.

### 2.2 Production Code Change (Patch 16 / D5)

| File | Line | Change |
|---|---|---|
| `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs` | 220 | `private static void ApplyIndexEvent(...)` — visibility reverted from `internal` to `private` ✅ |

Tests no longer bypass `ProjectAsync` to reach `ApplyIndexEvent` directly. The fixture's `RunSingletonIndexConformanceAsync` helper was removed; `T-10.4-CONF-002` now drives the production code path via `RunProjectionHandlerAsync` only.

### 2.3 Level Categorization

| Level | Story-10.4-relevant tests | Notes |
|---|---|---|
| **E2E** | 0 | Out of scope per test design |
| **API** | 0 | Below the API layer |
| **Component** | 0 | Backend story |
| **Unit (Tier 1)** | **28 primary** (11 conformance + 17 handler) + supporting (audit/index/tenant model) | All passing |
| **Fixture** | 1 (ProjectionWriteConformanceFixture) | R-008 fixture-binding rule |

---

## Step 3 — Map Coverage Oracle to Tests (Completed)

### 3.1 AC-to-Test Traceability Matrix

| AC# | Coverage | Priority | Primary tests | Patch status |
|---|---|---|---|---|
| **AC#1** | **FULL** | **P0** | T-10.4-CONF-002 (R-001 BLOCKER), T-10.4-CONF-001, T-10.4-CONF-006, T-10.4-CONF-008 | ✅ All applied |
| **AC#2** | **FULL** | P1 | T-10.4-CONF-001 (P10 — Apply-based seed), T-10.4-CONF-002/003/006 | ✅ P10 applied |
| **AC#3** | **FULL** | P1 | T-10.4-CONF-003 (extended sentinel: Name + UserId + ConfigValue), T-10.4-CONF-005 (tenant-detail exhaustion) | ✅ P9 applied + P14/D3 added |
| **AC#4** | **FULL** | P1 | T-10.4-CONF-009 (9-event-type mixed), T-10.4-CONF-011 (audit mixed), T-10.4-CONF-001/006 | ✅ P13/D2 + P15b/D4 added |
| **AC#5** | **FULL** | P1 | `ProjectionWriteConformanceFixture` with `GetReadAttemptCount` / `GetSaveAttemptCount` / `GetSaveAttempt` / `GetLogEntries` | ✅ P4 + P6 applied |
| **AC#6** | **FULL** | P1 | T-10.4-CONF-004, T-10.4-CONF-007 (audit replay), T-10.4-CONF-008 (index replay — **new**) | ✅ P11 added |
| **AC#7** | **FULL** | P1 | T-10.4-CONF-002/003 with `MaxAttempts` constant references + `MarkTerminalFailure` | ✅ P5 + P7 + P12 applied |
| **AC#8** | **FULL** | P1 | T-10.4-CONF-006 (persisted wins by **contest** — distinct ActorId + payload) | ✅ P8 applied |
| **AC#9** | **FULL** | P2 | Dev Agent Record evidence (story file lines 161–166) | ✅ P1 dismissed (false positive); P2 applied (Completion note refreshed) |
| **AC#10** | **FULL** | P1 | `ScriptedTenantProjectionStateStore.MarkTerminalFailure(key)` enforces AC10 invariant at seam | ✅ P5 applied |
| **AC#11** | **FULL** | P2 | T-10.4-CONF-003 asserts on `StructuredState["StateKeyCategory"]`, `["AttemptCount"]`, `["MaxAttempts"]`, `["CorrelationId"]` | ✅ P3 applied |

### 3.2 Coverage Logic Validation

| Check | Status |
|---|---|
| P0/P1 items have coverage | ✅ All 11 ACs FULL |
| No duplicate coverage across levels without justification | ✅ Defense-in-depth at handler + conformance tiers |
| Items not happy-path-only | ✅ Exhaustion + replay + invariant abort all covered |
| API items not marked FULL without endpoint check | N/A |
| Auth/authz negative paths | N/A |
| Synthetic UI journeys | N/A |

---

## Step 4 — Gap Analysis (Completed)

### 4.1 Coverage Statistics

| Metric | Value |
|---|---|
| **Total requirements (ACs)** | 11 |
| **Fully covered (FULL)** | **11** |
| **Partially covered** | 0 |
| **Uncovered (NONE)** | 0 |
| **Overall coverage %** | **100%** |

### 4.2 Priority Breakdown

| Priority | Total | FULL | % |
|---|---|---|---|
| **P0** | 1 | 1 | **100%** |
| **P1** | 8 | 8 | **100%** |
| **P2** | 2 | 2 | **100%** |
| **P3** | 0 | 0 | — |

### 4.3 Gap Analysis

| Severity | Count |
|---|---|
| Critical (P0) | **0** |
| High (P1) | **0** |
| Medium (P2) | **0** |
| Low (P3) | **0** |
| Partial-coverage refinements | **0** (all applied) |

### 4.4 Coverage Heuristics

| Heuristic | Status |
|---|---|
| State-key coverage (all 3 keys) | ✅ FULL |
| Retry-exhaustion coverage | ✅ FULL (all 3 keys + cross-key) |
| Idempotency / EventId dedup | ✅ FULL (wins-by-**contest** via distinct ActorId + narrative payload — P8 applied) |
| Cancellation precedence | ✅ FULL (3 handler-level + ~10 actor-layer) |
| PII non-leak sentinel gates | ✅ FULL (sentinel breadth extended to Name + UserId + ConfigValue per P9) |
| Invariant-failure boundary | ✅ FULL (T-10.4-CONF-010 covers malformed + invariant abort) |
| Stale-instance reuse | ✅ FULL (`MarkTerminalFailure` enforces; tests assert per-attempt fresh instance) |
| Fixture contract API | ✅ FULL (P6 applied) |
| Structured KV diagnostic assertion | ✅ FULL (`StructuredState` Dictionary captured + asserted per AC#11) |
| Defense-in-depth integration tier (R-001) | ⚠️ **STILL MISSING** for T-R001-INT-001 — carryover from Epic 10 REC-1B; blocks **Epic 10 close**, not Story 10.4 close |

### 4.5 Outstanding Recommendations (Epic-level, not Story-level)

| ID | Priority | Action | Effort | Blocks |
|---|---|---|---|---|
| `REC-EPIC-10-1` | MEDIUM | Decide T-R001-INT-001 build-or-waive (Epic 10 retrospective gate) | 0.5h (waive) / 3-6h (build) | Epic 10 retrospective |
| `REC-EPIC-10-2` | LOW | Optional: run `/bmad-testarch-test-review` against the conformance file + fixture | 1h | none |

---

## Step 5 — Gate Decision (Completed — Phase 2 Final)

### 5.1 Gate Decision Logic Applied

| Rule | Threshold | Actual | Outcome |
|---|---|---|---|
| Rule 1: P0 ≥ 100% | 100% | 100% | ✅ PASS |
| Rule 2: Overall ≥ 80% | ≥80% | **100%** | ✅ PASS |
| Rule 3: P1 ≥ 80% | ≥80% | **100%** | ✅ PASS |
| Rule 4: P1 ≥ 90% (PASS threshold) | ≥90% | **100%** | ✅ PASS |

**Workflow outcome:** `PASS`

### 5.2 Final Gate Decision

> # 🟢 PASS — Story 10.4 (done)

**Verdict:** **PASS**

**Decision date:** 2026-05-19

**Decision mode:** human_with_data

**Confidence:** HIGH

**Rationale:** Story 10.4 has full coverage on all 11 ACs (100% FULL). All 28 in-scope tests pass (11 conformance + 17 handler). Full solution gate passes 660 / 1 skipped (the 1 skip is an unrelated DAPR-precondition gated snapshot performance test). All 16 Review patches resolved (15 applied + 1 dismissed as false positive). Production code change (P16/D5) confirms `ApplyIndexEvent` reverted to `private`; conformance fixture drives behavior exclusively via `TenantProjectionHandler.ProjectAsync`. No blocking items remain at the story level.

### 5.3 Gate Criteria Summary

| Criterion | Required | Actual | Status |
|---|---|---|---|
| P0 coverage | 100% | 100% FULL | ✅ MET |
| P1 coverage (target) | 90% | 100% FULL | ✅ MET |
| P1 coverage (minimum) | 80% | 100% FULL | ✅ MET |
| Overall coverage | 80% | 100% FULL | ✅ MET |
| NFR assessment | scope-dependent | NOT_ASSESSED | — (optional; consider `/bmad-testarch-nfr` if NFR sign-off needed for Epic 10 close) |

### 5.4 Blocking Items (Story 10.4)

**None.** Story is `done`.

### 5.5 Epic-Close Items (not blocking Story 10.4)

| # | ID | Severity | Action | Target | Effort |
|---|---|---|---|---|---|
| 1 | T-R001-INT-001 | MEDIUM | Decide DAPR-backed integration test build/waive (carryover from Epic 10 trace REC-1B) | 2026-05-26 | 0.5h (waive) / 3-6h (build) |

### 5.6 Workflow Status

| Output artifact | Path |
|---|---|
| **Traceability matrix (markdown)** | `_bmad-output/test-artifacts/traceability-matrix.md` (this file) |
| **Coverage matrix (JSON)** | `_bmad-output/test-artifacts/trace-coverage-matrix-story-10-4-2026-05-19.json` |
| **Gate decision (JSON)** | `_bmad-output/test-artifacts/gate-decision.json` |
| **e2e trace summary (JSON)** | `_bmad-output/test-artifacts/e2e-trace-summary.json` |
| **Archived: review-stage Story 10.4 trace** | `_bmad-output/test-artifacts/traceability-matrix-story-10-4-review-stage-2026-05-19.md` |
| **Archived: Epic 10 trace** | `_bmad-output/test-artifacts/traceability-matrix-epic-10-2026-05-19.md` |

---

## Workflow Complete

**Final verdict for Story 10.4 (status: `done`):**

> ## 🟢 PASS
>
> Story 10.4 achieves full coverage on all 11 ACs (100% FULL). 28/28 in-scope tests pass; full solution gate 660 passed / 1 skipped. All 16 Review patches resolved (15 applied + 1 dismissed). Production code change (P16/D5) successfully reverted `ApplyIndexEvent` to `private`. No blocking items remain. Story may be closed and Epic 10 retrospective scheduled (gated only on the optional T-R001-INT-001 build-or-waive decision, which blocks Epic 10 close, not Story 10.4).

**Owner next actions:**

1. **Story 10.4 (Jerome / dev):** Already closed (`done`). No follow-up required.
2. **Epic 10 close (Jerome / dev):** Decide T-R001-INT-001 build-or-waive in a sprint-change-proposal (target 2026-05-26).
3. **Epic 10 retrospective (Jerome):** Schedule once T-R001-INT-001 is decided.
4. **Optional (Murat — TEA):** Run `/bmad-testarch-test-review` against `ProjectionWriteConformanceTests.cs` + `ProjectionWriteConformanceFixture.cs` to validate test-quality DoD compliance.

---

**End of traceability matrix.**
