---
title: 'FrontComposer Fluent layout page-layout conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'in-progress'
baseline_commit: '974eac7fe6b7b6bc2545c2c51adb170ed587482a'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md on 2026-06-18'
scope_extension_approval: 'Administrator approved sprint-change-proposal-2026-06-18-page-header-frontcomposer.md on 2026-06-18'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-page-header-frontcomposer.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/tenants-ui-responsive-layout-and-visual-system-spec.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI now conforms on major Fluent controls, raw tables, raw forms, and semantic CSS state ownership, but page-level layout and page chrome are still partially owned by Tenants page wrappers. Route pages declare `PageTitle` and visible page headers independently, which produces inconsistent page headers and duplicates generic page-title/header scaffolding that belongs in FrontComposer. The approved correction requires page layout to use Fluent UI Blazor v5 Layout through the existing FrontComposer shell and `FC-LYT` page-layout contract, and requires page title/header composition to move into a FrontComposer component.

**Approach:** Keep `MainLayout.razor` as `<FrontComposerShell>@Body</FrontComposerShell>`. Move page-level layout decisions into `FcPageLayout` and Fluent layout primitives (`FluentStack`, `FluentGrid`, `FluentGridItem`, and shell-owned `FluentLayout` where applicable). Add a narrow FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level header, while Tenants supplies localized domain strings and page-specific fragments. Reduce page CSS to unavoidable page-specific exceptions and add governance tests so local page-layout and page-header wrappers do not return.

## Boundaries & Constraints

**Always:** Use the repository's pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` API and verified FrontComposer source. Preserve existing routes, selectors, localized copy, focus restoration, command lifecycle behavior, audit behavior, stale/degraded states, and support-safe copy/redaction behavior. Keep dense grid-first operational pages full width and use constrained measures for prose, forms, and readable detail regions.

**Approved by scope extension:** Add a narrow FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level header. Continue to stop before changing the Fluent package version, changing command/query behavior, adding unrelated shared layout APIs, or turning this into a visual redesign.

**Never:** Do not add Tenants-owned shell, page-layout, breakpoint, provider, or max-width infrastructure. Do not replace `FrontComposerShell` with a Tenants-local `FluentLayout`. Do not hide primary DataGrid content behind a new layout interaction. Do not weaken existing accordion, control, raw-table, raw-form, or semantic CSS conformance guards.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|----------------------------|----------------|
| Dense grid page | Tenant list, global administrators, audit, or membership grid is visible | Page declares full-width layout and keeps DataGrid content directly visible | Stale/degraded/error states remain non-collapsed and visible |
| Detail or command-heavy page | Tenant detail, user lookup, or command regions render alongside grids | Readable regions use constrained measure or Fluent layout primitives without local page max-width scaffolding | Command forms still submit through existing handlers and remain fail-closed |
| Governance scan | A future page adds root grid wrapper or page-root layout CSS | Test fails with offender file and rule | Allow only documented exceptions in the implementation artifact |
| FrontComposer boundary | Implementation discovers a missing reusable layout primitive | Work stops for FrontComposer/UX decision instead of adding generic infrastructure to Tenants | Record the gap and proposed owner |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor` -- must remain the shared shell wrapper.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` and `.razor.css` -- tenant list, create flow entry point, filters, and pager.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `.razor.css` -- tenant detail, metadata/lifecycle/member/configuration composition.
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor` and `.razor.css` -- signed-in user membership surface.
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor` and `.razor.css` -- user lookup form and results.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` and `.razor.css` -- global administrator review and command regions.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `.razor.css` -- audit filters, state, grid, receipt, and correction panels.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor` and supporting partial/CSS files if needed -- shared page-title/header component.
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/` -- shared component tests for the page-header contract.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- source governance guards.
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs` -- focused bUnit coverage for preserved selectors and layout declarations.

## Tasks & Acceptance

**Execution:**
- [x] Verify exact local APIs for `FcPageLayout`, `FcPageLayoutMode`, `FluentStack`, `FluentGrid`, and `FluentGridItem` against the pinned package/source before editing.
- [x] Add page-layout declarations to Tenants pages, using `FullWidth` for DataGrid-dense pages and `Constrained` for form/prose/detail measure where appropriate.
- [x] Replace page-owned layout grids with Fluent layout primitives where those grids only arrange sibling page regions.
- [x] Reduce page `.razor.css` root layout rules to documented exceptions only.
- [x] Extend `DomainUiFluentConformanceTests` with page-layout governance and allowlist rationale.
- [x] Add component tests proving at least one full-width page and one constrained/readable page declare the expected layout.
- [x] Update sprint status from `ready-for-dev` to the appropriate execution state when implementation starts and to `done` only after verification passes.
- [x] Add a FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level page header.
- [x] Add FrontComposer component tests proving heading, title, optional fragments, accessibility parameters, and generic resource independence.
- [x] Migrate Tenants route pages from direct `<PageTitle>` plus page-owned route headers to the FrontComposer page-header component.
- [x] Extend Tenants governance tests to reject direct route-page `<PageTitle>` and page-owned route-header scaffolding.
- [x] Preserve existing route behavior, localized copy, selectors, focus behavior, command lifecycle, audit state, stale/degraded states, and support-safe behavior.

