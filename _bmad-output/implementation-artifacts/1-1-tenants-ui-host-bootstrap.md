---
baseline_commit: 7310a548f434645cafb809c9bce9946c31809eb8
---

# Story 1.1: Tenants UI Host Bootstrap

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 1.1. -->

## Story

As a platform operator,
I want to open the Tenants workspace inside the shared FrontComposer shell,
so that Tenants read workflows start from a real hosted UI surface rather than local scaffolding or mock screens.

## Acceptance Criteria

1. Given Story 1.0 has confirmed the minimum FrontComposer shell composition path in `story-1-0-spike-note-2026-06-05.md`, when the Tenants UI host is created, then `src/Hexalith.Tenants.UI` exists as a .NET 10 Blazor InteractiveServer web project using `Microsoft.NET.Sdk.Web`, and it is added to `Hexalith.Tenants.slnx` without creating a `.sln` file.
2. Given the UI must compose through shared Hexalith shell infrastructure, when the application starts, then the root Tenants workspace renders through the FrontComposer shell and Fluent UI Blazor v5 conventions, and no generic shell, DI, serialization, or event-store boilerplate is duplicated inside Hexalith.Tenants.
3. Given the browser must not call backend services directly, when the UI host is configured, then it contains server-side composition points for future BFF query and command gateways, and the browser has no backend access token storage and no direct backend API client.
4. Given the Tenants UI must run in local Aspire development, when the existing `Hexalith.Tenants.AppHost` is started after the project is added, then the UI host is registered as a resource with the existing Tenants service dependencies and auth configuration path, and `EnableKeycloak=false` remains compatible with the existing symmetric-key JWT development mode.
5. Given the UI host must be package/release compatible, when the project is built for container output, then it uses .NET SDK container support with `ContainerRepository=tenants-ui`, and no Dockerfile is added.
6. Given no tenant read story has been implemented yet, when an authorized or unauthenticated user reaches the Tenants workspace route, then the page shows an honest unavailable/not-yet-connected state rather than mock tenant data or fabricated success, and the state uses Tenants-owned `.resx` copy, accessible semantics, visible focus, forced-colors-safe styling, and stable selectors such as `data-testid="tenants-shell-status"`.
7. Given the bootstrap is complete, when tests run for the UI project, then component or smoke tests verify shell rendering, the no-mock-data unavailable state, key selectors, and localization resource lookup, and the story identifies any Playwright smoke coverage needed to verify the route in Aspire once the UI host is discoverable.

## Tasks / Subtasks

- [x] Create the UI host project and add it to the solution (AC: 1, 5)
  - [x] Add `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` using `Microsoft.NET.Sdk.Web`, `net10.0`, `IsPackable=false`, `EnableContainer=true`, and `ContainerRepository=tenants-ui`.
  - [x] Add the project to `Hexalith.Tenants.slnx`; do not create or use a `.sln`.
  - [x] Reference `Hexalith.Tenants.Client` and the shared FrontComposer Shell implementation. Prefer source `ProjectReference` to the `references/Hexalith.FrontComposer` submodule when building in this repo; do not copy FrontComposer shell code into Tenants.
  - [x] Add any needed central package versions to `Directory.Packages.props`; never put package versions in a `.csproj`.

- [x] Wire the Blazor InteractiveServer root through FrontComposer (AC: 1, 2, 6)
  - [x] Add `Program.cs`, `Components/App.razor`, `Components/Routes.razor`, `_Imports.razor`, `Components/Layout/MainLayout.razor`, and app settings/launch settings using the existing .NET 10 conventions.
  - [x] Register Razor components with `.AddInteractiveServerComponents()` and use InteractiveServer render mode. Architecture D1 supersedes the UX spine's earlier Blazor Auto assumption.
  - [x] Register Fluent UI services and FrontComposer in the confirmed order: Fluent components, `AddHexalithFrontComposerQuickstart(...)`, Tenants domain/manifest registration, then EventStore-backed integration only when the required Tenants options are available.
  - [x] Because no generated Tenants projection pages exist yet, register only a minimal Tenants domain/nav manifest for shell reachability. Do not add fake projection descriptors or mock generated pages.
  - [x] Compose layout as `<FrontComposerShell>@Body</FrontComposerShell>`; do not build a Tenants-owned generic shell, sidebar, theme system, or shell DI wrapper.

