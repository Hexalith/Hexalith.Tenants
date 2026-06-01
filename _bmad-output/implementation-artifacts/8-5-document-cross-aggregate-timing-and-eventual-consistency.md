---
created: 2026-06-01
source_story_key: 8-5-document-cross-aggregate-timing-and-eventual-consistency
baseline_commit: c0b3abd
---

# Story 8.5: Document Cross-Aggregate Timing and Eventual Consistency

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer integrating tenant events,
I want documentation for timing windows and eventual consistency,
so that I can design services that behave correctly while projections catch up.

## Acceptance Criteria

1. Given a developer reads the timing documentation, when tenant commands, event persistence, pub/sub publication, subscriber processing, and local projections are described, then the document explains the event propagation window clearly, and it identifies which state is authoritative at each stage.
2. Given the documentation includes a sequence diagram, when the developer follows the command-to-subscriber flow, then the diagram shows command submission, aggregate handling, event storage, publication, subscriber processing, and projection update, and it does not imply synchronous subscriber enforcement.
3. Given a consumer service needs security-critical enforcement, when the developer reviews guidance, then the document explains how to design for eventual consistency, and it references planned synchronous authorization plugin behavior as an optional future enforcement path.
4. Given projection lag or subscriber delay occurs, when the documentation describes user-visible behavior, then it provides practical guidance for stale data, retries, local projection rebuilds, and support-safe diagnostics, and it avoids advising `Thread.Sleep` or fixed-delay waits as correctness mechanisms.
5. Given timing documentation is validated, when architecture or event-flow implementation changes, then diagrams and text are checked for drift, and stale timing claims are corrected before release.

## Tasks / Subtasks

- [x] Audit the existing timing guide and define the source-backed correction scope. (AC: 1, 2, 5)
  - [x] Treat `docs/cross-aggregate-timing.md` as prior repository state that must be verified, corrected, and tested; do not assume it satisfies Story 8.5 because sprint status still has this story in backlog at creation time.
  - [x] Preserve the Epic 8 split: Story 8.5 owns timing, eventual consistency, projection lag, stale reads, diagnostics, and sequence diagrams; Story 8.6 owns compensating-command patterns.
  - [x] Reconcile the current guide with EventStore command lifecycle docs: `POST /api/v1/commands` returns `202 Accepted` with a status URL, but durable outcome proof comes from command status states such as `EventsStored`, `EventsPublished`, `Completed`, `Rejected`, or `PublishFailed`.
  - [x] Correct any diagram or wording that implies subscriber access enforcement is synchronous with command submission, aggregate handling, event persistence, or EventStore command completion.

- [x] Rewrite `docs/cross-aggregate-timing.md` as the authoritative timing-window guide. (AC: 1, 2, 3, 4)
  - [x] Add a state-authority table covering: command submission, aggregate/domain result, persisted event stream, EventStore command status, Tenants query projections, DAPR pub/sub delivery, consuming-service local projection, and Aspire/log/trace diagnostics.
  - [x] Explain that EventStore events are source-of-truth write history; Tenants query read models and consumer local projections are projection state and can lag independently.
  - [x] Explain the subscriber propagation window from event publication through DAPR delivery, handler execution, `TenantProjectionEventHandler`, `ITenantProjectionStore.SaveAsync`, and local endpoint reads such as `/access/{tenantId}/{userId}`.
  - [x] Include failure/recovery behavior: `PublishFailed`, drain recovery/republish, subscriber retry/redelivery, DAPR dead-letter topic configuration, local projection rebuild, and support-safe diagnostics.
  - [x] Explicitly discourage `Thread.Sleep`, magic fixed waits, or "wait N seconds means correct" guidance. Use status polling, bounded retry/backoff, projection metadata, health/log/trace evidence, and rebuild/catch-up procedures instead.
  - [x] Keep security-critical guidance precise: current MVP consumers must design for eventual consistency and fail closed; the planned EventStore authorization plugin is future/optional synchronous pipeline enforcement, not current behavior.
  - [x] Keep examples support-safe: no raw bearer tokens, decoded JWT payloads, secrets, full serialized event payload dumps, or real tenant/user data.

