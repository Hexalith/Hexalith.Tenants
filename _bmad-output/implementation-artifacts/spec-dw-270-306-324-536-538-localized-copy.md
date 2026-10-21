---
title: 'Finish French administrator and audit copy and qualify localized read-state hints'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: 'bf5d790749215cc8cd6f1271b0dd309a268fd0d7'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five deferred records: DW-270, DW-306 and DW-324's overlapping French accent/apostrophe corrections in the global-administrator and audit resource cluster, DW-536's exact localized unauthorized-detail title coverage, and DW-538's exact localized focus-return and focusless audit hints. Preserve resource keys, placeholders, authorization, return navigation and command semantics. Verify rendered English/French copy using shipped resources and literal expected text. Update only these five ledger dispositions after validation; the user explicitly requests committing and pushing the completed work to main.

</frozen-after-approval>

## Implementation Notes

- Small, reversible resource/test/evidence change; no intent gaps, data mutations or new public surface. Use the one-shot route. The five records share localization quality as their goal; the user explicitly requested five items.
- Reuse `CultureBoundTenantsLocalizer` for deterministic renderer culture. Existing detail and audit fixture registration permits overriding the localizer before rendering. Preserve existing denial and navigation assertions.
- Read repository baseline, Git and UX guidance, architecture AD-9/AD-11, localization acceptance spec, build configuration and test fixtures. Initial tree is clean on main at the full baseline above.
- Aspire startup succeeds; initial state has security Running and downstream services Waiting. Distributed health does not qualify the resource/component changes; focused and full maintained UI tests will.
- Corrected the full administrator/audit accent cluster. The editorial inspection caught and corrected candidate noun/verb substitutions before validation, including `demande`, `charges utiles`, `limites`, and finite `échoue`. Resource keys and placeholders remain intact.
- Added two real-resource detail-title cases and strengthened two audit-return cases into four EN/FR cases, preserving the denial and navigation assertions. Updated three existing French expectation literals after the first full run failed 43/6,882 on old copy; the repeated full run passes 6,882/6,882 with zero skips.
- Initial combined method filter using `|` matched zero cases and correctly failed the minimum-test policy. Three separate maintained filtered runs pass 2/2 each; no zero-test result is used as evidence.
- Independent review exposed nearby punctuation/wording inconsistencies within this same copy pass. Corrected the detail audit-launcher accent, semicolon spacing, domain-scope label, authorization descriptions, attribution terminology and French payload terminology. Final reviewed rebuild has zero warnings/errors; final full UI run is pending.
- Final reviewed UI run passes 6,882/6,882 with zero failures/skips; independent recheck confirms all six editorial findings resolved with unchanged resource keys/placeholders and preserved meanings. Closed exactly DW-270/DW-306/DW-324/DW-536/DW-538: 657 IDs retained, 415 done/242 open across 221 exact-covering index rows; unrelated records are unchanged. Gitlink and whitespace guards pass. Complete commands, initial failures and scope limits are in `dw-270-306-324-536-538-validation-evidence.md`.
- Final documentation recheck confirms counts, scope, preservation and final TRX accuracy with no findings. The separately required scoped formatter exits 2 on 122 existing diagnostics, all mapped to exact unchanged baseline lines and none on new/changed lines; its exact command and limitation are retained in the evidence. No formatting changes are applied.

## Review Triage Log

Blind Hunter's floor is six from 31.227 kB: `min(floor(sqrt(31.227) + 1), 10) = 6`. No behavior/schema defect was found. Each exposed editorial issue receives a simple correction within this copy pass:

1. **low / patch:** The adjacent detail audit-launcher label still uses `detail`; corrected its accent and apostrophes. This is the explicit one-key exception outside the administrator/audit prefixes.
2. **low / patch:** Nine removal/audit values omit French spacing before semicolons; normalized that punctuation without changing their words.
3. **low / patch:** The removal-preview scope label uses English `tenant`; aligned it with the grant label's `locataire`, preserving all three literal domain identifiers.
4. **low / patch:** Three authorization descriptions translate fail-closed implementation language literally; they now describe unavailable actions and the absence of revealed administrator data. Authorization predicates are unchanged.
5. **low / patch:** The action column and mobile description mix `accorder`, `octroi` and `attribution`; aligned those two descriptions with the attribution action.
6. **low / patch:** Two audit messages use English `payload`; replaced it with `charge utile`, preserving their no-data-disclosure meaning.
