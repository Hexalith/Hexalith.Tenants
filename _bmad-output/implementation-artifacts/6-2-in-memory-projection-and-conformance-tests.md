# Story 6.2: In-Memory Projection & Conformance Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want an in-memory projection for query testing and a conformance test suite proving production-test parity,
So that I can test query scenarios locally and trust that test behavior matches production behavior.

## Acceptance Criteria

1. **Given** the `InMemoryTenantProjection` exists in the Testing package
   **When** events produced by `InMemoryTenantService` are applied to the projection
   **Then** the projection maintains queryable tenant state (tenant details, user lists, configuration) in memory

2. **Given** the `InMemoryTenantProjection`
   **When** a developer queries for tenants, users, or configuration in a test
   **Then** results are returned from the in-memory projection without DAPR state store dependency

3. **Given** the conformance test suite in Testing.Tests
   **When** a reflection-based scan discovers all command types in the Contracts assembly
   **Then** every command type is automatically included in the conformance test — no manual registration required

4. **Given** the conformance test suite
   **When** an identical command sequence is executed against the real `TenantAggregate` and the `InMemoryTenantService`
   **Then** both produce identical event sequences (same event types, same field values) for every command type

5. **Given** the conformance test suite
   **When** a new command type is added to the Contracts assembly
   **Then** the reflection-based discovery automatically includes it in the next test run without any test code changes

6. **Given** the conformance test suite fails
   **When** the CI pipeline runs
   **Then** the build is marked as failed — this is a release blocker indicating production and test execution paths have diverged

## Tasks / Subtasks

