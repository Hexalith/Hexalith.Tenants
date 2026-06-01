---
baseline_commit: 419989e
---

# Story 5.5: Enforce Query-Side Authorization and Isolation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a security-conscious platform owner,
I want query endpoints to filter results by requester scope,
so that tenant data is never exposed across tenant or role boundaries.

## Acceptance Criteria

1. **Given** a caller has no membership or global-admin authority for a tenant
   **When** the caller requests tenant details, users, user memberships, or audit data
   **Then** unauthorized rows are not returned
   **And** the response does not reveal hidden tenant data through errors or pagination metadata.

2. **Given** a TenantReader queries their own tenant
   **When** query-side authorization is evaluated
   **Then** read-only tenant detail and user-list access is allowed
   **And** no state-changing command authority is granted by the query path.

3. **Given** a TenantOwner queries scoped user access
   **When** the target user has memberships across multiple tenants
   **Then** only rows for tenants controlled by the owner are returned
   **And** rows from other tenants are absent without disclosure.

4. **Given** a global administrator queries tenant state or audit data
   **When** query-side authorization is evaluated
   **Then** cross-tenant query visibility is allowed according to global-admin policy
   **And** the response still uses safe DTOs, cursor tokens, and Problem Details.

5. **Given** cross-tenant isolation tests run
   **When** query endpoints, projections, cursors, and error bodies are exercised across multiple tenants and users
   **Then** tests verify zero cross-tenant data leaks
   **And** coverage includes unauthorized, partially authorized, and global-admin cases.

## Tasks / Subtasks

