# Test Automation Summary

## Story 7.6A QA Generate E2E Tests - Production Auth Smoke Tests

**Workflow:** qa-generate-e2e-tests - **Date:** 2026-06-02
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + ASP.NET Core WebApplicationFactory

### Scope

QA automation audit for Story 7.6A production authentication smoke tests on protected Tenants query and command API paths. The story has no browser UI surface, so browser E2E tests are not applicable. Existing deterministic API/integration smoke tests were revalidated instead of duplicating equivalent coverage.

### Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Query auth smoke tests | `TenantsQueryControllerIntegrationTests` covers valid production-like JWT scope, source-claim normalization, missing/malformed/invalid JWTs, wrong issuer/audience, expired tokens, missing/blank/wrong/wrong-cased tenant claims, safe reason codes, `application/problem+json` where applicable, redaction checks, and no query-router dispatch on denied requests. | No test-code gap found. |
| Command auth smoke tests | `CommandApiRuntimeIntegrationTests` covers `POST /api/v1/commands` with valid `eventstore:tenant=system`, supported source claims, missing tenant claim, global-admin missing tenant claim, blank direct tenant claim with source alias, wrong/wrong-cased tenant, non-`system` request tenant, unrelated permissions, safe reason codes, and no command-router dispatch before auth succeeds. | No test-code gap found. |
| Production startup/options validation | `AuthenticationConfigurationTests` and `TenantClaimContractTests` cover production OIDC requirements, HTTPS metadata, signing-key separation, environment overrides, source-claim precedence, global-admin fail-closed tenant guard, and support-safe validation messages. | No test-code gap found. |
| Workflow output evidence | Existing Story 7.6A evidence was recorded as `dev-story`, not this QA workflow. | Gap found and closed in this summary section. |

### Generated Tests

- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - existing production-like query auth smoke coverage revalidated.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - existing command API auth smoke coverage revalidated.
- [x] `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` - existing production auth configuration validation revalidated.
- [x] `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` - existing tenant-claim contract validation revalidated.

### Coverage

- API endpoints covered: protected query route family and `POST /api/v1/commands`.
- UI workflows: N/A; Story 7.6A has no UI surface.
- Happy paths covered: valid direct `eventstore:tenant=system` and supported source-claim normalization reach dispatch.
- Critical error cases covered: missing/malformed/invalid JWT, wrong issuer, wrong audience, expired token, missing/blank/wrong/wrong-cased tenant claim, global-admin missing tenant claim, non-`system` request tenant, unrelated command permission, production `SigningKey`, non-HTTPS authority, and `RequireHttpsMetadata=false`.

### Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Integration focused via VSTest | `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"` | Aborted before execution with sandbox MSBuild/VSTest socket denial: `SocketException (13): Permission denied`. |
| Server focused via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~AuthenticationConfigurationTests|FullyQualifiedName~TenantClaimContractTests"` | Aborted before execution with sandbox MSBuild/VSTest socket denial: `SocketException (13): Permission denied`. |
| Integration focused via direct xUnit | `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` | Passed: 166 total, 0 errors, 0 failed, 0 skipped. |
| Server focused via direct xUnit | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests` | Passed: 53 total, 0 errors, 0 failed, 0 skipped. |

### Checklist

- [x] API tests generated or revalidated where applicable.
- [x] Browser E2E tests marked N/A because Story 7.6A has no UI.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy paths and critical error cases.
- [x] Tests use semantic HTTP assertions and no hardcoded waits.
- [x] Tests are independent and run successfully through the direct xUnit fallback.
- [x] Summary includes coverage metrics and validation results.

## Story 7.6A Dev Story - Production Auth Smoke Tests

**Workflow:** dev-story - **Date:** 2026-06-02
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + ASP.NET Core WebApplicationFactory

### Scope

Production auth smoke-test audit and evidence capture for protected Tenants query and command paths. Existing Story 7.3 and Story 11.3 validators, docs, and deterministic smoke tests were treated as the baseline. No production auth policy, JWT validation behavior, tenant-claim semantics, route shape, DAPR topology, EventStore actor path, or package reference was changed.

### Coverage

- Query smoke coverage preserved: valid direct `eventstore:tenant=system`, supported source claims (`tenants`, `tenant_id`, `tid`), missing token, malformed token, invalid signature, wrong issuer, wrong audience, expired token, missing tenant claim, global-admin missing tenant claim, blank tenant claim, wrong tenant, and wrong-cased tenant.
- Command smoke coverage preserved: valid `POST /api/v1/commands` dispatch with request tenant `system`, missing tenant claim, global-admin missing tenant claim, blank direct tenant claim with source alias present, wrong tenant, wrong-cased tenant, non-`system` request tenant, and unrelated permission claims.
- Startup/options coverage preserved: production placeholders, valid OIDC-style overrides, environment-variable overrides, missing `Authority`/`Issuer`/`Audience`, whitespace values, non-HTTPS authority, production `SigningKey`, and `RequireHttpsMetadata=false`.
- Support-safe evidence preserved: denied requests assert `401` or `403`, `application/problem+json` where applicable, stable safe reason codes (`principal_not_member`, `tenant_mismatch`, `insufficient_permission`), and no command/query router invocation before authorization succeeds.

### Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Integration focused via VSTest | `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"` | Aborted before execution with sandbox MSBuild/VSTest socket denial: `SocketException (13): Permission denied`. |
| Server focused via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~AuthenticationConfigurationTests|FullyQualifiedName~TenantClaimContractTests"` | Aborted before execution with sandbox MSBuild/VSTest socket denial: `SocketException (13): Permission denied`. |
| Integration focused via direct xUnit | `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` | Passed: 166 total, 0 errors, 0 failed, 0 skipped. |
| Server focused via direct xUnit | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests` | Passed: 53 total, 0 errors, 0 failed, 0 skipped. |
| Full direct xUnit regression suite | Contracts, Client, Testing, Sample, Server, and Integration Debug assemblies with `-parallel none` | Passed: 1357 total, 0 failed, 27 skipped. Skips were DAPR/performance prerequisite-gated. |
| Debug build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |

### Notes

- Full Server.Tests initially exposed two documentation drift failures outside the auth smoke area. The quickstart HMAC fallback now includes the unescaped compact payload audience marker for `hexalith-eventstore`, and the UI accessibility evidence spec now names `RejectionToHttpStatusMapper` as the rejection-copy boundary source. Server.Tests then passed in full.
- Evidence intentionally records commands, class names, pass/fail counts, safe status/reason-code categories, and the date only. It does not record compact JWTs, signing keys, decoded payloads, real issuer URLs, real tenant/user data, full command payloads, or PII.

## Story 8.6 QA Generate E2E Tests - Compensating Command Patterns

**Workflow:** qa-generate-e2e-tests - **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source-backed documentation checks

### Scope

QA automation for source-backed compensating-command documentation after mistaken user removal, wrong role assignment, configuration mistakes, and tenant lifecycle mistakes. No production command contracts, aggregate behavior, projection behavior, package references, or deployment configuration were changed.

### Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| EventStore command route drift | The guide documented `POST /api/v1/commands` and status polling, but Story 8.6's own tests did not bind those strings to the EventStore controller route attributes. | **Gap found and closed.** |
| Scenario-specific rejection/no-op coverage | Existing tests verified command, role, rejection, and `NoOp` terms existed somewhere in the guide, but did not prove each correction scenario listed its own safe command path and expected rejection/no-op cases. | **Gap found and closed.** |
| Browser UI E2E | Story 8.6 has no browser UI surface. | N/A. |

### Generated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs` - validates current command names, role enum values, rejection/no-op terms, source-file references, EventStore command request JSON examples, enum-name deserialization, hidden-undo exclusions, audit-versus-rejection language, support-safe sample content, and related-document navigation.
- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs` - added `Compensating_guide_command_gateway_and_status_routes_match_EventStore_source`, binding the guide to `CommandsController` and `CommandStatusController` route attributes.
- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs` - added `Compensating_guide_covers_each_scenario_with_safe_commands_and_expected_errors`, proving mistaken removal, wrong role, configuration, and lifecycle sections each carry the expected command path plus rejection/no-op guidance.

### Coverage

