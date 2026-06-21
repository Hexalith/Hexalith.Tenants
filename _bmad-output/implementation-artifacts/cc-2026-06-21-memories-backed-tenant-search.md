---
baseline_commit: d4aff694211983e34af4e1051d7e06dac69ab49b
---

# Story cc-2026-06-21: Memories-Backed Cross-Set Tenant Search

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Source of truth: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md (Phase 1). This is a Correct Course (cc-*) story, not an epics.md story. -->
<!-- Owner decisions locked 2026-06-21 (O1-O4 below). Architecture verified by code-mapping + an adversarial linchpin review of the Memories ingestion/search identity model. -->

## Story

As an **operator browsing the Tenants workspace**,
I want **the list-page search box to find tenants by name or id across the entire tenant set (not just the 20 rows currently loaded)**,
so that **I can locate any tenant directly instead of paging through the whole list, while the rows I see always show fresh, authoritative data**.

This completes **G1** of the 2026-06-21 Correct Course: replace the client-side search **stub** (which filters only the loaded page) with real cross-set search backed by `Hexalith.Memories`, using a **search-as-index-only** path — Memories returns a *match-set of tenant ids*; the existing **ETag-fresh `GetTenant`** read path (D6) supplies the *row data*. Closes the open Story 1.2 search follow-up and tightens PRD FR-1.

It is **Phase 1** of the proposal. Phase 2 (extracting reusable `FcAggregateListPage<T>`/`FcAggregateDetailPage<T>` into FrontComposer) is a **separate, blocked** story (`cc-2026-06-21-frontcomposer-aggregate-list-detail-extraction`) and is **out of scope here**.

### Owner decisions (locked 2026-06-21)
- **O1 — index identity:** stable `tenant:{tenantId}` id per tenant; index entry keyed by `(Tenant, AggregateId)`. Rename freshness comes from **upsert** (see O2 / `MemoriesIndexUpdate`), not a DELETE-then-reingest workaround.
- **O2 — filtering:** Memories indexes **structured attributes** (`Tenant`, `AggregateId`, and domain-defined customs e.g. `status`). The Tenants side emits a **`MemoriesIndexUpdate`** event carrying everything Memories needs; status filtering is a **structured attribute filter** in the search query (exact), not BM25 text.
- **O3 — ingestion ownership:** **Memories owns the index, ingestion endpoint, and search.** Tenants does **not** run a raw-event ingestion adapter. Instead Tenants emits **one curated `MemoriesIndexUpdate` per tenant**; Memories ingests/indexes/serves it.
- **O4 — infrastructure:** the **Tenants Aspire AppHost starts the Memories Server inline** (no separate Memories AppHost; MCP project excluded). Wiring plan in Dev Notes.

### Cross-repo split (read this first)
- **This story (Tenants repo, buildable now):** emit `MemoriesIndexUpdate`; the BFF search read path; in-AppHost Memories wiring; tests (against a mocked Memories); PRD/architecture updates; author the Memories handoff spec.
- **Memories submodule handoff (separate, tracked; BLOCKS end-to-end search):** define the `MemoriesIndexUpdate` contract + **upsert-by-`(Tenant,AggregateId)`** ingestion + **attribute indexing** + **attribute filtering in the REST search API** + **map the aggregate id into the search hit** + register the `tenants-index`. Do **not** edit `Hexalith.Memories/**` in this story — produce the spec and hand off. Unit tests here mock Memories; E2E is gated on the handoff landing.

## Acceptance Criteria

