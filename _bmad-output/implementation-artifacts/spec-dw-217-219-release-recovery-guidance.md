---
title: 'DW-217/DW-219: Align release recovery guidance with authentic lineage'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '089b575a6c144902e7de871c4b21fd4fecfacb52'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Publication-preflight and contributor recovery guidance still recommends restoring deleted tags or adding a breaking-change footer without the producing-commit and reachability safeguards already required by release policy. The registry-floor diagnostic claims a guaranteed version collision without computing the proposed version.

**Approach:** Align recovery text in the publication-preflight script and contributor release-floor guidance with the existing authentic-tag-only policy: restore a missing tag only at the actual producing commit, reachable from the dispatched source; otherwise stop for separately reviewed release-lineage recovery. Explain that registry versions above the reachable tag floor leave lineage unproven and risk a collision, without claiming a computed proposal. Preserve all validation logic, floors, package inventory, protected-environment/source gates, four-component NuGet behavior and nonzero-revision handling. Reuse the existing real-shell governance tests and update only DW-217/DW-219 maintenance records after verification and independent review. Leave the separately tracked current-release-line/trigger corrections in DW-218.

</frozen-after-approval>

## Implementation Notes

- The existing approved release-floor spec, recorded DW-217/DW-219 next actions, and current workflow recovery branch settle intent. No intent gaps, irreversible operation, new API or dependency change is required. Small footprint: diagnostic/comment text in two existing release files, the contributor recovery paragraph, existing governance assertions, and scoped spec/evidence/maintenance records.
- Reuse `PackageGovernanceTests.RunPublicationPreflightAsync` for local below-floor/allowed-boundary behavior and `RunTagFloorGuardAsync` for actual workflow Bash with isolated gh/curl fixtures. Preserve their no-publication and exact-invocation controls. Keep the three-part authentic-tag and four-part nonzero-revision branches intact.

- Pre-code Aspire snapshot started and described the exact Tenants AppHost: security was unhealthy, domain services waiting, and Memories resources healthy. Stopped only that task-owned instance afterward. Distributed-runtime acceptance is not claimed or needed for this isolated operator-guidance correction.
- The Debug source test-project build passes with zero warnings/errors. Existing real-shell governance tests were extended before production changes: original text fails exactly two of 214 cases (212 pass, zero skips), demonstrating missing provenance-safe preflight advice and overclaimed collision diagnostics. Added a same-message publish-phase check and sparse-index control with exact external-call assertions; preserved existing allowed-floor, malformed-version, revision, reachability and fail-closed controls.
- After diagnostic/comment and contributor-paragraph correction, all 214 focused governance tests pass with zero failures/skips. Actionlint, shell syntax, whitespace-diff and the story-gitlink guard pass. Independent review and the full owning Contracts suite are pending; both ledger entries remain open until acceptance.
- The full owning Contracts suite passes 333/333, zero failures/skips. Independent review is still pending; focused and full MTP reports are retained under `/tmp/dw-217-219/`.
- Independent Blind Hunter review computed `min(floor(sqrt(15.405) + 1), 10) = 4` and returned four findings. Complete conditional sentences now replace fragment-only policy assertions, contributor checks normalize whitespace, and contributor guidance names publication-run/artifact/source/ancestry evidence plus the repository-maintainer issue destination. Exact invocations/results are preserved in [validation evidence](dw-217-219-validation-evidence.md); review corrections and final rechecks are pending.

- Independent recheck confirms every review finding resolved with no residual concern. Three harmful mutation probes each produce the expected single-test failure; the restored final tree passes all 333 owning Contracts tests, zero failures/skips. Only DW-217/DW-219 close: 653 IDs retained, 367 done/286 open and 258 exact-covering maintenance rows; original fields remain under `pre-dw-217-*` and `pre-dw-219-*`. DW-218, unrelated records, archive/sprint, dependencies and release holds remain preserved. The contributor formatter reports 172 unchanged-line diagnostics and zero on added lines; no unrelated formatting was rewritten. Complete commands, results and limits are in [validation evidence](dw-217-219-validation-evidence.md).

## Review Triage Log

- **medium / patch — conditional policy reversal:** fragment-only assertions allowed “only when” to be reversed without failure. Require the complete authentic-tag and fallback sentences in actual preflight output and whitespace-normalized contributor text.
- **low / patch — reflowed obsolete advice:** a negative assertion embedded one specific newline. Normalize contributor whitespace so the unsafe legacy recommendation is rejected independent of reflow.
- **low / patch — recovery handoff clarity:** the new paragraph named the conditions without explaining evidence or a contact destination. Add original successful run, exact source, artifact/version tie and ancestry proof, with the existing repository-maintainer issue route. Preserve nonzero-revision escalation.
- **low / acceptance documentation — reproducibility:** the exact-command evidence artifact was being generated during review. It now records build, original failures, corrected focused/full tests, lint, syntax, gitlink, formatter and Aspire commands/results and is linked above. Frozen intent and architecture remain unchanged.
