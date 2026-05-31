# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - deterministic `POST /api/v1/commands` Problem Details coverage for every current Tenants rejection type.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - reflection guard fails when a Tenants rejection type lacks an explicit HTTP status/reason-code expectation.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - rejection responses assert `application/problem+json`, `type`, `title`, `status`, `instance`, `correlationId`, `tenantId`, `reasonCode`, `rejectionType`, and `correctiveAction`.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - rejection responses assert sensitive command payload, token, stack trace, local path, and synthetic tenant/user markers are not leaked.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs` - rejection event contracts reject prose/sensitive field names and unsupported payload field types.

### E2E Tests
- [x] UI E2E: not applicable. Story 2.6 is a backend command API and persisted rejection contract workflow.
- [x] API E2E/integration: command gateway tests exercise the HTTP boundary end-to-end through the in-memory ASP.NET host and substituted command stores/router.

## Coverage
- API endpoints: 1/1 story endpoint covered (`POST /api/v1/commands`).
- UI features: 0/0 applicable.
- Tenants rejection mappings: 13/13 explicit expectations covered.
- Missing-resource mappings: 2/2 covered as 404.
- Duplicate/current-state mappings: 5/5 covered as 409.
- Disabled, authorization, escalation, configuration-limit, user-not-in-tenant, and last-admin mappings: 6/6 covered as 422.
- Structured rejection contract guardrails: all current `IRejectionEvent` records in `Hexalith.Tenants.Contracts.Events.Rejections` covered by reflection.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false` - compiled, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` while starting `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer`.
- [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests --filter "CommandApiRuntimeIntegrationTests" -m:1 -nr:false` - compiled, then VSTest aborted before executing tests with `System.Net.Sockets.SocketException (13): Permission denied` while starting `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer`.
- [x] `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests -class Hexalith.Tenants.Contracts.Tests.NamingConventionTests -noLogo -noColor` - passed: 29 total, 0 failed.
- [x] `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -noLogo -noColor` - passed: 48 total, 0 failed.
- [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests -class Hexalith.Tenants.Contracts.Tests.NamingConventionTests -noLogo -noColor` - passed: 29 total, 0 failed.
- [x] `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -noLogo -noColor` - passed: 48 total, 0 failed.
- [ ] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Hexalith.EventStore.IntegrationTests.csproj --configuration Release -warnaserror -m:1 -nr:false` - blocked by restricted network access to `https://api.nuget.org/v3/index.json` during restore.

## Next Steps

- Run the focused `dotnet test` commands in CI or another environment that permits VSTest socket transport.
