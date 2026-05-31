# Story 11.1: Production JWT Configuration Validation

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want production JWT configuration to be validated before deployment,
so that Tenants does not fail later at runtime or accept unsafe authentication settings unexpectedly.

## Acceptance Criteria

1. Given production `appsettings.json` contains empty JWT `Authority` and `SigningKey` placeholders, when Tenants starts with `IHostEnvironment.IsProduction()` and without AppHost, environment, user-secret, or deployment overrides, then startup/options validation fails with a clear authentication configuration validation error.
2. Given production JWT settings are supplied through environment variables, AppHost, or deployment configuration, when the service starts with `IHostEnvironment.IsProduction()`, `Authority`, `Issuer`, `Audience`, and `RequireHttpsMetadata=true`, then `EventStoreAuthenticationOptions` validation succeeds without requiring secrets in committed appsettings files.
3. Given development mode uses symmetric-key JWT validation, when `appsettings.Development.json` or equivalent local overrides are loaded with a non-production host environment, then development authentication remains usable without weakening production validation.
4. Given authentication configuration fails validation, when logs or exception messages are emitted, then they identify the missing configuration key or invalid setting without exposing signing keys, tokens, or bearer material.
5. Given focused configuration tests run, when production-valid, production-invalid, and development-valid configurations are bound, then tests verify startup/options validation behavior for each mode.
6. Given a production deployment accidentally supplies both `Authority` and `SigningKey`, when validation runs, then Tenants rejects the ambiguous production configuration with a safe message instead of relying on implicit EventStore authority/OIDC precedence.
7. Given `RequireHttpsMetadata` is disabled, when `IHostEnvironment.IsProduction()` and `Authority` is configured, then validation rejects the setting; non-production development/test overrides may continue to use symmetric-key auth with `RequireHttpsMetadata=false`.
8. Given production `Authority` or `SigningKey` values contain only whitespace, when options validation runs, then whitespace is treated as missing or invalid configuration and does not satisfy production readiness.
9. Given production `Authority` is configured, when validation runs, then the authority value must be an absolute HTTPS URI before OIDC discovery is attempted; relative, malformed, empty, whitespace, or non-HTTPS authorities fail with a safe configuration error.
10. Given Tenants registers production-specific validation beside the shared EventStore validator, when `ValidateOnStart` or the options factory evaluates `EventStoreAuthenticationOptions`, then both the shared required-key rules and the Tenants production safety rules are exercised without replacing `ConfigureJwtBearerOptions`.

## Tasks / Subtasks

- [x] Confirm the existing EventStore authentication validation contract before changing code. (AC: 1-7)
  - [x] Read `EventStoreAuthenticationOptions`, `ValidateEventStoreAuthenticationOptions`, and `ConfigureJwtBearerOptions` from the current `Hexalith.EventStore` submodule commit.
  - [x] Verify the current rules: `Issuer` and `Audience` are required, either `Authority` or `SigningKey` is required, and `SigningKey` must be at least 32 characters when present.
  - [x] Verify whether the current EventStore validator treats whitespace-only `Authority`, `Issuer`, `Audience`, or `SigningKey` values as configured; if it does, cover Tenants production validation so whitespace placeholders cannot pass readiness checks.
  - [x] Confirm whether the current EventStore validator permits both `Authority` and `SigningKey`, and whether it has any environment-aware rule for production `RequireHttpsMetadata`.
  - [x] Do not modify `Hexalith.EventStore` or update nested submodules for this story. If the correct fix belongs in EventStore, record the blocker instead of duplicating or forking the shared validator.
