# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers Story 2.2 add-member command gateway submission mapping, literal tenant/user id preservation, ULID message-id idempotency key usage, explicit role serialization by name, returned correlation-id capture, validation blocking before submit, and safe rejection/status mapping.

### E2E Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs` - Covers the add-member workflow with stable selectors, assignable roles only, literal user id submission, projection-confirmed completion, projection-free pending state, already-member rejection, fail-closed freshness/authorization/lifecycle/command-surface reasons, and duplicate submission blocking while a command is in flight.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs` - Covers lifecycle non-collapse, projection evidence requirements, SignalR nudge-only behavior, and terminal non-success states that cannot become confirmed from projection evidence.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Preserves the member-table context, change/remove unavailable slots, stale/degraded states, responsive/forced-colors hooks, and AddMember EN/FR resource parity.

## Coverage

- Story 2.2 acceptance criteria: 10/10 covered across add-member gateway, component workflow, lifecycle state, member-table preservation, and resource parity tests.
- API/gateway boundaries: `POST /api/v1/commands` submission shape through the server-side command gateway, message id/correlation id handling, safe rejection mapping, status lookup mapping, and validation before submission.
- UI workflow: add member happy path through projection-confirmed evidence, required-field validation, role selection excluding `Unknown`, no invitation/email/users navigation, no optimistic member row, duplicate-submit lockout, fail-closed unavailable reasons, audit handoff, and live-region behavior.
- Critical error cases: `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, empty user id, `TenantRole.Unknown`, stale/unknown freshness, disabled/unknown tenant lifecycle, command-surface unavailable, in-flight command, projection pending without evidence, SignalR nudge without confirmation, and no raw payload/token/correlation leakage.

## Validation

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 -nr:false` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 205 total, 0 errors, 0 failed, 0 skipped.

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
