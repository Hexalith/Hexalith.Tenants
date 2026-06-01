---
baseline_commit: 6e2b87a
---

# Story 5.8: Query Tenant Details and Tenant Users

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant user,
I want to query tenant details and the tenant's users,
so that I can inspect the current tenant state allowed by my role.

## Acceptance Criteria

1. **Given** a tenant exists and its projection has been updated
   **When** an authorized caller requests `GET /api/tenants/{tenantId}`
   **Then** the response includes tenant metadata, status, users, roles, and configuration visible to that caller
   **And** the response uses typed query DTOs rather than anonymous response shapes.

2. **Given** a tenant exists and contains users
   **When** an authorized caller requests `GET /api/tenants/{tenantId}/users`
   **Then** the response returns the tenant's users with assigned roles
   **And** the endpoint supports pagination if the user list exceeds one page.

3. **Given** the requested tenant does not exist
   **When** tenant detail or users are queried
   **Then** the API returns a safe not-found response
   **And** it does not reveal data from another tenant or internal projection keys.

4. **Given** a caller has TenantReader or higher authority for the tenant
   **When** the caller queries details or users
   **Then** read access is allowed according to tenant role behavior
   **And** no state-changing authority is implied by query access.

5. **Given** tenant detail and users query tests run
   **When** enabled, disabled, missing, empty-users, multi-page, and unauthorized cases are exercised
   **Then** tests verify filtering, response shape, status codes, and isolation.

## Tasks / Subtasks

