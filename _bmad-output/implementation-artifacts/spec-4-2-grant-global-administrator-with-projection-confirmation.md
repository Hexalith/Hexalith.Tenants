---
title: '4.2 Grant Global Administrator with Projection Confirmation'
type: 'feature'
created: '2026-08-31'
status: done
baseline_revision: '7e88a571588fc7aa769ee1af01e91113f6f9b01f'
baseline_commit: '7e88a571588fc7aa769ee1af01e91113f6f9b01f'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings:
  - oversized
deferred:
  - summary: >-
      The concurrently introduced tenant-workspace tab migration lacks browser-level proof that inactive panels are not visible or focusable.
    evidence: |-
      The workspace-tab change arrived in a separate concurrent commit and is outside Story 4.2. Existing component assertions cover attributes, but an authenticated browser active-element and visibility trace would settle the remaining interaction risk.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:420
    severity: medium
  - summary: >-
      The concurrently introduced Memories secret-store topology lacks a deployed Aspire model and health verification.
    evidence: |-
      The AppHost secret-store change arrived in a separate concurrent commit and is outside Story 4.2. An Aspire resource-model inspection plus a healthy deployed startup using the intended secret provider would settle the topology risk.
    location: >-
      src/Hexalith.Tenants.AppHost/Program.cs:117
    severity: medium
  - summary: >-
      The tenant-workspace tab migration removed every inactive-panel visibility assertion inside this story's own commit, not a concurrent one.
    evidence: |-
      Commit 8da765ad -- the same commit that carries the Story 4.2 grant work -- replaced the `hidden`/`aria-hidden` assertions on `#tenants-retained-panel`/`#users-retained-panel` with `role="tabpanel"` checks that hold for the active and inactive panel alike. Fluent UI v5 owns the panel flip client-side, so bUnit cannot observe it and no assertion anywhere distinguishes the two states. An authenticated browser trace showing the inactive panel is neither visible, focusable, nor exposed to assistive technology would settle it.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:426
    severity: medium
  - summary: >-
      Focus containment, focus restoration, and viewport measurement are proven at the interop layer rather than in a real browser.
    evidence: |-
      The component tests assert which ElementReference the page asked the runtime to focus and drive the viewport by calling Observe on the observation singleton. Neither reaches document.activeElement, a real Tab cycle, `inert` semantics, or a real JS measurement, and there is no browser-driven lane in this repository. An authenticated browser trace over the grant preview -- open, Tab cycle, Escape, restore -- and a real viewport measurement would settle it.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
    severity: medium
  - summary: >-
      Identical transport conditions are classified two different ways depending on which tracked command was dispatched.
    evidence: |-
      `SetGlobalAdministratorTrackedAsync` now treats a retryable EventStoreGatewayException and a plain OperationCanceledException as same-identity ambiguity, while `SetTenantConfigurationTrackedAsync`, `RemoveTenantConfigurationTrackedAsync`, `EnableTenantTrackedAsync`, and `DisableTenantTrackedAsync` still key off status codes and `TaskCanceledException` alone. Those four are unchanged pre-existing behaviour outside this story's fixed-scope intent; a decision on whether the grant rule supersedes them would settle it.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:258
    severity: low
  - summary: >-
      Nothing in this repository pins EventStore's command-status contract, which the grant lifecycle reasons over directly.
    evidence: |-
      Every status assertion is fed by a stub. This pass corrected one concrete assumption -- that EventsStored/EventsPublished carry an EventCount -- only by reading AggregateActor and CommandStatusRecord in the submodule. A contract or integration test over a real command-status response would settle the remaining assumptions the same way.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:575
    severity: low
---

<intent-contract>

## Intent

**Problem:** Production grant remains intentionally unavailable because the historical form dispatches without a complete consequence preview, normalizes the target, lets the gateway mint an unrecoverable message id, and can confirm from target presence without causal projection evidence.

**Approach:** Install a BFF-composed, complete fixed-scope grant preview and a retained tracked-command lifecycle. Preserve the exact target and one caller-owned ULID, then report success only when a complete authoritative re-query proves the target present, the projection version advanced beyond the preview baseline, and status for that exact command proves an event was produced.

## Boundaries & Constraints

**Always:** Fix identity to `system / global-administrators / global-administrators`; keep API/domain authorization authoritative and recheck current circuit authority before preview and dispatch; treat non-whitespace UserIds as literal ordinal values without trimming, casing, parsing, or regeneration; require all preview facts, current complete-population evidence, measured safe viewport, lifecycle support, and fixed-aggregate admission; retain one message id and the preview baseline through ambiguous transport and renderer replacement; keep last-confirmed rows unchanged until qualified confirmation; use FrontComposer/Fluent UI V5, whole-string EN/FR resources, stable selectors, focus containment, and support-safe lifecycle states.

