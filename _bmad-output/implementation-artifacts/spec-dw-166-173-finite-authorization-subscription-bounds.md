---
title: 'DW-166/DW-173 finite authorization and subscription bounds'
type: 'bugfix'
created: '2026-10-15'
status: 'done'
route: 'oneshot'
baseline_commit: '51f404513f2657e98f949f58f0df43b99b68a7d0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="explicit implementation request and settled ledger decisions">

## Intent

Authorization resolution and optional notification setup must terminate within one
shared configurable finite bound, including providers that ignore cancellation.
Propagate replaceable page/circuit cancellation, fail authority closed to
Indeterminate, and charge current setup timeout against the existing three failed
attempts. Release gates, preserve exact caller/route ownership, reference-counted
leases and completion-driven handoff. Close only DW-166/DW-173 after independent
review and executable evidence; preserve inherited changes and all release holds.

</frozen-after-approval>

## Implementation Notes

Owning contract recorded before production edits:

- DW-166 decisions (2026-08-25/26) settle timeout charging. DW-173 decisions settle
  replaceable cancellation and one configured finite bound with Indeterminate on
  expiry. Story 1.10 owns exact projection/tenant scope, live registration before
  backend setup, reference counts, coalescing, authoritative-read-only freshness,
  generation checks, late cleanup and three failed attempts. Story 1.11 loop2/3/6
  records the unbounded waits, atomic setup, Empty cleanup and safe fixed-code logs.
- Reverification confirms GA resolver calls omit cancellation; authorization Retry
  owns a load gate until resolve returns. Workspace resolver/auth-event waits are
  unbounded. The real coordinator holds its semaphore through setup and cleanup;
  bounding the page alone cannot free it or safely clean late registrations.
- No numeric timeout/configuration exists in the owning records. Add narrow shared
  `Tenants:Ui:AuthorizationAndSubscriptionTimeout`, default 00:00:10, positive finite
  TimeSpan within timer limits. Invalid configured values reject configuration;
  do not silently select infinity. This is an implementation detail of the settled
  policy. Inject/use TimeProvider (System fallback) for deterministic control;
  no dependency/version change. Do not alter enrichment/HTTP/command/test bounds.
- Shared policy bounds awaited provider tasks and sends cancellation. Observe late
  faults and retain token-source disposal ownership until the underlying operation
  finishes, so late token registration does not encounter a disposed source.
- Coordinator must detach failed/obsolete callbacks promptly and release shared
  gates. Because the provider API unsubscribes by scope rather than registration
  identity, quarantine a scope while old setup/compensating cleanup is outstanding;
  never install a replacement there that late cleanup could remove. Other scopes
  remain usable. Current unavailable/quarantined attempts return Empty and consume
  existing budget. Late success/failure is observed and cleaned without callbacks.
  Last-reference cleanup is bounded; unfinished cleanup retains scoped ownership.
- GA owns replaceable authorization and subscription cancellation independently of
  ordinary reads. Capture authorization transition in setup/adoption/callbacks and
  hand off obsolete completion only after releasing its gate. Workspace wires the
  same bound into entry/create resolution and authentication-event settling.
  DW-176/DW-184 remain open related consumer obligations, with partial wiring noted.
- TenantDetail gets scope-owned cancellation on route/caller invalidation/disposal;
  never use its load CTS for subscriptions. Preserve DW-164/DW-183 route/caller
  epoch and completion handoff, DW-150 combined-refresh coordination, exact requests,
  ETags, refresh state, cursor/history, focus, command lifecycle and safe diagnostics.
- Small maintenance footprint: shared operation policy/configuration, real
  TenantReadRefreshSubscription, three consuming pages, meaningful rendered/coordinator
  regressions and bundle evidence/ledger/index/readiness. No persistence, irreversible
  action or unresolved intent gap. Explicit preserve-dirty-work/no-commit instructions
  override workflow dirty-tree halt/commit; no sprint-story transition applies.

