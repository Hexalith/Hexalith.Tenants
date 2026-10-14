# Audit stream storage and rollout

B6 separates the tenant aggregate named `system` from the global-administrator singleton. `system` remains a valid literal tenant identifier. The durable EventStore histories remain the source of truth; these keys hold disposable read models, not replacement event histories.

| Owner | Audit key after B6 | Legacy read source |
| --- | --- | --- |
| Ordinary tenant `{id}` | `audit:{id}` | Same key, unchanged |
| Tenant aggregate `system:tenants:system` | `audit-tenants:system` | Recognized tenant rows from `audit:system` |
| Singleton `system:global-administrators:global-administrators` | `audit-global-administrators:singleton` | Recognized global rows from `audit:system` |

The two dedicated keys are outside the entire `audit:{arbitraryTenantId}` namespace. A tenant identifier containing colons or a prefix-looking value cannot claim either dedicated audit key. Both audit writers use the EventStore read-model write policy to merge evidence by event identifier; duplicate and shorter replay deliveries preserve existing audit rows. Aggregate sequences remain local to their owner.

## Compatible reads

`GET /api/tenants/{tenantId}/audit` keeps its response shape and authorization requirement: the requester must be present in the current persisted global-administrator model. Authorization runs before audit reads or ownership diagnostics.

The optional `stream` query parameter accepts `tenant` or `global-administrators`. The latter is valid only with tenant identifier `system`. Omission preserves ordinary tenant reads and the combined system-scope feed. `stream=tenant` exposes only the literal tenant's evidence; `stream=global-administrators` exposes only singleton evidence. Filters, paging, signed cursor scopes and retained UI snapshots include the selector. Changing streams requires a fresh first-page request and its own validator. Pre-B6 combined-system cursors are rejected safely because owner-aware paging uses a new generation; restart those reads from the first page. Ordinary default tenant cursor scopes remain compatible.

Combined system reads use an opaque composite version, never either aggregate's ordered sequence. Explicit global reads can expose an ordered global sequence only when the dedicated canonical model supports that evidence; an old legacy sequence cannot establish completion. Global correction proof and retained original-global-receipt recovery explicitly request the global stream. Missing or incomplete provenance keeps those flows unavailable. A combined validator must cover every contributing source; a missing reliable validator must not be replaced by one source's ETag.

Receipt selection and existing receipt links use the original event reference. If the same reference exists under both owners, the combined feed refuses the entire ambiguous source before filters or paging can hide one side. Explicit tenant/global reads still expose each owner's unchanged evidence. No reference is renamed or discarded.

The compatibility reader leaves `audit:system` untouched and unions recognized legacy rows with dedicated records. Exact supported event types and matching tenant identity establish row ownership. Unknown, null or inconsistent legacy ownership, internal legacy conflicts, or conflicting evidence between the selected owner and its dedicated record causes a safe unavailable read. An explicit owner read does not load the other owner's dedicated record. It does not guess, silently discard uncertain rows, or rewrite legacy state. Recognized existing evidence remains readable even when a dedicated record already exists. When identical core evidence is already represented canonically, missing additive legacy provenance does not conflict with the canonical replay's verified metadata; contradictory nonnull provenance does. Redundant old legacy timestamps do not make a complete current canonical replay permanently stale. This code cannot recover evidence already overwritten before B6.

The current EventStore query failure taxonomy maps this domain refusal to sanitized HTTP 500; the UI displays its established `Error` state, with no rows or authoritative metadata. B6 verifies that boundary and does not add domain HTTP plumbing or change platform dependencies. This is a fail-closed response, not a claim that replay or an ownership reconciliation has succeeded.

## Rollout prerequisites

1. Inventory every replica, projection subscriber and operator replay route. Stop old projection writers and pause replay while deploying B6 readers and writers together. Drain in-flight old writes before allowing B6 projections. Mixed old/new writers can still damage `audit:system`; the compatibility reader cannot make that deployment safe.
2. Retain a verified backup of the durable histories and existing read-model records, including raw `audit:system` contents and metadata. Record each source identity and high-water mark. Keep legacy evidence immutable throughout the transition. Do not rename tenants or reserve `system`.
3. Inspect legacy ownership against the exact event-type allowlists in source. If evidence is ambiguous or conflicting, keep affected audit/correction reads unavailable. An operator must establish ownership from the authoritative histories under a separately approved reconciliation plan. No automatic inference or migration is provided here.
4. Establish serialization for each supported replay path, including replicas and operator-triggered requests. DW-627 remains open: this change does not prove distributed serialization of the singleton authority model. Prevent overlapping or stale singleton rebuilds operationally before any replay.
5. Confirm complete authoritative histories for both `system:tenants:system` and the global singleton, independently. A singleton replay alone cannot rebuild tenant evidence lost under the old shared key. Include stable event identifiers, actors and aggregate-local sequences; never substitute wall time or cross-aggregate ordering.

## Replay and verification

After the prerequisites are satisfied and replay is separately authorized, rebuild each owner through the supported EventStore projection path. Do not edit events or write projection state directly. This repository change performs neither production replay nor data migration, and supplies no production replay command.

Verify persisted end-state after each owner replay: its dedicated audit record must retain existing evidence and contain the expected authoritative rows exactly once; the other owner's dedicated record and the immutable legacy record must remain unchanged. Repeat in the reverse order and with duplicate delivery in a rehearsal environment. Verify ordinary tenant, explicit system-tenant, explicit global and combined system reads, filtering, paging and cache behavior with an authorized requester; verify unauthorized reads fail before evidence access.

For global correction, additionally require a complete authorized `/api/global-administrators` read and explicit global audit read with ordered `tenant-sequence:<number>` provenance covering the relevant committed sequence. Missing metadata, ambiguous ownership or incomplete evidence keeps correction unavailable. A successful command or projection response alone is insufficient.

Keep old backups and legacy records until a separately approved retention/reconciliation decision. Rolling back to old writers reintroduces the collision, so pause writers/replay before rollback. Candidate audit performance, production provider/authority guarantees, distributed replay serialization and broader release qualification remain separate gates.
