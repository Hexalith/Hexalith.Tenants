---
baseline_commit: fec602a9c4d5e520bd96e2e11141cbd94f402240
---

# Story 3.6: Remove Tenant Configuration Entries

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to remove tenant configuration entries,
so that obsolete or incorrect tenant-specific settings stop influencing consuming services.

## Acceptance Criteria

1. Given a tenant exists, is enabled, and contains the configuration key, when a `TenantOwner` or trusted global-admin actor removes the configuration entry, then the tenant aggregate records a `TenantConfigurationRemoved` event and the applied tenant state no longer contains the key.
2. Given the requested configuration key does not exist, when a `RemoveTenantConfiguration` command is handled by an authorized actor, then the aggregate returns a structured configuration-key-not-found rejection and no `TenantConfigurationRemoved` event is produced.
3. Given a non-owner tenant member attempts to remove configuration, when authorization is evaluated, then the command is rejected with `InsufficientPermissionsRejection` and no configuration state is changed.
4. Given the target tenant is missing or disabled, when a configuration remove command is handled, then the aggregate returns the appropriate structured `TenantNotFoundRejection` or `TenantDisabledRejection` and no configuration removal event is produced.
5. Given configuration removal tests run, when existing-key, missing-key, unauthorized, disabled-tenant, missing-tenant, and serialization round-trip cases are exercised, then tests verify state mutation, rejection outcomes, and configuration event/rejection serialization.

## Tasks / Subtasks

- [x] Verify the existing removal contract surface before editing (AC: 1-5)
  - [x] Reuse the existing `RemoveTenantConfiguration` command and `TenantConfigurationRemoved` success event; do not add duplicate command or success-event types.
  - [x] Confirm `RemoveTenantConfigurationValidator` remains registered through `TenantSubmitCommandValidator`.
  - [x] Confirm removal events and rejections use the managed tenant ID from `CommandEnvelope.AggregateId`, not `command.TenantId`, when body and envelope diverge.

- [x] Add the structured missing-key rejection contract (AC: 2, 5)
  - [x] Create a contracts rejection record for the absent configuration key, named consistently with existing rejection conventions, for example `ConfigurationKeyNotFoundRejection(string TenantId, string Key) : IRejectionEvent`.
  - [x] Keep rejection fields structured only: `TenantId` and `Key`; do not add prose `Message`, `Reason`, `Detail`, payload JSON, tokens, stack traces, or sensitive values.
  - [x] Add a replay-only `TenantState.Apply(ConfigurationKeyNotFoundRejection)` method if aggregate replay needs to recognize persisted rejection events, matching the existing rejection `Apply` pattern.
  - [x] Let reflection-driven contracts tests cover naming and serialization, and add targeted assertions only if the generic tests do not expose the new contract clearly enough.

- [x] Change aggregate behavior for missing configuration keys (AC: 1, 2, 4)
  - [x] Keep `TenantAggregate.Handle(RemoveTenantConfiguration, TenantState?, CommandEnvelope)` as a pure static handler with no async, I/O, DAPR calls, service dependencies, or thrown business-rule exceptions.
  - [x] Preserve branch ordering: null state -> disabled state -> owner/global-admin authorization -> missing-key rejection -> success event.
  - [x] Replace the current missing-key `DomainResult.NoOp()` behavior with `DomainResult.Rejection([new ConfigurationKeyNotFoundRejection(tenantId, command.Key)])`.
  - [x] Preserve exact key text in success and rejection payloads; do not trim, normalize casing, split namespaces, or reinterpret dot-delimited keys.
  - [x] Keep `TenantConfigurationRemoved(tenantId, command.Key)` as the only success event for an existing key.

- [x] Preserve owner-scoped authorization and global-admin bypass (AC: 1, 3)
  - [x] Require `TenantOwner` for non-global-admin actors; `TenantContributor`, `TenantReader`, and non-members must receive `InsufficientPermissionsRejection`.
  - [x] Preserve trusted global-admin bypass only through server-populated `CommandEnvelope.Extensions["actor:globalAdmin"] == "true"` with exact ordinal comparison.
  - [x] Keep Tenants domain RBAC inside the aggregate handler; do not move it to controllers, validators, client code, or EventStore tenant/RBAC validators.
  - [x] Ensure disabled tenants reject before authorization, so disabled-state failures do not leak role or key existence information.

