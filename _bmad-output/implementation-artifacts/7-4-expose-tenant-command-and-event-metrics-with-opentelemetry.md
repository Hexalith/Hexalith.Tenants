---
baseline_commit: 0f94de4
---

# Story 7.4: Expose Tenant Command and Event Metrics with OpenTelemetry

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want tenant command and event metrics through OpenTelemetry,
so that I can observe latency, failures, and processing health in production.

## Acceptance Criteria

1. Given tenant commands are submitted, when command processing completes, rejects, or fails, then OpenTelemetry spans or metrics record command latency, smoke-level telemetry presence checks run in the normal implementation lane, and p95 command duration evidence is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.
2. Given tenant events are published or projected, when event processing completes, retries, or fails, then OpenTelemetry spans or metrics record event processing latency and outcome, smoke-level telemetry presence checks run in the normal implementation lane, and p95 event publication duration evidence is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.
3. Given query endpoints are called, when read model queries complete, then query latency is observable, smoke-level telemetry presence checks run in the normal implementation lane, and p95 query duration evidence for single-page result sets is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.
4. Given telemetry is emitted, when logs and spans are inspected, then they include support-safe correlation, tenant, domain, aggregate, causation, command/event type, and stage metadata, and they do not include command payloads, event payloads, tokens, secrets, or PII.
5. Given telemetry tests or manual verification run, when successful, rejected, failed, and delayed operations are exercised, then metrics and structured logs distinguish normal domain rejections from infrastructure failures.

## Tasks / Subtasks

- [x] Reconcile current telemetry before changing code. (AC: 1-5)
  - [x] Confirm `src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs` defines the single Tenants activity source, and do not create a second source, meter, package, or telemetry abstraction.
  - [x] Confirm `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs` already records `tenants.command.duration`, `tenants.projection.query.duration`, and `tenants.projection.write.conflicts`.
  - [x] Confirm `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` registers `.AddMeter("Hexalith.Tenants")`, `.AddSource("Hexalith.Tenants")`, and `.AddSource("Hexalith.EventStore")`.
  - [x] Confirm EventStore publication spans/logs already exist in `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs` through `EventStoreActivitySource.EventsPublish` and EventIds `3100`/`3101`.
  - [x] Treat the existing implementation as partial coverage; complete gaps instead of replacing the telemetry stack.

- [x] Strengthen command telemetry outcome semantics. (AC: 1, 4, 5)
  - [x] Update `DomainServiceRequestHandler.ProcessAsync` so command telemetry distinguishes at least `success`, `rejection`, `noop`, and `failure` outcomes from `DomainResult.IsSuccess`, `IsRejection`, `IsNoOp`, and exceptions.
  - [x] Preserve the existing meaning of infrastructure success where needed, but add an explicit bounded outcome tag/dimension so domain rejections do not look like infrastructure failures.
  - [x] Add support-safe span tags for command stage, correlation ID, tenant, domain, aggregate, causation ID when available from `CommandEnvelope`, and command type.
  - [x] Add or update support-safe structured logs for command outcome and failure classification if existing logs cannot distinguish domain rejections from infrastructure failures during operations.
  - [x] Keep command/event payload bytes, decoded payload JSON, bearer tokens, signing material, user emails, and raw secret values out of logs, spans, and metrics.
  - [x] Keep metric dimensions bounded: command type must remain sanitized through the known-command allow-list; tenant IDs, aggregate IDs, correlation IDs, causation IDs, message IDs, and user IDs must not become metric tags.

- [x] Add Tenants event projection processing telemetry. (AC: 2, 4, 5)
  - [x] Instrument `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs` around the full projection dispatch for `tenants` and `global-administrators` domains.
  - [x] Add a Tenants span such as `Tenants.Event.Project` or `Tenants.Projection.Project` through `TenantActivitySource`, with support-safe tags for stage, tenant, domain, aggregate, projection type/domain, bounded event type summary, event count, correlation ID, and causation status.
  - [x] Add a histogram such as `tenants.event.processing.duration` or an equivalent projection/event-processing duration metric through `TenantMetrics`.
  - [x] Record low-cardinality metric tags only: domain, projection type/category, stage, and outcome are acceptable when sanitized; tenant, aggregate, correlation, causation, message IDs, and event type lists are not acceptable metric tags.
  - [x] Distinguish completed, rejected/unsupported-domain, retry-recovered, retry-exhausted, and failed outcomes where the projection path exposes them.
  - [x] Add or update support-safe structured logs for projection dispatch outcome if existing projection logs cannot tell unsupported-domain/invalid-identity outcomes from infrastructure failures.
  - [x] Preserve `ProjectionDispatcher` fail-closed behavior for unsupported domains and invalid global-administrator projection identity; telemetry must observe these paths without changing HTTP status or response semantics.