**Never:** Add an endpoint, tenant-membership lookup, bulk grant, optimistic row, direct state-store access, or a second aggregate identity; treat acceptance, SignalR, target presence alone, opaque version churn without exact-command event evidence, `GlobalAdministratorAlreadyExists`, or an audit promise as success; expose correlation ids, message ids, tokens, claims, payloads, ETags, cursors, projection versions, metadata, stack traces, or hidden administrator data; change removal/domain behavior; modify, stage, revert, or otherwise write `_bmad-output/implementation-artifacts/sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible preview | Authorized caller, exact absent target, complete current fixed-scope snapshot, safe viewport and free aggregate | Render a modal preview containing fixed scope, target, current/resulting count, authority change, freshness, recovery, audit expectation, caller/target context, known consequences, and known unknowns; dispatch nothing | Any missing or contradictory fact blocks confirmation with associated reason/recovery |
| Existing target | Complete current snapshot already contains the exact ordinal UserId | Refuse preview/dispatch and retain confirmed rows | Show localized safe rejection; never NoOp, AlreadyApplied, or success |
| Preview invalidated | Authority, projection version, completeness, viewport, support, target absence, or aggregate admission changes before confirmation | Refuse dispatch and require a refreshed preview | Preserve read-only rows; focus the visible failure/recovery region |
| Ambiguous dispatch | Timeout, retryable gateway error, cancellation not requested by the caller, or renderer loss after request send | Retain the same ULID, intent, baseline, and fixed-aggregate ownership for status lookup or same-id redispatch | Never release as terminal or mint a replacement id |
| Unqualified status | Missing/mismatched tracking identity, zero event evidence, rejected, publish-failed, timed-out, or unknown status | Keep the distinct non-success state and do not confirm | Duplicate and permission failures remain localized rejections; retryable/unknown evidence remains recoverable without double dispatch |
| Qualified confirmation | Exact tracked command has event evidence; complete current re-query contains target; current projection version differs from nonblank preview baseline | Transition to confirmed and update visible rows from the authoritative read | Audit remains pending/delayed/unavailable unless separately proven; no fabricated receipt |
| Non-causal projection | Target appears with unchanged version, version changes without exact-command event evidence, target is absent, or evidence is page-scoped/stale | Do not confirm | Remain projection-pending or unable-to-verify with explicit recovery |
| Cancel or Escape | Preview is open before dispatch | Close without I/O and return focus to the launcher | Abandon only a pre-dispatch lease; never cancel accepted work |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:202` -- replace the direct-submit grant form with preview handoff, focus trap, live rechecks, tracked dispatch, retained recovery, and complete-projection confirmation; preserve the review rows and removal ownership.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:24` -- reuse existing preview/focus/responsive/forced-colors layout hooks without defining theme primitives.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:8` -- current lifecycle lacks preview baseline, caller-owned message identity, exact-command event evidence, and causal confirmation.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs:11` -- retained circuit state currently loses preview/version/ambiguous-dispatch evidence.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs:13` -- preserve Story 4.1's pure availability handoff; production readiness becomes true only after the actual preview exists.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:11` and `TenantsBffComposition.cs:33` -- add the fail-closed grant-preview composition seam and replace the intentional production `false` readiness declaration.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23` -- reuse the bounded, cursor-safe, version-consistent complete walk for preview and post-command evidence.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:67`, `TenantCommandGateway.cs:333`, and `UnavailableTenantCommandGateway.cs` -- add caller-owned tracked grant dispatch, fixed identity, ULID validation, and ambiguous transport mapping while retaining compatibility.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:27` -- retain/adopt ambiguous and accepted grant reconciliation without weakening unrelated aggregate concurrency or terminal-only release.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:20` -- reuse opaque projection-version advancement only in conjunction with exact-command event evidence.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs:22` and `src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs:7` -- read-only enforcement/contracts; do not change domain behavior or payload shape.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- add parity-matched preview, acknowledgement, invalidation, ambiguity, and recovery whole strings.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2170`, `State/GlobalAdministratorGrantCommandSnapshotTests.cs:13`, `Services/Gateways/TenantCommandGatewayTests.cs:35`, `Services/Gateways/TenantsBffCompositionTests.cs:75`, and `State/TenantAggregateCommandAdmissionGateTests.cs:11` -- replace superseded direct-submit/presence-only expectations and cover the full matrix.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned and strictly read-only; its row state is not verification evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantPreview.cs` and `Services/Gateways/ITenantsBffComposition.cs`, `TenantsBffComposition.cs` -- add an immutable BFF-owned preview that validates current authority, exact absent target, matching complete/current/versioned evidence, and all ten required safe facts; expose readiness only when this concrete composition is installed.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs` -- model previewed intent, baseline projection version/count, one message id, attempt/event evidence, ambiguity, and qualified confirmation; status must require verified fixed-command identity and positive event evidence, and SignalR must only request reconciliation.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs` and `State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` -- retain the minimum support-safe grant attempt needed to adopt request-sent/ambiguous/accepted/projection-pending work after renderer replacement; preserve monotonic state and terminal-only release.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, and `UnavailableTenantCommandGateway.cs` -- add explicit-message-id grant dispatch, preserve the exact UserId, verify the ULID, pass the fixed aggregate in status handles, and map timeout/HTTP/retryable gateway outcomes to a same-id ambiguous result rather than terminal failure.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- render and focus-contain the complete preview, require deliberate acknowledgement, reauthorize and rebuild matching evidence immediately before dispatch, preserve last-confirmed rows, dispatch/retry one tracked attempt, adopt retained attempts, and confirm from the complete loader only after target presence plus baseline advancement plus exact-command event evidence.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`, `Resources/TenantsResources.resx`, and `TenantsResources.fr.resx` -- add only necessary preview layout/focus hooks and parity-matched whole strings for every preview item, invalidation, ambiguity, lifecycle, and recovery state.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs`, `State/TenantAggregateCommandAdmissionGateTests.cs`, `Services/Gateways/TenantCommandGatewayTests.cs`, `Services/Gateways/TenantsBffCompositionTests.cs`, and `Components/GlobalAdministratorsPageTests.cs` -- cover every matrix row, preview completeness and stale-submit refusal, exact literal whitespace/case preservation, stable ULID redispatch/adoption, verified status/event evidence, complete advanced-version confirmation, same-version/unrelated/page-scoped refusal, no optimistic rows, authorization/viewport/admission races, focus/Escape/keyboard/live-region behavior, EN/FR parity, forced colors, responsive fail-closed behavior, support safety, and grant/remove/unrelated-tenant lock isolation; use PascalCase for new test methods.

**Review-derived safeguards:**
- Treat confirmation as a single-flight operation. Capture and recheck the exact preview object, lease, message id, acknowledgement, and mutation generation across every await; a superseded handler must not dispatch a later preview or clear a lease another handler marked dispatched.
- On authorization loss, preview-composition failure, tracking mismatch, or blank acceptance correlation, preserve recoverability: abandon an undispatched lease, retain a dispatched same-id attempt, and collapse privileged rows whenever authoritative composition denies the caller. Never strand a marked lease.
- Compose the initial preview from a new complete bounded walk. Render every preview fact and its failure-specific recovery from BFF-owned completeness evidence; missing resource/evidence must keep confirmation unavailable.
- Keep lifecycle evidence monotonic. Pending/unknown status is handled before exact-identity qualification, `Received`/`Processing` cannot regress event evidence, `EventsStored` and `EventsPublished` are qualified like `Completed` only with positive exact-command event evidence, and SignalR triggers reconciliation without changing lifecycle state.
- Retain request-sent work across renderer replacement as explicitly ambiguous until acceptance identity exists, and make adoption wake the replacement for same-id redispatch or status lookup. A changed projection during ambiguous recovery must remain recoverable without releasing or orphaning the fixed lock.
- Make the modal interaction real rather than declarative only: focus actual interactive controls, restore the launcher, contain keyboard and assistive focus, and prevent background grant/removal/navigation actions while preview is open.
- On qualified confirmation, update authoritative evidence but preserve the existing paged display contract instead of replacing page one with the complete population.
- Keep strict test doubles: unexpected projection reads fail unless a test explicitly opts into repetition, and status identity is never forced true globally. Add PascalCase tests for concurrent confirmation, preview supersession, authorization collapse, composition exceptions, failure recovery copy, exact count rendering, fixed aggregate tracking handles, every event-producing status, renderer replacement, paged confirmation, and behavioral focus/background isolation.
- Treat preview creation as a cancellable, generation-bound operation. Capture the exact input and generation before the first await; Cancel, authorization collapse, disposal, input replacement, or a superseding attempt must cancel the bounded walk, prevent lease acquisition, and prevent a stale continuation from opening a preview.
- Re-evaluate authority, support, measured viewport, fixed-aggregate admission, acknowledgement, exact preview identity, and ownership inside the renderer callback that marks the lease dispatched. Evidence captured before that callback is not a dispatch permit.
- Use an actual Fluent/FrontComposer modal focus mechanism or explicit `ElementReference.FocusAsync` calls for initial focus, both Tab directions, and launcher restoration. Make the whole background subtree inert while the preview is open; disabling accordion headers or asserting `autofocus` markup is insufficient.
- Validate localized fact values as part of BFF preview completeness, not only their resource-key names. Missing, blank, or unresolved EN/FR values keep confirmation disabled and expose visible recovery.
- At every grant consumer, tracked-dispatch capability must be explicit. Retryable gateway Problem Details and non-caller transport cancellation map to same-id ambiguity; compatibility callers must preserve the returned message id, safe recovery key, fixed aggregate handle, and verified command identity rather than stranding a marked lease.
- Label correlation-less same-id redispatch as delivery retry, not status refresh. Projection confirmation copy must describe observed evidence without promising downstream authority-enforcement timing, and French resources must use native-quality diacritics.
- Add focused tests for initial-preview cancellation/supersession, input replacement, final-arm prerequisite races, stale-preview invalidation, independent tracked-dispatch capability, retryable Problem Details, non-caller cancellation, correction identity mismatch, positive event-evidence retention across renderer replacement, resolved-resource completeness, real active-element focus containment/restoration, and honest delivery-retry copy.

