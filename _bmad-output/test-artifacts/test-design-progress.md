---
workflowStatus: 'completed'
totalSteps: 5
stepsCompleted: ['step-01-detect-mode', 'step-02-load-context', 'step-03-risk-and-testability', 'step-04-coverage-plan', 'step-05-generate-output']
lastStep: 'step-05-generate-output'
nextStep: ''
lastSaved: '2026-05-19'
mode: 'epic-level'
target_scope: 'Epic 10 — Durable Projection Write Safety'
target_epic_id: 'epic-10'
user: 'Jerome'
project: 'Hexalith.Tenants'
detected_stack: 'backend'
inputDocuments:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md
  - _bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md
  - _bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
  - .claude/skills/bmad-tea/resources/knowledge/risk-governance.md
  - .claude/skills/bmad-tea/resources/knowledge/probability-impact.md
  - .claude/skills/bmad-tea/resources/knowledge/test-levels-framework.md
  - .claude/skills/bmad-tea/resources/knowledge/test-priorities-matrix.md
---

# Test Design Progress — Epic 10: Durable Projection Write Safety

## Step 1 — Mode & Prerequisites (Completed)

### Mode Decision

- **Mode:** Epic-Level
- **Rationale:**
  - User did not state explicit scope when invoking `/bmad-tea TD` — Priority A unclear.
  - `_bmad-output/implementation-artifacts/sprint-status.yaml` exists → Priority B file-based detection → **Epic-Level Mode**.
  - User confirmed Epic 10 (Projection Write Safety) as the target scope.

### Prerequisites Verified

- ✅ Epic 10 epic doc + 5 story files present
- ✅ Architecture, PRD, sprint-status all loaded
- ✅ Sprint status: Epic 10 in-progress; 2 stories done (10-1, 10-2), 1 review (10-3a), 2 ready-for-dev (10-3b, 10-4)

## Step 2 — Context & Knowledge Base (Completed)

### Config Resolved (`_bmad/tea/config.yaml`)

- `test_artifacts`: `_bmad-output/test-artifacts/`
- `tea_use_playwright_utils`: true (BUT detected stack is .NET backend → skip TS profile)
- `tea_use_pactjs_utils`: false
- `tea_pact_mcp`: none
- `tea_browser_automation`: auto (skipped — no UI scope)
- `risk_threshold`: p1

### Stack Detection: `backend`

- `.csproj` everywhere; no significant frontend in Tenants proper.
- Playwright Utils profile **skipped** — those are TypeScript Playwright helpers, not applicable to .NET xUnit v3 test design.
- Pact.js Utils skipped (disabled).
- Contract testing fragment NOT loaded — Epic 10 is about projection write safety, not service contracts.

### Epic 10 Surface Loaded

**Scope:** Tenant read-model projection persistence behavior under concurrency, retry, partial failure, and cancellation conditions.

**FRs covered:** FR25, FR26, FR27, FR28, FR29, FR30, FR53.
**NFRs reinforced:**
- **NFR5** — Zero cross-tenant data leaks (verified by Tier 3 integration tests)
- **NFR17** — Graceful degradation when DAPR pub/sub is unavailable
- **NFR20** — Event store is single source of truth; full state reconstruction via replay
- **NFR23** — No data loss under any failure scenario

**Production state keys (3):**
- `projection:tenants:{tenantId}` — tenant detail read model
- `projection:tenant-index:singleton` — shared cross-tenant index
- `audit:{tenantId}` — audit trail per tenant

**Stories:**
| Story | Status | Scope |
|-------|--------|-------|
| 10.1 | done | Optimistic concurrency / ETag for tenant detail + index — `TenantProjectionWritePolicy.cs` |
| 10.2 | done | Audit projection write safety — idempotent merge by EventId, Timestamp+EventId ordering |
| 10.3A | review | EventStore cancellation API prerequisite (submodule commit `bcccd504`) |
| 10.3B | ready-for-dev | Cancellation token threading through Tenants projection queries |
| 10.4 | ready-for-dev | Conformance + recovery test suite (this is largely *the* TD output target) |

### Test Stack Loaded

