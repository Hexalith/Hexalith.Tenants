---
title: 'Close DW-367 as an accepted disposition'
type: 'chore'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '03615cc91d940ca37b47fb13413a90a31151dd71'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-367 as an accepted disposition following the user's explicit choice to close it. This is a narrow exception to the maintenance index's rule that accepted limitations stay open for visibility. Preserve the deliberate role-selection policy, the absence of historical role information and the requirement for separately approved contract/product work before reopening the event schema. Do not claim a runtime fix, recovered historical roles or release qualification.

Update only DW-367 in the deferred ledger, remove its maintenance action row, reconcile current totals and retain concise validation evidence. Preserve all original fields, including replaced values under `pre-accepted-close-` keys, and keep its dependency and historical finding. Leave the already completed DW-117/DW-118/DW-133/DW-365 records, the withdrawn five-item draft, every unrelated record, application code, archive, sprint tracking, gitlinks and pre-existing user changes untouched. Independently review, validate and commit/push the scoped documentation changes to main as requested by the original invocation.

</frozen-after-approval>

## Implementation Notes

- The user resolved the policy choice with “close” after being offered preservation or a documentation-only closure plan. The withdrawn draft remains published planning history; this new spec records the narrower decision.
- Story 5.5 requires deliberate role selection and forbids inferring roles from history or adding contract/history fields. `UserRemovedFromTenant` still has only `TenantId` and `UserId`; `TenantCorrectionStartIntent.Evaluate` requires `context.IntendedRole`. These are retained limitations, not defects repaired by this task.
- Entry has 657 numbered records: 651 done and six open across six index rows. Only DW-367 is expected to close, leaving 652 done and five open across five rows, with zero open accepted limitations.
- Entry main has unrelated dependency-documentation changes in three tracked files and two untracked evidence files. Their bytes, the empty staging area and all nine gitlinks were captured before edits; stage only this closure's files.
- Concurrent dependency work subsequently changed four of those files and appended an unnumbered ledger follow-up. Preserve that work in the worktree and exclude its ledger hunk from this commit. Numbered-record parsing stops at section headings so the separate unnumbered appendix is not mistaken for part of DW-657. The strict whole-entry hash check reports this concurrent activity; the focused immutable-baseline closure check remains the acceptance gate.
- Final acceptance: 657 IDs remain, 652 done/five open across five exactly covering maintenance rows. The complete baseline ledger, all 656 unselected numbered bodies, original selected fields, application/tests, archived/sprint/source-story documents and dependency pointers are preserved. The independent reviewer confirmed all five corrections and independently repeated the positive and six rejection controls; no finding remains and nothing is deferred. Locale and optimized-execution controls behave as documented in the companion evidence.

## Review Triage Log

- Medium / patch: numbered-only comparison omitted baseline unnumbered history. The validator now restores the scoped record and preface edits and requires the complete baseline ledger byte sequence to remain intact; only the separately identified appended dependency section is allowed afterward.
- Medium / patch: historical reconstruction alone allowed deleting the resolution-spec link or weakening the current reopening policy. Require the exact closure link/date/next action and the retained role-selection, missing-history and no-runtime-fix verification facts.
- Low / patch: universal-newline text reads could not support byte-preservation claims. Decode raw bytes explicitly as UTF-8 without newline conversion, making line-ending changes detectable.
- Low / patch: locale-dependent reads failed under an ASCII locale. Use explicit UTF-8 decoding for every document and optional JSON snapshot.
- Low / patch: optimized Python could skip assertions and print a successful report. Reject optimized execution before loading data or reporting any result.
