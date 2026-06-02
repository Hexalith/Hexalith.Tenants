---
baseline_commit: d20a990
---

# Story 7.6C: Validate Health and Dependency Readiness Smoke Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want health and dependency readiness smoke tests,
so that the service does not report ready before required infrastructure is usable.

## Acceptance Criteria

1. Given health or dependency checks fail, when smoke tests run, then the failure identifies the missing deployment input or dependency, and it does not produce ambiguous runtime errors or leak secrets.
2. Given health and readiness smoke-test evidence is captured, when operators review deployment readiness, then readiness covers required DAPR/EventStore dependencies and tenant command/query paths, and readiness does not claim success before required dependencies are usable.

## Tasks / Subtasks

- [x] Reconcile current health/readiness behavior before changing code. (AC: 1, 2)
  - [x] Read Story 7.5, Story 7.6A, and Story 7.6B completion notes; treat the existing `/alive` versus `/ready` split, production auth smoke coverage, DAPR component/service-invocation evidence, and prerequisite-gated live smoke pattern as baseline.
  - [x] Confirm `src/Hexalith.Tenants/Program.cs` still registers `dapr-statestore` with tag `ready` and `failureStatus: HealthStatus.Unhealthy` after `builder.AddServiceDefaults()`.
  - [x] Confirm `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` still maps `/health`, `/alive`, and `/ready`; `/alive` filters `live`, `/ready` filters `ready`, and `Unhealthy` maps to HTTP 503.
  - [x] Confirm `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs` still probes `DaprClient.GetStateAsync("statestore", "health-probe")` and returns only the safe dependency category `DAPR state store is unreachable` on failure.
  - [x] Do not duplicate Story 7.6A auth tests, Story 7.6B DAPR service-invocation tests, Story 7.6D pub/sub recovery tests, or Story 7.6E readiness-checklist publishing.

- [x] Validate deterministic health/readiness smoke behavior. (AC: 1, 2)
  - [x] Reuse `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs`; extend it only if current coverage misses a Story 7.6C case.
  - [x] Assert `/ready` returns HTTP 503 when the ready-tagged dependency is unhealthy and HTTP 200 when it is healthy.
  - [x] Assert `/alive` can remain HTTP 200 while `/ready` returns HTTP 503, so liveness cannot be mistaken for deployment readiness.
  - [x] Assert development JSON health output names safe dependency categories only and does not expose raw exception messages, stack traces, tokens, command payloads, connection strings, tenant/user IDs, or PII.
  - [x] Keep these checks infrastructure-free through `WebApplicationFactory` or existing stubs; do not require Docker, Redis, DAPR CLI, Aspire, or Keycloak for this deterministic lane.

- [x] Validate live dependency readiness evidence without overclaiming. (AC: 1, 2)
  - [x] Reuse `AspireTopologyFixture`, `DaprFactAttribute`, `DaprTestPrerequisites`, and `TenantsDaprTestFixture`; do not create another AppHost or DAPR fixture unless a concrete limitation is documented.
  - [x] If `AspireTopologyTests` does not already check explicit readiness, add a focused prepared-environment smoke test that calls the Tenants `/ready` endpoint and asserts HTTP 200 only after fixture prerequisites pass.
  - [x] Preserve the fixture's startup wait on `/alive`; do not change fixture startup to block on `/ready` because that would hide the distinction between process liveness and dependency readiness.
  - [x] Ensure unavailable Docker, Redis, placement, scheduler, state store, or DAPR sidecar prerequisites are reported as prerequisite-gated skips or blocked environment diagnostics, not as passing live readiness evidence.
  - [x] Ensure real `/ready` failures after prerequisites pass remain product/dependency failures and are not broadly converted into skips by substring matching.

- [x] Prove tenant command and query paths are covered by the readiness smoke evidence set. (AC: 2)
  - [x] Command API path: reuse Story 7.6A `CommandApiRuntimeIntegrationTests` for protected `POST /api/v1/commands` auth/gateway dispatch evidence and Story 7.6B `DaprEndToEndTests.CreateTenant_succeeds_end_to_end_with_events_published` for live actor/service-invocation command evidence when DAPR prerequisites are available.
  - [x] Query path: reuse `TenantsQueryControllerIntegrationTests` for protected query route smoke evidence and `StatelessRestartTests` projection-query evidence when DAPR prerequisites are available.
  - [x] Record command/query path readiness as separate evidence from `/ready`; do not make `/ready` submit commands, call protected query endpoints, invoke `/process`, or depend on production JWT/OIDC availability.
  - [x] If any named command/query smoke class no longer covers the path stated above, add the smallest focused assertion in the existing class rather than introducing a parallel smoke suite.
  - [x] Do not mark live command/query path readiness as passed when the live DAPR/Aspire lane is skipped.

