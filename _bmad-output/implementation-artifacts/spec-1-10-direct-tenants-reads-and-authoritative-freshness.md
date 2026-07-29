---
title: 'Story 1.10: Direct Tenants Reads and Authoritative Freshness'
type: 'feature'
created: '2026-07-28'
status: 'in-progress'
baseline_commit: '8d64563c75423c861b0be0e3a7cc4de18f673d37'
baseline_revision: '54fabf9852168b7e1f1639f9253472889397915a'
review_loop_iteration: 4
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
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- register the REST client from `Tenants:BaseAddress` with FrontComposer bearer relay and service discovery, independently register command/status dependencies from `EventStore:BaseAddress`, and resolve unavailable gateways per missing side without fallback. ~~Do not edit AppHost.~~ **Superseded by review decision D2 (2026-07-28):** the constraint is waived for the single purpose of wiring `Tenants__BaseAddress` and the `tenants-api` reference onto the `tenants-ui` resource, which is what closes `HOST-REF-1`. No other AppHost change is authorized.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` and `ITenantQueryGateway.cs` -- replace every generic query submission with its direct typed GET; retain existing conservative snapshot semantics and Memories candidate hydration; add the tenant-users operation; delete query-client dependency without changing command/status behavior. Every transient failure during a refresh must retain the matching read's last-confirmed rows, ETag, paging context, and honest degraded/unknown state; a first-load failure with no retained evidence must use a true unavailable/error state rather than claiming retained degradation.
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersRequest.cs`, `TenantUsersSnapshot.cs`, `TenantUsersReason.cs`, and `TenantUsersSurfaceKind.cs` -- add immutable cursor paging, ETag/projection version, shared freshness/lifecycle, last-confirmed preservation, authorization-safe absence, and support-safe `ToString`; no rendered/internal metadata leakage.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `Components/Tenants/Members/MemberAccessReview.razor` -- load and refresh detail and tenant users independently; render member loading/empty/unavailable/stale/degraded/unknown and cursor navigation; source rows only from tenant users, but preserve detail-owned owner count and existing detail-based command confirmation. Enable member actions and show combined owner/total governance claims only when the required detail/member evidence is current and projection-version consistent; label a paged row count as the visible-page count. Retain last-confirmed rows while refreshing, reset cursor/history after page-one recovery, reject `HasMore` without a usable continuation cursor, and suppress duplicate paging requests while a load is active.
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs` and consuming pages -- coordinate subscriptions by exact projection/scope with reference-counted leases so one component cannot unsubscribe another; cancel or safely dispose late subscription setup after component disposal; coalesce matching nudges; and emit only bounded reason-coded diagnostics without literal tenant/user identifiers or transport metadata. Subscribe only where the actual notification producer is proven to emit the exact pair; do not subscribe `tenant-index:system` or another invented pair when the producer emits only per-tenant `tenants`. An unroutable optional notification remains a precisely recorded manual/command-refresh limitation and is not authority to change a shared package.
- `src/Hexalith.Tenants.UI/Components/Pages/{TenantDetailPage,TenantAuditPage,GlobalAdministratorsPage}.razor` and other refresh consumers -- use a captured route/scope plus monotonic load generation or equivalent cancellation/serialization for initial, notification, manual, filter, and paging loads. Dispose/rebind the old lease before new-scope work; never apply an obsolete completion; never let rapid paging corrupt cursor history; and never create privileged/global-administrator subscriptions while unauthorized or disconnected.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs` and `TenantQueryGatewayTests.cs` -- prove all six exact methods/routes/query parameters, escaping, bearer-handler composition, 200/304/empty/401/403/404/5xx, strong/weak/missing/contradictory metadata, authoritative `IsStale`/lifecycle independent of `ServedAt`, cancellation, safe failures, distinct ETags, no generic query calls, and unchanged Memories hydration semantics.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs` -- additionally prove no-validator and different-validator 304 rejection, exact-validator acceptance, non-200 2xx rejection, oversized ETag rejection, dot-only route escaping, null paginated items, `HasMore` without cursor, header/body timeout and transport exceptions, and caller-cancellation propagation.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` -- execute a real configured typed read through the registered Tenants client and a recording primary handler; assert the expected relayed `Authorization: Bearer` header when enabled and its absence when disabled, rather than counting handler registrations.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs`, `Services/TenantReadRefreshSubscriptionTests.cs`, `Components/TenantDetailSurfaceTests.cs`, member/page tests, and command regression tests -- prove independent dependency registration; first-load versus retained failure truth; reference-counted subscription cleanup and late-disposal safety; exact producer/subscriber projection pairs; no unauthorized privileged subscription; route-change and overlapping-load stale-completion rejection; member paging clicks, cursor recovery/history, and duplicate-click suppression; disjoint detail-only versus tenant-users-only fixtures proving visible rows/actions use the dedicated read; last-confirmed refreshing; EN/FR/accessibility selectors; no unsafe DOM/log/diagnostic data; and unchanged command/status endpoints.
- `_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md` and `tests/test-summary.md` -- record exact commands/results, six-route negative proof for `/api/v1/queries`, PLAT-FRESH-1 evidence, HOST-REF-1 live-host status, notification producer/subscriber compatibility, support-safety findings, and any owned external blocker without claiming it closed. Every moved `references/` gitlink must either remain at `baseline_commit` or be declared with its provenance; do not record a gitlink PASS until the exact validator command has exited 0 against the final tree.

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
- **KEEP:** Preserve the six typed direct REST methods and exact routes; independent `Tenants:BaseAddress` versus `EventStore:BaseAddress` registration; absence of generic query calls from production Tenants UI; server-only bearer relay/service discovery; conservative shared metadata/lifecycle types; dedicated immutable tenant-users snapshots and version-consistent action gating; optional nudge-only refresh semantics; existing EN/FR/accessibility work; focused controller/UI verification; public backend/query contracts, command/status endpoints, and baseline submodule pointers. AppHost is no longer "unchanged": review decision D2 authorized the `Tenants__BaseAddress` / `tenants-api` wiring on `tenants-ui`, and that wiring must be preserved — removing it reopens `HOST-REF-1`.

### 2026-07-29 — Review repair loop 3

- **Triggering findings:** Thirteen unchecked review items identified test-efficacy gaps around production-boundary failure mapping, snapshot state coverage, authorization transitions, rendered recovery affordances, audit rebinding/generation safety, member cursor recovery, independent ETag bounds, mid-flight cancellation, safe logging, direct gateway arguments, teardown behavior, and canonical return URLs.
- **Amendment:** Added focused tests at the production seams and component boundaries, then mutation-verified each assertion by temporarily removing or reversing the guarded behavior. The authorization-transition test exposed one production defect: a background administrator restore updated `_snapshot` without publishing a render; the page now calls `StateHasChanged()` after applying that result.
- **Known-bad state avoided:** Adapter-generated expectations can no longer substitute for direct typed-client arguments; entry cancellation cannot stand in for body-phase cancellation; formatted log text cannot hide an attached exception; late disposed authentication completions and stale audit generations are observable; cursor recovery, duplicate suppression, ETag limits, and canonical return-key coverage each fail when their production guard is removed.
- **KEEP:** Preserve the review-loop-2 transport, composition, freshness, authorization, paging, and host decisions. No public contract, command/status route, or dependency content changed in this loop. (AppHost *was* changed earlier in the range, by review decision D2 — see the Tasks entry above. The original "unchanged AppHost" wording was never amended after the waiver and is corrected here.)

### 2026-07-29 — Completion revalidation

- **Trigger:** The published `09947a2` dependency commit advanced `references/Hexalith.Memories` after the review-loop-3 evidence was recorded.
- **Validation:** Re-ran every repository test project individually in Release package mode, the generated-controller and non-performance integration lanes, the warning-as-error Release solution build, the two prohibited-symbol scans, whitespace checks, and the story gitlink validator against `09947a2` plus the completion metadata changes.
- **Result:** UI 1,507/1,507; generated-controller integration 26/26; non-performance integration 167/167; Contracts 120/120; Client 50/50; Testing 181/181; Server 738/738; Sample 39/39; solution build 0 warnings/0 errors; no prohibited generic-query or invented tenant-index symbols; all six moved gitlinks declared.

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
- [x] [Review][Patch] `ToEventStoreResult`'s failure branch is executed by zero tests; every read-failure test injects the exception upstream of it via `RestQueryClientAdapter`, which hardcodes `FailureKind.None` [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1910-1918]
- [x] [Review][Patch] The story's new sixth read is asserted against 2 of 11 surface kinds and 1 of 12 reasons; `Degraded`/`Error`/`Empty`/`NotFound`/`Unauthorized` and the tenant-id-mismatch retention rule are unverified [tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs]
- [x] [Review][Patch] The whole `GlobalAdministratorsPage` authorization-transition machinery never executes in any test — no `AuthenticationStateProvider` is registered in that fixture, so the handler is never attached [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:750-756]
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
- [x] [Review][Patch] Five new recovery/refresh affordances have zero test references (`tenants-global-admins-notification-refreshing`, `-page-recovered`, `-retry`, `-reset`, `tenants-audit-notification-refreshing`) [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:751]
- [x] [Review][Patch] `TenantAuditPage`'s new subscription, tenant rebinding and load-generation guards are untested; its test file was not touched by the story [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs]
- [x] [Review][Patch] `tests/test-summary.md` claims member "page-one recovery" and "duplicate suppression" coverage; the recovery branch is unreachable (`isListRefreshed: false` at the only call site) and no test clicks twice while a load is in flight [tests/test-summary.md]
- [x] [Review][Patch] `Request_and_response_etags_are_bounded_and_malformed_values_fail_closed` passes because of the validator mismatch, not the bound; deleting both ETag bounds keeps it green [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:207-231]
- [x] [Review][Patch] Body-phase caller-cancellation propagation is never exercised — the test cancels before the request, so the header stage throws first [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:398-411]
- [x] [Review][Patch] The principal resolver's mid-flight cancellation filter is untested; the cancellation test short-circuits at the entry guard [tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs:53]
- [x] [Review][Patch] The subscription "no unsafe detail" assertion cannot observe an attached exception — `CapturingLogger` records only the formatted message, and the default formatter ignores the exception argument [tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:176-199]
- [x] [Review][Patch] Gateway route/query assertions describe a shape only `RestQueryClientAdapter` produces; `QueryType`/`Domain`/`ProjectionType`/`EntityId` are hardcoded by the harness, so those assertions hold for any production behavior [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:4641-4744]
- [x] [Review][Patch] The "ignore late disposed completion" half of the workspace auth test asserts nothing after `cut.Dispose()` [tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:147-181]
- [x] [Review][Patch] `NormalizeReturnUrl`'s nine-key allowlist silently degrades return context to `/tenants` on any query key outside it, with no test coupling the allowlist to `TenantWorkspaceState.ToCanonicalUrl()` output [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1468-1515]

#### Rejected after verification

- [x] [Review][Rejected] "The global-administrators 304 branch relabels retained rows with the response's `ProjectionVersion`" — `Get_global_administrators_current_not_modified_promotes_unknown_truth_and_recomputes_completeness` pins the promotion, and it is sound: `IsSupportedNotModified` already requires a strong exact ETag match with projection-backed, versioned, non-degraded metadata, so the 304 proves the retained payload is identical to what the service holds at the newer version. Change reverted.
- [x] [Review][Rejected] "The grant path has no completeness gate" — `Incomplete_current_page_with_more_results_allows_safe_initiation` pins that grant is deliberately allowed on incomplete evidence; granting cannot remove the last administrator, so it needs no population evidence. The unused `Grant.Unavailable.Incomplete` key was dead copy and was deleted from both resx files (EN/FR parity 1,206/1,206). Change reverted.

#### Deferred

- [x] [Review][Defer] BMAD workflow render files were modified inside the story diff [_bmad/render/bmad-quick-dev/workflow.md] — deferred, tooling change unrelated to the story's ACs

### Review Findings — chunk 1 follow-up (2026-07-29)

- [x] [Review][Defer] Literal tenant/user identifiers containing `/` are not proven round-trippable through the six route-segment endpoints — deferred as a future feature requiring an explicit backend route contract; until then, this identifier class remains unsupported by the direct-read transport. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530]
- [x] [Review][Patch] Keep host-overridden query gateways supported and introduce an explicit matching read-surface availability contract, so a custom `ITenantQueryGateway` cannot disagree with the connected/disconnected state consumed by the UI; preserve the module-provided configuration-backed default and add override regression tests. [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:66]
- [x] [Review][Patch] Detect tenant/user identifiers containing `/` before constructing a route and fail closed with an explicit, deterministic client result plus regression tests, instead of issuing an ambiguous encoded-slash request that can surface as a misleading not-found response. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530]
- [x] [Review][Patch] Reject malformed compound schemes with empty segments instead of accepting values such as `http++https` [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:135]
- [x] [Review][Patch] Reject base addresses carrying user info, query strings, fragments, or no usable host instead of silently retargeting them [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:123]
- [x] [Review][Patch] Parse provenance and lifecycle headers only from the platform's exact canonical enum names so numeric or case-variant values cannot prove current projection truth [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:479]
- [x] [Review][Patch] Reject numeric payload enum values so malformed roles cannot become privileged `TenantOwner` evidence [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:35]
- [x] [Review][Patch] Enforce an actual byte bound and body-read deadline for both successful and Problem Details response streams, including chunked responses without `Content-Length` [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:200]
- [x] [Review][Patch] Map bad-request body cancellation, HTTP, and I/O failures to Timeout/Unavailable instead of swallowing them into InvalidRequest [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:393]
- [x] [Review][Patch] Preserve an effective service-unavailable status when Timeout/Unavailable failures occur after a 200 response so downstream gateway mappings do not misclassify first-load failures [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:214]
- [x] [Review][Patch] Reject null page elements and other structurally unusable DTOs before successful snapshots reach gateway row mapping [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:351]
- [x] [Review][Patch] Give the public failure enum an `Unknown = 0` fail-closed sentinel and string-enum serialization instead of treating the default value as success [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryFailureKind.cs:6]
- [x] [Review][Patch] Add a module-composition test that resolves `TenantReadRefreshSubscription` without manually registering it [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:46]
- [x] [Review][Patch] Exercise an actual request through the compound service-discovery handler pipeline instead of checking only the query-gateway descriptor [tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs:175]
- [x] [Review][Patch] Test malformed and non-HTTP `EventStore:BaseAddress` values while the independent Tenants read gateway remains available [tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs:196]
- [x] [Review][Patch] Pin `X-Hexalith-Is-Degraded` parsing for 200 and 304 responses so degraded evidence cannot regress to current [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:80]
- [x] [Review][Patch] Test plain non-caller `OperationCanceledException` at both header and body phases, not only `TaskCanceledException` [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:421]
- [x] [Review][Patch] Pin the supported lifecycle-absent 304 shape where projection-backed `X-Hexalith-Is-Stale: false` proves current freshness [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:134]
- [x] [Review][Patch] Resolve both gateways for all four Tenants/EventStore base-address combinations instead of validating only their service descriptors [tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs:196]
- [x] [Review][Patch] Complete XML documentation for the new public BFF method, REST-client methods, and positional response-record parameters [src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:19]
- [x] [Review][Defer] EventStore compound service-discovery addresses are accepted without attaching service discovery to the command/status clients [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:79] — deferred, pre-existing

### Review Findings — gateway transport and composition follow-up (2026-07-29)

- [x] [Review][Patch] [high] Stop tenant-users paging from sending the retained page's projection-wide ETag for a different cursor, and bind every retained 304 to matching request scope [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:294]
- [x] [Review][Patch] [high] Reject audit payload rows whose tenant identity differs from the requested tenant before they reach UI state [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:422]
- [x] [Review][Patch] [medium] Retry safely or return a true error/unavailable state when a paginated 304 has no matching retained snapshot instead of reporting empty retained degradation [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:400]
- [x] [Review][Patch] [medium] Preserve the explicit invalid-cursor signal and page-one recovery for My Tenants and user-membership reads [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:477]
- [x] [Review][Patch] [medium] Emit bounded failure telemetry for normal failure results across all six direct reads, not only unexpected tenant-users exceptions [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1988]
- [x] [Review][Patch] [medium] Contain unexpected typed-client and HTTP-pipeline failures consistently on list, user-tenants, global-administrator, and audit reads [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:477]
- [x] [Review][Patch] [medium] Add successful payload/deserialization coverage for detail, tenant-users, user-tenants, audit, and global-administrator REST responses [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:33]
- [x] [Review][Patch] [medium] Pin the tenant-users gateway mappings for authorization, absence, explicit invalid cursor, timeout/unavailability, and retained failures [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:204]
- [x] [Review][Patch] [medium] Ignore keyed DI registrations when detecting unkeyed host gateway/availability overrides [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:44]
- [x] [Review][Patch] [medium] Add the missing availability-only partial-override regression so both composition mismatch directions are enforced [tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs:195]
- [x] [Review][Patch] [low] Move the added availability interface, record, cursor-signal enum, and response-size exception into one-type-per-file declarations [src/Hexalith.Tenants.UI/Services/Gateways/TenantsReadSurfaceAvailability.cs:11]
- [x] [Review][Patch] [low] Complete required XML documentation for the new tenant-users gateway member and direct-read name constants [src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs:31]

### Review Findings — review loop 4, production `src/` chunk (2026-07-29)

Four layers (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor) over `8d64563..worktree -- src/`
(46 files, +4,820/-534). 42 raw findings deduped to 33 and re-verified against post-change source before rating.
Scope note: this pass covered production `src/` only. The `tests/` (22 files, +5,020/-185) and `_bmad-output/`
chunks were not reviewed as primary targets; test files were read only as evidence for verification-gap findings.

#### Decisions needed

- [ ] [Review][Decision] Global-administrators 304 promotes the response `ProjectionVersion` while the normative Task text forbids it — Task line 64 reads "Never relabel retained payload with a different ETag or projection version", but `TenantQueryGateway.cs:625` sets `ProjectionVersion = result.Metadata?.ProjectionVersion` and recomputes `IsCompleteEvidence` from it, diverging from all five sibling reads which keep `previous.ProjectionVersion`. Review loop 3 dismissed this as a code defect and added a rationale comment plus a pinning test; the retained `ETag` genuinely is preserved. The unresolved item is the contradiction itself: either amend the Task text to carve out the exact-strong-ETag case, or bring the branch back in line with its siblings. Leaving both as-is means the spec and the tree disagree on a rule that gates a destructive command.
- [ ] [Review][Decision] A 304 is accepted as authoritative on freshness derived from the legacy `X-Hexalith-Is-Stale` boolean when the platform reported no lifecycle evidence — When `X-Hexalith-Projection-Lifecycle` is absent, `lifecycle` is `Unknown` ("no authoritative projection lifecycle evidence"), yet `ResolveFreshness` falls through to `IsStale: false => Current` (`TenantsRestQueryClient.cs:396`) and `IsSupportedNotModified` (`:406`) accepts it. That `Current` then satisfies the freshness gate on grant/remove and on `MemberAccessReview.ActionsAreEvidenceBacked`. Review loop 2 decision D6 settled the classification question ("platform contract governs; behavior kept as-is"); this pass adds new downstream evidence that the same value now gates destructive commands, which D6 did not consider. Owner call: keep D6 as-is, or require projection-backed lifecycle evidence specifically for mutation-gating freshness.
- [ ] [Review][Decision] The Story 1.11 split leaves 1.10 declaring files whose in-range content is 1.11 work — Decision D1 extracted the global-administrator authorization work to Story 1.11, but `TenantsWorkspace.razor` is declared in 1.10's File List while its entire net change in the range is the 1.11 privileged entry point (`_canReviewGlobalAdministrators`, `GlobalAdministratorsHref`, the `AuthenticationStateProvider` subscription, `TenantsGlobalAdministratorClaims.Evaluate`). `GlobalAdministratorsPage.razor` and `TenantsBffComposition.cs` legitimately carry both stories' work, but `TenantsGlobalAdministratorClaims.cs` and `TenantConfigurationPrincipalResolver.cs` — same commit, same concern — are excluded. The boundary is drawn inconsistently within one commit. Owner call: re-cut both File Lists, or record that shared files are declared by both stories.

#### Patch

- [x] [Review][Patch] [high] Remove-global-administrator submit re-checks nothing: the last-admin hard stop, freshness and completeness gates are render-time only, so a notification refresh under a `Previewed` intent leaves Submit enabled and the last administrator removable [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:681]
- [x] [Review][Patch] [high] Paging into an emptied global-administrators page strands the operator with no controls at all — `Empty` is in `IsSuccessfulPage` (so the cursor commits) but absent from both `ShouldRenderRows` and `CanRecover`, removing pager, Refresh, Retry and Reset together [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:517]
- [x] [Review][Patch] [high] Global-administrator retention fabricates a `Degraded` surface with zero retained rows and renders "Last confirmed administrators remain visible" when none are; the tenant-users mapper has the guard this path lacks [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2113]
- [x] [Review][Patch] [high] `TenantAuditPage.LoadAsync` leaves the tenant-detail enrichment call unguarded, so a Refresh during initial load lets `OperationCanceledException` escape `OnParametersSetAsync` and tear down the circuit; the sibling call one line below has the filter [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:399]
- [x] [Review][Patch] [high] Page-one cursor recovery is silent on the member, My Tenants and user-membership surfaces, violating the `epic-1-context.md:27` notice constraint that decision D7 restored specifically to force this repair; `TenantUsersReason.ListRefreshed` is unreachable (its only producer is gated by `isListRefreshed`, and the sole call site passes `false`) and `UserTenantMembershipReason.PageRecovered` has no consumer [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:157]
- [x] [Review][Patch] [high] The member pager stays live during an in-flight member command, so paging unmounts `RemoveTenantMemberFlow` mid-command and destroys its lifecycle state and receipt; `_activeRemoveMemberUserId` is not cleared by paging, so returning silently re-opens a flow the user never re-initiated [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:243]
- [x] [Review][Patch] [high] The only live-stack read test substitutes an adapter that re-implements the pre-1.10 `SubmitQueryAsync` path, so nothing exercises `TenantsRestQueryClient` against a running topology — no route, no `X-Hexalith-*` header parsing, no base-path preservation, no failure mapping [tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:379]
- [x] [Review][Patch] [medium] The member-paging retention fix is inoperative: `HasMatchingTenantUsers` gates on `MatchesPageScope(previous.RequestCursor, …, request.Cursor, …)`, which is false by construction on every paging request, so passing `retained` cannot reach gateway retention and the in-component substitute remains the only path — while the code comment asserts the opposite [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:512]
- [x] [Review][Patch] [medium] Member invalid-cursor recovery assigns the recovered snapshot unconditionally, bypassing the non-usable-kind guard directly below it, so a transient failure on the recovery read empties the table after history was already cleared — leaving no Previous and no way back [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:528]
- [x] [Review][Patch] [medium] The mobile read-only notice is still permanently visible on desktop: `::deep` compiles to `[b-xxx] .global-admins__mobile-readonly` and the `FluentMessageBar` has no plain-HTML scoped ancestor from this component, so the rule matches nothing. Needs a scoped wrapper element, not `::deep` alone. The loop-3 patch and its stylesheet assertion both pin the inert construct [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:10]
- [x] [Review][Patch] [medium] `_supersededCancellations` is an unsynchronized `List<T>` mutated off the renderer dispatcher by `Retire` and enumerated during `DisposeAsync`, so a disposal concurrent with an authorization collapse throws `InvalidOperationException` out of component teardown; every other shared counter on this page uses `Interlocked`/`Volatile` [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:495]
- [x] [Review][Patch] [medium] The loop-3 cancellation fixes were applied only to `GlobalAdministratorsPage`: `TenantAuditPage` still uses plain `++_loadGeneration` with an unsynchronized read in `CanApply`, and both `TenantAuditPage` and `TenantDetailPage` still dispose a `CancellationTokenSource` whose token is in flight — the resulting `ObjectDisposedException` is not an `OperationCanceledException` and escapes every catch filter on those pages [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1001]
- [x] [Review][Patch] [medium] The audit refresh subscription is bound to the load cancellation token, so a Refresh during setup cancels `SubscribeAsync` and returns without recording `_subscriptionTenantId`; nothing retries for the same tenant, permanently disabling projection auto-refresh for the circuit. The sibling page passes `CancellationToken.None` [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:457]
- [x] [Review][Patch] [medium] The `_readRefreshSubscriptionInFlight` guard drops the changed-tenant case it does not cover: switching tenants mid-setup returns early for the new tenant, and the old tenant's completion disposes its lease without ever subscribing the new one [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:445]
- [x] [Review][Patch] [medium] `LoadMemberPageAsync` applies snapshot, cursor and history writes off the renderer dispatcher after its `CanApply` check with no re-check inside `InvokeAsync`, so a superseded member page can overwrite newer rows and mutate `_memberCursorHistory` concurrently with the renderer reading it; `RefreshTenantReadsAsync` re-checks inside `InvokeAsync` precisely to avoid this [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:521]
- [x] [Review][Patch] [medium] `GlobalAdministratorsPage.LoadAsync` writes `_snapshot = Loading()` on a thread-pool continuation after `await ReauthorizeAsync().ConfigureAwait(false)` and outside the generation guard, while the result write 30 lines later is deliberately marshalled; a stalled load can overwrite a newer applied snapshot and strand the page on `Loading`, which `CanRecover` excludes [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:838]
- [x] [Review][Patch] [medium] The member surface collapses distinct read states: `EmptyStateTestId` and `EmptyStateTitle` return the single `tenants-member-state` id and `Tenants.Members.State.Title` for every non-`Empty` kind, and `StateMessage`'s default arm merges `Invalid`, `Unavailable` and `Error` into one string — AC6 requires these to remain distinguishable, and the shared selector also blocks selector-based tests [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:413]
- [x] [Review][Patch] [medium] The AppHost `Tenants__BaseAddress` wiring that closes HOST-REF-1 is pinned only by `ShouldContain` substring matches over the raw `Program.cs` text; the literal string also appears in the adjacent comment at `Program.cs:147`, so deleting the `WithEnvironment` call keeps the test green, and nothing asserts the value resolves to the `tenants-api` https endpoint [tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs:92]
- [x] [Review][Patch] [medium] `UnavailableTenantQueryGateway.GetTenantUsersAsync` is the only one of the seven reads with no fail-closed test, and it is exactly the HOST-REF-1 misconfiguration path; an `Empty`-instead-of-`Unavailable` regression would render authorization-safe absence for members that simply could not be read [src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs:17]
- [x] [Review][Patch] [medium] The shared member fixture is built from `detail.Members`, so no `MemberAccessReview` test can prove rows, `ActiveChangeRoleMember` or `ActiveRemoveMember` come from the authoritative tenant-users read — reverting the story's central change would fail nothing those nine renders observe [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1900]
- [x] [Review][Patch] [medium] The owner-context fail-closed guard has no assertions: `tenants-member-owner-context` appears in no test, so dropping it would let a detail-derived `OwnerCount` render beside members read at a different projection version [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:106]
- [x] [Review][Patch] [medium] `HasValidPayloadShape`'s `TenantDetail` arm is never driven with a malformed detail payload, so removing it — or its non-blank member-id clause — would let a detail with null or blank-id members deserialize as success and reach `TenantDetailSnapshot.Ready` [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:450]
- [x] [Review][Patch] [medium] `MemberAccessReview`'s entire non-`Empty` state branch is never rendered by any test (`tenants-member-state` appears nowhere under `tests/`), and the localizer double stubs only two of the eight state keys this change added [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:29]
- [x] [Review][Patch] [medium] Five changed `src/` files are absent from the File List — `GlobalAdministratorGrantCommandSnapshot.cs`, `GlobalAdministratorRemoveCommandSnapshot.cs`, `GlobalAdministratorsReason.cs`, `GlobalAdministratorsRequest.cs`, `GlobalAdministratorsSurfaceKind.cs` — while the declared `GlobalAdministratorsSnapshot.cs` returns members defined in them [_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md:318]
- [x] [Review][Patch] [low] The audit pager keeps `_pageLoadInFlight` out of its `Disabled` expressions, unlike the global-administrators pager, so the buttons stay clickable during a load and the guard exists only inside the handlers with no test asserting pager state mid-load [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:159]
- [x] [Review][Patch] [low] The intent contract still forbids what the tree ships: Task line 65 ("Do not edit AppHost"), the KEEP list line 93 ("unchanged AppHost") and Spec Change Log line 100 ("No … AppHost … content changed in this loop") were never amended after decision D2 waived the constraint; a reader of the contract alone gets the opposite answer [_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md:65]
- [x] [Review][Patch] [low] The evidence record's six-route proof cites path-building sites at lines 39, 53, 68, 85, 102 and 120; the actual sites at HEAD are 52, 71, 91, 113, 135 and 167. Separately, `Tenants.Detail.Members.Summary` is now dead in production, referenced only by a test stub localizer [_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md:34]

#### Deferred

- [x] [Review][Defer] [high] `ApplyAuthenticationStateChangedAsync` authorizes with the uncorroborated `TenantsGlobalAdministratorClaims.Evaluate` (no `sub` corroboration against `IUserContextAccessor`), then calls `LoadAsync(reauthorize: false)` to skip the corroborated check, so a token refresh whose `sub` does not match the server-side user context unlocks the mutation surface for the circuit [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178] — deferred to Story 1.11's review loop; global-administrator authorization is 1.11-owned scope per decision D1
- [x] [Review][Defer] [high] `ResolveSystemScopeEvidence` now requires exactly one distinct `eventstore:tenant` claim value, so a token carrying both `system` and a tenant scope resolves `Indeterminate`; combined with the `GlobalAdministratorPolicy` switch to `Evaluate(...) == Authorized`, every policy-gated surface disappears for multi-scope platform administrators. The `authenticated.Length != 1` guard likewise denies cookie+bearer principals the previous any-match check tolerated [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116] — deferred to Story 1.11's review loop; 1.11-owned scope per decision D1
- [x] [Review][Defer] [medium] A single transient authorization-resolution fault collapses the page to the restricted surface permanently: `ResolveAuthorizationReflectionAsync` swallows every exception to `Indeterminate`, and the `!IsAuthorized` branch offers no Refresh, Retry or Reset while `EnsureReadRefreshLeaseAsync` and `CanRecover` are both gated on `IsAuthorized` [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235] — deferred to Story 1.11's review loop; 1.11-owned scope per decision D1
- [x] [Review][Defer] [medium] The workspace's Global Administrators entry link evaluates authorization uncorroborated (`:602`) while initial resolution uses the corroborated resolver (`:553`), so the link and the page it targets desynchronize in both directions after any `AuthenticationStateChanged` [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602] — deferred to Story 1.11's review loop; 1.11-owned scope per decision D1

### Review Findings — review loop 5, core transport/state follow-up (2026-07-29)

Four independent review layers produced 41 raw reports, normalized to 27 claims. Thirteen findings survived triage; 14 reports were dismissed after call-site and owned-server verification. The mandatory story gitlink validator passed with all six declared moves accounted for.

#### Decisions needed

- [ ] [Review][Decision] [high] Resolve the still-open global-administrators `304 Not Modified` projection-version contract: the gateway promotes the response `ProjectionVersion` and can mark the retained snapshot complete, while the normative task forbids relabeling retained payload with a different projection version. This follow-up independently reconfirmed the review-loop-4 decision above; amend the contract for an exact strong-ETag current `304`, or preserve the retained version and keep completeness closed [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:622]

#### Patch

- [ ] [Review][Patch] [high] Bind every conditional `304` reuse path to the retained snapshot's validator—not merely its scope—and give tenant-users the same unconditional retry used by sibling surfaces so a response for validator B can never relabel payload A [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:121]
- [ ] [Review][Patch] [high] Replace non-null/default-scope and `Rows.Count > 0` retention heuristics with explicit last-confirmed snapshot kinds: current predicates can promote an initial failure to `Degraded`, while a genuinely confirmed empty global-administrator result cannot be retained on a transient refresh failure [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2075]
- [ ] [Review][Patch] [high] Preserve the fixed `Unavailable` failure category through raw HTTP status wrapping; a first-load `500` or non-success `2xx` currently falls through exception mapping to `Degraded` despite having no retained payload [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:587]
- [ ] [Review][Patch] [high] Let a metadata-complete current `304` clear prior transport degradation for user-tenants, global administrators, audit and tenant-list snapshots; the current resolvers can leave `Kind=Degraded` after freshness becomes `Current`, permanently blocking destructive actions [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2334]
- [ ] [Review][Patch] [high] Add support-safe `ToString()` overrides for audit and user-membership snapshots so generated record formatting cannot disclose request cursors, projection versions, ETags, target identifiers or row contents [src/Hexalith.Tenants.UI/Services/State/TenantAudit/TenantAuditSnapshot.cs:6]
- [ ] [Review][Patch] [medium] Close the refresh-subscription registration race: a backend notification can arrive after `SubscribeAsync` succeeds but before the notifier handler and callback are registered, and is then lost [src/Hexalith.Tenants.UI/Services/Gateways/TenantReadRefreshSubscription.cs:72]
- [ ] [Review][Patch] [medium] Add a DI-level regression test proving the registered Tenants REST client emits no `System.Net.Http.HttpClient.*` logs containing a sentinel cursor after `.RemoveAllLoggers()` [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:72]
- [ ] [Review][Patch] [medium] Exercise the new `TenantReadRefreshLease.IsSubscribed` contract for missing services, failed setup, successful registration and successful retry after a failed setup [src/Hexalith.Tenants.UI/Services/Gateways/TenantReadRefreshLease.cs:27]
- [ ] [Review][Patch] [medium] Add callback-isolation coverage proving that one throwing refresh callback neither prevents the next callback nor poisons later nudges, and that only a support-safe reason-code log is emitted [src/Hexalith.Tenants.UI/Services/Gateways/TenantReadRefreshSubscription.cs:226]
- [ ] [Review][Patch] [medium] Cover tenant-detail projection-version mapping on `200` and preservation on supported `304`; member action consistency consumes this bridge, but existing gateway tests assert only identity and freshness [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:242]
- [ ] [Review][Patch] [medium] Add mismatched cursor/page-size `304` and failure tests for list, user-tenants, global-administrator and audit surfaces, proving scoped retention cannot reuse rows from another page [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:491]

#### Deferred

- [x] [Review][Defer] [high] Retained direct-read snapshots are scoped by entity/filter/paging but not by authenticated subject, so a principal change inside the same scoped circuit can expose the prior subject's authorized rows during a failure or insensitive `304` [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2075] — deferred, pre-existing

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-1-context.md`
- `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md`
- `src/Hexalith.Tenants.AppHost/Program.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsReadSurfaceAvailability.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsRestQueryClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/InvalidCursorSignal.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ResponseContentTooLargeException.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsReadSurfaceAvailability.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryFailureKind.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryResponse.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshLease.cs`
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsReason.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsRequest.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSurfaceKind.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersReason.cs`
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersRequest.cs`
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantUsers/TenantUsersSurfaceKind.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipReason.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipSnapshot.cs`
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceEntryPointTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantConfigurationEndToEndTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

Dependency pointers that moved inside this story's baseline range. They are declared here, not reverted:
the movements are carried by published commits in the story range, including the dedicated dependency
commits `f425b49` and `09947a2`. Reverting them would mean creating new dependency reversions rather than
un-bundling unpublished work.

- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.AI.Tools`

