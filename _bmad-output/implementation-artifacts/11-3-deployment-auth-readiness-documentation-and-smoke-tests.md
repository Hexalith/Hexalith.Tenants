# Story 11.3: Deployment Auth Readiness Documentation and Smoke Tests

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. Code review 2026-05-20 applied 18 patches (1 originally decision-needed, converted to patch P18 after verifying `ClaimsRbacValidator.cs:22,63-68` accepts `command:query` as `LegacyQueryPermission`) and recorded 3 deferred items. Full Debug/no-restore solution test gate: 723 passed, 1 skipped (+22 net new test cases from theory-row expansion, 0 regressions).

## Story

As a platform operator,
I want deployment documentation and smoke tests that prove production authentication is wired correctly,
so that auth misconfiguration is caught before users hit runtime failures.

## Acceptance Criteria

1. Given deployment documentation is updated, when an operator prepares Hexalith.Tenants for production, then the docs list required JWT settings, required IdP claims, environment variable names, and AppHost/deployment override expectations.
2. Given an operator follows the readiness checklist, when they verify a deployment, then the checklist includes token issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, and rate-limit partitioning checks.
3. Given production-like smoke tests run, when valid and invalid tokens are used against protected tenant endpoints, then valid tokens succeed only within their allowed scope and invalid or misconfigured tokens fail safely.
4. Given the service is deployed with missing or invalid auth overrides, when the smoke test or startup validation runs, then the failure points to the missing deployment input rather than producing ambiguous runtime errors.
5. Given local development docs remain available, when developers use dev-mode JWTs, then the docs clearly separate development token generation from production IdP configuration.

## Prerequisite Contracts

- Story 11.1 owns production JWT configuration validation. This story consumes that contract: production readiness requires OIDC-style `Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata=true`, no production `SigningKey`, safe validation errors, and no secrets in committed `appsettings.json`.
- Story 11.2 owns the EventStore tenant claim contract. This story consumes that contract: production tokens must result in downstream `eventstore:tenant=system` for tenant-management operations, and documentation must distinguish direct `eventstore:tenant` claims from source claims such as `tenants`, `tenant_id`, or `tid` when EventStore claims transformation is in play.
- Story 11.3 must not reopen the validator or claim-contract policy unless implementation proves the earlier story artifacts are stale. If stale, record the blocker in this story's Dev Agent Record rather than silently changing architecture scope.
- Story 11.3 verifies and documents inherited contracts; it does not create new production JWT policy, tenant-claim semantics, live IdP provisioning, or production Keycloak/AppHost architecture.

## Tasks / Subtasks

- [x] Inventory existing auth and deployment documentation before writing new docs. (AC: 1, 2, 5)
  - [x] Read `docs/quickstart.md`, `README.md`, `src/Hexalith.Tenants/appsettings.json`, `src/Hexalith.Tenants/appsettings.Development.json`, and the AppHost Keycloak realm sample.
  - [x] Read Story 11.1 and Story 11.2 before editing docs so production JWT validation and tenant claim wording stay aligned.
  - [x] Read EventStore security and configuration docs from the current submodule commit before copying claim, authority, or rate-limit terminology.
  - [x] Keep the existing local quickstart usable; add production readiness guidance as a separate section or page instead of turning the quickstart into a production deployment guide.
- [x] Add production auth readiness documentation. (AC: 1, 2, 4, 5)
  - [x] Document required production configuration keys using .NET hierarchical names and environment-variable forms: `Authentication:JwtBearer:Authority` / `Authentication__JwtBearer__Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata`, and the intentionally absent or empty production `SigningKey`.
  - [x] State that production must use an absolute HTTPS OIDC authority with `RequireHttpsMetadata=true`; symmetric signing-key auth is development/test only unless a later architecture decision creates an explicit production exception.
  - [x] Document that ASP.NET Core environment variables override appsettings through the default configuration providers, and that double underscores are the portable separator for hierarchical keys.
  - [x] Document required IdP token contents: issuer, audience, subject, expiration, tenant claim contract from Story 11.2, and EventStore permission/domain claims when command API smoke tests use EventStore gateway endpoints.
  - [x] Include safe operator examples with placeholders only. Do not commit real authorities, client IDs, signing keys, bearer tokens, secrets, tenant data, or production user identifiers.
  - [x] Use fake tokens, redacted decoded header/payload examples, or placeholder values only; never instruct operators to paste or log full bearer tokens, signing keys, client secrets, refresh tokens, or real issuer metadata.
  - [x] Keep development JWT generation separate and clearly labelled as local-only HMAC signing with `appsettings.Development.json`.
  - [x] Include an evidence map that links each acceptance criterion to the documentation section, test name, expected failure mode, or documented residual risk that proves it.
  - [x] For every checklist command, curl sample, decoded-token example, or test transcript, state the expected observable evidence and the redaction rule that keeps tokens, secrets, authorities, tenant data, and user identifiers out of committed docs and logs.
