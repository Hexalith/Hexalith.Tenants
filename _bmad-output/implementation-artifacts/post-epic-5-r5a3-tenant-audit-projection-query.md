# Post-Epic-5 R5-A3: Tenant Audit Projection and Query

Status: review

## Story

As a GlobalAdministrator,
I want tenant audit queries to return access and administrative events by tenant and date range,
so that I can produce operational and compliance evidence from the tenant event stream.

## Acceptance Criteria

1. Given tenant events exist for a tenant, when `TenantAuditProjection` processes them, then `TenantAuditReadModel` stores audit entries with event ID, event type, category, actor ID, timestamp, tenant ID, and narrative payload.
2. Given `GET /api/tenants/{tenantId}/audit` is called by a GlobalAdministrator with date range parameters, then paginated audit entries are returned instead of the current not-implemented failure.
3. Given a non-GlobalAdministrator calls the audit endpoint, then the request remains forbidden and does not reveal audit data.
4. Given `category=access` or `category=administrative` is provided, then only matching events are returned.
5. Given pagination parameters are provided, then results are returned in stable timestamp/event ordering with a valid cursor.
6. Given no matching audit entries exist for the tenant/date/category filter, then a successful empty `PaginatedResult<TenantAuditEntry>` is returned.
7. Tests cover projection application, category classification, date range filtering, GlobalAdministrator-only authorization, pagination, empty results, and the current metadata limitation described in Dev Notes.

## Tasks / Subtasks

