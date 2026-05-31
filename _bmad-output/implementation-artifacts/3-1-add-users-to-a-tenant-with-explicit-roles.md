---
baseline_commit: ecc5b252e5c95bcc4f5627998aa23b8c7bd79064
---

# Story 3.1: Add Users to a Tenant with Explicit Roles

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to add a user to my tenant with a specific tenant role,
so that I can grant access intentionally and produce an auditable membership event.

## Acceptance Criteria

1. Given a tenant exists and is enabled, when a TenantOwner submits `AddUserToTenant` with a target user and role `TenantReader`, `TenantContributor`, or `TenantOwner`, then the tenant aggregate records one `UserAddedToTenant` event with top-level `TenantId`, target `UserId`, and assigned `Role`.
2. Given a tenant has no membership history, when the first membership is added through the approved empty-tenant bootstrap path, then the aggregate allows the first-user membership flow, and subsequent membership additions require normal TenantOwner or trusted global-admin authority.
3. Given the target user is already a tenant member, when `AddUserToTenant` is submitted again, then the aggregate returns `UserAlreadyInTenantRejection` with the existing role, and no additional `UserAddedToTenant` event is produced.
4. Given the command attempts to assign a role outside the supported tenant role set, when the aggregate or validator handles the command, then the command is rejected before an event is produced, and `GlobalAdministrator` is not represented as a tenant role.
5. Given membership add tests run, when authorized, duplicate, invalid-role, disabled-tenant, missing-tenant, insufficient-permission, global-admin, and bootstrap cases are exercised, then tests verify events, rejections, and state transitions without live infrastructure.
6. Given `TenantRole.Unknown` is ordinal `0`, when an add-user command carries `Unknown`, an undefined role, a missing role field, or an unrecognized role name, then the command fails closed with validation failure or `RoleEscalationRejection`, and no membership event is produced.
7. Given an add-user event is persisted through EventStore, when consumers inspect the event, then actor context and timestamp data come from the EventStore envelope metadata, while the `UserAddedToTenant` payload remains the domain payload (`TenantId`, `UserId`, `Role`).

## Tasks / Subtasks

- [x] Verify existing contracts and serialization before changing code (AC: 1, 3, 4, 6, 7)
  - [x] Confirm `AddUserToTenant`, `UserAddedToTenant`, `UserAlreadyInTenantRejection`, and `RoleEscalationRejection` already exist in `src/Hexalith.Tenants.Contracts`; do not create duplicate contract types.
  - [x] Confirm `TenantRole` has `Unknown = 0`, serializes by name with `JsonStringEnumConverter<TenantRole>`, and contains only `Unknown`, `TenantOwner`, `TenantContributor`, and `TenantReader`.
  - [x] If docs or tests imply actor/timestamp fields belong in `UserAddedToTenant`, correct that assumption: actor/timestamp are EventStore envelope metadata, not payload fields.

- [x] Implement or preserve `AddUserToTenant` aggregate behavior (AC: 1-6)
  - [x] Use `public static DomainResult Handle(AddUserToTenant command, TenantState? state, CommandEnvelope envelope)` in `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`.
  - [x] Guard `command` and `envelope` with `ArgumentNullException.ThrowIfNull`.
  - [x] Return `TenantNotFoundRejection` when state is `null`.
  - [x] Reject any non-`TenantStatus.Active` tenant with `TenantDisabledRejection` before RBAC, duplicate, or role checks.
  - [x] Enforce TenantOwner authority with `IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)`, unless `IsGlobalAdmin(envelope)` is true or `state.HasMembershipHistory == false`.
  - [x] Preserve the first-user bootstrap exception exactly: it skips owner-only RBAC only when `state.HasMembershipHistory == false`; it is not the same as global-admin bootstrap.
  - [x] Reject `TenantRole.Unknown` and out-of-range enum values through `IsAssignableRole`, returning `RoleEscalationRejection`.
  - [x] Reject duplicates with `UserAlreadyInTenantRejection(command.TenantId, command.UserId, existingRole)`.
  - [x] On success, return exactly one `UserAddedToTenant(command.TenantId, command.UserId, command.Role)`.

- [x] Verify command validation path (AC: 4, 6)
  - [x] Keep `AddUserToTenantValidator` in `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`; do not move validators into Contracts.
  - [x] Ensure validator rules require non-empty `TenantId`, non-empty `UserId`, `IsInEnum()`, and `NotEqual(TenantRole.Unknown)`.
  - [x] Ensure `TenantSubmitCommandValidator` validates `SubmitCommand` payloads for `AddUserToTenant` so API-submitted JSON is checked before domain dispatch.
  - [x] Ensure `Program.cs` registers both `TenantSubmitCommandValidator` assembly and `TenantAggregate` assembly validators.

