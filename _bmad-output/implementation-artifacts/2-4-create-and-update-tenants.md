---
baseline_commit: 1c5882490d9c6f10b4f67557431bc5bce2fc39d6
---

# Story 2.4: Create and Update Tenants

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a global administrator,
I want to create tenants and update tenant metadata,
so that tenant records can be introduced and maintained as event-sourced domain state.

## Acceptance Criteria

1. Given a global administrator submits a create tenant command with a unique tenant identifier and name, when the tenant aggregate handles the command, then it records a tenant created event, and the event includes top-level `TenantId`, name, optional description, and `CreatedAt` data.
2. Given a create tenant command targets an existing tenant aggregate, when the command is handled, then the aggregate returns a structured duplicate tenant rejection, and no second tenant created event is produced.
3. Given an authorized actor submits an update tenant metadata command, when the target tenant exists and is enabled, then the aggregate records a tenant updated event, and the event includes top-level `TenantId`, updated metadata, and `UpdatedAt` data.
4. Given an update tenant metadata command targets a missing tenant, when the command is handled, then the aggregate returns a structured tenant-not-found rejection, and no update event is produced.
5. Given tenant lifecycle contracts are tested, when naming convention and serialization tests run, then create and update commands, events, and rejections follow the project naming conventions, and all events round-trip through `System.Text.Json`.

## Tasks / Subtasks

- [x] Complete create tenant aggregate behavior without reimplementing existing plumbing (AC: 1, 2)
  - [x] Preserve `CreateTenant` as an immutable contract record in `src/Hexalith.Tenants.Contracts/Commands/CreateTenant.cs`.
  - [x] Preserve `TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt)` in `src/Hexalith.Tenants.Contracts/Events/TenantCreated.cs`.
  - [x] Keep `TenantAggregate.Handle(CreateTenant, TenantState?, CommandEnvelope)` envelope-aware and pure; it must require trusted global-admin authority, use `envelope.AggregateId` as the canonical tenant ID, return `TenantAlreadyExistsRejection` for existing state, and emit `DateTimeOffset.UtcNow` only on successful creation.
  - [x] Do not create per-command REST endpoints; create commands continue through EventStore command submission and the Tenants `/process` domain processor route.

- [x] Add required update timestamp to the tenant update contract path (AC: 3, 5)
  - [x] Change `TenantUpdated` to include `DateTimeOffset UpdatedAt` while preserving top-level `TenantId`, `Name`, and nullable `Description`.
  - [x] Update `TenantAggregate.Handle(UpdateTenant, TenantState?, CommandEnvelope)` to emit `new TenantUpdated(envelope.AggregateId, command.Name, command.Description, DateTimeOffset.UtcNow)`.
  - [x] Add focused assertions that `TenantUpdated.UpdatedAt` is within the command execution time window.
  - [x] Update all direct `new TenantUpdated(...)` call sites in source, tests, docs samples, projection fixtures, and conformance tests.

- [x] Preserve update authorization and rejection ordering from Story 2.3 (AC: 3, 4)
  - [x] Keep global administrators authorized through the trusted `actor:globalAdmin=true` envelope extension only; do not parse claims in aggregate code and do not trust command body fields or request extensions.
  - [x] Keep tenant contributors and owners authorized for `UpdateTenant`; readers and non-members must receive `InsufficientPermissionsRejection`.
  - [x] Keep missing tenant before disabled tenant and authorization checks for `UpdateTenant`: null state returns `TenantNotFoundRejection`, disabled state returns `TenantDisabledRejection`, unauthorized active state returns `InsufficientPermissionsRejection`.
  - [x] Keep all create/update events and rejections using `envelope.AggregateId`, not `command.TenantId`, when the two differ.

- [x] Update state, projection, and audit consumers for the event shape (AC: 3, 5)
  - [x] Update `TenantState.Apply(TenantUpdated)` only for metadata unless a local `UpdatedAt` state property is added intentionally; do not disturb membership, configuration, or status state.
  - [x] Update `TenantReadModel.Apply(TenantUpdated)`, `TenantIndexReadModel.Apply(TenantUpdated)`, `TenantProjectionHandler`, `TenantAuditReadModel`, `InMemoryTenantProjection`, and `TenantProjectionEventHandler` call sites so deserialization and projection behavior still compile and remain idempotent.
  - [x] If audit details include tenant update metadata, add `updatedAt` using `ToString("O", CultureInfo.InvariantCulture)` matching the existing `createdAt`, `disabledAt`, and `enabledAt` audit metadata pattern.
  - [x] Update `docs/event-contract-reference.md` so the public event contract includes `UpdatedAt` and its JSON example.

