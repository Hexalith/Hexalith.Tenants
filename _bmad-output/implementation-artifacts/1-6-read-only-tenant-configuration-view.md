---
created: 2026-06-06T02:07:42+02:00
baseline_commit: 4f059a1
---

# Story 1.6: Read-Only Tenant Configuration View

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 1.6. -->

## Story

As an authorized tenant user,
I want to inspect tenant configuration values grouped by namespace,
so that I can understand tenant setup without exposing or changing configuration outside my scope.

## Acceptance Criteria

1. Given an authorized user opens the configuration section for a tenant, when configuration data is available from the existing tenant detail projection, then the UI shows read-only key/value entries grouped by namespace and values outside the caller's authorized namespace or prefix are not shown.
2. Given configuration values include sensitive-value candidates or unknown sensitivity, when the configuration section renders, then sensitive-value display remains outside the read MVP and the UI fails closed with localized unavailable text rather than exposing payloads, secrets, raw metadata, tokens, internal correlation ids, stack traces, real PII, or backend error details.
3. Given the user filters or scans namespaces, when results change, then grouped namespace context, tenant freshness, and authorization scope remain visible, and empty and filtered-empty states remain distinct.
4. Given configuration projection data is stale, degraded, unavailable, unauthorized, or unknown, when the configuration section renders, then the actual state is displayed with no false Success and no mutation affordance, and stale or unknown freshness blocks any future command preview entry point.
5. Given configuration keys or safe values are visually truncated, when the user inspects the row, then literal support-safe text remains available through accessible labels or future safe-copy affordances, and caller-supplied tenant/configuration identifiers are never parsed or reformatted as GUIDs or ULIDs.
6. Given the configuration section is used on desktop, tablet, or mobile, when layout changes, then namespace, key, value safety state, and freshness remain understandable, and keyboard navigation, row headers, focus order, forced-colors behavior, live-region announcements, and stable selectors such as `data-testid="tenants-config-table"` are preserved.
7. Given this story is complete, when verification is run, then component or gateway-adapter tests cover tenant-detail configuration source usage, namespace grouping, prefix filtering or fail-closed unavailable behavior, sensitive-value fail-closed behavior, empty/filtered-empty/stale/degraded/unavailable/unauthorized states, no mutation controls, localization parity, keyboard traversal, forced-colors-safe rendering, and stable selectors.

## Tasks / Subtasks

- [x] Add a read-only tenant configuration section under the existing tenant detail route (AC: 1, 3, 4, 6)
  - [x] Prefer a focused component such as `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` and `.razor.css`; compose it from `TenantDetailPage.razor` so `/tenants/{TenantId}` remains the source route.
  - [x] Use the existing `ITenantQueryGateway.GetTenantAsync` / `TenantDetailSnapshot` path and `TenantDetail.Configuration`; do not add backend endpoints, query contracts, copied DTOs, or browser-side backend calls.
  - [x] Render namespace groups from configuration keys using the first `.` segment as the namespace and the full key as the literal key; keys without `.` must land in a localized unscoped/other group rather than being dropped.
  - [x] Add namespace scan/filter controls with stable selectors such as `tenants-config-filter`, `tenants-config-clear-filter`, `tenants-config-group`, `tenants-config-row`, `tenants-config-key`, `tenants-config-value-state`, and `tenants-config-truth-state`.
  - [x] Keep switching between overview and configuration inside the tenant detail context without losing the selected tenant, freshness state, or list return URL.

