# Story 11.2: EventStore Tenant Claim Contract

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want the production identity provider to emit the tenant claim expected by EventStore infrastructure,
so that authenticated requests are partitioned and authorized consistently instead of falling into a shared anonymous bucket.

## Acceptance Criteria

1. Given Hexalith.Tenants is deployed with a production IdP, when an authenticated token is issued, then the token includes the configured `eventstore:tenant` claim value required by EventStore tenant validation and rate-limit partitioning.
2. Given an authenticated non-global-admin token is missing or has a blank `eventstore:tenant` claim after EventStore claims transformation, when a tenant-scoped request reaches authorization, then tenant access is denied explicitly rather than silently sharing an `"anonymous"` rate-limit bucket.
3. Given test JWTs and `TestAuthHandler` assert tenant claim behavior, when production claim-contract tests are reviewed, then test assumptions match the documented IdP claim contract.
4. Given a deployment operator configures Keycloak or another IdP, when they follow the deployment documentation, then the required claim mapping is documented with the exact claim name, value expectations, and verification steps.
5. Given focused auth tests run, when tokens include, omit, blank, alias, or vary the tenant claim, then tests verify the selected production authorization behavior and rate-limit partitioning assumptions only where the Tenants host actually registers rate limiting.

## Claim Contract Clarifications

- The production-facing downstream claim consumed by EventStore tenant authorization is `eventstore:tenant`; tenant-management commands must include the platform tenant value `system` in the effective principal.
- An IdP may emit `eventstore:tenant` directly, or it may emit a supported source claim that `EventStoreClaimsTransformation` normalizes into `eventstore:tenant`: `tenants`, `tenant_id`, or `tid`. Documentation and tests must label source claims separately from the normalized downstream claim.
- The local quickstart `tenants: ["system"]` example is a source-claim example. The AppHost Keycloak sample's `eventstore:tenant` mapper is direct-claim evidence. Do not present either example as the only valid production IdP shape unless implementation confirms that policy decision.
- Authorization and rate limiting are separate layers: missing or blank tenant claims should fail closed for non-global-admin tenant-scoped authorization, while an `"anonymous"` rate-limit partition is only a rate-limit fallback where rate limiting is registered and reached before or outside tenant validation.
- Global-administrator bypass behavior must not mask the tenant claim contract. Negative missing/blank/wrong-tenant tests must use non-global-admin principals; any global-admin exception needs separate, explicit coverage or a documented deferral.

## Tasks / Subtasks

- [ ] Confirm the current EventStore claim normalization and enforcement contract before changing Tenants code. (AC: 1-5)
  - [ ] Read `EventStoreClaimsTransformation`, `ClaimsTenantValidator`, `ClaimsRbacValidator`, `AuthorizationBehavior`, `CommandsController`, and the rate-limiting setup in the current `Hexalith.EventStore` submodule commit.
  - [ ] Verify that `ConfigureJwtBearerOptions` keeps original JWT claim names with `MapInboundClaims=false`; do not rely on Microsoft claim-type remapping.
  - [ ] Verify the accepted source JWT claim shapes: existing `eventstore:tenant` claims, `tenants` JSON array or space-delimited values transformed into `eventstore:tenant`, and singular `tenant_id` or `tid` transformed into one `eventstore:tenant`.
  - [ ] Verify missing tenant claims currently deny command/query tenant authorization, while the rate limiter falls back to an `"anonymous"` tenant partition when it runs before denial or on endpoints without tenant claims.
  - [ ] Verify whether EventStore global-administrator claims bypass tenant validation for the path under test; do not let a global-admin bypass hide a missing tenant partition claim unless the story explicitly documents that production policy.
  - [ ] Do not modify `Hexalith.EventStore` or initialize/update nested submodules for this story. If the correct production policy belongs in EventStore, record the blocker instead of forking shared infrastructure behavior.
