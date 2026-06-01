---
baseline_commit: 3be7ce3
---

# Story 6.4: Support Consumer Tenant Isolation Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want testing helpers for tenant isolation scenarios,
so that my service can prove its own projections and access checks do not leak tenant data.

## Acceptance Criteria

1. Given a consumer test uses the Testing package, when the developer creates multiple tenants and users in memory, then helpers make it straightforward to seed tenant memberships, roles, and lifecycle state, and the test can assert behavior for separate tenant contexts without live infrastructure.
2. Given a consumer projection subscribes to fake tenant events, when membership and lifecycle events are emitted by the fake, then the consumer can verify its local projection reacts to tenant A without adding tenant B data, and duplicate event delivery can be simulated for idempotency checks.
3. Given a user has roles in multiple tenants, when a consumer test evaluates access for each tenant, then helpers support asserting tenant-specific authorization, and roles from one tenant do not implicitly authorize another tenant.
4. Given a consumer wants to test removal and revocation, when the fake emits user-added and user-removed event sequences, then the consumer can assert access grant and revocation behavior in under ordinary unit-test timing, and no polling, Docker, DAPR sidecar, or network call is required.
5. Given helper documentation is reviewed, when developers follow examples, then documentation clearly states that aggregate-level fake parity is provided, and consuming services remain responsible for testing their own projection-level and query-level isolation.

## Tasks / Subtasks

- [x] Design the consumer-facing isolation helper API before coding (AC: 1, 2, 3, 4, 5)
  - [x] Review `TenantTestHelpers`, `InMemoryTenantService`, and `InMemoryTenantProjection` and extend the existing helper style instead of creating a parallel fake framework.
  - [x] Prefer a small public helper surface under `src/Hexalith.Tenants.Testing/Helpers/` such as `TenantIsolationTestHelpers` if the new scenario methods would make `TenantTestHelpers` too broad.
  - [x] Keep helpers command-driven: create tenants, assign roles, remove users, disable/enable tenants, and set up multi-tenant scenarios through `InMemoryTenantService.ProcessCommand(...)` or `TenantTestHelpers`, not by hand-mutating `TenantState` or projection read models.
  - [x] Return existing in-tier types where practical: `DomainResult`, `IEventPayload` sequences, tenant IDs, and role maps. Do not introduce a new Tenants-owned result wrapper.
- [x] Add multi-tenant seeding helpers for consumer tests (AC: 1, 3)
  - [x] Provide a concise way to create two or more active tenants with independent memberships and roles.
  - [x] Support the same user having different roles in different tenants, including at least `TenantOwner`, `TenantContributor`, and `TenantReader` coverage.
  - [x] Support disabled/enabled lifecycle setup per tenant without changing unrelated tenants.
  - [x] Ensure every helper creates a fresh `InMemoryTenantService` scenario or works against an explicitly supplied service so test state cannot leak between tests.
- [x] Add event sequence helpers for consumer projection tests (AC: 2, 4)
  - [x] Provide a way to capture or select the success events emitted for a tenant from `InMemoryTenantService.EventHistory`.
  - [x] Provide an intentional duplicate-delivery helper that repeats selected success events so consumer tests can verify idempotency.
  - [x] Ensure tenant-scoped event selection uses the payload `TenantId` for tenant events and does not mix tenant A and tenant B events.
  - [x] Do not add a dependency from `Hexalith.Tenants.Testing` to `Hexalith.Tenants.Client` just to build `TenantEventEnvelope` objects. Consumers that use `Client` can wrap payloads in their own test project; the Testing package should remain package-boundary clean unless a compile-time requirement proves otherwise.
