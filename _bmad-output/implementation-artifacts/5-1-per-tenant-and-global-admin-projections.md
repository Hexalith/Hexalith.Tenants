# Story 5.1: Per-Tenant & Global Admin Projections

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want per-tenant read model projections and a global administrator projection maintained automatically from domain events,
So that query endpoints have up-to-date data for tenant details, user lists, and admin lookups.

## Acceptance Criteria

1. **Given** a TenantCreated event is published
   **When** the TenantProjection processes the event
   **Then** a TenantReadModel is created with the tenant's ID, name, description, status (Active), empty members dictionary, empty configuration dictionary, and CreatedAt timestamp

2. **Given** UserAddedToTenant, UserRemovedFromTenant, and UserRoleChanged events are published
   **When** the TenantProjection processes these events
   **Then** the TenantReadModel's members dictionary is updated accordingly (add/remove/change entries)

3. **Given** TenantConfigurationSet and TenantConfigurationRemoved events are published
   **When** the TenantProjection processes these events
   **Then** the TenantReadModel's configuration dictionary is updated accordingly (add/remove entries)

4. **Given** TenantDisabled and TenantEnabled events are published
   **When** the TenantProjection processes these events
   **Then** the TenantReadModel's status is updated to Disabled or Active

5. **Given** GlobalAdministratorSet and GlobalAdministratorRemoved events are published
   **When** the GlobalAdministratorProjection processes these events
   **Then** the GlobalAdministratorReadModel is updated with the current set of global administrator user IDs

6. **Given** both projection classes exist in the Server project
   **When** the application starts
   **Then** projections are auto-discovered via EventStore's assembly scanning and registered for event processing

## Tasks / Subtasks

