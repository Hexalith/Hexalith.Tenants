# Handoff: Memories Search-Index Ingestion for Tenant Search (2026-06-21)

**From:** Hexalith.Tenants (story `cc-2026-06-21-memories-backed-tenant-search`, Phase 1)
**To:** Hexalith.Memories
**Status:** Tenants side shipped; Memories **server** side required to enable end-to-end search.
**Supersedes:** the P1.7 *paging* handoff framing in `sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md` (search is now *index-only*, not paged read-through).

---

## 1. What this enables

Cross-set tenant search on the Tenants list page: the BFF asks Memories *which* tenants match a term
(`MemoriesClient.SearchAsync` against a dedicated `tenants-index`), then hydrates each row from the
ETag-fresh tenant detail read. Memories decides **which** tenants appear; it never supplies **what each
row shows**. So a stale index can never render wrong data — only momentarily wrong membership of the
result set, which self-heals as the index catches up.

## 2. What Tenants already ships (no further Tenants work needed)

- **Two contract records added to `Hexalith.Memories.Contracts/V1`** (already committed in the submodule,
  with explicit approval): `SearchIndexEntryChanged` (upsert) and `SearchIndexEntryRemoved` (delete),
  registered in `MemoriesJsonSourceGenerationContext`. Domain-agnostic by design (any producer can feed
  any index). Shape:
  - `SearchIndexEntryChanged { string TenantId; string AggregateId; string Text; Dictionary<string,string> Attributes; string? CorrelationId; string? CausationId; }`
  - `SearchIndexEntryRemoved  { string TenantId; string AggregateId; string? CorrelationId; string? CausationId; }`
  - Here `TenantId` = the **index name** (Memories tenant partition), `AggregateId` = the source aggregate
    id within that index. Tenants emits only `SearchIndexEntryChanged` (tenants are soft-deleted, never
    hard-removed); `SearchIndexEntryRemoved` exists for producers that hard-delete.
- **A publisher** (`samples/Hexalith.Tenants.Sample/Handlers/MemoriesSearchIndexEventPublisher.cs`) that,
  on `TenantCreated` / `TenantUpdated` / `TenantDisabled` / `TenantEnabled`, publishes **one curated**
  `SearchIndexEntryChanged` per tenant via `DaprClient.PublishEventAsync`:
  - pub/sub component **`pubsub`**, topic **`memories-events`**
  - CloudEvent metadata: `cloudevent.id = "tenant:{tenantId}"`, `cloudevent.type = "SearchIndexEntryChanged"`,
    `cloudevent.source = "hexalith-tenants"`
  - payload `data`: `{ TenantId: "tenants-index", AggregateId: "{tenantId}", Text: "{name} {tenantId}", Attributes: { "status": "Active" | "Disabled" } }`
  - idempotent (relies on the platform `MessageId` dedup before dispatch) and upsert-shaped (re-publishing
    the same state is harmless).
- **The BFF search read path** (`TenantQueryGateway.SearchTenantsAsync`): `SearchAsync(TenantId:"tenants-index",
  Axis:"syntactic", Query: term, MaxResults: pageSize)`, recovers tenant ids **only** from
  `ScoredResult.SourceUri` (shape `tenant:{id}`), hydrates via the detail path, applies the status filter on
  the hydrated authoritative status (interim — see §4), and degrades to the cursor list if Memories is down.

## 3. What Memories MUST implement (the handoff)

> None of these are done by Tenants. Without them, ingestion of `SearchIndexEntryChanged` either no-ops or
> over-indexes, and the structured status filter (AC6) cannot be honored.

1. **Register the `tenants-index` tenant + source routing.** Add a `SourceToTenantMap` entry mapping the
   CloudEvent source prefix **`hexalith-tenants`** → tenant **`tenants-index`** (longest-prefix,
   case-insensitive, as `TenantEventRouter` already does). Ensure `tenants-index` is a provisioned/Active
   tenant so `ITenantStatusAccessor` does not drop the event.
2. **Recognize the `SearchIndexEntryChanged` / `SearchIndexEntryRemoved` CloudEvent types** on the ingestion
   topic (`EventIngestionController` / the routing pipeline) and route them to curated index maintenance
   rather than the generic raw-event ingestion path. The CloudEvent `type` is the contract name.
3. **Upsert by composite key `(TenantId, AggregateId)`.** Today ingestion dedups by `cloudevent.id` and has
   **no upsert** — re-posting the same id is silently dropped, so a rename would leave a permanently stale
   name. Implement replace-by-key: a new `SearchIndexEntryChanged` for an existing `(TenantId, AggregateId)`
   **overwrites** the prior entry's `Text` + `Attributes`. `SearchIndexEntryRemoved` deletes by the same key.
4. **Index the curated `Text` for BM25 (syntactic axis)** as the searchable content — NOT the raw event
   JSON. Because the `tenants-index` holds only one curated doc per tenant, a name search returns exactly one
   hit per matching tenant and is never polluted by member/config event text.
5. **Index `Attributes` as exactly-matched, filterable metadata** (e.g. `status`).
6. **Expose attribute filtering in the REST search API.** `SearchRequest` / `BuildSearchPath` /
   `GET /api/search` currently carry only `tenantId, axis, query, caseId, maxResults` — add an optional
   structured attribute filter (e.g. `attributeFilters` map) threaded into the search services so the BFF can
   pass `status` as an exact filter (replacing the interim hydrated-status filter in §4).
