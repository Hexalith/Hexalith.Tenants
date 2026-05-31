---
baseline_commit: e0c0a54
---

# Story 2.2: Manage Global Administrator Assignments

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want to add and remove global administrator assignments,
so that platform governance can be delegated and recovered without per-tenant role setup.

## Acceptance Criteria

1. Given an authenticated existing global administrator submits a command to add another user as global administrator, when the command is handled, then the global administrators aggregate records a global administrator added event, and the event contains structured identifiers and timestamp data required for audit.
2. Given an authenticated existing global administrator submits a command to remove another user's global administrator status, when the command is valid, then the aggregate records a global administrator removed event, and the removed user no longer has global administrator authority in subsequent command evaluation.
3. Given a global administrator attempts to remove themselves as the last remaining global administrator, when the command is handled, then the aggregate returns a specific last-global-administrator rejection, and the existing global administrator set remains unchanged.
4. Given a duplicate global administrator add operation is submitted, when the target user is already a global administrator, then the aggregate returns a structured duplicate-global-administrator rejection, and no additional global administrator added event is produced.
5. Given a duplicate global administrator remove operation is submitted, when the target user is not a global administrator, then the aggregate returns a structured global-administrator-not-found rejection, and no global administrator removed event is produced.
6. Given duplicate global administrator assignment tests run, when duplicate add and missing remove cases are exercised, then tests verify the exact rejection types, unchanged aggregate state, and absence of duplicate events.
7. Given global administrator assignment events are serialized, when contract round-trip tests run, then every global administrator event and rejection can be serialized and deserialized with `System.Text.Json`, and deep equality is preserved.

## Tasks / Subtasks

- [x] Harden global-admin assignment command handling (AC: 1, 2, 3, 4, 5)
  - [x] Reuse `SetGlobalAdministrator`, `RemoveGlobalAdministrator`, `GlobalAdministratorSet`, `GlobalAdministratorRemoved`, `LastGlobalAdministratorRejection`, `GlobalAdministratorsAggregate`, and `GlobalAdministratorsState`; do not create duplicate command names unless product explicitly renames `SetGlobalAdministrator` to `AddGlobalAdministrator`.
  - [x] Change `SetGlobalAdministrator` handling from duplicate `NoOp` to a structured duplicate rejection, e.g. `GlobalAdministratorAlreadyExistsRejection(TenantId, UserId)`, with no success event emitted.
  - [x] Change `RemoveGlobalAdministrator` handling for missing users from `NoOp` to a structured not-found rejection, e.g. `GlobalAdministratorNotFoundRejection(TenantId, UserId)`, with no success event emitted.
  - [x] Preserve last-admin protection with `LastGlobalAdministratorRejection(TenantId, UserId)` and leave `GlobalAdministratorsState.Administrators` unchanged.
  - [x] Do not let `SetGlobalAdministrator` create the initial administrator from null or unbootstrapped state; Story 2.1 owns first-admin bootstrap. Return a structured rejection rather than silently bootstrapping through assignment.

- [x] Add audit-complete assignment event payloads (AC: 1, 2, 7)
  - [x] Add `DateTimeOffset` timestamp fields using `{Action}At` naming, such as `SetAt` on `GlobalAdministratorSet` and `RemovedAt` on `GlobalAdministratorRemoved`.
  - [x] Include structured actor and target identifiers needed for audit. Use `envelope.UserId` from JWT `sub` for the actor, not `name`, `email`, or any user-controllable claim.
  - [x] Keep `TenantId` top-level on every success and rejection event. For global-admin events this is `TenantIdentity.DefaultTenantId` (`system`).
  - [x] If changing global-admin event constructors, update every projection, fake, test helper, serialization test factory, and sample event construction site in the same story.
  - [x] Do not persist English prose in rejection events; rejection payloads remain structured IDs/enums/counts only.