- [x] Add focused Tenants-side validation coverage for the current startup wiring. (AC: 1, 2, 5)
  - [x] Add tests that bind the committed production `Authentication:JwtBearer` section from `src/Hexalith.Tenants/appsettings.json` and assert validation fails because both `Authority` and `SigningKey` are empty.
  - [x] Add tests that supply production-style OIDC overrides (`Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata=true`, empty or absent `SigningKey`) and assert validation succeeds.
  - [x] Assert the validation failure mentions `Authentication:JwtBearer` and the missing `Authority`/`SigningKey` requirement without including token values or secret-like payloads.
  - [x] Assert whitespace-only production `Authority`, `SigningKey`, `Issuer`, or `Audience` values fail safely and are not treated as valid overrides.
  - [x] Assert production `Authority` rejects relative, malformed, and non-HTTPS values before network OIDC metadata discovery can occur.
  - [x] Prove the registered validator composition by exercising an options factory, startup validator, or narrow host startup path that includes both `ValidateEventStoreAuthenticationOptions` and any Tenants-specific production validator.
  - [x] Prefer direct `IOptions<EventStoreAuthenticationOptions>` / `IValidateOptions<EventStoreAuthenticationOptions>` tests when they prove the binding contract; use a host/WebApplicationFactory startup test only if direct options tests cannot prove `ValidateOnStart` integration.
- [x] Preserve local development symmetric-key behavior. (AC: 3, 5)
  - [x] Add or extend tests that bind `src/Hexalith.Tenants/appsettings.Development.json` and assert `Issuer`, `Audience`, `SigningKey`, and `RequireHttpsMetadata=false` remain valid for local development.
  - [x] Keep the existing integration-test JWT constants aligned with the development settings; do not copy production secrets into tests.
  - [x] If any dev-mode startup validation becomes stricter, prove existing JWT integration tests still exercise valid issuer, audience, expiry, and signature behavior.
- [x] Decide and encode ambiguous production safety rules at the Tenants boundary. (AC: 4, 6, 7)
  - [x] If EventStore already rejects ambiguous or unsafe production settings by the time this story is implemented, add Tenants tests that lock that behavior.
  - [x] If EventStore does not reject `Authority` plus `SigningKey`, add the least invasive Tenants-side production validation that fails with a safe message and does not log the signing key.
  - [x] If EventStore does not reject `RequireHttpsMetadata=false` with `Authority` in production, add narrow Tenants-side environment-aware validation for `IHostEnvironment.IsProduction()`; do not silently accept production OIDC over non-HTTPS metadata.
  - [x] If EventStore does not validate production authority URI shape, add narrow Tenants-side production validation that requires an absolute HTTPS `Authority` URI and does not perform OIDC discovery.
  - [x] Treat `IHostEnvironment.IsProduction()` as the production boundary for these Tenants-specific rules. Do not apply the production restrictions to `Development` or other explicitly tested local development paths unless the implementation records and justifies that broader policy.
  - [x] Keep any Tenants-specific validation narrow and registered beside the existing options setup in `Program.cs`; do not replace `ConfigureJwtBearerOptions`.
- [x] Keep committed configuration and deployment override boundaries explicit. (AC: 1, 2, 4)
  - [x] Keep `src/Hexalith.Tenants/appsettings.json` free of `SigningKey` secrets and real OIDC endpoints unless a deliberate non-secret placeholder is documented.
  - [x] Keep `src/Hexalith.Tenants/appsettings.Development.json` development-only; do not make production depend on the development signing key.
  - [x] Verify standard .NET configuration precedence can satisfy the production settings through environment variables using keys such as `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, and `Authentication__JwtBearer__Audience`.
  - [x] Do not add a new secret store integration, Keycloak setup, Entra app registration, deployment manifest, or smoke-test workflow; Stories 11.2 and 11.3 own claim-contract and deployment-readiness documentation.
- [x] Add or update tests without introducing infrastructure dependencies. (AC: 1-7)
  - [x] Place focused tests under `tests/Hexalith.Tenants.Server.Tests/Configuration/` unless an existing integration-test fixture is clearly required.
  - [x] Use xUnit v3 and Shouldly, matching existing configuration tests.
  - [x] Do not require DAPR sidecars, Redis, Aspire orchestration, Keycloak, Entra ID, network OIDC discovery, or real tokens to test configuration binding and validation.
  - [x] Add regression assertions that validation errors do not echo configured signing key values.
  - [x] Add regression assertions that non-production symmetric-key settings remain valid when `RequireHttpsMetadata=false`, while production `Authority` with `RequireHttpsMetadata=false` fails.

## Dev Notes

### Current Code State

- `src/Hexalith.Tenants/Program.cs` binds `Authentication:JwtBearer` to `EventStoreAuthenticationOptions`, calls `ValidateOnStart()`, registers `ValidateEventStoreAuthenticationOptions`, registers `ConfigureJwtBearerOptions`, and enables default JWT bearer authentication plus authorization. This story should harden or test that wiring rather than creating a parallel authentication stack. [Source: `src/Hexalith.Tenants/Program.cs`]
- Committed production `src/Hexalith.Tenants/appsettings.json` currently sets `Authentication:JwtBearer:Authority` to an empty string, `Audience` to `hexalith-tenants`, `Issuer` to `hexalith`, `SigningKey` to an empty string, and `RequireHttpsMetadata` to `true`. Without overrides, this should fail validation because neither OIDC authority nor symmetric signing key is configured. [Source: `src/Hexalith.Tenants/appsettings.json`]
- Committed development `src/Hexalith.Tenants/appsettings.Development.json` sets `Issuer` to `hexalith-dev`, `Audience` to `hexalith-tenants`, a test-only symmetric signing key, and `RequireHttpsMetadata=false`. Preserve this local development path. [Source: `src/Hexalith.Tenants/appsettings.Development.json`]
- Existing query integration tests create JWTs with issuer `hexalith-dev`, audience `hexalith-tenants`, and the development signing key, then assert missing, invalid signature, wrong issuer, wrong audience, and expired token behavior. Do not weaken these tests while adding config validation. [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- Existing configuration tests show the repository pattern for loading appsettings and validating options with xUnit and Shouldly. Follow that style for auth configuration tests. [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs`]

