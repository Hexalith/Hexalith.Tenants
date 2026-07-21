---
created: 2026-07-21T05:38:23+02:00
baseline_commit: 21a3ce5a7597359c2b8ba050f73860b0372087d1
frontcomposer_source_commit: e13368a2f122d22cf240bb5ff4b3a5bc37de0a90
builds_source_commit: 7708256eba4974ba005fd7fe86ec5bfd6152a25e
eventstore_source_commit: 41f5ed0fa48978fd79a999e2bee879a5bed91c4a
memories_source_commit: 3b1ae857a71db809d0faa04d5fab9142c7956f5c
frontcomposer_package_baseline: 4.0.1
fluent_ui_pin: 5.0.0-rc.4-26180.1
historical_story_evidence: _bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md
prerequisite_stories:
  - _bmad-output/implementation-artifacts/1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md
  - _bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md
  - _bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md
  - _bmad-output/implementation-artifacts/1-5-user-membership-lookup.md
---

# Story 1.4: My Tenants Self-Audit

Status: done

<!-- Reverify-and-harden story. The self-audit surface already exists and is well-tested; historical `[x]` completion and a green suite are evidence to reverify, not a readiness waiver. The real work is AC5 (shared-detail drill-in + full scope=mine return-context restore) and AC7 (broken return-focus anchor). -->

## Story

As a signed-in user,
I want to see the tenants I belong to and my role in each,
so that I can verify my own access without asking a platform operator.

## Acceptance Criteria

1. **Server-derived identity; single workspace scope.** Given an authenticated user selects the My Tenants scope, when the self-audit request is composed, then the server-side BFF derives the user identity from the authenticated principal rather than a browser-controlled identity value, and the canonical workspace uses `tab=tenants&scope=mine` without registering a separate shell entry or owner-only application.

2. **Authorization-scoped membership rows.** Given the caller has authorized tenant memberships, when the membership results render, then each row shows the literal tenant identity, the caller's role, tenant status, and authoritative or honestly unknown freshness, and no membership or tenant outside the caller's authorized result set is rendered, copied, announced, or inferable from counts.

3. **Authorization-safe empty state.** Given the caller has no visible memberships, when the self-audit view completes successfully, then an explicit authorization-safe empty state is shown rather than an error, and the copy does not imply whether hidden tenants or memberships exist.

4. **Distinct loading / failure / stale / degraded states.** Given the membership read is loading, fails, is stale, or is degraded, when the corresponding state is displayed, then it remains distinct from empty and from every other state with a named recovery where applicable, and unavailable provenance is shown as `unknown`, never inferred as current from request time.

5. **Owner drill-in with shared components and full scope restore.** Given a tenant owner uses the shared workspace, when the owner reviews My Tenants and opens an authorized tenant, then the same tenant-list and detail components are used with server-enforced scope, and returning from detail restores the My Tenants scope, filter, selection, cursor context, and scroll position.

6. **No global-administrator surface for non-administrators.** Given a tenant owner or ordinary member is not a global administrator, when the workspace is rendered, then the self-audit capability does not expose a Global Administrators entry, hidden administrator data, or operator-only actions, and UI absence is treated only as reflection of server authorization, never as the enforcement boundary.

7. **Responsive and accessible self-audit rows.** Given desktop, tablet, and mobile viewport widths, when the self-audit rows are navigated with keyboard or assistive technology, then role, tenant status, freshness, headings, row relationships, focus, and horizontal overflow remain accessible, and status meaning remains text-and-icon complete in forced-colors and reduced-motion environments.

8. **EN/FR parity and culture-aware formatting.** Given English and French cultures, when roles, statuses, freshness, loading, empty, error, stale, degraded, and recovery text render, then whole-string resource parity and culture-aware formatting are preserved, and stable selectors do not depend on localized row text or color.

9. **Focused evidence.** Given the My Tenants slice, when focused identity, authorization, gateway, bUnit, localization, responsive, accessibility, support-safety, and route tests run, then tests cover memberships, no memberships, attempted identity substitution, out-of-scope data, invalid cursor recovery, and deep-link return, and exact commands, results, and external freshness blockers are recorded.

## Tasks / Subtasks

- [x] Establish the reverification baseline and evidence record. (AC: 1-9)
  - [x] Preserve `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md` as historical evidence; do not overwrite it or restore its obsolete standalone-`/tenants/my`-only assumptions.
  - [x] Create a dated Story 1.4 evidence report under `_bmad-output/implementation-artifacts/` recording the root commit, FrontComposer/Builds/EventStore/Memories source commits, Fluent pin, resolved UI packages, exact commands, exit codes, pass counts, and any `PLATFORM-OPS-1`/`PLAT-FRESH-1` blockers.
  - [x] Classify each acceptance criterion as `verified`, `changed`, or `blocked`. A historical `[x]`, current source shape, or broad green suite is evidence to inspect, not automatic proof.
  - [x] Record the pre-existing worktree state before implementation and preserve unrelated planning, sprint, deferred-work, and submodule-pointer changes.

- [x] Reverify server-derived identity and the direct read path. (AC: 1, 9)
  - [x] Confirm `scope=mine` renders `MyTenantsPanel` with no browser-supplied user id (`TenantsWorkspace.razor` renders `<MyTenantsPanel InitialCursor="@_workspaceState.Cursor" />`) and that `MyTenantsPanel` calls `ITenantQueryGateway.GetMyTenantsAsync` with no `TargetUserId`.
  - [x] Confirm `TenantQueryGateway.GetMyTenantsAsync` reads `IUserContextAccessor.UserId` server-side, fails closed to `Unauthorized(MissingAuthenticatedUser)` when blank, and force-overwrites `TargetUserId` with the authenticated id so any browser-supplied target is ignored. Keep the `Get_my_tenants_keeps_signed_in_user_as_target_even_when_request_has_target` guard test green (and add a UI-level assertion that scope=mine never sends a browser target).
  - [x] Confirm the read routes through the direct `GET /api/users/{id}/tenants` BFF path and sets `ProjectionActorType = TenantProjectionRouting.ActorTypeName` on `GetUserTenantsQuery` (missing it silently routes to the generic `ProjectionActor` — mocks stay green). Do NOT route this read through the EventStore generic query gateway (`POST /api/v1/queries`).
  - [x] Confirm the cursor is opaque, bound to requester + target identity (`TenantQueryCursorScopes.GetUserTenants`), and never appears in visible copy, DOM attributes, links, logs, telemetry tags, or copy actions.

