---
baseline_commit: c996c3ba7a75d5e1f941691c78ef040a4cf64e4c
---

# Story 2.5: Disable and Re-Enable Tenants

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want to disable and re-enable tenants,
so that tenant operations can be stopped during risk or restored when the tenant is ready.

## Acceptance Criteria

1. Given a global administrator submits a disable tenant command for an enabled tenant, when the tenant aggregate handles the command, then it records a tenant disabled event, and subsequent tenant-scoped state-changing commands are rejected while the tenant is disabled.
2. Given a global administrator submits an enable tenant command for a disabled tenant, when the tenant aggregate handles the command, then it records a tenant enabled event, and normal tenant command processing is restored.
3. Given a disable or enable command targets a missing tenant, when the command is handled, then the aggregate returns a structured tenant-not-found rejection, and no lifecycle event is produced.
4. Given a command targets a disabled tenant, when the command is not the authorized enable-tenant recovery operation, then the aggregate rejects it immediately with a structured disabled-tenant rejection, and no state-changing event is produced.
5. Given a duplicate disable command is submitted, when the tenant is already disabled, then the aggregate returns a structured duplicate-tenant-lifecycle-state rejection, and no tenant disabled event is produced.
6. Given a duplicate enable command is submitted, when the tenant is already enabled, then the aggregate returns a structured duplicate-tenant-lifecycle-state rejection, and no tenant enabled event is produced.
7. Given tenant lifecycle duplicate tests run, when duplicate disable and duplicate enable cases are exercised, then tests verify the exact rejection type, current lifecycle state, and absence of duplicate lifecycle events.
8. Given the tenant status enum reserves ordinal 0 as a non-active `Unknown` sentinel (TEN-2 correction), when a tenant snapshot, read model, or query payload is deserialized with a missing or unrecognized status field, then the status resolves to the fail-closed `Unknown` sentinel rather than defaulting to `Active`, and the status enum serializes by name, and consuming services never treat an absent status as an active tenant.

## Tasks / Subtasks

- [x] Replace duplicate lifecycle `NoOp` outcomes with a structured rejection (AC: 5, 6, 7)
  - [x] Add a new immutable rejection record under `src/Hexalith.Tenants.Contracts/Events/Rejections/` for duplicate tenant lifecycle state. Use a naming-test-compliant name such as `TenantLifecycleStateAlreadySetRejection`.
  - [x] Include structured fields sufficient for tests and HTTP mapping work: `TenantId`, current `TenantStatus`, requested `TenantStatus`, and/or command name. Do not put user-facing prose in the rejection payload.
  - [x] Update `TenantAggregate.Handle(DisableTenant, TenantState?, CommandEnvelope)` so `{ Status: TenantStatus.Disabled }` returns the new rejection instead of `DomainResult.NoOp()`.
  - [x] Update `TenantAggregate.Handle(EnableTenant, TenantState?, CommandEnvelope)` so `{ Status: TenantStatus.Active }` returns the new rejection instead of `DomainResult.NoOp()`.
  - [x] Preserve `TenantNotFoundRejection` before duplicate-state checks and preserve trusted global-admin authorization before lifecycle execution.

- [x] Preserve successful disable and enable lifecycle behavior (AC: 1, 2, 3)
  - [x] Keep `DisableTenant` and `EnableTenant` as command records in `src/Hexalith.Tenants.Contracts/Commands/`.
  - [x] Keep `TenantDisabled(string TenantId, DateTimeOffset DisabledAt)` and `TenantEnabled(string TenantId, DateTimeOffset EnabledAt)` as event records with top-level `TenantId`.
  - [x] Keep successful lifecycle events using `envelope.AggregateId` as the canonical tenant ID, not `command.TenantId`.
  - [x] Keep timestamps sourced from `DateTimeOffset.UtcNow` only on successful lifecycle events.
  - [x] Do not create per-command REST endpoints; command submission stays on EventStore's `POST /api/commands` path and the Tenants `/process` domain processor.

