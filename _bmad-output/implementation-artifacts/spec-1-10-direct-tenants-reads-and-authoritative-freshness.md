---
title: 'Story 1.10: Direct Tenants Reads and Authoritative Freshness'
type: 'feature'
created: '2026-07-28'
status: 'in-review'
baseline_commit: '8d64563c75423c861b0be0e3a7cc4de18f673d37'
baseline_revision: '8d64563c75423c861b0be0e3a7cc4de18f673d37'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** Tenants UI reads still use the generic EventStore query client, so the six canonical REST routes are not proven and projection freshness is deliberately normalized to unknown. The member table also has no dedicated paged tenant-users read, so the canonical sixth route is absent from the user-facing path.

**Approach:** Introduce a server-only Tenants REST transport over a separate `Tenants:BaseAddress`, map all six typed reads directly, preserve the supported EventStore metadata contract conservatively, and give the member region its own immutable snapshot and ETag. Keep EventStore exclusively for commands/status, and let notifications request re-query without becoming evidence.

## Boundaries & Constraints

**Always:** Relay the authenticated bearer token only server-side; URI-escape literal route identities and use contract-supported query parameters; propagate cancellation; consume the shared `QueryResponseMetadata`, `ReadModelFreshnessState`, and projection lifecycle types; require projection-backed provenance plus supported strong ETag/version/freshness evidence before retaining a `304`; treat missing, malformed, weak, contradictory, degraded, or non-projection metadata as unknown/fail-closed; keep last-confirmed data separate from loading/refresh intent; keep each read's ETag/freshness independent; preserve authorization-safe absence and support-safe diagnostics.

**Block If:** Any direct endpoint or required metadata is absent from the consumed contracts/runtime; safe bearer relay, service discovery, or notification subscription requires a shared-package/submodule or public-contract change; or correct member action availability cannot be obtained by conservatively composing the existing detail and tenant-users contracts. A missing live composing-host `Tenants:BaseAddress` is evidence-limiting `HOST-REF-1`, not authority to edit AppHost or fabricate runtime proof.

