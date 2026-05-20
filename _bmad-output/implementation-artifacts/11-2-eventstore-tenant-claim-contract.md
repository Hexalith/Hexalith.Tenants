# Story 11.2: EventStore Tenant Claim Contract

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. Code review 2026-05-20 applied 15 patches (4 originally decision-needed, converted to patches per user decisions) and recorded 7 deferred items. Full Debug/no-restore solution test gate passes: 687 passed, 1 skipped (pre-existing performance test).

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
- Mixed direct and source tenant claims must be tested against the actual EventStore transformation behavior. Current `EventStoreClaimsTransformation` treats existing `eventstore:*` claims as already normalized and does not merge alias/source tenant claims into the same principal; documentation must not imply aliases override, supplement, or repair an already-present downstream `eventstore:tenant` claim.
- Authorization and rate limiting are separate layers: missing or blank tenant claims should fail closed for non-global-admin tenant-scoped authorization, while an `"anonymous"` rate-limit partition is only a rate-limit fallback where rate limiting is registered and reached before or outside tenant validation.
- Global-administrator bypass behavior must not mask the tenant claim contract. Negative missing/blank/wrong-tenant tests must use non-global-admin principals; any global-admin exception needs separate, explicit coverage or a documented deferral.

## Tasks / Subtasks

- [x] Confirm the current EventStore claim normalization and enforcement contract before changing Tenants code. (AC: 1-5)
  - [x] Read `EventStoreClaimsTransformation`, `ClaimsTenantValidator`, `ClaimsRbacValidator`, `AuthorizationBehavior`, `CommandsController`, and the rate-limiting setup in the current `Hexalith.EventStore` submodule commit.
  - [x] Verify that `ConfigureJwtBearerOptions` keeps original JWT claim names with `MapInboundClaims=false`; do not rely on Microsoft claim-type remapping.
  - [x] Verify the accepted source JWT claim shapes: existing `eventstore:tenant` claims, `tenants` JSON array or space-delimited values transformed into `eventstore:tenant`, and singular `tenant_id` or `tid` transformed into one `eventstore:tenant`.
  - [x] Verify transformation precedence and idempotency for mixed direct/source tokens: direct `eventstore:*` claims, direct plus matching aliases, direct plus conflicting aliases, and alias-only tokens must have documented effective downstream `eventstore:tenant` values.
  - [x] Verify missing tenant claims currently deny command/query tenant authorization, while the rate limiter falls back to an `"anonymous"` tenant partition when it runs before denial or on endpoints without tenant claims.
  - [x] Verify whether EventStore global-administrator claims bypass tenant validation for the path under test; do not let a global-admin bypass hide a missing tenant partition claim unless the story explicitly documents that production policy.
  - [x] Do not modify `Hexalith.EventStore` or initialize/update nested submodules for this story. If the correct production policy belongs in EventStore, record the blocker instead of forking shared infrastructure behavior.
- [x] Add focused Tenants tests that lock the accepted tenant claim contract. (AC: 1, 3, 5)
  - [x] Add or extend tests that create valid JWT principals/tokens with `eventstore:tenant` = `system` and prove Tenants command/query paths or their authorization components accept the expected tenant claim.
  - [x] Add tests for upstream `tenants` claim input if Tenants depends on EventStore claims transformation in production. Cover both JSON-array and space-delimited values when the test can exercise `EventStoreClaimsTransformation` directly without a full IdP.
  - [x] Add transformation-focused cases for whitespace-only tenant values, empty JSON-array entries, duplicate tenant values, multiple source claim types, and mixed direct/source claims so the effective principal contract is explicit rather than inferred from fixture defaults.
  - [x] Add tests for missing tenant claims that prove the chosen production behavior is explicit. Prefer fail-closed 403 behavior for protected tenant command/query paths unless an architecture decision deliberately documents another behavior.
  - [x] Include a non-global-admin missing-tenant case and, if global-admin tokens are allowed to omit tenant claims, a separate global-admin case that proves the rate-limit/partitioning consequence is documented and intentional.
  - [x] Add tests for wrong tenant claims that prove a token for another tenant cannot authorize `system` tenant management requests.
  - [x] Keep assertions on safe response shape and reason codes where available; do not assert brittle full exception/log text.
