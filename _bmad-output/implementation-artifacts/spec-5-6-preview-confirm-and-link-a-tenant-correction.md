---
title: 'Preview, confirm, and link a tenant correction'
type: 'feature'
created: '2026-10-02'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 11e65e37f0fbf6649642a512052eebd37d50166d
context:
  - AGENTS.md
  - _bmad-output/project-context.md
  - _bmad-output/implementation-artifacts/epic-5-context.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The historical preview can dispatch from stale authority, confirm from a matching role alone, lose an in-flight attempt, and link unrelated audit as proof. It does not meet the current Epic 5 contract.

**Approach:** Complete Story 5.5's handoff with a current-state preview, recheck before one forward command, retain and reconcile its attempt, and link receipts only with deterministic evidence.

## Boundaries & Constraints

**Always:** Preserve original evidence and last-confirmed projection; require explicit role, current authority, safe width, complete preview, and fresh confirmation-time checks. Reuse the command gateway, one ULID attempt, aggregate lock, status lookup, and projection version or attempt provenance. Separate command, projection, and audit states. Localize EN/FR copy and return focus.

**Never:** Infer historical role; double-dispatch; confirm from role alone or link from target/time coincidence; mutate history; add endpoints or public-contract changes; expose raw tracking data; enable global-admin correction.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Eligible restore/change | Fresh preview, authority, explicit role | One add/change command; confirm matching postcondition plus advanced version or attempt provenance | Keep original and lock until terminal evidence |
| Conflict/already applied | Target/role, tenant, authority, width, or admission changes | Re-derive supported command or block; same state sends nothing | Show safe reason and recovery |
| Ambiguous/rejected | Request may have reached gateway, or domain rejects | Retain ID; safe status/same-ID retry; no false success | Distinct lifecycle and focus |
| Proof unavailable | Audit row lacks deterministic attempt link | No paired receipt link; projection confirmation stays separate | Show actual audit availability and recovery |

</frozen-after-approval>

## Code Map

UI paths below are relative to `src/Hexalith.Tenants.UI/`.

- `Components/Pages/TenantAuditPage.razor` — retain handoff and submitted preview across page/viewport changes; reuse focus/refresh ownership.
- `Components/Tenants/Audit/CorrectionStartPanel.razor`, `State/TenantAudit/TenantCorrectionPreviewSnapshot.cs` — replace cached dispatch, role-only confirmation, and weak proof; split `TenantCorrectionProofLink` into its own file.
- `State/TenantCommands/{TenantAggregateCommandAdmissionGate,TenantCommandAggregateLock,TenantLifecycleAttemptTracker}.cs` — reuse lock/retention patterns; add correction state.
- `Services/Gateways/{ITenantCommandGateway,ITenantQueryGateway}.cs` — reuse command/status and current reads; retain projection version.

## Tasks & Acceptance

**Execution:**
- [x] `State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`, `Components/Tenants/Audit/CorrectionStartPanel.razor` — show original reference/time, tenant, target, current role, intended command/access and owner impact, freshness/provenance, authority/admission, recovery, proof expectation, and known/unknown consequences; block gaps/conflicts and re-evaluate role changes.
- [x] `Components/Tenants/Audit/CorrectionStartPanel.razor`, `Components/Pages/TenantAuditPage.razor`, `State/TenantCommands/TenantCorrectionAttemptTracker.cs`, `Extensions/TenantsUiServiceCollectionExtensions.cs` — recheck before dispatch; retain one ULID and aggregate lease across cancel, refresh, row loss, viewport and reconnect.
- [x] `State/TenantAudit/{TenantCorrectionPreviewSnapshot,TenantCorrectionProjection}.cs`, `Components/Pages/TenantAuditPage.razor`, `Components/Tenants/Audit/CorrectionStartPanel.razor` — carry projection version; require postcondition plus causal provenance; reuse one projection read per cycle; separate audit state.
- [x] `Components/Tenants/Audit/{CorrectionStartPanel,AuditDataGrid,AuditEvidenceReceipt}.razor` — remove false proof/dead anchors; pair receipts only with proven association, else show actual audit state.
- [x] `Resources/TenantsResources{,.fr}.resx`, `Components/Tenants/Audit/CorrectionStartPanel.razor.css`, `wwwroot/js/tenantsFocus.js` — safe EN/FR copy, selectors, keyboard/focus, live regions, forced colors, responsive blocking.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionPreviewSnapshotTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/{CorrectionStartPanel,TenantAuditPage}Tests.cs`, `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — cover matrix, attempt/provenance races, retention, proof refusal, accessibility and localization.