Acceptance through held asynchronous operations in actual Fluent rendering and the
real coordinator: deadline/cancellation releases authorization and setup gates;
late success/throw cannot restore authority, register obsolete callbacks, unsubscribe
replacement ownership or start post-disposal work. Three failures exhaust setup;
explicit recovery restores it; success does not charge failures. Route/caller changes
and established shared leases preserve exact ownership and actual requests. Exercise
cooperative and ignoring providers, stalled cleanup, truthful refresh evidence and
late disposal. Deterministic timers avoid increasing any existing test bound.

Run focused and full per-project UI Debug/maintained Release, maintained Chromium
checks, broad/package diagnostics, provenance and preservation audits. Record exact
commands/results, initial failures and existing skips. Preserve B1–B8, DW-517,
DW-105/DW-108, DW-112, DW-150, DW-164/DW-183, external commits/movements, archive,
sprint, gates, floors, exclusions and unresolved decisions. No nested initialization,
version/gitlink update, stage/commit/push/deploy/replay/migration. Closure is local
bundle evidence, never full release readiness.

Implementation/review notes (append-only):

- Optional policy is registered through production composition with container
  configuration preference and System clock fallback. Per-scope coordinator
  ownership replaces the global semaphore; same-scope joiners share one deadline
  across waiting/setup and retain independent reference-counted leases. Unfinished
  setup/cleanup remains quarantined until actual provider/cancellation completion.
- Page setup cancellation is independent of loads. Renderer-owned GA setup captures
  caller epoch, rejects obsolete adoption/callbacks, and hands off after gate release.
  Both pages disable callbacks owned by failed/timed-out setup even when caller/route
  remain current. Detail retains the original route/caller handoff and combined reads.
- Independent review caught same-caller overlapping refresh cancellation: authority
  resolves now share caller lifetime with separate local linked sources, so sibling
  read/notification checks do not cancel each other. Non-authorized collapse checks
  captured caller version inside the renderer, just like authorized application.
- Provider cancellation callbacks can stall, so the bounded waiter owns an independent
  clock/caller token. CancelAsync signals provider cancellation without running its
  callback inline; token disposal and coordinator retirement await actual provider
  and callback completion. No provider invocation is moved to a background thread.
- Initial focused663 failed10: six historical handoff cases assumed a held obsolete
  setup blocked intermediate routes; assertions now require exact intermediate/final
  registrations under prompt cancellation. One cooperative fixture observed the next
  automatic attempt rather than exhausted state. Two copied callbacks from timed-out
  setup remained active; production now invalidates failed-attempt callbacks. One
  Empty path lost its page-safe diagnostic; the fixed code is restored there.
- New rendered partials initially had three compile errors: an absent lifecycle enum
  member and two missing freshness imports. Corrected to the actual Idle state and
  owning namespace. The next focused704 failed3 because old assertions treated page
  gate release as completion of a cancellation-ignoring provider's cleanup. The fixture
  now awaits actual cleanup signals using the unchanged default wait, preserving exact
  unsubscribe assertions. No existing timeout or product gate was increased.

## Review Triage Log

Independent Blind Hunter review inspected entry-relative content (current HEAD was
moving externally);105147 bytes/1024=102.68KiB, finding floor min(floor(sqrt(102.68)+1),10)=10.
No other review layer is configured for this route. Every finding is retained:

| Finding | Verdict and disposition | Evidence/fix |
| --- | --- | --- |
| Same-caller GA refresh/notification checks cancel one another | high, patch | Concrete page-refresh plus established notifier path bypasses page gate. Share caller cancellation; held overlap regression checks both tokens, exact reads and lease identity. |
| Stale failed check can collapse newer caller between check and dispatch | high, patch | Use existing CollapseAuthorizationIfCurrentAsync with captured version predicate inside renderer for reauthorization and authentication results; late caller/recovery tests retain current authority. |
| Provider cancellation callbacks can strand token-based wait | high, patch | Independent TimeProvider deadline/caller waiter plus CancelAsync propagation; real policy/coordinator blocking-callback regressions and lifetime retirement. |
| GA authentication-event bound lacks rendered proof | medium, patch | Hold actual event task through expiry/recovery and active disposal; late success/fault remain inert. |
| GA held setup caller-transition ownership lacks proof | medium, patch | Actual Fluent caller invalidation with held real coordinator, no replacement-budget charge, completion handoff and independent lease. |
| Workspace active replacement/disposal cancellation unproven | medium, patch | Hold entry/event/create before deadline; assert token cancellation and inert obsolete completion with actual rendered actions. |
| Same-scope joining waiter cancellation unproven | medium, patch | Cancel only joiner; original provider token/callback stay live, one backend registration and independent final cleanup. |
| Circuit disposal with stalled cleanup unproven | medium, patch | Hold multiple established-scope cleanup operations; one deadline releases disposal, callbacks detached, no duplicate cleanup/replacement. |
| Late cleanup faults unproven | medium, patch | Success/fault variants retain quarantine until actual completion, contain logger teardown and preserve replacement notifications. |
| Production configuration/clock sharing unproven | medium, patch | Production module registration with nondefault bound covers auth/setup/cleanup and invalid settings; actual detail coordinator uses250ms configuration. |

Final verification refinements:

- The expanded focused724 run initially passed723; the cooperative detail fixture
  waited through an automatic replacement attempt. Its next run passed722 because
  ignoring-provider variants could also enter replacement before idle observation.
  The fixture now identifies each setup source and advances exactly one configured
  250ms period per owner, allowing renderer-driven retries while asserting exact
  three-failure exhaustion, one/three advances, total virtual elapsed time and no
  backend call after exhaustion. The default real wait is unchanged.
- A subsequent724 run passed723: old throw/return handoff fixture signalled backend
  entry before nested policy continuation adopted the page lease. It now awaits
  the existing page-gate completion helper before exact lease assertions; no render
  is manufactured to wake handoff. The independent reviewer approved this ordering
  and required the one-advance-per-owner assertion above. All failed XML identities
  and command outputs remain retained alongside interim successful runs.
- Final independent source recheck reports no unresolved concrete finding; it
  inspected all ten fixes and added cases without claiming to execute tests.

- First full Debug5100 passed5099: the composition graph test synchronously disposed
  a scope containing the now asynchronously disposable coordinator. The test uses
  await-using AsyncServiceScope/provider; every dependency-cycle assertion remains.
  Final focused coverage includes all91 composition cases (815 total).
- Debug/maintained Release subsequently pass focused815, full5100 and Chromium.
  An external EventStore movement changed Client sources after a Debug build; a
  current-source rebuild/recheck therefore ran instead of resetting that movement.
  Its first815-case run passed814: a new GA transition fixture used a DOM/render
  assertion to observe setup-gate completion, which does not itself render. It now
  polls that gate using the same BunitContext.DefaultWaitTimeout, keeping exact
  failure-budget/ownership/request assertions and increasing no timeout. Final
  qualification captures2136 source/configuration hashes across the rebuild and
  both configurations' checks, recording all interim results separately.

Final qualification: Debug/source and maintained Release builds pass; focused815/815
and full per-project MTP UI5100/5100 pass in each configuration, zero failures/skips.
Both unchanged Chromium lanes pass;2136 source/configuration hashes remain stable
across the final rebuild and checks. Only DW-166/DW-173 close; canonical counts are
327 done/316 open,282 index rows,123 reconciled done/228 actionable. DW-176/DW-184
remain open and are the next separate acceptance reconciliation bundle. Complete
commands, initial failures, movements, preservation and release holds are recorded
in [validation evidence](dw-166-validation-evidence.md). No Git/dependency/production
mutation was performed by this run; external commits/movements remain visible.
