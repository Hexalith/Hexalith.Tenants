---
title: 'DW-470 keyed member failure acceptance reconciliation'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: '9abc6f70607ec73bfb1da3fd49b74b3560609e8c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-470 still describes lost localized failure keys in AddMember and ChangeRole, although both initial and retry delivery completions now preserve them. Existing keyed-only retry tests assert identity and audit state without proving the visible explanation survives in English and French.

**Approach:** Reconcile DW-470 with focused actual Fluent component regressions for both member flows and shipped EN/FR resources. Cover an initial keyed-only failure, resubmission after a failed sent attempt, and same-ID delivery retry after ambiguity. Assert the retained key, empty literal message, localized explanation and failed lifecycle announcement, original identity/intent, dispatch count and independent audit semantics. Reuse the retained-flow fixture and culture-bound localizer. Close only DW-470 after focused/full Debug UI verification and independent review; preserve historical findings and all other records, archive, sprint and release holds. Add no runtime/public API, resource, dependency or orchestration changes. No staging, commit, push or deployment.

</frozen-after-approval>

## Implementation Notes

- Route facts: no intent gaps or irreversible operations; one existing test fixture plus this spec, validation evidence and two backlog documents. No new callable production surface.
- Source already copies `SafeMessageKey` in initial and retry nonaccepted completions: AddTenantMemberFlow SubmitAsync/RetryDeliveryAsync and ChangeTenantMemberRoleFlow equivalents. DisplaySafeMessage localizes the key first. Existing SupportSafetyBoundaryTests cover EN/FR rejections; existing keyed failure tests omit explanation assertions.
- Reuse `TenantRetainedCommandFlowTests.RegisterGateway`, `RenderFlow`, `Submit`, `InvokeSubmissionAsync`, snapshot/dispatch helpers, and `CultureBoundTenantsLocalizer`. Cover add/change × en/fr × initial/failed-resubmit/ambiguous-retry. A fresh pre-dispatch failure has NotStarted audit; a reused sent identity remains AuditUnavailable. Failure remains terminal without projection confirmation.
- Verify the UI test project Debug source build with `-p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`, then maintained per-project MTP focused retained-flow tests and full UI suite (minimum existing floor 5967). Run the story-gitlink guard and whitespace check. Record exact commands, outcomes and limits in `dw-470-validation-evidence.md`.
- Added 12 EN/FR cases to the existing retained-flow fixture, covering initial keyed-only failure, terminal failed resubmission and actual ambiguous delivery retry through the form submission handler. Assertions include literal/key distinction, exact localized DOM and assertive failed state, original intent/message ID, baseline, dispatch/status counts, audit independence and terminal admission release.
- Pre-edit isolated Aspire UI became healthy in 31.6 seconds; final inspection found all 23 resources Running/Healthy. Initial telemetry had 126 entries (94 Warning, 30 Information, two Error), so this is baseline qualification rather than complete runtime acceptance. Stopped only this exact Tenants AppHost.
- Initial build failed with CS8382 because Shouldly's expression tree cannot contain tuple equality. Replaced the aggregate predicate with tuple assertions in a foreach. Final Debug source build passes with zero errors and the same two existing NU1903 advisory warnings. Pre-review focused MTP retained-flow suite passes 207/207, zero failures/skips. Full UI verification and blind review are pending.
- Blind review returned three acceptance gaps; all are patched: compare initial and final baseline against the known fixture value, pin literal EN/FR failed announcements independently of resource fallback, and assert rendered audit availability/absence for reused/fresh identities. No runtime change or deferred follow-up is required. Reviewed focused/full verification is pending.
- Independent recheck confirms all three findings resolved with no additional regression. Reviewed build and focused207 pass with zero failures/skips; reviewed full suite remains pending. Initial full5979 also passed; it is retained as pre-review evidence.
- Final reviewed Debug focused207/full UI5979 pass with zero failures/skips. All 12 cases pass in both TRX reports. Only DW-470 and its index row close:339 done/304 open across643 IDs,272 rows. Unrelated records/rows and captured runtime/archive/sprint/build hashes are preserved. Exact commands, initial failure, review and validation limits are in [evidence](dw-470-validation-evidence.md). No commit is created because direct repository instructions prohibit unrequested staging/committing.

## Review Triage Log

- **medium / patch:** Initial baseline compared the final snapshot with itself. Initial and final snapshots now both require the explicitly supplied baseline `tenant-sequence:1`, so baseline loss fails every path.
- **medium / patch:** Expected failed announcements used the same localizer as the component and allowed neutral fallback. Added independent literal EN/FR expectations for both flows and checked the resource and live DOM against them.
- **medium / patch:** Audit assertions observed only snapshot state. Added rendered audit availability `unavailable` for reused sent identities, and absence of that availability control for fresh NotStarted failure.
