---
baseline_commit: 451abc9
---

# Story 5.9: Query the Tenants a User Belongs To

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer or administrator,
I want to query the list of tenants a user belongs to,
so that user access can be reviewed without scanning every tenant manually.

## Acceptance Criteria

1. **Given** a user belongs to one or more tenants
   **When** an authorized caller requests `GET /api/users/{userId}/tenants`
   **Then** the response returns each visible tenant with the user's role in that tenant
   **And** results are ordered consistently for pagination.

2. **Given** the requester asks for their own tenant memberships
   **When** query-side authorization is evaluated
   **Then** the requester can see their own allowed membership rows
   **And** rows outside their authorized scope are not returned.

3. **Given** a TenantOwner queries another user's tenant memberships
   **When** the target user has memberships in tenants the owner does and does not control
   **Then** only memberships visible through the owner's tenant scope are returned
   **And** memberships in other tenants are excluded without leaking their existence.

4. **Given** a global administrator queries a user's tenant memberships
   **When** query-side authorization is evaluated
   **Then** the global administrator can see memberships across tenants
   **And** the result still uses pagination and stable ordering.

5. **Given** user-tenants query tests run
   **When** self, tenant-owner, global-admin, missing-user, no-membership, and cross-tenant cases are exercised
   **Then** tests prove row-level filtering and zero cross-tenant leakage.

## Tasks / Subtasks

