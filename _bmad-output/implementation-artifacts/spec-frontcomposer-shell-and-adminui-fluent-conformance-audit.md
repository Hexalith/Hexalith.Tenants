---
title: 'FrontComposer Shell, Counter samples, and EventStore Admin.UI Fluent conformance audit'
type: 'audit'
created: '2026-06-18'
status: 'ready-for-dev'
sprint_key: 'cc-frontcomposer-shell-and-adminui-fluent-conformance-audit'
baseline_commit: '4ce8a84'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md (§5.2) on 2026-06-18'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md'
  - '{project-root}/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md'  # §4.1 UI component policy + carve-out table
  - '{project-root}/Hexalith.FrontComposer/_bmad-output/project-context.md'  # Fluent-only UI rule (lines ~131-136)
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

**Never:** Edit any file under `Hexalith.FrontComposer/` or `Hexalith.EventStore/` (source `.razor`/`.razor.css`, governance guards, or `architecture.md` §4.1) from the Tenants repo. Run `git submodule update --init --recursive` or initialize/modify nested submodules. Weaken or delete any existing conformance guard. Add UI infrastructure to Tenants. Redesign or restyle any surface. Treat the Counter **web app** (`Counter.Web` — shipped, scanned with zero carve-outs) as a fixture: only the `Counter.Specimens` tree is the excluded fixture surface.

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
- **Source:** `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor` (+ code-behind `FcHomeCard.razor.cs`; styling in `Components/Home/FcHomeDirectory.razor.css` — `.fc-home-card-button` is `background:transparent; border:0; display:block; width:100%; padding:calc(var(--design-unit)*3px)`).
- **Raw markup:** `<button type="button" role="link">` (custom keyboard activation) hosting `<h2>` title + projection `<ul>`/`<li>`/`<span>`. No inline styles; CSS is scoped + design-token based.
- **Allowlist:** `Hexalith.FrontComposer/tests/.../Governance/FluentConformanceTests.cs:36` → `carveOuts = ["FcHomeCard.razor"]` (test `Shell_components_use_fluent_v5_only_except_documented_carveouts`, line 29).
- **§4.1 row:** `architecture.md:102` — "framework chrome; `role="link"` + custom keyboard activation; hosts `<h2>` + projection `<ul>` a `FluentButton` cannot contain without regression."
- **Stated justification:** `FluentButton` cannot host nested `<h2>`+`<ul>` semantic content without visual/structural regression; the card needs custom keyboard activation the Razor compiler emits only on a real element. **Re-test:** does the pinned package offer a card-as-link primitive (`FluentCard` + `FluentAnchor` composition, or newer) that hosts heading + list without regression? **Verdict:** _(dev)_

### C2 — FrontComposer Counter samples · `Counter.Specimens`
- **Source:** `Hexalith.FrontComposer/samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor` (+ `FrontComposerDataFormattingSpecimen.razor` and the rest of the `Counter.Specimens/` tree). Raw `<button>/<input>/<label>/<form>/<table>` are the **content** (a11y/visual fixtures demonstrating unstyled fallback).
- **Allowlist:** `FluentConformanceTests.cs:43-47` — the guard scans `Counter.Web` (the **shipped** sample app) with **zero** carve-outs and **excludes the `Counter.Specimens` tree entirely**.
- **§4.1 row:** `architecture.md:103` — "the raw controls **are** the a11y/visual specimen fixtures; not a shipped UI page."
- **Re-test:** (1) Confirm `Counter.Web` is still Fluent-clean (guard passes, no new raw controls). (2) Confirm `Counter.Specimens` is genuinely non-shipped — not routed or linked from any shipped surface. If a specimen is reachable as a shipped route ⇒ REMEDIATE. **Verdict:** _(dev)_

### C3 — EventStore Admin.UI · `ActivityChart`
- **Source:** `Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/ActivityChart.razor`. `<button class="activity-chart-bar-wrapper">` wraps a height-scaled `<div class="activity-chart-bar" style="height:N%">` (data-driven bar). Carries `aria-label`, container `role="img"`, and a hidden `<table class="sr-only">` tabular fallback. CSS owns flex layout, hover/focus-visible ring, 200ms opacity transition (respects `prefers-reduced-motion`), responsive bar-hiding, forced-colors swap.
- **Allowlist:** `Hexalith.EventStore/tests/.../Governance/AdminUiFluentConformanceTests.cs:37` → `carveOuts = ["ActivityChart.razor", "Streams.razor"]` (test `AdminUi_components_use_fluent_v5_only_except_documented_carveouts`, line 28).
- **§4.1 row:** `architecture.md:104` — "data-visualization element (height-scaled `<div>`); `aria-label` present; `FluentButton` destroys the bar."
- **Re-test:** does the pinned package offer a chart/meter/data-bar primitive that preserves the data-driven height? Confirm the sr-only table fallback + aria still present. **Verdict:** _(dev)_

