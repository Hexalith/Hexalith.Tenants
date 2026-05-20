# Tenants UI FrontComposer Dependency Map

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact
Last reviewed: 2026-05-20

This map sequences the Hexalith.Tenants Phase 2 Admin UI against the current checked-out `Hexalith.FrontComposer` submodule. It uses `Hexalith.FrontComposer` for the current repository and source evidence. `FrontShell`, `@hexalith/ui`, `useCommand`, `useProjection`, `<PageLayout>`, `<AuditTimeline>`, and `<ConsequencePreview>` appear only as UX/planning aliases from older design language.

This document is not a Phase 1 backend blocker. Backend query, projection, authorization, deployment, package, and documentation stories remain independent of UI dependency readiness unless a future product decision explicitly promotes Admin UI work into Phase 1 scope.

## Evidence Scope

- FrontComposer checkout evidence was inspected from root-level submodule `Hexalith.FrontComposer` at commit `17c3605`.
- No nested submodules were initialized or updated for this map.
- Evidence paths are repo-relative. Missing or unverified evidence is recorded as `evidence: missing`.
- This map does not copy generated build artifacts, secrets, local absolute paths, private configuration, tenant/user production data, or transient logs.

## Readiness Values

| Value | Meaning |
| --- | --- |
| `available` | A current source path or documentation path exists in this checkout and is usable as planning evidence. |
| `needs-confirmation` | Some related source evidence exists, but a stable reusable contract for Tenants UI stories is not yet confirmed. |
| `missing` | No verified local evidence was found for the named deliverable. |
| `planned` | A planning artifact names the deliverable, but the source implementation is not verified in this checkout. |
| `approved-fallback` | Product and UX have approved a named fallback for a specific UI story or screen. |

## Dependency ID Catalog

The table below is the only place where dependency IDs are defined. Future Tenants UI stories should cite these IDs exactly in `blockedBy` fields when a dependency is not `available` or when the story requires product/UX fallback approval.