- [x] Add consumer-style isolation tests for the new helpers (AC: 1, 2, 3, 4)
  - [x] Add focused tests under `tests/Hexalith.Tenants.Testing.Tests/Helpers/`.
  - [x] Prove tenant A membership/configuration/lifecycle setup does not appear in tenant B when events are replayed through a minimal consumer projection fixture.
  - [x] Prove the same user can be an owner/contributor/reader in one tenant and denied or differently authorized in another tenant.
  - [x] Prove user removal emits a sequence that lets a consumer projection grant access and then revoke access without polling or infrastructure.
  - [x] Prove duplicate delivery simulation does not require DAPR, Docker, Aspire, HTTP, or a network call.
- [x] Preserve existing parity and conformance guarantees (AC: 1-5)
  - [x] Do not weaken `TenantConformanceTests` or `InMemoryTenantProjectionConformanceTests`.
  - [x] Do not change aggregate/domain logic in `Server` unless a helper test exposes a real product defect; story 6.4 should normally be Testing-package API plus tests/docs.
  - [x] Preserve `InMemoryTenantService.EventHistory` as successful events only; rejection and no-op commands must not be appended.
  - [x] Preserve `TenantIdentity.DefaultTenantId`, tenant domain identity, global-admin aggregate identity, and trusted `actor:globalAdmin` metadata semantics.
- [x] Document the helper usage and responsibility boundary (AC: 5)
  - [x] Add a short quickstart or README example showing multi-tenant setup, event replay into a consumer projection, duplicate delivery simulation, and removal/revocation assertion.
  - [x] State explicitly that `Hexalith.Tenants.Testing` guarantees aggregate-level fake parity: command validation, event production, and state transitions.
  - [x] State explicitly that consuming services remain responsible for their own projection-level and query-level isolation tests.
  - [x] Link or reference the existing idempotency guidance instead of duplicating the full event-processing document.
- [x] Validate locally (AC: 1-5)
  - [x] `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`
  - [x] If VSTest cannot open sockets in the local sandbox, run the built xUnit v3 in-process executable and record the fallback command and results, for example `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor`.

## Dev Notes

- Epic 6 goal: developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior. Story 6.4 is the consumer-isolation helper layer on top of the Story 6.1-6.3 fake/parity foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`]
- Story 6.4 requirements are specifically about multi-tenant setup, tenant-scoped event replay, duplicate delivery simulation, role isolation across tenants, removal/revocation tests, and documentation of the aggregate-vs-consumer-projection responsibility boundary. [Source: `_bmad-output/planning-artifacts/epics.md#Story-6.4-Support-Consumer-Tenant-Isolation-Tests`]
- PRD FR46/FR47 define the product boundary: testing fakes run without external infrastructure and execute production aggregate logic, but projection-level and query-level isolation remains the consuming service's own test responsibility. [Source: `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`]
- PRD NFR4 requires in-memory fakes to execute commands and produce events within 10ms as measured by xUnit test execution time. Keep this story Tier 1 and fast. [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`]
- NFR5 and NFR10 keep cross-tenant isolation and role authorization as security-critical areas. Story 6.4 should help consumers test their own projection/query isolation without claiming Tenants can prove every consumer's local model. [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- Architecture maps Epic 6 to `src/Hexalith.Tenants.Testing` and `tests/Hexalith.Tenants.Testing.Tests`. New test helpers/fakes go in `Testing`; in-memory fake parity tests stay in `Testing.Tests`. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`; `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`]
- Component boundary: `Testing` provides in-memory fakes and helpers without changing production domain behavior. `Client` remains the consumer event-handling package. Do not make `Testing` depend on `Client` unless a deliberate architecture change is approved. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`; `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj`]
- TEN-5 remains active: `InMemoryTenantService` and `TenantTestHelpers` intentionally expose EventStore `DomainResult`. Do not add a Tenants-owned wrapper for helper outcomes. [Source: `_bmad-output/planning-artifacts/architecture.md#Security-Contract-Hardening-Decisions-Correct-Course-2026-05-27`]
- Event handling docs already explain at-least-once delivery, duplicate events, and idempotent handler patterns. Story 6.4 docs should reference that guidance and add only the Testing helper usage needed for isolation tests. [Source: `docs/idempotent-event-processing.md#Why-Idempotency-Matters`; `docs/idempotent-event-processing.md#Making-Handlers-Idempotent`]