1. **Real cross-set name+id search.** A non-empty search term returns results computed across **all** tenants the caller may see (not just the loaded page), via `MemoriesClient.SearchAsync` (`syntactic`/BM25) against the dedicated `tenants-index` Memories tenant. A tenant matches when the term matches its **Name** or **TenantId** (token-based BM25). [Proposal P1.2; FR-1]
2. **Row data is ETag-fresh, never from Memories.** Each displayed row's data (Name, Status, member/owner counts, freshness, pending state) is hydrated through the existing `GetTenant` read path (`TenantQueryGateway` → `GET /api/tenants/{id}`), preserving D6 ETag/freshness. Memories decides *which* tenants appear, never *what each row shows*. [Proposal §3.2]
3. **Tenant id is recovered from the search hit reliably.** The BFF maps each `ScoredResult` back to a `TenantId` by parsing `ScoredResult.SourceUri` (shape `tenant:{tenantId}`). It must **not** parse `ContentSnippet` (200-char truncated) and must **not** depend on `cloudevent.subject` (not currently mapped into the hit). [Linchpin verdict A]
4. **Gateway carries the term end-to-end; stub removed.** `TenantListRequest.Search` reaches the BFF; `TenantQueryGateway.CreateListRequest` no longer silently drops `Search`/`Status`; the client-side search `.Where` in `TenantsWorkspace.ApplyVisibleRows()` is removed. [Proposal P1.2/P1.5; closes Story 1.2 follow-up]
5. **Search round-trips to the server.** `OnSearchChanged` triggers a server reload (`LoadAsync`), not an in-memory re-filter. Empty/whitespace term → the **unchanged** cursor-list path (no Memories call). [Proposal P1.2/P1.5]
6. **Status filter via structured attribute (O2).** When a search term is present, status is applied as a **structured attribute filter** passed to the Memories search (exact enum, indexed attribute) — *once the Memories REST search exposes attribute filtering (handoff)*. **Interim bridge** until the handoff lands: filter on the **hydrated authoritative** `TenantDetail.Status`. Either way the result is exact; never fuzzy BM25-text matching on the status word. [O2; Linchpin verdict C]
7. **`MemoriesIndexUpdate` emission (O1/O3).** On tenant lifecycle events (`TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`), Tenants publishes **one** curated `MemoriesIndexUpdate` per tenant — stable id `tenant:{tenantId}`, `Tenant=tenants-index`, `AggregateId=tenantId`, searchable text = Name (+ TenantId), attributes = `{ status }` — via `DaprClient.PublishEventAsync` to the Memories ingestion topic. It is **idempotent** (DAPR at-least-once) and uses **upsert** semantics so a rename overwrites the prior entry (no stale name, no DELETE-then-reingest hack). It must **not** call the `[Experimental("HXL001")]` `MemoriesClient.IngestAsync`/`CreateTenantAsync` (build-breaking under `TreatWarningsAsErrors`). [O1/O2/O3]
8. **No over-matching.** Because the `tenants-index` holds only curated per-tenant docs (not raw member/config/user events), a name search returns **one** hit per matching tenant and is never polluted by member/config event text. [Linchpin verdict E]
9. **Graceful degradation — search never blocks the list.** Memories unavailable (timeout, 503, `MemoriesRemoteException`) or `SearchResult.Degraded`/`UnavailableAxes` (syntactic axis down) → a non-blocking "search unavailable" state + fallback to the cursor list; no exception reaches the circuit. Match-set ids that hydrate to not-found/forbidden are dropped (degraded, not error). [Proposal P1.4]
10. **Eventual-consistency correct and documented.** A new tenant becomes searchable after ingestion (index lag accepted). Because rows hydrate fresh (AC2) and `MemoriesIndexUpdate` upserts (AC7), a renamed tenant is both displayed correctly **and** searchable by its new name once the update is indexed; residual index lag is documented as accepted eventual consistency. Empty results render the existing `FilteredEmpty` surface. [Proposal P1.4/P1.8]
11. **In-AppHost Memories (O4).** The Tenants AppHost starts the Memories Server inline and wires the BFF to it via Aspire service discovery (`Memories__BaseAddress` from `memoriesServer.GetEndpoint("http")`), **not** a hardcoded `:5000`. Memories reuses the existing `statestore`/`pubsub` Dapr components, runs its own FalkorDB, gets static `secretstore`/`llm` (dev `conversation.echo`) components, and unique Dapr ports. App-id `memories-server` is added to `pubsub` scopes and the ingestion topic + `SourceToTenantMap` are aligned. [O4; wiring plan in Dev Notes]
12. **No browser-side backend access; support-safe copy.** No Razor component gains `HttpClient`/`localStorage`/`sessionStorage`/token references (existing guard test stays green); all Memories access is server-side in the BFF. User-facing search/error copy contains no tokens, correlation ids, ETags, raw payloads, or stack traces. [project-context support-safety; Story 1.2 guard test]
13. **Tests prove it.** Gateway unit tests: search→match-set→`SourceUri` parse→hydrate; status filter; empty-search bypass; Memories-down fallback; not-found/forbidden match-set entry dropped; support-safety scrub. UI bUnit tests: search round-trip; `FilteredEmpty`/degraded surfaces; no-browser-backend guard. `MemoriesIndexUpdate` publisher tests: one curated event per lifecycle event, correct id/attributes, idempotent. All tiers pass; coverage gates unchanged (line >80% on the 5 package projects; 100% branch on isolation/auth files). [project-context Testing Rules]
14. **Memories handoff spec authored.** This story produces a written handoff (under `_bmad-output/` or the docs the team uses for submodule handoffs) specifying the `MemoriesIndexUpdate` contract, upsert-by-`(Tenant,AggregateId)` ingestion, attribute indexing + REST attribute filter, aggregate-id-in-hit mapping, and `tenants-index` registration. No `Hexalith.Memories/**` files are edited here. [O3; CLAUDE.md Submodule Policy]

## Tasks / Subtasks

> Build order: agree the `MemoriesIndexUpdate` contract shape (Task 2) → BFF read path + UI (Tasks 3–5, unit-testable against a mock) → publisher (Task 6) → AppHost (Task 7). The Memories submodule handoff (Task 9) runs in parallel in its own repo and gates E2E only.

- [x] **Task 1 — Package & submodule wiring** (AC: 1, 7)
  - [x] Add `ProjectReference`s from `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` to `Hexalith.Memories/src/Hexalith.Memories.Contracts/...` and `Hexalith.Memories/src/Hexalith.Memories.Client.Rest/...` (submodule **source** references like EventStore — not NuGet; do not touch `Directory.Packages.props`). Reference the `MemoriesIndexUpdate` contract from wherever the handoff places it (target: `Hexalith.Memories.Contracts`).
  - [x] Register the read client in the Tenants.UI host: `services.AddMemoriesClient(o => { o.Endpoint = <Memories:BaseAddress>; o.ApiToken = <HEXALITH_MEMORIES_API_TOKEN>; })`. Confirm the build is green under `TreatWarningsAsErrors`; ensure **no** `[Experimental]` Memories API (`IngestAsync`/`CreateTenantAsync`) is referenced from product code.
  - [x] Update `_bmad-output/project-context.md` consumed-submodule list to include `Hexalith.Memories` (and `Hexalith.PolymorphicSerializations` if pulled transitively). Submodule Policy: root-level only; never `--init --recursive`.
- [x] **Task 2 — Agree the `MemoriesIndexUpdate` contract** (AC: 7, 14)
  - [x] Define the contract shape (target home: `Hexalith.Memories.Contracts`, Memories-owned): an upsert/delete index-update carrying `Tenant` (index name), `AggregateId`, searchable text, and `attributes` (string key→value, domain-defined; tenants supply `status`), plus a stable source id (`tenant:{id}`) and an operation (Upsert/Delete). Record it in the handoff spec (Task 9); Tenants codes against this agreed shape.
  - [x] Confirm the CloudEvent mapping: `cloudevent.id = "tenant:{tenantId}"` so `ScoredResult.SourceUri` returns it verbatim for AC3 recovery; `cloudevent.source` matches the `SourceToTenantMap` prefix routing to `tenants-index`.
