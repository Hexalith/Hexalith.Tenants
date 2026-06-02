---
baseline_commit: 10da5cf2d1244624fed8b0b3c479df1f17a68771
---

# Story 7.6A: Validate Production Auth Smoke Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want production authentication smoke tests,
so that valid and invalid identity provider configuration is proven before users depend on the service.

## Acceptance Criteria

1. Given production-like smoke tests run, when valid and invalid tokens are used against protected tenant command and query endpoints, then valid tokens succeed only within their allowed scope, and invalid or misconfigured tokens fail safely.
2. Given production auth smoke-test evidence is captured, when results are reviewed, then issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, and development-token separation are documented, and evidence does not expose token material, secrets, or PII.

## Tasks / Subtasks

- [x] Reconcile existing production auth smoke coverage before changing code. (AC: 1, 2)
  - [x] Read Story 7.3 and Story 11.3 completion notes and treat their completed validators, docs, and deterministic smoke tests as current baseline, not as work to rebuild.
  - [x] Confirm `src/Hexalith.Tenants/Program.cs` still registers `EventStoreAuthenticationOptions`, `ValidateEventStoreAuthenticationOptions`, `ValidateTenantProductionAuthenticationOptions`, `ConfigureJwtBearerOptions`, `EventStoreClaimsTransformation`, `TenantsSystemTenantValidator`, `ClaimsRbacValidator`, authentication, authorization, controllers, and the EventStore command controller application part.
  - [x] Confirm `docs/production-auth-readiness.md` and `docs/production-auth-claim-contract.md` still describe the final Story 7.3 fail-closed contract: protected Tenants requests must use request tenant `system` and an effective non-blank `eventstore:tenant=system`, including global-administrator-shaped principals.
  - [x] Identify any stale Story 11.3 wording that predates Story 7.3's stricter global-admin/system-tenant guard and update only the stale evidence wording.

- [x] Harden production-like query smoke evidence. (AC: 1)
  - [x] Use the existing `TenantsQueryJwtWebApplicationFactory` and production-like smoke JWT seam in `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`; do not replace it with controller-only tests or a fake authorization handler while claiming production auth readiness.
  - [x] Preserve coverage for valid direct `eventstore:tenant=system`, supported source-claim normalization (`tenants`, `tenant_id`, `tid`), missing token, malformed token, invalid signature, wrong issuer, wrong audience, expired token, missing tenant claim, global-admin missing tenant claim, blank tenant claim, wrong tenant, and wrong-cased tenant.
  - [x] Ensure denied query requests assert safe `401` or `403` behavior, `application/problem+json` where applicable, stable reason codes for authorization failures (`principal_not_member` or `tenant_mismatch`), and no query-router invocation.
  - [x] Add or adjust query smoke tests only where a current gap exists; avoid broad endpoint rewrites unrelated to auth smoke evidence.

- [x] Harden production-like command smoke evidence. (AC: 1)
  - [x] Use `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` with the real EventStore command controller route `POST /api/v1/commands` and a substituted `ICommandRouter`; this proves auth and gateway dispatch without requiring live DAPR/EventStore actors.
  - [x] Preserve valid command smoke coverage where a token with `eventstore:tenant=system` reaches routing and captures a `SubmitCommand` with request tenant `system`.
  - [x] Preserve or add negative command smoke coverage for missing tenant claim, global-admin missing tenant claim, blank direct tenant claim with source alias present, wrong tenant claim, wrong-cased tenant claim, non-`system` request tenant, and unrelated permission claims.
  - [x] Ensure denied command requests fail before `ICommandRouter.RouteCommandAsync` and do not leak bearer tokens, signing material, command payloads, generated JWTs, or secret values in response bodies or test output.
  - [x] Do not require live Keycloak, Entra ID, OIDC discovery, DAPR sidecars, Redis, Aspire orchestration, or EventStore aggregate actors for deterministic smoke tests.

- [x] Verify production startup/options validation and development-token separation. (AC: 1, 2)
  - [x] Preserve `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` coverage for production placeholder failure, valid OIDC-style overrides, environment-variable overrides, missing `Authority`/`Issuer`/`Audience`, whitespace values, non-HTTPS authority, production `SigningKey`, and `RequireHttpsMetadata=false`.
  - [x] Confirm validation messages name configuration keys only and never echo signing keys, bearer tokens, issuer hosts, audiences, decoded payloads, or other secret values.
  - [x] Confirm `docs/quickstart.md` remains local-development guidance and that production readiness docs clearly separate local HMAC/dev Keycloak tokens from production OIDC authority-based JWT validation.

