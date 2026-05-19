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
- Story 11.3 verifies and documents inherited contracts; it does not create new production JWT policy, tenant-claim semantics, live IdP provisioning, or production Keycloak/AppHost architecture.

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
  - [ ] Use fake tokens, redacted decoded header/payload examples, or placeholder values only; never instruct operators to paste or log full bearer tokens, signing keys, client secrets, refresh tokens, or real issuer metadata.
  - [ ] Keep development JWT generation separate and clearly labelled as local-only HMAC signing with `appsettings.Development.json`.
  - [ ] Include an evidence map that links each acceptance criterion to the documentation section, test name, expected failure mode, or documented residual risk that proves it.
  - [ ] For every checklist command, curl sample, decoded-token example, or test transcript, state the expected observable evidence and the redaction rule that keeps tokens, secrets, authorities, tenant data, and user identifiers out of committed docs and logs.
- [ ] Add a deployment readiness checklist operators can run before release. (AC: 1, 2, 4)
  - [ ] Include startup validation checks for missing placeholders, whitespace values, non-HTTPS authorities, ambiguous Authority plus SigningKey, and `RequireHttpsMetadata=false` in production.
  - [ ] Include token inspection checks for `iss`, `aud`, `sub`, `exp`, and the source or normalized tenant claim that yields `eventstore:tenant=system`.
  - [ ] Include request-level checks for one protected tenant query endpoint and one tenant-management command endpoint when the local test topology can support both. State which endpoint proves authentication, which proves authorization, and which layer returns the failure.
  - [ ] Separate checks that can be proven by deterministic local smoke tests from checks that remain operator-run deployment verification, and ensure each manual step has pass/fail wording rather than relying on prose inspection alone.
  - [ ] Include rate-limit partition verification only if the Tenants host actually registers the relevant EventStore rate limiter. If enabled, confirm partitioning uses normalized subject/client identity and does not log token contents. If it does not, document the finding and defer executable rate-limit evidence to the EventStore host boundary or later deployment automation.
  - [ ] Include failure triage guidance that names missing configuration keys or claim types without instructing operators to log full tokens or secrets.
- [ ] Add production-like smoke test coverage without depending on a real external IdP. (AC: 3, 4)
  - [ ] Exercise the real Tenants ASP.NET Core authentication/authorization middleware through `WebApplicationFactory` or the existing integration host; do not instantiate controllers, handlers, or fake authorization paths while claiming production auth readiness.
  - [ ] Use a production-like local token issuer seam with deterministic issuer, audience, claims, signing material, and clock/skew behavior. Do not call a real IdP or OIDC discovery endpoint from the default smoke tests, and do not reuse the development HMAC key from `appsettings.Development.json` as production-like evidence.
  - [ ] Cover a valid token with matching issuer, audience, subject, and `eventstore:tenant=system` that can reach an authorized protected endpoint.
  - [ ] Cover missing token, malformed token, invalid signature, wrong issuer, wrong audience, and expired token as `401` authentication failures.
  - [ ] Cover valid authentication with missing, blank, or wrong `eventstore:tenant` as `403` authorization failures when the target endpoint enforces tenant-management authorization.
  - [ ] Cover missing or invalid production auth overrides through startup/options validation tests that force `IHostEnvironment.IsProduction()` for missing `Authority`, missing/blank `Issuer`, missing `Audience`, non-HTTPS `Authority`, `RequireHttpsMetadata=false`, and any production `SigningKey`.
  - [ ] Assert failed authentication, authorization, and startup validation paths name safe configuration or claim categories only and do not echo bearer tokens, signing material, decoded payloads, or secret values in response bodies, logs captured by tests, or validation messages.
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

### Completion Notes List

### File List

- _bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md

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
