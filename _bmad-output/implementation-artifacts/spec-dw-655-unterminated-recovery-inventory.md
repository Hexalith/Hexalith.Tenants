---
title: 'DW-655: Verify the final unterminated recovery inventory entry'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: 'c9feedb6656d70c7271e851c844ee29bdfb28c3e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-655 records a verified recovery-publisher defect: an existing-package inventory's final ID is skipped when its line lacks a newline. Recovery can then publish without verifying that package's reviewed hash.

**Approach:** Process the final nonempty existing-package ID before publication, with the same missing-evidence and content-mismatch refusals as terminated lines. Qualify single-line and multi-line inventories, matching hashes and canonical/empty controls through intercepted publication tools. Close only DW-655 after verification and independent review, preserving historical evidence, unrelated deferred work and release holds.

</frozen-after-approval>

## Implementation Notes

- There are no unresolved intent gaps, irreversible actions or public API changes. The footprint is the existing Bash verification loop, its Contracts test harness and DW-655 specification/evidence/closure records. Work began on clean `main`; no remote publication or dependency update is required.
- Code map: `scripts/publish-partial-release.sh` must accept a nonempty final `read` result at EOF in the first inventory loop. Reuse its existing hash lookup, missing-hash/content checks and final source gate. `scripts/validate-partial-release-recovery.sh` emits canonical terminated inventories; leave the producer, missing-package loop, manifest, workflow and dependencies unchanged.
- Reuse `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs::RunPublisherAsync` with an optional final-newline flag. Preserve distinct fixture bytes, reversed per-ID hash records, exact hash/publication call sequences, diagnostics, process exits and unchanged-state assertions. Exercise the final ID with matching, omitted and mismatching hashes across package, container, release and mixed destinations; include absent-file refusals for single-ID inventories. Existing terminated and no-existing-package cases remain positive controls.
- Validation: pre-edit Aspire state/telemetry snapshot, Debug source build, direct-assembly focused publisher cases, maintained Contracts project, isolated entry-revision replay, shell syntax, scoped formatting, whitespace and story-gitlink/dependency preservation. Record exact commands and results in `dw-655-validation-evidence.md`. Preserve historical DW-608/DW-654 probes: the older optional probe deliberately expects the bug and is not a post-fix gate.
- After review, close only DW-655 in the ledger and remove only its maintenance index action row. Preserve superseded fields as `pre-dw-655-*`, all other record bodies, archive/sprint tracking and existing release qualification holds. Validate the exact Conventional Commit candidate with pinned commitlint before the workflow-required local commit; do not push.
- Implementation adds 20 regression cases and changes only the existing-package loop's EOF condition. The original publisher fails all 20 new cases (116 prior cases pass); the repaired publisher passes 136/136 focused and 500/500 maintained Contracts cases with zero failures/skips. Debug source builds have zero warnings/errors. Syntax, scoped formatting, Python compilation, whitespace and dependency/story-gitlink checks pass.
- `dw-655-check-variants.py` supplies a fresh-build isolated replay: current production passes 28/28, the exact entry publisher fails the intended 20 cases while eight compatibility controls pass, and a blanket unterminated-inventory refusal fails all six matching-hash controls while eight canonical/empty controls pass. Exact identities, mechanisms, exits, fresh reports and content hashes are checked. Full commands and scope limits are retained in `dw-655-validation-evidence.md`; independent review is next.
- All five independent review findings are addressed below. The replay now uses explicit exceptions under Python optimization, parses actual call arrays separately from expected/difference text, and records the copied executed assembly plus source/manifest hashes captured at fixture creation. Final-ID test selection derives from the actual manifest inventory. Six additional cases advance main during the final unterminated hash. The corrected replay passes current production 34/34, reproduces 26 intended baseline failures and rejects blanket refusal in six matching controls.
- Independent correction recheck verifies all five repairs and finds no remaining code/replay issue. Final focused 142/142 and maintained Contracts 506/506 pass with zero failures/skips; the corrected replay's Debug source build has zero warnings/errors. The optimization negative control exits 1 without summary output, and six expected-only publication-marker controls are rejected. Final evidence distinguishes the initial 136/500/28 totals from the corrected 142/506/34 totals.
- Only DW-655 closes: all 655 original IDs and unrelated record bodies remain, 374 done/281 open and 194 actionable across 253 exactly covering maintenance rows. Original DW-655 fields remain under `pre-dw-655-*`; the original finding, prior historical prose/evidence, archive/sprint and dependency pointers remain unchanged. The local closure checker verifies preservation and exact coverage. Pinned commitlint 21.2.3 accepts the full local candidate with zero problems/warnings. No finding is deferred, no push or live publication is requested, and release holds remain.

## Review Triage Log

- **medium / patch — checks disabled under optimization:** `assert` can disappear under `PYTHONOPTIMIZE`; qualification now uses explicit `require` exceptions for every gate. A deliberately bad production variant under optimization must exit nonzero without successful summary output.
- **medium / patch — expected calls could masquerade as publication:** Shouldly's message contains expected and actual arrays. The replay now parses only the actual array and checks publication there. An early-refusal control with publication markers retained only in expected/difference text is rejected.
- **low / patch — provenance capture race:** late repository-file hashing could label a prior copied assembly incorrectly during another build. Capture and verify hashes of the executed fixture assembly/publisher/manifest; check test-source stability across the build/replay.
- **low / patch — hardcoded final positions:** new negative fixtures now select the final ID from `existingIds`, whose contents derive from the manifest. Replay identities no longer depend on fixed numeric package positions.
- **low / patch — source advancement on the new EOF path:** six additional cases change live main during the final unterminated package's hash; exact calls include all hashes and the final source read, with no publication. The entry-revision replay fails all six through intercepted publication continuation.
