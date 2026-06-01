# Test Automation Summary

## Story

Story 5.1: Persist Per-Tenant Detail Projections Without Silent Write Loss

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs` - validates tenant-detail optimistic concurrency, reload-and-reapply behavior, retry exhaustion, no downstream audit/index writes after terminal tenant-detail failure, support-safe diagnostics, and bounded diagnostic message/event sampling.
- [x] `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs` - validates the DAPR projection dispatch boundary uses `GetStateAndETagAsync` plus guarded `TrySaveStateAsync` with `ConcurrencyMode.FirstWrite` for the per-tenant detail key, and does not use plain `SaveStateAsync`.
- [x] Existing `TenantProjectionHandlerTests` continue to cover focused handler behavior for existing state, missing state, retries, audit/index sequencing, cancellation, and failure paths.

### E2E Tests

- [x] No browser UI exists for this story. The applicable end-to-end path is the projection request through `TenantProjectionHandler.ProjectAsync` into the production write policy and DAPR state-store adapter boundary.

## Coverage

- Story acceptance criteria: 2/2 covered by projection conformance and dispatcher tests.
- API/projection paths: tenant detail guarded writes, audit guarded writes, singleton index guarded writes, and DAPR dispatch shape covered.
- Happy path: tenant detail read/reapply/save succeeds with loaded state and ETag.
- Critical error cases: guarded-save conflict retry, retry exhaustion, downstream write suppression after tenant-detail exhaustion, safe structured diagnostics, and bounded diagnostic overflow covered.
- UI features: 0/0 applicable.

## Validation

- [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests" -m:1 -nr:false /p:NuGetAudit=false` - built successfully, then VSTest aborted before executing tests because the sandbox denied its TCP listener (`SocketException (13): Permission denied`).
- [x] Direct xUnit fallback: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests` - passed: 47 total, 0 errors, 0 failed, 0 skipped.
- [x] Direct xUnit fallback: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none` - passed: 579 total, 0 errors, 0 failed, 0 skipped.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI exists; no UI exists for this story.
- [x] Tests use standard project APIs: xUnit v3, Shouldly, NSubstitute, and existing projection fixture patterns.
- [x] Tests cover the happy path for tenant-detail projection reads, event application, and guarded saves.
- [x] Tests cover critical error cases: optimistic-concurrency conflicts, retry exhaustion, safe diagnostics, bounded diagnostic fields, and prevention of audit/index writes after tenant-detail terminal failure.
- [x] All generated tests run successfully through the direct xUnit runner.
- [x] Tests use clear descriptions.
- [x] Tests use semantic API/projection boundaries where applicable.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and have no order dependency.
- [x] Test summary created with coverage metrics.
