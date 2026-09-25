---
title: 'Reach scoped audit evidence from context'
type: 'feature'
created: '2026-09-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 7
baseline_commit: fc147e3efab879f0d5f8131a4ebfd178bf3a113c
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/planning-artifacts/epics.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Existing tenant, membership, and command audit links predate the corrected Story 5.2 contract. They can expose a route from uncertain context, lose origin focus, or suggest that a user hint filters all evidence.

**Approach:** Reverify each contextual entry point, make route and availability decisions fail closed, preserve safe origin context and focus, and show the audit page's filtering limits honestly. Reuse the existing tenant audit page, typed availability, and localized UI.

## Boundaries & Constraints

**Always:** Audit remains a tenant-scoped contextual route. A link requires an explicit safe tenant id and reliable authorization, scope freshness, and audit-read capability. User and command context are display hints unless the authoritative audit read supports a filter. Keep pending, delayed, unavailable, available, and missing-support states distinct; preserve approved return state and restore focus where possible. Use whole EN/FR strings, FrontComposer/Fluent v5, and stable accessibility selectors.

**Never:** Add a global Audit navigation entry, unscoped audit query, browser backend call, new audit endpoint, receipt, correction command, or fabricated command proof. Do not place raw message/correlation identifiers, payload, protected cursor, ETag, token, or unsafe reference in URLs or rendered output. Do not edit the existing user changes in `deferred-work.md` or `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Scoped entry | Authorized current tenant row, detail, membership row, or command context | Tenant audit route with safe source/return context | No audit query at origin |
| Uncertain entry | Missing, stale beyond safe use, unauthorized, or unsupported scope/read | Disabled entry with localized reason and applicable recovery | No link or scope discovery |
| User hint | Safe user id from lookup or member row | Banner names a contextual target, while grid remains tenant/date/category filtered | Never imply exhaustive user results |
| Command handoff | Typed audit state and no approved command reference | Tenant list link only when audit read is usable; show exact availability | Acceptance and projection confirmation are not proof |
| Return | Origin state and launch anchor exist or are gone | Restore safe state and launch focus; otherwise heading focus and visible notice | Reject external, encoded, or unsafe return/focus context |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor` -- shared link/disabled component; its current `CanOpen` and return URL checks are too permissive. Reuse its selectors and Fluent controls.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` and `Components/Tenants/Audit/AuditAvailabilityState.razor` -- existing canonical command audit states and recovery verbs; do not add string state tokens.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`, `Components/Pages/TenantDetailPage.razor`, `Components/Users/MyTenantsDataGrid.razor`, `Components/Users/UserMembershipLookupPanel.razor`, and `Components/Tenants/Members/MemberAccessReview.razor` -- existing row/detail/member sources; supply reliable availability and origin state.
- Eight command flows under `src/Hexalith.Tenants.UI/Components/Tenants/` -- existing audit handoffs; preserve lifecycle and route safe context.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs` and `TenantWorkspaceState.cs` -- canonical list return state; audit handoff currently drops the cursor.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- direct BFF audit read, context banner, stronger return URL parser, and back link; display hints must be labeled and return focus honored.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources{,.fr}.resx` -- EN/FR whole-string labels, reasons, and notices.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs` and `Components/Pages/TenantsWorkspace.razor:616` -- `ResolveGlobalAdministratorsAuthorizationAsync` is the existing read-authority reflection; the audit query handler authorizes global administrators, so connection alone cannot enable links.
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js` and `tests/Hexalith.Tenants.UI.Tests/Browser/tenants-focus-browser-validation.html` -- focus helper and real DOM fixture; list and lookup anchors are in sibling grid cells, and child rows may render after their parent page.
- Return context must survive every canonical navigation: standalone lookup, member row, detail heading, and detail command flows. Keep the original safe nested `returnUrl` intact through audit → detail → list, and consume `auditFocus`/`auditPartialReturn` once per URL rather than retaining stale flags. Avoid duplicate query keys.
- Focus ids derived from valid user identities may contain `+`; restrict focus to known launcher prefixes while accepting valid id characters. A disabled launcher can mean authorization is pending, so focus waits for an authority terminal state, not an early page header. The browser fixture must use the same Fluent anchor element as production.
- Audit-read reflection must be retried by every advertised refresh action, including workspace and standalone pages. A caller change must synchronously clear links and version both standalone authority resolution and the detail capability probe; delayed old-caller results cannot restore availability.
- `TenantAuditPage` must reject an unsafe route `TenantId` before any BFF read or subscription, including a direct deep link. Test direct routes as well as generated links.
- The safety validator must reject dot segments, malformed/repeated-slash paths, JWT-shaped identifiers, and protected values after bounded repeated decoding. Preserve a return focus only when its matching safe return route is retained. An invalid direct route must dispose prior read leases and clear retained paging/selection state.
- Every visible Refresh control on standalone and detail surfaces must retry audit-read authorization/capability as well as its ordinary read, while preserving command state. Caller-task failure must resolve authority/capability to a terminal unavailable state so return focus falls back promptly.
- Standalone heading fallback must use a focusable heading. Browser DOM tests must cover My Tenants/user lookup with pending authority and delayed content using Fluent anchors and production terminal markers. Audit return-context copy must describe the source in localized user terms, not print a DOM element id.
- Standalone My Tenants paging and detail links must preserve `/tenants/my` across list → detail → audit → detail → list. Command lifecycle `returnFocus` targets need matching focusable DOM ids. Reject unsafe return state by disabling an entry with an honest reason; never silently substitute `/tenants`.
- Token protection must recognize actual credential shapes while preserving ordinary search text, including `tokenization` and a literal escaped `%`. Audit focus markers must be scoped to the active origin surface, and a loading origin must not become terminal merely because its capability probe completed. Verify already-enabled links close on caller change at standalone and detail sources.
- `Components/Pages/MyTenantsPage.razor` and `UserMembershipLookupPage.razor` -- standalone routes require their own authorized entry and safe return-focus restoration; their panels currently build workspace URLs. Authentication changes must clear visible links on the renderer before awaiting the new caller.
- `Components/Pages/TenantDetailPage.razor` -- authentication changes must invalidate the independent audit capability probe, lifecycle must be current, and audit return must retain a safe incoming list return URL. The detail heading's audit control is in sibling metadata, not inside the heading.
- `Components/Tenants/Members/MemberAccessReview.razor` -- member identity lacks the id supplied as return focus, and both detail and member lifecycle must be current.
- `Components/Tenants/Audit/AuditEvidenceEntryPoint.razor` and command parent callbacks -- a full reload loses in-memory command state; refresh audit availability through the owning surface while preserving typed command state.
- `tests/Hexalith.Tenants.UI.Tests/Components/` and `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- component and hosted route evidence.
- Review 6 hardening: keep audit authority unresolved throughout a pending caller change, including every Refresh action. Decode each return-query layer once while scanning bounded encoded credential forms; strip a cursor before inspecting its protected value, and carry the partial-return notice through audit, detail, and list. Restore a create-flow audit return from circuit-local state and expand its enclosing accordion; resolve missing focus after the active origin route becomes terminal.
- Review 7 hardening: resolve absent-BFF and failed-caller states as terminal unavailable, clear handled focus and notices after ordinary navigation, cancel pending focus observers on route exit, expose disabled reasons to assistive technology, refresh stale member evidence, and hand unsafe audit returns to a visible workspace notice and heading. The standalone lookup route must read results after canonicalizing while keeping only safe focus and partial-return markers.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor` and `State/TenantAudit/TenantAuditNavigationSafety.cs` -- validate route inputs and known focus prefixes including valid `+` user ids; reject dot segments, repeated slashes, twice encoded tokens/ETags, and token-shaped identifiers; default link availability to false, strip protected cursors, preserve safe search, reject recognizable credential shapes while allowing ordinary `tokenization` and literal `%` search, disable invalid return state instead of substituting `/tenants`, avoid duplicate query keys, and refresh through an owner callback.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, standalone pages, authority boundary, and panels -- require current authorized rows, connected BFF, and authorized audit-read reflection; invalidate on caller changes, ignore stale results, retry reflection on every page/audit Refresh and resolve caller-task failure as terminal, and preserve `/tenants/my` through paging and detail drill-in and focus/notice through canonical lookup navigation; test an already-enabled link closing on caller change.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`, `Components/Tenants/Members/MemberAccessReview.razor`, and eight command flows -- version detail capability across caller changes, retry it on every detail/audit Refresh, and resolve caller-task failure as terminal, require current lifecycle/freshness, preserve safe nested list/standalone context through every audit return and give each command focus target a real focusable DOM id; test available-to-pending caller transition, and retain typed command states on refresh.
- [x] `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs` and `Components/Pages/TenantAuditPage.razor` -- show partial-return notice for stripped cursors, retain safe list/standalone state, reject unsafe direct tenant routes before BFF work and dispose prior leases/clear paging; carry focus only with its validated return route and localize return-context copy, label hints honestly, and canonicalize the return link without duplicate focus keys.
- [x] `src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js`, return handlers, and real DOM browser fixture -- wait for authority and origin readiness with terminal markers scoped to the active origin and only after the relevant content completes; do not fall back while still loading based only on a fixed timer or completed capability probe; find sibling and detail-header Fluent anchor controls; restore launch focus or focusable visible heading/notice and clear stale notice flags; browser-test delayed standalone authority/content.
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources{,.fr}.resx`, focused UI and hosted route smoke assertions -- maintain localized, accessible states; test delayed old-caller authority/capability completion, real Fluent anchor focus, nested return, direct unsafe route, and refresh recovery.