- [ ] Add focused Tenants tests that lock the accepted tenant claim contract. (AC: 1, 3, 5)
  - [ ] Add or extend tests that create valid JWT principals/tokens with `eventstore:tenant` = `system` and prove Tenants command/query paths or their authorization components accept the expected tenant claim.
  - [ ] Add tests for upstream `tenants` claim input if Tenants depends on EventStore claims transformation in production. Cover both JSON-array and space-delimited values when the test can exercise `EventStoreClaimsTransformation` directly without a full IdP.
  - [ ] Add tests for missing tenant claims that prove the chosen production behavior is explicit. Prefer fail-closed 403 behavior for protected tenant command/query paths unless an architecture decision deliberately documents another behavior.
  - [ ] Include a non-global-admin missing-tenant case and, if global-admin tokens are allowed to omit tenant claims, a separate global-admin case that proves the rate-limit/partitioning consequence is documented and intentional.
  - [ ] Add tests for wrong tenant claims that prove a token for another tenant cannot authorize `system` tenant management requests.
  - [ ] Keep assertions on safe response shape and reason codes where available; do not assert brittle full exception/log text.
- [ ] Make rate-limit partition behavior explicit and covered. (AC: 1, 2, 5)
  - [ ] Verify whether the Tenants host actually registers EventStore rate limiting in the current startup path; `Program.cs` currently registers EventStore client/domain services directly and may not call the full EventStore server `AddEventStore()` extension that wires the global limiter.
  - [ ] If Tenants registers EventStore rate limiting, add focused evidence that requests with `eventstore:tenant=system` use the tenant partition and requests without tenant claims do not silently merge authenticated production traffic into the shared `"anonymous"` bucket without an explicit deny/fallback decision.
  - [ ] If Tenants does not currently register the rate limiter, document that finding in this story's Dev Agent Record and production claim-contract docs, and defer executable rate-limit partition coverage to the EventStore host boundary or Story 11.3 smoke-test scope.
  - [ ] Do not reimplement EventStore rate limiting in Tenants as part of this story unless the existing host wiring already exposes a narrow missing registration defect.
- [ ] Document the production IdP claim mapping contract. (AC: 1, 4)
  - [ ] Add a focused production auth claim-contract section or page that states the required claim names and values for Tenants deployment: `eventstore:tenant` must include `system` for tenant management commands; `eventstore:domain` should include `tenants`; `eventstore:permission` must include the command/query permissions required by EventStore gateway authorization.
  - [ ] Name the concrete permissions used by existing samples and gateway checks, including `command:submit`, `command:query`, and `command:replay` where applicable.
  - [ ] Document the supported source claims that EventStore can normalize (`tenants`, `tenant_id`, `tid`) and distinguish them from the normalized downstream `eventstore:tenant` claim.
  - [ ] Document operator verification steps for decoding a token, confirming the emitted source or direct claim, confirming the normalized `eventstore:tenant` value in tests, and confirming that missing or blank non-global-admin tenant claims fail closed.
  - [ ] Update the existing quickstart only if needed to align local development token examples with the production contract; do not turn quickstart into a full deployment guide.
  - [ ] Use the existing Keycloak realm sample as implementation evidence, but do not commit real IdP endpoints, client secrets, or production user data.
  - [ ] Keep broader deployment walkthroughs, smoke-test workflow, and production environment readiness sequencing scoped to Story 11.3 unless the minimal claim-contract documentation needs a pointer for correctness.
- [ ] Keep test and fixture assumptions aligned. (AC: 3, 5)
  - [ ] Review `TenantsQueryControllerIntegrationTests`, `CommandApiRuntimeIntegrationTests`, and any `TestAuthHandler` usage for hard-coded `eventstore:tenant` assumptions.
  - [ ] Ensure test-only JWT creation uses `sub`, issuer, audience, and tenant claims that match `appsettings.Development.json` and the EventStore claim contract.
  - [ ] Use per-test token or principal variants for canonical, alias, missing, blank, wrong-tenant, and global-admin cases so shared fixture defaults cannot hide tenant-claim regressions.
  - [ ] If test handlers bypass claims transformation by directly issuing `eventstore:tenant`, add at least one direct test of `EventStoreClaimsTransformation` or document why that layer is already covered by EventStore tests and not duplicated in Tenants.
  - [ ] Do not weaken existing 401/403 tests for missing, invalid, wrong-issuer, wrong-audience, expired, or forbidden tokens.
