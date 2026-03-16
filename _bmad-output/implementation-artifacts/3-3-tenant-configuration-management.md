# Story 3.3: Tenant Configuration Management

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to set and remove key-value configuration entries for my tenant using namespaced keys,
So that consuming services can react to per-tenant settings like billing plans or feature flags.

## Acceptance Criteria

1. **Given** an active tenant exists and the requesting user is a TenantOwner or GlobalAdministrator
   **When** a SetTenantConfiguration command is processed with a key and value
   **Then** a TenantConfigurationSet event is produced with TenantId, Key, and Value

2. **Given** a configuration entry exists for a tenant
   **When** a RemoveTenantConfiguration command is processed with the matching key
   **Then** a TenantConfigurationRemoved event is produced with TenantId and Key

3. **Given** a configuration key uses dot-delimited namespace convention (e.g., `billing.plan`, `parties.maxContacts`)
   **When** the SetTenantConfiguration command is processed
   **Then** the key is accepted and stored preserving the namespace structure

4. **Given** a tenant already has 100 configuration keys
   **When** a SetTenantConfiguration command attempts to add a 101st key
   **Then** the command is rejected with ConfigurationLimitExceededRejection identifying the key count limit (100) and current usage

5. **Given** a SetTenantConfiguration command with a value exceeding 1024 characters
   **When** the command is processed
   **Then** the command is rejected with ConfigurationLimitExceededRejection identifying the value size limit (1024)

6. **Given** a SetTenantConfiguration command with a key exceeding 256 characters
   **When** the command is processed
   **Then** the command is rejected with ConfigurationLimitExceededRejection identifying the key length limit (256)

7. **Given** a SetTenantConfiguration command is submitted
   **When** FluentValidation runs in the MediatR pipeline
   **Then** the command is validated for required fields (TenantId, Key non-empty) and structural constraints (key max 256 chars, value max 1024 chars)

8. **Given** the TenantAggregate Handle methods for configuration commands
   **When** tested as static pure functions
   **Then** all Handle and Apply methods execute correctly as Tier 1 unit tests with 100% branch coverage on limit enforcement logic

## Tasks / Subtasks

