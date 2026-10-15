---
title: 'DW-166/DW-173 finite authorization and subscription bounds'
type: 'bugfix'
created: '2026-10-15'
status: 'in-progress'
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
