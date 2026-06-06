# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers EventStore command submission mapping, literal tenant id preservation, ULID message-id usage through the registered factory, correlation-id status lookup, validation blocking before submit, duplicate rejection, authorization rejection, not-found status lookup, malformed status payloads, rejected status payloads, publish-failed status, timed-out status, and safe message redaction.

### E2E Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs` - Covers create-tenant form workflow selectors, fail-closed unavailable state, required-field validation, literal tenant id submission, projection-confirmed completion, projection-free pending state, status rejection with projection evidence, degraded publish-failed state, unable-to-verify recovery state, safe duplicate text, audit handoff, live-region politeness, and no false Success styling.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs` - Covers lifecycle non-collapse, projection evidence requirements, SignalR nudge-only behavior, assertive non-success states, and the success-prohibited invariant for rejected/degraded/unable-to-verify states.
- [x] `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` and `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` - Preserve workspace composition, command-surface fail-closed behavior, resources, forced-colors hooks, and keyboard-reachable controls.

## Coverage

- API endpoints: 0 new Tenants endpoints; Story 2.1 submits only through the server-side EventStore command gateway path.
- API/gateway boundaries: command submission mapping, idempotency key generation, status lookup key selection, safe exception mapping, duplicate/authorization rejection, malformed/unavailable status, publish failure, and timeout.
- UI workflows: create tenant happy path through projection confirmation, validation block, command-surface unavailable, projection pending without evidence, rejection, degraded, unable to verify, refresh recovery action, audit handoff, stable `tenants-create-*` selectors, and live-region behavior.
- Critical error cases: `TenantAlreadyExistsRejection`, insufficient permissions, malformed command status response, missing command status, publish failure, timeout, missing required fields, projection evidence after non-success, no raw payload/token/correlation leakage, and no false Success.

## Validation

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 169 total, 0 errors, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 -nr:false` was attempted and is blocked before execution by the known .NET 10 Microsoft.Testing.Platform/VSTest target error.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -p:BuildInParallel=false -p:RestoreBuildInParallel=false -m:1 -v:m` passed with 0 warnings and 0 errors.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated for the implemented UI surface using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, NSubstitute, and bUnit APIs.
- [x] Tests cover happy path and critical validation/rejection/status/projection error cases.
- [x] Tests use semantic/accessibility-oriented assertions and stable selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