**Acceptance Criteria:**
- Given an authorized current tenant scope, when any required source opens audit, then the direct BFF page carries only safe scope and return context.
- Given uncertain scope or support, when an entry renders, then it shows a localized reason without a usable link.
- Given user or command hints, when audit loads, then it claims neither unsupported filtering nor proof.
- Given a return from audit, when the origin is present, then its state and launch focus return; otherwise heading focus and a notice appear.
- Given keyboard, screen-reader, mobile, or forced-colors use, when an entry renders, then its name, focus, status, and recovery remain clear.

## Implementation Notes

- Seventh-pass verification: Release UI and integration-test project builds passed with zero warnings/errors. The serial full UI suite passed 3126/3126 with zero skips after review fixes, including a rendered standalone lookup route, workspace create return, authority failure recovery, repeated focus, and unsafe return fallback. The Chrome Fluent DOM focus fixture passed after route-exit and empty terminal-attribute fixes. `git diff --check` passed. Hosted route smoke remains unexecuted because the earlier Aspire EventStore `/alive` fixture timed out before assertions; the hosted test project builds.

- Sixth-pass verification: Release UI and integration-test project builds passed with zero warnings/errors. The serial full UI suite passed 3097/3097 with zero skips after focused unsafe-heading, Unicode, standalone cursor, and rendered nested-return fixes; the affected focused classes passed 345/345. The Chrome Fluent DOM focus fixture passed. `git diff --check` passed. Hosted route smoke remains unexecuted after the earlier Aspire EventStore `/alive` fixture timeout; its assertion compiles.