- [x] Enforce authorization, namespace, and sensitive-value safety in the UI slice (AC: 1, 2, 3, 5)
  - [x] Treat `TenantDetail.Configuration` as already server-authorized projection data, but do not imply it proves every consumer-owned prefix is visible; surface copy as "visible configuration" or equivalent support-safe language.
  - [x] If no server-side prefix ownership signal exists in this story, fail closed for explicit "my prefix only" claims: show a localized unavailable or scope-limited message and do not fabricate prefix ownership.
  - [x] Add a conservative sensitive-value classifier for display only if needed by this read-only UI; candidates such as keys containing `secret`, `password`, `token`, `credential`, `connectionstring`, or unknown sensitivity must show localized unavailable/redacted text rather than the stored value.
  - [x] Do not log, render, serialize into markup, copy, or expose raw sensitive candidate values, EventStore metadata, cursor contents, command payloads, stack traces, internal correlation ids, bearer tokens, decoded JWT payloads, or real PII.
  - [x] Do not add set/remove/edit configuration affordances, command lifecycle panels, consequence previews, audit proof, bulk actions, inline mutations, or hidden disabled mutation buttons.

- [x] Preserve detail truth-state behavior and section states (AC: 3, 4, 6)
  - [x] Reuse the current detail state handling for `Loading`, `Stale`, `Degraded`, `Unauthorized`, `NotFound`, `Unavailable`, and `Unknown`; the configuration section must not render unauthorized or unavailable data as current.
  - [x] Display tenant freshness with the existing `TruthStateBadge`; if freshness is `Unknown` or `Stale`, present read-only data honestly and mark any future command entry point unavailable.
  - [x] Add distinct empty and filtered-empty states for configuration: no visible configuration in the projection is different from a namespace filter hiding all currently visible rows.
  - [x] Use a live region for filter result changes and state transitions; assertive announcements are reserved for unavailable, unauthorized, degraded, or unable-to-verify states.
  - [x] Keep color non-authoritative: use text, accessible labels, semantic structure, and forced-colors-safe CSS for value safety and freshness states.

- [x] Add localized copy and stable accessibility semantics (AC: 2, 3, 5, 6)
  - [x] Add whole-string `Tenants.Configuration.*` resources to `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx` with EN/FR parity.
  - [x] Include resource keys for title, description, namespace filter label/help, clear filter, table caption, namespace/group labels, key/value/safety/freshness headers, empty, filtered-empty, sensitive/unavailable value, stale, degraded, unauthorized, unavailable, announcements, and any unscoped namespace label.
  - [x] Use table or grid semantics with real headers and row relationships. Do not rely on row text or color for tests.
  - [x] Render keys and support-safe values in monospace with `overflow-wrap: anywhere`; if text is visually truncated, keep the full support-safe string available to assistive technology.
  - [x] Preserve keyboard operation for section navigation, filter input, clear action, row traversal, refresh/back controls, and any future safe-copy placeholder.

- [x] Add focused verification (AC: 1-7)
  - [x] Extend `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` or add `TenantConfigurationViewTests.cs` for grouped namespace rendering, unscoped keys, empty/filtered-empty behavior, sensitive candidate redaction, stable selectors, no mutation affordances, and accessible full key/value labels.
  - [x] Extend gateway tests only if gateway behavior changes; otherwise assert the configuration view consumes `TenantDetail.Configuration` from the existing detail snapshot without introducing a second transport.
  - [x] Add tests for stale/degraded/unauthorized/unavailable/unknown detail states so configuration does not collapse into current or empty success.
  - [x] Add resource parity tests for `Tenants.Configuration.*` keys in invariant and French `.resx` files.
  - [x] Add CSS or component coverage for responsive layout, forced-colors hooks, focus-visible styling, stable row footprints, and no-overlap behavior.
  - [x] Run `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` after restore, plus `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` or the established xUnit v3 in-process runner fallback if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

Story 1.6 delivers FR6 and part of FR7: a read-only tenant configuration view inside tenant detail. This story is a UI/read-model composition slice. It must not implement configuration edit/remove commands from Epic 3, sensitive-value reveal, audit evidence, consequence previews, or new backend capabilities. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Read-Only Tenant Configuration View`; `docs/tenants-ui-operations-shell-spec.md#3.1 Tenant detail sections (context preserved across all)`]

### Existing Implementation Context

