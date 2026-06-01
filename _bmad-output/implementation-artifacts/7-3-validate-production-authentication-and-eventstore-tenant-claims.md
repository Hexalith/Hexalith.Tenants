---
baseline_commit: 7f33bb4dd43b6eeab949b5398c0198c9db576a4b
---

# Story 7.3: Validate Production Authentication and EventStore Tenant Claims

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want production authentication configuration validated before deployment,
so that tenant operations are authorized consistently and unsafe defaults do not reach production.

## Acceptance Criteria

1. Given production configuration omits required JWT authority, audience, signing, or metadata settings, when the Tenants service starts in production mode, then startup validation fails with a clear configuration error, and logs identify missing keys without exposing secrets or token material.
2. Given valid production JWT settings are supplied through environment variables, AppHost, or deployment configuration, when the service starts, then authentication options validate successfully, and no committed appsettings file needs to contain production secrets.
3. Given a production identity provider issues a token for tenant-management operations, when the token reaches EventStore tenant validation, then it contains or is normalized to `eventstore:tenant=system`, and tenant validation does not fall into a shared anonymous partition silently.
4. Given a token is missing the `eventstore:tenant` claim, when a protected tenant endpoint is called in production mode, then the request is rejected with a safe authentication or authorization failure, and no command, query, projection, or rate-limit partition uses an anonymous or fallback tenant.
5. Given a token has an invalid `eventstore:tenant` claim, when a protected tenant endpoint is called in production mode, then the request is rejected fail-closed, and logs identify the claim contract failure without exposing token material.
6. Given auth tests run, when production-valid, production-invalid, development-valid, missing-claim, and wrong-claim tokens are exercised, then tests verify startup validation, authorization behavior, and safe failure responses.
7. Given the identifier-casing contract is documented in `docs/production-auth-claim-contract.md` (TEN-3 correction), when `sub`/userId and managed `tenantId` values are compared for membership, projection, or claim matching, then comparison is case-sensitive (`StringComparer.Ordinal`) and a casing mismatch fails closed by design, and canonical casing is the identity provider's and operator's responsibility, so consuming services rely on the published contract instead of case-folding claims.

## Tasks / Subtasks

- [x] Reconcile the existing production-auth artifacts before changing code. (AC: 1-7)
  - [x] Read Stories 11.1, 11.2, and 11.3 and map their completed implementation to this story's ACs instead of rebuilding already completed validator, claim-contract, and readiness work.
  - [x] Confirm `src/Hexalith.Tenants/Program.cs` still registers `EventStoreAuthenticationOptions`, `ValidateEventStoreAuthenticationOptions`, `ValidateTenantProductionAuthenticationOptions`, `ConfigureJwtBearerOptions`, `EventStoreClaimsTransformation`, `ClaimsTenantValidator`, `ClaimsRbacValidator`, and `AuthorizationBehavior<,>`.
  - [x] Confirm committed production placeholders in `src/Hexalith.Tenants/appsettings.json` still fail in `Production` without deployment overrides and that `src/Hexalith.Tenants/appsettings.Development.json` remains development-only.
  - [x] Record any stale Story 11.x guidance in the Dev Agent Record; do not silently preserve behavior that contradicts this Epic 7 story.

- [x] Resolve the Epic 7 fail-closed claim-contract conflict. (AC: 3, 4, 5, 6)
  - [x] Current conflict to address: `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` has `GlobalAdministratorMissingTenantClaimIsAuthorizedForSystemTenant`, and `docs/production-auth-claim-contract.md` documents that global administrators can be authorized without `eventstore:tenant`; this conflicts with AC4 for protected tenant endpoints in production mode.
  - [x] Implement the narrowest Tenants-side production guard that requires the effective principal for protected Tenants command/query requests to include `eventstore:tenant=system`, including global-administrator principals, before command/query/projection work can run.
  - [x] Preserve EventStore's shared `ClaimsTenantValidator` behavior unless a separate EventStore change is explicitly required; prefer Tenants host middleware, authorization policy, pipeline behavior, or validator composition that enforces the Tenants deployment contract without forking the submodule.
  - [x] Ensure missing, blank, whitespace-only, wrong-cased, or wrong-tenant claims fail closed and return safe 401/403 behavior without invoking command routing, query routing, projection dispatch, or anonymous/fallback partition assumptions.
  - [x] If implementation proves the global-admin bypass is an intentional product exception, stop and record the contradiction in this story; do not mark AC4 satisfied by a bypassing token.

