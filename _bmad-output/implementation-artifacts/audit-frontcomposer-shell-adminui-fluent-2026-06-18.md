---
title: 'Audit — FrontComposer Shell, Counter samples, and EventStore Admin.UI Fluent conformance'
type: 'audit-findings'
created: '2026-06-18'
sprint_key: 'cc-frontcomposer-shell-and-adminui-fluent-conformance-audit'
spec: 'spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md'
baseline_commit: '4ce8a84'
auditor: 'Amelia (bmad-dev-story)'
surfaces_audited:
  - 'Hexalith.FrontComposer.Shell (submodule, read-only)'
  - 'Hexalith.FrontComposer Counter samples (submodule, read-only)'
  - 'Hexalith.EventStore.Admin.UI (submodule, read-only)'
result_summary: 'C1 REMEDIATE · C2 KEEP · C3 REMEDIATE · C4 KEEP · 3 drift findings (Admin.UI) · 1 parity recommendation'
submodule_worktrees_modified: false
superproject_gitlink_changes_included:
  - 'Hexalith.FrontComposer 6edc855 -> f4910d7'
accepted_bundled_diff: true
---

# FrontComposer Shell + Counter + Admin.UI — Fluent Conformance Audit (2026-06-18)

> **Audit, not migration.** This record is the deliverable. No file inside the
> `references/Hexalith.FrontComposer/` or `references/Hexalith.EventStore/` worktrees was modified (verified: both
> `git -C <submodule> status --porcelain` empty). The accepted review range does include adjacent
> Tenants UI/DAPR/story-record changes and a superproject `Hexalith.FrontComposer` gitlink move
> (`6edc855` -> `f4910d7`); Administrator accepted that bundled diff during code review on
> 2026-06-18. Every remediation remains an **owner handoff** (see §6). Citations are `file:line`
> against baseline `4ce8a84`.

## 1. Baseline & decision criteria

**Pinned package (both submodules, central):** `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1`
— `references/Hexalith.FrontComposer/Directory.Packages.props:46`, `references/Hexalith.EventStore/Directory.Packages.props:45`.

**⚠️ MCP-vs-pin version caveat (material to most "no equivalent" verdicts).** The
`mcp__fluent-ui-blazor` server documents build **`5.0.0.26139`** and reports **INCOMPATIBLE** against
the `5.0.0-rc.3-26138.1` pin (one prerelease build newer). Component *additions/renames* can occur
between RC builds, so the MCP can surface a primitive that may **not** ship in the pin. Every
equivalence verdict below is therefore judged **conservatively**: a KEEP is only flipped to REMEDIATE
when an equivalent both (a) plausibly exists in the pin and (b) replaces the element without
visual/semantic regression. Post-review verification used the exact local NuGet package for C1:
`5.0.0-rc.3-26138.1` already exposes `FluentCard.OnClick`, `FluentCard.Role`, and a keyboard handler,
so C1 is classified as **REMEDIATE** rather than as a GA-only advisory.

**Two-part decision test (a carve-out is JUSTIFIED ⇔ both hold):**
- **(a) No-equivalent** — no FrontComposer or pinned-Fluent-v5 primitive can express the element
  without visual/semantic regression.
- **(b) Fully-accessible-and-styled** — the element carries `role`/`aria-*`/keyboard
  activation/screen-reader fallback as applicable; it is **not** the unstyled, a11y-stripped raw
  control the rule targets.

Fail either ⇒ **REMEDIATE** (handoff). The standing rule: FrontComposer `project-context.md:131-136`
("Fluent-only UI (project-wide)") and `architecture.md:83-105` §4.1; raw `<a>` nav links permitted.

**Guards re-confirmed (read-only):**
- Shell — `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`:
  carve-out `["FcHomeCard.razor"]` at **:36** (test `Shell_components_use_fluent_v5_only_except_documented_carveouts` :29);
  Counter.Web scanned with **zero** carve-outs at **:47** (test `CounterWeb_components_use_fluent_v5_only` :42);
  regex control-only `<(button|input|select|textarea)(\s|/|>)` :24-26.
- Admin.UI — `references/Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs`:
  carve-out `["ActivityChart.razor","Streams.razor"]` at **:37** (test `AdminUi_components_use_fluent_v5_only_except_documented_carveouts` :28); same control-only regex :22-24.

