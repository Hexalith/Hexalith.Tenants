# Tenants UI Accessibility, Localization, and Acceptance Evidence Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact (planning-only)
Last reviewed: 2026-06-02
Story: 9.7 - Define Accessibility, Localization, and UI Acceptance Evidence

This document defines the cross-cutting accessibility, localization, and UI acceptance evidence requirements that future Phase 2 Tenants Admin UI implementation stories must cite before they can be marked `ready` or `ready-with-approved-fallback`. It composes the existing Epic 9 planning artifacts and does not implement screens, components, tests, resources, tokens, routes, endpoints, commands, queries, or generated UI files.

## Scope and Boundary

- **Planning/specification only.** This story produces an accessibility, localization, and UI acceptance evidence specification for Phase 2 UI implementation stories. It does not make any screen, component, dialog, or test implementation-ready, and it does not author any executable test.
- **No implementation artifacts.** This document does not create Tenants Admin UI screens, FrontComposer/Fluent UI components, Blazor pages/routes/layouts, CSS/theme/token files, design tokens, `.resx` localization files, automated accessibility or E2E test projects, backend endpoints, commands, queries, package references, generated UI files, domain-contract annotations, or submodule pointer changes. The responsive/accessibility test matrix is a requirement definition here, not a test artifact.
- **Backend independence.** Missing UI dependencies block or defer future Phase 2 UI rows, never backend package/release work.
- **Reconcile, do not duplicate.** This spec references existing source-of-truth documents instead of renaming, re-enumerating, or contradicting their vocabularies, surfaces, dependency IDs, backlog rows, or readiness values.

### Why a new artifact

No accessibility/localization/acceptance-evidence spec existed in `docs/` before this story. The existing Epic 9 pattern is one focused specification per cross-cutting concern: dependency map, operations shell, truth state, remove-user journey, audit evidence/recovery, and responsive visual system. This document adds the evidence gate Story 9.6 explicitly deferred to Story 9.7, rather than adding a parallel dependency map or redefining existing patterns.

## Existing Source Ownership

- `docs/tenants-ui-truth-state-and-action-availability-spec.md` owns the badge/state/reason/feedback vocabularies, non-color-only state presentation, unavailable-action reason exposure, live-status ties, automation-selector ties, and no runtime sentence-fragment assembly rule.
- `docs/tenants-ui-operations-shell-spec.md` owns the four-area Operations Shell, the tenant list as default triage surface, the member table/read-only contexts, and stable selector/component-contract expectations.
- `docs/tenants-ui-responsive-layout-and-visual-system-spec.md` owns the layout rule breakpoints (mobile 320-767px, tablet 768-1023px, desktop 1024px and above, wide desktop 1440px and above), the no-color-only/forced-colors invariant, desktop-first/tablet-collapse/mobile-read-only rules, and the fail-closed responsive rule.
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` owns the `RemoveUserFromTenant` command-capable journey, including last-owner warning behavior and command preview context.
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md` owns audit unavailable/delayed/missing-support distinctions, audit evidence receipt, flat audit fallback, and compensating recovery language.
- `docs/tenants-ui-frontcomposer-dependency-map.md` owns the 10 fixed dependency IDs: `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`.
- `docs/tenants-ui-phase-2-story-backlog.md` owns `ui-01` through `ui-15`, their literal `blockedBy` arrays, readiness values, deferred decisions, and field encoding conventions.

## 1. WCAG Baseline, Target, and Scope

The Phase 2 Admin UI accessibility baseline is **WCAG 2.1 AA**. **WCAG 2.2 AA is the target where supported by the selected Fluent UI Blazor and FrontComposer stack**. Future stories must preserve that exact conditional target and must not promise unconditional WCAG 2.2 AA conformance.

The in-scope surfaces are exactly: **Operations Shell, tenant list, member table, command preview, command lifecycle feedback, and audit evidence surfaces**.