### EventStore Authentication Contract

- `EventStoreAuthenticationOptions` is owned by the EventStore submodule and exposes `Authority`, `Audience`, `Issuer`, `SigningKey`, and `RequireHttpsMetadata`. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreAuthenticationOptions.cs`]
- `ValidateEventStoreAuthenticationOptions` currently requires either `Authority` or `SigningKey`, always requires `Issuer` and `Audience`, and requires `SigningKey.Length >= 32` when a signing key is present. The current source uses empty-string checks, so implementation must verify whitespace behavior before relying on the shared validator for production readiness. It does not currently encode a Tenants-specific production environment policy in the source reviewed for this story. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreAuthenticationOptions.cs`]
- `ConfigureJwtBearerOptions` preserves original JWT claim names (`MapInboundClaims=false`), validates issuer, audience, signing key, and lifetime with one-minute clock skew, uses OIDC discovery when `Authority` is set, and falls back to symmetric key mode when only `SigningKey` is set. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs`]
- EventStore documentation says OIDC discovery is the recommended production mode; symmetric key mode is for development/testing; `Issuer` and `Audience` are always required; and if both `Authority` and `SigningKey` are present, the runtime takes the OIDC path, so signing keys should be cleared when switching to production OIDC. [Source: `Hexalith.EventStore/docs/guides/security-model.md#Layer 1: JWT Authentication`; `Hexalith.EventStore/docs/guides/configuration-reference.md#Authentication and JWT`]

### Architecture and Scope Boundaries

