---
title: 'Resolve five deferred async state ownership records'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
baseline_commit: '907029802377ab8e80c20e894f27c6ca84360478'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve exactly DW-180, DW-266, DW-473, DW-503 and DW-574. Keep obsolete asynchronous operations from changing the currently displayed tenant state or exposing another caller's retained correction intent. Preserve exact command identity, single-flight delivery, authoritative projection confirmation and aggregate admission through unresolved delivery.

Qualify the existing create-dismissal and membership callback semantics, repair demonstrated workspace/correction ownership gaps, and retain the original findings and unrelated ledger records. Verify and independently review the change, then commit and push it on `main` as requested.

</frozen-after-approval>

## Implementation Notes

- Planning route: bounded changes to existing internal asynchronous ownership checks and their tests; no unresolved intent gaps, persistence changes, new public service contracts, dependency changes or deployments. The final Git push is explicitly requested.
- DW-180: `Components/Pages/TenantsWorkspace.razor` must retire the old load generation before `BeginLoad` synchronously invokes cancellation callbacks. Add a controlled inline old-load completion regression and preserve token ownership.
- DW-266: evaluate `AsyncLocal<bool>` in add/change membership flows with causal callback reentry and independent overlapping-nudge tests. Preserve causal-context isolation if a component-wide boolean would drop independent work; document the verified policy rather than replacing a correct primitive merely for simplicity.
- DW-473: reuse `CreateTenantFlowTests.DismissalDuringRetriedPendingProofDropsLateSuccessOrFailure`; qualify the production workspace callback alongside the callback-free held list/detail cases. Existing generation/evidence publication checks should survive unchanged unless a failing regression demonstrates a gap.
- DW-503: `CorrectionStartPanel.razor` must capture the status operation's snapshot/intent and apply visible results only to its original mounted attempt. Rejected tracker writes must never fall back to displaying the rejected candidate. Test held status across replacement, expiry and remount, preserving exact retained ownership.
- DW-574: bind visibility of retained tenant correction intent to the authenticated circuit caller. Principal changes must synchronously hide earlier intent, including navigation/remount and late callbacks. Keep unresolved aggregate leases held until the existing terminal/expiry policy releases them; caller redaction cannot enable duplicate dispatch. Cover authorized replacement and sign-out, with same-caller continuity.
- Primary files: workspace and correction components, `State/TenantCommands/TenantCorrectionAttemptTracker.cs`, its existing DI registration if needed, and existing workspace/member/create/correction/audit/tracker tests. Reuse authentication-transition and fake-clock fixtures. Keep every C# type in its own file.
- Acceptance: given synchronous cancellation completion, an old load cannot apply after replacement begins. Given a causal callback reentry, no recursive status walk starts; an independent overlapping nudge is retained. Given dismissal during create proof, late success/failure cannot overwrite Idle. Given a superseded correction, late status cannot alter its successor. Given a different caller, prior correction facts remain hidden while unresolved command ownership stays enforced.
- Verify with the Debug UI source build, focused direct xUnit runs, the complete UI suite, exact five-record ledger/index preservation checks, whitespace and story gitlink checks, and the pinned commitlint CLI. Record exact commands, results and limitations in the batch evidence artifact.
- Aspire pre-edit baseline: start and describe succeeded; resource health is summarized in validation evidence. The started AppHost was stopped successfully before edits. This does not substitute for authenticated runtime acceptance.

- Canonical entry revision: `907029802377ab8e80c20e894f27c6ca84360478`. Aspire describe reported all 23 resources Running/Healthy. Sanitized names/state/health are retained; endpoint credentials and environment data are excluded.

