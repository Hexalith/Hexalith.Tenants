# Story 2.1: Tenant Domain Contracts

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want all tenant commands, events, enums, and identity types defined in the Contracts package,
So that consuming services and all other packages have a stable, shared API surface to reference.

## Acceptance Criteria

1. **Given** the Contracts project exists **When** a developer inspects the Commands folder **Then** it contains all 12 command records: CreateTenant, UpdateTenant, DisableTenant, EnableTenant, AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, SetTenantConfiguration, RemoveTenantConfiguration, BootstrapGlobalAdmin, SetGlobalAdministrator, RemoveGlobalAdministrator

2. **Given** the Contracts project exists **When** a developer inspects the Events folder **Then** it contains all 11 event records: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, TenantConfigurationSet, TenantConfigurationRemoved, GlobalAdministratorSet, GlobalAdministratorRemoved

3. **Given** the Contracts project exists **When** a developer inspects the Enums folder **Then** it contains TenantRole (TenantOwner, TenantContributor, TenantReader) and TenantStatus (Active, Disabled)

4. **Given** the Contracts project exists **When** a developer inspects the Identity folder **Then** it contains TenantIdentity with identity scheme helpers mapping to `system:tenants:{aggregateId}`

5. **Given** all event types exist **When** each event is serialized to JSON via System.Text.Json and deserialized back **Then** deep equality holds for all fields (serialization round-trip test in Contracts.Tests)

6. **Given** all command and event types exist **When** a reflection-based test scans the Contracts assembly **Then** all commands follow `{Verb}{Target}` naming and all events follow `{Target}{PastVerb}` naming

7. **Given** all event types exist **When** a developer inspects any event record **Then** every event includes `TenantId` as a top-level field identifying the managed tenant

## Tasks / Subtasks

