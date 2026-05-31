---
baseline_commit: a00d4907f0ee5d9d267f91fb4bb9e1c0b6ae444a
---

# Story 3.2: Remove Users from a Tenant

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to remove a user's tenant membership,
so that I can revoke access while preserving immutable audit history.

## Acceptance Criteria

1. Given a tenant exists, is enabled, and contains the target user, when a TenantOwner submits `RemoveUserFromTenant`, then the tenant aggregate records one `UserRemovedFromTenant` event, and applying that event removes the user from `TenantState.Users`.
2. Given the target user is not a tenant member, when `RemoveUserFromTenant` is handled by an authorized actor, then the aggregate returns `UserNotInTenantRejection`, and no `UserRemovedFromTenant` event is produced.
3. Given the target user is the last `TenantOwner` in the tenant, when a TenantOwner or trusted global administrator submits a valid removal command, then the aggregate allows the removal, and tests document that the backend does not enforce a must-retain-one-owner invariant.
4. Given a removed user attempts a subsequent tenant-owner-only command, when the aggregate evaluates current tenant membership, then the command is rejected with `InsufficientPermissionsRejection`, and the previous membership grants no residual authority.
5. Given removal tests run, when authorized, non-member, disabled-tenant, missing-tenant, insufficient-permission, global-admin, and last-owner cases are exercised, then tests verify event production, rejections, and final aggregate state without live infrastructure.
6. Given a removal event is persisted through EventStore, when consumers inspect the event stream and projections, then the immutable event and envelope metadata preserve auditability while `UserRemovedFromTenant` remains a payload of only `TenantId` and `UserId`.

## Tasks / Subtasks

- [x] Verify existing removal contracts and documentation before changing code (AC: 1, 2, 6)
  - [x] Confirm `RemoveUserFromTenant`, `UserRemovedFromTenant`, `UserNotInTenantRejection`, and `InsufficientPermissionsRejection` already exist in `src/Hexalith.Tenants.Contracts`; do not create duplicate contract types.
  - [x] Confirm `UserRemovedFromTenant` payload remains `TenantId` and `UserId` only; actor, timestamp, correlation, and causation context belong to the EventStore envelope.
  - [x] Confirm `docs/event-contract-reference.md` and `docs/compensating-commands.md` still describe removal, rejection, and explicit compensating re-add behavior accurately.

- [x] Implement or preserve `RemoveUserFromTenant` aggregate behavior (AC: 1-4)
  - [x] Use `public static DomainResult Handle(RemoveUserFromTenant command, TenantState? state, CommandEnvelope envelope)` in `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`.
  - [x] Guard `command` and `envelope` with `ArgumentNullException.ThrowIfNull`.
  - [x] Return `TenantNotFoundRejection(command.TenantId)` when `state` is `null`.
  - [x] Reject any non-`TenantStatus.Active` tenant with `TenantDisabledRejection(command.TenantId)` before RBAC or membership checks.
  - [x] Enforce TenantOwner authority with `IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)`, unless `IsGlobalAdmin(envelope)` is true.
  - [x] Reject non-members with `UserNotInTenantRejection(command.TenantId, command.UserId)` after RBAC passes.
  - [x] On success, return exactly one `UserRemovedFromTenant(command.TenantId, command.UserId)`.
  - [x] Do not add a last-owner protection check; last-owner removal is allowed by the ownership-transfer design.