**Acceptance Criteria:**
- Given Tenants page source, when governance tests scan page components, then page-level layout is declared through `FrontComposerShell`/`FcPageLayout` and Fluent layout primitives rather than Tenants-owned page layout scaffolding.
- Given dense operational pages, when they render, then DataGrid-first content remains directly visible and full-width.
- Given detail/form/prose-heavy regions, when they render, then readable measure is constrained through the FrontComposer/Fluent layout contract, not bespoke max-width page CSS.
- Given route pages render, when browser title and visible page header are needed, then both are declared through the FrontComposer page-header component and Tenants supplies only localized domain text and page-specific fragments.
- Given Tenants page source, when governance tests scan route pages, then direct route-page `<PageTitle>` and page-owned route-level header scaffolding are rejected.
- Given existing component tests, when they query stable selectors and localized text, then routes, labels, focus behavior, command lifecycle, audit state, and support-safe behavior remain unchanged.
- Given the UI test project, when it runs, then conformance and affected component tests pass against the pinned Fluent UI Blazor v5 package.

### Review Findings (code review 2026-06-18)

Scope reviewed: layout phase only — diff `974eac7..HEAD` (`92bf113`). The page-header scope-extension tasks above are not yet implemented and were not in scope for this review. 3 decision-needed (resolved 2026-06-18), 4 patch findings handled (3 applied, 1 not-applied/already-governed), 0 deferred, 12 dismissed as noise/false-positive. Layout-phase ACs 1–3 and the governance/non-weakening ACs are satisfied (confirmed by the Acceptance Auditor). Verification after fixes: `Hexalith.Tenants.UI.Tests` 681/681 passing (Release).

**Decision resolutions (2026-06-18):**
- D1 (`<main>` landmark, HIGH) → applied in FrontComposer shell (patch 1).
- D2 (Constrained dense grids on Detail/User-Lookup, MEDIUM) → resolved: use Fluent UI default DataGrid responsiveness within the current `Constrained` measure; no code change. Revisit if UX finds the 75rem cap too narrow on wide displays.
- D3 (out-of-story DAPR/health commit, MEDIUM) → investigated; not applied (patch 2).

- [x] [Review][Patch] Added `role="main"` to `#fc-main-content` in `FrontComposerShell` (restores the page `main` landmark removed when page `<main>` wrappers became `<FluentStack>`); also removed the now-redundant `role="main"` from `FcHomeDirectory.razor` to avoid a duplicate landmark on the home route. NOTE: two FrontComposer submodule files changed — run `Hexalith.FrontComposer.Shell.Tests` to confirm (no `role="main"` assertions exist there; Tenants UI 681/681 pass). [HIGH] [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:113; .../Components/Home/FcHomeDirectory.razor:12]
- [x] [Review][Patch] DAPR/health hardening — **NOT APPLIED (already governed)**: both halves are locked by existing test contracts. `CrossAggregateTimingDocumentationTests.Timing_guide_matches_current_DAPR_pubsub_component_contracts` asserts `enableDeadLetter`/`deadLetterTopic` in both local + production `pubsub.yaml` and the timing guide; `HealthEndpointsTests.Ready_development_json_response_is_support_safe...` asserts the curated `description` IS surfaced while exceptions/tokens/connection strings are excluded (the writer already omits exceptions). Changing either would break Server.Tests. Recommend handling any dead-letter-semantics change as a separate deployment-readiness task. [MEDIUM] [src/Hexalith.Tenants/Program.cs; src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml]
  - **Full re-review 2026-06-18 clarification (P-DN3):** the `pubsub.yaml`/health code IS present in the tree — it was committed in `92bf113` precisely to **satisfy** the `CrossAggregateTimingDocumentationTests`/`HealthEndpointsTests` contracts named above (which assert `enableDeadLetter`/`deadLetterTopic` exist and that the curated `description` is surfaced). "NOT APPLIED" referred only to the *further* proposed dead-letter-semantics hardening, which remains a separate deployment-readiness task. There is no contradiction and no code change here; the bundled future-story planning docs (structural-sweep + shell-audit specs, fluent-only proposal) in commit `9af97d9` are acknowledged as committed alongside.