- Fifth-pass verification: Release UI and integration-test project builds passed with zero warnings/errors. The serial full UI suite passed 3096/3096 with zero skips after focused detail, direct-route, and Unicode safety fixes. The Chrome DOM focus fixture passed, including delayed standalone content/authority and heading fallback. `git diff --check` passed. Hosted route smoke remains unexecuted because the prior Aspire EventStore `/alive` fixture initialization timed out before any assertion; its changed assertion builds.

- Fourth-pass verification: Release UI build succeeded with zero warnings/errors. Full UI suite passed 3080/3080 with zero skips before two additional detail regression tests; those delayed-caller and nested-return tests passed 2/2. The Chrome focus fixture passed with Fluent anchor controls, and the integration-test project built. Hosted route smoke remains blocked by the previously observed EventStore `/alive` fixture timeout; its assertion was compilation checked. `git diff --check` passed.

- Added `TenantAuditNavigationSafety` for approved scope, hint, source, return, and focus values. The entry component blocks unsafe supplied context and exposes a localized refresh recovery. List and user grids now default to unavailable and require current projections plus connected BFF reads; detail/member links retain the existing authoritative capability gate. Command handoffs retain the canonical typed availability component and never infer proof from acceptance.
- Return links omit protected cursors and mark that loss as partial restoration. Workspace and detail restore launch-control focus when present; otherwise they focus a heading and show a localized notice. The existing one-row detail capability probe remains independent of entry clicks because command proof gating also uses it; the entry itself performs no audit query.
- `TenantSummaryProjectionPage` now uses `FcPageLayout` and `FcPageHeader` so the full UI conformance gate passes. The static global-administrator route test now checks the shared return validator.
- Verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -m:1 --no-restore` passed with zero warnings/errors; `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -parallelMode none` passed 3092/3092, zero skipped. The integration test project also built with zero warnings/errors. `git diff --check` and `git diff --cached --check` passed.
- Hosted smoke blocker: `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests -class Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests -parallelMode none` failed all six cases during Aspire fixture initialization: `Resource 'eventstore' endpoint '/alive' did not return HTTP 200 within 00:04:00. Last status: n/a, Last error: The operation was canceled.` No hosted route assertion ran. Its audit assertion was updated to the new contextual hint and safe `auditFocus` return URL, and the project rebuild passed.
- Review loop 1 reverted the prior code and test diff before re-derivation. The preceding verification is historical evidence for that discarded diff; the current implementation must be rebuilt and retested.
- Second-pass verification: Release UI and integration-test builds passed with zero warnings/errors. The full UI suite passed 3076/3076 with no skips; the focused entry-point suite passed 17/17 after the explicit member-read gate was added. The browser DOM focus fixture passed before the final member gate change, which does not touch focus code. Hosted route smoke remains blocked by the EventStore `/alive` fixture timeout described above; its changed assertion compiles but did not execute.
- Third-pass verification: Release UI and integration-test builds passed with zero warnings/errors. The full UI suite passed 3075/3075 with no skips after the raw message-id and audit-read reason fixes. Two subsequent return-notice test cases passed in a focused workspace class run (32/32), and the Release build passed again. The Chrome DOM focus fixture passed row, member, detail, delayed, denied, and missing-origin cases. `git diff --check` passed. Hosted smoke was build-checked but not rerun because the prior EventStore `/alive` fixture timeout prevented all hosted assertions.
- Final URL safety audit limited return-focus IDs to known launch anchors and canonicalized a repeated partial-return marker. Release UI build passed, focused entry tests passed 14/14, and the complete UI suite passed 3077/3077 with no skips. Hosted smoke remains unexecuted for the EventStore fixture reason above.

## Spec Change Log

- Review loop 7, continued under the user's approval: review confirmed repeated-focus state, missing terminal state after caller failure or absent BFF, disabled-reason accessibility, member refresh and unsafe return fallback gaps, and missing rendered standalone/create route evidence. The rendered lookup test also exposed a standalone route that redirected before querying; a DOM check showed empty boolean data attributes must count as true. The code map now requires terminal failure and route-aware focus cleanup, accessible reasons, explicit fallback notices, and a standalone lookup read before canonical navigation. KEEP: authorization and safety guards from review 6, typed command state, one-time circuit create return, Fluent focus fixture, EN/FR notices, and safe nested return. Direct focused fixes retained the working implementation under the user's workflow-cap override.

- Review loop 6, continued with the user's explicit authorization beyond the five-loop cap: review found pending-caller Refresh races, unsafe credential aliases in return search, nested return decoding that changed valid search and focus text, lost partial-return notices, a missing-origin focus wait, and create-flow state lost on return. The nonfrozen Code Map now requires fail-closed caller refresh, layer-aware URL handling, cursor-first stripping, propagated partial notices, terminal focus fallback, and circuit-local create return state. KEEP: current typed audit availability, global-administrator reflection, scoped BFF reads, nested safe return, Fluent focus controls, EN/FR notices, command lifecycle preservation, and existing Story 5.2 tests. Avoid the known-bad sixth-pass behavior while keeping the existing diff for focused correction under the user's override.

- Review loop 5: review found standalone My Tenants navigation slipping to workspace, command focus targets without ids, valid search text rejected as token-like, silent return-route substitution, and focus waits affected by unrelated tab markers or fixed timeout. The Code Map and tasks now require exact standalone route preservation, real command DOM targets, narrow credential-shape rejection with ordinary `%` search support, fail-closed invalid return state, scoped terminal focus, and available-to-pending caller tests. KEEP: global-administrator authority reflection, caller-versioned detail probe, safe nested return, direct unsafe-route cleanup, typed audit state and command preservation, protected cursor notice, EN/FR contextual copy, Fluent browser fixture, FrontComposer conformance, and compile-checked hosted assertion. Do not add audit queries at list/standalone entry creation. Fifth-pass code and tests were reverted for re-derivation; its 3096/3096 serial UI result is historical evidence for the discarded diff.

- Review loop 4: review found dot-segment and repeated-slash route acceptance, twice encoded token-like values, stale read leases after invalid deep links, audit refresh controls that do not recover authority/capability, unresolved caller-failure focus waits, and nonfocusable standalone heading fallback. The Code Map and tasks now specify strict route/identifier safety, cleanup, refresh of both reads and audit authority, terminal failure behavior, focusable headings, and delayed standalone DOM tests. KEEP: typed command audit state, conjunctive cascaded command gate, safe nested detail/list return, caller-versioned probe, one-level return URL bound, no raw message id handoff, direct BFF destination only, EN/FR hint copy, Fluent focus fixture, FrontComposer repair, and hosted assertion. Do not add an audit query just to draw a link. Fourth-pass code and tests were reverted for re-derivation; 3080/3080 UI and later 2/2 focused detail tests are historical evidence for the discarded diff.

- Review loop 3: the third pass lost canonical lookup and nested detail return context, treated pending disabled launchers and loading headers as terminal, failed to retry audit-read reflection on refresh, and could accept a prior caller's detail capability result. The Code Map and tasks now specify canonical context preservation, caller-versioned capability, authority recovery, valid `+` focus ids, terminal-aware Fluent focus, safe direct routes, and one-time return notices. KEEP: the shared route validator, default-disabled entries, typed availability and command state, global-administrator reflection, independent detail capability probe, partial cursor notice, EN/FR contextual-hint copy, real Chrome DOM fixture, FrontComposer repair, and hosted assertion. Do not add an audit query merely to draw an entry. The third-pass code and tests were reverted for re-derivation; its 3077/3077 UI result and Release builds are historical evidence for the discarded diff.

- Review loop 1: review found audit links gated by read connectivity rather than the audit handler's global-administrator authority, and return focus could target sibling cells too early. The Code Map and tasks now require shared authority reflection at every source, explicit command gates, asynchronous origin readiness, real DOM focus evidence, a genuine refresh recovery, and safe ordinary search restoration. This avoids unauthorized or dead links and false focus restoration. KEEP: the typed audit availability model, shared route safety validator, protected-cursor stripping with an honest partial notice, EN/FR user-hint copy, current BFF audit destination, and FrontComposer conformance repair. The historical detail one-row audit capability probe supports command proof independently of entry clicks; do not add an audit query to entry creation.
- Review loop 2: the second pass left ordinary heading focus cancelled, detail/member launch focus unresolved, audit links stale across caller change, standalone and nested return context lost, and command refresh destructive to the lifecycle state. The Code Map and tasks now require renderer-safe authorization invalidation, re-probing detail capability, current lifecycle gates, exact origin route/focus preservation, terminal-aware focus, and owner refresh callbacks. KEEP: default-disabled audit entries, global-administrator authorization checks, the existing independent detail capability probe, the shared safe-route validator, protected-cursor notices, EN/FR contextual-hint copy, typed command availability, the real-DOM browser fixture, FrontComposer conformance repair, and the hosted assertion update. Do not introduce an audit query merely to draw an entry link. The second-pass code and tests were reverted for re-derivation; its 3076/3076 UI run and integration build are historical evidence for the discarded diff.

## Review Triage Log

- Review 7 verification gap 1 — medium, patch: standalone user lookup page tests do not exercise the page's resolved audit-read handoff into the result link; add a rendered authorized route assertion.
- Review 7 verification gap 2 — medium, bad_spec: direct create-flow tests do not prove the workspace passes restore state and opens the enclosing accordion; add a return-route assertion or equivalent rendered route evidence.
- Review 7 edge 1 — medium, patch: detail retains the handled focus value when query focus disappears, so a second return to the same component can skip restoration.
- Review 7 edge 2 — medium, patch: My Tenants retains the handled focus value across a no-focus navigation, so a second same-focus return can be skipped.
- Review 7 edge 3 — medium, patch: standalone user lookup has the same retained handled-focus problem.
- Review 7 edge 4 — medium, patch: workspace resets handled focus but retains prior return and partial notices on a later ordinary visit.
- Review 7 blind 1 — false: `TenantListNavigationContext.ToDetailUrl` explicitly sets `Cursor = null` before encoding its nested return URL.
- Review 7 blind 2 — medium, patch: workspace missing-BFF branch returns before setting `_auditAuthorityResolved`, leaving focus waiting for a terminal state.
- Review 7 blind 3 — false: with a valid lookup user, `RunLookupAsync` installs Loading before awaiting the gateway; a null snapshot is the no-query terminal state, where heading fallback is appropriate.
- Review 7 blind 4 — medium, patch: after a failed caller task, standalone Refresh cannot resolve authority but clears `IsResolved`, leaving focus pending.
- Review 7 blind 5 — medium, patch: detail capability Refresh likewise resets resolved state after a failed caller task and cannot start its guarded probe.
- Review 7 blind 6 — medium, patch: same My Tenants and user lookup repeated-focus defect as edge 2 and edge 3; correct each page once.
- Review 7 blind 7 — medium, bad_spec: focus helper can retain a pending MutationObserver across route navigation if the prior origin never settles; add cancellation on navigation.
- Review 7 blind 8 — medium, patch: disabled audit entry's `aria-label` overrides its visible reason; connect the localized reason with `aria-describedby`.
- Review 7 blind 9 — medium, patch: member audit Refresh only retries detail audit authority and cannot repair stale member evidence; request the member projection refresh too.
- Review 7 blind 10 — medium, bad_spec: an invalid or absent return URL falls back to `/tenants` without an explicit notice or heading-focus handoff.

- Review 6 verification gap 1 — medium, patch moot at loop cap: command Refresh invokes a new cascaded audit capability callback, but no rendered command test proves unavailable-to-available recovery; existing tests cover the ordinary status callback only.
- Review 6 verification other 1 — medium, bad_spec: `DecodeBounded` fully decodes a nested URL before its second parse converts literal `+` in a valid member focus id to space, rejecting the return route.
- Review 6 edge 1 — medium, patch moot at loop cap: `IsSafeFocus` omits `@`, which valid user-id-derived focus ids contain, so the entry is disabled.
- Review 6 edge 2 — high, bad_spec: standalone authority Refresh can run while a caller authentication task is pending and resolve under the previous caller, reopening a link before the new caller is known.
- Review 6 edge 3 — high, bad_spec: detail audit Refresh can start a capability probe while caller authentication is pending, with the same premature-link consequence.
- Review 6 edge 4 — medium, bad_spec: JS returns without installing a terminal check when the expected origin root is absent, so a wrong workspace tab can leave the focus promise and notice pending indefinitely.
- Review 6 blind 1 — medium, bad_spec: return validation checks a cursor for credential shape before dropping it, so a protected JWT-shaped cursor disables the entry instead of yielding a cursor-free partial return.
- Review 6 blind 2 — medium, bad_spec: repeated decoding turns an intentional `%2F` search literal into `/`, changing the restored search state.
- Review 6 blind 3 — high, bad_spec: the credential matcher omits `access_token=` and `refresh_token=`, allowing those values in a return search query URL.
- Review 6 blind 4 — medium, bad_spec: the audit destination discards `SafeReturnUrl`'s partial flag, so an older/direct link with a cursor can reset paging without a notice on Back.
- Review 6 blind 5 — medium, bad_spec: detail sanitizes a nested list return URL but drops its partial flag, so a stripped list cursor can disappear without a notice after the detail/audit handoff.
- Review 6 blind 6 — medium, bad_spec: focus JS waits forever when the expected origin root never appears; neither heading focus nor the missing-origin notice occurs.
- Review 6 blind 7 — medium, bad_spec: create command state is only in its component instance, so audit navigation back to `/tenants` recreates an Idle flow and cannot restore the command launcher state.
- Review 6 blind 8 — medium, bad_spec: create lifecycle lives in an initially collapsed accordion; returning to it cannot focus the hidden launcher until that panel opens.
- Review 6 blind 9 — medium, patch moot at loop cap: the invalid audit-route branch awaits raw lease disposal; an unsubscribe failure can skip its state clear, while the existing support-safe disposal helper would contain it.
- Review 6 blind 10 — medium, bad_spec: the Chrome fixture covers standalone delayed focus but lacks row, member, detail, and command DOM shapes used by the changed selector.
- Review 6 blind 11 — maybe-false, defer moot at loop cap: hosted smoke assertions did not execute because EventStore `/alive` timed out during fixture setup; a healthy fixture run would settle whether the hosted route behavior passes.

- Review 5 verification gap 1 — medium, bad_spec: boundary tests exercise caller transitions without a rendered standalone link, so an available My Tenants link could stay visible until new caller resolution without a test failing.
- Review 5 verification gap 2 — medium, bad_spec: the detail caller test starts with a disabled probe; it does not prove a previously enabled link closes immediately on caller change.
- Review 5 edge 1 — medium, patch moot after loopback: `IsAuditFocusTerminal` can become true when capability resolves while detail is still Loading, causing premature heading fallback before the launcher renders.
- Review 5 blind 1 — medium, bad_spec: standalone My Tenants paging still uses a workspace canonical URL, dropping `/tenants/my` during an audit return flow.
- Review 5 blind 2 — medium, bad_spec: My Tenants detail links still carry a workspace return URL, so standalone → detail → audit cannot return to its origin.
- Review 5 blind 3 — medium, bad_spec: command `ReturnFocus` values name lifecycle test selectors without matching element ids; `getElementById` cannot find any of the command launchers.
- Review 5 blind 4 — medium, bad_spec: bounded decoding rejects a safe escaped percent sign, so an ordinary `search=100%25` makes a contextual audit link unavailable.
- Review 5 blind 5 — medium, bad_spec: broad `token`/`etag` substring rejection blocks ordinary search terms such as “tokenization” despite the safe-search requirement.
- Review 5 blind 6 — medium, bad_spec: `ToAuditUrl` substitutes `/tenants` on invalid return state and still yields an audit route and focus, silently losing origin state without a partial notice.
- Review 5 blind 7 — medium, patch moot after loopback: unsafe context displays the stale-scope reason and offers a projection refresh that cannot repair an unsafe URL; report the invalid context and suppress inapplicable recovery.
- Review 5 blind 8 — medium, bad_spec: the JS terminal lookup is document-wide; a marker in another retained workspace tab can end a valid origin wait.
- Review 5 blind 9 — medium, bad_spec: the fixed ten-second focus timeout can declare a still-loading origin missing before it reaches a terminal state.
- Review 5 blind 10 — low, rejected: a manually supplied `auditPartialReturn=true` can show a conservative partial-return notice without a stripped cursor, but the visible-only notice has negligible everyday impact and stricter provenance needs extra state/guards.

- Review 4 verification gap 1 — medium, bad_spec: standalone main Refresh buttons reload membership data but do not retry `AuditReadAuthorityBoundary`; after a transient reflection failure the audit link remains blocked despite the page refresh.
- Review 4 blind 1 — high, bad_spec: `IsSafeTenantId` accepts `.` and `..`, so browser path normalization can turn a generated tenant audit link into another route.
- Review 4 blind 2 — medium, bad_spec: `SafeReturnUrl` validates split nonempty segments then emits the original path, accepting repeated slashes such as `/tenants//users`.
- Review 4 blind 3 — medium, bad_spec: audit Back applies `ReturnFocus` even after rejecting its matching return URL; focus can be sent to an unrelated fallback route.
- Review 4 blind 4 — low, patch moot after loopback: the audit page renders a raw DOM focus id in its return-context copy; `tenants-member-user+alpha` is unhelpful user-facing text. Use localized source copy.
- Review 4 blind 5 — medium, bad_spec: invalid tenant navigation returns before disposing the previous audit subscription or clearing its retained paging state.
- Review 4 blind 6 — low, patch moot after loopback: `AuditReadAuthorityBoundary.Refresh` lacks a disposal guard and can start a late resolution after unmount.
- Review 4 blind 7 — medium, bad_spec: a faulted caller task leaves detail audit capability unresolved, making return focus wait the full timeout instead of falling back promptly.
- Review 4 blind 8 — medium, bad_spec: workspace caller-task failure similarly leaves `_auditAuthorityResolved` false indefinitely.
- Review 4 blind 9 — medium, bad_spec: standalone fallback calls `focusElementById` for a heading lacking `tabindex`, so the heading does not receive focus.
- Review 4 blind 10 — high, bad_spec: return-query protection decodes once, allowing twice encoded token or ETag text into an emitted URL.
- Review 4 blind 11 — medium, bad_spec: the browser fixture has Fluent row/member/detail cases but no delayed standalone authority/content focus case, leaving the standalone readiness path unverified.
- Review 4 edge 1 — medium, bad_spec: `RefreshTenantDetailAsync` refreshes the detail projection but does not restart the audit capability probe; a transient audit failure stays disabled.
- Review 4 edge 2 — medium, bad_spec: the unsafe tenant branch retains the previous read-refresh lease, as blind 5 observes.
- Review 4 edge 3 — high, bad_spec: `IsSafeIdentifier` accepts JWT-shaped dot-separated values as tenant/user ids, contrary to the no-token URL claim.

