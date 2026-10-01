---
title: 'Close Story 5.4 review findings'
type: 'bugfix'
created: '2026-10-01'
status: 'ready-for-dev'
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
- [ ] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- Guard the ChangeRole zero-event `Completed` arm with `!HasCommandEventEvidence` so prior evidence uses the existing sticky arm. Update only the `ApplyRemovalProofMatch` XML summary to include kept Delayed and the Unavailable-to-Pending fallback.
- [ ] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationCommandSnapshot.cs` -- Keep `HasCommandEventEvidence` when it is already true or `EventCount > 0`. Set `CompletedWithoutEvents` only when there is no prior evidence and `EventCount == 0`.
- [ ] `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` -- Capture the message id before the status await and return before applying status or projection refresh when it no longer matches. Correct both continue-read-only comments.
- [ ] `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` -- After the in-flight refresh completes, run the follow-up lookup only while the attempt is still refreshable.
- [ ] `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md` -- Record the `AfterTrackingEnds` exception in the Spec Change Log and provenance Implementation Notes. Add `references/Hexalith.Commons` and a provenance note for the three `697697f9` moves.
- [ ] `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandAuditStatesTests.cs` and `tests/Hexalith.Tenants.UI.Tests/Components/{CreateTenantFlowTests,AddTenantMemberFlowTests,ChangeTenantMemberRoleFlowTests,RemoveTenantMemberFlowTests,EditTenantMetadataFlowTests,SetTenantConfigurationFlowTests,RemoveTenantConfigurationFlowTests}.cs` -- Add `Completed` with `EventCount` 0 to the stale-after-stored sweep and expect `AuditPending` for every flow. Hold Create's projection refresh after an unreadable status and assert continue-read-only appears only after release. Assert Create's tracker release before the continue-read-only click, or remove that line. Port the AddMember audit-refresh focus test to the other six flows, and the retry-before-dispatch `FailedWithKey` test to ChangeRole, RemoveMember, and Metadata, asserting `AuditUnavailable`. In the refused-retry tests, wait on the expected `State` and assert the refusal text the flow assigns. After each merged refresh settles, assert the same status-call count once more.

**Acceptance Criteria:**
- Given events were stored, when a later status reports completion with zero events, then every flow keeps the audit record pending.
- Given a refresh is in flight, when the user continues read-only or the attempt is already confirmed, then the late result does not restore the dismissed attempt or reopen the confirmed one.
- Given the parent spec is checked from its baseline to HEAD, when the story gitlink guard runs, then the `697697f9` pointer moves are declared and the guard exits 0.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` -- expected: 0 warnings, 0 errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` -- expected: all passed, zero failed or skipped.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md` -- expected: exit 0.
- `git diff --check` -- expected: clean.
