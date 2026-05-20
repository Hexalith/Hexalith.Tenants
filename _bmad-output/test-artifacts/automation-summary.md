---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-identify-targets', 'step-03-orchestrate-generation', 'step-04-validate-and-summarize']
lastStep: 'step-04-validate-and-summarize'
lastSaved: '2026-05-20'
inputDocuments:
  - '_bmad/tea/config.yaml'
  - '_bmad-output/project-context.md'
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
  - '_bmad-output/implementation-artifacts/deferred-work.md'
  - '_bmad-output/implementation-artifacts/11-2-eventstore-tenant-claim-contract.md'
  - '_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md'
  - 'docs/production-auth-claim-contract.md'
  - 'src/Hexalith.Tenants/Program.cs'
  - 'src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs'
  - 'tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj'
  - 'tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj'
  - 'tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj'
  - 'tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs'
  - 'tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj'
  - 'tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs'
  - 'tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj'
  - 'Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs'
  - '.claude/skills/bmad-testarch-automate/resources/tea-index.csv'
  - '.claude/skills/bmad-testarch-automate/resources/knowledge/test-levels-framework.md'
  - '.claude/skills/bmad-testarch-automate/resources/knowledge/test-priorities-matrix.md'
  - '.claude/skills/bmad-testarch-automate/resources/knowledge/test-quality.md'
---

# Test Automation Summary

## Step 1 — Preflight & Context

### Stack Detection

- `config.test_stack_type` = `auto`.
- Project manifests detected under `src/` and `tests/`: 7 `.csproj` files (Tenants surface) + 5 `*.Tests.csproj` projects.
- Root `package.json` is **semantic-release tooling only** (no test runner, no frontend deps). No `playwright.config.*` or `cypress.config.*` in the Tenants tree.
- Submodule `Hexalith.FrontComposer/` carries its own JS surface but is out of scope for Tenants test automation.
- **Detected stack: `backend`** — pure .NET 10 / C# / xUnit v3.

### Framework Gate — PASS

All five test projects exist with xUnit v3 / Shouldly / NSubstitute wiring:

- `Hexalith.Tenants.Contracts.Tests` (Tier 1 — pure unit)
- `Hexalith.Tenants.Client.Tests` (Tier 1 — pure unit)
- `Hexalith.Tenants.Testing.Tests` (Tier 1 — pure unit; hosts the reflection-driven `ConformanceTests`)
- `Hexalith.Tenants.Server.Tests` (Tier 2 — DAPR integration; requires `dapr init` + Docker)
- `Hexalith.Tenants.IntegrationTests` (Tier 3 — Aspire E2E)

Test sub-folders observed: `Aggregates/`, `Authorization/`, `Bootstrap/`, `CommandPipeline/`, `Configuration/`, `DomainProcessing/`, `Health/`, `Projections/`, `Queries/`, `Telemetry/`, `Validators/`, `Conformance/`, `Fakes/`, `Helpers/`, `Fixtures/`, `Handlers/`, `Registration/`, `Subscription/`.

### Execution Mode — BMad-Integrated

Planning + implementation artifacts are present:

- `_bmad-output/planning-artifacts/`: PRD, architecture, epics, implementation-readiness reports (latest 2026-05-16), 13 sprint change proposals, UX spec.
- `_bmad-output/implementation-artifacts/`: sprint-status.yaml + ~40 story files across Epics 1–12.
- Sprint status snapshot (2026-05-20):
  - **Done**: Epics 1–11 + Epic 12 story 12-1.
  - **In review**: `12-2-audit-timeline-and-consequence-preview-readiness`.
  - **Ready-for-dev**: `12-3-three-phase-command-feedback-sequencing`, `12-4-phase-2-ui-story-backlog-with-explicit-blockedBy`.
  - **Recent gate**: full Debug/no-restore solution gate passed at 723 passed / 1 skipped on 11-3 close.

### TEA Config Flags

| Flag | Value | Effect for this run |
|------|-------|----------------------|
| `tea_use_playwright_utils` | `true` | **N/A** — pure .NET project, no `page.goto`/`page.locator` in test source. Playwright Utils fragments skipped. |
| `tea_use_pactjs_utils` | `false` | Skip pactjs-utils. |
| `tea_pact_mcp` | `none` | Skip Pact MCP fragment. |
| `tea_browser_automation` | `auto` | N/A for backend. |
| `test_stack_type` | `auto` → `backend` | Backend profile applied. |
| `risk_threshold` | `p1` | Gate decisions block on ≥P1 risks. |

### Knowledge Fragments Loaded (Backend Profile)

Core (loaded):