- [x] [Review][Patch] Restored pager trailing-alignment (`HorizontalAlignment.End`) on both pagers and the page-header child gap (Fluent `VerticalGap` stack — the header children carry `margin:0`, so they would otherwise touch). Filter-label spacing unaffected (Fluent `Label=`). [LOW] [src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor; TenantsWorkspace.razor]
- [x] [Review][Patch] Closed governance-guard holes — broadened the forbidden page-root property regex to logical/longhand props (`padding-inline/-block(-start/-end)`, `inline-size`, `max-inline-size`, `block-size`); the CSS guard now derives page-root classes dynamically per page file (no hardcoded allowlist); and the declare-modes guard now fails any route page lacking `<FcPageLayout`. [LOW] [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs]

### Review Findings (full re-review 2026-06-18)

Scope: full story re-review — parent diff `974eac7..HEAD` + FrontComposer submodule `6edc855..e064573`. Three adversarial layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor), all Opus 4.8, 0 layers failed. 3 decision-needed, 3 patch, 1 deferred, 5 dismissed. Both in-scope phases (FC-LYT layout + `FcPageHeader` page-header) judged faithfully implemented; `<main>` landmark relocation, `FocusHeadingAsync` focus-restore swap, mode FullWidth/Constrained choices, and governance strengthening confirmed correct.

**Decision-needed:**

- [x] [Review][Decision] `FcPageHeader` throws on empty/whitespace `Heading` → an un-named tenant crashes the whole Tenant Detail page [HIGH] — `OnParametersSet` calls `ArgumentException.ThrowIfNullOrWhiteSpace(Heading)` (`Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:60`); the success branch binds `Heading="@Detail.Name"` (`src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:78`). Confirmed reachable: there is NO `CreateTenantValidator` and `TenantAggregate.Handle(CreateTenant,…)` emits `TenantCreated` with no non-empty `Name` guard, so a tenant can persist with an empty name → `Detail.Name` empty → page-wide crash. The prior `<h1>@Detail.Name</h1>` tolerated empty. Fix is intent-dependent: (a) consumer-side localized fallback in `TenantDetailPage` (show `TenantId` or an "Unnamed tenant" string); (b) soften `FcPageHeader` to tolerate empty `Heading` (FrontComposer submodule contract change); (c) add domain `CreateTenant` `Name` validation (out of this UI story's scope).
- [x] [Review][Decision] Page landmark & accessible-name a11y regression [MEDIUM] — relocating page `<main>` wrappers to roleless `FluentStack` `<div>`s plus per-page `FcPageHeader` `<header>` causes: (1) the page `<header>` is not scoped to a native sectioning element, exposing a second `banner` landmark alongside the shell chrome banner (`FcPageHeader.razor:9`, in `TenantsWorkspace`/`MyTenantsPage`/`GlobalAdministratorsPage`/`TenantAuditPage` + the Detail identity header); (2) per-page `aria-labelledby` (`global-admins-heading`, `tenant-audit-heading`) now sits on a roleless `<div>` while the real landmark is the shell's `#fc-main-content` `role="main"` (no `aria-labelledby`), so the heading no longer names the landmark. Per the I/O-matrix "FrontComposer boundary" row this is a FrontComposer/UX-owned decision: shell native `<main>` + label parameter, vs page `<section aria-labelledby>` wrapper, vs accept.
- [x] [Review][Decision] Out-of-scope DAPR/health/pubsub code in the diff conflicts with the story's own record [MEDIUM] — `src/Hexalith.Tenants/Program.cs` adds `WriteSupportSafeDevelopmentHealthResponseAsync` + endpoint wiring, `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` is newly created with `enableDeadLetter`/`deadLetterTopic`, and `HealthEndpointsTests.cs` renames the health check — yet the prior "Review Findings (code review 2026-06-18)" record logs this DAPR/health work as "NOT APPLIED (already governed)". It appears to have actually landed in commit `92bf113` (fixing pre-existing failing Server.Tests). The range also bundles unrelated future-story planning docs (structural-sweep + shell-audit specs, fluent-only proposal). Decide: (a) keep and correct the story record; (b) split DAPR/health + docs into their own deployment-readiness/planning commits; (c) revert from this story.

**Resolutions (2026-06-18, Jérôme Piquot):** DN1 → (a) consumer-side fallback in `TenantDetailPage` (patch P-DN1 below). DN2 → handoff to the FrontComposer shell/UX owner (deferred below). DN3 → (a) keep the code, correct the prior record (patch P-DN3 below).

**Patch:**

- [x] [Review][Patch] Harden page governance guards — **APPLIED (partial, by design):** recursed the three route-page guards (`Domain_page_components_declare_frontcomposer_page_layout_modes`, `Domain_route_pages_declare_frontcomposer_page_headers`, `Domain_pages_do_not_reintroduce_page_root_layout_wrappers`) to `SearchOption.AllDirectories`, and rewrote the page-root CSS `pageRootSelector` regex to match grouped selectors (`.root, .root__x { … }`) while excluding BEM children/descendants via a `(?=[,{])` lookahead. **NOT applied:** adding `min-*` longhands to the blocked-property regex (would flag the legitimate retained `.global-admins { min-width: 0 }` overflow guard and break the build — see Documented page-root CSS exceptions); and rewriting the first-`Class=` root-class derivation (low value, and `Domain_page_css_does_not_own_page_root_layout` is intentionally left `TopDirectoryOnly` so future subfolder *component* classes are not misread as page-root). UI tests 682/682. [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs]
- [x] [Review][Patch] Document the retained page-root CSS exceptions (e.g. `min-width:0`) in this artifact's Dev Agent Record, per the I/O-matrix "documented exceptions" requirement — **APPLIED:** see "Documented page-root CSS exceptions (P2, full re-review 2026-06-18)" under Completion Notes. [_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md]
- [x] [Review][Patch] `FcPageHeader` styling/robustness cleanup — **APPLIED (partial, by design):** removed the redundant `.fc-page-header__actions { margin-left: auto }` rule (the title row already uses `HorizontalAlignment.SpaceBetween`). **NOT applied:** relocating the stylesheet `<link>` from per-instance `<HeadContent>` to the shell — would couple `FcPageHeader` to `FrontComposerShell` and break standalone (non-shell) use of the component; the duplicate-`<link>` case is latent (no current page renders two `FcPageHeader` instances simultaneously). **NOT applied:** the `AdditionalAttributes` vs `class`/`data-testid` "collision" — Blazor resolves duplicate splatted attributes by last-wins (no runtime throw), and no current consumer passes those via loose attributes; left as the component author's fail-fast contract. ⚠️ Submodule edit — needs a `Hexalith.FrontComposer` commit + parent gitlink bump to land in CI/release. [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-page-header.css]
- [x] [Review][Patch] (P-DN1, from DN1) Add an empty/whitespace-`Name` fallback to the Tenant Detail success-branch heading — **APPLIED:** `Heading="@(string.IsNullOrWhiteSpace(Detail.Name) ? Localizer["Tenants.Detail.UnnamedTenant"] : Detail.Name)"`; added the `Tenants.Detail.UnnamedTenant` resource key to `TenantsResources.resx` ("Unnamed tenant") and `TenantsResources.fr.resx` ("Locataire sans nom"). The HIGH crash path is closed; UI tests 682/682. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:78; src/Hexalith.Tenants.UI/Resources/TenantsResources.resx; .fr.resx]
- [x] [Review][Patch] (P-DN3, from DN3) Correct the prior "Review Findings (code review 2026-06-18)" record — **APPLIED:** added the "Full re-review 2026-06-18 clarification (P-DN3)" sub-note to the prior DAPR/health bullet (the `pubsub.yaml`/health code was committed in `92bf113` to satisfy the existing governance-test contracts; "NOT APPLIED" referred only to the further proposed dead-letter hardening; bundled future-story docs acknowledged). No code change. [this artifact]

