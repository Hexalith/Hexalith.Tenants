# Sprint Change Proposal: Implementation Readiness Alignment

Date: 2026-05-13
Project: Hexalith.Tenants
Prepared for: Jerome
Mode: Incremental
Approval status: Approved for implementation by Jerome on 2026-05-13

## 1. Issue Summary

The 2026-05-13 implementation readiness assessment found that the planning artifacts have drifted after implementation. PRD, UX, architecture, epics, sprint status, and actual completed work no longer tell one consistent story about Phase 1 MVP scope and remaining backend obligations.

This is not a rollback issue. The original Epics 1-8 are marked done in `sprint-status.yaml`, and several prior runtime blockers, including post-Epic-5 JWT authentication wiring, are already complete. The problem is alignment and targeted backend completeness:

- PRD Phase 1 defines a backend/package/documentation MVP and explicitly defers Admin UI/dashboard to Phase 2.
- UX treats the Tenants UI as a production Admin UI and FrontShell reference module with multiple must-ship screens and interaction patterns.
- Architecture D11-D17 incorporates UX-driven decisions, but the current implementation artifacts only partially cover the backend-relevant parts.
- `GetUserTenantsQuery` implements self and GlobalAdministrator lookup, but not D11 TenantOwner scoped lookup.
- `GetTenantAuditQuery` and endpoint exist, but Story 5.3 intentionally returns HTTP 501 for GlobalAdministrator audit requests, leaving D12/FR29 incomplete.

## 2. Impact Analysis

### Epic Impact

All original epics remain marked done. This proposal does not reopen Epics 1-8 wholesale and does not recommend rollback.

Affected areas:

- Epic 5 requires post-epic correction items for D11 scoped authorization and D12 audit projection/query behavior.
- Epic 7 snapshot work appears implementation-complete; only PRD/architecture wording needs reconciliation.
- Phase 2 Admin UI / FrontShell work should remain deferred unless explicitly promoted.

### Story Impact

New post-epic correction stories are required:

- `post-epic-5-r5a2-get-user-tenants-scoped-authorization`
- `post-epic-5-r5a3-tenant-audit-projection-query`

No completed story should be rolled back.

### Artifact Conflicts

PRD:

- Needs an explicit Phase 1 MVP scope clarification.
- Needs Admin UI wording updated from dashboard-only to Admin UI / FrontShell reference module in Phase 2.
- Needs snapshot language reconciled with completed baseline snapshot configuration.

Architecture:

- Needs D11-D17 scope clarification.
- Needs snapshot implementation status note.

UX:

- Needs a scope note that the UX spec is Phase 2 Admin UI / FrontShell design input, not a Phase 1 backend release blocker.

Deferred work:

- Needs D13-D17 UI/FrontShell deferral recorded.

Sprint status:

- Needs the two new post-epic correction stories added after final approval.

### Technical Impact

Backend MVP impact:

- D11: Update `TenantsProjectionActor.HandleGetUserTenantsAsync` to support TenantOwner scoped lookup without cross-tenant leakage.
- D12: Implement `TenantAuditProjection` / `TenantAuditReadModel` and change the audit endpoint from 501 to returning paginated audit entries, or explicitly revise PRD/architecture if audit is not MVP.

UI/FrontShell impact:

- D13-D17 are deferred to Phase 2 unless promoted.

## 3. Recommended Approach

Recommended path: **Hybrid: PRD MVP Review + Direct Adjustment**.

Rationale:

- Rollback is not justified by the evidence.
- Backend MVP remains viable and should not be blocked by Phase 2 UI scope.
- D11 and D12 are backend-relevant because they map to PRD FR28/FR29 and NFR5.
- D13-D17 are useful architecture/UX planning material, but they should not silently become Phase 1 blockers while Admin UI remains Phase 2.

Effort estimate: Medium.

Risk level: Medium.

Change scope classification: Moderate. Product/architecture artifacts need alignment, and Developer follow-up is needed for two focused backend correction stories.

## 4. Detailed Change Proposals

### Proposal 1: PRD MVP Scope Clarification

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: `Product Scope` / `MVP Feature Set` and `Post-MVP Features`

Replace:

```markdown
### MVP Feature Set (Phase 1)

Event contracts may evolve with breaking changes during pre-1.0 development. Event contract stability (zero breaking changes) is a v1.0 release milestone.
```

With:

```markdown
### MVP Feature Set (Phase 1)

Event contracts may evolve with breaking changes during pre-1.0 development. Event contract stability (zero breaking changes) is a v1.0 release milestone.

**MVP scope clarification (2026-05-13):** Phase 1 remains a backend/package/documentation MVP. It includes tenant domain behavior, query endpoints, audit-query capability, packages, tests, deployment, observability, and adoption documentation. The Hexalith.Tenants Admin UI / FrontShell reference module is Phase 2 unless explicitly promoted by a future scope decision.
```

Replace:

```markdown
3. **Admin UI / dashboard** — Visual management interface for tenants, users, and configuration
```

With:

```markdown
3. **Admin UI / FrontShell reference module** — Visual management interface for tenants, users, configuration, audit, role-aware workflows, and real-time projection feedback, guided by `ux-design-specification.md`
```

### Proposal 2: Architecture Scope Clarification for D11-D17

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `UX-Driven Architecture Amendments (2026-03-25)`

Replace:

```markdown
_Amendments based on the UX Design Specification (ux-design-specification.md, completed 2026-03-24). The UX spec introduces frontend screens, interaction patterns, and data requirements that surface gaps in the original architecture. Each amendment references the original decision it extends._
```

With:

```markdown
_Amendments based on the UX Design Specification (ux-design-specification.md, completed 2026-03-24). The UX spec introduces frontend screens, interaction patterns, and data requirements that surface gaps in the original architecture. Each amendment references the original decision it extends._

**Scope clarification (2026-05-13):** D11 and D12 are backend MVP-relevant because they affect PRD FR28/FR29 and NFR5. D13-D17 are Phase 2 Admin UI / FrontShell reference-module concerns unless a future scope decision promotes the UI to MVP. Backend implementation must not treat D13-D17 as Phase 1 release blockers, but Phase 2 UI stories must explicitly sequence these dependencies.
```

### Proposal 3: Add Post-Epic D11 Correction Story

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

New story file: `_bmad-output/implementation-artifacts/post-epic-5-r5a2-get-user-tenants-scoped-authorization.md`

Sprint status addition:

```yaml
  # Post-Epic-5 Authorization Correction (SCP-2026-05-13)
  # R5-A2: GetUserTenants currently supports self and GlobalAdmin lookup only.
  # Add D11 TenantOwner scoped lookup so owners can query users only within tenants they own,
  # while preventing visibility into other tenants.
  post-epic-5-r5a2-get-user-tenants-scoped-authorization: backlog
```

Story summary:

```markdown
# Post-Epic-5 R5-A2: GetUserTenants Scoped Authorization

## Story

As a TenantOwner,
I want `GetUserTenantsQuery` to return another user's memberships only for tenants I own,
So that I can manage my tenant's users without seeing cross-tenant access data.

## Acceptance Criteria

1. Given requester is querying themselves, when `GetUserTenantsQuery` runs, then all of their own memberships are returned.
2. Given requester is GlobalAdministrator, when querying any user, then all target-user memberships are returned.
3. Given requester is TenantOwner of Tenant A and target user belongs to Tenant A and Tenant B, when querying the target user, then only Tenant A membership is returned.
4. Given requester is TenantOwner of Tenant A and target user belongs only to Tenant B, when querying the target user, then an empty result is returned, not forbidden and not Tenant B data.
5. Given requester is not self, not GlobalAdministrator, and owns none of the target user's tenants, when querying the target user, then an empty result is returned or 403 according to the final API contract, with no cross-tenant data leakage.
6. Tests cover self, GlobalAdmin, TenantOwner partial visibility, TenantOwner no overlap, and ordinary non-owner cases.

## Implementation Notes

Current code rejects all non-admin users querying another user. Update `TenantsProjectionActor.HandleGetUserTenantsAsync` to apply D11 row-level filtering using `TenantIndexReadModel` membership data. Do not use user-controllable claims for identity; use `QueryEnvelope.UserId`.
```

### Proposal 4: Add Post-Epic D12 Audit Projection Story

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

New story file: `_bmad-output/implementation-artifacts/post-epic-5-r5a3-tenant-audit-projection-query.md`

Sprint status addition:

```yaml
  # Post-Epic-5 Audit Projection Correction (SCP-2026-05-13)
  # R5-A3: Story 5.3 created GetTenantAuditQuery and endpoint but intentionally returns 501.
  # Implement or explicitly settle D12 TenantAuditProjection / TenantAuditReadModel so FR29 is truthful.
  post-epic-5-r5a3-tenant-audit-projection-query: backlog
```

Story summary:

```markdown
# Post-Epic-5 R5-A3: Tenant Audit Projection and Query

## Story

As a GlobalAdministrator,
I want tenant audit queries to return access and administrative events by tenant and date range,
So that I can produce operational and compliance evidence from the tenant event stream.

## Acceptance Criteria

1. Given tenant events exist for a tenant, when `TenantAuditProjection` processes them, then `TenantAuditReadModel` stores audit entries with event ID, event type, category, actor ID, timestamp, tenant ID, and narrative payload.
2. Given `GET /api/tenants/{tenantId}/audit` is called by GlobalAdministrator with date range parameters, then paginated audit entries are returned instead of HTTP 501.
3. Given a non-GlobalAdministrator calls the audit endpoint, then the request remains forbidden.
4. Given category filter is provided, then only matching `Access` or `Administrative` events are returned.
5. Given pagination parameters are provided, then results are returned in stable timestamp/event ordering with a valid cursor.
6. Tests cover projection application, category classification, date range filtering, authorization, pagination, and empty results.

## Implementation Notes

Current Story 5.3 deliberately returns 501 for admin audit requests. This story implements architecture D12. If product decides audit query implementation is not MVP, then PRD FR29 and architecture D12 must be amended instead; do not leave endpoint behavior and requirements contradictory.
```

### Proposal 5: Defer D13-D17 UI/FrontShell Work to Phase 2

Artifacts:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`

Deferred work addition:

```markdown
## Deferred from: implementation readiness correction (2026-05-13)

- D13-D17 UX-driven architecture amendments are Phase 2 Admin UI / FrontShell reference-module work unless explicitly promoted by a future scope decision.
- Deferred items include SignalR three-phase UI confirmation, FrontShell `pendingIds`, concurrent command support, toast batching, `<AuditTimeline>`, `<ConsequencePreview>`, FrontShell design tokens, and UI `blockedBy` sequencing.
- Backend MVP work must still satisfy D11 query-side authorization and D12 audit query requirements because those map to PRD FR28/FR29 and NFR5.
```

UX spec note addition:

```markdown
> **Scope note (2026-05-13):** This UX specification remains the authoritative design input for the Phase 2 Admin UI / FrontShell reference module. It is not a Phase 1 backend MVP release blocker unless the Admin UI scope is explicitly promoted. Backend requirements that originated from UX and affect FR28/FR29/NFR5 are tracked separately through D11/D12 correction work.
```

### Proposal 6: Documentation Reconciliation for Snapshot Policy

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`

PRD replacement:

```markdown
- Event store snapshots for faster state reconstruction at scale
```

to:

```markdown
- Advanced snapshot optimization beyond the baseline EventStore snapshot configuration
```

PRD replacement:

```markdown
NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Event store snapshots are a Phase 3 optimization if this target is exceeded at scale
```

to:

```markdown
NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Baseline EventStore snapshot configuration is part of Phase 1 reliability/performance work; advanced snapshot tuning beyond the baseline configuration is a Phase 3 optimization if this target is exceeded at scale.
```

Architecture Snapshot Strategy addition:

```markdown
**Implementation status note (2026-05-13):** Story 7.3 configured the tenant domain snapshot interval at 50 events and retained the default 100-event interval for GlobalAdministrator. This satisfies the Phase 1 baseline snapshot configuration. Future Phase 3 snapshot work refers only to advanced optimization beyond this baseline.
```

## 5. Implementation Handoff

Scope classification: **Moderate**.

Routed to:

- Product/Architecture owner for artifact alignment.
- Developer agent for D11/D12 post-epic correction stories.

Responsibilities:

- Product/Architecture owner: apply PRD, architecture, UX, and deferred-work clarifications.
- Product Owner / Scrum Master: add the two post-epic correction stories to sprint status after approval.
- Developer agent: implement or verify R5-A2 and R5-A3.
- QA/Test Architect: confirm D11 cross-tenant authorization coverage and D12 audit projection/query coverage.

Success criteria:

- PRD, architecture, UX, and deferred-work artifacts consistently define Admin UI/FrontShell as Phase 2 unless promoted.
- Sprint status tracks R5-A2 and R5-A3.
- `GetUserTenantsQuery` satisfies D11 without cross-tenant leakage.
- Audit endpoint no longer contradicts FR29/D12, either by implementation or by explicitly revised product scope.
- Snapshot policy language matches Story 7.3 implementation reality.

## 6. Verification

No source code tests were run for this proposal. This document is planning/status work only.