- **Frameworks:** xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`, `Microsoft.NET.Test.Sdk 18.5.1`
- **Tier 1 (Unit, no external deps):** `tests/Hexalith.Tenants.Server.Tests/` and sibling Contracts/Client/Sample/Testing test projects
- **Tier 2 (Integration with DAPR + Docker):** mainly `Hexalith.Tenants.Server.Tests` integration cases that boot DAPR sidecar
- **Tier 3 (Aspire E2E):** `tests/Hexalith.Tenants.IntegrationTests/` — `AspireTopologyTests` with `/alive` liveness endpoint (post-10-3a hardening)
- **Deterministic-first:** No live DAPR sidecars, no Redis, no Aspire, no sleeps in focused tests; pre-cancelled tokens + controllable fakes.

### Existing Test Surface (Projection-Related)

- `Projections/TenantProjectionHandlerTests.cs` ← 10.1 + 10.2 ETag work landed here
- `Projections/TenantAuditProjectionTests.cs`, `TenantAuditReadModelTests.cs`
- `Projections/TenantIndexProjectionTests.cs`, `TenantIndexReadModelTests.cs`
- `Projections/TenantProjectionTests.cs`, `TenantReadModelTests.cs`
- `Projections/TenantsProjectionActorTests.cs` ← 10.3B will touch here
- `Projections/ProjectionDispatcherTests.cs`
- `Projections/GlobalAdministrator*Tests.cs`

### Production Surface Under Test

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` (introduced by Story 10.1)
- `src/Hexalith.Tenants/Projections/DaprTenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`, `TenantIndexReadModel.cs`, `TenantAuditReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs`, `TenantIndexProjection.cs`, `TenantAuditProjection.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs` (cancellation entry point for 10.3B)

### Knowledge Fragments Loaded (Core Tier — Epic-Level Required)

- `risk-governance.md` — Risk scoring matrix (probability × impact, 1–9), gate decision engine, traceability validation, mitigation tracking
- `probability-impact.md` — Probability/Impact scales, action thresholds (DOCUMENT/MONITOR/MITIGATE/BLOCK)
- `test-levels-framework.md` — Unit vs Integration vs E2E decision rules; favor lower levels
- `test-priorities-matrix.md` — P0/P1/P2/P3 with risk-score → priority mapping (score 9 → P0; 6–8 → P0/P1; etc.)

### Knowledge Fragments NOT Loaded (Skipped by Design)

- Playwright Utils profile (UI/TypeScript-specific; this is .NET backend)
- Pact.js Utils / contract-testing (disabled)
- Pact MCP (none)
- Visual / browser exploration (no UI scope)

### Open Questions (Carry into Step 3)

1. Story 10-3a is in review status — should the test design treat 10-3a's EventStore-side cancellation tests as *complete coverage* or assume Tenants-side gaps remain? (Working assumption: EventStore tests are out of scope; Tenants-side cancellation tests will land in 10-3b.)
2. Story 10-4 is *literally about* conformance + recovery tests — the TD output should directly feed 10-4's implementation. (Working assumption: TD scope = Epic 10 holistic + 10-4 implementation guidance.)
3. Tier 3 Aspire E2E coverage on projection write safety — currently only the `_is_alive` liveness probe. Should we recommend a real projection-write E2E? (Defer to Step 4 — likely YES but with risk-cost tradeoff.)

### Next Step → Done

→ `step-03-risk-and-testability.md` — Risk identification (TECH/SEC/PERF/DATA/BUS/OPS) with probability × impact scoring against the surface above.

## Step 3 — Risk Assessment (Completed)

### Mode-Specific Note

Epic-Level mode → system-level testability review skipped. Risk assessment is the focus.

### Scoring Conventions (per `probability-impact.md`)

- **Probability:** 1 = Unlikely / 2 = Possible / 3 = Likely
- **Impact:** 1 = Minor / 2 = Degraded / 3 = Critical
- **Score** = P × I (range 1–9)
- **Action:** 1–3 DOCUMENT · 4–5 MONITOR · 6–8 MITIGATE · 9 BLOCK

### Risk Matrix (sorted by score, descending)

