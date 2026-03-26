# Story 3.2: Role Behavior Enforcement

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer integrating with the tenant system,
I want role-based authorization enforced at the domain level so that TenantReader, TenantContributor, and TenantOwner permissions are consistently applied,
So that tenant security boundaries are guaranteed by the aggregate regardless of the calling context.

## Acceptance Criteria

1. **Given** a user with TenantReader role in a tenant
   **When** a state-changing command (AddUserToTenant, ChangeUserRole, RemoveUserFromTenant, UpdateTenant) is processed with that user as the actor
   **Then** the command is rejected with InsufficientPermissionsRejection

2. **Given** a user with TenantContributor role in a tenant
   **When** a user-role management command (AddUserToTenant, ChangeUserRole, RemoveUserFromTenant) is processed with that user as the actor
   **Then** the command is rejected with InsufficientPermissionsRejection (Contributor cannot manage users)

3. **Given** a user with TenantOwner role in a tenant
   **When** a user-role management command is processed with that user as the actor
   **Then** the command succeeds (Owner has full tenant management capabilities)

4. **Given** a user with TenantContributor role in a tenant
   **When** an UpdateTenant command is processed with that user as the actor
   **Then** the command succeeds (Contributor can execute domain commands)

5. **Given** an actor who is not a member of the tenant (not in state.Users)
   **When** any state-changing command targeting that tenant is processed
   **Then** the command is rejected with InsufficientPermissionsRejection (unless GlobalAdmin bypass applies)

6. **Given** a GlobalAdministrator (identified by CommandEnvelope.Extensions["actor:globalAdmin"] = "true")
   **When** any tenant command is processed with that user as the actor
   **Then** the command succeeds regardless of per-tenant role assignment

7. **Given** all role behavior enforcement paths
   **When** tested as Tier 1 unit tests
   **Then** 100% branch coverage is achieved on role authorization logic in Handle methods

## Tasks / Subtasks