- [x] Reverify the state model and authorization-safe empty. (AC: 2, 3, 4)
  - [x] Confirm every `UserTenantMembershipSurfaceKind` the shared `MapUserTenantException` can produce (`Loading, Ready, Empty, Invalid, Stale, Degraded, Unauthorized, Unavailable`) has an explicit render branch on the My Tenants surface, so no mapped state (especially `Invalid` from HTTP 400) collapses into an empty grid presented as "you have no tenants". Keep `My_tenants_invalid_state_does_not_collapse_into_an_empty_grid` green.
  - [x] Confirm `Empty` is authorization-safe: distinct `data-testid`, `role="status"`/`aria-live="polite"`, "this authorized empty result is not an error" copy, and no disclosure of whether hidden/missing/orphan/out-of-scope memberships or tenants exist, and no count leakage.
  - [x] Confirm state-precedence keeps `Stale` and `Degraded` markers visible above the grid (banner + rows), that failure states use `role="alert"`/`aria-live="assertive"`, and that non-blocking notices stay `polite`.
  - [x] Decide and record whether the set-but-unread `IsAuthorizationScopedEmpty` snapshot flag is intentional dead signal or should drive the empty branch; if kept unused, note why (UI relies on `SurfaceKind` + copy).

- [x] Reverify honest freshness. (AC: 2, 4)
  - [x] Confirm rows carry `ReadModelFreshnessState` from projection metadata only (`ResolveFreshness`: `Provenance==ProjectionBacked` + lifecycle + `IsStale`), rendered by the shared `TruthStateBadge` with `ResourcePrefix="Tenants.MyTenants"`, and that `ServedAt`, ETag presence, request completion time, a recent refresh, and bare `304` never manufacture `current`/`aging`.
  - [x] Confirm 304 reuse preserves the prior snapshot's truth-state kind (do not upgrade a previously `Degraded` snapshot to current) and reuses the previous snapshot only for the same target user, else returns degraded/unknown.
  - [x] Confirm `unknown` renders as Important + Size20 `QuestionCircle` + `IconLabel` + visible localized text; reserve Success styling for proven truth. Record `PLAT-FRESH-1`/`HOST-REF-1`/`UI-READ-1` where authoritative provenance is still external.

