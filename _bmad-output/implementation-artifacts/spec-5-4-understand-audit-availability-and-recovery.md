---
title: 'Understand audit availability and recovery'
type: 'feature'
created: '2026-09-29'
status: 'in-progress'
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

### Review Findings

Review 2 (2026-09-29): diff `55f3dc63..1cdcc0a9`, spec excluded. Layers: blind hunter, edge-case hunter, verification gap, and acceptance auditor, with no failed layers. At the pinned HEAD, the Release build is 0/0 and the UI suite passes 3487/3487.

- [ ] [Review][Patch] Provenance failures still set the audit state to `AuditUnavailable`. **Decision (2026-09-29): option (a), keep the audit state on provenance failures.** The projection was read, but it did not prove the attempt: missing provenance, missing baseline, a missing target, or no proof reader. Create, AddMember, ChangeRole and Metadata all turn this command outcome into `AuditUnavailable`. The Unavailable explanation then says the audit status "could not be read", although no audit read happened. The Set and Remove configuration flows keep the audit state and wait on a proof mismatch. Choose between two options. (a) Keep the audit state, as the non-collapse rule requires; the matrix sends only failed status and audit reads to Unavailable. (b) Keep `AuditUnavailable`, reading "unverifiable status after dispatch" as covering an unverifiable outcome. Sites: `TenantCreateCommandModels.cs:414,573,784,813,1412,1442`; `EditTenantMetadataFlow.razor:790,798`; `TenantLifecycleCommandFlow.razor:1571` (no `ProjectionEvidenceProvider`). [blind-hunter, acceptance-auditor, edge-case-hunter]
- [ ] [Review][Patch] RemoveMember's proof walk reports failed audit reads as `AuditDelayed`, not `AuditUnavailable`. **Decision (2026-09-29): option (a), real read failures (Unavailable, Error and exceptions) become `AuditUnavailable`; InvalidCursor, Loading, non-current pages, cursor loops, page exhaustion and weak matches stay Delayed; matching is unchanged.** The audit surface kinds Unavailable, Error, InvalidCursor and Loading map to Delayed, as do the catch-all exception arms. Only Unauthorized maps to Unavailable. The matrix says "Status/audit read fails after dispatch → Unavailable (Severe)". However, the frozen block says "RemoveMember keeps its proof walk" and "never change removal-proof matching", and Review 1 (EC10, EC14) chose to broaden the Delayed copy instead. Choose between two options. (a) Map real read failures (Unavailable, Error and exceptions) to `AuditUnavailable`, and keep Delayed for pages that are not ready yet (non-current page, cursor loop, page exhaustion, weak match). (b) Keep the current mapping. `RemoveTenantMemberFlow.razor:1057`, catch arms at the end of `TryAssembleRemovalProofAsync`. [acceptance-auditor, edge-case-hunter]
- [ ] [Review][Patch] Failed projection and proof reads rewrite the audit state to `AuditUnavailable` in Create, Metadata and Lifecycle. Keep `_snapshot.AuditState` on these paths, as Set does with `ProjectionRefreshFailed` and RemoveConfiguration with `ProjectionVerificationFailed`. Inside Lifecycle, a generic proof-read fault already keeps it through `BlockedWithTracking`, while a `TimeoutException` sets Unavailable. Update the new pin `Attempt_deadline_terminalizes_…("proof", … AuditUnavailable)` and add Create and Metadata cases. [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:640; EditTenantMetadataFlow.razor:781,822; TenantLifecycleCommandFlow.razor:1631]
- [ ] [Review][Patch] The retry limit counts clicks that the host merged into a refresh already running. While their own refresh is running, AddMember and RemoveMember (`_refreshInFlight`/`_refreshPending`) and Lifecycle (`_refreshGate`) queue a replay and return immediately. The control counts each such click as an unchanged retry, so three clicks during one slow lookup withdraw Refresh after at most one replayed lookup. Fix: the host's refresh awaits the refresh already in flight, including its replay, before it returns. No new public surface. [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:315]
- [ ] [Review][Patch] Continue read-only drops keyboard focus in AddMember and ChangeRole. `ContinueReadOnlyAsync` sets a polite `Idle()` snapshot, which unmounts the control and the button that had focus, and no focus target is set. MissingSupport after confirmation now puts this button on every successful command. Move focus to the lifecycle section (`_focusLifecyclePending = true`) and add flow tests. [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:629; ChangeTenantMemberRoleFlow.razor:688]
- [ ] [Review][Patch] A retry refused by the activity lease hides the audit state of a possibly delivered attempt in AddMember, ChangeRole and Metadata. `Blocked(...)`/`BlockCurrentSnapshot` set `NotStarted` although `retryMessageId` marks an attempt that may have reached the server. Mirror RemoveMember's `retryMessageId is null ? NotStarted : Unverifiable`, and add one flow test per flow modelled on `Retry_refused_by_the_activity_lease_…`. [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:486; ChangeTenantMemberRoleFlow.razor:545; EditTenantMetadataFlow.razor:656]
- [ ] [Review][Patch] Continue read-only is never wired in Metadata, Lifecycle, SetConfiguration or RemoveConfiguration, although each has a close path (`CloseEditorAsync`/`CloseAsync`). The 2026-09-29 decision lists it for MissingSupport. Wire it only while that close path executes (`!RetainsAttempt`), so it never renders as a dead control. Create has no close path. [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:132; TenantLifecycleCommandFlow.razor:127; SetTenantConfigurationFlow.razor:165; RemoveTenantConfigurationFlow.razor:158]
- [ ] [Review][Patch] On all eight command surfaces the Inspect-audit recovery shows the noun "Audit evidence" / "Preuves d'audit", not the canonical verb. The receipt shows "Inspect audit" / "Inspecter l’audit", and the French button group mixes U+0027 and U+2019 apostrophes. Pass `Tenants.Audit.Availability.Action.InspectAudit` as the entry-point `Label`. Rewrite `Tenants.Audit.EntryPoint.Accessible.Command` in EN and FR so it starts with that label, which keeps WCAG 2.5.3 satisfied. [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:279 and the seven sibling flows]
- [ ] [Review][Patch] `EscalatesFailedRead` restates the page's escalation `@if` condition instead of the markup using it, so the two can drift apart and duplicate or drop escalation. The receipt-suppression test covers only the Unavailable kind. Render the page link from `EscalatesFailedRead`, and add an Error case to `Unavailable_audit_read_escalates_once_from_the_page_never_again_from_its_receipt`. [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:191,470]
- [ ] [Review][Patch] Test gap: no Metadata flow test asserts its own audit assignments. These are submission `FromSubmission`, the lease or block `NotStarted`, and in-flight loss `Unverifiable`. Reverting `:695` to a hard-coded `AuditUnavailable` leaves every test green. Extend the gateway-failure tests with audit-state and control-absence assertions, plus one `Failed` case that carries a MessageId. [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:695]
- [ ] [Review][Patch] Test gap: in AddMember, ChangeRole, Metadata, Lifecycle and RemoveConfiguration, no rendered test checks whether Refresh shows (`CanRequeryAudit`). Forcing it to true or false leaves the suites green. Add assertions that `[data-recovery-verb='refresh']` is present in a re-queryable state and absent otherwise. [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:277; ChangeTenantMemberRoleFlow.razor:317; EditTenantMetadataFlow.razor:323; TenantLifecycleCommandFlow.razor:401; RemoveTenantConfigurationFlow.razor:456]
- [ ] [Review][Patch] Test gap: no test asserts that operator abandonment stays `AuditUnavailable` in the configuration flows. Routing Abandon through `retentionExpired: true`, or collapsing the RemoveConfiguration tracker-refusal ternary to `ExpireRetention()`, leaves the suites green. Add `AuditState` assertions to an abandon test in each flow, and to a tracker refusal of a live RemoveConfiguration attempt. [src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor:850; RemoveTenantConfigurationFlow.razor:700]
- [ ] [Review][Patch] Test gap: two of Set's three projection-failure sites are untested. They are a throwing `OnProjectionRefreshRequested` and a cancelled `ProjectionEvidenceProvider`. Reverting either to `AmbiguousSubmission` brings back the Review 1 VG4 collapse, and no test fails. Add both cases to `A_failed_projection_read_after_events_are_stored_keeps_the_audit_record_pending`. [src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor:776,796]
- [x] [Review][Defer] RemoveMember's proof walk with no match leaves the audit state pending indefinitely [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1151] — deferred: pre-existing, and the frozen block keeps the proof walk. The new retry limit then withdraws Refresh after three unchanged walks.
- [x] [Review][Defer] A 404 status lag (`Pending`, null `Status`) is reported assertively as UnableToVerify plus `AuditUnavailable` in Create, AddMember, ChangeRole, RemoveMember and Metadata [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:255] — deferred: pre-existing at baseline. The command dimension mishandles the lag the same way; only Lifecycle and the configuration flows treat `IsPending` as a wait.
- [x] [Review][Defer] The Unavailable (Severe) badge uses `Tint`, while DESIGN.md asks for `Filled` for Danger and Severe [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:27] — deferred: the spec specified Tint, following `TruthStateBadge`, whose Severe (stale) badge is also Tint. Aligning with DESIGN.md needs one decision for the whole repo.