## 2. Carve-out verdicts (C1–C4)

| # | Surface · file | Verdict | (a) No-equivalent in pin | (b) Fully accessible | 3-record agreement |
|---|----------------|---------|--------------------------|----------------------|--------------------|
| C1 | Shell · `FcHomeCard.razor` | **REMEDIATE** | No — pinned `FluentCard` already supports arbitrary content, `Role`, `OnClick`, `tabindex`, and Enter/Space activation | Existing raw button is accessible, but no-equivalent fails | ⚠️ §4.1/allowlist justification is stale |
| C2 | Counter · `Counter.Specimens/*` | **KEEP** | N/A — raw controls *are* the fixtures | Yes — non-shipped (Dev/Test+flag gated) | ✅ agree |
| C3 | Admin.UI · `ActivityChart.razor` | **REMEDIATE** | Yes — no chart/data-bar/histogram primitive | Not proven — focusable buttons are nested inside a `role="img"` container | ⚠️ source/allowlist/§4.1 agree, but a11y needs owner fix |
| C4 | Admin.UI · `Streams.razor` copy cell | **KEEP** | Yes — no in-cell copy/action primitive; `FluentButton` (any appearance) breaks cell truncation | Yes — aria+testid+stopPropagation+fallback | ✅ agree |

### C1 — FrontComposer Shell · `FcHomeCard` → **REMEDIATE**
- **Source (re-read):** `Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor:26-31` —
  `<button type="button" class="fc-home-card-button" role="link" tabindex="0" aria-label="@ariaLabel"
  @onclick="HandleClickAsync" @onkeydown="HandleKeydownAsync">` hosting `<h2>` (`:32`) + projection
  `<ul>` (`:51`). Custom keyboard activation (Enter/Space) in `FcHomeCard.razor.cs:51-60`. CSS
  `.fc-home-card-button` is transparent/border:0 and 100% design-token-based —
  `FcHomeDirectory.razor.css:43-52` (zero-override; `:1-2` comment confirms intent).
- **(a) No-equivalent fails:** `FluentButton` still cannot host nested `<h2>`+`<ul>` without
  structural regression, but the exact pinned package already provides a closer primitive:
  `FluentCard` has arbitrary `ChildContent`, `OnClick`, and `Role`
  (`~/.nuget/packages/.../Microsoft.FluentUI.AspNetCore.Components.xml:1950-1984`). Decompilation of
  `5.0.0-rc.3-26138.1` shows it renders a `div` with `role="@Role"`, sets `tabindex="0"` when
  `OnClick` has a delegate, and invokes the click handler on Enter/Space via `KeyDownHandlerAsync`.
  That is enough to invalidate the prior "no card-as-link primitive in the pin" conclusion. ⇒ (a)
  fails.
- **(b) Current raw element is accessible:** `role="link"`, `tabindex="0"`, `aria-label`, Enter/Space
  keyboard handler, focus-within elevation (`FcHomeDirectory.razor.css:38-41`). The current carve-out
  is not an unstyled/a11y-stripped native control; it simply no longer satisfies no-equivalent.
- **3-record status:** source ↔ allowlist (`FluentConformanceTests.cs:36`) ↔ `architecture.md:102`
  still agree with the older justification, but the pinned component check makes that justification
  stale. **REMEDIATE / DOC-DRIFT** handoff: FrontComposer owner should either migrate to a
  `FluentCard Role="link" OnClick=...` shape (preserving full-card styling, dispatch/navigation, and
  nested heading/list semantics) or produce a new evidence-backed reason why that pinned component
  regresses behavior.

### C2 — FrontComposer Counter samples · `Counter.Specimens` → **KEEP**
- **Source (re-read):** `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor` — raw
  `<button>` (`:81,106,107,148`), `<input>` (`:84,104,105`), `<label>`, `<form>` (`:103`). These **are**
  the a11y/visual fixtures (unstyled-fallback demonstrations), so the Fluent-only rule does not apply
  to the controls themselves.
