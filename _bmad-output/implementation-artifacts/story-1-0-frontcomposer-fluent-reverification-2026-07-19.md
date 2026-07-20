---
title: 'Story 1.0 — FrontComposer Shell and Fluent contract reverification'
date: '2026-07-19'
story: '1-0-reverify-frontcomposer-shell-and-fluent-contracts'
status: 'complete-with-blocked-contracts'
tenants_commit: '088232a7255698e20105594d9e0ef12a0f09c73e'
frontcomposer_source_commit: 'd3761fa08ce2f4bf004e8adc7f500822d04276f8'
frontcomposer_source_description: 'v4.0.1-76-gd3761fa0'
frontcomposer_package_baseline: '4.0.1'
builds_source_commit: '9ec0a032d785dd0abdc14276e8784d6fdd826fd0'
builds_source_description: 'v4.21.7-10-g9ec0a03'
fluent_ui_version: '5.0.0-rc.4-26180.1'
historical_evidence_sha256: 'b902cd22766dd5d71f0b958f1ccefc5d9f32c26c1caac8ef67bb54cfa5fe58ee'
---

# Story 1.0 FrontComposer and Fluent Reverification

## Decision

The shell, single-module navigation, FrontComposer layout, accessibility, localization, documentation, and approved local fallback boundaries are verified against the implementation-time source. The exact Fluent UI Blazor rc.4 assembly exposes the required semantic colors, pinning, focus callbacks, MessageBar contract, ARIA-live enum, and all required Regular Size20 icon types.

The command gate is **not cleared**. `FC-CNC` remains circuit-global instead of aggregate-scoped, and `FC-CMD` remains blocked because Tenants SignalR handlers directly advance lifecycle state while command audit state has no typed `AuditAvailable` value. `FC-TBL` and `FC-TOK` are changed/partial contracts with conservative existing boundaries. These findings are gates, not permission to add replacement infrastructure in Tenants.

## Evidence Semantics

- `verified`: the implementation-time source/assembly and focused checks support the claimed contract.
- `changed`: useful capability exists, but the current contract differs from the corrected architecture or canonical Tenants requirement. The documented conservative boundary remains in force.
- `blocked`: the required contract is absent or contradicted. Affected downstream stories must not claim the blocked behavior until the owning module supplies and verifies it.

No source under a root-declared submodule was modified by this reverification. The root repository already had user-owned FrontComposer and Builds pointer changes before implementation; this report records the checked-out source commits without changing them.

## Immutable Baseline

| Baseline | Implementation-time evidence | Interpretation |
|---|---|---|
| Tenants repository | `088232a7255698e20105594d9e0ef12a0f09c73e` | Git `HEAD`; the story and sprint artifacts were uncommitted/user-owned at implementation start. |
| Historical Story 1.0 spike | SHA-256 `b902cd22766dd5d71f0b958f1ccefc5d9f32c26c1caac8ef67bb54cfa5fe58ee` | `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`; preserved and never edited. |
| FrontComposer source actually compiled by UI/UI tests | `d3761fa08ce2f4bf004e8adc7f500822d04276f8`, `v4.0.1-76-gd3761fa0` | Root-declared submodule checkout. Both UI projects use unconditional `ProjectReference`s to Contracts/Shell; UI tests also reference Testing. This source baseline supersedes the `064b886d...` create-story observation for this run. |
| Published FrontComposer package baseline | `HexalithFrontComposerVersion=4.0.1` | Central package baseline only. It is recorded separately and was not substituted for the unconditional local FrontComposer project references. |
| Builds source | `9ec0a032d785dd0abdc14276e8784d6fdd826fd0`, `v4.21.7-10-g9ec0a03` | Root-declared submodule supplying central package versions; supersedes the `cb8b2d4...` create-story observation for this run. |
| Fluent components pin and resolved UI version | `5.0.0-rc.4-26180.1` | Declared in Builds and resolved top-level by `Hexalith.Tenants.UI`. |
| Fluent components/icons resolved UI-test versions | Components `5.0.0-rc.4-26180.1`; Icons `5.0.0-rc.4-26180.1` | Components are top-level; Icons are transitive. The UI project also resolves Icons transitively at the same version. |
| Fluent package provenance | Components and Icons built `2026-06-29`, repository commit `a6ec02a5d26b2c64c68180d8a662736b4cb18e4a` | `dotnet-inspect package` against the exact NuGet versions. |

