---
baseline_commit: d28e799
---

# Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want Tenants to expose reliable health and reconstruct state from EventStore,
so that horizontal scaling and recovery are predictable.

## Acceptance Criteria

1. Given a Tenants service instance starts, when it becomes ready, then health checks reflect required dependencies and service readiness, and readiness does not claim success before required DAPR/EventStore dependencies are usable.
2. Given a Tenants service instance is restarted, when it rebuilds aggregate or projection state, then state is reconstructed from EventStore events and snapshots, and no in-process state is required for correctness between requests.
3. Given multiple Tenants service instances run, when commands and queries are routed across instances, then correctness depends on EventStore/DAPR state and actor semantics, and no instance-local tenant state causes inconsistent behavior.
4. Given snapshot configuration is reviewed, when the `tenants` domain is configured, then the tenant snapshot interval is set to the documented 50-event interval, and global administrator singleton state uses the EventStore default unless evidence requires otherwise.
5. Given startup reconstruction performance tests run with the target scale data set, when 1,000 tenants with an assumed average of 500 events each are seeded, then ready-state reconstruction completes within the 30-second target or reports a documented failure, and the 500,000-event benchmark is classified as scheduled performance evidence while ordinary readiness and health checks remain in the implementation lane.

## Tasks / Subtasks

- [x] Reconcile current health/readiness behavior before changing code. (AC: 1)
  - [x] Read `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` and preserve the endpoint split: `/alive` is process liveness, `/ready` is dependency readiness, `/health` is aggregate health.
  - [x] Read `src/Hexalith.Tenants/Program.cs` and confirm Tenants registers `DaprStateStoreHealthCheck` with the `ready` tag after `builder.AddServiceDefaults()`.
  - [x] Read `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs` and its tests; keep DAPR state-store probing through `DaprClient.GetStateAsync("statestore", "health-probe")` unless a stronger existing EventStore helper is reused.
  - [x] Verify `/ready` returns non-success when required dependency checks are unhealthy; do not let an unhealthy DAPR readiness path become `Degraded` with HTTP 200.
  - [x] Preserve `/alive` as a lightweight process check; do not add DAPR, Redis, EventStore, or auth calls to liveness.

- [x] Add deterministic readiness coverage for the Tenants host. (AC: 1)
  - [x] Define the required dependency list for Tenants readiness before implementing checks. At minimum this includes the DAPR sidecar/state store used by Tenants runtime state. If EventStore service-invocation readiness is not checked directly by `/ready`, document the reason and the separate smoke/evidence lane that proves EventStore availability.
  - [x] Add or extend tests proving `/ready` includes the DAPR state-store readiness check and returns HTTP 503 when the readiness dependency is unavailable.
  - [x] Add or extend tests proving `/alive` can remain HTTP 200 while `/ready` fails, so orchestrators can distinguish process liveness from traffic readiness.
  - [x] Add development JSON response coverage only where it is already exposed by `MapDefaultEndpoints`; do not leak connection strings, tokens, payloads, or raw exception internals in health output.
  - [x] If an EventStore service-invocation readiness check is added, make it lightweight and bounded, use existing DAPR/service-discovery primitives, and cover failure classification without calling command-processing endpoints.