- [x] Keep production JWT startup validation locked and deployment-friendly. (AC: 1, 2, 6)
  - [x] Preserve the Story 11.1 production contract: `IHostEnvironment.IsProduction()` requires absolute HTTPS `Authority`, non-whitespace `Issuer`, non-whitespace `Audience`, empty or absent `SigningKey`, and `RequireHttpsMetadata=true`.
  - [x] Keep validation failures specific to keys such as `Authentication:JwtBearer:Authority`, `Issuer`, `Audience`, `SigningKey`, and `RequireHttpsMetadata`, but never echo signing keys, bearer tokens, decoded payloads, issuer hosts, or private authorities.
  - [x] Confirm environment-variable overrides such as `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, and `Authentication__JwtBearer__Audience` still satisfy production validation.
  - [x] Do not add real production IdP endpoints, client secrets, signing keys, Keycloak exports, Entra registrations, or deployment manifests as part of this story.

- [x] Update claim-contract documentation to match the final Tenants production behavior. (AC: 3, 4, 5, 7)
  - [x] Update `docs/production-auth-claim-contract.md` if the global-administrator missing-tenant section remains inconsistent with Epic 7 fail-closed behavior.
  - [x] Keep direct `eventstore:tenant=system` as the preferred production contract and preserve supported source-claim normalization (`tenants`, `tenant_id`, `tid`) only where tests prove EventStore still normalizes those claims.
  - [x] Preserve the TEN-3 identifier-casing contract: `sub`/userId and managed `tenantId` comparisons are case-sensitive with `StringComparer.Ordinal`; casing mismatches fail closed by design.
  - [x] Update `docs/production-auth-readiness.md` only where checklist wording, failure triage, or evidence guidance needs to reflect the final fail-closed behavior.
  - [x] Keep all examples redacted or placeholder-only; do not instruct operators to log full tokens, secrets, or real tenant/user identifiers.

- [x] Add or adjust deterministic auth tests. (AC: 1-7)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` only if startup-validation gaps are found; existing Story 11.1/11.3 tests already cover most production/development cases.
  - [x] Replace or rewrite the current global-admin missing-tenant authorization expectation so a production protected Tenants path requires `eventstore:tenant=system` even for global-admin-shaped principals.
  - [x] Add/adjust integration tests in `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` and/or `CommandApiRuntimeIntegrationTests.cs` to exercise real JWT bearer middleware plus claims transformation for valid, missing, blank, wrong, wrong-cased, and alias-normalized tenant claims.
  - [x] Ensure denied requests assert safe response bodies and reason codes where available; avoid brittle full-message assertions and avoid logging token values in test output.
  - [x] Keep tests deterministic: no real IdP, OIDC network discovery, DAPR sidecars, Redis, Docker, Aspire orchestration, or process-wide env-var mutation unless existing fixtures isolate it.

- [x] Run focused validation and record evidence accurately. (AC: 1-7)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantClaimContractTests`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`.
  - [x] If command gateway authorization behavior changes, also run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CommandApiRuntimeIntegrationTests`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, startup wiring, project files, or shared docs links change.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from application failures.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.3 owns production authentication validation and the effective EventStore tenant-claim contract for Tenants deployment. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 7: Operators Can Deploy, Secure, and Observe Production Tenants`]
