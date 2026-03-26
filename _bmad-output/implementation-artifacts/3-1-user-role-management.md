# Story 3.1: User-Role Management

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to add users to my tenant with a specified role, remove users, and change their roles,
So that I can control who has access to my tenant and what they can do.

## Acceptance Criteria

1. **Given** an active tenant exists and the requesting user is a TenantOwner or GlobalAdministrator
   **When** an AddUserToTenant command is processed with a valid user ID and role (TenantOwner, TenantContributor, or TenantReader)
   **Then** a UserAddedToTenant event is produced with TenantId, UserId, and Role

2. **Given** a user is already a member of the tenant
   **When** an AddUserToTenant command is processed for the same user
   **Then** the command is rejected with UserAlreadyInTenantRejection including the existing role information

3. **Given** a user is a member of the tenant
   **When** a RemoveUserFromTenant command is processed
   **Then** a UserRemovedFromTenant event is produced with TenantId and UserId

4. **Given** a user is not a member of the tenant
   **When** a RemoveUserFromTenant command is processed for that user
   **Then** the command is rejected with UserNotInTenantRejection

5. **Given** a user is a member of the tenant with one role
   **When** a ChangeUserRole command is processed with a new valid role
   **Then** a UserRoleChanged event is produced with TenantId, UserId, OldRole, and NewRole

6. **Given** a TenantOwner attempts to assign an invalid/undefined role value
   **When** the ChangeUserRole or AddUserToTenant command is processed
   **Then** the command is rejected with RoleEscalationRejection

7. _(Infrastructure-verified, no implementation needed)_ **Given** two concurrent AddUserToTenant commands for the same user
   **When** both are processed against the same aggregate version
   **Then** the first succeeds and the second is rejected with a concurrency conflict error — guaranteed by EventStore's single-threaded actor model; no Handle method code needed

8. **Given** an AddUserToTenant command is submitted
   **When** FluentValidation runs in the MediatR pipeline
   **Then** the command is validated for required fields (TenantId, UserId non-empty, Role is valid enum value)

9. **Given** the TenantAggregate Handle methods for user-role commands
   **When** tested as static pure functions
   **Then** all Handle and Apply methods execute correctly as Tier 1 unit tests with 100% branch coverage on escalation boundaries and duplicate detection

## Tasks / Subtasks

- [x] Task 0: Investigate FluentValidation pipeline integration (BLOCKING — do before Task 2)
    - [x] 0.1: Read `Hexalith.EventStore.Hexalith.Tenants/Pipeline/ValidationBehavior.cs` — determine what `TRequest` it validates (is it `SubmitCommand` envelope or the inner domain command?)
    - [x] 0.2: Search for any `CommandPayloadValidationBehavior` or inner-command validation in EventStore.Hexalith.Tenants or EventStore.Server
    - [ ] 0.3: If validators fire for domain commands → proceed with Task 2 as written
    - [x] 0.4: If validators do NOT fire for domain commands → document the gap, skip Task 2 (validators would be dead code), and ensure Handle methods provide full domain validation (already do). Open a follow-up item for adding pipeline-level domain command validation

