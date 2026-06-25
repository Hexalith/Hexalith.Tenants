# Sprint Change Proposal — 2026-06-24 (FrontComposer accordion headers missing)

**Trigger:** "Il manque les titres sur les accordéons." The "Paramètres" (Settings) dialog renders its
accordion sections as bare chevrons with no visible section titles (screenshot supplied).
**Requested by:** Jérôme Piquot
**Workflow:** bmad-correct-course · **Mode:** Incremental
**Scope classification:** Minor (direct Developer-agent defect fix) — but spans the **`Hexalith.FrontComposer`
submodule** and its **source generator**, so it requires submodule-owner approval and an intentional
Verify-snapshot regeneration.
**Status:** Implemented & validated 2026-06-24 — **uncommitted** in the `Hexalith.FrontComposer` submodule
(no commit/push per request). SourceTools.Tests 1025/1025 + Shell.Tests 1958/1958 green.

---

## Section 1 — Issue Summary

The Shell **Settings dialog** (`FcSettingsDialog.razor`) groups its three sections (Density / Theme /
Preview) in a `FluentAccordion`. In the screenshot each `FluentAccordionItem` shows only its expand/collapse
chevron — **the section title is blank**. The "Preview" section is the one rendering the sample Orders grid +
Email field (the density-preview panel), which is why an Orders grid appears inside a Settings dialog.

**Root cause — FluentUI v4→v5 API rename not applied to FrontComposer's own code.** In
`Microsoft.FluentUI.AspNetCore.Components` **v5**, `FluentAccordionItem`'s plain-text heading parameter is
**`Header`** (string) — with `HeaderTemplate` (RenderFragment) for rich content. **There is no `Heading`
parameter** (verified against the v5 component API via the Fluent UI MCP). The pinned package is
`5.0.0-rc.3-26138.1`.

FrontComposer's components still use the **v4 name `Heading=`**. Because Fluent components splat unmatched
attributes onto the rendered element, `Heading="…"` is silently captured as an arbitrary HTML attribute and
**never rendered as the section title** — no compile error, no runtime error, just a blank header.

**Evidence:**
- v5 API: `FluentAccordionItem` exposes `Header` (string) + `HeaderTemplate` (RenderFragment); no `Heading`.
- Regression introduced by FrontComposer commit `20d2102` (2026-06-19): "convert sections in
  FcSettingsDialog and CounterPage to FluentAccordion" — replaced plain `<h3>` headings with
  `FluentAccordionItem Heading="…"`.
- Working reference: the **Tenants UI** uses the correct `Header=` on **15 `FluentAccordionItem`s across 6
  pages** (`TenantDetailPage`, `GlobalAdministratorsPage`, `UserMembershipLookupPage`, `TenantAuditPage`,
  `TenantConfigurationView`, `TenantsWorkspace`) and renders titles correctly — proving `Header` is the
  correct name on the pinned RC. The v4→v5 rename was applied to the consumer (Tenants) but never to
  FrontComposer itself.
- Localization keys (`DensitySectionLabel`, `ThemeSectionLabel`, `DensityPreviewHeading`) are present and
  resolve in both EN and FR `.resx` — so this is **not** a localization miss.

**Issue type:** Technical defect (UI rendering) discovered in manual use.

---

## Section 2 — Impact Analysis

| Artifact | Impact |
| --- | --- |
| **PRD** | None. No requirement, goal, or MVP scope change. |
| **Architecture** (`FrontComposer architecture.md` §4.2 "Page sections use FluentAccordion") | None to the decision. The accordion-grouping **intent is correct**; only the attribute **name** is wrong. |
| **Epics / Stories** | None added/removed/resequenced. Touches already-shipped Shell stories (3-3 settings dialog, the home directory) and the generator field-group story (4-5). |
| **UI/UX** | Visible defect: section titles missing. Minor a11y impact — sections lose their visible/programmatic name (FcSettingsDialog has `aria-label` fallbacks on inner controls; the generated forms and FcHomeDirectory do not). |
| **Source generator** (`ProjectionRoleBodyEmitter.cs`) | **Affected.** The grouped emit path emits `"Heading"`, so **every generated projection/detail form with `[ProjectionFieldGroup]`s** renders blank accordion headers. |
| **Verify snapshot** | `RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval.verified.txt` must be regenerated (intentional). |
| **Tests** | Generator unit tests (`RazorEmitterFieldGroupTests`) assert the wrong literal; `FcSettingsDialogTests` never checked header text (the gap that let this ship). No Governance guard exists for the v4 attribute name. |