- [x] Verify state, projections, fakes, and consumers remain aligned (AC: 1, 5)
  - [x] Keep `TenantState.Apply(TenantConfigurationRemoved)` removing the exact key from the aggregate configuration dictionary.
  - [x] Keep `TenantReadModel.Apply(TenantConfigurationRemoved)`, `TenantProjectionHandler`, `TenantProjectionEventHandler`, and `InMemoryTenantProjection` removal behavior intact unless tests expose a real gap.
  - [x] Keep `InMemoryTenantService` delegating to `TenantAggregate.Handle`; do not reimplement removal or missing-key logic in the fake.
  - [x] Confirm `TenantConfigurationSet` with `Value = ""` still means "key exists with empty value"; only `TenantConfigurationRemoved` means "key absent".

- [x] Update tests for the new story semantics (AC: 1-5)
  - [x] Update `RemoveTenantConfiguration_nonexistent_key_produces_NoOp` to expect `ConfigurationKeyNotFoundRejection` and zero success events.
  - [x] Add or preserve tests for existing-key success, applied aggregate state removal, trusted global-admin success, envelope aggregate-id source of truth, null-state rejection, disabled-state rejection, reader/contributor/non-member rejection, and exact key preservation.
  - [x] Add a conformance test proving production `TenantAggregate` and `InMemoryTenantService` produce the same missing-key rejection sequence for `RemoveTenantConfiguration`.
  - [x] Add or preserve validator and submit-command validator coverage for null/empty keys; keep whitespace-key behavior intentional and documented by tests if it remains allowed.
  - [x] Update command API rejection ProblemDetails expectations if the new rejection type participates in the EventStore rejection catalog or reflection-driven mapping tests.

- [x] Update documentation and operational references (AC: 2, 5)
  - [x] Update `docs/event-contract-reference.md` so `RemoveTenantConfiguration` lists the new missing-key rejection instead of the current NoOp behavior.
  - [x] Update the quick reference rejection table for `RemoveTenantConfiguration`.
  - [x] Avoid unrelated docs churn; this is not a query, UI, projection durability, or limit-enforcement story.

- [x] Run focused validation (AC: 1-5)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~RemoveTenantConfigurationValidatorTests|FullyQualifiedName~TenantSubmitCommandValidatorTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests|FullyQualifiedName~InMemoryTenantProjection" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution plus Release build evidence as fallback validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.6 specifically changes removal behavior for existing and missing configuration keys. It does not own set-configuration behavior, full limit-boundary enforcement, optimistic concurrency, query endpoints, Phase 2 UI, or new public REST endpoints. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.6: Remove Tenant Configuration Entries`]
- PRD FR20 requires tenant owners to remove configuration entries, FR22 requires domain events for configuration changes, and FR21 keeps keys as dot-delimited namespace strings such as `billing.plan` and `parties.maxContacts`. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Configuration`]
- PRD FR31-FR34 require tenant roles to remain tenant-scoped: `TenantOwner` can manage configuration, while `TenantReader` and `TenantContributor` cannot manage Tenants configuration. [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- NFR8 requires disabled tenants to reject commands immediately inside the aggregate, and NFR10 requires branch coverage for tenant isolation and role authorization logic. [Source: `_bmad-output/planning-artifacts/prd.md#Security`]

### Current Repository State

