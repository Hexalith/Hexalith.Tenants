---
title: 'DW-164/DW-183 tenant-route refresh-subscription handoff'
type: 'bugfix'
created: '2026-10-15'
status: 'done'
route: 'oneshot'
baseline_commit: '51f404513f2657e98f949f58f0df43b99b68a7d0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="explicit implementation intent and existing owning contracts">

## Intent

Complete obsolete tenant-detail notification setup by handing setup ownership to the
current authorized route without requiring an unrelated render. DW-183 explicitly
duplicates DW-164; both current records and Story 1.11 reviews describe the same
in-flight flag race. Preserve prior work, dependency pointers, all unrelated ledger
records, release blockers and existing policy. Close only these two records after
independent review and executable proof.

</frozen-after-approval>

## Implementation Notes

Pre-implementation contract, recorded before production edits:

- Story 1.10 (`spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`,
  matrix/tasks at69–70, retry review at1186/1458/1579) owns exact `tenants` projection
  and literal tenant scope, reference-counted leases, coalesced authoritative-read
  nudges, late-setup disposal and captured route/generation rejection. Story 1.11
  (`spec-1-11-authorized-global-administrator-review.md`,80/427) records the original
  and duplicate;412–416 require atomic setup, Empty cleanup and fixed-code logs.
- Current `EnsureReadRefreshLeaseAsync` rejects incoming setup while the old one
  holds `_readRefreshSubscriptionInFlight`; old completion disposes then only
  clears the flag. `OnAfterRenderAsync` supplies no completion wakeup. Prove this
  with held actual Fluent rendering before changing production code.
- Own setup, adoption and completion handoff on the renderer, retain the atomic
  in-flight guard, and re-evaluate the latest route after an obsolete completion.
  Bind setup and callbacks to route epoch plus caller version; A→B→A must not
  resurrect old callbacks. Require a resolved current caller; pending, signed-out,
  invalid-route and disposed scopes cannot establish or invoke a subscription.
  Authentication invalidation detaches the page's lease; other consumers retain
  their independent reference-counted leases. Route/caller invalidation precedes
  asynchronous reads/cleanup. Late success, Empty and throw all release ownership.
- Existing three-failed-attempt budget remains per route, reopened by route or
  explicit refresh, never by every nudge/render. Success does not charge it;
  obsolete failure cannot charge the incoming route. Ordinary current failures
  still wait for later renders/recovery, with no eager retry loop. No timeout,
  cancellation-token, retry-count or policy change: setup keeps
  `CancellationToken.None`; DW-166/DW-173 own finite-bound work separately.
- Notifications establish no freshness or command confirmation. Preserve independent
  detail/member validators, actual refresh state, obsolete read rejection and
  DW-150 combined-refresh coordination, cursor/history, commands/lifecycle/focus.
  Cleanup and diagnostics remain support-safe; disposal starts no further work.

Small footprint: `TenantDetailPage.razor` subscription ownership/handoff and scope
invalidation; meaningful actual Fluent regressions in `TenantDetailSurfaceTests.cs`;
new spec/evidence and only the two ledger dispositions/grouped index row. No public
API, persistence, configuration, gates, dependencies or production operations.
No unresolved intent choice or irreversible action. Explicit preserve-dirty-work and
no-commit user instructions override the workflow's dirty-tree halt/commit step;
this is a maintenance bundle, not a sprint-story status transition.

Verification: held old success/failure/Empty; repeated and returning routes; current
failure budget/recovery; caller/pending/signed-out changes; established shared lease;
invalid routes; disposal before/after completion. Assert installed/callback ownership,
no duplicate/leaked leases, obsolete callback rejection, exact requests/validators,
retained Refreshing state and final evidence. Run focused and full per-project UI in
Debug/maintained Release, unchanged Chromium checks, broad/package diagnostics and
historical provenance checks. Record exact commands, all initial failures, zero/current
skips and remaining blockers; never initialize nested dependencies or alter bounds.

Implementation and first verification:

- Production stays in the page: renderer-owned setup continuations, route epoch,
  caller-version checks on setup/adoption/copied callbacks, caller lease detachment,
  and completion-triggered latest-scope handoff after clearing the atomic flag.
  Current failures still wait for later renders; obsolete failures charge no current
  budget. DW-150 and all non-subscription code remain inherited.
- The first9 cases held initial lifecycle setup; six passed because completion of
  OnParametersSet can render automatically, while three exposed returning-route
  budget behavior. The next9-case fixture spent its budget during initial failed
  setup and failed before holding. A two-error fixture build used a wrong component
  name. The corrected real OnAfterRender fixture uses the existing explicit member
  refresh recovery;14 baseline cases then fail12/14: missed completion wakeups,
  returning-route callbacks and caller transitions. Invalid-route/disposal already
  pass. Every output/XML is retained. No test bound or policy was changed.
- First fixed16-case run passed13; three return-route release assertions included
  initial failure cleanup. Clearing only those fixture calls fixes the assertions;
  final16/16, focused348/348 and initial full Debug5020/5020 pass, zero skips.
  Added actual rendered member/configuration changes and independent request ETags.

## Review Triage Log

Only Blind Hunter is configured by this small-change workflow; no configured layer
was skipped. Independent review used the exact bundle delta against captured dirty
entry, rather than attributing inherited changes to this work. Floor6 findings:

1. **medium / patch — cleanup false positive.** The caller test's generic Received
   matched initial failure cleanup. Clear fixture calls at held entry; require exact
   old/current releases and an empty callback registry after terminal/disposal paths.
2. **medium / patch — same-route generation gap.** Add held setup across actual
   member paging, combined refresh and metadata detail refresh; assert stable route
   epoch, adopted lease, retained cursor and working authoritative callback.
3. **medium / patch — successive owners gap.** Hold alpha then beta setup, switch to
   gamma between completions, and require both completion-driven handoffs without
   another render, with exactly one release for each resulting owner.
4. **medium / patch — caller failure outcomes gap.** Exercise success, Empty and
   thrown completions for changed/pending/returned callers, sign-out, invalid route
   and disposal. Require no obsolete failure charge against the current budget.
5. **medium / patch — suspended invalidation cleanup gap.** Hold backend unsubscribe
   during caller change. Verify immediate old-row removal, current reads while held,
   inert copied callback, later replacement ownership and no late detach/leak.
6. **medium / patch — copied caller callback gap.** Capture the old callback, complete
   replacement including returning to the original caller, then invoke it directly.
   Require no reads from that callback and real reads from the current registration.

These are evidence improvements within the requested contract; none requires a
production adjustment, policy decision or additional deferred record. Final recheck
and completed qualification results follow after execution.

Independent correction recheck initially passed31/32; an unchanged focused repeat
passed18/18. A concurrent focused364-case run also failed the same all-gateway-count
pattern in suspended cleanup. Supplementary audit/global reads could start between
counting and dispatching a copied callback, so neither assertion isolated the
callback. Corrected tests compare actual detail/member reads plus unchanged snapshot
references inside one renderer operation. Both initial failures and the passing
repeat remain recorded; no timeout or assertion requirement was relaxed.

Final independent recheck resolves all six findings and the synchronization issue:
32 passed/zero failed/errors/skipped/not-run, exit0. Final Debug/maintained Release
focused364 and full per-project UI5036 each pass, zero failures/skips; both unchanged
Chromium lanes pass. Only DW-164/DW-183 close; recomputed counts/index/readiness and
preservation/provenance evidence remain explicit. No commit or sprint update ran.