**Acceptance Criteria:**
- Given an authorized operator enters an exact non-whitespace UserId, when grant is initiated, then no command is sent and an accessible, focus-contained preview shows all ten BFF-composed facts for the fixed platform scope without tenant-membership data.
- Given preview is open, when the operator cancels or presses Escape, then no command is sent and focus returns to the grant launcher; when any required fact is absent, confirmation remains unavailable with visible associated recovery.
- Given the preview remains current and deliberately acknowledged, when confirmation is submitted, then exactly one `SetGlobalAdministrator` attempt uses the fixed aggregate and one caller-owned ULID while last-confirmed rows remain unchanged.
- Given transport outcome is ambiguous or the renderer is replaced, when the attempt is retried or adopted, then the exact intent, baseline, message id, and lock are reused and a second command identity is never generated.
- Given status for the exact fixed-scope handle proves command events and a complete current re-query contains the target at a projection version advanced beyond the preview baseline, when reconciliation runs, then and only then the UI reports confirmed and refreshes rows.
- Given status, SignalR, target presence, version change, or audit availability is unqualified in isolation, when reconciliation runs, then success is not shown and the operator receives an honest pending, degraded, rejected, or unable-to-verify recovery state.
- Given the target already exists or current authority/evidence/support/viewport/admission changes, when preview or dispatch is evaluated, then the flow fails closed without data disclosure, normalization, optimistic mutation, or dispatch.
- Given EN/FR, keyboard, screen-reader, forced-colors, reduced-motion, desktop/tablet, unknown/phone viewport, and support-safety checks run, when the flow renders across its lifecycle, then stable selectors, localized whole strings, focus/live-region semantics, read-only unsafe-width behavior, and redaction all pass.

## Spec Change Log

### 2026-08-31 — Review pass 1 repair
- Triggering findings: the first independent review found confirmation races, stranded leases, non-monotonic status/SignalR transitions, incomplete recovery rendering, authority-collapse disclosure, renderer-replacement gaps, modal isolation issues, paged-display regression, and test seams that concealed those failures.
- Amendment: added the review-derived safeguards above so ownership, generation, authority, recovery, lifecycle monotonicity, BFF completeness, paging, accessibility, and verification behavior are explicit implementation obligations.
- Known-bad state avoided: a later or duplicate handler must not dispatch an unacknowledged target, a dispatched fixed-aggregate lease must never become ownerless, and unqualified evidence must never become confirmation or visible privileged data.
- KEEP: preserve exact literal UserIds, caller-owned canonical ULIDs, fixed `system / global-administrators / global-administrators` routing, complete projection walks, exact-command positive event evidence, last-confirmed rows until qualification, EN/FR whole strings, removal behavior, and the passing focused/full verification lanes.

### 2026-08-31 — Review pass 2 repair
- Triggering findings: the second review demonstrated cancellable-preview races, contradictory target rendering, a stale final dispatch arm, broken focus wrapping/restoration and modal isolation, retryable transport becoming terminal, correction consumers stranding or trusting unverified tracking, unresolved resource keys passing completeness, and required lifecycle/accessibility tests that remained declarative or absent.
- Amendment: generation-bound preview creation, last-moment live prerequisite checks, actual focus/inert behavior, resolved-resource completeness, explicit tracked capability and correction identity handling, truthful delivery-retry/enforcement copy, native French, and the named focused tests are now mandatory safeguards.
- Known-bad state avoided: Cancel cannot be undone by a stale bounded walk; an operator cannot acknowledge target A while editing target B or dispatch after support/viewport withdrawal; retryable delivery cannot release or orphan the fixed lock; and a modal or localized fact cannot claim safety from markup/key presence alone.
- KEEP: preserve the fixed identity, exact literal target, caller-owned canonical ULID, complete bounded reads, exact-command event evidence plus differing opaque projection version, same-id ambiguous recovery, last-confirmed rows, authoritative authorization collapse, paged display contract, support-safe diagnostics, and every warning-clean focused/full test that still expresses the corrected behavior.

## Review Triage Log