- Story 1.1 already created the Blazor InteractiveServer UI host, FrontComposer shell composition, Tenants domain registration, AppHost wiring, localization resources, BFF seams, and UI test project. Do not recreate host, shell, AppHost, ServiceDefaults, or package infrastructure. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Program.cs`]
- Story 1.2 created `ITenantQueryGateway`/`TenantQueryGateway`, the tenant list, `TenantDataGrid`, `TruthStateBadge`, `ListSurfaceStates`, stable selector conventions, resource patterns, and gateway/component tests. Reuse those patterns for state, selectors, and safety rendering. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- Story 1.3 added `/tenants/{TenantId}`, list return-context preservation, `TenantDetailSnapshot`, detail localized states, and a configuration summary. Story 1.6 should deepen that configuration summary into a read-only section without breaking detail deep links or return behavior. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- Story 1.4 and Story 1.5 added user membership read surfaces, target-aware BFF behavior, selector prefixes, EN/FR resource expansion, and the known `dotnet test` fallback pattern. Keep Users contextual and do not let configuration work modify My Tenants or user lookup behavior. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Previous Story Intelligence`; `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Verification evidence`]

### Contract And Backend Requirements

- The source contract is `TenantDetail`: `TenantId`, `Name`, `Description`, `Status`, `Members`, `Configuration`, and `CreatedAt`. `Configuration` is `IReadOnlyDictionary<string, string>`; there is no sensitivity flag, namespace ownership DTO, last-updated timestamp per key, audit reference, or edit capability in this response. Do not fabricate missing fields. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`]
- `TenantQueryGateway.GetTenantAsync` submits `GetTenantQuery` through `IEventStoreGatewayClient`, passes `If-None-Match`, preserves a cached snapshot on `304`, maps stale/degraded metadata to `TenantDetailSurfaceKind.Stale`/`Degraded`, and maps `401`/`403`/`404`/`503` to safe detail states. Preserve this behavior. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#GetTenantAsync`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`]
- The backend route is `GET /api/tenants/{tenantId}`. It validates the literal tenant id, uses authenticated `sub` as the requester, sets `EntityId = tenantId`, and dispatches `GetTenantQuery`; the browser must not call this endpoint directly. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetTenantAsync`]
- `GetTenantQueryHandler` authorizes by tenant membership or global administrator role and returns `model.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)`. The query is projection-backed; the UI must not read state stores, events, or projections directly. [Source: `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Tenant configuration is consumer namespaced by dot-prefix. Consuming services filter by their own prefix and ignore other namespaces. Because the current `TenantDetail` response has no explicit caller-owned prefix list, this UI must avoid claiming ownership beyond what the authorized projection supplies. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `_bmad-output/planning-artifacts/epics.md#FR6`]

### UX, Accessibility, And Safety Requirements

- Configuration read-only is `ui-05-tenant-configuration-read-only`: source is `GET /api/tenants/{tenantId}` / configuration read model, freshness is ETag/304 plus marker, authorization is read-only, sensitive-value handling is deferred, and localized empty/error copy is required. [Source: `docs/tenants-ui-operations-shell-spec.md#4. User Lookup and Global Administrator Read-Only Surfaces`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]
- Tenant detail sections are Overview, Members, Configuration, Command state, and Audit evidence; switching sections must preserve tenant context. Command state and audit proof are forward-looking and out of scope for this read-only story. [Source: `docs/tenants-ui-operations-shell-spec.md#3.1 Tenant detail sections (context preserved across all)`]
- `ui-05` has `blockedBy: [FC-LYT, FC-A11Y, FC-L10N, FC-DOC]`, does not consume `FC-TOK`, and can be implemented with the current shell/layout and table primitives while recording any shared FrontComposer gaps rather than patching a submodule. [Source: `docs/tenants-ui-responsive-layout-and-visual-system-spec.md#6.3 Consuming rows with verbatim blockedBy`; `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]
- Support-safety is hard: no surface, copy action, log, receipt, toast, or error may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or real PII. Sensitive configuration display is outside the read MVP. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/epics.md#Story 1.6: Read-Only Tenant Configuration View`]
- Use Tenants-owned `.resx` resources with whole strings and named placeholders where possible; do not assemble localized sentence fragments at runtime. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/project-context.md#Technology Stack & Versions`]

### Scope Boundaries

- Do not add backend endpoints, new query contracts, EventStore server plumbing, generic UI framework scaffolding, package versions in `.csproj`, Dockerfiles, `.sln` files, copied DTOs, or shared test harness helpers. [Source: `AGENTS.md#Domain Implementation Boundary`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Do not modify root-declared submodules under `references/`. Missing reusable table, redaction, accessibility, localization, or FrontComposer capability should be recorded as a follow-up unless this story can solve it entirely inside Tenants' read-only UI surface. [Source: `AGENTS.md#Submodule Policy`; `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Do not implement Story 1.7 member table, Story 1.8 copy support/readiness evidence, Epic 3 set/remove configuration commands, command lifecycle tracking, consequence preview, audit proof, global administrator review, or any mutation affordance. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`; `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: compose the configuration view and preserve `/tenants/{TenantId}`, loading/unauthorized/not-found/unavailable/stale/degraded behavior, identity summary, member summary, freshness badge, and safe return URL.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`: preserve responsive grid, `overflow-wrap: anywhere`, forced-colors hooks, and focus-visible styling; extend without causing text overlap.
- `src/Hexalith.Tenants.UI/Components/Tenants/`: add `TenantConfigurationView.razor` and `.razor.css` if using a separate component.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Configuration.*` keys with parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`: extend or split focused configuration component tests while preserving existing detail/list context tests.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`: only update if gateway behavior changes; existing detail tests already prove query construction, ETag, safe state mapping, and stale/degraded metadata.

### Previous Story Intelligence

- Story 1.5 showed that generalized shared state can regress another surface when a new state is not rendered. If this story adds a new configuration-specific state, render it everywhere it can appear and add a regression test so it cannot fall through to an empty table or false Success. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Senior Developer Review (AI)`]
- Story 1.5 separated selector/resource prefixes for My Tenants and user lookup. Story 1.6 should use `tenants-config-*` selectors and `Tenants.Configuration.*` resources so tests and localized copy do not depend on overview or list labels. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Completion Notes List`]
- Story 1.3 already requires `ProjectionActorType = TenantProjectionRouting.ActorTypeName` on detail queries, and the current gateway tests cover it. Do not remove projection routing while adding configuration behavior. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md#Senior Developer Review (AI)`]
- Story 1.5 verification passed Release build and the UI in-process xUnit v3 suite; `dotnet test` may still hit the known .NET 10 Microsoft.Testing.Platform/VSTest target issue, so use the in-process runner fallback only when needed and report it. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Debug Log References`]

### Git Intelligence

- Recent story commits are `feat(story-1.1): Tenants UI Host Bootstrap`, `feat(story-1.2): Tenant List Triage`, `feat(story-1.3): Tenant Detail Navigation and Overview`, `feat(story-1.4): My Tenants Self-Audit View`, and `feat(story-1.5): User Membership Lookup`; follow that story-scoped Conventional Commit style if committing later. [Source: `git log --oneline -8`]
- Story 1.5 most recently touched `TenantQueryGateway`, user lookup pages/components/state, resources, UI tests, route smoke tests, and `tests/test-summary.md`. Story 1.6 should be narrower: detail/configuration UI, resources, and focused UI tests unless gateway behavior genuinely changes. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#File List`]

