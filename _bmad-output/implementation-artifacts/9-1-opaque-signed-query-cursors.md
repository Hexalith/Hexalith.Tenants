# Story 9.1: Opaque Signed Query Cursors

Status: in-progress

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want paginated query cursors to be opaque and tamper-resistant,
so that clients cannot forge cursor positions or infer internal projection keys across tenant query endpoints.

## Acceptance Criteria

1. Given a paginated tenant query returns a continuation cursor, when the response is serialized, then the cursor is opaque and does not expose raw timestamps, event IDs, tenant keys, or projection keys.
2. Given a client submits a valid signed cursor, when the matching endpoint processes the next page request, then pagination resumes from the same logical position as the previous plain cursor behavior.
3. Given a client submits a tampered cursor, when the endpoint validates the cursor, then the request is rejected with a safe `400 Bad Request` ProblemDetails response and no query state is leaked.
4. Given cursor signing is enabled, when `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit` return paginated results, then each endpoint uses the same cursor codec/signing policy.
5. Given cursor validation fails, when logs are emitted, then logs include correlation metadata but do not include secrets, raw signing material, or full cursor payloads.
6. Given focused query tests run, when valid, malformed, and tampered cursors are exercised, then the tests verify success for valid cursors and safe rejection for invalid cursors across all affected paginated endpoints.

## Tasks / Subtasks

- [x] Add a shared cursor codec for Tenants query pagination. (AC: 1, 2, 4)
  - [x] Protect cursor payloads with ASP.NET Core Data Protection using a stable, versioned purpose such as `Hexalith.Tenants.QueryCursor.v1`.
  - [x] Encode a structured internal payload containing at least schema version, query type, endpoint scope, raw logical position, and issued timestamp.
  - [x] Decode signed cursors back to the existing raw logical cursor values used by current pagination code.
  - [x] Reject unsupported version, missing position, wrong query type, wrong endpoint scope, malformed payload, and failed unprotect cases.
- [x] Update paginated query handling to use the codec. (AC: 1, 2, 4)
  - [x] Update `TenantsProjectionActor` so `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit` decode incoming signed cursors before applying existing ordering logic.
  - [x] Update result creation so returned `PaginatedResult<T>.Cursor` values are signed/opaque, while `Items` and `HasMore` remain unchanged.
  - [x] Preserve current raw ordering semantics: tenant/member/user lists advance by sorted ID; audit advances by timestamp then event ID.
- [x] Return safe HTTP 400 ProblemDetails for invalid submitted cursors. (AC: 3, 5)
  - [x] Validate incoming cursor strings at the controller boundary before dispatching the query, or add an equivalent Tenants-specific mapping that cannot surface invalid cursor failures as 500.
  - [x] Keep response details generic, for example `Invalid cursor.`, and include normal ProblemDetails correlation metadata.
  - [x] Log validation failure with correlation ID/query type/endpoint only; do not log raw cursor text, unprotected payload, signing keys, or internal projection keys.
- [x] Preserve authorization and query behavior. (AC: 2, 4)
  - [x] Do not change who can see `list-tenants`, `get-tenant-users`, `get-user-tenants`, or `get-tenant-audit` results.
  - [x] Do not solve Story 9.2 cursor stability under role/membership mutation here; keep this story focused on token opacity and tamper resistance.
  - [x] Do not change Story 9.3 disabled-tenant/orphan-membership policy in this story.
- [x] Add focused tests. (AC: 1-6)
  - [x] Unit-test codec round trips, wrong purpose/scope/query type rejection, malformed input, and tamper rejection.
  - [x] Update actor tests to verify valid signed cursors resume at the same logical position as the current plain cursor behavior.
  - [x] Update actor tests to verify returned cursors do not contain raw tenant IDs, timestamps, event IDs, `audit:`, `projection:`, or `ticks:eventId` material.
  - [x] Update controller/integration tests to verify invalid cursor input returns `400` with `application/problem+json`.
  - [x] Keep existing 401/403/404 query behavior tests green.

## Dev Notes

### Current Query Behavior To Preserve

