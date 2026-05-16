# Sprint Change Proposal: Implementation Readiness Grooming

**Date:** 2026-05-16  
**Project:** Hexalith.Tenants  
**Trigger:** `implementation-readiness-report-2026-05-16.md`  
**Status:** Approved and applied
**Approval:** Jerome approved this Sprint Change Proposal on 2026-05-16.

## 1. Issue Summary

The May 16 implementation-readiness assessment found the planning set conditionally ready for Phase 1 backend/package/documentation implementation, with no critical Phase 1 blockers. The remaining risk was targeted grooming debt:

1. Story 9.3 carried an unsettled disabled-tenant/orphan-membership query policy.
2. Story 10.3 depended on Hexalith.EventStore projection APIs that may not support cancellation.
3. Story 2.4 was too large to use as a sprint-execution model.
4. Epic 12 could be misread as Phase 1 implementation scope instead of Phase 2 planning/readiness work.

The correction keeps the PRD and architecture intact. It tightens story readiness and sequencing rather than changing MVP scope.

## 2. Impact Analysis

### Epic Impact

**Epic 2:** Story 2.4 remains historically complete. It is not renumbered or reopened, but its acceptance evidence is now grouped into five logical work packages for review and future maintenance.

**Epic 9:** Story 9.3 is now implementation-ready because the query policy is explicit before runtime behavior is changed.

**Epic 10:** Story 10.3 is split into an EventStore prerequisite and a Tenants integration story. Tenants work is blocked until the EventStore API dependency is resolved or an existing cancellation-aware API is named.

**Epic 12:** Epic 12 remains valid as Phase 2 planning/readiness and dependency governance. It is not Phase 1 backend implementation scope and must not be counted as shipped UI behavior.

### Artifact Conflicts

**PRD:** No PRD change required. Phase 1 remains backend/package/documentation only, with Admin UI deferred to Phase 2 unless explicitly promoted.

**Architecture:** No architecture change required. Architecture already documents eventual query consistency, UX-driven query/projection needs, and FrontShell dependencies.

**UX:** No Phase 1 UX implementation change required. The UX spec remains the authoritative Phase 2 Admin UI input.

**Sprint Status:** Updated with a post-readiness grooming entry instead of changing completed historical story statuses.

## 3. Recommended Approach

**Selected path:** Direct Adjustment  
**Scope classification:** Moderate  
**Effort estimate:** Low  
**Risk level:** Low  
**Timeline impact:** Minimal for Phase 1, except Story 10.3B must wait for EventStore cancellation API readiness.

Direct adjustment is the right path because the readiness report did not identify missing requirements, invalid architecture, or a need to reduce MVP scope. The work is grooming: settle policy, expose a dependency, prevent oversized-story ambiguity, and protect the Phase 1/Phase 2 boundary.

Rollback is not useful because Story 2.4 is already complete and renumbering historical work would make the sprint record less clear. MVP review is not needed because all PRD FRs remain covered.

## 4. Detailed Change Proposals

### Story 9.3: Query Policy for Disabled Tenants and Orphan Memberships

**Section:** Story policy note and Acceptance Criteria

**OLD:**

```markdown
Given the product policy is not yet settled
When implementation starts
Then the story records the selected policy before changing runtime behavior.
```

**NEW:**

```markdown
Policy decision (2026-05-16):
Disabled tenants are included in query responses when the caller is otherwise authorized to see the tenant or membership. The response must include tenant status so clients can distinguish disabled access from active access. Disabled status does not imply command capability; state-changing commands against disabled tenants remain rejected by the aggregate.

Orphan memberships are filtered from normal user-facing query responses, never returned with blank or synthesized tenant names. The endpoint logs an observable projection-repair warning with correlation metadata and preserves immutable audit/event history for investigation.

Eventual consistency after membership removal is accepted and documented: a self-lookup can briefly show the prior membership until projection catch-up completes, but this query result grants no write capability.
```

**Rationale:** Removes implementation-time product drift and gives tests a concrete behavior to assert.

### Story 10.3: Cancellation Token Threading for Projection Queries

**Section:** Story split

**OLD:**

```markdown
Given EventStore projection infrastructure lacks cancellation-aware signatures
When implementation begins
Then the required submodule API changes are documented and coordinated before changing Tenants call sites.
```

**NEW:**

```markdown
Story 10.3A: EventStore Projection Cancellation API Prerequisite
- Adds cancellation-aware projection query/dispatch contracts in Hexalith.EventStore.
- Names `IProjectionActor.QueryAsync`, `CachingProjectionActor.ExecuteQueryAsync`, and `EventStoreProjection<TReadModel>.Project(...)` / `ProjectFromJson(...)` as current dependency points.

Story 10.3B: Cancellation Token Threading for Tenant Projection Queries
- Blocked by Story 10.3A unless grooming confirms an existing EventStore cancellation-aware projection path and records exact APIs before implementation.
```

