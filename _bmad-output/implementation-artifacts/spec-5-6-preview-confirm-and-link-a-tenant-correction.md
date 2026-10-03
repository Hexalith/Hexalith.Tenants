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
- The approved bounded retention policy now uses a deadline timer: an unresolved attempt releases aggregate admission at expiry while retaining its MessageId and exposing only the recovery actually available. A backward clock cannot expire it early. This is the explicit exception to the earlier unbounded-lock acceptance wording.
- Projection-version confirmation relies on serialized aggregate commands and ordered projection application. Add rejects an already present target; role change is a no-op when the intended role is already current. Thus a verified positive-event correction cannot follow another command that already established its intended role, and a later command's projected version includes the correction event first.
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
| Review 2, verification gap 1: status guard absent from CI | medium; defer | The local unverified-identity test passes, but main CI stops at package-consumer validation before the UI test tier; this is the previously recorded CI blocker. |
| Review 2, verification gap 2: preview browser check absent from CI | medium; defer | The local browser fixture passes, but the checked Story Guards run aborts Chromium before reaching this fixture; this is the previously recorded CI browser blocker. |
| Review 2, blind 1: failed pre-admission read traps preview | medium; patch | `SetCaptureUnavailable` changes an unsubmitted preview to `UnableToVerify`; `CanSubmit` becomes false and Refresh has no MessageId. Closing and reopening works, but the displayed refresh recovery does not. |
| Review 2, blind 2: same-ID retry skips new authority and viewport checks | low; reject | The same-ID retry is an approved recovery path for an already admitted attempt; the server enforces current authorization and domain state. This repeats the prior low, rejected Refresh finding and a new guard would complicate that path. |
| Review 2, blind 3: completed status without positive count stays pending | medium; patch | `ApplyStatus` maps `Completed` with null or negative count to `ProjectionPending`, while the panel requires `EventCount > 0` to read projection; repeated Refresh cannot resolve legacy or malformed status evidence. |
| Review 2, blind 4: unrelated sequence can confirm correction | false; reject | The claimed counterexample requires another command to set the intended role before this positive-event correction is projected. Tenant aggregate processing is serialized: `AddUserToTenant` then rejects an existing member, and `ChangeUserRole` returns no-op when the intended role already exists. If the other command runs after the correction, ordered projection advancement includes the correction event first. A matching role, positive event count for this verified command, and advanced aggregate sequence therefore satisfy the approved projection-version evidence rule. |
| Review 2, blind 5: post-command capture replaces reviewed current role | false; reject | The field is labeled current role, and after confirmation `LastConfirmedCorrectionProjection` is the current authoritative capture; displaying its role is accurate while the original audit reference remains separate. |
| Review 2, blind 6: expiry is only lazy | medium; patch | `PruneExpiredLocked` runs on correction-tracker calls, while other tenant flows query only the admission gate. An idle uncertain correction can keep those flows locked beyond the approved retention duration; schedule bounded release. |
| Review 2, blind 7: expired attempt offers inert Refresh | medium; patch | `Find` expires the lease but keeps a nonterminal snapshot, and `TryStartRetry` refuses expired attempts. The panel still offers Refresh and new correction attempts redirect to it; show an explicit expired status-only recovery and disable the inert retry. |
| Review 2, blind 8: gateway message appears in English on French screen | medium; patch | `SafeMessageText` falls back to `SafeMessage`, and the membership gateway maps definitive failures to English text. A French correction view therefore shows unlocalized failure copy. |
| Review 2, blind 9: detached browser Escape fixture | low; reject | This repeats the prior low, rejected browser-fixture finding: the component test invokes the shipped Razor Escape handler, while the browser fixture checks rendered focus and layout. A hosted Blazor browser test would be a larger second harness. |
| Review 2, blind 10: Refresh during submission | medium; patch | `CanRefresh` ignores `_isSubmitting`; a click after correlation is set can race the automatic status read. Disable the user action during submission and serialize status reads. |
| Review 2, blind 11: sibling titled sections lack an accordion | medium; patch | The preview and lifecycle sections each have a title inside one detail panel; the repository UX rule requires one `FluentAccordion` with the primary item expanded. |
| Review 2, blind 12: dangling role description reference | low; patch | The role select always names `tenants-correction-unavailable`, but that element is rendered only when a reason exists. Make the description conditional or persistent. |
| Review 2, edge 1: confirm-time capture regresses | high; patch | A fresh capture can have an ordered version older than the preview baseline while `IsCurrent` is true; `AdmitAndDispatchAsync` currently checks fields and admission but not version monotonicity. Block the older capture. |
| Review 2, edge 2: Bootstrap copy for blocked nonowner | low; patch | `OwnerImpactLabel` selects Bootstrap on empty membership before checking the intended role; for a nonowner role it says an owner will be created even though submission is blocked. |
| Review 2, edge 3: backward clock expires immediately | medium; patch | In `PruneExpiredLocked`, `now < StartedAtUtc` fails the only duration guard and releases the lease. Treat a backward clock as unexpired. |
| Review 2, edge 4: projection proof lacks attempt link | false; reject | Same counterexample as review 2 blind 4: serialized aggregate command rules prevent a positive-event add/change command from following an earlier command that already made the role match. Ordered projection advancement after a later command includes this correction first. |
| Review 2, edge 5: expiry releases without terminal evidence | medium; patch | The release after bounded retention is real and was explicitly approved in the prior review. The defect is the absent proactive release and truthful expired recovery; fix those without changing the accepted retention policy. |

