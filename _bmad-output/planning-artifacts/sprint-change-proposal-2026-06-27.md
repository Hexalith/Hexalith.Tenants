# Sprint Change Proposal - 2026-06-27

Approval: approved by Administrator on 2026-06-27.

Approved option: Option A - lookup-backed Users tab using the existing `GET /api/users/{userId}/tenants` behavior. A true exhaustive all-users membership list remains a separate Product/API decision.

## 1. Issue Summary

Trigger: direct user request, in French: the left menu should expose only one entry per module, and the Tenants module entry should open the page that shows the tenant list. That page should group several visible pages through tabs. The first tab is the tenant list and can be filtered to tenants the user belongs to. The second tab is the user list with each user's membership across tenants.

Current behavior conflicts with that target:

- `TenantsFrontComposerRegistration` currently contributes four Tenants-domain left-menu entries: `All tenants`, `My tenants`, `User lookup`, and `Global Administrators`.
- The PRD/UX artifacts currently define primary navigation as `Tenants`, `Global Administrators`, and `Audit`, with `Users` contextual rather than a co-equal primary nav tab.
- The implemented UI has separate routes/pages for `/tenants`, `/tenants/my`, `/tenants/users`, `/global-administrators`, and tenant-scoped audit pages.

This is a completed-UI IA correction, not a new greenfield feature.

## 2. Change Analysis Checklist Results

| Item | Status | Finding |
|---|---:|---|
| 1.1 Triggering story | [N/A] | No single active story revealed the issue. The affected completed stories are Epic 1 navigation/read surfaces, Epic 4 global-admin navigation, and Epic 5 audit entry points. |
| 1.2 Core problem | [x] | New stakeholder IA requirement: one left-menu entry per module, with Tenants sub-surfaces grouped inside the module page as tabs. |
| 1.3 Evidence | [x] | Current registration adds four Tenants nav entries; current PRD/UX says primary nav includes Tenants/Global Administrators/Audit; user explicitly requested a single module entry and tabbed Tenants page. |
| 2.1 Current epic impact | [x] | Epic 1 remains valid but Stories 1.2, 1.4, and 1.5 need to be reconciled into one tabbed workspace. |
| 2.2 Epic-level changes | [x] | Add a Correct Course implementation item for Tenants module tabbed workspace/navigation consolidation. No new product epic is required. |
| 2.3 Remaining epics | [x] | Epic 4 and Epic 5 must keep functionality but lose direct left-menu positioning unless Product explicitly keeps them as module-internal tabs/secondary routes. |
| 2.4 Future epic invalidation | [N/A] | No completed command/audit capability is invalidated. |
| 2.5 Epic order/priority | [x] | This should be sequenced before further UI polish because navigation shape affects user testing and documentation. |
| 3.1 PRD conflicts | [x] | PRD §5.1 and UX-DR20 conflict with the requested one-entry-per-module IA. |
| 3.2 Architecture conflicts | [x] | Architecture's shell composition remains valid, but the Tenants manifest/nav entry contract must change. |
| 3.3 UI/UX conflicts | [x] | Existing "Users is contextual" guidance changes: user membership becomes a module page tab. Tab behavior must still preserve filters, cursor, freshness, a11y, and stable selectors. |
| 3.4 Secondary artifacts | [x] | UI tests, resources, route docs, quickstart/demo screenshots or text, and implementation story docs that assert left-nav entries need updates. |
| 4.1 Direct adjustment | [x] | Viable for nav consolidation and tabbed workspace. Effort is moderate because current components can be reused. |
| 4.2 Potential rollback | [N/A] | Reverting completed stories would remove working surfaces without reducing the real IA requirement. |
| 4.3 PRD MVP review | [!] | Required only if "user list" means a cursor-paged list of all users. Current API supports one-user membership lookup, not all-users listing. |
| 4.4 Recommended path | [x] | Hybrid direct adjustment: implement tabbed workspace now; resolve the all-users-list API decision explicitly. |
| 5.1 Issue summary | [x] | Documented above. |
| 5.2 Epic/artifact needs | [x] | Listed in impact and proposals below. |
| 5.3 Recommendation | [x] | Consolidate nav, reuse existing list/user components under tabs, and avoid fake client aggregation for all-users. |
| 5.4 MVP impact/action plan | [x] | MVP remains achievable if the Users tab initially uses existing lookup/self-membership semantics. A true all-users tab adds backend/query scope. |
| 5.5 Handoff plan | [x] | Product Owner plus Developer handoff because this changes completed backlog semantics and UI implementation. |
| 6.1 Checklist completion | [x] | Applicable sections addressed. |
| 6.2 Proposal accuracy | [x] | Based on current PRD, epics, UX, sprint status, FrontComposer shell behavior, and UI source. |
| 6.3 User approval | [x] | Approved by Administrator on 2026-06-27 with Option A. |
| 6.4 Sprint status update | [x] | Added approved Correct Course handoff entry to `sprint-status.yaml` as backlog. |
| 6.5 Next steps/handoff | [x] | Handoff defined below. |