- `RemoveTenantConfiguration` already exists as a command record and `TenantConfigurationRemoved` already exists as an `IEventPayload` success event. Do not create replacement command/event types. [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`; `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs`]
- `TenantAggregate.Handle(RemoveTenantConfiguration, TenantState?, CommandEnvelope)` already derives `tenantId` from `envelope.AggregateId`, checks null/disabled state before RBAC, requires owner/global-admin authority, and emits `TenantConfigurationRemoved` for existing keys. The required change is the missing-key branch: it currently returns `DomainResult.NoOp()` but Story 3.6 requires a structured rejection. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- There is no existing `ConfigurationKeyNotFoundRejection` contract. Existing rejection names use structured record contracts under `src/Hexalith.Tenants.Contracts/Events/Rejections/` and are verified by reflection tests. [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections`; `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`]
- `TenantState.Apply(TenantConfigurationRemoved)` already removes the exact configuration key. Do not convert state to immutable records or add validation to `Apply`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- `RemoveTenantConfigurationValidator` currently validates non-empty `TenantId`, non-null `Key`, and minimum key length of 1. It intentionally does not enforce max key length; preserve or change only with explicit test evidence. [Source: `src/Hexalith.Tenants.Server/Validators/RemoveTenantConfigurationValidator.cs`; `tests/Hexalith.Tenants.Server.Tests/Validators/RemoveTenantConfigurationValidatorTests.cs`]
- Existing aggregate tests already cover remove success, envelope aggregate-id source of truth, null state, disabled state, reader/contributor/non-member rejection, global-admin success, and current non-existent-key NoOp behavior. Update the missing-key test and add focused gaps; do not rewrite the class wholesale. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- `docs/event-contract-reference.md` currently documents `RemoveTenantConfiguration` missing keys as NoOp and omits the missing-key rejection from the quick reference table. This must be corrected with the behavior change. [Source: `docs/event-contract-reference.md#RemoveTenantConfiguration`; `docs/event-contract-reference.md#Quick Reference`]

### Previous Story Intelligence

