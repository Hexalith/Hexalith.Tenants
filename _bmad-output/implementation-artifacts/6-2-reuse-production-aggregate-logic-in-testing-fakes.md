---
baseline_commit: 23990aa
---

# Story 6.2: Reuse Production Aggregate Logic in Testing Fakes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want in-memory fakes to execute the same aggregate logic as production,
so that my tests do not pass against behavior that can drift from the deployed service.

## Acceptance Criteria

1. Given the testing fake handles a tenant command, when the fake evaluates the command, then it invokes the same pure aggregate `Handle` logic used by production, and it applies resulting events through the same state `Apply` methods.
2. Given production aggregate logic changes, when the testing package is built and tested, then fake behavior changes with the production aggregate behavior, and no duplicate hand-written fake rules must be maintained separately.
3. Given a command depends on command-envelope identity, aggregate ID, or trusted global-admin metadata, when the fake executes the command, then the fake supplies equivalent envelope context through documented test helpers, and command bodies cannot override aggregate identity in fake execution.
4. Given domain business rules reject a command, when the same command is executed through production aggregate logic and through the fake, then both paths produce equivalent structured rejection outcomes, and tests verify equality without relying on localized message text.
5. Given fake implementation code is reviewed, when maintainers inspect dependencies, then the Testing package can depend on Server for production domain logic where required, and it does not introduce reverse dependencies from Contracts or Server into Testing.

## Tasks / Subtasks

- [x] Audit `InMemoryTenantService` for production aggregate delegation (AC: 1, 2, 5)
  - [x] Confirm every tenant command path calls `TenantAggregate.Handle(...)` and does not duplicate tenant lifecycle, membership, role, configuration, disabled-tenant, or permission business rules in Testing.
  - [x] Confirm global-admin command paths call `GlobalAdministratorsAggregate.Handle(...)` and do not duplicate bootstrap, set, remove, last-admin, or unauthorized rules in Testing.
  - [x] Confirm successful tenant/global-admin events are applied by calling `TenantState.Apply(...)` and `GlobalAdministratorsState.Apply(...)`, not by mutating fake-owned state fields directly.
  - [x] Preserve the Story 6.1 invariant: rejection and no-op results must not mutate state or append to `EventHistory`.
  - [x] Keep `Hexalith.Tenants.Testing` dependent on `Hexalith.Tenants.Server` and `Hexalith.Tenants.Contracts`; do not add any reverse dependency from `Contracts` or `Server` to `Testing`.
- [x] Harden envelope and aggregate-identity handling (AC: 3)
  - [x] Ensure fake execution derives the effective aggregate identity from `CommandEnvelope.AggregateId` where the production aggregate does, especially for the low-level `ProcessTenantCommand<T>(command, envelope)` API.
  - [x] Ensure command bodies cannot override aggregate identity in fake execution. A mismatched command `TenantId` and envelope `AggregateId` must follow production semantics by using the envelope aggregate ID.
  - [x] Preserve `TenantIdentity.DefaultTenantId`, `TenantIdentity.Domain`, `TenantIdentity.GlobalAdministratorsDomain`, and `TenantIdentity.GlobalAdministratorsAggregateId`; do not hard-code alternate identity literals.
  - [x] Preserve trusted global-admin metadata as the `actor:globalAdmin` envelope extension only; do not infer global-admin authority from command payloads or arbitrary user claims.
  - [x] Keep `TenantTestHelpers.CreateCommandEnvelope<T>` as the documented helper for explicit aggregate ID, actor user ID, and global-admin metadata setup.
