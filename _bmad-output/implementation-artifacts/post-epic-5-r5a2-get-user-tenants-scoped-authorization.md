# Post-Epic-5 R5-A2: GetUserTenants Scoped Authorization

Status: backlog

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

## Source

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13-implementation-readiness-alignment.md`
- `_bmad-output/planning-artifacts/architecture.md` D11
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-13.md`
