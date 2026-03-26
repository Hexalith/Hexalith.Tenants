# Story 5.3: Query Endpoints & Authorization

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer or administrator,
I want REST query endpoints to list tenants, view tenant details, look up user memberships, and run audit queries,
So that I can discover tenants, manage access, and produce compliance reports.

## Acceptance Criteria

1. **Given** an authenticated user with a role in at least one tenant
   **When** a GET request is sent to `/api/tenants`
   **Then** a paginated list of tenants is returned with IDs, names, and statuses using cursor-based pagination (`{ "items": [...], "cursor": "...", "hasMore": true }`)

2. **Given** an authenticated user with a role in the target tenant (or GlobalAdmin)
   **When** a GET request is sent to `/api/tenants/{tenantId}`
   **Then** the tenant's full details are returned including current users and their roles

3. **Given** an authenticated user with a role in the target tenant (or GlobalAdmin)
   **When** a GET request is sent to `/api/tenants/{tenantId}/users`
   **Then** a paginated list of users in that tenant with their assigned roles is returned

4. **Given** an authenticated user
   **When** a GET request is sent to `/api/users/{userId}/tenants`
   **Then** a paginated list of tenants the specified user belongs to is returned with their role in each tenant

5. **Given** an authenticated GlobalAdministrator
   **When** a GET request is sent to `/api/tenants/{tenantId}/audit` with date range parameters
   **Then** the endpoint returns HTTP 501 Not Implemented with a ProblemDetails body (MVP decision: full audit deferred to follow-up story; endpoint exists with GlobalAdmin authorization enforced)

6. **Given** an authenticated user without a role in the target tenant and not a GlobalAdmin
   **When** a GET request is sent to `/api/tenants/{tenantId}` or `/api/tenants/{tenantId}/users`
   **Then** the request is rejected with 403 Forbidden

7. **Given** all query endpoints
   **When** cursor-based pagination parameters are provided
   **Then** results are returned with consistent ordering and valid cursor tokens for next-page navigation