- [ ] Add implementation notes for unresolved cross-repository policy decisions. (AC: 2, 4, 5)
  - [ ] If missing tenant claims are rejected before rate limiting can use `"anonymous"`, record that as the selected production behavior.
  - [ ] If any endpoint remains intentionally anonymous or shared-bucketed, document why it is safe, which endpoint owns it, and what monitoring/operator guidance applies.
  - [ ] If the AppHost Keycloak realm emits `eventstore:tenant` directly while EventStore docs recommend source claim `tenants`, reconcile the docs/tests so operators know which form is authoritative for production.

## Dev Notes

### Current Code State

- `src/Hexalith.Tenants/Program.cs` registers JWT bearer authentication and authorization directly, binds `Authentication:JwtBearer` to `EventStoreAuthenticationOptions`, registers `ValidateEventStoreAuthenticationOptions`, and registers `ConfigureJwtBearerOptions`. This story should build on that existing auth path rather than adding a separate authentication stack. [Source: `src/Hexalith.Tenants/Program.cs`]
- `Program.cs` registers `builder.Services.AddEventStore(typeof(TenantAggregate).Assembly)` from the EventStore client/domain registration path, not the full EventStore server `AddEventStore()` extension. Confirm whether the Tenants host currently wires EventStore claims transformation and rate limiting before assuming rate-limit behavior is active in this service. [Source: `src/Hexalith.Tenants/Program.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Extensions/ServiceCollectionExtensions.cs`]
- Existing Tenants query JWT tests create development tokens with `sub`, issuer `hexalith-dev`, audience `hexalith-tenants`, and `eventstore:tenant=system`; `TestAuthHandler` also issues `eventstore:tenant=system` directly. These tests prove downstream Tenants handlers can run with normalized claims but do not by themselves prove production IdP mapping or EventStore claims transformation. [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- `docs/quickstart.md` generates local development JWTs with a `tenants: ["system"]` source claim and states that the claim authorizes commands targeting the `system` tenant. This is compatible with EventStore transformation, but the story must make the distinction between source claim and normalized `eventstore:tenant` explicit for production operators. [Source: `docs/quickstart.md`]
- `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` currently maps Keycloak user attributes to `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission` claims, including sample users with `system`, `tenant-a`, and `tenant-b` tenant attributes. Treat this as local/AppHost evidence, not a production secret or final deployment guide. [Source: `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`]

### EventStore Claim and Authorization Contract

- `ConfigureJwtBearerOptions` sets `MapInboundClaims=false`, so JWT claim names remain in their original form. Implementations and tests must use exact claim names such as `sub`, `tenants`, and `eventstore:tenant`; do not rely on framework remapping. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs`]
- `EventStoreClaimsTransformation` normalizes JWT custom claims into EventStore authorization claims. It preserves existing `eventstore:*` claims, adds `ClaimTypes.NameIdentifier` from `sub`, transforms `tenants` into `eventstore:tenant`, supports singular `tenant_id` or `tid`, and transforms `domains` / `permissions` into `eventstore:domain` / `eventstore:permission`. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreClaimsTransformation.cs`]
- `ClaimsTenantValidator` denies blank requested tenants, denies principals with no non-empty `eventstore:tenant` claims, and performs case-sensitive tenant matching unless the principal is a global administrator. This is the fail-closed behavior to preserve for tenant management requests. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsTenantValidator.cs`]
- `GlobalAdministratorHelper` treats `global_admin=true`, `is_global_admin=true`, role values such as `GlobalAdministrator`, and `roles` arrays/delimited values containing global administrator aliases as global-admin evidence. Because global admins can bypass tenant matching, tests must distinguish role bypass behavior from the tenant claim contract used for partitioning. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/GlobalAdministratorHelper.cs`]
- EventStore's command gateway requires a valid `sub` claim and rejects missing subjects as unauthorized before dispatch. It stores `RequestTenantId` for downstream error handling and rate-limit rejection context when a command supplies a tenant. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`]
- EventStore rate limiting, where registered, partitions tenant traffic by the first `eventstore:tenant` claim and falls back to `"anonymous"` when no tenant claim is present. The rejection callback reports the tenant claim, `RequestTenantId`, or `"unknown"` for diagnostics. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Extensions/ServiceCollectionExtensions.cs`; `Hexalith.EventStore/docs/guides/security-model.md#Rate Limiting`]
- EventStore security documentation describes the authentication stack as JWT validation, claims transformation, endpoint authorization, MediatR authorization, actor tenant validation, and DAPR access control. The documented claim mapping accepts `tenants`, `tenant_id`, or `tid` as source claims and produces normalized `eventstore:tenant`. [Source: `Hexalith.EventStore/docs/guides/security-model.md#Layer 2: Claims Transformation`]

