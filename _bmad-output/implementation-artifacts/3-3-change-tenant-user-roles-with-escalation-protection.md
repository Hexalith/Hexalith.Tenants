---
baseline_commit: f8074110d82cbbbd87462b00c24df56350da8d23
---

# Story 3.3: Change Tenant User Roles with Escalation Protection

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want to change a member's tenant role,
so that access can be adjusted without allowing unauthorized privilege escalation.

## Acceptance Criteria

1. Given a tenant exists, is enabled, and contains the target user, when a TenantOwner changes the user's role to `TenantReader`, `TenantContributor`, or `TenantOwner`, then the tenant aggregate records one `UserRoleChanged` event and applying that event updates `TenantState.Users[targetUserId]`.
2. Given the target user is not a tenant member, when an authorized actor handles `ChangeUserRole`, then the aggregate returns `UserNotInTenantRejection` and no `UserRoleChanged` event is produced.
3. Given an actor attempts to assign `TenantRole.Unknown`, an undefined enum value, or a payload role such as `GlobalAdministrator` through `ChangeUserRole`, when validation or aggregate handling runs, then the operation is rejected as role escalation or payload validation failure and no global-administrator state is modified.
4. Given a non-owner tenant member or non-member actor attempts to change another user's role, when the aggregate evaluates current tenant membership, then the command is rejected with `InsufficientPermissionsRejection` and no role-changed event is produced.
5. Given a trusted global administrator command envelope extension is present, when `ChangeUserRole` is valid, then the aggregate bypasses per-tenant owner RBAC and records `UserRoleChanged`; the bypass must come only from trusted envelope metadata.
6. Given role-change tests run, when all allowed role transitions, same-role no-op, disabled/missing tenant, target-not-member, insufficient-permission, and escalation paths are exercised, then tests verify event production, rejections, branch ordering, and final state without live infrastructure.
7. Given the command body tenant ID diverges from `CommandEnvelope.AggregateId`, when `ChangeUserRole` emits success or rejection payloads, then tenant-scoped payloads use the envelope aggregate ID as the EventStore source of truth.

## Tasks / Subtasks

- [x] Verify existing contracts and documentation before changing code (AC: 1-3)
  - [x] Confirm `ChangeUserRole`, `UserRoleChanged`, `UserNotInTenantRejection`, `RoleEscalationRejection`, and `InsufficientPermissionsRejection` already exist; do not create duplicate contract types.
  - [x] Confirm `TenantRole` contains only `Unknown`, `TenantOwner`, `TenantContributor`, and `TenantReader`; do not add `GlobalAdministrator` as a tenant role.
  - [x] Update `docs/event-contract-reference.md` if needed so `ChangeUserRole` examples show role names, not numeric enum ordinals.

- [x] Harden or preserve `ChangeUserRole` aggregate behavior (AC: 1-5, 7)
  - [x] Use `public static DomainResult Handle(ChangeUserRole command, TenantState? state, CommandEnvelope envelope)` in `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`.
  - [x] Guard `command` and `envelope` with `ArgumentNullException.ThrowIfNull`.
  - [x] Derive `tenantId` from `envelope.AggregateId` and use that value in all `ChangeUserRole` success/rejection payloads.
  - [x] Preserve branch order: missing state -> non-active/disabled tenant -> RBAC -> assignable-role check -> target membership -> same-role no-op -> success.
  - [x] Enforce TenantOwner authority with `IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)`, unless `IsGlobalAdmin(envelope)` is true.
  - [x] Reject `TenantRole.Unknown` and undefined enum values with `RoleEscalationRejection(tenantId, command.UserId, command.NewRole)`.
  - [x] Reject missing target users with `UserNotInTenantRejection(tenantId, command.UserId)` after authorization and role validation pass.
  - [x] Return `DomainResult.NoOp()` when the target already has `command.NewRole`.
  - [x] On success, return exactly one `UserRoleChanged(tenantId, command.UserId, oldRole, command.NewRole)`.
  - [x] Do not add a must-retain-one-owner invariant; owner self-demotion is allowed by the same ownership-transfer design that allows last-owner removal.

