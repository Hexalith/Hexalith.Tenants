---
title: 'Correct French audit receipt accents and apostrophes'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '1f0f4edb6be91dd07028c0303a3f308841c3b414'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-439 records fifteen older French audit receipt values with missing accents or straight apostrophes alongside correctly accented copy. This makes the receipt inconsistent for French-speaking operators.

**Approach:** Correct only those French receipt values, using the established U+2019 apostrophe convention and preserving their meaning, resource keys and placeholders. Update the existing French partial-receipt expectation, verify shipped receipt rendering and EN/FR parity, qualify and close only DW-439 while retaining its historical finding, and push the completed changes to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry is clean main at `1f0f4edb6be91dd07028c0303a3f308841c3b414`. Selected DW-439 as the next actionable record after completed DW-437; intervening DW-438 awaits a support-safety decision. Existing lower-ID obligations remain unchanged.
- Investigation: all 52 `Tenants.Audit.Receipt.*` keys exist in both resource files, with 1,717 keys each overall. Fifteen French values need accents or apostrophes; twelve include ASCII apostrophes. The other 37 receipt values already follow the established convention. Existing component and parity tests cover receipt rendering, copy safety and EN/FR resource completeness.
- Planned footprint: `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`, one expected partial-receipt string in `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafetyBoundaryTests.cs`, this spec, validation evidence, DW-439 ledger record and its maintenance row. Reuse existing tests for this reversible copy correction; add no tests that merely mirror resource text. No unresolved intent gap or irreversible implementation work; the final Git push is expressly authorized.
- Keep whole resource strings, all placeholders, neutral English resources, other French keys, component behavior, dependency pointers, archive/sprint and release holds. Read the Hexalith UX baseline, project context, architectural copy-ownership/parity rules and build configuration. Use an ordinary fix branch for the commit, then fast-forward main for the requested push, satisfying tracked Git guidance.
- Verification: validate exact resource changes/keys/placeholders and original-field/other-record preservation; build the UI test project in Debug with source references, then run existing receipt, support-safety and composition/parity coverage. Run own-story gitlink validation and whitespace checks, validate the exact commit message through pinned commitlint, and obtain the required independent workflow review before closing.
- Independent review asks for three documentation clarifications: the fifteen-value count comes from the current resource investigation (the historical ledger gives examples); the exact changed keys are listed in [validation evidence](dw-439-validation-evidence.md); key/placeholder parity and manual French editorial inspection are distinct from the existing real-resource French receipt render/copy and support-safety tests. The frozen intent remains unchanged.
- Debug source build passes with zero warnings/errors. The initial full UI run has 6,283 passed, one failed and zero skipped: `CorrectionStartPanelTests.Remounted_request_sent_panel_uses_advanced_tracker_before_refreshing_status` expects Accepted after refresh but observes RequestSent at line 1986. This test and its production sources are untouched; no causal attribution or baseline-defect claim is made. A focused class recheck and full rerun will investigate this unresolved acceptance result.
- Final acceptance: the untouched correction-panel class recheck passes 118/118 and full UI rerun passes 6284/6284, both with zero failures/skips and no intervening code changes. The initial failure is retained in validation evidence. TRX inventories confirm 82 receipt, 293 support-safety, 91 composition and three parity cases pass in both full runs. Independent correction recheck confirms all review clarifications are addressed and no concrete issue remains.
- Closed only DW-439 with replaced historical fields retained. All 656 IDs remain, 395 done/261 open and 236 exact-covering maintenance rows. The grouped row retains DW-270/DW-306/DW-324; dependencies, archive/sprint, unrelated records and release holds are unchanged. Exact commit message passes pinned commitlint 21.2.3; origin fetch confirms no entry divergence.

## Review Triage Log

- Low / patch: attribute the fifteen-value count to the current resource audit, since the historical ledger lists examples. Clarified in implementation notes and validation evidence without changing frozen intent.
- Low / patch: make the fifteen values independently identifiable. Listed all exact key suffixes in linked validation evidence.
- Low / patch: distinguish resource completeness/editorial inspection from real French rendering. Named the shipped-resource French receipt render/copy and partial-receipt safety tests, separately from structural key/placeholder checks and neutral-double cases.
- Independent correction recheck: all three findings addressed; French corrections preserve meaning, keys and placeholders; the existing expected partial-state copy matches. No concrete unresolved issue or deferred follow-up.
