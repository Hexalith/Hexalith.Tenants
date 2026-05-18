# Story 9.5: Shared Pagination Bounds and Cursor Utilities

Status: review

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a developer maintaining tenant query endpoints,
I want pagination bounds and cursor handling to be centralized,
so that tenant query behavior stays consistent as endpoints evolve.

## Acceptance Criteria

1. Given tenant query endpoints clamp page sizes, when `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit` apply defaults and maximums, then the shared policy uses one source of truth for default and maximum page size values.
2. Given `get-tenant-audit` currently has duplicate page-size clamping in controller and actor code, when the duplication is refactored, then both layers continue to enforce the same default `100` and maximum `1000` behavior unless a deliberate policy change is documented.
3. Given cursor encoding/decoding is required by multiple endpoints, when cursor utilities are introduced or refactored, then endpoint-specific ordering details remain explicit and testable rather than hidden behind unclear generic code.
4. Given invalid page sizes or cursors are submitted, when the endpoint validates request parameters, then responses remain safe and consistent with existing API error patterns.
5. Given focused tests run, when page size defaults, maximum clamping, invalid inputs, and endpoint-specific cursor behavior are exercised, then tests verify consistent behavior across all affected query endpoints.
6. Given this story is a refactor, when oversized page sizes are submitted, then endpoints continue to clamp to the current effective maximum and do not start returning validation errors for values previously accepted.
7. Given malformed pagination payloads reach the actor, when standard tenant queries receive malformed JSON, then they preserve the existing first-page fallback using default page size `20`; when audit receives malformed JSON, then it preserves the existing unsuccessful result with `"Invalid audit query payload."`.
8. Given cursor utility code is shared, when cursors are encoded or decoded, then the shared code centralizes serialization, signing validation, and scope checks only; each endpoint and actor call site remains responsible for its explicit ordering fields and logical cursor position.
9. Given signed cursors may have been issued before this refactor, when those cursors are decoded after the shared utility changes, then valid existing cursors continue to work and invalid existing cursors continue to fail with the same invalid-cursor behavior.
10. Given pagination parsing can fail before query execution, when parsing fails in controller or actor paths, then the failure path must not write projection state, emit decoded cursor details, or log serialized pagination payload bodies.
11. Given this story introduces shared helpers, when implementation is complete, then helper APIs remain internal to the server project and no public route, DTO, response envelope, or package dependency changes are introduced.

## Tasks / Subtasks

- [x] Introduce one shared pagination bounds policy for tenant queries. (AC: 1, 2, 5)
  - [x] Add a small internal server-side utility or constants type for standard query pagination values: default page size `20`, maximum page size `100`, audit default page size `100`, and audit maximum page size `1000`.
  - [x] Name the shared policy by endpoint family rather than caller location, for example standard tenant queries and audit queries; do not recreate controller-only and actor-only constants.
  - [x] Replace duplicated literals in `TenantsQueryController.ClampPageSize`, `TenantsQueryController.ClampAuditPageSize`, `TenantsProjectionActor.DeserializePaginationPayload`, and `TenantsProjectionActor.DeserializeAuditPayload`.
  - [x] Keep the current behavior exactly: `pageSize <= 0` falls back to the endpoint default, standard list endpoints cap at `100`, and audit caps at `1000`.
  - [x] Do not move the policy into public contract DTOs unless implementation proves a public surface is needed.
- [x] Centralize pagination payload parsing without hiding endpoint-specific behavior. (AC: 1, 3, 4)
  - [x] Extract the common `cursor` and `pageSize` parsing used by standard paginated endpoints into a focused helper that still lets audit parsing handle `from`, `to`, and `category` explicitly.
  - [x] Preserve existing malformed JSON behavior: standard pagination payloads fall back to `(cursor: null, pageSize: 20)`, while audit payloads return `"Invalid audit query payload."`.
  - [x] Preserve the current safe handling for missing, null, non-string cursor, non-number page size, and negative/zero page size values.
  - [x] Keep endpoint ordering keys explicit at call sites: tenant IDs for `list-tenants` and `get-user-tenants`, user IDs for `get-tenant-users`, and `Timestamp.UtcTicks + EventId` for audit.
  - [x] Where Story 9.4 guardrails exist, keep actor validation and authorization guardrails ahead of page-size parsing or cursor decoding.