### 2026-08-31 — Review pass
- verdicts: 47 findings — high 15, medium 20, low 2, false 8, maybe-false 2
- findings:
  - `[medium]` `[bad_spec]` Pending or unknown status is mislabeled as a tracking mismatch — `ApplyStatus` checks identity before its null-status branch; the safeguard now requires pending/unknown handling first.
  - `[high]` `[bad_spec]` Concurrent confirmations can clear a lease already marked by another handler — the failed `TryMarkDispatched` arm nulls `_grantAdmissionLease`; the safeguard now requires single-flight ownership.
  - `[high]` `[bad_spec]` A superseded revalidation can dispatch a later preview without its acknowledgement — no preview/lease/generation identity is rechecked after the awaits; the safeguard now binds every continuation to the exact preview attempt.
  - `[high]` `[bad_spec]` Ambiguous recovery can strand a marked lease after preview invalidation — correlation-less `UnableToVerify` is neither retainable nor releasable; the safeguard now requires same-id recovery for every dispatched ambiguous state.
  - `[high]` `[bad_spec]` Renderer replacement can adopt non-ambiguous request-sent work that cannot poll or redispatch — the adopted null-correlation state fails both recovery branches; the safeguard now retains it as explicitly ambiguous and wakes the adopter.
  - `[medium]` `[bad_spec]` Initial preview facts can come from a stale cached complete snapshot — initiation passes `_completeSnapshot` without a new bounded walk; the safeguard now requires a fresh complete load before preview.
  - `[high]` `[bad_spec]` Preview-composition exceptions can escape and retain a lease — initiation catches only caller cancellation and revalidation catches nothing; the safeguard now requires bounded fail-closed recovery and correct lease disposition.
  - `[high]` `[bad_spec]` Composition can deny authority without collapsing already rendered administrator rows — the unavailable preview changes only command state; the safeguard now requires authoritative denial to collapse privileged data.
  - `[medium]` `[bad_spec]` The confirm control remains enabled after live prerequisites change — support, viewport, authority, and exact ownership are rechecked only after activation; the safeguard now requires the modal state to reflect current prerequisites.
  - `[maybe-false]` `[defer]` Focus wrappers may produce an unusable active-element sequence — the wrappers are programmatically focusable, but only a real browser active-element trace can settle Tab and Shift+Tab behavior; re-derivation must add that behavioral evidence before deciding.
  - `[medium]` `[bad_spec]` `aria-modal` leaves background actions operable — preview state does not make the surrounding surface inert or disable removal/navigation controls; the safeguard now requires background isolation.
  - `[medium]` `[bad_spec]` Failure-specific recovery is discarded — `RecoveryKey` is neither retained nor rendered; the safeguard now requires visible associated recovery.
  - `[medium]` `[bad_spec]` Most claimed BFF-composed facts are static UI text outside preview completeness — missing or contradictory resources cannot fail `IsComplete`; the safeguard now makes every rendered fact part of BFF-owned completeness evidence.
  - `[medium]` `[bad_spec]` Verified event lifecycle can regress from projection-pending to accepted — `Received`/`Processing` assigns `Accepted` unconditionally; the safeguard now requires monotonic evidence.
  - `[maybe-false]` `[defer]` Plain `TimeoutException` may escape tracked dispatch — current concrete clients normally surface gateway, HTTP, or task-cancellation exceptions; a demonstrated reachable timeout type is needed to settle whether another catch is required.
  - `[false]` `[reject]` Fixed-scope string constants necessarily diverge from command identity — current values exactly match the authoritative contract and the story intentionally fixes this identity; no bad outcome exists today.
  - `[medium]` `[bad_spec]` The component gateway forces every status identity to verified — this masks null/unverified status paths; the safeguard now requires per-case identity evidence and an exact handle assertion.
  - `[medium]` `[bad_spec]` Projection test responses repeat by default and hide extra reads — the changed default suppresses queue-exhaustion failures across the suite; the safeguard now restores strict opt-in repetition.
  - `[low]` `[bad_spec]` New tests violate the explicit PascalCase naming rule — the added methods use underscores; re-derivation must rename them.
  - `[high]` `[bad_spec]` Critical concurrency, replacement, exception, prerequisite, focus, isolation, and recovery paths lack tests — the amended verification obligations enumerate those cases.
  - `[high]` `[bad_spec]` Duplicate confirmation can make a marked lease ownerless — independently confirmed at the dispatch guard; covered by the single-flight amendment.
  - `[high]` `[bad_spec]` Authorization collapse can retain an undispatched preview lease forever — collapse cannot reconcile `Previewed` and does not abandon it; covered by the lease-disposition amendment.
  - `[high]` `[bad_spec]` Mismatched acceptance identity or blank correlation can strand the fixed lock — the resulting state is neither valid reconciliation nor terminal; covered by the same-id recovery amendment.
  - `[high]` `[bad_spec]` A second authority-resolution exception can leave the event callback and pre-dispatch lock stuck — confirmed in revalidation; covered by bounded composition recovery.
  - `[false]` `[reject]` `Int32.MaxValue` administrator counts can reach the preview — the only production caller uses the bounded complete-population loader, so that population size is unreachable.
  - `[high]` `[bad_spec]` A different target can dispatch without its own acknowledgement — confirmed by the unguarded post-await handoff; covered by exact-attempt continuation guards.
  - `[high]` `[bad_spec]` Adopted request-sent work without correlation cannot resume — confirmed by the ambiguous/status branch predicates; covered by explicit ambiguous retention and wake-up.
  - `[medium]` `[bad_spec]` Status application is non-monotonic — older received/processing evidence regresses projection-pending; covered by the lifecycle amendment.
  - `[medium]` `[bad_spec]` SignalR changes request-sent/accepted into projection-pending without event evidence — `SignalRNudge` mutates lifecycle before lookup; the safeguard now limits it to requesting reconciliation.
  - `[false]` `[reject]` Opaque version inequality violates the intended advancement rule — the design notes deliberately require a different opaque version plus exact-command event evidence because this projection exposes ETag-like tokens.
  - `[medium]` `[bad_spec]` Pending status is reported as identity mismatch — duplicate confirmation of the null-status ordering defect; covered by the status amendment.
  - `[medium]` `[bad_spec]` Unavailable previews omit associated recovery — duplicate confirmation that `RecoveryKey` is dropped; covered by BFF/recovery completeness.
  - `[high]` `[bad_spec]` Authority loss during composition leaves privileged rows visible — duplicate confirmation of the collapse gap; covered by authoritative denial handling.
  - `[medium]` `[bad_spec]` Dialog semantics do not isolate focus or background actions — background controls remain reachable; covered by real modal containment.
  - `[medium]` `[bad_spec]` Six or more preview facts cannot participate in BFF completeness — duplicate confirmation of static consequence copy; covered by the BFF evidence amendment.
  - `[low]` `[bad_spec]` Added tests use underscore-separated names — duplicate confirmation of the naming deviation; re-derivation must use PascalCase.
  - `[medium]` `[bad_spec]` Qualified confirmation replaces the paged display with the entire complete population — `_snapshot = snapshot` removes paging and can render all administrators; the safeguard now preserves the paged display contract.
  - `[medium]` `[bad_spec]` Rendered consequence counts are only selector-tested — the stub localizer hides the numeric binding; the amended tests must assert both actual values.
  - `[medium]` `[bad_spec]` The page's fixed aggregate status handle is not verified — the stub ignores the handle and forces identity true; the amended tests must capture message, correlation, and aggregate.
  - `[medium]` `[bad_spec]` `EventsStored` and `EventsPublished` qualification is untested — only `Completed` exercises the branch; the amended matrix requires positive and zero-event cases for all three statuses.
  - `[high]` `[bad_spec]` Renderer replacement is tested only below the component boundary — no test disposes a page and proves automatic same-id adoption/resumption; the amended tests require that end-to-end component transition.
  - `[medium]` `[bad_spec]` Focus containment and launcher restoration lack behavioral verification — selector tests cannot prove active-element behavior; the amended tests require browser-level focus and background isolation evidence.
  - `[false]` `[reject]` The product diff should itself prove skill invocation — workflow execution is control-plane evidence, not a product-code responsibility.
  - `[false]` `[reject]` Product implementation diverges merely because the slug admits a narrower reading — repository planning context legitimately elaborates the named story.
  - `[false]` `[reject]` Absence of `operator_actions` is a defect — no acceptance criterion requires an external human setup action, so the ordinary completion branch applies.
  - `[false]` `[reject]` Repository tests must prove subagent use, commit existence, or orchestrator bookkeeping — those are workflow controls verified outside the product test surface.
  - `[false]` `[reject]` The in-review spec must already contain final command results and historical sprint proof — `## Auto Run Result` and terminal status are written only after review, while the unchanged sprint file is intentionally not evidence.

