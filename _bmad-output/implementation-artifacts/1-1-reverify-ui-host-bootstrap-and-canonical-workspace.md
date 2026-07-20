---
created: 2026-07-19T20:59:28+02:00
baseline_commit: 088232a7255698e20105594d9e0ef12a0f09c73e
frontcomposer_source_commit: d3761fa08ce2f4bf004e8adc7f500822d04276f8
builds_source_commit: 9ec0a032d785dd0abdc14276e8784d6fdd826fd0
frontcomposer_package_baseline: 4.0.1
fluent_ui_pin: 5.0.0-rc.4-26180.1
historical_story_evidence: _bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md
prerequisite_evidence: _bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md
---

# Story 1.1: Reverify UI Host Bootstrap and Canonical Workspace

Status: done

<!-- Created for the corrected Story 1.1 contract. Historical completion is evidence to reverify, not a readiness waiver. -->

## Story

As an authenticated Tenants user,
I want a single, stable Tenants workspace inside the platform shell,
so that I can reach authorized tenant-management capabilities through a consistent and support-safe entry point.

## Acceptance Criteria

1. **Existing host and runtime.** Given the existing `src/Hexalith.Tenants.UI` project, when the application is built and started, then it runs as a .NET 10 Blazor `InteractiveServer` application composed through FrontComposer and Fluent UI Blazor V5, and it remains registered in `Hexalith.Tenants.slnx` as an application/container rather than a NuGet package.

2. **One module entry.** Given an authenticated user opens the platform shell, when the domain navigation is rendered, then exactly one Tenants module entry targets `/tenants`, and All Tenants, My Tenants, Users, Global Administrators, Audit, and command lifecycle do not register additional Tenants shell entries.

3. **Page-local workspace tabs.** Given the user opens `/tenants`, when workspace navigation is displayed, then page-local `Tenants` and lookup-backed `Users` tabs are available through FrontComposer/Fluent components, and the Users tab is not represented as an exhaustive all-users inventory.