- [x] Route assignment decisions through the authenticated command envelope (AC: 1, 2, 3)
  - [x] Prefer `public static DomainResult Handle(SetGlobalAdministrator command, GlobalAdministratorsState? state, CommandEnvelope envelope)` and equivalent for remove so the aggregate can use trusted `envelope.UserId` for actor/audit data.
  - [x] Add `ArgumentNullException.ThrowIfNull(envelope)` to 3-argument handlers.
  - [x] Require the actor to already be present in `state.Administrators` before managing assignments. Bootstrap remains the only path for the first administrator.
  - [x] Do not trust `actor:globalAdmin` or any client-submitted extension for assignment authority; this story manages the global-admin set itself. Story 2.3 covers using trusted global-admin authority for tenant governance commands.

- [x] Update global-admin state, projections, and testing fakes (AC: 2, 4, 5, 6)
  - [x] Keep `GlobalAdministratorsState.Apply(GlobalAdministratorSet)` adding the target user and `Apply(GlobalAdministratorRemoved)` removing the target user.
  - [x] Add replay handlers for new duplicate/not-found rejection events that preserve existing state.
  - [x] Update `GlobalAdministratorReadModel`, `GlobalAdministratorProjectionHandler`, and projection tests so removed users no longer appear in `Administrators`.
  - [x] Update `InMemoryTenantService` and `InMemoryTenantProjection` to match production aggregate/projection behavior, including rejection semantics.
  - [x] Ensure `TenantsProjectionActor.IsGlobalAdminAsync`-dependent behavior still reads from `projection:global-administrators:singleton`; do not route global-admin events through tenant-domain projection keys.

- [x] Extend contract and aggregate tests (AC: 1-7)
  - [x] Update `GlobalAdministratorsAggregateTests` for successful add, successful remove, duplicate add rejection, missing remove rejection, last-admin rejection, null/unbootstrapped assignment rejection, and state unchanged after every rejection.
  - [x] Update tests so assignment commands are exercised through `ProcessAsync` with a `CommandEnvelope`, not only by direct static handler calls.
  - [x] Add or update replay tests for persisted `GlobalAdministratorAlreadyExistsRejection`, `GlobalAdministratorNotFoundRejection`, and `LastGlobalAdministratorRejection`.
  - [x] Update `EventSerializationTests.GetTestValue` for new constructor parameters such as `ActorUserId`, `SetAt`, and `RemovedAt`.
  - [x] Confirm `NamingConventionTests` passes for all new rejection names and every event still exposes `TenantId`.
  - [x] Update testing fake conformance coverage for global-admin assignment commands if existing conformance tests cover these commands.

- [x] Verify command API, telemetry, and safe observability boundaries (AC: 1, 2, 7)
  - [x] Ensure the assignment commands continue to use the EventStore command gateway (`POST /api/commands` or current EventStore command route); do not add per-command REST controllers.
  - [x] Validate command identity uses platform tenant `system`, domain `global-administrators`, and aggregate ID `global-administrators` via `TenantIdentity.ForGlobalAdministrators()`.
  - [x] Update `TenantMetrics` known command names if needed: current code lists `AddGlobalAdministrator`/`RegisterGlobalAdministrator`, while actual command contracts are `SetGlobalAdministrator` and `RemoveGlobalAdministrator`.
  - [x] Logs and traces may include support-safe correlation, tenant/domain/aggregate, command type, and stage metadata; do not log command payloads, event payloads, JWTs, secrets, or configured user IDs.

- [x] Run focused validation and record environment blockers honestly (AC: 1-7)
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "GlobalAdministratorsAggregateTests|GlobalAdministratorProjectionHandlerTests|GlobalAdministratorProjectionTests|GlobalAdministratorReadModelTests" -m:1 -nr:false`.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false`.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|InMemoryTenantProjectionTests|TenantConformanceTests" -m:1 -nr:false` if testing fakes are touched.
  - [x] Run `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`.
  - [x] If local VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostics and use build results as partial evidence only.

## Dev Notes

### Source Context