| ID  | Cat  | Title                                                                                                     | P | I | Score | Action   |
|-----|------|-----------------------------------------------------------------------------------------------------------|---|---|-------|----------|
| D2  | DATA | Silent last-writer-wins on `projection:tenant-index:singleton` (highest contention key)                   | 3 | 3 | **9** | BLOCK    |
| D1  | DATA | Silent last-writer-wins on `projection:tenants:{tenantId}` (concurrent membership/config events)          | 2 | 3 | **6** | MITIGATE |
| D3  | DATA | Audit entry loss under concurrent access changes on `audit:{tenantId}`                                    | 2 | 3 | **6** | MITIGATE |
| D4  | DATA | Duplicate `EventId` admits non-idempotent merge (DAPR at-least-once delivery — FR42)                      | 3 | 2 | **6** | MITIGATE |
| D5  | DATA | Stale `TenantReadModel`/`TenantIndexReadModel` instance reused across retry attempts                      | 2 | 3 | **6** | MITIGATE |
| T2  | TECH | ETag cache poisoning — `CachingProjectionActor` returns stale snapshot whose ETag mismatches storage      | 2 | 3 | **6** | MITIGATE |
| S1  | SEC  | Diagnostic logs leak tenant payload / user labels / cursor / audit content on retry-exhaustion failure    | 2 | 3 | **6** | MITIGATE |
| O2  | OPS  | Conformance fixture re-implements retry/merge algorithm → green tests, broken prod (false confidence)     | 2 | 3 | **6** | MITIGATE |
| D6  | DATA | Non-deterministic audit ordering after conflict-reload breaks cursor pagination (`Timestamp` then `EventId`) | 2 | 2 | 4   | MONITOR  |
| T1  | TECH | DAPR actor proxy cancellation transport ambiguity (boundary chosen wrong → cancellation silently dropped) | 2 | 2 | 4     | MONITOR  |
| T3  | TECH | Aspire topology fixture flakiness in Tier 3 (Aspire 13.3.3 sensitivity + Redis/HTTPS probe drift)         | 2 | 2 | 4     | MONITOR  |
| T4  | TECH | EventStore submodule version drift away from pinned `bcccd504` cancellation API surface                    | 2 | 2 | 4     | MONITOR  |
| T5  | TECH | `TenantProjectionWritePolicy` 3-attempt budget under heavy singleton-index contention                     | 2 | 2 | 4     | MONITOR  |
| P1  | PERF | Retry storm: many tenants writing concurrently → repeated ETag conflicts → CPU / state-store thrash      | 2 | 2 | 4     | MONITOR  |
| B1  | BUS  | Audit invariant boundary bypass — refactor weakens the malformed-JSON-skip / invariant-failure-propagate split | 2 | 2 | 4 | MONITOR  |
| O1  | OPS  | Retry-exhaustion observability gap — no structured ops signal for projection write loss                   | 2 | 2 | 4     | MONITOR  |
| D7  | DATA | Cross-key transactional claim creep (forbidden by stories, easy regression in future change)              | 1 | 3 | 3     | DOCUMENT |
| S2  | SEC  | Cross-tenant data leak via projection recovery path (NFR5 violation)                                      | 1 | 3 | 3     | DOCUMENT |
| S3  | SEC  | Cancellation short-circuits before auth → masks authorization regression                                  | 1 | 2 | 2     | DOCUMENT |
| P2  | PERF | Audit merge O(n) on large audit histories (10K+ entries per tenant)                                       | 2 | 1 | 2     | DOCUMENT |
| P3  | PERF | Cancellation checkpoint placement late → minimal compute saved                                            | 2 | 1 | 2     | DOCUMENT |
| B2  | BUS  | Event-order divergence between detail/audit/index views — eventually-consistent partial state            | 2 | 1 | 2     | DOCUMENT |
| O3  | OPS  | `InternalsVisibleTo` creep widens production API surface for test convenience                              | 1 | 1 | 1     | DOCUMENT |

### Risk Detail: CRITICAL (score 9)

**D2 — Silent last-writer-wins on tenant index singleton** · BLOCK · `Owner: Epic 10 author / Story 10-1 + 10-4`
- **Why P=3:** `projection:tenant-index:singleton` is the single point of contention across the entire system. Any two tenant lifecycle, membership, or removal events that arrive close in time race on this state key. Probability is structurally Likely.
- **Why I=3:** Silent loss of a previously indexed tenant corrupts FR25 (list tenants), FR27 (tenant users), FR28 (user tenants) — three query endpoints across the public API. NFR23 violation (data loss).
- **Mitigation:** Story 10.1 already implemented optimistic concurrency. Story 10-4 must prove via conformance tests that conflict-then-success preserves previously indexed tenants AND retry-exhaustion fails observably without claiming success.
- **Gate test:** `TenantIndex_ConflictThenSuccess_PreservesPreviouslyIndexedTenants` — must exist and pass before Epic 10 ships.
- **Deadline:** Before Story 10-4 review → done.