## Completion Notes List

- ✅ The Administrator explicitly accepted the `references/Hexalith.AI.Tools` pointer move from
  `991e8ea` to `859d53b` as part of Story 1.10, so the move is declared in this story's File List and
  retained rather than reverted.
- ✅ Gateway transport/composition follow-up: all 12 actionable findings are patched. Page validators are
  bound to exact request scope, unmatched 304s recover unconditionally, audit identity is enforced at both
  transport and gateway boundaries, My/User Tenants recover explicit invalid cursors to page one, all six
  read failures emit bounded diagnostics, unexpected transport failures are contained, keyed overrides are
  ignored by unkeyed composition detection, and the new declarations/documentation follow repository rules.
  Focused gateway/composition tests pass 582/582 and the full UI suite passes 1,507/1,507.
- ✅ Completion revalidation at `09947a2`: all seven repository test projects pass individually in Release
  package mode (2,802 tests across UI, Integration, Contracts, Client, Testing, Server, and Sample), the
  warning-as-error Release solution build has 0 warnings and 0 errors, both prohibited-symbol scans have no
  matches, and the exact story gitlink validator exits 0 with all six pointer movements declared.

- ✅ Resolved review finding: a direct typed-client invalid-cursor response now executes and mutation-pins
  `ToEventStoreResult` failure mapping before page-one recovery. Focused test and full UI suite pass
  (1,432/1,432).
