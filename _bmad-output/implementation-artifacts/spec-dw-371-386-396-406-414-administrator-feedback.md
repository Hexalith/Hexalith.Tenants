---
title: 'Keep global administrator command feedback specific and qualified'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '406114bb131792e8411e16b9aac6885b3132444e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five related global administrator command-feedback records: DW-371, DW-386, DW-396, DW-406 and DW-414. Show the actual localization blocker and its recovery on removal and correction surfaces, reuse the immutable fixed removal-localization result, preserve recognized grant rejection reasons, and qualify unavailable tracked dispatch and localized rejection rendering.

Keep authorization, viewport, admission, last-administrator, tracking identity and projection confirmation rules intact. Reuse shipped English/French resource keys and existing BFF contracts where possible. Verify only these five closures, preserve every historical and unrelated ledger entry, review the changes, then commit and push main as requested. No dependency update or deployment is required.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent choice or irreversible implementation step. The user already authorizes commit/push. Footprint is the existing BFF contract/implementation, gateway, administrator page/correction panel and their tests, plus this spec and closure evidence. No new service or persistence behavior.
- Reuse the grant-specific BFF readiness reason/recovery pattern for removal, with the existing per-instance immutable localization cache. Preserve higher-priority authority, viewport and transport failures. Reuse recognized, support-safe rejection resources; unknown rejection text must remain hidden.
- Verify with the Debug source-reference UI test project build and affected existing test classes, then full UI acceptance. Record measured localization enumeration savings, exact commands, independent review and ledger preservation evidence before closure.

- Implemented readiness reason/recovery pairs for removal and correction, preserving existing canonical reasons and refusal precedence. The existing immutable per-composition cache now serves removal composition; 100 valid previews require exactly two resource enumerations in either call order. Target/last-administrator evidence is still evaluated per preview.
- Recognized grant rejection codes use shipped command-neutral EN/FR keys on submission and fixed-aggregate status. Unknown codes retain generic safe copy. Review identified the newly consumed keys were absent from fixed grant readiness; all four are now required and eight missing-culture regressions cover the failure.
- Initial new test fixtures needed a two-argument accepted-result factory, captured Fluent callback instead of a removed DOM node, and removal reason slots instead of a disabled button. Two old authorization tests expected the generic grant key. All corrected while preserving original assertions. First corrected affected runs: correction 151, page 274, gateway 774 and BFF 101 pass; later review additions require a final BFF/full run.
- Scoped whitespace verification reports 112 inherited diagnostics in the existing page-test file and one introduced moved initializer; that initializer is corrected. Existing formatting is preserved outside changed lines. Debug builds and git whitespace/gitlink validation remain the acceptance checks.

## Review Triage Log

Blind Hunter: N = min(floor(sqrt(37.435 kB) + 1), 10) = 7. All seven findings are verified in-scope gaps with simple patches; none deferred.

1. **medium / patch:** Newly consumed rejection resources were absent from fixed grant readiness. Require all four shipped keys and reject missing EN/FR values in eight cases.
2. **medium / patch:** An initially disabled click did not qualify stale confirmation. Capture the actual Fluent callback while enabled, withdraw the applicable tracked capability, invoke it, and assert zero dispatch/lease; retain initial-false controls.
3. **low / patch:** Localization/transport and localization/viewport precedence lacked combined cases. Ambiguous tracked restore/revoke now require the effective higher-priority reason/recovery in both cultures.
4. **low / patch:** Correction's own pair validation lacked malformed-pair cases. Both actions and cultures now exercise null reason, empty recovery and whitespace reason, requiring the canonical paired fallback.
5. **medium / patch:** Correlated refresh withdrawal lacked direct coverage. Accepted tracked commands now retain identity/correlation/lease, disable refresh and issue no additional status reads after capability/localization withdrawal.
6. **low / patch:** Echoed missing values did not qualify explicit-culture absence. A French enumeration omits a required resource while its indexer returns a resolved English fallback; composition/readiness still refuse safely.
7. **low / patch:** Failed cache tests lacked composition-first controls. Both failure cultures now initialize through readiness and composition and retain one fixed scan across subsequent preview/readiness/diagnostic reads.

- Independent recheck resolves all seven findings by inspection and finds no concrete introduced defect. Reviewed correction 183/183 passes. Reviewed BFF 111/112 exposes its existing exact key-count expectation (104) after adding four required keys; update to 108 preserves the complete-resource inventory. Review-test parameter expansion initially hit an incorrect test block and failed compilation; corrected targeting restores zero-warning/error build.
- Final scoped formatter has 112 inherited diagnostics and zero diagnostics on introduced lines; repository whitespace and story-gitlink checks pass. No formatting gate or build policy is changed.

- First full acceptance 7114/7115 (zero skips) exposes the adjacent distinct-key count still 104 after the total-count correction. Update it to 108, retaining the uniqueness assertion, and preserve the first full TRX/log separately before final rebuild/recheck.

- Final accepted BFF 112/112 and full UI 7115/7115 pass with zero failures/skips; final Debug source build has zero warnings/errors. Independent recheck clears all findings. The ledger closeout preserves all 657 IDs and all historical fields, leaving 430 done/227 open across 206 uniquely exact-covering maintenance rows. Only the selected five records close; archive, sprint, dependencies and release holds stay intact. Exact commands and initial failures remain in the companion evidence.
