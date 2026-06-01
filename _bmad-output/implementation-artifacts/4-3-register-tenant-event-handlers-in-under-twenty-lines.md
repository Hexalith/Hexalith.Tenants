---
baseline_commit: 17879ed
---

# Story 4.3: Register Tenant Event Handlers in Under Twenty Lines

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming service developer,
I want a concise event-handler registration pattern,
so that a service can become tenant-aware without bespoke integration code.

## Acceptance Criteria

1. **Given** a consuming service references Contracts and Client packages
   **When** the developer registers tenant event handlers using the documented pattern
   **Then** the registration requires under 20 lines of DI configuration for the standard integration path
   **And** the code compiles without referencing tenant host or server projects.

2. **Given** the consuming service needs only selected tenant event types
   **When** event handlers are registered
   **Then** the registration supports filtering or dispatch by event type
   **And** handlers are not required to process unrelated event types.

3. **Given** DAPR invokes a tenant event subscription handler
   **When** the event is received
   **Then** the handler resolves consumer services through DI
   **And** it can process events without direct broker-specific APIs.

4. **Given** handler registration is invalid or incomplete
   **When** the consuming service starts or receives an event
   **Then** the failure mode is clear and actionable for the developer
   **And** sensitive event payloads are not logged as troubleshooting output.

5. **Given** registration tests and sample compilation run
   **When** the sample consuming service builds
   **Then** the handler registration remains under the target line count
   **And** the sample proves the documented registration path.

## Tasks / Subtasks