### Blast radius (all inside the `Hexalith.FrontComposer` submodule)

**Hand-written (7 occurrences / 3 files):**
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor` — lines **28, 47, 50**
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor` — line **142**
- `samples/Counter/Counter.Web/Components/Pages/CounterPage.razor` — lines **20, 23, 26**

**Generated (1 emit site → many outputs):**
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` — line **292** (grouped path)

**Tests / guards:**
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterFieldGroupTests.cs` — lines **62, 82, 83**
- `tests/.../Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` — new guard
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs` — new heading assertions

**Not a bug (verified):** `FcAggregateListPage.razor:18 Heading="@Heading"` passes through to **`FcPageHeader`**'s
own legitimate `Heading` parameter — leave it. `HeadingLevel` (a valid v5 `FluentAccordionItem` param) is fine.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment.** Mechanically rename the v4 `Heading` to the v5 `Header`
everywhere FrontComposer authors or emits a `FluentAccordionItem` heading, regenerate the one affected
snapshot, fix the test assertions, **add a Governance guard** so the v4 name cannot silently return, and
**add a heading-render assertion** to the dialog test that should have caught this.

Rollback (Option 2) and MVP review (Option 3) are not applicable.

- **Effort:** Low · **Risk:** Low (mechanical rename; corrected name proven by 15 working Tenants items) ·
  **Timeline impact:** None.

---

## Section 4 — Detailed Change Proposals

### 4.A `.razor` — rename `Heading=` → `Header=` (3 files, 7 sites)

`FcSettingsDialog.razor`:
```razor
:28  <FluentAccordionItem Heading="@Localizer["DensitySectionLabel"].Value" Expanded="true">
     →  <FluentAccordionItem Header="@Localizer["DensitySectionLabel"].Value" Expanded="true">
:47  <FluentAccordionItem Id="fc-theme-section" Heading="@Localizer["ThemeSectionLabel"].Value">
     →  <FluentAccordionItem Id="fc-theme-section" Header="@Localizer["ThemeSectionLabel"].Value">
:50  <FluentAccordionItem Heading="@Localizer["DensityPreviewHeading"].Value">
     →  <FluentAccordionItem Header="@Localizer["DensityPreviewHeading"].Value">
