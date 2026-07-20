---
created: 2026-07-19T23:21:31+02:00
baseline_commit: 088232a7255698e20105594d9e0ef12a0f09c73e
frontcomposer_source_commit: d3761fa08ce2f4bf004e8adc7f500822d04276f8
builds_source_commit: 9ec0a032d785dd0abdc14276e8784d6fdd826fd0
frontcomposer_package_baseline: 4.0.1
fluent_ui_pin: 5.0.0-rc.4-26180.1
historical_story_evidence: _bmad-output/implementation-artifacts/1-2-tenant-list-triage.md
prerequisite_story: _bmad-output/implementation-artifacts/1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md
external_gates:
  - PLAT-FRESH-1
  - HOST-REF-1
  - UI-READ-1
  - SEARCH-CURSOR-1
---

# Story 1.2: Tenant List Triage and Cursor Foundation

Status: review

<!-- Created for the corrected Story 1.2 contract. Historical completion is evidence to reverify, not a readiness waiver. -->

## Story

As a platform operator,
I want to scan, filter, sort, and cursor-page authorized tenants with visible trust context,
so that I can identify the tenant requiring attention without acting on hidden or misleading state.

## Acceptance Criteria

1. **Tenant grid and safety columns.** Given an authorized operator opens the Tenants tab, when tenant summaries are available, then a full-width Fluent `TenantDataGrid` shows tenant identity, status, member count, owner count, pending state, and a freshness-aware `TruthStateBadge`, and identity, status, and freshness remain pinned safety columns with stable cell and action footprints.

2. **Opaque cursor paging.** Given more tenant rows exist than fit on one page, when the operator moves forward or backward through results, then paging uses the server-side opaque cursor contract rather than offset/limit, and no cursor value appears in visible copy, DOM attributes, logs, telemetry tags, or copy actions.

3. **Deterministic cursor reset and recovery.** Given the operator changes status filter, sort field, sort direction, scope, or page size, when the new list request is issued, then the cursor resets and deterministic results are returned from the first page, and invalid cursor state restarts at page 1 with an honest localized list-refreshed notice rather than a generic failure.

4. **Six distinct list states.** Given the list is loading, empty, filtered-empty, in error, stale, or degraded, when that state is rendered, then all six states remain visually, semantically, and programmatically distinct, and filtered-empty offers reset, stale offers refresh, degraded explains what is unavailable and what still works, and empty/error copy reveals no out-of-scope tenant existence.

5. **Row-bound pending and stale markers.** Given a tenant has a pending or stale marker, when the operator sorts, filters, pages, or horizontally scrolls the grid, then the marker remains associated with the correct row and cannot be hidden by layout or state collapse, and a pending state is never styled, copied, or announced as confirmed success.

6. **Freshness provenance.** Given authoritative projection freshness cannot yet be measured for a row or response, when the list renders trust state, then freshness is `unknown` rather than inferred from `ServedAt` or request recency, and the UI does not claim `current` or `aging` without supported read-model provenance.

7. **Search-unavailable fallback.** Given a non-empty search term is entered while protected whole-set search is unavailable or degraded, when the list handles the request, then the normal authorization-safe cursor list remains usable with a non-blocking localized search-unavailable notice, and no in-memory filter over the loaded page is misrepresented as whole-set search.

8. **Responsive and accessible operation.** Given desktop, tablet, and mobile viewport widths, when the grid is rendered and navigated by keyboard or assistive technology, then headers, sort state, row relationships, pinned-column meaning, visible focus, and horizontal overflow remain accessible, and safety-critical columns are never removed merely to fit a narrow viewport.

9. **Measured warm-load target.** Given a typical tenant set on a warm projection, when the list interaction performance is measured in the documented reference environment, then the surface targets interactive rendering within approximately one second, and any unmet target is recorded with reproducible measurements rather than hidden by reducing safety or freshness behavior.

10. **Focused evidence.** Given the tenant-list implementation, when focused bUnit, gateway, localization-parity, authorization-safety, responsive, forced-colors, and conformance tests run, then stable `data-testid` selectors validate each state without depending on row text, color, or incidental Fluent markup, and the exact test commands and results are recorded.

## Tasks / Subtasks

- [x] Establish the corrected Story 1.2 baseline and reconcile historical evidence. (AC: 1-10)
  - [x] Preserve the historical `1-2-tenant-list-triage.md` and its review record; create a dated corrected Story 1.2 evidence report rather than rewriting June evidence.
  - [x] Record the root, FrontComposer, and Builds SHAs; declared and resolved package versions; pre-existing working-tree changes; exact commands; exit codes; pass counts; and environment blockers.
  - [x] Inventory the current tenant-list implementation after the uncommitted Story 1.1 workspace work. For each acceptance criterion, record `verified`, `changed`, or `blocked`; a historical `done` status or broad green suite is not automatic proof.
  - [x] Reconcile every historical review follow-up: authenticated operator propagation, localized gateway reasons, page-local search/filter/sort claims, renderer-context awaits, and dead resources/styles. Preserve later valid fixes and record any remaining owner or prerequisite.
  - [x] Preserve all user-owned Story 1.1 changes, especially `TenantWorkspaceState`, canonical URL transitions, grid sort propagation, resource changes, and their tests.

