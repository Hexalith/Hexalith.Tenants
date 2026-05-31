# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - Verifies `/api/v1/commands` ignores client-supplied `actor:globalAdmin` extension metadata when the JWT is not globally authorized.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - Verifies `/api/v1/commands` marks the submitted command as globally authorized only when the JWT carries a recognized global-admin claim.

### E2E Tests
- [x] `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/GlobalAdminCommandEnvelopeTests.cs` - Verifies EventStore `SubmitCommand` to `CommandEnvelope` conversion strips untrusted global-admin extension metadata and adds the trusted extension only from `SubmitCommand.IsGlobalAdmin`.
- [x] Existing story coverage retained in `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`, `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/CommandPipelineIntegrationTests.cs`, `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`, and `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`.
- [x] Review fix: Story 2.3 command-envelope helpers now generate sortable unique IDs for message and correlation fields instead of GUID strings.

## Coverage
- API endpoints: 1/1 Story 2.3 command submission boundary covered (`POST /api/v1/commands`).
- UI features: 0/0 applicable; Story 2.3 is backend/domain authorization only.
- Trusted metadata boundaries: 2/2 covered (API sanitization and `SubmitCommandExtensions.ToCommandEnvelope()`).
- Tenant lifecycle commands: 4/4 covered by existing and generated tests (`CreateTenant`, `UpdateTenant`, `DisableTenant`, `EnableTenant`).
- Critical error cases: non-global actor rejection, untrusted global-admin extension stripping, and body/envelope tenant ID mismatch are covered.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` - passed with 0 warnings and 0 errors.
- [x] Review rerun: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` - passed with 0 warnings and 0 errors after review fixes.
- [ ] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "GlobalAdminCommandEnvelopeTests|CommandPipelineIntegrationTests|TenantAggregateTests|TenantMetricsTests" -m:1 -nr:false` - test projects compiled, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` while starting its socket server.
- [ ] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests --filter "CommandApiRuntimeIntegrationTests" -m:1 -nr:false` - test project compiled, then VSTest aborted with the same socket permission error.
- [ ] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests --filter "InMemoryTenantServiceTests|TenantConformanceTests" -m:1 -nr:false` - test project compiled, then VSTest aborted with the same socket permission error.

## Next Steps

- Run the same focused `dotnet test` commands in CI or another environment that permits VSTest socket transport.