- [ ] Task 1: Add configuration Handle methods to TenantAggregate (AC: #1-#6)
  - [ ] 1.1: Add configuration limit constants to TenantAggregate: `MaxConfigurationKeys = 100`, `MaxKeyLength = 256`, `MaxValueLength = 1024`
  - [ ] 1.2: Add `Handle(SetTenantConfiguration, TenantState?, CommandEnvelope)` as 3-param with null/disabled/RBAC(TenantOwner)/key-length/value-length/key-count/same-value checks
  - [ ] 1.3: Add `Handle(RemoveTenantConfiguration, TenantState?, CommandEnvelope)` as 3-param with null/disabled/RBAC(TenantOwner)/key-not-found checks
  - [ ] 1.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Create SetTenantConfigurationValidator (AC: #7)
  - [ ] 2.1: Create `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs` with rules: TenantId NotEmpty, Key NotEmpty + MaximumLength(256), Value MaximumLength(1024)
  - [ ] 2.2: Update `src/Hexalith.Tenants.CommandApi/Validation/TenantSubmitCommandValidator.cs` to add `SetTenantConfiguration` case in the switch — inject `IValidator<SetTenantConfiguration>` in constructor
  - [ ] 2.3: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 3: Create unit tests (AC: #8)
  - [ ] 3.1: Add configuration Handle method tests to `TenantAggregateTests.cs` (~22 test cases — see test matrix below)
  - [ ] 3.2: Add validator tests `SetTenantConfigurationValidatorTests.cs` in `tests/Hexalith.Tenants.Server.Tests/Validators/`
  - [ ] 3.3: Update `TenantSubmitCommandValidatorTests.cs` with SetTenantConfiguration pipeline validation test
  - [ ] 3.4: Verify existing tests still pass: `dotnet test Hexalith.Tenants.slnx` — all pass, no regressions

- [ ] Task 4: Build verification (all ACs)
  - [ ] 4.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 4.2: `dotnet test` all test projects — all pass, no regressions

## Dev Notes

### Dependency: Story 3.2 (Role Behavior Enforcement)

**Story 3.2 MUST be completed before implementing this story.** Story 3.2:
- Extends EventStore to support 3-param Handle methods `Handle(Command, State?, CommandEnvelope)` — required for RBAC
- Establishes RBAC helper methods in TenantAggregate: `IsAuthorized()`, `MeetsMinimumRole()`, `IsGlobalAdmin()`
- Creates `InsufficientPermissionsRejection` contract type
- Converts AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, UpdateTenant to 3-param

This story adds 2 new 3-param Handle methods for SetTenantConfiguration and RemoveTenantConfiguration following the exact same RBAC pattern.

### Permission Matrix (extends Story 3.2)

| Command | Minimum Role | Handle Params |
|---------|-------------|---------------|
| SetTenantConfiguration | TenantOwner | 3-param |
| RemoveTenantConfiguration | TenantOwner | 3-param |

Per epics: "A TenantOwner has TenantContributor capabilities plus user-role management and tenant configuration management" (FR33). TenantContributor CANNOT manage configuration (FR31-33). GlobalAdmin bypasses all RBAC.

### Configuration Limit Constants

Define as `internal const` in TenantAggregate so both Handle methods and the validator (same Server project) can reference them:

```csharp
// FR23: Configuration limits — 1KB value limit interpreted as 1024 characters (not bytes).
// Using string.Length for simplicity. For Latin text chars ≈ bytes; for multi-byte this is more lenient.
// See "1KB interpretation" note below. Do NOT change to Encoding.UTF8.GetByteCount without updating tests and validator.
internal const int MaxConfigurationKeys = 100;    // FR23: max keys per tenant
internal const int MaxKeyLength = 256;             // FR23: max characters per key
internal const int MaxValueLength = 1024;          // FR23: max characters per value (~1KB)
```

**1KB interpretation:** FR23 says "maximum 1KB per value". Using character count (`string.Length`) instead of byte count (`Encoding.UTF8.GetByteCount`) for simplicity. For Latin text, characters ≈ bytes. For multi-byte characters this is more lenient. Acceptable for administrative settings.

### Handle Method Implementations

```csharp
public static DomainResult Handle(SetTenantConfiguration command, TenantState? state, CommandEnvelope envelope)
{
    ArgumentNullException.ThrowIfNull(command);
    ArgumentNullException.ThrowIfNull(envelope);
    // Null-guard individual string properties — the record allows null despite string type.
    // FluentValidation catches empty Key/TenantId, but null Value has no pipeline guard.
    ArgumentNullException.ThrowIfNull(command.Key);
    ArgumentNullException.ThrowIfNull(command.Value);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        // RBAC: TenantOwner only (skip if GlobalAdmin)
        _ when !IsGlobalAdmin(envelope)
            && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
            => DomainResult.Rejection([new InsufficientPermissionsRejection(
                command.TenantId, envelope.UserId,
                state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                nameof(SetTenantConfiguration))]),
        // Limit: key length (FR23)
        _ when command.Key.Length > MaxKeyLength
            => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                command.TenantId, "KeyLength", command.Key.Length, MaxKeyLength)]),
        // Limit: value length (FR23)
        _ when command.Value.Length > MaxValueLength
            => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                command.TenantId, "ValueSize", command.Value.Length, MaxValueLength)]),
        // Limit: key count — only when adding a NEW key (FR23)
        _ when !state.Configuration.ContainsKey(command.Key)
            && state.Configuration.Count >= MaxConfigurationKeys
            => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                command.TenantId, "KeyCount", state.Configuration.Count, MaxConfigurationKeys)]),
        // Idempotent: same key, same value → NoOp
        _ when state.Configuration.TryGetValue(command.Key, out string? existing)
            && existing == command.Value
            => DomainResult.NoOp(),
        _ => DomainResult.Success([new TenantConfigurationSet(command.TenantId, command.Key, command.Value)]),
    };
}

public static DomainResult Handle(RemoveTenantConfiguration command, TenantState? state, CommandEnvelope envelope)
{
    ArgumentNullException.ThrowIfNull(command);
    ArgumentNullException.ThrowIfNull(envelope);
    ArgumentNullException.ThrowIfNull(command.Key);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        // RBAC: TenantOwner only (skip if GlobalAdmin)
        _ when !IsGlobalAdmin(envelope)
            && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
            => DomainResult.Rejection([new InsufficientPermissionsRejection(
                command.TenantId, envelope.UserId,
                state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                nameof(RemoveTenantConfiguration))]),
        // Idempotent: key not present → NoOp (desired state already achieved)
        _ when !state.Configuration.ContainsKey(command.Key)
            => DomainResult.NoOp(),
        _ => DomainResult.Success([new TenantConfigurationRemoved(command.TenantId, command.Key)]),
    };
}
```

### Design Decisions

**RemoveTenantConfiguration for non-existent key → NoOp (not rejection):**
The contracts don't include a `ConfigurationKeyNotFoundRejection`. Using NoOp follows the idempotent pattern: the desired state (key absent) is already achieved. This differs from `RemoveUserFromTenant` (which rejects) because user removal is typically a deliberate action where the caller should know the user exists, while config key removal may be defensive cleanup. No new contract type needed.

**SetTenantConfiguration same-value → NoOp:**
Consistent with `ChangeUserRole` same-role → NoOp pattern. Prevents unnecessary events in the stream for idempotent operations.

**SetTenantConfiguration updating existing key → Success (not NoOp):**
Setting an existing key to a NEW value produces a `TenantConfigurationSet` event. The key count limit check skips existing keys (`!state.Configuration.ContainsKey(command.Key)`) — overwriting doesn't consume a new slot.

**Dot-delimited keys (FR21) — no special handling needed:**
Keys are stored as plain strings. Dot-delimited naming (e.g., `billing.plan`, `parties.maxContacts`) is a convention, not enforced by the domain. The aggregate treats keys as opaque strings. Namespace collision prevention is a consuming-service responsibility.

### Switch Arm Ordering (CRITICAL)

Guard arm ordering in SetTenantConfiguration matches the established pattern:
1. **Null state** → TenantNotFoundRejection
2. **Disabled tenant** → TenantDisabledRejection
3. **RBAC** → InsufficientPermissionsRejection
4. **Key length limit** → ConfigurationLimitExceededRejection
5. **Value length limit** → ConfigurationLimitExceededRejection
6. **Key count limit (new key only)** → ConfigurationLimitExceededRejection
7. **Same value** → NoOp
8. **Default** → Success

Limit checks come after RBAC: an unauthorized user should get a permissions error, not a limit error. This is consistent with Story 3.2's pattern where RBAC precedes domain logic.

### Technical Requirements

**New files:**
- `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs` — FluentValidation for structural constraints

**Modified files:**
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` — Add 2 Handle methods + 3 limit constants
- `src/Hexalith.Tenants.CommandApi/Validation/TenantSubmitCommandValidator.cs` — Add SetTenantConfiguration case
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` — Add ~22 configuration tests

**No new contract types needed.** All contracts already exist from Story 2.1:
- Commands: `SetTenantConfiguration(TenantId, Key, Value)`, `RemoveTenantConfiguration(TenantId, Key)`
- Events: `TenantConfigurationSet(TenantId, Key, Value) : IEventPayload`, `TenantConfigurationRemoved(TenantId, Key) : IEventPayload`
- Rejections: `ConfigurationLimitExceededRejection(TenantId, LimitType, CurrentCount, MaxAllowed) : IRejectionEvent`
- `InsufficientPermissionsRejection` — created by Story 3.2

**No new NuGet packages.** FluentValidation already available from Story 3.1.

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type | Project | Folder | File |
|------|---------|--------|------|
| Handle methods + limit constants | Server | Aggregates/ | TenantAggregate.cs (MODIFY) |
| SetTenantConfigurationValidator | Server | Validators/ | SetTenantConfigurationValidator.cs (CREATE) |
| TenantSubmitCommandValidator | CommandApi | Validation/ | TenantSubmitCommandValidator.cs (MODIFY) |

**DO NOT:**
- Create any new contract types — all commands, events, rejections already exist
- Modify TenantState — both Apply methods already exist (from Story 2.3)
- Create new projects or new solution references
- Create a RemoveTenantConfigurationValidator — architecture says simple commands rely on domain validation only
- Throw exceptions from Handle methods — use `DomainResult.Rejection()`
- Make Handle methods async — no async work needed
- Add instance state to TenantAggregate — Handle methods MUST remain static
- Use `state.Members` — the property is `state.Users`
- Add `[JsonPropertyName]` attributes — System.Text.Json camelCase is the default
- Enforce dot-delimited key format — FR21 is a convention, not a domain invariant

### Library & Framework Requirements

**No new NuGet packages required.**

All dependencies already available:
- `CommandEnvelope` — from `Hexalith.EventStore.Contracts.Commands` (already imported)
- `DomainResult` — globally imported via Server .csproj
- `IRejectionEvent` — globally imported via Server .csproj
- `InsufficientPermissionsRejection` — created by Story 3.2 in Contracts/Events/Rejections/
- FluentValidation 12.1.1 — already referenced by Server .csproj (Story 3.1)
- xUnit 2.9.3, Shouldly 4.3.0 — existing test infrastructure

### File Structure Requirements

```
src/Hexalith.Tenants.Server/
├── Aggregates/
│   └── TenantAggregate.cs                       (MODIFY: add 2 Handle methods + 3 constants)
└── Validators/
    └── SetTenantConfigurationValidator.cs       (CREATE)

src/Hexalith.Tenants.CommandApi/
└── Validation/
    └── TenantSubmitCommandValidator.cs          (MODIFY: add SetTenantConfiguration case)

tests/Hexalith.Tenants.Server.Tests/
├── Aggregates/
│   └── TenantAggregateTests.cs                  (MODIFY: add ~22 configuration tests)
└── Validators/
    └── SetTenantConfigurationValidatorTests.cs  (CREATE)
```

### SetTenantConfigurationValidator Implementation

```csharp
// src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs
using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Server.Aggregates;

namespace Hexalith.Tenants.Server.Validators;

public class SetTenantConfigurationValidator : AbstractValidator<SetTenantConfiguration>
{
    public SetTenantConfigurationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(TenantAggregate.MaxKeyLength);
        RuleFor(x => x.Value).MaximumLength(TenantAggregate.MaxValueLength);
    }
}
```

**Note:** Value is NOT `.NotEmpty()` — an empty string is a valid configuration value (clearing a value while keeping the key). FR19 says "set a key-value configuration entry" — the value can be empty.

**Note:** Constants must be `internal` (not `private`) for the validator to reference `TenantAggregate.MaxKeyLength` and `TenantAggregate.MaxValueLength` since the validator is in the same assembly.

### TenantSubmitCommandValidator Update

Add `SetTenantConfiguration` case to the existing switch statement and inject its validator:

```csharp
public TenantSubmitCommandValidator(
    IValidator<AddUserToTenant> addUserToTenantValidator,
    IValidator<ChangeUserRole> changeUserRoleValidator,
    IValidator<SetTenantConfiguration> setTenantConfigurationValidator)  // NEW
{
    RuleFor(x => x).Custom((command, context) =>
    {
        switch (command.CommandType)
        {
            case nameof(AddUserToTenant):
                ValidatePayload(command, context, addUserToTenantValidator);
                break;
            case nameof(ChangeUserRole):
                ValidatePayload(command, context, changeUserRoleValidator);
                break;
            case nameof(SetTenantConfiguration):  // NEW
                ValidatePayload(command, context, setTenantConfigurationValidator);
                break;
        }
    });
}
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Test state setup (reuse for RBAC configuration tests):**
```csharp
// Reuse CreateStateWithRoles() from Story 3.2 if available, otherwise create:
private static TenantState CreateStateWithRolesAndConfig()
{
    var state = new TenantState();
    state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
    state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
    state.Apply(new UserAddedToTenant("acme", "contributor-user", TenantRole.TenantContributor));
    state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));
    state.Apply(new TenantConfigurationSet("acme", "billing.plan", "pro"));
    return state;
}
```

**Test matrix — SetTenantConfiguration:**

| # | Command | Actor | State | Expected | AC |
|---|---------|-------|-------|----------|-----|
| C1 | SetTenantConfiguration("acme", "theme.color", "blue") | owner-user | Active, has roles | Success: TenantConfigurationSet | #1 |
| C2 | SetTenantConfiguration("acme", "billing.plan", "enterprise") | owner-user | Active, billing.plan="pro" | Success: TenantConfigurationSet (overwrite) | #1 |
| C3 | SetTenantConfiguration("acme", "parties.maxContacts", "500") | owner-user | Active | Success (dot-delimited key) | #3 |
| C4 | SetTenantConfiguration | null state | - | Rejection: TenantNotFoundRejection | #1 |
| C5 | SetTenantConfiguration | owner-user | Disabled tenant | Rejection: TenantDisabledRejection | #1 |
| C6 | SetTenantConfiguration | reader-user | Active | Rejection: InsufficientPermissionsRejection | #1 |
| C7 | SetTenantConfiguration | contributor-user | Active | Rejection: InsufficientPermissionsRejection | #1 |
| C8 | SetTenantConfiguration | global-admin (isGlobalAdmin=true) | Active | Success: TenantConfigurationSet | #1 |
| C9 | SetTenantConfiguration (key 257 chars) | owner-user | Active | Rejection: ConfigurationLimitExceededRejection("KeyLength", 257, 256) | #6 |
| C10 | SetTenantConfiguration (value 1025 chars) | owner-user | Active | Rejection: ConfigurationLimitExceededRejection("ValueSize", 1025, 1024) | #5 |
| C11 | SetTenantConfiguration (101st key) | owner-user | Active, 100 keys | Rejection: ConfigurationLimitExceededRejection("KeyCount", 100, 100) | #4 |
| C12 | SetTenantConfiguration (overwrite existing, 100 keys) | owner-user | Active, 100 keys incl. target key | Success: TenantConfigurationSet (no new slot) | #4 |
| C13 | SetTenantConfiguration("acme", "billing.plan", "pro") | owner-user | Active, billing.plan="pro" | NoOp (same value) | #1 |

**Test matrix — RemoveTenantConfiguration:**

| # | Command | Actor | State | Expected | AC |
|---|---------|-------|-------|----------|-----|
| C14 | RemoveTenantConfiguration("acme", "billing.plan") | owner-user | Active, billing.plan exists | Success: TenantConfigurationRemoved | #2 |
| C15 | RemoveTenantConfiguration | null state | - | Rejection: TenantNotFoundRejection | #2 |
| C16 | RemoveTenantConfiguration | owner-user | Disabled tenant | Rejection: TenantDisabledRejection | #2 |
| C17 | RemoveTenantConfiguration | reader-user | Active | Rejection: InsufficientPermissionsRejection | #2 |
| C18 | RemoveTenantConfiguration("acme", "nonexistent") | owner-user | Active, key absent | NoOp (key already absent) | #2 |
| C19 | RemoveTenantConfiguration | global-admin (isGlobalAdmin=true) | Active, key exists | Success: TenantConfigurationRemoved | #2 |

**Test matrix — Switch arm ordering & boundary verification:**

| # | Command | Actor | State | Expected | AC |
|---|---------|-------|-------|----------|-----|
| C20 | SetTenantConfiguration | reader-user | Disabled tenant | Rejection: TenantDisabledRejection (NOT InsufficientPermissionsRejection — verifies disabled guard precedes RBAC) | #1 |
| C21 | SetTenantConfiguration (key exactly 256 chars) | owner-user | Active | Success: TenantConfigurationSet (boundary: `>` not `>=`) | #6 |
| C22 | SetTenantConfiguration (value exactly 1024 chars) | owner-user | Active | Success: TenantConfigurationSet (boundary: `>` not `>=`) | #5 |

**Assertion patterns:**
```csharp
// SetTenantConfiguration success
result.IsSuccess.ShouldBeTrue();
var evt = result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
evt.TenantId.ShouldBe("acme");
evt.Key.ShouldBe("theme.color");
evt.Value.ShouldBe("blue");

// ConfigurationLimitExceededRejection
result.IsRejection.ShouldBeTrue();
var rejection = result.Events[0].ShouldBeOfType<ConfigurationLimitExceededRejection>();
rejection.TenantId.ShouldBe("acme");
rejection.LimitType.ShouldBe("KeyCount");
rejection.CurrentCount.ShouldBe(100);
rejection.MaxAllowed.ShouldBe(100);

// NoOp (same value or key absent)
result.IsNoOp.ShouldBeTrue();
result.Events.Count.ShouldBe(0);
```

**State setup for 100-key limit test:**
```csharp
private static TenantState CreateStateWith100ConfigKeys()
{
    var state = CreateStateWithRolesAndConfig(); // already has billing.plan
    for (int i = 1; i < 100; i++)
    {
        state.Apply(new TenantConfigurationSet("acme", $"key.{i}", $"value-{i}"));
    }
    // state.Configuration.Count == 100 (billing.plan + key.1..key.99)
    return state;
}
```

**Validator tests (SetTenantConfigurationValidatorTests.cs):**
```csharp
public class SetTenantConfigurationValidatorTests
{
    private readonly SetTenantConfigurationValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("", "key", "value"))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_Key_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "", "value"))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_have_error_when_Key_exceeds_max_length()
        => _validator.TestValidate(new SetTenantConfiguration("acme", new string('k', 257), "value"))
            .ShouldHaveValidationErrorFor(x => x.Key);

    [Fact]
    public void Should_have_error_when_Value_exceeds_max_length()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "key", new string('v', 1025)))
            .ShouldHaveValidationErrorFor(x => x.Value);

    [Fact]
    public void Should_not_have_error_when_Value_is_empty()
        => _validator.TestValidate(new SetTenantConfiguration("acme", "key", ""))
            .ShouldNotHaveValidationErrorFor(x => x.Value);
}
```

### Previous Story Intelligence

**Story 3.2 (ready-for-dev) — Role Behavior Enforcement (DEPENDENCY):**
- Establishes 3-param Handle method pattern with CommandEnvelope for RBAC
- Creates `InsufficientPermissionsRejection(TenantId, ActorUserId, ActorRole?, CommandName)` contract
- Adds `IsAuthorized()`, `MeetsMinimumRole()`, `IsGlobalAdmin()` private static helpers to TenantAggregate
- Converts 4 Handle methods from 2-param to 3-param — this story adds 2 more following the same pattern
- Permission matrix note: "Story 3.3 commands (SetTenantConfiguration, RemoveTenantConfiguration) will also need TenantOwner RBAC — the pattern established here applies directly"
- GlobalAdmin bypass via `CommandEnvelope.Extensions["actor:globalAdmin"]` = `"true"` (server-populated, SEC-4)
- Empty tenant bootstrap: `state.Users.Count > 0` check for AddUserToTenant only — NOT applicable to configuration commands (config changes always require RBAC)

**Story 3.1 (done) — User-Role Management:**
- Added FluentValidation to Server .csproj, created validator pattern in `Server/Validators/`
- Created `TenantSubmitCommandValidator` in CommandApi for pipeline-level validation
- `UserAlreadyInTenantRejection` was extended with `ExistingRole` field during review
- CA1062 → `ArgumentNullException.ThrowIfNull()` on all reference type parameters including `envelope`
- Test pattern: `ProcessAsync(CommandEnvelope, state)`, NOT direct Handle method calls

**Story 2.3 (done) — TenantAggregate lifecycle:**
- TenantState has `Configuration` property (`Dictionary<string, string>`) and both Apply methods already implemented
- Apply(TenantConfigurationSet): `Configuration[e.Key] = e.Value`
- Apply(TenantConfigurationRemoved): `Configuration.Remove(e.Key)`

**Key learnings applied:**
- CA1062 → `ArgumentNullException.ThrowIfNull()` on `command` AND `envelope`
- `TreatWarningsAsErrors = true` → all warnings are build failures
- `.editorconfig` → file-scoped namespaces, Allman braces, 4-space indent
- Test pattern: `ProcessAsync(CommandEnvelope, state)` via aggregate instance
- Switch arm ordering: null → disabled → RBAC → domain logic → NoOp → success
- Validator style: NO `_ = RuleFor(...)` discard pattern — call `RuleFor(...)` directly

### Git Intelligence

Recent commits show:
- `fc66d2a feat: Implement user-role management in TenantAggregate` — Story 3.1 implementation
- `f7a03c5 feat: Update Story 2.4 status to review and refine acceptance criteria`
- EventStore submodule updates are routine (`chore: Update Hexalith.EventStore submodule`)

Story 3.2 has NOT been committed yet (it's `ready-for-dev`). If Story 3.2 is implemented first, its commit will show the 3-param Handle pattern and RBAC helpers. This story extends that work.

### Critical Anti-Patterns (DO NOT)

- **DO NOT** create new contract types — SetTenantConfiguration, RemoveTenantConfiguration, TenantConfigurationSet, TenantConfigurationRemoved, ConfigurationLimitExceededRejection all exist
- **DO NOT** modify TenantState — both Apply methods already exist from Story 2.3
- **DO NOT** create a RemoveTenantConfigurationValidator — architecture says simple commands rely on domain validation
- **DO NOT** enforce dot-delimited key format — FR21 is a naming convention, not a domain invariant
- **DO NOT** throw exceptions from Handle methods — use `DomainResult.Rejection()` and `DomainResult.NoOp()`
- **DO NOT** make Handle methods async — no async work needed
- **DO NOT** add instance state to TenantAggregate — Handle methods MUST remain static
- **DO NOT** use `state.Members` — the property is `state.Users`
- **DO NOT** use byte count for value length — use `string.Length` (character count, see "1KB interpretation" above)
- **DO NOT** add `[JsonPropertyName]` attributes — System.Text.Json camelCase is the default
- **DO NOT** add `.NotEmpty()` validation on Value — empty string is a valid configuration value
- **DO NOT** skip `ArgumentNullException.ThrowIfNull` on `command.Key` and `command.Value` — C# records allow null strings at runtime even with non-nullable type annotations; `command.Value.Length` will throw NRE without this guard
- **DO NOT** skip RBAC for configuration commands — TenantOwner is required even though these are "lower risk" than user management
- **DO NOT** implement 2-param Handle methods — configuration commands MUST use 3-param for RBAC enforcement (depends on Story 3.2)
- **DO NOT** add "test-user" as GlobalAdmin in tests — add as TenantOwner to test actual per-tenant role flow

### Project Structure Notes

- Alignment with existing structure: Handle methods in TenantAggregate.cs, validator in Server/Validators/
- No new projects or test projects needed
- Validator registration already handled by `AddValidatorsFromAssembly()` in Program.cs (Story 3.1)
- TenantSubmitCommandValidator needs modification (add case), not replacement
- Configuration Apply methods in TenantState already verified by existing test `TenantState_apply_methods_update_users_and_configuration`

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3] — Story definition, ACs, BDD scenarios (Lines 746-784)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3] — Epic objectives (Lines 666-668)
- [Source: _bmad-output/planning-artifacts/prd.md#FR19-FR24] — Configuration functional requirements (Lines 508-513)
- [Source: _bmad-output/planning-artifacts/architecture.md#TenantState] — Configuration dictionary, 25KB max capacity (Lines 140-154)
- [Source: _bmad-output/planning-artifacts/architecture.md#Tenant Configuration Boundary] — Low-frequency settings, not real-time flags (Lines 168-170)
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — Handle/Apply patterns, three-outcome model (Lines 557-590)
- [Source: _bmad-output/planning-artifacts/architecture.md#Validation Pattern] — FluentValidation for SetTenantConfiguration (Line 616)
- [Source: _bmad-output/planning-artifacts/architecture.md#FR-to-Structure Mapping] — FR19-24 → TenantAggregate.cs (Line 831)
- [Source: src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs] — `record SetTenantConfiguration(string TenantId, string Key, string Value)`
- [Source: src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs] — `record RemoveTenantConfiguration(string TenantId, string Key)`
- [Source: src/Hexalith.Tenants.Contracts/Events/TenantConfigurationSet.cs] — `record TenantConfigurationSet(TenantId, Key, Value) : IEventPayload`
- [Source: src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs] — `record TenantConfigurationRemoved(TenantId, Key) : IEventPayload`
- [Source: src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationLimitExceededRejection.cs] — `record ConfigurationLimitExceededRejection(TenantId, LimitType, CurrentCount, MaxAllowed) : IRejectionEvent`
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs] — Current 7 Handle methods, add 2 more
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — Configuration dictionary + Apply methods already exist
- [Source: src/Hexalith.Tenants.CommandApi/Validation/TenantSubmitCommandValidator.cs] — Pipeline validator pattern to extend
- [Source: src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs] — Validator pattern to follow
- [Source: _bmad-output/implementation-artifacts/3-2-role-behavior-enforcement.md] — RBAC pattern, 3-param Handle, InsufficientPermissionsRejection, permission matrix
- [Source: _bmad-output/implementation-artifacts/3-1-user-role-management.md] — Validator creation, CA1062, TenantSubmitCommandValidator pattern

### Party Mode Review Findings (2026-03-16)

**Reviewers:** Winston (Architect), Amelia (Dev), Murat (Test Architect), Bob (Scrum Master)

**Applied fixes:**
1. **[MEDIUM] Null guard for string properties** — Added `ArgumentNullException.ThrowIfNull(command.Key)` and `ArgumentNullException.ThrowIfNull(command.Value)` to SetTenantConfiguration Handle method, and `ArgumentNullException.ThrowIfNull(command.Key)` to RemoveTenantConfiguration. C# records allow null strings at runtime despite non-nullable annotations; `command.Value.Length` would throw NRE without this.
2. **[LOW] Switch-arm ordering verification test (C20)** — Added test: SetTenantConfiguration on disabled tenant by reader-user must produce TenantDisabledRejection (not InsufficientPermissionsRejection), confirming disabled guard precedes RBAC.
3. **[LOW] Boundary tests (C21, C22)** — Added tests: key exactly 256 chars and value exactly 1024 chars must succeed, confirming limit checks use `>` not `>=`.
4. **[INFO] FR23 interpretation comment** — Added code comment on limit constants documenting the character-vs-byte decision and warning against changing to byte count without updating tests and validator.
5. **[INFO] Story 3.2 sequencing** — Dependency on Story 3.2 already documented as blocker. Sequencing confirmed: 3.2 must land first.

**Test count updated:** 19 → 22 test cases (added C20, C21, C22).

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