| ID | Owner | Expected deliverable | UX alias | Current FrontComposer name/path when verified | Readiness | Fallback or blocking policy | Evidence source | Phase 1 blocker |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `FC-TBL` | `Hexalith.FrontComposer` | Projection list/table rendering, filter/search affordances, projection template/slot overrides, empty placeholders, loading skeletons, and DataGrid helpers. | `<Table>`, `<EmptyState>`, `<LoadingState>`, `useProjection` | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Rendering` | `available` | Tenants UI stories may consume current projection/DataGrid primitives, but Tenants-specific column sets, route composition, and backend binding remain Tenants-owned. | `Hexalith.FrontComposer/docs/skills/frontcomposer/domain/projections.md`; `Hexalith.FrontComposer/_bmad-output/planning-artifacts/epics/epic-4-rich-datagrid-projection-interaction.md` | No |
| `FC-LYT` | `Hexalith.FrontComposer` plus product/UX for screen-level layout decisions | Application shell layout and explicit full-width/constrained page layout behavior for dense tables, detail views, forms, and standalone audit views. | `<PageLayout>`, full-width/constrained variants | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` | `needs-confirmation` | UI stories can remain planning-only or use the current shell layout. Full-width/constrained variants require product/UX approval or a FrontComposer story that confirms the contract. | `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout` | No |
| `FC-CMD` | `Hexalith.FrontComposer` | Blazor command lifecycle feedback, pending command identity, projection confirmation, authorized command regions, and command feedback publishing. | `useCommand`, `pendingIds`, three-phase command feedback | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Feedback`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor` | `needs-confirmation` | Command-capable UI stories must cite this ID until the Tenants-compatible command lifecycle contract is confirmed. Product/UX can approve a planning-only or reduced feedback fallback for a specific story. | `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands` | No |
| `FC-CNC` | `Hexalith.FrontComposer` plus product/UX for interaction policy | Concurrent command support and toast/message batching for rapid sequential actions without toast overflow. | concurrent command support, toast batching | Related pending-command state exists in `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`; no verified toast batching component or policy path found. | `missing` | Stories that require simultaneous row commands, rapid removals, or consolidated confirmations are blocked or planning-only unless product/UX approves a named fallback and FrontComposer owns the remaining contract. | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`; `evidence: missing` for toast batching | No |
| `FC-AUD` | `Hexalith.FrontComposer` plus product/UX for audit UX acceptance | Reusable audit timeline component or approved audit timeline fallback, including flat timeline MVP behavior, keyboard navigation, loading state, and performance expectations. | `<AuditTimeline>` | `evidence: missing` | Audit Trail and tenant-detail audit tab stories are blocked or planning-only until a FrontComposer audit timeline deliverable exists or product/UX approves a specific fallback such as a DataGrid-backed flat audit list. | `_bmad-output/planning-artifacts/ux-design-specification.md`; `evidence: missing` | No |
| `FC-CNS` | `Hexalith.FrontComposer` plus product/UX for consequence language | Reusable consequence preview component or approved fallback for disable tenant, remove user, revoke access, and remove global administrator workflows. | `<ConsequencePreview>` | `evidence: missing` | High-impact command stories must block on this ID or carry explicit product/UX approval for an inline text preview or modal-free fallback. Implementation convenience is not approval. | `_bmad-output/planning-artifacts/ux-design-specification.md`; `evidence: missing` | No |
| `FC-TOK` | `Hexalith.FrontComposer` plus product/UX for semantic mapping | Role/status semantic tokens, timeline connector token, consequence panel token, and shell-resolved theming boundaries. | role tokens, status tokens, `--timeline-connector-color`, `--consequence-bg` | Status badge and Fluent token planning exists; no verified timeline/consequence token implementation found. | `missing` | Stories may use existing Fluent/FrontComposer badge semantics for status and role only when the story names that fallback. Timeline and consequence visual tokens block audit/consequence component stories until resolved or explicitly approved. | `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges`; `evidence: missing` for timeline/consequence tokens | No |
| `FC-A11Y` | `Hexalith.FrontComposer` plus Tenants UI story author | Accessibility, keyboard, focus visibility, live-region, reduced-motion, forced-colors, and component test evidence for any consumed shell deliverable. | keyboard map, live regions, reduced motion, forced colors | Accessibility commitments and localized resource evidence exist, but Tenants-specific component coverage is not verified. | `needs-confirmation` | Every UI story must include this ID in validation. A story cannot move to ready-for-dev if its required component accessibility evidence is missing unless product/UX explicitly marks it planning-only. | `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md`; `Hexalith.FrontComposer/docs/how-to/test-generated-components.md` | No |
| `FC-L10N` | `Hexalith.FrontComposer` plus Tenants UI story author | Localization, culture-aware date/number formatting, adopter-facing terminology, and translation readiness for shell-generated and Tenants-specific UI strings. | i18n, adopter experience, localized labels | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx` | `needs-confirmation` | UI stories must define which labels and formatting are shell-owned versus Tenants-owned. Missing localization evidence keeps the UI story planning-only or blocked on this ID. | `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Resources`; `_bmad-output/planning-artifacts/ux-design-specification.md` | No |
| `FC-DOC` | `Hexalith.FrontComposer` plus Tenants UI story author | Storybook or equivalent component documentation/reference evidence for every consumed FrontComposer deliverable. | Storybook, component reference, docs reference | Equivalent docs exist for generated components and projection skills; no Storybook path or dependency was verified. | `needs-confirmation` | UI stories must cite component docs or mark `blockedBy: FC-DOC` when docs/reference evidence is missing. Do not assert Storybook coverage until a real path is verified. | `Hexalith.FrontComposer/docs/how-to/test-generated-components.md`; `Hexalith.FrontComposer/docs/skills/frontcomposer/domain/projections.md`; `Hexalith.FrontComposer/package.json`; `evidence: missing` for Storybook | No |

## Screen Dependency Matrix

