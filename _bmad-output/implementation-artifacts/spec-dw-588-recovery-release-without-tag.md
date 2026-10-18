---
title: 'DW-588: Reject partial-release recovery when an existing release has no tag'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
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
- Added `PartialReleaseRecoveryTests.cs` with sixteen executable cases and persisted-inventory assertions. The initial build rejected one Shouldly overload (CS1503); using its named `customMessage` argument repairs the fixture. The first direct runner invocation used unsupported MTP arguments and exited 3; the supported xUnit single-dash invocation then reproduced precisely two failures against original production (14 passed), both proceeding to six destination reads. After the one-line production guard, all 16 focused cases and all 349 Contracts cases pass, zero failures/skips. The Debug source build has zero warnings/errors.
- A concurrent workspace operation created commit `6d36b3446e79607579bf4bee326adf4036fa960c`, capturing the initial spec/test draft and six unrelated dependency-pointer changes. Preserved this external commit and kept the canonical original baseline. The historical story-gitlink validator therefore reports six undeclared external pointer changes; these must not be relabeled as DW-588 work or reverted. Final validation will separately prove the task does not change any pointer relative to that external commit.
- The pre-code isolated Aspire start, describe, telemetry capture and stop succeeded. Memories resources were healthy, security unhealthy and dependent domain services waiting; this snapshot is not runtime or release qualification. Only the task-owned instance was stopped.
- Independent review found five fixture improvements, all patched: persist unexpected-call evidence outside suppressed stderr; execute the actual CI jq filter over mixed matching/nonmatching runs; initialize call logs and cover invalid-source refusal before any API; exercise an absent release with a present package/container and compare the downloaded package hash; seed and compare all retained recovery-state files. The review build has zero warnings/errors and the expanded focused suite passes 18/18 with zero failures/skips. The production guard remains one line.
- Independent recheck resolves all five findings without residual code concern. Final maintained Contracts tests pass 351/351, zero failures/skips; scoped formatting passes. Seven harmful mutations in a copied validator are rejected by the actual compiled suite, with a pristine 18/18 control. Only DW-588 closes: all 653 IDs retained, 369 done/284 open across 256 exactly covering maintenance rows; every original replaced field and unrelated record remains. The first accounting-checker assumption about legacy done dispositions was corrected without changing ledger data. See [exact commands and preserved attribution/runtime limits](dw-588-validation-evidence.md). The historical story-gitlink check remains failed solely on the six external pointer moves; the separate post-external-commit pointer diff passes.
- Final independent artifact reconciliation confirms the closure counts, all historical evidence and attribution limits with no residual concern. The pinned commitlint 21.2.3 validates the exact local commit candidate with zero problems/warnings. Marked done after review, owning-suite acceptance and scoped maintenance validation; no release readiness or full historical-range gitlink pass is claimed.

## Review Triage Log

- **medium / patch — hidden unexpected requests:** `api_status` discards stub stderr, allowing an unknown endpoint to masquerade as the expected unknown-status refusal. Added independent file-backed unexpected-call evidence and assert it is empty after every run.
- **medium / patch — CI evidence fidelity:** the original fixture returned a canned count for any jq expression. It now evaluates the production expression against exact-source success plus wrong-SHA/branch/event/status/conclusion controls; the no-CI scenario contains only nonmatches.
- **low / patch — early-refusal fixture:** a refusal before the first API left no invocation log. Initialize logs before starting Bash and add an invalid-source case asserting zero API calls.
- **low / patch — existing artifacts compatibility:** all original supported inventories used absent destinations, leaving existing-artifact classification untested. Added absent-release recovery with one present package and container, exact probe/download sequence, downloaded content and SHA-256 evidence.
- **low / patch — retained-state preservation:** the original fixture seeded only missing-packages. Seed source/version, existing/missing package inventory, count/hash and container/package states; compare every filename and exact content after refusal. No finding is deferred.
