# Sprint Change Proposal — FrontComposer Fluent-Only Components, Layout & Styles

Date: 2026-06-18
Workflow: bmad-correct-course
Mode: Batch
Status: Draft — pending Administrator approval
Owner: Administrator (Jérôme Piquot)
Trigger: "FrontComposer should only use Blazor Fluent UI V5 components. HTML/CSS components should only be used if the needed component does not exist in Fluent UI. Use Fluent UI layout and styles. Use CSS only if required."

Decisions captured at navigation start:

- **Strictness:** Enforce Fluent for all visual/interactive controls, push **layout → Fluent primitives** and **styling → Fluent design tokens** (minimize CSS). **Keep** semantic landmarks (`<main>`/`<header>`/`<section>`/`<nav>`), description/bullet lists (`<dl>`/`<ul>`), and raw `<a>` nav links as the explicit "no Fluent equivalent" fallback, each recorded in a governance allowlist.
- **Scope:** Tenants.UI now; backlog a separate FrontComposer Shell + Counter samples + EventStore Admin.UI audit.
- **Mode:** Batch.

---

## 1. Issue Summary

The directive asks that module UI use **Blazor Fluent UI v5 + FrontComposer components only**, falling back to raw HTML/CSS/JS **only when no Fluent/FrontComposer equivalent exists**, and that **layout and styling come from Fluent primitives and design tokens** rather than bespoke CSS.

**This is not a new policy.** `Hexalith.AI.Tools/hexalith-ux-instructions.md` already states it verbatim:

> Module UI **must always** use the **FrontComposer** technical module and **Blazor Fluent UI V5** components. … Module UX **must avoid** using raw CSS, HTML tags, JavaScript … **when an equivalent component already exists** … Only fall back … when no such component exists.

What triggered the correction is an **enforcement gap**. The June 17–18 conformance sweeps already removed the *high-signal* offenders — and source scans confirm they are now zero:

| Already enforced (0 offenders today) | Guard |
|---|---|
| raw `<button>/<input>/<select>/<textarea>` | `Domain_ui_components_use_fluent_v5_only_with_no_raw_interactive_html_controls` |
| raw `<form>` | `Domain_ui_components_use_blazor_or_fluent_forms_with_no_raw_form_markup` |
| raw `<table>` family | `Domain_ui_components_use_fluent_grid_primitives_with_no_raw_table_markup` |
| page-root `<main>` / `<h1>` / `<PageTitle>` / page-root layout CSS | layout + page-header guards (5 Facts) |
| hard-coded semantic colors / native-control CSS selectors | `Domain_ui_component_css_does_not_own_semantic_colors_or_native_control_selectors` |

But **structural/typographic HTML and component-local styling CSS are still unguarded**, and a source scan of `src/Hexalith.Tenants.UI/Components/**/*.razor` shows they are widespread:

| Tag | Count | Tag | Count | Tag | Count |
|---|---|---|---|---|---|
| `<div>` | 138 | `<span>` | 107 | `<p>` | 116 |
| `<section>` | 56 | `<dt>`/`<dd>` | 52 / 52 | `<dl>` | 14 |
| `<header>` | 9 | `<nav>` | 5 | `<a>` | 5 |
| `<h2>`/`<h3>`/`<h4>` | 19 / 16 / 12 | `<ul>`/`<li>` | 1 / 1 | (`table`/`form`/controls) | 0 |

Plus **28 component-local `.razor.css` files** (several large: `GlobalAdministratorsPage` 201, `MemberAccessReview` 184, `TenantConfigurationView` 161, `ChangeTenantMemberRoleFlow` 130, `RemoveTenantConfigurationFlow` 126 lines) that still own spacing/typography/layout the directive wants expressed through Fluent.

So the gap is precisely: **layout-only `<div>` wrappers, text-styling `<span>`/`<p>`, and component CSS that owns layout/spacing/typography** — none of which the current governance catches.