4. **Canonical workspace state.** Given canonical workspace parameters `tab`, `scope`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor`, when valid or invalid combinations are loaded, then valid state is represented consistently, invalid state normalizes fail-safe, and tab/scope/filter/sort changes reset the cursor; `/tenants/my` and `/tenants/users` remain renderable compatibility routes while generated navigation uses canonical `/tenants` state.

5. **InteractiveServer trust boundary.** Given the InteractiveServer trust boundary, when a component needs backend data or command behavior, then it can depend only on injected server-side BFF gateway/composition contracts, and no browser-side component holds a backend bearer token or directly calls Tenants, EventStore, or Memories.

6. **Localization ownership.** Given shell chrome and Tenants domain copy, when the workspace renders in English or French, then shell-owned text comes from FrontComposer resources and Tenants-owned text comes from parity-checked whole-string `.resx` resources, and no sentence is assembled from localized fragments.

7. **Responsive Fluent shell.** Given desktop, tablet, and mobile viewport widths, when the workspace shell is rendered, then navigation and layout use FrontComposer/Fluent primitives, tablet navigation can collapse, and mobile remains a safe read-oriented shell; no raw interactive HTML controls, duplicate page chrome, route-level `PageTitle`, page-root `<main>`, theme redefinition, or unsupported layout CSS is introduced.

8. **Direction-safe layout.** Given workspace layout, spacing, alignment, overflow, or directional controls are authored, when the shell is rendered in any supported left-to-right culture, then FrontComposer/Fluent primitives and logical start/end behavior are used without hard-coded left/right assumptions that would prevent future bidirectional layout, and the story makes no claim that RTL verification or shipping is complete while that work remains explicitly deferred.

9. **Deployment and ownership boundary.** Given the UI deployment boundary, when publish configuration is inspected, then the app uses .NET SDK container support with `ContainerRepository=tenants-ui`, shared non-root defaults, externalized configuration, and no Dockerfile, and the transitional repository AppHost is not expanded with shared orchestration, ServiceDefaults, health, telemetry, configuration, or secrets infrastructure.

10. **Focused evidence.** Given the bootstrap and workspace composition, when focused route, shell, localization-parity, support-safety, Fluent-conformance, and responsive checks run, then stable `data-testid="tenants-{surface}-{element}"` contracts are available where interaction begins, and exact commands/results and any external `PLATFORM-OPS-1` blockers are recorded without weakening the story's local checks.

## Tasks / Subtasks

- [x] Establish the immutable reverification baseline and evidence record. (AC: 1-10)
  - [x] Preserve `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md` as historical evidence; do not overwrite it or restore its obsolete placeholder-only assumptions.
  - [x] Create a dated Story 1.1 evidence report under `_bmad-output/implementation-artifacts/` that records the root commit, FrontComposer/Builds source commits, FrontComposer package baseline, exact Fluent pin, resolved UI packages, commands, exit codes, pass counts, and environment blockers.
  - [x] Classify each acceptance criterion as `verified`, `changed`, or `blocked`. A historical `[x]`, current source shape, or broad green suite is evidence to inspect, not automatic proof.
  - [x] Record the pre-existing dirty worktree before implementation and preserve unrelated planning, sprint, deferred-work, and submodule-pointer changes.

- [x] Reverify the existing UI host rather than scaffold or replace it. (AC: 1, 5, 9)
  - [x] Verify the `Microsoft.NET.Sdk.Web`/`net10.0` host, global Interactive Server registration and render mode, `FrontComposerShell` layout, Fluent registration, request localization, authentication/authorization, antiforgery, and server-side gateway composition.
  - [x] Verify `Hexalith.Tenants.slnx` contains the UI and UI-test projects; use no `.sln` file.
  - [x] Verify `IsPackable=false`, `IsPublishable=true`, SDK container support, `ContainerRepository=tenants-ui`, inherited non-root `app` user/base-image/port defaults, externalized configuration, and the absence of a Dockerfile.
  - [x] Determine and record image-publication ownership and any gap. Change a release workflow only when a cited, existing repository-owned publication contract makes that change necessary for AC9; otherwise record the exact platform handoff/blocker and do not equate project properties with a shipped image.
  - [x] Do not add ServiceDefaults, health, OpenTelemetry, secrets, or shared orchestration plumbing to `Hexalith.Tenants.AppHost`; record these as `PLATFORM-OPS-1` where still external.

- [x] Prove the single-module FrontComposer composition contract. (AC: 2, 3, 7)
  - [x] Verify `TenantsFrontComposerRegistration` registers exactly one domain and one shell navigation entry, invariant id `tenants`, targeting `/tenants`.
  - [x] Source-scan and render-test that My Tenants, Users, Global Administrators, Audit, and command lifecycle add no shell entry.
  - [x] Preserve page-local `FluentTabs` for `Tenants` and lookup-backed `Users`; keep My Tenants as `scope=mine`, and keep global-administrator/audit/command surfaces contextual or inline.
  - [x] Preserve the explicit Users copy and behavior that says lookup/search-backed and never implies an exhaustive user directory.

- [x] Make canonical workspace state deterministic and fail-safe. (AC: 3, 4, 10)
  - [x] Build a transition matrix for `tab`, `scope`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor`, using existing query/sort constants rather than duplicating vocabularies.
  - [x] Normalize unsupported, contradictory, malformed, or surface-inapplicable values before use. Apply only limits already owned by existing state/domain contracts; when no authoritative `userId` or search limit exists, preserve safely encoded input and record the gap instead of inventing a Story 1.1 bound. Invalid input must converge on a safe canonical state without disclosing data or leaving an invalid URL presented as active state.
  - [x] Reset the relevant cursor/history whenever tab, scope, user id, search, status, sort field, or sort direction changes. Never carry a tenant-list cursor into My Tenants/Users or a lookup cursor into the tenant list.
  - [x] Keep the canonical URL synchronized after tab, scope, filter, and sort changes. Defaults may be omitted, but generated URLs must be deterministic and safely encoded.
  - [x] Propagate `TenantDataGrid` sort changes back to workspace state instead of leaving the grid's sort and canonical `sort`/`desc` query parameters divergent.
  - [x] Keep `/tenants/my` and `/tenants/users` renderable for compatibility, but make their generated back/return/tab navigation target canonical `/tenants?...` state.
  - [x] Preserve the current Story 1.3 `selected`/`anchor` return-and-focus behavior while changing filters. Treat those values as contextual return metadata, not extra shell-navigation dimensions.
  - [x] Prefer extending the existing `TenantListNavigationContext`/route helpers. Add one focused immutable workspace-state type only if it removes mixed responsibilities; do not create a second routing framework.

