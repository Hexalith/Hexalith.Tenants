---
baseline_commit: 596759dc5956d15bc60bd5036e0ddee1c4d443f5
---

# Story 6.1: Provide In-Memory Tenant Test Fakes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want in-memory tenant test fakes that require no external infrastructure,
so that I can write tenant integration tests quickly in ordinary unit-test projects.

## Acceptance Criteria

1. Given a test project references `Hexalith.Tenants.Testing`, when a developer creates the in-memory tenant fake using the documented helper, then the fake can execute tenant commands without DAPR, Aspire, Docker, or a live EventStore process, and setup remains small enough for the documented under-10-lines target.
2. Given the fake is initialized, when a test submits tenant lifecycle, membership, role, and configuration commands, then the fake returns success, rejection, or no-op outcomes using the same domain result semantics as production, and events are retained in memory for test assertions.
3. Given a test needs deterministic setup, when the fake is created for a new test, then it starts from an isolated empty state unless seeded explicitly, and previous tests cannot leak tenant state into the new test.
4. Given invalid commands are submitted to the fake, when business rules reject the command, then the fake exposes the same structured rejection event type expected from production domain logic, and no infrastructure exception is required to assert the business failure.
5. Given fake setup tests run, when basic create, add-user, remove-user, role-change, set-configuration, and rejection flows are exercised, then tests verify the fake can be used without external infrastructure, and command execution remains fast enough to support ordinary unit-test workflows.
6. Given the testing fakes expose EventStore's `DomainResult` as their public result type (TEN-5 decision), when the public `Hexalith.Tenants.Testing` surface is reviewed, then returning `DomainResult` is documented as intentional in an architecture decision record because the type is in-tier and reused by consuming-service tests without added coupling, and no wrapper type and no consuming-service architecture-fitness restriction are introduced.

## Tasks / Subtasks

- [x] Review and reconcile the existing `InMemoryTenantService` implementation with Story 6.1 ACs (AC: 1, 2, 3, 4, 5, 6)
  - [x] Confirm `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs` remains the single in-memory tenant command fake; do not create a second fake or alternate service name.
  - [x] Confirm it requires no DAPR, Aspire, Docker, actor runtime, live EventStore process, Redis, broker, HTTP host, or `DaprClient`.
  - [x] Keep public command outcomes as `Hexalith.EventStore.Contracts.Results.DomainResult`; do not add a Tenants-owned result wrapper.
  - [x] Preserve per-instance in-memory state isolation: each `new InMemoryTenantService()` starts empty unless this story explicitly adds documented seeding.
  - [x] Preserve in-memory event retention through `EventHistory` for successful events only; rejection and no-op results must not mutate tenant/global-admin state or append success history.
- [x] Verify tenant command coverage and semantics (AC: 2, 4, 5)
  - [x] Support lifecycle flows: `CreateTenant`, `DisableTenant`, `EnableTenant`.
  - [x] Support membership and role flows: `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`.
  - [x] Support configuration flows: `SetTenantConfiguration`, `RemoveTenantConfiguration`.
  - [x] Return structured rejection events for duplicate tenant/user, disabled tenant, insufficient permission, role escalation, missing tenant, lifecycle no-op, and invalid configuration cases where production aggregate logic returns those rejections.
  - [x] Do not throw for business-rule failures; throws are acceptable only for programmer errors such as null command arguments or unsupported command types.
- [x] Verify helper ergonomics (AC: 1, 3, 6)
  - [x] Keep `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs` as the documented helper surface for short test setup.
  - [x] Provide helpers for common setup: bootstrap global admin, create tenant, create tenant with owner, and create command envelope.
  - [x] Require explicit `aggregateId` in envelope helpers; do not use `dynamic` or reflection to infer tenant ID from arbitrary command payloads.
  - [x] Ensure examples/tests demonstrate the under-10-lines target for common tenant setup.
- [x] Verify tests and add missing Story 6.1 coverage (AC: 1, 2, 3, 4, 5, 6)
  - [x] Review `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` for create, add-user, remove-user, role-change, set-configuration, rejection, isolation, event-history, and no-infrastructure coverage.
  - [x] Review `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs` for helper output and envelope fields.
  - [x] Add missing tests for configuration commands if current tests do not exercise both set and remove configuration paths.
  - [x] Add or preserve deterministic isolation tests proving state does not leak between two service instances and tenant A state does not contain tenant B members.
  - [x] Keep performance evidence lightweight: Tier 1 only, no Docker/DAPR dependency, and target p95 under 10ms for ordinary in-memory command setup.
- [x] Confirm documentation/ADR alignment (AC: 1, 6)
  - [x] Confirm architecture documents TEN-5: Testing fakes intentionally return EventStore `DomainResult`.
  - [x] If public helper usage is not documented in tests or docs, add a concise usage example in the appropriate project documentation without introducing a new package or API layer.