- [x] Extend focused aggregate tests (AC: 1-6)
  - [x] Use the existing `CreateCommand<T>(command, actorUserId, isGlobalAdmin, aggregateId)` helper in `TenantAggregateTests.cs`; do not construct `CommandEnvelope` inline.
  - [x] Cover success for all three assignable roles.
  - [x] Cover null state, disabled/non-active state, duplicate membership with `ExistingRole`, `TenantRole.Unknown`, undefined enum value, TenantReader actor, TenantContributor actor, non-member actor, TenantOwner actor, trusted global admin, and first-user bootstrap.
  - [x] Assert `TenantState.Apply(UserAddedToTenant)` adds the user role and sets `HasMembershipHistory = true`.
  - [x] Assert disabled/non-active state wins over duplicate, RBAC, and role-validation branches.

- [x] Extend validation and serialization tests (AC: 4, 6, 7)
  - [x] Add or preserve validator tests for empty `TenantId`, empty `UserId`, undefined role, and `TenantRole.Unknown`.
  - [x] Add or preserve serialization tests proving role names round-trip and missing role payloads fail closed to `TenantRole.Unknown` or validation failure.
  - [x] Add or preserve command-pipeline validation tests proving invalid `AddUserToTenant` JSON is rejected through `TenantSubmitCommandValidator`.

- [x] Preserve Testing package parity (AC: 1-6)
  - [x] Keep `InMemoryTenantService.ProcessCommand(AddUserToTenant, userId, isGlobalAdmin)` delegating to `TenantAggregate.Handle`; do not reimplement membership logic in the fake.
  - [x] Update `TenantConformanceTests` only if behavior changes; conformance must compare identical event/rejection sequences and order between the real aggregate and `InMemoryTenantService`.
  - [x] Do not skip or weaken conformance tests.

- [x] Update public documentation only if implementation differs from docs (AC: 7)
  - [x] Verify `docs/event-contract-reference.md` documents `AddUserToTenant`, `UserAddedToTenant`, role-name serialization, and rejections accurately.
  - [x] Verify compensating-command docs still state that restoring a removed user requires an explicit role.
  - [x] Do not promise exactly-once pub/sub delivery or place actor/timestamp fields in domain payload examples.

