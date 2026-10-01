---
title: 'Close Story 5.4 remaining verification patches'
type: 'bugfix'
created: '2026-10-01'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: e2558361cac80d6ff253f66f308f3e511c584ade
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4's latest re-review leaves five patches open: actual ascending sort events are untested, successful refresh after disposal bypasses the Refresh button, finalization checks exception type without identity, and the verification records misdescribe ARIA counts and the final mutation/test totals.

**Approach:** Close those five patches with deterministic regression coverage and accurate evidence. Exercise ascending Tenant and Status sorts through the real grid callback, successful disposal through a real Refresh click, and live finalization exception identity. Correct the historical ARIA explanation and nine-mutation/3,586-test record, then record current verification separately. Preserve production behavior and existing deferrals.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent choices or irreversible operations. Footprint: `TenantListSurfaceTests.cs`, `AuditAvailabilityStateTests.cs`, the comment in `GeneratedTenantsSurfaceTests.cs`, this spec, the legacy story, the existing verification report, and sprint tracking. No new API or dependency changes.
- Reuse the real grid's `SortByColumnAsync`, gateway request captures, canonical navigation assertions, and existing bounded teardown/dispatcher helpers. Ascending tests must verify selected column, false descending flag, first-page cursor/history reset, and canonical URL after a descending page-two state. Keep cancellation assertions type-based because asynchronous propagation may recreate the cancellation exception.
- Story key: `5-4-understand-audit-availability-and-recovery`. Cached Epic 5 context is valid; Story 5.3's completed receipt work supplies continuity. The clean baseline is the canonical revision above. Aspire inspection confirmed all 20 resources Running and Healthy before edits.
- Verify a Debug source build, focused changed classes, the maintained UI MTP lane, a deliberate descending-only sort mutation, diff hygiene, and the follow-up gitlink guard. The original story baseline, CI package boundary, CI Chrome abort, Debug browser harness paths, and unrelated timing flakes remain previously recorded deferrals.
- Added two real ascending grid-sort cases from descending page two, changed successful teardown coverage to a Refresh click, and asserted live finalization exception identity. Corrected the ARIA timing rationale and historical totals in the legacy story and report; all five patch checklist entries are closed.
- Debug source build passed with zero warnings/errors; the three changed test classes passed 188/188. Forcing non-empty sort events descending failed both added cases. Original production source bytes were restored before a non-incremental Debug build (zero warnings/errors), then maintained MTP passed 3,588/3,588 with zero failed/skipped in 38.179 seconds. Follow-up gitlink guard and diff hygiene passed. Exact commands are recorded in the report's 2026-10-01 addendum.
- Independent review prompted a third ascending case from an already ascending Name sort, distinct snapshots through page three, visible row-order checks, renderer-driven disposal, and a bounded ARIA-count wait. All 189 focused cases pass. Replacing incoming direction with `!_sortDescending` fails precisely the new ascending-to-ascending Status case; source bytes were restored before a zero-warning/error non-incremental build.
- A supplementary read-only formatting check exits 2 because its same-line-brace expectations conflict with the baseline's required Allman style. It reported 299 whitespace diagnostics before the review amendments, including existing blocks and four new blocks matching the required style. No formatting rules were weakened or repository-wide rewrite applied; the policy conflict is recorded for separate maintenance.
- Final post-review non-incremental Debug source build passed with zero warnings/errors; maintained MTP passed 3,589/3,589 with zero failed/skipped in 36.437 seconds. All five independent review findings are patched. Story and sprint tracking move to review; this implementation spec is done. The pinned commitlint CLI accepted the exact full candidate with zero problems/warnings, with validation evidence preserved in the report.

## Review Triage Log

- `low` — identical snapshots could hide retained page rows or incorrect visible order; patched with distinct first/second/third-page snapshots and explicit ascending row-order assertions after sorting.
- `medium` — descending-to-ascending cases alone could permit toggling the previous direction instead of reading the event; patched with an ascending Name-to-ascending Status case. A direction-toggle mutation fails that case while the two descending-start cases pass.
- `low` — a single cursor-history entry cannot distinguish clearing history from popping once; patched by reaching page three before sorting and asserting the Previous action is disabled after returning to page one.
- `low` — manually setting component disposal could mask lifetime integration; patched with bounded `DisposeComponentsAsync`. The suggested wrapper-only `cut.Dispose()` was tested and left the component live (retry count 1); renderer disposal passes with zero retries.
- `low` — the populated ARIA count lacked an explicit completion wait; patched with a five-second `WaitForAssertion`, preserving the RC/stable row-count explanation.
- Supplemental validation, `low`, deferred — read-only formatter brace expectations conflict with required Allman blocks throughout the existing test files. Recorded the exact command and exit result in the report and appended a separate maintenance item; no build/test gate was weakened.