---

## 2. Change Analysis Checklist

| Area | Status | Notes |
|---|---|---|
| Triggering issue | [x] | Tighten Fluent-only conformance to cover structural HTML + component styling, not just controls/tables/forms/page-layout. |
| Triggering story | [x] | Emerged during the active `cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep`; this directive is the next (4th) beat of that sweep series. |
| Core problem | [x] | Written UX policy (FrontComposer/Fluent-first, CSS only as fallback) is not yet enforced for structural HTML and component-local styling. |
| Evidence gathered | [x] | Source scan: controls/tables/forms = 0; `div` 138 / `span` 107 / `p` 116 / `section` 56 / `dl·dt·dd` 14·52·52 remain; 28 `.razor.css` files own spacing/typography/layout. |
| Epic impact | [N/A] | No FR, command, read-model, authorization, audit, or recovery behavior changes. Cross-cutting UI refactor only. |
| Story impact | [!] | New cross-cutting Tenants.UI story (the "now" work) + one backlog story (FrontComposer Shell/samples/Admin.UI). Active layout/header story closes unchanged. |
| PRD impact | [N/A] | PRD already mandates FrontComposer/Fluent composition; no product-scope change. |
| Architecture impact | [!] | No new shared API needed (FluentStack/FluentGrid/FcPageLayout/FcPageHeader/FluentLabel already exist). One sentence of `architecture.md` §4.1 guidance updated to name structural-HTML + styling conformance. |
| UI/UX impact | [x] | Visual output unchanged; internal markup/styling moves to Fluent primitives + tokens. a11y landmarks/nav preserved by explicit allowlist. |
| Test impact | [!] | Add structural-HTML allowlist/budget guard + component-CSS styling guard; add the a11y/nav fallback allowlist. |
| Submodule impact | [!] | `hexalith-ux-instructions.md` and FrontComposer `project-context.md` live in submodules — **no edits here**; wording-alignment is routed to the FrontComposer owner as a handoff. |
| Path forward | [x] | Direct Adjustment (new story + backlog story). Rollback/MVP-review not justified. |

---

## 3. Impact Analysis

**Epic impact.** Epics 1–5 remain valid and complete. The change refactors the *internal markup and styling* of pages those epics produced; it does not touch FRs, command lifecycle, projection reads, authorization, audit, correction, or support-safety.

**Story impact.**

- **New "now" story (Tenants.UI):** `cc-2026-06-18-frontcomposer-fluent-structural-and-style-conformance-sweep` — push layout-only `<div>`/`<span>` to `FluentStack`/`FluentGrid`, text-styling `<span>`/`<p>` to `FluentLabel`/Fluent typography, and reduce component `.razor.css` to Fluent design tokens + documented exceptions.
- **Backlog story (separate surfaces):** `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` — audit FrontComposer Shell, Counter samples, and EventStore Admin.UI, re-justifying the existing carve-outs (`FcHomeCard`, `Counter.Specimens`, Admin.UI `ActivityChart` bar, `Streams` aggregate-id-copy cell) against the "only if no Fluent equivalent" test.
- **Active story unchanged:** `cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep` is implementation-complete (all tasks `[x]`, page-header phase done, Tenants.UI 682/682); it returns to `review`/`done` on its own track. We do **not** reopen it again.

**Artifact conflicts.**

- The active layout spec's "Never add Tenants-owned … max-width infrastructure" and the control/CSS spec's "Do not replace all semantic HTML containers" both remain true and are **compatible** with this proposal: we are not replacing a11y containers, we are replacing *layout-only* wrappers and *styling* CSS.
- `hexalith-ux-instructions.md` already supports the directive — no conflict, no edit.

**Technical impact.**

