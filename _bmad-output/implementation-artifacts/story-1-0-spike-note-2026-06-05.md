---
title: 'Story 1.0 Spike Note — FrontComposer Shell-integration verification & FC-LYT/FC-CMD contract confirmation'
story: '1.0 (enabler spike)'
date: '2026-06-05'
author: 'Tenants UI / Dev'
status: 'COMPLETE'
outcome: 'GO (with one scoped adjustment for Story 1.3)'
verified_against:
  - 'Hexalith.FrontComposer submodule (Shell source) @ working tree 2026-06-05'
  - 'Fluent UI pin 5.0.0-rc.3-26138.1'
related:
  - '_bmad-output/planning-artifacts/epics.md (Story 1.0 / 1.1)'
  - '_bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md (the asks)'
  - '_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md'
  - 'docs/tenants-ui-frontcomposer-dependency-map.md'
---

# Story 1.0 Spike — FrontComposer Shell-integration verification

> **Type:** time-boxed enabler spike. Deliverable = this written finding + a go/adjust decision (no production code).
> **Scope of evidence:** all paths below are inside the `Hexalith.FrontComposer` submodule unless noted. Paths are relative to `references/Hexalith.FrontComposer/`.

## TL;DR

**GO for Story 1.1 (host + shell bootstrap).** Every Shell-integration API the bootstrap needs is verified against source, and **FC-LYT — the single remaining build-start gate per the 2026-06-03 readiness report — is CONFIRMED by an explicit contract**, not a "use-as-is" concession. Fluxor, JWT/auth registration, manifest registration, and projection routing are all real and citable. FC-A11Y / FC-L10N / FC-DOC are confirmed ready.

**One scoped ADJUST, and it does not block Story 1.1:** the FrontComposer source-generated projection **DataGrid (FC-TBL) does NOT provide cursor pagination, column pinning, or the 6 non-collapsing list states** the Tenants read surfaces require. This is a **Story 1.3 (tenant list)** design decision, not a bootstrap blocker — resolve it before Epic 1 read surfaces (recommendation in §6).

**Bonus:** the command-lifecycle contract (FC-CMD) and concurrency policy (FC-CNC) — needed for Epic 3+ — are also confirmed, and the GROUP 2 open questions (key shape, uniqueness scope, ownership) are answered by source. Epics 3–5 lose their "unconfirmed contract" caveat ahead of schedule.

---

## 1. Shell-integration APIs (Story 1.0 / readiness GROUP 6) — VERIFIED

**Registration surface** (`src/Hexalith.FrontComposer.Shell/Extensions/`, namespace `Hexalith.FrontComposer.Shell.Extensions`; all extend `IServiceCollection`):

| Method | File:line | Role |
|--------|-----------|------|
| `AddHexalithFrontComposer(Action<FluxorOptions>?)` | `ServiceCollectionExtensions.cs:170` | **Foundational.** Registers Fluxor store (scans Shell assembly), `IFrontComposerRegistry`, authorization core, lifecycle/projection registries, pending-commands, data-grid, ETag cache + ~40 services. Registers the bootstrap validation hosted gate. |
| `AddHexalithFrontComposerQuickstart(...)` | `ServiceCollectionExtensions.cs:493` | Sugar: localization + `AddHexalithShellLocalization()` + `AddHexalithFrontComposer()` in one call. |
| `AddHexalithDomain<T>()` | `ServiceCollectionExtensions.cs:72` | Auto-discovers a generated `{Domain}Registration` (static `Manifest` + `RegisterDomain(IFrontComposerRegistry)`) by reflection. |
| `AddHexalithEventStore(Action<EventStoreOptions>?)` | `EventStoreServiceExtensions.cs:33` | Swaps stub command/query/subscription clients for EventStore-backed ones. |
| `AddHexalithFrontComposerAuthentication(...)` | `FrontComposerAuthenticationServiceExtensions.cs:33` | Auth wiring (pairs with Tenants JWT bearer / Keycloak-or-symmetric). |
| `AddHexalithShellLocalization(...)` | `ServiceCollectionExtensions.cs:454` | Resx/culture conventions (consumer still calls `AddLocalization()`). |
| `AddHexalithProjectionTemplates<TMarker>()` / `(IReadOnlyList<...>)` | `ServiceCollectionExtensions.cs:537` / `:560` | Level-2 projection template registration. |