**Acceptance Criteria:**
- Given an eligible intent, when preview opens or role changes, then ten current facts and original evidence appear; missing facts block Confirm with recovery.
- Given confirmation or a competing tenant command, when current authority, projection, and admission are rechecked, then one retained attempt dispatches through the existing gateway and the aggregate remains locked until terminal evidence.
- Given any command outcome, when status and projection refresh, then lifecycle, projection, and audit remain distinct; only causal postcondition evidence confirms correction.
- Given an audit candidate, when proof is evaluated, then only deterministic attempt-specific evidence creates both receipt links; otherwise the UI shows the truthful availability state and neither link.
- Given cancel, Escape, viewport change, refresh, or reconnect, when the surface reappears, then focus and tracking are preserved without a second logical command or unsafe copy.

## Implementation Notes

- Confirm uses a fresh authorized, versioned projection capture and retains one ULID plus aggregate lease through uncertain delivery. Closing a panel invalidates Confirm while its current read is pending; an admitted attempt remains available after remount.
- Review fixes require a fresh role selection, a second Confirm when the command changes, positive event evidence, and same-ID recovery for unverified delivery. A resumed panel reads the retained attempt before status refresh; retained details wait for a newly authorized audit surface.
- Status, projection confirmation, and audit availability are separate. The authorized audit DTO has no command-attempt correlation, so no paired receipt links are created from matching target, event type, or time alone.
- Existing focus, responsive, and forced-colors CSS/JS hooks were sufficient. The browser fixture checks actual rendered EN/FR preview markup, layout, and focus helpers; component tests exercise the Razor cancel/Escape handlers.
- Matrix audit: eligible add/change and causal confirmation are covered by `Panel_submits_restore_once_and_refuses_unlinked_audit_proof`, `Panel_change_role_workflow_sends_change_role_command_and_rechecks_projection`, and `Projection_confirmation_requires_causal_version_and_refuses_unlinked_audit_row`; conflicts and already-applied outcomes by `Panel_confirm_time_capture_already_applied_refuses_dispatch`, `Panel_confirm_time_authority_loss_refuses_dispatch`, `Admission_acquired_during_current_read_blocks_then_releases_the_same_preview`, and `Width_narrows_during_current_read_and_blocks_dispatch_until_safe_again`; ambiguous/rejected outcomes by `Panel_ambiguous_delivery_retries_only_the_retained_message_id` and `Panel_rejected_terminal_state_moves_focus_to_lifecycle`; unavailable proof by `Panel_provider_confirmed_correction_reports_missing_audit_association`. All ran in the complete test project with zero skipped tests.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and route | Evidence |
| --- | --- | --- |
| Blind 1: clearing the role selector | high; patch | `ResolvedIntent` returns the original available intent on an empty value, so Confirm can send its old role while the picker is blank. Require an explicit current selection. |
| Blind 2: command changes at confirmation | high; patch | `SubmitAsync` re-derives the command from the fresh capture and immediately dispatches it. The operator saw and accepted the previous command; present the changed preview for a second Confirm. |
| Blind 3: unverified gateway failure | high; patch | The existing add/change gateway maps HTTP 503 and unspecified failures to `Failed` with the attempt ID; the correction tracker then releases its lease. Treat unverified delivery as ambiguous and retain the ID. |
| Blind 4: full browser refresh creates another circuit | false; reject | Epic 5 explicitly scopes the aggregate lock to `(circuit, AggregateIdentity)`; a full browser restart creates a new circuit, outside the stated retention boundary. |
| Blind 5: absent or negative event count | high; patch | `status.EventCount is not 0` admits null and negative values, then an unrelated version advance can satisfy projection confirmation. Require positive event evidence before that read. |
| Blind 6: obsolete safe message key after confirmation | medium; patch | `ApplyStatus` can leave `SafeMessageKey` from an earlier unable-to-verify result; `ConfirmProjection` clears only `SafeMessage`, so Confirmed can display the old warning. Clear the key when evidence advances. |
| Blind 7: no automatic status reconciliation | low; reject | The panel offers explicit Refresh and preserves the last verified state. Automatic polling or notification integration would add a separate lifecycle mechanism for a minor delay in display. |
| Blind 8: proof link always absent | false; reject | The authorized audit DTO lacks attempt correlation. The frozen matrix explicitly requires no paired receipt link and truthful missing-support state in that case. |
| Blind 9: audit availability collapses into missing support | false; reject | The panel's `AuditState` reports proof-association support; the audit page separately renders its actual read availability. Missing correlation remains true even when rows are available. |
| Blind 10: retained details before renewed audit access | high; patch | Route load installs the retained intent while the audit snapshot is Loading, and `CaptureCorrectionAuthority` skips retained attempts. Hide retained details during Loading and after an unauthorized read, without discarding the attempt. |
| Blind 11: reopening terminal rejection or failure | low; reject | The frozen matrix retains the ID for rejected outcomes and requires safe status or same-ID recovery. Starting a new ID from the same evidence is not promised; adding a separate restart flow would exceed a direct correction. |
| Blind 12: browser fixture simulates Escape | low; reject | The browser fixture tests rendered markup, layout and focus helper, while `Cancel_or_Escape_during_pending_current_read_never_starts_an_attempt` invokes the actual Razor handler. A full browser-hosted Blazor test would duplicate this coverage at much higher cost. |
| Edge 1: unverified gateway failure | high; patch | Same verified gateway-to-tracker failure path as Blind 3; preserve a single uncertain attempt. |
| Edge 2: absent or negative event count | high; patch | Same verified nullable count path as Blind 5; an ordered projection alone does not prove this command wrote an event. |
| Edge 3: retained display after permission loss | high; patch | Same route-load and authorization-refresh path as Blind 10; retained content currently bypasses the audit surface's fresh access state. |
| Edge 4: current capture throws | medium; patch | `RefreshCaptureAsync` can fault in Confirm and status refresh without a catch, leaving the operator without a recoverable lifecycle state. Map the failed read to unable-to-verify without dispatch. |
| Edge 5: viewport changes between check and admission | maybe-false; defer | The observation is written by a Fluxor effect and read by the Razor event; the available code does not establish whether those callbacks can interleave on different threads inside the synchronous admission section. Dispatcher scheduling evidence or a controlled concurrency test would settle this. If reachable, severity is high. |
| Verification gap 1: resumed panel has stale in-flight snapshot | medium; patch | Pre-verified gap: a second panel can retain `RequestSent` without correlation after the original panel updates the tracker; Refresh then refuses same-ID retry and never loads the advanced status. Sync from the retained attempt before choosing the refresh path. |