### Existing Code To Reuse

- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`: command-driven in-memory fake. It delegates tenant/global-admin commands to production aggregates, applies successful events through production state `Apply` methods, exposes `EventHistory`, `GetTenantState`, and `GetGlobalAdminState`, and does not append rejection/no-op results to history.
- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`: current helper style. It contains `BootstrapGlobalAdmin`, `CreateTenant`, `CreateTenantWithOwner`, and `CreateCommandEnvelope`. Extend this style or add an adjacent helper class; do not create a new fake service abstraction unless it clearly reduces complexity.
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`: Testing-package projection over Server read models. Useful for verifying emitted tenant events route by tenant, but it is not a substitute for a consuming service's own projection tests.
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`, `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs`, and `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`: consumer-side patterns for event processing, per-tenant local state, and MessageId deduplication. Use these as reference behavior, but keep production Testing package dependencies clean.
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`: consumer-style access tests already prove grant, revocation, role change, lifecycle disable/enable, and repeated remove handling through the Client event pipeline. Use this as example context when shaping Story 6.4 helper tests.
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs`: has existing cross-tenant projection and replay tests. Reuse patterns where helpful, but Story 6.4 should add helper-focused consumer-isolation tests rather than only expanding projection unit tests.
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` and `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`: Story 6.3 release-blocker conformance guards. Do not weaken or bypass them.

### Previous Story Intelligence

- Story 6.1 introduced the initial `InMemoryTenantService`, `TenantTestHelpers`, and Testing package scaffolding. Story 6.4 should reuse this API shape instead of introducing a second fake framework.
- Story 6.2 made the fake delegate to production aggregate `Handle` methods and production state `Apply` methods. Keep new helpers command-driven so fake parity remains anchored in production logic.
- Story 6.3 added every-command conformance and projection success-event drift guards at commit `3be7ce3`. Treat those tests as protective rails; this story should add consumer-isolation helper coverage without deleting or broadening their scope.
- Story 6.3 validation encountered local VSTest socket failures in this sandbox, then passed through xUnit v3 in-process executables. Use the same fallback and document it if the environment repeats that behavior.

### File-Specific Guardrails

- `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
  - Current state: small setup helpers and explicit command-envelope creation.
  - This story changes: add consumer isolation scenario helpers here only if the API remains cohesive. Otherwise add a new helper type in the same folder.
  - Preserve: explicit tenant IDs, explicit actor user IDs, trusted `actor:globalAdmin` metadata only through helper-created envelopes.
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - Current state: production aggregate wrapper with successful-event history and tenant/global-admin state accessors.
  - This story changes: normally none, unless a narrowly scoped helper needs a safe read-only event selection method.
  - Preserve: no duplicated domain rules, no Client/DAPR/Aspire dependencies, no rejection/no-op events in `EventHistory`.
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
  - Current state: read-model projection used by Testing package tests, with a silent default guarded by conformance tests.
  - This story changes: normally none. Do not change projection behavior unless a Story 6.4 test exposes an actual helper need.
  - Preserve: explicit success-event routing and `IRejectionEvent` ignore behavior.
- `tests/Hexalith.Tenants.Testing.Tests/Helpers/`
  - Current state: helper tests for basic tenant creation, owner setup, global-admin bootstrap, and envelope creation.
  - This story changes: add consumer isolation helper tests here or in a clearly named adjacent folder.
  - Preserve: xUnit v3, Shouldly, Tier 1/no infrastructure.
- `docs/quickstart.md` and/or `README.md`
  - Current state: quickstart covers consuming tenant events and links idempotency guidance; README describes Testing package at package level.
  - This story changes: add a concise helper example and responsibility boundary if documentation does not already satisfy AC5.
  - Preserve: no large duplicated idempotency guide; link to `docs/idempotent-event-processing.md`.

