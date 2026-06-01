# Test Automation Summary

## Story

Story 5.8: Query Tenant Details and Tenant Users

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - added HTTP coverage for typed `GET /api/tenants/{tenantId}` detail shape and exact `SubmitQuery` dispatch values.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - added HTTP coverage for `GET /api/tenants/{tenantId}/users` paginated payload shape, exact dispatch values, default/valid/clamped page-size payload forwarding, signed cursor forwarding, missing-auth rejection, and safe 403/404 ProblemDetails responses.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` - added actor coverage for projection-backed tenant detail DTOs, disabled status, configuration, concrete-member filtering, per-tenant state key reads, role-based read authorization, empty users pages, user ordering, page-size policy, missing-tenant behavior, invalid cursor precedence, protected cursors, and between-page membership changes.
- [x] Existing cursor rejection tests cover malformed cursors, wrong-scope cursors, query-type mismatches, rotated keys, router short-circuiting, and sanitized invalid-cursor responses.

### E2E Tests

- [x] No browser UI exists for this story. The applicable end-to-end coverage is API/controller integration plus projection actor query execution for `GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users`.

## Coverage

- API endpoints: 2/2 story endpoints covered (`GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`).
- Query dispatch contract: tenant, domain, aggregate ID, query type, entity ID, projection type, payload, cursor, and page size covered.
- Response shape: `tenantId`, `name`, `description`, `status`, `members`, `configuration`, `createdAt`, `items`, `cursor`, and `hasMore` covered.
- Projection behavior: enabled/disabled detail state, configuration, concrete-role member filtering, global-admin bypass, tenant-member read access, missing/forbidden no-payload behavior, and projection key isolation covered.
- Pagination behavior: omitted/default, valid, zero, negative, oversized, signed cursor continuation, invalid cursor, wrong-scope cursor, hidden row filtering, empty users, and between-page membership changes covered.
- UI features: 0/0 applicable.

## Validation

- [x] `dotnet restore tests/Hexalith.Tenants.IntegrationTests/ --ignore-failed-sources -m:1 /nr:false /p:NuGetAudit=false /p:TreatWarningsAsErrors=false` - passed with local package cache.
- [x] `dotnet test tests/Hexalith.Tenants.IntegrationTests/ --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests -m:1 /nr:false` - compiled successfully; VSTest aborted before execution due sandbox socket setup (`SocketException (13): Permission denied`).
- [x] Direct xUnit fallback: `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests` - passed: 59 total, 0 errors, 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Tenants.Server.Tests/ --no-restore --filter "FullyQualifiedName~TenantsProjectionActorTests|FullyQualifiedName~TenantQueryPaginationPolicyTests|FullyQualifiedName~TenantQueryPaginationPayloadParserTests|FullyQualifiedName~TenantQueryCursorCodecTests" -m:1 /nr:false` - compiled successfully; VSTest aborted before execution due sandbox socket setup (`SocketException (13): Permission denied`).
- [x] Direct xUnit fallback: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPolicyTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryPaginationPayloadParserTests -class Hexalith.Tenants.Server.Tests.Queries.TenantQueryCursorCodecTests` - passed: 170 total, 0 errors, 0 failed, 0 skipped.
- [x] `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --no-restore --filter FullyQualifiedName~QueryDtoSerializationTests -m:1 /nr:false` - compiled successfully; VSTest aborted before execution due sandbox socket setup (`SocketException (13): Permission denied`).
- [x] Direct xUnit fallback: `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -class Hexalith.Tenants.Contracts.Tests.Queries.QueryDtoSerializationTests` - passed: 5 total, 0 errors, 0 failed, 0 skipped.
- [x] `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nr:false /p:NuGetAudit=false` - passed: 0 warnings, 0 errors.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI exists; no UI exists for this story.
- [x] Tests use standard project APIs: xUnit v3, Shouldly, NSubstitute, WebApplicationFactory, and existing projection actor helpers.
- [x] Tests cover happy path tenant detail and tenant users workflows.
- [x] Tests cover critical error cases: missing auth, forbidden, not found, invalid cursor, wrong-scope cursor, and no-payload actor failures.
- [x] All generated tests run successfully through the direct xUnit runner.
- [x] Tests use API/controller and actor/query boundaries where applicable.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and have no order dependency.
- [x] Test summary created with coverage metrics.