- [x] Make rate-limit partition behavior explicit and covered. (AC: 1, 2, 5)
  - [x] Verify whether the Tenants host actually registers EventStore rate limiting in the current startup path; `Program.cs` currently registers EventStore client/domain services directly and may not call the full EventStore server `AddEventStore()` extension that wires the global limiter.
  - [x] If Tenants registers EventStore rate limiting, add focused evidence that requests with `eventstore:tenant=system` use the tenant partition and requests without tenant claims do not silently merge authenticated production traffic into the shared `"anonymous"` bucket without an explicit deny/fallback decision.
  - [x] If Tenants does not currently register the rate limiter, document that finding in this story's Dev Agent Record and production claim-contract docs, and defer executable rate-limit partition coverage to the EventStore host boundary or Story 11.3 smoke-test scope.
  - [x] Treat the current `Program.cs` "domain service only" comment as an architectural guardrail: do not register the full EventStore server extension merely to satisfy a rate-limit test unless implementation uncovers an approved hosting-policy change.
  - [x] Do not reimplement EventStore rate limiting in Tenants as part of this story unless the existing host wiring already exposes a narrow missing registration defect.
- [x] Document the production IdP claim mapping contract. (AC: 1, 4)
  - [x] Add a focused production auth claim-contract section or page that states the required claim names and values for Tenants deployment: `eventstore:tenant` must include `system` for tenant management commands; `eventstore:domain` should include `tenants`; `eventstore:permission` must include the command/query permissions required by EventStore gateway authorization.
  - [x] Name the concrete permissions used by existing samples and gateway checks, including `command:submit`, `command:query`, and `command:replay` where applicable.
  - [x] Document the supported source claims that EventStore can normalize (`tenants`, `tenant_id`, `tid`) and distinguish them from the normalized downstream `eventstore:tenant` claim.
  - [x] Document mixed-claim behavior: operators should prefer one authoritative mapping style, and verification must inspect the effective downstream principal after transformation rather than assuming raw JWT aliases are merged with direct `eventstore:*` claims.
  - [x] Document operator verification steps for decoding a token, confirming the emitted source or direct claim, confirming the normalized `eventstore:tenant` value in tests, and confirming that missing or blank non-global-admin tenant claims fail closed.
  - [x] Update the existing quickstart only if needed to align local development token examples with the production contract; do not turn quickstart into a full deployment guide.
  - [x] Use the existing Keycloak realm sample as implementation evidence, but do not commit real IdP endpoints, client secrets, or production user data.
  - [x] Keep broader deployment walkthroughs, smoke-test workflow, and production environment readiness sequencing scoped to Story 11.3 unless the minimal claim-contract documentation needs a pointer for correctness.
- [x] Keep test and fixture assumptions aligned. (AC: 3, 5)
  - [x] Review `TenantsQueryControllerIntegrationTests`, `CommandApiRuntimeIntegrationTests`, and any `TestAuthHandler` usage for hard-coded `eventstore:tenant` assumptions.
  - [x] Ensure test-only JWT creation uses `sub`, issuer, audience, and tenant claims that match `appsettings.Development.json` and the EventStore claim contract.
  - [x] Use per-test token or principal variants for canonical, alias, missing, blank, wrong-tenant, and global-admin cases so shared fixture defaults cannot hide tenant-claim regressions.
  - [x] If test handlers bypass claims transformation by directly issuing `eventstore:tenant`, add at least one direct test of `EventStoreClaimsTransformation` or document why that layer is already covered by EventStore tests and not duplicated in Tenants.
  - [x] Do not weaken existing 401/403 tests for missing, invalid, wrong-issuer, wrong-audience, expired, or forbidden tokens.
- [x] Add implementation notes for unresolved cross-repository policy decisions. (AC: 2, 4, 5)
  - [x] If missing tenant claims are rejected before rate limiting can use `"anonymous"`, record that as the selected production behavior.
  - [x] If any endpoint remains intentionally anonymous or shared-bucketed, document why it is safe, which endpoint owns it, and what monitoring/operator guidance applies.
  - [x] If the AppHost Keycloak realm emits `eventstore:tenant` directly while EventStore docs recommend source claim `tenants`, reconcile the docs/tests so operators know which form is authoritative for production.

## Dev Notes

### Current Code State