### Risk Detail: HIGH (score 6–8)

**D1 — Silent LWW on tenant detail** · MITIGATE · `Owner: Story 10-1 + 10-4`
- Probability is Possible (2) — happens under concurrent member/config events targeting the same tenant. Impact is Critical (3) — silent data loss on FR26.
- Mitigation: 10.1's ETag policy + 10-4 conformance test asserting all incoming lifecycle/membership/config events present exactly once after conflict-then-success.

**D3 — Audit entry loss** · MITIGATE · `Owner: Story 10-2 + 10-4`
- P=2 (concurrent role changes in admin flows are realistic). I=3 (audit incompleteness breaks FR29 + audit/compliance assumptions).
- Mitigation: 10.2's idempotent merge + 10-4 audit conformance test (`Timestamp` then `EventId` ordering preserved across conflict reload).

**D4 — Duplicate EventId admits non-idempotent merge** · MITIGATE · `Owner: Story 10-2 + 10-4`
- P=3 (DAPR pub/sub is at-least-once by design; FR42 calls this out explicitly). I=2 (audit shows duplicates, query inflated).
- Mitigation: 10.2 specifies idempotent merge by `EventId`; persisted entry authoritative. 10-4 must include duplicate-EventId-with-mismatched-payload test.

**D5 — Stale read model instance reused on retry** · MITIGATE · `Owner: Story 10-1 + 10-4`
- P=2 (easy refactor bug). I=3 (compounding silent corruption).
- Mitigation: 10-4 acceptance criteria already calls this out. Conformance fixture must assert "no stale model reuse across attempts" via state-store reload assertions.

**T2 — ETag cache poisoning** · MITIGATE · `Owner: Story 10-1 + 10-3a hardening`
- P=2 (cache invalidation is classic). I=3 (false success; read divergence from persisted state).
- Mitigation: 10-3a hardened cache-hit precedence over cancellation. 10-4 conformance test must include cache-coherence-after-retry assertion (read after conflict-success returns persisted state, not cached stale snapshot).

**S1 — Diagnostic log leakage** · MITIGATE · `Owner: All Epic 10 stories + 10-4 + Story 9-4 logging policy`
- P=2 (failure paths under-reviewed). I=3 (privacy violation, cross-customer log infrastructure).
- Mitigation: 10-4 must include **negative log-content assertions** — diagnostic captured during retry exhaustion / cancellation must NOT contain tenant display names, user labels, cursor payloads, audit payloads, configuration values, or serialized event bodies. Property-level structured-field assertions over message-text matching.

