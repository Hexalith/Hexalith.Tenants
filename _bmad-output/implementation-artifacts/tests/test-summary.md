# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - `/process` dispatches `RemoveUserFromTenant` with current tenant state and returns one `UserRemovedFromTenant` payload.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - command API accepts `RemoveUserFromTenant`, routes the story payload, and preserves trusted global-admin context.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - command API maps `UserNotInTenantRejection` from `RemoveUserFromTenant` to deterministic 422 ProblemDetails.

### Aggregate and Fake Tests
- [x] `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs` - `UserRemovedFromTenant` remains payload-only with `TenantId` and `UserId`.
- [x] `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` - removal applies final state, preserves unrelated members and history, uses the EventStore envelope aggregate id for removal events/rejections, records insufficient-permission actor role details, allows global-admin last-owner removal, and denies removed owners residual authority.
- [x] `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs` - in-memory fake removal delegates through aggregate behavior and mutates membership state consistently.

### E2E Tests
- [x] Not applicable for UI: Story 3.2 is backend/domain command behavior and no UI exists in scope.

## Coverage
- API command routes: `RemoveUserFromTenant` happy path routing and `UserNotInTenantRejection` error mapping covered.
- Domain service callback: `/process` success path for removal covered with hydrated current tenant state.
- Aggregate branches: authorized, non-member, disabled/missing tenant, insufficient permission, global-admin, last-owner, post-removal authority, state mutation, and envelope aggregate-id consistency covered by focused tests.
- UI features: 0/0 applicable.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests" -m:1 -nr:false /p:NuGetAudit=false` - compiled, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` while starting `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer`.
- [x] `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -parallel none -noLogo` - passed: 54 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests -parallel none -noLogo -reporter quiet` - passed.
- [x] `tests/Hexalith.Tenants.Testing.Tests/bin/Debug/net10.0/Hexalith.Tenants.Testing.Tests -class Hexalith.Tenants.Testing.Tests.Fakes.InMemoryTenantServiceTests -parallel none -noLogo -reporter quiet` - passed.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests -parallel none -noLogo -reporter quiet` - passed.
- [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` - passed with 0 warnings and 0 errors.
- [x] Review rerun: VSTest focused Server, Testing, and Contracts commands compiled but aborted with the known `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit focused Server, Testing, Contracts, and Integration classes passed; Release build passed with 0 warnings and 0 errors.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated if UI exists: not applicable because no UI is in Story 3.2 scope.
- [x] Tests use xUnit v3 and Shouldly, matching the repository standard framework.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases: non-member removal and insufficient permissions.
- [x] Generated tests run successfully through the direct xUnit runner.
- [x] Tests use HTTP/domain-service command contracts and avoid hardcoded waits.
- [x] Tests are independent through local state setup and mocked command API dependencies.
- [x] Summary includes coverage metrics and sandbox VSTest fallback evidence.