- [x] Harden support-safe dependency diagnostics. (AC: 1)
  - [x] Reuse or extend `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs` for dependency-category wording and redaction.
  - [x] Diagnostics may name categories and local test ports: DAPR state store, DAPR sidecar, Redis `localhost:6379`, placement `50005` on Linux or `6050` on Windows, scheduler `50006` on Linux or `6060` on Windows, EventStore command gateway, Tenants query route, and service invocation boundary.
  - [x] Diagnostics must not include compact JWTs, bearer tokens, signing keys, decoded token payloads, command payloads, concrete production connection strings, private network addresses, real issuer URLs, real tenant/user IDs, or PII.
  - [x] Preserve narrow infrastructure-startup classification; product/runtime failures after prerequisites pass must fail tests.

- [x] Capture operator-ready smoke-test evidence. (AC: 2)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` with a Story 7.6C section that separates deterministic health/readiness checks, live AppHost `/ready` checks, command path checks, query path checks, and prerequisite-gated skips.
  - [x] Evidence must include date, workflow, test class/filter, pass/fail/skip counts, safe dependency categories, and whether live prerequisites were available.
  - [x] Do not record compact JWTs, bearer tokens, signing keys, decoded payloads, raw command bodies, private hosts, real tenant/user identifiers, connection strings, or PII.
  - [x] If live prerequisites are unavailable in the developer environment, record the exact safe skip reason and do not claim live readiness or command/query path AC evidence from static checks alone.

- [x] Run focused validation and record evidence accurately. (AC: 1, 2)
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~HealthEndpointsTests|FullyQualifiedName~AspireTopologyTests|FullyQualifiedName~DaprEndToEndTests|FullyQualifiedName~StatelessRestartTests|FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests|FullyQualifiedName~DaprTestPrerequisiteDiagnosticsTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DaprStateStoreHealthCheckTests|FullyQualifiedName~EventPublicationConfigurationTests"`.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from product failures.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, project files, AppHost/Aspire topology, ServiceDefaults, DAPR fixtures, docs, or shared evidence artifacts change.
  - [x] Do not mark ACs complete from skipped live tests; record skipped live readiness as a remaining deployment-evidence boundary.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.6C is the health and dependency readiness smoke-test slice of the corrected deployment-readiness story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6C: Validate Health and Dependency Readiness Smoke Tests`]
- The 2026-05-31 sprint correction split the old oversized Story 7.6 into auth, DAPR/service invocation, health/readiness, pub/sub recovery, and final evidence-template stories so each failure mode can be tested and diagnosed independently. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- PRD and epics map this story to FR56 deployment beside EventStore, NFR15/NFR16 DAPR infrastructure abstraction, NFR22 health-check availability evidence, and NFR23 durable recovery evidence. Story 7.6C owns smoke evidence that readiness does not report success before required dependencies and command/query paths are usable. [Source: `_bmad-output/planning-artifacts/epics.md#Functional Requirements`; `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`; `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- Architecture maps Epic 7 to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. Tenants host owns health checks; Aspire/AppHost own orchestration; EventStore remains the command gateway. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

### Current Repository State

