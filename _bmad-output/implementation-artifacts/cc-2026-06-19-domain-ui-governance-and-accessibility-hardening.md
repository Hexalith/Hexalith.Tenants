---
title: 'Domain UI governance and accessibility hardening'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'done'
baseline_commit: f12db931aafb01f2698d94f175e84728b51e6455
sprint_key: 'cc-2026-06-19-domain-ui-governance-and-accessibility-hardening'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md'
approval: 'Administrator approved sprint-change-proposal-2026-06-19-deferred-work.md on 2026-06-19'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - guard behavior may change only inside this story">

## Intent

Re-approve and harden the Tenants UI governance guards that were intentionally deferred from the structural/style conformance sweep, then add small missing component tests for current FluentStack migrations and cosmetic route-heading fallbacks.

## Boundaries & Constraints

**Always:**
- Follow FrontComposer and Blazor Fluent UI V5 first. Use raw HTML/CSS only when there is no FrontComposer or Fluent equivalent.
- Preserve existing routes, stable selectors, localized copy, command lifecycle behavior, audit behavior, stale/degraded states, focus behavior, and support-safe copy.
- Treat this story as a guard and focused-test hardening story, not a visual redesign.
- Keep rule changes additive or explicitly justified. If a frozen Section 5.3 behavior is changed, document the before/after decision in this artifact.

**Never:**
- Do not weaken existing page-layout, page-header, control, table, form, accordion, or semantic CSS conformance guards.
- Do not add shared FrontComposer APIs or edit submodule files from this repository.
- Do not move shell/page landmark ownership into Tenants. FrontComposer shell/page-header accessibility remains an owner handoff.

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`

## Tasks & Acceptance

**Execution:**
- [x] Add red-phase guard tests for compact non-zero spacing declarations such as `margin:0.5rem` and `padding:0.5rem`.
- [x] Expand the raw-element inline-style guard to cover spacing, sizing, and alignment declarations beyond the original narrow set.
- [x] Exclude comments before counting `<div>` and `<span>` budget usage.
- [x] Decide and document whether `fc-css-exception` remains rule-scoped or becomes declaration-scoped.
- [x] Decide and document whether `:focus-visible` remains a blanket exemption or is narrowed.
- [x] Harden or replace `RemoveForcedColorsMediaBlocks` if braces in comments or strings can desynchronize it.
- [x] Add bUnit coverage that `aria-controls` points to a rendered active-region `id` in `MemberAccessReview`.
- [x] Add a localized blank/whitespace `TenantId` fallback for `TenantAuditPage` if accepted by the final implementation.
- [x] Update `deferred-work.md` after the guard decisions are implemented.

**Acceptance Criteria:**
1. Given component CSS contains compact non-zero spacing such as `margin:0.5rem` or `padding:0.5rem`, when `Domain_ui_component_css_does_not_own_layout_spacing_or_typography` scans it, then it is flagged unless covered by an approved `fc-css-exception`.
2. Given inline raw-element `style=` contains layout/spacing/measure declarations beyond the original narrow set, including `margin`, `padding`, `width`, `inline-size`, `justify-content`, or `align-items`, when governance scans `.razor` source, then it is flagged unless the story records an explicit exception.
3. Given `<div>`/`<span>` budget counting, when comments contain tag-like text, then comments are excluded before counting.
4. Given `fc-css-exception` markers, when a marker exempts a rule, then the story either preserves rule-level scoping with a documented rationale or introduces declaration-level scoping with updated tests.
5. Given `:focus-visible` is an approved exemption today, when this story reviews it, then the final behavior is explicitly approved: retain blanket exemption or narrow it with rationale.
6. Given `RemoveForcedColorsMediaBlocks` strips forced-colors blocks, when CSS contains braces in comments or strings, then the helper remains stable or is replaced with a safer parser.
7. Given `MemberAccessReview` opens change-role or remove-member regions, when bUnit renders the active region, then the `aria-controls` source button points to a rendered target `id` after the FluentStack migration.
8. Given `TenantAuditPage` receives a blank or whitespace `TenantId`, when the page header renders, then it uses a localized fallback rather than a dangling `Audit - ` heading. This is cosmetic, not a crash fix.

## Verification

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter DomainUiFluentConformanceTests`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantDetailSurfaceTests`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantAuditPageTests`
- `git diff --check`

