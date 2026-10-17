---
title: 'DW-545 conflicting administrator projection evidence'
type: 'chore'
created: '2026-10-17'
status: 'in-review'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'ceac6269da3e99d4d5e33c9fa3dd163959395f0f'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-545 reports that duplicate administrator IDs can conceal conflicting row evidence in a complete projection walk, weakening the population evidence used by removal and command reconciliation. The user selected this deferred item for completion.

**Approach:** Establish the current row and page contracts, preserve supported identical duplicates and ordinal identities, and prove that conflicting duplicates within or across cursor pages produce canonical incomplete evidence. If current guards already exclude every representable conflict, preserve those guards and document that finding accurately rather than introduce an unreachable production check. Resolve only DW-545 after independent review and relevant verification; preserve its original finding and all unrelated deferred records.

</frozen-after-approval>

## Implementation Notes

- Entry: clean `main` at canonical `ceac6269da3e99d4d5e33c9fa3dd163959395f0f`. No intent gaps or irreversible operations. Footprint: the existing loader test file, this spec, selected ledger entry, maintenance index and focused validation evidence. No public API, dependencies, persistence, UI markup or Git mutation is required.
- `GlobalAdministratorRow` is a sealed record with exactly `UserId`, `Freshness`, and `Lifecycle`; the wire `GlobalAdministratorSummary` adds only `UserId`. The dictionary uses ordinal identity equality. `GlobalAdministratorsProjectionLoader.IsCurrentStablePage` requires every row to have Current freshness and lifecycle before `TryAdd`. Every unequal duplicate currently representable therefore fails page validation before merging. No production defect reproduction is claimed.
- Reuse `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` page/canonical-incomplete helpers and preserve `LoadAsyncDeduplicatesIdentitiesWithinAndAcrossPagesWithOrdinalComparison`. Add conflicts in both row orders within the first page and across subsequent cursor pages, with final/nonfinal conflict pages; assert canonical redaction, bounded reads, and the actual cancellation token/opaque cursor forwarding. Existing action evaluator and preview tests cover incomplete population denial.
- `GetGlobalAdministratorsQueryHandler` serves a HashSet-backed projection. Existing identical-repeat acceptance is deliberately supported by regression; reject-all-duplicates would broaden the requested change. No redundant inequality branch or weakened page invariant will be added.
- Given an equal-ID freshness/lifecycle disagreement, when the loader encounters it, then it must stop without reading a further continuation and return the canonical unavailable snapshot without rows, cursor, validator, caller binding, version or complete evidence. Given identical repeated rows and case-distinct identities, when the stable walk completes, then existing ordinal deduplication remains complete.
- Validate Debug source build, focused loader/action/preview/page tests and the full individual UI project via Microsoft.Testing.Platform. Run the required Aspire baseline and story-gitlink check; record exact results and limitations. Leave task changes uncommitted under the repository Git boundary.

- Initial execution: the Debug source build passes with zero warnings/errors; all 43 loader cases pass, including 16 added conflict cases. These cases pass against unchanged production guards and are verification coverage, not a before/after production fix. The full UI run reports inherited `PendingRemovalAuthorityReadReportsChangedSafety` failures; acceptance remains pending until that run and the focused recheck are recorded. No unrelated removal implementation or existing test has changed.

- Independent blind review identified three small coverage gaps: untested non-current enum values, no unrelated retained prefix row, and missing canonical request-page-size assertion. All three are patched in the existing test fixture. The theory now enumerates all nine defined non-current freshness/lifecycle alternatives across eight walk arrangements (72 added cases); it includes unrelated prefix evidence and asserts canonical page-size reset. Runtime guards and the existing identical/ordinal duplicate test are unchanged.
- Initial full UI execution: 6,172 passed, three failed, zero skipped, exit 2. Two failures are the EN/FR non-retry confirmation-change variants of `PendingRemovalAuthorityReadReportsChangedSafety`; the third is `OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority`. All production and pre-existing test methods are unchanged from entry; targeted recheck is required and no unexplained failure is suppressed.

- Final coverage recheck: independent reviewer accepts all three corrections with no remaining concrete gaps. The expanded loader suite passes 99/99 and the source build has zero warnings/errors. The focused inherited timing recheck runs nine cases: eight pass, one different removal confirmation/retry variant fails with the same exact-versus-generic message mismatch. The initially failing page case passes; its initial failure was a pending-timer count of one versus expected zero. Final full UI acceptance and ledger closure remain pending.

## Review Triage Log

- **low / patch — non-current alternatives:** The initial matrix sampled only Stale freshness and Rebuilding lifecycle. It now enumerates every defined non-current freshness/lifecycle value independently (nine alternatives) across eight page/order/continuation arrangements. All 72 added cases pass in the 99-case loader suite; independent source recheck accepts the correction.
- **low / patch — unrelated population redaction:** A result containing only the conflicting ID did not demonstrate that other retained rows are also cleared. The accepted prefix (or same page) now includes an unrelated current administrator; canonical empty-row assertions demonstrate full population redaction. Independent source recheck accepts the correction.
- **low / patch — canonical request metadata:** The initial canonical helper omitted page size. It now asserts reset to the default request page size, distinct from the seven-row input request. Independent source recheck accepts the correction. No review finding required production changes or a new deferred goal.

- Final acceptance outcome: all 72 new conflict cases pass in the full UI run; final counts are 6,229 passed, two failed, zero skipped (exit 2). Both failures are unchanged `PendingRemovalAuthorityReadReportsChangedSafety` variants: FR freshness-change/retry observes the prior delivery-recovery message; EN confirmation-change/non-retry observes the generic changed-safety message. Their root cause is not established. The Hexalith baseline's "All existing and new tests must pass before a story is complete" overrides the workflow's automatic done step: this spec stays `in-review` and DW-545 stays open/actionable.
- Current evidence and all initial results are in [validation evidence](dw-545-validation-evidence.md). Preservation passes for all 647 IDs, original DW-545 fields, the other 646 ledger records, archive/sprint and production sources. Counts remain 353 done/294 open across 265 exact-covering maintenance rows. The story-gitlink guard passes with no pointer movement. Five task files remain unstaged/untracked; the explicit repository restriction on staging/committing without a task requirement takes precedence over the workflow's automatic commit step. No production/API/dependency or release qualification change was made.

- **low / patch — final bookkeeping terminology:** Independent final review accepts the test identities/counts, preservation, open ledger status and in-review spec. Its wording correction is accepted: earlier append-only notes describing "inherited" failures or a "timing recheck" mean only failures in unchanged existing tests and a focused recheck. Those words do not establish a timing or other root cause; none is established. Current validation evidence uses the precise cause-neutral terms. No new deferred goal results.