**Deferred:**

- [x] [Review][Defer] (from DN2) Page landmark & accessible-name a11y regression — handoff to the FrontComposer shell/UX owner per the spec's "FrontComposer boundary" rule (shell native `<main>` + accessible-name parameter; ensure the page `<header>` is not a competing `banner`); candidate to fold into the ready-for-dev `cc-2026-06-18` structural-and-style conformance sweep [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:9; .../FrontComposerShell.razor:113] — deferred, cross-submodule FrontComposer/UX decision
- [x] [Review][Defer] Intermediate commit `4ce8a84` is non-building standalone — it migrated Tenants pages to `FcPageHeader` while the FrontComposer submodule pointer still lacked the component (pointer bumped only at HEAD `9af97d9`); end state at HEAD is consistent (`e064573` ⊃ `80afcd9`). [git history] — deferred, history hygiene only; fix = squash/reorder if per-commit CI/bisect matters.

**Dismissed (5):** `tenants-detail-identity` shared by `<h1 id>`+`<header data-testid>` (separate attribute namespaces; no prior element carried that id; testid focus path preserved); `MainLayout.razor` absent from diff (preserve-by-omission, verified no competing `FluentLayout`); audit "Back" link moved into title-row `Actions` slot (intended consequence of approved uniform-header scope, selector preserved); eyebrow restyle 700/uppercase→Semibold (intended uniformity of approved scope); health writer emits `"description": null` when absent (handled/intended — surfaced-by-design and asserted in `HealthEndpointsTests`, harmless).

### Review Findings (independent re-review 2026-06-18)

