---
baseline_commit: d6e5fc4bf23be1dda97a5c9d777e5c25e345abca
---

# Story 3.4: Enforce Tenant-Scoped Role Behavior

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant user,
I want my tenant role to grant only the capabilities intended for that tenant,
so that access does not escalate or leak across tenants.

## Acceptance Criteria

1. Given a user is `TenantReader` in a tenant, when the user attempts a tenant state-changing command, then the command is rejected for insufficient tenant authority and no state-changing event is produced.
2. Given a user is `TenantContributor` in a tenant, when the user's tenant role is evaluated, then the user has reader-level visibility and contributor-level domain-command capability for consuming-service semantics and the user cannot manage tenant membership, tenant roles, or tenant configuration.
3. Given a user is `TenantOwner` in a tenant, when the user's tenant role is evaluated for membership or configuration commands, then the user can perform owner-authorized membership and configuration operations and owner authority remains scoped to that tenant only.
4. Given a user has different roles in multiple tenants, when the user acts against tenant A, then only the user's role in tenant A is considered and roles from tenant B do not transfer or aggregate across tenants.
5. Given a trusted global-admin command envelope extension is present, when tenant role authorization is evaluated, then global-admin authority can bypass per-tenant role checks and the bypass is based on trusted envelope metadata, not user-supplied claims or command payload fields.
6. Given role behavior tests run, when reader, contributor, owner, global-admin, missing-member, and cross-tenant cases are exercised, then tests prove tenant isolation and role authorization branch coverage.

## Tasks / Subtasks

- [x] Verify the existing role model and avoid duplicate abstractions (AC: 1-6)
  - [x] Confirm `TenantRole` still contains only `Unknown`, `TenantOwner`, `TenantContributor`, and `TenantReader`; do not add `GlobalAdministrator` as a tenant role.
  - [x] Preserve name-based role comparison through `MeetsMinimumRole`; do not make authorization depend on enum ordinal ordering.
  - [x] Preserve `TenantRole.Unknown` as fail-closed and non-authorized for all owner/contributor-gated operations.