- `src/Hexalith.Tenants/Program.cs` registers JWT bearer authentication and authorization directly, binds `Authentication:JwtBearer` to `EventStoreAuthenticationOptions`, registers `ValidateEventStoreAuthenticationOptions`, and registers `ConfigureJwtBearerOptions`. This story should build on that existing auth path rather than adding a separate authentication stack. [Source: `src/Hexalith.Tenants/Program.cs`]
- `Program.cs` registers `builder.Services.AddEventStore(typeof(TenantAggregate).Assembly)` from the EventStore client/domain registration path, not the full EventStore server `AddEventStore()` extension. Confirm whether the Tenants host currently wires EventStore claims transformation and rate limiting before assuming rate-limit behavior is active in this service. [Source: `src/Hexalith.Tenants/Program.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Extensions/ServiceCollectionExtensions.cs`]
- The current Tenants host explicitly documents "Domain service only" registration and avoids the EventStore server extension so AggregateActor/ETagActor are hosted by EventStore rather than domain services. If rate limiting is absent in Tenants, that is likely a scope boundary to document, not automatically a defect to patch in this story. [Source: `src/Hexalith.Tenants/Program.cs`]
- Existing Tenants query JWT tests create development tokens with `sub`, issuer `hexalith-dev`, audience `hexalith-tenants`, and `eventstore:tenant=system`; `TestAuthHandler` also issues `eventstore:tenant=system` directly. These tests prove downstream Tenants handlers can run with normalized claims but do not by themselves prove production IdP mapping or EventStore claims transformation. [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`]
- `docs/quickstart.md` generates local development JWTs with a `tenants: ["system"]` source claim and states that the claim authorizes commands targeting the `system` tenant. This is compatible with EventStore transformation, but the story must make the distinction between source claim and normalized `eventstore:tenant` explicit for production operators. [Source: `docs/quickstart.md`]
- `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` currently maps Keycloak user attributes to `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission` claims, including sample users with `system`, `tenant-a`, and `tenant-b` tenant attributes. Treat this as local/AppHost evidence, not a production secret or final deployment guide. [Source: `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`]

### EventStore Claim and Authorization Contract

- `ConfigureJwtBearerOptions` sets `MapInboundClaims=false`, so JWT claim names remain in their original form. Implementations and tests must use exact claim names such as `sub`, `tenants`, and `eventstore:tenant`; do not rely on framework remapping. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs`]
- `EventStoreClaimsTransformation` normalizes JWT custom claims into EventStore authorization claims. It preserves existing `eventstore:*` claims, adds `ClaimTypes.NameIdentifier` from `sub`, transforms `tenants` into `eventstore:tenant`, supports singular `tenant_id` or `tid`, and transforms `domains` / `permissions` into `eventstore:domain` / `eventstore:permission`. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreClaimsTransformation.cs`]
- `EventStoreClaimsTransformation` is idempotent for already-normalized principals: when `eventstore:*` claims exist, alias/source tenant claims are not merged into new `eventstore:tenant` claims. Mixed direct/source tokens need explicit test evidence because "raw token contains alias" and "effective downstream principal contains tenant" are different assertions. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreClaimsTransformation.cs`]
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
- For alias tests, assert both raw-token input and post-transformation effective claims. A direct `eventstore:tenant` claim should be treated as the normalized contract; source aliases should only prove normalization when no `eventstore:*` claim short-circuits transformation.
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