- Mechanical, behavior-preserving refactor confined to `src/Hexalith.Tenants.UI/Components/**`.
- No package upgrade (stays on pinned `Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1`).
- No new shared FrontComposer API. If a genuinely missing Fluent/FrontComposer primitive is discovered (e.g. a definition-list equivalent), **stop and record it for the FrontComposer owner** — do not add generic infrastructure to Tenants (existing "FrontComposer boundary" edge-case rule).
- Risk surface: preserve `data-testid` selectors, localized copy, focus restoration, live regions, command lifecycle, audit/stale/degraded states, support-safe redaction. bUnit + governance suites are the safety net.

---

## 4. Recommended Approach

**Direct Adjustment**, split into one immediate Tenants.UI story and one backlog audit story.

Rationale:

- The directive restates already-approved UX policy; the work is to **close the enforcement gap**, not redesign anything.
- The "now" work needs only primitives that already exist, so it stays inside `Hexalith.Tenants.UI` (single repo) and is shippable on the current sweep cadence.
- Splitting off FrontComposer Shell/samples/Admin.UI keeps this correction shippable while honoring the literal "FrontComposer" naming through a tracked follow-up.

Rejected alternatives:

- **Maximal "replace nearly all raw HTML"** — rejected per your strictness choice: it would convert a11y landmarks/nav and risk regressing the `role="main"`/landmark work just completed.
- **Reaffirm-only (docs, no code)** — rejected: you chose to push structural layout + styling, so real migration + new guards are in scope.
- **Cram into the active layout/header story** — rejected: that story is complete and reviewed; reopening it a third time muddies its acceptance.
- **Edit the submodule policy docs here** — rejected: submodule-edit policy; routed as a handoff instead.

---

## 5. Detailed Change Proposals

### 5.1 New cross-cutting story (the "now" work) — Tenants.UI

Create `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md` with this kernel:

```markdown
## Intent
Problem: Tenants UI controls, tables, forms, page layout, and page headers now conform to
Fluent v5 / FrontComposer, but route pages and components still use layout-only <div>/<span>
wrappers, text-styling <span>/<p>, and 28 component-local .razor.css files that own
spacing/typography/layout. This violates the standing UX rule (FrontComposer/Fluent first;
raw HTML/CSS only when no equivalent exists; use Fluent layout and styles).
Approach: Replace layout-only wrappers with FluentStack/FluentGrid; replace text-styling
spans/paragraphs with FluentLabel/Fluent typography; reduce component .razor.css to Fluent
design tokens plus a documented exception allowlist. Keep a11y landmarks, lists, and nav
anchors as the documented "no Fluent equivalent" fallback. Add governance so the gap cannot
reopen.

## Boundaries & Constraints
Always: pinned Fluent v5 package + verified FrontComposer source; preserve routes, data-testid
selectors, localized copy, focus restoration, live regions, command lifecycle, audit/stale/
degraded states, support-safe redaction.
Keep (documented fallback — no Fluent v5 equivalent): semantic landmarks <main>/<header>/
<section>/<nav>, description lists <dl>/<dt>/<dd>, bullet lists <ul>/<li>, raw <a> nav links.
CSS exceptions allowed (record per file): @media (forced-colors)/forced-color-adjust, focus
outlines, overflow/scroll, and state visualization Fluent cannot express.
Never: add Tenants-owned shell/layout/breakpoint/provider infrastructure; convert a11y
landmarks or nav anchors to non-semantic markup; weaken any existing conformance guard;
add a new shared FrontComposer API (stop and hand off if a primitive is missing); touch
nested submodules.

## Tasks & Acceptance
- [ ] Add failing governance guards (5.3) first (red).
- [ ] Migrate layout-only <div>/<span> wrappers to FluentStack/FluentGrid across Components/**.
- [ ] Migrate text-styling <span>/<p> to FluentLabel / Fluent typography where they carry no
      semantic role; keep semantic/inline cases.
- [ ] Reduce each component .razor.css to Fluent design tokens (var(--*) spacing/typography)
      + documented exceptions; record exceptions in the allowlist with rationale.
- [ ] Record the kept a11y/nav HTML allowlist with rationale.
- [ ] Preserve selectors/behavior; update only assertions coupled to removed wrapper markup.
- [ ] Verify: dotnet test tests/Hexalith.Tenants.UI.Tests + git diff --check (green).
```

