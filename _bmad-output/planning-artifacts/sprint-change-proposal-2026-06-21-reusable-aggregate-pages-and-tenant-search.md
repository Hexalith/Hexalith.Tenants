# Sprint Change Proposal — Reusable Aggregate List/Detail Pages (FrontComposer) + Memories-Backed Tenant Search

- **Date:** 2026-06-21
- **Author / Approver:** Jérôme Piquot (Administrator)
- **Workflow:** BMAD Correct Course
- **Review mode:** Incremental
- **Status:** Approved in principle (Phase 1 design + key decisions approved interactively); pending final document sign-off
- **Scope classification:** **Major** (cross-module, new shared FrontComposer contract, new `Hexalith.Memories` infra dependency, crosses two submodule boundaries) — de-risked by sequencing

---

## 1. Issue Summary

### Trigger
User request (verbatim, FR): *"Nous devrions avoir une page de détail du tenant et une page qui liste les tenants et qui permet aussi de faire la recherche."* Refined across four follow-ups:
1. The **common elements** of an aggregate-**list** page and an aggregate-**detail** page must live in **FrontComposer**.
2. They must be **reusable across all domain-type modules**.
3. The pages must contain **toolbars**.
4. The text search must be backed by the **`Hexalith.Memories`** module ("Use memories module to index text search").

### Problem statement
This is **not** a request for net-new pages. A tenant list page and tenant detail page already exist and shipped (Stories 1.2, 1.3, Epic 1 = *done*). The request reframes into two distinct goals:

- **G1 (defect / completion):** the list page's search/filter/sort is a **client-side stub** that filters only the loaded 20-row page; true search across the tenant set does not work (open Story 1.2 review follow-up). The user wants real search — implemented on `Hexalith.Memories`.
- **G2 (platform refactor):** extract the **common chrome** of list + detail pages (grid, search, filter, sort, paging, **toolbar**, header, sections) into **FrontComposer** as reusable building blocks for every domain module, with Tenants as first consumer. This is the CLAUDE.md *Domain Implementation Boundary* rule applied.

### Evidence (current state, code-verified)
- **List:** `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` (`@page "/"`, `/tenants`). Search box + status filter exist but filter client-side via `ApplyVisibleRows()` (`:277-307`); `_search`/`_statusFilter` are placed on `TenantListRequest` but **dropped** by `TenantQueryGateway.CreateListRequest` (`:431-446`) — only `cursor`+`pageSize` reach the BFF.
- **Detail:** `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` (`@page "/tenants/{TenantId}"`): identity facts + `FluentAccordion` of 4 sections (metadata/lifecycle/members/config) + audit link.
- **Query contract:** `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs` is a bare routing marker — **zero** search/filter fields. Read store `TenantIndexReadModel.Tenants` is a `Dictionary<string, TenantIndexEntry(Name, Status)>` — `Name` is already materialized.
- **Specs:** PRD FR-1 ("scan, **search**, filter, sort, page"), FR-2 (open/return), FR-5 (detail). UX mockup `mock-tenant-list.html` literally shows `placeholder="Search tenants by name or id…"`. So search is **documented MVP scope, stubbed** — not new scope. FR-1 has **no testable consequence** defining search fields/semantics — a real gap.
- **FrontComposer:** already provides `FrontComposerShell`, `FcPageLayout` (FC-LYT ✔), `FcPageHeader` (with `Actions`/`Metadata` slots = the toolbar mechanism), a `[Projection]`-driven view source-generator (`DetailRecord` → `FluentCard`; `Default/ActionQueue/StatusOverview` → `FluentDataGrid`), and FC-TBL DataGrid blocks (`FcProjectionGlobalSearch`, `FcColumnFilterCell`, `FcStatusFilterChips`, `FcFilterResetButton`, `FcFilterEmptyState`, …). **Gap:** no *page-level* `FcAggregateListPage`/`FcAggregateDetailPage` wrapper.
- **Boundary:** `Hexalith.FrontComposer` and `Hexalith.Memories` are root-declared submodules under `references/`; Tenants must not edit submodule source unsolicited (CLAUDE.md Submodule Policy; FcPageHeader precedent = `sprint-change-proposal-2026-06-18-page-header-frontcomposer.md`).

---

## 2. Change Navigation Checklist — Results