**O2 — Conformance fixture diverges from production helper** · MITIGATE · `Owner: Story 10-4`
- P=2 (textbook anti-pattern; surfaces under time pressure). I=3 (provides false confidence; defeats the conformance suite's purpose).
- Mitigation: Test design must explicitly require the conformance fixture to drive *real* production code (`TenantProjectionWritePolicy`, `TenantProjectionHandler`) via scripted state-store outcomes, NOT re-implement merge/retry logic. Fixture-design rule + code review checkpoint.

### Risk Summary

- **Total risks identified:** 23
- **CRITICAL (BLOCK, score 9):** 1 (D2)
- **HIGH (MITIGATE, score 6–8):** 7 (D1, D3, D4, D5, T2, S1, O2)
- **MEDIUM (MONITOR, score 4–5):** 8 (D6, T1, T3, T4, T5, P1, B1, O1)
- **LOW (DOCUMENT, score 1–3):** 7 (D7, S2, S3, P2, P3, B2, O3)
- **Risk concentration:** DATA category (7 of 23 risks; 4 of 8 high-or-above) — expected since Epic 10 is about write durability and projection correctness.
- **Gate impact:** Score=9 D2 is an automatic FAIL until Story 10-4's conformance tests cover and pass the singleton index conflict-then-success case. Score 6–8 set CONCERNS until each has a covering test.

### Next Step → Done

→ `step-04-coverage-plan.md` — Map each risk (especially CRITICAL/HIGH) to test levels (Unit/Integration/E2E) and priorities (P0/P1/P2/P3) per `test-levels-framework.md` and `test-priorities-matrix.md`.

## Step 4 — Coverage Plan & Execution Strategy (Completed)

### Test Level Strategy (`test-levels-framework.md`)

Per the test pyramid and Hexalith.Tenants' 3-tier convention:

- **Tier 1 Unit** — Primary level for projection write conformance. Pure model tests + scripted deterministic state-store fakes. xUnit v3 + Shouldly + NSubstitute. No DAPR, no Redis, no Aspire, no sleeps.
- **Tier 2 Integration (DAPR-backed)** — A small set of contract-validation tests that prove our fakes match real DAPR ETag behavior. Gated by `dapr init` precondition (R3-A4 SCP pattern); skip-by-precondition rather than fail.
- **Tier 3 Aspire E2E** — Keep minimal. Liveness probes for `command-api`, `tenants`, `sample` already exist post-10-3a. Do NOT expand Tier 3 scope to projection write semantics — wrong cost/benefit.

**Anti-pattern guard:** No projection-write-safety logic tested first at integration level when a Tier 1 unit equivalent is cheaper.

### Coverage Matrix

Test ID format: `T-{RISK}-{LEVEL}-{SEQ}` where LEVEL ∈ {UNIT, INT, E2E, FIXTURE, CI}.

#### P0 — BLOCKER coverage (Score 9, D2)

| Test ID         | Level   | Scenario                                                                                              | Risk(s) | Story | DoD |
|-----------------|---------|-------------------------------------------------------------------------------------------------------|---------|-------|-----|
| T-D2-UNIT-001   | Unit    | `TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenants` — singleton index race resolves without dropping previously indexed tenants | D2      | 10-4  | Asserts final saved `TenantIndexReadModel` contains pre-existing tenants + new tenant + role-change effects |
| T-D2-UNIT-002   | Unit    | `TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccess` — exhaustion path emits structured failure, returns no-success projection result | D2, O1  | 10-4  | Asserts `ProjectionResponse.Success == false` + structured diagnostic shape (no payload) |
| T-D2-INT-001    | Tier 2  | `TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndex` — same race against real DAPR Redis state store | D2      | 10-4  | Skip-by-precondition if DAPR/Redis unavailable; otherwise pass |

#### P1 — HIGH risk coverage (Scores 6–8: D1, D3, D4, D5, T2, S1, O2)

| Test ID         | Level   | Scenario                                                                                              | Risk(s) | Story | DoD |
|-----------------|---------|-------------------------------------------------------------------------------------------------------|---------|-------|-----|
| T-D1-UNIT-001   | Unit    | `TenantDetail_ConflictThenSuccess_AllLifecycleMembershipConfigEventsPresent` — conflict → reload → reapply preserves every incoming event exactly once | D1, D5  | 10-4  | Mixed `TenantCreated`/`UserAddedToTenant`/`TenantConfigurationSet` batch; final state contains all |
| T-D1-UNIT-002   | Unit    | `TenantDetail_RetryExhaustion_FailsObservably` — same as D2-UNIT-002 for tenant detail key            | D1      | 10-4  | Same DoD |
| T-D3-UNIT-001   | Unit    | `Audit_ConflictThenSuccess_PreservesOriginalAndExternallyAddedAndIncomingEntries`                     | D3      | 10-4  | Three entry groups (original persisted + externally added during conflict + incoming) — all present in final state, no duplicates |
| T-D3-UNIT-002   | Unit    | `Audit_RetryExhaustion_FailsObservably` — same pattern for `audit:{tenantId}` key                     | D3      | 10-4  | Same DoD |
| T-D4-UNIT-001   | Unit    | `Audit_DuplicateEventId_PersistedEntryAuthoritative_DuplicateSuppressed`                              | D4      | 10-4  | Same `EventId`, same payload → 1 entry in final state |
| T-D4-UNIT-002   | Unit    | `Audit_DuplicateEventId_WithMismatchedPayload_PersistedWins_NoDiagnosticPayloadLeak`                  | D4, S1  | 10-4  | Persisted wins; diagnostic captured doesn't contain incoming payload bytes |
| T-D5-UNIT-001   | Unit    | `Projection_RetryAfterConflict_LoadsFreshModel_NoStaleInstanceReuse`                                  | D5      | 10-4  | Spy on state-store reads; assert N+1 reads for N+1 attempts; identity-check that each attempt sees a new model instance |
| T-T2-UNIT-001   | Unit    | `CachingProjectionActor_AfterConflictSuccess_ReturnsPersistedStateNotCachedSnapshot` — cache coherence after retry | T2 | 10-4 | Cache invalidation proven; persisted state authoritative |
| T-T2-INT-001    | Tier 2  | `Projection_ReadAfterConflictResolution_CacheCoherentWithStorage` — real DAPR backed cache sanity check | T2      | 10-4  | Light integration check; skip-by-precondition if DAPR unavailable |
| T-S1-UNIT-001   | Unit    | `Projection_RetryExhaustion_Diagnostics_ContainNoTenantPayloadOrPII` — **negative assertion**         | S1      | 10-4  | Diagnostic captured must NOT contain `tenantName`, `userId`, audit payload, cursor, configuration values, serialized event bytes |
| T-S1-UNIT-002   | Unit    | `Projection_Cancellation_Diagnostics_ContainNoTenantPayloadOrPII` — same for cancellation path        | S1      | 10-3b | Same DoD; tied to 10-3b's cancellation diagnostic surface |
| T-S1-UNIT-003   | Unit    | `Audit_DuplicateEventIdMismatch_NoIncomingPayloadInDiagnostics` — combines D4 + S1                    | S1, D4  | 10-4  | Mismatched payload diagnostic must not leak the incoming bytes |
| T-O2-FIXTURE-001| Fixture | `ProjectionWriteConformanceFixture` drives **real** `TenantProjectionWritePolicy` / `TenantProjectionHandler` (no algo re-implementation) | O2 | 10-4 | Code-review rule + fixture asserts production type is the one invoked (via spy or type assertion at construction); a fixture-internal merge would fail this check |

#### P2 — MEDIUM risk coverage (Scores 4–5)

| Test ID         | Level   | Scenario                                                                                              | Risk(s) | Story  | DoD |
|-----------------|---------|-------------------------------------------------------------------------------------------------------|---------|--------|-----|
| T-D6-UNIT-001   | Unit    | `Audit_AfterConflictReload_OrderedByTimestampThenEventId_StableForCursor`                             | D6      | 10-4   | Same-timestamp entries ordered by `EventId`; cursor pagination over result remains stable across two consecutive queries |
| T-T1-UNIT-001   | Unit    | `TenantsProjectionActor_PreCancelledToken_NoStateAccess` — pre-cancellation gate before DAPR `GetStateAsync` | T1 | 10-3b  | NSubstitute spy: state store never called |
| T-T1-UNIT-002   | Unit    | `TenantsProjectionActor_MidFlowCancellation_DuringDaprStateRead_AbortsCleanly`                        | T1      | 10-3b  | Cancellation during reaches the read; `OperationCanceledException` rethrown; no partial successful result |
| T-T1-UNIT-003   | Unit    | `Cancellation_NotConvertedTo_Forbidden_NotFound_InvalidCursor_ActorFailure_EtagConflict_RetryExhaustion` — taxonomy | T1 | 10-3b | Confirms cancellation surface is `OperationCanceledException`, not any other adapter failure |
| T-T3-E2E-001    | Tier 3  | `AspireTopologyTests.*_starts_and_is_alive` — regression guard for existing liveness probes (post-10-3a) | T3   | (live) | Pass when Aspire harness boots; skip-by-precondition when Docker absent |
| T-T4-CI-001     | CI      | `verify-eventstore-submodule-commit` — CI job asserts EventStore submodule pointer matches the expected cancellation API commit | T4 | (CI) | Fails build if drift; emits diff in log |
| T-T5-UNIT-001   | Unit    | `Projection_3xRetryUnderContention_EventuallySucceedsOrFailsObservably` — deterministic 3-attempt budget exercise | T5 | 10-4 | Scripted conflicts at attempts 1, 2; success at attempt 3; OR all 3 conflict → observable failure |
| T-B1-UNIT-001   | Unit    | `Audit_MalformedJsonPayload_Skipped_NoSave` (preserve existing) — regression guard                    | B1      | 10-4   | Boundary preserved; no save side-effect; structured log of skip |
| T-B1-UNIT-002   | Unit    | `Audit_MetadataInvariantFailure_PropagatesNotSilent` (preserve existing) — regression guard           | B1      | 10-4   | Exception propagates; no save; audit state unchanged |
| T-O1-UNIT-001   | Unit    | `Projection_RetryExhaustion_EmitsStructuredFailureContext` — positive log-shape assertion             | O1      | 10-4   | Diagnostic includes safe categories: stage, attempt count, projection key category, correlation/trace id |

#### P3 — LOW risk coverage (Scores 1–3) — documented, not implemented

| Risk | Disposition |
|------|-------------|
| D7   | Document scope boundary in 10-4 test names + comments; no test |
| S2   | Cross-tenant key isolation already covered by NFR5 Tier 3 tests (existing) |
| S3   | Covered transitively by T-T1-UNIT-001 (pre-cancellation gate) + existing 9.3/9.4 forbidden-precedence tests |
| P2   | Defer to performance NFR pass (Epic 7 or follow-up) |
| P3   | Design property; not testable as a defect |
| B2   | Architecture acceptance; no test required |
| O3   | Code-review guard; story 10-4 spec already constrains |

### Execution Strategy

**Three-lane model:**

- **PR lane (every push/PR to main, target ≤ 12 min):**
  - All Tier 1 unit tests for Tenants and EventStore (filtered exclude: `AspireTopology`, `Quarantined`, `NightlyProperty`).
  - Includes every P0 and P1 test in this matrix.
  - Command: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Quarantined&FullyQualifiedName!~AspireTopologyTests"`

- **Nightly lane (full regression):**
  - Tier 1 + Tier 2 (DAPR/Docker required) + Tier 3 (Aspire topology liveness).
  - Runs the integration variants of P0/P1 tests (`T-D2-INT-001`, `T-T2-INT-001`) against a real DAPR Redis state store.
  - Includes `AspireTopologyTests` (T-T3-E2E-001) for fixture regression.
  - Command: `dotnet test Hexalith.Tenants.slnx --configuration Release` (unfiltered).

- **Weekly lane (deferred / NFR follow-up):**
  - Performance smoke for retry storm (P1 risk) and large-audit replay (P2 risk).
  - Out of scope for Epic 10 ship; NFR pass owns these.

**Cancellation tests** (T-T1-*) belong to Story 10-3b's deliverable, but the test design covers them here for completeness.

### Resource Estimates (Ranges)

Assuming Stories 10-1 + 10-2 helpers are stable when 10-4 begins.

| Bucket | Test count | Estimate           |
|--------|-----------|--------------------|
| P0 (D2) — 3 tests + singleton-index fixture extension | 3 | **~12–20 hours** |
| P1 (D1, D3, D4, D5, T2, S1, O2) — 12 tests + diagnostic capture infra + fixture rule | 12 | **~30–50 hours** |
| P2 (D6, T1, T3, T4, T5, B1, O1) — 10 tests + 1 CI guard | 10+1 | **~20–35 hours** |
| P3 — documentation + smoke comments | — | **~3–6 hours** |
| **TOTAL** | **25 tests + 1 fixture rule + 1 CI guard** | **~65–110 hours** |

**Calendar timeline:** ~2–3 weeks (1 dev) or ~1–1.5 weeks (paired). Maps roughly to Story 10-3b (cancellation tests) + Story 10-4 (conformance tests) combined effort.

### Quality Gates

| Gate                                                                       | Threshold                       | Enforcement                              |
|----------------------------------------------------------------------------|---------------------------------|------------------------------------------|
| **P0 BLOCKER (D2) covered + passing**                                      | 100%                            | Epic 10 ships only when 3 D2 tests green |
| **P1 HIGH risks (D1, D3, D4, D5, T2, S1, O2) covered + passing**           | 100% (or documented waiver)     | Architecture lead sign-off if any waived |
| **P2 MEDIUM risks covered or explicit deferral with ≤60d deadline**        | 100% covered OR deferred        | Tracked in `deferred-work.md`            |
| **Diagnostic-leak negative assertions (S1)**                                | Zero tolerance                  | Any failure → block release immediately  |
| **Line coverage on `TenantProjectionWritePolicy.cs`, `TenantProjectionHandler.cs`, `TenantAuditProjection.cs`** | ≥ 90%                           | coverlet output gate              |
| **`AspireTopologyTests` (T3) pass rate**                                    | ≥ 95% across last 10 CI runs    | Manual review of flake history; below threshold → fixture re-hardening |
| **EventStore submodule pointer drift (T4)**                                 | 0 — must match expected commit  | CI guard `T-T4-CI-001`                   |
| **Conformance fixture drives production helper (O2)**                       | 100% (mechanical assertion)      | `T-O2-FIXTURE-001` rule                  |
| **PR lane wall-clock**                                                      | ≤ 12 min                        | Monitor; if exceeded, partition tests    |

### Coverage Summary

- **Tests required:** 25 new/asserted + 1 fixture rule + 1 CI guard
- **Test level distribution:** Unit 22 · Integration (Tier 2) 2 · E2E (Tier 3) 1 (regression-only)
- **By priority:** P0 = 3 · P1 = 12 · P2 = 10 · P3 = 0 (documented only)
- **By risk category covered:** DATA 11 · TECH 5 · SEC 3 · BUS 2 · OPS 4 · PERF 0 (deferred to NFR)
- **Open requests:** Stories 10-3b and 10-4 to own implementation; CI guard to be added in CI workflow PR.

### Next Step → Done

→ `step-05-generate-output.md` — Finalize the test plan artifact, gate criteria, and traceability summary.

## Step 5 — Generate Output & Validate (Completed)

### Execution Mode

- Requested: not specified → `auto` from config
- Resolved: **sequential** (single Epic-Level artifact, no parallelization needed)

### Output Document

- **File:** `_bmad-output/test-artifacts/test-design-epic-10.md`
- **Template used:** `test-design-template.md` (adapted for .NET backend; UI/Playwright sections omitted as N/A)
- **Status:** Draft (ready for team review)

### Checklist Validation (Epic-Level Mode)

- ✅ All risks have unique IDs (R-001 … R-023) with cross-reference to working IDs
- ✅ Risks classified by category (TECH/SEC/PERF/DATA/BUS/OPS)
- ✅ Probability (1-3) and Impact (1-3) scored; Score = P × I calculated correctly
- ✅ High-priority risks (≥6) clearly marked with mitigation, owner, timeline
- ✅ Coverage matrix: 25 tests + 1 fixture rule + 1 CI guard, mapped to test levels and priorities
- ✅ No duplicate coverage (per-key, per-risk scoping)
- ✅ Execution Strategy uses PR / Nightly / Weekly (simple, no tier-redundancy)
- ✅ Resource estimates as **ranges** (e.g., "~65–110 hours"), no false precision
- ✅ Quality Gate Criteria defined (P0 = 100%, P1 = 100% or waived, diagnostic-leak zero-tolerance)
- ✅ Priority/Execution-timing separation note at top of Test Coverage Plan
- ✅ Risk Mitigation Plans for all 8 high-priority risks (specific strategy + owner + verification)
- ✅ Out-of-scope items explicitly listed with reasoning + mitigation
- ✅ Entry / Exit criteria defined
- ✅ Knowledge base fragments cited in Appendix B
- ✅ Related documents linked
- ✅ Test artifacts stored in `_bmad-output/test-artifacts/` (config-compliant location)

### Polish Pass

- Consolidated repeated risk context across Step 3 and Step 5; no duplication in the deliverable
- Risk IDs unified (R-001..R-023) in deliverable; working IDs (D1, T2, etc.) kept in Appendix A for traceability back to Step 3
- Tables aligned; markdown formatting clean
- No emoji slop in deliverable (sparingly used only in headline summary)

### Completion Report

**Mode used:** Epic-Level / Sequential
**Output:** `_bmad-output/test-artifacts/test-design-epic-10.md`
**Progress file:** `_bmad-output/test-artifacts/test-design-progress.md`

**Key risks identified:**
- 1 CRITICAL (R-001 — singleton index silent LWW) → BLOCK Epic 10 until P0 trio passes
- 7 HIGH (R-002 through R-008) → all mitigated by specific tests in the coverage matrix
- Risk concentration: DATA category (8 of 23; 5 of 8 high-or-above)

**Key gate thresholds:**
- P0 pass rate = 100% before Epic 10 ships
- P1 pass rate = 100% or documented architecture-lead waiver
- Diagnostic-leak negative assertions: zero tolerance
- Line coverage on projection write paths ≥ 90%
- AspireTopologyTests pass rate ≥ 95% over last 10 CI runs

**Total effort:** ~65–110 hours (~2–3 weeks / 1 dev, ~1–1.5 weeks paired) — spans Stories 10.3B + 10.4

**Open assumptions:**
1. Stories 10.1/10.2 helper APIs remain stable when Story 10.4 implementation begins.
2. EventStore submodule stays pinned at commit `bcccd504` until Story 10.3B approves a pointer advance.
3. No DAPR/Aspire/xUnit dependency upgrades during Epic 10 ship.

**Recommended next workflow:** Story 10.3B implementation (`/bmad-dev-story 10-3b-cancellation-token-threading-for-tenant-projection-queries`), then Story 10.4 implementation.
