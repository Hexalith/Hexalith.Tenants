---
title: 'Reconcile recorded gitlink provenance acceptance for DW-526 and DW-531'
type: 'chore'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '6b62b6b307b10d5acaa86e5a722223bb34dd1b58'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-526 and DW-531 still describe missing recorded-provenance fixes and regressions that already landed with DW-643. The stale records misrepresent the remaining developer-tooling work.

**Approach:** Qualify the current CLI regressions for valid additions/removals, complete recorded chains and missing intermediate pointer commits. Independently remove each protection in temporary copies to prove the regressions detect its absence. Close only DW-526 and DW-531 after successful verification and review; preserve original record fields and all unrelated history, user edits, dependency pointers, archive and sprint tracking. This does not establish release readiness.

</frozen-after-approval>

## Implementation Notes

- Investigation: `scripts/validate-story-gitlinks.py` normalizes an absent baseline pointer to Git's zero SHA before chain comparison. `tests/scripts/test_validate_story_gitlinks.py` already covers recorded additions, renames, removals, complete multi-commit chains and omitted intermediate commits. Existing DW-643 evidence attributes these changes to its earlier implementation; this run does not claim to author them.
- Route facts: no intent gaps or irreversible actions; four documentation files planned (this spec, dedicated evidence, active ledger and maintenance index), with no new callable API. No application or test-source changes are needed. Therefore the step-02 small-change route applies.
- User authorized continuation with the dirty working tree. Entry revision is recorded above; a 3,685-file input hash snapshot and original ledger/index are retained under `/tmp/tenants-dw-526-531-udf5vz7w` for preservation checks. Existing contract/query edits and new tests are excluded from this work.
- No epic-story key applies. Repository Git-mutation instructions override the workflow's generic commit step; changes remain reviewable without staging, committing or pushing.
- Qualification: the maintained guard lane passes 69/69. The existing five relevant CLI cases pass unchanged. Removing only chain validation in a temporary copy fails exactly the missing-intermediate case; removing only absent-pointer normalization fails exactly valid addition and rename. Other focused controls pass in both mutations. No workspace source or test was replaced.
- Updated only the DW-526/DW-531 records and their maintenance rows, preserving every original field under the corresponding `pre-dw-*` key. Dedicated evidence includes complete reproduction scripts. Integrity checks pass: 656 IDs, 385 done/271 open, 243 rows, exact one-time index coverage, unchanged unrelated blocks, pointers, archive, sprint and staging.
- Independent review corrections: integrity now compares complete record bytes including headings and line endings; qualification enforces the recorded guard/test hashes. Four in-memory corruption controls prove both unrelated ledger changes and both changed inputs are refused. The corrected qualification and integrity scripts pass. Evidence distinguishes reproducible revision comparisons from the separate ephemeral entry-manifest check and records the existing endpoint-history limitation.
- Final independent recheck reran all three published scripts, the story gitlink guard and scoped whitespace checks successfully; all findings are resolved with no deferrals or remaining concrete issue. The spec is complete. Its embedded scripts match the executed copies exactly.
- Concurrent external edits appeared after the successful pre-review 3,683-file preservation comparison: five existing Contracts query files, `McpCliDecorationTests.cs` and `EnvelopePagingQueryHandlerTests.cs`. Final snapshot reconciliation records those seven changes, no unexpected new paths, 3,676 other unchanged entry files and unchanged HEAD. No workflow tool wrote those external files; they remain untouched. Ledger/index integrity, qualified guard/tests and dependency pointers still pass. The receipt is `/tmp/tenants-dw-526-531-udf5vz7w/preservation-final.json`.

## Review Triage Log

The independent Blind Hunter reviewed approximately 20.787 kB of initial changed content, yielding a five-finding floor. No other review layers are configured. Each finding was checked against the scripts and current workflow state:

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Integrity omitted headings and normalized line endings while claiming byte preservation. | Medium; patched. This weakened the developer evidence check. | Complete record blocks are now compared as bytes; separate unrelated heading and CRLF corruptions each refuse with `Unrelated record changed`. Normal integrity passes. |
| Existing user-edit preservation depends on an ephemeral snapshot. | Low; clarified. | The session-local check matches 3,683 unchanged entry files; evidence explicitly states its temporary-manifest limit and distinguishes it from the independently reproducible ledger check. No durable user-source snapshot is claimed. |
| Qualification did not enforce the published input hashes. | Low; patched. | Both SHA-256 inputs are required before controls run. Independently changing guard or test bytes refuses with `Qualified input mismatch`; corrected normal and mutation controls pass. |
| Missing-intermediate wording omitted restored-endpoint history limits. | Low; clarified. | DW-643 evidence and the existing restored-endpoint test preserve acceptance of intermediate moves restored to the recorded endpoint. Evidence now expressly limits the claim to the tested discontinuity. |
| Pending review/in-progress status conflicts with closed draft ledger entries. | False as a workflow defect; finalize after recheck. | The reviewer inspected during the prescribed review phase, when pending/in-progress were accurate. Final status and completed review evidence are recorded after corrections. `review_loop_iteration: 0` counts planning loopbacks; none occurred in this oneshot run. |
