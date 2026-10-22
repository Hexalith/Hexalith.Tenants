---
title: 'Qualify five deferred remove-administrator Cancel focus records'
type: 'chore'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
baseline_commit: 'ab6e99f656eaae14a01c021cf0c6eadf9f38cefe'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-345, DW-362, DW-378, DW-383 and DW-412 as one qualification of the remove-administrator Cancel focus contract. Export the actual production page's Blazor FluentButton markup, load its restored Fluent browser implementation in Chromium, and trace both document and shadow-root focus after calling the shipped focus helper. Establish whether the reported inner-control false failure exists and correct it only if reproduced.

Preserve unavailable-target fallback behavior and verify harmful helper mutations fail. Add this qualification to the maintained browser lane, retain exact evidence, and close only these five records if the evidence settles their shared risk. An authorized component fixture supplies read and authorization state; this work does not claim live identity-provider authentication, end-to-end modal event handling, keyboard trapping, assistive-technology testing or release acceptance. Preserve unrelated history, dependency pointers and release holds. Independently review, validate, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- No unresolved user-facing choice or irreversible implementation operation. Small test-fixture/browser qualification uses the oneshot route; the user explicitly authorized publication to main.
- Read required Hexalith baseline, Git/UX instructions, project context, architecture constraints, build/format/test configuration and the five complete records. Aspire inspection found an existing AppHost with 23 Running/Healthy resources; the bounded Error log query returned an empty list. No lifecycle restart or dependency change is required for test-only work.
- Investigation map: production `GlobalAdministratorsPage.razor` renders Cancel through FluentButton with `RemoveCancelButtonId`; `tenantsFocus.js` checks the document's active element. Existing `GlobalAdministratorsPageTests` supplies authorized composition, projection, command and localization fixtures. `Browser/validate-tenants-focus-browser.sh` already exports actual component markup, loads restored Fluent JavaScript and uses bounded CDP capture. Reuse those seams and preserve all existing scenarios.
- Add one partial fixture-test file and one browser HTML harness; extend the existing browser runner to export/check the fixtures and run positive and harmful controls. Keep production code unchanged unless a real failure requires repair. Add tracked validation evidence and conserve all 657 ledger identifiers and unrelated record bodies while removing only the shared selected maintenance row.
- Initial build failed with CS0103 for the fixture's missing TenantDetail namespace import. Corrected that import. A premature focused command against the old executable returned zero discovered cases; it is explicitly excluded from acceptance evidence. The browser runner independently requires both nonempty exported fixture files.
- The first maintained Chromium run failed the pre-existing start-sentinel case before the new checks. Isolated actual-control capture showed direct helper success but no sentinel focus event: document.hasFocus() was false. Enabling CDP foreground-focus emulation in the shared capture driver restores real focus-event dispatch. The actual fixture now asserts foreground focus; this is a test-driver correction, with no production helper change. Isolated EN desktop direct and sentinel-contract checks pass; hidden/inert/missing targets each report false and focus the actual Fluent input fallback.
- Debug source build passes with zero warnings/errors. New localized fixture cases pass 2/2, and the full UI suite passes 7119/7119 with zero failures/skips. The initial old-executable zero-test invocation remains excluded.
- The foreground-emulated browser run reaches all four new EN/FR scenarios and rejects both helper controls. Full Chrome reports innerWidth=500 when asked for a 390px window; the new lane therefore explicitly requests/asserts measured 500px and 1024px widths instead of claiming a 390px viewport. This qualification does not cover responsive breakpoint boundaries. The observed Cancel focus chain is the FLUENT-BUTTON host, with no shadow active element; unavailable fallback reaches the Fluent input's native INPUT shadow control.
- The first foreground-emulated run passed all browser scenarios, but exited 2 at EOF because the running shell file was edited during execution to correct viewport accounting. That run is not accepted as a green lane. Shell syntax checks pass; a fresh run against unchanged final files supplies acceptance evidence.
- Final immutable maintained browser run exits 0: all original lanes, four actual localized Cancel cases and both new helper controls pass on Chrome 154.0.8037.57. The tracked run manifest retains the exact focus observations and file/test identities. The conservation checker verifies 475 done/182 open across172 rows, all657 IDs and all652 unrelated bodies; the production focus helper and dependency pointers are unchanged.
- Independent review returns nine findings. All receive small test/evidence corrections: literal-boolean assertions, native fallback INPUT focus, an export-time ancestor guard, independent sentinel/fallback mutations, disabled-target coverage, exact current ledger fields plus wrong/duplicate controls, an explicit allowed-path gate, actual browser-input identities, and durable sanitized result output. No production behavior change or new deferred item is required. Reviewed Debug build passes with zero warnings/errors; reviewed browser and full UI reruns follow.

