---
title: 'DW-150: preserve combined member refresh during detail refresh'
type: 'bugfix'
created: '2026-10-15'
status: 'done'
route: 'oneshot'
baseline_commit: '51f404513f2657e98f949f58f0df43b99b68a7d0'
review_loop_iteration: 0
---

<frozen-after-approval reason="user-requested DW-150 contract; existing specifications settle implementation choices">

## Intent

Fix only DW-150. A detail refresh currently cancels a held combined detail/member
refresh and silently drops its required member read. Extend the existing reroute
so a superseding detail refresh reissues the combined read whenever member work
is refreshing. Keep the production change confined to TenantDetailPage's guard
and explanation; reuse its existing cancellation, generation and failure paths.

Preserve inherited B1–B8, DW-517, DW-105/DW-108, DW-112, external commits and
dependency movements. Do not change pointers, versions, gates, exclusions, floors,
timeouts, archive, sprint tracking or decisions. No staging, commit, push,
deployment, production replay or migration. Close only DW-150 after independent
review and focused/full UI plus applicable browser qualification. Preserve initial
failures and release blockers; select the next bundle from the current ledger.

</frozen-after-approval>

## Implementation Notes

Pre-implementation investigation (2026-10-15): no intent gap or irreversible
operation. Small footprint: one Razor guard/comment, held-read regressions in the
existing detail test class, and bounded DW-150 evidence/index updates. The user's
explicit dirty-tree preservation and no-commit instructions override the workflow
dirty-tree halt and commit step. This is deferred remediation, not a sprint story;
sprint status remains untouched. Allowed bmad-build renderer ran exactly once.

Owning contract, recorded before implementation:

- Story 1.7 establishes the member surface; dedicated member paging/refresh is
  owned by `spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`, intent,
  matrix, tasks and acceptance (lines 29–80). Its review patch at 1194 and rationale
  at 1568 prescribe rerouting a detail refresh that would discard member work.
  Story 1.6's review at `1-6-read-only-tenant-configuration.md:545` and DW-150
  identify the uncovered combined-read entry point.
- Held navigation requests an uncommitted next/previous cursor. Supersession
  intentionally refreshes the displayed, last committed `_memberCursor`, rather
  than completing that navigation. Held combined refresh already targets this
  committed cursor. Both require a replacement member read.
- Retain matching caller/tenant rows, independent detail/member ETags and cursor
  history while visibly refreshing. Only explicit usable page-one cursor recovery
  resets history. Ordinary success and operational failure retain current paging.
- Shared cancellation and monotonic generation supersede older reads. Exact
  tenant/caller binding rejects obsolete completions even when a gateway ignores
  cancellation. Tenant/caller changes invalidate evidence and reset paging; they
  must not resurrect old refresh obligations under another identity.
- Failure is independent per read: operational member failure retains rows as
  degraded; terminal authorization/absence replaces rows safely. A successful
  sibling read can apply. Nudges remain requests for evidence, never command
  confirmation or proof of freshness. Preserve lifecycle refresh observations,
  fail-closed command behavior and focus; add no navigation or focus operation.

Code map: `TenantDetailPage.razor` RefreshTenantDetailAsync tests only
`_memberPageLoadInFlight`; RefreshTenantReadsAsync marks the member snapshot
refreshing; BeginLoad cancels the shared source and clears that marker. Existing
`A_refresh_during_a_member_page_load_reissues_the_member_read` holds navigation,
not combined refresh. Existing stale detail, caller transition, invalid-cursor,
failure, subscription/coalescing and command tests remain required coverage.

Regression acceptance: given an actual Fluent-rendered page with held combined
reads, when detail refresh supersedes it (once or repeatedly), then requests retain
the committed cursor, tenant, caller and independent validators; rows remain
refreshing until the owning read finishes. Given failed/denied replacement reads,
then truthful independent detail/member states apply. Given cancellation, tenant
or caller changes and late obsolete completions, then current scope and results
survive without obsolete rows or cursor history. Verify actual rendered content
and snapshot/request scope as well as cancellation; use deterministic held tasks.

Validation plan: Debug source-reference and maintained Release UI builds;
direct xUnit focused tests and unfiltered per-project MTP UI in both; maintained
Chromium runner unchanged; broad Release/package-only diagnostics and historical
gitlink guards recorded separately. Entry capture records ten repository states
and hashes every owning file, with exact preserved entry bytes outside the repo.
No nested dependency initialization is permitted to resolve blocked validation.

Independent review round 1: five verified regression-evidence gaps, all patched.
The production guard/comment is unchanged. Added a synchronous completed-detail
case to establish the member-await state without treating an empty dispatcher
callback as a continuation barrier; token-registered cooperative cancellation
now finishes obsolete reads while replacement remains held; pending identity is
held until obsolete reads finish and rows/history/read absence are asserted;
response generations have distinct independent validators and versions followed
by another refresh; held invalid-cursor recovery now proves only owning recovery
can reset history. Exact initial and final results remain in command evidence.


## Review Triage Log

All active layers: independent context-free Blind Hunter. No additional review
layers are configured for the rendered small-change route.

1. **medium / patch — continuation barrier:** an empty dispatcher callback did
   not prove a task continuation ran. Removed that claim and callback; added an
   already-completed detail Task case, whose synchronous await necessarily reaches
   the held member read before yielding. Obsolete operation is fully awaited.
2. **medium / patch — cooperative cancellation timing:** manual late cancellation
   did not test replacement setup. Token callbacks now cancel both original Tasks;
   their operation is awaited while replacement remains held, with refreshing and
   lifecycle observation still required.
3. **medium / patch — pending caller interval:** obsolete reads now finish before
   identity resolves. Rows remain withheld, cursor/history empty, and no new
   gateway read starts until the pending caller becomes authenticated.
4. **medium / patch — auxiliary evidence:** every response marker carries distinct
   detail/member validators and projection versions. Final values and a followup
   authoritative request must use the replacement evidence independently.
5. **medium / patch — cursor recovery ownership:** held old recovery is superseded;
   its late page-one response must preserve current history. A separately held
   owning recovery alone may clear history and show the page-recovered notice.

The first correction run passed16/17. Its recovery request wait depended on a
render even though the request starts without rendering; explicit gateway-entry
signals now establish both recovery interleavings within the existing bUnit
DefaultWaitTimeout. The two earlier14-case detail-fault assertion failures were
fixture assumptions about safe configuration availability, corrected to the
existing degraded/unknown/unavailable contract. No gate or timeout was changed.

Final independent recheck: all five findings resolved, no remaining findings.
Reviewer directly ran the17 DW-150 cases against the final Debug assembly:
17 passed/zero failed/skipped,exit0. Exact command/output/XML counters are
preserved in dw-150-command-results.json and dw-150-ui-summary.json. No production
change was needed after the initial guard/comment patch.

Final qualification: Debug and maintained Release focused332/332, unfiltered
per-project UI5004/5004, zero failures/skips, and both maintained browser lanes
pass. Exact blocked broad/package/provenance gates remain recorded separately.
Only DW-150 closes; counts/index/readiness are reconciled. User no-commit and
preservation instructions remain controlling; archive/sprint are untouched.

Initial ledger audit rejected an incorrect explicit-disposition-only done count.
It passed every preservation/coverage check. Corrected recomputation uses total
done323 minus the preserved204 earlier closed-record boundary =119 reconciled
done;312 older closed records without disposition fields remain unchanged.
Exact failed/successful audit outputs are retained, without changing any record.