- Story 8.6 acceptance criteria: 5/5 covered by the rewritten compensating-command guide, navigation updates, source-backed documentation tests, and aggregate behavior regression tests.
- Happy path covered: mistaken removal corrected by explicit `AddUserToTenant`, intended `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`, and `EnableTenant` command examples.
- Critical drift/error cases covered: `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `ConfigurationLimitExceededRejection`, `ConfigurationKeyNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`, same-role `NoOp`, and same-value configuration `NoOp`.
- UI workflows: N/A; Story 8.6 has no browser UI surface.

### Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~CompensatingCommandsDocumentationTests --no-restore` | Aborted before execution with sandbox `SocketException (13): Permission denied`. |
| Server test assembly build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Story 8.6 documentation tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CompensatingCommandsDocumentationTests -parallel none -noLogo -noColor` | Passed: 9 total, 0 failed, 0 skipped. |
| Documentation namespace regression | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` | Passed: 42 total, 0 failed, 0 skipped. |
| Aggregate behavior regression | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests -parallel none -noLogo -noColor` | Passed: 140 total, 0 failed, 0 skipped. |
| Full direct xUnit regression suite | Contracts, Client, Testing, Server, Sample, and Integration test assemblies under `bin/Debug/net10.0` with `-parallel none` | Passed: 1349 total, 0 failed, 27 skipped. Skips were DAPR/performance prerequisite-gated. |

### Checklist

- [x] Source-backed documentation tests generated.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover explicit correction command examples, auditability, route drift, scenario-specific rejection/no-op drift, and support-safe sample constraints.
- [x] Focused aggregate tests anchor documented command behavior.
- [x] Direct xUnit fallback used where VSTest cannot open sockets.
- [x] Full direct xUnit regression suite passes with only prerequisite-gated skips.

## Story 8.5 QA Generate E2E Tests - Cross-Aggregate Timing

**Workflow:** qa-generate-e2e-tests - **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + YamlDotNet source checks

### Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Command lifecycle drift | The guide asserted command statuses with static strings and cited `CommandStatusController.cs`, but tests did not bind those claims to the actual EventStore `CommandStatus` enum. | **Gap found and closed.** |
| DAPR pub/sub drift | The guide asserted topic/dead-letter behavior and cited component YAML, while separate configuration tests covered YAML. Story 8.5's own documentation tests did not bind the timing guide to those source contracts. | **Gap found and closed.** |
| Subscriber/local projection behavior | Existing Client and Sample tests cover processed, duplicate, unknown, invalid payload, retry, fail-closed access, disable/enable, role change, removal, and configuration projection workflows. | Covered. |
| Browser UI E2E | Story 8.5 has no browser UI surface. | N/A. |

### Generated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs` - added `Timing_guide_matches_current_EventStore_command_status_contract`, binding guide claims to the current `CommandStatus` enum, terminal-status extension behavior, and controller documentation.
- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs` - added `Timing_guide_matches_current_DAPR_pubsub_component_contracts`, binding guide claims to local and production DAPR pub/sub component YAML for `pubsub`, `deadletter.tenants.events`, scopes, `sample=tenants.events`, and `resiliency.yaml` inbound retry policy.

### Coverage

- Story 8.5 acceptance criteria: 5/5 remain covered.
- Added drift coverage for EventStore command statuses: `Received`, `Processing`, `EventsStored`, `EventsPublished`, `Completed`, `Rejected`, `PublishFailed`, and `TimedOut`.
- Added drift coverage for DAPR component contracts: local/prod pubsub component name, dead-letter enablement, dead-letter topic, app scopes, production subscription scope, and pub/sub inbound retry resiliency.
- API tests: documentation/source-contract tests only; no new runtime HTTP API surface was introduced by this documentation story.
- E2E/UI tests: N/A; no browser UI surface.

### Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server targeted via VSTest | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CrossAggregateTimingDocumentationTests -m:1 /nr:false /p:BuildInParallel=false` | Built, then VSTest aborted before execution with `SocketException (13): Permission denied`. |
| Story 8.5 documentation tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CrossAggregateTimingDocumentationTests -parallel none -noLogo -noColor` | Passed: 7 total, 0 failed, 0 skipped. |
| Documentation namespace regression | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` | Passed: 33 total, 0 failed, 0 skipped. |
| Client focused related tests | `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventProcessorTests -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventSubscriptionEndpointsTests -parallel none -noLogo -noColor` | Passed: 22 total, 0 failed, 0 skipped. |
| Sample focused related tests | `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -parallel none -noLogo -noColor` | Passed: 23 total, 0 failed, 0 skipped. |