### 2026-08-31 — Review pass 2
- verdicts: 31 findings — high 8, medium 16, low 1, false 4, maybe-false 2
- findings:
  - `[medium]` `[bad_spec]` Same-attempt stale-preview invalidation has no behavioral test — current code compares the rebuilt preview, but no test changes version, count, completeness, or target presence and proves zero dispatch plus undispatched-lease release; the amendment names that matrix row.
  - `[medium]` `[bad_spec]` Modal focus containment is verified only as markup — existing tests never assert initial active element, Tab/Shift+Tab wrapping, or actual launcher restoration; the amendment requires browser-level active-element evidence.
  - `[medium]` `[bad_spec]` Tracked grant dispatch capability is not tested independently — no case enables legacy dispatch while disabling tracked dispatch, so deleting the evaluator guard would stay green; the amendment requires an independent capability case.
  - `[medium]` `[bad_spec]` Renderer replacement does not verify retained positive event evidence — adoption is tested only with `HasCommandEventEvidence: false`; the amendment requires projection-pending remount with monotonic positive evidence through final confirmation.
  - `[high]` `[bad_spec]` Cancel during the initial complete walk can later reopen the preview and reserve the lock — preview creation captured no generation and its final callback ignored Cancel's generation increment; generation-bound cancellable initiation now prevents stale lease acquisition.
  - `[high]` `[bad_spec]` The target field remains editable during preview composition — target A is captured before awaits while the field can change to B, producing a contradictory confirmation surface; the amendment binds and freezes the exact input for the operation.
  - `[medium]` `[bad_spec]` The start focus sentinel does not focus Cancel — it toggles `AutoFocus`, and `OnAfterRenderAsync` only clears the flag; actual focus calls or a Fluent focus mechanism are now required.
  - `[medium]` `[bad_spec]` Cancel and Escape do not reliably restore the launcher — the derivation removed the launcher reference and replaced `FocusAsync` with a dynamic `autofocus` attribute; actual restoration is now mandatory.
  - `[medium]` `[bad_spec]` The modal leaves background status content focusable — only the list is inert and disabled accordion headers do not isolate descendants; the amendment requires whole-background keyboard and assistive inertness.
  - `[high]` `[bad_spec]` Ambiguous compatibility dispatch strands the correction surface — the gateway returns request-sent tracking, but correction failure application discards its message id and recovery key after marking a nonterminal lease; all grant consumers must retain same-id ambiguity.
  - `[high]` `[bad_spec]` Correction status can advance from an unverified non-fixed handle — its handle omits the aggregate and status application ignores verified identity before creating retained reconciliation; fixed identity verification is now explicit at every consumer.
  - `[high]` `[bad_spec]` Tracked capability defaults to generic dispatch — the correction consumer inherits a fail-open default while using the compatibility method, exposing ambiguous-lease loss; tracked support must now be supplied explicitly.
  - `[maybe-false]` `[defer]` Retained reconciliation does not compare preview target with reconciliation target — every current internal producer binds them, so a contradictory producer must be demonstrated before this is a reachable defect.
  - `[false]` `[reject]` Admission must parse every retained tracking value — current producers already mint or validate canonical message ULIDs, correlation ids are intentionally opaque, and no invalid production producer was shown.
  - `[medium]` `[bad_spec]` Initial and confirmation bounded walks ignore cancellation — `CancellationToken.None` prevents Cancel, authority collapse, disposal, or supersession from stopping I/O and amplifies the stale-initiation race; generation-bound cancellation is now required.
  - `[false]` `[reject]` Fail-closed preview-composition exception mapping hides a defect — the wrapper intentionally converts composition failure into incomplete recoverable evidence, exactly as the specification requires.
  - `[medium]` `[bad_spec]` A control labelled status refresh can issue a command POST — correlation-less ambiguity performs same-id redispatch without disclosing delivery retry; the amendment requires truthful action copy.
  - `[medium]` `[bad_spec]` The correction test gateway forces all statuses to verified identity — this masks the production identity gap and violates the strict-double safeguard; mismatch coverage is now named explicitly.
  - `[medium]` `[bad_spec]` Focus and background isolation tests assert attributes rather than behavior — broken sentinel wrapping and launcher focus remain green; the amendment requires real active-element and background-focus evidence.
  - `[medium]` `[patch]` English consequence copy promises authority is exercisable after projection confirmation while admitting downstream enforcement timing is unknown — re-derivation must describe confirmation as observed evidence, not an enforcement boundary.
  - `[low]` `[patch]` New French preview strings omit native diacritics throughout — the direct resource correction is carried into re-derivation as native-quality French copy.
  - `[high]` `[bad_spec]` Initial-preview cancellation race is independently reproduced — Cancel clears state but the stale continuation can acquire the fixed lease and reopen; covered by generation-bound preview initiation.
  - `[high]` `[bad_spec]` Support or viewport withdrawal can race the final dispatch arm — revalidation snapshots prerequisites before a later renderer callback that marks dispatch without rechecking them; the final-arm callback must evaluate current evidence.
  - `[high]` `[bad_spec]` Retryable gateway Problem Details can become terminal — tracked dispatch ignores `EventStoreGatewayException.Retryable` outside its status-code list, losing same-id recovery; the gateway amendment requires retryable metadata mapping.
  - `[medium]` `[bad_spec]` A non-caller `OperationCanceledException` can escape tracked dispatch — the page compensates, but the correction caller converts it to terminal failure; transport cancellation without caller cancellation must map to same-id ambiguity.
  - `[maybe-false]` `[defer]` Malformed global-administrator correction intents can map tenant commands to removal reconciliation — normal evaluator/routing pairs domain and command type correctly; a reachable malformed public caller must be demonstrated before repair.
  - `[false]` `[reject]` Preview count can overflow at `Int32.MaxValue` in production — the bounded loader caps the complete population at 50 pages of 20 rows, making that input unreachable.
  - `[false]` `[reject]` Differing opaque projection tokens permit confirmation without causal evidence — confirmation already requires exact-command positive event evidence, complete current fixed-scope evidence, and target presence, matching the deliberate opaque-token design.
  - `[medium]` `[bad_spec]` Resource-key presence is mistaken for localized-fact presence — unresolved or blank localized values can leave `IsComplete` true; the amendment makes resolved EN/FR values part of completeness.
  - `[medium]` `[bad_spec]` Launcher focus restoration has no behavioral assertion — checking only `autofocus` cannot prove the browser's active element; covered by the active-element requirement.
  - `[medium]` `[bad_spec]` The correction strict double globally rewrites identity evidence — this independently confirms that mismatch paths cannot be exercised; the amendment forbids global identity forcing and names the negative case.

