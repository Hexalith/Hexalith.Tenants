---
title: 'Set Namespaced Configuration with Complete Preview'
type: 'feature'
created: '2026-08-26'
status: 'done'
baseline_revision: 'd5ce92881019d3deca20b5fe03b84f86489dd062'
baseline_commit: 'd5ce92881019d3deca20b5fe03b84f86489dd062'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
warnings:
  - oversized
deferred:
  - summary: >-
      The pre-existing global-administrator superseded-snapshot test depends on test execution order.
    evidence: |-
      The baseline-to-worktree range does not modify GlobalAdministratorsPage or GlobalAdministratorsPageTests. Running the single test with `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -method "Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests.Grant_requery_does_not_confirm_from_a_superseded_snapshot"` fails because the superseded snapshot confirms, while the prescribed serial full-suite command passes all 2,446 tests. This demonstrates pre-existing shared-state or ordering dependence outside Story 3.5.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2682
    severity: medium
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

## File List

- `_bmad-output/implementation-artifacts/spec-3-5-set-namespaced-configuration-with-complete-preview.md`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor`
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleProjectionVersion.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationAttemptTracker.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationCommandSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationCurrentState.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationIntent.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationPreview.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantSetConfigurationValueFingerprint.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationProjectionProof.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantSetConfigurationAttemptTrackerTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantSetConfigurationCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantConfigurationEndToEndTests.cs`

The reviewed baseline-to-head range also contains previously committed umbrella dependency-alignment moves for `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer`; this review preserved those root pointers and did not modify submodule content.

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

- 2026-08-27: Hardened the retained set-command lifecycle after formal review: ambiguous dispatches now reconcile by stable identity, stale or canceled preview evidence fails closed, dispatch ownership survives expiry/abandon races, status/projection evidence merges monotonically, fingerprints are process-keyed and strict, and outer page/authorization/race coverage proves the complete path. Declared the two ambient root gitlink movements included in the reviewed baseline range.

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 17: (high 13, medium 4, low 0)
- defer: 1: (high 0, medium 1, low 0)
- reject: 5: (high 1, medium 3, low 1)
- addressed_findings:
  - `[high]` `[patch]` Prevented expiry or abandonment from releasing aggregate ownership while a tracked dispatch is still running, then evaluated expiry only after transport settles.
  - `[high]` `[patch]` Made ambiguous submission snapshots retain a usable correlation identity and proved remount reconciliation reaches confirmation without redispatch.
  - `[high]` `[patch]` Bound preview completeness to the current form intent so a retained terminal preview cannot enable submission after the raw value is cleared or changed.
  - `[high]` `[patch]` Rechecked current availability after submit-time preview awaits so freshness, viewport, or policy changes block before lease acquisition and dispatch.
  - `[high]` `[patch]` Kept the retained set flow mounted across policy loss while hiding no-longer-authorized rows and remove actions.
  - `[high]` `[patch]` Distinguished stored/published event evidence from completed NoOp evidence and rejected completed statuses with absent or negative event counts.
  - `[high]` `[patch]` Made snapshot and tracker status merging lifecycle-monotonic so stale processing observations cannot regress projection or publish-failure evidence.
  - `[high]` `[patch]` Replaced reversible low-entropy SHA-256 value fingerprints with process-keyed HMAC-SHA-256 and strict UTF-8 encoding.
  - `[high]` `[patch]` Replaced same-tick deterministic message identities with validated random ULIDs while preserving per-attempt reuse.
  - `[high]` `[patch]` Converted provider cancellation into support-safe unavailable or ambiguous evidence instead of allowing an uncaught cancellation to tear down the circuit.
  - `[high]` `[patch]` Added tenant-detail page coverage for preview, one tracked dispatch, aggregate-aware status, advanced exact proof, and confirmed rendering.
  - `[high]` `[patch]` Added non-owner and absent-member preview tests that prove authorization resolves before any configuration dictionary lookup.
  - `[high]` `[patch]` Added held-preview race coverage for freshness and tenant-route changes, proving the captured prior intent cannot dispatch.
  - `[medium]` `[patch]` Replaced prohibited substring-based `ToString` safety assertions with one exact support-safe diagnostic contract so the evidence gate and suite pass.
  - `[medium]` `[patch]` Restored executable key-shape coverage for edge whitespace, format characters, non-ASCII separators, astral format characters, valid interior ASCII spaces, and invalid UTF-16 values.
  - `[medium]` `[patch]` Corrected global-administrator initial focus to the required namespace field and added an exact focus-target assertion.
  - `[medium]` `[patch]` Declared and documented the reviewed range's existing EventStore and FrontComposer root gitlink movements so story provenance validation is complete.

## Design Notes

Store only an ordinal value fingerprint after dispatch. The authorized query boundary compares the current raw value to that fingerprint and returns `match + projection version`; neither the proof object nor retained snapshot contains the value. Compose `prefix + "." + non-empty suffix` once without trimming or case conversion.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: solution builds cleanly.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-5-set-namespaced-configuration-with-complete-preview.md` -- expected: no undeclared gitlink movement.

## Auto Run Result

Status: done

### Summary

Completed the formal review and hardened the complete namespaced set-configuration path. The flow now preserves one safe logical attempt through dispatch, remount, policy/freshness changes, ambiguous outcomes, status reconciliation, and exact causal projection proof without retaining or rendering raw values.

### Files Changed

- The exact reviewed baseline range is declared under `## File List`, including the story specification and the two preserved ambient root gitlinks.
- `SetTenantConfigurationFlow.razor`, `TenantConfigurationManagement.razor`, and the tenant-detail page wiring implement retained ownership, fail-closed preview/race handling, truthful recovery, and complete outer-page reconciliation.
- The set-command snapshot, tracker, preview/intent/proof, and fingerprint state files implement stable random identity, monotonic evidence, strict process-keyed fingerprints, and causal confirmation.
- The command/query/BFF gateway files and UI registration compose authorization-first preview/proof reads and caller-owned tracked dispatch over the existing fixed routes.
- English/French resources and Fluent scoped styling provide support-safe whole-string validation, state, focus, responsive, and forced-colors behavior.
- Gateway, reducer, component, page, localization, and end-to-end-style tests cover the accepted, already-applied, unauthorized, stale, interrupted, remounted, route-changed, and unconfirmable matrix.

### Review Findings

- Patches applied: 17 (high 13, medium 4, low 0).
- Items deferred: 1 pre-existing medium-severity test-order dependency, recorded in frontmatter.
- Items rejected: 5 (high 1, medium 3, low 1), covering broader hosted/gateway interpretations or recommendations contradicted by repository policy and the implemented UI-slice contract.
- Follow-up review recommendation: `true`; high-severity patches independently require follow-up. Weighted medium/low score: `3 × 4 + 1 × 0 = 12`.

### Verification

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` — passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` — passed 2,446 tests with 0 failures and 0 skips.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` — passed with 0 warnings and 0 errors.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-5-set-namespaced-configuration-with-complete-preview.md` — passed; both baseline-range root gitlink movements are declared.
- `git diff --check` — passed with no whitespace errors.

### Residual Risks

- The prescribed repository verification is green. A live Aspire browser/assistive-technology pass was not available because the detached AppHost exited after a clean build and initial resource startup; page-level bUnit coverage verifies the production composition boundary instead.
- The unrelated global-administrator isolated-test ordering dependency is preserved as a structured deferred item and does not affect the passing serial repository gate.