- [x] Validate locally
  - [x] `dotnet build Hexalith.Tenants.slnx --configuration Release`
  - [x] `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release`
  - [x] If broader changes are made, also run `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"`

## Dev Notes

- This story is a reconciliation story against the current workspace state. Candidate implementation already exists in `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`, `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`, and `tests/Hexalith.Tenants.Testing.Tests/*`; dev work should audit, adjust, and complete coverage instead of recreating the package surface. [Source: repository scan; `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`]
- Epic 6 goal: developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`]
- `Hexalith.Tenants.Testing` guarantees isolation at the aggregate domain model level: command validation, event production, and state transitions. Projection-level and query-level isolation remains the consuming service's responsibility. [Source: `_bmad-output/planning-artifacts/prd.md#Isolation-Invariant-Guarantee-Through-Testing-Fakes`]
- Component boundary: `Testing` provides in-memory fakes and helpers without changing production domain behavior. New test helpers/fakes go in `Testing`; in-memory fake parity tests go in `Testing.Tests`. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`; `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`]
- TEN-5 decision: `InMemoryTenantService` and `TenantTestHelpers` return `Hexalith.EventStore.Contracts.Results.DomainResult` intentionally; do not add wrappers or consumer fitness restrictions. [Source: `_bmad-output/planning-artifacts/architecture.md#Readiness-Correction-Decisions`]
- Testing package can depend on `Server` for production domain logic where required, but `Contracts` and `Server` must not depend back on `Testing`. [Source: `_bmad-output/planning-artifacts/epics.md#Story-6.2-Reuse-Production-Aggregate-Logic-In-Testing-Fakes`]
- Do not initialize nested submodules. If submodules are needed for build/test, initialize only the root-level submodules listed in `AGENTS.md`.

### Existing Code To Reuse

- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`: currently stores tenant states in memory, stores global-admin state, exposes `EventHistory`, and has strongly typed `ProcessCommand` overloads plus an explicit-envelope `ProcessTenantCommand<T>` path.
- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`: currently provides bootstrap, create tenant, create tenant with owner, and explicit aggregate-ID envelope helpers.
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` and `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`: production tenant domain logic and state application methods. Reuse these; do not duplicate tenant business rules in Testing.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs` and `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`: production global-admin domain logic and state application methods. Reuse these for global-admin fake behavior.
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` and `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs`: current Tier 1 coverage that should be extended rather than replaced.

### Current State Notes

- `InMemoryTenantService` currently applies tenant/global-admin events only when `DomainResult.IsSuccess` is true. Preserve this invariant: rejections and no-op results must not mutate state or success event history.
- `ProcessTenantCommand<T>` currently throws `InvalidOperationException` for unknown tenant command types. Preserve this fail-fast behavior for unsupported command types.
- Event application currently ignores unknown event payloads in default switch branches. If you change this to fail fast, add tests and confirm it does not block forward-compatible test scenarios. If you keep ignore behavior, make sure Story 6.1 tests cover all known tenant/global-admin event types that this fake claims to support.
- Existing helper envelope creation uses `TenantIdentity.DefaultTenantId`, `TenantIdentity.Domain`, `TenantIdentity.GlobalAdministratorsDomain`, `TenantIdentity.GlobalAdministratorsAggregateId`, and `actor:globalAdmin` extension semantics. Preserve these identity constants; do not hard-code alternate platform tenant/domain literals.
- The archived legacy file `_bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/6-1-in-memory-tenant-service-and-test-helpers.md` is historical context only. Current acceptance criteria in `_bmad-output/planning-artifacts/epics.md` are authoritative.

### Testing Standards

- Tier 1 only for this story. Tests must run without DAPR, Aspire, Docker, actors, Redis, broker, or HTTP host. [Source: `_bmad-output/project-context.md#Testing-Rules`]
- Use xUnit v3 attributes and Shouldly assertions. Do not use xUnit v2 packages and do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Test classes use plural `{TypeUnderTest}Tests.cs`; test method names use `snake_case_with_PascalCase_for_type_names`; tests mirror source folders. [Source: `_bmad-output/project-context.md#Test-Naming-Layout`]
- Every test must contain at least one Shouldly assertion and must not use placeholder assertions. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Use the local package pins and central package management. Do not add inline `Version=` attributes to `PackageReference`. [Source: `_bmad-output/project-context.md#Package-Management`]

### Latest Technical Notes