## Design Notes

The authorized audit response drops command correlation. EventStore creates a new event MessageId, exposed as `TenantAuditEntry.EventId`; it cannot identify the command attempt. The existing matcher and `#audit-...` fragment prove nothing. Epic 5 requires missing-support state when association cannot be re-derived.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — clean build.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — 3,845 passed, 0 failed, 0 skipped after review fixes.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — rendered EN/FR preview markup, desktop, narrow, forced colors, focus helpers, and browser egress passed; mutation controls rejected.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md` — blocked by two externally moved working-tree gitlinks; the committed-range check and staged-pointer check pass as recorded below.
- `git diff --check` — passed.

**Review-fix verification (2026-10-03):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — passed with 0 warnings and 0 errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — final post-review run: 3,845 passed, 0 failed, 0 skipped. This run includes the correction preview, tracker, page-retention, gateway, and projection-guard tests covering every frozen matrix row.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — desktop, narrow, forced-colors, focus, and mutation checks passed.
- `git diff --check` and EN/FR resource key parity — passed.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md` — exits 1 because externally moved `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` gitlinks differ from the clean story baseline. The story change does not stage those pointers. The same validator with `--ref bfc10cb62dff6784c4b4bcabadfd537ef74fd77a` passes and reports no committed story-range pointer changes; `git diff --cached --raw -- references/Hexalith.Builds references/Hexalith.FrontComposer` is empty.
- Direct xUnit v3 assembly filters for the three changed correction test classes — 105 passed, 0 failed, 0 skipped.
- `node_modules/.bin/commitlint --edit /tmp/tenants-story-56-commit-message.txt` — exit 0 for the exact candidate `feat(audit): Complete tenant correction preview and confirmation`.

### Review Findings

Code review 2026-10-03 (bmad-code-review, full mode). Diff `11e65e37..17538e07` is the single story commit `17538e07`: 23 files, +1,965/−305, reviewed as one pass. Line anchors refer to `17538e07`.

Inputs and baseline checks:
- **Layers:** blind hunter, edge-case hunter, verification gap and acceptance auditor; none failed.
- **Gitlink validator:** `python3 scripts/validate-story-gitlinks.py` on this spec → PASS. No `references/` pointer moved in the range.
- **Build:** `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` → exit 0.
- **Tests:** `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` → 3,797/3,797 passed, 0 skipped. This matches the spec's claim and is local evidence only (see the CI defer below).

- [x] [Review][Patch] (resolved decision → option (a), restore the block) A role-change receipt whose target is now absent re-derives `AddUserToTenant` [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs:126]. The diff drops the `UserRoleChanged && currentRole is null → CurrentStateIndeterminate` guard from `TenantCorrectionStartIntent.cs:126` and the matching `ResolvedIntent` check, and rewrites `RoleChangeRequiresAKnownCurrentTargetRole` to expect an available Add.
  - **Conflict:** the epic ACs for both 5.5 (`epics.md:2512`) and 5.6 (`epics.md:2578`) say "target-absent role-change blocks". The frozen matrix allows "re-derive supported command or block". The Spec Change Log and triage log do not record the change.
  - **Harm:** the preview can re-add a user who was deliberately removed after the evidence, with no warning that the correction re-adds them.
  - **Panel coverage:** the panel path for this case is untested. Every panel `UserRoleChanged` fixture has a non-null current role.
  - **Options:**
    - (a) Restore the block (guard plus test).
    - (b) Keep re-derivation, add an explicit "target was removed after this evidence; this re-adds them" preview warning plus a panel test, and record the deviation from the epic AC.
- [x] [Review][Patch] (resolved decision → option (a), flag retryable codes as `IsAmbiguousFailure`; panel treats unflagged `Failed` as terminal) A definitive HTTP 400 is treated as ambiguous delivery, so the tenant stays locked for the circuit [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:768]. `CorrectionStartPanel.razor:768` maps every `Failed` result that carries the attempt MessageId to `UnableToVerify`.
  - **Cause:** `TenantCommandGateway.cs:748/765` returns `Failed` plus MessageId for 400, 503 and unclassified failures alike, so the panel cannot tell a definite refusal from an uncertain delivery.
  - **Result:** the lease is never released, because `UnableToVerify` is not terminal. "Refresh status" re-posts the same ID and gets the same 400 forever, and every tenant command on that aggregate stays blocked in the circuit.
  - **Context:** the Blind 3 fix was scoped to 503 and unclassified failures. The configuration and lifecycle gateway paths already use `Ambiguous(...)` for 408/429/5xx/transport errors only.
  - **Options:**
    - (a) The add and change-role mappers set `IsAmbiguousFailure` only for retryable codes and keep `Failed`. The panel then treats `Failed && !IsAmbiguousFailure` as terminal. `TenantCommandAuditStates.FromSubmission` already maps both shapes to Unverifiable, so membership audit state is unchanged.
    - (b) Switch add and change-role to the `Ambiguous(...)` RequestSent shape. This changes the member flows too.
    - (c) Treat every `Failed` as terminal in the panel. This drops the Blind 3 protection for 503.