## Design Notes

The authorized audit response drops command correlation. EventStore creates a new event MessageId, exposed as `TenantAuditEntry.EventId`; it cannot identify the command attempt. The existing matcher and `#audit-...` fragment prove nothing. Epic 5 requires missing-support state when association cannot be re-derived.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — clean build.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — 3,797 passed, 0 failed, 0 skipped after review fixes.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — rendered EN/FR preview markup, desktop, narrow, forced colors, focus helpers, and browser egress passed; mutation controls rejected.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md` — clean gitlinks.
- `git diff --check` — passed.

### Review Findings

Code review 2026-10-03 (bmad-code-review, full mode). Diff `11e65e37..17538e07` is the single story commit `17538e07`: 23 files, +1,965/−305, reviewed as one pass. Line anchors refer to `17538e07`.

Inputs and baseline checks:
- **Layers:** blind hunter, edge-case hunter, verification gap and acceptance auditor; none failed.
- **Gitlink validator:** `python3 scripts/validate-story-gitlinks.py` on this spec → PASS. No `references/` pointer moved in the range.
- **Build:** `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` → exit 0.
- **Tests:** `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` → 3,797/3,797 passed, 0 skipped. This matches the spec's claim and is local evidence only (see the CI defer below).

- [ ] [Review][Patch] (resolved decision → option (a), restore the block) A role-change receipt whose target is now absent re-derives `AddUserToTenant` [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs:126]. The diff drops the `UserRoleChanged && currentRole is null → CurrentStateIndeterminate` guard from `TenantCorrectionStartIntent.cs:126` and the matching `ResolvedIntent` check, and rewrites `RoleChangeRequiresAKnownCurrentTargetRole` to expect an available Add.
  - **Conflict:** the epic ACs for both 5.5 (`epics.md:2512`) and 5.6 (`epics.md:2578`) say "target-absent role-change blocks". The frozen matrix allows "re-derive supported command or block". The Spec Change Log and triage log do not record the change.
  - **Harm:** the preview can re-add a user who was deliberately removed after the evidence, with no warning that the correction re-adds them.
  - **Panel coverage:** the panel path for this case is untested. Every panel `UserRoleChanged` fixture has a non-null current role.
  - **Options:**
    - (a) Restore the block (guard plus test).
    - (b) Keep re-derivation, add an explicit "target was removed after this evidence; this re-adds them" preview warning plus a panel test, and record the deviation from the epic AC.
- [ ] [Review][Patch] (resolved decision → option (a), flag retryable codes as `IsAmbiguousFailure`; panel treats unflagged `Failed` as terminal) A definitive HTTP 400 is treated as ambiguous delivery, so the tenant stays locked for the circuit [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:768]. `CorrectionStartPanel.razor:768` maps every `Failed` result that carries the attempt MessageId to `UnableToVerify`.
  - **Cause:** `TenantCommandGateway.cs:748/765` returns `Failed` plus MessageId for 400, 503 and unclassified failures alike, so the panel cannot tell a definite refusal from an uncertain delivery.
  - **Result:** the lease is never released, because `UnableToVerify` is not terminal. "Refresh status" re-posts the same ID and gets the same 400 forever, and every tenant command on that aggregate stays blocked in the circuit.
  - **Context:** the Blind 3 fix was scoped to 503 and unclassified failures. The configuration and lifecycle gateway paths already use `Ambiguous(...)` for 408/429/5xx/transport errors only.
  - **Options:**
    - (a) The add and change-role mappers set `IsAmbiguousFailure` only for retryable codes and keep `Failed`. The panel then treats `Failed && !IsAmbiguousFailure` as terminal. `TenantCommandAuditStates.FromSubmission` already maps both shapes to Unverifiable, so membership audit state is unchanged.
    - (b) Switch add and change-role to the `Ambiguous(...)` RequestSent shape. This changes the member flows too.
    - (c) Treat every `Failed` as terminal in the panel. This drops the Blind 3 protection for 503.
- [ ] [Review][Patch] (resolved decision → option (a), retention expiry mirroring `TenantLifecycleAttemptTracker`, keeping the MessageId so it is never re-sent under a new ID) Non-terminal correction outcomes hold the tenant aggregate lease for the whole circuit [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:102]. `TenantCorrectionAttemptTracker.cs:102` releases only on Confirmed, Rejected, AlreadyApplied or Failed.
  - **Affected outcomes:** TimedOut, unverified status identity and a version that never advances all end in `UnableToVerify`; PublishFailed ends in `Degraded`. Each keeps every tenant command blocked until the browser circuit ends, with no operator release.
  - **Pattern drift:** the spec says to "reuse lock/retention patterns", but `TenantLifecycleAttemptTracker` has retention expiry (`PruneExpiredLocked`, `IsRetentionExpired`, `Forget`) and this tracker has none.
  - **Options:**
    - (a) Retention expiry mirroring the lifecycle tracker.
    - (b) An explicit operator "release after inspecting audit" action.
    - (c) Map EventStore-terminal statuses (TimedOut, PublishFailed) to terminal states.
    - (d) Accept, since AC2 says "locked until terminal evidence", and record it.
- [ ] [Review][Patch] (resolved decision → option (a), redacted owner count plus last-owner risk copy matching `ChangeTenantMemberRoleFlow`; the domain rejection stays the hard stop; closes R-B7) The preview has no owner-count or last-owner impact [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:364]. `OwnerImpactLabel` (`CorrectionStartPanel.razor:364`) only says "may grant or change tenant owner access".
  - **Requirement:** the epic's ten items require "access/owner-count impact" (`epics.md:2567`). The Story 5.5 ledger entry R-B7 ("Decide and expose redacted last-owner consequence facts", `deferred-work.md:3426`) was handed to 5.6 and is still open. `ChangeTenantMemberRoleFlow` already shows owner count and last-owner risk.
  - **Current behaviour:** demoting the last owner reaches the domain rejection with no warning in the preview.
  - **Options:**
    - (a) Add a redacted owner count to `TenantCorrectionProjection` and render last-owner risk copy, matching the member flow.
    - (b) Block last-owner demotion in the preview.
    - (c) Defer explicitly, and close R-B7 with that reason.
- [ ] [Review][Patch] The Confirm-time re-review gate compares only the command type [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:683]. A fresh read that changes the current role (for example Contributor → Owner), empty membership or owner impact while the command stays `ChangeUserRole` dispatches immediately. The operator reviewed "owner access is not changed" and the command then demotes an owner. Require a second Confirm whenever the current role, membership-empty flag, command or owner impact differs from what was shown.
- [ ] [Review][Patch] Confirm returns silently when the command changes at confirmation [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:692]. The state stays `Previewed`, with no reason, live-region change or focus move; only the command label changes. Set a localized EN/FR "preview changed, review and confirm again" message, announce it assertively and focus it. Assert the message in `Changed_command_at_confirmation_requires_review_and_a_second_click`.
- [ ] [Review][Patch] A missing or unordered projection version disables Confirm with no reason [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:55].
  - **Gap:** `CanSubmit` requires `IsOrdered(BaselineProjectionVersion)`, but the intent stays available with no reasons. `UnavailableReason` is empty while readiness says "resolve the reason below".
  - **Who hits it:** a legacy tenant still exposing its state-store ETag (see `TenantMembershipCommandProvenance.cs:57`) cannot be corrected until its next event.
  - **Fix:** add a canonical localized reason and a test asserting it.
- [ ] [Review][Patch] The post-read Confirm section runs off the renderer dispatcher [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:652].
  - **Problem:** after `await RefreshCaptureAsync().ConfigureAwait(false)`, the viewport check (`:696`), `SetSnapshotFromIntent` and `TryBegin` run on a pool thread. Meanwhile `OnParametersSet` live updates and viewport effects run on the dispatcher. So the deferred Edge 5 interleaving is reachable, and `TryBegin` can even receive a swapped handoff snapshot.
  - **Fix:** marshal the post-read section through `await InvokeAsync(...)`, check the viewport again inside it, and mark the Edge 5 ledger entry (`deferred-work.md:3482`) resolved.
- [ ] [Review][Patch] The correction panel and Resume button disappear whenever audit freshness is not `Current` [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:534].
  - **Problem:** `HasAuthorizedAuditSurface` also requires `Freshness is Current`. A Stale or Unknown audit read, which is likely right after a correction, hides an in-flight attempt and its Refresh while the lease stays held. This goes beyond the Blind 10 decision (hide only during Loading and after an unauthorized read).
  - **Fix:** split the predicate. Opening a correction keeps the Current requirement; showing a retained or submitted attempt needs only an authorized, non-Loading surface.
  - **Also:** update the stale `OnParametersSet` comment that says pager navigation "keeps this panel open".
- [ ] [Review][Patch] The page and the panel use different rules to recognise the retained attempt [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:406].
  - **Too strict (panel):** it matches on audit reference plus `Intent.IntendedRole`. The page reinstalls the handoff intent or a row-rebuilt intent carrying the grid-selected role (`TenantAuditPage.razor:1779`). So if the operator changed the role inside the preview, a remount after Loading shows a fresh "aggregate busy" preview instead of the in-flight attempt, and the Resume button is hidden.
  - **Too loose (page):** `HasDisplayedRetainedCorrection` (`TenantAuditPage.razor:1474`) matches on audit reference only. An unsubmitted preview then skips the viewport and authority closing paths.
  - **Fix:** use one shared predicate. Match a non-terminal attempt on tenant plus audit reference; for a terminal attempt, also require the role.
- [ ] [Review][Patch] A finished attempt reopens on every visit to the audit page, and closing it drops focus [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:730]. Route load installs `RetainedCorrectionAttempt?.Snapshot.Intent` even for a Confirmed or Rejected attempt, without setting `_activeCorrectionFocusReference` or the origin. Restore automatically only non-terminal attempts, and set the focus reference and origin when restoring.
- [ ] [Review][Patch] The readiness row says "blocked … resolve the reason below" after submission, including when Confirmed [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:353]. `ReadinessLabel` depends on `CanSubmit`, which is true only in `Previewed`, and the reason renders above the field list, not below it. Render a submitted-state readiness value (new EN/FR key) once `_hasSubmitted` is set, and fix the "below" wording.
- [ ] [Review][Patch] `TenantCorrectionAttemptTracker` has no unit tests of its own [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:23]. Add `TenantCorrectionAttemptTrackerTests` covering:
  - the `TryBegin` rollback when `TryMarkDispatched` fails
  - the `TryUpdate` refusals: ProjectionPending → Accepted or RequestSent, one terminal state to another, and losing the CorrelationId
  - `EndDelivery` and `TryStartRetry` ordering
  - lease release on each terminal state
- [ ] [Review][Patch] The `ConfirmProjection` freshness, authority and scope guard is untested [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:324]. Add a theory using captures that have the intended role and `tenant-sequence:2` but are Stale, unauthorized, `Provenance.Unknown`, or for another tenant or target. None may reach Confirmed. Removing the guard currently passes every test.
- [ ] [Review][Patch] The `HasVerifiedCommandIdentity` gate is untested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:838]. Every status stub sets it to true. Add a test with `Completed`, `EventCount: 1` and an unverified identity, asserting UnableToVerify, no confirmation read, the lease still held, and no Confirmed state.
- [ ] [Review][Patch] The page retention paths are untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:730]. Only the Resume click is exercised. Add page tests for:
  - a re-navigation or remount with a populated tracker, which restores the same MessageId
  - Start on a row while an attempt is RequestSent or Accepted, which resumes instead of opening a new preview (`:1505`)
- [ ] [Review][Patch] Preview fact values are only checked for presence [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:364].
  - **Unchecked values:** no test asserts the owner-impact text for Bootstrap, Changes or None, readiness Ready, or the target-absent current role.
  - **Stub gap:** the stub localizer lacks the `OwnerImpact.*`, `Preview.OriginalTime` and `Resume` keys, so raw key names render without any test failing.
  - **Fix:** add the keys to the stub and a theory over (current role, intended role, empty membership).
- [ ] [Review][Patch] The dispatch and status exception handlers are untested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:755]. Add a gateway stub that throws once. Assert that the attempt keeps its MessageId and lease, that a Refresh retry reuses the same ID, and that a throwing `GetStatusAsync` maps to a retryable state.
- [ ] [Review][Patch] The `TenantCorrectionAttemptTracker` scoped registration is not pinned [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:102]. Removing it, or registering it as a singleton, still passes. Add `Correction_attempt_tracker_is_scoped_to_a_circuit` to `TenantsUiCompositionTests`, beside the sibling tracker assertions.
- [ ] [Review][Patch] The MessageId-mismatch `UnableToVerify` branch is announced politely [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:761]. It inherits `RequestSent`'s Polite politeness and Lifecycle focus. Set Assertive and Refresh focus, as the sibling branch at `:770` does.
- [ ] [Review][Patch] Several preview items and the lock state have no stable selector [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:111]. Consequences, unknowns, audit expectation, recovery path and the aggregate-busy state lack a `data-testid`, as `epics.md:2653` requires. Add selectors.
- [ ] [Review][Patch] Starting a correction on another row silently reopens the pending one [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1505]. `OpenCorrectionAsync` resumes the retained attempt and returns false. The resumed panel shows no aggregate-locked reason, because `_hasSubmitted` suppresses `IsAggregateBusy`. Show the canonical aggregate-busy notice when redirecting.
- [ ] [Review][Patch] Resume leaves another row's start panel open and exempts it from closing [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1480]. `ResumeCorrectionAsync` does not clear `_activeCorrectionIntent`. `HasDisplayedRetainedCorrection` then also skips the closing paths for that start panel when its row, authority or safe viewport goes away. Clear `_activeCorrectionIntent` on Resume.
- [ ] [Review][Patch] Preview labels show facts from a non-current capture or an undetermined command [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:358].
  - **Current role:** `CurrentRoleLabel` shows a Stale or unauthorized capture's role as the current role, next to "projection unavailable".
  - **Owner impact:** `OwnerImpactLabel` says "owner access is not changed", or Bootstrap, when the role is cleared or the capture is not current.
  - **Fix:** render "-" unless the capture is current, authorized and matching, and the command is derived.
- [ ] [Review][Patch] An `Unknown` current role renders the raw key `Tenants.Correction.Role.Unknown` [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:917]. `ResolvedIntent` writes `inputs["currentRole"] = "Unknown"` (`:587`), and `PreviewValue` localizes it without a fallback. Route it through `RoleLabel` instead.
- [ ] [Review][Patch] Dead members remain on the correction snapshot [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:66]. `TryGetTrackingHandle` has no callers. The fail-closed `ConfirmProjection(TenantDetail?)` overload (`:304`) is called only by a test. Delete both and update `TenantCorrectionPreviewSnapshotTests.cs:242`.
- [ ] [Review][Patch] Ledger entries this story closes are still open [_bmad-output/implementation-artifacts/deferred-work.md:3420]. Mark the Story 5.5 hand-offs that this commit resolves as resolved, citing `17538e07`:
  - R-B6, the confirmation-time authority recheck
  - submitted-preview retention
  - BH5, attempt-specific proof
  - BH6, the dead proof anchor
  - BH7, the original timestamp
  - Cancel during an in-flight submission
  - Ambiguous same-ID retry
- [x] [Review][Defer] The global-administrator correction still links proof by event type plus a later timestamp [src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:679] — deferred: pre-existing; Story 5.7 owns global-administrator correction. `GlobalAdministratorCorrectionPanel.razor:114` renders it as a `#audit-…` fragment link, which this spec's Design Note says proves nothing. The new doc comment on `TenantCorrectionProofLink.cs:3` ("backed by attempt-specific evidence") is false for that only producer.
- [x] [Review][Defer] CI still runs no Tenants test tier [.github/workflows/ci.yml] — deferred: pre-existing and already tracked. "Validate package consumer references" fails, so every Tier 1 step is skipped, and all of this story's test evidence is local only.
- [x] [Review][Defer] The preview browser validation never runs in CI [tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:266] — deferred: pre-existing. Story Guards aborts with exit 134 at the first Chromium launch, before the preview steps (`:330-360`), so the rendered EN/FR preview, focus and forced-colors evidence is local only.

