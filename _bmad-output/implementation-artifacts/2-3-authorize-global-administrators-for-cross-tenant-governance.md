---
baseline_commit: bddfda5
---

# Story 2.3: Authorize Global Administrators for Cross-Tenant Governance

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want my platform authority to apply across tenant operations,
so that I can govern tenants without being assigned a role in every tenant.

## Acceptance Criteria

1. Given EventStore claims transformation marks a command envelope with the trusted global-admin extension, when a tenant governance command is handled, then the aggregate treats the actor as globally authorized, and the aggregate does not depend on user-supplied command body fields for global administrator authority.
2. Given a command envelope does not contain trusted global-admin authority, when the actor attempts a global-administrator-only tenant operation, then the command is rejected with a structured authorization rejection, and no tenant lifecycle event is produced.
3. Given a global administrator acts on any managed tenant aggregate, when the command envelope references the platform tenant `system`, domain `tenants`, and the managed aggregate ID, then the command uses the aggregate ID from the envelope, and the command body cannot override the target aggregate identity.
4. Given authorization tests run, when global and non-global actors execute create, update, disable, and enable tenant commands, then tests prove global administrators can perform cross-tenant operations, and non-global actors cannot bypass the aggregate authorization checks.
5. Given audit or telemetry metadata is emitted for global administrator commands, when logs and traces are inspected, then they include support-safe correlation and command-stage metadata, and they do not include command payloads, tokens, or sensitive user data.

## Tasks / Subtasks

- [x] Harden tenant lifecycle command authorization in `TenantAggregate` (AC: 1, 2, 3, 4)
  - [x] Reuse the existing `actor:globalAdmin` envelope extension check in `TenantAggregate.IsGlobalAdmin(CommandEnvelope)`; do not introduce a second authority flag, command body property, service call, or projection lookup inside aggregate handlers.
  - [x] Require trusted global-admin authority for `CreateTenant`, `DisableTenant`, and `EnableTenant`; non-global actors must receive a structured authorization rejection and no lifecycle event.
  - [x] Keep `UpdateTenant` compatible with existing RBAC: global admins bypass tenant membership, tenant contributors/owners can update, readers/non-members are rejected.
  - [x] Convert `DisableTenant` and `EnableTenant` to envelope-aware handlers so they can enforce global-admin authority and canonical aggregate identity.
  - [x] Use `envelope.AggregateId` as the canonical managed tenant ID for `CreateTenant`, `UpdateTenant`, `DisableTenant`, and `EnableTenant` events/rejections. The command body's `TenantId` must not be able to target a different aggregate.
  - [x] Preserve existing business-rule ordering after authorization: missing tenant -> `TenantNotFoundRejection`; disabled tenant checks where applicable; already-active/already-disabled behavior remains as currently implemented unless Story 2.5 changes duplicate lifecycle semantics later.

- [x] Preserve EventStore trusted-extension boundaries (AC: 1, 2, 5)
  - [x] Treat `SubmitCommand.IsGlobalAdmin` -> `SubmitCommandExtensions.ToCommandEnvelope()` as the trusted source that writes `actor:globalAdmin=true`.
  - [x] Do not trust client-supplied request extensions. Existing EventStore code strips inbound `actor:globalAdmin` in `CommandsController.BuildTrustedExtensions()` and again in `SubmitCommandExtensions`; keep that behavior intact.
  - [x] Do not edit the EventStore submodule unless a focused failing test proves the current trusted-extension path is broken.
  - [x] Keep logs/metrics to command type, tenant/domain/aggregate, correlation/message identifiers, status, and stage metadata. Do not log command payloads, event payloads, JWTs, configured bootstrap user IDs, or arbitrary user data.

- [x] Update in-memory testing parity and helpers (AC: 1, 2, 3, 4)
  - [x] Update `InMemoryTenantService.ProcessCommand(CreateTenant)`, `ProcessCommand(DisableTenant)`, `ProcessCommand(EnableTenant)`, and `ProcessTenantCommand<T>()` to call the same envelope-aware production handlers as `TenantAggregate`.
  - [x] Ensure testing helpers can create tenant command envelopes with `TenantIdentity.DefaultTenantId`, `TenantIdentity.Domain`, and the managed tenant aggregate ID.
  - [x] Ensure in-memory service tests prove global-admin commands work without tenant membership and non-global actors cannot create, disable, or enable tenants.