- [x] Add a minimal Tenants workspace route with an honest unavailable state (AC: 2, 6)
  - [x] Add a root or Tenants workspace page that renders inside the FrontComposer shell.
  - [x] Show a localized "not yet connected / read surfaces not implemented" state, not sample tenants, fabricated counts, or fake success.
  - [x] Include `data-testid="tenants-shell-status"` and any needed stable selectors for route smoke tests.
  - [x] Use accessible markup: visible heading or status region, programmatic status semantics, visible focus path, no color-only meaning, and forced-colors-safe CSS.

- [x] Add Tenants-owned localization for bootstrap copy (AC: 6, 7)
  - [x] Add `Resources/TenantsResources.resx` and `Resources/TenantsResources.fr.resx` or the localizer structure used by the project.
  - [x] Use dotted PascalCase keys under a `Tenants.` root and whole strings with named placeholders only.
  - [x] Keep shell chrome strings in FrontComposer resources; Tenants owns only domain/workspace copy.

- [x] Add server-side composition seams for future BFF gateways without implementing later read stories (AC: 3)
  - [x] Add interfaces/classes or placeholder registrations under `Services/` for future query/command gateways only if they are needed to prove server-side-only composition.
  - [x] Ensure browser code does not store access tokens and does not call Tenants/EventStore APIs directly.
  - [x] If adding authentication/context accessors, wire `IUserContextAccessor` from authenticated claims with null/empty output for unauthenticated users, and let FrontComposer's default tenant-context gate fail closed.

- [x] Register the UI host in Aspire AppHost (AC: 4)
  - [x] Add a new `HexalithTenantsUI : IProjectMetadata` class following the existing `HexalithTenants` and sample metadata pattern.
  - [x] Update `src/Hexalith.Tenants.AppHost/Program.cs` to add the UI project resource, reference the Tenants service and EventStore resources, wait for required dependencies, expose external HTTP endpoints, and pass auth configuration from Keycloak when enabled.
  - [x] Preserve the existing `EnableKeycloak=false` branch; symmetric-key development mode must continue to work.
  - [x] Do not add generic AppHost/Aspire scaffolding outside the existing pattern.

- [x] Add UI bootstrap tests (AC: 7)
  - [x] Add `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` with bUnit, xUnit v3, Shouldly, and required project references.
  - [x] Add component/smoke tests for shell composition, the unavailable state, `data-testid="tenants-shell-status"`, and localization resource lookup.
  - [x] Add or document the future Playwright/Aspire smoke scenario for route discoverability; do not block this story on full tenant-list E2E coverage.
  - [x] Run UI tests individually and run a Release build with warnings as errors.

## Dev Notes

### Scope

This story is the UI host bootstrap only. It may add the host, shell composition, AppHost registration, localization, an honest placeholder workspace state, and test scaffolding for those pieces. It must not implement tenant list triage, tenant detail, member tables, command flows, audit grids, consequence previews, or mock tenant data. Story 1.2 owns the tenant list and must resolve the `FC-TBL` caveat before list implementation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Tenants UI Host Bootstrap`; `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#4. FC-TBL`]

### FrontComposer Integration Facts

