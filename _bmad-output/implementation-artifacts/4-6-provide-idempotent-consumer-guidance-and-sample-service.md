---
baseline_commit: e396f0a
---

# Story 4.6: Provide Idempotent Consumer Guidance and Sample Service

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer evaluating Tenants,
I want a sample consuming service and idempotency guidance,
so that I can copy a safe event-driven integration pattern into my own service.

## Acceptance Criteria

1. Given the sample consuming service is opened, when a developer reviews tenant integration code, then the sample demonstrates tenant event subscription, DI registration, local projection update, access revocation, lifecycle handling, and configuration reaction, and the standard setup remains under the documented integration-code target.
2. Given the idempotent event processing documentation is reviewed, when a developer follows the guidance, then it explains DAPR at-least-once delivery, and it includes a deduplication-by-event-ID example and idempotent handler pattern with code.
3. Given tenant events include event ID and aggregate version metadata, when the sample handles events, then it stores or checks enough metadata to avoid duplicate side effects, and it uses aggregate version or event ordering only within documented limits.
4. Given the sample demonstrates access revocation, when a user is removed from a tenant, then the sample shows the consuming service revoking local access based on the tenant event stream, and no custom polling or manual synchronization job is required.
5. Given sample validation runs, when the sample and documentation snippets are built or tested, then code samples compile against the published package surface, and docs do not rely on internal project references or unavailable infrastructure for basic understanding.

## Tasks / Subtasks

