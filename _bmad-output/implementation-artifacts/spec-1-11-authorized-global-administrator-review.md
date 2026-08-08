---
title: 'Authorized Global Administrator Review'
type: 'feature'
created: '2026-07-28'
status: 'review'
baseline_commit: '2e61f57bda6379192007d1bc6fabbde61996b11d'
baseline_revision: '2e61f57bda6379192007d1bc6fabbde61996b11d'
review_loop_iteration: 5
followup_review_recommended: true
# Remaining bmad-code-review chunks after loop-5 chunk-2 apply: (3) workspace + tenant detail,
# (4) artifacts optional.
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** The fixed-scope global-administrator read exists, but the interactive UI can expose scope and command structure after authorization becomes indeterminate or the server denies a stale authorization reflection. It also presents unknown freshness too affirmatively, lacks an authorized contextual entry and safe return, and can mistake one cursor page for complete evidence.

**Approach:** Harden the existing review rather than create another read contract: use one strict circuit-aware principal interpretation for policy and BFF reflection, keep the server REST endpoint authoritative, gate all privileged markup and subscriptions on confirmed authorization, add contextual navigation, and represent freshness, recovery, and paging honestly.

## Boundaries & Constraints

**Always:** Keep the fixed `system / global-administrators / global-administrators` identity and server-only `GET /api/global-administrators`; use opaque requester-bound cursors, literal authorized user IDs, FrontComposer/Fluent V5, EN/FR whole strings, stable selectors, and support-safe diagnostics. Preserve the single `/tenants` shell registration and existing desktop grant/remove regressions, but require current complete projection evidence before list-wide mutation decisions.

**Block If:** Correctness requires a new backend endpoint, exposing protected metadata, treating `ServedAt` as projection age, or changing a shared platform/submodule contract.

**Never:** Do not enumerate global administrators through tenant/member reads, the generic EventStore query route, browser-side backend calls, Memories, or client filtering. Do not reveal identities, counts, freshness, fixed-scope literals, route details, or mutation affordances to unauthorized/indeterminate callers; infer totals from a page; expose cursors/ETags/tokens; or claim SignalR/HTTP success proves current projection state. Do not widen this story into AppHost composition; report the authorized live-host lane as unavailable if `Tenants:BaseAddress` remains absent.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized review | Strict circuit principal and current REST projection page | Contextual entry opens the review; literal IDs, platform scope, current truth, safe copy, and protected paging render | No error expected |
| Authorization denied | Indeterminate/non-admin reflection, or server 401/403 after optimistic reflection | Generic unavailable/denied surface only; no privileged query when reflection fails closed, no rows/scope/commands/subscription | Dispose any lease and discard retained privileged state |
| Untrusted truth | Unknown, stale, degraded, missing metadata, or transport failure | Distinct non-affirmative state; previously confirmed rows may remain labelled honestly; mutations fail closed | Offer localized retry/reset without leaking diagnostics |
| Incomplete page | `HasMore`, later page, invalid cursor, or racing page/notification loads | Next/Previous are serialized; late results do not apply; no page count becomes a global total | Recover page one honestly and announce refresh |
| Narrow viewport | Authorized mobile review | Read-only rows and recovery remain usable | Hide/disable high-impact controls with a localized reason |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs` and `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs` -- competing principal interpretations; consolidate strict request/circuit evidence.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition}.cs` -- authorization reflection consumed by navigation and page gating.
- `src/Hexalith.Tenants.UI/Components/{Routes.razor,Pages/TenantsWorkspace.razor,Pages/GlobalAdministratorsPage.razor}` -- route policy, contextual entry/return, privileged rendering, paging, refresh, and commands.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/` and `Services/Gateways/TenantQueryGateway.cs` -- truth-state, recovery, completeness, and support-safe diagnostic model.
- `tests/Hexalith.Tenants.UI.Tests/` -- outer component, principal, gateway, localization, conformance, and notification evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition}.cs`, and `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- make one strict tri-state evaluator work for HTTP requests and Blazor circuits; reject malformed/conflicting identities and keep server denial final.
- `src/Hexalith.Tenants.UI/Components/Routes.razor` and `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- enforce the registered policy, show one authorized contextual entry, and carry only a local canonical `/tenants...` return URL.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`, `GlobalAdministratorsSurfaceKind.cs`, `GlobalAdministratorsReason.cs`, and `GlobalAdministratorsRequest.cs`, plus `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- add explicit unknown/error/recovery and page-completeness semantics; bound `ToString()` output so rows, cursor, ETag, projection version, and identities cannot escape diagnostics.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` and `.razor.css` -- validate return navigation; render privileged structure only while authorized; unsubscribe and clear it on denial; add retry/reset, Previous/Next race protection, non-affirmative truth copy, exact literal display/copy, and mobile read-only behavior without legacy tokens.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx` -- add parity-checked whole strings for entry/return, denied, unknown/error, recovery, incomplete evidence, and mobile read-only states.
- `tests/Hexalith.Tenants.UI.Tests/{Components/GlobalAdministratorsPageTests.cs,TenantsWorkspaceTests.cs,Services/Configuration/TenantConfigurationReadPolicyTests.cs,Services/Gateways/TenantQueryGatewayTests.cs,Services/Gateways/TenantsBffCompositionTests.cs,DomainUiFluentConformanceTests.cs}` -- cover the matrix at the rendered UI/BFF boundary, including stale reflection plus server denial, circuit fallback, exact IDs, late-load rejection, notification disposal, cursor history, localization, support safety, and responsive controls.
- `tests/Hexalith.Tenants.Server.Tests/Queries/GetGlobalAdministratorsQueryHandlerTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` -- retain fixed-scope authorization/cursor and outer HTTP no-payload denial evidence without widening the API.

**Acceptance Criteria:**
- Given an authorized operator on a canonical Tenants workspace URL, when they enter and return from Global Administrators, then the exact safe workspace context is restored and no second shell entry or external return target is possible.
- Given authorization is indeterminate, malformed, non-admin, or denied by the server, when the route is requested or authorization changes, then no privileged identity, count, freshness, fixed-scope detail, command structure, subscription, or retained row is observable.
- Given current, stale, unknown, degraded, empty, invalid, and unavailable REST outcomes, when the review renders or recovers, then each state has distinct localized truth and recovery behavior, and only authoritative current evidence enables safety-sensitive actions.
- Given cursor paging, notifications, or rapid retries overlap, when completions arrive out of order, then only the newest scoped request renders, cursor history stays valid, and neither cursors nor inferred totals appear in URL, DOM, clipboard, logs, or diagnostics.
- Given an authorized narrow viewport, when the page renders, then the review, paging, copy, focus, and live-region behavior remains usable while grant/remove actions are visibly unavailable.

### Review Findings

- [x] [Review][Decision] Circuit authentication state is authoritative whenever it is available — **RESOLVED (2026-08-01, owner decision): keep the current circuit-over-HTTP precedence with no `HttpContext.User` fallback.** A stale request principal must not retain privilege after a live circuit authentication change; anonymous, pending, or faulty circuit evidence therefore fails closed to `Indeterminate`. The cancellation and non-blocking review patches address the availability cost without switching identity sources. [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:23]
- [x] [Review][Patch] Authentication transitions bypass the corroborated circuit-aware resolver and can re-enable the restricted workspace entry [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:580] — **APPLIED (2026-08-01):** authentication events now fail closed immediately and use the BFF's strict circuit-principal resolution before restoring the entry; rendered regression coverage proves raw event claims cannot bypass that decision.
- [x] [Review][Patch] Tenant lifecycle actions still consume an HTTP-only authorization reflection that remains indeterminate in an interactive circuit [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21] — **APPLIED (2026-08-01):** tenant detail now resolves lifecycle authorization asynchronously through the strict BFF seam, invalidates superseded results, and reauthorizes on circuit authentication transitions.
- [x] [Review][Patch] Caller cancellation does not interrupt a pending authentication-state read [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:28] — **APPLIED (2026-08-01):** the provider task is awaited with caller cancellation and a time-bounded regression test proves cancellation completes without provider cooperation.
- [x] [Review][Patch] Optional global-administrator resolution can block the ordinary workspace list indefinitely during initialization [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:521] — **APPLIED (2026-08-01):** optional entry authorization runs independently of the primary tenant-list load and remains fail closed while pending.
- [x] [Review][Defer] A route change while the prior tenant's refresh subscription is pending can leave the new tenant without projection auto-refresh [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:447] — deferred, pre-existing outside Story 1.11's attributed implementation