## 3. Impact Analysis

### Epic Impact

Epic 1 remains the primary affected epic:

- Story 1.2 `Tenant List Triage`: keep the tenant list as the first tab of the Tenants module page.
- Story 1.4 `My Tenants Self-Audit View`: replace the separate left-nav entry/page positioning with a filter or view mode inside the tenant-list tab.
- Story 1.5 `User Membership Lookup`: move from separate left-nav entry to the second module tab.
- Story 1.3 `Tenant Detail Navigation and Overview`: preserve return context from detail back to the tabbed Tenants workspace, including active tab and list filters.

Epic 4 and Epic 5 are impacted at the navigation layer only:

- Global administrator and audit capabilities should not remain separate Tenants module left-menu entries.
- Their implemented routes may remain as contextual/internal destinations, but the left menu should still show one Tenants module entry.

### Artifact Conflicts

PRD:

- §5.1 currently says the Operations Shell primary areas are `Tenants`, `Global Administrators`, and `Audit`.
- FR3/FR4 remain valid, but their IA placement changes from separate/contextual pages to tabs within the Tenants module workspace.

UX:

- UX-DR20 currently says primary navigation order is `Tenants`, `Global Administrators`, `Audit`, with Users contextual. This must be superseded.
- EXPERIENCE.md Information Architecture table must be updated so the Tenants module page owns page-local tabs.

Architecture:

- FrontComposer remains the shell owner.
- Tenants should contribute one module nav entry only.
- Page-local tabs can use Fluent UI Blazor v5 directly or FrontComposer `FcPageToolbar` tabs if that contract fits the desired page layout.

Technical:

- Current `FrontComposerNavigation` always renders a bounded-context rail tile from the manifest and a flyout containing registered entries. If Tenants registers only one entry (`/tenants`), the shell still has a module tile and a single flyout item. If the desired UX is a direct click from the rail tile to `/tenants` with no flyout, this may require a small FrontComposer shell enhancement. Otherwise, the Tenants-side change can be confined to registration and page composition.
- Current backend has `GET /api/users/{userId}/tenants` for a specific user's memberships. It does not expose a cursor-paged `GET /api/users/tenants` style "all users with memberships" read endpoint. The `TenantIndexReadModel.UserTenants` data exists server-side, but a safe public query/contract/authorization rule would need to be added before rendering a true all-users list.

## 4. Recommended Approach

Recommended path: Hybrid Direct Adjustment.

Implement the IA correction in two steps:

1. Consolidate Tenants left navigation and page-local tabs using existing components.
2. Decide whether the Users tab is a lookup tab backed by existing APIs or a true all-users list requiring a new read query.

Rationale:

- The user-visible left-menu problem can be fixed without undoing completed work.
- Existing `TenantDataGrid`, `MyTenantsDataGrid`, and `UserMembershipLookupPage` behavior can be reused rather than rewritten.
- A true all-users membership list must not be faked by walking tenant pages client-side. That would break cursor semantics, authorization boundaries, freshness honesty, and performance expectations.

Effort: Moderate.

Risk: Medium.

Primary risks:

- Regressing FrontComposer shell navigation expectations.
- Losing route/back-button context between tabs, tenant detail, and audit/detail flows.
- Misrepresenting "Users" as a complete list when it is still backed by one-user lookup.
- Accidentally exposing memberships outside the caller's authorization scope.

