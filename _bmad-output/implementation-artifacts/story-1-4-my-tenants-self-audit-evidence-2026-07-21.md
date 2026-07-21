# Story 1.4 — My Tenants Self-Audit — Evidence Report (2026-07-21)

Reverify-and-harden run for the `scope=mine` self-audit slice (FR3). This report records the
commits/pins, the exact validation commands and results, the per-AC verdict, and the external
runtime/freshness blockers. It complements — it does not replace — the preserved historical evidence
`1-4-my-tenants-self-audit-view.md` (June 2026 standalone-`/tenants/my` baseline).

## Environment and source pins

| Item | Value |
|---|---|
| Root baseline commit (story frontmatter) | `21a3ce5a7597359c2b8ba050f73860b0372087d1` (`main`) |
| SDK | .NET `10.0.302`, `rollForward=latestPatch`, target `net10.0` |
| Fluent UI Blazor pin | `5.0.0-rc.4-26180.1` (deliberate v5 prerelease; not upgraded) |
| FrontComposer package baseline | `4.0.1`; source `e13368a2` (unchanged — not modified) |
| Memories submodule | `3b1ae857` → `ae591ce7` (bumped in this commit — see below) |
| PolymorphicSerializations / Commons / AI.Tools | `a5dd24f5` / `ea1fc455` / `991e8ea1` (unchanged) |

### Submodule pointer bumps committed with the story (intentional — 2026-07-21 code-review decision)