### Checklist

- [x] API/source-contract tests generated where applicable.
- [x] E2E/UI tests generated if UI exists: N/A, no UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and existing YamlDotNet test dependency.
- [x] Tests cover the happy path timing claims.
- [x] Tests cover critical drift/error cases: command status changes and DAPR pub/sub component contract changes.
- [x] Tests use proper locators: N/A for non-UI story.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary updated.
- [x] Tests saved to the existing documentation test directory.
- [x] Summary includes coverage metrics.

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

# Test Automation Summary - Story 8.5 Cross-Aggregate Timing

**Story:** 8.5 - Document Cross-Aggregate Timing and Eventual Consistency
**Workflow:** dev-story - **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source-backed documentation checks

## Scope

Source-backed timing documentation for EventStore command status, event persistence, DAPR pub/sub delivery, subscriber processing, Tenants query projections, consuming-service local projections, stale reads, and support-safe diagnostics. No production runtime behavior was changed.

## Generated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs` - validates command lifecycle/status terms, source file citations, Mermaid flow coverage, source-of-truth versus projection wording, DAPR pub/sub/dead-letter/resiliency contracts, at-least-once/idempotency/order guidance, unsafe wait/security exclusions, and cross-document navigation links.

## Coverage

- Story 8.5 acceptance criteria: 5/5 covered by the rewritten timing guide, navigation updates, source-backed documentation tests, and focused Client/Sample projection tests.
- Happy path covered: command submission, aggregate handling, event storage, publication, subscriber processing, local projection save, and local access/configuration reads.
- Critical drift/error cases covered: `PublishFailed`, republish/drain recovery, subscriber redelivery, DAPR dead-letter topic configuration, stale local projections, fail-closed access decisions, support-safe diagnostics, no `Thread.Sleep`/fixed-delay correctness, no synchronous subscriber enforcement claim, and no cross-service ordering assumption.
- UI workflows: N/A; Story 8.5 has no browser UI surface.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Red test proof | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CrossAggregateTimingDocumentationTests -parallel none -noLogo -noColor` before doc rewrite | Failed: 5 total, 5 failed, confirming the prior guide and README navigation were incomplete. |
| Server targeted via VSTest | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CrossAggregateTimingDocumentationTests -m:1 /nr:false /p:BuildInParallel=false` | Built, then VSTest aborted before execution with `SocketException (13): Permission denied`. |
| Story 8.5 documentation tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CrossAggregateTimingDocumentationTests -parallel none -noLogo -noColor` | Passed: 7 total, 0 failed, 0 skipped. |
| Documentation namespace regression | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` | Passed: 33 total, 0 failed, 0 skipped. |
| Client focused related tests | `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventProcessorTests -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventSubscriptionEndpointsTests -parallel none -noLogo -noColor` | Passed: 22 total, 0 failed, 0 skipped. |
| Sample focused related tests | `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -parallel none -noLogo -noColor` | Passed: 23 total, 0 failed, 0 skipped. |
| Debug build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Full direct xUnit suite | Contracts, Client, Testing, Server, Sample, and Integration test assemblies under `bin/Debug/net10.0` with `-parallel none` | Passed: 1338 total, 0 failed, 27 skipped. Skips were DAPR/performance prerequisite-gated. |
| Live prerequisite check | `dapr --version`; `docker ps --format '{{.Names}}'`; `dotnet aspire --version` | DAPR available: CLI 1.17.1, runtime 1.17.8. Docker blocked: permission denied connecting to `unix:///var/run/docker.sock`. Aspire CLI unavailable: `dotnet-aspire does not exist`. Live AppHost timing proof was not claimed. |

## Checklist

- [x] Source-backed documentation tests generated.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover command lifecycle, subscriber timing, projection lag, recovery, and support-safe diagnostics.
- [x] Focused Client/Sample tests anchor the timing claims.
- [x] Direct xUnit fallback used where VSTest cannot open sockets.
- [x] Full direct xUnit regression suite passes with only prerequisite-gated skips.
- [x] Live infrastructure limitation recorded without claiming live execution.

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