### 2026-09-01 — Review pass 3
- verdicts: 40 findings — high 11, medium 23, low 3, false 2, maybe-false 1
- findings:
  - `[medium]` `[patch]` Grant-preview readiness accepted fallback-only localization — preview completeness now requires explicit resolved values in both invariant English and French resource sets.
  - `[high]` `[patch]` Count resources could omit or corrupt required placeholders — composition now formats sentinel values and requires both `{0}` and `{1}` to survive valid composite formatting.
  - `[high]` `[patch]` Initial preview authorization, complete-walk, and composition work ignored cancellation — the generation token now flows through every initial preview stage and caller cancellation is preserved.
  - `[high]` `[patch]` The renderer's final dispatch arm could use stale preview readiness or ownership — the callback now rechecks live readiness, exact attempt identity, acknowledgement, capabilities, and the exact lease before marking dispatch.
  - `[medium]` `[patch]` Correction adoption discarded retained safe recovery — reconciliation now preserves support-safe message and recovery keys, including same-command delivery retry guidance.
  - `[medium]` `[patch]` Correction reconciliation could lose an ambiguous restore's lifecycle meaning — restore adoption now retains ambiguity, delayed audit state, assertive announcement, and same-ID retry semantics.
  - `[medium]` `[patch]` Later correction progress retained stale failure copy — accepted, pending, already-applied, and event-evidence transitions now clear obsolete safe message, recovery, rejection, and ambiguity fields.
  - `[medium]` `[patch]` Grant-specific ambiguity keys could leak onto removal correction states — ambiguous same-ID grant recovery is now restricted to restore-access actions; removal stays terminal and uses correction-specific copy.
  - `[low]` `[patch]` French grant resources contained pervasive missing diacritics and awkward wording — the Story 4.2 resource block now uses native-quality French copy.
  - `[medium]` `[patch]` Initial preview cancellation and supersession had no behavioral proof — tests now cancel during authorization, complete walk, and BFF composition and assert no stale preview or lease.
  - `[high]` `[patch]` Final-arm support, viewport, readiness, and lease races lacked coverage — parameterized component tests now withdraw each prerequisite and prove zero dispatch plus exact undispatched-lease release.
  - `[medium]` `[patch]` Projection mutation between preview and final confirmation lacked an explicit refusal test — tests now mutate the evidence and require a fresh reviewed preview before dispatch.
  - `[medium]` `[patch]` Focus behavior was inferred from markup — the page now focuses referenced interactive controls and tests capture actual focus interop for initial focus, both sentinels, cancel, and launcher restoration.
  - `[medium]` `[patch]` Retryable gateway and transport-cancellation branches were under-specified in tests — gateway cases now pin retryable Problem Details and non-caller cancellation as same-message ambiguity.
  - `[medium]` `[patch]` The correction retry action was not proven to redispatch the retained identity — a component test now clicks the honest delivery-retry action and asserts the original message ID is reused.
  - `[medium]` `[patch]` Correction status tests did not prove mismatched identity fails closed — negative tests now retain the attempt and prevent projection confirmation from unverified status.
  - `[medium]` `[patch]` Renderer replacement did not prove positive event evidence survives — replacement tests now retain event evidence through adoption and release only on qualified projection confirmation.
  - `[medium]` `[defer]` Concurrent workspace tabs lack browser visibility and focus proof for inactive panels — this separate committed change is outside Story 4.2; an authenticated browser active-element and visibility trace would settle it.
  - `[false]` `[reject]` Tenant-audit tests hide user-facing Razor text by removing `@code` — the scan intentionally excludes identifiers such as `MessageId`; markup, resources, rendered output, and support-safety tests still cover visible copy, and no forbidden literal was demonstrated.
  - `[false]` `[reject]` The reviewed diff improperly mixes unrelated feature changes — the cited submodule, AppHost, and workspace changes are separate concurrent commits; Story 4.2's uncommitted and staged file set does not own or revert them.
  - `[high]` `[patch]` Cancel or target replacement could leave initial preview I/O running — cancellation is now generation-bound and observable at each awaited initial stage, with stale continuations unable to reserve admission.
  - `[medium]` `[patch]` Replacing the target after an open preview left the old intent and lease active — exact input replacement now invalidates that preview and abandons only its undispatched lease.
  - `[high]` `[patch]` Preview readiness could disappear after revalidation but before lease marking — the final renderer arm now checks current BFF preview readiness and invalidates the undispatched attempt on loss.
  - `[maybe-false]` `[patch]` The existing keyed Fluent autofocus mechanism might not focus the intended browser element — no runtime failure was proven, but this grouped focus repair removes the uncertainty with explicit `ElementReference.FocusAsync` calls and target-aware interop tests.
  - `[medium]` `[patch]` EN/FR localized-fact fallback could make an incomplete preview appear complete — both cultures now require every explicit nonblank, non-key-echo fact value.
  - `[high]` `[patch]` Malformed or incomplete administrator-count formats could pass readiness and fail during render — count composition now fails closed unless valid formatting consumes both count arguments.
  - `[high]` `[patch]` Correction confirmation used ordered-version semantics incompatible with opaque projection tokens — restore confirmation now follows Story 4.2's nonblank opaque-version inequality only after exact-command positive event evidence.
  - `[high]` `[patch]` Ambiguous removal delivery could become a nonterminal state with no safe retry and strand admission — removal ambiguity is now terminal correction failure and releases through the normal completion path.
  - `[low]` `[patch]` French preview and lifecycle text was not publication quality — corrected diacritics, punctuation, terminology, and evidence wording now match the English intent without overclaiming.
  - `[high]` `[patch]` The combined race, localization, correction, gateway, focus, and retention matrix was incomplete — focused Story 4.2 and correction tests now exercise all named branches and pass as a 549-test lane.
  - `[medium]` `[patch]` Initial preview cancellation was not verified at all three await boundaries — the component suite now proves cancellation during authorization, bounded projection loading, and preview composition.
  - `[high]` `[patch]` No test proved the last renderer callback rejects changed prerequisites — parameterized tests now remove authorization/support/viewport/readiness/ownership evidence at the final arm and assert no command.
  - `[medium]` `[patch]` Focus tests did not identify the actual focused target — test JS interop now records element-reference IDs and asserts preview Cancel, acknowledgement, and launcher targets.
  - `[medium]` `[patch]` Gateway tests omitted retryable Problem Details and non-caller `OperationCanceledException` — both outcomes are now asserted as recoverable ambiguity with the caller-owned message identity.
  - `[medium]` `[patch]` Localizer tests omitted absent, blank, key-echo, culture-specific, and count-format failures — the BFF suite now covers each fail-closed resource shape.
  - `[medium]` `[patch]` Correction identity-negative status behavior was untested — tests now prove unverified restore status cannot contribute event evidence or confirm projection.
  - `[medium]` `[patch]` The correction same-ID retry control was not exercised — the new interaction test proves the label, retained identity, marked lease, and retry dispatch.
  - `[medium]` `[patch]` Positive grant event evidence could be lost during correction renderer replacement without detection — adoption and qualified-release tests now pin monotonic evidence retention.
  - `[low]` `[patch]` Main-flow ambiguous delivery still used generic refresh wording — the visible action now says delivery retry and its recovery warns against creating a new grant attempt.
  - `[medium]` `[defer]` Concurrent Memories secret-store topology lacks deployed Aspire health evidence — this separate AppHost change is outside Story 4.2; Aspire model inspection and a healthy deployment with the intended provider would settle it.

### 2026-09-01 — Review pass
- verdicts: 61 findings — high 1, medium 16, low 37, false 7, maybe-false 0
- findings:
  - `[low]` `[reject]` Background isolation stops at the page content area, leaving the app shell tabbable — refuted for keyboard users: the preview's start/end focus sentinels return focus to Cancel/Acknowledge, so Tab never reaches the shell; only the shell's assistive-technology exposure remains, and the fix is a layout-shell restructure rather than a direct correction.
  - `[medium]` `[patch]` New focus interop is unguarded — `OnAfterRenderAsync` and both sentinel handlers now route through `FocusSafelyAsync`, which swallows `JSDisconnectedException`/`ObjectDisposedException` exactly as `GlobalAdministratorCorrectionPanel` and five sibling components already do.
  - `[false]` `[reject]` `ConfigureAwait(false)` on the focus chain drops off the renderer Dispatcher — the recorded circuit-teardown mode needs an `EventCallback`/`StateHasChanged` off-Dispatcher; this method invokes neither, and `ElementReference.FocusAsync` is a plain interop call.
  - `[low]` `[reject]` Two raw `<fluent-button>` custom elements — `FluentButton` exposes no `ElementReference`, unlike the `FluentTextInput`/`FluentCheckbox` `.Element` used elsewhere in the file, so the raw element is the only way to obtain the focus target the story requires.
  - `[medium]` `[patch]` Localization completeness runs a full resource scan on the render path — the fixed-key walk is now resolved once per composition instead of twice per read, several times per render.
  - `[low]` `[reject]` The validated culture set is hardcoded to invariant + `fr` — the app ships exactly two resource sets and configures no additional supported cultures, so the checked set is the shipped set.
  - `[low]` `[reject]` `resourceLocalizer` is an optional trailing parameter whose absence means "not ready" — every collaborator on this type is optional by design and absence already fails closed with its own localized reason.
  - `[low]` `[reject]` Two hand-synchronized lists of required fact keys — real drift risk, but deriving one from the other is a restructure, not a direct correction; the new preview-key coverage test pins the two lists' lengths instead.
  - `[low]` `[reject]` Duplicated capability predicates — behaviour is identical and the memoization above removes the cost that made the duplication matter.
  - `[false]` `[reject]` `ConfirmGrantAsync`'s cancellation catch has no exception filter — a transport `OperationCanceledException` never reaches it: `DispatchGrantAsync` wraps the gateway call in its own bare `catch` that maps to same-message-id ambiguity.
  - `[low]` `[reject]` The correction retry path is uncancellable and over-catches — `CancellationToken.None` matches the panel's pre-existing submit paths, and mapping a swallowed failure to ambiguity is the fail-closed outcome the intent requires.
  - `[low]` `[reject]` Grant-namespaced recovery keys leak into the removal path — the shared recovery string reads "Refresh the complete fixed-scope projection and rebuild the preview", which is accurate for the correction flow too; only the key's namespace is grant-flavoured and users never see it.
  - `[low]` `[patch]` `Tenants.Correction.GlobalAdmin.AlreadyGranted` is orphaned — deleted from both resx files and from the correction-panel localizer double; the localizer parity gate now passes on the reduced set.
  - `[low]` `[reject]` "Already a global administrator" is reported as a verification failure — the intent explicitly forbids `AlreadyApplied`, NoOp, and success for an existing grant target, so a non-success state is mandated; only the generic state label reads oddly, and no other state exists to move it to.
  - `[false]` `[reject]` `HasCommandTracking` and `TryGetTrackingHandle` disagree — deliberate and consistent: `RefreshStatusAsync` branches on the ambiguous/no-correlation shape before ever calling the handle-based status path, so no caller reaches an unexpected `false`.
  - `[low]` `[reject]` No dedicated unavailable reason for missing tracked dispatch — adding an enum member plus reason-to-copy wiring is more than a direct correction, and the generic lifecycle-support copy is not misleading.
  - `[low]` `[reject]` `SupportsTrackedGrantDispatch` default flipped without compiler enforcement — fail-closed is correct and both construction sites set it explicitly; making it `required` changes the public record's contract.
  - `[low]` `[defer]` `Retryable == false` is not honored and only the grant dispatch path was updated — over-classifying as ambiguous is fail-closed, and the sibling tracked dispatches are unchanged pre-existing behaviour outside this story's fixed-scope intent.
  - `[medium]` `[defer]` Deleted workspace-panel test coverage was not replaced — confirmed, and the deletion is in this story's own commit `8da765ad`, not a separate concurrent one; bUnit cannot observe Fluent's client-side panel flip, so a browser lane is what would settle it.
  - `[medium]` `[reject]` Five root gitlinks move with no File List declaration — real and reproduced by `validate-story-gitlinks.py`, but the only fix is to declare them in this build's spec File List.
  - `[low]` `[reject]` French repair is partial and introduces terminology drift — the untouched neighbours are pre-existing strings this story never edited, and "statut"/"état" both render correctly.
  - `[low]` `[reject]` The new localization-failure copy is not actionable for its audience — a missing satellite resource is a deployment defect; naming it is more useful than hiding it behind a generic support pointer.
  - `[low]` `[patch]` Dead `_grantPreviewInFlight` branch plus an unmarshalled cross-thread read — the branch is defence in depth behind the input's `disabled` term and is kept; the missing half was that no test pinned that `disabled` term, which the target-replacement test now asserts on both sides of the invalidation.
  - `[false]` `[reject]` Pending status arrives with `HasVerifiedCommandIdentity` still false — pending is classified first: `status.Status is null` returns before the identity gate is reached.
  - `[high]` `[patch]` `status.EventCount is null` on `EventsStored`/`EventsPublished` collapses to `UnableToVerify` — confirmed end to end: `CommandStatusRecord.EventCount` is "Completed status only" and `AggregateActor.WriteAdvisoryStatusAsync(command, CommandStatus.EventsStored)` leaves it null, so the story's own positive event evidence was reported as missing on both the grant page and the correction panel. Both snapshots now qualify `EventsStored`/`EventsPublished` on the status alone and keep the positive-count requirement for `Completed`; the two theories that pinned the fictional counts were rewritten to the shipped shape.
  - `[low]` `[patch]` `CultureInfo.GetCultureInfo("fr")` sits outside the fail-closed try — moved into its own guarded lookup so globalization-invariant hosting fails closed instead of throwing out of a render-path property.
  - `[low]` `[reject]` Only invariant and `fr` cultures are validated — same refutation as above: those are the shipped resource sets.
  - `[low]` `[reject]` A null localizer is indistinguishable from incomplete resources — absence already produces the localized unavailable reason and recovery.
  - `[medium]` `[patch]` Delivery-retry click can do nothing silently — `CanRefreshGrantStatus` now withdraws the action when the redispatch's own live prerequisites cannot hold, and a new test proves the withdrawal.
  - `[medium]` `[patch]` `isRedispatch` with lapsed live prerequisites returns with no state change or feedback — same root cause and same fix as the entry above.
  - `[low]` `[reject]` Correction delivery-retry guards return without a render — the panel's inner guards mirror `CreateCorrectionDecision().CanRefresh`, the same predicate that enables its button, so the silent window is a narrow race rather than a reachable dead end.
  - `[low]` `[reject]` Component disposed while the tracked redispatch await is outstanding — the `InvokeAsync` throw happens only during renderer teardown, where the renderer already absorbs it.
  - `[low]` `[reject]` The bare catch also swallows cancellation and disposal — mapping them to retained ambiguity is the fail-closed outcome; releasing them as terminal is what the intent forbids.
  - `[medium]` `[patch]` `RequestSent` retains a stale message id on the untracked revoke path — it now takes exactly the identity its caller owns, so the revoke path returns to letting acceptance supply the id instead of pairing an old message id with a new correlation id.
  - `[false]` `[reject]` A non-ambiguous failure clears an established correlation id — unreachable: every resubmit passes through `RequestSent`, which has already cleared `CorrelationId` to null.
  - `[false]` `[reject]` `_snapshot is null` short-circuits the tracked-dispatch requirement — with no snapshot there is no intent and nothing is submittable, so no unsupported path opens.
  - `[medium]` `[patch]` `JSDisconnectedException` from the new focus interop — same entry and same fix as the unguarded-focus finding above.
  - `[low]` `[reject]` Shell chrome outside the inert stack — same refutation as the background-isolation finding above.
  - `[medium]` `[defer]` Inactive workspace panels lost their invisibility guarantee — same entry as the deleted-coverage finding above.
  - `[low]` `[patch]` `_focusGrantAcknowledgementPending` is never set true — the dead field and its `OnAfterRenderAsync` branch were deleted; the acknowledgement checkbox is still focused by the end-sentinel handler.
  - `[low]` `[reject]` `SupportsTrackedGrantDispatch` initializer removed — same as above.
  - `[low]` `[patch]` Orphaned EN/FR resource pair plus a stale test-double string — same entry and same fix as the orphan-key finding above.
  - `[low]` `[reject]` Removed revoke `AlreadyApplied` and gateway-exception tests — the revoke `Completed`+`EventCount == 0` arm is unreachable in production because the platform writes a null count instead of zero; re-adding a test for it would pin a shape the platform never emits.
  - `[low]` `[reject]` Adjacent French `Remove.*` values keep undiacritized text — pre-existing strings this story never touched.
  - `[low]` `[patch]` Target-replacement invalidation is reachable only through bUnit and the `disabled` term is unpinned — the target-replacement test now asserts the field is disabled while a preview is open and live again after invalidation.
  - `[medium]` `[patch]` The grant page's delivery-retry action is never clicked by any test — the renderer-replacement test now clicks it and proves a third dispatch on the retained message id.
  - `[medium]` `[defer]` Inactive workspace tab panels lost their `hidden`/`aria-hidden` assertions with no browser lane — same entry as above.
  - `[medium]` `[patch]` The localization readiness gate is a production kill switch tested only against substitutes — a new test resolves the real `IStringLocalizer<TenantsResources>` over the shipped resources and asserts the gate holds, plus a test tying the gate's key list to the keys a real preview renders.
  - `[low]` `[defer]` The new ambiguity classification was adopted only in the grant dispatch method — same entry as the `Retryable` finding above.
  - `[medium]` `[patch]` Culture mutation and double enumeration on the render path — same entry and same fix as the memoization above.
  - `[low]` `[patch]` `GetCultureInfo` outside the guarded walk — same entry and same fix as above.
  - `[low]` `[reject]` The forbidden-copy scan now truncates `.razor` inputs at `@code` — all user-visible copy comes from the resx files, which are still scanned whole; the narrowing only stops matching C# identifiers.
  - `[medium]` `[defer]` The AppHost Memories secret-store component name is coupled to `EmbeddingSecretStore.SecretStoreName` with no topology test — a separate concurrent commit (`7453ba5b`) outside this story; already recorded in this spec's deferred list.
  - `[low]` `[defer]` Command-status semantics are asserted only against stubs — the concrete `EventCount` assumption is now corrected from the platform source, but nothing pins EventStore's status contract from this repository.
  - `[false]` `[reject]` "Advanced" is implemented as opaque-token inequality — the Design Notes settle this deliberately: this projection exposes ETag-like tokens, so inequality plus exact-command event evidence is the intended rule.
  - `[low]` `[reject]` Authorization is exercised through a private-field poke — a test technique for an in-process component, not a production defect.
  - `[medium]` `[defer]` Focus containment and background inertness are proven at the interop layer, not in a browser — real, and unsettled by this pass; a browser active-element and background-focus trace is what would close it.
  - `[low]` `[defer]` Measured viewport is driven through the observation singleton and never measured — same browser-lane entry as above.
  - `[low]` `[reject]` Localization completeness is verified for cultures a third-culture viewer will not see — same refutation as the hardcoded-culture entries.
  - `[low]` `[reject]` The message-id exposure scan now covers markup only — same refutation as the forbidden-copy entry.
  - `[medium]` `[reject]` The diff carries material outside any reading of the intent (gitlinks, AppHost, tab migration) — the gitlink half's only fix is to edit this build's spec File List; the AppHost and tab halves are already deferred.

## Design Notes

The preview baseline is the complete fixed-scope projection version captured before dispatch. A different post-command version is necessary but not sufficient: confirmation also requires positive event evidence from status whose message, correlation, and fixed aggregate identity match the retained attempt, plus exact ordinal target presence in a new complete current walk. This prevents pre-existing, concurrent, page-scoped, or unrelated projection observations from becoming success.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean UI test assembly.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorGrantCommandSnapshotTests -class Hexalith.Tenants.UI.Tests.State.TenantAggregateCommandAdmissionGateTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests` -- expected: focused grant preview, lifecycle, gateway, lock, and component matrix passes with no skipped/not-run tests.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` -- expected: maintained full UI test lane passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: full solution builds with zero warnings and errors.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md` -- expected: every moved root gitlink is declared or no gitlink moved.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

### Summary

Follow-up review pass over the whole Story 4.2 change since `7e88a571`. Four independent review layers reported 61 findings; each was verified at its cited location before triage. The pass found one high-severity correctness defect that had shipped in the story's core evidence rule: the platform writes `EventsStored`/`EventsPublished` command statuses with no `EventCount` at all -- `CommandStatusRecord.EventCount` is documented and implemented as "Completed status only" -- so requiring a positive count for those two statuses reported the story's own positive event evidence as missing, on the grant page and the correction panel alike. Eight patch entries were applied and re-verified; five items were deferred with settlement criteria.

### Files Changed

- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs` -- qualifies `EventsStored`/`EventsPublished` on the status itself and keeps the positive-count requirement for `Completed`.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` -- same event-evidence correction, and `RequestSent` now takes exactly the identity its caller owns so the untracked revoke path no longer pairs a stale message id with a new correlation id.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- routes every programmatic focus through a disconnect-safe helper, withdraws the delivery-retry action when the redispatch's own live prerequisites cannot hold, and drops a dead focus flag.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` -- resolves the fixed-key localization walk once per composition and fails closed on a missing French culture instead of throwing out of a render-path property.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- remove the orphaned `Tenants.Correction.GlobalAdmin.AlreadyGranted` pair.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs` -- replaces the fictional `EventCount` shapes with the ones the platform actually writes.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- clicks the delivery-retry action and proves a same-identity redispatch, proves the action is withdrawn when its prerequisites lapse, and pins the target field's `disabled` state on both sides of preview invalidation.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs` -- exercises the readiness gate against the real `IStringLocalizer<TenantsResources>` over the shipped resources, and ties the gate's key list to the keys a real preview renders.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs` -- drops the stale localizer-double entry for the deleted resource key.

### Review Findings

- Verdicts: 61 findings -- high 1, medium 16, low 37, false 7, maybe-false 0.
- Patches applied: 8 entries -- high 1, medium 4, low 3. The high entry is the `EventCount` event-evidence correction; the mediums are the unguarded focus interop, the silent delivery-retry no-op (with its missing click test), the localization-gate cost and its missing real-resource coverage, and the stale revoke message id; the lows are the orphaned resource key, the dead focus flag, and the unpinned `disabled` term.
- Items deferred: 4 new -- the workspace tab-panel assertions deleted inside this story's own commit `8da765ad` (which corrects the earlier entry's "separate concurrent commit" attribution), the absent browser lane for focus containment and viewport measurement, the sibling tracked dispatches that classify identical transport conditions differently, and the unpinned EventStore command-status contract. The two pre-existing deferred entries are preserved unchanged.
- Rejected: 7 findings were disproved outright -- the `ConfigureAwait` circuit-crash claim (no `EventCallback` on that path), the unfiltered confirm catch (dispatch has its own bare catch), the tracking-property disagreement (the refresh path branches before it matters), pending-before-identity (pending is classified first), the cleared correlation id (`RequestSent` already nulled it), the null-snapshot support short-circuit (nothing is submittable without a snapshot), and opaque-token inequality (a deliberate documented design).
- Rejected as low: the background-isolation finding (the focus sentinels contain Tab; only shell assistive-technology exposure remains and the fix is a layout restructure), the raw `<fluent-button>` downgrade (`FluentButton` exposes no `ElementReference`), the hardcoded culture set (those are the shipped resource sets), the grant-namespaced recovery copy (accurate for both flows), the "already a global administrator" state (the intent forbids `AlreadyApplied` for an existing target), and the remaining consistency/copy nits whose fixes exceed a direct correction.
- Rejected as spec-editing: the five undeclared root gitlinks and the wider "carries unrelated material" finding -- both real and reproduced, but their only fix is to edit this build's spec File List.
- Follow-up review recommendation: true. This pass patched a high-severity entry.

### Verification

- Source-linked Debug build: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false` -- 0 warnings, 0 errors.
- Full Debug UI lane: 2,725 tests, 2,724 passed, 1 failed, 0 skipped. The sole failure is `TenantsUiCompositionTests.TenantsUiProject_DoesNotHostGeneratedRestApiOrReferenceExternalApiHost(useProjectReferences: False)`, the package-consumer restore case that cannot resolve unpublished `Hexalith.EventStore.Client`/`Hexalith.EventStore.Contracts` `999.1.20-proof.fa2d1c9910f8` (nearest published `3.100.1`). It is the same pre-existing environmental failure recorded by the previous pass and is unrelated to these changes.
- Release lane: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` still stops during restore with the same `NU1102` unpublished-package condition before compilation, unchanged from the previous pass. No stale Release binary is claimed as evidence.
- `python3 scripts/validate-story-gitlinks.py ...` -- FAIL, 5 undeclared root pointer moves (Builds, Commons, EventStore, FrontComposer, PolymorphicSerializations) carried by separate committed workspace synchronization. Unchanged by this pass; declaring them would mean editing this build's spec.
- `git diff --check` -- clean.
- The complete frontmatter parses as YAML and `deferred` is one list of six items: the two prior entries plus the four appended here.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` and `_bmad-output/implementation-artifacts/deferred-work.md` were excluded from the reviewed diff and were neither read as evidence nor written.

### Residual Risks

- The Design Notes still state that `EventsStored` and `EventsPublished` "are qualified like `Completed` only with positive exact-command event evidence". That sentence encodes the platform assumption this pass disproved; the code now follows the platform contract instead, so the note is the stale half and should be corrected by whoever owns the spec.
- Package-mode and Release verification remain unavailable until the proof-version EventStore packages are published or the repository pin changes through its owning workflow.
- Authenticated live-browser acceptance on `/global-administrators` -- focus containment, background inertness, real viewport measurement -- is still absent and is now recorded as its own deferred item.
- Five unrelated committed root-submodule pointer moves keep the baseline-relative story-gitlink validator red.
