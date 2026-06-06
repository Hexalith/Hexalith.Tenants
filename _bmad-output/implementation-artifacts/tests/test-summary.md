# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers Story 2.2 add-member command gateway submission mapping, literal tenant/user id preservation, ULID message-id idempotency key usage, explicit role serialization by name, returned correlation-id capture, validation blocking before submit, and safe rejection/status mapping.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` - Covers Story 2.3 change-role command gateway submission mapping, literal tenant/user id preservation, ULID message-id idempotency key usage, `ChangeUserRole` payload serialization with `NewRole` by name, returned correlation-id capture, validation blocking for empty fields and `TenantRole.Unknown`, and safe rejection/status mapping for role escalation, missing member, insufficient permissions, disabled tenant, and unknown tenant.

### E2E Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs` - Covers the add-member workflow with stable selectors, assignable roles only, literal user id submission, projection-confirmed completion, projection-free pending state, already-member rejection, fail-closed freshness/authorization/lifecycle/command-surface reasons, and duplicate submission blocking while a command is in flight.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs` - Covers lifecycle non-collapse, projection evidence requirements, SignalR nudge-only behavior, and terminal non-success states that cannot become confirmed from projection evidence.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Preserves the member-table context, change/remove unavailable slots, stale/degraded states, responsive/forced-colors hooks, and AddMember EN/FR resource parity.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs` - Covers stable selectors, assignable roles excluding `Unknown`, current-role `AlreadyApplied`, literal user id submission, projection-confirmed role evidence, projection-free pending state, manual refresh through status lookup and projection re-query, fail-closed freshness/authorization/lifecycle/command-surface reasons, inline label/ARIA associations, spoofed invalid-role validation before gateway submission, owner-count risk warning without hard-blocking, duplicate submission blocking, close callback focus recovery, terminal lifecycle non-collapse, safe rejection copy, live-region politeness, and no internal correlation/payload/token disclosure.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantChangeRoleCommandSnapshotTests.cs` - Covers current-role and zero-event backend NoOp `AlreadyApplied`, accepted/projection-pending non-collapse, projection evidence requirements, missing target user unable-to-verify state, SignalR nudge-only behavior, and terminal non-success states that cannot become confirmed from projection evidence.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - Preserves member-table caption, headers, row relationships, add-member flow, remove-member unavailable slots, copy buttons, stale/degraded messaging, responsive/forced-colors hooks, and ChangeRole EN/FR resource parity while adding the row-scoped change-role flow.

## Coverage

- Story 2.2 acceptance criteria: 10/10 covered across add-member gateway, component workflow, lifecycle state, member-table preservation, and resource parity tests.
- Story 2.3 acceptance criteria: 10/10 covered across change-role gateway, component workflow, lifecycle state, member-table/detail preservation, responsive/accessibility style checks, and EN/FR resource parity tests.
- API/gateway boundaries: `POST /api/v1/commands` submission shape through the server-side command gateway, message id/correlation id handling, safe rejection mapping, status lookup mapping, and validation before submission.
- UI workflow: add member happy path through projection-confirmed evidence, required-field validation, role selection excluding `Unknown`, no invitation/email/users navigation, no optimistic member row, duplicate-submit lockout, fail-closed unavailable reasons, audit handoff, and live-region behavior.
- Story 2.3 UI workflow: change member role from row context, current role remains visible/selectable, same-role NoOp is `AlreadyApplied`, allowed new role submits through the server-side gateway, confirmation requires authoritative member projection evidence, manual refresh re-checks status and projection truth before confirmation, SignalR is nudge-only, owner-count risk is warning semantics rather than a hard block, and duplicate submissions are blocked while in flight.
- Critical error cases: `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, empty user id, `TenantRole.Unknown`, stale/unknown freshness, disabled/unknown tenant lifecycle, command-surface unavailable, in-flight command, projection pending without evidence, SignalR nudge without confirmation, and no raw payload/token/correlation leakage.
- Story 2.3 critical error cases: `RoleEscalationRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, empty tenant/user id validation, `TenantRole.Unknown`, stale/unknown freshness, unauthorized/degraded detail, disabled/unknown tenant lifecycle, command-surface unavailable, in-flight command, projection pending without requested-role evidence, missing target member unable-to-verify, SignalR nudge without confirmation, and no raw payload/token/correlation leakage.

## Validation

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 -nr:false` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 205 total, 0 errors, 0 failed, 0 skipped.
- Story 2.3 validation: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.3 validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so the xUnit v3 executable fallback was used.
- Story 2.3 QA generation validation: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Story 2.3 QA generation validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so the xUnit v3 executable fallback was used.
- Story 2.3 QA generation validation: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 252 total, 0 errors, 0 failed, 0 skipped.
- Broader executable regression signal: Contracts.Tests 103/103, Client.Tests 47/47, and Testing.Tests 181/181 passed. Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and an unrelated deployment-readiness summary expectation.

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