- [x] Add a deployment readiness checklist operators can run before release. (AC: 1, 2, 4)
  - [x] Include startup validation checks for missing placeholders, whitespace values, non-HTTPS authorities, ambiguous Authority plus SigningKey, and `RequireHttpsMetadata=false` in production.
  - [x] Include token inspection checks for `iss`, `aud`, `sub`, `exp`, and the source or normalized tenant claim that yields `eventstore:tenant=system`.
  - [x] Include request-level checks for one protected tenant query endpoint and one tenant-management command endpoint when the local test topology can support both. State which endpoint proves authentication, which proves authorization, and which layer returns the failure.
  - [x] Separate checks that can be proven by deterministic local smoke tests from checks that remain operator-run deployment verification, and ensure each manual step has pass/fail wording rather than relying on prose inspection alone.
  - [x] Include rate-limit partition verification only if the Tenants host actually registers the relevant EventStore rate limiter. If enabled, confirm partitioning uses normalized subject/client identity and does not log token contents. If it does not, document the finding and defer executable rate-limit evidence to the EventStore host boundary or later deployment automation.
  - [x] Include failure triage guidance that names missing configuration keys or claim types without instructing operators to log full tokens or secrets.
- [x] Add production-like smoke test coverage without depending on a real external IdP. (AC: 3, 4)
  - [x] Exercise the real Tenants ASP.NET Core authentication/authorization middleware through `WebApplicationFactory` or the existing integration host; do not instantiate controllers, handlers, or fake authorization paths while claiming production auth readiness.
  - [x] Use a production-like local token issuer seam with deterministic issuer, audience, claims, signing material, and clock/skew behavior. Do not call a real IdP or OIDC discovery endpoint from the default smoke tests, and do not reuse the development HMAC key from `appsettings.Development.json` as production-like evidence.
  - [x] Cover a valid token with matching issuer, audience, subject, and `eventstore:tenant=system` that can reach an authorized protected endpoint.
  - [x] Cover missing token, malformed token, invalid signature, wrong issuer, wrong audience, and expired token as `401` authentication failures.
  - [x] Cover valid authentication with missing, blank, or wrong `eventstore:tenant` as `403` authorization failures when the target endpoint enforces tenant-management authorization.
  - [x] Cover missing or invalid production auth overrides through startup/options validation tests that force `IHostEnvironment.IsProduction()` for missing `Authority`, missing/blank `Issuer`, missing `Audience`, non-HTTPS `Authority`, `RequireHttpsMetadata=false`, and any production `SigningKey`.
  - [x] Assert failed authentication, authorization, and startup validation paths name safe configuration or claim categories only and do not echo bearer tokens, signing material, decoded payloads, or secret values in response bodies, logs captured by tests, or validation messages.
  - [x] Do not require Keycloak, Entra ID, OIDC network discovery, real deployment manifests, DAPR sidecars, Redis, or Aspire orchestration for the narrow smoke tests unless an existing fixture already handles those prerequisites robustly.
  - [x] If an Aspire/AppHost smoke path is added, keep it as an optional or prerequisite-gated test and preserve existing skip behavior when Docker, DAPR, Redis, or placement prerequisites are unavailable.
- [x] Align sample AppHost and local docs with production wording. (AC: 1, 2, 5)
  - [x] Treat `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` as local sample evidence only; do not present it as a production realm export.
  - [x] If the sample realm already emits direct `eventstore:tenant`, document how that relates to Story 11.2's supported source-claim normalization.
  - [x] Verify quickstart token examples remain development-only and still explain why `tenants: ["system"]` works locally.
  - [x] Add links from `README.md` or `docs/quickstart.md` only where they help operators find production readiness guidance without cluttering first-run development flow.
- [x] Record unresolved deployment boundary decisions. (AC: 2, 3, 4)
  - [x] If smoke tests cannot prove a command endpoint without live DAPR/EventStore infrastructure, document the exact blocker and keep the test focused on protected query/auth behavior.
  - [x] If Tenants does not register rate limiting in the current host, document that rate-limit partitioning is not directly smoke-testable here and reference the EventStore boundary that owns it.
  - [x] If a future deployment manifest, Helm chart, Aspire publish profile, or cloud-specific Keycloak/Entra setup is needed, record it as deferred deployment automation rather than adding it ad hoc.

## Dev Notes

### Current Code and Documentation State

- `src/Hexalith.Tenants/Program.cs` registers JWT bearer authentication with `EventStoreAuthenticationOptions`, `ValidateOnStart`, `ValidateEventStoreAuthenticationOptions`, `ConfigureJwtBearerOptions`, default JWT bearer authentication, authorization, controllers, and EventStore command-controller application parts. Story 11.3 should smoke this existing path rather than creating a parallel auth path. [Source: `src/Hexalith.Tenants/Program.cs`]
- Committed production `src/Hexalith.Tenants/appsettings.json` keeps `Authentication:JwtBearer:Authority` and `SigningKey` empty, uses `Issuer` = `hexalith`, `Audience` = `hexalith-tenants`, and `RequireHttpsMetadata=true`. Story 11.1 expects production startup to fail until deployment overrides supply a real OIDC authority and required values. [Source: `src/Hexalith.Tenants/appsettings.json`; `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md`]
- Committed development `src/Hexalith.Tenants/appsettings.Development.json` uses issuer `hexalith-dev`, audience `hexalith-tenants`, a test-only symmetric signing key, and `RequireHttpsMetadata=false`. This remains local-only and must not be described as production-ready. [Source: `src/Hexalith.Tenants/appsettings.Development.json`]
- `docs/quickstart.md` is currently local-development focused. It explains DAPR, Docker, Aspire, the `system` tenant, local HMAC token generation, and the `tenants: ["system"]` source claim. Preserve that first-run path and add production auth readiness separately. [Source: `docs/quickstart.md`]
- The AppHost Keycloak realm sample maps sample user attributes to `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission`. It is useful local evidence for claim shape but must not be treated as a production realm export or source of secrets. [Source: `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`]