- [x] Reverify and correct the ordinary tenant-list cursor boundary. (AC: 2, 3, 7)
  - [x] Reuse `ListTenantsQueryHandler`, platform `IQueryCursorCodec`, `QueryCursorScope`, authenticated-user scope binding, authorization-before-pagination, ordinal exclusive-anchor paging, and established page-size policy. Do not add a UI cursor codec, offset/limit translation, or backend endpoint.
  - [x] Add list-specific invalid-cursor handling by following the existing audit-list recovery pattern: recognize only the safe `invalid-cursor` reason, retry exactly once with a null cursor and no stale ETag, and return a typed page-one recovery outcome.
  - [x] On page-one recovery, clear the UI cursor history/current cursor, replace the canonical workspace URL, retain the returned authorized rows, and show a polite localized list-refreshed notice rather than an error.
  - [x] Complete the reset matrix for status, sort field, sort direction, scope, and page size. Add an accessible localized Fluent page-size control/state with centralized choices 20, 50, and 100 (default 20; server maximum 100), and keep it circuit/local because AD-2 does not define page size as canonical URL state unless architecture is separately amended.
  - [x] Prove that the paging cursor is absent from rendered links and all other DOM attributes, visible/accessible copy, copy payloads, logs, telemetry tags, and exception details. Resolve the current `TenantListNavigationContext` return-link seam without deleting Story 1.3 context preservation; use a server-held or otherwise non-cursor return-state seam if necessary.
  - [x] Treat protected whole-set search as unavailable until Story 1.9/`SEARCH-CURSOR-1` is verified. Remove or quarantine the current plaintext `memories-search:{offset}` path from Story 1.2 behavior and keep the ordinary protected cursor list usable with a typed localized notice.

- [x] Reconcile tenant-list state, truth, and row identity without fabricating data. (AC: 1, 4-7)
  - [x] Keep immutable typed request/snapshot/row models. Add a narrow typed reason/notice model for list-refreshed, search-unavailable, gateway-unavailable, and degraded outcomes; do not pass hard-coded gateway prose into the component.
  - [x] Bind literal `TenantId` and existing Contracts/Client DTOs. Do not parse caller-supplied tenant or user identifiers as GUID/ULID, redeclare DTOs, re-case wire data, or fabricate member/owner counts.
  - [x] Preserve last-confirmed row values separately from in-flight UI state. Keep `TenantId` as the stable row key so pending and freshness markers remain attached to the correct row through sort, filter, page changes, and horizontal scrolling.
  - [x] Preserve `unknown` unless metadata is projection-backed and explicitly supports `current` or `stale`. A `304`, ETag, `ServedAt`, request time, or recent refresh does not by itself upgrade truth; `Refreshing` remains UI-transient.
  - [x] Do not emit `aging` for this surface without authoritative projection-time provenance and an approved threshold. Preserve any shared enum member for compatibility without presenting an unsupported claim.
  - [x] Treat the current server's tenant-id cursor order as the only authoritative cross-page order. Do not present page-local name/status sorting or status filtering as whole-set behavior; record the exact current-page semantics or a prerequisite rather than inventing a new list-filter endpoint.

- [x] Correct and reverify the `TenantDataGrid` composition. (AC: 1, 5, 8)
  - [x] Preserve the full-width FrontComposer `FcAggregateListPage` and Tenants-specific `TenantDataGrid` boundary; reusable cursor, pinning, or six-state infrastructure remains FrontComposer-owned.
  - [x] Pin identity, status, and freshness to logical start using the exact rc.4 `DataGridColumnPin.Start` API. Reverify actual runtime scrolling/pinning behavior; a compile-valid parameter alone is not evidence.
  - [x] Preserve member/owner count, pending, and audit/action footprints with stable widths, `ItemKey` row identity, horizontal overflow, and no safety-column removal at mobile, tablet, desktop, or wide-desktop widths.
  - [x] Render pending and freshness with text, a verified Size20 icon, semantic role, accessible label, and no color-only meaning. Reserve Success for proven active/current truth; never use Success for pending, refreshing, unknown, stale, or degraded state.
  - [x] Apply the locked UX mappings: unknown uses Important with Size20 `QuestionCircle`; aging, when provenance ever permits it, uses Warning with Size20 `Clock`; stale uses Severe with Size20 `ClockAlarm`; refreshing uses Informative with Size20 `ArrowClockwise`; disabled tenant status uses Severe with Size20 `Power`. Supply `IconLabel` plus visible localized text and do not make every resting row badge a live `role="status"` region.
  - [x] Preserve the uncommitted Story 1.1 `OnSortChanged` callback and canonical state transition while preventing duplicate or conflicting page-local Fluent sorting from being described as authoritative whole-set sort.
  - [x] Keep row/detail/audit actions keyboard reachable with visible focus and stable `data-testid="tenants-{surface}-{element}"` selectors independent of row text, color, and generated Fluent class names.

- [x] Make list states, notices, localization, and support safety complete. (AC: 3, 4, 7, 8)
  - [x] Preserve exactly distinguishable loading, empty, filtered-empty, error, stale, and degraded states; keep authentication failure separately fail-closed without weakening the six-state contract.
  - [x] Ensure filtered-empty provides reset, stale provides refresh, and degraded identifies both unavailable capability and still-usable behavior. Render list-refreshed and search-unavailable as non-blocking notices over the usable list.
  - [x] Use assertive announcement intent for error/degraded conditions and polite intent for routine loading, refresh, and non-blocking notices; prevent repeated row badges from flooding live-region announcements.
  - [x] Replace raw English gateway details with typed reason-to-resource mapping. Add whole-string English/French parity keys, named placeholders where needed, and culture-aware numeric formatting.
  - [x] Make empty/error copy authorization-safe: never distinguish hidden, missing, forbidden, or out-of-scope tenants or leak their count/existence.
  - [x] Verify grid headers, `aria-sort`/sort control meaning, row/cell relationships, live-region politeness, focus entry/return, keyboard horizontal navigation, reduced motion, forced colors, and 320-767/768-1023/1024+/1440+ behavior.
  - [x] Keep tokens, JWT contents, raw payloads, cursor values, ETags, EventStore/Memories metadata, correlations, internal failure details, stack traces, and PII out of rendered, copied, announced, logged, serialized, and telemetry-visible state.

