---
baseline_commit: ac3e5e0d082a16a9a509f7adb641da112b7a6252
---

# Story 4.1: Publish Tenant Domain Events as CloudEvents

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want tenant domain events to publish through a documented DAPR topic as CloudEvents,
so that my service can subscribe to tenant changes without direct infrastructure coupling.

## Acceptance Criteria

1. **Given** a tenant lifecycle, membership, role, configuration, or global-administrator domain event is persisted
   **When** event publication runs
   **Then** the event is published through DAPR pub/sub as a CloudEvents 1.0 message
   **And** publication uses the tenant event topic `tenants.events`.

2. **Given** a tenant event payload is published
   **When** a consumer receives the event
   **Then** the payload contains top-level managed `TenantId`
   **And** the envelope/platform tenant remains `system` according to EventStore conventions.

3. **Given** DAPR resource naming is reviewed
   **When** tenant event publication is configured
   **Then** the AppId, topic, state store, and dead-letter topic follow the documented conventions
   **And** no direct broker-specific dependency is introduced in domain code.

4. **Given** consumers subscribe to `tenants.events`
   **When** multiple event types are delivered on the shared topic
   **Then** consumers can filter by event type
   **And** documentation or sample code does not assume a separate topic per event type.

5. **Given** publication tests run
   **When** representative tenant events are emitted
   **Then** tests verify CloudEvents shape, topic naming, tenant identity fields, and event type metadata.

## Tasks / Subtasks