## Contract Gate Matrix

| Contract | Status | Reverified evidence | Owner / downstream impact / conservative behavior |
|---|---|---|---|
| Shell integration | `verified` | `Program.cs` orders `AddFluentUIComponents`, `AddHexalithFrontComposerQuickstart`, `AddHexalithDomain<TenantsFrontComposerDomain>`, optional `AddHexalithFrontComposerServerSecurity`, and optional `AddHexalithEventStore`; it calls `UseRequestLocalization`. `MainLayout.razor` contains only `<FrontComposerShell>@Body</FrontComposerShell>` plus explanatory comments. FrontComposer bootstrap markers/validator enforce Quickstart → Domain → EventStore when the optional stages are used. | No blocker. Tenants continues to supply domain-specific provider/options and gateway composition only. |
| `FC-LYT` | `verified` | FrontComposer owns `FcPageLayoutMode.FullWidth`/`Constrained`, `FcPageLayout`, and aggregate page wrappers. Tenants declares dense list/audit/global-admin surfaces FullWidth and detail/lookup surfaces Constrained; focused layout tests cover the declarations. | No blocker. No Tenants page-root or generic layout replacement is allowed. |
| `FC-CMD` | `blocked` | Tenants has distinct `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed` and audit-pending states, but every command snapshot `SignalRNudge()` changes `RequestSent`/`Accepted` to `ProjectionPending` without awaiting authoritative re-query. `TenantCommandAuditState` has no `AuditAvailable`; `TenantAuditAvailability.IsAuditAvailable` is always false, while receipt/list readiness is represented elsewhere. | Owners: Tenants UI for nudge-only transitions and a shared FrontComposer/Tenants typed lifecycle handoff for non-collapse. Affects 2.1-2.4, 3.1-3.6, 4.1-4.3, and 5.5-5.7. Conservative behavior: SignalR may only request re-query; never infer confirmation/audit proof, and affected command stories remain unpromoted until corrected and tested. |
| `FC-CNC` | `blocked` | `CommandExecutionAdmissionRequest` contains only command type and display label. `CommandExecutionAdmissionGate` has one `_currentAdmission` and denies when **any** pending entry exists. Generated command forms emit no `AggregateIdentity`. Source tests explicitly expect a second unrelated command to be denied. | Owner: FrontComposer. Affects all command/correction stories (2.1-2.4, 3.1-3.6, 4.1-4.3, 5.5-5.7), including the fixed `system/global-administrators/global-administrators` aggregate. Conservative behavior: retain circuit-global serialization (safe but over-restrictive), do not claim AD-12, and do not add a second Tenants admission framework. |
| `FC-A11Y` | `verified` | Shell source contains skip links/landmarks, focus-visible support, keyboard/focus JS, live regions, reduced-motion and forced-colors rules. HFC1050-HFC1055 are active SourceTools diagnostics with current diagnostic pages/tests. | Per-story keyboard path, entry/return/terminal focus, announcement intent, responsive reflow, reduced-motion, and forced-colors evidence remains mandatory. |
| `FC-L10N` | `verified` | `AddHexalithFrontComposerQuickstart` composes localization registration; `AddHexalithShellLocalization` configures request cultures; Tenants calls `UseRequestLocalization`. `FcShellResources` owns shell copy with EN/FR parity tests; the Tenants manifest points navigation localization at `TenantsResources`. | Per-story Tenants EN/FR resource parity, whole-string formatting, and domain-copy ownership remains mandatory. |
| `FC-DOC` | `verified` | Current shell/navigation/datagrid component references, generated-component testing guide, domain projection guide, accessibility evidence pack, and HFC1050-HFC1055 pages exist. No current Storybook path exists, so no Storybook evidence is claimed. | Per-story documentation/evidence updates remain mandatory; current source/docs paths, not the June spike alone, are the evidence. |
| `FC-TBL` | `changed` | FrontComposer generated paging remains `request.StartIndex` → `skip` and `request.Count` → `take`; `IProjectionPageLoader` is explicitly offset-based. Tenants keeps opaque cursors in its BFF/page state and composes Fluent grids with local list/audit state models. `TenantDataGrid` pins identity/status but not the UX-designated freshness safety column; `AuditDataGrid` pins timestamp/actor/outcome and retains stable receipt/correction action slots. | Owners: FrontComposer for reusable protected-cursor/list-state capability; Tenants for its explicit safety-column contract. Affects list/search/audit claims in 1.2, 1.4, 1.5, 1.8-1.10, and 5.1. Conservative behavior: retain the narrow `TenantDataGrid`/`AuditDataGrid` boundary, pass cursors opaquely, keep non-collapsing local states, and do not create a generic Tenants grid framework. |
| `FC-AUD` | `verified` | No shared `<AuditTimeline>` contract was found. The approved fallback remains the flat Tenants `AuditDataGrid`, with cursor/page state owned by the Tenants surface and Fluent primitives used for rendering. | No new fallback is approved. Any reusable timeline belongs to FrontComposer. |
| `FC-CNS` | `verified` | No shared `<ConsequencePreview>` contract was found. The approved fallback remains inline full-content consequence preview; incomplete inputs fail closed. | No modal/global replacement or partial preview is approved. Any reusable preview belongs to FrontComposer. |
| `FC-TOK` | `changed` | FrontComposer exposes six `BadgeSlot`s mapped to six Fluent roles and generic Size16 inline SVG icons. Tenants requires eight Fluent roles and state-specific Size20 icons. Actual Tenants status badges mostly use `FcFluentIcons` Size16 factories, and no badge sets Fluent's `IconLabel`; FrontComposer MessageBars also omit the verified `MessageBarLayout.Notification`/`AriaLive` parameters. | Owners: FrontComposer for a complete shared semantic contract; Tenants for current state rendering. Affects every story that renders the affected state/interaction. Conservative behavior: use only verified `BadgeColor` values and real icons, reserve Success for current/active/confirmed/audit-available truth, keep visible text and ARIA labels, and do not invent token names. |
| Fluent rc.4 API | `verified` | Exact assembly inspection verifies all eight `BadgeColor` names; `ColumnBase<T>.Pin: DataGridColumnPin`; `None/Start/End`; DataGrid `AutoFocus`, `OnCellFocus`, `OnRowFocus`, and keyboard handler; FluentBadge text/icon properties; FluentMessageBar `Layout`, `AriaLive`, `Intent`, `Title`, actions, dismissal, visibility; `MessageBarLayout.Notification`; `AriaLive.Off/Polite/Assertive`; and all 21 required `Icons.Regular.Size20.*` types. | This verifies availability, not Tenants usage. The `FC-TOK` rendering findings remain changed and must not be presented as rc.4 API absence. |