- [x] [Review][Patch] (resolved decision → option (a), retention expiry mirroring `TenantLifecycleAttemptTracker`, keeping the MessageId so it is never re-sent under a new ID) Non-terminal correction outcomes hold the tenant aggregate lease for the whole circuit [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:102]. `TenantCorrectionAttemptTracker.cs:102` releases only on Confirmed, Rejected, AlreadyApplied or Failed.
  - **Affected outcomes:** TimedOut, unverified status identity and a version that never advances all end in `UnableToVerify`; PublishFailed ends in `Degraded`. Each keeps every tenant command blocked until the browser circuit ends, with no operator release.
  - **Pattern drift:** the spec says to "reuse lock/retention patterns", but `TenantLifecycleAttemptTracker` has retention expiry (`PruneExpiredLocked`, `IsRetentionExpired`, `Forget`) and this tracker has none.
  - **Options:**
    - (a) Retention expiry mirroring the lifecycle tracker.
    - (b) An explicit operator "release after inspecting audit" action.
    - (c) Map EventStore-terminal statuses (TimedOut, PublishFailed) to terminal states.
    - (d) Accept, since AC2 says "locked until terminal evidence", and record it.
- [x] [Review][Patch] (resolved decision → option (a), redacted owner count plus last-owner risk copy matching `ChangeTenantMemberRoleFlow`; the domain rejection stays the hard stop; closes R-B7) The preview has no owner-count or last-owner impact [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:364]. `OwnerImpactLabel` (`CorrectionStartPanel.razor:364`) only says "may grant or change tenant owner access".
  - **Requirement:** the epic's ten items require "access/owner-count impact" (`epics.md:2567`). The Story 5.5 ledger entry R-B7 ("Decide and expose redacted last-owner consequence facts", `deferred-work.md:3426`) was handed to 5.6 and is still open. `ChangeTenantMemberRoleFlow` already shows owner count and last-owner risk.
  - **Current behaviour:** demoting the last owner reaches the domain rejection with no warning in the preview.
  - **Options:**
    - (a) Add a redacted owner count to `TenantCorrectionProjection` and render last-owner risk copy, matching the member flow.
    - (b) Block last-owner demotion in the preview.
    - (c) Defer explicitly, and close R-B7 with that reason.
- [x] [Review][Patch] The Confirm-time re-review gate compares only the command type [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:683]. A fresh read that changes the current role (for example Contributor → Owner), empty membership or owner impact while the command stays `ChangeUserRole` dispatches immediately. The operator reviewed "owner access is not changed" and the command then demotes an owner. Require a second Confirm whenever the current role, membership-empty flag, command or owner impact differs from what was shown.
- [x] [Review][Patch] Confirm returns silently when the command changes at confirmation [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:692]. The state stays `Previewed`, with no reason, live-region change or focus move; only the command label changes. Set a localized EN/FR "preview changed, review and confirm again" message, announce it assertively and focus it. Assert the message in `Changed_command_at_confirmation_requires_review_and_a_second_click`.
- [x] [Review][Patch] A missing or unordered projection version disables Confirm with no reason [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:55].
  - **Gap:** `CanSubmit` requires `IsOrdered(BaselineProjectionVersion)`, but the intent stays available with no reasons. `UnavailableReason` is empty while readiness says "resolve the reason below".
  - **Who hits it:** a legacy tenant still exposing its state-store ETag (see `TenantMembershipCommandProvenance.cs:57`) cannot be corrected until its next event.
  - **Fix:** add a canonical localized reason and a test asserting it.
- [x] [Review][Patch] The post-read Confirm section runs off the renderer dispatcher [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:652].
  - **Problem:** after `await RefreshCaptureAsync().ConfigureAwait(false)`, the viewport check (`:696`), `SetSnapshotFromIntent` and `TryBegin` run on a pool thread. Meanwhile `OnParametersSet` live updates and viewport effects run on the dispatcher. So the deferred Edge 5 interleaving is reachable, and `TryBegin` can even receive a swapped handoff snapshot.
  - **Fix:** marshal the post-read section through `await InvokeAsync(...)`, check the viewport again inside it, and mark the Edge 5 ledger entry (`deferred-work.md:3482`) resolved.
