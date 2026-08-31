---
title: 'FrontComposer Shell, Counter samples, and EventStore Admin.UI Fluent conformance audit'
type: 'audit'
created: '2026-06-18'
status: 'done'
sprint_key: 'cc-frontcomposer-shell-and-adminui-fluent-conformance-audit'
baseline_commit: '4ce8a84'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md (§5.2) on 2026-06-18'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md'
  - '{project-root}/references/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md'  # §4.1 UI component policy + carve-out table
  - '{project-root}/references/Hexalith.FrontComposer/_bmad-output/project-context.md'  # Fluent-only UI rule (lines ~131-136)
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The 2026-06-18 "Fluent-only components/styles" Correct Course (approved by Administrator) tightened the Tenants.UI conformance sweeps, but explicitly **deferred** the other three UI surfaces to this backlog story. Four raw-HTML/CSS **carve-outs** currently exist outside Tenants.UI and are allowlisted in their own surface's governance guard:

| # | Surface | Carve-out file | Element |
|---|---------|----------------|---------|
| C1 | FrontComposer Shell | `Components/Home/FcHomeCard.razor` | full-card `<button role="link">` hosting `<h2>` + projection `<ul>` |
| C2 | FrontComposer Counter samples | `Counter.Specimens/FrontComposerTypeSpecimen.razor` (+ Specimens tree) | raw `<button>/<input>/<label>/<form>/<table>` a11y/visual specimen fixtures |
| C3 | EventStore Admin.UI | `Components/ActivityChart.razor` | clickable bar-chart bar (`<button>` wrapping a height-scaled `<div>`) |
| C4 | EventStore Admin.UI | `Pages/Streams.razor` | inline monospace click-to-copy aggregate-ID grid cell (`<button>`) |

