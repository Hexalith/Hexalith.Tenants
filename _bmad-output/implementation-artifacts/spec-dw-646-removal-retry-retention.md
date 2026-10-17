---
title: 'DW-646 retain failed removal identity after refused retry'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
baseline_commit: 'f052c0094d952cb4856a4aa1e1e69cd15ca6ac72'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-646 records that a failed member removal may already have reached the server, but a refused retry replaces and then removes its retained attempt. Cancel/reopen destroys the remaining local snapshot, allowing a later removal to mint a different message identity.

**Approach:** Retain the original failed removal evidence across abandonment of an undispatched same-identity retry, including refused/faulted activity admission, held safety refusal, cleanup faults and renderer disposal. Reopening must recover the original request and message identity, require fresh activity admission and every current safety gate, and preserve existing dispatched recovery. Keep the aggregate unlocked after refusal. Close only DW-646 after focused behavioral checks and independent review; DW-647 remains separate.

</frozen-after-approval>

## Implementation Notes

- Freeform deferred maintenance: select the next cohesive Tenants-owned follow-up. Clean main entry is the canonical baseline above. No intent gaps, irreversible actions, dependency changes or public APIs. Small change limited to existing tracker/retained-attempt internals and behavioral tests, with a component adjustment only if required to persist refusal evidence. Tracking artifacts will record exact validation and close only this item. User repository instructions prohibit unsolicited commits/pushes; no Git mutation is part of this run.
- Code map: `RemoveTenantMemberFlow.SubmitAsync` detaches the failed attempt before activity admission, then `SetCommandActivityRaisedAsync` calls `TenantCommandAttemptTracker.TryBegin`, replacing that terminal record. Refusal or disposal calls `Detach`, which removes the new undispatched record. `RefuseRetry` restores only the local snapshot. `MemberAccessReview.CloseRemoveMemberAsync` destroys that renderer, and `RestoreRetainedAttempt` has nothing left to adopt.
- Reuse the tracker lock and exact current-attempt/renderer ownership checks. Preserve a narrowly matched prior Failed/correlationless removal when a RequestSent retry has the same tenant, intent, message and recovery baseline; restore it only after successful pre-dispatch abandonment. Its terminal lease is already released. Clear rollback ownership when actual delivery begins. Never overwrite a replacement attempt or revive stale terminal evidence after dispatch.
- Given failed delivery followed by held retry safety refusal, when the flow is cancelled and reopened, then the same message/request and original recovery evidence remain and later eligible delivery reuses them. Given refused or faulted reacquisition, when the flow is replaced, then identity survives without a pending aggregate lock. Given disposal or tenant/member replacement while admission/release is pending, when old callbacks complete, then current ownership/evidence remain intact. Given actual retry dispatch, when that renderer detaches, then the new dispatched attempt survives and no prior terminal snapshot is restored.
- Tests reuse `RenderLeaseRecoveryFlow`, the real gate/tracker, held TaskCompletionSource callbacks, existing removal safety fixtures and `TenantRetainedCommandAttemptTests`. Cover parent cancel/reopen through the existing MemberAccessReview host fixture where available. Run a Debug source build with warnings as errors, focused removal/tracker/parent tests, then the individual UI test project. Record initial failures and limits without changing unrelated gates.
- Required Aspire baseline passed before editing: isolated start, resource/log inspection and UI readiness; final describe showed all 23 resources Running/Healthy. Stopped this run's AppHost before source builds.

- Initial eight replacement-renderer regressions all fail against the original runtime source with null retained records (exit 2); the pre-fix source build succeeds with zero warnings/errors. The tracker repair adds one internal rollback reference, captures only a matching detached Failed removal, restores it atomically after successful pre-dispatch abandonment, and clears it when actual delivery starts. The removal component and parent runtime code need no changes.
- Added eight held/refused/faulted/disposal replacement cases, two real MemberAccessReview cancel/reopen cases, and six tracker ownership/mismatch/dispatched controls. The final focused source build has zero warnings/errors; 364 removal/retention and two parent cases pass with zero failures/skips. One initial filter command intersected class and method filters and ran zero cases (minimum-policy exit 8); corrected separate invocations pass. Full UI execution and independent blind review are running. Gitlink validation passes without reference-pointer changes.

- First full UI execution passes 6,058/6,058, zero failures/skips, exit 0 in 1m36.911s. Independent blind review returns six findings. All are accepted as bounded corrections: broaden failed-source correlation matching to the same whitespace predicate used by SubmitAsync; add pending-acquisition disposal/late-release replacement controls; prove current replacement safety gates and recovery through accepted/status/projection confirmation; and expand new one-line conditional bodies. DW-647 remains separately scoped, with no new deferrals.
- The first expanded-test build reports three CS0117 errors from invented enum member names in the new fixture. Corrected them to the repository's MissingPermission/Indeterminate/Phone values; exact failed and corrected build evidence is retained separately. No production policy change was needed for those fixture errors.

- First expanded focused run passes 375/380 with five new-fixture failures, zero skips (exit 2). Four checked tracker state before bUnit's scheduled handle disposal had executed; use the component's synchronous IDisposable path already established by existing lifecycle tests. The fifth assumed a single remaining owner forbids removal, but current UX deliberately treats that as elevated confirmation friction. Use Unknown member role, which the existing eligibility contract actually rejects. These are fixture corrections; no extra runtime policy change or assertion weakening is required. Independent source recheck accepted the six original corrections before execution exposed these fixture details; a final recheck follows.

- Independent final source recheck accepts the synchronous-disposal and actual eligibility fixtures, with all six original findings satisfied and no new concrete issue. Final Debug source build passes with zero warnings/errors (15.99s); final focused 380 and full UI 6074 are executing. Sixteen additional reviewed cases bring the total new coverage to 32; runtime changes remain the existing two tracker files, with no removal/parent component modification.

- Final reviewed verification passes: focused 380/380 and full UI 6074/6074, zero failures/skips; full duration1m37.011s. Independent correction rechecks and the zero-warning/error source build complete DW-646. Only that ledger entry/index row closes; all historical fields remain under pre-closure keys, with DW-647 and unrelated history preserved. Final preservation and gitlink checks are recorded in validation evidence. No Git mutation ran.

## Review Triage Log

- `medium / patch`: SubmitAsync accepts missing or whitespace correlation IDs for failed same-ID retries; the new tracker capture originally required null. Source check confirms the mismatch. Use string.IsNullOrWhiteSpace for failed-source correlation, and add null/empty/whitespace state and component controls.
- `medium / patch`: Initial disposal coverage began during release, leaving pending acquisition unproved. Add successful/faulted late acquisition after disposal with a replacement already delivering.
- `medium / patch`: Initial replacement awaited old cleanup, leaving stale completion unproved. Add same-tenant replacement and fresh delivery before old successful/faulted release completes, asserting exact current attempt/snapshot/lease ownership.
- `medium / patch`: Initial reopened flow always had current eligibility. Add denied/indeterminate authority, stale reads, phone viewport, unknown member role, disabled tenant and unavailable audit capability; assert zero new admission/delivery and original retained identity.
- `medium / patch`: Initial recovery stopped at another failure. Extend all eight original scenarios through accepted delivery, verified Completed status, projection-pending lock, exact committed-sequence projection confirmation and terminal lock release.
- `low / patch`: Expand new one-line conditional bodies into conventional Allman blocks for consistency/readability; the correction is local and mechanical.
