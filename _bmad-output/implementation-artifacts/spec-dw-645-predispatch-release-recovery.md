---
title: 'DW-645 recover undispatched removal after activity-release failure'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
baseline_commit: 'e667d5391dae306e631dec23acd191863e54130c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-645 records that a member removal refused after activity admission but before delivery loses its retained ownership before the release callback completes. If that callback throws, the component retains RequestSent and stale activity bookkeeping, preventing safe recovery.

**Approach:** Preserve an explicitly undispatched, recoverable refusal despite a release callback fault, and require a new successful activity admission before any later deliberate submission can deliver. Rerun current authority, freshness, preview and typed-confirmation checks. Keep generation/ownership guards and potentially delivered command identities intact. Close only DW-645 after focused regression verification and independent review.

</frozen-after-approval>

## Implementation Notes

- Freeform deferred maintenance, limited to the latest DW-644 follow-up. Clean entry on main at the canonical baseline above. No intent gaps, irreversible actions, dependency changes or new APIs. Small footprint: the removal component, its existing test class, this spec, validation evidence, deferred ledger and maintenance index. Repository user instructions prohibit unsolicited Git mutations, so no commit or push is part of this run.
- Code map: `RemoveTenantMemberFlow.SubmitAsync` abandons tracker ownership before awaiting `SetCommandActivityRaisedAsync(false)` in its post-admission safety refusal. That helper rethrows false callback errors and leaves activity true. `TenantCommandAttemptTracker.Detach` already distinguishes abandoned undispatched admissions from dispatched evidence; reuse it without changing tracker or snapshot contracts. Keep submission/ownership barriers until cleanup settles, then restore the existing guarded `RefuseBeforeDispatch` or `RefuseRetry` snapshot and require fresh admission.
- Given a held admission and changed freshness/confirmation, when its release callback throws, then no command/status/audit I/O occurs, the preview remains recoverable with null IDs and audit NotStarted, and later deliberate submission requires a new successful activity admission. Given an old release pending during tenant replacement, when it completes late, then the new tenant's ownership/evidence survives. Given a failed delivered attempt, when retry cleanup faults, then its original identity and uncertainty survive.
- Tests reuse `RenderLeaseRecoveryFlow`, real gate/tracker, held TaskCompletionSource callbacks, and existing safety/ambiguous-delivery cases. Added regression cases cover both callback forms, freshness/confirmation refusal, refused/faulted reacquisition, pending release reentry, late tenant replacement and same-ID failed-delivery retry.
- Pre-edit Aspire baseline: isolated start, describe, telemetry-log inspection and UI wait all succeeded; final snapshot had all 23 resources Running/Healthy. The isolated Tenants host was stopped before building; the pre-existing FrontComposer host was preserved. Exact commands and results will remain in `dw-645-validation-evidence.md`.
- The nine new regressions fail against original runtime source while all 121 existing removal cases pass (130 total, zero skips). The pre-fix Debug source build passes with zero warnings/errors. Failures include stranded lifecycle, escaped callback faults, blocked route replacement and pending-release behavior.
- The implementation keeps the retained attempt and submission barrier through the release await. This also preserves existing identity-change invalidation during a pending release. A narrowly scoped callback catch handles only known pre-delivery safety refusals; matching live generation and retained-attempt identity are checked before clearing activity, abandoning the undispatched admission and installing the original preview or failed-retry evidence. Other callback paths and delivered-attempt policy are unchanged.
- First post-fix execution passed 128/130; the two remaining fixtures used a render-based wait for a callback that suspends rendering, and asserted pending ownership after the default gateway returned terminal Failed. They now use explicit callback-entry completion signals and hold replacement delivery through ownership assertions. No runtime change was needed for those fixture corrections. The rechecked source build passes with zero warnings/errors; all 130 focused removal cases pass with zero failures/skips. Full UI execution and the configured context-free blind review are running.
- Independent blind review returned six findings, classified below. Four coverage gaps are being patched: accepted/status/projection-confirmed recovery and release, disposal with same-tenant renderer replacement, current authorization/audit/viewport checks plus held reacquisition, and successful as well as faulted pending-release continuations. Two older source-confirmed issues remain independent: failed-retry identity after renderer replacement and inaccurate authority refusal copy after admission invalidation. Neither was introduced by this patch; execution limits will be retained in their new ledger entries.
- The first full UI run passed 6031/6035 with four failures in unchanged global-administrator fixtures. A focused seven-case run and a control build using the original entry removal component reproduce the same four failures (three replacement timeouts and one unchanged Previewed state). The authored component was restored in a Python try/finally after that successful control build; no user edit was reverted. The failed fixtures send DOM changes to an explicitly disabled Fluent input. Two target-replacement callback sites now invoke its existing ValueChanged EventCallback through the renderer to simulate the queued/stale event they intend to test. Existing disabled/cancellation/lease assertions remain; production global-administrator code is unchanged. This adds one prerequisite fixture file to the footprint without changing user-facing scope or APIs.
- Independent correction recheck accepts all four coverage fixes, both source-only deferrals and the prerequisite fixture adaptation, with no remaining concrete correction. The final reviewed Debug source build passes with zero warnings/errors. Expanded focused removal 137 and global-administrator7 pass with zero failures/skips; all 16 new removal cases are included. The final full UI run is underway.

- Final reviewed full UI execution passes 6,042/6,042, zero failures/skips, exit 0 in 1m51.598s. Together with the zero-warning/error Debug build, focused removal 137/global 7 and independent correction recheck, this completes DW-645. Its historical fields remain preserved under pre-closure keys; only DW-645 closes. New DW-646/DW-647 retain older source-only defects. Final ledger/index preservation, gitlink and whitespace checks are recorded in the validation evidence. No Git mutation ran.

## Review Triage Log

- `medium / defer (DW-646)`: A failed-delivery retry replaces the terminal retained attempt with a new undispatched admission; safety refusal abandons that tracker record before restoring RefuseRetry locally. Cancel/reopen can therefore lose the prior message identity. Source inspection of canonical entry confirms identical loss with successful release before DW-645; no renderer-replacement reproduction was executed. Separate retention follow-up, while the same-live-renderer identity control remains required here.
- `medium / defer (DW-647)`: Freshness/confirmation changes invalidate admission generation; existing authority reauthorization then returns false and chooses the authority-unavailable explanation even while authority remains authorized. The decision logic is unchanged from canonical entry. Preserve this older feedback defect as source-verified; no EN/FR wrong-copy regression or repair is claimed in this run.
- `medium / patch`: Recovery initially ended at a stubbed Failed response. Extend all four callback/freshness/confirmation cases through accepted status, projection pending, authoritative absence plus committed sequence, and final activity/gate release.
- `medium / patch`: Add disposal during pending cleanup followed by a new same-tenant renderer; late old release fault must preserve replacement snapshot and admission.
- `medium / patch`: Add denied/indeterminate authority, audit and viewport refusals after cleanup fault; after restoring them, hold reacquisition and change freshness again, requiring zero delivery until a later fully eligible submit.
- `low / patch`: Parameterize pending-release reentry and tenant-replacement cases with successful cleanup as well as exceptions, because ownership abandonment now occurs after either completion.