#### Rejected

- BH2/AA5, "Completed with zero or null events is Unavailable in Lifecycle and the configuration flows": low. These flows treat an eventless Completed as an unverifiable status, which the canonical table maps to Unavailable, and a test pins it deliberately. EventStore always sends a count with Completed, so the case is anomalous.
- BH3/EC5/EC6, "the audit state regresses on a late Received/Processing or Completed(0)": low. It needs refreshes to apply out of order, and the command state already regresses the same way (pre-existing). Completed(0) after EventsStored contradicts EventStore semantics. The fix adds branches to five snapshots.
- BH4, "Refresh and Continue read-only buttons in the flow and page duplicate the limited control": low. These are pre-existing command and page controls, and the limit note is scoped with "here". Removing them is a design change.
- BH6, "`CanRequeryAudit` disagrees with `CanRefresh`": false. The divergence is intentional; for example, RemoveMember's Refresh after confirmation walks the proof again (`TryAssembleRemovalProofAsync`). The `_isRefreshing` case is covered by the retry-limit patch.
- BH7/EC2/EC3/EC4, "focus races or pull-back around the state line": low. Every target is inside the same lifecycle panel, the triggers are rare, and a fix needs JS checks on the active element.
- BH8, "the focus target is inside the live region, so focus plus announcement reads the state twice": low. The double read happens only on a refresh that removes Refresh; moving the target means restructuring the control.
- BH9, "the retry limit resets when the state flips or the control remounts": false. The spec says "a state change resets the count", and the control remounts only through NotStarted.
- BH10, "no busy feedback while a refresh runs": low. Refresh is fast, and ignored clicks are harmless.
- BH12, "`aria-label` on `fluent-badge`, and duplicate region names": low. This is the repo's `TruthStateBadge` pattern, and the section label predates the story (Review 1 BH15).
- BH13, "the browser fixture's inline layout is hand-written": low. A class-hook guard already exists; generating the fixture from rendered markup is test-infrastructure work.
- BH15, "the deferred-work live-region fix is scoped too narrowly": false. The entry describes the target behaviour; an implementation of it covers the flow wrappers.
- AA6, "the helper departs from the frozen table without a Spec Change Log entry": rejected, because the fix is a spec edit. Received/Processing → NotStarted follows the rule that pending exists only between events-stored and confirmation.
- AA7, "Lifecycle reports Delayed before dispatch when retention expires": low. Only a preflight longer than the retention window reaches it, and the fix needs a first-dispatch/retry branch.
- AA9, "the retry-limit note names retrying": false. The spec requires a limit note, and it names an action that is withdrawn, not offered.
- AA10, "spec status `done` against sprint `review`": rejected, because the fix is a spec edit. The status sync after this review sets both.
- EC11, "collapsing the state accordion hides the page escalation while the receipt suppresses its own": low. The accordion is expanded by default; the fix needs expansion-state tracking.
- EC12, "clock skew labels a tracker refusal as Delayed": low. It needs a clock earlier than the attempt start, and the fix adds a branch.
- EC13, "zero recoveries render with no explanation after the no-recovery note was deleted": low. The spec deleted the variant copy on purpose, the state explanation still renders, and the Continue read-only patch reduces the case.
- EC16, "a client status timeout should be Delayed": false. The spec's Delayed row means `CommandStatus.TimedOut`; a failed status read is the Unavailable row. The Lifecycle proof-read timeout is covered by the projection-read patch.