## Exact Fluent rc.4 Matrix

### Semantic and component surface

- `BadgeColor`: `Brand`, `Danger`, `Important`, `Informative`, `Severe`, `Subtle`, `Success`, `Warning`.
- `ColumnBase<T>.Pin`: `Microsoft.FluentUI.AspNetCore.Components.DataGridColumnPin`; enum values `None`, `Start`, `End`.
- `FluentDataGrid<T>`: `AutoFocus`, `OnCellFocus`, `OnRowFocus`, `OnKeyDownAsync`, `Items`, `ItemsProvider`, `ItemKey`, loading/error content, sorting, resizing, reordering, virtualization and pagination are present.
- `FluentBadge`: `Content`, `ChildContent`, `IconStart`, `IconEnd`, `IconLabel`, `Color`, `Appearance`, `Size`, and shape/position properties are present.
- `FluentMessageBar`: `Layout`, `AriaLive`, `Intent`, `Title`, `ActionsTemplate`, `AllowDismiss`, `Visible`, `Icon`, `Animation`, `Shape`, and child content are present. `MessageBarLayout.Notification` exists.
- `AriaLive`: `Off`, `Polite`, `Assertive`.

### Required Regular Size20 icon types

All were found in `Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1` under `Microsoft.FluentUI.AspNetCore.Components.Icons.Regular`:

`CheckmarkCircle`, `DocumentCheckmark`, `ArrowClockwise`, `Document`, `ClipboardClock`, `Clock`, `ClockDismiss`, `ClockWarning`, `Warning`, `ClockAlarm`, `Power`, `DocumentProhibited`, `ShieldError`, `Prohibited`, `DismissCircle`, `QuestionCircle`, `ShieldProhibited`, `ShieldQuestion`, `CheckmarkCircleHint`, `Shield`, and `ClockToolbox`.

## Reproducible Command Record

Every command below was run from the Tenants repository root unless a `git -C` path is shown. Final focused/full validation results are appended in the validation section.

| Command | Exit | Result |
|---|---:|---|
| `test ! -e _bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` | 0 | Red precondition: successor evidence did not exist. |
| `sha256sum _bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md` | 0 | `b902cd...58ee`; historical evidence preserved. |
| `git rev-parse HEAD` | 0 | `088232a7255698e20105594d9e0ef12a0f09c73e`. |
| `git submodule status references/Hexalith.FrontComposer references/Hexalith.Builds` | 0 | Builds `9ec0a032...`; FrontComposer `d3761fa0...`; leading `+` records pre-existing root pointer differences. |
| `git -C references/Hexalith.FrontComposer describe --tags --always --dirty` | 0 | `v4.0.1-76-gd3761fa0`; no `-dirty` suffix. |
| `git -C references/Hexalith.Builds describe --tags --always --dirty` | 0 | `v4.21.7-10-g9ec0a03`; no `-dirty` suffix. |
| `rg -n 'HexalithFrontComposerVersion\|Microsoft\\.FluentUI\\.AspNetCore\\.Components' references/Hexalith.Builds/Props/Directory.Packages.props` | 0 | FrontComposer `4.0.1`; components/icons `5.0.0-rc.4-26180.1`. |
| `dotnet package list --project src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --include-transitive --no-restore` | 0 | Components `5.0.0-rc.4-26180.1` top-level; Icons same version transitive. |
| `dotnet package list --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include-transitive --no-restore` | 0 | Components `5.0.0-rc.4-26180.1` top-level; Icons same version transitive. |
| `dnx dotnet-inspect -y -- package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1` | 0 | Exact package metadata and repository commit verified. |
| `dnx dotnet-inspect -y -- package Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1` | 0 | Exact icons package metadata and repository commit verified. |
| `dnx dotnet-inspect -y -- member ... --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1 --oneline` for BadgeColor, `ColumnBase<T>`, DataGridColumnPin, `FluentDataGrid<T>`, FluentBadge, FluentMessageBar, MessageBarLayout, and AriaLive | 0 | Exact rc.4 public surface recorded above. |
| `dnx dotnet-inspect -y -- find <IconName> --package Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1 --table` for all required icon names, checked for each exact `Regular.Size20` row | 0 | 21/21 exact Regular Size20 types verified. |

## Focused And Full Validation

All consolidated xUnit rows below used one of these exact runner forms, once per fully qualified class named in the row or result list:

```bash
# Tenants repository root
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class <fully-qualified-class>

# references/Hexalith.FrontComposer working directory
tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class <fully-qualified-class>
tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class <fully-qualified-class>
```

