# Story 5.2: Cross-Tenant Index Projection

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want a cross-tenant index projection that aggregates data across all tenants,
So that ListTenants and GetUserTenants queries can be served efficiently at scale.

## Acceptance Criteria

1. **Given** a TenantCreated event is published
   **When** the TenantIndexProjection processes the event
   **Then** the tenant is added to the cross-tenant index (TenantId, Name, Status=Active)

2. **Given** a TenantDisabled or TenantEnabled event is published
   **When** the TenantIndexProjection processes the event
   **Then** the tenant's status is updated in the cross-tenant index

3. **Given** UserAddedToTenant or UserRemovedFromTenant events are published
   **When** the TenantIndexProjection processes these events
   **Then** the user-to-tenant mapping index is updated (UserAddedToTenant adds {TenantId, Role} entry; UserRemovedFromTenant removes it)

4. **Given** two concurrent events trigger simultaneous updates to the cross-tenant index key
   **When** the projection performs a read-modify-write on the shared state key
   **Then** ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`) detects the conflict and retries (max 3 attempts)

5. **Given** the cross-tenant index is populated with 1,000 tenants
   **When** the index is queried
   **Then** it returns results within NFR2 latency targets (50ms p95 per page)

## Tasks / Subtasks

- [ ] Task 1: Create `TenantIndexReadModel.cs` (AC: #1, #2, #3)
  - [ ] 1.1: Create `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
  - [ ] 1.2: Properties: `Tenants` (Dictionary<string, TenantIndexEntry>), `UserTenants` (Dictionary<string, Dictionary<string, TenantRole>>)
  - [ ] 1.3: Create `src/Hexalith.Tenants.Server/Projections/TenantIndexEntry.cs` — record with `Name` (string), `Status` (TenantStatus)
  - [ ] 1.4: Apply methods for: `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`
  - [ ] 1.5: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Create `TenantIndexProjection.cs` (AC: #1, #2, #3)
  - [ ] 2.1: Create `src/Hexalith.Tenants.Server/Projections/TenantIndexProjection.cs` inheriting `EventStoreProjection<TenantIndexReadModel>`
  - [ ] 2.2: Override `OnConfiguring` to set domain name to `"tenants"` (verify via `NamingConventionEngine.GetDomainName(typeof(TenantIndexProjection))` first — override only if convention returns wrong name)
  - [ ] 2.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 3: Verify CachingProjectionActor fan-in support (AC: #4)
  - [ ] 3.1: Investigate whether `CachingProjectionActor` supports fan-in (events from ALL tenant aggregates → one projection actor). Focus on the **event routing and subscription model**, not the caching layer — caching is a query-time concern (Story 5.3)
  - [ ] 3.2: Investigate assembly scanning behavior — will EventStore infrastructure route events to BOTH `TenantProjection` and `TenantIndexProjection` since both are `EventStoreProjection<T>` subclasses in the same assembly? Or does the projection type determine routing?
  - [ ] 3.3: If fan-in supported → document in completion notes, Story 5.3 builds the actor using `CachingProjectionActor`
  - [ ] 3.4: If NOT supported → document fallback decision: manual DAPR state store with ETag retry pattern (implementation deferred to Story 5.3 query hosting)
  - [ ] 3.5: **Deliverable:** Add a `### CachingProjectionActor Fan-In Findings` section to this story's Dev Agent Record with specific findings from 3.1-3.4 — this is a required handoff artifact for Story 5.3

- [ ] Task 4: Create unit tests (AC: #1, #2, #3, #5)
  - [ ] 4.1: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexReadModelTests.cs`
  - [ ] 4.2: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexProjectionTests.cs`
  - [ ] 4.3: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [ ] Task 5: Build verification (all ACs)
  - [ ] 5.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 5.2: `dotnet test Hexalith.Tenants.slnx` — all tests pass, no regressions

## Dev Notes

### TL;DR

Build a "phone book" for all tenants. `TenantIndexReadModel` has two dictionaries: `Tenants` (who exists, active or disabled?) and `UserTenants` (which tenants is each user in, with what role?). `TenantIndexProjection` is an empty shell inheriting `EventStoreProjection<T>`. Create `TenantIndexEntry.cs` as a separate record file. 7 Apply methods, ~20 tests, one investigation task (Task 3). Do NOT build query endpoints or hosting — that's Story 5.3.

### Scope: Cross-Tenant Index Read Model + Projection Shell Only

This story creates the **TenantIndexReadModel** (fan-in data structure aggregating data across ALL tenants) and **TenantIndexProjection** (the `EventStoreProjection<T>` subclass). Query endpoints (`GET /api/tenants`, `GET /api/users/{userId}/tenants`) are Story 5.3. The concurrency hosting (subscription endpoint, state management, ETag retry loop) is Story 5.3 scope — this story defines the data structure and Apply mechanics only.

**Relationship to Story 5.1:** Story 5.1 creates per-tenant projections (`TenantProjection`, `GlobalAdministratorProjection`) — one read model per aggregate instance. This story creates a cross-tenant projection — one read model aggregating data from ALL aggregate instances. Same `EventStoreProjection<T>` pattern, fundamentally different hosting concern.

### Architecture: Fan-In Projection Pattern

Unlike per-aggregate projections (Story 5.1), the cross-tenant index receives events from ALL tenant aggregates and funnels them into a single read model. The `EventStoreProjection<T>` base class handles Apply method discovery identically — the difference is in how events are fed to the projection:

- **Per-tenant** (Story 5.1): Events replayed from one aggregate's event stream → one `TenantReadModel` per tenant
- **Cross-tenant** (this story): Events from ALL tenant aggregates → one `TenantIndexReadModel` for the entire system

**Critical: `Project()` is NOT the expected fan-in entry point.** `EventStoreProjection<T>.Project()` starts from `new TReadModel()` and replays a complete event sequence — suitable for per-aggregate replay but impractical for fan-in (would require collecting ALL events from ALL aggregates). Instead, the hosting layer (Story 5.3) will call Apply methods **incrementally** as events arrive from the pub/sub subscription. `Project()` is only useful for full replay scenarios (e.g., rebuilding the index from scratch).

**Consequence for ETag invalidation:** `FireProjectionChangeNotification()` is called at the end of `Project()`, NOT after individual Apply calls. For incremental fan-in hosting, Story 5.3 must call `Notifier.NotifyProjectionChangedAsync()` directly after each Apply — the projection's built-in notification won't fire.

### Read Model Design

**TenantIndexReadModel** — two indexes for serving FR25 (ListTenants) and FR28 (GetUserTenants):

```csharp
// src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class TenantIndexReadModel
{
    public Dictionary<string, TenantIndexEntry> Tenants { get; private set; } = new();
    public Dictionary<string, Dictionary<string, TenantRole>> UserTenants { get; private set; } = new();

    public void Apply(TenantCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Tenants[e.TenantId] = new TenantIndexEntry(e.Name, TenantStatus.Active);
    }

    public void Apply(TenantUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Name = e.Name };
        }
    }

    public void Apply(TenantDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Disabled };
        }
    }

    public void Apply(TenantEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Active };
        }
    }

    public void Apply(UserAddedToTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants = new Dictionary<string, TenantRole>();
            UserTenants[e.UserId] = tenants;
        }

        tenants[e.TenantId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants.Remove(e.TenantId);
            if (tenants.Count == 0)
            {
                UserTenants.Remove(e.UserId);
            }
        }
    }

    public void Apply(UserRoleChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants[e.TenantId] = e.NewRole;
        }
    }
}
```

```csharp
// src/Hexalith.Tenants.Server/Projections/TenantIndexEntry.cs (SEPARATE FILE)
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Server.Projections;

public sealed record TenantIndexEntry(string Name, TenantStatus Status);
```

### Projection Class — Shell with Domain Name Verification

```csharp
// src/Hexalith.Tenants.Server/Projections/TenantIndexProjection.cs
using Hexalith.EventStore.Client.Aggregates;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Cross-tenant index projection. Aggregates data from all tenant aggregates.
/// Auto-discovered by EventStore's assembly scanning.
/// </summary>
public sealed class TenantIndexProjection : EventStoreProjection<TenantIndexReadModel>
{
}
```

**Domain Name:** `NamingConventionEngine.GetDomainName(typeof(TenantIndexProjection))` should derive `"tenants"` (same as `TenantProjection`). Verify at test time. If the convention returns a different name (e.g., `"tenant-index"`), override `OnConfiguring`:

```csharp
protected override void OnConfiguring(EventStoreDomainOptions options)
{
    ArgumentNullException.ThrowIfNull(options);
    options.DomainName = "tenants";
}
```

### Design Decisions & Assumptions

**D1: TenantIndexEntry is a record in its own file.**
`TenantIndexEntry` is a lightweight immutable data carrier (Name + Status). Using a `record` enables the `with` expression pattern for partial updates (e.g., `existing with { Status = Disabled }`), which is cleaner than mutable setters. Per `.editorconfig` "one type per file" rule, it goes in a separate `TenantIndexEntry.cs` file in the same `Projections/` folder and namespace.

**D2: TenantUpdated IS handled even though not in ACs.**
The epics ACs only explicitly mention TenantCreated, TenantDisabled/Enabled, and UserAdded/Removed. However, FR25 requires "paginated list of all tenants with their IDs, names, and statuses." If `TenantUpdated` (which changes the name) is not handled, the index will show stale tenant names. Adding the Apply method is trivial and prevents a certain bug in Story 5.3 queries. This is a correctness decision, not scope creep.

**D3: Defensive Apply methods for Update/Disable/Enable (TryGetValue guard).**
Unlike `TenantReadModel` (Story 5.1) which trusts the event stream unconditionally (D8), `TenantIndexReadModel` uses `TryGetValue` guards before modifying existing entries. Reason: in a fan-in projection, event ordering across different aggregates is not guaranteed. A `TenantDisabled` event could arrive before `TenantCreated` if event delivery is reordered. The guard prevents `KeyNotFoundException` without masking data issues — a monitoring concern, not a defensive-code concern. The per-tenant `TenantReadModel` doesn't need this because its events come from a single aggregate with guaranteed ordering.

**D4: UserTenants cleanup on empty dictionary.**
When `UserRemovedFromTenant` leaves a user with zero tenant memberships, the user entry is removed from `UserTenants` entirely. This prevents unbounded growth of empty entries and keeps the index lean. At query time (Story 5.3), a missing user key means "user has no tenants" — semantically correct.

**D5: UserRoleChanged Apply method included (AC3 gap correction).**
AC3 only mentions `UserAddedToTenant` and `UserRemovedFromTenant`, but FR28 requires "list of tenants a specific user belongs to, **with their role in each tenant.**" Without handling `UserRoleChanged`, the `UserTenants` index would show stale roles (the original role from `UserAddedToTenant`). This is a correctness decision — AC3 is incomplete relative to FR28. The Apply method is included to prevent a guaranteed P1 bug in Story 5.3 queries.

**D6: CachingProjectionActor fan-in verification is a Task 3 investigation.**
Per architecture D4 Revision caveat: "Cross-tenant index CachingProjectionActor adoption is conditional on verifying the actor supports fan-in event processing." This story verifies and documents the finding. The actual hosting implementation (subscription handler, state management) is Story 5.3 scope. This story is the read model + projection definition only.

**D7: Mutable dictionary exposure is acceptable at MVP (same rationale as Story 5.1 D9).**
`Dictionary<string, TenantIndexEntry> Tenants` and `Dictionary<string, Dictionary<string, TenantRole>> UserTenants` expose mutable references. For projection definitions, this is acceptable — the hosting layer (Story 5.3) will serialize to JSON for state store persistence.

**D8: TenantIndexEntry in separate file per .editorconfig.**
The project enforces "one type per file" via `.editorconfig`. `TenantIndexEntry` is placed in `TenantIndexEntry.cs` in the same `Projections/` folder, same namespace. This is consistent with project standards even though the record is tightly coupled to `TenantIndexReadModel`.

### TenantIndexReadModel vs TenantReadModel (Story 5.1)

| Concern | TenantReadModel (Story 5.1) | TenantIndexReadModel (this story) |
|---------|---------------------------|-----------------------------------|
| Scope | One read model per tenant aggregate | One read model for ALL tenants |
| Events handled | 9 (all tenant + config events) | 7 (no config events) |
| Config events | Yes (`TenantConfigurationSet/Removed`) | No — config is per-tenant detail, not index data |
| Guard pattern | None — trusts single-aggregate ordering | `TryGetValue` — fan-in ordering not guaranteed |
| Data structure | Flat properties (TenantId, Name, Members, etc.) | Two dictionaries (Tenants index + UserTenants reverse index) |
| Hosting | Per-aggregate replay via `Project()` | Incremental Apply via hosting layer (NOT `Project()`) |
| ETag notification | Automatic via `Project()` → `FireProjectionChangeNotification()` | Manual — hosting must call `NotifyProjectionChangedAsync()` directly |

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder | File |
|------|---------|--------|------|
| TenantIndexReadModel | Server | Projections/ | TenantIndexReadModel.cs (CREATE) |
| TenantIndexEntry | Server | Projections/ | TenantIndexEntry.cs (CREATE — separate file per .editorconfig) |
| TenantIndexProjection | Server | Projections/ | TenantIndexProjection.cs (CREATE) |
| TenantIndexReadModel tests | Server.Tests | Projections/ | TenantIndexReadModelTests.cs (CREATE) |
| TenantIndexProjection tests | Server.Tests | Projections/ | TenantIndexProjectionTests.cs (CREATE) |

**DO NOT:**

- Create query endpoints, controllers, or REST routes — that's Story 5.3 scope
- Create CachingProjectionActor subclasses — that's Story 5.3 scope
- Create subscription endpoints or event handlers — that's Story 5.3 scope
- Add `IQueryContract` implementations — that's Story 5.3 scope
- Modify existing source files — this story only creates new files in Server/Projections/ and Server.Tests/Projections/
- Modify CommandApi Program.cs — assembly scanning handles projection discovery
- Add new NuGet packages — all dependencies available transitively
- Implement DAPR state store read-modify-write logic — that's Story 5.3 hosting
- Implement ETag retry loop — that's Story 5.3 hosting (AC4 applies to the hosting, not the read model definition)
- Create types outside the Server project
- Create `TenantReadModel` or `TenantProjection` — those are Story 5.1 scope (may or may not exist yet)
- Add aggregate-only properties from `TenantState` (e.g., `HasMembershipHistory`)

### Library & Framework Requirements

**No new NuGet packages required.** All dependencies available transitively:

- `EventStoreProjection<T>` is in `Hexalith.EventStore.Client.Aggregates` — via Server → EventStore.Server → EventStore.Client
- Event types in `Hexalith.Tenants.Contracts.Events` — via Server → Contracts
- Enum types in `Hexalith.Tenants.Contracts.Enums` — via Server → Contracts

**Test dependencies:** xUnit, Shouldly already in `tests/Directory.Build.props`. Server.Tests.csproj already references Server project.

### File Structure Requirements

```
src/Hexalith.Tenants.Server/
├── Aggregates/                           (EXISTS — no changes)
├── Projections/                          (CREATE directory — does not exist yet)
│   ├── TenantIndexReadModel.cs          (CREATE)
│   ├── TenantIndexEntry.cs              (CREATE — separate file per .editorconfig)
│   └── TenantIndexProjection.cs         (CREATE)
└── Validators/                           (EXISTS — no changes)

tests/Hexalith.Tenants.Server.Tests/
├── Aggregates/                           (EXISTS — no changes)
├── Projections/                          (CREATE directory — does not exist yet)
│   ├── TenantIndexReadModelTests.cs     (CREATE)
│   └── TenantIndexProjectionTests.cs    (CREATE)
├── Validators/                           (EXISTS — no changes)
└── ScaffoldingSmokeTests.cs              (EXISTS — no changes)
```

**Directory Note:** The `Projections/` directories do NOT exist yet — create them. Story 5.1 is `ready-for-dev` but not yet implemented. If Story 5.1 is being implemented in parallel and has already created `Projections/`, use the existing directory — do NOT recreate it or modify any Story 5.1 files within it.

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**TenantIndexReadModel tests — verify each Apply method:**

| # | Test | Setup | Expected | AC |
|---|------|-------|----------|-----|
| IX1 | Apply TenantCreated adds entry to Tenants | New TenantIndexReadModel, apply TenantCreated | Tenants["acme"] == TenantIndexEntry("Acme Corp", Active) | #1 |
| IX2 | Apply TenantUpdated updates name in index | Apply TenantCreated then TenantUpdated | Tenants["acme"].Name == "Acme Updated" | #1 |
| IX3 | Apply TenantDisabled updates status | Apply TenantCreated then TenantDisabled | Tenants["acme"].Status == Disabled | #2 |
| IX4 | Apply TenantEnabled updates status | Apply TenantCreated, TenantDisabled, TenantEnabled | Tenants["acme"].Status == Active | #2 |
| IX5 | Apply UserAddedToTenant adds user-tenant mapping | Apply TenantCreated then UserAddedToTenant | UserTenants["user1"]["acme"] == TenantOwner | #3 |
| IX6 | Apply UserRemovedFromTenant removes user-tenant mapping | Add then remove user | UserTenants does not contain "user1" (cleaned up) | #3 |
| IX7 | Multiple tenants in index | Create 3 tenants | Tenants.Count == 3, each with correct data | #1, #5 |
| IX8 | User in multiple tenants | Add user to 3 tenants | UserTenants["user1"].Count == 3 | #3 |
| IX9 | Remove user from one of multiple tenants | Add to 3, remove from 1 | UserTenants["user1"].Count == 2, removed tenant absent | #3 |
| IX10 | Apply TenantDisabled when tenant not in index (out-of-order) | Apply TenantDisabled without prior TenantCreated | No exception, Tenants remains empty | #2 |
| IX10b | Apply TenantUpdated when tenant not in index (out-of-order) | Apply TenantUpdated without prior TenantCreated | No exception, Tenants remains empty | #1 |
| IX11 | Apply UserRemovedFromTenant when user not in index | Apply UserRemovedFromTenant without prior UserAddedToTenant | No exception, UserTenants remains empty | #3 |
| IX11b | Apply UserRoleChanged when user not in index (out-of-order) | Apply UserRoleChanged without prior UserAddedToTenant | No exception, UserTenants remains empty | #3 |
| IX12 | Apply UserRoleChanged updates role in user-tenant mapping | Apply UserAddedToTenant then UserRoleChanged | UserTenants["user1"]["acme"] == new role | #3 |
| IX13 | Full lifecycle test across multiple tenants and users | Create 3 tenants, add users across them, disable one, remove users, change roles | Assert final state of Tenants and UserTenants completely | #1-3, #5 |

**TenantIndexProjection tests — verify reflection-based Apply discovery:**

| # | Test | Setup | Expected | AC |
|---|------|-------|----------|-----|
| IX14 | Project returns TenantIndexReadModel from events | Create TenantIndexProjection, call Project() with TenantCreated + UserAddedToTenant | Returned model has correct Tenants and UserTenants | #1, #3 |
| IX15 | Project handles all event types with correct final state | Project() with deterministic event sequence covering all 7 event types | Assert Tenants and UserTenants completely | #1-3 |
| IX16 | Project with empty event list returns default model | Project() with empty list | Empty Tenants, empty UserTenants | |
| IX17 | Project skips null events gracefully | Project() with nulls interspersed | Valid events still applied | |
| IX18 | TenantIndexProjection discovers all 7 Apply methods (canary) | Reflection count on TenantIndexReadModel Apply methods | Exactly 7: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged | #1-3 |
| IX19 | NamingConventionEngine derives correct domain name | Call `NamingConventionEngine.GetDomainName(typeof(TenantIndexProjection))` | Returns `"tenants"` — if not, the projection needs `OnConfiguring` override (see Domain Name section) | #1 |

**Test patterns:**

```csharp
// TenantIndexReadModel direct Apply test
var model = new TenantIndexReadModel();
model.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.UtcNow));
model.Tenants.ShouldContainKey("acme");
model.Tenants["acme"].Name.ShouldBe("Acme Corp");
model.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
model.UserTenants.ShouldBeEmpty();

// User-tenant mapping test
model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));
model.UserTenants.ShouldContainKey("user1");
model.UserTenants["user1"].ShouldContainKey("acme");
model.UserTenants["user1"]["acme"].ShouldBe(TenantRole.TenantOwner);

// TenantIndexProjection test using Project()
var projection = new TenantIndexProjection();
var events = new object[]
{
    new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
    new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow),
    new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
    new UserAddedToTenant("beta", "user1", TenantRole.TenantReader),
};
TenantIndexReadModel result = projection.Project(events);
result.Tenants.Count.ShouldBe(2);
result.UserTenants["user1"].Count.ShouldBe(2);

// Full lifecycle test (IX13) — multi-tenant, multi-user
var projection = new TenantIndexProjection();
var events = new object[]
{
    new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
    new TenantCreated("beta", "Beta Inc", "desc", DateTimeOffset.Parse("2026-01-02T00:00:00Z")),
    new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.Parse("2026-01-03T00:00:00Z")),
    new TenantUpdated("acme", "Acme Updated", "new desc"),
    new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
    new UserAddedToTenant("beta", "user1", TenantRole.TenantReader),
    new UserAddedToTenant("acme", "user2", TenantRole.TenantContributor),
    new UserAddedToTenant("gamma", "user3", TenantRole.TenantOwner),
    new UserRoleChanged("beta", "user1", TenantRole.TenantReader, TenantRole.TenantContributor),
    new UserRemovedFromTenant("acme", "user2"),
    new TenantDisabled("gamma", DateTimeOffset.UtcNow),
};
TenantIndexReadModel result = projection.Project(events);
// Tenants assertions
result.Tenants.Count.ShouldBe(3);
result.Tenants["acme"].Name.ShouldBe("Acme Updated");
result.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
result.Tenants["beta"].Status.ShouldBe(TenantStatus.Active);
result.Tenants["gamma"].Status.ShouldBe(TenantStatus.Disabled);
// UserTenants assertions
result.UserTenants.Count.ShouldBe(2); // user1, user3 (user2 removed)
result.UserTenants["user1"].Count.ShouldBe(2); // acme, beta
result.UserTenants["user1"]["beta"].ShouldBe(TenantRole.TenantContributor); // role changed
result.UserTenants["user3"]["gamma"].ShouldBe(TenantRole.TenantOwner);
result.UserTenants.ShouldNotContainKey("user2"); // removed, cleaned up
```

**Canary test (IX18) — Apply method count:**

```csharp
var applyMethods = typeof(TenantIndexReadModel)
    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
    .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
    .ToList();
applyMethods.Count.ShouldBe(7, "TenantIndexReadModel should handle 7 event types: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged");
```

**NamingConventionEngine test (IX19):**

```csharp
// IX19: Verify domain name derivation — requires using Hexalith.EventStore.Client.Conventions
using Hexalith.EventStore.Client.Conventions;

string domainName = NamingConventionEngine.GetDomainName(typeof(TenantIndexProjection));
domainName.ShouldBe("tenants", "TenantIndexProjection must route to 'tenants' domain");
```

**Null guard tests:** Test `Should.Throw<ArgumentNullException>(() => model.Apply((TenantCreated)null!))` for at least one event type.

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all Apply method parameters
- `sealed` classes — no subclassing needed
- Private setters on read model dictionary properties (mutated only via Apply methods)
- XML doc comments: `/// <summary>` on `TenantIndexProjection` class only. `TenantIndexReadModel` and `TenantIndexEntry` are self-documenting

### Previous Story Intelligence

**Story 5.1 (ready-for-dev) — Per-Tenant & Global Admin Projections:**

- Established the `EventStoreProjection<TReadModel>` pattern for Hexalith.Tenants projections
- Created `TenantReadModel` with Apply methods for all 9 tenant events — mirror Apply method signatures for event type compatibility
- `private set` is correct (not `init`) because Apply methods mutate post-construction
- Assembly scanning auto-discovers projection classes — no DI registration changes needed
- Domain name convention: `NamingConventionEngine.GetDomainName()` must return `"tenants"` for projection routing
- Key design decision: read models separate from aggregate state (no shared base class)
- D5 critical awareness: Rejection events exist in the event stream — typed `Project()` silently skips unknown event types (no Apply method → continue). This story's `TenantIndexReadModel` also benefits from this behavior — no need to handle rejection events
- D8: projections trust the event stream (no precondition validation). EXCEPTION for this story: `TenantIndexReadModel` uses `TryGetValue` guards because fan-in event ordering is not guaranteed (see D3 above)

**Story 4.2 (done) — Event Subscription & Local Projection Pattern:**

- Established client-side projection pattern (`TenantLocalState` with Apply methods)
- Client-side projections are for consuming services; server-side projections are for tenant service's own queries
- Apply method signature pattern: `public void Apply(TEvent e)` with null guards

**Story 3.3 (done) — Tenant Configuration Management:**

- Added `TenantConfigurationSet` and `TenantConfigurationRemoved` events
- These events are NOT handled by `TenantIndexReadModel` — configuration data is not part of the cross-tenant index (per-tenant detail queries use `TenantReadModel` from Story 5.1)

### Git Intelligence

Recent commits show Epic 4 completion and Story 5.1 story creation. The codebase has established:
- Apply method pattern with `ArgumentNullException.ThrowIfNull(e)` null guards
- Allman brace style consistently
- Private setters for state properties
- `sealed` classes where no inheritance is needed
- Server project already references Contracts and EventStore.Server — no changes needed

### Cross-Story Dependencies

**This story depends on:**
- Story 2.1 (done): Event contracts in Contracts project — `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`
- Story 2.3 (done): `TenantState` Apply method pattern as reference

**Stories that depend on this:**
- Story 5.3: Query Endpoints & Authorization — uses `TenantIndexReadModel` via hosting layer (subscription handler + `CachingProjectionActor` or manual DAPR state store) for `ListTenantsQuery` and `GetUserTenantsQuery`
- Story 6.2: In-Memory Projection & Conformance Tests — may include `InMemoryTenantIndexProjection`

**Parallel with Story 5.1:** This story can be implemented independently of Story 5.1. Both create projection files in the same `Projections/` directory but have no code dependencies on each other. If Story 5.1 is not yet implemented, the `Projections/` directory may not exist — create it.

### Critical Anti-Patterns (DO NOT)

- **DO NOT** put Apply methods on the Projection class — they go on the ReadModel class
- **DO NOT** create query endpoints, controllers, or `IQueryContract` types — Story 5.3 scope
- **DO NOT** implement DAPR state store read-modify-write with ETag retry — Story 5.3 hosting scope
- **DO NOT** implement `CachingProjectionActor` subclass — Story 5.3 scope
- **DO NOT** create subscription endpoints or event handlers — Story 5.3 scope
- **DO NOT** handle `TenantConfigurationSet` or `TenantConfigurationRemoved` — not part of the cross-tenant index (per-tenant details are Story 5.1/5.3)
- **DO NOT** create `TenantReadModel`, `TenantProjection`, or any Story 5.1 types — those are separate scope
- **DO NOT** modify existing source files — this story only creates new files
- **DO NOT** add new NuGet packages
- **DO NOT** modify CommandApi Program.cs
- **DO NOT** use `init` setters — Apply methods mutate post-construction, requiring `private set`
- **DO NOT** create shared base classes between index read model and per-tenant read model
- **DO NOT** trust aggregate event ordering in Apply methods — fan-in means events from different aggregates may arrive out of order (use `TryGetValue` guards)
- **DO NOT** add event deduplication logic to Apply methods — all Apply methods are naturally idempotent (dictionary assignment with same key+value is a no-op). DAPR pub/sub is at-least-once; duplicate events are harmless

### Concurrency & Hosting Notes for Story 5.3 Handoff

**Architecture requirement (AC4):** The cross-tenant index key is a shared write target. Every `TenantCreated`, `TenantDisabled`, etc. triggers a read-modify-write on the same DAPR state store key. The architecture mandates ETag-based optimistic concurrency with retry.

**Pattern for Story 5.3:**
1. `GET` state (serialized `TenantIndexReadModel`) from DAPR state store with ETag
2. Deserialize, apply new event via Apply method
3. Serialize and `PUT` state with ETag
4. On `409 Conflict` → retry from step 1 (max 3 attempts)

**CachingProjectionActor verification (Task 3):** The architecture D4 Revision says `CachingProjectionActor` adoption is conditional on fan-in support. Task 3 of this story investigates this and documents findings. If `CachingProjectionActor` doesn't support fan-in, Story 5.3 falls back to manual DAPR state store with ETag retry (original D4 design).

**Pagination:** `Dictionary<string, TenantIndexEntry> Tenants` is unordered. FR30 requires cursor-based pagination with consistent ordering. Story 5.3 must sort at query time: `.OrderBy(kvp => kvp.Key).Skip(cursor).Take(pageSize)`. At 1K entries, sorting is sub-millisecond — no need for `SortedDictionary`.

**ETag invalidation for incremental Apply:** `EventStoreProjection<T>.FireProjectionChangeNotification()` is called inside `Project()`, NOT after individual Apply calls. Since fan-in hosting calls Apply incrementally (not via `Project()`), Story 5.3 must call `Notifier.NotifyProjectionChangedAsync()` directly after each Apply to trigger ETag cache invalidation.

**Authorization:** The cross-tenant index is data-only — it has NO authorization logic. Story 5.3 handles authorization for ListTenants and GetUserTenants queries via JWT claims and the `GlobalAdministratorProjection` (Story 5.1). The index projection does not need to know about roles or permissions.

**`ProjectFromJson()` rejection event warning:** The typed `Project(IEnumerable)` method silently skips unknown event types (no Apply method → continue). However, `ProjectFromJson(JsonElement)` **throws** `InvalidOperationException` on unknown event types. If Story 5.3 ever uses `ProjectFromJson()` with a stream containing rejection events (`TenantDisabledRejection`, etc.), it will crash. Story 5.3 must either: (a) use typed `Project()`, (b) filter rejection events before calling `ProjectFromJson()`, or (c) add no-op Apply methods for rejection events on the read model.

### Project Structure Notes

- `Server/Projections/` folder matches architecture.md directory structure (`TenantIndexProjection.cs`, `TenantIndexReadModel.cs` listed in architecture)
- Server.csproj already references Contracts and EventStore.Server — no changes needed
- Server.Tests.csproj already references Server — no changes needed
- Solution file already includes Server and Server.Tests — no changes needed

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.2] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] — Epic objectives: tenant discovery & query
- [Source: _bmad-output/planning-artifacts/prd.md#FR25-FR30] — Tenant discovery & query requirements
- [Source: _bmad-output/planning-artifacts/prd.md#FR28] — GetUserTenants query (user-to-tenant mapping)
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Tenant Index Projections] — Fan-in pattern, DAPR state store key design, ETag concurrency
- [Source: _bmad-output/planning-artifacts/architecture.md#D4 Revision] — CachingProjectionActor conditional adoption, fan-in verification caveat
- [Source: _bmad-output/planning-artifacts/architecture.md#Type Location Rules] — Projections and ReadModels in Server project
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — TenantIndexProjection.cs and TenantIndexReadModel.cs in Server/Projections/
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs] — Base class with reflection-based Apply discovery
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs] — Caching actor for query hosting (Story 5.3)
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — Apply method pattern reference
- [Source: src/Hexalith.Tenants.Contracts/Events/] — All event record types
- [Source: _bmad-output/implementation-artifacts/5-1-per-tenant-and-global-admin-projections.md] — Previous story with design decisions and patterns

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
