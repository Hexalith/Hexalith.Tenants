---
title: 'Close Story 5.4 review findings'
type: 'bugfix'
created: '2026-10-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: c9cd9045638efc3d48377989b589c130259cc2d8
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 Review 5 left fourteen patches open. A stale zero-event completion can hide stored audit evidence, a late refresh can restore a dismissed or already-confirmed attempt, and several tests stay green if those guards are removed. The parent spec still says a released unverified lifecycle attempt keeps Pending, and it does not declare the pointer moves in `697697f9`.

**Approach:** Close the patches in place. Keep stored-event evidence sticky, drop a status result whose attempt is no longer current, and pin each guard with a test that fails when the guard is removed. Record the shipped lifecycle delay rule and the three pointer moves. Leave `AfterTrackingEnds` and the recorded deferrals unchanged.

## Boundaries & Constraints

**Always:**
- **Decision (2026-10-01):** terminal lifecycle `ProjectionUnverified` maps Pending to Delayed through `AfterTrackingEnds`. Every other projection or provenance failure keeps the audit state. Do not change that code or its tests; record the exception in the parent spec.
- After `EventsStored` or `EventsPublished`, a later `Completed` with `EventCount` 0 stays `AuditPending` and keeps `HasCommandEventEvidence`. ChangeRole must not become `AlreadyApplied` / `NotStarted`. Set configuration must not clear the flag or set `CompletedWithoutEvents` when earlier evidence exists.
- Apply a status only to the attempt captured before the await. Create drops it when the snapshot no longer carries that message id. Metadata starts a merged refresh's follow-up only while the attempt is still refreshable: non-blank message id and correlation id, and state `Accepted`, `ProjectionPending`, `UnableToVerify`, or `Degraded`.
- Create's continue-read-only resets the attempt panel and focuses the lifecycle section. It does not navigate to the tenant list.
- Declare pointers with full SHAs in prose, without arrow glyphs. Do not move any submodule.