```
`FcHomeDirectory.razor:142`:
```razor
<FluentAccordionItem Heading="@Localizer["HomeOtherAreasHeading"].Value" Expanded="false">
→  <FluentAccordionItem Header="@Localizer["HomeOtherAreasHeading"].Value" Expanded="false">
```
`CounterPage.razor:20/23/26`:
```razor
Heading="Compact inline (3 fields)"  →  Header="Compact inline (3 fields)"
Heading="Inline + popover (1 field)" →  Header="Inline + popover (1 field)"
Heading="Full-page command (5 fields)" → Header="Full-page command (5 fields)"
```
*Rationale:* `Header` is the v5 string heading param; matches the working Tenants UI usage.

### 4.B Source generator — `ProjectionRoleBodyEmitter.cs:292`
```csharp
b.AddAttribute(…, "Heading", headingExpression)  →  b.AddAttribute(…, "Header", headingExpression)
```
**Snapshot regeneration NOT needed (verified during implementation):** the only `.verified.txt` containing a
`FluentAccordionItem` (`RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval`) exercises the
**legacy single-group path** (emits `HeadingLevel` only, no header attribute), so it is unaffected. No
committed snapshot contained the buggy `"Heading",` literal. The grouped path is covered by string-assertion
emitter tests (§4.C), which were updated instead.
*Rationale:* fixes blank headers in all generated grouped projection/detail forms. The legacy single-group
path (lines 253-255) is unchanged (it emits no header text today — pre-existing, separate follow-up).

### 4.C Generator unit tests — `RazorEmitterFieldGroupTests.cs` + `RazorEmitterExpandInRowTests.cs`
```csharp
RazorEmitterFieldGroupTests.cs:62/82/83/86/87  "\"Heading\", \"Shipping\""/"…Billing…" → "\"Header\", …"
RazorEmitterExpandInRowTests.cs:74             "\"Heading\", \"Shipping\""             → "\"Header\", …"
```
*Rationale:* keep assertions (positive and negative, including the `IndexOf` ordering checks) bound to the
corrected emitted attribute. `RazorEmitterExpandInRowTests.cs:74` was **discovered during validation** (its
test failed first run) — a second emitter test asserting the same literal; both files now corrected.

### 4.D Regression guard — `FluentConformanceTests.cs` (Governance lane)
Add a `[Trait("Category","Governance")]` test that fails if either:
- any `src/**` or samples `.razor` contains `FluentAccordionItem` with a `Heading=` attribute, or
- the emitter source contains the `"Heading"` attribute literal for a `FluentAccordionItem`.

*Rationale:* the v4 name compiles and runs silently; only a source-scanning guard prevents recurrence.
Model it on the existing `FluentConformanceTests` source-scan guards.

### 4.E Dialog heading test — `FcSettingsDialogTests.cs`
Add bUnit assertions that the rendered dialog contains the three localized section titles
("Display density" / "Theme" / "Preview").
*Rationale:* closes the test gap that allowed the blank-header regression to ship.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor → **Developer agent**, direct implementation, executed **inside the `Hexalith.FrontComposer`
  submodule** (owner-approved per this proposal).
- **Validation — ACTUAL (2026-06-24):** The solution-level `Hexalith.FrontComposer.slnx` build **cannot run
  in this checkout**: FrontComposer's own nested submodules (EventStore/Tenants/Commons) are deinitialized per
  submodule policy, so the `.slnx` references missing project files (MSB3202). The affected projects reference
  only each other + `Contracts` + NuGet (none reference the nested submodules), so validation used per-project
  Release runs with `DiffEngine_Disabled=true` and the standard trait filter:
  - `Hexalith.FrontComposer.SourceTools.Tests` — **1025/1025 passed** (emitter fix + updated generator
    assertions).
  - `Hexalith.FrontComposer.Shell.Tests` — **1958/1958 passed** (the `.razor` renames compiled in `Shell` +
    `Counter.Web`; both new Governance guards pass; the new `FcSettingsDialogTests` heading test passes).
  - No `.verified.txt` change (see §4.B). When FrontComposer is built standalone with its submodules
    initialized, run the full solution lane as the canonical gate before release.
- **Success criteria:**
  1. The "Paramètres" dialog shows three titled sections (Display density / Theme / Preview) above each chevron.
  2. The Home "Other areas" accordion and the Counter sample sections show their titles.
  3. Generated grouped projection/detail forms render their field-group headers.
  4. A guard fails if `FluentAccordionItem … Heading=` reappears in source or in the emitter.
  5. Full FrontComposer default test lane green.
- **Submodule handling:** commit in `Hexalith.FrontComposer` (Conventional Commit, e.g.
  `fix(shell): use v5 FluentAccordionItem Header param so section titles render`); bump the Tenants gitlink
  to the new FrontComposer commit and verify it is reachable on the submodule remote before any Tenants push.
- **Out of scope (follow-ups):** the legacy single-group emitter path emitting no header text;
  `CounterPage.razor:19 HeadingLevel` placement on the parent `FluentAccordion`.
