---
title: 'Repair historical story audit provenance for five deferred records'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '7ddb864cda5e942925b056391617eae4895c1253'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-461/467/478 describe one broken Story 5.4 audit: its baseline is not a Git commit. DW-133/365 retain historical tooling attribution already distinguished from runtime implementation by the owning stories.

**Approach:** Repair the baseline through independently verified creation history, retain the final historical endpoint and dependency inventory, and publish reproducible tooling attribution. Close these five records after review and verification, then commit and push to main.

## Boundaries & Constraints

**Always:** Preserve the malformed original as provenance. Accept correction only to the verified parent of the sole original creation, with canonical revisions, an explicit historical endpoint and no pointer-bearing event commits. Retain ancestry, identity and target checks. Describe unrelated historical pointers as range contents. Preserve all other ledger records and the five modified submodule checkouts, as authorized by the user.

**Never:** Exempt historical stories, narrow valid baselines, rewrite history, modify generated workflows/skills, move dependencies, change application behavior, or present historical tests as fresh runtime evidence.

## I/O & Edge-Case Matrix

| Input | Expected behavior |
| --- | --- |
| Unavailable original, preserved provenance, sole creation and exact parent | Validate corrected Markdown at final historical endpoint |
| Missing/false provenance, available original, wrong parent, ambiguous rename/recreation | Fail closed |
| Wrong/omitted target, later story-associated pointer work, pointer-bearing repair event including move/revert | Retain refusal |
| No repair metadata | Existing behavior unchanged |
| Original tooling commits | Distinguish range attribution from runtime ownership |

</frozen-after-approval>

## Code Map

All artifact names below are under `_bmad-output/implementation-artifacts/`.

- `scripts/validate-changed-story-gitlinks.py`: `audit_ref` rejects both the unavailable prior baseline and historical metadata mismatch. Reuse existing snapshot, ancestry and pointer checks.
- `scripts/validate-story-gitlinks.py`: production declaration/target guard remains authoritative.
- `tests/scripts/test_validate_changed_story_gitlinks.py`: disposable histories exercise the actual runner and guard.
- `5-4-audit-availability-state-recovery.md`: original baseline `a5ca6e3f548e89b28a37826be721d9ef9f7cd51a`; creation `8a24128989e348768173875ffb3c38bbd7461657`; correct parent `a5ca6e3886bf0623a1ff8da8cdfd6fade8516652`; final endpoint `4f426e591c85825eb41ec4b289a43e91f2085004`.
- `spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`: File List already qualifies tooling introduced by `2e61f57bda6379192007d1bc6fabbde61996b11d`.
- `spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`: already excludes tooling introduced by `5d6601edf357772c80f5c02c487742eace0739e2`; three original pushall manifests have identical blobs. Preserve both source stories unchanged.

## Tasks & Acceptance

- [x] `scripts/validate-changed-story-gitlinks.py` — add narrowly verified malformed-baseline correction; retain existing refusal paths.
- [x] `tests/scripts/test_validate_changed_story_gitlinks.py` — exercise accepted correction and every negative matrix row through committed snapshots.
- [x] `5-4-audit-availability-state-recovery.md` — retain original baseline, record corrected baseline/creation/endpoint and seven exact historical additions; qualify old guard claims.
- [x] `docs/story-gitlink-ci.md` — document the correction contract and limits.
- [x] `dw-133-365-461-467-478-validation-evidence.md` — retain Git provenance, exact commands/results, review and limitations.
- [x] `deferred-work.md` and `docs/maintenance-backlog.md` — close exactly five records, preserve old fields, reconcile open-item coverage.

Given the corrected real legacy artifact, when historical and committed-event audits run, then both pass with all seven additions inventoried.

Given forged provenance or hidden pointer work, when the actual runner validates committed snapshots, then it refuses; all existing regressions still pass.

Given original tooling commits, when paths/diffs/blobs are checked, then attribution is reproducible without claiming old commits were split or behavior newly fixed.

Given the entry revision, when final scope is compared, then all 657 IDs remain, exactly five close, 652 other record bodies remain unchanged and no dependency pointer is committed.

## Implementation Notes

- Parent verification: read the staged eight-file unified diff, reran production 77/77 and event 69/69 regressions, and executed the retained preservation checker. A temporary unreferenced staged-tree commit `880e1fac8e7cdb1a7f359fd545435d8c82223c75` passed the actual committed-event runner (two stories, zero failures). It changes no branch reference. Original tooling assertions cover the matrix attribution row; new committed-history cases cover all refusal rows. Independent review and final publication evidence follow.

