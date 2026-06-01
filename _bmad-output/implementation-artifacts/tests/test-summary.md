# Test Automation Summary

## Generated Tests

### API Tests

- [x] `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` - Tenant claim normalization and fail-closed Tenants system-tenant validator coverage, including global-admin-shaped principals without `eventstore:tenant`.
- [x] `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` - Production JWT startup validation for missing/invalid settings, environment-variable overrides, development settings, and safe validation messages.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` - Protected query endpoint JWT coverage for production-like valid tokens, invalid tokens, missing/blank/wrong/wrong-cased tenant claims, safe ProblemDetails, router non-dispatch, and supported source-claim normalization.
- [x] `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` - Protected command endpoint JWT coverage for valid system tenant claims, missing/global-admin-missing/blank/wrong/wrong-cased tenant claims, supported source-claim normalization, safe authorization failures, and router non-dispatch.

### E2E Tests

- [x] Not applicable: Story 7.3 has no browser UI surface. The implemented behavior is production authentication configuration and protected API authorization, covered by live ASP.NET Core JWT middleware integration tests plus deterministic configuration/unit tests.

## Coverage

- Story 7.3 acceptance criteria: 7/7 covered by startup validation tests, claim-contract unit tests, query integration tests, command integration tests, and production-auth documentation checks from the story implementation.
- Production JWT startup validation covered: committed placeholders fail in Production; required `Authority`, `Issuer`, and `Audience` omissions fail; non-HTTPS/malformed authorities fail; production `SigningKey` fails without echoing secret values; `RequireHttpsMetadata=false` fails; deployment and environment-variable overrides pass.
- Token validation covered: production-like valid token, missing token, malformed token, invalid signature, wrong issuer, wrong audience, and expired token.
- Tenant-claim authorization covered: direct `eventstore:tenant=system`, JSON-array `tenants`, space-delimited `tenants`, `tenant_id`, `tid`, `tenant_id` precedence over `tid`, missing tenant, global-admin missing tenant, blank tenant, wrong tenant, wrong-cased tenant, and non-`system` request tenant fail-closed behavior for global-admin-shaped principals.
- Safe failure behavior covered: 401/403 ProblemDetails, reason-code assertions, no command/query router dispatch on denied requests, and response-body redaction of tokens/signing material where applicable.
- UI workflows: N/A, no UI surface in this story.

## Validation

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~AuthenticationConfigurationTests|FullyQualifiedName~TenantClaimContractTests" -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` compiled successfully, then VSTest aborted before execution with sandbox socket denial: `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests` passed: 52 total, 0 errors, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests" -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` compiled successfully, then VSTest aborted before execution with sandbox socket denial: `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` passed: 165 total, 0 errors, 0 failed, 0 skipped.
- Post-review build gate: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- Post-review Server fallback: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests` passed: 53 total, 0 errors, 0 failed, 0 skipped.
- Post-review Integration fallback: `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` passed: 166 total, 0 errors, 0 failed, 0 skipped.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated if UI exists.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic HTTP assertions and accessible locators are not applicable.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.