**Never:**
- Do not pass a retry message id into `FromSubmission` for Lifecycle, Set configuration, or Remove configuration, and do not thread `SafeMessageKey` through the AddMember or ChangeRole failure arms. Both are deferred.
- Do not change `AfterTrackingEnds`, the `ApplyRemovalProofMatch` body, removal-proof matching, canonical status tables, or recovery verb sets.
- Do not amend `697697f9`, edit the legacy story's historical sections, or implement rejected Review 5 findings.
- Do not redispatch, add endpoints, or derive `audit available` from command status, event count, confirmation, or SignalR.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Stale zero-event completion | `EventsStored`, then `Completed` with `EventCount` 0, every flow | Audit stays `AuditPending`; prior evidence stays set; ChangeRole stays projection-pending | N/A |
| Dismissed create refresh | Status lookup in flight, then Continue read-only | Result is dropped; panel stays Idle | N/A |
| Confirmed metadata refresh | Merged follow-up after confirmation | No second status apply | N/A |
| Create read still running | Unreadable status while projection refresh is held | Continue-read-only is absent until that refresh finishes | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` — ChangeRole `ApplyStatus` zero-event `Completed` arm (line 705) forces `AlreadyApplied` / `NotStarted`. The next `Completed` arm already keeps evidence. Add `&& !HasCommandEventEvidence` to that `EventCount == 0` arm only. `ApplyRemovalProofMatch` (line 1126) already keeps Delayed and falls Unavailable back to Pending; its XML summary does not say so.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationCommandSnapshot.cs` — `Completed` arm (line 213) overwrites `HasCommandEventEvidence` and `CompletedWithoutEvents` from the latest count. `FromCommandStatus` already receives the pre-assignment flag.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandAuditStates.cs` — remarks and `AfterTrackingEnds` (line 157) already document the exception. Do not edit them.
- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` — `RefreshStatusAsync` (line 546) applies status with no captured message id. Both continue-read-only comments claim a return to the tenant list.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` — `RefreshAttemptStatusAsync` (line 759) re-enters after `await running.Task` with no state check. Reuse the state set in `CanRefresh` (line 292), not its `_isRefreshing` clause.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandAuditStatesTests.cs` — `A_stale_status_after_stored_events_keeps_the_audit_record_pending` (line 132) iterates only Received and Processing.
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs` — sources to port: `Audit_refresh_that_resolves_to_not_started_moves_focus_to_the_lifecycle_section` (line 797) and `Retry_failed_before_dispatch_keeps_the_reused_identity_unverifiable` (line 833). Refused-retry waits use `SafeMessage.ShouldNotBe("Submission outcome is ambiguous.")`, which is true before submit.
- `tests/Hexalith.Tenants.UI.Tests/Components/{ChangeTenantMemberRoleFlowTests,RemoveTenantMemberFlowTests,EditTenantMetadataFlowTests,CreateTenantFlowTests,SetTenantConfigurationFlowTests,RemoveTenantConfigurationFlowTests}.cs` — same gaps. Merged-refresh method `Refresh_clicks_merged_into_a_running_lookup_wait_for_it_then_run_their_own_and_never_exhaust_the_retry_limit` expects 2, 2, and 3 status calls. Create's `Missing_support_without_audit_read_or_escalation_still_offers_continue_read_only` asserts a tracker release that the terminal refresh already performed.
- `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md` — provenance notes omit the `AfterTrackingEnds` exception. File List omits Commons. `697697f9` moved Commons `53f7961b517becde5b84ed4d20fe696b849b5cd9` to `c13dc6679aa91144b6d541078f3f20019d79c2eb`, EventStore `6dededdecd62dd6dc6d1f15810108d860ec70c8f` to `19dc1f82122564453163ac010dc7e5ae81db7ed3`, and FrontComposer `48f7dfef920e8217e5c6221f364f0a1e0f61f387` to `80c6d2b73088568e25acd8472c6c929cc033268d`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- Guard the ChangeRole zero-event `Completed` arm with `!HasCommandEventEvidence` so prior evidence uses the existing sticky arm. Update only the `ApplyRemovalProofMatch` XML summary to include kept Delayed and the Unavailable-to-Pending fallback.
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationCommandSnapshot.cs` -- Keep `HasCommandEventEvidence` when it is already true or `EventCount > 0`. Set `CompletedWithoutEvents` only when there is no prior evidence and `EventCount == 0`.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` -- Capture the message id before the status await and return before applying status or projection refresh when it no longer matches. Correct both continue-read-only comments.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` -- After the in-flight refresh completes, run the follow-up lookup only while the attempt is still refreshable.
- [x] `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md` -- Record the `AfterTrackingEnds` exception in the Spec Change Log and provenance Implementation Notes. Add `references/Hexalith.Commons` and a provenance note for the three `697697f9` moves.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandAuditStatesTests.cs` and `tests/Hexalith.Tenants.UI.Tests/Components/{CreateTenantFlowTests,AddTenantMemberFlowTests,ChangeTenantMemberRoleFlowTests,RemoveTenantMemberFlowTests,EditTenantMetadataFlowTests,SetTenantConfigurationFlowTests,RemoveTenantConfigurationFlowTests}.cs` -- Add `Completed` with `EventCount` 0 to the stale-after-stored sweep and expect `AuditPending` for every flow. Hold Create's projection refresh after an unreadable status and assert continue-read-only appears only after release. Assert Create's tracker release before the continue-read-only click, or remove that line. Port the AddMember audit-refresh focus test to the other six flows, and the retry-before-dispatch `FailedWithKey` test to ChangeRole, RemoveMember, and Metadata, asserting `AuditUnavailable`. In the refused-retry tests, wait on the expected `State` and assert the refusal text the flow assigns. After each merged refresh settles, assert the same status-call count once more.

**Acceptance Criteria:**
- Given events were stored, when a later status reports completion with zero events, then every flow keeps the audit record pending.
- Given a refresh is in flight, when the user continues read-only or the attempt is already confirmed, then the late result does not restore the dismissed attempt or reopen the confirmed one.
- Given the parent spec is checked from its baseline to HEAD, when the story gitlink guard runs, then the `697697f9` pointer moves are declared and the guard exits 0.

## Implementation Notes

- **Stale zero-event completion:** ChangeRole's `Completed when status.EventCount == 0` arm now also requires `!HasCommandEventEvidence`, so a stale completion falls through to the existing sticky `Completed` arm (ProjectionPending, evidence kept, `AuditPending`). Set configuration keeps `HasCommandEventEvidence` when it is already true or the count is positive, and sets `CompletedWithoutEvents` only without prior evidence. The `ApplyRemovalProofMatch` summary now names the kept Delayed and the Unavailable-to-Pending fallback.
- **Create:** `RefreshStatusAsync` captures the message id before the status await and returns before applying the status or starting the projection refresh when the snapshot no longer carries it. Both Continue read-only comments now say the panel resets and the lifecycle section takes focus, with no navigation.
- **Metadata:** `CanRefresh` is `!_isRefreshing && IsRefreshableAttempt`. A merged refresh's follow-up runs only while `IsRefreshableAttempt` holds (non-blank message and correlation ids; Accepted, ProjectionPending, UnableToVerify, or Degraded), checked on the renderer dispatcher after the running refresh completes.
- **Parent spec:** recorded the `AfterTrackingEnds` exception in the Spec Change Log and the provenance Implementation Notes, added `references/Hexalith.Commons` to the File List with a full-SHA provenance note for the three `697697f9` moves, and closed the fourteen Review 5 items in place. `TenantCommandAuditStates`, `AfterTrackingEnds`, and the legacy story are unchanged, and no submodule moved.
- **Tests (13 new, several tightened):**
  - The stale-after-stored sweep includes `Completed(EventCount: 0)` for all eight flows. A new fact pins the ChangeRole state and the Set configuration flags.
  - Create adds three tests: a held projection refresh withholds Continue read-only until release; a late lookup after Continue read-only is dropped; and the ported audit-refresh focus test. The tracker-release assertion now runs before the click.
  - The audit-refresh focus test is ported to ChangeRole, RemoveMember, Metadata, Set configuration, and Remove configuration. The `FailedWithKey` retry test is ported to ChangeRole, RemoveMember, and Metadata.
  - The refused-retry tests in AddMember, ChangeRole, RemoveMember, and Metadata now wait on the expected `State` and the exact refusal text.
  - The merged-refresh tests in Metadata, Set configuration, and Remove configuration await the merged click and assert the status-call count again. A new Metadata test pins that a merged click does not follow up after the attempt is confirmed.
- **Mutation checks:** each guard was reverted and its new test failed: the ChangeRole guard, the Set flags, Create's message-id check, Create's `CanContinueReadOnly`, the Metadata follow-up guard, `RefreshFromAuditControlAsync` in the six flows, the retry-aware `FromSubmission` in ChangeRole, RemoveMember, and Metadata, and the refusal reasons. A delayed extra follow-up lookup fails the Set and Remove configuration merged tests on the new post-settle assertion. The pre-existing wait does not see it.
- **Verification:** Release UI-tests build 0 warnings and 0 errors, including a `--no-incremental` rebuild after the mutation runs. `dotnet test` UI suite 3618/3618, with none failed or skipped. The gitlink guard exits 0 on the parent spec and on this spec. `git diff --check` is clean.

## Spec Change Log

## Review Triage Log

Review 1 (2026-10-01), diff `c9cd9045..` working tree. Layers: blind hunter (BH), edge-case hunter (EC), verification gap (VG); none failed. Before triage, at HEAD `3d7c0736`: Release UI-tests build (`--no-incremental`) 0 warnings and 0 errors, UI suite 3618/3618, the 12 matrix-covering cases run directly and pass, gitlink guard exit 0, `git diff --check` clean.

- `maybe-false`, deferred as unverified `medium` (BH1, EC2): Create's message-id guard covers only the status apply. A Continue read-only during the reconcile's `ProjectionEvidenceProvider` await would make the next `_snapshot.Intent.TenantId` throw, and the catch arm would write an Assertive `UnableToVerify` onto the Idle panel (`CreateTenantFlow.razor:590-609`). In the only host, `TenantsWorkspace.razor:188-189`, `OnProjectionRefreshRequested` re-renders the flow before the reconcile reads. ProjectionPending maps to `AuditPending` or `NotStarted` (`TenantCommandAuditStates.cs:75-77`), and neither offers Continue read-only. So this is reached only by a host without that callback. A host-level test that clicks Continue read-only during a held projection-evidence read would settle it.
- `low`, rejected (BH3, EC3): a dropped late result still sets `_focusLifecyclePending` in `RefreshFromAuditControlAsync` (`:316-325`), because Idle reads as NotStarted. It re-focuses the lifecycle section that Continue read-only just focused, so it only matters if the user moved focus during the slow lookup. The fix adds a guard.
- `low`, rejected (BH2, EC1, VG2): the off-dispatcher check-then-act window between the message-id comparison and `SetSnapshot` is a few instructions long. Closing it means moving the apply into `InvokeAsync`, and nobody would meet it in practice.
- `medium`, deferred (VG1): AddMember and ChangeRole (and RemoveMember) still apply a late lookup to the snapshot that Continue read-only reset (`AddTenantMemberFlow.razor:658-660`, `ChangeTenantMemberRoleFlow.razor:717-719`, `RemoveTenantMemberFlow.razor:998-1000`). AddMember then raises the command-activity lease again for an attempt that nothing tracks. This is pre-existing, and the intent closes only the fourteen Review 5 patches.
- `medium`, deferred (BH6, BH4, EC5): a status is applied to an attempt that is already confirmed. The AddMember and ChangeRole merged replay re-runs the lookup after the running refresh confirmed, and their `ApplyStatus` has no terminal guard (RemoveMember's has one at `TenantCreateCommandModels.cs:970`). Create has no in-flight refresh gate, so a slow audit-control lookup can reopen an attempt that a form Refresh confirmed; the message-id guard passes because Confirmed keeps its id. Pre-existing.
- `low`, patch (BH5): the new Metadata follow-up comment says "a close may have reset it". `CloseEditorAsync` (`:583-589`) and `OpenEditorAsync` never reset `_snapshot`; only `OnParametersSet` re-seeds an Idle one.
- `low`, patch (VG3): in `A_zero_event_completion_after_stored_events_keeps_the_event_evidence`, `set.ApplyStatus(staleProcessing)` passes with the pre-fix code. The ProjectionPending arm returns the snapshot unchanged (`TenantSetConfigurationCommandSnapshot.cs:182-186`), so the line and its "next stale status" clause prove nothing.
- `low`, patch (BH14): the new parent-spec provenance bullet for `445e4bad`, `d728ff46`, `55fc6f91` and `e2558361` names no SHAs, contrary to "Declare pointers with full SHAs in prose".
- `low`, patch (BH16): the parent-spec "Review 5 outcome" paragraph says each code fix was mutation-checked. The XML-summary and comment corrections cannot be mutation-checked. The paragraph also omits the closing evidence (build 0/0, UI 3618/3618, guard exit 0), while the Review 5 header still records exit 1. The sub-claim about this spec's mutation list is rejected, because its fix edits this build's spec.
- `false`, rejected (BH8): every `EventsStored or EventsPublished` arm sets `HasCommandEventEvidence = true` whatever the count, so neither a null `EventCount` nor `EventsPublished` as prior evidence changes the sweep's outcome.
- `false`, rejected (BH9): the new fact pins `CompletedWithoutEvents == false`, which is the only input to the AlreadyApplied settle at `TenantSetConfigurationCommandSnapshot.cs:284`. Reverting the fix fails it.
- `false`, rejected (BH10): the four focus ports without a `FocusTarget` precondition still fail when `RefreshFromAuditControlAsync` is reverted. Both the implementation mutation runs and the VG trace show this.
- `low`, rejected (BH11): the duplicated `FocusCalls` / `_lifecycleElement` reflection helpers are test-only. The fix is a cross-file refactor into `FluentBunitContext`.
- `maybe-false`, rejected (BH7): Lifecycle wires `RefreshFromAuditControlAsync` with no focus test. VG traced no polite Refresh path to NotStarted in Lifecycle (its Received/Processing arm keeps the audit state), so at most this is a low test gap in pre-existing code.
- `false`, rejected (BH12): the 2026-10-01 decision named where to record the exception (Spec Change Log, Implementation Notes, the `TenantCommandAuditStates` remarks). The frozen rows change only by renegotiation.
- `false`, rejected (BH13): the two related points ("no proof reader", released `NotStarted`) are recorded verbatim in the Review 5 decision item, and option (1) declined them.
- `false`, rejected (BH15): the `697697f9` note explicitly says "Release UI-tests build", and it claims no source-lane coverage.
- rejected (BH17): the incomplete acceptance and verification lists are in this build's spec.
- `false`, rejected (BH18): the story and sprint status move at the end of this workflow, not in the implementation diff.
- `low`, rejected (EC4): a stale status for the same message id, after Continue read-only and adoption, is the monotonic-lifecycle class rejected in Review 2 BH3 and Review 5 BH3/EC10.
- `false`, rejected (EC6): before the change, a ChangeRole `Completed` with a null count already fell through to the sticky arm. The new `&& !HasCommandEventEvidence` term does not change that path. It is pre-existing and needs a count the platform never sends on `Completed`.
- `low`, rejected (EC7): the AddMember, ChangeRole and RemoveMember merged tests are the `wait_for_its_replay` family, not the three `wait_for_it_then_run_their_own` tests that Review 5 named. This is a pre-existing test weakness that users never meet.

Review 1 outcome: no intent gap or bad spec, so no loopback. The implementation subagent applied the four `low` patches (BH5, VG3, BH14, BH16). The three deferrals are in `deferred-work.md` under "build review of spec-5-4-…-5.md". Re-verified at HEAD `3d7c0736` after the patches: Release UI-tests build (`--no-incremental`) 0 warnings and 0 errors, UI suite 3618/3618, gitlink guard exit 0, `git diff --check` clean.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` -- expected: 0 warnings, 0 errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` -- expected: all passed, zero failed or skipped.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md` -- expected: exit 0.
- `git diff --check` -- expected: clean.