### Story 11.1 and 11.2 Dependencies

- Story 11.1 tightened the production JWT contract around `IHostEnvironment.IsProduction()`, absolute HTTPS OIDC authority, non-whitespace issuer/audience, absent production signing key, `RequireHttpsMetadata=true`, safe validation messages, and composed EventStore plus Tenants options validation. This story's docs and smoke tests should verify those expectations instead of restating them loosely. [Source: `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md`]
- Story 11.2 clarified that the downstream production claim consumed by EventStore tenant authorization is `eventstore:tenant`; source claims such as `tenants`, `tenant_id`, or `tid` may be normalized by EventStore claims transformation; tenant-management operations require the `system` platform tenant context. This story should turn that into operator verification steps. [Source: `_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md`]
- Story 11.2 party-mode review deliberately deferred end-to-end deployment smoke verification across JWT middleware, claims transformation, authorization, and rate limiting to Story 11.3. This is the place to add that smoke path, but only at the level the repo can run without real production IdP dependencies. [Source: `_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md#Party-Mode Review`]

### Architecture and Scope Boundaries

- Epic 11 is Production Authorization Readiness. Story 11.3 is documentation and smoke verification, not new auth architecture. Do not change production JWT policy, tenant claim semantics, role behavior, query visibility, cursor behavior, DAPR access control, or EventStore shared infrastructure unless implementation uncovers a direct contradiction that must be recorded as a blocker. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 11: Production Authorization Readiness`]
- The architecture defines two authorization layers: EventStore JWT/gateway authorization for API access and Tenants domain RBAC inside aggregate `Handle` methods. Smoke tests should identify which layer rejects a request and should not blur 401 authentication failures with 403 authorization failures. [Source: `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]
- The platform tenant context is `system`, the domain is `tenants`, and tenant-management JWTs require an `eventstore:tenant` authorization path for `system`. The managed tenant ID in a command payload remains separate from the platform tenant context. [Source: `_bmad-output/planning-artifacts/architecture.md#Identity Mapping (ADR)`; `docs/quickstart.md#About the-system-Tenant`]
- Follow repository constraints from `AGENTS.md`: do not initialize or update nested submodules recursively; use Conventional Commits; avoid recursive submodule commands.

### Documentation Required Structure

- `docs/production-auth-readiness.md` should contain: production OIDC/JWT required settings, environment variable and AppHost override names, IdP claim contract including `eventstore:tenant=system`, local dev JWT versus production IdP separation, safe token inspection and redaction rules, an operator readiness checklist, and a smoke-test evidence matrix.
- The checklist should use concrete pass/fail wording for HTTPS authority, exact issuer, exact audience, `RequireHttpsMetadata=true`, token expiration, subject presence, tenant-management `eventstore:tenant=system`, no production signing key, invalid token rejection, and missing-config startup/readiness failure.
- Local Keycloak realm and AppHost examples may be referenced as sample/test evidence only. They must not be presented as production IdP exports, production deployment recommendations, or sources of real secrets.

### Smoke-Test Evidence Matrix

| Condition | Expected layer | Expected result |
| --- | --- | --- |
| Valid production-like token with issuer, audience, subject, expiration, and `eventstore:tenant=system` | Authentication and authorization | Success at the selected protected endpoint |
| Missing token, malformed token, invalid signature, wrong issuer, wrong audience, or expired token | Authentication | `401` |
| Valid token missing `eventstore:tenant`, blank `eventstore:tenant`, or wrong tenant claim for tenant-management operation | Authorization | `403` when the endpoint enforces tenant-management authorization |
| Missing production `Authority`, missing/blank `Issuer`, missing `Audience`, non-HTTPS `Authority`, `RequireHttpsMetadata=false`, or production `SigningKey` | Startup/options validation | Deterministic validation failure naming the missing or invalid input without secrets |
| Command endpoint or rate-limit evidence unavailable without live infrastructure | Residual risk / deferred evidence | Dated Dev Agent Record note with blocker and boundary owner |

### Latest Technical Information

- Microsoft ASP.NET Core configuration docs for .NET 10 state that configuration keys are case-insensitive, later providers override earlier ones, and double underscores in environment variable names are portable for hierarchical keys because they are converted to colons by configuration. Use `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, `Authentication__JwtBearer__Audience`, and `Authentication__JwtBearer__RequireHttpsMetadata` in operator examples. [Source: Microsoft Learn, "Configuration in ASP.NET Core", `https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0`]
- Microsoft JWT bearer guidance states APIs should validate token signature, issuer, audience, and expiration, and invalid claims or values should result in a 401 response at authentication. Authorization failures after a valid token is accepted may still be 403 depending on the protected endpoint policy. [Source: Microsoft Learn, "Configure JWT bearer authentication in ASP.NET Core", `https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0`]
- The repository is aligned to .NET 10 LTS and SDK `10.0.300`; do not introduce .NET 11 preview dependencies, package upgrades, or new auth libraries for this story. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-latest-dotnet.md`; `global.json`; `Directory.Packages.props`]

### Implementation Guardrails

