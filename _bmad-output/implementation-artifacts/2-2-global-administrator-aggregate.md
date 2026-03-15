# Story 2.2: Global Administrator Aggregate

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want to bootstrap the first global admin on initial deployment and manage global administrator designations,
So that the system has authorized actors who can create and manage tenants.

## Acceptance Criteria

1. **Given** no global administrators exist in the event store **When** a BootstrapGlobalAdmin command is processed with a valid user ID **Then** a GlobalAdministratorSet event is produced with the specified user ID

2. **Given** a global administrator already exists **When** a BootstrapGlobalAdmin command is processed **Then** the command is rejected with GlobalAdminAlreadyBootstrappedRejection

3. **Given** an existing global administrator **When** a SetGlobalAdministrator command is processed with a new user ID **Then** a GlobalAdministratorSet event is produced

4. **Given** an existing global administrator **When** a RemoveGlobalAdministrator command is processed for a designated admin **Then** a GlobalAdministratorRemoved event is produced

5. **Given** only one global administrator exists **When** a RemoveGlobalAdministrator command attempts to remove the last global administrator **Then** the command is rejected with LastGlobalAdministratorRejection

6. **Given** the GlobalAdministratorsAggregate Handle methods **When** tested as static pure functions with no infrastructure **Then** all Handle and Apply methods execute correctly as Tier 1 unit tests

7. **Given** the GlobalAdministratorsState class **When** Apply methods are called with each event type **Then** state is correctly mutated (administrators set added/removed, Bootstrapped flag set)

## Tasks / Subtasks

