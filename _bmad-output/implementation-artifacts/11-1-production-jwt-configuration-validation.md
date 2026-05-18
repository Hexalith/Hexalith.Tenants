# Story 11.1: Production JWT Configuration Validation

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want production JWT configuration to be validated before deployment,
so that Hexalith.Tenants does not fail later at runtime or accept unsafe authentication settings unexpectedly.

## Acceptance Criteria

1. Given production `appsettings.json` contains empty JWT `Authority` and `SigningKey` placeholders, when Hexalith.Tenants starts without AppHost, environment, user-secret, or deployment overrides, then startup fails with a clear authentication configuration validation error.
2. Given production JWT settings are supplied through environment variables, AppHost, or deployment configuration, when the service starts, then `EventStoreAuthenticationOptions` validation succeeds without requiring secrets in committed appsettings files.
3. Given development mode uses symmetric-key JWT validation, when `appsettings.Development.json` or equivalent local overrides are loaded, then development authentication remains usable without weakening production validation.
4. Given authentication configuration fails validation, when logs or exception messages are emitted, then they identify the missing configuration key or invalid setting without exposing signing keys, tokens, or bearer material.
5. Given focused configuration tests run, when production-valid, production-invalid, and development-valid configurations are bound, then tests verify startup/options validation behavior for each mode.
6. Given a production deployment accidentally supplies both `Authority` and `SigningKey`, when validation runs, then the behavior is explicit and tested: either reject the ambiguous configuration in Hexalith.Tenants or document that EventStore authority/OIDC mode takes precedence and no committed or logged secret value is exposed.
7. Given `RequireHttpsMetadata` is disabled, when the host environment is production and `Authority` is configured, then validation either rejects the setting or records an explicit approved local-only exception; production-ready tests must not silently accept non-HTTPS metadata discovery.

## Tasks / Subtasks

- [ ] Confirm the existing EventStore authentication validation contract before changing code. (AC: 1-7)
  - [ ] Read `EventStoreAuthenticationOptions`, `ValidateEventStoreAuthenticationOptions`, and `ConfigureJwtBearerOptions` from the current `Hexalith.EventStore` submodule commit.
  - [ ] Verify the current rules: `Issuer` and `Audience` are required, either `Authority` or `SigningKey` is required, and `SigningKey` must be at least 32 characters when present.
  - [ ] Confirm whether the current EventStore validator permits both `Authority` and `SigningKey`, and whether it has any environment-aware rule for production `RequireHttpsMetadata`.
  - [ ] Do not modify `Hexalith.EventStore` or update nested submodules for this story. If the correct fix belongs in EventStore, record the blocker instead of duplicating or forking the shared validator.
- [ ] Add focused Tenants-side validation coverage for the current startup wiring. (AC: 1, 2, 5)
  - [ ] Add tests that bind the committed production `Authentication:JwtBearer` section from `src/Hexalith.Tenants/appsettings.json` and assert validation fails because both `Authority` and `SigningKey` are empty.
  - [ ] Add tests that supply production-style OIDC overrides (`Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata=true`, empty or absent `SigningKey`) and assert validation succeeds.
  - [ ] Assert the validation failure mentions `Authentication:JwtBearer` and the missing `Authority`/`SigningKey` requirement without including token values or secret-like payloads.
  - [ ] Prefer direct `IOptions<EventStoreAuthenticationOptions>` / `IValidateOptions<EventStoreAuthenticationOptions>` tests when they prove the binding contract; use a host/WebApplicationFactory startup test only if direct options tests cannot prove `ValidateOnStart` integration.
- [ ] Preserve local development symmetric-key behavior. (AC: 3, 5)
  - [ ] Add or extend tests that bind `src/Hexalith.Tenants/appsettings.Development.json` and assert `Issuer`, `Audience`, `SigningKey`, and `RequireHttpsMetadata=false` remain valid for local development.
  - [ ] Keep the existing integration-test JWT constants aligned with the development settings; do not copy production secrets into tests.
  - [ ] If any dev-mode startup validation becomes stricter, prove existing JWT integration tests still exercise valid issuer, audience, expiry, and signature behavior.
- [ ] Decide and encode ambiguous production safety rules at the Tenants boundary. (AC: 4, 6, 7)
  - [ ] If EventStore already rejects ambiguous or unsafe production settings by the time this story is implemented, add Tenants tests that lock that behavior.
  - [ ] If EventStore does not reject `Authority` plus `SigningKey`, decide the least invasive Tenants-side behavior: fail in production with a safe message, or explicitly document/test authority precedence without logging the signing key.
  - [ ] If EventStore does not reject `RequireHttpsMetadata=false` with `Authority` in production, decide whether Tenants should add environment-aware validation or defer to EventStore; do not silently accept production OIDC over non-HTTPS metadata.
  - [ ] Keep any Tenants-specific validation narrow and registered beside the existing options setup in `Program.cs`; do not replace `ConfigureJwtBearerOptions`.