- Epic 11 is Production Authorization Readiness. Story 11.1 is limited to production JWT configuration validation; Story 11.2 owns the `eventstore:tenant` claim contract; Story 11.3 owns deployment documentation and smoke tests. Do not pull those later story scopes into this one. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 11: Production Authorization Readiness`]
- The architecture defines two authorization layers: EventStore JWT-based API authorization and Tenants domain RBAC inside aggregate Handle methods. This story hardens the first layer's configuration readiness only; do not change domain RBAC semantics. [Source: `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]
- The `system` tenant remains a deployment prerequisite in EventStore domain registration and identity provider JWT claims. This story may mention the deployment prerequisite but should not define the final claim mapping contract; Story 11.2 owns that. [Source: `_bmad-output/planning-artifacts/architecture.md#Identity Mapping (ADR)`; `_bmad-output/planning-artifacts/epics.md#Story 11.2: EventStore Tenant Claim Contract`]
- Do not add package dependencies or central package versions. The current relevant package pins are Microsoft ASP.NET Core packages `10.0.8`, Microsoft.Extensions.Configuration.Binder `10.0.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and Microsoft.NET.Test.Sdk `18.5.1`. [Source: `Directory.Packages.props`]

### Implementation Guardrails

- Reuse the EventStore validator and JWT options configurator wherever possible. Add Tenants-specific validation only for Tenants-specific production safety rules that EventStore intentionally does not own.
- Treat `IHostEnvironment.IsProduction()` as the concrete production boundary for Story 11.1. The required Tenants production-safe mode is OIDC with an absolute HTTPS `Authority`, non-whitespace `Issuer`, non-whitespace `Audience`, no `SigningKey`, and `RequireHttpsMetadata=true`; the committed empty-placeholder configuration, whitespace-only values, malformed or non-HTTPS authorities, `Authority` plus `SigningKey`, and production `Authority` with `RequireHttpsMetadata=false` must fail validation unless a later story or explicit architecture decision introduces a named escape hatch.
- Keep validation failure messages specific enough to identify keys such as `Authentication:JwtBearer:Authority`, `Authentication:JwtBearer:SigningKey`, `Authentication:JwtBearer:Issuer`, and `Authentication:JwtBearer:Audience`, but never include the configured signing key or bearer token value.
- Do not perform network OIDC metadata discovery in unit tests. Configuration validation should prove binding and option validation, not identity-provider availability.
- Register any Tenants-specific production validator as an additional `IValidateOptions<EventStoreAuthenticationOptions>` beside the existing EventStore validator so `ValidateOnStart` evaluates the combined policy. Do not replace the shared validator, and do not add direct JWT bearer configuration that bypasses `ConfigureJwtBearerOptions`.
- Do not change controller authorization policies, query routes, command routes, JWT claim names, RBAC role hierarchy, tenant visibility, rate-limit partitioning, cursor behavior, or DAPR component configuration.
- If a WebApplicationFactory startup test is added, override unrelated DAPR/EventStore dependencies narrowly so the test proves auth configuration startup validation and not infrastructure availability.
- Prefer options-validation and startup-validation tests with explicit host environments. Avoid brittle assertions on full exception text; assert the failed key or unsafe setting is named and configured signing key/token values are absent.

### Files Likely To Update

- `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs`: likely new focused tests for production invalid, production OIDC valid, development symmetric valid, ambiguous settings, and safe failure messages.
- `src/Hexalith.Tenants/Program.cs`: update only if Tenants-specific production validation must be registered beside the existing `EventStoreAuthenticationOptions` setup.
- `src/Hexalith.Tenants/Configuration/`: optional location for a narrow Tenants-specific auth options validator if EventStore does not own the required production safety rule.
- `src/Hexalith.Tenants/appsettings.json`: update only for placeholder clarity; do not commit real OIDC endpoints or secrets.
- `src/Hexalith.Tenants/appsettings.Development.json`: update only if test-only local JWT settings intentionally change, and keep integration tests aligned.

### Testing Requirements

- Run at minimum after implementation:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests`
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`
- If production code or startup registration changes, also run:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`
- If a validation rule is deferred to EventStore instead of implemented in Tenants, record the missing shared-contract decision in this story's Dev Agent Record during implementation.

### Latest Technical Information