- [x] Connect projection retry evidence to event processing outcome without duplicating Epic 5 logic. (AC: 2, 4, 5)
  - [x] Reuse `TenantProjectionWritePolicy` conflict/retry metrics and structured log fields; do not replace `SaveWithOptimisticConcurrencyAsync`, `SaveMergedWithOptimisticConcurrencyAsync`, `ITenantProjectionStateStore`, or `DaprTenantProjectionStateStore`.
  - [x] Ensure retry-exhausted projection writes still throw and surface as failed event-processing telemetry; recovered conflicts must remain observable but must not be tagged as terminal failures.
  - [x] Preserve EventIds `100101` for guarded-save conflicts and `100102` for retry exhaustion.
  - [x] Keep `CausationIdStatus = "unavailable-from-projection-dto"` or equivalent explicit metadata for projection paths because `ProjectionEventDto` does not expose causation IDs.
  - [x] Do not parse raw event payloads to recover causation, tenant, aggregate, user, or event details for telemetry.

- [x] Verify publication latency evidence through existing EventStore telemetry. (AC: 2, 4)
  - [x] Confirm `EventStoreActivitySource.EventsPublish` spans are included by Tenants ServiceDefaults via `.AddSource("Hexalith.EventStore")`.
  - [x] Confirm EventStore `EventPublisher` logs successful publication duration and safe failure diagnostics without payload data.
  - [x] Add Tenants-side smoke verification only if current tests do not prove source registration or publication telemetry visibility; do not fork or edit the EventStore submodule solely to add a duplicate Tenants publisher metric.
  - [x] If true p95 event publication evidence is required, document it as release/scheduled performance evidence and do not make it a normal unit-test gate without explicit approval.

- [x] Preserve and extend query telemetry. (AC: 3, 4, 5)
  - [x] Keep `TenantsProjectionActor.ExecuteQueryAsync` as the query latency instrumentation point.
  - [x] Add bounded query outcome metadata if missing so successful, forbidden/rejected, unknown-query, and infrastructure-failure paths are distinguishable.
  - [x] Keep query type sanitization through the known-query allow-list in `TenantMetrics`; unknown query types must collapse to `unknown`.
  - [x] Keep endpoint/controller telemetry secondary to projection-query telemetry; do not move query authorization or filtering into controllers.

- [x] Add deterministic telemetry tests. (AC: 1-5)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Telemetry/DomainServiceRequestHandlerTelemetryTests.cs` to cover success, domain rejection, no-op, missing processor/failure, sanitized command type, and support-safe tag/dimension behavior.
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs` and `TenantMetricsTests.cs` for query outcome tags and metric sanitization.
  - [x] Add projection-dispatch/event-processing telemetry tests for supported tenant projection, supported global-administrator projection, unsupported domain, invalid global-admin identity, retry-recovered conflict, retry exhaustion, and thrown infrastructure failure where practical.
  - [x] Add at least one deterministic delayed-operation test using a controlled delayed processor or projection fake so duration telemetry is proven by an observable elapsed value rather than only by presence.
  - [x] Capture structured logs with the existing test logger pattern where outcome classification is implemented through logs, and assert domain rejection and infrastructure failure use different bounded outcome/reason fields.
  - [x] Keep telemetry tests in `[Collection("Telemetry")]` or equivalent serialization because `ActivityListener` and `MeterListener` are process-global.
  - [x] Add negative assertions that metrics never contain tenant IDs, aggregate IDs, user IDs, correlation IDs, causation IDs, message IDs, payload values, tokens, or secrets.