- [x] Replace or extend the sequence diagram with a source-backed Mermaid flow. (AC: 2, 5)
  - [x] Show `Client -> EventStore command gateway -> MediatR/SubmitCommandHandler -> AggregateActor -> Tenants domain processor -> EventStore state store -> DAPR pub/sub -> Sample/consumer endpoint -> TenantEventProcessor -> TenantProjectionEventHandler -> ITenantProjectionStore -> local access/configuration endpoint`.
  - [x] Show command status polling separately from subscriber processing.
  - [x] Label the authoritative persistence boundary and the eventual-consistency window clearly.
  - [x] Show `PublishFailed`/republish and subscriber redelivery/dead-letter paths without implying hidden rollback of persisted events.
  - [x] If the diagram references multiple subscribing services, phrase them as independent consumers of the same topic; do not imply cross-service ordering or simultaneous projection catch-up.

- [x] Add source-backed documentation validation tests. (AC: 1, 2, 3, 4, 5)
  - [x] Add `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`.
  - [x] Verify the guide contains `sequenceDiagram`, `POST /api/v1/commands`, `GET /api/v1/commands/status/{correlationId}`, `EventsStored`, `EventsPublished`, `Completed`, `PublishFailed`, `tenants.events`, `deadletter.tenants.events`, `MapTenantEventSubscription()`, `TenantEventProcessor`, `TenantProjectionEventHandler`, `ITenantProjectionStore`, and `/access/{tenantId}/{userId}`.
  - [x] Verify the guide references source-backed files: `Hexalith.EventStore/docs/concepts/command-lifecycle.md`, `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`, `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`, `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`, `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`, `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`, and `deploy/dapr/pubsub.yaml`.
  - [x] Verify the guide distinguishes source-of-truth event history from Tenants query projections and consumer local projections.
  - [x] Verify the guide says DAPR delivery is at-least-once, idempotent handlers are required, `SequenceNumber` is aggregate-local only, and consumers must not assume cross-service ordering.
  - [x] Verify the guide forbids or avoids `Thread.Sleep`, fixed-delay correctness, synchronous subscriber enforcement claims, raw tokens, secrets, and full payload logging.
  - [x] Verify related docs link to the guide: `docs/demo.md`, `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md`, `docs/sample-consuming-service-walkthrough.md`, and `README.md` where navigation currently omits the timing guide.