### Review Findings — loop 2 (2026-08-01, chunk A: production source)

Four review layers completed, none failed. Findings below were each re-read in the source before rating;
three agent claims were downgraded or dropped because the code did not support them.

- [x] [Review][Decision] **RESOLVED (2026-08-01, owner decision): inject `AuthenticationStateProvider` directly and
  keep `CircuitServicesAccessor` only as a fallback.** *(Loop-3 correction: the shipped code keeps the accessor
  first and uses the injected provider as the fallback. Inside a circuit both resolve to the same scoped
  instance, so the substance of the decision — removing the notification path's dependency on the inbound-activity
  `AsyncLocal` — holds either way; the precedence wording did not match the code and is corrected here rather
  than churning a tested path. Loop 3 additionally gated the injected fallback on there being no active
  `HttpContext`, so the prerender pass fails closed instead of authorizing from the request principal.)*
  The resolver is registered `Scoped`, so inside a circuit its
  own injected services already are the circuit scope; resolving the provider directly removes the dependency on the
  inbound-activity `AsyncLocal` without reinstating any request-principal fallback. Circuit-over-HTTP precedence and
  the no-`HttpContext.User` rule from the earlier decision are preserved unchanged. Once this lands, the
  authentication-transition path below must be routed through the strict BFF seam, as `TenantsWorkspace` already is.
  Converted to a patch. Original finding follows.
- [x] [Review][Patch] Circuit-only principal resolution fails closed on every path that is not an inbound
  circuit activity, so each projection notification permanently revokes an authorized global administrator —
  `CircuitServicesAccessor.Services` is an `AsyncLocal` published only inside
  `FrontComposerCircuitServicesHandler.CreateInboundActivityHandler` and nulled in its `finally`. The
  notification path starts at `TenantReadRefreshSubscription.OnProjectionChanged` (notifier thread, no inbound
  activity) → `RunRefreshLoopAsync` → `RefreshFromNotificationAsync` → `LoadAsync` (`reauthorize` defaults to
  `true`) → `ReauthorizeAsync` → resolver sees `Services == null` → `Indeterminate` → `CollapseAuthorizationAsync`.
  Recovery is then impossible in-page: `CanRecover` requires `IsAuthorized`, and both `RefreshFromNotificationAsync`
  and `EnsureReadRefreshLeaseAsync` return early on `!IsAuthorized`. This contradicts the 2026-08-01 owner decision
  that removed the `HttpContext.User` fallback on the stated grounds that the cancellation and non-blocking patches
  covered the availability cost — they do not cover absence of the `AsyncLocal`. Options: reinstate a corroborated
  request-principal fallback (reverses the decision); capture the circuit `IServiceProvider` at component
  initialization and resolve from it; or stop re-authorizing on the notification path. Owner input required.
  [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:21]
- [x] [Review][Decision] **RESOLVED (2026-08-01, owner decision): route the authentication transition through the
  strict corroborated seam**, together with the resolver change above. The uncorroborated `Evaluate` call and the
  `reauthorize: false` suppression are removed, so one strict interpretation governs every path. Converted to a
  patch. Original finding follows.
- [x] [Review][Patch] The Global Administrators page re-grants authorization from raw authentication-event
  claims and then suppresses the strict gate — `TenantsGlobalAdministratorClaims.Evaluate(authenticationState.User)`
  runs with `requireCorroboration: false`, and the follow-up load passes `reauthorize: false`, so the
  `IUserContextAccessor.UserId` corroboration applied everywhere else is skipped before privileged markup, the
  grant/remove forms and the subscription are restored. `TenantsWorkspace` routes the same transition through the
  strict BFF seam. Entangled with the decision above: the weak path may be a deliberate workaround, because the
  strict resolver also returns `Indeterminate` on the authentication-changed path. Resolve together.
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1443]
- [x] [Review][Decision] **RESOLVED (2026-08-01, owner decision): require exactly one *distinct* `sub` value.**
  Duplicate claims carrying an identical value are not ambiguous evidence; conflicting values still fail closed to
  `Indeterminate`, so the intent of the earlier single-identity decision is preserved while the lockout disappears.
  Converted to a patch. Original finding follows.
- [x] [Review][Patch] `Evaluate` rejects a principal carrying two `sub` claims with an identical value, which a
  Keycloak/OIDC pipeline that maps `sub` from both the id_token and userinfo produces routinely; the result is a
  permanent restricted surface for a legitimate administrator. Relaxing to "exactly one distinct value" changes the
  2026-08-01 owner decision requiring exactly one literal `sub` claim. Owner input required.
  [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:39]

- [x] [Review][Patch] `Indeterminate` collapses into a terminal `Unauthorized()` surface that offers no retry, so a
  single transient resolver fault is rendered as "not authorized" and cannot be re-attempted [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1496]
- [x] [Review][Patch] `ReauthorizeAsync` writes `_authorizationReflection` off-dispatcher with no transition-version
  guard, so a resolve that started before sign-out can restore `Authorized` afterwards and re-render the grant and
  remove forms [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1492]
- [x] [Review][Patch] `_lifecycleAuthorizationCancellation` is cancelled after it may already have been disposed;
  `ObjectDisposedException` then escapes `OnParametersSetAsync` or `DisposeAsync` and tears down the circuit. The
  sibling `_loadCancellation` on the same page already uses the lock-based scheme that prevents this [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:454]
- [x] [Review][Patch] Test gap that hid the decision above: no `GlobalAdministratorsPageTests` case exercises the real
  principal resolver — only two files in the whole UI suite touch `CircuitServicesAccessor`, and neither is the page's
  [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:1]
- [x] [Review][Patch] `EnsureReadRefreshLeaseAsync` lacks the post-assignment `_disposed` re-check that the same diff
  added to `TenantDetailPage`, so a lease can be stored after disposal and keep invoking a dead component [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1152]
- [x] [Review][Patch] `Preview` blocks on `rows.Count <= 1` while ignoring the `isCompleteEvidence` parameter it now
  stores, asserting a platform-wide "last global administrator" fact from one page [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:54]
- [x] [Review][Patch] `GlobalAdministratorRow`, `GlobalAdministratorGrantCommandSnapshot` and
  `GlobalAdministratorRemoveCommandSnapshot` keep the compiler-generated `ToString()`, printing identities,
  `MessageId` and `CorrelationId`; only the snapshot and request siblings were bounded [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRow.cs:7]
- [x] [Review][Patch] `TenantsWorkspace.LoadAsync` still cancels and disposes the token source at replacement time —
  the pattern both sibling pages document as unsafe — while catching cancellation only [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679]
- [x] [Review][Patch] Hoisting `ReauthorizeAsync` to be the submit handlers' first await means the render at that
  suspension still shows `Idle`; `RequestSent` and `_isGrantSubmitting` are then written on a thread-pool
  continuation with no re-render, so the in-flight lifecycle state is never observable [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1640]
- [x] [Review][Patch] Grant and remove projection requery confirm from a snapshot that may have been discarded as
  superseded; the pagers guard this with a reference check, the requery paths do not [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1745]
- [x] [Review][Patch] `RefreshFromNotificationAsync` ignores `_pageLoadInFlight`, so a notification landing during a
  Next/Previous load supersedes it and the operator's click produces no navigation and no feedback [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1061]