### 5.2 Backlog story (separate surfaces)

Add to sprint status as `ready-for-dev`/`backlog`: `cc-frontcomposer-shell-and-adminui-fluent-conformance-audit` —
audit FrontComposer Shell, `samples/Counter`, and EventStore Admin.UI; re-justify or remediate each documented carve-out (`FcHomeCard`, `Counter.Specimens` fixtures, Admin.UI `ActivityChart` bar, `Streams` aggregate-id-copy cell) under the "only when no Fluent equivalent exists" test. Crosses submodule boundaries → coordinate with FrontComposer/EventStore owners.

### 5.3 Governance tests — extend `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`

A perfect static "is this `<div>` layout-only?" check is impractical, so enforce the directive through three composable, testable proxies plus an explicit allowlist:

**(a) Component CSS must not own layout/spacing/typography** — extend the existing page-root CSS pattern beyond `Pages/*` to all component `.razor.css`, with a per-file documented-exception allowlist.

```csharp
// New: applies to every *.razor.css under Components/**, not just page-root selectors.
private static readonly Regex StylingOwnershipDeclaration = new(
    "\\b(display\\s*:\\s*(?:flex|grid|inline-flex|inline-grid)|gap\\s*:|grid-template|"
    + "margin(?:-inline|-block|-top|-right|-bottom|-left)?\\s*:\\s*(?!0)|"
    + "padding(?:-inline|-block|-top|-right|-bottom|-left)?\\s*:\\s*(?!0)|"
    + "font-size\\s*:|font-weight\\s*:|line-height\\s*:)",
    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

[Fact]
[Trait("Category", "Governance")]
public void Domain_ui_component_css_does_not_own_layout_spacing_or_typography()
{
    // Scan Components/**/*.razor.css. A file may opt a rule out only via an inline
    // "/* fc-css-exception: <reason> */" marker on the preceding line; collect those
    // and assert every exception carries a reason. Offenders = styling declarations
    // outside @media (forced-colors), :focus-visible, overflow, and marked exceptions.
}
```

**(b) Raw layout wrappers via inline style are forbidden** — push inline layout to Fluent primitives.

```csharp
private static readonly Regex InlineLayoutStyle = new(
    "<\\w+[^>]*\\bstyle=\"[^\"]*\\b(display\\s*:\\s*(?:flex|grid)|gap\\s*:|grid-template|"
    + "flex-direction\\s*:)[^\"]*\"",
    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
// Fact: no .razor under Components/** carries an inline layout style; use FluentStack/FluentGrid.
```

**(c) Structural-HTML allowlist + descending budget** — record the kept a11y/nav HTML and ratchet the layout-tag count down so new `<div>`/`<span>` layout wrappers cannot creep back.

```csharp
// Allowlisted raw structural/semantic tags (no Fluent v5 equivalent):
//   landmarks: header, section, nav   (main is shell-owned, already guarded)
//   lists: dl, dt, dd, ul, ol, li
//   inline/anchors: a (nav links)
// Fact: assert the allowlist is documented with a rationale string per tag, AND that the
// total <div>+<span> layout-wrapper count across Components/** does not EXCEED a recorded
// ceiling (set to the post-migration count). The ceiling is lowered as migration proceeds;
// it can never be raised without an explicit code-review note. This converts "minimize raw
// layout markup" into a regression-proof, ratcheting budget instead of a brittle zero-rule.
```

> Keep all existing guards (controls, forms, tables, accordion, page-layout modes, page-headers, page-root layout/CSS, semantic colors) unchanged — these new guards are additive.

### 5.4 Architecture guidance clarification (in-repo doc only)