- [x] Task 1: Reconcile the existing user-tenants implementation before editing (AC: #1-#5)
  - [x] Read `TenantsQueryController.GetUserTenantsAsync`, `TenantsProjectionActor.HandleGetUserTenantsAsync`, `GetVisibleUserTenants`, `GetUserTenantsQuery`, `UserTenantMembership`, `PaginatedResult<T>`, `TenantIndexReadModel`, `TenantIndexEntry`, `TenantQueryCursorCodec`, `TenantQueryPaginationPolicy`, and the current query tests.
  - [x] Treat the existing `GET /api/users/{userId}/tenants` route as the baseline. Do not add a duplicate endpoint, direct state-store reads from the controller, a new query bus, anonymous public response shapes, offset/limit pagination, or plaintext/client-editable cursors.
  - [x] Confirm which ACs are already satisfied by current actor and cursor tests before adding code. This story is endpoint completion and evidence hardening over an existing surface, not a greenfield query stack.

- [x] Task 2: Preserve the HTTP contract and SubmitQuery dispatch shape (AC: #1, #2, #4, #5)
  - [x] Keep route `GET /api/users/{userId}/tenants` on `TenantsQueryController` with `[Authorize]`, route identifier validation through `IsValidIdentifier`, and authenticated requester identity from JWT `sub`.
  - [x] Keep standard page-size clamping before routing: default `20`, maximum `100`, and non-positive values reset to default through `TenantQueryPaginationPolicy`.
  - [x] Keep controller-side cursor validation before `IMediator.Send` using `GetUserTenantsQuery.QueryType` and `TenantQueryCursorScopes.GetUserTenants(authenticatedUserId, userId)`.
  - [x] Keep `SubmitQuery` values: `Tenant = "system"`, `Domain = GetUserTenantsQuery.Domain`, `AggregateId = "index"`, `QueryType = GetUserTenantsQuery.QueryType`, `EntityId = userId`, `UserId = authenticatedUserId`, `ProjectionType = TenantProjectionRouting.ActorTypeName`.
  - [x] Add or strengthen HTTP-level assertions that the successful response is `PaginatedResult<UserTenantMembership>` serialized as camelCase `items`, `cursor`, and `hasMore`, with item fields `tenantId`, `name`, `status`, and `role`.

- [x] Task 3: Preserve and prove query-side authorization rules (AC: #2-#5)
  - [x] For self lookups, allow the requester to see their own concrete membership rows across tenants in `TenantIndexReadModel.UserTenants[requesterUserId]`.
  - [x] For TenantOwner cross-user lookups, return only target-user memberships whose tenant ID is also present in the requester's membership map with `TenantRole.TenantOwner`.
  - [x] For global administrators, use `GlobalAdministratorReadModel` through `IsGlobalAdminAsync` and allow visibility across all target-user concrete membership rows.
  - [x] Treat `TenantRole.Unknown`, missing/default roles, requester non-owner roles, target-user hidden roles, and corrupt membership rows as non-privileged. They must not appear in results and must not confer owner authority.
  - [x] Return an empty page for missing target users, missing index state, no memberships, no overlap, or no authorized rows; do not return errors that allow user or tenant enumeration.

- [x] Task 4: Preserve and prove pagination, ordering, cursor, and consistency behavior (AC: #1, #4, #5)
  - [x] Order results by tenant ID using ordinal comparison after authorization filtering and orphan filtering.
  - [x] Keep cursor positions as exclusive lower bounds over the currently visible and materialized tenant ID set.
  - [x] Scope cursors by both requester and target user: `requester:{requesterUserId}|target-user:{targetUserId}` after the production escaping rules in `TenantQueryCursorScopes`.
  - [x] Reject cursors generated for another requester, target user, query type, endpoint, or Data Protection key with sanitized `400` ProblemDetails at the HTTP boundary or `"Invalid cursor."` at the actor boundary.
  - [x] Preserve the Story 5.6 consistency contract: no snapshot isolation promise; removals after page 1 are skipped; inserts before or equal to the cursor are not backfilled; inserts after the cursor may appear.
  - [x] Ensure `hasMore` and next cursor are computed only from authorized, concrete-role, existing-tenant rows.

- [x] Task 5: Preserve tenant-index projection semantics and safe orphan handling (AC: #1, #5)
  - [x] Use `TenantIndexReadModel.UserTenants` as the membership source and `TenantIndexReadModel.Tenants` as the tenant metadata source. Do not read per-tenant detail projections for this endpoint.
  - [x] Return `UserTenantMembership` rows containing tenant ID, tenant name, tenant status, and the target user's role in that tenant.
  - [x] Include disabled tenant status when the visible tenant exists and the user is authorized to see the row; disabled status is not a reason to hide historical/current membership.
  - [x] Filter orphan membership rows where `UserTenants` references a tenant ID absent from `Tenants`. Public responses must not include orphan diagnostics, raw projection keys, or placeholder tenant names.
  - [x] Keep orphan diagnostics support-safe: log correlation, query type, requester user ID, target user ID, and orphan tenant ID only after cursor validation succeeds, and avoid repeated log floods for the same actor lifetime.

- [x] Task 6: Strengthen focused tests for user-tenants completion evidence (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` for self lookup, owner overlap, owner no-overlap, non-owner cross-user empty page, global-admin cross-user lookup, missing target user, missing index, no memberships, disabled tenant status, unknown roles, orphan memberships, stable ordering, pagination after filtering, and between-page changes.
  - [x] Extend `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` for successful `GET /api/users/{userId}/tenants` response shape, query dispatch values, page-size forwarding/clamping, invalid route ID, unauthorized missing `sub`, invalid cursor router-not-invoked behavior, query-type mismatch, requester-scope mismatch, and target-user-scope mismatch.
  - [x] Extend `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs` for `UserTenantMembership` and `PaginatedResult<UserTenantMembership>` camelCase shape and enum string serialization if coverage is missing.
  - [x] Prefer existing helpers such as `CreateTenantIndexModel`, `SetupTenantIndexState`, `SetupGlobalAdminState`, `SetupNoGlobalAdmin`, `CreatePaginationPayload`, `DeserializePayload<T>`, `CreateRouter`, and `AssertProblemDetailsDoesNotLeakQueryData`; do not duplicate production cursor or pagination logic in tests.

- [x] Task 7: Preserve safe failures and leakage guardrails (AC: #3, #5)
  - [x] Error bodies for invalid cursors, forbidden/unauthorized cases, and invalid identifiers must be RFC 7807 ProblemDetails where the existing pipeline maps them that way.
  - [x] Public responses must not include raw protected cursors submitted by the client, decoded cursor JSON, signing material, bearer tokens, hidden tenant IDs/names, hidden user IDs, raw projection payloads, DAPR state keys, or orphan diagnostics.
  - [x] A valid cursor is not authorization. Actor handling must still evaluate current requester/target visibility after cursor validation and before response serialization.
  - [x] Cross-user missing-target and filtered-no-overlap cases should remain body-uniform empty pages, and the admin check should not create obvious timing differences that reintroduce user enumeration.

- [x] Task 8: Run focused validation (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --filter FullyQualifiedName~QueryDtoSerializationTests`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executables directly and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

`GET /api/users/{userId}/tenants` already exists. It is backed by `GetUserTenantsQuery`, `TenantsQueryController.GetUserTenantsAsync`, `TenantsProjectionActor.HandleGetUserTenantsAsync`, `TenantIndexReadModel`, and `UserTenantMembership`. The dev agent should reconcile the current code, close endpoint-specific evidence gaps, and preserve established contracts rather than rebuilding the query stack.

Canonical files:

- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs`
- `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPayloadParser.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexEntry.cs`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`

### Current State Of Files To Touch

`TenantsQueryController.GetUserTenantsAsync` currently validates `userId`, derives the requester from JWT `sub`, clamps standard page size, validates submitted cursors against `GetUserTenantsQuery.QueryType` and requester+target scope, then dispatches `SubmitQuery` to the tenant-index projection actor. Keep it a thin REST adapter; query authorization and row filtering stay in the actor.

`TenantsProjectionActor.HandleGetUserTenantsAsync` currently resolves the target user from `envelope.EntityId`, loads `TenantIndexReadModel` from the shared tenant index key, checks global-admin status before early missing-target returns, validates the cursor, filters visible memberships, filters orphan tenant references, logs repair diagnostics, paginates by tenant ID, protects the next cursor, and serializes `PaginatedResult<UserTenantMembership>`.

`TenantIndexReadModel` maintains two relevant maps: `Tenants[tenantId] = TenantIndexEntry(name, status)` for tenant metadata and `UserTenants[userId][tenantId] = TenantRole` for membership. It ignores membership events for tenants absent from the index and removes empty user membership maps after removals.

`UserTenantMembership` is the public DTO for this endpoint: tenant ID, name, status, and role. Do not replace it with `TenantSummary`, anonymous DTOs, or controller-local response shapes.

`TenantQueryCursorCodec` protects cursor payloads with ASP.NET Core Data Protection. User-tenants scopes include both requester and target user and escape `\`, `|`, and `:` inside caller-controlled segments. Do not weaken scope construction or expose decoded cursor positions.

### Cursor And Consistency Contract

- User-tenants cursor positions are exclusive lower bounds over the currently authorized and materialized tenant ID set.
- The server does not promise snapshot isolation across page requests.
- Rows inserted before or equal to the cursor position after page 1 are not backfilled into later pages.
- Rows inserted after the cursor position may appear later.
- Rows removed after page 1 are skipped without error.
- Requester role changes between pages are applied to the current page request; a cursor does not preserve old authorization.
- `hasMore` and next cursor are computed only from rows that are visible to the requester and present in `TenantIndexReadModel.Tenants`.
- Results reflect the latest successfully projected tenant index only; do not claim source-of-truth freshness or read-after-write behavior.

### Security And Authorization Guardrails

- Query-side global-admin authority comes from `GlobalAdministratorReadModel`; do not consume command-envelope `actor:globalAdmin` metadata in query handling.
- Self lookup and global-admin lookup can view all concrete memberships for the target user.
- TenantOwner cross-user lookup can view only tenants where the requester currently has `TenantOwner`.
- `TenantReader` and `TenantContributor` do not confer cross-user lookup authority. For another user's memberships, they should see an empty page unless they also own an overlapping tenant.
- `TenantRole.Unknown`, missing/default role values, corrupt rows, hidden tenants, and orphan memberships are non-privileged and must not appear in results or pagination anchors.
- Missing target user, no memberships, no authorized overlap, and missing index all return the standard empty page shape instead of revealing existence through status codes.
- Do not log raw payloads, protected cursor values, decoded cursor positions, bearer tokens, signing keys, tenant names, configuration values, or user-controlled identity claims beyond existing support-safe metadata.

### Story Boundaries

In scope:

- `GET /api/users/{userId}/tenants` completion evidence for FR28 and FR30.
- Response shape, query dispatch values, row-level filtering, self lookup, TenantOwner overlap, global-admin visibility, missing/no-membership empty pages, stable ordering, and standard pagination.
- Actor, HTTP-boundary, and contract serialization tests that prove row filtering and zero cross-tenant leakage.
- Orphan membership filtering as a defense against projection drift, with support-safe diagnostics only.

Out of scope:

- Adding new routes or changing route names.
- Redesigning `TenantIndexReadModel`, `UserTenantMembership`, `PaginatedResult<T>`, or `GetUserTenantsQuery` unless tests expose a story-critical defect.
- Offset/limit pagination, plaintext cursors, client-editable cursors, or cursor lifetimes beyond the current Data Protection key behavior.
- Command authorization, aggregate RBAC, EventStore claims validation, or global-admin command metadata.
- Projection write safety, retry policy, conflict diagnostics, replay, or recovery behavior from Stories 5.1-5.4.
- Tenant detail/users behavior from Story 5.8 and audit query behavior from Story 5.10.
- Phase 2 Admin UI fields such as owner count, warning indicators, freshness badges, pending command state, or audit links.

### Previous Story Intelligence

Story 5.8 completed and hardened tenant detail and tenant users over the same controller, actor, DTO, cursor, pagination, HTTP test, and contract serialization infrastructure. Reuse its pattern: preserve existing route/query contracts, strengthen typed payload assertions, and prove safe ProblemDetails without rebuilding the stack.

Story 5.7 established tenant-list endpoint proof over `TenantIndexReadModel` and `PaginatedResult<TenantSummary>`. Reuse its tenant-index ordering and empty-page patterns, but use `UserTenantMembership` for this story because the endpoint must include the target user's role.

Story 5.6 established the shared pagination contract: standard endpoints default to `20`, clamp at `100`, use Data Protection backed opaque cursors, validate query type and scope, and apply cursor positions as ordinal exclusive lower bounds after visibility filtering.

Story 5.5 established query-side authorization. Global administrators can read cross-tenant query state through `GlobalAdministratorReadModel`; tenant users can read their own tenant memberships; TenantOwners can inspect another user's memberships only for owned tenants. `TenantRole.Unknown` is non-privileged.

Stories 5.1-5.4 established durable projection write safety and diagnostics. This story consumes shared tenant-index projection state but must not change projection write policy.

The story automator learning from 2026-06-01 says exact current story keys are more reliable than broad searches because older same-number story artifacts remain in the repository. Use `5-9-query-the-tenants-a-user-belongs-to` and avoid archived legacy slugs.

### Git Intelligence

Recent relevant commits before story creation:

- `451abc9 feat(story-5.8): Query Tenant Details and Tenant Users`
- `b766ca8 feat(story-5.8): Implement query for tenant details and users`
- `6e2b87a feat(story-5.7): Query a Paginated Tenant List`
- `c468d7b feat(story-5.6): Provide Safe Cursor-Based Pagination for Query Endpoints`
- `f94fc36 feat(story-5.5): Enforce Query-Side Authorization and Isolation`

The current worktree already had an unrelated modified `_bmad-output/story-automator/orchestration-5-20260601-061130.md` before this story was created. Do not revert it as part of story 5.9.

### Latest Technical Specifics

No dependency upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300`
- ASP.NET Core Data Protection from the pinned .NET runtime stack
- DAPR SDK `1.17.9`
- MediatR `14.1.0`
- xUnit v3 `3.2.2`
- Shouldly `4.3.0`
- NSubstitute `6.0.0-rc.1`

Do not add new auth, cursor, serialization, query, logging, telemetry, or persistence packages. Latest-version research is intentionally limited to repository-pinned versions because this story does not include dependency upgrades.

### Project Structure Notes

- Query contracts and response DTOs belong under `src/Hexalith.Tenants.Contracts/Queries`.
- Runtime query handling belongs in `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`.
- REST adapters belong in `src/Hexalith.Tenants/Controllers`.
- Cursor and pagination helpers belong in `src/Hexalith.Tenants/Queries`.
- Read models remain in `src/Hexalith.Tenants.Server/Projections`; do not move EventStore-discovered projection/read-model types out of `Hexalith.Tenants.Server`.
- Server query actor tests live in `tests/Hexalith.Tenants.Server.Tests/Projections`.
- Cursor and pagination helper tests live in `tests/Hexalith.Tenants.Server.Tests/Queries`.
- HTTP boundary tests live in `tests/Hexalith.Tenants.IntegrationTests`.
- Contract DTO serialization tests live in `tests/Hexalith.Tenants.Contracts.Tests/Queries`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.9: Query the Tenants a User Belongs To]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5: Operators and Developers Can Query Tenant State and Audit Access]
- [Source: _bmad-output/planning-artifacts/epics.md#FR25-FR30]
- [Source: _bmad-output/planning-artifacts/prd.md#Tenant Discovery & Query]
- [Source: _bmad-output/planning-artifacts/prd.md#Role Behavior]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/project-context.md#API Surface]
- [Source: _bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)]
- [Source: _bmad-output/implementation-artifacts/5-8-query-tenant-details-and-tenant-users.md#Previous Story Intelligence]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Reconciled existing controller, actor, cursor, pagination, DTO, read-model, and test code. Existing production endpoint/actor behavior already satisfied the route, dispatch, authorization, cursor, pagination, orphan-filtering, and safe-failure contracts; implementation work focused on evidence hardening.
- 2026-06-01: `dotnet test` builds succeeded, but VSTest execution aborted in the sandbox with `System.Net.Sockets.SocketException (13): Permission denied` while opening the test platform socket. Per story guidance, validation was completed with the built xUnit v3 test executables.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added contract serialization evidence for `UserTenantMembership` and `PaginatedResult<UserTenantMembership>` camelCase shape with string enum values.
- Added actor evidence for missing index, empty existing memberships, stable tenant-ID ordering with membership fields, and invalid enum-role filtering without owner authority.
- Added HTTP boundary evidence for `/api/users/{userId}/tenants`: missing auth, authenticated identity without `sub`, invalid route ID, successful `PaginatedResult<UserTenantMembership>` response shape, SubmitQuery dispatch values, page-size clamping, and valid cursor forwarding.
- Preserved the existing production controller and actor implementation; no runtime code changes were required.
- Validation passed: direct xUnit v3 executable runs for contract, server, and integration focused tests, plus `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.

### File List

- tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs
- tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs

### Change Log

- 2026-06-01: Hardened Story 5.9 completion evidence for user-tenants query serialization, actor authorization/filtering edge cases, and HTTP routing/dispatch behavior.
- 2026-06-01: Senior developer review completed; no application-source defects remained, validation rerun, and story marked done.

## Senior Developer Review (AI)

Reviewer: Jerome on 2026-06-01

### Review Scope

- Checked story acceptance criteria and completed tasks against `GET /api/users/{userId}/tenants` controller dispatch, actor row-level filtering, cursor scope binding, pagination behavior, DTO serialization, and focused test evidence.
- Reviewed changed application test files:
  - `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`
  - `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
  - `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- Confirmed the story File List matches the changed application source/test files. `_bmad-output` tracking artifacts are excluded from application code review per workflow instructions.

### Findings

- No critical, high, or medium application-source defects found.
- Verified AC #1-#5 are covered by existing production code plus the added contract, actor, and HTTP boundary tests.
- Verified task claims for route preservation, `SubmitQuery` dispatch shape, row-level authorization, stable tenant-ID pagination, cursor scoping, orphan filtering, safe cursor rejection, and focused validation.

### Validation

- `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests" --no-restore -m:1 -nr:false` compiled, then VSTest aborted on sandbox socket setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryCursorCodecTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPolicyTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPayloadParserTests` passed: 174 total, 0 failed.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests --no-restore -m:1 -nr:false` compiled, then VSTest aborted on sandbox socket setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests` passed: 70 total, 0 failed.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --filter FullyQualifiedName~QueryDtoSerializationTests --no-restore -m:1 -nr:false` compiled, then VSTest aborted on sandbox socket setup: `System.Net.Sockets.SocketException (13): Permission denied`.
- `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.Queries.QueryDtoSerializationTests` passed: 7 total, 0 failed.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` passed: 0 warnings, 0 errors.