- [x] [Review][Patch] Every `AuthenticationStateChanged` clears cursor history and returns to page one with no
  announcement, while the same change added notices for the other two page-one jumps [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1535]
- [x] [Review][Patch] The lifecycle badge and the Retry/Reset stack were added inside the assertive state live
  region, so every lifecycle transition re-announces the whole block — the defect the neighbouring comment says was
  fixed by moving nudges out [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:112]
- [x] [Review][Patch] The notification-setup budget resets only in `RefreshAsync`, whose button renders only when
  rows exist, so a rows-free surface can exhaust the budget with no reachable reset [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:994]
- [x] [Review][Patch] A `roles` claim whose JSON payload carries leading whitespace falls through to the delimiter
  split and yields a definite `NonAdministrator` instead of `Indeterminate` [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:158]
- [x] [Review][Patch] The grant form accepts any non-whitespace user id, including control characters and unbounded
  length, so an id can be granted that `ResolvePrincipalEvidence` can never corroborate [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1651]
- [x] [Review][Patch] `TenantDetailPage` still disposes the refresh lease raw on the route-change and teardown paths
  while the safe helper added in the same change is used everywhere else [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:346]
- [x] [Review][Patch] `RefreshTenantReadsAsync` still uses `Task.WhenAll` with a cancellation-only catch, so a
  faulted read strands the member table on "refreshing" — the containment `OnParametersSetAsync` received [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:694]
- [x] [Review][Patch] `_memberPagingJumpedToFirstPage` is never cleared by a refresh, so the one-shot notice stays
  rendered indefinitely [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:786]
- [x] [Review][Patch] Next is enabled on `HasMore` alone while the handler also requires a non-blank cursor, making
  it a silent dead button when the service returns `HasMore` with no cursor [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:491]
- [x] [Review][Patch] `TenantDetailPage` charges the notification budget before the attempt — the behaviour the
  Global Administrators page documents as a bug — and offers no same-route reset [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:575]
- [x] [Review][Patch] `_pageLoadInFlight` is a non-atomic test-and-set read and written across the dispatcher and
  thread-pool continuations, so the mutual exclusion it exists to provide can be lost [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1012]
- [x] [Review][Patch] `OnInitializedAsync` mutates `_authorizationReflection` and `_snapshot` on a thread-pool
  continuation that races the first render batch [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:839]
- [x] [Review][Patch] The `ITenantsBffComposition` default member forwards to the `HttpContext`-only property this
  story discarded, so any implementation that does not override it silently gets the old interpretation [src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:26]
- [x] [Review][Patch] Test gap: the cursor suppression on the Global Administrators entry href is never exercised —
  no workspace test pages before reading the href, so deleting `with { Cursor = null }` stays green [tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:110]
- [x] [Review][Patch] Test gap: the page-scoped remove-count label arm never renders under test; all three
  assertions expect the complete-evidence string [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:1167]
- [x] [Review][Patch] Test gap: no test faults `GetTenantAsync` or `GetTenantUsersAsync`, so the new per-task fault
  containment on the initial detail load is unverified [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:528]
- [x] [Review][Patch] Test gap: the notification-setup failure log is unreachable from any test, and no
  `ILoggerFactory` is registered in the UI suite, so its support-safety claim is unproven [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1104]
- [x] [Review][Patch] The Review Triage Log still states the story is `in-progress` with an undeclared
  `references/Hexalith.PolymorphicSerializations` pointer; the frontmatter says `review`, the File List declares the
  pointer, and the validator now exits 0 [_bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md:91]

- [x] [Review][Defer] `TenantAuditPage` is the last consumer of the synchronous `HttpContext`-only reflection, so
  global-administrator correction affordances stay permanently unavailable on an interactive circuit [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1111] — deferred, file is not in this story's File List and the fix depends on the resolver decision above
- [x] [Review][Defer] `EnsureReadRefreshLeaseAsync` passes `CancellationToken.None` with no timeout, so one hung
  subscribe disables auto-refresh for the circuit [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1120] — deferred, needs a timeout policy decision rather than a mechanical fix
- [x] [Review][Defer] The grant and remove submit buttons are never disabled while a mutation is in flight, so a
  second click dispatches a second platform-authority command whose outcome is discarded [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:748] — deferred, pre-existing; `IsGrantSubmitDisabled` never depended on in-flight state before this story
- [x] [Review][Defer] `TenantDetailPage.IsSafeReturnUrl` accepts any `/tenants`-prefixed string while the sibling
  `NormalizeReturnUrl` enforces a canonical round-trip [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1173] — deferred, all admitted values stay same-origin relative so there is no redirect gap
- [x] [Review][Defer] On narrow viewports the per-row Remove control is hidden with no per-row localized reason;
  only a page-level notice explains it [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] — deferred, partial AC5 gap with the page-level reason present

### Review Findings — loop 3 (2026-08-01, uncommitted working tree: loop-1/loop-2 patches + the dev run closing 17 action items)

All four layers completed; none failed or timed out. Every finding below was re-read in the source before
rating. Story evidence was re-verified independently rather than taken from the Dev Agent Record: Release
build 0 warnings / 0 errors, full UI suite 1,866 passed / 0 failed / 0 skipped, `validate-story-gitlinks.py`
exit 0 with all seven pointers declared, no generic query path, `git diff --check` clean.

- [x] [Review][Decision] **RESOLVED (2026-08-01, owner decision): fail closed until hydration.** The injected
  fallback is now admitted only when no `HttpContext` is in scope — present for the prerender and static-SSR
  passes, null for circuit activity, notification threads and authentication transitions. Prerender therefore
  stays `Indeterminate` and renders the restricted surface; the interactive instance resolves once the circuit
  is connected. The loop-2 availability fix is preserved unchanged on the notification path. Fixed in the
  resolver so both consuming pages inherit it, rather than gating on `RendererInfo` per page (bUnit does not
  populate it). The precedence wording was corrected in the loop-2 decision entry rather than flipping a tested
  path, since inside a circuit both sources resolve to the same scoped instance. Original finding follows.
- [x] [Review][Decision] The strict resolver's non-circuit fallback re-admits request-principal evidence on the
  prerender / static-SSR pass, and its precedence is inverted relative to the recorded loop-2 decision. The
  decision text says "inject `AuthenticationStateProvider` directly and keep `CircuitServicesAccessor` only as a
  fallback"; the code does the opposite (`accessor ?? injected`) and says so in its own comment. Separately,
  `App.razor:15` renders `<Routes @rendermode="RenderMode.InteractiveServer" />` with prerendering on, so during
  the prerender pass `CircuitServicesAccessor.Services` is null and the injected request-scoped provider —
  seeded from `HttpContext.User` — authorizes the prerendered privileged markup. That is the evidence source the
  2026-08-01 decision deleted ("no `HttpContext.User` fallback"); `requireCorroboration: true` does not block it
  because `IUserContextAccessor.UserId` is request-scoped too. Options: (a) accept prerender-from-HTTP as
  in-scope and correct the decision text; (b) fail closed when there is no circuit (e.g. gate on
  `RendererInfo.IsInteractive`), accepting a restricted-surface flash before hydration; (c) keep the behavior and
  correct only the precedence wording. Owner input required.
  [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:30]

- [x] [Review][Patch] `ApplyAuthenticationStateChangedAsync` writes `_authorizationReflection = Authorized` inside
  its dispatcher callback with no transition-version guard — the exact guard `ReauthorizeAsync` was patched to add
  in this same diff. The version is checked at `:1541` (before the resolve) and `:1562` (after the write), never
  inside the callback. A sign-out landing while the resolve is in flight is overwritten by the pre-sign-out
  answer, re-rendering the grant form and remove launcher for a signed-out operator; if the newer transition then
  faults at `await authenticationStateTask` it returns without re-collapsing, so the privileged surface persists
  with no path that corrects it. Violates AC2 [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1554]