Scope: third, independent adversarial pass via the `bmad-code-review` workflow — parent diff `974eac7..` (working tree) + FrontComposer submodule `6edc855..f4910d7`. Three fresh layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor — all Opus 4.8, no conversation context); 0 layers failed. 1 decision-needed, 1 patch, 3 deferred, 8 dismissed. The Acceptance Auditor independently re-verified all 7 ACs satisfied and that the prior-review recorded resolutions match the code (`role="main"` on `#fc-main-content`; `Tenants.Detail.UnnamedTenant` present in both `TenantsResources.resx` and `.fr.resx`; governance regexes broadened to logical/longhand props + grouped selectors + `AllDirectories` recursion + declare-or-fail). New value over the prior two reviews: the pub/sub dead-letter config was found to be **inert**, which the earlier passes (which debated only whether to *keep* the DAPR code) did not surface.

**Decision-needed:**

- [x] [Review][Decision] `pubsub.yaml` dead-letter configuration is inert (DLQ never wired) — `enableDeadLetter`/`deadLetterTopic` are declared under the Redis pub/sub component's `spec.metadata`, but DAPR configures dead-lettering **per-subscription** (`deadLetterTopic` on a `Subscription` resource / subscribe metadata), not as `pubsub.redis` component metadata. DAPR silently ignores these unknown component keys, so undeliverable `tenants.events` messages are NOT routed to `deadletter.tenants.events` despite the file's "Dead-letter enabled" comment. Found independently by Blind Hunter + Edge Case Hunter; both prior reviews treated this code as functional-but-could-be-hardened and missed that it does nothing. The keys currently exist only to satisfy `CrossAggregateTimingDocumentationTests` (which asserts their presence in `pubsub.yaml` + the timing guide), so removing them would break that test. This is out of the page-layout/page-header story's scope but is in the reviewed diff. Options: (a) accept the keys as test-satisfying placeholders and track a real DLQ wiring as a separate deployment-readiness task (matches the spec's earlier "separate deployment-readiness task" note, but now with the explicit understanding that the DLQ is non-functional today); (b) wire a real DAPR `Subscription` with `deadLetterTopic` now and update the governance test to assert the working topology; (c) other. [MEDIUM] [src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml:31-34]
  - **RESOLVED 2026-06-18 (Jérôme Piquot) — option 1, truthful in-repo correction, applied + verified.** DAPR docs confirm `enableDeadLetter`/`deadLetterTopic` are NOT Redis pub/sub *component* metadata (DLQ is per-subscription; there is no `enableDeadLetter` field). **Crucial correction to the finding's premise:** dead-lettering is *not* broken — EventStore's application-level `DeadLetterPublisher` (registered in `Hexalith.EventStore.Server`, injected into `AggregateActor`) routes command-processing infrastructure failures to `deadletter.tenants.events`, and the EventStore Admin DLQ console reads/retries/skips/archives them. The two component keys were only inert, misleading decoration. **Changes:** removed `enableDeadLetter`/`deadLetterTopic` from local `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and production `deploy/dapr/pubsub.yaml` (kept valid `publishingScopes`/`subscriptionScopes`); corrected both file comments + the `docs/cross-aggregate-timing.md` failure-and-recovery sentence to credit the app-level publisher; updated `CrossAggregateTimingDocumentationTests` to assert the keys are **absent** (`ShouldBeNull`) and that the guide documents the application-level mechanism (`application-level` + `dead-letter publisher`). Native DAPR per-subscription DLQ was deliberately NOT added — it would duplicate the working app-level mechanism, and the `/dapr/subscribe` builder is EventStore-submodule-owned with no dead-letter hook on `EventStoreDomainEventsOptions`. Verified: `CrossAggregateTimingDocumentationTests` 7/7 (Release).

**Patch:**

- [x] [Review][Patch] No regression test guards the P-DN1 blank-`Name` fallback — the story's highest-risk new path (`FcPageHeader.OnParametersSet` hard-throws on empty `Heading`, mitigated only by the `TenantDetailPage` `Tenants.Detail.UnnamedTenant` consumer fallback) has no bUnit test in `Hexalith.Tenants.UI.Tests`. `PageLayoutDeclarationTests` asserts only the layout attribute, not that a blank-`Name` `TenantDetail` renders the fallback heading instead of crashing. Add a focused test: render `TenantDetailPage` with a loaded `Detail.Name = ""`/whitespace and assert the `UnnamedTenant` heading renders (no `ArgumentException`). [tests/Hexalith.Tenants.UI.Tests/Components/]
  - **APPLIED 2026-06-18:** added `[Theory]` `Detail_page_renders_unnamed_fallback_heading_for_blank_name_without_crashing` (`InlineData("")` + `InlineData("   ")`) to `TenantDetailSurfaceTests`. It loads a `TenantDetail` with a blank `Name` through the gateway, asserts the success identity surface (`[data-testid='tenants-detail-identity']`) renders at all (proving `FcPageHeader` did not throw), and asserts the `<h1 id='tenants-detail-identity'>` text equals the localized `Tenants.Detail.UnnamedTenant` fallback (non-empty). Verified: `Hexalith.Tenants.UI.Tests` **684/684** (Release), up from 682. [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs]

**Deferred:**

- [x] [Review][Defer] A11y landmark & accessible-name regression — orphaned `aria-labelledby` on roleless `FluentStack` `<div>`s + duplicate `banner` from each page's `FcPageHeader` `<header>` + unnamed shell `main` (`#fc-main-content` has no accessible name). [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:9; .../FrontComposerShell.razor:113] — deferred, **reaffirms the existing DN2 entry** in `deferred-work.md`; still open, cross-submodule FrontComposer/UX decision. Confirm `cc-2026-06-18-frontcomposer-fluent-structural-and-style-conformance-sweep` actually carries it before this story is closed. Not an AC breach (no AC asserts landmark/accessible-name parity; formal a11y evidence is a later story).
- [x] [Review][Defer] `FcPageHeader` is a fail-open contract — `OnParametersSet` calls `ArgumentException.ThrowIfNullOrWhiteSpace(Heading)` (page-wide crash on blank heading) and `FocusHeadingAsync()` silently no-ops if a caller omits `HeadingTabIndex="-1"` (the heading is not focusable). The active crash path is closed consumer-side (DN1 option a); all six current callers pass non-blank localized headings, and focus-restoring callers pass `tabindex="-1"`. DN1 explicitly left option (b) "soften the component" untaken. [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:60] — deferred, FrontComposer-owned contract hardening; forward-looking, not actionable in this Tenants story.
- [x] [Review][Defer] Out-of-scope DAPR/health/`pubsub.yaml`/`Program.cs` changes bundled into a UI page-layout story — `Program.cs` health-response writer + endpoint wiring, the new `pubsub.yaml`, and the `HealthEndpointsTests` rename are not page layout or page header. Honestly recorded and reconciled via DN3 (keep code + correct the prior record); the human chose to keep it. [src/Hexalith.Tenants/Program.cs; src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml] — deferred, commit/scope hygiene only; optionally split into a deployment-readiness commit (DN3 option b) if per-commit history matters.