- [x] Run focused validation (AC: 1-7)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~AddUserToTenantValidatorTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution or build evidence as partial validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, roles, role boundaries, tenant configuration, and tenant-scoped behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.1 covers adding users only. Removal and role-change behavior appear in Stories 3.2 and 3.3; do not expand this story into the older combined "user-role management" scope. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.1: Add Users to a Tenant with Explicit Roles`]
- PRD FR6, FR9, FR10, FR11, and FR12 apply directly: add user with role, reject duplicate membership, reject role escalation, produce user-role domain events, and preserve EventStore optimistic concurrency behavior. [Source: `_bmad-output/planning-artifacts/prd.md#User-Role Management`]
- PRD NFR6 and NFR8 apply: role escalation fails at the domain level, and disabled tenants reject state-changing tenant commands. [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- Architecture TEN-1/TEN-2 correction requires `TenantRole` and `TenantStatus` to serialize by name, reserve ordinal `0` as fail-closed `Unknown`, and reject `Unknown` in aggregate guards and validators. [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`]

### Current Repository State

- `AddUserToTenant`, `UserAddedToTenant`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, and `TenantRole` already exist under `src/Hexalith.Tenants.Contracts`. Do not add replacements or rename these contracts. [Source: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/UserAddedToTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/UserAlreadyInTenantRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/RoleEscalationRejection.cs`; `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- `TenantRole` currently has `Unknown = 0`, then `TenantOwner`, `TenantContributor`, and `TenantReader`, with `[JsonConverter(typeof(JsonStringEnumConverter<TenantRole>))]`. Preserve this fail-closed contract. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- `TenantAggregate` already contains a three-parameter `Handle(AddUserToTenant, TenantState?, CommandEnvelope)` with disabled-state rejection, owner/global-admin/first-user authorization, `IsAssignableRole`, duplicate detection, and `UserAddedToTenant` success. Treat this as the implementation to verify or preserve, not a reason to create a parallel handler. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState.Apply(UserAddedToTenant)` writes `Users[e.UserId] = e.Role` and sets `HasMembershipHistory = true`. Do not introduce a second membership collection such as `Members`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- `AddUserToTenantValidator` already rejects empty tenant/user IDs, undefined enum values, and `TenantRole.Unknown`. Keep the `NotEqual(TenantRole.Unknown)` guard; `IsInEnum()` alone accepts the sentinel. [Source: `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`]
- `TenantSubmitCommandValidator` dispatches `SubmitCommand` payload validation for `AddUserToTenant`, and `Program.cs` registers both the validator assembly and the server aggregate assembly validators. [Source: `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs`; `src/Hexalith.Tenants/Program.cs`]
- `InMemoryTenantService` delegates `AddUserToTenant` to `TenantAggregate.Handle`; keep test fakes aligned through delegation and conformance tests. [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`; `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]

### Previous Work Intelligence

- Epic 2 established that trusted global-admin authority is the server-populated `actor:globalAdmin` envelope extension. Do not parse raw claims or trust command payload fields inside aggregates. [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-31.md#Key Learnings`]
- Epic 2 established fail-closed enum behavior as security-sensitive. Unknown or malformed enum values must not map to privileged behavior. [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-31.md#Key Learnings`]
- Epic 2 action items explicitly call out `TenantRole.Unknown`, disabled-tenant rejection ordering, first-user membership bootstrap, and conformance tests as Epic 3 preparation. [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-31.md#Preparation for Epic 3`]
- Recent commits show the current codebase just completed tenant governance hardening and source-of-truth recovery: `8b66e85 docs(retro): sync epic 2 governance guidance`, `aad45de feat(story-2.7): preserve command source of truth when pub-sub is unavailable`, `fe6c361 feat(story-2.6): return structured tenant governance rejections`, `bd1e935 feat(story-2.5): disable and re-enable tenants`.

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `global.json`; `Directory.Packages.props`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Use K&R brace style, file-scoped namespaces, one type per file, System.Text.Json, collection expressions for result event lists, xUnit v3, Shouldly assertions, and no `Assert.*`. [Source: `_bmad-output/project-context.md#Language-Specific Rules (C# / .NET 10)`; `_bmad-output/project-context.md#Testing Rules`]
- Aggregate Handle methods must stay pure: no async, no I/O, no DAPR, no service calls, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are auto-discovered from `Hexalith.Tenants.Server` by signature. Do not move `TenantAggregate` or change `Handle` method names/signatures. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC belongs inside aggregate Handle methods for Tenants' own authorization. Do not move this story's owner/global-admin check to EventStore tenant validators. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Trusted global-admin bypass is based only on sanitized `actor:globalAdmin` envelope metadata. Client-submitted reserved extensions are untrusted. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Identifiers are case-sensitive. Do not normalize `UserId` or tenant IDs during membership checks. [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`; `docs/production-auth-claim-contract.md`]
- `UserAddedToTenant` payload contains tenant/user/role. EventStore envelope metadata provides actor and timestamp. Do not add actor/timestamp payload fields to satisfy AC7 unless architecture explicitly changes. [Source: `docs/event-contract-reference.md#Event Envelope Metadata`; `docs/event-contract-reference.md#AddUserToTenant`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` only if the current `AddUserToTenant` behavior has a gap.
- `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs` only if validator tests expose a gap.
- `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs` only if payload validation no longer covers `AddUserToTenant`.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/AddUserToTenantValidatorTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` only if behavior changes.
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` only if behavior changes.
- `docs/event-contract-reference.md` and `docs/compensating-commands.md` only if documentation no longer matches implemented contracts.

### Out of Scope

- Creating, removing, or renaming command/event/rejection contracts.
- Implementing `RemoveUserFromTenant` behavior except preserving existing tests that this story might affect.
- Implementing `ChangeUserRole` behavior except preserving shared `TenantRole.Unknown` and validation semantics.
- Adding a must-retain-one-owner invariant.
- Adding `GlobalAdministrator` to `TenantRole`.
- Adding actor/timestamp fields to `UserAddedToTenant` payload.
- Adding REST endpoints outside the existing EventStore command gateway.
- Changing DAPR, Aspire, EventStore, package versions, submodule topology, or CI workflow.

### Latest Technical Context

- No package or API upgrade is required for this story. Use the versions pinned in `Directory.Packages.props`: FluentValidation `12.1.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, DAPR `1.17.9`, and .NET SDK `10.0.300`.
- The relevant "latest" correction is local architecture, not a library upgrade: TEN-1/TEN-2 supersedes the older story file `_bmad-output/implementation-artifacts/3-1-user-role-management.md`, which warned that `TenantRole.TenantOwner = 0`. The current contract is `TenantRole.Unknown = 0`; do not reintroduce the old ordinal behavior.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.1: Add Users to a Tenant with Explicit Roles`]
- [Source: `_bmad-output/planning-artifacts/prd.md#User-Role Management`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`]
- [Source: `_bmad-output/project-context.md#Aggregates`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#CommandEnvelope Test Helper`]
- [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-05-31.md#Preparation for Epic 3`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- [Source: `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`]
- [Source: `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs`]
- [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- [Source: `docs/event-contract-reference.md#AddUserToTenant`]
- [Source: `docs/production-auth-claim-contract.md`]

## Project Structure Notes

- Alignment: Story 3.1 is a Server aggregate/validator/test story using existing Contracts, Testing, and documentation surfaces.
- The older `_bmad-output/implementation-artifacts/3-1-user-role-management.md` is a completed artifact from a previous story breakdown and has broader scope than the current sprint's Story 3.1. Do not overwrite or follow it blindly.
- Current source appears to already contain most Story 3.1 behavior. The dev agent should verify the current implementation against this story, close gaps with focused edits, and preserve established behavior.
- Most likely implementation risk is regression by "cleaning up" the first-user bootstrap exception, trusted global-admin metadata, or `TenantRole.Unknown` fail-closed behavior.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.1, PRD user-role requirements, PRD security NFRs, architecture TEN-1/TEN-2 hardening, and project context rules.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `AddUserToTenantValidator`, `TenantSubmitCommandValidator`, `Program.cs`, `InMemoryTenantService`, aggregate tests, validator tests, conformance tests, and event-contract docs.
- Previous-work intelligence incorporated Epic 2 retrospective guidance: trusted global-admin envelope metadata, envelope identity discipline, fail-closed enum defaults, disabled tenant rejection ordering, and conformance coverage.
- Disaster-prevention guardrails included: do not duplicate contracts, do not widen this story into remove/change role, do not add `GlobalAdministrator` as a tenant role, do not use old `TenantOwner = 0` guidance, do not add actor/timestamp to `UserAddedToTenant` payload, and do not bypass aggregate-domain RBAC.
- Latest technical context reviewed from local pins and architecture corrections. No external package-version change is needed.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: Confirmed existing contracts and aggregate implementation already satisfy the domain behavior requirements; no production code changes were needed.
- 2026-05-31: `dotnet test` via VSTest aborted in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied` for Server, Testing, and Contracts test projects after successful compilation.
- 2026-05-31: Direct xUnit fallback passed against Debug and Release-built assemblies: Server focused classes (`TenantAggregateTests`, `AddUserToTenantValidatorTests`, `TenantSubmitCommandValidatorTests`) 113/113; Testing focused classes (`TenantConformanceTests`, `InMemoryTenantServiceTests`) 68/68; Contracts full suite 73/73.
- 2026-05-31: Release build passed with 0 warnings and 0 errors.
- 2026-05-31 review: VSTest validation rebuilt Server, Contracts, and Testing test assemblies, then aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-05-31 review: Direct xUnit validation passed: Server focused classes 113/113; Contracts full suite 73/73; Testing focused classes 68/68.
- 2026-05-31 review: Integration test project build passed with 0 warnings and 0 errors; direct xUnit integration validation passed 63 total, 0 failed, 12 skipped for missing DAPR prerequisites.
- 2026-05-31 review: Release solution build passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Preserved the existing `AddUserToTenant` aggregate behavior and validator path; implementation already matched the story requirements.
- Added focused regression coverage for `TenantState.HasMembershipHistory`, disabled-tenant ordering over RBAC/role validation, `TenantRole.Unknown` validation, missing/unrecognized add-user role payloads, and AddUser role-name serialization round trips.
- Verified documentation already keeps actor/timestamp data in EventStore envelope metadata and keeps `UserAddedToTenant` as a tenant/user/role payload.

### File List

- `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.Tenants.Contracts.Tests/EnumFailSafeTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/AddUserToTenantValidatorTests.cs`

### Senior Developer Review (AI)

Reviewer: Jerome on 2026-05-31

Outcome: Approved after auto-fix.

Findings:

- [x] [AI-Review][Medium] Story File List did not include changed integration test files, obscuring part of the implemented validation surface. Fixed by adding `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` to the File List.

Review notes:

- Acceptance Criteria 1-7 were cross-checked against `TenantAggregate`, `TenantState`, validators, command-pipeline validation, Testing fake delegation, documentation, and changed tests.
- No production code defect was found. Existing aggregate behavior preserves first-user bootstrap, trusted global-admin bypass, duplicate rejection with existing role, fail-closed `TenantRole.Unknown`, and envelope-owned actor/timestamp metadata.
- VSTest remains blocked by this sandbox's socket permissions; direct xUnit and Release build validation passed.

### Change Log

- 2026-05-31: Completed Story 3.1 validation and added focused AddUserToTenant fail-closed regression tests.
- 2026-05-31: Senior developer review completed; fixed File List discrepancy and approved Story 3.1.