- [x] [Review][Patch] `OnInitializedAsync` awaits `ResolveAuthorizationReflectionAsync()` before attaching
  `AuthenticationStateChanged`, and captures no transition version. A sign-out during that initial resolve fires
  against no handler and is lost; the pre-sign-out `Authorized` is then written unconditionally and the full
  privileged surface renders for a signed-out principal until a page reload. `TenantDetailPage.OnInitialized:342`
  subscribes synchronously before any resolve; that ordering was not applied here. Violates AC2
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:866]
- [x] [Review][Patch] The `roles` whitespace fix was applied to shape detection only. `ResolveRoleCollection`
  computes `trimmed` and uses it for the `[`/`{` checks and `JsonSerializer.Deserialize`, but the delimiter branch
  still splits the untrimmed `value` on `[' ', ',']`, and `IsGlobalAdministratorValue` compares by exact equality.
  A `roles` claim of `"\tglobal-admin"` (tab, CR or newline) therefore yields a *definite* `NonAdministrator`,
  while `" global-admin"` (space) authorizes. The same asymmetry exists between claim types: `IsMalformedScalarRole`
  trims but `ResolveClaim:150` then evaluates the untrimmed `claim.Value`, so scalar `role=" global-admin"` denies
  where `roles=" global-admin"` authorizes. The outcome is `MissingPermission`, a terminal surface that renders no
  Retry (only `Indeterminate` does), so a legitimate global administrator is locked out for the session. The
  comment at `:163-165` claims this class of bug is fixed [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:191]
- [x] [Review][Patch] `MaxLength="MaximumGrantUserIdLength"` renders the DOM `maxlength`, which truncates typed and
  pasted input, so the `normalizedUserId.Length > MaximumGrantUserIdLength` half of the submit guard is unreachable
  from the real UI: a pasted 300-character id is silently shortened to 256 and dispatched as a platform-authority
  grant against a different identity, with no validation message. `TenantsWorkspace.razor:71` pairs the same
  attribute with an explicit too-long notice precisely because it is presentational. The new test reaches the
  guard only because bUnit's `.Change()` bypasses the DOM constraint
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:203]
- [x] [Review][Patch] `_authorizationPagingReset` is a one-shot notice with no unconditional clear. It is cleared
  only inside `LoadAsync`'s `CanApply`-guarded apply block and in `CollapseAuthorizationAsync`; `RefreshAsync`,
  `ResetPagingAsync` and both requery paths clear the sibling `_pagingJumpedToFirstPage` but not this one. A
  superseded or faulted load therefore strands "Authorization changed. The review restarted at the first page." in
  the polite live region for the life of the circuit — the stale-one-shot failure the comment at `:1037-1039`
  documents as already fixed for the sibling flag. It is also set unconditionally after every transition,
  including silent token renewals where paging never moved
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1577]
- [x] [Review][Patch] The Refresh button neither takes the atomic page-load gate nor binds `Disabled`, while Retry,
  Reset, Next and Previous all do. Clicking Refresh during an in-flight Next bumps `_loadGeneration`, cancels
  Next's token and makes its `LoadAsync` return `null`, so no cursor is pushed: the operator's Next click produces
  no navigation, no notice and no lifecycle state. AC4's "newest scoped request" rationale does not cover this —
  Refresh re-reads the same cursor rather than supplying fresher evidence
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:432]
- [x] [Review][Patch] `_readRefreshSubscriptionInFlight` is a plain `bool` test-and-set, not the
  `Interlocked.CompareExchange` gate this same diff introduced for `_pageLoadInFlight`. It is entered from
  `OnAfterRenderAsync` (dispatcher) and from `ApplyAuthenticationStateChangedAsync:1582`, which resumes on the
  thread pool after a `ConfigureAwait(false)`. Both can pass the guard, both subscribe, and the loser's `finally`
  clears the flag while the winner is still in flight. `_readRefreshAttempts++` is non-atomic on the same paths,
  so the bounded budget that exists to stop unbounded round trips can be overshot
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1211]
- [x] [Review][Patch] Hoisting `ReauthorizeAsync` to be the submit handlers' first await moved the whole body
  off-dispatcher, but only the `RequestSent` render was marshalled. The `Blocked` snapshot write (`:1780`), the
  `UserIdRequired` message and `_focusGrantUserIdPending` (`:1786`), and the new `UserIdInvalid` rejection (`:1794`)
  all mutate renderer-read state from a thread-pool continuation with no dispatcher hop, so the focus move to the
  user-id field can be lost and the assertive validation message announced with focus left on the submit button.
  On the remove path `SubmitRemoveAsync` re-reads `_removeSnapshot.Intent` at `:1984` off-dispatcher after
  checking it at `:1979`; a concurrent `CancelRemoveAsync` in that window makes `request` null and the resulting
  `ArgumentNullException` is not matched by the `OperationCanceledException` filter
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1780]
- [x] [Review][Patch] `RetryAuthorizationAsync` is the only page-load handler that releases its exclusion gate from
  inside `await InvokeAsync(...)` in the `finally`; the four siblings call `EndPageLoad()` directly. If the circuit
  tears down while the BFF resolve is suspended, the dispatch throws `ObjectDisposedException`, `EndPageLoad()`
  never runs and the exception escapes a `finally`. Two sibling dispatches on this page (`:1171-1182`) and on
  `TenantDetailPage.razor:520` catch exactly this [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1106]
- [x] [Review][Patch] Story records contradict the code and the frontmatter. (a) The Review Triage Log still ends
  "Story set to `in-progress`: 17 patch findings remain as action items" and "17 left as action items" while the
  frontmatter says `review` and the Completion Notes claim all 18 resolved — the loop-1 paragraph got an explicit
  "Superseded by" marker, the loop-2 paragraph did not. (b) The loop-2 decision text says the `reauthorize: false`
  suppression "are removed", but `GlobalAdministratorsPage.razor:1567` still passes `reauthorize: false`. (c) The
  File List declares `sprint-status.yaml` as "updated by this run"; it is unmodified. (d) `deferred-work.md:819-826`
  and the triage-log rationale both assert `IsGrantSubmitDisabled` "never depended on in-flight state" — it does,
  via `GrantUnavailableReason:725-728`, and `IsRemoveSubmitDisabled:785-790` names `IsGrantInFlight || IsRemoveInFlight`
  outright, so a permanent ledger entry future sweeps read is factually wrong
  [_bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md:219]
- [x] [Review][Patch] Test gap on the loop-2 owner decision itself: no test anywhere constructs a single
  `ClaimsIdentity` carrying two `sub` claims, so neither half of "exactly one *distinct* `sub`" is proven — not
  that duplicate identical values authorize (the Keycloak id_token+userinfo shape the decision cites), nor that
  conflicting values still fail closed. Reverting to `subjects[0].Value` keeps the suite green and silently
  restores the lockout the decision was written to remove
  [tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs:148]