- [x] Task 1: Create `TenantReadModel.cs` (AC: #1, #2, #3, #4) — BUILD FIRST: projection depends on this
    - [x] 1.1: Create `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
    - [x] 1.2: Properties: `TenantId` (string), `Name` (string), `Description` (string?), `Status` (TenantStatus), `Members` (Dictionary<string, TenantRole>), `Configuration` (Dictionary<string, string>), `CreatedAt` (DateTimeOffset)
    - [x] 1.3: Apply methods for all 9 tenant events: `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved`
    - [x] 1.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create `TenantProjection.cs` (AC: #1, #2, #3, #4, #6)
    - [x] 2.1: Create `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs` inheriting `EventStoreProjection<TenantReadModel>`
    - [x] 2.2: No additional code needed — `EventStoreProjection<T>` uses reflection to discover `Apply` methods on `TenantReadModel`
    - [x] 2.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create `GlobalAdministratorReadModel.cs` (AC: #5)
    - [x] 3.1: Create `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
    - [x] 3.2: Properties: `Administrators` (HashSet<string>)
    - [x] 3.3: Apply methods for: `GlobalAdministratorSet`, `GlobalAdministratorRemoved`
    - [x] 3.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 4: Create `GlobalAdministratorProjection.cs` (AC: #5, #6)
    - [x] 4.1: Create `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorProjection.cs` inheriting `EventStoreProjection<GlobalAdministratorReadModel>`
    - [x] 4.2: No additional code needed — reflection-based Apply discovery
    - [x] 4.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 5: Create unit tests (all ACs)
    - [x] 5.1: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantReadModelTests.cs`
    - [x] 5.2: Create `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionTests.cs`
    - [x] 5.3: Create `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorReadModelTests.cs`
    - [x] 5.4: Create `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionTests.cs`
    - [x] 5.5: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [x] Task 6: Build verification (all ACs)
    - [x] 6.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 6.2: `dotnet test Hexalith.Tenants.slnx` — all tests pass, no regressions

## Dev Notes

### Scope: Read Model Classes + Projection Shells Only

This story creates the per-tenant and global admin **read models** (the data structures) and **projections** (the `EventStoreProjection<T>` subclasses that EventStore auto-discovers). Query endpoints (`GET /api/tenants/*`) are Story 5.3. Cross-tenant index projection (`TenantIndexProjection`) is Story 5.2. This story is the data foundation for the query side.

### Architecture: EventStoreProjection<T> Pattern

EventStore provides `EventStoreProjection<TReadModel>` (in `Hexalith.EventStore.Client.Aggregates`) as the base class for read model projections. The pattern:

1. Create a **read model class** (e.g., `TenantReadModel`) with public `void Apply(TEvent e)` methods for each event type it handles
2. Create a **projection class** (e.g., `TenantProjection`) that inherits `EventStoreProjection<TReadModel>`
3. `EventStoreProjection<T>` uses reflection to discover all `Apply` methods on the read model class and calls them during event replay
4. Assembly scanning (`AssemblyScanner`) auto-discovers all `EventStoreProjection<T>` subclasses at startup

**Key insight:** The Apply methods go on the **read model** class, NOT the projection class. The projection class is typically an empty shell that just inherits the base class. This is identical to how `TenantState` has Apply methods while `TenantAggregate` delegates state mutation to them.

### Read Model Design

**TenantReadModel** — mirrors `TenantState` structurally (same properties, same Apply methods). The read model IS the query-side view of the aggregate's state. **Why a separate class instead of reusing `TenantState`?** Read models are query-optimized; state classes are invariant-optimized. They happen to be identical at MVP, but they serve different masters and will diverge as query needs evolve (e.g., adding computed fields, denormalized data). DO NOT create a shared base class or attempt to reuse `TenantState` — keep them independent. Property types:

```csharp
// src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class TenantReadModel
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }
    public Dictionary<string, TenantRole> Members { get; private set; } = new();
    public Dictionary<string, string> Configuration { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    public void Apply(TenantCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        TenantId = e.TenantId;
        Name = e.Name;
        Description = e.Description;
        Status = TenantStatus.Active;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(TenantUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Name = e.Name;
        Description = e.Description;
    }

    public void Apply(TenantDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Status = TenantStatus.Disabled;
    }

    public void Apply(TenantEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Status = TenantStatus.Active;
    }

    public void Apply(UserAddedToTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Members[e.UserId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Members.Remove(e.UserId);
    }

    public void Apply(UserRoleChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Members[e.UserId] = e.NewRole;
    }

    public void Apply(TenantConfigurationSet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Configuration[e.Key] = e.Value;
    }

    public void Apply(TenantConfigurationRemoved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Configuration.Remove(e.Key);
    }
}
```

**IMPORTANT:** `TenantReadModel` does NOT include `HasMembershipHistory` (exists on `TenantState` for aggregate invariant checking — irrelevant for query-side read models). Keep read models lean — only data needed for queries.

**GlobalAdministratorReadModel** — mirrors `GlobalAdministratorsState` (minus `Bootstrapped` flag — that's aggregate-only):

```csharp
// src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class GlobalAdministratorReadModel
{
    public HashSet<string> Administrators { get; private set; } = new();

    public void Apply(GlobalAdministratorSet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Add(e.UserId);
    }

    public void Apply(GlobalAdministratorRemoved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Remove(e.UserId);
    }
}
```

### Projection Classes — Empty Shells

```csharp
// src/Hexalith.Tenants.Server/Projections/TenantProjection.cs
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Per-tenant read model projection. Auto-discovered by EventStore's assembly scanning.
/// </summary>
[EventStoreDomain("tenants")]
public sealed class TenantProjection : EventStoreProjection<TenantReadModel>
{
}
```

```csharp
// src/Hexalith.Tenants.Server/Projections/GlobalAdministratorProjection.cs
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Global administrator read model projection. Auto-discovered by EventStore's assembly scanning.
/// </summary>
[EventStoreDomain("global-administrators")]
public sealed class GlobalAdministratorProjection : EventStoreProjection<GlobalAdministratorReadModel>
{
}
```

**IMPORTANT:** The projection classes are `sealed` — no subclassing needed. They remain shell types because `EventStoreProjection<T>` discovers Apply methods on `T` (the read model) via reflection, but they now use `EventStoreDomainAttribute` to pin projection discovery to the intended domains and avoid singular/plural naming drift.

### Differences Between ReadModel and State

| Concern                      | TenantState (aggregate)                    | TenantReadModel (projection)                                               |
| ---------------------------- | ------------------------------------------ | -------------------------------------------------------------------------- |
| `HasMembershipHistory`       | Yes — invariant for "new tenant" detection | No — query-irrelevant                                                      |
| `Bootstrapped` (GlobalAdmin) | Yes — prevents duplicate bootstrap         | No — query-irrelevant                                                      |
| Private setters              | Yes                                        | Yes — Apply methods mutate via private setters                             |
| Parameterless constructor    | Yes (implicit)                             | Yes (required by `EventStoreProjection<T>` which calls `new TReadModel()`) |

### Assembly Scanning — No Manual Registration

The `AddEventStore()` call in `Program.cs` triggers `AssemblyScanner` which discovers all `EventStoreProjection<T>` subclasses in referenced assemblies. Since `CommandApi` references `Server`, and `TenantProjection`/`GlobalAdministratorProjection` live in `Server`, they will be auto-discovered. **No changes to Program.cs or any DI registration are needed.**

Verify auto-discovery works by checking that `AssemblyScanner` finds the new projection types. If it doesn't, ensure the Server assembly is referenced by CommandApi (it already is — `CommandApi.csproj` references `Server.csproj`).

### Domain Name Convention Verification

`EventStoreProjection<T>` derives the domain name via `NamingConventionEngine.GetDomainName(typeof(TenantProjection))`. This must return `tenants` (matching the DAPR state store name `tenants-eventstore` and topic `tenants.events`). If the convention engine derives a different name (e.g., `tenant` singular), the projection won't route correctly. **Verification step:** After creating `TenantProjection`, write a quick assertion or manual check that `NamingConventionEngine.GetDomainName(typeof(TenantProjection))` returns `"tenants"`. If it doesn't, override `OnConfiguring` to set the domain name explicitly:

```csharp
protected override void OnConfiguring(EventStoreDomainOptions options)
{
    ArgumentNullException.ThrowIfNull(options);
    options.DomainName = "tenants";
}
```

Post-review implementation note: the final code uses `EventStoreDomainAttribute` on both projection classes (`"tenants"` and `"global-administrators"`) and adds regression tests asserting the resolved domain names via `NamingConventionEngine.GetDomainName(...)`.

### Design Decisions & Assumptions

**D1: Read model is a separate class from aggregate state (not shared).**
`TenantReadModel` mirrors `TenantState` structurally but is intentionally a separate class. Read models are query-optimized; state classes are invariant-optimized. At MVP they're identical, but they WILL diverge (e.g., adding denormalized fields, computed properties, or `UpdatedAt` tracking for query consumers). DO NOT create a shared base class, extract a common interface, or reuse `TenantState` directly.

**D2: `private set` is correct (not `init`).**
Apply methods mutate the read model post-construction (`EventStoreProjection<T>.Project()` calls `new TReadModel()` then invokes Apply in a loop). `init` setters would fail because mutation happens after construction. `private set` is the established pattern matching `TenantState`.

**D3: `Dictionary<string, TenantRole>` is the right MVP data structure for Members.**
Query endpoints (Story 5.3) will need to list members, which requires iterating the dictionary. At MVP scale (500 members max), `.Select()` transform at query time is negligible. If Story 5.3 needs a `List<TenantMemberReadModel>` for pagination, that's a read model divergence point — but premature to optimize now.

**D4: No `UpdatedAt` / `LastEventTimestamp` on read models.**
Current event contracts lack timestamps on most events (only `TenantCreated.CreatedAt`, `TenantDisabled.DisabledAt`, `TenantEnabled.EnabledAt`). Adding a projection-time `UpdatedAt` (using `DateTimeOffset.UtcNow` in each Apply) would make projections non-deterministic (different replay = different timestamp). If Story 5.3 query consumers need "last modified", it should be addressed there. Out of scope for 5.1.

**D5 (CRITICAL AWARENESS — Story 5.3): Rejection events exist in the event stream.**
`EventPersister` stores ALL `domainResult.Events` (including rejection events like `TenantDisabledRejection`) in the same event stream as successful domain events. The typed `EventStoreProjection<T>.Project(IEnumerable)` method **silently skips** unknown event types (no Apply method → continue). The JSON `ProjectFromJson(JsonElement)` method **throws** `InvalidOperationException` on unknown event types. When Story 5.3 builds the `CachingProjectionActor` query execution path, it MUST either: (a) use the typed `Project()` method, (b) filter rejection events before calling `ProjectFromJson()`, or (c) add no-op Apply methods for rejection events on the read models. This is NOT a Story 5.1 concern — the projection definitions are safe. Document this for Story 5.3 handoff.

**D6: `sealed` on read models is safe for Story 6.2.**
Story 6.2's `InMemoryTenantProjection` inherits `EventStoreProjection<TenantReadModel>` — it reuses the read model class, not subclasses it. `sealed` on `TenantReadModel` and `GlobalAdministratorReadModel` is correct.

**D7: Replay-from-scratch is the projection contract.**
`EventStoreProjection<T>.Project()` always starts from `new TReadModel()`. The read model MUST be fully reconstructable from a complete event replay at any time. DO NOT add constructor dependencies, initialization logic, or required setup steps to the read model. Default property values must represent a valid empty state.

**D8: Projections are intentionally non-defensive — they trust the event stream.**
Apply methods do not validate preconditions (e.g., `Apply(UserRoleChanged)` doesn't check if the user is in `Members` first). In a correct event stream, `UserAddedToTenant` always precedes `UserRoleChanged`. If a corrupted stream is replayed with missing events, the read model will silently add a phantom member via `Members[e.UserId] = e.NewRole`. This is by design — the projection trusts the event stream, matching the aggregate's own Apply pattern. Data quality issues should surface through monitoring, not defensive projection code.

**D9: Mutable collection exposure is acceptable at MVP.**
`Dictionary<string, TenantRole> Members` exposes a mutable reference via the public getter. Callers could mutate the collection. For Story 5.1 this is acceptable — read models are created per-projection-call and short-lived. `CachingProjectionActor` caches the serialized `JsonElement`, not the read model object. If Story 5.3 exposes read models directly (not via JSON serialization), consider changing to `IReadOnlyDictionary<string, TenantRole>` backed by a private field. Same applies to `Configuration` and `Administrators`.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type                                | Project      | Folder       | File                                           |
| ----------------------------------- | ------------ | ------------ | ---------------------------------------------- |
| TenantReadModel                     | Server       | Projections/ | TenantReadModel.cs (CREATE)                    |
| TenantProjection                    | Server       | Projections/ | TenantProjection.cs (CREATE)                   |
| GlobalAdministratorReadModel        | Server       | Projections/ | GlobalAdministratorReadModel.cs (CREATE)       |
| GlobalAdministratorProjection       | Server       | Projections/ | GlobalAdministratorProjection.cs (CREATE)      |
| TenantReadModel tests               | Server.Tests | Projections/ | TenantReadModelTests.cs (CREATE)               |
| TenantProjection tests              | Server.Tests | Projections/ | TenantProjectionTests.cs (CREATE)              |
| GlobalAdministratorReadModel tests  | Server.Tests | Projections/ | GlobalAdministratorReadModelTests.cs (CREATE)  |
| GlobalAdministratorProjection tests | Server.Tests | Projections/ | GlobalAdministratorProjectionTests.cs (CREATE) |

**DO NOT:**

- Create types outside the Server project — projections and read models live in Server
- Create query endpoints or controllers — that's Story 5.3 scope
- Create TenantIndexProjection or TenantIndexReadModel — that's Story 5.2 scope
- Add new NuGet packages — Server.csproj already has all dependencies (EventStore.Server brings EventStore.Client which has `EventStoreProjection<T>`)
- Modify existing source files — this story only creates new files in Server/Projections/ and Server.Tests/Projections/
- Add `IQueryContract` implementations — that's Story 5.3 scope
- Add query contracts to Contracts project — that's Story 5.3 scope
- Modify CommandApi Program.cs — assembly scanning handles projection discovery automatically
- Include `HasMembershipHistory` on TenantReadModel or `Bootstrapped` on GlobalAdministratorReadModel — these are aggregate-only concerns
- Create CachingProjectionActor subclasses — that infrastructure is for the query pipeline (Story 5.3), not the projection definitions

### Library & Framework Requirements

**No new NuGet packages required.** All dependencies available transitively:

- `EventStoreProjection<T>` is in `Hexalith.EventStore.Client.Aggregates` — available via Server → EventStore.Server → EventStore.Client
- Event types are in `Hexalith.Tenants.Contracts.Events` — available via Server → Contracts
- Enum types are in `Hexalith.Tenants.Contracts.Enums` — available via Server → Contracts

**Test dependencies:** xUnit, Shouldly already in `tests/Directory.Build.props`. Server.Tests.csproj already references Server project.

### File Structure Requirements

```
src/Hexalith.Tenants.Server/
├── Aggregates/                           (EXISTS — no changes)
│   ├── TenantAggregate.cs
│   ├── TenantState.cs
│   ├── GlobalAdministratorsAggregate.cs
│   └── GlobalAdministratorsState.cs
├── Projections/                          (CREATE directory)
│   ├── TenantReadModel.cs               (CREATE)
│   ├── TenantProjection.cs              (CREATE)
│   ├── GlobalAdministratorReadModel.cs  (CREATE)
│   └── GlobalAdministratorProjection.cs (CREATE)
└── Validators/                           (EXISTS — no changes)

tests/Hexalith.Tenants.Server.Tests/
├── Aggregates/                           (EXISTS — no changes)
├── Projections/                          (CREATE directory)
│   ├── TenantReadModelTests.cs          (CREATE)
│   ├── TenantProjectionTests.cs         (CREATE)
│   ├── GlobalAdministratorReadModelTests.cs  (CREATE)
│   └── GlobalAdministratorProjectionTests.cs (CREATE)
├── Validators/                           (EXISTS — no changes)
└── ScaffoldingSmokeTests.cs              (EXISTS — no changes)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**TenantReadModel tests — verify each Apply method mutates state correctly:**

| #   | Test                                             | Setup                                                  | Expected                                                                               | AC   |
| --- | ------------------------------------------------ | ------------------------------------------------------ | -------------------------------------------------------------------------------------- | ---- |
| P1  | Apply TenantCreated sets all properties          | New TenantReadModel, apply TenantCreated               | TenantId, Name, Description, Status=Active, CreatedAt set; Members/Configuration empty | #1   |
| P2  | Apply TenantUpdated updates name and description | Apply TenantCreated then TenantUpdated                 | Name and Description updated                                                           | #1   |
| P3  | Apply TenantDisabled sets status                 | Apply TenantCreated then TenantDisabled                | Status == Disabled                                                                     | #4   |
| P4  | Apply TenantEnabled sets status                  | Apply TenantCreated, TenantDisabled, TenantEnabled     | Status == Active                                                                       | #4   |
| P5  | Apply UserAddedToTenant adds member              | Apply TenantCreated then UserAddedToTenant             | Members contains user with correct role                                                | #2   |
| P6  | Apply UserRemovedFromTenant removes member       | Add then remove user                                   | Members no longer contains user                                                        | #2   |
| P7  | Apply UserRoleChanged updates role               | Add user then change role                              | Members[userId] == new role                                                            | #2   |
| P8  | Apply TenantConfigurationSet adds config         | Apply TenantCreated then TenantConfigurationSet        | Configuration contains key-value                                                       | #3   |
| P9  | Apply TenantConfigurationRemoved removes config  | Set then remove config                                 | Configuration no longer contains key                                                   | #3   |
| P10 | Apply multiple events in sequence                | Full lifecycle: create, add users, set config, disable | Final state reflects all mutations                                                     | #1-4 |

**TenantProjection tests — verify the `EventStoreProjection<T>` base class discovers and invokes Apply methods:**

| #   | Test                                                       | Setup                                                                                                                                   | Expected                                                                                                                                                                                                                                         | AC         |
| --- | ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| P11 | Project returns TenantReadModel from events                | Create TenantProjection, call Project() with TenantCreated + UserAddedToTenant events                                                   | Returned TenantReadModel has correct state                                                                                                                                                                                                       | #1, #2, #6 |
| P12 | Project handles all 9 event types with correct final state | Call Project() with a deterministic sequence of all 9 event types                                                                       | Assert every property on final TenantReadModel: TenantId, Name (updated), Description (updated), Status (re-enabled after disable), Members (added + role changed - removed = net result), Configuration (set + removed = net result), CreatedAt | #1-4, #6   |
| P13 | Project with empty event list returns default model        | Call Project() with empty list                                                                                                          | TenantReadModel with defaults (empty strings, empty dicts)                                                                                                                                                                                       | #6         |
| P19 | Project skips null events gracefully                       | Call Project() with list containing null entries interspersed with valid events                                                         | No exception thrown, valid events still applied correctly                                                                                                                                                                                        | #6         |
| P20 | TenantProjection discovers all 9 Apply methods (canary)    | Use reflection to count Apply methods on TenantReadModel matching EventStoreProjection discovery rules (public, void, single parameter) | Exactly 9 Apply methods discovered — fails if a new event is added to Contracts but not handled                                                                                                                                                  | #1-4, #6   |

**GlobalAdministratorReadModel tests:**

| #   | Test                                                   | Setup                                   | Expected                                 | AC  |
| --- | ------------------------------------------------------ | --------------------------------------- | ---------------------------------------- | --- |
| P14 | Apply GlobalAdministratorSet adds administrator        | New model, apply GlobalAdministratorSet | Administrators contains userId           | #5  |
| P15 | Apply GlobalAdministratorRemoved removes administrator | Set then remove                         | Administrators no longer contains userId | #5  |
| P16 | Apply multiple GlobalAdministratorSet events           | Add multiple admins                     | Administrators contains all userIds      | #5  |
| P17 | GlobalAdministratorSet is idempotent                   | Apply same userId twice                 | Administrators.Count == 1 (HashSet)      | #5  |

**GlobalAdministratorProjection tests:**

| #   | Test                                                                 | Setup                                                                                          | Expected                                                                           | AC     |
| --- | -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- | ------ |
| P18 | Project returns GlobalAdministratorReadModel from events             | Create projection, call Project() with events                                                  | Correct admin set                                                                  | #5, #6 |
| P21 | GlobalAdministratorProjection discovers all 2 Apply methods (canary) | Use reflection to count Apply methods on GlobalAdministratorReadModel matching discovery rules | Exactly 2 Apply methods discovered — fails if a new event is added but not handled | #5, #6 |

**Test patterns:**

```csharp
// TenantReadModel direct Apply test
var model = new TenantReadModel();
model.Apply(new TenantCreated("acme", "Acme Corp", "Test tenant", DateTimeOffset.UtcNow));
model.TenantId.ShouldBe("acme");
model.Name.ShouldBe("Acme Corp");
model.Status.ShouldBe(TenantStatus.Active);
model.Members.ShouldBeEmpty();
model.Configuration.ShouldBeEmpty();

// TenantProjection test using Project() — verifies reflection-based Apply discovery
var projection = new TenantProjection();
var events = new object[]
{
    new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
    new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
};
TenantReadModel result = projection.Project(events);
result.TenantId.ShouldBe("acme");
result.Members.ShouldContainKey("user1");
result.Members["user1"].ShouldBe(TenantRole.TenantOwner);
```

**Full 9-event lifecycle test (P12) — assert every property on final state:**

```csharp
// P12: All 9 event types with deterministic final state assertion
var projection = new TenantProjection();
var events = new object[]
{
    new TenantCreated("acme", "Acme Corp", "Original", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
    new TenantUpdated("acme", "Acme Updated", "New desc"),
    new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
    new UserAddedToTenant("acme", "user2", TenantRole.TenantReader),
    new UserRoleChanged("acme", "user2", TenantRole.TenantReader, TenantRole.TenantContributor),
    new UserRemovedFromTenant("acme", "user1"),
    new TenantConfigurationSet("acme", "theme", "dark"),
    new TenantConfigurationRemoved("acme", "theme"),
    new TenantDisabled("acme", DateTimeOffset.UtcNow),
    // Note: only 9 event types, TenantEnabled not in this sequence to verify Disabled final state
};
TenantReadModel result = projection.Project(events);
result.TenantId.ShouldBe("acme");
result.Name.ShouldBe("Acme Updated");
result.Description.ShouldBe("New desc");
result.Status.ShouldBe(TenantStatus.Disabled);
result.CreatedAt.ShouldBe(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
result.Members.Count.ShouldBe(1);
result.Members.ShouldContainKey("user2");
result.Members["user2"].ShouldBe(TenantRole.TenantContributor);
result.Members.ShouldNotContainKey("user1");
result.Configuration.ShouldBeEmpty(); // set then removed
```

**Null event in projection test (P19):**

```csharp
// P19: Project() skips null events — EventStoreProjection<T>.Project() handles this (line 73-75)
var projection = new TenantProjection();
var events = new object?[]
{
    new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
    null,
    new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
    null,
};
// Cast to IEnumerable to match Project() signature
TenantReadModel result = projection.Project((System.Collections.IEnumerable)events);
result.TenantId.ShouldBe("acme");
result.Members.ShouldContainKey("user1");
```

**Canary tests — Apply method discovery count (P20, P21):**

```csharp
// P20: TenantReadModel must have exactly 9 Apply methods (one per tenant event type)
// This is a regression canary — fails if someone adds a new event but forgets the Apply method
var applyMethods = typeof(TenantReadModel)
    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
    .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
    .ToList();
applyMethods.Count.ShouldBe(9, "TenantReadModel should handle all 9 tenant event types");

// P21: GlobalAdministratorReadModel must have exactly 2 Apply methods
var adminApplyMethods = typeof(GlobalAdministratorReadModel)
    .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
    .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
    .ToList();
adminApplyMethods.Count.ShouldBe(2, "GlobalAdministratorReadModel should handle all 2 admin event types");
```

**Null guard tests:** Each Apply method should have `ArgumentNullException.ThrowIfNull(e)` — test with `Should.Throw<ArgumentNullException>(() => model.Apply((TenantCreated)null!))` for at least one event type per read model.

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all Apply method parameters
- `sealed` classes — no subclassing needed
- Private setters on read model properties (mutated only via Apply methods)
- XML doc comments: `/// <summary>` on projection classes only (public API surface for assembly scanning). Read model classes are self-documenting — no XML docs needed

### Previous Story Intelligence

**Story 4.2 (review) — Event Subscription & Local Projection Pattern:**

- Established the **client-side** projection pattern (`TenantLocalState` with Apply methods, `InMemoryTenantProjectionStore`)
- Client-side projections are for consuming services; server-side projections (this story) are for the tenant service's own query endpoints
- The Apply method pattern is identical: `public void Apply(TEvent e)` with null guards
- `TenantLocalState` in the Client project handles the same event types — use it as a reference for Apply method signatures

**Story 4.1 (done) — Client DI Registration:**

- Established `AddHexalithTenants()` extension method pattern
- No DI registration needed for this story — assembly scanning handles it

**Story 3.3 (done) — Tenant Configuration Management:**

- Added `TenantConfigurationSet` and `TenantConfigurationRemoved` events
- These events must be handled by `TenantReadModel.Apply()`

**Story 2.3 (done) — Tenant Aggregate Lifecycle:**

- Established `TenantState` with Apply methods — the pattern to mirror for `TenantReadModel`
- All event types produced by `TenantAggregate` must have corresponding Apply methods on `TenantReadModel`

### Git Intelligence

Recent commits show Epic 4 implementation (client DI, event subscription, local projection). The codebase has established:

- Apply method pattern with `ArgumentNullException.ThrowIfNull(e)` null guards
- Allman brace style consistently
- Private setters for state properties
- `sealed` classes where no inheritance is needed

### Cross-Story Dependencies

**This story depends on:**

- Story 2.1 (done): All event contracts in Contracts project
- Story 2.3 (done): TenantState Apply method pattern to mirror
- Story 2.2 (done): GlobalAdministratorsState Apply method pattern to mirror

**Stories that depend on this:**

- Story 5.2: Cross-Tenant Index Projection — different concern (fan-in across all tenants)
- Story 5.3: Query Endpoints & Authorization — uses these projections via CachingProjectionActor for query responses
- Story 6.2: In-Memory Projection & Conformance Tests — InMemoryTenantProjection in Testing package mirrors TenantProjection

### Critical Anti-Patterns (DO NOT)

- **DO NOT** put Apply methods on the Projection class — they go on the ReadModel class
- **DO NOT** add generic constraints (e.g., `where T : IEventPayload`) on Apply methods — reflection-based discovery matches by method name, return type, and parameter count only
- **DO NOT** create a shared base class between `TenantReadModel` and `TenantState` — they serve different concerns (query vs. invariant) and will diverge
- **DO NOT** include aggregate-only properties (`HasMembershipHistory`, `Bootstrapped`) on read models
- **DO NOT** create query endpoints, controllers, or IQueryContract types — Story 5.3 scope
- **DO NOT** create TenantIndexProjection or TenantIndexReadModel — Story 5.2 scope
- **DO NOT** create CachingProjectionActor subclasses — Story 5.3 scope
- **DO NOT** modify Program.cs or any DI registration — assembly scanning handles discovery
- **DO NOT** add new NuGet packages — all dependencies already available transitively
- **DO NOT** modify existing source files — this story only creates new files
- **DO NOT** make read model properties public set — use private setters, mutated only via Apply methods
- **DO NOT** add validation logic to read models — they are pure state projections, not domain logic
- **DO NOT** create InMemoryTenantProjection — that's Story 6.2 scope (Testing package)

### Project Structure Notes

- Alignment with architecture: `Server/Projections/` folder matches the directory structure in architecture.md exactly
- Server.csproj already references Contracts and EventStore.Server — no changes needed
- Server.Tests.csproj already references Server — no changes needed
- `InternalsVisibleTo` configuration not needed — all types are public
- Solution file (`Hexalith.Tenants.slnx`) already includes Server and Server.Tests — no changes needed

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.1] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] — Epic objectives: tenant discovery & query
- [Source: _bmad-output/planning-artifacts/prd.md#FR25-FR30] — Tenant discovery & query requirements
- [Source: _bmad-output/planning-artifacts/architecture.md#Read Model (Query Side)] — `EventStoreProjection<TReadModel>` pattern, three projections needed
- [Source: _bmad-output/planning-artifacts/architecture.md#Type Location Rules] — Projections and ReadModels in Server project
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — `Server/Projections/` folder with exact file names
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs] — Base class with reflection-based Apply discovery
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/IEventStoreProjection.cs] — Internal interface for DI initialization
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — Apply method pattern to mirror for TenantReadModel
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs] — Apply method pattern to mirror for GlobalAdministratorReadModel
- [Source: src/Hexalith.Tenants.Contracts/Events/] — All event record types the projections must handle

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

Review follow-up applied on 2026-03-18 to fix projection domain routing and strengthen projection regression tests.

### Completion Notes List

- Created `TenantReadModel` with 9 Apply methods mirroring `TenantState` (minus `HasMembershipHistory`). Properties: TenantId, Name, Description, Status, Members, Configuration, CreatedAt with private setters.
- Created `TenantProjection` inheriting `EventStoreProjection<TenantReadModel>` and pinned it to the `tenants` domain with `EventStoreDomainAttribute` after review identified convention-based routing drift.
- Created `GlobalAdministratorReadModel` with 2 Apply methods mirroring `GlobalAdministratorsState` (minus `Bootstrapped`). Property: Administrators (HashSet<string>).
- Created `GlobalAdministratorProjection` inheriting `EventStoreProjection<GlobalAdministratorReadModel>` and pinned it to the `global-administrators` domain with `EventStoreDomainAttribute`.
- Created comprehensive unit tests: 12 TenantReadModel tests (P1-P10, null guard, P20 canary), 5 TenantProjection tests (domain-name regression + P11-P13, P19), 6 GlobalAdministratorReadModel tests (P14-P17, null guard, P21 canary), 3 GlobalAdministratorProjection tests (domain-name regression, P18, empty events).
- Fixed the projection integration canary so the "all nine event types" test actually exercises all 9 distinct tenant event types, including `TenantEnabled`, and verifies the final re-enabled `Active` state.
- Build: 0 warnings, 0 errors. All 155 Server.Tests pass. 2 pre-existing Tier 3 DAPR integration test failures (unrelated — infrastructure dependency).
- Post-review validation: targeted `TenantProjectionTests` and `GlobalAdministratorProjectionTests` passed (8/8).
- No new NuGet packages. No DI registration changes needed — assembly scanning auto-discovers projections once the domains are pinned correctly.

### Change Log

- 2026-03-18: Story 5.1 implementation complete — created per-tenant and global admin read models and projections with comprehensive unit tests.
- 2026-03-18: Applied review fixes — pinned projection domains with `EventStoreDomainAttribute`, added domain-name regression tests, and corrected the full projection event coverage test.

### File List

**New files (8):**

- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorProjection.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantReadModelTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorReadModelTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionTests.cs`

**Modified files (4):**

- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs` — pinned EventStore domain to `tenants`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorProjection.cs` — pinned EventStore domain to `global-administrators`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionTests.cs` — added naming regression coverage and corrected full event-type projection test
- `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionTests.cs` — added naming regression coverage
