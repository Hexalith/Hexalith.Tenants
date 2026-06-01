---
created: 2026-06-01
source_story_key: 8-2-publish-the-event-contract-reference
baseline_commit: 6c5e1f7515e21fd786eaaadb0638fdddf34000d1
---

# Story 8.2: Publish the Event Contract Reference

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want a complete event contract reference,
so that I can subscribe to the right tenant events and handle their schemas safely.

## Acceptance Criteria

1. Given the contract reference is opened, when a developer reviews tenant commands, events, and rejections, then the reference lists every public command, event, query, and rejection contract, and it identifies the owning package and intended consumer for each contract.
2. Given an event contract is documented, when a developer reads its schema, then required fields, optional fields, timestamp fields, tenant identity fields, event ID, aggregate version, and serialization shape are documented, and every tenant event identifies the top-level managed `TenantId` requirement.
3. Given a rejection contract is documented, when a developer reads the reference, then rejection payload fields are described as structured data, and the reference does not encourage consumers to depend on persisted English prose.
4. Given CloudEvents publication is documented, when a consumer subscribes through DAPR, then the reference identifies the topic `tenants.events`, event type filtering guidance, and at-least-once delivery assumptions, and it does not imply cross-service ordering guarantees.
5. Given contract documentation validation runs, when public contract types are added, removed, or renamed, then validation detects stale reference content or missing entries, and the docs are updated before the story can be considered complete.

## Tasks / Subtasks

- [x] Audit and update the existing `docs/event-contract-reference.md` instead of recreating it from scratch. (AC: 1-4)
  - [x] Keep this story scoped to the event/query/rejection contract reference. Do not absorb Story 8.3 sample walkthrough, Story 8.5 cross-aggregate timing, or Story 8.6 compensating-command content beyond short cross-links.
  - [x] Preserve useful existing sections: event delivery model, identity scheme, enums, TenantAggregate, GlobalAdministratorsAggregate, rejection table, query reference, quick reference, and idempotency links.
  - [x] Remove or correct any stale text inherited from the archived legacy Story 8.2 where it conflicts with current source or current Epic 8 split.

- [x] Verify and document every public contract type from source. (AC: 1, 2, 3)
  - [x] Commands: list all 12 records in `src/Hexalith.Tenants.Contracts/Commands/*.cs` with package `Hexalith.Tenants.Contracts`, owning aggregate/domain, fields, intended caller, and success/rejection outcomes.
  - [x] Success events: list all 11 records in `src/Hexalith.Tenants.Contracts/Events/*.cs` with fields, timestamp fields, managed `TenantId` semantics, producing command(s), topic `tenants.events`, and consumer use.
  - [x] Rejections: list all 14 records in `src/Hexalith.Tenants.Contracts/Events/Rejections/*.cs` with structured payload fields, mapped HTTP status/reason code where applicable, and corrective action that does not require parsing English prose.
  - [x] Queries and DTOs: list the 5 `IQueryContract` classes and their public response DTOs in `src/Hexalith.Tenants.Contracts/Queries/*.cs`, including `QueryType`, `Domain`, `ProjectionType`, response shape, intended REST adapter, and intended consumer.
  - [x] Enums: document `TenantRole`, `TenantStatus`, and `AuditEventCategory` values and serialization behavior.

- [x] Fix serialization-shape guidance and examples. (AC: 2, 3)
  - [x] Verify the actual EventStore serialization options used for persisted/published event payloads before editing examples; current source uses System.Text.Json and EventStore web defaults in relevant serialization paths, while contract enums add explicit converters.
  - [x] Ensure payload JSON examples use the actual published shape, including property casing, enum names, nullable fields, and `DateTimeOffset` examples with timezone offset.
  - [x] Document `TenantRole` serialization by name from `[JsonConverter(typeof(JsonStringEnumConverter<TenantRole>))]`.
  - [x] Document `TenantStatus` serialization by name and fail-closed unknown handling from `TenantStatusJsonConverter`.
  - [x] For envelope metadata, document the EventStore/CloudEvents fields consumers need for identity, deduplication, and ordering: CloudEvents `id`, `source`, `type`, `specversion`, plus EventStore `MessageId`, `SequenceNumber` or aggregate version, `Timestamp`, `CorrelationId`, `CausationId`, and `UserId` as exposed by the repository's envelope model.