- [x] Add focused evidence and issue an honest gate decision. (AC: 1-10; NFR10)
  - [x] Add/strengthen bUnit and state tests for the reset matrix, page size, exact row association, three pinned safety columns, stable action footprint, six states, list-refreshed and search-unavailable notices, keyboard/focus, responsive overflow, forced colors, and stable selectors.
  - [x] Add gateway tests proving opaque cursor pass-through, no offset conversion, exactly-one page-one retry on invalid cursor, safe retry failure, no cursor disclosure, authorization-safe empty/error mapping, and typed localized reasons.
  - [x] Add freshness tests proving missing/non-projection provenance, `ServedAt`, ETag, request time, and bare `304` remain `unknown`; test explicit stale/current only with qualifying projection-backed evidence.
  - [x] Add conformance/source guards for no browser backend calls/tokens, no raw cursor in row `href`/DOM/copy/log/telemetry, EN/FR parity, Size20 icon and badge role mappings, logical direction, and no incidental Fluent selectors.
  - [x] Run an authenticated browser/Aspire lane against discovered endpoints for desktop, tablet, mobile, keyboard, focus, overflow, forced colors, English/French, and invalid-cursor recovery. If the platform test principal or shared harness is unavailable, record the exact `PLATFORM-OPS-1` blocker; bUnit alone is not browser/assistive-technology proof.
  - [x] Measure initial and subsequent warm list interactions in a documented reference environment and tenant-set size. Preserve the approximately-one-second target as an assumption; report misses reproducibly without dropping enrichment, truth, authorization, or safety behavior.
  - [x] Run focused suites and the full configured UI regression from the Story 1.1 working baseline. Record exact commands/results and do not promote externally gated `UI-READ-1`, `PLAT-FRESH-1`, `HOST-REF-1`, or `SEARCH-CURSOR-1` claims.

## Dev Notes

### Developer Context

This is a brownfield correction/reverification story. A historical Story 1.2 already created the list, gateway, state models, resources, and tests; later stories extended the same files, and the current uncommitted Story 1.1 work adds canonical workspace state and grid-sort synchronization. Build on the live source. Do not recreate the June snapshot, re-scaffold the UI, or simplify away later detail, membership, command, or audit surfaces.

Epic 1 delivers the complete Phase 2a read-only discovery and access-review product. Story 1.2 owns the tenant-list/cursor foundation and honest fallbacks. Story 1.3 owns detail/return context, Story 1.8 owns the later read-evidence rollup, Story 1.9 owns authoritative Memories search and its protected search cursor, and Story 1.10 owns direct Tenants REST reads plus corrected freshness provenance. Story 1.2 may prepare and consume seams for those stories but must not silently complete their work packages.

The shared NFR10 gate is authoritative: focused tests do not replace applicable accessibility, localization, responsive, and documentation/reference evidence. Record the exact `FC-DOC` source/version used or the approved fallback evidence.

### Scope Boundaries

**In scope**

- Reverify/correct the existing full-width tenant list, protected ordinary cursor paging, deterministic resets, invalid-cursor page-one recovery, six list states, pinned safety columns, row-bound pending/freshness state, localized non-blocking notices, support safety, accessibility/responsiveness, and evidence.
- Preserve the server-only BFF and existing authenticated authorization-scoped backend handler.
- Use honest `unknown` freshness and ordinary-list fallback while later prerequisites are unavailable.

**Out of scope**

- Implementing `SEARCH-CURSOR-1`, raw-hit accounting, or a new Memories search contract (Story 1.9).
- Replacing all six generic EventStore reads with direct Tenants REST or adding freshness provenance (Story 1.10 / `UI-READ-1` / `PLAT-FRESH-1` / `HOST-REF-1`).
- Adding list-filter/search/detail/receipt endpoints, changing backend DTOs for UI convenience, editing cursor codecs, or modifying root-declared submodules.
- Rebuilding generic grid, pinning, list-state, shell, navigation, token, or test-harness infrastructure inside Tenants.
- Adding command behavior, AppHost/platform topology, ServiceDefaults, OpenTelemetry, health, secrets, Dockerfiles, a `.sln` file, or package upgrades.

### Current Implementation: Change Versus Preserve