- The repository is aligned to .NET 10 LTS and currently pins SDK `10.0.300` through `global.json`. Do not introduce .NET 11 preview dependencies or new package updates in this story. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-latest-dotnet.md`; `global.json`]
- ASP.NET Core JWT bearer package usage is already pinned centrally. This story should rely on the existing EventStore `ConfigureJwtBearerOptions` path instead of adding direct JWT bearer option configuration in Tenants. [Source: `Directory.Packages.props`; `src/Hexalith.Tenants/Program.cs`]

### Previous Story Intelligence

- Stories 9.3, 9.4, and 9.5 hardened query visibility, actor-layer guardrails, and pagination/cursor behavior. Auth configuration validation must not change query authorization outcomes, invalid-cursor behavior, or safe response-body requirements. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]
- Stories 10.1 through 10.4 are projection write-safety stories and are unrelated to JWT startup validation. Leave active projection implementation work untouched. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T11:01:20Z`]
- Epic 11 follows these stories with claim-contract and deployment-readiness work. Keep this story focused on configuration validation so later claim mapping and smoke tests have a stable startup contract to build on. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 11: Production Authorization Readiness`]

### Git Intelligence

- Recent history includes active code work for Story 9.5 and automation bookkeeping; this run began with an active-dev-story soft warning for Story 10.1 and projection source/test changes. Stage only this story creation work, sprint-status updates for Story 11.1, and the pre-dev hardening run log. [Source: `git log -5 --oneline`; `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T11:01:20Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; EventStore submodule docs and source are reference context only for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-19T16:39:32+02:00 - Red phase: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests` failed with CS0246 because `ValidateTenantProductionAuthenticationOptions` did not exist yet.
- 2026-05-19T16:39:32+02:00 - Green phase: focused authentication configuration tests passed: 15 passed, 0 failed.
- 2026-05-19T16:39:32+02:00 - Regression: `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` passed: 18 passed, 0 failed.
- 2026-05-19T16:39:32+02:00 - Build gate: `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` passed with 0 warnings and 0 errors.
- 2026-05-19T16:39:32+02:00 - Full regression: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` passed: 655 passed, 1 skipped.

### Completion Notes List

- 2026-05-18T16:06:26+02:00 - Party-mode review applied pre-dev clarifications for concrete production environment semantics, ambiguous `Authority` plus `SigningKey` rejection, production `RequireHttpsMetadata=false` rejection, safe error assertions, and narrow options/startup validation scope.
- 2026-05-19T16:39:32+02:00 - Confirmed the shared EventStore validator requires `Issuer`, `Audience`, either `Authority` or `SigningKey`, and signing keys of at least 32 characters, but it uses empty-string checks, permits `Authority` plus `SigningKey`, and has no production `RequireHttpsMetadata` or authority URI-shape rule.
- 2026-05-19T16:39:32+02:00 - Added a Tenants-specific production validator registered beside the shared EventStore validator. In `Production`, it requires an absolute HTTPS `Authority`, non-whitespace `Issuer` and `Audience`, empty `SigningKey`, and `RequireHttpsMetadata=true`; non-production symmetric-key behavior remains delegated to the shared validator.
- 2026-05-19T16:39:32+02:00 - Added focused options/startup validation tests for committed production placeholders, production OIDC overrides, environment variable precedence, development appsettings, whitespace values, malformed/non-HTTPS authorities, ambiguous signing keys, disabled metadata, composed validators, and safe failure messages.

### File List

- _bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs
- src/Hexalith.Tenants/Program.cs
- tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj

### Change Log

- 2026-05-19 - Implemented Tenants production JWT configuration validation and focused regression tests; story status moved to review.
- 2026-05-19 - Code review complete: 1 patch applied (env-var test xUnit isolation), 4 deferred (name-gating consistency, AddSingleton vs TryAddEnumerable, Authority host-trust policy, composition-test wording brittleness); ~17 dismissed. Server.Tests gate 438/438; story status moved to done.

## Party-Mode Review