- [x] Task 1: Reconcile the existing tenant detail/users implementation before editing (AC: #1-#5)
  - [x] Read `TenantsQueryController.GetTenantAsync`, `TenantsQueryController.GetTenantUsersAsync`, `TenantsProjectionActor.HandleGetTenantAsync`, `TenantsProjectionActor.HandleGetTenantUsersAsync`, `GetTenantQuery`, `GetTenantUsersQuery`, `TenantDetail`, `TenantMember`, `PaginatedResult<T>`, `TenantReadModel`, and the existing query tests.
  - [x] Treat the current routes as the baseline: `GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users`. Do not add duplicate endpoints, endpoint-local anonymous DTOs, offset/limit pagination, direct Redis/state-store reads, or a new query dispatch path.
  - [x] Confirm which ACs are already satisfied by existing actor, controller, and DTO serialization tests before adding new code. This story is endpoint completion and evidence hardening, not a greenfield query stack.

- [x] Task 2: Preserve tenant detail HTTP and query dispatch contract (AC: #1, #3, #4, #5)
  - [x] Keep `GET /api/tenants/{tenantId}` on `TenantsQueryController` with `[Authorize]`, identifier validation through `IsValidIdentifier`, and authenticated user identity from JWT `sub`.
  - [x] Keep `SubmitQuery` values for detail lookup: `Tenant = "system"`, `Domain = GetTenantQuery.Domain`, `AggregateId = tenantId`, `QueryType = GetTenantQuery.QueryType`, `EntityId = tenantId`, `ProjectionType = TenantProjectionRouting.ActorTypeName`.
  - [x] Ensure the successful response is the typed `TenantDetail` DTO serialized camelCase, including `tenantId`, `name`, `description`, `status`, `members`, `configuration`, and `createdAt`.
  - [x] Strengthen HTTP-level tests so successful detail responses prove typed DTO shape, query dispatch values, and no anonymous controller-side response rewriting.

- [x] Task 3: Preserve tenant users HTTP and query dispatch contract (AC: #2-#5)
  - [x] Keep `GET /api/tenants/{tenantId}/users` on `TenantsQueryController` with `[Authorize]`, identifier validation, JWT `sub`, standard page-size clamping, and controller-side cursor validation before `IMediator.Send`.
  - [x] Keep `SubmitQuery` values for users lookup: `Tenant = "system"`, `Domain = GetTenantUsersQuery.Domain`, `AggregateId = tenantId`, `QueryType = GetTenantUsersQuery.QueryType`, `EntityId = tenantId`, `ProjectionType = TenantProjectionRouting.ActorTypeName`.
  - [x] Keep request payload shape `{ cursor, pageSize }` and response shape `PaginatedResult<TenantMember>` serialized as `items`, `cursor`, and `hasMore`.
  - [x] Add or strengthen HTTP-level assertions that omitted, non-positive, valid, and oversized page sizes are bounded by `TenantQueryPaginationPolicy` and forwarded in the query payload.

- [x] Task 4: Prove actor behavior for detail DTOs, status, configuration, and member filtering (AC: #1, #3-#5)
  - [x] Verify `HandleGetTenantAsync` reads only `TenantProjectionKeyPrefix + envelope.AggregateId`; do not query `TenantIndexReadModel` or construct ad hoc state keys for detail lookup.
  - [x] Verify successful detail responses include projected tenant metadata, latest projected enabled/disabled `TenantStatus`, members with concrete roles only, configuration key/value pairs, and `CreatedAt`.
  - [x] Keep `TenantRole.Unknown`, missing/default role values, and corrupt rows out of detail members and users-list results.
  - [x] Verify `TenantReader`, `TenantContributor`, `TenantOwner`, and global administrator can read the tenant; `TenantRole.Unknown` and non-members cannot.
  - [x] Document in test names or comments that detail and users responses are projection-backed and may lag the event stream; do not claim read-after-write freshness.

- [x] Task 5: Prove tenant users pagination, cursor, and safe consistency behavior (AC: #2, #3, #5)
  - [x] Keep `HandleGetTenantUsersAsync` validating the submitted cursor before tenant state reads, so invalid or scope-mismatched cursors are not masked by missing-tenant or forbidden responses.
  - [x] Keep cursor scope binding to `TenantQueryCursorScopes.GetTenantUsers(tenantId)` and query type `GetTenantUsersQuery.QueryType`; a cursor for another tenant, query, or endpoint must return sanitized `400` ProblemDetails at the HTTP boundary or `"Invalid cursor."` at the actor boundary.
  - [x] Preserve standard page-size policy: default `20`, maximum `100`, and non-positive values reset to default.
  - [x] Verify users are ordered by user ID using ordinal comparison and cursor positions are exclusive lower bounds over the currently visible concrete-member set.
  - [x] Verify between-page membership changes follow the Story 5.6 contract: inserts before or equal to the cursor are not backfilled, inserts after the cursor can appear, removals are skipped without error, and hidden rows do not influence `items`, `cursor`, or `hasMore`.

- [x] Task 6: Preserve safe missing/unauthorized behavior and error mapping (AC: #3-#5)
  - [x] For non-admin callers, a missing tenant must be indistinguishable from an unauthorized tenant at the actor boundary: return forbidden with no payload, no page metadata, and no projection key disclosure.
  - [x] For global administrators, a missing tenant detail/users query may report not found safely, but the response body must not reveal internal projection keys, state-store names, hidden tenant IDs, raw cursor text, decoded cursor payloads, tokens, or signing material.
  - [x] Verify HTTP mapping remains RFC 7807 ProblemDetails for forbidden, not found, and invalid cursor failures.
  - [x] Do not log raw payloads, protected cursor values, decoded cursor positions, bearer tokens, tenant configuration values, or user-controlled identity claims beyond existing support-safe metadata.

- [x] Task 7: Strengthen focused tests for tenant detail/users completion evidence (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` for detail status/configuration/member DTO shape, disabled tenant detail, empty users page, users ordering, users page-size default/max/non-positive behavior, global-admin missing-users not-found behavior if not already covered, and no hidden metadata on missing/forbidden responses.
  - [x] Extend `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` for `GET /api/tenants/{tenantId}` typed payload shape and dispatch values, `GET /api/tenants/{tenantId}/users` paginated payload shape and dispatch values, page-size forwarding/clamping, safe 404/403 ProblemDetails, invalid cursor router-not-invoked behavior, and cursor scope mismatch.
  - [x] Extend `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs` only if DTO shape coverage is missing; preserve `System.Text.Json`, camelCase, and enum string serialization.
  - [x] Prefer existing helpers such as `CreateTenantReadModel`, `SetupTenantState`, `SetupGlobalAdminState`, `SetupNoGlobalAdmin`, `CreatePaginationPayload`, `DeserializePayload<T>`, `CreateRouter`, and `AssertProblemDetailsDoesNotLeakQueryData`; do not duplicate production cursor or pagination logic in tests.

- [x] Task 8: Run focused validation (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --filter FullyQualifiedName~QueryDtoSerializationTests`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executables directly and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

`GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users` already exist. They are backed by `GetTenantQuery`, `GetTenantUsersQuery`, `TenantsQueryController`, and `TenantsProjectionActor`. The dev agent should close story-specific evidence gaps and preserve current contracts rather than rebuilding the query stack.

Canonical files:

- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs`
- `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPayloadParser.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`

### Current State Of Files To Touch

`TenantsQueryController.GetTenantAsync` currently validates `tenantId`, derives `userId` from JWT `sub`, and dispatches `SubmitQuery` with `GetTenantQuery` to the tenants projection actor. It has no cursor or page-size input and should remain a thin REST adapter.

`TenantsQueryController.GetTenantUsersAsync` currently validates `tenantId`, derives `userId` from JWT `sub`, clamps standard page size, validates the submitted cursor against `GetTenantUsersQuery.QueryType` and `TenantQueryCursorScopes.GetTenantUsers(tenantId)`, then dispatches `SubmitQuery` with payload `{ cursor, pageSize }`.

`TenantsProjectionActor.HandleGetTenantAsync` currently reads `TenantReadModel` from DAPR state key `projection:tenants:{aggregateId}`. Missing state returns forbidden for non-admin callers and not found for global administrators. Existing state requires tenant membership with a concrete role or global-admin authority. Success returns `TenantDetail` with concrete members only and copies configuration from the read model.

`TenantsProjectionActor.HandleGetTenantUsersAsync` currently parses standard pagination payloads, validates the protected cursor before reading tenant state, checks tenant read authorization, filters members to concrete roles, orders by user ID through the shared `Paginate` helper, protects the next cursor, and serializes `PaginatedResult<TenantMember>`.

`TenantReadModel` is the per-tenant projection state. It updates metadata/status from tenant lifecycle events, `Members` from user membership/role events, and `Configuration` from tenant configuration events. Projection Apply methods trust events and must stay deterministic.

`TenantDetail` is the typed detail DTO. `TenantMember` is the typed membership DTO. `PaginatedResult<TenantMember>` is the tenant users response contract. Do not replace these with anonymous shapes in controllers or tests.

### Cursor And Consistency Contract

- Tenant users cursor positions are exclusive lower bounds over the currently visible concrete-member set.
- The server does not promise snapshot isolation across page requests.
- Members inserted before or equal to the cursor position after page 1 are not backfilled into later pages.
- Members inserted after the cursor position may appear later.
- Members removed after page 1 are skipped without error.
- `hasMore` and next cursor are computed only from visible concrete-role rows.
- Tenant detail and users results reflect the latest successfully projected tenant read model only; they are not source-of-truth command results and must not claim read-after-write freshness.

### Security And Authorization Guardrails

- Query-side global-admin authority comes from `GlobalAdministratorReadModel`; do not consume command-envelope `actor:globalAdmin` metadata in query handling.
- Tenant detail/users read access is allowed for `TenantReader`, `TenantContributor`, `TenantOwner`, and global administrators. It does not grant command authority.
- `TenantRole.Unknown`, missing roles, corrupt rows, and hidden users are non-privileged and must not appear in detail/users results or pagination anchors.
- A valid cursor is not authorization. The actor must still evaluate current tenant read visibility after cursor validation and before response serialization.
- Non-admin missing-tenant responses must not reveal whether the tenant exists elsewhere. Keep them forbidden/no payload at the actor boundary.
- Error bodies for invalid cursors, forbidden access, and not found must be RFC 7807 ProblemDetails at the HTTP boundary and must not contain `items`, `cursor`, `hasMore`, raw protected cursor text, decoded cursor JSON, hidden tenant IDs/names, hidden user IDs, bearer tokens, signing keys, or projection state keys.

### Story Boundaries

In scope:

- `GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users` completion evidence.
- Tenant detail typed DTO shape, projected status/configuration/members, missing/disabled/unauthorized behavior, and safe ProblemDetails.
- Tenant users pagination, ordering, page-size bounds, cursor scope validation, and concrete-role filtering.
- Focused actor, HTTP-boundary, and contract serialization tests that lock FR26, FR27, FR30, and FR31 behavior.

Out of scope:

- Adding new query endpoints or changing route names.
- Redesigning `TenantDetail`, `TenantMember`, `PaginatedResult<T>`, `GetTenantQuery`, or `GetTenantUsersQuery` unless a test exposes a story-critical defect.
- Adding offset/limit pagination or plaintext/client-editable cursors.
- Changing command authorization, aggregate RBAC, EventStore claims validation, or global-admin command metadata.
- Projection write safety, retry policy, conflict diagnostics, replay, or recovery behavior from Stories 5.1-5.4.
- User-tenants query behavior from Story 5.9 and audit query behavior from Story 5.10.
- Phase 2 Admin UI fields such as owner count, warning indicators, freshness badges, pending command state, or audit links.

### Previous Story Intelligence

Story 5.7 completed the tenant-list endpoint over the same controller, actor, pagination, cursor, and HTTP test infrastructure. Reuse its patterns for route-level payload assertions, bounded page-size forwarding, invalid cursor router short-circuiting, and safe ProblemDetails checks.

Story 5.6 established the shared pagination contract: standard endpoints default to `20`, clamp at `100`, use Data Protection backed opaque cursors, validate query type and scope, and apply cursor positions as ordinal exclusive lower bounds. Preserve that behavior for `get-tenant-users`.

Story 5.5 established query-side authorization. Global administrators can read cross-tenant query state through `GlobalAdministratorReadModel`; tenant users can read only tenants where their membership role is concrete. `TenantRole.Unknown` is non-privileged.

Stories 5.1-5.4 established durable projection write safety and diagnostics. This story consumes `TenantReadModel` projection state but must not change projection write policy.

The story automator learning from 2026-06-01 says exact current story keys are more reliable than broad searches because older same-number story artifacts remain in the repository. Use `5-8-query-tenant-details-and-tenant-users` and avoid archived legacy slugs.

### Git Intelligence

Recent relevant commits before story creation:

- `6e2b87a feat(story-5.7): Query a Paginated Tenant List`
- `c468d7b feat(story-5.6): Provide Safe Cursor-Based Pagination for Query Endpoints`
- `f94fc36 feat(story-5.5): Enforce Query-Side Authorization and Isolation`
- `419989e feat(story-5.4): Expose Projection Write Conflict Diagnostics and Recovery Evidence`
- `54376be feat(story-5.3): Persist the Tenant Audit Projection Without Silent Write Loss`

The current worktree already had an unrelated modified `_bmad-output/story-automator/orchestration-5-20260601-061130.md` before this story was created. Do not revert it as part of story 5.8.

### Latest Technical Specifics

No dependency upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300`
- ASP.NET Core Data Protection from the pinned .NET runtime stack
- DAPR SDK `1.17.9`
- MediatR `14.1.0`
- xUnit v3 `3.2.2`
- Shouldly `4.3.0`
- NSubstitute `6.0.0-rc.1`

Do not add new auth, cursor, serialization, query, telemetry, or persistence packages. Latest-version research is intentionally limited to repository-pinned versions because this story does not include dependency upgrades.

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

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.8: Query Tenant Details and Tenant Users]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5: Operators and Developers Can Query Tenant State and Audit Access]
- [Source: _bmad-output/planning-artifacts/epics.md#FR25-FR30]
- [Source: _bmad-output/planning-artifacts/prd.md#Tenant Discovery & Query]
- [Source: _bmad-output/planning-artifacts/prd.md#Role Behavior]
- [Source: _bmad-output/planning-artifacts/architecture.md#API Naming Conventions]
- [Source: _bmad-output/planning-artifacts/architecture.md#API Response Formats]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- [Source: _bmad-output/project-context.md#API Surface]
- [Source: _bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)]
- [Source: _bmad-output/implementation-artifacts/5-7-query-a-paginated-tenant-list.md#Previous Story Intelligence]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests"` initially hit MSBuild/VSTest socket restrictions; reran with `DOTNET_CLI_HOME=/tmp`, `MSBUILDDISABLENODEREUSE=1`, `NUGET_PACKAGES=/home/administrator/.nuget/packages`, local-cache restore, and direct xUnit v3 executables.
- 2026-06-01: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPolicyTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPayloadParserTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryCursorCodecTests` passed: 170 tests, 0 failures.
- 2026-06-01: `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests` passed: 59 tests, 0 failures.
- 2026-06-01: `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -class Hexalith.Tenants.Contracts.Tests.Queries.QueryDtoSerializationTests` passed: 5 tests, 0 failures.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` passed with 0 warnings and 0 errors.
- 2026-06-01 review: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests" --no-restore` was blocked by MSBuild named-pipe/socket setup (`SocketException (13): Permission denied`) before test execution.
- 2026-06-01 review: Direct xUnit v3 executables passed for server focused tests (170 tests), integration controller tests (59 tests), and contract query DTO serialization tests (5 tests), all with 0 failures.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Reconciled existing `GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users` implementation and preserved the controller, query dispatch, projection actor, DTO, cursor, and pagination contracts.
- Strengthened projection actor tests for projection-backed tenant detail DTOs, disabled status/configuration/member filtering, per-tenant projection key reads, users ordering, standard page-size policy, empty users, global-admin missing-users not found behavior, and safe no-payload error responses.
- Strengthened HTTP-boundary tests for typed tenant detail shape, tenant users paginated shape, query dispatch values, users page-size forwarding/clamping, and safe 403/404 ProblemDetails.
- Verified contract serialization coverage was already present and passed without requiring DTO changes.

### File List

- `_bmad-output/implementation-artifacts/5-8-query-tenant-details-and-tenant-users.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

### Review Findings

- [x] MEDIUM: Story File List omitted `_bmad-output/implementation-artifacts/tests/test-summary.md`, even though git shows it changed for story 5.8. Fixed by adding it to the File List.
- [x] LOW: `CreateTenantReadModel` in `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` had malformed indentation in a touched test helper. Fixed to match repository formatting expectations.
- [x] LOW: Story and sprint tracking remained in `review` after all critical review checks passed. Fixed by setting the story and sprint status to `done`.

### Checklist Validation

- [x] Story file loaded from `_bmad-output/implementation-artifacts/5-8-query-tenant-details-and-tenant-users.md`.
- [x] Story Status verified as reviewable (`review`) before review and updated to `done`.
- [x] Epic and Story IDs resolved as 5.8.
- [x] Story context located in `_bmad-output/project-context.md`.
- [x] Epic/architecture context located in `_bmad-output/planning-artifacts/architecture.md`.
- [x] Tech stack detected: .NET 10, C# latest, ASP.NET Core, MediatR, DAPR, xUnit v3, Shouldly, NSubstitute.
- [x] Docs fallback performed against Microsoft Learn for ASP.NET Core ProblemDetails/action results and System.Text.Json camelCase/string enum behavior.
- [x] Acceptance Criteria cross-checked against controller, actor, DTO, cursor, pagination, and tests.
- [x] File List reviewed and corrected for completeness.
- [x] Tests identified and mapped to ACs; no blocking test gap remains.
- [x] Code quality and security review performed on changed source-test files and relevant production query files.
- [x] Sprint status synced for `5-8-query-tenant-details-and-tenant-users`.
- [x] Story saved successfully.
- [x] Outcome: Approve after automatic fixes.

### Change Log

- 2026-06-01: Completed story 5.8 by hardening tenant detail/users actor and HTTP-boundary evidence while preserving existing production contracts.
- 2026-06-01: Senior developer review completed; fixed File List completeness, touched-test formatting, and story/sprint status sync.