- The repository pins .NET SDK `10.0.300` in `global.json`; Microsoft lists .NET 10.0 SDK `10.0.300` as the current .NET 10 SDK release on the official download page as of this story creation. Do not bump the SDK for this story. [Source: `_bmad-output/project-context.md#Technology-Stack-Versions`; https://dotnet.microsoft.com/en-us/download/dotnet/10.0]
- xUnit v3 `3.2.2` and Shouldly `4.3.0` are the project-approved test packages. NuGet shows newer or separate package metadata may exist for some test-platform packages, but this story must use repository pins unless a separate dependency-update story is created. [Source: `_bmad-output/project-context.md#Testing`; https://www.nuget.org/packages/xunit.v3; https://www.nuget.org/packages/shouldly/]

### Project Structure Notes

- Expected files to review/update:
  - `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
  - `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
  - `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs`
  - Optional docs only if helper usage is not already adequately documented.
- Do not add new projects, new infrastructure fixtures, Docker/Testcontainers dependencies, DAPR components, or AppHost changes for Story 6.1.
- Do not move domain logic into `Testing`; the fake should wrap production aggregate/state behavior, not fork it.
- Story 6.2 owns deeper production-aggregate reuse hardening. Story 6.3 owns production/fake conformance tests. Implement only Story 6.1 scope unless a missing Story 6.1 AC cannot pass without a minimal prerequisite.

### References

- `_bmad-output/planning-artifacts/epics.md#Story-6.1-Provide-In-Memory-Tenant-Test-Fakes`
- `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`
- `_bmad-output/planning-artifacts/prd.md#Isolation-Invariant-Guarantee-Through-Testing-Fakes`
- `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`
- `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`
- `_bmad-output/project-context.md#Testing-Rules`
- `_bmad-output/project-context.md#Technology-Stack-Versions`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Added Story 6.1 coverage for role-change, configuration set/remove, configuration no-op, invalid configuration rejection, and global-admin envelope identity.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release` initially hit sandbox VSTest TCP listener restrictions; validated with the built xUnit v3 in-process runner instead.
- 2026-06-01: Restored `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj` with cached packages using `--ignore-failed-sources`/`NuGetAudit=false` because network access to NuGet is blocked in this environment.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` passed with `DOTNET_CLI_HOME=/tmp`, `NUGET_PACKAGES=/home/administrator/.nuget/packages`, build servers disabled, and single-node MSBuild.
- 2026-06-01: Senior review replaced remaining hard-coded testing fake tenant identity literals with `TenantIdentity` constants and converted the Testing smoke test from a placeholder xUnit assertion to a real Shouldly assertion.
- 2026-06-01: Senior review reran focused xUnit v3 in-process tests and Release solution build successfully.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Reconciled the existing `InMemoryTenantService` and `TenantTestHelpers` surfaces against Story 6.1; no production fake/helper API changes were required.
- Added Tier 1 tests for `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`, configuration no-op behavior, invalid configuration rejection state safety, and global-administrator envelope identity.
- Senior review auto-fixed the remaining Story 6.1 identity-constant and test-style issues without changing the public fake/helper API.
- Confirmed TEN-5 is documented in `_bmad-output/planning-artifacts/architecture.md` and that tests/helper usage already provide concise public usage examples without adding docs or a new API layer.
- Validation passed via Release solution build and xUnit v3 in-process test execution for the available non-integration assemblies.

### File List

- _bmad-output/implementation-artifacts/6-1-provide-in-memory-tenant-test-fakes.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/story-automator/orchestration-5-20260601-061130.md
- src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs
- tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs
- tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs
- tests/Hexalith.Tenants.Testing.Tests/ScaffoldingSmokeTests.cs

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex
Date: 2026-06-01
Outcome: Approved after auto-fixes

Findings:
- HIGH: `InMemoryTenantService` still hard-coded the platform tenant/domain literals instead of using `TenantIdentity.DefaultTenantId` and `TenantIdentity.Domain`, conflicting with the identity-constant task and increasing drift risk. Fixed in `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`.
- MEDIUM: `tests/Hexalith.Tenants.Testing.Tests/ScaffoldingSmokeTests.cs` used `Assert.True(true)`, violating the project Shouldly/no-placeholder assertion rule. Replaced with a real Shouldly assertion against the Testing assembly.
- MEDIUM: Story File List did not include review-touched source, smoke test, and automation artifacts. Updated this story's File List.

Validation:
- `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` compiled, then VSTest aborted on sandbox socket setup (`SocketException (13): Permission denied`).
- `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -parallel none` passed: 107 total, 0 errors, 0 failed, 0 skipped.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` passed: 0 warnings, 0 errors.

### Change Log

- 2026-06-01: Added missing Story 6.1 tenant fake coverage for role changes, configuration success/no-op/rejection flows, and helper global-admin envelope identity; moved story to review after validation.
- 2026-06-01: Senior review auto-fixed identity-constant usage, smoke-test assertion quality, story File List completeness, and moved story to done after validation.