- Independent Blind Hunter reviewed approximately 72.819 kB and returned nine findings. Corrections below stay within the selected asynchronous ownership goal and add no public API. The initial focused run passed 668/671: two host-fixture assertions expected dismissal where the actual AuditPending host does not offer it, and one remount test inspected a disposed bUnit handle. After fixture corrections a second run passed 670/671; the remaining test needed explicit production teardown while its event was pending. A subsequent run started before its build finished and used the earlier assembly; that stale-binary run is retained but is not final validation evidence. An overly broad fixture edit also caused one CS0103 compile failure; it was corrected without production changes.

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Generation check and visible assignment run outside renderer | medium: a parameter update can interleave with a background continuation. | Patch: await one renderer operation for checking ownership, reading retained truth and publishing visible state. |
| Remount test forces a render and masks absent retention notification | medium: tracker updates previously notified neither a newly mounted panel nor the page. | Patch: publish StateChanged after exact updates/delivery completion outside the outer tracker lock; remove forced rendering from the held-status remount regression. |
| Delayed same-caller resolution does not refresh Resume | low: successful page loading can precede tracker subject resolution. | Patch: page subscribes/unsubscribes to retained state, removes hidden submitted intent and rerenders on resolution; controlled remount test needs no unrelated page input. |
| Old status read disables replacement Refresh | medium: a component-wide busy integer outlives its intent. | Patch: retire a unique status owner on generation changes; obsolete finally cannot clear its successor's guard. Replacement matrix starts successor lookup while original response is held. |
| Same-caller resolution leaves a mounted panel blank | low: invalidation cleared the message-bearing snapshot needed by the original callback. | Patch: restore only a caller-visible retained attempt matching the still-supplied intent; pending identity regression asserts recovery without parent parameters. |
| Public parameterless constructor or manual registration bypasses caller binding | false for the supported host: Program registers AddHexalithTenantsUiModule, whose explicit factory supplies AuthenticationStateProvider and fails closed when absent. Parameterless/clock construction is preserved for isolated legacy fixtures; arbitrary replacement registrations do not describe the shipped composition. | Reject the claimed shipped-host bypass; qualify the actual production registration with a DI regression. |
| Caller-isolation tests bypass production DI | low: a wrong factory would have escaped the initial tests. | Patch: resolve the tracker/gate through the real module extension and scope, then qualify redaction and retained lease ownership. |
| Caller transition tests omit held I/O | low: synchronous completion did not qualify late retained evidence. | Patch: hold delivery/status across authorized caller replacement and sign-out, remount, then complete and assert privacy plus exact owner terminal release. |
| Replacement regression omits query-gateway fallback | low: production uses this provider path. | Patch: hold status across tenant/target replacement and assert projection proof queries only the original literal tenant/target. |

- Independent recheck exposed a guard-acquisition window after stale publication was refused. Status-owner acquisition now validates generation/message together on the renderer. Obsolete reconciliation may retain its own outcome without claiming a successor's UI guard. Submission busy cleanup is generation-bound for the same reason. A held old delivery that begins reconciliation after a different tenant's replacement now has an explicit successor-refresh regression. No new public API or deferred record is introduced.

- Final independent recheck finds no remaining blocker. It confirms renderer-atomic guard acquisition and generation-bound successor cleanup; the shipped-host constructor triage is accepted. The three negative controls reject the original workspace ordering and component-wide add/change callback flags. Candidate source bytes were restored exactly afterward. Final validation uses a subsequent complete build.

## Final Verification

Final Debug source build (`build-verified-final`) passes with zero warnings/errors. Focused validation passes 680/680 and complete UI validation passes 7,434/7,434 with zero failures/skips/not-executed. All focused case names appear in the passing full TRX, and all 15 source/test and both assembly hashes remain unchanged. The corrected deterministic workspace negative observes an unretired generation under original ordering; both component-wide callback boolean negatives lose their required catch-up lookup. All three controls fail as required, and final candidate source is restored before the passing build.

The first 680-case run had four fixture failures: EventCallback completion was an unreliable scheduling proxy; beta intent fields still carried alpha inputs; a resume click needed rendered-component synchronization; an existing test expected stale RequestSent after automatic retained notification. Independent review accepts the corrected assertions. Production code remains unchanged by these final fixture corrections.

Ledger preservation passes: all 657 IDs remain, exactly the five selected records close, all 652 unrelated records and selected original fields are preserved. The maintenance index exactly covers 122 open records across 112 rows; 535 records are done. Archive, sprint, dependencies and release holds remain unchanged. Independent review/rechecks are clear with no new deferrals. See `dw-180-266-473-503-574-validation-evidence.md` and the raw archive for exact commands, original failures, results and limitations.
