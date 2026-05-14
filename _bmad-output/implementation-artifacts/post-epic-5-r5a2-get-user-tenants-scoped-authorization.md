# Post-Epic-5 R5-A2: GetUserTenants Scoped Authorization

Status: review

## Story

As a TenantOwner,
I want `GetUserTenantsQuery` to return another user's memberships only for tenants I own,
so that I can manage my tenant's users without seeing cross-tenant access data.

## Acceptance Criteria

1. Given the requester queries their own user ID, when `GetUserTenantsQuery` runs, then all of the requester's own memberships are returned with the existing `UserTenantMembership` response shape.
2. Given the requester is a GlobalAdministrator, when they query any user ID, then all target-user memberships are returned.
3. Given the requester is TenantOwner of Tenant A and the target user belongs to Tenant A and Tenant B, when the requester queries the target user, then only the Tenant A membership is returned.
4. Given the requester is TenantOwner of Tenant A and the target user belongs only to Tenant B, when the requester queries the target user, then the query succeeds with an empty page and no Tenant B data.
5. Given the requester is not the target user, not a GlobalAdministrator, and owns none of the target user's tenants, when the requester queries the target user, then the query succeeds with an empty page and no cross-tenant data leakage.
6. Given the target user has no memberships or the tenant index state is missing, when `GetUserTenantsQuery` runs, then the existing empty `PaginatedResult<UserTenantMembership>` behavior is preserved.
7. Given the filtered result set has more rows than the requested page size, when cursor pagination is used, then pagination is applied after authorization filtering using the existing stable tenant-id ordering.
8. Tests cover self lookup, GlobalAdministrator lookup, TenantOwner partial visibility, TenantOwner no-overlap empty result, ordinary non-owner empty result, missing target-user memberships, and pagination after filtering.

## Tasks / Subtasks