- [x] Extend focused aggregate and pipeline tests (AC: 1, 2, 3, 4)
  - [x] Add `TenantAggregateTests` coverage for global-admin success and non-global rejection for `CreateTenant`, `DisableTenant`, and `EnableTenant`.
  - [x] Add mismatch tests where `command.TenantId` differs from `envelope.AggregateId`; assert emitted events/rejections use the envelope aggregate ID and never the command body target.
  - [x] Keep existing `UpdateTenant` contributor/owner/global-admin success and reader/non-member rejection coverage, and add a mismatch assertion for canonical aggregate identity if missing.
  - [x] Add ProcessAsync or command-pipeline tests that use realistic `CommandEnvelope` values: tenant `system`, domain `tenants`, aggregate ID equal to the managed tenant, actor `sub`, and optional `actor:globalAdmin=true`.
  - [x] If tests assert extension sanitization, cover `SubmitCommandExtensions.ToCommandEnvelope()` stripping client-provided `actor:globalAdmin` when `IsGlobalAdmin` is false and adding it only when `IsGlobalAdmin` is true.

- [x] Verify contract, naming, telemetry, and no-regression boundaries (AC: 4, 5)
  - [x] Do not add new command contracts for this story. Reuse `CreateTenant`, `UpdateTenant`, `DisableTenant`, and `EnableTenant`.
  - [x] Prefer reusing `InsufficientPermissionsRejection(TenantId, ActorUserId, ActorRole, CommandName)` for structured authorization failure unless a failing requirement proves a new global-admin-specific rejection is necessary.
  - [x] Confirm `TenantMetrics` already allow-lists `CreateTenant`, `UpdateTenant`, `DisableTenant`, and `EnableTenant`; update only if a focused telemetry test fails.
  - [x] Do not add per-command REST controllers. Commands continue through EventStore's `POST /api/v1/commands` path and Tenants' `/process` domain processor route.
  - [x] Do not change Story 2.4 metadata requirements such as `TenantUpdated.UpdatedAt`, and do not implement Story 2.5 duplicate lifecycle rejection semantics unless required to make authorization tests pass.

- [x] Run focused validation and record environmental blockers honestly (AC: 1-5)
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests|CommandPipelineIntegrationTests|TenantMetricsTests" -m:1 -nr:false`.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests" -m:1 -nr:false`.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false` if any contract shape changes were made.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`.
  - [x] If local VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostics and use build results as partial evidence only.

## Dev Notes

### Source Context

- Epic 2 objective: global administrators can bootstrap the first admin, manage global administrators, create/update tenants, disable/enable tenants, and receive structured rejection outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Global Administrators Can Bootstrap and Govern Tenants`]
- Story 2.3 requires tenant governance commands to trust only EventStore-populated global-admin envelope metadata, reject non-global actors for global-admin-only operations, and use envelope aggregate identity as the command target. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Authorize Global Administrators for Cross-Tenant Governance`]
- PRD FR15 requires global administrators to perform any tenant operation across all tenants without per-tenant role assignment; FR16 requires auditable global-administrator actions. [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- Architecture defines layered authorization: API gate, domain RBAC, trusted global-admin override, and query filtering. The global-admin override is the `actor:globalAdmin` command-envelope extension, not raw user claims. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- UX guidance is backend-relevant here only as a safety constraint: authorization-sensitive and global-administrator actions need clear command outcomes and must not leak raw payloads, tokens, stack traces, internal correlation IDs, or sensitive user data. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Interaction Design Patterns`]

### Current Repository State