### Architecture and Scope Boundaries

- Epic 11 is Production Authorization Readiness. Story 11.1 owns production JWT configuration validation; Story 11.2 owns the tenant claim contract; Story 11.3 owns broader deployment auth readiness documentation and smoke tests. Keep this story focused on claim names, values, tests, and minimal docs needed to prevent ambiguous production tokens. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 11: Production Authorization Readiness`]
- The architecture defines two authorization layers: EventStore JWT/gateway authorization and Tenants domain RBAC inside aggregate Handle methods. This story is about the first layer's tenant claim contract; do not change TenantOwner/TenantContributor/TenantReader domain semantics. [Source: `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]
- The platform tenant context remains `system` for tenant management commands. Tokens that manage tenants must include authorization for `system` and must not be confused with the managed tenant ID in the command aggregate ID/payload. [Source: `_bmad-output/planning-artifacts/architecture.md#Identity Mapping (ADR)`; `docs/quickstart.md#About the-system-Tenant`]
- Do not add package dependencies or central package versions. The project is aligned to .NET 10 LTS and SDK `10.0.300`; keep story implementation on the existing package set and test framework. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-18-latest-dotnet.md`; `global.json`; `Directory.Packages.props`]

### Implementation Guardrails

- Prefer explicit fail-closed tenant authorization for protected Tenants command/query paths when `eventstore:tenant` is missing or does not include `system`.
- Keep claim names exact and case-sensitive. Tenant IDs are system-assigned identifiers and `ClaimsTenantValidator` uses ordinal matching.
- Define the focused test matrix before coding: canonical `eventstore:tenant=system`; omitted tenant; blank tenant; each supported alias as source input; canonical plus alias with same value; canonical plus alias conflict if EventStore behavior is observable; wrong tenant; non-global-admin missing tenant; and any global-admin missing-tenant exception that is intentionally supported.
- Do not add identity-provider network dependencies, Keycloak containers, Entra app registrations, or Aspire orchestration to unit tests for this story. Direct options, claims transformation, controller, or authorization-component tests are preferred.
- Do not log JWTs, signing keys, full bearer tokens, or raw claim payloads in new diagnostics. Safe messages may name the missing claim type or tenant ID under test.
- Do not modify EventStore source unless a separate cross-repository decision is made. This story may read the submodule and test against its public behavior from Tenants.
- Keep health/readiness endpoints out of scope unless rate-limit behavior for anonymous traffic requires an explicit note; they are intentionally unauthenticated in EventStore docs.
- If the chosen fix is documentation-only plus tests because production behavior is already correct, record that in the Dev Agent Record rather than adding unnecessary runtime code.

### Files Likely To Update

- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: likely extension point for JWT tenant-claim acceptance/missing/wrong-tenant behavior if controller-level coverage is enough.
- `tests/Hexalith.Tenants.Server.Tests/Authorization/` or `tests/Hexalith.Tenants.Server.Tests/Configuration/`: optional focused tests for claims transformation, tenant claim contract, and host wiring without DAPR or IdP dependencies.
- `docs/quickstart.md`: update only if the local token example needs clearer source-vs-normalized claim wording.
- `docs/production-auth-claim-contract.md` or a nearby auth documentation section: likely location for the exact production IdP claim mapping contract.
- `src/Hexalith.Tenants/Program.cs`: update only if implementation proves a narrow missing claims-transformation or rate-limit registration in the Tenants host.
- `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`: update only if the sample realm is inconsistent with the documented contract; do not add production secrets.

### Testing Requirements

- Run at minimum after implementation:
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`
- If focused server authorization/configuration tests are added, also run:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~Authorization`
- If startup/auth registration changes, also run:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`
- If documentation is the only production artifact changed, no docs build gate currently exists in this repository; record manual doc review in the Dev Agent Record.