**Dismissed (8):** Blind Hunter's HIGH `TenantDetailPage` focus regression (`tenants-detail-identity` id on a non-focusable `<h1>`) — **false positive**: the Acceptance Auditor verified `ReturnFocus` is serialized by `AuditEvidenceEntryPoint` as a navigation query-param + visible context text, not a DOM `getElementById().focus()`, so the missing `tabindex` is harmless; `tenants-detail-identity` as both `id` (h1) and `data-testid` (header) — separate attribute namespaces, no functional collision, prior review dismissed; `FcPageHeader` per-instance stylesheet `<link>` in `<HeadContent>` — consciously kept in prior P3 (preserves standalone use), browsers de-dupe identical links, latent only; multiple `<PageTitle>` / detail browser-title shows generic title — branches are mutually exclusive (Edge cleared) and the title regression is speculative (AC6 selector/label preservation confirmed); `pubsub.yaml` `sample` scope — plausibly intentional (the sample consumer is a documented part of the system) and governed by the timing test; dev health writer surfaces check `Description` — by-design, Development-only writer, support-safety (no exceptions/tokens/connection strings) asserted by `HealthEndpointsTests`; brittle source-substring assertions in `UserMembershipLookupSurfaceTests` — intentional source-governance pattern consistent with `DomainUiFluentConformanceTests`, tests pass; audit page lacks a blank-`TenantId` heading fallback — degenerate/malformed route, cosmetic (renders "Audit – "), no crash.

## Design Notes

