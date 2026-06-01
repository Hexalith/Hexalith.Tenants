---
baseline_commit: 15e3d69
---

# Story 4.5: React to Tenant Access, Lifecycle, and Configuration Changes

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want tenant events to trigger access, availability, and configuration reactions,
so that downstream services update behavior automatically when tenant state changes.

## Acceptance Criteria

1. Given a `UserAddedToTenant` event is processed, when the consuming service updates its local state, then the user can be granted the role-specific local capability represented by that service, and the projection records enough event metadata for idempotent processing.
2. Given a `UserRemovedFromTenant` event is processed, when the consuming service updates its local state, then the user's local tenant access is revoked, and repeated delivery of the removal event does not produce an error or duplicate side effect.
3. Given a `TenantDisabled` event is processed, when the consuming service evaluates tenant operations, then tenant operations are blocked or degraded according to the consuming service policy, and the behavior is documented as eventually consistent with the tenant event stream.
4. Given a `TenantEnabled` event is processed, when the consuming service evaluates tenant operations, then normal tenant operations can resume after the local projection reflects the event.
5. Given a tenant configuration set or removed event is processed, when the consuming service reads tenant-specific configuration from its local projection, then namespaced configuration keys are applied or removed deterministically, and unrelated service namespaces are ignored unless explicitly handled.
6. Given reaction tests run, when access, lifecycle, and configuration event sequences are processed, then tests prove the consuming service reacts without custom polling, sync jobs, or per-service Tenants API calls.

## Tasks / Subtasks

