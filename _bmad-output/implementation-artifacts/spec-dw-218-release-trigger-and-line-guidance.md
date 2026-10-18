---
title: 'DW-218: Align release trigger and version-line guidance with committed evidence'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: 'b39f1716177788b8e068711f9674560d51a94e0e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Contributor and agent guidance still describes automatic publication on merge through `workflow_run`, a current 4.x line and automatic repository changelog updates. The committed workflow instead requires manual dispatch, and recorded reachable release history extends through 5.x. These stale instructions mislead contributors and operators.

**Approach:** Correct the release guidance in `CONTRIBUTING.md`, `_bmad-output/project-context.md` and the publication-preflight comment using the committed workflow/configuration and date-bound local tag evidence. Describe explicit dispatch from the current main tip, exact-source successful push CI, protected production publication, the final source recheck and published-source verification. Distinguish the unchanged `4.0.0` static minimum from recorded reachable `v5.8.0` history, without claiming current registry publication or release readiness. Label old consumer/recovery observations as historical and point to GitHub Releases for generated notes; preserve the repository changelog history and authentic-lineage recovery guidance. Close only DW-218 after focused validation and independent review, preserving every other ledger record, archive, sprint state, dependency pointer and release hold.

</frozen-after-approval>

## Implementation Notes

- Selected DW-218 as the next cohesive deferred item following the completed DW-215 through DW-219 release-guidance work. The recorded next action and actual committed release configuration settle intent. There are no intent gaps, irreversible operations, API changes or dependency changes; the footprint is documentation/comments and scoped workflow/maintenance evidence.
- Entry revision: `b39f1716177788b8e068711f9674560d51a94e0e`. Investigation reused the existing governance suite and an independent read-only agent. `.github/workflows/release.yml` is dispatch-only; `.releaserc.json` omits changelog/git plugins. The highest locally available merged release tag is `v5.8.0`, peeled to `032573384d3df4bc7a5bc0e69945ecca4e00967f`; this is repository evidence, not registry qualification.
- Keep `minimum_release_version=4.0.0`, package inventory, executable guards and recovery conditions unchanged. Do not rewrite historical artifacts. Validate the documentation against configuration/history, run the existing owning governance tests, whitespace and story-gitlink checks, and verify exactly-once maintenance-index coverage and preservation of unrelated records. No new test code is needed for this reversible guidance correction.
- Corrected current guidance in the contributor guide and agent project context, labeled historical consumer comparisons, and clarified the preflight comment without changing executable shell lines. The first story-gitlink check refused the template's missing `baseline_commit`; added the canonical entry revision to enable attribution, then rerun the guard before acceptance.
- The first full existing Contracts run failed exactly one of 333 cases: its floor test required the obsolete contributor sentence “Nothing at or below 4.0.0 can be released again.” Updated that existing assertion/comment to require the documented static minimum and other publishability checks, retaining all real-shell boundary and provenance cases; no test was added. The required pre-C# isolated Aspire snapshot started and described this task's exact AppHost, showing unhealthy security, waiting domain services and healthy Memories; telemetry was captured and only that instance stopped. This is not runtime qualification. The Debug source test-project build passes with zero warnings/errors.
- Planning-note correction: the completed predecessor records are DW-215, DW-216, DW-217 and DW-219. The earlier shorthand “DW-215 through DW-219” wrongly included the still-open DW-218; DW-218 is this spec's sole closure target. The full Contracts rerun passes 333/333 with zero failures/skips. Scoped formatter verification reports 172 unchanged-line diagnostics and none on added lines.
- Independent review returned four findings. Added operator guidance for a stale dispatch source and a no-releasable-commits result, narrowed the preflight comment to ancestry/numeric coverage without production-provenance proof, and reduced the existing assertion's dependency on complete prose sentences while preserving actual boundary/provenance controls. The inaccurate predecessor shorthand is corrected explicitly above without rewriting the append-only notes.
- Independent recheck confirms all four findings resolved with no residual concern. Final Debug source build has zero warnings/errors and the post-review full Contracts suite passes 333/333, zero failures/skips. Only DW-218 closes: all 653 IDs retained, 368 done/285 open across 257 exactly covering index rows. The original fields remain under `pre-dw-218-*`; unrelated ledger bodies, archive/sprint, release holds, executable guards and dependency pointers are preserved. See [exact commands, initial failures and limits](dw-218-validation-evidence.md).

## Review Triage Log

- **low / patch — operator outcomes:** manual-dispatch guidance omitted stale-source and no-releasable-commit outcomes already represented by the workflow. Added exact-source-CI redispatch instructions and explained why an unchanged-source no-release retry does not help; independent recheck accepts the correction.
- **medium / patch — proof overstatement:** the new comment said the tag-floor guard establishes lineage, but it proves ancestry/numeric coverage and does not attest the producing commit. Narrowed the comment explicitly; authentic-provenance recovery safeguards remain unchanged and independent recheck accepts it.
- **low / patch — prose coupling:** the corrected existing fixture initially pinned complete documentation sentences. It now checks the configured floor token and static-minimum role, preserving all executable boundaries and original provenance checks; no new test was added. Independent recheck accepts it.
- **low / patch — predecessor attribution:** an agent-owned planning shorthand mistakenly included DW-218 among completed predecessors. An append-only correction enumerates DW-215/DW-216/DW-217/DW-219, preserving the frozen intent and previous notes. Independent recheck accepts the correction. No findings are deferred.