- Prefer one focused production readiness document, for example `docs/production-auth-readiness.md`, with links from `README.md` or quickstart only if the current docs navigation requires it.
- Keep operator instructions actionable but safe: placeholders are fine; real issuer URLs, client secrets, signing keys, bearer tokens, tenant identifiers, and production user data are not.
- Avoid tests that merely decode unsigned tokens or bypass JWT middleware while claiming production readiness. If a test uses a fake handler, label it as authorization-only and pair it with real JWT bearer validation coverage elsewhere.
- Keep smoke tests deterministic and local. The target is readiness wiring and failure mode coverage, not a live IdP certification suite. Use scoped test-host overrides only; do not mutate shared process environment variables for auth settings.
- Reuse existing test style: xUnit v3, Shouldly, file-scoped namespaces, nullable-safe C#, central package versions, and no inline package references.
- If smoke tests exercise `WebApplicationFactory`, override unrelated DAPR/EventStore dependencies narrowly and document why the test still proves auth readiness instead of infrastructure availability.
- Preserve existing invalid-token coverage in `TenantsQueryControllerIntegrationTests`; do not weaken tests for missing, invalid signature, wrong issuer, wrong audience, expired, forbidden, or safe response-body behavior.
- Do not modify `Hexalith.EventStore` or nested submodule state during this story. Read the submodule for contracts only.

### Files Likely To Update

- `docs/production-auth-readiness.md`: likely new production readiness guide covering required configuration, IdP claim contract, checklist, and smoke verification steps.
- `docs/quickstart.md`: optional small pointer to production readiness docs while preserving local development flow.
- `README.md`: optional documentation link if production readiness should be discoverable from the repo front page.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: likely place to extend valid/invalid JWT smoke coverage for protected tenant query endpoints.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs`: likely place for startup/options validation coverage if Story 11.1 did not already add the specific deployment override cases needed by this story.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: optional command endpoint smoke coverage if existing fixtures can support it without brittle DAPR/AppHost prerequisites.
- `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`: update only if sample claims are inconsistent with the documented contract; do not add production secrets.

### Testing Requirements

- Run at minimum after implementation:
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests`
- If command endpoint smoke coverage changes:
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CommandApiRuntimeIntegrationTests`
- If startup/auth registration or AppHost sample files change:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`
- If only documentation changes are made and no docs build exists, record manual review against the checklist in this story's Dev Agent Record.

### Previous Story Intelligence

- Story 11.1 should give implementers a stable startup validation contract. Do not duplicate its validator work; reference its tests and add only deployment-facing cases that are missing. [Source: `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md`]
- Story 11.2 should give implementers a stable tenant claim contract. Do not reopen direct-versus-source claim policy unless current source contradicts the story; if contradiction exists, record it as a deferred architecture/product decision. [Source: `_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md`]
- Stories 9.3 through 9.5 hardened query authorization, cursor, and response safety. Smoke tests must not regress safe 401/403 behavior or leak tenant data in failed auth paths. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]
- Stories 10.1 through 10.4 are projection write-safety work and are unrelated to deployment auth readiness. Leave active projection implementation changes untouched. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Project Context Reference

- No root application `project-context.md` was found. Submodule project contexts exist under `Hexalith.EventStore`, `Hexalith.Commons`, and `Hexalith.FrontComposer`, but they are reference context only and should not override this application's `AGENTS.md`.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, source-generated logging for structured logs, xUnit and Shouldly for tests, and no inline package versions.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-20T09:55:24+02:00 - Red phase: `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` initially failed with CS0266/CS1929 while wiring the smoke configuration seam, then with `ObjectDisposedException` when mutating `ConfigurationManager` too late in `WebApplicationFactory`.
- 2026-05-20T09:55:24+02:00 - Green phase: moved the deterministic smoke issuer to `JwtBearerOptions` post-configuration so tests exercise real JWT bearer middleware without OIDC discovery or the development HMAC key.
- 2026-05-20T09:55:24+02:00 - Red phase: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests` initially failed because the missing-override test still inherited committed `appsettings.json` issuer/audience values.
- 2026-05-20T09:55:24+02:00 - Green phase: switched the missing-override test to deployment-only in-memory configuration.
- 2026-05-20T09:55:24+02:00 - Focused query smoke gate passed: `TenantsQueryControllerIntegrationTests` 33 passed, 0 failed.
- 2026-05-20T09:55:24+02:00 - Focused auth configuration gate passed: `AuthenticationConfigurationTests` 19 passed, 0 failed.
- 2026-05-20T09:55:24+02:00 - Command API runtime smoke gate passed: `CommandApiRuntimeIntegrationTests` 10 passed, 0 failed.
- 2026-05-20T09:55:24+02:00 - Build gate passed: `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` completed with 0 warnings and 0 errors.
- 2026-05-20T09:55:24+02:00 - Full regression gate passed: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` passed with 697 passed and 5 skipped.

### Completion Notes List