- [x] Capture support-safe smoke-test evidence. (AC: 2)
  - [x] Update `docs/production-auth-readiness.md` only if the current evidence map, deterministic local smoke-test commands, manual deployment smoke checks, or failure triage are stale after the test audit.
  - [x] Record evidence as test command, test class/name or filter, pass/fail count, HTTP status, safe reason code, and date. Do not record compact JWTs, signing keys, decoded token payloads, real issuer URLs, real tenant/user data, full command payloads, or PII.
  - [x] If evidence is stored outside the story file, use an existing implementation-artifact location such as `_bmad-output/implementation-artifacts/tests/test-summary.md` or a narrowly named auth smoke evidence artifact; do not add ad hoc generated logs with secrets.
  - [x] Keep rate-limit partitioning explicitly out of Tenants-host evidence unless the host registers EventStore rate limiting; current docs should continue to identify that as an EventStore host boundary.

- [x] Run focused validation and record evidence accurately. (AC: 1, 2)
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~AuthenticationConfigurationTests|FullyQualifiedName~TenantClaimContractTests"`.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from product failures.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, startup wiring, project files, or shared docs change.
  - [x] Do not mark ACs complete from skipped, fake-auth-only, or manually inspected tests. Record exact blockers or residual deployment boundaries instead.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.6A is the auth-only slice of the former oversized deployment-readiness story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6A: Validate Production Auth Smoke Tests`]
- The 2026-05-31 sprint correction split Story 7.6 into independently testable auth, DAPR/service-invocation, health/readiness, pub/sub recovery, and checklist/evidence-template stories. Keep 7.6A scoped to production auth smoke tests and redacted auth evidence. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- PRD/epics map Epic 7 to deployment and production-readiness requirements: FR56 deploy Tenants alongside EventStore with standard DAPR configuration, FR57 stateless operation, NFR9 deployment-managed encryption, NFR22 health availability evidence, and NFR23 durable recovery evidence. For 7.6A, only the auth-specific deployment evidence is in scope. [Source: `_bmad-output/planning-artifacts/epics.md#Functional Requirements`; `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`]
- Architecture requires ASP.NET Core JWT Bearer with EventStore claims transformation/validation, production tokens normalized to `eventstore:tenant=system`, `sub` as user identity, and no command payloads, event payloads, tokens, secrets, or PII in logs. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]

### Current Repository State

- `src/Hexalith.Tenants/Program.cs` binds `Authentication:JwtBearer`, validates options on start, registers EventStore and Tenants production auth validators, configures JWT bearer auth, registers `EventStoreClaimsTransformation`, wires `TenantsSystemTenantValidator` as `ITenantValidator`, registers `ClaimsRbacValidator`, and imports the EventStore command controller assembly.
- `src/Hexalith.Tenants/Authorization/TenantsSystemTenantValidator.cs` is the Story 7.3 host guard. It denies blank request tenants, requires request tenant `system`, requires a non-blank effective `eventstore:tenant` claim, requires one such claim to equal `system` using ordinal comparison, then delegates to EventStore's shared `ClaimsTenantValidator`.
- `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs` enforces the production OIDC contract only when `IHostEnvironment.IsProduction()`: absolute HTTPS `Authority`, non-empty `Issuer`, non-empty `Audience`, empty/absent `SigningKey`, and `RequireHttpsMetadata=true`.
- `docs/production-auth-readiness.md` already contains required production settings, required token contents, IdP claim contract, deployment checklist, deterministic local smoke-test commands, manual deployment smoke checks, failure triage, evidence map, and current deployment boundaries.
- `docs/production-auth-claim-contract.md` already documents direct and source tenant claim shapes, source-claim precedence, global-administrator handling, exact `system` request tenant requirement, case-sensitive ID contract, and the EventStore rate-limit boundary.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` already includes a production-like smoke JWT issuer/key seam that exercises real JWT bearer middleware and EventStore claims transformation without live OIDC discovery.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` already exercises `POST /api/v1/commands` with a substituted `ICommandRouter`, proving auth/gateway behavior without requiring live DAPR/EventStore aggregate actors.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` and `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` already cover production startup/options validation and claim-contract edge cases.

### Previous Story Intelligence

- Story 7.3 corrected the fail-closed production tenant claim behavior after Story 11.2 allowed global-admin missing-tenant bypasses. 7.6A must preserve that stricter behavior in smoke evidence: global-admin-shaped tokens still need `eventstore:tenant=system` and request tenant `system` for protected Tenants command/query paths. [Source: `_bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md`]
- Story 11.3 already created production auth readiness docs and deterministic production-like smoke tests. 7.6A should audit, harden, and record the corrected Epic 7 evidence rather than duplicate the whole documentation story. [Source: `_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md`]
- Story 7.4 telemetry rules apply to smoke evidence: keep logs and test output support-safe; no payloads, tokens, secrets, or PII. [Source: `_bmad-output/implementation-artifacts/7-4-expose-tenant-command-and-event-metrics-with-opentelemetry.md`]
- Story 7.5 clarified evidence lanes. Deterministic auth smoke tests are normal implementation-lane evidence; live IdP/AppHost/DAPR production deployment checks are operator-run or later deployment evidence unless already prerequisite-gated. [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md`]

