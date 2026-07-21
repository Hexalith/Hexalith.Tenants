---
created: 2026-06-06T03:17:38+02:00
baseline_commit: bcb1911
---

# Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence

Status: done

> Superseded for current Story 1.8 verification by
> `story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md`.
> This historical June 2026 artifact and its original counts are retained as history, not current proof.

<!-- Note: Created by the BMAD create-story workflow for Story 1.8. -->

## Story

As a support-aware Tenants user,
I want to copy safe identifiers and references from read-only Tenants surfaces,
so that I can share precise troubleshooting context without exposing secrets, payloads, or personal data.

## Acceptance Criteria

1. Given a tenant id, user id, configuration key, safe configuration value, or approved support-safe reference is visually truncated on an Epic 1 surface, when the user copies it, then the copied value is the literal caller-supplied string or approved reference, and it is not parsed, normalized, shortened, enriched, case-changed, or reformatted as a GUID or ULID.
2. Given a value may expose payloads, bearer tokens, decoded JWT contents, command payloads, raw EventStore metadata, internal correlation ids, stack traces, cursors, backend problem details, infrastructure names, or real PII, when copy eligibility is evaluated, then the UI blocks or redacts the unsafe value with localized explanation, and unsafe values are not copied, logged, announced, or displayed in feedback.
3. Given copy succeeds, fails, or is unavailable because the browser disallows clipboard access, the Blazor Server circuit is disconnected, the value is unsafe, or the value is empty, when feedback is rendered, then the feedback uses Tenants-owned localized copy, appropriate live-region politeness, and no false Success for unavailable or failed copy.
4. Given copy controls appear in tenant list, tenant detail, My Tenants, user lookup, read-only configuration, or member table surfaces, when the user navigates by keyboard or screen reader, then each control has an accessible name, stable footprint, visible focus, forced-colors support, and stable selectors such as `data-testid="tenants-copy-reference"`, without causing row or table layout shift.
5. Given copy is launched from a row, summary, detail identity, configuration entry, or member row, when copy finishes or fails, then focus remains on or returns to the launching control and no unrelated row, filter, paging, stale/degraded, or command-unavailable state is reset.
6. Given Epic 1 readiness evidence is reviewed, when Story 1.8 is complete, then the story artifact maps `FR1` through `FR9` and applicable `UX-DR` items to stories `1.0` through `1.8`, and records remaining gates, read BFF assumptions, accessibility/localization/responsive obligations, documentation evidence, and test coverage expectations.
7. Given verification is run, then unit or component tests cover safe-copy eligibility, blocked unsafe values, localized feedback states, focus behavior, live-region politeness, selector stability, no direct browser backend/token access, and copy controls across at least tenant list, tenant detail, and member table surfaces; CSS/component coverage verifies forced-colors and no-overlap behavior.

## Tasks / Subtasks

- [x] Add a narrowly scoped shared support-safe copy affordance (AC: 1-5)
  - [x] Prefer `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` and `.razor.css`, plus a small JS module such as `src/Hexalith.Tenants.UI/wwwroot/js/tenantsClipboard.js` only for `navigator.clipboard.writeText`.
  - [x] Keep the component Tenants-owned and domain-specific. Do not add generic clipboard, toast, shell, logging, or FrontComposer infrastructure inside this repository.
  - [x] Inject `IJSRuntime` in the component and invoke JS only from the user's button click. Use `InvokeAsync`/`InvokeVoidAsync` with `ConfigureAwait(false)` in async C# paths.
  - [x] Treat `JSException`, `JSDisconnectedException`, missing Clipboard API, insecure context, permission denial, empty value, and unsafe value as distinct non-success outcomes surfaced through localized copy.
  - [x] Keep feedback inline or colocated with the launching control using a dedicated live region. Do not invent a toast-batching policy or reuse command lifecycle feedback for copy.

- [x] Implement copy eligibility and support-safety classification before any clipboard call (AC: 1, 2)
  - [x] Allow only literal caller-supplied tenant ids/user ids, configuration keys, explicitly safe configuration values already visible in the read-only UI, and approved support-safe references. Do not copy cursors, ETags, command correlation ids, EventStore `MessageId`, raw metadata, stack traces, tokens, serialized payloads, or hidden values.
  - [x] Reuse the Story 1.6 sensitive-value deny-list discipline from `TenantConfigurationView`; move shared classification only if it stays narrowly Tenants UI scoped, for example `Services/SupportSafety/SupportSafeCopyClassifier.cs`.
  - [x] Preserve `StringComparer.Ordinal`/literal-string behavior. Never call `Guid.TryParse`, `Ulid.TryParse`, casing normalization, trimming that changes the copied value, or display-name enrichment for `TenantId`/`UserId`.
  - [x] For blocked values, render only a safe localized reason such as unavailable/unsafe-to-copy. The blocked value must not appear in markup, logs, announcements, exception messages, or feedback text.