- `TenantsQueryController` is a thin authenticated REST controller. It validates route identifiers, extracts `sub`, clamps page sizes, serializes query payloads, and dispatches `SubmitQuery` through MediatR. Query logic and authorization are intentionally in `TenantsProjectionActor`, not the controller. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]
- Current controller payloads pass `cursor` through as raw query string text for `get-tenant-users`, `get-user-tenants`, `list-tenants`, and `get-tenant-audit`. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]
- `TenantsProjectionActor.DeserializePaginationPayload` currently treats cursor as an arbitrary string and page size as 20 by default, max 100. `DeserializeAuditPayload` defaults audit page size to 100, max 1000, and currently validates audit cursor shape with `^\d{20}:.+$`. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `Paginate` sorts by endpoint key selector and advances with `string.Compare(keySelector(kvp), cursor, Ordinal) > 0`. Existing raw cursors are tenant IDs or user IDs depending on endpoint. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `PaginateAuditEntries` sorts by timestamp then event ID and currently exposes raw audit cursor as `{UtcTicks:D20}:{EventId}`. This leaks both timing and event identifier details and is the main AC1 risk. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `PaginatedResult<T>` is a public contract with `Items`, `Cursor`, and `HasMore`; this story should keep that shape and only change the cursor string contents. [Source: `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`]

### Files Likely To Update

- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`: validate incoming signed cursors and return safe `400 Bad Request` ProblemDetails for invalid cursor input before query dispatch. Preserve existing identifier, auth, category, and page-size behavior.
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: replace raw cursor parsing/return with codec decode/encode. Preserve existing authorization checks, DAPR state reads, ordering, page-size clamping, metrics, and projection type strings.
- `src/Hexalith.Tenants/Program.cs`: register any cursor codec/Data Protection services needed by controller and actor. Do not disturb existing middleware order.
- New implementation file, suggested: `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs` or similar internal service in the web host project, because both controller and actor live there.
- Tests to update/add: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`, `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`, and a focused codec test file under the same test project that already has `InternalsVisibleTo`.

### Guardrails

- Do not change `PaginatedResult<T>` or query DTO public shape unless absolutely required; cursor opacity can be achieved inside the string value.
- Do not put Data Protection package versions inline in `.csproj`; this repo uses central package management. [Source: `Directory.Packages.props`; `Directory.Build.props`]
- Do not modify the `Hexalith.EventStore` submodule for this story unless the implementation truly cannot meet AC3 from Tenants. Prefer controller-side validation/mapping for Tenants cursor errors.
- Do not log raw cursor payloads or protected tokens. Existing logging patterns include correlation ID, tenant, domain, aggregate ID, query type, status, and stage; follow that style without adding sensitive payload data. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitQueryHandler.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/QueryExecutionFailedExceptionHandler.cs`]
- Preserve `GetUserTenants` timing-uniformity behavior from the recent R5-A2/R5-A3 work: cross-user lookups still run the admin check before early return. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; recent commits `post-epic-5-r5a2`, `post-epic-5-r5a3` story artifacts]

### Cursor Payload Policy

Use one shared codec/signing policy for all paginated tenant query endpoints:

- `queryType`: one of `list-tenants`, `get-tenant-users`, `get-user-tenants`, `get-tenant-audit`.
- `scope`: endpoint-specific stable scope so a cursor from one route cannot be replayed against another route or entity. Suggested scopes:
  - `list-tenants`: authenticated user ID or a hash of user ID plus query type, if product accepts user-bound cursors.
  - `get-tenant-users`: tenant ID.
  - `get-user-tenants`: target user ID plus requester scope decision if needed.
  - `get-tenant-audit`: tenant ID plus audit filter scope (`from`, `to`, `category`) so cursors cannot be replayed across different audit windows.
- `position`: the existing raw logical cursor value used internally by current pagination code.
- `version`: start at `1` so future cursor formats can be rejected or migrated deliberately.

The protected token itself is the only value exposed in `PaginatedResult<T>.Cursor`.

### Latest Technical Information

- ASP.NET Core Data Protection is the preferred in-platform API for protecting data sent to untrusted clients. The official docs state that `Protect` returns protected data and `Unprotect` throws `CryptographicException` if the protected payload was tampered with or produced for a different protector. [Source: Microsoft Learn, Data Protection consumer APIs: https://learn.microsoft.com/aspnet/core/security/data-protection/consumer-apis/overview?view=aspnetcore-10.0]
- Data Protection purpose strings isolate cryptographic consumers. Use a unique, versioned purpose string and do not let untrusted input be the only purpose-chain value. [Source: Microsoft Learn, Purpose strings: https://learn.microsoft.com/aspnet/core/security/data-protection/consumer-apis/purpose-strings?view=aspnetcore-10.0]
- `IDataProtector` instances are thread-safe and intended for reuse after `CreateProtector`; inject/create once in the codec instead of recreating per cursor. [Source: Microsoft Learn, Get started with Data Protection APIs: https://learn.microsoft.com/aspnet/core/security/data-protection/using-data-protection?view=aspnetcore-10.0]

### Testing Requirements

- Use xUnit and Shouldly, matching existing tests. [Source: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`; `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- Actor tests currently instantiate `TenantsProjectionActor` directly with substituted `DaprClient` and `NullLogger`; update helpers cleanly if the constructor needs a cursor codec.
- Integration tests currently verify auth failures and ProblemDetails mapping. Add invalid cursor cases beside `TenantsQueryControllerIntegrationTests` and assert `HttpStatusCode.BadRequest` plus `application/problem+json`.
- Add negative tests for tampering by changing at least one character in a protected cursor and for cross-endpoint misuse by submitting a `list-tenants` cursor to `get-tenant-users` or `get-tenant-audit`.
- Existing `ListTenants_cursor_skips_deleted_tenant`, `ListTenants_pagination_with_cursor`, `GetUserTenants_tenant_owner_paginates_after_filtering`, and `GetTenantAudit_paginates_after_filtering_with_stable_cursorAsync` should be converted from raw cursor assumptions to signed cursor assertions while preserving logical item order.

### Project Structure Notes

- Query contracts belong in `src/Hexalith.Tenants.Contracts/Queries`, but this story should not require public contract changes.
- Query endpoint/controller behavior belongs in `src/Hexalith.Tenants`.
- Projection read models remain in `src/Hexalith.Tenants.Server/Projections`; do not move them for cursor work.
- The app targets `net10.0`, nullable enabled, warnings as errors, and central package management. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`]