# Test Automation Summary — Story 8.3

**Story:** 8.3 — Document the Sample Consuming Service Walkthrough  
**Workflow:** dev-story · **Date:** 2026-06-01  
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

Documentation story for the existing sample consuming service. No runtime
behavior or package dependencies changed.

## Generated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs` —
  validates that `docs/sample-consuming-service-walkthrough.md` references the
  real sample/AppHost/test files, documents the current subscription calls,
  keeps the registration snippet synchronized with `samples/Hexalith.Tenants.Sample/Program.cs`,
  covers projection events/access/configuration behavior, and rejects JWT-like
  tokens or sensitive logging guidance. Review added assertions that the
  `/tenants/events` route is documented as the `MapTenantEventSubscription()`
  endpoint rather than an `HexalithTenantsOptions` default, and that demo
  navigation uses the current under-20-lines registration wording.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server focused build | `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Server focused via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~SampleConsumingServiceWalkthroughDocumentationTests -m:1 /nr:false /p:UseSharedCompilation=false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Server documentation via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -namespace Hexalith.Tenants.Server.Tests.Documentation -noLogo -noColor` | Passed: 17 total, 0 failed, 0 skipped. |
| Server full via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor` | Passed: 697 total, 0 failed, 0 skipped. |
| Sample tests via xUnit runner | `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor` | Passed: 31 total, 0 failed, 0 skipped. |
| Solution build | `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Tier 1 direct xUnit | Contracts, Client, and Testing test assemblies under `bin/Debug/net10.0` | Passed: 378 total, 0 failed, 0 skipped. |
| Integration direct xUnit | `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor` | Passed: 217 total, 0 failed, 26 skipped. Skips were DAPR/performance prerequisite-gated. |
| Full direct xUnit regression | Contracts, Client, Testing, Server, Sample, and Integration test assemblies under `bin/Debug/net10.0` | Passed: 1323 total, 0 failed, 26 skipped. |
| Senior review focused Server build | `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Senior review focused Server documentation via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.SampleConsumingServiceWalkthroughDocumentationTests -parallel none -noLogo -noColor` | Passed: 6 total, 0 failed, 0 skipped. |
| Senior review focused Sample build | `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Senior review focused Sample tests via xUnit runner | `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -class Hexalith.Tenants.Sample.Tests.Handlers.SampleLoggingEventHandlerTests -parallel none -noLogo -noColor` | Passed: 30 total, 0 failed, 0 skipped. |
| Senior review focused VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~SampleConsumingServiceWalkthroughDocumentationTests -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |

## Checklist

- [x] Source-backed documentation tests generated.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover registration, projection events, access behavior, configuration behavior, adaptation guidance, and security posture.
- [x] Tests verify documented C# registration snippet against sample source.
- [x] Review assertions cover route/options wording and navigation registration wording.
- [x] No live Docker, DAPR sidecar, AppHost, or pub/sub execution claimed for this documentation story; prerequisite-gated integration tests skipped as designed.

---

# Test Automation Summary — Story 8.3 QA Generate E2E Tests

**Story:** 8.3 — Document the Sample Consuming Service Walkthrough  
**Workflow:** qa-generate-e2e-tests · **Date:** 2026-06-01  
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

QA gap analysis of the sample consuming service walkthrough and its existing
sample API coverage. Tests only; no story validation or production code review.

## Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Walkthrough source synchronization | `SampleConsumingServiceWalkthroughDocumentationTests` pins required sample/AppHost/test files, subscription calls, projection events, adaptation guidance, sensitive-data exclusions, and the C# registration snippet against `Program.cs`. | Covered. |
| Access workflow API behavior | `AccessCheckEndpointsTests` covers granted access, non-member denial, disabled/unknown tenant denial, unknown/out-of-range roles, unknown tenant `404`, bad IDs `400`, projection-store dependency, and event-pipeline updates for user add/remove/role-change and tenant disable/enable. | Covered. |
| Configuration workflow API behavior | `TenantConfigurationEndpointsTests` covers sample namespace filtering, unknown tenant `404`, bad tenant ID `400`, projection-store dependency, event-pipeline set/update/remove behavior, unrelated namespace hiding, and repeated remove idempotency. | Covered. |
| Support-safe sample logging | `SampleLoggingEventHandlerTests` covers registered event handling, log levels, and no raw sample user ID or role in user-event logs. | Covered. |
| Browser UI workflow | Story 8.3 has no browser UI surface. | N/A. |

No additional test-code gaps were found during this QA pass.

## Generated / Validated Tests

### API Tests

- [x] `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs` —
  validates the projection-backed access endpoint happy path plus critical
  fail-closed and event-pipeline cases.
- [x] `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs` —
  validates the projection-backed configuration endpoint happy path plus
  namespace filtering, missing tenant, invalid input, and idempotent remove cases.
- [x] `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs` —
  validates support-safe sample logging behavior for handled events.
- [x] `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs` —
  validates the under-20-lines registration target used by the walkthrough.

### E2E Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs` —
  covers the non-UI walkthrough workflow end to end at the documentation-contract
  layer: package/setup guidance, subscription mapping, projection behavior,
  access/configuration documentation, adaptation boundaries, security posture,
  and source-checked snippets.