- [x] Compose copy controls into all Epic 1 read surfaces without changing read/query behavior (AC: 1, 4, 5)
  - [x] `TenantDataGrid.razor`: add a stable copy control beside each row tenant id while preserving the detail link, pinned identity column, `tenants-list-*` selectors, cursor paging, filters, pending state, and `TruthStateBadge`.
  - [x] `TenantDetailPage.razor`: add copy for the full tenant id in the identity summary while preserving deep-link loading, safe return URL behavior, stale/degraded/unauthorized states, member summary, configuration summary, member review, and configuration view.
  - [x] `MyTenantsDataGrid.razor`: add tenant-id copy for both My Tenants (`SelectorPrefix="tenants-my"`) and user lookup (`SelectorPrefix="tenants-user"`) without introducing a primary Users nav or mutation affordance.
  - [x] `TenantConfigurationView.razor`: add copy for safe configuration keys and safe visible values only; blocked/sensitive values remain unavailable and uncopied.
  - [x] `MemberAccessReview.razor`: add copy for literal member user ids while preserving row headers, `aria-describedby` action-reason associations, read-only action slots, and all `tenants-member-*` selectors.
  - [x] Use surface-specific selectors in addition to the shared selector where useful, for example `tenants-list-copy-reference`, `tenants-detail-copy-reference`, `tenants-config-copy-reference`, and `tenants-member-copy-reference`.

- [x] Add localized copy, accessibility behavior, and responsive styling (AC: 3-5)
  - [x] Add EN/FR parity for `Tenants.Copy.*` resource keys covering accessible labels, copied/failed/unavailable/unsafe/empty states, fallback help, and live-region announcements.
  - [x] Use whole-string resources with named placeholders only. Do not assemble localized sentence fragments at runtime.
  - [x] Use a real `<button type="button">` with stable dimensions, visible `:focus-visible`, an icon or compact symbol plus accessible name, and text that cannot overflow the control.
  - [x] Set live-region politeness deliberately: `polite` for successful copy, `assertive` for unsafe/unavailable/failed copy. Never announce success until `writeText` resolves.
  - [x] Ensure forced-colors styling preserves focus, unavailable state, and copied/failed state without relying on color alone.
  - [x] Preserve table/grid column stability: identity, status, freshness, role, and reason columns must not shift or overlap when a copy button renders or changes feedback state.

- [x] Produce Epic 1 readiness evidence in the story artifact (AC: 6)
  - [x] Add or update a dedicated section in this story file mapping `FR1` through `FR9` to stories `1.0` through `1.8`, including status, main selectors, source-of-truth projection, and test evidence.
  - [x] Record remaining gates: Story 1.0 confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; Story 1.2 resolved the `FC-TBL` caveat locally with Tenants-specific grid composition; `FC-TOK` remains handled through Tenants vocabulary/Fluent semantic fallbacks until a shared token contract exists.
  - [x] Record read BFF assumptions: all Epic 1 read surfaces use existing gateway/query paths and add no backend endpoints, no direct browser backend calls, and no browser token storage.
  - [x] Record accessibility/localization/responsive evidence expected from tests and manual/Playwright checks, including keyboard, focus return, live regions, forced-colors, no-overlap, and resource parity.