### Review Findings (Review 3)

Review 3 (2026-09-29): delta `1cdcc0a9..e077e65e` only, spec excluded. The user chose delta-only because `git diff 1cdcc0a9 e077e65e -- src tests` is empty. The delta covers `deferred-work.md`, `sprint-status.yaml`, three gitlinks, and the Builds inner diff `0610f783..85ca19bc`. Layers: blind hunter, edge-case hunter, verification gap, and acceptance auditor, with no failed layers. Checked at the pinned HEAD `e077e65e`:

- Release UI-tests build: 0 warnings, 0 errors.
- UI suite: 3487/3487. FrontComposer `f86fb730` compiles from source by default through `HexalithFrontComposerFromSource`.
- Debug source-lane build of `Hexalith.Tenants.UI` against the EventStore and FrontComposer submodule source: 0 errors.
- The three new SHAs are on their `origin/main`.

All thirteen Review 2 `[Review][Patch]` items above are still open and unchanged at HEAD. They are carried forward, not filed again.

- [ ] [Review][Patch] Treat a 404 status lag as a wait in the audit dimension. **Decision (2026-09-29): option (b)** — the user renegotiated the frozen row so that a 404 before the first status is a wait. The 404 arrives as `TenantCommandStatusResult.Pending` (null `Status`, `IsPending`).
  - Today Create, AddMember, ChangeRole, RemoveMember and Metadata set `AuditUnavailable` on it (`TenantCreateCommandModels.cs:255` and siblings), while Lifecycle (`:1725`) and Set/Remove configuration (`TenantSetConfigurationCommandSnapshot.cs:157`, `TenantRemoveConfigurationCommandSnapshot.cs:139`) keep the audit state.
  - Spec: amend the frozen canonical-derivation row to read "Missing, unknown, or unverifiable status after dispatch → `AuditUnavailable`; a 404 before the first status is a wait that keeps the current audit state." Add a Spec Change Log entry that cites this decision.
  - Code: in the five `ApplyStatus` null-status arms, keep `AuditState` when `status.IsPending` instead of `FromCommandStatus(null)`. The command dimension is out of this decision's scope; it still shows UnableToVerify on the lag, so narrow the Review 2 404-lag deferral to that dimension.
  - Leave `IsRetryableFailure` unchanged: Lifecycle and configuration treating it as a wait is a separate divergence from the matrix row "Status/audit read fails after dispatch → Unavailable", and this decision does not cover it.
  - Tests: add one `IsPending` case per flow to the snapshot tests, asserting `AuditState` is unchanged from NotStarted and from EventsStored, and extend the canonical-derivation table test. [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:255]
