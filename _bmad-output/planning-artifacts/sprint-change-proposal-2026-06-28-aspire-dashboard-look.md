# Sprint Change Proposal — Tenants `/tenants` page: Aspire-dashboard look & rendering fixes

- **Date:** 2026-06-28
- **Author:** Administrator (via Correct Course workflow)
- **Trigger:** "tenants design should have the same look as aspire dashboard … the page has many issues" (two screenshots: Aspire dashboard reference + the live `https://localhost:62445/tenants`).
- **Mode:** Incremental, with **live verification** (app already running under `aspire run`).
- **Scope chosen by user:** Full Aspire parity (Tenants-owned + shared FrontComposer chrome).
- **Status:** All edits **applied and verified live + by tests**. **Uncommitted** in both the Tenants repo and the `references/Hexalith.FrontComposer` submodule.

---

## Section 1 — Issue Summary

The `/tenants` page looked broken and unpolished versus the Aspire dashboard reference. Live inspection (computed styles + DOM geometry on the running app) confirmed a mix of a shared-shell rendering defect and Tenants-page layout issues:

| # | Symptom | Root cause (measured live) | Owner |
|---|---------|----------------------------|-------|
| 1 | App title "Hexalith FrontComposer", **invisible** | Adopter set no `AppTitle` (framework fallback) **and** Fluent v5 `.fluent-layout-item` defaults header/footer text to **white**; the shell never set a foreground → white-on-light `#fafafa`. Theme tokens were healthy (`--colorNeutralForeground1 = #242424`) → **not** a stale bundle. | FrontComposer + Tenants |
| 2 | Footer "Hexalith FrontComposer © 2026" **invisible** | Footer `FluentText` used `Color="Color.Lightweight"`, which in this Fluent v5 build emits `color: var(--colorNeutralForegroundInverted)` (**white**, the on-dark foreground). | FrontComposer |
| 3 | Long tenant IDs **overlap the copy button** | `.tenant-data-grid__detail-link { width: fit-content }` let a 36-char id grow to ~383px and overrun its `minmax(0,1fr)` track (copy button at x≈186px) → ~197px overlap. | Tenants |
| 4 | Table **horizontal scroll** at narrow widths | `min-width: 58rem` + 7 fixed columns (~1090px). | Tenants |
| 5 | **Cramped, stacked** controls (3 bands: tabs / scope+buttons / search+status) | `FcAggregateListPage` renders `Toolbar` and `Filters` as separate rows. | Tenants |
| 6 | **Sparse / cryptic** nav rail | `FrontComposerNavigation.razor.css` was **entirely inert**: the CSS-isolation scope id never reached the `FluentStack` root, so the rail divider, padding, active-accent indicator, and label rule **all failed at runtime**. | FrontComposer |

---

## Section 2 — Impact Analysis

- **Epic impact:** None at the requirement level. These are presentation/chrome corrections, not behavior or contract changes.
- **Story impact:** Touches the Tenants list/workspace surface (Epic 1 stories 1.2 / tabbed-workspace) at the CSS/markup level only; acceptance criteria unchanged.
- **Artifact conflicts:** Minor UX-doc drift (the "Filters" band is now folded into the toolbar control bar) — see Section 5 doc-sync note.
- **Cross-module impact:** The contrast fix and nav-rail revival are in the **shared** `Hexalith.FrontComposer.Shell` and therefore improve **every** Hexalith app (the white-on-light header/footer and the dead nav CSS were latent everywhere).
- **Technical impact:** No new dependencies. CSS/markup + one config-style decision (accent — declined). FrontComposer shell test snapshot updated for the header style string.

---

## Section 3 — Recommended Approach (chosen & executed)

**Direct adjustment** within the existing plan — no rollback, no MVP change. The user selected **Full Aspire parity**; during execution two scope refinements were made by the user:

1. **Accent color: keep teal `#0097A7`** (user declined the Aspire purple). "Aspire look" = layout / chrome / density, **not** brand color.
2. **Nav rail: full structural fix done in-line** (rather than deferring to a separate FrontComposer correct-course), after live inspection proved the rail CSS was 100% inert and a `::deep` tweak could not work.

Verification method: each edit was applied, the `tenants-ui` Aspire resource was rebuilt (`rebuild` command), the page reloaded, and computed styles / DOM geometry were re-measured live; test suites were run per change.

---

## Section 4 — Detailed Change Proposals (all applied)

### 🟢 Tenants repo (uncommitted)