| Surface | Owning specification | Evidence implication |
| --- | --- | --- |
| Operations Shell | Story 9.2 operations shell spec | Navigation, focus order, responsive collapse, stable selectors, and localized navigation/state copy. |
| Tenant list | Story 9.2 operations shell spec | Keyboard table scan, status/freshness labels, sorting, pagination, filtering, disabled explanations, and responsive table evidence. |
| Member table | Story 9.2 operations shell spec | Header/row relationships, role/status labels, row actions, unavailable reasons, focus return, and no-color-only state evidence. |
| Command preview | Story 9.4 remove-user journey spec | Modal/dialog focus trap, safe escape, consequence copy localization, last-owner warning, and focus return to launching row/action. |
| Command lifecycle feedback | Story 9.3 truth-state spec and Story 9.4 journey spec | Live-region politeness, no false success, exact lifecycle labels, reduced motion, and command-state selectors. |
| Audit evidence surfaces | Story 9.5 audit/recovery spec | Audit unavailable/delayed states, exact timestamps, accessible audit rows, support-safe copy, and flat audit fallback evidence. |

This baseline ties primarily to `FC-A11Y` (`needs-confirmation`). Exact Fluent UI Blazor v5 accessibility, ARIA, and focus-management behavior must be verified against the pinned package `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` at implementation time. This planning story does not assert specific component-level accessibility conformance as ready.

## 2. Keyboard and Focus Requirements

Future UI implementation stories must require:

- Keyboard reachability for all interactive elements.
- Focus order that follows task and visual order.
- Visible focus indicators in normal, high-contrast, and forced-colors modes.
- Modal focus trap for modal dialogs and command previews.
- Safe escape behavior: Escape may close or cancel where appropriate, but it must not commit a destructive or high-impact action.
- Focus return to the launching row or action after dialog close, command preview cancel, command submit, failure, or completed flow.
- Keyboard users must be able to complete or exit every modal, preview, table, and command workflow.
- Disabled or unavailable action explanations must be reachable without mouse hover. Inline-visible reasons are required; tooltips may supplement but cannot be the only explanation.

The disabled/unavailable explanation requirement stays consistent with the Story 9.3 Unavailable Action Reason pattern and must not invent new reason categories.

## 3. Screen Reader, Status, and Live-Region Requirements

Future UI implementation stories must require:

- Accessible names for status labels, truth-state badges, freshness indicators, row actions, recovery actions, command buttons, and audit references.
- Exact accessible timestamp labels representing the absolute instant, not relative time alone.
- Table semantics that expose headers, row relationships, sort state, and row actions clearly.
- Stable automation selectors or component contracts for accessibility, lifecycle, and responsive assertions. Tests must not rely on arbitrary row text or color alone.
- Live-region announcements with appropriate politeness for command lifecycle changes.
- Assertive announcements reserved for rejection, failure, destructive blockers, or unable-to-verify states only.

Live-region success announcements must obey the Story 9.3 non-collapse invariant. An implementation must never announce confirmed success before projection truth confirms, and SignalR remains a freshness nudge rather than proof.

## 4. Localization and Message Composition Requirements

Future UI implementation stories must require that these text categories are localizable:

- State labels.
- Role names.
- Timestamps.
- Warnings.
- Disabled reasons.
- Recovery actions.
- Confirmation copy.
- Empty, loading, error, degraded, stale, and unavailable copy.

Timestamps, dates, numbers, and culture-sensitive labels must use culture-aware formatting. Confirmation and warning messages must not rely on concatenated sentence fragments assembled at runtime. They must use whole localizable resource strings with named placeholders.

`FC-L10N` remains `needs-confirmation`. Resource ownership is not decided here: Shell-owned `FcShellResources` versus Tenants-owned copy keys and adopter terminology remain an implementation-time `FC-L10N` confirmation item.

Visible and announced labels must remain support-safe. The UI must never render raw payloads, bearer tokens, stack traces, internal correlation IDs, internal exception text, raw EventStore metadata, or PII. User-facing rejection text is composed at the HTTP boundary by EventStore's domain-rejection ProblemDetails handling/catalog using RFC 7807 Problem Details.

## 5. Reduced Motion and Visual Accessibility Requirements