**Call ORDER is enforced, fail-fast.** Required order is **FrontComposer → Domain → EventStore**. Each stage drops an immutable DI marker (`QuickstartBootstrapMarker` / `DomainBootstrapMarker` / `EventStoreBootstrapMarker`); a hosted `FrontComposerBootstrapValidationGate` validates at `StartAsync` and throws a named-fix `InvalidOperationException` before first render if the foundational call is missing or stages are mis-ordered (`Extensions/FrontComposerBootstrapValidator.cs`, `Extensions/FrontComposerBootstrapValidationGate.cs`, `Extensions/FrontComposerBootstrapMarkers.cs`). All registrations are idempotent (`TryAdd*`).

**Manifest registration** — `DomainManifest` record (`Contracts/Registration/DomainManifest.cs`): `(Name, BoundedContext, IReadOnlyList<string> Projections, IReadOnlyList<string> Commands, IReadOnlyDictionary<string,string>? CommandPolicies)`. Registered either via the auto-discovered `{Domain}Registration` class or manually through `IFrontComposerRegistry.RegisterDomain(...)` (`Contracts/Registration/IFrontComposerRegistry.cs`; default impl `Shell/Registration/FrontComposerRegistry.cs`). Companion opt-in registries exist for full-page routes, command write-access, and command policies.