The class selectors are the namespaces declared by the inspected test sources; the table records the exact short class name and pass count for each invocation. Consolidated totals are arithmetic summaries, not replacement runtime tests.

### Shell composition and layout

| Command | Exit | Result |
|---|---:|---|
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` | 0 | 16/16 passed. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.PageLayoutDeclarationTests` | 0 | 2/2 passed. |
| `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 --no-restore` (first attempt) | 1 | Reproducible environment prerequisite: `NETSDK1004`, Shell.Tests `obj/project.assets.json` absent. |
| `dotnet restore tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -m:1` | 0 | Restored the pinned FrontComposer test graph; no source/dependency-version change. |
| `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 --no-restore` (after restore) | 0 | Build succeeded, 0 warnings/0 errors. |
| FrontComposer Shell xUnit executable, `-class ...FrontComposerBootstrapGuardTests` | 0 | 22/22 passed. |
| FrontComposer Shell xUnit executable, `-class ...Story11BootstrapShellRenderTests` | 0 | 4/4 passed. |
| FrontComposer Shell xUnit executable, `-class ...Story12PageLayoutTests` | 0 | 14/14 passed. |
| FrontComposer Shell xUnit executable, `-class ...FcAggregateListPageTests` | 0 | 7/7 passed. |
| FrontComposer Shell xUnit executable, `-class ...FcAggregateDetailPageTests` | 0 | 12/12 passed. |

Source inspection found exactly one Tenants `AddNavEntry` call and it targets `/tenants`. Compatibility/contextual routes remain Razor routes only. `git -C references/Hexalith.FrontComposer status --short` remained empty after restore/build/testing.

### Command truth and concurrency

The implementation-time state mapping is explicit: `RequestSent` is submitted, `Accepted` is transport acceptance, `ProjectionPending` is accepted/completed without authoritative projection evidence, and `Confirmed` requires projection evidence. Audit state has `AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport`, but no typed `AuditAvailable`; receipt/list readiness is a separate surface concept and cannot be used as an implicit command lifecycle state.

The aggregate matrix cannot be satisfied by the current gate:

| First command | Second command in same circuit | Current result | Required result |
|---|---|---|---|
| Tenant A | Tenant A | denied | denied until terminal evidence for Tenant A |
| Tenant A | Tenant B | denied | admitted because aggregate identities differ |
| Tenant A | `system/global-administrators/global-administrators` | denied | admitted because aggregate identities differ |
| `system/global-administrators/global-administrators` | same fixed aggregate | denied | denied until terminal evidence for the fixed aggregate |

`CommandExecutionAdmissionRequest` and generated form emission carry no aggregate identity, so the two differing-aggregate rows cannot be executed as such. The existing source test deliberately verifies circuit-global denial. This is fail-closed but does not prove AD-12.

| Command/check | Exit | Result |
|---|---:|---|
| `rg -q 'AggregateIdentity' .../CommandExecutionAdmissionRequest.cs` | 1 | Expected blocked check: aggregate identity is absent. |
| `rg -q 'AggregateIdentity' .../CommandFormEmitter.cs` | 1 | Expected blocked check: generated admission requests cannot carry aggregate identity. |
| `rg -q 'AuditAvailable' src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` | 1 | Expected blocked check: no unambiguous typed command audit-available state exists. |
| FrontComposer Shell xUnit executable, `-class ...CommandExecutionAdmissionGateTests` | 0 | 8/8 passed; includes circuit-global second-command denial. |
| FrontComposer Shell xUnit executable, `-class ...PendingCommandStateServiceTests` | 0 | 23/23 passed. |
| FrontComposer Shell xUnit executable, `-class ...PendingCommandPollingCoordinatorTests` | 0 | 7/7 passed. |
| FrontComposer Shell xUnit executable, `-class ...PendingCommandOutcomeResolverTests` | 0 | 12/12 passed. |
| `dotnet restore tests/Hexalith.FrontComposer.SourceTools.Tests/... -m:1` | 0 | Restored the pinned source-tools test graph; no source/dependency-version change. |
| `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/... -c Release -m:1 --no-restore` | 0 | Build succeeded, 0 warnings/0 errors. |
| FrontComposer SourceTools xUnit executable, `-class ...CommandFormEmitterTests` | 0 | 36/36 passed; source inspection confirms no emitted aggregate identity. |
| Tenants xUnit command snapshot and flow-guard classes | 0 | 124/124 passed: `TenantCommandFlowGuardTests` 24, `TenantCreateCommandSnapshotTests` 7, `TenantAddMemberCommandSnapshotTests` 6, `TenantChangeRoleCommandSnapshotTests` 9, `TenantRemoveMemberCommandSnapshotTests` 8, `TenantUpdateMetadataCommandSnapshotTests` 7, `TenantSetConfigurationCommandSnapshotTests` 15, `TenantRemoveConfigurationCommandSnapshotTests` 11, `TenantLifecycleCommandSnapshotTests` 17, `GlobalAdministratorGrantCommandSnapshotTests` 6, `GlobalAdministratorRemoveCommandSnapshotTests` 5, and `TenantAuditAvailabilityTests` 9. |

