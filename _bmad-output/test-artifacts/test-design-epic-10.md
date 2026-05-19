---
workflowStatus: 'completed'
totalSteps: 5
stepsCompleted:
  - step-01-detect-mode
  - step-02-load-context
  - step-03-risk-and-testability
  - step-04-coverage-plan
  - step-05-generate-output
lastStep: 'step-05-generate-output'
nextStep: ''
lastSaved: '2026-05-19'
mode: 'epic-level'
target_scope: 'Epic 10 — Durable Projection Write Safety'
target_epic_id: 'epic-10'
inputDocuments:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md
  - _bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md
  - _bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
---

# Test Design: Epic 10 — Durable Projection Write Safety

**Date:** 2026-05-19
**Author:** Jerome (drafted by Murat — Master Test Architect)
**Status:** Draft

---

## Executive Summary

**Scope:** Epic-level test design for **Epic 10 — Durable Projection Write Safety** in Hexalith.Tenants. The epic covers projection persistence behavior under concurrent event delivery, retry semantics, partial-failure recovery, and (via Story 10.3B) request cancellation against three DAPR state-store keys: `projection:tenants:{tenantId}`, `projection:tenant-index:singleton`, and `audit:{tenantId}`.

**Stories covered:** 10.1 (done), 10.2 (done), 10.3A (review — EventStore prerequisite), 10.3B (ready-for-dev — Tenants-side cancellation), **10.4 (ready-for-dev — this design directly informs 10.4's conformance test suite)**.

**FRs reinforced:** FR25, FR26, FR27, FR28, FR29, FR30, FR53.
**NFRs reinforced:** NFR5 (no cross-tenant leak), NFR17 (graceful pub/sub degradation), NFR20 (event store as single source of truth), NFR23 (no data loss).

**Risk Summary:**

- Total risks identified: **23**
- **CRITICAL (score 9, BLOCK):** 1 — silent loss on `projection:tenant-index:singleton`
- **HIGH (score 6–8, MITIGATE):** 7 — tenant detail LWW, audit loss, duplicate EventId merge, stale model reuse, ETag cache poisoning, diagnostic PII leak, fixture-diverges-from-prod
- **MEDIUM (score 4–5, MONITOR):** 8
- **LOW (score 1–3, DOCUMENT):** 7
- **Risk concentration:** DATA category (8 of 23; 5 of 8 high-or-above) — expected for a write-durability epic.

**Coverage Summary:**

- **P0** scenarios: 3 (D2 BLOCKER trio)
- **P1** scenarios: 12
- **P2** scenarios: 10 + 1 fixture rule + 1 CI guard
- **P3** scenarios: 0 (low risks documented only)
- **Total tests:** 25 + 1 fixture rule + 1 CI guard
- **Effort estimate:** **~65–110 hours** (~2–3 weeks for 1 developer or ~1–1.5 weeks paired)

> **Note on priority labels:** P0/P1/P2/P3 here express **risk-driven priority**, not execution timing. Timing is set by the **Execution Strategy** section (PR / Nightly / Weekly).

---

## Not in Scope

| Item                                                            | Reasoning                                                                                                            | Mitigation                                                          |
|-----------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------|
| **EventStore-side cancellation API tests (Story 10.3A scope)**   | Already implemented in `Hexalith.EventStore` submodule at commit `bcccd504`; covered by 82 tests in Server.Tests.    | Out of scope here; tracked under Story 10.3A.                       |
| **Cross-key atomic rollback / two-phase commit testing**         | Architecture explicitly disclaims cross-key atomicity. DAPR per-key ETags + replay/idempotency is the recovery model.| Risk R-017 documented; no compensating-write tests.                  |
| **UI projection rendering / Blazor Admin UI tests**              | No Tenants UI exists in Epic 10 scope. Admin UI is Phase 2 (Epic 12, currently backlog).                              | Defer to Phase 2 UI epic.                                            |
| **Performance / load-test smoke on retry storm (R-014)**         | Performance NFR pass owns this; Epic 10 is correctness-first.                                                         | Tracked as MEDIUM (R-014); deferred to weekly/NFR follow-up.         |
| **Live DAPR sidecar / Redis / Aspire for unit tests**            | Story specs forbid sleeps, live sidecars, real network calls in focused conformance tests.                            | Deterministic state-store fakes + 2 Tier-2 integration sanity tests.|
| **Property-based / FsCheck audit-merge fuzzing**                 | Out of Epic 10's deterministic conformance scope; nightly property lane (already filtered out) can pick up later.    | Documented as future enhancement.                                    |

---

## Risk Assessment

**Risk Category Legend**
- **TECH**: Technical/Architecture
- **SEC**: Security / privacy / data exposure
- **PERF**: Performance / scalability / resource limits
- **DATA**: Data integrity / loss / corruption
- **BUS**: Business logic correctness
- **OPS**: Operations / deployment / observability

### High-Priority Risks (Score ≥ 6) — MITIGATE or BLOCK

| Risk ID | Cat  | Description                                                                                                  | P | I | Score | Mitigation                                                                                          | Owner            | Timeline             |
|---------|------|--------------------------------------------------------------------------------------------------------------|---|---|-------|-----------------------------------------------------------------------------------------------------|------------------|----------------------|
| R-001   | DATA | Silent last-writer-wins on `projection:tenant-index:singleton` — single highest-contention state key         | 3 | 3 | **9** | Story 10.1's optimistic concurrency + Story 10.4 conformance trio (T-R001-UNIT-001/002, T-R001-INT-001) | Story 10.4 dev   | Before Epic 10 ships |
| R-002   | DATA | Silent last-writer-wins on `projection:tenants:{tenantId}` (concurrent member/config events)                  | 2 | 3 | 6     | Story 10.1 ETag retry + T-R002-UNIT-001/002                                                          | Story 10.4 dev   | Before Epic 10 ships |
| R-003   | DATA | Audit entry loss under concurrent role-change events on `audit:{tenantId}`                                    | 2 | 3 | 6     | Story 10.2 idempotent merge + T-R003-UNIT-001/002                                                    | Story 10.4 dev   | Before Epic 10 ships |
| R-004   | DATA | Duplicate `EventId` admits non-idempotent merge (DAPR at-least-once delivery, FR42)                           | 3 | 2 | 6     | Story 10.2 EventId-keyed idempotency + T-R004-UNIT-001/002                                           | Story 10.4 dev   | Before Epic 10 ships |
| R-005   | DATA | Stale `TenantReadModel`/`TenantIndexReadModel` instance reused across retry attempts                          | 2 | 3 | 6     | Test asserts new instance per attempt + N+1 read counts (T-R005-UNIT-001)                            | Story 10.4 dev   | Before Epic 10 ships |
| R-006   | TECH | `CachingProjectionActor` ETag cache poisoning by stale snapshot after retry                                   | 2 | 3 | 6     | Cache coherence test after conflict-success (T-R006-UNIT-001, T-R006-INT-001)                        | Story 10.4 dev   | Before Epic 10 ships |
| R-007   | SEC  | Diagnostic logs leak tenant/user payload / cursor / audit content on retry-exhaustion / cancellation failure | 2 | 3 | 6     | Negative log-content assertions (T-R007-UNIT-001/002/003) — zero-tolerance gate                      | Story 10.4 + 10.3B dev | Before Epic 10 ships |
| R-008   | OPS  | Conformance fixture re-implements retry/merge algorithm → green tests, broken prod                            | 2 | 3 | 6     | Fixture-design rule: drive production helper via spy/type assertion (T-R008-FIXTURE-001)             | Story 10.4 dev   | Before Epic 10 ships |

### Medium-Priority Risks (Score 4–5) — MONITOR

| Risk ID | Cat  | Description                                                                                                  | P | I | Score | Mitigation                                                                                          | Owner               |
|---------|------|--------------------------------------------------------------------------------------------------------------|---|---|-------|-----------------------------------------------------------------------------------------------------|---------------------|
| R-009   | DATA | Non-deterministic audit ordering after conflict-reload breaks cursor pagination                              | 2 | 2 | 4     | Stable `Timestamp` then `EventId` sort + T-R009-UNIT-001                                              | Story 10.4 dev      |
| R-010   | TECH | DAPR actor proxy cancellation transport ambiguity (boundary chosen wrong → cancellation dropped)             | 2 | 2 | 4     | Story 10.3B verifies 10.3A handoff commit `bcccd504`; pre-cancellation + mid-flow + taxonomy tests   | Story 10.3B dev     |
| R-011   | TECH | Aspire topology fixture flakiness — Aspire 13.3.3 + Redis/HTTPS probe drift                                  | 2 | 2 | 4     | Existing post-10.3a hardening + Tier 3 regression guard with `/alive` liveness; monitor pass rate    | Integration test owner |
| R-012   | TECH | EventStore submodule pointer drift from pinned cancellation API commit                                       | 2 | 2 | 4     | CI guard `T-R012-CI-001` verifies submodule commit; fail build on drift                              | CI / Platform       |
| R-013   | TECH | `TenantProjectionWritePolicy` 3-attempt budget exhausted under heavy singleton-index contention              | 2 | 2 | 4     | Deterministic 3-attempt scripted test (T-R013-UNIT-001); operational signal via O1                   | Story 10.4 dev      |
| R-014   | PERF | Retry storm: concurrent tenant writes → repeated ETag conflicts → CPU/state-store thrash                     | 2 | 2 | 4     | Weekly performance smoke (NFR follow-up); not blocking Epic 10 ship                                  | Performance NFR owner |
| R-015   | BUS  | Audit invariant boundary regression — malformed-JSON-skip vs metadata-invariant-failure split weakens         | 2 | 2 | 4     | Regression guards T-R015-UNIT-001/002 (preserve existing 10.2 invariant tests)                        | Story 10.4 dev      |
| R-016   | OPS  | Retry-exhaustion observability gap — no structured ops signal for projection write loss                       | 2 | 2 | 4     | Positive log-shape assertion (T-R016-UNIT-001); flag for future metrics emission                      | Story 10.4 dev      |

### Low-Priority Risks (Score 1–3) — DOCUMENT

| Risk ID | Cat  | Description                                                                                                  | P | I | Score | Action / Note                                                                                    |
|---------|------|--------------------------------------------------------------------------------------------------------------|---|---|-------|--------------------------------------------------------------------------------------------------|
| R-017   | DATA | Cross-key transactional claim creep (future change implies tenant detail + audit + index atomicity)          | 1 | 3 | 3     | Scope boundary comment in conformance fixture; no test                                            |
| R-018   | SEC  | Cross-tenant data leak via projection recovery path (NFR5 violation)                                          | 1 | 3 | 3     | Already covered by NFR5 Tier 3 tests + per-key fixture scripting; no new test                    |
| R-019   | SEC  | Cancellation short-circuits before authorization check, masking auth regression                                | 1 | 2 | 2     | Covered transitively by 9.3/9.4 forbidden-precedence tests + T-R010 pre-cancel-after-auth gate    |
| R-020   | PERF | Audit merge O(n) on large audit histories (10K+ entries/tenant)                                               | 2 | 1 | 2     | Defer to Performance NFR pass                                                                     |
| R-021   | PERF | Cancellation checkpoint placement late → minimal compute saved on cancellation                                | 2 | 1 | 2     | Story 10.3B design choice; not a defect                                                            |
| R-022   | BUS  | Event-order divergence between detail/audit/index views — eventually-consistent partial state                | 2 | 1 | 2     | Architecture acceptance; queries handle eventual consistency                                       |
| R-023   | OPS  | `InternalsVisibleTo` creep widens production API surface for test convenience                                 | 1 | 1 | 1     | Story 10.4 acceptance constrains to narrowest accessibility change                                |

---

## Entry Criteria

- [x] Stories 10.1 (optimistic concurrency) and 10.2 (audit write safety) marked **done** with stable helper/adapter contracts
- [x] `TenantProjectionWritePolicy.cs` retry limit, failure shape, and diagnostic fields documented
- [x] Story 10.3A in **review** — EventStore submodule pinned at commit `bcccd504` exposing cancellation API
- [ ] Story 10.4 author confirms prerequisite evidence in Dev Agent Record (per 10.4 AC #9)
- [ ] xUnit v3 + Shouldly + NSubstitute test infra working in `tests/Hexalith.Tenants.Server.Tests/Projections/`
- [ ] Local `dapr init` available for Tier-2 integration sanity tests (skip-by-precondition otherwise)

## Exit Criteria

- [ ] All P0 tests (R-001 trio) implemented and passing
- [ ] All P1 tests (12) implemented and passing OR documented architecture-lead waiver
- [ ] P2 tests covered or explicitly deferred with ≤60-day deadline in `deferred-work.md`
- [ ] No diagnostic-leak negative-assertion failures (R-007)
- [ ] Line coverage on `TenantProjectionWritePolicy.cs`, `TenantProjectionHandler.cs`, `TenantAuditProjection.cs` ≥ 90%
- [ ] `AspireTopologyTests` pass rate ≥ 95% over the last 10 CI runs (R-011 fragility tolerance)
- [ ] CI guard `T-R012-CI-001` (EventStore submodule pointer) deployed and passing
- [ ] Conformance fixture proven (via mechanical assertion `T-R008-FIXTURE-001`) to drive production helper, not a test re-implementation

---

## Test Coverage Plan

> **Reminder:** P0/P1/P2/P3 here express priority/risk classification, NOT execution timing. Timing is set in the Execution Strategy section.

### P0 (Critical)

**Criteria:** Blocks core projection durability + risk score = 9 (BLOCK) + no acceptable workaround. R-001 (singleton index silent LWW) is the only P0 trigger.

| Test ID         | Requirement                                              | Test Level | Risk Link | Owner          | Notes                                                                                       |
|-----------------|----------------------------------------------------------|------------|-----------|----------------|---------------------------------------------------------------------------------------------|
| T-R001-UNIT-001 | Conflict-then-success preserves previously indexed tenants | Unit       | R-001     | Story 10.4 dev | Scripted state-store fixture; asserts final `TenantIndexReadModel` content                  |
| T-R001-UNIT-002 | Retry exhaustion fails observably without claiming success | Unit       | R-001     | Story 10.4 dev | Asserts `ProjectionResponse.Success == false` + structured diagnostic shape (no payload)    |
| T-R001-INT-001  | Real DAPR-backed conflict-then-success preserves index    | Tier 2     | R-001     | Story 10.4 dev | Skip-by-precondition if DAPR/Redis unavailable; proves fake matches real ETag behavior      |

**Total P0:** 3 tests · **~12–20 hours** (includes singleton-index fixture extension)

### P1 (High)

**Criteria:** Score 6–8 risks (MITIGATE) with mitigation owned by a covering test. Each P1 risk must be covered before Epic 10 ships.

| Test ID         | Requirement                                                                                  | Test Level | Risk Link  | Owner               | Notes                                                                                              |
|-----------------|----------------------------------------------------------------------------------------------|------------|------------|---------------------|----------------------------------------------------------------------------------------------------|
| T-R002-UNIT-001 | Tenant detail conflict-then-success preserves all incoming lifecycle/membership/config events | Unit       | R-002, R-005 | Story 10.4 dev      | Mixed event batch; final state contains all distinct events exactly once                          |
| T-R002-UNIT-002 | Tenant detail retry exhaustion fails observably                                              | Unit       | R-002      | Story 10.4 dev      | Same DoD as R-001-UNIT-002 but on `projection:tenants:{tenantId}` key                              |
| T-R003-UNIT-001 | Audit conflict-then-success preserves original + externally-added + incoming entries          | Unit       | R-003      | Story 10.4 dev      | Three entry groups merge; no duplicates; cursor stability                                          |
| T-R003-UNIT-002 | Audit retry exhaustion fails observably                                                       | Unit       | R-003      | Story 10.4 dev      | Same pattern for `audit:{tenantId}` key                                                            |
| T-R004-UNIT-001 | Duplicate `EventId`: persisted entry authoritative, duplicate suppressed                      | Unit       | R-004      | Story 10.4 dev      | Same EventId + same payload → 1 entry in final state                                               |
| T-R004-UNIT-002 | Duplicate `EventId` with mismatched payload: persisted wins, **no payload leak in diagnostics** | Unit     | R-004, R-007 | Story 10.4 dev      | Combined positive + negative assertion                                                              |
| T-R005-UNIT-001 | Retry after conflict loads fresh model — no stale instance reuse                              | Unit       | R-005      | Story 10.4 dev      | NSubstitute spy on state-store reads; identity-check each attempt sees new model                   |
| T-R006-UNIT-001 | `CachingProjectionActor` after conflict-success returns persisted state, not cached snapshot  | Unit       | R-006      | Story 10.4 dev      | Cache coherence after retry; persisted state authoritative                                          |
| T-R006-INT-001  | Read after conflict resolution cache-coherent with real DAPR storage                          | Tier 2     | R-006      | Story 10.4 dev      | Light integration sanity check; skip-by-precondition                                                |
| T-R007-UNIT-001 | Retry-exhaustion diagnostics contain no tenant payload or PII                                 | Unit       | R-007      | Story 10.4 dev      | **Negative assertion** — diagnostic must NOT contain `tenantName`, `userId`, audit payload, cursor, configuration values, serialized event bytes |
| T-R007-UNIT-002 | Cancellation-failure diagnostics contain no tenant payload or PII                             | Unit       | R-007      | Story 10.3B dev     | Negative assertion tied to 10.3B's cancellation diagnostic surface                                  |
| T-R007-UNIT-003 | Duplicate-EventId-mismatch diagnostic carries no incoming payload                              | Unit       | R-007, R-004 | Story 10.4 dev      | Combines R-007 + R-004 negative assertion                                                            |
| T-R008-FIXTURE-001 | `ProjectionWriteConformanceFixture` drives production `TenantProjectionWritePolicy`         | Fixture    | R-008      | Story 10.4 dev      | Mechanical assertion at fixture construction; a fixture-internal retry/merge re-implementation fails this check |

**Total P1:** 12 tests + 1 fixture rule · **~30–50 hours**

### P2 (Medium)

**Criteria:** Score 4–5 risks (MONITOR). Cover where reasonable; documented deferral allowed with ≤60-day deadline.

| Test ID         | Requirement                                                                                  | Test Level | Risk Link | Owner                  | Notes                                                                                  |
|-----------------|----------------------------------------------------------------------------------------------|------------|-----------|------------------------|----------------------------------------------------------------------------------------|
| T-R009-UNIT-001 | Audit-after-conflict ordered by `Timestamp` then `EventId`; cursor stable                     | Unit       | R-009     | Story 10.4 dev         | Same-timestamp entries ordered by EventId; cursor pagination stable across 2 queries   |
| T-R010-UNIT-001 | `TenantsProjectionActor` pre-cancelled token → no state access                                 | Unit       | R-010     | Story 10.3B dev        | NSubstitute spy: state store never invoked                                              |
| T-R010-UNIT-002 | `TenantsProjectionActor` mid-flow cancellation during DAPR state read → clean abort           | Unit       | R-010     | Story 10.3B dev        | `OperationCanceledException` rethrown; no partial successful result                     |
| T-R010-UNIT-003 | Cancellation taxonomy — not converted to Forbidden/NotFound/InvalidCursor/ActorFailure/ETagConflict/RetryExhaustion | Unit | R-010 | Story 10.3B dev | Confirms cancellation surfaces as `OperationCanceledException` only                     |
| T-R011-E2E-001  | `AspireTopologyTests.*_starts_and_is_alive` — regression guard for liveness probes (post-10.3a) | Tier 3   | R-011     | Integration test owner | Pass when Aspire harness boots; skip-by-precondition when Docker absent                |
| T-R012-CI-001   | CI verifies EventStore submodule pointer matches expected cancellation API commit             | CI         | R-012     | CI / Platform          | Fails build on drift; emits diff in log                                                |
| T-R013-UNIT-001 | 3-attempt budget under contention eventually succeeds or fails observably                     | Unit       | R-013     | Story 10.4 dev         | Scripted conflicts at attempts 1, 2; success at 3 OR all-3-conflict → observable fail  |
| T-R015-UNIT-001 | Malformed audit JSON payload skipped, no save (preserve existing 10.2 behavior)                | Unit       | R-015     | Story 10.4 dev         | Regression guard; structured log of skip; no save side effect                          |
| T-R015-UNIT-002 | Metadata invariant failure propagates, not silently swallowed (preserve existing 10.2 behavior)| Unit       | R-015     | Story 10.4 dev         | Exception propagates; no save; audit state unchanged                                    |
| T-R016-UNIT-001 | Retry-exhaustion emits structured failure context (safe categories only)                       | Unit       | R-016     | Story 10.4 dev         | Positive log-shape: stage, attempt count, projection key category, correlation/trace id |

**Total P2:** 10 tests + 1 CI guard · **~20–35 hours**

### P3 (Low)

**Criteria:** Score 1–3 risks (DOCUMENT). No tests required; documented as scope boundary in code comments or fixture docs.

| Risk Link | Disposition                                                                                          |
|-----------|------------------------------------------------------------------------------------------------------|
| R-017     | Scope-boundary comment in conformance fixture forbidding cross-key atomic rollback claims            |
| R-018     | Already covered by NFR5 Tier 3 tests + per-key fixture scripting; no new test                        |
| R-019     | Covered transitively by 9.3/9.4 forbidden-precedence tests + T-R010-UNIT-001 (pre-cancel-after-auth) |
| R-020     | Defer to Performance NFR pass (Epic 7 follow-up or dedicated NFR epic)                                |
| R-021     | Design property of Story 10.3B; not testable as a defect                                              |
| R-022     | Architecture acceptance; queries already handle eventual consistency                                  |
| R-023     | Story 10.4 acceptance criteria already constrains to narrowest `InternalsVisibleTo` change            |

**Total P3:** 0 tests · ~3–6 hours (documentation work)

---

## Execution Strategy

**Three-lane model. Default: run everything in PRs unless expensive / requires external infrastructure.**

### PR lane (every push/PR to main, target ≤ 12 min)

- All Tier 1 unit tests for Tenants and EventStore
- Includes every P0 and P1 test from this matrix
- Filter excludes `AspireTopology`, `Quarantined`, `NightlyProperty`, `Performance`
- Command: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Quarantined&Category!=NightlyProperty&Category!=Performance&FullyQualifiedName!~AspireTopologyTests"`

### Nightly lane (full regression)

- Tier 1 + Tier 2 (DAPR/Docker required) + Tier 3 (Aspire topology liveness)
- Includes integration variants `T-R001-INT-001` and `T-R006-INT-001`
- Includes `AspireTopologyTests` (T-R011-E2E-001) for fixture-regression coverage
- Command: `dotnet test Hexalith.Tenants.slnx --configuration Release` (unfiltered)

### Weekly lane (NFR follow-up)

- Performance smoke for retry storm (R-014) and large-audit replay (R-020)
- **Out of scope for Epic 10 ship**; NFR pass owns these

### Philosophy

Aim for fast PR feedback through Tier 1 unit dominance. Tier 2 integration tests exist only to prove the fakes track real DAPR ETag behavior; Tier 3 is reserved for cross-process Aspire integration. If a behavior can be tested at Tier 1 deterministically, it must be — sleeps and live sidecars are forbidden in focused tests per Stories 10.1–10.4 specs.

---

## Resource Estimates

| Bucket                                          | Test count                              | Estimate              |
|-------------------------------------------------|-----------------------------------------|-----------------------|
| **P0** — D2 BLOCKER (singleton index)            | 3 tests + fixture extension             | **~12–20 hours**      |
| **P1** — 7 high risks                            | 12 tests + diagnostic infra + fixture rule | **~30–50 hours**   |
| **P2** — 8 medium risks                          | 10 tests + 1 CI guard                    | **~20–35 hours**      |
| **P3** — 7 low risks                             | Documentation only                       | **~3–6 hours**        |
| **TOTAL**                                        | **25 tests + 1 fixture rule + 1 CI guard** | **~65–110 hours** |

**Calendar timeline:** ~2–3 weeks (1 developer) or ~1–1.5 weeks (paired). Spans Story 10.3B (cancellation tests) + Story 10.4 (conformance tests) combined effort.

### Prerequisites

**Test data / fixtures:**
- `ProjectionWriteConformanceFixture` (test-only, deterministic scripted state-store) — to be created in 10.4
- Existing `TenantProjectionHandlerTests` and `TenantAuditProjectionTests` extended
- Per-key, per-attempt scripted outcomes (no global queue) — fail-fast on unexpected key/order

**Tooling:**
- xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`, `Microsoft.NET.Test.Sdk 18.5.1`
- coverlet.collector for line coverage
- `dotnet test` filter expressions for lane selection
- Local `dapr init` + Docker for Tier 2 sanity tests

**Environment:**
- Local: .NET SDK `10.0.300`, EventStore submodule pinned at commit `bcccd504`
- CI (GitHub Actions): existing Tier 1 + Tier 2 workflows extended with `T-R012-CI-001` submodule pointer check

---

## Quality Gate Criteria

### Pass/Fail Thresholds

| Gate                                                                       | Threshold                       | Enforcement                              |
|----------------------------------------------------------------------------|---------------------------------|------------------------------------------|
| **P0 BLOCKER (R-001) tests covered and passing**                            | 100%                            | Epic 10 ships only when 3 R-001 tests green |
| **P1 tests covered and passing**                                            | 100% (or architecture-lead waiver) | All 12 P1 tests + fixture rule          |
| **P2 tests covered or explicit deferral with ≤60d deadline**                | 100% covered OR deferred         | Tracked in `_bmad-output/implementation-artifacts/deferred-work.md` |
| **Diagnostic-leak negative assertions (R-007)**                              | Zero tolerance                  | Any failure → block release immediately  |
| **Line coverage** on `TenantProjectionWritePolicy.cs`, `TenantProjectionHandler.cs`, `TenantAuditProjection.cs` | ≥ 90% | coverlet gate in PR lane         |
| **`AspireTopologyTests` (R-011) pass rate**                                  | ≥ 95% across last 10 CI runs    | Manual review; below threshold → fixture re-hardening |
| **EventStore submodule pointer drift (R-012)**                               | 0 — must match expected commit  | CI guard `T-R012-CI-001`                 |
| **Conformance fixture drives production helper (R-008)**                     | 100% (mechanical assertion)      | `T-R008-FIXTURE-001` rule                |
| **PR lane wall-clock**                                                       | ≤ 12 min                        | Monitor; if exceeded, partition tests    |

### Coverage Targets

- **Projection write paths** (production source): ≥ 90% line coverage (coverlet)
- **Critical risk scenarios** (R-001 through R-008): 100% covered by tests in this design
- **Security (negative log-content)** assertions: 100% pass (zero-tolerance)
- **Eventual consistency / replay semantics**: validated through deterministic state-store scripting

### Non-Negotiable Requirements

- [ ] R-001 (singleton index LWW) covered by passing P0 trio
- [ ] R-007 (PII leak) zero failures — any log-content assertion failure blocks release
- [ ] R-008 (fixture diverges from prod) mechanical assertion in place and passing
- [ ] EventStore submodule pinned (`T-R012-CI-001`) and CI guard active

---

## Mitigation Plans

### R-001: Silent LWW on tenant index singleton (Score 9)

**Mitigation Strategy:**
1. Story 10.1 has implemented optimistic-concurrency / ETag write path through `TenantProjectionWritePolicy.cs` for the singleton index key.
2. Story 10.4 must add the conformance trio:
   - `T-R001-UNIT-001`: scripted conflict-then-success preserves all previously indexed tenants
   - `T-R001-UNIT-002`: scripted retry-exhaustion (all 3 attempts conflict) emits structured failure, returns no-success
   - `T-R001-INT-001`: real DAPR-Redis-backed conflict-then-success sanity check (skip-by-precondition)
3. CI PR lane must run the two unit variants on every push; nightly lane runs the integration variant.

**Owner:** Story 10.4 developer
**Timeline:** Before Epic 10 → done
**Status:** Planned
**Verification:** All 3 R-001 tests green in CI; coverlet shows the singleton-index code path exercised at ≥ 90%.

### R-002: Silent LWW on tenant detail (Score 6)

**Mitigation Strategy:**
1. Same ETag / retry policy as R-001 (Story 10.1 helper).
2. Conformance pair `T-R002-UNIT-001/002` against `projection:tenants:{tenantId}` key.
3. Mixed event batch (`TenantCreated`, `TenantUpdated`, `UserAddedToTenant`, `UserRoleChanged`, `TenantConfigurationSet`) ensures incoming events apply exactly once.

**Owner:** Story 10.4 dev · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** Tests green; final read model contains all distinct events.

### R-003: Audit entry loss under concurrent access changes (Score 6)

**Mitigation Strategy:**
1. Story 10.2's idempotent merge implementation by `EventId`.
2. Conformance pair `T-R003-UNIT-001/002` for `audit:{tenantId}` covering original-persisted + externally-added + incoming entries merging without loss.
3. Ordering by `Timestamp` then `EventId` asserted in `T-R009-UNIT-001`.

**Owner:** Story 10.4 dev · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** Test count assertions on final entry list; cursor pagination stable across 2 consecutive queries on conflicted state.

### R-004: Duplicate EventId admits non-idempotent merge (Score 6)

**Mitigation Strategy:**
1. Story 10.2 enforces "persisted entry authoritative on duplicate `EventId`" semantics.
2. `T-R004-UNIT-001`: duplicate `EventId` + matching payload → 1 entry persisted.
3. `T-R004-UNIT-002`: duplicate `EventId` + mismatched payload → persisted wins, incoming payload NOT in diagnostic logs (combined with R-007).

**Owner:** Story 10.4 dev · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** Both tests green; payload-leak negative assertion in `T-R004-UNIT-002` confirms safe diagnostic shape.

### R-005: Stale model reuse across retry attempts (Score 6)

**Mitigation Strategy:**
1. `TenantProjectionWritePolicy` reloads fresh state on each retry.
2. `T-R005-UNIT-001` spies state-store read calls and asserts:
   - N+1 reads for N+1 attempts (each attempt reloads)
   - Identity check: each retry attempt receives a distinct model instance
3. Combined coverage with `T-R002-UNIT-001` (which already needs the no-stale-reuse property to pass).

**Owner:** Story 10.4 dev · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** State-store read count assertion + model-instance identity assertion green.

### R-006: ETag cache poisoning by stale snapshot (Score 6)

**Mitigation Strategy:**
1. Story 10.3A hardened `CachingProjectionActor` cache-hit precedence (over cancellation).
2. `T-R006-UNIT-001`: after conflict-then-success retry, a subsequent read returns persisted state (not the stale cached snapshot).
3. `T-R006-INT-001`: real DAPR-backed integration sanity check.

**Owner:** Story 10.4 dev · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** Cache freshness assertion green at unit level and integration level.

### R-007: Diagnostic log leakage (Score 6) — CRITICAL SECURITY GATE

**Mitigation Strategy:**
1. Three negative-assertion tests:
   - `T-R007-UNIT-001`: retry-exhaustion diagnostic does NOT contain tenant payload / user labels / cursor / audit content
   - `T-R007-UNIT-002`: cancellation-failure diagnostic does NOT contain those values either (tied to Story 10.3B)
   - `T-R007-UNIT-003`: duplicate-EventId-mismatch diagnostic does NOT contain incoming payload
2. Assertions structured: walk all captured `Log*` parameter values and string-search for known-forbidden tokens (tenant name, user GUID, cursor string, configuration value bytes).
3. Zero-tolerance gate: any failure blocks release immediately.

**Owner:** Story 10.4 dev (+ 10.3B for UNIT-002) · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** All 3 negative-content assertions green; review by security lead before Epic 10 close.

### R-008: Conformance fixture diverges from production helper (Score 6) — ANTI-PATTERN GUARD

**Mitigation Strategy:**
1. `ProjectionWriteConformanceFixture` must drive `TenantProjectionWritePolicy` and `TenantProjectionHandler` directly. It must NOT re-implement retry/merge logic in test code.
2. `T-R008-FIXTURE-001` mechanical assertion: at fixture construction, spy or type-assert that the policy under test is the production type. A test-internal merge implementation fails this check.
3. Code review checkpoint in Story 10.4 PR template: reviewer must confirm "fixture drives production helper, no algorithmic re-implementation in test code."

**Owner:** Story 10.4 dev + Code reviewer · **Timeline:** Before Epic 10 ships · **Status:** Planned
**Verification:** Fixture type assertion green; PR review checklist item ticked.

---

## Assumptions and Dependencies

### Assumptions

1. Stories 10.1 (`TenantProjectionWritePolicy`) and 10.2 (audit idempotent merge) helper APIs are **stable and merged** before Story 10.4 implementation begins.
2. EventStore submodule remains pinned at commit `bcccd504` (cancellation API surface) until Story 10.3B completes and approves a pointer advance.
3. DAPR Client `1.17.9` + Aspire `13.3.3` + `Microsoft.NET.Test.Sdk 18.5.1` versions remain unchanged through Epic 10 ship — no dependency upgrades in this epic.
4. xUnit v3 conventions and Shouldly assertions (no raw `Assert.*`) carry through all new tests per Hexalith conventions.
5. Local `dapr init` + Docker are available on developer machines for Tier 2 sanity tests; CI nightly has them too.
6. AspireTopologyFixture post-10.3a hardening holds steady (no Aspire version bump-driven regression).

### Dependencies

| Dependency                                                       | Required By              | Status  |
|------------------------------------------------------------------|--------------------------|---------|
| Story 10.1 + 10.2 helper APIs documented and stable               | Story 10.4 implementation start | done    |
| Story 10.3A EventStore commit `bcccd504` available to Tenants     | Story 10.3B + R-006 cache coherence | review (pointer advance pending) |
| `dapr init` and Docker available locally and in CI nightly lane  | Tier 2 tests (R-001-INT-001, R-006-INT-001) | local: yes, CI: yes |
| `_bmad-output/implementation-artifacts/deferred-work.md` updated  | P2 deferral tracking      | active  |

### Risks to Plan

| Risk to Plan                                                     | Impact                                  | Contingency                                                                  |
|------------------------------------------------------------------|-----------------------------------------|------------------------------------------------------------------------------|
| 10.1/10.2 helper API churn after Story 10.4 starts                | Re-work conformance tests; effort overflows the 50-hour P1 upper bound | Pause 10.4 implementation; re-record Dev Agent Record evidence (per 10.4 AC #9); re-baseline tests |
| EventStore submodule pointer advance changes cancellation API shape | R-006 + R-010 tests break             | Update CI guard `T-R012-CI-001` expected commit; re-run R-006 / R-010 tests |
| Aspire 13.3.3 → 13.4.x upgrade landing mid-epic                    | R-011 fixture flakiness regression      | Quarantine `AspireTopologyTests` temporarily; track via existing 10.3a fixture-hardening patterns |
| Tier 2 DAPR sanity tests flaky in CI (skip-by-precondition rate >20%) | Lose integration-level R-001/R-006 confidence | Promote one Tier 2 test to a hard-required gate; pin Docker/Redis versions |

---

## Interworking & Regression

| Service / Component                                          | Impact                                                                          | Regression Scope                                                                            |
|--------------------------------------------------------------|---------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| **`Hexalith.EventStore` (submodule, commit `bcccd504`)**     | Tenants depends on cancellation API surface for R-006 (cache coherence) + R-010 (cancellation transport) | Existing EventStore Server.Tests (82/82) + Client.Tests EventStoreProjectionTests (22/22) + Testing.Tests FakeProjectionActorTests (13/13) must remain green when this Tenants test design lands. |
| **`Hexalith.Tenants.Sample` (sample/demo service)**          | `AspireTopologyFixture` includes the Sample service `/alive` endpoint (post-10.3a) | `AspireTopologyTests.Sample_resource_starts_and_is_alive` must remain green                |
| **`Hexalith.Tenants.IntegrationTests` (Tier 3 Aspire)**      | R-011 regression guard; existing tests use renamed `_is_alive` names              | All `*_starts_and_is_alive` tests; fixture hardening from 10.3a (P5–P10) preserved          |
| **`Hexalith.Tenants.Server.Tests` (Tier 1 + 2)**             | New conformance tests land here; extends `TenantProjectionHandlerTests` etc.    | Existing 611 passing tests (post-10.3a baseline) must remain green                          |
| **`Hexalith.Tenants.Client.Tests`, `Contracts.Tests`, `Sample.Tests`** | No direct impact; smoke regression only                                  | Tier 1 PR-lane filter includes these projects                                               |
| **CI workflow (`.github/workflows/*.yml`)**                  | New CI guard `T-R012-CI-001` for EventStore submodule pointer drift             | Existing PR + nightly + release pipelines preserved; new step added to PR lane              |

---

## Appendix A: Test ID → Risk Cross-Reference

| Test ID                | Working ID (Step 3) | Risk ID | Priority |
|------------------------|---------------------|---------|----------|
| T-R001-UNIT-001        | T-D2-UNIT-001       | R-001   | P0       |
| T-R001-UNIT-002        | T-D2-UNIT-002       | R-001   | P0       |
| T-R001-INT-001         | T-D2-INT-001        | R-001   | P0       |
| T-R002-UNIT-001        | T-D1-UNIT-001       | R-002   | P1       |
| T-R002-UNIT-002        | T-D1-UNIT-002       | R-002   | P1       |
| T-R003-UNIT-001        | T-D3-UNIT-001       | R-003   | P1       |
| T-R003-UNIT-002        | T-D3-UNIT-002       | R-003   | P1       |
| T-R004-UNIT-001        | T-D4-UNIT-001       | R-004   | P1       |
| T-R004-UNIT-002        | T-D4-UNIT-002       | R-004   | P1       |
| T-R005-UNIT-001        | T-D5-UNIT-001       | R-005   | P1       |
| T-R006-UNIT-001        | T-T2-UNIT-001       | R-006   | P1       |
| T-R006-INT-001         | T-T2-INT-001        | R-006   | P1       |
| T-R007-UNIT-001        | T-S1-UNIT-001       | R-007   | P1       |
| T-R007-UNIT-002        | T-S1-UNIT-002       | R-007   | P1       |
| T-R007-UNIT-003        | T-S1-UNIT-003       | R-007   | P1       |
| T-R008-FIXTURE-001     | T-O2-FIXTURE-001    | R-008   | P1       |
| T-R009-UNIT-001        | T-D6-UNIT-001       | R-009   | P2       |
| T-R010-UNIT-001        | T-T1-UNIT-001       | R-010   | P2       |
| T-R010-UNIT-002        | T-T1-UNIT-002       | R-010   | P2       |
| T-R010-UNIT-003        | T-T1-UNIT-003       | R-010   | P2       |
| T-R011-E2E-001         | T-T3-E2E-001        | R-011   | P2       |
| T-R012-CI-001          | T-T4-CI-001         | R-012   | P2       |
| T-R013-UNIT-001        | T-T5-UNIT-001       | R-013   | P2       |
| T-R015-UNIT-001        | T-B1-UNIT-001       | R-015   | P2       |
| T-R015-UNIT-002        | T-B1-UNIT-002       | R-015   | P2       |
| T-R016-UNIT-001        | T-O1-UNIT-001       | R-016   | P2       |

## Appendix B: Knowledge Base References

- `risk-governance.md` — Risk scoring matrix, gate decision engine, traceability validation
- `probability-impact.md` — Probability × Impact scoring, threshold-based action classification (DOCUMENT / MONITOR / MITIGATE / BLOCK)
- `test-levels-framework.md` — Unit / Integration / E2E selection; favor lower levels; duplicate-coverage guard
- `test-priorities-matrix.md` — P0 / P1 / P2 / P3 with risk-score → priority mapping

## Appendix C: Related Documents

- **PRD:** `_bmad-output/planning-artifacts/prd.md`
- **Architecture:** `_bmad-output/planning-artifacts/architecture.md`
- **Epic doc (Epic 10 section, lines 1390–1532):** `_bmad-output/planning-artifacts/epics.md`
- **Story 10.1 (done):** `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`
- **Story 10.2 (done):** `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`
- **Story 10.3A (review):** `_bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md`
- **Story 10.3B (ready-for-dev):** `_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md`
- **Story 10.4 (ready-for-dev):** `_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md`
- **Sprint status:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
- **Deferred work tracking:** `_bmad-output/implementation-artifacts/deferred-work.md`

---

## Follow-on Workflows (manual)

- **Story 10.3B implementation** (`/bmad-dev-story` on `10-3b-cancellation-token-threading-for-tenant-projection-queries`) — picks up tests T-R007-UNIT-002, T-R010-UNIT-001/002/003.
- **Story 10.4 implementation** (`/bmad-dev-story` on `10-4-projection-write-conformance-and-recovery-tests`) — implements the bulk of P0 + P1 + P2 tests above.
- **ATDD scaffolding** for P0 trio: `/bmad-testarch-atdd` can generate the failing R-001 tests as a red-phase ATDD starting point if desired before implementing 10.4.
- **Trace coverage** after Stories 10.3B + 10.4 ship: `/bmad-testarch-trace` to confirm requirements ↔ tests mapping and produce the Epic 10 quality gate decision.

---

**Generated by:** Murat — Master Test Architect (BMad TEA module, workflow `bmad-testarch-test-design`)
**Workflow version:** BMad v6 / TEA 6.7.1
