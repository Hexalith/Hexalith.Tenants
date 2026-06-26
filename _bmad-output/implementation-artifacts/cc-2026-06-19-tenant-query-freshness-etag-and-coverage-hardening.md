---
baseline_commit: f12db931aafb01f2698d94f175e84728b51e6455
title: 'Tenant query freshness, ETag, and coverage hardening'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'done'
sprint_key: 'cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md'
approval: 'Administrator approved sprint-change-proposal-2026-06-19-deferred-work.md on 2026-06-19'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - do not expand without re-approval">

## Intent

Harden the REST-backed Tenants query path after Story 3.5 by making freshness truthful, making ETag behavior explicit and robust, restoring state-store reconstruction coverage on the production REST/handler path, and adding support-safety assertions for live gateway error mapping.

## Boundaries & Constraints

**Always:**
- Keep Tenants UI reads on the REST-backed Tenants domain endpoints. Do not reintroduce `TenantsProjectionActor`, `TenantProjectionRouting`, or an EventStore generic query-gateway route for tenant reads.
- Use `Hexalith.EventStore` persistence/read-model abstractions. If generic projection age or read-model metadata support is missing, consume or propose it in EventStore instead of adding generic persistence scaffolding to Tenants.
- Keep domain identifiers as caller-supplied strings, not GUIDs or ULIDs.
- Keep all user-facing failure copy support-safe: no raw payloads, stack traces, tokens, ETags, cursors, correlation IDs, or reason-code internals.

