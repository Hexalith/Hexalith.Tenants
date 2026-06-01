---
created: 2026-06-01
source_story_key: 8-3-document-the-sample-consuming-service-walkthrough
baseline_commit: 6541bddf3e63d7d74d3176d1f3797d242477d777
---

# Story 8.3: Document the Sample Consuming Service Walkthrough

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer adopting Tenants,
I want a guided walkthrough of the sample consuming service,
so that I can copy the event subscription and access-enforcement pattern into my own service.

## Acceptance Criteria

1. Given the sample service from the integration epic is available, when a developer reads the walkthrough, then it explains package references, DI registration, tenant event subscription, local projection updates, and access-enforcement behavior, and it points to the exact sample files or snippets that implement each step.
2. Given the sample registers tenant event handlers, when the walkthrough describes setup, then it demonstrates the under-20-lines event-handler registration target, and it distinguishes reusable package setup from sample-only code.
3. Given the sample processes access events, when the walkthrough explains behavior, then it shows how user add, remove, role-change, tenant disable, tenant enable, and configuration events update consumer state, and it explains eventual consistency rather than presenting the local projection as synchronous truth.
4. Given the developer wants to adapt the sample, when they follow the walkthrough, then the guide identifies which code is safe to copy, which pieces are application-specific, and which identifiers or secrets must be supplied by the deployment, and it avoids exposing raw tokens or sensitive tenant/user data.
5. Given sample walkthrough validation runs, when snippets are compiled or checked against the sample, then the documented code remains synchronized with the sample implementation, and broken snippets fail documentation validation.

## Tasks / Subtasks

- [x] Create a dedicated sample consuming service walkthrough document. (AC: 1, 2, 3, 4)
  - [x] Add `docs/sample-consuming-service-walkthrough.md` as the primary deliverable. Do not use Story 8.3 to expand the "aha moment" demo; Story 8.4 owns demo production.
  - [x] Start from the actual sample project, not a new example: `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj`, `Program.cs`, `Handlers/SampleLoggingEventHandler.cs`, `Endpoints/AccessCheckEndpoints.cs`, and `Endpoints/TenantConfigurationEndpoints.cs`.
  - [x] Explain package/project references from `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj`: the sample consumes `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` through project references in this repository; NuGet consumers install the matching packages.
  - [x] Include a file map table that points each walkthrough step to its exact source file: DI/subscription setup, custom logging handler, local projection access endpoint, configuration endpoint, AppHost sample registration, and sample tests.
  - [x] Keep the walkthrough English-only per MVP documentation requirements.

- [x] Document the reusable subscription setup without inventing a parallel integration path. (AC: 1, 2)
  - [x] Show the real `Program.cs` registration path: `AddHexalithTenants()`, selected `AddTenantEventHandler<TEvent, THandler>()` calls, `UseCloudEvents()`, `MapSubscribeHandler()`, and `MapTenantEventSubscription()`.
  - [x] Demonstrate that the tenant event registration stays under 20 meaningful lines using the same line-count definition as `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`.
  - [x] Distinguish reusable package setup from sample-only code: `AddHexalithTenants`, typed handler registration, CloudEvents middleware, DAPR subscribe handler, and `MapTenantEventSubscription` are reusable; `SampleLoggingEventHandler`, `/access/{tenantId}/{userId}`, and `/configuration/{tenantId}/sample` are sample-specific teaching surfaces.
  - [x] Explain `HexalithTenantsOptions` defaults: DAPR pub/sub component `pubsub`, shared topic `tenants.events`, and programmatic subscription endpoint `/tenants/events`.
  - [x] State that consumers filter by event type through typed handlers and must not create one DAPR topic per tenant event type.