| Section | Result |
|---|---|
| **1 Trigger & context** | ✅ Trigger = user feature request refined into G1 (search defect) + G2 (reusable extraction). Categories: *misunderstanding of original requirements* (search assumed done) + *new requirement emerged* (reusable FC pattern; Memories backing). |
| **2 Epic impact** | Epic 1 (list/detail) *done* — not invalidated. Epic 3 *in-progress*. ⚠ New cross-cutting work needed; Story 1.2 follow-up reopened; no existing epic made obsolete. |
| **3.1 PRD** | ⚠ FR-1 names search but has no testable consequence — must be tightened (fields, matching semantics). Reusable pattern is a platform concern, not a Tenants PRD requirement. MVP intact. |
| **3.2 Architecture** | ⚠ Must record the search-as-index-only data path (Memories supplies match-set; existing ETag-fresh read path supplies row data) and confirm it preserves D6 + the "consume-only backend / no new endpoints" constraint. |
| **3.3 UX/UI** | ⚠ Search box designed; **toolbars not specified** — needs a spec (list vs detail commands). |
| **3.4 Other artifacts** | ⚠ AppHost (Memories stack), packaging (new submodule refs), `project-context.md` (stale submodule list), tests, CI infra. |
| **4 Path forward** | Hybrid: **Direct Adjustment** (complete search) + **new stories** (FC extraction) under existing epic structure. Sequenced search-first. |
| **5 Proposal components** | This document. |
| **6 Review & handoff** | Final sign-off pending; sprint-status update + handoffs defined in §7–§8. |

---

## 3. Decisions (recorded from interactive Correct Course)

| # | Decision | Choice |
|---|---|---|
| D1 | Reuse model for the FC pages | **Wrapper components + data-source delegate** — `FcAggregateListPage<T>`/`FcAggregateDetailPage<T>` compose existing FC-TBL + FcPageHeader; each domain injects its own query gateway. Tenants keeps REST + ETag. |
| D2 | Sequencing | **Search-first, then extract.** |
| D3 | Proposal review mode | **Incremental.** |
| D4 | Search engine | **Adopt `Hexalith.Memories` now** — confirmed with full cost disclosure (below) and re-confirmed. |

### D4 accepted costs (confirmed during investigation)
- **Embeddings stack is mandatory even for BM25 search.** Ingestion (`IngestionWorkflow.cs:156,294`) unconditionally embeds + indexes semantic/graph → requires **Redis Stack + FalkorDB + Dapr + an embeddings provider** (Ollama or Google). "Syntactic-only, no embeddings" is dev-only.
- **No upsert.** Memories dedups on `sha256(SourceUri=cloudevent.id)`; renames require **delete-then-reingest** via an adapter.
- **Client paging gap.** `MemoriesClient.BuildSearchPath` omits `offset`/`subject`/`sourceType` → upstream patch handoff to the Memories repo for paging.
- **No reusable Aspire extension** → AppHost wiring or a separate Memories stack at `:5000`.

---

## 4. Recommended Approach

**Hybrid, two phases, search-first** (D2):

- **Phase 1 — Memories-backed tenant search** (completes G1, closes Story 1.2 stub). Self-contained value; de-risks Phase 2 by extracting a *working* list.
- **Phase 2 — FrontComposer reusable aggregate list/detail wrappers + toolbars** (delivers G2). Extracts the now-complete Tenants list/detail into `FcAggregateListPage<T>`/`FcAggregateDetailPage<T>`, then re-bases Tenants on them.

Rationale: lowest risk (proven artifact extracted second), delivers visible user value first, respects both submodule boundaries via explicit contracts/handoffs, and the search-as-index-only path **dissolves** the FrontComposer-`IQueryService` vs Tenants-REST/ETag conflict.

---

## 5. Detailed Change Proposals

### Phase 1 — Memories-backed tenant search

**P1.1 Ingestion adapter (NEW, Tenants host).** A projector/consumer that, on each tenant lifecycle event (`TenantCreated`, metadata/rename, `TenantDisabled/Enabled`, delete), publishes **one synthetic CloudEvent per tenant** to the Memories ingestion topic:
- stable `id = "tenant:{tenantId}"`, `subject` = TenantId, body `{ id, name, status }`;
- on rename/disable/delete → **delete-then-reingest** (Memories has no upsert);
- idempotent (DAPR at-least-once); `MessageId`-style dedup defense.
- Memories side ingests zero-code via `AddMemoriesEventStoreIntegration` (`EventStoreIntegrationServiceCollectionExtensions.cs:33`); `SourceToTenantMap` routes to a dedicated **`tenants-index` Memories tenant**.

