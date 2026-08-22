---
title: 'Lifecycle and Configuration Availability Guardrail'
type: 'feature'
created: '2026-08-22'
status: 'done'
baseline_commit: 'b2b80941df874c2ee6772ca316841c480e0e493b'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Four-action eligibility is fragmented across lifecycle state, configuration Razor, and submit flows. Gates are conflated, configuration lacks per-action results, and viewport/admission wiring can falsely expose high-impact actions.

**Approach:** Add a pure four-action kernel, compose safe evidence through the BFF/detail page, and render stable results before existing previews. The guardrail evaluates only; existing flows own commands.

## Boundaries & Constraints

**Always:** Evaluate each action independently; preserve literal identifiers and last-confirmed data; require server-reflected lifecycle authority and TenantOwner/global-administrator authority plus namespace scope for configuration; distinguish preview entry from confirmation readiness; use the six canonical reasons with deterministic precedence. Keep same-state, disabled-tenant, and missing-key outcomes separate from infrastructure failures. Unknown viewport/evidence fails closed; `refreshing` needs a current baseline, while `aging` may proceed with visible friction when other evidence qualifies.

**Ask First:** Changing FrontComposer or another submodule, adding an endpoint/command contract, or making audit proof mandatory where the consuming action declares it not required.

**Never:** Submit a command, acquire an aggregate lease, create attempt lifecycle state, infer authority from client claims/visibility, expose hidden namespaces or raw values, optimistically mutate projection truth, or absorb create/metadata/membership command work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible action | Current qualifying evidence, safe viewport, complete preview facts, free aggregate | Eligible result opens only its dedicated preview flow | No command attempt is created |
| Evidence failure | Indeterminate authority, stale/degraded data, missing support/preview/proof, busy aggregate, or unsafe viewport | Only affected action is blocked with canonical reason and recovery | Preserve last-confirmed data; disclose no internals |
| Domain-state block | Same-state lifecycle, disabled configuration, or proven missing remove key | Block with safe expected outcome | Never present success/NoOp or reveal unauthorized key existence |
| Set/remove context | Incomplete key/value or out-of-scope key | Confirmation remains blocked while safe input stays editable | Permission/readiness reason; no dispatch |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs` -- replace the lifecycle-only multi-type reducer with the shared kernel; preserve fail-closed ordering and same-state outcome behavior.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpact*.cs` -- one type per file for action/stage, six reasons, evidence, result, domain outcome, and pure evaluator.
- `src/Hexalith.Tenants.UI/Services/Configuration/{TenantConfigurationPrincipalEvidence,TenantConfigurationReadPolicyResolution,TenantConfigurationSafeComposer}.cs` and `State/TenantDetail/TenantConfigurationManagementContext.cs` -- carry TenantOwner/global-admin evidence separately from ordinal prefix scope.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs` and `TenantsBffComposition.cs` -- compose typed safe authorization/preview readiness from current circuit principal and sanitized authoritative detail; no new route.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- supply distinct support, admission, preview/proof, baseline, and observed FrontComposer viewport inputs; stop hard-coding readiness.
- `src/Hexalith.Tenants.UI/Components/Tenants/{Lifecycle,Configuration}` -- render per-action results and retain existing preview/submit owners.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources*.resx` -- whole-string resources with named-placeholder parity.
- `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services}` -- gate matrix, authority/scope, viewport, accessibility, localization, support-safety, and zero-dispatch coverage.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpact*.cs` and `TenantLifecycleAvailability.cs` -- implement immutable staged evaluation; split public types by file.
- [x] `src/Hexalith.Tenants.UI/Services/Configuration/*` and `src/Hexalith.Tenants.UI/Services/Gateways/*BffComposition.cs` -- compose role, scope, preview, and support evidence.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- wire capabilities, admission, baseline, and observed viewport without acquiring a lease.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/{Lifecycle,Configuration}` and `src/Hexalith.Tenants.UI/Resources/TenantsResources*.resx` -- render accessible results and gate existing previews.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services}` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- cover the matrix and track implementation/review.

**Acceptance Criteria:**
- Given any one evidence input is unknown or fails, when four actions are evaluated, then only affected actions fail closed with the evidence-honest canonical reason and recovery.
- Given same-state lifecycle, disabled configuration, or an authorized proven-missing key, when availability renders, then safe domain context is visible without success, disclosure, or submission.
- Given all entry/confirmation inputs qualify, when an action is used, then only its existing complete-preview flow opens and the guardrail creates no attempt state or gateway call.
- Given mobile/indeterminate viewport and EN/FR assistive use, when slots render, then mutation stays read-only and identity, status, freshness, action, reason, and recovery remain associated through stable selectors and whole strings.

## Spec Change Log

## Design Notes

Domain outcomes are orthogonal to blocker precedence. Model preview/proof as `NotRequired`, `Ready`, or `Missing`. Observe FrontComposer viewport changes from an initially unknown state; its Desktop default is not measurement evidence.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class '*TenantHighImpactActionAvailabilityTests'` plus focused BFF/component classes -- expected: all matrix and zero-dispatch tests pass; use the xUnit v3 executable fallback if project-level filtering is unavailable.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-3-lifecycle-and-configuration-availability-guardrail.md` -- expected: no undeclared gitlink movement.

## Suggested Review Order

**Availability kernel**

- Start with deterministic staged evaluation, canonical precedence, and exhaustive fail-closed validation.
  [`TenantHighImpactActionAvailabilityEvaluator.cs:17`](../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs#L17)

- See how typed lifecycle evidence preserves compatibility without weakening the kernel.
  [`TenantLifecycleAvailability.cs:50`](../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs#L50)

- Confirm only measured, known non-phone tiers become safe evidence.
  [`TenantHighImpactViewportObservation.cs:28`](../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactViewportObservation.cs#L28)

**Evidence composition**

- Review the BFF boundary separating lifecycle authority, configuration role, scope, and preview.
  [`ITenantsBffComposition.cs:103`](../../src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs#L103)

- Follow page-level composition of freshness, admission, support, viewport, and target evidence.
  [`TenantDetailPage.razor:361`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L361)

- Verify sanitized membership reauthorization tolerates malformed null members and fails closed.
  [`TenantConfigurationSafeComposer.cs:241`](../../src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs#L241)

**Lifecycle UI and confirmation**

- Check exact tenant/action correlation and evidence-derived lifecycle rendering.
  [`TenantLifecycleActionAvailability.razor:299`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor#L299)

- Inspect submit-time authority re-resolution before activity, lease, attempt, or dispatch.
  [`TenantLifecycleCommandFlow.razor:413`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L413)

**Configuration UI and outcomes**

- Review null-safe evidence resolution plus stable reason and recovery associations.
  [`TenantConfigurationManagement.razor:260`](../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor#L260)

- Confirm reauthorization correlation and AlreadyApplied precedence for set commands.
  [`SetTenantConfigurationFlow.razor:310`](../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor#L310)

- Confirm authorized disappearance becomes KeyNotFound without leaking unproven existence.
  [`RemoveTenantConfigurationFlow.razor:302`](../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor#L302)

- See the safe no-dispatch snapshot retained for proven missing targets.
  [`TenantCreateCommandModels.cs:1680`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1680)

**Verification and localized surfaces**

- Inspect undefined-domain, preview, proof, aging, and refreshing kernel coverage.
  [`TenantHighImpactActionAvailabilityTests.cs:256`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantHighImpactActionAvailabilityTests.cs#L256)

- Follow real Fluxor store dispatch through scoped observation and page eligibility.
  [`TenantDetailSurfaceTests.cs:129`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L129)

- Review complete lifecycle preview, reauthorization, correlation, and EN/FR rendering tests.
  [`TenantLifecycleActionAvailabilityTests.cs:549`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs#L549)

- Check evidence-aware set success and AlreadyApplied zero-dispatch behavior.
  [`SetTenantConfigurationFlowTests.cs:127`](../../tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs#L127)

- Check evidence-aware remove success and authorized-missing zero-dispatch behavior.
  [`RemoveTenantConfigurationFlowTests.cs:145`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs#L145)

- Verify matching TenantOwner reauthorization and runtime-null member handling.
  [`TenantsBffCompositionTests.cs:180`](../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs#L180)

- Compare whole-string refreshing facts in both shipped cultures.
  [`TenantsResources.resx:3885`](../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L3885)