### Testing Standards

- Tier 1 only. Do not require DAPR, Aspire, Docker, Redis, HTTP host, Testcontainers, or live EventStore for Story 6.4 helper tests. [Source: `_bmad-output/project-context.md#Three-Tier-Test-Model`]
- Use xUnit v3 and Shouldly. Do not use `Assert.*`; every test must contain at least one Shouldly assertion. [Source: `_bmad-output/project-context.md#Frameworks-Assertions`]
- Test class names use plural `{TypeUnderTest}Tests.cs`; test method names use `snake_case_with_PascalCase_for_type_names` for new tests. [Source: `_bmad-output/project-context.md#Test-Naming-Layout`]
- Keep package versions centralized. Do not add inline `Version=` attributes to `PackageReference`. [Source: `_bmad-output/project-context.md#Package-Management`]
- Do not skip or disable conformance tests. Story 6.3 made conformance a release blocker for fake parity and projection drift. [Source: `_bmad-output/project-context.md#Mandatory-Test-Categories`; `_bmad-output/implementation-artifacts/6-3-add-production-fake-conformance-tests.md#Dev-Notes`]
- If a helper creates command envelopes, use the existing `TenantTestHelpers.CreateCommandEnvelope<T>` pattern for aggregate ID, actor user ID, and global-admin metadata setup. [Source: `_bmad-output/project-context.md#CommandEnvelope-Test-Helper`]

### Latest Technical Notes

- No external package/API research is required for this story. The work is local Testing-package API, tests, and documentation using repository-pinned .NET 10, xUnit v3, Shouldly, and existing EventStore/Tenants project references.
- Do not add or upgrade packages to satisfy this story unless a compile-time gap proves the current Testing package cannot express the helper API.

### Project Structure Notes