### Git Intelligence

- The latest commits before story creation are Epic 9 planning/documentation work (`docs(retro): add epic 9 retrospective`, `feat(story-9.7)`, `feat(story-9.6)`, `feat(story-9.5)`, `feat(story-9.4)`). They are not implementation precedent for 7.6A. Do not infer Phase 2 UI scope from those commits.
- Current working tree had unrelated story-automator changes and new orchestration artifacts before this story was created. Leave those files untouched during 7.6A implementation unless the user explicitly routes work through story-automator.

### Latest Technical Information

- Microsoft ASP.NET Core JWT bearer guidance for .NET 10 says APIs should validate signature, issuer, audience, and token expiration. Invalid token standards or invalid critical claims should return `401`; valid authentication without required permissions may return `403`. [Source: Microsoft Learn, "Configure JWT bearer authentication in ASP.NET Core", `https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0`]
- Microsoft ASP.NET Core configuration guidance for .NET 10 states later configuration providers override earlier providers and the environment variable provider maps double underscores (`__`) to colons (`:`). Keep operator examples in `Authentication__JwtBearer__...` form. [Source: Microsoft Learn, "Configuration in ASP.NET Core", `https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0`]
- Use repo-pinned versions: .NET SDK `10.0.300`, JWT Bearer `10.0.8`, DAPR SDK `1.17.9`, Aspire `13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. Do not upgrade packages or add a new auth library for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

### Technical Guardrails

- Exercise real ASP.NET Core JWT bearer middleware for production-like smoke paths. Do not claim production auth readiness from a fake auth handler alone.
- Keep deterministic smoke tests local. Do not call live Keycloak, Entra ID, OIDC discovery, DAPR, Redis, Docker, Aspire, or EventStore actors unless the test is explicitly prerequisite-gated and records skips accurately.
- Production token evidence must prove issuer, audience, expiration, subject, signing/authority source, and effective tenant claim behavior. Evidence must not store the token itself.
- Direct `eventstore:tenant=system` is preferred. Supported source claims (`tenants`, `tenant_id`, `tid`) may be normalized by EventStore claims transformation, but a direct blank `eventstore:tenant` must not be repaired by source aliases.
- Identifier comparisons are case-sensitive. `System` must not authorize where `system` is required.
- Command smoke tests prove the command gateway/auth boundary. Full command processing still requires EventStore/DAPR infrastructure and belongs to 7.6B/7.6D or operator deployment smoke evidence.
- Rate-limit partitioning is not Tenants-host evidence today because Tenants does not register EventStore rate limiting.

### Existing Files Likely to Touch

- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: extend only if current production-like query smoke coverage misses a 7.6A case.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: extend only if current command smoke coverage misses a 7.6A case.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs`: extend only if production options validation or safe-message coverage has a gap.
- `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs`: extend only if the Story 7.3 claim contract is not pinned by unit coverage.
- `docs/production-auth-readiness.md`: update stale evidence wording, commands, or redaction guidance only.
- `docs/production-auth-claim-contract.md`: update only if current code/tests prove the documented claim contract is stale.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` or a narrowly scoped evidence artifact: optional location for redacted smoke-test results.

### Preserve Existing Behavior

- Do not change production JWT policy, tenant-claim semantics, EventStore shared validators, DAPR ACLs, AppHost topology, command/query route shapes, query filtering, domain RBAC, or cursor behavior unless a direct inconsistency is found and recorded.
- Do not weaken the Story 7.3 fail-closed guard for global administrators.
- Do not use local HMAC development JWT settings as production-like evidence.
- Do not print generated compact JWTs, signing keys, decoded token payloads, raw command payloads, real authorities, real tenant/user IDs, or PII in test output or docs.
- Do not edit the `Hexalith.EventStore` submodule for this story.

### Out of Scope

- DAPR component and service invocation smoke tests; Story 7.6B owns them.
- Health and dependency readiness smoke tests; Story 7.6C owns them.
- Pub/sub outage, recovery, and catch-up evidence; Story 7.6D owns it.
- Full deployment readiness checklist and evidence-template publishing beyond auth-specific updates; Story 7.6E owns the final consolidated artifact.
- Vendor-specific production IdP provisioning, Keycloak/Entra deployment automation, Helm charts, Aspire publish profiles, or live production smoke execution.
- New dashboards, telemetry exporters, rate limiter registration, or EventStore authorization redesign.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*`.
- Keep tests deterministic and infrastructure-free unless clearly marked/gated.
- Prefer existing `WebApplicationFactory` seams and substituted routers over new fixture families.
- Every test must assert an observable status, reason code or dispatch behavior, and the absence of unsafe token/secret leakage where relevant.
- If VSTest cannot run due to sandbox socket restrictions, build first if needed and use the direct xUnit executable fallback pattern already recorded in Stories 7.3 through 7.5.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6A: Validate Production Auth Smoke Tests`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
- [Source: `_bmad-output/project-context.md#Authorization (RBAC -- Role-Based Access Control)`]
- [Source: `_bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md`]
- [Source: `_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md`]
- [Source: `docs/production-auth-readiness.md`]
- [Source: `docs/production-auth-claim-contract.md`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `src/Hexalith.Tenants/Authorization/TenantsSystemTenantValidator.cs`]
- [Source: `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs`]