- Story 7.3 requires production startup validation for JWT settings, deployment-supplied secrets through configuration providers, `eventstore:tenant=system` for tenant-management tokens, fail-closed behavior for missing/invalid tenant claims, safe diagnostics, and case-sensitive identifier contracts. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.3: Validate Production Authentication and EventStore Tenant Claims`]
- PRD FR56/FR57 place this in production deployment readiness; NFR5/NFR10 require cross-tenant isolation and authorization coverage; NFR9 keeps encryption and IdP/deployment details as infrastructure concerns rather than custom cryptography in Tenants. [Source: `_bmad-output/planning-artifacts/prd.md#Observability & Operations`; `_bmad-output/planning-artifacts/prd.md#Security`; `_bmad-output/planning-artifacts/prd.md#Integration`]
- Architecture maps Epic 7 work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `src/Hexalith.Tenants/Program.cs` currently binds `Authentication:JwtBearer`, calls `ValidateOnStart()`, registers shared EventStore validation, registers `ValidateTenantProductionAuthenticationOptions`, configures JWT bearer authentication, registers `EventStoreClaimsTransformation`, and wires authorization before mapping controllers and internal `/process` and `/project` routes.
- `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs` currently enforces the Story 11.1 production OIDC contract: absolute HTTPS `Authority`, non-empty `Issuer` and `Audience`, empty production `SigningKey`, and `RequireHttpsMetadata=true`.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` already covers production placeholders, OIDC overrides, environment-variable overrides, development settings, whitespace values, malformed/non-HTTPS authority, signing key rejection, disabled HTTPS metadata, and composed validators.
- `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` already covers source-claim normalization, direct `eventstore:tenant`, blank/whitespace fail-closed cases, trusted subject shape, and non-global-admin missing-tenant denial.
- Current conflict: the same test file also asserts global-admin missing-tenant principals are authorized for `system`, and `docs/production-auth-claim-contract.md` documents that bypass. That is not compatible with Story 7.3 AC4 unless the product explicitly changes the AC.
- `docs/production-auth-readiness.md` already documents production settings, environment-variable names, token contents, readiness checklist, deterministic smoke tests, manual smoke commands, and redaction rules. Treat it as the baseline and update only for this story's final claim behavior.
- Story 11.3 recorded that the Tenants host does not register the EventStore rate limiter today. Do not claim Tenants proves rate-limit partitioning unless implementation actually adds or verifies that host boundary.

### Previous Story Intelligence

- Story 7.1 created the Aspire hosting extension and default DAPR resource model. Story 7.3 should not alter Aspire topology unless auth deployment overrides need a narrow AppHost configuration pass-through.
- Story 7.2 added production DAPR templates and deny-by-default service invocation. Story 7.3 should not change DAPR ACLs except to preserve the fact that `/process` and `/project` are internal EventStore service-invocation routes, not public unauthenticated command/query endpoints.
- Story 11.1 completed production JWT startup validation; reuse it and add only missing Epic 7 coverage.
- Story 11.2 completed EventStore tenant-claim documentation/tests but introduced the global-admin missing-tenant bypass conflict now called out in this story.
- Story 11.3 completed production auth readiness docs and deterministic auth smoke tests; reuse those tests and docs where they still match Epic 7.

### Technical Guardrails

- Use repo-pinned versions: .NET SDK `10.0.300`, ASP.NET Core JWT Bearer `10.0.8`, DAPR SDK `1.17.9`, Aspire `13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`. Do not upgrade packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Preserve `MapInboundClaims=false`; tests and docs must use exact JWT claim names such as `sub`, `eventstore:tenant`, `tenants`, `tenant_id`, and `tid`. [Source: `_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md#EventStore Claim and Authorization Contract`]
- Production-safe JWT mode remains OIDC authority-based validation. Symmetric `SigningKey` mode is development/test only unless a future architecture decision creates an explicit production exception. [Source: `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md#Implementation Guardrails`]
- The platform tenant context for tenant management is `system`; managed tenant IDs remain separate command/query payload or route values. Do not confuse `eventstore:tenant=system` with the managed tenant aggregate ID.
- Keep Tenants domain RBAC semantics separate from EventStore tenant-claim validation. This story validates the effective principal and gateway authorization contract; it must not weaken aggregate-level TenantOwner/TenantContributor/TenantReader rules.
- Never log or commit command payloads, event payloads, tokens, signing keys, secrets, PII, or sensitive tenant/user data. Safe failures should name keys or claim categories, not values.
- Do not modify the `Hexalith.EventStore` submodule for this story unless a separate cross-repo decision is explicitly made. Read it for contracts only.