## Coverage

- Story 8.3 acceptance criteria: 5/5 covered by source-backed documentation
  tests and sample endpoint workflow tests.
- API workflows covered: access grant, access denial, bad request, not found,
  projection event add/remove/role-change/disable/enable, configuration
  set/update/remove, and namespace filtering.
- Critical error cases covered: blank identifiers, unknown tenants, disabled or
  unknown tenant status, non-members, unknown/out-of-range roles, unrelated
  configuration namespaces, repeated removes, synchronous-client dependency
  regression, JWT-like token leakage, and sensitive logging guidance.
- UI workflows: N/A, Story 8.3 has no browser UI surface.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server focused build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Sample focused build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~SampleConsumingServiceWalkthroughDocumentationTests -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Sample targeted via VSTest | `dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~AccessCheckEndpointsTests\|FullyQualifiedName~TenantConfigurationEndpointsTests\|FullyQualifiedName~SampleRegistrationTests\|FullyQualifiedName~SampleLoggingEventHandlerTests" -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Built, then VSTest aborted on sandbox socket setup: `SocketException (13): Permission denied`. |
| Server targeted via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.SampleConsumingServiceWalkthroughDocumentationTests -parallel none -noLogo -noColor` | Passed: 5 total, 0 failed, 0 skipped. |
| Sample targeted via xUnit runner | `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Endpoints.TenantConfigurationEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -class Hexalith.Tenants.Sample.Tests.Handlers.SampleLoggingEventHandlerTests -parallel none -noLogo -noColor` | Passed: 30 total, 0 failed, 0 skipped. |

## Checklist

- [x] API tests generated/validated where applicable.
- [x] E2E tests generated/validated for the non-UI walkthrough workflow.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy paths.
- [x] Tests cover critical fail-closed, invalid-input, drift, and sensitive-data cases.
- [x] Semantic UI locators are N/A; tests use source-backed documentation assertions and sample endpoint calls.
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

---

# Test Automation Summary - Story 8.4 Reactive Access Aha Moment Demo

**Story:** 8.4 - Produce the Reactive Access "Aha Moment" Demo
**Workflow:** dev-story - **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + source/static documentation checks

## Scope

Validated the demo documentation and automation entry points for the reactive access proof: EventStore command/status routes, Tenants query routes, sample subscriber observation, script payload correctness, local auth mode separation, eventual-consistency wording, and support-safe output.

## Generated Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs` - validates current topology references, command JSON payloads, script command drift fixes, auth guidance, support-safe assets, one-live-subscriber wording, eventual consistency, and related-guide links.

## Coverage