- [x] Run focused validation and record evidence accurately. (AC: 1-5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~Telemetry`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~GlobalAdministratorProjectionHandlerTests"`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, project files, ServiceDefaults, or shared docs change.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from application failures.
  - [x] Do not claim p95/NFR threshold compliance from unit tests; record only telemetry presence and bounded-dimension evidence in the story unless a release/scheduled performance run was actually executed.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.4 specifically owns OpenTelemetry visibility for command, event/projection, and query latency/outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.4: Expose Tenant Command and Event Metrics with OpenTelemetry`]
- PRD FR54 requires tenant command latency metrics; FR55 requires event processing metrics; NFR1, NFR2, and NFR3 define 50ms p95 targets for commands, single-page queries, and DAPR event publication. The current corrected Epic 7 plan classifies p95 threshold evidence as release or scheduled performance evidence unless explicitly approved as a blocking CI gate. [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`; `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Artifact: _bmad-output/planning-artifacts/epics.md`]
- Architecture maps Epic 7 observability work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. Observability must use OpenTelemetry and structured logs with support-safe metadata. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/epics.md#Technical Requirements`]
- UX planning is Phase 2, but its support-safe evidence principle still applies: command lifecycle, projection freshness, audit trails, and support references must avoid raw payloads, bearer tokens, stack traces, internal metadata dumps, and sensitive tenant/user data. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Operations and Evidence Principles`]

### Current Repository State

- `src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs` currently exposes `SourceName = "Hexalith.Tenants"`, `Tenants.Command.Process`, `Tenants.Projection.Query`, and tag constants for command type, tenant ID, success, and query type.
- `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs` currently exposes `MeterName = "Hexalith.Tenants"`, histograms for `tenants.command.duration` and `tenants.projection.query.duration`, a counter for `tenants.projection.write.conflicts`, and allow-lists for command/query/projection/write dimensions.
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` already registers `AddMeter("Hexalith.Tenants")`, `AddSource("Hexalith.Tenants")`, and `AddSource("Hexalith.EventStore")`, plus ASP.NET Core, HTTP client, and runtime instrumentation.
- `src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs` currently records command duration and a command span, but its `success` tag means handler completed without throwing. It does not currently distinguish `DomainResult.IsRejection` from true success/no-op for operator outcome analysis.
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs` currently records projection query duration and query spans, including error status on thrown infrastructure failures. It needs outcome clarity for rejected/unknown-query paths if tests show ambiguity.
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs` currently routes `/project` requests to tenant or global-administrator projection handlers and returns safe ProblemDetails for unsupported domains or invalid global-admin identities. It is the best Tenants-owned event/projection dispatch boundary for Story 7.4 instrumentation.
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` already emits support-safe write-conflict/retry metrics and structured logs. Do not duplicate this logic; connect it to higher-level event-processing outcome only where useful.
- EventStore already emits publication spans and logs in `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs`. Tenants ServiceDefaults registers the `Hexalith.EventStore` source, so publication latency can be observed through existing EventStore spans.

### Previous Story Intelligence

- Story 7.1 and 7.2 established Aspire/DAPR hosting and production DAPR configuration. Do not change topology, DAPR component names, service invocation ACLs, or AppHost resource names for telemetry-only work.
- Story 7.3 completed fail-closed production tenant-claim validation. Telemetry must not weaken auth, must not log token material, and should preserve safe 401/403/ProblemDetails behavior.
- Story 5.4 completed projection write conflict diagnostics. Reuse `TenantMetrics`, `TenantActivitySource`, bounded dimensions, and EventIds `100101`/`100102`.
- Archived old Epic 7 telemetry work recorded two important lessons: telemetry listener tests must be serialized, and metric dimensions must remain bounded. Preserve both.
- Recent commits are `feat(story-7.3)`, `feat(story-7.2)`, and `feat(story-7.1)`. Follow their pattern: focused host/service tests, no dependency upgrades, and accurate evidence notes when infrastructure tests are not run.

### Technical Guardrails

- Use repo-pinned versions: .NET SDK `10.0.300`, DAPR SDK `1.17.9`, Aspire `13.3.x`, OpenTelemetry `1.15.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`. Do not upgrade packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`; do not introduce another telemetry framework or custom exporter.
- Keep high-cardinality and sensitive values out of metrics. Tenant, aggregate, correlation, causation, message, event ID, and user identifiers may be support-safe trace/log fields when needed, but must not be metric dimensions.
- Sanitize all user-controlled or externally supplied dimensions by allow-list or bounded mapping. Unknown values collapse to `unknown`.
- Treat command/event payloads as off-limits for telemetry. Do not deserialize payloads only to enrich telemetry.
- Do not modify `Hexalith.EventStore` submodule for this story unless a separate cross-repo decision is explicitly made. Reading EventStore source to understand existing spans/logs is expected.
- Do not claim p95/NFR compliance unless an actual performance/release evidence run has been executed and recorded. Unit tests prove telemetry presence and dimensions, not latency targets.

### Existing Files to Touch