- [x] Verify state mutation and post-removal authority (AC: 1, 3, 4)
  - [x] Confirm `TenantState.Apply(UserRemovedFromTenant)` removes only `e.UserId` from `Users` and does not reset `HasMembershipHistory`.
  - [x] Add or preserve a test proving the target user is absent from state after applying `UserRemovedFromTenant`.
  - [x] Add or preserve a test proving a removed former owner cannot subsequently run `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, or `RemoveTenantConfiguration` unless they regain membership or trusted global-admin metadata.
  - [x] Add or preserve a last-owner removal test for both TenantOwner and global-admin actors.

- [x] Extend focused aggregate tests (AC: 1-5)
  - [x] Use the existing `CreateCommand<T>(command, actorUserId, isGlobalAdmin, aggregateId)` helper in `TenantAggregateTests.cs`; do not construct `CommandEnvelope` inline.
  - [x] Cover success by TenantOwner and trusted global administrator.
  - [x] Cover null state, disabled/non-active state, target not in tenant, TenantReader actor, TenantContributor actor, actor not in tenant, and last-owner removal.
  - [x] Assert `InsufficientPermissionsRejection.ActorRole` is `null` for non-members and the actual role for insufficient members.
  - [x] Assert disabled/non-active state wins over RBAC and target-membership branches.
  - [x] Assert successful removal emits no role payload and does not mutate unrelated members.

- [x] Preserve Testing package parity and projection expectations (AC: 1, 4, 5)
  - [x] Keep `InMemoryTenantService.ProcessCommand(RemoveUserFromTenant, userId, isGlobalAdmin)` delegating to `TenantAggregate.Handle`; do not reimplement removal logic in the fake.
  - [x] Update or preserve `TenantConformanceTests` so real aggregate and in-memory fake produce identical event/rejection sequences and order for removal scenarios.
  - [x] Verify in-memory and server projection tests still remove memberships from tenant/member indexes when `UserRemovedFromTenant` is applied.

- [x] Run focused validation (AC: 1-6)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests|FullyQualifiedName~InMemoryTenantProjectionTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution or build evidence as partial validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, roles, role boundaries, tenant configuration, and tenant-scoped behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.2 covers removing users only. Do not expand this story into role changes, configuration, query endpoints, or Phase 2 UI command-lifecycle implementation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Remove Users from a Tenant`]
- PRD FR7, FR11, and FR12 apply directly: remove tenant members, produce user-role domain events, and rely on EventStore optimistic concurrency for conflicting aggregate modifications. [Source: `_bmad-output/planning-artifacts/prd.md#User-Role Management`]
- PRD NFR6, NFR7, NFR8, and NFR10 apply: domain-level role boundaries, auditable events with envelope metadata, disabled-tenant rejection, and branch coverage for role authorization. [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- PRD Journey 7 frames removal as a security incident-response command; after the aggregate processes `UserRemovedFromTenant`, later tenant commands by that removed actor must fail from current membership state. [Source: `_bmad-output/planning-artifacts/prd.md#Journey 7: Sofia Manages Tenant Security - Reactive and Proactive`]

### Current Repository State

- `RemoveUserFromTenant`, `UserRemovedFromTenant`, and `UserNotInTenantRejection` already exist in Contracts. Do not rename them or add alternate rejection names such as `MembershipNotFoundRejection`. [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/UserNotInTenantRejection.cs`]
- `TenantAggregate.Handle(RemoveUserFromTenant, TenantState?, CommandEnvelope)` already contains null-state, disabled-state, TenantOwner/global-admin RBAC, non-member rejection, and success-event branches. Treat this as implementation to verify or harden, not a reason to create parallel behavior. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState.Apply(UserRemovedFromTenant)` removes the user from `Users` and intentionally does not clear `HasMembershipHistory`; preserving history matters because the first-user bootstrap exception must not reopen after all members are removed. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`; `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md#Current Repository State`]
- `InMemoryTenantService` delegates `RemoveUserFromTenant` to `TenantAggregate.Handle` through both the typed `ProcessCommand` overload and `ProcessTenantCommand<T>`. Keep this delegation so test fakes stay production-equivalent. [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- Existing aggregate tests cover basic removal success, null state, disabled state, non-member rejection, and RBAC success/failure. This story should close gaps around final state application, last-owner removal, post-removal authority, and conformance parity if they are missing. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- Event contract docs already list `RemoveUserFromTenant`, `UserRemovedFromTenant`, `UserNotInTenantRejection`, and `InsufficientPermissionsRejection`. Compensating-command docs explicitly state restoring a wrongly removed user requires a new `AddUserToTenant` with an explicit role. [Source: `docs/event-contract-reference.md#RemoveUserFromTenant`; `docs/compensating-commands.md#Worked Example: Removing the Wrong User`]

### Previous Story Intelligence

- Story 3.1 is the authoritative previous story for current Epic 3 membership behavior. It supersedes the older completed `_bmad-output/implementation-artifacts/3-1-user-role-management.md`, whose broader combined scope and old enum-default warning are historical and should not drive this implementation. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`; `_bmad-output/implementation-artifacts/3-1-user-role-management.md`]
- Story 3.1 established `TenantRole.Unknown = 0`, string enum serialization, trusted `actor:globalAdmin` envelope metadata, disabled-tenant branch precedence, and the first-user bootstrap exception for `AddUserToTenant` only. Preserve those decisions. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md#Previous Work Intelligence`]
- Story 3.1 validation encountered VSTest socket restrictions in this sandbox, then used direct xUnit execution plus Release build as fallback evidence. Use the same fallback pattern if the sandbox blocks VSTest again. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md#Debug Log References`]
- Recent git history confirms the current baseline completed Story 3.1: `a00d490 feat(story-3.1): Add Users to a Tenant with Explicit Roles`. Use current source as the baseline before editing. [Source: `git log --oneline -5`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `global.json`; `Directory.Packages.props`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Use K&R brace style, file-scoped namespaces, one type per file, System.Text.Json, collection expressions for result event lists, xUnit v3, Shouldly assertions, and no `Assert.*`. [Source: `_bmad-output/project-context.md#Language-Specific Rules (C# / .NET 10)`; `_bmad-output/project-context.md#Testing Rules`]
- Aggregate Handle methods must stay pure: no async, no I/O, no DAPR, no service calls, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are auto-discovered from `Hexalith.Tenants.Server` by signature. Do not move `TenantAggregate` or change `Handle` method names/signatures. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC belongs inside aggregate Handle methods for Tenants' own authorization. Do not move removal authorization into EventStore tenant validators or controller branching. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Trusted global-admin bypass is based only on sanitized `actor:globalAdmin` envelope metadata. Client-submitted reserved extensions are untrusted. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- `TenantAggregate` does not enforce a "must retain at least one owner" invariant. Removing the last owner is allowed by design, with UX expected to surface owner-risk warnings. Do not add the invariant in this story. [Source: `_bmad-output/project-context.md#Aggregates`; `_bmad-output/planning-artifacts/epics.md#Story 3.2: Remove Users from a Tenant`]
- Identifiers are case-sensitive. Do not normalize `UserId` or tenant IDs during membership checks. [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`; `docs/production-auth-claim-contract.md`]

### UX and Documentation Context

- Phase 2 UX treats `RemoveUserFromTenant` as the first command-capable candidate, but this story is backend/domain behavior only. Do not implement UI shell, consequence preview, command lifecycle panel, audit receipt, SignalR, or FrontComposer work here. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Phase 2 - First Command-Capable Slice`]
- UX guidance still matters as a domain constraint: last-owner removal is a warning and consequence-preview concern, not a backend rejection. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#3. Decision`]
- Removal is not undoable. Recovery from a mistaken removal is a new explicit `AddUserToTenant` command with an explicit role; do not add auto-restore behavior or role data to the removal event. [Source: `docs/compensating-commands.md#Why the Role Must Be Explicitly Specified`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` only if verification finds a gap in removal behavior or branch ordering.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` only if `Apply(UserRemovedFromTenant)` fails to preserve the required state semantics.
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs` only if fake delegation or event application diverges.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs` or server projection tests only if projection behavior lacks removal coverage.
- `docs/event-contract-reference.md` and `docs/compensating-commands.md` only if documentation no longer matches implemented contracts.

### Out of Scope

- Creating, removing, or renaming command/event/rejection contracts.
- Implementing `ChangeUserRole`, tenant configuration, query endpoints, or UI command lifecycle behavior.
- Adding a must-retain-one-owner invariant.
- Reopening the first-user bootstrap exception after all users are removed.
- Adding `GlobalAdministrator` to `TenantRole`.
- Adding actor, role, timestamp, correlation, or causation fields to `UserRemovedFromTenant`.
- Adding REST endpoints outside the existing EventStore command gateway.
- Changing DAPR, Aspire, EventStore, package versions, submodule topology, or CI workflow.

### Latest Technical Context

- No package or API upgrade is required for this story. Use the versions pinned in `Directory.Packages.props`: FluentValidation `12.1.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, DAPR `1.17.9`, and .NET SDK `10.0.300`.
- The relevant current technical correction is local: `TenantRole.Unknown = 0` and the existing aggregate/domain RBAC pattern from Story 3.1 are authoritative. Do not reuse the older `TenantRole.TenantOwner = 0` guidance from `_bmad-output/implementation-artifacts/3-1-user-role-management.md`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Remove Users from a Tenant`]
- [Source: `_bmad-output/planning-artifacts/prd.md#User-Role Management`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Journey 7: Sofia Manages Tenant Security - Reactive and Proactive`]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#3. Decision`]
- [Source: `_bmad-output/project-context.md#Aggregates`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#CommandEnvelope Test Helper`]
- [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/UserNotInTenantRejection.cs`]
- [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- [Source: `docs/event-contract-reference.md#RemoveUserFromTenant`]
- [Source: `docs/compensating-commands.md`]

## Project Structure Notes

- Alignment: Story 3.2 is a Server aggregate/state/test story using existing Contracts, Testing, projection, and documentation surfaces.
- The existing `_bmad-output/implementation-artifacts/3-2-role-behavior-enforcement.md` is a completed artifact from a previous story breakdown and does not match the current sprint-status key. Do not overwrite it or follow it as the current 3.2 scope.
- Current source appears to already contain most removal behavior. The dev agent should verify the current implementation against this story, close focused test or documentation gaps, and preserve established behavior.
- Most likely implementation risks are adding an unintended last-owner invariant, reopening first-user bootstrap after removals, trusting client-supplied global-admin metadata, or expanding scope into UI and query work.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.2, PRD user-role and security requirements, UX last-owner/consequence guidance, current project context, and Story 3.1 previous-work intelligence.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `InMemoryTenantService`, aggregate tests, event-contract docs, and compensating-command docs.
- Previous-story intelligence incorporated Story 3.1 guidance: trusted global-admin envelope metadata, disabled-tenant rejection ordering, fail-closed role defaults, first-user bootstrap limited to add-user only, and direct xUnit fallback for VSTest socket restrictions.
- Disaster-prevention guardrails included: do not duplicate contracts, do not add `MembershipNotFoundRejection`, do not add last-owner backend rejection, do not add role/actor/timestamp to `UserRemovedFromTenant`, do not reimplement fake logic, and do not expand into Phase 2 UI.
- Latest technical context reviewed from local pins and architecture corrections. No external package-version change is needed.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: Verified existing contracts and docs; `RemoveUserFromTenant`, `UserRemovedFromTenant`, `UserNotInTenantRejection`, and `InsufficientPermissionsRejection` already existed, and docs already described removal/audit/compensating re-add behavior.
- 2026-05-31: VSTest focused Server, Testing, Contracts, and solution-level regression commands compiled test assemblies but aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-05-31: Direct xUnit focused Server aggregate validation passed: `TenantAggregateTests` total 100, 0 failed, 0 skipped.
- 2026-05-31: Direct xUnit focused Testing validation passed: `TenantConformanceTests`, `InMemoryTenantServiceTests`, and `InMemoryTenantProjectionTests` total 87, 0 failed, 0 skipped.
- 2026-05-31: Direct xUnit Contracts validation passed: total 74, 0 failed, 0 skipped.
- 2026-05-31: Release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` with 0 warnings, 0 errors.
- 2026-05-31: Direct xUnit Release regression passed for Sample, Client, Contracts, Integration, Server, and Testing assemblies: 864 total, 0 failed, 20 skipped for DAPR/performance prerequisites.
- 2026-06-01: Review VSTest focused Server, Testing, and Contracts commands compiled test assemblies but aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-06-01: Review direct xUnit focused validation passed for `TenantAggregateTests`, `TenantConformanceTests`, `InMemoryTenantServiceTests`, `InMemoryTenantProjectionTests`, `EventSerializationTests`, and `CommandApiRuntimeIntegrationTests`.
- 2026-06-01: Review Release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` with 0 warnings, 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Hardened `RemoveUserFromTenant` aggregate behavior so removal events and rejections use the EventStore envelope aggregate id.
- Added contract coverage proving `UserRemovedFromTenant` stays payload-only with `TenantId` and `UserId`.
- Strengthened aggregate tests for removal state mutation, unrelated member preservation, envelope aggregate-id consistency, insufficient-permission role details, global-admin last-owner removal, and removed-owner post-removal authority denial across owner-only commands.
- Added in-memory fake coverage proving removal delegates through production aggregate behavior and mutates fake state consistently.

### File List

- `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: `RemoveUserFromTenant` returned `TenantNotFoundRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, and `UserRemovedFromTenant` using `command.TenantId`. That could emit payloads for a tenant id different from the EventStore aggregate id if the command body and envelope diverged. Fixed in `TenantAggregate.Handle(RemoveUserFromTenant, ...)` by deriving the tenant id from `envelope.AggregateId`, and added regression tests for success, not-found, and non-member paths.
- MEDIUM: The story File List omitted changed source/test artifacts discovered by git, including the command API integration test and test-summary artifact. Updated the File List to match the actual review surface.

Review checklist:

- Story status verified as reviewable before review.
- Acceptance criteria cross-checked against aggregate, state, contract, fake, projection, documentation, and changed test surfaces.
- Tests mapped to removal success, non-member, disabled/missing tenant, insufficient-permission, global-admin, last-owner, state mutation, audit payload, and post-removal authority cases.
- Code quality and security review performed on changed source files.
- Sprint status sync prepared for `3-2-remove-users-from-a-tenant`.

### Change Log

- 2026-05-31: Completed Story 3.2 validation and focused test hardening for tenant user removal.
- 2026-06-01: Senior review fixed `RemoveUserFromTenant` envelope aggregate-id consistency, added regression tests, updated File List, and approved story.
