---
baseline_commit: 497a4ac
created: 2026-06-06T16:09:55+02:00
---

# Story 5.2: Scoped Audit Evidence Entry Points

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.2. -->

## Story

As an authorized user,
I want to reach audit evidence from the places where I discover or perform tenant work,
so that proof is attached to tenant, user, and command context instead of buried in a separate tool.

## Acceptance Criteria

1. Given the user is viewing tenant list, tenant detail, user membership lookup, member table, command lifecycle result, or primary Audit navigation, when audit evidence is available for the current context, then the UI exposes a scoped path to audit evidence for the relevant tenant, user, or command, and the path preserves authorization, tenant scope, filters, and return context.
2. Given an audit entry point is opened from a tenant row or tenant detail, when the audit surface loads, then audit filtering is scoped to that tenant, unauthorized tenant audit data is not revealed, and the audit page is reached through the existing server-side BFF audit query.
3. Given an audit entry point is opened from user lookup or a member row, when the audit surface loads, then the evidence context identifies the target user and tenant scope where available, and it does not promote Users to primary navigation or conflate user lookup with global administrator governance.
4. Given an audit entry point is opened from a command lifecycle result, when command-specific evidence is available, pending, delayed, unavailable, or unsupported, then the UI shows the actual audit availability state and does not fabricate proof from command acceptance, projection confirmation, or SignalR notification alone.
5. Given the user navigates to audit evidence and back, when they return to the originating surface, then filters, sort, cursor, selected row, focus target, and contextual state are preserved where applicable, and recovery from missing audit support is explicit and localized.
6. Given audit entry points render in dense rows or command panels, when keyboard or screen-reader users operate them, then each has an accessible name, visible focus, stable layout, forced-colors support, and selectors such as `data-testid="tenants-audit-entrypoint"`, and entry points are unavailable with visible reason when scope or authorization is indeterminate.
7. Given this story is complete, when verification is run, then component tests cover entry points from tenant row, tenant detail, user lookup, member row, command result, and Audit navigation, including scope preservation, authorization blocking, return context, and audit availability states.
8. Given accessibility or E2E verification is run, then keyboard navigation, focus restoration, live-region behavior, responsive safety, forced-colors rendering, and stable selectors are verified.

## Tasks / Subtasks