**P1.2 BFF search path.** `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` `ListTenantsAsync`:
- when `Search` is non-empty → `MemoriesClient.SearchAsync(new SearchRequest("tenants-index", "syntactic", term))` → ordered `ScoredResult.SourceUri` → parse `tenant:{id}` → **hydrate each via existing ETag-fresh `GetTenantQuery`** (D6 preserved); row data never comes from Memories.
- when empty → unchanged cursor list.
- `ITenantQueryGateway`/`TenantListRequest` carry the real `Search` term end-to-end; remove the dropped-field stub.

**P1.3 Paging.** Search mode pages by Memories `Offset`/`MaxResults` ranking (depends on the P1.7 client patch); non-search keeps the opaque cursor. Document that the two orderings differ.

**P1.4 Degradation & freshness.** Memories down → `MemoriesRemoteException`/503 → disable search box ("search unavailable"), fall back to cursor list, never block. Filter match-set hits that hydrate to not-found. Document index lag (~<7 s) + rename-staleness as accepted eventual consistency. UX `filtered-empty` state covers empty results.

**P1.5 UI.** `TenantsWorkspace.razor` — `OnSearchChanged` → server round-trip; delete client-side `.Where` in `ApplyVisibleRows()` (closes Story 1.2 follow-up). **Default:** push status filter into the search query for consistency (confirm at implementation).

**P1.6 Packaging & AppHost.**
- Tenants UI refs `Hexalith.Memories.Contracts` + `Hexalith.Memories.Client.Rest`; `AddMemoriesClient(o => { o.Endpoint = …; o.ApiToken = HEXALITH_MEMORIES_API_TOKEN; })`. Publisher uses `DaprClient.PublishEventAsync` only.
- Add `Hexalith.Memories` to consumed root submodules; update `project-context.md` submodule list (currently omits `Hexalith.Memories` and `Hexalith.PolymorphicSerializations`).
- **AppHost:** add Redis Stack + FalkorDB + Memories Server + 4 Dapr components + embeddings (**default Ollama for local** to avoid API key/cost; Google for prod — confirm at implementation), **or** run the Memories AppHost separately at `:5000`.
- Avoid `[Experimental]` APIs (`IngestAsync`/`CreateTenantAsync` are HXL001); `SearchAsync` is not experimental — safe under `TreatWarningsAsErrors`.