- [x] Task 1: Complete the sample-consuming-service evidence surface (AC: #1, #4, #5)
  - [x] Inspect `samples/Hexalith.Tenants.Sample/Program.cs`; keep the standard tenant integration setup on `AddHexalithTenants()`, `AddTenantEventHandler<TEvent, THandler>()`, `UseCloudEvents()`, `MapSubscribeHandler()`, and `MapTenantEventSubscription()`.
  - [x] Keep `SampleRegistrationTests.Program_standard_tenant_event_registration_stays_under_twenty_meaningful_lines` green; if setup changes, update the test to count only meaningful tenant integration lines.
  - [x] Ensure the sample demonstrates all required reactions through the existing local projection path: access grant/revoke, role change, disable/enable lifecycle, and `sample.` configuration reads.
  - [x] Do not add polling, background sync jobs, per-request Tenants API calls, or direct broker/database access to make the sample work.
  - [x] Add or update a sample README/walkthrough only if `docs/quickstart.md` and `docs/idempotent-event-processing.md` are not sufficient to show how to copy the pattern.

- [x] Task 2: Harden idempotency guidance with copyable, accurate code (AC: #2, #3, #5)
  - [x] Update `docs/idempotent-event-processing.md` to include a concrete deduplication-by-`MessageId` example and an idempotent handler pattern that compiles against `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Client`.
  - [x] Explain that DAPR pub/sub is at-least-once and that subscriber endpoints must return success only after safe handler execution; failed handler execution must allow redelivery.
  - [x] Document the limits of `SequenceNumber`: it can help reason about ordering within one aggregate stream, but it must not be treated as global ordering across services, tenants, aggregates, or subscriber instances.
  - [x] Document that `TenantProjectionEventHandler` records bounded `TenantLocalState.LastEvent` metadata and that production deduplication for scaled-out services should use a bounded/shared dedup store.
  - [x] Keep examples on `System.Text.Json`, `TenantEventContext`, `ITenantEventHandler<TEvent>`, and `ITenantProjectionStore`; do not introduce new serializers or infrastructure packages.

- [x] Task 3: Preserve and extend processor/projection idempotency tests where evidence is missing (AC: #2, #3, #4, #5)
  - [x] Reuse `TenantEventProcessor`, `TenantProjectionEventHandler`, `TenantLocalState`, and `InMemoryTenantProjectionStore`; do not create a second consumer event pipeline.
  - [x] Ensure `TenantEventProcessorTests` proves duplicate `MessageId` delivery returns `Duplicate` and does not re-save projection state.
  - [x] Ensure handler failure removes the in-progress message ID so a corrected redelivery with the same `MessageId` can process.
  - [x] Ensure payload `TenantId` must match envelope `AggregateId` before dispatch; mismatches must not write local projection state.
  - [x] Ensure `TenantProjectionEventHandlerTests` proves duplicate payloads are harmless for lifecycle, membership, role, and configuration events.

- [x] Task 4: Prove sample access revocation and lifecycle/configuration behavior from events (AC: #1, #4, #5)
  - [x] Extend `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs` only where coverage is missing for event-pipeline add, remove, repeated remove, role change, disable, and enable behavior.
  - [x] Extend `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs` only where coverage is missing for `sample.` namespace filtering, set, update, remove, unrelated-key hiding, and no synchronous Tenants API dependency.
  - [x] Keep fail-closed defaults: unknown tenant, `TenantStatus.Unknown`, `TenantRole.Unknown`, and out-of-range roles must deny access.
  - [x] Keep logs support-safe. If `SampleLoggingEventHandler` changes, tests must prove logs do not include full payloads, user IDs, role names, or configuration values.

- [x] Task 5: Align event contract and quickstart documentation with the final sample (AC: #1, #2, #3, #4, #5)
  - [x] Update `docs/event-contract-reference.md` so the event delivery model states the shared topic, at-least-once delivery, local projection semantics, bounded metadata, and eventual consistency.
  - [x] Update `docs/quickstart.md` so the "Consume Tenant Events" section includes the sample registration path and states that reactions come from local projection state, not polling or per-request Tenants API calls.
  - [x] Cross-link `docs/idempotent-event-processing.md`, `docs/event-contract-reference.md`, and `docs/quickstart.md` so a developer can move from the sample to the contract and idempotency rules.
  - [x] Make all snippets use public package surface only: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, ASP.NET Core, DAPR ASP.NET Core integration, and the sample project. Do not require references to `Hexalith.Tenants.Server`, the host project, AppHost internals, or test-only helpers for basic understanding.

- [x] Task 6: Verification (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.
  - [x] Run `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.
  - [x] If VSTest builds but aborts because the sandbox denies listener creation, run the direct xUnit assemblies as established by Stories 4.4 and 4.5, and record the command/results in this story.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.

## Dev Notes

### Scope

This is a developer-adoption story for the Epic 4 consumer integration path. It should finish the copyable sample and idempotency guidance that makes Stories 4.1 through 4.5 usable by another service.

The likely implementation is small and evidence-heavy: complete any missing sample/docs/tests, keep the existing Client pipeline intact, and verify that the sample can be understood without internal project references or live infrastructure. Do not add new domain commands, server aggregate behavior, query endpoints, AppHost topology, or DAPR components unless implementation discovers an in-scope defect.

### Current State To Inspect

- `samples/Hexalith.Tenants.Sample/Program.cs`
  - Currently registers `AddHexalithTenants()`, selected `SampleLoggingEventHandler` handlers, CloudEvents middleware, DAPR subscribe handler, `MapTenantEventSubscription()`, access-check endpoints, configuration endpoints, `/alive`, and `/health`.
  - Preserve the standard registration path and the under-20-line integration target.
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
  - Reads only `ITenantProjectionStore`.
  - Grants only active tenants with `TenantOwner`, `TenantContributor`, or `TenantReader`.
  - Denies unknown tenants, disabled/non-active tenants, missing users, unknown roles, and out-of-range roles.
- `samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs`
  - Reads only `ITenantProjectionStore`.
  - Returns keys under the `sample.` namespace and hides unrelated namespaces such as `billing.`.
  - Preserve `StringComparison.Ordinal` and no synchronous Tenants API dependency.
- `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`
  - Logs bounded metadata for selected events.
  - Preserve support-safe logging; do not log full payloads, user identifiers, role names, or configuration values.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
  - Deduplicates by `MessageId` using `_processedMessageIds`.
  - Removes the message ID on invalid payload or handler failure so a corrected redelivery can retry.
  - Validates payload `TenantId` against envelope `AggregateId` before dispatch.
  - Creates a DI scope per event and dispatches typed `ITenantEventHandler<TEvent>` implementations.
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
  - Handles tenant lifecycle, membership, role, and configuration events.
  - Uses per-tenant `SemaphoreSlim` locks.
  - Applies idempotent dictionary set/remove and property assignment operations.
  - Stamps `TenantLocalState.LastEvent` from `TenantEventContext` after each successful apply.
- `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`
  - Holds `TenantId`, `Name`, `Description`, `Status`, `LastEvent`, `Members`, and `Configuration`.
  - `Status` defaults to `TenantStatus.Unknown`; keep this fail-closed.
  - `Members` and `Configuration` are case-sensitive dictionaries; keep `StringComparer.Ordinal` cloning behavior.
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
  - `AddHexalithTenants()` is the primary consumer registration path.
  - `AddTenantEventHandler<TEvent, THandler>()` is the public selected-handler registration helper.
  - Built-in projection handlers are registered for lifecycle, membership, role, and configuration events.

### Architecture Guardrails

- Client-facing event processing and projection code belongs in `src/Hexalith.Tenants.Client`.
- Sample behavior belongs in `samples/Hexalith.Tenants.Sample` and `samples/Hexalith.Tenants.Sample.Tests`.
- Consumer-facing documentation belongs in `docs/quickstart.md`, `docs/event-contract-reference.md`, and `docs/idempotent-event-processing.md`.
- DAPR remains the pub/sub abstraction. Keep the default pub/sub component `pubsub` and shared topic `tenants.events`.
- Use CloudEvents 1.0 and the EventStore event envelope metadata already surfaced by `TenantEventEnvelope`.
- Keep JSON on `System.Text.Json`; do not introduce Newtonsoft.Json.
- Do not add direct Redis, SQL, Kafka, RabbitMQ, or broker-specific dependencies to `Hexalith.Tenants.Client`.
- Do not modify `Hexalith.Tenants.Server`, host query controllers, AppHost topology, DAPR component YAML, or the EventStore submodule for this story unless an in-scope defect is found.
- Do not change package versions or add inline `PackageReference Version=` values. Central package versions live in `Directory.Packages.props`.
- Use `Hexalith.Tenants.slnx`, not `.sln`.

### Event And Idempotency Semantics

- DAPR pub/sub is at-least-once. Duplicate delivery is normal and must not create duplicate side effects.
- `MessageId` is the event identifier for message-level deduplication.
- `SequenceNumber` is aggregate-local ordering metadata. It is useful inside one aggregate stream but is not a global event order across tenants, aggregates, services, or subscriber instances.
- `TenantEventProcessor` is responsible for message-level deduplication and typed handler dispatch.
- `TenantProjectionEventHandler` is responsible for deterministic local state application and bounded last-event metadata.
- Handler operations should be naturally idempotent: set dictionary values, remove dictionary keys, and assign status/properties. Avoid counters, appends, and external side effects unless protected by an external deduplication record.
- Scaled-out consumers should replace the default in-memory projection/dedup state with durable implementations. The Client package must not take a Redis/database dependency to provide that by default.

### Previous Story Intelligence

Story 4.5 completed reaction behavior and review auto-fix:

- Added bounded `TenantLocalState.LastEvent` metadata through `TenantProjectionEventMetadata`.
- Reused the existing projection processor/store/access endpoint path for access, role-change, disable/enable, and repeated-removal reactions.
- Added `TenantConfigurationEndpoints` for `sample.` namespace configuration reads from the local projection.
- Updated docs to describe local projection reactions, eventual consistency, owned namespace filtering, and no polling/sync/API-call requirement.
- Review auto-fix added fail-closed validation that event payload `TenantId` must match the envelope aggregate tenant before dispatching handlers.
- Validation evidence from Story 4.5: direct xUnit Client tests passed with 89 tests, direct xUnit Sample tests passed with 29 tests, and Release solution build passed with 0 warnings and 0 errors.

Stories 4.1 through 4.4 established:

- Tenant and global-administrator events publish to the shared `tenants.events` topic.
- Defaults are `PubSubName = "pubsub"` and `TopicName = "tenants.events"`.
- Consumers filter by event type, not by topic.
- `AddHexalithTenants()` is the primary registration extension.
- `AddTenantEventHandler<TEvent, THandler>()` registers selected handlers.
- `MapTenantEventSubscription()` maps the DAPR subscription endpoint.
- `TenantEventProcessor` deduplicates by `MessageId`; projection handlers are idempotent defense-in-depth.
- VSTest can fail in the sandbox because listener creation is denied. Use direct xUnit assembly execution as fallback after successful build.

### Git Intelligence

Recent relevant commits:

- `e396f0a feat(story-4.5): React to Tenant Access Lifecycle and Configuration Changes`
- `15e3d69 feat(story-4.4): Build Local Consumer Projection from Tenant Events`
- `a51c5bd feat(story-4.3): Register Tenant Event Handlers in Under Twenty Lines`
- `17879ed feat(story-4.2): Expose Consumer DI Registration for Tenant Client Services`
- `557de8d feat(story-4.1): Publish Tenant Domain Events as CloudEvents`

The current pattern is narrow Client/sample/docs work, focused Tier 1/sample tests, direct xUnit fallback when VSTest cannot run, and Release solution build verification.

### Latest Technical Notes

- Current DAPR v1.17 docs continue to state that pub/sub provides at-least-once delivery and redelivers when delivery/processing does not complete successfully. This supports duplicate-delivery tests and handler idempotency requirements.
- Current DAPR v1.17 topic-scoping docs describe `allowedTopics`, `publishingScopes`, `subscriptionScopes`, and `protectedTopics`. This story should not change DAPR component scopes, but docs and samples must remain compatible with production scoping of `tenants.events`.
- Current DAPR .NET SDK docs list .NET 8, .NET 9, and .NET 10 support and keep `Dapr.AspNetCore` as the ASP.NET Core integration package. Keep the repo-pinned DAPR package family; do not upgrade packages in this story.

### Project Structure Notes

- This story should primarily touch docs, Client tests, sample tests, and possibly the sample app if a demonstration gap remains.
- Existing sample/code may already satisfy parts of the story because Story 4.5 intentionally prepared reaction behavior. Do not rewrite working code just to make a larger diff; add missing evidence or documentation only.
- `samples/Hexalith.Tenants.Sample.Tests/ScaffoldingSmokeTests.cs` currently contains `Assert.True(true)` and does not follow the Shouldly/no-placeholder test rule. Remove or replace it if the sample test project is touched.
- There is an unrelated modified `_bmad-output/story-automator/orchestration-1-20260531-113112.md`; do not revert or edit it for this story.
- Do not initialize nested submodules.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.6 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` - FR35-FR42, FR44-FR45, FR62, NFR14, NFR19]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Event-driven integration, Client/sample boundaries, DAPR resource names, project structure]
- [Source: `_bmad-output/project-context.md` - DAPR conventions, Client package boundaries, testing rules, package/version constraints]
- [Source: `_bmad-output/implementation-artifacts/4-5-react-to-tenant-access-lifecycle-and-configuration-changes.md` - previous story completion notes, review fix, validation evidence]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs` - message-id deduplication, payload tenant validation, scoped handler dispatch]
- [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs` - idempotent local projection apply behavior]
- [Source: `samples/Hexalith.Tenants.Sample/Program.cs` - sample registration and endpoint mapping]
- [Source: `docs/idempotent-event-processing.md` - current idempotency guidance]
- [Source: `docs/event-contract-reference.md` - event delivery model and envelope metadata]
- [Source: `docs/quickstart.md` - consumer setup walkthrough]
- [External: DAPR docs, Pub/sub overview - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- [External: DAPR docs, Pub/sub topic scoping - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/]
- [External: DAPR docs, .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built the project, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while creating its listener.
- 2026-06-01: QA fallback `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll -noLogo -parallel none` passed: Total 92, Failed 0, Skipped 0.
- 2026-06-01: `dotnet test samples/Hexalith.Tenants.Sample.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built the project, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while creating its listener.
- 2026-06-01: QA fallback `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -noLogo -parallel none` passed: Total 31, Failed 0, Skipped 0.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Preserved the existing sample registration path in `Program.cs`; evidence remains under the under-20 meaningful-line registration test.
- Hardened idempotency documentation with `MessageId` deduplication, safe subscriber acknowledgement/redelivery behavior, aggregate-local `SequenceNumber` limits, bounded `LastEvent` metadata, and scaled-out shared dedup store guidance.
- Extended projection and sample evidence without adding a second event pipeline or any polling/API/broker/database dependency.
- Replaced placeholder smoke tests in touched Client/sample test projects with real Shouldly assertions.
- QA generate-e2e-tests added invalid-payload retry, subscription payload/envelope mismatch, and sample configuration null-store tests; updated the automation summary and checklist evidence.
- Verified through direct xUnit fallback after VSTest listener denial and Release solution build.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- [Medium] `docs/quickstart.md` said its event registration snippet used the same registration path as `samples/Hexalith.Tenants.Sample/Program.cs`, but the snippet omitted the sample's `TenantDisabled` handler. Added the missing handler registration so the walkthrough matches the sample lifecycle demonstration.
- [Low] `docs/event-contract-reference.md` used `key.startsWith("billing.")`, which is not a C# copyable example. Replaced it with `key.StartsWith("billing.", StringComparison.Ordinal)` to match the repository's C# and ordinal-comparison guidance.

Review validation:

- Story status was `review`; epic/story resolved as 4.6.
- Story context, planning references, architecture guardrails, project context, and DAPR documentation references were reviewed.
- Acceptance Criteria 1-5 were cross-checked against the sample app, Client processor/projection code, docs, and focused tests.
- File List was checked against git changes. The only extra changed file outside the story File List is `_bmad-output/story-automator/orchestration-1-20260531-113112.md`, already noted as unrelated and excluded from source review by workflow guardrails.
- Tests were mapped to ACs: Client idempotency and subscription tests cover AC2/AC3/AC5; sample endpoint tests cover AC1/AC4/AC5; docs cover AC1-AC5.
- VSTest commands built but aborted in this sandbox due `SocketException (13): Permission denied`, matching prior story evidence. Direct xUnit fallback passed for Client and sample tests.
- Release solution build passed with 0 warnings and 0 errors.

### File List

- `_bmad-output/implementation-artifacts/4-6-provide-idempotent-consumer-guidance-and-sample-service.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-contract-reference.md`
- `docs/idempotent-event-processing.md`
- `docs/quickstart.md`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/ScaffoldingSmokeTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventSubscriptionEndpointsTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/ScaffoldingSmokeTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

### Change Log

- 2026-06-01: Completed Story 4.6 idempotent consumer guidance and sample evidence; moved status to review.
- 2026-06-01: Senior developer review auto-fixed documentation alignment issues; validated Client/sample tests and Release build; moved status to done.