- [x] Document how local projection updates drive access and configuration behavior. (AC: 1, 3)
  - [x] Explain that `TenantProjectionEventHandler` is the built-in handler registered by `AddHexalithTenants()` and that it updates `TenantLocalState` through `ITenantProjectionStore`.
  - [x] Cover every built-in projection event relevant to the acceptance criteria: `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantDisabled`, `TenantEnabled`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`. Also mention `TenantCreated` and `TenantUpdated` as lifecycle/metadata setup events.
  - [x] Explain `/access/{tenantId}/{userId}` from `AccessCheckEndpoints.cs`: it reads the local projection, grants known active members with authorized roles, denies non-members, denies disabled or unknown tenant status, and fails closed for `TenantRole.Unknown` or out-of-range roles.
  - [x] Explain `/configuration/{tenantId}/sample` from `TenantConfigurationEndpoints.cs`: it reads the local projection, filters keys by the `sample.` prefix, strips the prefix for the response, and ignores unrelated namespaces such as `billing.plan`.
  - [x] Make eventual consistency explicit: EventStore remains the durable source of truth; the sample projection catches up asynchronously from `tenants.events`; endpoint responses are local projection state and must not be described as synchronous Tenants truth.
  - [x] Link to `docs/event-contract-reference.md` for schemas and envelope metadata, `docs/idempotent-event-processing.md` for duplicate delivery patterns, and `docs/cross-aggregate-timing.md` for propagation timing once Story 8.5 updates it.

- [x] Provide adaptation guidance for real consuming services. (AC: 4)
  - [x] Add a "Safe to copy" section: package references, `AddHexalithTenants()`, typed handler registration, DAPR subscription middleware/endpoint, handler interface shape, `ITenantProjectionStore` abstraction, and idempotent dictionary set/remove projection operations.
  - [x] Add an "Application-specific" section: custom event handlers, local endpoint routes, role-to-capability policy, configuration namespace prefix, durable projection store choice, durable deduplication store choice, logging levels, and side effects such as notifications or outbox writes.
  - [x] Add a "Deployment supplied" section: DAPR AppId, pub/sub component name, `tenants.events` topic access, OIDC/JWT tokens, tenant IDs, user IDs, configuration keys, secrets, and any production storage connection strings.
  - [x] Preserve security posture: do not paste raw bearer tokens, decoded JWT payloads, secrets, full event payload logs, or sensitive tenant/user data into docs. Use redacted placeholders only for tokens and secrets.
  - [x] Call out the current sample logging behavior: `SampleLoggingEventHandler` logs tenant ID plus message/correlation metadata and intentionally does not log the sample user ID or role.
  - [x] State that the default `InMemoryTenantProjectionStore` is suitable for local and single-instance samples; scaled-out production consumers should register a durable `ITenantProjectionStore` before `AddHexalithTenants()` and use bounded/shared deduplication for side effects.

- [x] Sync navigation and cross-links without absorbing adjacent Epic 8 scope. (AC: 1, 4)
  - [x] Update `README.md` docs/project-structure links if needed so the new walkthrough is discoverable alongside quickstart, event contract reference, idempotency, and demo docs.
  - [x] Update `docs/quickstart.md#consume-tenant-events-in-your-service` to link to the new walkthrough instead of trying to carry all sample details inline.
  - [x] Optionally add a short link from `docs/demo.md` to the walkthrough for "how the sample works"; do not add new demo steps or scripts in this story.
  - [x] Do not duplicate event contract tables, idempotency implementation details, cross-aggregate timing documentation, or compensating-command guidance. Link to the dedicated docs owned by Stories 8.2, 8.5, and 8.6.

- [x] Add source-backed documentation validation. (AC: 5)
  - [x] Add `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs` or an equivalent focused documentation test in the existing documentation-test area.
  - [x] Verify the walkthrough references all required sample files and public source files used in the walkthrough.
  - [x] Verify the walkthrough includes the current subscription setup calls from `Program.cs`: `AddHexalithTenants`, `AddTenantEventHandler<UserAddedToTenant, SampleLoggingEventHandler>`, `AddTenantEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>`, `AddTenantEventHandler<TenantDisabled, SampleLoggingEventHandler>`, `UseCloudEvents`, `MapSubscribeHandler`, and `MapTenantEventSubscription`.
  - [x] Verify the documented registration target remains under 20 meaningful lines using the same predicate as `SampleRegistrationTests`.
  - [x] Verify every event handled by `TenantProjectionEventHandler` is mentioned in the walkthrough, especially the acceptance-critical user add/remove/role-change, tenant disable/enable, and configuration set/remove events.
  - [x] Verify the walkthrough names `pubsub`, `tenants.events`, `/tenants/events`, `/access/{tenantId}/{userId}`, `/configuration/{tenantId}/sample`, `ITenantProjectionStore`, `TenantLocalState`, and eventual consistency.
  - [x] Verify fenced C# snippets are either exact source excerpts or source-checked for the required calls. Broken or drifted snippets must fail the test.
  - [x] Verify the walkthrough does not include raw bearer tokens, JWT-like `eyJ...` values, production secrets, or guidance to log full event payloads.