- [x] [Review][Patch] Sixteen further guards added by this diff are unverified, each with a named surviving
  mutation: the null-resolver fail-closed arm in `TenantsBffComposition:31` and the `ITenantsBffComposition`
  fail-closed default (every implementation overrides it); tenant-detail "fails closed while pending" and its
  superseded-generation guard (the stub resolves synchronously, so the pending window is zero-width); the
  `ReferenceEquals(_snapshot, snapshot)` requery supersession on both mutation paths; `ReauthorizeAsync`'s
  transition-version guard; the fail-closed half of the new Indeterminate Retry (the test flips the stub to
  `Authorized` before clicking); the faulted-detail arm of `RefreshTenantReadsAsync`; the `RequestSent` render
  marshalling (no test asserts either `RequestSent` string reaches the DOM); all three new support-safe
  `ToString()` overrides (the repo convention is an exact `ShouldBe` pin); the `roles` whitespace fix; two of the
  three notification-budget reopen sites; the Next blank-cursor disable; and resolver precedence. Three tests
  assert on raw source text (`ShouldNotContain` with hard-coded 8-space indentation) and pass under reformatting
  or relocation, and `Resolver_prefers_current_circuit_identity_over_stale_authenticated_http_identity` no longer
  has an HTTP identity to prefer over — `httpPrincipal` is now inert arrangement
  [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:742]
  — **CLOSED (2026-08-02, loop 4):** the loop-3 carry-forward set is covered with mutation-verified tests
  (1,887 → 1,915). Behavioral races plus structure pins for requery `ReferenceEquals`; grant-path
  `ReauthorizeAsync` transition-version guard; Indeterminate Retry fail-closed; combined-refresh fault
  containment; mid-flight `RequestSent` DOM; Reset/RetryAuthorization budget reopen; tenant-detail pending and
  superseded-generation; Refresh page-load gate; hardened regex source-structure assertions; dispose-vs-subscribe
  lease retention. Loop-3 closed half remains authoritative for the earlier subset.
- [x] [Review][Patch] The new French validation string is unaccented — "de 256 caracteres maximum, sans caractere
  de controle" should read "caractères" / "caractère" / "contrôle". The sibling key added in the same commit
  ("L'autorisation a changé… à la première page") is correctly accented. EN/FR key parity itself holds
  [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:2830]
- [x] [Review][Patch] `NextPageAsync` guards only on a blank `NextCursor`, while the `Disabled` binding it was
  aligned with now also checks `!HasMore` — the comment states the two "must match, as they do on the tenant-detail
  member pager", and `LoadNextMemberPageAsync:846` does check both. A click dispatched before the `disabled`
  attribute lands (the documented reason these handler guards exist) can page past a declared end
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1301]

- [x] [Review][Defer] A `Ready` snapshot reporting `HasMore` with a blank `NextCursor` is now a silent dead end:
  Next is correctly disabled, Previous is disabled on page one, and `CanRecover` deliberately excludes `Ready`, so
  neither Retry nor Reset renders and no notice explains it [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698]
  — deferred, needs a copy/design call on how to announce the condition rather than a mechanical fix
- [x] [Review][Defer] Authorization resolution is uncancellable from both pages: `ResolveAuthorizationReflectionAsync`
  and `TenantsWorkspace.razor:564` call the BFF seam with no token, so the loop-2 `WaitAsync(cancellationToken)`
  seam is inert for them, and `RetryAuthorizationAsync` holds the page-load gate across it — a hung provider leaves
  every recovery control disabled with nothing able to interrupt it
  [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1634] — deferred, same timeout-policy
  decision as the existing `EnsureReadRefreshLeaseAsync` `CancellationToken.None` deferral
- [x] [Review][Defer] AC5 remains partially unmet while the story sits in `review`: on narrow viewports the per-row
  Remove control is hidden with no per-row localized reason and the grant cell still renders an "available" string
  beside hidden controls [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] — deferred,
  already recorded by loop 2; re-confirmed still open

### Review Findings — loop 4 (2026-08-08, chunk 1: auth core)

Layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor completed. Verification Gap Reviewer returned empty
and is recorded as failed. Diff scoped to File List auth-core files vs baseline `2e61f57`. Acceptance Auditor
raised no AC violations for this chunk. Provider hang / `CancellationToken.None` and Audit sync-HTTP leftovers
were re-raised by agents and dismissed here as already deferred in loops 2–3.

- [x] [Review][Patch] Trim `global_admin` / `is_global_admin` claim values before `bool.TryParse` so padded booleans fail closed consistently with trimmed role tokens [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:141] — **APPLIED (2026-08-08):** `bool.TryParse` now runs on the trimmed span; focused auth suite 91/91.
- [x] [Review][Patch] Split role collections on every Unicode whitespace character, matching the comment that claims all whitespace is a separator [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:190] — **APPLIED (2026-08-08):** comma then `Split(null)` whitespace tokenization; em-space regression added.
- [x] [Review][Patch] Treat control characters in `eventstore:tenant` scope values as indeterminate evidence instead of a definite NonAdministrator miss [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:128] — **APPLIED (2026-08-08):** scope validation rejects control characters as indeterminate.
- [x] [Review][Patch] Fix misleading `httpPrincipal` resolver tests that never inject `HttpContext.User` and therefore do not prove HTTP identity is ignored [tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs:41] — **APPLIED (2026-08-08):** removed dead `httpPrincipal` helper parameter; renamed tests to state the real contract.
- [x] [Review][Patch] Assert the interface-default `ResolveLifecycleAuthorizationAsync` fails closed, not only `ResolveGlobalAdministratorsAuthorizationAsync` [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs:234] — **APPLIED (2026-08-08):** both interface-default seams asserted.
- [x] [Review][Patch] Add coverage for conflicting role grant plus explicit boolean denial (`roles`/`role` + `global_admin=false` → Indeterminate) [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:117] — **APPLIED (2026-08-08):** evaluator regression added.
- [x] [Review][Patch] Add coverage for authenticated principal with valid `sub` and zero administrator claim types → definite NonAdministrator/MissingPermission [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:95] — **APPLIED (2026-08-08):** evaluator regression added.

### Review Findings — loop 5 (2026-08-08, chunk 2: GA page/state/resources/tests)

Layers: Blind Hunter, Edge Case Hunter (retried after empty first pass), Verification Gap, Acceptance Auditor —
all completed. Diff scoped to File List GA page/state/resources/tests vs baseline `2e61f57` (~7,358 lines).
`validate-story-gitlinks.py` PASS (7 declared).