- [x] Task 1: Update Testing.Tests csproj references (unblocks Tasks 3 and 4)
    - [x] 1.1: Add Server project reference to `Hexalith.Tenants.Testing.Tests.csproj` (if not already present from Story 6.1) — conformance tests need direct access to `TenantAggregate`, `GlobalAdministratorsAggregate`, `TenantState`, and `GlobalAdministratorsState`
    - [x] 1.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create `InMemoryTenantProjection` in Testing project (AC: #1, #2)
    - [x] 2.1: Create `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
    - [x] 2.2: Implement per-tenant read model storage with `Dictionary<string, TenantReadModel>` and singleton `GlobalAdministratorReadModel`
    - [x] 2.3: Implement `void Apply(IEventPayload eventPayload)` — dispatches to correct read model's `Apply()` overload via pattern matching switch (mirrors the dispatch pattern from `InMemoryTenantService`)
    - [x] 2.4: Implement query methods:
        - `TenantReadModel? GetTenant(string tenantId)` — returns read model for tenant or null
        - `IReadOnlyList<TenantReadModel> GetAllTenants()` — returns all projected tenants
        - `GlobalAdministratorReadModel GetGlobalAdministrators()` — returns global admin read model (never null — initialize empty)
    - [x] 2.5: Implement `void ApplyEvents(IEnumerable<IEventPayload> events)` — convenience method that applies multiple events in sequence
    - [x] 2.6: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create InMemoryTenantProjection unit tests (AC: #1, #2)
    - [x] 3.1: Create `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs`
    - [x] 3.2: Test: Apply TenantCreated — projection stores tenant with correct fields
    - [x] 3.3: Test: Apply TenantUpdated — projection updates name and description
    - [x] 3.4: Test: Apply TenantDisabled/TenantEnabled — projection tracks status
    - [x] 3.5: Test: Apply UserAddedToTenant — projection adds member with role
    - [x] 3.6: Test: Apply UserRemovedFromTenant — projection removes member
    - [x] 3.7: Test: Apply UserRoleChanged — projection updates member role
    - [x] 3.8: Test: Apply TenantConfigurationSet/TenantConfigurationRemoved — projection manages config
    - [x] 3.9: Test: Apply GlobalAdministratorSet/GlobalAdministratorRemoved — projection tracks admins
    - [x] 3.10: Test: GetAllTenants returns all projected tenants
    - [x] 3.11: Test: Cross-tenant isolation — tenant A data never appears in tenant B query
    - [x] 3.12: Test: End-to-end — InMemoryTenantService command → events → InMemoryTenantProjection query
    - [x] 3.13: Verify all tests pass: `dotnet test tests/Hexalith.Tenants.Testing.Tests/`

- [x] Task 4: Create conformance test suite in Testing.Tests (AC: #3, #4, #5, #6)
    - [x] 4.1: Create `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
    - [x] 4.2: Implement reflection-based command type discovery — scan `Hexalith.Tenants.Contracts` assembly for all non-abstract classes in `Hexalith.Tenants.Contracts.Commands` namespace (the count assertion in 4.3 guards against non-command types being picked up)
    - [x] 4.3: Implement `AllCommandTypesDiscovered` test — asserts that the discovered command count matches the expected count (12 tenant + global admin commands), and lists all discovered types in test output for debugging
    - [x] 4.4: For **tenant commands** (9 commands with `TenantId` property): implement conformance test that for each command type:
        - Creates a **fresh** `InMemoryTenantService` instance per test case (no shared state between tests)
        - Creates a `TenantState` with appropriate preconditions (already-created tenant for most commands; null state for CreateTenant)
        - Creates the same command instance
        - Creates the same `CommandEnvelope` (for commands that require it)
        - Calls `TenantAggregate.Handle(command, state[, envelope])` → capture `DomainResult`
        - Calls `InMemoryTenantService.ProcessTenantCommand<T>(command, envelope)` → capture `DomainResult` (**ONLY** use the low-level `ProcessTenantCommand<T>` — never the high-level `ProcessCommand()` overloads in conformance tests)
        - Asserts both results have identical event sequences: same count, same types, same field values (using record equality)
        - For envelope-required commands, include at least one scenario where a non-admin caller with the correct role succeeds (validates RBAC path conformance, not just global-admin bypass)
    - [x] 4.5: For **global admin commands** (3 commands without `TenantId`): implement conformance test that for each command:
        - Creates a **fresh** `InMemoryTenantService` instance per test case
        - Creates a `GlobalAdministratorsState` with appropriate preconditions
        - Calls `GlobalAdministratorsAggregate.Handle(command, state)` → capture `DomainResult`
        - Calls `InMemoryTenantService.ProcessCommand(command)` → capture `DomainResult` (global admin commands don't use envelopes, so the high-level `ProcessCommand()` is correct here — this is NOT the same concern as tenant commands)
        - Asserts identical event sequences
    - [x] 4.6: Implement rejection conformance — test at least one rejection scenario per command type and assert rejection events match between aggregate and InMemoryTenantService
    - [x] 4.7: Implement NoOp conformance — test at least one NoOp scenario (where applicable) and assert both return NoOp
    - [x] 4.8: Mark conformance tests with `[Trait("Category", "Conformance")]` for CI pipeline filtering
    - [x] 4.9: Verify all tests pass: `dotnet test tests/Hexalith.Tenants.Testing.Tests/`

- [x] Task 5: Full solution validation
    - [x] 5.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 5.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — all tests pass

## Dev Notes

### How These Components Connect

This story delivers two components and one test suite that work together:

1. **`InMemoryTenantService`** (Story 6.1) — the command side. Accepts commands, produces events, maintains aggregate state in memory.
2. **`InMemoryTenantProjection`** (this story) — the query side. Consumes events, maintains read model state, answers queries.
3. **Conformance test suite** (this story) — validates that `InMemoryTenantService` produces identical events as `TenantAggregate` for every command type.

The bridge between command and query sides is `EventHistory` — the accumulated list of successful events from `InMemoryTenantService`. The intended developer workflow:

```csharp
// Full pipeline: command → events → projection → query (under 10 lines)
var svc = new InMemoryTenantService();
svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
svc.ProcessCommand(
    new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
    userId: "admin", isGlobalAdmin: true);

var projection = new InMemoryTenantProjection();
projection.ApplyEvents(svc.EventHistory);

var tenant = projection.GetTenant("acme");  // TenantReadModel with members, config, status
```

### Architecture & Design

**InMemoryTenantProjection** is a lightweight in-memory read model container that reuses the **same** `TenantReadModel` and `GlobalAdministratorReadModel` classes from `Hexalith.Tenants.Server.Projections`. It applies events to these read models using the same `Apply()` methods, maintaining query-testable state without DAPR state store.

**Design pattern:**

```
InMemoryTenantProjection
  ├── Dictionary<string, TenantReadModel> _tenants   // keyed by tenantId
  ├── GlobalAdministratorReadModel _globalAdmins      // singleton (initialized in constructor)
  │
  ├── Apply(IEventPayload) → dispatches event to correct read model
  ├── ApplyEvents(IEnumerable<IEventPayload>) → convenience batch apply
  │
  └── Query methods:
      ├── GetTenant(string tenantId) → TenantReadModel? (null if unknown)
      ├── GetAllTenants() → IReadOnlyList<TenantReadModel>
      └── GetGlobalAdministrators() → GlobalAdministratorReadModel
```

**Event dispatch in `Apply(IEventPayload)`** — use pattern matching exactly like `InMemoryTenantService`, but dispatch to `TenantReadModel.Apply()` and `GlobalAdministratorReadModel.Apply()` instead of `TenantState.Apply()` and `GlobalAdministratorsState.Apply()`:

```csharp
public void Apply(IEventPayload eventPayload)
{
    ArgumentNullException.ThrowIfNull(eventPayload);

    switch (eventPayload)
    {
        // Tenant events — route to per-tenant TenantReadModel
        case TenantCreated e:
            var model = new TenantReadModel();
            model.Apply(e);
            _tenants[e.TenantId] = model;
            break;
        case TenantUpdated e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case TenantDisabled e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case TenantEnabled e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case UserAddedToTenant e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case UserRemovedFromTenant e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case UserRoleChanged e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case TenantConfigurationSet e:
            GetOrThrow(e.TenantId).Apply(e);
            break;
        case TenantConfigurationRemoved e:
            GetOrThrow(e.TenantId).Apply(e);
            break;

        // Global admin events — route to singleton GlobalAdministratorReadModel
        case GlobalAdministratorSet e:
            _globalAdmins.Apply(e);
            break;
        case GlobalAdministratorRemoved e:
            _globalAdmins.Apply(e);
            break;

        // IRejectionEvent — skip (projections only process success events)
        case IRejectionEvent:
            break;

        default:
            throw new InvalidOperationException(
                $"Unknown event type: {eventPayload.GetType().Name}. Update InMemoryTenantProjection.Apply when adding new event types.");
    }
}
```

**Key difference from `InMemoryTenantService` dispatch:** `TenantCreated` must create a **new** `TenantReadModel` instance and add it to the dictionary (the tenant doesn't exist yet in the projection). All other tenant events use `GetOrThrow(tenantId)` to find the existing model. Add a private helper: `private TenantReadModel GetOrThrow(string tenantId) => _tenants.TryGetValue(tenantId, out var m) ? m : throw new InvalidOperationException($"Tenant '{tenantId}' not found in projection. Was TenantCreated applied first?");`

**Rejection event handling:** The projection must **silently skip** `IRejectionEvent` instances. When `InMemoryTenantService.EventHistory` is fed into the projection, it only contains success events (per Story 6.1 design). But when events are manually applied in tests, rejection events should not cause exceptions.

### Conformance Test Design

**Purpose:** Prove FR47 — `InMemoryTenantService` produces identical event sequences as `TenantAggregate` and `GlobalAdministratorsAggregate` for every command type. This is a **release blocker** per architecture mandate.

**What the conformance test validates:** Since `InMemoryTenantService` internally delegates to `TenantAggregate.Handle()`, changes to Handle logic are automatically reflected in both paths. The conformance test's real value is validating the **wrapping and state management layer** of `InMemoryTenantService`:

- State lookup correctness (right `Dictionary` key, right aggregate state for each command)
- Envelope construction accuracy (correct `TenantId`, `AggregateId`, `Domain`, extensions)
- Event application completeness (all events applied to state after success, none applied after rejection/NoOp)
- Guard against reimplementation drift (if someone rewrites the service to NOT delegate to Handle)

**What it does NOT catch:** Changes to `TenantAggregate.Handle()` logic itself — both paths call the same method, so both produce the same result. Aggregate logic correctness is covered by `TenantAggregateTests` in Server.Tests.

**Approach — reflection-driven command discovery:**

```csharp
// Discover all command types from Contracts assembly.
// Simple filter: non-abstract classes in the Commands namespace.
// The AllCommandTypesDiscovered test asserts the count is exactly 12 —
// if a non-command class is added to this namespace, the count assertion
// fails and lists all discovered types for debugging.
var contractsAssembly = typeof(CreateTenant).Assembly;
var commandTypes = contractsAssembly
    .GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract
        && t.Namespace == "Hexalith.Tenants.Contracts.Commands")
    .ToList();
```

**Expected 12 command types (as of this story):**

- Tenant commands (9): `CreateTenant`, `UpdateTenant`, `DisableTenant`, `EnableTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`
- Global admin commands (3): `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, `RemoveGlobalAdministrator`

**Conformance test structure — use individual `[Fact]` methods per command type** (not `[Theory]` — each command has different state setup, envelope requirements, and assertion logic; separate methods provide clear stack traces and easier debugging):

For each tenant command, the test method must:

1. Create a **fresh** `InMemoryTenantService` instance
2. Build equivalent state in both paths — feed the same prerequisite command sequence to both the service and a manually-constructed `TenantState` (e.g., for `AddUserToTenant`, both paths must start from a tenant that was created with identical commands)
3. Create the command instance and `CommandEnvelope` (for commands requiring RBAC)
4. Execute against `TenantAggregate.Handle()` directly with the manual state → capture `DomainResult`
5. Execute against `InMemoryTenantService.ProcessTenantCommand<T>()` with the **same envelope** → capture `DomainResult`
6. Assert: `result1.Events.Count.ShouldBe(result2.Events.Count)`
7. Assert: For each event pair, assert type equality and record equality

**Critical: Use `ProcessTenantCommand<T>(command, envelope)` — NOT the high-level overloads.** The high-level overloads build their own envelopes internally, which would produce different `MessageId` and `CorrelationId` values. The low-level method accepts the same envelope, ensuring identical input to the Handle method.

**Command categorization for test setup:**

| Category                      | Commands                                                                                                                           | Handle Signature                           | State Precondition                               |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------ | ------------------------------------------------ |
| Create (null state)           | `CreateTenant`                                                                                                                     | `(Command, TenantState?)`                  | `null`                                           |
| State-only (no envelope)      | `DisableTenant`, `EnableTenant`                                                                                                    | `(Command, TenantState?)`                  | Created tenant                                   |
| Envelope-required             | `UpdateTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration` | `(Command, TenantState?, CommandEnvelope)` | Created tenant (with user for Remove/ChangeRole) |
| Global admin (no envelope)    | `BootstrapGlobalAdmin`                                                                                                             | `(Command, GlobalAdministratorsState?)`    | `null`                                           |
| Global admin (existing state) | `SetGlobalAdministrator`, `RemoveGlobalAdministrator`                                                                              | `(Command, GlobalAdministratorsState?)`    | Bootstrapped state (with 2+ admins for Remove)   |

**Rejection conformance examples:**

- `CreateTenant` on existing tenant → `TenantAlreadyExistsRejection`
- `AddUserToTenant` on disabled tenant → `TenantDisabledRejection`
- `RemoveUserFromTenant` for non-member → `UserNotInTenantRejection`
- `BootstrapGlobalAdmin` when already bootstrapped → `GlobalAdminAlreadyBootstrappedRejection`

**NoOp conformance examples:**

- `DisableTenant` on already-disabled tenant → NoOp
- `EnableTenant` on already-active tenant → NoOp
- `SetTenantConfiguration` with same key+value → NoOp

**DateTimeOffset comparison caveat:** `TenantCreated`, `TenantDisabled`, and `TenantEnabled` events include `DateTimeOffset.UtcNow` in production Handle methods. Since both calls happen in the same test, the timestamps will differ slightly. For these event types, compare all fields **except** the timestamp field, or compare timestamps with a tolerance (e.g., within 1 second). Use a custom comparison helper that handles this:

```csharp
private static void AssertEventsEqual(DomainResult expected, DomainResult actual)
{
    expected.Events.Count.ShouldBe(actual.Events.Count);
    expected.IsSuccess.ShouldBe(actual.IsSuccess);
    expected.IsRejection.ShouldBe(actual.IsRejection);
    expected.IsNoOp.ShouldBe(actual.IsNoOp);

    for (int i = 0; i < expected.Events.Count; i++)
    {
        var e1 = expected.Events[i];
        var e2 = actual.Events[i];
        e1.GetType().ShouldBe(e2.GetType());

        // For events with timestamps, compare non-timestamp fields
        switch (e1)
        {
            case TenantCreated tc1 when e2 is TenantCreated tc2:
                tc1.TenantId.ShouldBe(tc2.TenantId);
                tc1.Name.ShouldBe(tc2.Name);
                tc1.Description.ShouldBe(tc2.Description);
                // CreatedAt may differ by a few ticks — both use DateTimeOffset.UtcNow
                break;
            case TenantDisabled td1 when e2 is TenantDisabled td2:
                td1.TenantId.ShouldBe(td2.TenantId);
                break;
            case TenantEnabled te1 when e2 is TenantEnabled te2:
                te1.TenantId.ShouldBe(te2.TenantId);
                break;
            default:
                // Record equality — all fields compared (events are records).
                // NOTE: If adding new event types with DateTimeOffset fields,
                // add a special-case branch above to avoid timestamp flakiness.
                e1.ShouldBe(e2);
                break;
        }
    }
}
```

**`AssertEventsEqual` maintenance note:** The helper currently special-cases 3 event types with `DateTimeOffset` fields (`TenantCreated`, `TenantDisabled`, `TenantEnabled`). If a future event type adds a `DateTimeOffset` field, the `default` branch will use record equality and may fail on timestamp differences. When adding new event types with timestamps, update `AssertEventsEqual` to include them in the special-case branches.

**RBAC conformance depth:** Conformance tests must not only test with `isGlobalAdmin: true`. For each envelope-required command, include at least one scenario where a non-admin caller with the minimum required role succeeds. This validates that the RBAC check path in `InMemoryTenantService` produces the same result as `TenantAggregate` — not just the global-admin bypass path.

### Existing Code to Reuse (DO NOT Recreate)

- `TenantReadModel` (`src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`) — read model with all Apply methods for tenant events
- `GlobalAdministratorReadModel` (`src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`) — read model with Apply methods for global admin events
- `TenantAggregate` (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`) — Handle methods for conformance comparison
- `GlobalAdministratorsAggregate` (`src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`) — Handle methods for conformance comparison
- `TenantState` (`src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`) — needed for conformance test state setup
- `GlobalAdministratorsState` (`src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`) — needed for conformance test state setup
- `InMemoryTenantService` (`src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`) — built in Story 6.1, provides `ProcessTenantCommand<T>()` and `EventHistory`
- `TenantTestHelpers` (`src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`) — built in Story 6.1, provides `CreateCommandEnvelope<T>()`
- `DomainResult` (`Hexalith.EventStore.Contracts.Results.DomainResult`) — the three-outcome result type
- `CommandEnvelope` (`Hexalith.EventStore.Contracts.Commands.CommandEnvelope`) — envelope record with eager validation
- `IEventPayload` (`Hexalith.EventStore.Contracts.Events.IEventPayload`) — base event interface
- `IRejectionEvent` (`Hexalith.EventStore.Contracts.Events.IRejectionEvent`) — marker interface for rejections (extends IEventPayload)
- Testing.csproj already references Server + Contracts — no new project references needed for Testing project

### File Structure

```
src/Hexalith.Tenants.Testing/
  ├── Hexalith.Tenants.Testing.csproj   # EXISTS — already references Server + Contracts
  ├── Fakes/
  │   └── InMemoryTenantService.cs      # EXISTS (Story 6.1) — provides ProcessTenantCommand<T> + EventHistory
  ├── Projections/
  │   └── InMemoryTenantProjection.cs   # NEW
  └── Helpers/
      └── TenantTestHelpers.cs          # EXISTS (Story 6.1)

tests/Hexalith.Tenants.Testing.Tests/
  ├── Hexalith.Tenants.Testing.Tests.csproj  # EXISTS — ADD Server project reference
  ├── Conformance/
  │   └── TenantConformanceTests.cs          # NEW
  ├── Projections/
  │   └── InMemoryTenantProjectionTests.cs   # NEW
  ├── Fakes/
  │   └── InMemoryTenantServiceTests.cs      # EXISTS (Story 6.1)
  ├── Helpers/
  │   └── TenantTestHelpersTests.cs          # EXISTS (Story 6.1)
  └── ScaffoldingSmokeTests.cs               # EXISTS — keep as-is
```

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `sealed` classes (no inheritance needed for projection)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- Shouldly for assertions in tests (`result.IsSuccess.ShouldBeTrue()`, `result.Events.Count.ShouldBe(1)`)
- 4-space indentation, CRLF line endings, UTF-8
- XML doc comments (`/// <summary>`) on public API types

### Critical Anti-Patterns (DO NOT)

- **DO NOT** reimplement Apply logic — call the existing `TenantReadModel.Apply()` and `GlobalAdministratorReadModel.Apply()` methods directly
- **DO NOT** add DAPR, actor, or infrastructure dependencies to Testing project
- **DO NOT** create async methods — the Apply methods are synchronous
- **DO NOT** manually register command types in conformance tests — use reflection to auto-discover from the Contracts assembly
- **DO NOT** use the high-level `ProcessCommand()` overloads in conformance tests — use `ProcessTenantCommand<T>(command, envelope)` to ensure identical envelopes
- **DO NOT** modify any existing aggregate, state, or contracts files
- **DO NOT** add unnecessary NuGet packages — Testing.csproj already has what's needed
- **DO NOT** assert exact timestamp equality for `TenantCreated`, `TenantDisabled`, `TenantEnabled` — both calls to `Handle()` create timestamps independently with `DateTimeOffset.UtcNow`

### Testing.Tests.csproj Change Required

Add Server project reference for conformance tests (direct access to aggregates and state types):

```xml
<ProjectReference Include="..\..\src\Hexalith.Tenants.Server\Hexalith.Tenants.Server.csproj" />
```

Note: Story 6.1 also specified this change. If already applied, no additional change needed.

### Previous Story Intelligence (6.1)

Story 6.1 established:

- `InMemoryTenantService` with per-aggregate `Dictionary<string, TenantState>` state and singleton `GlobalAdministratorsState?`
- Event application dispatch via pattern matching switch
- `ProcessTenantCommand<T>(T command, CommandEnvelope envelope)` — the low-level API used by conformance tests
- `EventHistory` — accumulated list of all successful events, reusable by projection tests
- `TenantTestHelpers.CreateCommandEnvelope<T>()` — envelope builder with explicit `aggregateId`
- Testing.csproj already has Server + Contracts references
- Per `Story 6.1 Dev Notes`: "Story 6.1 tests cover basic behavior and isolation; Story 6.2 proves production-test parity exhaustively"

### Git Intelligence

Recent commits show:

- Query contracts added (cd7f3f8) — `GetTenantQuery`, `ListTenantsQuery`, `TenantDetail`, `TenantSummary`, `TenantMember` records in Contracts
- Story 6.1 file created in last commit — InMemoryTenantService story ready for implementation
- Projection files exist: `TenantReadModel.cs`, `GlobalAdministratorReadModel.cs`, `TenantProjection.cs`, `GlobalAdministratorProjection.cs`, `TenantIndexEntry.cs`, `TenantIndexProjection.cs`, `TenantIndexReadModel.cs`
- Story 6.1 is in `review` status — InMemoryTenantService and TenantTestHelpers implementation complete, pending code review approval

### Dependency: Story 6.1 Must Be Complete First

Story 6.2 depends on Story 6.1 deliverables:

- `InMemoryTenantService` — conformance tests compare its output against aggregates
- `ProcessTenantCommand<T>()` — the low-level API for envelope-controlled conformance testing
- `EventHistory` — used for end-to-end projection tests (command → events → projection)
- `TenantTestHelpers` — used for test setup

Story 6.1 is currently in `review` status. It must reach `done` before starting Story 6.2 implementation.

**API stability caveat:** If Story 6.1's code review results in API changes (e.g., `ProcessTenantCommand<T>` signature, `EventHistory` type, `TenantTestHelpers.CreateCommandEnvelope<T>` parameters), update the corresponding references in this story before starting implementation.

### Project Structure Notes

- Testing.csproj references Server (for `TenantReadModel`, `GlobalAdministratorReadModel`, and aggregate access) — no new project references needed in Testing.csproj
- Testing.Tests.csproj: **ADD Server project reference** — conformance tests need direct access to `TenantAggregate`, `GlobalAdministratorsAggregate`, `TenantState`, `GlobalAdministratorsState` for side-by-side comparison
- No changes to any other project in the solution

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.2] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 6] — Epic objectives: testing package
- [Source: _bmad-output/planning-artifacts/prd.md#FR47] — Testing fakes use same domain logic, verified by conformance test suite
- [Source: _bmad-output/planning-artifacts/prd.md#NFR4] — 10ms test fake performance target
- [Source: _bmad-output/planning-artifacts/architecture.md#Conformance Test Pattern] — Mandatory test in Testing.Tests, Tier 1. Reflection-driven auto-discovery. Release blocker
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — Projections/ folder in Testing
- [Source: _bmad-output/planning-artifacts/architecture.md#Type Location Rules] — In-memory projection in Testing
- [Source: _bmad-output/planning-artifacts/architecture.md#Pattern Verification] — Tier 1 conformance tests verify InMemoryTenantService produces identical events as TenantAggregate
- [Source: src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs] — Read model Apply methods (reused by projection)
- [Source: src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs] — Global admin read model Apply methods
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs] — Handle method signatures for conformance comparison
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs] — GlobalAdmin Handle methods for conformance comparison
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — State for conformance test setup
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs] — GlobalAdmin state for conformance test setup
- [Source: _bmad-output/implementation-artifacts/6-1-in-memory-tenant-service-and-test-helpers.md] — Previous story with ProcessTenantCommand<T>, EventHistory, TenantTestHelpers APIs
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs] — Three-outcome model (Success, Rejection, NoOp)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs] — Envelope record with eager validation
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IEventPayload.cs] — Base event interface
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IRejectionEvent.cs] — Rejection event marker interface

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