Future UI implementation stories must require:

- Reduced-motion users do not depend on animation to understand lifecycle progression.
- Lifecycle and state transitions remain understandable from text, state labels, and structural changes when motion is disabled.
- Crossfades, connectors, progress indicators, loading transitions, and command feedback degrade gracefully under reduced motion.
- Color contrast is verified for text, icons, focus indicators, table states, badges, command states, warnings, and audit states.
- Forced-colors and high-contrast behavior is verified in light, dark, high-contrast, and forced-colors contexts.
- Color is never the sole signal for status, freshness, lifecycle, risk, authorization, or audit availability.

These requirements tie to `FC-A11Y` and `FC-TOK`. Token and component names must be verified against the selected Fluent UI Blazor and FrontComposer stack at implementation time.

## 6. UI Acceptance Evidence Matrix

This section defines the evidence required before future Phase 2 UI implementation stories can move to `ready` or `ready-with-approved-fallback`. It does not claim that any evidence has been produced in this planning story.

### 6.1 Responsive testing widths

Responsive testing must cover desktop **1024px, 1366px, 1440px, and wide layouts**; tablet **768px and 1024px**; mobile **375px and 430px**; plus horizontal table overflow, navigation collapse, and command preview/dialog behavior at narrow widths.

These are the testing widths. They are distinct from the Story 9.6 layout rule breakpoints: mobile 320-767px, tablet 768-1023px, desktop 1024px and above, and wide desktop 1440px and above.

### 6.2 Accessibility testing evidence set

Accessibility testing must cover keyboard-only navigation, screen reader review with **NVDA** and at least one browser/screen-reader pairing, automated accessibility checks, forced-colors/high-contrast mode, reduced motion, color contrast, live-region announcements, focus return, and disabled action explanations without mouse hover.

### 6.3 Required acceptance scenarios

Acceptance checks for UI stories must include **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, and permission-missing cases**.

| Scenario | Owning source | Evidence expectation |
| --- | --- | --- |
| stale projection | Story 9.3 truth-state spec | Freshness state, refresh path, fail-closed rule, and unavailable reason are visible and keyboard reachable. |
| rejected command | Story 9.3 truth-state spec; Story 9.4 remove-user journey spec | Rejection is announced with reserved assertive politeness, localized safe copy, preserved context, and recovery action. |
| unknown confirmation | Story 9.3 truth-state spec | Unable-to-verify state avoids success language, offers retry/status/audit recovery, and preserves last-confirmed projection data. |
| audit unavailable | Story 9.5 audit/recovery spec | Delayed, unavailable, and missing-support proof states remain distinct and support-safe. |
| last-owner warning | Story 9.4 remove-user journey spec | Last-owner removal is elevated-friction UI warning, not a backend prohibition, with safe focus and localized warning copy. |
| permission-missing | Story 9.3 unavailable-action reason pattern | Missing permission is distinct from stale data, blocked risk, and missing implementation dependency, with visible reason not hover-only. |

### 6.4 Ready-gate rule

A Phase 2 UI implementation story may not be marked `ready` or `ready-with-approved-fallback` until the applicable accessibility, localization, responsive, and documentation/reference evidence is cited. If reusable FrontComposer evidence is unavailable, an approved row-specific fallback must explicitly record keyboard/focus/live-region behavior, localizable copy responsibility, documentation/reference evidence, replacement path, and owner approval.

This rule is the acceptance gate behind the backlog Deferred Decision "Provide story-specific accessibility, localization, and documentation/reference evidence". It does not promote any row by itself.

## 7. Implementation Split Directive

If these outputs become implementation backlog later, split this story into focused proof targets:

- **9.7A** keyboard, focus, and modal accessibility evidence.
- **9.7B** screen reader, live region, and status accessibility evidence.
- **9.7C** localization and message composition evidence.
- **9.7D** reduced motion, forced colors, contrast, and visual accessibility evidence.
- **9.7E** responsive layout and scenario evidence matrix.

This planning story does not create those sub-stories.

## 8. Per-Row Consumption Mapping