- [x] Add or complete focused Story 6.2 tests in `Testing.Tests` (AC: 1, 2, 3, 4, 5)
  - [x] Add tests proving tenant fake success paths invoke production logic by comparing fake results against direct `TenantAggregate.Handle(...)` results for representative lifecycle, membership, role, and configuration commands.
  - [x] Add tests proving global-admin fake success paths invoke production logic by comparing fake results against direct `GlobalAdministratorsAggregate.Handle(...)` results for bootstrap, set, and remove scenarios.
  - [x] Add envelope identity tests where the command payload tenant ID intentionally differs from `CommandEnvelope.AggregateId`; assert fake behavior matches direct aggregate behavior and state is keyed by the envelope aggregate ID.
  - [x] Add rejection parity tests for structured rejection event types, including insufficient permissions and at least one tenant or global-admin domain rejection. Assert event type and stable fields, not localized message text.
  - [x] Add a regression test that no-op and rejection results leave state and `EventHistory` unchanged.
  - [x] Keep tests Tier 1 only. They must not require DAPR, Aspire, Docker, Redis, actors, HTTP hosting, or a live EventStore process.
- [x] Reconcile existing conformance code without stealing Story 6.3 scope (AC: 2, 4)
  - [x] If `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` already exists, leave it in place and use it as supporting evidence only.
  - [x] Do not expand Story 6.2 into exhaustive every-command conformance coverage; Story 6.3 owns reflection-driven, every-command production/fake conformance and projection drift coverage.
  - [x] If current conformance tests expose a Story 6.2 parity bug, fix the fake or helper behavior now, then document that Story 6.3 remains responsible for broad conformance policy.
- [x] Validate dependency and packaging boundaries (AC: 5)
  - [x] Confirm `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj` has project references to `Server` and `Contracts` only as needed for fake parity, and no inline package versions.
  - [x] Confirm `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` and `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj` do not reference `Hexalith.Tenants.Testing`.
  - [x] Do not introduce new projects, wrappers, infrastructure fixtures, Docker/Testcontainers dependencies, DAPR components, or AppHost changes for this story.
- [x] Validate locally
  - [x] `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] If VSTest cannot open sockets in the local sandbox, run the built xUnit v3 in-process test executable and record the fallback command and results.

## Dev Notes

- This is a reconciliation/hardening story against the current workspace state. Candidate implementation already exists in `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`, and candidate parity/conformance tests already exist under `tests/Hexalith.Tenants.Testing.Tests/`. Dev work should audit, adjust, and add focused Story 6.2 coverage instead of recreating the Testing package surface. [Source: repository scan; `_bmad-output/implementation-artifacts/6-1-provide-in-memory-tenant-test-fakes.md#Current-State-Notes`]
- Epic 6 goal: developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`]
- PRD FR47 is the core product guarantee: in-memory fakes execute the same domain logic as production for command validation, event production, and state transitions. Projection-level and query-level isolation remain consumer-service responsibilities. [Source: `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`]
- NFR4 remains in force: in-memory testing fakes should execute commands and produce events within 10ms as measured by xUnit test execution time. Keep tests pure and fast. [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`]
- Component boundary: `Testing` provides in-memory fakes and helpers without changing production domain behavior. In-memory fake parity tests live in `Testing.Tests/Conformance` or adjacent `Testing.Tests` folders. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`; `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`]
- Dependency direction is explicit: `Hexalith.Tenants.Testing` may reference `Server`, `Contracts`, and `Hexalith.EventStore.Testing`; `Contracts` and `Server` must never reference `Testing`. [Source: `_bmad-output/project-context.md#Project-Dependency-Direction-Hard-Architectural-Constraint`]
- TEN-5 remains active: `InMemoryTenantService` and `TenantTestHelpers` intentionally return `Hexalith.EventStore.Contracts.Results.DomainResult`. Do not introduce a Tenants-owned result wrapper. [Source: `_bmad-output/planning-artifacts/architecture.md#Security-Contract-Hardening-Decisions-Correct-Course-2026-05-27`]
- Global-admin authority is security-sensitive. The aggregate trusts only the server-populated `actor:globalAdmin` envelope extension; fake helpers can create this extension for tests, but fake logic must not trust command payloads or arbitrary user claims as authority. [Source: `_bmad-output/project-context.md#Authorization-RBAC-Role-Based-Access-Control`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]