### Previous Story Intelligence

- Story 11.1 deliberately left claim-contract and deployment-readiness documentation to Stories 11.2 and 11.3. Reuse its production JWT validation boundaries, but do not reopen Authority/SigningKey policy unless claim-contract implementation reveals a direct dependency. [Source: `_bmad-output/implementation-artifacts/11-1-production-jwt-configuration-validation.md`]
- Stories 9.3 through 9.5 hardened query visibility and cursor behavior. Tenant-claim tests must not weaken 401/403 boundaries, forbidden ProblemDetails behavior, invalid cursor responses, or no-leak query rejection bodies. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]
- Stories 10.1 through 10.4 are projection write-safety stories. This claim-contract story should leave active projection implementation/review changes untouched. [Source: `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T18:38:21Z`]

### Git Intelligence

- Recent history includes active Story 10.2 projection write work and automation bookkeeping. This story creation run began with an active-dev-story soft warning for dirty 10.2 story/source/test files; stage only this new 11.2 story, the sprint-status update for 11.2, and the pre-dev hardening run log. [Source: `git log -5 --oneline`; `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T18:38:21Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; EventStore submodule docs and source are reference context only for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

## Party-Mode Review

- Date/time: 2026-05-18T22:40:26+02:00
- Selected story key: 11-2-eventstore-tenant-claim-contract
- Command/skill invocation used: `/bmad-party-mode 11-2-eventstore-tenant-claim-contract; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Paige (Technical Writer)
- Findings summary:
  - Clarify authorization versus rate-limit behavior so the `"anonymous"` partition is not treated as an authorization fallback.
  - Condition rate-limit partition tests on verified Tenants host registration; otherwise document non-applicability and defer host-boundary coverage.
  - Name the downstream `eventstore:tenant` contract and distinguish direct IdP claims from source claims normalized by EventStore.
  - Tighten missing/blank/wrong-tenant/global-admin test expectations and avoid shared fixture defaults that hide claim regressions.
  - Add operator verification guidance without expanding into Story 11.1 JWT validation or Story 11.3 deployment smoke-test scope.
- Changes applied:
  - Added claim contract clarifications for source versus normalized claims, `system` platform tenant expectations, authorization/rate-limit layer boundaries, and global-admin caveats.
  - Tightened acceptance criteria for missing/blank non-global-admin claims and host-registered rate-limit evidence.
  - Added documentation and test-matrix guidance for token verification, per-test claim variants, aliases, wrong tenant, and global-admin exceptions.
- Findings deferred:
  - Whether production IdPs should standardize on direct `eventstore:tenant` only or continue documenting source-claim normalization as an allowed production path.
  - Whether Tenants should register full EventStore server rate-limiting middleware if implementation finds it absent.
  - End-to-end deployment smoke verification across JWT middleware, claims transformation, authorization, and rate limiting; keep in Story 11.3.
- Final recommendation: needs-story-update

### File List

- _bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md