7. **Guarantee the aggregate id is recoverable from the hit.** Either (a) ensure `ScoredResult.SourceUri`
   echoes `cloudevent.id` verbatim (`tenant:{id}`) for curated entries — the existing mapper already sets
   `SourceUri = cloudevent.id`, so preserving that through curated indexing is sufficient — or (b) add an
   explicit `AggregateId` to `ScoredResult`. The BFF currently parses `SourceUri`; option (a) requires no BFF
   change.

## 4. Interim behavior until §3.6 lands

The BFF passes **no** attribute filter to `SearchAsync` and instead filters the **hydrated authoritative**
`TenantDetail.Status` after the match-set is resolved. This is exact (never fuzzy BM25-on-status-word) but
applies only across the returned page. Once §3.6 ships, switch the BFF to pass `status` as a structured
attribute filter (one-line change in `TenantQueryGateway.SearchTenantsAsync`).

## 5. AppHost / runtime wiring (Tenants side, code/config only)

The Tenants AppHost starts the Memories Server inline (see story Task 7). For end-to-end ingestion the
operator must ensure: `memories-server` is in the `pubsub` component scopes; `MEMORIES_EVENTSTORE_TOPIC` =
`memories-events`; the `SourceToTenantMap` entry from §3.1 is configured; FalkorDB + secret/LLM components
are up. Vector/semantic search needs a real embedding provider + secret and is out of scope here — the
`syntactic` (BM25) axis is what tenant search uses.

## 6. Verification (post-handoff)

1. Publish a `TenantCreated` → confirm one `tenants-index` doc with `SourceUri = "tenant:{id}"`,
   searchable `Text`, and `status` attribute.
2. Publish a `TenantUpdated` rename → confirm the doc's `Text` is **overwritten** (no stale name, no second doc).
3. `GET /api/search?tenantId=tenants-index&axis=syntactic&query={name}` → exactly one hit per matching tenant.
4. With §3.6: the same search + `status=Active` returns only active tenants.

---

## 7. Implementation status (2026-06-21, Memories side)

Landed in `Hexalith.Memories` (server side) — search now works end-to-end with the interim status filter:

- **§3.1 — routing + provisioning.** `tenants-index` is auto-provisioned at startup when opted in:
  `TenantEventRoutingOptions.AutoProvisionRoutedTenants` + a new
  `RoutedTenantProvisioningStartupService` (Server) provision each distinct `SourceToTenantMap` target via
  `TenantProvisioningWorkflow` and wait for Active. The Tenants AppHost sets
  `EventStoreIntegration__Routing__SourceToTenantMap__hexalith-tenants=tenants-index` and
  `EventStoreIntegration__Routing__AutoProvisionRoutedTenants=true` on `memories-server`.
- **§3.2 — recognition.** `EventIngestionService.ProcessAsync` branches `SearchIndexEntryChanged` /
  `SearchIndexEntryRemoved` (via `CuratedSearchIndexEventTypes`) to a new `ISearchIndexMaintenance` adapter
  **after** tenant-status routing but **before** the preflight `cloudevent.id` dedup — because the curated id
  is stable across revisions, dedup would have dropped renames.
- **§3.3 — upsert by key.** `RedisSearchIndexMaintenanceAdapter` writes one hash at
  `{tenantId}:mu:{aggregateId}`, so a re-published entry overwrites the prior text/attributes;
  `SearchIndexEntryRemoved` deletes by the same key. Naturally idempotent → no dedup needed.
- **§3.4 — BM25.** The curated `Text` is stored in the existing syntactic `content` field; the entry uses the
  existing syntactic schema so `SyntacticSearchService` returns it unchanged. **No schema change.**
- **§3.5 — attributes (partial).** Attributes are persisted as a flattened, searchable `metadataText` plus a
  verbatim `metadataJson` blob (exact values preserved). The exact-match attribute **TAG** field is part of
  §3.6 below.
- **§3.7 — SourceUri.** `sourceUri` = the CloudEvent `id` (`tenant:{id}`) verbatim → the BFF recovers the
  tenant id from a hit with no change.

Tests: `EventIngestionServiceTests` (curated dispatch: upsert/remove bypass dedup+workflow, malformed →
invalid, maintenance fault → retryable) and `RedisSearchIndexMaintenanceAdapterTests` (searchable hash keyed
by aggregate, delete by key, guards).

### Deferred: §3.6 (exact-match attribute filter API) — bundle with the BFF switch

§3.6 (a filterable `attributes` TAG field on the shared syntactic schema + the REST `attributeFilters` query
param + REST client plumbing) is **not** implemented. Rationale: per §4 the BFF currently filters on the
**hydrated authoritative status**, so §3.6 has no consumer until the (on-hold) story resumes and makes the
"one-line BFF change"; implementing it now would migrate the shared product schema (new TAG field across
every tenant index + the strict equality validator + a multi-field upgrade path) for an unused surface.
Attributes are already persisted (above), so §3.6 can be added later — schema TAG + REST plumbing + a
re-tag/republish — as one coherent change alongside the BFF switch.