- Actual source repository root is `Hexalith.Tenants/`; this story file lives in the parent `_bmad-output/implementation-artifacts/`.
- `TenantAggregate` already has `private const string GlobalAdminExtensionKey = "actor:globalAdmin"` and `private static bool IsGlobalAdmin(CommandEnvelope envelope)`. Extend this path; do not duplicate it. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Current `CreateTenant` handler accepts `CommandEnvelope` and uses `envelope.AggregateId` for `TenantCreated`, but it does not require global-admin authority when creating a new tenant. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Current `UpdateTenant` handler accepts `CommandEnvelope` and uses `IsGlobalAdmin(envelope)` for RBAC bypass, but it still uses `command.TenantId` in not-found, disabled, authorization rejection, and `TenantUpdated` output. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Current `DisableTenant` and `EnableTenant` handlers do not accept `CommandEnvelope`, so they cannot enforce global-admin authority or envelope aggregate identity. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState` already has replay/apply handlers for lifecycle, membership, configuration, and structured rejection events; no new state model is expected for this story. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- EventStore's trusted metadata path already exists: `CommandsController` sanitizes request extensions, constructs `SubmitCommand(..., IsGlobalAdmin: IsGlobalAdministrator(User))`, and `SubmitCommandExtensions.ToCommandEnvelope()` removes any existing `actor:globalAdmin` key before adding it only when `IsGlobalAdmin` is true. [Source: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`; `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/SubmitCommandExtensions.cs`]
- `GlobalAdministratorHelper` recognizes global-admin claims from `global_admin`, `is_global_admin`, `role`, `ClaimTypes.Role`, and `roles`; Tenants aggregate code must not parse claims directly. [Source: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/GlobalAdministratorHelper.cs`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Use `System.Text.Json` only. Do not introduce Newtonsoft.Json or another serializer.
- Use K&R brace style, file-scoped namespaces, one type per file, and source folder structure matching namespaces.
- Aggregate `Handle` methods must remain pure static methods: no I/O, no DAPR, no async, no logging, and no captured state.
- Business authorization failures are domain rejections, not exceptions. Use collection expressions for event/rejection lists.
- Platform tenant ID is `TenantIdentity.DefaultTenantId` (`system`), tenant domain is `TenantIdentity.Domain` (`tenants`), and managed tenant aggregate ID is the tenant being governed.
- Use `envelope.UserId` for actor identity. Never use `name`, `email`, command body fields, or user-submitted extensions for authority.
- Commands/events/rejections are immutable records in `Hexalith.Tenants.Contracts`. Events implement `IEventPayload`; rejections implement `IRejectionEvent`.

### Previous Story Intelligence

- Story 2.2 completed global-admin assignment hardening and added structured duplicate/missing assignment rejections, audit fields, testing fake parity, and command allow-list cleanup. Build passed; local VSTest execution was environment-blocked by socket permission errors.
- Story 2.2 established that assignment authority is checked against `GlobalAdministratorsState`, while Story 2.3 uses EventStore's trusted `actor:globalAdmin` envelope extension for tenant-governance commands. Do not merge these mechanisms in aggregate code.
- Story 2.1 registered the `system|global-administrators|v1` domain service route and preserved the Tenants `/process` route. Keep command routing through the existing EventStore pipeline.
- Recent source commits:
  - `bddfda5 feat(story-2.2): Manage Global Administrator Assignments`
  - `e0c0a54 feat(story-2.1): Bootstrap the Initial Global Administrator`
  - `d733de4 docs(retro): sync epic 1 foundation guidance`
  - `344ffa5 feat(story-1.4): Verify Consumer Package Reference Experience`
  - `6ce94b8 feat(story-1.3): Add CI Quality Gates for Build, Test, Coverage, and Package Validation`

### Likely Files to Touch

- `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/CommandPipeline/CommandPipelineIntegrationTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- Optional only if a focused failing test proves the gap: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/SubmitCommandExtensions.cs` and its tests.

### Out of Scope

- Public admin UI, Phase 2 FrontComposer work, and UX implementation.
- New global-admin assignment behavior from Story 2.2.
- Tenant metadata/event contract changes planned by Story 2.4, including `TenantUpdated.UpdatedAt`.
- Disable/enable duplicate lifecycle rejection semantics planned by Story 2.5.
- Query-side global-admin filtering, projection fan-in durability, pagination, or audit query endpoints from Epic 5.
- Any SDK/package upgrade, EventStore submodule replacement, DAPR component redesign, or new per-command REST API.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Authorize Global Administrators for Cross-Tenant Governance`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`]
- [Source: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/SubmitCommandExtensions.cs`]
- [Source: `Hexalith.Tenants/Hexalith.EventStore/src/Hexalith.EventStore/Authorization/GlobalAdministratorHelper.cs`]

