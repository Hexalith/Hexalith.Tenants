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

---

# Test Automation Summary — Story 8.1

**Story:** 8.1 — Create a Prerequisite-Validated Quickstart
**Workflow:** dev-story · **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

Documentation hardening plus local-auth fixture validation for the prerequisite-validated quickstart. No production runtime behavior was changed.

## Generated / Updated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` —
  `LocalKeycloakRealm_AdminUserAuthorizesQuickstartCommandDomains`
  pins the local Keycloak `admin-user` realm claims required by the quickstart:
  `eventstore:tenant=system`, domains `global-administrators` and `tenants`, and `command:submit`.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs` —
  updated the submodule setup guardrail to allow explicit negative `--recursive` warnings while still requiring root-level `git submodule update --init` guidance.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Targeted tests via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~EventPublicationConfigurationTests|FullyQualifiedName~BootstrapConfigurationTests"` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Targeted tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests -class Hexalith.Tenants.Server.Tests.Configuration.BootstrapConfigurationTests -parallel none -noLogo -noColor` | Passed: 19 total, 0 failed, 0 skipped. |
| Release build | `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Full direct xUnit suite | Contracts, Client, Testing, Server, Sample, and Integration test assemblies under `bin/Release/net10.0` with `-parallel none` | Passed: 1306 total, 0 failed, 26 skipped. Skips were DAPR/performance prerequisite-gated. |
| Source/static checks | `rg`/file existence checks for solution, AppHost, production auth docs, DAPR docs, sample project, scripts, EventStore command/status routes, AppHost resources, appsettings registrations, query routes, ULID-shaped examples | Passed. |

## Live Environment Limitation

- `dapr --version` is available: CLI `1.17.1`, runtime `1.17.8`.
- Docker API access is denied in this sandbox: `permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`.
- Because Docker/AppHost infrastructure is unavailable, the quickstart was not live-run through first command submission. The guide now documents this as a prerequisite failure mode and the validation record does not claim live execution.

---

# Test Automation Summary — Story 8.1 QA Generate E2E Tests

**Story:** 8.1 — Create a Prerequisite-Validated Quickstart
**Workflow:** qa-generate-e2e-tests · **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

QA gap analysis of Story 8.1's quickstart journey against existing test coverage, then auto-application of discovered test gaps. Tests only.

## Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Local topology and DAPR contracts | `EventPublicationConfigurationTests` covered DAPR component names/scopes, domain-service routing, and local Keycloak `admin-user` claims. | Covered. |
| Root solution/submodule assumptions | `SolutionStructureTests` covered root-level submodules and non-recursive guidance. | Covered. |
| Quickstart command examples and prerequisite journey | No focused test parsed `docs/quickstart.md` to prove prerequisite checks, documented routes, command JSON examples, local paths, and success/rejection signals stayed current. | **Gap found and closed.** |
| Browser UI workflow | Story 8.1 has no UI surface. | N/A. |

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs` —
  validates the documented EventStore command gateway and status routes against EventStore source, deserializes first-command JSON examples into the real command contracts, verifies package names and ULID-shaped message IDs, and pins success/rejection interpretation.

### E2E Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs` —
  covers the prerequisite-validated quickstart journey end-to-end at the documentation-contract layer: .NET SDK, Docker, full DAPR local runtime, root-level submodules, AppHost path, EventStore gateway, local auth assumptions, `BootstrapGlobalAdmin`, `CreateTenant`, follow-up `AddUserToTenant`, and corrective action signals.