- [x] Keep disabled tenants fail-closed for all non-recovery tenant commands (AC: 4)
  - [x] Preserve disabled-state rejection for `UpdateTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, and `RemoveTenantConfiguration`.
  - [x] Verify the disabled-state switch arms occur before RBAC, duplicate, role, configuration, or no-op checks for tenant-scoped commands, so disabled tenants reject immediately with `TenantDisabledRejection`.
  - [x] Do not block `EnableTenant` with `TenantDisabledRejection`; it is the authorized recovery operation.
  - [x] Do not introduce a "must retain at least one owner" invariant or any membership behavior changes in this story.

- [x] Preserve and document fail-closed tenant status serialization (AC: 8)
  - [x] Keep `TenantStatus.Unknown = 0`, `Active`, and `Disabled`, with `JsonStringEnumConverter<TenantStatus>` on the enum.
  - [x] Keep `TenantLocalState.Status` defaulting to `TenantStatus.Unknown`.
  - [x] Keep `TenantSummary`, `TenantDetail`, and `UserTenantMembership` status serialization by name; do not allow absent status fields to become `Active`.
  - [x] Add explicit coverage for an unrecognized tenant status value. If the current `JsonStringEnumConverter<TenantStatus>` throws for unknown names, replace it with a focused fail-closed status converter that maps unrecognized names to `TenantStatus.Unknown` while continuing to serialize known values by name.
  - [x] Update docs only if the new lifecycle duplicate rejection changes the event contract reference tables or examples.

- [x] Update state, projection, client, and testing consumers only where needed (AC: 1, 2, 4, 8)
  - [x] Keep `TenantState.Apply(TenantDisabled)` setting `Status = TenantStatus.Disabled` and `TenantState.Apply(TenantEnabled)` setting `Status = TenantStatus.Active`.
  - [x] Add an `Apply` method for the new duplicate lifecycle-state rejection only if aggregate replay requires it for persisted rejection events; if added, it must be replay-only and must not mutate state.
  - [x] Keep `TenantReadModel`, `TenantIndexReadModel`, `TenantProjectionEventHandler`, and `InMemoryTenantProjection` status transitions unchanged unless tests expose an actual gap.
  - [x] Keep `InMemoryTenantService` delegating to `TenantAggregate.Handle(...)`; do not duplicate lifecycle logic in the fake.

- [x] Extend focused validation coverage (AC: 1-8)
  - [x] Update `TenantAggregateTests` so duplicate disable and duplicate enable expect the new rejection type, current lifecycle state, and zero `TenantDisabled`/`TenantEnabled` events.
  - [x] Add or update tests proving disabled tenants reject every non-enable state-changing command with `TenantDisabledRejection`.
  - [x] Update `TenantConformanceTests` and `InMemoryTenantServiceTests` that currently expect duplicate disable/enable `NoOp` outcomes.
  - [x] Ensure `EventSerializationTests` and naming convention tests cover the new rejection contract automatically; add explicit coverage only if the reflection tests do not catch it.
  - [x] Keep `EnumFailSafeTests`, query DTO serialization tests, and client local-state casing tests green for `TenantStatus.Unknown` and string enum serialization.
  - [x] Run focused contracts validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests|EnumFailSafeTests|QueryDtoSerializationTests" -m:1 -nr:false`
  - [x] Run focused aggregate/conformance validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests|CommandPipelineIntegrationTests" -m:1 -nr:false`
  - [x] Run focused testing package validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests|InMemoryTenantProjectionTests|InMemoryTenantProjectionConformanceTests" -m:1 -nr:false`
  - [x] Run release build:
    `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
  - [x] If local VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostics and use build results as partial evidence only.

## Dev Notes

### Source Context

- Epic 2 objective: global administrators can bootstrap the first admin, manage global administrators, create tenants, update metadata, disable or enable tenants, and receive structured rejection outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Global Administrators Can Bootstrap and Govern Tenants`]
- Story 2.5 requires `DisableTenant` and `EnableTenant` lifecycle operations, disabled-tenant command rejection, duplicate lifecycle-state rejection, and fail-closed `TenantStatus.Unknown` serialization. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Disable and Re-Enable Tenants`]
- PRD FR3 and FR4 require global administrators to disable and re-enable tenants; FR5 requires a domain event for created, updated, disabled, and enabled lifecycle changes; FR50-FR52 require structured missing, disabled, and duplicate-operation rejections. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Lifecycle Management`]
- NFR8 calls out unit tests proving disabled tenants reject commands; NFR4 requires zero cross-tenant leaks and NFR9 requires tenant isolation and role authorization branch coverage. [Source: `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`]
- UX is not implemented in this backend story, but future admin screens depend on precise lifecycle status, blocked-action reasons, and support-safe rejection states for high-impact disable/enable flows. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#2.5 Experience Mechanics`]

### Current Repository State

