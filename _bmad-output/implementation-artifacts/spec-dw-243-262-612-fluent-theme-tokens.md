---
title: 'Make tenant command-flow borders and focus follow the Fluent theme'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: 'a28e19330a3a97aaf48b4f77d8b66793e5d877a1'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-243/DW-262 and DW-612 record command-flow borders using obsolete Fluent/FAST tokens that fall back to system colors instead of following the active Fluent V5 theme. The same focus token survives in the configuration-set flow, and the conformance guard misses legacy custom-property names.

**Approach:** Replace the seven legacy usages in the existing member-removal, metadata-edit and configuration-set stylesheets with the established Fluent 2 border, destructive-border and focus roles. Preserve system-color fallbacks and forced-colors behavior, and extend the existing CSS conformance checks to reject reintroduced legacy tokens without exemptions. Reconcile only these three ledger records after verification.

</frozen-after-approval>

## Implementation Notes

- Clean entry on `main`, canonical revision `a28e19330a3a97aaf48b4f77d8b66793e5d877a1`. Intent gaps: none; UX guidance and sibling flows already establish the Fluent role mapping. Irreversibles: none. Footprint: three existing CSS files and `DomainUiFluentConformanceTests.cs`, plus this spec, validation evidence and the selected ledger/index records. No public API or dependency change.
- Independent read-only investigation confirms exactly seven usages. Reuse `--colorNeutralStroke1` (two borders), `--colorPaletteRedBorder2` (two destructive accents) and `--colorStrokeFocus2` (three outlines), already present in sibling correction/configuration flows and restored Fluent V5 theme assets. Preserve `CanvasText`/`Highlight` fallbacks, rule selectors, widths, media queries and documented layout exceptions.
- CSS paths: `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css`, `.../Metadata/EditTenantMetadataFlow.razor.css`, `.../Configuration/SetTenantConfigurationFlow.razor.css`. Guard path: `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`. Scan active CSS after removing comments; check both legacy references and declarations, including nested fallbacks, with positive/negative guard controls. Layout exception markers must not exempt legacy tokens.
- Verify a failing pre-migration guard, Debug source build, relevant conformance/flow tests and the individual UI project. Inspect actual computed CSS in Chromium using restored Fluent theme roles in light/dark and forced colors; record the isolated fixture's scope honestly. Independent blind review follows implementation. Preserve historical findings, unrelated ledger/index entries, archive/sprint and dependency pointers.
- Aspire baseline start succeeds. Initial resource inspection shows security starting with Tenants/UI waiting. Its final health and any live qualification limit will be recorded with exact commands.

- Pre-migration guard build passes with zero warnings/errors; the maintained single-method run fails as intended (exit 2), listing exactly the seven existing legacy usages. All seven replacements now preserve the original stylesheet bytes except token names. The guard scans component and wwwroot CSS without exemptions, strips comments, and has thirteen detection/allowance controls.
- After initialization, Aspire reports security, Tenants and Tenants UI Running/Healthy. This supersedes the initial startup wait; no topology fix was required.

- Initial post-migration Debug build has zero warnings/errors; focused UI run passes 375/375 with zero failures/skips. Independent review reports two guard gaps: common unprefixed FAST primitives and inline Razor CSS. Both are medium/patch: future styling could bypass the newly added guard. The existing scanner now also checks active Razor sources and recognizes the four named FAST primitives, with eight more rejection/comment controls. No additional production stylesheet or public API is changed.

## Review Triage Log

- medium / patch: `--fill-color`, `--body-font`, `--design-unit` and `--stroke-width` could bypass the new prefix guard despite the same obsolete-theme risk. Recognize these exact primitive names and add four rejection controls.
- medium / patch: an inline Fluent `Style` or `<style>` block could reintroduce legacy tokens outside CSS files. Scan active component Razor as well, using the shared matcher with Razor/HTML/CSS comment removal; add two active-inline and two commented-inline controls.

- Final reviewed Debug build passes with zero warnings/errors in 16.22s. The expanded focused run passes 382/383 but one unchanged held-authority retry case transiently displays its previous delivery message instead of the freshness refusal. The immediate maintained-method recheck passes all eight cases. This is retained as an observed validation limitation; no unrelated command behavior or test assertion was edited.
- Independent correction recheck accepts both guard fixes and finds no new concrete problem. No review finding is deferred.
- Playwright Chromium computed-style checks pass all seven properties against restored Fluent 5.0.1 roles in light and dark themes, then four borders and three keyboard-visible outlines against CanvasText/Highlight under forced colors. This is an isolated selector fixture using generated scoped CSS and restored Fluent assets, not an authenticated end-to-end workflow. The unchanged maintained browser runner also passes actual EN/FR removal rendering, measured viewport refusal, focus/recovery/forced-colors and its negative controls.
- Story-gitlink validation passes with no references/ pointer changes. Full individual UI execution is still pending; no ledger closure is claimed yet.

- Final full individual UI run passes 6159/6159, zero failures/skips, exit 0 in 1m51.830s; reviewed source hashes match. Only DW-243/DW-262/DW-612 close: 647 IDs, 351 done/296 open across 266 exact-covering rows. All 644 unrelated ledger bodies, every unrelated action row, archive/sprint hashes and dependency pointers remain intact. The current disposition table now explicitly includes earlier closed records; prior historical narratives are preserved. Exact commands, transient failure, browser scope and limits are in `dw-243-262-612-validation-evidence.md`; counters, role values and preservation checks are in its validation JSON. The skill-required local commit follows successful pinned commitlint validation.
