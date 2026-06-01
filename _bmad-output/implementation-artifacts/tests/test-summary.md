# Test Automation Summary

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/ServiceDefaultsTelemetryRegistrationTests.cs` - OpenTelemetry smoke coverage proving Tenants registers the `Hexalith.Tenants` meter/source and the `Hexalith.EventStore` source, and that EventStore publication telemetry remains exposed through `EventStore.Events.Publish` with safe EventIds `3100`/`3101`.

### E2E Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/DomainServiceRequestHandlerTelemetryTests.cs` - Command-processing telemetry workflow coverage for success, rejection, no-op, missing processor/failure, sanitized command metric dimensions, delayed duration evidence, and structured log outcome separation.
- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/ProjectionDispatcherTelemetryTests.cs` - Event/projection dispatch telemetry workflow coverage for tenant projection, global-administrator projection, unsupported domain, invalid identity, retry-recovered conflict, retry exhaustion, infrastructure failure, low-cardinality metrics, and support-safe structured logs.
- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs` - Query telemetry workflow coverage for successful, forbidden, unknown-query, and infrastructure-failure outcomes with bounded metric tags.
- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs` - Activity source smoke coverage for command, query, and projection span names and supported span metadata.
- [x] `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs` - Bounded metric dimension coverage for command, query, event-processing, and projection-write metrics.

## Coverage

- Story 7.4 acceptance criteria: 5/5 covered by command telemetry tests, projection/event-processing telemetry tests, query telemetry tests, ServiceDefaults/EventStore source registration smoke tests, and structured log safety assertions.
- Command telemetry covered: completed success, domain rejection, no-op, infrastructure failure, command latency, sanitized command type, support-safe span metadata, low-cardinality metric tags, and separate rejection/failure log classifications.
- Event/projection telemetry covered: tenant projection, global-administrator projection, unsupported domain, invalid global-admin identity, retry-recovered conflict, retry-exhausted conflict, thrown infrastructure failure, projection duration, safe causation-unavailable metadata, and safe structured logs.
- Query telemetry covered: successful read model query, forbidden/rejected query, unknown query, infrastructure failure, query latency, sanitized query type, and no high-cardinality metric dimensions.
- Event publication telemetry visibility covered: Tenants ServiceDefaults registers `Hexalith.EventStore`, and EventStore publisher source/log smoke checks protect the existing `EventStore.Events.Publish` span plus EventIds `3100`/`3101`.
- UI workflows: N/A, story 7.4 has no browser UI surface.
- P95/NFR thresholds: not claimed from these tests; unit/in-process tests prove telemetry presence, bounded dimensions, and safe classifications only.

## Validation

- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~Telemetry -m:1 /nr:false /p:BuildInParallel=false` compiled successfully, then VSTest aborted before execution with sandbox socket denial: `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Telemetry.DomainServiceRequestHandlerTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantActivitySourceTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantProjectionWritePolicyMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantsProjectionActorTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.ProjectionDispatcherTelemetryTests -class Hexalith.Tenants.Server.Tests.Telemetry.ServiceDefaultsTelemetryRegistrationTests -parallel none -noLogo -noColor` passed: 57 total, 0 errors, 0 failed, 0 skipped.
- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~GlobalAdministratorProjectionHandlerTests" -m:1 /nr:false /p:BuildInParallel=false` compiled successfully, then VSTest aborted before execution with sandbox socket denial: `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.GlobalAdministratorProjectionHandlerTests -parallel none -noLogo -noColor` passed: 50 total, 0 errors, 0 failed, 0 skipped.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated if UI exists.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover 1-2 critical error cases.
- [x] Tests use proper locators: N/A for this non-UI story; tests use production command/projection/query telemetry surfaces.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps outside the story-required controlled delayed-operation duration proof.
- [x] Tests are independent and order-free; telemetry listener tests run under `[Collection("Telemetry")]` or direct xUnit `-parallel none`.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

---

# Test Automation Summary — Story 7.5

**Story:** 7.5 — Prove Stateless Operation, Health, and Startup Reconstruction
**Workflow:** qa-generate-e2e-tests · **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + `WebApplicationFactory` (infra-free) / `[DaprFact]` (Tier 2/3)