- `src/Hexalith.Tenants/Program.cs` calls `builder.AddServiceDefaults()`, registers `DaprStateStoreHealthCheck` as `dapr-statestore` with tag `ready` and `failureStatus: HealthStatus.Unhealthy`, maps default endpoints, then maps `/process`, `/project`, controllers, subscribe handler, and actors.
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` maps `/health`, `/alive`, and `/ready`. `/alive` filters checks tagged `live`; `/ready` filters checks tagged `ready`; `Unhealthy` maps to HTTP 503 while `Healthy` and `Degraded` map to HTTP 200. Development responses use a JSON writer.
- `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs` performs a lightweight DAPR state read against component `statestore` and key `health-probe`. Failures return `HealthCheckResult.Unhealthy("DAPR state store is unreachable")` without attaching raw dependency exceptions.
- `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs` already proves the deterministic endpoint contract: `dapr-statestore` is ready-tagged with `FailureStatus == Unhealthy`, `/ready` returns 503 or 200 based on dependency health, `/alive` stays 200 while `/ready` fails, and development JSON response output hides raw exception internals.
- `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs` covers direct health-check behavior for reachable DAPR and failing `DaprException`, `TaskCanceledException`, and `HttpRequestException` cases.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` intentionally waits for `/alive`, not `/ready`, and states the fixture is process-liveness evidence. This boundary must be preserved; explicit readiness evidence belongs in targeted tests that call `/ready` after prerequisites pass.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` currently checks `/alive` for `eventstore`, `tenants`, and `sample`, plus command/demo flows through the AppHost topology. Add explicit `/ready` evidence only if needed; do not reinterpret existing `/alive` checks as readiness.
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` exercises live DAPR actor command processing through service invocation to Tenants `/process`, event persistence, and pub/sub publication. It is `[DaprFact]`-gated and skips when DAPR prerequisites are unavailable.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` and `CommandApiRuntimeIntegrationTests.cs` provide production-like protected query and command API smoke coverage from Story 7.6A.
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs` provides DAPR-gated projection/query reconstruction evidence from Story 7.5.
- `deploy/dapr/README.md` already documents local full `dapr init`, slim-mode responsibilities, expected local ports, missing component triage, wrong AppId/component/scope triage, denied service invocation, and the rule that static YAML validation is not live deployment proof.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` already contains Story 7.6A and 7.6B evidence sections. Add a 7.6C section instead of replacing those entries.

### Previous Story Intelligence

- Story 7.5 hardened the core health contract. `/ready` depends on the Tenants DAPR state store and returns 503 on unhealthy readiness; `/alive` remains process liveness; EventStore service-invocation readiness is intentionally not probed by `/ready` and is proven through DAPR end-to-end and deployment smoke lanes. [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md`]
- Story 7.6A completed production auth smoke tests for protected query and command paths. Reuse its command/query route evidence; do not weaken fail-closed auth behavior or log token material. [Source: `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- Story 7.6B completed DAPR component and service-invocation smoke tests. It established the current evidence pattern: static configuration validation is deterministic, live DAPR/AppHost evidence is prerequisite-gated, skipped live tests are not passing deployment proof, and support-safe diagnostics must redact secrets and payloads. [Source: `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md`]
- Story 7.6B senior review fixed raw `/process` and DAPR startup diagnostic leakage. Preserve `LastProcessDiagnostic`, `ToSupportSafeDiagnostic`, and narrow infrastructure-startup classification when adding any readiness diagnostics. [Source: `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md#Senior Developer Review (AI)`]

### Git Intelligence

- Latest relevant commit before story creation: `d20a990 feat(story-7.6B): Validate DAPR Component and Service Invocation Smoke Tests`.
- Recent commits also include `4db3ca7 feat(story-7.6A): Validate Production Auth Smoke Tests` and `9c6d976 fix(story-automator): support letter-suffixed story ids`; letter-suffixed story keys such as `7-6c-*` are expected.
- Current worktree at story creation has an unrelated modification in `_bmad-output/story-automator/orchestration-7-20260602-053838.md`. Do not restore, rewrite, or claim ownership of that file during 7.6C implementation unless the user explicitly routes work through story-automator.

### Latest Technical Information

- ASP.NET Core health checks support separate readiness and liveness probes through `MapHealthChecks`, `HealthCheckOptions.Predicate`, and `ResultStatusCodes`. Keep Tenants' tag-based `/alive` and `/ready` split rather than collapsing everything into `/health`. [Source: Microsoft Learn, Health checks in ASP.NET Core, `https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks`]
- Aspire AppHost resource health checks are distinct from application health check endpoints and surface resource status in the dashboard. Treat AppHost resource state and `/alive` as orchestration/liveness evidence unless an explicit `/ready` probe is added. [Source: Microsoft Learn, Aspire health checks, `https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/health-checks`]
- DAPR sidecar health endpoints are intended for infrastructure platforms; DAPR warns application code should not depend on sidecar `/healthz` because it can create circular dependency problems. Keep the application readiness probe as a bounded DAPR state-store operation through `DaprClient`, not a raw `/healthz` dependency in app code. [Source: DAPR Docs, Sidecar health, `https://docs.dapr.io/operations/resiliency/health-checks/sidecar-health/`]
- DAPR app health checks can cause the sidecar to stop accepting pub/sub, input binding, and service-invocation work on behalf of an unhealthy app; they are disabled by default. Story 7.6C should validate Tenants' app endpoint behavior without introducing new DAPR app-health configuration unless a separate deployment decision approves it. [Source: DAPR Docs, App health checks, `https://docs.dapr.io/operations/resiliency/health-checks/app-health/`]
- Use repo-pinned versions: .NET SDK `10.0.300`, DAPR SDK `1.17.9`, `CommunityToolkit.Aspire.Hosting.Dapr 13.3.0-preview.1.260514-0647`, Aspire `13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. Do not upgrade packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

### Technical Guardrails

- `/ready` is a readiness endpoint, not a workflow runner. It must not submit tenant commands, call protected query endpoints, invoke `/process`, require production JWTs, publish events, or mutate state.
- `/alive` remains lightweight process liveness. Do not add DAPR, Redis, EventStore, Keycloak, auth, or command/query calls to liveness.
- Required local readiness dependency for the Tenants host is the DAPR sidecar/state store named `statestore`. Command/query path readiness is separate evidence gathered through API and DAPR smoke tests.
- DAPR component names remain contracts: AppId `tenants`, state store `statestore`, pub/sub `pubsub`, topic `tenants.events`, dead letter `deadletter.tenants.events`. Do not invent `readiness-store`, `health-pubsub`, per-environment component names, or provider-specific package references.
- Do not add Redis, broker, database, cloud-provider, or connection-string parsing dependencies to `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, or domain aggregate code.
- Health/readiness diagnostics must be support-safe. Safe categories include `DAPR state store`, `DAPR sidecar`, `Redis`, `placement`, `scheduler`, `EventStore command gateway`, and `Tenants query route`. Unsafe details include secrets, tokens, raw payloads, raw production hosts, PII, and stack traces.
- Skipped live tests are not proof. Evidence must clearly distinguish deterministic static/API checks from live DAPR/Aspire checks and must record unavailable prerequisites as an evidence boundary.

### Existing Files Likely to Touch

- `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs`: primary deterministic health/readiness smoke coverage; extend only for missing 7.6C cases.
- `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs`: direct health-check behavior and safe failure category coverage.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`: likely place for explicit prepared-environment `/ready` smoke evidence if current AppHost coverage only checks `/alive`.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`: update only if readiness diagnostics need a narrow, documented improvement; preserve liveness-only startup semantics.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs`: likely place to extend support-safe dependency diagnostic coverage.
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`: command path live DAPR evidence; extend only if current `CreateTenant` coverage no longer proves the path.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: protected query route smoke evidence; extend only if current coverage misses a readiness-relevant query path assertion.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: protected command gateway route smoke evidence; extend only if current coverage misses a readiness-relevant command path assertion.
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`: projection/query DAPR evidence; extend only if query path evidence is insufficient.
- `docs/quickstart.md` and `deploy/dapr/README.md`: update only if readiness smoke instructions or failure triage are stale.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: required evidence artifact for Story 7.6C.
- `src/Hexalith.Tenants/Program.cs`, `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`, and `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs`: modify only if tests expose a product behavior gap; current Story 7.5 implementation appears aligned with 7.6C.

### Preserve Existing Behavior

- Preserve `/health`, `/alive`, and `/ready` route names and status-code semantics.
- Preserve `dapr-statestore` registration name, `ready` tag, `HealthStatus.Unhealthy` failure status, and safe health description.
- Preserve AppHost resource names, AppIds, dynamic DAPR sidecar ports, component names, local and production YAML file locations, and package boundaries.
- Preserve production deny-by-default receiver-specific access-control templates.
- Preserve Story 7.6A fail-closed production auth behavior and Story 7.6B support-safe DAPR diagnostics.
- Preserve direct xUnit fallback reporting when VSTest cannot open sockets in this sandbox.
- Do not edit `Hexalith.EventStore` submodule files for this story.

### Out of Scope

- Production JWT/OIDC validation and auth smoke evidence; Story 7.6A owns it.
- DAPR component topology and service-invocation access-control proof; Story 7.6B owns it.
- Pub/sub outage, drain recovery, and catch-up evidence; Story 7.6D owns it.
- Final deployment readiness checklist and evidence template publishing; Story 7.6E owns it.
- Adding DAPR app-health configuration, Kubernetes probe manifests, Helm charts, Azure Container Apps templates, dashboards, alert rules, or OpenTelemetry collector configuration.
- Changing snapshot intervals, EventStore aggregate actor behavior, query authorization, command routing, projection filtering, or cursor behavior.
- Phase 2 Admin UI readiness or visual health dashboards.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*` except xUnit skip mechanisms already used by fixture attributes.
- Keep deterministic health/configuration tests infrastructure-free.
- Use existing `DaprFactAttribute`, `DaprTestPrerequisites`, `TenantsDaprTestFixture`, and `AspireTopologyFixture` for live DAPR/Aspire checks.
- Every smoke test must assert observable behavior: HTTP status, health registration, safe description, route dispatch, command result, query result, prerequisite skip reason, or redaction.
- If local DAPR/Aspire prerequisites are absent, record skip reasons accurately; do not treat unavailable infrastructure as product failure or passing live smoke evidence.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6C: Validate Health and Dependency Readiness Smoke Tests`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`]
- [Source: `src/Hexalith.Tenants/Health/DaprStateStoreHealthCheck.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs`]
- [Source: `deploy/dapr/README.md`]
- [Source: `docs/quickstart.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md`]
- [Source: Microsoft Learn, Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Source: Microsoft Learn, Aspire health checks](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/health-checks)
- [Source: DAPR Docs, Sidecar health](https://docs.dapr.io/operations/resiliency/health-checks/sidecar-health/)
- [Source: DAPR Docs, App health checks](https://docs.dapr.io/operations/resiliency/health-checks/app-health/)

## Project Structure Notes

- Alignment: Story 7.6C belongs in existing health/readiness tests, DAPR/Aspire prerequisite-gated integration smoke tests, support-safe diagnostic tests, and the shared test evidence summary.
- Detected baseline: Story 7.5 already implemented the core Tenants readiness behavior. Story 7.6C should validate and package deployment smoke evidence, adding only narrow gaps such as explicit AppHost `/ready` smoke coverage if absent.
- Detected evidence boundary: `/alive` and Aspire resource `Running` state are liveness/orchestration evidence, not dependency readiness evidence. `/ready`, command API smoke tests, query API smoke tests, and DAPR-gated command/query tests must be recorded separately.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References
- 2026-06-02: Reconciled Story 7.5, Story 7.6A, and Story 7.6B completion notes. Confirmed Story 7.6C should preserve `/alive` as liveness, `/ready` as bounded DAPR state-store readiness, and command/query path readiness as separate smoke evidence.
- 2026-06-02: Confirmed `Program.cs` still calls `builder.AddServiceDefaults()` before registering `dapr-statestore` with tag `ready` and `failureStatus: HealthStatus.Unhealthy`.
- 2026-06-02: Confirmed `ServiceDefaults/Extensions.cs` still maps `/health`, `/alive`, and `/ready`, filters `live` and `ready` tags correctly, and maps `Unhealthy` to HTTP 503.
- 2026-06-02: Confirmed `DaprStateStoreHealthCheck` still probes `DaprClient.GetStateAsync("statestore", "health-probe")` and reports the safe dependency category `DAPR state store is unreachable` without carrying raw exceptions in the result.
- 2026-06-02: Required focused `dotnet test` commands for IntegrationTests and Server.Tests aborted before execution with sandbox MSBuild/VSTest socket denial: `System.Net.Sockets.SocketException (13): Permission denied`. Treated as environment limitation, not product failure.
- 2026-06-02: Direct xUnit fallback passed focused IntegrationTests: 209 total, 0 errors, 0 failed, 25 skipped. Skips were prerequisite-gated live DAPR/AppHost tests with the safe reason: `DAPR integration prerequisites are unavailable. Run 'dapr init' and ensure Redis, placement, and scheduler are reachable.`
- 2026-06-02: Direct xUnit fallback passed focused Server.Tests: 24 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-02: Full direct xUnit regression sweep passed: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 726/0 failed, Integration 232 total with 0 failed and 28 DAPR/performance prerequisite-gated skips.
- 2026-06-02: Debug solution build passed with 0 warnings and 0 errors using `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0`, `--configuration Debug`, `--no-restore`, `-m:1`, `/nr:false`, and `/p:BuildInParallel=false`.

### Completion Notes List
- Added focused AppHost Tenants `/ready` smoke coverage in `AspireTopologyTests`, guarded by existing DAPR prerequisites and preserving fixture startup on `/alive` so liveness is not conflated with dependency readiness.
- Extended deterministic health endpoint coverage so development JSON readiness output continues to expose only the safe DAPR state-store category and excludes raw exception internals plus unsafe token, connection, issuer, tenant/user, and PII shapes.
- Hardened support-safe DAPR diagnostic coverage and sanitizer behavior for issuer URLs, tenant/user identifiers, and email/PII while preserving existing token, secret, connection string, private-address, and narrow infrastructure-startup classification coverage.
- Reused existing command/query smoke evidence from `CommandApiRuntimeIntegrationTests`, `DaprEndToEndTests`, `TenantsQueryControllerIntegrationTests`, and `StatelessRestartTests`; no duplicate auth, DAPR service-invocation, pub/sub recovery, or readiness-checklist suite was introduced.
- Captured Story 7.6C evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`, separating deterministic readiness checks, live AppHost `/ready`, command path checks, query path checks, and prerequisite-gated skips.
- Live DAPR/AppHost tests were discoverable and correctly prerequisite-gated, but skipped in this sandbox because Redis, placement, and scheduler were unavailable. Skipped live checks are recorded as an evidence boundary, not passing live deployment readiness.

### Senior Developer Review (AI)

**Reviewer:** GPT-5 Codex
**Date:** 2026-06-02
**Outcome:** Approved after auto-fix.

#### Issues Found and Fixed

- **HIGH:** Development health JSON still serialized `HealthReportEntry.Data`. A failing dependency check could place tokens, connection strings, command payloads, tenant/user identifiers, or other unsafe diagnostics in the data bag, contradicting AC1 and the completed task that readiness output names safe dependency categories only. Fixed `WriteHealthCheckJsonResponse` to emit status, safe description, and duration only. Strengthened `HealthEndpointsTests` so unsafe health data is present in the stub result and absent from `/ready` JSON.
- **MEDIUM:** DAPR prerequisite diagnostic redaction did not fully consume quoted tenant/user identifiers. Fixed `ToSupportSafeDiagnostic` to redact quoted identifier values without leaving the original value behind, and extended `DaprTestPrerequisiteDiagnosticsTests` to cover quoted `tenantId` and `userId` shapes.

#### Verification

- Re-read `Program.cs`, `ServiceDefaults/Extensions.cs`, `DaprStateStoreHealthCheck.cs`, `HealthEndpointsTests.cs`, `AspireTopologyTests.cs`, `AspireTopologyFixture.cs`, `TenantsDaprTestFixture.cs`, `DaprTestPrerequisiteDiagnosticsTests.cs`, `DaprStateStoreHealthCheckTests.cs`, and the reused command/query smoke classes against AC1 and AC2.
- Cross-checked current official docs for ASP.NET Core health checks, Aspire health checks, and DAPR sidecar/app health. The local contract still aligns: tag predicates and `ResultStatusCodes` support separate liveness/readiness endpoints; Aspire AppHost resource checks are distinct from application health endpoints; and application code should not depend on the DAPR sidecar `/healthz` endpoint for readiness.
- Direct xUnit focused IntegrationTests after review patch: `HealthEndpointsTests` and `DaprTestPrerequisiteDiagnosticsTests` passed, 18 total, 0 failed, 0 skipped.
- Direct xUnit broader focused IntegrationTests review lane passed before the final synthetic-log cleanup: 209 total, 0 errors, 0 failed, 25 DAPR/Aspire prerequisite-gated skips.
- Direct xUnit focused Server.Tests review lane passed: 24 total, 0 errors, 0 failed, 0 skipped.
- Debug solution build passed after production ServiceDefaults change: 0 warnings, 0 errors.

### File List
- `_bmad-output/implementation-artifacts/7-6c-validate-health-and-dependency-readiness-smoke-tests.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`
- `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs`

## Change Log

| Date       | Version | Description | Author |
|------------|---------|-------------|--------|
| 2026-06-02 | 1.0     | Added explicit Tenants `/ready` AppHost smoke coverage, hardened support-safe readiness diagnostics, captured Story 7.6C evidence, and moved story to review. | GPT-5 Codex |
| 2026-06-02 | 1.1     | Senior review auto-fixed development health JSON data leakage and quoted identifier redaction, then marked story done. | GPT-5 Codex |
