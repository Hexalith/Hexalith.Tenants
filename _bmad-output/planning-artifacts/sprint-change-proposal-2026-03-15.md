# Sprint Change Proposal — EventStore Submodule Upgrade Alignment

**Date:** 2026-03-15
**Author:** Jerome (via Correct Course workflow)
**Change Scope:** Minor (documentation text fixes and pattern acknowledgment)
**Status:** DRAFT

---

## 1. Issue Summary

**Trigger:** Proactive upgrade audit — the Hexalith.EventStore submodule has received 11 source commits (102 files changed, +4,833 lines) since the Hexalith.Tenants microservice was created on 2026-03-07. The user requested a full assessment and alignment with the latest EventStore version.

**Finding:** The EventStore evolved **additively** with no breaking changes to core contracts (`IEventPayload`, `IRejectionEvent`, `AggregateIdentity`, `DomainResult`). The codebase builds successfully and all 24 contract tests pass. However, the planning artifacts contain stale text from the 2026-03-13 Sprint Change Proposal (rejection events pattern) that was approved but never fully applied to the epics document.

**Evidence:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release` — Build succeeded, 0 warnings, 0 errors
- `dotnet test` (Contracts) — 24/24 passed
- `git diff` on EventStore contracts (Events, Results, Identity) — No API changes
- Package versions (`Directory.Packages.props`) — Already in sync between both projects
- Submodule pointer — Already at latest (`006c6b6`, same as `origin/main`)

---

## 2. Impact Analysis

### Epic Impact

| Epic | Status | Impact |
|------|--------|--------|
| Epic 1 (Project Foundation) | Done | None — fully compatible |
| Epic 2 (Core Tenant Management) | In-progress | 4 stale exception→rejection text fixes in stories 2.2/2.3 |
| Epic 3 (Membership, Roles, Config) | Backlog | 6 stale exception→rejection text fixes in stories 3.1/3.3 |
| Epic 4 (Event-Driven Integration) | Backlog | None — compatible as-is |
| Epic 5 (Tenant Discovery & Query) | Backlog | New EventStore query pipeline infrastructure available (IQueryContract, QueryRouter, CachingProjectionActor) — noted in architecture |
| Epic 6 (Testing) | Backlog | 1 stale text fix in story 6.1; new test fakes available (FakeETagActor, FakeProjectionActor) |
| Epic 7 (Deployment & Observability) | Backlog | None — can leverage EventStore telemetry |
| Epic 8 (Documentation) | Backlog | None |

No epics added, removed, or resequenced.

### Artifact Conflicts Resolved

**Epics document (`epics.md`) — 11 text replacements applied:**

| Line | Old Reference | New Reference |
|------|--------------|---------------|
| 453 | `GlobalAdministratorAlreadyBootstrappedException` | `GlobalAdminAlreadyBootstrappedRejection` |
| 489 | `TenantAlreadyExistsException` | `TenantAlreadyExistsRejection` |
| 501 | `TenantDisabledException` | `TenantDisabledRejection` |
| 513 | `TenantNotFoundException` | `TenantNotFoundRejection` |
| 577 | `UserAlreadyInTenantException` | `UserAlreadyInTenantRejection` |
| 585 | `UserNotInTenantException` | `UserNotInTenantRejection` |
| 593 | `RoleEscalationException` | `RoleEscalationRejection` |
| 661 | `ConfigurationLimitExceededException` | `ConfigurationLimitExceededRejection` |
| 665 | `ConfigurationLimitExceededException` | `ConfigurationLimitExceededRejection` |
| 669 | `ConfigurationLimitExceededException` | `ConfigurationLimitExceededRejection` |
| 893 | `the same domain exceptions are thrown...` | `the same rejection events are returned via DomainResult.Rejection()...` |

**Architecture document (`architecture.md`) — 2 changes applied:**

1. **FR50-53 coverage table** (line 849): "Domain-specific exceptions" → "Domain-specific rejection events"
2. **New section added**: "Query Pipeline (EventStore Infrastructure — Available for Epic 5)" documenting `IQueryContract`, `IQueryResponse<T>`, `SubmitQuery`/`QueryRouter`, `CachingProjectionActor`, `ETagActor`, and `SelfRoutingETag` — so Epic 5 implementors know what infrastructure is available

### Technical Impact

- **Zero code changes needed** — the codebase is already compatible
- **No package version updates** — both projects use identical DAPR 1.16.1, MediatR 14.0.0, etc.
- **Submodule already at latest** — `006c6b6` matches `origin/main`

---

## 3. Recommended Approach

**Selected:** Direct Adjustment — Documentation text fixes within existing story boundaries.

**Rationale:**
- All EventStore changes are additive (new types/features) with zero breaking changes to core contracts
- The 11 stale exception references are residue from the approved 2026-03-13 SCP that wasn't fully applied
- The code already uses the correct rejection event pattern — only the planning text was stale
- New EventStore capabilities (query pipeline, projections, authorization, SignalR, GDPR encryption) will be naturally incorporated when their respective epics begin implementation
- No scope change, no new stories, no timeline impact

**Effort:** Low — text replacements only, already applied
**Risk:** Low — no code changes
**Timeline Impact:** None

---

## 4. Detailed Changes Applied

All changes have been applied to the planning artifacts during this workflow:

### Epics Document Changes
- 11 `*Exception` → `*Rejection` text replacements across stories 2.2, 2.3, 3.1, 3.3, and 6.1
- Completes the text updates that were approved in the 2026-03-13 Sprint Change Proposal but not applied

### Architecture Document Changes
- 1 FR coverage table text fix (exceptions → rejection events)
- 1 new section documenting EventStore query pipeline infrastructure for Epic 5

### No Changes Needed
- PRD: Already clean, no stale references
- Story 2.1 implementation artifact: Already correct (uses rejection events)
- CI/CD pipeline: Compatible
- DAPR component configs: Compatible
- Sprint status: No structural changes
- Codebase: Builds and passes all tests without modification

---

## 5. Implementation Handoff

**Scope:** Minor — No further action required.

All documentation fixes have been applied during this Correct Course workflow. No code changes, no backlog reorganization, no new stories.

**For future epic implementors:**

- **Stories 2.2/2.3 (Aggregates):** Use `EventStoreAggregate<TState>` fluent API as specified in the architecture document. Handle methods are `public static` pure functions returning `DomainResult`.
- **Story 2.4 (CommandApi):** Wire domain service with `builder.Services.AddEventStore()` and `app.UseEventStore()` following the EventStore sample pattern. Include `/process` endpoint for domain service invocation.
- **Epic 5 (Query):** Leverage the new EventStore query pipeline (`IQueryContract`, `QueryRouter`, `QueriesController`, `CachingProjectionActor`) as noted in the updated architecture document.
- **Epic 6 (Testing):** EventStore Testing package now provides `FakeETagActor`, `FakeProjectionActor`, `FakeRbacValidatorActor`, `FakeTenantValidatorActor` — use these for projection and query testing.

**Success Criteria:**
- All planning artifacts use consistent rejection event terminology (verified: zero stale exception references remain)
- Architecture document acknowledges EventStore query pipeline for Epic 5 (verified: section added)
- Build continues to succeed with zero warnings/errors (verified: `dotnet build` passes)
- All 24 contract tests continue to pass (verified: `dotnet test` passes)
