# Story 6.1: In-Memory Tenant Service & Test Helpers

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want an in-memory fake tenant service and test helpers that execute the same domain logic as production,
So that I can write tenant integration tests in under 10 lines without external infrastructure.

## Acceptance Criteria

1. **Given** a test project references the Hexalith.Tenants.Testing NuGet package
   **When** the developer creates an InMemoryTenantService instance
   **Then** the service accepts commands (CreateTenant, AddUserToTenant, etc.) and produces the same domain events as the production TenantAggregate

2. **Given** the InMemoryTenantService is instantiated
   **When** a CreateTenant command is processed followed by AddUserToTenant
   **Then** the events are returned and state is maintained in memory with no DAPR, no actors, and no external dependencies

3. **Given** the InMemoryTenantService
   **When** a command violates domain invariants (e.g., duplicate user, disabled tenant, role escalation)
   **Then** the same rejection events are returned as in production via DomainResult.Rejection() (UserAlreadyInTenantRejection, TenantDisabledRejection, RoleEscalationRejection, etc.)

4. **Given** TenantTestHelpers exist in the Testing package
   **When** a developer writes a tenant integration test
   **Then** common setup patterns (create tenant, add user, bootstrap admin) are available as helper methods reducing test authoring to under 10 lines per test

5. **Given** the InMemoryTenantService processes a command
   **When** execution time is measured
   **Then** commands execute and produce events within 10ms (NFR4)

6. **Given** the InMemoryTenantService
   **When** two tenants are created and users are added to each
   **Then** aggregate state for tenant A never contains data from tenant B (per-aggregate isolation guarantee)

## Tasks / Subtasks