- **(b) Non-shipped — confirmed:** the specimen routes are runtime-gated. `FrontComposerSpecimenRoutes.IsEnabled`
  (`src/Hexalith.FrontComposer.Shell/Components/Specimens/FrontComposerSpecimenRoutes.cs:25-31`) requires
  **both** the config flag `Hexalith:FrontComposer:Specimens:Enabled == "true"` **and**
  `IsDevelopment() || IsEnvironment("Test")`. `Counter.Web/Components/Routes.razor:23-26` adds the
  `FrontComposerTypeSpecimen` assembly to `AdditionalRouteAssemblies` **only** when that gate passes;
  a production shipped build never routes the specimens. (Counter.Web references
  `Counter.Specimens.csproj` at compile time — `Counter.Web.csproj:12` — but that is harmless given
  the runtime gate.)
- **Counter.Web is Fluent-clean:** manual scan for `<(button|input|select|textarea)>` over
  `samples/Counter/Counter.Web/**/*.razor` → **zero** matches, matching the guard's zero-carve-out
  expectation.
- **3-record agreement:** source (fixtures) ↔ guard (scan root = `Counter.Web` only, the entire
  `Counter.Specimens` tree is outside the scan — `FluentConformanceTests.cs:45,47`) ↔ `architecture.md:103`.
  **Agree.** *(Minor, non-drift: §4.1 names `FrontComposerTypeSpecimen.razor` as the representative
  file; the fixture surface is the whole 5-file `Counter.Specimens` tree, which the guard already
  excludes wholesale — consistent.)*

### C3 — EventStore Admin.UI · `ActivityChart` → **REMEDIATE**
- **Source (re-read):** `Hexalith.EventStore.Admin.UI/Components/ActivityChart.razor:34-39` —
  `<button class="activity-chart-bar-wrapper" title aria-label @onclick>` wrapping a height-scaled
  `<div class="activity-chart-bar" style="height:N%">` (`:38`). Container `role="img"` + aria-label
  (`:28`); hidden sr-only `<table>` tabular fallback (`:49-65`); summary `aria-live="polite"` (`:44`).
- **(a) No-equivalent:** MCP search `chart` → **no components**. The pinned inventory has
  `FluentProgressBar`/`FluentProgress`/`FluentProgressRing`/`FluentRatingDisplay`/`FluentSlider` — none
  expresses a 24-bucket, click-navigable, data-driven activity histogram. No regression-free
  equivalent ⇒ (a) holds.
- **(b) Accessibility gap:** the bar buttons are focusable interactive controls nested under a parent
  `role="img"` (`ActivityChart.razor:28,34-39`). The record previously asserted accessibility from
  the parent aria label, per-bar labels, and sr-only table, but did not prove that assistive
  technologies expose the nested buttons correctly under the image role. This fails the audit's
  "fully-accessible" proof bar. **REMEDIATE** handoff: EventStore owner should restructure the chart
  semantics (for example, use a group/list region for interactive bars and keep a separate static
  chart label/table fallback) or provide tested accessibility evidence.
- **3-record status:** source ↔ allowlist (`AdminUiFluentConformanceTests.cs:37`) ↔
  `architecture.md:104` agree on the carve-out, but the a11y proof is incomplete and needs owner
  remediation.

### C4 — EventStore Admin.UI · `Streams` aggregate-id-copy cell → **KEEP**
- **Source (re-read):** `Hexalith.EventStore.Admin.UI/Pages/Streams.razor:70-76` —
  `<button type="button" class="monospace grid-cell-truncate aggregate-id-copy" data-testid title
  aria-label @onclick @onclick:stopPropagation @onkeydown:stopPropagation>` inside a `FluentDataGrid`
  `TemplateColumn`. Copy via JS interop with toast + sr-only `copy-status` region (`:40-43`) +
  clipboard-unavailable fallback (`:362-378`).
- **(a) No-equivalent:** no in-cell action/copy primitive in the pin. `ButtonAppearance.Transparent`
  exists (removes bg+border) but `FluentButton` still renders a `<fluent-button>` custom element whose
  internal `.control` wrapper breaks the `.grid-cell-truncate` `text-overflow: ellipsis` monospace
  truncation and disrupts `FluentDataGrid` row alignment — i.e. FluentButton (any appearance) breaks
  the cell as documented. ⇒ (a) holds.
- **(b) Accessible:** `aria-label`, `data-testid`, `title`, `@onclick:stopPropagation` +
  `@onkeydown:stopPropagation`, plus the non-pointer sr-only status region and clipboard fallback. ⇒
  (b) holds.
- **3-record agreement:** source ↔ allowlist (`AdminUiFluentConformanceTests.cs:37`) ↔
  `architecture.md:105`. **Agree.**

