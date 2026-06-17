# Sprint Change Proposal - Global Administrators Navigation and Accordion Conformance Follow-up

Date: 2026-06-17
Workflow: bmad-correct-course
Mode: Batch, from explicit user correction request
Status: Approved and implemented
Owner: Administrator

## 1. Issue Summary

The implemented Tenants UI still had two correction gaps after the prior FrontComposer and Fluent UI v5 conformance sweep:

- The `Global Administrators` left-menu entry was registered, but the authorization policy only accepted the `GlobalAdministrator` role claim plus `eventstore:tenant=system`. The local Keycloak realm emits the platform administrator state through `global_admin=true`, so the FrontComposer shell `AuthorizeView` could hide the registered menu item for valid platform operators.
- `UserMembershipLookupPage.razor` and `TenantAuditPage.razor` still had sibling page regions outside `FluentAccordion`, while the FrontComposer guidance requires multi-region pages/panels to group sibling titled regions with Fluent accordions. The audit grid remains directly visible as a grid-first primary surface.

## 2. Change Analysis Checklist

| Area | Status | Notes |
| --- | --- | --- |
| Trigger understood | Complete | User reported missing Global Administrators navigation and incorrect non-accordion pages. |
| Epic impact | Complete | Affects completed Epic 4 navigation/readiness and the cross-cutting UI conformance hardening item. |
| Artifact conflicts | Complete | PRD, UX, and architecture already require Global Administrators visibility for platform operators and Fluent/FrontComposer page composition. |
| Path forward | Complete | Direct adjustment; no product scope or backend contract change required. |
| Approval | Complete | Approved by Administrator's correction request and implemented on 2026-06-17. |

## 3. Impact Analysis

### Epic Impact

No epic scope needs to be reopened. The correction is limited to the Tenants UI shell authorization shape and page composition:

- Epic 4 global administrator management: navigation policy now matches the claim shape emitted by the configured realm and already reflected by the BFF composition.
- UI conformance sweep: user lookup and audit support regions now use `FluentAccordion` while preserving existing test IDs, live regions, forms, gateway calls, and grid visibility.

### Artifact Impact

PRD: No requirement change. Existing requirements already describe platform-only Global Administrators navigation and FrontComposer/Fluent UI usage.

Architecture: No new architecture decision. The implementation now follows the existing claim reflection and accordion guidance.

UX: No redesign. The correction keeps page title/header content outside accordions and keeps the audit grid visible.

Tests: Add focused regression coverage for `global_admin=true` and source-level accordion conformance for known multi-region Tenants pages/components.

### Technical Impact

Risk is low. The authorization logic is centralized in `TenantsGlobalAdministratorClaims` and reused by both the navigation policy and BFF composition. The UI markup changes wrap existing regions without changing gateway calls, model state, localization keys, or `data-testid` selectors.

## 4. Recommended Approach

Use direct adjustment:

1. Centralize the server-side global administrator claim predicate.
2. Reuse that predicate from the FrontComposer navigation policy and BFF authorization reflection.
3. Add `FluentAccordion` grouping to the remaining applicable multi-region pages.
4. Add regression tests for the claim shape and accordion conformance.

## 5. Detailed Change Proposals

### Story 4.1 Follow-up

OLD:

Global Administrators navigation was registered and policy-gated, but the policy accepted only the role claim shape.

NEW:

The policy accepts the same fail-closed global administrator predicate as the BFF reflection:

- authenticated principal
- `eventstore:tenant=system`
- one accepted global administrator marker: `global_admin=true`, `is_global_admin=true`, `GlobalAdministrator`, `global-administrator`, or `global-admin` through supported role/roles claim shapes

### UI Conformance Follow-up

OLD:

`UserMembershipLookupPage.razor` and `TenantAuditPage.razor` rendered multiple sibling regions directly.

NEW:

- User lookup wraps controls, status, and dynamic results/state regions in `FluentAccordion` items expanded by default.
- Tenant audit wraps filters and status in a `FluentAccordion` expanded by default.
- Tenant audit keeps the audit grid directly visible outside the accordion when rows exist, preserving the grid-first exception.

## 6. Implementation Handoff

Implemented files:

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`

Validation:

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`
- Result: 674 passed, 0 failed, 0 skipped
- `git diff --check`
- Result: no whitespace errors

Definition of done:

- Global Administrators navigation is visible to authenticated platform operators with the Keycloak `global_admin=true` claim and hidden without the system tenant scope.
- Applicable multi-region Tenants pages use `FluentAccordion` with expanded default items.
- Grid-first audit results remain directly visible.
- Focused regression and conformance tests pass.