`git status` was clean apart from `references/Hexalith.Builds` + `sprint-status.yaml` at story start.
During the session the submodule working trees moved (the recurring "submodules drift externally
mid-work" pattern) and were **committed together with the story work in commit `41e047e`** — not left
out of the commit. Per the 2026-07-21 code-review decision these bumps are kept as intentional; all three
targets are reachable on their `origin/main`, so the parent gitlinks stay clone-/CI-reachable:

| Submodule | Story frontmatter | Committed in `41e047e` | Note |
|---|---|---|---|
| `references/Hexalith.Builds` | `7708256e` | `dfb2f3fd` | Already `M` at story start; committed with the story. |
| `references/Hexalith.EventStore` | `41f5ed0f` | `4245f0f8` | Became `M` during the session; Release build below is coherent with `4245f0f8`. |
| `references/Hexalith.Memories` | `3b1ae857` | `ae591ce7` | Committed with the story (previously mis-recorded here as unchanged). |

## Validation commands and results

All UI tests were run with the built xUnit v3 executable (the documented .NET 10 MTP/VSTest fallback);
`.slnx` was used for build only.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1` | Build succeeded — **0 Warning(s), 0 Error(s)** |
| `dotnet build Hexalith.Tenants.slnx -c Release -m:1 -warnaserror` | Build succeeded — **0 Warning(s), 0 Error(s)** (the previously-recorded NU1102 Release blocker did not recur — the pinned packages restored cleanly) |
| `…/Hexalith.Tenants.UI.Tests -noLogo -noColor` (full suite) | **Total: 948, Failed: 0** (was 942 pre-change; +6 new tests) |
| `…/Hexalith.Tenants.UI.Tests … -class …State.TenantWorkspaceStateTests` | **Total: 11, Failed: 0** (+3 new) |
| `…/Hexalith.Tenants.UI.Tests … -class …Components.MyTenantsSurfaceTests` | **Total: 14, Failed: 0** (+2 new) |
| `…/Hexalith.Tenants.UI.Tests … -class …TenantsWorkspaceTests` | **Total: 10, Failed: 0** (+1 new) |
| `…/Hexalith.Tenants.UI.Tests … -class …Components.UserMembershipLookupSurfaceTests` | **Total: 13, Failed: 0** (shared-grid change did not regress Story 1.5) |
| `…/Hexalith.Tenants.UI.Tests … -class …DomainUiFluentConformanceTests` | **Total: 51, Failed: 0** (new `<a>` drill-in + CSS pass governance) |
| `…/Hexalith.Tenants.UI.Tests … -class …TenantsUiCompositionTests` | **Total: 19, Failed: 0** (EN/FR key parity incl. the 2 new keys) |
| `…/Hexalith.Tenants.Contracts.Tests -noLogo -noColor` | **Total: 112, Failed: 0** |

Route-smoke (`Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests`) and the other Tier 2/Tier 3
suites are Aspire/`dapr init`-gated (Tier 3 is `continue-on-error` in CI). No local distributed stack was
running (only `dapr_redis`/`dapr_scheduler`/`dapr_placement`/`dapr_zipkin` containers were up; no Tenants
AppHost/EventStore/UI/Keycloak). They were not run locally — see the runtime blocker below.

## Runtime / external blockers

- **PLATFORM-OPS-1 (runtime, OPEN).** Authenticated `get-user-tenants` needs the EventStore operational
  index (`admin:query-types:tenants`) published at startup. No live self-audit surface was reachable this
  session (no AppHost running). Reopen trigger: a coherent `-p:UseHexalithProjectReferences=true` source
  rebuild of all host/Tenants child projects **and** the local revertable patch dropping the early
  `return` on `HasFailures` in `AdminOperationalIndexHostedService.StartAsync` (recipe recorded in the
  Story 1.2 evidence). Responsive/forced-colors/AT proof for this run rests on the grid-scoped CSS hooks +
  the green conformance suite; bUnit is not browser/AT proof. The local Chrome lane is a fixed ~1235px
  viewport (`window.resize` is a no-op), so responsive proof is via CSS rules, not resize.
- **PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1 (freshness provenance + direct reads, OPEN, platform-owned).**
  The self-audit read is submitted through the injected server-side query client keyed by
  `GetUserTenantsQuery` `Domain=tenants` / `QueryType=get-user-tenants` / `ProjectionType=tenant-index`.
  Migrating to the direct `GET /api/users/{id}/tenants` read and closing the AD-6/AD-8 gateway freshness
  normalization is platform-owned remediation, not a Story 1.4 defect. The surface renders freshness
  honestly (`current/stale/unknown`) and fails closed where the safety contract requires. Note: the old
  `ProjectionActorType = TenantProjectionRouting.ActorTypeName` pitfall (Story 1.3) no longer applies —
  `TenantProjectionRouting` no longer exists; tenant reads route to in-process query handlers, and this
  story introduced no query-request change.
- **AD-14 / single replica.** Cursor durability is not assumed; the invalid-cursor recovery to page 1
  (`Previous` seeds history with `null`) is preserved.

## Per-AC verdict

| AC | Verdict | Evidence |
|---|---|---|
| AC1 — server-derived identity, single workspace scope | **verified** | `TenantsWorkspace.razor` renders `<MyTenantsPanel InitialCursor=…/>` with no browser user id; `TenantQueryGateway.GetMyTenantsAsync` reads `IUserContextAccessor.UserId`, fails closed to `Unauthorized(MissingAuthenticatedUser)`, force-overwrites `TargetUserId`. New UI-level test `My_tenants_scope_sends_no_browser_supplied_target_user_id` proves the surface sends `TargetUserId == null`. Canonical `tab=tenants&scope=mine`, no separate shell entry. |
| AC2 — authorization-scoped membership rows | **verified** | `MyTenantsDataGrid` renders literal id + role + status + freshness; contracts-only row (`{TenantId,Name,Status,Role}` + freshness); no out-of-scope data; no counts leaked. |
| AC3 — authorization-safe empty | **verified** | `MyTenantsState` Empty → `role=status`/`aria-live=polite`, "this authorized empty result is not an error" copy, `tenants-my-empty`; no existence/count disclosure. |
| AC4 — distinct loading/failure/stale/degraded | **verified** | All 8 `UserTenantMembershipSurfaceKind`s have explicit branches; `Invalid` (HTTP 400) does not collapse into an empty grid; failure kinds `role=alert`/assertive, non-blocking polite; `unknown` never inferred as current. |
| AC5 — owner drill-in + full scope restore | **changed (implemented)** | Added shared `/tenants/{tenantId}` drill-in from self-audit rows (opt-in `DetailHref`), extended the `MyScope` `FromQuery`/`ToCanonicalUrl` to carry+emit `selected`/`anchor`, and added the scope=mine return-context banner. Cursor resets to page 1 on return, mirroring scope=all. See the grid-divergence decision below. |
| AC6 — no global-administrator surface for non-admins | **verified** | Workspace exposes only page-local Tenants/Users tabs; no Global Administrators entry/hidden admin data/operator action on the self-audit surface; `IsGlobalAdmin` server gate is the enforcement boundary. |
| AC7 — responsive/accessible rows | **changed (fixed)** | Added the missing row `id="{SelectorPrefix}-row-{TenantId}"` so the `ReturnFocus` anchor resolves (was a no-op); grid-scoped CSS keeps overflow/min-width/nowrap/forced-colors/`:focus-visible`/logical-direction, including a new forced-colors + focus-visible block for the detail link. |
| AC8 — EN/FR parity + culture-aware formatting | **verified** | Added `Tenants.MyTenants.DetailLinkLabel` + `Tenants.MyTenants.ReturnContext` to both resx (whole strings with `{0}`); parity test green; selectors `tenants-my-*` do not depend on localized text/color; kept off the Users lookup surface. |
| AC9 — focused evidence | **verified** | +6 focused tests (identity/no-browser-target, drill-in href + focus-anchor id, state round-trip, cursor-reset detail URL, query-changing null-out, workspace return-context banner); commands/results recorded above; runtime blocker recorded. |

## Grid-divergence decision (AC5 open product question — recorded for review)

AC5 says "the same tenant-list and detail components are used." Resolution taken (recommended option (a)):
the **detail** requirement is satisfied literally — the self-audit row drills into the shared
`/tenants/{tenantId}` `TenantDetailPage` under server-enforced scope, and returns restore scope=mine +
selection + cursor + focus via the shared `TenantListNavigationContext` / workspace return-context
mechanism. The **list** keeps the deliberately Role-augmented `MyTenantsDataGrid` (adds the caller's Role
— the point of "my role in each" — and omits Members/Owners/Pending/in-grid sort), composed from the same
Fluent/FrontComposer primitives as `TenantDataGrid`. `MyTenantsDataGrid` was **not** replaced with a
literal `TenantDataGrid` (which lacks the Role column); doing so without Product/UX approval would regress
the self-audit user story. Flagged for reviewer confirmation.

## Set-but-unread `IsAuthorizationScopedEmpty` (recorded decision)

`UserTenantMembershipSnapshot.Empty(isAuthorizationScoped: true, …)` sets the flag, but the UI branches on
`SurfaceKind is Empty` + the authorization-safe copy, not the boolean. Kept as an intentional, unused
signal (available for future telemetry): the UI contract for the authorization-safe empty state is the
`SurfaceKind` + copy, not the flag. No behavior change made.
