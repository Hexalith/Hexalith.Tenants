---
title: 'Deferred UI runtime qualification'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '15c0a1e9daeceb5f27bec3695eb759400e2fc297'
context: ['{project-root}/AGENTS.md', '{project-root}/docs/tenants-ui-deferred-at-handoff.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

The two P2 records DW-446 and DW-459 establish UI runtime qualification as the parent subject. Work through its open siblings in priority order, starting with Create audit speech, then administrator and grid focus, then other rendered UI verification. Use explicit ledger priorities first, recorded severities next, and item ID for ties. A dedicated subagent implements and verifies each eligible item. Record and skip duplicates, existing fixes, obsolete work and blockers. Stop at ten new verified implementations or when this group is exhausted.

## Boundaries & Constraints

Work from `main` and preserve concurrent changes. Qualify the exact dependency revisions in disposable source copies through `run-with-temp`. Keep native transcripts private until sanitized. Use only the fixture's simulated operator and command. Use shipping UI paths, and preserve command identity, authorization, localization and existing accessibility and browser acceptance gates. Any shared-component change belongs to its owning repository. Historical evidence remains immutable.

Commit verified items separately with their DW IDs and pinned commitlint. Keep all-suite recovery commits and the final ledger update separate. At ten items, run the full solution build and all individual suites, repair actual failures and rerun both before marking verified records done and pushing directly to `main`. If the group is exhausted sooner, report the shortfall and the verification actually run.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` already mounts persistent audit hosts and disables the nested `AuditAvailabilityState` announcement. Its separate lifecycle region also renders state and reason. `docs/tenants-ui-deferred-at-handoff.md` defines C1–C3. The browser fixture and `capture-deferred-nvda-speech.ps1` exercise actual output. The independent resumed assessment found repeated C2/C3 speech and left its source unassigned. DW-459 is the same requirement and shares the acceptance packet.
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs` includes opt-in standalone authentication, held subscription and held grid cases. `GlobalAdministratorsPage.razor` orders subscription setup and launcher-focus restoration. For DW-660, the exact historical UI diff is recoverable at `86f472e3c321c75b1572edc279e21685037f3b19` relative to `3808cd73f6560d035c417109d27d0a2b3365992b`. Its hash matches the retained timeout packet.
- For DW-662, the held-grid lane isolates a continuation running after detachment. The pinned Builds catalog already selects Fluent 5.1.0, whose grid checks disposal after initialization and cleans up the late controller. The existing diagnostic lacks a positive check for that cleanup. [Fluent grid source](https://github.com/microsoft/fluentui-blazor/blob/v5.1.0/src/Core/Components/DataGrid/FluentDataGrid.razor.cs)
- DW-696 belongs to FrontComposer's generated `FluentTimePicker` path. `FluentTypedFieldRuntimeTests.cs` establishes typed callback behavior. Use a generated Counter specimen with required and nullable times. Drive the actual native input and assert the dispatch payload in the FrontComposer browser fixture.
- DW-679 has committed endpoint identity binding and stale-host controls at `15c0a1e9daeceb5f27bec3695eb759400e2fc297`. DW-703's evidence driver already uses a monotonic elapsed deadline with bounded requests and sleeps at `8aa6f169b3e27f0a831ad6425c4bc1278ca2c445`. Neither counts toward new implementation.

## Tasks & Acceptance

- [x] DW-446, `CreateTenantFlow.razor` and the existing native collector. Provision the pinned Windows SDK privately. Reproduce and distinguish live-region duplication from focus reading before changing product behavior. Apply a focused fix if reproduced and independently assess all applicable EN/FR Create observations in NVDA/Chrome and NVDA/Firefox. Given first Pending, first Unavailable or same-state Refresh, when the recorded fixture triggers the state, then speech conveys its meaning without duplicated announcements and the response and focus controls pass.
- [x] DW-660, administrator dialog driver and the owning focus code. Run the foreground grant Escape scenario with held and unheld subscription, then replay pinned historical source if needed. Given a reproduced body-focus timeout, when the targeted trace captures detach, focus attempt, active element and render/subscription order, then the evidence identifies the failure owner and qualifies any repair. A passing replay without cause leaves the historical record open.
- [x] DW-662, held-grid scenario in the administrator dialog driver. Bind evidence to the Fluent version and module. Given detached grid initialization, when the late continuation resumes, then verify exactly one controller stop, usable workspace navigation on Back, and zero circuit errors. Keep the earlier 5.0.1 failure as the distinguishing control.
- [x] DW-696, FrontComposer browser fixture. Exercise actual native time input for invalid text, clearing and seconds-bearing input. Given each input, when the generated editor processes it, then assertions distinguish what the native control accepts and what the generated command receives. Keep any source fix in FrontComposer and verify the change through its maintained entry point.

## Implementation Notes

- DW-446 reproduced duplicate lifecycle speech and a separate region-focus read. Persistent announcement hosts now convey each first result once, while heading focus and unchanged Refresh behavior remove the repeated reads. The agent passed all 32 native windows, 643 focused xUnit checks, 28 headless handoff checks and six collector checks. Parent inspection confirmed both browser transcripts and each Refresh control. One unrelated Firefox desktop notification was redacted with its timing and successful controls retained. Windows and Linux browser sources differ only by line endings when compared with the recorded executed inputs.
- DW-459 shares the DW-446 speech fix and verification. It adds no independent implementation.
- DW-660 remains open. Eight headed standalone OIDC replays passed on the current and historical source with held and unheld subscriptions. Launcher focus returned 4.2–7.7 ms after detach, held controls passed, and no commands or circuit errors occurred. The original timeout did not recur. The replay environment differed from the original realm and headless harness.
- DW-662 adds standalone browser coverage for the already-pinned Fluent 5.1.0 fix. The exact-Gitlink Debug build passed with zero warnings or errors. The connected grid detached, the released controller stopped once, both Back paths returned to the usable workspace, and Refresh issued a read. No commands or circuit errors occurred. The archived 5.0.1 result remains the negative control.
- DW-696 adds a FrontComposer Counter specimen and generated-command assertions for invalid typing, required and optional clearing, and seconds. Debug and Release Counter builds passed. All nine cases passed in Chromium, Firefox and WebKit. The wider Release slice passed 127 tests with two existing engine-specific skips. TypeScript validation and four typed callback cases passed. The seconds cases used native-input fill. Manual seconds entry was outside the run.
- DW-679 was already implemented in `15c0a1e9daeceb5f27bec3695eb759400e2fc297`. The earlier verification reached the live authentication gate. The stale-host controls passed in the recorded Node run.
- DW-703 was already implemented in `8aa6f169b3e27f0a831ad6425c4bc1278ca2c445`. The elapsed deadline and seven earlier controls are recorded in that change.
- The final DW-446 heading precedes the named lifecycle region and labels it. That position prevents Firefox from rereading its nested audit region on focus. Both persistent announcement hosts stay within the region. After the review variant exposed the Firefox repeat, the corrected version passed the failing cell and then all 32 native windows. The combined Tenants build and focused suite passed with zero warnings or errors, 645 xUnit cases, 28 headless handoff checks and six collector checks.
- The reviewed DW-662 result passed with inherited alternate endpoint and log settings in the isolated harness. It recorded the actual browser, one controller stop, both Back paths, a newly rendered Refresh marker and zero flushed circuit errors. The reviewed DW-696 result passed all nine browser cases with native validity and settled command assertions.
- Local commits on `main` are Tenants DW-446 `420bc97d47257add2814eba60a4cc1e74327c7cc`, Tenants DW-662 `5511006e0b691de5f6fca33823b0a06c0ec08ec1`, and FrontComposer DW-696 `a6c12ca23d7814cca36a6a7e33672367194ebcae`. Pinned Commitlint accepted each exact message. The seven-entry sibling group yielded three implementations. The ten-item condition for the full suite, ledger closure and push remains unmet. The committed ledger contains 28 open records.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| Blind 1, dotted IDs in the normal and read-safety runners | medium | Outside these ledger items. Those older lanes predate both browser patches. The new held-grid and Create handoff paths use canonical IDs. |
| Blind 2, legacy removal scope assertion | medium | Outside these ledger items. The assertion predates this change and the selected grid and speech cases submit no membership command. |
| Blind 3, late server circuit log faults | medium | Patch. Gate the held-grid result on the flushed UI log after owned browser/process teardown. |
| Blind 4, incomplete dependency details in the grid packet | false | The retained identity manifest records all Gitlink revisions separately from the browser packet. The packet binds the observed Fluent version and assembly/module hashes. |
| Blind 5, reported browser channel | low | Patch. Record the channel actually applied to the launch. |
| Blind 6, Refresh could credit a background read | medium | Patch. Require a newly armed row-name marker to render after the click. |
| Blind 7, lifecycle reason moved out of the landmark | medium | Patch. Keep stable hosts inside the lifecycle region and retain its heading as the focus target. |
| Blind 8, the live-region selector moves | low | Patch. Give each host its own stable selector for both identity checks and trace attribution. |
| Blind 9, endpoint-only focus check | medium | Patch. Assert the synchronized product focus trace during Refresh. |
| Blind 10, manual timing input matrix | low | Documented limit. The tracked item calls for actual native input behavior and dispatched typed values. The seconds case uses the actual input with Playwright fill, and the invalid case uses physical key events. Native validity receives a direct assertion. |
| Blind 11, dispatch assertions can settle early | medium | Patch. Observe the generated command outcome before evaluating exact dispatch expectations in the fixture's bounded output. |
| Edge 1, inherited sidecar address in held-grid mode | medium | Patch. Keep the held-grid child on its disposable local source irrespective of inherited configuration. |
| Edge 2, server log formatting and teardown | medium | Patch. Use the expected circuit log format and wait for teardown before evaluating the server gate. |
| Edge 3, transient focus movement during Refresh | medium | Patch. Use the time-window focus trace together with its final element. |
| Edge 4, first Unavailable can follow a rejected fixture status request | medium | Patch. Require recorded status delivery before crediting the first insertion. |
| Edge 5, seconds values can coexist with invalid native input | medium | Patch. Check native validity alongside exact typed dispatch values. |
| Verification 1, stable host regression can escape automated checks | medium | Patch. Assert both lifecycle host identities and unchanged Refresh content in the maintained handoff. |
| Verification 2, changed assertive reason lacks focus coverage | medium | Patch. Exercise a second assertive result with a new reason and require a heading focus request. |

## Verification

- Run the focused native Create matrix, standalone administrator/grant and held-grid checks, and owning FrontComposer browser tests for the items attempted. Validate the exact build, runtime and browser identities before claiming acceptance.
- Verify each implementation diff and commit message. After ten implementations, build `Hexalith.Tenants.slnx` and run individual test projects and maintained product suites through repository entry points before closing the ledger and pushing.