| Screen | User workflow | Backend surface | Required FrontComposer deliverables | Readiness status | Fallback decision | `blockedBy` reference for future UI stories | Evidence source |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Tenant List | Elena or Sofia scans accessible tenants, filters by status/warnings/search, opens detail, and initiates tenant lifecycle actions where authorized. | `ListTenantsQuery`; tenant lifecycle commands for create, disable, enable, and update flows reachable from the list. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only until layout, command lifecycle, concurrency/toast, token, accessibility, localization, and documentation evidence are confirmed. | May use `FC-TBL` current DataGrid/projection primitives. Full-width dashboard layout, three-phase row feedback, batched toasts, and token gaps need product/UX approval or FrontComposer readiness. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid` |
| Tenant Detail | User inspects overview, members, configuration, and audit summary from one tenant context. | `GetTenantQuery`; `GetTenantUsersQuery`; `GetTenantAuditQuery` for audit summary; tenant lifecycle and metadata commands. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only because audit timeline, consequence preview, layout variant, command lifecycle, and token coverage are unresolved. | Detail overview can be scoped to read-only planning against `GetTenantQuery`. Audit and destructive actions block until `FC-AUD` and `FC-CNS` are resolved or approved as fallback. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering` |
| Create Tenant | Global administrator creates the first or next tenant from a form/slide-over and navigates to detail while projections catch up. | `CreateTenant`; read-after-write detail lookup through `GetTenantQuery` and projection confirmation. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only until command lifecycle and layout/fallback policy are confirmed. | A basic Fluent form shell can be planned, but optimistic/confirming/confirmed behavior must block on `FC-CMD` unless product/UX approves reduced feedback for an early slice. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Feedback` |
| User Management | TenantOwner or GlobalAdmin adds users, removes users, changes roles, and sees membership results settle without losing table context. | `GetTenantUsersQuery`; `AddUserToTenant`; `RemoveUserFromTenant`; `ChangeUserRole`. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only until command lifecycle, concurrent command/toast policy, consequence preview, role/status token evidence, and accessibility coverage are ready. | Read-only member table can use `FC-TBL`. Remove/change-role command stories require `FC-CMD`; destructive remove-user consequence preview requires `FC-CNS` or approved fallback. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands` |
| User Search | Sofia investigates a user across tenants; Marc self-audits or TenantOwner scopes lookup within owned tenants according to backend policy. | `GetUserTenantsQuery`; authorization behavior completed by backend stories and D11 scoping. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only for action-capable incident response; read-only search can be planned against `FC-TBL` with unresolved layout/docs/accessibility references. | Read-only results may proceed as a planning slice. Revoke/remove flows need `FC-CNS` and `FC-CMD` or explicit product/UX fallback. | `FC-LYT`, `FC-CMD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/architecture.md`; `_bmad-output/planning-artifacts/prd.md` |
| Tenant Configuration | TenantOwner edits namespace-grouped key/value configuration with validation and confirmation feedback. | `GetTenantQuery` or configuration read model; `SetTenantConfiguration`; `RemoveTenantConfiguration`. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only until command lifecycle, batching, configuration editor layout, and fallback decisions are confirmed. | Read-only configuration display can consume `FC-TBL`. Remove-setting or high-impact setting changes need `FC-CNS` only if product/UX classifies the action as requiring consequence preview. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid` |
| Audit Trail | Sofia or GlobalAdmin filters and reviews tenant access history with keyboard-scannable temporal context. | `GetTenantAuditQuery` with date range and cursor pagination. | `FC-TBL`, `FC-LYT`, `FC-AUD`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Blocked/planning-only until `FC-AUD`, timeline tokens, accessibility evidence, localization, and docs/reference coverage are resolved. | Product/UX may approve a DataGrid-backed flat audit list using `FC-TBL`; otherwise the screen blocks on `FC-AUD`. Grouped timeline mode should remain fast-follow unless separately approved. | `FC-LYT`, `FC-AUD`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `evidence: missing` for `AuditTimeline` |
| Global Admin Management | Platform administrator reviews and changes global administrator access. | Global administrator commands `SetGlobalAdministrator` and `RemoveGlobalAdministrator`; global administrator projection/read evidence from completed backend scope. | `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Planning-only until command lifecycle, consequence preview, concurrent command policy, tokens, accessibility, and docs are confirmed. | Read-only global admin table may use `FC-TBL`. Remove-global-admin workflow requires `FC-CNS` or explicit product/UX fallback because platform access is high impact. | `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | `_bmad-output/planning-artifacts/ux-design-specification.md`; `_bmad-output/planning-artifacts/prd.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor` |

## Backend Surface Inventory

The UI map consumes these completed or planned backend surfaces as evidence and must not create duplicate backend requirements:

| Surface | UI usage | Planning evidence |
| --- | --- | --- |
| `ListTenantsQuery` | Tenant List pagination, filtering, status scanning, and default landing page. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/epics.md` |
| `GetTenantQuery` | Tenant Detail overview and read-after-write detail navigation after create/update. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/epics.md` |
| `GetTenantUsersQuery` | User Management member table. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/epics.md` |
| `GetUserTenantsQuery` | User Search, incident response, and self-audit lookup. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/architecture.md` |
| `GetTenantAuditQuery` | Audit Trail and tenant-detail audit summary. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/epics.md` |
| Tenant lifecycle commands | Create, update, disable, and enable tenant workflows. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/ux-design-specification.md` |
| Member-role commands | Add, remove, and change tenant member roles. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/ux-design-specification.md` |
| Tenant configuration commands | Set and remove namespace-grouped configuration entries. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/ux-design-specification.md` |
| Global administrator commands | Set and remove global administrators. | `_bmad-output/planning-artifacts/prd.md`; `_bmad-output/planning-artifacts/ux-design-specification.md` |

## FrontComposer Evidence Summary

Current source-backed evidence exists for:

- Projection rendering and override contracts: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Rendering`.
- DataGrid helpers: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid`.
- Empty/loading projection rendering helpers: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering`.
- Authorized command region: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor`.
- Pending command state: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`.
- Command feedback services: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Feedback`.
- Projection connection status: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/EventStore/FcProjectionConnectionStatus.razor`.
- Layout shell components: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout`.
- Localization resources: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Resources`.
- Equivalent docs/testing references: `Hexalith.FrontComposer/docs/how-to/test-generated-components.md` and `Hexalith.FrontComposer/docs/skills/frontcomposer/domain/projections.md`.