- [x] Verify validator and command-pipeline behavior (AC: 3)
  - [x] Preserve `ChangeUserRoleValidator` rules: non-empty `TenantId`, non-empty `UserId`, `NewRole.IsInEnum()`, and `NewRole != TenantRole.Unknown`.
  - [x] Add or preserve validator coverage for `TenantRole.Unknown` in addition to undefined enum values.
  - [x] Add or preserve `TenantSubmitCommandValidator` coverage proving invalid role payloads such as `"GlobalAdministrator"`, missing/unrecognized role values, or undefined numeric values fail before domain success.

- [x] Extend focused aggregate tests (AC: 1-7)
  - [x] Use the existing `CreateCommand<T>(command, actorUserId, isGlobalAdmin, aggregateId)` helper in `TenantAggregateTests.cs`; do not construct `CommandEnvelope` inline.
  - [x] Cover all allowed target transitions: Reader -> Contributor, Reader -> Owner, Contributor -> Reader, Contributor -> Owner, Owner -> Reader, Owner -> Contributor.
  - [x] Cover TenantOwner success and trusted global-admin success.
  - [x] Cover null state, disabled/non-active tenant, target not in tenant, same-role no-op, reader actor, contributor actor, and non-member actor.
  - [x] Assert `InsufficientPermissionsRejection.ActorRole` is `null` for non-member actors and the actual role for insufficient member actors.
  - [x] Assert disabled/non-active state wins over RBAC, target membership, same-role, and role-escalation branches.
  - [x] Assert success and rejection payloads use `envelope.AggregateId` when it differs from `command.TenantId`.
  - [x] Assert `TenantState.Apply(UserRoleChanged)` updates only the target user's role and preserves other members.

- [x] Preserve Testing package and projection parity (AC: 1, 6)
  - [x] Keep `InMemoryTenantService.ProcessCommand(ChangeUserRole, userId, isGlobalAdmin)` and `ProcessTenantCommand<T>` delegating to `TenantAggregate.Handle`; do not reimplement role-change logic in the fake.
  - [x] Update or preserve `TenantConformanceTests` so the real aggregate and in-memory fake produce identical event/rejection sequences and order for role-change success, no-op, insufficient-permission, not-member, disabled, and escalation scenarios.
  - [x] Verify in-memory and server projection tests still apply `UserRoleChanged` consistently to tenant detail and cross-tenant user indexes.

- [x] Run focused validation (AC: 1-7)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~ChangeUserRoleValidatorTests|FullyQualifiedName~TenantSubmitCommandValidatorTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~TenantConformanceTests|FullyQualifiedName~InMemoryTenantServiceTests|FullyQualifiedName~InMemoryTenantProjectionTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution or build evidence as partial validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, roles, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.3 covers role changes only. Do not expand it into configuration management, query endpoints, UI command lifecycle, or global administrator management. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Change Tenant User Roles with Escalation Protection`]
- PRD FR31-FR34 define role boundaries: TenantReader is read-only, TenantContributor cannot manage membership/roles/configuration, TenantOwner can manage membership, roles, and configuration, and roles do not transfer across tenants. [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- PRD FR15 keeps global administrator authority separate from tenant roles: a global admin can perform tenant operations without per-tenant assignment, but `GlobalAdministrator` is not a `TenantRole`. [Source: `_bmad-output/planning-artifacts/prd.md#Global Administrators`]
- UX guidance frames change-role as a high-impact access workflow requiring consequence disclosure later, but this story is backend/domain behavior only. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]

### Current Repository State

