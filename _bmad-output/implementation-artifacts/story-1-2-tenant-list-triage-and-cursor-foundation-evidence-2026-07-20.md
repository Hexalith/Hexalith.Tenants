# Story 1.2 Tenant List Triage and Cursor Foundation Evidence

Date: 2026-07-20

## Evidence Scope

This report reverifies the corrected Story 1.2 contract against the live repository. The historical
`1-2-tenant-list-triage.md` and its 2026-06-06 review remain unchanged. Historical completion and the
Story 1.1 green suite are baseline evidence, not acceptance waivers.

## Immutable Baseline

| Item | Recorded value |
| --- | --- |
| Story baseline commit (preserved) | `088232a7255698e20105594d9e0ef12a0f09c73e` |
| Live root commit before implementation | `23943db4c709dc2a7ba6dcbb7e7f4748d75ab4db` |
| Root branch / remote | `main`, synchronized with `origin/main` |
| FrontComposer source | `550cb0602d506d9fd008a8c09f2cca6b328ec1e3` (`v4.0.1-84-g550cb060`) |
| Builds source | `ed7cea8e1f943b4c47a454a0e8f462f0fae9891d` (`v4.21.7-13-ged7cea8`) |
| Declared EventStore package baseline | `3.77.2` |
| Declared FrontComposer package baseline | `4.0.1` |
| Declared Memories package baseline | `2.14.0` |
| Declared Tenants package baseline | `3.2.18` |
| Declared and resolved Fluent UI Blazor | `5.0.0-rc.4-26180.1` |
| Resolved EventStore Client / Contracts | `3.77.2` / `3.77.2` |
| Resolved Memories Client.Rest / Contracts | `2.14.0` / `2.14.0` |
| Test framework pins | xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, NSubstitute `6.0.0`, Microsoft.NET.Test.Sdk `18.8.1` |

The initial `git status --short --branch` result was clean (`## main...origin/main`). There were no
pre-existing worktree changes to preserve. The workflow subsequently changed only sprint tracking
before this evidence report was created. Story 1.1 state, canonical URL transitions, sort propagation,
resources, tests, and submodule contents were left intact.

## Pre-implementation Commands