- [x] Tighten CloudEvents, DAPR, and ordering guidance. (AC: 4)
  - [x] State that Tenants domain events publish to the shared DAPR pub/sub topic `tenants.events`; consumers filter by event type and do not create per-event topics.
  - [x] State that DAPR pub/sub delivery is at-least-once, so handlers must be idempotent and deduplicate by the stable event/message identifier.
  - [x] State that aggregate-local order is available only within a single aggregate stream/version sequence. Do not imply cross-tenant, cross-aggregate, cross-service, or subscriber-observation ordering.
  - [x] Link to `docs/idempotent-event-processing.md` for implementation patterns instead of duplicating it.

- [x] Add source-backed validation tests for the reference. (AC: 5)
  - [x] Add or extend documentation tests under `tests/Hexalith.Tenants.Server.Tests/Documentation/`, following `QuickstartDocumentationTests.cs` patterns.
  - [x] Reflection-test that every public command, success event, rejection event, query contract, query DTO, and public enum in `Hexalith.Tenants.Contracts` appears in `docs/event-contract-reference.md`.
  - [x] Test that the reference contains the current DAPR topic `tenants.events`, CloudEvents/at-least-once guidance, no cross-service ordering guarantee, and a link to `docs/idempotent-event-processing.md`.
  - [x] Parse every fenced `json` block in the reference; assert valid JSON, no placeholder angle-bracket values, and enum examples that deserialize consistently with the contract converters.
  - [x] Add focused assertions for known drift-prone contracts: `TenantLifecycleStateAlreadySetRejection`, `ConfigurationKeyNotFoundRejection`, `GlobalAdministratorAlreadyExistsRejection`, `GlobalAdministratorNotFoundRejection`, `TenantUpdated.UpdatedAt`, `GlobalAdministratorSet.ActorUserId/SetAt`, `GlobalAdministratorRemoved.ActorUserId/RemovedAt`, and all query DTOs.

