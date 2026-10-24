---
title: 'Responsive command forms and reliable member focus recovery'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'fc0a6959844f6afde5a5141fe46c36f5a0cbc0ad'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five related deferred records: DW-227, DW-241, DW-242, DW-299 and DW-317. Focus the actual member Cancel and launching Fluent controls; restore a disappeared or unavailable launcher to the stable member heading; keep removal focus on visible controls at the existing 767/768px boundary. Repair isolated form and label selectors in configuration set/remove and lifecycle flows, and qualify computed layout and configuration warnings in real Chromium using actual localized production-rendered markup, compiled isolated CSS and restored Fluent assets. Preserve command admission, identity, authorization, retained proof and the existing breakpoint. Keep broader modal/inert redesign DW-194 and unrelated records open. Review, verify, commit and push these changes to main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation found no unresolved product choices, dependency updates or data migrations. This is a bounded correction to existing controls and styles, with no new public contract.
- Code map: `Components/Tenants/Members/RemoveTenantMemberFlow.razor` contains the Cancel wrapper and confirmation focus scheduling; `MemberAccessReview.razor` retains stale row wrapper references. Configuration set/remove and lifecycle `.razor.css` ordinary form selectors miss child-generated scope attributes. Existing UI component tests and `Browser/validate-tenants-focus-browser.sh` export real EN/FR markup and run Chromium against built CSS and Fluent assets.
- Local baseline: `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --no-logo` succeeded. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` reports Keycloak/security Running/Unhealthy and dependent services Waiting. Use the existing isolated browser lane; this does not establish authenticated live-topology health.

- Browser qualification caught a specificity interaction: anchoring ordinary configuration form selectors requires the same ancestor anchor in the existing narrow hide rules. Both hide rules now retain equal specificity and win in their media block.

- Member launch references now hold actual Fluent buttons, and current rows/action availability govern recovery. Missing/denied/detached launchers fall back to the stable focusable member heading. Removal targets actual Cancel, recovers after unsafe viewport transitions and uses lifecycle when Cancel is disabled; visible same-ID retry confirmation stays editable.
- Regression qualification covers current/missing/denied member rows, detached browser requests, both focus sentinels, measured Tablet/Phone/unmeasured states, real retained accepted/ambiguous delivery and retry correction. Browser fixtures qualify compiled form/label CSS at five widths, both configuration warnings, actual localized Cancel, native keyboard cycles and mutation controls.

- The maintained full UI lane exposed an existing configuration test race: a synchronous Submit wrapper was awaited instead of the actual async event. The test now awaits SubmitAsync, preserving its lease-cleanup assertions and the production implementation.

## Review Triage Log

1. medium, patched: anchored form selectors initially outranked narrow hide rules. Matching ancestor anchors retain equal specificity; real 390px checks exposed the regression before completion.
2. medium, patched: owned ambiguous delivery still allows visible confirmation correction. Removed the added owned-command condition and qualified real retained retry correction.
3. medium, patched: safe-to-Phone resize could drop hidden form focus. The open removal viewport callback now queues Cancel/lifecycle recovery; the transition is covered.
4. medium, patched: one reverse Tab did not qualify a cycle. The browser driver now executes complete native forward and reverse cycles at 767/768px.
5. low, patched: first-label checks missed distinct control shapes. Browser checks cover every set/lifecycle label, including the select, text input and textarea.
6. medium, patched: disabled Cancel fallback lacked a real owned-attempt control. Accepted and ambiguous retained-delivery tests qualify both sentinels, resize and original identity.
7. low, patched: member fixtures are English. Browser reporting now explicitly distinguishes EN/FR forms/removal from English member launcher/heading targets.

Independent recheck accepted all seven corrections and passed its 18 focused cases, with no remaining concrete defect among these findings. The parent final focused set has 20 cases, including the migrated legacy initial-focus case and the corrected configuration lease-cleanup test. No review findings were deferred.

## Verification

- Final Debug source-reference build: zero warnings and errors.
- Focused executable checks: 20 passed, zero failed/skipped.
- Maintained Microsoft.Testing.Platform UI project: 7,496 passed, zero failed/skipped.
- Final maintained Chromium browser lane: passed, including EN/FR compiled responsive layouts, actual Fluent controls, native keyboard cycles and required failure controls.
- Story gitlink validation: passed; no references/ pointers changed. Ledger audit preserves all 657 IDs and every original field, changes exactly five records, and leaves 560 done/97 open in 87 exact-covering maintenance rows.
- Exact commands, earlier failures, independent review, raw logs and scope limits: [validation evidence](dw-227-241-242-299-317-validation-evidence.md).
