---
title: 'DW-191: Reconcile membership notifications received before child attachment'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '6389595745e5c3a7fec705f47f759a963109a87e'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A projection notification can complete authoritative tenant reads while the member-review component has not attached. Restoring a retained membership attempt later does not reconcile its status, leaving its evidence and aggregate lock pending despite the notification.

**Approach:** Retain the membership refresh intent until the current Ready member component renders, then invoke its existing authoritative status/evidence reconciliation. Scope pending intent to the current tenant/read generation and caller; discard obsolete work on replacement or disposal. Coalesce duplicate intent without automatic dispatch or notification-based success. Qualify actual subscription events during held initial loads for retained add, role-change and removal attempts, including non-confirming evidence and obsolete-load/route controls. Preserve existing mounted flows, command identity, authorization, aggregate ownership, localization and projection/audit confirmation gates. Close only DW-191 with exact evidence and maintenance reconciliation. No API, persistence, dependency or topology change.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `6389595745e5c3a7fec705f47f759a963109a87e`, clean main. User explicitly authorizes completing the next deferred item and pushing on main. Earlier open items retain their recorded dependencies and qualification holds.
- Small reversible footprint: TenantDetailPage, one focused partial test file, spec and ledger/maintenance evidence. No unresolved intent gap or irreversible implementation action.
- Code investigation: TenantDetailPage.RefreshTenantReadsAsync forwards only to an existing member ref before StateHasChanged; OnAfterRenderAsync can register notifications while initial loads suspend with no child. MemberAccessReview.OnParametersSetAsync restores retained targets, while member flows restore snapshots without querying status. Reuse ApplySignalRNudgeAsync and existing generation/caller/route guards.
- Baseline Debug source build exits 0 with zero warnings/errors. Aspire initial ps empty; isolated start/describe/error sample/stop exit 0. Early describe has 10 Running and 13 Waiting resources, with two error records. This establishes app-state inspection only, not distributed functional acceptance.

- Initial 12-case reproduction fails all 12 cases against unchanged production; retained attempts stay Accepted. The first test build omitted an enum namespace (CS0103), corrected before reproduction. Initial fixed-code Debug source build exits 0 with zero warnings/errors; all 12 maintained focused cases pass with zero failures/skips.
- Keep mounted-flow forwarding inside its original callback context to preserve manual-refresh recursion protection. Defer only absent/mismatched children; Ready/Stale/Degraded body states remain eligible. Replay verifies current read/caller/route, child detail, authorization and authorization version, consumes before await, and contains optional provider failures.

- Review expands qualification to caller replacement, denied/indeterminate/faulted authority, replay already awaiting status/proof, repeated notifications, stale/degraded attachment and non-authoritative proof. Fixed-reason diagnostics use a source-generated logger and the existing synchronous test sink; exact tracking handles are asserted at lookup.
- Two initial expanded failures are test-only render-driven waits for obsolete authorization cleanup; bounded cancellation-source completion polling fixes them. A later logger assertion initially retains pooled source-generated state through NSubstitute; using the existing synchronous diagnostic sink captures the formatted message at Log time. No production cancellation or confirmation gate is weakened.
- Expanded Debug source build exits 0 with zero warnings/errors; focused 45/45 passes with zero failures/skips. Final full UI and independent correction recheck follow.

## Review Triage Log

- Medium / patch: a second notification while an attached child still awaits authority could discard pending intent through the mounted fast path. Require proven child authority for that fast path and retain the newest read generation otherwise; advanced/unchanged second-notification cases qualify recovery.
- Low / patch: deferred callback containment omitted the subscription callback failure diagnostic. A source-generated fixed-reason logger restores observability without tenant, exception or provider data; both pre-status and post-status proof failures assert the exact formatted message.
- Medium / patch: retained snapshot identity did not prove status lookup identity. Substitutes now assert the exact message, correlation, aggregate, domain and cancellation token.
- Medium / patch: caller-replacement coverage was absent. Three real-notification cases replace the authenticated caller while old authority/reads are held and prove no old reconciliation, adoption or permission leak.
- Medium / patch: delayed authority controls lacked denied, indeterminate and fault results. Nine cases preserve the retained snapshot, lock and zero status/proof reads while authority is unproven.
- Medium / patch: route/disposal controls stopped before replay. Held status/proof cases now release late responses after both boundaries and verify containment, original ownership and uncontaminated replacement state.
- Medium / patch: Ready-only fixtures left Stale/Degraded body eligibility unqualified. Current and non-authoritative command-proof controls distinguish confirmation from retained pending evidence on both surfaces.
- Medium / patch: fault coverage stopped before status publication. The proof-fault case preserves the partially advanced ProjectionPending state, identity and ownership; benign renders do not retry, and a new notification recovers.
- Medium / patch: successor-notification coverage stopped before replay. Held status/proof cases now deliver another actual notification, verify serialized bounded terminal coalescing and exact retained identity; existing MembershipRefreshIntentTests continue to qualify nonterminal queued batches.

- Independent correction recheck accepts all original behavior/coverage corrections, then identifies uncontained optional logger resolution/emission inside the failure handler. A nested diagnostic guard contains service/factory/provider failures; two extra fault rows qualify disposed logging scopes. Final recheck reports no unresolved review finding. Final Debug source build exits 0 with zero warnings/errors.
- Medium / patch (correction recheck): optional logger failures could escape the contained replay failure handler. Guard the diagnostic operation itself; throwing-factory controls require renderer containment, preserved evidence and explicit notification recovery.

## Final Verification

Final Debug source build has zero warnings/errors; focused 47/47 and full UI 6796/6796 pass with zero failures/skips. Independent review and correction recheck have no unresolved finding. Scoped formatting, exact pinned commitlint and own-spec gitlink checks pass. Only DW-191 closes: 656 IDs remain, 405 done/251 open across 226 exactly covering maintenance rows. See [complete evidence](dw-191-validation-evidence.md). No new work is deferred.