- Epic 2 objective: global administrators can bootstrap the first admin, manage global administrators, create/update tenants, disable/enable tenants, and receive structured rejection outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Global Administrators Can Bootstrap and Govern Tenants`]
- Story 2.2 specifically requires add/remove assignment commands, last-admin protection, duplicate-add and missing-remove structured rejections, state preservation on rejections, and JSON round-trip coverage. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Manage Global Administrator Assignments`]
- PRD requires an existing global administrator to designate or remove global administrator status, forbids removing self if they are the last global administrator, and requires global administrator actions to produce auditable domain events. [Source: `_bmad-output/planning-artifacts/prd.md#Global Administration`]
- Architecture maps Epic 2 global-admin work to `Contracts/Commands`, `Contracts/Events`, `Server/Aggregates`, and host bootstrap/runtime paths. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- Actual source repository root is `Hexalith.Tenants/`; this story file lives in the parent `_bmad-output/implementation-artifacts/`.
- Existing implementation is present and must be modified in place, not duplicated:
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorSet.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/Rejections/LastGlobalAdministratorRejection.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
- Current `GlobalAdministratorsAggregate.Handle(SetGlobalAdministrator, state)` returns `DomainResult.NoOp()` when the target is already an administrator, but Story 2.2 requires a structured duplicate rejection.
- Current `GlobalAdministratorsAggregate.Handle(RemoveGlobalAdministrator, state)` returns `DomainResult.NoOp()` when state is null or the target is absent, but Story 2.2 requires a structured not-found rejection for duplicate remove/missing target cases.
- Current `SetGlobalAdministrator` with null state succeeds, which would bypass the Story 2.1 bootstrap path. This story should close that path.
- Current `GlobalAdministratorSet` and `GlobalAdministratorRemoved` only contain `TenantId` and `UserId`; they do not currently carry timestamp or actor fields required by this story's audit wording.
- Current `TenantMetrics` known command set includes `AddGlobalAdministrator` and `RegisterGlobalAdministrator`, but the actual command contracts are `SetGlobalAdministrator` and `RemoveGlobalAdministrator`.

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Use System.Text.Json only. Do not introduce Newtonsoft.Json.
- Use K&R brace style, file-scoped namespaces, one type per file, and source folder structure matching namespaces.
- Commands/events/rejections are immutable records in `Hexalith.Tenants.Contracts`.
- Success events implement `IEventPayload`; rejection events implement `IRejectionEvent`.
- Business rule failures return `DomainResult.Rejection([new XxxRejection(...)])`; do not throw for duplicate or missing assignment outcomes.
- Aggregate `Handle` methods must remain pure static methods. No I/O, no logging, no DAPR, no async, no captured state.
- Use collection expressions for event/rejection lists.
- `DateTimeOffset` is the project timestamp type, and event timestamp fields use `{Action}At` naming. [Source: `_bmad-output/project-context.md#Serialization`]
- `DateTimeOffset.UtcNow` is the project-approved source of "now" inside Handle methods. [Source: `_bmad-output/project-context.md#Serialization`]

### Identity and Routing Rules

- Platform tenant ID is `system`.
- Tenant domain is `tenants`.
- Global administrator domain is `global-administrators`.
- Global administrator aggregate ID is `global-administrators`.
- Actor ID format is `{tenant}:{domain}:{aggregateId}`.
- Use `TenantIdentity.DefaultTenantId`, `TenantIdentity.GlobalAdministratorsDomain`, `TenantIdentity.GlobalAdministratorsAggregateId`, and `TenantIdentity.ForGlobalAdministrators()` rather than hard-coded literals in new or changed code.
- Global-admin assignment commands target the singleton global-admin aggregate, not any managed tenant aggregate.
- Do not route global-administrator events as tenant-domain events.

### Previous Story Intelligence

