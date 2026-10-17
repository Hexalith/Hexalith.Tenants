---
title: 'Recover configuration management after retained removal expires'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: 'fa56dc1e7abd48e770a87925d3aa3b221e648cf7'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-325/DW-587 identify the same configuration-management activity latch: retained removal evidence and activity owned by the rendered removal flow share a flag. Expiry can leave the management surface treating an attempt as active when its flow never acquired activity, including refused adoption. The historical unmounted-child description has not been reproduced and must not be reported as executed evidence.

**Approach:** Track retained removal activity independently from activity reported by the flow. Recompute retained state on parameters and the existing scoped tracker's expiry notification, preserving flow-owned activity through terminal projection refresh, sibling set activity, current-tenant isolation and disposal. Qualify the reachable refusal/expiry case and existing confirmation behavior, then reconcile only these duplicate ledger entries and their maintenance row with exact evidence.

</frozen-after-approval>

## Implementation Notes

- Entry is clean `main`, canonical HEAD `fa56dc1e7abd48e770a87925d3aa3b221e648cf7`. Intent gaps: none; expiry/ownership and terminal visibility are already defined by the ledger and tests. Irreversibles: none. Footprint: one existing Razor component and its existing component test file, plus this spec, validation evidence and the two backlog records/index row; no public API or dependency change.
- Reuse `TenantRemoveConfigurationAttemptTracker.OwnershipExpired`, `Find` and its existing bounded retention. `TenantDetailPage` already handles aggregate-gate expiry; management must not release upstream activity or sibling activity from its expiry callback. `RemoveTenantConfigurationFlow.SetSnapshot` forgets the retained attempt before awaited parent projection refresh completes: preserve callback-owned activity during that interval. Keep `_removeCommandStarted` as the existing dismissal/target latch.
- Investigation delegated to an independent agent; its source findings confirm the conflation and confirmation ordering. Verify with a clock-controlled retained snapshot and refused child acquisition before editing runtime source, then run management/detail, configuration-flow/tracker cases and the full individual UI project in Debug with source references. Independent blind review follows implementation.
- Aspire baseline startup succeeds. Initial resource inspection shows security readiness failing TLS with unexpected EOF at `https://localhost:8543/health/ready`, leaving Tenants resources waiting; security logs were inspected. This is a runtime qualification limit, separate from component verification.

- Initial regression build passed; its first two executions failed before reaching the runtime assertions because `RegisterComponentServices` initializes bUnit services before tracker registration. Registration now precedes that helper. The corrected original-source execution fails both cases at the missing unavailable landmark (exit 2); this proves the retained-only latch. The first patched run passes terminal confirmation but fails both new text assertions because the stub localizer returns translated copy rather than a raw key. The assertion now compares the same exact localized value. No assertion about the missing landmark was weakened.
- Runtime implementation separates retained activity, subscribes once to the existing scoped tracker, dispatches expiry recomputation with current-tenant/disposal checks and rereads current retention for replacement safety. Flow release/dismissal also recomputes retention because terminal `Forget` cancels expiry notifications. No upstream gate or sibling activity is changed by notification handling.
- Story-gitlink validation initially refused the new spec because it lacked `baseline_commit`. Added the canonical entry revision to frontmatter; no dependency pointers are in scope. The focused test run exposed an additional disposal-fixture error: bUnit prohibits reading `cut.Instance` after disposal. Capture the instance before disposal so the subscriber-removal assertion remains exact.
- Independent blind review returned five findings. All are accepted as medium/patch within the same expiry goal: child-first cancellation of tracker notification, modal focus loss on notification unmount, stale replacement target, missing queued tenant-switch coverage and incomplete disposal-drain evidence. The small correction also touches the existing remove tracker and its existing tests; it changes no public signature. `Forget` now emits the existing expiry event only for matching expired ownership after removal, outside the lock, while ordinary release and wrong identities remain silent. Management reconciles replacement targets and schedules the existing heading fallback when expiry removes its modal. Added child-first, identity, focus and actual held-renderer replacement/tenant-switch coverage.
- A second disposal execution showed that synchronous `cut.Dispose()` returns before the renderer completes teardown. The fixture now awaits established `DisposeComponentsAsync`, then invokes a previously captured callback and drains the still-live test dispatcher, asserting no render/renderer exception and an actually removed subscription.

- Final full UI run passes 6137/6137, zero failures/skips, exit 0 in 1m58.609s, and all four reviewed source/test hashes match. Only DW-325/DW-587 close: 647 IDs, 348 done/299 open across 268 exact-covering index rows. All 645 unrelated ledger bodies, every unrelated index row, archive/sprint hashes and dependency pointers are preserved. Exact commands, review and limits are in `dw-325-587-validation-evidence.md`; machine-readable preservation and TRX counts are in `dw-325-587-ledger-validation.json`. The skill-requested local commit follows successful pinned commitlint validation; no push, branch or dependency update is performed.
- Correction to the earlier fixture note: one initial invocation had two setup failures, followed by one corrected original-source invocation reaching the two runtime assertions.

## Review Triage Log

- medium / patch: A child expiry may call `Forget` before the tracker timer, canceling its only notification; a refused adoption has no accepted activity callback. Publish the existing expiry notification for matching expired `Forget` removal and cover both timer orderings, ordinary release and wrong-identity controls.
- medium / patch: The new expiry-render path removes the modal while its launcher is unavailable. Schedule the existing surviving-heading focus fallback and assert its captured focus target.
- medium / patch: Expiry queued before a replacement snapshot could render the replacement with the previous target key. Reconcile target and started state from current retained evidence; hold the renderer to prove a different-key replacement survives.
- medium / patch: Other-tenant expiry coverage did not exercise an actual tenant switch before queued notification execution. Add that held-renderer ordering and verify the replacement identity, target and retention.
- medium / patch: A captured disposal callback started discarded async work, so invocation completion alone proved too little. Await teardown and drain the renderer, retaining the explicit unsubscription assertion and checking renderer health.
- Reviewed source build passes with zero warnings/errors in 15.17s; expanded focused run passes 470/470, zero failures/skips, exit 0 in 31.481s. All thirteen new cases pass, including both expiry producers, exact focus fallback, ownership preservation, identity controls, queued replacement/tenant switch and disposal. The independent recheck accepts all five corrections without a new concrete issue. Full UI execution is now running against the four saved source/test hashes.