- `ChangeUserRole`, `UserRoleChanged`, `RoleEscalationRejection`, `UserNotInTenantRejection`, and `InsufficientPermissionsRejection` already exist in Contracts. Do not add alternate contract names. [Source: `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/RoleEscalationRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/UserNotInTenantRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs`]
- `TenantRole` is serialized by name and reserves ordinal `0` as `Unknown`. Treat `Unknown` as non-assignable and fail closed. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- `TenantAggregate.Handle(ChangeUserRole, TenantState?, CommandEnvelope)` already exists with null-state, disabled-state, owner/global-admin RBAC, role validation, target membership, same-role no-op, and success branches. This story should verify and harden it, not create a parallel implementation. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Current `ChangeUserRole` code uses `command.TenantId` in payloads. Story 3.2 fixed the same risk for `RemoveUserFromTenant`; apply that lesson here by using `envelope.AggregateId` for emitted payload tenant IDs. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState.Apply(UserRoleChanged)` updates `Users[e.UserId] = e.NewRole`; it does not alter membership history or other users. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- `ChangeUserRoleValidator` and `TenantSubmitCommandValidator` already route role-change validation. Add missing tests rather than new wiring unless verification finds a gap. [Source: `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs`; `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs`]
- `InMemoryTenantService` delegates `ChangeUserRole` to `TenantAggregate.Handle` and applies `UserRoleChanged` through `TenantState.Apply`. Preserve this production-equivalent fake pattern. [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- Existing tests cover several role-change paths, including success, null state, disabled state, target-not-member, same-role no-op, undefined role, reader/contributor/non-member RBAC, global-admin success, self-demotion, and `Unknown` aggregate rejection. Review for missing envelope aggregate-id and validator `Unknown`/payload-name coverage. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`; `tests/Hexalith.Tenants.Server.Tests/Validators/ChangeUserRoleValidatorTests.cs`; `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`]

### Previous Story Intelligence

- Story 3.1 established `TenantRole.Unknown = 0`, role-name JSON serialization, trusted `actor:globalAdmin` envelope metadata, disabled-tenant branch precedence, and first-user bootstrap for `AddUserToTenant` only. Preserve all of those decisions. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md#Previous Work Intelligence`]
- Story 3.2 established that tenant-scoped event/rejection payloads should derive tenant identity from `CommandEnvelope.AggregateId` when the command body diverges. Apply this specifically to `ChangeUserRole`. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`]
- Story 3.2 also documented that removing the last owner is allowed. Role-change must not add a must-retain-owner invariant; owner self-demotion remains valid. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Architecture and Technical Guardrails`]
- Prior validation in this workspace hit VSTest socket restrictions, then used direct xUnit execution plus Release build as fallback evidence. Use the same fallback pattern if needed. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Debug Log References`]
- Recent commits show the current baseline completed Story 3.2 after Story 3.1: `f807411 feat(story-3.2): Remove Users from a Tenant`, `a00d490 feat(story-3.1): Add Users to a Tenant with Explicit Roles`. [Source: `git log --oneline -8`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `global.json`; `Directory.Packages.props`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- Use K&R brace style, file-scoped namespaces, one type per file, System.Text.Json, collection expressions, xUnit v3, Shouldly assertions, and no `Assert.*`. [Source: `_bmad-output/project-context.md#Language-Specific Rules (C# / .NET 10)`; `_bmad-output/project-context.md#Testing Rules`]
- Aggregate Handle methods must stay pure: no async, no I/O, no DAPR, no service calls, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- Aggregates are auto-discovered from `Hexalith.Tenants.Server` by signature. Do not move `TenantAggregate` or change `Handle` method names/signatures. [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- Domain RBAC belongs inside aggregate Handle methods for Tenants' own authorization. Do not move role-change authorization into EventStore tenant validators, command controllers, or client-side checks. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Trusted global-admin bypass is based only on sanitized `actor:globalAdmin` envelope metadata. Client-submitted reserved extensions are untrusted. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Identifiers are case-sensitive. Do not normalize tenant IDs or user IDs during role-change membership checks. [Source: `_bmad-output/planning-artifacts/architecture.md#Security & Contract Hardening Decisions (Correct Course 2026-05-27)`]

### Likely Files to Touch

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` - only if verification confirms `ChangeUserRole` still emits command-body tenant IDs or branch behavior needs hardening.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/ChangeUserRoleValidatorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` only if fake behavior lacks role-change coverage.
- `docs/event-contract-reference.md` only if role-change examples still show numeric enum values.

### Out of Scope

- Creating, removing, or renaming command/event/rejection contracts.
- Adding `GlobalAdministrator` to `TenantRole`.
- Adding a must-retain-one-owner invariant.
- Implementing tenant configuration, query endpoints, projections beyond preserving existing behavior, or Phase 2 UI command lifecycle.
- Adding actor, timestamp, correlation, causation, or authorization metadata to `UserRoleChanged`; that belongs to the EventStore envelope.
- Adding REST endpoints outside the existing EventStore command gateway.
- Changing DAPR, Aspire, EventStore, package versions, submodule topology, or CI workflow.

### Latest Technical Context

- No package or API upgrade is required for this story. Use the versions pinned locally: FluentValidation `12.1.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, DAPR `1.17.9`, and .NET SDK `10.0.300`. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- The relevant current technical correction is local, not external: `TenantRole.Unknown = 0`, string enum serialization, trusted envelope metadata, and envelope aggregate-id ownership are authoritative. Do not reuse older combined Story 3 artifacts that predate the current sprint split.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Change Tenant User Roles with Escalation Protection`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Role Behavior`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Global Administrators`]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs`]
- [Source: `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs`]
- [Source: `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`]
- [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]
- [Source: `docs/event-contract-reference.md#ChangeUserRole`]