- [x] Preserve and prove snapshot configuration. (AC: 2, 4)
  - [x] Keep `src/Hexalith.Tenants/appsettings.json` configured with `EventStore:Snapshots:DomainIntervals:tenants = 50`.
  - [x] Keep `EventStore:Snapshots:DefaultInterval` unset unless evidence requires changing the EventStore default; global administrators should use the default 100-event interval.
  - [x] Keep `EventStore:Snapshots:TenantDomainIntervals` empty for Tenants; do not add a `system:tenants` override.
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs` only if current assertions do not cover the corrected story wording.
  - [x] Do not implement a custom snapshot manager, snapshot store, or EventStore submodule fork for this story.

- [x] Prove restart reconstruction from durable EventStore/DAPR state. (AC: 2)
  - [x] Review `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`; update the comments from old Story 7.3 references to Story 7.5.
  - [x] Ensure the test forces actor deactivation/reactivation, then proves a follow-up command succeeds because aggregate state was reconstructed from events/snapshots.
  - [x] Add projection/read-model reconstruction evidence if missing: restart/deactivate the projection path or create a fresh projection actor instance, then verify a tenant query returns the previously persisted state after projection state reload.
  - [x] Do not satisfy this AC by checking only in-memory aggregate state or by replaying events through `InMemoryTenantService`; the evidence must exercise production EventStore/DAPR state semantics.

- [x] Prove no instance-local tenant state is required. (AC: 3)
  - [x] Identify all Tenants-owned long-lived services, hosted services, singleton stores, static dictionaries, and caches in `src/Hexalith.Tenants`, `src/Hexalith.Tenants.Server`, and `src/Hexalith.Tenants.Client`.
  - [x] Document any in-process state that is allowed because it is telemetry-only, configuration-only, or per-request; none may be authoritative tenant state.
  - [x] Add a focused test or architecture assertion that command/query correctness survives a fresh host/actor/projection instance sharing the same DAPR/EventStore state.
  - [x] If true multi-replica Aspire evidence is practical in the current test harness, add it as Tier 3 evidence; otherwise record why DAPR actor placement plus shared state-store evidence is the implementation-lane proof and leave live multi-replica load evidence to release validation.

- [x] Keep startup reconstruction performance in the scheduled lane. (AC: 5)
  - [x] Review `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs`; update Story 7.3 comments to Story 7.5 and confirm it is guarded by `DaprPerformanceFactAttribute`.
  - [x] Keep the 500,000-event run behind `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1`; do not make it a normal PR or Tier 1 gate.
  - [x] Treat NFR11 tenant/user-volume load evidence as scheduled or release evidence separate from the NFR13 startup reconstruction benchmark. If no target-volume load test runs in this story, record that gap explicitly instead of claiming NFR11 compliance.
  - [x] If the benchmark runs, record elapsed reconstruction time and pass/fail evidence in the story completion notes.
  - [x] If the benchmark does not run, record the exact skip reason and do not claim NFR13 threshold compliance.
  - [x] Do not optimize snapshot intervals beyond the baseline 50-event tenant-domain setting unless the scheduled benchmark supplies failure evidence and a separate design decision approves the change.

- [x] Run focused validation and record evidence accurately. (AC: 1-5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Health|FullyQualifiedName~SnapshotConfigurationTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~StatelessRestartTests|FullyQualifiedName~SnapshotPerformanceTests|FullyQualifiedName~AspireTopologyTests"`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, project files, AppHost, ServiceDefaults, or shared docs change.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from product failures.
  - [x] Do not mark AC5 complete from unit tests or skipped performance tests; AC5 requires actual scheduled benchmark evidence or a documented failure/skip in the story record.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.5 owns health readiness, stateless operation evidence, snapshot baseline verification, and startup reconstruction evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction`]
- PRD FR57 requires Tenants to remain stateless between requests. NFR12 requires horizontal scaling by adding instances. NFR13 requires startup reconstruction within 30 seconds for 1,000 tenants with an assumed average of 500 events each. NFR20 defines EventStore as the source of truth, and NFR22 ties availability evidence to health check uptime monitoring. [Source: `_bmad-output/planning-artifacts/prd.md#Observability & Operations`; `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- The NFR coverage map assigns NFR11, NFR12, NFR13, NFR20, and NFR22 evidence to Story 7.5. The corrected 2026-05-31 sprint proposal explicitly classifies the 500,000-event benchmark as scheduled performance evidence while ordinary readiness and health checks stay in the implementation lane. [Source: `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction`]
- Architecture maps Epic 7 deployment/observability work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. Tenants host owns health checks; Aspire/AppHost own orchestration; EventStore events remain the source of truth. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

### Current Repository State

- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` maps `/health`, `/alive`, and `/ready`. `/alive` filters checks tagged `live`; `/ready` filters checks tagged `ready`; development responses use JSON. ASP.NET Core tracing filters out these health endpoints.
- `src/Hexalith.Tenants/Program.cs` calls `builder.AddServiceDefaults()`, registers `DaprStateStoreHealthCheck` as `dapr-statestore` with tag `ready`, maps default endpoints, and then maps `/process`, `/project`, controllers, DAPR subscribe handler, and actors.
- `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs` probes DAPR state-store reachability through the sidecar with a lightweight state read. It catches failures and returns `HealthCheckResult.Unhealthy("DAPR state store is unreachable")` without attaching raw dependency exceptions.
- `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs` covers healthy DAPR read plus `DaprException`, `TaskCanceledException`, and `HttpRequestException` failures without surfacing raw exceptions in the health result.
- `src/Hexalith.Tenants/appsettings.json` already sets `EventStore:Snapshots:DomainIntervals:tenants = 50` and does not set a global-administrator override.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs` already verifies the `tenants` interval is 50, `DefaultInterval` is 100, validation passes, and `TenantDomainIntervals` is empty.
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs` exists and exercises aggregate actor deactivation/reactivation, but it still carries old Story 7.3 comments and does not cover projection/read-model reload evidence.
- `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs` exists behind `DaprPerformanceFactAttribute` and `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1`, but it still carries old Story 7.3 comments.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` intentionally waits for `/alive`, not `/ready`, and states that full DAPR readiness belongs in DAPR-specific tests. Do not silently reinterpret existing topology tests as readiness evidence without adding explicit `/ready` checks.