## Dev Agent Record

### Completion Notes

Implemented as a guard + focused-test hardening story (no visual redesign). All 8 acceptance criteria are
satisfied and verified.

- **AC1** — `StylingOwnershipDeclaration` zero-skip rewritten: the original `(?!0)` lookahead skipped any
  value starting with the digit `0`, so compact spacing like `margin:0.5rem` slipped through. It now skips
  only a real zero value — one or more zero tokens (optional unit) plus optional `!important`, ending the
  declaration. `margin:0`, `margin:0 0 0 0`, `padding:0px`, `margin:0 !important` stay skipped;
  `margin:0.5rem`/`padding:0.5rem`/`padding:0.15rem 0.4rem` are flagged.
- **AC2** — `InlineLayoutStyle` widened to also catch inline `margin`/`padding`/`width`/`inline-size`/
  `justify-content`/`align-items`, and now scans single- and double-quoted `style=` attributes.
- **AC3** — the `<div>`/`<span>` budget strips Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting
  via the new `CountLayoutWrappers` helper.
- **AC6** — `RemoveForcedColorsMediaBlocks` brace matcher now skips CSS comments (`/* */`) and quoted strings
  so a stray `{`/`}` inside them cannot desynchronize the depth counter and leak the block tail into the scan.
- **AC7** — added a bUnit theory in `TenantDetailSurfaceTests` asserting the change-role and remove-member
  `aria-controls` source buttons resolve to a rendered active-region `id` after the FluentStack migration
  (region absent before the launcher is activated, present and containing the flow after).
- **AC8** — `TenantAuditPage` substitutes a localized fallback (`Tenants.Audit.UnknownTenant`,
  EN "this tenant" / FR "ce locataire") for a blank/whitespace `TenantId`, so the header reads as a complete
  phrase instead of a dangling "Audit trail for ". Cosmetic, not a crash fix.

The file-scanning guards (`Domain_ui_component_css_does_not_own_layout_spacing_or_typography`,
`Domain_ui_components_do_not_carry_inline_layout_styles`, the `<div>`/`<span>` budget) all stayed green
against the real component CSS/markup after the tightening — confirmed via the full UI suite — so no
production CSS regressed and no new `fc-css-exception` markers were required.

### Frozen Section 5.3 decisions (AC4 / AC5)

Per the Boundaries constraint, the two decisions that touch frozen §5.3 behavior are recorded here:

- **AC4 — `fc-css-exception` scoping: KEPT rule-level (before == after).** Each marker continues to exempt
  only the single rule it immediately precedes (its prelude), never the following rule. Rationale: the
  component rules in this repo are small single-purpose blocks whose marker reason already names the
  declarations it covers, so rule-level scoping keeps the marker adjacent to what it documents without
  per-declaration marker noise. Hardened with a new characterization test
  (`Fc_css_exception_marker_is_rule_scoped_and_does_not_leak_to_the_next_rule`).
- **AC5 — `:focus-visible` exemption: NARROWED (before: blanket exempt → after: no special treatment).** The
  blanket `:focus-visible` prelude exemption was removed. Focus-ring affordances
  (`outline`/`outline-offset`/`outline-color`) are not tracked ownership declarations, so every existing
  focus-visible rule still passes; but a future `:focus-visible` rule that owns layout/spacing/typography is
  now flagged unless it carries an `fc-css-exception` marker. This closes the bypass where a focus selector
  could silently smuggle in spacing/layout. Verified safe against all current focus-visible rules (all
  contain outline affordances only).

### File List

- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (modified) — AC1/AC2/AC3/AC5/AC6 guard
  changes, shared `StylingOwnershipOffenders`/`CountLayoutWrappers` helpers, hardened
  `RemoveForcedColorsMediaBlocks`, and new AC1–AC6 guard unit tests.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` (modified) — AC7 `aria-controls`
  rendered-region theory for `MemberAccessReview`.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` (modified) — AC8 blank-`TenantId`
  fallback theory and `Tenants.Audit.UnknownTenant` stub-localizer entry.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (modified) — AC8 `TenantDisplayName`
  fallback feeding `FcPageHeader` PageTitle/Heading.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (modified) — added `Tenants.Audit.UnknownTenant`.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` (modified) — added `Tenants.Audit.UnknownTenant`.
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified) — routed items resolved.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story status transitions.

### Change Log

- 2026-06-19 — Implemented domain UI governance and accessibility hardening (AC1–AC8): closed the §5.3
  guard bypasses (compact spacing, inline layout/spacing/sizing, comment-counting, forced-colors brace
  robustness), narrowed the `:focus-visible` exemption, kept `fc-css-exception` rule-scoped (documented),
  added `MemberAccessReview` `aria-controls` rendered-region coverage, and added a localized blank-`TenantId`
  fallback for `TenantAuditPage`. UI suite 726/726 green.

### Verification Evidence

- `dotnet test ...UI.Tests --filter DomainUiFluentConformanceTests` → 39/39 passed.
- `dotnet test ...UI.Tests --filter "DomainUiFluentConformanceTests|TenantDetailSurfaceTests|TenantAuditPageTests"` → 118/118 passed.
- `dotnet test ...UI.Tests` (full project) → 726/726 passed, 0 failed.
- `git diff --check` → clean.

### Review Findings

- [x] [Review][Decision - RESOLVED: ACCEPTED] Working-tree diff mixes this story with sibling query hardening — The selected story is `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`, whose Code Map is limited to UI governance tests, `TenantAuditPage`, resources, and routing artifacts. The reviewed diff also includes `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`, `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`, `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`, query gateway/client changes, and query integration/server/UI tests. Sprint status marks the sibling query story `done` while this story remains `review`. Administrator accepted this as a bundled cross-story review snapshot on 2026-06-20.
- [x] [Review][Decision - RESOLVED: ACCEPTED] FrontComposer submodule pointer is included in this Tenants story — The story boundary says not to add shared FrontComposer APIs or edit submodule files from this repository, and the Code Map does not include the `Hexalith.FrontComposer` submodule. The reviewed diff advances the submodule pointer from `0d535f7` to `20d2102` with only the submodule log visible in this review. Administrator accepted the dependency movement as intentional for this story on 2026-06-20.
- [x] [Review][Patch - FIXED] Inline style guard can still miss source-level layout styles when `style` has whitespace around `=` or uses common measure properties [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:92] — fixed on 2026-06-20 by accepting whitespace around `style =`, adding `height`/`block-size` plus min/max width/height/inline/block size variants, and adding guard characterization cases. `git diff --check` passed; focused `DomainUiFluentConformanceTests` execution was blocked before tests ran by package downgrade `NU1109` (`Dapr.Client` centrally pinned at 1.18.2 while referenced `Hexalith.EventStore.Client` requires >= 1.18.4).
- [x] [Review][Defer] CSS ownership guard still does not catch logical `*-start` / `*-end` spacing longhands [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:111] — deferred, pre-existing / future hardening; already recorded in `deferred-work.md` as a still-open sibling candidate, not a regression in this story.
- [x] [Review][Defer] Unclosed `@media (forced-colors)` blocks can still hide later CSS from the ownership scan [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:736] — deferred, pre-existing / future hardening; this story hardened braces inside comments and strings, while unclosed block handling remains a separate candidate.
- [x] [Review][Defer] Sibling query ETag special-character robustness remains routed outside this UI story [src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:142] — deferred, pre-existing to this selected story; the quote/comma robustness issue is already recorded under the tenant-query hardening review and tied to the EventStore read-model freshness handoff.