## Project Structure Notes

- Alignment: Story 3.3 is a Server aggregate/validator/test story using existing Contracts, Testing, projection, and documentation surfaces.
- The existing `_bmad-output/implementation-artifacts/3-3-tenant-configuration-management.md` is a completed artifact from a previous story breakdown and does not match the current `sprint-status.yaml` key for Story 3.3. Do not overwrite it or follow it as current scope.
- Current source appears to already contain most role-change behavior. The dev agent should verify the implementation against this story, close focused test/documentation gaps, and apply envelope aggregate-id consistency.
- Most likely implementation risks are trusting command-body tenant IDs over the EventStore envelope, accidentally treating `Unknown` as assignable, adding a must-retain-owner backend invariant, or expanding into UI/query/configuration scope.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.3, PRD role/global-admin requirements, UX high-impact access guidance, current project context, and previous Story 3.1/3.2 intelligence.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, `ChangeUserRoleValidator`, `TenantSubmitCommandValidator`, `InMemoryTenantService`, aggregate tests, conformance tests, and event-contract docs.
- Previous-story intelligence incorporated `TenantRole.Unknown`, trusted global-admin envelope metadata, disabled-state branch precedence, last-owner/self-demotion behavior, VSTest socket fallback, and envelope aggregate-id consistency from Story 3.2 review.
- Disaster-prevention guardrails included: do not duplicate contracts, do not add `GlobalAdministrator` to `TenantRole`, do not add must-retain-owner rules, do not reimplement fake logic, do not trust client-reserved extensions, and do not expand into Phase 2 UI.
- Latest technical context reviewed from local pins and source. No external package-version change is needed.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Red-phase direct xUnit focused server run failed 6 new envelope aggregate-id assertions for `ChangeUserRole` because payloads still used command-body `TenantId`.
- 2026-06-01: VSTest focused Server, Testing, and Contracts commands compiled assemblies but aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-06-01: Direct xUnit fallback passed for focused Server tests (`TenantAggregateTests`, `ChangeUserRoleValidatorTests`, `TenantSubmitCommandValidatorTests`): 134 total, 0 failed.
- 2026-06-01: Direct xUnit fallback passed for focused Testing tests (`TenantConformanceTests`, `InMemoryTenantServiceTests`, `InMemoryTenantProjectionTests`): 87 total, 0 failed.
- 2026-06-01: Direct xUnit fallback passed for Contracts tests: 74 total, 0 failed.
- 2026-06-01: Release solution build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` with 0 warnings and 0 errors.
- 2026-06-01: Review VSTest focused Server command compiled assemblies but aborted with known `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-06-01: Review direct xUnit fallback passed for focused Server tests (`TenantAggregateTests`, `ChangeUserRoleValidatorTests`, `TenantSubmitCommandValidatorTests`): 134 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for focused Testing tests (`TenantConformanceTests`, `InMemoryTenantServiceTests`, `InMemoryTenantProjectionTests`): 87 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for Contracts tests: 74 total, 0 failed.
- 2026-06-01: Review direct xUnit fallback passed for `CommandApiRuntimeIntegrationTests`: 57 total, 0 failed.
- 2026-06-01: Review Release solution build passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Verified existing role-change contracts and tenant role enum; no duplicate contracts or `GlobalAdministrator` tenant role were added.
- Updated `ChangeUserRole` aggregate handling to derive tenant-scoped success and rejection payload tenant IDs from `CommandEnvelope.AggregateId`.
- Added focused aggregate, validator, and command-pipeline tests for role transitions, branch ordering, invalid role payloads, and envelope aggregate-id source-of-truth behavior.
- Updated the event contract reference so `ChangeUserRole` examples show role names instead of numeric enum ordinals.