- Story 8.4 acceptance criteria: 5/5 covered by source-backed documentation/script tests plus existing Client/Sample projection and subscription tests.
- Demo command path covered: `POST /api/v1/commands`, `GET /api/v1/commands/status/{correlationId}`, ULID-shaped command IDs, `global-administrators` bootstrap domain, tenant command `aggregateId == payload.TenantId`, and `TenantContributor` enum-name payloads.
- Observation path covered: Aspire resources `eventstore`, `tenants`, and `sample`; Sample `/access/{tenantId}/{userId}`; DAPR topic `tenants.events`; `MapTenantEventSubscription()`; Tenants current-state and audit query surfaces.
- Safety covered: no raw JWT-like tokens, no `client_secret`, no full event payload logging guidance, and explicit Keycloak versus `EnableKeycloak=false` HMAC fallback separation.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Script syntax | `bash -n scripts/demo.sh` | Passed. |
| Script syntax | `pwsh -NoProfile -Command '$ErrorActionPreference="Stop"; $null = [scriptblock]::Create((Get-Content -Raw scripts/demo.ps1)); "pwsh syntax ok"'` | Passed. |
| Server documentation tests via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~Documentation --no-restore` | MSBuild/VSTest aborted before execution in this sandbox with `SocketException (13): Permission denied`. |
| Server build | `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Sample build | `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Client build | `dotnet build tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` | Passed: 0 warnings, 0 errors. |
| Focused Story 8.4 documentation tests via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.AhaMomentDemoDocumentationTests` | Passed: 6 total, 0 failed, 0 skipped. |
| Existing related documentation tests via xUnit runner | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.QuickstartDocumentationTests -class Hexalith.Tenants.Server.Tests.Documentation.SampleConsumingServiceWalkthroughDocumentationTests -class Hexalith.Tenants.Server.Tests.Documentation.EventContractReferenceDocumentationTests` | Passed: 18 total, 0 failed, 0 skipped. |
| Full Server direct xUnit regression | `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none` | Passed: 704 total, 0 failed, 0 skipped. |
| Full Client direct xUnit regression | `tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests -noLogo -noColor -parallel none` | Passed: 92 total, 0 failed, 0 skipped. |
| Full Sample direct xUnit regression | `samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` | Passed: 31 total, 0 failed, 0 skipped. |
| Live AppHost demo prerequisite check | `docker info --format '{{.ServerVersion}}'` | Blocked: permission denied connecting to `unix:///var/run/docker.sock`. DAPR CLI 1.17.1/runtime 1.17.8 and Aspire CLI 13.3.5 are installed, but live Docker-backed AppHost execution was not available in this sandbox. |

## Checklist

- [x] Source-backed documentation tests generated.
- [x] Tests cover command examples, scripts, auth mode split, support safety, and eventual consistency.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests are independent and order-free.
- [x] Source-backed tests and focused regressions pass.
- [x] Live infrastructure limitation recorded without claiming live execution.

---

# Test Automation Summary - Story 8.4 QA Generate E2E Tests

**Story:** 8.4 - Produce the Reactive Access "Aha Moment" Demo
**Workflow:** qa-generate-e2e-tests - **Date:** 2026-06-01
**Framework:** xUnit v3 (3.2.2) + Shouldly (4.3.0) + Aspire/DAPR-gated integration tests

## Scope

QA gap analysis of Story 8.4's reactive access demo coverage, then auto-application of discovered test gaps. Tests only.

## Gap Analysis