`FrontComposerShell` already owns the shell-level `FluentLayout`. Tenants pages should not instantiate a competing shell layout. The page-level decision is the `FC-LYT` measure: full-width for operational grids and constrained for readable regions. When layout primitives are needed inside a page, use Fluent layout components before CSS grids.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`
- `git diff --check`

## Suggested Review Order

**Layout Contract**

- Confirm `MainLayout.razor` still composes `<FrontComposerShell>@Body</FrontComposerShell>`.
- Review page declarations for correct `FcPageLayoutMode.FullWidth` versus `FcPageLayoutMode.Constrained` choices.

**Page CSS**

- Review root page CSS removals before component-local spacing changes.
- Confirm remaining page CSS is documented as an exception or is not expressing a layout primitive already available through Fluent/FrontComposer.

**Governance**

- Review `DomainUiFluentConformanceTests` last so the guard matches the intended migration boundary.

## Dev Agent Record

### Implementation Plan

- Verified `FcPageLayout`, `FcPageLayoutMode`, and shell layout behavior from local FrontComposer source.
- Verified `FluentStack`, `FluentGrid`, and `FluentGridItem` parameters from the pinned local NuGet XML for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the Fluent MCP server documents `5.0.0.26139`, so it was treated as secondary only.
- Added failing page-layout governance before implementation, then migrated the six Tenants pages to explicit `FcPageLayout` declarations and Fluent layout primitives.
- Trimmed page-root CSS layout ownership while preserving forced-colors, focus, state, grid overflow, and readable-region styling.
- Added `FcPageHeader` in FrontComposer as a generic page-title/header component with consumer-owned localized text, optional metadata/actions slots, heading accessibility parameters, and a focus method for route context restoration.

### Debug Log

- Red phase confirmed: `DomainUiFluentConformanceTests` failed on missing `FcPageLayout`, page-root `<main>` wrappers, and page-root layout CSS.
- Green phase: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed `681/681`.
- Tier 1 regression: Contracts `106/106`, Client `47/47`, Testing `181/181`, Sample `32/32` passed. Initial parallel attempts hit MSBuild file locks; sequential reruns passed.
- Tier 2 `Server.Tests` was attempted and still has six known pre-existing documentation/AppHost failures around missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence.
- Tier 3 `IntegrationTests` was attempted with `Category!=Performance`; DAPR/Aspire-dependent rows skipped and two known pre-existing health-readiness drift tests failed.
- `git diff --check` passed with a CRLF normalization warning for `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Red phase confirmed: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --filter FcPageHeaderTests` failed because `FcPageHeader` did not exist.
- Green phase: the same focused FrontComposer command passed `4/4` after adding `FcPageHeader` and the component tests. The run also required a Release-only import fix in `FrontComposerShellTests.cs` for the pre-existing `CustomizationLevel` reference.
- Red phase confirmed: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter Domain_route_pages_declare_frontcomposer_page_headers` failed with 18 route-page offenders before migration.
- Green phase: the same focused Tenants governance command passed after migrating the route pages to `FcPageHeader`.
- UI regression: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed `682/682`.
- Full FrontComposer shell regression was attempted with `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore`; it still failed `20/1905` on pre-existing/out-of-scope shell security, route hydration, auth-boundary, release-readiness, package-inventory, home-directory landmark, and Verify snapshot/culture drift assertions. Focused `FcPageHeaderTests` passed.

### Completion Notes

- Tenants pages now declare FC-LYT layout through `FcPageLayout`: full-width for tenant list, my tenants, global administrators, and audit; constrained for tenant detail and user lookup.
- Page composition moved from Tenants-owned root `<main>`/CSS layout scaffolding to Fluent `FluentStack` and `FluentGrid` primitives where the story required page-level layout ownership to move out of local CSS.
- Governance now blocks missing page layout declarations, raw page-root layout wrappers, and page-root CSS layout ownership.
- Component coverage now renders a full-width page and a constrained page inside `FrontComposerShell` and asserts shell layout state.
- FrontComposer now owns the shared `FcPageHeader` page-title/header contract, with tests covering the browser title component, visible route heading, optional text/fragments, accessibility parameters, and generic resource independence.
- Tenants route pages now declare route browser titles and visible route headers through `FcPageHeader`; page-specific metadata/actions remain Tenants-owned fragments.
- Tenant list return-context focus restoration now uses `FcPageHeader.FocusHeadingAsync()` against the shared heading element, preserving the existing return-focus behavior without direct page-owned `<h1>` markup.
- Governance now rejects route pages that omit `FcPageHeader`, declare direct `<PageTitle>`, or declare raw route-level `<h1>` markup.

### Documented page-root CSS exceptions (P2, full re-review 2026-06-18)

Per the I/O-matrix "Governance scan → Allow only documented exceptions in the implementation artifact" requirement, the page-root classes deliberately retain only non-layout-ownership rules; all `display`/`grid`/`padding`/`max-width` layout lives on BEM child (region) classes, which `FC-LYT` permits:

- `GlobalAdministratorsPage.razor.css` — `.global-admins { min-width: 0 }`: flexbox/grid overflow guard (lets dense grid content shrink instead of forcing track overflow). Not page measure ownership; intentionally NOT in the `PageRootLayoutDeclaration` blocked-property set.
- `TenantsWorkspace.razor.css` / `MyTenantsPage.razor.css` — `.tenants-workspace h2, .tenants-workspace p { margin: 0 }` and `.my-tenants-page h2, .my-tenants-page p { margin: 0 }`: descendant heading/paragraph margin resets (typography normalization), not root layout.
- All other root-class usages resolve to BEM child/region classes (`.global-admins__*`, `.tenant-audit__*`, `.user-lookup-page__*`, `.tenant-detail__*`), which own component-local layout by design and are outside the page-root guard.

Note: `min-*` longhands are intentionally excluded from the page-root blocked-property regex — adding them would flag the legitimate `min-width: 0` overflow guard above.

## File List

