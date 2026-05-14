# Post-Epic-5 R5-A2: GetUserTenants Scoped Authorization

Status: done

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

### Review Findings

_BMAD adversarial code review — 2026-05-14. Layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor. All 8 acceptance criteria are met by the Acceptance Auditor's trace; findings below are second-order risks and policy questions._

- [x] [Review][Patch] Close empty-page timing oracle on cross-user lookups [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:228-243, tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs:216-240] — Applied. Moved the `isSelfLookup`/`canViewAllTargetTenants` computation (and therefore the `IsGlobalAdminAsync` Dapr lookup for non-self lookups) above the early-return for missing target users. Both the "target missing from `UserTenants`" branch and the "target present but no owned overlap" branch now incur the same admin Dapr round-trip, making the empty-page response timing-comparable across the two paths. Self-lookup still short-circuits the admin check (no oracle exposure, since the requester is the target). Added a `.Received(1)` assertion on `GetUserTenants_missing_target_user_returns_empty_page` as a regression guard. 265/265 server tests pass.
- [x] [Review][Defer] Cursor stability under concurrent role mutation [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:148-156] — deferred, pre-existing pagination model. If a requester's `TenantOwner` role on a cursor tenant is revoked between page fetches, `Paginate`'s lexicographic `Where(key > cursor)` may skip a newly-visible tenant or advance past a now-hidden one. Same property as `list-tenants`; widened only because cross-user lookups now go through filtered pagination.
- [x] [Review][Defer] No defense-in-depth `envelope.UserId` non-empty check at actor layer [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:228] — deferred, project-wide pattern. Controller-layer authentication is the primary guard. Consistent with `HandleGetTenantAsync`, `HandleGetTenantUsersAsync`, `HandleListTenantsAsync` — none guard against empty `envelope.UserId`. Track as a broader actor-surface hardening item.
- [x] [Review][Defer] `TenantStatus.Disabled` tenants are surfaced via TenantOwner-scoped lookup [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:258-263] — deferred, spec silent. Self and admin lookups already return `Disabled` tenants; this story preserves that. Confirm with product whether the new TenantOwner-scoped path should filter inactive/disabled tenants.
- [x] [Review][Defer] Demotion race: admin check now runs *after* the index load (reordered from pre-diff) [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:231-243] — deferred, eventual consistency. A just-demoted admin still sees full target memberships in the brief window between the two Dapr reads. Negligible operational risk.
- [x] [Review][Defer] Orphan membership in `UserTenants` but missing from `Tenants` map yields blank-name response entry [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:258-263] — deferred, pre-existing fallback (`entry?.Name ?? string.Empty`). `TenantIndexReadModel.Apply` guards drop orphan adds today, so this is defensive.
- [x] [Review][Defer] Self-lookup with stale projection may surface revoked memberships briefly [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:242-248] — deferred, read-model eventual consistency. Pre-existing.
- [x] [Review][Defer] `StringComparer.Ordinal` is the project-wide comparison for tenant/user IDs [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:113,135,146,242] — deferred, consistent throughout the actor. Canonicalization (e.g., lowercasing `sub`) is expected at the auth boundary; track as a project-wide consistency item, not a per-story patch.

#### Dismissed during triage (with rationale)

- "Auth model silently broadened — any TenantOwner can enumerate other users' memberships" — false positive. This is the stated intent of the story (AC #3-#5) and the explicit D11 architecture decision; Acceptance Auditor confirmed all anti-patterns avoided.
- "GetVisibleUserTenants returns the raw `targetUserTenants` dictionary as IEnumerable — caller may mutate" — false positive. Helper is private with a single internal caller; matches the shape of other `Paginate` sites in the same actor; no downstream contract surface.
- "Non-owner cross-user test no longer asserts authorization — only asserts empty page" — false positive. The new test seeds `user-1` as `TenantReader` of `tenant-001` (overlap with `user-2`), so an implementation that filtered by *any* role overlap rather than `TenantOwner` would return `Items.Count = 1` and fail the assertion. The test does detect the regression it must detect.
- "Removed test leaves no negative authorization test" — false positive. AC #5 explicitly requires the empty-page behavior; the negative case is `GetUserTenants_non_owner_querying_other_user_returns_empty_page`.
- "Pagination test cursor brittle to dictionary ordering" — false positive. `Paginate` applies explicit `OrderBy(keySelector, StringComparer.Ordinal)` at `TenantsProjectionActor.cs:146`; the cursor assertion is deterministic.
- "HashSet recomputed per call" — acknowledged by Blind Hunter as non-defect; query-scoped allocation is acceptable.
- "Telemetry test `using Hexalith.EventStore.Contracts.Queries` looks dead" — intentional. `QueryEnvelope`/`QueryResult` migrated namespaces and the test references them; documented in Completion Notes.
- "isSelfLookup short-circuits the admin call" — explicitly correct and faster than pre-diff.
- "isSelfLookup case-sensitive Ordinal compare on user IDs" — folded into the broader Ordinal defer above.
- "Owner querying self via `entityId=self` short-circuit", "requesterOwnedTenantIds built when no overlap", "requesterTenants live dict / deferred enumeration" — Edge Case Hunter explicitly marked as non-findings.
- "targetUserId may be empty string" — controller guarantees non-empty `UserId`; an empty key in `UserTenants` would be a projection bug, not a query bug.
- "Helper introduces an abstraction the spec flagged as optional" — Acceptance Auditor concluded the helper is warranted and matches the allowed shape (`IEnumerable<KeyValuePair<string, TenantRole>>`).
- "File List mentions Telemetry test file with only cosmetic change" — listed for traceability; not a violation.

### Change Log

- 2026-05-14: Implemented D11 TenantOwner-scoped `GetUserTenantsQuery` authorization filtering, added actor regression coverage, and moved story to review.
- 2026-05-14: BMAD adversarial code review — 1 decision-needed (timing-oracle policy), 0 patches, 7 deferred (pre-existing or out-of-scope), 13 dismissed.
- 2026-05-14: Applied review patch — closed empty-page timing oracle by moving the admin lookup ahead of the early-return for missing target users; added regression assertion. Story → done.

## Story Completion Status

Story implementation complete and ready for review.