- [x] Task 1: Add user-role Handle methods to TenantAggregate (AC: #1-#6)
    - [x] 1.1: Add `Handle(AddUserToTenant, TenantState?)` with null/disabled/escalation/duplicate checks
    - [x] 1.2: Add `Handle(RemoveUserFromTenant, TenantState?)` with null/disabled/not-member checks
    - [x] 1.3: Add `Handle(ChangeUserRole, TenantState?)` with null/disabled/escalation/not-member/same-role checks
    - [x] 1.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create FluentValidation validators (AC: #8)
    - [x] 2.1: Add `FluentValidation` package reference to `Hexalith.Tenants.Server.csproj`
    - [x] 2.2: Create `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`
    - [x] 2.3: Create `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs`
    - [x] 2.4: Register validators in `Program.cs` and add a tenant-specific `SubmitCommand` payload validator in Hexalith.Tenants so inner command validation runs in the existing MediatR pipeline
    - [x] 2.5: Verify solution builds
    - [x] 2.6: Use the typed validators from the `SubmitCommand` MediatR validation path so `AddUserToTenant` and `ChangeUserRole` payloads are validated before domain dispatch

- [x] Task 3: Create unit tests (AC: #8, #9)
    - [x] 3.1: Add user-role Handle method tests to `TenantAggregateTests.cs` (16 test cases, use `[Theory]`/`[InlineData]` for AddUserToTenant across all 3 valid roles)
    - [x] 3.2: Add validator tests for `AddUserToTenantValidator`
    - [x] 3.3: Add validator tests for `ChangeUserRoleValidator` and the tenant-specific `SubmitCommand` payload validator
    - [x] 3.4: Verify all tests pass: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj` — 62/62 passed
    - [x] 3.5: Verify no contract regressions: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/` — 25/25 passed

- [x] Task 4: Build verification (all ACs)
    - [x] 4.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 4.2: Focused regression tests passed after fixes — Server.Tests 62/62, Contracts.Tests 25/25

## Dev Notes

### Developer Context

This story adds the **user-role management Handle methods** to the existing `TenantAggregate`. All supporting types already exist:

- **Commands** (from Story 2.1): `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole` — already in `Contracts/Commands/`
- **Events** (from Story 2.1): `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged` — already in `Contracts/Events/`
- **Rejections** (from Story 2.1): `UserAlreadyInTenantRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection` — already in `Contracts/Events/Rejections/` (`UserAlreadyInTenantRejection` now includes `ExistingRole`)
- **State** (from Story 2.3): `TenantState` already has all 3 Apply methods for user-role events (`Apply(UserAddedToTenant)`, `Apply(UserRemovedFromTenant)`, `Apply(UserRoleChanged)`)
- **Enum** (from Story 2.1): `TenantRole` with values `TenantOwner`, `TenantContributor`, `TenantReader`

### Security Note: TenantRole Enum Default Value (Out of Scope — Tracked Separately)

**`TenantRole.TenantOwner = 0`** — the highest-privilege role is the default enum value. If a malformed JSON payload omits the `Role` field, `System.Text.Json` deserializes it as `(TenantRole)0 = TenantOwner`. `Enum.IsDefined()` returns `true`, so the Handle method would accept it.

**DO NOT modify the TenantRole enum in this story** — the enum is defined in Contracts (Story 2.1) and may have serialized events using current ordinal values. Reordering would silently corrupt existing data (owners become readers).

**Recommended follow-up (file as separate tech-debt item):**

- Add explicit numeric values to prevent future drift: `TenantOwner = 0, TenantContributor = 1, TenantReader = 2`
- Enforce `[JsonRequired]` or `JsonSerializerOptions.RespectRequiredMembers = true` at Hexalith.Tenants level to reject payloads that omit `Role`
- This is a Hexalith.Tenants/serialization concern, not an aggregate concern

**This story adds:** 3 Handle methods, 2 typed validators, a tenant-specific `SubmitCommand` payload validator in Hexalith.Tenants, the `ExistingRole` field on `UserAlreadyInTenantRejection`, and focused tests. No new projects.

### Technical Requirements

**Handle Method Implementations (exact code to add to `TenantAggregate.cs`):**

```csharp
public static DomainResult Handle(AddUserToTenant command, TenantState? state)
{
    ArgumentNullException.ThrowIfNull(command);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        _ when !Enum.IsDefined(command.Role)
            => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.Role)]),
        _ when state.Users.ContainsKey(command.UserId)
            => DomainResult.Rejection([new UserAlreadyInTenantRejection(command.TenantId, command.UserId)]),
        _ => DomainResult.Success([new UserAddedToTenant(command.TenantId, command.UserId, command.Role)]),
    };
}

public static DomainResult Handle(RemoveUserFromTenant command, TenantState? state)
{
    ArgumentNullException.ThrowIfNull(command);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        _ when !state.Users.ContainsKey(command.UserId)
            => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
        _ => DomainResult.Success([new UserRemovedFromTenant(command.TenantId, command.UserId)]),
    };
}

public static DomainResult Handle(ChangeUserRole command, TenantState? state)
{
    ArgumentNullException.ThrowIfNull(command);
    return state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        _ when !Enum.IsDefined(command.NewRole)
            => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.NewRole)]),
        _ when !state.Users.ContainsKey(command.UserId)
            => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
        _ when state.Users[command.UserId] == command.NewRole
            => DomainResult.NoOp(),
        _ => DomainResult.Success([new UserRoleChanged(command.TenantId, command.UserId, state.Users[command.UserId], command.NewRole)]),
    };
}
```

**Critical Handle method notes:**

- All methods follow the established pattern: `public static`, pure, `DomainResult` return, nullable `TenantState?`
- `ArgumentNullException.ThrowIfNull(command)` required for CA1062 with `TreatWarningsAsErrors = true`
- **Disabled tenant guard:** All 3 commands check `Status: TenantStatus.Disabled` and reject with `TenantDisabledRejection`. Disabled tenants reject ALL commands except EnableTenant (NFR8)
- **Role escalation (defense-in-depth):** `!Enum.IsDefined(command.Role)` is a deserialization defense — catches undefined enum values (e.g., `(TenantRole)99` injected via raw JSON). This is NOT a GlobalAdministrator business rule; the `TenantRole` enum intentionally excludes GlobalAdministrator (GlobalAdmin is managed by `GlobalAdministratorAggregate`, not per-tenant roles). The type system prevents GlobalAdmin assignment at compile time; this check catches runtime integer casting attacks
- **Duplicate detection:** `state.Users.ContainsKey(command.UserId)` — the state property is `Users` (NOT `Members` — architecture uses `Members` in one example, but the actual implementation uses `Users`)
- **ChangeUserRole same-role:** Returns `NoOp()` when `state.Users[command.UserId] == command.NewRole` — consistent with idempotent patterns (DisableTenant, EnableTenant). **Note for Story 3.2:** This NoOp silently swallows same-role changes (no event produced). Story 3.2's role enforcement must not re-implement this check
- **ChangeUserRole OldRole:** Reads `state.Users[command.UserId]` to populate `UserRoleChanged.OldRole` — this is safe because the `ContainsKey` check precedes it

### Empty-String and Null Defense (Task 0 Gated)

Handle methods use `state.Users.ContainsKey(command.UserId)` — accepts empty strings, throws on `null`. Resolution depends on Task 0:

- **If validators fire:** `NotEmpty()` blocks empty/null before Handle. Handle methods are safe as written.
- **If validators do NOT fire:** Add `string.IsNullOrEmpty` guards before the switch expression in each Handle method. Note: this requires choosing a rejection type for invalid input — either reuse `TenantNotFoundRejection` or create a generic validation rejection (out of scope for this story; prefer Handle-only defense with existing rejection types).

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type                     | Project | Folder        | File                                   |
| ------------------------ | ------- | ------------- | -------------------------------------- |
| Handle methods           | Server  | `Aggregates/` | `TenantAggregate.cs` (MODIFY)          |
| AddUserToTenantValidator | Server  | `Validators/` | `AddUserToTenantValidator.cs` (CREATE) |
| ChangeUserRoleValidator  | Server  | `Validators/` | `ChangeUserRoleValidator.cs` (CREATE)  |

**DO NOT:**

- Create any new files in Contracts — all commands, events, rejections, enums are already defined
- Modify TenantState — all 3 user-role Apply methods already exist (from Story 2.3)
- Create new projects or new solution references
- Add role-based authorization checks in Handle methods — role enforcement is Story 3.2's scope
- Create a RemoveUserFromTenantValidator — architecture says simple commands rely on domain validation only
- Throw exceptions from Handle methods — use `DomainResult.Rejection()`
- Use `state.Members` — the property is `state.Users`
- Make Handle methods async — no async work needed

### Library & Framework Requirements

**New dependency needed in Server .csproj:**

```xml
<PackageReference Include="FluentValidation" />
```

FluentValidation 12.1.1 is already declared in `Directory.Packages.props` (centralized version management). The Server project needs the explicit reference to define `AbstractValidator<T>` subclasses.

**Validator registration needed in `Program.cs`:**

`AddHexalith.Tenants()` (from EventStore.Hexalith.Tenants) only registers EventStore's own validators via `AddValidatorsFromAssemblyContaining<SubmitCommandRequestValidator>()`. Tenant-specific validators must be separately registered:

```csharp
// Add after existing AddEventStore() call in Program.cs
builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
```

`AddValidatorsFromAssembly()` is an extension method from `FluentValidation.DependencyInjectionExtensions` — this is transitively available in Hexalith.Tenants via the `EventStore.Hexalith.Tenants` project reference. Add `using FluentValidation;` to `Program.cs` for the extension method to resolve.

**All other dependencies already available (NO new packages):**

- `DomainResult` — globally imported via Server .csproj (`Hexalith.EventStore.Contracts.Results`)
- `IEventPayload` / `IRejectionEvent` — globally imported via Server .csproj (`Hexalith.EventStore.Contracts.Events`)
- All Tenants.Contracts types — via Server → Contracts ProjectReference
- xUnit 2.9.3, Shouldly 4.3.0 — test infrastructure from Story 2.2

### File Structure Requirements

**Modified files:**

```
src/Hexalith.Tenants.Server/
├── Hexalith.Tenants.Server.csproj     (ADD FluentValidation PackageReference)
├── Aggregates/
│   └── TenantAggregate.cs             (ADD 3 Handle methods)
└── Validators/                        (CREATE folder)
    ├── AddUserToTenantValidator.cs    (CREATE)
    └── ChangeUserRoleValidator.cs     (CREATE)

src/Hexalith.Tenants/
└── Program.cs                         (ADD validator registration line + using)

tests/Hexalith.Tenants.Server.Tests/
├── Aggregates/
│   └── TenantAggregateTests.cs        (ADD 15 Handle method test methods)
└── Validators/                        (CREATE folder)
    ├── AddUserToTenantValidatorTests.cs  (CREATE — 3 tests)
    └── ChangeUserRoleValidatorTests.cs   (CREATE — 3 tests)
```

**FluentValidation Validators (exact implementations):**

```csharp
// src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs
using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class AddUserToTenantValidator : AbstractValidator<AddUserToTenant>
{
    public AddUserToTenantValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
```

```csharp
// src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs
using FluentValidation;

using Hexalith.Tenants.Contracts.Commands;

namespace Hexalith.Tenants.Server.Validators;

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRole>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}
```

**Style note:** Do NOT use `_ = RuleFor(...)` discard pattern. EventStore's own validators (`SubmitCommandRequestValidator`, etc.) call `RuleFor(...)` directly without discards. Match their established style. If IDE0058 fires, it should be suppressed at the .editorconfig level (check existing config), not worked around with discards.

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Part A: Handle method tests** — Add 15 test methods to existing `TenantAggregateTests.cs`. Use the established `ProcessAsync(CommandEnvelope, state)` pattern with the existing `CreateCommand<T>` helper.

For Test #1 (AddUserToTenant success), use `[Theory]` with `[InlineData]` to cover all 3 valid roles (TenantOwner, TenantContributor, TenantReader) instead of testing only one role:

```csharp
[Theory]
[InlineData(TenantRole.TenantOwner)]
[InlineData(TenantRole.TenantContributor)]
[InlineData(TenantRole.TenantReader)]
public async Task AddUserToTenant_on_active_tenant_produces_UserAddedToTenant(TenantRole role) { ... }
```

**Test cases:**

| #   | Given                                        | When                                                        | Then                                                                                        | AC     |
| --- | -------------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------------------------- | ------ |
| 1   | Active tenant, no members                    | AddUserToTenant("acme", "user-1", {all 3 roles via Theory}) | Success: UserAddedToTenant                                                                  | #1     |
| 2   | Null state                                   | AddUserToTenant("acme", "user-1", TenantReader)             | Rejection: TenantNotFoundRejection                                                          | #1     |
| 3   | Disabled tenant                              | AddUserToTenant("acme", "user-1", TenantReader)             | Rejection: TenantDisabledRejection                                                          | #1     |
| 4   | Active tenant, user-1 already member         | AddUserToTenant("acme", "user-1", TenantOwner)              | Rejection: UserAlreadyInTenantRejection                                                     | #2     |
| 5   | Active tenant, undefined role (TenantRole)99 | AddUserToTenant("acme", "user-1", (TenantRole)99)           | Rejection: RoleEscalationRejection                                                          | #6     |
| 6   | Active tenant, user-1 is member              | RemoveUserFromTenant("acme", "user-1")                      | Success: UserRemovedFromTenant                                                              | #3     |
| 7   | Null state                                   | RemoveUserFromTenant("acme", "user-1")                      | Rejection: TenantNotFoundRejection                                                          | #3     |
| 8   | Disabled tenant                              | RemoveUserFromTenant("acme", "user-1")                      | Rejection: TenantDisabledRejection                                                          | #3     |
| 9   | Active tenant, user-1 not member             | RemoveUserFromTenant("acme", "user-2")                      | Rejection: UserNotInTenantRejection                                                         | #4     |
| 10  | Active tenant, user-1 is TenantReader        | ChangeUserRole("acme", "user-1", TenantContributor)         | Success: UserRoleChanged(OldRole=Reader, NewRole=Contributor)                               | #5     |
| 11  | Null state                                   | ChangeUserRole("acme", "user-1", TenantContributor)         | Rejection: TenantNotFoundRejection                                                          | #5     |
| 12  | Disabled tenant                              | ChangeUserRole("acme", "user-1", TenantContributor)         | Rejection: TenantDisabledRejection                                                          | #5     |
| 13  | Active tenant, user-1 not member             | ChangeUserRole("acme", "user-2", TenantContributor)         | Rejection: UserNotInTenantRejection                                                         | #5     |
| 14  | Active tenant, user-1 is TenantReader        | ChangeUserRole("acme", "user-1", TenantReader)              | NoOp (same role)                                                                            | #5     |
| 15  | Active tenant, undefined role (TenantRole)99 | ChangeUserRole("acme", "user-1", (TenantRole)99)            | Rejection: RoleEscalationRejection                                                          | #6     |
| 16  | Disabled tenant, user-1 is member            | AddUserToTenant("acme", "user-1", TenantOwner)              | Rejection: TenantDisabledRejection (NOT UserAlreadyInTenant — verifies switch arm ordering) | #1, #2 |

**How to build state for tests (reuse existing patterns):**

```csharp
// Active tenant with one member
var state = new TenantState();
state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

// Disabled tenant
state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));
```

**Assertion patterns (consistent with existing tests):**

```csharp
// Success — verify event fields
result.IsSuccess.ShouldBeTrue();
result.Events.Count.ShouldBe(1);
IEventPayload evt = result.Events[0].ShouldBeOfType<UserAddedToTenant>();
((UserAddedToTenant)evt).TenantId.ShouldBe("acme");
((UserAddedToTenant)evt).UserId.ShouldBe("user-1");
((UserAddedToTenant)evt).Role.ShouldBe(TenantRole.TenantReader);

// Rejection
result.IsRejection.ShouldBeTrue();
result.Events[0].ShouldBeOfType<UserAlreadyInTenantRejection>();

// NoOp
result.IsNoOp.ShouldBeTrue();
result.Events.Count.ShouldBe(0);

// ChangeUserRole — verify OldRole and NewRole
var roleEvt = (UserRoleChanged)result.Events[0];
roleEvt.OldRole.ShouldBe(TenantRole.TenantReader);
roleEvt.NewRole.ShouldBe(TenantRole.TenantContributor);
```

**Part B: FluentValidation validator tests** — Create 2 new test files in `tests/Hexalith.Tenants.Server.Tests/Validators/`. These test AC #8 (structural validation before domain logic).

Test validators directly using `AbstractValidator<T>.Validate()` — no MediatR pipeline needed:

```csharp
// AddUserToTenantValidatorTests.cs
using FluentValidation.TestHelper;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Validators;

namespace Hexalith.Tenants.Server.Tests.Validators;

public class AddUserToTenantValidatorTests
{
    private readonly AddUserToTenantValidator _validator = new();

    [Fact]
    public void Should_have_error_when_TenantId_is_empty()
        => _validator.TestValidate(new AddUserToTenant("", "user-1", TenantRole.TenantReader))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void Should_have_error_when_UserId_is_empty()
        => _validator.TestValidate(new AddUserToTenant("acme", "", TenantRole.TenantReader))
            .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void Should_have_error_when_Role_is_invalid_enum()
        => _validator.TestValidate(new AddUserToTenant("acme", "user-1", (TenantRole)99))
            .ShouldHaveValidationErrorFor(x => x.Role);
}
```

Follow the same pattern for `ChangeUserRoleValidatorTests.cs` (validate TenantId, UserId, NewRole).

**Note:** `FluentValidation.TestHelper` is included in the base `FluentValidation` package — no additional test package needed. Server.Tests already references Server (which will reference FluentValidation after Task 2.1).

### Previous Story Intelligence

**Story 2.3 (done) — TenantAggregate lifecycle:**

- TenantAggregate has 4 Handle methods: CreateTenant, UpdateTenant, DisableTenant, EnableTenant
- TenantState has ALL 9 Apply methods already — user-role Apply methods are ready
- `ArgumentNullException.ThrowIfNull()` required on ALL method parameters (CA1062 + TreatWarningsAsErrors)
- State property is `Users` (not `Members`) — `Dictionary<string, TenantRole>`
- Test file `TenantAggregateTests.cs` has 12 existing tests + `CreateCommand<T>` helper

**Story 2.4 (review) — Hexalith.Tenants bootstrap:**

- Program.cs is fully wired: `AddHexalith.Tenants()`, `AddEventStoreServer()`, `AddEventStore(typeof(TenantAggregate).Assembly)`
- `AddHexalith.Tenants()` sets up MediatR pipeline with ValidationBehavior but only registers EventStore's own validators
- Validator registration for Tenants assembly NOT yet done — must add `AddValidatorsFromAssembly()` in this story

**Story 2.1 (done) — Contracts:**

- All 3 user-role commands exist: `AddUserToTenant(TenantId, UserId, Role)`, `RemoveUserFromTenant(TenantId, UserId)`, `ChangeUserRole(TenantId, UserId, NewRole)`
- All 3 user-role events exist: `UserAddedToTenant(TenantId, UserId, Role)`, `UserRemovedFromTenant(TenantId, UserId)`, `UserRoleChanged(TenantId, UserId, OldRole, NewRole)`
- All user-role rejections exist: `UserAlreadyInTenantRejection(TenantId, UserId)`, `UserNotInTenantRejection(TenantId, UserId)`, `RoleEscalationRejection(TenantId, UserId, AttemptedRole)`
- Naming convention and serialization tests already cover these types

**Key learnings applied:**

- CA1062 → `ArgumentNullException.ThrowIfNull()` on all reference type parameters
- `TreatWarningsAsErrors = true` → all warnings are build failures
- `.editorconfig` → file-scoped namespaces, Allman braces, 4-space indent
- `using Hexalith.Tenants.Contracts.Events;` is different from globally imported `Hexalith.EventStore.Contracts.Events;` — both needed
- Test pattern: `ProcessAsync(CommandEnvelope, state)`, NOT direct Handle method calls

### Critical Anti-Patterns (DO NOT)

- **DO NOT** create new contract types — AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, and all rejections already exist
- **DO NOT** modify TenantState — all Apply methods already exist from Story 2.3
- **DO NOT** call Handle methods directly in tests — use `aggregate.ProcessAsync(commandEnvelope, state)`
- **DO NOT** add role-based authorization checks (TenantOwner/Reader/Contributor enforcement) — that is Story 3.2
- **DO NOT** use `state.Members` — the property is `state.Users`
- **DO NOT** throw exceptions from Handle methods — use `DomainResult.Rejection()`
- **DO NOT** create a RemoveUserFromTenantValidator — architecture says simple commands use domain validation only
- **DO NOT** add constructor parameters to TenantAggregate — framework uses `Activator.CreateInstance()`
- **DO NOT** assert exact `DateTimeOffset` values — timestamps use `DateTimeOffset.UtcNow`
- **DO NOT** add `[JsonPropertyName]` attributes — System.Text.Json camelCase is the default

### Project Structure Notes

- Server `Aggregates/` folder exists — only modify `TenantAggregate.cs`
- Server `Validators/` folder does NOT exist — create it with 2 validator files
- Server.Tests `Aggregates/` folder exists — modify existing `TenantAggregateTests.cs`
- Program.cs exists — add one line for validator registration
- No new projects, no new test projects, no new solution entries

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1] — Story definition, ACs, BDD scenarios
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns] — Handle/Apply patterns, three-outcome model, disabled tenant guard
- [Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns] — Command/event/rejection naming conventions
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns] — Type location rules (validators in Server)
- [Source: _bmad-output/planning-artifacts/architecture.md#Validation Pattern] — FluentValidation for complex commands, domain validation in Handle
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs] — Existing 4 lifecycle Handle methods to extend
- [Source: src/Hexalith.Tenants.Server/Aggregates/TenantState.cs] — State with Users dictionary and existing Apply methods
- [Source: src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs] — `record AddUserToTenant(string TenantId, string UserId, TenantRole Role)`
- [Source: src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs] — `record RemoveUserFromTenant(string TenantId, string UserId)`
- [Source: src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs] — `record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole)`
- [Source: src/Hexalith.Tenants.Contracts/Events/UserAddedToTenant.cs] — `record UserAddedToTenant(string TenantId, string UserId, TenantRole Role) : IEventPayload`
- [Source: src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs] — `record UserRemovedFromTenant(string TenantId, string UserId) : IEventPayload`
- [Source: src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs] — `record UserRoleChanged(string TenantId, string UserId, TenantRole OldRole, TenantRole NewRole) : IEventPayload`
- [Source: tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs] — Existing test patterns and CreateCommand helper
- [Source: _bmad-output/implementation-artifacts/2-3-tenant-aggregate-lifecycle.md] — Previous story with CA1062 learnings, test patterns
- [Source: _bmad-output/implementation-artifacts/2-4-commandapi-bootstrap-and-event-publishing.md] — Program.cs wiring, validator registration gap
- [Source: _bmad-output/planning-artifacts/architecture.md#Handle Method] — FR10 (role escalation) mapped to `Enum.IsDefined()` deserialization defense; architecture example uses `state.Members.ContainsKey` but actual implementation uses `state.Users` (architecture doc typo)
- [Source: _bmad-output/planning-artifacts/prd.md#FR12] — Optimistic concurrency (AC #7) — infrastructure-guaranteed by EventStore actor model, no domain code needed
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Hexalith.Tenants/Pipeline/ValidationBehavior.cs] — Task 0: verify domain command validator integration

## Change Log

- 2026-03-15: Task 0 — Investigated FluentValidation pipeline integration. Confirmed typed domain-command validators do not execute automatically unless the inner payload is validated from the `SubmitCommand` pipeline path.
- 2026-03-15: Task 1 — Added 3 Handle methods (AddUserToTenant, RemoveUserFromTenant, ChangeUserRole) to TenantAggregate with null/disabled/escalation/duplicate/not-member checks.
- 2026-03-15: Task 3 — Added 16 unit tests covering all 3 Handle methods (success, rejection, NoOp paths). [Theory] with [InlineData] for all 3 TenantRole values. Switch arm ordering verified (disabled takes precedence over duplicate member).
- 2026-03-15: Task 4 — Full solution build (0 warnings, 0 errors) and all 81 tests pass across 5 test projects with no regressions.
- 2026-03-16: Senior developer review completed. Story moved back to in-progress after finding an unmet validation acceptance criterion and a duplicate-user rejection contract gap.
- 2026-03-16: Fix follow-up — Added typed validators plus a tenant-specific `SubmitCommand` payload validator, registered validators in Hexalith.Tenants, extended `UserAlreadyInTenantRejection` with `ExistingRole`, and added focused regression tests. Story returned to done.

## Dev Agent Record

### Agent Model Used

GPT-5.4

### Debug Log References

- Task 0: ValidationBehavior<TRequest> validates `SubmitCommand` (MediatR envelope). Inner tenant commands require an additional validator that deserializes `SubmitCommand.Payload` and delegates to typed validators before domain dispatch.

### Completion Notes List

- Task 0: FluentValidation pipeline investigation complete. Direct typed validators do not run automatically on deserialized domain commands, so the fix adds a tenant-specific `SubmitCommand` validator that invokes typed validators from the actual MediatR pipeline.
- Task 1: Added 3 Handle methods to TenantAggregate following established patterns (static, pure, DomainResult return). All methods include null state, disabled tenant, and domain-specific guards. ChangeUserRole returns NoOp for same-role (consistent with DisableTenant/EnableTenant idempotent patterns).
- Task 2: Added `AddUserToTenantValidator`, `ChangeUserRoleValidator`, and `TenantSubmitCommandValidator`. Registered both Tenants assemblies for FluentValidation discovery in Hexalith.Tenants.
- Task 3: Added aggregate assertions for `ExistingRole`, 2 typed validator test files, and `TenantSubmitCommandValidatorTests`. Server.Tests now pass 62/62 and Contracts.Tests pass 25/25.
- Task 4: Full Release build with 0 warnings/errors after fixes.

### File List

- `src/Hexalith.Tenants.Contracts/Events/Rejections/UserAlreadyInTenantRejection.cs` — MODIFIED: Added `ExistingRole` to the duplicate-user rejection payload
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` — MODIFIED: Added 3 Handle methods (AddUserToTenant, RemoveUserFromTenant, ChangeUserRole)
- `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` — MODIFIED: Added `FluentValidation` package reference
- `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs` — NEW: Typed validator for AddUserToTenant payloads
- `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs` — NEW: Typed validator for ChangeUserRole payloads
- `src/Hexalith.Tenants/Program.cs` — MODIFIED: Registered Tenants validators with FluentValidation discovery
- `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs` — NEW: MediatR-pipeline validator that deserializes tenant command payloads and delegates to typed validators
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` — MODIFIED: Added `ExistingRole` assertion for duplicate-user rejection
- `tests/Hexalith.Tenants.Server.Tests/Validators/AddUserToTenantValidatorTests.cs` — NEW: Validator tests for AddUserToTenant
- `tests/Hexalith.Tenants.Server.Tests/Validators/ChangeUserRoleValidatorTests.cs` — NEW: Validator tests for ChangeUserRole
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs` — NEW: Pipeline-level payload validation tests for tenant commands

## Senior Developer Review (AI)

**Reviewer:** Jerome
**Date:** 2026-03-16
**Outcome:** Approved after fixes

### Summary

- Git working tree review: `git status --porcelain`, `git diff --name-only`, and `git diff --cached --name-only` were all empty. No uncommitted or staged file discrepancies were present during review.
- Validation context reviewed: `Hexalith.EventStore/src/Hexalith.EventStore.Hexalith.Tenants/Pipeline/ValidationBehavior.cs`
- Verification run:
    - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore` → 53/53 passed
    - `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore` → 25/25 passed

### Resolution

- Added `AddUserToTenantValidator` and `ChangeUserRoleValidator` in `Hexalith.Tenants.Server`.
- Added `TenantSubmitCommandValidator` in `Hexalith.Tenants` so tenant payload validation now runs on `SubmitCommand` inside the existing MediatR pipeline.
- Registered validator discovery for both Hexalith.Tenants and Server assemblies in `Program.cs`.
- Extended `UserAlreadyInTenantRejection` with `ExistingRole` and updated `TenantAggregate` plus aggregate tests accordingly.
- Verification after fixes:
    - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj` → 62/62 passed
    - `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj` → 25/25 passed
    - `dotnet build Hexalith.Tenants.slnx --configuration Release` → succeeded

### Findings

1. **[CRITICAL] AC #8 is not implemented, and Tasks 0.4 / 2.6 overstate completion**
   Story AC #8 requires FluentValidation in the MediatR pipeline for non-empty `TenantId`, non-empty `UserId`, and valid enum values (`_bmad-output/implementation-artifacts/3-1-user-role-management.md:43-45`). The story then claims validators were skipped because they would be dead code and that the Handle methods now provide full validation (`...md:57`, `...md:71`). However:
    - `ValidationBehavior<TRequest, TResponse>` validates the outer MediatR request type, not the deserialized inner tenant command (`Hexalith.EventStore/src/Hexalith.EventStore.Hexalith.Tenants/Pipeline/ValidationBehavior.cs:14-25`, `:36-43`).
    - `src/Hexalith.Tenants/Program.cs:15-23` does not register any tenant-command validators.
    - `src/Hexalith.Tenants.Server/` contains no `Validators/` folder, and `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` has no `FluentValidation` package reference.
    - The Handle methods only validate enum values and membership state; they do **not** reject empty identifiers before producing events or probing the dictionary (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:52-90`).

    **Impact:** malformed commands can still pass aggregate validation. For example, empty-string identifiers are currently accepted on the success paths, and a null `UserId` can reach `state.Users.ContainsKey(command.UserId)` and fail with an exception instead of a controlled rejection. This leaves AC #8 unmet and means the completed-task narrative is inaccurate.

2. **[HIGH] AC #2 is only partially implemented — duplicate-user rejection omits the existing role**
   The story requires `UserAlreadyInTenantRejection` to include the existing role information (`_bmad-output/implementation-artifacts/3-1-user-role-management.md:19-21`). The emitted rejection currently contains only `TenantId` and `UserId`:
    - Contract: `src/Hexalith.Tenants.Contracts/Events/Rejections/UserAlreadyInTenantRejection.cs:3`
    - Emission site: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:61-62`

    **Impact:** callers cannot learn the user's current role from the rejection payload, so the acceptance criterion is not fully satisfied.

### Recommendation

Return this story to development. Fix the validation gap first, then decide whether AC #2 should be implemented by extending `UserAlreadyInTenantRejection` with the existing role or by revising the acceptance criterion/story if the contract must stay unchanged.