- [x] Task 0: Create missing rejection event type in Contracts (AC: #5)
    - [x] 0.1: Create `src/Hexalith.Tenants.Contracts/Events/Rejections/LastGlobalAdministratorRejection.cs` — `record LastGlobalAdministratorRejection(string TenantId, string UserId) : IRejectionEvent`
    - [x] 0.2: Verify existing tests still pass (`dotnet test tests/Hexalith.Tenants.Contracts.Tests/`) — the reflection-based serialization tests and rejection-event checks auto-discover the new rejection event, and `NamingConventionTests` was updated in this changeset to tighten success-event suffix validation

- [x] Task 1: Update DAPR package versions (prerequisite from sprint change proposal)
    - [x] 1.1: In `Directory.Packages.props`, update all 4 DAPR packages from `1.16.1` to `1.17.3` (`Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors`, `Dapr.Actors.AspNetCore`)
    - [x] 1.2: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Add global usings to Server .csproj (AC: all aggregate files)
    - [x] 2.1: Add `<Using Include="Hexalith.EventStore.Contracts.Events" />` and `<Using Include="Hexalith.EventStore.Contracts.Results" />` to `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` — avoids repetitive `using` directives in aggregate files

- [x] Task 3: Create GlobalAdministratorsState (AC: #7)
    - [x] 3.1: Create `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs` — state class with 2 Apply methods (see Dev Notes for exact implementation)

- [x] Task 4: Create GlobalAdministratorsAggregate (AC: #1, #2, #3, #4, #5)
    - [x] 4.1: Create `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs` — aggregate extending `EventStoreAggregate<GlobalAdministratorsState>` with 3 Handle methods (see Dev Notes for exact implementation)

- [x] Task 5: Create aggregate unit tests (AC: #6, #7)
    - [x] 5.1: Create `tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs` — Tier 1 tests using `ProcessAsync(commandEnvelope, state)` pattern (see Testing Requirements)

- [x] Task 6: Build verification (AC: all)
    - [x] 6.1: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — zero errors, zero warnings
    - [x] 6.2: Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` — all existing tests still pass (including new rejection event auto-discovered)
    - [x] 6.3: Run `dotnet test tests/Hexalith.Tenants.Server.Tests/` — all new aggregate tests pass

## Dev Notes

### Developer Context

This is the **first aggregate implementation** in Hexalith.Tenants. It establishes the pattern for all future aggregates (TenantAggregate in Story 2.3). The GlobalAdministratorsAggregate is a **singleton** aggregate — there is exactly one instance identified by `global-administrators` under the `system` platform tenant.

**Key mental model:** The `EventStoreAggregate<TState>` base class uses reflection to discover Handle and Apply methods automatically. You declare `public static DomainResult Handle(XxxCommand, TState?)` and the framework wires everything. No manual registration, no interface implementation on the aggregate beyond extending the base class.

**GlobalAdmin scope:** GlobalAdmin commands (`BootstrapGlobalAdmin`, `SetGlobalAdministrator`, `RemoveGlobalAdministrator`) operate on the singleton aggregate `system:tenants:global-administrators`. Their events carry `TenantId` set to `"system"` at runtime for consistency with the architecture rule that ALL events include `TenantId`.

### Technical Requirements

**State Class — `GlobalAdministratorsState.cs` (exact implementation):**

```csharp
namespace Hexalith.Tenants.Server.Aggregates;

public sealed class GlobalAdministratorsState
{
    public HashSet<string> Administrators { get; private set; } = new();
    public bool Bootstrapped { get; private set; }

    public void Apply(GlobalAdministratorSet e)
    {
        Administrators.Add(e.UserId);
        Bootstrapped = true;
    }

    public void Apply(GlobalAdministratorRemoved e)
    {
        Administrators.Remove(e.UserId);
    }
}
```

**Critical state design notes:**

- `Bootstrapped` tracks whether bootstrap has occurred — once `true`, `BootstrapGlobalAdmin` is rejected
- `Administrators` is a `HashSet<string>` — `Add` is idempotent, `Contains` is O(1)
- State class must have a **parameterless constructor** (required by `EventStoreAggregate<TState>` constraint: `where TState : class, new()`)
- Apply methods are `public void` with single event parameter — discovered by reflection via method name `"Apply"`
- State uses `private set` on properties — Apply methods mutate state, but external code cannot
- Events from Contracts are available via global using from Task 2 (`Hexalith.EventStore.Contracts.Events` makes `IEventPayload` available, and `GlobalAdministratorSet`/`GlobalAdministratorRemoved` are in `Hexalith.Tenants.Contracts.Events` namespace which needs explicit `using`)

**Aggregate Class — `GlobalAdministratorsAggregate.cs` (exact implementation):**

```csharp
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public class GlobalAdministratorsAggregate : EventStoreAggregate<GlobalAdministratorsState>
{
    public static DomainResult Handle(BootstrapGlobalAdmin command, GlobalAdministratorsState? state)
        => state?.Bootstrapped == true
            ? DomainResult.Rejection([new GlobalAdminAlreadyBootstrappedRejection("system")])
            : DomainResult.Success([new GlobalAdministratorSet("system", command.UserId)]);

    public static DomainResult Handle(SetGlobalAdministrator command, GlobalAdministratorsState? state)
        => state is not null && state.Administrators.Contains(command.UserId)
            ? DomainResult.NoOp()
            : DomainResult.Success([new GlobalAdministratorSet("system", command.UserId)]);

    public static DomainResult Handle(RemoveGlobalAdministrator command, GlobalAdministratorsState? state)
        => state switch
        {
            null => DomainResult.NoOp(),
            _ when !state.Administrators.Contains(command.UserId) => DomainResult.NoOp(),
            _ when state.Administrators.Count == 1 => DomainResult.Rejection([new LastGlobalAdministratorRejection("system", command.UserId)]),
            _ => DomainResult.Success([new GlobalAdministratorRemoved("system", command.UserId)]),
        };
}
```

**Critical Handle method notes:**

- All Handle methods are `public static` — the framework invokes them via reflection and supports both static and instance methods
- Return type is `DomainResult` (synchronous) — async `Task<DomainResult>` is also supported but not needed here
- Second parameter is `GlobalAdministratorsState?` (nullable) — null means no prior state (first command ever)
- Three outcomes: `DomainResult.Success([events])`, `DomainResult.Rejection([rejections])`, `DomainResult.NoOp()`
- `TenantId` on events is `"system"` — GlobalAdmin events belong to the platform tenant, not a managed tenant
- `BootstrapGlobalAdmin` reuses `GlobalAdministratorSet` event — no separate `GlobalAdminBootstrapped` event type
- `SetGlobalAdministrator` is idempotent — if user already in set, return `NoOp()`. Note: aggregate does NOT enforce authorization (e.g., "only existing admins can add admins") — authorization is enforced at the CommandApi layer via JWT/MediatR pipeline (Story 2.4). The aggregate only enforces domain invariants
- `RemoveGlobalAdministrator` with null state → `NoOp()` (nothing to remove); user not in set → `NoOp()`; last admin → rejection; else → success

**New Rejection Event — `LastGlobalAdministratorRejection.cs` (in Contracts):**

```csharp
namespace Hexalith.Tenants.Contracts.Events.Rejections;

public record LastGlobalAdministratorRejection(string TenantId, string UserId) : IRejectionEvent;
```

This rejection type was NOT defined in Story 2.1 (the contracts story defined 8 rejection types, but this one was missed). It is needed for AC #5 (last-admin protection). The naming follows `{Target}{Reason}Rejection` pattern. It will be auto-discovered by the existing `EventSerializationTests` and `NamingConventionTests` via reflection.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type                             | Project   | Folder               | File                                  |
| -------------------------------- | --------- | -------------------- | ------------------------------------- |
| GlobalAdministratorsState        | Server    | `Aggregates/`        | `GlobalAdministratorsState.cs`        |
| GlobalAdministratorsAggregate    | Server    | `Aggregates/`        | `GlobalAdministratorsAggregate.cs`    |
| LastGlobalAdministratorRejection | Contracts | `Events/Rejections/` | `LastGlobalAdministratorRejection.cs` |

**DO NOT:**

- Place aggregate or state in Contracts — aggregates belong in Server
- Add any interface to the aggregate beyond extending `EventStoreAggregate<GlobalAdministratorsState>`
- Add constructor parameters to the aggregate — framework creates via `Activator.CreateInstance()` (needs parameterless constructor)
- Throw exceptions from Handle methods — use `DomainResult.Rejection()` instead
- Add validation logic to Apply methods — Apply trusts events (validated in Handle)
- Hardcode `"system"` in the event record definitions — `TenantId` is a constructor parameter set at runtime by Handle methods
- Add `using Hexalith.EventStore.Contracts.Events;` in aggregate files — globally imported via Task 2. Note: `using Hexalith.Tenants.Contracts.Events;` is a DIFFERENT namespace and IS still required as an explicit using (provides `GlobalAdministratorSet`, `GlobalAdministratorRemoved`)

**Naming Conventions:**

- State class: `{AggregateName}State` (e.g., `GlobalAdministratorsState`)
- Aggregate class: `{AggregateName}Aggregate` (e.g., `GlobalAdministratorsAggregate`)
- Note: The aggregate/state use `GlobalAdministrators` (plural) matching the aggregate ID `global-administrators`

### Library & Framework Requirements

**Dependencies already available (no new NuGet packages needed):**

- `Hexalith.EventStore.Client.Aggregates.EventStoreAggregate<T>` — via Server → EventStore.Server → EventStore.Client transitive reference
- `Hexalith.EventStore.Contracts.Results.DomainResult` — via Server → EventStore.Server → EventStore.Contracts transitive reference
- `Hexalith.EventStore.Contracts.Events.IEventPayload` / `IRejectionEvent` — same transitive path
- `Hexalith.Tenants.Contracts.Commands.*` — via Server → Contracts ProjectReference
- `Hexalith.Tenants.Contracts.Events.*` — same path

**DAPR version prerequisite (Task 1):**

- `Directory.Packages.props` currently has DAPR at `1.16.1`
- EventStore submodule uses `1.17.3`
- Update before building to avoid version conflicts: `Dapr.Client`, `Dapr.AspNetCore`, `Dapr.Actors`, `Dapr.Actors.AspNetCore` → `1.17.3`

**Test dependencies already available (via tests/Directory.Build.props):**

- xUnit 2.9.3 — `Xunit` namespace globally imported
- Shouldly 4.3.0 — needs `using Shouldly;`
- coverlet.collector 6.0.4

### File Structure Requirements

New source files under `src/Hexalith.Tenants.Server/`:

```
src/Hexalith.Tenants.Server/
├── Hexalith.Tenants.Server.csproj  (modify: add global usings)
└── Aggregates/
    ├── GlobalAdministratorsState.cs  (new)
    └── GlobalAdministratorsAggregate.cs  (new)
```

New contract file under `src/Hexalith.Tenants.Contracts/`:

```
src/Hexalith.Tenants.Contracts/
└── Events/
    └── Rejections/
        └── LastGlobalAdministratorRejection.cs  (new)
```

Test files under `tests/Hexalith.Tenants.Server.Tests/`:

```
tests/Hexalith.Tenants.Server.Tests/
├── Hexalith.Tenants.Server.Tests.csproj  (no changes needed)
├── ScaffoldingSmokeTests.cs  (keep)
└── Aggregates/
    └── GlobalAdministratorsAggregateTests.cs  (new)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed:**

**Test file: `GlobalAdministratorsAggregateTests.cs`**

Use the `ProcessAsync(commandEnvelope, state)` pattern from Architecture §D10. This is critical — it tests through the framework's reflection-based dispatch, not by calling Handle methods directly.

**CommandEnvelope helper (put in test class or shared helper):**

```csharp
private static CommandEnvelope CreateCommand<T>(T command) where T : notnull
    => new(
        "system",
        "tenants",
        "global-administrators",
        typeof(T).Name,
        JsonSerializer.SerializeToUtf8Bytes(command),
        Guid.NewGuid().ToString(),
        null,
        "test-user",
        null);
```

All GlobalAdmin commands target the singleton aggregate `global-administrators` — no need for `((dynamic)command).TenantId` dispatch like TenantAggregate tests will use.

**Test cases (Given/When/Then, map to ACs):**

| #   | Given                           | When                                     | Then                                                            | AC               |
| --- | ------------------------------- | ---------------------------------------- | --------------------------------------------------------------- | ---------------- |
| 1   | No prior state (null)           | BootstrapGlobalAdmin("admin-1")          | Success: GlobalAdministratorSet("system", "admin-1")            | #1               |
| 2   | Bootstrapped state with admin-1 | BootstrapGlobalAdmin("admin-2")          | Rejection: GlobalAdminAlreadyBootstrappedRejection              | #2               |
| 3   | State with admin-1              | SetGlobalAdministrator("admin-2")        | Success: GlobalAdministratorSet("system", "admin-2")            | #3               |
| 4   | State with admin-1              | SetGlobalAdministrator("admin-1")        | NoOp (already in set)                                           | #3 (idempotency) |
| 5   | State with admin-1, admin-2     | RemoveGlobalAdministrator("admin-1")     | Success: GlobalAdministratorRemoved("system", "admin-1")        | #4               |
| 6   | State with admin-1 only         | RemoveGlobalAdministrator("admin-1")     | Rejection: LastGlobalAdministratorRejection                     | #5               |
| 7   | State with admin-1              | RemoveGlobalAdministrator("nonexistent") | NoOp (user not in set)                                          | #4 (idempotency) |
| 8   | No prior state (null)           | RemoveGlobalAdministrator("any")         | NoOp (nothing to remove)                                        | #4 (edge)        |
| 9   | No prior state (null)           | SetGlobalAdministrator("admin-1")        | Success: GlobalAdministratorSet (no bootstrap required for Set) | #3 (edge)        |

**State replay test (AC: #7):**

- Create aggregate, process BootstrapGlobalAdmin → verify state has 1 admin, Bootstrapped=true
- Process SetGlobalAdministrator with second user → verify state has 2 admins
- Process RemoveGlobalAdministrator on first user → verify state has 1 admin, Bootstrapped still true

**How to build state for tests:** Create a `GlobalAdministratorsState` instance and call Apply methods directly to set up the "Given" state, then pass it to `ProcessAsync`:

```csharp
var state = new GlobalAdministratorsState();
state.Apply(new GlobalAdministratorSet("system", "admin-1"));
// state now has Administrators = {"admin-1"}, Bootstrapped = true

DomainResult result = await aggregate.ProcessAsync(
    CreateCommand(new RemoveGlobalAdministrator("admin-1")),
    currentState: state);
```

**Required usings in test file:**

```csharp
using System.Text.Json;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;
using Shouldly;
```

**Assertion patterns:**

```csharp
// Success assertion
result.IsSuccess.ShouldBeTrue();
result.Events.Count.ShouldBe(1);
var evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
evt.TenantId.ShouldBe("system");
evt.UserId.ShouldBe("admin-1");

// Rejection assertion
result.IsRejection.ShouldBeTrue();
result.Events[0].ShouldBeOfType<GlobalAdminAlreadyBootstrappedRejection>();

// NoOp assertion
result.IsNoOp.ShouldBeTrue();
result.Events.Count.ShouldBe(0);
```

### Previous Story Intelligence

**Story 2.1 (done) — Tenant Domain Contracts:**

- All 12 commands, 11 events, 8 rejection events, 2 enums, 1 identity class created and tested
- Global using in Contracts .csproj for `Hexalith.EventStore.Contracts.Events` — same pattern should be replicated in Server .csproj
- Serialization round-trip test uses reflection to auto-discover ALL `IEventPayload` types — new `LastGlobalAdministratorRejection` will be covered automatically
- Naming convention tests are whitelist-based — `LastGlobalAdministratorRejection` ends with `Rejection` so it passes the rejection suffix test
- The `NamingConventionTests` also verifies all event types have `TenantId` property of type `string` — `LastGlobalAdministratorRejection` includes `TenantId` as first parameter so it passes
- `.editorconfig` enforces: file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indent
- `TreatWarningsAsErrors = true` — all warnings are build failures

**Story 2.1 Learnings:**

- Empty library projects compile without placeholder files — Server project is currently empty (only obj/ files)
- `using` for intra-project namespaces (e.g., `Hexalith.Tenants.Contracts.Enums`) still needed even with global usings — global usings only cover external namespaces added to .csproj

### Critical Anti-Patterns (DO NOT)

- **DO NOT** call Handle methods directly in tests — use `aggregate.ProcessAsync(commandEnvelope, state)` to test through the framework's reflection dispatch. This validates that the framework discovers your Handle methods correctly
- **DO NOT** add `CorrelationId`, `UserId`, or `Timestamp` to command records — infrastructure metadata is in `CommandEnvelope`
- **DO NOT** throw exceptions from Handle methods — use `DomainResult.Rejection()`
- **DO NOT** add validation in Apply methods — trust the event
- **DO NOT** make Handle methods `async` unless performing actual async work (they don't here — use sync `DomainResult` return)
- **DO NOT** use `class` instead of `record` for the rejection event — contracts use records
- **DO NOT** add `[JsonPropertyName]` attributes — default System.Text.Json camelCase is correct
- **DO NOT** create a separate `GlobalAdminBootstrapped` event — `BootstrapGlobalAdmin` reuses `GlobalAdministratorSet`
- **DO NOT** put `GlobalAdministratorsState` in Contracts — state classes belong in Server (not exposed to consuming services)

### Project Structure Notes

- Server project is currently an empty shell (no .cs source files) — this story creates the first source files in `Aggregates/`
- Server.csproj already has correct ProjectReferences: Contracts + EventStore.Server
- Server.Tests.csproj already references Server, Contracts, Testing, CommandApi, and Sample — no changes needed
- `Aggregates/` folder needs to be created under `src/Hexalith.Tenants.Server/` and `tests/Hexalith.Tenants.Server.Tests/`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.2] — Story definition, ACs, implementation blueprint
- [Source: _bmad-output/planning-artifacts/architecture.md#Aggregate Boundaries] — GlobalAdministratorAggregate is singleton, platform-level
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — Handle/Apply implementation patterns, three-outcome model
- [Source: _bmad-output/planning-artifacts/architecture.md#D10 Testing Blueprint] — ProcessAsync + CommandEnvelope test pattern
- [Source: _bmad-output/planning-artifacts/architecture.md#Error Handling] — DomainResult.Rejection() pattern, no domain exceptions
- [Source: _bmad-output/planning-artifacts/architecture.md#Identity Mapping] — system:tenants:global-administrators identity
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs] — Base class with reflection-based Handle/Apply discovery
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs] — Three-outcome result: Success, Rejection, NoOp
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs] — Command envelope with identity validation
- [Source: _bmad-output/implementation-artifacts/2-1-tenant-domain-contracts.md] — Previous story patterns and learnings
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-03-15-research.md] — DAPR version alignment (1.16.1 → 1.17.3) prerequisite

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- CA1062 null validation errors on Handle/Apply methods — resolved by adding `ArgumentNullException.ThrowIfNull()` calls (required because `TreatWarningsAsErrors = true`)
- DAPR 1.17.3 upgrade introduced transitive dependency on `Microsoft.Extensions.Configuration.Binder >= 10.0.3` — resolved by updating from `10.0.0` to `10.0.3` in `Directory.Packages.props`

### Completion Notes List

- ✅ Task 0: Created `LastGlobalAdministratorRejection` rejection event — serialization and rejection-event reflection tests covered it automatically, while `NamingConventionTests` was also updated in this changeset to tighten success-event suffix validation (25/25 pass)
- ✅ Task 1: Updated DAPR packages 1.16.1 → 1.17.3, also bumped `Microsoft.Extensions.Configuration.Binder` 10.0.0 → 10.0.3 to resolve transitive dependency conflict
- ✅ Task 2: Added global usings for `Hexalith.EventStore.Contracts.Events` and `Hexalith.EventStore.Contracts.Results` to Server .csproj
- ✅ Task 3: Created `GlobalAdministratorsState` with 2 Apply methods (`GlobalAdministratorSet`, `GlobalAdministratorRemoved`), parameterless constructor, `HashSet<string>` for administrators, `Bootstrapped` flag
- ✅ Task 4: Created `GlobalAdministratorsAggregate` extending `EventStoreAggregate<GlobalAdministratorsState>` with 3 static Handle methods for Bootstrap, Set, and Remove commands. Uses `DomainResult.Success`, `DomainResult.Rejection`, and `DomainResult.NoOp` outcomes
- ✅ Task 5: Created 10 aggregate unit tests using `ProcessAsync(CommandEnvelope, state)` pattern — covers all 7 ACs including idempotency edge cases and state replay verification
- ✅ Task 6: Full Release build (0 warnings, 0 errors), Contracts tests (25/25), Server tests (11/11)

### Change Log

- 2026-03-15: Implemented GlobalAdministratorsAggregate (Story 2.2) — first aggregate in Hexalith.Tenants establishing the pattern for future aggregates. Updated DAPR SDK 1.16.1 → 1.17.3.
- 2026-03-15: Senior Developer AI review completed — changes requested due to undocumented test-file modification and inaccurate completion-note wording around auto-discovered tests.
- 2026-03-15: Review follow-up applied — story record corrected to include the contracts naming test change and clarify what test coverage was auto-discovered versus explicitly updated.

### File List

- `src/Hexalith.Tenants.Contracts/Events/Rejections/LastGlobalAdministratorRejection.cs` (new)
- `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` (modified — added global usings)
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs` (new)
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs` (new)
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs` (new)
- `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` (modified — tightened success-event suffix validation)
- `Directory.Packages.props` (modified — DAPR 1.16.1 → 1.17.3, Configuration.Binder 10.0.0 → 10.0.3)

### Senior Developer Review (AI)

**Reviewer:** Jerome
**Date:** 2026-03-15
**Outcome:** Approved after fixes

#### Summary

The aggregate implementation satisfies the story's functional acceptance criteria and the claimed build/test verification passes locally. The documentation gaps found during review have now been corrected, so the review is approved.

#### Findings

1. **RESOLVED — Undocumented changed test file**
   `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` has been added to the story `File List`.

2. **RESOLVED — Completion note wording corrected**
   The completion notes and task wording now distinguish between the auto-discovered rejection-event coverage and the explicit update to `NamingConventionTests` in this changeset.

#### What was verified

- ACs 1-7 are implemented by `GlobalAdministratorsAggregate`, `GlobalAdministratorsState`, and `GlobalAdministratorsAggregateTests`.
- `dotnet build Hexalith.Tenants.slnx --configuration Release` passed.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --configuration Release --no-build` passed (25/25).
- `dotnet test tests/Hexalith.Tenants.Server.Tests/ --configuration Release --no-build` passed (11/11).
