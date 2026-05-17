# Story 9.5: Shared Pagination Bounds and Cursor Utilities

Status: ready-for-dev

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

## Tasks / Subtasks

- [ ] Introduce one shared pagination bounds policy for tenant queries. (AC: 1, 2, 5)
  - [ ] Add a small internal server-side utility or constants type for standard query pagination values: default page size `20`, maximum page size `100`, audit default page size `100`, and audit maximum page size `1000`.
  - [ ] Name the shared policy by endpoint family rather than caller location, for example standard tenant queries and audit queries; do not recreate controller-only and actor-only constants.
  - [ ] Replace duplicated literals in `TenantsQueryController.ClampPageSize`, `TenantsQueryController.ClampAuditPageSize`, `TenantsProjectionActor.DeserializePaginationPayload`, and `TenantsProjectionActor.DeserializeAuditPayload`.
  - [ ] Keep the current behavior exactly: `pageSize <= 0` falls back to the endpoint default, standard list endpoints cap at `100`, and audit caps at `1000`.
  - [ ] Do not move the policy into public contract DTOs unless implementation proves a public surface is needed.
- [ ] Centralize pagination payload parsing without hiding endpoint-specific behavior. (AC: 1, 3, 4)
  - [ ] Extract the common `cursor` and `pageSize` parsing used by standard paginated endpoints into a focused helper that still lets audit parsing handle `from`, `to`, and `category` explicitly.
  - [ ] Preserve existing malformed JSON behavior: standard pagination payloads fall back to `(cursor: null, pageSize: 20)`, while audit payloads return `"Invalid audit query payload."`.
  - [ ] Preserve the current safe handling for missing, null, non-string cursor, non-number page size, and negative/zero page size values.
  - [ ] Keep endpoint ordering keys explicit at call sites: tenant IDs for `list-tenants` and `get-user-tenants`, user IDs for `get-tenant-users`, and `Timestamp.UtcTicks + EventId` for audit.
  - [ ] Where Story 9.4 guardrails exist, keep actor validation and authorization guardrails ahead of page-size parsing or cursor decoding.
- [ ] Clarify and preserve cursor scope utilities. (AC: 3, 4)
  - [ ] Keep `TenantQueryCursorScopes` as the single place for scope strings used by controller validation and actor decoding.
  - [ ] Do not rename existing scope strings (`user:{userId}`, `tenant:{tenantId}`, `target-user:{targetUserId}`, and audit filter scope) because existing signed cursors rely on exact query type and scope matching.
  - [ ] Keep signed cursor payload shape, signing format, query type matching, scope matching, error behavior, and public response fields unchanged.
  - [ ] If helper names are changed, update both controller and actor call sites in the same patch so cursor validation stays symmetric.
  - [ ] Do not introduce a generic cursor abstraction that obscures which logical position each endpoint stores.
- [ ] Preserve invalid cursor and invalid audit payload error behavior. (AC: 4)
  - [ ] Keep controller-level invalid cursor responses as HTTP 400 ProblemDetails with reason code `invalid-cursor`.
  - [ ] Keep actor-level malformed cursor results as `new QueryResult(false, default, ErrorMessage: "Invalid cursor.")` so existing EventStore query error mapping remains unchanged.
  - [ ] Keep invalid audit category and `from > to` handling explicit in audit parsing.
  - [ ] Do not log protected cursor payloads, signing material, decoded positions, or serialized payload bodies.
- [ ] Add focused unit tests for bounds and cursor helper behavior. (AC: 1-5)
  - [ ] Add tests that prove standard paginated endpoints use default `20`, clamp `<= 0` to `20`, and cap values above `100`.
  - [ ] Add tests that prove audit uses default `100`, clamps `<= 0` to `100`, and caps values above `1000`.
  - [ ] Add tests for malformed standard pagination JSON and malformed audit JSON to preserve their intentionally different failure behavior.
  - [ ] Add or update cursor scope tests so controller and actor use matching scopes for all four paginated query types.
  - [ ] Keep signed cursor tests green and continue asserting cursors do not expose raw tenant IDs, user IDs, event IDs, or logical positions.
- [ ] Keep implementation scope tight. (AC: 1-5)
  - [ ] Do not change endpoint routes, public query DTO shapes, `PaginatedResult<T>`, or `QueryEnvelope`.
  - [ ] Do not change page-size policy values unless a separate product decision updates the epic.
  - [ ] Do not introduce generic pagination middleware, base controller behavior, or shared query-envelope changes.
  - [ ] Do not modify the `Hexalith.EventStore` submodule.
  - [ ] Do not add package dependencies or update package versions for this story.

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

- The repository pins .NET SDK `10.0.103` in `global.json` and centrally manages package versions in `Directory.Packages.props`. This story needs no dependency upgrade and no new package. [Source: `global.json`; `Directory.Packages.props`]
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

### Completion Notes List

### File List

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