### Existing Files to Touch

- `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs`: update the global-admin missing-tenant expectation and related claim-contract unit coverage.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: likely add or adjust protected query endpoint real-JWT cases for missing/invalid/wrong-cased tenant claims.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: update only if command gateway authorization behavior must prove the same fail-closed rule before routing.
- `src/Hexalith.Tenants/Program.cs`: update only if the Tenants host needs a narrow production tenant-claim guard registration or authorization policy.
- `src/Hexalith.Tenants/Configuration/`: possible location for a Tenants-specific production tenant-claim guard if startup validation alone is insufficient.
- `docs/production-auth-claim-contract.md`: update the global-administrator section and effective-principal wording to match the final behavior.
- `docs/production-auth-readiness.md`: update only if checklist/failure triage evidence becomes stale after the fail-closed change.

### Preserve Existing Behavior

- Keep local development JWTs and the AppHost Keycloak sample usable.
- Keep direct `eventstore:tenant=system` as the preferred production shape while preserving source-claim normalization for IdPs that cannot emit direct `eventstore:*` claims.
- Keep `AuthenticationConfigurationTests` deterministic and isolated from live infrastructure.
- Keep command/query denial responses safe and compatible with existing ProblemDetails/reason-code expectations.
- Keep Tenants host as a domain service; do not register the full EventStore server extension or rate limiter merely to satisfy this story unless an approved host-boundary decision exists.

### Out of Scope

- Vendor-specific IdP provisioning for Keycloak, Entra ID, or other providers.
- Live production smoke tests against a real OIDC authority.
- DAPR component/service-invocation changes owned by Story 7.2 or Story 7.6B.
- OpenTelemetry metrics owned by Story 7.4.
- Health/stateless reconstruction evidence owned by Story 7.5.
- Full deployment readiness checklist/evidence template owned by Story 7.6E, except for keeping existing auth readiness docs internally consistent.

### Testing Standards