- [x] [Review][Decision] Ready page with `HasMore` and blank `NextCursor` is a silent recovery dead-end — **RESOLVED (2026-08-08, owner decision): option 1 — treat as recoverable.** Do not add bare `Ready` to `CanRecover`. Gate Retry/Reset on incomplete paging evidence (`HasMore && blank NextCursor`), add a localized incomplete-paging notice, and cover with a focused page regression. Converted to a patch below. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:706]
- [x] [Review][Patch] Treat Ready+HasMore+blank-NextCursor as recoverable: condition-gated Retry/Reset plus localized incomplete-paging notice (do not broaden `CanRecover` to all Ready) [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:706] — **APPLIED (2026-08-08):** `HasIncompletePagingEvidence` / `HasIncompletePopulationOnReady` gate `CanRecover`; IncompletePaging notice + page regression.
- [x] [Review][Patch] Localize grant/remove `ConfirmProjection` `SafeMessage` strings (currently hardcoded English) into EN/FR whole-string resources [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:182] — **APPLIED (2026-08-08):** ConfirmProjection stores `Tenants.*` keys; page resolves via `ResolveSafeMessage`; EN/FR parity.
- [x] [Review][Patch] Close AC5 on narrow viewports: do not advertise Grant.Available beside CSS-hidden initiation; when per-row Remove is hidden, render a localized mobile/unavailable reason [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:493] — **APPLIED (2026-08-08):** Available claim uses `mutation-initiation`; mobile-only reason spans for grant/remove.
- [x] [Review][Patch] Hide grant/remove Cancel and status-Refresh on narrow viewports with the other mutation actions (they remain operable inside still-visible mutation sections) [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:252] — **APPLIED (2026-08-08):** Cancel/Refresh carry `global-admins__mutation-initiation`.
- [x] [Review][Patch] Dispose the failed notification lease when `SubscribeAsync` returns `IsSubscribed == false` before charging the attempt budget [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1294] — **APPLIED (2026-08-08):** dispose + null local lease before return.
- [x] [Review][Patch] Marshal grant/remove `RequestSent` + submitting-flag writes through `InvokeAsync` and re-check `CanApply*Mutation` so cancel/collapse between `Begin*Mutation` and `RequestSent` cannot stick [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1898] — **APPLIED (2026-08-08):** grant and remove arm RequestSent only when generation still applies.
- [x] [Review][Patch] Guard grant/remove projection requery cursor resets with `CanApply*Mutation` before clearing `_cursorHistory` / jumping to page one [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1989] — **APPLIED (2026-08-08):** generation checked before and inside cursor reset, and before LoadAsync.
- [x] [Review][Patch] Stop using `*.Unavailable.Freshness` when `IsMutationEvidenceBacked` fails solely on non-Current projection lifecycle; add honest lifecycle-gated copy (parity with Configuration/EditMetadata) [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:723] — **APPLIED (2026-08-08):** split Freshness vs ProjectionLifecycle gates + EN/FR strings.
- [x] [Review][Patch] Align grant user-id help text with the comment that advertises the 256-character / control-character bound [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:2823] — **APPLIED (2026-08-08):** EN/FR help mentions 256 / no control characters.
- [x] [Review][Patch] When Remove is blocked by incomplete population evidence on an otherwise Ready page, offer Reset/recovery (or steer copy to first-page complete evidence) so the operator is not stuck [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:771] — **APPLIED (2026-08-08):** `HasIncompletePopulationOnReady` included in `CanRecover`.
- [x] [Review][Patch] Assert grant `ConfirmProjection` page-scoped SafeMessage (mirror remove `Page_scoped_absence_…`) [tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs:27] — **APPLIED (2026-08-08):** page-scoped vs DidNotConfirm key assertions.
- [x] [Review][Patch] Cover Unknown-with-rows list rendering (`ShouldRenderRows` includes Unknown) [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:609] — **APPLIED (2026-08-08):** retained-rows + blocked mutations regression.
- [x] [Review][Patch] Assert the grant user-id input exposes no `maxlength` attribute after MaxLength removal [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:199] — **APPLIED (2026-08-08):** maxlength null assertion.
- [x] [Review][Defer] Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify` because requery always loads page one — deferred, pre-existing by-design honesty path; page-scoped SafeMessages document the limit; expanding to search-by-id is out of this story's boundaries [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:178]
- [x] [Review][Defer] UnableToVerify copy mentions the tenant audit trail without an in-page link — deferred, pre-existing; navigation to audit is outside chunk-2 / story boundary [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184]

## Spec Change Log

- 2026-08-02: Closed the remaining loop-3 unverified-guard carry-forward with mutation-verified coverage
  (1,887 → 1,915). Story returned to `review`.
- 2026-08-01: Review loop 3 — 1 owner decision resolved (prerender fails closed), 14 patches applied,
  3 deferred, 1 dismissed. 21 mutation-verified tests added (1,866 → 1,887). Corrected a false premise this
  story had written into the permanent deferred-work ledger.
- 2026-08-01: Addressed code review findings - 18 items resolved.
- 2026-08-01: Completed the development loop: confirmed the strict literal-`sub` identity contract,
  added evaluator regression coverage, corrected the workspace dispatcher regression, reconciled the
  published dependency-pointer provenance, and validated the story for review.

## Review Triage Log

- 2026-08-08, review loop 5, chunk 2 (GA page/state/resources/tests, ~7,358 diff lines / 15 files vs `2e61f57`).
  All four layers completed (Edge Case Hunter empty on first pass, succeeded on retry). 1 decision raised and
  resolved (incomplete paging → condition-gated recovery), 13 patches applied, 2 deferred, 6 dismissed as noise.
  Focused GA suite: 161/161 passed. AC5 mobile gaps previously deferred in loops 2–3 closed. Story returned to
  `review` for remaining chunks (3) workspace + tenant detail, (4) artifacts optional.

- 2026-08-01, review loop 1, auth/navigation chunk: 1 decision resolved, 4 patches applied, 1 pre-existing issue deferred, and 1 candidate dismissed as noise. Blind-hunter and verification-gap layers timed out; edge-case and acceptance layers completed and were corroborated against the implementation and focused tests.
- Verification: UI test project and full solution built warning-clean; 214 directly affected UI tests, the story's 585-test focused UI lane, and 5 fixed-scope server authorization tests passed. The generic-query-path search and `git diff --check` passed.
- Superseded by review loop 2 (2026-08-01). At the time it was written this paragraph was already stale: the frontmatter said `review`, the File List declared `references/Hexalith.PolymorphicSerializations`, and the validator exited 0. Corrected here rather than left to contradict the frontmatter.

- 2026-08-01, review loop 2, chunk A (production source, 3,584 diff lines across 9 files; tests read as evidence but not line-reviewed). All four layers completed, none failed or timed out. 3 decisions raised and resolved by the owner, 32 patches raised, 14 applied, 17 left as action items, 1 withdrawn during application, 5 deferred, 4 dismissed as noise.
- Withdrawn during application: gating `RefreshFromNotificationAsync` on `_pageLoadInFlight` was raised as a finding but contradicts AC4 ("only the newest scoped request renders") and the existing regression `Newer_notification_refresh_rejects_a_late_cursor_result_and_preserves_cursor_history`. The patch was reverted and the intent recorded as a comment at the call site instead.
- Three further layer claims were rejected at triage after reading the code: the submit buttons were never disabled in flight (so the double-dispatch exposure is pre-existing, not a regression from the hoisted re-authorization); `Routes.razor` policy enforcement is satisfied by endpoint metadata as the Design Notes describe; and the mobile read-only surface is presentation over a server-authoritative boundary, as the spec intends.
- Verification after the applied patches: UI test project builds warning-clean in Release, and the full UI suite passes 1,849 / 0 failed / 0 skipped. `validate-story-gitlinks.py` exits 0 with all seven pointers declared. `git diff --check` clean; no generic query path in `src/Hexalith.Tenants.UI`.
- Story set to `in-progress`: 17 patch findings remain as action items.
- **Superseded by review loop 3 (2026-08-01).** The two lines above are historical: the dev run that followed
  closed all 17, and the frontmatter, Completion Notes and File List are authoritative over this paragraph.
  Loop 3 recorded this explicitly rather than leaving a second stale "in-progress" claim in the log, which is
  the same defect loop 2 corrected for loop 1.
- Correction to the loop-2 decision text above: it states the `reauthorize: false` suppression "are removed".
  Only the uncorroborated `Evaluate` call was removed. `LoadAsync(reuseETag: false, reauthorize: false)` is
  retained deliberately on the transition path, because the strict BFF seam has already resolved authorization
  a few lines earlier and the transition-version guard is re-checked before the load; re-resolving there would
  be redundant, not safer. Recorded here so the text matches what shipped.

- 2026-08-01, review loop 3, uncommitted working tree (3,270 diff lines across 21 files: the loop-1/loop-2
  patches plus the dev run closing 17 action items, tests line-reviewed this time). All four layers completed,
  none failed or timed out. 1 decision raised and resolved by the owner, 14 patches raised and 14 applied,
  3 deferred, 1 dismissed as noise.
- Story evidence was re-verified independently before triage rather than read from the Dev Agent Record, and it
  held exactly: Release build 0/0, UI suite 1,866 passed, gitlink validator exit 0 with all seven pointers
  declared, no generic query path, `git diff --check` clean.
- Dismissed at triage: the `Preview` last-administrator relaxation. The layered stop still holds through
  `HasPositiveRemovalPopulationEvidence`, which requires either complete evidence with more than one row or the
  service's own `HasMore`; what loop 2 removed was the inference of a platform-wide total from a single page,
  which was the actual defect.
- Two agent claims were corrected during triage rather than accepted: the blind-hunter's framing of the resolver
  fallback as reinstating the request principal on *every* non-circuit path is overstated (inside a circuit the
  injected provider is the circuit's own scoped instance) — only the prerender pass was affected, which is what
  the owner decision addressed. And the finding that the submit buttons are never disabled in flight is simply
  false; that claim had been written into the permanent deferred-work ledger by loop 2 and is now withdrawn there.
- A gap in this loop's own first attempt, recorded because it is the interesting part: the initial
  subscribe-before-resolve test passed under its own mutation. `LoadAsync`'s default `reauthorize: true`
  re-resolved and collapsed the surface anyway, masking the missed authentication event. It only became
  discriminating once the read surface was disconnected so no re-authorizing load could run. Every new test here
  was mutation-verified against the specific revert it is meant to catch.
- Verification after the applied patches: `Hexalith.Tenants.slnx` and the UI test project both build Release
  warning-clean; the full UI suite passes 1,887 / 0 failed / 0 skipped (1,866 before, +21 added).
  `validate-story-gitlinks.py` exits 0 with all seven pointers declared. `git diff --check` clean; no generic
  query path in `src/Hexalith.Tenants.UI`. All 22 modified files were already declared in the File List.
- Story set to `in-progress`: one patch item remains partially open — roughly half of the 17 unverified guards
  now have mutation-verified coverage, and the remainder are enumerated in that item with the reusable seam and
  two harness gotchas needed to finish them.
- **Superseded by review loop 4 (2026-08-02).** The remaining unverified guards were closed with focused
  mutation-verified tests; frontmatter, Completion Notes, and sprint status are authoritative over the
  historical `in-progress` line above.

- 2026-08-02, review loop 4 (dev continuation): closed the loop-3 carry-forward unverified-guard item.
  Extended `StubTenantsBffComposition` / `StubTenantCommandGateway` gates, added behavioral and structure
  regressions across Global Administrators and Tenant Detail, hardened brittle source-text assertions, and
  made `RefreshAsync` internal for the same in-flight gate testing pattern as Retry/Reset.
- Verification: Release solution and UI test project build warning-clean; full UI suite 1,915 / 0 failed /
  0 skipped. `validate-story-gitlinks.py` exit 0 with all seven pointers declared. `git diff --check` clean;
  no generic query path in `src/Hexalith.Tenants.UI`.
- Story set to `review`: no open patch action items remain (deferred items from earlier loops stay deferred).

## Design Notes

The registered policy controls discoverability and route presentation, while `GET /api/global-administrators` remains the final data authorization boundary. A claim-authorized caller can still be denied safely by the server; that denial must collapse the page to generic output before any subscription or privileged markup persists.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean build.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.Services.Configuration.TenantConfigurationReadPolicyTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` -- expected: all focused tests pass.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false && tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Queries.GetGlobalAdministratorsQueryHandlerTests` -- expected: fixed-scope authorization tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: solution builds with no warnings; record the exact asset-graph blocker if the known environment issue recurs.
- `rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` -- expected: no production generic-query read path.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md` -- expected: exit 0.