- [x] `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` —
  reused for source-backed topology and local Keycloak coverage.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs` —
  reused for solution and submodule guard coverage.

## Coverage

- Story 8.1 acceptance criteria: 5/5 covered by documentation-contract, topology, local auth, and solution/submodule tests.
- First command path covered: `BootstrapGlobalAdmin` against `global-administrators`, `CreateTenant` against `tenants`, EventStore `POST /api/v1/commands`, `GET /api/v1/commands/status/{correlationId}`, contract-deserializable payloads, ULID-shaped message IDs, and matching `aggregateId`/`payload.TenantId`.
- Critical error cases covered: missing prerequisite triage, `401`, `403`, connection failure, structured command rejection via `rejectionEventType`, `GlobalAdminAlreadyBootstrappedRejection`, and `TenantAlreadyExistsRejection`.
- UI workflows: N/A, Story 8.1 has no browser UI surface.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter FullyQualifiedName~QuickstartDocumentationTests --no-restore -m:1 -nr:false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Server targeted via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.QuickstartDocumentationTests` | Passed: 5 total, 0 failed, 0 skipped. |
| Server documentation/topology via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests -class Hexalith.Tenants.Server.Tests.Documentation.QuickstartDocumentationTests` | Passed: 22 total, 0 failed, 0 skipped. |
| Contracts targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --filter FullyQualifiedName~SolutionStructureTests --no-restore -m:1 -nr:false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Contracts targeted via xUnit runner | `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.SolutionStructureTests` | Passed: 6 total, 0 failed, 0 skipped. |
| Release build | `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nr:false` | Passed: 0 warnings, 0 errors. |

## Checklist

- [x] API tests generated where applicable.
- [x] E2E tests generated for the non-UI quickstart journey.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical prerequisite/auth/rejection error cases.
- [x] Tests use source-backed route/path assertions; semantic UI locators are N/A.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

---

# Test Summary - Story 8.2 Publish the Event Contract Reference

**Date:** 2026-06-01

## Scope

Validated `docs/event-contract-reference.md` against the public `Hexalith.Tenants.Contracts` command, success event, rejection, query, DTO, and enum surface. Added source-backed documentation tests for inventory drift, DAPR/CloudEvents guidance, serialization shape, JSON examples, enum converter behavior, known drift-prone contracts, and authorization/rejection outcome drift.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~EventContractReferenceDocumentationTests --no-restore` | MSBuild/VSTest aborted before execution in this sandbox with `SocketException (13): Permission denied`. |
| Server build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Focused documentation tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.EventContractReferenceDocumentationTests -parallel none -noLogo -noColor` | Passed: 7 total, 0 failed, 0 skipped. |
| Tier 1 contract tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -namespace Hexalith.Tenants.Contracts.Tests -parallel none -noLogo -noColor` | Passed: 92 total, 0 failed, 0 skipped. |
| Full solution build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Full direct xUnit regression | Contracts, Client, Testing, Server, Sample, and Integration test assemblies under `bin/Debug/net10.0` with `-parallel none` | Passed: 1316 total, 0 failed, 26 skipped. Skips were DAPR/performance prerequisite-gated. |

## Infrastructure Notes

- No Docker, DAPR sidecar, AppHost, or live pub/sub execution was required for Story 8.2.
- `dotnet test` remained blocked by sandbox socket restrictions, so validation used single-node `dotnet build` plus direct xUnit v3 runner execution.

---

# Test Automation Summary — Story 8.2 QA Generate E2E Tests

**Story:** 8.2 — Publish the Event Contract Reference
**Workflow:** qa-generate-e2e-tests · **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

QA gap analysis of Story 8.2's event contract reference coverage against the existing documentation tests, then auto-application of discovered test gaps. Tests only.

## Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Public contract inventory | `EventContractReferenceDocumentationTests` reflected command, success event, rejection, query, and enum type names. | Covered for names. |
| Public contract fields and enum values | Existing coverage could pass if a public contract field was added, removed, or renamed while the reference still listed the type. Generic `PaginatedResult<T>` was not part of the reflected inventory. | **Gap found and closed.** |
| DAPR/CloudEvents/ordering guidance | Existing focused assertions covered `tenants.events`, CloudEvents 1.0, at-least-once delivery, idempotency, and aggregate-local ordering language. | Covered. |
| JSON examples and enum converters | Existing tests parsed fenced JSON and deserialized enum examples through real contracts. | Covered. |
| Browser UI workflow | Story 8.2 has no UI surface. | N/A. |

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs` —
  `Event_contract_reference_documents_every_public_contract_member_and_enum_value` reflects every public documented contract property, accepts nullable field notation such as `Cursor?` and `ActorRole?`, includes `PaginatedResult<T>`, and asserts all enum values remain documented.
- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs` —
  `Event_contract_reference_matches_source_backed_authorization_and_rejection_outcomes` pins drift-prone command authorization and rejection outcome rows to `TenantAggregate` behavior.

### E2E Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs` —
  covers the non-UI contract-reference workflow end to end at the documentation-contract layer: public type inventory, field/member drift, enum values, DAPR topic and CloudEvents guidance, ordering limits, structured rejection data, JSON validity, and converter-backed enum examples.

## Coverage

- Story 8.2 acceptance criteria: 5/5 covered by source-backed documentation tests.
- Public surface drift covered: 12 commands, 11 success events, 14 rejections, 5 query contracts, public query DTOs including `PaginatedResult<T>`, and 3 public enums.
- Critical error cases covered: stale/missing contract fields, stale enum values, invalid JSON examples, placeholder values in JSON, unsafe global-ordering implication, missing at-least-once/idempotency guidance, missing drift-prone contracts, and stale command authorization/rejection outcomes.
- UI workflows: N/A, Story 8.2 has no browser UI surface.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~EventContractReferenceDocumentationTests --no-restore` | MSBuild/VSTest aborted before execution in this sandbox with `SocketException (13): Permission denied`. |
| Server build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Server targeted via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.EventContractReferenceDocumentationTests -parallel none -noLogo -noColor` | Passed: 7 total, 0 failed, 0 skipped. |

## Checklist

- [x] API tests generated where applicable.
- [x] E2E tests generated for the non-UI contract-reference workflow.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical stale-doc/error cases.
- [x] Semantic UI locators are N/A; tests use source-backed contract reflection and documentation parsing.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