- [x] Reverify the server-only BFF and support-safety boundary. (AC: 5, 10)
  - [x] Verify Razor components depend on `ITenantQueryGateway`, `ITenantCommandGateway`, `ITenantsBffComposition`, or narrower server-side collaborators and do not construct backend `HttpClient` instances.
  - [x] Verify access-token acquisition/relay remains server-side. Tokens, decoded cursor contents, ETags, raw metadata, payloads, correlations, and stack traces never reach markup, browser storage, selectors, logs, or announcements. The only cursor permitted in a URL is the opaque canonical workspace cursor required by AC4; never expose a raw offset or decoded/scope-binding material. Story 1.9 owns protected search-cursor behavior and its stricter client-readable-state boundary.
  - [x] Preserve the fail-closed unavailable gateway registrations when backend configuration is absent.
  - [x] Do not pull Story 1.9 search-cursor or Story 1.10 direct-read/freshness work into this story; name `SEARCH-CURSOR-1`, `HOST-REF-1`, `UI-READ-1`, and `PLAT-FRESH-1` where they constrain later behavior.

- [x] Reverify localization, accessibility, responsive, and direction-safe behavior. (AC: 6-8, 10)
  - [x] Verify complete EN/FR resource-key parity, whole-string resources, named placeholders, culture-aware formatting, and the boundary between `FcShellResources` and `TenantsResources`.
  - [x] Verify the document `<html lang>` follows the active UI culture; the current hard-coded English value is a required inspection/fix target.
  - [x] Preserve one shell-owned `<main>`, FrontComposer-owned page title/header/focus behavior, keyboard-operable tabs, logical focus order, visible focus, no-color-only meaning, forced-colors behavior, and reduced-motion independence.
  - [x] Extend conformance coverage to reject physical left/right layout assumptions across the full Story 1.1-owned workspace/shell path, including pre-existing rules, while allowing documented semantic exceptions; describe the result as RTL-ready only, never RTL-tested or shipped.
  - [x] Collect reproducible phone/tablet/desktop browser evidence for navigation collapse, read-safe mobile layout, overflow, authenticated tab operation, one-main composition, focus after navigation, forced colors/high contrast, and EN/FR document/accessibility text. Authenticated EN/FR snapshots, console logs, exact commands, and assertion results are retained in the Story 1.1 evidence report and browser-evidence directory.
  - [x] Do not copy the historical tenant-list mockup: it contains obsolete multi-entry navigation, raw controls, stale package assumptions, and physical-direction CSS.

- [x] Run focused checks and issue the complete evidence decision. (AC: 1-10; NFR10)
  - [x] Add/adjust focused tests for the query transition matrix, invalid normalization, cursor reset, sort propagation, compatibility routes, one nav entry, BFF-only source boundaries, document culture, resource parity, stable selectors, and Fluent/layout/direction conformance.
  - [x] Run UI tests individually with xUnit v3/Shouldly/bUnit conventions; use the `.slnx` for restore/build only, not solution-level `dotnet test`.
  - [x] Run the package/solution governance tests affected by application/container/release registration.
  - [x] Run and retain the exact Aspire route-smoke command against the discovered `tenants-ui` endpoint. The final hosted lane passed 6/6 after explicitly rebuilding the Release UI resource launched by the fixture with `--no-build`.
  - [x] If responsive/assistive-technology proof needs a new E2E lane, reuse the narrowest existing platform/browser harness. Do not duplicate shared Playwright/Aspire test infrastructure in Tenants.
  - [x] Record every exact command/result, including browser commands and artifact paths. The evidence report records the remaining production-publication `PLATFORM-OPS-1` blocker and the unbounded-request-target `HTTP-TARGET-1` owner, consequence, conservative behavior, and reopen trigger.
  - [x] Confirm all existing later Story 1.x and command/audit UI behavior remains present and the full configured UI suite has no regression.

## Dev Notes

### Scope And Epic Context

Epic 1 delivers the complete read-only tenant discovery/access-review product (FR1-FR9 and FR18). Story 1.1 owns only the existing host, one shell entry, canonical workspace state, trust/localization/responsive boundaries, and its evidence. Story 1.2 owns list/cursor behavior; 1.3 detail/return context; 1.4 My Tenants; 1.5 Users lookup; 1.6 configuration; 1.7 member review/action availability; 1.8 safe copy/read evidence; 1.9 Memories search; 1.10 direct reads/freshness; and 1.11 global-administrator review. Preserve those implemented surfaces while fixing Story 1.1 foundations; do not rebuild or absorb them. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Trustworthy Tenant Discovery and Access Review`; `_bmad-output/planning-artifacts/epics.md#Story 1.1: Reverify UI Host Bootstrap and Canonical Workspace`]