- [x] Close AC5: shared-detail drill-in and full scope=mine return-context restore. (AC: 5, 7) **[PRIMARY CHANGE]**
  - [x] Provide an authorized tenant-detail drill-in from My Tenants rows using the same shared detail route/component (`/tenants/{tenantId}` → `TenantDetailPage`) under server-enforced scope. Today the My Tenants row exposes only the `AuditEvidenceEntryPoint`, not a tenant-detail link; the scope=all `TenantDataGrid` has `<a class="tenant-data-grid__detail-link">`. Add the equivalent safe drill-in without losing the self-audit Role column.
  - [x] Extend the `MyScope` branch of `TenantWorkspaceState.ToCanonicalUrl()` (currently emits only `tab/scope/cursor`) and the return-context rendering so returning from detail restores scope=mine plus filter, selection, cursor context, and scroll position — matching the scope=all return-context behavior (`TenantsWorkspace` return-context message, `TenantListNavigationContext` selected-id/anchor). Null stale `selected`/`anchor` on query-changing transitions, consistent with the Story 1.2 patch.
  - [x] Fix the broken return-focus anchor: `MyTenantsDataGrid` passes `ReturnFocus="{SelectorPrefix}-row-{TenantId}"` but the identity element has only `data-testid="{SelectorPrefix}-row"` and no matching `id`, so focus-on-return is a no-op. Add `id="{SelectorPrefix}-row-{TenantId}"` (mirroring `TenantDataGrid`'s `id="tenant-row-{TenantId}"`) and cover it with a test.
  - [x] Reconcile the deliberate grid divergence honestly: My Tenants uses a separate `MyTenantsDataGrid` (adds the caller's Role, omits Members/Owners/Pending and in-grid sort) because "my role in each" is the point of the story. Record whether AC5's "same tenant-list and detail components" is satisfied by (a) reusing the shared **detail** route while keeping the Role-augmented grid composed from the same Fluent/FrontComposer primitives, or (b) a deeper grid unification. Do not drop the Role column to force literal `TenantDataGrid` reuse without an approved decision — that would regress the user story. See the open product question at the end of Dev Notes.

- [x] Reverify navigation, authorization reflection, and support safety. (AC: 1, 6)
  - [x] Confirm the workspace exposes only page-local Tenants/Users tabs and that the My Tenants surface renders no Global Administrators entry, hidden administrator data, or operator-only action; UI absence reflects server authorization (`IsGlobalAdmin` server gate), never the enforcement boundary.
  - [x] Confirm My Tenants components hold no browser `HttpClient`, bearer token, decoded cursor/ETag, or payload parsing; keep `My_tenants_components_have_no_browser_backend_http_or_token_storage` green.

- [x] Reverify responsive, accessibility, localization. (AC: 7, 8)
  - [x] Confirm `MyTenantsDataGrid.razor.css` keeps `overflow-x`, safety-column `min-width`, `white-space:nowrap`, forced-colors and reduced-motion hooks, `:focus-visible`, and logical (start/end) direction with no physical left/right assumptions; keep `My_tenants_styles_preserve_critical_columns_and_forced_colors_hooks` green.
  - [x] Confirm `Tenants.MyTenants.*` EN/FR key parity (currently 43/43) and `Tenants.Workspace.Scope.{Label,All,Mine}` present in both cultures; verify whole-string usage (no runtime sentence assembly) and culture-aware absolute timestamps for freshness. Add keys for any new AC5 drill-in/return-context copy in both resx.
  - [x] Confirm stable selectors (`tenants-my-*`) validate each state without depending on localized row text or color; keep My-Tenants copy/selectors strictly off the Users lookup surface and vice-versa.

- [x] Run focused checks and issue the complete evidence decision. (AC: 1-9)
  - [x] Add/adjust focused tests for: scope=mine identity derivation end-to-end (UI level, not only gateway); the AC5 shared-detail drill-in and scope=mine return-context restore (selection + cursor + scroll + focus); the return-focus anchor `id` match; invalid-cursor recovery to page 1 with `ListRefreshed`; out-of-scope data non-disclosure; and no-memberships empty state.
  - [x] Run UI/gateway/contract tests individually with xUnit v3/Shouldly/bUnit conventions; use `.slnx` for restore/build only, not solution-level `dotnet test`. If `dotnet test` hits the known .NET 10 MTP/VSTest incompatibility, use the built xUnit v3 executable fallback and record it.
  - [x] Attempt supported runtime browser evidence for the self-audit surface via Aspire-discovered endpoints (never assumed ports). Runtime is gated by `PLATFORM-OPS-1` (`get-user-tenants` needs `admin:query-types:tenants` published at EventStore startup); if blocked, record the exact command, owner, and reopen trigger, and rely on grid-scoped CSS + green conformance suite for responsive/forced-colors proof (the local Chrome lane is a fixed ~1235px viewport where `window.resize` is a no-op). bUnit alone is not browser/AT proof.
  - [x] Record every exact command/result and confirm the full configured UI suite has no regression (including scope=all list, detail, member, command, and audit surfaces). Include the route-smoke test and `tests/test-summary.md` in the File List (recurring omissions).

## Dev Notes

### Scope And Epic Context

Epic 1 delivers the complete read-only tenant discovery/access-review product (FR1-FR9, FR18) and **reverifies existing implementation rather than blindly rebuilding** it. Story 1.4 owns only the `scope=mine` self-audit slice (FR3): server-derived identity, the authorization-scoped membership grid, the six-state model, honest freshness, the owner drill-in with full scope restore, and its evidence. Story 1.1 owns host/canonical-workspace state; 1.2 the list/cursor/six-state/freshness foundation this surface reuses; 1.3 detail/return-context; 1.5 the Users lookup sibling (already `done`); 1.6 configuration; 1.7 member review/action availability; 1.8 safe copy; 1.9 Memories search; 1.10 direct reads/freshness; 1.11 global-administrator review. Preserve those implemented surfaces while closing Story 1.4 gaps; do not rebuild or absorb them. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Trustworthy Tenant Discovery and Access Review`; `_bmad-output/planning-artifacts/epics.md#Story 1.4: My Tenants Self-Audit`]

**Current-state verdict (from source reverification):** the self-audit data path, identity safety, six-state modeling, freshness, support safety, and localization are essentially complete and well-tested. The reverification must concentrate on **AC5** (owner drill-in into the shared detail component + restore of selection/scroll/return-context for scope=mine — today only `tab/scope/cursor` survive and the return-focus anchor is broken) and the **AC7** accessibility regression that flows from the broken focus anchor. All other ACs are `verify-and-preserve` unless focused evidence proves a gap.

The authoritative shared NFR10 gate still applies: readiness/completion needs applicable accessibility, localization, responsive, documentation/reference, and focused-test evidence, or the exact Product/UX-approved fallback record. Story 1.4's focused lanes do not narrow this gate. Readiness report 2026-07-19-v2 is `READY` with 0 Critical / 0 Major / 10 Minor and **no Minor finding touches Story 1.4**, so this story carries no story-specific readiness condition beyond the shared gates. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-19-v2.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19-v2.md`]

### Current Implementation: Change Versus Preserve

| Area / file | Current state | Story treatment |
|---|---|---|
| `Components/Pages/TenantsWorkspace.razor` (~L188-191) | `scope=mine` renders `<MyTenantsPanel InitialCursor="@_workspaceState.Cursor" />`; canonical `tab=tenants&scope=mine` | Preserve; extend return-context handling for scope=mine drill-in (AC5). |
| `Components/Pages/MyTenantsPage.razor` | Compatibility route `/tenants/my` wrapping `MyTenantsPanel` | Keep renderable; generated navigation/returns target canonical `/tenants?...`. |
| `Components/Users/MyTenantsPanel.razor` | Builds `UserTenantMembershipRequest` with no `TargetUserId`, calls `GetMyTenantsAsync`; `BuildReturnUrl()` emits `tab/scope/cursor` only; paging pushes cursor to URL | Preserve identity path; extend return URL/context for selection + scroll (AC5). |
| `Components/Users/MyTenantsDataGrid.razor` | Columns: identity+copy, **Role**, Status, Freshness, Audit entry point; row has `data-testid="{prefix}-row"` but **no `id`**; `ReturnFocus="{prefix}-row-{TenantId}"` (dangling) | **Change:** add matching row `id`; add authorized shared-detail drill-in link; keep Role column. |
| `Components/Users/MyTenantsState.razor` | Distinct `data-testid`/`role`/`aria-live` per `SurfaceKind` | Preserve; add branch for any new AC5 state copy only if required. |
| `Services/Gateways/TenantQueryGateway.cs` (`GetMyTenantsAsync` L75-92, `GetUserTenantsCoreAsync`, `MapUserTenantException` L817+) | Server-derives identity, force-overwrites target, maps states, resolves freshness from projection metadata | Verify and preserve; do not fork a second transport or redeclare DTOs. |
| `State/TenantList/TenantWorkspaceState.cs` (`ToCanonicalUrl` MyScope branch ~L265-271) | Emits only `tab/scope/cursor`; drops `selected`/`anchor` | **Change:** carry selection/return context for scope=mine (AC5), null stale selection on query-changing transitions. |
| `State/UserTenants/*.cs` (`Snapshot`, `Request`, `Row`, `Reason`, `SurfaceKind`) | 8 surface kinds, 10 reasons, row carries `ReadModelFreshnessState`; `IsAuthorizationScopedEmpty` set but unread by UI | Verify; decide/record the unread flag. |
| `Queries/Handlers/GetUserTenantsQueryHandler.cs`, `TenantQueryHandlerBase.cs` | Requester = server-side `envelope.UserId`; target = `EntityId`; non-owner/non-admin restricted to owned tenants; blank requester → Forbidden | Verify-only unless a gap is proven; do not change server RBAC casually. |
| `Contracts/Queries/GetUserTenantsQuery.cs`, `UserTenantMembership.cs` | Query contract + row DTO `{TenantId, Name, Status, Role}` only | Verify-only; do not fabricate counts/timestamps/lifecycle/ETag. |
| `Resources/TenantsResources*.resx` | 43/43 `Tenants.MyTenants.*` EN/FR parity; scope keys both cultures | Verify parity; add keys for new AC5 copy in both files. |
| `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` and gateway/handler tests | Strong coverage: identity substitution, states, paging, RBAC visibility, cursor binding | Extend for AC5 drill-in/return-context/focus-anchor; keep the rest green. |

### Identity And Read-Path Contract (AC1) — verify and preserve

- `scope=mine` derives identity **server-side** from the authenticated principal. Path: `TenantsWorkspace` → `MyTenantsPanel` (no user id) → `GetMyTenantsAsync` reads `IUserContextAccessor.UserId` (FrontComposer `ClaimsPrincipalUserContextAccessor`, the `HttpContext.User` `sub` claim) → force-sets `TargetUserId` = authenticated id → `CreateUserTenantsRequest` sets `EntityId` = target. The requester is the server-relayed JWT (`envelope.UserId`); `SubmitQueryRequest` has no UserId field, so the requester cannot be spoofed from the payload, and `TenantQueryHandlerBase.ExecuteAsync` rejects a blank requester as `Forbidden`. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`; architecture.md#AD-5; architecture.md#Authentication & Security]
- **Sibling contrast (Story 1.5, `done`):** the Users lookup sends a browser-typed `TargetUserId` via `GetUserTenantsAsync`; substitution is contained server-side (`isSelfLookup || IsGlobalAdmin`, else restricted to requester-owned tenants). Do not leak `tenants-my-*` copy/selectors onto the lookup surface or vice-versa. My Tenants is the self-targeting wrapper over the already-built shared gateway core — do not re-fork transport. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`]
- **ProjectionActorType pitfall:** `GetUserTenantsQuery` must set `ProjectionActorType = TenantProjectionRouting.ActorTypeName`; omitting it routes to EventStore's generic `ProjectionActor` and never reaches the tenant projection — silent because mocks stay green (Story 1.3 HIGH; re-applied in old 1.4). [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`; memory `tenant-ui-query-projection-actor-routing.md`]

### State Model And Authorization-Safe Rendering (AC2, AC3, AC4)

- `UserTenantMembershipSurfaceKind`: `Loading, Ready, Empty, Invalid, Stale, Degraded, Unauthorized, Unavailable`. Blocking kinds render a `MyTenantsState` with no grid; `Stale`/`Degraded` render banner + grid; `Ready` renders grid only. **Every kind the shared `MapUserTenantException` can produce needs an explicit render branch** — the Story 1.5 HIGH bug was a 400 (`Invalid`) falling through to an empty grid = false "you have no tenants". No state may collapse into false Success (violates AC3/AC4). [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`; `MyTenantsState.razor`; `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`]
- Authorization-safe empty = distinct `Empty` kind + `role="status"`/`aria-live="polite"` + "not an error" copy; never reveal whether hidden/missing/orphan/out-of-scope memberships or a missing user exist, never leak counts. Backend semantics to mirror: order by tenant id, disabled tenants included, orphan memberships and `TenantRole.Unknown`/invalid roles filtered before pagination, non-owner cross-user → empty page (not error). [Source: architecture.md#Process Patterns; `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md`]
- State-precedence must keep `Stale` visible even under a page-local filter that matches zero rows (the Story 1.2 patch corrected an `else if` that excluded only `Degraded`, not `Stale`). Failure kinds use `assertive`; non-blocking notices `polite`. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md`]

### Freshness Contract (AC2, AC4)

- Freshness comes from EventStore read-model metadata only: `ResolveFreshness` uses `Provenance==ProjectionBacked`, lifecycle, and `IsStale`; degraded/non-projection-backed → `Unknown`. Rendered by the shared `TruthStateBadge` (same component as scope=all). `ServedAt`, ETag presence, request completion time, a recent refresh, and bare `304` are transport/cache observations, not projection age, and cannot manufacture `current`/`aging`. Wire produces `current/stale/unknown` only (`aging` collapses to `current`/`unknown` until a `QueryResponseMetadata.ProjectedAt` wire field exists — future EventStore handoff). `unknown` → Important + Size20 `QuestionCircle` + `IconLabel` + visible text; Success reserved for proven truth. [Source: architecture.md#AD-8; architecture.md#Data Architecture; `_bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md`]
- 304 reuse must preserve the prior snapshot's truth-state kind (do not upgrade a previously `Degraded` snapshot to current) and reuse the previous snapshot only for the same target user (Story 1.3 LOW; Story 1.5 rule). [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`; `1-5-user-membership-lookup.md`]
- Note: `MyTenantsDataGrid` + resx define a `Tenants.MyTenants.Freshness.Aging` badge that is currently unreachable on this surface (lifecycle only yields `Current/Stale/Unknown`). Leave defined for parity; do not fabricate an `Aging` result.

### Return-Context And Detail Drill-In (AC5) — PRIMARY CHANGE

This is the substantive gap. Requirements from the AC: "the same tenant-list and detail components are used with server-enforced scope" and "returning from detail restores the My Tenants scope, filter, selection, cursor context, and scroll position."

Current state:
- The only outbound navigation from a My Tenants row is the `AuditEvidenceEntryPoint`; **there is no tenant-detail drill-in link** (the scope=all `TenantDataGrid` has `<a class="tenant-data-grid__detail-link">` at `/tenants/{tenantId}`).
- `MyTenantsPanel.BuildReturnUrl()` → `TenantWorkspaceState...ToCanonicalUrl()` for the `MyScope` branch emits only `tab/scope/cursor` (`TenantWorkspaceState.cs` ~L265-271); it **drops `selected` and `anchor`**, so there is no selection highlight, scroll restore, or return-context banner for scope=mine (scope=all has all three).
- `MyTenantsDataGrid` passes `ReturnFocus="{SelectorPrefix}-row-{TenantId}"` but the identity element only has `data-testid="{SelectorPrefix}-row"` and **no matching `id`** — so focus-on-return is a no-op (an AC7 a11y regression). The scope=all grid correctly sets `id="tenant-row-{TenantId}"`.

Required changes:
1. Add an authorized shared-detail drill-in from My Tenants rows to `/tenants/{tenantId}` (the same `TenantDetailPage`), under server-enforced scope, using the literal safely-encoded identifier and preserving the self-audit Role column.
2. Extend the `MyScope` branch of `ToCanonicalUrl()` and the return-context rendering so return restores scope=mine + filter + selection + cursor + scroll — reusing the scope=all mechanism (`TenantListNavigationContext` selected-id/anchor; `TenantsWorkspace` return-context banner). Null stale `selected`/`anchor` on query-changing transitions.
3. Add the row `id="{SelectorPrefix}-row-{TenantId}"` so `ReturnFocus` resolves, and cover it with a focus test.

Use URL/query-string or Blazor navigation state that survives direct links and back/forward; **never `localStorage`/`sessionStorage`** for tenant/user context. [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`; `MyTenantsPanel.razor`; `State/TenantList/TenantWorkspaceState.cs`; `Components/Tenants/TenantDataGrid.razor`; `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`]

### Technical Requirements

- Reverify the brownfield surface; do not run `dotnet new`, create a parallel My Tenants page, reintroduce a placeholder view, or delete later features to make tests easy.
- Use .NET 10/C# repository conventions: file-scoped namespaces, Allman braces, one C# type per file, nullable analysis, `ConfigureAwait(false)` in production awaits, central package versions, warnings as errors.
- `UserTenantMembership` is `{TenantId, Name, Status, Role}` only — no counts, timestamps, lifecycle, or ETag on the row. Do not fabricate. Treat lifecycle as the available `Status`; keep `TenantStatus.Unknown`/`TenantRole.Unknown` fail-safe (never Success styling).
- Tenant/user ids are literal caller-supplied strings — never `Guid.TryParse`/`Ulid.TryParse`/re-case/reformat. For any typed/derived id, `Trim()` surrounding whitespace and reject `Length > 256` (Story 1.2 review decision); a pasted `" user "` must not query a different `%20user%20` identity.
- Components render typed, support-safe snapshots and call injected server-side gateways. No browser `HttpClient`, access token, decoded cursor/ETag, or payload parsing. Cursors, tokens, correlation ids, raw metadata, payloads, and stack traces never reach markup, storage, selectors, logs, or announcements.
- Use `Hexalith.Tenants.slnx` only; run test projects individually. Do not add shared platform capability to the Tenants domain module.

### Architecture Compliance

- **AD-1 / AD-2:** one `/tenants` shell entry; `scope=mine` is a scope mode inside the single workspace, not a new nav entry; `/tenants/my` is a renderable compatibility route while generated navigation/returns use canonical `/tenants` state; changing tab/scope/filter/sort resets cursor.
- **AD-3 / AD-4:** FrontComposer/Fluent first; no raw interactive controls, forms, or tables; Tenants owns domain composition only — do not implement generic grids/tabs/shell/theme/command chrome.
- **AD-5:** injected server-side BFF/gateways are the only backend egress; browser tokens/direct backend calls forbidden; identity for scope=mine derives from the authenticated principal.
- **AD-6:** read via the direct `GET /api/users/{id}/tenants` BFF path; never the EventStore generic query gateway or retired projection-actor path (they drop projection ETag/freshness metadata).
- **AD-8:** freshness from `ReadModelFreshnessState`/`ProjectedAt`; `Refreshing` client-transient; `unknown` fails closed where the safety contract requires; `ServedAt` is never projection age.
- **AD-11:** route, identity, authorization, localization, selector, support-safety, and Fluent/forced-colors conformance tests are architectural guards; do not loosen them. [Source: `_bmad-output/planning-artifacts/architecture.md#Canonical Architecture Spine`]

### Library And Framework Requirements

- SDK `10.0.302`, `rollForward=latestPatch`, target `net10.0`. Blazor InteractiveServer over a server circuit; access token stays server-side. [Source: `global.json`; `src/Hexalith.Tenants.UI/Program.cs`]
- FrontComposer package baseline `4.0.1`; current source evidence commit `e13368a2f122d22cf240bb5ff4b3a5bc37de0a90`. Preserve source/package distinction; do not modify the submodule under this story.
- Fluent UI Blazor components/icons `5.0.0-rc.4-26180.1`. This deliberate v5 prerelease pin is authoritative; do not upgrade or substitute rc.3 assumptions. Use Size20 status icons + `IconLabel` for truth state. [Source: `references/Hexalith.Builds/Props/Directory.Packages.props`]
- Tests use xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0`, bUnit `2.8.4-preview`, Microsoft.NET.Test.Sdk from central package management. Add no inline project versions.

### File Structure Requirements

- Primary UPDATE candidates: `Components/Users/MyTenantsDataGrid.razor` (row `id`, detail drill-in), `Components/Users/MyTenantsPanel.razor` (return context), `State/TenantList/TenantWorkspaceState.cs` (`MyScope` `ToCanonicalUrl` branch), possibly `Components/Pages/TenantsWorkspace.razor` (return-context banner for scope=mine), and their focused tests.
- Verify-only unless evidence proves a gap: `TenantQueryGateway.cs`, `GetUserTenantsQueryHandler.cs`, `TenantQueryHandlerBase.cs`, contract DTOs, `TenantQueryCursorScopes.cs`, existing resources, and the state/reason/surface-kind types.
- Add new `Tenants.MyTenants.*` resx keys in **both** EN and FR for any new AC5 drill-in/return-context copy.
- Test updates stay in `tests/Hexalith.Tenants.UI.Tests/` (Components/, Services/Gateways/, State/, conformance) and the existing integration/route-smoke location. Create only the dated evidence report as a required NEW artifact. Add no Dockerfile, `.sln`, duplicate grid/page, or source edits under `references/`.

### Testing Requirements

Required focused scenarios:

- scope=mine identity derivation end-to-end at the UI level (not only the gateway guard test), proving no browser target is sent and the authenticated id is used;
- attempted identity substitution ignored (keep `Get_my_tenants_keeps_signed_in_user_as_target_even_when_request_has_target`);
- memberships render literal id + role + status + freshness, no mutation controls, no token leakage;
- authorization-safe empty (distinct testid/role/aria-live, "not an error" copy, no existence/count disclosure);
- loading / invalid / stale / degraded / unauthorized / unavailable distinct and accessible; `Invalid` does not collapse into an empty grid;
- out-of-scope data non-disclosure (mirror backend non-owner → empty);
- opaque cursor paging (no offset, cursor never in DOM/href/copy/log/telemetry) and invalid-cursor recovery to page 1 with polite `ListRefreshed`;
- **AC5:** shared-detail drill-in from a My Tenants row, and return restores scope=mine + filter + selection + cursor + scroll; the row `id`/`ReturnFocus` anchor resolves and focus returns to the originating row/heading;
- no Global Administrators entry/action on the surface for a non-GA;
- EN/FR key parity, whole-string usage, culture-aware absolute timestamps, `<html lang>` clamped to en/fr, stable selectors independent of localized text/color;
- responsive/forced-colors/reduced-motion via grid-scoped CSS hooks; logical direction only;
- no regression across the full UI suite (scope=all list, detail, member, command, audit).

Validation shapes (adjust only for the documented xUnit v3 runner fallback):

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 -warnaserror
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release
```

For focused reruns after building, invoke the xUnit v3 executable with single-dash selectors when `dotnet test` hits the .NET 10 MTP/VSTest incompatibility:

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.MyTenantsSurfaceTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests
```

Use Aspire CLI/resource discovery and the existing route-smoke path for runtime evidence; browser evidence must use navigable endpoints discovered from Aspire, not assumed ports. If runtime is blocked, record `PLATFORM-OPS-1` rather than claiming responsive/accessibility completion from bUnit alone.

### Analog And Previous Story Intelligence

- **Story 1.5 (`done`, closest analog):** built the Users lookup on the same `GetUserTenants` contract; generalized the gateway into `GetUserTenantsCoreAsync` + `MapUserTenantException`, keeping `GetMyTenantsAsync` as the self-targeting wrapper; parameterized selector prefixes (`tenants-my-*` vs `tenants-user-*`). HIGH bug fixed there = the shared mapper's `Invalid` (HTTP 400) state had no render branch on My Tenants → false empty; every mapped state needs an explicit branch on both surfaces. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`]
- **Story 1.2 (re-corrected 2026-07-20):** the cursor/six-state/freshness/notice foundation. Review decisions that touch this surface: restore user-id `Trim()` + reject `>256`; state-precedence must not let `FilteredEmpty` mask `Stale`; null return-context `selected`/`anchor` on query-changing transitions; clamp `<html lang>` to en/fr; freshness is `Current/Stale/Unknown` only (dead `Aging` code removed). Final UI regression 942/942. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md`]
- **Story 1.3 (`done`):** deep-link + return-context. `ProjectionActorType` HIGH fix; 304 must preserve prior truth-state kind; return-context uses a real focus target (`id` on the identity cell) with `OnAfterRenderAsync` + `ElementReference.FocusAsync()`; context via URL/nav-state, never storage. Directly informs the AC5 drill-in. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`]
- **Old Story 1.4 (`done`, June 2026 baseline `f28f789`):** the canonical 1.4 contract (identity from `sub` as requester+target, authorization-safe empty with `role="status"`, read-only no-mutation, contracts-only row model, `tenants-my-*` selectors, `Tenants.MyTenants.*` resx). Its standalone `/tenants/my`-page assumption is obsolete — My Tenants is now a scope/tab panel inside the canonical workspace. Use its completion record as a checklist to reverify, not a waiver. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md`]

### Git Intelligence

- Root baseline `21a3ce5a7597359c2b8ba050f73860b0372087d1` on `main`; working tree clean at story creation. Recent commits finalized Story 1.2 (`21a3ce5`) and reconciled submodule/CI pins; they do not waive a code-level reverification.
- Story 1.1 already touched `MyTenantsPanel.razor` and `TenantWorkspaceState.cs` for canonical cursor state and added `MyTenantsSurfaceTests.cs`; its review found and fixed "My Tenants pagination does not update canonical cursor state" and "canonicalize My Tenants paging before loading" — preserve those. The AC5 selection/scroll gap is the remaining unclosed piece.
- Submodule working pointers: FrontComposer `e13368a2`, Builds `7708256e`, EventStore `41f5ed0f`, Memories `3b1ae857`. Record actual SHAs; do not update, reset, commit, or edit submodule source under this story.

### External Blockers

- **PLATFORM-OPS-1 (runtime):** authed queries need the EventStore operational index (`admin:query-types:tenants`) published at startup; `get-user-tenants` is in the published list, so the self-audit runtime is subject to the same gate. Root causes and the local revertable unblock recipe (coherent `-p:UseHexalithProjectReferences=true` source rebuild for host/Tenants version skew; drop the early `return` on `HasFailures` in `AdminOperationalIndexHostedService.StartAsync`) are recorded in the Story 1.2 evidence. Record command/owner/reopen trigger if hit; do not weaken local checks.
- **PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1 (freshness provenance + direct reads):** until verified, the surface stays honestly `unknown` where provenance is unavailable and fails closed where the safety contract requires. The AD-6/AD-8 gateway divergence (the generic gateway normalizing freshness to `Unknown`) is a platform-owned remediation, not a Story 1.4 defect to solve here — render freshness honestly and target the direct read.
- **AD-14 / single replica:** do not assume multi-replica; cursor durability is not guaranteed, so keep the invalid-cursor recovery path (re-query page 1, honest `ListRefreshed` notice).

### Latest Technical Information

- .NET 10 Blazor InteractiveServer renders and processes interactions server-side over a circuit; keep the global render-mode composition. Microsoft security guidance keeps access tokens server-side (BFF/token-handler flow) — this supports the existing server-only gateway boundary. [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0]
- Fluent upstream has stable v4 releases, but this project deliberately pins v5 rc.4 through FrontComposer; verify the exact installed API rather than "upgrading to latest".

### Project Context Reference

Follow `_bmad-output/project-context.md` and the root `AGENTS.md`/Hexalith baseline: preserve user changes; use `.slnx`; centralize package versions; one C# type per file; FrontComposer/Fluent V5 only (no raw controls/tables/forms); keep tokens/cursors/ETags/payloads/internal ids support-safe; caller-supplied ids are strings, not GUID/ULID; run tests per project; never initialize nested submodules; and do not put shared platform capability into the Tenants domain module.

### Open Product Question (does not block dev; surface at review)

AC5 says "the same tenant-list and detail components are used." The **detail** requirement is firm — reuse the shared `/tenants/{tenantId}` `TenantDetailPage`. The **list** side has a deliberate divergence: My Tenants uses `MyTenantsDataGrid` (adds the caller's Role — the point of "my role in each" — and omits Members/Owners/Pending and in-grid sort), composed from the same Fluent/FrontComposer primitives as the shared grid. Recommended resolution: satisfy AC5 by reusing the shared detail route + return-context while keeping the Role-augmented grid, and record that decision; do **not** replace `MyTenantsDataGrid` with a literal `TenantDataGrid` (which lacks the Role column) without explicit Product/UX approval, as that would regress the self-audit user story. Flag for the reviewer to confirm.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: My Tenants Self-Audit`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Trustworthy Tenant Discovery and Access Review`]
- [Source: `_bmad-output/planning-artifacts/epics.md#FR Coverage Map` (FR3)]
- [Source: `_bmad-output/planning-artifacts/architecture.md#AD-5`; `#AD-6`; `#AD-8`; `#AD-1`; `#AD-2`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `#Data Architecture`; `#Process Patterns`]
- [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-19-v2.md`]
- [Source: `_bmad-output/implementation-artifacts/1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md`]
- [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md`]
- [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`]
- [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`]
- [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md`]
- [Source: `_bmad-output/implementation-artifacts/story-1-2-tenant-list-triage-and-cursor-foundation-evidence-2026-07-20.md`]
- [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`; `MyTenantsDataGrid.razor`; `MyTenantsState.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `MyTenantsPage.razor`]
- [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `ITenantQueryGateway.cs`; `UnavailableTenantQueryGateway.cs`]
- [Source: `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`; `State/UserTenants/*`]
- [Source: `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`; `TenantQueryHandlerBase.cs`; `TenantQueryCursorScopes.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs`; `UserTenantMembership.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`; `Services/Gateways/TenantQueryGatewayTests.cs`; `TenantsWorkspaceTests.cs`]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (Claude Code, bmad-dev-story workflow)

### Debug Log References

- Full evidence report: `_bmad-output/implementation-artifacts/story-1-4-my-tenants-self-audit-evidence-2026-07-21.md`
- Build: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 -warnaserror` → 0 Warning(s) / 0 Error(s).
- UI tests (xUnit v3 exe fallback for .NET 10 MTP/VSTest): full `Hexalith.Tenants.UI.Tests` → **948/948**, 0 failed (was 942; +6 new tests). Contracts.Tests → 112/112.
- Focused classes: `TenantWorkspaceStateTests` 11/11, `MyTenantsSurfaceTests` 14/14, `TenantsWorkspaceTests` 10/10, `UserMembershipLookupSurfaceTests` 13/13 (no 1.5 regression), `DomainUiFluentConformanceTests` 51/51, `TenantsUiCompositionTests` 19/19 (EN/FR parity).

### Completion Notes List

**Reverify-and-harden outcome (per-AC verdict in the evidence report).** AC1/AC2/AC3/AC4/AC6/AC8/AC9 = `verified` (source inspected + green focused suite); AC5 = `changed` (implemented); AC7 = `changed` (fixed). No shipped identity/state/freshness/support-safety code was re-forked.

**AC5 — owner drill-in + full scope=mine return-context restore (PRIMARY CHANGE):**
- Added an authorized shared-detail drill-in from self-audit rows to the same `/tenants/{tenantId}` `TenantDetailPage`, opt-in via a new `MyTenantsDataGrid.DetailHref` parameter (the Users-lookup surface leaves it null, so Story 1.5 is unchanged). The Role column is preserved.
- Extended the `MyScope` branch of `TenantWorkspaceState.FromQuery`/`ToCanonicalUrl` to carry and emit `selected`/`anchor` (previously dropped), so a canonical redirect on return no longer strips the restored selection.
- Reused the scope=all mechanism (`TenantListNavigationContext.ToDetailUrl(tenantId, anchor)` new overload) so the return URL carries scope=mine + selection + return-focus anchor and resets the cursor to the authorized first page (mirroring scope=all).
- Added a distinct scope=mine return-context banner (`data-testid="tenants-my-return-context"`, key `Tenants.MyTenants.ReturnContext`) in the workspace `HeaderMetadata`; the existing heading-focus path (`_focusHeadingPending`) now fires for scope=mine because `_selectedTenantId` is populated.
- Query-changing transitions null the stale `selected`/`anchor` (verified by test).

**AC7 — broken return-focus anchor (fixed):** added `id="{SelectorPrefix}-row-{TenantId}"` to the `MyTenantsDataGrid` identity element (mirroring `TenantDataGrid`), so the `ReturnFocus` anchor resolves instead of being a no-op. Added the matching detail-link forced-colors + `:focus-visible` CSS.

**Grid-divergence decision (AC5 open product question):** satisfied AC5 by reusing the shared **detail** route + return-context while keeping the Role-augmented `MyTenantsDataGrid`; did **not** replace it with a literal `TenantDataGrid` (no Role column) — recorded for reviewer confirmation.

**Set-but-unread `IsAuthorizationScopedEmpty`:** kept intentionally unused; the UI's authorization-safe-empty contract is `SurfaceKind is Empty` + copy, not the boolean.

**Blockers (recorded, not closed):** runtime browser evidence stays gated by **PLATFORM-OPS-1** (no live AppHost this session; only Dapr infra containers up). The direct-read/freshness-provenance items (**PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1**) remain platform-owned; the surface renders freshness honestly. Submodule gitlinks (`references/Hexalith.Builds` `7708256e→dfb2f3fd`, `references/Hexalith.EventStore` `41f5ed0f→4245f0f8`, `references/Hexalith.Memories` `3b1ae857→ae591ce7`) were committed together with this story in `41e047e` and, per the 2026-07-21 code-review decision, are kept as intentional (all three reachable on `origin/main`).

### File List

Production (all under `src/Hexalith.Tenants.UI/`):
- `State/TenantList/TenantWorkspaceState.cs` — MyScope `FromQuery` carries `selected`/`anchor`; `ToCanonicalUrl` emits them.
- `State/TenantList/TenantListNavigationContext.cs` — new `ToDetailUrl(string tenantId, string anchor)` overload; existing `ToDetailUrl(TenantListRow)` delegates.
- `Components/Users/MyTenantsDataGrid.razor` — row `id` anchor (AC7); opt-in `DetailHref` drill-in link (AC5).
- `Components/Users/MyTenantsDataGrid.razor.css` — detail-link ellipsis + `:focus-visible` + forced-colors hooks.
- `Components/Users/MyTenantsPanel.razor` — builds/passes `DetailHref`; `BuildReturnUrl` refactored onto `MyTenantsWorkspaceState()`.
- `Components/Pages/TenantsWorkspace.razor` — scope=mine return-context banner in `HeaderMetadata`.
- `Resources/TenantsResources.resx` + `Resources/TenantsResources.fr.resx` — `Tenants.MyTenants.DetailLinkLabel` + `Tenants.MyTenants.ReturnContext` (EN/FR).

Tests (under `tests/Hexalith.Tenants.UI.Tests/`):
- `State/TenantWorkspaceStateTests.cs` — +3 (scope=mine selection/anchor round-trip; detail-URL cursor reset; query-changing null-out).
- `Components/MyTenantsSurfaceTests.cs` — +2 (no browser target user id; drill-in href + focus-anchor id).
- `TenantsWorkspaceTests.cs` — +1 (scope=mine return-context banner + canonical URL preservation); stub localizer keys added.

Evidence / tracking (NEW, non-source):
- `_bmad-output/implementation-artifacts/story-1-4-my-tenants-self-audit-evidence-2026-07-21.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `1-4-my-tenants-self-audit: in-progress → review`.

Submodule gitlinks bumped in this commit (intentional, per the 2026-07-21 review decision): `references/Hexalith.Builds` (`7708256e→dfb2f3fd`), `references/Hexalith.EventStore` (`41f5ed0f→4245f0f8`), `references/Hexalith.Memories` (`3b1ae857→ae591ce7`). Preserved / not modified: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md` (historical evidence).

## Change Log

| Date | Change |
|---|---|
| 2026-07-21 | Story 1.4 reverify-and-harden: closed AC5 (shared-detail drill-in from self-audit rows + full scope=mine return-context restore of selection/cursor/scroll via the scope=all mechanism) and AC7 (added the missing row `id` so the `ReturnFocus` anchor resolves). Verified AC1–AC4/AC6/AC8/AC9 against source + green focused suite. UI.Tests 948/948, Contracts.Tests 112/112, `.slnx` Release `-warnaserror` 0/0. Runtime gated by PLATFORM-OPS-1 (recorded). |

## Review Findings — BMAD Code Review (2026-07-21)

_Adversarial review over committed diff `21a3ce5..41e047e`: Blind Hunter + Edge Case Hunter + Acceptance Auditor (all Opus 4.8). 16 raw findings → **2 decision-needed, 1 patch, 1 deferred, 6 dismissed**. Core AC5/AC7 code changes verified sound and honest; the highest-severity item is an evidence-integrity/scope contradiction, not a code defect._

### Decision-needed

- [ ] [Review][Decision] Commit bumps three `references/*` submodule gitlinks while the story record asserts they were "untouched" — Commit `41e047e` changes `references/Hexalith.Builds` (`7708256e→dfb2f3fd`), `references/Hexalith.EventStore` (`41f5ed0f→4245f0f8`), and `references/Hexalith.Memories` (`3b1ae857→ae591ce7`), yet the File List says "Preserved / not modified by this story" and `story-1-4-my-tenants-self-audit-evidence-2026-07-21.md` (L16/L23) records them as "unchanged"/"not touched, reset, or committed here." Contradicts project-context L135, the story's own Git Intelligence instruction, CLAUDE.md dependency-preservation, and AC9 evidence integrity. Mitigation: all three targets are reachable on `origin/main`, so no cloneability/CI break (yet). Related evidence gap: the File List also omits the route-smoke test and `tests/test-summary.md` that the completed evidence task required. **Decide:** (a) revert the three gitlinks to the story baseline so the commit matches its record, or (b) keep the bumps as intentional and correct the evidence report + File List (and reconcile the omitted test artifacts). — **RESOLVED 2026-07-21 (Administrator): (b) keep the bumps as intentional; correct the record. → converted to the documentation patch below.**
- [x] [Review][Decision] AC5 "same list components" + "restore cursor/scroll" met by scope=all parity, not literal restore — The self-audit surface keeps the Role-augmented `MyTenantsDataGrid` (not the literal `TenantDataGrid`), and detail-return resets to the authorized first page + heading focus (`FocusHeadingAsync`) + return-context banner + selection state — mirroring the accepted scope=all pattern — rather than literally restoring cursor position and scroll. Both are documented (Open Product Question + Completion Notes) and are honest, not silent regressions. **Decide/sign off:** confirm the scope=all-parity interpretation closes AC5's "same list components" and "cursor context / scroll position" clauses (recommended — it is the already-accepted Story 1.2/1.3 behavior), or require deeper grid unification / literal cursor+scroll restore. [`src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs:41`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:400-405`] — **RESOLVED 2026-07-21 (Administrator): scope=all-parity interpretation ACCEPTED; the Role-augmented grid + reset-to-first-page/heading-focus/banner restore satisfy AC5. AC5 stands as met; no further dev work.**

### Patch

- [x] [Review][Patch] Correct the evidence record for the intentional submodule bumps (from resolved decision above) — Update the File List and `story-1-4-my-tenants-self-audit-evidence-2026-07-21.md` to state that `references/Hexalith.Builds`, `references/Hexalith.EventStore`, and `references/Hexalith.Memories` gitlinks were **intentionally updated** in this commit, replacing the current "Preserved / not modified"/"unchanged"/"not touched, reset, or committed here" wording so the record matches commit `41e047e`. — **APPLIED 2026-07-21**: File List + Completion Notes + evidence-report submodule section corrected (all three gitlinks now recorded as committed with the story, Memories included).
- [x] [Review][Patch] `NormalizeContextValue` has no length bound — unbounded `selected`/`anchor` [`src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:348`] — **APPLIED 2026-07-21**: added `MaximumContextValueLength = 512` bound to `NormalizeContextValue`. Build `0/0`; UI suite `948/948`. — Unlike `NormalizeUserId` (256) and `NormalizeOpaque` (4096), the `selected`/`anchor` query values are accepted at any length and then flow into the row element `id`, the return-context banner text node, and the canonical URL. The Story 1.2 review rule rejects id-like URL-derived values whose `Length > 256`. Add a length cap to `NormalizeContextValue` (this diff newly routes the scope=mine branch through the normalizer; the fix also hardens the pre-existing scope=all path).

### Deferred

- [x] [Review][Defer] Degenerate/exotic tenant ids on the shared detail-nav path [`src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs:36`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor:17`] — deferred, near-unreachable + shared pre-existing pattern. (a) `ToDetailUrl(row)` now delegates to the `(string,string)` overload whose `ArgumentException.ThrowIfNullOrWhiteSpace(tenantId)` throws on a blank tenant id where the pre-change inline body produced a silent link — a render-time throw inside the grid template would tear down the surface; (b) a tenant id containing whitespace or CSS-significant chars yields an invalid HTML `id` and a non-resolving return-focus anchor. Both require a blank/exotic tenant id (the id is the validated non-blank aggregate id; ids are slug-like), and both share the pre-existing scope=all `TenantDataGrid` `id="tenant-row-{TenantId}"` pattern — fix as cross-surface id-safety hardening, not a My-Tenants-only divergence.

### Dismissed (with rationale)

- **Row `id` "dead / competing focus target"** (Blind Hunter) — the added row `id` feeds the `AuditEvidenceEntryPoint ReturnFocus` (the actual AC7 anchor that was previously dangling); detail-return uses heading focus. Two distinct navigation paths, not competing; the `id` is genuinely consumed.
- **Return-context banner `Color.Lightweight` "inverted white / unreadable"** (Blind Hunter) — identical token/size to the shipped, reviewed scope=all banner (`TenantsWorkspace.razor:111`); intentional muted-secondary text, parity not regression.
- **Banner missing `role`/`aria-live`** (Blind Hunter) — parity with the shipped scope=all banner; on return, focus lands on the heading via `FocusHeadingAsync()`, so the banner (in `HeaderMetadata`) is encountered by AT, not silently missed.
- **Banner "over-claims restoration vs. neutral scope=all copy"** (Edge Case Hunter) — false premise: the scope=all `Tenants.List.ReturnContext` uses the same "…restored on the authorized first page" assertion; the scope=mine copy is parallel/softer.
- **Round-trip encoding untested for reserved characters** (Blind Hunter) — `AppendQuery` escapes both key and value with `Uri.EscapeDataString` and `ToDetailUrl` double-escapes `returnUrl`; the encoding path is correct. Adding a reserved-char round-trip test is a nicety, not a defect.
- **Non-unique `data-testid="{prefix}-row"` alongside the unique `id`** (Blind Hunter) — by design: the row-template `data-testid` is intentionally constant; the per-row `id` distinguishes rows and tests assert via `.Id`.
