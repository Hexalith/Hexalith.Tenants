---
title: 'Resolve five deferred UI evidence and recovery records'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete exactly DW-159, DW-200, DW-209, DW-326 and DW-448, verify their behavior, and push the finished change on main as requested. Keep the established authorization, provenance, command identity and retained ownership contracts.

Map denied and missing configuration surfaces to unavailable evidence even if a caller supplies an available model. Connect create recovery to the existing optional projection notification service: notifications initiate authoritative status/projection reads and never prove a command outcome. Qualify My Tenants, tenant audit and global-administrator full-width layouts through the actual FrontComposer shell. Preserve the approved ten-fact remove-configuration preview, explicitly document its Current/Current/Present proof facts and remove unreachable localization variants. Bound complete unmatched removal audit walks using the existing three-retry recovery threshold, retaining the count across remounts and allowing later exact evidence to recover.

</frozen-after-approval>

## Implementation Notes

- Investigation: clean main at `fc5d301dd07683475b7a1bcecf0ba5fab99ab265`; no story key. This is a bounded maintenance batch using existing components and services, with no unresolved product choices, new public service contracts or data migration. The requested final push is authorized.
- DW-159: `TenantConfigurationView.StateResourcePrefix`; add denied/not-found classification to existing Unavailable mapping. Exercise the component with available and unavailable safe models.
- DW-200: `CreateTenantFlow.razor`, `TenantReadRefreshSubscription` and existing refresh-drain/lease patterns; bind the created tenant, caller and attempt, contain obsolete callbacks, coalesce refreshes and avoid feedback from the parent projection refresh.
- DW-209: extend `PageLayoutDeclarationTests` with the remaining three page types and page markers. Existing constrained lookup behavior stays intact.
- DW-326: `RemoveTenantConfigurationFlow.razor`, `TenantRemoveConfigurationPreview`, EN/FR resources and preview tests. Keep ten complete-preview facts and fail closed for non-current/incomplete inputs.
- DW-448: retain a bounded unmatched-walk count in `TenantRemoveMemberCommandSnapshot`; only complete unmatched walks increment it. Reuse `TenantAuditAvailability.MaximumUnchangedRetries`; preserve confirmed access outcome and exact later proof.
- Verify focused component/state tests, localizer parity, full UI suite and independent blind review. Reconcile only the five selected ledger records and maintenance rows with exact evidence; preserve unrelated records and release/runtime holds.
- Runtime baseline: `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded. `aspire describe` reports security running and EventStore/Tenants resources waiting for security readiness; this does not prove a healthy authenticated topology.

- Implementation uses the existing refresh drain for create; optional subscription setup runs after audit-return retention and focus. Callback scope includes attempt generation and caller epoch. Complete unmatched removal walks use a retained, capped count; matching proof resets it. EN/FR complete-preview tests pin Present/Current/Current and ten facts.

- The production workspace now supplies a stable caller identity scope to create independently of temporary authority resolution. Actual workspace notification and five successive held manual authority renewals qualify the real list proof callback, same command identity and notification resubscription.
- Optional setup has a per-scope failure budget; successful adoption resets it. Held setup tests wait explicit cleanup/replacement signals. Renderer-dispatched publication rejects stale attached-caller responses while preserving exact retained completion of manual status reads after renderer detachment.

## Review Triage Log

Independent blind review ran with a separate, context-free reviewer. All review layers configured by the one-shot route ran; none was skipped. The initial reviewer reported a seven-finding floor and nine findings, followed by three concrete recheck findings.

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| Optional setup delayed retention/focus | medium; setup preceded existing after-render obligations and could wait the transport deadline | Patched: setup follows both obligations. |
| Setup after asynchronous focus lost renderer context | medium; focus awaits use ConfigureAwait(false) and subscription fields require serialization | Patched: InvokeAsync owns setup; result publication also uses the renderer. |
| Unbounded failed subscription attempts | low; repeated renders could retry optional transport indefinitely | Patched: three failures per attempt/caller scope, preserving manual recovery. |
| Setup lifecycle tests completed immediately | low verification gap; existing tests did not hold setup over caller/disposal boundaries | Patched: real coordinator setup is held and obsolete cleanup/current subscription are awaited explicitly. Extra attempt-replacement permutations are rejected as low: no demonstrated remaining generation defect, and the explicit generation guard plus existing dismissal/retention tests cover that invariant without a second lifecycle harness. |
| List/detail continuations lacked invalidation coverage | medium verification gap; status-only holds did not qualify separate proof continuations | Patched: status, list and detail holds cross caller changes/disposal, with late success and late read failures. |
| Coalescing tested only two nudges | low verification gap; parent projection intent was not exercised | Patched: manual Fluent Refresh joins a held notification read and drains its replay. |
| Workspace proof path unqualified | medium; component-only tests omitted the real list reload/rerender and caller cascade | Patched: workspace caller scope is supplied and actual production provider is exercised for notification and manual recovery without redispatch/feedback. |
| Terminal notification rendering/cleanup not asserted | low verification gap; explicit disposal could mask missing terminal cleanup | Patched: rendered confirmed lifecycle and automatic unsubscribe are checked before disposal. |
| Removal count lacked multi-page/failure-after-two coverage | low verification gap; count zero/single-page checks were incomplete | Patched: two-page walks, actual remount, failed read at count two, delayed escalation and late exact receipt. |
| Temporary authority resolution invalidated manual caller epoch | medium; the first cascade gated caller identity on authority and changed it to null during renewal | Patched: stable identity is separate from authorization; five held renewals pass through the actual workspace. |
| Successful subscriptions exhausted failure budget | medium; initial accounting charged every setup, including healthy renewal | Patched: only failed/empty setup charges the budget; adoption resets it, with more than three successful renewal cycles. |
| Neutral manual status fallback also accepted stale attached callers | medium; fallback lacked a renderer-detachment check | Patched: neutral fallback requires the old renderer to be detached; attached manual caller-change rejection and existing ownerless terminal completion are both qualified. |

No new deferred record was created. Unrelated permission-change policy, runtime acceptance and release holds remain tracked separately.

## Verification

Final Debug source build passes with zero warnings/errors; recovery 420/420 and full UI 7,478/7,478 pass without failures/skips. The ledger validator proves exactly five closures, 657 preserved IDs, 555 done/102 open, 92 exact-covering index rows, all original fields preserved and unrelated records unchanged. Full commands, review corrections, initial failures and runtime limits are recorded in `dw-159-200-209-326-448-validation-evidence.md`. The exact commit candidate passed pinned commitlint 21.2.3 with zero problems/warnings.