The authoritative shared NFR10 gate still applies: readiness/completion needs applicable accessibility, localization, responsive, documentation/reference, and focused-test evidence, or the exact Product/UX-approved fallback record. Story 1.1's focused lanes do not narrow this gate. [Source: `_bmad-output/planning-artifacts/epics.md#NonFunctional Requirements`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19-v2.md#CC-1 — Make the shared NFR10 evidence gate authoritative`]

### Current Implementation: Change Versus Preserve

| Area / file | Current state | Story treatment |
|---|---|---|
| `src/Hexalith.Tenants.UI/Program.cs` | Interactive Server, FrontComposer/Fluent, localization, conditional OIDC, server token relay, fail-closed gateways | Verify and preserve; change only a demonstrated local Story 1.1 gap. Do not add platform hosting plumbing. |
| `Components/Layout/MainLayout.razor`, `Components/Routes.razor` | Shell-only layout and FrontComposer route composition | Verify and preserve one shell-owned main/title/header/focus path. |
| `Composition/TenantsFrontComposerRegistration.cs` | One `tenants` nav entry to `/tenants` | Verify and preserve exactly one entry. |
| `Components/Pages/TenantsWorkspace.razor` | `/` + `/tenants`, local tabs/scopes, list/lookup composition; only tab/scope are visibly normalized | Update canonical state, URL synchronization, cursor resets, and sort propagation without breaking later surfaces. |
| `Components/Tenants/TenantDataGrid.razor` | Fluent grid owns sort locally | Add the narrow callback/state seam needed for canonical workspace sort; retain Story 1.2 grid behavior. |
| `Components/Users/UserMembershipLookupPanel.razor` | Lookup surface has its own sort/cursor state | Reset lookup cursor and emit canonical workspace navigation; retain authorization-safe lookup behavior. |
| `Components/Pages/MyTenantsPage.razor`, `UserMembershipLookupPage.razor` | Compatibility routes render but generate some compatibility-route returns | Keep renderable; switch generated navigation/returns to canonical `/tenants` state. |
| `Components/App.razor` | Global Interactive Server root, but `<html lang="en">` is fixed | Verify/fix active-culture document language without changing render-mode ownership. |
| `Resources/TenantsResources*.resx` | Large EN/FR domain resource sets with observed parity | Verify full parity/whole strings; change only required Story 1.1 copy. |
| `Hexalith.Tenants.UI.csproj`, `Directory.Build.targets`, `.slnx` | Non-packable SDK-container app `tenants-ui`, shared non-root defaults, registered projects | Verify; preserve central versioning and container defaults. |
| `src/Hexalith.Tenants.AppHost/*` | Transitional local resource composition | Verify route only; do not expand with shared platform concerns. |
| UI, contract-governance, and integration tests | Strong component/conformance coverage; no current viewport Playwright evidence found | Extend focused route/state/direction/culture checks and attach honest browser/platform evidence. |

### Technical Requirements

- Reverify the brownfield host; do not run `dotnet new`, create a parallel UI, reintroduce a placeholder-only workspace, or delete later features to make bootstrap tests easy.
- Use .NET 10/C# current repository conventions, file-scoped namespaces, Allman braces, one C# type per file, nullable analysis, `ConfigureAwait(false)` in production awaits, central package versions, and warnings as errors.
- Use `Hexalith.Tenants.slnx` only. Run test projects individually.
- Keep FrontComposer registration order and scoped-lifetime rules. Do not capture scoped shell, auth, gateway, or storage services in singletons.
- Use caller-supplied tenant/user identifiers as meaningful strings; do not parse them as GUID/ULID. Continue safely encoding query/route values.
- Components render typed, support-safe snapshots and call injected server-side collaborators. They do not own transport clients, access tokens, projection persistence, ServiceDefaults, or generic shell infrastructure.
- Preserve the distinction between historical evidence and current proof. Do not claim `PLATFORM-OPS-1`, production scaling, RTL shipping, WCAG 2.2, protected search cursors, direct-read freshness, or command-contract readiness beyond recorded evidence.

### Architecture Compliance

