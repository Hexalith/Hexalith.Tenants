---
title: 'DW-285 held submission acceptance reconciliation'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: '8a678be7d05985ab366eae0156c4094a02d90b02'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-285 still describes an absent submission guard, although create, add-member and change-role now guard reentry. Existing tests do not prove that availability changes during held delivery preserve the original attempt through completion.

**Approach:** Reconcile DW-285 by extending the real Fluent retained-flow fixture for all three flows. Hold dispatch, change freshness or command support through component parameters, resubmit, and assert unchanged snapshot, one send, retained ownership and aggregate admission. Complete the original delivery and verify its original identity, intent, baseline and returned tracking survive. Cover accepted and terminal failed completion. Reuse existing helpers; add no runtime/public API, dependency, gate, resource or orchestration changes. Keep DW-470 and every other record separate. Close only DW-285 after passing focused/full Debug UI verification and independent review; preserve historical findings, archive, sprint and release holds. No stage, commit, push or deployment.

</frozen-after-approval>

## Implementation Notes

- Investigation: all three `SubmitAsync` methods already guard `_isSubmitting` before authorization/unavailable branches. `SynchronizeRetainedAttempt` only synchronizes retained evidence. `TenantRetainedCommandFlowTests` supplies held delivery, typed snapshots, component submission and aggregate/tracker helpers.
- Route facts: no intent gaps or irreversible changes; one existing test file plus spec, evidence and two backlog documents. No new callable surface. Runtime behavior already exists; this is a small acceptance reconciliation.
- Verification: build `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` in Debug with `-p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`; run its maintained `dotnet test --project` lane with `--no-build --no-restore`, first `--filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1`, then the full UI suite with `--minimum-expected-tests 5954`. Run `python3 scripts/validate-story-gitlinks.py` on this spec and `git diff --check`. Record exact commands/results and any blockers in `dw-285-validation-evidence.md`.
- Implemented 12 actual Fluent cases in the existing retained-flow fixture. Pre-review Debug focused195/full5967 pass with no failures/skips. Initial CS0029 test compilation error was corrected without production changes. Isolated pre-edit Aspire reached all23 resources Running/Healthy and was stopped by exact target; unrelated AppHosts were not operated on.
- Blind review identified four acceptance-assertion gaps, all addressed below. No findings required runtime changes or deferral. Rebuilt reviewed tests with zero errors; focused/full rechecks are pending. No commit is made because the direct repository instructions and frozen scope prohibit staging/committing in this task.
- Final qualification: the independently rechecked assertions pass maintained Debug focused195/full5967 with zero failures/skips. Only DW-285 closes; ledger/index now retain all643 IDs as338 done/305 open across273 rows. Exact commands, initial failure, review and validation limits are in [evidence](dw-285-validation-evidence.md). Runtime/archive/sprint checkout hashes and the story-gitlink gate are preserved.

## Review Triage Log

- **medium / patch:** Dispatched arguments were discarded, so snapshot assertions could miss gateway identity/payload divergence. Captured actual intent/message ID and compare both to the original snapshot before reentry.
- **medium / patch:** Completion checked only baseline version, missing other reconciliation evidence. Added create absence/detail-version/start-time, membership postcondition/last projection and change-role current role/owner-count comparisons.
- **medium / patch:** Lock booleans could conceal attempt/lease replacement or lost renderer attachment. Capture the original attempt, lease and owner; verify exact identity, current attachment and active dispatched ownership after reentry and accepted completion, with exact release on failure.
- **medium / patch:** Completion explanation was unasserted. Assert the original safe failed message survives and accepted completion contains no stale literal or keyed unavailable reason. Keyed-localization DW-470 remains outside this scope.