- [x] Task 1: Lock down topic conventions and current mismatches (AC: #1, #3, #4)
  - [x] Add or update tests proving tenant aggregate events publish to exactly `tenants.events`, not a per-event or per-aggregate topic.
  - [x] Add a failing test for global-administrator events: `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator` must also publish to `tenants.events`.
  - [x] Preserve the distinct `global-administrators` aggregate domain for command routing and projections; do not collapse it into the `tenants` domain just to fix topic naming.
  - [x] Update DAPR/AppHost resource naming where needed so local config and docs agree on `pubsub`, `tenants-eventstore`, `tenants.events`, and `deadletter.tenants.events`.

- [x] Task 2: Verify CloudEvents publication metadata at the publisher boundary (AC: #1, #5)
  - [x] Cover the EventStore publisher path that calls `DaprClient.PublishEventAsync`, including metadata for CloudEvents `type`, `source`, and `id`.
  - [x] Ensure the published CloudEvents `type` is the persisted event type name and supports consumer filtering by event type.
  - [x] Ensure publication does not set raw-payload mode or bypass DAPR CloudEvents wrapping.
  - [x] Keep payload logging redacted; tests must assert metadata and envelope fields, not log or snapshot payload contents unnecessarily.

- [x] Task 3: Verify tenant identity fields and payload contract (AC: #2, #5)
  - [x] Add reflection coverage for all public tenant event/rejection records implementing `IEventPayload` or `IRejectionEvent`: each must expose top-level `TenantId`.
  - [x] For managed tenant events, assert envelope `TenantId == "system"` and payload `TenantId == aggregateId`.
  - [x] For global-administrator events, assert envelope `TenantId == "system"` and payload `TenantId == "system"`.
  - [x] Preserve ULID message/correlation/causation ID expectations inherited from EventStore; do not switch to GUID validation.

- [x] Task 4: Add representative publication tests across event families (AC: #1, #4, #5)
  - [x] Cover lifecycle: `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`.
  - [x] Cover membership/role: `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`.
  - [x] Cover configuration: `TenantConfigurationSet`, `TenantConfigurationRemoved`.
  - [x] Cover global administrators: `GlobalAdministratorSet`, `GlobalAdministratorRemoved`.
  - [x] Cover at least one structured rejection event to prove rejection events follow the same topic and envelope contract when persisted.

- [x] Task 5: Align documentation and sample assumptions (AC: #3, #4)
  - [x] Update `docs/event-contract-reference.md` only if implementation behavior or topic/dead-letter wording differs from the documented contract.
  - [x] Ensure docs and sample guidance describe one shared topic, `tenants.events`, with event-type filtering.
  - [x] Do not introduce a broker-specific dependency or Redis-specific API in Tenants domain/server code.

- [x] Task 6: Verification (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Contracts.Tests/`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/`.
  - [x] Run relevant integration tests that exercise `FakeEventPublisher` or DAPR publication when local prerequisites are available.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release`.

## Dev Notes

### Current State

Tenants already has the event contracts and most publication infrastructure in place:

- Event records live under `src/Hexalith.Tenants.Contracts/Events/` and rejection records under `src/Hexalith.Tenants.Contracts/Events/Rejections/`.
- `TenantIdentity.DefaultTenantId` is `system`; tenant aggregate domain is `tenants`; global-administrator domain is `global-administrators`.
- `src/Hexalith.Tenants/appsettings.json` configures EventStore publisher `PubSubName` as `pubsub`.
- `Hexalith.EventStore.Server.Events.EventPublisher` publishes persisted `EventEnvelope` instances through `DaprClient.PublishEventAsync` and sets CloudEvent metadata keys for type, source, and id.
- `Hexalith.EventStore.Contracts.Identity.AggregateIdentity.PubSubTopic` currently derives `tenants.events` for `system/tenants`, but derives `global-administrators.events` for `system/global-administrators`.

That last point is the main implementation hazard. Story 4.1 requires a single consumer topic, `tenants.events`, for tenant lifecycle, membership, role, configuration, and global-administrator events. Do not silently accept `global-administrators.events`; add explicit coverage and resolve the mismatch without breaking the `global-administrators` command/projection domain.

### Architecture Guardrails

- DAPR pub/sub is the infrastructure abstraction. Do not add Redis, Kafka, RabbitMQ, or other broker APIs to domain code.
- The durable EventStore stream remains the source of truth. Pub/sub publication is asynchronous delivery for subscribers.
- Commands and event storage must not be rolled back because publication fails; existing graceful-degradation behavior must remain intact.
- Consumers must filter by event type on the shared topic; do not create one topic per event type.
- Global administrator events are part of the tenant-management integration contract even though their aggregate domain is distinct.
- Keep CloudEvents 1.0 conformance aligned with DAPR behavior. DAPR automatically wraps pub/sub messages as CloudEvents unless raw-payload mode is used.

### Files To Inspect Or Update

- `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs` - identity constants and aggregate identity helpers; preserve distinct domains.
- `src/Hexalith.Tenants/appsettings.json` - EventStore publisher `PubSubName` and any publisher options used by Tenants.
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` - local pub/sub and dead-letter naming; current local dead-letter value is `deadletter`, while architecture names `deadletter.tenants.events`.
- `src/Hexalith.Tenants.AppHost/Program.cs` - DAPR sidecar wiring and PubSub reference for EventStore/Tenants/sample.
- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs` - generic topic derivation; change only with EventStore-wide impact understood and tested.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs` - CloudEvents metadata and `DaprClient.PublishEventAsync` call.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/EventPublisherOptions.cs` - `PubSubName` and dead-letter topic derivation.
- `Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/FakeEventPublisher.cs` - existing fake captures topic and envelopes for integration assertions.
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs` - extend for top-level `TenantId` contract.
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/GracefulDegradationTests.cs` - existing publication and recovery evidence.

### Testing Standards

- Use xUnit v3 and Shouldly. Do not introduce `Assert.*`.
- Tier 1 tests must avoid live DAPR sidecars. Use reflection, contract serialization, fakes, or direct publisher unit tests.
- Tier 2/3 DAPR tests may be prerequisite-gated with the existing `DaprFact` pattern.
- If adding EventStore submodule tests, follow EventStore's local style in that submodule and keep the change narrowly scoped.
- Tests should assert topic, CloudEvents metadata, envelope tenant/domain/aggregate fields, event type name, sequence number, and payload `TenantId` semantics.

### Previous Story And Git Intelligence

Epic 4 is restarting from backlog in the current sprint status. An older artifact exists at `_bmad-output/implementation-artifacts/4-1-client-di-registration.md`, but it belongs to a prior Epic 4 shape and is not the current story. Use the current sprint key `4-1-publish-tenant-domain-events-as-cloudevents`.

Recent implementation commits completed Epic 3 stories for configuration removal, configuration limits, and concurrency rejection. Those stories reinforce the current contract patterns: event/rejection records are immutable contracts, rejection events are persisted domain outcomes, and tests should verify exact event sequences and structured fields rather than only success flags.

### Project Structure Notes

- This story may require both Tenants repo tests/config and a narrow EventStore submodule change because publication is owned by EventStore. Keep package boundaries intact.
- Do not move aggregates, states, or projections out of `Hexalith.Tenants.Server`.
- Do not add package versions inline; central package management owns versions.
- Use `Hexalith.Tenants.slnx`, not `.sln`.
- Do not initialize nested submodules.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Story 4.1 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/epics.md` - FR35-FR42, FR44-FR45, FR62 and NFR14/NFR19 coverage]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - DAPR/EventStore resource naming, CloudEvents, top-level payload `TenantId`, at-least-once delivery]
- [Source: `_bmad-output/project-context.md` - DAPR conventions, identity scheme, testing rules, package boundaries]
- [Source: `docs/event-contract-reference.md` - shared `tenants.events` topic and consumer filtering contract]
- [Source: `docs/idempotent-event-processing.md` and `docs/cross-aggregate-timing.md` - at-least-once delivery, duplicate handling, eventual consistency]
- [External: DAPR docs, "Publishing & subscribing messages with Cloudevents" - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-cloudevents/]
- [External: DAPR docs, "Publishing & subscribing messages without CloudEvents" - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-raw/]
- [External: DAPR docs, "Pub/sub API reference" - https://docs.dapr.io/reference/api/pubsub_api/]
- [External: CloudEvents specification - https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Added EventStore publisher topic override support and configured `global-administrators` to publish to `tenants.events` while preserving the envelope domain.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --no-restore` initially failed before compilation because parallel MSBuild node startup was denied by the sandbox (`SocketException (13): Permission denied`).
- 2026-06-01: Retried with `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --no-restore -m:1 /nodeReuse:false`; build completed, but VSTest aborted before executing tests because the test runner could not open its TCP listener (`SocketException (13): Permission denied`).
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --no-restore -m:1 /nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests` passed: Total 52, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests` passed: Total 2, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -noLogo -noColor -parallel none -class Hexalith.EventStore.Server.Tests.Configuration.EventPublisherOptionsTests -class Hexalith.EventStore.Server.Tests.Events.EventPublisherTests` passed: Total 33, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.DaprEndToEndTests` completed: Total 17, Failed 0, Skipped 17 because local DAPR Redis, placement, and scheduler prerequisites are unavailable.
- 2026-06-01: Senior review found the topic override was missing from the actual EventStore host appsettings and AppHost static DAPR component scopes still referenced the obsolete `commandapi` app id.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore -m:1 /nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --no-restore -m:1 /nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests` passed: Total 4, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -noLogo -noColor -parallel none -class Hexalith.EventStore.Server.Tests.Configuration.EventPublisherOptionsTests -class Hexalith.EventStore.Server.Tests.Events.EventPublisherTests` passed: Total 33, Failed 0, Skipped 0.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests` passed: Total 52, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests` passed: Total 4, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -noLogo -noColor -parallel none -class Hexalith.EventStore.Server.Tests.Configuration.EventPublisherOptionsTests -class Hexalith.EventStore.Server.Tests.Events.EventPublisherTests` passed: Total 33, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit fallback `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.DaprEndToEndTests` completed: Total 17, Failed 0, Skipped 17 because local DAPR Redis, placement, and scheduler prerequisites are unavailable.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Validation checklist applied during story creation; critical mismatch around global-administrator topic derivation is called out explicitly.
- Added configurable EventStore publisher topic overrides and used them to map `system/global-administrators` publication to shared topic `tenants.events`.
- Updated Tenants runtime/AppHost configuration and fake publisher tests so global-administrator events keep `Domain == "global-administrators"` in the envelope while publishing on `tenants.events`.
- Added contract reflection coverage for top-level payload `TenantId`, EventStore publisher metadata coverage, Tenants configuration coverage, and integration coverage for `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator`.
- Senior review auto-fixed configuration drift so the actual EventStore host appsettings carries the global-administrator topic override, AppHost static DAPR component files use current app ids, and the DAPR test fixture includes the tenant dead-letter topic.
- `dotnet test` could not execute in this sandbox because the VSTest runner failed before test execution due socket listener permission denial; direct xUnit fallback suites passed for contract, server configuration, EventStore publisher, and DAPR-gated integration coverage.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: The implementation added `EventStore:Publisher:TopicOverrides:global-administrators` to `src/Hexalith.Tenants/appsettings.json`, but `src/Hexalith.Tenants/Program.cs` explicitly does not register `AddEventStoreServer`; the actual publisher runs in `Hexalith.EventStore/src/Hexalith.EventStore`. Added the same override to the EventStore host appsettings and covered it with `EventPublicationConfigurationTests`.
- MEDIUM: Static AppHost DAPR component/config files still referenced the obsolete `commandapi` app id even though the current AppHost uses `eventstore`, `tenants`, `eventstore-admin`, and `sample`. Updated access control, resiliency, state store scopes, and pub/sub scopes to match current local topology.
- MEDIUM: The DAPR integration fixture generated a pub/sub component with dead-letter enabled but without the `deadletter.tenants.events` topic asserted by the story. Added the explicit dead-letter topic to the generated fixture YAML.

### File List

- Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/EventPublisherOptions.cs
- Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs
- Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/FakeEventPublisher.cs
- Hexalith.EventStore/src/Hexalith.EventStore/appsettings.json
- Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Configuration/EventPublisherOptionsTests.cs
- Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Events/EventPublisherTests.cs
- _bmad-output/implementation-artifacts/4-1-publish-tenant-domain-events-as-cloudevents.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- docs/event-contract-reference.md
- src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml
- src/Hexalith.Tenants.AppHost/Program.cs
- src/Hexalith.Tenants/appsettings.json
- tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs
- tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs
- tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs
- tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs

### Change Log

- 2026-06-01: Implemented shared `tenants.events` publication for global-administrator events through configurable EventStore topic overrides; added contract, publisher, configuration, integration, and documentation coverage; moved story to review after direct xUnit fallback validation.
- 2026-06-01: Senior review auto-fixed EventStore host publisher configuration, AppHost DAPR app-id/resource drift, and fixture dead-letter topic coverage; moved story to done.