### File List

- `_bmad-output/implementation-artifacts/3-3-change-tenant-user-roles-with-escalation-protection.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Validators/ChangeUserRoleValidatorTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`

### Change Log

- 2026-06-01: Implemented Story 3.3 role-change hardening by moving tenant-scoped `ChangeUserRole` payloads to envelope aggregate-id source of truth and adding focused guardrail tests.
- 2026-06-01: Senior review auto-fixed story metadata by adding the changed integration test file to the File List, verified focused direct xUnit suites and Release build, and marked the story done.

## Senior Developer Review (AI)

### Review Summary

- Outcome: Approved after auto-fix.
- Issues found: 0 High, 1 Medium, 0 Low.
- Auto-fixes applied: 1 metadata/documentation issue fixed.
- Action items created: 0.

### Findings

- [MEDIUM] File List omitted a changed source test file: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`. Fixed by adding the file to the story File List.

### Acceptance Criteria Validation

- AC1: Verified `TenantAggregate.Handle(ChangeUserRole, TenantState?, CommandEnvelope)` emits exactly one `UserRoleChanged` for valid assignable role changes and `TenantState.Apply(UserRoleChanged)` updates only the target user.
- AC2: Verified missing target users produce `UserNotInTenantRejection` after authorization and role validation, without a role-changed event.
- AC3: Verified `TenantRole.Unknown`, undefined enum values, missing role payloads, and unrecognized role names such as `GlobalAdministrator` are rejected by validator and/or aggregate behavior.
- AC4: Verified reader, contributor, and non-member actors receive `InsufficientPermissionsRejection`, with actor role captured as expected.
- AC5: Verified trusted global-admin envelope metadata bypasses tenant-owner RBAC and client payload roles do not create global-administrator tenant state.
- AC6: Verified focused tests cover allowed transitions, same-role no-op, disabled/missing tenant, target-not-member, insufficient-permission, escalation, branch ordering, and final state behavior without live infrastructure.
- AC7: Verified success and rejection payloads use `CommandEnvelope.AggregateId` when it differs from `ChangeUserRole.TenantId`.

### Checklist

- Story file loaded and status verified as reviewable before review.
- Epic and story ID resolved as 3.3.
- No separate story context artifact or epic tech spec file was present; warning recorded. Architecture and project context loaded from `_bmad-output/project-context.md` and `_bmad-output/planning-artifacts/architecture.md`; local story and planning references reviewed.
- No external MCP/web doc lookup was needed because this review changed no package/API usage beyond pinned local sources.
- File List cross-checked against git changes; metadata discrepancy fixed.
- Code, tests, validation, security, and branch ordering reviewed against the story ACs.
- Sprint status synced to `done`.
