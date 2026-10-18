---
title: 'DW-588: Reject partial-release recovery when an existing release has no tag'
type: 'bugfix'
created: '2026-10-18'
status: 'in-progress'
route: 'oneshot'
baseline_commit: '784b468e455150933585fec7b84fee2a18f3713e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Partial-release recovery independently accepts a missing GitHub tag and an existing GitHub Release. Publication leaves that release untouched, so final tag verification cannot succeed after other artifacts may already have been published.

**Approach:** Reject an existing-release/missing-tag inventory during recovery preflight, before package/container inspection or recovery-state creation. Preserve source/CI checks, tag-source binding, draft and release-tag validation, and the currently supported absent-release recovery paths. Add isolated executable-shell regressions for the contradictory inventory, supported lightweight/annotated tags, absent releases, and existing refusal gates. Close only DW-588 after owning tests and independent review, retaining original findings, unrelated ledger records, archive/sprint, dependency pointers and release holds.

</frozen-after-approval>

## Implementation Notes

- Selected the next focused release-recovery defect from the deferred ledger following DW-215–DW-219 work. The ledger's recorded next action and existing final verifier settle intent; no human-only decisions or irreversible actions are needed. The small footprint is one shell guard, its owning executable tests and scoped maintenance evidence, with no new public API or dependency.
- Entry revision: `784b468e455150933585fec7b84fee2a18f3713e`. `scripts/validate-partial-release-recovery.sh` validates tag provenance before reading release state, then creates recovery files and probes NuGet/container destinations. Add the consistency guard to the existing release-200 branch. Keep `scripts/publish-partial-release.sh`, `scripts/verify-partial-release.sh`, workflow permissions/authentication and separate DW-581/DW-607 concerns unchanged.
- Use the existing Contracts project's Bash process/stub conventions, real jq, isolated temporary state and strict unexpected-command refusal. Verify the new negative cases against original production first, run the focused class and full owning Contracts suite with Debug source references, then shell syntax, whitespace and story-gitlink checks. A required isolated Aspire pre-edit snapshot supplies environment evidence, not release/runtime qualification. The build workflow directs independent review and a validated local Conventional Commit; no push or publication is requested.
