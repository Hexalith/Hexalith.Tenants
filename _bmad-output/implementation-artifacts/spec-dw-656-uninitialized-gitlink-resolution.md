---
title: 'DW-656: Resolve unchanged gitlink claims without a dependency checkout'
type: 'bugfix'
created: '2026-10-19'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '43786ea8c4a3cdd69562f7eec1109c1b891f6492'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Default-HEAD gitlink validation runs Git inside an empty dependency directory, discovers the parent repository, and rejects correct unchanged-pointer claims using the superproject SHA. Missing or deinitialized checkouts must not require initialization merely to validate a recorded pointer.

**Approach:** Resolve a dependency's own checkout only when it owns the discovered worktree; otherwise use the superproject's recorded gitlink for the working-tree comparison. Preserve actual checkout and staged movement detection, historical explicit-ref audits, table/arrow claims, and recorded provenance. Add isolated CLI regressions for matching and stale claims, false parent-HEAD claims, initialized checkouts and gitfile checkouts. Close only DW-656 with durable evidence and lossless ledger/index reconciliation after focused checks and independent review. Do not change dependency pointers or application behavior.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent gaps or irreversible actions; footprint is the Python guard, its maintained stdlib test lane, this spec, validation evidence and the two maintenance records. Route is oneshot.
- Reuse `GuardCommandIntegrationTests`, `_run_guard(include_worktree=True)`, existing staged/unstaged movement controls and explicit-ref coverage. `current_pointer` is the narrow resolution seam; table/arrow parsing and provenance contracts remain unchanged. `.github/workflows/story-guards.yml` already runs the maintained lane.
- Entry revision is recorded above using the full canonical Git identifier. Clean `main`; baseline maintained lane passes 61/61. Repository guidance, editor configuration, attributes, build configuration, contribution guidance, relevant architecture constraints and DW-520 evidence were inspected.
- Implementation: `current_pointer` verifies the discovered checkout root before reading its HEAD; empty/deinitialized paths use the exact stage-zero index gitlink with NUL-delimited parsing. Explicit-ref resolution and movement detection are unchanged.
- Added isolated table/arrow matching, stale and false-parent-HEAD controls; staged net-zero changes; real checkout and gitfile checkout controls, including checkout movement that disappears from the baseline diff; deinitialized-directory fallback; and missing-directory removal refusal.
- Initial 65-test run against original production reproduced nine assertion/subtest failures, including two false PASS outcomes for superproject-HEAD claims. The first repaired run exposed a test-fixture error: the separate Git-directory parent did not exist. Creating that parent fixes only the fixture; the final lane now includes an additional own-checkout/index precedence control.
- Aspire baseline: used the local Aspire skill, started the existing Debug assets with `aspire run --detach --no-build --isolated --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive`, inspected all 23 resources as Running/Healthy, then stopped this instance successfully through `aspire stop`. No .NET/browser or fresh-build validation is claimed for this Python-only repair.
- Corrected an over-specific assertion in the additional checkout control: Git reports a raw gitlink entry even when its resolved checkout target equals the baseline. The test now asserts the actual checkout result directly against the isolated Git repository, plus CLI matching/stale verdicts, without assuming an empty raw diff. No production expansion was needed.
- Final maintained direct lane passes 66/66 with zero failures/skips. Isolated entry-production replay with the same final tests exits 1 and reproduces nine assertion/subtest failures, including the two unsafe table/arrow superproject-HEAD PASS outcomes. No real workspace checkout or script is replaced during replay.
- Independent review exposed unchecked absent-gitlink claims and unresolved index conflicts. The two initial review-regression tests reproduced seven unsafe PASS failures before correction. Missing resolved pointers now use the existing zero-SHA representation for claim checking; unresolved exact-path index stages raise a validation error, even when an initialized checkout exists. Real index-read errors also propagate as failed validation.
- The preserved worktree boundary distinguishes missing checkout metadata in an existing directory from removal of the directory itself: only the former uses the index fallback. Completely missing directories retain Git's existing removal verdict. This clarifies the frozen intent without changing it.

- The independent correction recheck found a changed-path conflict bypass: raw Git output could supply index stage 1 and skip `current_pointer`. A new both-mode regression reproduced two assertion/subtest failures, including a legacy unsafe PASS. Working-tree `gitlink_changes` now refuses unmerged gitlinks before raw diff interpretation. Final direct lane passes 69/69; isolated entry-production replay reproduces 21 assertion/subtest failures.

- Final independent recheck confirms 69/69 maintained tests, the story gitlink guard, scoped whitespace checks and the recorded integrity script; no further concrete findings remain. Final Python discovery also passes 69/69. All 656 IDs remain: 383 done/273 open across 245 exactly covering maintenance rows. Only DW-656 closes; historical fields, unrelated records/index content, dependency pointers, archive and sprint remain preserved.
- Concurrent unrelated edits appeared in `Directory.Build.props`, the Contracts project, `PackageGovernanceTests.cs` and untracked `Contracts/Module.cs` during final review. They were left untouched and excluded from this repair's validation/review scope. No staging, commit or push is performed under the user-provided Git-mutation safeguard.

## Review Triage Log

- Medium, patch: an empty or ordinary directory without a gitlink silently skipped a stated target. Confirmed against current and entry production; current default mode newly passed nonzero claims, while explicit-ref mode already had the omission. All missing resolved pointers now receive the zero-SHA claim check. Both comparison modes reject nonzero claims and retain zero-target compatibility; tracked child files cannot substantiate a gitlink claim.
- Medium, patch: unresolved exact-path index stages returned no pointer and allowed any claim. Reproduced default-mode unsafe PASS for stale, zero and matching targets. The resolver now refuses unmerged claims before checkout lookup, with empty and initialized controls and an explicit-ref positive control.
- Low, patch in validation evidence: clarify missing-checkout terminology. Existing empty/deinitialized directories fall back to the index; absent directories retain removal detection. Frozen intent is unchanged and this distinction is explicit in the implementation notes and evidence.
- Low, patch as planned completion: durable evidence and lossless ledger/index reconciliation were pending during review. Finish them with final checks and retain historical fields and unrelated rows before closing DW-656.

- Medium, patch on independent recheck: changed unmerged paths bypassed the resolver conflict check and could accept a stage-1 claim. Confirmed with stage 1 different from the baseline; the new test fails twice before correction. The diff boundary now refuses unmerged gitlinks for default-mode legacy and recorded provenance; explicit revision audits remain valid.
