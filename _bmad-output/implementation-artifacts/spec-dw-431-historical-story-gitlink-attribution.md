---
title: 'Bind Story 5.2 historical gitlink evidence to its actual endpoint'
type: 'chore'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: 569e80e7364b57c92ffdd80519e58fc47bc8b096
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-431 records a historical Story 5.2 audit against moving HEAD. The existing File List names four dependencies but omits exact closure targets and the final story commit's two pointer moves. This attribution gap also obstructs the first queued CI gate bundle, DW-437/DW-451/DW-534.

**Approach:** Resolve DW-431 as a small historical-evidence repair. Preserve the true baseline, frozen product intent and prior observations; record canonical initial/final endpoints, every pointer-bearing commit in that range, exact old/new dependency IDs and whether each commit carried Story 5.2 work or independent work. Bind the existing four File List declarations to the final historical targets and document the explicit historical audit command. Keep the moving-HEAD refusal visible and preserve unrelated deferred records and release holds.

</frozen-after-approval>

## Implementation Notes

- Entry: clean `main`, canonical revision `569e80e7364b57c92ffdd80519e58fc47bc8b096`. DW-431 is selected as the historical-attribution prerequisite of the first maintenance bundle; the changed-story CI gate remains separately open.
- Investigation: `scripts/validate-story-gitlinks.py` supports explicit `--ref`. Baseline `fc147e3efab879f0d5f8131a4ebfd178bf3a113c` is the parent of initial implementation `f17027ac24a9af28f2e87d05951040be3be5db80`. The last Story 5.2 spec/status/test commit is `33d6dcca278952eaf65764e24b21000a9baa32d8`; it also carries EventStore and FrontComposer pointer changes omitted from the earlier attribution prose.
- Footprint: the historical Story 5.2 spec outside its frozen block, this spec, DW-431 evidence, the selected deferred record and its maintenance row. No production, test, workflow, dependency or submodule edits. No unresolved intent gaps or irreversible implementation step; commit/push is explicitly authorized by the user.
- Keep the legacy baseline-to-endpoint audit. A strict Story-Owned Gitlink Commits list cannot honestly include independent interleaved dependency updates. Do not weaken the guard or label those commits story-owned to force it through that mode.
- Verify the complete raw Git change chain and final snapshot independently; run the historical guard, wrong-target/removal controls in temporary spec copies, moving-HEAD refusal, the maintained guard regression lane, this work's gitlink guard, whitespace and exact ledger/index preservation. Existing initial guard PASS is a pre-repair observation; after binding closure targets, use the raw baseline-to-initial diff to prove its zero-pointer claim.
- Narrow validation applies to documentation and Git provenance; application builds, Aspire and browser execution do not exercise this repair.

- Implementation completed: added exact closure targets and an explicit historical audit section to the original story without modifying its frozen block or any earlier content. Added full seven-commit/fifteen-move evidence, resolved only DW-431 and removed only its maintenance row.
- Final verification: historical snapshot PASS; eight incorrect-target/declaration controls fail as expected; original initial/closure documents PASS; initial current-document and moving-HEAD refusals are explicit. The maintained guard suite passes 77/77. Exact preservation checks pass with 656 IDs, 391 done/265 open and 237 exact-covering rows; dependencies, archive/sprint/release and unrelated content are unchanged.
- Independent blind review returned five observations. The evidence now contains durable executable recipes, file-level attribution and explicit current-document-versus-historical-tree semantics. Completion metadata is finalized here. All findings are resolved within this documentation scope; no new deferral remains.
- Pinned commitlint 21.2.3 validates the full candidate with zero problems/warnings. Origin was fetched without submodule recursion and remained aligned at the entry revision; only the five selected documentation files will be committed.

- Independent correction recheck: all five findings are addressed; the reviewer independently executed both durable recipes successfully and reported no outstanding correction.

## Review Triage Log

- Medium / patch: negative controls previously depended on a temporary script unavailable to fresh checkouts. Added and executed a self-contained standard-library recipe in the evidence document for the passing historical snapshot and all eight rejected mutations.
- Medium / patch: preservation claims lacked a durable reproduction recipe. Added and executed exact selected-record, retained-field, full-index, preamble, frozen-intent, other-story-content, archive/sprint/release and dependency comparisons against the canonical entry revision.
- Low / patch: independent-commit classifications needed file-level support beyond subjects. Inspected each changed file list and Story 5.1 tracking diff; recorded bootstrap-only, gitlink-only and story-spec/UI/test evidence.
- Medium / patch: historical `--ref` semantics could be misread as loading old Markdown. Explicitly documented that the supplied current document is checked against the selected historical Git tree; entry-document replay remains separately identified.
- False / normal finalization: the in-progress spec and absent final review disposition were unfinished workflow steps during review, not a completed-work inconsistency. Finalized this spec to done, recorded review iteration 1 and the completed checks in the evidence.