- **AD-1/AD-2:** one `/tenants` shell entry; local Tenants/Users tabs and scope modes; compatibility/contextual routes are not generated navigation targets.
- **AD-3/AD-4:** FrontComposer/Fluent first; Tenants owns domain composition only, not generic shell, tabs, layout, theme, grid, command, or DI infrastructure.
- **AD-5:** injected server-side BFF/gateways are the only backend egress; browser tokens/direct backend calls are forbidden.
- **AD-9:** Tenants owns whole-string domain copy and support safety; FrontComposer owns shell chrome.
- **AD-11:** route, localization, selector, support-safety, and Fluent conformance tests are architectural guards.
- **AD-13/AD-14:** the UI app/container is domain-owned; orchestration/production operations are platform-owned. Keep the transitional AppHost from accumulating shared plumbing and keep InteractiveServer at one replica until DataProtection/session/cursor prerequisites are proven. [Source: `_bmad-output/planning-artifacts/architecture.md#Canonical Architecture Spine`]

### Library And Framework Requirements

- SDK `10.0.302`, `rollForward=latestPatch`, target `net10.0`. Interactive Server executes UI interaction on the server over a circuit; preserve `AddInteractiveServerComponents`/`AddInteractiveServerRenderMode` and the current global render-mode composition. [Source: `global.json`; `src/Hexalith.Tenants.UI/Program.cs`; `src/Hexalith.Tenants.UI/Components/App.razor`; https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0]
- FrontComposer package baseline `4.0.1`; current source evidence commit `d3761fa08ce2f4bf004e8adc7f500822d04276f8`. Preserve source/package distinction and do not modify the submodule under this story.
- Fluent UI Blazor components/icons `5.0.0-rc.4-26180.1`. This deliberate v5 prerelease pin is authoritative even though upstream stable releases use a different major line; do not upgrade or substitute rc.3 assumptions. [Source: `references/Hexalith.Builds/Props/Directory.Packages.props`; `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`; https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.4-26180.1]
- Aspire AppHost SDK `13.4.6` is local orchestration only. Preserve project-resource relationships and keep platform ownership explicit. [Source: `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`; https://aspire.dev/get-started/app-host/]
- SDK container publishing must stay Dockerfile-free; `ContainerRepository` controls the image name and `Directory.Build.targets` supplies the non-root `app` default. [Source: `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`; `Directory.Build.targets`; https://learn.microsoft.com/en-us/dotnet/core/containers/sdk-publish; https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration]
- Keep access tokens server-side. Microsoft explicitly warns against sending tokens to client components and recommends the server/BFF pattern. [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0]
- Tests use xUnit v3 `3.2.2`, Shouldly `4.3.0`, bUnit `2.8.4-preview`, and Microsoft.NET.Test.Sdk `18.8.1` from central package management; add no inline project versions.

### File Structure Requirements

- Primary UPDATE candidates: `TenantsWorkspace.razor`, `TenantDataGrid.razor`, `UserMembershipLookupPanel.razor`, both compatibility-route pages, possibly `App.razor`, and their focused UI tests.
- Extend `State/TenantList/TenantListNavigationContext.cs` or add one narrowly named immutable workspace-state file under the existing `State/` tree if required. Follow the one-type-per-file rule.
- Verify-only unless evidence proves a gap: `Program.cs`, shell layout/routes, FrontComposer registration, gateway implementations, AppHost, root build/package files, and existing resources.
- Test updates stay in `tests/Hexalith.Tenants.UI.Tests/`, `tests/Hexalith.Tenants.Contracts.Tests/`, and the existing integration/E2E location. Do not place test harness infrastructure under product source.
- Create only the dated evidence report as a required NEW artifact. Add no Dockerfile, `.sln`, duplicate shell/layout/navigation implementation, or source edits under `references/`.

### Testing Requirements

Required focused scenarios include:

- exactly one rendered/registered Tenants entry and no contextual/command entry;
- the default and each valid tab, scope, user, filter, and sort state class and transition, with representative pairwise cross-parameter cases rather than an exhaustive Cartesian product;
- invalid/contradictory `tab`, `scope`, `status`, `sort`, `desc`, and `cursor` normalization, plus `userId`/search handling against existing authoritative limits without inventing new bounds;
- cursor reset after tab, scope, user, search, status, sort, and direction transitions;
- tenant-grid and user-lookup sort propagation into canonical URLs;
- compatibility route rendering with canonical generated return navigation;
- lookup-only Users copy and authorization-safe states;
- BFF-only component/source boundary and absence of browser tokens/direct backend clients;
- EN/FR key parity, whole-string usage, navigation localization, active-culture `<html lang>`, and localized accessible names;
- one shell main, FrontComposer page chrome, keyboard tabs/focus, stable selectors, no raw controls/tables/forms, no inline layout styles, no unsupported page/theme/CSS ownership, logical direction, forced colors, reduced motion, and phone/tablet/desktop behavior;
- SDK container/non-packable/solution/release governance; and
- no regression in the complete UI suite, including already-implemented list/detail/member/command/audit surfaces.

Use these validation shapes, adjusting only where the current test runner requires the documented xUnit v3 executable fallback:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 -warnaserror
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release -m:1 -warnaserror
dotnet publish src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --configuration Release -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/tenants-ui-story-1-1.tar.gz
```

For focused reruns after building, invoke the xUnit v3 executable with single-dash selectors, for example:

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests
```

Use Aspire CLI/resource discovery and the existing route-smoke path for runtime evidence. Browser evidence must use navigable endpoints discovered from Aspire, not assumed ports. If a platform-owned browser harness is unavailable, record the exact `PLATFORM-OPS-1` blocker rather than claiming responsive/accessibility completion from bUnit alone.

### Story 1.0 Intelligence

Story 1.0 verified shell bootstrap/order, one-entry navigation, full-width/constrained FrontComposer layout, `FC-A11Y`, `FC-L10N`, `FC-DOC`, approved audit/preview fallbacks, and exact Fluent rc.4 APIs. Its full Tenants UI baseline reported 904/904 tests passing. Reuse those demonstrated contracts, then rerun Story 1.1 evidence after changes. [Source: `_bmad-output/implementation-artifacts/1-0-reverify-frontcomposer-shell-and-fluent-contracts.md`; `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`]

Do not overclaim Story 1.0's blocked/changed contracts: `FC-CMD` and `FC-CNC` remain blocked, shared `FC-TBL` is offset-based/partial, and `FC-TOK` is partial. These do not block shell/workspace reverification, but Story 1.1 must not present command lifecycle as a nav area or claim complete generic grid/token behavior.

### Historical Story 1.1 Intelligence

The old Story 1.1 implemented the UI host, shell composition, minimal domain manifest, BFF seams, resources, AppHost resource, container properties, and initial tests. Its premise that no tenant reads existed is obsolete; later read, command, and audit surfaces now share this host. Preserve those surfaces and use the old completion record only as a checklist of evidence to reverify. Old rc.3 package references, earlier authentication assumptions, placeholder-state scope, and permissive AppHost expansion do not override the current architecture. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`]

### Git Intelligence

- Root baseline is `088232a7255698e20105594d9e0ef12a0f09c73e` on `main`. Recent root commits primarily reconcile planning and submodule versions; they do not waive a current code-level reverification.
- Current FrontComposer/Builds working pointers differ from the root-recorded commits but both submodule working trees are clean. Record their actual SHAs and preserve the unrelated pointer changes; do not update, reset, commit, or edit submodule source.
- Relevant UI history includes server authorization fail-closed work and query/freshness changes. Canonical routing changes must not regress these later behaviors.
- The pre-existing sprint edit marks Story 1.0 done and revised Story 1.1 backlog while `epic-1` remains `done`. Do not change epic status in this story-creation step; sprint planning owns that stale aggregate status.

### Latest Technical Information

- .NET 10 guidance confirms Interactive Server renders and processes interactions on the server through a circuit. Global interactivity still requires the server service and endpoint render-mode registrations already present.
- Current Microsoft security guidance keeps access tokens on the server and recommends a BFF/token-handler flow; this supports the existing server-only gateway boundary.
- Current .NET SDK container guidance supports Dockerfile-free `PublishContainer`, explicit repository naming, and non-root execution. Repository pins/configuration remain the implementation authority.
- Aspire 13.4 project resources describe local service relationships; they do not transfer production-orchestration ownership into this domain repository.
- Fluent upstream has stable v4 releases, but this project deliberately pins a v5 rc.4 build through FrontComposer. Do not "upgrade to latest" inside Story 1.1; verify the exact installed API and package resolution.

### Project Context Reference

Follow `_bmad-output/project-context.md` and the root `AGENTS.md`/Hexalith baseline. In particular: preserve user changes; use `.slnx`; centralize package versions; keep one C# type per file; use FrontComposer/Fluent V5; keep tokens/cursors/ETags/payloads/internal ids support-safe; run tests per project; never initialize nested submodules; and do not put shared platform capability into the Tenants domain module.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Reverify UI Host Bootstrap and Canonical Workspace`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Additional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Canonical Architecture Spine`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#5.1 Operations Shell`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Information Architecture`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Layout & Spacing`]
- [Source: `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`]
- [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`]
- [Source: `src/Hexalith.Tenants.UI/Program.cs`; `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`; `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Reverified the existing .NET 10 InteractiveServer host, FrontComposer/Fluent composition, SDK container boundary, and AppHost graph against the immutable baseline.
- Added one immutable, surface-aware workspace state model and synchronized tab, scope, filter, sort, user lookup, cursor, and contextual return state to canonical `/tenants` URLs.
- Added focused tests and conformance guards for canonical transitions, support safety, localization parity/document culture, responsive/direction-safe layout, the production-publication blocker, and compatibility routes.
- Rebuilt and smoke-tested the Aspire UI resource, published the SDK container archive, and completed the individual Release validation gates.

### Debug Log References

- Create-story analysis completed against the full epics and architecture documents, selected current PRD/UX shards, the corrected readiness artifacts, Story 1.0 reverification, historical Story 1.1, current UI/AppHost/build/test files, recent Git history, and current primary technical documentation.
- Pre-change evidence captured in `story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md`; root/UI baseline was preserved, AppHost `tenants-ui` reached Healthy, and the Release UI suite passed 904/904 before implementation.
- Red-phase focused state tests first failed because the new immutable state/sort contracts did not yet exist; after implementation the state suite passed 6/6.
- Final focused suites passed: TenantListSurface 19/19, UserMembershipLookupSurface 11/11, TenantsUiComposition 19/19, DomainUiFluentConformance 51/51; final UI regression passed 916/916.
- Aspire refresh initially exposed a platform-owned `memories` exit-code-1 dependency incident; restarting that resource restored `tenants-ui` to Healthy and the route smoke completed.
- Final code-review remediation passed 65/65 focused state, tenant-list, user-lookup, and My Tenants tests; final Release regressions passed UI 933/933 and Contracts 112/112. Hosted route smoke passed 6/6.
- Code-review remediation solution validation passed in Release with warnings as errors (0 warnings, 0 errors), and `git diff --check` exited 0 with only the pre-existing `sprint-status.yaml` CRLF normalization warning.

### Completion Notes List

- Reverification completed against root baseline `088232a7255698e20105594d9e0ef12a0f09c73e`, FrontComposer `d3761fa08ce2f4bf004e8adc7f500822d04276f8`, Builds `9ec0a032d785dd0abdc14276e8784d6fdd826fd0`, FrontComposer 4.0.1, and Fluent UI `5.0.0-rc.4-26180.1`.
- The UI host remains an SDK-container, non-packable .NET 10 InteractiveServer application. Local SDK image publication is verified; production publication remains blocked on the platform-owned `/alive` and release-authority contracts, so the unsupported `tenants-ui` release mapping was removed. No Dockerfile or AppHost platform plumbing was added.
- Canonical workspace state is deterministic, surface-specific, safely encoded, and fail-safe; compatibility routes still render but generated navigation returns to `/tenants`.
- Authenticated Playwright evidence covers canonical `/tenants`, Users-tab focus, one-main composition, desktop/tablet/mobile overflow and navigation, reduced motion, forced colors, and EN/FR interactive rendering through the supported culture cookie; exact commands and artifacts are retained.
- Story status is `done`; the conservative AC9 production-publication boundary remains a recorded pre-existing defer because platform-owned `/alive` and durable publication-authority prerequisites remain unavailable. `HTTP-TARGET-1` is also recorded as an external hosting-policy gap without inventing a user/search truncation rule.

### File List

- `_bmad-output/implementation-artifacts/1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md`
- `_bmad-output/implementation-artifacts/story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md`
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/authenticated-workspace-en-2026-07-20.yml`
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/authenticated-workspace-fr-2026-07-20.yml`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/App.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/UserTenantMembershipSortColumns.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceEntryPointTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`

### Change Log

- 2026-07-19: Implemented Story 1.1 host/workspace reverification changes, focused evidence, release handoff, and regression coverage; moved status from `ready-for-dev` to `review`.
- 2026-07-19: Applied all 15 code-review patches, added navigation/state regressions, corrected release and evidence claims, and returned the story to `in-progress` for the unresolved AC7/AC9/AC10 external evidence gates.
- 2026-07-20: Applied all 16 rerun patches, fixed static-render/tab-disposal canonical navigation, retained authenticated EN/FR responsive evidence, closed AC7/AC10, and moved the story and sprint entry to `done`; the external AC9 publication contract remains recorded as a pre-existing defer.

### Review Findings

- [x] [Review][Patch] UI container publication mapping cannot pass the shared `/alive` liveness contract [.github/workflows/release.yml:33]
- [x] [Review][Patch] Query identity changes retain stale tenant-list cursor history [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:358]
- [x] [Review][Patch] Tenant-list cursors cross into the Users lookup surface [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:493]
- [x] [Review][Patch] Surface round-trips can show stale tenant rows under reset URL state [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:498]
- [x] [Review][Patch] Sorting user memberships after pagination leaves page-N data under page-one state [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:358]
- [x] [Review][Patch] Same-route query navigation is ignored after component initialization [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:284]
- [x] [Review][Patch] My Tenants pagination does not update canonical cursor state [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:153]
- [x] [Review][Patch] Clear and invalid user lookup transitions leave stale URL state [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:323]
- [x] [Review][Patch] Descending tenant-id sort is silently discarded [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:86]
- [x] [Review][Patch] Workspace state invents a forbidden 256-character user-id limit [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:35]
- [x] [Review][Patch] Canonical sort state cannot restore the grid's active sort presentation [src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:5]
- [x] [Review][Patch] A second Tenants-route serializer duplicates the active navigation helper [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:197]
- [x] [Review][Patch] AC7 and AC10 are overclaimed and browser evidence is not reproducible [_bmad-output/implementation-artifacts/story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md:79]
- [x] [Review][Patch] Direction-safety coverage misses valid physical-position declarations [tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:365]
- [x] [Review][Patch] Release ownership tests can pass on an unrelated string occurrence [tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:305]
- [x] [Review][Defer] Reusable release caller already omits required publication-authority inputs [.github/workflows/release.yml:29] — deferred, pre-existing
- [x] [Review][Defer] Pre-existing submodule pointer upgrades require their own review [references/Hexalith.Builds] — deferred, pre-existing
- [x] [Review][Defer] Epic 1 aggregate status is inconsistent with its child stories [_bmad-output/implementation-artifacts/sprint-status.yaml:52] — deferred, pre-existing

#### Review rerun — 2026-07-19

- [x] [Review][Patch] Normalize descending tenant-id state away when the default sort has no matching grid interaction [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:94]
- [x] [Review][Decision] Attempt the supported runtime evidence now — resolved 2026-07-20: the checked-in Keycloak development principal authenticated successfully, but `/tenants` redirected to itself and the hosted route-smoke suite failed 2/6; publication-authority inputs were absent. AC7/AC10 are patch-blocked and AC9 remains platform-blocked, so the story stays in progress.
- [x] [Review][Patch] Avoid unconditional/same-URL canonical navigation that redirects `/tenants` to itself, while still canonicalizing normalized-equal invalid query URLs before returning [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:323]
- [x] [Review][Patch] Drop cursors when query identity is missing, invalid, or normalized to a different state [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:60]
- [x] [Review][Patch] Enforce EventStore's 4,096-character opaque cursor limit [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:295]
- [x] [Review][Patch] Preserve meaningful caller-supplied user and search whitespace instead of silently trimming identity [src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:300]
- [x] [Review][Patch] Reapply Fluent grid sort state when same-route sort parameters change [src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:18]
- [x] [Review][Patch] Add stable selectors at the sortable Tenant and Status header interactions [src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:12]
- [x] [Review][Patch] Canonicalize My Tenants paging before loading to avoid stale URLs and duplicate compatibility-route queries [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:164]
- [x] [Review][Patch] Avoid querying the compatibility Users route before canonical workspace navigation [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:398]
- [x] [Review][Patch] Prevent a superseded sort lookup from overwriting the newer live announcement [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:382]
- [x] [Review][Patch] Extend direction-safety coverage to asymmetric physical CSS shorthands [tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:508]
- [x] [Review][Patch] Add explicit cursor-reset tests for user-id and sort-direction-only transitions [tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs:72]
- [x] [Review][Patch] Parse the release container map instead of rejecting only one exact UI mapping string [tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:308]
- [x] [Review][Patch] Record the HTTP 414 request-target gap for intentionally unbounded canonical text values [_bmad-output/implementation-artifacts/story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md:121]
- [x] [Review][Patch] Record resolved UI package output and explicit exit codes in the checked evidence report [_bmad-output/implementation-artifacts/story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md:65]
- [x] [Review][Patch] Align the hosted Users compatibility-route smoke assertion with canonical omission of the default tenant sort [tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs:136]

- [x] [Review][Decision] Post-patch runtime rerun resolved AC7/AC10: authenticated EN/FR Playwright assertions passed, `/tenants` returned 200, and hosted route smoke passed 6/6. AC9 remains platform-deferred; all review findings are resolved and the story is `done`.