- [x] **Task 3 — BFF search match-set + hydration** (AC: 1, 2, 3, 4, 8)
  - [x] In `TenantQueryGateway.ListTenantsAsync`, branch on non-empty `request.Search`: call `MemoriesClient.SearchAsync(new SearchRequest(TenantId: "tenants-index", Axis: "syntactic", Query: request.Search, MaxResults: <page size>), ct)`.
  - [x] Parse `SearchResult.Results[*].SourceUri` → `tenant:{id}` → ordered tenant ids (preserve BM25 score order; dedupe defensively).
  - [x] Hydrate each id via the existing detail read (`CreateDetailRequest` → `GetTenantQuery`); build `TenantListRow`s exactly as `EnrichRowsAsync` does (member/owner counts from `TenantDetail`). Reuse/extend that helper; do not duplicate.
  - [x] Map to `TenantListSnapshot.Ready`; empty match-set → the surface that drives `FilteredEmpty`. Document the BM25-vs-cursor ordering difference in a comment. Stop dropping `Search`/`Status` in `CreateListRequest`; leave the non-search cursor request unchanged.
- [x] **Task 4 — Wire the term through the UI to the server** (AC: 4, 5)
  - [x] `TenantsWorkspace.OnSearchChanged` → server `LoadAsync` (debounce per existing input conventions). Empty/whitespace → cursor-list path (no Memories call).
  - [x] Remove the client-side **search** `.Where` from `ApplyVisibleRows()` (~277–307); keep client-side **sort** of the visible page. Preserve `data-testid` selectors (`tenants-list-search`, `-refresh`, `-reset`, `-truth-state`) and the six list-state surfaces.
  - [x] Circuit-safe: any `EventCallback`/`StateHasChanged` after a `ConfigureAwait(false)` await must be marshalled via `InvokeAsync(...)` (known circuit-teardown trap). Keep `ConfigureAwait(false)` on awaits. Keep the `?search=` deep-link round-trip.
