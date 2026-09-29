---
title: 'Understand audit availability and recovery'
type: 'feature'
created: '2026-09-29'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 55f3dc63b6ce10bb0afdf929d026fa07cffd9105
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shared audit availability model and control exist, but the command flows derive the audit dimension inconsistently. A rejection or an undispatched block shows `audit unavailable` or `missing implementation support`, and timeouts and publish failures map differently per flow. The control explains only two of the four states, uses ad hoc glyphs (unavailable and missing support share `?`), never offers escalation, lets retry repeat without limit, and can promise an action it does not render. The flows also repeat flow-local state text beside it.

**Approach:** Reverify Story 5.4 against the current epic. Keep the typed model and one shared control. Give every state an explanation, the DESIGN role color and icon, a canonical recovery set, configured escalation, and bounded retry. Route every tenant command flow's audit dimension through one canonical derivation, so command, projection, and audit truth never collapse.

## Boundaries & Constraints

**Always:**
- `audit available` only from a complete redacted receipt (`TenantAuditReceipt` state Ready) matched to the attempt.
- Four incomplete states stay distinct, get no success styling, copy, or announcement, and never enable correction.
- Canonical derivation:
  - EventsStored, EventsPublished, or Completed → `AuditPending`.
  - TimedOut, PublishFailed, or retention-window expiry → `AuditDelayed`.
  - Missing, unknown, or unverifiable status after dispatch → `AuditUnavailable`.
  - Rejected, or any no-dispatch outcome (Blocked, AlreadyApplied, DuplicatePrevented) → `NotStarted`.
- **Decision (2026-09-29):** in Create, AddMember, ChangeRole, Lifecycle, SetConfig, and RemoveConfig, a projection confirmation without attempt-specific proof → `MissingSupport`. The same applies to Metadata when its evidence check finds no Ready matching row. The explanation names "in-panel audit verification", and the recovery is Continue read-only, Inspect audit, and Escalate. RemoveMember keeps its proof walk. `audit pending` exists only between events-stored and confirmation.
- Recovery reuses existing status, audit, and navigation paths and performs only the named action.
- Escalation goes only to the configured local `Tenants:Audit:EscalationRecoveryHref`.
- Pending and delayed are announced politely; unavailable and missing support assertively. The announced text holds only state and explanation, never the actions.
- Strings are Tenants EN/FR whole strings with accents; selectors are stable and independent of culture.