The focused suites verify the behavior that exists; they do not convert circuit-global serialization or SignalR-driven `ProjectionPending` transitions into the corrected contracts. Owners, affected stories, and conservative behavior are recorded in the gate matrix.

### Accessibility, localization, and documentation

| Command/check | Exit | Result |
|---|---:|---|
| FrontComposer Shell xUnit executable, `-class ...Story13AccessibilityPrimitivesTests` | 0 | 7/7 passed. |
| FrontComposer Shell xUnit executable, `-class ...FcShellResourcesTests` | 0 | 70/70 passed. |
| FrontComposer Shell xUnit executable, `-class ...LocalizationGovernanceTests` | 0 | 2/2 passed. |
| FrontComposer Shell xUnit executable, `-class ...FcFieldPlaceholderLocalizationTests` | 0 | 5/5 passed. |
| FrontComposer SourceTools xUnit executable, `-class ...CustomizationAccessibilityAnalyzerTests` | 0 | 12/12 passed for HFC1050-HFC1055 enforcement. |
| FrontComposer SourceTools xUnit executable, `-class ...FcDocComponentDocumentationContractTests` | 0 | 30/30 passed. |
| FrontComposer SourceTools xUnit executable, `-class ...DocsSiteValidationTests` | 0 | 6/6 passed. |
| Tenants xUnit executable, `-class ...DomainUiFluentConformanceTests` | 0 | 51/51 passed. |
| Tenants xUnit executable, `-class ...TenantsUiCompositionTests` | 0 | 16/16 passed, including navigation/resource composition. |
| `test -f` for the current accessibility README, generated-component testing guide, shell/navigation/datagrid references, and domain projection guide | 0 | All six cited current paths exist. |
| `find` for Storybook paths, excluding `node_modules` | 0 | Zero paths found; no Storybook evidence is claimed. |

Current source also contains shell skip/landmark and focus behavior, polite/assertive live regions, `prefers-reduced-motion`, and `forced-colors` rules. These shared primitives do not discharge consumer-story obligations: each affected story must still demonstrate its keyboard path; focus entry, return, and terminal behavior; announcement intent; EN/FR parity and whole-string formatting; responsive/reflow, reduced-motion, and forced-colors behavior; and updated documentation/evidence.

### Grid boundary and list states

| Command/check | Exit | Result |
|---|---:|---|
| Source inspection of `IProjectionPageLoader` and generated provider emission | 0 | Loader parameters are `skip`/`take`; generated code maps `request.StartIndex`/`request.Count` to those offsets. No protected cursor contract exists. |
| FrontComposer SourceTools xUnit executable, `-class ...RazorEmitterItemsProviderTests` | 0 | 4/4 passed and explicitly asserts `skip`/`take` emission. |
| FrontComposer Shell xUnit classes `LoadPageEffectIntegrationTests`, `LoadPageReducerTests`, and `VirtualizationActionsTests` | 0 | 23/23 passed (7, 6, and 10). |
| Tenants xUnit classes `TenantListSurfaceTests`, `TenantQueryGatewayTests`, `TenantAuditPageTests`, `AuditDataGridCorrectionTests`, and `TenantsWorkspaceTests` | 0 | 184/184 passed (16, 126, 30, 3, and 9). |

