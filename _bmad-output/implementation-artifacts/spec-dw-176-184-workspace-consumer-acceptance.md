---
title: 'DW-176/DW-184 workspace consumer acceptance'
type: 'bugfix'
created: '2026-10-15'
status: 'done'
route: 'oneshot'
baseline_commit: 'a3f43655d68370389439c7d2c212ab7939bd164e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="explicit scoped implementation request and settled ledger decisions">

## Intent

Prove workspace authorization and authentication-event consumer acceptance against
the existing shared TenantUiOperationPolicy. Close only DW-176/DW-184 after evidence
and independent review; implement only demonstrated consumer gaps. Pending, failed
or expired authentication/authorization leaves privileged entry and create unavailable,
with Indeterminate authority and explicit recovery. Superseded work cannot restore
authority, collapse a newer caller, dispatch a command or start work after disposal.

</frozen-after-approval>

## Implementation Notes

Owning acceptance contract, recorded before production changes:

- DW-176's 2026-08-25/26 decisions require replaceable cancellation plus the shared
  configured authorization bound. DW-184 requires the same bound/cancellation for
  authentication events, with entry hidden on expiry. Story1.11 loop1/2/3/6 and its
  scope-attribution decisions require circuit authority over stale HTTP evidence,
  fail-closed pending/faulty evidence, optional resolution independent of primary
  list loading, strict BFF corroboration rather than raw event claims, and guarded
  renderer application. No identity-policy decision is reopened.
- DW-173 already supplies separate entry/event/create sources, shared configured
  finite bounds, cancellation on caller replacement/disposal, generation rejection
  and late-fault observation. Reuse that policy and configured TimeProvider; change
  no timeout, dependency, provider/coordinator ownership or public contract.
- Given held initial authentication, entry or event resolution, when the ordinary
  list loads, time expires or the caller changes, then list access remains independent,
  current pending state settles without privilege and obsolete success/failure is inert.
- Given pending/expired authentication, when actual refresh/recovery is requested,
  then unresolved authentication cannot be bypassed by create or entry resolution;
  recovery rereads current provider evidence and restores only the current caller.
- Given a held undispatched create authorization, when active view/scope/route changes,
  caller changes or disposal occurs before expiry, then cancel and invalidate that
  admission, release its gate and dispatch nothing even after late success/failure.
  A return and explicit retry can submit exactly the intended request once. Existing
  dispatched command lifecycle/tracking and retained input remain owned by their flow.
- Given authority for caller A, when the accessor identifies B before the next provider
  event, then any render keeps privileged entry, audit availability and create closed
  until authority binds B. A denied/expired old result cannot collapse B's authority.
- Proven source gaps: retained Users-tab create admission captures no workspace scope
  and is not cancelled on view changes; raw entry/audit booleans omit the current-caller
  binding already present on CreateAuthorization. Limit production edits to
  `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`: active create-view
  binding, view-generation cancellation and current-caller entry/audit binding. Add
  preflight checks so unresolved authentication cannot invoke create's resolver.
- Add actual Fluent regressions in
  `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.ConsumerAcceptance.cs`, reusing
  existing finite-clock/services helpers. Cover held initialization, explicit expiry
  recovery, active replacement/disposal before expiry, late success/fault, view/route
  round trips, gate release, exact reads/commands and caller binding. Existing finite
  cases remain acceptance evidence; do not duplicate them or increase test waits.
- Small bounded footprint: one component, one test partial, scoped spec/evidence and
  canonical ledger/index/readiness updates. No unresolved intent gap or irreversible
  action. User preserve-dirty-work/no-commit instructions supersede workflow dirty-tree
  halt and commit steps. This maintenance bundle does not transition sprint stories.
- Run independent Blind Hunter review, focused/full per-project Debug and maintained
  Release UI plus unchanged browser checks. Capture exact commands/results, initial
  failures/skips and source hashes. Record broad/package and historical provenance
  blockers separately; do not reset or falsely attribute external Git/dependency work.
- Preserve B1–B8, DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183 and DW-166/DW-173,
  including notification ownership, combined refresh, cancellation separation, command
  lifecycle, cursor/history and focus. Preserve all unrelated records, historical
  readiness text, archive, sprint tracking, unresolved decisions, gates/exclusions/
  coverage floors. No dependency/version/gitlink change, nested initialization, staging,
  commit/push/deploy, production replay or migration. Closure is not release readiness.

Implementation/validation notes (append-only):

- Before production edits, added17 actual Fluent consumer cases and a separate
  stateful authentication-provider fixture, reusing the existing1s virtual policy
  bound, default bUnit wait and5s outer task guards. First Debug build exposed two
  nullable diagnostics on the captured recovery delegate (CS8600/CS8604); an explicit
  ShouldNotBeNull assertion corrects the fixture and strengthens the contract.
- First17-case red run failed11: nine demonstrated consumer failures (four expired
  authentication create preflight, two caller-binding, two retained Users-tab admission,
  one post-disposal recovery) plus two mine-scope fixtures expecting a populated list
  from an empty result. The latter now verifies the actual empty surface, exact route,
  normalized scope and exact membership request. First green passed15/17; mine scope
  correctly unmounted create, so its assertions now retain the pre-transition component
  instance instead of dereferencing bUnit's removed rendered handle.