**Never:**
- Promote audit state from command status, EventCount, projection confirmation, or SignalR.
- Redispatch, submit a correction, add endpoints, or edit history.
- Name recovery verbs in state copy.
- Change the global-administrator page, the correction panels, removal-proof matching, receipt field derivation, audit paging, or locking.
- Use `undo`, `rollback`, or `hidden edit`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Pending | Events stored/completed, no proof | Informative `ClipboardClock` badge and explanation; Refresh, Inspect audit | Polite; no proof claim |
| Delayed | TimedOut, PublishFailed, retention expiry | Warning `ClockWarning`; Refresh, Inspect audit, Escalate | Distinct from unavailable and command failure |
| Unavailable | Status/audit read fails after dispatch | Severe `DocumentProhibited`; Refresh, Continue read-only, Inspect audit, Escalate | Assertive once; no "never exists" claim |
| Missing support | Capability not built, including confirmed-without-proof per the decision | Subtle `ClockToolbox`; names capability; Continue read-only, Inspect audit, Escalate | Never shown as empty data or transient error |
| Not started | Rejected or undispatched | Control hidden; command dimension shows the outcome | No audit state implied |
| Retry bound | 3 refreshes leave state unchanged | Refresh removed, limit note, focus to state line | Other recoveries remain |
| No escalation config | Href absent or non-local | No Escalate control | No dead or external link |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` -- the typed model (`FromCommandAuditState`, verbs, politeness). Extend it with verb sets, state label keys, and retry-bound constants.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor{,.css}` -- the shared control. Replace the glyphs with `FluentBadge` (Tint, color, and `IconStart` following the `Components/Shared/TruthStateBadge.razor` pattern). Delete the `RecoveryCopySuffix` variant machinery, which lets MissingSupport show `.RefreshOnly` "retry" copy with no Refresh control. `Wait` stays copy-only.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- the status switches for Create (~272), AddMember (~496), ChangeRole (~709), RemoveMember (~978), Metadata (~1329, and Available at ~1447 through `IsMatchingUpdateAuditProof` ~1470 with no Ready-receipt gate), and Lifecycle (~1848, where retention expiry maps to Unavailable at ~2005). MissingSupport comes from `Blocked` (~185/440/862/1245) and from AlreadyApplied/DuplicatePrevented (~903/913/1563/1576).
- `src/Hexalith.Tenants.UI/State/TenantCommands/Tenant{Set,Remove}ConfigurationCommandSnapshot.cs` -- the configuration status and retention-expiry arms.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` -- MissingSupport at ~514 (a block) and ~585 (in-flight loss, which becomes Unavailable). `AuditEvidenceProvider` ~248.
- The eight flows `{CreateTenantFlow,Members/{Add,ChangeTenantMemberRole,RemoveTenantMember}Flow,Metadata/EditTenantMetadataFlow,Lifecycle/TenantLifecycleCommandFlow,Configuration/{Set,Remove}TenantConfigurationFlow}.razor` -- `AuditText` → `AvailabilityText` and the entry-point accessible name duplicate the state with flow-local keys.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` (~90-97, 388-396) -- receipt → shared control. `TenantAuditPage.razor` ~191 already renders the page escalation link, and ~1959 `SafeRecoveryHref`/`CanonicalLocalHref` should be reused for escalation.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:21` -- `AuditEscalationRecoveryHref`.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources{,.fr}.resx` -- the `Tenants.Audit.Availability.*` keys (unaccented in FR) and the flow-local `Tenants.<Flow>.Audit.*` keys.
- `tests/Hexalith.Tenants.UI.Tests/` -- `State/TenantAuditAvailabilityTests`, `Components/AuditAvailabilityStateTests` (pins variant keys and glyphs), the `State/*CommandSnapshotTests` InlineData for Rejected/MissingSupport, `TenantsUiCompositionTests` ~1582 (parity), and the flow and receipt tests.

## Tasks & Acceptance

**Execution:**
- [x] `State/TenantAudit/TenantAuditAvailability.cs` -- set the verb sets (Pending: Wait, Refresh, InspectAudit; Delayed: + Escalate; Unavailable: Refresh, ContinueReadOnly, InspectAudit, Escalate; MissingSupport: ContinueReadOnly, InspectAudit, Escalate). Add `MaximumUnchangedRetries = 3` and a state-label key helper. Add a canonical `TenantCommandAuditStates` helper in its own file.
- [x] Snapshot files above -- route every status, retention, block, and already-applied arm through the helper. Gate Metadata `AuditAvailable` on a Ready `TenantAuditReceipt.FromRow`. Apply the confirmed-without-proof decision.
- [x] `AuditAvailabilityState.razor{,.css}` -- add the badge, one explanation per incomplete state, and a live region around only the state and explanation. Render Escalate as an anchor to the safe configured href (a shared helper extracted from `TenantAuditPage`). Add the retry bound, with focus moving to the state line (`tabindex=-1`); a state change resets the count. Always render the recovery testids (default prefix `tenants-audit-availability-recovery`) and the `Source` → `data-audit-source` attribute. Update forced colors and responsive stacking.
- [x] The eight flows and `AuditEvidenceReceipt.razor` -- drop the duplicate `AvailabilityText`, use the shared label in accessible names, and pass `Source`. The receipt suppresses Escalate where its host page already escalates the failed read.
- [x] EN/FR resx -- add the Pending/Delayed explanations and an accented FR set. Delete the unused variant and flow-local `.Audit.*` keys.
- [x] Tests -- cover every matrix row plus the derivation table, non-collapse, the Ready-only transition, the retry bound with focus, escalation href safety, EN/FR parity, and live-region scope. Rewrite the vacuous glyph and variant tests. Extend the browser harness with a 390px availability fixture.