- [x] Validate executable behavior and record evidence. (AC: 5)
  - [x] Run focused documentation tests through `dotnet test` or the direct xUnit runner fallback used in Stories 8.1 through 8.4 if VSTest hits the sandbox socket limitation.
  - [x] Run focused related Client/Sample tests that anchor the timing claims: `TenantEventProcessorTests`, `TenantEventSubscriptionEndpointsTests`, `AccessCheckEndpointsTests`, `TenantConfigurationEndpointsTests`, and `SampleRegistrationTests`.
  - [x] If Docker, DAPR, Keycloak, and Aspire are available, use the existing AppHost/demo path to observe at least one command-status-to-local-projection transition and record the evidence.
  - [x] If live infrastructure is unavailable, record the exact missing prerequisite and do not claim live timing proof. Source-backed documentation tests remain required.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` only if this repository continues recording Epic 8 documentation validation evidence there.

## Dev Notes

### Source Context

- Epic 8 objective: developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands. Story 8.5 specifically owns cross-aggregate timing and eventual-consistency documentation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.5 requires timing-window explanation, a command-to-subscriber sequence diagram, eventual-consistency design guidance, optional future synchronous authorization plugin references, stale-data/retry/rebuild diagnostics, and validation against architecture or implementation drift. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.5: Document Cross-Aggregate Timing and Eventual Consistency`]
- PRD FR64 requires cross-aggregate timing documentation including propagation windows, eventual consistency, sequence diagram, design guidance, and planned auth plugin reference. [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- PRD journeys describe the exact behavior this story must make explicit: one consumer can process tenant events before another, commands on other aggregates can briefly succeed before a `TenantDisabled` event reaches that service, and pub/sub outages do not roll back stored events. [Source: `_bmad-output/planning-artifacts/prd.md#Journey 4: Alex - First Error`]
- Architecture states that events publish through DAPR pub/sub as CloudEvents 1.0 and consumers must assume at-least-once delivery and eventual consistency. It maps Epic 8 documentation/adoption work to `docs/`, `README.md`, and the sample project. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `docs/cross-aggregate-timing.md` already exists. It covers timing windows, a Mermaid sequence diagram, event ordering, idempotency, query confirmation, and Phase 2 synchronous enforcement, but Story 8.5 is still `backlog` at creation time. Treat the document as unaccepted prior work that must be source-checked and tested.
- Known drift risk in the current timing guide: the diagram simplifies the EventStore command lifecycle and can be read as `202 Accepted` after event storage but before publication. Align the final wording with EventStore's current command lifecycle and command-status states instead of relying on the simplified diagram.
- `README.md` currently lists quickstart, sample walkthrough, demo, event contract reference, idempotent processing, and related docs, but its docs tree omits `cross-aggregate-timing.md`. Update navigation if it remains stale.
- `docs/demo.md`, `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md`, and `docs/sample-consuming-service-walkthrough.md` already reference eventual consistency and link to timing guidance. Preserve the links and avoid duplicating their full scope.
- No `CrossAggregateTimingDocumentationTests.cs` exists yet. Existing documentation tests use source-backed string/regex assertions in `tests/Hexalith.Tenants.Server.Tests/Documentation/` and are the right pattern to extend.

### Technical Guardrails

- Use repo-pinned versions and package families from project context. Do not bump .NET, DAPR, Aspire, xUnit, Shouldly, or package references for this documentation story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Command submission goes through EventStore `POST /api/v1/commands`; Tenants does not expose per-command REST endpoints. Status proof uses `GET /api/v1/commands/status/{correlationId}`. [Source: `docs/quickstart.md`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- EventStore command statuses include in-flight `Received`, `Processing`, `EventsStored`, and `EventsPublished`, plus terminal `Completed`, `Rejected`, `PublishFailed`, and `TimedOut`. Do not treat initial HTTP acceptance as subscriber projection completion. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- EventStore command lifecycle persists events before publication and records terminal command state. Optimistic concurrency conflicts after `EventsStored` are not retried because events are already committed. [Source: `Hexalith.EventStore/docs/concepts/command-lifecycle.md`; `Hexalith.EventStore/docs/guides/configuration-reference.md#Command Concurrency`]
- DAPR pub/sub component names are contracts: component `pubsub`, topic `tenants.events`, dead-letter topic `deadletter.tenants.events`, publisher AppId `eventstore`, subscriber AppId `sample`. [Source: `_bmad-output/project-context.md#DAPR`; `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`; `deploy/dapr/pubsub.yaml`]
- `MapTenantEventSubscription()` maps `/tenants/events` and returns `200 OK` for processed, duplicate, unknown, or intentionally unhandled events, but returns a server error for invalid payloads so DAPR can redeliver. [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`; `docs/idempotent-event-processing.md`]
- `TenantEventProcessor` deduplicates by `MessageId`, skips unknown event types, rejects payload/aggregate tenant mismatches, and removes failed message IDs so invalid/failed deliveries can retry. [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`; `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`]
- `TenantProjectionEventHandler` updates `TenantLocalState` through `ITenantProjectionStore` with per-tenant locking and stores bounded `LastEvent` metadata. This metadata is diagnostic, not a durable audit log. [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`; `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`]
- The Sample `/access/{tenantId}/{userId}` endpoint reads only local projection state and fails closed for missing tenants, disabled/unknown status, missing membership, `TenantRole.Unknown`, and out-of-range roles. It does not call Tenants, EventStore, `DaprClient`, or `HttpClient` synchronously. [Source: `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`; `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`]
- Query read models are projections, not authoritative write state. A stale `GetUserTenants` self-lookup can briefly return the previous membership; this is read-only visibility and must not be described as write authorization. [Source: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs#GetUserTenants_stale_self_lookup_returns_current_projection_onlyAsync`; `_bmad-output/project-context.md#API Surface`]
- Keep logs and documentation support-safe. Do not include raw JWTs, decoded token payloads, secrets, full serialized event payloads, stack traces, or real tenant/user data.

### Previous Story Intelligence

- Story 8.1 established the quickstart validation pattern, current EventStore command/status routes, local auth assumptions, ULID command IDs, projection read-after-write retry language, and the VSTest socket fallback note. Reuse those assumptions. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md`; `docs/quickstart.md`]
- Story 8.2 established event contract reference validation, at-least-once delivery guidance, aggregate-local `SequenceNumber` boundaries, and no cross-service ordering guarantee. Link to it instead of duplicating all event schema details. [Source: `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md`; `docs/event-contract-reference.md`]
- Story 8.3 established sample walkthrough validation for `MapTenantEventSubscription()`, local projection behavior, `/access`, `/configuration`, support-safe logging, and under-20-lines registration. Use the sample files as current source of truth. [Source: `_bmad-output/implementation-artifacts/8-3-document-the-sample-consuming-service-walkthrough.md`; `docs/sample-consuming-service-walkthrough.md`]
- Story 8.4 established the reactive access demo, corrected demo scripts, and added `AhaMomentDemoDocumentationTests`. It reinforced that the runnable AppHost has one `sample` subscriber and that live proof cannot be claimed if Docker/AppHost prerequisites are unavailable. [Source: `_bmad-output/implementation-artifacts/8-4-produce-the-reactive-access-aha-moment-demo.md`; `docs/demo.md`]
- Recent commits show Stories 8.1 through 8.4 landed immediately before this story, so quickstart, event contract reference, sample walkthrough, and demo docs are current sources to link rather than rewrite. [Source: `git log --oneline -5`]
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 8.5 implementation.

### Latest Technical Notes

- Current DAPR docs state DAPR pub/sub uses CloudEvents 1.0 wrapping by default, supports declarative/streaming/programmatic subscriptions, and treats successful subscriber processing as a non-error response. [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- Current DAPR docs state pub/sub delivery is at least once; failed delivery or app crashes trigger redelivery attempts until successful delivery, subject to component and resiliency behavior. [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- Current DAPR API docs state subscriber `2xx` responses are success by default, `RETRY` requests redelivery, `DROP` logs and drops, `404` drops, and other status codes are retried. [Source: DAPR Docs, Pub/sub API reference](https://docs.dapr.io/reference/api/pubsub_api/)
- Current DAPR docs state dead-letter topics forward undeliverable messages after failures and should usually be paired with retry resiliency policy. Tenants component YAML enables `deadletter.tenants.events`; do not document it as automatic business correction. [Source: DAPR Docs, Dead letter topics](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/)
- Current Aspire dashboard docs describe resource state/endpoints/health, traces, structured logs, metrics, and configuration visibility. It is appropriate as support evidence, but docs should warn against recording secrets or sensitive runtime data. [Source: Aspire Dashboard](https://aspire.dev/dashboard/)

### Existing Files Likely to Touch

- `docs/cross-aggregate-timing.md`: primary documentation target.
- `README.md`: update docs/navigation if the timing guide remains omitted.
- `docs/demo.md`, `docs/event-contract-reference.md`, `docs/idempotent-event-processing.md`, `docs/sample-consuming-service-walkthrough.md`: adjust links or short references only if the timing guide shape changes.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`: new source-backed documentation validation.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs`, `EventContractReferenceDocumentationTests.cs`, and `SampleConsumingServiceWalkthroughDocumentationTests.cs`: update only if cross-doc link assertions need to include the timing guide.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if documentation validation evidence continues being recorded there.

### Project Structure Notes

- Alignment: Story 8.5 belongs in `docs/`, `README.md` navigation, and existing documentation-test areas. It should not change domain behavior, command contracts, projection semantics, DAPR component names, package versions, or production deployment posture unless validation exposes a concrete source drift bug.
- Boundary: do not implement the planned synchronous authorization plugin. It is future optional behavior and should be described only as a future enforcement path.
- Boundary: do not add a new broker/database dependency to the Client or Sample packages to make projection timing "stronger." Scaled-out durable projection storage remains a consumer-owned implementation choice behind `ITenantProjectionStore`.
- Boundary: do not create screenshots, videos, or new demo scripts for this story unless needed to validate timing docs; Story 8.4 owns the demo asset.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.5: Document Cross-Aggregate Timing and Eventual Consistency`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Journey 4: Alex - First Error`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#API Surface`]
- [Source: `docs/cross-aggregate-timing.md`]
- [Source: `docs/quickstart.md`]
- [Source: `docs/event-contract-reference.md`]
- [Source: `docs/idempotent-event-processing.md`]
- [Source: `docs/sample-consuming-service-walkthrough.md`]
- [Source: `docs/demo.md`]
- [Source: `README.md`]
- [Source: `Hexalith.EventStore/docs/concepts/command-lifecycle.md`]
- [Source: `Hexalith.EventStore/docs/guides/configuration-reference.md#Command Concurrency`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`]
- [Source: `deploy/dapr/pubsub.yaml`]
- [Source: `samples/Hexalith.Tenants.Sample/Program.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`]
- [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`]
- [Source: `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`]
- [Source: `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventSubscriptionEndpointsTests.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`]
- [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- [Source: DAPR Docs, Pub/sub API reference](https://docs.dapr.io/reference/api/pubsub_api/)
- [Source: DAPR Docs, Dead letter topics](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/)
- [Source: Aspire Dashboard](https://aspire.dev/dashboard/)

## Validation Checklist Results

- Story foundation: PASS. Story statement and all five Epic 8.5 acceptance criteria are preserved.
- Scope control: PASS. The story limits implementation to cross-aggregate timing documentation, navigation/link maintenance, source-backed doc tests, and evidence recording; it excludes Story 8.6 compensating-command scope and future auth-plugin implementation.
- Architecture/source context: PASS. The story cites EventStore command lifecycle/status, DAPR pub/sub semantics, Tenants Client subscription/processor/projection code, Sample access endpoint behavior, DAPR component YAML, and Epic/PRD/architecture source documents.
- Reinvention prevention: PASS. The story directs the developer to audit and correct existing `docs/cross-aggregate-timing.md` instead of creating a parallel timing guide.
- Wrong-library/version prevention: PASS. The story keeps repo-pinned .NET/DAPR/Aspire/testing versions and uses external DAPR/Aspire docs only to confirm current behavior.
- File-location prevention: PASS. Expected changes are limited to `docs/`, README navigation, existing documentation tests, and optional validation evidence.
- Regression prevention: PASS. The story calls out command-status lifecycle, subscriber processing as asynchronous, projection/read-model lag, no cross-service ordering, and current single-sample AppHost behavior.
- Security/privacy prevention: PASS. The story forbids raw tokens, secrets, full payload logs, stack traces, and sensitive tenant/user data in documentation, examples, diagnostics, and evidence.
- Validation evidence: PASS. The story requires a new source-backed documentation test plus focused Client/Sample tests and clearly separates live AppHost proof from source-backed validation when infrastructure is unavailable.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Red documentation tests failed against the prior guide (5/5 failures), proving missing command-status, source-backed, safety, and README navigation coverage.
- 2026-06-01: VSTest built focused Server/Client/Sample test projects but aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit runner fallback was used.
- 2026-06-01: Live AppHost timing proof was not run because Docker API access was denied and `dotnet aspire` was unavailable. DAPR CLI/runtime were present (`1.17.1`/`1.17.8`).
- 2026-06-01: Senior developer review found and fixed a DAPR retry/dead-letter source-backing gap by adding local/production `resiliency.yaml` anchors and regression assertions.
- 2026-06-01: Senior review validation passed after fixes: Server.Tests build 0 warnings/errors; `CrossAggregateTimingDocumentationTests` 7/7; documentation namespace 33/33; focused Client tests 22/22; focused Sample tests 23/23.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Rewrote `docs/cross-aggregate-timing.md` as the source-backed timing-window guide with state authority, command status, subscriber propagation, failure/recovery, stale projection, fail-closed, and diagnostic guidance.
- Added source-backed documentation validation tests and linked the timing guide from README, event contract reference, and idempotent event processing docs.
- Validated focused documentation, Client, and Sample timing anchors plus full direct xUnit regression. Live infrastructure proof was explicitly not claimed because Docker/Aspire prerequisites were unavailable.
- Senior developer review completed. The DAPR retry/dead-letter source-backing gap was auto-fixed and no critical issues remain.

### File List

- README.md
- _bmad-output/implementation-artifacts/8-5-document-cross-aggregate-timing-and-eventual-consistency.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- docs/cross-aggregate-timing.md
- docs/event-contract-reference.md
- docs/idempotent-event-processing.md
- tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex  
Date: 2026-06-01  
Outcome: Approve after auto-fix

### Review Findings

- [x] [AI-Review][Medium] `docs/cross-aggregate-timing.md` described subscriber redelivery/dead-letter behavior but only cited `pubsub.yaml`; the retry behavior is actually anchored by local and production `resiliency.yaml`. Fixed the guide to cite both resiliency files, clarified the dead-letter wording, and added documentation-test assertions for the `pubsub` inbound retry policy.
- [x] [AI-Review][Low] Story 8.5 validation evidence had stale counts from before the QA documentation-test additions. Updated the test summary and review record to match the current 7-test timing class and 33-test documentation namespace.

### Acceptance Criteria Validation

- AC1: PASS. The guide explains command submission, aggregate/domain handling, event persistence, command status, Tenants query projections, DAPR publication, subscriber processing, local projections, and authority boundaries.
- AC2: PASS. The Mermaid sequence diagram shows command submission through subscriber projection update, separates command status polling from subscriber processing, and avoids synchronous subscriber enforcement.
- AC3: PASS. Security-critical guidance tells current MVP consumers to design for eventual consistency and fail closed, and frames the EventStore authorization plugin as future/optional.
- AC4: PASS. Stale data, retries, local projection rebuild/catch-up, support-safe diagnostics, and no `Thread.Sleep`/fixed-delay correctness guidance are covered and tested.
- AC5: PASS. Source-backed documentation tests now bind command statuses, pub/sub/dead-letter component settings, resiliency retry settings, source-file citations, unsafe content exclusions, and related navigation links.

### Git and File List Validation

- Story File List covers all story-related changes: README, timing/event/idempotency docs, documentation tests, story file, sprint status, and test summary.
- `_bmad-output/story-automator/orchestration-7-20260601-143204.md` remains an unrelated dirty file and was not changed during review.

### Documentation References Checked

- DAPR official docs confirm at-least-once pub/sub delivery, redelivery on failed delivery/app crash, 2xx subscriber success behavior, and dead-letter topics paired with retry policy guidance.
- Aspire MCP lookup was unavailable during review; web fallback confirmed the Aspire dashboard surfaces resources, logs, traces, configuration, and potentially sensitive runtime data.

### Review Validation

- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` - PASS, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CrossAggregateTimingDocumentationTests -parallel none -noLogo -noColor` - PASS, 7 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` - PASS, 33 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventProcessorTests -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventSubscriptionEndpointsTests -parallel none -noLogo -noColor` - PASS, 22 total, 0 failed, 0 skipped.
- `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -parallel none -noLogo -noColor` - PASS, 23 total, 0 failed, 0 skipped.

## Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-06-01 | 0.1 | Initial story context created | GPT-5 Codex |
| 2026-06-01 | 1.0 | Implemented source-backed timing guide, navigation links, documentation validation tests, and validation evidence | GPT-5 Codex |
| 2026-06-01 | 1.1 | Senior review added DAPR resiliency source anchors/tests, corrected validation evidence, and marked story done | GPT-5 Codex |