- [x] Harden aggregate authorization boundaries where verification finds gaps (AC: 1-5)
  - [x] Keep domain RBAC inside `TenantAggregate.Handle` methods; do not move Tenants' own authorization into validators, controllers, client checks, or EventStore tenant validators.
  - [x] Reader actors must receive `InsufficientPermissionsRejection` and produce no success events for `UpdateTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, and `RemoveTenantConfiguration`.
  - [x] Contributor actors may execute contributor-level tenant domain commands such as `UpdateTenant`, but must be rejected for membership, role, and configuration management commands.
  - [x] Owner actors may execute membership, role, and configuration commands for the tenant where they are an owner.
  - [x] Non-members must be rejected with `ActorRole = null`; previous membership or membership in another tenant must not grant residual authority.
  - [x] Trusted global-admin bypass must be based only on `CommandEnvelope.Extensions["actor:globalAdmin"] == "true"` and must not depend on command payload fields or client-supplied claims.

- [x] Close tenant-scope source-of-truth gaps (AC: 3-5)
  - [x] Review every tenant-scoped success/rejection payload emitted by `TenantAggregate` and prefer `CommandEnvelope.AggregateId` as the managed tenant ID source of truth.
  - [x] Specifically verify `AddUserToTenant`, `SetTenantConfiguration`, and `RemoveTenantConfiguration`, because current code still emits `command.TenantId` in success and rejection payloads.
  - [x] Add tests where command body `TenantId` differs from `CommandEnvelope.AggregateId`; success and rejection payloads must use the envelope aggregate ID and must not mutate the command-body tenant.
  - [x] Preserve first-user bootstrap semantics for `AddUserToTenant`: the empty-tenant exception applies only before any membership history exists and only within the envelope aggregate tenant.

- [x] Add focused aggregate tests for role behavior (AC: 1-6)
  - [x] Use the existing `CreateCommand<T>(command, actorUserId, isGlobalAdmin, aggregateId)` helper in `TenantAggregateTests.cs`; do not construct `CommandEnvelope` inline.
  - [x] Cover reader rejections for tenant state-changing commands and assert no `TenantUpdated`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, or `TenantConfigurationRemoved` event is produced.
  - [x] Cover contributor success for `UpdateTenant` and contributor rejection for membership, role, and configuration commands.
  - [x] Cover owner success for add, remove, change-role, set-configuration, and remove-configuration.
  - [x] Cover missing-member/non-member actor rejections with `ActorRole` null.
  - [x] Cover cross-tenant role isolation: the same actor can be owner in tenant B and reader/contributor/non-member in tenant A, and only tenant A membership is considered when acting against tenant A.
  - [x] Cover trusted global-admin bypass for owner-gated commands without requiring membership in the target tenant.
  - [x] Cover that a command payload or client extension cannot manufacture global-admin authority; only the trusted envelope extension used by the server-side helper bypasses RBAC.

- [x] Preserve Testing package parity and consumer semantics (AC: 2, 4, 6)
  - [x] Keep `InMemoryTenantService.ProcessTenantCommand<T>` delegating to production `TenantAggregate.Handle`; do not reimplement RBAC in the fake.
  - [x] Add or preserve conformance cases proving the in-memory fake and real aggregate return identical event/rejection sequences for reader, contributor, owner, global-admin, non-member, and cross-tenant scenarios.
  - [x] Add or preserve `InMemoryTenantServiceTests` coverage proving tenant states remain isolated when the same user has different roles in different tenants.
  - [x] If documentation references role behavior for consuming services, align it with FR31-FR34: Reader is read-only, Contributor includes tenant-scoped consuming-service domain commands, Owner adds Tenants membership/role/configuration management.

- [x] Run focused validation (AC: 1-6)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~TenantSubmitCommandValidatorTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution plus Release build evidence as partial validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.4 is a backend/domain authorization hardening story. It must not expand into new query endpoints, Phase 2 UI, global-administrator management, or new role types. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.4: Enforce Tenant-Scoped Role Behavior`]
- PRD FR31-FR34 define the role boundaries: `TenantReader` can query tenant data only, `TenantContributor` has reader capabilities plus tenant-scoped domain-command capability for consuming services, `TenantOwner` adds membership/role/configuration management, and roles never transfer across tenants. [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- FR15 keeps global administrator authority separate from tenant roles: global admins can perform tenant operations across tenants without per-tenant role assignment, but `GlobalAdministrator` is not a `TenantRole`. [Source: `_bmad-output/planning-artifacts/prd.md#Global Administration`]
- NFR6 and NFR10 require role escalation and tenant isolation branch coverage for Stories 3.1-3.4. Treat missing branch coverage as a story gap, not optional cleanup. [Source: `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`]

### Current Repository State

- `TenantRole` already serializes by name and reserves `Unknown = 0` as non-privileged. Keep it fail-closed. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- `TenantAggregate` already owns role checks through `IsAuthorized`, `MeetsMinimumRole`, `GetActorRole`, and `IsGlobalAdmin`. Use these patterns instead of adding another authorization layer. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `UpdateTenant` currently requires `TenantContributor` or higher, while membership, role-change, and configuration handlers require `TenantOwner` unless trusted global-admin metadata is present. This matches PRD role boundaries and should be verified with focused tests. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- `RemoveUserFromTenant` and `ChangeUserRole` already derive tenant-scoped payload IDs from `envelope.AggregateId`. Story 3.3 moved `ChangeUserRole` to this source-of-truth pattern. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `_bmad-output/implementation-artifacts/3-3-change-tenant-user-roles-with-escalation-protection.md#Completion Notes List`]
- `AddUserToTenant`, `SetTenantConfiguration`, and `RemoveTenantConfiguration` still emit `command.TenantId` in success and rejection payloads. Because this story is about scoped role behavior and cross-tenant leakage, verify and likely align these handlers to `envelope.AggregateId`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState.Users` is the membership map used for RBAC. It is per aggregate instance; tests must prove roles from another tenant's state are not consulted. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- Existing `TenantAggregateTests` already cover many RBAC cases: reader/contributor rejections, owner success, global-admin bypass, non-member rejection, removed-owner residual authority, configuration RBAC, `Unknown` fail-closed behavior, and role hierarchy regression. This story should consolidate gaps and add cross-tenant/envelope-source tests rather than duplicate broad happy paths. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- `InMemoryTenantService.ProcessTenantCommand<T>` delegates to `TenantAggregate.Handle` and applies events to the envelope aggregate ID; preserve this production-equivalent fake pattern. The public `ProcessCommand` overloads use command-body tenant IDs because they create their own matching envelopes. [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]

### Previous Story Intelligence

- Story 3.1 established `TenantRole.Unknown = 0`, string enum serialization, the trusted `actor:globalAdmin` envelope extension, disabled-tenant branch precedence, and first-user bootstrap for `AddUserToTenant` only. Preserve these decisions. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- Story 3.2 established that tenant-scoped event/rejection payloads should derive tenant identity from `CommandEnvelope.AggregateId` when the command body diverges. Apply the same rule to any remaining handlers that can leak command-body tenant IDs. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`]
- Story 3.2 also confirmed that removing the last owner is allowed. Do not add a must-retain-one-owner invariant while tightening role behavior. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Architecture and Technical Guardrails`]
- Story 3.3 verified role-change escalation, same-role no-op, disabled/missing tenant behavior, global-admin bypass, and envelope aggregate ID consistency for `ChangeUserRole`. Build on those tests instead of reworking the role-change design. [Source: `_bmad-output/implementation-artifacts/3-3-change-tenant-user-roles-with-escalation-protection.md`]
- Recent commits show the current baseline completed Stories 3.1-3.3 in order: `a00d490 feat(story-3.1): Add Users to a Tenant with Explicit Roles`, `f807411 feat(story-3.2): Remove Users from a Tenant`, `d6e5fc4 feat(story-3.3): Change Tenant User Roles with Escalation Protection`. [Source: `git log --oneline -5`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, central package management, DAPR `1.17.9`, FluentValidation `12.1.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. Do not bump SDK or packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Aggregate `Handle` methods must remain pure static functions: no async, no I/O, no DAPR calls, no service dependencies, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are discovered from `Hexalith.Tenants.Server` by `Handle` signature. Do not move `TenantAggregate`, rename `Handle`, or alter public handler signatures except to preserve existing dispatch-compatible forms. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC for Tenants' own commands belongs inside aggregate handlers. EventStore `IRbacValidator`/`ITenantValidator` interfaces are for consuming services and API-gate validation; moving Tenants domain RBAC there creates a circular dependency. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- The trusted global-admin bypass is indicated by `actor:globalAdmin` on `CommandEnvelope.Extensions`; client-submitted reserved extensions are untrusted. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Identifier casing is significant. Do not normalize tenant IDs or user IDs when checking membership or cross-tenant isolation. [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`]
- Tests use xUnit v3 plus Shouldly, global `using Xunit`, K&R brace style, and snake_case test method names. Do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` - likely if envelope aggregate ID source-of-truth gaps remain in `AddUserToTenant`, `SetTenantConfiguration`, or `RemoveTenantConfiguration`.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` - primary focused role behavior and cross-tenant branch coverage.
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs` - parity checks between real aggregate and in-memory fake.
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` - cross-tenant state isolation and public fake behavior checks.
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs` - only if verification finds public overloads or state application violating aggregate source-of-truth behavior; prefer leaving it as a delegating fake.
- `docs/event-contract-reference.md` - only if role behavior or tenant-source-of-truth documentation is stale.

### Out of Scope

- Adding new tenant roles or adding `GlobalAdministrator` to `TenantRole`.
- Creating a public role-evaluation API unless an existing acceptance gap proves it is needed.
- Adding per-command REST endpoints or moving command authorization to controllers.
- Implementing query endpoints, read-model row filtering, Phase 2 UI, or FrontComposer surfaces.
- Adding a must-retain-one-owner invariant.
- Changing DAPR, Aspire, EventStore, package versions, submodule topology, or CI workflow.

### Latest Technical Context

- No external package or API upgrade is required. Use locally pinned versions and existing EventStore/DAPR/Aspire APIs. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- The relevant current technical correction is local: envelope aggregate ID is the canonical tenant scope for aggregate command handling, while command body tenant IDs must not retarget authorization or emitted payloads.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.4: Enforce Tenant-Scoped Role Behavior`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Global Administration`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md`]
- [Source: `_bmad-output/implementation-artifacts/3-3-change-tenant-user-roles-with-escalation-protection.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`]