- [ ] Keep committed configuration and deployment override boundaries explicit. (AC: 1, 2, 4)
  - [ ] Keep `src/Hexalith.Tenants/appsettings.json` free of `SigningKey` secrets and real OIDC endpoints unless a deliberate non-secret placeholder is documented.
  - [ ] Keep `src/Hexalith.Tenants/appsettings.Development.json` development-only; do not make production depend on the development signing key.
  - [ ] Verify standard .NET configuration precedence can satisfy the production settings through environment variables using keys such as `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, and `Authentication__JwtBearer__Audience`.
  - [ ] Do not add a new secret store integration, Keycloak setup, Entra app registration, deployment manifest, or smoke-test workflow; Stories 11.2 and 11.3 own claim-contract and deployment-readiness documentation.
- [ ] Add or update tests without introducing infrastructure dependencies. (AC: 1-7)
  - [ ] Place focused tests under `tests/Hexalith.Tenants.Server.Tests/Configuration/` unless an existing integration-test fixture is clearly required.
  - [ ] Use xUnit v3 and Shouldly, matching existing configuration tests.
  - [ ] Do not require DAPR sidecars, Redis, Aspire orchestration, Keycloak, Entra ID, network OIDC discovery, or real tokens to test configuration binding and validation.
  - [ ] Add regression assertions that validation errors do not echo configured signing key values.

## Dev Notes

### Current Code State

- `src/Hexalith.Tenants/Program.cs` binds `Authentication:JwtBearer` to `EventStoreAuthenticationOptions`, calls `ValidateOnStart()`, registers `ValidateEventStoreAuthenticationOptions`, registers `ConfigureJwtBearerOptions`, and enables default JWT bearer authentication plus authorization. This story should harden or test that wiring rather than creating a parallel authentication stack. [Source: `src/Hexalith.Tenants/Program.cs`]
- Committed production `src/Hexalith.Tenants/appsettings.json` currently sets `Authentication:JwtBearer:Authority` to an empty string, `Audience` to `hexalith-tenants`, `Issuer` to `hexalith`, `SigningKey` to an empty string, and `RequireHttpsMetadata` to `true`. Without overrides, this should fail validation because neither OIDC authority nor symmetric signing key is configured. [Source: `src/Hexalith.Tenants/appsettings.json`]
- Committed development `src/Hexalith.Tenants/appsettings.Development.json` sets `Issuer` to `hexalith-dev`, `Audience` to `hexalith-tenants`, a test-only symmetric signing key, and `RequireHttpsMetadata=false`. Preserve this local development path. [Source: `src/Hexalith.Tenants/appsettings.Development.json`]
- Existing query integration tests create JWTs with issuer `hexalith-dev`, audience `hexalith-tenants`, and the development signing key, then assert missing, invalid signature, wrong issuer, wrong audience, and expired token behavior. Do not weaken these tests while adding config validation. [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- Existing configuration tests show the repository pattern for loading appsettings and validating options with xUnit and Shouldly. Follow that style for auth configuration tests. [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs`]

### EventStore Authentication Contract

- `EventStoreAuthenticationOptions` is owned by the EventStore submodule and exposes `Authority`, `Audience`, `Issuer`, `SigningKey`, and `RequireHttpsMetadata`. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreAuthenticationOptions.cs`]
- `ValidateEventStoreAuthenticationOptions` currently requires either `Authority` or `SigningKey`, always requires `Issuer` and `Audience`, and requires `SigningKey.Length >= 32` when a signing key is present. It does not currently encode a Tenants-specific production environment policy in the source reviewed for this story. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreAuthenticationOptions.cs`]
- `ConfigureJwtBearerOptions` preserves original JWT claim names (`MapInboundClaims=false`), validates issuer, audience, signing key, and lifetime with one-minute clock skew, uses OIDC discovery when `Authority` is set, and falls back to symmetric key mode when only `SigningKey` is set. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs`]
- EventStore documentation says OIDC discovery is the recommended production mode; symmetric key mode is for development/testing; `Issuer` and `Audience` are always required; and if both `Authority` and `SigningKey` are present, the runtime takes the OIDC path, so signing keys should be cleared when switching to production OIDC. [Source: `Hexalith.EventStore/docs/guides/security-model.md#Layer 1: JWT Authentication`; `Hexalith.EventStore/docs/guides/configuration-reference.md#Authentication and JWT`]

### Architecture and Scope Boundaries

- Epic 11 is Production Authorization Readiness. Story 11.1 is limited to production JWT configuration validation; Story 11.2 owns the `eventstore:tenant` claim contract; Story 11.3 owns deployment documentation and smoke tests. Do not pull those later story scopes into this one. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 11: Production Authorization Readiness`]
- The architecture defines two authorization layers: EventStore JWT-based API authorization and Tenants domain RBAC inside aggregate Handle methods. This story hardens the first layer's configuration readiness only; do not change domain RBAC semantics. [Source: `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]
- The `system` tenant remains a deployment prerequisite in EventStore domain registration and identity provider JWT claims. This story may mention the deployment prerequisite but should not define the final claim mapping contract; Story 11.2 owns that. [Source: `_bmad-output/planning-artifacts/architecture.md#Identity Mapping (ADR)`; `_bmad-output/planning-artifacts/epics.md#Story 11.2: EventStore Tenant Claim Contract`]
- Do not add package dependencies or central package versions. The current relevant package pins are Microsoft ASP.NET Core packages `10.0.8`, Microsoft.Extensions.Configuration.Binder `10.0.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and Microsoft.NET.Test.Sdk `18.5.1`. [Source: `Directory.Packages.props`]

### Implementation Guardrails

- Reuse the EventStore validator and JWT options configurator wherever possible. Add Tenants-specific validation only for Tenants-specific production safety rules that EventStore intentionally does not own.
- Keep validation failure messages specific enough to identify keys such as `Authentication:JwtBearer:Authority`, `Authentication:JwtBearer:SigningKey`, `Authentication:JwtBearer:Issuer`, and `Authentication:JwtBearer:Audience`, but never include the configured signing key or bearer token value.
- Do not perform network OIDC metadata discovery in unit tests. Configuration validation should prove binding and option validation, not identity-provider availability.
- Do not change controller authorization policies, query routes, command routes, JWT claim names, RBAC role hierarchy, tenant visibility, rate-limit partitioning, cursor behavior, or DAPR component configuration.
- If a WebApplicationFactory startup test is added, override unrelated DAPR/EventStore dependencies narrowly so the test proves auth configuration startup validation and not infrastructure availability.

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

### Completion Notes List

### File List