The Tenants gateway/surface tests preserve opaque cursor pass-through, expired-cursor recovery, and distinct loading/empty/filtered-empty/error/unauthorized/stale/degraded states. `TenantDataGrid` and `AuditDataGrid` render `FluentDataGrid` with stable keys and action slots. The tenant grid pins identity and status but does not pin the UX-designated freshness safety column; the audit grid pins timestamp, actor, and outcome and preserves receipt/correction slots. This supports the `changed` FC-TBL classification: keep the narrow local grids and assign any reusable protected-cursor capability to FrontComposer.

### Approved fallbacks and semantic rendering

| Command/check | Exit | Result |
|---|---:|---|
| `rg -q 'class AuditTimeline|<AuditTimeline' references/Hexalith.FrontComposer/src` | 1 | Expected absence: flat local `AuditDataGrid` remains the approved FC-AUD fallback. |
| `rg -q 'class ConsequencePreview|<ConsequencePreview' references/Hexalith.FrontComposer/src` | 1 | Expected absence: inline full-content consequence preview remains the approved FC-CNS fallback. |
| Source inspection of `BadgeSlot`, `SlotAppearanceTable`, and `StatusIconTable` | 0 | Six slots (`Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Accent`) map to six Fluent roles and generic Size16 status icons. |
| Tenants rendering inventory | 0 | Six `FluentBadge` tags across five files; badges supply visible text and `IconStart`, but no `IconLabel`; most status factories are Size16. No Tenants `FluentMessageBar` exists. |
| `rg -n 'BadgeColor\.Success' src/Hexalith.Tenants.UI -g '*.razor' -g '*.cs'` | 0 | Four mappings only: Tenant `Active` in detail/my-tenants/list, and read-model freshness `Current`. |
| FrontComposer MessageBar source inventory | 0 | No `MessageBarLayout.Notification` or component `AriaLive` parameter usage was found. |
| Tenants xUnit classes for truth badges and consequence/correction fallback | 0 | 84/84 passed: `TruthStateBadgeTests` 1, `TenantCorrectionPreviewSnapshotTests` 10, `TenantCorrectionStartIntentTests` 14, `CorrectionStartPanelTests` 17, `GlobalAdministratorCorrectionSnapshotTests` 24, `GlobalAdministratorCorrectionPanelTests` 15, `AuditDataGridCorrectionTests` 3. **(2026-07-19 review patch: independently re-ran each class individually and confirmed each count exactly.)** |
| Tenants `DomainUiFluentConformanceTests` | 0 | 51/51 passed; protects the confirmed Fluent-only/grid/layout boundary without asserting the known partial token contract. |

The historical circuit-global FC-CNC fallback is not approved for new claims: AD-12 supersedes it with `(interactive circuit, AggregateIdentity)` scope. Current circuit-global behavior is retained only as conservative fail-closed behavior while FrontComposer owns the blocker. No shared token names were invented, and no intentionally failing guard was added for the known partial rendering contract.

### Exact rc.4 behavior and current rendering comparison

In addition to the public API matrix, exact-package decompilation verified behavior in the installed net10.0 rc.4 artifact:

- `OnAfterRenderAsync(firstRender)` initializes the DataGrid JavaScript module with the `AutoFocus` value; the exact packaged script focuses the first cell when enabled.
- The packaged DataGrid script implements arrow-key cell traversal, prevents default scrolling during handled movement, honors RTL for horizontal arrows, and excludes nested text-field/menu-item handling.
- `OnKeyDownAsync` handles Shift+R reset, Shift+S sort removal, discrete `-`/`+` resizing, and Alt+F/P/N/L column movement while reorder UI is active.
- `FluentBadge.IconLabel` applies the icon `aria-label`; `FluentMessageBar.Layout` defaults to `SingleLine`, so `Notification` must be selected explicitly when that contract is required.