- Use xUnit v3 and Shouldly; do not add `Assert.*`.
- Prefer real ASP.NET Core JWT bearer middleware for integration smoke behavior; use source-claim/unit tests only for transformation edge cases.
- Keep tests free of live IdP/OIDC discovery, DAPR, Docker, Redis, Aspire, and real secrets.
- Use stable reason-code/status assertions rather than brittle full exception or response text.
- If any test emits generated JWTs, signing material, or token payloads to output, treat that as a test defect and remove it.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.3: Validate Production Authentication and EventStore Tenant Claims`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC — Role-Based Access Control)`]
- [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`]
- [Source: `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md`]
- [Source: `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md`]
- [Source: `_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md`]
- [Source: `_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs`]
- [Source: `docs/production-auth-claim-contract.md`]
- [Source: `docs/production-auth-readiness.md`]

## Project Structure Notes

- Alignment: Story 7.3 belongs in Tenants host auth wiring, configuration validation, claim-contract tests, integration auth smoke tests, and production auth docs.
- Detected conflict: completed Story 11.2 artifacts allow global-admin missing-tenant authorization, but current Epic 7 Story 7.3 AC4 requires missing `eventstore:tenant` to fail closed for protected tenant endpoints in production mode.
- Detected boundary: Tenants does not register the EventStore rate limiter today. Do not claim rate-limit partition proof inside Tenants unless that boundary changes with explicit tests.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01 - Reconciled Stories 11.1, 11.2, and 11.3 against Story 7.3. Story 11.1/11.3 production JWT startup validation and readiness tests were kept; Story 11.2 global-admin missing-tenant guidance was stale for Epic 7 AC4 and was corrected.
- 2026-06-01 - `dotnet test` focused commands for Server and Integration tests could not start VSTest in this sandbox because MSBuild/VSTest socket creation failed with `System.Net.Sockets.SocketException (13): Permission denied`. This was treated as an environment limitation, not an application failure.
- 2026-06-01 - Build check found and fixed the new validator's `TenantIdentity` namespace before green validation.
- 2026-06-01 - Focused Server fallback: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests` passed: 52 total, 0 failed.
- 2026-06-01 - Focused Integration fallback: `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` passed: 161 total, 0 failed.
- 2026-06-01 - Build gate: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` passed with 0 warnings and 0 errors.
- 2026-06-01 - Full direct xUnit regression sweep passed for Contracts, Client, Sample, Testing, Server, and Integration assemblies. Integration full sweep: 206 total, 0 failed, 25 skipped for documented DAPR/performance prerequisites.
- 2026-06-01 - Senior review identified and fixed a global-administrator bypass edge case where a token with `eventstore:tenant=system` could still use a non-`system` request tenant through EventStore's shared global-admin validator bypass.
- 2026-06-01 - Post-review focused Server fallback passed: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests`: 53 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-01 - Post-review focused Integration fallback passed: `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests`: 166 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-01 - Post-review build gate passed: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false`: 0 warnings, 0 errors.

### Completion Notes List

- Added a Tenants host `ITenantValidator` composition guard that requires the effective transformed principal to contain non-blank `eventstore:tenant=system` before delegating to EventStore's shared `ClaimsTenantValidator`.
- Review fix: the Tenants host guard now also requires the request tenant itself to be `system`, preventing global-administrator-shaped principals from reaching command routing with a non-`system` request tenant.
- Preserved the shared EventStore global-administrator bypass generically while enforcing the Tenants deployment contract for protected command/query paths, including global-administrator-shaped principals.
- Covered missing, blank, wrong, wrong-cased, direct, and alias-normalized tenant-claim behavior with deterministic unit and real JWT integration tests. Denials assert safe status/reason codes and no router dispatch.
- Updated production auth claim-contract and readiness docs so operator guidance now matches the fail-closed Tenants behavior and preserves the case-sensitive identifier contract.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- [HIGH] Global-administrator principals with `eventstore:tenant=system` could still submit a command using a non-`system` request tenant because `TenantsSystemTenantValidator` delegated to EventStore's shared `ClaimsTenantValidator`, whose global-admin bypass ignores tenant matching. Fixed by requiring `tenantId == system` before delegation and adding unit/integration coverage.
- [MEDIUM] Review evidence and file tracking needed refresh after the guard fix. Updated documentation, validation counts, and story file list.

Checklist:

- [x] Story status was reviewable before review.
- [x] Acceptance Criteria 1-7 cross-checked against implementation.
- [x] Completed tasks audited against source, tests, and docs.
- [x] File List validated and refreshed.
- [x] Focused direct xUnit validation passed after rebuild.
- [x] Debug build gate passed after rebuild.
- [x] Remaining critical issues: 0.

### File List

- _bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- docs/production-auth-claim-contract.md
- docs/production-auth-readiness.md
- src/Hexalith.Tenants/Authorization/TenantsSystemTenantValidator.cs
- src/Hexalith.Tenants/Program.cs
- tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
- tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs

### Change Log

- 2026-06-01 - Implemented Tenants fail-closed system-tenant claim guard, updated deterministic auth coverage and production docs, completed direct xUnit/build validation, and moved story status to review.
- 2026-06-01 - Senior review fix required request tenant `system` before EventStore global-admin delegation, added regression coverage, refreshed docs/evidence, and moved story status to done.