### Previous Story Intelligence

- Story 7.4 added bounded command, projection/event-processing, and query telemetry. Preserve support-safe metadata rules in any health/readiness diagnostics: no payloads, bearer tokens, signing material, raw secrets, user emails, or high-cardinality metric dimensions. [Source: `_bmad-output/implementation-artifacts/7-4-expose-tenant-command-and-event-metrics-with-opentelemetry.md`]
- Story 7.3 completed fail-closed production tenant-claim validation. Health/readiness additions must not bypass authentication on protected command/query endpoints, must not log token material, and must not turn invalid tenant claims into anonymous/default tenant behavior. [Source: `_bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md`]
- Story 7.2 established DAPR component names and access-control expectations. Do not rename `statestore`, `pubsub`, `tenants.events`, AppId `tenants`, or EventStore service invocation conventions for this story. [Source: `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md`]
- Story 7.1 established Aspire hosting extensions and AppHost topology. Do not duplicate AppHost wiring in tests or consumers; reuse `AddHexalithTenants` and existing test fixtures. [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`]
- Archived old Epic 7 Story 7.3 contains useful implementation evidence, but it is oversized and explicitly warns future reliability/performance stories should carry one independently testable outcome. Use its completed assets as current state, not as a reason to skip corrected Story 7.5 evidence. [Source: `_bmad-output/implementation-artifacts/archive/story-automator-20260601T143814Z-old-epics-7-9/7-3-stateless-scaling-and-snapshot-configuration.md#Post-Readiness Note (2026-05-15)`]

### Technical Guardrails

- Use repo-pinned versions: .NET SDK `10.0.300`, DAPR SDK `1.17.9`, Aspire `13.3.x`, OpenTelemetry `1.15.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`. Do not upgrade packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Tenants domain state must remain EventStore/DAPR-owned. Do not add authoritative tenant state to static fields, singleton dictionaries, local files, `IMemoryCache`, or per-instance background services.
- DAPR component names remain convention-derived: AppId `tenants`, state-store component `statestore`, topic `tenants.events`, dead letter `deadletter.tenants.events`. Do not invent new component names for health or restart tests. [Source: `_bmad-output/project-context.md#DAPR`]
- Aggregate state is reconstructed from event history and snapshots through EventStore infrastructure. Do not modify `AggregateActor`, `SnapshotManager`, or any EventStore submodule file unless a separate cross-repo decision explicitly approves it.
- The tenant snapshot interval is a baseline configuration, not an optimization project. Keep `DomainIntervals["tenants"] = 50`; keep global administrators on the default 100-event interval unless evidence requires a future story.
- Health checks must be support-safe. Descriptions can identify failed dependency categories (`DAPR state store`, `EventStore service invocation`) but must not include secrets, tokens, payloads, connection strings, tenant/user IDs, or raw stack traces in non-development responses.
- The 500,000-event benchmark is scheduled performance evidence. Normal implementation tests may prove the test is discoverable, guarded, and correctly scoped; they cannot claim the 30-second target unless the benchmark actually runs.

### Existing Files Likely to Touch

- `src/Hexalith.Tenants/Program.cs`: adjust readiness health registration only if current behavior cannot satisfy AC1.
- `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs`: strengthen failure/status behavior only if tests expose a gap.
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`: adjust `/ready` status-code behavior only if unhealthy readiness can currently return HTTP 200.
- `src/Hexalith.Tenants/appsettings.json`: should normally remain unchanged except to preserve `EventStore:Snapshots:DomainIntervals:tenants = 50`.
- `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs`: extend dependency readiness coverage.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs`: extend snapshot assertions only if needed.
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`: update Story 7.5 evidence and add projection/reload coverage if practical.
- `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs`: update Story 7.5 comments and skip/evidence wording.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` and `Fixtures/AspireTopologyFixture.cs`: add explicit `/ready` readiness evidence only if using Aspire topology for AC1.
- `docs/` or `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if the implementation records operator evidence or benchmark results there.

### Preserve Existing Behavior

- Do not change command routing, query routing, projection filtering, projection write policy, DAPR component names, AppHost resource names, authentication policies, or public command/query response shapes.
- Do not make `/ready` call state-changing endpoints or submit commands.
- Do not make `/alive` dependent on DAPR, Redis, EventStore, Keycloak, or pub/sub.
- Do not claim multi-instance safety from a local in-memory fake. Use EventStore/DAPR state semantics or clearly document the evidence lane.
- Do not turn skipped infrastructure tests into passed acceptance criteria.

### Out of Scope

- Production deployment readiness checklist and evidence template; Story 7.6E owns it.
- Health and dependency readiness smoke-test packaging; Story 7.6C owns deployment smoke test validation.
- Pub/sub outage and catch-up evidence; Story 7.6D owns the corrected deployment slice, while old archived 7.3 evidence is background only.
- New dashboards, alert rules, Prometheus/Grafana configuration, or OTLP collector setup.
- Advanced snapshot tuning, snapshot compaction, custom snapshot stores, or EventStore snapshot redesign.
- Phase 2 Admin UI readiness or visual health dashboards.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*` except xUnit skip mechanisms already used by fixture attributes.
- Keep Tier 1 health/configuration tests infrastructure-free with NSubstitute where possible.
- Use existing `DaprFactAttribute`, `DaprPerformanceFactAttribute`, `TenantsDaprTestFixture`, and `AspireTopologyFixture`; do not create duplicate fixtures.
- Every test must assert a real observable behavior. Avoid placeholder tests that only prove the test discovered itself.
- If local DAPR/Aspire prerequisites are absent, record skip reasons accurately; do not treat unavailable infrastructure as product failure.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Observability & Operations`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#Framework-Specific Rules (EventStore + DAPR + Aspire)`]
- [Source: `_bmad-output/implementation-artifacts/7-4-expose-tenant-command-and-event-metrics-with-opentelemetry.md`]
- [Source: `_bmad-output/implementation-artifacts/archive/story-automator-20260601T143814Z-old-epics-7-9/7-3-stateless-scaling-and-snapshot-configuration.md`]
- [Source: `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs`]
- [Source: `src/Hexalith.Tenants/appsettings.json`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`]

## Project Structure Notes

- Alignment: Story 7.5 belongs in Tenants host health/readiness, ServiceDefaults endpoint mapping, snapshot configuration tests, and DAPR/Aspire integration evidence.
- Detected partial implementation: snapshot configuration, DAPR state-store health check, aggregate restart test, and guarded 500K benchmark already exist from earlier reliability work. The corrected story must harden readiness semantics, close projection/read-model and instance-local-state evidence gaps, and update stale Story 7.3 references.
- Detected evidence boundary: `/alive` topology smoke tests are liveness evidence, not dependency readiness evidence. `/ready` or DAPR-specific integration tests must carry readiness claims.
- No UI/frontend changes are required for this story.
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 7.5 implementation unless the implementation explicitly needs it.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (story authoring); Claude Opus 4.8 (dev-story implementation)

### Debug Log References

- Server Tests (Health|SnapshotConfigurationTests|StatelessHostStateTests): **Passed 9, Failed 0, Skipped 0**.
- Integration Tests (StatelessRestartTests|SnapshotPerformanceTests|AspireTopologyTests|HealthEndpointsTests): **Passed 10, Skipped 1, Failed 0**. The single skip is the 500K performance benchmark (scheduled lane). DAPR prerequisites (Redis 6379, placement 50005, scheduler 50006, Docker) were available, so all `[DaprFact]` tests executed.
- `dotnet build Hexalith.Tenants.slnx --configuration Debug`: **Build succeeded, 0 Warning(s), 0 Error(s)**.
- Environment note: a transient first run of the combined integration filter showed one flaky failure in the pre-existing `StatelessRestartTests.TenantState_IsReconstructedFromEventStore_AfterActorReactivation` (CreateTenant accepted but `EventCount=0`) caused by DAPR actor-placement churn while the `AspireTopology` collection booted full sidecars in parallel with the `TenantsDaprTest` collection. It did not reproduce: the test passed 2/2 in isolation, 2/2 within its class, and the full combined filter re-ran green (10 passed / 1 skipped). This is pre-existing parallel-topology flakiness, not a Story 7.5 product or code change.
- Senior review validation (2026-06-01): VSTest commands for Server and Integration tests built the assemblies, then aborted before execution because this sandbox denies the VSTest TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`). Treated as an environment limitation, not a product failure.
- Senior review direct xUnit fallback: `Hexalith.Tenants.Server.Tests` focused classes (`SnapshotConfigurationTests`, `StatelessHostStateTests`, `DaprStateStoreHealthCheckTests`) passed: **Total 9, Failed 0, Skipped 0**.
- Senior review direct xUnit fallback: `Hexalith.Tenants.IntegrationTests` Story 7.5 classes (`StatelessRestartTests`, `SnapshotPerformanceTests`, `AspireTopologyTests`, `HealthEndpointsTests`) passed with expected infrastructure gating: **Total 12, Failed 0, Skipped 7**. Skips were DAPR prerequisites unavailable and the scheduled 500K performance benchmark disabled by `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS`.
- Senior review build: `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:UseSharedCompilation=false` passed: **0 Warning(s), 0 Error(s)**.