**Rationale:** Makes the external submodule dependency explicit and prevents Tenants implementation from starting against unavailable APIs.

### Story 2.4: Tenant Service, Bootstrap & Event Publishing

**Section:** Post-readiness decomposition note

**NEW:**

```markdown
Story 2.4 is complete and should not be renumbered retroactively, but the May 16 implementation-readiness assessment found that it is too broad to use as a future sprint-execution model. For evidence review, maintenance, and any future rework, treat the completed story as five logical work packages:

2.4A Command API and EventStore processing endpoint wiring
2.4B Bootstrap hosted service and multi-instance idempotency
2.4C DAPR pub/sub publication and recovery behavior
2.4D API error and authentication response mapping
2.4E Tier 2 command pipeline verification
```

**Rationale:** Preserves historical sprint truth while giving reviewers and future agents clear boundaries.

### Epic 12: Phase 2 Admin UI Dependency Sequencing

**Section:** Epic scope note

**NEW:**

```markdown
Epic 12 is a Phase 2 planning/readiness and dependency-governance epic. It is not Phase 1 backend implementation scope and should not be counted as shipped Admin UI product behavior. When Phase 2 UI work is ready to implement, convert the outputs of these stories into concrete UI implementation stories with explicit blockedBy dependencies.
```

**Rationale:** Keeps Phase 1 implementation focused and prevents planning artifacts from being treated as shipped product capability.

## 5. Checklist Results

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | Done | Trigger is the May 16 readiness report. |
| 1.2 Core problem defined | Done | Grooming debt, not missing PRD coverage. |
| 1.3 Evidence gathered | Done | Report plus affected epics, story, sprint status, and EventStore APIs. |
| 2.1 Current epic impact | Done | Epics 2, 9, 10, and 12 affected. |
| 2.2 Epic-level changes | Done | Story 10.3 split; Epic 12 scope clarified. |
| 2.3 Remaining epics reviewed | Done | No additional Phase 1 blockers. |
| 2.4 Future epic invalidation | Done | No epic invalidated. |
| 2.5 Epic order/priority | Done | Tenants 10.3B now blocked by EventStore 10.3A. |
| 3.1 PRD conflict check | Done | No PRD change needed. |
| 3.2 Architecture conflict check | Done | No architecture change needed. |
| 3.3 UX conflict check | Done | UX remains Phase 2 input. |
| 3.4 Other artifacts | Done | `sprint-status.yaml` updated with post-readiness correction entry. |
| 4.1 Direct adjustment | Viable | Selected path. |
| 4.2 Rollback | Not viable | Would obscure completed Story 2.4 history. |
| 4.3 MVP review | Not viable | MVP scope remains achievable. |
| 4.4 Recommended path selected | Done | Direct adjustment. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact and adjustments | Done | Included above. |
| 5.3 Recommended path | Done | Included above. |
| 5.4 MVP impact/action plan | Done | No MVP reduction. |
| 5.5 Handoff plan | Done | Product/Developer for follow-up grooming; EventStore maintainer for 10.3A. |
| 6.1 Checklist completion | Done | Applicable items addressed. |
| 6.2 Proposal accuracy | Done | Consistent with readiness report and current sprint status. |
| 6.3 User approval | Done | Jerome approved this Sprint Change Proposal on 2026-05-16. |
| 6.4 sprint-status update | Done | Added May 16 post-readiness grooming entry. |
| 6.5 Next steps and handoff | Done | Listed below. |

## 6. Implementation Handoff

**Handoff recipients:** Product Owner / Developer, with EventStore maintainer coordination for Story 10.3A.

**Responsibilities:**

- Product Owner: Confirm the Story 9.3 policy remains the desired product behavior before implementation starts.
- Developer: Implement Story 9.3 against the settled policy and include tests/documentation notes for disabled tenants, orphan memberships, and eventual-consistency behavior.
- EventStore maintainer: Complete or explicitly reject Story 10.3A cancellation-aware projection APIs before Tenants Story 10.3B starts.
- Developer: Use Story 2.4 logical packages as review evidence boundaries and future maintenance slices.
- Product Owner / UX: Keep Epic 12 as Phase 2 planning/readiness until concrete UI stories with `blockedBy` dependencies are created.

**Success criteria:**

1. Story 9.3 no longer contains unsettled policy language.
2. Story 10.3 cannot start Tenants implementation while the EventStore dependency is unresolved.
3. Story 2.4 has clear evidence boundaries without rewriting completed sprint history.
4. Epic 12 is visibly Phase 2 planning/readiness, not Phase 1 product implementation.
5. Phase 1 remains backend/package/documentation only.