### Latest Technical Information

Network research was not performed because the environment has restricted network access and this story relies on repo-pinned local versions, existing source, and already implemented backend contracts.

- .NET SDK `10.0.300`, target `net10.0`, nullable/implicit usings, `TreatWarningsAsErrors=true`. [Source: `global.json`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- Fluent UI Blazor remains pinned through FrontComposer at `5.0.0-rc.3-26138.1`; do not upgrade Fluent as part of this story. Verify exact table/DataGrid/form APIs locally before using new component features. [Source: `references/Hexalith.FrontComposer/Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`]
- Tests use xUnit v3, Shouldly, NSubstitute, and bUnit. Test classes/files use plural `{Class}Tests.cs`; avoid raw `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`]

### Project Structure Notes

- Source should stay in `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Tenants/`, `Components/Shared/`, `State/TenantDetail/`, `State/TruthState/`, `Resources/`, and component CSS.
- Tests should stay in `tests/Hexalith.Tenants.UI.Tests/` unless existing integration route smoke coverage must be updated for a new route; this story should not need a new route.
- The architecture documents are broader than this story. Implement only the read-only configuration view slice of `ui-05-tenant-configuration-read-only`.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Read-Only Tenant Configuration View`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- PRD/FR source: `_bmad-output/planning-artifacts/epics.md#Functional Requirements` (`FR6`, `FR7`)
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`, `#Frontend Architecture`, `#Requirements to Structure Mapping`
- UX/specs: `docs/tenants-ui-operations-shell-spec.md#3.1 Tenant detail sections (context preserved across all)`, `#4. User Lookup and Global Administrator Read-Only Surfaces`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `docs/tenants-ui-responsive-layout-and-visual-system-spec.md#6.3 Consuming rows with verbatim blockedBy`
- Contracts/backend: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetTenantAsync`; `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- Previous stories: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`, `1-2-tenant-list-triage.md`, `1-3-tenant-detail-navigation-and-overview.md`, `1-4-my-tenants-self-audit-view.md`, `1-5-user-membership-lookup.md`
- Project rules: `_bmad-output/project-context.md`, `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent fact `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, relevant UX/spec sections, Story 1.5, current detail/gateway source, query contracts, backend controller/handler behavior, resource/test evidence, and recent git history.
- Network research was not performed; local pinned versions and source are the authority for this story.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; critical disaster-prevention checks are reflected in this story context.
- 2026-06-06: Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md` plus submodule project-context files.
- 2026-06-06: Red phase added configuration view tests; focused UI test build failed on missing `TenantConfigurationView` as expected before implementation.
- 2026-06-06: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false --no-restore` hit the known .NET 10 Microsoft.Testing.Platform/VSTest target error; used the established xUnit v3 in-process executable fallback.
- 2026-06-06: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- 2026-06-06: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 97 total, 0 failed, 0 skipped.
- 2026-06-06: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- 2026-06-06: Tier 1 in-process test executables passed: Contracts 103/103, Client 47/47, Testing 181/181, Sample 31/31, UI 97/97.
- 2026-06-06: `Hexalith.Tenants.Server.Tests` in-process executable was attempted and still has the known pre-existing 6 documentation/configuration failures from missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and the Story 7.6A deployment-readiness summary expectation; no failure touches Story 1.6 UI files.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 1.6 to the read-only tenant configuration section and separates it from configuration edit/remove commands, member table work, audit proof, and safe-copy implementation.
- Story context identifies the key implementation risk: the current `TenantDetail.Configuration` response has no explicit sensitivity or namespace-ownership metadata, so sensitive display and ownership claims must fail closed instead of being fabricated.
- Story context preserves the existing tenant detail route, BFF gateway, projection truth/freshness handling, localization pattern, and UI test conventions.
- Implemented `TenantConfigurationView` as a read-only tenant detail section backed by the existing `TenantDetail.Configuration` snapshot; no backend endpoints, query contracts, gateway transport, or browser-side backend calls were added.
- Configuration keys are grouped by first dot segment, unscoped keys render in a localized "Other" group, filter/clear controls keep visible namespace context, freshness, scope copy, live announcements, and stable `tenants-config-*` selectors.
- Sensitive candidate keys and values fail closed to localized unavailable text; raw bearer-token-like values, passwords, connection strings, and secret candidates are not rendered into markup. No mutation affordances were added.
- Stale/degraded/unknown freshness is surfaced with `TruthStateBadge` and command-preview-unavailable copy, while unauthorized/not-found/unavailable detail states stay in the existing safe state branches and do not render the configuration table.
- Added EN/FR `Tenants.Configuration.*` resources and bUnit/CSS/resource coverage for grouping, unscoped keys, filtering, redaction, selectors, accessible literal labels, stale/degraded states, forced-colors hooks, focus-visible styling, and responsive/no-overlap CSS.

### File List

- `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`

### Change Log

- 2026-06-06: Added read-only tenant configuration view, localized configuration resources, focused component/page tests, and validation evidence. Status moved to review.
- 2026-06-06: Senior Developer Review (AI) completed. Auto-fixed 3 findings (2 accessibility/selector MEDIUM, 1 test-quality). 0 CRITICAL/HIGH remaining. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-06
**Outcome:** Approved (changes auto-applied)
**Mode:** Adversarial review with automatic fixes (story-automator non-interactive).

### Scope verified

- Build: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` → 0 warnings, 0 errors.
- Tests: UI suite via the documented in-process xUnit v3 fallback (`dotnet test` still hits the known .NET 10 Microsoft.Testing.Platform/VSTest target issue) → 102 total, 0 failed, 0 skipped (was 101; +1 parity test added during review).
- AC 1–7 cross-checked against `TenantConfigurationView.razor`, `TenantDetailPage.razor`, resources, and tests. All ACs implemented. No fabricated `TenantDetail` fields; sensitive candidates fail closed; no mutation affordances; EN/FR parity holds (41 `Tenants.Configuration.*` keys in both files).
- Git vs File List: only out-of-scope `_bmad-output/**` artifacts (`tests/test-summary.md`, story-automator orchestration log) changed beyond the documented File List; all source/test changes are documented. No false claims.

