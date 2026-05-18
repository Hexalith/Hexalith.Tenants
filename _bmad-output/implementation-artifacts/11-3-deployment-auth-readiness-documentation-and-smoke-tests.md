# Story 11.3: Deployment Auth Readiness Documentation and Smoke Tests

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

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

## Tasks / Subtasks

- [ ] Inventory existing auth and deployment documentation before writing new docs. (AC: 1, 2, 5)
  - [ ] Read `docs/quickstart.md`, `README.md`, `src/Hexalith.Tenants/appsettings.json`, `src/Hexalith.Tenants/appsettings.Development.json`, and the AppHost Keycloak realm sample.
  - [ ] Read Story 11.1 and Story 11.2 before editing docs so production JWT validation and tenant claim wording stay aligned.
  - [ ] Read EventStore security and configuration docs from the current submodule commit before copying claim, authority, or rate-limit terminology.
  - [ ] Keep the existing local quickstart usable; add production readiness guidance as a separate section or page instead of turning the quickstart into a production deployment guide.
- [ ] Add production auth readiness documentation. (AC: 1, 2, 4, 5)
  - [ ] Document required production configuration keys using .NET hierarchical names and environment-variable forms: `Authentication:JwtBearer:Authority` / `Authentication__JwtBearer__Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata`, and the intentionally absent or empty production `SigningKey`.
  - [ ] State that production must use an absolute HTTPS OIDC authority with `RequireHttpsMetadata=true`; symmetric signing-key auth is development/test only unless a later architecture decision creates an explicit production exception.
  - [ ] Document that ASP.NET Core environment variables override appsettings through the default configuration providers, and that double underscores are the portable separator for hierarchical keys.
  - [ ] Document required IdP token contents: issuer, audience, subject, expiration, tenant claim contract from Story 11.2, and EventStore permission/domain claims when command API smoke tests use EventStore gateway endpoints.
  - [ ] Include safe operator examples with placeholders only. Do not commit real authorities, client IDs, signing keys, bearer tokens, secrets, tenant data, or production user identifiers.
  - [ ] Keep development JWT generation separate and clearly labelled as local-only HMAC signing with `appsettings.Development.json`.
- [ ] Add a deployment readiness checklist operators can run before release. (AC: 1, 2, 4)
  - [ ] Include startup validation checks for missing placeholders, whitespace values, non-HTTPS authorities, ambiguous Authority plus SigningKey, and `RequireHttpsMetadata=false` in production.
  - [ ] Include token inspection checks for `iss`, `aud`, `sub`, `exp`, and the source or normalized tenant claim that yields `eventstore:tenant=system`.
  - [ ] Include request-level checks for one protected tenant query endpoint and one command endpoint when the local test topology can support both.
  - [ ] Include rate-limit partition verification only if the Tenants host actually registers the relevant EventStore rate limiter. If it does not, document the finding and defer executable rate-limit evidence to the EventStore host boundary or later deployment automation.
  - [ ] Include failure triage guidance that names missing configuration keys or claim types without instructing operators to log full tokens or secrets.
- [ ] Add production-like smoke test coverage without depending on a real external IdP. (AC: 3, 4)
  - [ ] Prefer test infrastructure that exercises the real Tenants authentication path using locally generated valid and invalid JWTs, production-style options, and isolated host/test overrides.
  - [ ] Cover a valid token with matching issuer, audience, subject, and tenant claim that can reach an authorized protected endpoint.
  - [ ] Cover invalid signature, wrong issuer, wrong audience, expired token, missing tenant claim, blank tenant claim, and wrong tenant claim. Assert 401 versus 403 according to the layer that rejects the request.
  - [ ] Cover missing or invalid production auth overrides through startup/options validation tests when the service can be started without unrelated DAPR/AppHost dependencies.
  - [ ] Do not require Keycloak, Entra ID, OIDC network discovery, real deployment manifests, DAPR sidecars, Redis, or Aspire orchestration for the narrow smoke tests unless an existing fixture already handles those prerequisites robustly.
  - [ ] If an Aspire/AppHost smoke path is added, keep it as an optional or prerequisite-gated test and preserve existing skip behavior when Docker, DAPR, Redis, or placement prerequisites are unavailable.
- [ ] Align sample AppHost and local docs with production wording. (AC: 1, 2, 5)
  - [ ] Treat `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` as local sample evidence only; do not present it as a production realm export.
  - [ ] If the sample realm already emits direct `eventstore:tenant`, document how that relates to Story 11.2's supported source-claim normalization.
  - [ ] Verify quickstart token examples remain development-only and still explain why `tenants: ["system"]` works locally.
  - [ ] Add links from `README.md` or `docs/quickstart.md` only where they help operators find production readiness guidance without cluttering first-run development flow.
- [ ] Record unresolved deployment boundary decisions. (AC: 2, 3, 4)
  - [ ] If smoke tests cannot prove a command endpoint without live DAPR/EventStore infrastructure, document the exact blocker and keep the test focused on protected query/auth behavior.
  - [ ] If Tenants does not register rate limiting in the current host, document that rate-limit partitioning is not directly smoke-testable here and reference the EventStore boundary that owns it.
  - [ ] If a future deployment manifest, Helm chart, Aspire publish profile, or cloud-specific Keycloak/Entra setup is needed, record it as deferred deployment automation rather than adding it ad hoc.

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

### Latest Technical Information

- Microsoft ASP.NET Core configuration docs for .NET 10 state that configuration keys are case-insensitive, later providers override earlier ones, and double underscores in environment variable names are portable for hierarchical keys because they are converted to colons by configuration. Use `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, `Authentication__JwtBearer__Audience`, and `Authentication__JwtBearer__RequireHttpsMetadata` in operator examples. [Source: Microsoft Learn, "Configuration in ASP.NET Core", `https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0`]
- Microsoft JWT bearer guidance states APIs should validate token signature, issuer, audience, and expiration, and invalid claims or values should result in a 401 response at authentication. Authorization failures after a valid token is accepted may still be 403 depending on the protected endpoint policy. [Source: Microsoft Learn, "Configure JWT bearer authentication in ASP.NET Core", `https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0`]
- The repository is aligned to .NET 10 LTS and SDK `10.0.300`; do not introduce .NET 11 preview dependencies, package upgrades, or new auth libraries for this story. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-latest-dotnet.md`; `global.json`; `Directory.Packages.props`]

### Implementation Guardrails

- Prefer one focused production readiness document, for example `docs/production-auth-readiness.md`, with links from `README.md` or quickstart only if the current docs navigation requires it.
- Keep operator instructions actionable but safe: placeholders are fine; real issuer URLs, client secrets, signing keys, bearer tokens, tenant identifiers, and production user data are not.
- Avoid tests that merely decode unsigned tokens or bypass JWT middleware while claiming production readiness. If a test uses a fake handler, label it as authorization-only and pair it with real JWT bearer validation coverage elsewhere.
- Keep smoke tests deterministic and local. The target is readiness wiring and failure mode coverage, not a live IdP certification suite.
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

### Completion Notes List

### File List

- _bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md