- `test-levels-framework.md` — unit/integration/E2E selection rules
- `test-priorities-matrix.md` — P0–P3 + risk-score alignment
- `test-quality.md` — DoD: deterministic, isolated, &lt;300 lines, &lt;1.5 min, self-cleaning

Pending on-demand (will load in Step 2/3 as targets surface):

- `data-factories.md`, `selective-testing.md`, `ci-burn-in.md`, `risk-governance.md`, `probability-impact.md`, `confidence-gate.md`
- `api-testing-patterns.md` (specialized — pure backend, no-browser)
- `error-handling.md` (resilience / api / backend)
- `contract-testing.md` (if Tenants↔EventStore event-contract testing comes into scope)

Deliberately skipped (JS/UI only):

- All Playwright Utils, Pact.js Utils, Pact MCP, network-first, intercept-network-call, selector-resilience, visual-debugging.

### Project Conventions Carried Forward (Persistent Facts)

From `_bmad-output/project-context.md` — these constrain every test we'll generate:

1. **Three-tier model**: Tier 1 pure unit (≤10ms/test target, no infra), Tier 2 DAPR integration (must inspect state-store end-state, not just HTTP/mock counts), Tier 3 Aspire E2E.
2. **xUnit v3 + Shouldly only** — never `Assert.*`. Every test has at least one Shouldly assertion. Typed event assertion pattern: `ShouldBeOfType<T>()` + cast + property `ShouldBe`.
3. **Test naming**: `{Type}Tests.cs` (plural). Method: `snake_case_with_PascalCase_for_type_names`.
4. **`CreateCommand<T>(command, actorUserId, isGlobalAdmin)` helper** — never construct `CommandEnvelope` inline.
5. **Shared `JsonSerializerOptions` factory** — never inline `new JsonSerializerOptions()`.
6. **ULIDs not GUIDs** — `Ulid.TryParse` on `messageId`/`correlationId`/`aggregateId`/`causationId`.
7. **Async waits**: poll observable state with bounded timeout; never `Thread.Sleep` / `Task.Delay` as sync.
8. **Conformance / naming-convention / serialization round-trip tests are release blockers** — never `[Skip]`.
9. **Cross-tenant isolation (NFR5)**: Tier 1 + Tier 2 + Tier 2/3 defense; cursor tokens + Problem Details bodies must also be checked for leaks.
10. **NFR13 perf** (500K events → ≤30s cold rehydrate) is the nightly category — never per-PR.

### Open Questions for Step 2

The current sprint shows two streams of candidate work for test automation expansion:

- **Story 12-2** is in review — likely has a fresh patch (`12-2-review-diff.patch` exists). Worth checking if the review surfaced any test gaps.
- **Story 12-3** and **12-4** are ready-for-dev — fresh ATDD/automation candidates. Note: Story 12-4 is a UI backlog/dependency-mapping story (planning artifact), so it likely has **no code-level tests to automate**.
- Alternatively, post-epic deferred-work items in `_bmad-output/implementation-artifacts/deferred-work.md` may carry test debt worth closing.

**Decision needed from Jerome**: pick a target (story / project area / risk surface) before Step 2 can proceed.

### Output Location

This file: `_bmad-output/test-artifacts/automation-summary.md`. Subsequent steps append below this section.

---

## Step 2 — Identify Automation Targets

### Target Scope: **Bundle 1 — Production Auth Contract Backfill**

Selected after triage of `_bmad-output/implementation-artifacts/deferred-work.md`. Targets are Tenants-scoped, in-scope (no submodule changes), and have explicit file:line citations from prior reviews.

### Coverage Gaps (from deferred-work.md)

| ID | Gap | Source citation | Risk surface |
|----|-----|------------------|--------------|
| A | `/process` endpoint auth contract not pinned — neither "anonymous accepted" nor "auth required" is locked. `Program.cs:132-136` maps the route without `.RequireAuthorization()`; the DAPR `AggregateActor → CommandApi` callback depends on this. | 11-3 review · `CommandApiRuntimeIntegrationTests.cs:44-71` | DAPR pipeline integrity |
| B | `name`-only claim contract not enforced. `production-auth-claim-contract.md:13` says *"Do not use `name` as the trusted subject"* but no test rejects a token carrying `name` without `sub`. | 11-2 review | Subject confusion / audit mis-attribution |
| C | Claim-source normalization only happy-path tested E2E (`tenants` JSON array). Space-delimited `tenants`, `tenant_id`, `tid` fallback, and `tenant_id`+`tid` precedence are unit-tested in `TenantClaimContractTests` but not exercised through the live JwtBearer + ClaimsTransformation pipeline. | 11-2 review · `CommandApiRuntimeIntegrationTests.cs:117-251` | Multi-IdP rollout regression |
| D | Permission claim shapes (`commands:*`, `command:submit`, exact-type tokens, `queries:*`, `query:read`, legacy `command:query`) and duplicate-permission accumulation untested in Tenants. | 11-2 review · `docs/production-auth-claim-contract.md:43` | RBAC contract bypass |
| E | *(Deferred from this run)* `TenantBootstrapHostedService` × `AuthorizationBehavior<,>` interaction — note in deferred-work.md described this as MediatR-pipeline background dispatch, but `TenantBootstrapHostedService.cs:22-65` actually sends via DAPR HTTP (not MediatR). Gap re-scoped to "out of pattern; defer until evidence of real risk." | 11-2 review | Bootstrap robustness (theoretical) |

