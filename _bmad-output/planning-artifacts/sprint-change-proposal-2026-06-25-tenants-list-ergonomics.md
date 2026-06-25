# Sprint Change Proposal — Tenants list page ergonomics

- **Date:** 2026-06-25
- **Trigger:** User-reported "l'affichage n'est pas correct" on the Tenants list (`/tenants`)
  with a screenshot; instruction to "apply professional ergonomic best practices" and a follow-up:
  "CSS should only be used when mandatory — Blazor Fluent UI V5 has a design system and does not need
  custom CSS. Change FrontComposer if needed."
- **Mode:** Comprehensive Fluent pass · Batch
- **Scope classification:** Minor → Moderate (one shared-submodule change + a domain de-duplication that
  re-points two governance tests).

## 1. Issue summary

The Tenants list page rendered a stray **black box around the "Locataires" title** and duplicated the
"My tenants" / "User lookup" navigation in both the left rail and the list toolbar — reading as a dev
scaffold rather than a professional admin surface.

## 2. Investigation findings

1. **Boxed title (headline defect).** The route heading is `<h1 tabindex="-1">` inside FrontComposer's
   `FcPageHeader`. `TenantsWorkspace` calls `FcPageHeader.FocusHeadingAsync()` after returning from a
   tenant detail (an accessibility pattern that moves focus to the page title). Nothing suppressed the
   browser's default focus ring for that **programmatic** focus, so Chrome painted the box. No
   application stylesheet was responsible — it is the user-agent focus ring. Root cause and fix live in
   **FrontComposer (`FcPageHeader`)**.

2. **Toolbar / nav duplication = a fork between two prior, test-pinned decisions.**
   - "My tenants" (`/tenants/my`) and "User lookup" (`/tenants/users`) are registered as **left-nav
     entries** in `TenantsFrontComposerRegistration` (2026-06-17 nav-accordions change).
   - The workspace **also** rendered them as toolbar links, pinned by
     `GlobalAdministratorsPageTests.Route_and_workspace_keep_users_contextual_and_global_admins_top_level`
     and `MyTenantsSurfaceTests.Tenants_workspace_exposes_contextual_my_tenants_link`.
   - First attempt removed the toolbar links unilaterally → broke both tests. Reverted and surfaced the
     decision to the user.

3. **Workspace custom CSS was effectively dead but a11y-governance-pinned.** `TenantsWorkspace.razor.css`
   only styled `.tenants-workspace__status` / `.tenants-workspace__focus-link` classes that the markup no
   longer renders, plus a `<p>` margin reset. `TenantsUiCompositionTests.Styles_include_forced_colors_and_visible_focus_rules`
   pinned the file's existence.

## 3. Decisions (user-confirmed)

- **Duplication → remove from the toolbar.** The left nav stays the single source of navigation; the list
  command bar exposes list **actions** only (Refresh / Reset filters). Mirroring nav links in a toolbar is
  a recognised professional-admin-UI anti-pattern.
- **CSS → remove the dead workspace CSS and relax the a11y guard** (the surface is now composed entirely
  from Fluent v5 primitives + FrontComposer chrome, which own their own focus / forced-colors affordances).

## 4. Changes applied

### FrontComposer (submodule — owner-approved per "change FrontComposer if needed")
- **NEW** `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.css` — suppress the
  default focus ring for non-keyboard focus on the route `<h1>`:
  `h1:focus:not(:focus-visible) { outline: none; }`. Keyboard users still get a visible `:focus-visible`
  ring. This is the user-agent focus reset the UX rules explicitly allow.

### Hexalith.Tenants.UI
- `Components/Pages/TenantsWorkspace.razor`
  - Removed the two nav-duplicate `FluentAnchorButton`s from the `Toolbar` (kept Refresh + Reset).
  - Converted the return-context `<p>` to `FluentText` (`As=Paragraph`, `Size300`, `Color.Lightweight`).
  - Dropped the no-op CSS class hooks (`__controls`, `__filters`, `__commands`, `__pager`,
    `__return-context`). `<section aria-label>` / `<nav aria-label>` landmarks retained.
- **DELETED** `Components/Pages/TenantsWorkspace.razor.css` (all rules dead or replaced).

### Tests (re-pointed to the new architecture, not weakened)
- `GlobalAdministratorsPageTests` → renamed `Route_and_nav_keep_users_contextual_and_global_admins_top_level`;
  verifies the contextual routes from `TenantsFrontComposerRegistration` and asserts the workspace no
  longer duplicates them.
- `MyTenantsSurfaceTests` → `Tenants_workspace_toolbar_does_not_duplicate_shell_navigation_links` asserts
  the toolbar link test-ids are absent.
- `TenantsUiCompositionTests.Styles_include_forced_colors_and_visible_focus_rules` → drops the deleted
  workspace-CSS assertions; keeps the `GlobalAdministratorsPage` a11y-CSS pins.

## 5. Verification

- `Hexalith.Tenants.UI.Tests` — **761/761** pass, Release, 0 warnings / 0 errors.
- `FrontComposer.Shell` FcPageHeader + FcAggregateListPage tests — **18/18** pass (scope-id added by the
  new `.razor.css` does not affect them; the suite treats scope ids as an ignorable SDK detail).
- `Hexalith.Tenants.IntegrationTests` route smoke — **6/6** pass (`/tenants/my`, `/tenants/users` routes
  unchanged).

## 6. Handoff / follow-ups

- **Uncommitted** in BOTH the Tenants repo and the FrontComposer submodule (no commit/push requested).
- The FrontComposer `FcPageHeader.razor.css` benefits **every** adopter page that focuses its route
  heading (the Tenants detail/audit/global-admin pages and any other FrontComposer consumer).
- Live verification against a running AppHost still recommended (the earlier session's app at
  `localhost:62445` had already stopped; ports are Aspire-dynamic).