- [x] Add focused verification (AC: 1-7)
  - [x] Add component tests for `SupportSafeCopyButton` covering allowed literal copy values, blocked unsafe values, empty/unavailable states, JS success/failure, `JSDisconnectedException`, localized feedback, live-region politeness, and focus-preserving button semantics.
  - [x] Extend `TenantListSurfaceTests`, `TenantDetailSurfaceTests`, `MyTenantsSurfaceTests`, and `UserMembershipLookupSurfaceTests` for copy controls and selectors on list, detail, My Tenants, user lookup, configuration, and member surfaces.
  - [x] Add resource parity tests for every `Tenants.Copy.*` key in invariant and French `.resx` files.
  - [x] Add source-safety tests that component markup/source does not contain `localStorage`, `sessionStorage`, direct `GET /api/*` calls, `access_token`, `document.execCommand`, raw backend payload terms, or copied unsafe values.
  - [x] Add CSS/source tests for `SupportSafeCopyButton.razor.css` and touched surface CSS covering stable dimensions, `:focus-visible`, `@media (forced-colors: active)`, and no row layout shift.
  - [x] Run `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false`, then `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror -m:1 -nr:false` and the UI in-process xUnit v3 executable if `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue.

## Dev Notes

Story 1.8 completes Epic 1's FR7 support-safe copy behavior and creates the readiness handoff for FR1-FR9. It is still a read-only UI/support-safety slice: it must not add command flows, audit receipts, consequence previews, backend endpoints, browser-side REST clients, token storage, generic shell services, or shared FrontComposer code. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`; `AGENTS.md#Domain Implementation Boundary`]

### Existing Implementation Context

- Stories 1.1 through 1.7 already created the Blazor InteractiveServer UI host, FrontComposer shell composition, BFF query gateway, tenant list, tenant detail, My Tenants, user lookup, configuration view, member access review, resources, selectors, forced-colors hooks, and bUnit UI test patterns. Do not recreate host, shell, AppHost, ServiceDefaults, FrontComposer scaffolding, gateway plumbing, resource infrastructure, or test harness helpers. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md#Existing Implementation Context`]
- `TenantDataGrid.razor` currently renders tenant ids as literal text inside a detail link in the pinned identity column. Story 1.8 should add a copy control without changing `DetailHref`, row identity, paging, filters, status, pending, or freshness behavior. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`]
- `TenantDetailPage.razor` currently exposes the full tenant id through accessible text in `tenants-detail-identity`, composes `MemberAccessReview` and `TenantConfigurationView`, and preserves safe return URLs. Add copy to the identity summary only when the detail snapshot is renderable. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `TenantConfigurationView.razor` already blocks sensitive configuration values using `SensitiveFragments` and never renders unsafe values. Story 1.8 may copy safe keys and safe visible values; it must not make unavailable values copyable or expose their raw contents. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Senior Developer Review (AI)`]
- `MemberAccessReview.razor` renders literal `TenantMember.UserId` values as row headers and preserves action/reason ARIA associations. Add copy for member user ids without changing read-only unavailable action slots or the six canonical unavailable reason categories. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md#Senior Developer Review (AI)`]
- `MyTenantsDataGrid.razor` is shared by My Tenants and user lookup through `ResourcePrefix` and `SelectorPrefix`. Any copy control must honor those prefixes so `tenants-my-*` and `tenants-user-*` tests remain distinct. [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`]

### Contract And Backend Requirements

- Tenant ids and user ids are meaningful caller-supplied strings, case-sensitive, and not ULIDs/GUIDs. Copy must preserve the exact literal string supplied by the projection/DTO. [Source: `_bmad-output/project-context.md#Identity Rules`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- The browser must not call backend services directly and must not store backend access tokens. Copy behavior is browser-only clipboard interop over values already rendered by the server-side UI; it does not require a BFF endpoint. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs#Tenant_list_component_has_no_browser_backend_http_or_token_storage`]
- Cursors and ETags are backend mechanics, not support-safe identifiers. They must not be rendered as copyable values or user-facing references. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Interaction Rules`]
- Support-safety is hard: no surface, copy action, log, feedback, receipt, accessible label, or announcement may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, cursors, or real PII. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`]

### UX, Accessibility, And Safety Requirements

- FR7 requires full literal identifiers and support-safe references to remain copyable even when visually truncated. This includes preserving caller-supplied strings without GUID/ULID parsing. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Interaction Rules`]
- Copy controls are interactive elements. They need stable `data-testid` selectors, keyboard reachability, visible focus, no-color-only feedback, forced-colors behavior, stable footprint, and accessible names that identify the target without exposing unsafe data. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Usage Rules`]
- Live-region politeness is a product state, not a color derivative. Successful copy can be polite; unsafe, unavailable, failed, or disconnected copy should be assertive only when the user's requested support action failed or trust is affected. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Usage Rules`]
- Microcopy must stay calm, precise, localizable, and whole-string based. The prohibited words `undo`, `rollback`, and `hidden edit` must not appear in labels, feedback, announcements, or readiness evidence. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Voice & Copy`]
- The ready-gate evidence set requires per-story accessibility, localization, responsive, and documentation/reference evidence, even though Story 1.0 confirmed the shell-level `FC-A11Y`, `FC-L10N`, and `FC-DOC` gates. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`]

