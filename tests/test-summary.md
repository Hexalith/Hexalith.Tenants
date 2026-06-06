# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 1.8. Support-safe copy is browser clipboard interop over values already rendered by the server-side UI; no backend endpoint, query contract, direct browser API call, token storage, or command path was added.
- [x] Story 2.1 command gateway tests cover EventStore `SubmitCommandRequest` mapping, ULID-shaped message id idempotency key creation through the registered factory, literal tenant id preservation, returned correlation-id capture, correlation-id status lookup, validation blocking before submit, safe gateway exception mapping, `TenantAlreadyExistsRejection`, insufficient permissions, missing/malformed status lookup, publish failure, and timeout.

### E2E Tests
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` - bUnit workflow coverage for literal clipboard writes, GUID/ULID-shaped identifier preservation, blocked unsafe/empty values before JS interop, permission denial, insecure context, missing Clipboard API, generic JS failure, disconnected Blazor Server circuit handling, live-region politeness, no unsafe value disclosure, and classifier source checks against identifier parsing/normalization.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` - tenant list workflow coverage for stable copy selectors, tenant-id copy controls, list filtering/sorting/paging preservation, source-safety, responsive column stability, and forced-colors hooks.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` - tenant detail, read-only configuration, and member table workflow coverage for identity copy, safe configuration key/value copy, blocked sensitive values, member user-id copy, live read-surface preservation, resource parity, CSS/accessibility hooks, and clipboard source-safety.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` - My Tenants workflow coverage for `tenants-my-*` copy selectors, tenant-id copy controls, paging/freshness preservation, no mutation controls, and no browser backend/token storage.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` - user lookup workflow coverage for `tenants-user-*` copy selectors, literal target handling, tenant-id copy controls, lookup state preservation, no hidden membership leakage, and no browser backend/token storage.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs` - create tenant command-flow coverage for stable selectors, fail-closed unavailable state, required-field validation, literal tenant id submission, no projection-free confirmation, projection-confirmed lifecycle, safe rejection text, status rejection with projection evidence, degraded publish-failed state, unable-to-verify recovery, audit handoff, and live-region politeness.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs` - lifecycle reducer coverage for accepted/projection-pending/confirmed non-collapse, SignalR nudge-only behavior, audit pending/unavailable handoff, assertive non-success states, and the success-prohibited invariant for rejected/degraded/unable-to-verify states.
- [x] Story 2.1 review regression tests: accepted-vs-projection-pending non-collapse on evidence-free re-query (AC4), focusable lifecycle region for fail-closed focus recovery (AC6), and `aria-describedby` referencing only a rendered validation element (a11y).

## Coverage
- Story 1.8 acceptance criteria: 7/7 covered across support-safe copy component tests, tenant list/detail/configuration/member/My Tenants/user lookup surface tests, source-safety tests, style/accessibility checks, and resource parity checks.
- API endpoints: 0 new endpoints; existing read gateways remain covered by the surface tests and no new API adapter was required.
- UI surfaces covered: tenant list, tenant detail identity, My Tenants, user membership lookup, read-only configuration, and member table.
- Critical error cases covered: unsafe value, empty value, permission denial, insecure browser context, missing Clipboard API, generic JS failure, and disconnected Blazor Server circuit.
- Story 2.1 acceptance criteria: 8/8 covered across command gateway tests, create-flow component tests, lifecycle state tests, composition/resource tests, and workspace preservation checks.
- Story 2.1 critical error cases covered: duplicate tenant rejection, authorization rejection, missing required fields, unavailable command surface, malformed/unavailable status lookup, publish failure, timeout, no false success before projection evidence, no success after rejected/degraded/unable-to-verify status, SignalR nudge without confirmation, and audit evidence unavailable/pending handoff.

## Validation
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor` passed: 172 total, 0 errors, 0 failed, 0 skipped (169 dev tests + 3 story-automator-review regression tests).
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -p:BuildInParallel=false -p:RestoreBuildInParallel=false -m:1 -v:m` passed with 0 warnings and 0 errors.
- xUnit v3 executable fallback passed for Tier 1/UI projects: Contracts.Tests 103, Client.Tests 47, Testing.Tests 181, Sample.Tests 31, UI.Tests 156; all 518 passed with 0 failures.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor` was attempted and failed in pre-existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence unrelated to Story 2.1.

## Checklist
- [x] API tests generated if applicable.
- [x] E2E tests generated for the implemented UI surfaces using the existing xUnit v3/bUnit framework.
- [x] Tests use standard xUnit v3, Shouldly, NSubstitute, bUnit, and existing file-based style/resource assertions.
- [x] Tests cover happy path and critical unsafe/empty/unavailable/failed/disconnected cases.
- [x] Tests use semantic/accessibility-oriented assertions and stable selectors.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created with coverage metrics.