- 2026-05-20T09:55:24+02:00 - Added `docs/production-auth-readiness.md` with production JWT settings, environment variable names, IdP claim expectations, redaction rules, operator checklist, deterministic smoke-test commands, manual deployment smoke checks, failure triage, evidence map, and deployment boundary notes.
- 2026-05-20T09:55:24+02:00 - Linked the production readiness guide from `README.md` and the local quickstart while preserving the quickstart's development-only HMAC token flow.
- 2026-05-20T09:55:24+02:00 - Added production-like query smoke tests that use real JWT bearer middleware with a deterministic non-development issuer/key, covering valid scope, missing token, malformed token, invalid signature, wrong issuer, wrong audience, expired token, missing tenant, blank tenant, and wrong tenant.
- 2026-05-20T09:55:24+02:00 - Added deployment-facing production configuration tests for missing required overrides and production signing-key failure without echoing secret values.
- 2026-05-20T09:55:24+02:00 - Recorded residual boundaries: command-path local evidence uses a mocked command router; full command processing requires EventStore/DAPR infrastructure; rate-limit partition evidence belongs at the EventStore host boundary because Tenants does not register the EventStore rate limiter today.

### File List

- _bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- README.md
- docs/production-auth-readiness.md
- docs/quickstart.md
- tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs

### Change Log

- 2026-05-20 - Implemented deployment auth readiness documentation, deterministic production-like auth smoke tests, deployment override validation coverage, and full regression validation; story status moved to review.
- 2026-05-20 - Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) applied 18 patches (1 decision resolved via EventStore validator verification + 17 direct patches) and recorded 3 deferred items. Patches by file: `docs/production-auth-readiness.md` (exp placeholder, ulid placeholder footnote, 401 failure-triage split into expired/invalid/missing rows); `docs/production-auth-claim-contract.md` (tightened `command:query` legacy framing with `LegacyQueryPermission` constant reference and `query:read` recommendation for new IdPs); `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs` (smoke `application/problem+json` Content-Type, tightened `malformed-token` redaction, `WWW-Authenticate` distinction per 401 case, JSON-array smoke happy-path test, expired-token edge tightened to AddHours(-1)); `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` (real-HTTP `sub`-claim companion to MapInboundClaims test, descriptor + resolve-twice runtime check on `IClaimsTransformation` registration, same-tenant 202 triangulation theory); `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` (renamed to `BlankOrEmpty...` plus `\t`/`\n` rows, global-admin theory covering 10 accepted shapes including casing variants, non-global-admin theory covering boolean-parser deny shapes); `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs` (echo-redaction `ShouldNotContain(omittedValue)`, default-true `RequireHttpsMetadata` contract test, null/empty Authority theory on the no-authority signing-key test). Full Debug/no-restore solution gate: 723 passed, 1 skipped (+22 net new cases, 0 regressions). Story 11.3 -> done.

## Party-Mode Review