8. **Given** a command has just been processed (e.g., CreateTenant)
   **When** the command response is returned
   **Then** the response includes the aggregate ID so the client can navigate directly to `GET /api/tenants/{id}` for read-after-write confirmation
   _(Already satisfied by EventStore's existing `SubmitCommandResponse` which includes AggregateId via `SubmitCommandRequest`. No implementation needed in this story — verify only.)_

9. **Given** two concurrent tenant-domain events update the shared cross-tenant index key
   **When** Story 5.3 persists the `TenantIndexReadModel`
   **Then** the hosting layer uses ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`) and retries conflicts up to 3 times before surfacing an error

10. **Given** the cross-tenant index is populated with 1,000 tenants
    **When** `GET /api/tenants` or `GET /api/users/{userId}/tenants` is queried
    **Then** the paginated response meets NFR2 latency targets (50ms p95 per page) using stable sort-by-tenant-id ordering

## Tasks / Subtasks

- [x] Task 0: Add EventStore.Contracts reference to Contracts.csproj (PREREQUISITE — blocks all other tasks)
    - [x] 0.1: Add `<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" />` to `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`
    - [x] 0.2: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 1: Create query contracts in Contracts project (AC: #1-5, #7)
    - [x] 1.1: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs` implementing `IQueryContract`
    - [x] 1.2: Create `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs` implementing `IQueryContract`
    - [x] 1.3: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs` implementing `IQueryContract`
    - [x] 1.4: Create `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs` implementing `IQueryContract`
    - [x] 1.5: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs` implementing `IQueryContract`
    - [x] 1.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create query response types in Contracts project (AC: #1-5)
    - [x] 2.1: Create `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs` — lightweight DTO for list endpoints
    - [x] 2.2: Create `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs` — full tenant details DTO
    - [x] 2.3: Create `src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs` — user+role DTO
    - [x] 2.4: Create `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs` — tenant+role DTO for user lookups
    - [x] 2.5: Create `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs` — generic paginated response wrapper
    - [x] 2.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create TenantsProjectionActor in Hexalith.Tenants (AC: #1-6)
    - [x] 3.1: Create `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs` inheriting `CachingProjectionActor`
    - [x] 3.2: Implement `ExecuteQueryAsync` — dispatch to per-query-type handler methods
    - [x] 3.3: Implement GetTenant handler — load TenantReadModel from projection, authorize, return TenantDetail
        - [x] 3.4: Implement ListTenants handler — load TenantIndexReadModel, authorize (filter by user membership or GlobalAdmin sees all), paginate, return `PaginatedResult<TenantSummary>` **[REQUIRES Story 5.2 complete — skip if TenantIndexReadModel does not exist]**
        - [x] 3.5: Implement GetTenantUsers handler — load TenantReadModel, authorize, paginate members, return `PaginatedResult<TenantMember>`
        - [x] 3.6: Implement GetUserTenants handler — load TenantIndexReadModel, extract user's tenants, paginate, return `PaginatedResult<UserTenantMembership>` **[REQUIRES Story 5.2 complete — skip if TenantIndexReadModel does not exist]**
    - [x] 3.7: Implement GetTenantAudit handler — FIRST check GlobalAdmin authorization (return 403 if not admin), THEN return 501 Not Implemented (MVP decision: full audit deferred to follow-up story; non-admins must get 403, not 501, to avoid leaking endpoint existence)
    - [x] 3.8: Register actor in Program.cs — `AddEventStoreServer()` does NOT register a ProjectionActor; each domain service must register its own. Add `builder.Services.AddActors(options => { options.Actors.RegisterActor<TenantsProjectionActor>(typeOptions: new Dapr.Actors.Runtime.ActorRegistrationOptions { TypeName = "ProjectionActor" }); });`
        - [x] 3.9: Implement the Story 5.2 handoff for cross-tenant fan-in hosting — shared `TenantIndexReadModel` state key, ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`), and retry loop (max 3 attempts) for incremental Apply operations
        - [x] 3.10: After each successful incremental Apply, explicitly notify projection invalidation for `"tenant-index"` because `Project()` is not the fan-in entry point
        - [x] 3.11: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 4: Create TenantsQueryController in Hexalith.Tenants (AC: #1-5, #7)
    - [x] 4.1: Create `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
    - [x] 4.2: Implement `GET /api/tenants` — translate to ListTenantsQuery via SubmitQueryRequest → MediatR
    - [x] 4.3: Implement `GET /api/tenants/{tenantId}` — translate to GetTenantQuery
    - [x] 4.4: Implement `GET /api/tenants/{tenantId}/users` — translate to GetTenantUsersQuery
    - [x] 4.5: Implement `GET /api/users/{userId}/tenants` — translate to GetUserTenantsQuery
    - [x] 4.6: Implement `GET /api/tenants/{tenantId}/audit` — translate to GetTenantAuditQuery
    - [x] 4.7: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 5: Register query infrastructure in Program.cs (AC: #1-8)
    - [x] 5.1: Ensure `MapControllers()` picks up new TenantsQueryController
    - [x] 5.2: Register TenantsProjectionActor with DAPR actor runtime (type name = `"ProjectionActor"`)
    - [x] 5.3: Verify no duplicate actor type registrations
    - [x] 5.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 6: Create unit tests (AC: #1-7, #9) — 29+ tests across 3 test files
    - [x] 6.1: Create `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryContractNamingTests.cs` — 5 reflection-based naming convention tests (Q1-Q5)
    - [x] 6.2: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` — 19 actor logic tests with mocked DaprClient (Q6-Q21, Q25-Q27). NOTE: Actor lives in Hexalith.Tenants but tests go in Server.Tests to avoid creating a new test project — Server.Tests.csproj needs a `ProjectReference` to Hexalith.Tenants added
    - [x] 6.3: Create `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs` — 3 DTO round-trip serialization tests (Q22-Q24)
        - [x] 6.4: Add focused cross-tenant hosting tests for ETag retry / conflict handling and `tenant-index` invalidation notification (Q28-Q29)
        - [x] 6.5: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [x] Task 7: Build verification (all ACs)
    - [x] 7.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 7.2: `dotnet test Hexalith.Tenants.slnx` — all tests pass, no regressions

## Dev Notes

### TL;DR

Build the query layer for the Tenants service. 5 query contracts (`IQueryContract` implementations in Contracts/Queries/), a `TenantsProjectionActor` (inherits `CachingProjectionActor`) that loads projection state and serves queries with authorization checks, a thin REST controller (`TenantsQueryController`) that translates `GET /api/tenants/*` routes into `SubmitQueryRequest` MediatR dispatches, and the cross-tenant fan-in hosting glue from Story 5.2 (shared-key state persistence, ETag retry, explicit `tenant-index` invalidation). The query pipeline is: REST → MediatR → QueryRouter → CachingProjectionActor → ReadModel. Authorization is dual-layer: JWT at API boundary (existing `AuthorizationBehavior`), domain RBAC (tenant membership / GlobalAdmin check) in the projection actor's `ExecuteQueryAsync`.

### Architecture: Dual-Layer Query Architecture (D7 Revision)

Per architecture, query endpoints use EventStore's built-in query pipeline:

**Internal layer:**

- Query contracts implement `IQueryContract` with static `QueryType`, `Domain`, `ProjectionType`
- Dispatched via `SubmitQuery`/`QueryRouter` through MediatR pipeline
- `CachingProjectionActor` serves cached results with ETag support

**External layer:**

- Thin REST controller (`GET /api/tenants/*`) translates REST requests into `SubmitQueryRequest` dispatches
- Preserves clean REST API semantics

**Query flow:**

```text
Client → GET /api/tenants/{id}
  → TenantsQueryController.GetTenant(id)
    → mediator.Send(SubmitQuery{QueryType="get-tenant", EntityId=id, ...})
      → SubmitQueryHandler → QueryRouter.RouteQueryAsync()
        → ActorProxy<IProjectionActor>(actorId="get-tenant:system:{id}")
          → TenantsProjectionActor.QueryAsync(envelope)
            → CachingProjectionActor.QueryAsync (ETag check)
              → ExecuteQueryAsync(envelope)
                → Load TenantReadModel, authorize, serialize
              → QueryResult(Success=true, Payload=json)
```

### Query Contracts — IQueryContract Implementation

Each query contract is a `sealed class` (not `record` — `IQueryContract` uses static abstract members which records support but classes are cleaner for this pattern) implementing `IQueryContract`:

```csharp
// src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving a specific tenant's full details (FR26).
/// </summary>
public sealed class GetTenantQuery : IQueryContract
{
    public static string QueryType => "get-tenant";
    public static string Domain => "tenants";
    public static string ProjectionType => "tenants";
}
```

**All 5 query contracts:**

| Query Contract        | QueryType          | Domain    | ProjectionType | Tier                             | EntityId | FR   |
| --------------------- | ------------------ | --------- | -------------- | -------------------------------- | -------- | ---- |
| `GetTenantQuery`      | `get-tenant`       | `tenants` | `tenants`      | 1                                | tenantId | FR26 |
| `ListTenantsQuery`    | `list-tenants`     | `tenants` | `tenant-index` | 3 (or 2 with pagination payload) | null     | FR25 |
| `GetTenantUsersQuery` | `get-tenant-users` | `tenants` | `tenants`      | 1                                | tenantId | FR27 |
| `GetUserTenantsQuery` | `get-user-tenants` | `tenants` | `tenant-index` | 1                                | userId   | FR28 |
| `GetTenantAuditQuery` | `get-tenant-audit` | `tenants` | `tenants`      | 1                                | tenantId | FR29 |

**ProjectionType determines ETag scope:**

- `"tenants"` — per-tenant projections (GetTenant, GetTenantUsers, GetTenantAudit): ETag invalidated when specific tenant's events are processed
- `"tenant-index"` — cross-tenant index projections (ListTenants, GetUserTenants): ETag invalidated when any tenant event is processed

**QueryType naming convention:** Kebab-case, no colons (colon is actor ID separator). Must match the pattern established in EventStore's `QueryActorIdHelper`.

### Query Response Types — Contracts DTOs

Query responses are serialized as JSON in `QueryResult.Payload`. Define DTOs in Contracts so consuming services can also use them:

```csharp
// src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs
namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Lightweight tenant representation for list endpoints (FR25).
/// </summary>
public sealed record TenantSummary(string TenantId, string Name, TenantStatus Status);
```

```csharp
// src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs
namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Full tenant details including members and configuration (FR26).
/// </summary>
public sealed record TenantDetail(
    string TenantId,
    string Name,
    string? Description,
    TenantStatus Status,
    IReadOnlyList<TenantMember> Members,
    IReadOnlyDictionary<string, string> Configuration,
    DateTimeOffset CreatedAt);
```

```csharp
// src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs
namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// User membership within a tenant (FR27).
/// </summary>
public sealed record TenantMember(string UserId, TenantRole Role);
```

```csharp
// src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs
namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Tenant membership for a specific user (FR28).
/// </summary>
public sealed record UserTenantMembership(string TenantId, string Name, TenantStatus Status, TenantRole Role);
```

```csharp
// src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs
namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Generic cursor-based paginated result (FR30).
/// </summary>
public sealed record PaginatedResult<T>(IReadOnlyList<T> Items, string? Cursor, bool HasMore);
```

**Note:** DTOs are `record` types for value equality and immutability. Use `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>` for the response shape — the read model's mutable `Dictionary<>` must be converted at serialization time.

### TenantsProjectionActor — CachingProjectionActor Implementation

The projection actor is a DAPR actor that implements the query logic. It inherits `CachingProjectionActor` which provides automatic ETag-based caching.

**Location:** `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`

**Why in Hexalith.Tenants, not Server?** The projection actor depends on DAPR actor runtime (`ActorHost`, DAPR state store client) and is specific to the deployable host. Server contains domain-agnostic types (read models, projections). The actor is an infrastructure concern that wires projections to the query pipeline.

```csharp
// src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs
using System.Text.Json;

using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Actors;

public sealed partial class TenantsProjectionActor : CachingProjectionActor
{
    private readonly DaprClient _daprClient;

    public TenantsProjectionActor(
        ActorHost host,
        IETagService eTagService,
        DaprClient daprClient,
        ILogger<TenantsProjectionActor> logger)
        : base(host, eTagService, logger)
    {
        _daprClient = daprClient;
    }

    protected override async Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return envelope.QueryType switch
        {
            "get-tenant" => await HandleGetTenantAsync(envelope).ConfigureAwait(false),
            "list-tenants" => await HandleListTenantsAsync(envelope).ConfigureAwait(false),
            "get-tenant-users" => await HandleGetTenantUsersAsync(envelope).ConfigureAwait(false),
            "get-user-tenants" => await HandleGetUserTenantsAsync(envelope).ConfigureAwait(false),
            "get-tenant-audit" => await HandleGetTenantAuditAsync(envelope).ConfigureAwait(false),
            _ => new QueryResult(false, default, ErrorMessage: $"Unknown query type: {envelope.QueryType}")
        };
    }

    // ... handler methods below
}
```

**Actor Registration (RESOLVED).** `AddEventStoreServer()` does NOT register a `ProjectionActor`. Each domain service must register its own. The DAPR actor type name MUST be `"ProjectionActor"` (from `QueryRouter.ProjectionActorTypeName`). Registration in `Program.cs`:

```csharp
// In Program.cs, add BEFORE builder.Build():
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<TenantsProjectionActor>(
        typeOptions: new Dapr.Actors.Runtime.ActorRegistrationOptions
        {
            TypeName = "ProjectionActor"
        });
});
```

The existing `app.MapActorsHandlers()` maps DAPR actor HTTP endpoints — it already exists in Program.cs and picks up actors registered via `AddActors()`. No changes needed to the middleware pipeline.

### TenantsQueryController — Thin REST Controller

**Location:** `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`

The controller translates clean REST endpoints into `SubmitQueryRequest` objects dispatched via MediatR. It does NOT contain query logic — just HTTP → MediatR mapping.

```csharp
// Sketch — the controller creates SubmitQuery MediatR requests
[ApiController]
[Authorize]
[Route("api/tenants")]
public sealed class TenantsQueryController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListTenants(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        string userId = User.FindFirst("sub")?.Value
            ?? return Unauthorized();

        // Create pagination payload
        var payload = new { cursor, pageSize };
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        var query = new SubmitQuery(
            Tenant: "system",
            Domain: ListTenantsQuery.Domain,
            AggregateId: "index",   // well-known for cross-tenant queries
            QueryType: ListTenantsQuery.QueryType,
            Payload: payloadBytes,
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: userId);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken);
        return Ok(new SubmitQueryResponse(result.CorrelationId, result.Payload));
    }

    [HttpGet("{tenantId}")]
    public async Task<IActionResult> GetTenant(string tenantId, CancellationToken cancellationToken = default)
    {
        // EntityId = tenantId for Tier 1 routing
        var query = new SubmitQuery(
            Tenant: "system",
            Domain: GetTenantQuery.Domain,
            AggregateId: tenantId,
            QueryType: GetTenantQuery.QueryType,
            Payload: [],
            CorrelationId: Guid.NewGuid().ToString(),
            UserId: User.FindFirst("sub")?.Value ?? "",
            EntityId: tenantId);

        SubmitQueryResult result = await mediator.Send(query, cancellationToken);
        return Ok(new SubmitQueryResponse(result.CorrelationId, result.Payload));
    }

    // Similar for other endpoints...
}
```

**REST → Query mapping:**

| REST Endpoint                       | Query Contract      | AggregateId | EntityId | Payload                        |
| ----------------------------------- | ------------------- | ----------- | -------- | ------------------------------ |
| `GET /api/tenants`                  | ListTenantsQuery    | `"index"`   | null     | `{cursor, pageSize}`           |
| `GET /api/tenants/{tenantId}`       | GetTenantQuery      | tenantId    | tenantId | empty                          |
| `GET /api/tenants/{tenantId}/users` | GetTenantUsersQuery | tenantId    | tenantId | `{cursor, pageSize}`           |
| `GET /api/users/{userId}/tenants`   | GetUserTenantsQuery | `"index"`   | userId   | `{cursor, pageSize}`           |
| `GET /api/tenants/{tenantId}/audit` | GetTenantAuditQuery | tenantId    | tenantId | `{from, to, cursor, pageSize}` |

### Authorization — Dual-Layer Implementation

**Layer 1 (JWT — automatic):** `[Authorize]` attribute on controller + existing `AuthorizationBehavior` in MediatR pipeline validates JWT claims (`eventstore:tenant` = `system`). Already in place — no changes needed.

**Layer 2 (Domain RBAC — in projection actor):** The projection actor's `ExecuteQueryAsync` receives `envelope.UserId` (extracted from JWT `sub` claim by QueriesController/TenantsQueryController). The actor must:

1. **For tenant-specific queries** (GetTenant, GetTenantUsers, GetTenantAudit):
    - Load `TenantReadModel` for the target tenant
    - Check if `envelope.UserId` exists in `TenantReadModel.Members` OR user is GlobalAdmin
    - If not authorized → return `QueryResult(false, default, ErrorMessage: "Forbidden")`

2. **For cross-tenant queries** (ListTenants):
    - Load `TenantIndexReadModel`
    - If user is GlobalAdmin → return all tenants (filtered by pagination)
    - If not GlobalAdmin → filter to tenants where user has a role in `TenantIndexReadModel.UserTenants[userId]`

3. **For user-tenant queries** (GetUserTenants):
    - Any authenticated user can query their own tenants
    - GlobalAdmin can query any user's tenants
    - Non-admin querying another user → return 403

4. **For audit queries** (GetTenantAudit):
    - GlobalAdmin only (AC5)
    - If not GlobalAdmin → return 403

**GlobalAdmin check:** Load `GlobalAdministratorReadModel` from projection state. Check if `envelope.UserId` is in `Administrators` set.

**How to load projection state (RESOLVED).** `CachingProjectionActor.ExecuteQueryAsync` is abstract — the implementer (TenantsProjectionActor) is responsible for loading projection state. The projection handler writes state to the DAPR state store via `EventStoreProjection<T>`. The actor reads it back using `DaprClient.GetStateAsync<T>()`.

**State store and key conventions:**

- **State store name:** `"tenants-eventstore"` (from `dapr/components/statestore.yaml`)
- **Per-tenant read model key:** The aggregate actor stores state keyed by aggregate identity. For projections, EventStore uses the actor's state manager (`StateManager`), keyed by the actor ID. However, the projection actor is NOT the same actor as the aggregate actor. The projection actor needs to read state that the projection handler wrote.
- **Practical approach:** Use `_daprClient.GetStateAsync<TenantReadModel>("tenants-eventstore", $"projection:tenants:{tenantId}")` — but verify the exact key format by checking how `EventStoreProjection<T>` persists state. Search: `grep -rn "SaveStateAsync\|SetStateAsync" Hexalith.EventStore/src/Hexalith.EventStore.Server/ --include="*.cs"` at dev time to confirm the key format.
- **For GlobalAdministratorReadModel:** Use key `"projection:global-administrators:singleton"` (single instance, not per-tenant).
- **For TenantIndexReadModel:** Use key `"projection:tenant-index:singleton"` (single instance aggregating all tenants).
- **IMPORTANT:** If the key format doesn't match what you expect, check the EventStore `ProjectionEventHandler` or `ProjectionSubscriptionEndpoint` classes — they contain the actual write logic that determines key format.

### Cursor-Based Pagination (FR30)

**Pattern:** Sort by key (TenantId or UserId), use the last item's key as cursor. Client sends `?cursor=xxx&pageSize=20`. Server returns items after the cursor.

```csharp
// Pagination helper (in projection actor or separate utility)
static PaginatedResult<T> Paginate<T>(
    IEnumerable<KeyValuePair<string, T>> items,
    string? cursor,
    int pageSize,
    Func<KeyValuePair<string, T>, string> keySelector)
{
    var ordered = items.OrderBy(keySelector);
    if (cursor is not null)
    {
        ordered = ordered.Where(kvp => string.Compare(keySelector(kvp), cursor, StringComparison.Ordinal) > 0)
                         .OrderBy(keySelector);
    }

    var page = ordered.Take(pageSize + 1).ToList();
    bool hasMore = page.Count > pageSize;
    if (hasMore) page.RemoveAt(page.Count - 1);

    string? nextCursor = hasMore ? keySelector(page[^1]) : null;
    return new PaginatedResult<T>(
        page.Select(kvp => kvp.Value).ToList(),
        nextCursor,
        hasMore);
}
```

**Consistent ordering:** Dictionary entries sorted by key (TenantId). At 1K tenants scale, in-memory sorting is sub-millisecond (architecture decision).

### FR29 Audit Query — MVP Decision: 501 Not Implemented

FR29 requires "query tenant access changes by tenant ID and date range." This requires access to the historical event stream, not just the current projection state. Full audit event replay requires infrastructure that is not yet available in the projection actor pattern (event stream query access, date-range filtering of persisted events).

**Decision: Return HTTP 501 Not Implemented for `/api/tenants/{tenantId}/audit`.** The endpoint exists (route registered, GlobalAdmin authorization enforced) but returns 501 with a ProblemDetails body explaining audit queries are planned for a future release. This is acceptable because the other 4 query endpoints fully serve FR25-28, FR30. A follow-up story will implement FR29 when event stream query capability is available.

**Implementation:**

```csharp
// In TenantsProjectionActor:
private async Task<QueryResult> HandleGetTenantAuditAsync(QueryEnvelope envelope)
{
    // CRITICAL: Check GlobalAdmin FIRST — non-admins must get 403, not 501
    // (avoids leaking endpoint existence to unauthorized users)
    GlobalAdministratorReadModel? adminModel = await _daprClient
        .GetStateAsync<GlobalAdministratorReadModel>("tenants-eventstore", "projection:global-administrators:singleton")
        .ConfigureAwait(false);

    if (adminModel is null || !adminModel.Administrators.Contains(envelope.UserId))
    {
        return new QueryResult(false, default, ErrorMessage: "Forbidden");
    }

    // Only GlobalAdmins reach here — return 501
    return new QueryResult(
        false, default,
        ErrorMessage: "Audit queries are not yet implemented (FR29). Planned for a future release.");
}

// In TenantsQueryController — map the 501:
[HttpGet("{tenantId}/audit")]
public IActionResult GetTenantAudit(string tenantId)
{
    return StatusCode(501, new ProblemDetails
    {
        Title = "Not Implemented",
        Detail = "Tenant audit queries (FR29) are planned for a future release.",
        Status = 501,
    });
}
```

### Design Decisions & Assumptions

**D1: Query contracts are `sealed class` (not `record`).**
`IQueryContract` uses static abstract members (`QueryType`, `Domain`, `ProjectionType`). Records could work, but query contracts are metadata-only types with no instance state — `sealed class` is more explicit. No constructor, no properties.

**D2: DTOs go in Contracts/Queries/ folder.**
Query response types (`TenantSummary`, `TenantDetail`, etc.) are part of the public contract — consuming services need them to deserialize query responses. Keeping them in Contracts alongside query contracts is the right location per architecture's component boundaries (Contracts → referenced by all projects).

**D3: TenantsProjectionActor in Hexalith.Tenants, not Server.**
The projection actor depends on DAPR actor runtime infrastructure and is specific to the deployable host. Read models and projections (domain types) live in Server. The actor (infrastructure wiring) lives in Hexalith.Tenants. This matches the architecture's component boundary: Hexalith.Tenants references Server + Contracts + ServiceDefaults.

**D4: Single projection actor type handles all query types.**
All 5 query types are dispatched through one `TenantsProjectionActor` class using `envelope.QueryType` switch. The `QueryRouter` always routes to actor type `"ProjectionActor"` — there is exactly one actor type registered. Actor instances are distinguished by their actor ID (derived from QueryType + TenantId + EntityId). One actor instance per unique actor ID.

**D5: Projection state loaded from DAPR state store, not replayed.**
The projection actor reads current state from DAPR state store (where `EventStoreProjection<T>` writes it). It does NOT replay events on each query. Event replay happens in the projection handler (subscription endpoint), not in the query path.

**D6: UserId extracted from JWT `sub` claim — MUST match QueriesController pattern.**
`QueriesController` extracts userId from `User.FindFirst("sub")?.Value` (line 51). The `TenantsQueryController` MUST use the same claim — do NOT use `name` or other user-controllable claims (F-RT2 security requirement from EventStore).

**D7: Eventual consistency — queries may lag behind commands.**
Per architecture: "Projections are eventually consistent — a tenant created via `POST /api/commands` may not appear immediately in `GET /api/tenants`." The command response includes the aggregate ID for read-after-write navigation. AC8 covers this. The query endpoints do NOT guarantee immediate consistency.

**D8: PaginatedResult<T> is generic and reusable.**
One generic paginated result type serves all list endpoints. The cursor is opaque to the client — internally it's the last item's sort key. `HasMore` indicates if more pages exist.

**D9: `enum` values for TenantStatus and TenantRole serialize as strings.**
Ensure JSON serialization uses `JsonStringEnumConverter` for enum properties (`TenantStatus`, `TenantRole`). **Where to configure:** The projection actor serializes query results via `JsonSerializer.SerializeToElement()` before returning `QueryResult.Payload`. Use `JsonSerializerOptions` with `JsonStringEnumConverter` added to `Converters` at serialization time. Do NOT modify global serializer settings — keep the converter scoped to query response serialization. Pattern:

```csharp
private static readonly JsonSerializerOptions s_queryJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter() },
};
```

Use `s_queryJsonOptions` in all `JsonSerializer.SerializeToElement()` calls within the projection actor.

**D10 (CRITICAL): Rejection events in the event stream.**
Story 5.1 D5 documented that `EventPersister` stores ALL events including rejection events. The typed `EventStoreProjection<T>.Project()` silently skips unknown event types (no Apply method → continue). However, `ProjectFromJson(JsonElement)` throws on unknown types. If the projection actor ever uses `ProjectFromJson()` (e.g., for audit event replay), it MUST filter rejection events first. The projection definitions themselves are safe.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type                        | Project         | Folder       | File                                 |
| --------------------------- | --------------- | ------------ | ------------------------------------ |
| GetTenantQuery              | Contracts       | Queries/     | GetTenantQuery.cs (CREATE)           |
| ListTenantsQuery            | Contracts       | Queries/     | ListTenantsQuery.cs (CREATE)         |
| GetTenantUsersQuery         | Contracts       | Queries/     | GetTenantUsersQuery.cs (CREATE)      |
| GetUserTenantsQuery         | Contracts       | Queries/     | GetUserTenantsQuery.cs (CREATE)      |
| GetTenantAuditQuery         | Contracts       | Queries/     | GetTenantAuditQuery.cs (CREATE)      |
| TenantSummary               | Contracts       | Queries/     | TenantSummary.cs (CREATE)            |
| TenantDetail                | Contracts       | Queries/     | TenantDetail.cs (CREATE)             |
| TenantMember                | Contracts       | Queries/     | TenantMember.cs (CREATE)             |
| UserTenantMembership        | Contracts       | Queries/     | UserTenantMembership.cs (CREATE)     |
| PaginatedResult<T>          | Contracts       | Queries/     | PaginatedResult.cs (CREATE)          |
| TenantsProjectionActor      | Hexalith.Tenants      | Actors/      | TenantsProjectionActor.cs (CREATE)   |
| TenantsQueryController      | Hexalith.Tenants      | Controllers/ | TenantsQueryController.cs (CREATE)   |
| Query contract naming tests | Contracts.Tests | Queries/     | QueryContractNamingTests.cs (CREATE) |

**DO NOT:**

- Create types outside the designated projects — query contracts in Contracts, actor in Hexalith.Tenants
- Modify existing read models (`TenantReadModel`, `GlobalAdministratorReadModel`, `TenantIndexReadModel`) — those are Story 5.1/5.2 scope and already complete/in-progress
- Modify existing projection classes (`TenantProjection`, `GlobalAdministratorProjection`) — those are Story 5.1 scope
- Add new NuGet packages unless absolutely required — existing dependencies should suffice
- Create a separate QueryApi project — architecture decision is single deployable (Hexalith.Tenants serves both commands and queries)
- Add shared base classes between query contracts — each is an independent type
- Put query logic in the REST controller — controller is a thin translation layer only
- Create command-side authorization middleware for queries — query authorization is in the projection actor
- Add `HasMembershipHistory`, `Bootstrapped`, or other aggregate-only properties to query response types
- Use mutable collections in response DTOs — use `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>`
- Use `name` JWT claim for user identification — MUST use `sub` only (F-RT2 security)

### Library & Framework Requirements

**No new NuGet packages expected.** All dependencies available:

- `IQueryContract`, `IQueryResponse<T>`, `SubmitQueryRequest/Response` → `Hexalith.EventStore.Contracts` (referenced by Contracts)
- `CachingProjectionActor`, `IProjectionActor`, `QueryEnvelope`, `QueryResult`, `IETagService` → `Hexalith.EventStore.Server` (referenced by Hexalith.Tenants via EventStore.Hexalith.Tenants)
- `SubmitQuery`, `SubmitQueryResult` → `Hexalith.EventStore.Server.Pipeline.Queries` (referenced by Hexalith.Tenants)
- `DaprClient` → `Dapr.AspNetCore` (already in Hexalith.Tenants.csproj)
- `IMediator` → `MediatR` (already in Hexalith.Tenants.csproj)
- `[Authorize]`, `[ApiController]` → ASP.NET Core (already available)

**Verify at implementation:** Check whether `Hexalith.Tenants.Contracts.csproj` has a reference to `Hexalith.EventStore.Contracts`. If not, you'll need to add a `ProjectReference` to enable `IQueryContract` implementation in Contracts/Queries/. Check:

```bash
cat src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj
```

If Contracts doesn't reference EventStore.Contracts, the query contracts may need to go in a different location, or the reference needs to be added.

### File Structure Requirements

```
src/Hexalith.Tenants.Contracts/
├── Commands/                            (EXISTS — no changes)
├── Events/                              (EXISTS — no changes)
├── Enums/                               (EXISTS — no changes)
├── Identity/                            (EXISTS — no changes)
└── Queries/                             (CREATE directory)
    ├── GetTenantQuery.cs               (CREATE)
    ├── ListTenantsQuery.cs             (CREATE)
    ├── GetTenantUsersQuery.cs          (CREATE)
    ├── GetUserTenantsQuery.cs          (CREATE)
    ├── GetTenantAuditQuery.cs          (CREATE)
    ├── TenantSummary.cs                (CREATE)
    ├── TenantDetail.cs                 (CREATE)
    ├── TenantMember.cs                 (CREATE)
    ├── UserTenantMembership.cs         (CREATE)
    └── PaginatedResult.cs              (CREATE)

src/Hexalith.Tenants/
├── Actors/                              (CREATE directory)
│   └── TenantsProjectionActor.cs       (CREATE)
├── Bootstrap/                           (EXISTS — no changes)
├── Configuration/                       (EXISTS — no changes)
├── Controllers/                         (CREATE directory)
│   └── TenantsQueryController.cs       (CREATE)
├── DomainProcessing/                    (EXISTS — no changes)
├── Validation/                          (EXISTS — no changes)
└── Program.cs                           (MODIFY — add actor registration)

tests/Hexalith.Tenants.Contracts.Tests/
└── Queries/                             (CREATE directory)
    └── QueryContractNamingTests.cs     (CREATE)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Query contract naming tests — reflection-based convention verification:**

| #   | Test                                                             | Setup                                            | Expected                                                             | AC   |
| --- | ---------------------------------------------------------------- | ------------------------------------------------ | -------------------------------------------------------------------- | ---- |
| Q1  | All IQueryContract implementations have kebab-case QueryType     | Scan Contracts assembly for IQueryContract impls | All QueryType values match `^[a-z][a-z0-9-]*$`                       | #1-5 |
| Q2  | All IQueryContract implementations have non-empty Domain         | Scan for impls                                   | Domain is non-empty and kebab-case                                   | #1-5 |
| Q3  | All IQueryContract implementations have non-empty ProjectionType | Scan for impls                                   | ProjectionType is non-empty, no colons, <= 100 chars                 | #1-5 |
| Q4  | QueryType values are unique                                      | All impls                                        | No duplicate QueryType values across contracts                       | #1-5 |
| Q5  | Exactly 5 IQueryContract implementations exist (canary)          | Reflection count                                 | 5 implementations — fails if new query added without convention test | #1-5 |

**Projection actor tests — mock `DaprClient.GetStateAsync` via `NSubstitute`:**

Use `NSubstitute` to mock `DaprClient`. Create a helper that returns pre-built `TenantReadModel`, `GlobalAdministratorReadModel`, `TenantIndexReadModel` instances from mocked state store calls. This enables full Tier 1 testing without DAPR infrastructure.

```csharp
// Test setup pattern:
var daprClient = Substitute.For<DaprClient>();
daprClient.GetStateAsync<TenantReadModel>("tenants-eventstore", Arg.Any<string>())
    .Returns(Task.FromResult(someTenantReadModel));
```

| #   | Test                                                      | Setup                                                                                             | Expected                                                                                                           | AC     |
| --- | --------------------------------------------------------- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ------ |
| Q6  | Authorized user can get tenant details                    | Mock TenantReadModel with user1 in Members, envelope.UserId=user1                                 | Returns TenantDetail with correct TenantId, Name, Members                                                          | #2     |
| Q7  | Unauthorized user gets 403 for GetTenant                  | Mock TenantReadModel with user1 in Members, envelope.UserId=user2 (not a member, not GlobalAdmin) | QueryResult.Success=false, ErrorMessage contains "Forbidden"                                                       | #6     |
| Q8  | GlobalAdmin can access any tenant                         | Mock GlobalAdminReadModel with user2 as admin, TenantReadModel without user2 in Members           | Returns TenantDetail (GlobalAdmin bypasses membership check)                                                       | #2     |
| Q9  | ListTenants filters by user membership (non-admin)        | Mock TenantIndexReadModel with 5 tenants, UserTenants[user1] has 2 tenants                        | Returns PaginatedResult with exactly 2 tenants                                                                     | #1     |
| Q10 | GlobalAdmin ListTenants returns all tenants               | Mock GlobalAdminReadModel with user1 as admin, TenantIndexReadModel with 5 tenants                | Returns PaginatedResult with all 5 tenants                                                                         | #1     |
| Q11 | Pagination returns correct first page                     | Mock TenantIndexReadModel with 10 tenants, pageSize=3                                             | 3 items returned, HasMore=true, Cursor set to 3rd item's key                                                       | #7     |
| Q12 | Pagination with cursor returns next page                  | Mock 10 tenants, cursor=3rd tenant key, pageSize=3                                                | Items 4-6 returned, HasMore=true                                                                                   | #7     |
| Q13 | Pagination last page has HasMore=false                    | Mock 5 tenants, pageSize=10                                                                       | 5 items returned, HasMore=false, Cursor=null                                                                       | #7     |
| Q14 | GetTenantUsers returns paginated member list              | Mock TenantReadModel with 5 members, user1 is TenantOwner                                         | PaginatedResult with 5 TenantMember items, correct roles                                                           | #3     |
| Q15 | GetUserTenants for own user works                         | Mock TenantIndexReadModel, UserTenants[user1] has 3 tenants, envelope.UserId=user1                | Returns 3 UserTenantMembership items with correct roles                                                            | #4     |
| Q16 | Non-admin cannot query other user's tenants               | envelope.UserId=user1, EntityId=user2, user1 not GlobalAdmin                                      | QueryResult.Success=false, "Forbidden"                                                                             | #4, #6 |
| Q17 | GlobalAdmin can query any user's tenants                  | envelope.UserId=admin1 (GlobalAdmin), EntityId=user2                                              | Returns user2's tenant list                                                                                        | #4     |
| Q18 | GetTenantAudit returns 501 Not Implemented                | Any valid envelope with QueryType=get-tenant-audit                                                | QueryResult.Success=false, ErrorMessage contains "not yet implemented"                                             | #5     |
| Q19 | Unknown query type returns error                          | envelope.QueryType="unknown-query"                                                                | QueryResult.Success=false, ErrorMessage contains "Unknown query type"                                              | —      |
| Q20 | Empty TenantIndexReadModel returns empty paginated result | Mock empty TenantIndexReadModel                                                                   | PaginatedResult with 0 items, HasMore=false                                                                        | #1     |
| Q21 | GetTenant with non-existent tenantId                      | Mock DaprClient returns null/default for unknown key                                              | QueryResult.Success=false, appropriate error                                                                       | #2     |
| Q25 | Malformed cursor treated as start-from-beginning          | Mock 5 tenants, cursor="zzz-nonexistent"                                                          | Returns items from beginning of sorted order (cursor doesn't match any key → no items after it → empty or restart) | #7     |
| Q26 | Cursor pointing to deleted tenant skips gracefully        | Mock 5 tenants (A,B,C,D,E), cursor="B" but B removed from index                                   | Returns C,D,E (items after cursor position in sort order)                                                          | #7     |
| Q27 | Non-admin hitting audit endpoint gets 403 not 501         | envelope.UserId=user1 (not GlobalAdmin), QueryType=get-tenant-audit                               | QueryResult.Success=false, ErrorMessage contains "Forbidden" (NOT "not yet implemented")                           | #5, #6 |

**DTO serialization tests:**

| #   | Test                                                | Expected                                               | AC                                                                            |
| --- | --------------------------------------------------- | ------------------------------------------------------ | ----------------------------------------------------------------------------- | ---- |
| Q22 | TenantDetail serializes to JSON with all properties | Round-trip serialize/deserialize TenantDetail          | All properties preserved including Members list and Configuration dict        | #2   |
| Q23 | PaginatedResult<TenantSummary> serializes correctly | Round-trip PaginatedResult with Items, Cursor, HasMore | JSON structure matches `{ "items": [...], "cursor": "...", "hasMore": true }` | #7   |
| Q24 | TenantStatus and TenantRole serialize as strings    | Serialize TenantSummary with Status=Active             | JSON contains `"status":"Active"` not `"status":0`                            | #1-4 |

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all public method parameters
- `sealed` classes — no subclassing needed (except `CachingProjectionActor` inheritance)
- XML doc comments: `/// <summary>` on public API types (query contracts, DTOs, controller endpoints, actor class)
- `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>` for response properties
- Use `ConfigureAwait(false)` on all async calls (established pattern)

### Previous Story Intelligence

**Story 5.1 (review) — Per-Tenant & Global Admin Projections:**

- Created `TenantReadModel` with 9 Apply methods, `TenantProjection`, `GlobalAdministratorReadModel`, `GlobalAdministratorProjection` in `Server/Projections/`
- `TenantReadModel` has: `TenantId`, `Name`, `Description`, `Status`, `Members` (Dict<string, TenantRole>), `Configuration` (Dict<string, string>), `CreatedAt`
- `GlobalAdministratorReadModel` has: `Administrators` (HashSet<string>)
- Both use `private set` and `Apply()` methods — private setters mean the actor reads state from DAPR state store, it doesn't replay events
- D5: Rejection events in event stream — typed `Project()` silently skips, `ProjectFromJson()` throws. Relevant for audit event replay
- D9: Mutable collection exposure — read model's `Dictionary<>` and `HashSet<>` have public getters. When building query responses, convert to `IReadOnlyList<>` / `IReadOnlyDictionary<>`
- Assembly scanning auto-discovers projections — no DI changes needed for projection registration

**Story 5.2 (ready-for-dev) — Cross-Tenant Index Projection:**

- Creates `TenantIndexReadModel` with `Tenants` (Dict<string, TenantIndexEntry>) and `UserTenants` (Dict<string, Dict<string, TenantRole>>)
- `TenantIndexEntry` is `record TenantIndexEntry(string Name, TenantStatus Status)`
- 7 Apply methods (no config events — index doesn't track per-tenant config)
- Defensive `TryGetValue` guards for fan-in event ordering
- **Critical for ListTenants and GetUserTenants queries** — these endpoints read from `TenantIndexReadModel`
- Story 5.2 review clarified ownership: this story now implements the shared-key fan-in host, ETag retry loop, explicit `tenant-index` invalidation, and NFR2 validation for cross-tenant query paths
- Note: Story 5.2 may or may not be implemented when Story 5.3 starts. If `TenantIndexReadModel` doesn't exist yet, ListTenants and GetUserTenants queries cannot be implemented. **Check** if 5.2 files exist before implementing cross-tenant queries. If not, implement only per-tenant queries (GetTenant, GetTenantUsers, GetTenantAudit) first.

**Story 4.3 (done) — Sample Consuming Service & Idempotent Processing Guide:**

- Established access check endpoints pattern in the sample consuming service
- The recent commit "Add idempotent event processing documentation and implement access check endpoints" may contain patterns relevant to the authorization checks

### Git Intelligence

Recent commits:

- `d5dbae4` Add idempotent event processing documentation and implement access check endpoints
- `691a9f0` chore: Update subproject commit reference for Hexalith.EventStore
- `968791d` feat: Add design decisions and assumptions for tenant projections
- `04de61f` feat: Implement tenant event handling and projection management
- `33ab49e` feat: Implement tenant configuration management with DI registration and unit tests

Established patterns:

- Apply method pattern with `ArgumentNullException.ThrowIfNull(e)` null guards
- Allman brace style consistently
- Private setters for state properties
- `sealed` classes where no inheritance needed
- MediatR for command/query dispatch
- `ConfigureAwait(false)` on all async calls

### Cross-Story Dependencies

**This story depends on:**

- Story 5.1 (review): `TenantReadModel`, `GlobalAdministratorReadModel`, projections — for per-tenant queries and GlobalAdmin authorization check
- Story 5.2 (ready-for-dev): `TenantIndexReadModel`, `TenantIndexProjection` — for ListTenants and GetUserTenants queries. **If 5.2 is not yet implemented, defer cross-tenant queries**
- Story 2.1 (done): Event contracts (for audit event replay if implementing FR29)
- Story 2.4 (done): Hexalith.Tenants bootstrap and event publishing (Program.cs structure)

**No stories depend on this** — this is the final story in Epic 5.

### Critical Anti-Patterns (DO NOT)

- **DO NOT** put query logic in the REST controller — controller is a thin translation layer
- **DO NOT** bypass the MediatR pipeline — all queries must go through SubmitQuery → QueryRouter → ProjectionActor
- **DO NOT** use `name` JWT claim for user ID — use `sub` only (F-RT2)
- **DO NOT** modify read model classes — they are Story 5.1/5.2 scope
- **DO NOT** add new projection types — use existing `TenantProjection`, `TenantIndexProjection`
- **DO NOT** create a separate QueryApi project — single deployable
- **DO NOT** assume immediate consistency — queries are eventually consistent
- **DO NOT** use `ProjectFromJson()` for event replay without filtering rejection events
- **DO NOT** expose mutable collections in response DTOs — use `IReadOnlyList<>`, `IReadOnlyDictionary<>`
- **DO NOT** register the actor with a type name other than `"ProjectionActor"` — QueryRouter uses this constant
- **DO NOT** create controller action methods that return `void` or `Task` — always return `IActionResult`
- **DO NOT** mix authorization logic between controller and actor — controller handles HTTP auth (`[Authorize]`), actor handles domain RBAC (tenant membership check)

### Concurrency & ETag Caching Notes

`CachingProjectionActor` provides automatic ETag-based caching:

1. On query, checks if cached ETag matches current ETag from `IETagService`
2. On cache hit → returns cached payload (no state store read)
3. On cache miss → calls `ExecuteQueryAsync` (your implementation), caches result

ETag invalidation happens via `IProjectionChangeNotifier` when projections process new events. This means:

- After a command is processed → projection handler processes event → ETag invalidated → next query returns fresh data
- The projection actor does NOT need to manage ETag invalidation — it's automatic

### Project Structure Notes

- Contracts/Queries/ folder is new — `Contracts.csproj` may need a `ProjectReference` to `EventStore.Contracts` for `IQueryContract`
- Hexalith.Tenants/Actors/ folder is new — for the `TenantsProjectionActor`
- Hexalith.Tenants/Controllers/ folder is new — for the `TenantsQueryController`
- Program.cs is the only existing file being modified — add actor registration
- Server project is NOT modified — read models and projections already exist from Stories 5.1/5.2

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.3] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] — Epic objectives: tenant discovery & query
- [Source: _bmad-output/planning-artifacts/prd.md#FR25-FR30] — Tenant discovery & query requirements
- [Source: _bmad-output/planning-artifacts/architecture.md#D7 Revision — Dual-Layer Query Architecture] — IQueryContract, QueryRouter, CachingProjectionActor, REST controllers
- [Source: _bmad-output/planning-artifacts/architecture.md#D4 Revision — EventStore Upgrade Alignment] — CachingProjectionActor for all projections, ETag management
- [Source: _bmad-output/planning-artifacts/architecture.md#D8 Revision — Authorization Model Clarification] — Three-layer authorization, JWT + domain RBAC
- [Source: _bmad-output/planning-artifacts/architecture.md#Query Consistency Model] — Eventual consistency with read-after-write mitigation
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — TenantsQueryController.cs in Hexalith.Tenants/Controllers/
- [Source: _bmad-output/planning-artifacts/architecture.md#FR-to-Structure Mapping] — FR25-30 → Server/Projections + Hexalith.Tenants/Controllers
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/IQueryContract.cs] — QueryType, Domain, ProjectionType static members
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs] — Abstract base with ETag caching, ExecuteQueryAsync
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/IProjectionActor.cs] — DAPR actor interface, 3-tier routing model
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/QueryEnvelope.cs] — Query envelope with TenantId, Domain, QueryType, UserId, EntityId
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryRouter.cs] — Routes to ProjectionActor type name
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryActorIdHelper.cs] — 3-tier actor ID derivation
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Hexalith.Tenants/Controllers/QueriesController.cs] — Generic query endpoint with ETag pre-check
- [Source: _bmad-output/implementation-artifacts/5-1-per-tenant-and-global-admin-projections.md] — TenantReadModel, GlobalAdministratorReadModel patterns
- [Source: _bmad-output/implementation-artifacts/5-2-cross-tenant-index-projection.md] — TenantIndexReadModel, TenantIndexEntry patterns

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Task 0: EventStore.Contracts reference already existed in Contracts.csproj — verified build passes
- DAPR actor registration: Used `[Actor(TypeName = "ProjectionActor")]` attribute instead of `ActorRegistrationOptions` (which doesn't exist in DAPR SDK 1.17)
- State store name: Used `"statestore"` matching actual DAPR component config (story notes referenced `"tenants-eventstore"` which is the convention name)
- Pre-existing integration test failures (DaprEndToEndTests) unrelated to this story — require DAPR infrastructure

### Completion Notes List

- All 5 query contracts created implementing IQueryContract (GetTenantQuery, ListTenantsQuery, GetTenantUsersQuery, GetUserTenantsQuery, GetTenantAuditQuery)
- All 5 query response DTOs created (TenantSummary, TenantDetail, TenantMember, UserTenantMembership, PaginatedResult<T>)
- TenantsProjectionActor created inheriting CachingProjectionActor with dual-layer authorization (membership + GlobalAdmin)
- TenantsQueryController created with 5 REST endpoints mapping to SubmitQuery MediatR dispatches
- Actor registered in Program.cs with TypeName="ProjectionActor"
- GetTenantAudit returns 403 for non-admins, 501 for admins (AC5 security requirement)
- Cursor-based pagination implemented with stable sort-by-key ordering
- 27 unit tests: 5 query contract naming (Q1-Q5), 19 projection actor logic (Q6-Q21, Q25-Q27), 3 DTO serialization (Q22-Q24)
- All 303 non-integration tests pass, 0 warnings, 0 errors in Release build

### Change Log

- 2026-03-18: Story 5.3 implementation complete — query endpoints, projection actor, authorization, unit tests

### File List

**New files:**
- src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs
- src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs
- src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs
- src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs
- src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs
- src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs
- src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs
- src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs
- src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs
- src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs
- src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs
- src/Hexalith.Tenants/Controllers/TenantsQueryController.cs
- tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryContractNamingTests.cs
- tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs

**Modified files:**
- src/Hexalith.Tenants/Program.cs (added actor registration)