## 3. Undocumented-drift sweep (three surfaces)

**Raw interactive controls outside an allowlist:** **none.** Scan of
`<(button|input|select|textarea)(\s|/|>)` over each surface returns only the documented carve-outs
(Shell → `FcHomeCard.razor`; Admin.UI → `Streams.razor` + `ActivityChart.razor`; Counter.Web →
none). **The three guards are accurate for the control class they cover.**

**Non-semantic clickables (the class the control-only regex misses):**
- **Shell + Counter.Web:** **clean** — no `@onclick` on `div/span/td/li/a/h*`. (Shell's
  `FcAccountMenu.razor:23,43` `cursor:pointer` is on Fluent components, not raw clickables.)
- **Admin.UI — multiple, beyond the pre-named `Index.razor`:**

| ID | Verdict | Evidence (`file:line`) | Note |
|----|---------|------------------------|------|
| **NS-1** | **REMEDIATE** (a11y) | `Pages/Index.razor:26,40` | `<div style="cursor:pointer" @onclick>` wrapping `StatCard`; no `role`/`tabindex`/keyboard/`aria`. Spec-named adjacency. |
| **NS-2** | **REMEDIATE** (a11y, class) | `Pages/Commands.razor:99,103`; `Pages/Events.razor:97,101`; `Pages/TypeCatalog.razor:149`; `Components/TypeDetailPanel.razor:66`; `Components/RelatedTypeList.razor:8`; `Pages/DaprHealthHistory.razor:145,272` | Clickable link-styled `<span @onclick>` / `<div @onclick>` lacking button/link semantics + keyboard. Should be `FluentLink`/`FluentButton(Transparent)` or carry `role`+keydown. |
| **DV-1** | **REMEDIATE + DOC-DRIFT / NEW-CARVE-OUT** | `Components/StorageTreemap.razor:65` (`<g @onclick style="cursor:pointer">`), `:88` (`<table class="sr-only">`) | Second data-viz surface analogous to `ActivityChart` but the clickable SVG group has no native focus/role/keyboard affordance. It is also absent from §4.1 and any allowlist. Passes the control-only guard (uses no raw control), so document it only after the owner fixes or proves the interaction semantics. |
| — | **BENIGN** (no action) | `Storage.razor:151`, `Tenants.razor:125,189`, `DeadLetters.razor:158,167`, `Snapshots.razor:118`, `Backups.razor:176` | `<div @onclick:stopPropagation>` **without** an own `@onclick` — propagation suppressors around interactive children inside clickable grid rows; not click targets, not a11y violations. |

## 4. Governance-parity recommendation (advisory — for owners, no edits from this repo)

**Benchmark:** Tenants.UI `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` declares
**12** guards (raw-controls `:146`; raw-form `:174`; raw-table `:199`; accordion sectioning `:227`;
page-layout-mode `:258`; page-header `:301`; no page-root wrapper `:342`; CSS no page-root `:366`; CSS
no semantic-colors/native-selectors `:416`; **no inline layout style `:452`**; **CSS no
layout/spacing/typography `:476`**; **structural-HTML allowlist + `<div>`+`<span>` budget `:538`**,
ceiling 220 / baseline 245) and declares **no** carve-outs.

**Gap:** Shell `FluentConformanceTests` and Admin.UI `AdminUiFluentConformanceTests` each have **only
the single raw-interactive-control guard (1 of 12).** Highest-value additions, ranked by the drift
this audit found:
1. **Non-semantic-clickable detection** (no Tenants analogue yet — a cross-cutting idea): extend the
   control regex to also flag `@onclick`/`@onkeydown` on `div/span/td/g/li`. Would have caught NS-1 +
   NS-2. **Highest value for Admin.UI.**
2. **Inline-layout-style guard** (Tenants `:452`): Admin.UI carries inline `style=` at scale —
   `DaprPubSub.razor` 57, `DaprResiliency.razor` 45, `DaprActors.razor` 35, `Backups.razor` 29,
   `DaprHealthHistory.razor` 28, … (and several on `Streams.razor`/`Index.razor`). Tenants forbids
   these; Shell/Admin.UI do not.