- [x] Clarify and preserve cursor scope utilities. (AC: 3, 4)
  - [x] Keep `TenantQueryCursorScopes` as the single place for scope strings used by controller validation and actor decoding.
  - [x] Do not rename existing scope strings (`user:{userId}`, `tenant:{tenantId}`, `target-user:{targetUserId}`, and audit filter scope) because existing signed cursors rely on exact query type and scope matching.
  - [x] Keep signed cursor payload shape, signing format, query type matching, scope matching, error behavior, and public response fields unchanged.
  - [x] Add at least one regression assertion that a cursor encoded with the existing query type, scope, and logical position shape can still be decoded through the refactored path. (AC: 9)
  - [x] If helper names are changed, update both controller and actor call sites in the same patch so cursor validation stays symmetric.
  - [x] Do not introduce a generic cursor abstraction that obscures which logical position each endpoint stores.
- [x] Preserve invalid cursor and invalid audit payload error behavior. (AC: 4)
  - [x] Keep controller-level invalid cursor responses as HTTP 400 ProblemDetails with reason code `invalid-cursor`.
  - [x] Keep actor-level malformed cursor results as `new QueryResult(false, default, ErrorMessage: "Invalid cursor.")` so existing EventStore query error mapping remains unchanged.
  - [x] Keep invalid audit category and `from > to` handling explicit in audit parsing.
  - [x] Confirm malformed or invalid parse paths are read-only with respect to DAPR actor state and do not call state save operations. (AC: 10)
  - [x] Do not log protected cursor payloads, signing material, decoded positions, or serialized payload bodies.
- [x] Add focused unit tests for bounds and cursor helper behavior. (AC: 1-5)
  - [x] Add tests that prove standard paginated endpoints use default `20`, clamp `<= 0` to `20`, and cap values above `100`.
  - [x] Add tests that prove audit uses default `100`, clamps `<= 0` to `100`, and caps values above `1000`.
  - [x] Add tests for malformed standard pagination JSON and malformed audit JSON to preserve their intentionally different failure behavior.
  - [x] Add or update cursor scope tests so controller and actor use matching scopes for all four paginated query types.
  - [x] Keep signed cursor tests green and continue asserting cursors do not expose raw tenant IDs, user IDs, event IDs, or logical positions.
- [x] Keep implementation scope tight. (AC: 1-5)
  - [x] Do not change endpoint routes, public query DTO shapes, `PaginatedResult<T>`, or `QueryEnvelope`.
  - [x] Do not change page-size policy values unless a separate product decision updates the epic.
  - [x] Keep new pagination policy and parsing helpers `internal`; do not expose them from contract assemblies or make them part of API documentation. (AC: 11)
  - [x] Do not introduce generic pagination middleware, base controller behavior, or shared query-envelope changes.
  - [x] Do not modify the `Hexalith.EventStore` submodule.
  - [x] Do not add package dependencies or update package versions for this story.

## Dev Notes

### Current Behavior Inventory

| Endpoint family | Default page size | Maximum page size | Invalid size behavior | Malformed payload behavior | Ordering fields |
| --- | ---: | ---: | --- | --- | --- |
| `list-tenants` | `20` | `100` | `<= 0` falls back to `20`; oversized values clamp to `100` | Standard actor pagination payload falls back to first page with `(cursor: null, pageSize: 20)` | Tenant ID |
| `get-tenant-users` | `20` | `100` | `<= 0` falls back to `20`; oversized values clamp to `100` | Standard actor pagination payload falls back to first page with `(cursor: null, pageSize: 20)` | User ID |
| `get-user-tenants` | `20` | `100` | `<= 0` falls back to `20`; oversized values clamp to `100` | Standard actor pagination payload falls back to first page with `(cursor: null, pageSize: 20)` | Tenant ID |
| `get-tenant-audit` | `100` | `1000` | `<= 0` falls back to `100`; oversized values clamp to `1000` | Audit actor payload returns unsuccessful `QueryResult` with `"Invalid audit query payload."` | `Timestamp.UtcTicks` plus `EventId` |

### Policy To Implement

- Standard tenant list endpoints share default page size `20` and maximum page size `100`: `list-tenants`, `get-tenant-users`, and `get-user-tenants`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- Tenant audit keeps its separate default page size `100` and maximum page size `1000`. This is an explicit larger audit-history policy, not a discrepancy to "fix" by forcing all endpoints to `20/100`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- Invalid submitted cursors are rejected at the controller boundary before dispatch and inside the actor as defense in depth. Both paths must continue to return safe invalid-cursor errors and avoid exposing decoded cursor internals. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md`]
- Endpoint-specific ordering remains part of each endpoint's query semantics. This story centralizes common bounds and parsing mechanics only; it must not make audit timestamp/event ordering or tenant/user ID ordering implicit. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`; `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md`]