These are **already documented** (FrontComposer `architecture.md` §4.1 carve-out table) and **already governed** (each surface's `…FluentConformanceTests`). What has **not** happened is an explicit re-justification of each carve-out against the *current* pinned Fluent v5 package and a sweep for *undocumented* drift on those surfaces. The standing rule (FrontComposer `project-context.md`; `hexalith-ux-instructions.md`) is: every `.razor` page/component uses FrontComposer or Fluent v5 components; raw HTML/CSS/JS is allowed **only when no Fluent/FrontComposer equivalent exists**; raw `<a>` nav links are allowed.

**Approach:** This is an **audit + handoff** story, **not** a code-migration story. For each documented carve-out, re-apply the "only when no Fluent equivalent exists" test against the pinned `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` package and confirm the carve-out is still *fully styled + accessible* (not the unstyled-control defect the rule targets). Sweep the same three surfaces for **new/undocumented** raw controls or de-semanticized markup. Confirm each carve-out's three records agree (source ↔ guard allowlist ↔ `architecture.md` §4.1 row). Produce a **findings/decision record in this repo**; route every remediation and every guard/doc edit to the owning submodule via a **handoff** — the Tenants repo must not edit FrontComposer or EventStore files.

## Boundaries & Constraints

**Always:** Read submodule source read-only. Test every "no equivalent" judgment against the pinned `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` (use the `mcp__fluent-ui-blazor__*` tools — `list_components`, `search_components`, `get_component_details`, `check_project_version`). Cite `file:line` evidence for every verdict. Keep the findings record and any new sprint/handoff artifacts **inside this repo** (`_bmad-output/`).

**Decision test (a carve-out is JUSTIFIED only if BOTH hold):** (a) no FrontComposer or Fluent v5 primitive in the pinned package can express the element without visual/semantic regression, AND (b) the element is fully styled and accessible — carries `role`/`aria-*`/keyboard activation/screen-reader fallback as applicable — i.e. it is **not** the unstyled, a11y-stripped raw control the rule targets. Fail either ⇒ verdict **REMEDIATE** (handoff).

**Ask First / Stop and hand off:** If a carve-out is found no-longer-justified (a Fluent equivalent now exists, or a11y has regressed), record **REMEDIATE** with the exact owner + submodule file(s) — do **not** fix it in place from this repo. If the human explicitly approves an in-place submodule fix, it must run through that submodule's **own** BMAD workflow (FrontComposer / EventStore each have scoped `bmad-*` skills), not this story.

**Never:** Edit any file under `references/Hexalith.FrontComposer/` or `references/Hexalith.EventStore/` (source `.razor`/`.razor.css`, governance guards, or `architecture.md` §4.1) from the Tenants repo. Run `git submodule update --init --recursive` or initialize/modify nested submodules. Weaken or delete any existing conformance guard. Add UI infrastructure to Tenants. Redesign or restyle any surface. Treat the Counter **web app** (`Counter.Web` — shipped, scanned with zero carve-outs) as a fixture: only the `Counter.Specimens` tree is the excluded fixture surface.

## I/O & Edge-Case Matrix (audit decision matrix)

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|----------------------------|----------------|
| Carve-out still justified | Pinned package has no equivalent AND element is fully styled + accessible | Verdict **KEEP**; record evidence + confirm source ↔ allowlist ↔ §4.1 agree | If the three records disagree, record a **doc-drift** finding + handoff to align them |
| Fluent equivalent now exists | A pinned-package primitive (or new RC) can express the element without regression | Verdict **REMEDIATE**; handoff names owner + file + suggested Fluent replacement | Do not migrate from this repo; the handoff is the deliverable |
| a11y regression | Carve-out missing `role`/`aria`/keyboard/fallback | Verdict **REMEDIATE**; handoff flags the a11y gap | Treat as the unstyled-control defect the rule targets |
| Undocumented raw control | A raw `<button>/<input>/<select>/<textarea>` exists on a surface but is **not** allowlisted | New finding: **NEW-GUARD/REMEDIATE**; handoff to add allowlist entry or remediate | If the guard already fails on it, note the surface's guard is the safety net |
| Non-control a11y drift | Clickable `<div @onclick>` / de-semanticized landmark slips the narrow control regex (e.g. Admin.UI `Index.razor`) | New finding recorded with verdict + handoff; recommend semantic remediation | Flag that the surface guard does not yet catch this class |
| Governance parity gap | Shell/Admin.UI guards check only raw controls, not Tenants' newer structural-HTML budget / inline-layout-style / component-CSS guards | Record a **governance-parity recommendation** for the owners (advisory) | Do not add the guards from this repo; recommend only |
| Counter surface mix-up | `Counter.Web` (shipped) vs `Counter.Specimens` (fixture) | Confirm `Counter.Web` is Fluent-clean and Specimens is genuinely non-shipped (not routed/linked from a shipped page) | If a specimen is reachable as a shipped route, escalate as REMEDIATE |

</frozen-after-approval>

## Carve-Out Register (current evidence — pre-seeded; dev fills the Verdict column)

> Evidence below was gathered from the submodule source on baseline `4ce8a84`. The dev agent **re-verifies** each row against current source + the pinned package, then assigns a Verdict (KEEP / REMEDIATE / DOC-DRIFT) with citations in the Dev Agent Record.

### C1 — FrontComposer Shell · `FcHomeCard`
- **Source:** `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor` (+ code-behind `FcHomeCard.razor.cs`; styling in `Components/Home/FcHomeDirectory.razor.css` — `.fc-home-card-button` is `background:transparent; border:0; display:block; width:100%; padding:calc(var(--design-unit)*3px)`).
- **Raw markup:** `<button type="button" role="link">` (custom keyboard activation) hosting `<h2>` title + projection `<ul>`/`<li>`/`<span>`. No inline styles; CSS is scoped + design-token based.
- **Allowlist:** `references/Hexalith.FrontComposer/tests/.../Governance/FluentConformanceTests.cs:36` → `carveOuts = ["FcHomeCard.razor"]` (test `Shell_components_use_fluent_v5_only_except_documented_carveouts`, line 29).
- **§4.1 row:** `architecture.md:102` — "framework chrome; `role="link"` + custom keyboard activation; hosts `<h2>` + projection `<ul>` a `FluentButton` cannot contain without regression."
- **Stated justification:** `FluentButton` cannot host nested `<h2>`+`<ul>` semantic content without visual/structural regression; the card needs custom keyboard activation the Razor compiler emits only on a real element. **Re-test:** does the pinned package offer a card-as-link primitive (`FluentCard` + `FluentAnchor` composition, or newer) that hosts heading + list without regression? **Verdict:** _(dev)_

### C2 — FrontComposer Counter samples · `Counter.Specimens`
- **Source:** `references/Hexalith.FrontComposer/samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor` (+ `FrontComposerDataFormattingSpecimen.razor` and the rest of the `Counter.Specimens/` tree). Raw `<button>/<input>/<label>/<form>/<table>` are the **content** (a11y/visual fixtures demonstrating unstyled fallback).
- **Allowlist:** `FluentConformanceTests.cs:43-47` — the guard scans `Counter.Web` (the **shipped** sample app) with **zero** carve-outs and **excludes the `Counter.Specimens` tree entirely**.
- **§4.1 row:** `architecture.md:103` — "the raw controls **are** the a11y/visual specimen fixtures; not a shipped UI page."
- **Re-test:** (1) Confirm `Counter.Web` is still Fluent-clean (guard passes, no new raw controls). (2) Confirm `Counter.Specimens` is genuinely non-shipped — not routed or linked from any shipped surface. If a specimen is reachable as a shipped route ⇒ REMEDIATE. **Verdict:** _(dev)_

### C3 — EventStore Admin.UI · `ActivityChart`
- **Source:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/ActivityChart.razor`. `<button class="activity-chart-bar-wrapper">` wraps a height-scaled `<div class="activity-chart-bar" style="height:N%">` (data-driven bar). Carries `aria-label`, container `role="img"`, and a hidden `<table class="sr-only">` tabular fallback. CSS owns flex layout, hover/focus-visible ring, 200ms opacity transition (respects `prefers-reduced-motion`), responsive bar-hiding, forced-colors swap.
- **Allowlist:** `references/Hexalith.EventStore/tests/.../Governance/AdminUiFluentConformanceTests.cs:37` → `carveOuts = ["ActivityChart.razor", "Streams.razor"]` (test `AdminUi_components_use_fluent_v5_only_except_documented_carveouts`, line 28).
- **§4.1 row:** `architecture.md:104` — "data-visualization element (height-scaled `<div>`); `aria-label` present; `FluentButton` destroys the bar."
- **Re-test:** does the pinned package offer a chart/meter/data-bar primitive that preserves the data-driven height? Confirm the sr-only table fallback + aria still present. **Verdict:** _(dev)_

### C4 — EventStore Admin.UI · `Streams` (aggregate-id-copy cell)
- **Source:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Streams.razor`. `<button class="monospace grid-cell-truncate aggregate-id-copy">` inside a `FluentDataGrid` template column; carries `aria-label`, `data-testid="aggregate-id-copy"`, `title`, `@onclick:stopPropagation`/`@onkeydown:stopPropagation`; copies via JS interop with toast + clipboard-unavailable fallback. The button itself has **no** bespoke CSS (inherits `.monospace` + `.grid-cell-truncate`).
- **Allowlist:** same as C3 (`AdminUiFluentConformanceTests.cs:37`). **§4.1 row:** `architecture.md` (Admin.UI Streams row) — "grid-cell affordance; `FluentButton` breaks the cell layout."
- **Re-test:** does the pinned package offer an in-cell action affordance (e.g. a borderless/inline `FluentButton` appearance, or a copy primitive) that preserves `FluentDataGrid` row alignment? **Verdict:** _(dev)_

### Adjacencies to sweep (beyond the four named carve-outs)
- **Admin.UI `Pages/Index.razor`:** clickable `<div style="cursor:pointer" @onclick=…>` wrapping `StatCard` — slips the narrow `<(button|input|select|textarea)>` guard regex but is a non-semantic clickable (a11y concern). Record a verdict (likely REMEDIATE → semantic button/role, handoff to EventStore owner).
- **Admin.UI inline layout styles:** `Streams.razor` carries several inline `display:flex`/spacing styles on `<div>` — the exact class Tenants.UI's structural-and-style sweep removed. Feeds the governance-parity recommendation, not a control violation.
- **Any other raw control / de-semanticized landmark** discovered on the three surfaces during the sweep.

## Code Map (read-only unless noted)

**Carve-out source (READ — submodules, never edit from this repo):**
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor` (+ `.razor.cs`, `FcHomeDirectory.razor.css`).
- `references/Hexalith.FrontComposer/samples/Counter/Counter.Specimens/*.razor` and `references/Hexalith.FrontComposer/samples/Counter/Counter.Web/**` (confirm clean).
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/ActivityChart.razor`, `Pages/Streams.razor`, `Pages/Index.razor`.

**Governance + documentation (READ — submodules, never edit from this repo):**
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` (allowlist `["FcHomeCard.razor"]`; Counter.Web zero carve-outs; Specimens excluded).
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs` (allowlist `["ActivityChart.razor","Streams.razor"]`).
- `references/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md` §4.1 (lines 83-104 — UI component policy + carve-out table).
- `references/Hexalith.FrontComposer/_bmad-output/project-context.md` (Fluent-only UI rule, ~lines 131-136).

**Reference — Tenants conformance baseline (READ; in-repo):**
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (nine guards + the three structural/style guards added by the structural-and-style sweep; declares **no** carve-outs — the parity benchmark).

**Audit deliverables (WRITE — in-repo only):**
- This file's **Dev Agent Record** (carve-out verdicts + evidence + parity note), OR a companion `_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`.
- Owner handoff entries (FrontComposer owner: C1/C2 + Shell parity; EventStore owner: C3/C4 + Admin.UI parity/`Index.razor`). May reuse the proposal §5.5 handoff style.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — flip `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` → `done` only after the record + handoffs are complete.

## Tasks & Acceptance

**Execution:**
- [x] **Baseline & criteria.** Confirm the pinned Fluent v5 version on all three surfaces (`mcp__fluent-ui-blazor__check_project_version`; expect `5.0.0-rc.3-26138.1`). Restate the two-part decision test (no-equivalent AND fully-accessible) in the findings record. → Pin confirmed in both `Directory.Packages.props`; MCP build `5.0.0.26139` is one build newer (INCOMPATIBLE) — caveat recorded; criteria restated in findings §1.
- [x] **C1 audit — FcHomeCard.** Re-read source; confirm `<button role="link">` + nested `<h2>`/`<ul>` + keyboard activation; query the pinned package for a card-as-link equivalent; confirm source ↔ allowlist ↔ §4.1 agree; assign Verdict + evidence. → **REMEDIATE** after review verified pinned `FluentCard.OnClick`/`Role`/keyboard support (H-FC-1).
- [x] **C2 audit — Counter samples.** Confirm `Counter.Web` is Fluent-clean (no new raw controls); confirm `Counter.Specimens` is genuinely non-shipped (not routed/linked from a shipped surface); verify the fixture rationale; assign Verdict. → **KEEP** (routes Dev/Test+flag-gated; Counter.Web clean).
- [x] **C3 audit — ActivityChart.** Re-read source; confirm data-driven bar + aria + sr-only fallback; query the pinned package for a chart/meter/data-bar primitive; confirm allowlist + §4.1; assign Verdict. → **REMEDIATE** for accessibility proof gap (`role="img"` wrapping focusable buttons; H-ES-3).
- [x] **C4 audit — Streams copy cell.** Re-read source; confirm in-`FluentDataGrid` copy affordance + aria/data-testid/stopPropagation/clipboard fallback; query the pinned package for an in-cell action primitive; assign Verdict. → **KEEP** (no in-cell primitive; FluentButton breaks cell).
- [x] **Undocumented-drift sweep.** Scan all three surfaces for raw controls not in an allowlist and for non-semantic clickables/landmarks (incl. Admin.UI `Index.razor` clickable `<div>`); record each with a Verdict. → 0 undocumented raw controls; Shell+Counter.Web clean; Admin.UI **NS-1/NS-2** (REMEDIATE) + **DV-1** (REMEDIATE + DOC-DRIFT, StorageTreemap).
- [x] **Governance-parity recommendation.** Compare the three guards vs Tenants.UI's structural-HTML budget / inline-layout-style / component-CSS guards; record whether Shell + Admin.UI should adopt them (advisory only — no edits from this repo). → Tenants 12 guards vs Shell/Admin.UI 1; recommendation in findings §4.
- [x] **Findings record + handoffs.** Write the per-carve-out decision record (KEEP / REMEDIATE / DOC-DRIFT) with citations; file owner handoffs for every REMEDIATE / DOC-DRIFT / NEW-GUARD item, naming the exact submodule file(s). Make **no** edits under `references/Hexalith.FrontComposer/` or `references/Hexalith.EventStore/`. → `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`; handoffs H-FC-1..2 / H-ES-1..5; nested submodule worktrees verified unmodified; accepted review range includes the superproject FrontComposer gitlink move.
- [x] **Close.** Update sprint status to `done` only after the record + handoffs exist and are self-consistent. → Set to `review` per the `bmad-dev-story` Step 9 gate (audit deliverable routed for peer review before `done`); see Completion Notes.

**Acceptance Criteria:**
- Given each named carve-out (C1 FcHomeCard, C2 Counter.Specimens, C3 ActivityChart, C4 Streams), when the audit applies the two-part decision test against the pinned Fluent v5 package, then each has a recorded Verdict (KEEP re-justified / REMEDIATE / DOC-DRIFT) citing the no-equivalent result and the a11y result with `file:line` evidence.
- Given the three documentation records of each carve-out (source ↔ guard allowlist ↔ `architecture.md` §4.1 row), when the audit cross-checks them, then agreement is confirmed or any drift is recorded as a DOC-DRIFT finding with a handoff.
- Given the three surfaces, when the audit sweeps for undocumented raw controls and non-semantic clickables/landmarks (including Admin.UI `Index.razor`), then each is recorded with a Verdict and, where applicable, a handoff.
- Given the three surfaces' guards vs Tenants.UI's structural/style guards, when the audit compares them, then a governance-parity recommendation is recorded for the FrontComposer and EventStore owners.
- Given every REMEDIATE / DOC-DRIFT / NEW-GUARD finding, when the audit closes, then each has an owner handoff naming the exact submodule file(s), and **no** file under `references/Hexalith.FrontComposer/` or `references/Hexalith.EventStore/` was modified from the Tenants repo and no nested submodule was initialized.
- Given the findings record is complete and self-consistent, when sprint status is updated, then `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` is set to `done` (not before).

## Dev Notes

- **This is an audit, not a migration.** "Done" = a complete, evidence-backed decision record + handoffs. There is **no** Tenants source change and **no** new passing test required in this repo (the surface guards already exist in their submodules and stay green). Do not "fix" carve-outs here.
- **Submodule boundary is the #1 trap.** `Hexalith.FrontComposer` and `Hexalith.EventStore` are root-declared submodules under `references/`. Repo policy (CLAUDE.md + `project-context.md`): never modify submodule files without explicit human approval; never `--init --recursive`. The audit READS submodule source freely and WRITES only in-repo `_bmad-output/` artifacts. Every remediation is an owner handoff. If the human approves an in-place submodule fix, it is a separate task run via that submodule's own scoped `bmad-*` skills — not this story.
- **Authoritative carve-out list is in the FrontComposer submodule, not Tenants.** The Tenants `architecture.md` UI/Styling note (lines 251-260) has **no** §4.1; "§4.1" refers to `references/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md:83`. Cite the FrontComposer path.
- **Pinned package is RC.** `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` (no GA as of 2026-06; architecture.md:217, 251). Judge equivalence against **this** pin, not Fluent docs for a newer build. Use `mcp__fluent-ui-blazor__*` (`check_project_version`, `list_components`, `search_components`, `get_component_details`, `get_component_enums`) to confirm what the pinned package actually ships before declaring "no equivalent."
- **Counter has two surfaces — don't conflate them.** `Counter.Web` is a **shipped** sample app and is scanned with **zero** carve-outs (must stay Fluent-clean). Only `Counter.Specimens` is the excluded fixture tree. The proposal's shorthand "Counter samples" means the Specimens fixtures.
- **Each surface already self-governs.** Re-running a surface's guard is a read-only confirmation, not a requirement to change it. The three guards are deliberately **per-surface** (none scans across submodules) — do not propose a cross-submodule scanner from Tenants.
- **Decision-test framing matters.** The rule targets the *unstyled, a11y-stripped* raw control. A fully-styled, fully-accessible custom element with a genuine no-Fluent-equivalent reason is a legitimate KEEP — the audit's job is to *prove* both halves still hold, not to eliminate raw markup for its own sake.

### Project Structure Notes
- File named `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md` to match the four sibling `spec-frontcomposer-fluent-*-conformance-sweep.md` kernels; the sprint key keeps the `cc-` prefix (`cc-frontcomposer-shell-and-adminui-fluent-conformance-audit`).
- This is the 5th and final beat of the June 17-18 conformance series and the only one that crosses submodule boundaries (the prior four were all in-repo `Hexalith.Tenants.UI`).

### Previous Story Intelligence
- **`spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md` (ready-for-dev)** is the in-repo sibling; its three new guards (component-CSS layout/typography ownership, no inline layout `style=`, ratcheting `<div>`+`<span>` budget) are the **parity benchmark** this audit measures the other surfaces against.
- The three prior sweeps (`…fluent-v5-page-component…` done, `…fluent-control-and-css…` done, `…fluent-layout-page-layout…` in-progress) established the pattern: governance guard first, then evidence; all stayed inside Tenants.UI. This story breaks that pattern only by being read-only across submodules + handoff-based.
- Proposal §5.5 already routes the FrontComposer/AI.Tools `project-context.md` wording alignment as an owner handoff — reuse that channel for this audit's handoffs.

### Latest Tech Information
- Fluent v5 is mid-RC; primitives can appear/rename between RC builds. The `mcp__fluent-ui-blazor__*` server is version-aware (`check_project_version`/`get_version_info`) — prefer it over memory for "does a chart/card-link/in-cell-action primitive exist." A genuine new equivalent flips a KEEP to REMEDIATE; record the primitive name + the package build that introduced it.

## Verification

This story produces no Tenants source change, so there is no `dotnet test`/`git diff --check` gate **in this repo**. Verify the audit instead:

- Each of C1-C4 has a Verdict with `file:line` evidence and a pinned-package equivalence result in the findings record.
- Every REMEDIATE / DOC-DRIFT / NEW-GUARD finding has an owner handoff naming the exact submodule file(s).
- `git status` shows changes **only** under `_bmad-output/` (no submodule files touched); confirm submodules are unmodified:
  - `git -C Hexalith.FrontComposer status --porcelain` → empty.
  - `git -C Hexalith.EventStore status --porcelain` → empty.
- (Optional, read-only confirmation that the surface guards still pass — run inside each submodule, do not modify): the FrontComposer Shell and EventStore Admin.UI conformance test projects.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Amelia — `bmad-dev-story`), 2026-06-18.

### Audit Findings (C1-C4 verdicts, adjacencies, parity recommendation)

**Full evidence-backed record:** `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md` (in-repo).

**Baseline.** Pin `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` confirmed in both
submodules (`references/Hexalith.FrontComposer/Directory.Packages.props:46`, `references/Hexalith.EventStore/Directory.Packages.props:45`).
**Caveat:** the `mcp__fluent-ui-blazor` server documents build `5.0.0.26139` and reports
**INCOMPATIBLE** (one prerelease build newer) — equivalence verdicts judged conservatively against
the pin. Post-review verification used the exact local NuGet package for C1 and found
`FluentCard.OnClick`/`Role`/keyboard support already present in `5.0.0-rc.3-26138.1`.

**Carve-out verdicts:**
- **C1 `FcHomeCard` → REMEDIATE.** `<button role="link">`+`<h2>`+`<ul>`+Enter/Space keyboard
  (`FcHomeCard.razor:26-31`, `.razor.cs:51-60`), design-token CSS (`FcHomeDirectory.razor.css:43-52`).
  The current raw button is accessible, but no-equivalent fails because pinned `FluentCard` already
  supports arbitrary child content, `Role`, `OnClick`, `tabindex`, and Enter/Space activation. Source
  ↔ allowlist (`FluentConformanceTests.cs:36`) ↔ `architecture.md:102` agree with the old justification,
  so H-FC-1 remediates the component or updates the stale justification with new evidence.
- **C2 `Counter.Specimens` → KEEP.** Raw controls *are* the fixtures; routes runtime-gated to
  Dev/Test + config flag (`FrontComposerSpecimenRoutes.cs:25-31`, `Routes.razor:23-26`) — never shipped.
  `Counter.Web` Fluent-clean. Guard excludes the Specimens tree (scan root `Counter.Web`) ↔
  `architecture.md:103` agree.
- **C3 `ActivityChart` → REMEDIATE.** No chart/data-bar primitive in the pin (MCP `chart` → none),
  but the current accessible proof is incomplete because focusable bar `<button>` elements sit under
  a parent `role="img"` (`ActivityChart.razor:28,34-39`). H-ES-3 asks the EventStore owner to
  restructure the semantics or provide tested a11y evidence.
- **C4 `Streams` copy cell → KEEP.** No in-cell action primitive; `FluentButton` (incl.
  `Appearance="Transparent"`) breaks `.grid-cell-truncate` ellipsis + row alignment. Fully a11y
  (`Streams.razor:70-76`, status region `:40-43`, fallback `:362-378`). Allowlist `:37` ↔
  `architecture.md:105` agree.

**Drift sweep:** 0 undocumented raw controls (guards accurate). Shell + Counter.Web clean of
non-semantic clickables. Admin.UI: **NS-1** (REMEDIATE — `Index.razor:26,40` clickable `<div>`),
**NS-2** (REMEDIATE class — clickable link-styled `<span @onclick>` across `Commands`/`Events`/
`TypeCatalog`/`TypeDetailPanel`/`RelatedTypeList`/`DaprHealthHistory`), **DV-1** (REMEDIATE + DOC-DRIFT —
`StorageTreemap.razor` clickable `<g>`+sr-only `<table>`, undocumented data-viz analogue of
ActivityChart, but the clickable SVG group needs semantics before documentation alone is enough).
`<div @onclick:stopPropagation>` wrappers are benign (no own click target).

**Parity:** Tenants.UI `DomainUiFluentConformanceTests` = 12 guards, 0 carve-outs; Shell + Admin.UI =
1 guard (raw-controls) each. Recommend (advisory) Admin.UI adopt a non-semantic-clickable check +
inline-layout-style + div/span-budget + component-CSS-ownership guards (Admin.UI inline-`style=` is at
scale: DaprPubSub 57, DaprResiliency 45, …). Shell already clean → lower priority.

### Owner Handoffs

- **FrontComposer** (run via `Hexalith.FrontComposer:bmad-*`): **H-FC-1** (REMEDIATE) rework or
  re-justify C1 vs pinned `FluentCard.OnClick`/`Role`/keyboard support; **H-FC-2** (advisory) parity
  guards (low urgency, Shell clean).
- **EventStore** (run via `Hexalith.EventStore:bmad-*`): **H-ES-1** (REMEDIATE) `Index.razor:26,40`
  semantics; **H-ES-2** (REMEDIATE) NS-2 clickable-span class → `FluentLink`/`FluentButton`; **H-ES-3**
  (REMEDIATE) `ActivityChart` `role="img"`/button a11y proof; **H-ES-4** (REMEDIATE + DOC-DRIFT)
  `StorageTreemap` clickable SVG semantics + §4.1 documentation; **H-ES-5** (advisory) adopt parity
  guards + non-semantic-clickable check.
- **No** file under `references/Hexalith.FrontComposer/` or `references/Hexalith.EventStore/` was modified; handoffs are the
  deliverable.

### Completion Notes List

- Audit deliverable: evidence-backed decision record + handoffs. Code review accepted the broader
  bundled diff, so the reviewed range includes adjacent Tenants UI/DAPR/story-record changes and the
  superproject `Hexalith.FrontComposer` gitlink move; no nested FrontComposer/EventStore worktree files
  were edited from this repo.
- All 4 carve-outs now have explicit verdicts against the pinned package with `file:line` evidence:
  C2/C4 **KEEP**, C1/C3 **REMEDIATE**. The three documentation records (source ↔ guard allowlist ↔
  `architecture.md` §4.1) were checked for each; C1's justification is now stale because pinned
  `FluentCard` is a plausible equivalent, and C3's accessibility proof is incomplete.
- Undocumented-drift sweep found the spec-named `Index.razor` adjacency **plus** a wider Admin.UI
  non-semantic-clickable class (NS-2) and a second undocumented data-viz surface (DV-1, StorageTreemap)
  — each recorded with a verdict + owner handoff.
- **Verification:** `git -C Hexalith.FrontComposer status --porcelain` and `git -C Hexalith.EventStore
  status --porcelain` both **empty**; no nested submodule initialized. Superproject gitlink movement is
  acknowledged as part of the accepted bundled diff.
- **Status note:** initial dev workflow routed the completed audit to `review`; independent
  `bmad-code-review` resolved the decision and patch findings on 2026-06-18, leaving only deferred
  items, so the review workflow advanced the story to `done`.

### File List

- `_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md` (new — findings record + handoffs)
- `_bmad-output/implementation-artifacts/spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md` (modified — Tasks checkboxes, Dev Agent Record, Status)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — story status ready-for-dev → in-progress → review)

### Change Log

| Date | Change |
|------|--------|
| 2026-06-18 | Audit executed; C2/C4 KEEP, C1/C3 REMEDIATE; NS-1/NS-2/DV-1 drift + parity recommendation recorded; handoffs H-FC-1..2 / H-ES-1..5 filed; nested submodule worktrees verified unmodified; accepted bundled diff documented; code-review patches resolved; status → done. |

### Review Findings

- [x] [Review][Decision · RESOLVED: ACCEPT BUNDLED DIFF] Audit story scope is contaminated by adjacent changes — The accepted story is audit-only and says writes stay in `_bmad-output/`, but the baseline diff also includes Tenants UI/source/test/config/doc changes plus a `Hexalith.FrontComposer` gitlink move (`6edc855` → `f4910d7`). The audit record also says `submodules_modified: false` and "All changes confined to `_bmad-output/`", which is true only for nested submodule working trees, not for the superproject diff. Resolution 2026-06-18 (Administrator): accept the bundled diff and update the audit/story record to explicitly cover the broader set of changes. Evidence: `Hexalith.FrontComposer` gitlink hunk; `_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:14`; `_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:190`.
- [x] [Review][Patch] Update audit/story record to explicitly cover the accepted bundled diff [_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:190]
- [x] [Review][Patch] C1 `FcHomeCard` KEEP verdict is not proven against the pinned package [_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:65]
- [x] [Review][Patch] C3 `ActivityChart` accessibility is asserted without proving nested buttons remain accessible under `role="img"` [_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:113]
- [x] [Review][Patch] `StorageTreemap` clickable SVG is under-classified as doc drift instead of a remediation handoff [_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md:156]
- [x] [Review][Defer] Sibling structural-governance guard hardening gaps remain open [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:88] — deferred, pre-existing
- [x] [Review][Defer] Cross-aggregate timing guide still diagrams subscriber dead-lettering ambiguously [docs/cross-aggregate-timing.md:80] — deferred, pre-existing
- [x] [Review][Defer] Adjacent layout-story review/deferred records are internally stale or contradictory [_bmad-output/implementation-artifacts/deferred-work.md:15] — deferred, pre-existing

## Status

done