- [x] Add reusable scoped audit entry-point support inside Tenants UI only (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add a small Tenants-owned component or helper, for example `Components/Tenants/Audit/AuditEvidenceEntryPoint.razor`, that renders an audit link or unavailable inline reason with stable selectors.
  - [x] Keep the component surface tenant-specific and local. Do not add generic navigation, timeline, receipt, support-safety, or shared UI scaffolding to Tenants.
  - [x] Require explicit tenant scope before enabling an audit link. If tenant scope is missing, unauthorized, stale beyond actionability, or indeterminate, render a visible localized reason instead of a dead link.
  - [x] Use `data-testid="tenants-audit-entrypoint"` on every actionable entry point and add context-specific selectors such as `tenants-list-audit-entrypoint`, `tenants-detail-audit-entrypoint`, `tenants-user-audit-entrypoint`, `tenants-member-audit-entrypoint`, and `tenants-command-audit-entrypoint`.
  - [x] Use accessible names that include the source context and safe tenant/user reference, using localized whole strings.

- [x] Extend tenant-list and tenant-detail entry points (AC: 1, 2, 5, 6)
  - [x] Update `TenantDataGrid` so each tenant row exposes a tenant-scoped audit link to `/tenants/{tenantId}/audit` while preserving the existing detail link, copy button, pinned columns, and stable row layout.
  - [x] Update `TenantsWorkspace` navigation-context creation so an audit round trip can preserve list search, status filter, sort, direction, cursor, selected tenant, and focus anchor where applicable.
  - [x] Update `TenantDetailPage` to expose a tenant-scoped audit entry point in the overview/header area and pass a safe return URL back to the detail page.
  - [x] Do not query audit data directly from the list or detail components. They only create scoped navigation to the existing audit page.
  - [x] Update primary Audit navigation only as an honest scoped entry point: it may route to an audit scope-selection/last-context surface that requires explicit tenant scope, or remain unavailable with visible reason. It must not query audit data without an explicit tenant id.

- [x] Extend user and member audit entry points without promoting Users navigation (AC: 1, 3, 5, 6)
  - [x] Update `UserMembershipLookupPage` and `MyTenantsDataGrid` so each visible membership row exposes an audit entry point scoped to the row tenant and carrying the target user id as context.
  - [x] Update `MemberAccessReview` so each member row exposes an audit entry point scoped to `Detail.TenantId` and the member user id.
  - [x] Keep Users contextual. Do not add Users as a primary nav item and do not reuse global-administrator routes or governance copy for user membership audit links.
  - [x] Preserve lookup state on return: target user id, sort column, cursor/page context where available, status/focus target, and the selected membership row when possible.

- [x] Extend command-result audit handoff without fabricating proof (AC: 1, 4, 5, 6)
  - [x] Add command-result entry points to existing command lifecycle result panels only after a command has enough safe context to identify tenant scope and command/audit state.
  - [x] Cover tenant create, add member, change role, remove member, edit metadata, lifecycle disable/enable, set configuration, and remove configuration flows where the existing component already shows `TenantCommandAuditState`.
  - [x] Map command audit states exactly: `AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport` remain distinct and never render as success. `Accepted` and `Confirmed` are not audit proof.
  - [x] If command-specific audit evidence cannot be identified by a safe audit reference yet, link only to the tenant-scoped audit list with visible audit availability state and a localized reason. Do not build Story 5.3 receipts or Story 5.5 correction flows.
  - [x] SignalR nudges may trigger refresh/re-query but must never advance command audit state to audit available.

- [x] Preserve and extend the Story 5.1 audit page contract (AC: 1, 2, 3, 5)
  - [x] Extend `TenantAuditPage` route/query handling to accept optional context parameters such as `targetUserId`, `supportSafeCommandReference`, `returnUrl`, `returnFocus`, and source kind, without changing the backend audit query contract.
  - [x] Keep `GET /api/tenants/{tenantId}/audit` as the only audit data source through `ITenantQueryGateway.GetTenantAuditAsync`.
  - [x] Do not add backend audit, receipt, consequence, preview, or command-specific evidence endpoints.
  - [x] Keep cursor handling opaque and scope-bound. Changing tenant/date/category/user context clears cursor history and re-queries page 1.
  - [x] If user or command context is only a client-side filter hint and no backend filter exists, show the context banner and rely on the tenant-scoped audit grid/date/category filters. Do not pretend unsupported filtering was applied server-side.
  - [x] Keep raw event payloads, serialized command payloads, raw EventStore metadata, protected cursors, ETags, tokens, stack traces, internal correlation ids, and PII out of rendered text and copied references.

- [x] Add localization, accessibility, responsive, and styling evidence (AC: 5, 6, 8)
  - [x] Add EN/FR `Tenants.Audit.EntryPoint.*` and return-context resource keys for labels, unavailable reasons, context banners, return links, focus restoration, and command audit availability copy.
  - [x] Use whole localized strings with named placeholders. Do not assemble visible text or accessible names from fragments.
  - [x] Ensure every entry point has visible focus, keyboard operation, no color-only state, forced-colors hooks, stable dimensions in dense tables/panels, and no text overlap at mobile, tablet, desktop, and wide widths.
  - [x] Return focus to the launching row/control when the user comes back where possible; otherwise focus the origin page heading and render a visible return-context message.
  - [x] Keep live-region politeness semantic: routine navigation/context restore is polite; blocked authorization, missing support, and unable-to-verify states are assertive only when they block action.

- [x] Add focused tests and validation (AC: 1-8)
  - [x] Component tests cover tenant row, tenant detail, user lookup row, member row, command result, and primary Audit navigation entry points.
  - [x] Tests verify links include safe tenant/user/return context, preserve list/detail/user/member state, and do not expose raw payloads, command payloads, cursors, ETags, access tokens, internal correlation ids, or stack traces.
  - [x] Tests verify unavailable entry points render visible localized reasons when tenant scope, authorization, audit support, or command evidence is indeterminate.
  - [x] Tests verify command audit states remain non-collapsed and that accepted/projection-confirmed command states do not create audit proof.
  - [x] Resource tests verify EN/FR parity for every new `Tenants.Audit.EntryPoint.*` key.
  - [x] Static/component tests verify no browser-side `HttpClient`, backend route string calls from `.razor` components, `localStorage`, `sessionStorage`, `access_token`, raw payload text, cursor leakage, or EventStore metadata leakage.
  - [x] CSS/static tests verify focus-visible hooks, forced-colors hooks, dense-row layout stability, and responsive safety for entry points in rows and command panels.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] Run the xUnit v3 executable fallback for UI tests if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.2. Epic 5 covers audit evidence and forward recovery; this story only adds scoped entry points into existing audit evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`]
- FR21 requires audit evidence reachability from navigation, tenant row, tenant detail, user lookup, and command result entry points, scoped to tenant, user, or command context. [Source: `_bmad-output/planning-artifacts/epics.md#FR21`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR21`]
- FR23 requires `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support` to stay distinct, with wait, retry, continue-read-only, inspect-audit, or escalate paths. [Source: `_bmad-output/planning-artifacts/epics.md#FR23`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`]
- Receipts are Story 5.3, audit availability recovery is Story 5.4, correction start is Story 5.5, and linked proof is Story 5.6. Do not implement those here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3: Support-Safe Audit Evidence Receipt`; `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`]
- The approved `FC-AUD` fallback is a flat audit DataGrid, not a FrontComposer `<AuditTimeline>`. Story 5.2 should link to or contextualize the existing flat audit page; it should not build a timeline. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### Existing Implementation To Extend

