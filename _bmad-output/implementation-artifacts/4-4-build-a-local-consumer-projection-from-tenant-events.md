---
baseline_commit: a51c5bd
---

# Story 4.4: Build a Local Consumer Projection from Tenant Events

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want to build a local projection from tenant events,
so that my service can enforce tenant-aware behavior using its own runtime state.

## Acceptance Criteria

1. Given a consuming service subscribes to tenant events, when `TenantCreated`, `TenantUpdated`, `TenantDisabled`, and `TenantEnabled` events are received, then the local projection can maintain tenant lifecycle state, and it does not query Tenants synchronously for every consuming-service decision.
2. Given membership events are received, when users are added, removed, or assigned new roles, then the local projection can maintain user-to-tenant role state, and removed users no longer appear as authorized members after projection processing.
3. Given events may be delivered at least once, when the same event is received more than once, then the projection handles the duplicate idempotently, and no duplicate memberships, role transitions, or lifecycle records are created.
4. Given events from different services or subscriptions may arrive at different times, when a consuming service projection is updated, then the implementation and documentation do not assume cross-service ordering, and consumers are guided to design for eventual consistency.
5. Given projection tests run in the sample or client test suite, when representative lifecycle and membership event sequences are applied, then tests verify deterministic local projection state and idempotent duplicate handling.

## Tasks / Subtasks