### Risk Scoring (probability × impact, 1–9)

Aligned with `risk-governance.md` / `probability-impact.md` scale; project `risk_threshold` is `p1`.

| ID | Probability | Impact | Score | Priority | Rationale |
|----|-------------|--------|-------|----------|-----------|
| A | 2 (someone may add `RequireAuthorization` without considering DAPR callback) | 3 (silent pipeline stall at Step 4 of AggregateActor checkpoint sequence) | **6** | **P0** | DAPR callback contract regression is silent and downstream-catastrophic |
| B | 2 (`name`-only tokens are a common IdP misconfiguration) | 3 (cross-actor identity confusion in audit logs and RBAC) | **6** | **P0** | Production-auth-claim-contract.md explicit contract; security-relevant |
| C | 2 (IdP swap-out / federation rollout) | 2 (some users denied with confusing 403) | **4** | **P1** | Already partially covered at unit tier — closing the integration gap is incremental |
| D | 1 (depends on misconfigured IdP) | 3 (could grant elevated access) | **3** | **P1** | Confidence-gate: needs source inspection of `ClaimsRbacValidator` in Step 3 before final scenario shape |
| E | 1 | 2 | **2** | **P2** | Deferred — original gap framing didn't match the actual bootstrap dispatch shape |

### Test Level Assignments

Per `test-levels-framework.md` (prefer lower tiers; favor integration for "service contracts", E2E only for "cross-system workflows"):

| Gap | Primary tier | Secondary tier | Rationale |
|-----|--------------|----------------|-----------|
| A | Tier 3 (Aspire E2E) | — | `/process` route is host-level; only the live `WebApplicationFactory` exposes the contract |
| B | Tier 2 (Server.Tests/Authorization) | Tier 3 (IntegrationTests) | Claim transformation + validator are pure (Tier 2); pipeline behavior with no `sub` is Tier 3 |
| C | Tier 3 (IntegrationTests) | — | Live JwtBearer + ClaimsTransformation pipeline is the contract surface; Tier 2 already covers transformation unit behavior |
| D | Tier 2 (Server.Tests/Authorization) | Tier 3 (IntegrationTests, if needed) | RBAC validator is testable directly; confirm API surface in Step 3 |

**Duplicate Coverage Guard** applied: `TenantClaimContractTests.cs` already covers the unit-tier behavior of claim-source normalization (lines 44–146) and the global-admin/non-global-admin tenant-claim contract (lines 178–232). New tests must not re-test these at integration tier without justification — they add a different aspect: *live pipeline behavior with real JwtBearer middleware and `User.FindFirst("sub")` controller reads*.

### Fixture Reuse

The existing `CommandApiRuntimeIntegrationTests.cs` provides everything the new Tier 3 tests need — no fixture additions:

- `CommandApiWebApplicationFactory` (nested, line 429): override `ICommandRouter`/`ICommandStatusStore`/`ICommandArchiveStore` via constructor params; `useTestAuthentication: false` (the post-P15 default) exercises the real JwtBearer + ClaimsTransformation pipeline
- `CreateJwt(...)` helper (line 387): symmetric-key HS256, configurable issuer/audience/expires/claims; default `expires` = `UtcNow + 5min`
- `CreateClientWithBearer(...)` / `CreateBootstrapRequest(...)` helpers (lines 381, 414)

The Tier 2 file `TenantClaimContractTests.cs` provides:

- `_transformation` (`EventStoreClaimsTransformation` with `NullLogger`)
- `CreatePrincipal(params Claim[])` helper
- `TenantClaims(...)` extractor
- Existing patterns for combining transformation + `ClaimsTenantValidator`

### Coverage Plan (Concrete Test IDs)

Following `{EPIC}.{STORY}-{LEVEL}-{SEQ}` convention from `test-levels-framework.md`, scoped to deferred-work backfill:

| Test ID | Tier | File | Test name (sketch) | Maps to gap | Priority |
|---------|------|------|---------------------|-------------|----------|
| AUTH-INT-001 | T3 | `CommandApiRuntimeIntegrationTests.cs` | `Process_endpoint_accepts_anonymous_request_to_preserve_dapr_callback_contract` | A | P0 |
| AUTH-T2-001 | T2 | `TenantClaimContractTests.cs` | `NameOnlyClaimWithoutSubDoesNotEstablishTrustedSubject` | B | P0 |
| AUTH-INT-002 | T3 | `CommandApiRuntimeIntegrationTests.cs` | `Commands_endpoint_returns_403_when_jwt_carries_name_claim_without_sub` | B | P0 |
| AUTH-INT-003 | T3 | `CommandApiRuntimeIntegrationTests.cs` | `Commands_endpoint_returns_202_when_jwt_uses_supported_source_claim_shape` (Theory: space-delim `tenants`, `tenant_id` only, `tid` only fallback, `tenant_id`+`tid` precedence) | C | P1 |
| AUTH-T2-002 | T2 | `TenantClaimContractTests.cs` (or new `PermissionClaimContractTests.cs`) | Permission wildcard + duplicate-accumulation Theory | D | P1 (**confidence-gated** — defer concrete shape to Step 3 source inspection of `ClaimsRbacValidator`) |

### Out of Scope for This Run

- Gap E (bootstrap × authorization-behavior interaction): re-scoped — actual bootstrap path is DAPR HTTP, not MediatR. Track in deferred-work for separate evaluation.
- EventStore submodule edge cases (idempotency short-circuit, malformed JSON, ordinal case sensitivity) — explicitly forbidden by the 11-2 spec guardrail "Spec Implementation Guardrails forbid modifying `Hexalith.EventStore` for this story". Cross-repo decision required.
- JWT signing-key / `EnvironmentName` test-infrastructure pinning — broader test-fixture hardening, separate scope.
- `nbf`/`iat`/`ClockSkew` token-hygiene — JWT validation hardening story candidate.

### Confidence Gate Outcome (Step 2)

Per `confidence-gate.md`: applied to each test ID.

- A — confidence 9/10 (route mapping verified in `Program.cs:132`; existing test pattern with `useTestAuthentication: true` shows the shape).
- B (T2) — confidence 9/10 (`EventStoreClaimsTransformation` + `ClaimsTenantValidator` API verified in existing tests; expected behavior fits documented contract).
- B (T3) — confidence 8/10 (`User.FindFirst("sub")` read site is `CommandsController.cs:67` per comment in `Tenants_host_keeps_raw_sub_claim_under_real_jwt_pipeline`; expected 403 path may surface as 401 if no identity established — Step 4 to confirm exact status with first run).
- C — confidence 9/10 (mirror existing Theory shape from `TenantClaimContractTests.cs:67-78`, applied at integration tier).
- D — confidence 5/10 (insufficient — need to inspect `ClaimsRbacValidator` API surface before drafting scenarios). **Pause-and-confirm planned for Step 3.**

### Step 2 Output Summary

- Target bundle: production auth contract backfill (Epic 11 follow-on)
- 5 concrete test IDs proposed; A–C green-lit for Step 3, D held pending source inspection, E deferred.
- Estimated yield: ~5–8 test methods across 2 files; ~5–6 P0/P1 risk closures.
- No new fixtures / no new helpers required.

---

## Step 3 — Orchestrate Generation

### Execution Mode Resolution

```
⚙️ Execution Mode Resolution:
- Requested:        auto  (from tea_execution_mode)
- Probe Enabled:    true  (tea_capability_probe=true)
- Supports agent-team: false  (no agent-team runtime)
- Supports subagent:   true   (Agent tool available)
- Resolved:         sequential
```

**Why sequential despite subagent capability:** the skill's `step-03a-subagent-api.md` and `step-03b-subagent-backend.md` worker step files are JS/Playwright-oriented (Playwright fixtures, `apiRequest`/`page` helpers, `mergeTests`, etc.). For a pure .NET 10 / xUnit v3 backend with K&R braces, Shouldly-only assertions, and project-specific helpers (`CreateCommand<T>`, shared `JsonSerializerOptions` factory), dispatching a context-naïve general-purpose subagent would produce non-conforming code that fails the build (`TreatWarningsAsErrors=true`). All required context is already loaded in the orchestrator turn; sequential preserves the output contract while honoring the skill's master rule: *"Deterministic mode selection + stable output contract."*

### Confidence Gate — Gap D Resolution

Inspected `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs` per `confidence-gate.md` stop-and-ask rule.