- [x] Task 1: Reconcile the current query authorization baseline before editing (AC: #1-#5)
  - [x] Read `TenantsProjectionActor.ExecuteQueryAsync` and all five handlers; preserve the existing `QueryResult` adapter contract, `QueryAdapterFailureReason.Forbidden`, cancellation behavior, and metric/span emission.
  - [x] Read `TenantsQueryController`; keep it a thin REST adapter that validates route/query input, derives `sub`, validates protected cursors, and dispatches `SubmitQuery`.
  - [x] Read query contracts and DTOs under `src/Hexalith.Tenants.Contracts/Queries`; do not add anonymous response shapes or endpoint-local DTOs.
  - [x] Confirm no state writes occur from query handlers; query paths may read projection state only and must not call command dispatch, projection persistence, or `SaveStateAsync`.
  - [x] Do not replace `TenantsProjectionActor`, `TenantQueryCursorCodec`, EventStore `SubmitQuery`, or the existing projection read models with a new repository, direct Redis client, or custom query bus.

- [x] Task 2: Lock the query authorization matrix in production code (AC: #1-#4)
  - [x] `GetTenant` and `GetTenantUsers`: allow any member of the target tenant, including `TenantReader`, and allow global administrators from `projection:global-administrators:singleton`.
  - [x] `ListTenants`: global administrators see all tenant-index rows; non-admin callers see only tenant IDs present in `TenantIndexReadModel.UserTenants[requesterUserId]`.
  - [x] `GetUserTenants`: self lookups see the requester's own memberships; TenantOwner cross-user lookups see only target memberships for tenants where the requester is currently `TenantOwner`; global administrators see all target memberships.
  - [x] `GetTenantAudit`: only global administrators can read audit rows.
  - [x] Use ordinal, case-sensitive user and tenant ID comparison everywhere; do not case-fold `sub`, tenant IDs, or dictionary keys.
  - [x] Treat `TenantRole.Unknown` and missing roles as non-privileged; do not let unknown/default enum values satisfy owner or reader checks.

- [x] Task 3: Close row, error, and pagination disclosure paths (AC: #1, #3, #5)
  - [x] Filter authorization before pagination; `items`, `cursor`, and `hasMore` must be computed only from the authorized visible set.
  - [x] Do not return cursors that encode or advance from hidden tenant IDs, hidden user IDs, tenant names, audit event IDs, or raw projection positions.
  - [x] Keep submitted cursor validation scope-bound to query type plus user/tenant/filter scope; a cursor from one user, tenant, target user, query type, or audit filter must fail safely.
  - [x] Direct tenant detail/user queries must not disclose hidden tenant names, member counts, configuration keys, audit event IDs, or next-page state through `ProblemDetails`, logs, or pagination metadata.
  - [x] If a direct tenant lookup cannot prove the caller is authorized, fail closed with the existing sanitized query failure path; do not add a helpful "tenant exists but you are not a member" detail.
  - [x] Continue filtering audit entries by `TenantAuditEntry.TenantId == envelope.AggregateId` as defense in depth against projection corruption.

- [x] Task 4: Preserve safe global-admin and projection semantics (AC: #4)
  - [x] Query-side global-admin authority comes from `GlobalAdministratorReadModel`, not from client-submitted route/query values or cursor payloads.
  - [x] Do not consume `actor:globalAdmin` or command-envelope extension metadata in query handlers.
  - [x] Keep `GlobalAdministratorProjectionHandler` restricted to tenant `system` and aggregate `global-administrators`.
  - [x] Keep global-admin responses on the same safe DTOs: `TenantDetail`, `TenantMember`, `TenantSummary`, `UserTenantMembership`, `TenantAuditEntry`, and `PaginatedResult<T>`.
  - [x] Keep 403 responses mapped through EventStore query failure `ProblemDetails`; no raw actor exception messages, payload JSON, tokens, or stack traces.

- [x] Task 5: Strengthen focused isolation tests (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` for unauthorized, partially authorized, TenantReader, TenantOwner, global-admin, stale-membership, orphan-index, and corrupted-audit-row cases.
  - [x] Add explicit assertions that hidden rows do not affect `items`, `cursor`, `hasMore`, response shape, or logged public diagnostics.
  - [x] Cover direct tenant detail and tenant-users authorization for `TenantReader`, `TenantContributor`, `TenantOwner`, non-member, and global-admin callers.
  - [x] Cover `GetUserTenants` where the target user belongs to at least three tenants and the requester owns only a subset.
  - [x] Cover malformed or missing `UserId` on role-sensitive query envelopes; it must return forbidden before projection state access.
  - [x] Cover audit authorization and tenant-ID filtering: non-admin gets 403 before audit state read; global admin does not receive mismatched-tenant audit rows.
  - [x] Keep tests on public/internal production surfaces; do not call private helpers by reflection.

- [x] Task 6: Strengthen HTTP boundary tests (AC: #1, #4, #5)
  - [x] Extend `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` for 403 and invalid-cursor `ProblemDetails` bodies.
  - [x] Assert forbidden and bad-cursor bodies do not contain `items`, `cursor`, `hasMore`, tenant names, member user IDs, audit event IDs, raw cursor payloads, bearer tokens, or signing keys.
  - [x] Assert controller cursor rejection happens before `IQueryRouter.RouteQueryAsync`.
  - [x] Preserve existing JWT authentication and `eventstore:tenant=system` authorization behavior.

- [x] Task 7: Run focused validation (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryCursorCodecTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executable directly and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

The query surface already exists from the legacy Epic 5 query endpoint story and later hardening work. Treat this story as authorization/isolation hardening over the existing implementation, not as a rewrite.

Canonical files:

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPayloadParser.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`
- `src/Hexalith.Tenants.Contracts/Queries/*.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`

`TenantsProjectionActor` currently dispatches `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`. It already has protected cursor handling, projection-state reads through `DaprClient`, global-admin checks against `projection:global-administrators:singleton`, and query duration telemetry. Preserve those seams.

### Current State Of Files To Touch

`TenantsProjectionActor.ExecuteQueryAsync` currently rejects role-sensitive queries with missing/blank `UserId` before state access and returns `QueryAdapterFailureReason.Forbidden`. Keep that fail-closed behavior and its safe log event `1904`.

`HandleGetTenantAsync` and `HandleGetTenantUsersAsync` currently load `TenantReadModel` from `projection:tenants:{tenantId}`, return `Tenant not found` when the read model is absent, then allow members or global administrators. Review this path against AC1: hidden data must not be disclosed through error differences or response bodies. If changes are needed, prefer a fail-closed policy over adding descriptive errors.

`HandleListTenantsAsync` currently loads `TenantIndexReadModel`, checks global-admin state, filters non-admin rows by requester membership, then paginates. Keep the ordering: visibility set first, pagination second.

`HandleGetUserTenantsAsync` currently supports self lookup, global-admin lookup, and TenantOwner scoped cross-user lookup. It filters orphan memberships before response materialization and logs event `1903` only after cursor validation. Preserve the no-public-diagnostics behavior for orphan rows.

`HandleGetTenantAuditAsync` currently checks global-admin state before audit state access, then filters entries by requested tenant ID, date range, category, and cursor. Preserve the "non-admin 403 before audit state read" behavior.

`TenantsQueryController` currently validates route identifiers, derives user identity from JWT `sub`, validates protected cursors at the controller boundary, and dispatches `SubmitQuery`. Keep query authorization and row filtering out of controller branching; the controller must not become a second policy engine.

### Authorization Matrix

- Any tenant member (`TenantReader`, `TenantContributor`, `TenantOwner`) may read their own tenant detail and user list.
- `TenantReader` read access does not imply command authority. Do not alter command validators, aggregate RBAC, command claims, or command-envelope extensions in this story.
- `TenantOwner` may inspect another user's tenant memberships only for tenants where the requester is currently an owner.
- Global administrators may list all tenants, query any tenant detail/user list, query any user's memberships, and query tenant audit history.
- Non-members receive no rows for list/user-membership views and no tenant detail, user list, or audit payload for direct tenant views.

### Cursor And Metadata Rules

Cursors are signed, opaque, and scope-bound by `TenantQueryCursorCodec`. They must never be replaced with offset/limit pagination, plaintext tenant IDs, plaintext user IDs, audit event IDs, or client-editable JSON.

For list/user-tenants endpoints, filtering must happen before pagination so `hasMore` and `cursor` do not reveal hidden rows. For direct tenant-users and audit endpoints, cursor scope must include the target tenant and relevant audit filters. Invalid cursors must return sanitized `400` `ProblemDetails` and must not invoke the query router.

### Story Boundaries

In scope:

- Query-side authorization and row filtering.
- Safe error and pagination metadata behavior.
- Cross-tenant isolation tests for actor and HTTP boundary behavior.
- Query-only proof that `TenantReader` can read but gains no command authority.

Out of scope:

- Adding new query endpoints or changing route names.
- Redesigning query response DTOs.
- Changing command authorization, aggregate RBAC, or EventStore claims validation.
- Cursor key-ring production persistence; this remains deferred in `Program.cs`.
- Projection write safety, retry policy, telemetry dimensions, or recovery diagnostics from Stories 5.1-5.4.
- UI/FrontComposer behavior.

### Previous Story Intelligence

Story 5.1 established that Epic 5 work must reuse the existing projection write machinery rather than creating duplicate persistence paths.

Story 5.2 reinforced that tenant index fan-in writes are guarded separately from query filtering. Do not mix query authorization with projection write conflict handling.

Story 5.3 established tenant audit persistence and deterministic audit ordering. Audit query authorization must consume that read model safely and keep mismatched-tenant rows out of responses.

Story 5.4 added support-safe projection write diagnostics and telemetry. Do not leak raw payloads, tokens, secrets, tenant names from hidden rows, or PII while adding query isolation evidence.

The archived legacy story `archive/legacy-story-slugs-20260601/5-3-query-endpoints-and-authorization.md` created the first query endpoint implementation. Several legacy assumptions have changed: audit is no longer 501-only, cursor tokens are protected, and this story must harden row/error/cursor isolation rather than recreate the endpoint layer.

### Git Intelligence

Recent relevant commits before story creation:

- `419989e feat(story-5.4): Expose Projection Write Conflict Diagnostics and Recovery Evidence`
- `54376be feat(story-5.3): Persist the Tenant Audit Projection Without Silent Write Loss`
- `7ddd400 feat(story-5.2): Persist the Shared Tenant Index Projection Without Silent Write Loss`
- `9345bf0 feat(story-5.1): Persist Per-Tenant Detail Projections Without Silent Write Loss`
- `f67eecd docs(retro): finalize Epic 4 retrospective`

The current worktree also has an unrelated modified `_bmad-output/story-automator/orchestration-5-20260601-061130.md`; do not revert it as part of this story.

### Latest Technical Specifics

No dependency upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300`
- DAPR SDK `1.17.9`
- MediatR `14.1.0`
- xUnit v3 `3.2.2`
- Shouldly `4.3.0`
- NSubstitute `6.0.0-rc.1`

Do not add new auth, cursor, serialization, or telemetry packages.

### Project Structure Notes

- Query contracts and response DTOs belong under `src/Hexalith.Tenants.Contracts/Queries`.
- Runtime query handling belongs in `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`.
- REST adapters belong in `src/Hexalith.Tenants/Controllers`.
- Cursor and pagination helpers belong in `src/Hexalith.Tenants/Queries`.
- Read models remain in `src/Hexalith.Tenants.Server/Projections`; do not move EventStore-discovered projection types out of `Hexalith.Tenants.Server`.
- Server query actor tests currently live in `tests/Hexalith.Tenants.Server.Tests/Projections`.
- HTTP boundary tests live in `tests/Hexalith.Tenants.IntegrationTests`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.5: Enforce Query-Side Authorization and Isolation]
- [Source: _bmad-output/planning-artifacts/epics.md#FR25-FR34]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- [Source: _bmad-output/project-context.md#Authorization (RBAC — Role-Based Access Control)]
- [Source: _bmad-output/project-context.md#API Surface]
- [Source: _bmad-output/project-context.md#Testing Rules]
- [Source: _bmad-output/implementation-artifacts/5-4-expose-projection-write-conflict-diagnostics-and-recovery-evidence.md]
- [Source: _bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/5-3-query-endpoints-and-authorization.md]
- [Source: src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs]
- [Source: src/Hexalith.Tenants/Controllers/TenantsQueryController.cs]
- [Source: tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs]
- [Source: tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryCursorCodecTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests"` built successfully with `--no-restore -m:1 -nr:false`, then VSTest aborted on sandbox socket setup (`SocketException (13): Permission denied`).
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryCursorCodecTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPolicyTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPayloadParserTests` passed: 146 total, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` built successfully with `--no-restore -m:1 -nr:false`, then VSTest aborted on sandbox socket setup (`SocketException (13): Permission denied`).
- `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests` passed: 40 total, 0 failed, 0 skipped.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nr:false` passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Reconciled the existing actor/controller/query contract baseline and kept query handling on the existing projection actor, cursor codec, EventStore `SubmitQuery`, and shared query DTOs.
- Hardened query authorization so `TenantRole.Unknown` is non-privileged for direct tenant reads, tenant lists, and scoped user-membership reads.
- Changed direct tenant detail/user-list misses to fail closed for non-admin callers while preserving global-admin `Tenant not found` behavior.
- Scope-bound `GetUserTenants` cursors to both requester and target user to prevent cursor reuse across requester visibility sets.
- Review auto-fixes filter `TenantRole.Unknown` rows out of tenant detail and tenant-users responses, so corrupt/default roles do not appear as visible memberships.
- Review auto-fixes validate list-tenants cursors before empty-index responses and get-user-tenants cursors before empty missing-target responses.
- Added actor and HTTP boundary tests covering concrete roles, unknown-role filtering, cross-requester cursor rejection, safe 403/400 bodies, and VSTest sandbox fallback evidence.

### File List

- src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs
- src/Hexalith.Tenants/Controllers/TenantsQueryController.cs
- src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs
- tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs
- tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryCursorCodecTests.cs
- tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs
- tests/test-summary.md
- _bmad-output/implementation-artifacts/5-5-enforce-query-side-authorization-and-isolation.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-06-01: Enforced concrete-role-only query authorization, fail-closed direct tenant misses for non-admin callers, requester-bound user-tenant cursors, and strengthened query isolation tests.
- 2026-06-01: Senior review auto-fixes filtered corrupt/default membership rows from query DTOs, closed empty-state cursor bypasses, updated tests, and marked the story done.

## Senior Developer Review (AI)

### Review Outcome

Approved after auto-fix. No critical issues remain.

### Findings Fixed

- HIGH: `GetTenant` returned `TenantRole.Unknown` membership rows in `TenantDetail.Members` for otherwise authorized callers, exposing corrupt/default memberships as visible rows. Fixed by filtering tenant detail members to concrete roles only.
- HIGH: `GetTenantUsers` paginated all `TenantReadModel.Members`, so an `Unknown` role row could appear in user-list responses and influence cursor/`hasMore` metadata. Fixed by filtering before pagination.
- MEDIUM: `GetUserTenants` returned the empty missing-target response before validating a submitted cursor. Fixed so wrong-scope cursors fail with `Invalid cursor.` before the empty response.
- MEDIUM: `ListTenants` returned an empty index response before validating a submitted cursor. Fixed so invalid cursors fail before state reads or empty-page materialization.
- MEDIUM: Story File List did not include `tests/test-summary.md`, which was changed by the implementation. Fixed by adding it to the File List.

### Validation Checklist

- [x] Story file loaded and status verified as reviewable before review.
- [x] Acceptance Criteria and completed tasks cross-checked against source and tests.
- [x] File List compared with git changes; unrelated orchestration artifact left untouched.
- [x] Code quality and security review performed on changed source/test files.
- [x] MCP doc search attempted; Aspire MCP search was unavailable/cancelled, so local architecture and project-context references were used.
- [x] Focused tests and Release build completed with VSTest sandbox fallback recorded.
- [x] Story status and sprint status synced to `done`.