The implementation handoff produces edits and local verification; the parent owns review, staged scope, commit and authorized push. Reuse the already rendered workflow; do not render another workflow.

Baseline investigation reproduces the invalid legacy-baseline failure. Existing suites pass: production guard 77/77; committed-event runner 49/49. Entry ledger: 629 done/28 open. Application/runtime qualification is outside this tooling/documentation change.

- Final review corrections completed: trace identities through event head and repository-wide imports; handle branch merges without double-counting creation; reject rewritten imports with competing Markdown origins. Eight initial follow-up regressions plus one rewritten-import regression bring the event suite to 78. The corrected multi-parent fixture uses its exact first parent and rejects the first-parent-only mutation. Parent final checks pass production 77/77, event 78/78, retained provenance/ledger assertions and whitespace. All fourteen review findings are recorded; no new work is deferred. The final commit/event/publication record is retained in the companion evidence.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH1: feature-branch creation counted again at merge | medium | patch | Parent reproduced a single real creation rejected as two because first-parent merge additions are counted as original creation. Count a merge addition only when absent from every parent; retain single-parent creation requirement. |
| BH2: import from outside artifact root hides original start | medium | patch | Parent reproduced docs/old-story.md renamed into the artifact root and accepted with the import parent. Correction provenance must discover repository-wide renames before selecting artifact identities. |
| BH3: later documentation-only edits allowed after endpoint | low | patch | Existing endpoint policy refuses later story-associated pointer work, not all later prose. The real chosen endpoint is final through entry. Clarify this distinction in documentation rather than change ordinary endpoint policy. |
| BH4: multi-parent creation regression missing | medium | patch | Existing merge test concerns event pointers; it does not protect the new single-parent creation check. Add committed two-parent creation refusal. |
| BH5: overlapping identity regression missing | low | patch | Ambiguous rename cases exist, but no explicit history asserts refusal with overlapping known identities. Add a focused committed-history refusal case. |
| BH6: additions/removals correction regressions missing | low | patch | Current real audit proves seven additions; new disposable correction fixtures update one pointer. Add negative and positive addition/removal cases protecting target inventory. |
| BH7: final review/publication evidence pending | false | reject | This is the in-review staged artifact, not a published completion claim. The handoff explicitly leaves final review, commitlint and final event results for the parent to fill before publication. |
| BH8: preservation assertions overstate content evidence | low | patch | The retained assertions prove checkout HEAD revisions and superproject pointers, not complete submodule index/worktree/untracked snapshots. Narrow those evidence claims to the measured revision/pointer facts. No submodule content was edited by this task. |
| BH9: direct historical command missing beside metadata instructions | low | patch | Metadata is interpreted by event runner only. Document the direct production validator command with its explicit historical --ref to avoid a misleading current-HEAD replay. |
| EC1: deletion/recreation within repair event accepted | medium | patch | Parent reproduced a deletion commit followed by recreation within the event, both audited successfully because correction history stops at event base. Trace through event head and add refusal regression. |
| EC2: merged single creation refused | medium | patch | Same reproduced root cause as BH1; include the positive merged-history regression. |
| VG1: multi-parent creation check unprotected | medium | patch | Reviewer mutation replacing exact single-parent comparison with first-parent-only still passed all 69 tests and admitted a committed merge creation. Add a regression that rejects that mutation. |

- Follow-up review triage:

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| VG2: merge regression names wrong baseline | medium | patch | Recheck shows the new test passes the first-parent-only mutation because main advanced but corrected_story still supplies self.base. Capture the true first parent and use it in the correction, so only parent multiplicity causes refusal. |
| EC3: rewritten docs import loses Git rename link | medium | patch | Parent reproduced A artifact plus D docs/old-story.md accepted as original creation after enough content rewrites. Refuse ambiguous creation commits with a competing removed/renamed Markdown origin; keep the verified real creation eligible. |

## Verification

- Run both `tests/scripts/test_validate_story_gitlinks.py` and `tests/scripts/test_validate_changed_story_gitlinks.py` with Python; expect all cases passing.
- Run the legacy production guard with `--ref` selecting the full final endpoint above, followed by the committed-event runner on this isolated commit; expect exit zero.
- Verify historical diffs/blobs, ledger preservation, backlog coverage and unchanged submodules; run `git diff --check` and preserve successful pinned commitlint validation before committing.