## Project Structure Notes

- Alignment: implementation belongs in existing backend and testing projects only:
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing`
  - matching tests under `Hexalith.Tenants/tests/Hexalith.Tenants.*.Tests`
- No structural conflict detected. The main variance is historical: existing tests label some lifecycle tests as "Story 2.3" even though current Epic 2.3 is specifically authorization hardening. Prefer adding focused tests over renaming unrelated historical comments.
- Avoid broad edits in `Hexalith.EventStore/`; it is a submodule and already contains the trusted extension sanitization/conversion path this story depends on.

## Validation Checklist Results

- Story foundation extracted from Epic 2 and Story 2.3 acceptance criteria.
- PRD and architecture context incorporated: FR15 cross-tenant global-admin authority, FR16 auditable global-admin actions, layered authorization, trusted `actor:globalAdmin` envelope metadata, and envelope aggregate identity.
- Current source inspected for likely UPDATE files: `TenantAggregate`, `TenantState`, tenant command contracts/events/rejections, `TenantIdentity`, `InMemoryTenantService`, `TenantTestHelpers`, EventStore `CommandsController`, EventStore `SubmitCommandExtensions`, and existing aggregate/pipeline tests.
- Previous Story 2.2 learnings incorporated, including global-admin assignment boundary, testing fake parity, support-safe telemetry/logging, recent changed files, and VSTest socket blocker handling.
- Disaster-prevention gaps called out explicitly: do not trust command body identity, do not parse claims inside aggregates, do not duplicate global-admin authority flags, do not edit EventStore submodule unless necessary, and do not pull Story 2.4/2.5 lifecycle semantics into this authorization story.
- Latest technical context checked against local pinned project rules and source. No new external dependency or package-version research is required because this story uses existing .NET/EventStore APIs and the already-pinned code path.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests|CommandPipelineIntegrationTests|TenantMetricsTests" -m:1 -nr:false` built successfully, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` at `System.Net.Sockets.Socket..ctor(...)`, `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start(String endPoint)`, and `Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.ProxyExecutionManager.InitializeTestRun(...)`.
- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests" -m:1 -nr:false` built successfully, then VSTest aborted before executing tests with the same `System.Net.Sockets.SocketException (13): Permission denied` stack.
- 2026-05-31: Contracts validation command was not run because this story made no command, event, rejection, naming, or serialization contract shape changes.

### Completion Notes List

- Hardened `TenantAggregate` so `CreateTenant`, `DisableTenant`, and `EnableTenant` require trusted `actor:globalAdmin=true` envelope metadata and return `InsufficientPermissionsRejection` for non-global actors without lifecycle events.
- Converted disable/enable handlers to envelope-aware signatures and made create/update/disable/enable lifecycle events and rejections use `envelope.AggregateId` as the canonical managed tenant ID.
- Preserved `UpdateTenant` RBAC compatibility: global administrators bypass membership, contributors/owners can update, and readers/non-members remain rejected.
- Kept EventStore trusted-extension code untouched; no focused failure showed a gap in the existing sanitization and `SubmitCommand.IsGlobalAdmin` conversion path.
- Updated in-memory testing parity so lifecycle commands delegate to the same envelope-aware production handlers and added focused aggregate, pipeline, fake-service, and conformance coverage.

### File List

- `_bmad-output/implementation-artifacts/2-3-authorize-global-administrators-for-cross-tenant-governance.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/CommandPipeline/CommandPipelineIntegrationTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`

### Change Log

- 2026-05-31: Implemented Story 2.3 global-admin tenant governance authorization and envelope aggregate identity hardening.
- 2026-05-31: Added focused aggregate, command-pipeline, in-memory fake, and conformance tests for global-admin success, non-global rejection, and body/envelope tenant ID mismatch behavior.
- 2026-05-31: Recorded local VSTest socket blocker; Release build is clean and test projects build before VSTest aborts.