### Current Code State

- `TenantsQueryController` currently has two private clamp helpers: `ClampPageSize` returns `20` for `<= 0` and caps at `100`; `ClampAuditPageSize` returns `100` for `<= 0` and caps at `1000`. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]
- `TenantsProjectionActor.DeserializePaginationPayload` duplicates standard list defaults and maximums and silently falls back to `(null, 20)` when the payload is empty or malformed JSON. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `TenantsProjectionActor.DeserializeAuditPayload` duplicates audit defaults and maximums while also parsing date range, category, and malformed-payload errors. Keep that audit-specific validation explicit. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `TenantQueryCursorCodec` already owns protected cursor encoding/decoding. It validates query type, scope, version, and non-empty logical position and treats missing/blank cursor as valid "first page". [Source: `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`]
- `TenantQueryCursorScopes` is internal and already used by both controller validation and actor decoding for `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`. Preserve that symmetry. [Source: `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `Paginate<TSource,TResult>` and `PaginateAuditEntries` are separate today. That split is useful: audit cursor positions are timestamp/event pairs, while list-style positions are stable string keys. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]

### Implementation Guardrails

- Prefer a narrow internal helper near existing query infrastructure, for example under `src/Hexalith.Tenants/Queries/`, because both the controller and actor live in the server application project and already reference `Hexalith.Tenants.Queries`.
- Keep helper APIs boring and explicit. A good shape is constants plus `ClampStandardPageSize(int)` and `ClampAuditPageSize(int)`, or an internal `TenantQueryPaginationPolicy` with named standard/audit methods.
- If extracting payload parsing, avoid returning an over-general object that mixes audit and non-audit fields. Use a standard pagination payload record for `cursor/pageSize`; keep audit's date/category validation in its existing method or a clearly audit-specific method.
- Preserve `InvalidCursorResult()` string exactly unless the EventStore query error mapping is intentionally updated in another story.
- Do not add validation that rejects oversized page sizes with HTTP 400. Current policy clamps; changing to rejection would be a product/API behavior change.
- Do not normalize standard malformed pagination payload behavior to audit behavior, or audit malformed payload behavior to standard behavior. Their current divergent outcomes are intentional compatibility constraints.
- Do not make actor parsing trust controller clamping. Actor paths can be reached directly through actor proxy or future routing, so actor-side clamping remains required.
- Do not remove controller validation. It gives clients a safe 400 before dispatch and protects observability from unnecessary actor calls.
- Keep `TenantQueryCursorCodec` and `TenantQueryCursorScopes` mechanical: serialization, signing validation, query type, and scope matching. They must not infer endpoint ordering or choose the logical cursor position.
- Keep comments sparse. Use names such as `StandardDefaultPageSize`, `StandardMaximumPageSize`, `AuditDefaultPageSize`, and `AuditMaximumPageSize` so policy intent is visible without extra prose.

### Elicitation Clarifications

- Treat compatibility as the primary success measure. The refactor is complete only when existing standard and audit pagination behavior can be proven unchanged at the controller boundary, actor boundary, and cursor codec boundary.
- Prefer extracting constants and clamp methods before payload parsing. This keeps the first change mechanically reviewable and makes later parsing centralization easier to compare against existing behavior.
- Keep JSON parse-failure branches side-effect free. Standard malformed payload fallback and audit malformed payload rejection should happen before any projection state mutation and without logging protected payload content.
- Do not broaden cursor helper responsibilities to choose scopes, infer authorization context, derive endpoint ordering, or normalize audit and standard pagination semantics.
- If an edge case currently behaves inconsistently but is not covered by this story's acceptance criteria, document it as deferred work instead of changing behavior inside this refactor.

### Files Likely To Update

- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`: likely location for cursor scope preservation; optionally add related pagination policy only if the file stays cohesive, otherwise create a small sibling file in `Queries`.
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`: likely new internal helper for page-size constants and clamp methods if a new file is cleaner.
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: replace duplicated page-size literals and optionally use extracted standard pagination payload parsing.
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`: replace duplicated clamp helpers with the shared policy while preserving route and ProblemDetails behavior.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: add actor-side default/clamp/malformed payload tests for standard and audit endpoints.
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryCursorCodecTests.cs`: add scope/policy tests if helpers are placed in `Queries`.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: optional only if controller-level ProblemDetails or query-string clamping behavior changes.

### Testing Requirements

- Use xUnit and Shouldly, matching existing tests.
- Actor tests can reuse `CreatePaginationPayload`, `CreateAuditPayload`, `CreateTenantIndexModel`, `SetupTenantIndexState`, `SetupTenantState`, `SetupGlobalAdminState`, `SetupNoGlobalAdmin`, `CreateActor`, and `DeserializePayload`.
- For standard bounds, use a small dataset larger than the maximum where practical, or assert via page item count and `HasMore` on existing `ListTenants` / `GetTenantUsers` paths. Avoid massive fixtures beyond what the test needs.
- For audit maximum, `GetTenantAudit_clamps_page_size_to_one_thousandAsync` already covers the `1000` cap; keep or update it to use the shared constants rather than hard-coded literals.
- Add explicit tests for `pageSize = 0`, negative `pageSize`, and missing `pageSize` so both controller and actor defaults cannot drift later.
- Add page-size boundary tests for null or omitted, zero, negative, exact maximum, and maximum plus one for both standard and audit paths where those inputs can reach the boundary under test.
- Add malformed JSON actor tests:
  - Standard payload malformed JSON should produce a successful first page using default `20` when authorization/state are otherwise valid.
  - Audit payload malformed JSON should return unsuccessful `QueryResult` with `"Invalid audit query payload."`.
- Run at minimum:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryCursorCodecTests`
  - If controller helpers are changed in a way that could affect API responses, also run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`

### Latest Technical Information

- The repository pins the latest supported .NET 10 SDK in `global.json`; as of 2026-05-18 this is `10.0.300`. Package versions are centrally managed in `Directory.Packages.props`. This story needs no dependency upgrade and no new package. [Source: `global.json`; `Directory.Packages.props`]
- Current relevant package versions include Dapr `1.17.9`, Aspire `13.3.3`, Microsoft ASP.NET Core OpenAPI/JWT packages `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. Keep tests and implementation within those existing dependencies. [Source: `Directory.Packages.props`]

### Previous Story Intelligence

- Story 9.1 introduced `ITenantQueryCursorCodec`, Data Protection-backed signed cursors, `TenantQueryCursorScopes`, controller-level invalid cursor ProblemDetails, and actor-level invalid cursor handling. Story 9.5 should reuse and clarify those utilities, not replace them. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md`; `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`]
- Story 9.2 defines current-state keyset continuation after authorization filtering. Page bounds must not change that ordering/filtering model; the shared utility only prevents bounds drift. [Source: `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md`]
- Story 9.3 plans orphan filtering before pagination and disabled-tenant visibility. Story 9.5 must leave that policy intact and avoid changing how visible item sets are produced before pagination. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`]
- Story 9.4 plans actor-layer missing-user guardrails before any state read. Shared parsing/bounds helpers must not move cursor decoding or payload parsing ahead of required authorization guardrails if Story 9.4 has already been implemented by the time this story starts. [Source: `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`]

