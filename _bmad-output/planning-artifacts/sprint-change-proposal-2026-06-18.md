# Sprint Change Proposal - FrontComposer and Fluent UI V5 Control Conformance Follow-up

Date: 2026-06-18
Workflow: bmad-correct-course
Mode: Batch, inferred from the explicit correction request
Status: Approved and implemented
Owner: Administrator

## 1. Issue Summary

The June 17 FrontComposer and Fluent UI v5 page conformance sweep closed the main raw table and accordion gaps, but the correction request identifies a remaining class of non-conformant UI controls: HTML form/control structure and CSS-owned control or state visuals that should be expressed through FrontComposer or Blazor Fluent UI v5 primitives.

Current evidence:

- No raw `<button>`, `<input>`, `<select>`, `<textarea>`, `<table>`, `<thead>`, `<tbody>`, `<tr>`, `<td>`, or `<th>` tags remain under `src/Hexalith.Tenants.UI/Components`.
- Ten raw `<form>` elements remain in command, lookup, and administration flows.
- Component CSS still owns some status/control presentation with hard-coded semantic colors or token fallbacks, notably in `TruthStateBadge.razor.css`, `MyTenantsDataGrid.razor.css`, `TenantDetailPage.razor.css`, `AuditEvidenceEntryPoint.razor.css`, and `TenantAuditPage.razor.css`.
- `DomainUiFluentConformanceTests` blocks raw interactive controls and raw tables, but does not yet guard raw form markup or CSS-owned semantic status/control visuals.
- The Fluent UI Blazor MCP documentation currently describes component package `5.0.0.26139`, while the repository is pinned to `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; implementation must verify exact APIs against the pinned package/source before coding.

This is a conformance correction, not a product scope change.

## 2. Change Analysis Checklist

| Area | Status | Notes |
| --- | --- | --- |
| Triggering issue | Done | User requested replacement of remaining HTML/CSS controls with FrontComposer/Blazor Fluent UI v5 controls. |
| Evidence gathered | Done | Source scan confirms no raw button/input/select/textarea/table tags, but raw form tags and CSS-owned state visuals remain. |
| Epic impact | Done | No epic objectives change. A cross-cutting UI governance follow-up is needed across completed UI stories. |
| Story impact | Done | Existing completed stories stay complete; add one cross-cutting follow-up story/spec for conformance cleanup and tests. |
| Architecture impact | Done | Existing architecture already mandates FrontComposer/Fluent v5. Add implementation-level clarification only. |
| PRD impact | Done | No PRD behavior or feature changes required. |
| UX impact | Done | Existing UX guidance already says Fluent UI is the visual authority and semantic state should use Fluent roles, not bespoke colors. |
| Test impact | Done | Governance tests need to cover raw form markup and semantic visual CSS. |
| Risk assessment | Done | Moderate implementation size, low-to-medium product risk, because changes are mostly presentation and governance across many surfaces. |

## 3. Impact Analysis

Epic impact:

- Epics 1, 2, 4, and 5 remain done.
- Epic 3 remains in progress by sprint status, with its current stories done.
- Add one cross-cutting conformance story rather than reopening each completed story.

Artifact impact:

- `sprint-status.yaml`: add a proposed cross-cutting backlog/ready story after approval.
- Implementation artifacts: add `spec-frontcomposer-fluent-control-and-css-conformance-sweep.md` after approval.
- Architecture/UX docs: no required rewrite before implementation; optional note can clarify that raw native forms and CSS-owned semantic control visuals are not acceptable unless explicitly documented as exceptions.
- Tests: extend `DomainUiFluentConformanceTests`.

Technical impact:

- Replace raw `<form>` wrappers in UI flows with Blazor `EditForm` and Fluent field/input/button/message components where validation or submit semantics are needed.
- Where a native semantic element is still required and no Fluent/FrontComposer equivalent exists, document the exception in the implementation artifact and keep the native element non-visual.
- Replace status/control CSS color ownership with Fluent components and parameters, especially `FluentBadge` with `BadgeColor`, `FluentMessageBar`, `FluentButton`, `FluentAnchorButton`, `FluentField`, and existing FrontComposer primitives.
- Keep layout-only CSS when required, but avoid hard-coded state colors, borders, radii, and control styling that Fluent components should own.

## 4. Recommended Approach

Recommended path: Direct Adjustment.

Reason:

- The product direction, UX direction, and architecture already require FrontComposer/Fluent UI v5.
- The current issue is a conformance gap in implementation and governance tests.
- A full PRD or architecture replan would add process without changing the intended outcome.

Rejected alternatives:

- Roll back the June 17 conformance work: not appropriate, because that work moved the UI in the correct direction and is already implemented.
- Rework all UI markup broadly: too risky and unnecessary; semantic layout markup, raw navigation anchors where allowed by FrontComposer guidance, and non-control HTML should remain in scope only when it violates the control or visual authority rule.
- Add generic shared UI infrastructure inside Tenants: not allowed by repository boundaries. Missing generic capability belongs in `Hexalith.FrontComposer`.

## 5. Detailed Change Proposals

### 5.1 Sprint Status

Old:

```yaml
cross-cutting:
  cc-2026-06-17-frontcomposer-fluent-v5-page-component-conformance-sweep: done
```

New, after approval:

```yaml
cross-cutting:
  cc-2026-06-17-frontcomposer-fluent-v5-page-component-conformance-sweep: done
  cc-2026-06-18-frontcomposer-fluent-control-and-css-conformance-sweep: ready-for-dev