### Latest Technical Information

- Use Blazor JS interop from .NET for clipboard writes. Microsoft Learn documents importing JS modules through `IJSRuntime` and notes that server-side Blazor JS interop can fail when the SignalR circuit is disconnected; catch `JSDisconnectedException` instead of logging noisy internal details. [Source: `https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-10.0`]
- Use `navigator.clipboard.writeText` for text copy only. The W3C Clipboard API defines `writeText(data)` as an async promise-returning write of `text/plain;charset=utf-8` and rejects with `NotAllowedError` when clipboard write permission is not allowed. [Source: `https://www.w3.org/TR/clipboard-apis/#dom-clipboard-writetext`]
- Browser clipboard writes require a secure context and can fail for permission or browser policy reasons. Treat this as an unavailable/failed copy state with localized copy, not as implementation success. [Source: `https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText#security_considerations`]
- Do not use `document.execCommand` fallback. It is not needed for the current support-safety requirement and would complicate testability, focus, and browser behavior.

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor`: new focused component for copy controls and feedback.
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor.css`: stable dimensions, focus-visible, forced-colors, disabled/failed/success states.
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsClipboard.js`: new tiny JS module for `navigator.clipboard.writeText` only, if JS interop is implemented through a static module.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`: optional focused classifier if shared logic is cleaner than keeping eligibility inside the component. Do not create generic infrastructure.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` and `.razor.css`: add tenant-id copy while preserving pinned identity/status/freshness columns, no layout shift, and existing selectors.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `.razor.css`: add tenant-id copy in the identity summary without changing loading/state/back-link/detail behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` and `.razor.css`: add safe key/value copy only for already-visible safe values.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` and `.razor.css`: add member user-id copy while preserving table semantics and action-reason associations.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor` and `.razor.css`: add tenant-id copy using `SelectorPrefix` and `ResourcePrefix`.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Copy.*` resources with parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs`: extend existing surface tests and add `SupportSafeCopyButtonTests.cs` if helpful.
- `tests/test-summary.md`: update only if verification summaries are maintained for the implementation story.

### Scope Boundaries

- Do not add or change backend endpoints, query contracts, DTOs, command APIs, EventStore plumbing, DAPR/Aspire wiring, AppHost registration, package versions, Dockerfiles, `.sln` files, or submodule files.
- Do not implement Epic 2 mutation flows, command lifecycle feedback, consequence preview, audit evidence receipts, global administrator review, or compensating recovery.
- Do not add reusable clipboard infrastructure to `Hexalith.FrontComposer` unless a human explicitly assigns that shared-module work. This story can file/record the follow-up in readiness evidence if reusable shell capability is missing.
- Do not log copied values or copy failures with raw values. If logging is unavoidable for diagnostics, log only a safe reason/category and surface name.

### Previous Story Intelligence

- Story 1.7 introduced `MemberAccessReview` and fixed ARIA list semantics during review. Do not wrap copy controls in invalid `role="list"` structures or break the existing `aria-describedby` relationship between member action slots and reason lists. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md#Senior Developer Review (AI)`]
- Story 1.7 verification established the current UI suite size at 118 tests and the xUnit v3 in-process executable fallback. Reuse that verification pattern if `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md#Debug Log References`]
- Story 1.6 review caught selector leakage from shared `TruthStateBadge` defaults into configuration. For Story 1.8 every composed copy control needs explicit surface selectors and must not leak list defaults into configuration/member/user surfaces. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Senior Developer Review (AI)`]
- Stories 1.5 through 1.7 kept resource prefixes surface-specific. Use `Tenants.Copy.*` for shared copy behavior and surface-specific resource prefixes only where the accessible label needs target context. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`]

### Git Intelligence

- Recent commits use story-scoped Conventional Commit style through `feat(story-1.7): Tenant Member Table and Action Availability`. If this story is committed later, use `feat(story-1.8): Support-Safe Identifier Copy and Epic 1 Readiness Evidence`. [Source: `git log --oneline -8`]
- Story 1.7 touched only the story artifact, sprint status, detail/member UI, resources, focused UI tests, and test summary. Story 1.8 should similarly stay in UI components, resources, optional UI support-safety service, story evidence, and focused tests. [Source: `git show --stat --oneline -1 HEAD`]