**Projection / page routing** — standard Blazor component routing (no custom middleware/`MapXxx` beyond the host's `MapRazorComponents`). Commands route to `/domain/{kebab-context}/{kebab-command}` (`Shell/Routing/CommandRouteBuilder.cs`). Projection views resolve through `IProjectionTemplateRegistry.Resolve(projectionType, role)` (`Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`); home route `/`,`/home` (`Shell/Components/Pages/FcHomeRouteView.razor`) is overridable by a later-scanned `@page "/"`.

**Fluxor — CONFIRMED.** `Fluxor.Blazor.Web`; `AddFluxor(o => o.ScanAssemblies(...))` inside `AddHexalithFrontComposer`; `Feature<T>` states, `[ReducerMethod]` reducers, and `Effect` classes throughout `Shell/State/*`. Consistent with Tenants D1 (InteractiveServer + Fluxor TruthState).

**Integration gotchas to carry into Story 1.1:**
- **Consumer must implement `IUserContextAccessor` / `IFrontComposerTenantContextAccessor`** — defaults are `Null*` stubs. This is where the Tenants BFF supplies actor identity (JWT `sub`) and tenant scope (`eventstore:tenant=system`). Wire it in the bootstrap.
- **SignalR for projection nudges** needs host configuration (Shell registers the services, not the endpoint wiring).
- **`IStorageService` is Scoped** (per FrontComposer ADR-030) — never capture it in a singleton.

---

## 2. FC-LYT (build-start gate) — **CONFIRMED**

The open question was: *"full-width vs constrained variant contract, or use the shell layout as-is?"* The answer from source is **a real variant contract exists** (better than the "use-as-is" floor the readiness request was prepared to accept):

- `FcPageLayoutMode` enum — `Contracts/Rendering/FcPageLayoutMode.cs`. Its own XML doc reads **"Page-layout measure … (FC-LYT contract, Story 1.2)."** Values: `FullWidth` (zero/default, edge-to-edge — right for dense DataGrids/audit) and `Constrained` (capped at `--fc-page-max-inline-size`, default 75rem, centred — for prose/forms/detail).
- A page opts in via `<FcPageLayout Mode="FcPageLayoutMode.Constrained">` (`Shell/Components/Layout/FcPageLayout.razor`), cascaded through `FcPageLayoutCoordinator` and applied by `FrontComposerShell` on `#fc-main-content` (CSS `.fc-page-layout--constrained`). Disposal resets to `FullWidth` (zero-regression).

**This satisfies UX-DR6** (full-width operational surfaces + constrained readable inner regions) directly — no Tenants-side CSS hack needed for the basic constraint. **Gate FC-LYT → clears.** Story 1.1's `Gate:` line can cite `Contracts/Rendering/FcPageLayoutMode.cs` as the confirmation evidence.

---

## 3. FC-CMD + FC-CNC (Epic 3+ command contract) — **CONFIRMED & Tenants-compatible**

Verified in `Shell/State/PendingCommands/`, `Shell/Services/Lifecycle/`, `Contracts/Lifecycle/`, `Shell/Infrastructure/EventStore/`. Answers the readiness GROUP 2 open questions:

- **(a) Identity / key shape** — `CorrelationId` is a caller-supplied **string** (D1, `Contracts/Lifecycle/CommandLifecycleTransition.cs`); `MessageId` is a **26-char ULID**, validated/normalized by `PendingCommandStateService.TryNormalizeUlid` (Crockford base-32, case-normalized). The "26 chars" the readiness note flagged as *not approved as reusable* **is a framework-standard reusable utility** — answered.
- **(b) Uniqueness scope** — **per-circuit** (Blazor Server) / per-user (WASM), keyed by `MessageId` (`Dictionary<string,…>(StringComparer.Ordinal)` in `PendingCommandStateService`; `IPendingCommandStateService` doc: "Circuit-local, bounded pending-command index keyed by … ULID MessageId"). Open question **(b) answered: per-circuit.**
- **(c) Ownership / persistence** — `PendingCommandStateService` + `LifecycleStateService` are **Scoped**; v0.1 pending state is **circuit-local and lost on browser refresh** (durable lookup deferred to FrontComposer's own Story 5-4). Matches Tenants NFR-3 (reconnect re-derives truth from the projection; never resurrect optimistic success).
- **Lifecycle states** — `CommandLifecycleState { Idle, Submitting, Acknowledged, Syncing, Confirmed, Rejected }`; pending layer adds `IdempotentConfirmed` + `NeedsReview` (`Contracts/Lifecycle/CommandLifecycleState.cs`, `Shell/State/PendingCommands/PendingCommandModels.cs`). Maps onto the Tenants 10-token lifecycle vocabulary (Story 1.2).
- **alreadyApplied** — explicit `IdempotentConfirmed` outcome with an `idempotencyResolved` flag that suppresses false "success" celebration. Matches the Tenants NoOp `already applied` rule.
- **Confirmation = re-query authoritative; SignalR = nudge only** — `EventStorePendingCommandStatusQuery` GETs `/api/v1/commands/status/{messageId}`; sources enumerated as `LiveNudgeRefresh` / `ReconnectReconciliation` / `FallbackPolling` / `IdempotencyStatusQuery`. **Exactly architecture D2.**
- **Scope-flush** — `EnforceScopeBoundary()` clears pending state on tenant/user transition, fail-closed.
- **FC-CNC (concurrency)** — `CommandExecutionAdmissionGate` implements **one-at-a-time** admission; bounded cap `FcShellOptions.MaxPendingCommandEntries` (default 100, overflow → `NeedsReview`); duplicate `MessageId` de-dupes. **The approved fallback IS the shipped policy** — "confirm you own it" is satisfied.

> Net effect: when Tenants reaches Epic 3, the command-lifecycle plumbing is a confirmed reusable contract. The remaining GROUP 4 **numeric budgets** (confirming→degraded threshold, polling budget, retry budget) are still open and owned jointly by Product/UX + FrontComposer + EventStore — they don't block read-only Epics 1–2.

---

## 4. FC-TBL (read-surface grid) — **AVAILABLE, with 3 caveats that land on Story 1.3**

The grid renders (source-generated projection views over `FluentDataGrid<T>`), and **Fluent version is an EXACT match (`5.0.0-rc.3-26138.1`, `Directory.Packages.props`)**. But three Tenants read-surface requirements are **not** met by the source-generated projection grid:

| Tenants requirement | FrontComposer projection grid | Evidence |
|---------------------|-------------------------------|----------|
| **Cursor pagination (never offset/limit)** — FR-1, NFR-1, D9 | **Not supported** — offset/virtualization only (`GridItemsProviderRequest` StartIndex/Count); no `ContinuationToken`/cursor field | grep for `continuationtoken\|cursor` in `Components/DataGrid` + `Contracts/Rendering` = empty |
| **Column pinning** (`DataGridColumnPin.Start` for safety-critical cols) — UX-DR8 | **Not supported** — only hide/show prioritization (`FcColumnPrioritizer`, >15 cols) | grep for `columnpin\|DataGridColumnPin\|pinnedcolumn` in `Components/DataGrid` = empty |
| **Six non-collapsing list states** (loading/empty/filtered-empty/error/stale/degraded) — UX-DR9 | Provides **3** (loading / empty / loaded); no error/stale/filtered-empty/degraded | `SourceTools/Emitters/RazorEmitter.cs` lifecycle (Loading>Empty>Loaded) |

**Why this is an ADJUST, not a blocker:** Tenants' own UX already specifies a **custom `TenantDataGrid` component composing Fluent v5 primitives** (UX-DR1 item 6), not necessarily FrontComposer's source-generated projection grid. Fluent v5 `FluentDataGrid` *does* support column pinning and a custom `GridItemsProvider`, and the Tenants BFF already holds opaque cursors as server-side pass-through (D9). So the realistic paths are:

- **(Recommended) Compose `FluentDataGrid` directly** in a Tenants `TenantDataGrid` + `ListSurfaceStates` (Story 1.3), wiring cursor→items via the BFF gateway and pinning via Fluent's own column-pin API. Keeps the safety-critical contract in Tenants where the UX spec already puts it.
- **(Alternative) Push cursor-paging + pinning + the 6 states into FrontComposer** as shared grid capability (aligns with the repo domain-boundary policy that generic UI capability belongs in FrontComposer). Heavier; only worth it if other domains need the same. Track as a FrontComposer enhancement either way.

**Decision owner:** Tenants UI + FrontComposer maintainers, **before Story 1.3** (not before Story 1.1).

---

## 5. FC-A11Y / FC-L10N / FC-DOC (Epic 1–2 ready-gate) — **CONFIRMED**

- **FC-A11Y** — Shell PROVIDES skip links (`#fc-main-content`/`#fc-nav`), `:focus-visible` rings, `aria-live` status regions, `prefers-reduced-motion` fallbacks, and global keyboard routing (`wwwroot/js/fc-keyboard.js`, `fc-focus.js`). Build-time diagnostics **HFC1050–HFC1055** enforce consumer a11y compliance (missing accessible name, keyboard reachability, focus suppression, aria-live parity, reduced-motion, forced-colors) — promoted to build-breakers under `TreatWarningsAsErrors`. **Consumer-supplied per screen:** accessible names on custom controls, and **focus-trap for any custom modal/preview** (FrontComposer's own full focus-trap is deferred to its Story 10-2). → Carry the focus-trap obligation into Story 4.1 (ConsequencePreview/DestructiveControl).
- **FC-L10N** — Documented boundary: `Resources/FcShellResources.resx` (+`.fr.resx`, EN↔FR parity test) owns shell chrome (nav, theme, palette, density, lifecycle labels, datagrid chrome, auth messages). **Consumer owns** projection/field labels via `[Display(Name=…)]` + own `IStringLocalizer<T>`. `IStringLocalizer<FcShellResources>` + `AddHexalithShellLocalization()` provided. Matches Tenants D4.
- **FC-DOC** — Reference docs exist: `docs/reference/components/{front-composer-shell,navigation,datagrid}.md`, `docs/skills/frontcomposer/domain/projections.md`, `docs/how-to/test-generated-components.md`, and a `docs/accessibility-verification/` evidence framework. **Storybook does NOT exist** (readiness "unverified" → confirmed absent); public component API is instead frozen by parameter-surface snapshot tests.

---

## 6. Recommendation — GO with one scoped adjustment

1. **Proceed to Story 1.1 (bootstrap) now.** All integration APIs verified; FC-LYT confirmed; no open blocker for host + shell + JWT/BFF + Fluxor TruthState scaffold.
2. **In Story 1.1, wire `IUserContextAccessor`/tenant-context, SignalR nudge endpoint, and respect Scoped `IStorageService`** — these are the concrete integration gotchas.
3. **Before Story 1.3, take the FC-TBL grid decision (§4)** — recommended: Tenants `TenantDataGrid` composing `FluentDataGrid` primitives (cursor via BFF, Fluent column-pin, 6-state `ListSurfaceStates`). File the cursor-pagination/pinning/6-states need as a FrontComposer fast-follow enhancement regardless, so the contract is recorded.
4. **Carry the custom-modal focus-trap obligation (FC-A11Y) into Story 4.1.**
5. **GROUP 4 numeric budgets remain open** but are only needed for command phases (Epic 3+), not the read-only MVP.

---

## 7. Gate-clearing evidence map

| Gate | Verdict | Citable evidence (under `references/Hexalith.FrontComposer/`) |
|------|---------|----------------------------------------------------|
| **FC-LYT** (build-start) | ✅ **Confirmed** — explicit variant contract | `src/Hexalith.FrontComposer.Contracts/Rendering/FcPageLayoutMode.cs`; `Shell/Components/Layout/FcPageLayout.razor` |
| **Shell-integration spike** (GROUP 6) | ✅ **Done** — APIs verified | `Shell/Extensions/ServiceCollectionExtensions.cs:72,170,454,493,537`; `EventStoreServiceExtensions.cs:33`; `FrontComposerAuthenticationServiceExtensions.cs:33`; `Shell/Registration/FrontComposerRegistry.cs`; `Shell/Routing/CommandRouteBuilder.cs` |
| **FC-TBL** | ⚠️ **Available w/ caveats** — no cursor paging / pinning / 6 states | `Shell/Components/DataGrid/*`; `Contracts/Rendering/DataGridNavigationActions.cs`; `Directory.Packages.props` (Fluent `5.0.0-rc.3-26138.1`, exact match) |
| **FC-CMD** | ✅ **Confirmed** reusable & Tenants-compatible | `Contracts/Lifecycle/CommandLifecycleState.cs`; `Shell/State/PendingCommands/PendingCommandStateService.cs`; `Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs` |
| **FC-CNC** | ✅ **Confirmed** — one-at-a-time is the shipped policy | `Shell/State/PendingCommands/CommandExecutionAdmissionGate.cs`; `Contracts/FcShellOptions.cs` (`MaxPendingCommandEntries`) |
| **FC-A11Y** | ✅ **Confirmed** (consumer supplies per-screen + custom-modal focus-trap) | `wwwroot/js/fc-keyboard.js`, `fc-focus.js`; diagnostics HFC1050–HFC1055; `docs/accessibility-verification/` |
| **FC-L10N** | ✅ **Confirmed** boundary | `Shell/Resources/FcShellResources.resx` (+`.fr.resx`); `Shell/Extensions/ServiceCollectionExtensions.cs:454` |
| **FC-DOC** | ✅ **Confirmed** (no Storybook) | `docs/reference/components/*.md`; `docs/skills/frontcomposer/domain/projections.md`; `docs/how-to/test-generated-components.md` |

## 8. Open follow-ups (tracked, none block Story 1.1)

- **F1 — FC-TBL grid decision** (Tenants UI + FrontComposer): cursor pagination + column pinning + 6 list states. Resolve before Story 1.3.
- **F2 — GROUP 4 numeric budgets** (Product/UX + FrontComposer + EventStore): confirming→degraded threshold, polling budget, retry budget. Before Epic 3 ships.
- **F3 — Durable pending-command state** (FrontComposer Story 5-4): only matters when Tenants wants cross-refresh command continuity; v1 circuit-local is acceptable.
- **F4 — Custom-modal focus-trap** (Tenants Story 4.1): Shell does not provide a generic focus-trap for consumer modals.