- [x] Update validation evidence. (AC: 5)
  - [x] Run the focused documentation tests through the direct xUnit runner if `dotnet test` hits the sandbox VSTest socket limitation recorded in Story 8.1.
  - [x] Run the relevant Tier 1 contract tests if touched validation code references contract assembly reflection.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` or `tests/test-summary.md` only if this repository continues recording documentation validation evidence there.
  - [x] Record any infrastructure limitations explicitly; this story should not require Docker, DAPR, AppHost, or live pub/sub execution.

## Dev Notes

### Source Context

- Epic 8 objective: developers can adopt through validated documentation and demo evidence. Story 8.2 owns the event contract reference; Story 8.3 owns the sample walkthrough; Story 8.5 owns cross-aggregate timing; Story 8.6 owns compensating commands. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.2 acceptance requires the reference to list every public command, event, query, and rejection contract; document owning package and consumer; specify payload fields, optional fields, timestamp fields, identity fields, event ID, aggregate version, serialization shape, DAPR topic, filtering guidance, at-least-once assumptions, and validation for stale content. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.2: Publish the Event Contract Reference`]
- PRD FR61 requires event contract reference documentation for commands, events, and schemas. FR42 requires idempotent event processing guidance, but this story should link to the existing idempotency document rather than duplicate it. [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`; `_bmad-output/planning-artifacts/epics.md#Functional Requirements`]
- Architecture maps Epic 8 work to `docs/`, `README.md`, and the sample project, and maps serialization/contract stability to `Contracts.Tests`. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#File Organization Patterns`]

### Current Repository State

- `docs/event-contract-reference.md` already exists. It is the primary update target, not a blank deliverable.
- `docs/idempotent-event-processing.md`, `docs/quickstart.md`, `docs/cross-aggregate-timing.md`, and `docs/compensating-commands.md` already exist. Story 8.2 may link to them but should not expand their scope.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs` provides a useful pattern for documentation tests: read markdown from the repo root, assert required source-backed text, parse fenced JSON, and deserialize command payloads against real contracts.
- Existing `docs/event-contract-reference.md` already mentions current additions such as `TenantLifecycleStateAlreadySetRejection`, `ConfigurationKeyNotFoundRejection`, global administrator duplicate/not-found rejections, query API reference, and fail-closed enum handling. Implementation should audit this content against source and tests instead of assuming it is complete.
- Potential drift to verify carefully: current event JSON examples in `docs/event-contract-reference.md` use PascalCase property names, while project API guidance and System.Text.Json web defaults generally use camelCase. Do not preserve that shape without verifying the actual EventStore persisted/published payload shape.

### Contract Inventory Verified During Story Creation

Commands in `Hexalith.Tenants.Contracts`:

| Command | Fields |
| --- | --- |
| `CreateTenant` | `TenantId`, `Name`, `Description?` |
| `UpdateTenant` | `TenantId`, `Name`, `Description?` |
| `DisableTenant` | `TenantId` |
| `EnableTenant` | `TenantId` |
| `AddUserToTenant` | `TenantId`, `UserId`, `Role` |
| `RemoveUserFromTenant` | `TenantId`, `UserId` |
| `ChangeUserRole` | `TenantId`, `UserId`, `NewRole` |
| `SetTenantConfiguration` | `TenantId`, `Key`, `Value` |
| `RemoveTenantConfiguration` | `TenantId`, `Key` |
| `BootstrapGlobalAdmin` | `UserId` |
| `SetGlobalAdministrator` | `UserId` |
| `RemoveGlobalAdministrator` | `UserId` |

Success events in `Hexalith.Tenants.Contracts`:

| Event | Fields |
| --- | --- |
| `TenantCreated` | `TenantId`, `Name`, `Description?`, `CreatedAt` |
| `TenantUpdated` | `TenantId`, `Name`, `Description?`, `UpdatedAt` |
| `TenantDisabled` | `TenantId`, `DisabledAt` |
| `TenantEnabled` | `TenantId`, `EnabledAt` |
| `UserAddedToTenant` | `TenantId`, `UserId`, `Role` |
| `UserRemovedFromTenant` | `TenantId`, `UserId` |
| `UserRoleChanged` | `TenantId`, `UserId`, `OldRole`, `NewRole` |
| `TenantConfigurationSet` | `TenantId`, `Key`, `Value` |
| `TenantConfigurationRemoved` | `TenantId`, `Key` |
| `GlobalAdministratorSet` | `TenantId`, `UserId`, `ActorUserId`, `SetAt` |
| `GlobalAdministratorRemoved` | `TenantId`, `UserId`, `ActorUserId`, `RemovedAt` |

Rejection events in `Hexalith.Tenants.Contracts`:

| Rejection | Fields |
| --- | --- |
| `TenantAlreadyExistsRejection` | `TenantId` |
| `TenantNotFoundRejection` | `TenantId` |
| `TenantDisabledRejection` | `TenantId` |
| `TenantLifecycleStateAlreadySetRejection` | `TenantId`, `CurrentStatus`, `RequestedStatus`, `CommandName` |
| `UserAlreadyInTenantRejection` | `TenantId`, `UserId`, `ExistingRole` |
| `UserNotInTenantRejection` | `TenantId`, `UserId` |
| `RoleEscalationRejection` | `TenantId`, `UserId`, `AttemptedRole` |
| `InsufficientPermissionsRejection` | `TenantId`, `ActorUserId`, `ActorRole?`, `CommandName` |
| `ConfigurationLimitExceededRejection` | `TenantId`, `LimitType`, `CurrentCount`, `MaxAllowed` |
| `ConfigurationKeyNotFoundRejection` | `TenantId`, `Key` |
| `GlobalAdminAlreadyBootstrappedRejection` | `TenantId` |
| `GlobalAdministratorAlreadyExistsRejection` | `TenantId`, `UserId` |
| `GlobalAdministratorNotFoundRejection` | `TenantId`, `UserId` |
| `LastGlobalAdministratorRejection` | `TenantId`, `UserId` |

Query contracts and DTOs:

| Query Contract | QueryType | ProjectionType | Response DTO |
| --- | --- | --- | --- |
| `ListTenantsQuery` | `list-tenants` | `tenant-index` | `PaginatedResult<TenantSummary>` |
| `GetTenantQuery` | `get-tenant` | `tenants` | `TenantDetail` |
| `GetTenantUsersQuery` | `get-tenant-users` | `tenants` | `PaginatedResult<TenantMember>` |
| `GetUserTenantsQuery` | `get-user-tenants` | `tenant-index` | `PaginatedResult<UserTenantMembership>` |
| `GetTenantAuditQuery` | `get-tenant-audit` | `tenants` | `PaginatedResult<TenantAuditEntry>` |

### Technical Guardrails

- Use repo-pinned versions and package families from project context. Do not bump dependencies for this documentation story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Public contracts belong to `Hexalith.Tenants.Contracts`; do not move commands/events/query DTOs or add package references to satisfy documentation tests. [Source: `_bmad-output/project-context.md#Publishing`; `_bmad-output/planning-artifacts/prd.md#NuGet Package Architecture`]
- Platform tenant is `system`; tenant domain is `tenants`; global administrator domain and aggregate ID are `global-administrators`. Every tenant-domain success event payload must include top-level managed `TenantId`. [Source: `_bmad-output/project-context.md#Identity Scheme`; `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`]
- DAPR resource naming is contractual: AppId `tenants`, state store `statestore`, topic `tenants.events`, dead letter topic `deadletter.tenants.events`. [Source: `_bmad-output/project-context.md#DAPR`]
- Error responses use Problem Details at the HTTP boundary, while persisted rejection events are structured data. The reference must not teach consumers to parse persisted English messages. [Source: `_bmad-output/project-context.md#API Surface`; `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- Use xUnit v3 and Shouldly for new tests; do not use `Assert.*`; do not add per-file `using Xunit;`. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Previous Story Intelligence

- Story 8.1 completed quickstart hardening and added documentation tests that validate markdown examples against source contracts. Reuse that pattern for Story 8.2. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md#Completion Notes List`]
- Story 8.1 recorded that `dotnet test` can build but VSTest may abort in this sandbox with `SocketException (13): Permission denied`; direct xUnit runner execution worked for focused tests. Use the same fallback and record it if it recurs. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md#Debug Log References`]
- Recent commit history shows Story 8.1 landed after Epic 7 auth, DAPR, observability, and stateless readiness work. Contract documentation should reflect those latest source changes, especially production claim boundaries, command gateway route, DAPR topic naming, command status/rejection behavior, and health/readiness wording where referenced.

### Latest Technical Notes

- DAPR's current docs state pub/sub uses CloudEvents 1.0 and that DAPR wraps application payloads in a CloudEvents envelope for pub/sub publication. [Source: DAPR Docs, Publishing & subscribing messages with CloudEvents](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/)
- DAPR's current pub/sub API docs state at-least-once semantics and CloudEvents 1.0 adherence. [Source: DAPR Docs, Pub/sub API reference](https://docs.dapr.io/reference/api/pubsub_api/)
- CloudEvents requires `id`, `source`, `specversion`, and `type` context attributes; consumers may treat identical `source` plus `id` as duplicates. [Source: CloudEvents specification](https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md)
- Microsoft docs confirm `JsonStringEnumConverter` serializes enum names as strings. [Source: Microsoft Learn, Customize properties and values with System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)

### Existing Files Likely to Touch

- `docs/event-contract-reference.md`: primary update target.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs`: likely new validation test file.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`: reference pattern; edit only if extracting shared helpers is clearly worthwhile.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` and/or `tests/test-summary.md`: update only if validation evidence continues to be recorded there.
- Avoid editing `docs/cross-aggregate-timing.md`, `docs/compensating-commands.md`, and `docs/demo.md` unless link drift discovered by this story requires a small correction.

### Project Structure Notes

- Alignment: Epic 8 documentation belongs under `docs/`, with validation in existing test projects. This story should not change domain behavior, package topology, AppHost topology, DAPR component templates, or sample-service behavior.
- Detected conflict: the archived legacy `8-2-event-contract-reference-and-technical-documentation.md` bundled event contracts, cross-aggregate timing, and compensating commands. Current sprint status splits those into separate stories, so do not copy that scope wholesale.
- Detected drift risk: `docs/event-contract-reference.md` exists but was not marked ready in current sprint status. Treat it as prior work that needs source-backed audit, not as accepted completion.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.2: Publish the Event Contract Reference`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#File Organization Patterns`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md`]
- [Source: `docs/event-contract-reference.md`]
- [Source: `docs/idempotent-event-processing.md`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/`]
- [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/`]
- [Source: `src/Hexalith.Tenants.Contracts/Queries/`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- [Source: DAPR Docs, Publishing & subscribing messages with CloudEvents](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/)
- [Source: DAPR Docs, Pub/sub API reference](https://docs.dapr.io/reference/api/pubsub_api/)
- [Source: CloudEvents specification](https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md)
- [Source: Microsoft Learn, Customize properties and values with System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)

## Validation Checklist Results

- Story foundation: PASS. Story statement and all five Epic 8.2 acceptance criteria are preserved.
- Scope control: PASS. The story explicitly excludes Story 8.3, 8.5, and 8.6 implementation scope while allowing cross-links.
- Architecture/source context: PASS. The story cites current contracts, query DTOs, identity constants, DAPR topic naming, serialization rules, and existing documentation-test patterns.
- Reinvention prevention: PASS. The story directs the developer to audit/update existing `docs/event-contract-reference.md` and add source-backed validation instead of recreating a parallel reference.
- Wrong-library/version prevention: PASS. The story records System.Text.Json, DAPR, CloudEvents, xUnit v3, Shouldly, and package-management constraints without dependency changes.
- File-location prevention: PASS. Expected changes are limited to `docs/`, existing test projects, and validation evidence files where appropriate.
- Regression prevention: PASS. The story calls out drift-prone contracts, current rejection count, query contracts, EventStore/CloudEvents envelope metadata, and at-least-once ordering limits.
- Validation evidence: PASS. The story requires reflection/source-backed documentation tests and JSON parsing/deserialization checks so stale contract docs fail in CI.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (story authoring)

### Debug Log References

- Senior review found source-backed drift: `SetTenantConfiguration` and `RemoveTenantConfiguration` were documented as tenant contributor/owner commands, but `TenantAggregate` requires `TenantOwner` unless the actor is global admin.
- Senior review found incomplete rejection outcome documentation: `DisableTenant` and `EnableTenant` omitted `InsufficientPermissionsRejection` for non-global-admin actors.
- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors after review fixes.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.EventContractReferenceDocumentationTests -parallel none -noLogo -noColor` passed after review fixes: 7 total, 0 failed, 0 skipped.
- Resolved `bmad-dev-story` workflow customization: no activation prepend/append steps; persistent fact `_bmad-output/project-context.md` loaded.
- Marked Story 8.2 in-progress with baseline commit `6c5e1f7515e21fd786eaaadb0638fdddf34000d1`.
- Verified EventStore persisted event payload serialization uses `JsonSerializer.SerializeToUtf8Bytes(eventPayload, eventPayload.GetType())` with default options, while EventStore gateway HTTP paths use `JsonSerializerDefaults.Web`.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~EventContractReferenceDocumentationTests --no-restore` aborted before execution in this sandbox with `SocketException (13): Permission denied`; direct xUnit runner fallback was used.
- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- Direct xUnit full regression across Contracts, Client, Testing, Server, Sample, and Integration test assemblies passed: 1316 total, 0 failed, 26 skipped for DAPR/performance prerequisites.
- Resolved `bmad-create-story` workflow customization: no activation prepend/append steps; persistent fact `_bmad-output/project-context.md` loaded.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: `planning_artifacts` and `implementation_artifacts` resolved under `_bmad-output/`.
- Loaded sprint status and selected explicit Story 8.2 from current Epic 8 backlog key `8-2-publish-the-event-contract-reference`.
- Loaded Epic 8, PRD documentation/adoption requirements, architecture structure guidance, Story 8.1, archived legacy Story 8.2, current `docs/event-contract-reference.md`, contract source files, and documentation-test patterns.
- Researched current DAPR CloudEvents/pub-sub docs, CloudEvents specification, and Microsoft System.Text.Json enum converter documentation for latest technical context.

### Completion Notes List

- Senior review fixed source-backed contract reference drift for configuration command authorization and lifecycle command rejection outcomes.
- Added focused documentation regression coverage for those authorization/rejection outcome rows.
- Updated `docs/event-contract-reference.md` in place with source-backed contract inventory tables for all 12 commands, 11 success events, 14 rejections, 5 query contracts, public DTOs, and 3 enums.
- Added explicit serialization guidance for EventStore payload casing, enum converters, `DateTimeOffset` examples, and CloudEvents/EventStore envelope metadata used for identity, deduplication, and aggregate-local ordering.
- Preserved the existing delivery model, identity, aggregate, rejection, query, quick reference, and idempotency sections while keeping Story 8.3/8.5/8.6 content to links and short references only.
- Added `EventContractReferenceDocumentationTests` to prevent stale contract names, invalid JSON examples, missing DAPR/CloudEvents guidance, and known drift-prone contract omissions.
- Recorded validation evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Created Story 8.2 as a source-backed documentation audit/update story for the existing event contract reference.
- Preserved the current Epic 8 story split and avoided importing cross-aggregate timing or compensating-command scope from the archived legacy story.
- Captured current command, event, rejection, query, DTO, enum, DAPR, serialization, and validation guardrails for implementation.

### File List

- `docs/event-contract-reference.md`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md`
- `_bmad-output/story-automator/orchestration-7-20260601-143204.md`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

### Outcome

Approved after automatic fixes. No critical issues remain.

### Findings Fixed

- HIGH: `SetTenantConfiguration` and `RemoveTenantConfiguration` were documented as callable by tenant contributors, but `TenantAggregate` requires `TenantOwner` unless the trusted command envelope marks the actor as global admin. Fixed the command inventory and detailed sections, and added regression assertions.
- HIGH: `DisableTenant` and `EnableTenant` omitted `InsufficientPermissionsRejection` even though source returns it for non-global-admin actors. Fixed the command inventory, detailed lifecycle sections, and quick reference.

### Validation

- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.EventContractReferenceDocumentationTests -parallel none -noLogo -noColor` passed: 7 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -namespace Hexalith.Tenants.Contracts.Tests -parallel none -noLogo -noColor` passed: 92 total, 0 failed, 0 skipped.

### Checklist Result

- Story status was `review` at review start; story and sprint status are now `done`.
- Acceptance criteria, completed tasks, source contracts, claimed file list, changed files, tests, code quality, and security-sensitive documentation claims were reviewed.
- Architecture/standards context came from `_bmad-output/project-context.md`, planning artifacts, source contracts, and existing test patterns. External DAPR/CloudEvents references were already captured in the story context; no new dependency or live service lookup was required for the source-backed fixes.

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-01 | 1.1 | Senior review fixed source-backed authorization/rejection documentation drift and added regression assertions. | GPT-5 Codex |
| 2026-06-01 | 1.0 | Implemented source-backed event contract reference audit, validation tests, and validation evidence. | GPT-5 Codex |
| 2026-06-01 | 0.1 | Created Story 8.2 context for source-backed event contract reference audit and validation. | GPT-5 Codex |