| Command/check | Exit | Result |
|---|---:|---|
| `dnx dotnet-inspect ... FluentDataGrid<TGridItem> -m OnKeyDownAsync -v:d` | 0 | Exact rc.4 method and keyboard-handling contract found. |
| `dnx dotnet-inspect ... FluentDataGrid<TGridItem> -m AutoFocus -v:d` | 0 | Exact rc.4 first-cell focus property found. |
| `dnx dotnet-inspect ... FluentBadge -m IconLabel -v:d` | 0 | Exact icon ARIA-label property found. |
| `dnx dotnet-inspect ... FluentMessageBar -m Layout -v:d` | 0 | Nullable layout property found; documented default is `SingleLine`. |
| ``ilspycmd -t '...FluentDataGrid`1'`` against the exact net10.0 rc.4 assembly and inspection of the package's exact `FluentDataGrid.razor.js` | 0 | Confirmed initialization, autofocus, arrow traversal, resize/sort reset, and reorder shortcuts described above. |

Actual Tenants rendering uses valid `BadgeColor` values and real installed icon types, with Success confined to active/current mappings in the inspected source. The exact API is therefore `verified`, while Size16 factories, missing badge `IconLabel`, and missing explicit MessageBar notification/live configuration remain `FC-TOK changed` findings rather than rc.4 API gaps. Existing tests already guard confirmed Tenants boundaries; adding a knowingly failing test or modifying FrontComposer was out of scope.

### Final quality gate

| Command | Exit | Result |
|---|---:|---|
| `tests/.../Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.CommandFlowGuardConformanceTests` | 0 | 1/1 passed. |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` | 0 | Build succeeded, 0 warnings/0 errors. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` | 0 | 904/904 passed, 0 failed/skipped. The anticipated runner incompatibility did not occur. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | 0 | 904/904 passed, 0 errors/failed/skipped/not-run, 8.167 seconds. |
| `sha256sum _bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md` | 0 | Historical hash remained `b902cd22766dd5d71f0b958f1ccefc5d9f32c26c1caac8ef67bb54cfa5fe58ee`. |
| `git diff --check` | 0 | No whitespace error; Git emitted only the pre-existing/mixed line-ending warning for sprint status. |
| `git diff --submodule=short -- references/Hexalith.FrontComposer references/Hexalith.Builds` | 0 | Shows the same pre-existing root pointer differences: FrontComposer `064b886d` → `d3761fa0`, Builds `cb8b2d4` → `9ec0a032`. |
| `git -C references/Hexalith.FrontComposer status --short` and equivalent Builds command | 0 | Both submodule working trees are clean; restore/build/test created no tracked source change. |
| `git status --short` | 0 | Only the pre-existing sprint/planning/submodule-pointer state plus this story and its successor evidence are present; unrelated planning proposal remained untouched. |

The verification story is complete with explicit blocked/changed gates. Completion of this evidence story does not promote the downstream stories identified under FC-CMD, FC-CNC, FC-TBL, or FC-TOK.

## Downstream Promotion Decision

- Shell/navigation/layout, `FC-A11Y`, `FC-L10N`, `FC-DOC`, `FC-AUD`, and `FC-CNS` evidence is usable, subject to every story's own tests and evidence.
- `FC-CMD` and `FC-CNC` do **not** clear any command/correction story for promotion.
- `FC-TBL` permits only the documented narrow Tenants grid boundary; it does not establish a shared cursor/safety-state grid contract.
- `FC-TOK` permits verified Fluent semantic/icon mappings only; it does not establish a complete shared token contract or prove current Tenants Size20/icon-label/MessageBar rendering conformance.