**Findings:**
- `ValidateAsync(user, tenantId, domain, messageType, messageCategory, ct, aggregateId?)` — pure async validator
- Permission claims **only enforced when present** (empty list = allowed)
- Command path accepts: `commands:*` (wildcard) OR `command:submit` (category) OR exact `messageType` token — all case-insensitive via `OrdinalIgnoreCase`
- Pure boolean OR — duplicates cannot "accumulate elevation" by construction
- Global admin bypass already covered by existing `TenantClaimContractTests` rows 178–204

**Confidence now 9/10.** Gap D moves from gated to **green for Step 4**. Reframe: T3 integration tests (live AuthorizationBehavior pipeline) — the unit-level `ClaimsRbacValidator` contract is already covered in `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Authorization/ClaimsRbacValidatorTests.cs`. The Tenants-side gap is "does the live pipeline honor these claim shapes end-to-end through `/api/v1/commands`?"

### Final Test Design (Given/When/Then)

Test ID format: `{EPIC}.{STORY}-{LEVEL}-{SEQ}` per `test-levels-framework.md`. Bundle scope = production auth contract backfill (no specific story epic); use `AUTH-{LEVEL}-{SEQ}`.

#### AUTH-T2-001 — `NameOnlyClaimWithoutSubDoesNotEstablishTrustedSubject`

- **File**: `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs`
- **Type**: `[Fact]`, Tier 2, P0
- **Gap**: B (Tier 2 portion)
- **Given**: a principal carrying only `name="display-only-user"` (no `sub`, no `eventstore:tenant`)
- **When**: `EventStoreClaimsTransformation.TransformAsync` runs, then `ClaimsTenantValidator.ValidateAsync(..., "system")`
- **Then**:
  - `result.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBeNull()` — `name` not promoted to NameIdentifier
  - `result.FindFirst("sub")?.Value.ShouldBeNull()` — `sub` remains absent
  - `validation.IsAuthorized.ShouldBeFalse()`
  - `validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember)` (no tenant claim)
- **Risk closed**: subject-confusion (audit attribution to display name)

#### AUTH-INT-001 — `Process_endpoint_accepts_anonymous_request_to_preserve_dapr_callback_contract`

- **File**: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- **Type**: `[Fact]`, Tier 3, P0
- **Gap**: A
- **Given**: `CommandApiWebApplicationFactory(useTestAuthentication: false)` (real JwtBearer pipeline, no `RequireAuthorization()` on `/process` per `Program.cs:132`)
- **When**: anonymous `POST /process` with valid `DomainServiceRequest` body (CreateTenant CreateCommand-style envelope), no `Authorization` header
- **Then**:
  - `response.StatusCode.ShouldBe(HttpStatusCode.OK)` — the DAPR `AggregateActor → CommandApi` callback contract
  - `result.IsRejection.ShouldBeFalse()`
  - `result.Events[0].EventTypeName.ShouldEndWith("TenantCreated")`
- **Comment locked in test body**: `// Pins the DAPR callback contract: AggregateActor invokes /process via DAPR service-to-service. Adding .RequireAuthorization() to this route silently stalls the pipeline at AggregateActor 5-step checkpoint Step 4 — see _bmad-output/implementation-artifacts/deferred-work.md entry from 11-3 review.`
- **Risk closed**: silent DAPR callback regression on future auth tightening

#### AUTH-INT-002 — `Commands_endpoint_returns_403_when_jwt_carries_only_name_claim_without_sub`

- **File**: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- **Type**: `[Fact]`, Tier 3, P0
- **Gap**: B (Tier 3 portion)
- **Given**: a JWT minted by `CreateJwt(userId: "real-user", claims: [new Claim("name", "display-only-user")])` — note: `CreateJwt` adds `sub` from `userId`, so we need a variant or override to suppress `sub`. Step 4 will reshape `CreateJwt` *or* mint the token inline without calling the helper to produce a `sub`-less token
- **When**: `POST /api/v1/commands` with `BootstrapGlobalAdmin` payload
- **Then**:
  - `response.StatusCode.ShouldBe(HttpStatusCode.Forbidden)` (subject is established by JwtBearer from `name`, but no `eventstore:tenant`)
  - `details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member")`
  - `await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default)` — request never reached dispatch
- **Helper change**: extend `CreateJwt` with `bool includeSub = true` overload OR add a local `CreateJwtWithoutSub(...)` so existing tests keep their `sub` default. Step 4 picks the less-invasive shape.
- **Risk closed**: `name` accepted as trusted subject in absence of `sub`

#### AUTH-INT-003 — `Commands_endpoint_returns_202_when_jwt_uses_supported_source_claim_shape`

