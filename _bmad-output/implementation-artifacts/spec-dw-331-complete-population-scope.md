---
title: 'DW-331 reject contradictory complete-population scope'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: 'e44ac7f537e9f192605e24efc3e61a97d00d44f3'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The public global-administrator removal availability evaluator accepts otherwise qualified, nonempty complete-population evidence whose `CompleteIsAuthorizationScopedEmpty` flag is true. This contradictory evidence must not authorize removal, even though installed bounded loaders already reject it.

**Approach:** Reject this contradiction through the existing `IncompletePopulation` outcome. Preserve grant independence, common safety-gate precedence, valid removal, last-administrator protection and existing localized reason/recovery identifiers. Add direct behavioral regressions, then reconcile only DW-331 and its maintenance index row after verification and independent review.

</frozen-after-approval>

## Implementation Notes

- Entry: clean `main`, HEAD `e44ac7f537e9f192605e24efc3e61a97d00d44f3`. This is freeform deferred maintenance, not a sprint story; sprint tracking and archive remain unchanged. No staging, commit, push, branch or dependency mutation is authorized by the user-provided repository instructions.
- Route facts: no unresolved intent gaps; no irreversible or external product changes; small footprint limited to one evaluator, its existing test class, this spec, evidence and the two backlog documents. Story 4.1 already requires unsafe population evidence to fail closed.
- Code map: `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs`, `EvaluateRemove` complete-population rejection; add the missing scope check there. Reuse `GlobalAdministratorActionUnavailableReason.IncompletePopulation` and its current reason/recovery keys. `EvaluateGrant` remains independent of complete-population proof.
- Test map: `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`, `ReadyEvidence()` and the existing prerequisite-precedence theory. Prove otherwise valid contradictory evidence is unavailable for removal with canonical action/reason/recovery; prove grant remains available and population refusal precedes missing preview. Existing valid-removal and last-administrator tests are controls.
- The bounded `GlobalAdministratorsProjectionLoader` and installed page/correction consumers require no edits. Keep the evaluator pure and evidence immutable; do not change components, resources, authorization policy or lifecycle ownership.
- Verify a failing focused regression before the fix, then build the UI test project in Debug with source references and warnings as errors; run the focused class and full UI project through maintained Microsoft.Testing.Platform. Run the story-gitlink validator and whitespace check. Preserve exact commands/results and any failures in `dw-331-validation-evidence.md`.
- Required pre-edit Aspire inspection uses the repository Aspire skill and an isolated Tenants AppHost, leaving other running hosts untouched. Entry hashes and canonical baseline are retained in `/tmp/dw-331-entry-state.json`.
- Implementation: added the missing complete-scope rejection in `EvaluateRemove`; added a direct contradiction test with exact existing reason/recovery keys and independent grant assertion, plus a prerequisite-precedence theory case. Before the fix the focused class had 24 passes/two failures; after the fix it passes 26/26. Both Debug source builds pass with zero errors and the existing two NU1903 advisories. Full UI verification is running; exact commands and limits are in `dw-331-validation-evidence.md`.
- Pre-edit Aspire final inspection: all 23 Tenants resources Running/Healthy; isolated validation host stopped. Story-gitlink and whitespace checks pass; C# checkout CRLF is preserved.
- Pre-review full UI passes 5,981/5,981 with zero failures/skips. Independent review requested additional refusal-precedence acceptance proof: five common gates and missing-target/last-administrator cases now cover the contradictory flag directly. The evidence document includes the full-suite commands and clarifies two warning occurrences refer to one existing advisory. Reviewed verification remains pending; see the evidence document for exact commands and artifacts.
- Review recheck confirms both test-coverage findings and full-suite command evidence resolved, with no new code regression. Correction to the earlier implementation build note: “two NU1903 advisories” means two warning occurrences for one existing NU1903 advisory, GHSA-334f-vvp4-7g6p; the earlier phrasing is retained as historical notes, not an assertion of two distinct findings. Reviewed build passes and focused suite passes 33/33, zero failures/skips; the reviewed full suite is running.
- Final validation supersedes the earlier pending notes: reviewed build passes; focused33 and full UI5988 pass with zero failures/skips. The first reviewed full run had one intermittent unchanged correction-test failure (5,987 passed/one failed); its isolated class passes118 and unchanged full recheck passes5988. Exact commands, failure, logs, reports and limits remain in `dw-331-validation-evidence.md`. Only DW-331 closes; all643 IDs remain340 done/303 open across271 exact-covering index rows. Archive, sprint, unrelated ledger records, build/configuration, dependencies and existing release holds are preserved. No commit or other Git mutation was performed, following the user-provided repository instructions.

## Review Triage Log

- `medium / patch`: Common-gate precedence was stated in intent but tested with contradictory complete-scope evidence only when common gates passed. Added five reachable public-evidence combinations proving permission, visible freshness, viewport measurement, lifecycle support and busy admission retain precedence for removal and grant. No runtime ordering change was needed.
- `low / patch`: Population refusal precedence over missing target and last administrator lacked direct contradictory-scope coverage. Added two cases that would fail if the new population check moved below those decisions; both require `IncompletePopulation` and unavailable removal.
- `low / patch`: The spec referred to exact full-suite verification commands absent from the initial evidence document. Added pre-review and reviewed commands, count floors, artifact paths and explicit pending reviewed status.
- `low / patch`: Two warning occurrences were described as two advisories although both identify GHSA-334f-vvp4-7g6p. Corrected the evidence wording to one existing NU1903 advisory emitted twice. Initial build results and audit policy remain preserved.