- [x] [Review][Patch] The correction panel and Resume button disappear whenever audit freshness is not `Current` [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:534].
  - **Problem:** `HasAuthorizedAuditSurface` also requires `Freshness is Current`. A Stale or Unknown audit read, which is likely right after a correction, hides an in-flight attempt and its Refresh while the lease stays held. This goes beyond the Blind 10 decision (hide only during Loading and after an unauthorized read).
  - **Fix:** split the predicate. Opening a correction keeps the Current requirement; showing a retained or submitted attempt needs only an authorized, non-Loading surface.
  - **Also:** update the stale `OnParametersSet` comment that says pager navigation "keeps this panel open".
- [x] [Review][Patch] The page and the panel use different rules to recognise the retained attempt [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:406].
  - **Too strict (panel):** it matches on audit reference plus `Intent.IntendedRole`. The page reinstalls the handoff intent or a row-rebuilt intent carrying the grid-selected role (`TenantAuditPage.razor:1779`). So if the operator changed the role inside the preview, a remount after Loading shows a fresh "aggregate busy" preview instead of the in-flight attempt, and the Resume button is hidden.
  - **Too loose (page):** `HasDisplayedRetainedCorrection` (`TenantAuditPage.razor:1474`) matches on audit reference only. An unsubmitted preview then skips the viewport and authority closing paths.
  - **Fix:** use one shared predicate. Match a non-terminal attempt on tenant plus audit reference; for a terminal attempt, also require the role.
- [x] [Review][Patch] A finished attempt reopens on every visit to the audit page, and closing it drops focus [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:730]. Route load installs `RetainedCorrectionAttempt?.Snapshot.Intent` even for a Confirmed or Rejected attempt, without setting `_activeCorrectionFocusReference` or the origin. Restore automatically only non-terminal attempts, and set the focus reference and origin when restoring.
- [x] [Review][Patch] The readiness row says "blocked … resolve the reason below" after submission, including when Confirmed [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:353]. `ReadinessLabel` depends on `CanSubmit`, which is true only in `Previewed`, and the reason renders above the field list, not below it. Render a submitted-state readiness value (new EN/FR key) once `_hasSubmitted` is set, and fix the "below" wording.
- [x] [Review][Patch] `TenantCorrectionAttemptTracker` has no unit tests of its own [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:23]. Add `TenantCorrectionAttemptTrackerTests` covering:
  - the `TryBegin` rollback when `TryMarkDispatched` fails
  - the `TryUpdate` refusals: ProjectionPending → Accepted or RequestSent, one terminal state to another, and losing the CorrelationId
  - `EndDelivery` and `TryStartRetry` ordering
  - lease release on each terminal state
- [x] [Review][Patch] The `ConfirmProjection` freshness, authority and scope guard is untested [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:324]. Add a theory using captures that have the intended role and `tenant-sequence:2` but are Stale, unauthorized, `Provenance.Unknown`, or for another tenant or target. None may reach Confirmed. Removing the guard currently passes every test.
- [x] [Review][Patch] The `HasVerifiedCommandIdentity` gate is untested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:838]. Every status stub sets it to true. Add a test with `Completed`, `EventCount: 1` and an unverified identity, asserting UnableToVerify, no confirmation read, the lease still held, and no Confirmed state.
- [x] [Review][Patch] The page retention paths are untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:730]. Only the Resume click is exercised. Add page tests for:
  - a re-navigation or remount with a populated tracker, which restores the same MessageId
  - Start on a row while an attempt is RequestSent or Accepted, which resumes instead of opening a new preview (`:1505`)
- [x] [Review][Patch] Preview fact values are only checked for presence [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:364].
  - **Unchecked values:** no test asserts the owner-impact text for Bootstrap, Changes or None, readiness Ready, or the target-absent current role.
  - **Stub gap:** the stub localizer lacks the `OwnerImpact.*`, `Preview.OriginalTime` and `Resume` keys, so raw key names render without any test failing.
  - **Fix:** add the keys to the stub and a theory over (current role, intended role, empty membership).
- [x] [Review][Patch] The dispatch and status exception handlers are untested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:755]. Add a gateway stub that throws once. Assert that the attempt keeps its MessageId and lease, that a Refresh retry reuses the same ID, and that a throwing `GetStatusAsync` maps to a retryable state.
- [x] [Review][Patch] The `TenantCorrectionAttemptTracker` scoped registration is not pinned [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:102]. Removing it, or registering it as a singleton, still passes. Add `Correction_attempt_tracker_is_scoped_to_a_circuit` to `TenantsUiCompositionTests`, beside the sibling tracker assertions.
- [x] [Review][Patch] The MessageId-mismatch `UnableToVerify` branch is announced politely [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:761]. It inherits `RequestSent`'s Polite politeness and Lifecycle focus. Set Assertive and Refresh focus, as the sibling branch at `:770` does.
- [x] [Review][Patch] Several preview items and the lock state have no stable selector [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:111]. Consequences, unknowns, audit expectation, recovery path and the aggregate-busy state lack a `data-testid`, as `epics.md:2653` requires. Add selectors.
- [x] [Review][Patch] Starting a correction on another row silently reopens the pending one [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1505]. `OpenCorrectionAsync` resumes the retained attempt and returns false. The resumed panel shows no aggregate-locked reason, because `_hasSubmitted` suppresses `IsAggregateBusy`. Show the canonical aggregate-busy notice when redirecting.
- [x] [Review][Patch] Resume leaves another row's start panel open and exempts it from closing [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1480]. `ResumeCorrectionAsync` does not clear `_activeCorrectionIntent`. `HasDisplayedRetainedCorrection` then also skips the closing paths for that start panel when its row, authority or safe viewport goes away. Clear `_activeCorrectionIntent` on Resume.
- [x] [Review][Patch] Preview labels show facts from a non-current capture or an undetermined command [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:358].
  - **Current role:** `CurrentRoleLabel` shows a Stale or unauthorized capture's role as the current role, next to "projection unavailable".
  - **Owner impact:** `OwnerImpactLabel` says "owner access is not changed", or Bootstrap, when the role is cleared or the capture is not current.
  - **Fix:** render "-" unless the capture is current, authorized and matching, and the command is derived.