## Review Triage Log

Blind Hunter: 68.805 kB; `N = min(floor(sqrt(68.805) + 1), 10) = 9`.

| Finding | Verdict / disposition | Checked evidence and correction |
| --- | --- | --- |
| 1. Success checks accept a truthy non-boolean | medium / patch | The browser used truthiness while Blazor consumes bool; both success paths now require literal true. |
| 2. Fallback host focus can pass without a native input | medium / patch | Require the actual Fluent acknowledgement shadow active element to be INPUT for every refusal, retaining the full focus trace. |
| 3. Dialog-only export omits production ancestors | medium / patch | Before export, the real rendered dialog must have no inert, hidden or aria-hidden=true ancestor (including itself). |
| 4. Direct-call controls mask later assertion gaps | medium / patch | Add a helper that first succeeds directly then reports false from the sentinel, plus a generated harness control that omits required fallback focus. Each must fail its own assertion. |
| 5. Actual disabled Cancel is absent from the matrix | low / patch | Add the disabled Fluent host to the unavailable-state matrix; existing production Cancel is enabled, and this simple control qualifies its disabled behavior. |
| 6. Conservation checker accepts a wrong disposition | medium / patch | Selected current fields must occur exactly once, status must be dated done, and disposition must be done. Wrong/duplicate disposition controls reject. |
| 7. Broad preservation claims exceed checked paths | medium / patch | Enforce an exact allowed-change path set against tracked and untracked paths, excluding production, archive, sprint and release files. |
| 8. Browser inputs are not bound to retained hashes | medium / patch | The runner emits hashes of its exact exported EN/FR dialogs, loaded CSS and runtime UI assembly; retain these alongside test-assembly/source hashes. |
| 9. Temporary log hashes cannot preserve raw results | medium / patch | Retain sanitized raw command output in a tracked transcript; keep TRX counters/hash while explicitly identifying the complete temporary per-test TRX as local evidence. |

- Independent recheck clears all nine original findings and finds one new provenance error: a shared Python dictionary reference had updated the historical hash map along with the reviewed current map. Verdict: medium / patch. Restore the exact complete initial map from the independent reviewer's retained first-review observation, keeping current hashes separate. Do not reconstruct or invent historical assembly identity.
- A concurrent external `.mcp.json` edit appears after the clean entry snapshot. The first exact-path conservation check correctly rejects it. Preserve its observed bytes (SHA-256 recorded by the checker), prohibit staging that file, and permit no other unexpected path. This work does not author, reset or include that configuration change.
- Reviewed browser lane passes all 24 observed focus states and four harmful controls. One unchanged metadata case failed in the first reviewed full run (7118/7119); both isolated cases pass. The final separate full-suite recheck passes7119/7119, zero failures/skips, in1m57.175s. Preserve the failure and rerun without claiming a root cause. No production, metadata-test or dependency edit is needed.

- Final independent recheck reports no remaining findings. Historical hashes match its original snapshot; current hashes, final TRX counters/hash, transcript and pinned external bytes match disk. The external configuration remains unstaged. All nine original findings and the historical-identity correction are accepted; no new work is deferred. Final commitlint, conservation, gitlink and whitespace checks pass; fetched main has no divergence before commit.