## Project Structure Notes

- Alignment: Story 7.6A belongs in IntegrationTests auth smoke coverage, Server.Tests auth configuration/claim-contract coverage, and production auth documentation/evidence artifacts.
- Detected baseline: much of the necessary smoke coverage already exists from Stories 7.3 and 11.3. The implementation should audit and close gaps, not replace the auth stack.
- Detected boundary: deterministic command smoke evidence uses a substituted `ICommandRouter`; it proves auth/gateway dispatch, not full EventStore/DAPR command processing.
- Detected boundary: Tenants host does not register EventStore rate limiting; rate-limit partition evidence remains an EventStore host or deployment boundary.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02 - Loaded BMAD dev-story workflow, project context, sprint status, Story 7.3 completion notes, and Story 11.3 completion notes. Added baseline commit `10da5cf2d1244624fed8b0b3c479df1f17a68771` and moved the story to in-progress.
- 2026-06-02 - Reconciled `Program.cs`, `docs/production-auth-readiness.md`, `docs/production-auth-claim-contract.md`, `TenantsQueryControllerIntegrationTests`, `CommandApiRuntimeIntegrationTests`, `AuthenticationConfigurationTests`, and `TenantClaimContractTests` against the Story 7.6A task list. No auth product-code gap found.
- 2026-06-02 - Requested focused `dotnet test` commands aborted before execution with sandbox MSBuild/VSTest socket denial: `System.Net.Sockets.SocketException (13): Permission denied`. This was treated as an environment limitation and not a product failure.
- 2026-06-02 - Focused Integration direct xUnit fallback passed: `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests`: 166 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-02 - Focused Server direct xUnit fallback passed: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests -class Hexalith.Tenants.Server.Tests.Authorization.TenantClaimContractTests`: 53 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-02 - Full direct xUnit regression sweep initially found two documentation drift failures outside auth smoke tests: quickstart EventStore HMAC audience marker and Story 9.7 accessibility evidence mapper reference. Applied doc-only repairs and reran Server.Tests.
- 2026-06-02 - Full direct xUnit regression sweep passed after doc repairs: Contracts 105/0, Client 92/0, Testing 181/0, Sample 31/0, Server 730/0, Integration 218 total with 0 failed and 27 DAPR/performance prerequisite-gated skips.
- 2026-06-02 - Debug build gate passed: `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false`: 0 warnings, 0 errors.
- 2026-06-02 - Senior review found stale Story 11.3 AC numbering in `docs/production-auth-readiness.md` Evidence Map. Auto-fixed the table to map the actual Story 7.6A AC1/AC2 evidence. Focused Integration direct xUnit validation passed: 166 total, 0 errors, 0 failed, 0 skipped. Focused Server direct xUnit validation passed: 53 total, 0 errors, 0 failed, 0 skipped.

### Completion Notes List

- Audited existing production auth smoke coverage against Story 7.6A and preserved the established Story 7.3/11.3 implementation instead of rebuilding validators or smoke-test seams.
- Confirmed Tenants host auth wiring still registers the expected EventStore/Tenants validators, JWT bearer options, claims transformation, tenant/RBAC validators, authentication/authorization middleware, controllers, and EventStore command controller application part.
- Confirmed production auth readiness and claim-contract docs already describe the stricter fail-closed contract: protected Tenants command/query requests require request tenant `system` and effective `eventstore:tenant=system`, including global-administrator-shaped principals.
- Confirmed query and command smoke suites already cover direct tenant claims, supported source-claim normalization, invalid JWTs, missing/blank/wrong/wrong-cased tenant claims, global-admin missing tenant claims, non-`system` request tenants, safe reason codes, and no router dispatch on denied requests.
- Confirmed startup/options validation tests still cover production OIDC requirements and support-safe validation messages.
- Added redacted Story 7.6A evidence to `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Repaired two documentation drift failures found by the full Server.Tests gate: quickstart EventStore HMAC fallback audience evidence and the Story 9.7 accessibility evidence rejection-mapper source reference.
- Senior review auto-fixed stale readiness Evidence Map wording so production auth evidence now references the actual Story 7.6A acceptance criteria.

