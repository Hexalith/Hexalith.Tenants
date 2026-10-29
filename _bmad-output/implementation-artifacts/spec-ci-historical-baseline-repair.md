---
title: 'Verify historical baseline repairs from committed creation evidence'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
baseline_commit: '1d8c31d2abe809829e10ce1ddadc978c79489f3e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="explicitly authorized guard-policy repair">

## Intent

Repair the CI provenance policy blocking `spec-global-admin-projection-paging.md` through a general, independently verified historical-baseline correction. Retain the rejected baseline as evidence. Permit an available rejected commit only when Git proves it is not an ancestor of the historical endpoint, the sole original creation records the corrected baseline as `baseline_revision`, that baseline is creation's exact single parent, and the endpoint retains both the rejected baseline and recorded start. Reject conflicting creation metadata. Keep the existing unavailable-object correction and all ancestry, canonical-ID, literal identity, rename, recreation, historical-target inventory and gitlink-bearing-event checks.

Changes are limited to the CI guard, regression tests, documentation, separate workflow evidence and paging provenance/completion metadata. Preserve existing worktree changes, paging implementation, completed Story 5.7 behavior, unrelated work and current gitlinks. Do not edit the deferred ledger, commit or push. Verify positive and adversarial committed-event fixtures, actual repository history, existing CI regression lanes and paging completion gates. Mark paging done only after every applicable gate passes.

</frozen-after-approval>

## Implementation Notes

- Plan: no intent gaps or irreversible actions; four maintained files plus this separate spec and evidence. Internal verification changes no public API.
- Code map: `scripts/validate-changed-story-gitlinks.py::verify_baseline_correction` already independently walks all identities and verifies sole creation; retain that logic. `audit_ref` retains event-base ancestry and endpoint/later-pointer checks. `tests/scripts/test_validate_changed_story_gitlinks.py` exercises both runner and production guard in disposable committed histories. Update `docs/story-gitlink-ci.md` to distinguish unavailable-object correction from recorded-start repair.
- Actual history: creation `7716f0b11423eb54d74935b6cc6e3edf405dc400` records only `baseline_revision: 6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`, equal to its sole parent. Endpoint `ded413dfd4786983585c7f56a399825cc1afbae3` retains that revision and rejects baseline `23b2a7691ae667fd160a48a5d7293f3cf38d48fe`; the rejected object is an available commit outside endpoint ancestry.
- Required evidence: fresh independent blind, edge-case and verification-gap reviews; exact commands/results; read-only actual-history preflight plus committed push/PR fixtures using repository objects. Record preservation hashes. Existing paging reviews/tests remain context; rerun its explicit build/full UI test/historical audit gates before completion.
- Initial verification: 96 changed-story and 77 production-guard tests pass; attachment/default-branch lanes pass 13/37. Paging build passes with zero warnings/errors. First UI run: 8,052 total, one preexisting deadline-test timeout; isolated two cases pass, full rerun passes all 8,052 (91.863 seconds), no errors/failures/skips/not-run. Preserve both results.

## Review Triage Log

All three independent review layers completed before classification. No findings deferred and no ledger edits.

- Blind B1 / Edge E1: high, patch. A natural transport clone omits the unreachable rejected object, so availability-based proof selection rejects identical committed evidence. Select recorded-start proof from the committed snapshots for both genuine availability states; still inspect available commits and reject valid ancestry. Add actual-history and maintained fresh-clone push/PR controls without removing or disguising source objects.
- Blind B2: medium, patch. The flat parser silently overwrites duplicate historical proof fields, allowing conflicting creation/endpoint evidence. Require unambiguous correction frontmatter and add adversarial committed snapshots.
- Blind B3: medium, patch. Unterminated creation frontmatter can supply baseline proof. Require a complete frontmatter block on correction evidence and add adversarial creation/endpoint fixtures.
- Blind B4 / Edge E2 / Verification V1: medium, patch. Both Git inspection-error refusals lack isolated regression coverage. Inject exit 128 for the exact commands, preserving genuine object availability and avoiding a second rejection masking the tested branch.
- Blind B5: low, patch. Inspection diagnostics omit the Git command, exit and stderr. Preserve those details through a simple message correction and assert them in the error fixtures.

### Completion — 2026-10-29

All three follow-up reviews closed their findings with no remaining blocker. The final changed-story lane passes 101 tests; the production guard and attachment/default-branch lanes pass 77/13/37. The paging build has zero warnings/errors and the final full UI run passes all 8,052 tests. Actual-history fixtures pass push/PR and later edits in both shared-object and natural transport clones; false creation and pointer move/revert events remain refused. Initial failures and exact final results are retained in [verification evidence](evidence/ci-historical-baseline-repair-2026-10-29/verification.md). No findings were deferred. Paging provenance retains the rejected ID and independently verified creation; its completion metadata is updated only after these gates pass. Existing UI test edits, all production/Story 5.7 code, ledger, HEAD and current gitlinks are preserved. No source commit or push was made.
