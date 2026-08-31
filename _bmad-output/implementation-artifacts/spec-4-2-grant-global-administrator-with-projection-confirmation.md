---
title: '4.2 Grant Global Administrator with Projection Confirmation'
type: 'feature'
created: '2026-08-31'
status: done
baseline_revision: '7e88a571588fc7aa769ee1af01e91113f6f9b01f'
baseline_commit: '7e88a571588fc7aa769ee1af01e91113f6f9b01f'
review_loop_iteration: 2
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

Implemented and review-hardened the fixed-scope global-administrator grant flow. The UI composes a complete localized consequence preview from fresh authoritative evidence, retains one caller-owned tracked command identity through ambiguous delivery and renderer replacement, refuses stale final-arm dispatch, and reports confirmation only after exact-command positive event evidence plus a newer complete projection containing the literal target. The correction surface now preserves the same grant evidence and retry semantics without changing removal-domain rules.

### Files Changed

- `_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md` — records the intent, three review passes, deferred concurrent risks, verification evidence, and terminal result.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` — adds cancellable preview composition, exact-attempt/final-arm guards, explicit focus behavior, same-ID recovery, and qualified projection confirmation.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` — adopts retained grant attempts, exposes honest same-ID delivery retry, and releases only terminal or qualified work.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` — adds fail-closed localization recovery, delivery-retry copy, and evidence wording that does not promise downstream enforcement.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` — provides parity-matched native-quality French preview, lifecycle, ambiguity, and recovery copy.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` — maps retryable and non-caller transport failures to retained same-message ambiguity.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` — propagates preview cancellation and requires explicit resolved EN/FR facts with valid two-count formatting.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs` — carries the grant-specific readiness evidence consumed by the fail-closed action gate.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs` — preserves ambiguous delivery retry semantics and qualified exact-command projection evidence.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs` — retains support-safe ambiguity and recovery keys across renderer replacement.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` — preserves restore evidence, rejects unverified identity, applies opaque-token advancement, and keeps removal ambiguity terminal.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs` — covers retained-identity retry, correction replacement, event-evidence retention, and qualified release.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` — covers cancellation, supersession, final-arm races, exact focus targets, ambiguity, paging preservation, and every confirmation refusal.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` — keeps correction rendering and support-safe audit copy covered.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` — pins retryable transport mapping and caller-owned tracked identities.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs` — covers missing, fallback, blank, key-echo, culture-specific, and malformed-count localization failures.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs` — verifies tracked-dispatch readiness remains independent and fail-closed.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs` — covers mismatch, ambiguity adoption, stale-state clearing, opaque advancement, and removal isolation.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs` — covers positive event evidence, unchanged/noncausal projection refusal, and honest retry recovery.

### Review Findings

- Review pass 3 recorded 40 rows: high 11, medium 23, low 3, false 2, maybe-false 1.
- Patches applied: 36 rows consolidated into 8 repair entries (`high 4`, `medium 2`, `low 2`). The grouped focus repair includes the maybe-false autofocus concern and replaces the uncertain mechanism with explicit focus calls.
- Items deferred: 2 medium findings from separate concurrent changes — inactive workspace-tab browser visibility and Memories secret-store Aspire topology. Both are recorded in frontmatter with evidence and settlement criteria.
- Rejected: the tenant-audit source scan does not hide a demonstrated user-facing violation; it excludes Razor code identifiers while resource, markup, render, and support-safety checks remain active.
- Rejected: unrelated AppHost, workspace, and submodule changes are not Story 4.2 changes; commit history and the explicit story staging set keep them separate and they were neither edited nor reverted by this repair.
- Follow-up review recommendation: true. This pass patched high-severity entries and at least two medium-severity entries.

### Verification

- Source-linked Debug build: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- Focused Story 4.2/correction lane: 549 passed, 0 failed, 0 skipped, 0 not run.
- Full Debug UI assembly: 2,720 passed and 1 failed out of 2,721. The sole failure is the package-consumer restore case, which cannot resolve unpublished `Hexalith.EventStore.Client` and `Hexalith.EventStore.Contracts` version `999.1.20-proof.fa2d1c9910f8`; nearest published version is `3.100.1`.
- Release UI-test build and full solution Release build stop during restore with the same `NU1102` unpublished-package condition before Story 4.2 compilation; no stale Release binary is claimed as evidence.
- Story gitlink validation reports five root gitlinks moved by separate committed workspace synchronization: Builds, Commons, EventStore, FrontComposer, and PolymorphicSerializations. Story 4.2 neither owns nor reverts those pointers.
- `git diff --check` passed, and the complete frontmatter parses as YAML.
- Matrix audit: each of the eight intent-contract rows is represented in the passing 549-test lane, including preview eligibility, existing target, invalidation, ambiguity, unqualified status, qualified confirmation, noncausal projection, and cancel/Escape behavior.
- An isolated current-Debug UI was launched against the existing Aspire endpoints and authenticated, but the local identity lacked the runtime global-administrator policy and the route redirected to AccessDenied; no unauthorized state or shared bootstrap identity was mutated.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` remained byte-for-byte outside the diff and staging set; its orchestrator row was not used as verification evidence.

### Residual Risks

- Package-mode and Release verification remain unavailable until the proof-version EventStore packages are published or the repository pin changes through its owning workflow.
- Authenticated live-browser acceptance on `/global-administrators` still needs a runtime identity that satisfies the global-administrator policy; component focus behavior is covered by target-aware JS interop tests in the meantime.
- Five unrelated committed root-submodule pointer moves keep the baseline-relative story-gitlink validator red; they are preserved as concurrent workspace state.
- The two unrelated medium findings in frontmatter retain their own browser/Aspire settlement criteria and do not change Story 4.2 acceptance.