- Story 1.0 is complete and gives a GO for Story 1.1. It confirms `FC-LYT`, shell registration APIs, manifest registration, projection routing, Fluxor, JWT/auth registration, `FC-A11Y`, `FC-L10N`, and `FC-DOC`. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#TL;DR`]
- Confirmed registration APIs live in `Hexalith.FrontComposer.Shell.Extensions`: `AddHexalithFrontComposerQuickstart(...)`, `AddHexalithFrontComposer(...)`, `AddHexalithDomain<T>()`, `AddHexalithEventStore(...)`, `AddHexalithFrontComposerAuthentication(...)`, and `AddHexalithShellLocalization(...)`. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#1. Shell-integration APIs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`]
- FrontComposer validates call order at startup. The order is FrontComposer first, domain registration next, EventStore integration last. Mis-ordering throws before first render through `FrontComposerBootstrapValidationGate`. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#1. Shell-integration APIs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapValidator.cs`]
- `AddHexalithDomain<T>()` discovers generated `*Registration` classes with a static `Manifest` and `RegisterDomain(IFrontComposerRegistry)`. Story 1.1 has no generated Tenants projections yet, so use a minimal registration path that adds a Tenants nav/domain manifest without pretending read projections exist. Manual registration through `IFrontComposerRegistry.RegisterDomain(new DomainManifest(...))` is acceptable if it follows the FrontComposer contract and stays bootstrap-only. [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Registration/IFrontComposerRegistry.cs`]
- `MainLayout.razor` should collapse to the confirmed shell wrapper pattern: `<FrontComposerShell>@Body</FrontComposerShell>`. The Counter sample uses this directly. [Source: `references/Hexalith.FrontComposer/samples/Counter/Counter.Web/Components/Layout/MainLayout.razor`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`]
- FrontComposer Shell supplies skip links, layout regions, navigation auto-population from registered manifests, `FluentProviders`, projection connection status, pending command summary, keyboard support, and page layout coordination. Do not recreate these in Tenants. [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`]
- Consumer must provide a real or fail-closed `IUserContextAccessor`. It returns `TenantId` and `UserId`, with null/empty/whitespace meaning unauthenticated. FrontComposer's default `IFrontComposerTenantContextAccessor` blocks missing, malformed, mismatched, or synthetic tenant/user context when demo context is not allowed. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#1. Shell-integration APIs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy/FrontComposerTenantContextAccessor.cs`]
- `IStorageService` and FrontComposer per-circuit services are scoped. Do not capture scoped FrontComposer services in singletons. [Source: `_bmad-output/project-context.md#Host Composition & Framework Rules`; `references/Hexalith.FrontComposer/_bmad-output/project-context.md#Blazor Shell & Fluxor Rules`]

### Architecture Requirements