- Story 3.1 established `TenantRole.Unknown = 0`, string enum serialization, trusted `actor:globalAdmin` envelope metadata, disabled-tenant branch precedence, and first-user bootstrap for `AddUserToTenant` only. Preserve these decisions. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- Story 3.2 established that tenant-scoped event/rejection payloads should derive tenant identity from `CommandEnvelope.AggregateId` when the command body diverges. Keep this rule for `RemoveTenantConfiguration`. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`]
- Story 3.4 specifically hardened `RemoveTenantConfiguration` to emit success/rejection payload tenant IDs from `CommandEnvelope.AggregateId` and tightened global-admin bypass to exact `actor:globalAdmin=true`. Do not regress this while changing missing-key semantics. [Source: `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md#Completion Notes List`; `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md#Senior Developer Review (AI)`]
- Story 3.5 completed configuration-set hardening and added API routing coverage for `SetTenantConfiguration`; it also documented that `SetTenantConfiguration` same-key/same-value remains NoOp. Do not copy that idempotent set behavior into Story 3.6 because the 3.6 AC now requires a missing-key rejection. [Source: `_bmad-output/implementation-artifacts/3-5-set-tenant-configuration-entries.md`]
- The historical `_bmad-output/implementation-artifacts/3-3-tenant-configuration-management.md` says missing-key removal is NoOp because no missing-key rejection existed at that time. Treat that as superseded by current Story 3.6 acceptance criteria and the 2026-05-31 readiness note that missing configuration key semantics are resolved as structured rejection outcomes. [Source: `_bmad-output/implementation-artifacts/3-3-tenant-configuration-management.md`; `_bmad-output/planning-artifacts/epics.md#Epic Readiness Status`]
- Recent commits show current Epic 3 implementation order: `a00d490 feat(story-3.1)`, `f807411 feat(story-3.2)`, `d6e5fc4 feat(story-3.3)`, `47bb606 feat(story-3.4)`, and `fec602a feat(story-3.5)`. [Source: `git log --oneline -5`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, central package management, DAPR `1.17.9`, FluentValidation `12.1.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. Do not bump SDK or packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Aggregate `Handle` methods must remain pure static functions: no async, no I/O, no DAPR calls, no service dependencies, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are discovered from the `Hexalith.Tenants.Server` assembly by `Handle` signature. Do not move `TenantAggregate`, rename `Handle`, or alter dispatch-compatible signatures. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC for Tenants' own commands belongs inside aggregate handlers. EventStore `IRbacValidator`/`ITenantValidator` interfaces are for consuming services and API-gate validation; moving Tenants domain RBAC there creates a circular dependency. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- The trusted global-admin bypass is indicated by `actor:globalAdmin` on `CommandEnvelope.Extensions`; client-submitted reserved extensions are untrusted. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Every event and rejection payload must include top-level managed `TenantId` because the EventStore envelope tenant is `system`. [Source: `_bmad-output/project-context.md#Identity Scheme`; `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`]
- Error responses follow EventStore Problem Details mapping. New rejection events should be included in `CommandApiRuntimeIntegrationTests.TenantRejectionProblemDetailsExpectations` if the catalog coverage expects every Tenants rejection type to be represented. Default non-not-found/non-conflict domain rejections map to 422 unless EventStore has an explicit override. [Source: `_bmad-output/project-context.md#API Surface`; `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- Tests use xUnit v3 plus Shouldly, global `using Xunit`, K&R brace style, and snake_case test method names. Do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationKeyNotFoundRejection.cs` - new structured rejection for AC2.
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` - change missing-key branch from NoOp to structured rejection.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` - add replay-only `Apply(ConfigurationKeyNotFoundRejection)` if required by aggregate replay conventions.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` - update missing-key behavior and preserve branch-order/identity/RBAC tests.
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` - add/update production fake parity for missing-key rejection.
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs` and `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` - likely covered by reflection; adjust only if needed for the new rejection constructor/test value.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - update rejection ProblemDetails catalog if reflection coverage requires all rejection events.
- `docs/event-contract-reference.md` - update `RemoveTenantConfiguration` rejection and NoOp documentation.

### Out of Scope

- Changing `SetTenantConfiguration` semantics, same-value NoOp behavior, or configuration limit handling; Story 3.7 owns exhaustive limits.
- Optimistic concurrency behavior; Story 3.8 owns conflicting concurrent modifications.
- Query endpoints, cursor pagination, projection durability, audit query behavior, or Phase 2 UI/FrontComposer implementation.
- New tenant roles, custom/extensible roles, or adding `GlobalAdministrator` to `TenantRole`.
- Package, SDK, DAPR, Aspire, EventStore, submodule, or CI workflow changes.

### Latest Technical Context

- No external package or API upgrade is required. Use the pinned local stack and existing EventStore/DAPR/Aspire APIs. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- The current technical correction is local and story-specific: `RemoveTenantConfiguration` missing-key behavior must change from the old historical NoOp design to a structured rejection required by the current Epic 3.6 acceptance criteria.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.6: Remove Tenant Configuration Entries`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Configuration`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements Traceability Matrix`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#API Surface`]
- [Source: `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md`]
- [Source: `_bmad-output/implementation-artifacts/3-5-set-tenant-configuration-entries.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs`]
- [Source: `src/Hexalith.Tenants.Server/Validators/RemoveTenantConfigurationValidator.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- [Source: `docs/event-contract-reference.md#RemoveTenantConfiguration`]

## Project Structure Notes

- Alignment: Story 3.6 stays in the Contracts/Server aggregate/validator/test, Testing parity, Integration catalog, and event-contract docs areas. It should not add projects, package dependencies, controllers, query endpoints, or UI files.
- Detected variance: current code and docs still encode historical missing-key NoOp behavior. This story intentionally supersedes that behavior with a structured rejection, matching the current 3.6 AC and readiness status.
- Keep additions localized. The primary production change should be a new rejection contract plus one aggregate branch change, not a broader configuration subsystem refactor.
- The dirty worktree currently contains an unrelated modification to `_bmad-output/story-automator/orchestration-1-20260531-113112.md`. Do not revert or rewrite it as part of this story.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.6, PRD FR20-FR24/FR31-FR34, NFR8/NFR10, architecture/project context, and previous Stories 3.1-3.5.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `RemoveTenantConfiguration`, `TenantConfigurationRemoved`, `RemoveTenantConfigurationValidator`, aggregate tests, submit-command validator tests, conformance tests, in-memory service/projection, projection handlers, integration command API tests, and event contract docs.
- Previous-story intelligence incorporated trusted global-admin envelope metadata, disabled-state branch precedence, envelope aggregate-id consistency, exact global-admin extension comparison, current Story 3.5 validation fallback notes, and the historical NoOp decision that this story now supersedes.
- Disaster-prevention guardrails included: do not duplicate existing removal contracts, do not move RBAC out of the aggregate, do not trust command-body tenant IDs over envelope aggregate IDs, do not normalize configuration keys, do not leave docs claiming missing-key NoOp, and do not reimplement fake behavior separately from production aggregate logic.
- Latest technical context reviewed from local pins and source. No external package-version change or web research is needed for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: focused server `dotnet test` failed before implementation with `CS0246` for missing `ConfigurationKeyNotFoundRejection`.
- VSTest validation: `dotnet test` hit `System.Net.Sockets.SocketException (13): Permission denied` at `System.Net.Sockets.TcpListener..ctor` / `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Fallback validation used direct xUnit v3 in-process test assembly execution plus Release solution build evidence.
- Review validation: direct xUnit fallback passed focused Contracts (31), Server (149), Testing (75), and Integration (70) checks; Release solution build passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added `ConfigurationKeyNotFoundRejection` with structured `TenantId` and `Key` fields and replay-only state handling.
- Changed `RemoveTenantConfiguration` missing-key behavior from `DomainResult.NoOp()` to a structured rejection while preserving envelope aggregate ID source of truth, branch order, exact key text, owner RBAC, and trusted global-admin bypass.
- Updated aggregate, conformance, validator, submit-command, contracts, and ProblemDetails catalog coverage for the new removal semantics.
- Review auto-fix added direct coverage that applying `TenantConfigurationRemoved` removes the key from aggregate state and that disabled remove commands reject before RBAC even for a reader actor.
- Updated event contract documentation to remove the historical missing-key NoOp behavior and list the new rejection in the command section, rejection table, and quick reference.
- Validation passed through direct xUnit fallback and Release build after VSTest socket restrictions prevented normal `dotnet test` execution.

### File List

- `_bmad-output/implementation-artifacts/3-6-remove-tenant-configuration-entries.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationKeyNotFoundRejection.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/RemoveTenantConfigurationValidatorTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`

### Change Log

- 2026-06-01: Implemented Story 3.6 missing-key removal rejection semantics, test coverage, docs updates, and validation evidence.
- 2026-06-01: Senior review auto-fixed focused aggregate test coverage gaps, reran validation, and marked Story 3.6 done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix.

### Findings

- Medium: `RemoveTenantConfiguration_existing_key_produces_TenantConfigurationRemoved` verified the event but did not prove the applied aggregate state no longer contained the key required by AC1. Fixed in `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`.
- Medium: Disabled-tenant branch precedence for `RemoveTenantConfiguration` was not directly covered with an unauthorized reader actor, leaving the AC4/NFR8 no-information-leak ordering claim under-tested. Fixed in `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`.
- Medium: `_bmad-output/implementation-artifacts/tests/test-summary.md` had changed but was missing from the story File List. Fixed by adding it to the File List.

### Validation

- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~RemoveTenantConfigurationValidatorTests|FullyQualifiedName~TenantSubmitCommandValidatorTests" -m:1 -nr:false /p:NuGetAudit=false` - compiled, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false` - passed.
- Direct xUnit fallback: focused Server classes passed, 149 total, 0 failed, 0 skipped.
- Direct xUnit fallback: focused Contracts classes passed, 31 total, 0 failed, 0 skipped.
- Direct xUnit fallback: focused Testing classes passed, 75 total, 0 failed, 0 skipped.
- Direct xUnit fallback: `CommandApiRuntimeIntegrationTests` passed, 70 total, 0 failed, 0 skipped.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` - passed with 0 warnings and 0 errors.