## Project Structure Notes

- Alignment: Story 3.4 stays in the Server aggregate/domain test area with Testing package parity checks. It should not add new projects or package dependencies.
- Existing role behavior tests are spread across older numbered comments and configuration sections in `TenantAggregateTests.cs`. Keep additions localized and clearly named rather than reshaping the entire test file.
- The current source appears to implement most role hierarchy behavior already. The highest-risk gap is tenant scope consistency when command-body `TenantId` diverges from `CommandEnvelope.AggregateId`.
- The dirty worktree currently contains an unrelated modification to `_bmad-output/story-automator/orchestration-1-20260531-113112.md`. Do not revert or rewrite it as part of this story.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.4, PRD FR31-FR34, NFR6/NFR10, architecture authorization guidance, project context, and previous Stories 3.1-3.3.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `TenantRole`, `InMemoryTenantService`, aggregate tests, conformance tests, fake tests, and event-contract docs.
- Previous-story intelligence incorporated `TenantRole.Unknown`, trusted global-admin envelope metadata, disabled-state branch precedence, last-owner/self-demotion behavior, VSTest socket fallback, and envelope aggregate ID consistency.
- Disaster-prevention guardrails included: do not duplicate role abstractions, do not add `GlobalAdministrator` as a tenant role, do not add must-retain-owner rules, do not reimplement fake authorization logic, do not trust client-provided authority flags, and do not expand into UI/query scope.
- Latest technical context reviewed from local pins and source. No external package-version change or web research is needed for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Verified `TenantRole` remains limited to `Unknown`, `TenantOwner`, `TenantContributor`, and `TenantReader`; no global-admin tenant role was added.
- 2026-06-01: Red test evidence: new envelope/source-of-truth aggregate tests failed because `AddUserToTenant`, `SetTenantConfiguration`, and `RemoveTenantConfiguration` emitted command-body `TenantId`.
- 2026-06-01: Requested VSTest commands aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` from `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-06-01: Direct xUnit fallback passed for Server targeted classes: 136 total, 0 failed.
- 2026-06-01: Direct xUnit fallback passed for Testing targeted classes: 71 total, 0 failed.
- 2026-06-01: Direct xUnit fallback passed for Contracts tests: 74 total, 0 failed.
- 2026-06-01: Release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`.
- 2026-06-01: Review found and fixed exact global-admin extension value enforcement; focused Server aggregate direct xUnit run passed: 126 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for Testing conformance/fake tests: 71 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for Integration `CommandApiRuntimeIntegrationTests`: 66 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for Contracts tests: 74 total, 0 failed.
- 2026-06-01: Review Release build passed with warnings as errors: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Hardened `AddUserToTenant`, `SetTenantConfiguration`, and `RemoveTenantConfiguration` to emit success/rejection payload tenant IDs from `CommandEnvelope.AggregateId`.
- Preserved aggregate-local RBAC, `TenantRole.Unknown` fail-closed behavior, first-user bootstrap semantics, and the trusted envelope-extension global-admin bypass.
- Added aggregate tests for divergent command-body/envelope tenant IDs across add-user and configuration success/rejection branches.
- Added Testing-package conformance and fake-service coverage for same-user cross-tenant role isolation.
- Senior review tightened trusted global-admin bypass evaluation to require the exact envelope value `actor:globalAdmin=true`.
- Validated with direct xUnit fallback because VSTest socket startup is blocked in this sandbox; Release build also passed with warnings as errors.

### File List

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-01: Implemented tenant-scoped role behavior hardening and test coverage for story 3.4; moved story to review.
- 2026-06-01: Senior review auto-fixed global-admin extension exact-value enforcement, documented changed integration tests, and moved story to done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

### Findings Fixed

- HIGH: `TenantAggregate.IsGlobalAdmin` accepted non-exact extension values such as `TRUE`, while AC5 and the story task require the trusted envelope bypass to use `CommandEnvelope.Extensions["actor:globalAdmin"] == "true"` exactly. Fixed by switching value comparison to ordinal equality and adding aggregate regression coverage.
- MEDIUM: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` had story-related changes but was missing from the Dev Agent File List. Fixed by adding it to the File List.

### Review Notes

- AC1-AC6 were cross-checked against `TenantAggregate`, aggregate tests, integration tests, conformance tests, and fake-service tests.
- VSTest remains blocked in this sandbox by `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit validation was used as the local fallback.
- External/MCP documentation lookup was not needed for this local aggregate review; project context stated no external package or API change was required.
