---
title: 'Set Namespaced Configuration with Complete Preview'
type: 'feature'
created: '2026-08-26'
status: 'in-review'
baseline_revision: 'd5ce92881019d3deca20b5fe03b84f86489dd062'
baseline_commit: 'd5ce92881019d3deca20b5fe03b84f86489dd062'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
warnings:
  - oversized
deferred: []
---

<intent-contract>

## Intent

**Problem:** The historical set-configuration flow can render raw current values, accepts a full key without a distinct reflected namespace control, generates attempt identity inside the gateway, loses pending ownership on remount, and confirms from a matching value without causal projection advancement. These gaps can leak sensitive data, double-dispatch ambiguous attempts, or label pre-existing state as a new success.

**Approach:** Preserve Story 3.3 availability and the fixed command/read endpoints, but bind a redacted ten-fact preview to fresh BFF authority and an ordered projection baseline. Dispatch once with a retained message id, reconcile through aggregate-aware status plus exact redacted proof, and retain only support-safe attempt evidence across rerender/remount.

## Boundaries & Constraints

**Always:** Preserve literal case-sensitive tenant ids, namespace prefixes, key suffixes, and values; validate the composed full key at 256 characters and value at 1024; require every set mutation to preview; keep last-confirmed configuration visible; authorize before key lookup; lock one command per tenant; keep accepted, projected, already-applied, and audit states distinct; use Fluent UI V5, EN/FR whole strings, stable selectors, and support-safe diagnostics.

**Block If:** A new endpoint, domain-contract or aggregate change, submodule edit, raw-value persistence outside the active form/dispatch boundary, or product decision about a preview bypass becomes necessary.

**Never:** Write or revert `sprint-status.yaml`; expose raw values in preview, lifecycle, recovery, audit handoff, announcements, logs, snapshots, selectors, or diagnostics; infer namespace grants from client claims or visible rows; optimistically mutate configuration; confirm from acceptance, SignalR, an unchanged/pre-existing projection, or unrelated projection movement; broaden into Story 3.6 removal.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible change | Reflected prefix `billing`, suffix `mode`, non-identical value, current baseline | Show ten redacted facts; dispatch `billing.mode` once with retained id; confirm only after exact match and ordered version advancement | Pending evidence stays pending, then becomes unable to verify at the bounded terminal path |
| Already applied | Fresh authoritative baseline proves the same composed key/value | Report `already applied` without dispatch and without success/audit-proof language | If exact comparison is unavailable, fail closed instead of assuming NoOp |
| Interrupted attempt | Timeout, ambiguous transport result, refresh, rerender, or remount | Adopt the same tenant/key fingerprint/message id and aggregate lease; reconcile without redispatch | Explicit terminal abandon or bounded expiry releases ownership with unable-to-verify copy |
| Unsafe input/evidence | Invalid prefix/key/value, revoked scope, stale preview, disabled tenant, unsafe viewport, or missing support | Preserve last-confirmed rows and block before dispatch with localized reason/recovery | Never disclose hidden key existence, raw value, payload, correlation, or internal metadata |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor` -- current full-key form, locally assembled preview, unretained dispatch, status refresh, and non-causal confirmation; replace these focused seams while preserving safe validation/exit behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css` -- responsive/focus rules; repair isolated-form selectors and remove legacy `--accent-fill-rest` while keeping Refresh/Cancel outside the hidden form.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor` and `Components/Pages/TenantDetailPage.razor` -- availability, launcher ownership, SignalR/route-safe refresh, aggregate lease, and remount adoption boundaries.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition,ITenantQueryGateway,TenantQueryGateway}.cs` and `State/TenantDetail/TenantConfigurationProjectionProof.cs` -- authorize and assemble a redacted preview/baseline; return exact-match proof with projection version, never a raw value oracle.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantCommandGateway,TenantCommandGateway,UnavailableTenantCommandGateway}.cs` -- add explicit tracked set dispatch and verify message plus aggregate identity during status reconciliation.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`, new `TenantSetConfigurationCommandSnapshot.cs`, and new `TenantSetConfigurationAttemptTracker.cs` -- extract the set reducer, retain a value fingerprint/message/baseline/deadline, merge evidence monotonically, and reuse ordered version comparison.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` and `Resources/TenantsResources*.resx` -- circuit-local tracker registration and support-safe EN/FR preview/lifecycle/recovery copy.
- `src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`, and validator -- read-only evidence: existing command, NoOp, limits, authorization, disabled-tenant rejection, and event behavior are sufficient.

## Tasks & Acceptance

**Execution:**
- [x] `Services/Gateways/*BffComposition*.cs`, `State/TenantDetail/TenantConfigurationProjectionProof.cs`, and query gateway files -- produce one authorization-scoped redacted preview/proof carrying tenant, scope, key reference, current-state classification, freshness/lifecycle, and ordered version.
- [x] `State/TenantCommands/TenantSetConfiguration{CommandSnapshot,AttemptTracker}.cs` and command gateway files -- retain one safe logical attempt, use caller-supplied message identity, validate aggregate-aware status, and require causal projection proof or exact zero-event NoOp proof.
- [x] `Components/Tenants/Configuration/{SetTenantConfigurationFlow,TenantConfigurationManagement}.razor*` and `Components/Pages/TenantDetailPage.razor` -- implement Fluent prefix/key/value input, immutable complete preview, guarded preflight/dispatch/reconciliation, remount/SignalR behavior, focus, responsive refusal, and lease release.
- [x] `Extensions/TenantsUiServiceCollectionExtensions.cs` and `Resources/TenantsResources*.resx` -- register state and localize every visible/support-safe outcome with parity.
- [x] `tests/Hexalith.Tenants.UI.Tests/{Components,Services/Gateways,State,Localization}/**` and `TenantConfigurationEndToEndTests.cs` -- cover every matrix row, causal/version failures, races/remounts, aggregate exclusivity, sensitive-value absence, ten selectors, EN/FR, focus, forced colors, and 767/768 responsive behavior.

**Acceptance Criteria:**
- Given eligible reflected scope and fresh preview evidence, when a non-identical value is confirmed, then exactly one fixed-route command is dispatched and success appears only from the exact postcondition plus qualifying post-baseline provenance.
- Given identical, stale, unauthorized, disabled, incomplete, narrow, interrupted, or unconfirmable conditions, when the flow evaluates or reconciles, then it preserves last-confirmed truth, exposes a precise safe recovery, and never leaks or falsely confirms.
- Given keyboard, screen-reader, forced-colors, English/French, SignalR, refresh, route, and remount use, when the flow changes state, then focus, live-region intent, selectors, retained ownership, and accepted/projected/audited distinctions remain truthful.

## Spec Change Log

## Review Triage Log

## Design Notes

Store only an ordinal value fingerprint after dispatch. The authorized query boundary compares the current raw value to that fingerprint and returns `match + projection version`; neither the proof object nor retained snapshot contains the value. Compose `prefix + "." + non-empty suffix` once without trimming or case conversion.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: solution builds cleanly.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-5-set-namespaced-configuration-with-complete-preview.md` -- expected: no undeclared gitlink movement.
