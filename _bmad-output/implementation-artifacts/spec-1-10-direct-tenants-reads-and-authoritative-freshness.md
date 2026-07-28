---
title: 'Story 1.10: Direct Tenants Reads and Authoritative Freshness'
type: 'feature'
created: '2026-07-28'
status: 'in-progress'
baseline_commit: '8d64563c75423c861b0be0e3a7cc4de18f673d37'
baseline_revision: '54fabf9852168b7e1f1639f9253472889397915a'
review_loop_iteration: 2
followup_review_recommended: true
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
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs` -- accept only HTTP/HTTPS base addresses; encode literal route segments so dot-only identifiers cannot be normalized as `.` or `..`; bound both request and response ETags; accept payloads only from `200 OK`; reject every other non-304 status, including other 2xx responses; validate paginated payload shape (`Items`, `HasMore`, continuation cursor); and map header-stage plus body-stream `TaskCanceledException`, `HttpRequestException`, and `IOException` to fixed timeout/unavailable results while propagating caller cancellation.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs` and `TenantQueryGateway.cs` -- accept `304 Not Modified` only when a supported strong request validator was actually sent and the response repeats the exact normalized strong ETag with projection-backed, non-degraded version/freshness evidence. Never relabel retained payload with a different ETag or projection version. Do not infer `invalid-cursor` from an undifferentiated HTTP 400 or automatically retry it as page one without an explicit safe contract signal.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- register the REST client from `Tenants:BaseAddress` with FrontComposer bearer relay and service discovery, independently register command/status dependencies from `EventStore:BaseAddress`, and resolve unavailable gateways per missing side without fallback. Do not edit AppHost.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` and `ITenantQueryGateway.cs` -- replace every generic query submission with its direct typed GET; retain existing conservative snapshot semantics and Memories candidate hydration; add the tenant-users operation; delete query-client dependency without changing command/status behavior. Every transient failure during a refresh must retain the matching read's last-confirmed rows, ETag, paging context, and honest degraded/unknown state; a first-load failure with no retained evidence must use a true unavailable/error state rather than claiming retained degradation.
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersRequest.cs`, `TenantUsersSnapshot.cs`, `TenantUsersReason.cs`, and `TenantUsersSurfaceKind.cs` -- add immutable cursor paging, ETag/projection version, shared freshness/lifecycle, last-confirmed preservation, authorization-safe absence, and support-safe `ToString`; no rendered/internal metadata leakage.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `Components/Tenants/Members/MemberAccessReview.razor` -- load and refresh detail and tenant users independently; render member loading/empty/unavailable/stale/degraded/unknown and cursor navigation; source rows only from tenant users, but preserve detail-owned owner count and existing detail-based command confirmation. Enable member actions and show combined owner/total governance claims only when the required detail/member evidence is current and projection-version consistent; label a paged row count as the visible-page count. Retain last-confirmed rows while refreshing, reset cursor/history after page-one recovery, reject `HasMore` without a usable continuation cursor, and suppress duplicate paging requests while a load is active.
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs` and consuming pages -- coordinate subscriptions by exact projection/scope with reference-counted leases so one component cannot unsubscribe another; cancel or safely dispose late subscription setup after component disposal; coalesce matching nudges; and emit only bounded reason-coded diagnostics without literal tenant/user identifiers or transport metadata. Subscribe only where the actual notification producer is proven to emit the exact pair; do not subscribe `tenant-index:system` or another invented pair when the producer emits only per-tenant `tenants`. An unroutable optional notification remains a precisely recorded manual/command-refresh limitation and is not authority to change a shared package.
- `src/Hexalith.Tenants.UI/Components/Pages/{TenantDetailPage,TenantAuditPage,GlobalAdministratorsPage}.razor` and other refresh consumers -- use a captured route/scope plus monotonic load generation or equivalent cancellation/serialization for initial, notification, manual, filter, and paging loads. Dispose/rebind the old lease before new-scope work; never apply an obsolete completion; never let rapid paging corrupt cursor history; and never create privileged/global-administrator subscriptions while unauthorized or disconnected.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs` and `TenantQueryGatewayTests.cs` -- prove all six exact methods/routes/query parameters, escaping, bearer-handler composition, 200/304/empty/401/403/404/5xx, strong/weak/missing/contradictory metadata, authoritative `IsStale`/lifecycle independent of `ServedAt`, cancellation, safe failures, distinct ETags, no generic query calls, and unchanged Memories hydration semantics.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs` -- additionally prove no-validator and different-validator 304 rejection, exact-validator acceptance, non-200 2xx rejection, oversized ETag rejection, dot-only route escaping, null paginated items, `HasMore` without cursor, header/body timeout and transport exceptions, and caller-cancellation propagation.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` -- execute a real configured typed read through the registered Tenants client and a recording primary handler; assert the expected relayed `Authorization: Bearer` header when enabled and its absence when disabled, rather than counting handler registrations.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs`, `Services/TenantReadRefreshSubscriptionTests.cs`, `Components/TenantDetailSurfaceTests.cs`, member/page tests, and command regression tests -- prove independent dependency registration; first-load versus retained failure truth; reference-counted subscription cleanup and late-disposal safety; exact producer/subscriber projection pairs; no unauthorized privileged subscription; route-change and overlapping-load stale-completion rejection; member paging clicks, cursor recovery/history, and duplicate-click suppression; disjoint detail-only versus tenant-users-only fixtures proving visible rows/actions use the dedicated read; last-confirmed refreshing; EN/FR/accessibility selectors; no unsafe DOM/log/diagnostic data; and unchanged command/status endpoints.
- `_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md` and `tests/test-summary.md` -- record exact commands/results, six-route negative proof for `/api/v1/queries`, PLAT-FRESH-1 evidence, HOST-REF-1 live-host status, notification producer/subscriber compatibility, support-safety findings, and any owned external blocker without claiming it closed. The three `references/` gitlinks must remain at `baseline_commit`; do not record a gitlink PASS until the exact validator command has exited 0 against the final tree.

**Acceptance Criteria:**
- Given any of the six UI reads, when the BFF executes it, then the exact direct Tenants REST GET is used with server-side bearer relay and no generic EventStore query path, projection actor, or browser backend client is reachable.
- Given independently present or absent `Tenants:BaseAddress` and `EventStore:BaseAddress`, when services resolve, then query and command/status availability follow only their owning reference and all existing command endpoints remain unchanged.
- Given supported 200/304/empty/authorization-safe responses, when snapshots resolve, then only projection-backed ETag/version/freshness evidence determines current/stale/unknown, metadata-deficient 304 never proves recovery, and `ServedAt`/signals never determine projection age.
- Given tenant detail and tenant-users responses have different paging, versions, ETags, or failures, when the detail page renders or a membership action evaluates, then rows/paging use only tenant-users data, owner/risk context is composed only from mutually current version-consistent evidence, command confirmation retains its detail re-query, and indeterminate action inputs fail closed.
- Given a notification or terminal command signal, when an affected surface handles it, then it issues an authoritative re-query, retains last-confirmed data during refreshing, and the signal alone advances no freshness, command confirmation, or audit availability.
- Given transport, authorization, malformed-response, or support-safety regression cases, when gateway/component tests run, then empty, unauthorized, not-found, error, degraded, stale, and unknown remain distinct and no unsafe internal value reaches DOM, announcements, clipboard, logs, telemetry tags, or diagnostics.
- Given the focused UI and existing generated-controller suites, when verification runs, then all six routes, 200/304 metadata, client separation, negative generic-query usage, member-state separation, and command regressions pass; any live `HOST-REF-1` or external platform gap is recorded precisely rather than worked around.

## Spec Change Log

### 2026-07-28 — Review repair loop 1

- **Triggering findings:** The first implementation accepted mismatched/no-validator 304s, let body-stream failures escape, inferred cursor recovery from every 400, dropped retained reads on refresh failure, allowed stale route/paging completions, used unroutable or uncoordinated notification subscriptions, exposed misleading member totals, and lacked security/user-surface tests for bearer relay, member authority, paging, and notification wiring. It also committed three undeclared `references/` gitlinks while the evidence claimed the gitlink guard passed.
- **Amendment:** Tightened transport/status/payload/ETag invariants; made prior-state retention and first-load truth explicit; added exact route/load-generation, paging, subscription reference-count/disposal/producer-compatibility, authorization, and member-summary rules; required outbound bearer observation and component-level negative evidence; and bound evidence completion to a real final gitlink-validator exit code.
- **Known-bad state avoided:** Old payload can no longer be certified by unrelated cache metadata; cross-route or late refreshes cannot overwrite the active tenant/page; one lease cannot detach another; synthetic `tenant-index:system` subscriptions cannot masquerade as live refresh; transient failures cannot erase confirmed rows; and story completion cannot silently absorb dependency pointer changes.
- **KEEP:** Preserve the six typed direct REST methods and exact routes; independent `Tenants:BaseAddress` versus `EventStore:BaseAddress` registration; absence of generic query calls from production Tenants UI; server-only bearer relay/service discovery; conservative shared metadata/lifecycle types; dedicated immutable tenant-users snapshots and version-consistent action gating; optional nudge-only refresh semantics; existing EN/FR/accessibility work; focused controller/UI verification; unchanged AppHost, public backend/query contracts, command/status endpoints, and baseline submodule pointers.

## Review Triage Log

### 2026-07-28 — Review pass
- intent_gap: 0
- bad_spec: 27: (high 17, medium 10, low 0)
- patch: 0
- defer: 0
- reject: 3: (high 0, medium 3, low 0)
- addressed_findings:
  - `[high]` `[bad_spec]` Restore the three undeclared gitlinks to baseline and bind evidence to the validator's real exit code.
  - `[high]` `[bad_spec]` Stop subscribing list/user surfaces to the unroutable `tenant-index:system` pair and verify producer compatibility.
  - `[high]` `[bad_spec]` Bind 304 retention to the exact sent strong validator and reject mismatched or missing validators.
  - `[high]` `[bad_spec]` Map body-stream timeouts and transport failures to fixed support-safe results.
  - `[high]` `[bad_spec]` Stop treating every undifferentiated HTTP 400 as an invalid cursor.
  - `[high]` `[bad_spec]` Preserve matching last-confirmed rows and paging context on transient refresh failures.
  - `[high]` `[bad_spec]` Discard obsolete tenant-detail/member completions after a route change.
  - `[high]` `[bad_spec]` Serialize or generation-guard notification, manual, filter, and paging refreshes.
  - `[high]` `[bad_spec]` Version-gate combined member/owner claims and label paged counts honestly.
  - `[high]` `[bad_spec]` Observe a real outbound bearer header through the configured typed client.
  - `[high]` `[bad_spec]` Prevent unauthorized or disconnected global-administrator subscriptions.
  - `[high]` `[bad_spec]` Reference-count identical projection/scope leases so one consumer cannot detach another.
  - `[high]` `[bad_spec]` Reject non-200 success responses as authoritative payloads.
  - `[high]` `[bad_spec]` Encode dot-only route identities so URI normalization cannot change the target resource.
  - `[high]` `[bad_spec]` Suppress concurrent member-page requests that can corrupt history or overwrite newer rows.
  - `[high]` `[bad_spec]` Add component tests proving notifications retain state and trigger only matching authoritative re-queries.
  - `[high]` `[bad_spec]` Use disjoint detail/member fixtures to prove visible rows and actions are tenant-users authoritative.
  - `[medium]` `[bad_spec]` Rebind the tenant-audit subscription when the routed tenant changes.
  - `[medium]` `[bad_spec]` Reset member cursor/history when an explicit page-one recovery occurs.
  - `[medium]` `[bad_spec]` Reject non-HTTP/HTTPS Tenants base-address schemes during composition.
  - `[medium]` `[bad_spec]` Emit bounded reason-coded diagnostics for notification setup, callback, and cleanup failures.
  - `[medium]` `[bad_spec]` Dispose a subscription lease that completes after its component has already been disposed.
  - `[medium]` `[bad_spec]` Validate paginated payload items instead of accepting a null collection.
  - `[medium]` `[bad_spec]` Bound strong response and request ETags before retaining or forwarding them.
  - `[medium]` `[bad_spec]` Represent a first-load failure without prior evidence as error/unavailable, not retained degradation.
  - `[medium]` `[bad_spec]` Reject `HasMore` without a usable continuation cursor.
  - `[medium]` `[bad_spec]` Exercise rendered member next/previous controls and assert the exact gateway cursors and history.

## Design Notes

The detail payload may continue to contain `Members` for contract compatibility and existing command-confirmation/owner-count evidence, but it must not supply visible member rows or certify tenant-users freshness. Because `PaginatedResult<TenantMember>` carries no global owner count, compose risk/action availability only when both direct reads are current and share supported projection-version evidence; otherwise keep the table readable and actions unavailable. Do not weaken or replace the existing projection-confirmed command lifecycle.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- expected: direct-client, gateway, component/state, composition, support-safety, localization, and command regressions pass.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` -- expected: all six direct routes and supported metadata/304 behavior pass.
- `dotnet test Hexalith.Tenants.slnx --no-restore` -- expected: repository regression suite passes, or an exact environment/external blocker is recorded.
- `rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` -- expected: no Tenants UI read path uses the generic query route; command/status code remains outside this prohibition.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` -- expected: exit 0 with no undeclared `references/` pointer movement.

### Review Findings

Review pass 2 — 2026-07-28. Four layers (blind-hunter, edge-case-hunter, verification-gap,
acceptance-auditor) over `8d64563..HEAD` (70 files, +6,873/-704). Deduped and re-verified against
source before rating.

#### Decisions resolved — 2026-07-28

- [x] [Review][Decision] Story 1.11 implementation bundled into this diff — **RESOLVED: split 1.11 back out.** The global-administrator authorization work (`TenantsGlobalAdministratorClaims.cs`, `TenantConfigurationPrincipalResolver.cs`, `TenantsWorkspace.razor` entry point, `GlobalAdministrators*` `IsCompleteEvidence`) is extracted from 1.10 and re-lands under Story 1.11 with its own review loop. See patch item below.
- [x] [Review][Decision] Principal-resolution precedence inverted (circuit over `HttpContext`) — **RESOLVED: deferred to Story 1.11.** Decided against 1.11's acceptance criteria, where the change belongs. Not actionable inside 1.10.
- [x] [Review][Decision] `Evaluate` requires exactly one identity with exactly one literal `sub` claim — **RESOLVED: deferred to Story 1.11.** The claim contract must be confirmed against `docs/production-auth-claim-contract.md` as part of 1.11.
- [x] [Review][Decision] `Tenants:BaseAddress` forbidden by a passing guard test; transport unreachable — **RESOLVED: both guards treated as superseded.** The `ShouldNotContain("Tenants__BaseAddress")` assertion describes the pre-1.10 architecture and is retired; `Tenants__BaseAddress` is wired in AppHost, closing `HOST-REF-1`. This explicitly waives the spec's "do not edit AppHost" constraint, by owner decision. See patch items below.
- [x] [Review][Decision] Module-wide permanent ban on endpoint authorization — **RESOLVED: narrow the invariant.** The composition test is scoped to the Keycloak-disabled topology so OIDC deployments retain defence-in-depth on `/global-administrators`. See patch item below.
- [x] [Review][Decision] `ResolveFreshness` treats `IsStale: false` + absent lifecycle as `Current` — **RESOLVED: platform contract governs; behavior kept as-is.** `X-Hexalith-Is-Stale` is the documented freshness wire signal and `Aging` is dormant on the wire, so a definite `false` is legitimate projection-backed evidence. Finding dismissed; a call-site comment records the rationale. See patch item below.
- [x] [Review][Decision] `epic-1-context.md` rewritten inside the story diff — **RESOLVED: restore the constraints and fix the code.** All four deleted constraints remain binding; the cursor-recovery constraint forces the dead invalid-cursor path to be repaired rather than documented away. See patch item below.

#### Patch

- [x] [Review][Patch] [from D1] Split Story 1.11's authorization work out of the 1.10 range and re-land it under 1.11 — `TenantsGlobalAdministratorClaims.cs`, `TenantConfigurationPrincipalResolver.cs`, `TenantsWorkspace.razor` privileged entry point, `GlobalAdministrators*` `IsCompleteEvidence` [commits 7d7b701, 536596f]
- [x] [Review][Patch] [from D2] Retire the superseded `Tenants__BaseAddress` prohibition [tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs:87]
- [x] [Review][Patch] [from D2] Wire `Tenants__BaseAddress` for the `tenants-ui` resource so the six direct reads resolve a real gateway, closing HOST-REF-1 [src/Hexalith.Tenants.AppHost/Program.cs:135-144]
- [x] [Review][Patch] [from D4] Scope the endpoint-authorization invariant to the Keycloak-disabled topology instead of banning `IAuthorizeData` module-wide [tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs:608-627]
- [x] [Review][Patch] [from D6] Record at the call site why lifecycle-absent + `IsStale: false` classifies as `Current` under the platform wire contract [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:303-307]
- [x] [Review][Patch] [from D7] Restore the four constraints deleted from `epic-1-context.md`, including the page-one cursor-recovery notice [_bmad-output/implementation-artifacts/epic-1-context.md]
- [x] [Review][Patch] Four `references/` gitlinks moved undeclared; the evidence record claims the validator passed [references/Hexalith.{Builds,EventStore,FrontComposer,Memories}]
- [x] [Review][Patch] Recorded verification results were produced against a different dependency graph than HEAD [_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md:165]
- [x] [Review][Patch] Circular DI: resolving `ITenantsBffComposition` throws once `Tenants:BaseAddress` is set (empirically reproduced) [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:13]
- [x] [Review][Rejected] The global-administrators 304 branch relabels retained rows with the response's `ProjectionVersion`; six sibling reads keep `previous.ProjectionVersion` [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:492]
- [x] [Review][Patch] `CanRetain*` requires a non-empty `request.ETag`, so an unconditional Retry/refresh skips retention and collapses to `Error()` with empty rows [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1786-1794]
- [x] [Review][Patch] Last-admin hard stop fails open: `HasPositiveRemovalPopulationEvidence` accepts client-local `_cursorHistory.Count > 0` and `HasMore` as proof other administrators exist [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:661-664]
- [x] [Review][Patch] `GetTenantUsersAsync` is the only gateway read with no exception containment; a `UriFormatException`/`InvalidOperationException`/handler fault tears down the circuit [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:258-290]
- [x] [Review][Patch] Invalid-cursor page-one recovery is unreachable in production — `ToEventStoreResult` emits `reasonCode = "InvalidRequest"`, never `"invalid-cursor"`; the passing tests fabricate the reason code at a seam production cannot reach [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2118-2120]
- [ ] [Review][Patch] `ToEventStoreResult`'s failure branch is executed by zero tests; every read-failure test injects the exception upstream of it via `RestQueryClientAdapter`, which hardcodes `FailureKind.None` [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1910-1918]
- [ ] [Review][Patch] The story's new sixth read is asserted against 2 of 11 surface kinds and 1 of 12 reasons; `Degraded`/`Error`/`Empty`/`NotFound`/`Unauthorized` and the tenant-id-mismatch retention rule are unverified [tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs]
- [ ] [Review][Patch] The whole `GlobalAdministratorsPage` authorization-transition machinery never executes in any test — no `AuthenticationStateProvider` is registered in that fixture, so the handler is never attached [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:750-756]
- [x] [Review][Patch] Tenant-users first-load failure renders `Degraded` with zero retained rows instead of a true error state; the audit sibling returns `Error` [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1698-1704]
- [x] [Review][Patch] Mobile read-only banner is permanently visible on desktop — `.global-admins__mobile-readonly` is applied to a `FluentMessageBar`, which receives no CSS-isolation scope attribute, and the file has zero `::deep` selectors [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:7]
- [x] [Review][Patch] The per-row Remove launcher is not inside any `.global-admins__mutation-section` and sits on a Fluent component, so the mobile hide rule cannot match it [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:440]
- [x] [Review][Patch] The only mobile read-only test is a regex over the raw `.razor.css` text; it passes while both defects above are live [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:976]
- [x] [Review][Patch] `IsReadSurfaceConnected` fails open when the query gateway is null; correct polarity is `is not null and not UnavailableTenantQueryGateway` [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:14]
- [x] [Review][Patch] `CreateRequestUri` discards any path component of the configured base address via `GetLeftPart(UriPartial.Authority)`; a path-based ingress silently 404s and renders as authorization-safe absence [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:461]
- [x] [Review][Patch] The `http`/`https`-only scheme gate rejects the Aspire service-discovery form (`https+http://tenants`) while `.AddServiceDiscovery()` is attached to the same client, silently disabling all reads [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:106-118]
- [x] [Review][Patch] The scheme guard was applied to `Tenants:BaseAddress` only; `EventStore:BaseAddress` still accepts any absolute scheme including `file://` [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:72]
- [x] [Review][Patch] `InternalsVisibleTo("DynamicProxyGenAssembly2")` was added to the shipping UI assembly to satisfy a mocking framework, with no `#if DEBUG` guard [src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj:14]
- [x] [Review][Patch] `.RemoveAllLoggers()` on the primary read client leaves all six canonical reads with no telemetry; combined with the base-path bug and `MapFailure`'s status collapse, an outage produces zero correlatable log lines [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:58]
- [x] [Review][Patch] Member paging controls render only inside the non-empty branch, so a page emptied by concurrent removals strands the user with no Previous control [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:233-250]
- [x] [Review][Patch] Paging into `Unauthorized`/`NotFound`/`Invalid` still advances `_memberCursor` and pushes cursor history, so Previous walks a history built from failed reads [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:473-483]
- [x] [Review][Patch] Member page loads pass `previous: null`, bypassing the gateway's tested conditional-read and retention path for the one flow it was written for [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:464]
- [x] [Review][Patch] `Task.WhenAll` rethrows only the first fault; a cancelled detail task plus a faulted members task leaves the page stuck in `Loading` with an unobserved exception [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:305-312]
- [x] [Review][Patch] `BeginLoad()` disposes a `CancellationTokenSource` whose token is still in flight; a later `Register` throws `ObjectDisposedException`, which passes through the `OperationCanceledException` filters [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1030-1036]
- [x] [Review][Patch] Cancelling or refreshing an in-flight command leaves `_isGrantSubmitting`/`_isRemoveSubmitting` stuck true for the rest of the circuit, permanently disabling grant and remove [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1339-1342]
- [x] [Review][Patch] Projection re-query resets the view to page one without clearing `_currentCursor`/`_cursorHistory`, so Previous is enabled on page one and Refresh re-requests a stale cursor [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1395-1401]
- [x] [Review][Patch] A transient notification-setup failure returns `TenantReadRefreshLease.Empty` without registering the callback, but the caller stores it — auto-refresh is then permanently disabled for the circuit [src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs:88-98]
- [x] [Review][Patch] `TenantAuditPage.RefreshFromNotificationAsync` has no non-cancellation failure path and its `finally` calls `InvokeAsync` with no `_disposed` guard, unlike the global-administrators page [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:475-492]
- [x] [Review][Patch] The audit-page subscription has no in-flight guard; a concurrent `OnParametersSetAsync` for the same tenant subscribes twice and orphans the first lease [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:433-465]
- [x] [Review][Patch] `TenantAuditPage` mutates `_cursorHistory`/`_currentCursor`/`ResolveReceiptSelection()` off the renderer dispatcher after `ConfigureAwait(false)`, while the adjacent `_snapshot` write is deliberately marshalled [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:404-410]
- [x] [Review][Patch] `_loadGeneration` and `_mutationGeneration` use plain read-modify-write from `ConfigureAwait(false)` continuations, while `_authorizationTransitionVersion` correctly uses `Interlocked` [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1030-1054]
- [x] [Review][Patch] An `OperationCanceledException` that is not a `TaskCanceledException` and not the caller's token matches neither catch filter and escapes the transport's fixed failure mapping [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:144-159]
- [x] [Review][Patch] `retainConfirmed` was added to three components that never pass it, advertising retained-refresh behavior those surfaces do not have [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:656]
- [x] [Review][Patch] `ITenantQueryGateway.GetTenantUsersAsync` is a default interface method returning `Unavailable`; any implementation that omits it degrades the member table with no compile error, unlike every other member [src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs:31-38]
- [x] [Review][Patch] `Tenants.GlobalAdministrators.Grant.Unavailable.Incomplete` was added to both resx files but has no call site; the grant path has no completeness gate while the mirror-image remove path does [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx]
- [x] [Review][Patch] `UnavailableTenantQueryGateway.GetTenantUsersAsync` has no test and is the only method on the type that dereferences its request without `ArgumentNullException.ThrowIfNull` [src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs:17-21]
- [ ] [Review][Patch] Five new recovery/refresh affordances have zero test references (`tenants-global-admins-notification-refreshing`, `-page-recovered`, `-retry`, `-reset`, `tenants-audit-notification-refreshing`) [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:751]
- [ ] [Review][Patch] `TenantAuditPage`'s new subscription, tenant rebinding and load-generation guards are untested; its test file was not touched by the story [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs]
- [ ] [Review][Patch] `tests/test-summary.md` claims member "page-one recovery" and "duplicate suppression" coverage; the recovery branch is unreachable (`isListRefreshed: false` at the only call site) and no test clicks twice while a load is in flight [tests/test-summary.md]
- [ ] [Review][Patch] `Request_and_response_etags_are_bounded_and_malformed_values_fail_closed` passes because of the validator mismatch, not the bound; deleting both ETag bounds keeps it green [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:207-231]
- [ ] [Review][Patch] Body-phase caller-cancellation propagation is never exercised — the test cancels before the request, so the header stage throws first [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:398-411]
- [ ] [Review][Patch] The principal resolver's mid-flight cancellation filter is untested; the cancellation test short-circuits at the entry guard [tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs:53]
- [ ] [Review][Patch] The subscription "no unsafe detail" assertion cannot observe an attached exception — `CapturingLogger` records only the formatted message, and the default formatter ignores the exception argument [tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:176-199]
- [ ] [Review][Patch] Gateway route/query assertions describe a shape only `RestQueryClientAdapter` produces; `QueryType`/`Domain`/`ProjectionType`/`EntityId` are hardcoded by the harness, so those assertions hold for any production behavior [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:4641-4744]
- [ ] [Review][Patch] The "ignore late disposed completion" half of the workspace auth test asserts nothing after `cut.Dispose()` [tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:147-181]
- [ ] [Review][Patch] `NormalizeReturnUrl`'s nine-key allowlist silently degrades return context to `/tenants` on any query key outside it, with no test coupling the allowlist to `TenantWorkspaceState.ToCanonicalUrl()` output [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1468-1515]