- [ ] [Review][Patch] Declare the three submodule pointer moves carried by `e077e65e` in a `## File List`, following the 5.3 D1 precedent: bare list items `references/Hexalith.Builds`, `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer`, and a provenance note with no `->` arrows.
  - Say that `e077e65e` carried them as external `origin/main` drift: Builds G-6 toolchain, EventStore 6.5c, FrontComposer 13.3. Its commit message omits them.
  - Record the re-verification at `e077e65e`: Release 0/0, UI 3487/3487, and the source-lane build.
  - Correct the Implementation Notes claim "the gitlink guard passes".
  - Rerun `python3 scripts/validate-story-gitlinks.py` on this spec. It exits 1 today with `[UNDECLARED]` Builds `0610f78`, EventStore `801f3e5` and FrontComposer `7d3af61`, and it passes with `--ref 1cdcc0a9`.
  - Do not commit the working tree's uncommitted EventStore move (`cc79c03a..771269ae`, pulled at 18:05 by another session) with the story unless it is declared too. [_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md:1]
- [ ] [Review][Patch] Correct the Review 2 deferred-work entry "unmatched RemoveMember proof walk" in three places. Keep it deferred. [_bmad-output/implementation-artifacts/deferred-work.md:3274]
  - The trigger: the Pending case is the last page (`!audit.HasMore`) with no weak match (`RemoveTenantMemberFlow.razor:1122`). Running out of the page budget is already Delayed (`:1137`), so "exhausts every page" is the wrong description.
  - The "retention window" remedy: RemoveMember has no retention or expiry concept.
  - The missing dead end: the Pending recovery set has no Escalate, so once the retry limit removes Refresh, only Inspect audit is left.