- Review 3 verification gap 1 — medium, bad_spec: standalone authority tests cover initial authorization but not a delayed previous-caller completion; `AuditReadAuthorityBoundary` has asynchronous caller resolution, so test the transition and stale result rejection.
- Review 3 verification gap 2 — medium, bad_spec: the browser fixture uses native anchors while `AuditEvidenceEntryPoint` renders `fluent-anchor-button`; the fixture can pass while custom-element focus fails.
- Review 3 verification other 1 — medium, bad_spec: `UserMembershipLookupPanel` canonicalizes its URL after return and omits `auditFocus` and `auditPartialReturn`, losing the handoff before the page can restore it.
- Review 3 blind 1 — medium, bad_spec: `IsSafeFocus` rejects `+`, although member/user ids can contain it and are embedded in row focus ids; the link is wrongly disabled.
- Review 3 blind 2 — medium, bad_spec: `focusAuditLauncher` returns false as soon as it sees the pending disabled control, before authorization can replace it with an enabled launcher.
- Review 3 blind 3 — medium, bad_spec: the detail state header exists during loading, so its terminal selector can end a member-row focus wait before members render.
- Review 3 blind 4 — medium, bad_spec: workspace audit refresh calls `LoadAsync` but does not retry failed global-administrator authority reflection, leaving a recoverable disabled link blocked.
- Review 3 blind 5 — medium, bad_spec: standalone panel refresh leaves `AuditReadAuthorityBoundary` authority unchanged after a transient reflection failure.
- Review 3 blind 6 — medium, bad_spec: `MemberReturnUrl` is a bare detail URL and drops the safe nested list return context.
- Review 3 blind 7 — medium, bad_spec: the detail command return URLs are bare detail URLs, losing the safe nested list context carried into the detail page.
- Review 3 blind 8 — medium, bad_spec: `TenantAuditPage` does not apply the new safe tenant-id guard before its direct BFF request, so a crafted deep link bypasses the contextual entry guard.
- Review 3 blind 9 — medium, bad_spec: `BackHref` appends `auditFocus` to a validated return URL that can already contain it, making a duplicate key rejected by the next handoff.
- Review 3 blind 10 — low, patch moot after loopback: workspace/detail partial-return flags accumulate on same-route changes and can show a stale restoration notice.
- Review 3 edge 1 — medium, bad_spec: the disabled control appears before authority resolves and makes `focusAuditLauncher` declare a missing focus prematurely, as blind 2 also observes.
- Review 3 edge 2 — high, bad_spec: detail audit probe checks the load generation but not the caller identity version at result application; a previous-caller result can race with caller invalidation and re-enable the link.