#### Rejected

- BH8/AA10, "the audit state always shows missing support": false. Blind 9 settled that `AuditState` reports proof-association support. The authorized audit DTO has no attempt correlation in any state, so "missing support" is true before and after dispatch. The preview data already renders the recovery path.
- BH12/AA14/AA15, "spec status `done` vs sprint `review`, `review_loop_iteration: 0`, Task 4 ticked for untouched files": low. Each fix is an edit of the spec under review. Section 6 of this workflow sets the status.
- BH15, "event-evidence tests use `EventsStored` with a count EventStore never sends": low. The realistic `Completed` + count path is covered by the panel confirmation tests. `EventsStored` with a null count correctly stays ProjectionPending under the spec's manual-refresh model (Blind 7).
- BH16a/EC16, "Escape that dismisses the role dropdown closes the panel": maybe-false, low if true. Escape dispatches nothing and returns focus to the launcher. Whether the Fluent v5 dropdown stops propagation needs a browser run.
- BH16b/AA7/EC4, "Refresh re-sends an uncorrelated attempt without rechecking viewport or authority": low. Same-ID retry of an already confirmed attempt is the frozen matrix's recovery. The server enforces authority (403 → Rejected), and adding the rechecks adds guards for a rare path.
- EC9, "`ClearPaging` drops a submitted panel on list-refresh or filter changes": low. The attempt, ID and lease stay retained and Resume restores the panel. The fix adds guards for an uncommon path.
- EC13, "a parent live update replaces a Confirm-time block with the older handoff preview": low. Confirm always performs a fresh read and blocks again. The fix needs read-origin tracking.
- BH4 (sub-claim), "a Failed attempt can never be confirmed again from the same evidence": rejected; Blind 11 decided that starting a new ID from the same evidence is not promised.
- AA16, "the new ledger entry uses an absolute `source_spec` path": false. Absolute paths are the majority form in `deferred-work.md` (for example `:2808`-`:3346`). Only the Story 5.5 code-review entries are repo-relative.