- [x] Task 1: Add a public Client registration helper for selected tenant event handlers (AC: #1, #2)
  - [x] Add a chainable `IServiceCollection` extension in `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`, or a new one-type file in the same namespace, for registering `ITenantEventHandler<TEvent>` implementations.
  - [x] Preferred API shape: `AddTenantEventHandler<TEvent, THandler>() where TEvent : IEventPayload where THandler : class, ITenantEventHandler<TEvent>`.
  - [x] Keep the standard consumer path compact: `builder.Services.AddHexalithTenants().AddTenantEventHandler<UserAddedToTenant, SampleLoggingEventHandler>();`.
  - [x] Support one handler class implementing multiple selected event interfaces without requiring the consumer to manually register the concrete type plus factory descriptors.
  - [x] Return the same `IServiceCollection`, guard null inputs, and do not introduce Server, host, AppHost, broker, Redis, or database dependencies.

- [x] Task 2: Preserve event-type filtering and DI-based dispatch semantics (AC: #2, #3, #4)
  - [x] Keep event filtering based on `TenantEventEnvelope.EventTypeName` resolved through the Client event type registry built from `Hexalith.Tenants.Contracts` event payload types.
  - [x] Ensure `TenantEventProcessor` dispatches only to handlers for the resolved `TEvent`; selected custom handlers must not receive unrelated event types.
  - [x] Review handler lifetime behavior. If custom handlers may depend on scoped services, update `TenantEventProcessor` to resolve handlers through an event/request scope, for example via `IServiceScopeFactory`, while preserving existing singleton-safe built-in projection behavior.
  - [x] Keep `TenantEventProcessor` deduplication by `MessageId` and retry semantics: failed handler execution removes the message ID so DAPR redelivery or a retry can process the event again.
  - [x] Keep logs actionable but bounded to metadata such as message ID, event type, tenant ID, and correlation ID. Do not log serialized payload bytes or whole event objects.

- [x] Task 3: Tighten clear-failure behavior for invalid or incomplete registration (AC: #4)
  - [x] Preserve clear outcomes for unknown event types, invalid payloads, duplicate messages, and known events with no registered handlers.
  - [x] If no custom handler is registered for a selected business event, the processor must not crash; it may return `SkippedNoHandlers` only when no built-in or custom handler exists for the resolved event type.
  - [x] If handler construction fails through DI, let the startup or request failure identify the missing service/dependency without swallowing the exception as a successful event.
  - [x] Keep `MapTenantEventSubscription()` returning non-retry responses only for intentionally skipped or duplicate events; invalid payloads should remain a failure response as currently implemented.

- [x] Task 4: Update the sample to prove the concise registration pattern (AC: #1, #5)
  - [x] Update `samples/Hexalith.Tenants.Sample/Program.cs` to use the new helper instead of manual `AddSingleton<ITenantEventHandler<TEvent>>(...)` registrations.
  - [x] Keep the standard setup under 20 meaningful DI/middleware/subscription lines. The count should include `AddHexalithTenants()`, selected event handler registrations, `UseCloudEvents()`, `MapSubscribeHandler()`, and `MapTenantEventSubscription()`, but exclude unrelated liveness endpoints.
  - [x] Preserve `MapAccessCheckEndpoints()`, `/alive`, and `/health`; they were added for sample and Aspire topology verification and are not part of handler registration scope.
  - [x] Keep `SampleLoggingEventHandler` focused on selected event types (`UserAddedToTenant`, `UserRemovedFromTenant`, `TenantDisabled`) unless this story intentionally updates the sample event set.

- [x] Task 5: Update documentation for the registration pattern (AC: #1, #2, #4, #5)
  - [x] Update `docs/quickstart.md` or `docs/event-contract-reference.md` with the concise handler registration snippet.
  - [x] Document that consumers subscribe to shared topic `tenants.events` and select event types by registering typed handlers, not by creating one topic per event type.
  - [x] Document that DAPR pub/sub is at-least-once; handlers must be idempotent and must not assume cross-service ordering.
  - [x] Document troubleshooting outcomes without suggesting logging full payloads.

- [x] Task 6: Add or update focused tests (AC: #1-#5)
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` for the new helper: chaining, selected event registration, multi-event handler support, idempotent duplicate registration, and no Server/AppHost references.
  - [x] Extend `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs` for selected-type dispatch and, if scoped resolution is added, proof that scoped handler dependencies resolve correctly.
  - [x] Add a sample test that reads `samples/Hexalith.Tenants.Sample/Program.cs` and verifies the standard integration section remains under 20 meaningful lines.
  - [x] Use xUnit v3 and Shouldly. Do not add `using Xunit;`; tests inherit it from `tests/Directory.Build.props`.

- [x] Task 7: Verification (AC: #1-#5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore` if the sandbox allows VSTest.
  - [x] If VSTest fails before executing tests because the sandbox denies TCP listener creation, build the test project and run the direct xUnit test assembly fallback documented in Story 4.2.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false`.
  - [x] Confirm the sample project builds without referencing `Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, or `Hexalith.Tenants.AppHost`.

## Dev Notes

### Scope

This story is the developer-experience bridge between Story 4.2 and later projection/sample stories. It should make custom tenant event handler registration concise and hard to misuse.

Use the existing Client infrastructure:

- `ITenantEventHandler<TEvent>` is the typed handler contract.
- `TenantEventContext` carries event metadata for handlers.
- `TenantEventProcessor` resolves the event type, deserializes the payload, deduplicates by `MessageId`, and dispatches typed handlers through DI.
- `TenantEventSubscriptionEndpoints.MapTenantEventSubscription()` maps the DAPR subscription endpoint and applies `WithTopic(options.PubSubName, options.TopicName)`.
- `AddHexalithTenants()` is still the primary base registration path.

Do not create a second event-processing pipeline, a new pub/sub abstraction, a server callback, or sample-only registration code.

### Current State

`src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` currently registers:

- DAPR client if one is not already registered.
- `HexalithTenantsOptions` and startup validation.
- Default `ITenantProjectionStore` as `InMemoryTenantProjectionStore` if the consumer did not provide one.
- Built-in `TenantProjectionEventHandler` for all current tenant event payload types.
- A registry of event payload CLR types from `Hexalith.Tenants.Contracts`.
- Singleton `TenantEventProcessor`.

Custom handler registration currently requires manual DI code in the sample:

```csharp
builder.Services.AddSingleton<SampleLoggingEventHandler>();
builder.Services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<TenantDisabled>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
```

This works but is the bespoke integration code this story should eliminate.

### Expected Consumer Pattern

Keep the public setup small and readable. The sample should end up close to this shape:

```csharp
builder.Services
    .AddHexalithTenants()
    .AddTenantEventHandler<UserAddedToTenant, SampleLoggingEventHandler>()
    .AddTenantEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>()
    .AddTenantEventHandler<TenantDisabled, SampleLoggingEventHandler>();

WebApplication app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapTenantEventSubscription();
app.MapAccessCheckEndpoints();
```

This is below the FR45 target and proves the selected-event-type model. The helper name may differ if the implementation has a stronger local naming convention, but it must be obvious from IntelliSense and remain in the Client registration namespace.

### Architecture Guardrails

- Client-facing code belongs under `src/Hexalith.Tenants.Client`.
- Public immutable contracts remain in `src/Hexalith.Tenants.Contracts`.
- The sample belongs under `samples/Hexalith.Tenants.Sample`; sample tests belong under `samples/Hexalith.Tenants.Sample.Tests`.
- Do not touch Server aggregates, host `Program.cs`, AppHost topology, DAPR component YAML, or EventStore submodule code for this story.
- DAPR remains the pub/sub abstraction. Do not introduce Kafka, RabbitMQ, Redis, database, or broker-specific APIs.
- Shared topic remains `tenants.events`; consumers filter by event type through typed handler registration.
- `Dapr.AspNetCore` endpoint subscription still requires `UseCloudEvents()`, `MapSubscribeHandler()`, and an endpoint decorated with `WithTopic(...)`.
- Event payloads and envelopes use `System.Text.Json`; do not add Newtonsoft.Json.
- Do not log full payloads. Payloads may contain tenant configuration or user identifiers. Prefer event type, message ID, correlation ID, and tenant ID for diagnostics.

### Previous Story Intelligence

Story 4.2 completed the base Client registration and fixed a real default drift:

- `CommandApiAppId = "commandapi"` was removed from the Client options surface because it was stale against current AppHost app IDs.
- `HexalithTenantsOptions.ConfigurationSectionName` is `Tenants`.
- Defaults are `PubSubName = "pubsub"` and `TopicName = "tenants.events"`.
- Registration must remain idempotent: no duplicate `DaprClient`, no duplicate options validation, and do not overwrite consumer-provided `ITenantProjectionStore`.
- Unit tests should inspect descriptors and options behavior without resolving `DaprClient`.

Story 4.2 also documented the sandbox test constraint: plain `dotnet test` may build successfully and then fail before test execution because VSTest cannot open a TCP listener. Use the direct xUnit runner fallback after building when that happens.

Story 4.1 established the shared event topic and app-id correction:

- Tenant and global-administrator events publish to `tenants.events`.
- The current topology uses app IDs `eventstore`, `tenants`, `eventstore-admin`, and `sample`; do not reintroduce `commandapi`.
- Consumers filter shared-topic events by type rather than assuming one topic per event type.

### Git Intelligence

Recent relevant commits:

- `17879ed feat(story-4.2): Expose Consumer DI Registration for Tenant Client Services`
- `557de8d feat(story-4.1): Publish Tenant Domain Events as CloudEvents`
- `9a779f0 feat(story-3.8): Reject Conflicting Concurrent Tenant Modifications`

The recent pattern is narrow Client/package changes with descriptor tests, direct xUnit fallback when VSTest is blocked, and Release solution build verification.

### Files To Inspect Or Update

- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs` - current base registration path; likely location for the new helper or the helper's neighboring file.
- `src/Hexalith.Tenants.Client/Handlers/ITenantEventHandler.cs` - typed handler contract to preserve.
- `src/Hexalith.Tenants.Client/Handlers/TenantEventContext.cs` - event metadata available to custom handlers.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs` - dispatch, deduplication, handler resolution, retry semantics, and logging.
- `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs` - DAPR endpoint mapping; should keep `WithTopic(options.PubSubName, options.TopicName)`.
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs` - registration tests to extend.
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs` - dispatch tests to extend.
- `samples/Hexalith.Tenants.Sample/Program.cs` - sample proof of concise registration.
- `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs` - selected custom handler implementation.
- `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs` - sample handler tests to preserve or extend.
- `docs/quickstart.md`, `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md` - update only where the public registration pattern or troubleshooting guidance is stale.

### Testing Standards

- Use xUnit v3 and Shouldly.
- Do not use `Assert.*`.
- Do not add `using Xunit;` to test files.
- Test names in existing files use the local PascalCase style; follow the surrounding file.
- Descriptor assertions should not instantiate `DaprClient`.
- If adding scoped handler tests, use a small fake scoped service and prove the handler receives it through DI during `TenantEventProcessor.ProcessAsync`.
- Do not add live DAPR sidecar tests for this story; keep tests Tier 1 unless a future story explicitly expands integration coverage.

### External Technical Notes

- Microsoft options guidance supports validating options at startup with `ValidateOnStart()` and custom `IValidateOptions<TOptions>`. Keep the existing Story 4.2 validation approach unless implementation finds a concrete bug.
- Current DAPR docs identify `Dapr.AspNetCore` as the ASP.NET Core integration package and the troubleshooting docs still call out `MapSubscribeHandler()` plus topic registration as the route discovery path for pub/sub endpoints.
- DAPR current docs also describe pub/sub topic scoping. Do not implement DAPR component scope changes in this story, but keep names compatible with `pubsub` and `tenants.events`.

### Project Structure Notes

- This story should touch Client, Client.Tests, sample, sample tests, and documentation only.
- It should not require changes in `Hexalith.EventStore`, `Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, `src/Hexalith.Tenants.AppHost`, or DAPR component files.
- There is an older completed artifact at `_bmad-output/implementation-artifacts/4-3-sample-consuming-service-and-idempotent-processing-guide.md`. Do not use that file as the current sprint story; the active sprint key is `4-3-register-tenant-event-handlers-in-under-twenty-lines`.
- Do not initialize nested submodules.
- Use `Hexalith.Tenants.slnx`, not `.sln`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.3 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` - FR35-FR45, FR62, NFR14-NFR19]
- [Source: `_bmad-output/planning-artifacts/architecture.md` - Client/Handlers, Client/Subscription, DAPR resource names, event integration boundaries]
- [Source: `_bmad-output/project-context.md` - package boundaries, DAPR conventions, testing rules, central package management]
- [Source: `_bmad-output/implementation-artifacts/4-2-expose-consumer-di-registration-for-tenant-client-services.md` - Client registration defaults, validation, test fallback, stale app-id learning]
- [Source: `_bmad-output/implementation-artifacts/4-1-publish-tenant-domain-events-as-cloudevents.md` - shared topic and app-id drift learning]
- [Source: `docs/event-contract-reference.md` - shared `tenants.events` topic, CloudEvents 1.0, at-least-once delivery, event metadata]
- [Source: `docs/idempotent-event-processing.md` - message-level deduplication and handler idempotency guidance]
- [External: Microsoft Learn, Options pattern - https://learn.microsoft.com/en-us/dotnet/core/extensions/options]
- [External: DAPR docs, .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/]
- [External: DAPR docs, Pub/sub topic scoping - https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/]
- [External: DAPR docs, Troubleshoot Pub/Sub with the .NET SDK - https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-troubleshooting/dotnet-troubleshooting-pubsub/]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore` failed before test execution because MSBuild could not open sandbox-denied named pipe/socket listeners (`SocketException (13): Permission denied`); used documented direct xUnit fallback.
- 2026-06-01: Debug build `dotnet build tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed.
- 2026-06-01: Direct xUnit Debug Client run `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 73, Failed 0, Skipped 0.
- 2026-06-01: Debug build `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed.
- 2026-06-01: Direct xUnit Debug Sample run `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` passed: Total 18, Failed 0, Skipped 0.
- 2026-06-01: Release solution build `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed.
- 2026-06-01: Sample project references only `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts`; no Server, host, AppHost, or `src/Hexalith.Tenants` project reference found.
- 2026-06-01: Direct xUnit Release regression passed: Contracts 101/0 failed/0 skipped, Client 73/0/0, Sample 18/0/0, Server 578/0/0, Testing 99/0/0, Integration 136/0 failed/25 DAPR/performance prerequisite skips.
- 2026-06-01: Review found sample logging still emitted payload user identifiers and role values; updated `SampleLoggingEventHandler` to log only event metadata and added regression assertions.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Client.Tests/ --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` built successfully but VSTest aborted before execution because sandbox denied TCP listener creation (`SocketException (13): Permission denied`); used direct xUnit fallback.
- 2026-06-01: Review direct xUnit Debug Client run `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 79, Failed 0, Skipped 0.
- 2026-06-01: Review direct xUnit Debug Sample run `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` passed: Total 18, Failed 0, Skipped 0.
- 2026-06-01: Review Release solution build `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false` passed.
- 2026-06-01: Review direct xUnit Release Client run `tests/Hexalith.Tenants.Client.Tests/bin/Release/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` passed: Total 79, Failed 0, Skipped 0.
- 2026-06-01: Review direct xUnit Release Sample run `samples/Hexalith.Tenants.Sample.Tests/bin/Release/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` passed: Total 18, Failed 0, Skipped 0.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Validation checklist applied during story creation; current code, prior story learnings, package boundaries, sample line-count target, and payload logging guardrails are called out explicitly.
- Added chainable `AddTenantEventHandler<TEvent, THandler>()` in the Client registration namespace with null guarding, duplicate-registration idempotency, selected event registration, and support for one handler class across multiple typed event interfaces.
- Updated `TenantEventProcessor` to create a DI scope for each event dispatch so scoped custom handler dependencies resolve correctly while existing singleton projection handlers continue to work.
- Updated the sample to use the concise selected handler chain and added a source-level sample test that keeps the standard integration path under 20 meaningful lines.
- Updated event consumption documentation to show typed handler registration on shared topic `tenants.events`, at-least-once/idempotency expectations, cross-service ordering limits, and payload-safe troubleshooting guidance.
- Senior developer review completed. Auto-fixed payload-safe sample logging and story File List drift; no critical issues remain.

### File List

- `_bmad-output/implementation-artifacts/4-3-register-tenant-event-handlers-in-under-twenty-lines.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`
- `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventSubscriptionEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample/Program.cs`
- `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs`
- `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`
- `docs/quickstart.md`
- `docs/event-contract-reference.md`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes. Story status set to `done`.

Review validation:
- Acceptance Criteria 1, 2, and 5 are implemented by `AddTenantEventHandler<TEvent, THandler>()`, sample usage under 20 relevant registration/middleware/subscription lines, and documentation updates for the shared `tenants.events` topic.
- Acceptance Criteria 2 and 3 are implemented by typed event registry dispatch and request/event-scope handler resolution in `TenantEventProcessor`.
- Acceptance Criterion 4 is implemented by existing unknown-event, duplicate, invalid-payload, missing-handler, and handler-failure behavior, with review hardening to avoid payload-sensitive sample logs.
- File List was cross-checked against git changes and updated for the new endpoint subscription test plus review-touched sample handler files.
- Project context, PRD, architecture, epics, and current DAPR/Microsoft primary docs were checked for FR35-FR45, shared-topic pub/sub, at-least-once delivery, `MapSubscribeHandler()`/topic metadata, and options validation guidance.

Findings fixed:
- HIGH: `SampleLoggingEventHandler` logged payload values (`UserId` and `Role`) even though the story requires troubleshooting logs to avoid sensitive payload output. Fixed by logging event metadata (`TenantId`, `MessageId`, `CorrelationId`) and event names only.
- MEDIUM: Story File List omitted `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventSubscriptionEndpointsTests.cs`. Fixed by adding it to the File List.
- MEDIUM: Review changes touched `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs` and `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs`; both are now recorded in the File List.

Verification:
- Direct xUnit Debug Client: 79 passed, 0 failed, 0 skipped.
- Direct xUnit Debug Sample: 18 passed, 0 failed, 0 skipped.
- Release solution build: passed with 0 warnings and 0 errors.
- Direct xUnit Release Client: 79 passed, 0 failed, 0 skipped.
- Direct xUnit Release Sample: 18 passed, 0 failed, 0 skipped.
- `dotnet test` VSTest path was attempted and blocked by sandbox TCP listener restrictions before test execution; direct xUnit fallback was used.

### Change Log

- 2026-06-01: Implemented concise typed tenant event handler registration, scoped event dispatch, sample registration proof, documentation updates, and focused regression coverage for Story 4.3.
- 2026-06-01: Senior developer review auto-fixed payload-safe sample logging, updated File List drift, verified focused Debug/Release test fallback runs, and marked Story 4.3 done.