- [x] [Review][Defer] Nothing in CI runs `validate-story-gitlinks.py` against a changed story file. `.github/workflows/story-guards.yml` runs only the guard's regression suite, so the three undeclared moves passed CI [.github/workflows/story-guards.yml:35] — deferred: this is repo-wide infrastructure already tracked at `deferred-work.md:3216` (owner decision 2026-09-27). It is the seventh occurrence (Story 5.4, `e077e65e`).
- [x] [Review][Defer] The Builds G-6 validator pins CommunityToolkit.Aspire.Hosting.Dapr `13.5.1-beta.767`, but the Builds catalog pins `.770` (`Props/Directory.Packages.props:151`). A real `validate_baseline` run fails with central package pin drift. The self-test builds its catalog from `EXPECTED_TUPLE`, so CI stays green [references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py:21] — deferred: Builds-owned, and no Tenants workflow runs this validator. Hand off to Builds.
- [x] [Review][Defer] Builds 6-1 record claims the 2026-09-06 baseline "remains byte-identical at SHA-256 `525615c6…`". It hashes to `62a6a555…` at both `0610f783` and `85ca19bc`. The record also claims the baseline matches the central catalog, and its "review the fresh packet" and "seven bound changes remain uncommitted" statements are stale [references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md:2379] — deferred: Builds-owned documentation. Hand off to Builds.
- [x] [Review][Defer] The historical 2026-09-06 baseline no longer validates. The validator and schema hard-code `approvedOn == "2026-09-27"`, and `README.md:269` still points at `Tools/runtime-toolchain-baseline.json` [references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py:240] — deferred: Builds-owned. Hand off to Builds.
- [x] [Review][Defer] G-6 candidate and accepted evidence cannot be told apart. Accepted mode differs from candidate mode only in the `status` string, and requires no named acceptance record. The schema's `status` enum `["pending","accepted"]` has no mode condition [references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py:307] — deferred: Builds-owned governance. Hand off to Builds.
- [x] [Review][Defer] The G-6 self-test's new mode checks assert only `returncode == 1`, so a verified crash mutant passes. They use bare `assert` statements and print a literal "22 scenarios" [references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py:289] — deferred: Builds-owned test efficacy. Hand off to Builds.
- [x] [Review][Defer] The 2026-09-27 baseline certifies Aspire SDK `13.5.4` while five AppHosts, including `Hexalith.Tenants.AppHost`, audit at `13.5.3`, with no `aspireSdk` disposition. It also has case-duplicate `dapr`/`Dapr` disposition keys [references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json:12] — deferred: Builds-owned disposition. The Tenants AppHost SDK bump is not in this story's scope.

#### Rejected (Review 3)

- AA1–AA9, "the Review 2 patches are unimplemented" (provenance → Unavailable, RemoveMember read failures → Delayed, projection-read collapse, Continue read-only focus and wiring, lease-refused retry, retry-bound merged clicks, Inspect-audit label, `EscalatesFailedRead`, four test gaps): duplicates. They are already open as Review 2 `[Review][Patch]` items and unchanged at HEAD.
- BH, "nothing verifies the build after the bumps": false. At `e077e65e`: Release 0/0; UI 3487/3487 against FrontComposer `f86fb730` built from source; the Debug source-lane Tenants.UI build against EventStore and FrontComposer source passes. The missing record is covered by the File List patch.
- BH, "the EventStore `APP_API_TOKEN` breaking change needs a Tenants note": false. The token is provisioned on the receiving EventStore gateway and its sidecar, not on the caller. Tenants CI and production use the EventStore package, not this pin. `deploy/dapr` holds no EventStore workload, and the AppHost runs Development. EventStore documents the step (`7509d952`).
- BH, "the EventStore pin lands on the in-progress 6.5c commit `cc79c03a`": low. It is a status-only commit on EventStore `origin/main`, the build and suite are green, and choosing a different pin is not a direct correction.
- BH and AA, "the Tint-vs-Filled deferral names no owner and has no Spec Change Log entry": rejected. The fix edits the spec or DESIGN.md, and the entry already asks for the single repo-wide decision. All 9 `BadgeAppearance` uses are Tint.
- BH, "the 404 lag plus the retry limit strands Refresh": low. The flow's command-level Refresh still re-queries status, and a changed state resets the count. It is folded into the 404 decision.
- EC, "`capturedUtc` is not compared with `approvedOn`": low. Builds-owned; it needs evidence captured before approval, and the fix adds a guard.
