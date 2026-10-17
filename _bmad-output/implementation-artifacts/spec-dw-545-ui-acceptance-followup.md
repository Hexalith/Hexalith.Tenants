---
title: 'Complete DW-545 UI acceptance after held-authority safety changes'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '43ecec769267309f9801fb772fcad711403a2301'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-545 has accepted conflicting-administrator evidence coverage, but existing held-authority removal safety tests fail the full UI acceptance gate. Confirmation changes can cancel a pending authority operation before publishing the changed input, allowing refusal feedback to observe obsolete confirmation. The current failures must be explained and corrected before the deferred item can close.

**Approach:** Establish the failing interleaving with focused, controlled regressions; correct the smallest proven ordering or test-synchronization defects while preserving exact EN/FR safety explanations, cancellation, no dispatch, current reauthorization and same-identity retry recovery. Complete independent review, a Debug source build and the full individual UI suite before closing only DW-545. Preserve all historical failures, original ledger fields, unrelated work and release holds.

</frozen-after-approval>

## Implementation Notes

- Clean entry: main at canonical `43ecec769267309f9801fb772fcad711403a2301`. No intent gaps, external side effects or new public API. Small footprint: removal flow and its existing test fixture if runtime evidence requires it; this spec, prior DW-545 spec/evidence, selected ledger record and maintenance index. No dependency updates or Git mutations are needed.
- Reuse `RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety` and existing lease/tracker/localizer helpers. It covers eight combinations of EN/FR, freshness/confirmation change and first attempt/retry. Keep every assertion about exact localized safe/live feedback, cancellation, late denied results, ownership release, zero dispatch/status and recovered same-ID retry. Add explicit completion barriers or snapshot checks only where they prove the intended state; never broaden acceptable messages or add delays/retry-until-green.
- `RemoveTenantMemberFlow.OnConfirmationInput` currently invalidates admission (synchronously cancelling the provider token) before assigning `_confirmationText`. `ReauthorizeAdmissionAsync` and `SubmitAsync` resume off the renderer via ConfigureAwait(false). Investigate the exact refusal and retained-attempt/render ordering before choosing a repair. `OnParametersSet`, `RestoreRetainedAttempt`, and the release callback share the affected state; preserve tenant/member replacement invalidation and disposal behavior.
- Given a held authority read and changed confirmation or stale evidence, when admission is cancelled, then the settled snapshot and rendered EN/FR message must describe the current safety refusal; the late provider result cannot mutate it or dispatch. Given safety recovers, when the user explicitly confirms and submits again, then current authority is read and retry identity is preserved.
- Baseline Debug source build succeeds with zero warnings/errors. Existing focused test reproduces three failures out of eight, at the exact rendered-refusal assertion. Raw results: `/tmp/tenants-dw-545-followup/baseline-tests.log` and `baseline/baseline.trx`; preserve these as failing controls. Aspire baseline started isolated and resource/log inspection is recorded separately. Existing conflicts and accepted loader coverage are unchanged.
- Validate the focused affected flow and loader, then the full UI project through Microsoft.Testing.Platform. Review with the workflow's independent blind reviewer and recheck any corrections. Record exact commands, outcomes and limits. Keep changes uncommitted under the explicit repository Git boundary.

- The strengthened existing theory now covers 16 EN/FR, freshness/confirmation, first/retry and immediate/held-cleanup combinations. The release callback records renderer access; settled snapshot feedback is checked before the unchanged exact DOM/live-region assertions. Held cleanup proves pending ownership, locked aggregate, duplicate suppression, and completion from a worker. Against unchanged runtime all 16 fail the dispatcher-access assertion (zero passed/skipped, exit 2); this is the controlled regression evidence, not a retry-until-green result.
- Runtime correction publishes confirmation before invalidation and explicitly captures renderer context on the four admission/cleanup awaits. Initial authority consumption and final admission each recheck the generation after resuming, so a queued old authority result cannot authorize a changed input generation. Existing ownership, disposal, evidence, no-dispatch and retry-identity guards remain.
- Post-fix Debug source build succeeds with zero warnings/errors. Workflow review has one active layer, Blind Hunter; no configured review layer is skipped. Test and review outcomes will be appended below.

- The first full UI run after the runtime fix reports 6,238 passed, one failed, zero skipped (exit 2). The sole failure is `TenantDetailSurfaceTests.MemberReviewCancelReopenPreservesRefusedRemovalRetryIdentity(true)`: an immediate removal assertion follows synchronous Click on an asynchronous Cancel handler. Source review confirms the parent release owner has already cleared and no runtime dismissal blocker is established. The fixture now awaits ClickAsync before the unchanged removal/reopen/identity assertions. This one-line dependent fixture correction remains within UI acceptance; production parent behavior is unchanged.
- Four controlled successful-initial-authority cases hold the renderer, let the authorization wrapper finish, change and restore confirmation/freshness before queued submission resumes, then prove no admission/delivery/status/identity mutation and safe explicit recovery. First-phase invalidation keeps the original snapshot because no new attempt has been admitted; the localized refusal correction concerns the original regression's leased second authority read, including first sends and failed-delivery retries. Ambiguous RequestSent retry semantics are outside this repaired path and will be recorded separately without claiming a reproduced defect.

## Review Triage Log

- **medium / patch — parent cancellation fixture:** Confirmed synchronous Click followed by immediate removal assertion raced CloseAsync's awaited activity release and renderer callback. Awaited ClickAsync preserves all removal/reopen and same-ID safety assertions; no test retry or accepted-output widening was added.
- **low / patch — authority-phase coverage/scope:** The original theory deliberately holds read two after allocation. Successful first-read completion queued before a restored safety transition now has four explicit no-admission/no-dispatch/identity-preservation regressions. Initial invalidation retains prior evidence rather than synthesizing a second-phase refusal; this boundary is documented here without redesigning first-phase copy.
- **maybe-false, medium if proven / defer — ambiguous delivery retry resumption:** RetryDeliveryAsync's separate RequestSent path remains unchanged and lacks this specific controlled queued-authority coverage. The reviewer demonstrates a coverage gap, not a production regression caused by this change. Record it as an actionable follow-up with exact evidence and proof needed, preserving existing ambiguous-delivery semantics.
- **low / patch — successful queued authority generation:** Added four controlled first-read cases to exercise post-resumption generation validation after a successful read, distinct from cancelled-provider cases. A mutation control and source recheck will verify the added guard is required. The second-read guard has source-level and cancelled-read coverage; no independently controlled successful-second-read mutation result is claimed.

- Final review recheck finds no remaining concrete issue in the repaired scope; the original review's four findings and their dispositions are retained above. All four queued-authority cases fail when only the initial post-await generation comparison is removed; the exact runtime bytes are restored and rebuilt before acceptance. Final Debug source build: zero warnings/errors. Final full UI: 6243/6243 passed, zero failures/skips; all72 conflict cases and all20 strengthened/new safety cases pass. Only DW-545 closes; DW-648 records the separate qualification gap. Changes remain uncommitted under the repository boundary; no dependency or release qualification change is made.
