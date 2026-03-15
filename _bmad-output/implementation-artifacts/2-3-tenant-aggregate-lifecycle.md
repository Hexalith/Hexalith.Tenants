# Story 2.3: Tenant Aggregate Lifecycle

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want to create, update, disable, and enable tenants,
So that I can manage the tenant lifecycle for all consuming services.

## Acceptance Criteria

1. **Given** no tenant exists with the specified ID **When** a CreateTenant command is processed with a valid tenant ID and name **Then** a TenantCreated event is produced with TenantId, Name, Description, and CreatedAt

2. **Given** a tenant already exists with the specified ID **When** a CreateTenant command is processed with the same ID **Then** the command is rejected with TenantAlreadyExistsRejection

3. **Given** an active tenant exists **When** an UpdateTenant command is processed with new name and description **Then** a TenantUpdated event is produced with the updated metadata

4. **Given** an active tenant exists **When** a DisableTenant command is processed **Then** a TenantDisabled event is produced and the tenant status becomes Disabled

5. **Given** a disabled tenant exists **When** any command targeting that tenant is processed (except EnableTenant) **Then** the command is rejected with TenantDisabledRejection — applies to UpdateTenant in this story; DisableTenant on disabled returns NoOp (idempotent, not rejected)

6. **Given** a disabled tenant exists **When** an EnableTenant command is processed **Then** a TenantEnabled event is produced and the tenant status becomes Active

7. **Given** commands targeting a non-existent tenant (Update, Disable, Enable) **When** processed against null state **Then** the command is rejected with TenantNotFoundRejection identifying the missing tenant

8. **Given** the TenantAggregate Handle methods **When** tested as static pure functions with no infrastructure **Then** all Handle and Apply methods for lifecycle commands execute correctly as Tier 1 unit tests with 100% branch coverage on validation logic

9. _(Deferred to Story 2.4)_ **Given** a CreateTenant command is submitted **When** FluentValidation runs in the MediatR pipeline **Then** the command is validated for required fields — this AC is about MediatR pipeline validation, not aggregate domain logic

## Tasks / Subtasks