`architecture.md` **UI / Styling** note (there is no "§4.1" in the Tenants architecture; that
reference belongs to FrontComposer's submodule architecture). Append to the UI/Styling note:

NEW (appended):
```markdown
UI uses FrontComposer or Fluent v5 components, never raw <button>/<input>/<select>/<textarea>,
and expresses page/section layout and spacing through Fluent layout primitives (FluentStack/
FluentGrid) and Fluent design tokens rather than component-local layout/typography CSS.
Raw semantic landmarks (<header>/<section>/<nav>), description/bullet lists, and <a> nav links
remain the documented fallback where Fluent v5 has no equivalent (governance allowlist).
```
Applied 2026-06-18.

### 5.5 Submodule doc alignment — handoff, not an edit here

`hexalith-ux-instructions.md` (Hexalith.AI.Tools) **already** states the rule; no change needed. The FrontComposer `project-context.md` "Fluent-only UI" bullet currently lists only controls as forbidden — propose extending its wording to name layout/styling-token conformance. Both files are in submodules: **route to the FrontComposer/AI.Tools owner**; do not edit from this repo.

### 5.6 Sprint status edits — `_bmad-output/implementation-artifacts/sprint-status.yaml`

```yaml
# Active layout/header sweep finishes on its own track (no further reopen):
cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep: review   # → done after its own verification

# New "now" Tenants.UI story (this proposal):
cc-2026-06-18-frontcomposer-fluent-structural-and-style-conformance-sweep: ready-for-dev

# Backlog audit for the other surfaces:
cc-frontcomposer-shell-and-adminui-fluent-conformance-audit: backlog
```

---

## 6. Implementation Handoff

**Scope classification: Moderate** — cross-cutting, behavior-preserving refactor + new governance over `Hexalith.Tenants.UI`, single repo, no new shared API. The backlog audit is separately scoped because it crosses submodule boundaries.

Execution sequence:

1. Approve this proposal; add the two new sprint-status keys (5.6).
2. Author the new spec story (5.1) and add the three governance guards (5.3) in the **red** phase first.
3. Migrate layout-only `<div>`/`<span>` → `FluentStack`/`FluentGrid`; text-styling `<span>`/`<p>` → `FluentLabel`/typography.
4. Reduce each of the 28 `.razor.css` files to Fluent design tokens + documented exceptions; lower the structural-tag budget to the achieved count.
5. Preserve selectors, localized copy, focus, live regions, command lifecycle, audit/stale/degraded states, support-safe redaction.
6. Apply the in-repo `architecture.md` §4.1 edit (5.4); file the submodule-doc handoff (5.5) and the backlog story (5.2).
7. Verify and only then move the new story to `done`.

Verification:

```bash
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore
git diff --check
```

Success criteria:

- Layout/spacing/typography in Tenants.UI is expressed through Fluent primitives + design tokens; component `.razor.css` retains only documented exceptions.
- Layout-only `<div>`/`<span>` wrappers and inline layout styles are gone; the structural-tag budget guard is green and ratcheted to the new floor.
- Kept a11y landmarks, lists, and `<a>` nav links are documented in the allowlist with rationale.
- All existing conformance/bUnit tests still pass; routes, selectors, copy, focus, command/audit/support-safe behavior unchanged.
- FrontComposer Shell/samples/Admin.UI audit is tracked as a backlog story; submodule-doc alignment routed to its owner.

Handoff recipients:

- **Developer agent** — implement the new Tenants.UI story + guards.
- **FrontComposer / AI.Tools owner** — submodule `project-context.md` wording alignment (5.5) and the backlog Shell/Admin.UI audit (5.2).
- **PO / sprint owner** — apply sprint-status keys (5.6).

---

## 7. Approval

Pending. On approval this is **Moderate** scope → routed to the Developer agent for the Tenants.UI story, with PO coordination for the backlog audit and submodule-doc handoff.