**Never:**
- Do not treat response time as projection age.
- Do not claim `aging` or `stale` unless backed by a real persisted projection timestamp/version or an explicitly documented direct-read rule.
- Do not restore the retired actor-based stateless restart test. Add REST/handler production-path coverage instead.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.Server/Projections/*ReadModel.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/*`
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/*`

## Tasks & Acceptance

**Execution:**
- [x] Audit current query metadata flow from read-model store through handler result, `TenantsQueryController`, REST client, and `TenantQueryGateway.ResolveFreshness`.
- [x] Decide and implement the D6 freshness model: persisted projection age/version if available, shared EventStore capability if needed, or an explicitly documented direct-read `current` / unknown fallback.
- [x] Harden null/whitespace ETag behavior and document the 200/no-ETag/no-304 path.
- [x] Harden `If-None-Match` normalization for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
- [x] Add live gateway support-safety tests for populated `correlationId` / `reasonCode` paths.
- [x] Replace the deleted actor reconstruction coverage with REST/handler persisted read-model reconstruction coverage.
- [x] Re-run focused suites and record any remaining Tier 2/Tier 3 blockers with exact current evidence.
- [x] Update `deferred-work.md` and test-summary artifacts after implementation so stale Story 3.5 blockers are not carried forward.

**Acceptance Criteria:**
1. Given a successful tenant query response, when freshness metadata is emitted, then `ServedAt` is not used as a proxy for projection age unless it is backed by persisted projection metadata; otherwise the response reports freshness as `unknown` or uses an explicitly documented direct-read `current` rule.
2. Given D6 freshness states, when a real persisted projection timestamp/version is available, then `ResolveFreshness` can produce `current`, `aging`, `stale`, and `unknown` according to configurable thresholds with tests for each state.
3. Given the current `IReadModelStore` only exposes `Value` + `ETag`, when implementation needs generic read-model metadata, then the developer either consumes a shared EventStore capability or records an EventStore handoff instead of adding generic persistence scaffolding to Tenants.
4. Given a read-model ETag is null or whitespace, when a REST query succeeds, then the response behavior is explicit and tested: 200 with no ETag and no 304 support, and UI freshness fails closed unless a real projection marker exists.
5. Given `If-None-Match` contains weak tags, `*`, escaped strong tags, or unsupported multi-tag input, when the server/client normalizes it, then unsupported input maps to a safe non-leaking query state and supported strong tags compare consistently with the emitted strong ETag.
6. Given a gateway error response includes `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, or ETags, when `TenantQueryGateway` maps it to UI snapshots, then rendered/user-facing copy excludes those values on the live populated-correlation path.
7. Given the retired actor path was removed, when integration coverage runs, then a REST/handler equivalent proves persisted read-model state survives a fresh service instance or handler/store boundary. Do not restore the retired projection actor test.
8. Given Tier 2 and Tier 3 evidence, when full suites remain blocked, then the story records exact current blockers; if the prior pubsub/health blockers are now resolved, `deferred-work.md` is updated to remove stale blocker text.

## Verification

Run focused tests first, then broaden only as needed:

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter Query`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantQuery`
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-restore --filter StatelessRestart`
- `git diff --check`

## Dev Agent Record

### Implementation Plan

- Preserve the REST-backed Tenants query path and harden only metadata/freshness/conditional-read behavior.
- Treat EventStore read-model ETag/projection-version as the direct-read freshness marker; do not fabricate projection age from `ServedAt`.
- Keep generic persisted projection timestamp/version needs routed to EventStore instead of adding Tenants-owned persistence scaffolding.
- Add focused tests for no-ETag behavior, ETag normalization, support-safe live gateway errors, and REST/handler persisted read-model reconstruction.

### Debug Log

- Audited `TenantQueryResult`, `TenantsQueryController`, `TenantsQueryApiClient`, and `TenantQueryGateway.ResolveFreshness`; confirmed `ServedAt` was making unmarked successful reads appear `Current`.
- Removed automatic `ServedAt` stamping from tenant query results; REST responses now emit freshness headers only when real metadata exists.
- Updated freshness resolution so `Current` requires not-modified, ETag, metadata ETag, or projection-version evidence; `ServedAt` alone now fails closed to `Unknown`.
- Hardened client/server `If-None-Match` handling: unsupported weak tags, `*`, malformed/whitespace values, and unsupported multi-tag inputs no longer throw or produce 304; escaped strong tags compare with the emitted strong ETag.
- Added REST/handler reconstruction coverage using the shared EventStore in-memory read-model store without restoring the retired actor path.
- Full Server.Tests remains blocked by 3 unrelated DAPR dead-letter metadata expectation tests; IntegrationTests now passes with DAPR/Aspire/performance skips instead of health-readiness failures.

### Completion Notes

- Implemented the explicit direct-read freshness model: ETag/projection-version means `Current`, absent markers mean `Unknown`, and response time is not used as projection age.
- Made null/whitespace ETags explicit: successful reads return 200 with no ETag/projection-version/served-at headers and no 304 support.
- Added support-safety and conditional-read tests covering populated gateway ProblemDetails, weak/star/multi/escaped ETags, and persisted read-model reconstruction through the production REST/handler path.
- Recorded the remaining EventStore metadata handoff and current full-suite blockers in `deferred-work.md` and test summaries.

## File List

- `_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsQueryApiClientTests.cs`
- `tests/test-summary.md`

## Change Log

- 2026-06-19T14:18:04+02:00 - Implemented tenant query freshness/ETag hardening, REST/handler reconstruction coverage, support-safety tests, and current validation evidence.

## Review Findings

_Adversarial code review (bmad-code-review) on 2026-06-19 — Blind Hunter + Edge Case Hunter + Acceptance Auditor, working-tree diff vs baseline `f12db93`. Outcome: 1 decision-needed, 2 patch, 1 deferred, 2 dismissed. No CRITICAL/HIGH findings; no frozen-boundary violations; ACs 1, 3, 4, 5, 6, 7, 8 verified MET with direct test evidence._

- [x] [Review][Decision] AC2 `aging`/`stale` states and configurable thresholds are unimplemented and untested — `ResolveFreshness` only ever yields `Current`/`Unknown`, no threshold config type exists, and no test asserts an `aging` state. This is the spec-sanctioned outcome (the frozen boundary forbids faking `aging`/`stale` without a real persisted projection marker, and the generic-metadata need is routed to EventStore handoff `eventstore-2026-06-19-read-model-freshness-metadata`), but the test-summary "AC1–8 covered" line overstates AC2. **Resolved 2026-06-19 (Administrator): deferral accepted as documented; the AC2 `aging`/threshold portion remains routed to the EventStore handoff. The test-summary coverage lines were tightened to "AC1, AC3–AC8 covered; AC2 aging/threshold portion deferred."** [auditor]
- [x] [Review][Patch] Degenerate quote-only read-model ETag produces non-null empty metadata instead of failing closed to `null` [src/Hexalith.Tenants/Queries/TenantQueryResult.cs:43] — `eTag.Trim().Trim('"')` collapses `"`/`""`/`" "` to `""`, which is not `null`, so `FromPayload` builds a `QueryResponseMetadata` with empty `ETag`/`ProjectionVersion`. Behavior stays safe (no header emitted, never 304) but is inconsistent with AC4's null-metadata contract. **Fixed 2026-06-19: `NormalizeETag` now returns `null` when the trimmed/unquoted value is empty or whitespace.** [edge]
- [x] [Review][Patch] `using System.Net;` inserted out of sort order vs repo convention [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:1] — sits before `System.Globalization`; sibling test files keep `System.*` directives alphabetical. **Fixed 2026-06-19: reordered to `System.Globalization`, `System.Net`, `System.Text.Json`.** [blind]
- [x] [Review][Defer] ETag special-character (quote/comma) robustness [src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107; src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29] — deferred, gated by the opaque-store-ETag contract. `NormalizeETagToken` unquotes any value starting+ending with `"` (asymmetric vs raw store tokens), client/server reject commas substring-wise (dropping a single quoted strong tag whose content contains a comma), and client/server normalization disagree on quoted-whitespace/`"*"`. All latent and non-exploitable today because Redis/DAPR read-model ETags are opaque numeric strings without quotes/commas; the emit→submit→compare round-trip is internally symmetric. Revisit only if the EventStore store contract emits special-character ETags. [blind+edge]