- Date/time: 2026-05-19T00:03:50+02:00
- Selected story key: 11-3-deployment-auth-readiness-documentation-and-smoke-tests
- Command/skill invocation used: `/bmad-party-mode 11-3-deployment-auth-readiness-documentation-and-smoke-tests; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Paige (Technical Writer)
- Findings summary:
  - Reviewers agreed the story is valuable and implementable, but needed sharper inherited-contract boundaries so it verifies Story 11.1 production JWT policy and Story 11.2 tenant-claim semantics instead of redefining either.
  - Smoke-test wording needed exact middleware seams, deterministic local JWT fixtures, startup/options validation coverage, and explicit `401` versus `403` outcomes.
  - Documentation needed an operator evidence map, safe token redaction rules, executable checklist structure, and clearer local-development versus production-OIDC separation.
  - Deployment scope needed to keep AppHost/Keycloak material as sample evidence only and avoid live IdP, Aspire, DAPR, Redis, EventStore, or rate-limit dependencies unless robustly gated.
- Changes applied:
  - Added an inherited-contract/non-goal prerequisite stating Story 11.3 verifies existing JWT and tenant-claim contracts and does not create new production auth architecture.
  - Tightened documentation tasks for redacted/fake examples, acceptance-evidence mapping, pass/fail checklist wording, and conditional rate-limit evidence.
  - Tightened smoke-test tasks to require real ASP.NET Core middleware, deterministic local token issuer seams, `eventstore:tenant=system`, `401` authentication cases, `403` authorization cases, and required production configuration validation cases.
  - Added `Documentation Required Structure` and `Smoke-Test Evidence Matrix` sections for the dev agent.
  - Added scoped test-host override guidance to avoid shared environment-variable mutation.
- Findings deferred:
  - Full production IdP setup automation, vendor-specific Keycloak/Entra deployment guidance, live IdP smoke tests, deployment pipeline changes, broader EventStore authorization changes, and rate-limit proof outside this host remain out of scope.
  - Command endpoint and rate-limit executable evidence may be recorded as residual risk if current infrastructure cannot prove them without brittle live dependencies.
- Final recommendation: ready-for-dev after applied clarifications.
- Preflight note: this run treated `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T22:01:07Z` as an active-dev-story soft warning and left the captured dirty 10-3a, sprint-status, submodule, and integration-test paths untouched.

## Advanced Elicitation

- Date/time: 2026-05-19T02:03:36+02:00
- Selected story key: 11-3-deployment-auth-readiness-documentation-and-smoke-tests
- Command/skill invocation used: `/bmad-advanced-elicitation 11-3-deployment-auth-readiness-documentation-and-smoke-tests`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Pre-mortem Analysis; Failure Mode Analysis; Critique and Refine
- Reshuffled Batch 2 method names: Self-Consistency Validation; Architecture Decision Records; User Persona Focus Group; Reverse Engineering; Expand or Contract for Audience
- Findings summary:
  - The story already had the right inherited-contract boundary, but the docs needed stronger evidence discipline so examples, checklists, and transcripts each state what proves readiness and what must be redacted.
  - The smoke-test tasks needed an explicit production-environment validation requirement so tests cannot accidentally pass through development defaults while claiming production readiness.
  - The deterministic JWT fixture guidance needed to avoid reusing the local development HMAC key as production-like evidence.
  - Failure-path verification needed to cover safe diagnostics across authentication, authorization, startup validation, response bodies, captured logs, and validation messages.
- Changes applied:
  - Added evidence/redaction expectations for checklist commands, curl samples, decoded-token examples, and test transcripts.
  - Clarified which checklist checks are deterministic local smoke tests versus operator-run deployment verification and required pass/fail wording for manual steps.
  - Required production-auth override validation tests to force `IHostEnvironment.IsProduction()`.
  - Clarified that production-like local token fixtures must not reuse the development HMAC signing key.
  - Added safe-diagnostic assertions for failed auth, authorization, and startup validation paths.
- Findings deferred:
  - Live IdP certification, deployment-manifest automation, production Keycloak/Entra setup, and EventStore-host rate-limit proof remain deferred to later deployment automation or the owning host boundary.
  - No product-scope, architecture-policy, or cross-story contract changes were applied.
- Final recommendation: ready-for-dev after applied clarifications.
- Preflight note: this run treated `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-19T00:02:05Z` as an active-dev-story soft warning and left the captured dirty 10-3a, sprint-status, submodule, and integration-test paths untouched.

## Review Findings

_Code review 2026-05-20 — Blind Hunter + Edge Case Hunter + Acceptance Auditor adversarial layers, commit `e932bb3`._

### Acceptance Auditor Summary

All 5 acceptance criteria satisfied. Inherited contracts from Stories 11.1 (production JWT validation) and 11.2 (tenant claim contract) verified. Test-seam constraints (real ASP.NET Core middleware via `WebApplicationFactory`, deterministic non-development issuer/key, no OIDC discovery, no process env-var mutation) satisfied. 401 set (missing/malformed/invalid-signature/wrong-issuer/wrong-audience/expired) and 403 set (missing/blank/wrong `eventstore:tenant`) fully covered. Documentation safety (placeholders only) and scope discipline (no production code or submodule changes) satisfied.

### Decisions Resolved (2026-05-20)

- [x] [Review][Decision] `command:query` query-path permission row in `docs/production-auth-claim-contract.md` → resolved as **keep with refined `legacy` framing**. Verified `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs:22,63-68`: `command:query` is accepted by the validator as a category-permission for the query path via the explicit `LegacyQueryPermission` constant (`string.Equals(p, LegacyQueryPermission, StringComparison.OrdinalIgnoreCase)`). The validator's own XML doc-comment names this as "the legacy `command:query` permission still used by the local Keycloak realm." Converted to patch P18: keep the row but tighten the framing to name the validator constant, point at `hexalith-realm.json` as the local sample origin, and recommend `query:read` for new IdP integrations.

### Patches (Action Items)

- [x] [Review][Patch] Replace `"exp": 1893456000` placeholder with `<exp>` in decoded-payload example [docs/production-auth-readiness.md:31] — The Unix timestamp `1893456000` (2030-01-01) will read as "looks expired" in a few years; every other field in the example uses placeholder tokens like `<expected-issuer>` and `<redacted-subject>`. Use a placeholder for consistency.
- [x] [Review][Patch] Make `"<ulid>"` placeholder usable in manual smoke check curl example [docs/production-auth-readiness.md ~line 124] — Operator pasting the literal `<ulid>` placeholder gets a controller validator rejection (R2-A7: ULID parsing required); add a footnote stating "replace `<ulid>` with `Ulid.NewUlid().ToString()`" or substitute a concrete example ULID.
- [x] [Review][Patch] Add `Content-Type: application/problem+json` assertion to 401 smoke tests [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:91-141] — `ListTenants_production_like_smoke_authentication_rejects_invalid_tokens_safely` asserts status and redaction but not Content-Type. A regression that drops the ProblemDetails shape on 401 would still pass; the 403 sister test already asserts this.
- [x] [Review][Patch] Tighten malformed-token redaction assertion to avoid false-positive substring collision [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:119-124] — For `malformed-token`, `body.ShouldNotContain("not-a-jwt")` can flip from green to red if a future ProblemDetails Detail says "Token does not appear to be a JWT". Either skip the literal-token check for the `missing-token`/`malformed-token` rows and add `body.ShouldNotContain("Bearer ")` instead, or pin malformed cases to a higher-entropy token string.
- [x] [Review][Patch] Add `WWW-Authenticate`/reason-code distinction to 401 smoke tests [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:91-141] — All 6 cases assert the same `Unauthorized` status. A regression where wrong-audience starts producing the same code as expired (or vice-versa) cannot be detected. Assert distinct `WWW-Authenticate` `error_description` values (e.g., `TokenExpired` vs `InvalidSignature`) so each row pins a distinct failure layer.
- [x] [Review][Patch] Make `Tenants_host_keeps_jwt_bearer_map_inbound_claims_false` exercise a real HTTP request [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:274-286] — Currently the test resolves `IOptionsMonitor<JwtBearerOptions>.Get(...)` without issuing any request, so a future late `PostConfigure` that flips `MapInboundClaims` after first-request resolution could silently regress. Mint a JWT with both `name` and `sub` claims, send it through the smoke pipeline, and assert the resulting principal exposes the raw `sub` (not `ClaimTypes.NameIdentifier`).
- [x] [Review][Patch] Pin `..._as_transient` descriptor assertion to materialized provider [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:253-272] — The captured `descriptor` reflects the inner `ConfigureServices` builder state and would miss a later `services.Replace(...)` decorator registering `EventStoreClaimsTransformation` via factory. Replace the closure capture with enumeration over `factory.Services.GetServices<IClaimsTransformation>()` or inspect `factory.Services` after `CreateClient()`.
- [x] [Review][Patch] Triangulate cross-tenant theory [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:219-251] — Both InlineData rows (`tenant-a`/`system` and `system`/`tenant-a`) assert the same `reasonCode = "tenant_mismatch"`, so the comment's "triangulate the cross-tenant gate" intent collapses to a single shared assertion. Either assert different `reasonCode` values per direction (if the validator distinguishes them), or add a third row `("tenant-a", "tenant-a")` that asserts `HttpStatusCode.Accepted` to prove the validator is doing equality (not a hard-coded `system` whitelist) and the gate site is well-located.
- [x] [Review][Patch] Extend `GlobalAdministratorMissingTenantClaim...` to source-claim and casing variants [tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs:166-183] — Only `global_admin=true` (lowercase string) is covered. The documented contract also accepts `is_global_admin=true`, `role=GlobalAdministrator`, `role=global-administrator`, and `roles`-array shapes. Convert to `[Theory]` and add rows for each supported global-admin claim shape plus boolean casing (`"True"`, `"TRUE"`) to lock the parser contract.
- [x] [Review][Patch] Extend `NonGlobalAdministratorMissingTenantClaim...` to explicit-deny shapes [tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs:185-199] — Cover `global_admin=false`, `global_admin=""`, and `global_admin="yes"`/`"1"` (not parsed as boolean true) to lock that presence-of-claim alone is NOT bypass; only the truthy boolean-parser path elevates.
- [x] [Review][Patch] Add `"\t"`, `"\n"` rows to `BlankDirectEventStoreTenantClaim` theory and rename to indicate empty-string is covered [tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs:148-164] — Current rows are `""` and `" "`; documented "whitespace fails closed" contract is only enforced for single space. Add tab and newline rows. Also rename the test (e.g., `BlankOrEmptyDirectEventStoreTenantClaimIsNotRepairedBySourceClaims`) since empty string is not "blank" in colloquial English and the current name is misleading.
- [x] [Review][Patch] Extend `ProductionMissingRequiredDeploymentOverrideShouldFailValidation` to `RequireHttpsMetadata` [tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs:51-65] — Theory iterates `Authority`, `Issuer`, `Audience` but not `RequireHttpsMetadata`. Add a row that omits `RequireHttpsMetadata` from overrides and asserts the binder yields the default value AND production validation succeeds — pinning the default-true contract so a future default flip would break this test.
- [x] [Review][Patch] Extend `ProductionSigningKeyWithoutAuthorityShouldFailWithoutEchoingSecret` to null Authority [tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs:159-176] — Test uses `Authority = string.Empty`. Add `[InlineData(null)]` and `[InlineData("")]` to lock that null and empty produce identical failure wording naming `Authentication:JwtBearer:Authority`; otherwise a nullable-binding refactor could shift the error message and downstream tooling silently breaks.
- [x] [Review][Patch] Add `ShouldNotContain(overrides[key])` echo-redaction check to override tests [tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs:51-65] — Currently `ProductionMissingRequiredDeploymentOverrideShouldFailValidation` only asserts the failure message does not echo `SecretSigningKey`. For each theory row, also assert `message.ShouldNotContain(overrides[$"{AuthenticationSectionName}:{key}"]!)` so a validator that prints "expected non-empty, got '<production-issuer-host>'" cannot leak issuer host into deployment logs.
- [x] [Review][Patch] Add JSON-array tenant claim variant to smoke happy path [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:73-89] — `ListTenants_returns_200_when_production_like_smoke_jwt_has_allowed_scope` only uses the direct `eventstore:tenant=system` claim shape, so the smoke factory never exercises `EventStoreClaimsTransformation` end-to-end. Add a sibling test that issues `tenants: ["system"]` (JSON array) through the smoke factory and asserts 200 — proves the transformation chain is wired under the smoke `JwtBearerOptions` post-configure.
- [x] [Review][Patch] Make expired-token edge unambiguous [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:91-127 `expired-token` row] — `expires: DateTime.UtcNow.AddMinutes(-10)` works today because `ConfigureSmokeJwtBearer` sets `ClockSkew = TimeSpan.Zero`. If a future refactor lifts ClockSkew out of `TokenValidationParameters` into a separate `JwtBearerOptions` shadow property, 10-minute expiry could still pass with the 5-minute default skew. Set `expires: DateTime.UtcNow.AddHours(-1)` to make the case unambiguous, or add a second `expired-token-edge` row with `expires: DateTime.UtcNow.AddSeconds(-1)` to lock the zero-skew contract.
- [x] [Review][Patch] Split 401 row in Failure Triage table [docs/production-auth-readiness.md:132-141] — The current table lumps all 401 outcomes ("Check signature source, issuer, audience, expiration, and malformed token shape") into one row. The handler emits distinct `WWW-Authenticate` headers for expired vs invalid vs missing tokens. Split into three rows: (1) `401 with WWW-Authenticate "error_description=The token has expired"` → expired; (2) `401 with WWW-Authenticate error="invalid_token"` → signature/issuer/audience; (3) `401 with no WWW-Authenticate error attribute` → missing token.
- [x] [Review][Patch] Tighten `command:query` "legacy" framing in `docs/production-auth-claim-contract.md` query-path permission row [docs/production-auth-claim-contract.md] — Verified accepted by `ClaimsRbacValidator.cs:22,63-68` as the explicit `LegacyQueryPermission` constant; keep the row but tighten the wording: (a) name the validator constant (`LegacyQueryPermission` in `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs`); (b) cite `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` as the local sample origin; (c) recommend `query:read` (the non-legacy shape) for new IdP integrations so new realms do not propagate the legacy shape.

### Deferred

- [x] [Review][Defer] `/process` endpoint authentication behavior test [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:44-71] — The `/process` route is not mapped with `RequireAuthorization()`, so it accepts anonymous requests today. There is no defensive test asserting either "anonymous accepted" or "auth required". A future patch that adds `RequireAuthorization()` to the route (and breaks DAPR sidecar contracts) would not be caught. Pre-existing test-infrastructure gap, defer to a `/process` authorization story.
- [x] [Review][Defer] Increase `SmokeJwtSigningKey` constant entropy [tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:40] — The constant `"this-is-a-smoke-test-signing-key-minimum-32-chars"` literally embeds the string "smoke-test-signing-key" in the source file. If a future dev-mode exception page or verbose test logger formatter ever surfaces source file paths into 401 response bodies, the redaction assertion would catch a false positive caused by the constant collision. Low-risk hypothetical; address as part of broader test-fixture hardening.
- [x] [Review][Defer] Commit hygiene: single commit `e932bb3 test(auth): add production readiness smoke coverage` bundled story 11.2 close-out (status `review -> done`, 15 patches recorded), story 11.3 implementation (docs + tests), sprint-status update, and deferred-work update — Conventional Commits split would have produced `chore(story): close 11.2 patches` + `test(auth): add 11.3 smoke coverage`. Commit already shipped to `main`; rebase would rewrite shared history. Pre-existing process gap; record for retrospective.

### Dismissed

- `useTestAuthentication` default flip ripple risk (Blind Hunter) — false positive. Full regression gate passed (697 passed, 5 skipped) per `## Dev Agent Record`; if any sibling test class relied on the previous `true` default it would have surfaced in regression.
- `SmokeJwtIssuer = "https://identity.smoke.example.test/realms/hexalith"` looks production-real (Blind Hunter) — false positive. `.test` TLD is RFC 2606-reserved; `example.test` is the documented safe-example subdomain. Operator grep collision risk is negligible.
- `SmokeJwtSigningKey` committed in test source contradicts doc rule (Blind Hunter) — false positive. The doc rule "never commit signing keys" applies to production secrets; the constant is explicitly named with `SmokeJwt` prefix, lives in a test file, and is the documented smoke-only signing material.
- GlobalAdmin test should also assert claim survival post-transformation (Blind Hunter) — false positive. Speculative regression chain (strip `global_admin` AND silently flip "no global-admin + no tenant" to authorized) is too implausible to be worth the test; existing fail-closed coverage in sibling tests already pins the behavior.
- `MessageId` switched from `Guid.NewGuid()` to `UniqueIdHelper.GenerateSortableUniqueStringId()` is unattributed scope creep (Blind Hunter) — false positive. Documented in this same commit as story 11.2 review patch P15 (CLAUDE.md R2-A7 compliance: ULID required, GUID rejected for `messageId`/`correlationId`/`aggregateId`/`causationId`).
- `docs/quickstart.md` "valid for 8 hours" claim is unsubstantiated by this diff (Blind Hunter) — false positive. The unchanged context line is pre-existing in the quickstart and out of scope; the new lines in this diff are limited to a pointer to the production readiness guide.
- `docs/production-auth-claim-contract.md` rate-limit "anonymous fallback" callout reads contradictory (Blind Hunter) — false positive. The sentence is saying "rate-limit not registered on Tenants host, but provision the claim anyway so audit logs/partitioning behave consistently when EventStore host is involved". The two statements address different hosts and are complementary, not contradictory.
- Permission row shape differs between `docs/production-auth-readiness.md` (single row) and `docs/production-auth-claim-contract.md` (split rows) (Blind Hunter) — false positive. The two docs are intentionally for different audiences: readiness doc is operator-facing (concise checklist), claim-contract doc is IdP-integrator-facing (precise mapping). Audience-appropriate.
- Spec's "Smoke-Test Evidence Matrix" structure not rendered as a single table (Acceptance Auditor `nit`) — false positive. Spec wording is "should contain a smoke-test evidence matrix". Content is present in the Deployment Readiness Checklist, Failure Triage, Evidence Map, and Recorded Deployment Boundaries sections. Editorial drift only.
- Sprint-status.yaml duplicate `# last_updated:` comment lines (Blind Hunter + Edge Case Hunter) — false positive. The header comments are an append-only audit log; the canonical `last_updated:` YAML field at line 123 (`2026-05-20`) is unambiguous and matches the latest update. No automation parses the comment headers; downstream tools read the YAML field.