## Scope

QA gap analysis of Story 7.5's five acceptance criteria against the tests already produced by
dev-story, then auto-application of the discovered gap. Tests only — no story validation or code review.

## Gap Analysis

| AC | Existing coverage | Verdict |
|----|-------------------|---------|
| AC1 — readiness/liveness contract | `HealthEndpointsTests` (registration name/tag/FailureStatus; `/ready` 503 unhealthy; `/ready` 200 healthy; `/alive` 200 while `/ready` 503) + `DaprStateStoreHealthCheckTests` (healthy + 3 failure exceptions) | **Gap found** — the Development JSON response writer (`WriteHealthCheckJsonResponse`, exposed by `MapDefaultEndpoints` on `/health` + `/ready`) was untested production code. AC1 + Technical Guardrails require health output be *support-safe*; completion notes claimed it but nothing asserted it. **Closed.** |
| AC2 — restart reconstruction | aggregate reactivation reload + projection fresh-client reload | Covered |
| AC3 — no instance-local state | architecture assertion: no writable static fields in host assembly | Covered |
| AC4 — snapshot baseline | `tenants=50`, default `100`, validation passes, no `TenantDomainIntervals`, no global-admin override | Covered |
| AC5 — scheduled startup reconstruction perf | `SnapshotPerformanceTests` guarded by `DaprPerformanceFactAttribute` | Covered (correctly scoped; benchmark stays in scheduled lane) |

## Generated Tests

### Endpoint / API Tests

- [x] `tests/Hexalith.Tenants.IntegrationTests/HealthEndpointsTests.cs` —
  `Ready_development_json_response_is_support_safe_and_hides_exception_internals`

  Boots the real Tenants host in the `Development` environment, swaps the readiness check for a stub
  that fails with an exception carrying an embedded secret
  (`Server=tenants-db;Password=SUPER-SECRET-TOKEN-12345`), hits `/ready`, and asserts the dev JSON body:
  - returns HTTP 503 with `application/json`;
  - **surfaces** the status (`Unhealthy`) and safe category description (`DAPR state store is unreachable`);
  - **never leaks** the exception message, embedded secret, `Password=`, a stack trace, or the exception type name.

  Supporting infrastructure extended in the same file: `HealthWebApplicationFactory` now accepts an
  environment + failing description/exception; `StubReadinessCheck` propagates them.

## Coverage

- AC1 health endpoints: support-safe Development output path now covered (previously 0 tests).
- All five ACs now have happy-path + critical-error automated evidence in the implementation lane
  (AC5 remains scheduled-lane evidence by design).

## Validation Results

| Lane | Command (filter) | Result |
|------|------------------|--------|
| Integration (AC1 health, infra-free) | `IntegrationTests --filter FullyQualifiedName~HealthEndpointsTests` | **Passed 5, Failed 0, Skipped 0** |
| Server (AC1 health + AC4 snapshot + AC3 architecture) | `Server.Tests --filter Health\|SnapshotConfigurationTests\|StatelessHostStateTests` | **Passed 9, Failed 0, Skipped 0** |
| Build | `dotnet build Hexalith.Tenants.IntegrationTests` | **0 Warning(s), 0 Error(s)** |

> Tier 2/3 `[DaprFact]` tests (AC2 restart, AC5 perf) require `dapr init` + Docker and were not re-run
> in this QA pass; they are unchanged by this gap fix. The 500K-event benchmark stays in the scheduled
> lane behind `HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1` and is not claimed here.

## Checklist

- [x] API/endpoint tests generated (support-safe health output).
- [x] E2E/UI tests: N/A — Story 7.5 has no browser UI surface.
- [x] Tests use standard xUnit v3 + Shouldly APIs.
- [x] Tests cover happy path (healthy/dev-JSON surfaces status).
- [x] Tests cover critical error case (failing dependency must not leak secrets/internals).
- [x] Clear descriptive test name; Given/When/Then-style comments.
- [x] No hardcoded waits or sleeps; deterministic stub, no `Thread.Sleep`/`Task.Delay`.
- [x] Test is independent and order-free (own factory instance per test).
- [x] Summary updated; tests saved to existing project directory; coverage + validation recorded.