- Story 2.1 registered `system|global-administrators|v1` domain service routing to the Tenants `/process` route; keep that route intact.
- Story 2.1 hardened startup bootstrap to use 26-character ULID `messageId` and `correlationId`; assignment command tests should keep ULID command envelope identifiers.
- Story 2.1 review removed configured administrator user ID from bootstrap logs and bounded rejection-response probing. Keep assignment logs equally support-safe.
- Story 2.1 validation showed deterministic builds pass, but VSTest can abort before executing tests in this environment with `System.Net.Sockets.SocketException (13): Permission denied` at `SocketServer.Start` / `TcpListener`. Record this as blocked test execution rather than claiming tests passed.
- Recent source commits:
  - `e0c0a54 feat(story-2.1): Bootstrap the Initial Global Administrator`
  - `d733de4 docs(retro): sync epic 1 foundation guidance`
  - `344ffa5 feat(story-1.4): Verify Consumer Package Reference Experience`
  - `6ce94b8 feat(story-1.3): Add CI Quality Gates for Build, Test, Coverage, and Package Validation`
  - `76065d4 feat(story-1.2): Configure Central Build and Package Governance`

### Latest Technical Notes

- Local pinned versions are authoritative for implementation: .NET 10 SDK `10.0.300`, System.Text.Json, xUnit v3, Shouldly, and current EventStore submodule APIs from this workspace.
- Microsoft documents `System.Text.Json` as supporting serialization/deserialization of records and POCO-style public properties, which matches the contract record approach. [Source: Microsoft Learn, `Supported types in System.Text.Json`](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-types)
- Microsoft documents `DateTimeOffset.UtcNow` as a UTC timestamp source with zero offset; this aligns with the project rule to use `DateTimeOffset.UtcNow` for event timestamps. [Source: Microsoft Learn, `DateTimeOffset.UtcNow`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset.utcnow)
- No dependency or package-version change is required by this story. If serializer or timestamp behavior appears inconsistent, verify against the pinned SDK and project tests before changing dependencies.

### Out of Scope

- Story 2.1 startup bootstrap behavior, except where assignment commands must not bypass it.
- Story 2.3 global administrator authorization for cross-tenant tenant governance commands.
- Tenant create/update/disable/enable behavior from Stories 2.4 and 2.5.
- Query endpoint authorization beyond preserving existing global-admin projection/read model behavior.
- Public admin UI, Phase 2 UX work, quickstart documentation, deployment smoke tests, or new CLI tooling.
- Renaming public command contracts from `SetGlobalAdministrator` to `AddGlobalAdministrator` unless product explicitly accepts the pre-1.0 contract churn.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Manage Global Administrator Assignments`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Global Administration`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Bootstrap`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Serialization`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorSet.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: Microsoft Learn `Supported types in System.Text.Json`](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-types)
- [Source: Microsoft Learn `DateTimeOffset.UtcNow`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset.utcnow)

## Project Structure Notes

- Alignment: story work belongs in existing backend/package projects only:
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server`
  - `Hexalith.Tenants/src/Hexalith.Tenants`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing`
  - matching tests under `Hexalith.Tenants/tests/Hexalith.Tenants.*.Tests`
- Likely implementation touches:
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorSet.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs`
  - new `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdministratorAlreadyExistsRejection.cs`
  - new `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdministratorNotFoundRejection.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
  - `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs`
  - projection/read-model tests under `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Projections/`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
  - relevant testing package tests under `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/`
- Avoid touching tenant lifecycle aggregate behavior, package governance scripts, CI workflows, docs, FrontComposer/UI artifacts, or submodule source unless a focused failing test proves it is required.

## Validation Checklist Results

