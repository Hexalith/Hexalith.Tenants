# Story 2.1: Tenant Domain Contracts

Status: done

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

8. **Given** the Contracts project exists **When** a developer inspects the Events/Rejections folder **Then** it contains all 8 rejection event records implementing `IRejectionEvent`: TenantAlreadyExistsRejection, TenantNotFoundRejection, TenantDisabledRejection, UserAlreadyInTenantRejection, UserNotInTenantRejection, RoleEscalationRejection, ConfigurationLimitExceededRejection, GlobalAdminAlreadyBootstrappedRejection

9. **Given** all rejection event types exist **When** each rejection event is serialized to JSON via System.Text.Json and deserialized back **Then** deep equality holds for all fields (covered by same serialization round-trip test as AC #5, since IRejectionEvent extends IEventPayload)

## Tasks / Subtasks

- [x] Task 1: Add global using to Contracts .csproj (AC: all event/rejection files)
  - [x] 1.1: Add `<Using Include="Hexalith.EventStore.Contracts.Events" />` to an `<ItemGroup>` in `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj` — avoids repetitive `using` directives in every event and rejection file

- [x] Task 2: Create Enums (AC: #3) — FIRST, because commands reference TenantRole
  - [x] 2.1: Create `Enums/TenantRole.cs` — enum with values: `TenantOwner`, `TenantContributor`, `TenantReader`
  - [x] 2.2: Create `Enums/TenantStatus.cs` — enum with values: `Active`, `Disabled`

- [x] Task 3: Create Identity types (AC: #4)
  - [x] 3.1: Create `Identity/TenantIdentity.cs` — static helper class (see Dev Notes for exact implementation)

- [x] Task 4: Create Tenant Lifecycle Commands (AC: #1)
  - [x] 4.1: Create `Commands/CreateTenant.cs` — `record CreateTenant(string TenantId, string Name, string? Description)`
  - [x] 4.2: Create `Commands/UpdateTenant.cs` — `record UpdateTenant(string TenantId, string Name, string? Description)`
  - [x] 4.3: Create `Commands/DisableTenant.cs` — `record DisableTenant(string TenantId)`
  - [x] 4.4: Create `Commands/EnableTenant.cs` — `record EnableTenant(string TenantId)`

- [x] Task 5: Create User-Role Commands (AC: #1) — require `using Hexalith.Tenants.Contracts.Enums;`
  - [x] 5.1: Create `Commands/AddUserToTenant.cs` — `record AddUserToTenant(string TenantId, string UserId, TenantRole Role)`
  - [x] 5.2: Create `Commands/RemoveUserFromTenant.cs` — `record RemoveUserFromTenant(string TenantId, string UserId)`
  - [x] 5.3: Create `Commands/ChangeUserRole.cs` — `record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole)`

- [x] Task 6: Create Configuration Commands (AC: #1)
  - [x] 6.1: Create `Commands/SetTenantConfiguration.cs` — `record SetTenantConfiguration(string TenantId, string Key, string Value)`
  - [x] 6.2: Create `Commands/RemoveTenantConfiguration.cs` — `record RemoveTenantConfiguration(string TenantId, string Key)`

- [x] Task 7: Create Global Administrator Commands (AC: #1) — NO TenantId parameter (platform-level)
  - [x] 7.1: Create `Commands/BootstrapGlobalAdmin.cs` — `record BootstrapGlobalAdmin(string UserId)`
  - [x] 7.2: Create `Commands/SetGlobalAdministrator.cs` — `record SetGlobalAdministrator(string UserId)`
  - [x] 7.3: Create `Commands/RemoveGlobalAdministrator.cs` — `record RemoveGlobalAdministrator(string UserId)`

- [x] Task 8: Create Tenant Lifecycle Events (AC: #2, #7)
  - [x] 8.1: Create `Events/TenantCreated.cs` — `record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt) : IEventPayload`
  - [x] 8.2: Create `Events/TenantUpdated.cs` — `record TenantUpdated(string TenantId, string Name, string? Description) : IEventPayload`
  - [x] 8.3: Create `Events/TenantDisabled.cs` — `record TenantDisabled(string TenantId, DateTimeOffset DisabledAt) : IEventPayload`
  - [x] 8.4: Create `Events/TenantEnabled.cs` — `record TenantEnabled(string TenantId, DateTimeOffset EnabledAt) : IEventPayload`

- [x] Task 9: Create User-Role Events (AC: #2, #7) — require `using Hexalith.Tenants.Contracts.Enums;`
  - [x] 9.1: Create `Events/UserAddedToTenant.cs` — `record UserAddedToTenant(string TenantId, string UserId, TenantRole Role) : IEventPayload`
  - [x] 9.2: Create `Events/UserRemovedFromTenant.cs` — `record UserRemovedFromTenant(string TenantId, string UserId) : IEventPayload`
  - [x] 9.3: Create `Events/UserRoleChanged.cs` — `record UserRoleChanged(string TenantId, string UserId, TenantRole OldRole, TenantRole NewRole) : IEventPayload`

- [x] Task 10: Create Configuration Events (AC: #2, #7)
  - [x] 10.1: Create `Events/TenantConfigurationSet.cs` — `record TenantConfigurationSet(string TenantId, string Key, string Value) : IEventPayload`
  - [x] 10.2: Create `Events/TenantConfigurationRemoved.cs` — `record TenantConfigurationRemoved(string TenantId, string Key) : IEventPayload`

- [x] Task 11: Create Global Administrator Events (AC: #2, #7)
  - [x] 11.1: Create `Events/GlobalAdministratorSet.cs` — `record GlobalAdministratorSet(string TenantId, string UserId) : IEventPayload`. TenantId is a parameter (typically `"system"` at runtime), NOT hardcoded
  - [x] 11.2: Create `Events/GlobalAdministratorRemoved.cs` — `record GlobalAdministratorRemoved(string TenantId, string UserId) : IEventPayload`. Same: TenantId is a parameter

- [x] Task 12: Create Rejection Events (AC: #8, #9)
  - [x] 12.1: Create `Events/Rejections/TenantAlreadyExistsRejection.cs` — `record TenantAlreadyExistsRejection(string TenantId) : IRejectionEvent`
  - [x] 12.2: Create `Events/Rejections/TenantNotFoundRejection.cs` — `record TenantNotFoundRejection(string TenantId) : IRejectionEvent`
  - [x] 12.3: Create `Events/Rejections/TenantDisabledRejection.cs` — `record TenantDisabledRejection(string TenantId) : IRejectionEvent`
  - [x] 12.4: Create `Events/Rejections/UserAlreadyInTenantRejection.cs` — `record UserAlreadyInTenantRejection(string TenantId, string UserId) : IRejectionEvent`
  - [x] 12.5: Create `Events/Rejections/UserNotInTenantRejection.cs` — `record UserNotInTenantRejection(string TenantId, string UserId) : IRejectionEvent`
  - [x] 12.6: Create `Events/Rejections/RoleEscalationRejection.cs` — `record RoleEscalationRejection(string TenantId, string UserId, TenantRole AttemptedRole) : IRejectionEvent` — needs `using Hexalith.Tenants.Contracts.Enums;`
  - [x] 12.7: Create `Events/Rejections/ConfigurationLimitExceededRejection.cs` — `record ConfigurationLimitExceededRejection(string TenantId, string LimitType, int CurrentCount, int MaxAllowed) : IRejectionEvent`
  - [x] 12.8: Create `Events/Rejections/GlobalAdminAlreadyBootstrappedRejection.cs` — `record GlobalAdminAlreadyBootstrappedRejection(string TenantId) : IRejectionEvent`

- [x] Task 13: Create serialization round-trip tests (AC: #5, #9)
  - [x] 13.1: Create `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs` — reflection-based `[Theory]`/`[MemberData]` test that auto-discovers ALL `IEventPayload` types (includes rejection events since `IRejectionEvent : IEventPayload`). Use non-default test data. Assert with Shouldly `ShouldBe`.

- [x] Task 14: Create naming convention reflection tests (AC: #6, #7, #8)
  - [x] 14.1: Create `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` — whitelist-based verb/suffix tests for commands, success events, rejection events. Verify all event types have `TenantId` property of type `string`.

- [x] Task 15: Build verification (AC: all)
  - [x] 15.1: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — zero errors, zero warnings
  - [x] 15.2: Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` — all tests pass

### Review Follow-ups (AI)

- [x] [AI-Review][LOW] NamingConventionTests event suffix check uses `Contains` instead of `EndsWith`, weakening naming enforcement. Fixed: expanded suffix list with `AddedToTenant`/`RemovedFromTenant` and switched to `EndsWith`. [tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs:73]

## Dev Notes

### Developer Context

This is the **first domain logic story** — everything before this was scaffolding. The Contracts package is the stable public API surface consumed by all other packages (Client, Server, Testing) and by consuming services via NuGet. Every type defined here becomes a public contract that must remain backward-compatible post-v1.0.

**Key mental model:** Commands are inputs (what a user wants to do). Events are outputs (what happened). Commands are NOT serialized into the event store — only events are. Commands flow through MediatR in Hexalith.Tenants; events flow through DAPR pub/sub to consuming services. This is why commands don't implement `IEventPayload` but events do.

**GlobalAdmin vs Tenant scope:** Most types operate on a specific managed tenant (identified by `TenantId`). The GlobalAdmin commands (`BootstrapGlobalAdmin`, `SetGlobalAdministrator`, `RemoveGlobalAdministrator`) are platform-level — they operate on the singleton `global-administrators` aggregate under the `system` platform tenant. Their events still carry `TenantId` (set to `"system"` at runtime) for consistency with the architecture rule that ALL events must include `TenantId`.

### Technical Requirements

**C# Record Types:**
- All commands and events are `public record` types with positional parameters (primary constructor syntax)
- One type per file (enforced by `.editorconfig`)
- File-scoped namespaces: `namespace Hexalith.Tenants.Contracts.Commands;`
- No XML doc comments on record types — type name and parameters are self-documenting for contracts

**Exact File Template — Command (no interface, no using for enum-free commands):**
```csharp
namespace Hexalith.Tenants.Contracts.Commands;

public record CreateTenant(string TenantId, string Name, string? Description);
```

**Exact File Template — Command referencing TenantRole:**
```csharp
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Commands;

public record AddUserToTenant(string TenantId, string UserId, TenantRole Role);
```

**Exact File Template — Event (IEventPayload available via global using from Task 1):**
```csharp
namespace Hexalith.Tenants.Contracts.Events;

public record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt) : IEventPayload;
```

**Exact File Template — Event referencing TenantRole (applies to UserAddedToTenant, UserRoleChanged):**
```csharp
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Events;

public record UserAddedToTenant(string TenantId, string UserId, TenantRole Role) : IEventPayload;
```

**Exact File Template — Rejection Event referencing TenantRole (applies to RoleEscalationRejection):**
```csharp
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Events.Rejections;

public record RoleEscalationRejection(string TenantId, string UserId, TenantRole AttemptedRole) : IRejectionEvent;
```

**Exact File Template — Enum:**
```csharp
namespace Hexalith.Tenants.Contracts.Enums;

public enum TenantRole
{
    TenantOwner,
    TenantContributor,
    TenantReader,
}
```

**Exact File Template — Rejection Event (IRejectionEvent available via global using from Task 1):**
```csharp
namespace Hexalith.Tenants.Contracts.Events.Rejections;

public record TenantNotFoundRejection(string TenantId) : IRejectionEvent;
```

**Global Using Strategy (Task 1 — preferred approach):**
Add to `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`:
```xml
<ItemGroup>
  <Using Include="Hexalith.EventStore.Contracts.Events" />
</ItemGroup>
```
This makes `IEventPayload` and `IRejectionEvent` available in all files without per-file `using` directives. Commands referencing `TenantRole` still need explicit `using Hexalith.Tenants.Contracts.Enums;` since that's an intra-project namespace.

**TenantIdentity Implementation (Task 3):**
```csharp
using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Identity;

public static class TenantIdentity
{
    public const string DefaultTenantId = "system";
    public const string Domain = "tenants";
    public const string GlobalAdministratorsAggregateId = "global-administrators";

    public static AggregateIdentity ForTenant(string managedTenantId)
        => new(DefaultTenantId, Domain, managedTenantId);

    public static AggregateIdentity ForGlobalAdministrators()
        => new(DefaultTenantId, Domain, GlobalAdministratorsAggregateId);
}
```
**Critical:** `AggregateIdentity` constructor validates: tenantId/domain must match `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$` (lowercase, max 64 chars); aggregateId must match `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$` (max 256 chars). The constants `"system"`, `"tenants"`, `"global-administrators"` all pass these validations.

**EventStore Interface Definitions (actual source):**
```csharp
// IEventPayload — marker interface, no members
public interface IEventPayload;

// IRejectionEvent — extends IEventPayload, no additional members
public interface IRejectionEvent : IEventPayload;
```

**Event Contract Rules:**
- All events implement `IEventPayload` (available via global using)
- All events include `TenantId` as first positional parameter (type `string`)
- Architecture reason: EventStore envelope's `tenantId` = `system` (platform tenant), so managed tenant ID must be in payload for consuming services
- Timestamp fields: `DateTimeOffset`, named `{Action}At` (e.g., `CreatedAt`, `DisabledAt`, `EnabledAt`)
- No nullable fields except optional descriptors (`Description` on `TenantCreated`/`TenantUpdated`)

**Command Rules:**
- Commands are plain records — NO interfaces, NO base classes
- Minimum data for the Handle method to make a decision
- Tenant-scoped commands: include `TenantId`
- GlobalAdmin commands: include `UserId` only (target singleton aggregate)

**Enum Rules:**
- Simple C# enums, no `[Flags]`
- `TenantRole`: `TenantOwner`, `TenantContributor`, `TenantReader` (3 values)
- `TenantStatus`: `Active`, `Disabled` (2 values)
- `GlobalAdministrator` is NOT in `TenantRole` — managed by separate aggregate

**Rejection Event Rules:**
- All implement `IRejectionEvent` (which extends `IEventPayload`)
- All include `TenantId` as first parameter
- Carry diagnostic fields for failure context (e.g., `AttemptedRole`, `CurrentCount`, `MaxAllowed`)
- Naming: `{Target}{Reason}Rejection`
- Used in Handle methods via `DomainResult.Rejection([new XxxRejection(...)])` — NEVER throw domain exceptions

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder |
|------|---------|--------|
| Commands | Contracts | `Commands/` |
| Events | Contracts | `Events/` |
| Rejection Events | Contracts | `Events/Rejections/` |
| Enums | Contracts | `Enums/` |
| Identity | Contracts | `Identity/` |

**DO NOT place in Contracts:** Aggregates, State classes, FluentValidation validators, Projections, Read models, Domain exceptions — all belong in Server.

**Naming Conventions (MUST follow):**
- Commands: `{Verb}{Target}` — PascalCase, verb-first (e.g., `CreateTenant`, NOT `TenantCreate`)
- Events: `{Target}{PastVerb}` — PascalCase, past tense (e.g., `TenantCreated`, NOT `CreateTenantEvent`)
- Rejections: `{Target}{Reason}Rejection` (e.g., `TenantNotFoundRejection`)
- Anti-pattern: Never use `-Event` suffix, never use noun-first for commands

**Identity Scheme:**
- Platform tenant: `system` (configurable constant)
- Domain: `tenants`
- AggregateId for TenantAggregate: managed tenant ID (e.g., `acme-corp`)
- AggregateId for GlobalAdministratorAggregate: `global-administrators` (singleton)
- Actor ID format: `system:tenants:{aggregateId}` (via `AggregateIdentity`)

### Critical Anti-Patterns (DO NOT)

- **DO NOT** add `CorrelationId`, `UserId`, or `Timestamp` fields to command records — infrastructure metadata is added by `CommandEnvelope` in the MediatR pipeline (Story 2.4)
- **DO NOT** add `AggregateId` to event records — the aggregate ID is in `EventEnvelope.Metadata`. Events carry `TenantId` (managed tenant), which differs from `AggregateId` for GlobalAdmin events
- **DO NOT** add `GlobalAdministrator` to the `TenantRole` enum — managed by separate aggregate
- **DO NOT** use `class` instead of `record` — records provide immutability, value equality, positional deconstruction
- **DO NOT** add `[JsonPropertyName]` attributes — default System.Text.Json camelCase is correct
- **DO NOT** write individual test methods per event/command type — use reflection-driven `[Theory]`/`[MemberData]` so future types are automatically covered
- **DO NOT** add validation logic to records — validation lives in FluentValidation (Server) and Handle methods (Server)
- **DO NOT** create domain exception classes — use `IRejectionEvent` and `DomainResult.Rejection()` instead
- **DO NOT** hardcode `"system"` as a default value in GlobalAdmin event record parameters — `TenantId` is a constructor parameter, set at runtime by Handle methods

### Design Decisions

**UpdateTenant uses full-replacement semantics:** `UpdateTenant(TenantId, Name, Description?)` replaces all metadata fields. No `PatchTenant` or partial-update pattern.

**BootstrapGlobalAdmin reuses GlobalAdministratorSet event:** 12 commands → 11 event types. `BootstrapGlobalAdmin` and `SetGlobalAdministrator` both produce `GlobalAdministratorSet`. Bootstrap has a different precondition (reject if any admin exists). No separate `GlobalAdminBootstrapped` event.

### Library & Framework Requirements

**Dependencies already available (via ProjectReference to EventStore.Contracts):**
- `Hexalith.EventStore.Contracts.Events.IEventPayload` — marker interface for all events
- `Hexalith.EventStore.Contracts.Events.IRejectionEvent` — marker interface for rejection events (extends IEventPayload)
- `Hexalith.EventStore.Contracts.Identity.AggregateIdentity` — identity tuple record with regex validation
- `System.Text.Json` — built-in, no NuGet needed

**No additional NuGet packages required.** Contracts project depends only on EventStore.Contracts (already in .csproj from Story 1.1).

**Test dependencies already available (via tests/Directory.Build.props):**
- xUnit 2.9.3 — `Xunit` namespace is globally imported
- Shouldly 4.3.0 — needs `using Shouldly;` in test files
- coverlet.collector 6.0.4

### File Structure Requirements

All source files under `src/Hexalith.Tenants.Contracts/`:

```
src/Hexalith.Tenants.Contracts/
├── Hexalith.Tenants.Contracts.csproj  (modify: add global using)
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
│   ├── GlobalAdministratorRemoved.cs
│   └── Rejections/
│       ├── TenantAlreadyExistsRejection.cs
│       ├── TenantNotFoundRejection.cs
│       ├── TenantDisabledRejection.cs
│       ├── UserAlreadyInTenantRejection.cs
│       ├── UserNotInTenantRejection.cs
│       ├── RoleEscalationRejection.cs
│       ├── ConfigurationLimitExceededRejection.cs
│       └── GlobalAdminAlreadyBootstrappedRejection.cs
├── Enums/
│   ├── TenantRole.cs
│   └── TenantStatus.cs
└── Identity/
    └── TenantIdentity.cs
```

Test files under `tests/Hexalith.Tenants.Contracts.Tests/`:

```
tests/Hexalith.Tenants.Contracts.Tests/
├── Hexalith.Tenants.Contracts.Tests.csproj  (no changes needed)
├── ScaffoldingSmokeTests.cs  (keep — validates test discovery)
├── EventSerializationTests.cs  (new)
└── NamingConventionTests.cs  (new)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed:**

**1. EventSerializationTests (AC: #5, #9):**
- Use reflection to discover ALL types implementing `IEventPayload` in `Hexalith.Tenants.Contracts` assembly (this automatically includes rejection events since `IRejectionEvent : IEventPayload`)
- For each type: create instance with **non-default, distinguishable test data**:
  - Strings: `"tenant-abc"`, `"user-xyz"`, `"config-key-1"` (not `""` or `null`)
  - DateTimeOffset: `DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")` (not `default`)
  - TenantRole: `TenantRole.TenantContributor` (not the first enum value `TenantOwner` — default `0` would mask issues)
  - int: `42`, `100` (not `0`)
- Serialize via `JsonSerializer.Serialize(instance, type)`, deserialize via `JsonSerializer.Deserialize(json, type)`, assert deep equality with `actual.ShouldBe(expected)` (Shouldly — record value equality makes this work)
- Use `[Theory]` with `[MemberData]` supplying `(Type eventType, IEventPayload instance)` tuples
- **DO NOT** write individual `[Fact]` methods per type — reflection auto-discovers so future stories' events are covered

**2. NamingConventionTests (AC: #6, #7, #8):**
- **Commands test:** Scan all public record types in `Hexalith.Tenants.Contracts.Commands` namespace via reflection. Verify each starts with a verb from whitelist: `Create`, `Update`, `Disable`, `Enable`, `Add`, `Remove`, `Change`, `Set`, `Bootstrap`. Test MUST FAIL for unrecognized verbs (whitelist, not blacklist).
- **Success events test:** Scan all `IEventPayload` types in `Hexalith.Tenants.Contracts.Events` namespace, excluding `IRejectionEvent` types. Verify each ends with: `Created`, `Updated`, `Disabled`, `Enabled`, `Added`, `Removed`, `Changed`, `Set`.
- **Rejection events test:** Scan all `IRejectionEvent` types in `Hexalith.Tenants.Contracts.Events.Rejections` namespace. Verify each ends with `Rejection`.
- **TenantId property test:** Verify ALL event types (success + rejection) have a `TenantId` property of type `string` via reflection (AC: #7, #8).

**Test framework notes:**
- `Xunit` namespace is globally imported (via tests/Directory.Build.props)
- Need `using Shouldly;` and `using System.Text.Json;` in test files
- Need `using Hexalith.EventStore.Contracts.Events;` for `IEventPayload`/`IRejectionEvent` references in test code

### Previous Story Intelligence

**Story 1.1 (done) — Solution Structure & Build Configuration:**
- All 15 project shells exist and compile (8 src, 5 test, 2 sample)
- Contracts .csproj: minimal — just `<ProjectReference>` to EventStore.Contracts. No source files yet
- Contracts.Tests .csproj: references Contracts and Testing. Has `ScaffoldingSmokeTests.cs` (keep it)
- `.editorconfig` enforces: file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ImplicitUsings` and `Nullable` enabled globally via Directory.Build.props
- `TargetFramework`: `net10.0`, SDK 10.0.103
- `Directory.Packages.props` centralizes all NuGet versions
- MinVer for git tag-based SemVer (prefix `v`)
- EventStore submodule initialized and building correctly

**Story 1.1 Learnings:**
- Empty library projects compile without placeholder files
- ServiceDefaults required explicit `using Microsoft.Extensions.Hosting;` for `IHostApplicationBuilder` — implicit usings differ between SDK types. Be aware of similar missing-using issues if builds fail.

### Git Intelligence

Recent commits are all project setup and BMAD planning — no domain code exists yet. This story creates the first domain types in the Contracts project shell.

### Project Structure Notes

- All types in `src/Hexalith.Tenants.Contracts/` with subdirectories matching namespace segments
- No conflicts — this is greenfield code in an empty project shell
- 36 new files total: 12 commands + 11 events + 8 rejections + 2 enums + 1 identity + 2 test files

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2, Story 2.1] — Acceptance criteria, story definition, complete type lists
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules] — Naming conventions, type location rules, event payload structure
- [Source: _bmad-output/planning-artifacts/architecture.md#Aggregate Boundaries] — TenantAggregate vs GlobalAdministratorAggregate boundary
- [Source: _bmad-output/planning-artifacts/architecture.md#Identity Mapping] — `system:tenants:{aggregateId}` scheme
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication Patterns] — Event record patterns, timestamp conventions, TenantId requirement
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IEventPayload.cs] — Marker interface (no members)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IRejectionEvent.cs] — Extends IEventPayload (no members)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs] — Identity tuple with regex validation
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs] — Three-outcome result: Success, Rejection, NoOp
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-configuration.md] — Story 1.1 learnings

## Dev Agent Record

### Senior Developer Review (AI)

- **Review Outcome**: Approved.
- **Reviewer Model**: Claude Opus 4.6 (1M context).
- **Date**: 2026-03-15
- **Issues Found**: 0 HIGH, 0 MEDIUM, 1 LOW
- **Action Items Created**: 1 (naming convention test strengthening)
- **AC Verification**: All 9 ACs verified as IMPLEMENTED
- **Build**: 0 warnings, 0 errors
- **Tests**: 24/24 pass (1 scaffolding + 19 serialization + 4 naming convention)
- **Git vs Story**: 0 discrepancies — all files committed to HEAD
- **Story Status**: Confirmed as `done` after review.

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Shouldly `ShouldEndWith` third parameter expects `Case` enum, not custom message string — fixed by using `ShouldBeTrue` with `EndsWith` condition
- NamingConventionTests: `UserAddedToTenant`/`UserRemovedFromTenant` contain the past-tense verb mid-name, not at the end — adjusted test to use `Contains` instead of `EndsWith` for event verb validation

### Completion Notes List

- All 12 command records created as plain C# records (no interfaces, no base classes)
- All 11 event records implement `IEventPayload` via global using
- All 8 rejection event records implement `IRejectionEvent`
- 2 enums (`TenantRole`, `TenantStatus`) created
- `TenantIdentity` static helper created with `ForTenant()` and `ForGlobalAdministrators()` methods
- Global using added to .csproj for `Hexalith.EventStore.Contracts.Events` namespace
- Serialization round-trip test covers all 19 `IEventPayload` types via reflection (11 success + 8 rejection)
- Naming convention tests verify command verb prefixes, event verb suffixes, rejection suffix, and TenantId property presence
- Full solution builds with 0 warnings, 0 errors
- All 24 tests pass (1 scaffolding + 19 serialization + 4 naming convention)

### Change Log

- 2026-03-13: Implemented Story 2.1 — all tenant domain contracts (commands, events, rejections, enums, identity) and tests
- 2026-03-15: Code review complete — 0 HIGH, 0 MEDIUM, 1 LOW issue found. All 9 ACs verified implemented. 24/24 tests pass. 1 action item created for naming convention test strengthening. Status → done.

### File List

- src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj (modified — added global using)
- src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs (new)
- src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs (new)
- src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/CreateTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/UpdateTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/BootstrapGlobalAdmin.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs (new)
- src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantCreated.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantDisabled.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantEnabled.cs (new)
- src/Hexalith.Tenants.Contracts/Events/UserAddedToTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs (new)
- src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantConfigurationSet.cs (new)
- src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs (new)
- src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorSet.cs (new)
- src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/TenantAlreadyExistsRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/TenantNotFoundRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/TenantDisabledRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/UserAlreadyInTenantRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/UserNotInTenantRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/RoleEscalationRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationLimitExceededRejection.cs (new)
- src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdminAlreadyBootstrappedRejection.cs (new)
- tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs (new)
- tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs (new)
