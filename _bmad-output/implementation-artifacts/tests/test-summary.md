# Test Automation Summary

## Generated Tests

### API Tests

- [x] `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs` - additive explicit `target`, `scope`, and `outcome` audit evidence fields.
- [x] `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs` - global-administrator events persist system-scoped audit state under `audit:system`.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs` - `TenantAuditEntry` and `PaginatedResult<TenantAuditEntry>` public JSON shape, camelCase fields, string enum serialization, and explicit target/scope/outcome fields.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - `GET /api/tenants/{tenantId}/audit` response shape, `SubmitQuery` dispatch values, page-size forwarding and clamping, valid cursor forwarding, invalid route ID, missing subject, invalid category, invalid date window, non-admin `403` ProblemDetails, invalid cursor rejection, query-type mismatch, tenant/category scope mismatch, and date-range scope mismatch before routing.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs` - audit entry classification, actor metadata requirements, deterministic sorting, unknown-event tolerance, and support-safe narrative payloads.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs` - audit projection coverage for lifecycle, membership, role, configuration, and global-admin events, malformed payload tolerance, invariant failure propagation, support-safe configuration narratives, and timestamp/event ID ordering.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionHandlerTests.cs` - global-administrator projection writes support-safe system audit rows.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs` - dispatcher still routes global-administrator projection requests through the updated handler.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` - audit query authorization, empty result behavior, inclusive date boundaries, category filtering, stable pagination, cursor round trip, cursor mismatch rejection before audit-state reads, between-page consistency, tenant mismatch filtering, page-size defaults, max-size clamping, and system-scoped global-administrator audit query behavior.

### E2E Tests

- [ ] Not applicable: story 5.10 exposes an API/query surface and no UI workflow exists in the repository for tenant audit history.

## Coverage

- API endpoint: 1/1 covered for Story 5.10 (`GET /api/tenants/{tenantId}/audit`).
- Audit event categories: lifecycle, membership, role, configuration, and global-admin events covered, including the actual global-admin projection write path.
- Authorization cases: global administrator success, non-global-admin forbidden, missing authenticated subject, and safe forbidden ProblemDetails covered.
- Filtering cases: tenant isolation, `from`/`to` inclusive boundaries, empty ranges, invalid date window, optional category, invalid category, and mismatched stored tenant IDs covered.
- Cursor/pagination cases: default audit page size 100, valid page size forwarding, non-positive fallback, maximum 1,000 clamping, protected cursor forwarding, invalid cursor, query-type mismatch, tenant scope mismatch, date-range scope mismatch, category scope mismatch, stable ordering, and between-page changes covered.
- UI workflows: N/A, no UI surface exists for this story.

## Validation

- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --filter FullyQualifiedName~QueryDtoSerializationTests --no-restore /m:1 /nr:false -p:WarningsNotAsErrors=NU1900` compiled successfully, then VSTest aborted on sandbox socket setup (`System.Net.Sockets.SocketException (13): Permission denied`).
- `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -class Hexalith.Tenants.Contracts.Tests.Queries.QueryDtoSerializationTests -noLogo -parallel none` passed: 8 total, 0 failed, 0 skipped.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/ --no-restore /m:1 /nr:false -p:WarningsNotAsErrors=NU1900` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.TenantAuditReadModelTests -class Hexalith.Tenants.Server.Tests.Projections.TenantAuditProjectionTests -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -class Hexalith.Tenants.Server.Tests.Projections.GlobalAdministratorProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPolicyTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryCursorCodecTests -noLogo -parallel none` passed: 203 total, 0 failed, 0 skipped.
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests -noLogo -parallel none` passed: 10 total, 0 failed, 0 skipped.
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.GlobalAdministratorProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.TenantAuditReadModelTests -noLogo -parallel none` passed: 30 total, 0 failed, 0 skipped.
- `dotnet build tests/Hexalith.Tenants.IntegrationTests/ --no-restore /m:1 /nr:false -p:WarningsNotAsErrors=NU1900` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -noLogo -parallel none` passed: 81 total, 0 failed, 0 skipped.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore /m:1 /nr:false -p:WarningsNotAsErrors=NU1900` passed with 0 warnings and 0 errors.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E/UI tests marked not applicable because no UI exists for this story.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path and critical error cases.
- [x] Tests use semantic HTTP assertions and production query/cursor surfaces.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.