### Epic 1 Readiness Evidence To Produce During Implementation

| FR | Epic 1 story evidence | Current source/test anchor | Story 1.8 readiness obligation |
| --- | --- | --- | --- |
| FR1 Tenant list triage | Story 1.2 done | `TenantDataGrid.razor`, `TenantsWorkspace.razor`, `TenantListSurfaceTests.cs` | Add safe tenant-id copy and record list selectors/a11y/responsive evidence. |
| FR2 Detail navigation | Story 1.3 done | `TenantDetailPage.razor`, `TenantDetailSurfaceTests.cs` | Preserve return URL/deep-link behavior while adding identity copy. |
| FR3 My Tenants | Story 1.4 done | `MyTenantsPage.razor`, `MyTenantsDataGrid.razor`, `MyTenantsSurfaceTests.cs` | Add tenant-id copy with `tenants-my-*` selectors. |
| FR4 User membership lookup | Story 1.5 done | `UserMembershipLookupPage.razor`, `MyTenantsDataGrid.razor`, `UserMembershipLookupSurfaceTests.cs` | Add tenant-id/user-context copy without promoting Users to primary nav or leaking hidden memberships. |
| FR5 Tenant overview | Story 1.3 done | `TenantDetailPage.razor` | Add tenant-id copy in the detail identity summary and preserve status/freshness evidence. |
| FR6 Read-only configuration | Story 1.6 done | `TenantConfigurationView.razor`, `TenantDetailSurfaceTests.cs` | Add safe key/value copy only for visible safe values; keep sensitive values unavailable. |
| FR7 Support-safe copy | Story 1.8 | New `SupportSafeCopyButton` and copy tests | Deliver literal copy, blocked unsafe values, feedback, focus, live-region, selectors, forced-colors. |
| FR8 Member table | Story 1.7 done | `MemberAccessReview.razor`, `TenantDetailSurfaceTests.cs` | Add member user-id copy while preserving row headers and reason associations. |
| FR9 Action availability | Story 1.7 done | `MemberAccessReview.razor`, six unavailable reasons | Preserve fail-closed action availability; copy must not imply mutation readiness or success. |

### Project Structure Notes

