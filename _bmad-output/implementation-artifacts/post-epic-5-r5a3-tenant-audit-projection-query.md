# Post-Epic-5 R5-A3: Tenant Audit Projection and Query

Status: backlog

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

## Source

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13-implementation-readiness-alignment.md`
- `_bmad-output/planning-artifacts/architecture.md` D12
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-13.md`