- `DisableTenant`, `EnableTenant`, `TenantDisabled`, `TenantEnabled`, `TenantStatus`, and `TenantDisabledRejection` already exist in `src/Hexalith.Tenants.Contracts`.
- `TenantStatus` already has `Unknown = 0`, `Active`, and `Disabled`, and is decorated with `JsonStringEnumConverter<TenantStatus>`.
- Missing `TenantStatus` fields already default to `Unknown`; unrecognized status strings need explicit verification because `JsonStringEnumConverter<T>` commonly fails closed by throwing rather than materializing `Unknown`.
- `TenantAggregate.Handle(DisableTenant, ..., CommandEnvelope)` already requires trusted global-admin authority, uses `envelope.AggregateId`, returns `TenantNotFoundRejection` for missing state, and emits `TenantDisabled(..., DateTimeOffset.UtcNow)` for active tenants.
- `TenantAggregate.Handle(EnableTenant, ..., CommandEnvelope)` already requires trusted global-admin authority, uses `envelope.AggregateId`, returns `TenantNotFoundRejection` for missing state, and emits `TenantEnabled(..., DateTimeOffset.UtcNow)` for disabled tenants.
- The key implementation gap is duplicate lifecycle handling: duplicate disable and duplicate enable currently return `DomainResult.NoOp()`, and tests currently assert `IsNoOp`. Story 2.5 requires a structured duplicate lifecycle-state rejection instead.
- Disabled-state guards already exist for `UpdateTenant`, membership commands, role changes, and tenant configuration commands. Preserve their ordering before RBAC and other domain checks.
- `TenantState`, `TenantReadModel`, `TenantIndexReadModel`, client projection handling, and in-memory projection already track disabled/enabled status transitions.
- `InMemoryTenantService` already delegates lifecycle commands to `TenantAggregate.Handle(...)`; keep that parity rather than reimplementing fake-specific behavior.

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Contracts are immutable records in `Hexalith.Tenants.Contracts`; success events implement `IEventPayload`; rejection events implement `IRejectionEvent`.
- Aggregate `Handle` methods must remain pure static methods: no I/O, no DAPR, no async, no logging, and no captured state.
- Business-rule failures are domain rejections, not exceptions. Use collection expressions for result event lists.
- Platform tenant ID is `TenantIdentity.DefaultTenantId` (`system`), tenant domain is `TenantIdentity.Domain` (`tenants`), and managed tenant aggregate ID is the aggregate being governed.
- Every event payload must include top-level `TenantId`. Event envelope `TenantId` is the platform tenant (`system`), so consumers identify the managed tenant from the payload.
- Commands/events/rejections must satisfy naming tests: commands `{Verb}{Target}`, events `{Target}{PastVerb}`, rejections `{Target}{Reason}Rejection`.
- Use `System.Text.Json` only. Do not introduce Newtonsoft.Json or another serializer.
- Use K&R brace style, file-scoped namespaces, one type per file, source folder structure matching namespaces, and no inline `PackageReference Version=`.
- No external package or API research is needed: this story uses existing pinned .NET/EventStore/Tenants APIs and introduces no dependency upgrades.

### Previous Story Intelligence

- Story 2.4 updated `TenantUpdated` to include `UpdatedAt` and touched aggregate, audit read model, public event-contract docs, projection tests, integration tests, and BMAD sprint status. Avoid undoing those changes.
- Story 2.4 confirmed create/update lifecycle operations use canonical `envelope.AggregateId`; Story 2.5 must preserve that for disable/enable success and rejection payloads.
- Story 2.4 kept `UpdateTenant` rejection ordering as missing tenant before disabled tenant before authorization. Do not weaken this while adding lifecycle duplicate rejections.
- Story 2.4 documented the local VSTest socket permission failure after test projects compiled. Record the same blocker if it recurs rather than claiming tests passed.
- Recent commits:
  - `c996c3b feat(story-2.4): create and update tenants`
  - `1c58824 feat(story-2.3): authorize global administrators for cross-tenant governance`
  - `9240d0c feat(tests): add global admin extension handling in integration tests and telemetry`
  - `8a0b2a1 change BMAD project name from Hexalith.Tenants to Tenants`
  - `644a987 BMAD 6.8.0`

### Likely Files to Touch

- `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`
- `src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs`
- `src/Hexalith.Tenants.Contracts/Serialization/TenantStatusJsonConverter.cs` (only if the built-in enum converter cannot satisfy AC 8)
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`
- `docs/event-contract-reference.md`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EnumFailSafeTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/CommandPipelineIntegrationTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`

### Out of Scope

- Public admin UI, FrontComposer, or Phase 2 disable/enable command flows.
- New lifecycle commands beyond `DisableTenant` and `EnableTenant`.
- Query endpoint redesign, cursor pagination, projection write conflict recovery, or audit query behavior from Epic 5.
- Problem Details mapping completeness from Story 2.6, except updating contract docs so the new rejection is not invisible.
- DAPR, Aspire, OpenTelemetry, SDK, or package version upgrades.
- EventStore submodule changes unless a focused failing test proves a direct compatibility issue.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Disable and Re-Enable Tenants`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Lifecycle Management`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/2-4-create-and-update-tenants.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs`]
- [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `docs/event-contract-reference.md#DisableTenant`]

## Project Structure Notes

- Alignment: implementation belongs in existing contracts, server aggregate/state, testing fake/conformance tests, focused server tests, and event contract docs. No new project, package, controller, infrastructure dependency, or AppHost change is expected.
- The main implementation risk is preserving existing lifecycle code while changing only duplicate disable/enable semantics from no-op to structured rejection.
- The existing older artifacts for Story 2.x include historical implementation outputs; use this story file for current sprint key `2-5-disable-and-re-enable-tenants`.