- Edge 1 — high, bad_spec: command entry `IsAvailable` defaults true while pending/delayed inspect actions render; no audit-read authorization is passed, so a usable link can appear on a disconnected or unauthorized audit read.
- Edge 2 — medium, bad_spec: `tenantsFocus.js` searches descendants of the identity anchor, but list and membership audit controls are sibling grid cells; return focus lands on identity.
- Edge 3 — medium, bad_spec: detail return focus runs only for Ready snapshots; unauthorized or unavailable detail states never show the promised heading fallback.
- Blind 1 — high, bad_spec: current tenant-list data plus a connected BFF does not prove global-administrator audit authorization; the audit handler requires that authority.
- Blind 2 — high, bad_spec: a My Tenants membership read can succeed for a non-admin, so its connected-BFF gate can expose an audit link without audit authorization.
- Blind 3 — high, bad_spec: user lookup readiness likewise does not establish global-administrator audit authorization.
- Blind 4 — high, bad_spec: all eight command handoffs omit an explicit audit-read gate; the default true link survives uncertain capability, as in Edge 1.
- Blind 5 — medium, bad_spec: tenant-list `returnFocus` names an identity cell, and the JS descendant search cannot reach the sibling audit column.
- Blind 6 — medium, bad_spec: My Tenants and user-lookup row anchors have the same sibling-cell focus mismatch.
- Blind 7 — medium, bad_spec: workspace consumes the focus attempt on its first render; a deferred user-tab grid can load later, after the fallback was declared.
- Blind 8 — medium, bad_spec: detail can become Ready before member rows load, so a valid member audit control can be called missing prematurely.
- Blind 9 — medium, bad_spec: the blocked entry's plain same-route link promises refresh without a guaranteed re-query in an interactive circuit.
- Blind 10 — medium, bad_spec: workspace search normalization accepts `/` and `@`, but the return validator rejects them and blocks an otherwise valid scoped audit entry.
- Verification 1 — high, bad_spec: the command-flow test gap is demonstrated by default `IsAvailable=true` and a pending inspect action; no existing command test asserts an unavailable audit read blocks the link.
- Verification 2 — medium, bad_spec: bUnit mocks the focus module, and the existing browser fixture has no audit row; removing the JS audit-control selection would leave the current checks green.
- Review 2 verification gap 1 — medium, patch: standalone My Tenants and user lookup page tests never assert an authorized audit link; direct grid tests bypass each page's new authorization handoff. Add page-level authorized-link assertions.
- Review 2 verification other 1 — medium, patch: the detail heading id is outside the metadata audit control's descendants, so the JS row/descendant search cannot restore focus to its available launcher. Search the enclosing page header.
- Review 2 edge 1 — medium, patch: `IsSafeFocus(null)` is true, so an ordinary workspace return clears its existing heading-focus request. Require a non-null audit focus before overriding it.
- Review 2 edge 2 — medium, patch: member rows supply `tenants-member-{userId}` as return focus but render no matching id. Add that id to the member identity anchor.
- Review 2 edge 3 — medium, patch: the detail header audit control is outside the heading's descendants, causing false missing-origin fallback. Search the enclosing page header, as verification other 1 reports.
- Review 2 edge 4 — false: the entry switches between `FluentButton` and `FluentAnchorButton` when availability changes, replacing the DOM child; an in-place disabled-attribute flip is not produced by this component.
- Review 2 edge 5 — medium, patch: an older authorization request failure can clear a newer success on the standalone My Tenants page because its catch lacks the version check. Apply the same version guard on failure.
- Review 2 edge 6 — medium, patch: the standalone lookup page has the same unguarded failure catch. Apply its version guard on failure.
- Review 2 edge 7 — medium, bad_spec: workspace search accepts token-like text and the new audit return URL copies it; the URL safety validator has no protected-value rule. Define and apply a narrow protected-value rejection without rejecting ordinary search syntax.
- Review 2 edge 8 — low, patch: `TenantAuditPage.IsSafeReturnQueryPart` is left unused after delegation to the shared validator, leaving a second stale safety rule for future callers. Delete the dead method.
- Review 2 edge 9 — medium, bad_spec: the new focus wait uses a ten-second timeout even when the origin has reached an unauthorized or unavailable terminal state. Amend the focus contract to end waiting on a terminal state.
- Review 2 blind 1 — medium, patch: the same null-focus guard from edge 1 cancels ordinary workspace heading restoration. Require a non-null audit focus.
- Review 2 blind 2 — high, bad_spec: detail authentication change restarts lifecycle authorization but leaves `_auditProofCapabilityAvailable` true, so a previously authorized audit link survives caller revocation. Invalidate and re-resolve audit capability on identity change.
- Review 2 blind 3 — high, bad_spec: standalone page authentication callbacks clear audit authority outside renderer dispatch and render only after the incoming task completes, leaving enabled links visible during a caller transition. Clear and render immediately on the renderer, then resolve the new caller.
- Review 2 blind 4 — maybe-false, defer: connected BFF plus authorized administrator does not prove a successful future audit query, but no separate capability reflection exists for list/self/lookup and the audit destination already reports gateway failure. A demonstrated connected and authorized yet unsupported endpoint would settle whether this is a link-availability defect.
- Review 2 blind 5 — medium, patch: detail and command audit gates omit `_snapshot.Lifecycle`, so a current-freshness detail with stale lifecycle can show a link. Include current lifecycle in the gate.
- Review 2 blind 6 — medium, patch: member gate omits detail and member lifecycle states while list grids require current lifecycle. Include both lifecycle states.
- Review 2 blind 7 — medium, bad_spec: standalone `/tenants/my` and `/tenants/users` links return to `/tenants` because their panel return builders always use workspace paths; those origin pages and controls are not restored. Preserve the actual standalone route and focus there.
- Review 2 blind 8 — medium, bad_spec: detail audit return URL drops the detail page's incoming list `returnUrl`, so list → detail → audit → detail loses the prior list state. Preserve safe nested list return context.
- Review 2 blind 9 — medium, bad_spec: forced full-page refresh on a disabled command entry discards its in-memory lifecycle and availability state. Use a parent re-query callback that preserves command state, with full reload only where safe.
- Review 2 blind 10 — false: the disabled-to-enabled entry switches rendered component types, so the claimed in-place attribute mutation does not occur in this application.
- Review 2 blind 11 — false: workspace and every detail state render an `FcPageHeader` heading; the claimed missing-heading case is not reachable on these return routes.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -m:1 --no-restore` -- expected: no errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.AuditEvidenceEntryPointTests -parallelMode none` -- expected: all tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all tests pass.
