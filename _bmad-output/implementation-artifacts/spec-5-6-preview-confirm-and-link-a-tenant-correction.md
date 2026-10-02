---
title: 'Preview, confirm, and link a tenant correction'
type: 'feature'
created: '2026-10-02'
status: 'done'
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