- Story 5.1 added the tenant audit page at `/tenants/{TenantId}/audit`, `TenantAudit` state models, `AuditDataGrid`, resources, and `ITenantQueryGateway.GetTenantAuditAsync`. Reuse this work. [Source: `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#File List`]
- `TenantAuditPage` currently supports tenant scope, date range, category, cursor paging, state rendering, UTC timestamps, and a back link to tenant detail. Extend it for optional entry-point context and return context without changing the backend query shape. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `AuditDataGrid` renders support-safe `TenantAuditRow` fields and approved event-reference copy. Keep `EventReference` support-safe handling and do not expose MessageId, protected cursors, ETags, raw payloads, or EventStore metadata. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`]
- `TenantDataGrid` currently renders tenant identity, detail link, copy button, status, counts, pending, and freshness. Add a row-level audit entry point without disrupting the pinned identity/status columns or existing `tenants-list-detail-link` behavior. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`]
- `TenantsWorkspace` already restores search/status/sort/cursor/selected/anchor query state when returning from detail. Reuse this pattern for audit round trips instead of creating a second navigation state model. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`]
- `TenantDetailPage` composes tenant overview, metadata flow, lifecycle flow, member review, and configuration flow. Add the tenant-detail audit entry point near overview/header context so the scope is unambiguous. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `UserMembershipLookupPage` and `MyTenantsDataGrid` own contextual user lookup rows. Add audit links from rows but keep `/tenants/users` contextual and absent from primary navigation. [Source: `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`]
- `MemberAccessReview` owns member rows and change/remove launch controls. Add member-row audit entry points using `Detail.TenantId` and `row.Member.UserId`; preserve existing action availability, reason lists, and focus-return dictionaries. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`]
- `OperationsShellNavigation` currently renders Audit as unavailable. Story 5.2 should make primary Audit navigation honest: either enabled only when a valid scope can be selected or explicitly unavailable with visible reason. Do not fabricate a global unscoped audit view. [Source: `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/OperationsShellNavigationTests.cs`]
- If a future primary Audit route is added, it must collect or restore explicit tenant scope before loading `TenantAuditPage`; an unscoped global audit list is out of scope for this repository and this story. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`]

### Command Lifecycle Guardrails

- Existing command snapshots include `TenantCommandAuditState` values `NotStarted`, `AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport`. Use these values directly; do not add stringly typed audit-state tokens. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`]
- Existing command flows render audit handoff text such as `Tenants.AddMember.Audit.*`, `Tenants.ChangeRole.Audit.*`, `Tenants.RemoveMember.Audit.*`, `Tenants.EditMetadata.Audit.*`, `Tenants.Configuration.Set.Audit.*`, `Tenants.Configuration.Remove.Audit.*`, and `Tenants.Lifecycle.Audit.*`. Story 5.2 should add scoped entry points around those handoff states, not replace the lifecycle model. [Source: `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`]
- Command entry points must not use `Accepted`, `EventsStored`, `Completed`, projection confirmation, or SignalR as audit proof. If no audit event reference is available, link to tenant-scoped audit with a visible pending/delayed/unavailable/missing-support state. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4.3 SignalR is a freshness nudge only`]
- Do not pass `MessageId`, `CorrelationId`, protected cursor, ETag, or raw command payload as a command reference. Only a support-safe command reference may be included in URL or copy context; otherwise render the audit availability state without a command reference. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#3.3 Copy-safety rule`]
- One-at-a-time command policy remains in force. Do not add bulk audit actions or concurrent command behavior while adding command-result links. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#3. FC-CNC -> one-at-a-time command policy`]

### Backend And Data Boundary

