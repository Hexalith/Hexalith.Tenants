---
title: 'DW-419: Reject detected mid-story baselines in legacy gitlink audits'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '7f1e449dbe411853e065c938ca75d29e5688e15a'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-419 identifies a legacy gitlink audit that can pass with a warning when its recorded baseline already touches the story's own artifacts. Changes already included at that baseline fall outside the audit, so a passing verdict cannot qualify the complete story range.

**Approach:** Refuse detected mid-story baselines in legacy mode with an actionable true-start-baseline diagnostic. Preserve recorded-commit provenance checks, valid legacy audits, explicit historical refs, declarations, target checking and working-tree movement detection. Add isolated real-Git CLI regressions proving both refusal and valid-baseline behavior. Close only DW-419 after focused validation and independent review, preserving original ledger fields, unrelated records, dependency pointers, archive, sprint and release holds. Commit and push the completed change as requested.

</frozen-after-approval>

## Implementation Notes

- Selected the remaining legacy-baseline item in the recent gitlink maintenance work. This is freeform deferred work, with no sprint story key.
- Planning facts: no unresolved product-intent gaps; the code/documents establish the legacy versus recorded provenance distinction. Local edits are reversible; the requested push is authorized. Small footprint: Python guard, its existing stdlib integration lane, this spec, durable evidence, the DW-419 ledger block and its maintenance row. No new callable API or dependency is needed; the step-02 oneshot route applies.
- Code map: `scripts/validate-story-gitlinks.py::check` currently adds the result of `baseline_is_mid_story` to warnings before selecting provenance mode. `GuardCommandIntegrationTests` supplies isolated Git history and `_run_guard`; reuse those fixtures. `.github/workflows/story-guards.yml` already runs this lane. Keep historical planning artifacts and unrelated CI/story-attribution work outside this change.
- Clean entry tree on `main`; full canonical baseline is recorded above. Baseline maintained lane: `python3 tests/scripts/test_validate_story_gitlinks.py` passes 69/69. Repository guidance, attributes, editor/build configuration, contribution guide, July 28 gitlink proposal and relevant architecture constraints were inspected.
- Investigation subagent confirmed the recorded historical Story 1.6 baseline touches an associated spec rather than the exact story path. Preserve the existing conservative filename-stem detector; this scope changes its legacy verdict, not the general ownership heuristic. Inspection errors now propagate rather than silently establishing safety.
- Added six methods. The initial entry-production run had 28 failures, including three fixture errors caused by an absent dependency directory; corrected empty-directory fixtures yield exactly 25 intended failures (24 unsafe CLI PASS cases and one swallowed-inspection-error assertion). Repaired maintained lane passes 75/75 with no failures/skips.
- Added a seventh planned file, `dw-419-check-variants.py`, to keep the qualification reproducible without embedding a large replay harness. Its six focused methods pass on current production; entry, unconditional rejection, warning-only and swallowed-inspection variants produce exactly their expected failures. The harness requires an explicit output directory and never replaces workspace inputs.
- Aspire baseline used the local skill and existing Debug assets in an isolated instance. All 23 resources reported Running/Healthy; a bounded error query exposed existing Memories HTTP 500 / registry-initialization errors. Stopped the instance successfully. No .NET or live application acceptance is claimed for this Python-only repair.
- Ledger/index reconciliation preserves every unrelated record and all original DW-419 fields. The first integrity run caught one extra blank line in the selected record; restoring the original spacing makes the exact seven-file preservation check pass: 656 IDs, 387 done/269 open, 241 index rows. Dedicated evidence contains the reproducible check and execution limits.
- Independent review corrections: diagnostics now report the matched paths and filename-stem heuristic. The internal path collector retains the Boolean `baseline_is_mid_story` helper's interface. Historical refusal refs now differ from HEAD after a later ordinary commit. Two added methods qualify command-entry inspection failure and recorded-mode target/endpoint/worktree/chain refusal, including the earlier recorded endpoint's successful audit.
- The replay harness invalidates its previous success manifest before loading inputs. An isolated injected failed rerun proves the stale manifest is absent. Expanded focused replay uses eight methods; entry production has 26 exact failures, unconditional rejection has three, warning-only behavior has 24 and swallowed inspection errors have two. The initial expanded replay caught its stale expected PASS count; the corrected oracle requires all 25 unsafe command verdicts (24 subprocess and one in-process command-entry case).
- Final maintained lane passes 77/77. All six review findings are patched; none are deferred. Only the configured Blind Hunter layer ran, with no review layers skipped. Spec completion metadata now records done; `review_loop_iteration` remains zero because the oneshot route does not run dispatch review loops.
- Independent correction recheck confirmed all six findings resolved with no remaining concrete problems. The reviewer reran 77/77 tests, the eight-method replay with exact 26/3/24/2 faulty-variant counts, whitespace checks and the embedded seven-file integrity check successfully. The exact Conventional Commit message passes pinned commitlint; the user requested commit/push to complete this work.

## Review Triage Log

Blind Hunter's initial finding floor: 29,343 changed bytes / 1,024 = 28.66 KiB; min(floor(sqrt(28.66) + 1), 10) = six findings.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Refusal omitted the artifact and heuristic that triggered it. | Low; patch developer diagnostic. | The path collector now supplies matched paths; both legacy refusal and recorded warning name the filename-stem heuristic. Real-CLI cases assert the exact triggering artifact. |
| Inspection failure was qualified only at the helper. | Medium; patch verification gap. | Added a fault-injected command-entry case invoking `main()` with actual isolated Git history; it requires the failed exit value, error text and FAIL with no PASS. Swallowed-error replay now fails exactly the helper and command-entry cases. |
| Canonical refusal refs coincided with HEAD. | Low; patch historical test coverage. | The refusal fixture commits a later unrelated endpoint, asserts different canonical IDs and audits the earlier baseline as well as default/explicit HEAD. |
| Detected-mid-story recorded controls omitted other retained protections. | Low; patch targeted coverage. | An added fixture independently asserts MISSTATED, UNRECORDED ENDPOINT, WORKTREE MOVE and CHAIN GAP, plus a valid historical endpoint after later movement. An unconditional-refusal mutant fails the expected control. |
| Failed replay reruns could retain an earlier success manifest. | Medium; patch developer evidence lifecycle. | The harness removes `qualification.json` before qualification. A temporary injected failing copy starts with a seeded success receipt, exits 1 and leaves no manifest; the restored final harness writes a fresh receipt. |
| Completion evidence and spec status still awaited review. | Low; patch planned completion metadata. | All six findings and their dispositions are recorded here; dedicated evidence records final commands/counts. The oneshot spec is done with no deferred findings. |