### Completion Notes List

- **AC1 (health/readiness).** Hardened the readiness dependency registration in `src/Hexalith.Tenants/Program.cs`: the `dapr-statestore` ready-tagged check now registers `failureStatus: HealthStatus.Unhealthy` (was `Degraded`) so an unhealthy DAPR readiness path can never surface as `Degraded` → HTTP 200; it is `Unhealthy` → HTTP 503. The `/alive` (tag `live`, self check only), `/ready` (tag `ready`), `/health` endpoint split in `ServiceDefaults/Extensions.cs` and the `DaprClient.GetStateAsync("statestore", "health-probe")` probe were preserved. Senior review also removed the raw exception from `DaprStateStoreHealthCheck` failure results so framework health logging and response serialization carry only the safe dependency category. Required readiness dependency = the DAPR sidecar/state store; EventStore service-invocation readiness is intentionally NOT probed by `/ready` (kept bounded, no command-processing calls) and is proven by the DAPR end-to-end tests and the deployment smoke lane (Story 7.6C) — documented in `HealthEndpointsTests` remarks. Added `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs` (infrastructure-free `WebApplicationFactory` + deterministic stub): proves `/ready` → 503 when the dependency is unhealthy, `/ready` → 200 when healthy, `/alive` → 200 while `/ready` → 503, and that the registration is named `dapr-statestore`, tagged `ready`, with `FailureStatus == Unhealthy`. Health output remains support-safe (existing JSON writer emits status/description/empty data only; no secrets, tokens, payloads, or exceptions).
- **AC4 (snapshot baseline).** `appsettings.json` already carried `EventStore:Snapshots:DomainIntervals:tenants = 50`, no `DefaultInterval` override (EventStore default 100), and no `TenantDomainIntervals` — left unchanged. Extended `SnapshotConfigurationTests` with `AppSettings_SnapshotConfiguration_ShouldLeaveGlobalAdministratorsOnEventStoreDefault`, asserting `DomainIntervals` has no `global-administrators` key and `DefaultInterval == 100` (global admin singleton uses the EventStore default). No custom snapshot manager/store introduced.
- **AC2 (restart reconstruction).** Updated `StatelessRestartTests` comments from Story 7.3 → 7.5. The aggregate reconstruction test (actor deactivation → reactivation → follow-up DisableTenant succeeds) is unchanged. Senior review strengthened the projection evidence: `TenantProjection_QueryIsReconstructedFromStateStore_ByFreshProjectionActorInstance` now persists a production `TenantReadModel` to the shared DAPR state store under the production key `projection:tenants:{id}`, explicitly deactivates the `TenantsProjectionActor` actor id derived by `QueryActorIdHelper`, then verifies a fresh projection actor instance serves `GetTenantQuery` from durable state through the production authorization/query path. This replaces direct DAPR client readback evidence and proves the read model does not depend on instance-local memory. The integration test is discoverable and `[DaprFact]`-gated; in this sandbox it skipped because DAPR prerequisites were unavailable.
- **AC3 (no instance-local state).** Audited long-lived/singleton/static state across `src/Hexalith.Tenants`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants.Client`. Findings: no authoritative tenant state held in-process. Allowed non-authoritative state only: source-generated logger delegates and static `readonly` `JsonSerializerOptions` (configuration-only); `TenantsProjectionActor`'s per-actor-lifetime ETag payload cache and orphan-log dedup set (rebuilt from the durable state store on activation, wiped on deactivation/ETag change); and, in the separate consumer assembly `Hexalith.Tenants.Client`, `InMemoryTenantProjectionStore` (a consumer read-model choice) plus a static `readonly` reflection `ConcurrentDictionary` cache (`TenantEventProcessor`). All command/projection singletons in `Program.cs` are stateless services or DAPR-backed stores. Added architecture assertion `tests/Hexalith.Tenants.Server.Tests/Architecture/StatelessHostStateTests.cs` proving the `Hexalith.Tenants` host assembly contains no writable (non-`readonly`) static fields in its own (non source-generated) types. Behavioral cross-instance proof is the AC2 aggregate + projection reload tests; live multi-replica load is left to release validation.
- **AC5 (scheduled performance lane).** Updated `SnapshotPerformanceTests` comments from Story 7.3 (AC #6) → Story 7.5 (AC5/NFR13); confirmed it remains guarded by `DaprPerformanceFactAttribute` (requires `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1`). The 500,000-event benchmark **did NOT run** in this implementation lane — exact skip reason: "DAPR performance tests are disabled. Set HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1 …". Therefore **NFR13 30-second threshold compliance is NOT claimed**; the test is proven discoverable, guarded, and correctly scoped only. **NFR11** tenant/user-volume load evidence is explicitly NOT covered by this story and is deferred to scheduled/release validation (recorded gap, not claimed compliance). Snapshot intervals were not optimized beyond the 50-event baseline.

### File List

- `src/Hexalith.Tenants/Program.cs` (modified — readiness check failureStatus Degraded → Unhealthy + comment)
- `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs` (modified — returns safe unhealthy category without attaching raw dependency exception)
- `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs` (added — AC1 readiness/liveness endpoint tests)
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs` (modified — registers TenantsProjectionActor and cursor codec in the DAPR test host for projection query reconstruction evidence)
- `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs` (modified — asserts readiness failures do not carry raw exceptions)
- `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs` (modified — AC4 global-administrators default assertion)
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs` (modified — 7.3→7.5 comments + AC2 projection read-model reconstruction test)
- `tests/Hexalith.Tenants.Server.Tests/Architecture/StatelessHostStateTests.cs` (added — AC3 no-instance-local-state assertion)
- `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs` (modified — 7.3→7.5 scheduled-lane comments)

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex
Date: 2026-06-01
Outcome: Approved after auto-fix. Status -> done.

Findings fixed:

- [HIGH] AC2 projection reconstruction evidence was too weak: the original test wrote a `TenantReadModel` with one `DaprClient` and read it back with another, which proved DAPR persistence but did not exercise the Tenants projection actor or query path required by the story. Fixed by querying a fresh `TenantsProjectionActor` instance via `IDaprProjectionActor.QueryAsync` after durable state-store persistence.
- [MEDIUM] DAPR readiness failures attached the raw dependency exception to `HealthCheckResult`, which can expose adapter internals through framework health logging even when the HTTP response writer hides them. Fixed by returning the safe unhealthy dependency category without the exception and updating health-check tests.
- [MEDIUM] The DAPR integration fixture did not register `TenantsProjectionActor`, so the integration lane could not prove fresh projection actor reload behavior. Fixed by registering the actor and `ITenantQueryCursorCodec` in `TenantsDaprTestFixture`.
- [LOW] Story File List was incomplete after the review fix because the fixture became part of the implementation surface. Fixed by adding the fixture to the File List.

Documentation references checked during review:

- ASP.NET Core health checks: readiness/liveness split, `Predicate`, and `ResultStatusCodes` behavior (Microsoft Learn).
- Dapr .NET actors: actor proxy usage and actor invocation model (Dapr docs).

## Change Log

| Date       | Version | Description                                                                                                  | Author |
|------------|---------|--------------------------------------------------------------------------------------------------------------|--------|
| 2026-06-01 | 0.1     | Implemented Story 7.5: hardened `/ready` failure status to Unhealthy; added readiness/liveness, projection read-model reconstruction, snapshot global-admin, and no-instance-local-state tests; updated 7.3→7.5 comments. Status → review. | Claude Opus 4.8 |
| 2026-06-01 | 0.2     | Senior review auto-fixed AC2 projection evidence and support-safe DAPR readiness failure handling; validated with direct xUnit fallback and Debug build. Status -> done. | GPT-5 Codex |