- [x] Task 1: Complete and harden the built-in local projection model (AC: #1, #2, #3)
  - [x] Reuse `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`; do not create a parallel local-state model.
  - [x] Verify lifecycle state is deterministic for `TenantCreated`, `TenantUpdated`, `TenantDisabled`, and `TenantEnabled`.
  - [x] Verify membership state is deterministic for `UserAddedToTenant`, `UserRemovedFromTenant`, and `UserRoleChanged`.
  - [x] Preserve fail-closed defaults: unknown tenant status must not read as active, and missing/unknown role must not grant access.
  - [x] Preserve case-sensitive user and tenant identifiers. Do not switch dictionaries to `OrdinalIgnoreCase`.

- [x] Task 2: Preserve projection storage boundaries and consumer extensibility (AC: #1, #2)
  - [x] Reuse `ITenantProjectionStore` as the consumer-facing storage seam and `InMemoryTenantProjectionStore` as the default single-instance implementation.
  - [x] Keep `AddHexalithTenants()` from overwriting a consumer-provided `ITenantProjectionStore`.
  - [x] Document or test that scaled-out consumers should replace the default in-memory store with a durable implementation, without adding a concrete Redis/database dependency to Client.
  - [x] Do not introduce synchronous calls back to the Tenants host for access decisions; the consuming service should read its local projection.

- [x] Task 3: Prove idempotent duplicate handling (AC: #3, #5)
  - [x] Extend `TenantEventProcessor` tests to show duplicate delivery with the same `MessageId` returns `Duplicate` and does not mutate projection state a second time.
  - [x] Extend `TenantProjectionEventHandler` tests to show applying the same lifecycle and membership payload twice remains state-equivalent.
  - [x] If a test uses different message IDs for the same payload, treat the result as handler-level idempotency; do not rely on processor-level message deduplication alone.
  - [x] Keep failure behavior unchanged: failed handler execution removes the message ID so DAPR redelivery can retry.

- [x] Task 4: Update sample access enforcement around the local projection (AC: #1, #2, #4, #5)
  - [x] Reuse `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs` as the demonstration endpoint unless implementation finds a concrete defect.
  - [x] Verify access is granted from the local projection after `TenantCreated` plus `UserAddedToTenant`.
  - [x] Verify access is denied after `UserRemovedFromTenant`.
  - [x] Verify access is denied when the tenant is disabled and restored appropriately when enabled, if the sample surface exposes that scenario.
  - [x] Keep the endpoint as a sample of projection-based decision making only; do not turn it into a production authorization plugin.

- [x] Task 5: Document local projection semantics for consumers (AC: #1, #3, #4)
  - [x] Update `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md`, and/or `docs/quickstart.md` where the local projection guidance is incomplete or stale.
  - [x] State that `tenants.events` is a shared topic and consumers filter by event type through typed handlers.
  - [x] State that DAPR pub/sub is at-least-once and consumers must be idempotent.
  - [x] State that different consuming services process events independently and must not assume cross-service ordering or immediate read-after-write visibility.
  - [x] Explain that the local projection is runtime state for the consuming service, while EventStore remains the durable source of truth.

- [x] Task 6: Add focused tests without live infrastructure (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs` for full lifecycle, membership, duplicate, and tenant-isolation sequences.
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs` for duplicate message processing and deterministic projection state after representative event sequences.
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs` only if store behavior needs additional proof.
  - [x] Extend `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs` for projection-backed deny/grant behavior where sample coverage is missing.
  - [x] Use xUnit v3 and Shouldly. Do not add `using Xunit;`; tests inherit it from `tests/Directory.Build.props`.

- [x] Task 7: Verification (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore` if the sandbox permits VSTest.
  - [x] If VSTest fails before execution because of sandbox listener restrictions, build the project and use the direct xUnit assembly fallback pattern recorded in Story 4.3.
  - [x] Run `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore` or the same direct xUnit fallback.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.

## Dev Notes

### Scope

This story is not a greenfield projection implementation. Story 4.2 and Story 4.3 already established the Client registration and event-processing path:

- `AddHexalithTenants()` registers the default projection store, built-in projection event handler, event type registry, and `TenantEventProcessor`.
- `TenantEventProcessor` resolves `TenantEventEnvelope.EventTypeName`, deserializes the payload with `System.Text.Json`, deduplicates by `MessageId`, and dispatches through scoped DI.
- `TenantProjectionEventHandler` is already the built-in handler for lifecycle, membership, role, and configuration events.
- `AccessCheckEndpoints` in the sample already demonstrates projection-backed access decisions.

Use those types and make the story about completeness, correctness, tests, and consumer guidance. Do not add a second event-processing abstraction, broker-specific API, host callback, or sample-only projection model.

### Current State To Inspect

- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
  - Holds `TenantId`, `Name`, `Description`, `Status`, `Members`, and `Configuration`.
  - `Status` defaults to `TenantStatus.Unknown`, which must remain fail-closed.
  - `Members` uses `StringComparer.Ordinal`; preserve case-sensitive identifiers.
- `src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`
  - Consumer-facing abstraction for projection storage.
  - Keep this interface as the extension point for durable consumer stores.
- `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs`
  - Thread-safe in-memory default using clone-on-read/write.
  - Suitable for local and single-instance sample use, not durable scaled-out state.
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
  - Applies lifecycle, membership, role, and configuration events to `TenantLocalState`.
  - Uses per-tenant `SemaphoreSlim` locks to serialize updates within one process.
  - Uses idempotent operations: property assignment, dictionary set, and dictionary remove.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
  - Deduplicates by `MessageId` and removes the message ID if handler execution fails.
  - Creates a DI scope per event so scoped handler dependencies are supported.
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
  - Reads `ITenantProjectionStore` and grants/denies access from local projected state.

### Architecture Guardrails

- Client-facing projection and event-processing code belongs under `src/Hexalith.Tenants.Client`.
- Sample integration proof belongs under `samples/Hexalith.Tenants.Sample` and `samples/Hexalith.Tenants.Sample.Tests`.
- Do not modify `Hexalith.Tenants.Server`, host controllers, AppHost topology, DAPR component YAML, or the EventStore submodule for this story unless a test exposes an in-scope defect.
- DAPR remains the pub/sub abstraction. Do not introduce Kafka, RabbitMQ, Redis, SQL, or direct database APIs into Client.
- Shared topic remains `tenants.events`; consumers filter by registered event payload type.
- EventStore remains the durable source of truth. The consumer projection is local runtime state for fast tenant-aware decisions.
- Do not query Tenants synchronously from the sample access endpoint. That would defeat FR37's local projection model.
- Keep JSON on `System.Text.Json`; do not add Newtonsoft.Json.
- Do not log full payloads. Keep diagnostics to metadata such as message ID, event type, tenant ID, sequence number, and correlation ID.

### Event Semantics

Lifecycle projection requirements:

- `TenantCreated` sets display name, description, and active status.
- `TenantUpdated` updates display name and description without changing membership.
- `TenantDisabled` sets disabled status.
- `TenantEnabled` restores active status.
- A tenant that has not seen `TenantCreated` should never be treated as active by default.

Membership projection requirements:

- `UserAddedToTenant` sets the user's role for the tenant.
- `UserRoleChanged` replaces the user's role with the new role.
- `UserRemovedFromTenant` removes the user from the tenant.
- Removed users must not appear authorized after the removal event has been processed.
- Reapplying the same add/change/remove payload must not create duplicate records or non-deterministic state.

Ordering and consistency requirements:

- DAPR pub/sub is at-least-once. Duplicate delivery is expected.
- Consumers process events independently. Do not assume two consuming services observe the same event at the same time.
- Within one aggregate stream, use event metadata such as `MessageId`, `SequenceNumber`, and `CorrelationId` for diagnostics and deduplication. Do not invent global ordering across services.
- Access checks backed by a local projection are eventually consistent with tenant commands. This is acceptable for the sample and consumer guidance; the planned authorization plugin is a separate future concern.

### Previous Story Intelligence

Story 4.3 completed concise handler registration and produced these implementation constraints:

- `AddTenantEventHandler<TEvent, THandler>()` is the public selected-handler registration helper.
- Custom handlers are resolved through an event scope, so scoped dependencies are allowed.
- Built-in projection handlers continue to process all supported tenant events.
- `TenantEventProcessor` should dispatch selected custom handlers only for matching event payload types.
- Duplicate message handling and retry semantics already exist; do not break them while adding tests.
- Sample logging was tightened to avoid logging payload user identifiers and role values; keep payloads out of logs.
- VSTest may be blocked by sandbox socket/listener restrictions. Story 4.3 used direct xUnit assembly execution after a successful build.

Story 4.2 and 4.1 added these relevant constraints:

- Defaults are `PubSubName = "pubsub"` and `TopicName = "tenants.events"`.
- `HexalithTenantsOptions.ConfigurationSectionName` is `Tenants`.
- The current app IDs are `eventstore`, `tenants`, `eventstore-admin`, and `sample`; do not reintroduce stale `commandapi` defaults.
- Tenant and global-administrator events publish to the shared `tenants.events` topic.

### Git Intelligence

Recent relevant commits:

- `a51c5bd feat(story-4.3): Register Tenant Event Handlers in Under Twenty Lines`
- `17879ed feat(story-4.2): Expose Consumer DI Registration for Tenant Client Services`
- `557de8d feat(story-4.1): Publish Tenant Domain Events as CloudEvents`

The recent pattern is narrow Client/sample/documentation work with focused descriptor and processor tests, direct xUnit fallback when VSTest cannot run, and Release solution build verification.

### Files To Inspect Or Update

- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
- `src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs`
- `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs`
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs`
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `docs/event-contract-reference.md`
- `docs/idempotent-event-processing.md`
- `docs/quickstart.md`

### Testing Standards

- Use xUnit v3 and Shouldly.
- Do not use `Assert.*`.
- Do not add per-file `using Xunit;`.
- Keep this story Tier 1/sample-test focused. Do not require live DAPR sidecars.
- Prefer representative event sequences over excessive single-property tests.
- Include duplicate-delivery proof through `TenantEventProcessor` and handler-level idempotency proof where useful.
- Every new test must contain a meaningful Shouldly assertion.

### Latest Technical Notes

- Current DAPR docs still define pub/sub as platform-agnostic messaging with at-least-once delivery; this directly supports the duplicate-delivery requirements in AC3.
- Current DAPR .NET SDK docs list .NET 10 support and `Dapr.AspNetCore` as the ASP.NET Core integration package; keep using the pinned `Dapr.AspNetCore` package family already in `Directory.Packages.props`.
- Current DAPR topic scoping docs describe component/topic access controls. Do not change component scopes in this story, but keep the names compatible with `pubsub` and `tenants.events`.

### Project Structure Notes

- This story should primarily touch Client, Client.Tests, sample, sample tests, and docs.
- The active sprint key is `4-4-build-a-local-consumer-projection-from-tenant-events`.
- There are older duplicate Epic 4 artifacts from a previous planning pass. Use the active sprint status key and this story file as the source for implementation.
- Do not initialize nested submodules.
- Use `Hexalith.Tenants.slnx`, not `.sln`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.4 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` - FR35-FR45, FR62, NFR14-NFR19]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Client/Handlers, Client/Subscription, DAPR resource names, consumer projection boundaries]
- [Source: `_bmad-output/project-context.md` - DAPR conventions, Client package boundaries, testing rules, package/version constraints]
- [Source: `_bmad-output/implementation-artifacts/4-3-register-tenant-event-handlers-in-under-twenty-lines.md` - handler registration, scoped dispatch, sample logging, test fallback learning]
- [Source: `_bmad-output/implementation-artifacts/4-2-expose-consumer-di-registration-for-tenant-client-services.md` - Client registration defaults and idempotency]
- [Source: `_bmad-output/implementation-artifacts/4-1-publish-tenant-domain-events-as-cloudevents.md` - shared event topic and app ID correction]
- [Source: `docs/event-contract-reference.md` - event delivery model, shared topic, CloudEvents, event metadata]
- [Source: `docs/idempotent-event-processing.md` - message-level deduplication and handler idempotency guidance]
- [External: DAPR docs, Pub/sub overview - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- [External: DAPR docs, .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/]
- [External: DAPR docs, Pub/sub topic scoping - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built Debug successfully, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Direct xUnit Debug Client run `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 84, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Debug Sample run before endpoint fix failed as expected: Total 21, Failed 2 (`UnknownTenantStatus` and `UnknownRole` granted access).
- 2026-06-01: Direct xUnit Debug Sample run after endpoint fix passed: Total 21, Failed 0, Skipped 0.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit Release Client run passed: Total 84, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Release Sample run passed: Total 21, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Release Contracts run passed: Total 101, Failed 0, Skipped 0.
- 2026-06-01: Direct xUnit Release Testing run passed: Total 99, Failed 0, Skipped 0.
- 2026-06-01: Review reran `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`; build succeeded, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Review reran `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`; build succeeded, then VSTest aborted before execution because the sandbox denied TCP listener creation (`SocketException (13): Permission denied`).
- 2026-06-01: Review direct xUnit Debug Client run passed: Total 84, Failed 0, Skipped 0.
- 2026-06-01: Review rebuilt the sample tests after auto-fix: `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Review direct xUnit Debug Sample run passed after auto-fix: Total 22, Failed 0, Skipped 0.
- 2026-06-01: Review `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Review direct xUnit Release Client run passed: Total 84, Failed 0, Skipped 0.
- 2026-06-01: Review direct xUnit Release Sample run passed after auto-fix: Total 22, Failed 0, Skipped 0.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Reused the existing Client local projection model/store/handler path; no parallel projection model, broker abstraction, or synchronous Tenants callback was introduced.
- Hardened sample access decisions to fail closed unless the local projection has an explicitly active tenant and a known user role.
- Added deterministic lifecycle, membership, duplicate-delivery, case-sensitive identifier, custom store boundary, and sample projection-backed access tests.
- Documented local projection semantics, shared `tenants.events` filtering, at-least-once delivery, independent consumer processing, eventual consistency, and EventStore source-of-truth boundaries.
- Senior review auto-fix tightened the sample endpoint role check to allow-list only known tenant roles, so corrupted or out-of-range projection role values fail closed.

### File List

- `_bmad-output/implementation-artifacts/4-4-build-a-local-consumer-projection-from-tenant-events.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md`
- `docs/event-contract-reference.md`
- `docs/idempotent-event-processing.md`
- `docs/quickstart.md`
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix. No critical issues remain.

Findings:

- HIGH fixed: Sample access enforcement denied `TenantRole.Unknown` but would grant access for any out-of-range `TenantRole` value present in a custom or durable projection store. Fixed by allow-listing `TenantOwner`, `TenantContributor`, and `TenantReader` only in `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`.
- MEDIUM fixed: Added a regression test for out-of-range role values in `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`.
- MEDIUM fixed: Story File List did not include changed BMAD bookkeeping files. Added `_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/story-automator/orchestration-1-20260531-113112.md`.

Checklist validation:

- Story file loaded and status verified as `review` before review.
- Epic/story resolved as 4.4.
- Project context, architecture, PRD/epics references, and official DAPR docs were checked for DAPR pub/sub at-least-once delivery and .NET SDK support assumptions.
- Acceptance criteria and completed tasks were cross-checked against implementation, tests, docs, and git changes.
- Code quality, security/fail-closed behavior, and test quality reviewed for changed application source files.
- Auto-fix applied, tests rerun, story status updated to `done`, and sprint status synced.

## Change Log

- 2026-06-01: Completed Story 4.4 local consumer projection hardening, focused client/sample tests, consumer docs, and verification.
- 2026-06-01: Senior review auto-fixed fail-closed role validation for out-of-range projected roles, added regression coverage, refreshed verification evidence, and marked story done.