- [x] Keep testing fake parity with production aggregate behavior (AC: 1-5)
  - [x] Ensure `InMemoryTenantService.ProcessCommand(CreateTenant)`, `ProcessCommand(UpdateTenant)`, and `ProcessTenantCommand<T>()` continue to delegate to `TenantAggregate.Handle(...)` rather than duplicating domain logic.
  - [x] Keep `TenantTestHelpers.CreateCommandEnvelope(...)` using `TenantIdentity.DefaultTenantId`, `TenantIdentity.Domain`, sortable unique IDs from `UniqueIdHelper`, and optional trusted global-admin extension setup.
  - [x] Update conformance assertions only where the new timestamp requires tolerant comparison; event sequences and ordering must still match between production aggregate and in-memory service.

- [x] Extend focused validation coverage (AC: 1-5)
  - [x] Update `TenantAggregateTests` for create success, duplicate create rejection, update success with `UpdatedAt`, missing update rejection, disabled update rejection, RBAC success/rejection, and command-body/envelope aggregate mismatch behavior.
  - [x] Update `EventSerializationTests` and `NamingConventionTests` expectations only as required by the `TenantUpdated` constructor shape; do not weaken reflection-based coverage.
  - [x] Update `InMemoryTenantServiceTests`, `TenantConformanceTests`, client projection tests, testing projection tests, server projection tests, and audit projection tests that construct or inspect `TenantUpdated`.
  - [x] Run focused contracts/server/testing/client validation before broader build:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false`
  - [x] Run focused aggregate/conformance/projection validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests|TenantProjectionHandlerTests|TenantIndexProjectionTests" -m:1 -nr:false`
  - [x] Run focused client/testing validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Client.Tests --filter "TenantProjectionEventHandlerTests|TenantEventProcessorTests" -m:1 -nr:false`
  - [x] Run focused testing package validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests|InMemoryTenantProjectionTests|InMemoryTenantProjectionConformanceTests" -m:1 -nr:false`
  - [x] Run release build:
    `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
  - [x] If local VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostics and use build results as partial evidence only.

## Dev Notes

### Source Context

- Epic 2 objective: global administrators can bootstrap the first admin, manage global administrators, create tenants, update metadata, disable or enable tenants, and receive structured rejection outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Global Administrators Can Bootstrap and Govern Tenants`]
- Story 2.4 requires create/update domain state to be event-sourced and explicitly calls for `TenantCreated.CreatedAt` and `TenantUpdated.UpdatedAt`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Create and Update Tenants`]
- FR coverage: FR1 global administrator creates tenants, FR2 tenant metadata can be updated, FR5 lifecycle changes produce domain events, FR49 structured rejections, FR50 missing tenant rejection, FR52 duplicate operation rejection. [Source: `_bmad-output/planning-artifacts/epics.md#FR Coverage Map`]
- PRD success criteria include a developer sending `CreateTenant` within 30 minutes, zero cross-tenant leaks, 100% tenant isolation/role authorization branch coverage, and event processing p95 below 50ms. [Source: `_bmad-output/planning-artifacts/prd.md#Success Criteria`]
- UX is not directly implemented in this backend story, but backend outcomes must remain precise enough for future UI command lifecycle and rejection states: accepted submission is not confirmed outcome, stale projection is normal, and failure details must be support-safe. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Core User Experience`]

### Current Repository State

- `CreateTenant`, `UpdateTenant`, `TenantCreated`, `TenantAlreadyExistsRejection`, and `TenantNotFoundRejection` already exist in `src/Hexalith.Tenants.Contracts`.
- `TenantAggregate.Handle(CreateTenant, ..., CommandEnvelope)` already requires trusted global-admin authority, uses `envelope.AggregateId`, emits `TenantCreated(..., DateTimeOffset.UtcNow)`, and returns `TenantAlreadyExistsRejection` for existing state.
- `TenantAggregate.Handle(UpdateTenant, ..., CommandEnvelope)` already uses `envelope.AggregateId`, permits global admins and tenant contributors/owners, rejects missing tenants, rejects disabled tenants, and rejects insufficient permissions. The important gap is that it currently emits `TenantUpdated(tenantId, name, description)` without `UpdatedAt`.
- `TenantUpdated` currently has only `TenantId`, `Name`, and `Description`; this is the primary AC gap and is a contract shape change. Update every constructor call and projection/documentation consumer in the same change.
- `TenantState.Apply(TenantUpdated)` and `TenantReadModel.Apply(TenantUpdated)` currently update only `Name` and `Description`. That behavior is valid unless the implementation intentionally adds an `UpdatedAt` state/read-model property with tests.
- Projection and consumer surfaces already deserialize or handle `TenantUpdated`: server `TenantProjectionHandler`, server read models, `TenantAuditReadModel`, client `TenantProjectionEventHandler`, testing `InMemoryTenantProjection`, and related tests.

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Contracts are immutable records in `Hexalith.Tenants.Contracts`; success events implement `IEventPayload`; rejection events implement `IRejectionEvent`.
- Aggregate `Handle` methods must remain pure static methods: no I/O, no DAPR, no async, no logging, and no captured state.
- Business-rule failures are domain rejections, not exceptions. Use collection expressions for result event lists.
- Use `DateTimeOffset.UtcNow` in aggregate handlers for `CreatedAt` and `UpdatedAt`.
- Platform tenant ID is `TenantIdentity.DefaultTenantId` (`system`), tenant domain is `TenantIdentity.Domain` (`tenants`), and managed tenant aggregate ID is the aggregate being governed.
- Every event payload must include top-level `TenantId`. Event envelope `TenantId` is the platform tenant (`system`), so consumers identify the managed tenant from the payload.
- Commands/events/rejections must satisfy naming tests: commands `{Verb}{Target}`, events `{Target}{PastVerb}`, rejections `{Target}{Reason}Rejection`.
- Use `System.Text.Json` only. Do not introduce Newtonsoft.Json or another serializer.
- Use K&R brace style, file-scoped namespaces, one type per file, source folder structure matching namespaces, and no inline `PackageReference Version=`.
- No external package or API research is needed: this story uses existing pinned .NET/EventStore/Tenants APIs and introduces no dependency upgrades.

### Previous Story Intelligence

- Story 2.3 hardened create/update/disable/enable to use trusted `actor:globalAdmin` envelope metadata and canonical `envelope.AggregateId`. Do not regress to command-body tenant IDs.
- Story 2.3 deliberately kept `UpdateTenant` compatible with existing RBAC: global admins bypass tenant membership; contributors and owners can update; readers and non-members are rejected.
- Story 2.3 added/fixed in-memory fake parity and command envelope helpers using `UniqueIdHelper.GenerateSortableUniqueStringId()`. Do not reintroduce GUID string IDs in tests or helpers.
- Story 2.3 test execution was locally blocked by VSTest socket permissions after successful builds. Record that exact blocker if it recurs instead of claiming tests passed.
- Recent commits:
  - `1c58824 feat(story-2.3): authorize global administrators for cross-tenant governance`
  - `9240d0c feat(tests): add global admin extension handling in integration tests and telemetry`
  - `8a0b2a1 change BMAD project name from Hexalith.Tenants to Tenants`
  - `644a987 BMAD 6.8.0`
  - `65ab1c0 docs(bmad): sync output artifacts`

### Likely Files to Touch

- `src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexProjectionTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`
- `docs/event-contract-reference.md`

### Out of Scope

- New tenant lifecycle commands beyond `CreateTenant` and `UpdateTenant`.
- Story 2.5 duplicate disable/enable rejection semantics.
- Public admin UI, FrontComposer, Phase 2 command flows, or new UX screens.
- Query endpoint redesign, cursor pagination, projection write conflict recovery, or audit query behavior from Epic 5.
- Global administrator assignment behavior from Story 2.2.
- Changing EventStore submodule code unless a focused failing test proves a direct compatibility issue.
- SDK, package, DAPR, Aspire, or OpenTelemetry version upgrades.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Create and Update Tenants`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Success Criteria`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#Convention-Driven Wiring (Reflection)`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/2-3-authorize-global-administrators-for-cross-tenant-governance.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs`]
- [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]
- [Source: `docs/event-contract-reference.md#UpdateTenant`]