### Existing Code To Reuse

- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`: current in-memory command processor. It stores tenant states, global-admin state, and successful `EventHistory`; it exposes high-level `ProcessCommand` overloads plus low-level `ProcessTenantCommand<T>(command, envelope)`.
- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`: current helper surface for bootstrap, tenant setup, and explicit command-envelope creation. Preserve explicit aggregate ID; do not use `dynamic` or reflection to infer tenant ID from arbitrary command payloads.
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`: production tenant `Handle` logic. The fake should call this for `CreateTenant`, `UpdateTenant`, `DisableTenant`, `EnableTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, and `RemoveTenantConfiguration`.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`: production tenant `Apply` logic. The fake should call `Apply` methods for success events rather than duplicate state mutation.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`: production global-admin `Handle` logic for `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator`.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`: production global-admin `Apply` logic for global-admin success events.
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`: existing fake behavior tests. Extend these or add adjacent focused tests; do not replace useful coverage.
- `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs`: existing helper tests for envelope fields and global-admin extension behavior.
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`: existing broader parity tests. Treat as supporting context; Story 6.3 owns making this exhaustive and policy-complete.

### Current State Notes

- Recent Story 6.1 work landed at commit `23990aa` and added or updated `InMemoryTenantService`, fake tests, helper tests, and the Testing smoke test. Start from that implementation and verify it against Story 6.2 rather than assuming the backlog story starts from an empty package.
- `InMemoryTenantService.ProcessTenantCommand<T>` currently uses `envelope.AggregateId` to choose fake state, then delegates to `TenantAggregate.Handle(c, state, envelope)`. Preserve this because production aggregate logic derives the tenant ID from the envelope aggregate ID.
- Some high-level `ProcessCommand` overloads currently construct an envelope from the command tenant ID for normal ergonomic use. That is acceptable for helpers, but Story 6.2 must prove the explicit-envelope path follows production identity semantics when payload and envelope disagree.
- `ApplyTenantEvents` and `ApplyGlobalAdminEvents` currently ignore unknown event payloads in default switch branches. Do not change this casually; Story 6.3 owns projection drift and full conformance checks. If this story changes the behavior, add targeted tests and document why forward-compatible fake behavior remains acceptable.
- The archived legacy file `_bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/6-2-in-memory-projection-and-conformance-tests.md` is historical context only. Current Story 6.2 is `Reuse Production Aggregate Logic in Testing Fakes`; do not implement the archived projection/conformance scope as the authoritative story.
- Story 6.3 follows this story and owns broad reflection-driven production/fake conformance coverage for every command type. Story 6.2 should leave the system ready for that, but should not expand scope into all Story 6.3 acceptance criteria unless a narrow fix is required to satisfy Story 6.2.

### File-Specific Guardrails

- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - Current state: stateful in-memory wrapper around production aggregate/state methods.
  - This story changes: audit and harden delegation, envelope identity, rejection/no-op state safety, and tests.
  - Preserve: no infrastructure dependency, `DomainResult` public result type, per-instance isolation, successful-event-only `EventHistory`.
- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
  - Current state: helper API for short test setup and explicit envelope creation.
  - This story changes: add helper tests or small helper adjustments only if envelope parity requires them.
  - Preserve: explicit aggregate ID, `TenantIdentity` constants, and `actor:globalAdmin` extension behavior.
- `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj`
  - Current state: references `Hexalith.Tenants.Server` and `Hexalith.Tenants.Contracts`; package references use central versions.
  - This story changes: normally none.
  - Preserve: no inline `Version=` and no infrastructure-only dependencies.
- `tests/Hexalith.Tenants.Testing.Tests/*`
  - Current state: Tier 1 fake, helper, projection, and conformance tests exist.
  - This story changes: add focused Story 6.2 tests for delegation, envelope identity, rejection parity, and no mutation on rejection/no-op.
  - Preserve: xUnit v3, Shouldly, no `Assert.*`, no skipped conformance tests, no DAPR/Aspire/Docker requirement.

### Testing Standards

- Tier 1 only for this story. Tests must run without DAPR, Aspire, Docker, actors, Redis, broker, HTTP host, or a live EventStore process. [Source: `_bmad-output/project-context.md#Three-Tier-Test-Model`]
- Use xUnit v3 attributes and Shouldly assertions. Do not use xUnit v2 packages and do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Test classes use plural `{TypeUnderTest}Tests.cs`; test method names use `snake_case_with_PascalCase_for_type_names`; tests mirror source folders. [Source: `_bmad-output/project-context.md#Test-Naming-Layout`]
- Every test must contain at least one Shouldly assertion and must not use placeholder assertions. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Do not disable conformance, naming-convention, or serialization round-trip tests to make this story pass. [Source: `_bmad-output/project-context.md#What-NOT-to-Do-in-Tests`]
- Keep local package pins and central package management. Do not add inline `Version=` attributes to `PackageReference`. [Source: `_bmad-output/project-context.md#Package-Management`]

### Git Intelligence

- Recent commit `23990aa feat(story-6.1): Provide In-Memory Tenant Test Fakes` updated `InMemoryTenantService`, `InMemoryTenantServiceTests`, `TenantTestHelpersTests`, and `ScaffoldingSmokeTests`. It is the immediate implementation baseline for this story.
- Recent aggregate/configuration work has already updated `TenantAggregate`, `TenantState`, and `TenantConformanceTests` for membership, role, and configuration behavior. Do not regress those paths while hardening fake parity.
- The current worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-5-20260601-061130.md`. Ignore it unless the user explicitly asks to reconcile story-automator artifacts.

### Latest Technical Notes

- No dependency upgrade is required for this story. Use the repository-pinned .NET 10 SDK, xUnit v3, Shouldly, and EventStore/Tenants project references from `global.json`, `Directory.Packages.props`, and existing project files.
- Network package research is intentionally not part of implementation. This story is about using existing production aggregate/state APIs; adding or upgrading packages would increase risk without satisfying any acceptance criterion.

### Project Structure Notes

- Expected source files to review/update:
  - `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
  - `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj`
- Expected test files to review/update:
  - `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
  - `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantTestHelpersTests.cs`
  - Optional focused new test file under `tests/Hexalith.Tenants.Testing.Tests/Fakes/` or `tests/Hexalith.Tenants.Testing.Tests/Conformance/` if that is cleaner than extending existing classes.
- Do not move production domain logic from `Server` into `Testing`.
- Do not place aggregates or state classes in `Testing`; EventStore auto-discovers aggregates from `Hexalith.Tenants.Server`.
- Do not add host, Aspire, DAPR component, container, Testcontainers, or integration-test changes for this story.

### References

- `_bmad-output/planning-artifacts/epics.md#Story-6.2-Reuse-Production-Aggregate-Logic-In-Testing-Fakes`
- `_bmad-output/planning-artifacts/epics.md#Story-6.3-Add-Production-Fake-Conformance-Tests`
- `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`
- `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`
- `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`
- `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Security-Contract-Hardening-Decisions-Correct-Course-2026-05-27`
- `_bmad-output/project-context.md#Project-Dependency-Direction-Hard-Architectural-Constraint`
- `_bmad-output/project-context.md#Testing-Rules`
- `_bmad-output/implementation-artifacts/6-1-provide-in-memory-tenant-test-fakes.md#Dev-Notes`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` built the project but VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` in the local sandbox.
- `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -class Hexalith.Tenants.Testing.Tests.Fakes.Story62ProductionAggregateParityTests` passed: 5 total, 0 failed.
- `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor` passed: 112 total, 0 failed.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` passed: 0 warnings, 0 errors.
- Tier 1 regression executables passed: Contracts.Tests 104 total, Client.Tests 92 total, Sample.Tests 31 total, all 0 failed.
- Dependency audit: no `Hexalith.Tenants.Testing` references found under `src/Hexalith.Tenants.Server` or `src/Hexalith.Tenants.Contracts`; Testing project references Server and Contracts with central package versions.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 6.2 created as a reconciliation/hardening story against the current workspace, with explicit guardrails to reuse existing production aggregate/state logic and avoid duplicating fake business rules.
- Validation checklist applied during story creation; no blocking gaps remain for dev-story handoff.
- Audited `InMemoryTenantService`; tenant and global-admin command paths already delegate to production aggregate `Handle` methods and apply successful events through production state `Apply` methods.
- Added focused Story 6.2 tests covering representative tenant success parity, global-admin success parity, explicit-envelope aggregate identity precedence, tenant/global-admin structured rejection parity, and rejection/no-op non-mutation.
- Left existing `Conformance/TenantConformanceTests.cs` unchanged as supporting evidence; Story 6.3 remains responsible for exhaustive every-command conformance policy.
- Confirmed dependency direction and packaging boundaries: no reverse Testing dependency from Contracts or Server, no new dependencies, no infrastructure fixtures.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved. No source defects were found that required an automatic code fix.

Review evidence:
- Story status was reviewable and Epic/Story ID resolved as 6.2.
- Project standards loaded from `_bmad-output/project-context.md` and architecture guardrails loaded from `_bmad-output/planning-artifacts/architecture.md`; no external package/API research was needed for this local aggregate/fake parity review.
- Acceptance criteria 1 and 2 verified against `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`: tenant commands dispatch to `TenantAggregate.Handle(...)`, global-administrator commands dispatch to `GlobalAdministratorsAggregate.Handle(...)`, and successful events are applied through production `TenantState.Apply(...)` / `GlobalAdministratorsState.Apply(...)` methods.
- Acceptance criterion 3 verified by `ProcessTenantCommand_uses_envelope_aggregate_id_when_command_payload_tenant_id_differs`, which proves the explicit-envelope path keys state by `CommandEnvelope.AggregateId`.
- Acceptance criterion 4 verified by tenant insufficient-permission and global-administrator last-admin parity tests that compare stable event types and fields without localized message text.
- Acceptance criterion 5 verified by project references: Testing references Server and Contracts, while Server and Contracts do not reference Testing.
- File List validated. The unrelated `_bmad-output/story-automator/orchestration-5-20260601-061130.md` worktree modification was ignored because it is outside application source and explicitly called out as unrelated in the story notes.
- Test quality reviewed: Story 6.2 tests use xUnit v3 and Shouldly, contain real assertions, and remain Tier 1 with no DAPR, Aspire, Docker, Redis, HTTP host, or live EventStore requirement.

Validation results:
- `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` built successfully, then VSTest aborted on local sandbox socket setup with `System.Net.Sockets.SocketException (13): Permission denied`.
- `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -class Hexalith.Tenants.Testing.Tests.Fakes.Story62ProductionAggregateParityTests` passed: 5 total, 0 failed.
- `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor` passed: 112 total, 0 failed.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` passed with 0 warnings and 0 errors.

### File List

- _bmad-output/implementation-artifacts/6-2-reuse-production-aggregate-logic-in-testing-fakes.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- tests/Hexalith.Tenants.Testing.Tests/Fakes/Story62ProductionAggregateParityTests.cs

### Change Log

- 2026-06-01: Added focused Story 6.2 production/fake parity tests, filled the global-admin rejection parity gap, and recorded dependency-boundary audit; no production fake changes were required after audit.
- 2026-06-01: Senior developer review approved Story 6.2, recorded validation evidence, and moved the story to done; no source fixes were required.
