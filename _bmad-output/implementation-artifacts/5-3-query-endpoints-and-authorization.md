# Story 5.3: Query Endpoints & Authorization

Status: ready-for-dev

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
   **Then** tenant access change events are returned with pagination support (default 100, max 1,000 results per page)

6. **Given** an authenticated user without a role in the target tenant and not a GlobalAdmin
   **When** a GET request is sent to `/api/tenants/{tenantId}` or `/api/tenants/{tenantId}/users`
   **Then** the request is rejected with 403 Forbidden

7. **Given** all query endpoints
   **When** cursor-based pagination parameters are provided
   **Then** results are returned with consistent ordering and valid cursor tokens for next-page navigation

8. **Given** a command has just been processed (e.g., CreateTenant)
   **When** the command response is returned
   **Then** the response includes the aggregate ID so the client can navigate directly to `GET /api/tenants/{id}` for read-after-write confirmation

## Tasks / Subtasks

- [ ] Task 1: Create query contracts in Contracts project (AC: #1-5, #7)
  - [ ] 1.1: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs` implementing `IQueryContract`
  - [ ] 1.2: Create `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs` implementing `IQueryContract`
  - [ ] 1.3: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs` implementing `IQueryContract`
  - [ ] 1.4: Create `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs` implementing `IQueryContract`
  - [ ] 1.5: Create `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs` implementing `IQueryContract`
  - [ ] 1.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Create query response types in Contracts project (AC: #1-5)
  - [ ] 2.1: Create `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs` — lightweight DTO for list endpoints
  - [ ] 2.2: Create `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs` — full tenant details DTO
  - [ ] 2.3: Create `src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs` — user+role DTO
  - [ ] 2.4: Create `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs` — tenant+role DTO for user lookups
  - [ ] 2.5: Create `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs` — generic paginated response wrapper
  - [ ] 2.6: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 3: Create TenantsProjectionActor in CommandApi (AC: #1-6)
  - [ ] 3.1: Create `src/Hexalith.Tenants.CommandApi/Actors/TenantsProjectionActor.cs` inheriting `CachingProjectionActor`
  - [ ] 3.2: Implement `ExecuteQueryAsync` — dispatch to per-query-type handler methods
  - [ ] 3.3: Implement GetTenant handler — load TenantReadModel from projection, authorize, return TenantDetail
  - [ ] 3.4: Implement ListTenants handler — load TenantIndexReadModel, authorize (filter by user membership or GlobalAdmin sees all), paginate, return PaginatedResult<TenantSummary>
  - [ ] 3.5: Implement GetTenantUsers handler — load TenantReadModel, authorize, paginate members, return PaginatedResult<TenantMember>
  - [ ] 3.6: Implement GetUserTenants handler — load TenantIndexReadModel, extract user's tenants, paginate, return PaginatedResult<UserTenantMembership>
  - [ ] 3.7: Implement GetTenantAudit handler — GlobalAdmin-only authorization, replay events from state store, return PaginatedResult<AuditEntry> (simplified MVP implementation)
  - [ ] 3.8: Register actor in Program.cs via `MapActorsHandlers` (verify DAPR actor registration uses `ProjectionActorTypeName`)
  - [ ] 3.9: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 4: Create TenantsQueryController in CommandApi (AC: #1-5, #7)
  - [ ] 4.1: Create `src/Hexalith.Tenants.CommandApi/Controllers/TenantsQueryController.cs`
  - [ ] 4.2: Implement `GET /api/tenants` — translate to ListTenantsQuery via SubmitQueryRequest → MediatR
  - [ ] 4.3: Implement `GET /api/tenants/{tenantId}` — translate to GetTenantQuery
  - [ ] 4.4: Implement `GET /api/tenants/{tenantId}/users` — translate to GetTenantUsersQuery
  - [ ] 4.5: Implement `GET /api/users/{userId}/tenants` — translate to GetUserTenantsQuery
  - [ ] 4.6: Implement `GET /api/tenants/{tenantId}/audit` — translate to GetTenantAuditQuery
  - [ ] 4.7: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 5: Register query infrastructure in Program.cs (AC: #1-8)
  - [ ] 5.1: Ensure `MapControllers()` picks up new TenantsQueryController
  - [ ] 5.2: Register TenantsProjectionActor with DAPR actor runtime (type name = `"ProjectionActor"`)
  - [ ] 5.3: Verify no duplicate actor type registrations
  - [ ] 5.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 6: Create unit tests (AC: #1-7)
  - [ ] 6.1: Create `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryContractNamingTests.cs` — reflection-based naming convention test
  - [ ] 6.2: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` — actor logic tests
  - [ ] 6.3: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [ ] Task 7: Build verification (all ACs)
  - [ ] 7.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 7.2: `dotnet test Hexalith.Tenants.slnx` — all tests pass, no regressions

## Dev Notes

### TL;DR

Build the query layer for the Tenants service. 5 query contracts (`IQueryContract` implementations in Contracts/Queries/), a `TenantsProjectionActor` (inherits `CachingProjectionActor`) that loads projection state and serves queries with authorization checks, and a thin REST controller (`TenantsQueryController`) that translates `GET /api/tenants/*` routes into `SubmitQueryRequest` MediatR dispatches. The query pipeline is: REST → MediatR → QueryRouter → CachingProjectionActor → ReadModel. Authorization is dual-layer: JWT at API boundary (existing `AuthorizationBehavior`), domain RBAC (tenant membership / GlobalAdmin check) in the projection actor's `ExecuteQueryAsync`.

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
```
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

| Query Contract | QueryType | Domain | ProjectionType | Tier | EntityId | FR |
|---------------|-----------|--------|----------------|------|----------|-----|
| `GetTenantQuery` | `get-tenant` | `tenants` | `tenants` | 1 | tenantId | FR26 |
| `ListTenantsQuery` | `list-tenants` | `tenants` | `tenant-index` | 3 (or 2 with pagination payload) | null | FR25 |
| `GetTenantUsersQuery` | `get-tenant-users` | `tenants` | `tenants` | 1 | tenantId | FR27 |
| `GetUserTenantsQuery` | `get-user-tenants` | `tenants` | `tenant-index` | 1 | userId | FR28 |
| `GetTenantAuditQuery` | `get-tenant-audit` | `tenants` | `tenants` | 1 | tenantId | FR29 |

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

**Location:** `src/Hexalith.Tenants.CommandApi/Actors/TenantsProjectionActor.cs`

**Why in CommandApi, not Server?** The projection actor depends on DAPR actor runtime (`ActorHost`, DAPR state store client) and is specific to the deployable host. Server contains domain-agnostic types (read models, projections). The actor is an infrastructure concern that wires projections to the query pipeline.

```csharp
// src/Hexalith.Tenants.CommandApi/Actors/TenantsProjectionActor.cs
using System.Text.Json;

using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.CommandApi.Actors;

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

**Critical: Actor registration.** The DAPR actor type name MUST be `"ProjectionActor"` (from `QueryRouter.ProjectionActorTypeName`). Registration in `Program.cs`:

```csharp
// In Program.cs, configure actor runtime:
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<TenantsProjectionActor>(
        typeOptions: new ActorRuntimeOptions { ActorTypeName = "ProjectionActor" });
});
```

**Wait — check if `MapActorsHandlers()` already handles this.** The existing `app.MapActorsHandlers()` call in Program.cs maps DAPR actor endpoints. Actor registration needs to happen via `AddActors()` in DI configuration. Check whether `AddEventStore()` or `AddEventStoreServer()` already registers a default `ProjectionActor`. If they do, the Tenants service needs to provide its own implementation under the same type name. If they don't, the registration above is needed.

**CRITICAL INVESTIGATION:** Before implementing, check what `AddEventStoreServer(builder.Configuration)` and `AddEventStore(typeof(TenantAggregate).Assembly)` do for actor registration. Search for `RegisterActor` and `ProjectionActor` in EventStore.Server and EventStore.CommandApi:

```bash
grep -rn "RegisterActor\|ProjectionActor\|AddActors" Hexalith.EventStore/src/ --include="*.cs"
```

If EventStore already registers a generic ProjectionActor, you may need to replace or extend it rather than adding a parallel registration.

### TenantsQueryController — Thin REST Controller

**Location:** `src/Hexalith.Tenants.CommandApi/Controllers/TenantsQueryController.cs`

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

| REST Endpoint | Query Contract | AggregateId | EntityId | Payload |
|--------------|---------------|-------------|----------|---------|
| `GET /api/tenants` | ListTenantsQuery | `"index"` | null | `{cursor, pageSize}` |
| `GET /api/tenants/{tenantId}` | GetTenantQuery | tenantId | tenantId | empty |
| `GET /api/tenants/{tenantId}/users` | GetTenantUsersQuery | tenantId | tenantId | `{cursor, pageSize}` |
| `GET /api/users/{userId}/tenants` | GetUserTenantsQuery | `"index"` | userId | `{cursor, pageSize}` |
| `GET /api/tenants/{tenantId}/audit` | GetTenantAuditQuery | tenantId | tenantId | `{from, to, cursor, pageSize}` |

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

**How to load projection state:** The projection actor uses `_daprClient.GetStateAsync<T>()` to read projection state from the DAPR state store. The state store name is `"tenants-eventstore"` (from `dapr/components/statestore.yaml`). The state key for per-tenant read models follows the pattern established by `EventStoreProjection<T>` — investigate the key naming convention by checking `EventStoreProjection<T>.Project()` or `CachingProjectionActor` for how state is stored.

**CRITICAL INVESTIGATION:** How does EventStore store projection state? Check:
- `EventStoreProjection<T>` — does it persist to DAPR state store automatically?
- What state key format is used?
- Does `CachingProjectionActor` read projection state, or does it delegate to the implementer?

Looking at `CachingProjectionActor.ExecuteQueryAsync` — this is abstract and the implementer must provide the actual query logic including state loading. The implementer is responsible for reading projection state from wherever it's stored.

**State key conventions — investigate at implementation time:**
```bash
grep -rn "StateStoreName\|GetStateAsync\|SaveStateAsync\|stateStore" Hexalith.EventStore/src/ --include="*.cs" | head -30
```

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

### FR29 Audit Query — MVP Simplification

FR29 requires "query tenant access changes by tenant ID and date range." This requires access to the historical event stream, not just the current projection state. **MVP approach:**

**Option A (Recommended):** Replay events from the tenant's event stream via `EventStoreProjection<T>.Project()` or by loading the aggregate's event history from DAPR state store. Filter events that are "access change" events (`UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantDisabled`, `TenantEnabled`) by date range. This is a read-only operation.

**Option B (Simpler fallback):** Add an `AuditLog` property to `TenantReadModel` (list of audit entries with timestamps). This grows unbounded — not ideal but workable at MVP scale. **Do NOT choose this option** — it pollutes the read model with historical data.

**Option C (Most practical MVP):** Return a "not yet implemented" response with 501 Not Implemented for the audit endpoint. Document that full audit requires event stream query capability. This is acceptable if the other 4 query endpoints are complete.

**Decision for dev agent:** Implement Option A if the event stream is accessible. If loading historical events proves too complex for the projection actor, fall back to Option C and document the limitation.

### Design Decisions & Assumptions

**D1: Query contracts are `sealed class` (not `record`).**
`IQueryContract` uses static abstract members (`QueryType`, `Domain`, `ProjectionType`). Records could work, but query contracts are metadata-only types with no instance state — `sealed class` is more explicit. No constructor, no properties.

**D2: DTOs go in Contracts/Queries/ folder.**
Query response types (`TenantSummary`, `TenantDetail`, etc.) are part of the public contract — consuming services need them to deserialize query responses. Keeping them in Contracts alongside query contracts is the right location per architecture's component boundaries (Contracts → referenced by all projects).

**D3: TenantsProjectionActor in CommandApi, not Server.**
The projection actor depends on DAPR actor runtime infrastructure and is specific to the deployable host. Read models and projections (domain types) live in Server. The actor (infrastructure wiring) lives in CommandApi. This matches the architecture's component boundary: CommandApi references Server + Contracts + ServiceDefaults.

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
Ensure JSON serialization uses `JsonStringEnumConverter` or that `System.Text.Json` serializer options include string enum conversion. Check if the existing serialization pipeline handles this.

**D10 (CRITICAL): Rejection events in the event stream.**
Story 5.1 D5 documented that `EventPersister` stores ALL events including rejection events. The typed `EventStoreProjection<T>.Project()` silently skips unknown event types (no Apply method → continue). However, `ProjectFromJson(JsonElement)` throws on unknown types. If the projection actor ever uses `ProjectFromJson()` (e.g., for audit event replay), it MUST filter rejection events first. The projection definitions themselves are safe.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder | File |
|------|---------|--------|------|
| GetTenantQuery | Contracts | Queries/ | GetTenantQuery.cs (CREATE) |
| ListTenantsQuery | Contracts | Queries/ | ListTenantsQuery.cs (CREATE) |
| GetTenantUsersQuery | Contracts | Queries/ | GetTenantUsersQuery.cs (CREATE) |
| GetUserTenantsQuery | Contracts | Queries/ | GetUserTenantsQuery.cs (CREATE) |
| GetTenantAuditQuery | Contracts | Queries/ | GetTenantAuditQuery.cs (CREATE) |
| TenantSummary | Contracts | Queries/ | TenantSummary.cs (CREATE) |
| TenantDetail | Contracts | Queries/ | TenantDetail.cs (CREATE) |
| TenantMember | Contracts | Queries/ | TenantMember.cs (CREATE) |
| UserTenantMembership | Contracts | Queries/ | UserTenantMembership.cs (CREATE) |
| PaginatedResult<T> | Contracts | Queries/ | PaginatedResult.cs (CREATE) |
| TenantsProjectionActor | CommandApi | Actors/ | TenantsProjectionActor.cs (CREATE) |
| TenantsQueryController | CommandApi | Controllers/ | TenantsQueryController.cs (CREATE) |
| Query contract naming tests | Contracts.Tests | Queries/ | QueryContractNamingTests.cs (CREATE) |

**DO NOT:**

- Create types outside the designated projects — query contracts in Contracts, actor in CommandApi
- Modify existing read models (`TenantReadModel`, `GlobalAdministratorReadModel`, `TenantIndexReadModel`) — those are Story 5.1/5.2 scope and already complete/in-progress
- Modify existing projection classes (`TenantProjection`, `GlobalAdministratorProjection`) — those are Story 5.1 scope
- Add new NuGet packages unless absolutely required — existing dependencies should suffice
- Create a separate QueryApi project — architecture decision is single deployable (CommandApi serves both commands and queries)
- Add shared base classes between query contracts — each is an independent type
- Put query logic in the REST controller — controller is a thin translation layer only
- Create command-side authorization middleware for queries — query authorization is in the projection actor
- Add `HasMembershipHistory`, `Bootstrapped`, or other aggregate-only properties to query response types
- Use mutable collections in response DTOs — use `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>`
- Use `name` JWT claim for user identification — MUST use `sub` only (F-RT2 security)

### Library & Framework Requirements

**No new NuGet packages expected.** All dependencies available:

- `IQueryContract`, `IQueryResponse<T>`, `SubmitQueryRequest/Response` → `Hexalith.EventStore.Contracts` (referenced by Contracts)
- `CachingProjectionActor`, `IProjectionActor`, `QueryEnvelope`, `QueryResult`, `IETagService` → `Hexalith.EventStore.Server` (referenced by CommandApi via EventStore.CommandApi)
- `SubmitQuery`, `SubmitQueryResult` → `Hexalith.EventStore.Server.Pipeline.Queries` (referenced by CommandApi)
- `DaprClient` → `Dapr.AspNetCore` (already in CommandApi.csproj)
- `IMediator` → `MediatR` (already in CommandApi.csproj)
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

src/Hexalith.Tenants.CommandApi/
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

| # | Test | Setup | Expected | AC |
|---|------|-------|----------|-----|
| Q1 | All IQueryContract implementations have kebab-case QueryType | Scan Contracts assembly for IQueryContract impls | All QueryType values match `^[a-z][a-z0-9-]*$` | #1-5 |
| Q2 | All IQueryContract implementations have non-empty Domain | Scan for impls | Domain is non-empty and kebab-case | #1-5 |
| Q3 | All IQueryContract implementations have non-empty ProjectionType | Scan for impls | ProjectionType is non-empty, no colons, <= 100 chars | #1-5 |
| Q4 | QueryType values are unique | All impls | No duplicate QueryType values across contracts | #1-5 |
| Q5 | Exactly 5 IQueryContract implementations exist (canary) | Reflection count | 5 implementations — fails if new query added without convention test | #1-5 |

**Projection actor tests (if testable without DAPR):**

| # | Test | Expected | AC |
|---|------|----------|-----|
| Q6 | Authorized user can get tenant details | Returns TenantDetail with correct data | #2 |
| Q7 | Unauthorized user gets 403 for GetTenant | QueryResult.Success = false | #6 |
| Q8 | GlobalAdmin can access any tenant | Returns TenantDetail | #2 |
| Q9 | ListTenants filters by user membership | Only user's tenants returned | #1 |
| Q10 | GlobalAdmin ListTenants returns all | All tenants returned | #1 |
| Q11 | Pagination returns correct page with cursor | Page size respected, cursor valid | #7 |
| Q12 | GetUserTenants for own user works | Returns user's tenant list | #4 |
| Q13 | Non-admin cannot query other user's tenants | 403 returned | #4, #6 |

**Note:** Projection actor tests may require mocking DAPR state store calls. Use `NSubstitute` to mock `DaprClient.GetStateAsync`. If the DAPR dependency makes unit testing impractical, document as a testing limitation and defer to Tier 2/3 integration tests.

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
- Story 2.4 (done): CommandApi bootstrap and event publishing (Program.cs structure)

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
- CommandApi/Actors/ folder is new — for the `TenantsProjectionActor`
- CommandApi/Controllers/ folder is new — for the `TenantsQueryController`
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
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — TenantsQueryController.cs in CommandApi/Controllers/
- [Source: _bmad-output/planning-artifacts/architecture.md#FR-to-Structure Mapping] — FR25-30 → Server/Projections + CommandApi/Controllers
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/IQueryContract.cs] — QueryType, Domain, ProjectionType static members
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs] — Abstract base with ETag caching, ExecuteQueryAsync
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/IProjectionActor.cs] — DAPR actor interface, 3-tier routing model
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/QueryEnvelope.cs] — Query envelope with TenantId, Domain, QueryType, UserId, EntityId
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryRouter.cs] — Routes to ProjectionActor type name
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryActorIdHelper.cs] — 3-tier actor ID derivation
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.CommandApi/Controllers/QueriesController.cs] — Generic query endpoint with ETag pre-check
- [Source: _bmad-output/implementation-artifacts/5-1-per-tenant-and-global-admin-projections.md] — TenantReadModel, GlobalAdministratorReadModel patterns
- [Source: _bmad-output/implementation-artifacts/5-2-cross-tenant-index-projection.md] — TenantIndexReadModel, TenantIndexEntry patterns

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