### File List

- _bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- docs/production-auth-readiness.md
- docs/quickstart.md
- docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex
Date: 2026-06-02
Outcome: Approved after auto-fix

#### Findings

- [MEDIUM][Fixed] `docs/production-auth-readiness.md` Evidence Map still used stale Story 11.3-style AC3/AC4/AC5 rows even though Story 7.6A has only AC1 and AC2. This made the completed stale-wording task only partially true and could mislead operators reviewing 7.6A evidence. Updated the table to map directly to Story 7.6A AC1 and AC2.

#### Verification

- Re-read `Program.cs`, `TenantsSystemTenantValidator`, `ValidateTenantProductionAuthenticationOptions`, production auth docs, query smoke tests, command smoke tests, authentication configuration tests, and tenant-claim contract tests against the story acceptance criteria and completed tasks.
- Confirmed query smoke coverage uses real JWT bearer middleware and covers valid direct/source tenant claims, invalid JWT cases, missing/blank/wrong/wrong-cased tenant claims, safe reason codes, `application/problem+json` where applicable, redaction checks, and no query-router invocation on denied requests.
- Confirmed command smoke coverage exercises `POST /api/v1/commands`, validates direct/source tenant claims, missing/global-admin-missing/blank/wrong/wrong-cased/non-`system` tenant failures, unrelated permission denial, safe reason codes, and no command-router invocation before authorization succeeds.
- Confirmed production startup/options validation covers OIDC authority, issuer, audience, HTTPS metadata, signing-key separation, environment overrides, placeholder failure, whitespace/non-HTTPS failures, and support-safe validation messages.
- Cross-checked official Microsoft Learn ASP.NET Core JWT bearer and configuration guidance during review. The current docs still align with the story contract: APIs validate JWT signature, issuer, audience, and expiration; invalid token claims return 401 while valid authentication without permission returns 403; environment variables override JSON configuration and use double underscores for hierarchical keys.

## Change Log

| Date       | Version | Description                                                                 | Author |
|------------|---------|-----------------------------------------------------------------------------|--------|
| 2026-06-02 | 0.1     | Created Story 7.6A implementation context for production auth smoke tests. | GPT-5 Codex |
| 2026-06-02 | 1.0     | Audited production auth smoke coverage, captured support-safe evidence, repaired doc drift found by regression tests, and moved story to review. | GPT-5 Codex |
| 2026-06-02 | 1.1     | Senior review auto-fixed stale production auth Evidence Map AC numbering and marked story done. | GPT-5 Codex |