- ✅ Resolved review finding: tenant-users snapshot tests now cover all 11 non-collapsing surface kinds,
  all 12 support-safe reasons, and cross-tenant retention rejection. Both the scope guard and factory
  matrix were mutation-verified; full UI suite passes (1,436/1,436).
- ✅ Resolved review finding: the global-administrator page fixture now registers a mutable
  `AuthenticationStateProvider` and proves pending revocation clears privileged state before an authorized,
  claim-correct restore re-queries and renders it. The test exposed and now pins the missing background-read
  render publication; handler attachment was mutation-verified and the full Release UI suite passes
  (1,441/1,441).
- ✅ Resolved review finding: the five global-administrator and audit notification/recovery affordances now
  have executable component coverage for retained rows, refresh completion, page-one recovery, reset, and
  retry. All selector mutations failed the focused tests; the full Release UI suite passes (1,441/1,441).
- ✅ Resolved review finding: audit subscription rebinding now proves the previous tenant lease is released
  and only the new scope refreshes; a concurrent paging/notification test proves late generations cannot
  replace the newest rows or corrupt cursor history. Both guards were mutation-verified; the full Release
  UI suite passes (1,443/1,443).
- ✅ Resolved review finding: member invalid-cursor recovery and duplicate in-flight Next suppression now
  have dedicated component tests, and `tests/test-summary.md` describes only the executable evidence. The
  recovery branch and combined UI/handler guard were mutation-verified; the full Release UI suite passes
  (1,445/1,445).
