---
title: 'Replace brittle UI source guards with behavioral tests'
type: 'refactor'
created: '2026-09-02'
status: ready-for-dev
baseline_revision: d2b7ede359830c27934ac9f577e3073955c3e2c2
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

**Approach:** Replace the cursor and dispatcher source-text checks with runtime bUnit assertions at the rendered navigation and renderer-dispatcher boundaries. Back the workspace localizer double directly with the production `TenantsResources` bundle, failing on unknown keys and enumerating the keys it actually resolves. Also synchronize only the six baseline resource-copy mismatches in `GlobalAdministratorsPageTests.StubTenantsLocalizer`, updating assertions in that same file only where those six corrected values directly change expected output.

## Boundaries & Constraints

**Always:** Preserve the existing opaque-cursor, page-one return, support-safety, and Fluent UI behavior. Exercise real `TenantsWorkspace`/`TenantListNavigationContext` behavior through bUnit and observable URLs, requests, DOM output, and renderer dispatcher access. Resolve localized values using the current UI culture and keep `GetAllStrings` consistent with indexer lookup. Keep changes inside the two original test files named by DW-101, with one narrow exception: `GlobalAdministratorsPageTests.cs` may change only the six stale `StubTenantsLocalizer` values named below and assertions in that file directly affected by those corrected values.

**Never:** Edit the deferred-work ledger, weaken unrelated security/conformance guards, assert implementation identifiers or exact source occurrence counts, duplicate `.resx` keys/copy in the workspace fixture, convert the global-administrators localizer double wholesale, change unrelated global-administrator tests or behavior, or change production UI code and resources.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tenant drill-in | Workspace state contains an opaque cursor plus search/filter/sort state | Detail and audit links retain safe query/selection context but their decoded return URLs omit the cursor; the cursor and ETag do not appear in rendered output | Missing links or leaked sentinels fail the test |
| Async load completion | Initial gateway load completes from a worker thread with page-one recovery | State application/navigation runs with renderer dispatcher access and the refreshed UI renders | Off-dispatch callback or render failure fails the test |
| Resource lookup | Workspace requests a shipped neutral or localized key | Stub returns production copy and enumerates production keys | Unknown key throws instead of echoing the key |
| Baseline global-administrators fixture parity | The parity gate inspects the six stale `GlobalAdministratorsPageTests.StubTenantsLocalizer` entries named in Tasks | Those six entries and directly affected assertions use the current shipped neutral resource copy; all other global-administrator fixture behavior is unchanged | Any broader rewrite or unrelated test change exceeds this exception |

</intent-contract>

## Code Map

- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- existing cursor URL/reset coverage at `Search_change_updates_canonical_workspace_url_and_resets_cursor`, `Tenant_grid_sort_updates_canonical_workspace_url_and_resets_cursor`, `Query_identity_change_clears_the_previous_cursor_history`, and `Page_size_change_is_local_resets_cursor_history_and_requests_supported_size`; replace only the brittle grid/navigation source assertions near `Tenant_row_surface_has_no_cursor_etag_logging_or_telemetry_channel` and the source-only dispatcher test near `Renderer_lifecycle_and_event_awaits_stay_on_the_blazor_dispatcher`.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs` -- read-only evidence: both `ToDetailUrl` and `ToAuditUrl` build return state with `Cursor = null`; behavioral tests must pin their outward URLs rather than source spelling.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- read-only evidence: `LoadAsync` applies completed gateway results inside `InvokeAsync`; the test should observe dispatcher access during recovery navigation.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- `StubTenantsLocalizer` currently hand-copies a large subset of resource entries; replace it with a `ResourceManager`-backed, fail-closed double.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- narrow write exception: synchronize only the six stale `StubTenantsLocalizer` entries named in Tasks and update assertions in this file directly affected by their corrected copy; do not refactor the double or change unrelated tests.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- read-only source of production keys/copy; removed tenant-list sort-select keys must not be reintroduced into the fixture.
- `tests/Hexalith.Tenants.UI.Tests/LocalizerDoubleParityTests.cs` -- read-only suite gate requiring every localizer double to enumerate keys and agree with shipped neutral/French resources.

## Tasks & Acceptance

**Execution:**
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- replace cursor/source-count checks with rendered detail/audit return-URL and sentinel non-disclosure assertions; replace the `ConfigureAwait(false)` scan with a worker-thread completion test that observes `Renderer.Dispatcher.CheckAccess()` during state-driven navigation; retain unrelated gateway telemetry guards.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- generate the localizer double's values and enumeration from the production resource set for `CultureInfo.CurrentUICulture`, format arguments using the active culture, and throw for unknown keys.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- make exactly these six `StubTenantsLocalizer` entries match the shipped neutral resource values, and update only directly affected assertions in this file: `Remove.Preview.Target.Value` = `Exact target: “{0}”`; `Remove.Preview.CallerTargetContext.Other.Value` = `This removes another administrator’s global authority.`; `Remove.Preview.Acknowledge` = `Type the exact target “{0}” to acknowledge this removal.`; `Remove.Preview.Confirm` = `Confirm removal`; `Remove.Status.Rejected.LastAdministrator` = `The server rejected removal of the last global administrator.`; and `Remove.Status.Rejected.NotFound` = `The server could not find the exact administrator target.` All keys have the `Tenants.GlobalAdministrators.` prefix.

**Acceptance Criteria:**
- Given an opaque cursor and ETag plus safe list query state, when tenant detail and audit navigation are rendered, then no protected sentinel is present in markup and both decoded return URLs start at page one while retaining the intended non-cursor context.
- Given a pending list load completed outside the renderer thread, when `TenantsWorkspace` applies a page-one recovery result, then the observable navigation callback has renderer-dispatcher access and the recovered surface renders without an unhandled renderer exception.
- Given the workspace localizer double, when its indexers and `GetAllStrings` are used, then shipped production values are returned for the active UI culture and an unknown resource key fails closed.
- Given the global-administrators localizer fixture, when the suite-wide parity gate and directly affected global-administrator tests run, then the six named entries match the shipped neutral resource copy exactly and no unrelated fixture value or test behavior changes.
- Given the focused UI test project, when the affected classes and full test assembly run, then all tests pass without changes to production UI/resources or the deferred-work ledger.

## Spec Change Log

- 2026-09-02: Resolved the verification-boundary contradiction by allowing a narrow third-file exception for the six baseline `GlobalAdministratorsPageTests.StubTenantsLocalizer` copy mismatches and only their directly affected assertions; retained the full-suite pass requirement.

## Review Triage Log

## Design Notes

Use the bUnit renderer itself as the dispatcher oracle. Complete a `TaskCompletionSource<TenantListSnapshot>` on a worker thread and observe a recovery-triggered `NavigationManager.LocationChanged` callback; `Renderer.Dispatcher.CheckAccess()` must be true there. This proves the post-await state/navigation application crosses the renderer boundary without constraining whether library awaits use `ConfigureAwait(false)` internally.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release` -- expected: test project and dependencies build with warnings treated as errors.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests` -- expected: affected cursor and dispatcher tests pass.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests` -- expected: workspace and resource-parity tests pass.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll` -- expected: full UI test assembly passes.