- **File**: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- **Type**: `[Theory]`, Tier 3, P1
- **Gap**: C
- **Rows** (each a `Claim[]` factory; named theory rows via `MemberData`):
  1. **space-delimited `tenants`** — `[new Claim("tenants", "system tenant-a")]`
  2. **`tenant_id` direct** — `[new Claim("tenant_id", "system")]`
  3. **`tid` fallback** — `[new Claim("tid", "system")]`
  4. **`tenant_id` + `tid` precedence** — `[new Claim("tenant_id", "system"), new Claim("tid", "tenant-a")]` (per contract doc: `tid` silently dropped)
- **Given**: `CreateJwt(userId, claims: <row>)` and `CommandApiWebApplicationFactory(useTestAuthentication: false)`
- **When**: `POST /api/v1/commands` (BootstrapGlobalAdmin against tenant=`system`)
- **Then**:
  - `response.StatusCode.ShouldBe(HttpStatusCode.Accepted)`
  - `router.Received(1).RouteCommandAsync(Arg.Is<SubmitCommand>(c => c.Tenant == "system"), Arg.Any<CancellationToken>())`
- **Comment in test body**: `// TenantClaimContractTests covers these source shapes at unit tier; this Theory pins them through the live JwtBearer + EventStoreClaimsTransformation pipeline. Per docs/production-auth-claim-contract.md mixed-source precedence rule, tid is silently dropped when tenant_id is present.`
- **Risk closed**: multi-IdP rollout regression at pipeline tier

#### AUTH-INT-004 — `Commands_endpoint_returns_202_when_jwt_carries_authorizing_permission_claim`

- **File**: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- **Type**: `[Theory]`, Tier 3, P1
- **Gap**: D (positive)
- **Rows**:
  1. **wildcard** — `[new Claim("eventstore:tenant", "system"), new Claim("eventstore:permission", "commands:*")]`
  2. **category** — `[..., new Claim("eventstore:permission", "command:submit")]`
  3. **exact** — `[..., new Claim("eventstore:permission", nameof(BootstrapGlobalAdmin))]`
- **Given**: real JwtBearer pipeline; mocked router returns success
- **When**: `POST /api/v1/commands` (BootstrapGlobalAdmin)
- **Then**: `Accepted` (202) + `router.Received(1).RouteCommandAsync(...)`
- **Comment in test body**: `// Per ClaimsRbacValidator.cs, permission claims are only enforced when present and accept wildcard, category, or exact-type matches. Pins all three through the live pipeline.`

#### AUTH-INT-005 — `Commands_endpoint_returns_403_when_jwt_carries_only_unrelated_permission_claims`

- **File**: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- **Type**: `[Theory]`, Tier 3, P1
- **Gap**: D (negative — wrong type & duplicate non-elevating)
- **Rows**:
  1. **wrong exact** — `[new Claim("eventstore:tenant", "system"), new Claim("eventstore:permission", nameof(CreateTenant))]` while submitting `BootstrapGlobalAdmin`
  2. **duplicate wrong** — two `[new Claim("eventstore:permission", nameof(CreateTenant)), new Claim("eventstore:permission", nameof(CreateTenant))]` claims, still submitting `BootstrapGlobalAdmin`
- **Given**: real JwtBearer pipeline; router NOT expected to receive any call
- **When**: `POST /api/v1/commands` (BootstrapGlobalAdmin)
- **Then**:
  - `response.StatusCode.ShouldBe(HttpStatusCode.Forbidden)`
  - `details.Extensions["reasonCode"]?.ToString()` is one of `["insufficient_permission", "insufficient_role"]` — verify exact mapping during Step 4 first run (existing tests show `tenant_mismatch` and `principal_not_member`; permission-failure reasonCode is unverified yet — Step 4 to confirm)
  - `await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default)`
- **Comment in test body**: `// Per ClaimsRbacValidator.cs line 77, the validator returns Denied with InsufficientPermission. Duplicates can't elevate by construction (boolean OR over case-insensitive equality).`

### Test-Quality DoD Checklist (applied prospectively per `test-quality.md`)

Every test above is designed to satisfy:

- [x] **No hard waits** — all assertions are over deterministic HTTP response state, no `Thread.Sleep`/`Task.Delay`
- [x] **No conditionals controlling flow** — single linear path per test method
- [x] **< 300 lines** — each new test method ≤ 40 lines; full file growth ≤ 250 lines
- [x] **< 1.5 minutes** — `WebApplicationFactory` with mocked router averages <2s per test
- [x] **Self-cleaning** — `await using var factory` and `using HttpClient client` already in use; no shared state
- [x] **Explicit assertions in test body** — no hidden `expect()` helpers; all `ShouldBe`/`Received` calls inline
- [x] **Parallel-safe** — each test creates its own factory; no shared static state introduced