- ✅ Resolved review finding: request and response ETag length bounds are now tested independently on 200
  responses, so validator mismatch can no longer mask either guard. Removing either bound fails its focused
  test; the full Release UI suite passes (1,446/1,446).
- ✅ Resolved review finding: caller cancellation now has separate pre-request and response-body tests; the
  latter proves headers completed and content consumption began before cancellation propagated. Reversing
  the body-phase cancellation filter fails the focused test; the full Release UI suite passes (1,447/1,447).
- ✅ Resolved review finding: principal resolution now cancels after the asynchronous circuit identity read
  has entered and before it completes, proving the post-await propagation guard. Removing that guard fails
  the focused test; the full Release UI suite passes (1,448/1,448).
- ✅ Resolved review finding: notification logging capture now records the exception argument separately
  and asserts it is null, in addition to checking the bounded formatted reason. Attaching an otherwise
  formatter-invisible exception fails the focused test; the full Release UI suite passes (1,448/1,448).
- ✅ Resolved review finding: one production-boundary test now captures all six typed Tenants client calls
  (including self/explicit user variants) and pins literal scope, filters, cursor, page size, validator, and
  cancellation independently of `RestQueryClientAdapter`. Mutating a gateway query argument fails the test;
  the full Release UI suite passes (1,449/1,449).
