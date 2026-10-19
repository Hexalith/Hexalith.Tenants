---
title: 'DW-520: Validate three-column gitlink target tables'
type: 'bugfix'
created: '2026-10-19'
status: 'done'
route: 'oneshot'
baseline_commit: 'c4c781791b0b50eabeb0b58505d943be40538e7f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The story gitlink guard ignores three-column dependency rows containing a path, baseline and target. An explicitly declared pointer can therefore pass while its table states a stale target.

**Approach:** Bind strict three-cell path/base/target rows alongside existing arrow claims in File List and the directly scoped Completion Notes List. Accept hexadecimal SHA prefixes of 7–40 characters, including full zero-SHA removal targets, and the existing `absent` baseline spelling for additions. Recognize ordinary Markdown outer pipes, optional backticks and existing Unicode normalization. Last applicable claim wins across arrow and table forms using the existing section order. Historical review subsections and unrelated sections stay nonbinding. A table does not replace the existing explicit bullet-path declaration.

Given a declared pointer and matching table target, the CLI must pass; given a stale target, it must report MISSTATED and fail in both legacy and recorded-provenance modes. Recorded additions/removals, unchanged pointers with claims, existing endpoint/chain/working-tree refusals and arrow claims retain their behavior. Headers, separators, prose and rows outside the recognized three-cell shape do not create new table claims.

Close only DW-520 after regression and independent review verification. Preserve other deferred entries, their historical evidence, dependencies and release holds. This scope changes the developer guard, its regression lane and completion records; it adds no application/runtime, CI, dependency or Git mutations.

</frozen-after-approval>

## Implementation Notes

- Selected the live DW-520 defect from the deferred-work request after repository and independent investigation. Entry is clean `main` at the full baseline above. DW-526/DW-531 already have source/test repairs, but remain separate records for later acceptance reconciliation.
- Route assessment: no unresolved intent choices or irreversible actions; small Python parser/test footprint plus spec, evidence, ledger and maintenance index. Existing docstring promises to check stale pointer tables; supporting the tracked table format fulfills that contract.
- Code map: `scripts/validate-story-gitlinks.py::stated_targets` and its existing normalization/section helpers; preserve `declared_paths`, canonical Git provenance, chain and endpoint checks. `tests/scripts/test_validate_story_gitlinks.py` supplies parser tests and isolated real-Git CLI fixtures. Reuse that fixture without repository Git operations.
- The tracked motivating table in `spec-5-6-preview-confirm-and-link-a-tenant-correction.md` uses full SHA bases and `absent` for new paths. Independent parser and CLI probes reproduce the missing binding and unsafe PASS. Initial maintained guard lane: 39/39 pass.
- Verification plan: exercise positive/negative table CLI cases in both modes, parser normalization/order/section boundaries, addition/removal and unchanged-pointer controls. Run new tests against original production before the repair, then the maintained Python lane, an isolated original-script replay, story-gitlink validation and `git diff --check`. Reconcile all IDs and unrelated record bodies against the baseline. No .NET or distributed-runtime behavior changes require runtime validation.
- Implemented a narrow three-cell decoder feeding the existing ordered claims map. Headers and other shapes fall through to unchanged arrow parsing; explicit declarations and Git/provenance validation remain unchanged. Added 15 test methods, including real-Git CLI rejection and compatibility controls in both modes.
- Before the production repair, the 54-test lane fails with 19 assertion/subtest failures, including two unsafe legacy PASS outcomes. The repaired lane and full Python discovery each pass 54/54. An isolated copy of the original production script with current tests reproduces the same 19 failures. `git diff --check` and this spec's story-gitlink check pass. Independent review and final ledger reconciliation remain pending.
- Independent review exposed fenced and whole-inline-code table examples overriding live claims. Contained the new table decoder to non-code rows, with cell-local paired backticks and backtick/tilde fence tracking; indented examples are excluded too. Existing arrow handling remains unchanged. Added both-mode CLI negative controls, fence recovery controls, default-HEAD coverage with a real isolated dependency checkout, recorded unowned-claim refusal and addition/removal refusals. Final direct lane passes 61/61. The first expanded 61-test run had three fixture failures because the synthetic gitlink had no checkout; initializing only its isolated local checkout fixes those controls.
- The original production script with the final 61-test lane reproduces 45 assertion/subtest failures. The unchanged-pointer table control is explicitly an audited-commit case: default HEAD with an empty dependency directory inherits a separate superproject-fallback bug. Both current table/arrow and original-production arrow CLI reproductions fail with MISSTATED despite a matching pointer; retain this as new DW-656. Do not broaden this repair into current-pointer resolution or alter its working-tree contract.
- Final full Python discovery and independent correction recheck each pass 61/61 with zero failures/skips; `git diff --check` and the spec's story-gitlink validation pass. The reviewer confirms all five dispositions and finds no further concrete defects. Lossless ledger/index reconciliation passes: all 655 original IDs remain, only DW-520 closes and all replaced fields are preserved, with DW-656 added. Totals are 656 IDs, 382 done/274 open and 246 exactly covering maintenance rows. Every unrelated record/row, dependency pointer, archive and sprint file is preserved. Exact historical and final commands/results are in `dw-520-validation-evidence.md`. Changes remain uncommitted under the user-provided Git-mutation safeguard.

## Review Triage Log

- Medium, patch: fenced examples could replace a stale live table claim with the actual target and produce unsafe PASS. Backtick/tilde fence tracking excludes new table claims inside examples; parser and both-mode CLI regressions prove refusal and parsing recovery after fence closure.
- Medium, patch: stripping unmatched backticks separately from cells interpreted a whole inline-code span as a table. Only paired cell-local backticks are unwrapped; parser and both-mode CLI regressions prevent the reproduced unsafe PASS. Indented code examples receive the same narrow exclusion.
- Medium, defer as DW-656: default HEAD with an empty/deinitialized dependency directory lets `git -C <path> rev-parse HEAD` resolve the superproject instead of its recorded gitlink. Current table/arrow and original-production arrow CLI controls reproduce the inherited false failure. New table default-mode tests use an actual isolated checkout; unchanged-pointer evidence is qualified to explicit `--ref`. This repair leaves `current_pointer()` unchanged.
- Low, patch: recorded provenance still rejects a matching claim for an unchanged unowned dependency with UNOWNED CLAIM. Added a compatibility regression and qualified the evidence; the intended refusal remains unchanged.
- Low, patch: special addition/removal spellings previously had acceptance-only controls. Added stale addition, present-pointer zero-target and removed-pointer nonzero-target CLI refusals, while retaining matching controls.