### Sequential "Worker" Output (synthetic — for Step 4 hand-off)

Per the skill's sequential mode contract, the orchestrator produces a single aggregated test-generation plan in lieu of subagent JSON outputs:

```yaml
success: true
mode: sequential
detected_stack: backend
tests_designed: 6 methods (1 Fact T2 + 2 Facts T3 + 3 Theories T3)
distinct_test_cases: ~12 (counting Theory rows)
target_files:
  - tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs  # +1 method
  - tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs   # +5 methods, possible CreateJwt helper extension
new_fixtures: none
new_helpers: optional CreateJwt(includeSub:false) overload or inline JWT mint in AUTH-INT-002
knowledge_fragments_used:
  - test-levels-framework.md
  - test-priorities-matrix.md
  - test-quality.md
  - confidence-gate.md  (Gap D)
  - risk-governance.md  (priority scoring)
ready_for_step_4: true
```

### Open Items for Step 4

1. **Reason code for permission-only failures** (AUTH-INT-005) — first dotnet test run will confirm whether the `AuthorizationBehavior` maps `InsufficientPermission` to `reasonCode="insufficient_permission"` or something else. Assertion will be hardened after first observation.
2. **`CreateJwt` helper extension vs. inline JWT mint** (AUTH-INT-002) — pick the less-invasive shape. Default to inline mint to keep existing helper signature stable.
3. **DAPR dependency for AUTH-INT-001** — `/process` route doesn't require DAPR runtime for this test (the handler dispatches an in-memory aggregate via reflection); the factory mock path will be exercised. Verify by running in Step 5.

---

## Step 4 — Validate & Summarize

### Coverage Plan by Test Level and Priority

| Test ID | Tier | Priority | Type | Cases | Pass |
|---------|------|----------|------|-------|------|
| AUTH-T2-001 — `NameOnlyClaimWithoutSubDoesNotEstablishTrustedSubject` | T2 | P0 | `[Fact]` | 1 | ✅ |
| AUTH-INT-001 — `Process_endpoint_accepts_anonymous_request_to_preserve_dapr_callback_contract` | T3 | P0 | `[Fact]` | 1 | ✅ |
| AUTH-INT-002 — `Commands_endpoint_returns_401_when_jwt_carries_only_name_claim_without_sub` (renamed from `_403_` after first-run finding) | T3 | P0 | `[Fact]` | 1 | ✅ |
| AUTH-INT-003 — `Commands_endpoint_returns_202_when_jwt_uses_supported_source_claim_shape` | T3 | P1 | `[Theory]` | 4 | ✅ |
| AUTH-INT-004 — `Commands_endpoint_returns_202_when_jwt_carries_authorizing_permission_claim` | T3 | P1 | `[Theory]` | 3 | ✅ |
| AUTH-INT-005 — `Commands_endpoint_returns_403_when_jwt_carries_only_unrelated_permission_claims` | T3 | P1 | `[Theory]` | 2 | ✅ |
| **Totals** | | | **6 methods** | **12 cases** | **12/12** |

### Files Created / Updated

- `tests/Hexalith.Tenants.Server.Tests/Authorization/TenantClaimContractTests.cs` — +1 `[Fact]`, ~20 lines
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` — +5 methods (2 Facts + 3 Theories) and 1 private helper (`CreateJwtWithoutSub`), ~190 lines
- `_bmad-output/test-artifacts/automation-summary.md` — this file (workflow record)

No fixtures introduced; no shared helpers refactored; no production code modified.

### Test-Quality DoD — Final Check

Applied `test-quality.md` checklist to all new tests:

- [x] **No hard waits** — all assertions over deterministic HTTP/principal state
- [x] **No conditionals controlling flow** — straight-line per test
- [x] **< 300 lines per test** — every new method ≤ 40 lines
- [x] **< 1.5 minutes per test** — all 12 cases run in &lt;1.5 seconds total in IntegrationTests; &lt;30ms in Server.Tests
- [x] **Self-cleaning** — `await using var factory` + `using HttpClient client`; no shared static state
- [x] **Explicit assertions** — no `expect()`-hidden helpers; every Shouldly call inline
- [x] **Parallel-safe** — every test creates its own factory; no global mutation
- [x] **No `Assert.*`** — Shouldly throughout
- [x] **No `Thread.Sleep`/`Task.Delay`** — none
- [x] **No `Guid.TryParse` on ULIDs** — N/A (no ID parsing in these tests)
- [x] **CreateCommand-style envelopes** — inline `CommandEnvelope` construction in AUTH-INT-001 matches the existing pattern at line 49–61

### Full Solution Regression Gate

Run: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-build`