**Acceptance Criteria:**
- Given any command outcome, when the audit dimension is derived, then it follows the canonical table, and no command, projection, EventCount, or SignalR input yields Available.
- Given a recovery action, when it runs, then only the named existing path executes, and scope and focus are preserved.
- Given keyboard, screen reader, forced colors, or a 390px width, when the control renders, then badge, text, and icon convey the state, the actions stack, and repeated identical renders produce no repeat announcement.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -m:1 --no-restore` -- 0 warnings, 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -parallelMode none` -- all pass.
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` -- passes.
- `git diff --check`; `python3 scripts/validate-story-gitlinks.py` -- clean.

## Implementation Notes

- **Canonical derivation:** `State/TenantCommands/TenantCommandAuditStates.cs` owns every audit arm (`FromCommandStatus`, `FromSubmission`, `FromConfirmationEvidence`, and the `NotStarted`/`EventsStored`/`Delayed`/`Unverifiable`/`ConfirmedWithoutProof` constants). All eight snapshots and the flow-level assignments route through it.
- **Accepted is `NotStarted`:** receipt or processing proves nothing is stored, so per "audit pending exists only between events-stored and confirmation" the control stays hidden until EventsStored/EventsPublished/Completed.
- **Submission outcomes:** rejected and definite `Failed` submissions are `NotStarted` (nothing was dispatched); only an ambiguous submission is `AuditUnavailable`. A RemoveMember retry refused by the activity lease is `AuditUnavailable` because its reused identity may already have reached the server. A `Completed` status with zero events is `NotStarted` (an unreported count stays pending).
- **Retention expiry vs. abandonment:** `TenantLifecycleCommandSnapshot.StatusTimedOut()` (status/dispatch timeouts and retention expiry) and `Tenant{Set,Remove}ConfigurationCommandSnapshot.ExpireRetention()` yield `AuditDelayed`; an operator `Abandon()` stays `AuditUnavailable`. The configuration flows release both through one `ReleaseAttemptAsync(retentionExpired)` path.
- **Non-collapse:** projection-read failures never rewrite the audit dimension (`TenantRemoveConfigurationCommandSnapshot.ProjectionVerificationFailed` and the Set flow's `ProjectionRefreshFailed`). A timed-out status after stored events delays audit without changing the RemoveConfiguration lifecycle. Tracker refusals of expired attempts map to retention expiry (`ExpireRetention()` / `StatusTimedOut()`).
- **Metadata gate:** `AuditAvailable` requires `TenantAuditReceipt.FromRow(matchingRow).State is Ready`; provenance alone still confirms the projection but yields `MissingSupport`.
- **Shared control:** `FluentBadge` (Tint, DESIGN role color, Size20 per-state icon), one explanation per incomplete state, a live region (`tenants-audit-availability-announcement`) holding only state and explanation, default recovery test-id prefix `tenants-audit-availability-recovery`, `Source` → `data-audit-source`, escalation anchored to `TenantAuditNavigationSafety.SafeRecoveryHref` (extracted from `TenantAuditPage`), and the 3-refresh bound (`TenantAuditAvailability.MaximumUnchangedRetries`) with focus to the `tabindex=-1` state line described by the limit note. `OnEscalate` and the `RecoveryCopySuffix` variants are gone. Fluent-root rules use `::deep` from the plain-HTML root.
- **Recovery wiring:** Refresh binds only while the attempt can be re-queried (tracking handle or retained attempt); clicks during an in-flight refresh are ignored; focus moves to the state line whenever a refresh leaves no Refresh control. No Inspect-audit fragment is passed when audit read is denied. Escalate reuses `Tenants.Audit.Recovery.Action.Escalate` ("Escalate without diagnostics").
- **Flows:** each passes a distinct `Source` (`create-tenant`, `add-member`, `change-role`, `remove-member`, `edit-metadata`, `lifecycle`, `set-configuration`, `remove-configuration`); the entry point's accessible name uses the shared state label and `AvailabilityText` was removed from `AuditEvidenceEntryPoint`. Flows that already exposed Continue read-only keep wiring it; the others render the recoveries whose paths exist.
- **Receipt:** passes `Source="audit-receipt"` and `SuppressEscalation` from the new `HostEscalatesFailedRead` parameter, which `TenantAuditPage` sets whenever its own escalation link renders for the failed read.
- **Resources:** added `Reason.Pending`, `Reason.Delayed`, `RetryLimit`; rewrote the explanations so no copy names a recovery verb; accented the FR availability set; deleted the `Accessible.*`, `.NoEscalation`/`.NoRecovery`/`.RefreshOnly`, `Action.Wait`, receipt `.Accessible`, and eight flow-local `.Audit.*` key families. `Tenants.Audit.EntryPoint.Accessible.Command` now reads "Open audit evidence for tenant {1} ({0})".
- **Browser harness:** loads the Fluent bundle stylesheet (FluentStack layout lives there) plus the compiled availability CSS; the 390px run asserts stacked full-width recoveries, the desktop run one row, and a new mutation run proves removing the responsive block is rejected.

- **Review 1 refinement (orchestrator):** `FromSubmission` maps a non-ambiguous `Failed` result to `NotStarted` only when it carries no message ID (pre-dispatch validation or an unavailable gateway). A `Failed` result that carries the message ID was reported by the gateway after the POST was sent, including its "failed before it could be verified" fallback, so it may have reached the server and stays `AuditUnavailable`. `Submission_outcomes_follow_the_canonical_table` and `RemoveTenantMemberFlowTests.Retry_refused_by_the_activity_lease_reports_the_possibly_delivered_attempt_as_unverifiable` pin both cases. Final verification: Release build 0/0, UI suite 3487/3487, Chrome 154 harness passes, including the three mutation rejections; `git diff --check` is clean; the gitlink guard passes.

## Spec Change Log

## Review Triage Log

Review 1 (2026-09-29), diff `55f3dc63..working tree` (spec excluded). Layers: blind hunter (BH), edge-case hunter (EC), verification gap (VG).

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1/EC2 Completed with zero events shows pending | medium: `FromCommandStatus(Completed)` ignores `EventCount`; Set configuration marks `CompletedWithoutEvents` yet shows "events are stored" pending until confirmation (EventCount is Completed-only). | patch |
| BH2/EC12 definite submission failure shows unavailable | medium: `FromSubmission` maps non-ambiguous `Failed` (for example `UnavailableTenantCommandGateway`, nothing sent) to `AuditUnavailable`; the spec sends no-dispatch outcomes to `NotStarted`. | patch |
| BH3/EC7 empty Inspect audit shell when audit read is denied | medium: every flow passes a non-null `InspectAuditAction` whose body is `@if (!AuditReadDenied)`, so `CanRenderRecovery` emits an empty `inspectaudit` shell, now also on the success-path MissingSupport. | patch |
| BH4 no request-permission recovery | low: the frozen recovery sets exclude it; the audit page keeps its permission link. | reject |
| BH5/EC8 success announced assertively with Escalate | low: frozen decision (2026-09-29) chose MissingSupport with Escalate after confirmation, with the assertive consequence disclosed. | reject |
| BH6/EC4 retry bound counts concurrent clicks | medium: `RefreshAsync` has no in-flight guard; a second click while the host refresh runs returns immediately unchanged and increments the count. | patch |
| BH7a refresh that removes Refresh drops focus | medium: Pending → Retry → confirmation → MissingSupport removes the focused Refresh while the control stays rendered; only the retry bound moves focus. | patch |
| BH7b control unmounts on Processing after Unavailable | low: needs a null status followed by Processing; the unmount cannot be handled by the control and a flow fix adds branches. | reject |
| BH7c command entry point hidden during Accepted/Processing | low: consistent with the frozen rule that pending starts at events-stored; no audit record exists earlier. | reject |
| BH8 live region inserted with its first content may not announce | maybe-false: same insertion pattern as the baseline control, and flows keep their own live regions; settle with an NVDA check of NotStarted → Pending. Medium if true. | defer |
| BH9 Escalate lost support-safe wording and has two labels | medium: the control renders bare "Escalate" while the page's link to the same destination reads "Escalate without diagnostics"; the removed copy carried the support-safe guidance. | patch |
| BH10 receipt pending/delayed/missing-support copy is command wording | false: production `FromRow` callers pass only `NotStarted` (audit page) or `AuditAvailable` (removal proof), so those receipt states are unreachable. | reject |
| BH11 flow Recovery.* strings still name verbs beside the control | medium: pre-existing flow lifecycle copy (for example `Tenants.EditMetadata.Recovery.Degraded`), outside this story's changes. | defer |
| BH12 French label not contained in accessible name | medium: visible `Preuves d'audit` (U+0027) is not a substring of `Ouvrir les preuves d’audit …` (U+2019), breaking WCAG 2.5.3 introduced by the rewritten name. | patch |
| BH13/VG5 browser fixture not tied to rendered markup | medium: the harness measures a hand-written fixture (unavailable attributes with MissingSupport strings, wrapper not inner control); renaming the rendered class hooks leaves every check green. | patch |
| BH14 `data-audit-source` on host and entry point | low: spec-mandated attribute; no production selector or script reads it. | reject |
| BH15 state label repeated by section and badge | low: the section label predates this story and the badge name equals its text. | reject |
| BH16 `EventsStored` constant documented as unconfirmed | low: RemoveMember uses it after confirmation; a doc correction is direct. | patch |
| BH17 service lookup and recomputation per render | low: negligible cost, same injection pattern as `AuditEvidenceEntryPoint`. | reject |
| BH18 `SafeRecoveryHref` lacks direct unit tests | low: moved code with unchanged behavior, covered through component cases. | reject |
| BH19 epic context header and 5.1 readiness wording | low: generated planning context, no product effect. | reject |
| BH20 spec untracked and old 5.4 story not superseded | false: the spec is excluded from the review diff by design and the historical story is an archive. | reject |
| BH21 flow-hosting test depends on attribute order | low: brittle but correct; rewriting to rendered markup is more than a direct fix. | reject |
| EC1/VG6 Refresh rendered when the host refresh is a no-op | medium: ambiguous Create failures lack a correlation id and configuration attempts after expiry or abandon fail `RetainsAttempt`, so the host returns at once while clicks exhaust the bound with a misleading note. | patch |
| EC3 receipt retry bound never reached | low: the receipt control unmounts during Loading; a fix needs lifted state and each retry is a real audit read. | reject |
| EC5/EC6 tracker-rejected expired attempt shows unavailable | medium: both trackers refuse expired snapshots; RemoveConfiguration then calls `Abandon()` and Lifecycle `TrackingMismatch`, both `AuditUnavailable` instead of retention-expiry `AuditDelayed`. | patch |
| EC9 Metadata non-Ready match says verification unsupported | low: follows the frozen decision for Metadata without a Ready matching row. | reject |
| EC10 Delayed explanation names the wrong cause | medium: RemoveMember proof-read failures set Delayed, whose copy claims a status timeout, publish failure, or tracking-window expiry. | patch |
| EC11 blocked retry of a possibly delivered removal hides audit | medium: `RemoveTenantMemberFlow` sets `NotStarted` although `retryMessageId` marks an attempt that may have reached the server. | patch |
| EC13 RemoveConfiguration timeout after events stays pending | medium: the `TimedOut when HasCommandEventEvidence` arm keeps `AuditPending`, contradicting the canonical TimedOut → Delayed row. | patch |
| EC14 removal proof walk bypasses the helper | false: the frozen block forbids changing removal-proof matching. | reject |
| VG1 configuration retention expiry untested through flows | medium: reverting `retentionExpired: true` leaves the flow tests green. | patch |
| VG2 lifecycle timeout sites untested for Delayed | medium: reverting any `StatusTimedOut()` site leaves the flow tests green. | patch |
| VG3 rejected submission → NotStarted untested in six flows | medium: only Create and Lifecycle assert it. | patch |
| VG4 Set configuration projection failure rewrites audit | medium: `SetTenantConfigurationFlow.razor:770,790,800` route projection-read failures through `AmbiguousSubmission`, now `AuditUnavailable`, unlike the RemoveConfiguration non-collapse. | patch |
