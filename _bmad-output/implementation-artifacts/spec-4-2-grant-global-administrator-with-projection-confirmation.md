---
title: '4.2 Grant Global Administrator with Projection Confirmation'
type: 'feature'
created: '2026-08-31'
status: 'in-progress'
baseline_revision: '7ec00718363a2681b2937496583b5bd652cbb3ec'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings:
  - oversized
deferred: []
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

## Review Triage Log

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