**P1.7 Upstream handoff → `Hexalith.Memories` repo** (submodule; propose, don't edit unsolicited): add `offset`/`subject`/`sourceType` to `MemoriesClient.BuildSearchPath`; (optional) a syntactic-only ingestion gate to drop the embeddings/FalkorDB requirement.

**P1.8 Story.** New story replacing the 1.2 stub follow-up — **"Memories-backed cross-set tenant search"** — ACs: real name+id search across all tenants; ETag-fresh row data; paging; degradation; rename/lag eventual-consistency documented; tests (incl. Memories-down fallback).

### Phase 2 — FrontComposer reusable aggregate list/detail + toolbars

**P2.1 New FrontComposer contracts** (via the FC readiness-request handshake + this boundary-expansion approval; FcPageHeader precedent): **FC-LST** (aggregate list page) and **FC-DTL** (aggregate detail page).

**P2.2 New components** (FrontComposer repo, `Shell/Components/Layout/`):
- `FcAggregateListPage<TItem>` — composes `FcPageHeader` (+ `Actions` = **toolbar**) + `FcPageLayout` + FC-TBL grid blocks; parameters: `Items`/data-source **delegate** (D1), columns, opt-in search via `IProjectionSearchProvider<TItem>`, filters via `FcStatusFilterChips`/`FcColumnFilterCell`, paging, `EventCallback<TItem> OnRowSelected`. Empty/loading reuse existing placeholders.
- `FcAggregateDetailPage<TItem>` — `FcPageHeader` (`Actions` = **toolbar**, `Metadata` = return context) + `DetailRecord`-style body; parameters: `Item`, `RenderFragment<TItem> Sections`/field-group slots, optional `Tabs`, loading/not-found states.
- Both parameterize via the established triad (read-model attributes / override registries / RenderFragment+DI); domain copy stays in consumer `.resx`.

**P2.3 Tenants migration.** Re-base `TenantsWorkspace.razor` on `FcAggregateListPage<TenantSummary>` (data-delegate = `ITenantQueryGateway`, preserving the Phase 1 Memories search + ETag re-read) and `TenantDetailPage.razor` on `FcAggregateDetailPage`. Existing command-flow components (the 12 `*Flow.razor` already modified on this branch) rehost into toolbar `Actions` slots.

**P2.4 Upstream handoff → `Hexalith.FrontComposer` repo:** implement FC-LST/FC-DTL per the confirmed contracts; record contract docs in `references/Hexalith.FrontComposer/_bmad-output/contracts/`.

### Artifact updates

- **PRD** (`prds/prd-tenants-2026-06-02/prd.md`): add FR-1 **testable consequences** for search — fields (Name + Id), matching = Memories syntactic/BM25 (token, not literal substring; semantic optional), eventual-consistency/freshness caveat, degradation behavior.
- **Architecture** (`architecture.md`): add a "Tenant search (Memories-backed, index-only)" note — match-set from Memories, row data from existing ETag-fresh read path; confirm no new backend endpoint; record the resolved FC-`IQueryService`-vs-REST/ETag position (search bypasses it). Add FC-LST/FC-DTL to the FrontComposer contract section.
- **UX** (`ux-designs/ux-tenants-2026-06-02/`): spec the **toolbars** (list commands e.g. Create/Refresh; detail commands e.g. Edit/Lifecycle) and finalize the search component (placeholder, results/empty/degraded states).
- **Epics** (`epics.md`): reopen Story 1.2 search follow-up; add Phase 1 + Phase 2 stories (or a new Epic 6 "Reusable Aggregate Browse/Detail" — see §9).

---

## 6. Sprint-status.yaml changes (proposed)

Add (following the existing `cc-*` cross-cutting convention; final epic-vs-cc framing per §9):

```yaml
  # --- Cross-cutting Correct Course 2026-06-21 ---
  # Approved by Administrator via:
  # _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md
  cc-2026-06-21-memories-backed-tenant-search: backlog            # Phase 1
  cc-2026-06-21-frontcomposer-aggregate-list-detail-extraction: backlog  # Phase 2 (blocked by Phase 1)
  # Upstream handoffs (tracked, executed in submodule repos):
  #   - Hexalith.Memories: MemoriesClient paging params (offset/subject/sourceType); optional syntactic-only ingestion gate
  #   - Hexalith.FrontComposer: FC-LST / FC-DTL contracts + FcAggregateListPage<T>/FcAggregateDetailPage<T>
```

---

## 7. Implementation Handoff

**Scope: Major** → PM/Architect + Developer + two upstream submodule owners.

| Recipient | Responsibility |
|---|---|
| **Architect** | Confirm FC-LST/FC-DTL contracts; ratify the Memories index-only data path in `architecture.md`; AppHost-vs-separate-stack decision. |
| **Developer (Tenants)** | Phase 1 ingestion adapter + BFF search + UI + AppHost + packaging; then Phase 2 migration onto FC wrappers. |
| **FrontComposer owner (handoff)** | Build `FcAggregateListPage<T>`/`FcAggregateDetailPage<T>` per contracts; record contract docs. |
| **Memories owner (handoff)** | `MemoriesClient` paging params; optional syntactic-only ingestion gate. |
| **PM** | PRD FR-1 search testable consequences; epic framing (§9). |

**Success criteria:** operator searches the tenant list by name/id across all tenants with ETag-fresh rows and graceful Memories-down fallback (Phase 1); Tenants list+detail render via reusable FrontComposer components with toolbars, consumable by other domain modules (Phase 2); all tiers green; coverage gates held.

---

## 8. Open sub-decisions (defaults assumed; confirm at implementation)

1. **Embeddings provider:** default **Ollama (local)** / Google (prod). 
2. **Status filter:** default **push into the Memories search query** (vs keep client-side).
3. **Epic framing:** default **`cc-*` entries** (vs a new **Epic 6**).
4. **AppHost:** default **separate Memories stack at `:5000`** for first iteration (vs full in-AppHost wiring).
5. **Semantic axis:** default **syntactic/BM25 only at read** (semantic deferred to a future "global search").

---

## 9. Rollback / reversibility
Phase 1 is additive (new adapter + gateway branch); revert = remove the Memories branch, search box falls back to disabled/cursor list. Phase 2 migration is component-level; the pre-migration `TenantsWorkspace`/`TenantDetailPage` remain in git history. No event/projection rewrites; no compensating-command risk.