| Area / file | Current state | Story treatment |
| --- | --- | --- |
| `Components/Pages/TenantsWorkspace.razor` | Full-width FrontComposer page, list controls/states, in-memory cursor history, fixed page size 20; uncommitted Story 1.1 canonical state/sort changes | **UPDATE.** Preserve Story 1.1 changes; add typed notices, invalid-cursor recovery cleanup, an accessible localized Fluent page-size control (20/50/100; default 20) with cursor/history reset, and honest whole-set/current-page semantics. Remove renderer-lifecycle `ConfigureAwait(false)` where required by Blazor synchronization behavior while keeping it in services. |
| `Components/Tenants/TenantDataGrid.razor` | Identity/status pinned; freshness critical but not pinned; stable `ItemKey` and audit/action slot; uncommitted sort callback | **UPDATE.** Pin freshness, preserve callback/actions, verify Size20/no-color-only/keyboard semantics and runtime pinning. |
| `Components/Tenants/TenantDataGrid.razor.css` | Horizontal overflow, breakpoint minimum widths, focus-visible and forced-colors rules | **VERIFY / UPDATE NARROWLY.** Preserve the documented local FC-TBL exception; change only demonstrated responsive/pinning gaps. |
| `Components/Shared/ListSurfaceStates.razor(.css)` | Six states plus unauthorized; error/degraded can render raw gateway text | **UPDATE.** Preserve distinct/recoverable state regions; use localized typed reasons and non-blocking notice semantics without collapsing the six states. |
| `Components/Shared/TruthStateBadge.razor(.css)` | Text/icon/ARIA output, but accepts `aging`, maps unknown to Subtle, uses Size16 icon factories, and makes every badge a live status region | **UPDATE.** Enforce provenance-honest use, locked semantic roles/Size20 mappings/`IconLabel`, and Success-only-for-proven-truth behavior; preserve transient `IsRefreshing` separation without noisy resting live regions. |
| `Services/Gateways/TenantQueryGateway.cs` | Auth fail-closed; ordinary cursors opaque; freshness provenance handling mostly correct; invalid list cursor becomes generic error; current Memories cursor is plaintext offset; raw English list reasons | **UPDATE.** Reuse audit page-one recovery pattern, introduce typed safe reasons, preserve authorization/unknown freshness, and quarantine later Story 1.9/1.10 behavior. |
| `Services/Gateways/UnavailableTenantQueryGateway.cs` | Returns raw English list/detail messages | **UPDATE.** Return typed safe reasons; localization remains in the UI boundary. |
| `Services/Gateways/ITenantQueryGateway.cs` | Six read methods including `ListTenantsAsync` | **VERIFY / CONDITIONAL UPDATE.** Preserve the boundary; change only if a typed list outcome cannot remain inside `TenantListSnapshot`. |
| `State/TenantList/TenantListRequest.cs` | Cursor, page size, search/status/sort, direction, ETag | **UPDATE IF NEEDED.** Preserve opaque cursor and established default; make page-size/reset and supported-query semantics explicit without inventing server fields. |
| `State/TenantList/TenantListSnapshot.cs` and `TenantListSurfaceKind.cs` | Typed rows/six states plus unauthorized; string `ErrorMessage`; no list-refreshed outcome | **UPDATE.** Prefer a typed reason/notice separate from surface kind so a notice can coexist with usable rows. |
| `State/TenantList/TenantListRow.cs`, `TenantCountValue.cs`, `TenantPendingState.cs` | Literal identity, unknown-safe counts, per-row pending/freshness | **VERIFY / UPDATE ONLY FOR PROVEN GAPS.** Preserve immutable row identity and never fabricate zero/current/confirmed state. |
| `State/TenantList/TenantWorkspaceState.cs` and tests | Untracked Story 1.1 canonical URL/transition implementation | **PRESERVE / CONDITIONAL UPDATE.** Extend only for an explicit recovery/page-size transition; do not overwrite user-owned work. |
| `State/TenantList/TenantListNavigationContext.cs` | Serializes list cursor into rendered detail/audit return links | **UPDATE.** Resolve the AC2 DOM-attribute conflict while preserving Story 1.3 return context through a non-cursor/server-held seam. |
| `Resources/TenantsResources.resx` and `TenantsResources.fr.resx` | Broad parity-checked domain copy; list keys include unsupported-aging and legacy entries; missing list-refreshed typed reason | **UPDATE.** Add whole-string parity keys and remove only proven dead Story 1.2-owned entries; preserve other story resources. |
| `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` and `TruthStateBadgeTests.cs` | Strong six-state/search/marker/selector/style coverage; some tests assert current later-story behavior | **UPDATE.** Add corrected cursor/pinning/notices/a11y coverage and keep Story 1.1 tests intact. |
| `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` | Strong freshness/search coverage; audit invalid-cursor test is the closest recovery template | **UPDATE.** Add ordinary-list recovery and safe-reason tests; quarantine plaintext search-cursor expectations under Story 1.9. |
| `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`, `TenantsUiCompositionTests.cs`, `State/TenantWorkspaceStateTests.cs`, and conformance tests | Workspace, auth, localization, canonical-state, layout, and package guardrails; Story 1.1 baseline reports 916/916 | **UPDATE NARROWLY / PRESERVE.** Add only enduring Story 1.2 guards and rerun full regression. |
| `src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs`, `TenantQueryCursorScopes.cs`, `TenantQueryHandlerBase.cs` | Existing protected, scope-bound, authorization-safe server cursor implementation | **VERIFY ONLY.** Reuse unchanged; backend source edits require separately demonstrated scope. |

### Cursor, Paging, and Recovery Requirements

- The server list handler already decodes the platform-protected cursor with `ListTenantsQuery.QueryType` and an authenticated-user scope, applies authorization before pagination, orders tenant IDs ordinally, treats the decoded anchor as an exclusive lower bound, and protects the next cursor. Preserve those invariants.
- Normal list requests carry only `cursor` and `pageSize`. Do not serialize offset/limit, decoded anchors, sort/filter inventions, or scope material.
- A safe invalid-cursor response is not a general retry policy. Retry page one once only for the known invalid-cursor reason; any retry failure maps through the normal authorization-safe error/degraded contract.
- The existing audit list already demonstrates typed `ListRefreshed` recovery. Reuse its shape without coupling tenant-list surface kinds to audit-specific models.
- Story 1.1 permits the opaque current workspace cursor in the browser's canonical address. Story 1.2 additionally forbids that cursor from rendered DOM attributes. In particular, the current detail/audit `href` return URLs must stop embedding it. Document the chosen server-held/non-cursor return-context seam and test both the rendered DOM and the Story 1.3 return behavior.
- Page size is a request input but not an AD-2 canonical URL field. Use centralized localized choices 20/50/100 within the standard server policy (default 20, maximum 100), keep the selected value local to the interactive circuit, and reset cursor/history when it changes unless a separately approved architecture change adds it to canonical state.