- Date/time: 2026-05-18T16:06:26+02:00
- Selected story key: 11-1-production-jwt-configuration-validation
- Command/skill invocation used: `/bmad-party-mode 11-1-production-jwt-configuration-validation; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Reviewers agreed the story is directionally valuable but left production policy choices open in a way that could yield incompatible implementations.
  - The main architecture risk was placing Tenants-specific production policy into shared EventStore validation without an explicit cross-repo decision.
  - The main implementation and test risks were ambiguous behavior for production `Authority` plus `SigningKey`, unclear `RequireHttpsMetadata=false` semantics, brittle startup-message assertions, and tests that accidentally require OIDC metadata, DAPR, AppHost, or controller infrastructure.
- Changes applied:
  - Clarified that Story 11.1 production behavior is keyed to `IHostEnvironment.IsProduction()`.
  - Tightened production-ready mode to OIDC with `Authority`, `Issuer`, `Audience`, and `RequireHttpsMetadata=true`.
  - Required Tenants to reject production `Authority` plus `SigningKey` instead of relying on implicit EventStore OIDC precedence.
  - Required Tenants to reject production `Authority` with `RequireHttpsMetadata=false`, while preserving non-production symmetric-key development behavior.
  - Added guidance for narrow Tenants-side validation, explicit environment-scoped options/startup tests, and safe non-brittle error assertions.
- Findings deferred:
  - Whether shared EventStore validation should later encode the same production policy for all adopters.
  - Whether a future story should add an explicit production escape hatch for symmetric signing keys or insecure metadata.
  - Deployment examples, OIDC provider/AppHost environment variable documentation, tenant-claim contracts, and smoke tests remain scoped to Stories 11.2 and 11.3.
- Final recommendation: ready-for-dev after applied clarifications.

## Advanced Elicitation

- Date/time: 2026-05-18T18:52:13+02:00
- Selected story key: 11-1-production-jwt-configuration-validation
- Command/skill invocation used: `/bmad-advanced-elicitation 11-1-production-jwt-configuration-validation`
- Batch 1 method names: Red Team vs Blue Team; Failure Mode Analysis; Security Audit Personas; Self-Consistency Validation; First Principles Analysis.
- Reshuffled Batch 2 method names: Pre-mortem Analysis; Code Review Gauntlet; Comparative Analysis Matrix; Occam's Razor Application; Architecture Decision Records.
- Findings summary:
  - The story had the right production OIDC direction, but a validator could still treat whitespace placeholders as configured values if it relies only on empty-string checks.
  - Production readiness should fail before runtime OIDC discovery for malformed, relative, or non-HTTPS authorities; otherwise deployment can pass options validation and fail later in authentication middleware.
  - Tests should prove the Tenants-specific production validator composes with the shared EventStore validator under options validation rather than only validating a standalone class.
  - The narrowest architecture decision remains Tenants-side production safety rules registered beside the shared EventStore validator, without replacing `ConfigureJwtBearerOptions` or moving claim-contract/deployment scope into this story.
- Changes applied:
  - Added acceptance criteria for whitespace-only configuration values, absolute HTTPS production authority validation, and combined validator execution through `ValidateOnStart` or the options factory.
  - Tightened tasks to verify current EventStore whitespace behavior, reject malformed/non-HTTPS authorities without OIDC network discovery, and test composed validator registration.
  - Updated implementation guardrails so production-safe mode requires absolute HTTPS `Authority`, non-whitespace `Issuer` and `Audience`, empty or absent `SigningKey`, and `RequireHttpsMetadata=true`.
  - Added test guidance for whitespace placeholders, malformed authorities, production `RequireHttpsMetadata=false`, and preservation of non-production symmetric-key development behavior.
- Findings deferred:
  - Whether the shared EventStore validator should eventually switch from empty-string checks to whitespace-aware validation for all adopters remains a cross-repository policy decision.
  - Exact class name and registration shape for any Tenants-specific production validator remain implementation details.
  - Full deployment examples, identity provider setup, tenant-claim contract, and production smoke-test workflow remain scoped to Stories 11.2 and 11.3.
- Final recommendation: ready-for-dev after applied clarifications.

## Review Findings

Adversarial code review of commit `0d0f64a` ("Add validation for tenant production authentication options") completed on 2026-05-19 using parallel Blind Hunter, Edge Case Hunter, and Acceptance Auditor layers. Acceptance Auditor confirmed all 10 ACs are Met with no constraint violations (no replacement of `ConfigureJwtBearerOptions`, no EventStore submodule changes, no new packages, no infrastructure, no network OIDC discovery in tests). Tests pass per `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` (655 passed, 1 skipped).

- [x] [Review][Patch] Env-var test mutates process state without xUnit isolation — `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs:14` + `139-156` — applied 2026-05-19: added `AuthenticationConfigurationCollection` with `[CollectionDefinition(DisableParallelization = true)]` and `[Collection]` on `AuthenticationConfigurationTests` to serialize env-var-mutating tests against any future class that might enumerate or depend on process environment. Server.Tests gate passed: 438/438; AuthenticationConfigurationTests filter: 15/15.
- [x] [Review][Defer] `IValidateOptions.Validate(string? name, …)` does not gate on `name` — `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs:11` — deferred, consistent with shared `ValidateEventStoreAuthenticationOptions` which also ignores `name`. No AC requires named-options support; revisit only if a consumer registers a non-default-named `EventStoreAuthenticationOptions`.
- [x] [Review][Defer] `AddSingleton<IValidateOptions<EventStoreAuthenticationOptions>, ValidateTenantProductionAuthenticationOptions>()` should be `TryAddEnumerable(...)` — `src/Hexalith.Tenants/Program.cs:93` — deferred, current registration works because `OptionsBuilder` resolves `IEnumerable<IValidateOptions<T>>`. Idiomatic improvement only; would also tidy the adjacent shared `TryAddSingleton` registration.
- [x] [Review][Defer] `ValidateAuthority` has no loopback / private-IP / DNS-shape sanity beyond absolute HTTPS — `src/Hexalith.Tenants/Configuration/ValidateTenantProductionAuthenticationOptions.cs:34-44` — deferred, AC9 scopes Authority validation to "absolute HTTPS URI shape" before OIDC discovery. Deployment-time issuer trust policy belongs in Story 11.2 (tenant claim contract) and 11.3 (deployment auth readiness).
- [x] [Review][Defer] Composition test asserts the exact shared-validator wording `either 'Authority' (production OIDC) or 'SigningKey'` — `tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs:162` — deferred, the story dev encoded an explicit two-source proof rather than a loose count. If `Hexalith.EventStore`'s `ValidateEventStoreAuthenticationOptions` wording drifts, this test breaks first — that is the intended signal. Consider loosening to `exception.Failures.Count >= 2` + per-validator-unique substrings only if EventStore wording churn becomes a problem.

### Dismissed (false positives or by-design)

- `IsProduction()` is case-sensitive — false positive: `HostEnvironmentEnvExtensions.IsEnvironment` uses `StringComparison.OrdinalIgnoreCase`, so `production`, `PRODUCTION`, `Production` all match.
- `Uri.TryCreate("https:///", UriKind.Absolute, …)` accepts hostless URI — false positive: verified in PowerShell that `https:///`, `https://`, `https:///path` all return `ok=False`; `ValidateAuthority` already rejects them.
- `ValidateSigningKey` whitespace inconsistency (`null or { Length: 0 }` vs `IsNullOrWhiteSpace` used elsewhere) — by design: whitespace SigningKey is "present, not empty" → must be rejected as ambiguous (AC6); switching to `IsNullOrWhiteSpace` would silently accept `SigningKey="   "` alongside a valid Authority, which is the exact scenario AC6 forbids. The asymmetric semantics are correct.
- Tautological `ShouldNotContain(SecretSigningKey)` / `ShouldNotContain(new string(' ', 40))` / `ShouldNotContain(authority)` assertions — defense-in-depth; the validator never interpolates field values into messages, but these assertions catch a regression if someone ever does.
- `ProductionAppSettingsAuthenticationShouldFailValidation` couples to committed `appsettings.json` content — by design: AC1 is specifically "committed production placeholders fail validation," so the test must read the committed file.
- `appsettings.Development.json` loaded with `optional: false` — by design: the test csproj `<Content Include …>` copy guarantees the file exists at test runtime.
- Most tests use `IOptions<T>.Value` rather than `IStartupValidator.Validate()` — composition AC10 is covered by the dedicated `StartupValidationShouldComposeEventStoreAndTenantsValidators` test; other tests intentionally focus on per-validator behavior.
- `InternalsVisibleTo` wiring — verified at `src/Hexalith.Tenants/Hexalith.Tenants.csproj:17`: `<InternalsVisibleTo Include="Hexalith.Tenants.Server.Tests" />`.
- `ServiceProvider` not disposed in tests — fixture holds only singleton `IHostEnvironment` and `IOptions` cache; no unmanaged resources to leak.
- `Environment.SetEnvironmentVariable(name, "")` deletes on Windows — handled: the relevant test overrides empty/missing values via configuration overrides, not via empty env strings.
- Other minor noise: bind-time exception for non-boolean `RequireHttpsMetadata`, `IConfiguration` reload-token concerns, Issuer-vs-Authority cross-field validation, test working-directory edge cases — none manifests in this codebase.