- `src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs`: likely add event/projection span names and tag constants.
- `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`: likely add event/projection processing histogram and bounded outcome sanitizers; adjust command/query dimensions only with compatibility in mind.
- `src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs`: refine command outcome tagging and metrics.
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`: add event/projection dispatch telemetry around supported and fail-closed paths.
- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: refine query outcome tagging if current observability cannot distinguish rejection/unknown/failure.
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`: touch only if source/meter registration is missing after verification.
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/*.cs`: extend in-process telemetry coverage.
- `tests/Hexalith.Tenants.Server.Tests/Projections/*`: extend only where projection-dispatch or retry telemetry needs production-path proof.
- `docs/` or deployment evidence docs: update only if operator telemetry names or verification commands need to be published now.

### Preserve Existing Behavior

- Do not change command routing, query routing, projection write order, retry counts, health endpoint behavior, DAPR component names, AppHost topology, authentication policies, or public REST response shapes.
- Do not convert domain rejections into exceptions or infrastructure failures for the sake of telemetry. Domain rejection is a normal command outcome.
- Do not add tenant/user/payload values to metric dimensions.
- Do not make scheduled performance evidence a CI gate in this story.

### Out of Scope

- New dashboards, Prometheus/Grafana/Datadog configuration, or vendor-specific OTLP collector setup.
- Live DAPR, Redis, broker, OIDC, or Aspire smoke tests unless the environment is already prepared and the dev agent chooses to run them as optional evidence.
- EventStore submodule telemetry redesign.
- Health/stateless reconstruction evidence owned by Story 7.5.
- Deployment readiness checklist/evidence template owned by Story 7.6E.
- Phase 2 UI command lifecycle panels or audit evidence UI.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*`.
- Keep telemetry tests deterministic and infrastructure-free using `ActivityListener`, `MeterListener`, NSubstitute, and existing production handlers.
- Use `[Collection("Telemetry")]` for tests that install global listeners.
- Every test must assert both positive emission and negative safety/cardinality where relevant.
- If direct `dotnet test` is blocked by sandbox socket restrictions, use the built xUnit executable fallback and record the limitation separately from product failures.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.4: Expose Tenant Command and Event Metrics with OpenTelemetry`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Artifact: _bmad-output/planning-artifacts/epics.md`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#Framework-Specific Rules (EventStore + DAPR + Aspire)`]
- [Source: `_bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md`]
- [Source: `_bmad-output/implementation-artifacts/5-4-expose-projection-write-conflict-diagnostics-and-recovery-evidence.md`]
- [Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-05-15.md`]
- [Source: `src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs`]
- [Source: `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`]
- [Source: `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`]
- [Source: `src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs`]
- [Source: `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`]
- [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- [Source: `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs`]

## Project Structure Notes

- Alignment: Story 7.4 belongs in Tenants host telemetry/service defaults, domain processing, projection dispatch, projection query actor, and server telemetry tests.
- Detected partial implementation: Tenants already has command, query, and projection-write-conflict telemetry. The implementation story must complete outcome/event-processing gaps, not rebuild instrumentation.
- Detected boundary: Event publication is owned by EventStore's aggregate/event publisher pipeline. Tenants should observe and register that source, not fork publisher behavior in the domain service.
- Detected metadata gap: projection DTOs do not carry causation IDs. Projection telemetry must make causation unavailable explicit instead of inventing values.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~DomainServiceRequestHandlerTelemetryTests` initially hit MSBuild named-pipe/socket restrictions before execution.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Focused direct xUnit telemetry fallback passed: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Telemetry.DomainServiceRequestHandlerTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantProjectionWritePolicyMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantsProjectionActorTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.ProjectionDispatcherTelemetryTests -parallel none -noLogo -noColor` => Total 43, 0 failed, 0 skipped.
- 2026-06-01: Requested VSTest telemetry command built the test assembly, then aborted in VSTest with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit telemetry fallback above is the application validation evidence.
- 2026-06-01: Requested VSTest projection command built the test assembly, then aborted in VSTest with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit fallback passed `ProjectionWriteConformanceTests`, `TenantProjectionHandlerTests`, and `GlobalAdministratorProjectionHandlerTests` => Total 50, 0 failed, 0 skipped.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit regression fallback passed after the VSTest socket blocker: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 670/0 failed, Integration 211 total with 0 failed and 25 expected skips.
- 2026-06-01: Senior review build passed: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` => 0 warnings, 0 errors.
- 2026-06-01: Senior review focused telemetry fallback passed: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Telemetry.DomainServiceRequestHandlerTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantActivitySourceTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantProjectionWritePolicyMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantsProjectionActorTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.ProjectionDispatcherTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.ServiceDefaultsTelemetryRegistrationTests -parallel none -noLogo -noColor` => Total 57, 0 failed, 0 skipped.
- 2026-06-01: Senior review projection fallback passed: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.GlobalAdministratorProjectionHandlerTests -parallel none -noLogo -noColor` => Total 50, 0 failed, 0 skipped.
- 2026-06-01: Senior review solution build passed: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` => 0 warnings, 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added bounded command outcomes (`success`, `rejection`, `noop`, `failure`) to command spans, command metrics, and structured logs while preserving infrastructure success semantics.
- Added projection dispatch event-processing telemetry with `Tenants.Projection.Project` spans, `tenants.event.processing.duration`, safe projection dispatch logs, unsupported-domain/invalid-identity/failure/retry-exhausted outcome classification, and explicit projection DTO causation-unavailable metadata.
- Extended query telemetry with bounded query outcomes for success, forbidden/rejected, unknown-query, and infrastructure-failure paths without moving query authorization/filtering into controllers.
- Confirmed EventStore publication telemetry is already observed through ServiceDefaults `.AddSource("Hexalith.EventStore")` and EventPublisher EventIds `3100`/`3101`; no EventStore submodule or duplicate publisher metric changes were made.
- Added deterministic telemetry tests for command, query, projection dispatch, retry-recovered/retry-exhausted paths, safe dimensions, structured outcome logs, and controlled delayed command duration. Unit tests prove telemetry presence and bounded dimensions only; no p95/NFR threshold compliance is claimed.
- Senior review tightened projection telemetry safety by bounding unsupported telemetry domains to `unknown` and allow-listing projection span event type summaries so externally supplied names cannot leak payload-like, token-like, or PII-like strings.

### File List

- _bmad-output/implementation-artifacts/7-4-expose-tenant-command-and-event-metrics-with-opentelemetry.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs
- src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs
- src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs
- src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs
- src/Hexalith.Tenants/Telemetry/TenantMetrics.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/DomainServiceRequestHandlerTelemetryTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/ProjectionDispatcherTelemetryTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/ServiceDefaultsTelemetryRegistrationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs

### Change Log

- 2026-06-01: Implemented Story 7.4 OpenTelemetry command, projection/event-processing, and query outcome instrumentation with deterministic telemetry tests and direct xUnit validation evidence.
- 2026-06-01: Senior review auto-fixed projection telemetry metadata bounding, updated telemetry regression coverage, synced story file list, and marked story done.

## Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

### Findings Fixed

- MEDIUM: Projection spans and dispatch logs used the raw unsupported projection domain as telemetry metadata. This was fail-closed functionally, but did not preserve bounded telemetry metadata for unsupported domains. Fixed by routing telemetry/log domain values through a bounded mapper while preserving the existing HTTP ProblemDetails behavior.
- MEDIUM: Projection span event type summaries accepted raw `ProjectionEventDto.EventTypeName` values. Fixed by allow-listing known Tenants event names, emitting short event names for known events, and collapsing unknown event type values to `unknown`.
- MEDIUM: Story File List omitted telemetry test files that were changed or added during implementation. Fixed by documenting `TenantActivitySourceTests.cs`, `ServiceDefaultsTelemetryRegistrationTests.cs`, and test summary evidence.

### Acceptance Criteria Review

- AC1 command telemetry: Implemented and covered by command span/metric/log tests for success, rejection, no-op, failure, sanitized command type, and delayed duration evidence.
- AC2 event/projection telemetry: Implemented and covered by projection dispatch tests for supported domains, unsupported domain, invalid global-admin identity, retry-recovered conflict, retry exhaustion, infrastructure failure, bounded event type summaries, and EventStore source registration visibility.
- AC3 query telemetry: Implemented and covered by query span/metric tests for success, forbidden, unknown query, infrastructure failure, and bounded query metric dimensions.
- AC4 support-safe metadata: Verified and tightened during review. Metrics avoid high-cardinality IDs; projection spans now bound unsupported domains and event type summaries; logs avoid payload/event-type/message/user/token/secret fields.
- AC5 outcome distinction: Implemented through bounded command/query/projection outcomes and structured log tests that distinguish domain rejections from infrastructure failures.

### Outcome

Approved after auto-fixes. No critical issues remain.