## Validation Checklist Results

- Story foundation extracted from Epic 2 and Story 2.5 acceptance criteria.
- PRD, architecture, UX, project context, previous Story 2.4, current source, tests, docs, and recent git history were reviewed.
- Current source inspection found substantial lifecycle implementation already present; the required duplicate lifecycle-state rejection gap is explicitly called out.
- Existing UPDATE files and consumers were identified, including aggregate, state, projection/read model surfaces, client/testing projection behavior, testing fake, docs, and tests.
- Disaster-prevention guardrails included: do not duplicate in-memory fake logic, do not trust command body tenant IDs, do not parse claims in aggregate code, do not add REST endpoints, do not weaken fail-closed `Unknown` status serialization, and do not weaken disabled-tenant rejection ordering.
- Latest technical context checked against local pinned project rules. No external dependency research is required because no package or framework update is part of this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: `dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests" -m:1 -nr:false --no-restore` failed compilation before implementation because `TenantLifecycleStateAlreadySetRejection` did not exist.
- Focused contracts validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Focused aggregate/conformance validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Focused testing package validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`.
- Review aggregate validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Review contracts validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Review testing package validation built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Review release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`.

### Completion Notes List

- Added `TenantLifecycleStateAlreadySetRejection` for duplicate disable/enable commands with tenant ID, current status, requested status, and command name.
- Updated duplicate disable/enable aggregate behavior from `DomainResult.NoOp()` to structured rejection while preserving missing-tenant and global-admin ordering.
- Added replay-only handling for the new rejection in `TenantState`.
- Added fail-closed `TenantStatusJsonConverter` so unknown status names deserialize to `TenantStatus.Unknown` while known values serialize by name.
- Updated focused aggregate, conformance, in-memory fake, event serialization, enum fail-safe, and query DTO serialization tests.
- Updated event contract reference docs for duplicate lifecycle rejections and fail-closed tenant status handling.
- Review auto-fix: tenant-scoped state-changing commands now reject `TenantStatus.Unknown` with `TenantDisabledRejection` so deserialized missing/unrecognized status is not treated as active.
- Review auto-fix: added aggregate coverage proving `TenantStatus.Unknown` blocks update, membership, role, and configuration mutations.

### File List

- `_bmad-output/implementation-artifacts/2-5-disable-and-re-enable-tenants.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs`
- `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`
- `src/Hexalith.Tenants.Contracts/Serialization/TenantStatusJsonConverter.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EnumFailSafeTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`

### Change Log

- 2026-05-31: Implemented duplicate tenant lifecycle-state rejection, fail-closed tenant status converter, focused tests, and contract docs for Story 2.5.
- 2026-05-31: Senior review auto-fixed fail-closed `TenantStatus.Unknown` command handling, added aggregate coverage, updated File List, and marked Story 2.5 done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after auto-fix.

### Findings Fixed

- HIGH: `TenantStatus.Unknown` was not treated as a fail-closed status by tenant-scoped state-changing aggregate handlers. The guards only rejected `TenantStatus.Disabled`, so a tenant state deserialized from a missing/unrecognized status field could still accept `UpdateTenant`, membership, role, and configuration mutations. Fixed by changing non-enable tenant-scoped handlers to reject any status other than `TenantStatus.Active` with `TenantDisabledRejection`, and by adding aggregate coverage for `TenantStatus.Unknown`.
- MEDIUM: The Dev Agent File List omitted changed integration test files. Added `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`.

### Acceptance Criteria Review

- AC1-2: Successful disable and enable events are preserved and use `envelope.AggregateId`.
- AC3: Missing lifecycle targets still return `TenantNotFoundRejection` before duplicate lifecycle checks.
- AC4: Disabled and non-active tenant states reject non-enable state-changing commands before RBAC, duplicate, role, and configuration checks.
- AC5-7: Duplicate disable/enable now return `TenantLifecycleStateAlreadySetRejection` with current status, requested status, command name, and no duplicate lifecycle event.
- AC8: `TenantStatus.Unknown` remains ordinal 0, serializes by name, unrecognized status names deserialize to `Unknown`, and aggregate command handling now fails closed for non-active status.

### Validation

- MCP doc search: not applicable; this story introduced no external dependency or unfamiliar API usage, and local project context/docs were used as the source of truth.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests" -m:1 -nr:false --no-restore`: project compiled, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests|EnumFailSafeTests|QueryDtoSerializationTests" -m:1 -nr:false --no-restore`: project compiled, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests|InMemoryTenantProjectionTests|InMemoryTenantProjectionConformanceTests" -m:1 -nr:false --no-restore`: project compiled, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`: passed.
