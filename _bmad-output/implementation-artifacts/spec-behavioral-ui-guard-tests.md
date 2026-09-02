---
title: 'Replace brittle UI source guards with behavioral tests'
type: 'refactor'
created: '2026-09-02'
status: 'blocked'
baseline_revision: '1065b09a9439ae354257a9f7afd3fbe3ff9c1db1'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Tenant-list cursor and Blazor-dispatcher safeguards are partly asserted by matching implementation text, so harmless renames can fail tests while equivalent regressions can pass. `TenantsWorkspaceTests` also duplicates production localization keys and copy, allowing its fixture to drift from the shipped resource bundle.

**Approach:** Replace the cursor and dispatcher source-text checks with runtime bUnit assertions at the rendered navigation and renderer-dispatcher boundaries. Back the workspace localizer double directly with the production `TenantsResources` bundle, failing on unknown keys and enumerating the keys it actually resolves.

## Boundaries & Constraints

**Always:** Preserve the existing opaque-cursor, page-one return, support-safety, and Fluent UI behavior. Exercise real `TenantsWorkspace`/`TenantListNavigationContext` behavior through bUnit and observable URLs, requests, DOM output, and renderer dispatcher access. Resolve localized values using the current UI culture and keep `GetAllStrings` consistent with indexer lookup. Keep changes inside the two test files named by DW-101.

**Never:** Edit the deferred-work ledger, weaken unrelated security/conformance guards, assert implementation identifiers or exact source occurrence counts, duplicate `.resx` keys/copy in the workspace fixture, or change production UI code and resources.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tenant drill-in | Workspace state contains an opaque cursor plus search/filter/sort state | Detail and audit links retain safe query/selection context but their decoded return URLs omit the cursor; the cursor and ETag do not appear in rendered output | Missing links or leaked sentinels fail the test |
| Async load completion | Initial gateway load completes from a worker thread with page-one recovery | State application/navigation runs with renderer dispatcher access and the refreshed UI renders | Off-dispatch callback or render failure fails the test |
| Resource lookup | Workspace requests a shipped neutral or localized key | Stub returns production copy and enumerates production keys | Unknown key throws instead of echoing the key |

</intent-contract>

## Code Map

- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- existing cursor URL/reset coverage at `Search_change_updates_canonical_workspace_url_and_resets_cursor`, `Tenant_grid_sort_updates_canonical_workspace_url_and_resets_cursor`, `Query_identity_change_clears_the_previous_cursor_history`, and `Page_size_change_is_local_resets_cursor_history_and_requests_supported_size`; replace only the brittle grid/navigation source assertions near `Tenant_row_surface_has_no_cursor_etag_logging_or_telemetry_channel` and the source-only dispatcher test near `Renderer_lifecycle_and_event_awaits_stay_on_the_blazor_dispatcher`.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs` -- read-only evidence: both `ToDetailUrl` and `ToAuditUrl` build return state with `Cursor = null`; behavioral tests must pin their outward URLs rather than source spelling.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- read-only evidence: `LoadAsync` applies completed gateway results inside `InvokeAsync`; the test should observe dispatcher access during recovery navigation.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- `StubTenantsLocalizer` currently hand-copies a large subset of resource entries; replace it with a `ResourceManager`-backed, fail-closed double.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- read-only source of production keys/copy; removed tenant-list sort-select keys must not be reintroduced into the fixture.
- `tests/Hexalith.Tenants.UI.Tests/LocalizerDoubleParityTests.cs` -- read-only suite gate requiring every localizer double to enumerate keys and agree with shipped neutral/French resources.

## Tasks & Acceptance

**Execution:**
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- replace cursor/source-count checks with rendered detail/audit return-URL and sentinel non-disclosure assertions; replace the `ConfigureAwait(false)` scan with a worker-thread completion test that observes `Renderer.Dispatcher.CheckAccess()` during state-driven navigation; retain unrelated gateway telemetry guards.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- generate the localizer double's values and enumeration from the production resource set for `CultureInfo.CurrentUICulture`, format arguments using the active culture, and throw for unknown keys.

**Acceptance Criteria:**
- Given an opaque cursor and ETag plus safe list query state, when tenant detail and audit navigation are rendered, then no protected sentinel is present in markup and both decoded return URLs start at page one while retaining the intended non-cursor context.
- Given a pending list load completed outside the renderer thread, when `TenantsWorkspace` applies a page-one recovery result, then the observable navigation callback has renderer-dispatcher access and the recovered surface renders without an unhandled renderer exception.
- Given the workspace localizer double, when its indexers and `GetAllStrings` are used, then shipped production values are returned for the active UI culture and an unknown resource key fails closed.
- Given the focused UI test project, when the affected classes and full test assembly run, then all tests pass without changes to production UI/resources or the deferred-work ledger.

## Spec Change Log

## Review Triage Log

## Design Notes

Use the bUnit renderer itself as the dispatcher oracle. Complete a `TaskCompletionSource<TenantListSnapshot>` on a worker thread and observe a recovery-triggered `NavigationManager.LocationChanged` callback; `Renderer.Dispatcher.CheckAccess()` must be true there. This proves the post-await state/navigation application crosses the renderer boundary without constraining whether library awaits use `ConfigureAwait(false)` internally.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release` -- expected: test project and dependencies build with warnings treated as errors.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests` -- expected: affected cursor and dispatcher tests pass.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests` -- expected: workspace and resource-parity tests pass.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll` -- expected: full UI test assembly passes.

## Auto Run Result

Status: blocked
Blocking condition: implementation verification failed

- Release build passed with 0 warnings and 0 errors.
- `TenantListSurfaceTests` passed 102/102, covering the rendered cursor/ETag non-disclosure and off-thread dispatcher matrix rows.
- All 27 `TenantsWorkspaceTests` passed, covering production resource lookup, formatting, enumeration, culture selection, and fail-closed unknown-key behavior.
- The required combined workspace/localizer-parity lane failed 1/30 because `GlobalAdministratorsPageTests.StubTenantsLocalizer` contains six copy mismatches against the shipped resources. The stale values and corrected `.resx` values both exist unchanged at baseline revision `1065b09a9439ae354257a9f7afd3fbe3ff9c1db1`; that fixture is outside this spec's two-file boundary.
- The full UI assembly failed 17/2738, all in pre-existing Global Administrators page/snapshot/parity tests outside the authorized files. Fixing those independent failures would expand the deferred-work bundle beyond DW-101.
- `git diff --check` passed. No production UI/resource file or deferred-work ledger was changed.