## Project Structure Notes

- Alignment: implementation belongs in existing contracts, server aggregate/projection, client projection, testing fake/projection, tests, and event contract docs locations. No new project, package, API controller, or infrastructure dependency is expected.
- The main implementation risk is broad constructor churn from adding `UpdatedAt` to `TenantUpdated`; use compiler errors and `rg "new TenantUpdated"` to find every call site.
- The existing old artifact `_bmad-output/implementation-artifacts/2-4-tenant-service-bootstrap-and-event-publishing.md` is unrelated historical output. Use this story file for current sprint key `2-4-create-and-update-tenants`.

## Validation Checklist Results

- Story foundation extracted from Epic 2 and Story 2.4 acceptance criteria.
- PRD, architecture, UX, project context, previous Story 2.3, current source, and recent git history were reviewed.
- Current source inspection found partial implementation already present; the required `TenantUpdated.UpdatedAt` contract gap is explicitly called out.
- Existing UPDATE files and consumers were identified, including aggregate, state, projections, client handler, testing fake/projection, tests, and event contract docs.
- Disaster-prevention guardrails included: do not duplicate create/update plumbing, do not trust command body tenant IDs, do not parse claims in aggregate code, do not add REST endpoints, do not weaken reflection tests, and do not move Story 2.5 lifecycle semantics into this story.
- Latest technical context checked against local pinned project rules. No external dependency research is required because no package or framework update is part of this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: Added red-phase assertions for `TenantUpdated.UpdatedAt`; focused server test compile failed as expected before the contract change because `TenantUpdated` had no `UpdatedAt`.
- 2026-05-31: Required focused `dotnet test` commands built their test assemblies, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` at `System.Net.Sockets.TcpListener..ctor(IPEndPoint localEP)` / `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` succeeded with 0 warnings and 0 errors.
- 2026-05-31 review: Re-ran release build successfully with 0 warnings and 0 errors. Focused Contracts, Server, Client, Testing, and Integration test commands all compiled, then VSTest aborted before execution with the same `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.