### Git Intelligence

- Recent commits created Story 9.3 and Story 9.4 contexts and released `1.7.2` / `1.7.3`. The latest source code still contains duplicated pagination literals, so this story remains valid as a narrow refactor target. [Source: `git log -5 --oneline`]
- A recent fix updated Aspire package versions, but this story should not touch Aspire/AppHost files or package versions. [Source: `git log -5 --oneline`; `Directory.Packages.props`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only and should not drive Tenants story scope.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryPaginationPolicyTests` (red: `TenantQueryPaginationPolicy` missing)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (passed: 65)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryPaginationPolicyTests` (passed: 11)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (passed: 350)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryPaginationPolicyTests` (red: standard payload parser missing)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryPaginationPolicyTests` (passed: 19)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (passed: 65)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (passed: 358)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryCursorCodecTests` (passed: 10)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (passed: 360)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (passed: 67)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (transient failure: `TenantsProjectionActorTelemetryTests.QueryAsync_WhenHandlerThrows_ShouldMarkActivityAsErrorAndRecordMetric`, passed when isolated)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTelemetryTests.QueryAsync_WhenHandlerThrows_ShouldMarkActivityAsErrorAndRecordMetric` (passed: 1)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (passed: 362)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (analyzer red: xUnit1030 from `ConfigureAwait(false)` in test method)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (passed: 72)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryPaginationPolicyTests` (passed: 19)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantQueryCursorCodecTests` (passed: 10)
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` (passed: 367)
- `git status --short` / `git diff --stat` (scope check: no route/DTO/package/submodule changes; unrelated `_bmad-output/process-notes` changes left untouched)
- `Select-String ... '- \[ \]'` against this story file (passed: no unchecked tasks/subtasks)
- `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` (passed: 589, skipped: 1)

### Completion Notes List

- Added internal `TenantQueryPaginationPolicy` as the single source of truth for standard and audit query defaults/maximums.
- Replaced controller and actor page-size clamp literals with the shared policy while preserving standard `20/100` and audit `100/1000` behavior.
- Added internal `TenantQueryPaginationPayloadParser` for standard cursor/pageSize payload parsing; audit date/category parsing remains explicit in `TenantsProjectionActor`.
- Added cursor regression coverage for exact scope string formats and an existing list-tenants query/scope/logical-position round trip.
- Added actor regression coverage proving malformed standard payloads fall back to the default first page and malformed audit payloads fail before audit state reads or state writes.
- Added actor boundary tests proving all standard paginated query types cap oversized page sizes at `100`, and audit non-positive sizes fall back to `100`.
- Verified implementation scope stayed internal to the server project with no public contract, route, package, or submodule changes.
- Completed story 9.5 and moved it to review after full solution regression passed.

### File List

- `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPayloadParser.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryCursorCodecTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryPaginationPolicyTests.cs`

### Change Log

- 2026-05-18: Implemented shared pagination bounds/payload helpers, preserved cursor scope behavior, added focused regression coverage, and moved story to review.

## Party-Mode Review

- Date: 2026-05-17T12:10:41+02:00
- Selected story key: `9-5-shared-pagination-bounds-and-cursor-utilities`
- Command/skill invocation used: `/bmad-party-mode 9-5-shared-pagination-bounds-and-cursor-utilities; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Clarify no-behavior-drift expectations so centralization cannot become a page-size policy or API behavior change.
  - Lock down the intentionally divergent malformed payload behavior between standard pagination and audit pagination.
  - Make cursor utility boundaries explicit: shared code may own serialization, signing validation, query type, and scope checks, but not endpoint ordering semantics.
  - Add behavior-parity and boundary test wording for standard and audit defaults, maximums, invalid values, malformed payloads, and cursor scope symmetry.
- Changes applied:
  - Added acceptance criteria for oversized page-size clamping, malformed payload compatibility, and endpoint-owned cursor ordering.
  - Added a current behavior inventory table for endpoint families.
  - Added implementation guardrails for malformed payload divergence and cursor utility boundaries.
  - Added focused test guidance for page-size boundary coverage.
- Findings deferred:
  - Exact helper shape remains an implementation decision as long as it stays internal, has one source of truth, and preserves endpoint behavior.
  - Controller integration test expansion remains conditional on implementation touching API serialization, status mapping, or `ProblemDetails` construction.
- Final recommendation: needs-story-update

## Advanced Elicitation

- Date: 2026-05-17T14:34:34+02:00
- Selected story key: `9-5-shared-pagination-bounds-and-cursor-utilities`
- Command/skill invocation used: `/bmad-advanced-elicitation 9-5-shared-pagination-bounds-and-cursor-utilities`
- Batch 1 method names: Red Team vs Blue Team; Failure Mode Analysis; Self-Consistency Validation; First Principles Analysis; Critique and Refine
- Reshuffled Batch 2 method names: Pre-mortem Analysis; Security Audit Personas; Architecture Decision Records; Occam's Razor Application; Active Recall Testing
- Findings summary:
  - Compatibility needs explicit coverage for cursors issued before the refactor, not just newly encoded cursors.
  - Parse-failure branches need a stated no-state-write and no-sensitive-logging constraint so shared helpers cannot accidentally add side effects.
  - Helper boundaries should remain internal and server-local; exposing a public pagination abstraction would exceed the story's refactor scope.
  - The implementation should preserve current divergent standard-vs-audit malformed payload behavior even where a more uniform helper might look cleaner.
- Changes applied:
  - Added acceptance criteria for existing cursor compatibility, read-only parse failures, and internal-only helper scope.
  - Added task guidance for cursor compatibility regression coverage and DAPR state save avoidance on parse failures.
  - Added elicitation clarifications covering extraction order, side-effect-free parsing, cursor helper boundaries, and deferred edge-case handling.
- Findings deferred:
  - Exact helper type names and file placement remain implementation decisions as long as helper APIs stay internal and behavior-compatible.
  - Any cleanup of inconsistent but pre-existing edge behavior remains deferred unless it is already covered by this story's acceptance criteria.
- Final recommendation: ready-for-dev