- [x] Task 1: Create TenantState (AC: #1-#8)
    - [x] 1.1: Create `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` — state class with ALL 9 Apply methods (4 lifecycle + 3 user-role + 2 configuration). User-role and configuration Apply methods are needed now for state completeness; their Handle methods come in Epic 3
    - [x] 1.2: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create TenantAggregate (AC: #1-#7)
    - [x] 2.1: Create `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` — aggregate extending `EventStoreAggregate<TenantState>` with 4 lifecycle Handle methods (CreateTenant, UpdateTenant, DisableTenant, EnableTenant)

- [x] Task 3: Create aggregate unit tests (AC: #8)
    - [x] 3.1: Create `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` — 12 test cases covering all branches using `ProcessAsync(CommandEnvelope, state)` pattern

- [x] Task 4: Build verification (AC: all)
    - [x] 4.1: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — zero errors, zero warnings
    - [x] 4.2: Run `dotnet test tests/Hexalith.Tenants.Server.Tests/` — all tests pass (existing GlobalAdmin + new TenantAggregate)
    - [x] 4.3: Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` — all existing tests still pass

## Dev Notes

### Developer Context

This is the **second aggregate** in Hexalith.Tenants and the core business object. It follows the pattern established by `GlobalAdministratorsAggregate` in Story 2.2, but with per-tenant identity instead of singleton. The `TenantAggregate` manages the full lifecycle of a managed tenant — each tenant has its own aggregate instance identified by the managed tenant ID (e.g., `acme-corp`).

**Key difference from GlobalAdministratorsAggregate:** GlobalAdmin uses singleton aggregate ID `global-administrators`. TenantAggregate uses the managed tenant ID as the aggregate ID (e.g., `acme-corp`). Both operate under the `system` platform tenant with domain `tenants`.

**TenantState includes ALL Apply methods now:** The state class has 9 Apply methods covering lifecycle (4), user-role (3), and configuration (2) events. The Handle methods for user-role and configuration are implemented in Epic 3 (Stories 3.1, 3.3), but the state class is created here complete. This allows the aggregate to correctly replay its full event stream.

**Disabled tenant guard:** UpdateTenant on a disabled tenant is rejected with `TenantDisabledRejection`. DisableTenant on an already-disabled tenant returns `NoOp()` (idempotent). EnableTenant is the only command exempt from the disabled guard.

### Technical Requirements

**State Class — `TenantState.cs` (exact implementation):**

```csharp
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Aggregates;

public sealed class TenantState
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }
    public Dictionary<string, TenantRole> Users { get; private set; } = new();
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
        Users[e.UserId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Users.Remove(e.UserId);
    }

    public void Apply(UserRoleChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Users[e.UserId] = e.NewRole;
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

**Critical state design notes:**

- Parameterless constructor required by `EventStoreAggregate<TState>` constraint: `where TState : class, new()`
- `private set` on all properties — Apply methods mutate, external code cannot
- `TenantStatus` defaults to `Active` (enum default = 0 = Active) — this is correct because TenantCreated.Apply sets it explicitly
- `Users` is `Dictionary<string, TenantRole>` — key = UserId, value = role
- `Configuration` is `Dictionary<string, string>` — key = config key, value = config value
- All Apply methods include `ArgumentNullException.ThrowIfNull(e)` — required by CA1062 with `TreatWarningsAsErrors = true` (learned from Story 2.2)

**Aggregate Class — `TenantAggregate.cs` (exact implementation):**

```csharp
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public class TenantAggregate : EventStoreAggregate<TenantState>
{
    public static DomainResult Handle(CreateTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state is not null
            ? DomainResult.Rejection([new TenantAlreadyExistsRejection(command.TenantId)])
            : DomainResult.Success([new TenantCreated(command.TenantId, command.Name, command.Description, DateTimeOffset.UtcNow)]);
    }

    public static DomainResult Handle(UpdateTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ => DomainResult.Success([new TenantUpdated(command.TenantId, command.Name, command.Description)]),
        };
    }

    public static DomainResult Handle(DisableTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantDisabled(command.TenantId, DateTimeOffset.UtcNow)]),
        };
    }

    public static DomainResult Handle(EnableTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Active } => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantEnabled(command.TenantId, DateTimeOffset.UtcNow)]),
        };
    }
}
```

**Critical Handle method notes:**

- All Handle methods are `public static` pure functions — discovered by reflection, no instance state
- Return type is `DomainResult` (synchronous) — no async needed
- Second parameter is `TenantState?` (nullable) — null means aggregate never created
- Three outcomes: `Success([events])`, `Rejection([rejections])`, `NoOp()`
- `ArgumentNullException.ThrowIfNull(command)` required for CA1062 (TreatWarningsAsErrors = true)
- `CreateTenant` uses `state is not null` to detect existing tenant — framework passes null for new aggregates
- `UpdateTenant` checks disabled status BEFORE producing event — disabled tenants reject all commands except EnableTenant
- `DisableTenant` on already-disabled returns `NoOp()` (idempotent) — NOT a rejection
- `EnableTenant` on already-active returns `NoOp()` (idempotent) — NOT a rejection
- `DateTimeOffset.UtcNow` for timestamp fields — consistent with architecture patterns

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type            | Project | Folder        | File                 |
| --------------- | ------- | ------------- | -------------------- |
| TenantState     | Server  | `Aggregates/` | `TenantState.cs`     |
| TenantAggregate | Server  | `Aggregates/` | `TenantAggregate.cs` |

**DO NOT:**

- Place TenantState or TenantAggregate in Contracts — aggregates and state belong in Server
- Add constructor parameters to TenantAggregate — framework uses `Activator.CreateInstance()` (parameterless)
- Add any interface beyond extending `EventStoreAggregate<TenantState>`
- Throw exceptions from Handle methods — use `DomainResult.Rejection()`
- Add validation in Apply methods — Apply trusts events
- Make Handle methods `async` — no async work needed
- Add `[JsonPropertyName]` attributes — default System.Text.Json camelCase is correct
- Add `using Hexalith.EventStore.Contracts.Events;` explicitly — globally imported via Server .csproj
- Add `using Hexalith.EventStore.Contracts.Results;` explicitly — globally imported via Server .csproj

**Naming Conventions:**

- State class: `TenantState` (not `TenantAggregateState`)
- Aggregate class: `TenantAggregate`
- Follows established pattern from Story 2.2: `{AggregateName}State`, `{AggregateName}Aggregate`

**Identity Scheme:**

- Platform tenant: `system` (in CommandEnvelope)
- Domain: `tenants`
- AggregateId: managed tenant ID (e.g., `acme-corp`) — differs from GlobalAdmin's singleton `global-administrators`

### Library & Framework Requirements

**Dependencies already available (NO new NuGet packages needed):**

- `Hexalith.EventStore.Client.Aggregates.EventStoreAggregate<T>` — via Server → EventStore.Server → EventStore.Client
- `Hexalith.EventStore.Contracts.Results.DomainResult` — globally imported via Server .csproj
- `Hexalith.EventStore.Contracts.Events.IEventPayload` / `IRejectionEvent` — globally imported via Server .csproj
- `Hexalith.Tenants.Contracts.*` — via Server → Contracts ProjectReference

**Test dependencies already available (via tests/Directory.Build.props):**

- xUnit 2.9.3 — `Xunit` namespace globally imported
- Shouldly 4.3.0 — needs `using Shouldly;`
- coverlet.collector 6.0.4

### File Structure Requirements

New source files under `src/Hexalith.Tenants.Server/`:

```
src/Hexalith.Tenants.Server/
├── Hexalith.Tenants.Server.csproj  (NO changes needed — global usings added in Story 2.2)
└── Aggregates/
    ├── GlobalAdministratorsState.cs      (existing from Story 2.2)
    ├── GlobalAdministratorsAggregate.cs  (existing from Story 2.2)
    ├── TenantState.cs                    (new)
    └── TenantAggregate.cs               (new)
```

Test files under `tests/Hexalith.Tenants.Server.Tests/`:

```
tests/Hexalith.Tenants.Server.Tests/
├── Hexalith.Tenants.Server.Tests.csproj  (NO changes needed)
├── ScaffoldingSmokeTests.cs              (keep)
└── Aggregates/
    ├── GlobalAdministratorsAggregateTests.cs  (existing from Story 2.2)
    └── TenantAggregateTests.cs               (new)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed:**

**Test file: `TenantAggregateTests.cs`**

Use `ProcessAsync(commandEnvelope, state)` pattern (same as Story 2.2). CommandEnvelope helper uses `((dynamic)command).TenantId` for per-tenant aggregate ID:

```csharp
private static CommandEnvelope CreateCommand<T>(T command) where T : notnull
    => new(
        "system",
        "tenants",
        ((dynamic)command).TenantId,
        typeof(T).Name,
        JsonSerializer.SerializeToUtf8Bytes(command),
        Guid.NewGuid().ToString(),
        null,
        "test-user",
        null);
```

**Test cases (12 tests covering all branches):**

| #   | Given                                            | When                                         | Then                                        | AC            |
| --- | ------------------------------------------------ | -------------------------------------------- | ------------------------------------------- | ------------- |
| 1   | No prior state (null)                            | CreateTenant("acme", "Acme Corp", "Test")    | Success: TenantCreated with matching fields | #1            |
| 2   | Active tenant exists                             | CreateTenant("acme", ...)                    | Rejection: TenantAlreadyExistsRejection     | #2            |
| 3   | Active tenant exists                             | UpdateTenant("acme", "New Name", "New Desc") | Success: TenantUpdated with new values      | #3            |
| 4   | No prior state (null)                            | UpdateTenant("acme", ...)                    | Rejection: TenantNotFoundRejection          | #7            |
| 5   | Disabled tenant exists                           | UpdateTenant("acme", ...)                    | Rejection: TenantDisabledRejection          | #5            |
| 6   | Active tenant exists                             | DisableTenant("acme")                        | Success: TenantDisabled with timestamp      | #4            |
| 7   | No prior state (null)                            | DisableTenant("acme")                        | Rejection: TenantNotFoundRejection          | #7            |
| 8   | Disabled tenant exists                           | DisableTenant("acme")                        | NoOp (already disabled)                     | #4 idempotent |
| 9   | Disabled tenant exists                           | EnableTenant("acme")                         | Success: TenantEnabled with timestamp       | #6            |
| 10  | No prior state (null)                            | EnableTenant("acme")                         | Rejection: TenantNotFoundRejection          | #7            |
| 11  | Active tenant exists                             | EnableTenant("acme")                         | NoOp (already active)                       | #6 idempotent |
| 12  | State replay: Create → Update → Disable → Enable | Verify state transitions                     | Correct properties after each step          | #8            |

**How to build state for tests:** Create a `TenantState` instance and call Apply methods to set up "Given" state:

```csharp
// Active tenant
var state = new TenantState();
state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
// state: TenantId="acme", Name="Acme Corp", Status=Active

// Disabled tenant
state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));
// state: Status=Disabled
```

**Required usings in test file:**

```csharp
using System.Text.Json;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;
using Shouldly;
```

**Assertion patterns (consistent with Story 2.2):**

```csharp
// Success — verify event type and fields
result.IsSuccess.ShouldBeTrue();
result.Events.Count.ShouldBe(1);
var evt = result.Events[0].ShouldBeOfType<TenantCreated>();
((TenantCreated)evt).TenantId.ShouldBe("acme");

// Rejection — verify rejection type
result.IsRejection.ShouldBeTrue();
result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();

// NoOp — verify no events
result.IsNoOp.ShouldBeTrue();
result.Events.Count.ShouldBe(0);

// Timestamp — verify recent, not exact value (Handle uses DateTimeOffset.UtcNow)
((TenantCreated)evt).CreatedAt.ShouldBeInRange(
    DateTimeOffset.UtcNow.AddSeconds(-5),
    DateTimeOffset.UtcNow.AddSeconds(1));
```

**State replay test (Test #12):** Process Create → Update → Disable → Enable via ProcessAsync, applying events to state after each step. Verify:

- After Create: TenantId="acme", Name="Acme Corp", Status=Active, CreatedAt non-default
- After Update: Name="Updated Name", Description="Updated Desc"
- After Disable: Status=Disabled
- After Enable: Status=Active

### Previous Story Intelligence

**Story 2.2 (done) — GlobalAdministratorsAggregate:**

- Established aggregate pattern: `EventStoreAggregate<TState>`, static Handle methods, void Apply methods
- `ArgumentNullException.ThrowIfNull()` required in ALL Handle and Apply methods (CA1062 + TreatWarningsAsErrors)
- DAPR packages already updated to 1.17.3, `Microsoft.Extensions.Configuration.Binder` bumped to 10.0.3
- Server .csproj already has global usings for `Hexalith.EventStore.Contracts.Events` and `Hexalith.EventStore.Contracts.Results`
- Test pattern: `ProcessAsync(CommandEnvelope, state)` with state built via direct Apply calls
- No new NuGet packages or .csproj changes needed — all infrastructure from Story 2.2 is reusable

**Story 2.2 Learnings:**

- CA1062 null validation errors — resolved with `ArgumentNullException.ThrowIfNull()` on ALL method parameters that are reference types
- DAPR 1.17.3 transitive dependency issue already resolved
- `using Hexalith.Tenants.Contracts.Events;` is a DIFFERENT namespace from the globally imported `Hexalith.EventStore.Contracts.Events;` — both are needed, only the EventStore one is global

**Story 2.1 Learnings:**

- `TreatWarningsAsErrors = true` — all warnings are build failures, be thorough
- `.editorconfig` enforces: file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indent

### Critical Anti-Patterns (DO NOT)

- **DO NOT** call Handle methods directly in tests — use `aggregate.ProcessAsync(commandEnvelope, state)` to validate framework reflection dispatch
- **DO NOT** add constructor parameters to the aggregate — framework uses `Activator.CreateInstance()`
- **DO NOT** throw exceptions from Handle methods — use `DomainResult.Rejection()`
- **DO NOT** add validation in Apply methods — Apply trusts events
- **DO NOT** create FluentValidation validators — those belong in Story 2.4 (MediatR pipeline)
- **DO NOT** modify Server .csproj — global usings already added in Story 2.2
- **DO NOT** create new contracts — all commands, events, and rejections exist from Stories 2.1/2.2
- **DO NOT** assert exact `DateTimeOffset` values in tests — assert non-default only (timestamps use `DateTimeOffset.UtcNow`)
- **DO NOT** use `Members` instead of `Users` in TenantState — the property is named `Users` (architecture uses `Members` in one example, but the blueprint and events use `Users`)

### Project Structure Notes

- Server project already has `Aggregates/` folder (from Story 2.2) — add TenantState.cs and TenantAggregate.cs alongside existing files
- Server.Tests project already has `Aggregates/` folder — add TenantAggregateTests.cs alongside existing file
- No new projects, no new folders needed
- No .csproj modifications needed — all dependencies and global usings already configured

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.3] — Story definition, ACs, implementation blueprint
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — Handle/Apply patterns, three-outcome model, disabled tenant guard
- [Source: _bmad-output/planning-artifacts/architecture.md#Aggregate Boundaries] — TenantAggregate scope (lifecycle + user-role + config)
- [Source: _bmad-output/planning-artifacts/architecture.md#D10 Aggregate Testing Blueprint] — ProcessAsync + CommandEnvelope test pattern with `((dynamic)command).TenantId`
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs] — Established aggregate pattern with ArgumentNullException.ThrowIfNull
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs] — Established state pattern with ArgumentNullException.ThrowIfNull in Apply methods
- [Source: tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs] — Established test pattern with ProcessAsync
- [Source: _bmad-output/implementation-artifacts/2-2-global-administrator-aggregate.md] — Previous story patterns, CA1062 learning, DAPR version fix
- [Source: _bmad-output/implementation-artifacts/2-1-tenant-domain-contracts.md] — Contract definitions, naming conventions, TreatWarningsAsErrors learning

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

None — clean implementation, no debugging needed.

### Completion Notes List

- Created `TenantState.cs` with 9 Apply methods (4 lifecycle + 3 user-role + 2 configuration) matching story spec exactly
- Created `TenantAggregate.cs` extending `EventStoreAggregate<TenantState>` with 4 static Handle methods for lifecycle commands
- Created `TenantAggregateTests.cs` with the requested 12 lifecycle test cases covering all branches: Create (2), Update (3), Disable (3), Enable (3), State replay (1)
- All Handle methods follow the established pattern: static, pure functions, `DomainResult` return, nullable `TenantState?` parameter
- Disabled tenant guard: UpdateTenant rejected with `TenantDisabledRejection`; DisableTenant returns NoOp (idempotent); EnableTenant exempt
- Build: 0 warnings, 0 errors (Release configuration)
- Tests: 24 Server tests passed (10 existing + 12 story tests + 1 review follow-up state test + 1 scaffolding), 25 Contracts tests passed (no regressions)
- No .csproj changes needed — all dependencies and global usings from Story 2.2 reused
- Senior Developer review follow-up: verified that additional git changes in the working tree belong to earlier stories/repo updates, and added a direct state-mutation test that brought focused `TenantState` and `TenantAggregate` coverage to 100% line / 100% branch

### Change Log

- 2026-03-15: Story 2.3 implemented — TenantState, TenantAggregate, and 12 unit tests for tenant lifecycle management
- 2026-03-15: Senior Developer AI review completed — approved after fixes.
- 2026-03-15: Review follow-up applied — clarified cross-story git discrepancies and added direct coverage for the remaining `TenantState` Apply methods.

### File List

- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` (new)
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` (new)
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` (new)

### Senior Developer Review (AI)

**Reviewer:** Jerome
**Date:** 2026-03-15
**Outcome:** Approved after fixes

#### Summary

The tenant lifecycle aggregate implementation satisfies the story acceptance criteria and the claimed build/test verification passes locally. The review follow-up clarified that the extra git changes observed during review were attributable to earlier story work already documented elsewhere, and a targeted state-mutation test was added to reduce blind spots around the non-lifecycle `TenantState` Apply methods.

#### Findings

1. **RESOLVED — Cross-story git discrepancies clarified**
   The additional modified files seen in the working tree during review were traced to earlier story/repository updates, notably Story 2.2 documentation and package/global-using changes, rather than to undocumented Story 2.3 implementation scope.

2. **RESOLVED — Direct coverage added for remaining state mutations**
   `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` now includes a direct test for `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved` state mutations.

#### What was verified

- ACs 1-8 are implemented by `TenantAggregate`, `TenantState`, and `TenantAggregateTests`.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` passed.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build` passed.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build` passed.
- Coverage collection confirmed `TenantAggregate.cs` and `TenantState.cs` at 100% line / 100% branch in the focused server test run.