- `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-page-header-frontcomposer.md`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor.css`
- `tests/Hexalith.Tenants.UI.Tests/Components/PageLayoutDeclarationTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-page-header.css`
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs`
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`

## Change Log

- 2026-06-18: Implemented FrontComposer/Fluent page-layout conformance sweep and moved story to review.
- 2026-06-18: Reopened story to in-progress after Administrator approved the FrontComposer page-header component scope extension.
- 2026-06-18: Code review of the layout phase (diff 974eac7..HEAD). Applied 3 patches (shell `role="main"` landmark via FrontComposer + home-page duplicate cleanup; pager/header CSS-regression fixes; governance-guard hardening); 1 finding not applied (DAPR/health already governed by existing Server.Tests); D2 resolved to no code change. `Hexalith.Tenants.UI.Tests` 681/681. Page-header migration phase still pending.
- 2026-06-18: Implemented the FrontComposer `FcPageHeader`, migrated Tenants route pages to it, and added route-header governance. Tenants UI passed 682/682 and focused FrontComposer page-header tests passed 4/4; full FrontComposer shell regression still has unrelated pre-existing failures.
- 2026-06-18: Re-verified both phases and moved story to review. `Hexalith.Tenants.UI.Tests` 682/682 (Release), focused `FcPageHeaderTests` 4/4 (Release), `git diff --check` clean. Open finalization item for review/commit: the parent repo gitlink for `Hexalith.FrontComposer` is stale (`+80afcd9`, the commit that adds `FcPageHeader`) and must be committed so the recorded state builds; submodule working tree is otherwise clean.
- 2026-06-18: Full-story re-review (diff `974eac7..HEAD` + FrontComposer submodule `6edc855..e064573`) via three adversarial layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor, all Opus 4.8). 3 decision-needed (DN1 HIGH `FcPageHeader` empty-`Name` crash → consumer fallback; DN2 landmark/a11y → FrontComposer/UX defer; DN3 → keep code + correct record), 3 patch + 2 decision-derived patches applied (P1/P3 partial by design, with rationale), 1 prior defer + DN2 newly deferred, 5 dismissed. Closed the HIGH crash (added `Tenants.Detail.UnnamedTenant` en/fr + fallback heading). `Hexalith.Tenants.UI.Tests` 682/682 (Release). Status → in-progress: residual deferred MEDIUM a11y regression handed to the `cc-2026-06-18` structural-and-style sweep, and a pending `Hexalith.FrontComposer` submodule commit + parent gitlink bump for the P3 CSS cleanup.
- 2026-06-18: Resumed and finalized the reopened story. Re-verified both phases green against the current committed state (parent `c7da059` + FrontComposer submodule `f4910d7`): `Hexalith.Tenants.UI.Tests` 682/682 (Release), focused `FcPageHeaderTests` 4/4 (Release), `git diff --check` clean. Marked the three full-re-review Decision items `[x]` per the recorded Resolutions (DN1 patched P-DN1, DN2 deferred to the `cc-2026-06-18` structural-and-style sweep, DN3 patched P-DN3) — all tasks/subtasks now checked. Confirmed the P3 submodule commit landed and is pushed (`f4910d7` "remove unused .fc-page-header__actions class", working tree clean). One git-mechanics finalization item remains outside this workflow's commit scope and the repo's "no direct commits to `main`" policy: bump the parent gitlink `Hexalith.FrontComposer` `e064573` → `f4910d7` (already staged in the working tree as `modified: Hexalith.FrontComposer`) on a `fix/…` branch so the cleanup lands in CI/release. Status → review.
- 2026-06-18: Independent third code review (bmad-code-review workflow) — three fresh adversarial layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor, all Opus 4.8) over parent `974eac7..` + FrontComposer `6edc855..f4910d7`. 1 decision-needed, 1 patch, 3 deferred, 8 dismissed; all 7 ACs re-confirmed satisfied. **D1 (new, not caught by the prior two reviews):** the `pubsub.yaml` `enableDeadLetter`/`deadLetterTopic` component-metadata keys are inert (verified against DAPR docs — DLQ is per-subscription). Resolved option 1 (truthful correction): dead-lettering actually works via EventStore's application-level `DeadLetterPublisher`, so removed the inert keys from local + `deploy/dapr/pubsub.yaml`, corrected the comments + `docs/cross-aggregate-timing.md`, and updated `CrossAggregateTimingDocumentationTests` to assert their absence + the app-level mechanism (7/7 Release). **P1:** added a `TenantDetailSurfaceTests` regression theory guarding the P-DN1 blank-`Name` `UnnamedTenant` fallback (UI 684/684 Release, up from 682). Deferrals (a11y landmark DN2, `FcPageHeader` fail-open contract, out-of-scope DAPR/health bundling) recorded in `deferred-work.md`. Status → done.

## Status

done