Current source evidence is missing or incomplete for:

- `AuditTimeline` component.
- `ConsequencePreview` component.
- Verified Storybook coverage.
- Timeline connector token implementation.
- Consequence panel token implementation.
- Tenants-specific accessibility and localization proof for future UI screens.
- A confirmed Tenants-compatible full-width/constrained page layout contract.
- A confirmed toast batching policy for rapid command confirmation bursts.

## Future Story Author Checklist

Every Phase 2 Tenants UI story should pass this checklist before it is marked ready for development:

| Check | Required outcome |
| --- | --- |
| Dependency IDs | The story cites every consumed dependency ID from the catalog. |
| `blockedBy` | Unavailable or unconfirmed deliverables appear as exact IDs such as `FC-AUD`, `FC-CNS`, or `FC-CNC`, not broad prose like "FrontShell work". |
| Fallbacks | Any fallback for `FC-AUD`, `FC-CNS`, `FC-CMD`, `FC-CNC`, `FC-LYT`, or `FC-TOK` names the product/UX decision owner. |
| Backend scope | The story references completed backend/query evidence and does not create a duplicate backend requirement. |
| Phase 1 boundary | Missing UI-only dependencies are marked not Phase 1 blockers. |
| Evidence | Every dependency reference cites a repo-relative source path, planning artifact, decision record, or `evidence: missing`. |
| Accessibility | Keyboard, focus, live-region, reduced-motion, forced-colors, and component test evidence are explicit, usually through `FC-A11Y`. |
| Localization | Culture-aware formatting, adopter terminology, and resource ownership are explicit, usually through `FC-L10N`. |
| Documentation | Storybook or equivalent documentation/reference evidence is cited through `FC-DOC`; Storybook is not assumed. |
| Sanitization | No local absolute paths, generated `bin/` or `obj/` evidence, secrets, production tenant/user data, or copied private configuration appear in the story. |

## Review Checklist

- Every Phase 2 screen from the UX specification has a row.
- Every dependency row has an ID, owner, expected deliverable, UX alias, current path or `evidence: missing`, readiness, fallback/blocking policy, evidence, and Phase 1 blocker status.
- Accessibility, keyboard, live-region, reduced-motion, forced-colors, localization/adopter experience, and documentation/reference evidence are first-class dependencies.
- Missing UI dependencies are not promoted into Phase 1 backend scope.
- Future UI stories can copy exact `blockedBy` IDs from this document without relying on narrative prose or screen names.