## File List

Re-cut by the Story 1.10 code review (decision D-B, 2026-07-29) so the boundary between the two stories is
stated once, consistently. This story has not run its own dev loop, so this list records the files that
already carry its implementation inside the 1.10 commit range.

Declared here only — their entire in-range net change is this story's work, and they were removed from
Story 1.10's File List:

- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

Declared here **and** in Story 1.10 — these files carry both stories' work in the same range, so the overlap
is intentional rather than a duplicate declaration:

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsReason.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsRequest.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSurfaceKind.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorRemoveCommandSnapshotTests.cs` — added by review
  loop 13 of Story 1.10. Its grant and snapshot siblings were already declared here as shared, and the
  `GlobalAdministratorRemoveCommandSnapshot.cs` production type above is declared here too; only this test
  file was missing. It is changed in the Story 1.10 range (+63) and is declared by Story 1.10 as well.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorsSnapshotTests.cs`

Added by review loop 1:

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantConfigurationEndToEndTests.cs`

Added by review loop 2 completion:

- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRow.cs`

Dependency pointers that moved inside this story's baseline range (`2e61f57..HEAD`). This story's baseline
sits *inside* Story 1.10's range, so it inherits the same six movements, which are carried by the published
dependency commits `f425b49` and `09947a2`. They are declared here for the same reason 1.10 declares them —
the commits are published, so reverting would mean creating new dependency reversions rather than
un-bundling unpublished work. Story 1.10 owns the range and records the provenance; this declaration is a
cross-reference, not a second claim of authorship.