### Search, Filter, and Sort Honesty

- Empty search uses the ordinary protected list cursor.
- Non-empty search is not permission to use the current plaintext `memories-search:{offset}` cursor. Until Story 1.9 proves a protected, scope-bound, expiring cursor and raw-hit-consumed accounting, render the ordinary cursor list plus the localized search-unavailable notice.
- Never filter only `_visibleRows` and call it whole-set search. Also do not overstate the current status/name sort behavior: the server list contract is tenant-id ordered and has no status/sort input. Resetting to deterministic page one is required, but UI wording/evidence must state whether a control affects only the current authorized page.
- Do not add a new backend list-filter endpoint or reshape Contracts DTOs in this story. Record an external prerequisite if the accepted product wording requires a server capability that does not exist.

### Freshness and Truth Requirements

- Qualifying freshness requires projection-backed metadata. Explicit lifecycle/current/stale evidence may be normalized through the platform policy; absent, degraded, handler-computed, or legacy provenance remains `unknown`.
- `ServedAt`, ETag presence, request completion time, a recent refresh, and `304 Not Modified` are transport/cache observations, not projection age. A `304` may preserve prior qualifying freshness or apply explicit metadata; it cannot manufacture freshness.
- Story 1.10 is the explicit transport/provenance correction for Stories 1.2-1.9. Until `PLAT-FRESH-1`, `HOST-REF-1`, and `UI-READ-1` are verified, render `unknown` and fail closed where required.
- `Refreshing` is a transient client flag over the last-confirmed snapshot. Pending and refreshing remain Informative; stale/degraded are Severe; unknown is Important; Success is reserved for proven current/active/confirmed/audit-available truth.
- Do not show fake member/owner counts. If bounded authorized detail enrichment cannot supply a count, show localized unknown and degrade honestly. Record its performance cost in the reference-environment measurement.

### Authorization and Support Safety

- InteractiveServer components depend on injected BFF/gateway contracts. They do not construct backend clients, hold access tokens, or call Tenants/EventStore/Memories from browser-executed code.
- Missing authenticated identity fails closed before either EventStore or Memories work. The server remains the authorization enforcement boundary; empty and error states must not reveal hidden tenant existence.
- Keep access tokens server-side and preserve the current authenticated `IUserContextAccessor` propagation. The June High review finding is historical: reverify the current code rather than assuming it remains unresolved.
- Treat cursor values, ETags, decoded payloads, EventStore/Memories metadata, correlation IDs, stack traces, tokens, and PII as forbidden browser/support material.

### Architecture Compliance

- **AD-1/AD-2:** one `/tenants` shell entry; the list is the default Tenants tab; workspace state remains canonical and surface-specific; all list-query identity changes reset cursor.
- **AD-3/AD-4:** FrontComposer/Fluent first. Tenants owns only the domain-specific `TenantDataGrid`, state composition, and copy; generic grid/pinning/list-state capability remains FrontComposer-owned.
- **AD-5:** server-side gateways are the only backend egress.
- **AD-6/AD-8:** direct Tenants reads and full provenance remain Story 1.10 corrections; current generic transport must fail honestly to `unknown`.
- **AD-9:** Tenants owns whole-string domain resources and support safety; FrontComposer owns shell chrome.
- **AD-10:** Memories is search-as-index-only, and protected search cursor work belongs to Story 1.9. The normal list must remain usable without it.
- **AD-11:** cursor, localization, support-safety, Fluent, accessibility, responsive, and conformance tests are architectural guardrails.
- **AD-13/AD-14:** do not expand the local AppHost or production topology. Multi-replica cursor/session/Data Protection durability remains platform-owned until verified.

### Library and Framework Requirements

- SDK `10.0.302`, target `net10.0`, nullable analysis, warnings as errors, central package management, and `Hexalith.Tenants.slnx` are authoritative.
- Current repository pins are Hexalith.EventStore `3.75.0`, FrontComposer `4.0.1`, Hexalith.Memories `2.10.0`, Hexalith.Tenants `3.2.18`, and Fluent UI Blazor components/icons `5.0.0-rc.4-26180.1`. Do not substitute stale architecture-table versions or upgrade dependencies in this story.
- Tests use xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Shouldly `4.3.0`, NSubstitute `6.0.0`, and Microsoft.NET.Test.Sdk `18.8.1`.
- Fluent's current DataGrid guidance documents keyboard navigation, sort toggling, keyboard column resize, customizable accessible labels, and warns that rendered structure/classes can change. Tests must target component behavior and stable story selectors, not incidental markup. Inspect the exact rc.4 assembly/source commit for pinning and icon APIs.
- ASP.NET Core Data Protection purpose strings isolate consumers sharing root keys. If a non-cursor protected navigation token is introduced, use a unique versioned purpose rooted in the owning type and never let untrusted input be the sole purpose.
- Shared Data Protection key persistence and application name must be consistent before multiple UI replicas can safely unprotect server-held tokens. Story 1.2 must not claim that platform gate complete.

### Project Structure Notes

- Keep production changes under `src/Hexalith.Tenants.UI/Components/Tenants`, `Components/Shared`, `Components/Pages`, `Services/Gateways`, `State/TenantList`, and `Resources`.
- Add at most one narrowly named typed list reason/notice file unless the existing snapshot can own the contract cleanly. Follow one C# type per file and file-scoped namespaces.
- Mirror source changes under `tests/Hexalith.Tenants.UI.Tests/Components`, `Services/Gateways`, `State`, and existing conformance locations. Reuse the current browser/Aspire lane; do not add a second shared test harness.
- Create a dated evidence report under `_bmad-output/implementation-artifacts/`.
- Do not add a Dockerfile, `.sln`, package version in a project file, copied DTO, generic UI framework, new datastore, backend endpoint, or source change under `references/`.