- [x] [Review][Patch] An `Unknown` current role renders the raw key `Tenants.Correction.Role.Unknown` [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:917]. `ResolvedIntent` writes `inputs["currentRole"] = "Unknown"` (`:587`), and `PreviewValue` localizes it without a fallback. Route it through `RoleLabel` instead.
- [x] [Review][Patch] Dead members remain on the correction snapshot [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs:66]. `TryGetTrackingHandle` has no callers. The fail-closed `ConfirmProjection(TenantDetail?)` overload (`:304`) is called only by a test. Delete both and update `TenantCorrectionPreviewSnapshotTests.cs:242`.
- [x] [Review][Patch] Ledger entries this story closes are still open [_bmad-output/implementation-artifacts/deferred-work.md:3420]. Mark the Story 5.5 hand-offs that this commit resolves as resolved, citing `17538e07`:
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

### Review Findings (pass 2: fix-pass re-review)

Code review 2026-10-03 (bmad-code-review, full mode) of the fix pass `17538e07..93b0b96d`: 23 files, +1,520/−177. That range is commit `bfc10cb6` (status only) plus commit `93b0b96d`, which closed the 27 pass-1 patches. Line anchors refer to `93b0b96d`.

Inputs and baseline checks:
- **Layers:** blind hunter, edge-case hunter, verification gap and acceptance auditor; none failed.
- **Gitlink validator:** `python3 scripts/validate-story-gitlinks.py` on this spec → **exit 1**, caused by the working-tree gitlinks for `references/Hexalith.Builds` and `references/Hexalith.FrontComposer`. With `--ref 93b0b96d` it passes, the committed range moves no pointer, and nothing is staged. Recorded as a defer below.
- **Build:** `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` → 0 warnings, 0 errors.
- **Tests:** `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` → 3,845/3,845 passed, 0 skipped. This is local evidence only: CI runs no UI tier, and the run used the drifted FrontComposer gitlink.

- [ ] [Review][Patch] (resolved decision → option (a): align with the member flow, warning that the change can leave no owner and is not blocked; fix the R-B7 resolution text) The last-owner preview copy says the demotion "will be rejected", but the domain allows it — `Tenants.Correction.Preview.OwnerImpact.LastOwner` (`TenantsResources.resx:4240`, FR the same) reads "Demoting the last owner will be rejected."
  - **Behaviour:** `OwnerImpactLabel` (`CorrectionStartPanel.razor:398`) selects that text when a sole owner is demoted, while `CanSubmit` stays true.
  - **The domain:** `TenantAggregate.Handle(ChangeUserRole)` (`TenantAggregate.cs:197-218`) and its validator have no last-owner rule. The member flow says so explicitly: `Tenants.ChangeRole.OwnerRisk.LastOwner` reads "…can reduce the visible owner count to zero, but the command is not blocked…".
  - **The test agrees with the domain:** `Current_role_change_with_same_command_requires_a_second_review` asserts the "last owner" text, then reaches Confirmed on that very demotion.
  - **Effect:** the operator is told the system will stop the change, and then it leaves the tenant with no owner.
  - **Why this is a decision:** decision D4 chose "last-owner risk copy matching `ChangeTenantMemberRoleFlow`; the domain rejection stays the hard stop", and that premise is false. The ledger resolution of R-B7 (`deferred-work.md:3430`) repeats the false claim.
  - **Options:**
    - (a) Align the copy with the member flow: warn that the change can leave the tenant with no owner and is not blocked. Fix the R-B7 resolution text to match.
    - (b) Block last-owner demotion in the preview, with a canonical reason and its recovery.