- [x] Task 0: Extend EventStore Handle method discovery to support CommandEnvelope parameter (BLOCKING)
  - [x] 0.1: Modify `EventStoreAggregate<TState>.DiscoverHandleMethods()` to accept 2-param `Handle(Command, State?)` OR 3-param `Handle(Command, State?, CommandEnvelope)` methods
  - [x] 0.2: Modify `EventStoreAggregate<TState>.DispatchCommandAsync()` to pass `CommandEnvelope` as 3rd argument when the discovered Handle method has 3 parameters
  - [x] 0.3: Add unit test in `Hexalith.EventStore.Client.Tests` verifying 3-param Handle methods are discovered and invoked correctly (backward-compatible: existing 2-param tests still pass)
  - [x] 0.4: Check if `DomainProcessorBase<TState>` (at `Hexalith.EventStore/src/Hexalith.EventStore.Client/Handlers/DomainProcessorBase.cs`) also needs 3-param support — it has its own `HandleAsync(CommandEnvelope, TState?)` dispatch. If `InMemoryTenantService` extends this instead of `EventStoreAggregate`, 3-param Handle methods won't be discovered. Apply the same extension if needed
  - [x] 0.5: Verify no existing EventStore tests directly construct `HandleMethodInfo` (it's `private sealed`, so unlikely — but verify before adding `HasEnvelope` field)
  - [x] 0.6: Verify EventStore solution builds: `dotnet build Hexalith.EventStore.slnx --configuration Release`
  - [x] 0.7: Commit EventStore submodule changes, then in the parent repo run `git add Hexalith.EventStore` to update the submodule pointer. Verify with `git submodule status` — the pointer must reference your new commit

- [x] Task 1: Create InsufficientPermissionsRejection contract type (AC: #1-#5)
  - [x] 1.1: Create `src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs`
  - [x] 1.2: Verify naming convention test in Contracts.Tests auto-discovers the new rejection
  - [x] 1.3: Verify serialization round-trip test auto-discovers the new rejection
  - [x] 1.4: Build verification: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Add RBAC checks to existing Handle methods in TenantAggregate (AC: #1-#6)
  - [x] 2.1: Convert `Handle(AddUserToTenant, TenantState?)` to 3-param `Handle(AddUserToTenant, TenantState?, CommandEnvelope)` — add Owner-only RBAC check after disabled guard
  - [x] 2.2: Convert `Handle(RemoveUserFromTenant, TenantState?)` to 3-param — add Owner-only RBAC check
  - [x] 2.3: Convert `Handle(ChangeUserRole, TenantState?)` to 3-param — add Owner-only RBAC check
  - [x] 2.4: Convert `Handle(UpdateTenant, TenantState?)` to 3-param — add Contributor-or-higher RBAC check
  - [x] 2.5: Add private static helpers: `IsAuthorized(TenantState, string actorUserId, TenantRole minimumRole)`, `MeetsMinimumRole(TenantRole actorRole, TenantRole minimumRole)` (explicit switch, NOT integer comparison), `IsGlobalAdmin(CommandEnvelope envelope)`
  - [x] 2.6: Leave `Handle(CreateTenant, ...)`, `Handle(DisableTenant, ...)`, `Handle(EnableTenant, ...)` as 2-param (RBAC for these is a Layer 1 concern — GlobalAdmin only, enforced by JWT AuthorizationBehavior)
  - [x] 2.7: Verify SEC-4 extension sanitization: check that `CommandsController.Submit()` strips client-provided extensions before creating the CommandEnvelope. If not implemented yet, add `// SECURITY: actor:globalAdmin extension MUST be server-populated only (SEC-4). Verify CommandsController strips client extensions.` comment near `IsGlobalAdmin` method
  - [x] 2.8: Build verification: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create RBAC unit tests (AC: #7)
  - [x] 3.1: Update `CreateCommand<T>` helper in TenantAggregateTests.cs to accept `actorUserId` and `isGlobalAdmin` parameters
  - [x] 3.2: Add RBAC test methods for AddUserToTenant (Reader→rejected, Contributor→rejected, Owner→success, GlobalAdmin→success, actor-not-member→rejected)
  - [x] 3.3: Add RBAC test methods for RemoveUserFromTenant (Reader→rejected, Contributor→rejected, Owner→success, GlobalAdmin→success)
  - [x] 3.4: Add RBAC test methods for ChangeUserRole (Reader→rejected, Contributor→rejected, Owner→success, GlobalAdmin→success)
  - [x] 3.5: Add RBAC test methods for UpdateTenant (Reader→rejected, Contributor→success, Owner→success, GlobalAdmin→success)
  - [x] 3.6: Verify existing non-RBAC tests still pass (they use "test-user" which is not in state.Users — need to either add test-user to state OR use GlobalAdmin bypass)
  - [x] 3.7: Add enum ordinal regression test: assert `TenantOwner < TenantContributor < TenantReader` AND `Enum.GetValues<TenantRole>().Length == 3` to guard against enum reordering or unexpected new values
  - [x] 3.8: Add empty-tenant bootstrap test: AddUserToTenant on active tenant with empty Users dict by a non-member, non-GlobalAdmin actor → should SUCCEED (first user bootstrap). Then verify a subsequent AddUserToTenant by the now-Reader user is rejected (RBAC now active)
  - [x] 3.9: Add self-removal test (owner-user removes themselves) and self-demotion test (owner-user changes own role to Reader) — both should succeed (allowed for MVP)
  - [x] 3.10: Add 3-param discovery guard test: use reflection to assert TenantAggregate exposes at least one Handle method with 3 parameters (Command, State?, CommandEnvelope). If EventStore 3-param extension is accidentally reverted, this test fails immediately
  - [x] 3.11: Investigate conformance test impact — if `InMemoryTenantService` in `Testing` uses its own dispatch mechanism, it may also need RBAC-aware dispatch. Check `Testing.Tests` conformance test. If impacted, document as follow-up (Story 3.2 scope is TenantAggregate only)
  - [x] 3.12: Run all tests: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [x] Task 4: Build verification (all ACs)
  - [x] 4.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [x] 4.2: `dotnet test` all test projects — all pass, no regressions

## Dev Notes

### Critical Design Challenge: Actor Identity in Handle Methods

**Problem:** The story requires Handle methods to enforce RBAC (check WHO is executing the command). Currently, Handle methods receive only `(Command, TenantState?)`. The actor's identity lives in `CommandEnvelope.UserId`, which the framework does NOT pass to Handle methods.

**Root cause:** `EventStoreAggregate<TState>.DiscoverHandleMethods()` in `Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs:76` strictly requires `parameters.Length != 2`. Handle methods with 3 parameters are silently skipped.

**Recommended solution (Task 0):** Extend the EventStore framework to support 3-parameter Handle methods: `Handle(Command, State?, CommandEnvelope)`. This is a minimal, backward-compatible change:

```csharp
// In DiscoverHandleMethods (EventStoreAggregate.cs, ~line 76):
// BEFORE:
if (parameters.Length != 2) { continue; }

// AFTER:
if (parameters.Length < 2 || parameters.Length > 3) { continue; }
bool hasEnvelope = parameters.Length == 3
    && parameters[2].ParameterType == typeof(CommandEnvelope);
if (parameters.Length == 3 && !hasEnvelope) { continue; }
```

```csharp
// In DispatchCommandAsync (EventStoreAggregate.cs, ~line 118):
// BEFORE:
object? result = handleInfo.Method.Invoke(
    handleInfo.IsStatic ? null : this, [commandPayload, state]);

// AFTER:
object?[] args = handleInfo.HasEnvelope
    ? [commandPayload, state, command]
    : [commandPayload, state];
object? result = handleInfo.Method.Invoke(
    handleInfo.IsStatic ? null : this, args);
```

```csharp
// HandleMethodInfo record — add HasEnvelope field:
private sealed record HandleMethodInfo(
    MethodInfo Method,
    Type CommandType,
    bool IsAsync,
    bool IsStatic,
    bool HasEnvelope);
```

**Why not alternatives:**
- Adding `ActorUserId` to command records: Couples pipeline to domain command structure; client-submitted payloads could forge actor identity; requires pipeline to deserialize/re-serialize payloads
- Using `AsyncLocal<CommandContext>`: Handle methods are static — no access to scoped state; fragile and hard to test
- Overriding ProcessAsync: `DispatchCommandAsync` is private, cannot be intercepted

### Permission Matrix

| Command | Minimum Role | Check Level | Handle Params |
|---------|-------------|-------------|---------------|
| UpdateTenant | TenantContributor | Layer 2 (Handle) | 3-param |
| AddUserToTenant | TenantOwner | Layer 2 (Handle) | 3-param |
| RemoveUserFromTenant | TenantOwner | Layer 2 (Handle) | 3-param |
| ChangeUserRole | TenantOwner | Layer 2 (Handle) | 3-param |
| CreateTenant | GlobalAdmin | Layer 1 (JWT) | 2-param (unchanged) |
| DisableTenant | GlobalAdmin | Layer 1 (JWT) | 2-param (unchanged) |
| EnableTenant | GlobalAdmin | Layer 1 (JWT) | 2-param (unchanged) |

**Story 3.3 commands** (SetTenantConfiguration, RemoveTenantConfiguration) will also need TenantOwner RBAC — the pattern established here applies directly.

### TenantRole Hierarchy and Permission Check

```csharp
public enum TenantRole
{
    TenantOwner,       // 0 — highest privilege
    TenantContributor, // 1
    TenantReader,      // 2 — lowest privilege
}
```

**Use an explicit hierarchy check, NOT integer comparison.** While enum ordinals happen to order correctly now, relying on `actorRole <= minimumRole` is fragile — adding a role like `TenantAdmin = 1` between Owner and Contributor would silently break all RBAC checks. Instead, use an explicit method:

```csharp
/// <summary>
/// Checks if the actor's role meets or exceeds the minimum required role.
/// Uses explicit hierarchy to avoid fragile enum ordinal dependency.
/// Default deny: unknown roles are rejected. Update this method when adding new TenantRole values.
/// </summary>
private static bool MeetsMinimumRole(TenantRole actorRole, TenantRole minimumRole)
    => minimumRole switch
    {
        TenantRole.TenantReader => true, // all roles meet Reader minimum
        TenantRole.TenantContributor => actorRole is TenantRole.TenantContributor or TenantRole.TenantOwner,
        TenantRole.TenantOwner => actorRole is TenantRole.TenantOwner,
        _ => false, // deny unknown roles — security default
    };
```

**Add a regression test** in `TenantAggregateTests.cs` or `Contracts.Tests` to guard the hierarchy assumption:
```csharp
[Fact]
public void TenantRole_ordinal_values_maintain_privilege_hierarchy()
{
    // Guard: if someone reorders the enum, this test catches it
    ((int)TenantRole.TenantOwner).ShouldBeLessThan((int)TenantRole.TenantContributor);
    ((int)TenantRole.TenantContributor).ShouldBeLessThan((int)TenantRole.TenantReader);
}
```

### GlobalAdmin Bypass Mechanism

**Problem:** GlobalAdministrator status is tracked in `GlobalAdministratorAggregate` (separate aggregate). `TenantState` does not know who is a GlobalAdmin. Handle methods cannot query other aggregates.

**Solution:** Use `CommandEnvelope.Extensions` dictionary. The MediatR `AuthorizationBehavior` (or a new behavior) checks the actor's GlobalAdmin status and adds `extensions["actor:globalAdmin"] = "true"` before the command reaches the aggregate actor.

**Handle method pattern:**
```csharp
private static bool IsGlobalAdmin(CommandEnvelope envelope)
    => envelope.Extensions?.TryGetValue("actor:globalAdmin", out string? value) == true
       && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
```

**Important:** The `AuthorizationBehavior` or `CommandsController` MUST set this extension server-side from JWT claims or GlobalAdmin projection. The controller already sanitizes extension metadata (SEC-4 per architecture), so client-provided extensions are stripped. The "actor:globalAdmin" extension is trusted because it's server-populated.

**For this story's tests:** The `CreateCommand<T>` helper passes `Extensions` to CommandEnvelope. Tests can pass `isGlobalAdmin: true` to set the extension, simulating the pipeline behavior.

**Production status of GlobalAdmin bypass:** Until a pipeline behavior (MediatR or CommandsController) actually populates the `actor:globalAdmin` extension from JWT claims or GlobalAdmin projection, GlobalAdmin commands that reach TenantAggregate Handle methods WILL be rejected at Layer 2 if the GlobalAdmin has no per-tenant role.

**Impact on GlobalAdmin cross-tenant operations:**
- **Unaffected (2-param, no RBAC):** CreateTenant, DisableTenant, EnableTenant — these work for GlobalAdmin today
- **BLOCKED until pipeline wiring:** AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, UpdateTenant — rejected if GlobalAdmin has no per-tenant role in the target tenant
- **No workaround exists** for a GlobalAdmin who needs to manage users in a tenant they don't belong to. The GlobalAdmin must first be added to that tenant as TenantOwner (but AddUserToTenant itself requires Owner role — catch-22 unless another Owner exists)
- **Pipeline wiring for the `actor:globalAdmin` extension is a prerequisite for PRD Journey 6 (security incident response)** where a GlobalAdmin must revoke access to a compromised tenant. Consider implementing as a follow-on story before Epic 4 if GlobalAdmin cross-tenant operations are needed

### Empty Tenant Bootstrap Problem (CRITICAL — Elicitation Finding)

**Problem:** After `CreateTenant`, `state.Users` is empty. The first `AddUserToTenant` command hits the RBAC check: `IsAuthorized(state, envelope.UserId, TenantOwner)` returns `false` because no one is in Users yet. Without GlobalAdmin bypass (extension not wired in production), **new tenants are permanently empty — no user can ever be added.**

**Recommended solution: Allow first AddUserToTenant when state.Users is empty.**

Add a special case in the Handle method RBAC check:

```csharp
// RBAC: Owner only (skip if GlobalAdmin OR first user bootstrap)
_ when !IsGlobalAdmin(envelope)
    && state.Users.Count > 0  // Skip RBAC for empty tenant (first user bootstrap)
    && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
    => DomainResult.Rejection([new InsufficientPermissionsRejection(...)]),
```

**Security analysis:** The `state.Users.Count > 0` check means the first AddUserToTenant to an empty tenant succeeds regardless of caller role. Is this a security risk?
- **No** — CreateTenant is a Layer 1 (GlobalAdmin JWT) operation. Only a GlobalAdmin can create a tenant. The GlobalAdmin then adds the first Owner. If a non-GlobalAdmin somehow gets past Layer 1 and sends AddUserToTenant to the empty tenant, they'd need to know the TenantId. The actor model serializes commands, so there's no race condition.
- The first user added becomes the bootstrap Owner. After that, normal RBAC applies.
- This matches the natural flow: GlobalAdmin creates tenant → immediately adds first Owner → Owner manages subsequent users.

**Alternative considered:** Require GlobalAdmin bypass as hard prerequisite. Rejected because it creates a hard dependency on pipeline wiring that doesn't exist yet, making new tenants unusable until Epic 4+.

**Test case to add:**

| # | Command | Actor | State | Expected | AC |
|---|---------|-------|-------|----------|-----|
| R20 | AddUserToTenant | any-user (no role) | Active tenant, Users empty | Success: UserAddedToTenant (bootstrap) | #5 exception |

**Apply this special case to AddUserToTenant ONLY.** RemoveUserFromTenant, ChangeUserRole, and UpdateTenant on an empty tenant are nonsensical (no users to remove/change, and UpdateTenant by a non-member is still rejected).

### Self-Removal and Self-Demotion Policy (Party Mode Review Finding)

**Allow both for MVP.** An Owner can remove themselves from a tenant or demote their own role. The PRD (FR7, FR33) does not explicitly forbid self-removal or self-demotion — these are valid administrative actions (e.g., an owner stepping down).

**Edge case: Last TenantOwner removes themselves.** After self-removal, no user has Owner privileges — the tenant becomes unmanageable (no one can add users or change roles). This is a valid concern but is **out of scope for this story**. Track as a follow-up enhancement: "Prevent removing or demoting the last TenantOwner" (requires a count check: `state.Users.Count(u => u.Value == TenantRole.TenantOwner) > 1`). A GlobalAdmin can always recover by adding a new Owner.

### RBAC Check Placement in Handle Methods

RBAC check goes AFTER null/disabled guards, BEFORE domain logic:

```csharp
public static DomainResult Handle(AddUserToTenant command, TenantState? state, CommandEnvelope envelope)
{
    ArgumentNullException.ThrowIfNull(command);
    ArgumentNullException.ThrowIfNull(envelope);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        // RBAC: Owner only (skip if GlobalAdmin OR first user bootstrap on empty tenant)
        _ when !IsGlobalAdmin(envelope)
            && state.Users.Count > 0
            && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
            => DomainResult.Rejection([new InsufficientPermissionsRejection(
                command.TenantId, envelope.UserId,
                state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                nameof(AddUserToTenant))]),
        // Existing domain checks (unchanged)
        _ when !Enum.IsDefined(command.Role)
            => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.Role)]),
        _ when state.Users.ContainsKey(command.UserId)
            => DomainResult.Rejection([new UserAlreadyInTenantRejection(command.TenantId, command.UserId)]),
        _ => DomainResult.Success([new UserAddedToTenant(command.TenantId, command.UserId, command.Role)]),
    };
}

private static bool IsAuthorized(TenantState state, string actorUserId, TenantRole minimumRole)
    => state.Users.TryGetValue(actorUserId, out TenantRole actorRole) && MeetsMinimumRole(actorRole, minimumRole);

// Default deny: unknown roles are rejected. Update this method when adding new TenantRole values.
private static bool MeetsMinimumRole(TenantRole actorRole, TenantRole minimumRole)
    => minimumRole switch
    {
        TenantRole.TenantReader => true,
        TenantRole.TenantContributor => actorRole is TenantRole.TenantContributor or TenantRole.TenantOwner,
        TenantRole.TenantOwner => actorRole is TenantRole.TenantOwner,
        _ => false,
    };

// SECURITY: "actor:globalAdmin" extension MUST be server-populated only (SEC-4).
// CommandsController strips client-provided extensions before creating CommandEnvelope.
// If SEC-4 sanitization is not yet implemented, this is an accepted risk — document as TODO.
private static bool IsGlobalAdmin(CommandEnvelope envelope)
    => envelope.Extensions?.TryGetValue("actor:globalAdmin", out string? value) == true
       && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
```

**Extension key note:** The key `"actor:globalAdmin"` is a convention. Consider namespacing to `"hexalith:actor:globalAdmin"` if other extensions are likely. For MVP, `"actor:globalAdmin"` is sufficient — just be consistent across pipeline and Handle methods. Define the key as a `const string` in TenantAggregate to avoid magic string duplication.

### InsufficientPermissionsRejection Contract

```csharp
// src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Events.Rejections;

public record InsufficientPermissionsRejection(
    string TenantId,
    string ActorUserId,
    TenantRole? ActorRole,
    string CommandName) : IRejectionEvent;
```

- `ActorRole` is nullable — null when actor is not a member of the tenant at all
- `CommandName` identifies which command was rejected (for error messages / FR49)
- Follows naming convention: `{Noun}Rejection`

### Impact on Existing Tests (CRITICAL — Task 3.6)

Existing tests in `TenantAggregateTests.cs` use `"test-user"` as the CommandEnvelope UserId. With RBAC enforcement, `"test-user"` must have the correct role in state OR be a GlobalAdmin for the test to pass.

**For 2-param Handle methods** (CreateTenant, DisableTenant, EnableTenant): No change — these don't get RBAC checks.

**For 3-param Handle methods** (AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, UpdateTenant): Existing tests will FAIL because "test-user" is not in `state.Users` and is not a GlobalAdmin.

**Fix approach:** Update existing tests to either:
1. Add "test-user" as TenantOwner to the test state before processing the command
2. OR use GlobalAdmin bypass (`isGlobalAdmin: true`) for non-RBAC-specific tests

**Recommended:** Option 1 — add `state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner))` to test setup for existing user-role and update tests. **Scope:** Only add test-user to state in tests where (a) the Handle method is converted to 3-param AND (b) the state is non-null and active. Null-state tests and disabled-tenant tests are unaffected — those guard arms fire before the RBAC check. Do NOT add test-user to state in those tests as it would change test semantics.

### Technical Requirements

**New contract type:**
- `InsufficientPermissionsRejection` record in `Contracts/Events/Rejections/`

**Modified files in EventStore submodule:**
- `Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs` — extend DiscoverHandleMethods and DispatchCommandAsync

**Modified files in Tenants:**
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` — convert 4 Handle methods from 2-param to 3-param, add IsAuthorized/IsGlobalAdmin helpers
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` — update CreateCommand helper, add RBAC tests, fix existing tests

**New files:**
- `src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs`

### Architecture Compliance

**Authorization Model (Architecture D8 Revision):**
- Layer 1: EventStore's `AuthorizationBehavior` in MediatR pipeline — validates JWT claims (GlobalAdmin, tenant scope)
- Layer 2: Domain RBAC in aggregate Handle methods — THIS STORY. Checks `state.Users[actorUserId]` for TenantOwner/Contributor/Reader permissions
- Layer 3: Consuming services implement `IRbacValidator` using Tenants projections

**Type Location Rules (MUST follow):**

| Type | Project | Folder | File |
|------|---------|--------|------|
| InsufficientPermissionsRejection | Contracts | Events/Rejections/ | InsufficientPermissionsRejection.cs (CREATE) |
| RBAC Handle methods | Server | Aggregates/ | TenantAggregate.cs (MODIFY) |
| 3-param Handle support | EventStore.Client | Aggregates/ | EventStoreAggregate.cs (MODIFY) |

**DO NOT:**
- Create any new projects or solution references
- Add authorization middleware or MediatR behaviors — this story is domain-level RBAC in Handle methods only
- Modify TenantState — no new state properties needed
- Add role checks to CreateTenant/DisableTenant/EnableTenant — these are Layer 1 (GlobalAdmin JWT) concerns
- Use instance state in Handle methods — they MUST remain static
- Throw exceptions from Handle methods — use `DomainResult.Rejection()`
- Modify TenantRole enum ordering — RBAC check depends on it
- Add `[JsonPropertyName]` attributes — System.Text.Json camelCase is the default
- Make Handle methods async — no async work needed

### Library & Framework Requirements

**No new NuGet packages required.**

All dependencies already available:
- `CommandEnvelope` — from `Hexalith.EventStore.Contracts.Commands` (already imported in TenantAggregate.cs)
- `DomainResult` — globally imported via Server .csproj
- `IRejectionEvent` — globally imported via Server .csproj
- xUnit 2.9.3, Shouldly 4.3.0 — existing test infrastructure

### File Structure Requirements

**Modified files:**
```
Hexalith.EventStore/src/Hexalith.EventStore.Client/
└── Aggregates/
    └── EventStoreAggregate.cs             (MODIFY: 3-param Handle support)

src/Hexalith.Tenants.Contracts/
└── Events/Rejections/
    └── InsufficientPermissionsRejection.cs (CREATE)

src/Hexalith.Tenants.Server/
└── Aggregates/
    └── TenantAggregate.cs                 (MODIFY: 4 Handle methods → 3-param with RBAC)

tests/Hexalith.Tenants.Server.Tests/
└── Aggregates/
    └── TenantAggregateTests.cs            (MODIFY: update helper, add ~25 RBAC tests, fix existing)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Part A: EventStore 3-param Handle test** — Add 1-2 tests in `Hexalith.EventStore.Client.Tests` verifying:
- A 3-param Handle method is discovered and invoked with the CommandEnvelope
- A 2-param Handle method still works (backward compatibility)

**Part B: RBAC test cases** — Add ~25 test methods to `TenantAggregateTests.cs` (17 RBAC matrix + 1 bootstrap + 2 self-action edge cases + 1 enum guard + 1 3-param discovery guard + conformance investigation):

**Test state setup (reuse for all RBAC tests):**
```csharp
private static TenantState CreateStateWithRoles()
{
    var state = new TenantState();
    state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
    state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
    state.Apply(new UserAddedToTenant("acme", "contributor-user", TenantRole.TenantContributor));
    state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));
    return state;
}
```

**Test helper update:**
```csharp
private static CommandEnvelope CreateCommand<T>(
    T command,
    string actorUserId = "test-user",
    bool isGlobalAdmin = false)
    where T : notnull
    => new(
        Guid.NewGuid().ToString(),
        "system",
        "tenants",
        command is BootstrapGlobalAdmin or SetGlobalAdministrator or RemoveGlobalAdministrator
            ? "global-administrators"
            : ((dynamic)command).TenantId,
        typeof(T).Name,
        JsonSerializer.SerializeToUtf8Bytes(command),
        Guid.NewGuid().ToString(),
        null,
        actorUserId,
        isGlobalAdmin
            ? new Dictionary<string, string> { ["actor:globalAdmin"] = "true" }
            : null);
```

**IMPORTANT:** The helper must preserve the existing branching logic for `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator` commands — these use `"global-administrators"` as the aggregate ID, not `command.TenantId`. The `CommandEnvelope` constructor defensively copies the Extensions dictionary, so it must be passed at construction time (not mutated after).

**RBAC test matrix:**

| # | Command | Actor | Role in State | Expected | AC |
|---|---------|-------|---------------|----------|-----|
| R1 | AddUserToTenant | reader-user | TenantReader | Rejection: InsufficientPermissionsRejection | #1 |
| R2 | AddUserToTenant | contributor-user | TenantContributor | Rejection: InsufficientPermissionsRejection | #2 |
| R3 | AddUserToTenant | owner-user | TenantOwner | Success: UserAddedToTenant | #3 |
| R4 | AddUserToTenant | global-admin | (not in Users) + isGlobalAdmin=true | Success: UserAddedToTenant | #6 |
| R5 | AddUserToTenant | unknown-user | (not in Users) | Rejection: InsufficientPermissionsRejection | #5 |
| R6 | RemoveUserFromTenant | reader-user | TenantReader | Rejection: InsufficientPermissionsRejection | #1 |
| R7 | RemoveUserFromTenant | contributor-user | TenantContributor | Rejection: InsufficientPermissionsRejection | #2 |
| R8 | RemoveUserFromTenant | owner-user | TenantOwner | Success: UserRemovedFromTenant | #3 |
| R9 | RemoveUserFromTenant | global-admin | (not in Users) + isGlobalAdmin=true | Success: UserRemovedFromTenant | #6 |
| R10 | ChangeUserRole | reader-user | TenantReader | Rejection: InsufficientPermissionsRejection | #1 |
| R11 | ChangeUserRole | contributor-user | TenantContributor | Rejection: InsufficientPermissionsRejection | #2 |
| R12 | ChangeUserRole | owner-user | TenantOwner | Success: UserRoleChanged | #3 |
| R13 | ChangeUserRole | global-admin | (not in Users) + isGlobalAdmin=true | Success: UserRoleChanged | #6 |
| R14 | UpdateTenant | reader-user | TenantReader | Rejection: InsufficientPermissionsRejection | #1 |
| R15 | UpdateTenant | contributor-user | TenantContributor | Success: TenantUpdated | #4 |
| R16 | UpdateTenant | owner-user | TenantOwner | Success: TenantUpdated | #3 |
| R17 | UpdateTenant | global-admin | (not in Users) + isGlobalAdmin=true | Success: TenantUpdated | #6 |
| R18 | RemoveUserFromTenant | owner-user | TenantOwner (self-removal) | Success: UserRemovedFromTenant | #3 |
| R19 | ChangeUserRole | owner-user | TenantOwner (self-demotion to Reader) | Success: UserRoleChanged | #3 |
| R20 | AddUserToTenant | any-user (no role) | Active tenant, Users EMPTY | Success: UserAddedToTenant (bootstrap) | #5 exception |

**RBAC assertion patterns:**
```csharp
// Rejection — verify rejection type and fields
result.IsRejection.ShouldBeTrue();
var rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
rejection.TenantId.ShouldBe("acme");
rejection.ActorUserId.ShouldBe("reader-user");
rejection.ActorRole.ShouldBe(TenantRole.TenantReader);
rejection.CommandName.ShouldBe(nameof(AddUserToTenant));

// Success — verify no RBAC interference with normal event production
result.IsSuccess.ShouldBeTrue();
result.Events[0].ShouldBeOfType<UserAddedToTenant>();
```

**Part C: Fix existing tests** — Existing user-role tests use `"test-user"` as actor but state doesn't include "test-user" in Users. Add `state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner))` to test setup for all existing 3-param Handle tests. Existing 2-param Handle tests (CreateTenant, DisableTenant, EnableTenant) need no changes.

### Previous Story Intelligence

**Story 3.1 (review) — User-Role Management:**
- Added 3 Handle methods (AddUserToTenant, RemoveUserFromTenant, ChangeUserRole) — these get RBAC checks
- 16 unit tests established — these need "test-user" added as TenantOwner in state
- Task 0 finding: FluentValidation validators don't fire for domain commands — Task 2 was skipped. No impact on RBAC story
- State property is `Users` (Dictionary<string, TenantRole>), NOT `Members` (architecture doc typo)
- `ChangeUserRole` returns `NoOp()` for same-role — RBAC check should run BEFORE NoOp check (rejected user shouldn't get NoOp)
- CA1062 → `ArgumentNullException.ThrowIfNull()` on all reference type parameters (include `envelope`)

**Story 2.4 (in-progress) — Hexalith.Tenants bootstrap:**
- Program.cs is fully wired with `AddHexalith.Tenants()`, `AddEventStoreServer()`
- `AuthorizationBehavior` is in the MediatR pipeline but only validates JWT claims (Layer 1)
- This story does NOT modify Hexalith.Tenants/pipeline — domain RBAC is Handle-method-only

**Key learnings applied:**
- CA1062 → `ArgumentNullException.ThrowIfNull()` on all reference type parameters including the new `envelope` parameter
- `TreatWarningsAsErrors = true` → all warnings are build failures
- `.editorconfig` → file-scoped namespaces, Allman braces, 4-space indent
- Test pattern: `ProcessAsync(CommandEnvelope, state)`, NOT direct Handle method calls
- Switch arm ordering matters: disabled guard precedes RBAC, RBAC precedes domain logic

### Security Considerations (Red Team Review)

**Threat 1 — GlobalAdmin Extension Forgery (HIGH):** If `CommandsController` does not strip client-provided extensions (SEC-4), an attacker can send `{"extensions": {"actor:globalAdmin": "true"}}` and bypass ALL Layer 2 RBAC. Task 2.7 verifies this. If SEC-4 is not implemented, add a `// TODO: SEC-4` comment and document the accepted risk.

