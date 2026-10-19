---
title: 'DW-643: Verify recorded story gitlink endpoints'
type: 'bugfix'
created: '2026-10-19'
status: 'done'
route: 'oneshot'
baseline_commit: '63b7b3d8d6cd18db189c7dbfaf3cd94491115fa9'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A story with a `Story-Owned Gitlink Commits` section can pass the guard after an unrecorded commit moves a submodule pointer. The guard checks listed commits but never reconciles their final targets with the compared revision.

**Approach:** Enforce the recorded endpoints at the revision being audited. Every owned path must match its last recorded target, including a path reverted to baseline; every other changed gitlink must be rejected. Keep full commit provenance, declarations, target claims, chain checks, working-tree refusal, legacy stories and CLI exit codes. Use the existing explicit `--ref <historical-story-commit>` to audit a historical endpoint after independent dependency updates; do not infer ownership from commit messages or introduce exclusion lists. A matching recorded endpoint passes even when an unrecorded move was subsequently reverted: this task validates net endpoints, not exhaustive intermediate history.

Reject later advances, reversions, removals, additions and unrelated-path movements with a diagnostic naming the path and expected/actual endpoint. Confirm an unchanged recorded endpoint and an explicitly selected historical endpoint pass. Only DW-643 closes after successful verification; retain all other deferred items, historical evidence and release holds. No application changes, dependency changes, CI expansion, commits, pushes or submodule operations are included.

</frozen-after-approval>

## Implementation Notes

- Selected by the user on 2026-10-19 after the recommended DW-643 scope proposal. Entry baseline: `63b7b3d8d6cd18db189c7dbfaf3cd94491115fa9`; clean `main` working tree.
- Route assessment: no remaining intent gaps; no irreversible actions; small Python guard/test change plus spec, validation evidence and the two existing backlog records. The tracked DW-643 finding explicitly requests endpoint reconciliation. Existing explicit `--ref` preserves historical auditing without the removed Non-Story exemption.
- Code map: `scripts/validate-story-gitlinks.py`, `check()` owned-commit branch; reuse `gitlink_changes()`, `current_pointer()`, canonical commits and existing failure aggregation. Compare the union of owned paths and baseline-to-audited-ref changes so reversions to baseline cannot disappear.
- Tests: `tests/scripts/test_validate_story_gitlinks.py`, real isolated-Git CLI fixtures. Replace the blanket later-unrelated-update PASS expectation with current-endpoint refusal and historical-reference success. Extend the helper only as needed to select a ref and create representative real gitlinks.
- Documentation: update the guard's docstring and diagnostic guidance to explain exact compared endpoints and historical `--ref`. Keep `.github/workflows/story-guards.yml` unchanged; DW-585 remains separate.
- Verification: baseline `python3 tests/scripts/test_validate_story_gitlinks.py` passes 23/23. Add meaningful refusal/control fixtures, run them against the original script to demonstrate the defect, then run the full guard lane and other Python script tests. Run `git diff --check` and validate that only DW-643 and its maintenance row close, all IDs and unrelated record bodies remain intact, and dependency pointers remain unchanged.
- Implemented endpoint reconciliation over recorded paths plus the net gitlink diff, normalized absent recorded targets, and disabled rename folding to preserve each path. The docstring and CLI explanation now state the audited-endpoint contract and existing historical-reference option.
- Added 12 test methods (35 total), with real isolated Git commits for all refusal and control cases. The original production script with final tests reproduces eight unsafe PASS failures (exit 1), while the implemented script passes 35/35 (exit 0). The full `tests/scripts` discovery lane also passes 35/35; no other Python test files are present. Exact scope and reproducible control: `dw-643-validation-evidence.md`.
- A fully recorded rename control exposed the inherited `None` versus all-zero old-pointer chain mismatch once renames are parsed as separate paths. Normalized absent old pointers to Git's representation and added recorded-addition and recorded-rename success controls. Final maintained lane is 37/37; original production with those tests fails ten cases: eight unsafe PASS cases and these two legitimate-change controls. This preserves chain validation without rejecting a declared rename.
- Blind Hunter returned six findings. Applied all six within scope: absent-pointer normalization, an unchanged existing unowned-pointer control, real unstaged submodule-checkout coverage, exact expected/actual unowned diagnostic assertions, endpoint-only historical-ref guidance, and completion/evidence reconciliation. No findings were deferred. Review corrections add two test methods, making the lane 39 tests; final recheck follows.
- Final direct lane and independent reviewer recheck: 39/39 pass; final full `tests/scripts` discovery: 39/39 pass, zero failures/skips. The original script with final tests runs 39 cases and fails exactly ten as expected. The task's own story-gitlink check passes with no pointer movement, and `git diff --check` passes. Ledger reconciliation preserves all 655 IDs, changes only DW-643 and removes only its maintenance row: 380 done/275 open across 247 exact-covering rows. All review findings are resolved; no new deferrals or release-readiness claims. Changes remain uncommitted under the repository's Git-mutation safeguard and frozen scope.

## Review Triage Log

- Medium, patch: recorded additions/renames produced `CHAIN GAP` because absent baseline pointers were `None` instead of Git's zero SHA. Independently reproduced; normalize the chain predecessor and exercise both valid changes. The guard now accepts them without weakening missing-intermediate refusal.
- Low, patch: unchanged existing unowned pointers lacked a direct positive control. The new fixture leaves Other at baseline, declares only Dependency and requires a CLI PASS.
- Low, patch: the inherited working-tree test staged its change and did not cover checkout-only movement. A local isolated clone now proves an unchanged checked-out pointer passes, moves the checkout without staging, verifies the index is clean and requires the exact WORKTREE MOVE diagnostic.
- Low, patch: existing unowned-change diagnostics were asserted only by marker. Assert the full baseline expected SHA and actual recorded target too.
- Low, patch: historical `--ref` advice appeared for declaration/claim/chain/worktree failures. Emit it only when an endpoint mismatch exists; positive/negative hint assertions cover both routes.
- Low, patch: review/evidence/spec completion records were pending while the draft closure record linked them. Finalize the evidence and spec only after the review corrections and final verification; retain earlier run counts as historical notes.