**Never:** Call `POST /api/v1/queries`, `QueryRouter`, `HandlerAwareQueryRouter`, projection actors, Tenants APIs from the browser, or the transitional repository AppHost for new shared orchestration; alter command submission/status routes or public backend/query contracts; infer current from HTTP success, non-empty data, `ServedAt`, request/cache/notification time, command state, or another read; claim wire `aging`; expose raw headers, ETags, cursors, tokens, Problem Details, payloads, correlations, or stack traces in rendered state, announcements, clipboard, logs, or telemetry.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Direct 200 | Valid payload plus supported projection metadata | Deserialize the expected contract, retain internal ETag/version/freshness, render current/stale/unknown honestly | Malformed payload or metadata becomes support-safe degraded/unknown; never invent evidence |
| Conditional 304 | Prior snapshot and strong ETag; response repeats supported projection metadata | Retain only that read's last-confirmed payload and classify from response metadata | Missing/weak/contradictory metadata cannot prove recovery/current and fails closed |
| Empty or denied | Authorized empty, 401/403, or 404 as defined per surface | Preserve distinct empty, unauthorized, or not-found semantics with no existence leak | Do not deserialize Problem Details into UI state or log raw response content |
| Separate dependencies | Tenants configured without EventStore, or EventStore without Tenants | Reads and commands resolve independently; unavailable side fails closed | No all-or-nothing registration and no fallback to the other service |
| Member read | Tenant-users page differs from detail payload/version | Rows and paging use the dedicated member snapshot; owner-count/risk context composes only mutually current, version-consistent detail and member evidence | Mismatch, incomplete, or unknown evidence disables sensitive actions; command confirmation stays on its existing authoritative detail re-query |
| Refresh nudge | Projection notification or terminal command signal | Mark affected read refreshing and perform an authoritative direct GET while retaining last-confirmed data | Signal alone changes no freshness, confirmation, audit availability, or payload |
| Transport failure | Timeout, cancellation, invalid JSON/header, network/5xx | Preserve applicable last-confirmed read-only data as stale/degraded/unknown with safe retry | Caller cancellation propagates; no unsafe diagnostics cross the BFF |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- currently couples query and command availability to `EventStore:BaseAddress`; must register the two clients independently.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` and `ITenantQueryGateway.cs` -- current six UI surfaces build generic `SubmitQueryRequest`s; search detail hydration also passes through this seam.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `Components/Tenants/Members/MemberAccessReview.razor` -- currently render and confirm member commands from `TenantDetail.Members`.
- `src/Hexalith.Tenants.UI/State/` -- immutable per-surface snapshots already model last-confirmed ETag/freshness; add equivalent tenant-users and nudge state without duplicating shared enums.
- `src/Hexalith.Tenants.Contracts/Queries/` -- authoritative six route/query/payload contracts; consume unchanged.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`, `TenantsUiCompositionTests.cs`, and component/state tests -- focused transport, separation, freshness, nudge, and regression evidence.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` -- existing PLAT-FRESH-1 route/header evidence to reverify, not rewrite unless a Tenants-owned regression is found.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsRestQueryClient.cs`, `TenantsRestQueryClient.cs`, `TenantsRestQueryResponse.cs`, and `TenantsRestQueryFailureKind.cs` -- implement the typed server-side GET seam for exactly list, detail, tenant users, user tenants, audit, and global administrators; build only the documented paths/query fields; send conditional ETags; parse supported response headers case-insensitively into shared metadata; validate 304 evidence; map status/transport failures to fixed support-safe categories; avoid response-content/default HTTP logging.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- register the REST client from `Tenants:BaseAddress` with FrontComposer bearer relay and service discovery, independently register command/status dependencies from `EventStore:BaseAddress`, and resolve unavailable gateways per missing side without fallback. Do not edit AppHost.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` and `ITenantQueryGateway.cs` -- replace every generic query submission with its direct typed GET; retain existing conservative snapshot semantics and Memories candidate hydration; add the tenant-users operation; delete query-client dependency without changing command/status behavior.
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersRequest.cs`, `TenantUsersSnapshot.cs`, `TenantUsersReason.cs`, and `TenantUsersSurfaceKind.cs` -- add immutable cursor paging, ETag/projection version, shared freshness/lifecycle, last-confirmed preservation, authorization-safe absence, and support-safe `ToString`; no rendered/internal metadata leakage.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `Components/Tenants/Members/MemberAccessReview.razor` -- load and refresh detail and tenant users independently; render member loading/empty/unavailable/stale/degraded/unknown and cursor navigation; source rows only from tenant users, but preserve detail-owned owner count and existing detail-based command confirmation. Enable member actions only when the required detail/member evidence is current and projection-version consistent; retain last-confirmed rows while refreshing.
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs` and `Components/Pages/{TenantsWorkspace,MyTenantsPage,UserMembershipLookupPage,TenantDetailPage,TenantAuditPage,GlobalAdministratorsPage}.razor` -- wrap the existing optional `IProjectionSubscription` and tenant-aware notifier contracts, subscribe/unsubscribe canonical projection/scope pairs, coalesce matching nudges, retain last-confirmed snapshots under a client-only refreshing intent, and invoke the appropriate gateway re-query. Missing notification services degrade to manual/command-triggered refresh and never block direct reads.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs` and `TenantQueryGatewayTests.cs` -- prove all six exact methods/routes/query parameters, escaping, bearer-handler composition, 200/304/empty/401/403/404/5xx, strong/weak/missing/contradictory metadata, authoritative `IsStale`/lifecycle independent of `ServedAt`, cancellation, safe failures, distinct ETags, no generic query calls, and unchanged Memories hydration semantics.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`, `State/TenantUsersSnapshotTests.cs`, `Services/TenantReadRefreshSubscriptionTests.cs`, `Components/TenantDetailSurfaceTests.cs`, member/page tests, and command regression tests -- prove independent dependency registration, missing-host fail-closed behavior, member paging/state/version separation, last-confirmed refreshing, scoped subscription cleanup/coalescing/nudge-only behavior, EN/FR/accessibility selectors, no unsafe DOM/log/diagnostic data, and unchanged command/status endpoints.
- `_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md` and `tests/test-summary.md` -- record exact commands/results, six-route negative proof for `/api/v1/queries`, PLAT-FRESH-1 evidence, HOST-REF-1 live-host status, support-safety findings, and any owned external blocker without claiming it closed.

**Acceptance Criteria:**
- Given any of the six UI reads, when the BFF executes it, then the exact direct Tenants REST GET is used with server-side bearer relay and no generic EventStore query path, projection actor, or browser backend client is reachable.
- Given independently present or absent `Tenants:BaseAddress` and `EventStore:BaseAddress`, when services resolve, then query and command/status availability follow only their owning reference and all existing command endpoints remain unchanged.
- Given supported 200/304/empty/authorization-safe responses, when snapshots resolve, then only projection-backed ETag/version/freshness evidence determines current/stale/unknown, metadata-deficient 304 never proves recovery, and `ServedAt`/signals never determine projection age.
- Given tenant detail and tenant-users responses have different paging, versions, ETags, or failures, when the detail page renders or a membership action evaluates, then rows/paging use only tenant-users data, owner/risk context is composed only from mutually current version-consistent evidence, command confirmation retains its detail re-query, and indeterminate action inputs fail closed.
- Given a notification or terminal command signal, when an affected surface handles it, then it issues an authoritative re-query, retains last-confirmed data during refreshing, and the signal alone advances no freshness, command confirmation, or audit availability.
- Given transport, authorization, malformed-response, or support-safety regression cases, when gateway/component tests run, then empty, unauthorized, not-found, error, degraded, stale, and unknown remain distinct and no unsafe internal value reaches DOM, announcements, clipboard, logs, telemetry tags, or diagnostics.
- Given the focused UI and existing generated-controller suites, when verification runs, then all six routes, 200/304 metadata, client separation, negative generic-query usage, member-state separation, and command regressions pass; any live `HOST-REF-1` or external platform gap is recorded precisely rather than worked around.

## Spec Change Log

## Review Triage Log

## Design Notes

The detail payload may continue to contain `Members` for contract compatibility and existing command-confirmation/owner-count evidence, but it must not supply visible member rows or certify tenant-users freshness. Because `PaginatedResult<TenantMember>` carries no global owner count, compose risk/action availability only when both direct reads are current and share supported projection-version evidence; otherwise keep the table readable and actions unavailable. Do not weaken or replace the existing projection-confirmed command lifecycle.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- expected: direct-client, gateway, component/state, composition, support-safety, localization, and command regressions pass.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` -- expected: all six direct routes and supported metadata/304 behavior pass.
- `dotnet test Hexalith.Tenants.slnx --no-restore` -- expected: repository regression suite passes, or an exact environment/external blocker is recorded.
- `rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` -- expected: no Tenants UI read path uses the generic query route; command/status code remains outside this prohibition.