3. **`<div>`+`<span>` budget + component-CSS-ownership** (Tenants `:476`, `:538`): structural ratchet.
4. **No-raw-table** (Tenants `:199`): if adopted, Admin.UI must document **two** sr-only-table
   carve-outs (`ActivityChart.razor:49`, `StorageTreemap.razor:88`).

Shell is already clean of the clickable/structural drift, so parity uplift is **lower-priority for
Shell, higher for Admin.UI**. Advisory only — each owner adopts via their own scoped `bmad-*`
workflow; this repo proposes, never edits.

## 5. Verification

- `git -C Hexalith.FrontComposer status --porcelain` → **empty** (nested worktree unmodified).
- `git -C Hexalith.EventStore status --porcelain` → **empty** (nested worktree unmodified).
- No nested submodule initialized; no `--init --recursive` run.
- Code-review scope note: Administrator accepted the broader bundled diff on 2026-06-18. That range
  includes adjacent Tenants UI/DAPR/story-record changes and a superproject `Hexalith.FrontComposer`
  gitlink update (`6edc855` -> `f4910d7`). Those are tracked as part of the accepted review range;
  this audit still performs no in-place edits inside FrontComposer or EventStore submodule worktrees.

## 6. Owner handoffs

### FrontComposer owner (run via `Hexalith.FrontComposer:bmad-*` skills — not this repo)
- **H-FC-1 (REMEDIATE, C1):** Rework or re-justify `FcHomeCard` now that the exact pinned package
  already exposes `FluentCard.OnClick`, `Role`, `tabindex`, and Enter/Space activation. Candidate:
  migrate the full-card surface to `FluentCard Role="link" OnClick=...` while preserving the current
  nested `<h2>`/`<ul>` content, full-card styling, dispatch/navigation, and screen-reader label. If
  that still regresses behavior, update `architecture.md:102` and
  `FluentConformanceTests.cs:36` with the new evidence. File:
  `Components/Home/FcHomeCard.razor`, doc: `_bmad-output/project-docs/architecture.md:102`.
- **H-FC-2 (advisory, parity):** Consider adopting the Tenants structural/style guards (inline-layout-style,
  div+span budget, component-CSS ownership) and a non-semantic-clickable check in
  `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`. Shell is currently
  clean, so low urgency.

### EventStore owner (run via `Hexalith.EventStore:bmad-*` skills — not this repo)
- **H-ES-1 (REMEDIATE, NS-1):** Give `Pages/Index.razor:26,40` real button/link semantics
  (`FluentButton Appearance="Transparent"` or `role="button" tabindex="0"` + keydown) so the StatCard
  navigation is keyboard-accessible.
- **H-ES-2 (REMEDIATE, NS-2 class):** Convert the clickable link-styled `<span @onclick>` / `<div
  @onclick>` affordances (`Commands.razor:99,103`; `Events.razor:97,101`; `TypeCatalog.razor:149`;
  `TypeDetailPanel.razor:66`; `RelatedTypeList.razor:8`; `DaprHealthHistory.razor:145,272`) to
  `FluentLink`/`FluentButton(Transparent)` or add `role`+keyboard activation.
- **H-ES-3 (REMEDIATE, C3):** Fix or prove `ActivityChart.razor` accessibility. The current chart
  wraps focusable bar `<button>` elements in a parent `role="img"` container; restructure the
  interactive bars as a group/list region or provide tested evidence that keyboard and screen-reader
  access remain correct. Keep the sr-only table fallback.
- **H-ES-4 (REMEDIATE + DOC-DRIFT, DV-1):** Fix `StorageTreemap.razor` clickable SVG semantics
  (`<g @onclick style="cursor:pointer">`) with focus/role/keyboard behavior or a Fluent equivalent,
  then document the resulting data-viz carve-out in `architecture.md` §4.1 alongside `ActivityChart`
  if it still needs a carve-out. Include the sr-only table (`StorageTreemap.razor:88`) in any no-raw-table
  allowlist only after the interaction semantics are addressed.
- **H-ES-5 (advisory, parity):** Adopt the Tenants structural/style guards + a non-semantic-clickable
  check in `tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs`
  (highest value, given NS-1/NS-2/C3/DV-1 and the inline-style magnitude). If no-raw-table is adopted,
  add `ActivityChart.razor` + `StorageTreemap.razor` sr-only tables to the allowlist.

> **None** of H-ES-1..5 / H-FC-1..2 is performed from the Tenants repo. They are the deliverable.