**4.1 App title** — `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`
```diff
- <FrontComposerShell>@Body</FrontComposerShell>
+ <FrontComposerShell AppTitle="Hexalith.Tenants">@Body</FrontComposerShell>
```
Live: header now reads "Hexalith.Tenants" (`color rgb(36,36,36)` after 4.5).

**4.2 Tenant-ID truncation** — `Components/Tenants/TenantDataGrid.razor.css`
```diff
  .tenant-data-grid__identity-line { align-items: center; /* was start */ ... }
- .tenant-data-grid__identity strong, .tenant-data-grid__identity span { overflow-wrap: anywhere; }
+ .tenant-data-grid__identity span { overflow-wrap: anywhere; }
  .tenant-data-grid__detail-link {
      color: LinkText;
-     width: fit-content;
+     min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
  }
```
+ `TenantDataGrid.razor`: added `title="@context.TenantId"` to the id link (full value on hover; copy button retained).
Live: long ids now clip with an ellipsis; **no overlap** (verified ids no longer overrun the copy button).

**4.3 Single Aspire-style control bar** — `Components/Pages/TenantsWorkspace.razor`
- Moved the search + status controls **up from the `Filters` slot into the toolbar row** (gated to the list view); emptied the `Filters` slot. Result: tabs → one wrapping row `[Scope] [Search] [Status] [Refresh] [Reset]`. All `data-testid`s preserved.

**4.4 Test sync** — `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- Updated the MainLayout composition assertion to expect `<FrontComposerShell AppTitle="Hexalith.Tenants">`.

### 🟠 FrontComposer submodule (`references/Hexalith.FrontComposer`, uncommitted, shared)

**4.5 Header/footer contrast** — `…/Shell/Components/Layout/FrontComposerShell.razor`
```diff
  HEADER Style: … background: var(--colorNeutralBackground2);
+                  color: var(--colorNeutralForeground1);  border-block-end: …
  FOOTER Style: … background: var(--colorNeutralBackground2);
+                  color: var(--colorNeutralForeground2);  border-block-start: …
- <FluentText As="TextTag.Span" Size="TextSize.Size200" Color="Color.Lightweight">
+ <FluentText As="TextTag.Span" Size="TextSize.Size200">   @* inherit muted fg2; Lightweight = inverted white *@
```
Live: title `rgb(36,36,36)`, footer `rgb(66,66,66)` — both legible.

**4.6 Nav rail revival + Aspire restyle** — `…/Shell/Components/Layout/FrontComposerNavigation.razor(.css)`
- Wrapped the rail in a **plain `<div class="@RailClass" …>`** root (carries the scope id; data-testid/`data-rail-width`/role/aria moved onto it) with the existing `FluentStack` nested inside for the vertical stack.
- CSS now applies: rail divider + padding revived; **`::deep .fc-navigation-rail__tile { flex-direction: column }`** → icon-over-label; **active accent indicator** (`border-inline-start: 3px var(--fc-color-accent)`) live; label centered, single line.

**4.7 Test sync** — `…/Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- Header-chrome assertion updated to include `color: var(--colorNeutralForeground1)`.

### ⚪ Declined
- **Accent purple** — user kept teal `#0097A7`.

---

## Section 5 — Implementation Handoff

- **Scope class:** Minor–Moderate (presentation only; spans one shared submodule).
- **Verification (all green):**
  - Release `-warnaserror` build (Tenants.UI + FrontComposer.Shell): **0/0**.
  - Tenants `UI.Tests`: **777/777** (Release).
  - FrontComposer `Shell.Tests` (nav + shell + conformance + slot-mapping): **86/86** (Debug).
  - Live: header/footer legible, ids non-overlapping, single control bar, Aspire icon-over-label rail with active indicator.
- **Commit/push:** **None performed.** The FrontComposer changes are in the **shared submodule** — per submodule policy they need the user's explicit decision before commit/push, and the gitlink/submodule-pointer consistency must be checked when they are.
- **Doc-sync:** **No drift / no changes needed.** Verified against the canonical UX mockup (`ux-designs/ux-tenants-2026-06-02/mockups/mock-tenant-list.html`), which already specifies a **single `.toolbar`** (search + status + actions together) — the control-bar consolidation (4.3) brings the implementation **into** alignment with the documented design. PRD §157 describes search/status/paging at the feature level only (no layout drift); no epic AC changes.
- **Optional future parity (not required):** finer rail spacing / icon sizing tuning is now discretionary; the originally-anticipated *separate* FrontComposer correct-course is **no longer needed** for the visible items, since the nav rail was fixed here.