- Source should stay under `src/Hexalith.Tenants.UI/`, mainly `Components/Shared/`, `Components/Tenants/`, `Components/Tenants/Members/`, `Components/Users/`, optional `Services/SupportSafety/`, `Resources/`, and component CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/Components/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Avoid raw `Assert.*`.
- This story should not require `ITenantQueryGateway`, backend adapters, server tests, contracts, client package changes, AppHost changes, or package version changes.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- PRD/UX sources: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#Support-safe reference`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Interaction Rules`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Usage Rules`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- Existing UI tests: `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`; `TenantDetailSurfaceTests.cs`; `MyTenantsSurfaceTests.cs`; `UserMembershipLookupSurfaceTests.cs`
- External API references: `https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-10.0`; `https://www.w3.org/TR/clipboard-apis/#dom-clipboard-writetext`; `https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md` plus matching submodule project-context files.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, relevant PRD/UX/docs sections, Story 1.7, current Epic 1 UI source files, UI test patterns, recent git history, and current Clipboard/Blazor JS interop documentation.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; critical disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Red phase: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` failed before implementation because `SupportSafeCopyButton` and `SupportSafeCopyClassifier` did not exist.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -m:1 -nr:false` hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue, so verification used the xUnit v3 in-process executable.
- Green verification: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- Green verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- Green verification: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed 144 tests, 0 failed (post-review re-run; the original dev-run note of 134 was stale — 141 were present at review intake and 3 review-hardening tests were added).
- Green verification: direct Tier 1-style executables passed: Sample.Tests 31, Client.Tests 47, Contracts.Tests 103, Testing.Tests 181, UI.Tests 144.
- Broader regression note: `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests` failed 6 pre-existing documentation/configuration evidence tests that expect `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and existing `tests/test-summary.md` Story 7.6A content. These failures are outside Story 1.8 UI scope and were not changed here.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 1.8 to support-safe copy controls and Epic 1 readiness evidence, separating it from command, audit, backend, and shared FrontComposer infrastructure work.
- Story context identifies the key implementation risk: copy must preserve literal caller-supplied identifiers while blocking unsafe payload/token/metadata/PII values before any clipboard call.
- Story context names the concrete Epic 1 surfaces to update and the source/test patterns to preserve.
- Added a Tenants-owned `SupportSafeCopyButton` with inline live-region feedback, localized success/failure/unsafe/empty/unavailable states, `JSDisconnectedException` handling, and a tiny `navigator.clipboard.writeText` module only.
- Added `SupportSafeCopyClassifier` in Tenants UI scope and moved configuration sensitive-value discipline behind the classifier so blocked values are rejected before any clipboard call.
- Composed safe copy controls into tenant list, tenant detail identity, My Tenants/user lookup grids, read-only configuration safe rows, and member rows while preserving existing query paths, selectors, return URLs, filters, paging, stale/degraded states, and read-only action availability.
- Added invariant/French `Tenants.Copy.*` resource parity, stable button dimensions, visible focus, forced-colors styling, and no row/table layout-shift CSS checks.
- Added component and surface verification for literal copy behavior, blocked unsafe values, JS success/failure/disconnection, live-region politeness, selector stability, source-safety, resource parity, and CSS no-overlap hooks.
- The broader Server.Tests regression executable has 6 unrelated pre-existing documentation/configuration evidence failures (they expect `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`, which was removed earlier, plus Story 7.6A `test-summary.md` content). These are outside Story 1.8's permitted AppHost/DAPR scope and are not Story 1.8 regressions.
- Adversarial review (2026-06-06) confirmed all seven acceptance criteria implemented, re-verified the Release build and the 144/144 UI executable, hardened the identifier deny-list against raw-JWT mis-wiring, closed source-safety and focus-semantics test gaps, and corrected stale evidence. With zero critical issues remaining in story scope, status advanced to `done`.

### Epic 1 Readiness Evidence

| FR | Stories | Status | Main selectors | Source of truth | Test evidence |
| --- | --- | --- | --- | --- | --- |
| FR1 Tenant list triage | 1.2, 1.8 | Implemented | `tenants-list-grid`, `tenants-list-detail-link`, `tenants-list-copy-reference`, `tenants-copy-reference` | Server-side tenant list gateway/projection rows | `TenantListSurfaceTests`, UI xUnit executable 144/144 passing |
| FR2 Detail navigation | 1.3, 1.8 | Implemented | `tenants-detail`, `tenants-detail-back`, `tenants-detail-identity`, `tenants-detail-copy-reference` | Server-side tenant detail gateway/projection snapshot | `TenantDetailSurfaceTests`, safe return URL/deep-link tests passing |
| FR3 My Tenants | 1.4, 1.8 | Implemented | `tenants-my-list`, `tenants-my-row`, `tenants-my-copy-reference` | Existing My Tenants gateway/query path | `MyTenantsSurfaceTests`, selector and no-token checks passing |
| FR4 User membership lookup | 1.5, 1.8 | Implemented | `tenants-user-lookup-results`, `tenants-user-row`, `tenants-user-copy-reference` | Existing user membership lookup gateway/query path | `UserMembershipLookupSurfaceTests`, literal target/cursor tests passing |
| FR5 Tenant overview | 1.3, 1.8 | Implemented | `tenants-detail-identity`, `tenants-detail-truth-state`, `tenants-detail-copy-reference` | Tenant detail projection snapshot | `TenantDetailSurfaceTests`, freshness/status evidence passing |
| FR6 Read-only configuration | 1.6, 1.8 | Implemented | `tenants-config-table`, `tenants-config-key`, `tenants-config-copy-reference` | Tenant detail configuration dictionary, visible safe rows only | `TenantDetailSurfaceTests`, redaction and safe-copy count tests passing |
| FR7 Support-safe copy | 1.8 | Implemented | `tenants-copy-reference` plus surface-specific copy selectors | Literal values already rendered by the server-side UI; browser clipboard only | `SupportSafeCopyButtonTests`, source-safety tests, resource parity tests passing |
| FR8 Member table | 1.7, 1.8 | Implemented | `tenants-member-table`, `tenants-member-user-id`, `tenants-member-copy-reference` | Tenant detail members projection | `TenantDetailSurfaceTests`, member row/header/reason association tests passing |
| FR9 Action availability | 1.7, 1.8 | Preserved | `tenants-member-action-slot`, `tenants-member-reason-list` | Existing fail-closed action availability rules | `TenantDetailSurfaceTests`, unavailable reason and no mutation-submit tests passing |

Remaining gates: Story 1.0 confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; Story 1.2 resolved the `FC-TBL` caveat locally with Tenants-specific grid composition; `FC-TOK` remains handled through Tenants vocabulary/Fluent semantic fallbacks until a shared token contract exists.

Read BFF assumptions: all Epic 1 read surfaces continue to use existing server-side gateway/query paths. Story 1.8 adds no backend endpoints, no direct browser backend calls, no browser token storage, no command flows, and no shared FrontComposer clipboard infrastructure.

Accessibility/localization/responsive evidence: tests cover keyboard-reachable real buttons, stable shared and surface selectors, live-region politeness, localized EN/FR resource parity, forced-colors hooks, stable dimensions, no row layout-shift CSS hooks, blocked unsafe values, and source-safety against browser backend/token/legacy clipboard paths.

### File List

- `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsClipboard.js`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T03:31:49+02:00 - Implemented Story 1.8 support-safe copy controls, localized feedback, safety classifier, Epic 1 readiness evidence, and focused UI verification. Story remains in progress pending unrelated Server.Tests evidence failures.
- 2026-06-06 - Senior Developer Review (AI, adversarial): applied auto-fixes (identifier deny-list `eyj` hardening + test; `localStorage`/`sessionStorage` source-safety assertions; focus-preserving button-semantics test; File List + stale test-count corrections), re-verified Release build and 144/144 UI executable, and advanced status to `done` (0 critical issues in story scope).

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-06 · **Mode:** adversarial, auto-fix · **Outcome:** Approved (status → done)

**Verification re-run:** `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` → 0 warnings / 0 errors. `dotnet build tests/Hexalith.Tenants.UI.Tests/...` → 0/0. `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` → 144 total, 0 failed.

**AC validation:** AC1–AC7 all IMPLEMENTED. Literal copy preserves caller-supplied strings (no GUID/ULID/case/trim normalization — verified by `Copy_button_preserves_guid_ulid_and_case_shaped_literals_exactly`); unsafe values are blocked before any clipboard call (`SupportSafeCopyClassifier`); no false `Success` on unavailable/failed/disconnected copy; stable `data-testid` selectors, real `<button type="button">`, `:focus-visible`, forced-colors, and grid no-layout-shift CSS are present and asserted; FR1–FR9 readiness evidence recorded.

**Findings and resolutions (all auto-fixed):**

- **[Med] File List incomplete** — `tests/test-summary.md` was changed in git but missing from the story File List. *Fixed:* added to File List.
- **[Low] Stale test counts** — Debug Log and readiness evidence claimed 134 UI tests; 141 were actually present at review intake. *Fixed:* corrected to the post-review verified count (144) and annotated the discrepancy.
- **[Low] Source-safety test gap (AC2)** — the task subtask claimed `localStorage`/`sessionStorage` coverage, but `Copy_source_uses_clipboard_module_without_browser_backend_or_legacy_fallbacks` omitted those assertions. *Fixed:* added component + script assertions.
- **[Low] Identifier deny-list false-negative (AC2 defense-in-depth)** — `IdentifierUnsafeFragments` contained `jwt` but not `eyj`; a raw JWT (`eyJ…`, no literal `jwt`) mis-wired into a tenant/user-id copy would have passed. *Fixed:* added `eyj` to the identifier list plus `Classifier_blocks_raw_jwt_miswired_into_identifier_copy`.
- **[Low] Focus-semantics coverage gap (AC5/AC7)** — the task claimed "focus-preserving button semantics" tested, but no test asserted it. *Fixed:* added `Copy_button_uses_focus_preserving_button_semantics` (single real `type="button"` control + non-interactive `role="status"` region ⇒ click cannot move focus).

**Observations (no change required):** the identifier deny-list intentionally stays looser than the strict config list to avoid false-positives on legitimate caller-supplied identifiers; the prior `TenantConfigurationView` separator-stripping of keys is dropped, but the value-side strict check (incl. `@`, `password`, `token`) still fail-closes realistic secrets; the single toggled `aria-live` region satisfies the politeness AC and tests, with two-region separation a possible future a11y refinement.

**Out of scope (pre-existing, not a Story 1.8 regression):** 6 `Hexalith.Tenants.Server.Tests` documentation/configuration evidence tests fail because they expect the removed `pubsub.yaml` and Story 7.6A `test-summary.md` content. Story 1.8 is forbidden from touching AppHost/DAPR scope; recommend tracking these separately.