- [ ] [Review][Patch] (resolved decision → option (a): mirror `TenantLifecycleAttemptTracker`; an expired attempt stops blocking `TryBegin` and the auto-redirect, and stays viewable through Resume with status lookup when correlated until a new attempt replaces it) An expired attempt blocks every later correction on that tenant for the rest of the circuit, behind a false "aggregate busy" notice [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:225].
  - **Behaviour:** `PruneExpiredLocked` releases the lease but keeps the attempt as non-terminal `UnableToVerify`.
    - `TryBegin` (`:65`) refuses every new attempt for the tenant while a non-terminal attempt exists, for any audit evidence.
    - The page restores the attempt on every visit and redirects every Start to it with the competing notice (`TenantAuditPage.razor:1521`).
  - **What the operator sees:** "Another tenant command is being reconciled. Wait or refresh its status", although the lease is released. Refresh is disabled when the attempt has no correlation (`CanRefresh`, `TryStartRetry`). Correlated attempts whose status never becomes terminal stay stuck in the same way.
  - **Pattern drift:** D3 chose "retention expiry mirroring `TenantLifecycleAttemptTracker`". That tracker moves expired snapshots to a terminal record so a new attempt can start (`TenantLifecycleAttemptTracker.cs:336-375`). Only a new browser circuit clears the correction block.
  - **Options:**
    - (a) Mirror the lifecycle tracker. An expired attempt no longer blocks `TryBegin` and is no longer auto-redirected; it stays viewable through Resume, with status lookup when correlated, until a new attempt replaces it. A fresh preview re-derives from current state, so a command that already landed shows as already applied.
    - (b) Block only the same audit evidence after expiry; other evidence can start.
    - (c) Keep the block and add an explicit operator "dismiss expired attempt" action.
- [ ] [Review][Patch] The competing-attempt busy notice is never cleared. It masks real outcomes and labels fresh previews as busy [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1510]
  - **Lifecycle:** `_competingCorrectionNotice` is reset only on route load (`:746`) and by a non-competing Resume. `CloseCorrectionAsync`, handoff, a non-redirected open and terminal outcomes leave it set.
  - **Precedence:** `UnavailableReason` (`CorrectionStartPanel.razor:268`) ranks the notice above `SafeMessageText`.
  - **Effects:**
    - A resumed attempt that reaches Confirmed or Rejected keeps saying "Another tenant command is being reconciled".
    - After that panel closes, a fresh preview on any row mounts with the busy alert and `tenants-correction-aggregate-busy` while Confirm is enabled and readiness says Ready.
  - **Fix:** clear the flag on close, on handoff and on a non-redirected open; ignore it once the retained attempt is terminal. Add a page test.
- [ ] [Review][Patch] The operator's own submission is announced as "another tenant command" for the whole dispatch [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:798]
  - **Cause:** admission now runs on the renderer dispatcher, so `TryAcquireLease` → `NotifyStateChanged` → `OnAdmissionChanged` → `InvokeAsync(StateHasChanged)` renders inline before `_hasSubmitted = true`.
  - **What renders:** `IsAggregateBusy` is true for the panel's own lease, so the `role="alert"` reason shows `AggregateBusy` and readiness shows Blocked.
  - **Duration:** the next render is the `finally` in `SubmitAsync`, after the POST and the post-dispatch status read. So that false alert, not RequestSent, stays visible for the whole dispatch.
  - **Regression:** under the off-dispatcher code of `17538e07` the render was queued until after `_hasSubmitted` was set.
  - **Fix:** mark the panel as submitting its own attempt before `TryBegin` and revert on refusal, or exclude the panel's own lease from `IsAggregateBusy`. Assert the reason text while the gateway is pending.
- [ ] [Review][Patch] The Confirm-time re-review gate compares against a live-updated snapshot, not the facts the operator confirmed [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:757]
  - **Cause:** `preview = _snapshot` is read after the awaited capture. `ShouldLiveUpdatePreview` ignores `_isSubmitting`, so a parent re-render with a newer or equal `StartProjection` during the read replaces `_snapshot` through `OnParametersSet` (`:491`).
  - **Effect:** if the confirm-time capture equals that replacement, the change gate (`:777-790`) passes. A command is dispatched on role, owner-count or membership facts that were not on screen when the operator clicked, for example the demotion of an owner. This is the bypass that the pass-1 patch "require a second Confirm whenever … differs from what was shown" was meant to close.
  - **Fix:** capture the reviewed snapshot in `SubmitAsync` before the await and gate (and restore on refusal) against it, or skip live updates while `_isSubmitting`.
- [ ] [Review][Patch] Typed rejection reasons are dropped, and the operator sees only a generic "was rejected" [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:870]
  - **Cause:** dispatch now sets `SafeMessage = null` and uses `result.SafeMessageKey ?? FailureMessageKey(...)`. The membership gateway's rejections carry a `RejectionCode` and no key. The status path behaves the same way.
  - **Effect:** `UserAlreadyInTenant`, `UserNotInTenant`, `RoleEscalation` and `InsufficientPermissions` all render "The tenant correction was rejected. Review current evidence and the rejection…", but no rejection is shown.
  - **Context:** the pass-1 fix for gateway English on French screens discarded the reason instead of localizing it.
  - **Fix:** map known rejection codes to localized EN/FR `Tenants.Correction.Rejection.{code}` keys before the generic key, and add a French test per code.
