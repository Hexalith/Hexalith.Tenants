---
title: 'Qualify five deferred command recovery verification gaps'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
baseline_commit: '772fd4d8fb995c95efea5288edcc9d06b6cfe5f0'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-253, DW-261, DW-284, DW-475 and DW-512 as one command recovery verification change. Drive malformed Accepted tracking responses through real component gateway paths, qualify create message identity retention on indeterminate delivery, and dispatch then remount a create flow using the production scoped tracker. Replace source-only denied-audit wiring checks with rendered coverage across all seven affected flows, including positive controls. Make correction completion render the outcome atomically retained by EndDelivery so a wrong outcome cannot be concealed by a second tracker update, and qualify both successful and unavailable delivery outcomes.

Preserve existing authorization, freshness, projection confirmation and exact retry boundaries. No backend state, runtime deployment, dependency upgrade, new public contract or release acceptance change is included. Preserve historical ledger evidence, unrelated open items and the existing .mcp.json edit. Independently review and validate the scoped change, then commit and push it to main as requested.

</frozen-after-approval>

## Implementation Notes

- The user delegated five deferred items and authorized publication on main. Selected one cohesive command recovery verification goal. The existing local .mcp.json change is unrelated and will remain untouched and unstaged.
- Read Hexalith baseline/Git/UX instructions, project architecture AD-1 through AD-14, format/build/test configuration and all five ledger records. Scope is local component/gateway verification plus a small correction rendering change; no distributed AppHost operation is necessary to establish these behaviors.
- No unresolved user-facing intent gap or irreversible implementation operation; use the oneshot route. Publication to main is already authorized. Preserve all existing production command policies and use the actual gateway, service registration and rendered component seams.
- Code map: EditTenantMetadataFlowTests.cs (replace private snapshot injection for tracking loss); TenantCommandGatewayTests.cs (create minted identity on indeterminate failure); RemoveTenantMemberFlowTests.cs (accepted/lost tracking submit); CreateTenantFlowTests.cs (real dispatch/unmount/remount); AuditAvailabilityStateTests.cs and reusable flow fixtures (seven rendered denial/allowed cases); CorrectionStartPanel.razor and its component tests (consume EndDelivery's retained snapshot).
- Validation: build the UI test project in Debug with source project references; invoke its built assembly for focused classes and then the maintained UI suite. Use isolated wrong-outcome/source controls where practical. Preserve exact command/results and blockers in a tracked evidence report. Ledger conservation must retain all 657 IDs, close exactly these five open items, preserve all unrelated record bodies and keep maintenance open-row coverage exact.

- Runtime baseline: Aspire CLI inspection found the existing AppHost running with all 23 resources healthy. No start/restart or AppHost edit was needed. The direct assembly runner supports single-dash filters; builds are Debug source mode with no warnings/errors. Initial fixture errors and their corrections are recorded in the evidence report. The real renderer-remount cases now pass 2/2, and both wrong correction outcomes and seven bypassed audit guards are rejected.

- Independent review identified ten verification/evidence improvements. Corrected the checker for portable versus explicit local-edit validation and complete historical narrative conservation, qualified existing TrackingLost branch coverage with a six-failure deletion control, expanded audit authority to indeterminate/revoked cases, pinned correction outcome fields before a held status read, and checked independent circuit admission. The existing create held-delivery matrix supplies complementary async coverage without duplicating it in the production-registration fixture.

- Final full-suite testing exposed a render-bound wait for an off-render status-entry event in the new held test. Replaced it with an explicit task-completion signal from the test gateway, preserving inspection before the status response and all expected-field assertions. No production change or timeout relaxation was needed; the initial failure is preserved in evidence.

- Final reviewed Debug source build has zero warnings/errors; focused acceptance passes 1578/1578 and full UI passes 7158/7158 with zero failures/errors/skips/not-run. Wrong-outcome, audit-denial/revocation and TrackingLost-removal controls reject; source bytes are restored. Independent recheck accepts all corrections, rejects the held-delivery finding based on existing complementary coverage, and reports no remaining concrete issues. All 657 records remain with exactly five closures (480 done/177 open across 167 exact-covering rows); source/run hashes, pinned commitlint proof and historical fixture failures are retained in evidence.

## Review Triage Log

Blind Hunter: changed content 81.808 kB; N = min(floor(sqrt(81.808) + 1), 10) = 10.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| 1. Checker depends on dirty MCP bytes | medium / patch | Default validation uses committed evidence. Explicit --verify-local-edit checks the original workspace edit; the run manifest retains its digest. |
| 2. New removal theory does not hit TrackingLost | medium / patch | Evidence now attributes owned-attempt prevention to the new gateway cases and TrackingLost to six already-present defensive tests. Removing the exact TrackingLost block fails all six. No natural orphan-state route is claimed. |
| 3. Conservation omits historical narrative | medium / patch | Compare the entire original ledger preamble and maintenance document after only the declared summary/row/count changes; two historical-summary deletion controls reject. |
| 4. Audit matrix couples denial and availability | medium / patch | Add seven indeterminate cases with both flags false, requiring a populated disabled control and no link. |
| 5. Audit matrix lacks mounted revocation | medium / patch | Each allowed case rerenders the original cascades to denied, proves the same child instance loses its shell/link and preserves command/status counts. |
| 6. Correction outcome fields are under-specified | medium / patch | Pin audit state, message key, rejection code, focus and announcement values for every outcome, alongside retained identity and admission. |
| 7. Automatic status can obscure accepted handoff | medium / patch | Hold the status task and assert Accepted/correlation and all delivery fields in both component and tracker before releasing status. Then qualify the distinct pending-status key/focus. |
| 8. New create fixture lacks deferred-delivery remount | false / reject | Existing TenantRetainedCommandFlowTests.HeldDeliverySurvivesReplacementBeforeOrAfterCompletion includes create before/after delivery variants, preserving identity/intent/baseline/timestamp, one dispatch and admission. It complements the new real renderer-unmount/production-scope fixture and runs in final focused acceptance. |
| 9. Independent scope omits its gate | low / patch | Check a distinct unlocked gate, independent tenant acquisition/release and unchanged original-circuit admission. |
| 10. Evidence uses a failing focused run filename | medium / patch | Record the exact final accepted assembly command in the report and manifest; retain earlier failed fixture runs under historical evidence. |