## 5. Detailed Change Proposals

### PRD Update

Section: PRD §5.1 Operations Shell

OLD:

```md
Three primary navigation areas, in order: **Tenants** (default landing / triage surface), **Global Administrators**, **Audit**. **Users is contextual** — reached from a member row and global search (realized by FR-3 "My Tenants" and FR-4 user lookup), not a co-equal tab.
```

NEW:

```md
The shell left navigation exposes one entry per module. For the Tenants module, the single **Tenants** entry opens the Tenants module workspace. The workspace groups Tenants-domain read surfaces as page-local tabs.

The first tab is **Tenants**: the tenant list/triage surface with search, status filter, cursor paging, freshness, pending state, and an optional "my tenants" filter that narrows results to tenants the signed-in user belongs to when the caller is authorized.

The second tab is **Users**: the user membership surface showing a user's tenant memberships. If Product requires a complete cursor-paged list of all users and their tenant memberships, the backend must add an authorization-scoped read query before the UI presents it as a complete list. Until then, the tab must be labeled and behaved as lookup/search, not as exhaustive all-users inventory.

Global administrator and audit capabilities remain available through module-internal tabs or contextual entry points, not as separate left-menu entries for the Tenants module unless a future module-level IA decision adds them explicitly.
```

Rationale: Aligns the product contract with the new one-entry-per-module IA and prevents the UI from overstating the existing user-membership API.

### UX Update

Section: UX-DR20 / EXPERIENCE.md Information Architecture

OLD:

```md
Operations Shell IA must use primary navigation in this order: Tenants, Global Administrators, Audit. Users is contextual from a member row and global search, not a co-equal nav tab. Command lifecycle is never navigation.
```

NEW:

```md
Operations Shell IA must expose one left-menu entry per module. The Tenants module entry opens the Tenants workspace. Within that workspace, use page-local tabs for Tenants-domain read surfaces. The first tab is Tenants and the second tab is Users. Command lifecycle remains inline and is never navigation.
```

Rationale: Keeps the command-lifecycle invariant while replacing the old primary-nav model.

### Epic / Story Update

Affected stories: 1.2, 1.3, 1.4, 1.5, plus navigation assertions from Epic 4 and Epic 5.

Proposed new Correct Course implementation item:

```md
### Correct Course 2026-06-27: Tenants module tabbed workspace and single nav entry

As a Hexalith operator or tenant user,
I want the left menu to show a single Tenants module entry and use tabs inside the Tenants workspace,
So that module navigation stays compact and Tenants-domain read surfaces are grouped in one place.

Acceptance Criteria:

- The FrontComposer left rail exposes only one Tenants module entry for this module.
- Activating the Tenants module opens `/tenants`.
- `/tenants` renders page-local tabs with at least `Tenants` and `Users`.
- The `Tenants` tab contains the current tenant list, preserves search/status/sort/cursor state, and adds a "my tenants" filter or view mode that uses the existing authorized membership query semantics.
- The `Users` tab contains the user membership surface. If it remains lookup-backed, labels must not imply an exhaustive all-users list. If it is promoted to an exhaustive user list, add the backend query/contract first.
- Existing `/tenants/my` and `/tenants/users` routes either redirect to `/tenants?tab=tenants&scope=mine` and `/tenants?tab=users` or remain deep-link aliases that set the active tab.
- Tenant detail, member rows, audit entry points, and command result return links preserve active tab and context.
- Global Administrators and Audit are removed from the Tenants left-menu entries; their existing pages remain accessible only through approved module-internal or contextual paths.
- All changed controls use FrontComposer/Fluent UI Blazor v5 components, Tenants-owned localized copy, stable `data-testid` selectors, keyboard operation, visible focus, and forced-colors-safe status rendering.
```

Rationale: Localizes the correction without rewriting completed command/audit work.

### UI Implementation Proposal

Files:

- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- UI component tests under `tests/Hexalith.Tenants.UI.Tests`

Registration OLD:

```csharp
registry.AddNavEntry(new FrontComposerNavEntry("tenants", "All tenants", "/tenants", ...));
registry.AddNavEntry(new FrontComposerNavEntry("tenants", "My tenants", "/tenants/my", ...));
registry.AddNavEntry(new FrontComposerNavEntry("tenants", "User lookup", "/tenants/users", ...));
registry.AddNavEntry(new FrontComposerNavEntry("tenants", "Global Administrators", "/global-administrators", ...));
```

Registration NEW:

```csharp
registry.AddNavEntry(new FrontComposerNavEntry(
    "tenants",
    "Tenants",
    "/tenants",
    Order: 0,
    TitleKey: "Tenants.Navigation.Tenants",
    Resource: typeof(TenantsResources)));
```

Rationale: One module entry points at the tabbed Tenants workspace.

Workspace OLD:

```razor
@page "/"
@page "/tenants"
...
<TenantDataGrid Rows="@_visibleRows" DetailHref="CreateDetailHref" AuditHref="CreateAuditHref" />
```

Workspace NEW:

```razor
@page "/"
@page "/tenants"
...
<FcPageToolbar Tabs="@WorkspaceTabs"
               ActiveTabId="@_activeTab"
               ActiveTabIdChanged="OnActiveTabChangedAsync"
               ... />

@if (_activeTab == "tenants")
{
    <TenantDataGrid ... />
}
else if (_activeTab == "users")
{
    <UserMembershipWorkspaceTab ... />
}
```

Rationale: Keep one page owner for module workspace, with page-local tabs rather than left-menu entries. Exact component composition should follow existing FrontComposer/Fluent patterns and avoid custom raw HTML controls when Fluent components exist.

### Backend/API Decision Proposal

Decision required for the second tab:

Option A - Lookup-backed Users tab:

- Reuse existing `GET /api/users/{userId}/tenants`.
- Rename/copy must be honest: "User membership lookup" or "Users" with visible search-first empty state.
- Scope: UI-only moderate correction.

Option B - True all-users membership list:

- Add new Tenants domain query contract, response DTO, handler, REST endpoint, BFF gateway method, UI state, tests, and docs.
- Use `TenantIndexReadModel.UserTenants` as source evidence, but enforce authorization:
  - global administrators can see all indexed users and memberships;
  - tenant owners see only users whose memberships intersect authorized owned tenants;
  - normal users should not receive a cross-user inventory unless Product explicitly grants it.
- Cursor scope must include requester identity and filters.
- Result rows must not leak hidden tenant membership counts.
- Scope: product/API change beyond navigation-only correction.

Recommendation: approve Option A for immediate IA correction unless Product confirms the all-users inventory is required now. If Product confirms Option B, add it as a separate backend + UI story before the Users tab claims to be exhaustive.

## 6. Implementation Handoff

Scope classification: Moderate.

Route to: Product Owner + Developer agent.

Responsibilities:

- Product Owner: confirm whether the Users tab is lookup-backed or an exhaustive all-users membership list.
- Developer: consolidate nav registration, build the tabbed workspace, preserve route aliases/back links, update resources/tests/docs, and avoid changing shared FrontComposer unless direct rail-click behavior requires a shell enhancement.
- Architect/FrontComposer owner: only needed if the shell must support direct rail activation to `/tenants` without a flyout for single-entry modules.

Success criteria:

- The shell left menu exposes only one Tenants module entry.
- `/tenants` is the single module workspace and shows page-local tabs.
- The first tab renders tenant list behavior with a safe "my tenants" filter/view.
- The second tab renders honest user membership behavior matching the approved API scope.
- Existing tenant detail, audit, global administrator, and command flows remain reachable through approved internal/contextual paths.
- Tests assert nav entry count, active tab routing, route aliases, localization parity, keyboard/focus behavior, stable selectors, and no authorization leakage.

## 7. Approval State

Proposal status: approved.

Approved by: Administrator.

Approved on: 2026-06-27.

Approved option: Option A - lookup-backed Users tab.

Implementation route: Product Owner / Developer handoff, then Developer implementation of the approved tabbed-workspace correction.

Deferred decision: Option B, a true exhaustive all-users membership list, remains out of this approved scope and requires a separate backend read-query contract before UI implementation.