### C4 — EventStore Admin.UI · `Streams` (aggregate-id-copy cell)
- **Source:** `Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Streams.razor`. `<button class="monospace grid-cell-truncate aggregate-id-copy">` inside a `FluentDataGrid` template column; carries `aria-label`, `data-testid="aggregate-id-copy"`, `title`, `@onclick:stopPropagation`/`@onkeydown:stopPropagation`; copies via JS interop with toast + clipboard-unavailable fallback. The button itself has **no** bespoke CSS (inherits `.monospace` + `.grid-cell-truncate`).
- **Allowlist:** same as C3 (`AdminUiFluentConformanceTests.cs:37`). **§4.1 row:** `architecture.md` (Admin.UI Streams row) — "grid-cell affordance; `FluentButton` breaks the cell layout."
- **Re-test:** does the pinned package offer an in-cell action affordance (e.g. a borderless/inline `FluentButton` appearance, or a copy primitive) that preserves `FluentDataGrid` row alignment? **Verdict:** _(dev)_

### Adjacencies to sweep (beyond the four named carve-outs)
- **Admin.UI `Pages/Index.razor`:** clickable `<div style="cursor:pointer" @onclick=…>` wrapping `StatCard` — slips the narrow `<(button|input|select|textarea)>` guard regex but is a non-semantic clickable (a11y concern). Record a verdict (likely REMEDIATE → semantic button/role, handoff to EventStore owner).
- **Admin.UI inline layout styles:** `Streams.razor` carries several inline `display:flex`/spacing styles on `<div>` — the exact class Tenants.UI's structural-and-style sweep removed. Feeds the governance-parity recommendation, not a control violation.
- **Any other raw control / de-semanticized landmark** discovered on the three surfaces during the sweep.

## Code Map (read-only unless noted)