| Command | Exit | Result |
| --- | ---: | --- |
| `git status --short --branch` | 0 | Clean, synchronized `main` |
| `git remote -v` | 0 | `origin` = `https://github.com/Hexalith/Hexalith.Tenants.git` |
| `git log --oneline --decorate --max-count=8` | 0 | Live head `23943db` |
| `git submodule status -- references/Hexalith.FrontComposer references/Hexalith.Builds` | 0 | FrontComposer `550cb060`; Builds `ed7cea8` |
| `dotnet list src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj package --include-transitive` | 0 | Declared/resolved packages matched the table above |
| `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 -warnaserror` | 0 | Build succeeded, 0 warnings, 0 errors |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build` | 0 | 933 passed, 0 failed, 0 skipped |

## Initial Acceptance-Criteria Inventory

| AC | Initial classification | Live evidence and required correction |
| ---: | --- | --- |
| 1 | changed | Full-width grid and stable row key exist, but freshness is not pinned and status/freshness icons are Size16. |
| 2 | changed | Ordinary list cursor is opaque pass-through, but detail/audit return links embed it in DOM attributes and Memories search creates plaintext `memories-search:{offset}` cursors. |
| 3 | changed | Story 1.1 resets canonical cursor for filter/sort/scope changes, but page size is fixed at 20 and ordinary invalid-cursor recovery is a generic error. |
| 4 | changed | Six states exist, but gateway prose can bypass localization and non-blocking refreshed/search notices do not exist. |
| 5 | verified | `TenantId` is the `ItemKey`; rows own pending/freshness values. Semantic styling and regression evidence still need strengthening. |
| 6 | verified | Gateway freshness resolution requires projection-backed provenance and does not infer current from `ServedAt`, ETag, or a bare 304. Unsupported `Aging` remains renderable and must be prevented on this list surface. |
| 7 | changed | Search currently uses Memories and a plaintext offset cursor; corrected Story 1.2 requires the ordinary list plus a localized search-unavailable notice until Story 1.9. |
| 8 | changed | Horizontal overflow, forced-colors CSS, and stable selectors exist; the third safety column, Size20/icon-label semantics, and focused runtime evidence remain incomplete. |
| 9 | blocked | No current warm authenticated reference-environment measurement was attached at baseline. Runtime evidence must use discovered Aspire endpoints or record the exact platform prerequisite. |
| 10 | changed | Baseline UI regression is green, but corrected invalid-cursor, page-size, notice, cursor-disclosure, pinning, and badge tests do not yet exist. |

## Historical Review Follow-up Reconciliation

| Historical finding | Live disposition before correction |
| --- | --- |
| Authenticated operator propagation | Fixed later: OIDC/FrontComposer server security relays gateway authorization, and `TenantQueryGateway` fails closed through `IUserContextAccessor`. Reverification tests remain required. |
| Localized gateway reasons | Still open: `TenantListSnapshot.ErrorMessage` carries raw English gateway/search prose into `ListSurfaceStates`. |
| Page-local search/filter/sort claims | Still open and partially regressed: the current component claims whole-set Memories search and the gateway emits plaintext offset cursors. Status and name sorting remain current-page presentation over the server's tenant-id cursor order. |
| Renderer-context awaits | Still open: `TenantsWorkspace.razor` uses `ConfigureAwait(false)` in lifecycle and event methods. |
| Dead resources/styles | Partially open: obsolete `Tenants.Workspace.Status*` and `Unavailable*` resources remain; obsolete workspace CSS selectors no longer exist. |

## External Gates and Conservative Claims

- `SEARCH-CURSOR-1` remains unverified; Story 1.2 must not use or claim protected whole-set search.
- `PLAT-FRESH-1`, `HOST-REF-1`, and `UI-READ-1` remain external to this story; unqualified freshness stays `unknown`.
- Browser, assistive-technology, and warm-load evidence is pending the runtime lane. If the checked-in
  principal or shared harness cannot run, the final record will name `PLATFORM-OPS-1`, the exact command,
  owner, consequence, and reopen trigger.

## Final Evidence

### Implemented Corrections

- Ordinary list invalid cursors recognize only the safe `invalid-cursor` reason, retry exactly once with
  a null cursor and ETag, and return a typed localized page-one recovery notice. Retry failures are
  authorization-safe and disclose no cursor.
- Page size is a circuit-local Fluent control with 20/50/100 choices and a default of 20. Page size,
  status, sort field/direction, scope, and reset transitions clear the current cursor and history.
- Detail/audit return URLs preserve canonical filter, sort, selection, and focus context without embedding
  the current paging cursor. The former plaintext Memories offset cursor path is no longer reachable;
  unverified whole-set search uses the ordinary authorized list plus a typed search-unavailable notice.
- Tenant-list failures and degraded results now use `TenantListReason` rather than gateway prose. Last
  confirmed rows are kept separately during refresh, missing evidence remains unknown, and no member or
  owner count is fabricated.
- Identity, status, and freshness columns use rc.4 `DataGridColumnPin.Start`, stable widths, and literal
  `TenantId` row identity. Status, pending, and freshness badges use visible localized text, `IconLabel`,
  Size20 icons, and the locked semantic mappings. Resting row badges are not repeated live regions.
- Error/degraded states are assertive; routine states and non-blocking notices are polite. EN/FR copy now
  discloses that status filtering and column sorting are current-page behavior. Renderer lifecycle/event
  awaits stay on the Blazor dispatcher, and proven-dead select-sort resource entries were removed.

### Focused and Regression Commands

| Command | Exit | Result |
| --- | ---: | --- |
| `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 -warnaserror --no-restore` | 0 | 0 warnings, 0 errors |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore` | 0 | 942 passed, 0 failed, 0 skipped |
| UI test executable, class `TenantListSurfaceTests` | 0 | 43 passed |
| UI test executable, class `TenantQueryGatewayTests` | 0 | 117 passed |
| UI test executable, class `TenantWorkspaceStateTests` | 0 | 8 passed |
| UI test executable, class `TenantsWorkspaceTests` | 0 | 9 passed |
| UI test executable, class `TenantsUiCompositionTests` | 0 | 19 passed |
| `dotnet test ...Contracts.Tests... --configuration Release --no-build --no-restore` | 0 | 112 passed |
| `dotnet test ...Client.Tests... --configuration Release --no-build --no-restore` | 0 | 50 passed |
| `dotnet test ...Server.Tests... --configuration Release --no-build --no-restore` | 0 | 738 passed |
| `dotnet test ...Testing.Tests... --configuration Release --no-build --no-restore` | 0 | 181 passed |
| `dotnet test ...Sample.Tests... --configuration Release --no-build --no-restore` | 0 | 39 passed |

The focused tests cover the complete reset matrix, page-size normalization, cursor pass-through and
absence from row surfaces, exactly-one invalid-cursor retry, safe retry failure, typed reasons, missing and
explicit freshness provenance, three pins, stable row/action footprints, Size20 badge mappings, all six
states and recovery actions, EN/FR parity, logical direction, responsive/forced-colors CSS, dispatcher
awaits, and avoidance of incidental Fluent selectors.