- [x] Run validation and record evidence. (AC: 5)
  - [x] Run a focused build for the documentation/sample test surface, preferably `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` if restore is already complete.
  - [x] Run the focused documentation tests. If `dotnet test` hits the sandbox VSTest socket issue seen in Stories 8.1 and 8.2, use the direct xUnit runner fallback and record the limitation.
  - [x] Run `samples/Hexalith.Tenants.Sample.Tests` focused tests if any sample-source assumptions are touched.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` only if this repository continues recording documentation validation evidence there.
  - [x] Do not require Docker, DAPR, AppHost, or live pub/sub execution for this documentation story unless a prepared environment is available; source-backed documentation tests are the required validation gate.

## Dev Notes

### Source Context

- Epic 8 objective: developers can adopt through validated documentation and demo evidence. Story 8.3 owns the sample consuming service walkthrough; Story 8.4 owns the reactive access demo; Story 8.5 owns cross-aggregate timing; Story 8.6 owns compensating commands. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.3 acceptance requires package references, DI registration, tenant event subscription, local projection updates, access enforcement, under-20-lines handler registration, copy/adapt/deployment guidance, sensitive-data avoidance, and validation that snippets stay synchronized with sample source. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.3: Document the Sample Consuming Service Walkthrough`]
- PRD FR62 requires a sample consuming service demonstrating event subscription and access enforcement. Journey 3 emphasizes under-20-lines registration, local per-service projections, no cross-service ordering guarantee, and configuration-event utility. [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`; `_bmad-output/planning-artifacts/prd.md#Journey 3: Alex Integrates Tenant Events Across Services`]
- Architecture maps Epic 8 documentation and adoption work to `docs/`, `README.md`, and the sample project. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- The sample service already exists under `samples/Hexalith.Tenants.Sample/` and is wired into the AppHost as DAPR AppId `sample`. The AppHost gives the sample a DAPR sidecar with a pub/sub reference only; it does not give the sample Tenants actor state-store access. [Source: `src/Hexalith.Tenants.AppHost/Program.cs`; `src/Hexalith.Tenants.AppHost/HexalithTenantsSample.cs`]
- `samples/Hexalith.Tenants.Sample/Program.cs` registers `AddHexalithTenants()`, three sample logging handlers, CloudEvents middleware, DAPR subscribe handler, tenant event subscription endpoint, access endpoint, configuration endpoint, and `/alive`/`/health`.
- `SampleLoggingEventHandler` currently handles only `UserAddedToTenant`, `UserRemovedFromTenant`, and `TenantDisabled`. Do not claim the sample custom logger handles role changes, tenant enable, or configuration events. Those are handled by the built-in `TenantProjectionEventHandler` through the local projection.
- `TenantProjectionEventHandler` handles `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`.
- `AccessCheckEndpoints.CheckAccessAsync` depends on `ITenantProjectionStore`, not `HttpClient` or `DaprClient`; it reads `TenantLocalState` and fails closed for disabled/unknown tenant status, non-members, `TenantRole.Unknown`, and out-of-range roles.
- `TenantConfigurationEndpoints.GetSampleConfigurationAsync` filters only keys beginning with `sample.` and hides unrelated namespaces.
- `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs` already enforces the under-20-lines registration target for meaningful tenant-registration lines.
- `docs/quickstart.md` already has a short "Consume Tenant Events in Your Service" section. Story 8.3 should turn the detailed explanation into a dedicated walkthrough and link to it.
- `docs/demo.md`, `README.md` demo links, and `scripts/demo.*` already exist from older/archived work, but current sprint status still splits Story 8.3 from Story 8.4. Treat existing demo artifacts as repo state, not as permission to merge demo scope into this story.

### Technical Guardrails

- Use repo-pinned versions and package families from project context. Do not bump .NET, DAPR, Aspire, xUnit, Shouldly, or package references for this documentation story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Do not add a new sample service, new DAPR topic, new command endpoint, new projection API, new broker/database dependency, or direct synchronous Tenants lookup for access checks.
- DAPR resource naming is contractual: pub/sub component `pubsub`, topic `tenants.events`, subscriber AppId `sample`, Tenants AppId `tenants`, EventStore AppId `eventstore`. [Source: `_bmad-output/project-context.md#DAPR`; `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`; `deploy/dapr/pubsub.yaml`]
- Event publication is at-least-once and eventually consistent. Use `MessageId` for deduplication, `SequenceNumber` only as aggregate-local metadata, and avoid cross-service ordering claims. [Source: `docs/event-contract-reference.md#Event Delivery Model`; `docs/idempotent-event-processing.md#Local Projection Semantics`]
- Keep logging support-safe. Existing tests assert sample logging does not include raw user ID or role for user events. Preserve that expectation when documenting troubleshooting and diagnostics. [Source: `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs`]
- Use xUnit v3 and Shouldly for any new tests. Do not use `Assert.*`; do not add per-file `using Xunit;` in test projects that already provide global usings. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Previous Story Intelligence

- Story 8.1 established the quickstart validation-test pattern: read markdown from repo root, assert paths/routes/source-backed terms, parse fenced JSON where relevant, and record infrastructure limitations without claiming live execution. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md#Dev Notes`]
- Story 8.2 established the event contract reference validation-test pattern and warns not to absorb Story 8.3, 8.5, or 8.6 scope into one document. Reuse the source-backed documentation-test approach. [Source: `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md#Dev Notes`]
- Stories 8.1 and 8.2 both recorded that `dotnet test` can build but VSTest may abort in this sandbox with `SocketException (13): Permission denied`; direct xUnit runner execution worked for focused tests. Use the same fallback if it recurs. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md#Debug Log References`; `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md#Debug Log References`]
- Recent commits show Story 8.1 and 8.2 landed immediately before this story, so quickstart and event contract docs are current sources to link rather than duplicate. [Source: `git log --oneline -5`]

### Latest Technical Notes

- Current DAPR docs confirm pub/sub uses CloudEvents 1.0 and provides at-least-once delivery semantics; the walkthrough should reinforce idempotent handlers and eventual consistency. [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- Current DAPR docs describe programmatic subscriptions and show ASP.NET Core subscription setup with topic attributes plus `MapSubscribeHandler`; the Tenants Client wraps the topic mapping through `MapTenantEventSubscription()`, so use the repository API instead of teaching raw DAPR attributes. [Source: DAPR Docs, Declarative, streaming, and programmatic subscription types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/)
- Current DAPR .NET SDK docs list .NET 10 support and DAPR ASP.NET Core package usage. This is contextual only; keep the repository-pinned DAPR SDK `1.17.9` and package setup. [Source: DAPR Docs, .NET SDK](https://docs.dapr.io/developing-applications/sdks/dotnet/)

### Existing Files Likely to Touch

- `docs/sample-consuming-service-walkthrough.md`: new primary walkthrough.
- `docs/quickstart.md`: likely link/update in the "Consume Tenant Events in Your Service" section.
- `README.md`: likely add the walkthrough to docs/project structure or adoption links.
- `docs/demo.md`: optional short cross-link only; do not change demo flow.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs`: likely new validation test.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if validation evidence continues to be recorded there.

### Project Structure Notes

- Alignment: Story 8.3 belongs in `docs/`, README/quickstart navigation if needed, and existing documentation-test projects. The sample project is source evidence, not a target for new behavior unless validation uncovers source/doc drift.
- Detected conflict: archived legacy Story 8.3 bundled demo scripts, changelog, contributing docs, and project documentation. Current sprint split makes this story narrower: sample consuming service walkthrough only.
- Detected drift risk: README and `docs/demo.md` already imply demo/script readiness from old work while sprint status still has 8.4 backlog. Do not mark 8.4 work done from this story.
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 8.3 implementation.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.3: Document the Sample Consuming Service Walkthrough`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `samples/Hexalith.Tenants.Sample/Program.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj`]
- [Source: `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`]
- [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`]
- [Source: `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs`]
- [Source: `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs`]
- [Source: `docs/quickstart.md#Consume Tenant Events in Your Service`]
- [Source: `docs/event-contract-reference.md#Event Delivery Model`]
- [Source: `docs/idempotent-event-processing.md#Local Projection Semantics`]
- [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- [Source: DAPR Docs, Declarative, streaming, and programmatic subscription types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/)
- [Source: DAPR Docs, .NET SDK](https://docs.dapr.io/developing-applications/sdks/dotnet/)

## Validation Checklist Results

- Story foundation: PASS. Story statement and all five Epic 8.3 acceptance criteria are preserved.
- Scope control: PASS. The story explicitly excludes Story 8.4 demo production and avoids duplicating event contract, idempotency, cross-aggregate timing, or compensating-command scope.
- Architecture/source context: PASS. The story cites sample source files, Client subscription/projection APIs, AppHost sample registration, DAPR resource names, and existing sample/documentation tests.
- Reinvention prevention: PASS. The story directs the developer to document the existing sample and Client package APIs instead of creating another sample, topic, endpoint, or subscription abstraction.
- Wrong-library/version prevention: PASS. The story keeps repository-pinned .NET/DAPR/Aspire/testing versions and treats external DAPR docs as conceptual confirmation only.
- File-location prevention: PASS. Expected changes are limited to `docs/`, README/quickstart navigation, existing documentation tests, and optional validation evidence.
- Regression prevention: PASS. The story calls out the difference between custom sample logging and built-in projection handling, fail-closed access behavior, support-safe logging, local projection eventual consistency, and scaled-out durable-store boundaries.
- Validation evidence: PASS. The story requires source-backed documentation tests that keep snippets synchronized with sample implementation and fail on sensitive-token or full-payload logging guidance.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (dev-story implementation)

### Debug Log References

- Resolved `bmad-create-story` workflow customization: no activation prepend/append steps; persistent fact `_bmad-output/project-context.md` loaded.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: `planning_artifacts` and `implementation_artifacts` resolved under `_bmad-output/`.
- Loaded sprint status and selected explicit Story 8.3 from current Epic 8 backlog key `8-3-document-the-sample-consuming-service-walkthrough`.
- Loaded Epic 8 source, PRD documentation/adoption requirements, architecture structure guidance, Story 8.1, Story 8.2, archived legacy Story 8.3, current quickstart/demo/event/idempotency docs, sample source files, Client subscription/projection files, and sample/documentation-test patterns.
- Researched current DAPR pub/sub, programmatic subscription, and .NET SDK docs for latest conceptual guidance.
- Noted unrelated dirty worktree file `_bmad-output/story-automator/orchestration-7-20260601-143204.md`; it was not modified by story creation.
- Resolved `bmad-dev-story` workflow customization: no activation prepend/append steps; persistent fact `_bmad-output/project-context.md` loaded.
- Marked Story 8.3 and sprint status in-progress, preserving existing `baseline_commit`.
- Added source-backed documentation tests first; `dotnet test` built but VSTest aborted with sandbox `SocketException (13): Permission denied`, so validation used the direct xUnit v3 runner.
- Validation passed: Server.Tests documentation namespace 17/17, Server.Tests full 697/697, Sample.Tests 31/31, Contracts.Tests 105/105, Client.Tests 92/92, Testing.Tests 181/181, IntegrationTests 217 total with 26 prerequisite-gated skips, and solution build 0 warnings/errors.
- Senior developer review loaded story, checklist, git status, DAPR docs, source files, sample tests, and generated walkthrough/tests.
- Review auto-fixed two documentation precision issues: `/tenants/events` is now documented as the `MapTenantEventSubscription()` route rather than an `HexalithTenantsOptions` default, and `docs/demo.md` no longer advertises the stale "12 lines of DI config" wording.
- Review validation passed after fixes: Server.Tests documentation class 6/6 via direct xUnit runner; Sample focused endpoint/registration/handler tests 30/30 via direct xUnit runner; focused Server.Tests build and Sample.Tests build both passed with 0 warnings/errors. `dotnet test` built but VSTest again aborted with sandbox `SocketException (13): Permission denied`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Created Story 8.3 as a source-backed documentation story for the existing sample consuming service walkthrough.
- Preserved the current Epic 8 split and avoided importing old archived Story 8.3 demo/changelog/contributing scope.
- Captured source-backed guardrails for sample DI registration, DAPR subscription, local projection updates, access/configuration endpoints, adaptation guidance, sensitive-data handling, and documentation validation.
- Added `docs/sample-consuming-service-walkthrough.md` covering the existing sample's package references, DI/subscription path, projection updates, access/configuration endpoints, AppHost registration, adaptation guidance, and security posture.
- Updated README, quickstart, and demo navigation so the walkthrough is discoverable without expanding Story 8.4 demo scope.
- Added documentation validation tests that pin required source files, subscription calls, under-20-line registration, projection event coverage, snippet synchronization, and sensitive-data exclusions.
- Recorded validation evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Definition of Done: PASS. All tasks/subtasks are complete, acceptance criteria are covered, source-backed tests pass, file list is current, and Story 8.3 is ready for review.
- Senior developer review completed. All review findings were auto-fixed; no critical issues remain.

### File List

- `README.md`
- `docs/demo.md`
- `docs/quickstart.md`
- `docs/sample-consuming-service-walkthrough.md`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/8-3-document-the-sample-consuming-service-walkthrough.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex  
Date: 2026-06-01  
Outcome: Approve after auto-fix

### Review Findings

- [x] [AI-Review][Medium] The walkthrough grouped `/tenants/events` under `HexalithTenantsOptions` defaults, but source shows options only define `PubSubName` and `TopicName`; the endpoint route is mapped by `MapTenantEventSubscription()`. Fixed in `docs/sample-consuming-service-walkthrough.md` and pinned by documentation tests.
- [x] [AI-Review][Low] `docs/demo.md` still said the sample used "12 lines of DI config," which drifted from the current source-backed under-20-lines registration target. Fixed the navigation wording and added a regression assertion.

### Acceptance Criteria Validation

- AC1: PASS. Walkthrough covers package/project references, DI/subscription setup, tenant event subscription, local projection updates, access behavior, configuration behavior, and exact source-file map.
- AC2: PASS. Registration snippet matches `samples/Hexalith.Tenants.Sample/Program.cs`, remains under 20 meaningful lines, and separates reusable package setup from sample-only endpoints/handler.
- AC3: PASS. Projection behavior covers user add/remove/role-change, tenant disable/enable, configuration set/remove, lifecycle events, and eventual consistency.
- AC4: PASS. Safe-to-copy, application-specific, deployment-supplied, durable-store, deduplication, and sensitive-data guidance are present.
- AC5: PASS. Source-backed documentation tests verify required files, registration calls, projection event coverage, snippet synchronization, navigation wording, and sensitive-token/logging exclusions.

### Git and File List Validation

- Story File List covers all story-related changes: README, quickstart, demo, walkthrough, documentation test, story file, sprint status, and test summary.
- `_bmad-output/story-automator/orchestration-7-20260601-143204.md` remains an unrelated pre-existing dirty file and was not changed during review.

### Validation Evidence

- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` — PASS, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.SampleConsumingServiceWalkthroughDocumentationTests -parallel none -noLogo -noColor` — PASS, 6 total, 0 failed, 0 skipped.
- `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` — PASS, 0 warnings, 0 errors.
- `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -class Hexalith.Tenants.Sample.Tests.Handlers.SampleLoggingEventHandlerTests -parallel none -noLogo -noColor` — PASS, 30 total, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~SampleConsumingServiceWalkthroughDocumentationTests -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` — BUILT, then VSTest aborted on sandbox `SocketException (13): Permission denied`; direct xUnit runner used as fallback.

## Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-06-01 | 0.1 | Created Story 8.3 context for the sample consuming service walkthrough and validation. | GPT-5 Codex |
| 2026-06-01 | 1.0 | Implemented sample consuming service walkthrough, navigation links, source-backed documentation validation, and validation evidence. | GPT-5 Codex |
| 2026-06-01 | 1.1 | Senior developer review auto-fixed documentation precision issues and added regression assertions. | GPT-5 Codex |