**Carve-out source (READ — submodules, never edit from this repo):**
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor` (+ `.razor.cs`, `FcHomeDirectory.razor.css`).
- `Hexalith.FrontComposer/samples/Counter/Counter.Specimens/*.razor` and `Hexalith.FrontComposer/samples/Counter/Counter.Web/**` (confirm clean).
- `Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/ActivityChart.razor`, `Pages/Streams.razor`, `Pages/Index.razor`.

**Governance + documentation (READ — submodules, never edit from this repo):**
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` (allowlist `["FcHomeCard.razor"]`; Counter.Web zero carve-outs; Specimens excluded).
- `Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs` (allowlist `["ActivityChart.razor","Streams.razor"]`).
- `Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md` §4.1 (lines 83-104 — UI component policy + carve-out table).
- `Hexalith.FrontComposer/_bmad-output/project-context.md` (Fluent-only UI rule, ~lines 131-136).

**Reference — Tenants conformance baseline (READ; in-repo):**
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (nine guards + the three structural/style guards added by the structural-and-style sweep; declares **no** carve-outs — the parity benchmark).

**Audit deliverables (WRITE — in-repo only):**
- This file's **Dev Agent Record** (carve-out verdicts + evidence + parity note), OR a companion `_bmad-output/implementation-artifacts/audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`.
- Owner handoff entries (FrontComposer owner: C1/C2 + Shell parity; EventStore owner: C3/C4 + Admin.UI parity/`Index.razor`). May reuse the proposal §5.5 handoff style.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — flip `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` → `done` only after the record + handoffs are complete.

## Tasks & Acceptance

**Execution:**
- [ ] **Baseline & criteria.** Confirm the pinned Fluent v5 version on all three surfaces (`mcp__fluent-ui-blazor__check_project_version`; expect `5.0.0-rc.3-26138.1`). Restate the two-part decision test (no-equivalent AND fully-accessible) in the findings record.
- [ ] **C1 audit — FcHomeCard.** Re-read source; confirm `<button role="link">` + nested `<h2>`/`<ul>` + keyboard activation; query the pinned package for a card-as-link equivalent; confirm source ↔ allowlist ↔ §4.1 agree; assign Verdict + evidence.
- [ ] **C2 audit — Counter samples.** Confirm `Counter.Web` is Fluent-clean (no new raw controls); confirm `Counter.Specimens` is genuinely non-shipped (not routed/linked from a shipped surface); verify the fixture rationale; assign Verdict.
- [ ] **C3 audit — ActivityChart.** Re-read source; confirm data-driven bar + aria + sr-only fallback; query the pinned package for a chart/meter/data-bar primitive; confirm allowlist + §4.1; assign Verdict.
- [ ] **C4 audit — Streams copy cell.** Re-read source; confirm in-`FluentDataGrid` copy affordance + aria/data-testid/stopPropagation/clipboard fallback; query the pinned package for an in-cell action primitive; assign Verdict.
- [ ] **Undocumented-drift sweep.** Scan all three surfaces for raw controls not in an allowlist and for non-semantic clickables/landmarks (incl. Admin.UI `Index.razor` clickable `<div>`); record each with a Verdict.
- [ ] **Governance-parity recommendation.** Compare the three guards vs Tenants.UI's structural-HTML budget / inline-layout-style / component-CSS guards; record whether Shell + Admin.UI should adopt them (advisory only — no edits from this repo).
- [ ] **Findings record + handoffs.** Write the per-carve-out decision record (KEEP / REMEDIATE / DOC-DRIFT) with citations; file owner handoffs for every REMEDIATE / DOC-DRIFT / NEW-GUARD item, naming the exact submodule file(s). Make **no** edits under `Hexalith.FrontComposer/` or `Hexalith.EventStore/`.
- [ ] **Close.** Update sprint status to `done` only after the record + handoffs exist and are self-consistent.

**Acceptance Criteria:**
- Given each named carve-out (C1 FcHomeCard, C2 Counter.Specimens, C3 ActivityChart, C4 Streams), when the audit applies the two-part decision test against the pinned Fluent v5 package, then each has a recorded Verdict (KEEP re-justified / REMEDIATE / DOC-DRIFT) citing the no-equivalent result and the a11y result with `file:line` evidence.
- Given the three documentation records of each carve-out (source ↔ guard allowlist ↔ `architecture.md` §4.1 row), when the audit cross-checks them, then agreement is confirmed or any drift is recorded as a DOC-DRIFT finding with a handoff.
- Given the three surfaces, when the audit sweeps for undocumented raw controls and non-semantic clickables/landmarks (including Admin.UI `Index.razor`), then each is recorded with a Verdict and, where applicable, a handoff.
- Given the three surfaces' guards vs Tenants.UI's structural/style guards, when the audit compares them, then a governance-parity recommendation is recorded for the FrontComposer and EventStore owners.
- Given every REMEDIATE / DOC-DRIFT / NEW-GUARD finding, when the audit closes, then each has an owner handoff naming the exact submodule file(s), and **no** file under `Hexalith.FrontComposer/` or `Hexalith.EventStore/` was modified from the Tenants repo and no nested submodule was initialized.
- Given the findings record is complete and self-consistent, when sprint status is updated, then `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` is set to `done` (not before).

## Dev Notes

- **This is an audit, not a migration.** "Done" = a complete, evidence-backed decision record + handoffs. There is **no** Tenants source change and **no** new passing test required in this repo (the surface guards already exist in their submodules and stay green). Do not "fix" carve-outs here.
- **Submodule boundary is the #1 trap.** `Hexalith.FrontComposer` and `Hexalith.EventStore` are root-level submodules. Repo policy (CLAUDE.md + `project-context.md`): never modify submodule files without explicit human approval; never `--init --recursive`. The audit READS submodule source freely and WRITES only in-repo `_bmad-output/` artifacts. Every remediation is an owner handoff. If the human approves an in-place submodule fix, it is a separate task run via that submodule's own scoped `bmad-*` skills — not this story.
- **Authoritative carve-out list is in the FrontComposer submodule, not Tenants.** The Tenants `architecture.md` UI/Styling note (lines 251-260) has **no** §4.1; "§4.1" refers to `Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md:83`. Cite the FrontComposer path.
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

### Audit Findings (C1-C4 verdicts, adjacencies, parity recommendation)

### Owner Handoffs

### Completion Notes List

### File List

## Status

ready-for-dev