- [ ] [Review][Patch] The add-member and change-role gateway fallback classification is untested beyond 400 and 503 [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:762]
  - **Current test:** the `_` arm combines `Retryable`, `StatusCode < 400` and `IsRetryableStatusCode`, but `Correction_membership_gateway_classifies_only_uncertain_delivery_as_ambiguous` (`TenantCommandGatewayTests.cs:1477`) only has 400 and 503 rows.
  - **Risk:** dropping either term turns a 5xx that the server may have committed into a terminal `Failed`. The lease is released and same-ID recovery is lost.
  - **Fix:** add rows for 408, 429, 500, 502, 504 and a transport status (below 400) as ambiguous, and for 409 as non-ambiguous, for both commands.
- [ ] [Review][Patch] Only the current-role branch of the Confirm-time change gate is tested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:781]
  - **Gap:** both existing tests change `CurrentRole`. No test changes only `OwnerCount` (for example 2→1) or only `IsMembershipEmpty` between preview and Confirm.
  - **Risk:** dropping either comparison dispatches an owner demotion without re-review.
  - **Fix:** add a theory that asserts no dispatch plus `ChangedAtConfirm` for each case.
- [ ] [Review][Patch] After confirmation, the owner-impact line describes the post-command capture [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:398]
  - **Cause:** `CurrentProjectionForDisplay` (`:342`) becomes `LastConfirmedCorrectionProjection`. After a confirmed Owner→Reader correction the current and intended roles match.
  - **Effect:** the line reads "…This command does not change tenant owner access" next to the post-command count.
  - **Fix:** once `_hasSubmitted`, derive the impact category and count from the pre-submit capture (`_snapshot.Intent.CurrentProjection`).
- [ ] [Review][Patch] The new correction copy is unverified on the status path [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:928]
  - **Missing assertions:** no test asserts `Tenants.Correction.Status.Pending`, `Status.Retryable` or any `Tenants.Correction.Failure.*` key. Reverting to `status.SafeMessage` or deleting the failure-key block still passes.
  - **Stub gap:** the panel's `StubTenantsLocalizer` lacks `ConfirmReadFailed`, `ProjectionRegressed`, `Failure.*` and `Status.*`, so raw keys render without any test failing.
  - **Fix:** add a French-culture theory over Pending, retryable, Rejected, PublishFailed and TimedOut that asserts the resource copy and the absence of gateway English, and extend the stub.
- [ ] [Review][Patch] The "one status read at a time" test never reaches its guard [tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs:232]
  - **Gap:** the second click happens while `_isSubmitting` is still true, so removing both the `Interlocked.CompareExchange` and the `_statusReadInFlight` term of `CanRefresh` still passes.
  - **Fix:** after submission completes, click Refresh with an unresolved status task, then assert that Refresh is disabled and a second click adds no status call.
- [ ] [Review][Patch] The BFF owner count is only asserted for a membership with no owners [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:571]
  - **Gap:** `BatchCorrectionCompositionResolvesOnePrincipalForAllSafeTargets` uses a zero-owner fixture, and the page-test fake recomputes the count with the production formula.
  - **Fix:** add a composition test with one and with two owners, including the target as an owner, that asserts the exact count.
- [ ] [Review][Patch] The last-owner boundary (`count == 1`) is never exercised with two or more owners [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:412]. Add an Owner→Reader row with `OwnerCount = 2` that asserts the non-last-owner copy. Whatever copy the last-owner decision settles on, this row is needed.
- [ ] [Review][Patch] Focus after closing a route-restored correction is untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:744]. Deleting the restored focus reference and origin lines still passes. Extend `Remount_restores_pending_attempt_on_a_stale_authorized_audit_surface` with a close step and a `focusCorrectionLauncher` assertion.
- [ ] [Review][Patch] Resume closing another row's start panel is untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1508]. Deleting `_activeCorrectionIntent = null;` still passes. Open Start on a second row, click Resume, then assert that only the resumed panel remains.
- [ ] [Review][Patch] The focus move to the reason element after a refused Confirm is unverified [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:749]. The ConfirmReadFailed, ProjectionRegressed and ChangedAtConfirm tests assert text and `aria-live` only, so deleting `_focusReasonPending = true` still passes. Add the focus-count assertion.
- [ ] [Review][Patch] An out-of-range current role is no longer blocked by the panel [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:649]. `ResolvedIntent` now strips the start-time `CurrentStateIndeterminate` but re-adds it only for `TenantRole.Unknown`, so an undefined role value becomes an available `ChangeUserRole`. Block anything other than Owner, Contributor or Reader, as `Evaluate` does.
- [ ] [Review][Patch] The accordion conversion dropped the h4 section headings [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:117]. The two `FluentAccordionItem`s render at the default heading level under the panel's h3, which breaks the outline. The `.correction-start-panel__preview h4` reset and its `fc-css-exception` (`CorrectionStartPanel.razor.css:22`) are now dead. Set `HeadingLevel="4"` and remove the dead selector.
- [ ] [Review][Patch] The role select is no longer described by the actual reason [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:105]. `aria-describedby` now names only the readiness value ("Resolve the reason above"). Also reference `tenants-correction-unavailable` whenever it is rendered.
- [ ] [Review][Patch] The expired-without-correlation copy asks the operator to keep an ID that the UI never shows [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:4259]. MessageIds are support-safe-hidden. Use the canonical `escalate` recovery verb in EN and FR, and align the wording with the expired-attempt decision.
- [ ] [Review][Patch] `TenantCorrectionUnavailableReason.ProjectionVersionUnavailable` is a dead enum member, inserted mid-enum [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionUnavailableReason.cs:13]. Nothing adds it to a reasons list, because both blocks use literal `SafeMessageKey` strings. Delete it.
- [ ] [Review][Patch] Six new French correction strings say "tenant" where the other 21 correction strings say "locataire" [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:4255]. Affected keys: ConfirmReadFailed, ProjectionRegressed and the four `Failure.*` keys.
- [ ] [Review][Patch] Two new ledger entries duplicate the existing CI deferrals [_bmad-output/implementation-artifacts/deferred-work.md:3510]. `:3510` and `:3513` restate the `:3504` and `:3507` items ("Restore CI test execution" and the Story Guards Chromium abort). Delete them.
- [ ] [Review][Patch] Ledger resolutions cite "the Story 5.6 review patch" instead of the commit [_bmad-output/implementation-artifacts/deferred-work.md:3430]. Cite `93b0b96d` at `:3430` and `:3494`.
- [x] [Review][Defer] The gitlink validator exits 1 on the working tree [references/Hexalith.Builds, references/Hexalith.FrontComposer] — deferred: pre-existing external drift that this story did not cause. Builds is at `3639c8d → c16249a` and FrontComposer at `24033f7 → bf40099`, both unstaged. `--ref 93b0b96d` passes. Keep both pointers out of any story commit, or revert them and commit separately as `build(deps)`.

