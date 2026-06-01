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