### Authenticated Browser and Aspire Lane

The AppHost was run through the Aspire CLI, and endpoints were read from live Aspire state rather than
assumed. The Tenants UI was exercised at its discovered HTTP endpoint because the development HTTPS
certificate was untrusted by the browser. The checked-in development administrator authenticated through
Keycloak; credentials and tokens were never printed or recorded.

Verified browser evidence:

- authenticated shell and fail-closed typed list error;
- English and French whole-string rendering (`html lang="fr"` in the French lane);
- stable current-page disclosure and localized status-filter accessible name;
- keyboard traversal through tabs, scope, search, status, page size, refresh, reset, and create accordion;
- viewport widths 320, 768, 1024, and 1440 with no page-level horizontal overflow;
- forced-colors active and reduced-motion reduce media modes;
- error state `role="alert"` with `aria-live="assertive"`;
- zero console errors after the topology stabilized and the console was cleared before the final reload.

`UseAppHost=true` Debug builds were used only as build artifacts to start repository projects whose normal
Aspire `--no-build` launch expected apphost executables. No source or configuration was changed for that
workaround. An ephemeral tenant command was accepted and reached `Completed` with one event, proving the
command/projection service was operating.

### PLATFORM-OPS-1 Blocker

Actual grid rendering, horizontal scrolling/pinning, row keyboard relationships, browser invalid-cursor
recovery, and valid warm-list performance measurement remain blocked. Three topology recovery attempts
produced the same platform condition:

1. start the AppHost, authenticate, and query the list;
2. build/start the Tenants service, complete an ephemeral tenant command, then restart EventStore;
3. build/start the Sample and Memories services, then restart EventStore again.

On each EventStore startup, `AdminOperationalIndexHostedService` logged that domain metadata was unavailable
and skipped operational-index writes. The authenticated `list-tenants` request consequently routed to the
generic projection actor, which reported no projection state for the tenant index and returned 404. Direct
metadata invocation succeeded after startup, demonstrating a startup/index-publication timing or lifecycle
gap rather than a missing checked-in principal. Owner: EventStore/AppHost platform operations. Reopen trigger:
the supported AppHost startup path reliably publishes the Tenants query-type index before the first list
request. Until then, no honest tenant-set size or approximately-one-second warm-list timing can be reported.

### Additional Regression Audit

The story-required full configured UI regression is green. A broader optional repository audit found a
separate existing integration-host failure outside the UI change set: the isolated
`Commands_endpoint_accepts_CreateTenant_and_routes_story_payload` test returns HTTP 500 instead of 202 in
both Debug and Release. The full integration-project run showed the same systemic 500 and was stopped after
the representative test reproduced independently. This story changes no backend or integration-test source,
but the failure is recorded because it prevents claiming the workflow's broader all-tests completion gate.

### Final Gate Decision

| AC | Decision | Evidence |
| ---: | --- | --- |
| 1 | changed, runtime-blocked | Three pins, widths, row identity, and action footprint pass component tests; actual grid is blocked by `PLATFORM-OPS-1`. |
| 2 | verified | Opaque pass-through, no offset conversion, cursor-free row surfaces, and safe failures are covered. |
| 3 | verified, browser-blocked | Reset/retry/recovery tests pass; browser invalid-cursor recovery awaits the platform list route. |
| 4 | verified | Six state selectors, recovery actions, typed copy, and assertive/polite semantics pass; EN/FR error state was browser-verified. |
| 5 | verified, runtime-blocked | Stable `TenantId` association and marker semantics pass; actual horizontal scroll awaits the platform list route. |
| 6 | verified | Missing/non-projection evidence, `ServedAt`, ETag, request time, and bare 304 stay unknown. |
| 7 | verified | Ordinary-list fallback and localized non-blocking notice replace the plaintext search cursor. |
| 8 | changed, runtime-blocked | Shell breakpoints, media modes, keyboard toolbar, CSS, and badge semantics pass; grid relationships remain blocked. |
| 9 | blocked | No valid warm tenant-list interaction can be measured while the platform list route returns 404. |
| 10 | verified | Required Release build, 942-test UI regression, and focused suites are green with exact commands recorded. |

The implementation is not promoted to `review`: Story 1.2 remains `in-progress` until `PLATFORM-OPS-1`
unblocks the grid/performance claims and the broader integration-host regression gate is resolved or formally
baselined by the owning team.