**Threat 2 — Race conditions:** The DAPR actor model serializes all commands per aggregate. Concurrent commands (e.g., self-demotion followed by user management) execute in order. No race conditions possible at the domain level.

**Threat 3 — GlobalAdmin OR-logic bypass:** `!IsGlobalAdmin(envelope) && !IsAuthorized(...)` means GlobalAdmin skips RBAC but NOT domain invariants (duplicate user, invalid role, disabled tenant). This is correct by design.

**Threat 4 — Unknown role default:** `MeetsMinimumRole` returns `false` for any unrecognized `TenantRole` value (default deny). A new enum value would be silently denied all operations until `MeetsMinimumRole` is updated. The enum ordinal regression test (Task 3.7) will detect new values.

### Git Intelligence

Recent commits show EventStore submodule updates are routine (`chore: Update Hexalith.EventStore submodule`). Modifying the submodule for Task 0 follows established workflow — commit submodule changes, then commit Tenants changes that depend on them.

Last functional commit: `fc66d2a feat: Implement user-role management in TenantAggregate` — this is Story 3.1's implementation. All 3 Handle methods to modify are from this commit.

### Critical Anti-Patterns (DO NOT)

- **DO NOT** add RBAC checks to CreateTenant, DisableTenant, EnableTenant — these are Layer 1 (GlobalAdmin JWT) concerns, not per-tenant role checks. The tenant doesn't exist yet for CreateTenant; Disable/Enable are platform operations
- **DO NOT** create new MediatR behaviors or middleware — this story is domain-level RBAC in Handle methods only
- **DO NOT** modify command records to add ActorUserId — use CommandEnvelope.UserId from the 3-param Handle
- **DO NOT** use `state.Members` — the property is `state.Users`
- **DO NOT** throw exceptions from Handle methods — use `DomainResult.Rejection()`
- **DO NOT** make Handle methods async — no async work needed
- **DO NOT** add instance state to TenantAggregate — Handle methods MUST remain static
- **DO NOT** use `actorRole <= minimumRole` integer comparison for role hierarchy — use explicit `MeetsMinimumRole()` switch method to avoid fragile ordinal dependency
- **DO NOT** reorder TenantRole enum values without updating `MeetsMinimumRole()` — the regression test will catch this but the method should be the source of truth
- **DO NOT** add "last owner" protection in this story — self-removal and self-demotion are allowed for MVP; track last-owner guard as follow-up
- **DO NOT** skip RBAC for same-role ChangeUserRole NoOp — a Contributor shouldn't get NoOp instead of rejection when attempting a role change
- **DO NOT** add "test-user" as GlobalAdmin in tests — add as TenantOwner to test the actual per-tenant role flow