- 2026-05-19: Red test pass setup - first parallel focused run hit Windows build file locks on EventStore/Tenants obj DLLs, then reran sequentially.
- 2026-05-19: `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantClaimContractTests` - passed, 11/11.
- 2026-05-19: Red integration run before host wiring - `dotnet test tests\Hexalith.Tenants.IntegrationTests\Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"` failed as expected on missing Tenants `EventStoreClaimsTransformation` registration and missing/blank/wrong tenant 403 behavior.
- 2026-05-19: Green focused integration run after host wiring and JWT fixture correction - same integration filter passed, 31/31.
- 2026-05-19: `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 0 warnings, 0 errors.
- 2026-05-19: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 682 passed, 1 skipped.

### Completion Notes List

- Verified EventStore claim behavior from the current submodule without modifying EventStore: `MapInboundClaims=false`; `tenants`, `tenant_id`, and `tid` normalize only when no `eventstore:*` claim is already present; direct/blank downstream `eventstore:tenant` is authoritative; non-global-admin missing/blank/wrong tenants fail closed through `ClaimsTenantValidator`.
- Added narrow Tenants host authorization wiring: `EventStoreClaimsTransformation`, claims-based `ITenantValidator`/`IRbacValidator`, `AuthorizationBehavior<,>`, `IHttpContextAccessor`, and EventStore authorization exception handlers. Did not register the full EventStore server extension or EventStore rate limiter.
- Added focused Tenants claim-contract tests for direct `eventstore:tenant`, JSON-array and space-delimited `tenants`, `tenant_id`, `tid`, duplicates, whitespace-only values, mixed direct/source tokens, missing tenants, blank direct claims, wrong tenants, and fixture paths that use real JwtBearer authentication.
- Documented the production IdP claim contract in `docs/production-auth-claim-contract.md`, clarified quickstart source-claim normalization, linked the doc from README, and recorded that executable rate-limit partition coverage belongs to the EventStore host boundary or Story 11.3.

### Change Log

- 2026-05-19: Implemented EventStore tenant claim contract enforcement and documentation; full Debug/no-restore solution gate passed; story -> review.
- 2026-05-20: Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) applied 15 patches (4 decisions resolved + 11 patches) and recorded 7 deferred items. Patches: production-auth-claim-contract.md rewritten with mixed-source precedence section, direct-claim Keycloak shape as authoritative, rate-limit non-applicability callout, split command/query permission rows, removed `command:replay`; added `TenantClaimContractTests` global-admin missing-tenant + non-global-admin missing-tenant + empty-string blank-direct claim variant; added integration tests for `IClaimsTransformation` lifetime, host-effective `MapInboundClaims=false`, triangulated cross-tenant theory; flipped `CommandApiWebApplicationFactory.useTestAuthentication` default to `false`; strengthened `reasonCode` assertions to use `ShouldContainKey`; replaced `Guid.NewGuid()` with `UniqueIdHelper.GenerateSortableUniqueStringId()` for `MessageId`. Full Debug/no-restore solution gate: 687 passed, 1 skipped (+5 net new tests, 0 regressions); story -> done.

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

## Review Findings

_Code review 2026-05-20 — Blind Hunter + Edge Case Hunter + Acceptance Auditor adversarial layers, commit `2aaade8`._

### Decisions Resolved (2026-05-20)

- [x] [Review][Decision] Global-admin missing-tenant negative case → resolved as **best-practice / add coverage**. Converted to patch P12. Add a global-admin missing-tenant test that sets `global_admin=true` (or `roles=GlobalAdministrator`) with no `eventstore:tenant` claim, assert the host-actual behavior, and document the operator-visible consequence.
- [x] [Review][Decision] Keycloak Mapping doc vs sample realm inconsistency → resolved as **rewrite doc to match realm**. Converted to patch P13. Update `docs/production-auth-claim-contract.md` to describe the realm's direct-claim mapping (`eventstore:*` emitted directly by Keycloak attribute mappers) as authoritative; note source claims as alternative for IdPs that cannot emit `eventstore:*` directly.
- [x] [Review][Decision] Rate-limit section in production Tenants claim contract → resolved as **add 'not enforced by Tenants' callout**. Converted to patch P14. Lead the rate-limit subsection with an explicit statement that the Tenants host does not register the EventStore rate limiter today and the partitioning content describes the EventStore host boundary only.
- [x] [Review][Decision] `useTestAuthentication=true` default → resolved as **flip default to false (strict)**. Converted to patch P15. Change the default in `CommandApiWebApplicationFactory` and migrate any existing test that relied on the TestAuth principal to opt in explicitly.

### Patches (Action Items)

- [x] [Review][Patch] Doc fix: `tenants` space-delimited row wording [docs/production-auth-claim-contract.md] — Current row says "One `eventstore:tenant` claim per token". Actual behavior (verified by `TenantsSpaceDelimitedSourceClaimNormalizesToEventStoreTenantClaims`) is one downstream claim per non-empty space-delimited part. Replace "per token" with "per non-empty part".
- [x] [Review][Patch] Remove `command:replay` from Tenants permissions row [docs/production-auth-claim-contract.md] — Replay is exposed by the EventStore Admin host, not the Tenants host. Listing it in the Tenants production contract misleads operators into provisioning excess permissions on Tenants tokens.
- [x] [Review][Patch] Doc fix: clarify multi-source claim precedence [docs/production-auth-claim-contract.md] — Add explicit note that (a) when both `tenants` (array/space-delimited) and `tenant_id`/`tid` are present, both contribute claims (`tenants` first, then `tenant_id`-or-`tid`); (b) when both `tenant_id` and `tid` are present, only `tenant_id` is used and `tid` is silently dropped; (c) recommend operators emit exactly one source-claim shape per token to avoid surprising multi-tenant principals. This is the documented effective-principal contract the spec requires (Tasks lines 41-44).
- [x] [Review][Patch] Doc fix: whitespace-only tenant claim wording [docs/production-auth-claim-contract.md] — Current row claims "Whitespace-only tenant values are ignored by tenant authorization and fail closed". The transformation does NOT filter whitespace at normalization — the whitespace claim survives on the principal and `ClaimsTenantValidator` rejects it later. Rewrite to: "Whitespace-only tenant claims survive normalization and remain on the principal, but are rejected by tenant authorization (fail-closed). Operators inspecting the effective principal will see the literal whitespace claim until validation runs."
- [x] [Review][Patch] Strengthen `reasonCode` assertions [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:189,217,239; tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs] — Replace `details.Extensions["reasonCode"]?.ToString().ShouldBe(...)` with `details.Extensions.ShouldContainKey("reasonCode"); details.Extensions["reasonCode"]?.ToString().ShouldBe(...)`. The current `IDictionary<string, object?>` indexer throws `KeyNotFoundException` if the extension is dropped, producing a confusing failure instead of a clear Shouldly mismatch.
- [x] [Review][Patch] Use ULID for `MessageId` in `CreateBootstrapRequest` [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:399-407] — Replace `Guid.NewGuid().ToString()` with `Ulid.NewUlid().ToString()`. Repository rule R2-A7 (CLAUDE.md) forbids `Guid` shape for `messageId`/`correlationId`/`aggregateId`/`causationId`.
- [x] [Review][Patch] Add empty-string `""` direct-claim variant to idempotency test [tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs] — `BlankDirectEventStoreTenantClaimIsNotRepairedBySourceClaims` only tests `" "` (single space). Add a sibling test (or `[Theory]` rows) covering empty string `""` to lock the "any existing `eventstore:tenant` claim short-circuits source merging" behavior across both whitespace and empty-string values.
- [x] [Review][Patch] Pin `IClaimsTransformation` lifetime in registration test [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:243-251] — `Tenants_host_registers_eventstore_claims_transformation` only asserts the type is registered. Extend to inspect the registered `ServiceDescriptor` and assert `Lifetime == ServiceLifetime.Transient`. A future change to Singleton/Scoped would silently pass today and break ASP.NET Core's per-request re-invocation contract.
- [x] [Review][Patch] Add host-level `MapInboundClaims=false` assertion [tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs OR a new Tenants-host test] — `ConfigureJwtBearerOptionsPreservesOriginalJwtClaimNames` constructs a fresh `JwtBearerOptions` and asserts the configurer flips the flag. Spec Tasks line 33 requires verifying the host-effective option. Add a test resolving `IOptionsMonitor<JwtBearerOptions>.Get(JwtBearerDefaults.AuthenticationScheme)` from a `CommandApiWebApplicationFactory` and asserting `MapInboundClaims == false` on the resolved options.
- [x] [Review][Patch] Triangulate cross-tenant 403 test [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:221-241] — `Commands_endpoint_returns_403_when_jwt_tenant_claim_targets_another_tenant` uses claim=`tenant-a` and body Tenant=`system` — the 403 could come from the claim, the body, the route, or any of them mismatching. Add a second variant that flips them (claim=`system`, body=`tenant-a`) or asserts the `reasonCode` distinguishes `tenant_mismatch` from `principal_not_member` based on which side mismatches. Currently the gate site is ambiguous.
- [x] [Review][Patch] Split `eventstore:permission` doc row [docs/production-auth-claim-contract.md] — Current row jams six different shapes (`command:submit`, `command:query`, `command:replay`, `commands:*`, `queries:*`, `query:read`, "or a specific command/query type") into one cell with implicit "or" semantics. The actual matcher (`ClaimsRbacValidator`) handles category-prefixed wildcards differently from specific permission strings, and `command:query` is not a recognized category at all. Split into Command-side and Query-side rows with concrete examples and remove any permission shape not actually accepted by EventStore's validator.
- [x] [Review][Patch] P12 — Add global-admin missing-tenant negative test [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs OR tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs] — Add a test that mints a JWT with `global_admin=true` (or `roles=GlobalAdministrator`) and NO `eventstore:tenant` claim. Assert the host-actual behavior (currently expected: 403 fail-closed for command path since `ClaimsTenantValidator` denies blank tenants without a separate global-admin bypass on the Tenants path). Add a complementary documentation note in `docs/production-auth-claim-contract.md` recording the global-admin tenant-claim expectation and the consequence of any production IdP issuing global-admin tokens without a tenant claim.
- [x] [Review][Patch] P13 — Rewrite Keycloak Mapping section [docs/production-auth-claim-contract.md] — Reflect the actual `hexalith-realm.json` shape: Keycloak emits `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission` directly via attribute mappers. Describe direct-claim mapping as the authoritative local-sample shape. Add a subsection naming source claims (`tenants`, `tenant_id`, `tid`, `domains`, `permissions`) as an alternative supported by `EventStoreClaimsTransformation` for IdPs that cannot emit `eventstore:*` directly, with a warning to choose one shape per IdP.
- [x] [Review][Patch] P14 — Add "not enforced by Tenants" callout to rate-limit section [docs/production-auth-claim-contract.md] — Lead the rate-limit subsection with explicit statement: "The Tenants host does NOT register the EventStore rate limiter today; the partitioning content below describes the EventStore host boundary only and is provided for operators routing through both services." Keep the existing partitioning explanation as background only.
- [x] [Review][Patch] P15 — Flip `useTestAuthentication` default to `false` and migrate existing tests [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:304] — Change the parameter default. Audit all existing constructions of `CommandApiWebApplicationFactory` in the file; any test that previously relied on the TestAuth principal (`eventstore:tenant=system`) must now pass `useTestAuthentication: true` explicitly. Run the Tenants integration suite filter and fix any regression that surfaces. Tests authored after this story for the JWT flow inherit fail-closed behavior by default.

### Deferred

- [x] [Review][Defer] EventStore submodule edge cases — five edge-case findings on `EventStoreClaimsTransformation` (idempotency short-circuit when `sub` missing, malformed-JSON falling back to space-delimited parsing of raw JSON text, empty-string `tenant_id` shadowing `tid` fallback, ordinal case-sensitivity vs IdP casing variations, `eventstore:domain`-only token short-circuiting tenant normalization). Spec Implementation Guardrails line 110 forbids modifying `Hexalith.EventStore` for this story; record for a future cross-repository EventStore decision.
- [x] [Review][Defer] JWT signing-key + EnvironmentName test infrastructure [tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:262-287] — Hard-coded `JwtSigningKey` constant must match `appsettings.Development.json`; `WebApplicationFactory` doesn't pin `EnvironmentName="Development"`, so a CI runner with `ASPNETCORE_ENVIRONMENT` set differently silently flips signing-key resolution. Pre-existing test setup pattern; broader story than the claim contract.
- [x] [Review][Defer] JWT `nbf`/`iat`/`ClockSkew` test hygiene — Test tokens minted with `DateTime.UtcNow.AddMinutes(5)` and no `notBefore`/`issuedAt`; no test pins `ClockSkew = TimeSpan.Zero` on the host's `JwtBearerOptions`. Pre-existing test-infrastructure shape; address in a JWT-validation hardening story.
- [x] [Review][Defer] `name`-claim subject-confusion negative test — `docs/production-auth-claim-contract.md:40` says "Do not use `name` as the trusted subject"; no test demonstrates the host rejects/ignores a `name`-only token. Broader authentication audit; not strictly the tenant-claim contract.
- [x] [Review][Defer] Permission wildcard handling and duplicate-permission accumulation tests — Doc lists `commands:*` / `queries:*` wildcards and "duplicates tolerated, do not add permissions" but no Tenants test exercises wildcard matching or duplicate-accumulation safety. Out of 11.2 scope (tenant claim only); belongs to a future RBAC contract story.
- [x] [Review][Defer] `TenantBootstrapHostedService` + new `AuthorizationBehavior<,>` interaction [src/Hexalith.Tenants/Program.cs:52] — Hosted service dispatches commands via MediatR at startup; the new `AddOpenBehavior(typeof(AuthorizationBehavior<,>))` now runs on every MediatR send including non-HTTP background dispatch. No current evidence of breakage; address only if a regression surfaces or when authorization-behavior coverage is expanded.
- [x] [Review][Defer] End-to-end source-claim normalization E2E coverage gap — Only one happy-path test (`Commands_endpoint_returns_202_when_jwt_uses_tenants_source_claim`) exercises the full real-JWT → `IClaimsTransformation` → `ClaimsTenantValidator` pipeline; `tenant_id`, `tid`, JSON-array, space-delimited, mixed-source variants are validated at the unit-transformation layer only. Broader follow-up — partial coverage exists in this diff; full E2E matrix can be added incrementally.

### Dismissed

- `TenantsQueryJwtWebApplicationFactory` "missing from diff" — false positive. Class exists at `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs:454`; the auditor reviewed only added lines.
- Exception handler registration order risk — false positive. `AuthorizationServiceUnavailableHandler` checks `AuthorizationServiceUnavailableException`; `AuthorizationExceptionHandler` checks `CommandAuthorizationException`. Distinct exception types, no inheritance shadowing.
- `AddHttpContextAccessor` registered after `IClaimsTransformation` — false positive. DI registration order is irrelevant; only resolution order matters.
- Mock-router `Substitute.For<IQueryRouter>()` could mask 500/NRE — false positive. Each test also asserts `response.StatusCode.ShouldBe(HttpStatusCode.Forbidden)`; a 500 from a default-mock NRE would fail the status assertion.
- `useTestAuthentication: false` branch doesn't add a JWT scheme — false positive. Program.cs already registers `JwtBearer` as the default scheme; the factory only needs to overlay when adding the Test scheme.
- Shared `_transformation` instance in `TenantClaimContractTests` causes parallel-state leak — false positive. `EventStoreClaimsTransformation` is stateless on its constructor args; xUnit does not parallelize tests within the same class.

## Advanced Elicitation

- Date/time: 2026-05-19T01:02:12+02:00
- Selected story key: 11-2-eventstore-tenant-claim-contract
- Command/skill invocation used: `/bmad-advanced-elicitation 11-2-eventstore-tenant-claim-contract`
- Batch 1 method names: Tree of Thoughts; Red Team vs Blue Team; Failure Mode Analysis; Socratic Questioning; Architecture Decision Records
- Reshuffled Batch 2 method names: Self-Consistency Validation; Pre-mortem Analysis; Code Review Gauntlet; First Principles Analysis; Occam's Razor Application
- Findings summary:
  - The story was ready in broad shape, but mixed direct/source claim behavior could still be misread by implementers because raw JWT aliases and effective downstream `eventstore:*` claims are not equivalent.
  - The highest-risk implementation trap is accidentally treating EventStore rate limiting as a Tenants host requirement when the current host intentionally registers only the domain/client EventStore services.
  - Edge-case tests need to cover whitespace, duplicates, mixed direct/source claims, and transformation idempotency so shared test defaults cannot hide contract drift.
- Changes applied:
  - Added claim-contract clarification that existing `eventstore:*` claims short-circuit alias/source tenant merging in `EventStoreClaimsTransformation`.
  - Added tasks for transformation precedence, mixed direct/source tokens, whitespace/duplicate/source-claim edge cases, and post-transformation effective-principal assertions.
  - Added rate-limit scope guardrails so absent Tenants host rate limiting is documented or deferred rather than patched by registering the full EventStore server extension without an approved hosting-policy change.
  - Strengthened operator documentation guidance to prefer one authoritative mapping style and verify the effective transformed principal.
- Findings deferred:
  - Whether production IdPs should standardize on direct `eventstore:tenant` only remains a product/architecture decision.
  - Whether EventStore should change transformation behavior for mixed direct/source tokens remains outside this Tenants story.
  - End-to-end deployment smoke validation across the real IdP and host boundary remains in Story 11.3.
- Final recommendation: ready-for-dev

### File List

- _bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- README.md
- docs/production-auth-claim-contract.md
- docs/quickstart.md
- src/Hexalith.Tenants/Program.cs
- tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
- tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs
- tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs
