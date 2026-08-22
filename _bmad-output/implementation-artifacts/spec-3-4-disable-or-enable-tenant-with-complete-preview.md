---
title: 'Disable or Enable Tenant with Complete Preview'
type: 'feature'
created: '2026-08-22'
status: 'in-review'
baseline_commit: '536e5c33230f2c2b04b80fb07ed0be631db9b5db'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The legacy lifecycle flow renders a complete-looking preview but can confirm from target status alone after a status check or SignalR nudge, without causal projection advancement, and can lose its tracked attempt when the flow is dismissed or remounted. Some preview copy also describes disable as soft deletion instead of reversible availability control.

**Approach:** Preserve Story 3.3's four-action eligibility kernel and the existing enable/disable UI, while binding each logical attempt to fresh authorized preview evidence, a stable message id, exact-command event evidence, and an authoritative post-baseline projection proof. Retain pending attempts until reconciliation and keep command, projection, and audit truth distinct.

## Boundaries & Constraints

**Always:** Require server-reflected global-administrator authority, usable freshness/lifecycle, safe viewport, free aggregate admission, and all preview facts before submit. Preserve literal case-sensitive tenant ids and last-confirmed detail. Confirm only from the intended status plus ordered projection-version advancement backed by the tracked command's event evidence, or equally specific safe audit provenance. SignalR only requests reconciliation; pending work retains its aggregate lease and attempt identity.

**Ask First:** Adding an endpoint, changing domain contracts/aggregate behavior, touching a submodule, making audit proof mandatory, or broadening shared command infrastructure beyond the focused lifecycle seam.

**Never:** Implement hard delete/purge, Stories 3.5/3.6 configuration commands, optimistic status changes, confirmation from acceptance/status/SignalR/pre-existing state/unrelated projection churn, or exposure of tokens, payloads, correlations, ETags, cursors, raw metadata, unsafe values, or stack traces.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible preview | Current authoritative detail, authority, support, admission, and safe viewport | Show tenant, current/intended state, operational impact, freshness marker, recovery, audit expectation, authorization, known consequences/unknowns; require exact tenant-id confirmation | Any missing or changed fact blocks with localized reason and refresh recovery |
| Accepted attempt | Fresh submit-time evidence and no retained attempt | Dispatch once with a stable message id; retain handle, baseline version, intent, and lease through reconciliation | Same intent/remount adopts the handle and polls; different intent remains blocked while pending |
| Proven outcome | Exact-command event evidence plus current matching-tenant projection at intended status and version strictly after baseline | Render Confirmed while audit remains independently pending/available | Missing, unchanged, regressed, stale, wrong-tenant, or unrelated evidence stays pending or becomes unable to verify |
| Exit or failure | Cancel/Escape before dispatch, or close/remount during pending work | Pre-dispatch exit sends nothing and restores launcher focus; pending work cannot be silently discarded | Rejection/transport/proof failure uses localized non-success copy and releases only terminal ownership |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs` and `TenantLifecycleAvailability.cs` -- reuse the dirty Story 3.3 fail-closed kernel; extend only submit-time evidence threading.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycle{ActionAvailability,CommandFlow}.razor` -- existing launch, ten-item preview, focus loop, lifecycle rendering, refresh, and close ownership.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` and `TenantMembershipCommandProvenance.cs` -- strengthen lifecycle snapshot with baseline, command-event evidence, and ordered proof; do not weaken other snapshots.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantQueryGateway,TenantQueryGateway}.cs` and `Components/Pages/TenantDetailPage.razor` -- add an unconditional, route-safe lifecycle proof carrying detail plus freshness/lifecycle/version from one read.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantCommandGateway,TenantCommandGateway,UnavailableTenantCommandGateway}.cs` and `State/TenantCommands/TenantLifecycleAttemptTracker.cs` -- retained message-id dispatch and circuit-local handle recovery.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources*.resx` and lifecycle CSS -- whole-string EN/FR availability-control, proof, dismissal, focus, and forced-colors behavior.
- `src/Hexalith.Tenants.Contracts/**` and `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` -- read-only; commands, rejections, authorization, and events already exist.

## Tasks & Acceptance

**Execution:**
- [x] `State/TenantCommands/{TenantCreateCommandModels,TenantLifecycleAttemptTracker}.cs` and `TenantMembershipCommandProvenance.cs` -- retain one logical attempt and require causal proof without changing sibling command semantics.
- [x] `Services/Gateways/{ITenantQueryGateway,TenantQueryGateway}.cs` and `Components/Pages/TenantDetailPage.razor` -- perform qualified unconditional proof reads with cancellation/generation guards and forward refresh nudges.
- [x] `Components/Tenants/Lifecycle/*.razor`, lifecycle CSS, command gateway interfaces/implementations, and `TenantsResources*.resx` -- revalidate preview at submit, reconcile retained attempts, block unsafe dismissal, and render localized support-safe states.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services}` and `sprint-status.yaml` -- cover both directions, provenance failures, remount/close/lease behavior, preview selectors, EN/FR parity, and current tracking.

**Acceptance Criteria:**
- Given any eligibility fact is missing, stale, mismatched, or changes while preview is open, when enable/disable is evaluated or submitted, then the affected action fails closed before dispatch with a visible reason and named recovery.
- Given an accepted lifecycle attempt and arbitrary status, SignalR, projection, close, or remount events, when reconciliation runs, then exactly one logical command is tracked and success appears only from causal authoritative proof.
- Given enable and disable in English/French keyboard, narrow, and forced-colors contexts, when the flow is used or exited, then complete support-safe facts, stable selectors, non-color-only states, focus containment/return, and truthful audit handoff remain intact.

## Spec Change Log

## Design Notes

Lifecycle proof is conjunctive: exact tracked-command event evidence AND intended authoritative status AND a strictly newer comparable tenant projection version. A SignalR notification may trigger that read but contributes no proof itself. Audit provenance is an optional equivalent only when it is uniquely command-specific and support-safe.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` -- expected: no undeclared gitlink movement.