- Story foundation extracted from Epic 2 and Story 2.2 acceptance criteria.
- PRD and architecture context incorporated: existing global administrators manage assignment, last-admin protection, auditable domain events, global-admin singleton aggregate, and structured rejection outcomes.
- Current source files inspected for existing implementation and likely touch points.
- Previous Story 2.1 learnings incorporated, including global-admin routing, ULID command identifiers, support-safe logging, and VSTest socket blocker handling.
- Disaster-prevention gaps called out explicitly: do not duplicate existing contracts, do not leave duplicate/missing assignment outcomes as `NoOp`, do not let assignment commands bypass bootstrap, add timestamp/actor audit fields, update projections/testing fakes after event-shape changes, and keep Story 2.3 tenant-governance authorization out of scope.
- Latest technical context checked against official Microsoft documentation for System.Text.Json supported types and `DateTimeOffset.UtcNow`; no dependency/version change is required.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red-phase focused server validation first failed at compile time because `GlobalAdministratorSet.ActorUserId`, `GlobalAdministratorSet.SetAt`, `GlobalAdministratorRemoved.ActorUserId`, `GlobalAdministratorRemoved.RemovedAt`, `GlobalAdministratorAlreadyExistsRejection`, and `GlobalAdministratorNotFoundRejection` did not exist.
- Final focused `dotnet test` commands built their assemblies, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start(String endPoint)` / `TcpListener`.
- Final release build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`.

### Completion Notes List

- Implemented envelope-aware `SetGlobalAdministrator` and `RemoveGlobalAdministrator` handlers that require the actor from `CommandEnvelope.UserId` to already be a global administrator.
- Replaced duplicate add and missing remove `NoOp` outcomes with structured `GlobalAdministratorAlreadyExistsRejection` and `GlobalAdministratorNotFoundRejection` events while preserving last-admin rejection behavior.
- Added audit fields to global-admin assignment success events: `ActorUserId`, `SetAt`, and `RemovedAt`, using `DateTimeOffset.UtcNow`.
- Updated replay handlers, audit projection detail payloads, in-memory testing fake behavior, command identity helpers, telemetry known command names, aggregate tests, and testing conformance coverage.
- Review auto-fixes corrected the telemetry allow-list for the actual `UpdateTenant` command, added a regression proving removed administrators lose subsequent assignment authority, and tightened audit read-model tests for platform-scoped global-admin events and audit fields.
- No new dependencies were added; no per-command REST controller or logging of command/event payloads was introduced.
- Test execution is environment-blocked by the known local VSTest socket restriction; build output is clean and used as partial validation evidence.

### File List

- `_bmad-output/implementation-artifacts/2-2-manage-global-administrator-assignments.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorSet.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdministratorAlreadyExistsRejection.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdministratorNotFoundRejection.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Helpers/TenantTestHelpers.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after auto-fixes.

Findings fixed:

- [MEDIUM] `TenantMetrics` still treated the nonexistent `UpdateTenantInformation` name as known while sanitizing the actual `UpdateTenant` command to `unknown`. Fixed the allow-list and telemetry test data.
- [MEDIUM] Acceptance Criterion 2 lacked a direct regression proving a removed global administrator no longer has assignment authority in the next command evaluation. Added `Removed_administrator_cannot_manage_assignments_in_subsequent_command`.
- [LOW] Audit read-model tests still classified global-administrator events with tenant `tenant-1` and default audit payload values. Updated tests to use platform tenant `system` and assert `actorUserId`, `setAt`, and `removedAt` narrative fields.

Validation:

- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "GlobalAdministratorsAggregateTests|GlobalAdministratorProjectionHandlerTests|GlobalAdministratorProjectionTests|GlobalAdministratorReadModelTests|TenantMetricsTests|TenantAuditReadModelTests" -m:1 -nr:false` built successfully, then VSTest aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start(String endPoint)` / `TcpListener`.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false` built successfully, then VSTest aborted with the same socket restriction.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|InMemoryTenantProjectionTests|TenantConformanceTests" -m:1 -nr:false` built successfully, then VSTest aborted with the same socket restriction.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.

### Change Log

- 2026-05-31: Implemented global administrator assignment management with structured duplicate/not-found rejections, audit-complete assignment events, envelope-based actor authorization, fake/conformance updates, and validation evidence.
- 2026-05-31: Completed senior developer review auto-fixes for telemetry command naming, removed-admin authority regression coverage, audit read-model assertion coverage, and sprint status sync.