### ADR: Handle Method Signature for Domain RBAC

**Decision:** Extend `EventStoreAggregate<TState>` to support optional 3-parameter Handle methods `Handle(Command, State?, CommandEnvelope)`.

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **3-param Handle (chosen)** | Clean, backward-compatible, principled, ~15 LOC | Modifies submodule | Best option |
| Add ActorUserId to commands | No submodule change | Forgeable by clients, couples pipeline to domain, breaks command purity | Rejected |
| AsyncLocal/ThreadStatic stash | No submodule change | Hidden state, violates pure-function principle, fragile in async | Rejected |
| Wrapper command types | No submodule change | Breaks wire format, framework dispatch mismatch | Rejected |
| Pipeline-only authorization | No Handle changes | Architecture says Layer 2 is domain logic; loses aggregate-level enforcement | Rejected |

**First principles validation:** RBAC in Handle methods IS appropriate domain logic for this service — the TenantRole hierarchy is the domain model, not a cross-cutting infrastructure concern. Extending the framework is the principled approach: it's backward-compatible, reusable across the ecosystem, and keeps Handle methods as pure functions.

### Project Structure Notes

- No new projects or test projects needed
- `EventStoreAggregate.cs` is in the Git submodule — commit there first, then in Tenants
- `HandleMethodInfo` record in EventStoreAggregate.cs needs a new `HasEnvelope` field
- Existing `Contracts/Events/Rejections/` folder has 8 rejection types — adding 9th
- Existing test infrastructure (CreateCommand helper, TenantState setup) is extended, not replaced

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.2] — Story definition, ACs, BDD scenarios
- [Source: _bmad-output/planning-artifacts/architecture.md#Authorization] — Two-layer authorization model, D8 Revision (Lines 322-333)
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — Handle/Apply patterns, three-outcome model (Lines 557-590)
- [Source: _bmad-output/planning-artifacts/architecture.md#FR-to-Structure Mapping] — FR31-34 → TenantAggregate.cs Handle methods (Line 833)
- [Source: _bmad-output/planning-artifacts/architecture.md#D9 Revision] — Command processing pipeline, CommandEnvelope flow (Lines 840-889)
- [Source: _bmad-output/planning-artifacts/prd.md#FR31-34] — Role behavior requirements (Lines 528-531)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs] — Handle method discovery (2-param limit at line 76), dispatch (line 118)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs] — UserId field (actor identity), Extensions dictionary
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs] — Current 7 Handle methods (4 needing RBAC)
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — Users dictionary (Dictionary<string, TenantRole>)
- [Source: src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs] — TenantOwner=0, TenantContributor=1, TenantReader=2
- [Source: tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs] — Existing test patterns, CreateCommand helper
- [Source: _bmad-output/implementation-artifacts/3-1-user-role-management.md] — Previous story with CA1062 learnings, test patterns, validator skip rationale

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Task 0.4: DomainProcessorBase delegates to abstract HandleAsync(CommandEnvelope, TState?) — not reflection-based, no changes needed
- Task 0.5: HandleMethodInfo is private sealed, no external construction possible
- Task 1.2-1.3: Serialization test needed Nullable<TenantRole> support added to EventSerializationTests.cs GetTestValue helper
- Task 2.7: SEC-4 extension sanitization is already implemented in CommandsController.Submit() via ExtensionMetadataSanitizer. The sanitizer validates extensions but allows client-provided ones through after sanitization. The `actor:globalAdmin` extension must be server-populated by an AuthorizationBehavior (not yet wired). Comment documenting this is in IsGlobalAdmin method
- Task 3.6: 9 existing tests were fixed by adding test-user as TenantOwner (or Contributor for UpdateTenant) to state. Null-state and disabled-tenant tests were unaffected (guards fire before RBAC)
- Task 3.11: InMemoryTenantService does not exist yet (Epic 6). No conformance test impact

### Completion Notes List

- Extended EventStore framework with backward-compatible 3-param Handle method support (HandleMethodInfo.HasEnvelope)
- Created InsufficientPermissionsRejection with nullable ActorRole and CommandName fields
- Added RBAC enforcement to 4 Handle methods: AddUserToTenant (Owner), RemoveUserFromTenant (Owner), ChangeUserRole (Owner), UpdateTenant (Contributor)
- Left CreateTenant/DisableTenant/EnableTenant as 2-param (Layer 1 GlobalAdmin JWT concern)
- Implemented empty-tenant bootstrap exception: first AddUserToTenant succeeds when Users is empty
- GlobalAdmin bypass via CommandEnvelope.Extensions["actor:globalAdmin"] = "true"
- Explicit MeetsMinimumRole switch (not integer comparison) with default deny
- 23 new RBAC tests + 9 existing tests fixed = 54 total TenantAggregateTests (was 31)
- 122 total tests across all projects, all passing, 0 warnings, 0 regressions

### File List

**EventStore submodule (committed as 5c9dc56):**
- Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs (MODIFIED: 3-param Handle support)
- Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Aggregates/EventStoreAggregateTests.cs (MODIFIED: 2 new 3-param tests)

**Tenants repo:**
- src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs (CREATED)
- src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs (MODIFIED: 4 Handle methods → 3-param with RBAC + 3 helper methods)
- tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs (MODIFIED: Nullable<TenantRole> support)
- tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs (MODIFIED: updated helper, fixed 9 tests, added 23 RBAC tests)
- Hexalith.EventStore (submodule pointer updated)

### Change Log

- 2026-03-16: Story 3.2 implementation — Role Behavior Enforcement with domain-level RBAC in TenantAggregate Handle methods