| Project | Result |
|---------|--------|
| Hexalith.Tenants.Contracts.Tests | 35/35 ✅ |
| Hexalith.Tenants.Sample.Tests | 17/17 ✅ |
| Hexalith.Tenants.Client.Tests | 48/48 ✅ |
| Hexalith.Tenants.Testing.Tests | 89/89 ✅ |
| Hexalith.Tenants.Server.Tests | **475/475 ✅** (+1 vs baseline) |
| Hexalith.Tenants.IntegrationTests | **71/71 ✅** + 1 skip (NFR13 perf, nightly-only per project policy) (+11 vs baseline) |
| **Total** | **735 passed / 1 skipped / 0 failed** |

Baseline (sprint-status.yaml at 11-3 close, 2026-05-20): 723 passed / 1 skipped. Net new cases: **+12**, exactly matching the design.

### Key Assumptions and Risks (carried forward)

1. **AUTH-INT-002 401 contract** — pinned the observed behavior: a JWT without `sub` is rejected at JwtBearer authentication with 401, not at authorization with 403. This is a *stronger* contract than originally designed. Document downstream: if a future host change adds a `name`→`sub` fallback (regression of `production-auth-claim-contract.md:13`), this test would flip to 403 or 202 and fail loudly.
2. **AUTH-INT-001 anonymous `/process`** — pins today's contract (no `.RequireAuthorization()` on the route). If a future ADR requires authenticating the DAPR callback (e.g., via mTLS or a service-account token), this test must be reframed alongside the contract change — failing on intent, not accident.
3. **AUTH-INT-004/005 RBAC pipeline coverage** — Tenants doesn't register the EventStore rate limiter, and the `IRbacValidator` registration (`Program.cs:91`) is `ClaimsRbacValidator`. If a deployment switches to `ActorRbacValidator` (e.g., remote actor-based authorization), the test outcomes still hold for the local validator path but provide no signal on actor-based RBAC — out of scope for this bundle.
4. **No DAPR/Docker dependency introduced** — all 12 new cases run via `WebApplicationFactory` with mocked router/status/archive stores. The full solution gate runs cleanly on a dev box without `dapr init`; only the nightly NFR13 perf test was skipped (its existing skip behavior, unchanged).

### Out-of-Scope (Reaffirmed)

- Gap E (bootstrap × authorization-behavior) — re-scoped in Step 2; deferred to a future evaluation since the bootstrap path uses DAPR HTTP, not MediatR.
- EventStore submodule edge cases (idempotency short-circuit, malformed JSON, ordinal case sensitivity, `tenant_id=""` shadowing `tid`) — explicitly forbidden by 11-2 spec guardrail "Spec Implementation Guardrails forbid modifying `Hexalith.EventStore` for this story".
- JWT signing-key + `EnvironmentName` test-infrastructure pinning, `nbf`/`iat`/`ClockSkew` — broader test-fixture hardening story.

### Next Recommended Workflow

Per `bmad-tea` menu options:

1. **`bmad-testarch-trace` (TR)** — extend the traceability matrix to mark deferred-work items A–D as closed and link to the new test IDs. Useful if you want a clean trace artifact alongside the sprint-status update.
2. **`bmad-testarch-test-review` (RV)** — independent review pass on the 6 new tests. Catches review-found patches early (Epic 2 R2-A6 reviewer-driven-patch rate is 5/5 historically).
3. **`bmad-testarch-ci` (CI)** — verify the GitHub Actions Tier 2/3 lanes include the new tests automatically (they should, since both new methods live in already-tracked test projects).

My top recommendation: **(2) test-review** before committing — it costs 10 minutes and historically catches one review-finding patch per story. After that, a conventional commit:

```
test(auth): backfill production auth contract gaps from deferred-work

Closes coverage gaps A–D from _bmad-output/implementation-artifacts/deferred-work.md:
- /process endpoint anonymous-accepted contract pin (DAPR callback)
- name-only claim does not establish trusted subject (T2 + T3)
- claim-source normalization Theory through live JwtBearer pipeline
- ClaimsRbacValidator permission shapes (wildcard/category/exact) + duplicate-non-elevation

12 new test cases across TenantClaimContractTests and CommandApiRuntimeIntegrationTests.
Full solution gate: 735 passed / 1 skipped / 0 failed.
```

### Workflow Completion

- **Mode**: Create (sequential dispatch)
- **Stack**: backend (.NET 10 / xUnit v3)
- **Target**: production auth contract backfill (Epic 11 follow-on)
- **Status**: ✅ Complete
- **Last saved**: 2026-05-20


