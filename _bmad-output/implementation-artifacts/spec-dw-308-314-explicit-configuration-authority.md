---
title: 'Require explicit configuration mutation authority'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'e1d7ecbfaa87dabdf8a25c54fe05fb0f90ae6250'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-308 and its duplicate DW-314 leave `TenantConfigurationManagementContext.Available` able to omit authority and silently grant `TenantOwner`, or `GlobalAdministrator` when that flag is set. The private resolvers also accept tenant lifecycle status and discard it, so status looks like it could change mutation role. Production already passes an explicit resolved state; the public trap and the unused parameter remain.

**Approach:** Require an explicit authority state and stop deriving a role from a missing argument. Preserve each existing test's meaning by passing the authority the old default would have chosen. Drop the unused status parameter from the private resolvers while the context still stores lifecycle status. Prove that status does not change resolved authority and that production composition still supplies an explicit state. Reconcile only these two ledger records.

</frozen-after-approval>

## Implementation Notes

- Route facts: no intent gap the operator would notice; production resolution already passes explicit authority and must stay that way. Nothing irreversible. Footprint is the internal factory, two private resolvers, the existing call sites that omit the argument, one status-independence regression, this spec, validation evidence, and the two backlog documents. No new public API or dependency.
- Entry revision: `e1d7ecbfaa87dabdf8a25c54fe05fb0f90ae6250`; branch `main`, clean tree. This is the next Tenants-owned verified defect with no blocking dependency after DW-648 closed without a successor. Selecting that one duplicate pair keeps this run to a single goal.
- Code map: `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs`, `Available` (lines 85–109). The null default at lines 105–107 grants `GlobalAdministrator` or `TenantOwner`. The private constructor already requires a non-null `TenantConfigurationAuthorityState`. `HasMutationAuthority` is true only for `TenantOwner` and `GlobalAdministrator`. `Unavailable` already stores `Indeterminate` and stays unchanged.
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs`: `Compose` and both `Reauthorize` overloads already pass `ResolveAuthority(...)`. The two private `ResolveAuthority` overloads (lines 221–239 and 241–270) discard `tenantStatus`. Status still belongs on the context. Lifecycle status must not revoke mutation role; a disabled tenant stays blocked by the existing domain outcome.
- Authority type: `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationAuthorityState.cs` (`Indeterminate`, `MissingPermission`, `TenantOwner`, `GlobalAdministrator`). `Available` is `internal`, visible to the UI test assemblies. Do not add `[Obsolete]`. Do not change `HasMutationAuthority`.
- Test map: every current `Available` call uses five arguments. `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` (26), `TenantHighImpactAvailabilityComponentTests.cs` (5), `SetTenantConfigurationFlowTests.cs` helper `Context` (3), `RemoveTenantConfigurationFlowTests.cs` helper `Context` (2), `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` (4). Pass `GlobalAdministrator` when the existing global flag is true, otherwise `TenantOwner`. Add the status-independence case beside `TenantConfigurationReadPolicyTests.Reauthorizing_against_an_unavailable_policy_drops_previously_safe_rows`. Reuse `TenantsBffCompositionTests` authority expectations as the production control; do not rewrite them.
- Validation: the signature change must fail compilation of the old five-argument calls before they are updated. Then build the UI test project in Debug with source references and warnings as errors, and run the focused configuration/context classes plus the full UI project. Record exact commands, results, and limits in `dw-308-314-validation-evidence.md`. Close only DW-308 and DW-314 after that full project passes and independent review findings are resolved. Preserve historical findings, unrelated records, archive, sprint status, dependencies, and release holds. No version-control mutation is required by the repository instructions unless a later workflow step explicitly commits.
- Implemented the required authority argument and removed `tenantStatus` from both private resolvers. The pre-update build exited 1 with CS7036 on all 40 old calls. Existing calls now pass the authority the old default computed. `Lifecycle_status_does_not_change_resolved_configuration_authority` covers owner, contributor, global, and both reauthorization overloads for Unknown, Active, and Disabled.
- Final Debug source build has zero warnings/errors. Policy tests pass 80/80 and the full UI project passes 6263/6263, zero failures/skips. Only DW-308 and DW-314 close: 649 IDs, 357 done/292 open, 264 index rows. DW-649 records the pre-existing detail-page stub that still grants TenantOwner. Evidence is `dw-308-314-validation-evidence.md`.

## Review Triage Log

- medium, patch: the new status test never called `Reauthorize(TenantDetail, ...)`, so a status check on that overload alone would pass. The test now reauthorizes the owner detail at each status and requires TenantOwner plus the supplied status.
- medium, patch: safe-model reauthorization was only an owner policy expecting MissingPermission, so a global policy could start dropping GlobalAdministrator for Disabled or Unknown. The loop now reauthorizes the safe model with the global policy and requires GlobalAdministrator.
- medium, patch: every composed detail was a TenantOwner member, so a status-based grant for a non-owner was untested. The loop now composes a TenantContributor and requires MissingPermission at each status.
- low, rejected: the test does not assert `IsGlobalAdministrator`, prefixes, or removable rows, and it does not exercise the separate TenantDisabled command outcome. Those fields and that outcome are outside the authority decision this change makes; adding them would widen the fact without guarding the signature.
- false: `Available` should reject a global flag that disagrees with the authority enum, and the test helpers should accept MissingPermission. The flag is namespace-wildcard scope and the enum is mutation role; production supplies both independently. The helpers preserve the old default, which the spec requires.
- false: the detail-page stub grants TenantOwner where production safe-model reauthorization returns MissingPermission, so this change made the page test wrong. The stub already received that grant by omitting the argument. This change preserved it. The mismatch is pre-existing and is DW-649.
- false: `tenantId` is unused after the status parameter was removed. Both resolvers still reject a blank `tenantId` before reading policy or members.
- low, patch: the lifecycle comment remained on the membership overload after that overload stopped receiving status, and the safe-model overload did not say status was intentionally absent. Both comments now say status is not an input.
- false: evidence and ledger reconciliation were missing at review time. They follow accepted review patches and the final 6263/6263 run, and are recorded in this spec and `dw-308-314-validation-evidence.md`.
