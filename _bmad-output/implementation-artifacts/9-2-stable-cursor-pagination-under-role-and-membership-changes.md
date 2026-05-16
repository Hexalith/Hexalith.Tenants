# Story 9.2: Stable Cursor Pagination Under Role and Membership Changes

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a tenant operator,
I want paginated tenant query results to remain predictable when roles or memberships change between page requests,
so that users do not silently skip or gain visibility into tenants because projection state changed mid-pagination.

## Acceptance Criteria

1. Given a user is paging through `get-user-tenants` results, when their role on a tenant is revoked between page requests, then the next page does not reveal tenants the requester is no longer allowed to see.
2. Given a user is paging through `get-user-tenants` results, when a newly visible tenant would sort before or at the submitted cursor position, then the endpoint behavior is documented and tested so the result is predictable rather than accidental.
3. Given `list-tenants` and `get-user-tenants` both use cursor-based pagination, when role or tenant state changes occur between page fetches, then both endpoints follow the same documented cursor stability policy where applicable.
4. Given a cursor references an item that is no longer visible to the requester, when the endpoint processes the request, then it safely advances or rejects according to the chosen policy without leaking the hidden item.
5. Given focused tests simulate concurrent membership and role mutation, when paginated queries continue from a prior cursor, then tests verify no cross-tenant data leak and document any accepted eventual-consistency behavior.

## Tasks / Subtasks

- [ ] Document and enforce the tenant query cursor stability policy. (AC: 1-4)
  - [ ] Treat the decoded cursor position as an exclusive lower bound over stable ordinal tenant IDs or user IDs after the current request's authorization filter has been applied.
  - [ ] Do not implement snapshot cursors in this story; each page reflects the latest projected read model visible to the requester at the time of that request.
  - [ ] If a tenant becomes newly visible and sorts before or equal to the cursor, do not backfill it into later pages; document this accepted current-state behavior.
  - [ ] If a tenant or membership becomes hidden after the cursor was issued, never return it on a later page; continue with visible items whose stable keys sort after the decoded position.
  - [ ] If the cursor anchor no longer exists or is no longer visible, do not disclose that fact; continue using the opaque decoded position as a lower bound or reject only when cursor validation itself fails.
- [ ] Update `TenantsProjectionActor` pagination behavior only where needed. (AC: 1-4)
  - [ ] Preserve existing ordering for `list-tenants`, `get-tenant-users`, and `get-user-tenants`: `StringComparer.Ordinal` on the key selected by the endpoint.
  - [ ] Keep authorization filtering before pagination for `list-tenants` non-admin users and `get-user-tenants` tenant-owner scoped cross-user lookups.
  - [ ] Keep the R5-A2 timing-uniformity guard: cross-user `get-user-tenants` lookups still run the global-admin check before early empty responses.
  - [ ] Reuse the Story 9.1 cursor codec/scope path if it is complete; do not introduce a second cursor format or a parallel signing abstraction.
  - [ ] Keep `get-tenant-audit` ordering by timestamp then event ID; include it in tests only if shared pagination utility changes could affect it.
- [ ] Add focused mutation-between-pages tests. (AC: 1-5)
  - [ ] Add `get-user-tenants` self-lookup test where a tenant visible on the next page is removed between page requests and is not returned.
  - [ ] Add `get-user-tenants` test where a newly visible tenant sorts before or at the cursor and is not backfilled into the next page.
  - [ ] Add `get-user-tenants` tenant-owner cross-user test where owner visibility changes between pages and only currently owned overlapping tenants are returned.
  - [ ] Add `list-tenants` non-admin test where membership changes between pages and the next page applies current membership filtering.
  - [ ] Preserve or update existing cursor tests such as `ListTenants_cursor_skips_deleted_tenant` so the behavior is described as "cursor anchor missing/hidden continues from lower bound", not as a deletion feature.
- [ ] Update documentation or test names/comments to make the policy visible. (AC: 2, 3, 5)
  - [ ] Prefer concise comments near the tests and/or a short docs note if an existing tenant query documentation file covers pagination.
  - [ ] State that the API offers stable keyset continuation, not repeatable-read snapshots across page requests.
  - [ ] State that authorization is re-evaluated on every page request and revoked visibility wins over cursor continuity.

## Dev Notes

### Current Pagination Policy To Implement

- Use current-state keyset continuation. The cursor's internal position is an exclusive lower bound, not a durable snapshot of all items that were visible on page one.
- The allowed behavior under projection mutation is:
  - Revoked or hidden tenant access must never appear in later pages, even if the cursor was issued before revocation.
  - Newly visible items with keys less than or equal to the cursor are not returned later. This is an accepted no-backfill behavior.
  - Newly visible items with keys greater than the cursor may appear on later pages because each page uses current projection state.
  - Missing or hidden cursor anchors are not special. Do not look up the anchor to decide visibility; compare visible keys to the decoded position.
