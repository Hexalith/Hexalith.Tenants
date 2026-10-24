# Tenant projection schema rebuild

This is a planning and rehearsal procedure. Executing a production rebuild,
changing a selected read-model generation or store, retiring records, and restoring
backups each require separate explicit operator authorization. This change runs no
production replay and supplies no automatic schema migration or recovery command.
Canonical events are immutable. Business corrections still use compensating commands.

A normal replay skips events at or below a persisted aggregate watermark. Replaying
the same history against existing keys therefore cannot apply newly introduced
historical projection logic. `ProjectedAt`, a tenant detail version, wall time and
an HTTP success response cannot establish completeness of the shared tenant index.

## Scope and admission

Inventory every tenant by its exact ordinal identity `system:tenants:{tenantId}`,
including the literal tenant `system`, tenants with no current members and disabled
tenants. Include every key affected by the changed schema:

| Model | Durable key | Rebuild scope |
| --- | --- | --- |
| Tenant detail, members and configuration | `projection:tenants:{tenantId}` | Complete history for each affected tenant |
| Ordinary tenant audit | `audit:{tenantId}` | Every ordinary tenant history |
| Literal system-tenant audit | `audit-tenants:system` | `system:tenants:system` only |
| Tenant list and user membership index | `projection:tenant-index:singleton` | All tenant histories, including unaffected tenants |
| Global administrator authority | `projection:global-administrators:singleton` | Separate global history if moving a whole projection namespace/store |
| Global administrator audit | `audit-global-administrators:singleton` | Separate global history if moving a whole projection namespace/store |
| Immutable legacy audit compatibility evidence | `audit:system` | Retain original evidence and ownership decisions; never infer or rewrite owners |

The index stores `AggregateSequences`, an ordinal dictionary of aggregate-local
watermarks. A missing tenant slice initializes only from a contiguous history
beginning at sequence one and containing every sequence through its supplied head.
Known pre-creation `TenantNotFoundRejection` and `InsufficientPermissionsRejection`
events may precede `TenantCreated`; a rejection-only history establishes an empty
owner slice without creating a tenant row. Successful tenant changes require the
matching creation first. Same-sequence sibling events remain together. Persisted
owner watermarks must be positive; zero and negative values are refused before writes.
A suffix or gap is refused
before detail, audit or index writes. An existing legacy tenant slice without its
own watermark is also refused before writes, even if its detail model is current.
A contiguous older prefix cannot prove which newer events that legacy slice already
contains. Other watermarked tenants remain projectable while the affected legacy
slice awaits the coordinated rebuild. Missing legacy metadata deserializes compatibly;
it does not silently establish freshness or authorize partial migration.

A populated global-administrator singleton without a positive ordered version is
also refused before audit or authority writes. A supplied older prefix cannot prove
freshness against that legacy authority. A missing or empty singleton may initialize
populated authority only from a complete canonical replay. Incomplete requests cannot
replace existing ordered authority or establish a new ordered version. A populated
unknown-version singleton requires the coordinated rebuild with a verified frozen
global source head. This does not change existing revocation freshness policy.

The preflight and guarded index transform both check initialization. Multi-key
writes remain independently guarded rather than transactional: a conflict or failure
can leave a subset committed. Retrying the identical authoritative history converges
without duplicating audit evidence or regressing ordered index state. A failed request
is never proof of completed cutover.

## Preparation and writer fencing

1. Record the old and candidate application revisions, exact root dependency
   revisions, schema change, affected key inventory, operator, approval, rollback
   criteria and separately authorized execution window. Pin the source and binaries.
2. Inventory and fence **all** projection writers: replicas, subscribers, operator
   replay tools, scheduled jobs and old deployments. Pause command/delivery intake
   or use a platform-supported durable buffering and catch-up plan. Drain in-flight
   writes and retain the fence through final validation and cutover. Do not mix old
   and new schema writers or old unguarded singleton writers.
3. Back up complete authoritative event histories and existing read-model contents
   and metadata. Preserve `audit:system` and both dedicated audit records independently.
   Verify restore in isolation. Record each aggregate identity, frozen durable head,
   stable message identifiers, actor identities, payload bytes and aggregate-local
   sequences in a manifest. Sequences from different aggregates are incomparable.
4. Prove every prefix is complete through its frozen source head using EventStore's
   authoritative history facilities. Include rejection events and same-sequence
   siblings where present. Pub/sub deliveries, client projections, detail snapshots,
   list/search results and audit projections are not substitutes for history.
   Missing, erased, ambiguous or contradictory history blocks the rebuild; do not
   manufacture rows, rename identities or lower the expected source head.
5. Choose a platform-supported isolated read-model generation or a projection-only
   store/namespace, with empty candidate keys and the exact new handler binaries.
   Preserve the old generation for rollback. The current Tenants `/project` route
   writes fixed keys in `statestore` and does not select a generation or verify
   evolution proofs. Do not invent a direct state-write/reset endpoint or send
   unsupported evolution hints to bypass its admission. If the deployed platform
   cannot isolate and select all affected projection keys safely, obtain that
   platform capability and its approved execution plan before proceeding. Never
   replace a shared store that also contains canonical events or unrelated state.

## Isolated replay and validation

Replay each tenant's complete authoritative prefix through the supported protected
EventStore projection path into the isolated candidate environment. Rebuild the shared
index from **all** tenant histories; rebuilding only changed tenant details is
insufficient. Replay the global owner separately when the selected namespace includes
its keys, following [audit storage rollout](audit-stream-rollout.md). Preserve immutable
legacy compatibility evidence using the approved platform backup/restore facility;
if ownership cannot be reconciled with canonical history, keep the affected reads
unavailable and stop cutover. Do not copy an old watermark onto newly interpreted data.

Compare persisted contents with an independent fold of the manifest's histories:
names, lifecycle, membership roles, configuration, every index tenant/user edge, and
each owner's exact watermark must match. Check tenant/audit isolation for literal
`system`, colons and case-distinct identities. Audit event identifiers must appear
exactly once with the original actor and owner; canonical completion provenance must
cover each required source head. Authorization must derive from the independently
rebuilt global singleton. Preserve existing support-safe failure behavior.

Rehearse duplicate delivery, older and equal prefixes, all same-sequence siblings,
ETag conflicts, a competing newer replay and a failure between individual key writes.
Retry and inspect durable end-state, including unrelated tenants and the other audit
owner. Exercise protected query reads, list/user membership, authorized and unauthorized
audit reads, paging, validators and freshness headers. Old cursors and retained UI
snapshots must be invalidated or restarted when their selected generation changes;
never reuse a validator as proof across generations. Record exact checks and results.

## Cutover and rollback

Keep writers fenced while confirming the candidate covers every frozen head. Account
for any queued post-fence events by a verified ordered catch-up plan and recheck the
manifest heads. Switch all readers and writers to the validated schema/generation
through the platform-supported coordinated deployment. Check selection consistency
before releasing buffered delivery and command intake. Monitor projection failures,
lag, persisted watermarks, authority and isolation after resuming.

On a failed validation, incomplete history, mixed selection or regressed authority,
keep the fence in place and select the preserved old generation with matching old
binaries using the separately authorized rollback plan. Account for every event
committed since the backup; never roll canonical events back. Do not re-enable old
writers against new-schema records or restore only details while leaving a newer
shared index/audit model selected. Revalidate durable state and authority before
resuming. Preserve backups and both generations until retention is separately approved.

These steps establish a coordinated tenant schema procedure. They do not settle
unresolved revocation freshness, search paging, status, production-provider authority
or broader release-acceptance policies.