- The consume-only backend surface is fixed. For audit, continue using `GET /api/tenants/{tenantId}/audit` through the server-side BFF and `GetTenantAuditQuery`. Do not add new backend endpoints for entry points, receipts, consequences, previews, command-specific evidence, or return context. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#Confirmed Backend Facts`]
- `TenantAuditEntry` is the existing support-safe audit read DTO. `NarrativePayload` is structured narrative metadata, not a raw persisted event payload. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#3.2 The receipt composes from the existing read model`]
- User and tenant ids are meaningful caller-supplied strings, not GUIDs or ULIDs. Preserve literal values and URI-escape them only for navigation. [Source: `_bmad-output/project-context.md#Identity Rules`]
- `SequenceNumber` is aggregate-local only and must not be used for global ordering. Audit ordering comes from the returned audit projection and cursor. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#Support Safety And Data Boundaries`]
- Authorization remains server-enforced. The UI may render unavailable reasons and avoid links when scope is indeterminate, but it must not loosen backend authorization or infer access from client-only state. [Source: `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]

### UX, Accessibility, And Localization Guardrails

- Operations Shell IA order is Tenants, Global Administrators, Audit. Users remains contextual from member row and global/user search, not a co-equal primary nav tab. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR20`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Use canonical casing-significant audit states. Badge copy may use space-form tokens such as `audit pending`; machine state uses enum values. Do not leak raw snake_case machine tokens into accessible names. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR23`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-accessibility.md#A11Y-10`]
- Entry points in dense rows or command panels need visible text or icon+text, accessible names, focus-visible styling, forced-colors support, stable dimensions, and no overlap at supported widths. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- Tenants-owned `.resx` strings are required for labels, unavailable reasons, context banners, and live-region text. Use named placeholders and avoid sentence-fragment assembly. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Safe return URLs must stay under Tenants routes and reject protocol-relative or external URLs, following the existing `TenantDetailPage.IsSafeReturnUrl` pattern. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]

### Previous Story Intelligence