#### Rejected after verification

- [x] [Review][Rejected] "The global-administrators 304 branch relabels retained rows with the response's `ProjectionVersion`" — `Get_global_administrators_current_not_modified_promotes_unknown_truth_and_recomputes_completeness` pins the promotion, and it is sound: `IsSupportedNotModified` already requires a strong exact ETag match with projection-backed, versioned, non-degraded metadata, so the 304 proves the retained payload is identical to what the service holds at the newer version. Change reverted.
- [x] [Review][Rejected] "The grant path has no completeness gate" — `Incomplete_current_page_with_more_results_allows_safe_initiation` pins that grant is deliberately allowed on incomplete evidence; granting cannot remove the last administrator, so it needs no population evidence. The unused `Grant.Unavailable.Incomplete` key was dead copy and was deleted from both resx files (EN/FR parity 1,206/1,206). Change reverted.

#### Deferred

- [x] [Review][Defer] BMAD workflow render files were modified inside the story diff [_bmad/render/bmad-quick-dev/workflow.md] — deferred, tooling change unrelated to the story's ACs

## File List

Dependency pointers that moved inside this story's baseline range. They are declared here, not reverted:
the bump is already carried by its own separate `build(deps)` commit (`f425b49`), which is the remedy the
gitlink guard prescribes. That commit is published on `origin/main`, so reverting would mean a new commit
undoing a legitimate dependency bump rather than un-bundling anything.

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`

## Completion Notes List

- `references/Hexalith.Builds` — `53d53ae -> 86aa4cb`. Carries `HexalithMemoriesVersion 2.16.2 -> 2.19.4`
  and `HexalithTenantsVersion 3.2.18 -> 5.0.0`. Not Story 1.10 work; external dependency maintenance.
  The major Tenants bump means every verification lane recorded in the evidence file must be re-run
  against this pointer before the story can close.
- `references/Hexalith.EventStore` — `5a1d277 -> 7ab1f08` ("Docs/story 3 1 closure (#334)"). Not Story
  1.10 work; upstream documentation closure.
- `references/Hexalith.FrontComposer` — `7870526 -> b6efcad` (release-workflow SHA pin and identifier
  inventory metrics). Not Story 1.10 work; upstream release tooling.
- `references/Hexalith.Memories` — `1868c8f -> a451765` ("add story slice scope validation script"). Not
  Story 1.10 work; upstream tooling.

All four are reachable on their respective `origin/main`, so the superproject remains cloneable. The
earlier evidence record claimed these were reverted and that the validator exited 0; that claim was false
against this tree and has been corrected in the evidence file.