| Area | Existing coverage | Verdict |
|------|-------------------|---------|
| Demo docs and scripts | `AhaMomentDemoDocumentationTests` parsed command JSON, checked route/topology references, auth split, support safety, eventual consistency, and script drift fixes. | Covered. |
| Sample/client projection behavior | Existing Client and Sample tests covered `MapTenantEventSubscription()`, event processing, projection updates, `/access`, and support-safe logging. | Covered. |
| Full AppHost reactive access proof | No generated test exercised the demo path through the Aspire topology: EventStore command gateway -> `tenants.events` -> Sample `/access` transition. | **Gap found and closed.** |

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs` -
  `Demo_has_Aspire_E2E_coverage_for_reactive_access_transition` pins that Story 8.4 has a source-backed Aspire E2E test covering bootstrap, create tenant, add user, remove user, command/status routes, and `granted -> denied` access evidence.

### E2E Tests

- [x] `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` -
  `Aha_moment_demo_revokes_sample_access_from_tenant_events` submits the full demo command flow to `POST /api/v1/commands`, polls `GET /api/v1/commands/status/{correlationId}`, waits for Sample `/access/{tenantId}/{userId}` to report `granted`, removes the user, then waits for `denied` from the Sample local projection.

## Coverage

- Story 8.4 acceptance criteria: 5/5 covered across documentation-contract tests, source-backed client/sample tests, and the new Aspire-gated demo E2E test.
- Happy path covered: bootstrap global admin, create tenant, add contributor, observe local access granted, remove user, observe local access denied.
- Critical error/drift cases covered: already-bootstrapped global admin is accepted as rerunnable setup, the Aspire E2E token includes the required `GlobalAdministrator` role claim for privileged tenant commands, stale command domains/message IDs/script payloads are rejected by documentation tests, and Docker/DAPR prerequisites gate live AppHost execution without false pass claims.
- UI workflows: N/A; Story 8.4 has no browser UI surface.

## Validation Results

| Lane | Command | Result |
|------|---------|--------|
| Server build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Integration build | `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_HOME=/tmp NUGET_PACKAGES=/home/administrator/.nuget/packages dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` | Passed: 0 warnings, 0 errors. |
| Server targeted via VSTest | `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AhaMomentDemoDocumentationTests -m:1 /nr:false /p:BuildInParallel=false` | Built, then VSTest aborted before execution with `SocketException (13): Permission denied`. |
| Integration targeted via VSTest | `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~Aha_moment_demo_revokes_sample_access_from_tenant_events -m:1 /nr:false /p:BuildInParallel=false` | Built, then VSTest aborted before execution with `SocketException (13): Permission denied`. |
| Story 8.4 documentation tests via xUnit runner | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.AhaMomentDemoDocumentationTests -parallel none -noLogo -noColor` | Passed: 8 total, 0 failed, 0 skipped. |
| Aspire topology tests via xUnit runner | `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.AspireTopologyTests -parallel none -noLogo -noColor` | Passed with prerequisite skips: 5 total, 0 failed, 5 skipped. New Story 8.4 E2E test skipped because DAPR integration prerequisites are unavailable. |
| Client focused related tests | `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventProcessorTests -class Hexalith.Tenants.Client.Tests.Subscription.TenantEventSubscriptionEndpointsTests -class Hexalith.Tenants.Client.Tests.Handlers.TenantProjectionEventHandlerTests -parallel none -noLogo -noColor` | Passed: 38 total, 0 failed, 0 skipped. |
| Sample focused related tests | `dotnet samples/Hexalith.Tenants.Sample.Tests/bin/Debug/net10.0/Hexalith.Tenants.Sample.Tests.dll -class Hexalith.Tenants.Sample.Tests.Endpoints.AccessCheckEndpointsTests -class Hexalith.Tenants.Sample.Tests.Registration.SampleRegistrationTests -class Hexalith.Tenants.Sample.Tests.Handlers.SampleLoggingEventHandlerTests -parallel none -noLogo -noColor` | Passed: 24 total, 0 failed, 0 skipped. |
| Live prerequisite check | `dapr --version`; `docker info --format '{{.ServerVersion}}'` | DAPR available: CLI 1.17.1, runtime 1.17.8. Docker blocked: permission denied connecting to `unix:///var/run/docker.sock`, so live AppHost execution was not claimed. |

## Senior Review Addendum

- Fixed HMAC fallback token drift in `scripts/demo.sh`, `scripts/demo.ps1`, and `AspireTopologyTests`: fallback demo tokens now target the EventStore command gateway's `hexalith-eventstore` development auth settings.
- Added `Hmac_fallback_tokens_target_the_EventStore_command_gateway` to keep the scripts from regressing to Tenants-only development auth values.
- Re-ran script syntax, Server/Integration single-node builds, direct xUnit Story 8.4 documentation tests, Aspire topology tests with prerequisite skips, and focused Client/Sample projection tests successfully.

## Checklist

- [x] API tests generated where applicable.
- [x] E2E tests generated for the non-UI reactive access workflow.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical rerun/drift/prerequisite cases.
- [x] Semantic UI locators are N/A; tests use HTTP API calls and source-backed documentation assertions.
- [x] Tests have clear descriptions.
- [x] No hardcoded sleeps in generated source-backed tests; the Aspire E2E uses bounded polling for asynchronous command/projection completion.
- [x] Tests are independent and order-free.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