None — implementation completed without issues.

### Completion Notes List

- **Task 1**: Added Server project reference to Testing.Tests.csproj for direct aggregate/state access in conformance tests. Build verified: 0 warnings, 0 errors.
- **Task 2**: Created `InMemoryTenantProjection` in Testing/Projections/ — lightweight in-memory read model container reusing `TenantReadModel` and `GlobalAdministratorReadModel` from Server. Implements `Apply(IEventPayload)` with pattern matching dispatch, `ApplyEvents(IEnumerable<IEventPayload>)` convenience method, and query methods (`GetTenant`, `GetAllTenants`, `GetGlobalAdministrators`). Silently skips `IRejectionEvent` instances. Build verified.
- **Task 3**: Created 17 unit tests covering all event types, cross-tenant isolation, end-to-end pipeline (service → events → projection → query), rejection event skipping, null/empty edge cases. All 17 tests pass.
- **Task 4**: Created 37 conformance tests: 1 discovery test (reflection-based, asserts 12 command types), 15 success conformance tests (9 tenant + 3 global admin commands, with non-admin RBAC variants for all 6 envelope-required commands), 11 rejection conformance tests (at least one per command type), 7 NoOp conformance tests, plus 3 global admin conformance tests. All tests use `[Trait("Category", "Conformance")]` for CI filtering. `AssertEventsEqual` helper handles `DateTimeOffset` field comparison for `TenantCreated`, `TenantDisabled`, `TenantEnabled` events. All 37 tests pass.
- **Task 5**: Full solution build: 0 warnings, 0 errors. All 325 Tier 1 tests pass (71 Testing.Tests + 34 Contracts.Tests + 203 Server.Tests + 17 Sample.Tests). 2 pre-existing IntegrationTests failures (DAPR infrastructure not available locally — unrelated to this story).

### Change Log

- 2026-03-18: Story 6.2 implementation complete — InMemoryTenantProjection + 37 conformance tests + 17 projection unit tests

### File List

- `tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj` — MODIFIED (added Server project reference)
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs` — NEW
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs` — NEW
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` — NEW