- [x] Task 1: Add projection metadata needed for consumer reactions and idempotency diagnostics (AC: #1, #2)
  - [x] Extend `TenantLocalState` with bounded last-event metadata such as `LastMessageId`, `LastSequenceNumber`, `LastUpdatedAt`, and `LastCorrelationId`, or a small value object with those fields.
  - [x] Populate that metadata in `TenantProjectionEventHandler.ApplyAsync` from `TenantEventContext` after each successful apply.
  - [x] Preserve clone-on-read/write behavior in `TenantLocalState.Clone()` and `InMemoryTenantProjectionStore`.
  - [x] Do not persist an unbounded processed-message collection in `TenantLocalState`; `TenantEventProcessor` remains responsible for message-id deduplication.

- [x] Task 2: Harden access and lifecycle reactions through the existing local projection (AC: #1, #2, #3, #4, #6)
  - [x] Reuse `TenantProjectionEventHandler`, `TenantEventProcessor`, `ITenantProjectionStore`, and `AccessCheckEndpoints`; do not create a second projection pipeline.
  - [x] Ensure `UserAddedToTenant`, `UserRoleChanged`, and `UserRemovedFromTenant` produce deterministic grant, role-change, and revoke behavior from local state.
  - [x] Ensure `TenantDisabled` causes sample tenant operations to deny or degrade while preserving the last projected membership.
  - [x] Ensure `TenantEnabled` resumes normal sample operations only after the local projection reflects the enable event.
  - [x] Keep fail-closed defaults: `TenantStatus.Unknown`, `TenantRole.Unknown`, out-of-range roles, missing tenants, and missing users must not grant access.

- [x] Task 3: Add a configuration reaction sample without leaking unrelated namespaces (AC: #5, #6)
  - [x] Add a small sample endpoint or helper under `samples/Hexalith.Tenants.Sample/Endpoints/` that reads tenant-specific configuration from `ITenantProjectionStore`.
  - [x] Use an explicit service namespace/prefix, for example `sample.`; apply matching keys and ignore or hide unrelated keys such as `billing.plan` unless the sample explicitly asks for that namespace.
  - [x] Prove `TenantConfigurationSet` adds or updates the local value deterministically.
  - [x] Prove `TenantConfigurationRemoved` removes the local value and repeated removal delivery is harmless.
  - [x] Do not call the Tenants API synchronously to read configuration.

- [x] Task 4: Keep sample event registration and logging aligned with the reaction story (AC: #1-#6)
  - [x] Register any additional sample handlers only through `AddTenantEventHandler<TEvent, THandler>()`.
  - [x] If `SampleLoggingEventHandler` is extended, keep logs bounded to metadata such as tenant ID, message ID, event type, and correlation ID.
  - [x] Do not log full payloads, user identifiers beyond what already exists in support-safe context, role values, or configuration values.
  - [x] Keep DAPR integration on `MapTenantEventSubscription()` and the shared `tenants.events` topic.

- [x] Task 5: Update consumer documentation for reaction behavior (AC: #3, #5, #6)
  - [x] Update `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md`, and/or `docs/quickstart.md` where reaction guidance is incomplete.
  - [x] State that add/remove/role/lifecycle/configuration reactions come from the consuming service's local projection.
  - [x] State that lifecycle and configuration reactions are eventually consistent with the tenant event stream.
  - [x] State that configuration keys are dot-delimited by convention and consumers should filter by owned namespace/prefix.
  - [x] State that no polling, sync job, or per-request Tenants API call is required for the sample reaction path.

- [x] Task 6: Add focused Tier 1 and sample tests (AC: #1-#6)
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs` for metadata capture and configuration set/remove idempotency.
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs` for representative access, lifecycle, configuration, duplicate-removal, and duplicate-configuration sequences if coverage is missing.
  - [x] Extend `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs` for any missing role-change, disable/enable, and repeated-removal behavior.
  - [x] Add sample configuration endpoint/helper tests proving namespace filtering, set, update, remove, unrelated-key ignore, and no synchronous Tenants API dependency.
  - [x] Use xUnit v3 and Shouldly. Do not add `using Xunit;`; tests inherit it from `tests/Directory.Build.props`.

- [x] Task 7: Verification (AC: #1-#6)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.
  - [x] Run `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.
  - [x] If VSTest fails before execution because the sandbox denies listener creation, build the projects and run the direct xUnit test assemblies as recorded in Story 4.4.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.

## Dev Notes

### Scope

This story extends the Client and sample consumer path built in Stories 4.2 through 4.4. It is not a new Tenants server feature, not a production authorization plugin, and not a new broker abstraction.

The implementation should prove that a consuming service can react to projected state changes already flowing through `TenantEventProcessor` and `TenantProjectionEventHandler`:

- access grant/revoke from membership events,
- tenant operation denial/degradation from lifecycle events,
- tenant operation recovery after enable events,
- namespaced local configuration reads from configuration events,
- bounded event metadata available for diagnostics and idempotency evidence.

Do not add polling, sync jobs, or per-request calls back to the Tenants host. EventStore remains the durable source of truth; the consumer projection is local runtime state and is eventually consistent.

### Current State To Inspect

- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
  - Holds `TenantId`, `Name`, `Description`, `Status`, `Members`, and `Configuration`.
  - `Status` defaults to `TenantStatus.Unknown`; preserve this fail-closed default.
  - `Members` and `Configuration` use `StringComparer.Ordinal`; preserve case-sensitive keys.
  - `Clone()` must copy any new metadata fields.
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
  - Already handles `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`.
  - Uses per-tenant `SemaphoreSlim` locks and idempotent property assignment / dictionary set / dictionary remove operations.
  - This is the right place to stamp last-event metadata from `TenantEventContext`.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
  - Deduplicates by `MessageId`, deserializes with `System.Text.Json`, creates a DI scope, dispatches typed handlers, and removes a message ID when handler execution fails so redelivery can retry.
  - Do not change retry semantics.
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
  - Reads `ITenantProjectionStore` and denies unknown tenants, disabled tenants, unknown roles, out-of-range roles, and non-members.
  - Keep it projection-backed only.
- `samples/Hexalith.Tenants.Sample/Program.cs`
  - Uses `AddHexalithTenants()`, `AddTenantEventHandler<TEvent, THandler>()`, `MapTenantEventSubscription()`, and `MapAccessCheckEndpoints()`.
  - Any new sample endpoint should be mapped here.

### Architecture Guardrails

- Client-facing projection and event-processing code belongs under `src/Hexalith.Tenants.Client`.
- Sample reaction proof belongs under `samples/Hexalith.Tenants.Sample` and `samples/Hexalith.Tenants.Sample.Tests`.
- Do not modify `Hexalith.Tenants.Server`, host query controllers, AppHost topology, DAPR component YAML, or the EventStore submodule for this story unless implementation finds an in-scope defect.
- DAPR remains the pub/sub abstraction. Do not introduce Kafka, RabbitMQ, Redis, SQL, or direct database APIs into Client.
- Shared topic remains `tenants.events`; consumers filter by registered event payload type.
- Keep JSON on `System.Text.Json`; do not add Newtonsoft.Json.
- Do not change package versions or add inline `PackageReference Version=` values.
- Use `Hexalith.Tenants.slnx`, not `.sln`.

### Event Semantics

Access and role reactions:

- `UserAddedToTenant` sets or replaces the user's role for the tenant.
- `UserRoleChanged` replaces the user's role with `NewRole`.
- `UserRemovedFromTenant` removes the user from the tenant.
- Reapplying the same add/change/remove payload must not create duplicate records or non-deterministic state.
- Removed users must not appear authorized after the removal event is processed.

Lifecycle reactions:

- `TenantDisabled` sets local status to `Disabled`; sample tenant operations should deny or degrade from that projected state.
- `TenantEnabled` sets local status to `Active`; sample tenant operations may resume only once the projection reflects the event.
- A tenant with `Unknown` status must not be treated as active.
- Membership state should not be erased by disable/enable events; lifecycle controls availability, not membership history.

Configuration reactions:

- `TenantConfigurationSet` writes `Configuration[Key] = Value`.
- `TenantConfigurationRemoved` removes `Configuration[Key]`.
- Configuration keys are preserved exactly and are dot-delimited by convention, not by enforced regex.
- Consumers should filter by their owned namespace/prefix, such as `sample.` or `billing.`. Unrelated namespaces must be ignored unless explicitly handled.
- Do not log configuration values.

Ordering and consistency:

- DAPR pub/sub is at-least-once. Duplicate delivery is expected.
- Consumers process events independently. Do not assume two consuming services observe the same event at the same time.
- Use metadata such as `MessageId`, `SequenceNumber`, `Timestamp`, and `CorrelationId` for diagnostics and idempotency evidence. Do not invent global ordering across services.
- Local projection reads are eventually consistent with tenant commands. This is acceptable for the sample; a synchronous authorization plugin is a future concern.

### Previous Story Intelligence

Story 4.4 completed the local projection foundation:

- `TenantLocalState`, `ITenantProjectionStore`, `InMemoryTenantProjectionStore`, `TenantProjectionEventHandler`, and `TenantEventProcessor` are the existing implementation path.
- The sample access endpoint already proves projection-backed grant, revoke, disable, enable, unknown status, unknown role, and out-of-range role behavior.
- Duplicate delivery is handled at two levels: `TenantEventProcessor` deduplicates by `MessageId`, and projection handlers use idempotent set/remove operations.
- Scaled-out consumers should replace `InMemoryTenantProjectionStore` with a durable `ITenantProjectionStore`; Client must not take a Redis/database dependency.
- VSTest can fail in the sandbox because listener creation is denied. Story 4.4 used direct xUnit assembly execution after successful build as the fallback.

Stories 4.1 through 4.3 established these constraints:

- Defaults are `PubSubName = "pubsub"` and `TopicName = "tenants.events"`.
- Tenant and global-administrator events publish to the shared `tenants.events` topic.
- `AddTenantEventHandler<TEvent, THandler>()` is the public selected-handler registration helper.
- Custom handlers are resolved through a per-event DI scope, so scoped dependencies are allowed.
- Logs must stay support-safe and avoid full event payloads.

### Git Intelligence

Recent relevant commits:

- `15e3d69 feat(story-4.4): Build Local Consumer Projection from Tenant Events`
- `a51c5bd feat(story-4.3): Register Tenant Event Handlers in Under Twenty Lines`
- `17879ed feat(story-4.2): Expose Consumer DI Registration for Tenant Client Services`
- `557de8d feat(story-4.1): Publish Tenant Domain Events as CloudEvents`

The recent pattern is narrow Client/sample/documentation work with focused Tier 1 tests, direct xUnit fallback when VSTest cannot run, and Release solution build verification.

### Files To Inspect Or Update

- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
- `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs`
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
- `samples/Hexalith.Tenants.Sample/Endpoints/*Configuration*.cs` if a new endpoint/helper is added
- `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`
- `samples/Hexalith.Tenants.Sample/Program.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/*Configuration*.cs` if a new endpoint/helper is added
- `docs/event-contract-reference.md`
- `docs/idempotent-event-processing.md`
- `docs/quickstart.md`

### Testing Standards

- Use xUnit v3 and Shouldly.
- Do not use `Assert.*`.
- Do not add per-file `using Xunit;`.
- Keep this story Tier 1/sample-test focused. Do not require live DAPR sidecars.
- Prefer representative event sequences over excessive single-property tests.
- Every new test must contain a meaningful Shouldly assertion.
- Keep test method names in the existing snake_case/PascalCase style where that file already uses it; otherwise match the surrounding file.

### Latest Technical Notes

- Current DAPR pub/sub documentation still states at-least-once delivery semantics and broker-agnostic pub/sub APIs. This supports duplicate-delivery tests and idempotent handler requirements.
- Current DAPR topic scoping documentation describes `allowedTopics`, `publishingScopes`, and `subscriptionScopes`. Do not change component scopes in this story, but keep the sample compatible with `pubsub` and `tenants.events`.
- Current DAPR .NET SDK documentation continues to list `Dapr.AspNetCore` as the ASP.NET Core integration package. Keep using the pinned DAPR package family already in `Directory.Packages.props`.

### Project Structure Notes

- This story should primarily touch Client, Client.Tests, sample, sample tests, and docs.
- There are older duplicate Epic 4 artifacts from a previous planning pass. Use the active sprint status key `4-5-react-to-tenant-access-lifecycle-and-configuration-changes`.
- Do not initialize nested submodules.
- Do not alter the unrelated modified `_bmad-output/story-automator/orchestration-1-20260531-113112.md`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.5 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` - FR37-FR42, FR62, NFR5, NFR10, NFR19]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Client/Handlers, Client/Subscription, consumer projection boundaries, DAPR resource names]
- [Source: `_bmad-output/project-context.md` - DAPR conventions, Client package boundaries, testing rules, package/version constraints]
- [Source: `_bmad-output/implementation-artifacts/4-4-build-a-local-consumer-projection-from-tenant-events.md` - local projection implementation, duplicate delivery, sample access behavior, VSTest fallback]
- [Source: `docs/event-contract-reference.md` - configuration namespace convention and event contract semantics]
- [Source: `docs/idempotent-event-processing.md` - local projection semantics and production deduplication considerations]
- [External: DAPR docs, Pub/sub overview - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- [External: DAPR docs, Pub/sub topic scoping - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/]
- [External: DAPR docs, .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Red-phase `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` failed at compile as expected because `TenantLocalState.LastEvent` did not exist.
- 2026-06-01: Red-phase `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` failed at compile as expected because `TenantConfigurationEndpoints` did not exist.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Direct xUnit Debug Client run `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 86, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Debug Sample run `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` passed: Total 28, Failed 0, Skipped 0.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit Release Client run passed: Total 86, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Release Sample run passed: Total 28, Failed 0, Skipped 0.
- 2026-06-01: Review `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Review direct xUnit Debug Client run passed: Total 89, Failed 0, Skipped 0.
- 2026-06-01: Review `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Review direct xUnit Debug Sample run passed: Total 29, Failed 0, Skipped 0.
- 2026-06-01: Review `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Review direct xUnit Release Client run passed: Total 89, Failed 0, Skipped 0.
- 2026-06-01: Review direct xUnit Release Sample run passed: Total 29, Failed 0, Skipped 0.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added bounded last-event metadata to the local tenant projection without changing `TenantEventProcessor` message-id deduplication semantics.
- Reused the existing projection processor/store/access endpoint path for access, role-change, disable/enable, and repeated-removal reactions.
- Added a sample `sample.` namespace configuration endpoint backed only by `ITenantProjectionStore`, filtering out unrelated configuration namespaces.
- Updated consumer documentation to describe local projection reactions, eventual consistency, owned namespace filtering, and no polling/sync/API-call requirement.
- Review auto-fix added fail-closed validation that event payload `TenantId` must match the envelope aggregate tenant before dispatching handlers.

### File List

- `_bmad-output/implementation-artifacts/4-5-react-to-tenant-access-lifecycle-and-configuration-changes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `docs/event-contract-reference.md`
- `docs/idempotent-event-processing.md`
- `docs/quickstart.md`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs`
- `samples/Hexalith.Tenants.Sample/Program.cs`
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
- `src/Hexalith.Tenants.Client/Projections/TenantProjectionEventMetadata.cs`
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`

### Senior Developer Review (AI)

Reviewer: Jerome on 2026-06-01

Outcome: Approved after auto-fix.

Findings:

- [x] [AI-Review][High] `TenantEventProcessor` accepted a deserialized tenant event without verifying that payload `TenantId` matched the envelope aggregate ID. A malformed broker message could dispatch handlers with cross-tenant payload/context disagreement. Fixed by rejecting mismatches as `FailedInvalidPayload`, removing the message ID from the dedup cache for retry, and adding a regression test that proves no projection is written before corrected redelivery.
- [x] [AI-Review][Medium] Story File List missed changed review-relevant files. Added `_bmad-output/implementation-artifacts/tests/test-summary.md`, `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`, and `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs`.
- [x] [AI-Review][Low] Test evidence was stale after the review fix. Updated test-summary counts and review validation notes to the current 89 Client tests / 29 Sample tests.

Validation:

- Web fallback documentation check performed for DAPR pub/sub overview, topic scoping, and .NET SDK docs. Current docs still support at-least-once pub/sub delivery, topic scoping with `allowedTopics`/`publishingScopes`/`subscriptionScopes`, and `Dapr.AspNetCore` for ASP.NET Core services.
- `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- Direct xUnit Debug Client run `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 89, Failed 0, Skipped 0.
- `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- Direct xUnit Debug Sample run `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` passed: Total 29, Failed 0, Skipped 0.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- Direct xUnit Release Client run passed: Total 89, Failed 0, Skipped 0.
- Direct xUnit Release Sample run passed: Total 29, Failed 0, Skipped 0.

### Change Log

- 2026-06-01: Implemented Story 4.5 reaction behavior, tests, documentation, and verification; story ready for review.
- 2026-06-01: Senior Developer Review auto-fixed payload/envelope tenant mismatch validation, updated evidence, and approved story.
