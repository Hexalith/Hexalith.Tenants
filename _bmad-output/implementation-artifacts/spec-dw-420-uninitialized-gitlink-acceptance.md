---
title: 'Reconcile uninitialized legacy gitlink acceptance for DW-420'
type: 'chore'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '5f18497fa034d7cd9a45422a507eb0bbc9fc6ab6'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-420 still reports that an uninitialized dependency directory can make the legacy story gitlink guard accept the superproject HEAD or reject the actual unchanged dependency pointer. DW-656 already repaired this behavior and added regressions, leaving the remaining-work record stale.

**Approach:** Qualify the existing real-CLI regressions for empty/deinitialized dependency directories, stage-zero indexed pointers, real checkouts and explicit historical refs. Use isolated historical-code and mutation controls to prove the tests detect the original defect and incorrect alternatives. Close only DW-420 after successful verification and independent review, retaining its original fields and all unrelated records, dependency pointers, archive, sprint tracking and release holds.

</frozen-after-approval>

## Implementation Notes

- Selected the next self-contained, dependency-free item in the current gitlink maintenance work. DW-419 needs a separate baseline-ownership policy; cross-repository and CI-enforcement items are independently scoped.
- Investigation: `scripts/validate-story-gitlinks.py::current_pointer` already verifies checkout ownership and falls back to the exact stage-zero mode-160000 index entry. Explicit refs read the recorded tree. Commit `948141b48aa29af67caa8531d821be205096c46d` implemented this in DW-656; this run claims acceptance reconciliation only.
- `tests/scripts/test_validate_story_gitlinks.py::GuardCommandIntegrationTests` covers unchanged empty directories with table/arrow claims, valid/stale/parent-HEAD targets, staged net-zero changes, deinitialized directories, real checkout directory/gitfile forms, missing and ordinary directories, and conflicts. Preserve these existing production/test inputs.
- Route facts: no unresolved intent gaps or irreversible actions; four documentation files planned (this spec, dedicated evidence, selected ledger block and maintenance row). No new callable API or application change. The step-02 small-change route applies; no epic-story key applies.
- Entry tree was clean on `main`; canonical revision is above. Entry hashes and focused inputs are retained in `/tmp/tenants-dw-420-ip7yk35h`. Repository safeguards prohibit unsolicited staging/committing/pushing; leave the result uncommitted.
- Qualification completed: maintained guard suite 69/69; five focused methods pass. The historical-resolver transplant produces nine assertion/subtest failures across three methods; HEAD-tree fallback produces exactly two staged-pointer failures; unconditional-index resolution produces exactly one checkout-precedence failure. Harnesses enforce pinned inputs, exact failure multiplicities, summaries and no unexpected errors/skips.
- Updated only DW-420 and its maintenance row; original selected fields remain under `pre-dw-420-*`. The reproducible byte-level check passes: 656 IDs, 386 done/270 open, 242 index rows with exact one-time coverage. Archive, sprint, dependencies, production/tests, staging and HEAD remain unchanged. Dedicated evidence embeds both complete executed scripts.
- Review correction to the investigation note above: explicit refs other than the literal `HEAD` read a recorded tree; default and explicit `--ref HEAD` include the working tree. Frozen intent remains unchanged.
- Added only a temporary test-source overlay to stage/verify the `.gitmodules` declaration; its five focused methods pass, including empty/deinitialized and real-checkout cases. The first overlay attempt failed before tests with an indentation error, corrected before final qualification. Maintained sources are unchanged.
- Strengthened historical qualification beyond failure counts: both table/arrow parent-HEAD unsafe passes and valid-target rejection diagnostics must be present. A constant-pointer counterfeit reproduces the same nine failures but is correctly refused. The index-only mutation is explicitly attributed to its direct resolver assertion before its CLI cases execute.
- Integrity now enforces exact current closure field values and both links, plus the complete four-file changed/untracked path set. Actual temporary-clone controls reject an unrelated tracked AGENTS.md edit and each of four missing metadata fields; the restored clone passes. All three complete scripts are embedded in the evidence.

- Final independent correction recheck extracted all three embedded scripts, verified exact source matches and reran qualification, integrity and temporary-clone corruption controls successfully. All six findings are resolved, none are deferred and no concrete issue remains. Final story-gitlink and whitespace checks pass; the spec is complete with the four planned documentation paths left uncommitted.

## Review Triage Log

Independent Blind Hunter review computed 25.455 kB of initial changed content: min(floor(sqrt(25.455) + 1), 10) = six findings. No other layers are configured.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Explicit-ref wording omits literal `HEAD` behavior. | Low; patched by append-only clarification and corrected evidence. | `check()` selects working-tree comparison when `ref == "HEAD"`; historical commit/ref wording now states the exception. |
| Original action requested an uninitialized declared submodule. | Low; patched qualification gap. | A temporary overlay stages and verifies `.gitmodules`; all five focused methods pass, including empty/deinitialized directories. No maintained test is changed. |
| Index-only rejection occurs before that method's CLI cases. | Low; patched evidence attribution. | The direct `current_pointer` assertion rejects the mutant; only the passing current run reaches its subsequent CLI cases and second checkout form. |
| Method names/counts alone cannot prove false-parent behavior. | Medium; patched developer evidence oracle. | The harness now checks unsafe parent-HEAD PASS and matching valid-target refusal diagnostics in both forms. The constant-pointer counterfeit has the same nine failures and is explicitly refused. |
| Integrity did not enforce complete four-file scope. | Medium; patched developer preservation check. | Complete changed/untracked paths must match the four planned documents. An actual unrelated AGENTS.md edit in a temporary clone refuses with Unexpected changed-file scope; the restored clone passes. |
| Current closure metadata could be missing while integrity passed. | Medium; patched developer evidence check. | Every expected field must appear once with its exact value and both resolution links must exist. Removing each of four specified fields in a temporary clone produces its matching refusal. |