- Expected files to review/update:
  - `src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
  - `src/Hexalith.Tenants.Testing/Helpers/TenantIsolationTestHelpers.cs` if a separate helper type is clearer
  - `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantIsolationTestHelpersTests.cs`
  - `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs` only for narrowly scoped read-only support if needed
  - `docs/quickstart.md` and/or `README.md` for AC5 documentation
- Do not create new projects.
- Do not add host, AppHost, DAPR component, Aspire, Testcontainers, Redis, HTTP, or live EventStore changes.
- Do not touch archived legacy story files.
- Do not modify `src/Hexalith.Tenants.Client` for this story unless documentation/tests prove the existing consumer patterns are impossible to reference without a small test-only fixture.

### References

- `_bmad-output/planning-artifacts/epics.md#Story-6.4-Support-Consumer-Tenant-Isolation-Tests`
- `_bmad-output/planning-artifacts/epics.md#Epic-6-Developers-Can-Test-Tenant-Behavior-Without-Infrastructure`
- `_bmad-output/planning-artifacts/prd.md#Developer-Experience-Packaging`
- `_bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements`
- `_bmad-output/planning-artifacts/prd.md#Security`
- `_bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries`
- `_bmad-output/planning-artifacts/architecture.md#Requirements-to-Structure-Mapping`
- `_bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Security-Contract-Hardening-Decisions-Correct-Course-2026-05-27`
- `_bmad-output/project-context.md#Testing-Rules`
- `_bmad-output/project-context.md#Mandatory-Test-Categories`
- `docs/idempotent-event-processing.md#Making-Handlers-Idempotent`
- `_bmad-output/implementation-artifacts/6-3-add-production-fake-conformance-tests.md#Dev-Notes`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Red phase confirmed with `dotnet test tests/Hexalith.Tenants.Testing.Tests/ --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false`; compilation failed because `TenantIsolationTestHelpers` did not exist yet.
- 2026-06-01: Required build passed: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` (0 warnings, 0 errors).
- 2026-06-01: Required VSTest command compiled but aborted in sandbox with `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01: xUnit v3 in-process fallback passed: `tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor` (181 total, 0 errors, 0 failed, 0 skipped).
- 2026-06-01: Senior review build passed after auto-fix: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:NuGetAudit=false` (0 warnings, 0 errors).
- 2026-06-01: Senior review VSTest command compiled but aborted in sandbox with `System.Net.Sockets.SocketException (13): Permission denied`; xUnit v3 in-process fallback passed with 181 total, 0 errors, 0 failed, 0 skipped.

### Implementation Plan

- Add a separate `TenantIsolationTestHelpers` class in the Testing package instead of broadening `TenantTestHelpers`.
- Keep all setup command-driven through `InMemoryTenantService` and return existing `DomainResult`, `IEventPayload`, tenant ID, and role-map types.
- Cover consumer behavior with helper-focused tests using a minimal local consumer projection fixture rather than depending on `Hexalith.Tenants.Client`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 6.4 created as a consumer-isolation helper story building on the completed Story 6.1-6.3 Testing package and conformance foundation.
- Validation checklist applied during story creation; no blocking gaps remain for dev-story handoff.
- Added `TenantIsolationTestHelpers` for fresh multi-tenant service scenarios, explicit seeding into supplied services, command-driven user/config/lifecycle changes, tenant-scoped event selection, duplicate-delivery simulation, role-map extraction, and tenant-specific role checks.
- Added consumer-style helper tests that prove cross-tenant event isolation, independent roles for the same user, disabled tenant setup, revocation through user removal, duplicate delivery replay, and no infrastructure dependency.
- Documented Testing package helper usage in the quickstart and stated the responsibility boundary: Testing guarantees aggregate-level fake parity, while consuming services must test their own projection/query isolation.
- Definition of Done checklist passed for story 6.4; VSTest is blocked by sandbox socket restrictions, and the required xUnit v3 in-process fallback passed.
- Senior review fixed tenant lifecycle authorization in `TenantIsolationTestHelpers.IsAuthorizedForTenant` so disabled tenants do not authorize existing memberships.

### File List

- `_bmad-output/implementation-artifacts/6-4-support-consumer-tenant-isolation-tests.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-5-20260601-061130.md`
- `docs/quickstart.md`
- `src/Hexalith.Tenants.Testing/Helpers/TenantIsolationTestHelpers.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantIsolationTestHelpersTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: `TenantIsolationTestHelpers.IsAuthorizedForTenant` checked only membership and role, so a disabled tenant could still authorize a user with an existing role. Fixed by requiring `TenantStatus.Active` before evaluating the role hierarchy in `src/Hexalith.Tenants.Testing/Helpers/TenantIsolationTestHelpers.cs`, with regression coverage in `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantIsolationTestHelpersTests.cs`.
- MEDIUM: The story File List omitted non-source automation artifacts changed during story execution. Fixed by adding `_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/story-automator/orchestration-5-20260601-061130.md`.

Checklist validation:

- Story status was reviewable before review and is now updated to done.
- Acceptance Criteria 1-5 are implemented by Testing helpers, helper-focused tests, and quickstart documentation.
- File List was reconciled against git status.
- No external package/API research was needed; the story's Latest Technical Notes explicitly scoped the review to local Testing-package APIs and repository-pinned dependencies.
- Security review covered tenant-specific authorization, lifecycle state, event payload tenant filtering, and package-boundary dependencies.
- VSTest remains blocked by sandbox socket restrictions; the xUnit v3 in-process fallback passed.

### Change Log

- 2026-06-01: Implemented consumer tenant isolation helpers, helper-focused tests, and quickstart documentation; validated with Release build and xUnit v3 fallback test execution.
- 2026-06-01: Senior review auto-fixed disabled-tenant authorization in isolation helpers, updated regression coverage, reconciled File List, and marked story done.