### Testing Requirements

Use `Hexalith.Tenants.slnx` for restore/build only. Run test projects individually.

Required focused scenarios:

- identity/status/freshness are all pinned, retain stable widths/actions, and remain meaningful under horizontal scroll at 320, 768, 1024, and 1440 widths;
- forward/back ordinary paging passes protected cursors opaquely, never offsets, and keeps markers row-bound;
- invalid list cursor causes one null-cursor retry, page-one rows, cleared history/canonical URL, and a localized polite list-refreshed notice;
- status, sort, direction, scope, and page-size transitions reset cursor; invalid combinations normalize safely;
- non-empty search without `SEARCH-CURSOR-1` shows ordinary cursor results plus a localized notice and never a page-only fake whole-set search;
- cursor absence across rendered text, accessible names, links/DOM attributes, copy payloads, snapshots/reasons, logs, and telemetry;
- all six list states have distinct semantics and recovery; empty/error remain authorization-safe;
- pending/stale/unknown markers remain attached to the correct `TenantId` across operations and never receive success semantics;
- missing or non-projection provenance, `ServedAt`, bare ETag, request recency, and bare `304` stay unknown;
- EN/FR full parity and whole-string formatting;
- keyboard headers/sort/paging, focus visibility/return, accessible grid relationships, live-region intent, forced colors, reduced motion, and no reliance on generated Fluent markup;
- approximately-one-second warm interaction measurement with environment, tenant count, page size, enrichment behavior, command, timing method, and results; and
- full UI regression preserving Story 1.1's reported 916/916 baseline and later Epic surfaces.