### Findings and resolutions

1. **[MEDIUM][a11y] Configuration data rows had no row header.** AC6 requires "row headers ... and row relationships," but each data row was all `<td>`. Fixed by promoting the Key cell to `<th scope="row">` (kept `data-testid="tenants-config-key"`, accessible literal label, and monospace styling via a new `.tenant-config__key-header` rule set to normal weight). `TenantConfigurationView.razor`, `TenantConfigurationView.razor.css`.
2. **[MEDIUM][selectors] Per-row freshness badge leaked a list-surface selector.** The in-table `TruthStateBadge` used no `TestId`, so it rendered the default `data-testid="tenants-list-truth-state"` once per row inside the configuration table — contradicting the story's `tenants-config-*` selector-separation rule (Story 1.5 lesson). Fixed by setting `TestId="tenants-config-row-freshness"`. `TenantConfigurationView.razor`.
3. **[LOW][test quality] Resource-parity test only spot-checked ~4 keys.** AC7 requires localization parity coverage. Added `Configuration_resources_have_full_invariant_and_french_parity`, which extracts every `Tenants.Configuration.*` key from both `.resx` files and asserts set equality, guarding against future EN/FR drift. `TenantDetailSurfaceTests.cs`.

### Notes (reviewed, accepted as designed — no change)

- Sensitive-value classifier is intentionally conservative/fail-closed (key+value keyword heuristics, dots stripped from keys, `@` redacts PII candidates). Over-redaction is acceptable per the story's deferred sensitive-value scope; it never fails open for the listed candidates and never renders raw secret/token/PII payloads into markup.
- Prefix-ownership is not claimed; the scope notice and "visible configuration" copy correctly fail closed because `TenantDetail` carries no caller-owned prefix list.
- The per-row Freshness column repeats the tenant-wide freshness (the contract has no per-key freshness); retained because the story explicitly mandates a freshness header.
- Unauthorized/Unavailable configuration states are guarded at the page level (the view is not rendered in those states); the matching `Tenants.Configuration.State.*`/`Announcement.*` keys exist per the story's required-resource list and are kept as forward-looking defensive copy.