### Completion Notes List

- `TenantUpdated` now carries `DateTimeOffset UpdatedAt`, and `TenantAggregate.Handle(UpdateTenant, ...)` emits it from `DateTimeOffset.UtcNow` while preserving canonical `envelope.AggregateId` usage and existing authorization/rejection ordering.
- Projection/read-model behavior remains metadata-only for tenant state, while audit entries now include `updatedAt` in round-trip `O` format with invariant culture.
- Direct `TenantUpdated` construction sites in focused server/client/testing tests were updated, aggregate coverage now asserts the timestamp execution window, and public event-contract docs include `UpdatedAt`.
- Full test execution is locally blocked by the VSTest socket permission issue documented above; Release solution build is clean and the required test projects compile before the abort.
- Review fixed the story File List so it matches source files actually modified for Story 2.4, including added API and DAPR integration coverage.

### File List

- `_bmad-output/implementation-artifacts/2-4-create-and-update-tenants.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexProjectionTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexReadModelTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantReadModelTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionTests.cs`

### Senior Developer Review (AI)

Reviewer: Codex on 2026-05-31

Outcome: Approved after automatic review fix.

Findings fixed:

- [Medium] The story File List omitted changed source test files present in git: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`. Fixed by adding both files to the Dev Agent Record File List.

Validation notes:

- Acceptance criteria were cross-checked against `TenantUpdated`, `TenantAggregate.Handle(CreateTenant/UpdateTenant)`, projection/read-model consumers, audit metadata, event-contract docs, and affected tests.
- `rg "new TenantUpdated"` found no remaining source/test constructor call sites missing `UpdatedAt` outside excluded BMAD historical artifacts.
- No external dependency or API research was needed; the story only uses existing pinned .NET/EventStore/Tenants APIs.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` passed.
- Focused `dotnet test` validation remains blocked by the local VSTest socket permission restriction after successful test assembly compilation.

### Change Log

- 2026-05-31: Added `UpdatedAt` to `TenantUpdated`, updated aggregate emission, audit metadata, direct test constructors, and event contract documentation.
- 2026-05-31: Review approved after synchronizing the story File List with changed integration test source files and re-running build/test validation.