`FC-A11Y`, `FC-L10N`, and `FC-DOC` are cross-cutting across all rows `ui-01` through `ui-15`. Each literal `blockedBy` array below is copied verbatim from `docs/tenants-ui-phase-2-story-backlog.md`. No row becomes implementation-ready in this story.

| Backlog row | Readiness | `blockedBy` (verbatim) |
| --- | --- | --- |
| `ui-01-tenant-list-read-only` | `planning-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-02-my-tenants-and-user-search-read-only` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-03-tenant-detail-overview-read-only` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-04-user-management-member-table` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-05-tenant-configuration-read-only` | `planning-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-06-global-admin-read-only` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-07-create-tenant-command` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-08-edit-tenant-metadata-command` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-09-user-management-add-or-change-role` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-10-tenant-configuration-edit` | `blocked` | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-11-audit-trail-flat-timeline` | `blocked` | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-12-tenant-detail-audit-tab` | `blocked` | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-13-disable-or-enable-tenant` | `blocked` | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-14-user-management-remove-user` | `blocked` | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-15-global-admin-command-management` | `blocked` | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |

Rows `ui-01` through `ui-09` remain `planning-only`. Rows `ui-10` through `ui-15` remain `blocked`. None becomes `ready` or `ready-with-approved-fallback` here.

## 9. Future Implementation Story Rules

A future Phase 2 UI story may apply these patterns only when it can cite all applicable evidence:

1. WCAG 2.1 AA baseline and WCAG 2.2 AA target where supported by the selected Fluent UI Blazor and FrontComposer stack.
2. Keyboard reachability, task-order focus, visible focus in normal/high-contrast/forced-colors modes, modal focus trap, safe escape, and focus return.
3. Accessible names, exact timestamp labels, table headers, row relationships, sort state, row actions, and stable selectors/component contracts.
4. Live regions with appropriate politeness and assertive announcements reserved for rejection, failure, destructive blockers, or unable-to-verify states.
5. Localizable copy for state labels, role names, timestamps, warnings, disabled reasons, recovery actions, confirmation copy, and empty/error/loading/degraded states.
6. No runtime sentence-fragment assembly for confirmation or warning messages.
7. Reduced-motion-independent lifecycle progression.
8. Verified contrast, high-contrast, forced-colors, and no-color-only behavior.
9. Full responsive testing widths and narrow-width behavior evidence.
10. Required acceptance scenarios: stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, and permission-missing.
11. Documentation/reference evidence through `FC-DOC` or an approved equivalent reference path.

## 10. Backend and Data Boundaries

This spec specifies acceptance/evidence requirements only. It adds no backend endpoint, command, query, projection field, package reference, CSS/theme/token/resource file, test project, generated artifact, or Phase 1 release gate.

Accessibility/localization requirements compose over the already-specified read endpoints `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, and `GET /api/tenants/{tenantId}/audit`, and the command endpoint `POST /api/v1/commands` per FrontComposer `EventStoreOptions.CommandEndpointPath`. The unversioned `POST /api/commands` route remains an alias to confirm against the deployed gateway, not a route normalization introduced by this story.

## 11. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.7: Define Accessibility, Localization, and UI Acceptance Evidence`
- `_bmad-output/planning-artifacts/epics.md` NFR24, UX-DR64, UX-DR69, UX-DR70, UX-DR71, UX-DR72, UX-DR73, UX-DR74, UX-DR75, UX-DR76, UX-DR77, UX-DR78, UX-DR79, UX-DR80
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Considerations`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Approach`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Fluent UI API Verification Prerequisite`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Future Story Author Checklist`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `docs/tenants-ui-phase-2-story-backlog.md#Deferred Decisions`
- `docs/tenants-ui-phase-2-story-backlog.md#Validation Notes`
- `docs/tenants-ui-operations-shell-spec.md`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md`
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- `docs/tenants-ui-responsive-layout-and-visual-system-spec.md`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#Rejection Event Payloads`
- `_bmad-output/project-context.md#Logging & Telemetry`