- [ ] Task 1: Create Enums (AC: #3)
  - [ ] 1.1: Create `Enums/TenantRole.cs` — enum with values: `TenantOwner`, `TenantContributor`, `TenantReader`
  - [ ] 1.2: Create `Enums/TenantStatus.cs` — enum with values: `Active`, `Disabled`

- [ ] Task 2: Create Identity types (AC: #4)
  - [ ] 2.1: Create `Identity/TenantIdentity.cs` — static helper class providing:
    - `const string DefaultTenantId = "system"` (platform tenant context)
    - `const string Domain = "tenants"` (domain name)
    - `static AggregateIdentity ForTenant(string managedTenantId)` → returns `new AggregateIdentity("system", "tenants", managedTenantId)`
    - `static AggregateIdentity ForGlobalAdministrators()` → returns `new AggregateIdentity("system", "tenants", "global-administrators")`

- [ ] Task 3: Create Tenant Lifecycle Commands (AC: #1)
  - [ ] 3.1: Create `Commands/CreateTenant.cs` — `record CreateTenant(string TenantId, string Name, string? Description)`
  - [ ] 3.2: Create `Commands/UpdateTenant.cs` — `record UpdateTenant(string TenantId, string Name, string? Description)`
  - [ ] 3.3: Create `Commands/DisableTenant.cs` — `record DisableTenant(string TenantId)`
  - [ ] 3.4: Create `Commands/EnableTenant.cs` — `record EnableTenant(string TenantId)`

- [ ] Task 4: Create User-Role Commands (AC: #1)
  - [ ] 4.1: Create `Commands/AddUserToTenant.cs` — `record AddUserToTenant(string TenantId, string UserId, TenantRole Role)`
  - [ ] 4.2: Create `Commands/RemoveUserFromTenant.cs` — `record RemoveUserFromTenant(string TenantId, string UserId)`
  - [ ] 4.3: Create `Commands/ChangeUserRole.cs` — `record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole)`

- [ ] Task 5: Create Configuration Commands (AC: #1)
  - [ ] 5.1: Create `Commands/SetTenantConfiguration.cs` — `record SetTenantConfiguration(string TenantId, string Key, string Value)`
  - [ ] 5.2: Create `Commands/RemoveTenantConfiguration.cs` — `record RemoveTenantConfiguration(string TenantId, string Key)`

- [ ] Task 6: Create Global Administrator Commands (AC: #1)
  - [ ] 6.1: Create `Commands/BootstrapGlobalAdmin.cs` — `record BootstrapGlobalAdmin(string UserId)`
  - [ ] 6.2: Create `Commands/SetGlobalAdministrator.cs` — `record SetGlobalAdministrator(string UserId)`
  - [ ] 6.3: Create `Commands/RemoveGlobalAdministrator.cs` — `record RemoveGlobalAdministrator(string UserId)`

- [ ] Task 7: Create Tenant Lifecycle Events (AC: #2, #7)
  - [ ] 7.1: Create `Events/TenantCreated.cs` — `record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt) : IEventPayload`
  - [ ] 7.2: Create `Events/TenantUpdated.cs` — `record TenantUpdated(string TenantId, string Name, string? Description) : IEventPayload`
  - [ ] 7.3: Create `Events/TenantDisabled.cs` — `record TenantDisabled(string TenantId, DateTimeOffset DisabledAt) : IEventPayload`
  - [ ] 7.4: Create `Events/TenantEnabled.cs` — `record TenantEnabled(string TenantId, DateTimeOffset EnabledAt) : IEventPayload`

- [ ] Task 8: Create User-Role Events (AC: #2, #7)
  - [ ] 8.1: Create `Events/UserAddedToTenant.cs` — `record UserAddedToTenant(string TenantId, string UserId, TenantRole Role) : IEventPayload`
  - [ ] 8.2: Create `Events/UserRemovedFromTenant.cs` — `record UserRemovedFromTenant(string TenantId, string UserId) : IEventPayload`
  - [ ] 8.3: Create `Events/UserRoleChanged.cs` — `record UserRoleChanged(string TenantId, string UserId, TenantRole OldRole, TenantRole NewRole) : IEventPayload`

- [ ] Task 9: Create Configuration Events (AC: #2, #7)
  - [ ] 9.1: Create `Events/TenantConfigurationSet.cs` — `record TenantConfigurationSet(string TenantId, string Key, string Value) : IEventPayload`
  - [ ] 9.2: Create `Events/TenantConfigurationRemoved.cs` — `record TenantConfigurationRemoved(string TenantId, string Key) : IEventPayload`

- [ ] Task 10: Create Global Administrator Events (AC: #2, #7)
  - [ ] 10.1: Create `Events/GlobalAdministratorSet.cs` — `record GlobalAdministratorSet(string TenantId, string UserId) : IEventPayload`. Note: `TenantId` is a parameter — it will typically be `"system"` when produced by GlobalAdministratorAggregate Handle methods (Story 2.2), but do NOT hardcode a default in the record itself
  - [ ] 10.2: Create `Events/GlobalAdministratorRemoved.cs` — `record GlobalAdministratorRemoved(string TenantId, string UserId) : IEventPayload`. Note: same as 10.1 — `TenantId` is a parameter, not a hardcoded value

- [ ] Task 11: Create serialization round-trip tests in Contracts.Tests (AC: #5)
  - [ ] 11.1: Create `EventSerializationTests.cs` — for each event type implementing `IEventPayload`, create instance with all fields populated using non-default, distinguishable test data (e.g., `"tenant-abc"` not `""`, `DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")` not `default`, `TenantRole.TenantContributor` not the first enum value), serialize to JSON via `System.Text.Json.JsonSerializer`, deserialize back, assert deep equality with Shouldly
  - [ ] 11.2: Use reflection to auto-discover all event types implementing `IEventPayload` in the Contracts assembly — DO NOT write individual test methods per event type; use `[Theory]` with `[MemberData]` so new events in future stories are automatically covered

- [ ] Task 12: Create naming convention reflection tests in Contracts.Tests (AC: #6)
  - [ ] 12.1: Create `NamingConventionTests.cs` — scan Contracts assembly for all record types in `Commands` namespace, verify each starts with a recognized verb from this whitelist: `Create`, `Update`, `Disable`, `Enable`, `Add`, `Remove`, `Change`, `Set`, `Bootstrap`. The test MUST FAIL if a command uses an unrecognized verb prefix (whitelist approach, not blacklist). Scan all types implementing `IEventPayload` in `Events` namespace, verify each ends with a recognized past-tense suffix: `Created`, `Updated`, `Disabled`, `Enabled`, `Added`, `Removed`, `Changed`, `Set`
  - [ ] 12.2: Verify all event types include a `TenantId` property (AC: #7) via reflection

- [ ] Task 13: Build verification (AC: all)
  - [ ] 13.1: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — zero errors, zero warnings
  - [ ] 13.2: Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` — all tests pass

## Dev Notes

### Developer Context

This is the **first domain logic story** — everything before this was scaffolding. The Contracts package is the stable API surface consumed by all other packages (Client, Server, Testing) and by consuming services via NuGet. Every type defined here becomes a public contract.

**Key mental model:** Commands are inputs (what a user wants to do). Events are outputs (what happened). Commands are NOT serialized into the event store — only events are. Commands flow through MediatR in the CommandApi; events flow through DAPR pub/sub to consuming services. This is why commands don't implement `IEventPayload` but events do.

**GlobalAdmin vs Tenant scope:** Most types operate on a specific managed tenant (identified by `TenantId`). The GlobalAdmin commands (`BootstrapGlobalAdmin`, `SetGlobalAdministrator`, `RemoveGlobalAdministrator`) are platform-level — they operate on the singleton `global-administrators` aggregate under the `system` platform tenant. Their events still carry `TenantId` (set to `"system"`) for consistency with the architecture rule that ALL events must include `TenantId`.

### Technical Requirements

**C# Record Types:**
- All commands and events are `public record` types with positional parameters (primary constructor syntax)
- One type per file (enforced by `.editorconfig`)
- File-scoped namespaces: `namespace Hexalith.Tenants.Contracts.Commands;`
- No XML doc comments needed on record types — the type name and parameters are self-documenting for contracts

**Exact File Template — Command Example:**
```csharp
namespace Hexalith.Tenants.Contracts.Commands;

public record CreateTenant(string TenantId, string Name, string? Description);
```

**Exact File Template — Event Example:**
```csharp
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Tenants.Contracts.Events;

public record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt) : IEventPayload;
```

**Exact File Template — Enum Example:**
```csharp
namespace Hexalith.Tenants.Contracts.Enums;

public enum TenantRole
{
    TenantOwner,
    TenantContributor,
    TenantReader,
}
```

**Required Using Directives:**
- Event files MUST include `using Hexalith.EventStore.Contracts.Events;` for `IEventPayload` — this is NOT covered by implicit usings
- Command files referencing `TenantRole` MUST include `using Hexalith.Tenants.Contracts.Enums;`
- Alternative: add `<Using Include="Hexalith.EventStore.Contracts.Events" />` to the Contracts .csproj `<ItemGroup>` as a global using to avoid repetitive directives in all 11 event files. This is the preferred approach.

**Event Contract Rules:**
- All events implement `IEventPayload` from `Hexalith.EventStore.Contracts.Events`
- All events include `TenantId` as first positional parameter (architecture risk mitigation — the EventStore envelope's `tenantId` field is `system`, so the managed tenant ID must be in the event payload)
- Timestamp fields use `DateTimeOffset` and follow `{Action}At` naming (e.g., `CreatedAt`, `DisabledAt`, `EnabledAt`)
- No nullable fields on events except optional descriptors (`Description` on `TenantCreated` and `TenantUpdated`)
- Events represent immutable facts — records enforce this naturally

**Command Rules:**
- Commands are plain records — they do NOT implement `IEventPayload` or any interface
- Commands carry the minimum data needed for the Handle method to make a decision
- Tenant-scoped commands include `TenantId` to identify the target tenant
- GlobalAdmin commands include `UserId` only (they target the singleton `global-administrators` aggregate)

**Enum Rules:**
- Simple C# enums, no `[Flags]` attribute
- `TenantRole`: `TenantOwner`, `TenantContributor`, `TenantReader` (3 values — `GlobalAdministrator` is NOT a role in this enum; it's managed by a separate aggregate)
- `TenantStatus`: `Active`, `Disabled` (2 values)

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder |
|------|---------|--------|
| Commands | Contracts | `Commands/` |
| Events | Contracts | `Events/` |
| Enums | Contracts | `Enums/` |
| Identity | Contracts | `Identity/` |

**DO NOT place in Contracts:**
- Aggregates, State classes → Server
- Domain exceptions → Server
- FluentValidation validators → Server
- Projections, Read models → Server

**Naming Conventions (MUST follow):**
- Commands: `{Verb}{Target}` — PascalCase, verb-first (e.g., `CreateTenant`, NOT `TenantCreate`)
- Events: `{Target}{PastVerb}` — PascalCase, past tense (e.g., `TenantCreated`, NOT `CreateTenantEvent`)
- Anti-pattern: Never use `-Event` suffix, never use noun-first for commands

**Identity Scheme:**
- Platform tenant: `system` (configurable constant)
- Domain: `tenants`
- AggregateId for TenantAggregate: managed tenant ID (e.g., `acme-corp`)
- AggregateId for GlobalAdministratorAggregate: `global-administrators` (singleton)
- Actor ID format: `system:tenants:{aggregateId}` (via `AggregateIdentity` from EventStore.Contracts)

### Critical Anti-Patterns (DO NOT)

- **DO NOT** add `CorrelationId`, `UserId`, or `Timestamp` fields to command records — infrastructure metadata is added by `CommandEnvelope` in the MediatR pipeline (Story 2.4), not by domain commands
- **DO NOT** add `AggregateId` to event records — the aggregate ID is in `EventEnvelope.Metadata`, managed by EventStore infrastructure. Events carry `TenantId` (the managed tenant), which is different from `AggregateId` for GlobalAdmin events
- **DO NOT** add `GlobalAdministrator` to the `TenantRole` enum — GlobalAdmin is managed by a separate aggregate (`GlobalAdministratorAggregate`), not by tenant-level roles
- **DO NOT** use `class` instead of `record` for commands or events — records provide immutability, value equality, and positional deconstruction
- **DO NOT** add `[JsonPropertyName]` attributes — the default `System.Text.Json` camelCase serialization is correct; record property names map naturally
- **DO NOT** write individual test methods per event/command type — use reflection-driven parameterized tests so future types are automatically covered
- **DO NOT** add validation logic to command or event records — validation lives in FluentValidation validators (Server) and Handle methods (Server)

### Design Decisions

**UpdateTenant uses full-replacement semantics:** `UpdateTenant(TenantId, Name, Description?)` replaces all metadata fields. The caller always sends the full current values. There is no `PatchTenant` or nullable-field partial-update pattern. This is simpler and avoids null-vs-missing ambiguity.

**BootstrapGlobalAdmin reuses the GlobalAdministratorSet event:** 12 commands produce 11 event types because `BootstrapGlobalAdmin` and `SetGlobalAdministrator` both produce `GlobalAdministratorSet`. Bootstrap is just the first set operation with a different precondition check (reject if any admin already exists). No separate `GlobalAdminBootstrapped` event type is needed.

### Library & Framework Requirements

**Dependencies available (via ProjectReference to EventStore.Contracts):**
- `Hexalith.EventStore.Contracts.Events.IEventPayload` — marker interface for all events
- `Hexalith.EventStore.Contracts.Identity.AggregateIdentity` — identity tuple for aggregate addressing
- `Hexalith.EventStore.Contracts.Results.DomainResult` — command result wrapper (not needed in this story, but available)
- `System.Text.Json` — built-in, no additional NuGet needed

**No additional NuGet packages required for this story.** The Contracts project only depends on EventStore.Contracts (already in .csproj from Story 1.1).

### File Structure Requirements

All files under `src/Hexalith.Tenants.Contracts/`:

```
src/Hexalith.Tenants.Contracts/
├── Commands/
│   ├── CreateTenant.cs
│   ├── UpdateTenant.cs
│   ├── DisableTenant.cs
│   ├── EnableTenant.cs
│   ├── AddUserToTenant.cs
│   ├── RemoveUserFromTenant.cs
│   ├── ChangeUserRole.cs
│   ├── SetTenantConfiguration.cs
│   ├── RemoveTenantConfiguration.cs
│   ├── BootstrapGlobalAdmin.cs
│   ├── SetGlobalAdministrator.cs
│   └── RemoveGlobalAdministrator.cs
├── Events/
│   ├── TenantCreated.cs
│   ├── TenantUpdated.cs
│   ├── TenantDisabled.cs
│   ├── TenantEnabled.cs
│   ├── UserAddedToTenant.cs
│   ├── UserRemovedFromTenant.cs
│   ├── UserRoleChanged.cs
│   ├── TenantConfigurationSet.cs
│   ├── TenantConfigurationRemoved.cs
│   ├── GlobalAdministratorSet.cs
│   └── GlobalAdministratorRemoved.cs
├── Enums/
│   ├── TenantRole.cs
│   └── TenantStatus.cs
└── Identity/
    └── TenantIdentity.cs
```

Test files under `tests/Hexalith.Tenants.Contracts.Tests/`:

```
tests/Hexalith.Tenants.Contracts.Tests/
├── EventSerializationTests.cs
└── NamingConventionTests.cs
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed:**

1. **EventSerializationTests** (AC: #5):
   - Use reflection to discover all types implementing `IEventPayload` in `Hexalith.Tenants.Contracts` assembly
   - For each event type: create instance with **non-default, distinguishable test data** — e.g., `"tenant-abc"` not `""`, `DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")` not `default`, `TenantRole.TenantContributor` not the first enum value. Default values can mask serialization failures (a round-trip that silently drops a field and returns `default` would still "pass" with default test data)
   - Serialize to JSON via `JsonSerializer.Serialize`, deserialize via `JsonSerializer.Deserialize<T>`, assert deep equality with Shouldly
   - Use `[Theory]` with `[MemberData]` supplying all event types for parameterized testing — DO NOT write individual `[Fact]` methods per event type
   - Ensures future schema changes that break serialization are caught immediately

2. **NamingConventionTests** (AC: #6, #7):
   - Scan `Hexalith.Tenants.Contracts.Commands` namespace for all public record types
   - Verify each command name starts with a recognized verb from a **whitelist**: `Create`, `Update`, `Disable`, `Enable`, `Add`, `Remove`, `Change`, `Set`, `Bootstrap`. The test MUST FAIL for any command using an unrecognized verb prefix — this catches naming drift
   - Scan all types implementing `IEventPayload` in `Hexalith.Tenants.Contracts.Events` namespace
   - Verify each event name ends with a recognized past-tense suffix from a **whitelist**: `Created`, `Updated`, `Disabled`, `Enabled`, `Added`, `Removed`, `Changed`, `Set`
   - Verify every event type has a `TenantId` property of type `string` (AC: #7)

**Test framework:** xUnit 2.9.3, Shouldly 4.3.0 (already in Contracts.Tests .csproj from Story 1.1)

### Previous Story Intelligence

**Story 1.1 (done) — Solution Structure & Build Configuration:**
- All 15 project shells exist and compile
- Contracts project has `ProjectReference` to EventStore.Contracts — NO source files yet
- Contracts.Tests project exists with xUnit, Shouldly, coverlet — NO test files yet
- Root `.editorconfig` enforces: file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ImplicitUsings` and `Nullable` enabled globally
- `Directory.Packages.props` has all NuGet versions centralized

**Learnings from Story 1.1:**
- SDK version 10.0.103 confirmed available
- Empty library projects compile without placeholder files (no stub needed)
- ServiceDefaults required explicit `using Microsoft.Extensions.Hosting;` for `IHostApplicationBuilder` — implicit usings differ between SDK types. Be aware of similar issues in Contracts (unlikely since records have no framework dependencies)
- EventStore submodule is initialized and its projects build correctly

### Git Intelligence

Recent commits are all project setup and BMAD planning — no domain code exists yet. This story creates the first domain types.

### Project Structure Notes

- Alignment with unified project structure: All types in `src/Hexalith.Tenants.Contracts/` with subdirectories matching namespace segments (`Commands/`, `Events/`, `Enums/`, `Identity/`)
- No conflicts or variances detected — this is greenfield code in an empty project shell

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.1] — Acceptance criteria, story definition, complete command/event lists
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules] — Naming conventions, type location rules, event payload structure
- [Source: _bmad-output/planning-artifacts/architecture.md#Aggregate Boundaries] — TenantAggregate vs GlobalAdministratorAggregate boundary, state design
- [Source: _bmad-output/planning-artifacts/architecture.md#Identity Mapping] — `system:tenants:{aggregateId}` scheme, platform tenant context
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication Patterns] — Event record patterns, timestamp conventions, TenantId field requirement
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IEventPayload.cs] — Marker interface for events
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs] — Identity tuple record with validation
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-configuration.md] — Previous story learnings, project structure established

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### Change Log

### File List