- Story 5.1 completed the flat audit DataGrid and fixed UTC timestamp rendering after review. Keep UTC timestamp behavior and deterministic UTC date filter parsing. [Source: `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#Senior Developer Review (AI)`]
- Story 5.1 tests already cover audit page query mapping, opaque cursors, support-safe rows, state rendering, no browser backend/token storage, resource parity, and responsive/a11y CSS hooks. Extend those tests rather than duplicating a separate audit test harness. [Source: `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`]
- Recent commit `497a4ac feat(story-5.1): Tenant Audit Trail DataGrid` touched only Story 5.1 UI, gateway, state, resources, tests, and sprint artifacts. A compatible implementation commit for this story would be `feat(story-5.2): add scoped audit evidence entry points`. [Source: `git show --stat --oneline --name-only 497a4ac`; `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated and must not be reverted. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack and existing APIs: .NET 10, Blazor InteractiveServer, Fluent UI Blazor, EventStore query/command gateways, FrontComposer shell, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add packages or package versions. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package research is not required for this story because implementation relies on existing repo-pinned components and local contracts. The primary risks are scope, authorization, navigation context, audit-state honesty, and support safety.

### Project Structure Notes

- Expected UI changes: `TenantDataGrid`, `TenantsWorkspace`, `TenantDetailPage`, `UserMembershipLookupPage`, `MyTenantsDataGrid`, `MemberAccessReview`, `OperationsShellNavigation`, `TenantAuditPage`, command flow components that already render audit handoff state, local audit entry-point component/CSS, and EN/FR resources.
- Expected tests: `TenantListSurfaceTests`, `TenantDetailSurfaceTests`, `UserMembershipLookupSurfaceTests`, member/command flow component tests, `OperationsShellNavigationTests`, `TenantAuditPageTests`, resource parity tests, CSS/static safety tests, and no-browser-backend/token-storage tests.
- Avoid backend contract/projection changes, `GetTenantAuditQueryHandler`, audit projection storage, EventStore server registration, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, and submodule changes unless a compile-time break proves a direct integration need.
- Do not add generic routing/state/navigation abstractions, audit timelines, audit receipts, consequence endpoints, correction flows, or cross-domain support-safety scaffolding to Tenants.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#1. Audit Entry Points and the Missing-Capability Fallback Rule`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`
- PRD/UX: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR21`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`
- Existing UI: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/OperationsShellNavigationTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Implemented a Tenants-owned `AuditEvidenceEntryPoint` component, scoped URLs, localized unavailable reasons, and context banners without adding backend audit endpoints or shared scaffolding.
- Focused UI validation passed: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed 513/513.
- Full solution build passed: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore`.
- Tier 1 executable tests passed: Contracts 105/105, Client 47/47, Testing 181/181, Sample 31/31, UI 513/513.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue; xUnit v3 executable fallback was used.
- Server executable tests ran 695 tests with 6 unrelated failures from missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and pre-existing deployment documentation expectations.
- Integration executable tests ran 223 tests with 33 DAPR-prerequisite skips and 54 failures from DAPR/runtime integration setup returning `DaprException`/500s; no failures traced to Story 5.2 UI changes.

### Completion Notes List

- Added scoped audit entry points for tenant list rows, tenant detail, user lookup rows, my-tenants rows, member rows, command lifecycle panels, and primary Audit navigation.
- Extended `TenantAuditPage` to accept optional source, target user, command reference, return URL, and focus context while keeping `ITenantQueryGateway.GetTenantAuditAsync` as the only audit data source.
- Preserved command audit-state honesty: command panels link only to the tenant-scoped audit list with visible audit availability state and do not create receipts, correction flows, or command-specific proof.
- Added EN/FR resources and focused component/static tests for scoped links, unavailable reasons, return context, resource parity, responsive/focus/forced-colors hooks, and support-safety boundaries.

### File List

- `_bmad-output/implementation-artifacts/5-2-scoped-audit-evidence-entry-points.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`
- `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceEntryPointTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

### Change Log

- 2026-06-06T16:09:55+02:00 - Created Story 5.2 context and marked it ready for development.
- 2026-06-06T16:29:42+02:00 - Implemented scoped audit evidence entry points, audit-page context handling, localization, styling, and focused tests; marked story ready for review.
- 2026-06-06 - Adversarial code review (auto-fix): fixed undefined nav CSS class, localized leaked source-kind tokens in the audit context banner, removed a no-op live-region ternary, added missing audit-page context styling, corrected the File List, and added a localization regression test. UI tests 515/515. Marked story done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-06 · **Outcome:** Approve (with auto-fixes applied)

Scope: validated all 8 acceptance criteria and every `[x]` task against the implementation and git reality. No CRITICAL findings — every claimed task is genuinely implemented, all 8 command flows carry honest command-result entry points, the audit-page gateway contract is unchanged (`ITenantQueryGateway.GetTenantAuditAsync` remains the only audit data source), EN/FR resource parity holds, and command audit states (`AuditPending`/`AuditDelayed`/`AuditUnavailable`/`MissingSupport`) never collapse into success. Baseline and post-fix UI suite: 515/515 passing via the xUnit v3 executable fallback.

Findings, all auto-fixed in this pass:

1. **[MEDIUM] Undefined nav CSS class** — `OperationsShellNavigation.razor` referenced `operations-shell-nav__reason`, which did not exist in `OperationsShellNavigation.razor.css`; the previous `operations-shell-nav__visually-hidden` rule was left orphaned. The newly-visible audit "choose a tenant" reason therefore rendered unstyled and crammed into the flex row with no forced-colors hook (AC6). Fixed: added a `.operations-shell-nav__reason` rule (full-width wrap, hint color, forced-colors) and removed the dead `.operations-shell-nav__visually-hidden` rule; set the unavailable item to wrap.
2. **[MEDIUM] Raw source-kind token leaked into visible copy** — `TenantAuditPage.ContextText` interpolated the machine `source` token (e.g. `tenant-list`, `tenant-detail`) directly into the visible/aria-live context banner, contrary to the "whole localized strings / no machine tokens" rule (UX-DR23). Fixed: added a `SourceLabel` switch mapping each source kind to a localized whole phrase, with new EN/FR `Tenants.Audit.Context.SourceKind.*` keys, plus a regression test.
3. **[MEDIUM] File List incomplete** — `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` (a new hosted audit-context smoke test) and `_bmad-output/implementation-artifacts/tests/test-summary.md` were changed but undocumented. Fixed: added them, plus the two CSS files touched during review.
4. **[LOW] No-op ternary** — `ContextLiveRegion => SourceKind is "command-result" ? "polite" : "polite"` had identical branches (incomplete/dead code). Fixed: simplified to a documented constant `"polite"` (context restore is informational, never blocking).
5. **[LOW] Missing audit-page context styling** — `tenant-audit__context` / `tenant-audit__return-context` classes had no CSS. Fixed: added a context banner accent border, return-context hint styling, and a forced-colors rule.

Notes (no change, intentional/tested): the `TenantDataGrid.AuditHref` → `ListReturnUrl` string round-trip is convoluted but correct and covered by tests; the return-context message intentionally echoes the focus-target id per the story task and the hosted smoke test asserts it.