- Runtime model is Blazor InteractiveServer with a server-side BFF in the UI host. The browser never owns backend access tokens and never calls Tenants/EventStore directly. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- The UI owns no datastore. The Fluxor store is an ephemeral runtime cache; projections are authoritative. This bootstrap should not add local persistence for Tenants domain data. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Backend egress belongs in server-side `Services/Gateways/`; components dispatch intent and never call gateways directly. This story can add seams/placeholders, but real query/command behavior belongs to later stories. [Source: `_bmad-output/planning-artifacts/architecture.md#File Organization Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]
- Use `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` DTOs for future BFF types. Never redeclare DTOs, re-case wire fields, or parse `TenantId`/`UserId` as GUID/ULID. [Source: `_bmad-output/project-context.md#Identity Rules`; `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#E. Naming & ID hazards`]
- UI host containerization uses .NET SDK container support, not Dockerfiles. Set `EnableContainer=true` and `ContainerRepository=tenants-ui`; root `Directory.Build.targets` supplies common container defaults. [Source: `_bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment`; `Directory.Build.targets`]

### Existing Files To Update And Preserve

- `Hexalith.Tenants.slnx` currently includes AppHost, Client, host, Contracts, Server, Testing, samples, and selected EventStore/Commons submodule projects. Add the UI and UI test project entries in the existing folder structure. Preserve `.slnx`; do not create `.sln`. [Source: `Hexalith.Tenants.slnx`]
- `Directory.Packages.props` centralizes package versions. It currently has DAPR, Aspire, Microsoft.Extensions, application, and testing package groups. Add missing package versions only here. FrontComposer source pins Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, SignalR client `10.0.8`, and OIDC auth `10.0.8`; keep compatibility with the FrontComposer pin. [Source: `Directory.Packages.props`; `references/Hexalith.FrontComposer/Directory.Packages.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- `Directory.Build.props` already sets `net10.0`, nullable, implicit usings, `TreatWarningsAsErrors`, package metadata, and `HexalithEventStoreRoot` auto-detection. Do not break existing EventStore root detection. If a FrontComposer root property is needed for source `ProjectReference`, add it narrowly and consistently rather than hardcoding paths in multiple projects. [Source: `Directory.Build.props`]
- `src/Hexalith.Tenants.AppHost/Program.cs` currently wires EventStore, Admin Server, Admin UI, Tenants, sample, DAPR configs, and Keycloak auth. Preserve EventStore domain-module wiring, DAPR config resolution, the `EnableKeycloak=false` branch, and `ConfigureAwait(false)` on the final await. [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- `src/Hexalith.Tenants.AppHost/*` metadata classes wrap project paths through `ProjectMetadataPaths.GetProjectPath(...)` with `SuppressBuild => true`. Add `HexalithTenantsUI` using the same pattern. [Source: `src/Hexalith.Tenants.AppHost/HexalithTenants.cs`; `src/Hexalith.Tenants.AppHost/ProjectMetadataPaths.cs`]
- `tests/Directory.Build.props` imports root build props and already supplies xUnit v3, Shouldly, NSubstitute, coverlet, and Microsoft.NET.Test.Sdk. A UI test project can use this shared test setup and add bUnit separately if needed. [Source: `tests/Directory.Build.props`]

### UX, Accessibility, And Localization Requirements

- The first viewport is the actual Tenants workspace surface inside FrontComposer, not a marketing/landing page. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Tenants UI Host Bootstrap`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Information Architecture`]
- Use Fluent UI Blazor v5 through FrontComposer. Do not invent a palette, hard-code hex colors, or assert unverified Fluent token names. [Source: `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Brand & Style`]
- The placeholder state must be honest: no mock data, no "success", no fake counts, no implied read support before Story 1.2+ implements reads. Use neutral/not-yet-available semantics. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Tenants UI Host Bootstrap`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#The honesty contract`]
- Bootstrap copy is Tenants-owned `.resx` copy. Use whole strings with named placeholders and dotted PascalCase keys under `Tenants.`. Shell chrome remains FrontComposer-owned. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`]
- Per-story evidence still applies even though `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed. This story needs component/smoke evidence for keyboard reachability of the placeholder, visible focus, no-color-only status, forced-colors-safe styling, localization lookup, and documentation/reference traceability. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-05.md#4.8 UX - update FrontComposer readiness table and remove duplicate fallback bullet`]

### Anti-Patterns To Avoid

- Do not initialize nested submodules or modify `Hexalith.FrontComposer` files for this story.
- Do not add generic shell, theme, navigation, serialization, EventStore, DI, test harness, or Aspire scaffolding to Tenants when FrontComposer/EventStore already provide it.
- Do not add Dockerfiles.
- Do not store bearer tokens in browser storage or add browser-side backend HTTP clients.
- Do not add backend endpoints for reads, command status, previews, receipts, or shell bootstrap.
- Do not implement Story 1.2 tenant list or any later read/command surfaces under this bootstrap story.
- Do not use raw `Assert.*` in tests; use Shouldly and xUnit v3 conventions.
- Do not add copyright headers or package versions in `.csproj` files.

### Latest Technical Information

Use the repo-pinned versions and APIs as the implementation authority:

- .NET SDK `10.0.300`, target `net10.0`, `TreatWarningsAsErrors=true`. [Source: `global.json`; `Directory.Build.props`]
- Aspire AppHost SDK `13.4.2` in this repo. [Source: `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`; `Directory.Packages.props`]
- FrontComposer Shell package/project depends on Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, SignalR client `10.0.8`, and OIDC auth `10.0.8`. Do not upgrade these as part of Story 1.1; verify exact component/API names against the local source and pinned packages. [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`; `references/Hexalith.FrontComposer/Directory.Packages.props`]
- If adding bUnit, align the central package version with the FrontComposer test stack unless a Tenants-specific reason is documented. FrontComposer currently pins `bunit` `2.8.1-preview`. [Source: `references/Hexalith.FrontComposer/Directory.Packages.props`]
- xUnit package versions in this repo are pre-release v4 package IDs for xUnit v3 usage; follow current repo test project patterns, not older artifact text that names `3.2.2`. [Source: `Directory.Packages.props`; `tests/Directory.Build.props`]

### Testing Standards

- Run test projects individually; use `.slnx` for restore/build only. Do not run solution-level `dotnet test` as the Tenants default. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Suggested verification:
  - `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror`
  - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release`
  - Existing Tier 1 test projects if package/build files changed broadly.
- If Aspire smoke is added or manually verified, restart `aspire run` after AppHost changes because the app model is built at startup. [Source: `_bmad-output/project-context.md#Host Composition & Framework Rules`]

### Project Structure Notes

- New source should live under `src/Hexalith.Tenants.UI/`, with `Components/`, `Services/`, `Resources/`, and `wwwroot/css/` as needed for bootstrap. Keep future `State/`, `Vocabulary/`, and surface folders minimal unless the bootstrap needs them. [Source: `_bmad-output/planning-artifacts/architecture.md#Structure Patterns`; `_bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure`]
- New tests should live in `tests/Hexalith.Tenants.UI.Tests/`. Do not co-locate tests under `src/`. [Source: `_bmad-output/planning-artifacts/architecture.md#Structure Patterns`; `tests/Directory.Build.props`]
- The planned UI tree in architecture is broader than this story. Treat it as the north star, not permission to build every planned component in Story 1.1.
- `FC-TBL` remains caveated and is not a bootstrap blocker. Do not consume FrontComposer's generated projection grid for the tenant list in this story. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md#4. FC-TBL`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`]

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.1: Tenants UI Host Bootstrap`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- Spike evidence: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`, `#Project Structure & Boundaries`, `#Implementation Patterns & Consistency Rules`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`, `#C. Backend surfaces consumed`, `#G. Canonical state sets`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`, `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- Persistent project rules: `_bmad-output/project-context.md`, `references/Hexalith.FrontComposer/_bmad-output/project-context.md`
- Existing files: `Hexalith.Tenants.slnx`, `Directory.Packages.props`, `Directory.Build.props`, `Directory.Build.targets`, `src/Hexalith.Tenants.AppHost/Program.cs`, `tests/Directory.Build.props`

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- Create-story artifact analysis completed against `epics.md`, `architecture.md`, nested PRD addendum, UX design/experience spines, Story 1.0 spike note, persistent project context, FrontComposer source snippets, and existing solution/AppHost/build files.
- 2026-06-05: Implemented Tenants UI bootstrap using source FrontComposer project references and a generated-style minimal Tenants `DomainManifest` registration with no projections or commands.
- 2026-06-05: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false -p:NuGetAudit=false` restored successfully but was blocked by the .NET 10/Microsoft.Testing.Platform `dotnet test` VSTest target error; local execution used the repo-established xUnit v3 in-process executable fallback.
- 2026-06-05: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 -nr:false -p:NuGetAudit=false -warnaserror` passed with 0 warnings and 0 errors.
- 2026-06-05: Focused xUnit v3 in-process test lanes passed: Contracts 103/103, Client 47/47, Testing 181/181, UI 7/7, Sample 31/31.
- 2026-06-05: Tier 2 Server test executable was attempted and remains blocked by pre-existing missing baseline artifacts: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- 2026-06-05: Direct UI host startup was attempted with `dotnet run --project src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj -c Release --no-build --urls http://127.0.0.1:62448`; Kestrel socket binding is denied in this sandbox with `SocketException (13): Permission denied`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 1.1 is gate-cleared by the completed Story 1.0 spike.
- Remaining known gate is `FC-TBL`, which belongs to Story 1.2 tenant-list implementation, not this bootstrap.
- Added the .NET 10 Blazor InteractiveServer Tenants UI host, containerized as `tenants-ui`, and registered it in `Hexalith.Tenants.slnx`.
- Composed the UI through FrontComposer Shell with Fluent UI services, a minimal Tenants domain/nav manifest, and optional EventStore integration only when `EventStore:BaseAddress` is configured.
- Added an honest localized Tenants workspace placeholder at `/` and `/tenants` with `data-testid="tenants-shell-status"`, status semantics, focusable status link, and forced-colors-safe CSS.
- Added server-side-only BFF composition seams and a fail-closed authenticated-claims `IUserContextAccessor`; no browser token storage or browser-side backend API client was added.
- Registered `tenants-ui` in the Aspire AppHost with Tenants/EventStore references, external HTTP endpoints, and Keycloak auth environment propagation while preserving the `EnableKeycloak=false` path.
- Added UI bootstrap tests and governance updates so the UI project is classified as a non-packable host and included in blocking CI/release test lanes.
- Future Playwright/Aspire smoke coverage should discover the `tenants-ui` external endpoint from Aspire state, navigate to `/tenants`, and verify `data-testid="tenants-shell-status"` renders inside the FrontComposer shell; full tenant-list E2E remains Story 1.2+ scope.

### File List

- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `Directory.Build.props`
- `Directory.Packages.props`
- `Hexalith.Tenants.slnx`
- `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.AppHost/HexalithTenantsUI.cs`
- `src/Hexalith.Tenants.AppHost/Program.cs`
- `src/Hexalith.Tenants.UI/Components/App.razor`
- `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor.css`
- `src/Hexalith.Tenants.UI/Components/Routes.razor`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerDomain.cs`
- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Properties/launchSettings.json`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/ClaimsUserContextAccessor.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/appsettings.Development.json`
- `src/Hexalith.Tenants.UI/appsettings.json`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

### Change Log

- 2026-06-05: Implemented Story 1.1 Tenants UI host bootstrap and moved story to review.
- 2026-06-05: Story-automator adversarial review (auto-fix). Fixed a11y focus-target gap on the
  workspace status region (added `tabindex="-1"` + visible/forced-colors focus outline so the
  in-page focus link moves keyboard focus, not just the viewport); extended the bUnit focus test to
  assert the focusable target; corrected three File List omissions (`Program.cs`, the integration
  route smoke test, and the modified Aspire topology fixture). Release build clean (0 warnings) and
  UI tests 7/7. Status moved review → done (0 critical issues).

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-05 (story-automator non-interactive, auto-fix)

**Outcome:** Approve (status → done). No CRITICAL or HIGH issues. All 7 acceptance criteria are
implemented and every `[x]` task was verified against the actual implementation, git changes, and
the FrontComposer source contract.

**Verification performed:**

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror`
  → succeeded, 0 warnings / 0 errors (transitively builds `Hexalith.Tenants.UI` and the FrontComposer
  Shell/Contracts source references).
- `Hexalith.Tenants.UI.Tests` xUnit v3 in-process runner → 7/7 passing (after fix).
- FrontComposer contract cross-check: `AddHexalithFrontComposerQuickstart` / `AddHexalithDomain<T>` /
  `AddHexalithEventStore` signatures, `IFrontComposerRegistry` / `DomainManifest` shape, the
  Quickstart → Domain → EventStore bootstrap ordering enforced by `FrontComposerBootstrapValidator`,
  and the `<FrontComposerShell>@Body</FrontComposerShell>` layout pattern all match the implementation.
- `<html lang="en">` and the `MapStaticAssets()` + `UseStaticFiles()` pairing match the canonical
  FrontComposer Counter sample the story cites — not Tenants defects, so not flagged.

**Findings fixed (MEDIUM):**

- A11y (AC6): workspace status region was not programmatically focusable, so the "Review status
  details" fragment link scrolled to it without moving keyboard focus — inconsistent with the shell's
  own `tabindex="-1"` skip targets. Added `tabindex="-1"` + a visible (forced-colors-safe) focus
  outline and a regression assertion.
- Documentation: File List omitted `src/Hexalith.Tenants.UI/Program.cs`,
  `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`, and the modified
  `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`. Corrected.

**Advisory (LOW — not blocking, deferred to later stories):**

- AppHost propagates `Authentication__JwtBearer__*` to the `tenants-ui` resource, but the UI host
  configures no authentication scheme yet, so that environment is currently inert. This is acceptable
  forward-wiring — authentication consumption is out of scope for the bootstrap — but should be wired
  when the first authenticated read surface lands (Story 1.2+).
- `TenantsUiCompositionTests` asserts raw file text for the layout/CSS checks (a pragmatic bUnit
  compromise since the full shell cannot be hosted in-component); behavior coverage lives in the
  workspace render tests and the Aspire route smoke test.