- [x] **Task 5 — Status filter + degradation + freshness** (AC: 6, 9, 10)
  - [x] Status filter (O2): pass a **structured attribute filter** to `SearchAsync` once the Memories REST search exposes it (handoff). **Interim** until then: filter the hydrated authoritative `TenantDetail.Status`. Document which path is active.
  - [x] Degradation: catch Memories unavailability + `SearchResult.Degraded`/`UnavailableAxes` (syntactic) → non-blocking "search unavailable" snapshot + cursor-list fallback; never let an exception reach the circuit. Drop match-set ids that hydrate 404/403/503 (degraded, not error — mirror `EnrichRowsAsync`'s catch).
  - [x] Freshness: hydrated rows resolve via the existing `ResolveFreshness` (ETag/ProjectionVersion → `Current`; absent → `Unknown`). Never derive freshness from Memories.
- [x] **Task 6 — `MemoriesIndexUpdate` publisher (curated per-tenant)** (AC: 7, 8, 10)
  - [x] Add a projection/handler on tenant lifecycle events (`IEventStoreDomainEventHandler<TenantCreated|TenantUpdated|TenantDisabled|TenantEnabled>`) that publishes **one** `MemoriesIndexUpdate` per event via `DaprClient.PublishEventAsync("pubsub", <memories ingestion topic>, evt, metadata, ct)`: `Tenant=tenants-index`, `AggregateId=tenantId`, text = Name (+ TenantId), attributes `{ status }`, `cloudevent.id = "tenant:{tenantId}"`, Upsert op. Disable/Enable update the `status` attribute.
  - [x] Idempotent (DAPR at-least-once): rely on the platform `MessageId` dedup in `EventStoreDomainEventProcessor`; republishing the same tenant state is harmless (upsert). Do **not** use `MemoriesClient.IngestAsync`.
  - [x] **Host placement:** put the publisher in a host/consumer service, **never** in `Hexalith.Tenants.Client` (that package is deliberately broker-free; `DaprClient` is a broker dependency). Verify how the read-model projection is currently hosted (the host calls `MapSubscribeHandler()` but not `MapEventStoreDomainEvents()`); co-locate if the host already consumes `tenants.events`, else a minimal `samples/Hexalith.Tenants.Sample`-style consumer. **Coverage note:** if placed in a **package** project (Client/Server) it falls inside the >80% line gate — prefer host/consumer.
- [x] **Task 7 — In-AppHost Memories wiring (O4)** (AC: 11)
  - [x] `Hexalith.Tenants.AppHost.csproj`: add `ProjectReference` to `Hexalith.Memories/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` (the generator emits `Projects.Hexalith_Memories_Server`; the `*.Aspire` project **does not exist**). Verify the relative/MSBuild-prop path; do a clean combined `dotnet restore` and watch for `Directory.Packages.props` conflicts (Memories has its own).
  - [x] `AddProject<Projects.Hexalith_Memories_Server>("memories-server", launchProfileName: "http")` with a Dapr sidecar on **unique** ports (`3502`/`50002`; EventStore uses `3501`), referencing the shared `statestore`+`pubsub` Dapr components (from `eventStoreResources`) plus **static committed** `secretstore.yaml` + `llm.yaml` (dev `conversation.echo`) + a dev `secrets.json` under `src/Hexalith.Tenants.AppHost/deploy/dapr/`. Env: `ConnectionStrings__redis`, `ConnectionStrings__falkordb`, `MEMORIES_EVENTSTORE_TOPIC`. `WaitFor` all of redis, falkordb, secretstore, llm.
  - [x] Run a **dedicated FalkorDB** container (`memories-falkordb`, internal 6379→auto host port) and (if vector-index isolation wanted) a dedicated `memories-redis` (volume `hexalith-tenants-memories-redis-data`); otherwise reuse the `dapr init` Redis via the shared components (no real 6379 clash — Aspire auto-allocates host ports). Embeddings need **no** AppHost resource (HTTP `EmbeddingClient`, per-tenant config) — do **not** add an Ollama container expecting auto-embed.
  - [x] BFF endpoint: `tenantsUI.WithReference(memoriesServer).WaitFor(memoriesServer).WithEnvironment("Memories__BaseAddress", memoriesServer.GetEndpoint("http"))`; in `Tenants.UI/Program.cs` read `Memories:BaseAddress` and call `AddMemoriesClient(o => o.Endpoint = ...)` (UI needs a `ProjectReference` to `Hexalith.Memories.Client.Rest`; relay per-user token like the EventStore/Tenants clients if auth is enabled). Verify Memories.Server exposes `http` (likely no `https`).
  - [x] Pub/sub: add app-id `memories-server` to the `pubsub` component `scopes`; **align** the ingestion topic (`MEMORIES_EVENTSTORE_TOPIC` = `memories-events` on both sides) and document the `SourceToTenantMap` entry (`hexalith-tenants` → `tenants-index`) for the tenants source in the handoff — otherwise ingestion silently no-ops. **Live end-to-end smoke-test DEFERRED** per the owner-approved "wire as code/config, no live E2E" decision (2026-06-21) and because real ingestion is gated on the Memories server handoff; the AppHost builds and the app model is correct. Pre-run for a future live boot: rebuild all new Memories child projects in **Debug** (`--no-build` stale-binary trap).
- [x] **Task 8 — Tests** (AC: 13, and all)
  - [x] Gateway unit tests (`tests/Hexalith.Tenants.UI.Tests/Services/Gateways/...`; xUnit v3 + Shouldly + NSubstitute; `{Class}Tests.cs`): substitute `MemoriesClient` (concrete, `virtual SearchAsync`); reuse `CapturingGatewayClient`/`StaticResponseHandler` for hydration. Cover: search→`SourceUri` parse→ordered hydrate; status filter; empty-search bypass (no Memories call); Memories-down fallback; match-set id 404/403 dropped+degraded; support-safety (`snapshot.ToString()` excludes tokens/correlation/etag/raw payload).
  - [x] UI bUnit tests (data-testid based): typing a term triggers a server reload; empty results render `tenants-list-filtered-empty`; degraded search renders a non-blocking "search unavailable" affordance; the no-browser-backend guard test still passes.
  - [x] `MemoriesIndexUpdate` publisher tests: one curated event per lifecycle event with correct id/`AggregateId`/attributes; idempotent re-delivery; status attribute updates on disable/enable.
  - [x] Run Tier 1 + the UI.Tests project; confirm coverage gates unaffected (UI host excluded from the >80% line gate; no isolation/auth branch logic added).
- [x] **Task 9 — Memories handoff spec + Tenants artifact updates** (AC: 6, 14)
  - [x] Author the **Memories handoff spec** (no `Hexalith.Memories/**` edits): `MemoriesIndexUpdate` contract; upsert-by-`(Tenant,AggregateId)` ingestion (overwrite, not append-dedup); attribute indexing; **REST search attribute filter** (`SearchRequest`/`BuildSearchPath`/`GET /api/search` currently expose none — required for AC6 structured status filter); **map aggregate id into `ScoredResult`** or guarantee `SourceUri="tenant:{id}"` for AC3; register the `tenants-index` tenant + `SourceToTenantMap` prefix. Note it supersedes the proposal's P1.7 paging handoff framing.
  - [x] PRD FR-1: add testable consequences (fields = Name + TenantId; matching = Memories syntactic/BM25; structured `status` filter; eventual-consistency + degradation caveats).
  - [x] Architecture: add the "Tenant search (Memories-backed, index-only)" note (match-set from Memories; rows from the ETag-fresh path; no new EventStore endpoint; `MemoriesIndexUpdate` as the cross-domain index-maintenance pattern; the FC-`IQueryService`-vs-REST/ETag position dissolved by search bypassing it).

## Dev Notes

### What this story is (and is NOT)
- **IS:** complete the *already-designed* search on the *already-shipped* list page, server-side, via Memories as a search index; rows hydrated fresh; Tenants emits a curated `MemoriesIndexUpdate` per tenant; the Tenants AppHost starts Memories.
- **IS NOT:** a new list/detail page (shipped in 1.2/1.3); a new EventStore query endpoint (consume-only backend — no new `/api` route, no server-side filter on `ListTenantsQuery`); a Tenants raw-event ingestion adapter (O3 — Memories owns ingestion); the FrontComposer extraction (Phase 2). Do not reinvent the list page or detail read path — extend them.

### The data path (search-as-index-only) — the core idea
```
search term ──► MemoriesClient.SearchAsync(tenants-index, syntactic[, status attr filter])
                       │
                       ▼
        SearchResult.Results[*].SourceUri == "tenant:{id}"   (one curated doc per tenant; BM25-ordered)
                       │  parse → tenantId   (NOT ContentSnippet, NOT subject)
                       ▼
        for each id: GetTenantQuery (existing ETag-fresh detail read)  ──► TenantListRow
                       ▼
                 TenantListSnapshot.Ready

Index maintenance (separate, async):
  TenantCreated/Updated/Disabled/Enabled ──► Tenants publishes MemoriesIndexUpdate
     (id=tenant:{id}, Tenant=tenants-index, AggregateId=id, text=name+id, attrs={status}, UPSERT)
                       ──► Memories ingests & upserts the tenants-index (Memories owns this)
```
Memories decides *which* tenants; the existing read path decides *what each row shows* (so a stale index never shows wrong data). Upsert keeps the index name fresh on rename; a dedicated `tenants-index` (curated docs only) means no over-matching.

### Linchpin facts (code-verified — do not relitigate)
- **Tenant-id recovery is ONLY reliable via `ScoredResult.SourceUri`** (returned verbatim from the publisher's `cloudevent.id`). `cloudevent.subject` is **not** mapped into the hit; `ContentSnippet` is truncated to **200 chars** (unsafe to JSON-parse). ⇒ publisher MUST set `cloudevent.id = "tenant:{id}"` (AC3).
- **Memories has DELETE but NO update/upsert primitive today** and dedups raw event ingestion on `cloudevent.id` — re-posting the same id is **silently dropped** (never re-indexed). ⇒ O1 "stable id" alone would give a permanently stale name. The `MemoriesIndexUpdate` **upsert** (handoff) is what makes stable-id + fresh-name work; without it, the fallback is DELETE-then-reingest on change.
- **Raw event ingestion over-matches:** the BM25 `content` field is the raw event JSON, so a tenant name inside a member-add/config-set event would match. The dedicated `tenants-index` with **only** curated `MemoriesIndexUpdate` docs avoids this (AC8).
- **The REST search client exposes no structured filter** (`SearchRequest`/`BuildSearchPath` carry only `tenantId, axis, query, caseId, maxResults`; `cloudEventSubject`/`sourceType` exist server-side but are unreachable). ⇒ O2 structured status filter **requires** the Memories REST handoff; interim = filter hydrated status.
- `SearchAsync` is **non-experimental** (safe under `TreatWarningsAsErrors`); `IngestAsync`/`CreateTenantAsync` are `[Experimental("HXL001")]` — never call them from product code.

### O4 — in-AppHost wiring (verified plan)
- **Composition:** inline in `src/Hexalith.Tenants.AppHost/Program.cs`, scoped to `Hexalith.Memories.Server` only (CLAUDE.md blesses the AppHost as repo-specific; no Memories submodule edit for AppHost wiring). MCP project excluded. Leave a `// TODO: extract to a reusable Memories Aspire extension if a 2nd consumer appears`.
- **ProjectReference:** to `Hexalith.Memories.Server.csproj` → generated `Projects.Hexalith_Memories_Server` (no hand-written `IProjectMetadata` needed). The nonexistent `Hexalith.Memories.Aspire` from earlier notes is wrong.
- **Redis/Dapr:** reuse the shared `statestore`/`pubsub` Dapr components + `dapr init` Redis (no real 6379 clash — host ports auto-allocate); **dedicated FalkorDB** is mandatory; **static** `secretstore.yaml`+`llm.yaml` (dev `conversation.echo`) committed under `deploy/dapr/`. Dapr ports `3502`/`50002`.
- **Endpoint:** `Memories__BaseAddress` from `memoriesServer.GetEndpoint("http")` + `.WithReference`+`.WaitFor`; UI reads `Memories:BaseAddress`. The `:5000` literal is wrong (Aspire auto-allocates). Use `http` (verify Memories.Server has no `https`).
- **Pub/sub:** add `memories-server` to `pubsub` scopes; align topic + `SourceToTenantMap`, else silent no-op (same class as the actor-routing trap).
- **MUST-VERIFY in code:** (i) submodule relative/MSBuild-prop path from the AppHost csproj; (ii) `https` vs `http` on Memories.Server; (iii) `SourceToTenantMap` env-var key shape; (iv) the chosen Dapr path actually appends `memories-server` to `pubsub` scopes; (v) clean combined restore (Memories has its own `Directory.Packages.props`).
- **Risks:** stale-binary `--no-build` (rebuild the new Memories child projects in Debug); Dapr port uniqueness; FalkorDB first-pull `WaitFor` timeout (pre-pull); transitive-dep weight/version conflicts. Embeddings: dev `conversation.echo` covers the NL path; **vector embeddings need a real provider+secret** and are **out of scope** here (E2E vector search may not work locally without provisioning — call this out).

### Key files (current state — verified) and what changes
| File | Current state | This story |
|---|---|---|
| `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` | search box `tenants-list-search`→`_search`; `OnSearchChanged`→`ApplyVisibleRows()` (client filter, ~263–307). `LoadAsync` (~197–217) already builds `TenantListRequest` WITH `Search`+`Status`. Six states incl. `FilteredEmpty`. | `OnSearchChanged`→server `LoadAsync`; remove client search `.Where`; keep sort; circuit-safe callbacks. |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` | `ListTenantsAsync` ~378–429 → `CreateListRequest` ~431–446 **drops** `Search`/`Status`. `EnrichRowsAsync`/`LoadTenantDetailAsync` ~505–545 hydrate via `GetTenantQuery` (reuse this). `ResolveFreshness` ~569–584. | Add Memories search branch + `SourceUri`→`tenant:{id}`→hydrate; stop dropping `Search`. |
| `src/Hexalith.Tenants.UI/State/TenantList/TenantListRequest.cs` | already has `Search`,`Status`,`SortColumn`,`SortDescending`,`ETag`. | No change. |
| `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs` / `TenantSummary.cs` / `TenantDetail.cs` | bare marker; `TenantSummary(TenantId,Name,Status)`; `TenantDetail(...,Members,Configuration,...)`. | No change (read shapes reused; backend stays consume-only). |
| Events `TenantCreated`(…,**Name**,…) / `TenantUpdated`(…,**Name**,…) / `TenantDisabled` / `TenantEnabled` | `TenantUpdated` carries renamed Name; **no `TenantDeleted`** (soft-delete). | Publisher (Task 6) subscribes to these four. |
| `src/Hexalith.Tenants.AppHost/Program.cs` + `DaprComponents/*.yaml` | 6 children + Keycloak; `AddEventStoreDomainModule`; `pubsub` scopes `[eventstore, sample]`. | Add Memories Server + FalkorDB + components; scope `memories-server`; wire UI endpoint. |

### Memories API — exact surface (verified)
- `MemoriesClient.SearchAsync(SearchRequest, CancellationToken) → SearchResult` (non-experimental). `SearchRequest(string TenantId, string Axis, string? Query, string? CaseId=null, int MaxResults=10, bool Explain=false, int? TokenBudget=null)` — use `TenantId="tenants-index"`, `Axis="syntactic"`. `SearchResult{ IReadOnlyList<ScoredResult> Results; bool Degraded; IReadOnlyList<string>? UnavailableAxes; long TotalCount; … }`. `ScoredResult{ string SourceUri; double Score; string ContentSnippet; SourceType SourceType; string MemoryUnitId; … }` — parse `SourceUri`.
- DI: `AddMemoriesClient(Action<MemoriesClientOptions>?)`; `MemoriesClientOptions{ Uri? Endpoint; string? ApiToken; }`; token env `HEXALITH_MEMORIES_API_TOKEN`; 30s timeout; ns `Hexalith.Memories.Client.Rest`, contracts `Hexalith.Memories.Contracts.V1`.
- **Never** call `IngestAsync`/`CreateTenantAsync`/`CreateCaseAsync`/telemetry/handler/consistency methods — `[Experimental(HXL001/HXL002)]`.
- Ingestion (pub/sub, Memories side): `AddMemoriesEventStoreIntegration` binds `EventStoreIntegration:Routing`→`TenantEventRoutingOptions{ PubSubName="pubsub"; Topic; SourceToTenantMap (longest-prefix, case-insensitive); CaseNameTemplate="events:{aggregateType}"; … }`; subscribes `POST /events/ingest` on the configured topic.

### Known traps (honor these)
- **Blazor circuit teardown:** `EventCallback`/parent `StateHasChanged` after `ConfigureAwait(false)` throws off-Dispatcher → circuit torn down. Marshal with `InvokeAsync(...)`. bUnit runs on the dispatcher and won't catch it.
- **Projection-actor routing:** BFF `SubmitQueryRequest` must set `ProjectionActorType = TenantProjectionRouting.ActorTypeName` (silent prod break if omitted; mocks stay green). The detail read path already does this — reuse `CreateDetailRequest`/`GetTenantQuery`, don't hand-roll.
- **D6 freshness:** never derive `Current` from `ServedAt`/timing — only ETag/ProjectionVersion; absent ⇒ `Unknown`. Reuse `ResolveFreshness`.
- **Stale `bin/Debug`:** Aspire runs children `--no-build`; rebuild all child host projects (incl. the new Memories ones) in Debug before running. `.slnx` is restore/build-only; never solution-level `dotnet test`.
- **Submodules:** root-level only; never `--init --recursive`; never edit `Hexalith.Memories/**` or `Hexalith.FrontComposer/**` here (handoffs only).
- **Records/style:** commands/events/rejections are plain `public record` (no `sealed`, no XML docs); query response DTOs are `sealed record` + `<summary>`. Enums carry `Unknown=0` + string converter. No file headers. `ConfigureAwait(false)` on every host/Client await.

### Testing standards (verified patterns to mirror)
- xUnit v3 + Shouldly (never raw `Assert.*`) + NSubstitute; `{Class}Tests.cs`; BDD method names. Gateway tests use `CapturingGatewayClient` + `StaticResponseHandler`; HTTP-client tests use `CapturingHandler`. `MemoriesClient` is concrete with `virtual SearchAsync` → substitute or wrap behind a seam; never hit a live Memories in unit tests.
- bUnit is data-testid based; assert `tenants-list-filtered-empty`, the search affordance, role attributes; the guard test scans `src/Hexalith.Tenants.UI/Components/**/*.razor` for `HttpClient`/`localStorage`/`sessionStorage`/`access_token` — keep all Memories access in the gateway.
- `DomainUiFluentConformanceTests` caps inline layout/spacing + the `<div>`/`<span>` budget; new markup uses FluentStack/FluentGrid/FluentLabel or a `/* fc-css-exception: reason */` marker. Prefer reusing existing list markup.
- Support-safety: assert user-facing snapshots/copy exclude tokens/correlation-ids/etags/raw payloads/stack traces (see `Get_tenant_live_problem_details_path_maps_...`).

### Project Structure Notes
- Tenants-repo changes: `src/Hexalith.Tenants.UI/**` (gateway + workspace + DI), the `MemoriesIndexUpdate` publisher (host/consumer, **not** the broker-free `Client` package), `src/Hexalith.Tenants.AppHost/**` (+ `deploy/dapr/` static components), `tests/Hexalith.Tenants.UI.Tests/**`, plus this-repo PRD/architecture + `project-context.md` + the handoff spec. **No edits** to `Hexalith.Memories/**` or `Hexalith.FrontComposer/**`.
- Coverage: UI host excluded from the >80% line gate (`scripts/validate-coverage.py` `PACKAGE_LINE_SCOPE` = Contracts/Client/Server/Testing). The gateway/UI/publisher changes sit outside the gate **if** the publisher stays in a host/consumer; placing it in a package project pulls it inside the gate. No isolation/auth branch logic added. Author AC13 tests regardless.
- **Cross-repo dependency:** end-to-end search is **blocked** until the Memories handoff (Task 9) lands its `MemoriesIndexUpdate` ingestion + attribute filter + hit identity + `tenants-index`. Unit tests here mock Memories and are not blocked. Consider tracking the Memories handoff as its own Memories-side sprint item.

### References
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md#5] (Phase 1 P1.1–P1.8, §8 defaults, §3.2 data-path)
- [Source: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs] `ListTenantsAsync` ~378–429; `CreateListRequest` ~431–446; `EnrichRowsAsync`/`LoadTenantDetailAsync` ~505–545; `ResolveFreshness` ~569–584
- [Source: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor] search box ~30–40; `LoadAsync` ~197–217; `OnSearchChanged`/`ApplyVisibleRows` ~263–307
- [Source: src/Hexalith.Tenants.UI/State/TenantList/TenantListRequest.cs] / [Contracts/Queries/{ListTenantsQuery,TenantSummary,TenantDetail}.cs]
- [Source: Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:159] `SearchAsync`; `BuildSearchPath` ~540–579 (no structured filter); `SearchRequest.cs`; `IngestAsync` ~404 `[Experimental]`
- [Source: Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/{SearchResult,ScoredResult}.cs] (`SourceUri` returned verbatim; `ContentSnippet` ≤200 chars)
- [Source: Hexalith.Memories/src/Hexalith.Memories.EventStore/{EventStoreIntegrationServiceCollectionExtensions.cs:33,TenantEventRoutingOptions.cs,EventIngestionController.cs}] ingestion path; `CloudEventToIngestionInputMapper.cs:78` (SourceUri=cloudevent.id); `DedupKeyBuilder.cs` (no upsert)
- [Source: Hexalith.Memories/src/Hexalith.Memories.AppHost/Program.cs] resource graph (Redis Stack, FalkorDB, statestore/pubsub/secretstore/llm, Memories Server `AddProject<Projects.Hexalith_Memories_Server>` launch profile `http`, ports 3500/50001)
- [Source: src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs] + [Registration/TenantServiceCollectionExtensions.cs] topic `tenants.events`; [samples/Hexalith.Tenants.Sample/Program.cs] consumer template; [Hexalith.EventStore/.../EventPublisher.cs] `PublishEventAsync` + CloudEvents metadata
- [Source: src/Hexalith.Tenants.AppHost/Program.cs] + [DaprComponents/pubsub.yaml] (`pubsub` scopes `[eventstore, sample]`)
- [Source: tests/Hexalith.Tenants.UI.Tests/{Services/Gateways/TenantQueryGatewayTests.cs,Services/Gateways/TenantsQueryApiClientTests.cs,Components/TenantListSurfaceTests.cs,DomainUiFluentConformanceTests.cs}]; [scripts/validate-coverage.py:40–54]
- [Source: _bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md] (D6 ETag/freshness); [1-2-tenant-list-triage.md] (list page + stubbed search follow-up)
- [Source: _bmad-output/project-context.md] (contract/eventing/identity/testing/quality rules)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) via the BMAD dev-story workflow.

### Debug Log References

- **Memories submodule build guard.** `Hexalith.Memories/Directory.Build.props` `CheckSubmodules` errored ("Git submodule 'Hexalith.Tenants' is missing") when any Memories project is referenced from the Tenants build, because it required a `Hexalith.Tenants` submodule that does not exist when Memories is nested *inside* the Tenants repo. Fixed surgically: the `Hexalith.Tenants` requirement is dropped only when `../Hexalith.Tenants.slnx` exists (the "hosted in Tenants repo" layout); standalone-Memories is unaffected.
- **`TenantStatus`/`TenantSummary` namespace collisions.** `Hexalith.Memories.Contracts.V1` also defines `TenantStatus`/`TenantSummary`; importing it wholesale collided with the tenant domain contracts. Resolved with targeted type aliases (`MemoriesScoredResult`, `MemoriesSearchResult`, `MemoriesSourceType`, `MemoriesErrorResponse`, `SearchIndexEntryChanged`) instead of blanket `using`.
- **Pre-existing build break (UNRELATED to this story), fixed to unblock the regression suite.** `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs` did not compile on the baseline commit (`d4aff69`): commit `04a321b` added an `IConfiguration` ctor param to `TenantBootstrapHostedService` (for JWT acquisition) without updating the test. Added an empty `new ConfigurationBuilder().Build()` arg at the 8 call sites (safe — the service reads config via null-tolerant indexers); the 10 bootstrap tests pass.

### Completion Notes List

- **Owner decisions applied (2026-06-21, in conversation):** (1) the new contract is named **`SearchIndexEntryChanged`** (generic/domain-agnostic, in `Hexalith.Memories.Contracts.V1`) with a `SearchIndexEntryRemoved` sibling, **superseding** the story's `MemoriesIndexUpdate` naming and its handoff-only "do not edit Memories" split — the owner explicitly authorized adding the contract to `Hexalith.Memories.Contracts` and "modify submodules if needed"; (2) Task 7 is **code/config only, no live E2E**.
- **Search-as-index-only (AC1-3,8):** `TenantQueryGateway.SearchTenantsAsync` calls `MemoriesClient.SearchAsync(tenants-index, syntactic)`, recovers ids **only** from `ScoredResult.SourceUri` (`tenant:{id}`), and hydrates each via the existing `GetTenantAsync` ETag-fresh path (reused so 404/403/503 map to non-throwing snapshots + freshness from `ResolveFreshness`, never from Memories).
- **UI (AC4-5):** `OnSearchChanged`/`OnStatusFilterChanged` round-trip to the server `LoadAsync`; the client-side search `.Where` was removed (status stays a page-local filter for the cursor list); debounce uses `FluentTextInput` built-in `Immediate`/`ImmediateDelay` (fires on the Dispatcher → no circuit-teardown trap, no manual `Task.Delay`/`InvokeAsync`). `?search=` deep-link preserved.
- **Degradation (AC9-10):** Memories unavailable / `Degraded` / `syntactic` axis down → non-blocking fallback to the cursor list with a support-safe notice; empty match-set → `FilteredEmpty`; exceptions filtered (`MemoriesRemoteException`/`HttpRequestException`/`InvalidOperationException`/timeout) so none reach the circuit (CA1031-clean via `when` filter).
- **Publisher (AC7, Task 6):** placed in the **Sample consumer** (`samples/Hexalith.Tenants.Sample`), the canonical tenant-events consumer — **not** the broker-free Client (story rule). The agent-suggested "put it in Client" placement was rejected for that reason. Reads the authoritative current Name/Status from the local `ITenantProjectionStore` (event-derived fallback) and emits one curated `SearchIndexEntryChanged` with `cloudevent.id = "tenant:{id}"`. Never calls the `[Experimental]` `IngestAsync`.
- **AppHost (AC11, Task 7 — code/config):** Memories.Server is added via a hand-written `HexalithMemoriesServer : IProjectMetadata` with `SuppressBuild = true` (the AppHost's cross-repo convention), so the AppHost build **never compiles Memories.Server** — this side-steps the `NSubstitute`/`StackExchange.Redis` combined-restore version conflicts the investigation flagged for a `ProjectReference`. Wired: dedicated FalkorDB, Dapr sidecar on unique HTTP port 3502, shared statestore/pubsub + static dev secretstore/llm components, `memories-server` added to `pubsub` scopes, BFF `Memories__BaseAddress` from `GetEndpoint("http")`. Live boot/smoke-test deferred per the approved scope.
- **Tests (AC12-13):** gateway search tests (10) + UI bUnit search tests (round-trip / filtered-empty / degraded) + publisher tests (7, incl. idempotent re-delivery & support-safety). Existing `Search_filter_and_sort_preserve_safety_markers` rewritten for server search. Suites green: Contracts 106, Client 47, Testing 181, UI **744**, Sample **39**, Server bootstrap 10. **Coverage gate unaffected** — no code added to the 5 gated package projects (Contracts/Client/Server/Testing/Aspire) or to isolation/auth files; the gateway/UI live in the UI host (excluded) and the publisher in the Sample (not gated).
- **End-to-end search is BLOCKED on the Memories server handoff** (`_bmad-output/planning-artifacts/memories-search-index-handoff-2026-06-21.md`): upsert-by-`(TenantId,AggregateId)` ingestion, attribute indexing + REST attribute filter, `tenants-index`/`SourceToTenantMap` registration. Unit tests here mock Memories and are not blocked. The status filter uses the **interim** hydrated-status path until the REST attribute filter lands (AC6).
- The `Hexalith.EventStore` submodule shows as modified only from build artifacts (obj/bin) — **no EventStore source was changed**; its gitlink must not be committed.

### File List

**`Hexalith.Memories` submodule (owner-approved edits):**
- `Hexalith.Memories/Directory.Build.props` — CheckSubmodules guard tolerant of the nested-in-Tenants layout
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/SearchIndexEntryChanged.cs` — NEW contract (upsert)
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/SearchIndexEntryRemoved.cs` — NEW contract (delete)
- `Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — register the two contracts in the source-gen JSON context

**Tenants repo — product/config:**
- `Directory.Build.props` — add `$(HexalithMemoriesRoot)` auto-detection
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` — ProjectReferences to Memories.Contracts + Memories.Client.Rest
- `src/Hexalith.Tenants.UI/Program.cs` — `AddMemoriesClient` (reads `Memories:BaseAddress`)
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` — Memories search branch + hydration + degradation + status filter
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` — server-side debounced search; removed client search `.Where`
- `samples/Hexalith.Tenants.Sample/Handlers/MemoriesSearchIndexEventPublisher.cs` — NEW curated `SearchIndexEntryChanged` publisher
- `samples/Hexalith.Tenants.Sample/Program.cs` — register the publisher on the 4 lifecycle events
- `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` — ProjectReference to Memories.Contracts
- `src/Hexalith.Tenants.AppHost/Program.cs` — inline Memories Server + FalkorDB + Dapr components + BFF endpoint
- `src/Hexalith.Tenants.AppHost/HexalithMemoriesServer.cs` — NEW cross-repo project metadata (SuppressBuild)
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` — add `memories-server` scope
- `src/Hexalith.Tenants.AppHost/DaprComponents/secretstore.memories.yaml` — NEW dev secret store component
- `src/Hexalith.Tenants.AppHost/DaprComponents/llm.memories.yaml` — NEW dev echo conversation component
- `src/Hexalith.Tenants.AppHost/DaprComponents/secrets.json` — NEW dev secrets file

**Tenants repo — tests:**
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` — search-path gateway tests + `MemoriesClient` seam in `CreateGateway`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` — server-search bUnit tests + `ChangeSearchAsync`
- `samples/Hexalith.Tenants.Sample.Tests/Handlers/MemoriesSearchIndexEventPublisherTests.cs` — NEW publisher tests
- `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs` — pre-existing compile-break fix (unrelated to this story)

**Tenants repo — docs/planning:**
- `_bmad-output/planning-artifacts/memories-search-index-handoff-2026-06-21.md` — NEW Memories server handoff spec
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md` — FR-1 search consequences
- `_bmad-output/planning-artifacts/architecture.md` — "Tenant search (Memories-backed, index-only)" note
- `_bmad-output/project-context.md` — consumed-submodule list (+ Hexalith.Memories / PolymorphicSerializations)

### Change Log

| Date | Change |
|---|---|
| 2026-06-21 | Implemented Phase 1 Memories-backed cross-set tenant search: generic `SearchIndexEntryChanged`/`SearchIndexEntryRemoved` contracts (Memories.Contracts), BFF search read path (search-as-index-only), debounced server-side UI search, curated per-tenant publisher (Sample consumer), in-AppHost Memories wiring (code/config), tests (gateway/UI/publisher), and the Memories server handoff spec + PRD/architecture/project-context updates. Adopted owner decisions: contract named `SearchIndexEntryChanged` and added directly to `Hexalith.Memories.Contracts`; Task 7 code/config only (no live E2E). Fixed a pre-existing unrelated `Server.Tests` compile break to keep the full build/regression suite green. |