```

### 5.2 New Cross-Cutting Story/Spec

Create:

`_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-control-and-css-conformance-sweep.md`

Proposed acceptance criteria:

1. No raw visual or interactive HTML controls remain in `src/Hexalith.Tenants.UI/Components`; this includes raw `<form>` unless the implementation artifact documents a specific non-visual semantic exception.
2. Command and lookup flows use Blazor `EditForm` plus Fluent/FrontComposer fields, inputs, buttons, validation, and message surfaces where submit semantics are required.
3. Status, role, truth-state, and audit-state visuals use Fluent semantic component parameters such as `BadgeColor`, not hard-coded CSS colors.
4. CSS files retain only layout or spacing rules that do not duplicate Fluent/FrontComposer component responsibilities.
5. Hard-coded hex colors are removed from semantic status/control CSS, or justified as verified Fluent token fallbacks only where no component parameter exists.
6. The implementation does not add generic UI infrastructure to Tenants. Missing shared primitives are requested or implemented in `Hexalith.FrontComposer`.
7. `DomainUiFluentConformanceTests` blocks raw forms or enforces a documented exception list.
8. `DomainUiFluentConformanceTests` adds governance for semantic control/status CSS ownership.
9. Component tests verify key state components render Fluent components with semantic role parameters.
10. UI tests pass with the repository's pinned Fluent UI Blazor package.

### 5.3 Governance Tests

Old:

- Guard raw button/input/select/textarea markup.
- Guard raw table markup.
- Guard expected accordion usage on known multi-region pages.

New:

- Keep all existing guards.
- Add a raw form guard for `.razor` files under `src/Hexalith.Tenants.UI/Components`.
- Add a narrow allowlist mechanism only for documented non-visual semantic exceptions.
- Add a semantic CSS guard for status/control surfaces, starting with `TruthStateBadge`, tenant status/role badge styling, audit entry point state styling, and tenant audit state styling.
- Prefer component-render assertions where possible, so the tests prove use of `FluentBadge`, `FluentMessageBar`, `FluentField`, or other relevant Fluent primitives rather than only scanning text.

### 5.4 Architecture and UX Clarification

Old:

- UI components should use FrontComposer or Fluent UI v5.
- No bespoke palette; Fluent UI is the visual authority.

New implementation note:

- Hand-authored Tenants UI controls and state visuals must be expressed through FrontComposer or Blazor Fluent UI v5 primitives. Native HTML may remain only for non-visual semantic structure or explicitly documented exceptions. CSS may support layout, but must not own semantic state colors, control chrome, or interaction styling where a Fluent/FrontComposer component parameter exists.

## 6. Target Inventory

| Current surface | Current gap | Target |
| --- | --- | --- |
| `GlobalAdministratorsPage.razor` | Raw `<form>` around grant command | `EditForm` or dedicated flow component using Fluent field/input/button/message controls |
| `UserMembershipLookupPage.razor` | Raw `<form>` around lookup | `EditForm` or Fluent input/button interaction without raw form styling |
| `CreateTenantFlow.razor` | Raw `<form>` | `EditForm` with Fluent field/input/button/validation surface |
| `AddTenantMemberFlow.razor` | Raw `<form>` | `EditForm` with Fluent field/input/select/button/validation surface |
| `ChangeTenantMemberRoleFlow.razor` | Raw `<form>` | `EditForm` with Fluent select/button/validation surface |
| `RemoveTenantMemberFlow.razor` | Raw `<form>` | `EditForm` with Fluent confirmation/message/destructive controls |
| `SetTenantConfigurationFlow.razor` | Raw `<form>` | `EditForm` with Fluent field/input/button/message controls |
| `RemoveTenantConfigurationFlow.razor` | Raw `<form>` | `EditForm` with Fluent confirmation/message/destructive controls |
| `TenantLifecycleCommandFlow.razor` | Raw `<form>` | `EditForm` with Fluent confirmation/message/destructive controls |
| `EditTenantMetadataFlow.razor` | Raw `<form>` | `EditForm` with Fluent field/input/textarea/button/validation surface |
| `TruthStateBadge` and tenant status/role styling | CSS-owned semantic colors | `FluentBadge` semantic `BadgeColor` mapping |
| Audit entry point and audit page state styling | CSS-owned state/focus color treatment | Fluent button/anchor/message/badge primitives or documented layout-only CSS |

## 7. Implementation Handoff

Scope: Moderate

Suggested execution sequence:

1. Create the implementation artifact/story with the acceptance criteria above.
2. Verify exact Fluent component APIs against the pinned package `5.0.0-rc.3-26138.1` before editing Razor components.
3. Replace raw form wrappers in command and lookup flows.
4. Move status/control visual semantics from CSS into Fluent component parameters.
5. Reduce component CSS to layout-only exceptions and document any necessary exceptions.
6. Extend `DomainUiFluentConformanceTests` and focused component tests.
7. Run `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`.
8. Run `git diff --check`.

Required assignees:

- Developer agent for implementation.
- PO/sprint owner for sprint status update after approval.
- Architect/UX only if implementation discovers a missing shared primitive that must be added to `Hexalith.FrontComposer`.

## 8. Approval Request

Approved by Administrator on 2026-06-18. Implemented as `cc-2026-06-18-frontcomposer-fluent-control-and-css-conformance-sweep`.