- Production changes remain in the workspace: caller-bound entry/audit denial/availability,
  active create-view gating, changed-workspace generation plus create-only cancellation,
  unresolved-authentication create preflight, linked-token recheck and post-disposal
  authority callback/source guards. Entry/event/list/dispatched-command lifetimes remain
  independent; shared policy/coordinator and existing timeout values are unchanged.

## Review Triage Log

Independent context-free Blind Hunter reviewed39156 changed bytes/1024=38.24KiB;
finding floor min(floor(sqrt(38.24)+1),10)=7. Other layers are not configured.

| Finding | Verdict and disposition | Evidence/fix |
| --- | --- | --- |
| Timer disappearance/renderer no-op is not causal waiter retirement | medium, patch | Capture consumer CTS before invalidation and await its disposal; create also awaits Submit. Retain late success/fault and exact no-work assertions. |
| View test changes URL without exercising route lifecycle | medium, patch | Add actual route-driven parameter/component lifetime coverage with held create across a route round trip. |
| Final pre-dispatch authorization after lease allocation is untested | medium, patch | Hold the second resolver call, invalidate view and prove allocated admission release, no dispatch and exact retry. |
| Already dispatched command lifetime across view change is untested | medium, patch | Hold gateway response across retained Users and mine unmount; verify token, identity, delivery/tracking/admission continuity. |
| Old view completion precedes newer admission in test | medium, patch | Establish replacement dispatch first, then release obsolete success/fault; assert newer identity/authority survives. |
| Explicit recovery while authentication remains pending cannot reacquire provider | medium, patch | Source pending branch only returned; explicit recovery now replaces the authentication wait with current provider evidence, still bounded/fail-closed. Add before-expiry initial/event recovery cases. |
| Same-caller entry replacement uses a different path from auth notifications | medium, patch | Hold entry, invoke actual audit-authority recovery delegate before expiry, assert cancellation, exact replacement resolution and no list reload. |

These findings refine the recorded consumer contract; no identity-policy decision,
shared-provider behavior, dispatched-command policy or unrelated record is changed.

Final review refinements (append-only):

- The two test partials now add34 cases (24 consumer acceptance and10 actual
  Router/RouteView lifecycle cases), plus the separate stateful provider fixture.
  Capture each page CTS before invalidation and await disposal before raw late
  completion; await actual Submit for create. Pending-before-expiry recovery replaces
  the provider wait; a synchronous provider acquisition failure cancels all prior
  authority sources rather than leaving them until expiry.
- Router cases exercise real path/query parameter updates on the same workspace,
  first and final admission, allocated message/undispatched lease release, Users
  retention and mine unmount. Held dispatched responses preserve None delivery token,
  exact intent/message/correlation/baseline/timestamp, marked lease, tracker/remount
  ownership, unrelated aggregate access and status recovery with one delivery.
- Review rechecked all seven fixes and found no unresolved concrete issue. It was
  read-only and did not claim test execution. The lifecycle partial's first build
  found CS4014 on an undiscarded NSubstitute verification Task and CS0103 from a
  missing freshness namespace; explicit discard/import correct the fixture without
  changing assertions. Full raw command failures remain in dw-176-command-results.json.
- Workspace-wide git diff --check exits2 solely on the inherited archive's blank
  EOF line; its exact entry bytes are preserved. Changed production source and all
  three new C# files pass focused whitespace checks. No whitespace gate is weakened.

- Executed focused849 exposed a further proven consumer gap (Debug6 failures,
  Release5): all five Users Router cases retain parent Tenants state. Same-component
  literal aliases can skip parameter delivery; FrontComposer tabs independently
  follow LocationChanged without notifying the parent. Before the corrective edit,
  the owning contract is extended explicitly: reconcile actual workspace tab
  navigation from the captured destination URI on the renderer, reject superseded
  destinations, ignore non-workspace routes, suppress duplicate normalized state/load
  work, cancel undispatched admission and unsubscribe on disposal. Reuse FromQuery;
  never canonicalize from stale supplied query fields. No FrontComposer edit/route
  change or retained-panel reset is permitted. Independent source recheck confirms it.
- Debug's sixth failure is in the unchanged prior detail handoff fixture: after
  rendering gamma it assumes only two backend attempts before releasing ignored beta
  setup. DW-173's prompt cancellation can already start gamma's third attempt. Replace
  that scheduling assumption with awaiting finalEntered, asserting exactly3, then
  releasing beta; preserve exact final scope/lease/read assertions and the same bound.

Final qualification: Debug/source and maintained Release builds pass; focused849
and full per-project MTP UI5134 pass in each configuration with zero failures/skips.
Both unchanged Chromium lanes pass. Final2150 input hashes are unchanged across the
rebuilds/checks, with12 compiled output hashes. Only DW-176/DW-184 close; ledger/index,
archive/sprint, inherited-file and historical-readiness preservation audits pass.
All release/provenance blockers remain in dw-176-validation-evidence.md. No commit
or dependency/pointer mutation was performed. Next selected bundle: DW-245/DW-265/DW-290.