- ✅ Resolved review finding: the workspace authorization test now inspects state after production teardown,
  proving a pending administrator completion stays discarded and later provider events are unsubscribed.
  Removing the unsubscribe fails the focused test; the full Release UI suite passes (1,449/1,449).
- ✅ Resolved review finding: canonical all-tenants, My Tenants, and user-lookup states now flow from
  `TenantWorkspaceState.ToCanonicalUrl()` into the global-administrator return link, covering every allowed
  cursor-free key while preserving the explicit cursor rejection. Removing an emitted key from the allowlist
  fails the theory; the full Release UI suite passes (1,452/1,452).
- ✅ Review loop 3 closure: all 13 review patches are checked and mutation-verified. Release validation passes
  for UI 1,452/1,452; generated-controller integration 26/26; non-performance integration 167/167;
  Contracts 120/120; Client 50/50; Testing 181/181; Server 738/738; and Sample 39/39. The Release
  solution build passes with 0 warnings and 0 errors; forbidden generic-query and invented tenant-index
  scans return no matches; staged and unstaged diff checks pass; and the final story gitlink validator exits 0.

- `references/Hexalith.Builds` — `53d53ae -> 13cad86`. Published dependency maintenance; the completion
  gates were re-run against this exact pointer.
- `references/Hexalith.Commons` — `427530e -> f2b5f1b` ("feat(workflow): enhance commitlint configuration
  for pull request title handling"). This upstream workflow-only bump was carried by the published Story
  1.10 review commit `96bdfd8`; it belongs to the story's declared commit range and provenance record, not
  its runtime implementation. Reverting published `origin/main` is outside this review repair.
- `references/Hexalith.EventStore` — `5a1d277 -> 1d42528`. Published dependency maintenance; declared as
  commit-range provenance and included in the completion validation graph.
- `references/Hexalith.FrontComposer` — `7870526 -> b6efcad` (release-workflow SHA pin and identifier
  inventory metrics). Not Story 1.10 work; upstream release tooling.
- `references/Hexalith.Memories` — `1868c8f -> ccd8efa`. The final movement is carried by the published
  dependency commit `09947a2`; every completion gate was re-run after that movement.

All six moved pointers are declared and the validator passes against the completion tree. Earlier pointer
tables are retained as historical evidence only; the current values above and in the dated evidence report
are authoritative.