- This policy preserves NFR5: no cross-tenant query leak. It also preserves FR30's consistent ordering without promising repeatable-read pagination across projection changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.2`; `_bmad-output/planning-artifacts/prd.md#FR30`; `_bmad-output/planning-artifacts/architecture.md#Cross-Tenant Index Projections`]

### Current Code State

- `TenantsProjectionActor` handles all tenant query authorization and pagination. `Paginate` currently orders items by endpoint key selector with `StringComparer.Ordinal`, filters with `string.Compare(key, cursor, Ordinal) > 0`, takes `pageSize + 1`, and returns the last returned key as the next cursor. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleListTenantsAsync` loads `TenantIndexReadModel`, branches global admins to all tenants, filters non-admins to tenant IDs from `indexModel.UserTenants[envelope.UserId]`, then paginates. This is the main `list-tenants` surface for membership mutation tests. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetUserTenantsAsync` resolves target user from `EntityId`, checks global-admin status before early empty return for timing uniformity, filters cross-user visibility to requester-owned tenant IDs unless the requester is self or global admin, then paginates. Preserve this ordering. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `TenantIndexReadModel` keeps `Tenants` and `UserTenants` dictionaries. User add/remove/role-change projection events update `UserTenants`; tenant disable/enable changes `TenantIndexEntry.Status` without removing visibility. [Source: `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- `UserTenantMembership` and `TenantSummary` already include `TenantStatus`, so Story 9.2 does not need a public query contract change. [Source: `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs`]

### Story 9.1 Dependency And Guardrails

- Story 9.1 is currently in progress in `sprint-status.yaml`. This story should reuse its cursor codec and endpoint scopes once that work is complete. The current source already contains `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`, but `TenantsProjectionActor` still has raw cursor parsing at the time this story was created; verify the final 9.1 implementation before editing. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- Do not create another cursor codec, another Data Protection purpose, or a second internal cursor DTO. Extend or reuse the Story 9.1 path.
- If Story 9.1 changes `Paginate` to encode/decode opaque cursors, Story 9.2 should test against signed public cursors and decoded internal positions, not raw public cursor strings.
- Invalid/tampered/scope-mismatched cursors remain Story 9.1 behavior. Story 9.2 is about valid cursors whose logical position remains meaningful after read-model authorization state changes.

### Boundaries

- Do not implement Story 9.3 disabled-tenant/orphan-membership policy here. Current status values should continue to flow through query results. Story 9.3 owns any repair logging or orphan filtering changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- Do not centralize all page-size policy or cursor utility behavior unless it is necessary for the mutation tests. Story 9.5 owns shared pagination bounds and utility cleanup. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`]
- Do not modify the `Hexalith.EventStore` submodule for this story.
- Preserve API/controller shape. `PaginatedResult<T>` remains `Items`, `Cursor`, and `HasMore`. [Source: `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`]

### Files Likely To Update

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: ensure list/user pagination applies the policy after authorization filtering and after Story 9.1 cursor decoding.
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`: only if Story 9.1 left a small scope/position gap needed by this story.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: add mutation-between-pages tests and update raw cursor assertions if Story 9.1 signed cursors are active.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: only if the final behavior needs API-level evidence for no leak under signed public cursors.
- Existing docs under `docs/` or tenant query docs if a pagination policy page already exists; otherwise focused test names/comments are acceptable.

### Testing Requirements

- Use xUnit and Shouldly. Keep test names PascalCase/descriptive, matching existing project style.
- Actor tests currently instantiate `TenantsProjectionActor` with substituted `DaprClient` and `NullLogger`; adjust the helper only if Story 9.1 added cursor codec dependencies.
- Test by issuing page one, mutating or substituting the in-memory `TenantIndexReadModel`, then issuing page two with the prior cursor. The important assertion is current visible results after the cursor position, not object identity of the old page.
- Include at least one non-admin `list-tenants` test and at least two `get-user-tenants` tests because Story 9.2's risk is highest in scoped user lookup.
- Keep existing 401/403/404 behavior green. This story should not change controller authorization or query error mapping.

### Previous Story Intelligence

- Story 9.1 establishes opaque signed cursors and should be treated as the cursor format dependency. Its guardrails say not to solve Story 9.2 cursor stability there; this story is that follow-up.
- R5-A2 introduced tenant-owner scoped `get-user-tenants` visibility and timing-uniformity checks for missing target users. Do not regress those protections while adding mutation tests.
- R5-A3 implemented audit query behavior and tenant-scoped audit filtering. Do not broaden audit visibility while touching shared pagination helpers.
- Recent git commits are release/preflight automation-oriented (`462b3d9`, `7f399fc`, `f43dc69`) and do not provide a better pagination pattern than the current actor tests.

### Project Context Reference

- Follow `AGENTS.md`: do not run recursive submodule initialization/update, and use Conventional Commits for any commit.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, no inline package versions, Shouldly assertions, and focused tests.
- The persistent BMAD context files reinforce that root-level submodules are separate roots; `Hexalith.EventStore` is reference context only unless explicitly approved.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List