Before this re-cut, Story 1.11 had no File List at all, so
`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md`
exited 1 with all six pointers UNDECLARED. Declaring those six made that historical range pass. Review loop 1
reported a seventh `references/Hexalith.PolymorphicSerializations` movement. This dev run declares that
published range provenance: commit `3503890` bundled the pointer update with the global-administrator changes
already attributed to this story, so reverting it here would create a new dependency rollback rather than
unbundle unpublished work.

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.PolymorphicSerializations`

Development artifacts updated by this run:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Scope Attribution — added by code review of Story 1.10 (2026-07-28)

This story's implementation landed inside Story 1.10's commit range rather than under its own key. The
1.10 review split the scope forward rather than by rewriting history, because commits `7d7b701` and
`536596f` are published on `origin/main`.

Work belonging to this story, already present in the tree:

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs` (+190) — strict tri-state
  evaluator, `ResolveAdministratorEvidence`, `explicitDenial` veto, `MissingPermission` state.
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs` (-203) —
  consolidated onto the evaluator above.
- `src/Hexalith.Tenants.UI/Services/Gateways/{I,}TenantsBffComposition.cs` —
  `ResolveGlobalAdministratorsAuthorizationAsync`.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` — `_canReviewGlobalAdministrators`,
  `GlobalAdministratorsHref`, `tenants-global-administrators-entry`.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/*` — `IsCompleteEvidence` and surface/reason additions.

Three decisions were transferred here from the 1.10 review because no acceptance criterion in either spec
covers them. All are security-relevant and must be resolved against this story's ACs before it closes:

- [x] [Review][Decision] Principal-resolution precedence was inverted — the circuit `AuthenticationStateProvider`
  now outranks `HttpContext.User`, where `HttpContext` was previously primary. **RESOLVED (2026-08-01,
  owner decision): retain circuit-over-HTTP precedence with no request-principal fallback.** The live circuit
  identity is authoritative for authentication transitions; stale HTTP evidence must not restore privilege.
  Anonymous, pending, or faulty circuit evidence fails closed, with the review's cancellation and non-blocking
  patches handling availability separately. [TenantConfigurationPrincipalResolver.cs:17-48]
- [x] [Review][Decision] `Evaluate` now requires exactly one authenticated identity carrying exactly one
  literal, non-whitespace, control-char-free `sub` claim. Any handler mapping `sub` to
  `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities
  (cookie + bearer), denies a genuine global administrator. Confirm against
  `docs/production-auth-claim-contract.md`. [TenantsGlobalAdministratorClaims.cs:36-46]
- [x] [Review][Decision] `LifecycleAuthorizationReflection` resolves the principal from `IHttpContextAccessor`, which
  is null for the whole interactive circuit, so `Evaluate(null)` returns `Indeterminate` permanently and
  `TenantDetailPage.razor:149` gates tenant lifecycle actions off for a signed-in global administrator for the rest of
  the session. Story 1.10 added `ResolveGlobalAdministratorsAuthorizationAsync` to the same type and migrated the
  workspace and global-administrators pages to circuit-aware resolution, but left the tenant-detail consumer on the
  synchronous `HttpContext`-only path. Transferred here by owner decision during the 1.10 chunk-A+B review
  (2026-07-30) so that this story's two principal-resolution decisions above and this one are settled as one coherent
  authorization change rather than two stories patching the same evaluator. Accepted interim consequence: tenant
  lifecycle actions stay `Indeterminate` for global administrators until this story lands.
  **RESOLVED (2026-08-01):** tenant detail now consumes `ResolveLifecycleAuthorizationAsync`, which shares the strict
  circuit-principal resolver, fails closed while pending or faulty, cancels superseded work, and reauthorizes on live
  authentication transitions. The synchronous request reflection is no longer consumed by tenant lifecycle actions.
  [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27]

This story completed review loop 1 (`review_loop_iteration: 1`), including the identity-shape decision and
gitlink reconciliation. Its story status and sprint status are `review`.

## Dev Agent Record

### Implementation Plan

- Resolve the remaining review actions in document order with focused red tests, minimal circuit-safe UI
  changes, and a full UI regression run after each completed action.
- Confirm the unresolved identity-shape decision against the production auth contract and effective OIDC/JWT
  configuration.
- Pin the strict fail-closed evaluator behavior with focused coverage, then validate all carried-in Story 1.11
  authorization and navigation changes.
- Reconcile baseline-range dependency pointers and run the full configured regression ladder before review.

### Debug Log

- The production contract and both authentication hosts preserve raw claim names (`MapInboundClaims = false`),
  so literal `sub` remains the trusted subject and multiple authenticated identities remain indeterminate.
- The first full UI regression run found one renderer-dispatcher failure caused by `ConfigureAwait(false)` in
  `TenantsWorkspace.razor`; removing those component awaits from the renderer path made the focused regression
  and full UI suite pass.
- The non-performance integration lane passed 166 of 168 tests. The two non-blocking Aspire topology tests were
  environment-blocked by an unreachable DAPR `statestore` health check (60-second timeout), followed by a
  command-path HTTP 500. `aspire doctor` passed the CLI, AppHost, SDK, Docker, and WSL checks and reported only
  the existing partial HTTPS trust warning; the story-relevant generated-controller lane passed 27 of 27.
- Final rerun of the non-performance integration lane discovered 168 tests: 146 passed, 2 Aspire topology
  tests failed, and 20 were skipped. The same environment blocker recurred — DAPR state/config traffic timed
  out despite healthy AppHost resource state, and the Aha command path timed out after 60 seconds. The
  story-relevant generated-controller lane independently passed 27 of 27.

### Completion Notes

- ✅ Resolved review finding [Patch]: closed the loop-3 carry-forward of unverified guards with mutation-verified
  coverage for requery supersession (behavioral + `ReferenceEquals` structure pin), grant-path
  `ReauthorizeAsync` transition-version, Indeterminate Retry fail-closed, combined-refresh fault containment,
  mid-flight `RequestSent` DOM, Reset/RetryAuthorization notification-budget reopen, tenant-detail pending and
  superseded-generation authorization, Refresh page-load gate, hardened source-structure assertions, and
  dispose-vs-subscribe lease retention. Full UI suite passes (1,915 tests). `validate-story-gitlinks.py` exit 0.
- ✅ Resolved review finding [Patch]: indeterminate authorization now exposes a safe retry that re-runs the
  strict BFF authorization seam before any privileged query or markup can return; focused coverage and the
  full UI suite pass (1,850 tests).
- ✅ Resolved review finding [Patch]: the rendered Global Administrators page now has regression coverage
  through the real circuit-aware principal resolver and BFF composition, including an authorization
  transition outside an inbound circuit activity; the full UI suite passes (1,851 tests).
- ✅ Resolved review finding [Patch]: workspace load replacement now cancels a superseded operation but
  leaves cancellation-source disposal to that operation's owner, with behavioral and source-structure
  regressions; the full UI suite passes (1,853 tests).
- ✅ Resolved review finding [Patch]: preserved notification-over-pager supersession because review triage
  withdrew the proposed gate as contrary to AC4; the focused late-cursor regression and the 1,853-test UI
  suite prove only the newest scoped request renders while cursor history remains valid.
- ✅ Resolved review finding [Patch]: strict authentication transitions now announce their first-page restart
  through a dedicated polite EN/FR recovery message after the authoritative reload; focused paging evidence
  and the full UI suite pass (1,854 tests).
- ✅ Resolved review finding [Patch]: the projection lifecycle badge and Retry/Reset controls now render as
  siblings of the assertive truth-state region, with a DOM-boundary regression; the full UI suite passes
  (1,855 tests).
- ✅ Resolved review finding [Patch]: every rows-free recovery entry point now reopens the bounded
  notification-subscription setup budget before its authoritative retry, with regression coverage for the
  exhausted Error surface; the full UI suite passes (1,856 tests).
- ✅ Resolved review finding [Patch]: grant submission now keeps control-character and over-256-character
  user identifiers local, supplies a whole-string EN/FR recovery message, and advertises the supported input
  bound; focused coverage and the full UI suite pass (1,857 tests).
- ✅ Resolved review finding [Patch]: combined tenant/member refresh now observes both reads independently,
  contains operational faults, applies a successful sibling result, and degrades retained member evidence
  instead of stranding it on Refreshing; focused coverage and the full UI suite pass (1,858 tests).
- ✅ Resolved review finding [Patch]: the member page-one jump notice is now one-shot evidence and clears
  after the next authoritative combined refresh; the real bounded-history pager regression and the full UI
  suite pass (1,858 tests).
- ✅ Resolved review finding [Patch]: tenant-detail notification setup now charges only failed or empty
  subscriptions and both explicit detail/member refresh paths reopen a bounded same-route budget; focused
  bounded/recovery coverage and the full UI suite pass (1,859 tests).
- ✅ Resolved review finding [Patch]: all Global Administrators paging and recovery handlers now acquire
  one atomic test-and-set gate, while rendered disabled state uses a volatile read; structure and behavioral
  mutual-exclusion regressions plus the full UI suite pass (1,860 tests).
- ✅ Resolved review finding [Patch]: asynchronous page initialization now resolves authorization off the
  renderer but marshals authorization, provider subscription, and terminal snapshot mutations back through
  the dispatcher before loading; focused structure/real-resolver coverage and the full UI suite pass (1,861 tests).
- ✅ Resolved review finding [Patch]: the authorized workspace entry now has a real paging regression proving
  the active opaque cursor remains in workspace navigation but is suppressed from the Global Administrators
  return context; the full UI suite passes (1,862 tests).
- ✅ Resolved review finding [Patch]: page-scoped remove preview coverage now renders and pins the honest
  "Administrators visible on this page" count label instead of the complete-platform label; the full UI suite
  passes (1,863 tests).
- ✅ Resolved review finding [Patch]: both initial tenant-detail read tasks now have fault-injection coverage
  proving operational details stay contained, each failed surface terminates honestly, and the sibling task is
  still observed; the full UI suite passes (1,865 tests).
- ✅ Resolved review finding [Patch]: notification setup failure now has an injected `ILoggerFactory`
  regression proving the warning carries only the fixed `notification-setup-failed` reason code, no scope or
  exception detail; the full UI suite passes (1,866 tests).
- ✅ Resolved review finding [Patch]: retained the already-corrected superseded review-loop paragraph and
  closed its stale finding; the historical review-loop-2 `in-progress` entry remains explicitly historical,
  while the declared dependency pointer and current completion state are authoritative.
- Kept strict single-identity, literal-`sub` authorization because Tenants JWT bearer and FrontComposer OIDC
  preserve raw claim names; mapped aliases and multi-identity principals fail closed instead of widening trust.
- Added evaluator coverage for the accepted literal subject and rejected mapped/multiple-identity shapes.
- Preserved the prior review-loop fixes for circuit authorization, cancellation, non-blocking workspace loading,
  and tenant lifecycle reauthorization, and corrected their Blazor dispatcher regression.
- Declared `references/Hexalith.PolymorphicSerializations` because published commit `3503890` moved the pointer in
  the same baseline range as this story's global-administrator changes; this run did not move the pointer.
- Release solution and project builds completed with zero warnings/errors. Full passing suites: Contracts 120,
  Client 50, Testing 181, Sample 39, Server 738, UI 1,866; focused story UI lane 599, fixed-scope server lane 5,
  and generated-controller integration lane 27. The broader non-blocking Aspire lane is recorded separately
  above with its exact environment blocker and totals.