## Previous Story Intelligence

This is the first story in Epic 9, so there is no previous Epic 9 story file to inherit from. Relevant completed hardening context comes from:

- R5-A2: `get-user-tenants` scoped authorization added TenantOwner visibility only for tenants they own.
- R5-A3: audit projection/query implemented and added safe audit filtering, date/category pagination, and non-admin 403 behavior.
- Current recent commits are automation/release/preflight oriented; no recent commit introduced a cursor utility. Reuse the current query/projection tests as the implementation guide rather than following the recent automation commits.

## Project Context Reference

- Follow repository rules in `AGENTS.md`: no recursive submodule initialization/update, and Conventional Commits for any commit message.
- Follow C# conventions already in this repo: file-scoped namespaces, nullable-safe code, `ArgumentNullException.ThrowIfNull`, no inline package versions, Shouldly assertions, and focused tests.
- Root-level submodule `Hexalith.EventStore` is read as architecture/reference context only for this story unless a deliberate Tenants-blocking API gap is discovered and approved.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` passed: 304 tests.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` passed: 18 tests.
- `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` passed with 0 warnings.
- `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore --filter FullyQualifiedName!~AspireTopologyTests&FullyQualifiedName!~SnapshotPerformanceTests` passed: 522 tests.
- `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` did not satisfy the full DoD gate because unrelated integration tests failed: Aspire topology fixture requires a newer Aspire AppHost runtime/package, and `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` hit `ConfigurationLimitExceededRejection` while seeding event 301.

### Completion Notes List

- Added a Data Protection-backed cursor codec with versioned purpose `Hexalith.Tenants.QueryCursor.v1` and payload fields for version, query type, endpoint scope, raw logical position, and issued timestamp.
- Wired `TenantsProjectionActor` to decode protected submitted cursors for `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`, preserve the previous raw ordering semantics internally, and protect returned continuation cursors.
- Added controller-boundary cursor validation for all paginated query endpoints with generic `400 Bad Request` ProblemDetails responses and source-generated warning logs that omit cursor payloads and signing material.
- Updated query projection, telemetry, codec, and controller integration tests for signed cursor round trips, opaque cursor assertions, tamper/malformed rejection, and existing authorization behavior.
- Implementation is complete, but story status remains `in-progress` because the unfiltered full solution test pass has unrelated failing integration/performance gates.

### File List

- `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Program.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryCursorCodecTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs`

### Change Log

- 2026-05-16: Implemented opaque signed query cursors and focused coverage; retained `in-progress` status pending unrelated full-suite blockers.