- [x] Task 1: Create `InMemoryTenantService` in Testing project (AC: #1, #2, #3, #5, #6)
    - [x] 1.1: Create `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
    - [x] 1.2: Implement per-aggregate state storage with `Dictionary<string, TenantState>` for tenant aggregates and a single `GlobalAdministratorsState?` for the global admin singleton
    - [x] 1.3: Implement **tenant command** strongly-typed overloads — one per command type:
        - `DomainResult ProcessCommand(CreateTenant command)` — no envelope needed (state-only Handle signature)
        - `DomainResult ProcessCommand(DisableTenant command)` — no envelope needed
        - `DomainResult ProcessCommand(EnableTenant command)` — no envelope needed
        - `DomainResult ProcessCommand(UpdateTenant command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
        - `DomainResult ProcessCommand(AddUserToTenant command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
        - `DomainResult ProcessCommand(RemoveUserFromTenant command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
        - `DomainResult ProcessCommand(ChangeUserRole command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
        - `DomainResult ProcessCommand(SetTenantConfiguration command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
        - `DomainResult ProcessCommand(RemoveTenantConfiguration command, string userId, bool isGlobalAdmin = false)` — builds envelope internally
    - [x] 1.4: Implement **global admin command** strongly-typed overloads:
        - `DomainResult ProcessCommand(BootstrapGlobalAdmin command)` — no envelope needed
        - `DomainResult ProcessCommand(SetGlobalAdministrator command)` — no envelope needed
        - `DomainResult ProcessCommand(RemoveGlobalAdministrator command)` — no envelope needed
    - [x] 1.5: Implement low-level overload `DomainResult ProcessTenantCommand<T>(T command, CommandEnvelope envelope)` — used by Story 6.2 conformance tests that need to compare behavior with identical envelopes between InMemoryTenantService and TenantAggregate
    - [x] 1.6: Apply resulting events to state — **ONLY when `result.IsSuccess` is true** (NoOp and Rejection results must NOT mutate state). Use a pattern matching switch for event dispatch to call the correct `Apply()` overload (see Dev Notes: Event Application Dispatch Pattern)
    - [x] 1.7: Return `DomainResult` directly from Handle methods — no translation, no wrapping
    - [x] 1.8: Expose read-only state inspection API for test assertions:
        - `TenantState? GetTenantState(string tenantId)` — returns current state for a tenant (or null if no commands processed for that tenant)
        - `GlobalAdministratorsState? GetGlobalAdminState()` — returns current global admin state (or null if not bootstrapped)
        - `IReadOnlyList<IEventPayload> EventHistory` — accumulated list of all successful events across all commands (append on `IsSuccess` only). Enables sequence assertions and is reused by Story 6.2 conformance tests.
    - [x] 1.9: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create `TenantTestHelpers` in Testing project (AC: #4)
    - [x] 2.1: Create `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
    - [x] 2.2: Implement `CreateTenant()` — creates a tenant via InMemoryTenantService and returns the DomainResult with TenantCreated event (synchronous — Handle methods are sync, no Async suffix per anti-pattern)
    - [x] 2.3: Implement `CreateTenantWithOwner()` — creates a tenant and adds an owner user, returns the combined DomainResults
    - [x] 2.4: Implement `BootstrapGlobalAdmin()` — bootstraps a global administrator, returns DomainResult
    - [x] 2.5: Implement `CreateCommandEnvelope<T>(T command, string aggregateId, string userId, bool isGlobalAdmin = false)` — builds a CommandEnvelope with explicit `aggregateId` parameter (DO NOT use `dynamic` to extract TenantId — require caller to pass it explicitly for type safety)
    - [x] 2.6: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create unit tests for InMemoryTenantService (AC: #1-6)
    - [x] 3.1: Create `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
    - [x] 3.2: Test: CreateTenant produces TenantCreated event
    - [x] 3.3: Test: CreateTenant followed by AddUserToTenant produces correct events with maintained state
    - [x] 3.4: Test: Duplicate tenant creation returns TenantAlreadyExistsRejection
    - [x] 3.5: Test: AddUserToTenant on disabled tenant returns TenantDisabledRejection
    - [x] 3.6: Test: Duplicate user add returns UserAlreadyInTenantRejection
    - [x] 3.7: Test: Role escalation returns RoleEscalationRejection
    - [x] 3.8: Test: Cross-tenant isolation — tenant A data never leaks into tenant B
    - [x] 3.9: Test: BootstrapGlobalAdmin + SetGlobalAdministrator + RemoveGlobalAdministrator
    - [x] 3.10: Test: Performance — 100 iterations of CreateTenant + AddUserToTenant, skip first 5 as warmup, assert p95 < 10ms per command (NFR4). Use `Stopwatch` per iteration, collect timings, sort, check 95th percentile. Mark with `[Trait("Category", "Performance")]` so CI can exclude if runners are too slow — the 10ms target is for local execution
    - [x] 3.11: Verify all tests pass: `dotnet test tests/Hexalith.Tenants.Testing.Tests/`

- [x] Task 4: Create unit tests for TenantTestHelpers (AC: #4)
    - [x] 4.1: Create `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs`
    - [x] 4.2: Test: CreateTenant returns success with TenantCreated event
    - [x] 4.3: Test: CreateTenantWithOwner returns success with both TenantCreated and UserAddedToTenant events
    - [x] 4.4: Test: BootstrapGlobalAdmin returns success with GlobalAdministratorSet event
    - [x] 4.5: Test: CreateCommandEnvelope builds valid CommandEnvelope with correct fields
    - [x] 4.6: Verify all tests pass: `dotnet test tests/Hexalith.Tenants.Testing.Tests/`

- [x] Task 5: Full solution validation
    - [x] 5.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 5.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — all Tier 1 tests pass

## Dev Notes

### Architecture & Design

**InMemoryTenantService** is a lightweight in-memory command processor that delegates to the **same** `TenantAggregate.Handle()` and `GlobalAdministratorsAggregate.Handle()` static methods used in production. This is the key design: the Testing package does NOT reimplement domain logic — it wraps the existing pure-function Handle/Apply methods in a simple in-memory container.

**Design pattern — two-tier API:**

```
InMemoryTenantService
  ├── Dictionary<string, TenantState> _tenantStates   // keyed by aggregateId (tenantId)
  ├── GlobalAdministratorsState? _globalAdminState     // singleton, aggregateId = "system"
  │
  ├── HIGH-LEVEL: Strongly-typed overloads (primary API for test authors)
  │   ├── ProcessCommand(CreateTenant command) → DomainResult
  │   ├── ProcessCommand(AddUserToTenant command, string userId, bool isGlobalAdmin) → DomainResult
  │   ├── ProcessCommand(BootstrapGlobalAdmin command) → DomainResult
  │   └── ... (one overload per command type — 12 total)
  │
  ├── LOW-LEVEL: Advanced envelope-based method
  │   └── ProcessTenantCommand<T>(T command, CommandEnvelope envelope) → DomainResult
  │
  └── Internal flow for all methods:
        1. Look up state by aggregateId (null if first command for that aggregate)
        2. Build CommandEnvelope internally if not provided (for high-level methods)
        3. Call TenantAggregate.Handle(command, state, [envelope]) → DomainResult
        4. If IsSuccess → Apply each event to state via runtime dispatch, store updated state
        5. Return DomainResult (unchanged — same events as production)
```

**High-level overloads hide envelope construction** from test authors. **Simple rule: if the aggregate's `Handle()` method takes a `CommandEnvelope` parameter, the overload needs `userId` + `isGlobalAdmin`. If it doesn't, only the command is needed.**

- **Command-only** (no RBAC): CreateTenant, DisableTenant, EnableTenant, BootstrapGlobalAdmin, SetGlobalAdministrator, RemoveGlobalAdministrator
- **Command + userId + isGlobalAdmin** (RBAC): UpdateTenant, AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, SetTenantConfiguration, RemoveTenantConfiguration

**Critical constraint:** The Handle methods on `TenantAggregate` have different signatures:

- Some take `(Command, TenantState?)` — CreateTenant, DisableTenant, EnableTenant
- Some take `(Command, TenantState?, CommandEnvelope)` — UpdateTenant, AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, SetTenantConfiguration, RemoveTenantConfiguration

The InMemoryTenantService must handle both patterns. For commands requiring a `CommandEnvelope`, the service must accept or construct one. The `TenantTestHelpers.CreateCommandEnvelope<T>()` helper simplifies this.

**GlobalAdmin extension key:** Commands needing RBAC checks use `envelope.Extensions["actor:globalAdmin"] = "true"` to indicate the caller is a global admin. In production, this is server-populated from JWT claims (SEC-4). In tests, the envelope must be constructed with this extension when simulating global admin operations.

**Instance lifecycle:** Create a new `InMemoryTenantService` instance per test method. xUnit creates a new test class instance per test, so storing the service as a field achieves natural isolation. No `Reset()` method needed.

**Target ergonomics — "under 10 lines" example (AC4 validation):**

```csharp
[Fact]
public void Adding_user_to_tenant_produces_event()
{
    var svc = new InMemoryTenantService();
    svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
    var result = svc.ProcessCommand(
        new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
        userId: "owner", isGlobalAdmin: true);
    result.IsSuccess.ShouldBeTrue();
    result.Events[0].ShouldBeOfType<UserAddedToTenant>();
}
```

This is 8 lines of test body. The dev agent should validate that real tests achieve similar brevity.

### Event Application Dispatch Pattern

After a successful `Handle()` call, apply each event to the correct state object using a **pattern matching switch**. This gives compile-time exhaustiveness checking — if a new event type is added, the compiler will warn.

```csharp
// Apply events ONLY when result.IsSuccess is true.
// NoOp (empty events) and Rejection (IRejectionEvent) must NOT mutate state.
private void ApplyTenantEvents(TenantState state, DomainResult result)
{
    if (!result.IsSuccess) return;

    foreach (var evt in result.Events)
    {
        switch (evt)
        {
            case TenantCreated e: state.Apply(e); break;
            case TenantUpdated e: state.Apply(e); break;
            case TenantDisabled e: state.Apply(e); break;
            case TenantEnabled e: state.Apply(e); break;
            case UserAddedToTenant e: state.Apply(e); break;
            case UserRemovedFromTenant e: state.Apply(e); break;
            case UserRoleChanged e: state.Apply(e); break;
            case TenantConfigurationSet e: state.Apply(e); break;
            case TenantConfigurationRemoved e: state.Apply(e); break;
            default: throw new InvalidOperationException(
                $"Unknown event type: {evt.GetType().Name}. Update ApplyTenantEvents when adding new event types.");
        }
    }
}

private void ApplyGlobalAdminEvents(GlobalAdministratorsState state, DomainResult result)
{
    if (!result.IsSuccess) return;

    foreach (var evt in result.Events)
    {
        switch (evt)
        {
            case GlobalAdministratorSet e: state.Apply(e); break;
            case GlobalAdministratorRemoved e: state.Apply(e); break;
            default: throw new InvalidOperationException(
                $"Unknown event type: {evt.GetType().Name}. Update ApplyGlobalAdminEvents when adding new event types.");
        }
    }
}
```

The `default` arm is a safety net — the conformance test in Story 6.2 will catch any missing dispatch before release.

### Existing Code to Reuse (DO NOT Recreate)

- `TenantAggregate` (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`) — all Handle methods are `public static`, directly callable
- `TenantState` (`src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`) — all Apply methods are `public`, directly callable
- `GlobalAdministratorsAggregate` (`src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`) — all Handle methods are `public static`
- `GlobalAdministratorsState` (`src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`) — all Apply methods are `public`
- `DomainResult` (`Hexalith.EventStore.Contracts.Results.DomainResult`) — returned as-is from Handle methods
- `CommandEnvelope` (`Hexalith.EventStore.Contracts.Commands.CommandEnvelope`) — required by some Handle methods
- Testing.csproj already references Server and Contracts — no new project references needed

### Existing Test Patterns Reference

See `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` for the existing `CreateCommand<T>` helper pattern. The `TenantTestHelpers.CreateCommandEnvelope<T>()` should follow the same envelope construction approach but with an explicit `aggregateId` parameter instead of `dynamic` cast.

### EventStore.Testing Reference Decision

**DO NOT add EventStore.Testing reference in Story 6.1.** Keep helpers self-contained. EventStore.Testing assertions can be added in Story 6.2 if needed.

### File Structure

```
src/Hexalith.Tenants.Testing/
  ├── Hexalith.Tenants.Testing.csproj   # EXISTS — already references Server + Contracts
  ├── Fakes/
  │   └── InMemoryTenantService.cs      # NEW
  └── Helpers/
      └── TenantTestHelpers.cs          # NEW

tests/Hexalith.Tenants.Testing.Tests/
  ├── Hexalith.Tenants.Testing.Tests.csproj  # EXISTS — references Testing + Contracts; ADD Server reference
  ├── Fakes/
  │   └── InMemoryTenantServiceTests.cs      # NEW
  ├── Helpers/
  │   └── TenantTestHelpersTests.cs          # NEW
  └── ScaffoldingSmokeTests.cs               # EXISTS — keep as-is
```

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `sealed` classes (no inheritance needed for fakes/helpers)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- Shouldly for assertions in tests (`result.IsSuccess.ShouldBeTrue()`, `result.Events.Count.ShouldBe(1)`)
- 4-space indentation, CRLF line endings, UTF-8
- XML doc comments (`/// <summary>`) on public API types

### Critical Anti-Patterns (DO NOT)

- **DO NOT** reimplement Handle/Apply logic — call the existing aggregate methods directly
- **DO NOT** add DAPR, actor, or infrastructure dependencies to Testing project
- **DO NOT** create async methods if the underlying logic is synchronous — the Handle methods are sync; only wrap in Task if needed for interface compliance
- **DO NOT** throw exceptions for domain rejections — return `DomainResult.Rejection()` (same as production)
- **DO NOT** use `dynamic` type in the public API — use strongly-typed overloads or generics
- **DO NOT** modify any existing aggregate, state, or contracts files
- **DO NOT** add unnecessary NuGet packages to Testing.csproj — it already has Shouldly, NSubstitute, xunit.assert
- **DO NOT** mix GlobalAdministrators state with per-tenant state — they are separate aggregates with different aggregate IDs ("system" vs tenant-specific)

### CommandEnvelope Construction Notes

`CommandEnvelope` constructor validates all fields eagerly:

- `MessageId` — must be non-empty (use `Guid.NewGuid().ToString()` or ULID)
- `TenantId` — must be non-empty (use `"system"` — all tenant domain commands execute in the `system` tenant context per architecture)
- `Domain` — must be non-empty (use `"tenants"`)
- `AggregateId` — must be non-empty (the managed tenantId for tenant commands, `"system"` for global admin commands)
- `CommandType` — must be non-empty (use `typeof(T).Name`)
- `Payload` — must be non-null (use `JsonSerializer.SerializeToUtf8Bytes(command)`)
- `CorrelationId` — must be non-empty (use `Guid.NewGuid().ToString()`)
- `CausationId` — nullable (can be null)
- `UserId` — must be non-empty (the acting user ID)
- `Extensions` — nullable (use `new Dictionary<string, string> { ["actor:globalAdmin"] = "true" }` for global admin simulation)

### TenantId vs AggregateId Clarification

In the Hexalith.EventStore architecture, the `system` tenant is the operational tenant for all tenant management commands. The managed tenant ID (e.g., "acme-corp") is the `AggregateId`. So:

- `envelope.TenantId = "system"` (operational context)
- `envelope.AggregateId = command.TenantId` (the tenant being managed)
- `envelope.Domain = "tenants"`

For GlobalAdministrators commands:

- `envelope.TenantId = "system"`
- `envelope.AggregateId = "system"` (singleton aggregate)
- `envelope.Domain = "tenants"`

### Performance Note (NFR4)

All Handle/Apply methods are pure synchronous functions with dictionary lookups. No I/O, no async, no serialization during command processing (serialization only happens in `CommandEnvelope` construction for the `Payload` field — this is unavoidable but fast). The 10ms target is easily achievable. The performance test should run 100 iterations with `Stopwatch` per iteration, skip the first 5 as warmup, collect timings, sort, and assert the 95th percentile is under 10ms. This approach avoids CI flakiness from single-run measurements.

### Conformance Test Dependency (Story 6.2)

This story's correctness guarantee is fully validated by **Story 6.2's conformance test suite**, which reflection-discovers all command types and asserts that `InMemoryTenantService` produces identical event sequences as `TenantAggregate` for every command. Story 6.1 tests cover basic behavior and isolation; Story 6.2 proves production-test parity exhaustively. This is a release blocker per architecture mandate.

### Project Structure Notes

- Testing.csproj already has the right references — no csproj changes needed (EventStore.Testing deliberately excluded, see decision above)
- Testing.Tests.csproj: **ADD Server project reference** — tests need direct access to `TenantState` and aggregate types for cross-tenant isolation assertions and state verification. Add: `<ProjectReference Include="..\..\src\Hexalith.Tenants.Server\Hexalith.Tenants.Server.csproj" />`
- No changes to any other project in the solution

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.1] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 6] — Epic objectives: testing package
- [Source: _bmad-output/planning-artifacts/prd.md#FR46-FR47] — In-memory fakes, same domain logic
- [Source: _bmad-output/planning-artifacts/prd.md#NFR4] — 10ms test fake performance target
- [Source: _bmad-output/planning-artifacts/architecture.md#Conformance Test Pattern] — InMemoryTenantService must match TenantAggregate output (enforced in Story 6.2)
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — Fakes/, Helpers/, Projections/ folder layout
- [Source: _bmad-output/planning-artifacts/architecture.md#Type Location Rules] — In-memory fakes in Testing, In-memory projection in Testing
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs] — Handle method signatures (static pure functions)
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — Apply method patterns
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs] — GlobalAdmin Handle methods
- [Source: src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs] — GlobalAdmin Apply methods
- [Source: tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs] — Existing test patterns, CreateCommand helper
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs] — Constructor validation requirements
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs] — Three-outcome model (Success, Rejection, NoOp)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- All builds: 0 warnings, 0 errors
- Testing.Tests: 17/17 tests passed (10 InMemoryTenantService + 6 TenantTestHelpers + 1 scaffolding smoke)
- Full solution Tier 1: 285 tests passed (17 Testing.Tests + 17 Sample.Tests + 48 Client.Tests + 203 Server.Tests)
- Performance test p95 well under 10ms target (NFR4 satisfied)
- 2 pre-existing IntegrationTests failures (Tier 3, DAPR infra-dependent) — unrelated to this story

### Completion Notes List

- InMemoryTenantService: 12 strongly-typed ProcessCommand overloads (9 tenant + 3 global admin) + 1 generic ProcessTenantCommand<T> for conformance testing
- Two-tier API design: high-level (hides envelope) and low-level (explicit envelope for Story 6.2)
- Event application uses pattern matching switch with default arm safety net
- State isolation: Dictionary<string, TenantState> for per-tenant + GlobalAdministratorsState? singleton
- EventHistory tracks all successful events for sequence assertions
- TenantTestHelpers: CreateTenant, CreateTenantWithOwner, BootstrapGlobalAdmin, CreateCommandEnvelope<T>
- CreateCommandEnvelope uses explicit aggregateId parameter (no dynamic cast) for type safety
- Tests cover: success paths, all rejection types (TenantAlreadyExists, TenantDisabled, UserAlreadyInTenant, RoleEscalation), NoOp non-mutation, cross-tenant isolation, global admin lifecycle, performance p95<10ms
- Added Server project reference to Testing.Tests.csproj for state inspection in tests
- No existing files modified (only new files created + 1 csproj reference addition)

### Change Log

- 2026-03-18: Story 6.1 implemented — InMemoryTenantService, TenantTestHelpers, and comprehensive test suite (17 tests)

### File List

New files:

- src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs
- src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs
- tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs
- tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs

Modified files:

- tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj (added Server project reference)