#### Rejected (pass 2)

- AA3, "timer release contradicts the frozen matrix and AC2; Spec Change Log empty": rejected. The fix edits the spec under review, and the bounded release was the human-approved D3 option.
- AA8/BH5, "Completed without a valid event count loops on Refresh": low. EventStore sets `EventCount` on every `Completed` status, so this needs malformed or legacy status. A resolvable mapping needs a new terminal outcome, not a direct correction.
- AA6/BH15, "a verified absent target is reported as indeterminate": low. D1 restored the original guard together with its original `CurrentStateIndeterminate` reason. A dedicated reason needs a new enum member and EN/FR keys for an uncommon path.
- AA12/BH16, "a collapsed accordion item hides the outcome and swallows focus": low. The operator would have to collapse the lifecycle item by hand, and re-expanding it before focus needs bound state.
- AA13/BH20, "spec Commands record rewritten, status `done` vs sprint `review`, AC deviation only in notes": rejected. Each fix edits the spec under review, and section 6 of this workflow sets the status.
- BH2/ECH5, "a lazy prune swallows the expiry notification": low. The window between the deadline and the timer callback is milliseconds. The fix adds event raising to every prune path.
- BH4, "wall-clock expiry; real-time timers in tests": low. This matches `TenantLifecycleAttemptTracker`'s wall-clock pattern. Migrating to `TimeProvider` is not a direct correction.
- BH8/ECH6, "dispatch results are handled off the renderer dispatcher": low. `SetSnapshot` and `SetRetainedSnapshot` only write fields, with no `StateHasChanged` and no `EventCallback`. The continuation was already off the dispatcher in `17538e07`, and the race window is microseconds.
- BH12/ECH8, "a terminal attempt with an in-panel role change disappears on a Stale read": low. It needs an in-panel role change, a terminal outcome and a Stale read before closing. The remounted preview re-reads current state, and the fix needs page-side intent tracking.
- BH14 (sub-claim), "`Enum.TryParse` accepts numeric roles": low. The BFF rejects undefined roles before the capture reaches the panel.
- BH10/AA10, "a stored confirm-time message masks a later viewport or busy block; the busy `data-testid` can wrap other text": low. It needs a refused Confirm followed by a new block. Reordering the precedence also changes terminal-message priority.
- ECH7/ECH15, "the retained panel vanishes on Error, Unavailable or InvalidCursor reads": low. Those reads do not prove authorization, which is consistent with the Blind 10 gate. The attempt is retained and restored once the read recovers.
- ECH9, "`SetCaptureUnavailable` forces Previewed over a live-updated block": low. It needs a live update and a failed read within one Confirm, and the reviewed-snapshot patch above subsumes it.
- ECH10, "with no capture provider and no query gateway, Confirm is retried forever": false. `ITenantQueryGateway` is always registered (`TenantQueryGateway` or `UnavailableTenantQueryGateway`, `TenantsUiServiceCollectionExtensions.cs:145/157`).
- ECH11, "a role change after ChangedAtConfirm reverts to the older `StartProjection` facts": low. This is safe: the next Confirm re-reads and gates again. The fix adds retained-capture state for an uncommon sequence.