- [x] Task 1: Settle audit metadata truthfully before implementing response shape (AC: #1, #7)
  - [x] 1.1 Inspect `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs` and `Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs`.
  - [x] 1.2 Verify whether projection requests expose a true event ID and actor user ID. Current evidence: they do not.
  - [x] 1.3 Do not fabricate `actorId` or `eventId` from payload fields. If `MessageId` and `UserId` remain unavailable, either make an explicit additive EventStore contract change or revise the acceptance/product artifacts before marking this story complete.
  - [x] 1.4 If changing EventStore projection metadata, keep it additive and backward-compatible: add optional/wire-compatible members to `ProjectionEventDto`, map from persisted `EventEnvelope.MessageId` and `EventEnvelope.UserId`, and update EventStore tests in the submodule in its own commit/branch flow.

- [x] Task 2: Add audit query contracts and DTOs in Contracts (AC: #1-6)
  - [x] 2.1 Add `src/Hexalith.Tenants.Contracts/Enums/AuditEventCategory.cs` with `Access` and `Administrative`.
  - [x] 2.2 Add `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs` as an immutable response DTO containing `EventId`, `EventType`, `AuditEventCategory Category`, `ActorId`, `Timestamp`, `TenantId`, and `IReadOnlyDictionary<string,string> NarrativePayload`.
  - [x] 2.3 Update `GetTenantAuditQuery` XML docs so it no longer describes MVP 501 behavior.
  - [x] 2.4 Keep `GetTenantAuditQuery` as the existing static-member `IQueryContract` class unless the local query contract pattern has changed. Do not replace it with the architecture sketch record unless the rest of the repository supports typed query payload records.

- [x] Task 3: Add audit read model and projection in Server (AC: #1, #4-6)
  - [x] 3.1 Add `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`.
  - [x] 3.2 Add `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`.
  - [x] 3.3 Store audit entries per managed tenant with deterministic ordering by timestamp, then event identity. Prefer `audit:{tenantId}` state keys as specified by D12.
  - [x] 3.4 Classify `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `GlobalAdministratorSet`, and `GlobalAdministratorRemoved` as `Access`.
  - [x] 3.5 Classify `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `TenantConfigurationSet`, and `TenantConfigurationRemoved` as `Administrative`.
  - [x] 3.6 Build `NarrativePayload` from stable payload fields only, such as target `userId`, `role`, `oldRole`, `newRole`, `key`, tenant `name`, status timestamps, or config key. Do not include secrets, raw payload JSON, bearer tokens, or user-controllable display names as trusted identity.

- [x] Task 4: Wire audit projection updates through the existing projection endpoint (AC: #1)
  - [x] 4.1 Update `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs` so tenant projection requests also update the per-tenant audit state for the same `ProjectionRequest.Events`.
  - [x] 4.2 Preserve current per-tenant `TenantReadModel` rebuild behavior and existing `TenantIndexReadModel` update behavior.
  - [x] 4.3 Keep `ProjectionDispatcher` domain routing fail-closed. Do not add a new public projection endpoint or duplicate projection dispatcher.
  - [x] 4.4 If global-administrator events must appear in tenant audit results, define their tenant mapping explicitly. Current `GlobalAdministratorProjectionHandler` validates `TenantId == "system"` and `AggregateId == "global-administrators"`, so those events are not naturally per-managed-tenant audit rows.

- [x] Task 5: Implement `get-tenant-audit` actor query behavior (AC: #2-6)
  - [x] 5.1 Replace the not-implemented result in `TenantsProjectionActor.HandleGetTenantAuditAsync`.
  - [x] 5.2 Preserve the first check: non-GlobalAdministrators must receive `Forbidden` before any audit state lookup.
  - [x] 5.3 Parse audit payload from `TenantsQueryController`: `from`, `to`, `category`, `cursor`, and `pageSize`.
  - [x] 5.4 Load `TenantAuditReadModel` from `statestore` key `audit:{tenantId}`.
  - [x] 5.5 Apply inclusive date range filtering with `DateTimeOffset` values. Treat missing bounds as open-ended.
  - [x] 5.6 Apply optional category filtering. Invalid category values should fail clearly at the API boundary or fall back only if the repository already uses that pattern for query payload parsing.
  - [x] 5.7 Paginate after filtering using stable timestamp/event ordering. Do not paginate before filtering.
  - [x] 5.8 Return `PaginatedResult<TenantAuditEntry>` serialized with camelCase and string enums, matching existing query serialization options.

- [x] Task 6: Update REST endpoint payload and behavior (AC: #2-6)
  - [x] 6.1 Update `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs` to accept `category`, `cursor`, and `pageSize` for `/api/tenants/{tenantId}/audit`.
  - [x] 6.2 Clamp audit `pageSize` to FR29 defaults and limits: default 100, maximum 1000. This differs from existing list endpoints, which currently clamp to 20/100.
  - [x] 6.3 Keep the controller thin: it should validate route/query shape, serialize the query payload, set `UserId` from `sub`, and dispatch `SubmitQuery`.
  - [x] 6.4 Rely on `SubmitQueryHandler` to map actor failures to HTTP status via `QueryExecutionFailedException`; do not return successful `Ok` responses for forbidden or not-implemented results.

- [x] Task 7: Add focused tests (AC: #1-7)
  - [x] 7.1 Add `TenantAuditReadModelTests` for all classified event types, narrative payload fields, empty state, ordering, and unknown event handling.
  - [x] 7.2 Add `TenantAuditProjectionTests` for projection naming and full-event-list projection behavior.
  - [x] 7.3 Update `TenantsProjectionActorTests`: replace `GetTenantAudit_global_admin_returns_not_implementedAsync` with successful audit query tests.
  - [x] 7.4 Keep and strengthen `GetTenantAudit_non_admin_returns_forbidden_not_501Async`.
  - [x] 7.5 Add actor tests for date range filtering, category filtering, cursor pagination, invalid/missing state empty result, and page size clamping if clamping lives below the controller.
  - [x] 7.6 Add or update contract serialization tests for `TenantAuditEntry` and `AuditEventCategory`.
  - [x] 7.7 Add controller-level or integration coverage only if the existing fixture can run it without live DAPR fragility. Actor and read-model tests are the Tier 1 guard.

- [x] Task 8: Verify focused build and tests (AC: #7)
  - [x] 8.1 Run `dotnet test .\tests\Hexalith.Tenants.Contracts.Tests\Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Queries`.
  - [x] 8.2 Run `dotnet test .\tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantAudit`.
  - [x] 8.3 Run `dotnet test .\tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`.
  - [x] 8.4 If EventStore projection DTO changes are required, run the relevant EventStore projection contract/server tests inside `Hexalith.EventStore` and document them in the Dev Agent Record.

## Dev Notes

### Current State

R5-A3 is the next backlog item in `sprint-status.yaml`. A stub story already existed from the 2026-05-13 implementation readiness alignment proposal; this version expands it into the developer guide for D12.

Story 5.3 created:

- `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`

Current `TenantsProjectionActor.HandleGetTenantAuditAsync` first checks `IsGlobalAdminAsync(envelope.UserId)`. That security ordering is correct and must be preserved. For admins it returns an unsuccessful `QueryResult` with "Audit queries are not yet implemented (FR29). Planned for a future release." `SubmitQueryHandler` maps that not-implemented failure to HTTP 501 by throwing `QueryExecutionFailedException`.

Current `TenantsQueryController.GetTenantAuditAsync` only accepts `from` and `to`, serializes them into an anonymous payload, and dispatches `SubmitQuery` with:

- `Tenant = "system"`
- `Domain = GetTenantAuditQuery.Domain`
- `AggregateId = tenantId`
- `QueryType = GetTenantAuditQuery.QueryType`
- `UserId = authenticated sub`
- `EntityId = tenantId`
- `ProjectionType = TenantProjectionRouting.ActorTypeName`

The endpoint must keep using the authenticated `sub` claim. Do not use `name`, email, or display-name claims for authorization or actor identity.

### Critical Metadata Limitation

Acceptance criterion #1 requires event ID and actor ID. The current projection path does not expose both values to the Tenants service.

Current `ProjectionEventDto` fields:

- `EventTypeName`
- `Payload`
- `SerializationFormat`
- `SequenceNumber`
- `Timestamp`
- `CorrelationId`

The persisted EventStore envelopes have `MessageId` and `UserId`, but `ProjectionUpdateOrchestrator` currently drops them when mapping `EventEnvelope` to `ProjectionEventDto`. The `ProjectionEventDto` comment says it deliberately excludes server-internal fields including `UserId` and `MessageId`.

Implementation must not pretend `CorrelationId`, `SequenceNumber`, or target `UserId` from event payload is the actor ID. For a truthful audit trail, the developer must either:

- make an explicit additive EventStore projection contract change to include the required metadata, or
- revise PRD FR29 / architecture D12 / this story if product chooses an audit response that does not expose actor ID and true event ID.

Do not mark this story done with fabricated audit metadata. That would create compliance-looking output that is not actually compliant.

### Required Design

Implement D12 as a per-tenant audit read model served by the existing query actor:

- Projection storage key: `audit:{tenantId}`.
- Query response: `PaginatedResult<TenantAuditEntry>`.
- REST endpoint: `GET /api/tenants/{tenantId}/audit?from=2026-03-01&to=2026-03-25&category=access&cursor=...&pageSize=100`.
- Date filters use `DateTimeOffset`.
- Category values serialize as strings with the existing `JsonStringEnumConverter`.
- Ordering must be stable: timestamp first, then event identity. If EventStore metadata is not yet available, ordering can temporarily use `SequenceNumber` internally, but the public `EventId` still must not be faked.

### Event Classification

Access:

- `UserAddedToTenant`
- `UserRemovedFromTenant`
- `UserRoleChanged`
- `GlobalAdministratorSet`
- `GlobalAdministratorRemoved`

Administrative:

- `TenantCreated`
- `TenantUpdated`
- `TenantDisabled`
- `TenantEnabled`
- `TenantConfigurationSet`
- `TenantConfigurationRemoved`

Open mapping issue: global-administrator events currently project through `GlobalAdministratorProjectionHandler` with tenant `system` and aggregate `global-administrators`, not through per-managed-tenant projection requests. The developer must not silently make them appear under every tenant. If they are included, document and test the exact tenant mapping rule. If they cannot be tenant-scoped truthfully, amend D12/FR29 expectations for those event types.

### Files To Update

Expected updates in this repository:

- `src/Hexalith.Tenants.Contracts/Enums/AuditEventCategory.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/*`
- `tests/Hexalith.Tenants.Server.Tests/Projections/*`

Potential dependency updates if metadata is required:

- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs`
- relevant EventStore projection contract/server tests

Do not update:

- `Directory.Packages.props` or project-level package references; no dependency change is required.
- `Hexalith.Builds` submodule.
- DAPR components or AppHost resources unless tests prove audit projection needs a missing resource.
- Query authorization to use JWT role claims.
- Existing list/query page-size behavior unless intentionally shared through a helper without changing semantics.

### Existing Patterns To Reuse

- Query contracts are static-member classes implementing `IQueryContract`, not payload records.
- Query response DTOs live under `src/Hexalith.Tenants.Contracts/Queries`.
- Read models and projections live under `src/Hexalith.Tenants.Server/Projections`.
- `TenantsProjectionActor` owns query-side domain authorization and serialization.
- `TenantsQueryController` is a thin REST-to-MediatR translator.
- `TenantProjectionHandler` already updates both per-tenant state and the cross-tenant index from the same projection request.
- Tests use xUnit v3, Shouldly, and NSubstitute.
- Projection actor tests already have `CreateActor`, `CreateEnvelope`, `SetupGlobalAdminState`, `SetupNoGlobalAdmin`, and `DeserializePayload<T>` helpers.

### Previous Story Intelligence

Story 5.1 established the per-tenant and global administrator projection patterns:

- `TenantReadModel` applies tenant lifecycle, membership, role, and configuration events.
- `GlobalAdministratorReadModel` supports the `IsGlobalAdminAsync` authorization check.

Story 5.2 established the cross-tenant fan-in index:

- `TenantIndexReadModel` uses a singleton state key.
- `UserTenants` maps user IDs to tenant IDs and current roles.
- Existing projection Apply methods skip or ignore impossible historical states rather than throwing for every mismatch.

Story 5.3 established query infrastructure:

- `GetTenantAuditQuery` exists but intentionally returns not implemented for admins.
- `TenantsProjectionActor` serializes query payloads using camelCase and string enums.
- `Paginate` currently sorts by string keys. Audit pagination needs timestamp plus event identity ordering, so reuse the approach but not necessarily the exact helper.
- Query controller actions dispatch `SubmitQuery`; `SubmitQueryHandler` translates actor failures into HTTP status exceptions.

Post-Epic-5 R5-A1 fixed authentication pipeline wiring:

- Controller actions now receive authenticated identities through `sub`.
- Do not regress `AddJwtAuthentication`, `UseAuthentication`, or `UseAuthorization`.

Post-Epic-5 R5-A2 was recently implemented:

- `GetUserTenantsQuery` was corrected to use scoped result filtering rather than broad cross-user visibility.
- Keep that behavior intact while editing `TenantsProjectionActor`.

### Architecture And Requirements Context

This story implements D12: dedicated `TenantAuditProjection` and `TenantAuditReadModel` for filtered audit reads.

Relevant requirements:

- FR16: global administrator actions produce auditable domain events.
- FR25-FR30: tenant read model supports tenant discovery, audit, and cursor-based query endpoints.
- FR29: GlobalAdministrator can query tenant access changes by tenant ID and date range for audit reporting, default page size 100 and max 1000.
- FR30: all list/query endpoints support cursor-based pagination with consistent ordering.
- NFR2: read model queries complete within 50ms p95 for a single page.
- NFR5: zero cross-tenant data leaks.
- NFR7: state-changing operations produce immutable auditable domain events with actor ID, timestamp, and operation context.
- NFR10: branch coverage on tenant isolation and role authorization logic.

Scope clarification from 2026-05-13: D12 is backend MVP-relevant because it affects FR29 and NFR5. D13-D17 UI/FrontShell concerns remain Phase 2 unless explicitly promoted.

### Latest Technical Context

No external package or library upgrade is required. Use the repository's current stack:

- .NET SDK `10.0.103`, target `net10.0`, nullable enabled, warnings as errors.
- Dapr package family is centrally pinned; do not add package versions in `.csproj` files.
- Tests use xUnit v3, Shouldly, and NSubstitute.
- Keep central package management intact.

### Anti-Patterns To Avoid

- Do not fabricate actor IDs or event IDs.
- Do not return raw event payload JSON as audit narrative.
- Do not expose secrets, tokens, raw claims, or local paths in audit payloads.
- Do not leak audit rows to TenantOwner, TenantContributor, TenantReader, or ordinary authenticated users unless the product explicitly changes FR29.
- Do not paginate before applying authorization, date, and category filters.
- Do not implement audit query by replaying the event store on every HTTP request; D12 requires a materialized read model.
- Do not create a separate Query API project.
- Do not bypass MediatR/QueryRouter/ProjectionActor for this endpoint.
- Do not change global query routing type name `"ProjectionActor"`.
- Do not initialize or update nested submodules recursively.

### References

- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13-implementation-readiness-alignment.md#Proposal-4-Add-Post-Epic-D12-Audit-Projection-Story`]
- [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-13.md#Critical-Violations`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#D12-Audit-Projection-and-Query-Design`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Tenant-Discovery--Query`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/implementation-artifacts/5-1-per-tenant-and-global-admin-projections.md#Dev-Notes`]
- [Source: `_bmad-output/implementation-artifacts/5-2-cross-tenant-index-projection.md#Dev-Notes`]
- [Source: `_bmad-output/implementation-artifacts/5-3-query-endpoints-and-authorization.md#Dev-Agent-Record`]
- [Source: `_bmad-output/implementation-artifacts/post-epic-5-r5a1-tenants-jwt-auth-wiring.md#Review-Findings`]
- [Source: `_bmad-output/implementation-artifacts/post-epic-5-r5a2-get-user-tenants-scoped-authorization.md#Dev-Notes`]
- [Source: `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`]
- [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs#HandleGetTenantAuditAsync`]
- [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetTenantAuditAsync`]
- [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex.

### Debug Log References

- 2026-05-14: Verified `ProjectionEventDto` lacked true `MessageId` and actor `UserId`; added optional additive EventStore DTO fields and mapped from `EventEnvelope`.
- 2026-05-14: Avoided fabricated audit metadata. `TenantAuditReadModel` skips projection events without true `MessageId` or `UserId`.
- 2026-05-14: Avoided duplicate EventStore projection discovery by keeping `TenantAuditProjection` as a tenant-handler projection helper rather than a second discoverable `tenants` projection.
- 2026-05-14: Full `dotnet test --configuration Release --no-restore` timed out at 5 minutes. Project-level validation was run serially instead.
- 2026-05-14: Full integration project failed only in nightly `Category=Performance` test `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents`, which hits `ConfigurationLimitExceededRejection` while seeding event 301. Main-lane integration tests with `Category!=Performance` passed.

### Completion Notes List

- Added audit query contract DTOs and category enum with string-enum serialization coverage.
- Added per-tenant `TenantAuditReadModel` and projection helper that classify access vs administrative events, build bounded narrative payloads, and maintain timestamp/event-id ordering.
- Wired `TenantProjectionHandler` to save `audit:{tenantId}` from the same projection request while preserving existing tenant and tenant-index updates.
- Replaced the audit query 501 path with GlobalAdministrator-only audit reads, inclusive date filtering, category filtering, stable cursor pagination, missing-state empty results, and FR29 page-size limits.
- Updated the REST endpoint to accept `from`, `to`, `category`, `cursor`, and `pageSize`, validate category values, clamp audit page size to default 100 / max 1000, and continue using authenticated `sub`.
- Added EventStore projection metadata mapping for true event IDs and actor IDs.
- Global administrator events remain classified by the read model but are not silently fanned out to every tenant; the existing global administrator projection path remains separate.

### File List

- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Projections/ProjectionUpdateOrchestratorTests.cs`
- `src/Hexalith.Tenants.Contracts/Enums/AuditEventCategory.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-14: Implemented tenant audit projection/query behavior and additive EventStore projection metadata support.

## Story Completion Status

Implementation complete; ready for review. Main-lane validation passed. Nightly performance integration test has an unrelated existing seed-data/domain-limit failure documented above.