- [x] Task 1: Update `GetUserTenantsQuery` authorization filtering (AC: #1-7)
  - [x] 1.1 Replace the early non-admin cross-user `Forbidden` branch in `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`.
  - [x] 1.2 Load `TenantIndexReadModel` before deciding non-admin cross-user scope, because requester ownership is stored in the index.
  - [x] 1.3 Preserve self lookup: if `targetUserId == envelope.UserId`, return the target user's full membership dictionary.
  - [x] 1.4 Preserve GlobalAdministrator lookup using `IsGlobalAdminAsync(envelope.UserId)`: admins see the target user's full membership dictionary.
  - [x] 1.5 Add TenantOwner scoped lookup: build requester-owned tenant IDs from `indexModel.UserTenants[envelope.UserId]` where role is `TenantRole.TenantOwner`, then keep only target-user memberships whose tenant ID is in that owned set.
  - [x] 1.6 Return an empty successful page for non-admin cross-user lookups with no owned-tenant overlap; do not return 403 after the user is authenticated and the query reaches actor logic.
  - [x] 1.7 Keep current response projection to `UserTenantMembership(TenantId, Name, Status, Role)` and existing default behavior when a tenant entry is absent from `indexModel.Tenants`.

- [x] Task 2: Update actor tests for D11 cases (AC: #1-8)
  - [x] 2.1 Keep or strengthen `GetUserTenants_own_user_returns_memberships`.
  - [x] 2.2 Keep `GetUserTenants_global_admin_can_query_any_user`.
  - [x] 2.3 Replace `GetUserTenants_non_admin_querying_other_user_returns_forbidden` with an ordinary non-owner cross-user test that expects a successful empty page.
  - [x] 2.4 Add TenantOwner partial visibility test: requester owns Tenant A, target has Tenant A and Tenant B, result contains only Tenant A.
  - [x] 2.5 Add TenantOwner no-overlap test: requester owns Tenant A, target has Tenant B only, result is empty and successful.
  - [x] 2.6 Add pagination-after-filtering test using a target user with multiple memberships and requester ownership over only a subset.

- [x] Task 3: Add integration coverage if the existing test fixture can seed index state cheaply (AC: #3-5, #8)
  - [x] 3.1 Prefer focused actor tests as the required regression guard.
  - [x] 3.2 Add or extend `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` only if the fixture already exposes a stable way to seed the cross-tenant index without live DAPR fragility.
  - [x] 3.3 If integration setup would require broad infrastructure work, document that actor tests are the Tier 1 guard and leave Tier 2/Tier 3 expansion to the DAPR-backed security suite.

- [x] Task 4: Verify focused tests (AC: #8)
  - [x] 4.1 Run `dotnet test .\tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`.
  - [x] 4.2 If integration tests were changed, run the focused integration test filter for `TenantsQueryControllerIntegrationTests`.

## Dev Notes

### Current State

The selected sprint-status item is this post-Epic-5 correction. A stub story file already existed, but it only summarized the issue. This version expands it into the implementation guide for D11.

`TenantsProjectionActor.HandleGetUserTenantsAsync` currently rejects every cross-user non-admin lookup before loading the cross-tenant index:

- Dispatch enters `HandleGetUserTenantsAsync` from the `"get-user-tenants"` switch in `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`.
- The current method computes `targetUserId` from `envelope.EntityId` or `envelope.UserId`.
- The current guard returns `Forbidden` when `targetUserId != envelope.UserId` and the requester is not GlobalAdmin.
- Only after that guard does it load `TenantIndexReadModel` from `projection:tenant-index:singleton`.
- This means TenantOwners cannot query users they manage, contradicting D11 and the 2026-05-13 readiness correction.

`TenantsQueryController.GetUserTenantsAsync` already routes `GET /api/users/{userId}/tenants` through `SubmitQuery` with `UserId` set from the authenticated `sub` claim and `EntityId` set to the route `userId`. Keep that contract. Do not add route-level role checks; domain query scoping belongs in the actor.

### Required Algorithm

Implement the D11 row-level filter in `HandleGetUserTenantsAsync`:

1. Resolve `targetUserId`.
2. Load `TenantIndexReadModel` from `StateStoreName` and `TenantIndexProjectionKey`.
3. If the index is null or target user has no memberships, return the existing successful empty `PaginatedResult<UserTenantMembership>`.
4. If requester is querying themselves, use all `targetUserTenants`.
5. Else if requester is GlobalAdmin, use all `targetUserTenants`.
6. Else compute `requesterOwnedTenantIds` from `indexModel.UserTenants[envelope.UserId]` where `TenantRole.TenantOwner`.
7. Filter `targetUserTenants` to keys in `requesterOwnedTenantIds`.
8. Paginate the filtered dictionary with the existing `Paginate` helper.
9. Serialize with existing `s_queryJsonOptions` and return `CreateSuccessResult(payload, "tenant-index")`.

Preferred implementation shape: keep the method small by introducing one private helper only if it improves clarity, for example a helper that returns `IEnumerable<KeyValuePair<string, TenantRole>>` for the visible target memberships. Do not create a new service abstraction for this narrow correction unless adjacent code already demands it.

### Authorization Rules

- Self lookup is always allowed for authenticated users.
- GlobalAdministrator lookup is allowed for any target user and returns all target-user memberships.
- TenantOwner cross-user lookup is allowed but filtered to tenants where requester role is exactly `TenantRole.TenantOwner`.
- TenantContributor and TenantReader do not grant cross-user visibility.
- Ordinary users querying another user should receive an empty successful page rather than `Forbidden`. This follows D11's result-filtering pattern and avoids leaking whether the target user has memberships elsewhere.
- Do not use JWT role claims, `name`, display names, or any user-controllable claim to decide ownership. The authenticated user identity already reaches the actor as `QueryEnvelope.UserId`, derived from `sub`.

### Files To Update

Update:

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`

Potentially update only if cheap and stable:

- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`

Do not update:

- `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs` - query contract already has `QueryType = "get-user-tenants"`, `Domain = "tenants"`, and `ProjectionType = "tenant-index"`.
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs` - route and `EntityId` mapping already provide the target user ID.
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs` - it already contains the needed `UserTenants` dictionary with user-to-tenant roles.
- `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs` - response DTO already has `TenantId`, `Name`, `Status`, and `Role`.
- `Directory.Packages.props` or any `.csproj` package references - no dependency change is required.

### Existing Tests To Adjust

`tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` already has helpers that make this story cheap to test:

- `CreateTenantIndexModel(...)` can seed many tenants and user memberships.
- `SetupTenantIndexState(...)` can return the cross-tenant index.
- `SetupGlobalAdminState(...)` and `SetupNoGlobalAdmin(...)` cover admin and non-admin paths.
- `DeserializePayload<PaginatedResult<UserTenantMembership>>(...)` already validates response payloads.

The current test named `GetUserTenants_non_admin_querying_other_user_returns_forbidden` documents the bug relative to D11. Replace it with a successful empty-result expectation for ordinary non-owner users.

Suggested new actor tests:

- `GetUserTenants_tenant_owner_querying_user_with_overlap_returns_owned_tenants_only`
- `GetUserTenants_tenant_owner_querying_user_without_overlap_returns_empty_page`
- `GetUserTenants_non_owner_querying_other_user_returns_empty_page`
- `GetUserTenants_tenant_owner_paginates_after_filtering`

When seeding tests, remember that `TenantIndexReadModel.Apply(UserAddedToTenant)` ignores memberships for unknown tenants. Create tenant entries first with `TenantCreated`, then add memberships.

### Previous Story Intelligence

Story 5.2 created `TenantIndexReadModel` as the cross-tenant fan-in read model:

- `Tenants` maps tenant ID to `TenantIndexEntry`.
- `UserTenants` maps user ID to tenant ID to `TenantRole`.
- `UserRoleChanged` updates roles, so owner visibility must use current roles from `UserTenants`, not stale role assumptions.
- The cross-tenant index is data-only; authorization belongs in query handling, not in projection Apply methods.

Story 5.3 created the query layer:

- `GetUserTenantsQuery` reads the cross-tenant index and returns `PaginatedResult<UserTenantMembership>`.
- Existing self and GlobalAdmin cases were implemented and tested.
- The audit endpoint intentionally returns 501 for admins and is handled by R5-A3, not this story.
- Query controllers are thin REST-to-MediatR translators; keep business authorization in `TenantsProjectionActor`.

Post-Epic-5 R5-A1 fixed JWT authentication pipeline wiring:

- Controller actions now receive real authenticated identities.
- Tests should keep relying on `sub` through `QueryEnvelope.UserId`.
- Do not regress `UseAuthentication()` / `UseAuthorization()` or JWT options while touching query behavior.

### Architecture And Requirements Context

This story implements architecture D11: `GetUserTenantsQuery` applies query-side result filtering based on requester scope. D11 defines this as a third authorization pattern alongside API JWT validation and command-side domain RBAC.

Relevant requirements:

- FR28: query the list of tenants a specific user belongs to, with their role in each tenant.
- FR30: list and query endpoints use cursor-based pagination with consistent ordering.
- FR33: TenantOwner has user-role management capabilities for tenants they own.
- FR34: roles do not transfer or aggregate across tenants.
- NFR5: zero cross-tenant data leaks.
- NFR10: tenant isolation and authorization logic needs branch coverage.

### Latest Technical Context

No external library or package change is required. Use the current repository versions and patterns:

- .NET SDK `10.0.103`, target `net10.0`, nullable enabled, `TreatWarningsAsErrors=true`.
- Dapr packages are centrally pinned to `1.17.7`.
- Tests use xUnit v3, Shouldly, and NSubstitute.
- Do not add package versions to project files; central package management is in `Directory.Packages.props`.

### Anti-Patterns To Avoid

- Do not keep the early non-admin `Forbidden` branch for all cross-user lookups.
- Do not return unfiltered target memberships for TenantOwner cross-user lookup.
- Do not grant cross-user lookup to TenantContributor or TenantReader.
- Do not use HTTP route logic or controller attributes to implement D11; the actor must filter the result set.
- Do not use user-controllable JWT claims for authorization decisions.
- Do not mutate `TenantIndexReadModel` in query handling.
- Do not paginate before filtering; doing so can leak page shape and skip authorized rows.
- Do not broaden this story into R5-A3 audit projection/query work.
- Do not run recursive submodule initialization or updates.

### Project Structure Notes

This is a narrow backend correction. It should not introduce new projects, packages, contracts, routes, controller methods, or DAPR components. The likely code change is localized to one actor method and actor tests.

### References

- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13-implementation-readiness-alignment.md#Proposal-3-Add-Post-Epic-D11-Correction-Story`]
- [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-13.md#Critical-Violations`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#D11-User-Search-Authorization-Scoping`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Tenant-Discovery--Query`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Role-Behavior`]
- [Source: `_bmad-output/implementation-artifacts/5-2-cross-tenant-index-projection.md#Dev-Notes`]
- [Source: `_bmad-output/implementation-artifacts/5-3-query-endpoints-and-authorization.md#Dev-Notes`]
- [Source: `_bmad-output/implementation-artifacts/post-epic-5-r5a1-tenants-jwt-auth-wiring.md#Review-Findings`]
- [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs#HandleGetUserTenantsAsync`]
- [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetUserTenantsAsync`]
- [Source: `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: `dotnet test .\tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` failed on five new D11 cross-user visibility tests before production changes.
- Green phase: focused actor tests passed after filtering change: 23 passed.
- Regression: `dotnet test .\tests\Hexalith.Tenants.Contracts.Tests\Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore` passed: 34 passed.
- Regression: `dotnet test .\tests\Hexalith.Tenants.Client.Tests\Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-restore` passed: 48 passed.
- Regression: `dotnet test .\tests\Hexalith.Tenants.Testing.Tests\Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-restore` passed: 89 passed.
- Regression: `dotnet test .\tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore` passed: 265 passed.
- Integration smoke: `dotnet test .\tests\Hexalith.Tenants.IntegrationTests\Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` passed: 13 passed.
- Full solution regression: `dotnet test .\Hexalith.Tenants.slnx --configuration Release --no-restore` timed out after 5 minutes before producing test results.
- Full integration project: `dotnet test .\tests\Hexalith.Tenants.IntegrationTests\Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore` timed out after 3 minutes before producing test results.

### Implementation Plan

- Replace the early cross-user non-admin `Forbidden` branch with a query-side visibility calculation based on the tenant index.
- Preserve full target-user membership visibility for self lookup and GlobalAdministrator lookup.
- For other cross-user requests, compute requester-owned tenant IDs from `TenantIndexReadModel.UserTenants` where the requester role is exactly `TenantRole.TenantOwner`, filter the target user's memberships to those tenant IDs, then paginate.
- Use actor tests as the Tier 1 guard because the existing controller integration fixture mocks `IQueryRouter` and does not seed actor projection state cheaply.

### Completion Notes List

- Implemented TenantOwner-scoped `GetUserTenantsQuery` filtering in `TenantsProjectionActor`.
- Non-admin cross-user lookups now return successful empty pages when the requester owns none of the target user's tenants, avoiding cross-tenant data leakage and membership existence leakage.
- Self lookup and GlobalAdministrator lookup continue to return all target-user memberships.
- Pagination now runs after authorization filtering for TenantOwner cross-user lookup.
- Replaced the old forbidden cross-user test with an empty-page expectation and added D11 actor coverage for missing target memberships, TenantOwner partial visibility, no-overlap empty result, ordinary non-owner empty result, and pagination after filtering.
- Did not add actor-state integration coverage because `TenantsQueryControllerIntegrationTests` uses a mocked `IQueryRouter`; broad DAPR-backed state seeding belongs to the existing higher-tier security/infrastructure suite.
- Added missing `Hexalith.EventStore.Contracts.Queries` imports to server test files so `QueryEnvelope` and `QueryResult` compile against the current contracts namespace.

### File List

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs`
- `_bmad-output/implementation-artifacts/post-epic-5-r5a2-get-user-tenants-scoped-authorization.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-14: Implemented D11 TenantOwner-scoped `GetUserTenantsQuery` authorization filtering, added actor regression coverage, and moved story to review.

## Story Completion Status

Story implementation complete and ready for review.