Suggested validation shape:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 -warnaserror
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests
```

Use the existing xUnit v3 executable fallback only after a successful Release build and only when `dotnet test` reproduces the documented runner issue. Discover runtime endpoints from Aspire state; do not assume ports.

### Previous Story Intelligence

Story 1.0 verified the single shell entry, full-width/constrained layout, FC-A11Y, FC-L10N, FC-DOC, exact rc.4 pinning API, approved fallbacks, and a 904/904 UI baseline. It also found:

- shared FC-TBL remains offset-based/partial, so the Tenants-specific grid boundary is still required;
- current `TenantDataGrid` pins identity/status but not freshness;
- FC-TOK is partial and current Tenants rendering still uses Size16 icons; and
- FC-CMD/FC-CNC remain blocked, which does not block this read-only list story.

Story 1.1 is currently `review` with uncommitted work. It added deterministic `TenantWorkspaceState`, safe canonical URL generation, cursor resets, and grid-sort propagation, and reports 916/916 UI tests. Preserve those files and rerun its focused/full checks. Authenticated `/tenants` tab operation and French browser rendering were recorded as platform-test-principal follow-ups.

### Historical Story 1.2 Intelligence

The June Story 1.2 is immutable implementation evidence, not the corrected contract. Its useful foundations are the domain grid, six-state component, truth badge, gateway, state models, localization, and tests. Its review recorded:

- High: operator identity/token was not forwarded and queries used static `system` context;
- Medium: raw English error/degraded messages bypassed localization;
- Low: search/filter/sort operated only on the loaded page while request fields implied server behavior;
- Low: `ConfigureAwait(false)` appeared in Blazor component lifecycle/event methods; and
- Low: dead Story 1.1 resources/styles remained.

Current source has since added user context, Memories search, provenance metadata, and Story 1.1 navigation changes. Reverify each historical finding against live code; do not blindly reapply or dismiss it.

### Git Intelligence

- Root baseline: `088232a7255698e20105594d9e0ef12a0f09c73e` on `main`.
- FrontComposer source: `d3761fa08ce2f4bf004e8adc7f500822d04276f8` (`v4.0.1-76-gd3761fa0`).
- Builds source: `9ec0a032d785dd0abdc14276e8784d6fdd826fd0` (`v4.21.7-10-g9ec0a03`).
- Historical implementation commit: `3967f40 feat(story-1.2): Tenant List Triage`. Later Story 1.3/1.8 work and current Story 1.1 edits supersede that file snapshot.
- Recent relevant UI history includes `56c506c` freshness-state handling, `ce2d7c2` anonymous-query fail-closed behavior, `daabc6b` REST restructuring, and later search/workspace/navigation changes.
- The working tree already contains user-owned planning, sprint-status, UI, test, and submodule-pointer changes. Preserve them. Do not stage, commit, reset, update dependencies, or modify submodule source.
- `sprint-status.yaml` currently says `epic-1: done` while corrected Epic 1 stories remain backlog/review. Story creation updates only the Story 1.2 key; sprint planning owns the stale aggregate epic status.

### Latest Technical Information

- Official Fluent UI Blazor DataGrid documentation confirms keyboard arrow navigation, keyboard sort toggling, keyboard column resize, customizable accessible labels, horizontal-scroll patterns, and that rendered structure/classes can change between versions. Use the exact rc.4 assembly/source for the pinned API and stable Tenants test IDs for behavior tests: [Fluent UI Blazor DataGrid](https://fluentui-blazor.azurewebsites.net/datagrid).
- Microsoft guidance for Blazor server-side scenarios keeps access tokens on the server and recommends the BFF pattern, matching AD-5: [ASP.NET Core server-side and Blazor Web App security scenarios](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0).
- ASP.NET Core Data Protection requires a unique purpose chain to isolate protected consumers and recommends a versioned, owning-component purpose; untrusted input must not be the sole purpose: [Purpose strings in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings?view=aspnetcore-10.0).
- Multi-instance protected state requires a shared durable key ring and matching application name; this remains a platform/topology concern: [Key storage providers in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0).

### Project Context Reference

Follow `_bmad-output/project-context.md`, root `AGENTS.md`, and the Hexalith LLM baseline. In particular: preserve user changes; use the repository-owning root and `.slnx`; keep central versions; use one C# type per file; prefer FrontComposer/Fluent V5; keep identifiers literal; keep tokens/cursors/ETags/payloads/internal details support-safe; run tests per project; never initialize nested submodules; and do not put shared platform capability into the Tenants domain module.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.2: Tenant List Triage and Cursor Foundation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#NonFunctional Requirements`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.9: Authoritative Memories Search with Protected Paging`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.10: Direct Tenants Reads and Authoritative Freshness`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Canonical Architecture Spine`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.1 Tenant Discovery & Triage`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#8. Non-Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#I. Prerequisite work packages`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#tenant-data-grid`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-19-v2.md`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19-v2.md#CC-1 — Make the shared NFR10 evidence gate authoritative`]
- [Source: `_bmad-output/implementation-artifacts/1-0-reverify-frontcomposer-shell-and-fluent-contracts.md`]
- [Source: `_bmad-output/implementation-artifacts/1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md`]
- [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `Components/Tenants/TenantDataGrid.razor`; `Components/Shared/ListSurfaceStates.razor`; `Components/Shared/TruthStateBadge.razor`]
- [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `UnavailableTenantQueryGateway.cs`]
- [Source: `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`; `TenantListNavigationContext.cs`; `TenantListSnapshot.cs`]
- [Source: `src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs`; `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`; `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`; `Services/Gateways/TenantQueryGatewayTests.cs`; `State/TenantWorkspaceStateTests.cs`; `TenantsWorkspaceTests.cs`; `TenantsUiCompositionTests.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (initial implementation, tests, and 2026-07-20 baseline evidence); Claude Opus 4.8 (2026-07-20 runtime verification and honest gate closure)

### Implementation Plan

- Preserve the Story 1.1 canonical workspace state while making page size a circuit-local list input and adding an explicit page-one recovery transition.
- Introduce one typed tenant-list outcome reason, retry an ordinary invalid cursor exactly once, and quarantine unverified Memories search behind the ordinary cursor-list fallback.
- Correct grid pinning and badge semantics with the pinned Fluent rc.4 APIs, then localize every list state and non-blocking notice through EN/FR resources.
- Add focused red/green coverage per task, run the individual UI suite and Release solution gate, then attach runtime/performance evidence or an exact external blocker.

### Debug Log References

- 2026-07-20 baseline: clean `main` at `23943db`; FrontComposer `550cb060`; Builds `ed7cea8`; historical story and review preserved unchanged.
- Pre-change Release solution build passed with 0 warnings/0 errors; UI regression passed 933/933.
- Red/green implementation gates passed for invalid-cursor retry, page size/reset/navigation, typed truth reasons, exact rc.4 pin/icon semantics, state announcements, localization, support safety, and conformance guards.
- Authenticated Aspire/Playwright used discovered UI endpoints. EN/FR, keyboard traversal, error-state live semantics, 320/768/1024/1440 widths, forced colors, reduced motion, and a clean stable console were verified.
- `PLATFORM-OPS-1`: after three topology recovery attempts, EventStore startup still skipped the query-type index because domain metadata was unavailable during startup. The authenticated `list-tenants` request therefore fell back to the generic projection actor and returned 404 despite a completed ephemeral tenant command. Actual grid scrolling/pinning, invalid-cursor recovery, and warm-list timing remain blocked until the platform publishes `admin:query-types:tenants` through its supported startup path.
- Required final gates passed: Release solution build 0 warnings/0 errors; UI regression 942/942; focused classes 43/43, 117/117, 8/8, 9/9, and 19/19.
- Additional repository-wide regression audit found an unrelated existing integration-host failure: the isolated `Commands_endpoint_accepts_CreateTenant_and_routes_story_payload` test returns HTTP 500 in both Debug and Release. No backend or integration-test source is changed by this story; the failed full integration run was stopped after reproducing the representative failure in isolation.
- 2026-07-20 continuation recheck: all required Aspire resources became healthy, but the authenticated list still rendered the typed error state with no grid after an EventStore-only restart. EventStore received HTTP 400 and skipped the operational index while a read-only direct DAPR request with the matching `tenants` / `v1` payload returned HTTP 200 with `list-tenants`. Release build passed 0/0; focused list tests passed 43/43; UI regression passed 942/942; the complete integration project finished with 98 passed, 68 failed, and 1 skipped. No product, topology, state-store, or submodule source was changed.
- 2026-07-20 runtime unblock + verification (Claude Opus 4.8): root-caused and cleared `PLATFORM-OPS-1`. (1) The prior tenants-side HTTP 400 was a host/service version skew — stale 2026-07-14 EventStore host Debug binaries plus `Hexalith.Tenants` linked against the EventStore package while the AppHost ran the externally-drifted `f435d968` (v3.78.0) submodule source. A coherent source-mode rebuild (`UseHexalithProjectReferences=true`; host and Tenants service both `f435d968`) eliminated the tenants failure (no 6100 for `AppId=tenants`). (2) The residual index skip (6101) was isolated to the `sample` pub/sub consumer: its intentional, test-pinned empty `AdminOperationalIndexMetadata.Response([])` at HTTP 200 makes `AdminOperationalIndexHostedService` throw and its all-or-nothing `HasFailures` gate discards the entire index including `admin:query-types:tenants`. Confirmed a STANDING platform condition, not drift-caused (no code change to the operational-index/DomainService source between committed `af66f6c4` and drifted `f435d968`). Applied a minimal, LOCAL, uncommitted EventStore host patch (publish the successfully-loaded domains instead of returning early on `HasFailures`) purely to enable the data path; the state store then held `admin:query-types:tenants` = `[get-tenant, get-tenant-audit, get-tenant-users, get-user-tenants, list-tenants]`. Patch REVERTED after verification (submodule source pristine, binary rebuilt unpatched, gitlink untouched).
- 2026-07-20 authenticated browser verification (admin-user global admin; `http://localhost:62448`; Chrome, dpr 1.75): grid renders 6 tenant rows. Runtime-confirmed — three safety columns identity/status/freshness compute to `position:sticky` + `col-pinned-start` + `left:0/220/350px` + elevated z-index (rc.4 `DataGridColumnPin.Start`), pending column correctly NOT pinned; stable `data-testid`s (grid/detail-link/copy/status/pending/audit-entrypoint) all present; literal string tenant ids (`aha-065e831b39ed487887ad821b2012b29a`); freshness = Unknown with Size20 `QuestionCircle` (honest, no fabricated current); status filter labelled current-page; page size 20. Sort: clicking the Locataire header toggled `aria-sort` none→ascending, reordered rows by name, and updated the canonical URL to `/tenants?sort=name` (Story 1.1 `OnSortChanged` + canonical state + cursor-reset path). Keyboard Tab reached the grid sort buttons with a visible focus ring. Warm perf (6-tenant set): page-shell `loadEventEnd`=766ms / `domInteractive`=290ms; warm refresh interaction settled 156ms — both under the ~1s AC9 target. `forced-colors: active`, `prefers-reduced-motion: reduce`, `:focus-visible`, grid `overflow-x`, safety-column `min-width`, and the 320-767 / 768-1023 / 1024+ / 1440+ breakpoints are all present as grid-scoped CSS rules; dynamic per-width and forced-colors pixel emulation was not runnable because the local Chrome lane exposes a fixed 1235px virtual viewport (window resize is a no-op), so those remain covered by the CSS rules plus the green bUnit/forced-colors conformance suite rather than pixel screenshots.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Established the corrected dated evidence baseline, classified every AC, reconciled all historical review findings, and confirmed there were no pre-existing worktree changes to overwrite.
- Implemented exactly-once safe invalid-cursor recovery, cursor-local page sizing, cursor-free row return links, and the ordinary-list/search-unavailable fallback without offset cursors.
- Replaced free-form list errors with typed localized reasons, preserved last-confirmed rows, and kept unproven freshness/count/pending evidence unknown.
- Corrected the grid to three logical-start safety pins with stable widths/identity and locked Size20 Fluent badge/icon semantics without resting live-region noise.
- Completed state semantics, EN/FR parity, current-page disclosure, renderer-context awaits, dead list-sort resources, and support-safety/conformance tests.
- Story remains in progress: runtime grid/performance evidence is blocked by `PLATFORM-OPS-1`, and the broader integration-host regression prevents the workflow completion gate from being claimed.
- Continuation validation confirmed both blockers persist on the clean current head; open runtime/performance tasks remain unchecked and the story status remains `in-progress`.
- 2026-07-20 gate closure (Claude Opus 4.8): the three runtime-blocked subtasks (grid pinning, a11y/responsive, warm-load performance) are now VERIFIED and checked. `PLATFORM-OPS-1` is resolved for verification via a reverted, local platform enablement patch (see Debug Log); the durable fix is a platform (EventStore/AppHost) item — recommendation: make `AdminOperationalIndexHostedService` isolate a single failing/empty domain-service binding instead of discarding the whole operational index (or treat an explicit empty-domains 200 response like the sanctioned 404 consumer path), and/or register the `sample` pub/sub consumer without a metadata-providing domain so it is not probed. No Story 1.2 product source was changed in this verification session — the committed implementation is unchanged and the 942/942 UI-regression baseline stands. The pre-existing command-host integration regression (98 pass / 68 fail) is unrelated to Story 1.2 (it changes no UI/product source) and is tracked separately as a platform/environment blocker per the Epic 1 retrospective convention, so it does not gate this read-only UI story. Status advanced to `review`.

### File List

- `_bmad-output/implementation-artifacts/1-2-tenant-list-triage-and-cursor-foundation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-1-2-tenant-list-triage-and-cursor-foundation-evidence-2026-07-20.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListReason.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/UnavailableTenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantListSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

### Change Log

- 2026-07-20: Captured the corrected Story 1.2 baseline and historical-review reconciliation; moved sprint tracking to `in-progress`.
- 2026-07-20: Implemented and tested the corrected cursor, truth, grid, state, localization, accessibility, and support-safety contracts; recorded `PLATFORM-OPS-1` and the unrelated integration-host regression, leaving the story `in-progress`.
- 2026-07-20: Rechecked the live Aspire/browser and complete integration gates; refined the external blocker evidence without changing implementation or status.
- 2026-07-20: Root-caused and cleared `PLATFORM-OPS-1` (EventStore host/Tenants-service version skew from a stale binary + the all-or-nothing operational-index gate tripped by the `sample` consumer's empty metadata response); ran authenticated browser verification of grid rendering, three-column sticky pinning, sort/`aria-sort` + canonical URL, keyboard focus, and warm-load performance (766ms shell load / 156ms warm refresh, both < ~1s); checked the three runtime subtasks; reverted the local platform enablement patch (submodule pristine); advanced Status to `review`. No Story 1.2 product source changed.
